using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace PureDOTS.Rendering
{
    /// <summary>
    /// Development-only guard that verifies render contract assumptions so spawn/bake paths don't regress.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PresentationContractValidationSystem : ISystem
    {
        private EntityQuery _materialMeshWithoutVariantQuery;
        private EntityQuery _materialMeshWithoutPresenterQuery;
        private EntityQuery _worldBoundsQuery;
        private EntityQuery _chunkBoundsQuery;

        private bool _reportedMissingVariant;
        private bool _reportedMissingPresenter;
        private bool _reportedWorldBounds;
        private bool _reportedChunkBounds;

        public void OnCreate(ref SystemState state)
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            state.Enabled = false;
            return;
#else
            var queryOptions = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab;

            _materialMeshWithoutVariantQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<MaterialMeshInfo>() },
                None = new[] { ComponentType.ReadOnly<RenderVariantKey>() },
                Options = queryOptions
            });

            _materialMeshWithoutPresenterQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<MaterialMeshInfo>(),
                    ComponentType.ReadOnly<RenderVariantKey>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<MeshPresenter>(),
                    ComponentType.ReadOnly<SpritePresenter>(),
                    ComponentType.ReadOnly<DebugPresenter>(),
                    ComponentType.ReadOnly<TracerPresenter>()
                },
                Options = queryOptions
            });

            _worldBoundsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<WorldRenderBounds>() },
                Options = queryOptions
            });

            _chunkBoundsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ChunkWorldRenderBounds>() },
                Options = queryOptions
            });

            state.RequireForUpdate<RenderPresentationCatalog>();
#endif
        }

        public void OnUpdate(ref SystemState state)
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            state.Enabled = false;
            return;
#else
            ReportIfNeeded(_materialMeshWithoutVariantQuery, ref _reportedMissingVariant,
                "[PresentationContractValidationSystem] MaterialMeshInfo detected without RenderVariantKey. Spawners/bakers must create RenderVariantKey (and semantic key) so variant resolution can run.");

            ReportIfNeeded(_materialMeshWithoutPresenterQuery, ref _reportedMissingPresenter,
                "[PresentationContractValidationSystem] MaterialMeshInfo detected on an archetype with no presenter (Mesh/Sprite/Tracer/Debug). Spawn/bake code must add at least one presenter component.");

            ReportIfNeeded(_worldBoundsQuery, ref _reportedWorldBounds,
                "[PresentationContractValidationSystem] WorldRenderBounds is present. Presentation/bake code must no longer add this component – Unity's bounds systems populate it automatically.");

            ReportIfNeeded(_chunkBoundsQuery, ref _reportedChunkBounds,
                "[PresentationContractValidationSystem] ChunkWorldRenderBounds is present. Remove any code paths that add it; Entities Graphics manages chunk bounds.");
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static void ReportIfNeeded(EntityQuery query, ref bool wasReported, string message)
        {
            if (query.IsEmptyIgnoreFilter)
            {
                wasReported = false;
                return;
            }

            if (wasReported)
                return;

            var count = query.CalculateEntityCount();
            Debug.LogError($"{message} (count={count})");
            wasReported = true;
        }
#endif

        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
