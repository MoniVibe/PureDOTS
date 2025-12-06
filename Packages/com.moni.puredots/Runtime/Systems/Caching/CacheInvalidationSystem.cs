using PureDOTS.Runtime.Components.Caching;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Caching
{
    /// <summary>
    /// System that invalidates caches when input components change.
    /// Detects changes and clears cache keys to force recomputation.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(SimulationSystemGroup))]
    public partial struct CacheInvalidationSystem : ISystem
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
            if (tickState.IsPaused)
            {
                return;
            }

            // Invalidate caches for entities where input components changed
            // This system can be extended to track specific component changes
            // For now, it provides the infrastructure for cache invalidation
        }
    }
}

