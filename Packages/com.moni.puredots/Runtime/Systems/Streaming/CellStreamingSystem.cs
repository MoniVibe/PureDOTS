using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Streaming
{
    /// <summary>
    /// System managing simulation cell activation/deactivation.
    /// Streams entire chunks in/out using EntityScene API.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CellStreamingSystem : ISystem
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

            // Manage cell streaming
            // In full implementation, would:
            // 1. Determine which cells should be active based on player/camera position
            // 2. Activate cells (load from disk if serialized)
            // 3. Deactivate cells (serialize to disk)
            // 4. Use EntityScene API for chunk serialization
            // 5. Integrate with SpatialGridSystem for cell boundaries
        }
    }
}

