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
    [BurstCompile]
    public static class ReplayJumpAPI
    {
        /// <summary>
        /// Jumps to a specific tick in the replay.
        /// Restores world state to that tick.
        /// </summary>
        [BurstCompile]
        public static bool JumpToTick(
            ref SystemState state,
            uint targetTick,
            in ReplayMetadata metadata)
        {
            if (targetTick < metadata.StartTick || targetTick > metadata.EndTick)
            {
                return false;
            }

            var rewindState = SystemAPI.GetSingletonRW<RewindState>();
            ref var rewind = ref rewindState.ValueRW;

            // Set rewind mode to playback
            rewind.Mode = RewindMode.Playback;
            rewind.PlaybackTick = targetTick;

            // The RewindCoordinatorSystem will handle the actual state restoration
            return true;
        }

        /// <summary>
        /// Gets the current tick in the replay.
        /// </summary>
        [BurstCompile]
        public static uint GetCurrentTick(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            return timeState.Tick;
        }

        /// <summary>
        /// Validates replay integrity by checking tick hashes.
        /// </summary>
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

