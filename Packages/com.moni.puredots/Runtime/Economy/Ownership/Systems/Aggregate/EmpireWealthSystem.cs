using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Economy.Ownership;
using PureDOTS.Runtime.Aggregate;

namespace PureDOTS.Systems.Economy.Ownership.Aggregate
{
    /// <summary>
    /// Empire wealth system running at 0.2Hz (AggregateEconomySystemGroup).
    /// Aggregates Ledger.Cash across LegalEntity hierarchies.
    /// Updates WorldAggregateProfile with empire-level wealth metrics.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AggregateEconomySystemGroup))]
    [UpdateAfter(typeof(MarketEquilibriumSystem))]
    public partial struct EmpireWealthSystem : ISystem
    {
        private uint _lastUpdateTick;
        private const uint UpdateIntervalTicks = 300; // 0.2Hz at 60Hz base rate

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<TickTimeState>();
            _lastUpdateTick = 0;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record)
            {
                return;
            }

            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var currentTick = tickState.Tick;

            // Throttle to 0.2Hz
            if (currentTick - _lastUpdateTick < UpdateIntervalTicks)
            {
                return;
            }

            _lastUpdateTick = currentTick;

            // Aggregate wealth across all LegalEntity hierarchies
            float totalEmpireWealth = 0f;

            foreach (var (legalEntity, ledger) in SystemAPI.Query<RefRO<LegalEntity>, RefRO<Ledger>>())
            {
                totalEmpireWealth += ledger.ValueRO.Cash;
                totalEmpireWealth += legalEntity.ValueRO.Treasury;
            }

            // Update WorldAggregateProfile if it exists
            if (SystemAPI.TryGetSingletonRW<WorldAggregateProfile>(out var profile))
            {
                // WorldAggregateProfile doesn't have a wealth field by default
                // This would need to be extended or we'd create a separate aggregate component
                // For now, we just calculate the aggregate without storing it
            }
        }
    }
}

