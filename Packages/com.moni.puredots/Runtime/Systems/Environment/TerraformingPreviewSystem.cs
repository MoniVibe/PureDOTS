using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Runs terraforming calculations on shadow buffers for instant preview.
    /// Commit = swap buffers (zero risk to live simulation).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    [UpdateAfter(typeof(TerraformingDeltaSystem))]
    public partial struct TerraformingPreviewSystem : ISystem
    {
        private TimeAwareController _timeAware;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _timeAware = new TimeAwareController(
                TimeAwareExecutionPhase.Record | TimeAwareExecutionPhase.CatchUp,
                TimeAwareExecutionOptions.SkipWhenPaused);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (!_timeAware.TryBegin(timeState, rewindState, out _))
            {
                return;
            }

            // Check if preview is active
            if (!SystemAPI.HasSingleton<TerraformingPreviewActiveTag>())
            {
                return;
            }

            // Process preview commit commands
            if (SystemAPI.TryGetSingletonEntity<TerraformingPreviewBuffer>(out var previewEntity))
            {
                if (SystemAPI.HasBuffer<TerraformingPreviewCommitCommand>(previewEntity))
                {
                    var commands = SystemAPI.GetBuffer<TerraformingPreviewCommitCommand>(previewEntity);
                    for (int i = 0; i < commands.Length; i++)
                    {
                        if (commands[i].Commit == 1)
                        {
                            CommitPreview(ref state, previewEntity);
                        }
                        // Cancel preview (do nothing, shadow buffer is discarded)
                    }
                    commands.Clear();
                }
            }
        }

        private void CommitPreview(ref SystemState state, Entity previewEntity)
        {
            // Swap shadow buffer to live (implementation depends on field storage)
            // For now, this is a placeholder - actual swap logic depends on how fields are stored
            // In a full implementation, this would swap blob references or copy shadow → live
        }
    }
}

