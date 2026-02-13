using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
{
    public struct CombatEvidenceAccumulator
    {
        public bool Initialized;
        public float PreviousTotalHealth;
        public int PreviousDeadCount;
        public int PreviousActiveEngagements;

        public double EngagementTransitions;
        public double DamageTotal;
        public double LossesTotal;
        public double SalvageTotal;

        public bool MonotonicInvariantOk;
        public bool DamageLossInvariantOk;
        public bool SalvageInvariantOk;
    }

    public static class CombatEvidenceMetricsMath
    {
        public static void Step(
            ref CombatEvidenceAccumulator accumulator,
            float currentTotalHealth,
            int currentDeadCount,
            int currentActiveEngagements)
        {
            if (!accumulator.Initialized)
            {
                accumulator.Initialized = true;
                accumulator.PreviousTotalHealth = math.max(0f, currentTotalHealth);
                accumulator.PreviousDeadCount = math.max(0, currentDeadCount);
                accumulator.PreviousActiveEngagements = math.max(0, currentActiveEngagements);
                accumulator.MonotonicInvariantOk = true;
                accumulator.DamageLossInvariantOk = true;
                accumulator.SalvageInvariantOk = true;
                return;
            }

            var clampedHealth = math.max(0f, currentTotalHealth);
            var clampedDeadCount = math.max(0, currentDeadCount);
            var clampedEngagements = math.max(0, currentActiveEngagements);

            var engagementDelta = math.abs(clampedEngagements - accumulator.PreviousActiveEngagements);
            var damageDelta = math.max(0f, accumulator.PreviousTotalHealth - clampedHealth);
            var lossesDelta = math.max(0, clampedDeadCount - accumulator.PreviousDeadCount);

            accumulator.EngagementTransitions += engagementDelta;
            accumulator.DamageTotal += damageDelta;
            accumulator.LossesTotal += lossesDelta;
            accumulator.SalvageTotal += lossesDelta;

            accumulator.PreviousTotalHealth = clampedHealth;
            accumulator.PreviousDeadCount = clampedDeadCount;
            accumulator.PreviousActiveEngagements = clampedEngagements;

            accumulator.MonotonicInvariantOk = accumulator.EngagementTransitions >= 0.0 &&
                                               accumulator.DamageTotal >= 0.0 &&
                                               accumulator.LossesTotal >= 0.0 &&
                                               accumulator.SalvageTotal >= 0.0;
            accumulator.DamageLossInvariantOk = accumulator.DamageTotal + 1e-6 >= accumulator.LossesTotal;
            accumulator.SalvageInvariantOk = accumulator.SalvageTotal <= accumulator.LossesTotal + 1e-6;
        }
    }
}

