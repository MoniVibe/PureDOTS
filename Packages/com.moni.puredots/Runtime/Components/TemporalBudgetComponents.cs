using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Temporal budget allocation for a system.
    /// Tracks time budget and actual cost for adaptive scheduling.
    /// </summary>
    public struct TemporalBudget : IComponentData
    {
        public float AllocatedBudgetMs;    // Allocated time budget in milliseconds
        public float ActualCostMs;         // Actual cost last frame (ms)
        public float AverageCostMs;         // Rolling average cost (ms)
        public float BudgetUtilization;     // ActualCostMs / AllocatedBudgetMs (0-1+)
        public int Priority;                // System priority (higher = more important)
        public uint LastUpdateTick;         // When budget was last updated
    }

    /// <summary>
    /// Global temporal budget state tracking all system budgets.
    /// </summary>
    public struct TemporalBudgetState : IComponentData
    {
        public float TotalBudgetMs;         // Total available budget (e.g., 16.67ms for 60 FPS)
        public float TotalUsedMs;           // Total used across all systems
        public float UtilizationRatio;      // TotalUsedMs / TotalBudgetMs
        public int SystemCount;             // Number of systems with budgets
        public uint LastDistributionTick;   // When budgets were last redistributed
    }
}

