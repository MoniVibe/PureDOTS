using System.Text;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace PureDOTS.Runtime.Scenarios
{
    /// <summary>
    /// Minimal JSON serializer for ScenarioRunResult to avoid pulling in heavy dependencies.
    /// Uses StringBuilder to keep allocations bounded.
    /// </summary>
    public static class ScenarioRunResultJson
    {
        public static string Serialize(in ScenarioRunResult result)
        {
            var sb = new StringBuilder(256);
            sb.Append("{");
            AppendString(sb, "scenarioId", result.ScenarioId); sb.Append(",");
            AppendUInt(sb, "seed", result.Seed); sb.Append(",");
            AppendInt(sb, "runTicks", result.RunTicks); sb.Append(",");
            AppendUInt(sb, "finalTick", result.FinalTick); sb.Append(",");
            AppendUInt(sb, "telemetryVersion", result.TelemetryVersion); sb.Append(",");
            AppendInt(sb, "commandLogCount", result.CommandLogCount); sb.Append(",");
            AppendInt(sb, "snapshotLogCount", result.SnapshotLogCount); sb.Append(",");
            AppendBool(sb, "frameTimingBudgetExceeded", result.FrameTimingBudgetExceeded); sb.Append(",");
            AppendFloat(sb, "frameTimingWorstMs", result.FrameTimingWorstMs); sb.Append(",");
            AppendString(sb, "frameTimingWorstGroup", result.FrameTimingWorstGroup ?? string.Empty); sb.Append(",");
            AppendInt(sb, "registryContinuityWarnings", result.RegistryContinuityWarnings); sb.Append(",");
            AppendInt(sb, "registryContinuityFailures", result.RegistryContinuityFailures); sb.Append(",");
            AppendInt(sb, "entityCountEntries", result.EntityCountEntries); sb.Append(",");
            AppendInt(sb, "commandCapacity", result.CommandCapacity); sb.Append(",");
            AppendInt(sb, "snapshotCapacity", result.SnapshotCapacity); sb.Append(",");
            AppendInt(sb, "commandBytes", result.CommandBytes); sb.Append(",");
            AppendInt(sb, "snapshotBytes", result.SnapshotBytes); sb.Append(",");
            AppendInt(sb, "totalLogBytes", result.TotalLogBytes);
            if (result.Metrics != null && result.Metrics.Count > 0)
            {
                sb.Append(",");
                AppendMetrics(sb, result.Metrics);
            }
            sb.Append("}");
            return sb.ToString();
        }

        private static void AppendString(StringBuilder sb, string key, string value)
        {
            sb.Append("\"").Append(key).Append("\":\"").Append(Escape(value)).Append("\"");
        }

        private static void AppendInt(StringBuilder sb, string key, int value)
        {
            sb.Append("\"").Append(key).Append("\":").Append(value);
        }

        private static void AppendUInt(StringBuilder sb, string key, uint value)
        {
            sb.Append("\"").Append(key).Append("\":").Append(value);
        }

        private static void AppendBool(StringBuilder sb, string key, bool value)
        {
            sb.Append("\"").Append(key).Append("\":").Append(value ? "true" : "false");
        }

        private static void AppendFloat(StringBuilder sb, string key, float value)
        {
            sb.Append("\"").Append(key).Append("\":").Append(value.ToString("0.###"));
        }

        private static void AppendDouble(StringBuilder sb, string key, double value)
        {
            sb.Append("\"").Append(key).Append("\":").Append(value.ToString("0.###"));
        }

        private static void AppendMetrics(StringBuilder sb, List<ScenarioMetric> metrics)
        {
            sb.Append("\"metrics\":[");
            for (int i = 0; i < metrics.Count; i++)
            {
                var metric = metrics[i];
                sb.Append("{");
                AppendString(sb, "key", metric.Key ?? string.Empty); sb.Append(",");
                AppendDouble(sb, "value", metric.Value);
                sb.Append("}");
                if (i < metrics.Count - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append("]");
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
