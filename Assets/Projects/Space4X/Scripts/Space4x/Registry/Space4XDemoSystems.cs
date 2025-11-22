using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Knowledge;
using PureDOTS.Runtime.Resource;
using PureDOTS.Runtime.Skills;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Space4X.Registry
{
    /// <summary>
    /// Manages carrier patrol behavior, generating waypoints and waiting at them.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct CarrierPatrolSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private Random _random;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(false);
            _random = new Random(12345u);
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);

            // Use TimeState.FixedDeltaTime for consistency with PureDOTS patterns
            var deltaTime = SystemAPI.TryGetSingleton<TimeState>(out var timeState) 
                ? timeState.FixedDeltaTime 
                : SystemAPI.Time.DeltaTime;

            var carrierQuery = SystemAPI.QueryBuilder()
                .WithAll<Carrier, PatrolBehavior, MovementCommand, LocalTransform>()
                .Build();
            var carrierCount = carrierQuery.CalculateEntityCount();

            if (carrierCount == 0)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning("[CarrierPatrolSystem] No carriers found! Make sure Space4XMiningDemoAuthoring component is configured and entities are baked.");
#endif
                return;
            }

            foreach (var (carrier, patrol, movement, transform, entity) in SystemAPI.Query<RefRO<Carrier>, RefRW<PatrolBehavior>, RefRW<MovementCommand>, RefRW<LocalTransform>>().WithEntityAccess())
            {
                var carrierData = carrier.ValueRO;
                var position = transform.ValueRO.Position;
                var movementCmd = movement.ValueRO;
                var patrolBehavior = patrol.ValueRO;

                // Initialize waypoint if it's uninitialized (zero vector or very close to current position)
                var waypointInitialized = math.lengthsq(patrolBehavior.CurrentWaypoint) > 0.01f;
                if (!waypointInitialized || math.distance(position, patrolBehavior.CurrentWaypoint) < 0.01f)
                {
                    // Generate initial waypoint
                    var angle = _random.NextFloat(0f, math.PI * 2f);
                    var radius = _random.NextFloat(0f, carrierData.PatrolRadius);
                    var offset = new float3(
                        math.cos(angle) * radius,
                        0f,
                        math.sin(angle) * radius
                    );
                    patrolBehavior.CurrentWaypoint = carrierData.PatrolCenter + offset;
                    patrolBehavior.WaitTimer = 0f;
                    waypointInitialized = true;
                }

                // Check if we've arrived at the current waypoint
                var distanceToWaypoint = math.distance(position, patrolBehavior.CurrentWaypoint);
                var arrivalThreshold = movementCmd.ArrivalThreshold > 0f ? movementCmd.ArrivalThreshold : 1f;

                if (distanceToWaypoint <= arrivalThreshold)
                {
                    // Update wait timer
                    patrolBehavior.WaitTimer += deltaTime;

                    if (patrolBehavior.WaitTimer >= patrolBehavior.WaitTime)
                    {
                        // Generate new waypoint within patrol radius
                        var angle = _random.NextFloat(0f, math.PI * 2f);
                        var radius = _random.NextFloat(0f, carrierData.PatrolRadius);
                        var offset = new float3(
                            math.cos(angle) * radius,
                            0f,
                            math.sin(angle) * radius
                        );
                        var newWaypoint = carrierData.PatrolCenter + offset;

                        patrolBehavior.CurrentWaypoint = newWaypoint;
                        patrolBehavior.WaitTimer = 0f;

                        movement.ValueRW = new MovementCommand
                        {
                            TargetPosition = newWaypoint,
                            ArrivalThreshold = arrivalThreshold
                        };
                    }
                }
                else
                {
                    // Move towards waypoint
                    var toWaypoint = patrolBehavior.CurrentWaypoint - position;
                    var distanceSq = math.lengthsq(toWaypoint);
                    
                    if (distanceSq > 0.0001f) // Safety check to avoid normalizing zero vector
                    {
                        var direction = math.normalize(toWaypoint);
                        var movementSpeed = carrierData.Speed * deltaTime;
                        var newPosition = position + direction * movementSpeed;

                        transform.ValueRW = LocalTransform.FromPositionRotationScale(newPosition, transform.ValueRO.Rotation, transform.ValueRO.Scale);

                        // Update movement command target if needed
                        if (math.distance(position, movementCmd.TargetPosition) > arrivalThreshold * 2f)
                        {
                            movement.ValueRW = new MovementCommand
                            {
                                TargetPosition = patrolBehavior.CurrentWaypoint,
                                ArrivalThreshold = arrivalThreshold
                            };
                        }
                    }
                }

                patrol.ValueRW = patrolBehavior;
            }
        }
    }

    /// <summary>
    /// Manages mining vessel behavior: moving to asteroids, mining, returning to carrier, and transferring resources.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CarrierPatrolSystem))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct MiningVesselSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<Carrier> _carrierLookup;
        private ComponentLookup<Asteroid> _asteroidLookup;
        private BufferLookup<ResourceStorage> _resourceStorageLookup;
        private ComponentLookup<SkillSet> _skillSetLookup;
        private ComponentLookup<VillagerKnowledge> _knowledgeLookup;
        private EntityQuery _asteroidQuery;
        private static readonly FixedString64Bytes DefaultResourceTag = default;
        private static readonly FixedString64Bytes MineralsTag = CreateMineralsTag();
        private static readonly FixedString64Bytes RareMetalsTag = CreateRareMetalsTag();
        private static readonly FixedString64Bytes EnergyCrystalsTag = CreateEnergyCrystalsTag();
        private static readonly FixedString64Bytes OrganicMatterTag = CreateOrganicMatterTag();

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(false);
            _carrierLookup = state.GetComponentLookup<Carrier>(true);
            _asteroidLookup = state.GetComponentLookup<Asteroid>(false);
            _resourceStorageLookup = state.GetBufferLookup<ResourceStorage>(false);
            _skillSetLookup = state.GetComponentLookup<SkillSet>(true);
            _knowledgeLookup = state.GetComponentLookup<VillagerKnowledge>(true);
            state.RequireForUpdate<KnowledgeLessonEffectCatalog>();

            _asteroidQuery = SystemAPI.QueryBuilder()
                .WithAll<Asteroid, LocalTransform>()
                .Build();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);
            _carrierLookup.Update(ref state);
            _asteroidLookup.Update(ref state);
            _resourceStorageLookup.Update(ref state);
            _skillSetLookup.Update(ref state);
            _knowledgeLookup.Update(ref state);

            // Use TimeState.FixedDeltaTime for consistency with PureDOTS patterns
            var deltaTime = SystemAPI.TryGetSingleton<TimeState>(out var timeState) 
                ? timeState.FixedDeltaTime 
                : SystemAPI.Time.DeltaTime;

            var lessonCatalog = SystemAPI.GetSingleton<KnowledgeLessonEffectCatalog>();
            var lessonBlob = lessonCatalog.Blob;
            if (!lessonBlob.IsCreated)
            {
                return;
            }

            ref var lessonBlobValue = ref lessonBlob.Value;

            // Collect available asteroids
            var asteroidList = new NativeList<(Entity entity, float3 position, Asteroid asteroid)>(Allocator.Temp);
            foreach (var (asteroid, transform, entity) in SystemAPI.Query<RefRO<Asteroid>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (asteroid.ValueRO.ResourceAmount > 0f)
                {
                    asteroidList.Add((entity, transform.ValueRO.Position, asteroid.ValueRO));
                }
            }

            var vesselQuery = SystemAPI.QueryBuilder()
                .WithAll<MiningVessel, MiningJob, LocalTransform>()
                .Build();
            var vesselCount = vesselQuery.CalculateEntityCount();

