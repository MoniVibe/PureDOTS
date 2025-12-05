using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Mobility;
using PureDOTS.Runtime.Performance;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems.Performance;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.AI
{
    internal struct AISensorCategoryMask
    {
        public FixedList32Bytes<AISensorCategory> Categories;

        public static AISensorCategoryMask FromConfig(in AISensorConfig config)
        {
            var mask = new AISensorCategoryMask { Categories = default };
            if (config.PrimaryCategory != AISensorCategory.None)
            {
                mask.Categories.Add(config.PrimaryCategory);
            }

            if (config.SecondaryCategory != AISensorCategory.None &&
                config.SecondaryCategory != config.PrimaryCategory)
            {
                mask.Categories.Add(config.SecondaryCategory);
            }

            return mask;
        }
    }

    internal struct AISensorCategoryFilter : ISpatialQueryFilter
    {
        [ReadOnly] public NativeArray<AISensorCategoryMask> Masks;
        [ReadOnly] public ComponentLookup<VillagerId> VillagerLookup;
        [ReadOnly] public ComponentLookup<ResourceSourceConfig> ResourceLookup;
        [ReadOnly] public ComponentLookup<StorehouseConfig> StorehouseLookup;
        // Miracle category is game-specific (Godgame) - games can extend this filter if needed
        [ReadOnly] public ComponentLookup<TransportUnitTag> TransportLookup;

        public bool Accept(int descriptorIndex, in SpatialQueryDescriptor descriptor, in SpatialGridEntry entry)
        {
            if (!Masks.IsCreated || descriptorIndex >= Masks.Length)
            {
                return true;
            }

            var categories = Masks[descriptorIndex].Categories;
            if (categories.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < categories.Length; i++)
            {
                var category = categories[i];
                switch (category)
                {
                    case AISensorCategory.Villager:
                        if (VillagerLookup.HasComponent(entry.Entity))
                        {
                            return true;
                        }
                        break;
                    case AISensorCategory.ResourceNode:
                        if (ResourceLookup.HasComponent(entry.Entity))
                        {
                            return true;
                        }
                        break;
                    case AISensorCategory.Storehouse:
                        if (StorehouseLookup.HasComponent(entry.Entity))
                        {
                            return true;
                        }
                        break;
                    case AISensorCategory.TransportUnit:
                        if (TransportLookup.HasComponent(entry.Entity))
                        {
                            return true;
                        }
                        break;
                    case AISensorCategory.Miracle:
                        // Miracle category is game-specific - games can extend this filter if needed
                        // For now, skip miracle filtering in generic PureDOTS AI
                        break;
                    default:
                        return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// WARM path: Updates AI sensor awareness via spatial queries.
    /// Group-level sensing: sensor anchors (squad leaders, watchtowers) do the sensing.
    /// Respects budget and uses spatial hashing for efficient queries.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(WarmPathSystemGroup))]
    [UpdateAfter(typeof(UniversalPerformanceBudgetSystem))]
    public partial struct AISensorUpdateSystem : ISystem
    {
        private EntityQuery _sensorQuery;
        private ComponentLookup<VillagerId> _villagerLookup;
        private ComponentLookup<ResourceSourceConfig> _resourceLookup;
        private ComponentLookup<StorehouseConfig> _storehouseLookup;
        // Miracle lookup removed - Miracle category is game-specific (Godgame)
        private ComponentLookup<SpatialGridResidency> _residencyLookup;
        private ComponentLookup<TransportUnitTag> _transportLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _sensorQuery = SystemAPI.QueryBuilder()
                .WithAll<AISensorConfig, AISensorState, LocalTransform>()
                .Build();

            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<SpatialGridConfig>();
            state.RequireForUpdate<SpatialGridState>();
            state.RequireForUpdate<UniversalPerformanceBudget>();
            state.RequireForUpdate<UniversalPerformanceCounters>();
            state.RequireForUpdate(_sensorQuery);

            _villagerLookup = state.GetComponentLookup<VillagerId>(true);
            _resourceLookup = state.GetComponentLookup<ResourceSourceConfig>(true);
            _storehouseLookup = state.GetComponentLookup<StorehouseConfig>(true);
            // Miracle lookup removed - Miracle category is game-specific (Godgame)
            _residencyLookup = state.GetComponentLookup<SpatialGridResidency>(true);
            _transportLookup = state.GetComponentLookup<TransportUnitTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
            var gridConfig = SystemAPI.GetSingleton<SpatialGridConfig>();
            var gridState = SystemAPI.GetSingleton<SpatialGridState>();
            var rangesBuffer = state.EntityManager.GetBuffer<SpatialGridCellRange>(gridEntity);
            var entriesBuffer = state.EntityManager.GetBuffer<SpatialGridEntry>(gridEntity);
            if (rangesBuffer.Length == 0 || entriesBuffer.Length == 0)
            {
                return;
            }

            _villagerLookup.Update(ref state);
            _resourceLookup.Update(ref state);
            _storehouseLookup.Update(ref state);
            // Miracle lookup removed - Miracle category is game-specific (Godgame)
            _residencyLookup.Update(ref state);
            _transportLookup.Update(ref state);

            var descriptorList = new NativeList<SpatialQueryDescriptor>(Allocator.TempJob);
            var rangeList = new NativeList<SpatialQueryRange>(Allocator.TempJob);
            var sensorList = new NativeList<Entity>(Allocator.TempJob);
            var maskList = new NativeList<AISensorCategoryMask>(Allocator.TempJob);
            var configList = new NativeList<AISensorConfig>(Allocator.TempJob);

            var budget = SystemAPI.GetSingleton<UniversalPerformanceBudget>();
            var counters = SystemAPI.GetSingletonRW<UniversalPerformanceCounters>();
            
            var offset = 0;
            int processedCount = 0;
            foreach (var (config, sensorState, transform, entity) in SystemAPI.Query<RefRO<AISensorConfig>, RefRW<AISensorState>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                // Check budget
                if (processedCount >= budget.MaxPerceptionChecksPerTick)
                {
                    break;
                }

                var sensorConfig = config.ValueRO;
                var stateRef = sensorState.ValueRW;
                stateRef.Elapsed += timeState.FixedDeltaTime;

                if (sensorConfig.UpdateInterval > 0f &&
                    stateRef.Elapsed + 1e-5f < sensorConfig.UpdateInterval)
                {
                    sensorState.ValueRW = stateRef;
                    continue;
                }

                stateRef.Elapsed = 0f;
                stateRef.LastSampleTick = timeState.Tick;
                sensorState.ValueRW = stateRef;
                
                processedCount++;

                var capacity = math.max(1, sensorConfig.MaxResults);
                descriptorList.Add(new SpatialQueryDescriptor
                {
                    Origin = transform.ValueRO.Position,
                    Radius = math.max(sensorConfig.Range, 0.1f),
                    MaxResults = capacity,
                    Options = sensorConfig.QueryOptions | SpatialQueryOptions.IgnoreSelf | SpatialQueryOptions.RequireDeterministicSorting,
                    Tolerance = 1e-4f,
                    ExcludedEntity = entity
                });

                rangeList.Add(new SpatialQueryRange
                {
                    Start = offset,
                    Capacity = capacity,
                    Count = 0
                });

                sensorList.Add(entity);
                maskList.Add(AISensorCategoryMask.FromConfig(sensorConfig));
                configList.Add(sensorConfig);

                offset += capacity;
            }

            if (descriptorList.Length == 0)
            {
                descriptorList.Dispose();
                rangeList.Dispose();
                sensorList.Dispose();
                maskList.Dispose();
                configList.Dispose();
                return;
            }

            var resultsArray = new NativeArray<KNearestResult>(offset, Allocator.TempJob, NativeArrayOptions.ClearMemory);

            var job = new SpatialQueryHelper.SpatialKNearestBatchJob<AISensorCategoryFilter>
            {
                Config = gridConfig,
                CellRanges = rangesBuffer.AsNativeArray(),
                Entries = entriesBuffer.AsNativeArray(),
                Descriptors = descriptorList.AsArray(),
                Ranges = rangeList.AsArray(),
                Results = resultsArray,
                Filter = new AISensorCategoryFilter
                {
                    Masks = maskList.AsArray(),
                    VillagerLookup = _villagerLookup,
                    ResourceLookup = _resourceLookup,
                    StorehouseLookup = _storehouseLookup,
                    // MiracleLookup removed - Miracle category is game-specific (Godgame)
                    TransportLookup = _transportLookup
                }
            };

            state.Dependency = job.Schedule(descriptorList.Length, 1, state.Dependency);
            state.Dependency.Complete();

            var rangeArray = rangeList.AsArray();
            var maskArray = maskList.AsArray();
            var configArray = configList.AsArray();

            for (var i = 0; i < sensorList.Length; i++)
            {
                var entity = sensorList[i];
                var range = rangeArray[i];
                var sensorConfig = configArray[i];
                var mask = maskArray[i];

                var readings = state.EntityManager.GetBuffer<AISensorReading>(entity);
                readings.Clear();

                if (range.Count <= 0)
                {
                    continue;
                }

                var slice = new NativeSlice<KNearestResult>(resultsArray, range.Start, math.min(range.Count, range.Capacity));
                readings.ResizeUninitialized(slice.Length);

                for (var r = 0; r < slice.Length; r++)
                {
                    var nearest = slice[r];
                    var category = ResolveCategory(nearest.Entity, mask, _villagerLookup, _resourceLookup, _storehouseLookup, _transportLookup);
                    var normalized = ComputeSensorScore(nearest.DistanceSq, sensorConfig.Range);
                    var cellId = -1;
                    uint spatialVersion = 0;

                    if (_residencyLookup.HasComponent(nearest.Entity))
                    {
                        var residency = _residencyLookup[nearest.Entity];
                        cellId = residency.CellId;
                        spatialVersion = residency.Version;
                    }
                    else if (gridConfig.CellCount > 0 && gridConfig.CellSize > 0f && state.EntityManager.HasComponent<LocalTransform>(nearest.Entity))
                    {
                        var targetTransform = state.EntityManager.GetComponentData<LocalTransform>(nearest.Entity);
                        SpatialHash.Quantize(targetTransform.Position, gridConfig, out var coords);
                        var computedCell = SpatialHash.Flatten(in coords, in gridConfig);
                        if ((uint)computedCell < (uint)gridConfig.CellCount)
                        {
                            cellId = computedCell;
                            spatialVersion = gridState.Version;
                        }
                    }

                    readings[r] = new AISensorReading
                    {
                        Target = nearest.Entity,
                        DistanceSq = nearest.DistanceSq,
                        NormalizedScore = normalized,
                        CellId = cellId,
                        SpatialVersion = spatialVersion,
                        Category = category
                    };
                }
            }

            // Update counters
            counters.ValueRW.PerceptionChecksThisTick += processedCount;
            counters.ValueRW.TotalWarmOperationsThisTick += processedCount;
            
            descriptorList.Dispose();
            rangeList.Dispose();
            sensorList.Dispose();
            maskList.Dispose();
            configList.Dispose();
            resultsArray.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        private static float ComputeSensorScore(float distanceSq, float range)
        {
            var distance = math.sqrt(math.max(distanceSq, 1e-6f));
            var normalized = 1f - distance / math.max(range, 1e-3f);
            return math.saturate(normalized);
        }

        private static AISensorCategory ResolveCategory(
            Entity entity,
            in AISensorCategoryMask mask,
            ComponentLookup<VillagerId> villagerLookup,
            ComponentLookup<ResourceSourceConfig> resourceLookup,
            ComponentLookup<StorehouseConfig> storehouseLookup,
            ComponentLookup<TransportUnitTag> transportLookup
        )
        {
            if (entity == Entity.Null)
            {
                return AISensorCategory.None;
            }

            var categories = mask.Categories;
            if (categories.Length > 0)
            {
                for (var i = 0; i < categories.Length; i++)
                {
                    var category = categories[i];
                    if (MatchesCategory(entity, category, villagerLookup, resourceLookup, storehouseLookup, transportLookup))
                    {
                        return category;
                    }
                }
            }
            else
            {
                if (MatchesCategory(entity, AISensorCategory.Villager, villagerLookup, resourceLookup, storehouseLookup, transportLookup))
                {
                    return AISensorCategory.Villager;
                }

                if (MatchesCategory(entity, AISensorCategory.ResourceNode, villagerLookup, resourceLookup, storehouseLookup, transportLookup))
                {
                    return AISensorCategory.ResourceNode;
                }

                if (MatchesCategory(entity, AISensorCategory.Storehouse, villagerLookup, resourceLookup, storehouseLookup, transportLookup))
                {
                    return AISensorCategory.Storehouse;
                }

                // Miracle category is game-specific - skip in generic PureDOTS AI
                // Games can extend this system if they need miracle sensing

                if (MatchesCategory(entity, AISensorCategory.TransportUnit, villagerLookup, resourceLookup, storehouseLookup, transportLookup))
                {
                    return AISensorCategory.TransportUnit;
                }
            }

            return AISensorCategory.None;
        }

        private static bool MatchesCategory(
            Entity entity,
            AISensorCategory category,
            ComponentLookup<VillagerId> villagerLookup,
            ComponentLookup<ResourceSourceConfig> resourceLookup,
            ComponentLookup<StorehouseConfig> storehouseLookup,
            ComponentLookup<TransportUnitTag> transportLookup
        )
        {
            return category switch
            {
                AISensorCategory.Villager => villagerLookup.HasComponent(entity),
                AISensorCategory.ResourceNode => resourceLookup.HasComponent(entity),
                AISensorCategory.Storehouse => storehouseLookup.HasComponent(entity),
                AISensorCategory.Miracle => false, // Miracle category is game-specific - games can extend this if needed
                AISensorCategory.TransportUnit => transportLookup.HasComponent(entity),
                _ => true
            };
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(AISensorUpdateSystem))]
    public partial struct AIUtilityScoringSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            state.RequireForUpdate<AIBehaviourArchetype>();
            state.RequireForUpdate<AISensorConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            _transformLookup.Update(ref state);

            foreach (var (archetype, utilityState, sensorBuffer, transform, entity) in SystemAPI.Query<RefRO<AIBehaviourArchetype>, RefRW<AIUtilityState>, DynamicBuffer<AISensorReading>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                var blobRef = archetype.ValueRO.UtilityBlob;
                if (!blobRef.IsCreated)
                {
                    continue;
                }

                ref var blob = ref blobRef.Value;
                if (blob.Actions.Length == 0)
                {
                    continue;
                }

                var readings = sensorBuffer.AsNativeArray();
                var bestScore = float.MinValue;
                byte bestIndex = 0;

                var hasActionBuffer = SystemAPI.HasBuffer<AIActionState>(entity);
                DynamicBuffer<AIActionState> actionBuffer = default;
                if (hasActionBuffer)
                {
                    actionBuffer = SystemAPI.GetBuffer<AIActionState>(entity);
                    actionBuffer.Clear();
                    actionBuffer.ResizeUninitialized(blob.Actions.Length);
                }

                for (var actionIndex = 0; actionIndex < blob.Actions.Length; actionIndex++)
                {
                    ref var action = ref blob.Actions[actionIndex];
                    var score = 0f;

                    for (var factorIndex = 0; factorIndex < action.Factors.Length; factorIndex++)
                    {
                        ref var factor = ref action.Factors[factorIndex];
                        var sensorValue = factor.SensorIndex < readings.Length
                            ? readings[factor.SensorIndex].NormalizedScore
                            : 0f;
                        score += EvaluateCurve(sensorValue, in factor);
                    }

                    if (hasActionBuffer)
                    {
                        actionBuffer[actionIndex] = new AIActionState
                        {
                            Score = score
                        };
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIndex = (byte)actionIndex;
                    }
                }

                var stateRef = utilityState.ValueRW;
                stateRef.BestActionIndex = bestIndex;
                stateRef.BestScore = bestScore;
                stateRef.LastEvaluationTick = timeState.Tick;
                utilityState.ValueRW = stateRef;

                if (state.EntityManager.HasComponent<AITargetState>(entity))
                {
                    var targetState = state.EntityManager.GetComponentData<AITargetState>(entity);
                    targetState.ActionIndex = bestIndex;
                    targetState.Flags = 0;
                    targetState.TargetEntity = readings.Length > 0 ? readings[0].Target : Entity.Null;
                    targetState.TargetPosition = targetState.TargetEntity != Entity.Null && _transformLookup.HasComponent(targetState.TargetEntity)
                        ? _transformLookup[targetState.TargetEntity].Position
                        : transform.ValueRO.Position;

                    SystemAPI.SetComponent(entity, targetState);
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        private static float EvaluateCurve(float sensorValue, in AIUtilityCurveBlob curve)
        {
            var normalized = math.saturate(sensorValue / math.max(curve.MaxValue, 1e-3f));
            var delta = math.max(normalized - curve.Threshold, 0f);
            return math.pow(delta, math.max(curve.ResponsePower, 1f)) * curve.Weight;
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(AIUtilityScoringSystem))]
    public partial struct AISteeringSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);

            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<AISteeringConfig>();
            state.RequireForUpdate<AISteeringState>();
            state.RequireForUpdate<AITargetState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            _transformLookup.Update(ref state);

            foreach (var (config, steering, target, transform, entity) in SystemAPI.Query<RefRO<AISteeringConfig>, RefRW<AISteeringState>, RefRO<AITargetState>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                var steeringState = steering.ValueRW;
                var targetState = target.ValueRO;
                var steeringConfig = config.ValueRO;

                var targetPosition = targetState.TargetPosition;
                if (targetState.TargetEntity != Entity.Null && _transformLookup.HasComponent(targetState.TargetEntity))
                {
                    targetPosition = _transformLookup[targetState.TargetEntity].Position;
                }

                var direction = targetPosition - transform.ValueRO.Position;
                direction = ProjectDegreesOfFreedom(direction, steeringConfig.DegreesOfFreedom);

                var distance = math.length(direction);
                var desiredDirection = distance > 1e-4f
                    ? math.normalizesafe(direction)
                    : float3.zero;

                var responsiveness = math.saturate(steeringConfig.Responsiveness);
                steeringState.DesiredDirection = math.normalizesafe(math.lerp(steeringState.DesiredDirection, desiredDirection, responsiveness));

                var maxSpeed = math.max(0f, steeringConfig.MaxSpeed);
                var acceleration = math.max(0f, steeringConfig.Acceleration);
                var deltaTime = math.max(timeState.FixedDeltaTime, 1e-4f);
                var targetSpeed = math.min(maxSpeed, distance / deltaTime);
                var desiredVelocity = steeringState.DesiredDirection * targetSpeed;
                var lerpFactor = math.saturate(acceleration * deltaTime);

                steeringState.LinearVelocity = math.lerp(steeringState.LinearVelocity, desiredVelocity, lerpFactor);
                steeringState.LastSampledTarget = targetPosition;
                steeringState.LastUpdateTick = timeState.Tick;

                steering.ValueRW = steeringState;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        private static float3 ProjectDegreesOfFreedom(float3 vector, byte degreesOfFreedom)
        {
            if (degreesOfFreedom <= 2)
            {
                vector.y = 0f;
            }

            return vector;
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(AISteeringSystem))]
    public partial struct AITaskResolutionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AICommandQueueTag>();
            state.RequireForUpdate<AIUtilityState>();
            state.RequireForUpdate<AITargetState>();
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var queueEntity = SystemAPI.GetSingletonEntity<AICommandQueueTag>();
            var commands = state.EntityManager.GetBuffer<AICommand>(queueEntity);
            commands.Clear();

            foreach (var (utility, target, entity) in SystemAPI.Query<RefRO<AIUtilityState>, RefRO<AITargetState>>()
                         .WithEntityAccess())
            {
                var utilityState = utility.ValueRO;
                if (utilityState.LastEvaluationTick != timeState.Tick || utilityState.BestScore <= 0f)
                {
                    continue;
                }

                var targetState = target.ValueRO;
                commands.Add(new AICommand
                {
                    Agent = entity,
                    ActionIndex = utilityState.BestActionIndex,
                    TargetEntity = targetState.TargetEntity,
                    TargetPosition = targetState.TargetPosition
                });
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
