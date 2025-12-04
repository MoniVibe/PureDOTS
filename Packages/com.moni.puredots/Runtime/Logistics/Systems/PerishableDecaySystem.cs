using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Economy.Resources;
using PureDOTS.Runtime.Logistics.Blobs;
using PureDOTS.Runtime.Logistics.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Logistics.Systems
{
    /// <summary>
    /// Applies perishable decay to cargo items.
    /// Uses ResourceDef.PerishRate and container TempControl.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PerishableDecaySystem : ISystem
    {
        private BufferLookup<CargoItem> _cargoBufferLookup;
        private BufferLookup<CargoContainerSlot> _containerBufferLookup;
        private ComponentLookup<CargoLoadState> _loadStateLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<ItemSpecCatalog>();
            _cargoBufferLookup = state.GetBufferLookup<CargoItem>(false);
            _containerBufferLookup = state.GetBufferLookup<CargoContainerSlot>(false);
            _loadStateLookup = state.GetComponentLookup<CargoLoadState>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<ItemSpecCatalog>(out var itemCatalog))
            {
                return;
            }

            ref var itemCatalogBlob = ref itemCatalog.Catalog.Value;

            _cargoBufferLookup.Update(ref state);
            _containerBufferLookup.Update(ref state);
            _loadStateLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // Process perishable cargo on haulers
            foreach (var (cargoBuffer, haulerEntity) in SystemAPI.Query<DynamicBuffer<CargoItem>>()
                .WithAll<HaulerTag>()
                .WithEntityAccess())
            {
                bool cargoChanged = false;

                for (int i = cargoBuffer.Length - 1; i >= 0; i--)
                {
                    var cargo = cargoBuffer[i];

                    // Check if item is perishable
                    if (!TryFindItemSpec(cargo.ResourceId, itemCatalogBlob, out var itemSpec))
                    {
                        continue;
                    }

                    // Only process perishable items
                    if (!itemSpec.IsPerishable || itemSpec.PerishRate <= 0f)
                    {
                        continue;
                    }

                    // Get container temp control (if using container)
                    float tempControl = 0f; // Default: no temp control
                    if (cargo.ContainerSlotIndex >= 0 && 
                        _containerBufferLookup.HasBuffer(haulerEntity))
                    {
                        var containers = _containerBufferLookup[haulerEntity];
                        if (cargo.ContainerSlotIndex < containers.Length)
                        {
                            var container = containers[cargo.ContainerSlotIndex];
                            // Get container def to get TempControl
                            // For now, assume temp control based on container tier
                            // In practice, would look up ContainerDefBlob
                            tempControl = 0.5f; // Placeholder
                        }
                    }

                    // Calculate decay rate
                    // Better temp control = slower decay
                    float effectivePerishRate = itemSpec.PerishRate * (1.0f - tempControl);

                    // Apply decay
                    float decayAmount = effectivePerishRate;
                    float newAmount = cargo.Amount - decayAmount;

                    if (newAmount <= 0f)
                    {
                        // Remove item if fully decayed
                        cargoBuffer.RemoveAt(i);
                        cargoChanged = true;
                    }
                    else
                    {
                        // Update amount
                        cargo = cargoBuffer[i];
                        cargo.Amount = newAmount;
                        cargoBuffer[i] = cargo;
                        cargoChanged = true;
                    }
                }

                // If cargo changed, mark load state for update
                if (cargoChanged && _loadStateLookup.HasComponent(haulerEntity))
                {
                    var loadState = _loadStateLookup.GetRefRW(haulerEntity);
                    loadState.ValueRW.LastUpdateTick = 0; // Force recalculation
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private static bool TryFindItemSpec(FixedString64Bytes itemId, ItemSpecCatalogBlob catalog, out ItemSpecBlob spec)
        {
            for (int i = 0; i < catalog.Items.Length; i++)
            {
                if (catalog.Items[i].ItemId.Equals(itemId))
                {
                    spec = catalog.Items[i];
                    return true;
                }
            }

            spec = default;
            return false;
        }
    }
}

