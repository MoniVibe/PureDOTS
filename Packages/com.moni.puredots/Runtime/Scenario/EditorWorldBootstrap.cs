using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Runtime.Scenario
{
    /// <summary>
    /// Bootstrap for separate editor ECS world used for scenario authoring.
    /// Keeps editor entities separate from simulation to avoid chunk churn.
    /// </summary>
    public static class EditorWorldBootstrap
    {
        private static World _editorWorld;

        /// <summary>
        /// Get or create the editor world.
        /// </summary>
        public static World GetOrCreateEditorWorld()
        {
            if (_editorWorld != null && _editorWorld.IsCreated)
            {
                return _editorWorld;
            }

            _editorWorld = new World("EditorWorld", WorldFlags.Editor);
            World.DefaultGameObjectInjectionWorld = _editorWorld;

            // Register editor-specific systems
            var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Editor);
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(_editorWorld, systems);

            // Ensure basic systems exist
            _editorWorld.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            _editorWorld.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

            return _editorWorld;
        }

        /// <summary>
        /// Dispose the editor world.
        /// </summary>
        public static void DisposeEditorWorld()
        {
            if (_editorWorld != null && _editorWorld.IsCreated)
            {
                _editorWorld.Dispose();
                _editorWorld = null;
            }
        }
    }
}

