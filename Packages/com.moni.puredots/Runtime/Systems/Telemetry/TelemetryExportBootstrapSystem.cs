using System;
using System.IO;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Systems.Telemetry
{
    /// <summary>
    /// Enables telemetry export automatically in headless or env-var controlled runs.
    /// </summary>
    [UpdateInGroup(typeof(TimeSystemGroup))]
    [UpdateAfter(typeof(CoreSingletonBootstrapSystem))]
    public partial struct TelemetryExportBootstrapSystem : ISystem
    {
        private bool _initialized;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TelemetryExportConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_initialized)
            {
                return;
            }

            var configRW = SystemAPI.GetSingletonRW<TelemetryExportConfig>();
            if (configRW.ValueRO.Enabled != 0 && configRW.ValueRO.OutputPath.Length > 0)
            {
                _initialized = true;
                return;
            }

            var enableValue = GetEnv("PUREDOTS_TELEMETRY_ENABLE");
            var shouldEnable = Application.isBatchMode || IsTruthy(enableValue);
            if (!shouldEnable)
            {
                _initialized = true;
                return;
            }

            var runIdValue = GetEnv("PUREDOTS_TELEMETRY_RUN_ID");
            var pathValue = GetEnv("PUREDOTS_TELEMETRY_PATH");
            var flagsValue = GetEnv("PUREDOTS_TELEMETRY_FLAGS");

            var flags = configRW.ValueRO.Flags;
            if (!string.IsNullOrEmpty(flagsValue) && TryParseFlags(flagsValue, out var parsedFlags))
            {
                flags = parsedFlags;
            }

            FixedString128Bytes runId = configRW.ValueRO.RunId;
            if (!string.IsNullOrEmpty(runIdValue))
            {
                runId = new FixedString128Bytes(runIdValue);
            }
            else if (runId.Length == 0)
            {
                runId = GenerateRunId();
            }

            if (string.IsNullOrEmpty(pathValue))
            {
                pathValue = BuildDefaultPath(runId.ToString());
            }

            configRW.ValueRW.OutputPath = new FixedString512Bytes(pathValue);
            configRW.ValueRW.RunId = runId;
            configRW.ValueRW.Flags = flags;
            configRW.ValueRW.Enabled = 1;
            configRW.ValueRW.Version++;
            _initialized = true;
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

        private static string BuildDefaultPath(string runId)
        {
            var basePath = Application.persistentDataPath;
            if (string.IsNullOrEmpty(basePath))
            {
                basePath = ".";
            }

            return Path.Combine(basePath, "telemetry", $"telemetry_{runId}.ndjson");
        }

        private static string GetEnv(string key)
        {
            return global::System.Environment.GetEnvironmentVariable(key);
        }

        private static bool IsTruthy(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            value = value.Trim();
            return value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseFlags(string value, out TelemetryExportFlags flags)
        {
            flags = TelemetryExportFlags.None;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (int.TryParse(value, out var numeric))
            {
                flags = (TelemetryExportFlags)numeric;
                return true;
            }

            var parts = value.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return false;
            }

            foreach (var part in parts)
            {
                var token = part.Trim();
                if (Enum.TryParse(token, true, out TelemetryExportFlags parsed))
                {
                    flags |= parsed;
                    continue;
                }

                if (TryMapFlagAlias(token, out parsed))
                {
                    flags |= parsed;
                }
            }

            return flags != TelemetryExportFlags.None;
        }

        private static bool TryMapFlagAlias(string token, out TelemetryExportFlags flag)
        {
            flag = TelemetryExportFlags.None;
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            token = token.Trim().ToLowerInvariant();
            switch (token)
            {
                case "metrics":
                    flag = TelemetryExportFlags.IncludeTelemetryMetrics;
                    return true;
                case "frame":
                case "frametiming":
                    flag = TelemetryExportFlags.IncludeFrameTiming;
                    return true;
                case "behavior":
                    flag = TelemetryExportFlags.IncludeBehaviorTelemetry;
                    return true;
                case "replay":
                    flag = TelemetryExportFlags.IncludeReplayEvents;
                    return true;
                case "events":
                case "telemetryevents":
                    flag = TelemetryExportFlags.IncludeTelemetryEvents;
                    return true;
            }

            return false;
        }
    }
}
