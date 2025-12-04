using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Environment
{
    /// <summary>
    /// Global climate state for a world/planet.
    /// Singleton component tracking temperature, humidity, and optional seasons.
    /// </summary>
    public struct ClimateState : IComponentData
    {
        /// <summary>Current temperature (abstract units or °C).</summary>
        public float Temperature;

        /// <summary>Current humidity (0-1, where 1 = 100%).</summary>
        public float Humidity;

        /// <summary>Current season index (0=Spring, 1=Summer, 2=Fall, 3=Winter).</summary>
        public byte SeasonIndex;

        /// <summary>Current tick within the current season.</summary>
        public uint SeasonTick;

        /// <summary>Length of each season in ticks.</summary>
        public uint SeasonLength;

        /// <summary>Last update tick for climate oscillation.</summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Configuration for climate oscillation and seasonal behavior.
    /// Singleton component defining how climate changes over time.
    /// </summary>
    public struct ClimateConfig : IComponentData
    {
        /// <summary>Base temperature (center of oscillation).</summary>
        public float BaseTemperature;

        /// <summary>Base humidity (center of oscillation).</summary>
        public float BaseHumidity;

        /// <summary>Temperature oscillation amplitude.</summary>
        public float TemperatureOscillation;

        /// <summary>Humidity oscillation amplitude.</summary>
        public float HumidityOscillation;

        /// <summary>Temperature oscillation period in ticks.</summary>
        public uint TemperaturePeriod;

        /// <summary>Humidity oscillation period in ticks.</summary>
        public uint HumidityPeriod;

        /// <summary>Length of each season in ticks (if seasons enabled).</summary>
        public uint SeasonLengthTicks;

        /// <summary>Whether seasons are enabled (0 = disabled, 1 = enabled).</summary>
        public byte SeasonsEnabled;

        /// <summary>
        /// Default configuration with sensible values.
        /// </summary>
        public static ClimateConfig Default => new ClimateConfig
        {
            BaseTemperature = 20f, // 20°C (temperate)
            BaseHumidity = 0.5f, // 50% humidity
            TemperatureOscillation = 10f, // ±10°C variation
            HumidityOscillation = 0.3f, // ±30% variation
            TemperaturePeriod = 1000u, // Oscillation period
            HumidityPeriod = 800u,
            SeasonLengthTicks = 250u, // 4 seasons per 1000 ticks
            SeasonsEnabled = 0 // Disabled by default
        };
    }

    /// <summary>
    /// Per-cell climate override (for Tier-2 spatial climate).
    /// Used when climate varies spatially (e.g., deserts vs forests).
    /// </summary>
    public struct ClimateCell
    {
        /// <summary>Temperature override (additive to base).</summary>
        public float TemperatureOverride;

        /// <summary>Humidity override (additive to base).</summary>
        public float HumidityOverride;

        /// <summary>Biome type index (for reference).</summary>
        public byte BiomeIndex;
    }
}
