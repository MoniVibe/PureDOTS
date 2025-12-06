using Unity.Entities;

namespace PureDOTS.Runtime.Modifiers
{
    /// <summary>
    /// Singleton component tracking modifier system statistics for profiling/debugging.
    /// </summary>
    public struct ModifierStats : IComponentData
    {
        /// <summary>
        /// Total number of active modifiers across all entities.
        /// </summary>
        public int ActiveCount;

        /// <summary>
        /// Number of modifiers expired this tick.
        /// </summary>
        public int ExpiredThisTick;

        /// <summary>
        /// Number of modifiers applied this tick.
        /// </summary>
        public int AppliedThisTick;

        /// <summary>
        /// Modifier churn rate (expired + applied per second).
        /// </summary>
        public float ChurnRate;

        /// <summary>
        /// Average modifiers per entity.
        /// </summary>
        public float AvgModifiersPerEntity;

        /// <summary>
        /// Tick when stats were last updated.
        /// </summary>
        public uint LastUpdateTick;
    }
}

