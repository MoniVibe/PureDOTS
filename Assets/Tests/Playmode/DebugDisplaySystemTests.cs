using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Streaming;
using PureDOTS.Runtime.Spatial;
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
                TargetTick = 50,
                TickDuration = 1f / 60f,
                MaxHistoryTicks = 600,
                PendingStepTicks = 0
            });
            _entityManager.AddComponentData(rewindEntity, new RewindLegacyState
            {
                PlaybackSpeed = 1f,
                CurrentTick = 0,
                StartTick = 10,
                PlaybackTick = 25,
                PlaybackTicksPerSecond = 90f,
                ScrubDirection = 1,
                ScrubSpeedMultiplier = 2f,
                RewindWindowTicks = 0,
                ActiveTrack = default
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

            var resourceRegistryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceRegistry>()).GetSingletonEntity();
            var metadata = _entityManager.GetComponentData<RegistryMetadata>(resourceRegistryEntity);
            metadata.MarkUpdated(12, 5, RegistryContinuitySnapshot.WithoutSpatialData());
            _entityManager.SetComponentData(resourceRegistryEntity, metadata);

            _entityManager.SetComponentData(resourceRegistryEntity, new RegistryHealth
            {
                HealthLevel = RegistryHealthLevel.Warning,
                StaleEntryCount = 3,
                StaleEntryRatio = 0.25f,
                SpatialVersionDelta = 0,
                TicksSinceLastUpdate = 7,
                DirectoryVersionDelta = 0,
                TotalEntryCount = 12,
                LastHealthCheckTick = 10,
                FailureFlags = RegistryHealthFlags.StaleEntriesWarning
            });

            _world.UpdateSystem<RegistryDirectorySystem>();
            _world.UpdateSystem<RegistryContinuityValidationSystem>();
            _world.UpdateSystem<RegistryInstrumentationSystem>();

            var registryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<RegistryDirectory>()).GetSingletonEntity();
            var directory = _entityManager.GetComponentData<RegistryDirectory>(registryEntity);

            _debugSystem.OnUpdate(ref systemState);

            var debugData = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>()).GetSingleton<DebugDisplayData>();
            Assert.AreEqual(1, debugData.RegisteredRegistryCount);
            Assert.AreEqual(12, debugData.RegisteredEntryCount);
            Assert.Greater(debugData.RegistryStateText.Length, 0);
            Assert.AreEqual(directory.Version, debugData.RegistryDirectoryVersion);
            Assert.AreEqual(0, debugData.RegistryHealthyCount);
            Assert.AreEqual(1, debugData.RegistryWarningCount);
            Assert.AreEqual(0, debugData.RegistryCriticalCount);
            Assert.AreEqual(0, debugData.RegistryFailureCount);
            Assert.GreaterOrEqual(debugData.RegistryInstrumentationVersion, 1u);
            Assert.AreEqual(0, debugData.RegistryContinuityWarningCount);
            Assert.AreEqual(0, debugData.RegistryContinuityFailureCount);
            Assert.AreEqual(0, debugData.RegistryContinuityAlerts.Length);
            Assert.Greater(debugData.RegistryHealthHeadline.Length, 0);
            Assert.IsTrue(debugData.RegistryHealthAlerts.ToString().Contains("Alerts:"));
        }

        [Test]
        public void DebugDisplaySystem_PopulatesStreamingDiagnostics()
        {
            var handle = CreateDebugSystem();
            ref var systemState = ref _world.Unmanaged.ResolveSystemStateRef(handle);

            Entity coordinatorEntity;
            using (var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<StreamingCoordinator>()))
            {
                if (query.IsEmptyIgnoreFilter)
                {
                    coordinatorEntity = _entityManager.CreateEntity(typeof(StreamingCoordinator));
                    _entityManager.AddBuffer<StreamingSectionCommand>(coordinatorEntity);
                    _entityManager.AddComponentData(coordinatorEntity, new StreamingDebugControl());
                    _entityManager.AddComponentData(coordinatorEntity, new StreamingStatistics
                    {
                        FirstLoadTick = StreamingStatistics.TickUnset,
                        FirstUnloadTick = StreamingStatistics.TickUnset
                    });
                }
                else
                {
                    coordinatorEntity = query.GetSingletonEntity();
                }
            }

            if (!_entityManager.HasComponent<StreamingStatistics>(coordinatorEntity))
            {
                _entityManager.AddComponentData(coordinatorEntity, new StreamingStatistics
                {
                    FirstLoadTick = StreamingStatistics.TickUnset,
                    FirstUnloadTick = StreamingStatistics.TickUnset
                });
            }

            _entityManager.SetComponentData(coordinatorEntity, new StreamingStatistics
            {
                DesiredCount = 4,
                LoadedCount = 3,
                LoadingCount = 1,
                UnloadingCount = 0,
                QueuedLoads = 2,
                QueuedUnloads = 1,
                PendingCommands = 3,
                PeakPendingCommands = 5,
                ActiveCooldowns = 2,
                FirstLoadTick = 12,
                FirstUnloadTick = StreamingStatistics.TickUnset
            });

            _debugSystem.OnUpdate(ref systemState);

            var debugData = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>()).GetSingleton<DebugDisplayData>();
            Assert.AreEqual(4, debugData.StreamingDesiredCount);
            Assert.AreEqual(3, debugData.StreamingLoadedCount);
            Assert.AreEqual(1, debugData.StreamingLoadingCount);
            Assert.AreEqual(2, debugData.StreamingQueuedLoads);
            Assert.AreEqual(1, debugData.StreamingQueuedUnloads);
            Assert.AreEqual(3, debugData.StreamingPendingCommands);
            Assert.AreEqual(2, debugData.StreamingActiveCooldowns);
            Assert.AreEqual(12u, debugData.StreamingFirstLoadTick);
            Assert.Greater(debugData.StreamingStateText.Length, 0);

            var telemetryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TelemetryStream>()).GetSingletonEntity();
            var buffer = _entityManager.GetBuffer<TelemetryMetric>(telemetryEntity);

            bool foundStreamingDesired = false;
            bool foundStreamingCooldown = false;
            for (int i = 0; i < buffer.Length; i++)
            {
                var metric = buffer[i];
                if (metric.Key.ToString() == "streaming.desired")
                {
                    foundStreamingDesired = true;
                    Assert.AreEqual(4f, metric.Value);
                }

                if (metric.Key.ToString() == "streaming.cooldowns.active")
                {
                    foundStreamingCooldown = true;
                    Assert.AreEqual(2f, metric.Value);
                }
            }

            Assert.IsTrue(foundStreamingDesired, "Streaming desired metric should be present.");
            Assert.IsTrue(foundStreamingCooldown, "Streaming cooldown metric should be present.");
        }

        [Test]
        public void DebugDisplaySystem_PopulatesSpatialDiagnostics()
        {
            var handle = CreateDebugSystem();
            ref var systemState = ref _world.Unmanaged.ResolveSystemStateRef(handle);

            var gridEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialGridConfig>()).GetSingletonEntity();
            var config = _entityManager.GetComponentData<SpatialGridConfig>(gridEntity);
            config.CellCounts = new int3(4, 1, 4);
            config.CellSize = 1f;
            _entityManager.SetComponentData(gridEntity, config);

            _entityManager.SetComponentData(gridEntity, new SpatialGridState
            {
                ActiveBufferIndex = 1,
                TotalEntries = 12,
                Version = 5,
                LastUpdateTick = 42,
                LastDirtyTick = 0,
                DirtyVersion = 0,
                DirtyAddCount = 0,
                DirtyUpdateCount = 0,
                DirtyRemoveCount = 0,
                LastRebuildMilliseconds = 0f,
                LastStrategy = SpatialGridRebuildStrategy.Full
            });

            _debugSystem.OnUpdate(ref systemState);

            var debugData = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>()).GetSingleton<DebugDisplayData>();
            Assert.AreEqual(16, debugData.SpatialCellCount);
            Assert.AreEqual(12, debugData.SpatialIndexedEntityCount);
            Assert.AreEqual(5u, debugData.SpatialVersion);
            Assert.AreEqual(42u, debugData.SpatialLastUpdateTick);
            Assert.AreEqual(SpatialGridRebuildStrategy.Full, debugData.SpatialLastStrategy);
            Assert.AreEqual(0f, debugData.SpatialLastRebuildMilliseconds);
            Assert.AreEqual(0, debugData.SpatialDirtyAddCount);
            Assert.AreEqual(0, debugData.SpatialDirtyUpdateCount);
            Assert.AreEqual(0, debugData.SpatialDirtyRemoveCount);
            Assert.AreEqual(0, debugData.ResourceSpatialFallback);
            Assert.AreEqual(0, debugData.ResourceSpatialUnmapped);
            Assert.AreEqual(0, debugData.StorehouseSpatialFallback);
            Assert.AreEqual(0, debugData.StorehouseSpatialUnmapped);
            Assert.IsTrue(debugData.SpatialStateText.ToString().Contains("Spatial Cells"));

            var telemetryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TelemetryStream>()).GetSingletonEntity();
            var buffer = _entityManager.GetBuffer<TelemetryMetric>(telemetryEntity);

            bool foundSpatialEntries = false;
            bool foundSpatialVersion = false;
            for (int i = 0; i < buffer.Length; i++)
            {
                var metric = buffer[i];
                if (metric.Key.ToString() == "spatial.entries")
                {
                    foundSpatialEntries = true;
                    Assert.AreEqual(12f, metric.Value);
                }

                if (metric.Key.ToString() == "spatial.version")
                {
                    foundSpatialVersion = true;
                    Assert.AreEqual(5f, metric.Value);
                }
            }

            Assert.IsTrue(foundSpatialEntries, "Spatial entries metric should be present.");
            Assert.IsTrue(foundSpatialVersion, "Spatial version metric should be present.");
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
        public void DebugDisplaySystem_RecordsSpatialMetricsAccurately()
        {
            var handle = CreateDebugSystem();
            ref var systemState = ref _world.Unmanaged.ResolveSystemStateRef(handle);

            var gridEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialGridConfig>()).GetSingletonEntity();
            var config = _entityManager.GetComponentData<SpatialGridConfig>(gridEntity);
            config.CellCounts = new int3(8, 1, 8);
            config.CellSize = 1f;
            _entityManager.SetComponentData(gridEntity, config);

            _entityManager.SetComponentData(gridEntity, new SpatialGridState
            {
                ActiveBufferIndex = 0,
                TotalEntries = 25,
                Version = 10,
                LastUpdateTick = 100,
                LastDirtyTick = 99,
                DirtyVersion = 5,
                DirtyAddCount = 3,
                DirtyUpdateCount = 7,
                DirtyRemoveCount = 2,
                LastRebuildMilliseconds = 1.234f,
                LastStrategy = SpatialGridRebuildStrategy.Partial
            });

            _debugSystem.OnUpdate(ref systemState);

            var debugData = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>()).GetSingleton<DebugDisplayData>();
            Assert.AreEqual(64, debugData.SpatialCellCount);
            Assert.AreEqual(25, debugData.SpatialIndexedEntityCount);
            Assert.AreEqual(10u, debugData.SpatialVersion);
            Assert.AreEqual(100u, debugData.SpatialLastUpdateTick);
            Assert.AreEqual(3, debugData.SpatialDirtyAddCount);
            Assert.AreEqual(7, debugData.SpatialDirtyUpdateCount);
            Assert.AreEqual(2, debugData.SpatialDirtyRemoveCount);
            Assert.AreEqual(1.234f, debugData.SpatialLastRebuildMilliseconds);
            Assert.AreEqual(SpatialGridRebuildStrategy.Partial, debugData.SpatialLastStrategy);
        }

        [Test]
        public void DebugDisplaySystem_TelemetryIncludesSpatialDirtyMetrics()
        {
            var handle = CreateDebugSystem();
            ref var systemState = ref _world.Unmanaged.ResolveSystemStateRef(handle);

            var gridEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialGridConfig>()).GetSingletonEntity();
            _entityManager.SetComponentData(gridEntity, new SpatialGridState
            {
                ActiveBufferIndex = 0,
                TotalEntries = 50,
                Version = 12,
                LastUpdateTick = 200,
                LastDirtyTick = 200,
                DirtyVersion = 8,
                DirtyAddCount = 5,
                DirtyUpdateCount = 10,
                DirtyRemoveCount = 3,
                LastRebuildMilliseconds = 2.5f,
                LastStrategy = SpatialGridRebuildStrategy.Partial
            });

            _debugSystem.OnUpdate(ref systemState);

            var telemetryEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TelemetryStream>()).GetSingletonEntity();
            var buffer = _entityManager.GetBuffer<TelemetryMetric>(telemetryEntity);

            bool foundSpatialEntries = false;
            bool foundSpatialVersion = false;

            for (int i = 0; i < buffer.Length; i++)
            {
                var metric = buffer[i];
                if (metric.Key.ToString() == "spatial.entries")
                {
                    foundSpatialEntries = true;
                    Assert.AreEqual(50f, metric.Value);
                }
                if (metric.Key.ToString() == "spatial.version")
                {
                    foundSpatialVersion = true;
                    Assert.AreEqual(12f, metric.Value);
                }
            }

            Assert.IsTrue(foundSpatialEntries, "Telemetry should include spatial.entries");
            Assert.IsTrue(foundSpatialVersion, "Telemetry should include spatial.version");
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
