using Unity.Entities;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PureDOTS.Runtime.BlobAssets
{
    /// <summary>
    /// Centralized blob asset registry to prevent duplicate creation.
    /// </summary>
    public static class BlobRegistry
    {
        private static bool _initialized = false;

        /// <summary>
        /// Initializes blob assets (materials, skills, doctrines).
        /// </summary>
        public static void Initialize(World world)
        {
            if (_initialized)
            {
            UnityEngine.Debug.LogWarning("[BlobRegistry] Already initialized, skipping.");
                return;
            }

            var entityManager = world.EntityManager;
            
            // Blob assets are typically created during authoring/baking
            // This registry ensures they're available and prevents duplicates
            // Actual blob creation happens via authoring components
            
            _initialized = true;
            UnityEngine.Debug.Log("[BlobRegistry] Blob registry initialized.");
        }
    }
}

