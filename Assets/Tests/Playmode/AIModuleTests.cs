using NUnit.Framework;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems;
using PureDOTS.Systems.AI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Playmode
{
    public class AIModuleTests
    {
        private World _world;
        private World _previousWorld;
        private EntityManager _entityManager;
        private BlobAssetReference<AIUtilityArchetypeBlob> _utilityBlob;
        private Entity _timeEntity;

        [SetUp]
        public void SetUp()
        {
            _world = new World("AIModuleTestsWorld");
            _previousWorld = World.DefaultGameObjectInjectionWorld;
            World.DefaultGameObjectInjectionWorld = _world;
            _entityManager = _world.EntityManager;
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);

            // Ensure time/rewind defaults are writable for tests.
            _timeEntity = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<TimeState>()).GetSingletonEntity();
            _entityManager.SetComponentData(_timeEntity, new TimeState
            {
                FixedDeltaTime = 1f / 60f,
                CurrentSpeedMultiplier = 1f,
                Tick = 0,
                IsPaused = false
            });

            var rewindQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<RewindState>());
            var rewindEntity = rewindQuery.CalculateEntityCount() == 0
                ? _entityManager.CreateEntity(typeof(RewindState))
                : rewindQuery.GetSingletonEntity();
            var rewindState = _entityManager.GetComponentData<RewindState>(rewindEntity);
            rewindState.Mode = RewindMode.Record;
            _entityManager.SetComponentData(rewindEntity, rewindState);
        }

        [TearDown]
        public void TearDown()
        {
            if (_utilityBlob.IsCreated)
            {
                _utilityBlob.Dispose();
                _utilityBlob = default;
            }

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
        public void AISystems_ProduceDeterministicCommandQueue()
        {
            var gridEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialGridConfig>()).GetSingletonEntity();

            var gridConfig = new SpatialGridConfig
            {
                CellSize = 10f,
                WorldMin = new float3(-10f, -1f, -10f),
                WorldMax = new float3(10f, 1f, 10f),
                CellCounts = new int3(1, 1, 1),
                HashSeed = 0u,
                ProviderId = 0
            };
            _entityManager.SetComponentData(gridEntity, gridConfig);

            var ranges = _entityManager.GetBuffer<SpatialGridCellRange>(gridEntity);
            ranges.Clear();
            ranges.ResizeUninitialized(1);
            ranges[0] = new SpatialGridCellRange
            {
                StartIndex = 0,
                Count = 2
            };

            var entries = _entityManager.GetBuffer<SpatialGridEntry>(gridEntity);
            entries.Clear();

            // Create resource targets at different distances.
            var nearResource = CreateResourceTarget(new float3(0f, 0f, 0f), "Wood");
            var farResource = CreateResourceTarget(new float3(5f, 0f, 0f), "Stone");

            entries.Add(new SpatialGridEntry
            {
                Entity = nearResource,
                Position = new float3(0f, 0f, 0f),
                CellId = 0
            });
            entries.Add(new SpatialGridEntry
            {
                Entity = farResource,
                Position = new float3(5f, 0f, 0f),
                CellId = 0
            });

            var agent = CreateAgent(nearResource, farResource);

            var aiGroup = _world.GetOrCreateSystemManaged<AISystemGroup>();

            // First update populates sensor readings and scores.
            var timeState = _entityManager.GetComponentData<TimeState>(_timeEntity);
            timeState.Tick = 1;
            _entityManager.SetComponentData(_timeEntity, timeState);
            aiGroup.Update();

            // Second update validates determinism and command behaviour.
            timeState.Tick = 2;
            _entityManager.SetComponentData(_timeEntity, timeState);
            aiGroup.Update();

            var utilityState = _entityManager.GetComponentData<AIUtilityState>(agent);
            Assert.AreEqual(0, utilityState.BestActionIndex, "Expected nearest-target action to be selected.");
            Assert.Greater(utilityState.BestScore, 0f);

            var steeringState = _entityManager.GetComponentData<AISteeringState>(agent);
            Assert.Greater(math.lengthsq(steeringState.LinearVelocity), 0f, "Steering system should produce non-zero velocity toward target.");

            var commandQueueEntity = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<AICommandQueueTag>()).GetSingletonEntity();
            var commands = _entityManager.GetBuffer<AICommand>(commandQueueEntity);

            Assert.AreEqual(1, commands.Length);
            var command = commands[0];
            Assert.AreEqual(agent, command.Agent);
            Assert.AreEqual(utilityState.BestActionIndex, command.ActionIndex);
            Assert.AreNotEqual(Entity.Null, command.TargetEntity);
        }

        private Entity CreateResourceTarget(float3 position, FixedString64Bytes typeId)
        {
            var entity = _entityManager.CreateEntity();

            _entityManager.AddComponentData(entity, new ResourceTypeId { Value = typeId });
            _entityManager.AddComponentData(entity, new ResourceSourceConfig());
            _entityManager.AddComponentData(entity, new ResourceSourceState { UnitsRemaining = 100f });
            _entityManager.AddComponentData(entity, LocalTransform.FromPosition(position));

            return entity;
        }

        private Entity CreateAgent(Entity nearResource, Entity farResource)
        {
            var agent = _entityManager.CreateEntity(
                typeof(LocalTransform),
                typeof(AISensorConfig),
                typeof(AISensorState),
                typeof(AIUtilityState),
                typeof(AISteeringConfig),
                typeof(AISteeringState),
                typeof(AIBehaviourArchetype),
                typeof(AITargetState));

            _entityManager.SetComponentData(agent, LocalTransform.FromPosition(float3.zero));

            _entityManager.SetComponentData(agent, new AISensorConfig
            {
                UpdateInterval = 0f,
                Range = 20f,
                MaxResults = 2,
                QueryOptions = SpatialQueryOptions.IgnoreSelf | SpatialQueryOptions.RequireDeterministicSorting,
                PrimaryCategory = AISensorCategory.ResourceNode,
                SecondaryCategory = AISensorCategory.None
            });

            _entityManager.SetComponentData(agent, new AISensorState
            {
                Elapsed = 0f,
                LastSampleTick = 0
            });

            _entityManager.SetComponentData(agent, new AIUtilityState
            {
                BestActionIndex = 0,
                BestScore = 0f,
                LastEvaluationTick = 0
            });

            _entityManager.SetComponentData(agent, new AISteeringConfig
            {
                MaxSpeed = 4f,
                Acceleration = 8f,
                Responsiveness = 0.5f,
                DegreesOfFreedom = 2,
                ObstacleLookAhead = 0f
            });

            _entityManager.SetComponentData(agent, new AISteeringState
            {
                DesiredDirection = float3.zero,
                LinearVelocity = float3.zero,
                LastSampledTarget = float3.zero,
                LastUpdateTick = 0
            });

            _entityManager.SetComponentData(agent, new AITargetState
            {
                TargetEntity = Entity.Null,
                TargetPosition = float3.zero,
                ActionIndex = 0,
                Flags = 0
            });

            _entityManager.AddBuffer<AISensorReading>(agent);
            _entityManager.AddBuffer<AIActionState>(agent);

            _utilityBlob = CreateUtilityBlob();
            _entityManager.SetComponentData(agent, new AIBehaviourArchetype
            {
                UtilityBlob = _utilityBlob
            });

            return agent;
        }

        private static BlobAssetReference<AIUtilityArchetypeBlob> CreateUtilityBlob()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AIUtilityArchetypeBlob>();

            var actions = builder.Allocate(ref root.Actions, 2);

            // Action 0: prefers sensor reading 0 (nearest target).
            ref var action0 = ref actions[0];
            var factors0 = builder.Allocate(ref action0.Factors, 1);
            factors0[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 0,
                Threshold = 0f,
                Weight = 1f,
                ResponsePower = 1f,
                MaxValue = 1f
            };

            // Action 1: evaluates sensor reading 1 (farther target).
            ref var action1 = ref actions[1];
            var factors1 = builder.Allocate(ref action1.Factors, 1);
            factors1[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 1,
                Threshold = 0f,
                Weight = 1f,
                ResponsePower = 1f,
                MaxValue = 1f
            };

            var blob = builder.CreateBlobAssetReference<AIUtilityArchetypeBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }
    }
}
