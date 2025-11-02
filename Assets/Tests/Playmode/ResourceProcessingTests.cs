using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Resource;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.TestTools;

namespace PureDOTS.Tests.Playmode
{
    public sealed class ResourceProcessingTests : EcsTestFixture
    {
        private BlobAssetReference<ResourceRecipeSetBlob> _recipeSetBlob;
        private BlobAssetReference<ResourceTypeIndexBlob> _resourceCatalogBlob;

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            if (_recipeSetBlob.IsCreated)
            {
                _recipeSetBlob.Dispose();
                _recipeSetBlob = default;
            }

            if (_resourceCatalogBlob.IsCreated)
            {
                _resourceCatalogBlob.Dispose();
                _resourceCatalogBlob = default;
            }
        }

        [UnityTest]
        public System.Collections.IEnumerator Processor_ConsumesInputsAndProducesOutput()
        {
            CreateTimeSingletons();
            CreateRecipeSet();
            CreateResourceTypeCatalog();

            var processorEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(processorEntity, new ResourceProcessorConfig
            {
                FacilityTag = new FixedString32Bytes("refinery"),
                AutoRun = 1
            });
            EntityManager.AddComponentData(processorEntity, new ResourceProcessorState());
            EntityManager.AddBuffer<ResourceProcessorQueue>(processorEntity);

            EntityManager.AddComponentData(processorEntity, new StorehouseInventory
            {
                TotalCapacity = 100f,
                TotalStored = 4f,
                ItemTypeCount = 1
            });

            var inventoryBuffer = EntityManager.AddBuffer<StorehouseInventoryItem>(processorEntity);
            inventoryBuffer.Add(new StorehouseInventoryItem
            {
                ResourceTypeId = new FixedString64Bytes("iron_ore"),
                Amount = 4f,
                Reserved = 0f
            });

            EntityManager.AddComponentData(processorEntity, LocalTransform.FromPosition(float3.zero));

            var systemHandle = World.GetOrCreateSystem<ResourceProcessingSystem>();

            yield return null;

            RunSystem(systemHandle);

            var inventory = EntityManager.GetComponentData<StorehouseInventory>(processorEntity);
            var items = EntityManager.GetBuffer<StorehouseInventoryItem>(processorEntity);

            Assert.That(inventory.TotalStored, Is.GreaterThan(4f - 2f));
            Assert.That(inventory.ItemTypeCount, Is.GreaterThanOrEqualTo(1));

            bool hasIngot = false;
            bool oreReduced = false;
            foreach (var item in items)
            {
                if (item.ResourceTypeId.Equals(new FixedString64Bytes("iron_ingot")))
                {
                    hasIngot = item.Amount >= 1f;
                }

                if (item.ResourceTypeId.Equals(new FixedString64Bytes("iron_ore")))
                {
                    oreReduced = item.Amount <= 2f;
                }
            }

            Assert.IsTrue(hasIngot, "Processor should add refined output to inventory");
            Assert.IsTrue(oreReduced, "Processor should consume raw inputs");
        }

        private void RunSystem(SystemHandle handle)
        {
            ref var system = ref World.Unmanaged.GetUnsafeSystemRef<ResourceProcessingSystem>(handle);
            ref var systemState = ref World.Unmanaged.ResolveSystemStateRef(handle);
            system.OnUpdate(ref systemState);
        }

        private void CreateTimeSingletons()
        {
            var timeEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(timeEntity, new TimeState { Tick = 0, IsPaused = false, FixedDeltaTime = 0.016f });

            var rewindEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(rewindEntity, new RewindState { Mode = RewindMode.Record });
        }

        private void CreateRecipeSet()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ResourceRecipeSetBlob>();

            var families = builder.Allocate(ref root.Families, 1);
            ref var family = ref families[0];
            family.Id = new FixedString64Bytes("metals");
            family.DisplayName = new FixedString64Bytes("Metals");
            family.RawResourceId = new FixedString64Bytes("iron_ore");
            family.RefinedResourceId = new FixedString64Bytes("iron_ingot");
            family.CompositeResourceId = new FixedString64Bytes("steel");

            var recipes = builder.Allocate(ref root.Recipes, 1);
            ref var recipe = ref recipes[0];
            recipe.Id = new FixedString64Bytes("refine_iron_ingot_test");
            recipe.Kind = ResourceRecipeKind.Refinement;
            recipe.FacilityTag = new FixedString32Bytes("refinery");
            recipe.OutputResourceId = new FixedString64Bytes("iron_ingot");
            recipe.OutputAmount = 1;
            recipe.ProcessSeconds = 0f;
            var ingredients = builder.Allocate(ref recipe.Ingredients, 1);
            ingredients[0] = new ResourceIngredientBlob
            {
                ResourceId = new FixedString64Bytes("iron_ore"),
                Amount = 2
            };

            _recipeSetBlob = builder.CreateBlobAssetReference<ResourceRecipeSetBlob>(Allocator.Persistent);
            builder.Dispose();

            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, new ResourceRecipeSet { Value = _recipeSetBlob });
        }

        private void CreateResourceTypeCatalog()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ResourceTypeIndexBlob>();

            var ids = builder.Allocate(ref root.Ids, 2);
            ids[0] = new FixedString64Bytes("iron_ore");
            ids[1] = new FixedString64Bytes("iron_ingot");

            var names = builder.Allocate(ref root.DisplayNames, 2);
            builder.AllocateString(ref names[0], "Iron Ore");
            builder.AllocateString(ref names[1], "Iron Ingot");

            var colors = builder.Allocate(ref root.Colors, 2);
            colors[0] = new UnityEngine.Color32(134, 86, 71, 255);
            colors[1] = new UnityEngine.Color32(180, 180, 190, 255);

            _resourceCatalogBlob = builder.CreateBlobAssetReference<ResourceTypeIndexBlob>(Allocator.Persistent);
            builder.Dispose();

            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, new ResourceTypeIndex { Catalog = _resourceCatalogBlob });
        }
    }
}

