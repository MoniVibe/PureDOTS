using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace PureDOTS.Rendering
{
    /// <summary>
    /// Emits a single warning if there are visible RenderKey entities but no Entities Graphics renderables.
    /// Helps catch broken catalog/bootstrap wiring early.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    public partial struct RenderSanitySystem : ISystem
    {
        private EntityQuery _materialMeshInfoQuery;
        private EntityQuery _renderKeyQuery;
        private bool _warned;
        private bool _warnedNoKeys;
        private bool _warnedNoVisible;

        public void OnCreate(ref SystemState state)
        {
            _materialMeshInfoQuery = state.GetEntityQuery(ComponentType.ReadOnly<MaterialMeshInfo>());
            _renderKeyQuery = state.GetEntityQuery(ComponentType.ReadOnly<RenderKey>());
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!Application.isPlaying)
                return;

            if (_renderKeyQuery.IsEmptyIgnoreFilter)
            {
                if (!_warnedNoKeys)
                {
                    Debug.LogWarning("[PureDOTS.Rendering] No RenderKey entities found; render pipeline is idle.");
                    _warnedNoKeys = true;
                }
                _warned = false;
                return;
            }

            _warnedNoKeys = false;

            int visibleSimEntities = 0;
            foreach (var flags in SystemAPI.Query<RefRO<RenderFlags>>().WithAll<RenderKey>())
            {
                if (flags.ValueRO.Visible != 0)
                {
                    visibleSimEntities++;
                }
            }

            if (visibleSimEntities == 0)
            {
                if (!_warnedNoVisible)
                {
                    Debug.LogError("[PureDOTS.Rendering] RenderKey entities exist but none are marked visible (RenderFlags.Visible == 0).");
                    _warnedNoVisible = true;
                }
                return;
            }

            _warnedNoVisible = false;

            if (_warned)
                return;

            var renderableCount = _materialMeshInfoQuery.CalculateEntityCount();
            if (renderableCount > 0)
                return;

            _warned = true;
            Debug.LogWarning("[PureDOTS.Rendering] Visible RenderKey entities detected but no MaterialMeshInfo present. Check ApplyRenderCatalogSystem and render bootstrap.");
        }
    }
}
