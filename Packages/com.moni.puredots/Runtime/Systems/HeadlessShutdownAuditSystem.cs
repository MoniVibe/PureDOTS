using System;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Scenarios;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Runtime.Time;
using Unity.Entities;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Emits a compact audit log right before headless shutdown.
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(HeadlessExitSystem))]
    public partial struct HeadlessShutdownAuditSystem : ISystem
    {
        private byte _logged;

        public void OnCreate(ref SystemState state)
        {
            if (!RuntimeMode.IsHeadless || !Application.isBatchMode || !BugHuntGate.ShutdownAuditEnabled)
            {
                state.Enabled = false;
                return;
            }

            state.RequireForUpdate<HeadlessExitRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_logged != 0)
            {
                return;
            }

            _logged = 1;
            var em = state.EntityManager;
            var totalEntities = em.UniversalQuery.CalculateEntityCount();
            var tick = SystemAPI.TryGetSingleton<TimeState>(out var timeState) ? timeState.Tick : 0u;
            var scenarioId = SystemAPI.TryGetSingleton<ScenarioInfo>(out var info) ? info.ScenarioId.ToString() : "unknown";

            var exitRequest = SystemAPI.GetSingleton<HeadlessExitRequest>();
            var ticksSinceRequest = tick >= exitRequest.RequestedTick ? tick - exitRequest.RequestedTick : 0u;
            var headlessExitImmediate = IsTruthyEnv("PUREDOTS_HEADLESS_EXIT_IMMEDIATE");
            var exitGraceMs = ResolveExitGraceMs();
            var exitKillMs = ResolveExitKillMs(exitGraceMs);
            var telemetryFlushGraceMs = ResolveTelemetryFlushGraceMs();
            var exitPolicy = ScenarioExitUtility.ResolveExitPolicy();
            var exitPolicyEnv = ScenarioExitUtility.GetExitPolicyEnvRaw();

            UnityDebug.Log($"[ShutdownAudit] tick={tick} scenario={scenarioId} entities={totalEntities} bughunt_disabled={BugHuntGate.DisabledRaw}");
            UnityDebug.Log($"[ShutdownAudit] exit_code={exitRequest.ExitCode} requested_tick={exitRequest.RequestedTick} ticks_since_request={ticksSinceRequest} exit_immediate={headlessExitImmediate}");
            UnityDebug.Log($"[ShutdownAudit] exit_policy={exitPolicy} exit_policy_env='{exitPolicyEnv}' exit_grace_ms={exitGraceMs} exit_kill_ms={exitKillMs} telemetry_flush_grace_ms={telemetryFlushGraceMs}");
            UnityDebug.Log($"[ShutdownAudit] worlds={World.All.Count} exit_request_count={Count<HeadlessExitRequest>(em)}");

            EmitTelemetryAudit(ref state, tick);
        }

        private static void EmitTelemetryAudit(ref SystemState state, uint tick)
        {
            if (!SystemAPI.TryGetSingleton<TelemetryExportConfig>(out var exportConfig))
            {
                return;
            }

            var exportStatePresent = SystemAPI.TryGetSingleton<TelemetryExportState>(out var exportState);
            var capReached = exportStatePresent && exportState.CapReached != 0;
            var bytesWritten = exportStatePresent ? exportState.BytesWritten : 0ul;
            var maxBytes = exportStatePresent ? exportState.MaxOutputBytes : 0ul;
            var runId = exportStatePresent ? exportState.RunId.ToString() : string.Empty;
            var outputPath = exportConfig.OutputPath.ToString();

            UnityDebug.Log($"[ShutdownAudit] telemetry enabled={(exportConfig.Enabled != 0)} flags={(int)exportConfig.Flags} cadence={exportConfig.CadenceTicks} tick={tick}");
            UnityDebug.Log($"[ShutdownAudit] telemetry path='{outputPath}' run_id='{runId}' bytes={bytesWritten}/{maxBytes} cap_reached={capReached}");

            if (SystemAPI.TryGetSingletonEntity<TelemetryStream>(out var telemetryEntity) &&
                state.EntityManager.HasBuffer<TelemetryMetric>(telemetryEntity))
            {
                var metricCount = state.EntityManager.GetBuffer<TelemetryMetric>(telemetryEntity).Length;
                UnityDebug.Log($"[ShutdownAudit] telemetry metric_buffer_count={metricCount}");
            }

            if (SystemAPI.TryGetSingletonEntity<TelemetryStreamSingleton>(out var streamSingletonEntity))
            {
                var streamSingleton = state.EntityManager.GetComponentData<TelemetryStreamSingleton>(streamSingletonEntity);
                if (streamSingleton.Stream != Entity.Null && state.EntityManager.HasBuffer<TelemetryEvent>(streamSingleton.Stream))
                {
                    var eventCount = state.EntityManager.GetBuffer<TelemetryEvent>(streamSingleton.Stream).Length;
                    UnityDebug.Log($"[ShutdownAudit] telemetry event_buffer_count={eventCount}");
                }
            }
        }

        private static int Count<T>(EntityManager em) where T : unmanaged, IComponentData
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.CalculateEntityCount();
        }

        private static bool IsTruthyEnv(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            value = value.Trim();
            return value == "1"
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        private static int ResolveExitGraceMs()
        {
            return GetEnvInt("PUREDOTS_HEADLESS_EXIT_GRACE_MS", 2000, 100, 120000);
        }

        private static int ResolveExitKillMs(int graceMs)
        {
            var defaultKillMs = graceMs + 5000;
            var minKillMs = graceMs + 1000;
            var killMs = GetEnvInt("PUREDOTS_HEADLESS_EXIT_KILL_MS", defaultKillMs, minKillMs, 300000);
            if (killMs <= graceMs)
            {
                killMs = minKillMs;
            }

            return killMs;
        }

        private static int ResolveTelemetryFlushGraceMs()
        {
            return GetEnvInt("PUREDOTS_TELEMETRY_FLUSH_GRACE_MS", 0, 0, 300000);
        }

        private static int GetEnvInt(string name, int defaultValue, int minValue, int maxValue)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (!int.TryParse(value.Trim(), out var parsed))
            {
                return defaultValue;
            }

            if (parsed < minValue)
            {
                return minValue;
            }

            return parsed > maxValue ? maxValue : parsed;
        }
    }
}
