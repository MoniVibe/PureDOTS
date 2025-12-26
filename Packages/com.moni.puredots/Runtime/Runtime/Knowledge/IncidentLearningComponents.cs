using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Knowledge
{
    public enum IncidentLearningKind : byte
    {
        Unknown = 0,
        Hit = 1,
        NearMiss = 2,
        Observation = 3,
        Failure = 4
    }

    /// <summary>
    /// Global tuning for incident-driven learning.
    /// </summary>
    public struct IncidentLearningConfig : IComponentData
    {
        public int MaxEntries;
        public float MemoryGainOnHit;
        public float MemoryGainOnNearMiss;
        public float MemoryGainOnObservation;
        public float MemoryGainDefault;
        public float MemoryDecayPerSecond;
        public float IncidentCooldownSeconds;
        public float MinSeverity;
        public float MinBias;
        public float MaxBias;

        public static IncidentLearningConfig Default => new IncidentLearningConfig
        {
            MaxEntries = 4,
            MemoryGainOnHit = 0.35f,
            MemoryGainOnNearMiss = 0.15f,
            MemoryGainOnObservation = 0.05f,
            MemoryGainDefault = 0.1f,
            MemoryDecayPerSecond = 0.003f,
            IncidentCooldownSeconds = 1.5f,
            MinSeverity = 0.01f,
            MinBias = 0f,
            MaxBias = 1f
        };
    }

    /// <summary>
    /// Tag enabling incident learning on an entity.
    /// </summary>
    public struct IncidentLearningAgent : IComponentData
    {
    }

    /// <summary>
    /// Per-category learning memory for an entity.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct IncidentLearningMemory : IBufferElementData
    {
        public FixedString64Bytes CategoryId;
        public float Bias;
        public float RecentSeverity;
        public uint LastIncidentTick;
        public uint NextIncidentAllowedTick;
        public uint LastUpdateTick;
        public ushort IncidentCount;
        public ushort NearMissCount;
    }

    /// <summary>
    /// Singleton tag for the incident learning event buffer.
    /// </summary>
    public struct IncidentLearningEventBuffer : IComponentData
    {
    }

    /// <summary>
    /// Incident event routed to learning agents.
    /// </summary>
    [InternalBufferCapacity(32)]
    public struct IncidentLearningEvent : IBufferElementData
    {
        public Entity Target;
        public Entity Source;
        public float3 Position;
        public FixedString64Bytes CategoryId;
        public float Severity;
        public IncidentLearningKind Kind;
        public uint Tick;
    }

    /// <summary>
    /// Sample incident categories for early integrations.
    /// </summary>
    public static class IncidentLearningCategories
    {
        public static readonly FixedString64Bytes TreeFall = new FixedString64Bytes("tree_fall");
        public static readonly FixedString64Bytes TreeFallNearMiss = new FixedString64Bytes("tree_fall_near_miss");
        public static readonly FixedString64Bytes FallingDebris = new FixedString64Bytes("falling_debris");
        public static readonly FixedString64Bytes ConstructionIncident = new FixedString64Bytes("construction_incident");
        public static readonly FixedString64Bytes ConstructionCollapse = new FixedString64Bytes("construction_collapse");
        public static readonly FixedString64Bytes ToolFailure = new FixedString64Bytes("tool_failure");
    }

    public static class IncidentLearningUtility
    {
        public static void ApplyDecay(ref IncidentLearningMemory memory, uint currentTick, float secondsPerTick, float decayPerSecond, float minBias)
        {
            if (decayPerSecond <= 0f || currentTick <= memory.LastUpdateTick)
            {
                memory.LastUpdateTick = currentTick;
                return;
            }

            var ticksElapsed = currentTick - memory.LastUpdateTick;
            var decay = decayPerSecond * secondsPerTick * ticksElapsed;
            memory.Bias = math.max(minBias, memory.Bias - decay);
            memory.RecentSeverity = math.max(0f, memory.RecentSeverity - decay);
            memory.LastUpdateTick = currentTick;
        }
    }
}
