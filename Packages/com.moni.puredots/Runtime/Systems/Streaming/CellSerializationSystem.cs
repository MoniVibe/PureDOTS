using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Systems.Streaming
{
    /// <summary>
    /// System handling deterministic cell serialization/rehydration.
    /// Inactive cells serialize to disk; rehydrate deterministically when revisited.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CellStreamingSystem))]
    public partial struct CellSerializationSystem : ISystem
    {
        private CellSnapshotStore _snapshotStore;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<CellStreamingConfig>();
            _snapshotStore = new CellSnapshotStore(128, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _snapshotStore.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            if (tickState.IsPaused)
            {
                return;
            }

            var config = SystemAPI.GetSingleton<CellStreamingConfig>();
            var agentSizeBytes = config.EstimatedAgentBytes <= 0 ? 64f : config.EstimatedAgentBytes; // fallback estimate

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            int activeCells = 0;
            int serializedCells = 0;
            int activeAgents = 0;
            int serializedAgents = 0;
            int approxBytes = 0;

            foreach (var (cell, streamingState, agents, cellEntity) in SystemAPI.Query<
                         RefRW<SimulationCell>,
                         RefRW<CellStreamingState>,
                         DynamicBuffer<CellAgentBuffer>>()
                         .WithEntityAccess())
            {
                bool shouldBeActive = cell.ValueRO.IsActive != 0;
                bool isSerialized = streamingState.ValueRO.IsSerialized != 0;

                if (!shouldBeActive && !isSerialized)
                {
                    // Serialize: disable agents, mark serialized.
                    serializedCells++;
                    serializedAgents += agents.Length;
                    for (int i = 0; i < agents.Length; i++)
                    {
                        var agent = agents[i].AgentEntity;
                        if (!state.EntityManager.HasComponent<Disabled>(agent))
                        {
                            ecb.AddComponent<Disabled>(agent);
                        }
                    }
                    streamingState.ValueRW.IsSerialized = 1;
                    streamingState.ValueRW.LastSerializedTick = tickState.Tick;
                    if (_snapshotStore.Snapshots.IsCreated)
                    {
                        _snapshotStore.Set(cellEntity, new CellSnapshot
                        {
                            AgentCount = agents.Length
                        });
                    }
                }
                else if (shouldBeActive && isSerialized)
                {
                    // Rehydrate: enable agents, clear serialized flag.
                    activeCells++;
                    activeAgents += agents.Length;
                    for (int i = 0; i < agents.Length; i++)
                    {
                        var agent = agents[i].AgentEntity;
                        if (state.EntityManager.HasComponent<Disabled>(agent))
                        {
                            ecb.RemoveComponent<Disabled>(agent);
                        }
                    }
                    streamingState.ValueRW.IsSerialized = 0;
                }
                else
                {
                    if (shouldBeActive)
                    {
                        activeCells++;
                        activeAgents += agents.Length;
                    }
                    else
                    {
                        serializedCells += isSerialized ? 1 : 0;
                        serializedAgents += isSerialized ? agents.Length : 0;
                    }
                }

                if (isSerialized)
                {
                    approxBytes += (int)math.round(agents.Length * agentSizeBytes);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            // Update metrics singleton.
            if (!SystemAPI.TryGetSingleton<CellStreamingMetrics>(out var metrics))
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, new CellStreamingMetrics
                {
                    ActiveCells = activeCells,
                    SerializedCells = serializedCells,
                    ActiveAgents = activeAgents,
                    SerializedAgents = serializedAgents,
                    ApproxBytes = approxBytes
                });
            }
            else
            {
                var metricsEntity = SystemAPI.GetSingletonEntity<CellStreamingMetrics>();
                state.EntityManager.SetComponentData(metricsEntity, new CellStreamingMetrics
                {
                    ActiveCells = activeCells,
                    SerializedCells = serializedCells,
                    ActiveAgents = activeAgents,
                    SerializedAgents = serializedAgents,
                    ApproxBytes = approxBytes
                });
            }
        }
    }
}

