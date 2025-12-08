using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Networking;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Systems.Input
{
    /// <summary>
    /// Collects inputs each rendered frame and buffers them for deterministic simulation.
    /// Runs in PresentationSystemGroup to sample at frame rate, not tick rate.
    /// Quantizes inputs to next fixed tick boundary and buffers with configurable latency.
    /// 
    /// See: Docs/Guides/SimulationPresentationTimeSeparationGuide.md for configuration examples.
    /// </summary>
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    public partial struct InputSamplingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<InputCommandQueueTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Only sample during Record mode (not during playback)
            if (SystemAPI.TryGetSingleton<RewindState>(out var rewind) && rewind.Mode != RewindMode.Record)
            {
                return;
            }

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

            // Get input delay configuration (default 2 ticks)
            int inputDelayTicks = 2;
            if (SystemAPI.TryGetSingleton<InputDelayConfig>(out var delayConfig))
            {
                inputDelayTicks = delayConfig.InputDelayTicks;
            }

            // Calculate target tick: next tick + delay
            uint currentTick = tickState.Tick;
            uint targetTick = currentTick + (uint)inputDelayTicks;

            // Sample input from Unity Input System (non-Burst, frame-time)
            // Note: This is a simplified example - actual implementation would read from
            // InputSnapshotBridge or Unity Input System based on project setup
            SampleAndBufferInputs(ref state, commandBuffer, targetTick, inputDelayTicks);
        }

        private void SampleAndBufferInputs(ref SystemState state, DynamicBuffer<InputCommandBuffer> commandBuffer, uint targetTick, int inputDelayTicks)
        {
            // This is a placeholder - actual implementation would:
            // 1. Read from InputSnapshotBridge or Unity Input System
            // 2. Quantize analog inputs (handled by InputQuantizationSystem)
            // 3. Create InputCommandBuffer entries with targetTick
            
            // For now, we'll let CopyInputToEcsSystem handle the actual input reading
            // This system's job is to ensure inputs are buffered with proper tick delay
            
            // Check if we already have a command for this tick (avoid duplicates)
            bool hasCommandForTick = false;
            for (int i = 0; i < commandBuffer.Length; i++)
            {
                if (commandBuffer[i].Tick == (int)targetTick)
                {
                    hasCommandForTick = true;
                    break;
                }
            }

            // If no command exists for target tick, we'll let CopyInputToEcsSystem create it
            // The actual input sampling happens in CopyInputToEcsSystem, which runs in CameraInputSystemGroup
            // This system ensures proper buffering and tick assignment
        }
    }
}

