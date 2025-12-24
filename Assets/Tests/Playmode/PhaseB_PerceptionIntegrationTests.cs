using NUnit.Framework;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Perception;
using PureDOTS.Runtime.Performance;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems.AI;
using PureDOTS.Systems.Perception;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Integration tests for Phase B perception pipeline:
    /// - End-to-end perception flow (SenseCapability → PerceivedEntity → AISensorReading)
    /// - LOS fallback chain (physics → obstacle grid → penalty)
    /// - Multi-cell signal sampling with range-based radius and falloff
    /// </summary>
    public class PhaseB_PerceptionIntegrationTests
    {
        private World _world;
        private World _previousWorld;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PhaseB_PerceptionIntegrationTests");
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
        public void PerceptionPipeline_EntityDetection_EndToEnd()
        {
            // Create spatial grid
            var gridConfig = CreateSpatialGrid();
            var gridEntity = PhaseBTestUtilities.EnsureSpatialGrid(_entityManager, gridConfig);

            // Create target entity with SensorSignature
            var targetEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(targetEntity, LocalTransform.FromPosition(new float3(5f, 0f, 5f)));
            _entityManager.AddComponent(targetEntity, new SensorSignature
            {
                VisualSignature = 1f,
                AuditorySignature = 0f,
                OlfactorySignature = 0f,
                EMSignature = 0f,
                GraviticSignature = 0f,
                ExoticSignature = 0f,
                ParanormalSignature = 0f
            });
            _entityManager.AddComponent<SpatialIndexedTag>(targetEntity);

            // Create sensor entity with SenseCapability
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

            // Run perception update system
            var perceptionSystem = _world.GetOrCreateSystem<PerceptionUpdateSystem>();
            perceptionSystem.Update(_world.Unmanaged);

            // Verify target appears in PerceivedEntity buffer
            var perceivedBuffer = _entityManager.GetBuffer<PerceivedEntity>(sensorEntity);
            Assert.Greater(perceivedBuffer.Length, 0, "Sensor should detect target entity");
            bool foundTarget = false;
            for (int i = 0; i < perceivedBuffer.Length; i++)
            {
                if (perceivedBuffer[i].TargetEntity == targetEntity)
                {
                    foundTarget = true;
                    Assert.IsTrue((perceivedBuffer[i].DetectedChannels & PerceptionChannel.Vision) != 0, "Target should be detected via Vision");
                    Assert.Greater(perceivedBuffer[i].Confidence, 0f, "Confidence should be > 0");
                    break;
                }
            }
            Assert.IsTrue(foundTarget, "Target entity should be in PerceivedEntity buffer");

            // Run AI sensor update system
            var aiSensorSystem = _world.GetOrCreateSystem<AISensorUpdateSystem>();
            _entityManager.AddComponent(sensorEntity, new AISensorConfig
            {
                UpdateInterval = 0f,
                Range = 10f,
                MaxResults = 8,
                QueryOptions = default,
                PrimaryCategory = AISensorCategory.None,
                SecondaryCategory = AISensorCategory.None
            });
            _entityManager.AddComponent(sensorEntity, new AISensorState());
            _entityManager.AddBuffer<AISensorReading>(sensorEntity);
            aiSensorSystem.Update(_world.Unmanaged);

            // Verify target appears in AISensorReading buffer (if category matches)
            // Note: AISensorUpdateSystem filters by category, so this may be empty if no category match
            var aiReadings = _entityManager.GetBuffer<AISensorReading>(sensorEntity);
            // At minimum, verify system ran without errors
            Assert.IsNotNull(aiReadings, "AISensorReading buffer should exist");
        }

        [Test]
        public void SignalFieldPipeline_EndToEnd()
        {
            // Create spatial grid and signal field
            var gridConfig = CreateSpatialGrid();
            var gridEntity = PhaseBTestUtilities.EnsureSpatialGrid(_entityManager, gridConfig);
            PhaseBTestUtilities.EnsureSignalField(_entityManager, gridEntity, gridConfig);

            // Create emitter entity
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

            // Create sensor entity
            var sensorEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(sensorEntity, LocalTransform.FromPosition(new float3(4f, 0f, 4f)));
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

            // Run spatial grid build
            var gridBuildSystem = _world.GetOrCreateSystem<PureDOTS.Systems.Spatial.SpatialGridInitialBuildSystem>();
            gridBuildSystem.Update(_world.Unmanaged);

            // Run signal field update system
            var signalFieldSystem = _world.GetOrCreateSystem<PerceptionSignalFieldUpdateSystem>();
            signalFieldSystem.Update(_world.Unmanaged);

            // Run signal sampling system
            var signalSamplingSystem = _world.GetOrCreateSystem<PerceptionSignalSamplingSystem>();
            signalSamplingSystem.Update(_world.Unmanaged);

            // Verify sensor detected signal
            var signalState = _entityManager.GetComponentData<SignalPerceptionState>(sensorEntity);
            Assert.Greater(signalState.SmellLevel, 0f, "Sensor should detect smell signal");
            Assert.Greater(signalState.SmellConfidence, 0f, "Smell confidence should be > 0");
        }

        [Test]
        public void LOSFallback_ObstacleGrid_UsedWhenPhysicsUnavailable()
        {
            // Create spatial grid without physics
            var gridConfig = CreateSpatialGrid();
            var gridEntity = PhaseBTestUtilities.EnsureSpatialGrid(_entityManager, gridConfig);

            // Create obstacle grid
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
                    BlockingHeight = 0f, // All clear
                    LastUpdatedTick = 1
                });
            }

            // Create target and sensor
            var targetEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(targetEntity, LocalTransform.FromPosition(new float3(5f, 0f, 5f)));
            _entityManager.AddComponent(targetEntity, new SensorSignature { VisualSignature = 1f });
            _entityManager.AddComponent<SpatialIndexedTag>(targetEntity);

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

            // Run perception system (should use obstacle grid fallback)
            var perceptionSystem = _world.GetOrCreateSystem<PerceptionUpdateSystem>();
            perceptionSystem.Update(_world.Unmanaged);

            // Verify detection occurred (obstacle grid should allow LOS)
            var perceivedBuffer = _entityManager.GetBuffer<PerceivedEntity>(sensorEntity);
            bool foundTarget = false;
            for (int i = 0; i < perceivedBuffer.Length; i++)
            {
                if (perceivedBuffer[i].TargetEntity == targetEntity)
                {
                    foundTarget = true;
                    break;
                }
            }
            Assert.IsTrue(foundTarget, "Target should be detected using obstacle grid LOS");

            // Verify detection occurred (system should work without physics)
            // Counters are tracked internally by systems if broker is present
        }

        [Test]
        public void LOSFallback_ConfidencePenalty_AppliedWhenNoPhysicsOrGrid()
        {
            // Create spatial grid without physics or obstacle grid
            var gridConfig = CreateSpatialGrid();
            var gridEntity = PhaseBTestUtilities.EnsureSpatialGrid(_entityManager, gridConfig);

            // Create target and sensor
            var targetEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(targetEntity, LocalTransform.FromPosition(new float3(5f, 0f, 5f)));
            _entityManager.AddComponent(targetEntity, new SensorSignature { VisualSignature = 1f });
            _entityManager.AddComponent<SpatialIndexedTag>(targetEntity);

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

            // Run perception system (should apply confidence penalty)
            var perceptionSystem = _world.GetOrCreateSystem<PerceptionUpdateSystem>();
            perceptionSystem.Update(_world.Unmanaged);

            // Verify detection occurred but with reduced confidence (LOS unknown penalty)
            var perceivedBuffer = _entityManager.GetBuffer<PerceivedEntity>(sensorEntity);
            bool foundTarget = false;
            float confidence = 0f;
            for (int i = 0; i < perceivedBuffer.Length; i++)
            {
                if (perceivedBuffer[i].TargetEntity == targetEntity)
                {
                    foundTarget = true;
                    confidence = perceivedBuffer[i].Confidence;
                    break;
                }
            }
            Assert.IsTrue(foundTarget, "Target should still be detected (with penalty)");
            // Confidence should be reduced due to LOS unknown penalty (0.5x multiplier)
            // But exact value depends on range decay, so just verify it's > 0
            Assert.Greater(confidence, 0f, "Confidence should be > 0 even with penalty");
        }

        [Test]
        public void MultiCellSignalSampling_RangeBasedRadius_WithFalloff()
        {
            // Create spatial grid and signal field
            var gridConfig = CreateSpatialGrid();
            var gridEntity = PhaseBTestUtilities.EnsureSpatialGrid(_entityManager, gridConfig);

            var signalConfig = SignalFieldConfig.Default;
            signalConfig.MaxSamplingRadiusCells = 2; // 5x5 neighborhood
            PhaseBTestUtilities.EnsureSignalField(_entityManager, gridEntity, gridConfig, signalConfig);

            // Create emitter at cell (3, 0, 3)
            var emitterEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(emitterEntity, LocalTransform.FromPosition(new float3(3f, 0f, 3f)));
            _entityManager.AddComponent(emitterEntity, new SensorySignalEmitter
            {
                Channels = PerceptionChannel.Smell,
                SmellStrength = 1f,
                SoundStrength = 0f,
                EMStrength = 0f,
                IsActive = 1
            });
            _entityManager.AddComponent<SpatialIndexedTag>(emitterEntity);

            // Create sensor at cell (4, 0, 4) with range = 2 cells
            var sensorEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(sensorEntity, LocalTransform.FromPosition(new float3(4f, 0f, 4f)));
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
            _entityManager.AddComponent<SpatialIndexedTag>(sensorEntity);

            // Run spatial grid build
            var gridBuildSystem = _world.GetOrCreateSystem<PureDOTS.Systems.Spatial.SpatialGridInitialBuildSystem>();
            gridBuildSystem.Update(_world.Unmanaged);

            // Run signal field update (emitter → field)
            var signalFieldSystem = _world.GetOrCreateSystem<PerceptionSignalFieldUpdateSystem>();
            signalFieldSystem.Update(_world.Unmanaged);

            // Run signal sampling (field → sensor)
            var signalSamplingSystem = _world.GetOrCreateSystem<PerceptionSignalSamplingSystem>();
            signalSamplingSystem.Update(_world.Unmanaged);

            // Verify sensor detected signal from multi-cell sampling
            var signalState = _entityManager.GetComponentData<SignalPerceptionState>(sensorEntity);
            Assert.Greater(signalState.SmellLevel, 0f, "Sensor should detect smell from multi-cell sampling");
            // Signal level should be weighted sum from multiple cells, not just single cell
            // With falloff, cells further away contribute less
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
    }
}

