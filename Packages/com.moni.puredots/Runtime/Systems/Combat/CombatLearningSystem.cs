using PureDOTS.Systems;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Combat;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Tracks hit/miss, block/fail rates, adjusts BehaviorNode.Weight via baseWeight * (1 + (successRate - 0.5f) * LearningRate).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(MultiTargetCombatSystem))]
    public partial struct CombatLearningSystem : ISystem
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

            var job = new UpdateLearningJob
            {
                CurrentTick = timeState.Tick
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct UpdateLearningJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                in CombatLearningState learningState,
                in BehaviorModifier modifier,
                DynamicBuffer<BehaviorSuccessRate> successRates,
                DynamicBuffer<HitBuffer> recentHits)
            {
                // Update success rates based on recent hits
                for (int i = 0; i < successRates.Length; i++)
                {
                    var rate = successRates[i];
                    
                    // Calculate success rate
                    float successRate = 0f;
                    if (rate.AttemptCount > 0)
                    {
                        successRate = (float)rate.SuccessCount / (float)rate.AttemptCount;
                    }

                    // Adjust weight: baseWeight * (1 + (successRate - 0.5f) * LearningRate)
                    float adjustedLearningRate = learningState.LearningRate * modifier.LearningRateMultiplier;
                    float weightAdjustment = (successRate - 0.5f) * adjustedLearningRate;
                    rate.Weight = math.max(0.1f, rate.Weight * (1f + weightAdjustment));

                    successRates[i] = rate;
                }

                // Track new attempts from recent hits
                // Simplified: would match hits to behavior IDs
                for (int i = 0; i < recentHits.Length; i++)
                {
                    var hit = recentHits[i];
                    // In full implementation, would determine which behavior caused this hit
                    // and update corresponding BehaviorSuccessRate entry
                }
            }
        }
    }
}

