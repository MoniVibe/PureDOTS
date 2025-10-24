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
    public class StorehouseRegistryTests
    {
        private World _world;
        private EntityManager _entityManager;
        private Entity _catalogEntity;
        private BlobAssetReference<ResourceTypeIndexBlob> _catalogBlob;

        [SetUp]
        public void SetUp()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            Assert.IsNotNull(_world, "DefaultGameObjectInjectionWorld must be created before running StorehouseRegistryTests.");
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
        public System.Collections.IEnumerator StorehouseRegistry_CreatesSingleton()
        {
            CreateResourceTypeCatalog("Wood");

            // Create storehouse
            var storehouseEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(storehouseEntity, new StorehouseConfig
            {
                ShredRate = 0f,
                MaxShredQueueSize = 0,
                InputRate = 10f,
                OutputRate = 10f
            });
            _entityManager.AddComponentData(storehouseEntity, new StorehouseInventory
            {
                TotalStored = 0f,
                TotalCapacity = 100f,
                ItemTypeCount = 0,
                IsShredding = 0,
                LastUpdateTick = 0
            });
            _entityManager.AddComponentData(storehouseEntity, new LocalTransform
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
            Assert.IsTrue(_entityManager.HasSingleton<StorehouseRegistry>());
            var registry = _entityManager.GetSingleton<StorehouseRegistry>();
            Assert.AreEqual(1, registry.TotalStorehouses);
            Assert.AreEqual(100f, registry.TotalCapacity);
            Assert.AreEqual(0f, registry.TotalStored);
        }

        [UnityTest]
        public System.Collections.IEnumerator StorehouseRegistry_UpdatesOnEntitySpawn()
        {
            CreateResourceTypeCatalog("Wood");

            // Setup singletons
            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(timeEntity, new TimeState { Tick = 0, IsPaused = false, FixedDeltaTime = 0.016f });

            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState { Mode = RewindMode.Record });

            // Create first storehouse
            var storehouse1 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(storehouse1, new StorehouseConfig());
            _entityManager.AddComponentData(storehouse1, new StorehouseInventory
            {
                TotalStored = 0f,
                TotalCapacity = 100f,
                ItemTypeCount = 0,
                IsShredding = 0,
                LastUpdateTick = 0
            });
            _entityManager.AddComponentData(storehouse1, new LocalTransform { Position = float3.zero, Rotation = quaternion.identity, Scale = 1f });

            yield return null;

            var registry1 = _entityManager.GetSingleton<StorehouseRegistry>();
            Assert.AreEqual(1, registry1.TotalStorehouses);
            Assert.AreEqual(100f, registry1.TotalCapacity);

            // Create second storehouse
            var storehouse2 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(storehouse2, new StorehouseConfig());
            _entityManager.AddComponentData(storehouse2, new StorehouseInventory
            {
                TotalStored = 50f,
                TotalCapacity = 200f,
                ItemTypeCount = 1,
                IsShredding = 0,
                LastUpdateTick = 0
            });
            _entityManager.AddComponentData(storehouse2, new LocalTransform { Position = new float3(10, 0, 0), Rotation = quaternion.identity, Scale = 1f });

            yield return null;

            var registry2 = _entityManager.GetSingleton<StorehouseRegistry>();
            Assert.AreEqual(2, registry2.TotalStorehouses);
            Assert.AreEqual(300f, registry2.TotalCapacity);
            Assert.AreEqual(50f, registry2.TotalStored);
        }

        [UnityTest]
        public System.Collections.IEnumerator StorehouseRegistry_TracksCapacity()
        {
            CreateResourceTypeCatalog("Wood");

            // Setup singletons
            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(timeEntity, new TimeState { Tick = 0, IsPaused = false, FixedDeltaTime = 0.016f });

            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState { Mode = RewindMode.Record });

            // Create storehouse with capacity
            var storehouseEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(storehouseEntity, new StorehouseConfig());
            _entityManager.AddComponentData(storehouseEntity, new StorehouseInventory
            {
                TotalStored = 30f,
                TotalCapacity = 100f,
                ItemTypeCount = 1,
                IsShredding = 0,
                LastUpdateTick = 0
            });
            _entityManager.AddComponentData(storehouseEntity, new LocalTransform { Position = float3.zero, Rotation = quaternion.identity, Scale = 1f });

            var capacityBuffer = _entityManager.AddBuffer<StorehouseCapacityElement>(storehouseEntity);
            capacityBuffer.Add(new StorehouseCapacityElement
            {
                ResourceTypeId = "Wood",
                MaxCapacity = 100f
            });

            var inventoryItems = _entityManager.AddBuffer<StorehouseInventoryItem>(storehouseEntity);
            inventoryItems.Add(new StorehouseInventoryItem
            {
                ResourceTypeId = "Wood",
                Amount = 30f,
                Reserved = 5f
            });

            yield return null;

            var registry = _entityManager.GetSingleton<StorehouseRegistry>();
            Assert.AreEqual(1, registry.TotalStorehouses);
            Assert.AreEqual(100f, registry.TotalCapacity);
            Assert.AreEqual(30f, registry.TotalStored);

            // Verify entries can be filtered for available capacity
            var registryEntity = _entityManager.GetSingletonEntity<StorehouseRegistry>();
            var entries = _entityManager.GetBuffer<StorehouseRegistryEntry>(registryEntity);
            Assert.Greater(entries.Length, 0);

            Assert.AreEqual(1, entries[0].TypeSummaries.Length);
            var resourceSummary = entries[0].TypeSummaries[0];
            Assert.AreEqual(100f, resourceSummary.Capacity);
            Assert.AreEqual(30f, resourceSummary.Stored);
            Assert.AreEqual(5f, resourceSummary.Reserved);
            var catalogEntity = _entityManager.GetSingletonEntity<ResourceTypeIndex>();
            var catalogData = _entityManager.GetComponentData<ResourceTypeIndex>(catalogEntity);
            var woodId = new FixedString64Bytes("Wood");
            Assert.AreEqual(catalogData.Catalog.Value.LookupIndex(woodId), resourceSummary.ResourceTypeIndex);

            // Filter for available capacity
            int availableCount = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].TotalStored < entries[i].TotalCapacity)
                {
                    availableCount++;
                }
            }
            Assert.AreEqual(1, availableCount);
        }

        [UnityTest]
        public System.Collections.IEnumerator StorehouseRegistry_UpdatesLastTick()
        {
            CreateResourceTypeCatalog("Wood");

            // Setup singletons
            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(timeEntity, new TimeState { Tick = 0, IsPaused = false, FixedDeltaTime = 0.016f });

            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState { Mode = RewindMode.Record });

            // Create storehouse
            var storehouseEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(storehouseEntity, new StorehouseConfig());
            _entityManager.AddComponentData(storehouseEntity, new StorehouseInventory
            {
                TotalStored = 0f,
                TotalCapacity = 100f,
                ItemTypeCount = 0,
                IsShredding = 0,
                LastUpdateTick = 0
            });
            _entityManager.AddComponentData(storehouseEntity, new LocalTransform { Position = float3.zero, Rotation = quaternion.identity, Scale = 1f });

            yield return null;

            var registry1 = _entityManager.GetSingleton<StorehouseRegistry>();
            Assert.AreEqual(0u, registry1.LastUpdateTick);

            // Advance time
            _entityManager.SetComponentData(timeEntity, new TimeState { Tick = 5, IsPaused = false, FixedDeltaTime = 0.016f });

            yield return null;

            var registry2 = _entityManager.GetSingleton<StorehouseRegistry>();
            Assert.AreEqual(5u, registry2.LastUpdateTick);
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
