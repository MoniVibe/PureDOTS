using NUnit.Framework;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Perception;
using PureDOTS.Runtime.Performance;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems.Perception;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Determinism tests for sensor/comms scaling prototype:
    /// - Deterministic behavior (same seed → same results)
    /// - No global scans (only colored subset of cells processed per tick)
    /// - Bounded work per tick
    /// - Deterministic snapshot output
    /// </summary>
    public class SensorCommsScalingDeterminismTests
    {
        private World _world;
        private World _previousWorld;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("SensorCommsScalingDeterminismTests");
            _previousWorld = World.DefaultGameObjectInjectionWorld;
            World.DefaultGameObjectInjectionWorld = _world;
            _entityManager = _world.EntityManager;

            EnsureSingleton(new TimeState
            {
                Tick = 1,
                FixedDeltaTime = 1f / 60f,
                DeltaTime = 1f / 60f,
                DeltaSeconds = 1f / 60f,
                CurrentSpeedMultiplier = 1f,
                ElapsedTime = 1f / 60f,
                WorldSeconds = 1f / 60f,
                IsPaused = false
            });

            EnsureSingleton(new RewindState
            {
                Mode = RewindMode.Record,
                TargetTick = 0,
                TickDuration = 1f / 60f,
                MaxHistoryTicks = 256,
                PendingStepTicks = 0
            });

            EnsureSingleton(new SimulationFeatureFlags
            {
                Flags = SimulationFeatureFlags.PerceptionEnabled | SimulationFeatureFlags.SensorCommsScalingPrototype
            });

            EnsureSingleton(SimulationScalars.Default);
            EnsureSingleton(SimulationOverrides.Default);

            EnsureSingleton(UniversalPerformanceBudget.CreateDefaults());
            EnsureSingleton(new UniversalPerformanceCounters());
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null)
            {
                if (World.DefaultGameObjectInjectionWorld == _world)
                {
                    World.DefaultGameObjectInjectionWorld = _previousWorld;
                }

                _world.Dispose();
                _world = null;
            }
        }

        [Test]
        public void SensorCommsScaling_DeterministicSnapshots()
        {
            // Create deterministic scenario
            var (gridEntity, sensorEntities, targetEntities) = CreateDeterministicScenario(seed: 12345);

            // Run systems for several ticks
            var coloringSystem = _world.GetOrCreateSystem<SensorCellColoringSystem>();
            var eventEmitSystem = _world.GetOrCreateSystem<SensorEventEmitSystem>();
            var phaseSystem = _world.GetOrCreateSystem<AwarenessCellPhaseSystem>();
            var mergeSystem = _world.GetOrCreateSystem<AwarenessSnapshotMergeSystem>();

            // First run
            for (int tick = 0; tick < 10; tick++)
            {
                var timeState = GetSingleton<TimeState>();
                timeState.Tick = (uint)(tick + 1);
                SetSingleton(timeState);

                coloringSystem.Update(_world.Unmanaged);
                eventEmitSystem.Update(_world.Unmanaged);
                phaseSystem.Update(_world.Unmanaged);
                mergeSystem.Update(_world.Unmanaged);
            }

            // Capture first run results
            var snapshots1 = CaptureSnapshots(gridEntity);

            // Reset and run again with same seed
            var (gridEntity2, sensorEntities2, targetEntities2) = CreateDeterministicScenario(seed: 12345);

            var timeState2 = GetSingleton<TimeState>();
            timeState2.Tick = 1;
            SetSingleton(timeState2);

            for (int tick = 0; tick < 10; tick++)
            {
                var ts = GetSingleton<TimeState>();
                ts.Tick = (uint)(tick + 1);
                SetSingleton(ts);

                coloringSystem.Update(_world.Unmanaged);
                eventEmitSystem.Update(_world.Unmanaged);
                phaseSystem.Update(_world.Unmanaged);
                mergeSystem.Update(_world.Unmanaged);
            }

            // Capture second run results
            var snapshots2 = CaptureSnapshots(gridEntity2);

            // Verify identical results
            Assert.AreEqual(snapshots1.Count, snapshots2.Count, "Same number of snapshots");
            foreach (var kvp in snapshots1)
            {
                Assert.IsTrue(snapshots2.ContainsKey(kvp.Key), $"Snapshot for cell {kvp.Key} exists in second run");
                var snap1 = kvp.Value;
                var snap2 = snapshots2[kvp.Key];

                Assert.AreEqual(snap1.CellId, snap2.CellId, $"Cell ID matches for cell {kvp.Key}");
                Assert.AreEqual(snap1.EntityCount, snap2.EntityCount, $"Entity count matches for cell {kvp.Key}");
                Assert.AreEqual(snap1.HighestThreat, snap2.HighestThreat, $"Highest threat matches for cell {kvp.Key}");
                Assert.AreEqual(snap1.HostileEntityCount, snap2.HostileEntityCount, $"Hostile count matches for cell {kvp.Key}");
            }

            snapshots1.Clear();
            snapshots2.Clear();
        }

        [Test]
        public void SensorCommsScaling_BoundedWorkPerTick()
        {
            // Create scenario with many entities
            var (gridEntity, sensorEntities, targetEntities) = CreateDeterministicScenario(seed: 54321, entityCount: 1000);

            var gridConfig = _entityManager.GetComponentData<SpatialGridConfig>(gridEntity);
            var totalCells = gridConfig.CellCount;
            var colorCount = 4; // Default 4-color map
            var maxCellsPerTick = (totalCells / colorCount) + 10; // Allow small buffer

            var coloringSystem = _world.GetOrCreateSystem<SensorCellColoringSystem>();
            var eventEmitSystem = _world.GetOrCreateSystem<SensorEventEmitSystem>();
            var phaseSystem = _world.GetOrCreateSystem<AwarenessCellPhaseSystem>();
            var mergeSystem = _world.GetOrCreateSystem<AwarenessSnapshotMergeSystem>();

            // Run for several ticks and verify bounded work
            for (int tick = 0; tick < 20; tick++)
            {
                var timeState = GetSingleton<TimeState>();
                timeState.Tick = (uint)(tick + 1);
                SetSingleton(timeState);

                coloringSystem.Update(_world.Unmanaged);
                eventEmitSystem.Update(_world.Unmanaged);
                phaseSystem.Update(_world.Unmanaged);
                mergeSystem.Update(_world.Unmanaged);

                // Check telemetry
                var telemetryQuery = _entityManager.CreateEntityQuery(typeof(SensorCommsScalingTelemetry));
                if (!telemetryQuery.IsEmpty)
                {
                    var telemetry = _entityManager.GetComponentData<SensorCommsScalingTelemetry>(
                        telemetryQuery.GetSingletonEntity());
                    Assert.LessOrEqual(telemetry.CellsProcessedThisTick, maxCellsPerTick,
                        $"Cells processed per tick ({telemetry.CellsProcessedThisTick}) should be bounded by {maxCellsPerTick}");
                }
                telemetryQuery.Dispose();
            }
        }

        [Test]
        public void SensorCommsScaling_NoGlobalScans()
        {
            // Create scenario
            var (gridEntity, sensorEntities, targetEntities) = CreateDeterministicScenario(seed: 99999);

            var gridConfig = _entityManager.GetComponentData<SpatialGridConfig>(gridEntity);
            var totalCells = gridConfig.CellCount;
            var colorCount = 4;

            var coloringSystem = _world.GetOrCreateSystem<SensorCellColoringSystem>();
            var eventEmitSystem = _world.GetOrCreateSystem<SensorEventEmitSystem>();

            // Run for one tick
            coloringSystem.Update(_world.Unmanaged);
            eventEmitSystem.Update(_world.Unmanaged);

            // Verify only colored subset of cells processed
            // This is verified by checking that events are only emitted for current color
            if (_entityManager.HasBuffer<SensorCellEvent>(gridEntity))
            {
                var eventsBuffer = _entityManager.GetBuffer<SensorCellEvent>(gridEntity);
                var coloringState = _entityManager.GetComponentData<SensorCellColoringState>(gridEntity);
                var currentColor = coloringState.CurrentColor;

                // Count cells with events
                var cellsWithEvents = new NativeHashSet<int>(64, Allocator.Temp);
                for (int i = 0; i < eventsBuffer.Length; i++)
                {
                    cellsWithEvents.Add(eventsBuffer[i].CellId);
                }

                // Verify cells match current color
                foreach (var cellId in cellsWithEvents)
                {
                    SpatialHash.Unflatten(cellId, gridConfig, out var cellCoords);
                    var mortonKey = SpatialHash.MortonKey(cellCoords, gridConfig.HashSeed);
                    var cellColor = (byte)(mortonKey & 0x3);
                    Assert.AreEqual(currentColor, cellColor, $"Cell {cellId} should match current color {currentColor}");
                }

                // Verify bounded by color fraction
                var maxExpectedCells = (totalCells / colorCount) + 10;
                Assert.LessOrEqual(cellsWithEvents.Count, maxExpectedCells,
                    $"Cells with events ({cellsWithEvents.Count}) should be bounded by {maxExpectedCells}");

                cellsWithEvents.Dispose();
            }
        }

        private (Entity gridEntity, NativeList<Entity> sensors, NativeList<Entity> targets) CreateDeterministicScenario(int seed, int entityCount = 100)
        {
            var random = new Unity.Mathematics.Random((uint)seed);

            // Create spatial grid
            var gridConfig = new SpatialGridConfig
            {
                CellSize = 10f,
                WorldMin = new float3(-100f, -10f, -100f),
                WorldMax = new float3(100f, 10f, 100f),
                CellCounts = new int3(20, 1, 20),
                HashSeed = (uint)seed,
                ProviderId = 0
            };

            var gridEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(gridEntity, gridConfig);
            _entityManager.AddComponentData(gridEntity, new SpatialGridState
            {
                ActiveBufferIndex = 0,
                TotalEntries = 0,
                Version = 1,
                LastUpdateTick = 0,
                LastDirtyTick = 0,
                DirtyVersion = 0,
                DirtyAddCount = 0,
                DirtyUpdateCount = 0,
                DirtyRemoveCount = 0,
                LastRebuildMilliseconds = 0f,
                LastStrategy = SpatialGridRebuildStrategy.None
            });

            _entityManager.AddBuffer<SpatialGridCellRange>(gridEntity);
            _entityManager.AddBuffer<SpatialGridEntry>(gridEntity);

            var sensors = new NativeList<Entity>(entityCount / 2, Allocator.Temp);
            var targets = new NativeList<Entity>(entityCount / 2, Allocator.Temp);

            // Create sensor entities
            for (int i = 0; i < entityCount / 2; i++)
            {
                var pos = new float3(
                    random.NextFloat(-80f, 80f),
                    0f,
                    random.NextFloat(-80f, 80f));

                var sensorEntity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(sensorEntity, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, 1f));
                _entityManager.AddComponentData(sensorEntity, new SenseCapability
                {
                    EnabledChannels = PerceptionChannel.EM | PerceptionChannel.Vision,
                    Range = 50f,
                    FieldOfView = 360f,
                    Acuity = 1f,
                    UpdateInterval = 0.1f,
                    MaxTrackedTargets = 16,
                    Flags = 0
                });
                _entityManager.AddComponentData(sensorEntity, new SensorSignature
                {
                    VisualSignature = 0.5f,
                    EMSignature = 0.7f
                });
                _entityManager.AddComponentData(sensorEntity, new SpatialIndexedTag());
                _entityManager.AddComponentData(sensorEntity, new SpatialGridResidency
                {
                    CellId = 0,
                    LastPosition = pos,
                    Version = 1
                });
                _entityManager.AddBuffer<PerceivedEntity>(sensorEntity);
                _entityManager.AddComponentData(sensorEntity, new PerceptionState());

                sensors.Add(sensorEntity);
            }

            // Create target entities
            for (int i = 0; i < entityCount / 2; i++)
            {
                var pos = new float3(
                    random.NextFloat(-80f, 80f),
                    0f,
                    random.NextFloat(-80f, 80f));

                var targetEntity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(targetEntity, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, 1f));
                _entityManager.AddComponentData(targetEntity, new SensorSignature
                {
                    VisualSignature = 0.8f,
                    EMSignature = 0.9f
                });
                _entityManager.AddComponentData(targetEntity, new Detectable
                {
                    Visibility = 0.8f,
                    ThreatLevel = (byte)random.NextInt(0, 10),
                    Category = DetectableCategory.Neutral
                });
                _entityManager.AddComponentData(targetEntity, new SpatialIndexedTag());
                _entityManager.AddComponentData(targetEntity, new SpatialGridResidency
                {
                    CellId = 0,
                    LastPosition = pos,
                    Version = 1
                });

                targets.Add(targetEntity);
            }

            return (gridEntity, sensors, targets);
        }

        private NativeHashMap<int, AwarenessCellSnapshot> CaptureSnapshots(Entity gridEntity)
        {
            var snapshots = new NativeHashMap<int, AwarenessCellSnapshot>(64, Allocator.Temp);

            if (_entityManager.HasBuffer<AwarenessCellSnapshotBuffer>(gridEntity))
            {
                var buffer = _entityManager.GetBuffer<AwarenessCellSnapshotBuffer>(gridEntity);
                for (int i = 0; i < buffer.Length; i++)
                {
                    var snapshot = buffer[i].Snapshot;
                    snapshots[snapshot.CellId] = snapshot;
                }
            }

            return snapshots;
        }

        private void EnsureSingleton<T>(T component) where T : unmanaged, IComponentData
        {
            var query = _entityManager.CreateEntityQuery(typeof(T));
            if (query.IsEmpty)
            {
                var entity = _entityManager.CreateEntity(typeof(T));
                _entityManager.SetComponentData(entity, component);
            }
            else
            {
                _entityManager.SetComponentData(query.GetSingletonEntity(), component);
            }

            query.Dispose();
        }

        private T GetSingleton<T>() where T : unmanaged, IComponentData
        {
            var query = _entityManager.CreateEntityQuery(typeof(T));
            var result = _entityManager.GetComponentData<T>(query.GetSingletonEntity());
            query.Dispose();
            return result;
        }

        private void SetSingleton<T>(T component) where T : unmanaged, IComponentData
        {
            var query = _entityManager.CreateEntityQuery(typeof(T));
            _entityManager.SetComponentData(query.GetSingletonEntity(), component);
            query.Dispose();
        }
    }
}
