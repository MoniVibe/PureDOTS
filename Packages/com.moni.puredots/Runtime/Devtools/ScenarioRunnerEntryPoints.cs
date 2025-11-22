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
            var args = Environment.GetCommandLineArgs();
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
