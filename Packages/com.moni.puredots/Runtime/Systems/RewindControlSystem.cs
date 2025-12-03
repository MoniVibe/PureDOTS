using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using UnityEngine;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Manages the preview-based rewind control system.
    /// Handles phase transitions, world freezing, and preview tick scrubbing.
    /// Processes TimeControlCommand entities created by TimeAPI.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct RewindControlSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindControlState>();
            state.RequireForUpdate<TickTimeState>(); // Required for HandleBeginPreview
        }

        [BurstDiscard] // Contains Debug.Log calls
        public void OnUpdate(ref SystemState state)
        {
            var controlRW = SystemAPI.GetSingletonRW<RewindControlState>();
            var control = controlRW.ValueRO;
            var em = state.EntityManager;

            // Debug: show we're alive (only log occasionally to reduce spam)
#if UNITY_EDITOR && DEBUG_REWIND
            if (UnityEngine.Time.frameCount % 60 == 0) // Every 60 frames
            {
                Debug.Log($"[RewindControlSystem] OnUpdate Phase={control.Phase}");
            }
#endif

            // Create an ECB on the stack for this frame
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            int commandCount = 0;
            // Process commands
            foreach (var (cmdBuffer, entity) in SystemAPI
                     .Query<DynamicBuffer<TimeControlCommand>>()
                     .WithEntityAccess())
            {
                for (int i = 0; i < cmdBuffer.Length; i++)
                {
                    var cmd = cmdBuffer[i];
                    commandCount++;

#if UNITY_EDITOR
                    UnityEngine.Debug.Log($"[RewindControlSystem] Got command {cmd.Type} from entity {entity.Index} (FloatParam={cmd.FloatParam})");
#endif

                    switch (cmd.Type)
                    {
                        case TimeControlCommandType.BeginPreviewRewind:
                            HandleBeginPreview(ref state, ref controlRW.ValueRW, cmd.FloatParam);
                            break;

                        case TimeControlCommandType.UpdatePreviewRewindSpeed:
                            controlRW.ValueRW.ScrubSpeed = cmd.FloatParam;
#if UNITY_EDITOR
                            UnityEngine.Debug.Log($"[RewindControlSystem] UpdatePreviewRewindSpeed: {controlRW.ValueRW.ScrubSpeed:F2}x");
#endif
                            break;

                        case TimeControlCommandType.EndScrubPreview:
                            HandleEndScrub(ref controlRW.ValueRW);
                            break;

                        case TimeControlCommandType.CommitRewindFromPreview:
                            HandleCommitRequest(ref controlRW.ValueRW);
                            break;

                        case TimeControlCommandType.CancelRewindPreview:
                            HandleCancel(ref state, ref controlRW.ValueRW);
                            break;
                    }
                }

                // Defer entity destruction until after iteration
                ecb.DestroyEntity(entity);
            }

            // Apply all structural changes after iteration
            ecb.Playback(em);
            ecb.Dispose();

#if UNITY_EDITOR
            if (commandCount > 0)
            {
                UnityEngine.Debug.Log($"[RewindControlSystem] Processed {commandCount} command(s), Phase now={controlRW.ValueRO.Phase}");
            }
#endif

            // TODO: update PreviewTick while in ScrubbingPreview
        }

        // Keep these NON-static so SystemAPI is allowed inside
        [BurstDiscard]
        private void HandleBeginPreview(ref SystemState state, ref RewindControlState control, float scrubSpeed)
        {
            // Set phase, ticks, and freeze timescale
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            control.Phase = RewindPhase.ScrubbingPreview;
            control.PresentTickAtStart = (int)tickState.Tick;
            control.PreviewTick = (int)tickState.Tick;
            control.ScrubSpeed = scrubSpeed;

#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[RewindControlSystem] BeginPreview -> Phase={control.Phase} Present={control.PresentTickAtStart} Preview={control.PreviewTick} Speed={control.ScrubSpeed:F2}x");
#endif

            EnqueueTimeScaleCommand(ref state, 0f); // freeze
        }

        [BurstDiscard]
        private void HandleEndScrub(ref RewindControlState control)
        {
            if (control.Phase == RewindPhase.ScrubbingPreview)
            {
                control.Phase = RewindPhase.FrozenPreview;
#if UNITY_EDITOR
                UnityEngine.Debug.Log($"[RewindControlSystem] EndScrub -> FrozenPreview at PreviewTick={control.PreviewTick}");
#endif
            }
        }

        [BurstDiscard]
        private void HandleCommitRequest(ref RewindControlState control)
        {
            if (control.Phase == RewindPhase.ScrubbingPreview ||
                control.Phase == RewindPhase.FrozenPreview)
            {
                control.Phase = RewindPhase.CommitPlayback;
#if UNITY_EDITOR
                UnityEngine.Debug.Log($"[RewindControlSystem] Commit request -> CommitPlayback at PreviewTick={control.PreviewTick}");
#endif
            }
        }

        [BurstDiscard]
        private void HandleCancel(ref SystemState state, ref RewindControlState control)
        {
            if (control.Phase == RewindPhase.ScrubbingPreview ||
                control.Phase == RewindPhase.FrozenPreview)
            {
                control.Phase = RewindPhase.Inactive;
                EnqueueTimeScaleCommand(ref state, 1f); // unfreeze

#if UNITY_EDITOR
                UnityEngine.Debug.Log($"[RewindControlSystem] Cancel -> Inactive + speed 1.0");
#endif
            }
        }

        [BurstDiscard]
        private void EnqueueTimeScaleCommand(ref SystemState state, float targetScale)
        {
            var entity = state.EntityManager.CreateEntity();
            var buffer = state.EntityManager.AddBuffer<TimeControlCommand>(entity);
            buffer.Add(new TimeControlCommand
            {
                Type = TimeControlCommandType.SetSpeed,  // this MUST be the enum your TimeScaleCommandSystem understands
                FloatParam = targetScale,
                Scope = TimeControlScope.Global,
                Source = TimeControlSource.System,
                PlayerId = 0,
                Priority = 100
            });

#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[RewindControlSystem] Enqueued SetSpeed {targetScale:F2}x");
#endif
        }
    }
}
