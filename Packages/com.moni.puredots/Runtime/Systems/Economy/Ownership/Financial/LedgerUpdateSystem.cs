using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Economy.Ownership;
using PureDOTS.Systems;

namespace PureDOTS.Systems.Economy.Ownership.Financial
{
    /// <summary>
    /// Ledger update system running at 1Hz (MindEconomySystemGroup).
    /// Calculates Income from Portfolio assets, updates Cash balance.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MindEconomySystemGroup))]
    public partial struct LedgerUpdateSystem : ISystem
    {
        private uint _lastUpdateTick;
        private const uint UpdateIntervalTicks = 60; // 1Hz at 60Hz base rate

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<TickTimeState>();
            _lastUpdateTick = 0;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record)
            {
                return;
            }

            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var currentTick = tickState.Tick;

            // Throttle to 1Hz (update every 60 ticks)
            if (currentTick - _lastUpdateTick < UpdateIntervalTicks)
            {
                return;
            }

            _lastUpdateTick = currentTick;

            var job = new LedgerUpdateJob
            {
                CurrentTick = currentTick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct LedgerUpdateJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                Entity entity,
                ref Ledger ledger,
                ref FinancialState financialState,
                in DynamicBuffer<Portfolio> portfolioBuffer)
            {
                // Skip if not dirty and recently updated
                if (!financialState.DirtyFlag && CurrentTick - financialState.LastUpdateTick < 60)
                {
                    return;
                }

                // Calculate income from portfolio: Income = Σ(asset.OutputValue × OwnershipShare)
                float totalIncome = 0f;

                for (int i = 0; i < portfolioBuffer.Length; i++)
                {
                    var portfolioEntry = portfolioBuffer[i];
                    totalIncome += portfolioEntry.ExpectedOutputValue * portfolioEntry.OwnershipShare;
                }

                // Update ledger
                ledger.Income = totalIncome;
                ledger.Cash += (ledger.Income - ledger.Expenses);
                ledger.LastUpdateTick = CurrentTick;

                // Clear dirty flag
                financialState.DirtyFlag = false;
                financialState.LastUpdateTick = CurrentTick;
            }
        }
    }
}

