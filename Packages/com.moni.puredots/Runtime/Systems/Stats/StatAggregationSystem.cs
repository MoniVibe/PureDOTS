using PureDOTS.Runtime.Stats;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Stats
{
    /// <summary>
    /// System that computes aggregate stat values for entity groups (e.g., fleet averages).
    /// Updates at configurable intervals to avoid performance issues.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(StatTelemetrySystem))]
    public partial struct StatAggregationSystem : ISystem
    {
        /// <summary>
        /// Update interval in ticks (default: every 60 ticks = 1 second at 60 FPS).
        /// </summary>
        private const uint UpdateInterval = 60;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            uint currentTick = timeState.Tick;

            // Only update at intervals
            if (currentTick % UpdateInterval != 0)
            {
                return;
            }

            // Compute aggregate stats for entities with IndividualStats
            // This is a placeholder - actual implementation will:
            // 1. Query all entities with IndividualStats
            // 2. Group by fleet/team/aggregate
            // 3. Compute averages, min, max
            // 4. Store in FleetStatAggregate or similar component
            // 5. Publish telemetry metrics (space4x.stats.command.avg, etc.)
        }
    }

    /// <summary>
    /// Aggregate stat component for fleet/team-level stat queries.
    /// </summary>
    public struct FleetStatAggregate : IComponentData
    {
        public half AvgCommand;
        public half MaxCommand;
        public half MinCommand;
        public half AvgTactics;
        public half MaxTactics;
        public half MinTactics;
        public half AvgLogistics;
        public half MaxLogistics;
        public half MinLogistics;
        public half AvgDiplomacy;
        public half MaxDiplomacy;
        public half MinDiplomacy;
        public half AvgEngineering;
        public half MaxEngineering;
        public half MinEngineering;
        public half AvgResolve;
        public half MaxResolve;
        public half MinResolve;
    }
}

