using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
#if INCLUDE_GODGAME_IN_PUREDOTS
using Godgame.Temporal;
#endif
#if INCLUDE_SPACE4X_IN_PUREDOTS
using Space4X.Temporal;
#endif

namespace PureDOTS.Tests.EditMode
{
    /// <summary>
    /// Tests for game-facing time API wiring (PlayerId, Scope, Source fields).
    /// Verifies that commands are created with correct MP-aware fields.
    /// </summary>
    [TestFixture]
    public class TimeAPIWiringTests
    {
        private World _world;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("Test World");
            _entityManager = _world.EntityManager;
            
            // Create minimal singletons needed for APIs
            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(timeEntity, new TimeState
            {
                Tick = 0,
                DeltaTime = 1f / 60f,
                ElapsedTime = 0f,
                IsPaused = false,
                FixedDeltaTime = 1f / 60f,
                CurrentSpeedMultiplier = 1f
            });
            
            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Record,
                TargetTick = 0,
                TickDuration = 1f / 60f,
                MaxHistoryTicks = 3600,
                PendingStepTicks = 0
            });
            _entityManager.AddComponentData(rewindEntity, new RewindLegacyState
            {
                PlaybackSpeed = 1f,
                CurrentTick = 0,
                StartTick = 0,
                PlaybackTick = 0,
                PlaybackTicksPerSecond = 60f,
                ScrubDirection = ScrubDirection.None,
                ScrubSpeedMultiplier = 1f,
                RewindWindowTicks = 3600,
                ActiveTrack = default
            });
            _entityManager.AddBuffer<TimeControlCommand>(rewindEntity);
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

#if INCLUDE_GODGAME_IN_PUREDOTS
        [Test]
        public void GodgameTimeAPI_SetGlobalTimeSpeed_SP_SetsCorrectFields()
        {
            bool result = GodgameTimeAPI.SetGlobalTimeSpeed(_world, 2.0f);
            
            Assert.IsTrue(result, "Command should be accepted");
            
            var rewindEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>()).GetSingletonEntity();
            var commands = _entityManager.GetBuffer<TimeControlCommand>(rewindEntity);
            
            Assert.AreEqual(1, commands.Length, "Should have one command");
            var cmd = commands[0];
            
            Assert.AreEqual(TimeControlCommandType.SetSpeed, cmd.Type);
            Assert.AreEqual(2.0f, cmd.FloatParam);
            Assert.AreEqual(TimeControlScope.Global, cmd.Scope);
            Assert.AreEqual(TimePlayerIds.SinglePlayer, cmd.PlayerId);
            Assert.AreEqual(TimeControlSource.Player, cmd.Source);
        }
#endif

#if INCLUDE_GODGAME_IN_PUREDOTS
        [Test]
        public void GodgameTimeAPI_SetGlobalTimeSpeed_WithPlayerId_SetsCorrectFields()
        {
            byte playerId = 1;
            bool result = GodgameTimeAPI.SetGlobalTimeSpeed(_world, 2.0f, playerId);
            
            Assert.IsTrue(result, "Command should be accepted");
            
            var rewindEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>()).GetSingletonEntity();
            var commands = _entityManager.GetBuffer<TimeControlCommand>(rewindEntity);
            
            Assert.AreEqual(1, commands.Length, "Should have one command");
            var cmd = commands[0];
            
            Assert.AreEqual(TimeControlCommandType.SetSpeed, cmd.Type);
            Assert.AreEqual(2.0f, cmd.FloatParam);
            Assert.AreEqual(TimeControlScope.Global, cmd.Scope);
            Assert.AreEqual(playerId, cmd.PlayerId, "Should use provided playerId");
            Assert.AreEqual(TimeControlSource.Player, cmd.Source);
        }
#endif

#if INCLUDE_GODGAME_IN_PUREDOTS
        [Test]
        public void GodgameTimeAPI_RequestGlobalRewind_SP_SetsCorrectFields()
        {
            bool result = GodgameTimeAPI.RequestGlobalRewind(_world, 100, 0);
            
            Assert.IsTrue(result, "Command should be accepted");
            
            var rewindEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>()).GetSingletonEntity();
            var commands = _entityManager.GetBuffer<TimeControlCommand>(rewindEntity);
            
            Assert.AreEqual(1, commands.Length, "Should have one command");
            var cmd = commands[0];
            
            Assert.AreEqual(TimeControlCommandType.StartRewind, cmd.Type);
            Assert.AreEqual(TimeControlScope.Global, cmd.Scope);
            Assert.AreEqual(TimePlayerIds.SinglePlayer, cmd.PlayerId);
            Assert.AreEqual(TimeControlSource.Player, cmd.Source);
        }
#endif

#if INCLUDE_GODGAME_IN_PUREDOTS
        [Test]
        public void GodgameTimeAPI_RequestGlobalRewind_WithPlayerId_SetsCorrectFields()
        {
            byte playerId = 2;
            bool result = GodgameTimeAPI.RequestGlobalRewind(_world, 100, 0, playerId);
            
            Assert.IsTrue(result, "Command should be accepted");
            
            var rewindEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>()).GetSingletonEntity();
            var commands = _entityManager.GetBuffer<TimeControlCommand>(rewindEntity);
            
            Assert.AreEqual(1, commands.Length, "Should have one command");
            var cmd = commands[0];
            
            Assert.AreEqual(TimeControlCommandType.StartRewind, cmd.Type);
            Assert.AreEqual(TimeControlScope.Global, cmd.Scope);
            Assert.AreEqual(playerId, cmd.PlayerId, "Should use provided playerId");
        }
