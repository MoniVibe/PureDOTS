using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;
using PureDOTS.Runtime.AI.Social;

namespace PureDOTS.Runtime.AI.Social.Systems
{
    /// <summary>
    /// Burst-compiled utility functions for deterministic cooperation resolution.
    /// Calculates utility gains and interaction outcomes.
    /// </summary>
    [BurstCompile]
    public static class CooperationResolutionSystem
    {
        /// <summary>
        /// Calculates mutual utility gain for a cooperative interaction.
        /// Returns true if mutual profit exceeds threshold.
        /// </summary>
        [BurstCompile]
        public static bool CalculateMutualUtility(
            float senderUtility,
            float receiverUtility,
            float cooperationThreshold,
            out float mutualGain)
        {
            mutualGain = senderUtility + receiverUtility;
            return mutualGain > cooperationThreshold;
        }

        /// <summary>
        /// Calculates expected outcome based on trust and previous interactions.
        /// Used for trust update calculations.
        /// </summary>
        [BurstCompile]
        public static float CalculateExpectedOutcome(
            float trust,
            float baseSuccessRate,
            float interactionHistory)
        {
            // Expected outcome scales with trust and interaction history
            return math.lerp(baseSuccessRate, trust, interactionHistory);
        }

        /// <summary>
        /// Updates trust value based on interaction outcome.
        /// Formula: Trust = Trust + (InteractionOutcome - ExpectedOutcome) * LearningRate
        /// </summary>
        [BurstCompile]
        public static float UpdateTrust(
            float currentTrust,
            float interactionOutcome,
            float expectedOutcome,
            float learningRate)
        {
            var trustDelta = (interactionOutcome - expectedOutcome) * learningRate;
            return math.clamp(currentTrust + trustDelta, 0f, 1f);
        }

        /// <summary>
        /// Calculates indirect trust propagation.
        /// If A trusts B and B trusts C, A inherits partial trust in C.
        /// </summary>
        [BurstCompile]
        public static float CalculateIndirectTrust(
            float directTrustAB,
            float directTrustBC,
            float propagationFactor)
        {
            // Indirect trust is product of direct trusts, scaled by propagation factor
            return math.clamp(directTrustAB * directTrustBC * propagationFactor, 0f, 1f);
        }

        /// <summary>
        /// Determines if an offer should be accepted based on utility comparison.
        /// </summary>
        [BurstCompile]
        public static bool ShouldAcceptOffer(
            float offerValue,
            float counterOfferValue,
            float minimumAcceptableValue)
        {
            var bestValue = math.max(offerValue, counterOfferValue);
            return bestValue >= minimumAcceptableValue;
        }

        /// <summary>
        /// Calculates cooperation bias based on group goals and personal traits.
        /// EffectiveCoop = Group.CooperationWeight * Personality.Altruism
        /// </summary>
        [BurstCompile]
        public static float CalculateEffectiveCooperation(
            float groupCooperationWeight,
            float personalityAltruism)
        {
            return groupCooperationWeight * personalityAltruism;
        }

        /// <summary>
        /// Calculates competition bias based on group goals and personal traits.
        /// EffectiveComp = Group.CompetitionWeight * Personality.Ambition
        /// </summary>
        [BurstCompile]
        public static float CalculateEffectiveCompetition(
            float groupCompetitionWeight,
            float personalityAmbition)
        {
            return groupCompetitionWeight * personalityAmbition;
        }

        /// <summary>
        /// Calculates combined utility: PersonalUtility + WeightedGroupUtility
        /// </summary>
        [BurstCompile]
        public static float CalculateCombinedUtility(
            float personalUtility,
            float groupUtility,
            float groupWeight)
        {
            return personalUtility + (groupUtility * groupWeight);
        }
}

