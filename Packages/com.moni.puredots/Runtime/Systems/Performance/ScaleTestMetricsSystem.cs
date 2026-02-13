using System;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Rendering;
using PureDOTS.Runtime.Scenarios;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace PureDOTS.Runtime.Systems.Performance
{
    /// <summary>
    /// Collects performance metrics during scale test scenarios.
    /// Runs in SimulationSystemGroup to measure simulation performance.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct ScaleTestMetricsSystem : ISystem
    {
        private const int MaxTickSamples = 2048;

        private double _lastTickStartTime;
        private bool _initialized;
        private EntityQuery _villagerQuery;
        private EntityQuery _resourceChunkQuery;
        private EntityQuery _aggregateQuery;

        private FixedString64Bytes _averageTickTimeMsKey;
        private FixedString64Bytes _maxTickTimeMsKey;
        private FixedString64Bytes _p95TickTimeMsKey;
        private FixedString64Bytes _p99TickTimeMsKey;
        private FixedString64Bytes _peakMemoryMbKey;
        private FixedString64Bytes _totalEntitiesKey;
        private FixedString64Bytes _targetTickTimeMsKey;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScaleTestMetricsConfig>();

            _villagerQuery = state.GetEntityQuery(ComponentType.ReadOnly<VillagerId>());
            _resourceChunkQuery = state.GetEntityQuery(ComponentType.ReadOnly<ResourceChunkState>());
            _aggregateQuery = state.GetEntityQuery(ComponentType.ReadOnly<AggregateTag>());

            _averageTickTimeMsKey = new FixedString64Bytes("averageTickTimeMs");
            _maxTickTimeMsKey = new FixedString64Bytes("maxTickTimeMs");
            _p95TickTimeMsKey = new FixedString64Bytes("p95TickTimeMs");
            _p99TickTimeMsKey = new FixedString64Bytes("p99TickTimeMs");
            _peakMemoryMbKey = new FixedString64Bytes("peakMemoryMB");
            _totalEntitiesKey = new FixedString64Bytes("totalEntities");
            _targetTickTimeMsKey = new FixedString64Bytes("targetTickTimeMs");
        }

        public void OnUpdate(ref SystemState state)
        {
            var currentTime = UnityEngine.Time.realtimeSinceStartupAsDouble;

            if (!_initialized)
            {
                _lastTickStartTime = currentTime;
                _initialized = true;

                if (!SystemAPI.TryGetSingleton<ScaleTestMetrics>(out _))
                {
                    var entity = state.EntityManager.CreateEntity();
                    state.EntityManager.AddComponentData(entity, new ScaleTestMetrics
                    {
                        MinTickTime = float.MaxValue,
                        MaxTickTime = 0f,
                        SampleCount = 0,
                        SampleWriteIndex = 0
                    });
                    state.EntityManager.AddBuffer<TickTimeSample>(entity);
                }

                return;
            }

            var tickTime = (float)((currentTime - _lastTickStartTime) * 1000.0);
            _lastTickStartTime = currentTime;

            if (!SystemAPI.TryGetSingleton<ScaleTestMetricsConfig>(out var config))
            {
                return;
            }

            var tick = 0u;
            if (SystemAPI.TryGetSingleton<TimeState>(out var timeState))
            {
                tick = timeState.Tick;
            }

            if (config.SampleInterval > 0 && tick % config.SampleInterval != 0)
            {
                return;
            }

            var scenarioEntity = ResolveScenarioEntity();
            var canWriteScenarioMetrics = scenarioEntity != Entity.Null;
            var metricLookup = default(BufferLookup<ScenarioMetricSample>);
            if (canWriteScenarioMetrics)
            {
                metricLookup = SystemAPI.GetBufferLookup<ScenarioMetricSample>(isReadOnly: false);
                metricLookup.Update(ref state);
                canWriteScenarioMetrics = metricLookup.HasBuffer(scenarioEntity);
            }

            foreach (var (metrics, samples) in
                SystemAPI.Query<RefRW<ScaleTestMetrics>, DynamicBuffer<TickTimeSample>>())
            {
                metrics.ValueRW.CurrentTick = tick;
                metrics.ValueRW.TotalTickTime = tickTime;
                metrics.ValueRW.SampleCount++;
                metrics.ValueRW.SumTickTime += tickTime;
                metrics.ValueRW.SumSquaredTickTime += tickTime * tickTime;
                metrics.ValueRW.AverageTickTime = metrics.ValueRO.SumTickTime / metrics.ValueRO.SampleCount;
                metrics.ValueRW.MaxTickTime = math.max(metrics.ValueRO.MaxTickTime, tickTime);
                metrics.ValueRW.MinTickTime = math.min(metrics.ValueRO.MinTickTime, tickTime);

                UpdateTickSamples(ref metrics.ValueRW, samples, tickTime, tick);
                ComputePercentiles(samples, out var p95, out var p99);
                metrics.ValueRW.P95TickTime = p95;
                metrics.ValueRW.P99TickTime = p99;

                CountEntities(ref state, ref metrics.ValueRW);

                if (config.CollectMemoryStats != 0)
                {
                    SampleMemory(ref metrics.ValueRW);
                }

                if (canWriteScenarioMetrics)
                {
                    PublishScenarioMetrics(ref metricLookup, scenarioEntity, in metrics.ValueRO, in config);
                }

                if (config.LogInterval > 0 && tick % config.LogInterval == 0)
                {
                    LogMetrics(in metrics.ValueRO, tick, in config);
                }
            }
        }

        private Entity ResolveScenarioEntity()
        {
            if (SystemAPI.TryGetSingleton<ScenarioEntitySingleton>(out var singleton) &&
                singleton.Value != Entity.Null)
            {
                return singleton.Value;
            }

            return SystemAPI.HasSingleton<ScenarioInfo>()
                ? SystemAPI.GetSingletonEntity<ScenarioInfo>()
                : Entity.Null;
        }

        private void CountEntities(ref SystemState state, ref ScaleTestMetrics metrics)
        {
            metrics.VillagerCount = _villagerQuery.CalculateEntityCount();
            metrics.ResourceChunkCount = _resourceChunkQuery.CalculateEntityCount();
            metrics.AggregateCount = _aggregateQuery.CalculateEntityCount();
            metrics.TotalEntityCount = state.EntityManager.UniversalQuery.CalculateEntityCount();

            metrics.ProjectileCount = 0;
            metrics.CarrierCount = 0;
        }

        private static void SampleMemory(ref ScaleTestMetrics metrics)
        {
            var totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
            var monoUsed = Profiler.GetMonoUsedSizeLong();
            var gcManaged = GC.GetTotalMemory(false);
            var managedRough = monoUsed > gcManaged ? monoUsed : gcManaged;
            var nativeRough = totalAllocated - managedRough;
            if (nativeRough < 0)
            {
                nativeRough = 0;
            }

            metrics.TotalMemoryBytes = totalAllocated;
            metrics.ChunkMemoryBytes = nativeRough;
            if (totalAllocated > metrics.PeakMemoryBytes)
            {
                metrics.PeakMemoryBytes = totalAllocated;
            }
        }

        private static void UpdateTickSamples(ref ScaleTestMetrics metrics, DynamicBuffer<TickTimeSample> samples, float tickTimeMs, uint tick)
        {
            var sample = new TickTimeSample
            {
                TickTimeMs = tickTimeMs,
                Tick = tick
            };

            if (samples.Length < MaxTickSamples)
            {
                samples.Add(sample);
            }
            else
            {
                var index = metrics.SampleWriteIndex % MaxTickSamples;
                samples[index] = sample;
            }

            metrics.SampleWriteIndex = (metrics.SampleWriteIndex + 1) % MaxTickSamples;
        }

        private static void ComputePercentiles(in DynamicBuffer<TickTimeSample> samples, out float p95, out float p99)
        {
            p95 = 0f;
            p99 = 0f;

            if (samples.Length == 0)
            {
                return;
            }

            var values = new NativeArray<float>(samples.Length, Allocator.Temp);
            try
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    values[i] = samples[i].TickTimeMs;
                }

                values.Sort();
                var p95Index = math.clamp((int)math.ceil(samples.Length * 0.95f) - 1, 0, samples.Length - 1);
                var p99Index = math.clamp((int)math.ceil(samples.Length * 0.99f) - 1, 0, samples.Length - 1);
                p95 = values[p95Index];
                p99 = values[p99Index];
            }
            finally
            {
                values.Dispose();
            }
        }

        private void PublishScenarioMetrics(
            ref BufferLookup<ScenarioMetricSample> metricLookup,
            in Entity scenarioEntity,
            in ScaleTestMetrics metrics,
            in ScaleTestMetricsConfig config)
        {
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _averageTickTimeMsKey, metrics.AverageTickTime);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _maxTickTimeMsKey, metrics.MaxTickTime);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _p95TickTimeMsKey, metrics.P95TickTime);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _p99TickTimeMsKey, metrics.P99TickTime);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _totalEntitiesKey, metrics.TotalEntityCount);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _targetTickTimeMsKey, config.TargetTickTimeMs);

            var peakMemoryMb = metrics.PeakMemoryBytes / (1024.0 * 1024.0);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _peakMemoryMbKey, peakMemoryMb);
        }

        private static void LogMetrics(in ScaleTestMetrics metrics, uint tick, in ScaleTestMetricsConfig config)
        {
            var budgetStatus = metrics.TotalTickTime <= config.TargetTickTimeMs ? "OK" : "OVER";
            var peakMemoryMb = metrics.PeakMemoryBytes / (1024.0 * 1024.0);

            Debug.Log($"[ScaleTest] Tick {tick}: " +
                      $"TickTime={metrics.TotalTickTime:F2}ms (avg={metrics.AverageTickTime:F2}ms, p95={metrics.P95TickTime:F2}ms, max={metrics.MaxTickTime:F2}ms) [{budgetStatus}] | " +
                      $"PeakMemory={peakMemoryMb:F1}MB | " +
                      $"Entities: Villagers={metrics.VillagerCount}, Resources={metrics.ResourceChunkCount}, " +
                      $"Aggregates={metrics.AggregateCount}, Total={metrics.TotalEntityCount}");
        }
    }

    /// <summary>
    /// Validates performance metrics against budgets and logs warnings.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(ScaleTestMetricsSystem))]
    public partial struct PerformanceBudgetValidationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PerformanceBudget>();
            state.RequireForUpdate<ScaleTestMetrics>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<PerformanceBudget>(out var budget))
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<ScaleTestMetrics>(out var metrics))
            {
                return;
            }

            // Only validate periodically (every 100 ticks)
            if (metrics.CurrentTick % 100 != 0)
            {
                return;
            }

            // Check tick time budget
            if (metrics.TotalTickTime > budget.MaxTickTimeMs)
            {
                Debug.LogWarning($"[PerformanceBudget] Tick time {metrics.TotalTickTime:F2}ms exceeds budget {budget.MaxTickTimeMs:F2}ms");
            }

            // Check average tick time
            if (metrics.AverageTickTime > budget.MaxTickTimeMs * 0.8f)
            {
                Debug.LogWarning($"[PerformanceBudget] Average tick time {metrics.AverageTickTime:F2}ms approaching budget {budget.MaxTickTimeMs:F2}ms");
            }

            // Check entity counts
            if (metrics.VillagerCount > 100000)
            {
                Debug.LogWarning($"[PerformanceBudget] Villager count ({metrics.VillagerCount}) exceeds recommended 100k. Consider LOD/aggregation.");
            }
        }
    }

    /// <summary>
    /// Collects LOD debug metrics when enabled.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(ScaleTestMetricsSystem))]
    public partial struct LODDebugMetricsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScaleTestMetricsConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ScaleTestMetricsConfig>(out var config))
            {
                return;
            }

            if (config.EnableLODDebug == 0)
            {
                return;
            }

            var tick = 0u;
            if (SystemAPI.TryGetSingleton<TimeState>(out var timeState))
            {
                tick = timeState.Tick;
            }

            // Only collect periodically
            if (config.SampleInterval > 0 && tick % config.SampleInterval != 0)
            {
                return;
            }

            // Initialize or get debug metrics singleton
            if (!SystemAPI.TryGetSingleton<LODDebugMetrics>(out _))
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, new LODDebugMetrics());
            }

            foreach (var lodMetrics in SystemAPI.Query<RefRW<LODDebugMetrics>>())
            {
                CollectLODMetrics(ref state, ref lodMetrics.ValueRW, tick, config);
            }
        }

        private void CollectLODMetrics(ref SystemState state, ref LODDebugMetrics metrics, uint tick, ScaleTestMetricsConfig config)
        {
            metrics.LOD0Count = 0;
            metrics.LOD1Count = 0;
            metrics.LOD2Count = 0;
            metrics.LOD3Count = 0;
            metrics.ShouldRenderCount = 0;
            metrics.CulledCount = 0;

            float sumDistance = 0f;
            float sumImportance = 0f;
            int lodDataCount = 0;

            // Count LOD levels
            foreach (var lodData in SystemAPI.Query<RefRO<PureDOTS.Runtime.Rendering.RenderLODData>>())
            {
                var lod = lodData.ValueRO.RecommendedLOD;
                if (lod == 0) metrics.LOD0Count++;
                else if (lod == 1) metrics.LOD1Count++;
                else if (lod == 2) metrics.LOD2Count++;
                else if (lod >= 3) metrics.LOD3Count++;

                sumDistance += lodData.ValueRO.CameraDistance;
                sumImportance += lodData.ValueRO.ImportanceScore;
                lodDataCount++;
            }

            // Count render density
            foreach (var sample in SystemAPI.Query<RefRO<PureDOTS.Runtime.Rendering.RenderSampleIndex>>())
            {
                if (sample.ValueRO.ShouldRender != 0)
                {
                    metrics.ShouldRenderCount++;
                }
            }

            // Count culled
            foreach (var cullable in SystemAPI.Query<RefRO<PureDOTS.Runtime.Rendering.RenderCullable>>())
            {
                // Note: Actual culling check would require camera distance
                // This is a placeholder for the count
                metrics.CulledCount++;
            }

            if (lodDataCount > 0)
            {
                metrics.AverageCameraDistance = sumDistance / lodDataCount;
                metrics.AverageImportanceScore = sumImportance / lodDataCount;
            }

            // Log if interval reached
            if (config.LogInterval > 0 && tick % config.LogInterval == 0)
            {
                Debug.Log($"[LODDebug] Tick {tick}: " +
                          $"LOD0={metrics.LOD0Count}, LOD1={metrics.LOD1Count}, LOD2={metrics.LOD2Count}, LOD3={metrics.LOD3Count} | " +
                          $"ShouldRender={metrics.ShouldRenderCount}, AvgDistance={metrics.AverageCameraDistance:F1}, AvgImportance={metrics.AverageImportanceScore:F2}");
            }
        }
    }

    /// <summary>
    /// Collects aggregate debug metrics when enabled.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(ScaleTestMetricsSystem))]
    public partial struct AggregateDebugMetricsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScaleTestMetricsConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ScaleTestMetricsConfig>(out var config))
            {
                return;
            }

            if (config.EnableAggregateDebug == 0)
            {
                return;
            }

            var tick = 0u;
            if (SystemAPI.TryGetSingleton<TimeState>(out var timeState))
            {
                tick = timeState.Tick;
            }

            // Only collect periodically
            if (config.SampleInterval > 0 && tick % config.SampleInterval != 0)
            {
                return;
            }

            // Initialize or get debug metrics singleton
            if (!SystemAPI.TryGetSingleton<AggregateDebugMetrics>(out _))
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, new AggregateDebugMetrics());
            }

            foreach (var aggMetrics in SystemAPI.Query<RefRW<AggregateDebugMetrics>>())
            {
                CollectAggregateMetrics(ref state, ref aggMetrics.ValueRW, tick, config);
            }
        }

        private void CollectAggregateMetrics(ref SystemState state, ref AggregateDebugMetrics metrics, uint tick, ScaleTestMetricsConfig config)
        {
            metrics.AggregateCount = 0;
            metrics.TotalMemberCount = 0;
            metrics.MinMembersPerAggregate = int.MaxValue;
            metrics.MaxMembersPerAggregate = 0;

            float sumHealth = 0f;
            float sumStrength = 0f;
            uint lastUpdateTick = 0;

            // Count aggregates and members
            foreach (var (summary, stateComp) in 
                SystemAPI.Query<RefRO<PureDOTS.Runtime.Rendering.AggregateRenderSummary>, RefRO<PureDOTS.Runtime.Rendering.AggregateState>>())
            {
                metrics.AggregateCount++;
                var memberCount = summary.ValueRO.MemberCount;
                metrics.TotalMemberCount += memberCount;
                metrics.MinMembersPerAggregate = math.min(metrics.MinMembersPerAggregate, memberCount);
                metrics.MaxMembersPerAggregate = math.max(metrics.MaxMembersPerAggregate, memberCount);

                sumHealth += summary.ValueRO.TotalHealth;
                sumStrength += summary.ValueRO.TotalStrength;
                lastUpdateTick = math.max(lastUpdateTick, stateComp.ValueRO.LastAggregationTick);
            }

            if (metrics.AggregateCount > 0)
            {
                metrics.AverageMembersPerAggregate = metrics.TotalMemberCount / metrics.AggregateCount;
                metrics.AverageTotalHealth = sumHealth / metrics.AggregateCount;
                metrics.AverageTotalStrength = sumStrength / metrics.AggregateCount;
            }
            else
            {
                metrics.MinMembersPerAggregate = 0;
            }

            metrics.LastAggregationUpdateTick = lastUpdateTick;

            // Log if interval reached
            if (config.LogInterval > 0 && tick % config.LogInterval == 0)
            {
                Debug.Log($"[AggregateDebug] Tick {tick}: " +
                          $"Aggregates={metrics.AggregateCount}, TotalMembers={metrics.TotalMemberCount}, " +
                          $"AvgMembers={metrics.AverageMembersPerAggregate}, Range=[{metrics.MinMembersPerAggregate}, {metrics.MaxMembersPerAggregate}] | " +
                          $"AvgHealth={metrics.AverageTotalHealth:F1}, AvgStrength={metrics.AverageTotalStrength:F1}");
            }
        }
    }
}
