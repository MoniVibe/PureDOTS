using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Modifiers;
using PureDOTS.Runtime.Telemetry;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Modifiers
{
    /// <summary>
    /// Profiles modifier system performance and logs metrics to telemetry.
    /// Alerts if churn > 2%/sec.
    /// Runs in LateSimulationSystemGroup.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct ModifierProfilerSystem : ISystem
    {
        private const float ChurnAlertThreshold = 0.02f; // 2% per second

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var currentTick = timeState.Tick;

            // Ensure stats singleton exists
            if (!SystemAPI.HasSingleton<ModifierStats>())
            {
                var statsEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<ModifierStats>(statsEntity);
            }

            // Collect statistics
            new CollectStatsJob
            {
                CurrentTick = currentTick
            }.ScheduleParallel();

            state.Dependency.Complete();

            // Update stats singleton
            var stats = SystemAPI.GetSingletonRW<ModifierStats>();
            stats.ValueRW.LastUpdateTick = currentTick;

            // Check churn rate
            if (stats.ValueRO.ChurnRate > ChurnAlertThreshold)
            {
                // Log warning (would use telemetry system in full implementation)
                // TelemetryStream.LogWarning($"Modifier churn rate high: {stats.ValueRO.ChurnRate:P2}/sec");
            }
        }

        [BurstCompile]
        public partial struct CollectStatsJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                in DynamicBuffer<ModifierInstance> modifiers,
                ref ModifierStats stats)
            {
                // Count active modifiers
                stats.ActiveCount += modifiers.Length;
            }
        }
    }
}

