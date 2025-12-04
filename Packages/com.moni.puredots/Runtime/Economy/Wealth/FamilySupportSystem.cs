using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Economy.Wealth
{
    /// <summary>
    /// Handles intra-family wealth transfers (richer members help poorer).
    /// Triggered by wealth tier differences, alignment/purity modifiers, relationship strength.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct FamilySupportSystem : ISystem
    {
        private ComponentLookup<VillagerWealth> _villagerWealthLookup;
        private ComponentLookup<FamilyWealth> _familyWealthLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            _villagerWealthLookup = state.GetComponentLookup<VillagerWealth>(false);
            _familyWealthLookup = state.GetComponentLookup<FamilyWealth>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            _villagerWealthLookup.Update(ref state);
            _familyWealthLookup.Update(ref state);

            // Process family support requests
            foreach (var (support, entity) in SystemAPI.Query<RefRO<FamilySupportRequest>>().WithEntityAccess())
            {
                ProcessFamilySupport(ref state, entity, support.ValueRO);
            }
        }

        [BurstCompile]
        private void ProcessFamilySupport(ref SystemState state, Entity donor, FamilySupportRequest request)
        {
            if (!_villagerWealthLookup.HasComponent(donor))
            {
                return;
            }

            var donorWealth = _villagerWealthLookup[donor];
            if (donorWealth.Balance < request.Amount)
            {
                // Insufficient funds
                state.EntityManager.RemoveComponent<FamilySupportRequest>(donor);
                return;
            }

            // Record transaction
            WealthTransactionSystem.RecordTransaction(
                ref state,
                donor,
                request.Recipient,
                request.Amount,
                TransactionType.Transfer,
                new FixedString64Bytes("family_support"),
                request.Context
            );

            // Remove support request
            state.EntityManager.RemoveComponent<FamilySupportRequest>(donor);
        }
    }

    /// <summary>
    /// Component requesting intra-family wealth transfer.
    /// Added by social systems, processed by FamilySupportSystem.
    /// </summary>
    public struct FamilySupportRequest : IComponentData
    {
        public Entity Recipient;
        public float Amount;
        public FixedString128Bytes Context;
    }
}

