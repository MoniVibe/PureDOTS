using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Deterministic RNG for rewind-safe random number generation.
    /// Uses counter-based RNG (Philox/XorShift) seeded by (EntityId, Tick).
    /// Rewinding simply decrements the counter - no stored randoms, no divergence.
    /// </summary>
    [BurstCompile]
    public static class DeterministicRNG
    {
        /// <summary>
        /// Generate deterministic seed from EntityId and Tick.
        /// Formula: (EntityId, Tick) → uint4 key for Philox.
        /// </summary>
        [BurstCompile]
        public static uint4 GenerateSeed(uint entityId, uint tick)
        {
            // Combine EntityId and Tick into a deterministic uint4 seed
            // Using hash-like mixing to ensure good distribution
            uint h1 = entityId;
            uint h2 = tick;
            uint h3 = (entityId << 16) | (tick >> 16);
            uint h4 = (tick << 16) | (entityId >> 16);
            
            // Mix bits for better distribution
            h1 ^= h2;
            h2 ^= h3;
            h3 ^= h4;
            h4 ^= h1;
            
            return new uint4(h1, h2, h3, h4);
        }

        /// <summary>
        /// Philox counter-based RNG implementation.
        /// Deterministic: same key + counter → same output.
        /// </summary>
        [BurstCompile]
        public static uint4 Philox(uint4 key, uint counter)
        {
            // Simplified Philox-like RNG for deterministic replay
            // This is a lightweight implementation suitable for game simulation
            
            uint4 state = key;
            uint c = counter;
            
            // Multiple rounds of mixing
            for (int round = 0; round < 10; round++)
            {
                // Mix counter into state
                state.x ^= c;
                state.y ^= (c << 1) | (c >> 31);
                state.z ^= (c << 2) | (c >> 30);
                state.w ^= (c << 3) | (c >> 29);
                
                // Permute state
                uint temp = state.x;
                state.x = state.y;
                state.y = state.z;
                state.z = state.w;
                state.w = temp ^ (state.x << 1);
                
                // Mix key
                state.x ^= key.x;
                state.y ^= key.y;
                state.z ^= key.z;
                state.w ^= key.w;
                
                c = (c << 1) | (c >> 31);
            }
            
            return state;
        }

        /// <summary>
        /// XorShift-based RNG for simpler use cases.
        /// Deterministic: same seed + counter → same output.
        /// </summary>
        [BurstCompile]
        public static uint XorShift(uint seed, uint counter)
        {
            uint state = seed ^ counter;
            
            // XorShift32 algorithm
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            
            return state;
        }

        /// <summary>
        /// Create a deterministic Random instance from EntityId and Tick.
        /// Use this instead of new Random() for rewind-safe RNG.
        /// </summary>
        [BurstCompile]
        public static Random CreateFromEntityTick(uint entityId, uint tick)
        {
            uint4 seed = GenerateSeed(entityId, tick);
            // Use first component as seed for Unity.Mathematics.Random
            return new Random(seed.x);
        }

        /// <summary>
        /// Create a deterministic Random instance from Tick only (for global RNG).
        /// </summary>
        [BurstCompile]
        public static Random CreateFromTick(uint tick)
        {
            uint4 seed = GenerateSeed(0u, tick);
            return new Random(seed.x);
        }

        /// <summary>
        /// Generate next random value using Philox RNG.
        /// Counter increments each call - rewind by decrementing counter.
        /// </summary>
        [BurstCompile]
        public static uint4 NextPhilox(ref uint4 state, ref uint counter)
        {
            uint4 result = Philox(state, counter);
            counter++;
            return result;
        }

        /// <summary>
        /// Generate next random float [0, 1) using Philox RNG.
        /// </summary>
        [BurstCompile]
        public static float NextFloatPhilox(ref uint4 state, ref uint counter)
        {
            uint4 r = NextPhilox(ref state, ref counter);
            // Convert uint to float [0, 1)
            return (r.x & 0x7FFFFFu) / 8388608.0f;
        }

        /// <summary>
        /// Generate next random int [min, max) using Philox RNG.
        /// </summary>
        [BurstCompile]
        public static int NextIntPhilox(ref uint4 state, ref uint counter, int min, int max)
        {
            float f = NextFloatPhilox(ref state, ref counter);
            return min + (int)(f * (max - min));
        }
    }

    /// <summary>
    /// Component storing deterministic RNG state for an entity.
    /// Allows rewinding RNG by decrementing Counter.
    /// </summary>
    public struct DeterministicRNGState : IComponentData
    {
        /// <summary>RNG key (derived from EntityId + base Tick).</summary>
        public uint4 Key;
        /// <summary>Current counter (increments on each Next call, decrements on rewind).</summary>
        public uint Counter;
        /// <summary>Base tick when this RNG was initialized.</summary>
        public uint BaseTick;
    }

    /// <summary>
    /// Helper for managing deterministic RNG per entity.
    /// </summary>
    [BurstCompile]
    public static class DeterministicRNGHelper
    {
        /// <summary>
        /// Get or create deterministic RNG state for an entity.
        /// </summary>
        [BurstCompile]
        public static DeterministicRNGState GetOrCreateRNGState(Entity entity, uint currentTick)
        {
            return new DeterministicRNGState
            {
                Key = DeterministicRNG.GenerateSeed((uint)entity.Index, currentTick),
                Counter = 0u,
                BaseTick = currentTick
            };
        }

        /// <summary>
        /// Get next random float [0, 1) for an entity.
        /// </summary>
        [BurstCompile]
        public static float NextFloat(ref DeterministicRNGState rngState)
        {
            return DeterministicRNG.NextFloatPhilox(ref rngState.Key, ref rngState.Counter);
        }

        /// <summary>
        /// Get next random int [min, max) for an entity.
        /// </summary>
        [BurstCompile]
        public static int NextInt(ref DeterministicRNGState rngState, int min, int max)
        {
            return DeterministicRNG.NextIntPhilox(ref rngState.Key, ref rngState.Counter, min, max);
        }

        /// <summary>
        /// Rewind RNG state by decrementing counter (if > 0).
        /// </summary>
        [BurstCompile]
        public static void Rewind(ref DeterministicRNGState rngState, uint targetTick)
        {
            if (rngState.BaseTick > targetTick)
            {
                // Reset to target tick
                rngState.Counter = 0u;
                rngState.BaseTick = targetTick;
                rngState.Key = DeterministicRNG.GenerateSeed(
                    DeterministicRNG.XorShift((uint)rngState.Key.x, targetTick).x,
                    targetTick);
            }
            else if (rngState.Counter > 0)
            {
                // Decrement counter (simplified - in practice may need to track call count)
                rngState.Counter = math.max(0u, rngState.Counter - 1u);
            }
        }
    }
}

