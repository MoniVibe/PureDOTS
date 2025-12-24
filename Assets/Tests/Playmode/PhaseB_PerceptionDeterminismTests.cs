using NUnit.Framework;
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
    /// Determinism tests for Phase B perception:
    /// - Deterministic behavior (same seed → same results)
    /// - Obstacle grid LOS determinism
    /// - Multi-cell sampling determinism
    /// - Rewind compatibility
    /// </summary>
    public class PhaseB_PerceptionDeterminismTests
    {
        private World _world;
        private World _previousWorld;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PhaseB_PerceptionDeterminismTests");
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
                Flags = SimulationFeatureFlags.PerceptionEnabled
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
        public void PerceptionDeterminism_SameScenario_SameResults()
        {
            // Create scenario
            var (sensorEntity, targetEntity) = CreatePerceptionScenario();

            // Run perception system
            var perceptionSystem = _world.GetOrCreateSystem<PerceptionUpdateSystem>();
            perceptionSystem.Update(_world.Unmanaged);

            // Capture first run results
            var perceivedBuffer1 = _entityManager.GetBuffer<PerceivedEntity>(sensorEntity);
            var results1 = new NativeList<PerceivedEntity>(Allocator.Temp);
            for (int i = 0; i < perceivedBuffer1.Length; i++)
            {
                results1.Add(perceivedBuffer1[i]);
            }

            // Reset and run again
            perceivedBuffer1.Clear();
            var timeState = GetSingleton<TimeState>();
            timeState.Tick = 1; // Reset tick
            SetSingleton(timeState);
            var perceptionState = _entityManager.GetComponentData<PerceptionState>(sensorEntity);
            perceptionState.LastUpdateTick = 0;
            perceptionState.PerceivedCount = 0;
            _entityManager.SetComponentData(sensorEntity, perceptionState);
            perceptionSystem.Update(_world.Unmanaged);

            // Capture second run results
            var perceivedBuffer2 = _entityManager.GetBuffer<PerceivedEntity>(sensorEntity);
            var results2 = new NativeList<PerceivedEntity>(Allocator.Temp);
            for (int i = 0; i < perceivedBuffer2.Length; i++)
            {
                results2.Add(perceivedBuffer2[i]);
            }

            // Verify identical results
            Assert.AreEqual(results1.Length, results2.Length, "Same number of perceived entities");
            for (int i = 0; i < results1.Length; i++)
            {
                Assert.AreEqual(results1[i].TargetEntity, results2[i].TargetEntity, "Same target entities");
                Assert.AreEqual(results1[i].DetectedChannels, results2[i].DetectedChannels, "Same detected channels");
                Assert.AreEqual(results1[i].Confidence, results2[i].Confidence, 1e-5f, "Same confidence");
            }

            results1.Dispose();
            results2.Dispose();
        }

        [Test]
        public void ObstacleGridLOS_Deterministic()
        {
            // Create spatial grid and obstacle grid
            var gridConfig = CreateSpatialGrid();
            var gridEntity = PhaseBTestUtilities.EnsureSpatialGrid(_entityManager, gridConfig);
            var obstacleConfig = new ObstacleGridConfig
            {
                CellSize = 1f,
                ObstacleThreshold = 0.5f,
                Enabled = 1
            };
            _entityManager.AddComponent<ObstacleGridConfig>(gridEntity);
            _entityManager.SetComponentData(gridEntity, obstacleConfig);

            var obstacleCells = _entityManager.AddBuffer<ObstacleGridCell>(gridEntity);
            for (int i = 0; i < gridConfig.CellCount; i++)
            {
                obstacleCells.Add(new ObstacleGridCell
                {
                    BlockingHeight = 0f,
                    LastUpdatedTick = 1
                });
            }

            // Test same start/end → same result
            var start = new float3(1f, 1f, 1f);
            var end = new float3(5f, 1f, 5f);

            var result1 = ObstacleGridUtilities.CheckLOS(start, end, gridConfig, obstacleConfig, obstacleCells);
            var result2 = ObstacleGridUtilities.CheckLOS(start, end, gridConfig, obstacleConfig, obstacleCells);
            var result3 = ObstacleGridUtilities.CheckLOS(start, end, gridConfig, obstacleConfig, obstacleCells);

            Assert.AreEqual(result1, result2, "Same LOS result on second call");
            Assert.AreEqual(result2, result3, "Same LOS result on third call");
        }

        [Test]
        public void MultiCellSampling_Deterministic()
        {
            // Create spatial grid and signal field
            var gridConfig = CreateSpatialGrid();
            var gridEntity = PhaseBTestUtilities.EnsureSpatialGrid(_entityManager, gridConfig);
            PhaseBTestUtilities.EnsureSignalField(_entityManager, gridEntity, gridConfig);

            // Create emitter at fixed position
            var emitterEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(emitterEntity, LocalTransform.FromPosition(new float3(2f, 0f, 2f)));
            _entityManager.AddComponent(emitterEntity, new SensorySignalEmitter
            {
                Channels = PerceptionChannel.Smell,
                SmellStrength = 1f,
                SoundStrength = 0f,
                EMStrength = 0f,
                IsActive = 1
            });
            _entityManager.AddComponent<SpatialIndexedTag>(emitterEntity);

            // Create sensor at fixed position
            var sensorEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(sensorEntity, LocalTransform.FromPosition(new float3(4f, 0f, 4f)));
            _entityManager.AddComponent(sensorEntity, new SenseCapability
            {
                EnabledChannels = PerceptionChannel.Smell,
                Range = 2f,
                FieldOfView = 360f,
                Acuity = 1f,
                UpdateInterval = 0f,
                MaxTrackedTargets = 8
            });
            _entityManager.AddComponent(sensorEntity, new SignalPerceptionState());
            _entityManager.AddComponent<SpatialIndexedTag>(sensorEntity);

            // Run spatial grid build
            var gridBuildSystem = _world.GetOrCreateSystem<PureDOTS.Systems.Spatial.SpatialGridInitialBuildSystem>();
            gridBuildSystem.Update(_world.Unmanaged);

            // Run signal field update → sampling
            var signalFieldSystem = _world.GetOrCreateSystem<PerceptionSignalFieldUpdateSystem>();
            var signalSamplingSystem = _world.GetOrCreateSystem<PerceptionSignalSamplingSystem>();

            signalFieldSystem.Update(_world.Unmanaged);
            signalSamplingSystem.Update(_world.Unmanaged);

            // Capture first run
            var signalState1 = _entityManager.GetComponentData<SignalPerceptionState>(sensorEntity);
            var smellLevel1 = signalState1.SmellLevel;

            // Reset and run again
            _entityManager.SetComponentData(sensorEntity, new SignalPerceptionState());
            signalFieldSystem.Update(_world.Unmanaged);
            signalSamplingSystem.Update(_world.Unmanaged);

            // Capture second run
            var signalState2 = _entityManager.GetComponentData<SignalPerceptionState>(sensorEntity);
            var smellLevel2 = signalState2.SmellLevel;

            // Verify same result
            Assert.AreEqual(smellLevel1, smellLevel2, 1e-5f, "Same signal level on second run");
        }

        [Test]
        public void PerceptionRewind_Compatibility()
        {
            // Create scenario
            var (sensorEntity, targetEntity) = CreatePerceptionScenario();

            // Advance to tick 100 and record perception state
            var timeState = GetSingleton<TimeState>();
            timeState.Tick = 100;
            SetSingleton(timeState);

            var perceptionSystem = _world.GetOrCreateSystem<PerceptionUpdateSystem>();
            perceptionSystem.Update(_world.Unmanaged);

            var perceivedBufferAt100 = _entityManager.GetBuffer<PerceivedEntity>(sensorEntity);
            var resultsAt100 = new NativeList<PerceivedEntity>(Allocator.Temp);
            for (int i = 0; i < perceivedBufferAt100.Length; i++)
            {
                resultsAt100.Add(perceivedBufferAt100[i]);
            }

            // Rewind to tick 50
            var rewindState = GetSingleton<RewindState>();
            rewindState.Mode = RewindMode.Rewind;
            rewindState.TargetTick = 50;
            SetSingleton(rewindState);

            timeState.Tick = 50;
            SetSingleton(timeState);

            // Clear perception state
            perceivedBufferAt100.Clear();
            var perceptionState = _entityManager.GetComponentData<PerceptionState>(sensorEntity);
            perceptionState.LastUpdateTick = 0;
            perceptionState.PerceivedCount = 0;
            _entityManager.SetComponentData(sensorEntity, perceptionState);

            // Advance back to tick 100
            rewindState.Mode = RewindMode.Record;
            SetSingleton(rewindState);

            for (int tick = 51; tick <= 100; tick++)
            {
                timeState.Tick = (uint)tick;
                SetSingleton(timeState);
                perceptionSystem.Update(_world.Unmanaged);
            }

            // Verify perception state matches original
            var perceivedBufferAfterRewind = _entityManager.GetBuffer<PerceivedEntity>(sensorEntity);
            Assert.AreEqual(resultsAt100.Length, perceivedBufferAfterRewind.Length, "Same number of perceived entities after rewind");

            for (int i = 0; i < resultsAt100.Length; i++)
            {
                bool found = false;
                for (int j = 0; j < perceivedBufferAfterRewind.Length; j++)
                {
                    if (perceivedBufferAfterRewind[j].TargetEntity == resultsAt100[i].TargetEntity)
                    {
                        found = true;
                        Assert.AreEqual(resultsAt100[i].DetectedChannels, perceivedBufferAfterRewind[j].DetectedChannels, "Same detected channels");
                        Assert.AreEqual(resultsAt100[i].Confidence, perceivedBufferAfterRewind[j].Confidence, 1e-5f, "Same confidence");
                        break;
                    }
                }
                Assert.IsTrue(found, $"Target {resultsAt100[i].TargetEntity} should be present after rewind");
            }

            resultsAt100.Dispose();
        }

        private (Entity sensorEntity, Entity targetEntity) CreatePerceptionScenario()
        {
            // Create spatial grid
            var gridConfig = CreateSpatialGrid();
            var gridEntity = PhaseBTestUtilities.EnsureSpatialGrid(_entityManager, gridConfig);

            // Create target entity
            var targetEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(targetEntity, LocalTransform.FromPosition(new float3(5f, 0f, 5f)));
            _entityManager.AddComponent(targetEntity, new SensorSignature { VisualSignature = 1f });
            _entityManager.AddComponent<SpatialIndexedTag>(targetEntity);

            // Create sensor entity
            var sensorEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(sensorEntity, LocalTransform.FromPositionRotation(
                new float3(3f, 0f, 3f),
                quaternion.LookRotation(math.normalize(new float3(5f, 0f, 5f) - new float3(3f, 0f, 3f)), math.up())));
            _entityManager.AddComponent(sensorEntity, new AIFidelityTier { Tier = AILODTier.Tier0_Full });
            _entityManager.AddComponent(sensorEntity, new SenseCapability
            {
                EnabledChannels = PerceptionChannel.Vision,
                Range = 10f,
                FieldOfView = 360f,
                Acuity = 1f,
                UpdateInterval = 0f,
                MaxTrackedTargets = 8
            });
            _entityManager.AddComponent(sensorEntity, new PerceptionState());
            _entityManager.AddBuffer<PerceivedEntity>(sensorEntity);
            _entityManager.AddComponent<SpatialIndexedTag>(sensorEntity);

            // Run spatial grid build
            var gridBuildSystem = _world.GetOrCreateSystem<PureDOTS.Systems.Spatial.SpatialGridInitialBuildSystem>();
            gridBuildSystem.Update(_world.Unmanaged);

            return (sensorEntity, targetEntity);
        }

        private SpatialGridConfig CreateSpatialGrid()
        {
            return new SpatialGridConfig
            {
                CellSize = 1f,
                WorldMin = new float3(0f, 0f, 0f),
                WorldMax = new float3(10f, 10f, 10f),
                CellCounts = new int3(10, 10, 10),
                HashSeed = 0,
                ProviderId = 0
            };
        }

        private Entity EnsureSingleton<T>(T data) where T : unmanaged, IComponentData
        {
            using var query = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<T>());
            Entity entity;
            if (query.IsEmptyIgnoreFilter)
            {
                entity = _entityManager.CreateEntity(typeof(T));
            }
            else
            {
                entity = query.GetSingletonEntity();
            }

            _entityManager.SetComponentData(entity, data);
            return entity;
        }

        private T GetSingleton<T>() where T : unmanaged, IComponentData
        {
            using var query = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<T>());
            var entity = query.GetSingletonEntity();
            return _entityManager.GetComponentData<T>(entity);
        }

        private void SetSingleton<T>(T data) where T : unmanaged, IComponentData
        {
            using var query = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<T>());
            var entity = query.GetSingletonEntity();
            _entityManager.SetComponentData(entity, data);
        }
    }
}



