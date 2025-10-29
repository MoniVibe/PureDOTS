using PureDOTS.Environment;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Builds the runtime environment singleton using the authored configuration.
    /// Creates grid blob assets with deterministic defaults so simulation systems
    /// can update them during the environment phase each record tick.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(TimeSystemGroup))]
    [UpdateAfter(typeof(CoreSingletonBootstrapSystem))]
    public partial struct EnvironmentGridBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnvironmentGridConfigData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var configEntity = SystemAPI.GetSingletonEntity<EnvironmentGridConfigData>();
            var config = SystemAPI.GetSingleton<EnvironmentGridConfigData>();

            var entityManager = state.EntityManager;
            if (!entityManager.HasComponent<ClimateState>(configEntity))
            {
                entityManager.AddComponentData(configEntity, CreateDefaultClimateState());
            }

            if (!entityManager.HasComponent<MoistureGrid>(configEntity))
            {
                entityManager.AddComponentData(configEntity, CreateMoistureGrid(config));
            }

            EnsureMoistureRuntimeBuffers(ref state, configEntity);

            if (!entityManager.HasComponent<TemperatureGrid>(configEntity))
            {
                entityManager.AddComponentData(configEntity, CreateTemperatureGrid(config));
            }

            if (!entityManager.HasComponent<SunlightGrid>(configEntity))
            {
                entityManager.AddComponentData(configEntity, CreateSunlightGrid(config));
            }

            EnsureSunlightRuntimeBuffer(ref state, configEntity);

            if (!entityManager.HasComponent<WindField>(configEntity))
            {
                entityManager.AddComponentData(configEntity, CreateWindField(config));
            }

            if (config.BiomeEnabled != 0 && !entityManager.HasComponent<BiomeGrid>(configEntity))
            {
                entityManager.AddComponentData(configEntity, CreateBiomeGrid(config));
            }

            if (!entityManager.HasComponent<MoistureGridSimulationState>(configEntity))
            {
                entityManager.AddComponentData(configEntity, new MoistureGridSimulationState
                {
                    LastEvaporationTick = uint.MaxValue,
                    LastSeepageTick = uint.MaxValue
                });
            }

            state.Enabled = false;
        }

        private static ClimateState CreateDefaultClimateState()
        {
            return new ClimateState
            {
                CurrentSeason = Season.Spring,
                SeasonProgress = 0f,
                TimeOfDayHours = 6f,
                DayNightProgress = 6f / 24f,
                GlobalTemperature = 18f,
                GlobalWindDirection = math.normalize(new float2(0.7f, 0.5f)),
                GlobalWindStrength = 8f,
                AtmosphericMoisture = 55f,
                CloudCover = 20f,
                LastUpdateTick = uint.MaxValue
            };
        }

        private static MoistureGrid CreateMoistureGrid(in EnvironmentGridConfigData config)
        {
            var blob = CreateMoistureBlob(config.Moisture);
            return new MoistureGrid
            {
                Metadata = config.Moisture,
                Blob = blob,
                ChannelId = config.MoistureChannelId,
                DiffusionCoefficient = math.max(0f, config.MoistureDiffusion),
                SeepageCoefficient = math.max(0f, config.MoistureSeepage),
                LastUpdateTick = uint.MaxValue,
                LastTerrainVersion = 0u
            };
        }

        private static TemperatureGrid CreateTemperatureGrid(in EnvironmentGridConfigData config)
        {
            var blob = CreateTemperatureBlob(config.Temperature, config.BaseSeasonTemperature);
            return new TemperatureGrid
            {
                Metadata = config.Temperature,
                Blob = blob,
                ChannelId = config.TemperatureChannelId,
                BaseSeasonTemperature = config.BaseSeasonTemperature,
                TimeOfDaySwing = config.TimeOfDaySwing,
                SeasonalSwing = config.SeasonalSwing,
                LastUpdateTick = uint.MaxValue,
                LastTerrainVersion = 0u
            };
        }

        private static SunlightGrid CreateSunlightGrid(in EnvironmentGridConfigData config)
        {
            var blob = CreateSunlightBlob(config.Sunlight, config.DefaultSunIntensity);
            return new SunlightGrid
            {
                Metadata = config.Sunlight,
                Blob = blob,
                ChannelId = config.SunlightChannelId,
                SunDirection = math.lengthsq(config.DefaultSunDirection) > 0f
                    ? math.normalize(config.DefaultSunDirection)
                    : new float3(0f, -1f, 0f),
                SunIntensity = config.DefaultSunIntensity,
                LastUpdateTick = uint.MaxValue,
                LastTerrainVersion = 0u
            };
        }

        private static WindField CreateWindField(in EnvironmentGridConfigData config)
        {
            var blob = CreateWindBlob(config.Wind, config.DefaultWindDirection, config.DefaultWindStrength);
            return new WindField
            {
                Metadata = config.Wind,
                Blob = blob,
                ChannelId = config.WindChannelId,
                GlobalWindDirection = math.lengthsq(config.DefaultWindDirection) > 0f
                    ? math.normalize(config.DefaultWindDirection)
                    : new float2(0f, 1f),
                GlobalWindStrength = config.DefaultWindStrength,
                LastUpdateTick = uint.MaxValue,
                LastTerrainVersion = 0u
            };
        }

        private static BiomeGrid CreateBiomeGrid(in EnvironmentGridConfigData config)
        {
            var blob = CreateBiomeBlob(config.Biome);
            return new BiomeGrid
            {
                Metadata = config.Biome,
                Blob = blob,
                ChannelId = config.BiomeChannelId,
                LastUpdateTick = uint.MaxValue,
                LastTerrainVersion = 0u
            };
        }

        private static void EnsureMoistureRuntimeBuffers(ref SystemState state, Entity configEntity)
        {
            var entityManager = state.EntityManager;
            if (!entityManager.HasComponent<MoistureGrid>(configEntity))
            {
                return;
            }

            var moistureGrid = entityManager.GetComponentData<MoistureGrid>(configEntity);
            var cellCount = math.max(1, moistureGrid.Metadata.CellCount);
            if (!entityManager.HasBuffer<MoistureGridRuntimeCell>(configEntity))
            {
                InitialiseMoistureRuntimeBuffer(entityManager, configEntity, in moistureGrid);
            }
            else
            {
                var buffer = entityManager.GetBuffer<MoistureGridRuntimeCell>(configEntity);
                if (buffer.Length != cellCount)
                {
                    buffer.Clear();
                    buffer.ResizeUninitialized(cellCount);
                    PopulateMoistureRuntimeBuffer(buffer, in moistureGrid);
                }
            }
        }

        private static void EnsureSunlightRuntimeBuffer(ref SystemState state, Entity configEntity)
        {
            var entityManager = state.EntityManager;
            if (!entityManager.HasComponent<SunlightGrid>(configEntity))
            {
                return;
            }

            var sunlightGrid = entityManager.GetComponentData<SunlightGrid>(configEntity);
            var cellCount = math.max(1, sunlightGrid.Metadata.CellCount);

            DynamicBuffer<SunlightGridRuntimeSample> buffer;
            if (!entityManager.HasBuffer<SunlightGridRuntimeSample>(configEntity))
            {
                buffer = entityManager.AddBuffer<SunlightGridRuntimeSample>(configEntity);
                buffer.ResizeUninitialized(cellCount);
            }
            else
            {
                buffer = entityManager.GetBuffer<SunlightGridRuntimeSample>(configEntity);
                if (buffer.Length != cellCount)
                {
                    buffer.Clear();
                    buffer.ResizeUninitialized(cellCount);
                }
            }

            PopulateSunlightRuntimeBuffer(buffer, in sunlightGrid);
        }

        private static void InitialiseMoistureRuntimeBuffer(EntityManager entityManager, Entity entity, in MoistureGrid grid)
        {
            if (!grid.IsCreated)
            {
                return;
            }

            var cellCount = math.max(1, grid.Metadata.CellCount);
            var buffer = entityManager.AddBuffer<MoistureGridRuntimeCell>(entity);
            buffer.ResizeUninitialized(cellCount);
            PopulateMoistureRuntimeBuffer(buffer, in grid);
        }

        private static void PopulateMoistureRuntimeBuffer(DynamicBuffer<MoistureGridRuntimeCell> buffer, in MoistureGrid grid)
        {
            if (!grid.IsCreated)
            {
                return;
            }

            ref var moist = ref grid.Blob.Value.Moisture;
            ref var evaporation = ref grid.Blob.Value.EvaporationRate;
            ref var lastRain = ref grid.Blob.Value.LastRainTick;

            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = new MoistureGridRuntimeCell
                {
                    Moisture = i < moist.Length ? moist[i] : 0f,
                    EvaporationRate = i < evaporation.Length ? evaporation[i] : 0f,
                    LastRainTick = i < lastRain.Length ? lastRain[i] : 0u
                };
            }
        }

        private static void PopulateSunlightRuntimeBuffer(DynamicBuffer<SunlightGridRuntimeSample> buffer, in SunlightGrid grid)
        {
            if (!grid.IsCreated)
            {
                for (var i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = new SunlightGridRuntimeSample { Value = default };
                }
                return;
            }

            ref var samples = ref grid.Blob.Value.Samples;
            for (var i = 0; i < buffer.Length; i++)
            {
                var value = i < samples.Length ? samples[i] : default;
                buffer[i] = new SunlightGridRuntimeSample { Value = value };
            }
        }

        private static BlobAssetReference<MoistureGridBlob> CreateMoistureBlob(in EnvironmentGridMetadata metadata)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<MoistureGridBlob>();

            var cellCount = math.max(1, metadata.CellCount);

            var moisture = builder.Allocate(ref root.Moisture, cellCount);
            var drainage = builder.Allocate(ref root.DrainageRate, cellCount);
            var terrain = builder.Allocate(ref root.TerrainHeight, cellCount);
            var lastRain = builder.Allocate(ref root.LastRainTick, cellCount);
            var evaporation = builder.Allocate(ref root.EvaporationRate, cellCount);

            for (var i = 0; i < cellCount; i++)
            {
                moisture[i] = 50f;
                drainage[i] = 0.05f;
                terrain[i] = 0f;
                lastRain[i] = 0u;
                evaporation[i] = 1f;
            }

            var blob = builder.CreateBlobAssetReference<MoistureGridBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private static BlobAssetReference<TemperatureGridBlob> CreateTemperatureBlob(in EnvironmentGridMetadata metadata, float baseTemperature)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<TemperatureGridBlob>();

            var cellCount = math.max(1, metadata.CellCount);
            var temperature = builder.Allocate(ref root.TemperatureCelsius, cellCount);
            var altitude = builder.Allocate(ref root.AltitudeMeters, cellCount);

            for (var i = 0; i < cellCount; i++)
            {
                temperature[i] = baseTemperature;
                altitude[i] = 0f;
            }

            var blob = builder.CreateBlobAssetReference<TemperatureGridBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private static BlobAssetReference<SunlightGridBlob> CreateSunlightBlob(in EnvironmentGridMetadata metadata, float defaultIntensity)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<SunlightGridBlob>();

            var cellCount = math.max(1, metadata.CellCount);
            var samples = builder.Allocate(ref root.Samples, cellCount);

            for (var i = 0; i < cellCount; i++)
            {
                samples[i] = new SunlightSample
                {
                    DirectLight = defaultIntensity,
                    AmbientLight = defaultIntensity * 0.25f,
                    OccluderCount = 0
                };
            }

            var blob = builder.CreateBlobAssetReference<SunlightGridBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private static BlobAssetReference<WindFieldBlob> CreateWindBlob(in EnvironmentGridMetadata metadata, float2 defaultDirection, float defaultStrength)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<WindFieldBlob>();

            var cellCount = math.max(1, metadata.CellCount);
            var samples = builder.Allocate(ref root.Samples, cellCount);
            var direction = math.lengthsq(defaultDirection) > 0f
                ? math.normalize(defaultDirection)
                : new float2(0f, 1f);

            for (var i = 0; i < cellCount; i++)
            {
                samples[i] = new WindSample
                {
                    Direction = direction,
                    Strength = defaultStrength
                };
            }

            var blob = builder.CreateBlobAssetReference<WindFieldBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private static BlobAssetReference<BiomeGridBlob> CreateBiomeBlob(in EnvironmentGridMetadata metadata)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<BiomeGridBlob>();

            var cellCount = math.max(1, metadata.CellCount);
            var biomes = builder.Allocate(ref root.Biomes, cellCount);

            for (var i = 0; i < cellCount; i++)
            {
                biomes[i] = BiomeType.Unknown;
            }

            var blob = builder.CreateBlobAssetReference<BiomeGridBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }
    }
}
