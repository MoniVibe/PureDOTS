using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems.Telemetry;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Defers <see cref="Application.Quit(int)"/> until after telemetry export has flushed.
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(TelemetryExportSystem))]
    public partial struct HeadlessExitSystem : ISystem
    {
        private static int _forceExitInit;
        private static bool _forceExitImmediate;
        private byte _exitStage;
        private double _exitStartTime;
        private int _exitCode;

        public void OnCreate(ref SystemState state)
        {
            if (!RuntimeMode.IsHeadless || !Application.isBatchMode)
            {
                state.Enabled = false;
                return;
            }

            state.RequireForUpdate<HeadlessExitRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<HeadlessExitRequest>(out var requestEntity))
            {
                return;
            }

            var request = state.EntityManager.GetComponentData<HeadlessExitRequest>(requestEntity);
            if (_exitStage == 0)
            {
                _exitStage = 1;
                _exitStartTime = UnityEngine.Time.realtimeSinceStartupAsDouble;
                _exitCode = request.ExitCode;
                UnityDebug.Log($"[HeadlessExitSystem] Quit requested (code={request.ExitCode}, tick={request.RequestedTick}); quitting.");
                UnityDebug.Log($"[HeadlessExitSystem] headless={RuntimeMode.IsHeadless} batch={Application.isBatchMode}");

                state.Dependency.Complete();
                state.EntityManager.CompleteAllTrackedJobs();

                if (RuntimeMode.IsHeadless && Application.isBatchMode)
                {
                    if (ForceImmediateExitEnabled())
                    {
                        UnityDebug.LogWarning("[HeadlessExitSystem] ForceImmediateExit enabled; calling Environment.Exit.");
                        System.Environment.Exit(_exitCode);
                        return;
                    }
                    HeadlessExitFallback.ScheduleExit(_exitCode, 2000);
                    HeadlessExitFallback.ScheduleKill(7000);
                }

                Quit(request.ExitCode);
                return;
            }

            if (!RuntimeMode.IsHeadless || !Application.isBatchMode)
            {
                return;
            }

            var elapsed = UnityEngine.Time.realtimeSinceStartupAsDouble - _exitStartTime;
            if (_exitStage == 1 && elapsed >= 2.0)
            {
                _exitStage = 2;
                UnityDebug.LogWarning("[HeadlessExitSystem] Quit still pending; escalating to Environment.Exit.");
                HeadlessExitFallback.ScheduleKill(5000);
                System.Environment.Exit(_exitCode);
                return;
            }

            if (_exitStage == 2 && elapsed >= 7.0)
            {
                UnityDebug.LogError("[HeadlessExitSystem] Environment.Exit did not terminate; forcing process kill.");
                HeadlessExitFallback.KillImmediate();
            }
        }

        private static void Quit(int exitCode)
        {
#if UNITY_EDITOR
            if (Application.isEditor && Application.isBatchMode)
            {
                UnityEditor.EditorApplication.Exit(exitCode);
                return;
            }
#endif
            if (RuntimeMode.IsHeadless && Application.isBatchMode && ForceImmediateExitEnabled())
            {
                UnityDebug.LogWarning("[HeadlessExitSystem] ForceImmediateExit enabled; calling Environment.Exit.");
                System.Environment.Exit(exitCode);
                return;
            }
            Application.Quit(exitCode);
        }

        private static bool ForceImmediateExitEnabled()
        {
            if (Interlocked.CompareExchange(ref _forceExitInit, 1, 0) == 0)
            {
                _forceExitImmediate = IsTruthyEnv("PUREDOTS_HEADLESS_EXIT_IMMEDIATE");
            }

            return _forceExitImmediate;
        }

        private static bool IsTruthyEnv(string name)
        {
            var value = System.Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value)) { return false; }
            value = value.Trim();
            return value == "1"
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        private static class HeadlessExitFallback
        {
            private static int _scheduled;
            private static int _exitScheduled;

            public static void ScheduleKill(int delayMs)
            {
                if (Interlocked.Exchange(ref _scheduled, 1) != 0)
                {
                    return;
                }

                var thread = new Thread(() =>
                {
                    Thread.Sleep(delayMs);
                    try
                    {
                        UnityDebug.LogWarning("[HeadlessExitSystem] forced kill fallback");
                    }
                    catch
                    {
                    }
                    try
                    {
                        Process.GetCurrentProcess().Kill();
                    }
                    catch
                    {
                    }
                });
                thread.IsBackground = true;
                thread.Start();
            }

            public static void ScheduleExit(int exitCode, int delayMs)
            {
                if (Interlocked.Exchange(ref _exitScheduled, 1) != 0)
                {
                    return;
                }

                var thread = new Thread(() =>
                {
                    Thread.Sleep(delayMs);
                    try
                    {
                        UnityDebug.LogWarning("[HeadlessExitSystem] forced Environment.Exit fallback");
                    }
                    catch
                    {
                    }
                    try
                    {
                        System.Environment.Exit(exitCode);
                    }
                    catch
                    {
                    }
                });
                thread.IsBackground = true;
                thread.Start();
            }

            public static void KillImmediate()
            {
                try
                {
                    Process.GetCurrentProcess().Kill();
                }
                catch
                {
                }
            }
        }
    }
}
