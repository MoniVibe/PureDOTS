using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Relations;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Debugging
{
    /// <summary>
    /// Displays relation/econ/social performance counters and budget warnings in a debug overlay.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct RelationPerformanceDebugSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RelationPerformanceBudget>();
            state.RequireForUpdate<RelationPerformanceCounters>();
            state.RequireForUpdate<DebugDisplayData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var budget = SystemAPI.GetSingleton<RelationPerformanceBudget>();
            var counters = SystemAPI.GetSingleton<RelationPerformanceCounters>();
            var debugDisplay = SystemAPI.GetSingletonRW<DebugDisplayData>();

            // Build relation/econ performance data string
            var text = new Unity.Collections.FixedString512Bytes();
            text.Append("--- Relation/Econ Performance ---\n");
            text.Append($"Relation Events: {counters.RelationEventsThisTick}/{budget.MaxRelationEventsPerTick}\n");
            text.Append($"Market Updates: {counters.MarketUpdatesThisTick}/{budget.MaxMarketUpdatesPerTick}\n");
            text.Append($"Political Decisions: {counters.PoliticalDecisionsThisTick}/{budget.MaxPoliticalDecisionsPerTick}\n");
            text.Append($"Social Interactions: {counters.SocialInteractionsThisTick}/{budget.MaxSocialInteractionsPerTick}\n");
            text.Append($"Personal Relations: {counters.TotalPersonalRelations} (Max: {budget.MaxPersonalRelationsPerIndividual})\n");
            text.Append($"Org Relations: {counters.TotalOrgRelations} (Max: {budget.MaxOrgRelationsPerOrg})\n");
            text.Append($"Operations Dropped: {counters.OperationsDroppedThisTick}\n");

            // Warn if budgets exceeded
            if (counters.RelationEventsThisTick >= budget.MaxRelationEventsPerTick ||
                counters.MarketUpdatesThisTick >= budget.MaxMarketUpdatesPerTick ||
                counters.PoliticalDecisionsThisTick >= budget.MaxPoliticalDecisionsPerTick ||
                counters.SocialInteractionsThisTick >= budget.MaxSocialInteractionsPerTick)
            {
                text.Append("<color=yellow>WARNING: Budget Exceeded!</color>\n");
            }

            // Warn if graph sizes too large
            if (counters.TotalPersonalRelations > budget.RelationGraphWarningThreshold)
            {
                text.Append($"<color=yellow>WARNING: Personal Relations Graph Large ({counters.TotalPersonalRelations})</color>\n");
            }

            // TODO: PerformanceDebugText field needs to be added to DebugDisplayData
            // debugDisplay.ValueRW.PerformanceDebugText = text;
        }
    }
}

