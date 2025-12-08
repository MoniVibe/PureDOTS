using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Interrupts;
using PureDOTS.Runtime.Perception;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Perception
{
    /// <summary>
    /// Bridges PerceptionState to Interrupts.
    /// Emits interrupts when perception detects new threats or loses threats.
    /// Phase 1: Basic threat detection interrupts.
    /// Phase 2: Extended with more perception-based interrupts.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PerceptionSystemGroup))]
    [UpdateAfter(typeof(PerceptionUpdateSystem))]
    public partial struct PerceptionToInterruptBridgeSystem : ISystem
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

            // Process entities with perception state
            foreach (var (perceptionState, perceivedBuffer, entity) in
                SystemAPI.Query<RefRO<PerceptionState>, DynamicBuffer<PerceivedEntity>>()
                .WithEntityAccess())
            {
                // Ensure entity has interrupt buffer
                if (!SystemAPI.HasBuffer<Interrupt>(entity))
                {
                    state.EntityManager.AddBuffer<Interrupt>(entity);
                }

                var interruptBuffer = SystemAPI.GetBuffer<Interrupt>(entity);

                // Check for new threats
                if (perceptionState.ValueRO.HighestThreatEntity != Entity.Null)
                {
                    // Check if we already have a NewThreatDetected interrupt for this entity
                    bool hasThreatInterrupt = false;
                    for (int i = 0; i < interruptBuffer.Length; i++)
                    {
                        var interrupt = interruptBuffer[i];
                        if (interrupt.Type == InterruptType.NewThreatDetected &&
                            interrupt.TargetEntity == perceptionState.ValueRO.HighestThreatEntity &&
                            interrupt.IsProcessed == 0)
                        {
                            hasThreatInterrupt = true;
                            break;
                        }
                    }

                    if (!hasThreatInterrupt && perceptionState.ValueRO.HighestThreat > 100)
                    {
                        // Emit new threat interrupt
                        InterruptUtils.EmitPerception(
                            ref interruptBuffer,
                            InterruptType.NewThreatDetected,
                            entity,
                            perceptionState.ValueRO.HighestThreatEntity,
                            float3.zero, // TODO: Get position from perceived buffer
                            timeState.Tick,
                            InterruptPriority.High);
                    }
                }

                // Check for lost threats (Phase 1: simple check)
                // TODO Phase 2: Track previous highest threat and detect when it's lost
            }
        }
    }
}

