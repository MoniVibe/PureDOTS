using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace PureDOTS.Editor
{
    public static class RoleSlotTestRunnerCLI
    {
        public static void Run()
        {
            var args = System.Environment.GetCommandLineArgs();
            var filter = GetArg(args, "-testFilter");
            var resultsPath = GetArg(args, "-testResults");
            if (string.IsNullOrWhiteSpace(resultsPath))
            {
                resultsPath = Path.Combine(Directory.GetCurrentDirectory(), "CI", "TestResults", "editmode-seats-roles.xml");
            }

            var settings = new ExecutionSettings
            {
                filters = new[]
                {
                    new Filter { testMode = TestMode.EditMode }
                },
                runSynchronously = true
            };

            if (!string.IsNullOrWhiteSpace(filter))
            {
                settings.filters[0].testNames = new[] { filter };
                settings.filters[0].groupNames = new[] { filter };
            }

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new CliRunCallbacks(resultsPath));
            api.Execute(settings);
        }

        private static string GetArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return string.Empty;
        }

        private sealed class CliRunCallbacks : ICallbacks
        {
            private readonly string _resultsPath;

            public CliRunCallbacks(string resultsPath)
            {
                _resultsPath = resultsPath;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                if (!string.IsNullOrWhiteSpace(_resultsPath))
                {
                    TestRunnerApi.SaveResultToFile(result, _resultsPath);
                }

                EditorApplication.Exit(result.FailCount > 0 ? 1 : 0);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }
    }
}
