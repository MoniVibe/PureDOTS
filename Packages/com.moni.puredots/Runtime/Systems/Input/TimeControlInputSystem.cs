using InputState = PureDOTS.Input.TimeControlInputState;
using RuntimeTimeControlInputState = PureDOTS.Runtime.Components.TimeControlInputState;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Input
{
    /// <summary>
    /// Converts player-facing time control input into deterministic commands consumed by <see cref="RewindCoordinatorSystem"/>.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CopyInputToEcsSystem))]
    public partial struct TimeControlInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeControlSingletonTag>();
            state.RequireForUpdate<TimeControlConfig>();
            state.RequireForUpdate<RuntimeTimeControlInputState>();
            state.RequireForUpdate<TimeControlCommand>();
            state.RequireForUpdate<TimeState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeStateRO = SystemAPI.GetSingleton<TimeState>();

            foreach (var (inputRef, configRO, commandBuffer, entity) in SystemAPI
                .Query<RefRW<RuntimeTimeControlInputState>, RefRO<TimeControlConfig>, DynamicBuffer<TimeControlCommand>>()
                .WithAll<TimeControlSingletonTag>()
                .WithEntityAccess())
            {
                var input = inputRef.ValueRO;
                var config = configRO.ValueRO;

                if (input.PauseToggleTriggered != 0)
                {
                    bool currentlyPaused = timeStateRO.IsPaused;
                    commandBuffer.Add(new TimeControlCommand
                    {
                        Type = currentlyPaused ? TimeControlCommandType.Resume : TimeControlCommandType.Pause
                    });
                }

                if (input.StepDownTriggered != 0)
                {
                    // When paused, treat as a single step; otherwise drop into slow-motion.
                    commandBuffer.Add(new TimeControlCommand
                    {
                        Type = timeStateRO.IsPaused
                            ? TimeControlCommandType.StepTicks
                            : TimeControlCommandType.SetSpeed,
                        FloatParam = math.max(0.1f, config.SlowMotionSpeed),
                        UintParam = 1
                    });
                }

                if (input.StepUpTriggered != 0)
                {
                    commandBuffer.Add(new TimeControlCommand
                    {
                        Type = timeStateRO.IsPaused
                            ? TimeControlCommandType.StepTicks
                            : TimeControlCommandType.SetSpeed,
                        FloatParam = math.max(0.1f, config.FastForwardSpeed),
                        UintParam = 1
                    });
                }

                if (input.RewindPressedThisFrame != 0)
                {
                    uint currentTick = timeStateRO.Tick;
                    uint depthTicks = (uint)math.max(1f, math.round(3f / math.max(0.0001f, timeStateRO.FixedDeltaTime)));
                    uint targetTick = currentTick > depthTicks ? currentTick - depthTicks : 0u;

                    commandBuffer.Add(new TimeControlCommand
                    {
                        Type = TimeControlCommandType.StartRewind,
                        UintParam = targetTick
                    });
                }

                if (input.EnterGhostPreview != 0 && input.RewindSpeedLevel > 0)
                {
                    commandBuffer.Add(new TimeControlCommand
                    {
                        Type = TimeControlCommandType.StopRewind
                    });
                }

                // Reset one-shot fields but keep speed level if key remains held
                inputRef.ValueRW = new RuntimeTimeControlInputState
                {
                    SampleTick = input.SampleTick,
                    RewindSpeedLevel = input.RewindHeld != 0 ? input.RewindSpeedLevel : (byte)0,
                    RewindHeld = input.RewindHeld
                };
            }
        }
    }
}
