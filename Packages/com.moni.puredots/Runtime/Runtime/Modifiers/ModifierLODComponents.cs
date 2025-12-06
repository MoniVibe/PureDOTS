using Unity.Entities;

namespace PureDOTS.Runtime.Modifiers
{
    /// <summary>
    /// Statistical aggregates for distant/background entities (LOD culling).
    /// Replaces per-entity modifier buffers with averaged values.
    /// </summary>
    public struct ModifierLODAggregate : IComponentData
    {
        /// <summary>
        /// Average morale across all entities in this LOD group.
        /// </summary>
        public float MoraleAvg;

        /// <summary>
        /// Average productivity across all entities in this LOD group.
        /// </summary>
        public float ProductivityAvg;

        /// <summary>
        /// Number of entities contributing to this aggregate.
        /// </summary>
        public int EntityCount;

        /// <summary>
        /// Distance threshold for LOD (entities beyond this use aggregates).
        /// </summary>
        public float LODDistance;
    }
}

