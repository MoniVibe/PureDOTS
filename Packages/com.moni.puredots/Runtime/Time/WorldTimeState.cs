using Unity.Entities;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Tracks per-world time state for multi-world simulations.
    /// </summary>
    public struct WorldTimeState : IComponentData
    {
        /// <summary>
        /// Real time elapsed in this world (wall-clock time).
        /// </summary>
        public double RealTimeMs;

        /// <summary>
        /// Simulation time elapsed in this world (scaled by TimeCoordinator).
        /// </summary>
        public double SimTimeMs;

        /// <summary>
        /// Compression factor: ΔSim / ΔReal.
        /// </summary>
        public float CompressionFactor;

        /// <summary>
        /// Drift between this world and the primary world in milliseconds.
        /// </summary>
        public float DriftMs;

        /// <summary>
        /// Current tick in this world.
        /// </summary>
        public uint Tick;

        public WorldTimeState(byte worldId)
        {
            RealTimeMs = 0.0;
            SimTimeMs = 0.0;
            CompressionFactor = 1.0f;
            DriftMs = 0.0f;
            Tick = 0;
        }
    }
}

