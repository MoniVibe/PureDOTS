using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PureDOTS.Rendering
{
    /// <summary>
    /// Emits a single warning if there are visible RenderKey entities but no Entities Graphics renderables.
    /// Helps catch broken catalog/bootstrap wiring early.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
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
#if UNITY_EDITOR
            var diagnosticsEnabled = RenderSanityDebugSettings.LogsEnabled;
#else
            const bool diagnosticsEnabled = false;
#endif

            if (_renderKeyQuery.IsEmptyIgnoreFilter)
            {
#if UNITY_EDITOR
                if (diagnosticsEnabled && !_warnedNoKeys)
                {
                    Debug.LogWarning("[PureDOTS.Rendering] No RenderKey entities found; render pipeline is idle.");
                    _warnedNoKeys = true;
                }
#endif
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
#if UNITY_EDITOR
                if (diagnosticsEnabled && !_warnedNoVisible)
                {
                    Debug.LogError("[PureDOTS.Rendering] RenderKey entities exist but none are marked visible (RenderFlags.Visible == 0).");
                    _warnedNoVisible = true;
                }
#endif
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

#if UNITY_EDITOR
    static class RenderSanityDebugSettings
    {
        const string MenuPath = "PureDOTS/Debug/Rendering/Enable Render Sanity Logs";
        const string EditorPrefKey = "PureDOTS.Rendering.RenderSanity.EnableLogs";

        static bool _initialized;
        static bool _enabled;

        static void EnsureInitialized()
        {
            if (_initialized)
                return;

            _enabled = EditorPrefs.GetBool(EditorPrefKey, false);
            Menu.SetChecked(MenuPath, _enabled);
            _initialized = true;
        }

        public static bool LogsEnabled
        {
            get
            {
                EnsureInitialized();
                return _enabled;
            }
            set
            {
                EnsureInitialized();
                _enabled = value;
                EditorPrefs.SetBool(EditorPrefKey, value);
                Menu.SetChecked(MenuPath, value);
            }
        }

        [MenuItem(MenuPath)]
        static void ToggleLogs()
        {
            LogsEnabled = !LogsEnabled;
        }

        [MenuItem(MenuPath, true)]
        static bool ValidateToggle()
        {
            EnsureInitialized();
            Menu.SetChecked(MenuPath, _enabled);
            return true;
        }
    }
#endif
}
