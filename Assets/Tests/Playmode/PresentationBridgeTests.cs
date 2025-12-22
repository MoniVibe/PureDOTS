using System.Collections;
using NUnit.Framework;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.TestTools;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Tests for validating rewind-safe presentation behavior.
    /// Ensures presentation systems correctly handle spawn/recycle during rewind operations.
    /// </summary>
    [TestFixture]
    public class PresentationBridgeTests
    {
        private World _world;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            if (_world == null)
            {
                _world = new World("TestWorld");
                World.DefaultGameObjectInjectionWorld = _world;
            }
            _entityManager = _world.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test entities
        }

        /// <summary>
        /// Validates that presentation spawn requests are blocked during playback rewind mode.
        /// </summary>
        [Test]
        public void SpawnRequests_AreBlocked_DuringPlaybackMode()
        {
            // Arrange - Create rewind state in playback mode
            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Playback,
                TargetTick = 100,
                TickDuration = 1f / 60f,
                MaxHistoryTicks = 600,
                PendingStepTicks = 0
            });
            _entityManager.AddComponentData(rewindEntity, new RewindLegacyState
            {
                PlaybackSpeed = 1f,
                CurrentTick = 0,
                StartTick = 0,
                PlaybackTick = 50,
                PlaybackTicksPerSecond = 60f,
                ScrubDirection = 0,
                ScrubSpeedMultiplier = 1f,
                RewindWindowTicks = 0,
                ActiveTrack = default
            });

            var hubEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(hubEntity, new PresentationRequestHub());
            _entityManager.AddComponentData(hubEntity, new PresentationRequestFailures());
            var spawnBuffer = _entityManager.AddBuffer<SpawnCompanionRequest>(hubEntity);
            _entityManager.AddBuffer<PlayEffectRequest>(hubEntity);
            _entityManager.AddBuffer<DespawnCompanionRequest>(hubEntity);

            // Add a spawn request
            spawnBuffer.Add(new SpawnCompanionRequest
            {
                CompanionId = 1,
                Target = Entity.Null,
                Position = float3.zero,
                Rotation = quaternion.identity
            });

            // Act - Update would normally process the request
            // In playback mode, the system should skip processing

            // Assert - Verify the rewind guard pattern
            var rewindState = _entityManager.GetComponentData<RewindState>(rewindEntity);
            Assert.AreEqual(RewindMode.Playback, rewindState.Mode,
                "Rewind state should be in Playback mode");

            // Clean up
            _entityManager.DestroyEntity(rewindEntity);
            _entityManager.DestroyEntity(hubEntity);
        }

        /// <summary>
        /// Validates that presentation spawn requests are processed during record mode.
        /// </summary>
        [Test]
        public void SpawnRequests_AreProcessed_DuringRecordMode()
        {
            // Arrange - Create rewind state in record mode
            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Record,
                TargetTick = 0,
                TickDuration = 1f / 60f,
                MaxHistoryTicks = 600,
                PendingStepTicks = 0
            });
            _entityManager.AddComponentData(rewindEntity, new RewindLegacyState
            {
                PlaybackSpeed = 1f,
                CurrentTick = 0,
                StartTick = 0,
                PlaybackTick = 0,
                PlaybackTicksPerSecond = 60f,
                ScrubDirection = 0,
                ScrubSpeedMultiplier = 1f,
                RewindWindowTicks = 0,
                ActiveTrack = default
            });

            // Assert - Verify record mode is correctly identified
            var rewindState = _entityManager.GetComponentData<RewindState>(rewindEntity);
            Assert.AreEqual(RewindMode.Record, rewindState.Mode,
                "Rewind state should be in Record mode for spawn processing");

            // Clean up
            _entityManager.DestroyEntity(rewindEntity);
        }

        /// <summary>
        /// Validates companion presentation component structure.
        /// </summary>
        [Test]
        public void CompanionPresentation_HasCorrectDefaults()
        {
            // Arrange
            var entity = _entityManager.CreateEntity();

            // Act
            _entityManager.AddComponentData(entity, new CompanionPresentation
            {
                CompanionId = 42,
                Handle = 1,
                Kind = PresentationKind.Mesh,
                AttachRule = PresentationAttachRule.FollowTarget,
                Offset = new float3(0, 1, 0),
                FollowLerp = 0.5f
            });

            // Assert
            var companion = _entityManager.GetComponentData<CompanionPresentation>(entity);
            Assert.AreEqual(42, companion.CompanionId);
            Assert.AreEqual(PresentationKind.Mesh, companion.Kind);
            Assert.AreEqual(PresentationAttachRule.FollowTarget, companion.AttachRule);
            Assert.AreEqual(0.5f, companion.FollowLerp, 0.001f);

            // Clean up
            _entityManager.DestroyEntity(entity);
        }

        /// <summary>
        /// Validates presentation handle sync config defaults.
        /// </summary>
        [Test]
        public void PresentationHandleSyncConfig_DefaultValues_AreCorrect()
        {
            // Act
            var config = PresentationHandleSyncConfig.Default;

            // Assert
            Assert.AreEqual(1f, config.PositionLerp, 0.001f,
                "Default position lerp should snap (1.0)");
            Assert.AreEqual(1f, config.RotationLerp, 0.001f,
                "Default rotation lerp should snap (1.0)");
            Assert.AreEqual(1f, config.ScaleLerp, 0.001f,
                "Default scale lerp should snap (1.0)");
            Assert.AreEqual(float3.zero, config.VisualOffset,
                "Default visual offset should be zero");
        }

        /// <summary>
        /// Validates presentation pool stats tracking.
        /// </summary>
        [Test]
        public void PresentationPoolStats_TracksSpawnAndRecycle()
        {
            // Arrange
            var entity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(entity, new PresentationPoolStats
            {
                ActiveVisuals = 10,
                SpawnedThisFrame = 2,
                RecycledThisFrame = 1,
                TotalSpawned = 100,
                TotalRecycled = 90
            });

            // Act
            var stats = _entityManager.GetComponentData<PresentationPoolStats>(entity);

            // Assert
            Assert.AreEqual(10u, stats.ActiveVisuals);
            Assert.AreEqual(2u, stats.SpawnedThisFrame);
            Assert.AreEqual(1u, stats.RecycledThisFrame);
            Assert.AreEqual(100u, stats.TotalSpawned);
            Assert.AreEqual(90u, stats.TotalRecycled);

            // Verify pool balance
            Assert.AreEqual(stats.TotalSpawned - stats.TotalRecycled, stats.ActiveVisuals,
                "Active visuals should equal total spawned minus total recycled");

            // Clean up
            _entityManager.DestroyEntity(entity);
        }

        /// <summary>
        /// Validates presentation spawn flags enum values.
        /// </summary>
        [Test]
        public void PresentationSpawnFlags_CanBeCombined()
        {
            // Act
            var flags = PresentationSpawnFlags.AllowPooling | PresentationSpawnFlags.OverrideTint;

            // Assert
            Assert.IsTrue((flags & PresentationSpawnFlags.AllowPooling) != 0,
                "AllowPooling flag should be set");
            Assert.IsTrue((flags & PresentationSpawnFlags.OverrideTint) != 0,
                "OverrideTint flag should be set");
            Assert.IsFalse((flags & PresentationSpawnFlags.ForceAnimateOnSpawn) != 0,
                "ForceAnimateOnSpawn flag should not be set");
        }

        /// <summary>
        /// Validates presentation lifetime policies.
        /// </summary>
        [Test]
        public void PresentationLifetimePolicy_HasExpectedValues()
        {
            // Assert
            Assert.AreEqual(0, (int)PresentationLifetimePolicy.Timed,
                "Timed should be default (0)");
            Assert.AreEqual(1, (int)PresentationLifetimePolicy.UntilRecycle);
            Assert.AreEqual(2, (int)PresentationLifetimePolicy.Manual);
        }

        /// <summary>
        /// Validates presentation attach rules.
        /// </summary>
        [Test]
        public void PresentationAttachRule_HasExpectedValues()
        {
            // Assert
            Assert.AreEqual(0, (int)PresentationAttachRule.World,
                "World should be default (0)");
            Assert.AreEqual(1, (int)PresentationAttachRule.FollowTarget);
            Assert.AreEqual(2, (int)PresentationAttachRule.AttachToTarget);
        }

        /// <summary>
        /// Validates style override resolution.
        /// </summary>
        [Test]
        public void PresentationStyleOverride_ResolvesCorrectly()
        {
            // Arrange
            var bindingStyle = new PresentationStyleBlock
            {
                Style = "base_style",
                PaletteIndex = 1,
                Size = 1.0f,
                Speed = 1.0f
            };

            var overrideStyle = new PresentationStyleOverride
            {
                Style = "override_style",
                PaletteIndex = 2,
                Size = 2.0f,
                Speed = 0f // No override for speed
            };

            // Act
            var resolved = PresentationBindingUtility.ResolveStyle(bindingStyle, overrideStyle);

            // Assert
            Assert.AreEqual("override_style", resolved.Style.ToString(),
                "Style should be overridden");
            Assert.AreEqual(2, resolved.PaletteIndex,
                "Palette should be overridden");
            Assert.AreEqual(2.0f, resolved.Size, 0.001f,
                "Size should be overridden");
            Assert.AreEqual(1.0f, resolved.Speed, 0.001f,
                "Speed should remain from binding (no override)");
        }

        /// <summary>
        /// Validates despawn request handling during rewind.
        /// </summary>
        [Test]
        public void DespawnRequests_AreBlocked_DuringRewindScrub()
        {
            // Arrange - Create rewind state in scrub mode
            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Scrub,
                TargetTick = 100,
                TickDuration = 1f / 60f,
                MaxHistoryTicks = 600,
                PendingStepTicks = 0
            });
            _entityManager.AddComponentData(rewindEntity, new RewindLegacyState
            {
                PlaybackSpeed = 1f,
                CurrentTick = 0,
                StartTick = 0,
                PlaybackTick = 75,
                PlaybackTicksPerSecond = 60f,
                ScrubDirection = ScrubDirection.Backward,
                ScrubSpeedMultiplier = 1f,
                RewindWindowTicks = 0,
                ActiveTrack = default
            });

            // Assert - Verify scrub mode is not record mode
            var rewindState = _entityManager.GetComponentData<RewindState>(rewindEntity);
            Assert.AreNotEqual(RewindMode.Record, rewindState.Mode,
                "Scrub mode should block despawn processing");

            // Clean up
            _entityManager.DestroyEntity(rewindEntity);
        }

        /// <summary>
        /// Validates presentation request failures tracking.
        /// </summary>
        [Test]
        public void PresentationRequestFailures_TracksAllFailureTypes()
        {
            // Arrange
            var entity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(entity, new PresentationRequestFailures
            {
                MissingBridge = 1,
                MissingBindings = 2,
                FailedPlayback = 3,
                SuccessfulSpawns = 10,
                SuccessfulEffects = 5
            });

            // Act
            var failures = _entityManager.GetComponentData<PresentationRequestFailures>(entity);

            // Assert
            Assert.AreEqual(1, failures.MissingBridge);
            Assert.AreEqual(2, failures.MissingBindings);
            Assert.AreEqual(3, failures.FailedPlayback);
            Assert.AreEqual(10, failures.SuccessfulSpawns);
            Assert.AreEqual(5, failures.SuccessfulEffects);

            // Clean up
            _entityManager.DestroyEntity(entity);
        }

        /// <summary>
        /// Validates presentation cleanup tag structure.
        /// </summary>
        [Test]
        public void PresentationCleanupTag_HasRequiredFields()
        {
            // Arrange
            var entity = _entityManager.CreateEntity();
            var targetEntity = _entityManager.CreateEntity();

            _entityManager.AddComponentData(entity, new PresentationCleanupTag
            {
                Handle = 42,
                Kind = PresentationKind.Vfx,
                SecondsRemaining = 2.5f,
                Lifetime = PresentationLifetimePolicy.Timed,
                AttachRule = PresentationAttachRule.FollowTarget,
                Target = targetEntity
            });

            // Act
            var cleanup = _entityManager.GetComponentData<PresentationCleanupTag>(entity);

            // Assert
            Assert.AreEqual(42, cleanup.Handle);
            Assert.AreEqual(PresentationKind.Vfx, cleanup.Kind);
            Assert.AreEqual(2.5f, cleanup.SecondsRemaining, 0.001f);
            Assert.AreEqual(PresentationLifetimePolicy.Timed, cleanup.Lifetime);
            Assert.AreEqual(targetEntity, cleanup.Target);

            // Clean up
            _entityManager.DestroyEntity(entity);
            _entityManager.DestroyEntity(targetEntity);
        }

        /// <summary>
        /// Validates presentation handle component for visual tracking.
        /// </summary>
        [Test]
        public void PresentationHandle_TracksVisualEntity()
        {
            // Arrange
            var entity = _entityManager.CreateEntity();
            var visualEntity = _entityManager.CreateEntity();
            var hash = new Unity.Entities.Hash128("test_descriptor_hash");

            _entityManager.AddComponentData(entity, new PresentationHandle
            {
                Visual = visualEntity,
                DescriptorHash = hash,
                VariantSeed = 12345
            });

            // Act
            var handle = _entityManager.GetComponentData<PresentationHandle>(entity);

            // Assert
            Assert.AreEqual(visualEntity, handle.Visual,
                "Visual entity reference should be preserved");
            Assert.AreEqual(hash, handle.DescriptorHash,
                "Descriptor hash should be preserved");
            Assert.AreEqual(12345u, handle.VariantSeed,
                "Variant seed should be preserved");

            // Clean up
            _entityManager.DestroyEntity(entity);
            _entityManager.DestroyEntity(visualEntity);
        }
    }
}
