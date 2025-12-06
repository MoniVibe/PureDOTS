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
    /// Dynamic load balancer for density-based world splitting.
    /// Measures entity density × CPU load, migrates hot clusters to new ECS worlds.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct DynamicLoadBalancer : ISystem
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

            // Measure load metrics
            var metricsQuery = state.GetEntityQuery(typeof(LoadBalanceMetrics));
            
            if (metricsQuery.IsEmpty)
            {
                return;
            }

            var job = new MeasureLoadJob
            {
                CurrentTick = tickState.Tick
            };

            state.Dependency = job.ScheduleParallel(metricsQuery, state.Dependency);

            // Check if rebalancing is needed
            CheckRebalance(ref state, tickState.Tick);
        }

        [BurstCompile]
        private void CheckRebalance(ref SystemState state, uint currentTick)
        {
            // Check if rebalancing is needed
            // In full implementation, would:
            // 1. Collect load metrics from all partitions
            // 2. Calculate average and max load scores
            // 3. If max load exceeds threshold, trigger rebalancing
            // 4. Split hot clusters into new worlds
            // 5. Migrate entities to new worlds

            if (!SystemAPI.TryGetSingletonEntity<LoadBalancerState>(out var stateEntity))
            {
                return;
            }

            var metricsQuery = state.GetEntityQuery(typeof(LoadBalanceMetrics));
            var metrics = metricsQuery.ToComponentDataArray<LoadBalanceMetrics>(Allocator.Temp);

            var totalLoad = 0f;
            var maxLoad = 0f;
            var activeWorlds = 0;

            for (int i = 0; i < metrics.Length; i++)
            {
                if (metrics[i].LoadScore > 0f)
                {
                    totalLoad += metrics[i].LoadScore;
                    maxLoad = math.max(maxLoad, metrics[i].LoadScore);
                    activeWorlds++;
                }
            }

            var balancerState = new LoadBalancerState
            {
                ActiveWorldCount = activeWorlds,
                AverageLoadScore = activeWorlds > 0 ? totalLoad / activeWorlds : 0f,
                MaxLoadScore = maxLoad,
                LastRebalanceTick = currentTick,
                NeedsRebalance = maxLoad > 0.8f // Threshold for rebalancing
            };

            SystemAPI.SetComponent(stateEntity, balancerState);
            metrics.Dispose();
        }

        [BurstCompile]
        private partial struct MeasureLoadJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(ref LoadBalanceMetrics metrics)
            {
                // Calculate load score: density × CPU load
                metrics.LoadScore = metrics.EntityDensity * metrics.CPULoad;
                metrics.LastUpdateTick = CurrentTick;
            }
        }
    }
}

