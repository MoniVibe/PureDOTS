using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Networking;
using PureDOTS.Runtime.Telemetry;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;

namespace PureDOTS.Systems.Telemetry
{
    /// <summary>
    /// Computes and logs sync telemetry metrics for multiplayer debugging.
    /// Logs tick number, input hash, world state CRC, and latency placeholders.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct SyncTelemetrySystem : ISystem
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

            // Get or create sync telemetry singleton
            var query = SystemAPI.QueryBuilder()
                .WithAll<SyncTelemetry>()
                .Build();

            Entity telemetryEntity;
            if (query.IsEmpty)
            {
                telemetryEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<SyncTelemetry>(telemetryEntity);
                state.EntityManager.AddBuffer<SyncTelemetrySample>(telemetryEntity);
            }
            else
            {
                telemetryEntity = query.GetSingletonEntity();
            }

            var syncTelemetry = SystemAPI.GetComponentRW<SyncTelemetry>(telemetryEntity);
            ref var telemetry = ref syncTelemetry.ValueRW;

            // Update tick
            telemetry.Tick = currentTick;

            // Compute input hash (CRC32 of input commands)
            telemetry.InputHash = ComputeInputHash(ref state, currentTick);

            // Compute world state CRC (CRC32 of critical components)
            telemetry.WorldStateCRC = ComputeWorldStateCRC(ref state);

            // Placeholder latency values (in multiplayer, these would be actual network latencies)
            telemetry.LocalLatencyMs = 0;
            telemetry.RemoteLatencyMs = 0;

            // Store sample in buffer
            var sampleBuffer = SystemAPI.GetBuffer<SyncTelemetrySample>(telemetryEntity);
            sampleBuffer.Add(new SyncTelemetrySample
            {
                Tick = telemetry.Tick,
                InputHash = telemetry.InputHash,
                WorldStateCRC = telemetry.WorldStateCRC,
                LocalLatencyMs = telemetry.LocalLatencyMs,
                RemoteLatencyMs = telemetry.RemoteLatencyMs
            });

            // Keep buffer size reasonable
            if (sampleBuffer.Length > 1000)
            {
                sampleBuffer.RemoveAt(0);
            }
        }

        [BurstCompile]
        private uint ComputeInputHash(ref SystemState state, uint tick)
        {
            uint hash = 0;

            // Find input command queue
            var query = SystemAPI.QueryBuilder()
                .WithAll<InputCommandQueueTag, InputCommandBuffer>()
                .Build();

            if (!query.IsEmpty)
            {
                var entity = query.GetSingletonEntity();
                var commandBuffer = SystemAPI.GetBuffer<InputCommandBuffer>(entity);

                // Hash commands for this tick
                for (int i = 0; i < commandBuffer.Length; i++)
                {
                    var cmd = commandBuffer[i];
                    if (cmd.Tick == (int)tick)
                    {
                        // Simple hash combination
                        hash ^= (uint)cmd.Tick;
                        hash ^= (uint)(cmd.PlayerId & 0xFFFFFFFF);
                        hash ^= (uint)((cmd.PlayerId >> 32) & 0xFFFFFFFF);
                        
                        // Hash payload
                        unsafe
                        {
                            fixed (void* ptr = &cmd.Payload)
                            {
                                byte* bytes = (byte*)ptr;
                                for (int j = 0; j < 16; j++)
                                {
                                    hash = (hash << 1) ^ bytes[j];
                                }
                            }
                        }
                    }
                }
            }

            return hash;
        }

        [BurstCompile]
        private uint ComputeWorldStateCRC(ref SystemState state)
        {
            uint crc = 0;

            // Sample critical components for CRC
            // In full implementation, this would hash all critical simulation state
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            crc ^= tickState.Tick;
            crc ^= (uint)(tickState.IsPlaying ? 1 : 0);
            crc ^= (uint)(tickState.IsPaused ? 1 : 0);

            // Hash a sample of entities with NetworkId (representative of world state)
            int sampleCount = 0;
            foreach (var networkId in SystemAPI.Query<RefRO<NetworkId>>())
            {
                if (sampleCount++ >= 100) // Sample first 100 entities
                {
                    break;
                }
                crc ^= (uint)(networkId.ValueRO.Guid & 0xFFFFFFFF);
                crc ^= (uint)((networkId.ValueRO.Guid >> 32) & 0xFFFFFFFF);
                crc ^= networkId.ValueRO.Authority;
            }

            return crc;
        }
    }
}

