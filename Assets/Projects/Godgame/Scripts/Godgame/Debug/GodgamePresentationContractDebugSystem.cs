using PureDOTS.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Godgame.Debugging
{
    /// <summary>
    /// Development-only probe that logs the first MaterialMeshInfo + RenderVariantKey entity missing presenters.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct GodgamePresentationContractDebugSystem : ISystem
    {
        private EntityQuery _missingPresenterQuery;
        private bool _reported;

        public void OnCreate(ref SystemState state)
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            state.Enabled = false;
            return;
#else
            var options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab;
            _missingPresenterQuery = state.GetEntityQuery(new EntityQueryDesc
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
                Options = options
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
            if (_reported || _missingPresenterQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            using var entities = _missingPresenterQuery.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0)
            {
                return;
            }

            var entity = entities[0];
            var entityManager = state.EntityManager;
            var name = entityManager.GetName(entity);
            var isPrefab = entityManager.HasComponent<Prefab>(entity);
            var isDisabled = entityManager.HasComponent<Disabled>(entity);
            var hasSemantic = entityManager.HasComponent<RenderSemanticKey>(entity);
            var hasVariant = entityManager.HasComponent<RenderVariantKey>(entity);
            var variantValue = hasVariant ? entityManager.GetComponentData<RenderVariantKey>(entity).Value : -1;
            var semanticValue = hasSemantic ? entityManager.GetComponentData<RenderSemanticKey>(entity).Value : (ushort)0;
            var hasMeshPresenter = entityManager.HasComponent<MeshPresenter>(entity);
            var hasSpritePresenter = entityManager.HasComponent<SpritePresenter>(entity);
            var hasDebugPresenter = entityManager.HasComponent<DebugPresenter>(entity);
            var hasTracerPresenter = entityManager.HasComponent<TracerPresenter>(entity);

            Debug.LogError(
                "[GodgamePresentationContractDebug] Missing presenter entity=" + entity +
                " name='" + name + "'" +
                " prefab=" + isPrefab +
                " disabled=" + isDisabled +
                " hasSemantic=" + hasSemantic +
                " semantic=" + semanticValue +
                " hasVariant=" + hasVariant +
                " variant=" + variantValue +
                " presenters(mesh=" + hasMeshPresenter +
                " sprite=" + hasSpritePresenter +
                " debug=" + hasDebugPresenter +
                " tracer=" + hasTracerPresenter + ")");

            _reported = true;
#endif
        }
    }
}
