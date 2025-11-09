using PureDOTS.Input;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Input
{
    /// <summary>
    /// Converts player-facing time control input into deterministic commands consumed by <see cref="RewindCoordinatorSystem"/>.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CopyInputToEcsSystem))]
    public partial struct TimeControlInputSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeControlSingletonTag>();
            state.RequireForUpdate<TimeControlConfig>();
            state.RequireForUpdate<TimeControlInputState>();
            state.RequireForUpdate<TimeControlCommand>();
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeStateRO = SystemAPI.GetSingleton<TimeState>();

            foreach (var (inputRef, configRO, commandBuffer, entity) in SystemAPI
                .Query<RefRW<TimeControlInputState>, RefRO<TimeControlConfig>, DynamicBuffer<TimeControlCommand>>()
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
                        Type = currentlyPaused ? TimeControlCommand.CommandType.Resume : TimeControlCommand.CommandType.Pause
                    });
                }

                if (input.StepDownTriggered != 0)
                {
                    commandBuffer.Add(new TimeControlCommand
                    {
                        Type = TimeControlCommand.CommandType.SetSpeed,
                        FloatParam = math.max(0.1f, config.SlowMotionSpeed)
                    });
                }

                if (input.StepUpTriggered != 0)
                {
                    commandBuffer.Add(new TimeControlCommand
                    {
                        Type = TimeControlCommand.CommandType.SetSpeed,
                        FloatParam = math.max(0.1f, config.FastForwardSpeed)
                    });
                }

                if (input.RewindPressedThisFrame != 0)
                {
                    uint currentTick = timeStateRO.Tick;
                    uint depthTicks = (uint)math.clamp(input.RewindSpeedLevel * 120u, 60u, 480u);
                    uint targetTick = currentTick > depthTicks ? currentTick - depthTicks : 0u;

                    commandBuffer.Add(new TimeControlCommand
                    {
                        Type = TimeControlCommand.CommandType.StartRewind,
                        UintParam = targetTick
                    });
                }

                if (input.EnterGhostPreview != 0 && input.RewindSpeedLevel > 0)
                {
                    commandBuffer.Add(new TimeControlCommand
                    {
                        Type = TimeControlCommand.CommandType.StopRewind
                    });
                }

                // Reset one-shot fields but keep speed level if key remains held
                inputRef.ValueRW = new TimeControlInputState
                {
                    SampleTick = input.SampleTick,
                    RewindSpeedLevel = input.RewindHeld != 0 ? input.RewindSpeedLevel : (byte)0,
                    RewindHeld = input.RewindHeld
                };
            }
        }
    }
}
