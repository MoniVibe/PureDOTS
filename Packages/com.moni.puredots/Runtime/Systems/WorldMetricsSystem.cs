using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Profiling;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Counts entities once per second and stores counts in WorldMetrics singleton.
    /// Provides simple metrics for debug overlay display.
    /// Game-specific entity types (MiningVessel, CarrierTag, Asteroid) are counted
    /// by game-specific systems that update WorldMetrics directly.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct WorldMetricsSystem : ISystem
    {
        private static readonly ProfilerMarker UpdateMetricsMarker = new("WorldMetricsSystem.UpdateMetrics");
        private uint _lastUpdateTick;
        private const uint UpdateIntervalTicks = 60; // Update once per second at 60 Hz

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _lastUpdateTick = 0;
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            
            // Update once per second
            if (timeState.Tick - _lastUpdateTick < UpdateIntervalTicks)
            {
                return;
            }

            using (UpdateMetricsMarker.Auto())
            {
                _lastUpdateTick = timeState.Tick;

                // Ensure WorldMetrics singleton exists
                Entity metricsEntity;
                if (!SystemAPI.TryGetSingletonEntity<WorldMetrics>(out metricsEntity))
                {
                    metricsEntity = state.EntityManager.CreateEntity();
                    state.EntityManager.AddComponent<WorldMetrics>(metricsEntity);
                }

                var metrics = SystemAPI.GetComponentRW<WorldMetrics>(metricsEntity);

                // Count PureDOTS entities
                var villagerQuery = SystemAPI.QueryBuilder().WithAll<VillagerId>().Build();
                metrics.ValueRW.VillagerCount = villagerQuery.CalculateEntityCount();

                var resourceQuery = SystemAPI.QueryBuilder().WithAll<ResourceSourceConfig>().Build();
                metrics.ValueRW.ResourceCount = resourceQuery.CalculateEntityCount();

                var storehouseQuery = SystemAPI.QueryBuilder().WithAll<StorehouseConfig>().Build();
                metrics.ValueRW.StorehouseCount = storehouseQuery.CalculateEntityCount();

                // Game-specific counts (MiningVessel, CarrierTag, Asteroid) should be updated
                // by game-specific systems that have access to those types.
                // This system only handles PureDOTS-agnostic counts.

                // Frame time would need to be tracked separately (not in this system)
                // For now, leave it at 0 or calculate from TimeState if available
                metrics.ValueRW.AverageFrameTimeMs = 0f; // TODO: Track frame time if needed
                metrics.ValueRW.LastUpdateTick = timeState.Tick;
            }
        }
    }
}

