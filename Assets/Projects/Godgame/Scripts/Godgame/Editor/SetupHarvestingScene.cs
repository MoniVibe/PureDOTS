using UnityEditor;
using UnityEngine;
using Godgame.Authoring;
using PureDOTS.Authoring;
using PureDOTS.Runtime.Components;

namespace Godgame.Editor
{
    /// <summary>
    /// Editor utility to set up harvesting scene components.
    /// </summary>
    public static class SetupHarvestingScene
    {
        [MenuItem("Godgame/Setup Harvesting Scene")]
        public static void SetupScene()
        {
            // Add ResourceNodeAuthoring to wood nodes
            var woodNodes = new[] { "ResourceNode_Wood1", "ResourceNode_Wood2", "ResourceNode_Wood3" };
            foreach (var nodeName in woodNodes)
            {
                var go = GameObject.Find(nodeName);
                if (go == null)
                {
                    continue;
                }

                if (go.GetComponent<ResourceNodeAuthoring>() == null)
                {
                    var component = go.AddComponent<ResourceNodeAuthoring>();
                    // Set resource type to Wood via reflection or serialized property
                    var so = new SerializedObject(component);
                    so.FindProperty("resourceType").enumValueIndex = 1; // Wood = 1
                    so.ApplyModifiedProperties();
                    Debug.Log($"Added ResourceNodeAuthoring (Wood) to {nodeName}");
                }

                EnsurePlaceholder(go, PlaceholderVisualKind.Vegetation, new Color(0.25f, 0.75f, 0.35f), 1.1f);
            }

            // Add ResourceNodeAuthoring to ore nodes
            var oreNodes = new[] { "OreNode1", "OreNode2" };
            foreach (var nodeName in oreNodes)
            {
                var go = GameObject.Find(nodeName);
                if (go == null)
                {
                    continue;
                }

                if (go.GetComponent<ResourceNodeAuthoring>() == null)
                {
                    var component = go.AddComponent<ResourceNodeAuthoring>();
                    var so = new SerializedObject(component);
                    so.FindProperty("resourceType").enumValueIndex = 2; // Ore = 2
                    so.ApplyModifiedProperties();
                    Debug.Log($"Added ResourceNodeAuthoring (Ore) to {nodeName}");
                }

                EnsurePlaceholder(go, PlaceholderVisualKind.Barrel, new Color(0.45f, 0.38f, 0.32f), 1.4f);
            }

            // Add StorehouseAuthoring to Storehouse
            var storehouse = GameObject.Find("Storehouse");
            if (storehouse != null)
            {
                if (storehouse.GetComponent<StorehouseAuthoring>() == null)
                {
                    storehouse.AddComponent<StorehouseAuthoring>();
                    Debug.Log("Added StorehouseAuthoring to Storehouse");
                }

                EnsurePlaceholder(storehouse, PlaceholderVisualKind.Crate, new Color(0.7f, 0.55f, 0.3f), 2f);
            }

            Debug.Log("Harvesting scene setup complete!");
        }

        private static void EnsurePlaceholder(GameObject go, PlaceholderVisualKind kind, Color baseColor, float baseScale)
        {
            var placeholder = go.GetComponent<PlaceholderVisualAuthoring>();
            if (placeholder == null)
            {
                placeholder = go.AddComponent<PlaceholderVisualAuthoring>();
            }

            placeholder.kind = kind;
            placeholder.baseColor = baseColor;
            placeholder.baseScale = baseScale;
            placeholder.localOffset = Vector3.zero;
            placeholder.enforceTransformScale = true;

            PlaceholderVisualUtility.EnsurePlaceholderVisual(go, placeholder);
        }
    }
}



