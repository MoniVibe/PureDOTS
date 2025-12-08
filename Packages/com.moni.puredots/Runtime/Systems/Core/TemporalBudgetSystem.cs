using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;

namespace PureDOTS.Runtime.Core
{
    /// <summary>
    /// Adaptive temporal budget allocation system.
    /// Assigns time budgets dynamically per system based on load.
    /// If Body ECS nears 10ms, throttle Mind ECS to half frequency.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(TimeTickSystem))]
    public partial struct TemporalBudgetSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
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

            // Update budgets based on actual costs
            var budgetQuery = state.GetEntityQuery(typeof(TemporalBudget));
            
            if (budgetQuery.IsEmpty)
            {
                return;
            }

            // Get total budget (e.g., 16.67ms for 60 FPS)
            var totalBudget = 16.67f; // Would get from TimeState or config
            
            var job = new UpdateBudgetsJob
            {
                TotalBudgetMs = totalBudget,
                CurrentTick = tickState.Tick
            };

            state.Dependency = job.ScheduleParallel(budgetQuery, state.Dependency);

            // Redistribute budgets if needed
            RedistributeBudgets(ref state, totalBudget, tickState.Tick);
        }

        [BurstCompile]
        private void RedistributeBudgets(ref SystemState state, float totalBudget, uint currentTick)
        {
            // In full implementation, would:
            // 1. Collect all system budgets and costs
            // 2. Calculate utilization ratios
            // 3. If Body ECS nears 10ms, throttle Mind ECS
            // 4. Redistribute budgets based on priority and load
            // 5. Update CognitiveTickProfile automatically

            if (!SystemAPI.TryGetSingletonEntity<TemporalBudgetState>(out var stateEntity))
            {
                return;
            }

            var budgetQuery = state.GetEntityQuery(typeof(TemporalBudget));
            var budgets = budgetQuery.ToComponentDataArray<TemporalBudget>(Allocator.Temp);

            var totalUsed = 0f;
            for (int i = 0; i < budgets.Length; i++)
            {
                totalUsed += budgets[i].ActualCostMs;
            }

            var budgetState = new TemporalBudgetState
            {
                TotalBudgetMs = totalBudget,
                TotalUsedMs = totalUsed,
                UtilizationRatio = totalUsed / totalBudget,
                SystemCount = budgets.Length,
                LastDistributionTick = currentTick
            };

            SystemAPI.SetComponent(stateEntity, budgetState);
            budgets.Dispose();
        }

        [BurstCompile]
        private partial struct UpdateBudgetsJob : IJobEntity
        {
            public float TotalBudgetMs;
            public uint CurrentTick;

            public void Execute(ref TemporalBudget budget)
            {
                // Update rolling average
                budget.AverageCostMs = budget.AverageCostMs * 0.9f + budget.ActualCostMs * 0.1f;

                // Calculate utilization
                if (budget.AllocatedBudgetMs > 0f)
                {
                    budget.BudgetUtilization = budget.ActualCostMs / budget.AllocatedBudgetMs;
                }

                budget.LastUpdateTick = CurrentTick;
            }
        }
    }
}

