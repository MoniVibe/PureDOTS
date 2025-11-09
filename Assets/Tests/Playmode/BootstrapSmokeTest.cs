using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Navigation;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Resource;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.TestTools;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Smoke test verifying that DOTS bootstrap creates all required singletons
    /// and systems can run without manual setup.
    /// </summary>
    public class BootstrapSmokeTest
    {
        private World _world;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            Assert.IsNotNull(_world, "DefaultGameObjectInjectionWorld must be created before running BootstrapSmokeTest.");
            _entityManager = _world.EntityManager;
        }

        [UnityTest]
        public System.Collections.IEnumerator Bootstrap_CreatesCoreSingletons()
        {
            // Ensure bootstrap has run
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);

            // Verify time singletons
            Assert.IsTrue(_entityManager.HasComponent<TimeState>(_entityManager.GetSingletonEntity<TimeState>()),
                "TimeState singleton should exist");
            Assert.IsTrue(_entityManager.HasComponent<RewindState>(_entityManager.GetSingletonEntity<RewindState>()),
                "RewindState singleton should exist");
            Assert.IsTrue(_entityManager.HasComponent<HistorySettings>(_entityManager.GetSingletonEntity<HistorySettings>()),
                "HistorySettings singleton should exist");

            // Verify spatial grid
            Assert.IsTrue(_entityManager.HasComponent<SpatialGridConfig>(_entityManager.GetSingletonEntity<SpatialGridConfig>()),
                "SpatialGridConfig singleton should exist");
            Assert.IsTrue(_entityManager.HasComponent<SpatialGridState>(_entityManager.GetSingletonEntity<SpatialGridConfig>()),
                "SpatialGridState should exist on grid entity");

            // Verify flow field
            Assert.IsTrue(_entityManager.HasComponent<FlowFieldConfig>(_entityManager.GetSingletonEntity<FlowFieldConfig>()),
                "FlowFieldConfig singleton should exist");

            // Verify core registries
            Assert.IsTrue(_entityManager.HasComponent<ResourceRegistry>(_entityManager.GetSingletonEntity<ResourceRegistry>()),
                "ResourceRegistry singleton should exist");
            Assert.IsTrue(_entityManager.HasComponent<StorehouseRegistry>(_entityManager.GetSingletonEntity<StorehouseRegistry>()),
                "StorehouseRegistry singleton should exist");
            Assert.IsTrue(_entityManager.HasComponent<VillagerRegistry>(_entityManager.GetSingletonEntity<VillagerRegistry>()),
                "VillagerRegistry singleton should exist");

            // Verify registry directory
            Assert.IsTrue(_entityManager.HasComponent<RegistryDirectory>(_entityManager.GetSingletonEntity<RegistryDirectory>()),
                "RegistryDirectory singleton should exist");

            yield return null;
        }

        [UnityTest]
        public System.Collections.IEnumerator Bootstrap_CreatesRequiredBuffers()
        {
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);

            // Verify spatial grid buffers
            var gridEntity = _entityManager.GetSingletonEntity<SpatialGridConfig>();
            Assert.IsTrue(_entityManager.HasBuffer<SpatialGridCellRange>(gridEntity),
                "SpatialGridCellRange buffer should exist");
            Assert.IsTrue(_entityManager.HasBuffer<SpatialGridEntry>(gridEntity),
                "SpatialGridEntry buffer should exist");

            // Verify flow field buffers
            var flowFieldEntity = _entityManager.GetSingletonEntity<FlowFieldConfig>();
            Assert.IsTrue(_entityManager.HasBuffer<FlowFieldLayer>(flowFieldEntity),
                "FlowFieldLayer buffer should exist");
            Assert.IsTrue(_entityManager.HasBuffer<FlowFieldCellData>(flowFieldEntity),
                "FlowFieldCellData buffer should exist");

            // Verify registry buffers
            var resourceRegistryEntity = _entityManager.GetSingletonEntity<ResourceRegistry>();
            Assert.IsTrue(_entityManager.HasBuffer<ResourceRegistryEntry>(resourceRegistryEntity),
                "ResourceRegistryEntry buffer should exist");

            yield return null;
        }

        [UnityTest]
        public System.Collections.IEnumerator Bootstrap_SystemsCanRunWithoutErrors()
        {
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);

            // Create minimal ResourceTypeIndex (required for resource systems)
            var catalogEntity = _entityManager.CreateEntity();
            var catalogBlob = CreateSingleResourceCatalog();
            _entityManager.AddComponentData(catalogEntity, new ResourceTypeIndex { Catalog = catalogBlob });

            var recipeEntity = _entityManager.CreateEntity();
            var recipeBlobRef = CreateEmptyRecipeSet();
            _entityManager.AddComponentData(recipeEntity, new ResourceRecipeSet { Value = recipeBlobRef });

            // Run a few frames to ensure systems don't crash
            for (int i = 0; i < 3; i++)
            {
                _world.Update();
                yield return null;
            }

            // Verify systems ran without errors (check that entities still exist)
            Assert.IsTrue(_entityManager.Exists(_entityManager.GetSingletonEntity<TimeState>()),
                "TimeState should still exist after updates");
            Assert.IsTrue(_entityManager.Exists(_entityManager.GetSingletonEntity<SpatialGridConfig>()),
                "SpatialGridConfig should still exist after updates");

            catalogBlob.Dispose();
            recipeBlobRef.Dispose();
        }

        [UnityTest]
        public System.Collections.IEnumerator Bootstrap_DefaultConfigsAreValid()
        {
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);

            // Verify TimeState defaults
            var timeState = _entityManager.GetComponentData<TimeState>(_entityManager.GetSingletonEntity<TimeState>());
            Assert.Greater(timeState.FixedDeltaTime, 0f, "FixedDeltaTime should be positive");
            Assert.GreaterOrEqual(timeState.Tick, 0u, "Tick should be non-negative");

            // Verify SpatialGridConfig defaults
            var spatialConfig = _entityManager.GetComponentData<SpatialGridConfig>(_entityManager.GetSingletonEntity<SpatialGridConfig>());
            Assert.Greater(spatialConfig.CellSize, 0f, "CellSize should be positive");
            Assert.Greater(spatialConfig.CellCount, 0, "CellCount should be positive");
            Assert.IsTrue(math.all(spatialConfig.WorldMax > spatialConfig.WorldMin), "WorldMax should be greater than WorldMin");

            // Verify FlowFieldConfig defaults
            var flowConfig = _entityManager.GetComponentData<FlowFieldConfig>(_entityManager.GetSingletonEntity<FlowFieldConfig>());
            Assert.Greater(flowConfig.CellSize, 0f, "FlowField CellSize should be positive");
            Assert.IsTrue(math.all(flowConfig.WorldBoundsMax > flowConfig.WorldBoundsMin), "FlowField bounds should be valid");
            Assert.Greater(flowConfig.CellCount, 0, "FlowField CellCount should be positive");

            yield return null;
        }

        private static BlobAssetReference<ResourceTypeIndexBlob> CreateSingleResourceCatalog()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ResourceTypeIndexBlob>();
            var idsBuilder = builder.Allocate(ref root.Ids, 1);
            var displayNamesBuilder = builder.Allocate(ref root.DisplayNames, 1);
            var colorsBuilder = builder.Allocate(ref root.Colors, 1);
            idsBuilder[0] = new FixedString64Bytes("Wood");
            builder.AllocateString(ref displayNamesBuilder[0], "Wood");
            colorsBuilder[0] = (Color32)new Color(0.5f, 0.3f, 0.1f, 1f);
            var blob = builder.CreateBlobAssetReference<ResourceTypeIndexBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private static BlobAssetReference<ResourceRecipeSetBlob> CreateEmptyRecipeSet()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ResourceRecipeSetBlob>();
            builder.Allocate(ref root.Families, 0);
            builder.Allocate(ref root.Recipes, 0);
            var blob = builder.CreateBlobAssetReference<ResourceRecipeSetBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }
    }
}
