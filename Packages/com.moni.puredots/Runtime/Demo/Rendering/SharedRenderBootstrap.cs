using Unity.Entities;

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
    /// Singleton component that holds the shared render mesh array for demo entities.
    /// Host games must populate this singleton with meshes at the indices defined in DemoMeshIndices.
    /// </summary>
    [System.Serializable]
    public struct RenderMeshArraySingleton : ISharedComponentData, System.IEquatable<RenderMeshArraySingleton>
    {
        public Unity.Rendering.RenderMeshArray Value;

        public bool Equals(RenderMeshArraySingleton other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is RenderMeshArraySingleton other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value != null ? Value.GetHashCode() : 0;
        }
    }

    /// <summary>
    /// Bootstrap system that initializes the shared render mesh array singleton.
    /// Provides stable indices for demo meshes (cubes/capsules with Simple Lit materials).
    /// Host games should populate the RenderMeshArray with appropriate meshes at DemoMeshIndices.
    /// </summary>
    [Unity.Entities.UpdateInGroup(typeof(Unity.Entities.InitializationSystemGroup))]
    public partial struct SharedRenderBootstrap : Unity.Entities.ISystem
    {
        public void OnCreate(ref Unity.Entities.SystemState state)
        {
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

            UnityEngine.Debug.Log($"[SharedRenderBootstrap] RenderMeshArray singleton created in world: {state.WorldUnmanaged.Name}. Shader: Universal Render Pipeline/Simple Lit");
        }

        public void OnUpdate(ref Unity.Entities.SystemState state)
        {
            // One-time bootstrap, no updates needed
        }
    }
}

