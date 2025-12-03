using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;

namespace PureDOTS.Tests.EditMode
{
    /// <summary>
    /// Unit tests for timescale resolution logic.
    /// </summary>
    [TestFixture]
    public class TimeScaleResolutionTests
    {
        [Test]
        public void TimeScaleEntry_CreatePause_SetsCorrectValues()
        {
            var entry = TimeScaleEntry.CreatePause(1, TimeScaleSource.Player, 100, 200);
            
            Assert.AreEqual(1u, entry.EntryId);
            Assert.AreEqual(TimeScaleSource.Player, entry.Source);
            Assert.AreEqual(100u, entry.SourceId);
            Assert.AreEqual(200, entry.Priority);
            Assert.IsTrue(entry.IsPause);
            Assert.AreEqual(0f, entry.Scale);
        }

        [Test]
        public void TimeScaleEntry_CreateSpeed_SetsCorrectValues()
        {
            var entry = TimeScaleEntry.CreateSpeed(2, 2.5f, TimeScaleSource.Miracle, 50, 150, 100, 500);
            
            Assert.AreEqual(2u, entry.EntryId);
            Assert.AreEqual(2.5f, entry.Scale);
            Assert.AreEqual(TimeScaleSource.Miracle, entry.Source);
            Assert.AreEqual(50u, entry.SourceId);
            Assert.AreEqual(150, entry.Priority);
            Assert.AreEqual(100u, entry.StartTick);
            Assert.AreEqual(500u, entry.EndTick);
            Assert.IsFalse(entry.IsPause);
        }

        [Test]
        public void TimeScaleConfig_CreateDefault_HasCorrectLimits()
        {
            var config = TimeScaleConfig.CreateDefault();
            
            Assert.AreEqual(TimeControlLimits.DefaultMinSpeed, config.MinScale);
            Assert.AreEqual(TimeControlLimits.DefaultMaxSpeed, config.MaxScale);
            Assert.AreEqual(1.0f, config.DefaultScale);
            Assert.IsFalse(config.AllowStacking);
        }

        [Test]
        public void TimeScalePresets_HaveExpectedValues()
        {
            Assert.AreEqual(0.01f, TimeScalePresets.SuperSlow);
            Assert.AreEqual(0.1f, TimeScalePresets.VerySlow);
            Assert.AreEqual(0.25f, TimeScalePresets.Slow);
            Assert.AreEqual(0.5f, TimeScalePresets.HalfSpeed);
            Assert.AreEqual(1.0f, TimeScalePresets.Normal);
            Assert.AreEqual(2.0f, TimeScalePresets.Fast);
            Assert.AreEqual(4.0f, TimeScalePresets.VeryFast);
            Assert.AreEqual(8.0f, TimeScalePresets.SuperFast);
            Assert.AreEqual(16.0f, TimeScalePresets.Maximum);
        }

        [Test]
        public void TimeControlLimits_HasCorrectDefaults()
        {
            Assert.AreEqual(0.01f, TimeControlLimits.DefaultMinSpeed);
            Assert.AreEqual(16f, TimeControlLimits.DefaultMaxSpeed);
            Assert.AreEqual(60f, TimeControlLimits.DefaultPlaybackTicksPerSecond);
        }

        [Test]
        public void TimeControlConfig_CreateDefault_HasCorrectValues()
        {
            var config = TimeControlConfig.CreateDefault();
            
            Assert.AreEqual(0.25f, config.SlowMotionSpeed);
            Assert.AreEqual(4.0f, config.FastForwardSpeed);
            Assert.AreEqual(0.01f, config.MinSpeedMultiplier);
            Assert.AreEqual(16.0f, config.MaxSpeedMultiplier);
        }

        [Test]
        public void TimeHelpers_ClampSpeed_ClampsCorrectly()
        {
            Assert.AreEqual(0.01f, TimeHelpers.ClampSpeed(-1f));
            Assert.AreEqual(0.01f, TimeHelpers.ClampSpeed(0f));
            Assert.AreEqual(0.5f, TimeHelpers.ClampSpeed(0.5f));
            Assert.AreEqual(1f, TimeHelpers.ClampSpeed(1f));
            Assert.AreEqual(8f, TimeHelpers.ClampSpeed(8f));
            Assert.AreEqual(16f, TimeHelpers.ClampSpeed(20f));
            Assert.AreEqual(16f, TimeHelpers.ClampSpeed(100f));
        }

        [Test]
        public void TimeHelpers_SecondsToTicks_ConvertsCorrectly()
        {
            float fixedDeltaTime = 1f / 60f; // 60 TPS
            
            Assert.AreEqual(60u, TimeHelpers.SecondsToTicks(1f, fixedDeltaTime));
            Assert.AreEqual(300u, TimeHelpers.SecondsToTicks(5f, fixedDeltaTime));
            Assert.AreEqual(0u, TimeHelpers.SecondsToTicks(-1f, fixedDeltaTime));
            Assert.AreEqual(0u, TimeHelpers.SecondsToTicks(1f, 0f));
        }

