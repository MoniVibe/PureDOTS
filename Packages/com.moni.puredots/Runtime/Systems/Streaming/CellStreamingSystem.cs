using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

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
            state.RequireForUpdate<CellStreamingConfig>();
            state.RequireForUpdate<CellStreamingWindow>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            if (tickState.IsPaused)
            {
                return;
            }

            // Require config + window; if missing, no-op to avoid churn.
            if (!SystemAPI.TryGetSingleton<CellStreamingConfig>(out var config) ||
                !SystemAPI.TryGetSingleton<CellStreamingWindow>(out var window))
            {
                return;
            }

            var windowCenterXZ = new float2(window.Center.x, window.Center.z);
            var windowHalfExtents = window.HalfExtents;
            var hysteresis = config.Hysteresis;
            var cellSize = config.CellSize;

            // Toggle desired active state based on window overlap (XZ plane).
            foreach (var (cell, streamingState, _) in SystemAPI.Query<
                         RefRW<SimulationCell>,
                         RefRW<CellStreamingState>,
                         DynamicBuffer<CellAgentBuffer>>())
            {
                var cellCenter = (float2)cell.ValueRO.CellCoordinates * cellSize + (cellSize * 0.5f);
                var delta = math.abs(cellCenter - windowCenterXZ);
                var inWindow = math.all(delta <= (windowHalfExtents + hysteresis));

                var wasActive = cell.ValueRO.IsActive != 0;
                if (inWindow && !wasActive)
                {
                    cell.ValueRW.IsActive = 1;
                    cell.ValueRW.LastActivationTick = tickState.Tick;
                    // Serialization system will rehydrate if needed.
                }
                else if (!inWindow && wasActive)
                {
                    cell.ValueRW.IsActive = 0;
                    cell.ValueRW.LastDeactivationTick = tickState.Tick;
                    // Serialization system will serialize if needed.
                }
            }
        }
    }
}
