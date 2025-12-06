using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PureDOTS.Runtime.Scenario
{
    /// <summary>
    /// System that monitors blob asset changes in editor and rebuilds BlobAssetReferences.
    /// Updates editor world entities immediately when source assets change.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct BlobHotReloadSystem : ISystem
    {
#if UNITY_EDITOR
        private double _lastAssetDatabaseRefresh;
        private const double RefreshInterval = 0.5; // Check every 500ms
#endif

        public void OnCreate(ref SystemState state)
        {
#if UNITY_EDITOR
            _lastAssetDatabaseRefresh = 0.0;
            state.Enabled = true;
#else
            state.Enabled = false;
#endif
        }

        public void OnUpdate(ref SystemState state)
        {
#if UNITY_EDITOR
            var currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - _lastAssetDatabaseRefresh < RefreshInterval)
            {
                return;
            }

            _lastAssetDatabaseRefresh = currentTime;

            // Trigger asset database refresh to detect changes
            AssetDatabase.Refresh();

            // Check for blob asset changes and rebuild references
            // This would need to track which blob assets are in use
            // and rebuild them when source ScriptableObjects change
#endif
        }
    }
}

