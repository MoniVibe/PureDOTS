using Godgame.Registry;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Spatial;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Godgame.Authoring
{
    /// <summary>
    /// Authoring component for storehouses that villagers deposit resources into.
    /// Bakes PureDOTS components required for storehouse registry and inventory management.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StorehouseAuthoring : MonoBehaviour
    {
        [SerializeField]
        private string label = "Storehouse";

        [SerializeField]
        [Tooltip("Storehouse ID for identification")]
        private int storehouseId = 1001;

        [SerializeField]
        [Tooltip("Total capacity across all resource types")]
        private float totalCapacity = 500f;

        [SerializeField]
        [Tooltip("Initial stored amount")]
        private float initialStored = 0f;

        [SerializeField]
        [Tooltip("Capacity for Wood (ResourceTypeIndex 1)")]
        private float woodCapacity = 250f;

        [SerializeField]
        [Tooltip("Capacity for Ore (ResourceTypeIndex 2)")]
        private float oreCapacity = 250f;

        private sealed class Baker : Unity.Entities.Baker<StorehouseAuthoring>
        {
            public override void Bake(StorehouseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);

                // Add PureDOTS storehouse components
                AddComponent(entity, new StorehouseConfig
                {
                    ShredRate = 0f,
                    MaxShredQueueSize = 0,
                    InputRate = 0f,
                    OutputRate = 0f,
                    Label = new FixedString64Bytes(authoring.label ?? string.Empty)
                });

                AddComponent(entity, new StorehouseInventory
                {
                    TotalCapacity = math.max(0f, authoring.totalCapacity),
                    TotalStored = math.max(0f, authoring.initialStored),
                    LastUpdateTick = 0
                });

                // Add capacity buffers for each resource type
                var capacityBuffer = AddBuffer<StorehouseCapacityElement>(entity);
                
                // Wood capacity (ResourceTypeIndex 1)
                if (authoring.woodCapacity > 0f)
                {
                    capacityBuffer.Add(new StorehouseCapacityElement
                    {
                        ResourceTypeId = new FixedString64Bytes("wood"),
                        MaxCapacity = math.max(0f, authoring.woodCapacity)
                    });
                }

                // Ore capacity (ResourceTypeIndex 2)
                if (authoring.oreCapacity > 0f)
                {
                    capacityBuffer.Add(new StorehouseCapacityElement
                    {
                        ResourceTypeId = new FixedString64Bytes("ore"),
                        MaxCapacity = math.max(0f, authoring.oreCapacity)
                    });
                }

                // Add inventory items buffer (initially empty)
                var inventoryBuffer = AddBuffer<StorehouseInventoryItem>(entity);
                // Inventory items are added/removed at runtime by systems

                // Add GodgameStorehouse mirror component (for registry bridge)
                var labelStr = string.IsNullOrWhiteSpace(authoring.label)
                    ? $"Storehouse-{authoring.storehouseId}"
                    : authoring.label;

                var summaries = default(FixedList32Bytes<GodgameStorehouseResourceSummary>);
                
                if (authoring.woodCapacity > 0f)
                {
                    summaries.Add(new GodgameStorehouseResourceSummary
                    {
                        ResourceTypeIndex = 1, // Wood
                        Capacity = authoring.woodCapacity,
                        Stored = 0f,
                        Reserved = 0f
                    });
                }

                if (authoring.oreCapacity > 0f)
                {
                    summaries.Add(new GodgameStorehouseResourceSummary
                    {
                        ResourceTypeIndex = 2, // Ore
                        Capacity = authoring.oreCapacity,
                        Stored = 0f,
                        Reserved = 0f
                    });
                }

                AddComponent(entity, new GodgameStorehouse
                {
                    Label = new FixedString64Bytes(labelStr),
                    StorehouseId = authoring.storehouseId,
                    TotalCapacity = math.max(0f, authoring.totalCapacity),
                    TotalStored = math.max(0f, authoring.initialStored),
                    TotalReserved = 0f,
                    PrimaryResourceTypeIndex = summaries.Length > 0 ? summaries[0].ResourceTypeIndex : (ushort)0,
                    LastMutationTick = 0,
                    ResourceSummaries = summaries
                });

                // Add spatial indexing
                AddComponent<SpatialIndexedTag>(entity);
            }
        }
    }
}

