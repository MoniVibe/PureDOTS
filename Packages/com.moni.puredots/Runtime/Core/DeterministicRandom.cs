using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Core
{
    /// <summary>
    /// Deterministic random number generation utilities.
    /// All randomness must be seeded from (Tick, EntityId) for rewind safety.
    /// </summary>
    [BurstCompile]
    public static class DeterministicRandom
    {
        /// <summary>
        /// Creates a deterministic Random instance from tick and entity.
        /// Formula: Hash(tick, entity.Index) → Random seed.
        /// </summary>
        [BurstCompile]
        public static Random CreateFromTickAndEntity(uint tick, Entity entity)
        {
            uint seed = HashTickAndEntity(tick, entity);
            return Random.CreateFromIndex(seed);
        }

        /// <summary>
        /// Creates a deterministic Random instance from tick and entity index.
        /// </summary>
        [BurstCompile]
        public static Random CreateFromTickAndEntityIndex(uint tick, int entityIndex)
        {
            uint seed = HashTickAndEntity(tick, (uint)entityIndex);
            return Random.CreateFromIndex(seed);
        }

        /// <summary>
        /// Creates a deterministic Random instance from tick only (for global/system-level RNG).
        /// </summary>
        [BurstCompile]
        public static Random CreateFromTick(uint tick)
        {
            uint seed = HashTickAndEntity(tick, 0u);
            return Random.CreateFromIndex(seed);
        }

        /// <summary>
        /// Hashes tick and entity index into a deterministic seed.
        /// Uses Wang hash for good distribution.
        /// </summary>
        [BurstCompile]
        private static uint HashTickAndEntity(uint tick, uint entityIndex)
        {
            // Combine tick and entity index
            uint combined = tick ^ (entityIndex << 16) ^ (entityIndex >> 16);
            
            // Wang hash for good distribution
            combined = (combined ^ 61u) ^ (combined >> 16);
            combined *= 9u;
            combined = combined ^ (combined >> 4);
            combined *= 0x27d4eb2du;
            combined = combined ^ (combined >> 15);
            
            // Ensure non-zero
            return combined == 0 ? 1u : combined;
        }
    }
}

