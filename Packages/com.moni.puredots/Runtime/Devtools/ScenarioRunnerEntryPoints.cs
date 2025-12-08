using System;
using System.IO;
using System.Text;
using PureDOTS.Runtime.Scenarios;
using PureDOTS.Runtime.Telemetry;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;

namespace PureDOTS.Runtime.Devtools
{
    /// <summary>
    /// Entry points for running scenarios from CLI (-executeMethod) or debug menus.
    /// Future slices will drive actual world boot/run; for now this validates inputs and prints a summary.
    /// </summary>
    public static class ScenarioRunnerEntryPoints
    {
        /// <summary>
        /// Invoked via -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScenarioFromArgs
        /// Expected args: --scenario <path to json> [--report <path>]
        /// </summary>
        public static void RunScenarioFromArgs()
        {
            var args = System.Environment.GetCommandLineArgs();
            var scenarioPath = ReadArg(args, "--scenario");
            var reportPath = ReadArg(args, "--report");

            if (string.IsNullOrWhiteSpace(scenarioPath))
            {
                UnityDebug.LogWarning("ScenarioRunner: missing --scenario <path>");
                return;
            }

            if (!File.Exists(scenarioPath))
            {
                UnityDebug.LogError($"ScenarioRunner: scenario not found at {scenarioPath}");
                return;
            }

            var json = File.ReadAllText(scenarioPath);
            if (!ScenarioRunner.TryParse(json, out var data, out var parseError))
            {
                UnityDebug.LogError($"ScenarioRunner: failed to parse JSON: {parseError}");
                return;
            }

            if (!ScenarioRunner.TryBuild(data, Allocator.Temp, out var scenario, out var buildError))
            {
                UnityDebug.LogError($"ScenarioRunner: failed to build scenario: {buildError}");
                return;
            }

            using (scenario)
            {
                var summary = $"ScenarioRunner: loaded {scenario.ScenarioId} seed={scenario.Seed} ticks={scenario.RunTicks} entities={scenario.EntityCounts.Length} commands={scenario.InputCommands.Length}";
                UnityDebug.Log(summary);

                if (!string.IsNullOrWhiteSpace(reportPath))
                {
                    File.WriteAllText(reportPath, summary);
                }
            }
        }

