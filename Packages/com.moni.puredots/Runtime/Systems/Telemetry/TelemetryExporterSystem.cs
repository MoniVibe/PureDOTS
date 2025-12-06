using System.IO;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Telemetry;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Systems.Telemetry
{
    /// <summary>
    /// Telemetry exporter that writes metrics to file in JSON lines format.
    /// Runs in PresentationSystemGroup, exports data every tick when enabled.
    /// </summary>
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    public partial struct TelemetryExportSystem : ISystem
    {
        private TelemetryFileWriter _fileWriter;
        private EntityQuery _telemetryQuery;
        private EntityQuery _frameTimingQuery;
        private bool _exportEnabled;
        private string _currentFilePath;

        public void OnCreate(ref SystemState state)
        {
            _telemetryQuery = SystemAPI.QueryBuilder()
                .WithAll<TelemetryStream, TelemetryMetric>()
                .Build();

            _frameTimingQuery = SystemAPI.QueryBuilder()
                .WithAll<FrameTimingStream, FrameTimingSample>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Check if export is enabled (can be toggled via DebugDisplayData in future)
            // For now, export is always enabled when telemetry stream exists
            _exportEnabled = true;

            if (!SystemAPI.HasSingleton<TelemetryStream>())
            {
                return;
            }

            var telemetryStream = SystemAPI.GetSingleton<TelemetryStream>();
            var timeState = SystemAPI.GetSingleton<TimeState>();

            // Initialize file writer if needed
            if (_fileWriter == null && _exportEnabled)
            {
                var timestamp = System.DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                _currentFilePath = Path.Combine(Application.persistentDataPath, "Telemetry", $"metrics_{timestamp}.jsonl");
                _fileWriter = new TelemetryFileWriter(_currentFilePath);
            }

            if (!_exportEnabled || _fileWriter == null)
            {
                return;
            }

            // Collect metrics
            var metricsList = new NativeList<TelemetryMetric>(Allocator.Temp);
            var frameTimingsList = new NativeList<FrameTimingSample>(Allocator.Temp);
            var allocationDiagnostics = default(AllocationDiagnostics);

            // Get telemetry metrics
            if (_telemetryQuery.CalculateEntityCount() > 0)
            {
                var telemetryEntity = SystemAPI.GetSingletonEntity<TelemetryStream>();
                if (SystemAPI.HasBuffer<TelemetryMetric>(telemetryEntity))
                {
                    var metrics = SystemAPI.GetBuffer<TelemetryMetric>(telemetryEntity);
                    for (int i = 0; i < metrics.Length; i++)
                    {
                        metricsList.Add(metrics[i]);
                    }
                }
            }

            // Get frame timings
            if (_frameTimingQuery.CalculateEntityCount() > 0)
            {
                var frameTimingEntity = SystemAPI.GetSingletonEntity<FrameTimingStream>();
                if (SystemAPI.HasBuffer<FrameTimingSample>(frameTimingEntity))
                {
                    var timings = SystemAPI.GetBuffer<FrameTimingSample>(frameTimingEntity);
                    for (int i = 0; i < timings.Length; i++)
                    {
                        frameTimingsList.Add(timings[i]);
                    }
                }
            }

            // Get allocation diagnostics if available
            if (SystemAPI.HasSingleton<AllocationDiagnostics>())
            {
                allocationDiagnostics = SystemAPI.GetSingleton<AllocationDiagnostics>();
            }

            // Write tick data
            _fileWriter.WriteTick(
                timeState.Tick,
                telemetryStream,
                metricsList.AsArray(),
                frameTimingsList.AsArray(),
                allocationDiagnostics,
                simulationHash: 0); // TODO: Calculate simulation hash

            metricsList.Dispose();
            frameTimingsList.Dispose();
        }

        public void OnDestroy(ref SystemState state)
        {
            _fileWriter?.Dispose();
            _fileWriter = null;
        }
    }
}

