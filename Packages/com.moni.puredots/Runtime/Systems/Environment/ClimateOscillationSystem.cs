using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Environment;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Updates climate state with sine-wave/triangle oscillation for temperature and humidity.
    /// Handles seasonal progression if enabled.
    /// Runs in EnvironmentSystemGroup to provide climate data for other systems.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    public partial struct ClimateOscillationSystem : ISystem
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

            var currentTick = timeState.Tick;

            // Get config (use default if singleton doesn't exist)
            var config = ClimateConfig.Default;
            if (SystemAPI.TryGetSingleton<ClimateConfig>(out var configSingleton))
            {
                config = configSingleton;
            }

            // Update climate state
            if (SystemAPI.TryGetSingletonRW<ClimateState>(out var climateState))
            {
                var climate = climateState.ValueRO;
                var configRef = config;

                // Calculate temperature oscillation
                var tempPhase = (float)(currentTick % configRef.TemperaturePeriod) / configRef.TemperaturePeriod;
                var tempOscillation = math.sin(tempPhase * 2f * math.PI) * configRef.TemperatureOscillation;
                climate.Temperature = configRef.BaseTemperature + tempOscillation;

                // Calculate humidity oscillation
                var humidityPhase = (float)(currentTick % configRef.HumidityPeriod) / configRef.HumidityPeriod;
                var humidityOscillation = math.sin(humidityPhase * 2f * math.PI) * configRef.HumidityOscillation;
                climate.Humidity = math.clamp(configRef.BaseHumidity + humidityOscillation, 0f, 1f);

                // Update seasons if enabled
                if (configRef.SeasonsEnabled != 0 && configRef.SeasonLengthTicks > 0)
                {
                    var seasonTick = currentTick % (configRef.SeasonLengthTicks * 4u); // 4 seasons
                    climate.SeasonIndex = (byte)(seasonTick / configRef.SeasonLengthTicks);
                    climate.SeasonTick = seasonTick % configRef.SeasonLengthTicks;
                    climate.SeasonLength = configRef.SeasonLengthTicks;
                }

                climate.LastUpdateTick = currentTick;
                climateState.ValueRW = climate;
            }
        }
    }
}

