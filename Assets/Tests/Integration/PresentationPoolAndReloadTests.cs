using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PureDOTS.Tests;

namespace PureDOTS.Tests.Integration
{
    public class PresentationPoolAndReloadTests
    {
        private World _world;
        private EntityManager _entityManager;
        private Entity _queueEntity;
        private BlobAssetReference<PresentationRegistryBlob> _registry;
        private Unity.Entities.Hash128 _testDescriptor;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PresentationPoolTests");
            _entityManager = _world.EntityManager;
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);
            CreateQueue();
            CreateStatsAndConfig();
            CreateRegistry();
        }

        [TearDown]
        public void TearDown()
        {
            if (_registry.IsCreated)
            {
                _registry.Dispose();
            }

            if (_world.IsCreated)
            {
                _world.Dispose();
            }
        }

        [Test]
        public void PresentationPoolStats_UpdateAfterSpawnAndRecycle()
        {
            var entityA = CreateTargetEntity(new float3(0f, 0f, 0f));
            var entityB = CreateTargetEntity(new float3(1f, 0f, 0f));

            QueueSpawn(entityA);
            QueueSpawn(entityB);

            _world.UpdateSystem<PresentationPoolStatsResetSystem>();
            _world.UpdateSystem<PresentationSpawnSystem>();

            var stats = _entityManager.CreateEntityQuery(typeof(PresentationPoolStats)).GetSingleton<PresentationPoolStats>();
            Assert.AreEqual(2u, stats.ActiveVisuals);
            Assert.AreEqual(2u, stats.SpawnedThisFrame);
            Assert.AreEqual(0u, stats.RecycledThisFrame);

            var recycleBuffer = _entityManager.GetBuffer<PresentationRecycleRequest>(_queueEntity);
            recycleBuffer.Add(new PresentationRecycleRequest { Target = entityA });
            recycleBuffer.Add(new PresentationRecycleRequest { Target = entityB });

            _world.UpdateSystem<PresentationRecycleSystem>();

            stats = _entityManager.CreateEntityQuery(typeof(PresentationPoolStats)).GetSingleton<PresentationPoolStats>();
            Assert.AreEqual(0u, stats.ActiveVisuals);
            Assert.AreEqual(2u, stats.RecycledThisFrame);
        }

        [Test]
        public void PresentationReloadSystem_QueuesSpawnRequests()
        {
            var entity = CreateTargetEntity(new float3(2f, 0f, 0f));
            QueueSpawn(entity);

            _world.UpdateSystem<PresentationPoolStatsResetSystem>();
            _world.UpdateSystem<PresentationSpawnSystem>();

            var handle = _entityManager.GetComponentData<PresentationHandle>(entity);
            Assert.AreNotEqual(Entity.Null, handle.Visual);
            var originalVisual = handle.Visual;

            _entityManager.AddComponentData(_queueEntity, new PresentationReloadCommand { RequestId = 1 });
            _world.UpdateSystem<PresentationReloadSystem>();

            handle = _entityManager.GetComponentData<PresentationHandle>(entity);
            Assert.AreEqual(Entity.Null, handle.Visual);
            Assert.IsFalse(_entityManager.Exists(originalVisual));

            var spawnBuffer = _entityManager.GetBuffer<PresentationSpawnRequest>(_queueEntity);
            Assert.AreEqual(1, spawnBuffer.Length);
            Assert.AreEqual(entity, spawnBuffer[0].Target);
            Assert.AreEqual(_testDescriptor, spawnBuffer[0].DescriptorHash);
        }

        [Test]
        public void PresentationHandleSyncSystem_BlendsTransforms()
        {
            var visual = _entityManager.CreateEntity(typeof(LocalTransform));
            _entityManager.SetComponentData(visual, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));

            var source = CreateTargetEntity(new float3(10f, 0f, 0f));
            _entityManager.AddComponentData(source, new PresentationHandle
            {
                Visual = visual,
                DescriptorHash = _testDescriptor,
                VariantSeed = 1
            });

            var configEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(configEntity, new PresentationHandleSyncConfig
            {
                PositionLerp = 0.5f,
                RotationLerp = 1f,
                ScaleLerp = 1f,
                VisualOffset = float3.zero
            });

            _world.UpdateSystem<PresentationHandleSyncSystem>();

            var synced = _entityManager.GetComponentData<LocalTransform>(visual);
            Assert.That(synced.Position.x, Is.EqualTo(5f).Within(0.01f));
        }

        [Test]
        public void PresentationPoolTelemetrySystem_WritesMetrics()
        {
            var telemetryEntity = _entityManager.CreateEntity(typeof(TelemetryStream));
            var telemetryBuffer = _entityManager.AddBuffer<TelemetryMetric>(telemetryEntity);
            telemetryBuffer.Clear();

            var statsEntity = _entityManager.CreateEntityQuery(typeof(PresentationPoolStats)).GetSingletonEntity();
            _entityManager.SetComponentData(statsEntity, new PresentationPoolStats
            {
                ActiveVisuals = 3,
                SpawnedThisFrame = 1,
                RecycledThisFrame = 2,
                TotalSpawned = 10,
                TotalRecycled = 5
            });

            _world.UpdateSystem<PresentationPoolTelemetrySystem>();

            telemetryBuffer = _entityManager.GetBuffer<TelemetryMetric>(telemetryEntity);
            Assert.IsTrue(ContainsMetric(telemetryBuffer, "presentation.pool.active", 3f));
            Assert.IsTrue(ContainsMetric(telemetryBuffer, "presentation.pool.recycledFrame", 2f));
            Assert.IsTrue(ContainsMetric(telemetryBuffer, "presentation.pool.totalSpawned", 10f));
        }

        private static bool ContainsMetric(DynamicBuffer<TelemetryMetric> buffer, string key, float expected)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Key.ToString() == key && math.abs(buffer[i].Value - expected) < 0.001f)
                {
                    return true;
                }
            }

            return false;
        }

        private Entity CreateTargetEntity(in float3 position)
        {
            var entity = _entityManager.CreateEntity(typeof(LocalTransform));
            _entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(position, quaternion.identity, 1f));
            return entity;
        }

        private void QueueSpawn(Entity target)
        {
            var buffer = _entityManager.GetBuffer<PresentationSpawnRequest>(_queueEntity);
            buffer.Add(new PresentationSpawnRequest
            {
                Target = target,
                DescriptorHash = _testDescriptor,
                Position = float3.zero,
                Rotation = quaternion.identity,
                ScaleMultiplier = 1f,
                Tint = float4.zero,
                VariantSeed = 1,
                Flags = PresentationSpawnFlags.AllowPooling
            });
        }

        private void CreateQueue()
        {
            _queueEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(_queueEntity, new PresentationCommandQueue());
            _entityManager.AddBuffer<PresentationSpawnRequest>(_queueEntity);
            _entityManager.AddBuffer<PresentationRecycleRequest>(_queueEntity);
        }

        private void CreateStatsAndConfig()
        {
            var statsEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(statsEntity, new PresentationPoolStats());

            var configEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(configEntity, PresentationHandleSyncConfig.Default);
        }

        private void CreateRegistry()
        {
            var prefab = _entityManager.CreateEntity(typeof(LocalTransform));
            _entityManager.SetComponentData(prefab, LocalTransform.Identity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<PresentationRegistryBlob>();
            var descriptors = builder.Allocate(ref root.Descriptors, 1);
            _testDescriptor = new Unity.Entities.Hash128(0x12345678, 0x9abcdef0, 0x0, 0x1);
            descriptors[0] = new PresentationDescriptor
            {
                KeyHash = _testDescriptor,
                Prefab = prefab,
                DefaultOffset = float3.zero,
                DefaultScale = 1f,
                DefaultTint = float4.zero,
                DefaultFlags = PresentationSpawnFlags.AllowPooling
            };

            _registry = builder.CreateBlobAssetReference<PresentationRegistryBlob>(Allocator.Persistent);
            builder.Dispose();

            var registryEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(registryEntity, new PresentationRegistryReference
            {
                Registry = _registry
            });
        }
    }
}
