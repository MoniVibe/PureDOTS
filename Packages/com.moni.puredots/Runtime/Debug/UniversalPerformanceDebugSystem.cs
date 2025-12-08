using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Performance;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Debugging
{
    /// <summary>
    /// Displays universal performance counters and budget warnings across all domains in a debug overlay.
    /// </summary>
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    public partial struct UniversalPerformanceDebugSystem : ISystem
    {
        private static readonly FixedString128Bytes HeaderLabel = "=== Universal Performance ===\n";
        private static readonly FixedString64Bytes PerceptionLabel = "Perception: ";
        private static readonly FixedString64Bytes CombatLabel = "Combat: ";
        private static readonly FixedString64Bytes TargetSelectionLabel = "Target Selection: ";
        private static readonly FixedString64Bytes TacticalLabel = "Tactical: ";
        private static readonly FixedString64Bytes OperationalLabel = "Operational: ";
        private static readonly FixedString64Bytes StrategicLabel = "Strategic: ";
        private static readonly FixedString64Bytes JobReassignLabel = "Job Reassignments: ";
        private static readonly FixedString64Bytes CellUpdatesLabel = "Cell Updates: ";
        private static readonly FixedString64Bytes WorldSimLabel = "World Sim: ";
        private static readonly FixedString64Bytes TotalWarmLabel = "Total Warm: ";
        private static readonly FixedString64Bytes TotalColdLabel = "Total Cold: ";
        private static readonly FixedString64Bytes OperationsDroppedLabel = "Operations Dropped: ";
        private static readonly FixedString128Bytes WarningLabel = "<color=yellow>WARNING: Budget Exceeded!</color>\n";

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UniversalPerformanceBudget>();
            state.RequireForUpdate<UniversalPerformanceCounters>();
            state.RequireForUpdate<DebugDisplayData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var budget = SystemAPI.GetSingleton<UniversalPerformanceBudget>();
            var counters = SystemAPI.GetSingleton<UniversalPerformanceCounters>();
            var debugDisplay = SystemAPI.GetSingletonRW<DebugDisplayData>();

            var text = new FixedString512Bytes();
            text.Append(HeaderLabel);
            AppendRatio(ref text, PerceptionLabel, counters.PerceptionChecksThisTick, budget.MaxPerceptionChecksPerTick);
            AppendRatio(ref text, CombatLabel, counters.CombatOperationsThisTick, budget.MaxCombatOperationsPerTick);
            AppendRatio(ref text, TargetSelectionLabel, counters.TargetSelectionsThisTick, budget.MaxTargetSelectionsPerTick);
            AppendRatio(ref text, TacticalLabel, counters.TacticalDecisionsThisTick, budget.MaxTacticalDecisionsPerTick);
            AppendRatio(ref text, OperationalLabel, counters.OperationalDecisionsThisTick, budget.MaxOperationalDecisionsPerTick);
            AppendRatio(ref text, StrategicLabel, counters.StrategicDecisionsThisTick, budget.MaxStrategicDecisionsPerTick);
            AppendRatio(ref text, JobReassignLabel, counters.JobReassignmentsThisTick, budget.MaxJobReassignmentsPerTick);
            AppendRatio(ref text, CellUpdatesLabel, counters.CellUpdatesThisTick, budget.MaxCellUpdatesPerTick);
            AppendRatio(ref text, WorldSimLabel, counters.WorldSimOperationsThisTick, budget.MaxWorldSimOperationsPerTick);
            AppendValue(ref text, TotalWarmLabel, counters.TotalWarmOperationsThisTick);
            AppendValue(ref text, TotalColdLabel, counters.TotalColdOperationsThisTick);
            AppendValue(ref text, OperationsDroppedLabel, counters.TotalOperationsDroppedThisTick);

            if (counters.PerceptionChecksThisTick >= budget.MaxPerceptionChecksPerTick ||
                counters.CombatOperationsThisTick >= budget.MaxCombatOperationsPerTick ||
                counters.TotalWarmOperationsThisTick >= budget.TotalOperationsWarningThreshold ||
                counters.TotalColdOperationsThisTick >= budget.TotalOperationsWarningThreshold)
            {
                text.Append(WarningLabel);
            }

            // TODO: PerformanceDebugText field needs to be added to DebugDisplayData
            // debugDisplay.ValueRW.PerformanceDebugText = text;
        }

        private static void AppendRatio(ref FixedString512Bytes text, in FixedString64Bytes label, int current, int max)
        {
            text.Append(label);
            text.Append(current);
            text.Append('/');
            text.Append(max);
            text.Append('\n');
        }

        private static void AppendValue(ref FixedString512Bytes text, in FixedString64Bytes label, int value)
        {
            text.Append(label);
            text.Append(value);
            text.Append('\n');
        }
    }
}

