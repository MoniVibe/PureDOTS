using System.Threading;
using Unity.Entities;
using UnityEngine;
using PureDOTS.Runtime.Components;
using Space4X.CameraComponents;

namespace PureDOTS.Diagnostics
{
    /// <summary>
    /// Introduces periodic frame hitches and logs camera/villager diagnostics to validate catch-up behaviour.
    /// </summary>
    public sealed class CatchupHarness : MonoBehaviour
    {
        [Header("Hitch Configuration")]
        [Tooltip("Seconds between injected frame hitches.")]
        [SerializeField] private float hitchIntervalSeconds = 10f;

        [Tooltip("Duration of the injected hitch in milliseconds.")]
        [SerializeField] private int hitchDurationMilliseconds = 80;

        [Header("Logging")]
        [SerializeField] private bool logCameraDiagnostics = true;

        [SerializeField] private bool logVillagerDiagnostics = true;

        private float _elapsed;
        private int _hitchCount;

        private void Update()
        {
            if (hitchIntervalSeconds <= 0f || hitchDurationMilliseconds <= 0)
            {
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < hitchIntervalSeconds)
            {
                return;
            }

            _elapsed = 0f;
            _hitchCount++;

            Thread.Sleep(Mathf.Max(0, hitchDurationMilliseconds));
            EmitDiagnostics();
        }

        private void EmitDiagnostics()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogWarning("[CatchupHarness] DOTS world not available.");
                return;
            }

            var entityManager = world.EntityManager;

            if (logCameraDiagnostics && TryGetSingleton(entityManager, out Space4XCameraDiagnostics cameraDiagnostics))
            {
                Debug.Log($"[CatchupHarness] Hitch {_hitchCount}: Camera frame={cameraDiagnostics.FrameId}, ticks={cameraDiagnostics.TicksThisFrame}, catchUp={cameraDiagnostics.CatchUpTicks}, stale={cameraDiagnostics.InputStaleTicks}, budgetTicks={cameraDiagnostics.BudgetTicksRemaining}, monoOwner={cameraDiagnostics.MonoControllerActive}");
            }

            if (logVillagerDiagnostics && TryGetSingleton(entityManager, out VillagerJobDiagnostics jobDiagnostics))
            {
                Debug.Log($"[CatchupHarness] Hitch {_hitchCount}: Villagers total={jobDiagnostics.TotalVillagers}, idle={jobDiagnostics.IdleVillagers}, assigned={jobDiagnostics.AssignedVillagers}, pendingRequests={jobDiagnostics.PendingRequests}, activeTickets={jobDiagnostics.ActiveTickets}");
            }
        }

        private static bool TryGetSingleton<T>(EntityManager entityManager, out T component) where T : unmanaged, IComponentData
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
            if (query.TryGetSingleton(out component))
            {
                return true;
            }

            component = default;
            return false;
        }
    }
}



