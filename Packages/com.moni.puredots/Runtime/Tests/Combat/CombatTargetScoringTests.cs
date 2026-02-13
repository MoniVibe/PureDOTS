using NUnit.Framework;
using PureDOTS.Systems.Combat;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Tests.Combat
{
    public class CombatTargetScoringTests
    {
        [Test]
        public void RankTargets_IsDeterministic_WithStableTieBreak()
        {
            var doctrine = new CombatDoctrineProfile
            {
                ThreatWeight = 0.8f,
                RangeWeight = 0.6f,
                HealthWeight = 0.5f,
                ObjectiveWeight = 0.3f,
                FocusFireWeight = 0.4f,
                EngageScoreThreshold = 0.2f,
                RetreatHullThreshold = 0.3f,
                RetreatRiskMultiplier = 1f
            };

            var individual = new CombatIndividualProfile
            {
                AggressionBias = 0.1f,
                ObjectiveBias = 0f,
                FinishOffBias = 0f,
                RangeBias = 0f,
                RiskTolerance = 0.5f
            };

            var self = new CombatSelfState
            {
                PreferredEngagementRange = 20f,
                NormalizedHull = 0.75f
            };

            using var candidates = new NativeArray<CombatTargetCandidate>(3, Allocator.Temp);
            candidates[0] = BuildCandidate(index: 7, distance: 20f, threat: 0.9f, hull: 0.4f, objective: 0.8f, focus: 0.6f);
            candidates[1] = BuildCandidate(index: 5, distance: 12f, threat: 0.5f, hull: 0.5f, objective: 0.2f, focus: 0.3f);
            candidates[2] = BuildCandidate(index: 2, distance: 12f, threat: 0.5f, hull: 0.5f, objective: 0.2f, focus: 0.3f);

            using var firstRun = new NativeList<CombatScoredTarget>(Allocator.Temp);
            using var secondRun = new NativeList<CombatScoredTarget>(Allocator.Temp);

            CombatTargetScoring.RankTargets(doctrine, individual, self, candidates, ref firstRun);
            CombatTargetScoring.RankTargets(doctrine, individual, self, candidates, ref secondRun);

            Assert.That(firstRun.Length, Is.EqualTo(3));
            Assert.That(secondRun.Length, Is.EqualTo(3));

            for (var i = 0; i < firstRun.Length; i++)
            {
                Assert.That(firstRun[i].Target.Index, Is.EqualTo(secondRun[i].Target.Index));
                Assert.That(firstRun[i].Score, Is.EqualTo(secondRun[i].Score).Within(1e-6f));
            }

            Assert.That(firstRun[0].Target.Index, Is.EqualTo(7));
            Assert.That(firstRun[1].Target.Index, Is.EqualTo(2));
            Assert.That(firstRun[2].Target.Index, Is.EqualTo(5));
        }

        [Test]
        public void EngagementDecision_FlipsWhenProfileWeightsChange()
        {
            var doctrine = new CombatDoctrineProfile
            {
                ThreatWeight = 0.9f,
                RangeWeight = 0.2f,
                HealthWeight = 0.6f,
                ObjectiveWeight = 0.3f,
                FocusFireWeight = 0.1f,
                EngageScoreThreshold = 0.7f,
                RetreatHullThreshold = 0.55f,
                RetreatRiskMultiplier = 1.2f
            };

            var cautious = new CombatIndividualProfile
            {
                AggressionBias = -0.3f,
                ObjectiveBias = 0f,
                FinishOffBias = 0.1f,
                RangeBias = 0f,
                RiskTolerance = 0.1f
            };

            var aggressive = cautious;
            aggressive.AggressionBias = 0.45f;
            aggressive.RiskTolerance = 0.95f;

            var self = new CombatSelfState
            {
                PreferredEngagementRange = 18f,
                NormalizedHull = 0.35f
            };

            var candidate = BuildCandidate(index: 11, distance: 16f, threat: 0.85f, hull: 0.6f, objective: 0.25f, focus: 0.2f);

            var cautiousScore = CombatTargetScoring.ScoreTarget(doctrine, cautious, self, candidate).Score;
            var aggressiveScore = CombatTargetScoring.ScoreTarget(doctrine, aggressive, self, candidate).Score;

            var cautiousDecision = CombatTargetScoring.DecideEngagement(doctrine, cautious, self, cautiousScore);
            var aggressiveDecision = CombatTargetScoring.DecideEngagement(doctrine, aggressive, self, aggressiveScore);

            Assert.That(cautiousDecision, Is.EqualTo(CombatEngagementDecision.Retreat));
            Assert.That(aggressiveDecision, Is.EqualTo(CombatEngagementDecision.Engage));
            Assert.That(aggressiveScore, Is.GreaterThan(cautiousScore));
        }

        private static CombatTargetCandidate BuildCandidate(
            int index,
            float distance,
            float threat,
            float hull,
            float objective,
            float focus)
        {
            return new CombatTargetCandidate
            {
                Target = new Entity { Index = index, Version = 1 },
                Distance = distance,
                Threat = threat,
                NormalizedHull = hull,
                ObjectivePriority = objective,
                FocusFireSupport = focus
            };
        }
    }
}