#endif

#if INCLUDE_GODGAME_IN_PUREDOTS
        [Test]
        public void GodgameTimeAPI_SpawnTimeBubble_SP_SetsOwnerPlayerId()
        {
            var center = new float3(0, 0, 0);
            float radius = 10f;
            
            Entity bubble = GodgameTimeAPI.SpawnTimeBubble(_world, center, radius, TimeBubbleMode.Scale, 0.5f);
            
            Assert.AreNotEqual(Entity.Null, bubble, "Bubble should be created");
            Assert.IsTrue(_entityManager.HasComponent<TimeBubbleParams>(bubble));
            
            var params_ = _entityManager.GetComponentData<TimeBubbleParams>(bubble);
            Assert.AreEqual(TimePlayerIds.SinglePlayer, params_.OwnerPlayerId);
            Assert.IsFalse(params_.AffectsOwnedEntitiesOnly);
        }
#endif

#if INCLUDE_GODGAME_IN_PUREDOTS
        [Test]
        public void GodgameTimeAPI_SpawnTimeBubble_WithOwnerPlayerId_SetsCorrectOwner()
        {
            var center = new float3(0, 0, 0);
            float radius = 10f;
            byte ownerPlayerId = 3;
            
            Entity bubble = GodgameTimeAPI.SpawnTimeBubble(_world, center, radius, TimeBubbleMode.Scale, 0.5f, 
                0, 100, Entity.Null, ownerPlayerId);
            
            Assert.AreNotEqual(Entity.Null, bubble, "Bubble should be created");
            
            var params_ = _entityManager.GetComponentData<TimeBubbleParams>(bubble);
            Assert.AreEqual(ownerPlayerId, params_.OwnerPlayerId, "Should use provided ownerPlayerId");
        }
#endif

#if INCLUDE_SPACE4X_IN_PUREDOTS
        [Test]
        public void Space4XTimeAPI_SetGlobalTimeSpeed_SP_SetsCorrectFields()
        {
            bool result = Space4XTimeAPI.SetGlobalTimeSpeed(_world, 2.0f);
            
            Assert.IsTrue(result, "Command should be accepted");
            
            var rewindEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>()).GetSingletonEntity();
            var commands = _entityManager.GetBuffer<TimeControlCommand>(rewindEntity);
            
            Assert.AreEqual(1, commands.Length, "Should have one command");
            var cmd = commands[0];
            
            Assert.AreEqual(TimeControlCommandType.SetSpeed, cmd.Type);
            Assert.AreEqual(2.0f, cmd.FloatParam);
            Assert.AreEqual(TimeControlScope.Global, cmd.Scope);
            Assert.AreEqual(TimePlayerIds.SinglePlayer, cmd.PlayerId);
            Assert.AreEqual(TimeControlSource.Player, cmd.Source);
        }
#endif

#if INCLUDE_SPACE4X_IN_PUREDOTS
        [Test]
        public void Space4XTimeAPI_SetGlobalTimeSpeed_WithPlayerId_SetsCorrectFields()
        {
            byte playerId = 1;
            bool result = Space4XTimeAPI.SetGlobalTimeSpeed(_world, 2.0f, playerId);
            
            Assert.IsTrue(result, "Command should be accepted");
            
            var rewindEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>()).GetSingletonEntity();
            var commands = _entityManager.GetBuffer<TimeControlCommand>(rewindEntity);
            
            Assert.AreEqual(1, commands.Length, "Should have one command");
            var cmd = commands[0];
            
            Assert.AreEqual(playerId, cmd.PlayerId, "Should use provided playerId");
        }
#endif

#if INCLUDE_SPACE4X_IN_PUREDOTS
        [Test]
        public void Space4XTimeAPI_SpawnLocalTimeField_SP_SetsOwnerPlayerId()
        {
            var center = new float3(0, 0, 0);
            float radius = 10f;
            
            Entity field = Space4XTimeAPI.SpawnLocalTimeField(_world, center, radius, TimeBubbleMode.Scale, 0.5f);
            
            Assert.AreNotEqual(Entity.Null, field, "Field should be created");
            Assert.IsTrue(_entityManager.HasComponent<TimeBubbleParams>(field));
            
            var params_ = _entityManager.GetComponentData<TimeBubbleParams>(field);
            Assert.AreEqual(TimePlayerIds.SinglePlayer, params_.OwnerPlayerId);
            Assert.IsFalse(params_.AffectsOwnedEntitiesOnly);
        }
#endif

#if INCLUDE_SPACE4X_IN_PUREDOTS
        [Test]
        public void Space4XTimeAPI_SpawnLocalTimeField_WithOwnerPlayerId_SetsCorrectOwner()
        {
            var center = new float3(0, 0, 0);
            float radius = 10f;
            byte ownerPlayerId = 4;
            
            Entity field = Space4XTimeAPI.SpawnLocalTimeField(_world, center, radius, TimeBubbleMode.Scale, 0.5f,
                0, 100, Entity.Null, ownerPlayerId);
            
            Assert.AreNotEqual(Entity.Null, field, "Field should be created");
            
            var params_ = _entityManager.GetComponentData<TimeBubbleParams>(field);
            Assert.AreEqual(ownerPlayerId, params_.OwnerPlayerId, "Should use provided ownerPlayerId");
        }
#endif
    }
}
