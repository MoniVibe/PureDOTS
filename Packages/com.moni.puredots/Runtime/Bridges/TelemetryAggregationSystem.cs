using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Shared;
using PureDOTS.Systems;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Global telemetry collection system.
    /// Aggregates telemetry from all agents for utility learning.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BodyToMindSyncSystem))]
    public partial struct TelemetryAggregationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AgentSyncState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Aggregate telemetry from all agents
            // In full implementation, would:
            // 1. Read BodyToMind messages from AgentSyncBus
            // 2. Aggregate global statistics (average morale, health, etc.)
            // 3. Store aggregated telemetry for UtilityLearningSystem
            // 4. Update InfluenceFieldData with global averages
        }
    }
}

