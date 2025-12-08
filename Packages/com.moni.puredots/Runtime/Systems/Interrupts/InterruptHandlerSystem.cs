using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Interrupts;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Interrupts
{
    /// <summary>
    /// Processes interrupts and writes EntityIntent for behavior systems.
    /// Picks highest-priority interrupt and converts to intent.
    /// Runs after perception/combat/group logic, before AI/GOAP systems.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InterruptSystemGroup))]
    public partial struct InterruptHandlerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var interruptBufferLookup = SystemAPI.GetBufferLookup<Interrupt>(false);
            interruptBufferLookup.Update(ref state);

            // Process interrupts for all entities with interrupt buffers
            foreach (var (intent, entity) in
                SystemAPI.Query<RefRW<EntityIntent>>()
                .WithEntityAccess())
            {
                if (!interruptBufferLookup.HasBuffer(entity))
                {
                    continue;
                }

                var interruptBuffer = interruptBufferLookup[entity];
                
                if (interruptBuffer.Length == 0)
                {
                    // No interrupts, clear intent if invalid
                    if (intent.ValueRO.IsValid == 0)
                    {
                        continue; // Already cleared
                    }
                    // Intent may still be valid from previous tick
                    continue;
                }

                // Find highest priority unprocessed interrupt
                Interrupt bestInterrupt = default;
                int bestIndex = -1;
                InterruptPriority bestPriority = InterruptPriority.Low;

                for (int i = 0; i < interruptBuffer.Length; i++)
                {
                    var interrupt = interruptBuffer[i];
                    if (interrupt.IsProcessed != 0)
                    {
                        continue;
                    }

                    // Compare priority (higher byte value = higher priority)
                    if ((byte)interrupt.Priority > (byte)bestPriority)
                    {
                        bestInterrupt = interrupt;
                        bestIndex = i;
                        bestPriority = interrupt.Priority;
                    }
                }

                // If found valid interrupt, convert to intent
                if (bestIndex >= 0)
                {
                    ConvertInterruptToIntent(bestInterrupt, timeState.Tick, out var newIntent);

                    // Update intent
                    intent.ValueRW = newIntent;

                    // Mark interrupt as processed
                    bestInterrupt.IsProcessed = 1;
                    interruptBuffer[bestIndex] = bestInterrupt;

                    // Clear processed interrupts older than N ticks (cleanup)
                    CleanupProcessedInterrupts(ref interruptBuffer, timeState.Tick, 300); // Keep for 5 seconds at 60fps
                }
                else
                {
                    // No valid interrupts, but keep existing intent if still valid
                    // Intent will be cleared by behavior systems when completed
                }
            }
        }

        /// <summary>
        /// Converts an interrupt to an EntityIntent.
        /// Phase 1: Simple mapping.
        /// Phase 2: More sophisticated intent generation based on context.
        /// </summary>
        [BurstCompile]
        private static void ConvertInterruptToIntent(in Interrupt interrupt, uint currentTick, out EntityIntent intent)
        {
            intent = new EntityIntent
            {
                TriggeringInterrupt = interrupt.Type,
                Priority = interrupt.Priority,
                IntentSetTick = currentTick,
                TargetEntity = interrupt.TargetEntity,
                TargetPosition = interrupt.TargetPosition,
                IsValid = 1
            };

            // Map interrupt type to intent mode
            intent.Mode = interrupt.Type switch
            {
                InterruptType.UnderAttack => IntentMode.Attack,
                InterruptType.TookDamage => IntentMode.Flee, // Default: flee when damaged
                InterruptType.LostTarget => IntentMode.Idle,
                InterruptType.TargetDestroyed => IntentMode.Idle,
                InterruptType.NewThreatDetected => IntentMode.Attack,
                InterruptType.LostThreat => IntentMode.Idle,
                InterruptType.AllyInDanger => IntentMode.Defend,
                InterruptType.ResourceSpotted => IntentMode.Gather,
                InterruptType.ObjectiveSpotted => IntentMode.MoveTo,
                InterruptType.NewOrder => IntentMode.ExecuteOrder,
                InterruptType.OrderCancelled => IntentMode.Idle,
                InterruptType.ObjectiveChanged => IntentMode.ExecuteOrder,
                InterruptType.LowHealth => IntentMode.Flee,
                InterruptType.LowResources => IntentMode.Gather,
                InterruptType.AbilityReady => IntentMode.UseAbility,
                _ => IntentMode.Idle
            };
        }

        /// <summary>
        /// Removes processed interrupts older than specified age.
        /// </summary>
        [BurstCompile]
        private static void CleanupProcessedInterrupts(ref DynamicBuffer<Interrupt> buffer, uint currentTick, uint maxAgeTicks)
        {
            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                var interrupt = buffer[i];
                if (interrupt.IsProcessed != 0)
                {
                    var age = currentTick - interrupt.Timestamp;
                    if (age > maxAgeTicks)
                    {
                        buffer.RemoveAt(i);
                    }
                }
            }
        }
    }
}

