using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems.Telemetry;
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
                state.Dependency.Complete();
                state.EntityManager.CompleteAllTrackedJobs();
                HeadlessExitFallback.Immediate(request.ExitCode);
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

            public static void Schedule(int exitCode, int delayMs)
            {
                if (Interlocked.Exchange(ref _scheduled, 1) != 0)
                {
                    return;
                }

                Task.Run(() =>
                {
                    Thread.Sleep(delayMs);
                    try
                    {
                        System.Console.Error.WriteLine("[HeadlessExitSystem] forced exit fallback");
                    }
                    catch
                    {
                    }
                    System.Environment.Exit(exitCode);
                });
            }

            public static void Immediate(int exitCode)
            {
                if (Interlocked.Exchange(ref _scheduled, 1) != 0)
                {
                    return;
                }

                System.Environment.Exit(exitCode);
            }
        }
    }
}
