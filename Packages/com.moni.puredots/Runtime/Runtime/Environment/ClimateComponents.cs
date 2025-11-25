using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Environment
{
    /// <summary>
    /// Consolidated climate and environment components for shared use by both Godgame and Space4X.
    /// These components provide the foundation for climate simulation, moisture tracking, and weather systems.
    /// </summary>
    
    /// <summary>
    /// Global climate state singleton used to coordinate seasonal and atmospheric state.
    /// Temperature, humidity, wind, and seasonal progression.
    /// </summary>
    public struct ClimateState : IComponentData
    {
        /// <summary>
        /// Current season (Spring, Summer, Autumn, Winter).
        /// </summary>
        public Season CurrentSeason;

        /// <summary>
        /// Progress through current season (0-1).
        /// </summary>
        public float SeasonProgress;

        /// <summary>
        /// Time of day in hours (0-24).
        /// </summary>
        public float TimeOfDayHours;

        /// <summary>
        /// Day-night cycle progress (0-1).
        /// </summary>
        public float DayNightProgress;

        /// <summary>
        /// Global temperature in degrees Celsius.
        /// </summary>
        public float GlobalTemperature;

        /// <summary>
        /// Global wind direction (normalized XZ vector).
        /// </summary>
        public float2 GlobalWindDirection;

        /// <summary>
        /// Global wind strength in m/s.
        /// </summary>
        public float GlobalWindStrength;

        /// <summary>
        /// Atmospheric moisture/humidity (0-100).
        /// </summary>
        public float AtmosphericMoisture;

        /// <summary>
        /// Cloud cover percentage (0-100).
        /// </summary>
        public float CloudCover;

        /// <summary>
        /// Tick when climate state was last updated.
        /// </summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Seasons for climate simulation.
    /// </summary>
    public enum Season : byte
    {
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3
    }

    /// <summary>
    /// Moisture grid singleton providing 2D moisture field across terrain.
    /// Used for biome resolution, vegetation growth, and environmental effects.
    /// </summary>
    public struct MoistureGrid : IComponentData
    {
        /// <summary>
        /// Grid width in cells.
        /// </summary>
        public int GridWidth;

        /// <summary>
        /// Grid height in cells.
        /// </summary>
        public int GridHeight;

        /// <summary>
        /// Cell size in meters.
        /// </summary>
        public float CellSize;

        /// <summary>
        /// Blob asset reference containing moisture cell data.
        /// </summary>
        public BlobAssetReference<MoistureCellBlob> Cells;

        /// <summary>
        /// Diffusion coefficient for moisture spread between cells.
        /// </summary>
        public float DiffusionCoefficient;

        /// <summary>
        /// Seepage coefficient for moisture flow.
        /// </summary>
        public float SeepageCoefficient;

        /// <summary>
        /// Tick when moisture grid was last updated.
        /// </summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Blob structure for moisture grid cell data.
    /// </summary>
    public struct MoistureCellBlob
    {
        /// <summary>
        /// Moisture values per cell (0-100).
        /// </summary>
        public BlobArray<float> Moisture;

        /// <summary>
        /// Drainage rates per cell.
        /// </summary>
        public BlobArray<float> DrainageRate;

        /// <summary>
        /// Terrain height per cell (for seepage calculations).
        /// </summary>
        public BlobArray<float> TerrainHeight;

        /// <summary>
        /// Last rain tick per cell.
        /// </summary>
        public BlobArray<uint> LastRainTick;

        /// <summary>
        /// Evaporation rates per cell.
        /// </summary>
        public BlobArray<float> EvaporationRate;
    }

    /// <summary>
    /// Global weather state singleton tracking current weather conditions.
    /// Used by both Godgame (environmental effects) and Space4X (planet conditions).
    /// </summary>
    public struct WeatherState : IComponentData
    {
        /// <summary>
        /// Current weather type (Clear, Rain, Storm, Drought).
        /// </summary>
        public WeatherType CurrentWeather;

        /// <summary>
        /// Duration remaining in ticks until weather change.
        /// </summary>
        public uint DurationRemaining;

        /// <summary>
        /// Weather intensity (0-1). Affects moisture addition rate, visual effects, etc.
        /// </summary>
        public float Intensity;

        /// <summary>
        /// Random seed for deterministic weather transitions.
        /// </summary>
        public uint WeatherSeed;
    }

    /// <summary>
    /// Weather types for the weather state system.
    /// </summary>
    public enum WeatherType : byte
    {
        Clear = 0,
        Rain = 1,
        Storm = 2,
        Drought = 3
    }
}


