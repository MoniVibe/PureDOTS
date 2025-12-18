#if PUREDOTS_SCENARIO
using Unity.Entities;
using Unity.Rendering;

namespace PureDOTS.Demo.Rendering
{
    /// <summary>
    /// Centralized mesh and material index constants for the PureDOTS demo systems.
    /// These indices correspond to positions in the RenderMeshArray used by demo entities.
    /// </summary>
    public static class DemoMeshIndices
    {
        /// <summary>
        /// Mesh index for village ground/terrain (index 0 in RenderMeshArray).
        /// </summary>
        public const int VillageGroundMeshIndex = 0;

        /// <summary>
        /// Mesh index for village home structures (index 1 in RenderMeshArray).
        /// </summary>
        public const int VillageHomeMeshIndex = 1;

        /// <summary>
        /// Mesh index for village workplace structures (index 2 in RenderMeshArray).
        /// </summary>
        public const int VillageWorkMeshIndex = 2;

        /// <summary>
        /// Mesh index for villager entities and orbit cubes (index 3 in RenderMeshArray).
        /// </summary>
        public const int VillageVillagerMeshIndex = 3;

        /// <summary>
        /// Material index for demo entities (typically 0, using Simple Lit shader).
        /// </summary>
        public const int DemoMaterialIndex = 0;
    }

    /// <summary>
    /// DEMO-ONLY: Bootstrap system that initializes shared render mesh array for demo systems.
    /// This is an example implementation for testing PureDOTS demo functionality.
    ///
    /// IMPORTANT: Real games should NOT use this system. Instead:
    /// - Use game-specific RenderKey components
    /// - Implement RenderCatalogAuthoring/Baker for your game's render data
    /// - Use ApplyRenderCatalogSystem to assign render components
    ///
    /// Only runs when PureDOTS demo profile is active.
    /// </summary>
    [DisableAutoCreation]
    [Unity.Entities.UpdateInGroup(typeof(Unity.Entities.InitializationSystemGroup))]
    public partial struct SharedRenderBootstrap : Unity.Entities.ISystem
    {
        public void OnCreate(ref Unity.Entities.SystemState state)
        {
            // Only run in demo worlds
            var activeProfile = PureDOTS.Systems.SystemRegistry.ResolveActiveProfile();
            if (activeProfile.Id != PureDOTS.Systems.SystemRegistry.BuiltinProfiles.Demo.Id)
            {
                state.Enabled = false;
                return;
            }

            // Create an empty RenderMeshArray that will be populated by host game setup
            var renderMeshArray = new Unity.Rendering.RenderMeshArray
            {
                // Meshes and materials will be set up externally by host games
                // This singleton just provides access to the array
            };

            var entityManager = state.EntityManager;
            var singletonEntity = entityManager.CreateEntity();
            entityManager.AddSharedComponentManaged(singletonEntity, new RenderMeshArraySingleton
            {
                Value = renderMeshArray
            });

            UnityEngine.Debug.Log($"[SharedRenderBootstrap] DEMO RenderMeshArray singleton created in world: {state.WorldUnmanaged.Name}. This is for demo purposes only.");
        }

        public void OnUpdate(ref Unity.Entities.SystemState state)
        {
            // One-time bootstrap, no updates needed
        }
    }
}

#endif

