using System.IO;
using System.Text;
using UnityEngine;

namespace PureDOTS.Runtime.Scenarios
{
    /// <summary>
    /// Lightweight CSV serializer for ScenarioRunResult. Appends a header if the file is new.
    /// </summary>
    public static class ScenarioRunResultCsv
    {
        private static readonly string Header = string.Join(",",
            "scenarioId",
            "seed",
            "runTicks",
            "finalTick",
            "telemetryVersion",
            "commandLogCount",
            "commandCapacity",
            "commandBytes",
            "snapshotLogCount",
            "snapshotCapacity",
            "snapshotBytes",
            "totalLogBytes",
            "frameTimingBudgetExceeded",
            "frameTimingWorstMs",
            "frameTimingWorstGroup",
            "registryContinuityWarnings",
            "registryContinuityFailures",
            "entityCountEntries");

        public static void Write(string path, in ScenarioRunResult result)
        {
            var line = BuildLine(result);
            var fileExists = File.Exists(path);
            var sb = new StringBuilder();
            if (!fileExists)
            {
                sb.AppendLine(Header);
            }
            sb.AppendLine(line);
            File.AppendAllText(path, sb.ToString());
            Debug.Log($"ScenarioRunner: wrote CSV report to {path}");
        }

        private static string BuildLine(in ScenarioRunResult result)
        {
            // Basic CSV escaping for commas/quotes if they appear; expected fields are simple.
            string Escape(string v) => $"\"{v.Replace("\"", "\"\"")}\"";
            return string.Join(",",
                Escape(result.ScenarioId ?? string.Empty),
                result.Seed,
                result.RunTicks,
                result.FinalTick,
                result.TelemetryVersion,
                result.CommandLogCount,
                result.CommandCapacity,
                result.CommandBytes,
                result.SnapshotLogCount,
                result.SnapshotCapacity,
                result.SnapshotBytes,
                result.TotalLogBytes,
                result.FrameTimingBudgetExceeded ? "true" : "false",
                result.FrameTimingWorstMs.ToString("0.###"),
                Escape(result.FrameTimingWorstGroup ?? string.Empty),
                result.RegistryContinuityWarnings,
                result.RegistryContinuityFailures,
                result.EntityCountEntries);
        }
    }
}
