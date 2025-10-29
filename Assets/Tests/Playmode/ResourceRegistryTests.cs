using System.Text.RegularExpressions;
using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Spatial;
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
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);
            _catalogEntity = Entity.Null;
            _catalogBlob = default;
            ConfigureSpatialGrid(42u);
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
            _entityManager.AddComponent<SpatialIndexedTag>(resourceEntity);

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
            Assert.AreEqual(42u, registry.LastSpatialVersion);
            Assert.AreEqual(1, registry.SpatialResolvedCount);
            Assert.AreEqual(0, registry.SpatialFallbackCount);
            Assert.AreEqual(0, registry.SpatialUnmappedCount);

            var registryEntity = _entityManager.GetSingletonEntity<ResourceRegistry>();
            var metadata = _entityManager.GetComponentData<RegistryMetadata>(registryEntity);
            Assert.AreEqual(1, metadata.EntryCount);
            Assert.Greater(metadata.Version, 0u);
            Assert.IsTrue(metadata.Continuity.HasSpatialData, "Metadata continuity snapshot should record spatial data.");
            Assert.AreEqual(42u, metadata.Continuity.SpatialVersion);
            Assert.AreEqual(1, metadata.Continuity.SpatialResolvedCount);
            Assert.AreEqual(0, metadata.Continuity.SpatialFallbackCount);
            Assert.AreEqual(0, metadata.Continuity.SpatialUnmappedCount);

            var entries = _entityManager.GetBuffer<ResourceRegistryEntry>(registryEntity);
            Assert.AreEqual(1, entries.Length);
            Assert.AreEqual(0, entries[0].CellId, "Resource registry entry should record spatial cell id.");
            Assert.AreEqual(42u, entries[0].SpatialVersion, "Resource registry entry should mirror spatial grid version.");
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
            _entityManager.AddComponent<SpatialIndexedTag>(resource1);

            yield return null;

            var registry1 = _entityManager.GetSingleton<ResourceRegistry>();
            Assert.AreEqual(1, registry1.TotalResources);

            // Create second resource
            var resource2 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(resource2, new ResourceTypeId { Value = "Stone" });
            _entityManager.AddComponentData(resource2, new ResourceSourceConfig());
            _entityManager.AddComponentData(resource2, new ResourceSourceState { UnitsRemaining = 75f });
            _entityManager.AddComponentData(resource2, new LocalTransform { Position = new float3(10, 0, 0), Rotation = quaternion.identity, Scale = 1f });
            _entityManager.AddComponent<SpatialIndexedTag>(resource2);

            yield return null;

            var registry2 = _entityManager.GetSingleton<ResourceRegistry>();
            Assert.AreEqual(2, registry2.TotalResources);
            Assert.AreEqual(2, registry2.TotalActiveResources);
            Assert.AreEqual(2, registry2.SpatialResolvedCount);
            Assert.AreEqual(0, registry2.SpatialFallbackCount);
            Assert.AreEqual(0, registry2.SpatialUnmappedCount);
        }

        [UnityTest]
        public System.Collections.IEnumerator ResourceRegistry_SkipsUpdateDuringPlayback()
        {
            CreateResourceTypeCatalog("Wood");

            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(timeEntity, new TimeState { Tick = 0, IsPaused = false, FixedDeltaTime = 0.016f });

            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState { Mode = RewindMode.Record });

            var resource = _entityManager.CreateEntity();
            _entityManager.AddComponentData(resource, new ResourceTypeId { Value = "Wood" });
            _entityManager.AddComponentData(resource, new ResourceSourceConfig());
            _entityManager.AddComponentData(resource, new ResourceSourceState { UnitsRemaining = 10f });
            _entityManager.AddComponentData(resource, new LocalTransform { Position = float3.zero, Rotation = quaternion.identity, Scale = 1f });
            _entityManager.AddComponent<SpatialIndexedTag>(resource);

            yield return null;

            var registryEntity = _entityManager.GetSingletonEntity<ResourceRegistry>();
            var metadataBefore = _entityManager.GetComponentData<RegistryMetadata>(registryEntity);

            _entityManager.SetComponentData(rewindEntity, new RewindState { Mode = RewindMode.Playback });
            _entityManager.SetComponentData(timeEntity, new TimeState { Tick = 5, IsPaused = false, FixedDeltaTime = 0.016f });

            yield return null;

            var metadataAfter = _entityManager.GetComponentData<RegistryMetadata>(registryEntity);
            Assert.AreEqual(metadataBefore.Version, metadataAfter.Version);
            Assert.AreEqual(metadataBefore.EntryCount, metadataAfter.EntryCount);
        }

        [UnityTest]
        public System.Collections.IEnumerator ResourceRegistry_SpatialVersionUpdates()
        {
            CreateResourceTypeCatalog("Wood");

            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(timeEntity, new TimeState { Tick = 0, IsPaused = false, FixedDeltaTime = 0.016f });

            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState { Mode = RewindMode.Record });

            var resourceEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(resourceEntity, new ResourceTypeId { Value = "Wood" });
            _entityManager.AddComponentData(resourceEntity, new ResourceSourceConfig());
            _entityManager.AddComponentData(resourceEntity, new ResourceSourceState { UnitsRemaining = 100f });
            _entityManager.AddComponentData(resourceEntity, new LocalTransform
            {
                Position = new float3(1, 0, 1),
                Rotation = quaternion.identity,
                Scale = 1f
            });
            _entityManager.AddComponent<SpatialIndexedTag>(resourceEntity);

            yield return null;

            var registryEntity = _entityManager.GetSingletonEntity<ResourceRegistry>();
            var entries = _entityManager.GetBuffer<ResourceRegistryEntry>(registryEntity);
            Assert.AreEqual(42u, entries[0].SpatialVersion);
            var registry = _entityManager.GetSingleton<ResourceRegistry>();
            Assert.AreEqual(1, registry.SpatialResolvedCount);
            Assert.AreEqual(0, registry.SpatialFallbackCount);
            Assert.AreEqual(0, registry.SpatialUnmappedCount);

            registry = _entityManager.GetSingleton<ResourceRegistry>();
            Assert.AreEqual(42u, registry.LastSpatialVersion);

            // Bump spatial grid version and tick
            ConfigureSpatialGrid(99u);
            _entityManager.SetComponentData(timeEntity, new TimeState { Tick = 5, IsPaused = false, FixedDeltaTime = 0.016f });

            yield return null;

            registry = _entityManager.GetSingleton<ResourceRegistry>();
            Assert.AreEqual(99u, registry.LastSpatialVersion);

            entries = _entityManager.GetBuffer<ResourceRegistryEntry>(registryEntity);
            Assert.AreEqual(99u, entries[0].SpatialVersion);
            Assert.GreaterOrEqual(entries[0].CellId, 0);
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

        private void ConfigureSpatialGrid(uint version)
        {
            var gridQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<SpatialGridConfig>(), ComponentType.ReadWrite<SpatialGridState>());
            if (gridQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var gridEntity = gridQuery.GetSingletonEntity();
            var config = _entityManager.GetComponentData<SpatialGridConfig>(gridEntity);
            config.WorldMin = float3.zero;
            config.CellSize = 1f;
            config.CellCounts = new int3(8, 1, 8);
            config.WorldMax = new float3(config.CellCounts.x * config.CellSize, config.CellCounts.y * config.CellSize, config.CellCounts.z * config.CellSize);
            _entityManager.SetComponentData(gridEntity, config);

            var state = _entityManager.GetComponentData<SpatialGridState>(gridEntity);
            state.Version = version;
            _entityManager.SetComponentData(gridEntity, state);
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

        [UnityTest]
        public System.Collections.IEnumerator ResourceRegistry_ConsoleInstrumentation_LogsSummary()
        {
            CreateResourceTypeCatalog("Wood");

            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(timeEntity, new TimeState { Tick = 0, IsPaused = false, FixedDeltaTime = 0.016f });

            var rewindEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(rewindEntity, new RewindState { Mode = RewindMode.Record });

            var resource = _entityManager.CreateEntity();
            _entityManager.AddComponentData(resource, new ResourceTypeId { Value = "Wood" });
            _entityManager.AddComponentData(resource, new ResourceSourceConfig());
            _entityManager.AddComponentData(resource, new ResourceSourceState { UnitsRemaining = 25f });
            _entityManager.AddComponentData(resource, new LocalTransform { Position = float3.zero, Rotation = quaternion.identity, Scale = 1f });
            _entityManager.AddComponent<SpatialIndexedTag>(resource);

            var instrumentationEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(instrumentationEntity, new RegistryConsoleInstrumentation
            {
                MinTickDelta = 0,
                LastLoggedTick = 0,
                LastDirectoryVersion = 0,
                Flags = 0
            });

            LogAssert.Expect(LogType.Log, new Regex(@"\[Registry\]"));

            yield return null;

            LogAssert.NoUnexpectedReceived();
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
