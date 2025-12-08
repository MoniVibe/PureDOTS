using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Economy.Resources
{
    /// <summary>
    /// Calculates total mass and volume for inventories from items using ItemSpec catalog.
    /// Updates Inventory.CurrentMass and CurrentVolume.
    /// Also updates MassComponent for hierarchical aggregation.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct InventoryMassSystem : ISystem
    {
        private ComponentLookup<Inventory> _inventoryLookup;
        private BufferLookup<InventoryItem> _itemBufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<ItemSpecCatalog>();
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

            if (!SystemAPI.TryGetSingleton<ItemSpecCatalog>(out var catalog))
            {
                return;
            }

            ref var catalogBlob = ref catalog.Catalog.Value;

            _inventoryLookup.Update(ref state);
            _itemBufferLookup.Update(ref state);

            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            var tick = tickTimeState.Tick;

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var (inventory, entity) in SystemAPI.Query<RefRW<Inventory>>().WithEntityAccess())
            {
                if (!_itemBufferLookup.HasBuffer(entity))
                {
                    continue;
                }

                var items = _itemBufferLookup[entity];
                float totalMass = 0f;
                float totalVolume = 0f;

                for (int i = 0; i < items.Length; i++)
                {
                    var item = items[i];
                    if (TryFindItemSpec(item.ItemId, ref catalogBlob, out var spec))
                    {
                        totalMass += item.Quantity * spec.MassPerUnit;
                        totalVolume += item.Quantity * spec.VolumePerUnit;
                    }
                }

                inventory.ValueRW.CurrentMass = totalMass;
                inventory.ValueRW.CurrentVolume = totalVolume;
                inventory.ValueRW.LastUpdateTick = tick;

                // Update MassComponent for hierarchical aggregation
                if (SystemAPI.HasComponent<MassComponent>(entity))
                {
                    var mass = SystemAPI.GetComponent<MassComponent>(entity);
                    mass.Mass = totalMass;
                    SystemAPI.SetComponent(entity, mass);
                    // mark dirty via ECB (Structural change must use ECB in Entities 1.4 with ISystem)
                    ecb.AddComponent<MassDirtyTag>(entity);
                }
                else if (totalMass > 0f)
                {
                    ecb.AddComponent(entity, new MassComponent
                    {
                        Mass = totalMass,
                        CenterOfMass = float3.zero,
                        InertiaTensor = float3.zero
                    });
                    ecb.AddComponent<MassDirtyTag>(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private static bool TryFindItemSpec(in FixedString64Bytes itemId, ref ItemSpecCatalogBlob catalog, out ItemSpecBlob spec)
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

