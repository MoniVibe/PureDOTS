using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Derives biome classifications per cell using current moisture and temperature fields.
    /// Uses BiomeLUT for fast lookup and processes only dirty chunks for incremental updates.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    [UpdateAfter(typeof(BiomeChunkDirtyTrackingSystem))]
    public partial struct BiomeDerivationSystem : ISystem
    {
        private TimeAwareController _timeAware;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<BiomeGrid>();
            state.RequireForUpdate<MoistureGrid>();
            state.RequireForUpdate<TemperatureGrid>();

            _timeAware = new TimeAwareController(
                TimeAwareExecutionPhase.Record | TimeAwareExecutionPhase.CatchUp,
                TimeAwareExecutionOptions.SkipWhenPaused);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (!_timeAware.TryBegin(timeState, rewindState, out _))
            {
                return;
            }

            var biomeEntity = SystemAPI.GetSingletonEntity<BiomeGrid>();
            if (!SystemAPI.HasBuffer<BiomeGridRuntimeCell>(biomeEntity))
            {
                return;
            }

            var biomeBuffer = SystemAPI.GetBuffer<BiomeGridRuntimeCell>(biomeEntity);
            if (biomeBuffer.Length == 0)
            {
                return;
            }

            var biomeGrid = SystemAPI.GetSingletonRW<BiomeGrid>();
            var moistureGrid = SystemAPI.GetSingleton<MoistureGrid>();
            var temperatureGrid = SystemAPI.GetSingleton<TemperatureGrid>();
            
            // Check terrain version - if terrain changed, force biome recalculation
            uint currentTerrainVersion = 0;
            var forceFullRebuild = false;
            if (SystemAPI.TryGetSingleton<PureDOTS.Environment.TerrainVersion>(out var terrainVersion))
            {
                currentTerrainVersion = terrainVersion.Value;
                if (currentTerrainVersion != biomeGrid.ValueRO.LastTerrainVersion)
                {
                    // Terrain changed, force biome update
                    biomeGrid.ValueRW.LastTerrainVersion = currentTerrainVersion;
                    biomeGrid.ValueRW.LastUpdateTick = uint.MaxValue; // Force rebuild
                    forceFullRebuild = true;
                }
            }

            // Try to use BiomeLUT if available
            BlobAssetReference<BiomeLUTBlob> biomeLUT = default;
            var hasLUT = SystemAPI.TryGetSingleton<BiomeLUT>(out var lut) && lut.IsCreated;
            if (hasLUT)
            {
                biomeLUT = lut.Blob;
            }

            // Get chunk metadata if available (for incremental updates)
            BiomeChunkMetadata chunkMetadata = default;
            NativeArray<BiomeChunkDirtyFlag> dirtyFlags = default;
            var useChunks = SystemAPI.HasComponent<BiomeChunkMetadata>(biomeEntity) &&
                           SystemAPI.HasBuffer<BiomeChunkDirtyFlag>(biomeEntity);
            if (useChunks)
            {
                chunkMetadata = SystemAPI.GetComponent<BiomeChunkMetadata>(biomeEntity);
                dirtyFlags = SystemAPI.GetBuffer<BiomeChunkDirtyFlag>(biomeEntity).AsNativeArray();
            }

            NativeArray<MoistureGridRuntimeCell> moistureRuntime = default;
            var hasMoistureRuntime = false;
            if (SystemAPI.TryGetSingletonEntity<MoistureGrid>(out var moistureEntity) &&
                SystemAPI.HasBuffer<MoistureGridRuntimeCell>(moistureEntity))
            {
                var buffer = SystemAPI.GetBuffer<MoistureGridRuntimeCell>(moistureEntity);
                if (buffer.Length == biomeBuffer.Length)
                {
                    moistureRuntime = buffer.AsNativeArray();
                    hasMoistureRuntime = true;
                }
            }

            NativeArray<ClimateGridRuntimeCell> climateRuntime = default;
            var hasClimateRuntime = false;
            if (SystemAPI.TryGetSingletonEntity<MoistureGrid>(out var climateEntity) &&
                SystemAPI.HasBuffer<ClimateGridRuntimeCell>(climateEntity))
            {
                var buffer = SystemAPI.GetBuffer<ClimateGridRuntimeCell>(climateEntity);
                if (buffer.Length == biomeBuffer.Length)
                {
                    climateRuntime = buffer.AsNativeArray();
                    hasClimateRuntime = true;
                }
            }

            // Get light and chemical fields if available
            BlobAssetReference<SunlightGridBlob> sunlightBlob = default;
            BlobAssetReference<ChemicalFieldBlob> chemicalBlob = default;
            if (SystemAPI.TryGetSingleton<SunlightGrid>(out var sunlightGrid))
            {
                sunlightBlob = sunlightGrid.Blob;
            }
            if (SystemAPI.TryGetSingleton<ChemicalField>(out var chemicalField))
            {
                chemicalBlob = chemicalField.Blob;
            }

            if (useChunks && !forceFullRebuild)
            {
                // Process only dirty chunks
                var job = new BiomeDerivationChunkJob
                {
                    Biomes = biomeBuffer.AsNativeArray(),
                    ChunkMetadata = chunkMetadata,
                    DirtyFlags = dirtyFlags,
                    MoistureRuntime = moistureRuntime,
                    MoistureBlob = moistureGrid.Blob,
                    HasMoistureRuntime = hasMoistureRuntime,
                    TemperatureBlob = temperatureGrid.Blob,
                    SunlightBlob = sunlightBlob,
                    ChemicalBlob = chemicalBlob,
                    ClimateRuntime = climateRuntime,
                    HasClimateRuntime = hasClimateRuntime,
                    BiomeLUT = biomeLUT,
                    HasLUT = hasLUT
                };

                state.Dependency = job.Schedule(chunkMetadata.TotalChunkCount, 1, state.Dependency);
            }
            else
            {
                // Full rebuild (all cells)
                var job = new BiomeDerivationJob
                {
                    Biomes = biomeBuffer.AsNativeArray(),
                    MoistureRuntime = moistureRuntime,
                    MoistureBlob = moistureGrid.Blob,
                    HasMoistureRuntime = hasMoistureRuntime,
                    TemperatureBlob = temperatureGrid.Blob,
                    SunlightBlob = sunlightBlob,
                    ChemicalBlob = chemicalBlob,
                    ClimateRuntime = climateRuntime,
                    HasClimateRuntime = hasClimateRuntime,
                    BiomeLUT = biomeLUT,
                    HasLUT = hasLUT
                };

                state.Dependency = job.ScheduleParallel(biomeBuffer.Length, 64, state.Dependency);
            }

            biomeGrid.ValueRW.LastUpdateTick = timeState.Tick;
        }

        [BurstCompile]
        private struct BiomeDerivationJob : IJobFor
        {
            public NativeArray<BiomeGridRuntimeCell> Biomes;

            [ReadOnly] public NativeArray<MoistureGridRuntimeCell> MoistureRuntime;
            [ReadOnly] public BlobAssetReference<MoistureGridBlob> MoistureBlob;
            public bool HasMoistureRuntime;

            [ReadOnly] public BlobAssetReference<TemperatureGridBlob> TemperatureBlob;
            [ReadOnly] public BlobAssetReference<SunlightGridBlob> SunlightBlob;
            [ReadOnly] public BlobAssetReference<ChemicalFieldBlob> ChemicalBlob;

            [ReadOnly] public NativeArray<ClimateGridRuntimeCell> ClimateRuntime;
            public bool HasClimateRuntime;

            [ReadOnly] public BlobAssetReference<BiomeLUTBlob> BiomeLUT;
            public bool HasLUT;

            public void Execute(int index)
            {
                BiomeType biome;
                
                if (HasClimateRuntime && ClimateRuntime.IsCreated && index < ClimateRuntime.Length)
                {
                    // Use climate vector for classification
                    var climate = ClimateRuntime[index].Climate;
                    biome = ClassifyBiomeFromClimate(climate);
                }
                else
                {
                    // Sample input fields
                    var moisture = SampleMoisture(index);
                    var temperature = SampleTemperature(index);
                    var light = SampleLight(index);
                    var chemical = SampleChemical(index);

                    // Use LUT if available
                    if (HasLUT && BiomeLUT.IsCreated)
                    {
                        ref var lut = ref BiomeLUT.Value;
                        if (chemical > 0f && lut.TempMoistureLightChemicalMatrix.IsCreated)
                        {
                            biome = lut.EvaluateBiomeWithChemical(temperature, moisture, light, chemical);
                        }
                        else
                        {
                            biome = lut.EvaluateBiome(temperature, moisture, light);
                        }
                    }
                    else
                    {
                        // Fallback to legacy classification
                        biome = ClassifyBiome(temperature, moisture);
                    }
                }

                Biomes[index] = new BiomeGridRuntimeCell
                {
                    Value = biome
                };
            }

            private float SampleLight(int index)
            {
                if (SunlightBlob.IsCreated)
                {
                    ref var samples = ref SunlightBlob.Value.Samples;
                    if (index >= 0 && index < samples.Length)
                    {
                        return samples[index].DirectLight + samples[index].AmbientLight;
                    }
                }
                return 50f; // Default light level
            }

            private float SampleChemical(int index)
            {
                if (ChemicalBlob.IsCreated)
                {
                    ref var samples = ref ChemicalBlob.Value.Samples;
                    if (index >= 0 && index < samples.Length)
                    {
                        // Return combined chemical factor (pollutants primarily)
                        return samples[index].Pollutants;
                    }
                }
                return 0f;
            }

            private float SampleMoisture(int index)
            {
                if (HasMoistureRuntime && MoistureRuntime.IsCreated && index < MoistureRuntime.Length)
                {
                    return MoistureRuntime[index].Moisture;
                }

                if (MoistureBlob.IsCreated)
                {
                    ref var moisture = ref MoistureBlob.Value.Moisture;
                    if (index >= 0 && index < moisture.Length)
                    {
                        return moisture[index];
                    }
                }

                return 0f;
            }

            private float SampleTemperature(int index)
            {
                if (TemperatureBlob.IsCreated)
                {
                    ref var temperatures = ref TemperatureBlob.Value.TemperatureCelsius;
                    if (index >= 0 && index < temperatures.Length)
                    {
                        return temperatures[index];
                    }
                }

                return 0f;
            }

            private static BiomeType ClassifyBiome(float temperature, float moisture)
            {
                if (temperature <= -10f)
                {
                    return BiomeType.Tundra;
                }

                if (temperature <= 2f)
                {
                    return moisture >= 55f ? BiomeType.Swamp : BiomeType.Taiga;
                }

                if (temperature <= 18f)
                {
                    if (moisture >= 70f)
                    {
                        return BiomeType.Forest;
                    }

                    if (moisture >= 45f)
                    {
                        return BiomeType.Grassland;
                    }

                    return BiomeType.Savanna;
                }

                if (temperature <= 30f)
                {
                    if (moisture >= 75f)
                    {
                        return BiomeType.Rainforest;
                    }

                    if (moisture >= 35f)
                    {
                        return BiomeType.Grassland;
                    }

                    return BiomeType.Savanna;
                }

                return moisture >= 30f ? BiomeType.Savanna : BiomeType.Desert;
            }

            private static BiomeType ClassifyBiomeFromClimate(in ClimateVector climate)
            {
                // Convert normalized temperature back to Celsius for compatibility
                var temperature = 20f + climate.Temperature * 20f;
                var moisture = climate.Moisture * 100f;

                // Water level overrides: ocean/swamp
                if (climate.WaterLevel > 0.7f)
                {
                    return BiomeType.Swamp;
                }

                // Use temperature/moisture classification
                return ClassifyBiome(temperature, moisture);
            }
        }

        /// <summary>
        /// Job for processing biome derivation on a per-chunk basis (incremental updates).
        /// </summary>
        [BurstCompile]
        private struct BiomeDerivationChunkJob : IJobFor
        {
            public NativeArray<BiomeGridRuntimeCell> Biomes;
            public BiomeChunkMetadata ChunkMetadata;
            [ReadOnly] public NativeArray<BiomeChunkDirtyFlag> DirtyFlags;

            [ReadOnly] public NativeArray<MoistureGridRuntimeCell> MoistureRuntime;
            [ReadOnly] public BlobAssetReference<MoistureGridBlob> MoistureBlob;
            public bool HasMoistureRuntime;

            [ReadOnly] public BlobAssetReference<TemperatureGridBlob> TemperatureBlob;
            [ReadOnly] public BlobAssetReference<SunlightGridBlob> SunlightBlob;
            [ReadOnly] public BlobAssetReference<ChemicalFieldBlob> ChemicalBlob;

            [ReadOnly] public NativeArray<ClimateGridRuntimeCell> ClimateRuntime;
            public bool HasClimateRuntime;

            [ReadOnly] public BlobAssetReference<BiomeLUTBlob> BiomeLUT;
            public bool HasLUT;

            public void Execute(int chunkIndex)
            {
                // Skip clean chunks
                if (chunkIndex >= DirtyFlags.Length || DirtyFlags[chunkIndex].Value == 0)
                {
                    return;
                }

                ChunkMetadata.GetChunkCellRange(chunkIndex, out var minCell, out var maxCell);

                // Process all cells in this chunk
                for (int y = minCell.y; y < maxCell.y; y++)
                {
                    for (int x = minCell.x; x < maxCell.x; x++)
                    {
                        var cellIndex = EnvironmentGridMath.GetCellIndex(ChunkMetadata.GridMetadata, new int2(x, y));
                        if (cellIndex < 0 || cellIndex >= Biomes.Length)
                        {
                            continue;
                        }

                        BiomeType biome;

                        if (HasClimateRuntime && ClimateRuntime.IsCreated && cellIndex < ClimateRuntime.Length)
                        {
                            var climate = ClimateRuntime[cellIndex].Climate;
                            biome = ClassifyBiomeFromClimate(climate);
                        }
                        else
                        {
                            var moisture = SampleMoisture(cellIndex);
                            var temperature = SampleTemperature(cellIndex);
                            var light = SampleLight(cellIndex);
                            var chemical = SampleChemical(cellIndex);

                            if (HasLUT && BiomeLUT.IsCreated)
                            {
                                ref var lut = ref BiomeLUT.Value;
                                if (chemical > 0f && lut.TempMoistureLightChemicalMatrix.IsCreated)
                                {
                                    biome = lut.EvaluateBiomeWithChemical(temperature, moisture, light, chemical);
                                }
                                else
                                {
                                    biome = lut.EvaluateBiome(temperature, moisture, light);
                                }
                            }
                            else
                            {
                                biome = ClassifyBiome(temperature, moisture);
                            }
                        }

                        Biomes[cellIndex] = new BiomeGridRuntimeCell { Value = biome };
                    }
                }
            }

            private float SampleMoisture(int index)
            {
                if (HasMoistureRuntime && MoistureRuntime.IsCreated && index < MoistureRuntime.Length)
                {
                    return MoistureRuntime[index].Moisture;
                }
                if (MoistureBlob.IsCreated)
                {
                    ref var moisture = ref MoistureBlob.Value.Moisture;
                    if (index >= 0 && index < moisture.Length)
                    {
                        return moisture[index];
                    }
                }
                return 0f;
            }

            private float SampleTemperature(int index)
            {
                if (TemperatureBlob.IsCreated)
                {
                    ref var temperatures = ref TemperatureBlob.Value.TemperatureCelsius;
                    if (index >= 0 && index < temperatures.Length)
                    {
                        return temperatures[index];
                    }
                }
                return 0f;
            }

            private float SampleLight(int index)
            {
                if (SunlightBlob.IsCreated)
                {
                    ref var samples = ref SunlightBlob.Value.Samples;
                    if (index >= 0 && index < samples.Length)
                    {
                        return samples[index].DirectLight + samples[index].AmbientLight;
                    }
                }
                return 50f;
            }

            private float SampleChemical(int index)
            {
                if (ChemicalBlob.IsCreated)
                {
                    ref var samples = ref ChemicalBlob.Value.Samples;
                    if (index >= 0 && index < samples.Length)
                    {
                        return samples[index].Pollutants;
                    }
                }
                return 0f;
            }

            private static BiomeType ClassifyBiome(float temperature, float moisture)
            {
                if (temperature <= -10f)
                {
                    return BiomeType.Tundra;
                }
                if (temperature <= 2f)
                {
                    return moisture >= 55f ? BiomeType.Swamp : BiomeType.Taiga;
                }
                if (temperature <= 18f)
                {
                    if (moisture >= 70f)
                    {
                        return BiomeType.Forest;
                    }
                    if (moisture >= 45f)
                    {
                        return BiomeType.Grassland;
                    }
                    return BiomeType.Savanna;
                }
                if (temperature <= 30f)
                {
                    if (moisture >= 75f)
                    {
                        return BiomeType.Rainforest;
                    }
                    if (moisture >= 35f)
                    {
                        return BiomeType.Grassland;
                    }
                    return BiomeType.Savanna;
                }
                return moisture >= 30f ? BiomeType.Savanna : BiomeType.Desert;
            }

            private static BiomeType ClassifyBiomeFromClimate(in ClimateVector climate)
            {
                var temperature = 20f + climate.Temperature * 20f;
                var moisture = climate.Moisture * 100f;
                if (climate.WaterLevel > 0.7f)
                {
                    return BiomeType.Swamp;
                }
                return ClassifyBiome(temperature, moisture);
            }
        }
    }
}

