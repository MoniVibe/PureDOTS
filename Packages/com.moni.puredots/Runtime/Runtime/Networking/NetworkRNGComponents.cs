using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Networking
{
    /// <summary>
    /// Network RNG component for deterministic random number generation.
    /// Seeds RNG with (WorldSeed, PlayerId, Tick) combination.
    /// No shared random state between worlds = no cross-client divergence.
    /// </summary>
    public struct NetworkRNG : IComponentData
    {
        /// <summary>
        /// World seed for deterministic RNG.
        /// </summary>
        public uint WorldSeed;

        /// <summary>
        /// Player ID for per-player RNG streams.
        /// </summary>
        public ulong PlayerId;

        /// <summary>
        /// Current tick for tick-based RNG seeding.
        /// </summary>
        public uint Tick;

        /// <summary>
        /// Creates a deterministic Random instance from this NetworkRNG.
        /// </summary>
        public Random CreateRandom()
        {
            // Combine WorldSeed, PlayerId, and Tick for deterministic seed
            uint combinedSeed = WorldSeed;
            combinedSeed ^= (uint)(PlayerId & 0xFFFFFFFF);
            combinedSeed ^= (uint)((PlayerId >> 32) & 0xFFFFFFFF);
            combinedSeed ^= Tick;
            
            // Ensure non-zero seed
            if (combinedSeed == 0)
            {
                combinedSeed = 1;
            }

            return new Random(combinedSeed);
        }
    }

    /// <summary>
    /// Singleton component storing global RNG configuration.
    /// </summary>
    public struct NetworkRNGConfig : IComponentData
    {
        /// <summary>
        /// Global world seed for deterministic simulation.
        /// </summary>
        public uint WorldSeed;

        /// <summary>
        /// Default player ID for single-player mode.
        /// </summary>
        public ulong DefaultPlayerId;
    }
}

