using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Economy.Wealth
{
    /// <summary>
    /// Handles charity and opportunism wealth flows.
    /// Good/Pure alignment triggers charity; Evil/Corrupt triggers opportunism.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct DonationSystem : ISystem
    {
        private ComponentLookup<VillagerWealth> _villagerWealthLookup;
        private ComponentLookup<FamilyWealth> _familyWealthLookup;
        private static readonly FixedString64Bytes OpportunismReason = new FixedString64Bytes("opportunism");
        private static readonly FixedString64Bytes DonationReason = new FixedString64Bytes("donation");

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();
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

            // Process donation requests
            foreach (var (donation, entity) in SystemAPI.Query<RefRO<DonationRequest>>().WithEntityAccess())
            {
                ProcessDonation(ref state, entity, donation.ValueRO);
            }
        }

        [BurstCompile]
        private void ProcessDonation(ref SystemState state, Entity donor, DonationRequest request)
        {
            if (!_villagerWealthLookup.HasComponent(donor))
            {
                return;
            }

            var donorWealth = _villagerWealthLookup[donor];
            if (donorWealth.Balance < request.Amount)
            {
                // Insufficient funds
            state.EntityManager.RemoveComponent<DonationRequest>(donor);
            return;
        }

        // Record transaction
        var reason = request.IsOpportunism ? OpportunismReason : DonationReason;
        WealthTransactionSystem.RecordTransaction(
            ref state,
            donor,
            request.Recipient,
            request.Amount,
                TransactionType.Transfer,
                reason,
                request.Context
            );

            // Remove donation request
            state.EntityManager.RemoveComponent<DonationRequest>(donor);
        }
    }

    /// <summary>
    /// Component requesting a donation/opportunism transfer.
    /// Added by social systems, processed by DonationSystem.
    /// </summary>
    public struct DonationRequest : IComponentData
    {
        public Entity Recipient;
        public float Amount;
        public bool IsOpportunism;
        public FixedString128Bytes Context;
    }
}

