using Space4X.Combat;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;

namespace Space4X.Systems
{
    /// <summary>
    /// Processes detected dangers and triggers appropriate responses based on DangerResponseFlags.
    /// Unlocked responses expand as entity levels up (behavior tree depth).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(DangerDetectionSystem))]
    public partial struct DangerResponseSystem : ISystem
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
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var currentTick = timeState.Tick;
            var deltaTime = timeState.DeltaTime;

            var ecbSingleton = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // Process danger responses
            new ProcessDangerResponsesJob
            {
                CurrentTick = currentTick,
                DeltaTime = deltaTime,
                Ecb = ecb
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessDangerResponsesJob : IJobEntity
        {
            public uint CurrentTick;
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                in DangerPerception perception,
                in DynamicBuffer<DetectedDanger> detectedDangers,
                in DynamicBuffer<DangerAlert> alerts)
            {
                // Process detected dangers
                for (int i = 0; i < detectedDangers.Length; i++)
                {
                    var danger = detectedDangers[i];

                    // Check if we have time to react
                    if (danger.TimeToImpact < perception.ReactionTime)
                    {
                        continue; // Too late to react
                    }

                    // Determine appropriate response based on enabled flags
                    if ((perception.EnabledResponses & DangerResponseFlags.Evade) != 0)
                    {
                        // TODO: Trigger evasion movement (would integrate with movement system)
                        // Example: Add EvadeRequest component or modify movement target
                    }

                    if ((perception.EnabledResponses & DangerResponseFlags.Shield) != 0)
                    {
                        // TODO: Activate shields (would integrate with shield system)
                        // Example: Add ShieldActivationRequest component
                    }

                    if ((perception.EnabledResponses & DangerResponseFlags.CounterMeasures) != 0)
                    {
                        // TODO: Deploy countermeasures (would integrate with weapon/countermeasure system)
                        // Example: Add CounterMeasureRequest component
                    }

                    if ((perception.EnabledResponses & DangerResponseFlags.Intercept) != 0)
                    {
                        // TODO: Attempt to shoot down projectile (requires high tier)
                        // Example: Add InterceptRequest component with target danger entity
                    }

                    if ((perception.EnabledResponses & DangerResponseFlags.Teleport) != 0)
                    {
                        // TODO: Emergency jump/teleport (requires high tier and capability)
                        // Example: Add TeleportRequest component
                    }
                }

                // Process alerts from squad leaders
                for (int i = 0; i < alerts.Length; i++)
                {
                    var alert = alerts[i];

                    // Treat alerts similarly to direct detections, but with reduced urgency
                    // (entity didn't directly perceive, so reaction may be slower)
                    // TODO: Process alert-based responses
                }
            }
        }
    }
}

