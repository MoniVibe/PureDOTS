using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Maintains a registry of all storehouses with capacity information for efficient queries.
    /// Updates singleton component and buffer with current storehouse state.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(ResourceSystemGroup))]
    [UpdateAfter(typeof(ResourceRegistrySystem))]
    [UpdateBefore(typeof(ResourceDepositSystem))]
    public partial struct StorehouseRegistrySystem : ISystem
    {
        private EntityQuery _storehouseQuery;
        private EntityQuery _registryQuery;
        private ComponentLookup<StorehouseJobReservation> _reservationLookup;
        private BufferLookup<StorehouseReservationItem> _reservationItemsLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _storehouseQuery = SystemAPI.QueryBuilder()
                .WithAll<StorehouseConfig, StorehouseInventory>()
                .Build();

            _registryQuery = SystemAPI.QueryBuilder()
                .WithAll<StorehouseRegistry>()
                .Build();

            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<ResourceTypeIndex>();

            _reservationLookup = state.GetComponentLookup<StorehouseJobReservation>(true);
            _reservationItemsLookup = state.GetBufferLookup<StorehouseReservationItem>(true);
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

            // Ensure registry singleton exists
            if (!SystemAPI.HasSingleton<StorehouseRegistry>())
            {
                var registryEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<StorehouseRegistry>(registryEntity);
                state.EntityManager.AddBuffer<StorehouseRegistryEntry>(registryEntity);
            }

            var registryEntity = SystemAPI.GetSingletonEntity<StorehouseRegistry>();
            var registry = SystemAPI.GetComponentRW<StorehouseRegistry>(registryEntity);
            var entries = SystemAPI.GetBufferRW<StorehouseRegistryEntry>(registryEntity);

            entries.Clear();

            var totalStorehouses = 0;
            var totalCapacity = 0f;
            var totalStored = 0f;

            var catalogRef = SystemAPI.GetSingleton<ResourceTypeIndex>().Catalog;
            if (!catalogRef.IsCreated)
            {
                return;
            }

            var catalog = catalogRef.Value;

            _reservationLookup.Update(ref state);
            _reservationItemsLookup.Update(ref state);

            // Query all storehouses
            foreach (var (inventory, transform, entity) in SystemAPI.Query<RefRO<StorehouseInventory>, RefRO<LocalTransform>>()
                .WithAll<StorehouseConfig>()
                .WithEntityAccess())
            {
                var typeSummaries = new FixedList32Bytes<StorehouseRegistryCapacitySummary>();
                var reservation = _reservationLookup.HasComponent(entity)
                    ? _reservationLookup[entity]
                    : default;

                DynamicBuffer<StorehouseReservationItem> reservationItems = default;
                var hasReservationItems = _reservationItemsLookup.HasBuffer(entity);
                if (hasReservationItems)
                {
                    reservationItems = _reservationItemsLookup[entity];
                }

                if (SystemAPI.TryGetBuffer<StorehouseCapacityElement>(entity, true, out var capacityBuffer))
                {
                    for (int i = 0; i < capacityBuffer.Length; i++)
                    {
                        var capacity = capacityBuffer[i];
                        var typeIndex = catalog.LookupIndex(capacity.ResourceTypeId);
                        if (typeIndex < 0)
                        {
                            continue;
                        }

                        if (!typeSummaries.IsFull)
                        {
                            typeSummaries.Add(new StorehouseRegistryCapacitySummary
                            {
                                ResourceTypeIndex = (ushort)typeIndex,
                                Capacity = capacity.MaxCapacity,
                                Stored = 0f,
                                Reserved = 0f
                            });
                        }
                    }
                }

                if (SystemAPI.TryGetBuffer<StorehouseInventoryItem>(entity, true, out var inventoryItems))
                {
                    for (int i = 0; i < inventoryItems.Length; i++)
                    {
                        var item = inventoryItems[i];
                        var typeIndex = catalog.LookupIndex(item.ResourceTypeId);
                        if (typeIndex < 0)
                        {
                            continue;
                        }

                        var summaryIndex = FindSummaryIndex(ref typeSummaries, (ushort)typeIndex);
                        if (summaryIndex >= 0)
                        {
                            var summary = typeSummaries[summaryIndex];
                            summary.Stored = item.Amount;
                            summary.Reserved = item.Reserved;
                            typeSummaries[summaryIndex] = summary;
                        }
                        else if (!typeSummaries.IsFull)
                        {
                            typeSummaries.Add(new StorehouseRegistryCapacitySummary
                            {
                                ResourceTypeIndex = (ushort)typeIndex,
                                Capacity = 0f,
                                Stored = item.Amount,
                                Reserved = item.Reserved
                            });
                        }
                    }
                }

                if (hasReservationItems)
                {
                    for (int i = 0; i < reservationItems.Length; i++)
                    {
                        var item = reservationItems[i];
                        var idx = FindSummaryIndex(ref typeSummaries, item.ResourceTypeIndex);
                        if (idx >= 0)
                        {
                            var summary = typeSummaries[idx];
                            summary.Reserved += item.Reserved;
                            typeSummaries[idx] = summary;
                        }
                        else if (!typeSummaries.IsFull)
                        {
                            typeSummaries.Add(new StorehouseRegistryCapacitySummary
                            {
                                ResourceTypeIndex = item.ResourceTypeIndex,
                                Capacity = 0f,
                                Stored = 0f,
                                Reserved = item.Reserved
                            });
                        }
                    }
                }

                entries.Add(new StorehouseRegistryEntry
                {
                    StorehouseEntity = entity,
                    Position = transform.ValueRO.Position,
                    TotalCapacity = inventory.ValueRO.TotalCapacity,
                    TotalStored = inventory.ValueRO.TotalStored,
                    TypeSummaries = typeSummaries,
                    LastMutationTick = math.max(inventory.ValueRO.LastUpdateTick, reservation.LastMutationTick)
                });

                totalStorehouses++;
                totalCapacity += inventory.ValueRO.TotalCapacity;
                totalStored += inventory.ValueRO.TotalStored;
            }

            registry.ValueRW = new StorehouseRegistry
            {
                TotalStorehouses = totalStorehouses,
                TotalCapacity = totalCapacity,
                TotalStored = totalStored,
                LastUpdateTick = timeState.Tick
            };
        }

        private static int FindSummaryIndex(ref FixedList32Bytes<StorehouseRegistryCapacitySummary> summaries, ushort resourceTypeIndex)
        {
            for (int i = 0; i < summaries.Length; i++)
            {
                if (summaries[i].ResourceTypeIndex == resourceTypeIndex)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

