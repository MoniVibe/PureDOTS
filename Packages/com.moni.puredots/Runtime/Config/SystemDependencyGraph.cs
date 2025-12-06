using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Config
{
    /// <summary>
    /// System dependency graph for meta-scheduling.
    /// Tracks system dependencies and execution order constraints.
    /// </summary>
    public struct SystemDependencyGraph : IComponentData
    {
        // Dependency graph stored as adjacency list
        // In full implementation, would use NativeHashMap for efficient lookups
        public uint Version;
    }

    /// <summary>
    /// System impact metrics tracked per system for adaptive scheduling.
    /// </summary>
    public struct SystemImpactMetrics : IComponentData
    {
        public float EnergyUse;          // CPU cost in ms
        public int ChangedEntityCount;   // Number of entities modified
        public float DeltaImpact;        // Impact per tick
        public uint LastUpdateTick;      // When metrics were last updated
        public int Priority;             // Current priority (higher = more important)
    }

    /// <summary>
    /// System dependency edge in the dependency graph.
    /// </summary>
    public struct SystemDependencyEdge : IBufferElementData
    {
        public FixedString64Bytes SourceSystem;
        public FixedString64Bytes TargetSystem;
        public byte DependencyType; // 0 = before, 1 = after, 2 = requires
    }
}

