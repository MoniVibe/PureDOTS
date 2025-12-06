using PureDOTS.Runtime.Skills;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Applies efficiency modifiers FocusCost * (1 - Skill) based on skill level.
    /// Learning upgrades efficiency coefficients, creating visible "ease" for veterans.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(BehaviorUnlockSystem))]
    public partial struct SkillEfficiencySystem : ISystem
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

            var job = new ApplySkillEfficiencyJob();
            job.ScheduleParallel();
        }

        [BurstCompile]
        partial struct ApplySkillEfficiencyJob : IJobEntity
        {
            void Execute(
                in SkillSet skillSet,
                ref BehaviorModifier modifier)
            {
                // Calculate efficiency based on max skill level
                byte maxLevel = skillSet.GetMaxLevel();
                float skillNormalized = math.clamp(maxLevel / 100f, 0f, 1f); // Normalize to 0-1

                // Efficiency reduces costs: FocusCost * (1 - Skill)
                // Higher skill = lower cost multiplier
                float efficiencyMultiplier = 1f - (skillNormalized * 0.5f); // Max 50% reduction at skill 100

                modifier.FocusCostMultiplier = efficiencyMultiplier;
                modifier.StaminaCostMultiplier = efficiencyMultiplier;

                // Learning rate increases with skill (wisdom affects this too, handled in CognitiveModifierSystem)
                modifier.LearningRateMultiplier = 1f + (skillNormalized * 0.3f); // Up to 30% faster learning
            }
        }
    }
}

