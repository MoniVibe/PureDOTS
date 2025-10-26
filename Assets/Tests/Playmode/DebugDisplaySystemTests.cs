using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Systems;
using Unity.Entities;
using Unity.Collections;
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
        public void DebugDisplaySystem_PopulatesRegistryDiagnostics()
        {
            var handle = CreateDebugSystem();
            ref var systemState = ref _world.Unmanaged.ResolveSystemStateRef(handle);

            var registryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistryDirectory>()).GetSingletonEntity();
            var entries = _entityManager.GetBuffer<RegistryDirectoryEntry>(registryEntity);
            entries.Clear();

            var resourceRegistryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceRegistry>()).GetSingletonEntity();
            var metadata = _entityManager.GetComponentData<RegistryMetadata>(resourceRegistryEntity);
            metadata.MarkUpdated(12, 5);
            _entityManager.SetComponentData(resourceRegistryEntity, metadata);

            entries.Add(new RegistryDirectoryEntry
            {
                Kind = RegistryKind.Resource,
                Handle = metadata.ToHandle(resourceRegistryEntity),
                Label = metadata.Label
            });

            var directory = _entityManager.GetComponentData<RegistryDirectory>(registryEntity);
            directory.MarkUpdated(metadata.LastUpdateTick, 1);
            _entityManager.SetComponentData(registryEntity, directory);

            _debugSystem.OnUpdate(ref systemState);

            var debugData = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>()).GetSingleton<DebugDisplayData>();
            Assert.AreEqual(1, debugData.RegisteredRegistryCount);
            Assert.AreEqual(12, debugData.RegisteredEntryCount);
            Assert.Greater(debugData.RegistryStateText.Length, 0);
            Assert.AreEqual(directory.Version, debugData.RegistryDirectoryVersion);
        }

        [Test]
        public void DebugDisplaySystem_WritesTelemetryMetrics()
        {
            var handle = CreateDebugSystem();
            ref var systemState = ref _world.Unmanaged.ResolveSystemStateRef(handle);

            var timeEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = _entityManager.GetComponentData<TimeState>(timeEntity);
            timeState.Tick = 123;
            _entityManager.SetComponentData(timeEntity, timeState);

            _debugSystem.OnUpdate(ref systemState);

            var telemetryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TelemetryStream>()).GetSingletonEntity();
            var telemetry = _entityManager.GetComponentData<TelemetryStream>(telemetryEntity);
            Assert.AreEqual(123u, telemetry.LastTick);
            Assert.Greater(telemetry.Version, 0u);

            var buffer = _entityManager.GetBuffer<TelemetryMetric>(telemetryEntity);
            Assert.IsTrue(buffer.Length > 0);

            bool foundTickMetric = false;
            for (int i = 0; i < buffer.Length; i++)
            {
                var metric = buffer[i];
                if (metric.Key.ToString() == "tick.current")
                {
                    foundTickMetric = true;
                    Assert.AreEqual(123f, metric.Value);
                    break;
                }
            }

            Assert.IsTrue(foundTickMetric, "Tick telemetry metric should be present.");
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

        [Test]
        public void DebugDisplaySystem_PresentsFrameTimingSummary()
        {
            var recorder = _world.CreateSystemManaged<FrameTimingRecorderSystem>();
            recorder.RecordGroupTiming(FrameTimingGroup.Environment, 1.2f, 2, false);
            recorder.Update();

            var handle = CreateDebugSystem();
            ref var systemState = ref _world.Unmanaged.ResolveSystemStateRef(handle);
            _debugSystem.OnUpdate(ref systemState);

            var debugData = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>()).GetSingleton<DebugDisplayData>();
            Assert.Greater(debugData.FrameTimingSampleCount, 0);
            Assert.IsTrue(debugData.FrameTimingText.ToString().Contains("Environment"));
        }

        [Test]
        public void DebugDisplaySystem_PresentsReplayDiagnostics()
        {
            var captureSystem = _world.CreateSystemManaged<ReplayCaptureSystem>();
            var label = new FixedString64Bytes("SpawnVillager");
            ReplayCaptureSystem.RecordEvent(_world, ReplayableEvent.EventType.Spawn, 5u, label, 1f);
            captureSystem.Update();

            var handle = CreateDebugSystem();
            ref var systemState = ref _world.Unmanaged.ResolveSystemStateRef(handle);
            _debugSystem.OnUpdate(ref systemState);

            var debugData = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>()).GetSingleton<DebugDisplayData>();
            Assert.AreEqual(1, debugData.ReplayEventCount);
            Assert.IsTrue(debugData.ReplayStateText.ToString().Contains("SpawnVillager"));
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


