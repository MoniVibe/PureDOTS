using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Economy.Ownership;

namespace PureDOTS.Systems.Economy.Ownership.Aggregate
{
    /// <summary>
    /// Market equilibrium system running at 0.2Hz (AggregateEconomySystemGroup).
    /// Calculates Price = Price + k * (Demand - Supply) per commodity.
    /// Batches per commodity type, smooths prices galaxy-wide.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AggregateEconomySystemGroup))]
    public partial struct MarketEquilibriumSystem : ISystem
    {
        private uint _lastUpdateTick;
        private const uint UpdateIntervalTicks = 300; // 0.2Hz at 60Hz base rate (every 5 seconds)

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

            // Throttle to 0.2Hz (every 300 ticks at 60Hz)
            if (currentTick - _lastUpdateTick < UpdateIntervalTicks)
            {
                return;
            }

            _lastUpdateTick = currentTick;

            var job = new MarketEquilibriumJob
            {
                CurrentTick = currentTick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct MarketEquilibriumJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                Entity entity,
                ref MarketCell marketCell,
                DynamicBuffer<MarketCommodityData> commodityBuffer)
            {
                // Update market cell tick
                marketCell.LastUpdateTick = CurrentTick;

                // Calculate price equilibrium for each commodity
                for (int i = 0; i < commodityBuffer.Length; i++)
                {
                    var commodity = commodityBuffer[i];

                    // Price adjustment: Price = Price + k * (Demand - Supply)
                    float supplyDemandDelta = commodity.Demand - commodity.Supply;
                    float priceAdjustment = commodity.PriceAdjustmentRate * supplyDemandDelta;
                    commodity.Price = math.max(0.01f, commodity.Price + priceAdjustment); // Clamp to minimum price

                    // Update buffer
                    commodityBuffer[i] = commodity;
                }
            }
        }
    }
}

