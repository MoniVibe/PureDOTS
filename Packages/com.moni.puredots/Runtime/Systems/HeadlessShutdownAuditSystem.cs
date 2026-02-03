using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Time;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Emits a compact audit log right before headless shutdown.
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(HeadlessExitSystem))]
    public partial struct HeadlessShutdownAuditSystem : ISystem
    {
        private byte _logged;

        public void OnCreate(ref SystemState state)
        {
            if (!RuntimeMode.IsHeadless || !Application.isBatchMode || !BugHuntGate.ShutdownAuditEnabled)
            {
                state.Enabled = false;
                return;
            }

            state.RequireForUpdate<HeadlessExitRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_logged != 0)
            {
                return;
            }

            _logged = 1;
            var em = state.EntityManager;
            var totalEntities = em.UniversalQuery.CalculateEntityCount();
            var tick = SystemAPI.TryGetSingleton<TimeState>(out var timeState) ? timeState.Tick : 0u;
            var scenarioId = SystemAPI.TryGetSingleton<ScenarioInfo>(out var info) ? info.ScenarioId.ToString() : "unknown";

            Debug.Log($"[ShutdownAudit] tick={tick} scenario={scenarioId} entities={totalEntities} bughunt_disabled={BugHuntGate.DisabledRaw}");
            Debug.Log($"[ShutdownAudit] worlds={World.All.Count} job_component_count={Count<HeadlessExitRequest>(em)}");
        }

        private static int Count<T>(EntityManager em) where T : unmanaged, IComponentData
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.CalculateEntityCount();
        }
    }
}
