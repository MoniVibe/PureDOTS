using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Components.Events;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Systems.AI
{
    /// <summary>
    /// Damage event applied to a limb.
    /// </summary>
    public struct LimbDamageEvent : IBufferElementData
    {
        public float DamageAmount;
        public uint TickNumber;
    }

    /// <summary>
    /// Burst-compiled system that processes damage events and updates limb health.
    /// Event-driven: only processes limbs with changed LimbHealth or new damage events.
    /// Runs at fixed-step (60 Hz) for deterministic damage simulation.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    public partial struct LimbDamageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TickTimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            // Event-driven: only process entities with changed LimbHealth or damage events
            // Use change filter to skip entities where nothing changed
            var job = new ProcessLimbDamageJob
            {
                CurrentTick = timeState.Tick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct ProcessLimbDamageJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                Entity entity,
                ref LimbHealth health,
                DynamicBuffer<LimbDamageEvent> damageEvents)
            {
                // Only process if there are damage events
                if (damageEvents.IsEmpty)
                {
                    return;
                }

                // Process all damage events
                float totalDamage = 0f;
                for (int i = 0; i < damageEvents.Length; i++)
                {
                    var damageEvent = damageEvents[i];
                    // Only process recent damage events (within last 10 ticks)
                    if (CurrentTick - damageEvent.TickNumber <= 10)
                    {
                        totalDamage += damageEvent.DamageAmount;
                    }
                }

                // Apply damage
                if (totalDamage > 0f)
                {
                    health.Value = math.max(0f, health.Value - totalDamage);
                    
                    // Mark as destroyed if health reaches zero
                    if (health.Value <= 0f)
                    {
                        health.IsDestroyed = 1;
                    }
                }

                // Clear processed damage events
                damageEvents.Clear();
            }
        }
    }

    /// <summary>
    /// Burst-compiled system that processes limb activation commands from Mind ECS.
    /// Updates LimbCapability.Active based on commands.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(Bridges.MindToBodySyncSystem))]
    public partial struct CapabilityActivationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TickTimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var job = new ProcessLimbCommandsJob
            {
                CurrentTick = timeState.Tick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct ProcessLimbCommandsJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                Entity entity,
                ref LimbCapability capability,
                DynamicBuffer<LimbCommandBuffer> commands)
            {
                if (commands.IsEmpty)
                {
                    return;
                }

                // Process most recent command (last in buffer)
                var command = commands[commands.Length - 1];
                
                // Only process recent commands (within last 10 ticks)
                if (CurrentTick - command.TickNumber > 10)
                {
                    commands.Clear();
                    return;
                }

                // Apply command
                switch (command.Action)
                {
                    case LimbAction.Activate:
                        if (capability.Enabled == 1)
                        {
                            capability.Active = 1;
                        }
                        break;

                    case LimbAction.Deactivate:
                        capability.Active = 0;
                        break;

                    case LimbAction.Target:
                        // Target action - limb remains active but targets new position
                        if (capability.Enabled == 1)
                        {
                            capability.Active = 1;
                        }
                        break;

                    case LimbAction.Use:
                        // Use action - activate limb temporarily
                        if (capability.Enabled == 1)
                        {
                            capability.Active = 1;
                        }
                        break;

                    case LimbAction.None:
                    default:
                        // No action
                        break;
                }

                // Clear processed commands
                commands.Clear();
            }
        }
    }
}

