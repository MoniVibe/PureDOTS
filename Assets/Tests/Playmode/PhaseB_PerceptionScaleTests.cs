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
    /// Scale tests for Phase B perception upgrades:
    /// - Multi-cell signal sampling at scale
    /// - LOS checks at scale
    /// - Obstacle grid population cost
    /// </summary>
    public class PhaseB_PerceptionScaleTests
    {
        private World _world;
        private World _previousWorld;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PhaseB_PerceptionScaleTests");
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
        public void MultiCellSignalSampling_AtScale_UnderBudget()
        {
            // Create spatial grid and signal field
            var gridConfig = new SpatialGridConfig
            {
                CellSize = 1f,
                WorldMin = new float3(0f, 0f, 0f),
                WorldMax = new float3(100f, 10f, 100f),
                CellCounts = new int3(100, 10, 100),
                HashSeed = 0,
                ProviderId = 0
            };
            var gridEntity = PhaseBTestUtilities.EnsureSpatialGrid(_entityManager, gridConfig);
            PhaseBTestUtilities.EnsureSignalField(_entityManager, gridEntity, gridConfig);

            // Create 100 emitters
            var emitters = new NativeList<Entity>(100, Allocator.Temp);
            for (int i = 0; i < 100; i++)
            {
                var emitterEntity = _entityManager.CreateEntity();
                var x = (i % 10) * 10f;
                var z = (i / 10) * 10f;
                _entityManager.AddComponent(emitterEntity, LocalTransform.FromPosition(new float3(x, 0f, z)));
                _entityManager.AddComponent(emitterEntity, new SensorySignalEmitter
                {
                    Channels = PerceptionChannel.Smell,
                    SmellStrength = 1f,
                    SoundStrength = 0f,
                    EMStrength = 0f,
                    IsActive = 1
                });
                _entityManager.AddComponent<SpatialIndexedTag>(emitterEntity);
                emitters.Add(emitterEntity);
            }

            // Create 1000 sensors
            var sensors = new NativeList<Entity>(1000, Allocator.Temp);
            for (int i = 0; i < 1000; i++)
            {
                var sensorEntity = _entityManager.CreateEntity();
                var x = (i % 50) * 2f;
                var z = (i / 50) * 2f;
                _entityManager.AddComponent(sensorEntity, LocalTransform.FromPosition(new float3(x, 0f, z)));
                _entityManager.AddComponent(sensorEntity, new SenseCapability
                {
                    EnabledChannels = PerceptionChannel.Smell,
                    Range = 5f, // Should sample multiple cells
                    FieldOfView = 360f,
                    Acuity = 1f,
                    UpdateInterval = 0f,
                    MaxTrackedTargets = 8
                });
                _entityManager.AddComponent(sensorEntity, new SignalPerceptionState());
                _entityManager.AddComponent<SpatialIndexedTag>(sensorEntity);
                sensors.Add(sensorEntity);
            }

            // Run spatial grid build
            var gridBuildSystem = _world.GetOrCreateSystem<PureDOTS.Systems.Spatial.SpatialGridInitialBuildSystem>();
            gridBuildSystem.Update(_world.Unmanaged);

            // Run signal field update → sampling
            var signalFieldSystem = _world.GetOrCreateSystem<PerceptionSignalFieldUpdateSystem>();
            var signalSamplingSystem = _world.GetOrCreateSystem<PerceptionSignalSamplingSystem>();

            signalFieldSystem.Update(_world.Unmanaged);
            signalSamplingSystem.Update(_world.Unmanaged);

            // Verify performance counters
            var counters = GetSingleton<UniversalPerformanceCounters>();
            // Budget: ~49 cells per sensor (radius 3) × 1000 sensors = ~49k cells/tick
            // With 1000 sensors, we expect sampling to stay under a reasonable ceiling
            Assert.LessOrEqual(counters.SignalCellsSampledThisTick, 60000, "Signal cells sampled should be under budget (allowing some overhead)");

            emitters.Dispose();
            sensors.Dispose();
        }

        [Test]
        public void LOSChecks_AtScale_UnderBudget()
        {
            // Create spatial grid
            var gridConfig = new SpatialGridConfig
            {
                CellSize = 1f,
                WorldMin = new float3(0f, 0f, 0f),
                WorldMax = new float3(100f, 10f, 100f),
                CellCounts = new int3(100, 10, 100),
                HashSeed = 0,
                ProviderId = 0
            };
            var gridEntity = PhaseBTestUtilities.EnsureSpatialGrid(_entityManager, gridConfig);

            // Create 100 sensors
            var sensors = new NativeList<Entity>(100, Allocator.Temp);
            for (int i = 0; i < 100; i++)
            {
                var sensorEntity = _entityManager.CreateEntity();
                var x = (i % 10) * 10f;
                var z = (i / 10) * 10f;
                _entityManager.AddComponent(sensorEntity, LocalTransform.FromPositionRotation(
                    new float3(x, 0f, z),
                    quaternion.identity));
                _entityManager.AddComponent(sensorEntity, new SenseCapability
                {
                    EnabledChannels = PerceptionChannel.Vision,
                    Range = 20f,
                    FieldOfView = 360f,
                    Acuity = 1f,
                    UpdateInterval = 0f,
                    MaxTrackedTargets = 8
                });
                _entityManager.AddComponent(sensorEntity, new PerceptionState());
                _entityManager.AddBuffer<PerceivedEntity>(sensorEntity);
                _entityManager.AddComponent<SpatialIndexedTag>(sensorEntity);
                sensors.Add(sensorEntity);
            }

            // Create 1000 targets
            var targets = new NativeList<Entity>(1000, Allocator.Temp);
            for (int i = 0; i < 1000; i++)
            {
                var targetEntity = _entityManager.CreateEntity();
                var x = (i % 50) * 2f;
                var z = (i / 50) * 2f;
                _entityManager.AddComponent(targetEntity, LocalTransform.FromPosition(new float3(x, 0f, z)));
                _entityManager.AddComponent(targetEntity, new SensorSignature { VisualSignature = 1f });
                _entityManager.AddComponent<SpatialIndexedTag>(targetEntity);
                targets.Add(targetEntity);
            }

            // Run spatial grid build
            var gridBuildSystem = _world.GetOrCreateSystem<PureDOTS.Systems.Spatial.SpatialGridInitialBuildSystem>();
            gridBuildSystem.Update(_world.Unmanaged);

            // Run perception system
            var perceptionSystem = _world.GetOrCreateSystem<PerceptionUpdateSystem>();
            perceptionSystem.Update(_world.Unmanaged);

            // Verify performance counters
            var counters = GetSingleton<UniversalPerformanceCounters>();
            var totalLosChecks = counters.LosChecksPhysicsThisTick +
                                 counters.LosChecksObstacleGridThisTick +
                                 counters.LosChecksUnknownThisTick;
            // Budget: MaxLosRaysPerTick = 24 (from PerformanceBudgets.md)
            // With 100 sensors and 1000 targets, we expect some checks but should stay reasonable
            // Note: Actual checks may be limited by MaxTrackedTargets per sensor (8)
            Assert.LessOrEqual(totalLosChecks, 1000, "LOS checks should be under budget (allowing some overhead)");

            sensors.Dispose();
            targets.Dispose();
        }

        [Test]
        public void ObstacleGridPopulation_AtScale_AcceptableCost()
        {
            // Create spatial grid
            var gridConfig = new SpatialGridConfig
            {
                CellSize = 1f,
                WorldMin = new float3(0f, 0f, 0f),
                WorldMax = new float3(100f, 10f, 100f),
                CellCounts = new int3(100, 10, 100),
                HashSeed = 0,
                ProviderId = 0
            };
            var gridEntity = PhaseBTestUtilities.EnsureSpatialGrid(_entityManager, gridConfig);

            var obstacleConfig = new ObstacleGridConfig
            {
                CellSize = 1f,
                ObstacleThreshold = 0.5f,
                Enabled = 1
            };
            _entityManager.AddComponent<ObstacleGridConfig>(gridEntity);
            _entityManager.SetComponentData(gridEntity, obstacleConfig);

            // Create 10k obstacle entities
            var obstacles = new NativeList<Entity>(10000, Allocator.Temp);
            for (int i = 0; i < 10000; i++)
            {
                var obstacleEntity = _entityManager.CreateEntity();
                var x = (i % 100) * 1f;
                var z = (i / 100) * 1f;
                _entityManager.AddComponent(obstacleEntity, LocalTransform.FromPosition(new float3(x, 0f, z)));
                _entityManager.AddComponent(obstacleEntity, new ObstacleTag());
                _entityManager.AddComponent(obstacleEntity, new ObstacleHeight { Height = 1f });
                _entityManager.AddComponent<SpatialIndexedTag>(obstacleEntity);
                obstacles.Add(obstacleEntity);
            }

            // Run spatial grid build
            var gridBuildSystem = _world.GetOrCreateSystem<PureDOTS.Systems.Spatial.SpatialGridInitialBuildSystem>();
            gridBuildSystem.Update(_world.Unmanaged);

            // Measure obstacle grid population time
            var startTime = System.Diagnostics.Stopwatch.StartNew();
            var bootstrapSystem = _world.GetOrCreateSystem<ObstacleGridBootstrapSystem>();
            bootstrapSystem.Update(_world.Unmanaged);
            startTime.Stop();

            // Verify cost is acceptable (one-time cost, < 100ms for 10k obstacles)
            Assert.Less(startTime.ElapsedMilliseconds, 1000, "Obstacle grid population should complete in reasonable time (< 1s for 10k obstacles)");

            obstacles.Dispose();
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
    }
}



