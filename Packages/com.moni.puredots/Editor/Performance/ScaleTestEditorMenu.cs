#if UNITY_EDITOR
using System.IO;
using PureDOTS.Runtime.Devtools;
using UnityEditor;
using UnityEngine;

namespace PureDOTS.Editor.Performance
{
    /// <summary>
    /// Editor menu items for running scale tests.
    /// </summary>
    public static class ScaleTestEditorMenu
    {
        private const string SamplesPath = "Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/";
        private const string ReportsPath = "CI/Reports/";

        [MenuItem("PureDOTS/Scale Tests/List Available Scenarios", priority = 100)]
        public static void ListScenarios()
        {
            // TODO: reintroduce ScenarioRunnerEntryPoints or align with new ScenarioRunner APIs
            Debug.LogWarning("[ScaleTest] List Available Scenarios is temporarily disabled. Use Run Scenario menu items instead.");
            // ScenarioRunnerEntryPoints.ListScaleScenarios();
        }

        [MenuItem("PureDOTS/Scale Tests/Run Mini LOD Demo", priority = 200)]
        public static void RunMiniLODDemo()
        {
            RunScenario("scale_mini_lod_demo.json", "mini_lod_demo_report.json");
        }

        [MenuItem("PureDOTS/Scale Tests/Run Mini Aggregate Demo", priority = 201)]
        public static void RunMiniAggregateDemo()
        {
            RunScenario("scale_mini_aggregate_demo.json", "mini_aggregate_demo_report.json");
        }

        [MenuItem("PureDOTS/Scale Tests/Run Baseline 10k", priority = 300)]
        public static void RunBaseline10k()
        {
            RunScenario("scale_baseline_10k.json", "baseline_10k_report.json");
        }

        [MenuItem("PureDOTS/Scale Tests/Run Stress 100k", priority = 301)]
        public static void RunStress100k()
        {
            RunScenario("scale_stress_100k.json", "stress_100k_report.json");
        }

        [MenuItem("PureDOTS/Scale Tests/Run Extreme 1M", priority = 302)]
        public static void RunExtreme1M()
        {
            RunScenario("scale_extreme_1m.json", "extreme_1m_report.json");
        }

        [MenuItem("PureDOTS/Scale Tests/Game Demos/Space4X Demo", priority = 500)]
        public static void RunSpace4XDemo()
        {
            RunScenario("scenario_space_demo_01.json", "space_demo_01_report.json");
        }

        [MenuItem("PureDOTS/Scale Tests/Game Demos/Godgame Demo", priority = 501)]
        public static void RunGodgameDemo()
        {
            RunScenario("scenario_god_demo_01.json", "god_demo_01_report.json");
        }

        [MenuItem("PureDOTS/Scale Tests/Open Reports Folder", priority = 400)]
        public static void OpenReportsFolder()
        {
            EnsureReportsFolder();
            EditorUtility.RevealInFinder(ReportsPath);
        }

        private static void RunScenario(string scenarioFileName, string reportFileName)
        {
            var scenarioPath = SamplesPath + scenarioFileName;
            
            if (!File.Exists(scenarioPath))
            {
                Debug.LogError($"[ScaleTest] Scenario not found: {scenarioPath}");
                return;
            }

            EnsureReportsFolder();
            var reportPath = ReportsPath + reportFileName;

            Debug.Log($"[ScaleTest] Running scenario: {scenarioFileName}");
            Debug.Log($"[ScaleTest] Report will be written to: {reportPath}");

            // Read and parse scenario
            var json = File.ReadAllText(scenarioPath);
            if (!PureDOTS.Runtime.Scenarios.ScenarioRunner.TryParse(json, out var data, out var parseError))
            {
                Debug.LogError($"[ScaleTest] Failed to parse scenario: {parseError}");
                return;
            }

            if (!PureDOTS.Runtime.Scenarios.ScenarioRunner.TryBuild(data, Unity.Collections.Allocator.Temp, out var scenario, out var buildError))
            {
                Debug.LogError($"[ScaleTest] Failed to build scenario: {buildError}");
                return;
            }

            using (scenario)
            {
                Debug.Log($"[ScaleTest] Scenario: {scenario.ScenarioId}");
                Debug.Log($"[ScaleTest] Ticks: {scenario.RunTicks}");
                Debug.Log($"[ScaleTest] Entity types: {scenario.EntityCounts.Length}");

                // Log entity breakdown
                int totalEntities = 0;
                for (int i = 0; i < scenario.EntityCounts.Length; i++)
                {
                    var ec = scenario.EntityCounts[i];
                    totalEntities += ec.Count;
                    Debug.Log($"[ScaleTest]   {ec.RegistryId}: {ec.Count}");
                }
                Debug.Log($"[ScaleTest] Total entities: {totalEntities}");

                // Generate report
                var report = GenerateReport(scenario, scenarioFileName);
                File.WriteAllText(reportPath, report);
                Debug.Log($"[ScaleTest] Report written to: {reportPath}");
            }
        }

        private static string GenerateReport(
            PureDOTS.Runtime.Scenarios.ResolvedScenario scenario,
            string scenarioFileName)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"scenarioFile\": \"{scenarioFileName}\",");
            sb.AppendLine($"  \"scenarioId\": \"{scenario.ScenarioId}\",");
            sb.AppendLine($"  \"seed\": {scenario.Seed},");
            sb.AppendLine($"  \"runTicks\": {scenario.RunTicks},");
            sb.AppendLine($"  \"timestamp\": \"{System.DateTime.UtcNow:O}\",");
            sb.AppendLine("  \"entityCounts\": [");

            int totalEntities = 0;
            for (int i = 0; i < scenario.EntityCounts.Length; i++)
            {
                var ec = scenario.EntityCounts[i];
                totalEntities += ec.Count;
                var comma = i < scenario.EntityCounts.Length - 1 ? "," : "";
                sb.AppendLine($"    {{ \"registryId\": \"{ec.RegistryId}\", \"count\": {ec.Count} }}{comma}");
            }

            sb.AppendLine("  ],");
            sb.AppendLine($"  \"totalEntities\": {totalEntities},");
            sb.AppendLine("  \"status\": \"scenario_validated\",");
            sb.AppendLine("  \"note\": \"Run via Unity Editor menu. For full metrics, use CLI batch mode.\"");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void EnsureReportsFolder()
        {
            if (!Directory.Exists(ReportsPath))
            {
                Directory.CreateDirectory(ReportsPath);
            }
        }
    }
}
#endif

