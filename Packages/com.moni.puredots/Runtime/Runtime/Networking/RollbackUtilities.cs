using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Networking
{
    /// <summary>
    /// Utilities for integrating rollback buffers with existing RewindState system.
    /// Provides helpers for loading snapshots, re-applying inputs, and catching up.
    /// </summary>
    public static class RollbackUtilities
    {
        /// <summary>
        /// Loads a snapshot at the specified tick for rollback.
        /// Integrates with existing RewindState system.
        /// </summary>
        public static void LoadSnapshotAtTick(ref SystemState state, uint targetTick)
        {
            LoadSnapshotAtTickManaged(ref state, targetTick);
        }

        private static void LoadSnapshotAtTickManaged(ref SystemState state, uint targetTick)
        {
            var em = state.EntityManager;

            var rewindQuery = em.CreateEntityQuery(ComponentType.ReadWrite<RewindState>());
            var tickQuery = em.CreateEntityQuery(ComponentType.ReadOnly<TickTimeState>());
            if (rewindQuery.IsEmpty || tickQuery.IsEmpty)
            {
                return;
            }

            var rewindState = em.GetComponentData<RewindState>(rewindQuery.GetSingletonEntity());
            var tickState = em.GetComponentData<TickTimeState>(tickQuery.GetSingletonEntity());

            // Use existing rewind system to jump to target tick
            rewindState.Mode = RewindMode.Playback;
            rewindState.TargetTick = targetTick;
            rewindState.PlaybackTick = tickState.Tick;

            em.SetComponentData(rewindQuery.GetSingletonEntity(), rewindState);
        }

        /// <summary>
        /// Re-applies queued inputs from confirmed tick to current tick.
        /// Used for rollback catch-up after server correction.
        /// </summary>
        public static void ReapplyInputs(ref SystemState state, uint confirmedTick, uint currentTick)
        {
            ReapplyInputsManaged(ref state, confirmedTick, currentTick);
        }

        private static void ReapplyInputsManaged(ref SystemState state, uint confirmedTick, uint currentTick)
        {
            var em = state.EntityManager;

            // Find input command queue
            var query = em.CreateEntityQuery(ComponentType.ReadOnly<InputCommandQueueTag>(), ComponentType.ReadOnly<InputCommandBuffer>());

            if (query.IsEmpty)
            {
                return;
            }

            var entity = query.GetSingletonEntity();
            var commandBuffer = em.GetBuffer<InputCommandBuffer>(entity);

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
        public static void CatchUpToTick(ref SystemState state, uint confirmedTick, uint currentTick)
        {
            CatchUpToTickManaged(ref state, confirmedTick, currentTick);
        }

        private static void CatchUpToTickManaged(ref SystemState state, uint confirmedTick, uint currentTick)
        {
            // Load snapshot at confirmed tick
            LoadSnapshotAtTickManaged(ref state, confirmedTick);

            // Re-apply queued inputs
            ReapplyInputsManaged(ref state, confirmedTick, currentTick);

            // Transition to catch-up mode
            var em = state.EntityManager;
            var rewindQuery = em.CreateEntityQuery(ComponentType.ReadWrite<RewindState>());
            if (rewindQuery.IsEmpty)
            {
                return;
            }

            var rewindState = em.GetComponentData<RewindState>(rewindQuery.GetSingletonEntity());
            rewindState.Mode = RewindMode.CatchUp;
            em.SetComponentData(rewindQuery.GetSingletonEntity(), rewindState);
        }
    }
}

