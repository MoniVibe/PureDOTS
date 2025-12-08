using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Telemetry;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems
{
    internal static class PresentationPoolTelemetryKeys
    {
        public static readonly FixedString64Bytes Active = "presentation.pool.active";
        public static readonly FixedString64Bytes SpawnedFrame = "presentation.pool.spawned_frame";
        public static readonly FixedString64Bytes RecycledFrame = "presentation.pool.recycled_frame";
        public static readonly FixedString64Bytes TotalSpawned = "presentation.pool.total_spawned";
        public static readonly FixedString64Bytes TotalRecycled = "presentation.pool.total_recycled";
    }

    /// <summary>
    /// Emits telemetry counters for presentation pooling so HUD/analytics can surface them.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PresentationRecycleSystem))]
    public partial struct PresentationPoolTelemetrySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PresentationPoolStats>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var stats = SystemAPI.GetSingleton<PresentationPoolStats>();
            TelemetryHub.Enqueue(new TelemetryMetric { Key = PresentationPoolTelemetryKeys.Active, Value = stats.ActiveVisuals, Unit = TelemetryMetricUnit.Count });
            TelemetryHub.Enqueue(new TelemetryMetric { Key = PresentationPoolTelemetryKeys.SpawnedFrame, Value = stats.SpawnedThisFrame, Unit = TelemetryMetricUnit.Count });
            TelemetryHub.Enqueue(new TelemetryMetric { Key = PresentationPoolTelemetryKeys.RecycledFrame, Value = stats.RecycledThisFrame, Unit = TelemetryMetricUnit.Count });
            TelemetryHub.Enqueue(new TelemetryMetric { Key = PresentationPoolTelemetryKeys.TotalSpawned, Value = stats.TotalSpawned, Unit = TelemetryMetricUnit.Count });
            TelemetryHub.Enqueue(new TelemetryMetric { Key = PresentationPoolTelemetryKeys.TotalRecycled, Value = stats.TotalRecycled, Unit = TelemetryMetricUnit.Count });
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
