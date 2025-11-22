using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Entities;

namespace PureDOTS.Tests
{
    public class TimeStateTests
    {
        private World _world;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PureDOTS Test World");
            _entityManager = _world.EntityManager;
            EnsureCoreSingletons();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void CoreSingletonBootstrapSystem_CreatesTimeHistoryAndRewindSingletons()
        {
            EnsureCoreSingletons();

            Assert.IsTrue(_entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).TryGetSingleton(out TimeState _));
            Assert.IsTrue(_entityManager.CreateEntityQuery(ComponentType.ReadOnly<HistorySettings>()).TryGetSingleton(out HistorySettings _));
            Assert.IsTrue(_entityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>()).TryGetSingleton(out RewindState _));
            Assert.IsTrue(_entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceRegistry>()).TryGetSingleton(out ResourceRegistry _));
            Assert.IsTrue(_entityManager.CreateEntityQuery(ComponentType.ReadOnly<StorehouseRegistry>()).TryGetSingleton(out StorehouseRegistry _));
            Assert.IsTrue(_entityManager.CreateEntityQuery(ComponentType.ReadOnly<VillagerRegistry>()).TryGetSingleton(out VillagerRegistry _));
            var resourceRegistryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceRegistry>()).GetSingletonEntity();
            Assert.IsTrue(_entityManager.HasBuffer<ResourceRegistryEntry>(resourceRegistryEntity));
            var storehouseRegistryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<StorehouseRegistry>()).GetSingletonEntity();
            Assert.IsTrue(_entityManager.HasBuffer<StorehouseRegistryEntry>(storehouseRegistryEntity));
            var villagerRegistryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<VillagerRegistry>()).GetSingletonEntity();
            Assert.IsTrue(_entityManager.HasBuffer<VillagerRegistryEntry>(villagerRegistryEntity));
        }

        [Test]
        public void TimeSettingsConfigSystem_AppliesOverrides()
        {
            var timeEntity = _entityManager.CreateEntity(typeof(TimeState));
            _entityManager.SetComponentData(timeEntity, new TimeState
            {
                FixedDeltaTime = 0.02f,
                CurrentSpeedMultiplier = 1f,
                Tick = 0,
                IsPaused = false
            });

            var overrideEntity = _entityManager.CreateEntity(typeof(TimeSettingsConfig));
            _entityManager.SetComponentData(overrideEntity, new TimeSettingsConfig
            {
                FixedDeltaTime = 0.1f,
                DefaultSpeedMultiplier = 2f,
                PauseOnStart = true
            });

            var system = _world.CreateSystem<TimeSettingsConfigSystem>();
            system.Update(_world.Unmanaged);

            var updated = _entityManager.GetComponentData<TimeState>(timeEntity);
            Assert.AreEqual(0.1f, updated.FixedDeltaTime);
            Assert.AreEqual(2f, updated.CurrentSpeedMultiplier);
            Assert.IsTrue(updated.IsPaused);
        }

        [Test]
        public void GameplayFixedStepSyncSystem_PropagatesTimeStateDelta()
        {
            EnsureCoreSingletons();

            var timeEntity = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<TimeState>()).GetSingletonEntity();
            var fixedStepEntity = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<GameplayFixedStep>()).GetSingletonEntity();

            var system = _world.CreateSystemManaged<GameplayFixedStepSyncSystem>();

            // First delta
            var timeState = _entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.FixedDeltaTime = 0.02f;
            _entityManager.SetComponentData(timeEntity, timeState);

            system.Update();

            var fixedStep = _entityManager.GetComponentData<GameplayFixedStep>(fixedStepEntity);
            Assert.AreEqual(0.02f, fixedStep.FixedDeltaTime, 1e-6f);

            var fixedStepGroup = _world.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            Assert.IsNotNull(fixedStepGroup);
            Assert.AreEqual(0.02f, fixedStepGroup.Timestep, 1e-6f);

            // Change delta and ensure propagation occurs again.
            timeState.FixedDeltaTime = 0.05f;
            _entityManager.SetComponentData(timeEntity, timeState);

            system.Update();

            fixedStep = _entityManager.GetComponentData<GameplayFixedStep>(fixedStepEntity);
            Assert.AreEqual(0.05f, fixedStep.FixedDeltaTime, 1e-6f);
            Assert.AreEqual(0.05f, fixedStepGroup.Timestep, 1e-6f);
        }

        private void EnsureCoreSingletons()
        {
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);
        }
    }
}
