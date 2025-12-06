using PureDOTS.Environment;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Authoring.Environment
{
    /// <summary>
    /// Authoring asset for generating biome lookup tables.
    /// Pre-computes biome classifications for all temperature/moisture/light combinations.
    /// </summary>
    [CreateAssetMenu(fileName = "BiomeLUT", menuName = "PureDOTS/Environment/Biome Lookup Table", order = 8)]
    public sealed class BiomeLUTAuthoring : ScriptableObject
    {
        [Header("Resolution")]
        [SerializeField, Range(10, 200)] int _temperatureResolution = 100;
        [SerializeField, Range(10, 200)] int _moistureResolution = 100;
        [SerializeField, Range(10, 100)] int _lightResolution = 50;
        [SerializeField, Range(10, 50)] int _chemicalResolution = 20;

        [Header("Value Ranges")]
        [SerializeField] float _temperatureMin = -50f;
        [SerializeField] float _temperatureMax = 50f;
        [SerializeField] float _moistureMin = 0f;
        [SerializeField] float _moistureMax = 100f;
        [SerializeField] float _lightMin = 0f;
        [SerializeField] float _lightMax = 100f;
        [SerializeField] float _chemicalMin = 0f;
        [SerializeField] float _chemicalMax = 100f;

        [Header("Biome Thresholds")]
        [SerializeField] BiomeThresholds _thresholds = new BiomeThresholds();

        [System.Serializable]
        public struct BiomeThresholds
        {
            [Header("Tundra")]
            public float TundraMaxTemp;

            [Header("Taiga")]
            public float TaigaMinTemp;
            public float TaigaMaxTemp;
            public float TaigaMinMoisture;

            [Header("Grassland")]
            public float GrasslandMinTemp;
            public float GrasslandMaxTemp;
            public float GrasslandMinMoisture;
            public float GrasslandMaxMoisture;

            [Header("Forest")]
            public float ForestMinTemp;
            public float ForestMaxTemp;
            public float ForestMinMoisture;

            [Header("Desert")]
            public float DesertMinTemp;
            public float DesertMaxMoisture;

            [Header("Rainforest")]
            public float RainforestMinTemp;
            public float RainforestMaxTemp;
            public float RainforestMinMoisture;

            [Header("Savanna")]
            public float SavannaMinTemp;
            public float SavannaMaxTemp;
            public float SavannaMinMoisture;
            public float SavannaMaxMoisture;

            [Header("Swamp")]
            public float SwampMinTemp;
            public float SwampMaxTemp;
            public float SwampMinMoisture;

            public static BiomeThresholds CreateDefaults()
            {
                return new BiomeThresholds
                {
                    TundraMaxTemp = -10f,
                    TaigaMinTemp = -10f,
                    TaigaMaxTemp = 2f,
                    TaigaMinMoisture = 55f,
                    GrasslandMinTemp = 2f,
                    GrasslandMaxTemp = 30f,
                    GrasslandMinMoisture = 35f,
                    GrasslandMaxMoisture = 70f,
                    ForestMinTemp = 2f,
                    ForestMaxTemp = 18f,
                    ForestMinMoisture = 70f,
                    DesertMinTemp = 30f,
                    DesertMaxMoisture = 30f,
                    RainforestMinTemp = 18f,
                    RainforestMaxTemp = 30f,
                    RainforestMinMoisture = 75f,
                    SavannaMinTemp = 18f,
                    SavannaMaxTemp = 30f,
                    SavannaMinMoisture = 30f,
                    SavannaMaxMoisture = 75f,
                    SwampMinTemp = 2f,
                    SwampMaxTemp = 18f,
                    SwampMinMoisture = 55f
                };
            }
        }

        public BlobAssetReference<BiomeLUTBlob> CreateBlobAsset()
        {
            var builder = new BlobBuilder(Allocator.Temp);

            ref var root = ref builder.ConstructRoot<BiomeLUTBlob>();

            // Initialize defaults if thresholds not set
            var thresholds = _thresholds.TundraMaxTemp == 0f ? BiomeThresholds.CreateDefaults() : _thresholds;

            root.TemperatureResolution = _temperatureResolution;
            root.MoistureResolution = _moistureResolution;
            root.LightResolution = _lightResolution;
            root.ChemicalResolution = _chemicalResolution;
            root.TemperatureMin = _temperatureMin;
            root.TemperatureMax = _temperatureMax;
            root.MoistureMin = _moistureMin;
            root.MoistureMax = _moistureMax;
            root.LightMin = _lightMin;
            root.LightMax = _lightMax;
            root.ChemicalMin = _chemicalMin;
            root.ChemicalMax = _chemicalMax;

            // Build 3D matrix (temperature × moisture × light)
            var matrixSize = _temperatureResolution * _moistureResolution * _lightResolution;
            var matrix = builder.Allocate(ref root.TempMoistureLightMatrix, matrixSize);

            var tempStep = (_temperatureMax - _temperatureMin) / _temperatureResolution;
            var moistStep = (_moistureMax - _moistureMin) / _moistureResolution;
            var lightStep = (_lightMax - _lightMin) / _lightResolution;

            for (int t = 0; t < _temperatureResolution; t++)
            {
                var temperature = _temperatureMin + t * tempStep;
                for (int m = 0; m < _moistureResolution; m++)
                {
                    var moisture = _moistureMin + m * moistStep;
                    for (int l = 0; l < _lightResolution; l++)
                    {
                        var light = _lightMin + l * lightStep;
                        var index = t * _moistureResolution * _lightResolution + m * _lightResolution + l;
                        matrix[index] = ClassifyBiome(temperature, moisture, light, thresholds);
                    }
                }
            }

            // Build 4D matrix (temperature × moisture × light × chemical) - optional, smaller resolution
            var matrix4DSize = _temperatureResolution * _moistureResolution * _lightResolution * _chemicalResolution;
            var matrix4D = builder.Allocate(ref root.TempMoistureLightChemicalMatrix, matrix4DSize);

            var chemStep = (_chemicalMax - _chemicalMin) / _chemicalResolution;

            for (int t = 0; t < _temperatureResolution; t++)
            {
                var temperature = _temperatureMin + t * tempStep;
                for (int m = 0; m < _moistureResolution; m++)
                {
                    var moisture = _moistureMin + m * moistStep;
                    for (int l = 0; l < _lightResolution; l++)
                    {
                        var light = _lightMin + l * lightStep;
                        for (int c = 0; c < _chemicalResolution; c++)
                        {
                            var chemical = _chemicalMin + c * chemStep;
                            var index = t * _moistureResolution * _lightResolution * _chemicalResolution +
                                       m * _lightResolution * _chemicalResolution +
                                       l * _chemicalResolution +
                                       c;
                            matrix4D[index] = ClassifyBiome(temperature, moisture, light, thresholds, chemical);
                        }
                    }
                }
            }

            var blobAsset = builder.CreateBlobAssetReference<BiomeLUTBlob>(Allocator.Persistent);
            builder.Dispose();

            return blobAsset;
        }

        private static BiomeType ClassifyBiome(float temperature, float moisture, float light, BiomeThresholds thresholds, float chemical = 0f)
        {
            // Chemical factors can override (e.g., high pollutants → desert-like)
            if (chemical > 80f)
            {
                return BiomeType.Desert;
            }

            // Water level override (if moisture > 70%, consider swamp)
            if (moisture > 70f && temperature >= thresholds.SwampMinTemp && temperature <= thresholds.SwampMaxTemp)
            {
                return BiomeType.Swamp;
            }

            // Temperature-based classification
            if (temperature <= thresholds.TundraMaxTemp)
            {
                return BiomeType.Tundra;
            }

            if (temperature <= thresholds.TaigaMaxTemp)
            {
                return moisture >= thresholds.TaigaMinMoisture ? BiomeType.Swamp : BiomeType.Taiga;
            }

            if (temperature <= thresholds.GrasslandMaxTemp)
            {
                if (moisture >= thresholds.ForestMinMoisture && temperature <= thresholds.ForestMaxTemp)
                {
                    return BiomeType.Forest;
                }

                if (moisture >= thresholds.GrasslandMinMoisture && moisture <= thresholds.GrasslandMaxMoisture)
                {
                    return BiomeType.Grassland;
                }

                if (moisture >= thresholds.SavannaMinMoisture && moisture <= thresholds.SavannaMaxMoisture)
                {
                    return BiomeType.Savanna;
                }

                return BiomeType.Savanna;
            }

            if (temperature <= thresholds.RainforestMaxTemp)
            {
                if (moisture >= thresholds.RainforestMinMoisture)
                {
                    return BiomeType.Rainforest;
                }

                if (moisture >= thresholds.GrasslandMinMoisture)
                {
                    return BiomeType.Grassland;
                }

                return BiomeType.Savanna;
            }

            // Hot temperatures
            return moisture >= thresholds.DesertMaxMoisture ? BiomeType.Savanna : BiomeType.Desert;
        }
    }
}

