using System.Collections.Generic;
using System.IO;
using System.Text;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;
using LateSimulationSystemGroup = PureDOTS.Systems.LateSimulationSystemGroup;

namespace Space4X.Systems
{
    /// <summary>
    /// Emits performance traces as CSV per scenario.
    /// Tracks phase times, job counts, ECB playback times.
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct Space4XPerformanceTraceSystem : ISystem
    {
        private static readonly ProfilerMarker UpdateMarker = new("Space4XPerformanceTrace.Update");
        private static List<PerformanceTraceEntry> _traceEntries = new();

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            using (UpdateMarker.Auto())
            {
                var timeState = SystemAPI.GetSingleton<TimeState>();
                
                // Collect trace data
                var entry = new PerformanceTraceEntry
                {
                    Tick = timeState.Tick,
                    FixedTickMs = timeState.FixedDeltaTime * 1000f,
                    // Would collect actual phase times, job counts, ECB playback times
                    PhaseTimeMs = 0f,
                    JobCount = 0,
                    ECBPlaybackMs = 0f
                };

                _traceEntries.Add(entry);

                // Flush to CSV periodically
                if (_traceEntries.Count >= 1000)
                {
                    FlushTraceToCSV();
                    _traceEntries.Clear();
                }
            }
        }

        public static void FlushTraceToCSV()
        {
            if (_traceEntries.Count == 0) return;

            var csv = new StringBuilder();
            csv.AppendLine("Tick,FixedTickMs,PhaseTimeMs,JobCount,ECBPlaybackMs");

            foreach (var entry in _traceEntries)
            {
                csv.AppendLine($"{entry.Tick},{entry.FixedTickMs:F3},{entry.PhaseTimeMs:F3},{entry.JobCount},{entry.ECBPlaybackMs:F3}");
            }

            var directory = Path.Combine(Application.dataPath, "..", "CI/TestResults/Artifacts");
            Directory.CreateDirectory(directory);
            var filepath = Path.Combine(directory, $"perf_trace_{System.DateTime.UtcNow:yyyyMMddHHmmss}.csv");
            File.WriteAllText(filepath, csv.ToString());

            Debug.Log($"[Space4XPerformanceTraceSystem] Flushed {_traceEntries.Count} trace entries to {filepath}");
        }

        private struct PerformanceTraceEntry
        {
            public uint Tick;
            public float FixedTickMs;
            public float PhaseTimeMs;
            public int JobCount;
            public float ECBPlaybackMs;
        }
    }
}

