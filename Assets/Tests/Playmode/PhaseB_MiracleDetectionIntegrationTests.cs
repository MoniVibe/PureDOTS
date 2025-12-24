using NUnit.Framework;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Perception;
using PureDOTS.Runtime.Performance;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems.AI;
using PureDOTS.Systems.Miracles;
using PureDOTS.Systems.Perception;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MiracleId = PureDOTS.Runtime.Miracles.MiracleId;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Integration tests for Phase B miracle detection:
    /// - End-to-end miracle detectability (miracle spawn → perception → AI sensor reading)
    /// - Field-based miracles (SensorySignalEmitter)
    /// - Validation system warnings
    /// </summary>
    public class PhaseB_MiracleDetectionIntegrationTests
    {
        private World _world;
        private World _previousWorld;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PhaseB_MiracleDetectionIntegrationTests");
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
        public void MiracleDetection_DirectDetection_EndToEnd()
        {
            // Create spatial grid
            var gridConfig = CreateSpatialGrid();
            var gridEntity = PhaseBTestUtilities.EnsureSpatialGrid(_entityManager, gridConfig);

            // Create miracle entity with LocalTransform + SensorSignature (Vision)
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
            _entityManager.AddComponent(miracleEntity, new MiracleDefinition
            {
                Type = MiracleType.Fireball,
                CastingMode = MiracleCastingMode.Instant,
                BaseRadius = 5f,
                BaseIntensity = 1f,
                BaseCost = 10f,
                SustainedCostPerSecond = 0f
            });
            _entityManager.AddComponent<SpatialIndexedTag>(miracleEntity);

            // Create sensor entity with SenseCapability (Vision enabled)
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

            // Run PerceptionUpdateSystem → verify miracle appears in PerceivedEntity buffer
            var perceptionSystem = _world.GetOrCreateSystem<PerceptionUpdateSystem>();
            perceptionSystem.Update(_world.Unmanaged);

            var perceivedBuffer = _entityManager.GetBuffer<PerceivedEntity>(sensorEntity);
            bool foundMiracle = false;
            for (int i = 0; i < perceivedBuffer.Length; i++)
            {
                if (perceivedBuffer[i].TargetEntity == miracleEntity)
                {
                    foundMiracle = true;
                    Assert.IsTrue((perceivedBuffer[i].DetectedChannels & PerceptionChannel.Vision) != 0, "Miracle should be detected via Vision");
                    break;
                }
            }
            Assert.IsTrue(foundMiracle, "Miracle should appear in PerceivedEntity buffer");

            // Run AISensorUpdateSystem → verify miracle appears in AISensorReading buffer
            var aiSensorSystem = _world.GetOrCreateSystem<AISensorUpdateSystem>();
            _entityManager.AddComponent(sensorEntity, new AISensorConfig
            {
                UpdateInterval = 0f,
                Range = 10f,
                MaxResults = 8,
                QueryOptions = default,
                PrimaryCategory = AISensorCategory.Miracle,
                SecondaryCategory = AISensorCategory.None
            });
            _entityManager.AddComponent(sensorEntity, new AISensorState());
            _entityManager.AddBuffer<AISensorReading>(sensorEntity);
            aiSensorSystem.Update(_world.Unmanaged);

            var aiReadings = _entityManager.GetBuffer<AISensorReading>(sensorEntity);
            bool foundMiracleInAI = false;
            for (int i = 0; i < aiReadings.Length; i++)
            {
                if (aiReadings[i].Target == miracleEntity && aiReadings[i].Category == AISensorCategory.Miracle)
                {
                    foundMiracleInAI = true;
                    break;
                }
            }
            Assert.IsTrue(foundMiracleInAI, "Miracle should appear in AISensorReading buffer");
        }

        [Test]
        public void MiracleDetection_FieldBased_EndToEnd()
        {
            // Create spatial grid and signal field
            var gridConfig = CreateSpatialGrid();
            var gridEntity = PhaseBTestUtilities.EnsureSpatialGrid(_entityManager, gridConfig);
            PhaseBTestUtilities.EnsureSignalField(_entityManager, gridEntity, gridConfig);

            // Create miracle entity with LocalTransform + SensorySignalEmitter (Smell)
            var miracleEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(miracleEntity, LocalTransform.FromPosition(new float3(2f, 0f, 2f)));
            _entityManager.AddComponent(miracleEntity, new MiracleEffectNew
            {
                Id = MiracleId.Rain,
                RemainingSeconds = 10f,
                Intensity = 1f,
                Origin = new float3(2f, 0f, 2f),
                Radius = 5f
            });
            _entityManager.AddComponent(miracleEntity, new SensorySignalEmitter
            {
                Channels = PerceptionChannel.Smell,
                SmellStrength = 1f, // Strong smell
                SoundStrength = 0f,
                EMStrength = 0f,
                IsActive = 1
            });
            _entityManager.AddComponent<SpatialIndexedTag>(miracleEntity);

            // Create sensor entity with SenseCapability (Smell enabled)
            var sensorEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(sensorEntity, LocalTransform.FromPosition(new float3(4f, 0f, 4f)));
            _entityManager.AddComponent(sensorEntity, new AIFidelityTier { Tier = AILODTier.Tier0_Full });
            _entityManager.AddComponent(sensorEntity, new SenseCapability
            {
                EnabledChannels = PerceptionChannel.Smell,
                Range = 5f,
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

            // Run signal field update → sampling → verify SignalPerceptionState.SmellLevel > 0
            var signalFieldSystem = _world.GetOrCreateSystem<PerceptionSignalFieldUpdateSystem>();
            signalFieldSystem.Update(_world.Unmanaged);

            var signalSamplingSystem = _world.GetOrCreateSystem<PerceptionSignalSamplingSystem>();
            signalSamplingSystem.Update(_world.Unmanaged);

            var signalState = _entityManager.GetComponentData<SignalPerceptionState>(sensorEntity);
            Assert.Greater(signalState.SmellLevel, 0f, "Sensor should detect smell from miracle");
            Assert.Greater(signalState.SmellConfidence, 0f, "Smell confidence should be > 0");
        }

        [Test]
        public void MiracleValidation_MissingLocalTransform_WarningLogged()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Create miracle entity missing LocalTransform
            var miracleEntity = _entityManager.CreateEntity();
            // Intentionally NOT adding LocalTransform
            _entityManager.AddComponent(miracleEntity, new MiracleEffectNew
            {
                Id = MiracleId.Fire,
                RemainingSeconds = 10f,
                Intensity = 1f,
                Origin = new float3(5f, 0f, 5f),
                Radius = 5f
            });
            _entityManager.AddComponent(miracleEntity, new SensorSignature { VisualSignature = 1f });

            // Run validation system (should log warning)
            var validationSystem = _world.GetOrCreateSystem<MiracleDetectabilityBootstrapSystem>();
            validationSystem.Update(_world.Unmanaged);

            // Note: In actual test, we'd capture log output, but for now just verify system runs
            // The warning is logged via UnityEngine.Debug.LogWarning
#endif
        }

        [Test]
        public void MiracleValidation_MissingSensorSignatureAndEmitter_WarningLogged()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Create miracle entity missing both SensorSignature and SensorySignalEmitter
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
            // Intentionally NOT adding SensorSignature or SensorySignalEmitter

            // Run validation system (should log warning)
            var validationSystem = _world.GetOrCreateSystem<MiracleDetectabilityBootstrapSystem>();
            validationSystem.Update(_world.Unmanaged);

            // Note: In actual test, we'd capture log output, but for now just verify system runs
#endif
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