        [Test]
        public void TimeHelpers_TicksToSeconds_ConvertsCorrectly()
        {
            float fixedDeltaTime = 1f / 60f; // 60 TPS
            
            Assert.That(TimeHelpers.TicksToSeconds(60u, fixedDeltaTime), Is.EqualTo(1f).Within(0.001f));
            Assert.That(TimeHelpers.TicksToSeconds(300u, fixedDeltaTime), Is.EqualTo(5f).Within(0.001f));
            Assert.AreEqual(0f, TimeHelpers.TicksToSeconds(0u, fixedDeltaTime));
        }

        [Test]
        public void TimeHelpers_GetDeterministicSeed_IsDeterministic()
        {
            uint seed1 = TimeHelpers.GetDeterministicSeed(100, 5, 0);
            uint seed2 = TimeHelpers.GetDeterministicSeed(100, 5, 0);
            uint seed3 = TimeHelpers.GetDeterministicSeed(100, 5, 1);
            uint seed4 = TimeHelpers.GetDeterministicSeed(101, 5, 0);
            
            Assert.AreEqual(seed1, seed2, "Same inputs should produce same seed");
            Assert.AreNotEqual(seed1, seed3, "Different salt should produce different seed");
            Assert.AreNotEqual(seed1, seed4, "Different tick should produce different seed");
        }

        [Test]
        public void TimeSystemFeatureFlags_CreateDefault_EnablesExpectedFeatures()
        {
            var flags = TimeSystemFeatureFlags.CreateDefault();
            
            Assert.AreEqual(TimeSimulationMode.SinglePlayer, flags.SimulationMode);
            Assert.IsTrue(flags.EnableGlobalRewind);
            Assert.IsTrue(flags.EnableLocalBubbleRewind);
            Assert.IsTrue(flags.EnableWorldSnapshots);
            Assert.IsTrue(flags.EnableTimeScaleScheduling);
            Assert.IsTrue(flags.EnableGlobalSnapshots);
            Assert.IsTrue(flags.EnableComponentHistory);
            Assert.IsTrue(flags.EnableTimeBubbles);
            Assert.IsFalse(flags.EnableLocalRewind, "Local rewind should be disabled by default");
            Assert.IsTrue(flags.EnableStasis);
            Assert.IsFalse(flags.EnforceMultiplayerCompatibility);
            Assert.IsFalse(flags.UseLegacySpeedLimits);
        }

        [Test]
        public void TimeSystemFeatureFlags_CreateMinimal_DisablesAdvancedFeatures()
        {
            var flags = TimeSystemFeatureFlags.CreateMinimal();
            
            Assert.IsFalse(flags.EnableTimeScaleScheduling);
            Assert.IsFalse(flags.EnableGlobalSnapshots);
            Assert.IsFalse(flags.EnableComponentHistory);
            Assert.IsFalse(flags.EnableTimeBubbles);
            Assert.IsFalse(flags.EnableLocalRewind);
            Assert.IsFalse(flags.EnableStasis);
            Assert.IsTrue(flags.UseLegacySpeedLimits);
        }

        [Test]
        public void TimeSystemFeatureFlags_CreateMultiplayer_DisablesLocalBubbles()
        {
            var flags = TimeSystemFeatureFlags.CreateMultiplayer();
            
            Assert.AreEqual(TimeSimulationMode.MultiplayerServer, flags.SimulationMode);
            Assert.IsFalse(flags.EnableGlobalRewind);
            Assert.IsFalse(flags.EnableLocalBubbleRewind);
            Assert.IsFalse(flags.EnableWorldSnapshots);
            Assert.IsFalse(flags.EnableTimeBubbles);
            Assert.IsFalse(flags.EnableLocalRewind);
            Assert.IsFalse(flags.EnableStasis);
            Assert.IsTrue(flags.EnforceMultiplayerCompatibility);
        }

        [Test]
        public void TimeSystemFeatureFlags_CreateMultiplayerClient_SetsCorrectMode()
        {
            var flags = TimeSystemFeatureFlags.CreateMultiplayerClient();
            
            Assert.AreEqual(TimeSimulationMode.MultiplayerClient, flags.SimulationMode);
            Assert.IsFalse(flags.EnableGlobalRewind);
            Assert.IsFalse(flags.EnableLocalBubbleRewind);
            Assert.IsFalse(flags.EnableWorldSnapshots);
            Assert.IsTrue(flags.EnforceMultiplayerCompatibility);
        }
    }
}

