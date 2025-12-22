using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Perception;
using PureDOTS.Runtime.Spatial;
using System.Runtime.InteropServices;
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
        private ComponentLookup<MediumContext> _mediumLookup;
        private BufferLookup<SenseOrganState> _organLookup;

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

            var useSignalField = SystemAPI.HasSingleton<SignalFieldState>();

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
                var medium = _mediumLookup.HasComponent(entity)
                    ? _mediumLookup[entity].Type
                    : MediumType.Gas;

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
                    HasSignature = true,
                    Medium = medium
                });
            }

            // Collect entities with only Detectable (fallback)
            foreach (var (detectable, transform, entity) in SystemAPI.Query<RefRO<Detectable>, RefRO<LocalTransform>>()
                .WithEntityAccess())
            {
                if (!_signatureLookup.HasComponent(entity))
                {
                    var medium = _mediumLookup.HasComponent(entity)
                        ? _mediumLookup[entity].Type
                        : MediumType.Gas;
                    detectables.Add(new DetectableData
                    {
                        Entity = entity,
                        Position = transform.ValueRO.Position,
                        Forward = math.forward(transform.ValueRO.Rotation),
                        Signature = SensorSignature.Default, // Use default signature
                        ThreatLevel = detectable.ValueRO.ThreatLevel,
                        Category = detectable.ValueRO.Category,
                        HasSignature = false,
                        Medium = medium
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
