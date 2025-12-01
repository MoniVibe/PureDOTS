using PureDOTS.Environment;
using Unity.Collections;
using Unity.Entities;

namespace Godgame.Environment
{
    /// <summary>
    /// Biome specification blob. Defines biome properties and resolution weights.
    /// </summary>
    public struct BiomeSpecBlob
    {
        public FixedString32Bytes Id;
        public float TemperatureMin;
        public float TemperatureMax;
        public float HumidityMin;
        public float HumidityMax;
        public BlobArray<BiomeWeight> Weights; // For resolution scoring
        public FixedString32Bytes StyleToken; // Visual binding ID
    }

    /// <summary>
    /// Biome weight entry for resolution scoring.
    /// </summary>
    public struct BiomeWeight
    {
        public float TemperatureWeight;
        public float HumidityWeight;
        public float MoistureWeight;
    }
}

