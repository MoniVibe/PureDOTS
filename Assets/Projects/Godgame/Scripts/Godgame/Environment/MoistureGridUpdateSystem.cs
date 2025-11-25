using PureDOTS.Environment;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Godgame.Environment
{
    /// <summary>
    /// Updates moisture grid based on weather, evaporation, and plant consumption.
    /// Runs in FixedStep simulation group.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct MoistureGridUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<MoistureGrid>(out var moistureGrid) ||
                !moistureGrid.IsCreated)
                return;

            if (!SystemAPI.TryGetSingleton<WeatherState>(out var weatherState))
                return;

            if (!SystemAPI.TryGetSingleton<ClimateState>(out var climateState))
                return;

            var dt = SystemAPI.Time.DeltaTime;

            // TODO: Update moisture grid cells
            // For MVP, this is a placeholder system
            // Future: Process moisture sources/sinks per cell
            // - Add moisture from rain weather
            // - Subtract evaporation based on temperature
            // - Subtract plant consumption
            // - Clamp to 0-1 range
        }
    }
}

