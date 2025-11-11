using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Resource;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Executes resource processing recipes for facilities that hold inputs in
    /// their storehouse inventory and produce refined or composite outputs.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(ResourceSystemGroup))]
    [UpdateAfter(typeof(ResourceRegistrySystem))]
    public partial struct ResourceProcessingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<ResourceRecipeSet>();
            state.RequireForUpdate<ResourceTypeIndex>();
            state.RequireForUpdate<ResourceProcessorConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var recipeSetComponent = SystemAPI.GetSingleton<ResourceRecipeSet>();
            if (!recipeSetComponent.Value.IsCreated)
            {
                return;
            }

            ref var recipeSet = ref recipeSetComponent.Value.Value;
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (configRO, processorStateRW, inventoryRW, inventoryItems, queue) in SystemAPI.Query<RefRO<ResourceProcessorConfig>, RefRW<ResourceProcessorState>, RefRW<StorehouseInventory>, DynamicBuffer<StorehouseInventoryItem>, DynamicBuffer<ResourceProcessorQueue>>())
            {
                ref var processorState = ref processorStateRW.ValueRW;
                ref var inventory = ref inventoryRW.ValueRW;

                UpdateInProgressRecipe(ref processorState, ref inventory, inventoryItems, deltaTime);

                if (processorState.RecipeId.Length != 0)
                {
                    continue;
                }

                if (TryStartQueuedRecipe(configRO.ValueRO.FacilityTag, ref processorState, ref inventory, inventoryItems, queue, ref recipeSet))
                {
                    continue;
                }

                if (configRO.ValueRO.AutoRun != 0)
                {
                    TryStartAutoRecipe(configRO.ValueRO.FacilityTag, ref processorState, ref inventory, inventoryItems, ref recipeSet);
                }
            }

            static void UpdateInProgressRecipe(ref ResourceProcessorState processorState, ref StorehouseInventory inventory, DynamicBuffer<StorehouseInventoryItem> items, float deltaTime)
            {
                if (processorState.RecipeId.Length == 0)
                {
                    return;
                }

                if (processorState.RemainingSeconds > 0f)
                {
                    processorState.RemainingSeconds = math.max(0f, processorState.RemainingSeconds - deltaTime);
                }

                if (processorState.RemainingSeconds <= 0f)
                {
                    ProduceOutput(ref processorState, ref inventory, items);
                    processorState = default;
                }
            }

            static bool TryStartQueuedRecipe(FixedString32Bytes facilityTag, ref ResourceProcessorState processorState, ref StorehouseInventory inventory, DynamicBuffer<StorehouseInventoryItem> items, DynamicBuffer<ResourceProcessorQueue> queue, ref ResourceRecipeSetBlob recipeSet)
            {
                for (int i = 0; i < queue.Length; i++)
                {
                    var entry = queue[i];
                    if (!TryGetRecipeById(entry.RecipeId, ref recipeSet, out var recipe))
                    {
                        queue.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if (!FacilityMatches(facilityTag, recipe.FacilityTag))
                    {
                        continue;
                    }

                    if (TryStartRecipe(in recipe, ref processorState, ref inventory, items))
                    {
                        if (entry.Repeat > 1)
                        {
                            entry.Repeat -= 1;
                            queue[i] = entry;
                        }
                        else
                        {
                            queue.RemoveAt(i);
                        }

                        return true;
                    }
                }

                return false;
            }

            static void TryStartAutoRecipe(FixedString32Bytes facilityTag, ref ResourceProcessorState processorState, ref StorehouseInventory inventory, DynamicBuffer<StorehouseInventoryItem> items, ref ResourceRecipeSetBlob recipeSet)
            {
                for (int i = 0; i < recipeSet.Recipes.Length; i++)
                {
                    ref var recipe = ref recipeSet.Recipes[i];
                    if (!FacilityMatches(facilityTag, recipe.FacilityTag))
                    {
                        continue;
                    }

                    if (TryStartRecipe(in recipe, ref processorState, ref inventory, items))
                    {
                        break;
                    }
                }
            }

            static bool TryGetRecipeById(FixedString64Bytes recipeId, ref ResourceRecipeSetBlob recipeSet, out ResourceRecipeBlob recipe)
            {
                for (int i = 0; i < recipeSet.Recipes.Length; i++)
                {
                    ref var candidate = ref recipeSet.Recipes[i];
                    if (candidate.Id.Equals(recipeId))
                    {
                        recipe = candidate;
                        return true;
                    }
                }

                recipe = default;
                return false;
            }

            static bool FacilityMatches(FixedString32Bytes processorTag, FixedString32Bytes recipeTag)
            {
                if (processorTag.Length == 0)
                {
                    return true;
                }

                if (recipeTag.Length == 0)
                {
                    return true;
                }

                return processorTag.Equals(recipeTag);
            }

            static bool TryStartRecipe(in ResourceRecipeBlob recipe, ref ResourceProcessorState processorState, ref StorehouseInventory inventory, DynamicBuffer<StorehouseInventoryItem> items)
            {
                if (!CanFulfill(recipe, items))
                {
                    return false;
                }

                ConsumeIngredients(in recipe, ref inventory, items);

                processorState.RecipeId = recipe.Id;
                processorState.OutputResourceId = recipe.OutputResourceId;
                processorState.Kind = recipe.Kind;
                processorState.OutputAmount = math.max(1, recipe.OutputAmount);
                processorState.RemainingSeconds = math.max(0f, recipe.ProcessSeconds);

                if (processorState.RemainingSeconds <= 0f)
                {
                    ProduceOutput(ref processorState, ref inventory, items);
                    processorState = default;
                }

                return true;
            }

            static bool CanFulfill(in ResourceRecipeBlob recipe, DynamicBuffer<StorehouseInventoryItem> items)
            {
                for (int ingredientIndex = 0; ingredientIndex < recipe.Ingredients.Length; ingredientIndex++)
                {
                    var ingredient = recipe.Ingredients[ingredientIndex];
                    var available = 0f;

                    for (int itemIndex = 0; itemIndex < items.Length; itemIndex++)
                    {
                        if (items[itemIndex].ResourceTypeId.Equals(ingredient.ResourceId))
                        {
                            available = items[itemIndex].Amount;
                            break;
                        }
                    }

                    if (available + 1e-3f < ingredient.Amount)
                    {
                        return false;
                    }
                }

                return true;
            }

            static void ConsumeIngredients(in ResourceRecipeBlob recipe, ref StorehouseInventory inventory, DynamicBuffer<StorehouseInventoryItem> items)
            {
                float consumedTotal = 0f;

                for (int ingredientIndex = 0; ingredientIndex < recipe.Ingredients.Length; ingredientIndex++)
                {
                    var ingredient = recipe.Ingredients[ingredientIndex];

                    for (int itemIndex = 0; itemIndex < items.Length; itemIndex++)
                    {
                        var item = items[itemIndex];
                        if (!item.ResourceTypeId.Equals(ingredient.ResourceId))
                        {
                            continue;
                        }

                        item.Amount -= ingredient.Amount;
                        consumedTotal += ingredient.Amount;

                        if (item.Amount <= 1e-3f)
                        {
                            items.RemoveAt(itemIndex);
                        }
                        else
                        {
                            items[itemIndex] = item;
                        }

                        break;
                    }
                }

                inventory.TotalStored = math.max(0f, inventory.TotalStored - consumedTotal);
                inventory.ItemTypeCount = items.Length;
            }

            static void ProduceOutput(ref ResourceProcessorState processorState, ref StorehouseInventory inventory, DynamicBuffer<StorehouseInventoryItem> items)
            {
                if (processorState.OutputResourceId.Length == 0 || processorState.OutputAmount <= 0)
                {
                    return;
                }

                var amount = (float)processorState.OutputAmount;
                var matched = false;

                for (int itemIndex = 0; itemIndex < items.Length; itemIndex++)
                {
                    var item = items[itemIndex];
                    if (!item.ResourceTypeId.Equals(processorState.OutputResourceId))
                    {
                        continue;
                    }

                    item.Amount += amount;
                    if (item.TierId == 0)
                    {
                        item.TierId = (byte)ResourceQualityTier.Unknown;
                    }
                    if (item.AverageQuality == 0)
                    {
                        item.AverageQuality = 200;
                    }
                    items[itemIndex] = item;
                    matched = true;
                    break;
                }

                if (!matched)
                {
                    items.Add(new StorehouseInventoryItem
                    {
                        ResourceTypeId = processorState.OutputResourceId,
                        Amount = amount,
                        Reserved = 0f,
                        TierId = (byte)ResourceQualityTier.Unknown,
                        AverageQuality = 200
                    });
                }

                inventory.TotalStored += amount;
                inventory.ItemTypeCount = items.Length;
            }
        }
    }
}

