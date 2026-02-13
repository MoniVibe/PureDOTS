using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Scenarios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Emits deterministic combat evidence counters for exterminate-style proofs:
    /// engagement transitions, damage accumulation, losses, and salvage progression.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(DeathSystem))]
    public partial struct CombatEvidenceMetricsSystem : ISystem
    {
        private static readonly FixedString64Bytes EngagementActiveKey = new FixedString64Bytes("combat.engagement.active");
        private static readonly FixedString64Bytes EngagementTransitionsKey = new FixedString64Bytes("combat.engagement.transitions");
        private static readonly FixedString64Bytes DamageTotalKey = new FixedString64Bytes("combat.damage.total");
        private static readonly FixedString64Bytes LossesTotalKey = new FixedString64Bytes("combat.losses.total");
        private static readonly FixedString64Bytes SalvageTotalKey = new FixedString64Bytes("combat.salvage.total");
        private static readonly FixedString64Bytes InvariantMonotonicKey = new FixedString64Bytes("combat.invariant.monotonic");
        private static readonly FixedString64Bytes InvariantDamageLossKey = new FixedString64Bytes("combat.invariant.damage_vs_losses");
        private static readonly FixedString64Bytes InvariantSalvageLossKey = new FixedString64Bytes("combat.invariant.salvage_vs_losses");

        private EntityQuery _activeCombatQuery;
        private CombatEvidenceAccumulator _accumulator;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _activeCombatQuery = state.GetEntityQuery(ComponentType.ReadOnly<ActiveCombat>());
            _accumulator = default;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState) || timeState.IsPaused)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var totalHealth = 0f;
            foreach (var health in SystemAPI.Query<RefRO<Health>>())
            {
                totalHealth += math.max(0f, health.ValueRO.Current);
            }

            var deadCount = 0;
            foreach (var deathState in SystemAPI.Query<RefRO<DeathState>>())
            {
                if (deathState.ValueRO.IsDead)
                {
                    deadCount++;
                }
            }

            var activeCombatCount = _activeCombatQuery.CalculateEntityCount();
            var fireEventCount = 0;
            foreach (var events in SystemAPI.Query<DynamicBuffer<FireEvent>>())
            {
                fireEventCount += events.Length;
            }

            var hitEventCount = 0;
            foreach (var events in SystemAPI.Query<DynamicBuffer<HitEvent>>())
            {
                hitEventCount += events.Length;
            }

            var damageEventCount = 0;
            foreach (var events in SystemAPI.Query<DynamicBuffer<DamageEvent>>())
            {
                damageEventCount += events.Length;
            }

            var activeEngagements = (activeCombatCount > 0 || fireEventCount > 0 || hitEventCount > 0 || damageEventCount > 0)
                ? 1
                : 0;

            CombatEvidenceMetricsMath.Step(ref _accumulator, totalHealth, deadCount, activeEngagements);

            if (!_accumulator.Initialized)
            {
                return;
            }

            ScenarioMetricsUtility.SetMetric(state.EntityManager, EngagementActiveKey, math.max(0, activeEngagements));
            ScenarioMetricsUtility.SetMetric(state.EntityManager, EngagementTransitionsKey, _accumulator.EngagementTransitions);
            ScenarioMetricsUtility.SetMetric(state.EntityManager, DamageTotalKey, _accumulator.DamageTotal);
            ScenarioMetricsUtility.SetMetric(state.EntityManager, LossesTotalKey, _accumulator.LossesTotal);
            ScenarioMetricsUtility.SetMetric(state.EntityManager, SalvageTotalKey, _accumulator.SalvageTotal);
            ScenarioMetricsUtility.SetMetric(state.EntityManager, InvariantMonotonicKey, _accumulator.MonotonicInvariantOk ? 1.0 : 0.0);
            ScenarioMetricsUtility.SetMetric(state.EntityManager, InvariantDamageLossKey, _accumulator.DamageLossInvariantOk ? 1.0 : 0.0);
            ScenarioMetricsUtility.SetMetric(state.EntityManager, InvariantSalvageLossKey, _accumulator.SalvageInvariantOk ? 1.0 : 0.0);
        }
    }
}
