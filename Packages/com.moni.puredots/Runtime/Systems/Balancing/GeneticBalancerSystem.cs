using PureDOTS.Runtime.Balancing;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Balancing
{
    /// <summary>
    /// System running genetic balancer for automated parameter tuning.
    /// Outputs JSON patches to Blob manifests for CI integration.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct GeneticBalancerSystem : ISystem
    {
        private GeneticBalancer _balancer;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            _balancer = new GeneticBalancer(50, Unity.Collections.Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _balancer.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            if (tickState.IsPaused)
            {
                return;
            }

            // Run genetic balancer
            // In full implementation, would:
            // 1. Evolve population periodically (e.g., every 1000 ticks)
            // 2. Evaluate fitness (performance + behavior diversity)
            // 3. Output JSON patches to Blob manifests
            // 4. Integrate with CI pipeline for nightly optimization
        }
    }
}

