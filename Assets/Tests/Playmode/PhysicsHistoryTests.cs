using System;
using System.IO;
using NUnit.Framework;
using PureDOTS.Runtime.Config;
using PureDOTS.Runtime.History;
using PureDOTS.Systems;
using Unity.Entities;
using Unity.Physics;

namespace PureDOTS.Tests
{
    public class PhysicsHistoryTests
    {
        private World _world;
        private EntityManager _entityManager;
        private PhysicsHistoryCaptureSystem _historySystem;
        private Entity _physicsEntity;
        private Entity _simulationEntity;
        private Entity _timeEntity;
        private string _configPath;

        [SetUp]
        public void SetUp()
        {
            RuntimeConfigRegistry.ResetForTests();
            _configPath = Path.Combine(Path.GetTempPath(), $"puredots_physics_history_{Guid.NewGuid():N}.cfg");
            RuntimeConfigRegistry.StoragePath = _configPath;
            RuntimeConfigRegistry.Initialize();
            RuntimeConfigRegistry.SetValue("history.physics.enabled", "1", out _);
            RuntimeConfigRegistry.SetValue("history.physics.length", "3", out _);

            _world = new World("PhysicsHistoryTests");
            _entityManager = _world.EntityManager;

            _physicsEntity = _entityManager.CreateEntity(typeof(PhysicsWorldSingleton));
            _entityManager.SetComponentData(_physicsEntity, new PhysicsWorldSingleton
            {
                PhysicsWorld = new PhysicsWorld(0, 0, 0)
            });

            _simulationEntity = _entityManager.CreateEntity(typeof(SimulationSingleton));
            _entityManager.SetComponentData(_simulationEntity, new SimulationSingleton());

            _timeEntity = _entityManager.CreateEntity(typeof(PureDOTS.Runtime.Components.TimeState));
            _entityManager.SetComponentData(_timeEntity, new PureDOTS.Runtime.Components.TimeState
            {
                Tick = 0,
                IsPaused = false
            });

            _historySystem = _world.CreateSystemManaged<PhysicsHistoryCaptureSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_entityManager.Exists(_physicsEntity))
            {
                var physics = _entityManager.GetComponentData<PhysicsWorldSingleton>(_physicsEntity);
                physics.PhysicsWorld.Dispose();
            }

            _world.Dispose();

            if (File.Exists(_configPath))
            {
                File.Delete(_configPath);
            }
        }

        [Test]
        public void CaptureStoresLatestTick()
        {
            AdvanceTick(1);
            AdvanceTick(2);

            var handle = PhysicsHistory.GetHandle();
            Assert.IsTrue(handle.IsCreated);
            Assert.IsTrue(handle.TryCloneLatest(out var world, out var tick));
            Assert.AreEqual(2u, tick);
            world.Dispose();
        }

        [Test]
        public void DisabledConfigSkipsCapture()
        {
            AdvanceTick(1);
            RuntimeConfigRegistry.SetValue("history.physics.enabled", "0", out _);
            AdvanceTick(2);

            var handle = PhysicsHistory.GetHandle();
            Assert.IsTrue(handle.IsCreated);
            Assert.IsTrue(handle.TryCloneLatest(out var world, out var tick));
            Assert.AreEqual(1u, tick);
            world.Dispose();
        }

        private void AdvanceTick(uint tick)
        {
            var timeState = _entityManager.GetComponentData<PureDOTS.Runtime.Components.TimeState>(_timeEntity);
            timeState.Tick = tick;
            _entityManager.SetComponentData(_timeEntity, timeState);
            _historySystem.Update();
        }
    }
}


