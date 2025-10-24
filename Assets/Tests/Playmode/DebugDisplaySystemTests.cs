using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests
{
    /// <summary>
    /// Tests for DebugDisplaySystem verifying singleton creation and data population.
    /// </summary>
    public class DebugDisplaySystemTests
    {
        private World _world;
        private EntityManager _entityManager;
        private DebugDisplaySystem _debugSystem;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PureDOTS Test World");
            _entityManager = _world.EntityManager;
            _debugSystem = new DebugDisplaySystem();
            EnsureCoreSingletons();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void DebugDisplaySystem_CreatesSingletonOnCreate()
        {
            var handle = CreateDebugSystem();
            using var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>());
            Assert.IsFalse(query.IsEmptyIgnoreFilter);
        }

        [Test]
        public void DebugDisplaySystem_PopulatesTimeState()
        {
            var handle = CreateDebugSystem();
            ref var systemState = ref _world.Unmanaged.ResolveSystemStateRef(handle);

            var timeEntity = _entityManager.CreateEntity(typeof(TimeState));
            _entityManager.SetComponentData(timeEntity, new TimeState
            {
                FixedDeltaTime = 0.016f,
                CurrentSpeedMultiplier = 2f,
                Tick = 42,
                IsPaused = true
            });

            _debugSystem.OnUpdate(ref systemState);

            var debugData = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>()).GetSingleton<DebugDisplayData>();
            Assert.AreEqual(42u, debugData.CurrentTick);
            Assert.IsTrue(debugData.IsPaused);
            Assert.Greater(debugData.TimeStateText.Length, 0);
        }

        [Test]
        public void DebugDisplaySystem_PopulatesRewindState()
        {
            var handle = CreateDebugSystem();
            ref var systemState = ref _world.Unmanaged.ResolveSystemStateRef(handle);

            var rewindEntity = _entityManager.CreateEntity(typeof(RewindState));
            _entityManager.SetComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Playback,
                StartTick = 10,
                TargetTick = 50,
                PlaybackTick = 25,
                PlaybackTicksPerSecond = 90f,
                ScrubDirection = 1,
                ScrubSpeedMultiplier = 2f
            });

            _debugSystem.OnUpdate(ref systemState);

            var debugData = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>()).GetSingleton<DebugDisplayData>();
            Assert.Greater(debugData.RewindStateText.Length, 0);
            Assert.IsTrue(debugData.RewindStateText.ToString().Contains("Playback"));
        }

        [Test]
        public void DebugDisplaySystem_CountsVillagers()
        {
            var handle = CreateDebugSystem();
            ref var systemState = ref _world.Unmanaged.ResolveSystemStateRef(handle);

            // Create test villagers
            for (int i = 0; i < 5; i++)
            {
                var villager = _entityManager.CreateEntity(typeof(VillagerId));
                _entityManager.SetComponentData(villager, new VillagerId { Value = i, FactionId = 0 });
            }

            _debugSystem.OnUpdate(ref systemState);

            var debugData = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>()).GetSingleton<DebugDisplayData>();
            Assert.AreEqual(5, debugData.VillagerCount);
        }

        [Test]
        public void DebugDisplaySystem_SumStorehouseTotals()
        {
            var handle = CreateDebugSystem();
            ref var systemState = ref _world.Unmanaged.ResolveSystemStateRef(handle);

            // Create test storehouses
            var storehouse1 = _entityManager.CreateEntity(typeof(StorehouseInventory));
            _entityManager.SetComponentData(storehouse1, new StorehouseInventory
            {
                TotalStored = 100f,
                TotalCapacity = 200f,
                ItemTypeCount = 2,
                IsShredding = 0,
                LastUpdateTick = 0
            });

            var storehouse2 = _entityManager.CreateEntity(typeof(StorehouseInventory));
            _entityManager.SetComponentData(storehouse2, new StorehouseInventory
            {
                TotalStored = 50f,
                TotalCapacity = 100f,
                ItemTypeCount = 1,
                IsShredding = 0,
                LastUpdateTick = 0
            });

            _debugSystem.OnUpdate(ref systemState);

            var debugData = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>()).GetSingleton<DebugDisplayData>();
            Assert.AreEqual(150f, debugData.TotalResourcesStored, 0.01f);
        }

        [Test]
        public void DebugDisplaySystem_HandlesEmptyEntityQuery()
        {
            var handle = CreateDebugSystem();
            ref var systemState = ref _world.Unmanaged.ResolveSystemStateRef(handle);

            _debugSystem.OnUpdate(ref systemState);

            var debugData = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>()).GetSingleton<DebugDisplayData>();
            Assert.AreEqual(0, debugData.VillagerCount);
            Assert.AreEqual(0f, debugData.TotalResourcesStored);
        }

        private void EnsureCoreSingletons()
        {
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);
        }

        private SystemHandle CreateDebugSystem()
        {
            var handle = _world.CreateSystem<DebugDisplaySystem>();
            ref var state = ref _world.Unmanaged.ResolveSystemStateRef(handle);
            _debugSystem.OnCreate(ref state);
            return handle;
        }
    }
}


