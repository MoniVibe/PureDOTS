using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Environment
{
    /// <summary>
    /// Pre-computed biome lookup table mapping temperature/moisture/light/chemical combinations to biome IDs.
    /// Eliminates expensive biome classification logic at runtime - just index into the LUT.
    /// </summary>
    public struct BiomeLUTBlob
    {
        // 3D matrix: [temperature_index, moisture_index, light_index] -> BiomeType
        // Temperature range: -50°C to 50°C, quantized to 100 steps (1°C per step)
        // Moisture range: 0-100, quantized to 100 steps (1% per step)
        // Light range: 0-100, quantized to 50 steps (2% per step)
        public BlobArray<BiomeType> TempMoistureLightMatrix; // [100 * 100 * 50]

        // Optional 4D matrix including chemical factors (O₂, CO₂)
        // Chemical range: 0-100 for each component, quantized to 20 steps (5% per step)
        public BlobArray<BiomeType> TempMoistureLightChemicalMatrix; // [100 * 100 * 50 * 20]

        // Resolution parameters
        public int TemperatureResolution; // Default: 100
        public int MoistureResolution;   // Default: 100
        public int LightResolution;      // Default: 50
        public int ChemicalResolution; // Default: 20

        // Value ranges for quantization
        public float TemperatureMin; // Default: -50°C
        public float TemperatureMax; // Default: 50°C
        public float MoistureMin;    // Default: 0
        public float MoistureMax;    // Default: 100
        public float LightMin;       // Default: 0
        public float LightMax;       // Default: 100
        public float ChemicalMin;    // Default: 0
        public float ChemicalMax;    // Default: 100

        /// <summary>
        /// Evaluates biome from temperature and moisture using the 3D LUT.
        /// </summary>
        public BiomeType EvaluateBiome(float temperature, float moisture, float light = 50f)
        {
            if (!TempMoistureLightMatrix.IsCreated || TempMoistureLightMatrix.Length == 0)
            {
                return BiomeType.Unknown;
            }

            var tempIndex = QuantizeTemperature(temperature);
            var moistIndex = QuantizeMoisture(moisture);
            var lightIndex = QuantizeLight(light);

            var index = tempIndex * MoistureResolution * LightResolution +
                       moistIndex * LightResolution +
                       lightIndex;

            if (index >= 0 && index < TempMoistureLightMatrix.Length)
            {
                return TempMoistureLightMatrix[index];
            }

            return BiomeType.Unknown;
        }

        /// <summary>
        /// Evaluates biome including chemical factors using the 4D LUT.
        /// </summary>
        public BiomeType EvaluateBiomeWithChemical(float temperature, float moisture, float light, float chemical)
        {
            if (!TempMoistureLightChemicalMatrix.IsCreated || TempMoistureLightChemicalMatrix.Length == 0)
            {
                // Fallback to 3D LUT
                return EvaluateBiome(temperature, moisture, light);
            }

            var tempIndex = QuantizeTemperature(temperature);
            var moistIndex = QuantizeMoisture(moisture);
            var lightIndex = QuantizeLight(light);
            var chemIndex = QuantizeChemical(chemical);

            var index = tempIndex * MoistureResolution * LightResolution * ChemicalResolution +
                       moistIndex * LightResolution * ChemicalResolution +
                       lightIndex * ChemicalResolution +
                       chemIndex;

            if (index >= 0 && index < TempMoistureLightChemicalMatrix.Length)
            {
                return TempMoistureLightChemicalMatrix[index];
            }

            return EvaluateBiome(temperature, moisture, light);
        }

        private int QuantizeTemperature(float temperature)
        {
            var normalized = (temperature - TemperatureMin) / (TemperatureMax - TemperatureMin);
            return (int)math.clamp(math.floor(normalized * TemperatureResolution), 0, TemperatureResolution - 1);
        }

        private int QuantizeMoisture(float moisture)
        {
            var normalized = (moisture - MoistureMin) / (MoistureMax - MoistureMin);
            return (int)math.clamp(math.floor(normalized * MoistureResolution), 0, MoistureResolution - 1);
        }

        private int QuantizeLight(float light)
        {
            var normalized = (light - LightMin) / (LightMax - LightMin);
            return (int)math.clamp(math.floor(normalized * LightResolution), 0, LightResolution - 1);
        }

        private int QuantizeChemical(float chemical)
        {
            var normalized = (chemical - ChemicalMin) / (ChemicalMax - ChemicalMin);
            return (int)math.clamp(math.floor(normalized * ChemicalResolution), 0, ChemicalResolution - 1);
        }
    }

    /// <summary>
    /// Singleton component providing access to the biome lookup table.
    /// </summary>
    public struct BiomeLUT : IComponentData
    {
        public BlobAssetReference<BiomeLUTBlob> Blob;

        public readonly bool IsCreated => Blob.IsCreated;

        public BiomeType EvaluateBiome(float temperature, float moisture, float light = 50f)
        {
            if (!IsCreated)
            {
                return BiomeType.Unknown;
            }

            ref var lut = ref Blob.Value;
            return lut.EvaluateBiome(temperature, moisture, light);
        }

        public BiomeType EvaluateBiomeWithChemical(float temperature, float moisture, float light, float chemical)
        {
            if (!IsCreated)
            {
                return EvaluateBiome(temperature, moisture, light);
            }

            ref var lut = ref Blob.Value;
            return lut.EvaluateBiomeWithChemical(temperature, moisture, light, chemical);
        }
    }
}

