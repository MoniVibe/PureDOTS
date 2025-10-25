using NUnit.Framework;
using PureDOTS.Environment;
using PureDOTS.Systems.Environment;
using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests
{
    public class EnvironmentGridTests
    {
        [Test]
        public void MoistureGrid_SampleBilinearInterpolatesCorrectly()
        {
            var metadata = EnvironmentGridMetadata.Create(new float3(0f, 0f, 0f), new float3(2f, 0f, 2f), 1f, new int2(2, 2));

            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<MoistureGridBlob>();
            var values = builder.Allocate(ref root.Moisture, metadata.CellCount);
            var drainage = builder.Allocate(ref root.DrainageRate, metadata.CellCount);
            var terrain = builder.Allocate(ref root.TerrainHeight, metadata.CellCount);
            var lastRain = builder.Allocate(ref root.LastRainTick, metadata.CellCount);
            var evaporation = builder.Allocate(ref root.EvaporationRate, metadata.CellCount);

            values[0] = 10f;
            values[1] = 20f;
            values[2] = 30f;
            values[3] = 40f;

            for (var i = 0; i < metadata.CellCount; i++)
            {
                drainage[i] = 0f;
                terrain[i] = 0f;
                lastRain[i] = 0u;
                evaporation[i] = 1f;
            }

            var blob = builder.CreateBlobAssetReference<MoistureGridBlob>(Allocator.Temp);

            var grid = new MoistureGrid
            {
                Metadata = metadata,
                Blob = blob
            };

            var sample = grid.SampleBilinear(new float3(0.5f, 0f, 0.5f), 0f);
            Assert.AreEqual(25f, sample, 1e-3f);

            blob.Dispose();
        }

        [Test]
        public void EnvironmentUpdateUtility_ShouldUpdateHonoursStrideAndWrapAround()
        {
            const uint stride = 5u;

            Assert.IsTrue(EnvironmentEffectUtility.ShouldUpdate(5u, uint.MaxValue, stride), "First update should always run");
            Assert.IsFalse(EnvironmentEffectUtility.ShouldUpdate(5u, 5u, stride), "Same tick should not update");

            var shouldUpdate = EnvironmentEffectUtility.ShouldUpdate(10u, 5u, stride);
            Assert.IsTrue(shouldUpdate, "Should update when stride reached");

            var wrapped = EnvironmentEffectUtility.ShouldUpdate(2u, uint.MaxValue - 1u, stride: 1u);
            Assert.IsTrue(wrapped, "Tick wrap-around should be handled");

            var delta = EnvironmentEffectUtility.TickDelta(3u, uint.MaxValue - 1u);
            Assert.AreEqual(5u, delta);
        }

        [Test]
        public void EnvironmentEffectsPipeline_AppliesScalarEffectDeterministically()
        {
            using var world = new World("EnvironmentEffectTest");
            var entityManager = world.EntityManager;

            var configEntity = entityManager.CreateEntity(typeof(EnvironmentGridConfigData));
            var metadata = EnvironmentGridMetadata.Create(new float3(0f, 0f, 0f), new float3(4f, 0f, 4f), 1f, new int2(4, 4));
            entityManager.SetComponentData(configEntity, new EnvironmentGridConfigData
            {
                Moisture = metadata,
                Temperature = metadata,
                Sunlight = metadata,
                Wind = metadata,
                Biome = metadata,
                BiomeEnabled = 0,
                MoistureChannelId = (FixedString64Bytes)"moisture",
                TemperatureChannelId = (FixedString64Bytes)"temperature",
                SunlightChannelId = (FixedString64Bytes)"sunlight",
                WindChannelId = (FixedString64Bytes)"wind",
                BiomeChannelId = (FixedString64Bytes)"biome",
                MoistureDiffusion = 0f,
                MoistureSeepage = 0f,
                BaseSeasonTemperature = 18f,
                TimeOfDaySwing = 0f,
                SeasonalSwing = 0f,
                DefaultSunDirection = new float3(0f, -1f, 0f),
                DefaultSunIntensity = 1f,
                DefaultWindDirection = new float2(0f, 1f),
                DefaultWindStrength = 1f
            });

            var scalarParameters = new EnvironmentScalarEffectParameters
            {
                BaseOffset = 1f,
                Amplitude = 0f,
                Frequency = 0f,
                NoiseOffset = 0f,
                Damping = 0f
            };

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<EnvironmentEffectCatalogBlob>();
            var effects = builder.Allocate(ref root.Effects, 1);
            var scalarArray = builder.Allocate(ref root.ScalarParameters, 1);
            builder.Allocate(ref root.VectorParameters, 0);
            builder.Allocate(ref root.PulseParameters, 0);

            effects[0] = new EnvironmentEffectDefinition
            {
                EffectId = (FixedString64Bytes)"moisture_constant",
                ChannelId = (FixedString64Bytes)"moisture",
                Type = EnvironmentEffectType.ScalarField,
                UpdateStride = 1,
                ParameterIndex = 0
            };

            scalarArray[0] = scalarParameters;

            var catalogBlob = builder.CreateBlobAssetReference<EnvironmentEffectCatalogBlob>(Allocator.Temp);
            builder.Dispose();

            var catalogEntity = entityManager.CreateEntity(typeof(EnvironmentEffectCatalogData));
            entityManager.SetComponentData(catalogEntity, new EnvironmentEffectCatalogData { Catalog = catalogBlob });

            var timeEntity = entityManager.CreateEntity(typeof(TimeState));
            entityManager.SetComponentData(timeEntity, new TimeState
            {
                FixedDeltaTime = 0.1f,
                Tick = 1u,
                CurrentSpeedMultiplier = 1f,
                IsPaused = false
            });

            world.UpdateSystem<EnvironmentGridBootstrapSystem>();
            world.UpdateSystem<EnvironmentEffectBootstrapSystem>();
            world.UpdateSystem<EnvironmentEffectUpdateSystem>();

            var contributions = entityManager.GetBuffer<EnvironmentScalarContribution>(catalogEntity);
            for (var i = 0; i < contributions.Length; i++)
            {
                Assert.AreEqual(1f, contributions[i].Value, 1e-5f);
            }

            var runtimes = entityManager.GetBuffer<EnvironmentEffectRuntime>(catalogEntity);
            Assert.AreEqual(1u, runtimes[0].LastUpdateTick);

            // Advance tick and update again to ensure deterministic cadence.
            var timeState = entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick = 2u;
            entityManager.SetComponentData(timeEntity, timeState);

            world.UpdateSystem<EnvironmentEffectUpdateSystem>();

            contributions = entityManager.GetBuffer<EnvironmentScalarContribution>(catalogEntity);
            for (var i = 0; i < contributions.Length; i++)
            {
                Assert.AreEqual(1f, contributions[i].Value, 1e-5f);
            }

            runtimes = entityManager.GetBuffer<EnvironmentEffectRuntime>(catalogEntity);
            Assert.AreEqual(2u, runtimes[0].LastUpdateTick);

            entityManager.SetComponentData(catalogEntity, new EnvironmentEffectCatalogData { Catalog = BlobAssetReference<EnvironmentEffectCatalogBlob>.Null });
            catalogBlob.Dispose();
        }
    }
}
