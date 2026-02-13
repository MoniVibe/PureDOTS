using NUnit.Framework;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Combat.Targeting;
using PureDOTS.Systems.Combat;

namespace PureDOTS.Tests.Combat
{
    public class CombatTargetProfileFallbacksTests
    {
        [Test]
        public void DoctrineFallback_ClampsNegativeWeights_AndUsesConfig()
        {
            var config = new TargetSelectionConfig
            {
                ThreatWeight = 0.7f,
                DistanceWeight = -0.3f,
                HealthWeight = 0.4f
            };

            var profile = CombatTargetProfileFallbacks.BuildDoctrineFallback(config);

            Assert.That(profile.ThreatWeight, Is.EqualTo(0.7f));
            Assert.That(profile.RangeWeight, Is.EqualTo(0f));
            Assert.That(profile.HealthWeight, Is.EqualTo(0.4f));
            Assert.That(profile.ObjectiveWeight, Is.EqualTo(0.2f));
            Assert.That(profile.FocusFireWeight, Is.EqualTo(0.2f));
            Assert.That(profile.RetreatHullThreshold, Is.EqualTo(0.3f));
        }

        [Test]
        public void IndividualFallback_FromCombatAI_IsDeterministic()
        {
            var combatAI = new CombatAI
            {
                Aggression = 30,
                FleeThresholdHP = 20
            };

            var first = CombatTargetProfileFallbacks.BuildIndividualFromCombatAI(combatAI);
            var second = CombatTargetProfileFallbacks.BuildIndividualFromCombatAI(combatAI);

            Assert.That(first.AggressionBias, Is.EqualTo(0.6f).Within(1e-6f));
            Assert.That(first.RiskTolerance, Is.EqualTo(0.8f).Within(1e-6f));
            Assert.That(first.AggressionBias, Is.EqualTo(second.AggressionBias).Within(1e-6f));
            Assert.That(first.RiskTolerance, Is.EqualTo(second.RiskTolerance).Within(1e-6f));
        }

        [Test]
        public void NeutralIndividualFallback_IsCentered()
        {
            var neutral = CombatTargetProfileFallbacks.BuildNeutralIndividualFallback();

            Assert.That(neutral.AggressionBias, Is.EqualTo(0f));
            Assert.That(neutral.ObjectiveBias, Is.EqualTo(0f));
            Assert.That(neutral.FinishOffBias, Is.EqualTo(0f));
            Assert.That(neutral.RangeBias, Is.EqualTo(0f));
            Assert.That(neutral.RiskTolerance, Is.EqualTo(0.5f));
        }
    }
}
