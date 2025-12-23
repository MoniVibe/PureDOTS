using NUnit.Framework;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Perception;
using PureDOTS.Runtime.Social;
using PureDOTS.Runtime.Space;
using PureDOTS.Systems.AI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Playmode
{
    public class AIResourceContextScoringTests
    {
        private World _world;
        private World _previousWorld;
        private EntityManager _entityManager;
        private BlobAssetReference<ResourceTypeIndexBlob> _resourceCatalog;

        [SetUp]
        public void SetUp()
        {
            _world = new World("AIResourceContextScoringTests");
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
        }

        [TearDown]
        public void TearDown()
        {
            if (_resourceCatalog.IsCreated)
            {
                _resourceCatalog.Dispose();
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
        public void AIResourceContextScoringSystem_UsesHungerUrgencyForFoodNodes()
        {
            var agent = _entityManager.CreateEntity(typeof(VillagerNeedState));
            _entityManager.AddBuffer<AISensorReading>(agent);

            var resource = _entityManager.CreateEntity(typeof(ResourceTypeId));
            _entityManager.SetComponentData(resource, new ResourceTypeId { Value = new FixedString64Bytes("food") });

            _entityManager.SetComponentData(agent, new VillagerNeedState
            {
                HungerUrgency = 0.8f,
                RestUrgency = 0.1f,
                FaithUrgency = 0f,
                SafetyUrgency = 0f,
                SocialUrgency = 0f,
                WorkUrgency = 0f
            });

            var readings = _entityManager.GetBuffer<AISensorReading>(agent);
            readings.Add(new AISensorReading
            {
                Target = resource,
                DistanceSq = 0f,
                NormalizedScore = 1f,
                CellId = -1,
                SpatialVersion = 0,
                Category = AISensorCategory.ResourceNode
            });

            var catalogEntity = _entityManager.CreateEntity(typeof(ResourceValueCatalogTag));
            var catalogBuffer = _entityManager.AddBuffer<ResourceValueEntry>(catalogEntity);
            catalogBuffer.Add(new ResourceValueEntry
            {
                ResourceTypeId = new FixedString64Bytes("food"),
                BaseValue = 0.2f
            });

            RunSystem<AIResourceContextScoringSystem>();

            readings = _entityManager.GetBuffer<AISensorReading>(agent);
            Assert.AreEqual(0.8f, readings[0].NormalizedScore, 1e-4f);
        }

        [Test]
        public void AIResourceContextScoringSystem_AppliesInventoryPenalty()
        {
            var agent = _entityManager.CreateEntity(typeof(VillagerNeedState), typeof(VillagerInventoryRef));
            _entityManager.AddBuffer<AISensorReading>(agent);

            var inventoryEntity = _entityManager.CreateEntity();
            var inventoryBuffer = _entityManager.AddBuffer<VillagerInventoryItem>(inventoryEntity);
            inventoryBuffer.Add(new VillagerInventoryItem
            {
                ResourceTypeIndex = 0,
                Amount = 10f,
                MaxCarryCapacity = 10f
            });
            _entityManager.SetComponentData(agent, new VillagerInventoryRef
            {
                CompanionEntity = inventoryEntity
            });

            _entityManager.SetComponentData(agent, new VillagerNeedState
            {
                HungerUrgency = 1f,
                RestUrgency = 0f,
                FaithUrgency = 0f,
                SafetyUrgency = 0f,
                SocialUrgency = 0f,
                WorkUrgency = 0f
            });

            _resourceCatalog = CreateResourceCatalog("food");
            var catalogEntity = _entityManager.CreateEntity(typeof(ResourceTypeIndex));
            _entityManager.SetComponentData(catalogEntity, new ResourceTypeIndex { Catalog = _resourceCatalog });

            var resource = _entityManager.CreateEntity(typeof(ResourceTypeId));
            _entityManager.SetComponentData(resource, new ResourceTypeId { Value = new FixedString64Bytes("food") });

            var readings = _entityManager.GetBuffer<AISensorReading>(agent);
            readings.Add(new AISensorReading
            {
                Target = resource,
                DistanceSq = 0f,
                NormalizedScore = 1f,
                CellId = -1,
                SpatialVersion = 0,
                Category = AISensorCategory.ResourceNode
            });

            RunSystem<AIResourceContextScoringSystem>();

            readings = _entityManager.GetBuffer<AISensorReading>(agent);
            Assert.AreEqual(0f, readings[0].NormalizedScore, 1e-4f);
        }

        [Test]
        public void SensorUpdateSystem_PersonalRelationOverridesFaction()
        {
            var observer = _entityManager.CreateEntity(typeof(SensorConfig), typeof(SensorState), typeof(LocalTransform), typeof(VillagerId));
            _entityManager.AddBuffer<DetectedEntity>(observer);
            var relations = _entityManager.AddBuffer<EntityRelation>(observer);

            _entityManager.SetComponentData(observer, new VillagerId { Value = 1, FactionId = 0 });
            _entityManager.SetComponentData(observer, new SensorConfig
            {
                Range = 10f,
                FieldOfView = 360f,
                DetectionMask = DetectionMask.Sight,
                UpdateInterval = 0f,
                MaxTrackedTargets = 4,
                Flags = SensorCapabilityFlags.None
            });
            _entityManager.SetComponentData(observer, new SensorState { LastUpdateTick = 0 });
            _entityManager.SetComponentData(observer, LocalTransform.FromPosition(float3.zero));

            var target = _entityManager.CreateEntity(typeof(Detectable), typeof(LocalTransform), typeof(VillagerId));
            _entityManager.SetComponentData(target, new VillagerId { Value = 2, FactionId = 1 });
            _entityManager.SetComponentData(target, new Detectable
            {
                Visibility = 1f,
                Audibility = 1f,
                ThreatLevel = 0,
                Category = DetectableCategory.Enemy
            });
            _entityManager.SetComponentData(target, LocalTransform.FromPosition(new float3(1f, 0f, 0f)));

            relations.Add(new EntityRelation
            {
                OtherEntity = target,
                Type = RelationType.Ally,
                Intensity = 100
            });

            var factionEntity = _entityManager.CreateEntity(typeof(FactionRelationships));
            var factionRelations = new FactionRelationships
            {
                FactionId = 0
            };
            factionRelations.SetRelationship(1, -50);
            _entityManager.SetComponentData(factionEntity, factionRelations);

            RunSystem<SensorUpdateSystem>();

            var detections = _entityManager.GetBuffer<DetectedEntity>(observer);
            Assert.AreEqual(1, detections.Length);
            var detection = detections[0];
            Assert.AreEqual(100, detection.Relationship);
            Assert.AreEqual(PerceivedRelationKind.Ally, detection.RelationKind);
            Assert.IsTrue((detection.RelationFlags & PerceivedRelationFlags.ForcedAlly) != 0);
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

        private void RunSystem<T>() where T : unmanaged, ISystem
        {
            var handle = _world.GetOrCreateSystem<T>();
            handle.Update(_world.Unmanaged);
        }

        private static BlobAssetReference<ResourceTypeIndexBlob> CreateResourceCatalog(string resourceId)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ResourceTypeIndexBlob>();
            var idsBuilder = builder.Allocate(ref root.Ids, 1);
            var displayNamesBuilder = builder.Allocate(ref root.DisplayNames, 1);
            var colorsBuilder = builder.Allocate(ref root.Colors, 1);
            idsBuilder[0] = new FixedString64Bytes(resourceId);
            builder.AllocateString(ref displayNamesBuilder[0], resourceId);
            colorsBuilder[0] = new UnityEngine.Color32(255, 255, 255, 255);
            var blob = builder.CreateBlobAssetReference<ResourceTypeIndexBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }
    }
}
