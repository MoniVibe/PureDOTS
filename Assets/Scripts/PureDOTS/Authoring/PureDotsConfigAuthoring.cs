using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Authoring
{
    [DisallowMultipleComponent]
    public sealed class PureDotsConfigAuthoring : MonoBehaviour
    {
        public PureDotsRuntimeConfig config;
    }

    public sealed class PureDotsConfigBaker : Baker<PureDotsConfigAuthoring>
    {
        public override void Bake(PureDotsConfigAuthoring authoring)
        {
            if (authoring.config == null)
            {
                Debug.LogWarning("PureDotsConfigAuthoring has no config asset assigned.", authoring);
                return;
            }

            var entity = GetEntity(TransformUsageFlags.None);

            var time = authoring.config.Time.ToComponent();
            var history = authoring.config.History.ToComponent();

            AddComponent(entity, time);
            AddComponent(entity, history);

            // Bake ResourceTypeIndex blob asset
            if (authoring.config.ResourceTypes != null && authoring.config.ResourceTypes.entries.Count > 0)
            {
                var catalog = authoring.config.ResourceTypes;
                var builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<ResourceTypeIndexBlob>();

                var idsBuilder = builder.Allocate(ref root.Ids, catalog.entries.Count);
                var displayNamesBuilder = builder.Allocate(ref root.DisplayNames, catalog.entries.Count);
                var colorsBuilder = builder.Allocate(ref root.Colors, catalog.entries.Count);

                for (int i = 0; i < catalog.entries.Count; i++)
                {
                    var entry = catalog.entries[i];
                    idsBuilder[i] = new FixedString64Bytes(entry.id);
                    builder.AllocateString(ref displayNamesBuilder[i], entry.id); // Use ID as display name for now
                    colorsBuilder[i] = entry.displayColor;
                }

                var blobAsset = builder.CreateBlobAssetReference<ResourceTypeIndexBlob>(Allocator.Persistent);
                builder.Dispose();

                AddComponent(entity, new ResourceTypeIndex { Catalog = blobAsset });
            }
        }
    }
}
