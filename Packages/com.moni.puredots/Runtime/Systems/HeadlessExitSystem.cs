using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems.Telemetry;
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
            if (RuntimeMode.IsHeadless && Application.isBatchMode)
            {
                // Avoid Unity shutdown crashes in headless by terminating immediately.
                HeadlessExitFallback.ScheduleKill(5000);
                System.Environment.Exit(exitCode);
                return;
            }

            Application.Quit(exitCode);
        }

        private static class HeadlessExitFallback
        {
            private static int _scheduled;

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
