using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.TestTools;

namespace PureDOTS.Tests.Playmode
{
    public class ResourceRegistryTests
    {
        private World _world;
        private EntityManager _entityManager;
        private Entity _catalogEntity;
        private BlobAssetReference<ResourceTypeIndexBlob> _catalogBlob;

        [SetUp]
        public void SetUp()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            Assert.IsNotNull(_world, "DefaultGameObjectInjectionWorld must be created before running ResourceRegistryTests.");
            _entityManager = _world.EntityManager;
            _catalogEntity = Entity.Null;
            _catalogBlob = default;
        }

        [TearDown]
        public void TearDown()
        {
            if (_catalogEntity != Entity.Null && _entityManager.Exists(_catalogEntity))
            {
                _entityManager.DestroyEntity(_catalogEntity);
                _catalogEntity = Entity.Null;
            }

            if (_catalogBlob.IsCreated)
            {
                _catalogBlob.Dispose();
                _catalogBlob = default;
            }
        }

        [UnityTest]
        public System.Collections.IEnumerator ResourceRegistry_CreatesSingleton()
        {
            // Create resource type catalog
            CreateResourceTypeCatalog("Wood");

            // Create a resource source
            var resourceEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(resourceEntity, new ResourceTypeId { Value = "Wood" });
            _entityManager.AddComponentData(resourceEntity, new ResourceSourceConfig
            {
                GatherRatePerWorker = 1f,
                MaxSimultaneousWorkers = 1,
                RespawnSeconds = 0f,
                Flags = 0
            });
            _entityManager.AddComponentData(resourceEntity, new ResourceSourceState { UnitsRemaining = 100f });
            _entityManager.AddComponentData(resourceEntity, new LocalTransform
            {
                Position = float3.zero,
                Rotation = quaternion.identity,
                Scale = 1f
            });

            // Create time/rewind singletons
            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(timeEntity, new TimeState { Tick = 0, IsPaused = false, FixedDeltaTime = 0.016f });

            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState { Mode = RewindMode.Record });

            yield return null;

