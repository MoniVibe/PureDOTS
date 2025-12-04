using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Economy.Resources
{
    /// <summary>
    /// Bootstraps the ItemSpec catalog singleton with default items.
    /// Creates a default catalog if none exists.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct ItemSpecBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            EnsureCatalog(ref state);
            state.Enabled = false; // Only run once
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // No-op after initial bootstrap
        }

        [BurstCompile]
        private static void EnsureCatalog(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<ItemSpecCatalog>())
            {
                return;
            }

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ItemSpecCatalogBlob>();

            // Create default items
            var items = new NativeList<ItemSpecBlob>(32, Allocator.Temp);
            
            // Food items
            items.Add(new ItemSpecBlob
            {
                ItemId = new FixedString64Bytes("grain"),
                Name = new FixedString64Bytes("Grain"),
                Category = ItemCategory.Food,
                MassPerUnit = 0.5f,
                VolumePerUnit = 0.001f,
                StackSize = 1000,
                Tags = ItemTags.Food | ItemTags.BulkOnly | ItemTags.Perishable,
                BaseValue = 0.2f,
                IsPerishable = true,
                PerishRate = 0.01f,
                IsDurable = false,
                BaseDurability = 0f
            });

            items.Add(new ItemSpecBlob
            {
                ItemId = new FixedString64Bytes("bread"),
                Name = new FixedString64Bytes("Bread"),
                Category = ItemCategory.Food,
                MassPerUnit = 0.3f,
                VolumePerUnit = 0.0005f,
                StackSize = 20,
                Tags = ItemTags.Food | ItemTags.Perishable,
                BaseValue = 0.5f,
                IsPerishable = true,
                PerishRate = 0.05f,
                IsDurable = false,
                BaseDurability = 0f
            });

            // Raw materials
            items.Add(new ItemSpecBlob
            {
                ItemId = new FixedString64Bytes("iron_ore"),
                Name = new FixedString64Bytes("Iron Ore"),
                Category = ItemCategory.Raw,
                MassPerUnit = 5.0f,
                VolumePerUnit = 0.002f,
                StackSize = 100,
                Tags = ItemTags.BulkOnly,
                BaseValue = 1.0f,
                IsPerishable = false,
                PerishRate = 0f,
                IsDurable = true,
                BaseDurability = 1.0f
            });

            items.Add(new ItemSpecBlob
            {
                ItemId = new FixedString64Bytes("wood"),
                Name = new FixedString64Bytes("Wood"),
                Category = ItemCategory.Raw,
                MassPerUnit = 0.8f,
                VolumePerUnit = 0.001f,
                StackSize = 500,
                Tags = ItemTags.BulkOnly | ItemTags.Flammable,
                BaseValue = 0.3f,
                IsPerishable = false,
                PerishRate = 0f,
                IsDurable = true,
                BaseDurability = 1.0f
            });

            // Processed materials
            items.Add(new ItemSpecBlob
            {
                ItemId = new FixedString64Bytes("iron_ingot"),
                Name = new FixedString64Bytes("Iron Ingot"),
                Category = ItemCategory.Processed,
                MassPerUnit = 5.0f,
                VolumePerUnit = 0.001f,
                StackSize = 100,
                Tags = ItemTags.BulkOnly,
                BaseValue = 5.0f,
                IsPerishable = false,
                PerishRate = 0f,
                IsDurable = true,
                BaseDurability = 1.0f
            });

            // Tools
            items.Add(new ItemSpecBlob
            {
                ItemId = new FixedString64Bytes("hammer"),
                Name = new FixedString64Bytes("Hammer"),
                Category = ItemCategory.Tool,
                MassPerUnit = 2.0f,
                VolumePerUnit = 0.001f,
                StackSize = 1,
                Tags = ItemTags.Durable,
                BaseValue = 10.0f,
                IsPerishable = false,
                PerishRate = 0f,
                IsDurable = true,
                BaseDurability = 1.0f
            });

            // Weapons
            items.Add(new ItemSpecBlob
            {
                ItemId = new FixedString64Bytes("sword"),
                Name = new FixedString64Bytes("Sword"),
                Category = ItemCategory.Weapon,
                MassPerUnit = 2.5f,
                VolumePerUnit = 0.002f,
                StackSize = 1,
                Tags = ItemTags.Durable | ItemTags.MilitaryGrade,
                BaseValue = 50.0f,
                IsPerishable = false,
                PerishRate = 0f,
                IsDurable = true,
                BaseDurability = 1.0f
            });

            var itemsArray = builder.Allocate(ref root.Items, items.Length);
            for (int i = 0; i < items.Length; i++)
            {
                itemsArray[i] = items[i];
            }

            items.Dispose();

            var blob = builder.CreateBlobAssetReference<ItemSpecCatalogBlob>(Allocator.Persistent);
            builder.Dispose();

            var entity = state.EntityManager.CreateEntity(typeof(ItemSpecCatalog));
            state.EntityManager.SetComponentData(entity, new ItemSpecCatalog { Catalog = blob });
        }
    }
}

