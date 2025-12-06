using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Networking;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;

namespace PureDOTS.Systems.Networking
{
    /// <summary>
    /// Computes frame hash validation each tick.
    /// Computes CRC32 across critical component buffers.
    /// Store locally; later compare between peers to detect divergence early.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct WorldHashSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            if (tickState.IsPaused)
            {
                return;
            }

            uint currentTick = tickState.Tick;

            // Get or create world hash singleton
            var query = SystemAPI.QueryBuilder()
                .WithAll<WorldHash>()
                .Build();

            Entity hashEntity;
            if (query.IsEmpty)
            {
                hashEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<WorldHash>(hashEntity);
            }
            else
            {
                hashEntity = query.GetSingletonEntity();
            }

            var worldHash = SystemAPI.GetComponentRW<WorldHash>(hashEntity);
            ref var hash = ref worldHash.ValueRW;

            // Compute CRC32 across critical component buffers
            hash.CRC32 = ComputeWorldStateCRC(ref state);
            hash.Tick = currentTick;
        }

        [BurstCompile]
        private uint ComputeWorldStateCRC(ref SystemState state)
        {
            uint crc = 0;

            // Hash tick state
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            crc ^= tickState.Tick;
            crc ^= (uint)(tickState.IsPlaying ? 1 : 0);
            crc ^= (uint)(tickState.IsPaused ? 1 : 0);

            // Hash network IDs (representative of entity state)
            int entityCount = 0;
            foreach (var networkId in SystemAPI.Query<RefRO<NetworkId>>())
            {
                if (entityCount++ >= 1000) // Sample up to 1000 entities
                {
                    break;
                }
                crc ^= (uint)(networkId.ValueRO.Guid & 0xFFFFFFFF);
                crc ^= (uint)((networkId.ValueRO.Guid >> 32) & 0xFFFFFFFF);
                crc ^= networkId.ValueRO.Authority;
            }

            // Hash input commands for this tick
            var inputQuery = SystemAPI.QueryBuilder()
                .WithAll<InputCommandQueueTag, InputCommandBuffer>()
                .Build();

            if (!inputQuery.IsEmpty)
            {
                var entity = inputQuery.GetSingletonEntity();
                var commandBuffer = SystemAPI.GetBuffer<InputCommandBuffer>(entity);
                
                for (int i = 0; i < commandBuffer.Length; i++)
                {
                    var cmd = commandBuffer[i];
                    if (cmd.Tick == (int)tickState.Tick)
                    {
                        crc ^= (uint)cmd.Tick;
                        crc ^= (uint)(cmd.PlayerId & 0xFFFFFFFF);
                        crc ^= (uint)((cmd.PlayerId >> 32) & 0xFFFFFFFF);
                    }
                }
            }

            return crc;
        }
    }
}

