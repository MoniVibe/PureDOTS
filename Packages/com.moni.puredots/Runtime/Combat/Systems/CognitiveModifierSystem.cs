using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Computes modifiers from CognitiveStats, applies to behavior costs and learning rates.
    /// Wisdom affects learning speed & focus regen, Finesse affects multi-action precision, Phys affects mass/stamina.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(SkillEfficiencySystem))]
    public partial struct CognitiveModifierSystem : ISystem
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

            var job = new ComputeCognitiveModifiersJob();
            job.ScheduleParallel();
        }

        [BurstCompile]
        partial struct ComputeCognitiveModifiersJob : IJobEntity
        {
            void Execute(
                in CognitiveStats stats,
                ref BehaviorModifier modifier)
            {
                // Normalize stats (assuming 0-10 range, map to 0-1)
                float wisdom = math.clamp(stats.Wisdom / 10f, 0f, 1f);
                float finesse = math.clamp(stats.Finesse / 10f, 0f, 1f);
                float physique = math.clamp(stats.Physique / 10f, 0f, 1f);

                // Wisdom: learning speed, focus regen (applied via modifier)
                modifier.LearningRateMultiplier = 1f + (wisdom * 0.3f); // Up to 30% faster learning

                // Finesse: multi-action precision, ambidexterity, reduces focus cost for dual actions
                modifier.FocusCostMultiplier = 1f - (finesse * 0.15f); // Up to 15% reduction
                
                // Physique: mass, stamina, inertia
                modifier.StaminaCostMultiplier = 1f - (physique * 0.1f); // Up to 10% reduction
            }
        }
    }
}

