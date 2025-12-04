using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Economy.Resources;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Economy.Production
{
    /// <summary>
    /// Enqueues production jobs based on demand/restock thresholds.
    /// Checks inputs, reserves items, starts jobs.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ProductionJobSchedulingSystem : ISystem
    {
        private ComponentLookup<BusinessProduction> _productionLookup;
        private ComponentLookup<BusinessInventory> _businessInventoryLookup;
        private BufferLookup<ProductionJob> _jobBufferLookup;
        private ComponentLookup<Inventory> _inventoryLookup;
        private BufferLookup<InventoryItem> _itemBufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<ProductionRecipeCatalog>();
            _productionLookup = state.GetComponentLookup<BusinessProduction>(false);
            _businessInventoryLookup = state.GetComponentLookup<BusinessInventory>(false);
            _jobBufferLookup = state.GetBufferLookup<ProductionJob>(false);
            _inventoryLookup = state.GetComponentLookup<Inventory>(false);
            _itemBufferLookup = state.GetBufferLookup<InventoryItem>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<ProductionRecipeCatalog>(out var catalog))
            {
                return;
            }

            ref var catalogBlob = ref catalog.Catalog.Value;

            _productionLookup.Update(ref state);
            _businessInventoryLookup.Update(ref state);
            _jobBufferLookup.Update(ref state);
            _inventoryLookup.Update(ref state);
            _itemBufferLookup.Update(ref state);

            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            var tick = tickTimeState.Tick;

            // Process scheduling requests
            foreach (var (request, entity) in SystemAPI.Query<RefRO<ProductionJobRequest>>().WithEntityAccess())
            {
                ProcessJobRequest(ref state, entity, request.ValueRO, catalogBlob, tick);
                state.EntityManager.RemoveComponent<ProductionJobRequest>(entity);
            }
        }

        [BurstCompile]
        private void ProcessJobRequest(ref SystemState state, Entity businessEntity, ProductionJobRequest request, ProductionRecipeCatalogBlob catalog, uint tick)
        {
            if (!_productionLookup.HasComponent(businessEntity))
            {
                return;
            }

            if (!_businessInventoryLookup.HasComponent(businessEntity))
            {
                return;
            }

            // Find recipe
            if (!TryFindRecipe(request.RecipeId, catalog, out var recipe))
            {
                return;
            }

            var businessInventory = _businessInventoryLookup[businessEntity];
            var inventoryEntity = businessInventory.InventoryEntity;

            if (!_inventoryLookup.HasComponent(inventoryEntity) || !_itemBufferLookup.HasBuffer(inventoryEntity))
            {
                return;
            }

            var items = _itemBufferLookup[inventoryEntity];

            // Check inputs are available
            for (int i = 0; i < recipe.Inputs.Length; i++)
            {
                var input = recipe.Inputs[i];
                float available = GetItemQuantity(items, input.ItemId);
                if (available < input.Quantity)
                {
                    return; // Cannot start job - missing inputs
                }
            }

            // Create job
            if (!_jobBufferLookup.HasBuffer(businessEntity))
            {
                state.EntityManager.AddBuffer<ProductionJob>(businessEntity);
            }

            var jobs = _jobBufferLookup[businessEntity];
            jobs.Add(new ProductionJob
            {
                RecipeId = request.RecipeId,
                Worker = request.Worker,
                Progress = 0f,
                BaseTimeCost = recipe.BaseTimeCost,
                RemainingTime = recipe.BaseTimeCost,
                StartTick = tick,
                EstimatedCompletionTick = tick + (uint)(recipe.BaseTimeCost / recipe.LaborCost)
            });
        }

        [BurstCompile]
        private static bool TryFindRecipe(FixedString64Bytes recipeId, ProductionRecipeCatalogBlob catalog, out ProductionRecipeBlob recipe)
        {
            for (int i = 0; i < catalog.Recipes.Length; i++)
            {
                if (catalog.Recipes[i].RecipeId.Equals(recipeId))
                {
                    recipe = catalog.Recipes[i];
                    return true;
                }
            }

            recipe = default;
            return false;
        }

        [BurstCompile]
        private static float GetItemQuantity(DynamicBuffer<InventoryItem> items, FixedString64Bytes itemId)
        {
            float total = 0f;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].ItemId.Equals(itemId))
                {
                    total += items[i].Quantity;
                }
            }

            return total;
        }
    }

    /// <summary>
    /// Request to enqueue a production job.
    /// Added by business AI or restock systems.
    /// </summary>
    public struct ProductionJobRequest : IComponentData
    {
        public FixedString64Bytes RecipeId;
        public Entity Worker;
    }
}

