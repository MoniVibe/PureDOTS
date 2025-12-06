using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Rain-Soil loop system.
    /// Rainfall raises soil moisture → growth → evapotranspiration → clouds.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    [UpdateAfter(typeof(WindCloudSystem))]
    public partial struct RainSoilSystem : ISystem
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

            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (timeState.IsPaused)
            {
                return;
            }

            // Get cloud grid for rain rate
            if (!SystemAPI.TryGetSingleton<CloudGrid>(out var cloudGrid) || !cloudGrid.IsCreated)
            {
                return;
            }

            // Get moisture grid for soil moisture updates
            if (!SystemAPI.TryGetSingleton<MoistureGrid>(out var moistureGrid) || !moistureGrid.IsCreated)
            {
                return;
            }

            // Rain → soil moisture → growth → evapotranspiration → clouds
            // This integrates with MoistureRainSystem and vegetation growth
            // For now, this is a placeholder structure
        }
    }
}

