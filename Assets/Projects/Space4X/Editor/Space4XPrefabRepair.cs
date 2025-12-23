#if UNITY_EDITOR && INCLUDE_SPACE4X_IN_PUREDOTS
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Space4X.Editor
{
    /// <summary>
    /// Repairs existing prefabs and bindings: fixes sockets/components on legacy prefabs.
    /// </summary>
    public static class Space4XPrefabRepair
    {
        public static void RepairAllPrefabs()
        {
            Debug.Log("[Space4XPrefabRepair] Repairing all prefabs...");
            
            // Find all prefabs that might need repair
            var prefabGuids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Projects/Space4X/Prefabs" });
            var repairedCount = 0;

            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                if (RepairPrefab(prefab, path))
                {
                    repairedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Space4XPrefabRepair] Repaired {repairedCount} prefabs.");
        }

        public static void RepairBindings()
        {
            Debug.Log("[Space4XPrefabRepair] Repairing bindings...");
            
            // Regenerate bindings to fix any inconsistencies
            Space4XBindingGenerator.GenerateMinimalBindings();
            Space4XBindingGenerator.GenerateFancyBindings();
            
            Debug.Log("[Space4XPrefabRepair] Bindings repaired.");
        }

        private static bool RepairPrefab(GameObject prefab, string path)
        {
            var repaired = false;
            
            // Check for missing components that should exist based on prefab name/tags
            // This is a placeholder - actual repair logic would be more sophisticated
            
            // Example: If prefab has "Weapon" in name, ensure it has WeaponMount component
            if (prefab.name.Contains("Weapon") || prefab.name.Contains("weapon"))
            {
                // Would check for WeaponMount authoring component and add if missing
                // For now, just log
                Debug.Log($"[Space4XPrefabRepair] Found weapon prefab: {prefab.name}");
            }

            // Example: If prefab has "Hull" in name, ensure socket components exist
            if (prefab.name.Contains("Hull") || prefab.name.Contains("hull"))
            {
                // Would check for socket markers and add if missing
                Debug.Log($"[Space4XPrefabRepair] Found hull prefab: {prefab.name}");
            }

            return repaired;
        }
    }
}
#endif
