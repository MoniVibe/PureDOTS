using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Economy.Ownership;

namespace PureDOTS.Systems.Economy.Ownership.Mind
{
    /// <summary>
    /// Investment decision system running at 1Hz (MindEconomySystemGroup).
    /// Evaluates utility using MindECS traits, enqueues PurchaseEvent.
    /// Reads from AgentSyncBus for MindECS→BodyECS communication.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MindEconomySystemGroup))]
    public partial struct InvestmentDecisionSystem : ISystem
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

            var job = new InvestmentDecisionJob
            {
                ECB = ecb.AsParallelWriter(),
                CurrentTick = currentTick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct InvestmentDecisionJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public uint CurrentTick;

            public void Execute(
                Entity investorEntity,
                ref Ledger ledger,
                in DynamicBuffer<Portfolio> portfolioBuffer,
                [ChunkIndexInQuery] int chunkIndex)
            {
                // For now, this is a placeholder that will be extended to:
                // 1. Read MindECS profile traits (Intelligence, Greed, Fear) via AgentSyncBus
                // 2. Evaluate available assets for investment opportunities
                // 3. Calculate Utility(asset) = ExpectedReturn * Intelligence + EmotionalBias * Greed - Risk * Fear
                // 4. Enqueue PurchaseEvent when Utility > Threshold

                // Placeholder: Simple investment logic
                // In full implementation, this would query available assets and evaluate utility
            }
        }
    }
}