#if UNITY_EDITOR
            if (vesselCount == 0)
            {
                UnityEngine.Debug.LogWarning("[MiningVesselSystem] No mining vessels found! Make sure Space4XMiningDemoAuthoring component is configured.");
            }
            else if (asteroidList.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[MiningVesselSystem] No asteroids found! Make sure asteroids are configured in Space4XMiningDemoAuthoring.");
            }
#endif

            foreach (var (vessel, job, transform, entity) in SystemAPI.Query<RefRW<MiningVessel>, RefRW<MiningJob>, RefRW<LocalTransform>>().WithEntityAccess())
            {
                var vesselData = vessel.ValueRO;
                var jobData = job.ValueRO;
                var position = transform.ValueRO.Position;

                switch (jobData.State)
                {
                    case MiningJobState.None:
                        // Find nearest asteroid
                        Entity? nearestAsteroid = null;
                        float nearestDistance = float.MaxValue;

                        for (int i = 0; i < asteroidList.Length; i++)
                        {
                            var asteroidEntry = asteroidList[i];
                            var distance = math.distance(position, asteroidEntry.position);
                            if (distance < nearestDistance)
                            {
                                nearestDistance = distance;
                                nearestAsteroid = asteroidEntry.entity;
                            }
                        }

                        if (nearestAsteroid.HasValue)
                        {
                            job.ValueRW = new MiningJob
                            {
                                State = MiningJobState.MovingToAsteroid,
                                TargetAsteroid = nearestAsteroid.Value,
                                MiningProgress = 0f
                            };
                        }
                        break;

                    case MiningJobState.MovingToAsteroid:
                        if (!_asteroidLookup.HasComponent(jobData.TargetAsteroid))
                        {
                            job.ValueRW = new MiningJob { State = MiningJobState.None };
                            break;
                        }

                        var asteroidTransform = _transformLookup[jobData.TargetAsteroid];
                        var asteroidPosition = asteroidTransform.Position;
                        var distanceToAsteroid = math.distance(position, asteroidPosition);

                        if (distanceToAsteroid <= 2f)
                        {
                            job.ValueRW = new MiningJob
                            {
                                State = MiningJobState.Mining,
                                TargetAsteroid = jobData.TargetAsteroid,
                                MiningProgress = 0f
                            };
                        }
                        else
                        {
                            var toAsteroid = asteroidPosition - position;
                            var distanceSq = math.lengthsq(toAsteroid);
                            
                            if (distanceSq > 0.0001f) // Safety check to avoid normalizing zero vector
                            {
                                var direction = math.normalize(toAsteroid);
                                var movementSpeed = vesselData.Speed * deltaTime;
                                var newPosition = position + direction * movementSpeed;
                                transform.ValueRW = LocalTransform.FromPositionRotationScale(newPosition, transform.ValueRO.Rotation, transform.ValueRO.Scale);
                            }
                        }
                        break;

                    case MiningJobState.Mining:
                        if (!_asteroidLookup.HasComponent(jobData.TargetAsteroid))
                        {
                            job.ValueRW = new MiningJob { State = MiningJobState.None };
                            break;
                        }

                        var asteroid = _asteroidLookup[jobData.TargetAsteroid];
                        if (asteroid.ResourceAmount <= 0f || vesselData.CurrentCargo >= vesselData.CargoCapacity)
                        {
                            // Start returning to carrier
                            job.ValueRW = new MiningJob
                            {
                                State = MiningJobState.ReturningToCarrier,
                                TargetAsteroid = jobData.TargetAsteroid,
                                MiningProgress = 0f
                            };
                            break;
                        }

                        var lessonIds = BuildLessonList(entity, vesselData.CarrierEntity);
                        var knowledgeFlags = GetKnowledgeFlags(entity) | GetKnowledgeFlags(vesselData.CarrierEntity);
                        var resourceTypeId = ToResourceTypeId(asteroid.ResourceType);
                        var modifiers = KnowledgeLessonEffectUtility.EvaluateHarvestModifiers(ref lessonBlobValue, lessonIds, resourceTypeId, asteroid.QualityTier);
                        var skillLevel = CombineSkillLevels(entity, vesselData.CarrierEntity);

                        var miningRate = vesselData.MiningEfficiency * asteroid.MiningRate * deltaTime * modifiers.YieldMultiplier;
                        miningRate /= math.max(0.1f, ResourceQualityUtility.GetHarvestTimeMultiplier(skillLevel) * modifiers.HarvestTimeMultiplier);

                        var amountToMine = math.min(miningRate, asteroid.ResourceAmount);
                        amountToMine = math.min(amountToMine, vesselData.CargoCapacity - vesselData.CurrentCargo);

                        // Update asteroid resource amount
                        var asteroidRef = _asteroidLookup.GetRefRW(jobData.TargetAsteroid);
                        asteroidRef.ValueRW.ResourceAmount -= amountToMine;

                        // Update vessel cargo
                        if (amountToMine > 0f)
                        {
                            var minedQuality = KnowledgeLessonEffectUtility.EvaluateHarvestQuality(
                                asteroid.BaseQuality,
                                asteroid.QualityVariance,
                                asteroid.QualityTier,
                                skillLevel,
                                modifiers,
                                knowledgeFlags);

                            var cargoQuality = vessel.ValueRO.AverageCargoQuality;
                            if (vessel.ValueRO.CurrentCargo <= 0f)
                            {
                                cargoQuality = minedQuality;
                            }
                            else
                            {
                                cargoQuality = ResourceQualityUtility.BlendQuality(
                                    vessel.ValueRO.AverageCargoQuality,
                                    vessel.ValueRO.CurrentCargo,
                                    minedQuality,
                                    amountToMine);
                            }

                            vessel.ValueRW.AverageCargoQuality = cargoQuality;
                            vessel.ValueRW.CargoTier = (byte)ResourceQualityUtility.DetermineTier(cargoQuality);
                            vessel.ValueRW.CurrentCargo += amountToMine;
                        }

                        // Update mining progress
                        job.ValueRW = new MiningJob
                        {
                            State = MiningJobState.Mining,
                            TargetAsteroid = jobData.TargetAsteroid,
                            MiningProgress = jobData.MiningProgress + miningRate
                        };

                        if (vessel.ValueRO.CurrentCargo >= vessel.ValueRO.CargoCapacity || asteroidRef.ValueRO.ResourceAmount <= 0f)
                        {
                            job.ValueRW = new MiningJob
                            {
                                State = MiningJobState.ReturningToCarrier,
                                TargetAsteroid = jobData.TargetAsteroid,
                                MiningProgress = 0f
                            };
                        }
                        break;

                    case MiningJobState.ReturningToCarrier:
                        if (!_carrierLookup.HasComponent(vesselData.CarrierEntity))
                        {
                            job.ValueRW = new MiningJob { State = MiningJobState.None };
                            break;
                        }

                        var carrierTransform = _transformLookup[vesselData.CarrierEntity];
                        var carrierPosition = carrierTransform.Position;
                        var distanceToCarrier = math.distance(position, carrierPosition);

                        if (distanceToCarrier <= 3f)
                        {
                            job.ValueRW = new MiningJob
                            {
                                State = MiningJobState.TransferringResources,
                                TargetAsteroid = jobData.TargetAsteroid,
                                MiningProgress = 0f
                            };
                        }
                        else
                        {
                            var toCarrier = carrierPosition - position;
                            var distanceSq = math.lengthsq(toCarrier);
                            
                            if (distanceSq > 0.0001f) // Safety check to avoid normalizing zero vector
                            {
                                var direction = math.normalize(toCarrier);
                                var movementSpeed = vesselData.Speed * deltaTime;
                                var newPosition = position + direction * movementSpeed;
                                transform.ValueRW = LocalTransform.FromPositionRotationScale(newPosition, transform.ValueRO.Rotation, transform.ValueRO.Scale);
                            }
                        }
                        break;

                    case MiningJobState.TransferringResources:
                        if (!_carrierLookup.HasComponent(vesselData.CarrierEntity))
                        {
                            job.ValueRW = new MiningJob { State = MiningJobState.None };
                            break;
                        }

                        if (vessel.ValueRO.CurrentCargo <= 0f)
                        {
                            // Reset and start new mining cycle
                            job.ValueRW = new MiningJob { State = MiningJobState.None };
                            vessel.ValueRW.CurrentCargo = 0f;
                            break;
                        }

                        // Determine resource type from asteroid if available
                        var resourceType = ResourceType.Minerals;
                        if (_asteroidLookup.HasComponent(jobData.TargetAsteroid))
                        {
                            resourceType = _asteroidLookup[jobData.TargetAsteroid].ResourceType;
                        }

                        // Transfer resources to carrier's ResourceStorage buffer
                        if (_resourceStorageLookup.HasBuffer(vesselData.CarrierEntity))
                        {
                            var resourceBuffer = _resourceStorageLookup[vesselData.CarrierEntity];
                            var cargoToTransfer = vessel.ValueRO.CurrentCargo;
                            var cargoTier = vessel.ValueRO.CargoTier;
                            var cargoQuality = vessel.ValueRO.AverageCargoQuality;

                            // Find or create resource storage slot for this type
                            bool foundSlot = false;
                            for (int i = 0; i < resourceBuffer.Length; i++)
                            {
                                if (resourceBuffer[i].Type == resourceType)
                                {
                                    var storage = resourceBuffer[i];
                                    var remaining = storage.AddAmount(cargoToTransfer, cargoTier, cargoQuality);
                                    resourceBuffer[i] = storage;

                                    // Update vessel cargo
                                    vessel.ValueRW.CurrentCargo = remaining;
                                    foundSlot = true;
                                    break;
                                }
                            }

                            if (!foundSlot && resourceBuffer.Length < 4)
                            {
                                var newStorage = ResourceStorage.Create(resourceType);
                                var remaining = newStorage.AddAmount(cargoToTransfer, cargoTier, cargoQuality);
                                resourceBuffer.Add(newStorage);

                                vessel.ValueRW.CurrentCargo = remaining;
                            }

                            if (vessel.ValueRO.CurrentCargo <= 0f)
                            {
                                job.ValueRW = new MiningJob { State = MiningJobState.None };
                                vessel.ValueRW.AverageCargoQuality = 0;
                                vessel.ValueRW.CargoTier = (byte)ResourceQualityTier.Unknown;
                            }
                        }
                        break;
                }
            }

            asteroidList.Dispose();
        }

        private float CombineSkillLevels(Entity vessel, Entity carrier)
        {
            return math.max(GetSkillLevel(vessel), GetSkillLevel(carrier));
        }

        private float GetSkillLevel(Entity entity)
        {
            if (entity == Entity.Null || !_skillSetLookup.HasComponent(entity))
            {
                return 0f;
            }

            var skillSet = _skillSetLookup[entity];
            return skillSet.GetLevel(SkillId.Mining);
        }

        private uint GetKnowledgeFlags(Entity entity)
        {
            if (entity == Entity.Null || !_knowledgeLookup.HasComponent(entity))
            {
                return 0u;
            }

            return _knowledgeLookup[entity].Flags;
        }

        private FixedList32Bytes<VillagerLessonProgress> BuildLessonList(Entity vessel, Entity carrier)
        {
            var lessons = new FixedList32Bytes<VillagerLessonProgress>();
            AppendLessons(ref lessons, vessel);
            AppendLessons(ref lessons, carrier);
            return lessons;
        }

        private void AppendLessons(ref FixedList32Bytes<VillagerLessonProgress> list, Entity entity)
        {
            if (entity == Entity.Null || !_knowledgeLookup.HasComponent(entity))
            {
                return;
            }

            var knowledge = _knowledgeLookup[entity];
            for (int i = 0; i < knowledge.Lessons.Length; i++)
            {
                AddLesson(ref list, knowledge.Lessons[i]);
            }
        }

        private static void AddLesson(ref FixedList32Bytes<VillagerLessonProgress> list, in VillagerLessonProgress lesson)
        {
            if (lesson.LessonId.Length == 0)
            {
                return;
            }

            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].LessonId.Equals(lesson.LessonId))
                {
                    if (lesson.Progress > list[i].Progress)
                    {
                        var entry = list[i];
                        entry.Progress = lesson.Progress;
                        list[i] = entry;
                    }
                    return;
                }
            }

            if (list.Length < list.Capacity)
            {
                list.Add(lesson);
            }
        }

        private static FixedString64Bytes ToResourceTypeId(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Minerals:
                    return MineralsTag;
                case ResourceType.RareMetals:
                    return RareMetalsTag;
                case ResourceType.EnergyCrystals:
                    return EnergyCrystalsTag;
                case ResourceType.OrganicMatter:
                    return OrganicMatterTag;
                default:
                    return DefaultResourceTag;
            }
        }

        private static FixedString64Bytes CreateMineralsTag()
        {
            FixedString64Bytes fs = default;
            fs.Append('s'); fs.Append('p'); fs.Append('a'); fs.Append('c'); fs.Append('e'); fs.Append('4'); fs.Append('x'); fs.Append('.'); fs.Append('m'); fs.Append('i'); fs.Append('n'); fs.Append('e'); fs.Append('r'); fs.Append('a'); fs.Append('l'); fs.Append('s');
            return fs;
        }

        private static FixedString64Bytes CreateRareMetalsTag()
        {
            FixedString64Bytes fs = default;
            fs.Append('s'); fs.Append('p'); fs.Append('a'); fs.Append('c'); fs.Append('e'); fs.Append('4'); fs.Append('x'); fs.Append('.'); fs.Append('r'); fs.Append('a'); fs.Append('r'); fs.Append('e'); fs.Append('_'); fs.Append('m'); fs.Append('e'); fs.Append('t'); fs.Append('a'); fs.Append('l'); fs.Append('s');
            return fs;
        }

        private static FixedString64Bytes CreateEnergyCrystalsTag()
        {
            FixedString64Bytes fs = default;
            fs.Append('s'); fs.Append('p'); fs.Append('a'); fs.Append('c'); fs.Append('e'); fs.Append('4'); fs.Append('x'); fs.Append('.'); fs.Append('e'); fs.Append('n'); fs.Append('e'); fs.Append('r'); fs.Append('g'); fs.Append('y'); fs.Append('_'); fs.Append('c'); fs.Append('r'); fs.Append('y'); fs.Append('s'); fs.Append('t'); fs.Append('a'); fs.Append('l'); fs.Append('s');
            return fs;
        }

        private static FixedString64Bytes CreateOrganicMatterTag()
        {
            FixedString64Bytes fs = default;
            fs.Append('s'); fs.Append('p'); fs.Append('a'); fs.Append('c'); fs.Append('e'); fs.Append('4'); fs.Append('x'); fs.Append('.'); fs.Append('o'); fs.Append('r'); fs.Append('g'); fs.Append('a'); fs.Append('n'); fs.Append('i'); fs.Append('c'); fs.Append('_'); fs.Append('m'); fs.Append('a'); fs.Append('t'); fs.Append('t'); fs.Append('e'); fs.Append('r');
            return fs;
        }
    }
}

