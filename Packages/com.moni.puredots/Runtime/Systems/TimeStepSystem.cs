using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Updates world time tick counter.
    /// Runs in InitializationSystemGroup to ensure time is updated before simulation systems execute.
    /// This aligns with DOTS 1.4 lifecycle where "Update world time" occurs in InitializationSystemGroup.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TimeStepSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var time = SystemAPI.GetSingletonRW<TimeState>();
            if (time.ValueRO.IsPaused)
            {
                return;
            }

            uint increment = 1u;
            if (time.ValueRO.CurrentSpeedMultiplier > 1f)
            {
                increment = (uint)math.max(1f, math.round(time.ValueRO.CurrentSpeedMultiplier));
            }

            time.ValueRW.Tick += increment;
        }
    }
}
