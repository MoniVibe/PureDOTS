using NUnit.Framework;
using PureDOTS.Systems.Combat;

namespace PureDOTS.Tests.Combat
{
    public class CombatEvidenceMetricsMathTests
    {
        [Test]
        public void Step_IsDeterministic_ForSameTickSequence()
        {
            var first = default(CombatEvidenceAccumulator);
            var second = default(CombatEvidenceAccumulator);

            StepSequence(ref first);
            StepSequence(ref second);

            Assert.That(first.EngagementTransitions, Is.EqualTo(second.EngagementTransitions));
            Assert.That(first.DamageTotal, Is.EqualTo(second.DamageTotal));
            Assert.That(first.LossesTotal, Is.EqualTo(second.LossesTotal));
            Assert.That(first.SalvageTotal, Is.EqualTo(second.SalvageTotal));
            Assert.That(first.FireEventsTotal, Is.EqualTo(second.FireEventsTotal));
            Assert.That(first.HitEventsTotal, Is.EqualTo(second.HitEventsTotal));
            Assert.That(first.DamageEventsTotal, Is.EqualTo(second.DamageEventsTotal));
            Assert.That(first.MonotonicInvariantOk && first.DamageLossInvariantOk && first.SalvageInvariantOk, Is.True);
        }

        [Test]
        public void Step_DamageCounter_IsMonotonic_WithHealing()
        {
            var accumulator = default(CombatEvidenceAccumulator);

            CombatEvidenceMetricsMath.Step(ref accumulator, currentTotalHealth: 100f, currentDeadCount: 0, currentActiveEngagements: 1, fireEventCount: 1, hitEventCount: 1, damageEventCount: 1);
            CombatEvidenceMetricsMath.Step(ref accumulator, currentTotalHealth: 90f, currentDeadCount: 0, currentActiveEngagements: 1, fireEventCount: 2, hitEventCount: 1, damageEventCount: 1);
            var afterDamage = accumulator.DamageTotal;

            // Healing should not reduce cumulative damage evidence.
            CombatEvidenceMetricsMath.Step(ref accumulator, currentTotalHealth: 95f, currentDeadCount: 0, currentActiveEngagements: 1, fireEventCount: 0, hitEventCount: 0, damageEventCount: 0);

            Assert.That(accumulator.DamageTotal, Is.EqualTo(afterDamage));
            Assert.That(accumulator.MonotonicInvariantOk, Is.True);
        }

        [Test]
        public void Step_SalvageNeverExceedsLosses()
        {
            var accumulator = default(CombatEvidenceAccumulator);

            CombatEvidenceMetricsMath.Step(ref accumulator, currentTotalHealth: 80f, currentDeadCount: 0, currentActiveEngagements: 2, fireEventCount: 2, hitEventCount: 1, damageEventCount: 1);
            CombatEvidenceMetricsMath.Step(ref accumulator, currentTotalHealth: 70f, currentDeadCount: 1, currentActiveEngagements: 2, fireEventCount: 2, hitEventCount: 2, damageEventCount: 2);
            CombatEvidenceMetricsMath.Step(ref accumulator, currentTotalHealth: 55f, currentDeadCount: 2, currentActiveEngagements: 1, fireEventCount: 3, hitEventCount: 2, damageEventCount: 2);

            Assert.That(accumulator.SalvageTotal, Is.LessThanOrEqualTo(accumulator.LossesTotal));
            Assert.That(accumulator.SalvageInvariantOk, Is.True);
            Assert.That(accumulator.DamageLossInvariantOk, Is.True);
        }

        [Test]
        public void Step_EventEvidenceAccumulatesMonotonically()
        {
            var accumulator = default(CombatEvidenceAccumulator);

            CombatEvidenceMetricsMath.Step(ref accumulator, currentTotalHealth: 120f, currentDeadCount: 0, currentActiveEngagements: 0, fireEventCount: 1, hitEventCount: 0, damageEventCount: 0);
            CombatEvidenceMetricsMath.Step(ref accumulator, currentTotalHealth: 110f, currentDeadCount: 0, currentActiveEngagements: 1, fireEventCount: 2, hitEventCount: 1, damageEventCount: 1);
            CombatEvidenceMetricsMath.Step(ref accumulator, currentTotalHealth: 100f, currentDeadCount: 1, currentActiveEngagements: 1, fireEventCount: 0, hitEventCount: 0, damageEventCount: 0);

            Assert.That(accumulator.FireEventsTotal, Is.GreaterThan(0.0));
            Assert.That(accumulator.HitEventsTotal, Is.GreaterThan(0.0));
            Assert.That(accumulator.DamageEventsTotal, Is.GreaterThan(0.0));
            Assert.That(accumulator.MonotonicInvariantOk, Is.True);
        }

        private static void StepSequence(ref CombatEvidenceAccumulator accumulator)
        {
            CombatEvidenceMetricsMath.Step(ref accumulator, currentTotalHealth: 120f, currentDeadCount: 0, currentActiveEngagements: 0, fireEventCount: 0, hitEventCount: 0, damageEventCount: 0);
            CombatEvidenceMetricsMath.Step(ref accumulator, currentTotalHealth: 110f, currentDeadCount: 0, currentActiveEngagements: 2, fireEventCount: 2, hitEventCount: 1, damageEventCount: 1);
            CombatEvidenceMetricsMath.Step(ref accumulator, currentTotalHealth: 90f, currentDeadCount: 1, currentActiveEngagements: 3, fireEventCount: 3, hitEventCount: 2, damageEventCount: 2);
            CombatEvidenceMetricsMath.Step(ref accumulator, currentTotalHealth: 70f, currentDeadCount: 2, currentActiveEngagements: 1, fireEventCount: 1, hitEventCount: 1, damageEventCount: 1);
            CombatEvidenceMetricsMath.Step(ref accumulator, currentTotalHealth: 60f, currentDeadCount: 2, currentActiveEngagements: 0, fireEventCount: 0, hitEventCount: 0, damageEventCount: 0);
        }
    }
}
