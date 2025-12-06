using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Economy.Ownership;

namespace PureDOTS.Systems.Economy.Ownership.Financial
{
    /// <summary>
    /// Portfolio management system running at 1Hz (MindEconomySystemGroup).
    /// Handles PurchaseEvent/SaleEvent, updates Portfolio buffers.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MindEconomySystemGroup))]
    [UpdateAfter(typeof(LedgerUpdateSystem))]
    public partial struct PortfolioManagementSystem : ISystem
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

            // Throttle to 1Hz
            if (currentTick - _lastUpdateTick < UpdateIntervalTicks)
            {
                return;
            }

            _lastUpdateTick = currentTick;

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Process purchase events
            var purchaseJob = new ProcessPurchaseEventsJob
            {
                ECB = ecb.AsParallelWriter(),
                CurrentTick = currentTick
            };
            state.Dependency = purchaseJob.ScheduleParallel(state.Dependency);

            // Process sale events
            var saleJob = new ProcessSaleEventsJob
            {
                ECB = ecb.AsParallelWriter(),
                CurrentTick = currentTick
            };
            state.Dependency = saleJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct ProcessPurchaseEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public uint CurrentTick;

            public void Execute(
                Entity entity,
                DynamicBuffer<PurchaseEvent> purchaseEvents,
                ref DynamicBuffer<Portfolio> portfolioBuffer,
                ref Ledger ledger,
                ref FinancialState financialState,
                [ChunkIndexInQuery] int chunkIndex)
            {
                for (int i = purchaseEvents.Length - 1; i >= 0; i--)
                {
                    var purchase = purchaseEvents[i];

                    // Validate purchase
                    if (purchase.Price > ledger.Cash)
                    {
                        // Insufficient funds - remove event
                        purchaseEvents.RemoveAt(i);
                        continue;
                    }

                    // Deduct cash
                    ledger.Cash -= purchase.Price;

                    // Add to portfolio
                    bool found = false;
                    for (int j = 0; j < portfolioBuffer.Length; j++)
                    {
                        var entry = portfolioBuffer[j];
                        if (entry.Asset == purchase.Asset)
                        {
                            // Update existing entry
                            entry.OwnershipShare = math.min(1.0f, entry.OwnershipShare + purchase.Share);
                            portfolioBuffer[j] = entry;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // Add new portfolio entry
                        portfolioBuffer.Add(new Portfolio
                        {
                            Asset = purchase.Asset,
                            OwnershipShare = purchase.Share,
                            ExpectedOutputValue = 0f // Will be calculated by asset production systems
                        });
                    }

                    // Mark financial state as dirty for recalculation
                    financialState.DirtyFlag = true;

                    // Remove processed event
                    purchaseEvents.RemoveAt(i);
                }
            }
        }

        [BurstCompile]
        private partial struct ProcessSaleEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public uint CurrentTick;

            public void Execute(
                Entity entity,
                DynamicBuffer<SaleEvent> saleEvents,
                ref DynamicBuffer<Portfolio> portfolioBuffer,
                ref Ledger ledger,
                ref FinancialState financialState,
                [ChunkIndexInQuery] int chunkIndex)
            {
                for (int i = saleEvents.Length - 1; i >= 0; i--)
                {
                    var sale = saleEvents[i];

                    // Find portfolio entry
                    bool found = false;
                    for (int j = 0; j < portfolioBuffer.Length; j++)
                    {
                        var entry = portfolioBuffer[j];
                        if (entry.Asset == sale.Asset)
                        {
                            // Validate share
                            if (entry.OwnershipShare < sale.Share)
                            {
                                // Insufficient ownership - remove event
                                saleEvents.RemoveAt(i);
                                break;
                            }

                            // Add cash
                            ledger.Cash += sale.Price;

                            // Update portfolio
                            entry.OwnershipShare -= sale.Share;
                            if (entry.OwnershipShare <= 0f)
                            {
                                // Remove entry if no ownership remaining
                                portfolioBuffer.RemoveAt(j);
                            }
                            else
                            {
                                portfolioBuffer[j] = entry;
                            }

                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // Asset not in portfolio - remove event
                        saleEvents.RemoveAt(i);
                        continue;
                    }

                    // Mark financial state as dirty
                    financialState.DirtyFlag = true;

                    // Remove processed event
                    saleEvents.RemoveAt(i);
                }
            }
        }
    }
}

