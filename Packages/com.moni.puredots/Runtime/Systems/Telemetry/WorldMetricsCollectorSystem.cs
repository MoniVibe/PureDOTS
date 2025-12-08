using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Components.Events;
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
        // Pre-created FixedString constants (initialized at class load time, before Burst compilation)
        private static readonly FixedString64Bytes KeyPopulationVillagers = "population.villagers";
        private static readonly FixedString64Bytes KeyMoraleAverage = "morale.average";
        private static readonly FixedString64Bytes KeyHungerAverage = "hunger.average";
        private static readonly FixedString64Bytes KeyEnergyAverage = "energy.average";
        private static readonly FixedString64Bytes KeyResourcesCount = "resources.count";
        private static readonly FixedString64Bytes KeyResourcesTotalGatherRate = "resources.total_gather_rate";
        private static readonly FixedString64Bytes KeyEventBufferLen = "event.buffer_len";
        private static readonly FixedString64Bytes KeyEventProcessed = "event.processed";
        private static readonly FixedString64Bytes KeyEventDropped = "event.dropped";
        private static readonly FixedString64Bytes KeyEventConsumed = "event.consumed";
        private static readonly FixedString64Bytes KeyEventConsumedLastTick = "event.consumed_last_tick";
        private static readonly FixedString64Bytes KeyCacheLookups = "cache.lookups";
        private static readonly FixedString64Bytes KeyCacheHits = "cache.hits";
        private static readonly FixedString64Bytes KeyCacheMisses = "cache.misses";
        private static readonly FixedString64Bytes KeyCacheHitRate = "cache.hit_rate";
        private static readonly FixedString64Bytes KeyDomainCognitiveTicksUntilNext = "domain.cognitive_ticks_until_next";
        private static readonly FixedString64Bytes KeyDomainEconomyTicksUntilNext = "domain.economy_ticks_until_next";

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

            // Ensure telemetry stream exists (needed for versioning)
            Entity telemetryEntity;
            if (!SystemAPI.TryGetSingletonEntity<TelemetryStream>(out telemetryEntity))
            {
                telemetryEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<TelemetryStream>(telemetryEntity);
            }

            var stream = SystemAPI.GetComponentRW<TelemetryStream>(telemetryEntity);

            // Collect population metrics using jobified collection
            var villagerQuery = SystemAPI.QueryBuilder().WithAll<VillagerNeeds>().Build();
            var villagerMetrics = new NativeList<VillagerMetricsData>(villagerQuery.CalculateEntityCountWithoutFiltering(), Allocator.TempJob);

            var villagerMetricsJob = new VillagerMetricsJob
            {
                Metrics = villagerMetrics.AsParallelWriter()
            };
            state.Dependency = villagerMetricsJob.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            // Sum on main thread
            int villagerCount = villagerMetrics.Length;
            float totalMorale = 0f;
            float totalHunger = 0f;
            float totalEnergy = 0f;
            for (int i = 0; i < villagerMetrics.Length; i++)
            {
                var data = villagerMetrics[i];
                totalMorale += data.Morale;
                totalHunger += data.Hunger;
                totalEnergy += data.Energy;
            }

            villagerMetrics.Dispose();

            if (villagerCount > 0)
            {
                float avgMorale = totalMorale / villagerCount;
                float avgHunger = totalHunger / villagerCount;
                float avgEnergy = totalEnergy / villagerCount;

                TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyPopulationVillagers, Value = villagerCount, Unit = TelemetryMetricUnit.Count });
                TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyMoraleAverage, Value = avgMorale, Unit = TelemetryMetricUnit.Ratio });
                TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyHungerAverage, Value = avgHunger, Unit = TelemetryMetricUnit.Ratio });
                TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyEnergyAverage, Value = avgEnergy, Unit = TelemetryMetricUnit.Ratio });
            }

            // Collect resource metrics using jobified collection
            var resourceQuery = SystemAPI.QueryBuilder().WithAll<ResourceSourceConfig>().Build();
            var resourceMetrics = new NativeList<ResourceMetricsData>(resourceQuery.CalculateEntityCountWithoutFiltering(), Allocator.TempJob);

            var resourceMetricsJob = new ResourceMetricsJob
            {
                Metrics = resourceMetrics.AsParallelWriter()
            };
            state.Dependency = resourceMetricsJob.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            // Sum on main thread
            int resourceCount = resourceMetrics.Length;
            float totalGatherRate = 0f;
            for (int i = 0; i < resourceMetrics.Length; i++)
            {
                totalGatherRate += resourceMetrics[i].GatherRate;
            }

            resourceMetrics.Dispose();

            if (resourceCount > 0)
            {
                TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyResourcesCount, Value = resourceCount, Unit = TelemetryMetricUnit.Count });
                TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyResourcesTotalGatherRate, Value = totalGatherRate, Unit = TelemetryMetricUnit.None });
            }

            // Collect event queue metrics
            if (SystemAPI.TryGetSingletonEntity<EventQueue>(out var eventQueueEntity))
            {
                var eq = SystemAPI.GetComponent<EventQueue>(eventQueueEntity);
                if (state.EntityManager.HasBuffer<EventPayload>(eventQueueEntity))
                {
                    var eventBuffer = state.EntityManager.GetBuffer<EventPayload>(eventQueueEntity);
                    TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyEventBufferLen, Value = eventBuffer.Length, Unit = TelemetryMetricUnit.Count });
                }
                TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyEventProcessed, Value = eq.ProcessedEvents, Unit = TelemetryMetricUnit.Count });
                TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyEventDropped, Value = eq.DroppedEvents, Unit = TelemetryMetricUnit.Count });
            }

            // Event consumer metrics
            if (SystemAPI.TryGetSingleton<EventQueueConsumerStats>(out var consumerStats))
            {
                TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyEventConsumed, Value = consumerStats.ConsumedCount, Unit = TelemetryMetricUnit.Count });
                TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyEventConsumedLastTick, Value = consumerStats.LastTick, Unit = TelemetryMetricUnit.Count });
            }

            // Cache stats (global)
            if (SystemAPI.HasSingleton<CacheStats>())
            {
                var cacheStats = SystemAPI.GetSingleton<CacheStats>();
                TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyCacheLookups, Value = cacheStats.TotalLookups, Unit = TelemetryMetricUnit.Count });
                TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyCacheHits, Value = cacheStats.CacheHits, Unit = TelemetryMetricUnit.Count });
                TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyCacheMisses, Value = cacheStats.CacheMisses, Unit = TelemetryMetricUnit.Count });
                var hitRate = cacheStats.TotalLookups > 0 ? (float)cacheStats.CacheHits / cacheStats.TotalLookups : 0f;
                TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyCacheHitRate, Value = hitRate, Unit = TelemetryMetricUnit.Ratio });
            }

            // Collect tick-domain gating metrics (typically singleton, keep foreach for simplicity)
            foreach (var domain in SystemAPI.Query<RefRO<TickDomain>>())
            {
                var d = domain.ValueRO;
                var ticksUntilNext = (float)math.max(0, d.NextTick > timeState.Tick ? d.NextTick - timeState.Tick : 0);
                switch (d.DomainType)
                {
                    case TickDomainType.Cognitive:
                        TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyDomainCognitiveTicksUntilNext, Value = ticksUntilNext, Unit = TelemetryMetricUnit.Count });
                        break;
                    case TickDomainType.Economy:
                        TelemetryHub.Enqueue(new TelemetryMetric { Key = KeyDomainEconomyTicksUntilNext, Value = ticksUntilNext, Unit = TelemetryMetricUnit.Count });
                        break;
                }
            }

            // Update stream version
            stream.ValueRW.Version++;
            stream.ValueRW.LastTick = timeState.Tick;
        }

        private struct VillagerMetricsData
        {
            public float Morale;
            public float Hunger;
            public float Energy;
        }

        private struct ResourceMetricsData
        {
            public float GatherRate;
        }

        [BurstCompile]
        private partial struct VillagerMetricsJob : IJobEntity
        {
            public NativeList<VillagerMetricsData>.ParallelWriter Metrics;

            public void Execute(RefRO<VillagerNeeds> needs)
            {
                Metrics.AddNoResize(new VillagerMetricsData
                {
                    Morale = needs.ValueRO.MoraleFloat,
                    Hunger = needs.ValueRO.HungerFloat,
                    Energy = needs.ValueRO.EnergyFloat
                });
            }
        }

        [BurstCompile]
        private partial struct ResourceMetricsJob : IJobEntity
        {
            public NativeList<ResourceMetricsData>.ParallelWriter Metrics;

            public void Execute(RefRO<ResourceSourceConfig> source)
            {
                Metrics.AddNoResize(new ResourceMetricsData
                {
                    GatherRate = source.ValueRO.GatherRatePerWorker
                });
            }
        }

    }
}

