using Godgame.Items;
using PureDOTS.Runtime.Shared;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;

namespace Godgame.Authoring.Items
{
    /// <summary>
    /// Authoring ScriptableObject for material catalog.
    /// </summary>
    [CreateAssetMenu(fileName = "MaterialCatalog", menuName = "Godgame/Items/Material Catalog")]
    public class MaterialCatalogAuthoring : ScriptableObject
    {
        public List<MaterialEntryAuthoring> Materials = new List<MaterialEntryAuthoring>();
    }

    /// <summary>
    /// Material entry authoring data.
    /// </summary>
    [System.Serializable]
    public struct MaterialEntryAuthoring
    {
        public string Id;
        public float BaseQuality; // 0-100
        public float Purity; // 0-100
        public Rarity Rarity;
        public byte TechTier; // 0-10
    }

    /// <summary>
    /// Baker for MaterialCatalogAuthoring.
    /// </summary>
    public sealed class MaterialCatalogBaker : Baker<MaterialCatalogAuthoring>
    {
        public override void Bake(MaterialCatalogAuthoring authoring)
        {
            using var bb = new BlobBuilder(Unity.Collections.Allocator.Temp);
            ref var root = ref bb.ConstructRoot<MaterialSpecBlob>();

            var materials = bb.Allocate(ref root.Entries, authoring.Materials.Count);
            for (int i = 0; i < authoring.Materials.Count; i++)
            {
                var entry = authoring.Materials[i];
                materials[i] = new MaterialSpecEntry
                {
                    Id = entry.Id,
                    BaseQuality = entry.BaseQuality,
                    Purity = entry.Purity,
                    Rarity = entry.Rarity,
                    TechTier = entry.TechTier
                };
            }

            var blob = bb.CreateBlobAssetReference<MaterialSpecBlob>(Unity.Collections.Allocator.Persistent);
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new MaterialSpecRef { Blob = blob });
        }
    }

    /// <summary>
    /// Material specification reference component.
    /// </summary>
    public struct MaterialSpecRef : IComponentData
    {
        public BlobAssetReference<MaterialSpecBlob> Blob;
    }

    /// <summary>
    /// Material spec entry in blob.
    /// </summary>
    public struct MaterialSpecEntry
    {
        public FixedString64Bytes Id;
        public float BaseQuality;
        public float Purity;
        public Rarity Rarity;
        public byte TechTier;
    }

    /// <summary>
    /// Material spec blob array wrapper.
    /// </summary>
    public struct MaterialSpecBlob
    {
        public BlobArray<MaterialSpecEntry> Entries;
    }
}

