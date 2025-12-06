using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Cognitive detail level for AI quality scaling.
    /// High-fidelity for visible/critical agents, statistical simulation for distant/idle entities.
    /// </summary>
    public enum CognitiveDetail : byte
    {
        High = 0,       // Full decision logic, all systems active
        Medium = 1,     // Reduced update frequency, simplified logic
        Low = 2,        // Statistical simulation, minimal updates
        Sleep = 3       // No cognitive updates, pure simulation only
    }

    /// <summary>
    /// Cognitive LOD component marking entity's current detail level.
    /// Systems early-exit for lower tiers.
    /// </summary>
    public struct CognitiveLOD : IComponentData
    {
        public CognitiveDetail Detail;     // Current detail level
        public float DistanceScore;         // Distance-based score (0-1, closer = higher)
        public float ImportanceScore;       // Importance score (0-1, leader/elite = higher)
        public float CPULoadFactor;         // Current CPU load factor (0-1, higher = more load)
        public uint LastLODUpdateTick;      // When LOD was last updated
    }

    /// <summary>
    /// LOD assignment state tracking LOD distribution.
    /// </summary>
    public struct CognitiveLODState : IComponentData
    {
        public int HighCount;              // Number of High detail entities
        public int MediumCount;             // Number of Medium detail entities
        public int LowCount;                // Number of Low detail entities
        public int SleepCount;              // Number of Sleep detail entities
        public float TargetCPULoad;          // Target CPU load (0-1)
        public uint LastDistributionTick;   // When distribution was last computed
    }
}

