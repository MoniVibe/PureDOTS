using PureDOTS.Systems;
using PureDOTS.Runtime.Combat;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityPhysicsVelocity = Unity.Physics.PhysicsVelocity;
using Unity.Transforms;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Converts damage to stamina drain, applies counter-impulse based on stamina ratio.
    /// Parry converts damage to stamina drain.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(ImpulseReactionSystem))]
    public partial struct ParryReactionSystem : ISystem
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

            var job = new ProcessParriesJob
            {
                CurrentTick = timeState.Tick
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessParriesJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                ref StaminaState stamina,
                ref UnityPhysicsVelocity velocity,
                in MotionReactionState reactionState,
                DynamicBuffer<HitBuffer> incomingHits)
            {
                // Check for parry opportunities
                for (int i = incomingHits.Length - 1; i >= 0; i--)
                {
                    var hit = incomingHits[i];
                    
                    // Simplified: if entity has active parry action, convert damage to stamina drain
                    // In full implementation, would check ActionComposition buffer for active Parry action
                    bool isParrying = reactionState.ReactionSkill > 0.5f; // Simplified check

                    if (isParrying)
                    {
                        // Convert damage to stamina drain
                        float reactionCost = hit.Damage * 0.5f; // 50% of damage becomes stamina cost
                        stamina.Current = math.max(0f, stamina.Current - reactionCost);

                        // Apply counter-impulse based on stamina ratio
                        float staminaRatio = stamina.Current / math.max(stamina.Max, 0.01f);
                        float3 counterImpulse = -hit.HitPoint * staminaRatio * 5f; // Simplified direction
                        velocity.Linear += counterImpulse;

                        // Remove hit (parried)
                        incomingHits.RemoveAt(i);
                    }
                }
            }
        }
    }
}

