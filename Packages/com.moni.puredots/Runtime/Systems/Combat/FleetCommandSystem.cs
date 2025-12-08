using PureDOTS.Runtime.Groups;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Tracks tactic success vs culture, updates weights via lerp(weight, observedEffectiveness, LearnRate).
    /// Leaders run meta-behavior graphs operating on subordinate fleets.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(LearningDecaySystem))]
    public partial struct FleetCommandSystem : ISystem
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

            var job = new UpdateFleetTacticsJob
            {
                CurrentTick = timeState.Tick
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        partial struct UpdateFleetTacticsJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                in LeaderTag leaderTag,
                in FleetCommandState commandState,
                in GroupMeta groupMeta)
            {
                // Maintain success probability per tactic vs. culture
                // In full implementation, would:
                // 1. Track tactic effectiveness vs different cultures
                // 2. Update weights: tacticWeight[culture] = lerp(weight, observedEffectiveness, LearnRate)
                // 3. Bias fleet AI toward successful doctrines

                if (!commandState.Tactics.IsCreated)
                    return;

                // Simplified: would iterate through tactic blob and update weights based on observed outcomes
                // This requires tracking combat outcomes per culture, which would be in a separate system
            }
        }
    }
}

