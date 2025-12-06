using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Telemetry;

namespace PureDOTS.Systems.Telemetry
{
    /// <summary>
    /// Serializes telemetry frames into NativeStream for async presentation polling.
    /// Presentation layer polls latest N frames asynchronously.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WorldMetricsCollectorSystem))]
    public partial struct TelemetryStreamingSystem : ISystem
    {
        private NativeStream _telemetryStream;
        private const int MaxFrames = 600; // 10 minutes at 1 Hz

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _telemetryStream = new NativeStream(MaxFrames, state.WorldUpdateAllocator);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<TelemetryStream>(out var telemetryEntity))
            {
                return;
            }

            if (!state.EntityManager.HasBuffer<TelemetryMetric>(telemetryEntity))
            {
                return;
            }

            var stream = SystemAPI.GetComponentRO<TelemetryStream>(telemetryEntity).ValueRO;
            var metrics = state.EntityManager.GetBuffer<TelemetryMetric>(telemetryEntity);

            // Write frame to stream
            var writer = _telemetryStream.AsWriter();
            writer.BeginForEachIndex(0);
            writer.Write(stream.LastTick);
            writer.Write(metrics.Length);

            for (int i = 0; i < metrics.Length; i++)
            {
                var metric = metrics[i];
                writer.Write(metric.Key);
                writer.Write(metric.Value);
                writer.Write(metric.Unit);
            }

            writer.EndForEachIndex();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_telemetryStream.IsCreated)
            {
                _telemetryStream.Dispose();
            }
        }

        /// <summary>
        /// Get the telemetry stream for presentation layer polling.
        /// </summary>
        public NativeStream GetTelemetryStream()
        {
            return _telemetryStream;
        }
    }
}

