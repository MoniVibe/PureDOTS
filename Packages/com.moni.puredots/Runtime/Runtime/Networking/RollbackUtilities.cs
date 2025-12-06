using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Networking
{
    /// <summary>
    /// Utilities for integrating rollback buffers with existing RewindState system.
    /// Provides helpers for loading snapshots, re-applying inputs, and catching up.
    /// </summary>
    [BurstCompile]
    public static class RollbackUtilities
    {
        /// <summary>
        /// Loads a snapshot at the specified tick for rollback.
        /// Integrates with existing RewindState system.
        /// </summary>
        [BurstCompile]
        public static void LoadSnapshotAtTick(ref SystemState state, uint targetTick)
        {
            var rewindState = SystemAPI.GetSingletonRW<RewindState>();
            var tickState = SystemAPI.GetSingletonRW<TickTimeState>();

            // Use existing rewind system to jump to target tick
            rewindState.ValueRW.Mode = RewindMode.Playback;
            rewindState.ValueRW.TargetTick = targetTick;
            rewindState.ValueRW.PlaybackTick = tickState.ValueRO.Tick;
        }

        /// <summary>
        /// Re-applies queued inputs from confirmed tick to current tick.
        /// Used for rollback catch-up after server correction.
        /// </summary>
        [BurstCompile]
        public static void ReapplyInputs(ref SystemState state, uint confirmedTick, uint currentTick)
        {
            // Find input command queue
            var query = SystemAPI.QueryBuilder()
                .WithAll<InputCommandQueueTag, InputCommandBuffer>()
                .Build();

            if (query.IsEmpty)
            {
                return;
            }

            var entity = query.GetSingletonEntity();
            var commandBuffer = SystemAPI.GetBuffer<InputCommandBuffer>(entity);

            // Process commands between confirmed and current tick
            for (uint tick = confirmedTick + 1; tick <= currentTick; tick++)
            {
                for (int i = 0; i < commandBuffer.Length; i++)
                {
                    var cmd = commandBuffer[i];
                    if (cmd.Tick == (int)tick)
                    {
                        // Re-apply command (systems will consume it)
                        // In full implementation, this would trigger command processing
                    }
                }
            }
        }

        /// <summary>
        /// Catches up simulation from confirmed tick to current tick.
        /// Loads snapshot, re-applies inputs, then simulates forward.
        /// </summary>
        [BurstCompile]
        public static void CatchUpToTick(ref SystemState state, uint confirmedTick, uint currentTick)
        {
            // Load snapshot at confirmed tick
            LoadSnapshotAtTick(ref state, confirmedTick);

            // Re-apply queued inputs
            ReapplyInputs(ref state, confirmedTick, currentTick);

            // Transition to catch-up mode
            var rewindState = SystemAPI.GetSingletonRW<RewindState>();
            rewindState.ValueRW.Mode = RewindMode.CatchUp;
        }
    }
}

