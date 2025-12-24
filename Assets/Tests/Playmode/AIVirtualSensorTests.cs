using NUnit.Framework;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Perception;
using PureDOTS.Runtime.Performance;
using PureDOTS.Systems.AI;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests.Playmode
{
    public class AIVirtualSensorTests
    {
        private World _world;
        private World _previousWorld;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("AIVirtualSensorTests");
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

            EnsureSingleton(MindCadenceSettings.CreateDefault());
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
        public void AIVirtualSensorSystem_UpdatesNeedStateReadingsWithoutDuplication()
        {
            var entity = _entityManager.CreateEntity(typeof(VillagerNeedState));
            _entityManager.AddBuffer<AISensorReading>(entity);

            var readings = _entityManager.GetBuffer<AISensorReading>(entity);
            readings.Add(new AISensorReading
            {
                Target = Entity.Null,
                DistanceSq = 9f,
                NormalizedScore = 0.25f,
                CellId = 0,
                SpatialVersion = 0,
                Category = AISensorCategory.ResourceNode
            });

            _entityManager.SetComponentData(entity, new VillagerNeedState
            {
                HungerUrgency = 0.7f,
                RestUrgency = 0.2f,
                FaithUrgency = 0.1f,
                SafetyUrgency = 0.1f,
                SocialUrgency = 0.6f,
                WorkUrgency = 0.2f
            });

            RunSystem<AIVirtualSensorSystem>();

            readings = _entityManager.GetBuffer<AISensorReading>(entity);
            Assert.AreEqual(4, readings.Length);
            AssertVirtualReading(entity, readings[0], 0.7f);
            AssertVirtualReading(entity, readings[1], 0.2f);
            AssertVirtualReading(entity, readings[2], 0.6f);
            Assert.AreEqual(AISensorCategory.ResourceNode, readings[3].Category);

            _entityManager.SetComponentData(entity, new VillagerNeedState
            {
                HungerUrgency = 0.1f,
                RestUrgency = 0.4f,
                FaithUrgency = 0.8f,
                SafetyUrgency = 0.1f,
                SocialUrgency = 0.2f,
                WorkUrgency = 0.2f
            });

            RunSystem<AIVirtualSensorSystem>();

            readings = _entityManager.GetBuffer<AISensorReading>(entity);
            Assert.AreEqual(4, readings.Length);
            AssertVirtualReading(entity, readings[0], 0.1f);
            AssertVirtualReading(entity, readings[1], 0.4f);
            AssertVirtualReading(entity, readings[2], 0.8f);
        }

        [Test]
        public void AIVirtualSensorSystem_UsesLegacyNeedsWhenNeedStateMissing()
        {
            var entity = _entityManager.CreateEntity(typeof(VillagerNeeds), typeof(VillagerMood));
            _entityManager.AddBuffer<AISensorReading>(entity);

            var needs = new VillagerNeeds();
            needs.SetHunger(25f);
            needs.SetEnergy(50f);
            needs.SetMorale(40f);
            _entityManager.SetComponentData(entity, needs);

            _entityManager.SetComponentData(entity, new VillagerMood
            {
                Mood = 40f,
                TargetMood = 40f,
                MoodChangeRate = 1f,
                Wellbeing = 40f,
                Alignment = 50f,
                LastAlignmentInfluenceTick = 0
            });

            RunSystem<AIVirtualSensorSystem>();

            var readings = _entityManager.GetBuffer<AISensorReading>(entity);
            Assert.AreEqual(3, readings.Length);
            AssertVirtualReading(entity, readings[0], 0.75f);
            AssertVirtualReading(entity, readings[1], 0.5f);
            AssertVirtualReading(entity, readings[2], 0.6f);
        }

        [Test]
        public void AISensorUpdateSystem_DetectsMiracleCategory()
        {
            var agent = _entityManager.CreateEntity(typeof(AISensorConfig), typeof(AISensorState));
            _entityManager.AddBuffer<AISensorReading>(agent);
            var perceived = _entityManager.AddBuffer<PerceivedEntity>(agent);

            var miracle = _entityManager.CreateEntity(typeof(MiracleDefinition));
            _entityManager.SetComponentData(miracle, new MiracleDefinition
            {
                Type = MiracleType.Rain,
                CastingMode = MiracleCastingMode.Instant,
                BaseRadius = 2f,
                BaseIntensity = 1f,
                BaseCost = 1f,
                SustainedCostPerSecond = 0f
            });

            perceived.Add(new PerceivedEntity
            {
                TargetEntity = miracle,
                DetectedChannels = PerceptionChannel.Vision,
                Confidence = 1f,
                Distance = 2f,
                Direction = math.normalize(new float3(1f, 0f, 0f)),
                FirstDetectedTick = 1,
                LastSeenTick = 1,
                ThreatLevel = 0,
                Relationship = 0
            });

            _entityManager.SetComponentData(agent, new AISensorConfig
            {
                UpdateInterval = 0f,
                Range = 10f,
                MaxResults = 8,
                QueryOptions = default,
                PrimaryCategory = AISensorCategory.Miracle,
                SecondaryCategory = AISensorCategory.None
            });

            _entityManager.SetComponentData(agent, new AISensorState
            {
                Elapsed = 0f,
                LastSampleTick = 0
            });

            RunSystem<AISensorUpdateSystem>();

            var readings = _entityManager.GetBuffer<AISensorReading>(agent);
            Assert.AreEqual(1, readings.Length);
            Assert.AreEqual(AISensorCategory.Miracle, readings[0].Category);
            Assert.AreEqual(miracle, readings[0].Target);
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

        private static void AssertVirtualReading(Entity entity, in AISensorReading reading, float expectedScore)
        {
            Assert.AreEqual(entity, reading.Target);
            Assert.AreEqual(AISensorCategory.None, reading.Category);
            Assert.AreEqual(0f, reading.DistanceSq);
            Assert.AreEqual(expectedScore, reading.NormalizedScore, 1e-4f);
        }

        private void RunSystem<T>() where T : unmanaged, ISystem
        {
            var handle = _world.GetOrCreateSystem<T>();
            handle.Update(_world.Unmanaged);
        }
    }
}
