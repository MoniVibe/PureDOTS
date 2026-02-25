using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Runtime.Telemetry
{
    /// <summary>
    /// Ensures telemetry config + buffers exist in every gameplay world.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct BehaviorTelemetryBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!RuntimeMode.IsHeadless && !Application.isBatchMode)
            {
                // Behavior telemetry is only required for headless proof/export lanes.
                // Skip bootstrapping in editor/presentation runs to avoid structural churn on dense gameplay archetypes.
                state.Enabled = false;
                return;
            }

            if (!SystemAPI.HasSingleton<BehaviorTelemetryConfig>())
            {
                var configEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(configEntity, new BehaviorTelemetryConfig
                {
                    AggregateCadenceTicks = 30
                });
            }

            if (!SystemAPI.HasSingleton<BehaviorTelemetryState>())
            {
                var telemetryEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<BehaviorTelemetryState>(telemetryEntity);
                var buffer = state.EntityManager.AddBuffer<BehaviorTelemetryRecord>(telemetryEntity);
                buffer.Clear();
            }

            state.Enabled = false;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}
