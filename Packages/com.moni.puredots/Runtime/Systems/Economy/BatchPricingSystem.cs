using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Economy;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Economy
{
    /// <summary>
    /// Computes a simple dynamic price multiplier based on inventory fill level.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BatchInventorySystem))]
    public partial struct BatchPricingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BatchInventory>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (SystemAPI.GetSingleton<RewindState>().Mode != RewindMode.Record)
            {
                return;
            }

            var cfg = SystemAPI.TryGetSingleton<BatchPricingConfig>(out var config)
                ? config
                : BatchPricingConfig.CreateDefault();

            foreach (var (inventory, pricing) in SystemAPI.Query<RefRO<BatchInventory>, RefRW<BatchPricingState>>())
            {
                var fill = inventory.ValueRO.MaxCapacity > 0f
                    ? math.saturate(inventory.ValueRO.TotalUnits / inventory.ValueRO.MaxCapacity)
                    : 0f;

                float multiplier;
                if (fill <= cfg.LowFillThreshold)
                {
                    multiplier = cfg.MaxMultiplier;
                }
                else if (fill >= cfg.HighFillThreshold)
                {
                    multiplier = cfg.MinMultiplier;
                }
                else
                {
                    var t = math.saturate((fill - cfg.LowFillThreshold) / math.max(0.0001f, cfg.HighFillThreshold - cfg.LowFillThreshold));
                    multiplier = math.lerp(cfg.MaxMultiplier, cfg.MinMultiplier, t);
                }

                pricing.ValueRW.LastPriceMultiplier = multiplier;
                pricing.ValueRW.LastUpdateTick = timeState.Tick;
            }
        }
    }
}
