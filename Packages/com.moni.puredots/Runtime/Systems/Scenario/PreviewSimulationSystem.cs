using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Systems;

namespace PureDOTS.Runtime.Scenario
{
    /// <summary>
    /// System that runs a small PureDOTS world in parallel for instant preview feedback.
    /// Syncs editor world entities to preview world on demand.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PreviewSimulationSystem : ISystem
    {
        private World _previewWorld;
        private bool _isPreviewActive;

        public void OnCreate(ref SystemState state)
        {
            _previewWorld = null;
            _isPreviewActive = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            var previewState = SystemAPI.GetSingletonRW<PreviewSimulationState>();
            
            if (previewState.ValueRO.Enabled && !_isPreviewActive)
            {
                StartPreview(ref state);
            }
            else if (!previewState.ValueRO.Enabled && _isPreviewActive)
            {
                StopPreview();
            }

            if (_isPreviewActive && _previewWorld != null && _previewWorld.IsCreated)
            {
                SyncEditorToPreview(ref state);
                UpdatePreviewWorld();
            }
        }

        public void OnDestroy(ref SystemState state)
        {
            StopPreview();
        }

        private void StartPreview(ref SystemState state)
        {
            if (_previewWorld != null && _previewWorld.IsCreated)
            {
                return;
            }

            // Create preview world using PureDotsWorldBootstrap pattern
            _previewWorld = new World("PreviewWorld", WorldFlags.Game);
            World.DefaultGameObjectInjectionWorld = _previewWorld;

            // Use default systems for preview (headless profile)
            var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(_previewWorld, systems);

            _isPreviewActive = true;
        }

        private void StopPreview()
        {
            if (_previewWorld != null && _previewWorld.IsCreated)
            {
                _previewWorld.Dispose();
                _previewWorld = null;
            }
            _isPreviewActive = false;
        }

        private void SyncEditorToPreview(ref SystemState state)
        {
            // Sync entities from editor world to preview world
            // This would copy relevant entities/components
        }

        private void UpdatePreviewWorld()
        {
            if (_previewWorld == null || !_previewWorld.IsCreated) return;

            var time = _previewWorld.Unmanaged.Time;
            time.UpdateTime(UnityEngine.Time.deltaTime);
            _previewWorld.Unmanaged.Time = time;

            _previewWorld.Update();
        }
    }

    /// <summary>
    /// State component controlling preview simulation.
    /// </summary>
    public struct PreviewSimulationState : IComponentData
    {
        public bool Enabled;
    }
}

