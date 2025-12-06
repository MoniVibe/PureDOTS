using Unity.Entities;

namespace PureDOTS.Runtime.Modifiers
{
    /// <summary>
    /// Singleton component managing a pool of reusable ModifierInstance structs.
    /// Push expired modifiers back to pool instead of freeing memory.
    /// No GC allocations.
    /// </summary>
    public struct ModifierPool : IComponentData
    {
        /// <summary>
        /// Initial pool capacity (pre-allocated).
        /// </summary>
        public int InitialCapacity;

        /// <summary>
        /// Current pool size (managed by ModifierPoolSystem).
        /// </summary>
        public int CurrentSize;
    }
}

