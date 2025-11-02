using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Space4X.Systems
{
    /// <summary>
    /// Debug system to verify ResourceTypeIndex catalog is populated and resources are registered.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class ResourceCatalogDebugSystem : SystemBase
    {
        private bool _hasLogged;

        protected override void OnCreate()
        {
            RequireForUpdate<ResourceTypeIndex>();
            RequireForUpdate<ResourceRegistry>();
        }

        protected override void OnUpdate()
        {
            if (_hasLogged)
                return;

            // Check if catalog exists
            if (!SystemAPI.HasSingleton<ResourceTypeIndex>())
            {
                Debug.LogError("[ResourceCatalogDebug] ResourceTypeIndex singleton NOT FOUND! Resources won't be registered.");
                _hasLogged = true;
                return;
            }

            var catalog = SystemAPI.GetSingleton<ResourceTypeIndex>();
            if (!catalog.Catalog.IsCreated)
            {
                Debug.LogError("[ResourceCatalogDebug] ResourceTypeIndex catalog blob NOT CREATED! Check PureDotsConfigAuthoring has ResourceTypes configured.");
                _hasLogged = true;
                return;
            }

            // Log catalog contents
            ref var blob = ref catalog.Catalog.Value;
            Debug.Log($"[ResourceCatalogDebug] Catalog has {blob.Ids.Length} resource types:");
            for (int i = 0; i < blob.Ids.Length; i++)
            {
                Debug.Log($"  [{i}] {blob.Ids[i]}");
            }

            // Check registry
            if (!SystemAPI.HasSingleton<ResourceRegistry>())
            {
                Debug.LogWarning("[ResourceCatalogDebug] ResourceRegistry singleton NOT FOUND!");
                _hasLogged = true;
                return;
            }

            var registry = SystemAPI.GetSingleton<ResourceRegistry>();
            var registryEntity = SystemAPI.GetSingletonEntity<ResourceRegistry>();
            var entries = EntityManager.GetBuffer<ResourceRegistryEntry>(registryEntity);

            Debug.Log($"[ResourceCatalogDebug] ResourceRegistry has {entries.Length} registered resources (Total: {registry.TotalResources}, Active: {registry.TotalActiveResources})");

            if (entries.Length == 0)
            {
                Debug.LogWarning("[ResourceCatalogDebug] NO RESOURCES REGISTERED! This means:");
                Debug.LogWarning("  1. resourceTypeId might be empty on ResourceSourceAuthoring components");
                Debug.LogWarning("  2. Resource types might not be in the catalog");
                Debug.LogWarning("  3. Resources might not be baked into entities yet");
            }
            else
            {
                Debug.Log("[ResourceCatalogDebug] Registered resources:");
                ref var blobRef = ref catalog.Catalog.Value;
                for (int i = 0; i < math.min(entries.Length, 10); i++)
                {
                    var entry = entries[i];
                    var typeName = entry.ResourceTypeIndex < blobRef.Ids.Length 
                        ? blobRef.Ids[entry.ResourceTypeIndex].ToString() 
                        : $"Unknown({entry.ResourceTypeIndex})";
                    Debug.Log($"  [{i}] Type: {typeName}, Position: {entry.Position}, Units: {entry.UnitsRemaining}");
                }
                if (entries.Length > 10)
                {
                    Debug.Log($"  ... and {entries.Length - 10} more");
                }
            }

            _hasLogged = true;
        }
    }
}

