using Unity.Burst;
using Unity.Entities;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Applies per-world time scales from TimeCoordinator.
    /// Allows independent time acceleration/freezing per world.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(TimeSystemGroup))]
    public partial struct TimeCoordinatorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeCoordinator>();
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var coordinator = SystemAPI.GetSingleton<TimeCoordinator>();
            var timeStateHandle = SystemAPI.GetSingletonRW<TimeState>();

            ref var timeState = ref timeStateHandle.ValueRW;

            // Apply time scale
            if (coordinator.IsFrozen)
            {
                // Freeze time
                timeState.IsPaused = true;
            }
            else
            {
                // Apply time scale
                timeState.IsPaused = false;
                // Note: TimeState.TimeScale would need to be added to TimeState component
                // For now, this is a placeholder that shows the pattern
            }

            // Update WorldTimeState if it exists
            if (SystemAPI.TryGetSingletonRW<WorldTimeState>(out var worldTimeHandle))
            {
                ref var worldTime = ref worldTimeHandle.ValueRW;
                double realTime = SystemAPI.Time.ElapsedTime * 1000.0;
                double simTime = timeState.Tick * timeState.FixedDeltaTime * 1000.0;

                worldTime.RealTimeMs = realTime;
                worldTime.SimTimeMs = simTime;

                if (realTime > 0.0)
                {
                    worldTime.CompressionFactor = (float)(simTime / realTime);
                }
            }
        }
    }
}

