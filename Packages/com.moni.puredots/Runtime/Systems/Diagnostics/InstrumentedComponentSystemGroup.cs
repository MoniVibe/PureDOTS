using System;
using System.Collections.Generic;
using System.Diagnostics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Telemetry;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Systems
{
    /// <summary>
    /// ComponentSystemGroup variant that logs the specific system name whenever an exception is thrown.
    /// This makes it possible to identify which unmanaged system triggered a Burst exception without
    /// modifying Unity packages or disabling Burst globally.
    /// </summary>
    public abstract unsafe partial class InstrumentedComponentSystemGroup : ComponentSystemGroup
    {
        private const int DefaultSystemTimingTopN = 8;

        private struct SystemTimingSample
        {
            public string Name;
            public float DurationMs;
        }

        private List<SystemTimingSample> _systemTimingSamples;
        private Stopwatch _systemTimingStopwatch;

        protected virtual bool EmitSystemTimings => false;
        protected virtual string SystemTimingMetricPrefix => "timing.system.";
        protected virtual int SystemTimingTopN => DefaultSystemTimingTopN;

        private static string ResolveSystemName(World world, SystemHandle handle)
        {
            if (handle.Equals(SystemHandle.Null))
            {
                return "<null system>";
            }

            try
            {
                ref var state = ref world.Unmanaged.ResolveSystemStateRef(handle);
                var debugName = state.DebugName;
                if (!debugName.IsEmpty)
                {
                    return debugName.ToString();
                }
            }
            catch
            {
                // If DebugName cannot be materialized, fall back to the hash representation.
            }

            return $"SystemHandle(0x{handle.GetHashCode():X})";
        }

        private static string TrimSystemName(string systemName)
        {
            if (string.IsNullOrWhiteSpace(systemName))
            {
                return "UnknownSystem";
            }

            var lastDot = systemName.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < systemName.Length - 1)
            {
                return systemName[(lastDot + 1)..];
            }

            return systemName;
        }

        private void EnsureSystemTimingBuffers()
        {
            _systemTimingSamples ??= new List<SystemTimingSample>(16);
            _systemTimingStopwatch ??= new Stopwatch();
        }

        private bool TryPrepareSystemTimingMetrics(out DynamicBuffer<TelemetryMetric> metrics)
        {
            metrics = default;

            if (!EmitSystemTimings)
            {
                return false;
            }

            if (!SystemAPI.TryGetSingleton<TelemetryExportConfig>(out var exportConfig))
            {
                return false;
            }

            if (exportConfig.Enabled == 0 || (exportConfig.Flags & TelemetryExportFlags.IncludeTelemetryMetrics) == 0)
            {
                return false;
            }

            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
            {
                return false;
            }

            var cadence = exportConfig.CadenceTicks > 0 ? exportConfig.CadenceTicks : 30u;
            if (cadence > 1u && timeState.Tick % cadence != 0u)
            {
                return false;
            }

            if (!SystemAPI.HasSingleton<TelemetryStream>())
            {
                return false;
            }

            var telemetryEntity = SystemAPI.GetSingletonEntity<TelemetryStream>();
            if (!EntityManager.HasBuffer<TelemetryMetric>(telemetryEntity))
            {
                EntityManager.AddBuffer<TelemetryMetric>(telemetryEntity);
            }

            metrics = EntityManager.GetBuffer<TelemetryMetric>(telemetryEntity);
            return true;
        }

        private void EmitSystemTimingMetrics(DynamicBuffer<TelemetryMetric> metrics)
        {
            if (_systemTimingSamples == null || _systemTimingSamples.Count == 0)
            {
                return;
            }

            _systemTimingSamples.Sort((a, b) => b.DurationMs.CompareTo(a.DurationMs));
            var limit = SystemTimingTopN > 0
                ? Math.Min(SystemTimingTopN, _systemTimingSamples.Count)
                : _systemTimingSamples.Count;

            for (var i = 0; i < limit; i++)
            {
                var sample = _systemTimingSamples[i];
                var shortName = TrimSystemName(sample.Name);
                var keyString = $"{SystemTimingMetricPrefix}{shortName}";
                if (keyString.Length > 63)
                {
                    keyString = keyString.Substring(0, 63);
                }

                metrics.AddMetric(new FixedString64Bytes(keyString), sample.DurationMs, TelemetryMetricUnit.DurationMilliseconds);
            }
        }

        private void UpdateAllWithDiagnostics()
        {
            SortSystems();

            using var systems = GetAllSystems(Allocator.Temp);
            var world = World.Unmanaged;
            var collectSystemTimings = TryPrepareSystemTimingMetrics(out var telemetryMetrics);
            if (collectSystemTimings)
            {
                EnsureSystemTimingBuffers();
                _systemTimingSamples.Clear();
            }

            for (int i = 0; i < systems.Length; ++i)
            {
                var handle = systems[i];
                try
                {
                    if (collectSystemTimings)
                    {
                        _systemTimingStopwatch.Restart();
                    }

                    handle.Update(world);
                }
                catch (Exception ex)
                {
                    var systemName = ResolveSystemName(World, handle);
                    UnityEngine.Debug.LogError($"[{GetType().Name}] Exception while updating {systemName}. See stack trace below.");
                    UnityEngine.Debug.LogException(ex);
                }
                finally
                {
                    if (collectSystemTimings)
                    {
                        _systemTimingStopwatch.Stop();
                        _systemTimingSamples.Add(new SystemTimingSample
                        {
                            Name = ResolveSystemName(World, handle),
                            DurationMs = (float)_systemTimingStopwatch.Elapsed.TotalMilliseconds
                        });
                    }
                }

                if (World.QuitUpdate)
                {
                    break;
                }
            }

            if (collectSystemTimings)
            {
                EmitSystemTimingMetrics(telemetryMetrics);
            }
        }

        protected override void OnUpdate()
        {
            if (RateManager == null)
            {
                UpdateAllWithDiagnostics();
            }
            else
            {
                while (RateManager.ShouldGroupUpdate(this))
                {
                    UpdateAllWithDiagnostics();
                }
            }
        }
    }
}
