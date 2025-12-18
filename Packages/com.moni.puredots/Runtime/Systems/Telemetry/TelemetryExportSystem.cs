using System;
using System.Globalization;
using System.IO;
using System.Text;
using PureDOTS.Runtime;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;

namespace PureDOTS.Systems.Telemetry
{
    /// <summary>
    /// Shared NDJSON exporter that flushes telemetry metrics, frame timing samples,
    /// behavior telemetry records, and replay metadata to a single stream.
    /// </summary>
    [UpdateInGroup(typeof(PureDotsPresentationSystemGroup))]
    public partial class TelemetryExportSystem : SystemBase
    {
        private bool _headerWritten;
        private uint _knownConfigVersion;
        private FixedString128Bytes _runIdCache;
        private string _runIdString;
        private string _activePath;

        protected override void OnCreate()
        {
            RequireForUpdate<TelemetryExportConfig>();
            _activePath = string.Empty;
        }

        protected override void OnDestroy()
        {
        }

        protected override void OnUpdate()
        {
            var configEntity = SystemAPI.GetSingletonEntity<TelemetryExportConfig>();
            var config = SystemAPI.GetComponent<TelemetryExportConfig>(configEntity);

            if (config.Enabled == 0 || config.OutputPath.Length == 0)
            {
                _headerWritten = false;
                return;
            }

            if (config.RunId.Length == 0)
            {
                config.RunId = GenerateRunId();
                config.Version++;
                SystemAPI.SetComponent(configEntity, config);
            }

            if (_knownConfigVersion != config.Version)
            {
                _knownConfigVersion = config.Version;
                _headerWritten = false;
                _runIdCache = config.RunId;
                _runIdString = _runIdCache.ToString();
                _activePath = config.OutputPath.ToString();
            }

            if (string.IsNullOrEmpty(_activePath))
            {
                return;
            }

            try
            {
                EnsureDirectory(_activePath);
                using var fileStream = new FileStream(_activePath, FileMode.Append, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(fileStream, Encoding.UTF8);

                if (!_headerWritten)
                {
                    WriteRunHeader(writer, config);
                    _headerWritten = true;
                }

                var tick = GetCurrentTick();

                if ((config.Flags & TelemetryExportFlags.IncludeTelemetryMetrics) != 0)
                {
                    ExportTelemetryMetrics(writer, tick);
                }

                if ((config.Flags & TelemetryExportFlags.IncludeFrameTiming) != 0)
                {
                    ExportFrameTiming(writer, tick);
                }

                if ((config.Flags & TelemetryExportFlags.IncludeBehaviorTelemetry) != 0)
                {
                    ExportBehaviorTelemetry(writer);
                }

                if ((config.Flags & TelemetryExportFlags.IncludeReplayEvents) != 0)
                {
                    ExportReplayTelemetry(writer, tick);
                }

                if ((config.Flags & TelemetryExportFlags.IncludeTelemetryEvents) != 0)
                {
                    ExportTelemetryEvents(writer);
                }

                writer.Flush();
            }
            catch (Exception ex)
            {
                UnityDebug.LogError($"[TelemetryExportSystem] Failed to export telemetry to '{_activePath}': {ex}");
            }
        }

        private static FixedString128Bytes GenerateRunId()
        {
            FixedString128Bytes id = default;
            var guid = Guid.NewGuid().ToString("N");
            for (int i = 0; i < guid.Length && i < id.Capacity; i++)
            {
                id.Append(guid[i]);
            }
            return id;
        }

        private static void EnsureDirectory(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private uint GetCurrentTick()
        {
            if (SystemAPI.TryGetSingleton<TimeState>(out var timeState))
            {
                return timeState.Tick;
            }

            if (SystemAPI.TryGetSingleton<TickTimeState>(out var tickState))
            {
                return tickState.Tick;
            }

            return 0;
        }

        private void ExportTelemetryMetrics(StreamWriter writer, uint tick)
        {
            if (!SystemAPI.TryGetSingletonEntity<TelemetryStream>(out var telemetryEntity))
            {
                return;
            }

            if (!EntityManager.HasBuffer<TelemetryMetric>(telemetryEntity))
            {
                return;
            }

            var buffer = EntityManager.GetBuffer<TelemetryMetric>(telemetryEntity);
            if (buffer.Length == 0)
            {
                return;
            }

            var culture = CultureInfo.InvariantCulture;
            for (int i = 0; i < buffer.Length; i++)
            {
                var metric = buffer[i];
            writer.Write("{\"type\":\"metric\",\"runId\":\"");
            WriteEscapedString(writer, _runIdString);
                writer.Write("\",\"tick\":");
                writer.Write(tick);
                writer.Write(",\"key\":\"");
                WriteEscapedString(writer, metric.Key.ToString());
                writer.Write("\",\"value\":");
                writer.Write(metric.Value.ToString("R", culture));
                writer.Write(",\"unit\":\"");
                writer.Write(GetUnitLabel(metric.Unit));
                writer.WriteLine("\"}");
            }

            buffer.Clear();
        }

        private void ExportFrameTiming(StreamWriter writer, uint tick)
        {
            if (!SystemAPI.TryGetSingletonEntity<FrameTimingStream>(out var frameEntity))
            {
                return;
            }

            if (!EntityManager.HasBuffer<FrameTimingSample>(frameEntity))
            {
                return;
            }

            var samples = EntityManager.GetBuffer<FrameTimingSample>(frameEntity);
            if (samples.Length > 0)
            {
                var culture = CultureInfo.InvariantCulture;
                for (int i = 0; i < samples.Length; i++)
                {
                    var sample = samples[i];
                    var label = FrameTimingRecorderSystem.GetGroupLabel(sample.Group).ToString();
                    writer.Write("{\"type\":\"frameTiming\",\"runId\":\"");
                    WriteEscapedString(writer, _runIdString);
                    writer.Write("\",\"tick\":");
                    writer.Write(tick);
                    writer.Write(",\"group\":\"");
                    WriteEscapedString(writer, label);
                    writer.Write("\",\"durationMs\":");
                    writer.Write(sample.DurationMs.ToString("R", culture));
                    writer.Write(",\"budgetMs\":");
                    writer.Write(sample.BudgetMs.ToString("R", culture));
                    writer.Write(",\"systemCount\":");
                    writer.Write(sample.SystemCount);
                    writer.Write(",\"budgetExceeded\":");
                    writer.Write((sample.Flags & FrameTimingFlags.BudgetExceeded) != 0 ? "true" : "false");
                    writer.Write(",\"catchUp\":");
                    writer.Write((sample.Flags & FrameTimingFlags.CatchUp) != 0 ? "true" : "false");
                    writer.WriteLine("}");
                }
            }

            if (EntityManager.HasComponent<AllocationDiagnostics>(frameEntity))
            {
                var allocations = EntityManager.GetComponentData<AllocationDiagnostics>(frameEntity);
                writer.Write("{\"type\":\"allocation\",\"runId\":\"");
                WriteEscapedString(writer, _runIdString);
                writer.Write("\",\"tick\":");
                writer.Write(tick);
                writer.Write(",\"totalAllocated\":");
                writer.Write(allocations.TotalAllocatedBytes);
                writer.Write(",\"totalReserved\":");
                writer.Write(allocations.TotalReservedBytes);
                writer.Write(",\"unusedReserved\":");
                writer.Write(allocations.TotalUnusedReservedBytes);
                writer.Write(",\"gc0\":");
                writer.Write(allocations.GcCollectionsGeneration0);
                writer.Write(",\"gc1\":");
                writer.Write(allocations.GcCollectionsGeneration1);
                writer.Write(",\"gc2\":");
                writer.Write(allocations.GcCollectionsGeneration2);
                writer.WriteLine("}");
            }

            samples.Clear();
        }

        private void ExportTelemetryEvents(StreamWriter writer)
        {
            var buffer = GetTelemetryEventBuffer();
            if (!buffer.IsCreated || buffer.Length == 0)
            {
                return;
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                ref var record = ref buffer.ElementAt(i);
                writer.Write("{\"type\":\"event\",\"runId\":\"");
                WriteEscapedString(writer, _runIdString);
                writer.Write("\",\"tick\":");
                writer.Write(record.Tick);
                writer.Write(",\"event\":\"");
                WriteEscapedString(writer, record.EventType.ToString());
                writer.Write("\",\"source\":\"");
                WriteEscapedString(writer, record.Source.ToString());
                writer.Write("\",\"payload\":");
                var payload = record.Payload.ToString();
                if (string.IsNullOrEmpty(payload))
                {
                    writer.Write("null");
                }
                else
                {
                    writer.Write(payload);
                }
                writer.WriteLine("}");
            }

            buffer.Clear();
        }

        private Entity GetTelemetryEventStreamEntity()
        {
            if (SystemAPI.TryGetSingleton<TelemetryStreamSingleton>(out var reference) && reference.Entity != Entity.Null)
            {
                return reference.Entity;
            }

            return Entity.Null;
        }

        private DynamicBuffer<TelemetryEvent> GetTelemetryEventBuffer()
        {
            var entity = GetTelemetryEventStreamEntity();
            if (entity == Entity.Null || !EntityManager.HasBuffer<TelemetryEvent>(entity))
            {
                return default;
            }

            return EntityManager.GetBuffer<TelemetryEvent>(entity);
        }

        private void ExportBehaviorTelemetry(StreamWriter writer)
        {
            if (!SystemAPI.HasSingleton<BehaviorTelemetryState>())
            {
                return;
            }

            var buffer = SystemAPI.GetSingletonBuffer<BehaviorTelemetryRecord>();

            if (buffer.Length == 0)
            {
                return;
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                var record = buffer[i];
                writer.Write("{\"type\":\"behavior\",\"runId\":\"");
                WriteEscapedString(writer, _runIdString);
                writer.Write("\",\"tick\":");
                writer.Write(record.Tick);
                writer.Write(",\"behaviorId\":");
                writer.Write((ushort)record.Behavior);
                writer.Write(",\"behaviorKind\":");
                writer.Write((byte)record.Kind);
                writer.Write(",\"metricId\":");
                writer.Write(record.MetricOrInvariantId);
                writer.Write(",\"valueA\":");
                writer.Write(record.ValueA);
                writer.Write(",\"valueB\":");
                writer.Write(record.ValueB);
                writer.Write(",\"passed\":");
                writer.Write(record.Passed != 0 ? "true" : "false");
                writer.WriteLine("}");
            }

            buffer.Clear();
        }

        private void ExportReplayTelemetry(StreamWriter writer, uint tick)
        {
            if (!SystemAPI.TryGetSingletonEntity<ReplayCaptureStream>(out var replayEntity))
            {
                return;
            }

            var stream = SystemAPI.GetComponent<ReplayCaptureStream>(replayEntity);
            writer.Write("{\"type\":\"replay\",\"runId\":\"");
            WriteEscapedString(writer, _runIdString);
            writer.Write("\",\"tick\":");
            writer.Write(tick);
            writer.Write(",\"eventCount\":");
            writer.Write(stream.EventCount);
            writer.Write(",\"lastEventType\":\"");
            WriteEscapedString(writer, ReplayCaptureSystem.GetEventTypeLabel(stream.LastEventType).ToString());
            writer.Write("\",\"lastEventLabel\":\"");
            WriteEscapedString(writer, stream.LastEventLabel.ToString());
            writer.WriteLine("\"}");

            if (EntityManager.HasBuffer<ReplayCaptureEvent>(replayEntity))
            {
                var events = EntityManager.GetBuffer<ReplayCaptureEvent>(replayEntity);
                for (int i = 0; i < events.Length; i++)
                {
                    var evt = events[i];
                    writer.Write("{\"type\":\"replayEvent\",\"runId\":\"");
                    WriteEscapedString(writer, _runIdString);
                    writer.Write("\",\"tick\":");
                    writer.Write(evt.Tick);
                    writer.Write(",\"eventType\":\"");
                    WriteEscapedString(writer, ReplayCaptureSystem.GetEventTypeLabel(evt.Type).ToString());
                    writer.Write("\",\"label\":\"");
                    WriteEscapedString(writer, evt.Label.ToString());
                    writer.Write("\",\"value\":");
                    writer.Write(evt.Value.ToString("R", CultureInfo.InvariantCulture));
                    writer.WriteLine("}");
                }

                events.Clear();
            }
        }

        private void WriteRunHeader(StreamWriter writer, in TelemetryExportConfig config)
        {
            writer.Write("{\"type\":\"run\",\"runId\":\"");
            WriteEscapedString(writer, _runIdString);
            writer.Write("\",\"timestamp\":\"");
            writer.Write(DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            writer.Write("\",\"world\":\"");
            WriteEscapedString(writer, World.Name);
            writer.Write("\",\"flags\":");
            writer.Write((int)config.Flags);
            writer.Write(",\"application\":\"");
            WriteEscapedString(writer, Application.productName);
            writer.Write("\",\"unityVersion\":\"");
            WriteEscapedString(writer, Application.unityVersion);
            writer.Write("\"");

            if (SystemAPI.TryGetSingleton<ScenarioState>(out var scenario))
            {
                writer.Write(",\"scenarioKind\":");
                writer.Write((byte)scenario.Current);
                writer.Write(",\"scenarioKindLabel\":\"");
                WriteEscapedString(writer, scenario.Current.ToString());
                writer.Write("\",\"bootPhase\":");
                writer.Write((byte)scenario.BootPhase);
                writer.Write(",\"isInitialized\":");
                writer.Write(scenario.IsInitialized ? "true" : "false");
                writer.Write(",\"enableGodgame\":");
                writer.Write(scenario.EnableGodgame ? "true" : "false");
                writer.Write(",\"enableSpace4x\":");
                writer.Write(scenario.EnableSpace4x ? "true" : "false");
                writer.Write(",\"enableEconomy\":");
                writer.Write(scenario.EnableEconomy ? "true" : "false");
            }

            writer.WriteLine("}");
        }

        private static void WriteEscapedString(StreamWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                switch (c)
                {
                    case '\\':
                        writer.Write("\\\\");
                        break;
                    case '\"':
                        writer.Write("\\\"");
                        break;
                    case '\n':
                        writer.Write("\\n");
                        break;
                    case '\r':
                        writer.Write("\\r");
                        break;
                    case '\t':
                        writer.Write("\\t");
                        break;
                    default:
                        if (c < 32)
                        {
                            writer.Write($"\\u{(int)c:X4}");
                        }
                        else
                        {
                            writer.Write(c);
                        }
                        break;
                }
            }
        }

        private static string GetUnitLabel(TelemetryMetricUnit unit)
        {
            return unit switch
            {
                TelemetryMetricUnit.Count => "count",
                TelemetryMetricUnit.Ratio => "ratio",
                TelemetryMetricUnit.DurationMilliseconds => "ms",
                TelemetryMetricUnit.Bytes => "bytes",
                TelemetryMetricUnit.None => "none",
                TelemetryMetricUnit.Custom => "custom",
                _ => "unknown"
            };
        }
    }
}
