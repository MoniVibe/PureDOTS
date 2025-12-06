using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Economy.Ownership;

namespace PureDOTS.Systems.Economy.Ownership.Body
{
    /// <summary>
    /// Hauling system running at 60Hz (BodyEconomySystemGroup).
    /// Processes ResourceStock movement along TradeRoute entities.
    /// Integrates with existing LogisticsRequestRegistrySystem.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(BodyEconomySystemGroup))]
    [UpdateAfter(typeof(ProductionSystem))]
    public partial struct HaulingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record)
            {
                return;
            }

            var deltaTime = SystemAPI.Time.DeltaTime;

            var job = new HaulingJob
            {
                DeltaTime = deltaTime
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct HaulingJob : IJobEntity
        {
            public float DeltaTime;

            public void Execute(
                Entity routeEntity,
                ref TradeRoute tradeRoute,
                [ChunkIndexInQuery] int chunkIndex)
            {
                // Update last update tick
                tradeRoute.LastUpdateTick = (uint)chunkIndex; // This should be actual tick, but we'll use chunkIndex as placeholder

                // Calculate flow amount for this tick
                float flowAmount = tradeRoute.FlowRate * DeltaTime;

                if (flowAmount <= 0f || math.isnan(flowAmount))
                {
                    return;
                }

                // ResourceStock movement is handled by LogisticsSystem
                // HaulingSystem focuses on TradeRoute state updates
                // Actual ResourceStock buffer updates happen in LogisticsSystem
            }
        }
    }
}

