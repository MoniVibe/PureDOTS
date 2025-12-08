using PureDOTS.Systems;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Combat;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityPhysicsVelocity = Unity.Physics.PhysicsVelocity;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Processes ImpulseEvent buffer, computes resulting velocity via physics.
    /// Model dynamic reactions like "assassin rides knockback" deterministically.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(CombatExecutionSystem))]
    public partial struct ImpulseReactionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            if (timeState.IsPaused)
                return;

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
                return;

            var job = new ProcessImpulsesJob
            {
                CurrentTick = timeState.Tick,
                DeltaTime = timeState.FixedDeltaTime
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessImpulsesJob : IJobEntity
        {
            public uint CurrentTick;
            public float DeltaTime;

            void Execute(
                ref UnityPhysicsVelocity velocity,
                in MotionReactionState reactionState,
                DynamicBuffer<ImpulseEvent> impulses)
            {
                // Process all impulse events
                for (int i = impulses.Length - 1; i >= 0; i--)
                {
                    var impulse = impulses[i];
                    
                    // Only process recent impulses (within last tick)
                    if (CurrentTick - impulse.Tick > 1)
                    {
                        impulses.RemoveAt(i);
                        continue;
                    }

                    // Apply impulse force to velocity
                    // If ReactionSkill > threshold, allow mid-air behavior selection
                    float reactionMultiplier = 1f;
                    if (reactionState.ReactionSkill > 0.7f && reactionState.CanMidAirParry)
                    {
                        // Skilled entities can redirect impulses
                        reactionMultiplier = 0.5f; // Reduce knockback
                    }

                    velocity.Linear += impulse.Force * reactionMultiplier * DeltaTime;
                    
                    // Remove processed impulse
                    impulses.RemoveAt(i);
                }
            }
        }
    }
}

