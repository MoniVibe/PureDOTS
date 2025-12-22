using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using PureDOTS.Runtime.Time;

namespace PureDOTS.Tests.EditMode
{
    /// <summary>
    /// Tests for multiplayer mode behavior - systems should still tick but rewind/snapshots are guarded.
    /// </summary>
    [TestFixture]
    public class TimeMPModeTests
    {
        private World _world;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("Test World");
            _entityManager = _world.EntityManager;
            
            // Create core singletons with MP mode
            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(timeEntity, new TimeState
            {
                Tick = 100,
                DeltaTime = 1f / 60f,
                ElapsedTime = 100f / 60f,
                IsPaused = false,
                FixedDeltaTime = 1f / 60f,
                CurrentSpeedMultiplier = 1f
            });
            _entityManager.AddComponentData(timeEntity, new TickTimeState
            {
                Tick = 100,
                FixedDeltaTime = 1f / 60f,
                CurrentSpeedMultiplier = 1f,
                TargetTick = 100,
                IsPaused = false,
                IsPlaying = true
            });
            
            // Set MP mode flags
            _entityManager.AddComponentData(timeEntity, TimeSystemFeatureFlags.CreateMultiplayer());
            
            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Record,
                TargetTick = 100,
                TickDuration = 1f / 60f,
                MaxHistoryTicks = 3600,
                PendingStepTicks = 0
            });
            _entityManager.AddComponentData(rewindEntity, new RewindLegacyState
            {
                PlaybackSpeed = 1f,
                CurrentTick = 100,
                StartTick = 0,
                PlaybackTick = 0,
                PlaybackTicksPerSecond = 60f,
                ScrubDirection = ScrubDirection.None,
                ScrubSpeedMultiplier = 1f,
                RewindWindowTicks = 3600,
                ActiveTrack = default
            });
            _entityManager.AddBuffer<TimeControlCommand>(rewindEntity);
            
            // Create snapshot state
            var snapshotEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(snapshotEntity, WorldSnapshotState.CreateDefault());
            _entityManager.AddBuffer<WorldSnapshotMeta>(snapshotEntity);
            _entityManager.AddBuffer<WorldSnapshotData>(snapshotEntity);
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void TimeSystemFeatureFlags_MultiplayerMode_DisablesRewindAndSnapshots()
        {
            var flags = TimeSystemFeatureFlags.CreateMultiplayer();
            
            Assert.AreEqual(TimeSimulationMode.MultiplayerServer, flags.SimulationMode);
            Assert.IsFalse(flags.EnableGlobalRewind, "Global rewind should be disabled in MP");
            Assert.IsFalse(flags.EnableLocalBubbleRewind, "Local bubble rewind should be disabled in MP");
            Assert.IsFalse(flags.EnableWorldSnapshots, "World snapshots should be disabled in MP");
        }

        [Test]
        public void TimeSystemFeatureFlags_SinglePlayerMode_EnablesRewindAndSnapshots()
        {
            var flags = TimeSystemFeatureFlags.CreateDefault();
            
            Assert.AreEqual(TimeSimulationMode.SinglePlayer, flags.SimulationMode);
            Assert.IsTrue(flags.EnableGlobalRewind, "Global rewind should be enabled in SP");
            Assert.IsTrue(flags.EnableLocalBubbleRewind, "Local bubble rewind should be enabled in SP");
            Assert.IsTrue(flags.EnableWorldSnapshots, "World snapshots should be enabled in SP");
        }

        [Test]
        public void WorldSnapshotMeta_HasMPAwareFields()
        {
            var meta = new WorldSnapshotMeta
            {
                Tick = 100,
                IsValid = true,
                ByteOffset = 0,
                ByteLength = 1000,
                CompressionType = SnapshotCompressionType.None,
                EntityCount = 10,
                Checksum = 0,
                OwnerPlayerId = 0,
                Scope = TimeControlScope.Global
            };
            
            Assert.AreEqual(0, meta.OwnerPlayerId, "SP default should be 0");
            Assert.AreEqual(TimeControlScope.Global, meta.Scope, "SP default should be Global");
        }

        [Test]
        public void TimeBubbleParams_HasMPAwareFields()
        {
            var bubbleParams = TimeBubbleParams.CreateScale(1, 0.5f, 100);
            
            Assert.AreEqual(0, bubbleParams.OwnerPlayerId, "SP default should be 0");
            Assert.IsFalse(bubbleParams.AffectsOwnedEntitiesOnly, "SP default should be false");
        }

        [Test]
        public void TimeCheckpointHelpers_IsValidCheckpoint_ReturnsTrueForValidSnapshot()
        {
            var meta = new WorldSnapshotMeta
            {
                Tick = 100,
                IsValid = true,
                OwnerPlayerId = 0,
                Scope = TimeControlScope.Global
            };
            
            Assert.IsTrue(TimeCheckpointHelpers.IsValidCheckpoint(meta));
        }

        [Test]
        public void TimeCheckpointHelpers_IsValidCheckpoint_ReturnsFalseForInvalidSnapshot()
        {
            var meta = new WorldSnapshotMeta
            {
                Tick = 100,
                IsValid = false,
                OwnerPlayerId = 0,
                Scope = TimeControlScope.Global
            };
            
            Assert.IsFalse(TimeCheckpointHelpers.IsValidCheckpoint(meta));
        }

        [Test]
        public void TimeCheckpointHelpers_IsGlobalCheckpoint_ReturnsTrueForGlobal()
        {
            var meta = new WorldSnapshotMeta
            {
                Tick = 100,
                IsValid = true,
                OwnerPlayerId = 0,
                Scope = TimeControlScope.Global
            };
            
            Assert.IsTrue(TimeCheckpointHelpers.IsGlobalCheckpoint(meta));
        }

        [Test]
        public void TimeCheckpointHelpers_IsGlobalCheckpoint_ReturnsFalseForPlayerScope()
        {
            var meta = new WorldSnapshotMeta
            {
                Tick = 100,
                IsValid = true,
                OwnerPlayerId = 1,
                Scope = TimeControlScope.Player
            };
            
            Assert.IsFalse(TimeCheckpointHelpers.IsGlobalCheckpoint(meta));
        }

        [Test]
        public void TimePlayerIds_Constants_HaveCorrectValues()
        {
            Assert.AreEqual(0, TimePlayerIds.SinglePlayer);
            Assert.AreEqual(byte.MaxValue, TimePlayerIds.Invalid);
        }

        [Test]
        public void TimeSimulationMode_Enum_HasCorrectValues()
        {
            Assert.AreEqual(0, (byte)TimeSimulationMode.SinglePlayer);
            Assert.AreEqual(1, (byte)TimeSimulationMode.MultiplayerServer);
            Assert.AreEqual(2, (byte)TimeSimulationMode.MultiplayerClient);
        }

        [Test]
        public void TimeSystemFeatureFlags_SPMode_AllowsRewindAndSnapshots()
        {
            var flags = TimeSystemFeatureFlags.CreateDefault();
            
            // SP mode should allow all rewind/snapshot features
            Assert.AreEqual(TimeSimulationMode.SinglePlayer, flags.SimulationMode);
            Assert.IsTrue(flags.EnableGlobalRewind);
            Assert.IsTrue(flags.EnableLocalBubbleRewind);
            Assert.IsTrue(flags.EnableWorldSnapshots);
            Assert.IsTrue(flags.EnableTimeBubbles);
        }

        [Test]
        public void TimeSystemFeatureFlags_MPMode_DisablesRewindAndSnapshots()
        {
            var flags = TimeSystemFeatureFlags.CreateMultiplayer();
            
            // MP mode should disable rewind/snapshot features
            Assert.AreEqual(TimeSimulationMode.MultiplayerServer, flags.SimulationMode);
            Assert.IsFalse(flags.EnableGlobalRewind, "Global rewind not supported in MP yet");
            Assert.IsFalse(flags.EnableLocalBubbleRewind, "Local bubble rewind not supported in MP yet");
            Assert.IsFalse(flags.EnableWorldSnapshots, "World snapshots not supported in MP yet");
        }

        [Test]
        public void TimeBubbleParams_SPDefaults_AreCorrect()
        {
            var bubbleParams = TimeBubbleParams.CreateScale(1, 0.5f, 100);
            
            // SP defaults
            Assert.AreEqual(0, bubbleParams.OwnerPlayerId, "SP should use OwnerPlayerId = 0");
            Assert.IsFalse(bubbleParams.AffectsOwnedEntitiesOnly, "SP should use AffectsOwnedEntitiesOnly = false");
        }

        [Test]
        public void WorldSnapshotMeta_SPDefaults_AreCorrect()
        {
            var meta = new WorldSnapshotMeta
            {
                Tick = 100,
                IsValid = true,
                OwnerPlayerId = 0,
                Scope = TimeControlScope.Global
            };
            
            // SP defaults
            Assert.AreEqual(0, meta.OwnerPlayerId, "SP should use OwnerPlayerId = 0");
            Assert.AreEqual(TimeControlScope.Global, meta.Scope, "SP should use Scope = Global");
        }
    }
}
