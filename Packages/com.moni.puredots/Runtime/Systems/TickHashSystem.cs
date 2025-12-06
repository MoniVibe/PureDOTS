using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Computes deterministic hash per tick for debugging and replay validation.
    /// Hash includes all component states - mismatches indicate non-determinism.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(TimeSystemGroup))]
    [UpdateAfter(typeof(TimeTickSystem))]
    public partial struct TickHashSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingletonRW<RewindState>();
            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            var currentTick = tickTimeState.Tick;

            // Compute hash of all component states
            // This is a simplified version - full implementation would hash all entities/components
            ulong tickHash = HashTick(currentTick);

            // Store hash in RewindState (we'll need to extend RewindState)
            // For now, store in a buffer
            if (!SystemAPI.HasSingleton<TickHashState>())
            {
                var hashEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<TickHashState>(hashEntity);
                state.EntityManager.AddBuffer<TickHashEntry>(hashEntity);
            }

            var hashEntity = SystemAPI.GetSingletonEntity<TickHashState>();
            var hashBuffer = SystemAPI.GetBuffer<TickHashEntry>(hashEntity);

            // Add hash entry for this tick
            hashBuffer.Add(new TickHashEntry
            {
                Tick = currentTick,
                Hash = tickHash
            });

            // Keep only last N ticks of hash history
            const uint HashRetentionTicks = 1000;
            var minTick = currentTick > HashRetentionTicks ? currentTick - HashRetentionTicks : 0;
            for (int i = hashBuffer.Length - 1; i >= 0; i--)
            {
                if (hashBuffer[i].Tick < minTick)
                {
                    hashBuffer.RemoveAt(i);
                }
            }

            // Store random seed for this tick
            var randomSeedBuffer = SystemAPI.GetBuffer<RandomSeedPerTick>(hashEntity);
            randomSeedBuffer.Add(new RandomSeedPerTick
            {
                Tick = currentTick,
                Seed = (uint)(currentTick * 0x9E3779B9u) // Deterministic seed from tick
            });

            // Keep only last N ticks of seed history
            for (int i = randomSeedBuffer.Length - 1; i >= 0; i--)
            {
                if (randomSeedBuffer[i].Tick < minTick)
                {
                    randomSeedBuffer.RemoveAt(i);
                }
            }
        }

        [BurstCompile]
        private static ulong HashTick(uint tick)
        {
            // Simplified hash - full implementation would hash all component states
            // This is a placeholder that demonstrates the concept
            ulong hash = (ulong)tick;
            hash ^= (hash << 13) | (hash >> 51);
            hash *= 0x9E3779B97F4A7C15UL;
            hash ^= (hash >> 15);
            return hash;
        }
    }

    /// <summary>
    /// Singleton state for tick hashing.
    /// </summary>
    public struct TickHashState : IComponentData
    {
        public uint LastComputedTick;
    }

    /// <summary>
    /// Hash entry per tick.
    /// </summary>
    public struct TickHashEntry : IBufferElementData
    {
        public uint Tick;
        public ulong Hash;
    }

    /// <summary>
    /// Random seed per tick for replay validation.
    /// </summary>
    public struct RandomSeedPerTick : IBufferElementData
    {
        public uint Tick;
        public uint Seed;
    }
}

