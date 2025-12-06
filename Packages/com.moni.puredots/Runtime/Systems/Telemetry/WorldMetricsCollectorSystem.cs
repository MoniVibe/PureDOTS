using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Telemetry;

namespace PureDOTS.Systems.Telemetry
{
    /// <summary>
    /// Aggregates raw component stats into TelemetryBuffers every 5-10 ticks.
    /// Deterministic arithmetic over ECS components, all Burst-safe.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct WorldMetricsCollectorSystem : ISystem
    {
        private uint _lastCollectionTick;
        private const uint CollectionInterval = 5; // Collect every 5 ticks

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _lastCollectionTick = 0;
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.Tick - _lastCollectionTick < CollectionInterval)
            {
                return;
            }

            _lastCollectionTick = timeState.Tick;

            // Ensure telemetry stream exists
            Entity telemetryEntity;
            if (!SystemAPI.TryGetSingletonEntity<TelemetryStream>(out telemetryEntity))
            {
                telemetryEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<TelemetryStream>(telemetryEntity);
                state.EntityManager.AddBuffer<TelemetryMetric>(telemetryEntity);
            }

            var stream = SystemAPI.GetComponentRW<TelemetryStream>(telemetryEntity);
            var metrics = state.EntityManager.GetBuffer<TelemetryMetric>(telemetryEntity);

            // Clear previous metrics
            metrics.Clear();

            // Collect population metrics
            int villagerCount = 0;
            float totalMorale = 0f;
            float totalHunger = 0f;
            float totalEnergy = 0f;

            foreach (var needs in SystemAPI.Query<RefRO<VillagerNeeds>>())
            {
                villagerCount++;
                totalMorale += needs.ValueRO.MoraleFloat;
                totalHunger += needs.ValueRO.HungerFloat;
                totalEnergy += needs.ValueRO.EnergyFloat;
            }

            if (villagerCount > 0)
            {
                float avgMorale = totalMorale / villagerCount;
                float avgHunger = totalHunger / villagerCount;
                float avgEnergy = totalEnergy / villagerCount;

                metrics.AddMetric(new FixedString64Bytes("population.villagers"), villagerCount, TelemetryMetricUnit.Count);
                metrics.AddMetric(new FixedString64Bytes("morale.average"), avgMorale, TelemetryMetricUnit.Ratio);
                metrics.AddMetric(new FixedString64Bytes("hunger.average"), avgHunger, TelemetryMetricUnit.Ratio);
                metrics.AddMetric(new FixedString64Bytes("energy.average"), avgEnergy, TelemetryMetricUnit.Ratio);
            }

            // Collect resource metrics
            int resourceCount = 0;
            float totalGatherRate = 0f;

            foreach (var source in SystemAPI.Query<RefRO<ResourceSourceConfig>>())
            {
                resourceCount++;
                totalGatherRate += source.ValueRO.GatherRatePerWorker;
            }

            if (resourceCount > 0)
            {
                metrics.AddMetric(new FixedString64Bytes("resources.count"), resourceCount, TelemetryMetricUnit.Count);
                metrics.AddMetric(new FixedString64Bytes("resources.total_gather_rate"), totalGatherRate, TelemetryMetricUnit.None);
            }

            // Update stream version
            stream.ValueRW.Version++;
            stream.ValueRW.LastTick = timeState.Tick;
        }
    }
}

