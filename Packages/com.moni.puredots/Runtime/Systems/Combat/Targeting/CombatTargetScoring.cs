using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
{
    public enum CombatEngagementDecision : byte
    {
        Retreat = 0,
        Engage = 1
    }

    /// <summary>
    /// Situational target features sampled before scoring.
    /// Most values are expected in [0,1] and are clamped during scoring.
    /// </summary>
    public struct CombatTargetCandidate
    {
        public Entity Target;
        public float Distance;
        public float Threat;
        public float NormalizedHull;
        public float ObjectivePriority;
        public float FocusFireSupport;
    }

    public struct CombatSelfState
    {
        public float PreferredEngagementRange;
        public float NormalizedHull;
    }

    public struct CombatScoredTarget
    {
        public Entity Target;
        public float Score;
        public uint StableKey;
    }

    public static class CombatTargetScoring
    {
        private const float ScoreEpsilon = 1e-5f;

        public static CombatScoredTarget ScoreTarget(
            in CombatDoctrineProfile doctrine,
            in CombatIndividualProfile individual,
            in CombatSelfState self,
            in CombatTargetCandidate candidate)
        {
            var threatScore = math.saturate(candidate.Threat);
            var objectiveScore = math.saturate(candidate.ObjectivePriority);
            var focusFireScore = math.saturate(candidate.FocusFireSupport);
            var weakTargetScore = 1f - math.saturate(candidate.NormalizedHull);

            var preferredRange = math.max(0.001f, self.PreferredEngagementRange);
            var distance = math.max(0f, candidate.Distance);
            var rangeError = math.abs(distance - preferredRange) / preferredRange;
            var rangeScore = 1f - math.saturate(rangeError);

            var weightedScore =
                math.max(0f, doctrine.ThreatWeight) * threatScore +
                math.max(0f, doctrine.RangeWeight) * rangeScore +
                math.max(0f, doctrine.HealthWeight) * weakTargetScore +
                math.max(0f, doctrine.ObjectiveWeight) * objectiveScore +
                math.max(0f, doctrine.FocusFireWeight) * focusFireScore;

            var individualAdjustment =
                math.clamp(individual.AggressionBias, -1f, 1f) * threatScore +
                math.clamp(individual.ObjectiveBias, -1f, 1f) * objectiveScore +
                math.clamp(individual.FinishOffBias, -1f, 1f) * weakTargetScore +
                math.clamp(individual.RangeBias, -1f, 1f) * rangeScore;

            return new CombatScoredTarget
            {
                Target = candidate.Target,
                Score = weightedScore + individualAdjustment,
                StableKey = BuildStableKey(candidate.Target)
            };
        }

        public static bool TrySelectBestTarget(
            in CombatDoctrineProfile doctrine,
            in CombatIndividualProfile individual,
            in CombatSelfState self,
            in NativeArray<CombatTargetCandidate> candidates,
            out CombatScoredTarget bestTarget)
        {
            bestTarget = default;
            if (candidates.Length == 0)
            {
                return false;
            }

            var best = ScoreTarget(doctrine, individual, self, candidates[0]);
            for (var i = 1; i < candidates.Length; i++)
            {
                var scored = ScoreTarget(doctrine, individual, self, candidates[i]);
                if (CompareScoredTargets(scored, best) < 0)
                {
                    best = scored;
                }
            }

            bestTarget = best;
            return true;
        }

        public static void RankTargets(
            in CombatDoctrineProfile doctrine,
            in CombatIndividualProfile individual,
            in CombatSelfState self,
            in NativeArray<CombatTargetCandidate> candidates,
            ref NativeList<CombatScoredTarget> rankedTargets)
        {
            rankedTargets.Clear();
            if (candidates.Length == 0)
            {
                return;
            }

            rankedTargets.Capacity = math.max(rankedTargets.Capacity, candidates.Length);
            for (var i = 0; i < candidates.Length; i++)
            {
                rankedTargets.Add(ScoreTarget(doctrine, individual, self, candidates[i]));
            }

            // Small candidate sets are expected here; insertion sort keeps ordering deterministic.
            for (var i = 1; i < rankedTargets.Length; i++)
            {
                var cursor = i;
                while (cursor > 0 && CompareScoredTargets(rankedTargets[cursor], rankedTargets[cursor - 1]) < 0)
                {
                    var tmp = rankedTargets[cursor - 1];
                    rankedTargets[cursor - 1] = rankedTargets[cursor];
                    rankedTargets[cursor] = tmp;
                    cursor--;
                }
            }
        }

        public static CombatEngagementDecision DecideEngagement(
            in CombatDoctrineProfile doctrine,
            in CombatIndividualProfile individual,
            in CombatSelfState self,
            float topTargetScore)
        {
            var hull = math.saturate(self.NormalizedHull);
            var retreatThreshold = math.saturate(doctrine.RetreatHullThreshold);
            var retreatPressure = retreatThreshold <= 0f
                ? 0f
                : math.saturate((retreatThreshold - hull) / math.max(retreatThreshold, 0.001f));

            retreatPressure *= math.max(0f, doctrine.RetreatRiskMultiplier);
            retreatPressure *= 1f - math.saturate(individual.RiskTolerance);

            var engageDrive =
                topTargetScore +
                math.clamp(individual.AggressionBias, -1f, 1f) -
                math.max(0f, doctrine.EngageScoreThreshold);

            return engageDrive >= retreatPressure
                ? CombatEngagementDecision.Engage
                : CombatEngagementDecision.Retreat;
        }

        public static int CompareScoredTargets(in CombatScoredTarget left, in CombatScoredTarget right)
        {
            if (left.Score > right.Score + ScoreEpsilon)
            {
                return -1;
            }

            if (right.Score > left.Score + ScoreEpsilon)
            {
                return 1;
            }

            if (left.StableKey < right.StableKey)
            {
                return -1;
            }

            if (left.StableKey > right.StableKey)
            {
                return 1;
            }

            return 0;
        }

        public static uint BuildStableKey(Entity entity)
        {
            return ((uint)entity.Index << 8) ^ (uint)(entity.Version & 0xFF);
        }
    }
}

