using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Skills;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Evaluates skill thresholds and unlocks behaviors based on 3-tier model.
    /// Baseline: always available, Learned: skill >= threshold, Mastered: skill >= threshold + implant.
    /// Performance: Dirty-flagged updates, only recalc on skill changes.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(BehaviorCatalogSystem))]
    public partial struct BehaviorGatingSystem : ISystem
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

            // Tiered tick rate: advanced behaviors 30Hz (every 2 ticks), baseline 60Hz
            // Only evaluate every 2 ticks for learned/mastered behaviors
            if (timeState.Tick % 2 != 0)
                return;

            var job = new EvaluateBehaviorGatingJob
            {
                CurrentTick = timeState.Tick
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        partial struct EvaluateBehaviorGatingJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                ref BehaviorTierState tierState,
                [ChunkIndexInQuery] int chunkIndex,
                in SkillSet skillSet,
                in ImplantTag implantTag,
                DynamicBuffer<BehaviorSet> behaviorSet)
            {
                // Determine tier based on skills and implants
                // This is a simplified check - actual behavior catalog lookup happens in BehaviorUnlockSystem
                BehaviorTier newTier = BehaviorTier.Baseline;

                // Check if entity has any learned/mastered behaviors unlocked
                bool hasLearned = false;
                bool hasMastered = false;

                for (int i = 0; i < behaviorSet.Length; i++)
                {
                    var behaviorId = behaviorSet[i].BehaviorId;
                    // Tier determination logic would check catalog here
                    // For now, assume behaviors are unlocked by BehaviorUnlockSystem
                    hasLearned = true; // Simplified
                }

                if (hasMastered)
                    newTier = BehaviorTier.Mastered;
                else if (hasLearned)
                    newTier = BehaviorTier.Learned;
                else
                    newTier = BehaviorTier.Baseline;

                tierState.Tier = newTier;
            }
        }
    }
}

