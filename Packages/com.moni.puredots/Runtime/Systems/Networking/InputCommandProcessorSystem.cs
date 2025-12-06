using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Networking;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Networking
{
    /// <summary>
    /// Processes input commands by tick for deterministic lockstep simulation.
    /// Commands are stored and processed, not player state.
    /// Currently feeds from test scripts; later will serialize from network.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct InputCommandProcessorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            if (tickState.IsPaused)
            {
                return;
            }

            // Find input command queue singleton
            var query = SystemAPI.QueryBuilder()
                .WithAll<InputCommandQueueTag, InputCommandBuffer>()
                .Build();

            if (query.IsEmpty)
            {
                return;
            }

            var entity = query.GetSingletonEntity();
            var commandBuffer = SystemAPI.GetBuffer<InputCommandBuffer>(entity);
            var commandState = SystemAPI.GetComponentRW<InputCommandState>(entity);

            // Process commands for current tick (or delayed tick if input delay is configured)
            int targetTick = (int)tickState.Tick;
            
            // Check for input delay configuration
            if (SystemAPI.TryGetSingleton<InputDelayConfig>(out var delayConfig))
            {
                targetTick = (int)tickState.Tick - delayConfig.InputDelayTicks;
            }

            // Process all commands for the target tick
            var commandsToProcess = new NativeList<InputCommandBuffer>(Allocator.Temp);
            for (int i = 0; i < commandBuffer.Length; i++)
            {
                var cmd = commandBuffer[i];
                if (cmd.Tick == targetTick)
                {
                    commandsToProcess.Add(cmd);
                }
                else if (cmd.Tick < targetTick - 100) // Drop very old commands
                {
                    commandState.ValueRW.DroppedCommandCount++;
                }
            }

            // Process commands (currently no-op; systems will consume commands later)
            // In multiplayer, this is where commands would be applied to simulation
            commandState.ValueRW.LastProcessedTick = targetTick;
            commandState.ValueRW.CommandCount += commandsToProcess.Length;

            // Remove processed commands
            for (int i = commandBuffer.Length - 1; i >= 0; i--)
            {
                if (commandBuffer[i].Tick <= targetTick)
                {
                    commandBuffer.RemoveAt(i);
                }
            }

            commandsToProcess.Dispose();
        }
    }
}

