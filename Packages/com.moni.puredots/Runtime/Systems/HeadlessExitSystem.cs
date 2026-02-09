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
        private int _exitGraceMs;
        private int _exitKillMs;

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
                _exitGraceMs = ResolveExitGraceMs();
                _exitKillMs = ResolveExitKillMs(_exitGraceMs);
                UnityDebug.Log($"[HeadlessExitSystem] Quit requested (code={request.ExitCode}, tick={request.RequestedTick}); quitting.");
                UnityDebug.Log($"[HeadlessExitSystem] headless={RuntimeMode.IsHeadless} batch={Application.isBatchMode}");
                UnityDebug.Log($"[HeadlessExitSystem] exit grace={_exitGraceMs}ms kill={_exitKillMs}ms");

                state.Dependency.Complete();
                state.EntityManager.CompleteAllTrackedJobs();

                if (RuntimeMode.IsHeadless && Application.isBatchMode)
                {
                    if (ForceImmediateExitEnabled())
                    {
                        UnityDebug.LogWarning("[HeadlessExitSystem] ForceImmediateExit enabled; calling Environment.Exit.");
                        HeadlessExitFallback.ScheduleKill(_exitKillMs);
                        System.Environment.Exit(_exitCode);
                        return;
                    }
                    HeadlessExitFallback.ScheduleExit(_exitCode, _exitGraceMs);
                    HeadlessExitFallback.ScheduleKill(_exitKillMs);
                    // Avoid Application.Quit in headless runs; it can trigger a shutdown crash.
                    return;
                }

                Quit(request.ExitCode);
                return;
            }

            if (!RuntimeMode.IsHeadless || !Application.isBatchMode)
            {
                return;
            }

            var elapsedMs = (UnityEngine.Time.realtimeSinceStartupAsDouble - _exitStartTime) * 1000.0;
            if (_exitStage == 1 && elapsedMs >= _exitGraceMs)
            {
                _exitStage = 2;
                var remainingKillMs = Math.Max(1000, _exitKillMs - (int)Math.Round(elapsedMs));
                UnityDebug.LogWarning($"[HeadlessExitSystem] Quit still pending after {elapsedMs:F0}ms (grace={_exitGraceMs}ms); escalating to Environment.Exit.");
                HeadlessExitFallback.ScheduleKill(remainingKillMs);
                System.Environment.Exit(_exitCode);
                return;
            }

            if (_exitStage == 2 && elapsedMs >= _exitKillMs)
            {
                UnityDebug.LogError($"[HeadlessExitSystem] Environment.Exit did not terminate after {elapsedMs:F0}ms (kill={_exitKillMs}ms); forcing process kill.");
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
