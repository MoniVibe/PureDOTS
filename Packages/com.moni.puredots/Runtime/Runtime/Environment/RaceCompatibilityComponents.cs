using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Environment
{
    /// <summary>
    /// Race preference profile defining environmental comfort ranges.
    /// Used for colonization checks, villager happiness, and biodeck validation.
    /// </summary>
    public struct RacePreferenceProfileBlob
    {
        public FixedString64Bytes RaceId;
        public float MinTemperature;      // Minimum comfortable temperature (°C)
        public float MaxTemperature;      // Maximum comfortable temperature (°C)
        public float MinMoisture;         // Minimum comfortable moisture (0-100)
        public float MaxMoisture;         // Maximum comfortable moisture (0-100)
        public float MinOxygen;           // Minimum required O₂ (0-100)
        public float MaxOxygen;            // Maximum tolerable O₂ (0-100)
        public float MinPressure;          // Minimum atmospheric pressure (kPa)
        public float MaxPressure;          // Maximum atmospheric pressure (kPa)
    }

    /// <summary>
    /// Cached biome compatibility per race per chunk.
    /// Updated on biome change, cached for N ticks.
    /// </summary>
    public struct RaceBiomeCompatibilityCache : IComponentData
    {
        public BlobAssetReference<RaceCompatibilityCacheBlob> Blob;
        public uint LastUpdateTick;
        public uint CacheDurationTicks; // How long to cache (default: 100 ticks)
    }

    /// <summary>
    /// Blob payload storing compatibility scores (chunk → race → compatibility 0-1).
    /// </summary>
    public struct RaceCompatibilityCacheBlob
    {
        public BlobArray<float> CompatibilityScores; // [chunkCount * raceCount]
        public int ChunkCount;
        public int RaceCount;
    }
}

