using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Time
{
    /// <summary>
    /// Integrates sunlight factor from orbit/time-of-day system into vegetation environment state.
    /// Updates VegetationEnvironmentState.Light field based on SunlightFactor from the planet.
    /// Runs in VegetationSystemGroup before VegetationHealthSystem so health calculations use updated light.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(VegetationSystemGroup))]
    [UpdateBefore(typeof(VegetationHealthSystem))]
    public partial struct VegetationSunlightIntegrationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            // Skip if paused or rewinding
            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // For now, we'll use a simple approach:
            // - Find the first planet with SunlightFactor (could be singleton or per-planet)
            // - Apply that sunlight to all vegetation entities
            // 
            // Future enhancement: Link vegetation entities to specific planets via a component
            // For now, assume a single global sunlight source

            float globalSunlight = 1.0f; // Default to full sunlight if no planet found

            // Try to find a planet with sunlight factor (prefer singleton if exists)
            if (SystemAPI.HasSingleton<SunlightFactor>())
            {
                globalSunlight = SystemAPI.GetSingleton<SunlightFactor>().Sunlight;
            }
            else
            {
                // Find first planet with sunlight factor
                foreach (var sunlightFactor in SystemAPI.Query<RefRO<SunlightFactor>>())
                {
                    globalSunlight = sunlightFactor.ValueRO.Sunlight;
                    break; // Use first found
                }
            }

            // Update all vegetation environment states with sunlight
            foreach (var envState in SystemAPI.Query<RefRW<VegetationEnvironmentState>>())
            {
                envState.ValueRW.Light = globalSunlight;
                envState.ValueRW.LastSampleTick = timeState.Tick;
            }
        }
    }
}

