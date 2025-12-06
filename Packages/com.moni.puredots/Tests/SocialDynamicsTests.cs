using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.AI.Social;
using PureDOTS.Runtime.AI.Social.Systems;
using PureDOTS.Shared;

namespace PureDOTS.Tests
{
    /// <summary>
    /// Tests for social dynamics systems.
    /// Validates trust networks, cooperation, and cultural propagation.
    /// </summary>
    public class SocialDynamicsTests
    {
        [Test]
        public void TrustUpdateRule_CalculatesCorrectly()
        {
            // Test trust update formula: Trust = Trust + (InteractionOutcome - ExpectedOutcome) * LearningRate
            float currentTrust = 0.5f;
            float interactionOutcome = 1.0f; // Successful interaction
            float expectedOutcome = 0.6f;
            float learningRate = 0.1f;

            float newTrust = CooperationResolutionSystem.UpdateTrust(
                currentTrust,
                interactionOutcome,
                expectedOutcome,
                learningRate);

            float expectedTrust = 0.5f + (1.0f - 0.6f) * 0.1f; // 0.5 + 0.04 = 0.54
            Assert.AreEqual(expectedTrust, newTrust, 0.001f);
        }

        [Test]
        public void IndirectTrust_PropagatesCorrectly()
        {
            // Test indirect trust: if A trusts B and B trusts C, A inherits partial trust in C
            float trustAB = 0.8f;
            float trustBC = 0.7f;
            float propagationFactor = 0.5f;

            float indirectTrust = CooperationResolutionSystem.CalculateIndirectTrust(
                trustAB,
                trustBC,
                propagationFactor);

            float expectedTrust = 0.8f * 0.7f * 0.5f; // 0.28
            Assert.AreEqual(expectedTrust, indirectTrust, 0.001f);
        }

        [Test]
        public void MutualUtility_CalculatesCorrectly()
        {
            // Test mutual utility calculation
            float senderUtility = 0.5f;
            float receiverUtility = 0.4f;
            float threshold = 0.3f;

            bool shouldCooperate = CooperationResolutionSystem.CalculateMutualUtility(
                senderUtility,
                receiverUtility,
                threshold,
                out float mutualGain);

            Assert.IsTrue(shouldCooperate);
            Assert.AreEqual(0.9f, mutualGain, 0.001f); // 0.5 + 0.4
        }

        [Test]
        public void EffectiveCooperation_ModulatesByPersonality()
        {
            // Test: EffectiveCoop = Group.CooperationWeight * Personality.Altruism
            float groupCooperationWeight = 0.7f;
            float personalityAltruism = 0.8f;

            float effectiveCoop = CooperationResolutionSystem.CalculateEffectiveCooperation(
                groupCooperationWeight,
                personalityAltruism);

            float expected = 0.7f * 0.8f; // 0.56
            Assert.AreEqual(expected, effectiveCoop, 0.001f);
        }

        [Test]
        public void CombinedUtility_MaximizesCorrectly()
        {
            // Test: PersonalUtility + WeightedGroupUtility
            float personalUtility = 0.6f;
            float groupUtility = 0.5f;
            float groupWeight = 0.3f;

            float combinedUtility = CooperationResolutionSystem.CalculateCombinedUtility(
                personalUtility,
                groupUtility,
                groupWeight);

            float expected = 0.6f + (0.5f * 0.3f); // 0.6 + 0.15 = 0.75
            Assert.AreEqual(expected, combinedUtility, 0.001f);
        }
    }
}

