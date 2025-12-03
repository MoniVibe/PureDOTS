using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Perception;
using PureDOTS.Runtime.Spatial;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _signatureLookup = state.GetComponentLookup<SensorSignature>(true);
            _detectableLookup = state.GetComponentLookup<Detectable>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            _transformLookup.Update(ref state);
            _signatureLookup.Update(ref state);
            _detectableLookup.Update(ref state);

            // Collect all detectable entities (with SensorSignature or Detectable fallback)
            var detectableCount = 0;
            foreach (var _ in SystemAPI.Query<RefRO<SensorSignature>>())
            {
                detectableCount++;
            }
            foreach (var (_, entity) in SystemAPI.Query<RefRO<Detectable>>().WithEntityAccess())
            {
                // Only count if doesn't have SensorSignature (avoid double-counting)
                if (!_signatureLookup.HasComponent(entity))
                {
                    detectableCount++;
                }
            }

            var detectables = new NativeList<DetectableData>(detectableCount, Allocator.TempJob);

            // Collect entities with SensorSignature
            foreach (var (signature, transform, entity) in SystemAPI.Query<RefRO<SensorSignature>, RefRO<LocalTransform>>()
                .WithEntityAccess())
            {
                var threatLevel = (byte)0;
                var category = DetectableCategory.Neutral;

                // Try to get threat/category from Detectable if present
                if (_detectableLookup.HasComponent(entity))
                {
                    var detectable = _detectableLookup[entity];
                    threatLevel = detectable.ThreatLevel;
                    category = detectable.Category;
                }

                detectables.Add(new DetectableData
                {
                    Entity = entity,
                    Position = transform.ValueRO.Position,
                    Forward = math.forward(transform.ValueRO.Rotation),
                    Signature = signature.ValueRO,
                    ThreatLevel = threatLevel,
                    Category = category,
                    HasSignature = true
                });
            }

            // Collect entities with only Detectable (fallback)
            foreach (var (detectable, transform, entity) in SystemAPI.Query<RefRO<Detectable>, RefRO<LocalTransform>>()
                .WithEntityAccess())
            {
                if (!_signatureLookup.HasComponent(entity))
                {
                    detectables.Add(new DetectableData
                    {
                        Entity = entity,
                        Position = transform.ValueRO.Position,
                        Forward = math.forward(transform.ValueRO.Rotation),
                        Signature = SensorSignature.Default, // Use default signature
                        ThreatLevel = detectable.ValueRO.ThreatLevel,
                        Category = detectable.ValueRO.Category,
                        HasSignature = false
                    });
                }
            }

            // Update perception for entities with SenseCapability
            foreach (var (capability, perceptionState, transform, perceivedBuffer, entity) in
                SystemAPI.Query<RefRO<SenseCapability>, RefRW<PerceptionState>, RefRO<LocalTransform>, DynamicBuffer<PerceivedEntity>>()
                .WithEntityAccess())
            {
                // Check update interval
                var ticksSinceUpdate = timeState.Tick - perceptionState.ValueRO.LastUpdateTick;
                var secondsSinceUpdate = ticksSinceUpdate * timeState.FixedDeltaTime;

                if (secondsSinceUpdate < capability.ValueRO.UpdateInterval)
                {
                    continue;
                }

                // Clear old perceptions
                perceivedBuffer.Clear();

                var sensorPos = transform.ValueRO.Position;
                var sensorForward = math.forward(transform.ValueRO.Rotation);
                var rangeSq = capability.ValueRO.Range * capability.ValueRO.Range;
                var fovCos = math.cos(math.radians(capability.ValueRO.FieldOfView * 0.5f));

                var highestThreat = (byte)0;
                var highestThreatEntity = Entity.Null;
                var nearestEntity = Entity.Null;
                var nearestDistSq = float.MaxValue;
                var perceptionCount = 0;

                // Detect entities on enabled channels
                for (int i = 0; i < detectables.Length && perceptionCount < capability.ValueRO.MaxTrackedTargets; i++)
                {
                    var target = detectables[i];

                    // Skip self
                    if (target.Entity == entity)
                    {
                        continue;
                    }

                    var toTarget = target.Position - sensorPos;
                    var distSq = math.lengthsq(toTarget);

                    // Range check
                    if (distSq > rangeSq)
                    {
                        continue;
                    }

                    var distance = math.sqrt(distSq);
                    var direction = distance > 0.001f ? toTarget / distance : float3.zero;

                    // Determine which channels detected this entity
                    PerceptionChannel detectedChannels = PerceptionChannel.None;
                    float bestConfidence = 0f;

                    // Check each enabled channel
                    var enabledChannels = capability.ValueRO.EnabledChannels;
                    if ((enabledChannels & PerceptionChannel.Vision) != 0)
                    {
                        var confidence = EvaluateChannelDetection(
                            PerceptionChannel.Vision,
                            target,
                            sensorForward,
                            direction,
                            distance,
                            capability.ValueRO.Range,
                            fovCos,
                            capability.ValueRO.Acuity);
                        if (confidence > 0f)
                        {
                            detectedChannels |= PerceptionChannel.Vision;
                            bestConfidence = math.max(bestConfidence, confidence);
                        }
                    }

                    if ((enabledChannels & PerceptionChannel.Hearing) != 0)
                    {
                        var confidence = EvaluateChannelDetection(
                            PerceptionChannel.Hearing,
                            target,
                            sensorForward,
                            direction,
                            distance,
                            capability.ValueRO.Range,
                            fovCos,
                            capability.ValueRO.Acuity);
                        if (confidence > 0f)
                        {
                            detectedChannels |= PerceptionChannel.Hearing;
                            bestConfidence = math.max(bestConfidence, confidence);
                        }
                    }

                    if ((enabledChannels & PerceptionChannel.Smell) != 0)
                    {
                        var confidence = EvaluateChannelDetection(
                            PerceptionChannel.Smell,
                            target,
                            sensorForward,
                            direction,
                            distance,
                            capability.ValueRO.Range,
                            fovCos,
                            capability.ValueRO.Acuity);
                        if (confidence > 0f)
                        {
                            detectedChannels |= PerceptionChannel.Smell;
                            bestConfidence = math.max(bestConfidence, confidence);
                        }
                    }

                    if ((enabledChannels & PerceptionChannel.EM) != 0)
                    {
                        var confidence = EvaluateChannelDetection(
                            PerceptionChannel.EM,
                            target,
                            sensorForward,
                            direction,
                            distance,
                            capability.ValueRO.Range,
                            fovCos,
                            capability.ValueRO.Acuity);
                        if (confidence > 0f)
                        {
                            detectedChannels |= PerceptionChannel.EM;
                            bestConfidence = math.max(bestConfidence, confidence);
                        }
                    }

                    if ((enabledChannels & PerceptionChannel.Proximity) != 0)
                    {
                        // Proximity always works if in range
                        detectedChannels |= PerceptionChannel.Proximity;
                        bestConfidence = math.max(bestConfidence, 1f - distance / capability.ValueRO.Range);
                    }

                    // If detected on any channel, add to perception
                    if (detectedChannels != PerceptionChannel.None)
                    {
                        var relationship = (sbyte)(target.Category == DetectableCategory.Ally ? 127 :
                                                  target.Category == DetectableCategory.Enemy ? -128 : 0);

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
                            Relationship = relationship
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

            detectables.Dispose();
        }

        /// <summary>
        /// Evaluates detection on a specific channel (pluggable rule).
        /// Phase 1: Simple signature-based detection.
        /// Phase 2: Add LOS queries, occlusion, channel-specific behaviors.
        /// </summary>
        [BurstCompile]
        private static float EvaluateChannelDetection(
            PerceptionChannel channel,
            DetectableData target,
            float3 sensorForward,
            float3 direction,
            float distance,
            float maxRange,
            float fovCos,
            float acuity)
        {
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
            public bool HasSignature;
        }
    }
}