        /// <summary>
        /// Run scale test scenario with metrics collection.
        /// Invoked via -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScaleTest
        /// Expected args: --scenario <name or path> [--metrics <report path>] [--target-ms <tick time budget>]
        ///                [--enable-lod-debug] [--enable-aggregate-debug]
        /// </summary>
        public static void RunScaleTest()
        {
            var args = System.Environment.GetCommandLineArgs();
            var scenarioArg = ReadArg(args, "--scenario");
            var metricsPath = ReadArg(args, "--metrics");
            var targetMsArg = ReadArg(args, "--target-ms");
            var enableLodDebug = HasFlag(args, "--enable-lod-debug");
            var enableAggregateDebug = HasFlag(args, "--enable-aggregate-debug");

            if (string.IsNullOrWhiteSpace(scenarioArg))
            {
                UnityDebug.LogWarning("ScaleTest: missing --scenario <name or path>");
                UnityDebug.Log("Available scale scenarios:");
                UnityDebug.Log("  Scale: scale_baseline_10k, scale_stress_100k, scale_extreme_1m");
                UnityDebug.Log("  Sanity: scale_mini_lod_demo, scale_mini_aggregate_demo");
                UnityDebug.Log("  Game Demos: scenario_space_demo_01, scenario_god_demo_01");
                return;
            }

            // Resolve scenario path
            var scenarioPath = ResolveScenarioPath(scenarioArg);
            if (string.IsNullOrWhiteSpace(scenarioPath) || !File.Exists(scenarioPath))
            {
                UnityDebug.LogError($"ScaleTest: scenario not found: {scenarioArg}");
                return;
            }

            // Parse target tick time
            var targetTickTimeMs = 16.67f; // Default 60 FPS
            if (!string.IsNullOrWhiteSpace(targetMsArg) && float.TryParse(targetMsArg, out var parsed))
            {
                targetTickTimeMs = parsed;
            }

            // Load and validate scenario
            var json = File.ReadAllText(scenarioPath);
            if (!ScenarioRunner.TryParse(json, out var data, out var parseError))
            {
                UnityDebug.LogError($"ScaleTest: failed to parse JSON: {parseError}");
                return;
            }

            if (!ScenarioRunner.TryBuild(data, Allocator.Temp, out var scenario, out var buildError))
            {
                UnityDebug.LogError($"ScaleTest: failed to build scenario: {buildError}");
                return;
            }

            using (scenario)
            {
                UnityDebug.Log($"[ScaleTest] Starting: {scenario.ScenarioId}");
                UnityDebug.Log($"[ScaleTest] Target tick time: {targetTickTimeMs}ms");
                UnityDebug.Log($"[ScaleTest] Ticks to run: {scenario.RunTicks}");
                UnityDebug.Log($"[ScaleTest] Entity counts: {scenario.EntityCounts.Length} types");
                UnityDebug.Log($"[ScaleTest] Debug flags: LOD={enableLodDebug}, Aggregate={enableAggregateDebug}");

                // Log entity breakdown
                for (int i = 0; i < scenario.EntityCounts.Length; i++)
                {
                    var ec = scenario.EntityCounts[i];
                    UnityDebug.Log($"[ScaleTest]   {ec.RegistryId}: {ec.Count}");
                }

                // Create metrics config for this run
                var metricsConfig = new ScaleTestMetricsConfigData
                {
                    SampleInterval = 10,
                    LogInterval = 50,
                    CollectSystemTimings = true,
                    CollectMemoryStats = true,
                    EnableLODDebug = enableLodDebug,
                    EnableAggregateDebug = enableAggregateDebug,
                    TargetTickTimeMs = targetTickTimeMs,
                    TargetMemoryMB = 2048f
                };

                // Generate metrics report
                var report = GenerateScaleTestReport(scenario, targetTickTimeMs, metricsConfig);
                UnityDebug.Log(report);

                if (!string.IsNullOrWhiteSpace(metricsPath))
                {
                    // Ensure directory exists
                    var dir = Path.GetDirectoryName(metricsPath);
                    if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.WriteAllText(metricsPath, report);
                    UnityDebug.Log($"[ScaleTest] Report written to: {metricsPath}");
                }
            }
        }

        /// <summary>
        /// Serializable config data for scale test metrics.
        /// </summary>
        [Serializable]
        public struct ScaleTestMetricsConfigData
        {
            public uint SampleInterval;
            public uint LogInterval;
            public bool CollectSystemTimings;
            public bool CollectMemoryStats;
            public bool EnableLODDebug;
            public bool EnableAggregateDebug;
            public float TargetTickTimeMs;
            public float TargetMemoryMB;
        }

        /// <summary>
        /// Lists available scale test scenarios.
        /// </summary>
        public static void ListScaleScenarios()
        {
            UnityDebug.Log("[ScaleTest] Available scale test scenarios:");
            UnityDebug.Log("");
            UnityDebug.Log("  Scale Tests:");
            UnityDebug.Log("    - scale_baseline_10k     : 10k entities, target 60 FPS");
            UnityDebug.Log("    - scale_stress_100k      : 100k entities, target 30 FPS");
            UnityDebug.Log("    - scale_extreme_1m       : 1M+ entities, target 10 FPS");
            UnityDebug.Log("");
            UnityDebug.Log("  Sanity Demos:");
            UnityDebug.Log("    - scale_mini_lod_demo       : 2k test entities with LOD components");
            UnityDebug.Log("    - scale_mini_aggregate_demo : 5 aggregates with 200 members");
            UnityDebug.Log("");
            UnityDebug.Log("  Game Demos:");
            UnityDebug.Log("    - scenario_space_demo_01    : Space4X demo (carriers/crafts/asteroids/fleets)");
            UnityDebug.Log("    - scenario_god_demo_01      : Godgame demo (villagers/resources/villages)");
            UnityDebug.Log("");
            UnityDebug.Log("Usage:");
            UnityDebug.Log("  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScaleTest \\");
            UnityDebug.Log("    --scenario <name> --metrics <output.json> [--enable-lod-debug] [--enable-aggregate-debug]");
        }

