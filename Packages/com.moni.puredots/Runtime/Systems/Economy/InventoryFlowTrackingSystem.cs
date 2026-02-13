using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Economy;
using PureDOTS.Runtime;
using PureDOTS.Runtime.Scenarios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Economy
{
    /// <summary>
    /// Tracks smoothed inflow/outflow per batch inventory for downstream pricing and trade signals.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BatchInventorySystem))]
    public partial struct InventoryFlowTrackingSystem : ISystem
    {
        private static readonly FixedString64Bytes MinedPerTickMetricKey = new FixedString64Bytes("minedPerTick");
        private static readonly FixedString64Bytes StockpileDeltaMetricKey = new FixedString64Bytes("stockpileDelta");

        private ComponentLookup<InventoryFlowState> _flowLookup;
        private float _baselineStockpileUnits;
        private double _cumulativeMinedUnits;
        private uint _metricSamples;
        private bool _metricsInitialized;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BatchInventory>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _flowLookup = state.GetComponentLookup<InventoryFlowState>(false);
            _baselineStockpileUnits = 0f;
            _cumulativeMinedUnits = 0.0;
            _metricSamples = 0;
            _metricsInitialized = false;
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

            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused
                || !SystemAPI.TryGetSingleton<RewindState>(out var rewindState)
                || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var settings = SystemAPI.TryGetSingleton<InventoryFlowSettings>(out var flowCfg)
                ? flowCfg
                : InventoryFlowSettings.CreateDefault();
            var smoothing = math.clamp(settings.Smoothing * math.max(1f, timeState.CurrentSpeedMultiplier), 0f, 1f);

            _flowLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var totalStockpileUnits = 0f;
            var minedThisTick = 0f;

            foreach (var (inventory, entity) in SystemAPI.Query<RefRO<BatchInventory>>().WithEntityAccess())
            {
                totalStockpileUnits += inventory.ValueRO.TotalUnits;

                if (!_flowLookup.HasComponent(entity))
                {
                    ecb.AddComponent(entity, new InventoryFlowState
                    {
                        LastUnits = inventory.ValueRO.TotalUnits,
                        SmoothedInflow = 0f,
                        SmoothedOutflow = 0f,
                        LastUpdateTick = timeState.Tick
                    });
                    continue;
                }

                var flow = _flowLookup[entity];
                var delta = inventory.ValueRO.TotalUnits - flow.LastUnits;
                var inflow = math.max(0f, delta);
                var outflow = math.max(0f, -delta);
                minedThisTick += inflow;

                flow.SmoothedInflow = math.lerp(flow.SmoothedInflow, inflow, smoothing);
                flow.SmoothedOutflow = math.lerp(flow.SmoothedOutflow, outflow, smoothing);
                flow.LastUnits = inventory.ValueRO.TotalUnits;
                flow.LastUpdateTick = timeState.Tick;

                _flowLookup[entity] = flow;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            if (!_metricsInitialized)
            {
                _baselineStockpileUnits = totalStockpileUnits;
                _cumulativeMinedUnits = 0.0;
                _metricSamples = 0;
                _metricsInitialized = true;
            }

            _metricSamples++;
            _cumulativeMinedUnits += minedThisTick;

            var minedPerTick = _metricSamples > 0
                ? _cumulativeMinedUnits / _metricSamples
                : 0.0;
            var stockpileDelta = totalStockpileUnits - _baselineStockpileUnits;

            ScenarioMetricsUtility.SetMetric(state.EntityManager, MinedPerTickMetricKey, minedPerTick);
            ScenarioMetricsUtility.SetMetric(state.EntityManager, StockpileDeltaMetricKey, stockpileDelta);
        }
    }
}
