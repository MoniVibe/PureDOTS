using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Combat;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityPhysicsVelocity = Unity.Physics.PhysicsVelocity;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Applies anime-style rebound based on stamina ratio.
    /// ReboundSystem applies counter-impulse based on stamina ratio.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(ParryReactionSystem))]
    public partial struct ReboundSystem : ISystem
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

            var job = new ApplyReboundJob
            {
                DeltaTime = timeState.FixedDeltaTime
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ApplyReboundJob : IJobEntity
        {
            public float DeltaTime;

            void Execute(
                ref UnityPhysicsVelocity velocity,
                in StaminaState stamina,
                in MotionReactionState reactionState)
            {
                // Apply rebound when entity has high stamina and is reacting
                if (reactionState.ReactionSkill > 0.7f)
                {
                    float staminaRatio = stamina.Current / math.max(stamina.Max, 0.01f);
                    
                    // High stamina = stronger rebound
                    if (staminaRatio > 0.8f)
                    {
                        // Apply upward/forward rebound impulse
                        float3 reboundDirection = new float3(0f, 1f, 1f);
                        float reboundStrength = staminaRatio * 10f;
                        velocity.Linear += reboundDirection * reboundStrength * DeltaTime;
                    }
                }
            }
        }
    }
}

