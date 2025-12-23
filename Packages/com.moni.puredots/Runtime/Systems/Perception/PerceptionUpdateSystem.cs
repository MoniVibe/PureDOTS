using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Armies;
using PureDOTS.Runtime.Bands;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Deception;
using PureDOTS.Runtime.Perception;
using PureDOTS.Runtime.Performance;
using PureDOTS.Runtime.Spatial;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace PureDOTS.Systems.Perception
{
    /// <summary>
    /// Updates perception state using channel-based detection.
    /// Integrates with existing SensorUpdateSystem by sharing detection logic.
    /// Phase 1: Basic channel detection with pluggable rules.
    /// Phase 2: Advanced LOS queries, occlusion, channel-specific behaviors.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PerceptionSystemGroup))]
    public partial struct PerceptionUpdateSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<SensorSignature> _signatureLookup;
        private ComponentLookup<Detectable> _detectableLookup; // Fallback for entities without SensorSignature
        private ComponentLookup<MediumContext> _mediumLookup;
        private BufferLookup<SenseOrganState> _organLookup;
        private BufferLookup<PureDOTS.Runtime.Social.EntityRelation> _relationLookup;
        private ComponentLookup<FactionId> _factionLookup;
        private ComponentLookup<DisguiseIdentity> _disguiseLookup;
        private BufferLookup<DisguiseDiscovery> _discoveryLookup;
        private ComponentLookup<VillagerId> _villagerLookup;
        private ComponentLookup<VillageId> _villageLookup;
        private ComponentLookup<BandId> _bandLookup;
        private ComponentLookup<ArmyId> _armyLookup;
        private ComponentLookup<AIFidelityTier> _tierLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<SimulationFeatureFlags>();
            state.RequireForUpdate<SimulationScalars>();
            state.RequireForUpdate<SimulationOverrides>();

            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _signatureLookup = state.GetComponentLookup<SensorSignature>(true);
            _detectableLookup = state.GetComponentLookup<Detectable>(true);
            _mediumLookup = state.GetComponentLookup<MediumContext>(true);
            _organLookup = state.GetBufferLookup<SenseOrganState>(true);
            _relationLookup = state.GetBufferLookup<PureDOTS.Runtime.Social.EntityRelation>(true);
            _factionLookup = state.GetComponentLookup<FactionId>(true);
            _disguiseLookup = state.GetComponentLookup<DisguiseIdentity>(true);
            _discoveryLookup = state.GetBufferLookup<DisguiseDiscovery>(true);
            _villagerLookup = state.GetComponentLookup<VillagerId>(true);
            _villageLookup = state.GetComponentLookup<VillageId>(true);
            _bandLookup = state.GetComponentLookup<BandId>(true);
            _armyLookup = state.GetComponentLookup<ArmyId>(true);
            _tierLookup = state.GetComponentLookup<AIFidelityTier>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState))
            {
                return;
            }
            var features = SystemAPI.GetSingleton<SimulationFeatureFlags>();
            var scalars = SystemAPI.GetSingleton<SimulationScalars>();
            var overrides = SystemAPI.GetSingleton<SimulationOverrides>();

            // Check if perception is disabled
            if ((features.Flags & SimulationFeatureFlags.PerceptionEnabled) == 0)
            {
                return;
            }

            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Get effective perception range multiplier
            float perceptionRangeMult = overrides.OverridePerception
                ? overrides.PerceptionOverride
                : scalars.PerceptionRangeMult;

            _transformLookup.Update(ref state);
            _signatureLookup.Update(ref state);
            _detectableLookup.Update(ref state);
            _mediumLookup.Update(ref state);
            _organLookup.Update(ref state);
            _relationLookup.Update(ref state);
            _factionLookup.Update(ref state);
            _disguiseLookup.Update(ref state);
            _discoveryLookup.Update(ref state);
            _villagerLookup.Update(ref state);
            _villageLookup.Update(ref state);
            _bandLookup.Update(ref state);
            _armyLookup.Update(ref state);
            _tierLookup.Update(ref state);

            var useSignalField = SystemAPI.HasSingleton<SignalFieldState>();
            var hasGrid = SystemAPI.HasSingleton<SpatialGridConfig>() && SystemAPI.HasSingleton<SpatialGridState>();
            SpatialGridConfig gridConfig = default;
            DynamicBuffer<SpatialGridCellRange> gridRanges = default;
            DynamicBuffer<SpatialGridEntry> gridEntries = default;
            if (hasGrid)
            {
                gridConfig = SystemAPI.GetSingleton<SpatialGridConfig>();
                var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
                if (SystemAPI.HasBuffer<SpatialGridCellRange>(gridEntity) && SystemAPI.HasBuffer<SpatialGridEntry>(gridEntity))
                {
                    gridRanges = SystemAPI.GetBuffer<SpatialGridCellRange>(gridEntity);
                    gridEntries = SystemAPI.GetBuffer<SpatialGridEntry>(gridEntity);
                }
                else
                {
                    hasGrid = false;
                }
            }

            var relationshipCount = 0;
            foreach (var _ in SystemAPI.Query<RefRO<FactionRelationships>>())
            {
                relationshipCount++;
            }

            var factionRelationships = new NativeList<FactionRelationships>(relationshipCount, Allocator.Temp);
            foreach (var relationship in SystemAPI.Query<RefRO<FactionRelationships>>())
            {
                factionRelationships.Add(relationship.ValueRO);
            }

            var profile = SystemAPI.HasSingleton<TierProfileSettings>()
                ? SystemAPI.GetSingleton<TierProfileSettings>()
                : TierProfileSettings.CreateDefaults(TierProfileId.Mid);

            var hasBroker = SystemAPI.HasSingleton<AIBudgetBrokerState>() && SystemAPI.HasSingleton<UniversalPerformanceCounters>();
            RefRW<AIBudgetBrokerState> brokerRW = default;
            RefRW<UniversalPerformanceCounters> countersRW = default;
            UniversalPerformanceBudget perfBudget = default;
            if (hasBroker)
            {
                brokerRW = SystemAPI.GetSingletonRW<AIBudgetBrokerState>();
                countersRW = SystemAPI.GetSingletonRW<UniversalPerformanceCounters>();
                if (SystemAPI.HasSingleton<UniversalPerformanceBudget>())
                {
                    perfBudget = SystemAPI.GetSingleton<UniversalPerformanceBudget>();
                }
            }

            var hasPhysics = SystemAPI.HasSingleton<PhysicsWorldSingleton>();
            var collisionWorld = hasPhysics
                ? SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld
                : default;

            var candidateEntities = hasGrid ? new NativeList<Entity>(64, Allocator.Temp) : default;
            var rayFilter = CollisionFilter.Default;
            var remainingPerceptionChecks = hasBroker ? perfBudget.MaxPerceptionChecksPerTick : int.MaxValue;

            // Update perception for entities with SenseCapability (radius → FOV → (budgeted) LOS)
            foreach (var (capability, perceptionState, transform, perceivedBuffer, entity) in
                     SystemAPI.Query<RefRO<SenseCapability>, RefRW<PerceptionState>, RefRO<LocalTransform>, DynamicBuffer<PerceivedEntity>>()
                         .WithEntityAccess())
            {
                if (remainingPerceptionChecks <= 0)
                {
                    if (hasBroker)
                    {
                        countersRW.ValueRW.TotalOperationsDroppedThisTick++;
                    }
                    break;
                }

                var tier = _tierLookup.HasComponent(entity) ? _tierLookup[entity].Tier : AILODTier.Tier1_Reduced;
                if (tier == AILODTier.Tier3_Aggregate)
                {
                    continue;
                }

                var tierCadenceTicks = tier switch
                {
                    AILODTier.Tier0_Full => (uint)math.max(1, profile.Tier0SensorCadenceTicks),
                    AILODTier.Tier1_Reduced => (uint)math.max(1, profile.Tier1SensorCadenceTicks),
                    AILODTier.Tier2_EventDriven => (uint)math.max(1, profile.Tier2SensorCadenceTicks),
                    _ => 1u
                };

                // Check update interval
                var ticksSinceUpdate = timeState.Tick - perceptionState.ValueRO.LastUpdateTick;
                var secondsSinceUpdate = ticksSinceUpdate * timeState.FixedDeltaTime;

                if (ticksSinceUpdate < tierCadenceTicks || secondsSinceUpdate < capability.ValueRO.UpdateInterval)
                {
                    continue;
                }

                // Clear old perceptions
                perceivedBuffer.Clear();

                var sensorPos = transform.ValueRO.Position;
                var sensorForward = math.forward(transform.ValueRO.Rotation);
                // Apply perception range multiplier
                var baseRange = capability.ValueRO.Range * perceptionRangeMult;
                var fovCos = math.cos(math.radians(capability.ValueRO.FieldOfView * 0.5f));

                var highestThreat = (byte)0;
                var highestThreatEntity = Entity.Null;
                var nearestEntity = Entity.Null;
                var nearestDistSq = float.MaxValue;
                var perceptionCount = 0;

                var sensorMedium = _mediumLookup.HasComponent(entity)
                    ? _mediumLookup[entity].Type
                    : MediumType.Gas;
                var enabledChannels = MediumUtilities.FilterChannels(sensorMedium, capability.ValueRO.EnabledChannels);
                var hasOrgans = _organLookup.HasBuffer(entity);
                var organBuffer = hasOrgans ? _organLookup[entity] : default;
                var maxRangeMult = hasOrgans ? PerceptionOrganUtilities.GetMaxRangeMultiplier(enabledChannels, organBuffer) : 1f;
                var maxRange = baseRange * maxRangeMult;
                var rangeSq = maxRange * maxRange;

                var maxTracked = capability.ValueRO.MaxTrackedTargets;
                if (tier == AILODTier.Tier1_Reduced)
                {
                    maxTracked = (byte)math.max(1, maxTracked / 2);
                }
                else if (tier == AILODTier.Tier2_EventDriven)
                {
                    maxTracked = (byte)math.max(1, maxTracked / 4);
                }

                if (!hasGrid)
                {
                    // Fallback: no spatial grid available, keep Phase-1 global scan disabled (avoid N^2 surprises).
                    perceptionState.ValueRW.LastUpdateTick = timeState.Tick;
                    perceptionState.ValueRW.PerceivedCount = 0;
                    perceptionState.ValueRW.HighestThreat = 0;
                    perceptionState.ValueRW.HighestThreatEntity = Entity.Null;
                    perceptionState.ValueRW.NearestEntity = Entity.Null;
                    perceptionState.ValueRW.NearestDistance = 0f;
                    continue;
                }

                candidateEntities.Clear();
                var queryPos = sensorPos;
                SpatialQueryHelper.GetEntitiesWithinRadius(
                    ref queryPos,
                    maxRange,
                    gridConfig,
                    gridRanges,
                    gridEntries,
                    ref candidateEntities);

                // Detect entities on enabled channels
                for (int i = 0; i < candidateEntities.Length && perceptionCount < maxTracked; i++)
                {
                    if (remainingPerceptionChecks <= 0)
                    {
                        if (hasBroker)
                        {
                            countersRW.ValueRW.TotalOperationsDroppedThisTick++;
                        }
                        break;
                    }

                    var targetEntity = candidateEntities[i];

                    // Skip self
                    if (targetEntity == entity)
                    {
                        continue;
                    }

                    if (!_transformLookup.HasComponent(targetEntity))
                    {
                        continue;
                    }

                    var targetTransform = _transformLookup[targetEntity];
                    var targetPos = targetTransform.Position;
                    var toTarget = targetPos - sensorPos;
                    var distSq = math.lengthsq(toTarget);

                    // Range check
                    if (distSq > rangeSq)
                    {
                        continue;
                    }

                    var distance = math.sqrt(distSq);
                    var direction = distance > 0.001f ? toTarget / distance : float3.zero;

                    // Count this as a perception check once it makes it past radius (and before channel tests).
                    if (hasBroker)
                    {
                        remainingPerceptionChecks--;
                        countersRW.ValueRW.PerceptionChecksThisTick++;
                        countersRW.ValueRW.TotalWarmOperationsThisTick++;
                    }

                    var targetMedium = _mediumLookup.HasComponent(targetEntity)
                        ? _mediumLookup[targetEntity].Type
                        : MediumType.Gas;

                    var targetSignature = _signatureLookup.HasComponent(targetEntity)
                        ? _signatureLookup[targetEntity]
                        : SensorSignature.Default;

                    var targetThreatLevel = (byte)0;
                    var targetCategory = DetectableCategory.Neutral;
                    if (_detectableLookup.HasComponent(targetEntity))
                    {
                        var detectable = _detectableLookup[targetEntity];
                        targetThreatLevel = detectable.ThreatLevel;
                        targetCategory = detectable.Category;
                    }

                    var target = new DetectableData
                    {
                        Entity = targetEntity,
                        Position = targetPos,
                        Forward = math.forward(targetTransform.Rotation),
                        Signature = targetSignature,
                        ThreatLevel = targetThreatLevel,
                        Category = targetCategory,
                        HasSignature = _signatureLookup.HasComponent(targetEntity),
                        Medium = targetMedium
                    };

                    // Determine which channels detected this entity
                    PerceptionChannel detectedChannels = PerceptionChannel.None;
                    float bestConfidence = 0f;

                    // Check each enabled channel
                    if ((enabledChannels & PerceptionChannel.Vision) != 0)
                    {
                        float channelRange = baseRange;
                        float channelAcuity = capability.ValueRO.Acuity;
                        float channelNoiseFloor = 0f;
                        if (hasOrgans)
                        {
                            PerceptionOrganUtilities.GetChannelModifiers(
                                PerceptionChannel.Vision,
                                organBuffer,
                                out var rangeMult,
                                out var acuityMult,
                                out var noiseFloor);
                            channelRange *= rangeMult;
                            channelAcuity *= acuityMult;
                            channelNoiseFloor = noiseFloor;
                        }

                        var confidence = EvaluateChannelDetection(
                            PerceptionChannel.Vision,
                            target,
                            sensorForward,
                            direction,
                            distance,
                            channelRange,
                            fovCos,
                            channelAcuity,
                            channelNoiseFloor,
                            sensorMedium,
                            target.Medium);

                        if (confidence > 0f && hasPhysics)
                        {
                            // Budgeted LOS refinement (Radius → FOV → LOS).
                            if (hasBroker)
                            {
                                countersRW.ValueRW.LosRaysAttemptedThisTick++;
                                if (brokerRW.ValueRO.RemainingLosRays > 0)
                                {
                                    brokerRW.ValueRW.RemainingLosRays--;
                                    countersRW.ValueRW.LosRaysGrantedThisTick++;

                                    var input = new RaycastInput
                                    {
                                        Start = sensorPos,
                                        End = target.Position,
                                        Filter = rayFilter
                                    };

                                    if (collisionWorld.CastRay(input, out var hit) && hit.Entity != target.Entity)
                                    {
                                        confidence = 0f;
                                    }
                                }
                                else
                                {
                                    brokerRW.ValueRW.DeferredLosRays++;
                                    countersRW.ValueRW.LosRaysDeferredThisTick++;
                                    // Soft-cap degrade: keep a weak visual belief but reduce confidence.
                                    confidence *= 0.5f;
                                }
                            }
                        }

                        if (confidence > 0f)
                        {
                            detectedChannels |= PerceptionChannel.Vision;
                            bestConfidence = math.max(bestConfidence, confidence);
                        }
                    }

                    if (!useSignalField && (enabledChannels & PerceptionChannel.Hearing) != 0)
                    {
                        float channelRange = baseRange;
                        float channelAcuity = capability.ValueRO.Acuity;
                        float channelNoiseFloor = 0f;
                        if (hasOrgans)
                        {
                            PerceptionOrganUtilities.GetChannelModifiers(
                                PerceptionChannel.Hearing,
                                organBuffer,
                                out var rangeMult,
                                out var acuityMult,
                                out var noiseFloor);
                            channelRange *= rangeMult;
                            channelAcuity *= acuityMult;
                            channelNoiseFloor = noiseFloor;
                        }

                        var confidence = EvaluateChannelDetection(
                            PerceptionChannel.Hearing,
                            target,
                            sensorForward,
                            direction,
                            distance,
                            channelRange,
                            fovCos,
                            channelAcuity,
                            channelNoiseFloor,
                            sensorMedium,
                            target.Medium);
                        if (confidence > 0f)
                        {
                            detectedChannels |= PerceptionChannel.Hearing;
                            bestConfidence = math.max(bestConfidence, confidence);
                        }
                    }

                    if (!useSignalField && (enabledChannels & PerceptionChannel.Smell) != 0)
                    {
                        float channelRange = baseRange;
                        float channelAcuity = capability.ValueRO.Acuity;
                        float channelNoiseFloor = 0f;
                        if (hasOrgans)
                        {
                            PerceptionOrganUtilities.GetChannelModifiers(
                                PerceptionChannel.Smell,
                                organBuffer,
                                out var rangeMult,
                                out var acuityMult,
                                out var noiseFloor);
                            channelRange *= rangeMult;
                            channelAcuity *= acuityMult;
                            channelNoiseFloor = noiseFloor;
                        }

                        var confidence = EvaluateChannelDetection(
                            PerceptionChannel.Smell,
                            target,
                            sensorForward,
                            direction,
                            distance,
                            channelRange,
                            fovCos,
                            channelAcuity,
                            channelNoiseFloor,
                            sensorMedium,
                            target.Medium);
                        if (confidence > 0f)
                        {
                            detectedChannels |= PerceptionChannel.Smell;
                            bestConfidence = math.max(bestConfidence, confidence);
                        }
                    }

                    if ((enabledChannels & PerceptionChannel.EM) != 0)
                    {
                        float channelRange = baseRange;
                        float channelAcuity = capability.ValueRO.Acuity;
                        float channelNoiseFloor = 0f;
                        if (hasOrgans)
                        {
                            PerceptionOrganUtilities.GetChannelModifiers(
                                PerceptionChannel.EM,
                                organBuffer,
                                out var rangeMult,
                                out var acuityMult,
                                out var noiseFloor);
                            channelRange *= rangeMult;
                            channelAcuity *= acuityMult;
                            channelNoiseFloor = noiseFloor;
                        }

                        var confidence = EvaluateChannelDetection(
                            PerceptionChannel.EM,
                            target,
                            sensorForward,
                            direction,
                            distance,
                            channelRange,
                            fovCos,
                            channelAcuity,
                            channelNoiseFloor,
                            sensorMedium,
                            target.Medium);

                        if (confidence > 0f && hasPhysics)
                        {
                            if (hasBroker)
                            {
                                countersRW.ValueRW.LosRaysAttemptedThisTick++;
                                if (brokerRW.ValueRO.RemainingLosRays > 0)
                                {
                                    brokerRW.ValueRW.RemainingLosRays--;
                                    countersRW.ValueRW.LosRaysGrantedThisTick++;

                                    var input = new RaycastInput
                                    {
                                        Start = sensorPos,
                                        End = target.Position,
                                        Filter = rayFilter
                                    };

                                    if (collisionWorld.CastRay(input, out var hit) && hit.Entity != target.Entity)
                                    {
                                        confidence = 0f;
                                    }
                                }
                                else
                                {
                                    brokerRW.ValueRW.DeferredLosRays++;
                                    countersRW.ValueRW.LosRaysDeferredThisTick++;
                                    confidence *= 0.5f;
                                }
                            }
                        }

                        if (confidence > 0f)
                        {
                            detectedChannels |= PerceptionChannel.EM;
                            bestConfidence = math.max(bestConfidence, confidence);
                        }
                    }

                    if ((enabledChannels & PerceptionChannel.Proximity) != 0)
                    {
                        float channelRange = baseRange;
                        float channelAcuity = capability.ValueRO.Acuity;
                        float channelNoiseFloor = 0f;
                        if (hasOrgans)
                        {
                            PerceptionOrganUtilities.GetChannelModifiers(
                                PerceptionChannel.Proximity,
                                organBuffer,
                                out var rangeMult,
                                out var acuityMult,
                                out var noiseFloor);
                            channelRange *= rangeMult;
                            channelAcuity *= acuityMult;
                            channelNoiseFloor = noiseFloor;
                        }

                        if (distance <= channelRange)
                        {
                            var confidence = math.saturate((1f - distance / channelRange) * channelAcuity - channelNoiseFloor);
                            if (confidence > 0f)
                            {
                                detectedChannels |= PerceptionChannel.Proximity;
                                bestConfidence = math.max(bestConfidence, confidence);
                            }
                        }
                    }

                    // If detected on any channel, add to perception
                    if (detectedChannels != PerceptionChannel.None)
                    {
                        var relation = PerceptionRelationResolver.Resolve(
                            entity,
                            target.Entity,
                            target.Category,
                            _relationLookup,
                            _factionLookup,
                            _disguiseLookup,
                            _discoveryLookup,
                            _villagerLookup,
                            _villageLookup,
                            _bandLookup,
                            _armyLookup,
                            factionRelationships.AsArray());

                        perceivedBuffer.Add(new PerceivedEntity
                        {
                            TargetEntity = target.Entity,
                            DetectedChannels = detectedChannels,
                            Confidence = math.saturate(bestConfidence),
                            Distance = distance,
                            Direction = direction,
                            FirstDetectedTick = timeState.Tick, // TODO: Track persistent first detection
                            LastSeenTick = timeState.Tick,
                            ThreatLevel = target.ThreatLevel,
                            Relationship = relation.Score,
                            RelationKind = relation.Kind,
                            RelationFlags = relation.Flags
                        });

                        perceptionCount++;

                        // Track highest threat
                        if (target.ThreatLevel > highestThreat)
                        {
                            highestThreat = target.ThreatLevel;
                            highestThreatEntity = target.Entity;
                        }

                        // Track nearest
                        if (distSq < nearestDistSq)
                        {
                            nearestDistSq = distSq;
                            nearestEntity = target.Entity;
                        }
                    }
                }

                // Update perception state
                perceptionState.ValueRW.LastUpdateTick = timeState.Tick;
                perceptionState.ValueRW.PerceivedCount = (byte)perceptionCount;
                perceptionState.ValueRW.HighestThreat = highestThreat;
                perceptionState.ValueRW.HighestThreatEntity = highestThreatEntity;
                perceptionState.ValueRW.NearestEntity = nearestEntity;
                perceptionState.ValueRW.NearestDistance = math.sqrt(nearestDistSq);
            }

            if (candidateEntities.IsCreated)
            {
                candidateEntities.Dispose();
            }
            factionRelationships.Dispose();
        }

        /// <summary>
        /// Evaluates detection on a specific channel (pluggable rule).
        /// Phase 1: Simple signature-based detection.
        /// Phase 2: Add LOS queries, occlusion, channel-specific behaviors.
        /// </summary>
        [BurstCompile]
        private static float EvaluateChannelDetection(
            PerceptionChannel channel,
            in DetectableData target,
            in float3 sensorForward,
            in float3 direction,
            float distance,
            float maxRange,
            float fovCos,
            float acuity,
            float noiseFloor,
            MediumType sensorMedium,
            MediumType targetMedium)
        {
            if (!MediumUtilities.SupportsChannel(sensorMedium, channel) ||
                !MediumUtilities.SupportsChannel(targetMedium, channel))
            {
                return 0f;
            }

            if (maxRange <= 0f || distance > maxRange)
            {
                return 0f;
            }

            // Get signature for this channel
            float signature = target.Signature.GetSignature(channel);

            if (signature <= 0f)
            {
                return 0f; // Not detectable on this channel
            }

            // Channel-specific rules (pluggable, not hardcoded switch)
            float confidence = signature;

            // LOS check for vision/EM channels
            if (channel == PerceptionChannel.Vision || channel == PerceptionChannel.EM)
            {
                var dot = math.dot(sensorForward, direction);
                if (dot < fovCos)
                {
                    return 0f; // Outside FOV
                }
                // TODO Phase 2: Actual LOS raycast query
            }

            // Range decay (simple linear for Phase 1)
            confidence *= (1f - distance / maxRange);

            // Apply acuity modifier
            confidence *= acuity;

            confidence -= noiseFloor;

            return math.saturate(confidence);
        }

        private struct DetectableData
        {
            public Entity Entity;
            public float3 Position;
            public float3 Forward;
            public SensorSignature Signature;
            public byte ThreatLevel;
            public DetectableCategory Category;
            [MarshalAs(UnmanagedType.U1)]
            public bool HasSignature;
            public MediumType Medium;
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(PerceptionSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(PerceptionUpdateSystem))]
    public partial struct PerceptionSignalFieldUpdateSystem : ISystem
    {
        private ComponentLookup<MediumContext> _mediumLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<SimulationFeatureFlags>();
            state.RequireForUpdate<SpatialGridConfig>();

            _mediumLookup = state.GetComponentLookup<MediumContext>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var features = SystemAPI.GetSingleton<SimulationFeatureFlags>();
            if ((features.Flags & SimulationFeatureFlags.PerceptionEnabled) == 0)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<SpatialGridConfig>(out var gridConfig))
            {
                return;
            }

            var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
            if (!SystemAPI.HasBuffer<SignalFieldCell>(gridEntity))
            {
                return;
            }

            var signalConfig = SystemAPI.HasComponent<SignalFieldConfig>(gridEntity)
                ? SystemAPI.GetComponentRO<SignalFieldConfig>(gridEntity).ValueRO
                : SignalFieldConfig.Default;

            var smellDecayPerTick = math.clamp(1f - signalConfig.SmellDecayPerSecond * timeState.FixedDeltaTime, 0f, 1f);
            var soundDecayPerTick = math.clamp(1f - signalConfig.SoundDecayPerSecond * timeState.FixedDeltaTime, 0f, 1f);
            var emDecayPerTick = math.clamp(1f - signalConfig.EMDecayPerSecond * timeState.FixedDeltaTime, 0f, 1f);
            var emissionScale = math.max(0f, signalConfig.EmissionScale);
            var maxStrength = math.max(0f, signalConfig.MaxStrength);

            _mediumLookup.Update(ref state);

            var cells = SystemAPI.GetBuffer<SignalFieldCell>(gridEntity);
            var updated = false;

            foreach (var (emitter, transform, entity) in SystemAPI.Query<RefRO<SensorySignalEmitter>, RefRO<LocalTransform>>()
                .WithEntityAccess())
            {
                if (emitter.ValueRO.IsActive == 0)
                {
                    continue;
                }

                var emitterChannels = emitter.ValueRO.Channels;
                if (emitterChannels == PerceptionChannel.None)
                {
                    continue;
                }

                var medium = _mediumLookup.HasComponent(entity)
                    ? _mediumLookup[entity].Type
                    : MediumType.Gas;
                var channels = MediumUtilities.FilterChannels(medium, emitterChannels);
                if (channels == PerceptionChannel.None)
                {
                    continue;
                }

                SpatialHash.Quantize(transform.ValueRO.Position, gridConfig, out var cellCoords);
                var cellId = SpatialHash.Flatten(in cellCoords, in gridConfig);
                if ((uint)cellId >= (uint)gridConfig.CellCount)
                {
                    continue;
                }

                var cell = cells[cellId];
                ApplyDecay(ref cell, timeState.Tick, smellDecayPerTick, soundDecayPerTick, emDecayPerTick);

                if ((channels & PerceptionChannel.Smell) != 0)
                {
                    cell.Smell = math.min(maxStrength, cell.Smell + emitter.ValueRO.SmellStrength * emissionScale);
                }

                if ((channels & PerceptionChannel.Hearing) != 0)
                {
                    cell.Sound = math.min(maxStrength, cell.Sound + emitter.ValueRO.SoundStrength * emissionScale);
                }

                if ((channels & PerceptionChannel.EM) != 0)
                {
                    cell.EM = math.min(maxStrength, cell.EM + emitter.ValueRO.EMStrength * emissionScale);
                }

                cell.LastUpdatedTick = timeState.Tick;
                cells[cellId] = cell;
                updated = true;
            }

            if (updated && SystemAPI.HasComponent<SignalFieldState>(gridEntity))
            {
                var stateRW = SystemAPI.GetComponentRW<SignalFieldState>(gridEntity);
                stateRW.ValueRW.LastUpdateTick = timeState.Tick;
                stateRW.ValueRW.Version++;
            }
        }

        [BurstCompile]
        private static void ApplyDecay(
            ref SignalFieldCell cell,
            uint currentTick,
            float smellDecayPerTick,
            float soundDecayPerTick,
            float emDecayPerTick)
        {
            if (cell.LastUpdatedTick == currentTick)
            {
                return;
            }

            var ticks = currentTick - cell.LastUpdatedTick;
            if (ticks == 0)
            {
                return;
            }

            var ticksF = (float)ticks;
            cell.Smell *= math.pow(smellDecayPerTick, ticksF);
            cell.Sound *= math.pow(soundDecayPerTick, ticksF);
            cell.EM *= math.pow(emDecayPerTick, ticksF);
            cell.LastUpdatedTick = currentTick;
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(PerceptionSystemGroup))]
    [UpdateAfter(typeof(PerceptionSignalFieldUpdateSystem))]
    [UpdateBefore(typeof(PerceptionToInterruptBridgeSystem))]
    public partial struct PerceptionSignalSamplingSystem : ISystem
    {
        private ComponentLookup<MediumContext> _mediumLookup;
        private BufferLookup<SenseOrganState> _organLookup;
        private BufferLookup<SignalFieldCell> _signalFieldLookup;
        private ComponentLookup<SensorySignalEmitter> _emitterLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<SimulationFeatureFlags>();
            state.RequireForUpdate<SpatialGridConfig>();

            _mediumLookup = state.GetComponentLookup<MediumContext>(true);
            _organLookup = state.GetBufferLookup<SenseOrganState>(true);
            _signalFieldLookup = state.GetBufferLookup<SignalFieldCell>(true);
            _emitterLookup = state.GetComponentLookup<SensorySignalEmitter>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var features = SystemAPI.GetSingleton<SimulationFeatureFlags>();
            if ((features.Flags & SimulationFeatureFlags.PerceptionEnabled) == 0)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<SpatialGridConfig>(out var gridConfig))
            {
                return;
            }

            var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
            _signalFieldLookup.Update(ref state);
            if (!_signalFieldLookup.HasBuffer(gridEntity))
            {
                return;
            }

            var signalConfig = SystemAPI.HasComponent<SignalFieldConfig>(gridEntity)
                ? SystemAPI.GetComponentRO<SignalFieldConfig>(gridEntity).ValueRO
                : SignalFieldConfig.Default;

            var smellDecayPerTick = math.clamp(1f - signalConfig.SmellDecayPerSecond * timeState.FixedDeltaTime, 0f, 1f);
            var soundDecayPerTick = math.clamp(1f - signalConfig.SoundDecayPerSecond * timeState.FixedDeltaTime, 0f, 1f);
            var emDecayPerTick = math.clamp(1f - signalConfig.EMDecayPerSecond * timeState.FixedDeltaTime, 0f, 1f);

            _mediumLookup.Update(ref state);
            _organLookup.Update(ref state);
            _emitterLookup.Update(ref state);

            var cells = _signalFieldLookup[gridEntity];

            foreach (var (capability, signalState, transform, entity) in
                SystemAPI.Query<RefRO<SenseCapability>, RefRW<SignalPerceptionState>, RefRO<LocalTransform>>()
                    .WithEntityAccess())
            {
                var ticksSinceUpdate = timeState.Tick - signalState.ValueRO.LastUpdateTick;
                var secondsSinceUpdate = ticksSinceUpdate * timeState.FixedDeltaTime;

                if (secondsSinceUpdate < capability.ValueRO.UpdateInterval)
                {
                    continue;
                }

                var sensorMedium = _mediumLookup.HasComponent(entity)
                    ? _mediumLookup[entity].Type
                    : MediumType.Gas;
                var enabledChannels = MediumUtilities.FilterChannels(sensorMedium, capability.ValueRO.EnabledChannels);

                float smellLevel = 0f;
                float soundLevel = 0f;
                float emLevel = 0f;

                if ((enabledChannels & (PerceptionChannel.Smell | PerceptionChannel.Hearing | PerceptionChannel.EM)) != 0)
                {
                    SpatialHash.Quantize(transform.ValueRO.Position, gridConfig, out var cellCoords);
                    var cellId = SpatialHash.Flatten(in cellCoords, in gridConfig);
                    if ((uint)cellId < (uint)gridConfig.CellCount)
                    {
                        var cell = cells[cellId];
                        SampleDecayed(cell, timeState.Tick, smellDecayPerTick, soundDecayPerTick, emDecayPerTick, out smellLevel, out soundLevel, out emLevel);
                    }
                }

                if (_emitterLookup.HasComponent(entity))
                {
                    var emitter = _emitterLookup[entity];
                    if (emitter.IsActive != 0)
                    {
                        if ((emitter.Channels & PerceptionChannel.Smell) != 0)
                        {
                            smellLevel = math.max(0f, smellLevel - emitter.SmellStrength * signalConfig.EmissionScale);
                        }

                        if ((emitter.Channels & PerceptionChannel.Hearing) != 0)
                        {
                            soundLevel = math.max(0f, soundLevel - emitter.SoundStrength * signalConfig.EmissionScale);
                        }

                        if ((emitter.Channels & PerceptionChannel.EM) != 0)
                        {
                            emLevel = math.max(0f, emLevel - emitter.EMStrength * signalConfig.EmissionScale);
                        }
                    }
                }

                var hasOrgans = _organLookup.HasBuffer(entity);
                var organBuffer = hasOrgans ? _organLookup[entity] : default;
                var baseAcuity = capability.ValueRO.Acuity;

                float smellConfidence = 0f;
                if ((enabledChannels & PerceptionChannel.Smell) != 0)
                {
                    var channelAcuity = baseAcuity;
                    var noiseFloor = 0f;
                    if (hasOrgans)
                    {
                        PerceptionOrganUtilities.GetChannelModifiers(
                            PerceptionChannel.Smell,
                            organBuffer,
                            out _,
                            out var acuityMult,
                            out var organNoise);
                        channelAcuity *= acuityMult;
                        noiseFloor = organNoise;
                    }

                    smellConfidence = math.saturate(smellLevel * channelAcuity - noiseFloor);
                }
                else
                {
                    smellLevel = 0f;
                }

                float soundConfidence = 0f;
                if ((enabledChannels & PerceptionChannel.Hearing) != 0)
                {
                    var channelAcuity = baseAcuity;
                    var noiseFloor = 0f;
                    if (hasOrgans)
                    {
                        PerceptionOrganUtilities.GetChannelModifiers(
                            PerceptionChannel.Hearing,
                            organBuffer,
                            out _,
                            out var acuityMult,
                            out var organNoise);
                        channelAcuity *= acuityMult;
                        noiseFloor = organNoise;
                    }

                    soundConfidence = math.saturate(soundLevel * channelAcuity - noiseFloor);
                }
                else
                {
                    soundLevel = 0f;
                }

                float emConfidence = 0f;
                if ((enabledChannels & PerceptionChannel.EM) != 0)
                {
                    var channelAcuity = baseAcuity;
                    var noiseFloor = 0f;
                    if (hasOrgans)
                    {
                        PerceptionOrganUtilities.GetChannelModifiers(
                            PerceptionChannel.EM,
                            organBuffer,
                            out _,
                            out var acuityMult,
                            out var organNoise);
                        channelAcuity *= acuityMult;
                        noiseFloor = organNoise;
                    }

                    emConfidence = math.saturate(emLevel * channelAcuity - noiseFloor);
                }
                else
                {
                    emLevel = 0f;
                }

                signalState.ValueRW.SmellLevel = smellLevel;
                signalState.ValueRW.SmellConfidence = smellConfidence;
                signalState.ValueRW.SoundLevel = soundLevel;
                signalState.ValueRW.SoundConfidence = soundConfidence;
                signalState.ValueRW.EMLevel = emLevel;
                signalState.ValueRW.EMConfidence = emConfidence;
                signalState.ValueRW.LastUpdateTick = timeState.Tick;
            }
        }

        [BurstCompile]
        private static void SampleDecayed(
            in SignalFieldCell cell,
            uint currentTick,
            float smellDecayPerTick,
            float soundDecayPerTick,
            float emDecayPerTick,
            out float smell,
            out float sound,
            out float em)
        {
            var ticks = currentTick - cell.LastUpdatedTick;
            if (ticks == 0)
            {
                smell = cell.Smell;
                sound = cell.Sound;
                em = cell.EM;
                return;
            }

            var ticksF = (float)ticks;
            smell = cell.Smell * math.pow(smellDecayPerTick, ticksF);
            sound = cell.Sound * math.pow(soundDecayPerTick, ticksF);
            em = cell.EM * math.pow(emDecayPerTick, ticksF);
        }
    }
}
