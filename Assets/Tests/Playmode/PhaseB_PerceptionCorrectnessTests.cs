using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Miracles;
using PureDOTS.Runtime.Perception;
using PureDOTS.Runtime.Performance;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems.Perception;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Tests for Phase B perception correctness upgrades:
    /// - LOS fallback (physics → obstacle grid → penalty)
    /// - Multi-cell signal sampling with range-based radius and falloff
    /// - Miracle detectability contract
    /// </summary>
    public class PhaseB_PerceptionCorrectnessTests
    {
        private World _world;
        private World _previousWorld;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PhaseB_PerceptionCorrectnessTests");
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
        public void ObstacleGrid_CheckLOS_ReturnsTrueForClearPath()
        {
            // Create spatial grid
            var gridConfig = new SpatialGridConfig
            {
                CellSize = 1f,
                WorldMin = new float3(0f, 0f, 0f),
                WorldMax = new float3(10f, 10f, 10f),
                CellCounts = new int3(10, 10, 10),
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

            var obstacleCells = _entityManager.AddBuffer<ObstacleGridCell>(gridEntity);
            for (int i = 0; i < gridConfig.CellCount; i++)
            {
                obstacleCells.Add(new ObstacleGridCell
                {
                    BlockingHeight = 0f, // All cells clear
                    LastUpdatedTick = 1
                });
            }

            // Test clear LOS
            var start = new float3(1f, 1f, 1f);
            var end = new float3(5f, 1f, 5f);
            var hasLOS = ObstacleGridUtilities.CheckLOS(start, end, gridConfig, obstacleConfig, obstacleCells);
            Assert.IsTrue(hasLOS, "Clear path should have LOS");
        }

        [Test]
        public void ObstacleGrid_CheckLOS_ReturnsFalseForBlockedPath()
        {
            // Create spatial grid with obstacle
            var gridConfig = new SpatialGridConfig
            {
                CellSize = 1f,
                WorldMin = new float3(0f, 0f, 0f),
                WorldMax = new float3(10f, 10f, 10f),
                CellCounts = new int3(10, 10, 10),
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

            var obstacleCells = _entityManager.AddBuffer<ObstacleGridCell>(gridEntity);
            for (int i = 0; i < gridConfig.CellCount; i++)
            {
                obstacleCells.Add(new ObstacleGridCell
                {
                    BlockingHeight = 0f,
                    LastUpdatedTick = 1
                });
            }

            // Block cell at (3, 1, 3)
            SpatialHash.Quantize(new float3(3f, 1f, 3f), gridConfig, out var blockedCell);
            var blockedCellId = SpatialHash.Flatten(in blockedCell, in gridConfig);
            var blockedCellData = obstacleCells[blockedCellId];
            blockedCellData.BlockingHeight = 1f; // Above threshold
            obstacleCells[blockedCellId] = blockedCellData;

            // Test blocked LOS
            var start = new float3(1f, 1f, 1f);
            var end = new float3(5f, 1f, 5f); // Path goes through blocked cell
            var hasLOS = ObstacleGridUtilities.CheckLOS(start, end, gridConfig, obstacleConfig, obstacleCells);
            Assert.IsFalse(hasLOS, "Path through obstacle should be blocked");
        }

        [Test]
        public void SignalSampling_MultiCellSampling_UsesRangeBasedRadius()
        {
            // Create spatial grid and signal field
            var gridConfig = new SpatialGridConfig
            {
                CellSize = 1f,
                WorldMin = new float3(0f, 0f, 0f),
                WorldMax = new float3(10f, 10f, 10f),
                CellCounts = new int3(10, 10, 10),
                HashSeed = 0,
                ProviderId = 0
            };
            var gridEntity = PhaseBTestUtilities.EnsureSpatialGrid(_entityManager, gridConfig);

            var signalConfig = SignalFieldConfig.Default;
            PhaseBTestUtilities.EnsureSignalField(_entityManager, gridEntity, gridConfig, signalConfig);

            var signalCells = _entityManager.GetBuffer<SignalFieldCell>(gridEntity);
            for (int i = 0; i < signalCells.Length; i++)
            {
                signalCells[i] = new SignalFieldCell
                {
                    Smell = 0f,
                    Sound = 0f,
                    EM = 0f,
                    LastUpdatedTick = 1
                };
            }

            // Create emitter in cell (2, 0, 2)
            SpatialHash.Quantize(new float3(2f, 0f, 2f), gridConfig, out var emitterCell);
            var emitterCellId = SpatialHash.Flatten(in emitterCell, in gridConfig);
            var emitterCellData = signalCells[emitterCellId];
            emitterCellData.Smell = 1f; // Strong smell
            signalCells[emitterCellId] = emitterCellData;

            // Create sensor with range = 2 cells (should sample 5x5 neighborhood)
            var sensorEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(sensorEntity, LocalTransform.FromPosition(new float3(3f, 0f, 3f)));
            _entityManager.AddComponent(sensorEntity, new SenseCapability
            {
                EnabledChannels = PerceptionChannel.Smell,
                Range = 2f, // 2 cells
                FieldOfView = 360f,
                Acuity = 1f,
                UpdateInterval = 0f,
                MaxTrackedTargets = 8
            });
            _entityManager.AddComponent(sensorEntity, new SignalPerceptionState());

            // Run signal sampling system
            var samplingSystem = _world.GetOrCreateSystem<PerceptionSignalSamplingSystem>();
            samplingSystem.Update(_world.Unmanaged);

            // Verify sensor sampled multiple cells (not just single cell)
            var signalState = _entityManager.GetComponentData<SignalPerceptionState>(sensorEntity);
            Assert.Greater(signalState.SmellLevel, 0f, "Sensor should detect smell from multi-cell sampling");
        }

        [Test]
        public void MiracleDetectability_MiracleEntity_HasRequiredComponents()
        {
            // Create miracle effect entity with required components
            var miracleEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(miracleEntity, LocalTransform.FromPosition(new float3(5f, 0f, 5f)));
            _entityManager.AddComponent(miracleEntity, new MiracleEffectNew
            {
                Id = MiracleId.Fire,
                RemainingSeconds = 10f,
                Intensity = 1f,
                Origin = new float3(5f, 0f, 5f),
                Radius = 5f
            });
            _entityManager.AddComponent(miracleEntity, new SensorSignature
            {
                VisualSignature = 1f, // Visible miracle
                AuditorySignature = 0f,
                OlfactorySignature = 0f,
                EMSignature = 0f,
                GraviticSignature = 0f,
                ExoticSignature = 0f,
                ParanormalSignature = 0f
            });

            // Verify required components exist
            Assert.IsTrue(_entityManager.HasComponent<LocalTransform>(miracleEntity), "Miracle entity must have LocalTransform");
            Assert.IsTrue(_entityManager.HasComponent<SensorSignature>(miracleEntity), "Miracle entity must have SensorSignature or SensorySignalEmitter");
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
    }
}



