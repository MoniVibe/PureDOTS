using Unity.Burst;
using Unity.Entities;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Replay;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Writes replay data (command log + tick hashes) each frame.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct ReplayWriterSystem : ISystem
    {
        private ReplayService _replayService;
        private bool _initialized;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _replayService = new ReplayService(Unity.Collections.Allocator.Persistent);
            _initialized = false;
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _replayService.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            uint currentTick = timeState.Tick;

            // Write tick hash (simplified - would compute actual hash of world state)
            ulong tickHash = ComputeTickHash(ref state, currentTick);
            _replayService.WriteTickHash(currentTick, tickHash);

            // Write commands from InputCommandLogState if available
            if (SystemAPI.TryGetSingleton<InputCommandLogState>(out var commandLog))
            {
                // Commands would be written here
                // For now, this is a placeholder showing the pattern
            }
        }

        [BurstCompile]
        private ulong ComputeTickHash(ref SystemState state, uint tick)
        {
            // Simplified hash computation
            // In a real implementation, this would hash the entire world state
            return (ulong)tick * 0x9e3779b97f4a7c15UL;
        }
    }
}

