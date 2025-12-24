using NUnit.Framework;
using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems.Environment;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests
{
    public class ClimateStateUpdateSystemTests
    {
        [Test]
        public void ClimateStateUpdate_UsesFallbackProfileWhenNoneProvided()
        {
            using var world = new World("ClimateFallbackTest");
            var entityManager = world.EntityManager;

            var configEntity = entityManager.CreateEntity(typeof(EnvironmentGridConfigData));
            var config = CreateTestConfig();
            entityManager.SetComponentData(configEntity, config);

            var timeEntity = entityManager.CreateEntity(typeof(TimeState));
            entityManager.SetComponentData(timeEntity, new TimeState
            {
                FixedDeltaTime = 1f / 60f,
                CurrentSpeedMultiplier = 1f,
                Tick = 1u,
                IsPaused = false
            });

            var rewindEntity = entityManager.CreateEntity(typeof(RewindState));
            entityManager.SetComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Record,
                TargetTick = 0,
                TickDuration = 1f / 60f,
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

            var climateEntity = entityManager.CreateEntityQuery(typeof(ClimateState)).GetSingletonEntity();
            var climate = entityManager.GetComponentData<ClimateState>(climateEntity);

            Assert.AreEqual(1u, climate.LastUpdateTick);

            var fallbackProfile = ClimateProfileDefaults.Create(in config);
            var deltaSeconds = (1f / 60f) * 1f;
            var expectedHoursDelta = deltaSeconds * fallbackProfile.HoursPerSecond;
            var expectedHours = EnvironmentEffectUtility.WrapHours(6f + expectedHoursDelta);

            Assert.That(climate.TimeOfDayHours, Is.EqualTo(expectedHours).Within(1e-4f));
            Assert.That(climate.DayNightProgress, Is.EqualTo(expectedHours / 24f).Within(1e-5f));

            var expectedSeasonProgress = math.clamp((expectedHoursDelta / 24f) / fallbackProfile.DaysPerSeason, 0f, 0.999f);
            Assert.That(climate.SeasonProgress, Is.EqualTo(expectedSeasonProgress).Within(1e-6f));
            Assert.AreEqual(Season.Spring, climate.CurrentSeason);

            var expectedTemperature = ComputeExpectedTemperature(climate.SeasonProgress, climate.DayNightProgress, Season.Spring, fallbackProfile);
            Assert.That(climate.GlobalTemperature, Is.EqualTo(expectedTemperature).Within(1e-4f));

            Assert.That(math.lengthsq(climate.GlobalWindDirection), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(climate.GlobalWindStrength, Is.GreaterThan(0f));
            Assert.That(climate.AtmosphericMoisture, Is.InRange(54.9f, 55.1f));
            Assert.That(climate.CloudCover, Is.InRange(24.9f, 25.1f));
        }

        [Test]
        public void ClimateStateUpdate_UsesAuthoredProfileData()
        {
            using var world = new World("ClimateProfileTest");
            var entityManager = world.EntityManager;

            var configEntity = entityManager.CreateEntity(typeof(EnvironmentGridConfigData));
            var config = CreateTestConfig();
            entityManager.SetComponentData(configEntity, config);

            var profile = new ClimateProfileData
            {
                SeasonalTemperatures = new float4(10f, 20f, 30f, 40f),
                DayNightTemperatureSwing = 0f,
                SeasonalTransitionSmoothing = 0f,
                BaseWindDirection = math.normalizesafe(new float2(0.3f, 0.9f), new float2(0f, 1f)),
                BaseWindStrength = 5f,
                WindVariationAmplitude = 0f,
                WindVariationFrequency = 0f,
                AtmosphericMoistureBase = 70f,
                AtmosphericMoistureVariation = 0f,
                CloudCoverBase = 40f,
                CloudCoverVariation = 0f,
                HoursPerSecond = 1f,
                DaysPerSeason = 10f,
                EvaporationBaseRate = 0.25f
            };
            entityManager.AddComponentData(configEntity, profile);

            var timeEntity = entityManager.CreateEntity(typeof(TimeState));
            entityManager.SetComponentData(timeEntity, new TimeState
            {
                FixedDeltaTime = 1f / 30f,
                CurrentSpeedMultiplier = 1f,
                Tick = 2u,
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

            var climateEntity = entityManager.CreateEntityQuery(typeof(ClimateState)).GetSingletonEntity();
            var climate = entityManager.GetComponentData<ClimateState>(climateEntity);

            Assert.AreEqual(2u, climate.LastUpdateTick);

            var deltaSeconds = (1f / 30f) * 2f;
            var expectedHours = EnvironmentEffectUtility.WrapHours(6f + deltaSeconds * profile.HoursPerSecond);
            Assert.That(climate.TimeOfDayHours, Is.EqualTo(expectedHours).Within(1e-4f));

            var expectedSeasonProgress = math.clamp((deltaSeconds * profile.HoursPerSecond / 24f) / profile.DaysPerSeason, 0f, 0.999f);
            Assert.That(climate.SeasonProgress, Is.EqualTo(expectedSeasonProgress).Within(1e-6f));
            Assert.That(climate.GlobalTemperature, Is.EqualTo(10f).Within(1e-4f));

            var expectedDirection = profile.BaseWindDirection;
            Assert.That(math.distance(climate.GlobalWindDirection, expectedDirection), Is.LessThan(1e-4f));
            Assert.That(climate.GlobalWindStrength, Is.EqualTo(profile.BaseWindStrength).Within(1e-4f));

            var lerpFactor = math.saturate(deltaSeconds * 0.5f);
            var expectedMoisture = math.lerp(55f, profile.AtmosphericMoistureBase, lerpFactor);
            Assert.That(climate.AtmosphericMoisture, Is.EqualTo(expectedMoisture).Within(1e-4f));
            Assert.That(climate.CloudCover, Is.EqualTo(profile.CloudCoverBase).Within(1e-4f));
        }

        static EnvironmentGridConfigData CreateTestConfig()
        {
            var metadata = EnvironmentGridMetadata.Create(new float3(0f, 0f, 0f), new float3(8f, 0f, 8f), 1f, new int2(4, 4));

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
                MoistureSeepage = 0.1f,
                BaseSeasonTemperature = 18f,
                TimeOfDaySwing = 6f,
                SeasonalSwing = 12f,
                DefaultSunDirection = new float3(0f, -1f, 0f),
                DefaultSunIntensity = 1f,
                DefaultWindDirection = new float2(0.6f, 0.2f),
                DefaultWindStrength = 7f
            };
        }

        static float ComputeExpectedTemperature(float seasonProgress, float dayNightProgress, Season currentSeason, in ClimateProfileData profile)
        {
            var currentIndex = (int)currentSeason;
            var nextIndex = (currentIndex + 1) % 4;

            var currentBase = profile.SeasonalTemperatures[currentIndex];
            var nextBase = profile.SeasonalTemperatures[nextIndex];

            var smoothing = math.saturate(profile.SeasonalTransitionSmoothing);
            var blendStart = smoothing > 0f ? math.max(0f, 1f - smoothing) : 1f;
            var blendT = 0f;
            if (smoothing > 0f && seasonProgress >= blendStart)
            {
                var range = math.max(1e-3f, smoothing);
                blendT = math.saturate((seasonProgress - blendStart) / range);
            }

            var baseTemperature = math.lerp(currentBase, nextBase, blendT);
            var dayPhase = dayNightProgress - 0.5f;
            var dayNightWave = math.cos(dayPhase * math.PI * 2f);
            return baseTemperature + profile.DayNightTemperatureSwing * dayNightWave;
        }
    }
}
