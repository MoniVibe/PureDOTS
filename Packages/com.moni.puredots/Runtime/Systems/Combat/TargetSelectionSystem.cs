using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Combat.Targeting;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Performance;
using PureDOTS.Systems.Performance;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// WARM path: Target selection for combat.
    /// Only for units "ready to act" (initiative above threshold).
    /// Small local neighbor set (spatial grid/cell lists).
    /// Ability/special action selection with bounded options.
    /// Cap on re-evaluation frequency.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(WarmPathSystemGroup))]
    [UpdateAfter(typeof(UniversalPerformanceBudgetSystem))]
    public partial struct TargetSelectionSystem : ISystem
    {
        private ComponentLookup<CombatDoctrineProfile> _doctrineLookup;
        private ComponentLookup<CombatIndividualProfile> _individualLookup;
        private ComponentLookup<CombatAI> _combatAiLookup;
        private ComponentLookup<Health> _healthLookup;
        private ComponentLookup<TargetSelectionConfig> _selectionConfigLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<UniversalPerformanceBudget>();
            state.RequireForUpdate<UniversalPerformanceCounters>();

            _doctrineLookup = state.GetComponentLookup<CombatDoctrineProfile>(true);
            _individualLookup = state.GetComponentLookup<CombatIndividualProfile>(true);
            _combatAiLookup = state.GetComponentLookup<CombatAI>(true);
            _healthLookup = state.GetComponentLookup<Health>(true);
            _selectionConfigLookup = state.GetComponentLookup<TargetSelectionConfig>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var budget = SystemAPI.GetSingleton<UniversalPerformanceBudget>();
            var counters = SystemAPI.GetSingletonRW<UniversalPerformanceCounters>();
            _doctrineLookup.Update(ref state);
            _individualLookup.Update(ref state);
            _combatAiLookup.Update(ref state);
            _healthLookup.Update(ref state);
            _selectionConfigLookup.Update(ref state);

            // Check budget
            if (counters.ValueRO.TargetSelectionsThisTick >= budget.MaxTargetSelectionsPerTick)
            {
                return;
            }

            // Process units that are "ready to act" and have candidate targets.
            foreach (var (targetPriorityRef, potentialTargets, cadence, entity) in
                SystemAPI.Query<RefRW<TargetPriority>, DynamicBuffer<PotentialTarget>, RefRO<UpdateCadence>>()
                .WithEntityAccess())
            {
                // Check update cadence
                if (!UpdateCadenceHelpers.ShouldUpdate(timeState.Tick, cadence.ValueRO))
                {
                    continue;
                }

                // Check budget
                if (counters.ValueRO.TargetSelectionsThisTick >= budget.MaxTargetSelectionsPerTick)
                {
                    break;
                }

                var targetPriority = targetPriorityRef.ValueRO;
                if (targetPriority.CurrentTarget != Entity.Null)
                {
                    if (!targetPriority.AllowAutoSwitch)
                    {
                        continue;
                    }

                    var ticksSinceSelect = timeState.Tick >= targetPriority.TargetSelectedTick
                        ? timeState.Tick - targetPriority.TargetSelectedTick
                        : 0u;
                    if (ticksSinceSelect < targetPriority.TargetLockDuration)
                    {
                        continue;
                    }
                }

                if (potentialTargets.Length == 0)
                {
                    continue;
                }

                var config = _selectionConfigLookup.HasComponent(entity)
                    ? _selectionConfigLookup[entity]
                    : default;

                var doctrine = ResolveDoctrine(entity, config);
                var individual = ResolveIndividual(entity);

                var normalizedHull = 1f;
                if (_healthLookup.HasComponent(entity))
                {
                    var health = _healthLookup[entity];
                    normalizedHull = health.MaxHealth > 0f
                        ? math.saturate(health.Current / health.MaxHealth)
                        : 1f;
                }

                var selfState = new CombatSelfState
                {
                    PreferredEngagementRange = config.MaxDetectionRange > 0f ? config.MaxDetectionRange * 0.5f : 25f,
                    NormalizedHull = normalizedHull
                };

                var hasBest = false;
                var best = default(CombatScoredTarget);
                for (var i = 0; i < potentialTargets.Length; i++)
                {
                    var potential = potentialTargets[i];
                    var candidate = new CombatTargetCandidate
                    {
                        Target = potential.Target,
                        Distance = math.max(0f, potential.Distance),
                        Threat = math.max(0f, potential.ThreatScore),
                        NormalizedHull = math.saturate(potential.HealthPercent),
                        ObjectivePriority = potential.IsHighValue ? 1f : 0f,
                        FocusFireSupport = potential.IsAttackingUs ? 1f : 0f
                    };

                    var scored = CombatTargetScoring.ScoreTarget(doctrine, individual, selfState, candidate);
                    if (!hasBest || CombatTargetScoring.CompareScoredTargets(scored, best) < 0)
                    {
                        best = scored;
                        hasBest = true;
                    }
                }

                if (!hasBest)
                {
                    continue;
                }

                var decision = CombatTargetScoring.DecideEngagement(doctrine, individual, selfState, best.Score);
                var updated = targetPriority;
                if (decision == CombatEngagementDecision.Retreat)
                {
                    updated.CurrentTarget = Entity.Null;
                    updated.ThreatScore = 0f;
                }
                else
                {
                    if (updated.CurrentTarget != best.Target)
                    {
                        updated.TargetSelectedTick = timeState.Tick;
                    }

                    updated.CurrentTarget = best.Target;
                    updated.ThreatScore = best.Score;
                    updated.LastEngagedTick = timeState.Tick;
                }

                targetPriorityRef.ValueRW = updated;

                counters.ValueRW.TargetSelectionsThisTick++;
                counters.ValueRW.CombatOperationsThisTick++;
            }
        }

        private CombatDoctrineProfile ResolveDoctrine(Entity entity, in TargetSelectionConfig config)
        {
            if (_doctrineLookup.HasComponent(entity))
            {
                return _doctrineLookup[entity];
            }

            return new CombatDoctrineProfile
            {
                ThreatWeight = math.max(0f, config.ThreatWeight),
                RangeWeight = math.max(0f, config.DistanceWeight),
                HealthWeight = math.max(0f, config.HealthWeight),
                ObjectiveWeight = 0.2f,
                FocusFireWeight = 0.2f,
                EngageScoreThreshold = 0f,
                RetreatHullThreshold = 0.3f,
                RetreatRiskMultiplier = 1f
            };
        }

        private CombatIndividualProfile ResolveIndividual(Entity entity)
        {
            if (_individualLookup.HasComponent(entity))
            {
                return _individualLookup[entity];
            }

            if (_combatAiLookup.HasComponent(entity))
            {
                var combatAI = _combatAiLookup[entity];
                return new CombatIndividualProfile
                {
                    AggressionBias = math.clamp(combatAI.Aggression / 50f, -1f, 1f),
                    ObjectiveBias = 0f,
                    FinishOffBias = 0f,
                    RangeBias = 0f,
                    RiskTolerance = 1f - math.saturate(combatAI.FleeThresholdHP / 100f)
                };
            }

            return new CombatIndividualProfile
            {
                AggressionBias = 0f,
                ObjectiveBias = 0f,
                FinishOffBias = 0f,
                RangeBias = 0f,
                RiskTolerance = 0.5f
            };
        }
    }
}
