using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Economy.Wealth;
using PureDOTS.Runtime.Economy.Ownership;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Economy.Policies
{
    /// <summary>
    /// Calculates taxes, records transactions.
    /// Income tax, business tax, transaction tax using Chunk 1 transaction APIs.
    /// Extended to support Ownership system: queries GoverningEntity, calculates tax = Income × TaxRate × LoyaltyModifier.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AggregateEconomySystemGroup))]
    public partial struct TaxCollectionSystem : ISystem
    {
        // Pre-created FixedString constants (initialized at class load time, before Burst compilation)
        private static readonly FixedString64Bytes IncomeTaxLabel = "income_tax";
        private static readonly FixedString64Bytes BusinessTaxLabel = "business_tax";
        private static readonly FixedString128Bytes TaxCollectionContext = "tax_collection";

        private ComponentLookup<TaxPolicy> _taxPolicyLookup;
        private ComponentLookup<VillagerWealth> _villagerWealthLookup;
        private ComponentLookup<BusinessBalance> _businessBalanceLookup;
        private ComponentLookup<VillageTreasury> _villageTreasuryLookup;
        private ComponentLookup<GoverningEntity> _governingEntityLookup;
        private ComponentLookup<LegalEntity> _legalEntityLookup;
        private ComponentLookup<Ledger> _ledgerLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();
            _taxPolicyLookup = state.GetComponentLookup<TaxPolicy>(false);
            _villagerWealthLookup = state.GetComponentLookup<VillagerWealth>(false);
            _businessBalanceLookup = state.GetComponentLookup<BusinessBalance>(false);
            _villageTreasuryLookup = state.GetComponentLookup<VillageTreasury>(false);
            _governingEntityLookup = state.GetComponentLookup<GoverningEntity>(true);
            _legalEntityLookup = state.GetComponentLookup<LegalEntity>(false);
            _ledgerLookup = state.GetComponentLookup<Ledger>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            _taxPolicyLookup.Update(ref state);
            _villagerWealthLookup.Update(ref state);
            _businessBalanceLookup.Update(ref state);
            _villageTreasuryLookup.Update(ref state);
            _governingEntityLookup.Update(ref state);
            _legalEntityLookup.Update(ref state);
            _ledgerLookup.Update(ref state);

            // Process tax collection requests (legacy system)
            // Note: Keeping foreach since tax requests are typically infrequent and RecordTransaction uses EntityManager (not Burst-compatible)
            // TODO: Convert to IJobEntity when RecordTransaction is made Burst-compatible or transaction recording uses ECB
            foreach (var (taxRequest, entity) in SystemAPI.Query<RefRO<TaxCollectionRequest>>().WithEntityAccess())
            {
                ProcessTaxCollection(ref state, taxRequest.ValueRO);
                state.EntityManager.RemoveComponent<TaxCollectionRequest>(entity);
            }

            // Process ownership-based tax collection (new system)
            var ownershipTaxJob = new OwnershipTaxCollectionJob
            {
                GoverningEntityLookup = _governingEntityLookup,
                LegalEntityLookup = _legalEntityLookup,
                LedgerLookup = _ledgerLookup
            };
            state.Dependency = ownershipTaxJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct OwnershipTaxCollectionJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<GoverningEntity> GoverningEntityLookup;
            public ComponentLookup<LegalEntity> LegalEntityLookup;
            public ComponentLookup<Ledger> LedgerLookup;

            public void Execute(
                Entity assetEntity,
                ref Ledger ledger)
            {
                // Check if asset has a governing entity
                if (!GoverningEntityLookup.HasComponent(assetEntity))
                {
                    return;
                }

                var governingEntity = GoverningEntityLookup[assetEntity];
                var legalEntityRef = governingEntity.LegalEntity;

                if (!LegalEntityLookup.HasComponent(legalEntityRef))
                {
                    return;
                }

                var legalEntity = LegalEntityLookup[legalEntityRef];

                // Calculate tax: tax = Income × TaxRate × LoyaltyModifier
                // LoyaltyModifier is 1.0 for now (can be calculated from relationships)
                float loyaltyModifier = 1.0f;
                float taxAmount = ledger.Income * legalEntity.TaxRate * loyaltyModifier;

                if (taxAmount <= 0f)
                {
                    return;
                }

                // Deduct tax from asset ledger
                ledger.Cash -= taxAmount;
                ledger.Expenses += taxAmount;

                // Add to legal entity treasury
                var legalEntityData = LegalEntityLookup[legalEntityRef];
                legalEntityData.Treasury += taxAmount;
                LegalEntityLookup[legalEntityRef] = legalEntityData;
            }
        }

        [BurstCompile]
        private void ProcessTaxCollection(ref SystemState state, TaxCollectionRequest request)
        {
            if (!_taxPolicyLookup.HasComponent(request.TaxPolicyEntity))
            {
                return;
            }

            var taxPolicy = _taxPolicyLookup[request.TaxPolicyEntity];
            var treasury = taxPolicy.TargetEntity;

            // Collect income tax from villagers
            if (_villagerWealthLookup.HasComponent(request.Taxpayer))
            {
                var wealth = _villagerWealthLookup[request.Taxpayer];
                float taxAmount = wealth.Balance * 0.1f; // Simplified income tax

                if (treasury != Entity.Null && _villageTreasuryLookup.HasComponent(treasury))
                {
                    WealthTransactionSystem.RecordTransaction(
                        ref state,
                        request.Taxpayer,
                        treasury,
                        taxAmount,
                        TransactionType.Expense,
                        IncomeTaxLabel,
                        TaxCollectionContext
                    );
                }
            }

            // Collect business tax
            if (_businessBalanceLookup.HasComponent(request.Taxpayer))
            {
                var balance = _businessBalanceLookup[request.Taxpayer];
                float taxAmount = balance.Cash * taxPolicy.BusinessProfitTaxRate;

                if (treasury != Entity.Null && _villageTreasuryLookup.HasComponent(treasury))
                {
                    WealthTransactionSystem.RecordTransaction(
                        ref state,
                        request.Taxpayer,
                        treasury,
                        taxAmount,
                        TransactionType.Expense,
                        BusinessTaxLabel,
                        TaxCollectionContext
                    );
                }
            }
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

