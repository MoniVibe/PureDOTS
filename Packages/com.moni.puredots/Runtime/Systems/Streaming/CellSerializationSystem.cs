using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Streaming
{
    /// <summary>
    /// System handling deterministic cell serialization/rehydration.
    /// Inactive cells serialize to disk; rehydrate deterministically when revisited.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CellStreamingSystem))]
    public partial struct CellSerializationSystem : ISystem
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

            // Serialize/dehydrate inactive cells
            // In full implementation, would:
            // 1. Serialize inactive cells to disk using EntityScene API
            // 2. Rehydrate cells deterministically when activated
            // 3. Preserve deterministic state across serialization
        }
    }
}

