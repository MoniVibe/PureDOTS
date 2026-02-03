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
            UnityDebug.Log($"[HeadlessExitSystem] Quit requested (code={request.ExitCode}, tick={request.RequestedTick}); quitting.");
            UnityDebug.Log($"[HeadlessExitSystem] headless={RuntimeMode.IsHeadless} batch={Application.isBatchMode}");
            if (RuntimeMode.IsHeadless && Application.isBatchMode)
            {
                // Avoid Unity shutdown crashes by exiting directly in headless batch runs.
                System.Environment.Exit(request.ExitCode);
                return;
            }

            state.Dependency.Complete();
            state.EntityManager.CompleteAllTrackedJobs();
            Quit(request.ExitCode);
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
                        System.Console.Error.WriteLine("[HeadlessExitSystem] forced kill fallback");
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
        }
    }
}
