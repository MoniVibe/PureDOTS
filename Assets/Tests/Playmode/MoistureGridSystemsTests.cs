using NUnit.Framework;
using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems.Environment;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests
{
    public class MoistureGridSystemsTests
    {
        [Test]
        public void MoistureEvaporationSystem_ReducesMoistureOverTime()
        {
            using var world = new World("MoistureEvapTest");
            var entityManager = world.EntityManager;

            var configEntity = entityManager.CreateEntity(typeof(EnvironmentGridConfigData));
            entityManager.SetComponentData(configEntity, CreateTestConfig());

            var timeEntity = entityManager.CreateEntity(typeof(TimeState));
            entityManager.SetComponentData(timeEntity, new TimeState
            {
                FixedDeltaTime = 1f / 30f,
                CurrentSpeedMultiplier = 1f,
                Tick = 10u,
                IsPaused = false
            });

            var rewindEntity = entityManager.CreateEntity(typeof(RewindState));
            entityManager.SetComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Record,
                TargetTick = 0,
                TickDuration = 1f / 30f,
                MaxHistoryTicks = 600,
                PendingStepTicks = 0
            });
            entityManager.AddComponentData(rewindEntity, new RewindLegacyState
            {
                PlaybackSpeed = 1f,
                CurrentTick = 0,
                StartTick = 0,
                PlaybackTick = 0,
                PlaybackTicksPerSecond = 1f,
                ScrubDirection = ScrubDirection.Forward,
                ScrubSpeedMultiplier = 1f,
                RewindWindowTicks = 0,
                ActiveTrack = default
            });

            world.UpdateSystem<EnvironmentGridBootstrapSystem>();
            world.UpdateSystem<ClimateStateUpdateSystem>();

            var moistureBuffer = entityManager.GetBuffer<MoistureGridRuntimeCell>(configEntity);
            for (var i = 0; i < moistureBuffer.Length; i++)
            {
                var cell = moistureBuffer[i];
                cell.Moisture = 90f;
                cell.EvaporationRate = 0f;
                moistureBuffer[i] = cell;
            }

            world.UpdateSystem<MoistureEvaporationSystem>();

            moistureBuffer = entityManager.GetBuffer<MoistureGridRuntimeCell>(configEntity);
            Assert.Less(moistureBuffer[0].Moisture, 90f);
            Assert.Greater(moistureBuffer[0].EvaporationRate, 0f);
        }

        [Test]
        public void MoistureSeepageSystem_DiffusesBetweenNeighbours()
        {
            using var world = new World("MoistureSeepTest");
            var entityManager = world.EntityManager;

            var configEntity = entityManager.CreateEntity(typeof(EnvironmentGridConfigData));
            var config = CreateTestConfig();
            entityManager.SetComponentData(configEntity, config);

            var timeEntity = entityManager.CreateEntity(typeof(TimeState));
            entityManager.SetComponentData(timeEntity, new TimeState
            {
                FixedDeltaTime = 1f / 30f,
                CurrentSpeedMultiplier = 1f,
                Tick = 20u,
                IsPaused = false
            });

            var rewindEntity = entityManager.CreateEntity(typeof(RewindState));
            entityManager.SetComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Record,
                TargetTick = 0,
                TickDuration = 1f / 30f,
                MaxHistoryTicks = 600,
                PendingStepTicks = 0
            });
            entityManager.AddComponentData(rewindEntity, new RewindLegacyState
            {
                PlaybackSpeed = 1f,
                CurrentTick = 0,
                StartTick = 0,
                PlaybackTick = 0,
                PlaybackTicksPerSecond = 1f,
                ScrubDirection = ScrubDirection.Forward,
                ScrubSpeedMultiplier = 1f,
                RewindWindowTicks = 0,
                ActiveTrack = default
            });

            world.UpdateSystem<EnvironmentGridBootstrapSystem>();

            var moistureBuffer = entityManager.GetBuffer<MoistureGridRuntimeCell>(configEntity);
            Assert.GreaterOrEqual(moistureBuffer.Length, 2);

            var first = moistureBuffer[0];
            first.Moisture = 0f;
            moistureBuffer[0] = first;

            var second = moistureBuffer[1];
            second.Moisture = 100f;
            moistureBuffer[1] = second;

            world.UpdateSystem<MoistureSeepageSystem>();

            moistureBuffer = entityManager.GetBuffer<MoistureGridRuntimeCell>(configEntity);
            Assert.Greater(moistureBuffer[0].Moisture, 0f);
            Assert.Less(moistureBuffer[1].Moisture, 100f);
        }

        static EnvironmentGridConfigData CreateTestConfig()
        {
            var metadata = EnvironmentGridMetadata.Create(new float3(0f, 0f, 0f), new float3(4f, 0f, 4f), 1f, new int2(4, 4));

            return new EnvironmentGridConfigData
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
                MoistureDiffusion = 0.25f,
                MoistureSeepage = 0.05f,
                BaseSeasonTemperature = 18f,
                TimeOfDaySwing = 4f,
                SeasonalSwing = 8f,
                DefaultSunDirection = new float3(0f, -1f, 0f),
                DefaultSunIntensity = 1f,
                DefaultWindDirection = new float2(0.6f, 0.2f),
                DefaultWindStrength = 6f
            };
        }
    }
}
