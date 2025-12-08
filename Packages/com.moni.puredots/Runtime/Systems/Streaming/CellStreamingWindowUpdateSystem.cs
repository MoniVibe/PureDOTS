using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Streaming
{
    /// <summary>
    /// Updates the active streaming window from a target (e.g., camera/player anchor).
    /// Writes into the CellStreamingWindow singleton so streaming reacts to movement.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(CellStreamingSystem))]
    public partial struct CellStreamingWindowUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<CellStreamingConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            if (tickState.IsPaused)
            {
                return;
            }

            // If there is no target, leave the current window unchanged.
            if (!SystemAPI.TryGetSingleton<CellStreamingWindowTarget>(out var target))
            {
                return;
            }

            // Ensure window singleton exists.
            Entity windowEntity;
            if (!SystemAPI.TryGetSingletonEntity<CellStreamingWindow>(out windowEntity))
            {
                windowEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<CellStreamingWindow>(windowEntity);
            }

            var window = SystemAPI.GetComponentRW<CellStreamingWindow>(windowEntity);
            window.ValueRW = new CellStreamingWindow
            {
                Center = target.Position,
                HalfExtents = target.HalfExtents
            };
        }
    }
}
