using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.Replay
{
    /// <summary>
    /// API for jumping to a specific tick in a replay.
    /// Used for debugging, QA, and future spectator mode.
    /// </summary>
    public static class ReplayJumpAPI
    {
        /// <summary>
        /// Jumps to a specific tick in the replay.
        /// Restores world state to that tick.
        /// </summary>
#if UNITY_BURST
        // In Burst context, skip managed allocations; gameplay uses managed path.
        [BurstDiscard]
#endif
        public static bool JumpToTick(
            ref SystemState state,
            uint targetTick,
            in ReplayMetadata metadata)
        {
            if (targetTick < metadata.StartTick || targetTick > metadata.EndTick)
            {
                return false;
            }

            var em = state.EntityManager;
            var rewindQuery = em.CreateEntityQuery(ComponentType.ReadWrite<RewindState>());
            if (rewindQuery.IsEmpty)
            {
                return false;
            }

            var rewind = em.GetComponentData<RewindState>(rewindQuery.GetSingletonEntity());

            // Set rewind mode to playback
            rewind.Mode = RewindMode.Playback;
            rewind.PlaybackTick = targetTick;

            em.SetComponentData(rewindQuery.GetSingletonEntity(), rewind);

            // The RewindCoordinatorSystem will handle the actual state restoration
            return true;
        }

        /// <summary>
        /// Gets the current tick in the replay.
        /// </summary>
#if UNITY_BURST
        [BurstDiscard]
#endif
        public static uint GetCurrentTick(ref SystemState state)
        {
            var em = state.EntityManager;
            var timeQuery = em.CreateEntityQuery(ComponentType.ReadOnly<TimeState>());
            if (timeQuery.IsEmpty)
            {
                return 0;
            }

            var timeState = em.GetComponentData<TimeState>(timeQuery.GetSingletonEntity());
            return timeState.Tick;
        }

        /// <summary>
        /// Validates replay integrity by checking tick hashes.
        /// </summary>
#if UNITY_BURST
        [BurstDiscard]
#endif
        public static bool ValidateReplay(
            NativeHashMap<uint, ulong> tickHashes,
            in ReplayMetadata metadata)
        {
            // Check that all ticks in range have hashes
            for (uint tick = metadata.StartTick; tick <= metadata.EndTick; tick++)
            {
                if (!tickHashes.ContainsKey(tick))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

