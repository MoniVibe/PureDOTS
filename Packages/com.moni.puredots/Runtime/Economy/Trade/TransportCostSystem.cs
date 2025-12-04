using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Economy.Wealth;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Economy.Trade
{
    /// <summary>
    /// Charges operating costs via Chunk 1 transactions.
    /// Base cost per day + cost per km.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct TransportCostSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            var deltaTime = tickTimeState.FixedDeltaTime * math.max(0f, tickTimeState.CurrentSpeedMultiplier);

            foreach (var (transport, progress, entity) in SystemAPI.Query<RefRO<TransportEntity>, RefRO<TransportProgress>>().WithEntityAccess())
            {
                // Calculate operating cost
                float costPerDay = 10f; // Simplified
                float cost = costPerDay * deltaTime;

                if (transport.ValueRO.OwnerWallet != Entity.Null)
                {
                    // Charge owner wallet (simplified - should use proper transaction system)
                    // WealthTransactionSystem.RecordTransaction(...)
                }
            }
        }
    }
}

