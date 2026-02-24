using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Economy.Wealth;
using PureDOTS.Runtime.Individual;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Economy.Policies
{
    /// <summary>
    /// Calculates taxes, records transactions.
    /// Income tax, business tax, transaction tax using Chunk 1 transaction APIs.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct TaxCollectionSystem : ISystem
    {
        private FixedString64Bytes _incomeTaxReason;
        private FixedString64Bytes _businessTaxReason;
        private FixedString64Bytes _leakageTaxReason;
        private FixedString128Bytes _taxCollectionChannel;
        private FixedString128Bytes _taxLeakageChannel;

        private ComponentLookup<TaxPolicy> _taxPolicyLookup;
        private BufferLookup<TaxBracket> _taxBracketLookup;
        private ComponentLookup<TaxPolicyRuntimeState> _taxRuntimeLookup;
        private ComponentLookup<TaxableIncome> _taxableIncomeLookup;
        private ComponentLookup<TaxAssessmentState> _taxAssessmentLookup;

        private ComponentLookup<AlignmentTriplet> _alignmentLookup;
        private ComponentLookup<PersonalityAxes> _personalityLookup;
        private ComponentLookup<BehaviorTuning> _behaviorLookup;

        private ComponentLookup<VillagerWealth> _villagerWealthLookup;
        private ComponentLookup<FamilyWealth> _familyWealthLookup;
        private ComponentLookup<DynastyWealth> _dynastyWealthLookup;
        private ComponentLookup<BusinessBalance> _businessBalanceLookup;
        private ComponentLookup<GuildTreasury> _guildTreasuryLookup;
        private ComponentLookup<VillageTreasury> _villageTreasuryLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            _taxPolicyLookup = state.GetComponentLookup<TaxPolicy>(false);
            _taxBracketLookup = state.GetBufferLookup<TaxBracket>(true);
            _taxRuntimeLookup = state.GetComponentLookup<TaxPolicyRuntimeState>(false);
            _taxableIncomeLookup = state.GetComponentLookup<TaxableIncome>(true);
            _taxAssessmentLookup = state.GetComponentLookup<TaxAssessmentState>(false);

            _alignmentLookup = state.GetComponentLookup<AlignmentTriplet>(true);
            _personalityLookup = state.GetComponentLookup<PersonalityAxes>(true);
            _behaviorLookup = state.GetComponentLookup<BehaviorTuning>(true);

            _villagerWealthLookup = state.GetComponentLookup<VillagerWealth>(false);
            _familyWealthLookup = state.GetComponentLookup<FamilyWealth>(true);
            _dynastyWealthLookup = state.GetComponentLookup<DynastyWealth>(true);
            _businessBalanceLookup = state.GetComponentLookup<BusinessBalance>(false);
            _guildTreasuryLookup = state.GetComponentLookup<GuildTreasury>(true);
            _villageTreasuryLookup = state.GetComponentLookup<VillageTreasury>(false);

            _incomeTaxReason = "income_tax";
            _businessTaxReason = "business_tax";
            _leakageTaxReason = "tax_leakage";
            _taxCollectionChannel = "tax_collection";
            _taxLeakageChannel = "tax_leakage";
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ScenarioState>(out var scenario) ||
                !scenario.IsInitialized ||
                !scenario.EnableEconomy)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) ||
                rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            _taxPolicyLookup.Update(ref state);
            _taxBracketLookup.Update(ref state);
            _taxRuntimeLookup.Update(ref state);
            _taxableIncomeLookup.Update(ref state);
            _taxAssessmentLookup.Update(ref state);
            _alignmentLookup.Update(ref state);
            _personalityLookup.Update(ref state);
            _behaviorLookup.Update(ref state);
            _villagerWealthLookup.Update(ref state);
            _familyWealthLookup.Update(ref state);
            _dynastyWealthLookup.Update(ref state);
            _businessBalanceLookup.Update(ref state);
            _guildTreasuryLookup.Update(ref state);
            _villageTreasuryLookup.Update(ref state);

            var requestList = new NativeList<TaxRequestWorkItem>(Allocator.Temp);
            foreach (var (taxRequest, requestEntity) in SystemAPI.Query<RefRO<TaxCollectionRequest>>().WithEntityAccess())
            {
                requestList.Add(new TaxRequestWorkItem
                {
                    RequestEntity = requestEntity,
                    Request = taxRequest.ValueRO
                });
            }

            if (requestList.Length == 0)
            {
                requestList.Dispose();
                return;
            }

            var societyProfiles = new NativeParallelHashMap<Entity, ProfileAggregate>(16, Allocator.Temp);
            for (int i = 0; i < requestList.Length; i++)
            {
                var request = requestList[i].Request;
                if (!_taxPolicyLookup.HasComponent(request.TaxPolicyEntity))
                {
                    continue;
                }

                var profileScore = ResolveEntityTaxDispositionScore(request.Taxpayer, _alignmentLookup, _personalityLookup, _behaviorLookup);
                var aggregate = societyProfiles.TryGetValue(request.TaxPolicyEntity, out var existing)
                    ? existing
                    : default;
                aggregate.ScoreSum += profileScore;
                aggregate.Count += 1;
                societyProfiles[request.TaxPolicyEntity] = aggregate;
            }

            var runtimeAccumulators = new NativeParallelHashMap<Entity, RuntimeAggregate>(16, Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var currentTick = SystemAPI.GetSingleton<TickTimeState>().Tick;

            for (int i = 0; i < requestList.Length; i++)
            {
                var workItem = requestList[i];
                ProcessTaxCollection(
                    ref state,
                    workItem.Request,
                    currentTick,
                    societyProfiles,
                    runtimeAccumulators);

                ecb.RemoveComponent<TaxCollectionRequest>(workItem.RequestEntity);
            }

            var runtimeKvp = runtimeAccumulators.GetKeyValueArrays(Allocator.Temp);
            for (int i = 0; i < runtimeKvp.Keys.Length; i++)
            {
                var policyEntity = runtimeKvp.Keys[i];
                var aggregate = runtimeKvp.Values[i];
                var assessmentCount = (float)math.max(1u, aggregate.AssessmentCount);

                var runtimeState = new TaxPolicyRuntimeState
                {
                    LastUpdateTick = currentTick,
                    AssessmentCount = aggregate.AssessmentCount,
                    TotalTaxableBase = aggregate.TotalTaxableBase,
                    TotalAssessedTax = aggregate.TotalAssessed,
                    TotalCollectedTax = aggregate.TotalCollected,
                    TotalLeakageTax = aggregate.TotalLeakage,
                    AverageEffectiveRate = aggregate.SumEffectiveRate / assessmentCount,
                    AverageCompliance01 = aggregate.SumCompliance / assessmentCount,
                    AverageProfileFactor = aggregate.SumProfileFactor / assessmentCount
                };

                if (_taxRuntimeLookup.HasComponent(policyEntity))
                {
                    _taxRuntimeLookup[policyEntity] = runtimeState;
                }
                else
                {
                    state.EntityManager.AddComponentData(policyEntity, runtimeState);
                }
            }

            runtimeKvp.Dispose();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            runtimeAccumulators.Dispose();
            societyProfiles.Dispose();
            requestList.Dispose();
        }

        private void ProcessTaxCollection(
            ref SystemState state,
            TaxCollectionRequest request,
            uint currentTick,
            NativeParallelHashMap<Entity, ProfileAggregate> societyProfiles,
            NativeParallelHashMap<Entity, RuntimeAggregate> runtimeAccumulators)
        {
            if (!_taxPolicyLookup.HasComponent(request.TaxPolicyEntity))
            {
                return;
            }

            var taxPolicy = ResolvePolicyDefaults(_taxPolicyLookup[request.TaxPolicyEntity]);
            var treasury = taxPolicy.TargetEntity;
            if (treasury == Entity.Null || !HasWallet(treasury))
            {
                return;
            }

            var profileFactor = ResolveProfileFactor(taxPolicy, societyProfiles, _alignmentLookup, _personalityLookup, _behaviorLookup, request.TaxPolicyEntity);
            var compliance = ResolveCompliance01(request.Taxpayer, taxPolicy, profileFactor, _alignmentLookup, _personalityLookup, _behaviorLookup);
            var leakageRate = ResolveLeakageRate01(request.Taxpayer, taxPolicy, _alignmentLookup, _personalityLookup);

            var personalTaxable = ResolvePersonalTaxableIncome(request.Taxpayer, taxPolicy, _taxableIncomeLookup, _villagerWealthLookup);
            var businessTaxable = ResolveBusinessTaxableIncome(request.Taxpayer, _taxableIncomeLookup, _businessBalanceLookup);

            var incomeRate = ResolveIncomeRate(personalTaxable, taxPolicy, request.TaxPolicyEntity, _taxBracketLookup) * profileFactor;
            var businessRate = math.max(0f, taxPolicy.BusinessProfitTaxRate) * profileFactor;
            var maxRate = math.clamp(taxPolicy.MaxEffectiveRate, 0f, 0.95f);
            incomeRate = math.clamp(incomeRate, 0f, maxRate);
            businessRate = math.clamp(businessRate, 0f, maxRate);

            var assessedIncomeTax = personalTaxable * incomeRate;
            var assessedBusinessTax = businessTaxable * businessRate;
            var totalAssessedTax = assessedIncomeTax + assessedBusinessTax;
            if (totalAssessedTax <= 0f)
            {
                return;
            }

            var collectedBeforeLeakage = totalAssessedTax * compliance;
            var leakageTax = collectedBeforeLeakage * leakageRate;
            var collectedTax = math.max(0f, collectedBeforeLeakage - leakageTax);

            if (collectedTax > 0f)
            {
                var reason = assessedBusinessTax > assessedIncomeTax ? _businessTaxReason : _incomeTaxReason;
                WealthTransactionSystem.RecordTransaction(
                    ref state,
                    request.Taxpayer,
                    treasury,
                    collectedTax,
                    TransactionType.Expense,
                    reason,
                    _taxCollectionChannel
                );
            }

            if (leakageTax > 0f)
            {
                if (taxPolicy.LeakageSinkEntity != Entity.Null && HasWallet(taxPolicy.LeakageSinkEntity))
                {
                    WealthTransactionSystem.RecordTransaction(
                        ref state,
                        request.Taxpayer,
                        taxPolicy.LeakageSinkEntity,
                        leakageTax,
                        TransactionType.Expense,
                        _leakageTaxReason,
                        _taxLeakageChannel
                    );
                }
            }

            var taxableBase = personalTaxable + businessTaxable;
            var effectiveRate = taxableBase > 0f ? totalAssessedTax / taxableBase : 0f;
            UpsertTaxAssessment(ref state, request.Taxpayer, new TaxAssessmentState
            {
                EffectiveRate = effectiveRate,
                Compliance01 = compliance,
                AssessedTax = totalAssessedTax,
                CollectedTax = collectedTax,
                LeakageTax = leakageTax,
                TaxableBase = taxableBase,
                LastAssessmentTick = currentTick
            });

            var runtimeAggregate = runtimeAccumulators.TryGetValue(request.TaxPolicyEntity, out var existing)
                ? existing
                : default;
            runtimeAggregate.AssessmentCount += 1;
            runtimeAggregate.TotalTaxableBase += taxableBase;
            runtimeAggregate.TotalAssessed += totalAssessedTax;
            runtimeAggregate.TotalCollected += collectedTax;
            runtimeAggregate.TotalLeakage += leakageTax;
            runtimeAggregate.SumEffectiveRate += effectiveRate;
            runtimeAggregate.SumCompliance += compliance;
            runtimeAggregate.SumProfileFactor += profileFactor;
            runtimeAccumulators[request.TaxPolicyEntity] = runtimeAggregate;
        }

        private void UpsertTaxAssessment(ref SystemState state, Entity taxpayer, TaxAssessmentState assessment)
        {
            if (_taxAssessmentLookup.HasComponent(taxpayer))
            {
                _taxAssessmentLookup[taxpayer] = assessment;
            }
            else
            {
                state.EntityManager.AddComponentData(taxpayer, assessment);
            }
        }

        private bool HasWallet(Entity entity)
        {
            return _villagerWealthLookup.HasComponent(entity)
                   || _familyWealthLookup.HasComponent(entity)
                   || _dynastyWealthLookup.HasComponent(entity)
                   || _businessBalanceLookup.HasComponent(entity)
                   || _guildTreasuryLookup.HasComponent(entity)
                   || _villageTreasuryLookup.HasComponent(entity);
        }

        private static float ResolvePersonalTaxableIncome(
            Entity taxpayer,
            TaxPolicy policy,
            ComponentLookup<TaxableIncome> taxableIncomeLookup,
            ComponentLookup<VillagerWealth> villagerWealthLookup)
        {
            float gross = 0f;
            float deductions = 0f;
            if (taxableIncomeLookup.HasComponent(taxpayer))
            {
                var taxableIncome = taxableIncomeLookup[taxpayer];
                gross = math.max(0f, taxableIncome.PersonalIncome);
                deductions = math.max(0f, taxableIncome.DeductibleAmount + taxableIncome.ExemptIncome);
            }
            else if (villagerWealthLookup.HasComponent(taxpayer))
            {
                gross = math.max(0f, villagerWealthLookup[taxpayer].Balance);
            }

            var exemption = math.max(0f, policy.ExemptionFloor);
            return math.max(0f, gross - deductions - exemption);
        }

        private static float ResolveBusinessTaxableIncome(
            Entity taxpayer,
            ComponentLookup<TaxableIncome> taxableIncomeLookup,
            ComponentLookup<BusinessBalance> businessBalanceLookup)
        {
            if (taxableIncomeLookup.HasComponent(taxpayer))
            {
                var taxableIncome = taxableIncomeLookup[taxpayer];
                if (taxableIncome.BusinessIncome > 0f)
                {
                    return math.max(0f, taxableIncome.BusinessIncome);
                }
            }

            return businessBalanceLookup.HasComponent(taxpayer)
                ? math.max(0f, businessBalanceLookup[taxpayer].Cash)
                : 0f;
        }

        private static float ResolveIncomeRate(float taxableIncome, TaxPolicy policy, Entity policyEntity, BufferLookup<TaxBracket> bracketLookup)
        {
            var baseRate = math.max(0f, policy.BaseIncomeTaxRate);
            var maxRate = math.clamp(policy.MaxEffectiveRate, 0f, 0.95f);

            if (taxableIncome <= 0f)
            {
                return 0f;
            }

            if (bracketLookup.HasBuffer(policyEntity))
            {
                var brackets = bracketLookup[policyEntity];
                if (brackets.Length > 0)
                {
                    var assessed = 0f;
                    for (int i = 0; i < brackets.Length; i++)
                    {
                        var start = math.max(0f, brackets[i].Threshold);
                        var rate = math.clamp(brackets[i].MarginalRate, 0f, maxRate);
                        var end = i + 1 < brackets.Length
                            ? math.max(start, brackets[i + 1].Threshold)
                            : float.MaxValue;

                        if (taxableIncome <= start)
                        {
                            continue;
                        }

                        var amountInBracket = math.min(taxableIncome, end) - start;
                        if (amountInBracket > 0f)
                        {
                            assessed += amountInBracket * rate;
                        }
                    }

                    if (assessed > 0f)
                    {
                        return math.clamp(assessed / taxableIncome, 0f, maxRate);
                    }
                }
            }

            var scale = math.max(1f, policy.ProgressivityIncomeScale);
            var progressT = math.saturate(taxableIncome / scale);
            var progressBonus = math.max(0f, policy.Progressivity) * progressT;
            return math.clamp(baseRate + progressBonus, 0f, maxRate);
        }

        private static float ResolveProfileFactor(
            TaxPolicy policy,
            NativeParallelHashMap<Entity, ProfileAggregate> societyProfiles,
            ComponentLookup<AlignmentTriplet> alignmentLookup,
            ComponentLookup<PersonalityAxes> personalityLookup,
            ComponentLookup<BehaviorTuning> behaviorLookup,
            Entity policyEntity)
        {
            if (policy.ProfileMode == TaxProfileSourceMode.None || policy.ProfileElasticity <= 0f)
            {
                return 1f;
            }

            var societyScore = 0f;
            if (societyProfiles.TryGetValue(policyEntity, out var aggregate) && aggregate.Count > 0)
            {
                societyScore = aggregate.ScoreSum / aggregate.Count;
            }

            var rulerScore = ResolveEntityTaxDispositionScore(policy.RulerEntity, alignmentLookup, personalityLookup, behaviorLookup);
            var societyWeight = math.saturate(policy.SocietyInfluence01);
            var rulerWeight = math.saturate(policy.RulerInfluence01);

            var blendedScore = 0f;
            switch (policy.ProfileMode)
            {
                case TaxProfileSourceMode.SocietyAverage:
                    blendedScore = societyScore * math.max(0.01f, societyWeight);
                    break;
                case TaxProfileSourceMode.RulerOnly:
                    blendedScore = rulerScore * math.max(0.01f, rulerWeight);
                    break;
                case TaxProfileSourceMode.SocietyAndRulerBlend:
                    var totalWeight = math.max(0.01f, societyWeight + rulerWeight);
                    blendedScore = ((societyScore * societyWeight) + (rulerScore * rulerWeight)) / totalWeight;
                    break;
            }

            var elasticity = math.clamp(policy.ProfileElasticity, 0f, 1.5f);
            return math.clamp(1f + (blendedScore * elasticity), 0.25f, 2f);
        }

        private static float ResolveCompliance01(
            Entity taxpayer,
            TaxPolicy policy,
            float profileFactor,
            ComponentLookup<AlignmentTriplet> alignmentLookup,
            ComponentLookup<PersonalityAxes> personalityLookup,
            ComponentLookup<BehaviorTuning> behaviorLookup)
        {
            var baseCompliance = math.saturate(policy.BaseCompliance01);
            var enforcement = math.saturate(policy.EnforcementStrength01);
            var disposition = ResolveEntityTaxDispositionScore(taxpayer, alignmentLookup, personalityLookup, behaviorLookup);
            var burdenPenalty = math.max(0f, profileFactor - 1f) * 0.2f;

            var compliance = baseCompliance
                             + (enforcement * 0.45f)
                             + (disposition * 0.2f)
                             - burdenPenalty;
            return math.saturate(compliance);
        }

        private static float ResolveLeakageRate01(
            Entity taxpayer,
            TaxPolicy policy,
            ComponentLookup<AlignmentTriplet> alignmentLookup,
            ComponentLookup<PersonalityAxes> personalityLookup)
        {
            var baseLeakage = math.saturate(policy.CorruptionLeakage01);
            var enforcement = math.saturate(policy.EnforcementStrength01);

            var antiCorruption = 0.5f;
            if (alignmentLookup.HasComponent(taxpayer))
            {
                var alignment = alignmentLookup[taxpayer];
                antiCorruption += math.saturate(alignment.Order) * 0.25f;
                antiCorruption += math.saturate(alignment.Purity) * 0.25f;
            }

            if (personalityLookup.HasComponent(taxpayer))
            {
                var personality = personalityLookup[taxpayer];
                antiCorruption += math.saturate(personality.Selflessness) * 0.1f;
            }

            antiCorruption = math.saturate(antiCorruption);
            var leakage = baseLeakage * (1f - (enforcement * 0.8f)) * (1f - antiCorruption);
            return math.saturate(leakage);
        }

        private static float ResolveEntityTaxDispositionScore(
            Entity entity,
            ComponentLookup<AlignmentTriplet> alignmentLookup,
            ComponentLookup<PersonalityAxes> personalityLookup,
            ComponentLookup<BehaviorTuning> behaviorLookup)
        {
            if (entity == Entity.Null)
            {
                return 0f;
            }

            var score = 0f;
            var weight = 0f;

            if (alignmentLookup.HasComponent(entity))
            {
                var alignment = alignmentLookup[entity];
                score += alignment.Moral * 0.15f;
                score += alignment.Order * 0.35f;
                score += alignment.Purity * 0.3f;
                weight += 0.8f;
            }

            if (personalityLookup.HasComponent(entity))
            {
                var personality = personalityLookup[entity];
                score += personality.Selflessness * 0.2f;
                score += personality.Conviction * 0.1f;
                score -= personality.RiskTolerance * 0.05f;
                weight += 0.35f;
            }

            if (behaviorLookup.HasComponent(entity))
            {
                var behavior = behaviorLookup[entity];
                score += math.clamp(behavior.ObedienceBias - 1f, -1f, 1f) * 0.2f;
                score -= math.clamp(behavior.GreedBias - 1f, -1f, 1f) * 0.2f;
                weight += 0.4f;
            }

            if (weight <= 1e-4f)
            {
                return 0f;
            }

            return math.clamp(score / weight, -1f, 1f);
        }

        private static TaxPolicy ResolvePolicyDefaults(TaxPolicy policy)
        {
            var appearsLegacy = policy.BaseIncomeTaxRate == 0f
                                && policy.ExemptionFloor == 0f
                                && policy.Progressivity == 0f
                                && policy.ProgressivityIncomeScale == 0f
                                && policy.MaxEffectiveRate == 0f
                                && policy.BaseCompliance01 == 0f
                                && policy.EnforcementStrength01 == 0f
                                && policy.CorruptionLeakage01 == 0f;

            if (!appearsLegacy)
            {
                return policy;
            }

            policy.BaseIncomeTaxRate = 0.1f;
            policy.ExemptionFloor = 0f;
            policy.Progressivity = 0f;
            policy.ProgressivityIncomeScale = 300f;
            policy.MaxEffectiveRate = 0.9f;
            policy.BaseCompliance01 = 1f;
            policy.EnforcementStrength01 = 1f;
            policy.CorruptionLeakage01 = 0f;
            return policy;
        }

        private struct TaxRequestWorkItem
        {
            public Entity RequestEntity;
            public TaxCollectionRequest Request;
        }

        private struct ProfileAggregate
        {
            public float ScoreSum;
            public int Count;
        }

        private struct RuntimeAggregate
        {
            public uint AssessmentCount;
            public float TotalTaxableBase;
            public float TotalAssessed;
            public float TotalCollected;
            public float TotalLeakage;
            public float SumEffectiveRate;
            public float SumCompliance;
            public float SumProfileFactor;
        }
    }

    /// <summary>
    /// Request to collect taxes from an entity.
    /// </summary>
    public struct TaxCollectionRequest : IComponentData
    {
        public Entity TaxPolicyEntity;
        public Entity Taxpayer;
    }
}
