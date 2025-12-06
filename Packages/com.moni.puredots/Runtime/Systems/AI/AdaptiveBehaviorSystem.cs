using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Cognitive;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Systems.AI
{
    /// <summary>
    /// Adaptive behavior system for emergent behavior cascades.
    /// Ambush victims avoid known ambushers (memory-based pathfinding).
    /// Species reputations form naturally (aggregate from individual experiences).
    /// Prejudices and alliances emerge statistically (culture belief propagation).
    /// Cultural doctrines adapt per war (meta-learning updates).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(AIUtilityScoringSystem))]
    public partial struct AdaptiveBehaviorSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            // Process entities with memory and AI components
            foreach (var (utilityState, memoryProfile, emotionModulator, grudgeBuffer) in SystemAPI.Query<RefRW<AIUtilityState>, RefRO<MemoryProfile>, RefRO<EmotionModulator>, DynamicBuffer<GrudgeEntry>>())
            {
                // Apply memory-based modifiers to utility scoring
                ApplyMemoryModifiers(ref utilityState, memoryProfile, emotionModulator, grudgeBuffer);
            }

            // Process entities with culture beliefs for reputation-based behavior
            foreach (var (utilityState, cultureBeliefBuffer) in SystemAPI.Query<RefRW<AIUtilityState>, DynamicBuffer<CultureBelief>>())
            {
                // Apply culture-based reputation modifiers
                ApplyReputationModifiers(ref utilityState, cultureBeliefBuffer);
            }
        }

        [BurstCompile]
        private static void ApplyMemoryModifiers(
            ref RefRW<AIUtilityState> utilityState,
            in MemoryProfile memoryProfile,
            in EmotionModulator emotionModulator,
            in DynamicBuffer<GrudgeEntry> grudgeBuffer)
        {
            // Memory-based modifiers affect utility scoring
            // Entities with grudges avoid or attack specific cultures

            var baseScore = utilityState.ValueRO.BestScore;

            // Apply emotion-based confidence modifier
            var confidenceModifier = emotionModulator.ValueRO.ConfidenceModifier;
            var adjustedScore = baseScore * (1f + confidenceModifier * 0.2f); // Max 20% modifier

            // Apply grudge-based avoidance/aggression
            float grudgeModifier = 0f;
            for (int i = 0; i < grudgeBuffer.Length; i++)
            {
                var grudge = grudgeBuffer[i];
                // High grudge = avoid or attack (would need target culture ID)
                grudgeModifier += grudge.GrudgeValue * 0.1f; // 10% modifier per grudge
            }

            adjustedScore += grudgeModifier;

            // Update utility state
            var state = utilityState.ValueRO;
            state.BestScore = math.clamp(adjustedScore, 0f, 1f);
            utilityState.ValueRW = state;
        }

        [BurstCompile]
        private static void ApplyReputationModifiers(
            ref RefRW<AIUtilityState> utilityState,
            in DynamicBuffer<CultureBelief> cultureBeliefBuffer)
        {
            // Culture-based reputation affects sensor confidence and target selection
            // Low reputation = higher suspicion, higher attack priority

            var baseScore = utilityState.ValueRO.BestScore;

            float reputationModifier = 0f;
            for (int i = 0; i < cultureBeliefBuffer.Length; i++)
            {
                var belief = cultureBeliefBuffer[i];
                // Low belief value (low trustworthiness) = higher aggression
                var trustModifier = (1f - belief.BeliefValue) * belief.Confidence * 0.15f; // Max 15% modifier
                reputationModifier += trustModifier;
            }

            var adjustedScore = baseScore + reputationModifier;

            // Update utility state
            var state = utilityState.ValueRO;
            state.BestScore = math.clamp(adjustedScore, 0f, 1f);
            utilityState.ValueRW = state;
        }
    }
}

