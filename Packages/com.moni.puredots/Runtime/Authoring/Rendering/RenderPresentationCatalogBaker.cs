using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using PureDOTS.Rendering;

namespace PureDOTS.Authoring.Rendering
{
#if UNITY_EDITOR
    /// <summary>
    /// Converts <see cref="RenderPresentationCatalogAuthoring"/> to the runtime catalog components.
    /// Lives in the authoring assembly to avoid Editor-only dependencies in runtime code.
    /// </summary>
    public sealed class RenderPresentationCatalogBaker : Baker<RenderPresentationCatalogAuthoring>
    {
        public override void Bake(RenderPresentationCatalogAuthoring authoring)
        {
            if (authoring == null || authoring.CatalogDefinition == null)
            {
                Debug.LogError("[RenderPresentationCatalogBaker] Catalog asset is missing.");
                return;
            }

            var buildInput = authoring.CatalogDefinition.ToBuildInput();
            if (!RenderPresentationCatalogBuilder.TryBuild(buildInput, Allocator.Temp, out var blobRef, out var renderMeshArray))
            {
                return;
            }

            var mainEntity = GetEntity(TransformUsageFlags.None);
            var renderMeshEntity = CreateAdditionalEntity(TransformUsageFlags.None);
            AddSharedComponentManaged(renderMeshEntity, renderMeshArray);

            var catalogComponent = new RenderPresentationCatalog
            {
                Blob = blobRef,
                RenderMeshArrayEntity = renderMeshEntity
            };

            AddComponent(mainEntity, catalogComponent);

            AddComponent(mainEntity, new RenderCatalogVersion
            {
                Value = RenderCatalogVersionUtility.Next()
            });
        }
    }
#endif
}
