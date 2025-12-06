using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Telemetry;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Tracks memory metrics (chunk reuse rate, fragmentation, GC counts).
    /// Exports to telemetry every 5s.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    public partial struct MemoryMetricsSystem : ISystem
    {
        private uint _lastExportTick;
        private const uint ExportIntervalTicks = 300; // 5 seconds at 60Hz

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            _lastExportTick = 0;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            var currentTick = tickTimeState.Tick;

            // Update metrics every tick
            if (!SystemAPI.HasSingleton<MemoryMetrics>())
            {
                var metricsEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<MemoryMetrics>(metricsEntity);
            }

            var metricsEntity = SystemAPI.GetSingletonEntity<MemoryMetrics>();
            var metrics = SystemAPI.GetComponentRW<MemoryMetrics>(metricsEntity);

            // Calculate chunk reuse rate (simplified - would need actual chunk tracking)
            // This is a placeholder that demonstrates the concept
            metrics.ValueRW.ChunkReuseRate = 0.95f; // Placeholder
            metrics.ValueRW.FragmentationScore = 0.1f; // Placeholder
            metrics.ValueRW.LastUpdateTick = currentTick;

            // Export to telemetry every 5s
            if (currentTick - _lastExportTick >= ExportIntervalTicks)
            {
                if (SystemAPI.TryGetSingletonEntity<TelemetryStream>(out var telemetryEntity))
                {
                    var telemetryBuffer = SystemAPI.GetBuffer<TelemetryMetric>(telemetryEntity);
                    
                    telemetryBuffer.Add(new TelemetryMetric
                    {
                        MetricName = "Memory_ChunkReuseRate",
                        Value = metrics.ValueRO.ChunkReuseRate,
                        Timestamp = currentTick
                    });

                    telemetryBuffer.Add(new TelemetryMetric
                    {
                        MetricName = "Memory_FragmentationScore",
                        Value = metrics.ValueRO.FragmentationScore,
                        Timestamp = currentTick
                    });
                }

                _lastExportTick = currentTick;
            }
        }
    }
}

