using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PureDOTS.Runtime.Telemetry;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Runtime.Telemetry
{
    /// <summary>
    /// Managed file writer for telemetry data in JSON lines format (one object per tick).
    /// 
    /// See: Docs/Guides/DemoLockSystemsGuide.md#telemetry-export
    /// API Reference: Docs/Guides/DemoLockSystemsAPI.md#telemetry-export-api
    /// </summary>
    public class TelemetryFileWriter : IDisposable
    {
        private StreamWriter _writer;
        private bool _disposed;

        public TelemetryFileWriter(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _writer = new StreamWriter(filePath, append: true, Encoding.UTF8);
        }

        public void WriteTick(
            uint tick,
            TelemetryStream telemetryStream,
            NativeArray<TelemetryMetric> metrics,
            NativeArray<FrameTimingSample> frameTimings,
            AllocationDiagnostics allocationDiagnostics,
            uint simulationHash = 0)
        {
            if (_disposed || _writer == null)
                return;

            var entry = new TelemetryEntry
            {
                Tick = tick,
                Timestamp = DateTime.UtcNow.ToString("O"),
                TelemetryStream = telemetryStream,
                Metrics = new List<TelemetryMetricData>(),
                FrameTimings = new List<FrameTimingData>(),
                AllocationDiagnostics = allocationDiagnostics,
                SimulationHash = simulationHash
            };

            // Convert metrics
            for (int i = 0; i < metrics.Length; i++)
            {
                var metric = metrics[i];
                entry.Metrics.Add(new TelemetryMetricData
                {
                    Key = metric.Key.ToString(),
                    Value = metric.Value,
                    Unit = metric.Unit.ToString()
                });
            }

            // Convert frame timings
            for (int i = 0; i < frameTimings.Length; i++)
            {
                var timing = frameTimings[i];
                entry.FrameTimings.Add(new FrameTimingData
                {
                    Group = timing.Group.ToString(),
                    DurationMs = timing.DurationMs,
                    BudgetMs = timing.BudgetMs,
                    Flags = timing.Flags.ToString(),
                    SystemCount = timing.SystemCount
                });
            }

            // Write as JSON line
            var json = JsonUtility.ToJson(entry);
            _writer.WriteLine(json);
            _writer.Flush();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _writer?.Dispose();
            _writer = null;
            _disposed = true;
        }

        [Serializable]
        private class TelemetryEntry
        {
            public uint Tick;
            public string Timestamp;
            public TelemetryStream TelemetryStream;
            public List<TelemetryMetricData> Metrics;
            public List<FrameTimingData> FrameTimings;
            public AllocationDiagnostics AllocationDiagnostics;
            public uint SimulationHash;
        }

        [Serializable]
        private class TelemetryMetricData
        {
            public string Key;
            public float Value;
            public string Unit;
        }

        [Serializable]
        private class FrameTimingData
        {
            public string Group;
            public float DurationMs;
            public float BudgetMs;
            public string Flags;
            public int SystemCount;
        }
    }
}