            // Verify registry singleton exists
            Assert.IsTrue(_entityManager.HasSingleton<ResourceRegistry>());
            var registry = _entityManager.GetSingleton<ResourceRegistry>();
            Assert.AreEqual(1, registry.TotalResources);
            Assert.AreEqual(1, registry.TotalActiveResources);
        }

        [UnityTest]
        public System.Collections.IEnumerator ResourceRegistry_UpdatesOnEntitySpawn()
        {
            // Setup catalog and singletons
            CreateResourceTypeCatalog("Wood", "Stone");

            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(timeEntity, new TimeState { Tick = 0, IsPaused = false, FixedDeltaTime = 0.016f });

            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState { Mode = RewindMode.Record });

            // Create first resource
            var resource1 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(resource1, new ResourceTypeId { Value = "Wood" });
            _entityManager.AddComponentData(resource1, new ResourceSourceConfig());
            _entityManager.AddComponentData(resource1, new ResourceSourceState { UnitsRemaining = 50f });
            _entityManager.AddComponentData(resource1, new LocalTransform { Position = float3.zero, Rotation = quaternion.identity, Scale = 1f });

            yield return null;

            var registry1 = _entityManager.GetSingleton<ResourceRegistry>();
            Assert.AreEqual(1, registry1.TotalResources);

            // Create second resource
            var resource2 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(resource2, new ResourceTypeId { Value = "Stone" });
            _entityManager.AddComponentData(resource2, new ResourceSourceConfig());
            _entityManager.AddComponentData(resource2, new ResourceSourceState { UnitsRemaining = 75f });
            _entityManager.AddComponentData(resource2, new LocalTransform { Position = new float3(10, 0, 0), Rotation = quaternion.identity, Scale = 1f });

            yield return null;

            var registry2 = _entityManager.GetSingleton<ResourceRegistry>();
            Assert.AreEqual(2, registry2.TotalResources);
            Assert.AreEqual(2, registry2.TotalActiveResources);
        }

        [UnityTest]
        public System.Collections.IEnumerator ResourceRegistry_FiltersByType()
        {
            // Setup catalog
            CreateResourceTypeCatalog("Wood");

            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(timeEntity, new TimeState { Tick = 0, IsPaused = false, FixedDeltaTime = 0.016f });

            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState { Mode = RewindMode.Record });

            // Create resources
            var woodResource = _entityManager.CreateEntity();
            _entityManager.AddComponentData(woodResource, new ResourceTypeId { Value = "Wood" });
            _entityManager.AddComponentData(woodResource, new ResourceSourceConfig());
            _entityManager.AddComponentData(woodResource, new ResourceSourceState { UnitsRemaining = 100f });
            _entityManager.AddComponentData(woodResource, new LocalTransform { Position = float3.zero, Rotation = quaternion.identity, Scale = 1f });

            yield return null;

            // Verify entries can be filtered
            var registryEntity = _entityManager.GetSingletonEntity<ResourceRegistry>();
            var entries = _entityManager.GetBuffer<ResourceRegistryEntry>(registryEntity);
            Assert.Greater(entries.Length, 0);

            // Filter for wood type (index 0)
            int woodCount = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].ResourceTypeIndex == 0)
                {
                    woodCount++;
                }
            }
            Assert.AreEqual(1, woodCount);
        }

        [UnityTest]
        public System.Collections.IEnumerator ResourceRegistry_TracksActiveResources()
        {
            // Setup catalog
            CreateResourceTypeCatalog("Wood");

            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(timeEntity, new TimeState { Tick = 0, IsPaused = false, FixedDeltaTime = 0.016f });

            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState { Mode = RewindMode.Record });

            // Create active resource
            var activeResource = _entityManager.CreateEntity();
            _entityManager.AddComponentData(activeResource, new ResourceTypeId { Value = "Wood" });
            _entityManager.AddComponentData(activeResource, new ResourceSourceConfig());
            _entityManager.AddComponentData(activeResource, new ResourceSourceState { UnitsRemaining = 50f });
            _entityManager.AddComponentData(activeResource, new LocalTransform { Position = float3.zero, Rotation = quaternion.identity, Scale = 1f });

            // Create depleted resource
            var depletedResource = _entityManager.CreateEntity();
            _entityManager.AddComponentData(depletedResource, new ResourceTypeId { Value = "Wood" });
            _entityManager.AddComponentData(depletedResource, new ResourceSourceConfig());
            _entityManager.AddComponentData(depletedResource, new ResourceSourceState { UnitsRemaining = 0f });
            _entityManager.AddComponentData(depletedResource, new LocalTransform { Position = new float3(10, 0, 0), Rotation = quaternion.identity, Scale = 1f });

            yield return null;

            var registry = _entityManager.GetSingleton<ResourceRegistry>();
            Assert.AreEqual(2, registry.TotalResources);
            Assert.AreEqual(1, registry.TotalActiveResources);
        }

        private void CreateResourceTypeCatalog(params string[] ids)
        {
            if (_catalogEntity != Entity.Null && _entityManager.Exists(_catalogEntity))
            {
                _entityManager.DestroyEntity(_catalogEntity);
                _catalogEntity = Entity.Null;
            }

            if (_catalogBlob.IsCreated)
            {
                _catalogBlob.Dispose();
                _catalogBlob = default;
            }

            _catalogEntity = _entityManager.CreateEntity();
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ResourceTypeIndexBlob>();

            var length = ids?.Length ?? 0;
            var idsBuilder = builder.Allocate(ref root.Ids, length);
            var displayNamesBuilder = builder.Allocate(ref root.DisplayNames, length);
            var colorsBuilder = builder.Allocate(ref root.Colors, length);

            for (int i = 0; i < length; i++)
            {
                idsBuilder[i] = new FixedString64Bytes(ids[i]);
                builder.AllocateString(ref displayNamesBuilder[i], ids[i]);
                colorsBuilder[i] = new Color32(255, 255, 255, 255);
            }

            _catalogBlob = builder.CreateBlobAssetReference<ResourceTypeIndexBlob>(Allocator.Persistent);
            builder.Dispose();

            _entityManager.AddComponentData(_catalogEntity, new ResourceTypeIndex
            {
                Catalog = _catalogBlob
            });
        }
    }
}
