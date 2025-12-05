using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Performance;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Debugging
{
    /// <summary>
    /// Displays universal performance counters and budget warnings across all domains in a debug overlay.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct UniversalPerformanceDebugSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UniversalPerformanceBudget>();
            state.RequireForUpdate<UniversalPerformanceCounters>();
            state.RequireForUpdate<DebugDisplayData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var budget = SystemAPI.GetSingleton<UniversalPerformanceBudget>();
            var counters = SystemAPI.GetSingleton<UniversalPerformanceCounters>();
            var debugDisplay = SystemAPI.GetSingletonRW<DebugDisplayData>();

            // Build universal performance data string
            var text = new Unity.Collections.FixedString512Bytes();
            text.Append("=== Universal Performance ===\n");
            
            // Perception
            text.Append($"Perception: {counters.PerceptionChecksThisTick}/{budget.MaxPerceptionChecksPerTick}\n");
            
            // Combat
            text.Append($"Combat: {counters.CombatOperationsThisTick}/{budget.MaxCombatOperationsPerTick}\n");
            text.Append($"Target Selection: {counters.TargetSelectionsThisTick}/{budget.MaxTargetSelectionsPerTick}\n");
            
            // AI Layers
            text.Append($"Tactical: {counters.TacticalDecisionsThisTick}/{budget.MaxTacticalDecisionsPerTick}\n");
            text.Append($"Operational: {counters.OperationalDecisionsThisTick}/{budget.MaxOperationalDecisionsPerTick}\n");
            text.Append($"Strategic: {counters.StrategicDecisionsThisTick}/{budget.MaxStrategicDecisionsPerTick}\n");
            
            // Jobs
            text.Append($"Job Reassignments: {counters.JobReassignmentsThisTick}/{budget.MaxJobReassignmentsPerTick}\n");
            
            // World Sim
            text.Append($"Cell Updates: {counters.CellUpdatesThisTick}/{budget.MaxCellUpdatesPerTick}\n");
            text.Append($"World Sim: {counters.WorldSimOperationsThisTick}/{budget.MaxWorldSimOperationsPerTick}\n");
            
            // Aggregated
            text.Append($"Total Warm: {counters.TotalWarmOperationsThisTick}\n");
            text.Append($"Total Cold: {counters.TotalColdOperationsThisTick}\n");
            text.Append($"Operations Dropped: {counters.TotalOperationsDroppedThisTick}\n");

            // Warn if budgets exceeded
            if (counters.PerceptionChecksThisTick >= budget.MaxPerceptionChecksPerTick ||
                counters.CombatOperationsThisTick >= budget.MaxCombatOperationsPerTick ||
                counters.TotalWarmOperationsThisTick >= budget.TotalOperationsWarningThreshold ||
                counters.TotalColdOperationsThisTick >= budget.TotalOperationsWarningThreshold)
            {
                text.Append("<color=yellow>WARNING: Budget Exceeded!</color>\n");
            }

            // TODO: PerformanceDebugText field needs to be added to DebugDisplayData
            // debugDisplay.ValueRW.PerformanceDebugText = text;
        }
    }
}