        private static string ResolveScenarioPath(string scenarioArg)
        {
            // If it's already a path, use it directly
            if (File.Exists(scenarioArg))
            {
                return scenarioArg;
            }

            // Try to find in Samples folder
            var basePath = "Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/";
            
            // Try with .json extension
            var withExtension = scenarioArg.EndsWith(".json") ? scenarioArg : scenarioArg + ".json";
            var fullPath = basePath + withExtension;
            
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            // Try common variations
            var variations = new[]
            {
                $"scale_{scenarioArg}.json",
                $"{scenarioArg}_scale.json",
                withExtension
            };

            foreach (var variant in variations)
            {
                var path = basePath + variant;
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static string GenerateScaleTestReport(ResolvedScenario scenario, float targetTickTimeMs, ScaleTestMetricsConfigData metricsConfig)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"scenarioId\": \"{scenario.ScenarioId}\",");
            sb.AppendLine($"  \"seed\": {scenario.Seed},");
            sb.AppendLine($"  \"runTicks\": {scenario.RunTicks},");
            sb.AppendLine($"  \"targetTickTimeMs\": {targetTickTimeMs},");
            sb.AppendLine($"  \"timestamp\": \"{DateTime.UtcNow:O}\",");
            sb.AppendLine("  \"entityCounts\": [");
            
            var totalEntities = 0;
            for (int i = 0; i < scenario.EntityCounts.Length; i++)
            {
                var ec = scenario.EntityCounts[i];
                totalEntities += ec.Count;
                var comma = i < scenario.EntityCounts.Length - 1 ? "," : "";
                sb.AppendLine($"    {{ \"registryId\": \"{ec.RegistryId}\", \"count\": {ec.Count} }}{comma}");
            }
            
            sb.AppendLine("  ],");
            sb.AppendLine($"  \"totalEntities\": {totalEntities},");
            sb.AppendLine("  \"metricsConfig\": {");
            sb.AppendLine($"    \"sampleInterval\": {metricsConfig.SampleInterval},");
            sb.AppendLine($"    \"logInterval\": {metricsConfig.LogInterval},");
            sb.AppendLine($"    \"collectSystemTimings\": {metricsConfig.CollectSystemTimings.ToString().ToLower()},");
            sb.AppendLine($"    \"collectMemoryStats\": {metricsConfig.CollectMemoryStats.ToString().ToLower()},");
            sb.AppendLine($"    \"enableLODDebug\": {metricsConfig.EnableLODDebug.ToString().ToLower()},");
            sb.AppendLine($"    \"enableAggregateDebug\": {metricsConfig.EnableAggregateDebug.ToString().ToLower()},");
            sb.AppendLine($"    \"targetTickTimeMs\": {metricsConfig.TargetTickTimeMs},");
            sb.AppendLine($"    \"targetMemoryMB\": {metricsConfig.TargetMemoryMB}");
            sb.AppendLine("  },");
            sb.AppendLine("  \"status\": \"scenario_loaded\",");
            sb.AppendLine("  \"note\": \"Actual metrics collected during runtime execution\"");
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        private static string ReadArg(string[] args, string key)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, key, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                    {
                        return args[i + 1];
                    }
                    return string.Empty;
                }

                if (arg.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring(key.Length + 1);
                }
            }

            return string.Empty;
        }

        private static bool HasFlag(string[] args, string flag)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Loads a scenario v2 file with version detection and migration.
        /// Invoked via -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.LoadScenarioV2
        /// Expected args: --scenario <path to v2 JSON>
        /// </summary>
        public static void LoadScenarioV2()
        {
            var args = System.Environment.GetCommandLineArgs();
            var scenarioPath = ReadArg(args, "--scenario");

            if (string.IsNullOrWhiteSpace(scenarioPath))
            {
                UnityDebug.LogWarning("LoadScenarioV2: missing --scenario <path>");
                return;
            }

            if (!File.Exists(scenarioPath))
            {
                UnityDebug.LogError($"LoadScenarioV2: scenario not found at {scenarioPath}");
                return;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                UnityDebug.LogError("LoadScenarioV2: no active world found");
                return;
            }

            if (!PureDOTS.Runtime.Devtools.Scenario.ScenarioLoaderV2.Load(scenarioPath, world, out var error))
            {
                UnityDebug.LogError($"LoadScenarioV2: failed to load scenario: {error}");
                return;
            }

            UnityDebug.Log($"LoadScenarioV2: successfully loaded scenario from {scenarioPath}");
        }
    }
}
