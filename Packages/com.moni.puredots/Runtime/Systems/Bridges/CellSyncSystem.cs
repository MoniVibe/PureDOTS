using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Components;
using PureDOTS.Shared;
using PureDOTS.Systems;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Cell synchronization system for binary diff streams between cells.
    /// Syncs state between multiple PureDOTS worlds via AgentSyncBus extension.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BodyToMindSyncSystem))]
    public partial struct CellSyncSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldCellConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Sync cells via binary diff streams
            // In full implementation, would:
            // 1. Compute binary diffs for entities crossing cell boundaries
            // 2. Serialize diffs to binary format
            // 3. Send diffs to other cells via AgentSyncBus extension
            // 4. Apply diffs in receiving cells
            // 5. Maintain determinism across cells

            var cellQuery = state.GetEntityQuery(typeof(WorldCellConfig), typeof(WorldCellState));
            if (cellQuery.IsEmpty)
            {
                return;
            }

            var job = new SyncCellsJob
            {
                CurrentTick = tickState.Tick
            };

            state.Dependency = job.ScheduleParallel(cellQuery, state.Dependency);
        }

        [BurstCompile]
        private partial struct SyncCellsJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                RefRO<WorldCellConfig> config,
                RefRW<WorldCellState> state)
            {
                if (!config.ValueRO.IsActive)
                {
                    return;
                }

                // Mark cell as needing sync
                state.ValueRW.NeedsSync = true;
                state.ValueRW.LastSyncTick = CurrentTick;
            }
        }
    }
}

