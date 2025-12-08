using PureDOTS.Runtime.Telemetry;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Telemetry
{
    /// <summary>
    /// Async aggregator thread for telemetry (second level of hierarchy).
    /// Aggregates metrics from local buffers across systems.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class TelemetryAggregatorSystem : SystemBase
    {
        private NativeList<TelemetryMetric> _drainBuffer;

        protected override void OnCreate()
        {
            RequireForUpdate<TickTimeState>();
            TelemetryHub.Initialize();
            _drainBuffer = new NativeList<TelemetryMetric>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            if (_drainBuffer.IsCreated)
            {
                _drainBuffer.Dispose();
            }
            TelemetryHub.Dispose();
        }

        protected override void OnUpdate()
        {
            // Early out if no telemetry stream
            if (!SystemAPI.TryGetSingletonEntity<TelemetryStream>(out var telemetryEntity))
            {
                return;
            }

            if (!SystemAPI.HasBuffer<TelemetryMetric>(telemetryEntity))
            {
                SystemAPI.GetEntityManager().AddBuffer<TelemetryMetric>(telemetryEntity);
            }

            var metrics = SystemAPI.GetBuffer<TelemetryMetric>(telemetryEntity);

            // Drain any pending local metrics from the hub into the global buffer.
            _drainBuffer.Clear();
            TelemetryHub.Drain(ref _drainBuffer);

            for (int i = 0; i < _drainBuffer.Length; i++)
            {
                metrics.Add(_drainBuffer[i]);
            }

            // Bump version/tick bookkeeping
            if (SystemAPI.TryGetSingletonRW<TelemetryStream>(out var telemetryStream))
            {
                telemetryStream.ValueRW.LastTick = SystemAPI.GetSingleton<TickTimeState>().Tick;
                telemetryStream.ValueRW.Version++;
            }
        }
    }
}
