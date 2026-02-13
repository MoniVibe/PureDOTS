using System;
using System.IO;
using PureDOTS.Runtime.Scenarios;
using Unity.Collections;
using UnityEngine;

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
                Debug.LogWarning("ScenarioRunner: missing --scenario <path>");
                return;
            }

            if (!File.Exists(scenarioPath))
            {
                Debug.LogError($"ScenarioRunner: scenario not found at {scenarioPath}");
                return;
            }

            var json = File.ReadAllText(scenarioPath);
            if (!ScenarioRunner.TryParse(json, out var data, out var parseError))
            {
                Debug.LogError($"ScenarioRunner: failed to parse JSON: {parseError}");
                return;
            }

            if (!ScenarioRunner.TryBuild(data, Allocator.Temp, out var scenario, out var buildError))
            {
                Debug.LogError($"ScenarioRunner: failed to build scenario: {buildError}");
                return;
            }

            using (scenario)
            {
                var summary = $"ScenarioRunner: loaded {scenario.ScenarioId} seed={scenario.Seed} ticks={scenario.RunTicks} entities={scenario.EntityCounts.Length} commands={scenario.InputCommands.Length}";
                Debug.Log(summary);

                if (!string.IsNullOrWhiteSpace(reportPath))
                {
                    File.WriteAllText(reportPath, summary);
                }
            }
        }

        /// <summary>
        /// Invoked via -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScenarioExecutorFromArgs
        /// Expected args: --scenario <name or path> [--report <path>]
        /// </summary>
        public static void RunScenarioExecutorFromArgs()
        {
            var args = System.Environment.GetCommandLineArgs();
            var scenarioArg = ReadArg(args, "--scenario");
            var reportPath = ReadArg(args, "--report");

            if (string.IsNullOrWhiteSpace(scenarioArg))
            {
                Debug.LogWarning("ScenarioExecutor: missing --scenario <name or path>");
                return;
            }

            var scenarioPath = ResolveScenarioPath(scenarioArg);
            if (string.IsNullOrWhiteSpace(scenarioPath) || !File.Exists(scenarioPath))
            {
                Debug.LogError($"ScenarioExecutor: scenario not found: {scenarioArg}");
                return;
            }

            try
            {
                var result = ScenarioRunnerExecutor.RunFromFile(scenarioPath, reportPath);
                Debug.Log($"ScenarioExecutor: completed {result.ScenarioId} ticks={result.RunTicks} snapshots={result.SnapshotLogCount}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ScenarioExecutor: run failed: {ex}");
            }
        }

        /// <summary>
        /// Run scale test scenario with metrics collection.
        /// Invoked via -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScaleTest
        /// Expected args: --scenario <name or path> [--metrics <report path>]
        /// </summary>
        public static void RunScaleTest()
        {
            var args = System.Environment.GetCommandLineArgs();
            var scenarioArg = ReadArg(args, "--scenario");
            var metricsPath = ReadArg(args, "--metrics");

            if (string.IsNullOrWhiteSpace(scenarioArg))
            {
                Debug.LogWarning("ScaleTest: missing --scenario <name or path>");
                ListScaleScenarios();
                return;
            }

            var scenarioPath = ResolveScenarioPath(scenarioArg);
            if (string.IsNullOrWhiteSpace(scenarioPath) || !File.Exists(scenarioPath))
            {
                Debug.LogError($"ScaleTest: scenario not found: {scenarioArg}");
                return;
            }

            var reportPath = metricsPath;
            if (string.IsNullOrWhiteSpace(reportPath))
            {
                var scenarioName = Path.GetFileNameWithoutExtension(scenarioPath);
                reportPath = Path.Combine(Path.GetTempPath(), $"{scenarioName}_scale_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
            }
            else
            {
                var reportDir = Path.GetDirectoryName(reportPath);
                if (!string.IsNullOrWhiteSpace(reportDir) && !Directory.Exists(reportDir))
                {
                    Directory.CreateDirectory(reportDir);
                }
            }

            try
            {
                var result = ScenarioRunnerExecutor.RunFromFile(scenarioPath, reportPath);
                Debug.Log($"[ScaleTest] Completed: {result.ScenarioId} runTicks={result.RunTicks} finalTick={result.FinalTick}");
                Debug.Log($"[ScaleTest] Report written to: {reportPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScaleTest] Run failed: {ex}");
            }
        }

        /// <summary>
        /// Lists available scale test scenarios.
        /// </summary>
        public static void ListScaleScenarios()
        {
            Debug.Log("[ScaleTest] Available scale test scenarios:");
            Debug.Log("");
            Debug.Log("  Scale Tests:");
            Debug.Log("    - scale_baseline_10k     : 10k entities, target 60 FPS");
            Debug.Log("    - scale_stress_100k      : 100k entities, target 30 FPS");
            Debug.Log("    - scale_extreme_1m       : 1M+ entities, target 10 FPS");
            Debug.Log("");
            Debug.Log("  Sanity scenarios:");
            Debug.Log("    - scale_mini_lod       : 2k test entities with LOD components");
            Debug.Log("    - scale_mini_aggregate : 5 aggregates with 200 members");
            Debug.Log("");
            Debug.Log("  Game scenarios:");
            Debug.Log("    - scenario_space_01    : Space4X scenario (carriers/crafts/asteroids/fleets)");
            Debug.Log("    - scenario_god_01      : Godgame scenario (villagers/resources/villages)");
            Debug.Log("");
            Debug.Log("Usage:");
            Debug.Log("  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScaleTest \\");
            Debug.Log("    --scenario <name> --metrics <output.json> [--enable-lod-debug] [--enable-aggregate-debug]");
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

    }
}
