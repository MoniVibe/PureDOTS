using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Derives biome classifications per cell using current moisture and temperature fields.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    [UpdateAfter(typeof(MoistureSeepageSystem))]
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
            if (SystemAPI.TryGetSingleton<PureDOTS.Environment.TerrainVersion>(out var terrainVersion))
            {
                currentTerrainVersion = terrainVersion.Value;
                if (currentTerrainVersion != biomeGrid.ValueRO.LastTerrainVersion)
                {
                    // Terrain changed, force biome update
                    biomeGrid.ValueRW.LastTerrainVersion = currentTerrainVersion;
                    biomeGrid.ValueRW.LastUpdateTick = uint.MaxValue; // Force rebuild
                }
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

            var job = new BiomeDerivationJob
            {
                Biomes = biomeBuffer.AsNativeArray(),
                MoistureRuntime = moistureRuntime,
                MoistureBlob = moistureGrid.Blob,
                HasMoistureRuntime = hasMoistureRuntime,
                TemperatureBlob = temperatureGrid.Blob
            };

            state.Dependency = job.ScheduleParallel(biomeBuffer.Length, 64, state.Dependency);
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

            public void Execute(int index)
            {
                var moisture = SampleMoisture(index);
                var temperature = SampleTemperature(index);
                Biomes[index] = new BiomeGridRuntimeCell
                {
                    Value = ClassifyBiome(temperature, moisture)
                };
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
        }
    }
}

