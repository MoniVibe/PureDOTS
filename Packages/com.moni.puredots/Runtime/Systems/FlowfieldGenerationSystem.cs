using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Generates flowfields per zone (not per entity) for efficient pathfinding.
    /// Updates every N ticks or on topology change.
    /// Burst-compiled for SIMD-ready field generation.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SpatialSystemGroup))]
    [UpdateAfter(typeof(SpatialGridBuildSystem))]
    public partial struct FlowfieldGenerationSystem : ISystem
    {
        private uint _lastUpdateTick;
        private const uint UpdateIntervalTicks = 10; // Update every 10 ticks

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<SpatialGridState>();
            _lastUpdateTick = 0;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            var currentTick = tickTimeState.Tick;
            var spatialState = SystemAPI.GetSingleton<SpatialGridState>();

            // Check if update is needed (every N ticks or on topology change)
            bool needsUpdate = (currentTick - _lastUpdateTick >= UpdateIntervalTicks) ||
                              (spatialState.Version != _lastSpatialVersion);

            if (!needsUpdate)
            {
                return;
            }

            // Generate flowfields per zone
            // This is a placeholder - full implementation would:
            // 1. Iterate zones
            // 2. Generate flowfield vectors per cell
            // 3. Cache results in PathCacheBlob
            // 4. Update FlowfieldGrid shared component

            _lastUpdateTick = currentTick;
            _lastSpatialVersion = spatialState.Version;
        }

        private uint _lastSpatialVersion;
    }
}

