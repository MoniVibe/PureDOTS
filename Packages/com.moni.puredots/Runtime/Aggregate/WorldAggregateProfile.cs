using Unity.Entities;

namespace PureDOTS.Runtime.Aggregate
{
    /// <summary>
    /// Singleton component storing aggregate world metrics computed from simulation data.
    /// Updated by reduction systems each N ticks for visualization and feedback.
    /// </summary>
    public struct WorldAggregateProfile : IComponentData
    {
        /// <summary>
        /// Total population count across all entities.
        /// </summary>
        public float Population;

        /// <summary>
        /// Energy flux (resources, miracles, production).
        /// </summary>
        public float EnergyFlux;

        /// <summary>
        /// Averaged morality / pollution balance (0-1, higher = more harmonious).
        /// </summary>
        public float Harmony;

        /// <summary>
        /// Chaos metric (conflicts, disasters, instability).
        /// </summary>
        public float Chaos;
    }

    /// <summary>
    /// History buffer storing deltas for visualization.
    /// </summary>
    public struct HistoryBuffer : IBufferElementData
    {
        public uint Tick;
        public WorldAggregateProfile Profile;
    }
}

