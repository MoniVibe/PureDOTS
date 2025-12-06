using PureDOTS.Config;
using PureDOTS.Runtime.Modifiers;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Entities.Hybrid;
#endif

namespace PureDOTS.Authoring
{
    /// <summary>
    /// MonoBehaviour that references a modifier catalog asset for baking.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ModifierCatalogAuthoring : MonoBehaviour
    {
        [Tooltip("Reference to the ScriptableObject catalog that defines modifier data.")]
        public ModifierCatalog catalog;

        class Baker : Unity.Entities.Baker<ModifierCatalogAuthoring>
        {
            public override void Bake(ModifierCatalogAuthoring authoring)
            {
                if (authoring.catalog == null)
                {
                    Debug.LogWarning("[ModifierCatalogBaker] Missing catalog reference.");
                    return;
                }

                var catalog = authoring.catalog;
                if (catalog.modifiers == null || catalog.modifiers.Count == 0)
                {
                    Debug.LogWarning($"[ModifierCatalogBaker] No modifiers defined in {catalog.name}.");
                    return;
                }

                // Build blob data
                var builder = new BlobBuilder(Allocator.Temp);
                ref var catalogBlob = ref builder.ConstructRoot<ModifierCatalogBlob>();
                var modifiersArrayBuilder = builder.Allocate(ref catalogBlob.Modifiers, catalog.modifiers.Count);

                // Build dependency graph data
                var dependencyList = new System.Collections.Generic.List<System.Collections.Generic.List<ushort>>();
                dependencyList.Add(new System.Collections.Generic.List<ushort>()); // Chain 0 is empty

                for (int i = 0; i < catalog.modifiers.Count; i++)
                {
                    var modifierDef = catalog.modifiers[i];
                    ref var modifierBlob = ref modifiersArrayBuilder[i];

                    builder.AllocateString(ref modifierBlob.Name, modifierDef.name ?? string.Empty);

                    modifierBlob.Operation = (byte)modifierDef.operation;
                    modifierBlob.BaseValue = modifierDef.baseValue;
                    modifierBlob.Category = (byte)modifierDef.category;
                    modifierBlob.DurationScale = modifierDef.durationScale;

                    // Build dependency chain (simplified - full implementation would do topological sort)
                    if (modifierDef.dependencies != null && modifierDef.dependencies.Count > 0)
                    {
                        var chain = new System.Collections.Generic.List<ushort>(modifierDef.dependencies);
                        chain.Add((ushort)i); // Add self to chain
                        dependencyList.Add(chain);
                        modifierBlob.DependencyChainIndex = (ushort)(dependencyList.Count - 1);
                    }
                    else
                    {
                        modifierBlob.DependencyChainIndex = 0; // No dependencies
                    }
                }

                // Build dependency chains blob array
                var chainsBuilder = builder.Allocate(ref catalogBlob.DependencyChains, dependencyList.Count);
                for (int i = 0; i < dependencyList.Count; i++)
                {
                    var chain = dependencyList[i];
                    var chainBuilder = builder.Allocate(ref chainsBuilder[i], chain.Count);
                    for (int j = 0; j < chain.Count; j++)
                    {
                        chainBuilder[j] = chain[j];
                    }
                }

                var blobAsset = builder.CreateBlobAssetReference<ModifierCatalogBlob>(Allocator.Persistent);
                builder.Dispose();

                // Register blob asset for automatic disposal / deduplication
                AddBlobAsset(ref blobAsset, out _);

                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ModifierCatalogRef
                {
                    Blob = blobAsset
                });

                Debug.Log($"[ModifierCatalogBaker] Created catalog from {catalog.name} with {catalog.modifiers.Count} modifiers.");
            }
        }
    }
}

