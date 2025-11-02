using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using PureDOTS.Authoring;
using PureDOTS.Runtime.Components;

public static class ApplyPlaceholderVisuals
{
    [MenuItem("Tools/PureDOTS/Apply Placeholder Visuals (SubScene)")]
    public static void Execute()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (!scene.IsValid()) { Debug.LogError("Active scene invalid."); return; }

        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogWarning("URP/Lit shader not found. Materials will keep current shader.");
        }

        var placeholders = Object.FindObjectsByType<PlaceholderVisualAuthoring>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var placeholder in placeholders)
        {
            ApplyDefaultAppearance(placeholder);
            PlaceholderVisualUtility.EnsurePlaceholderVisual(placeholder.gameObject, placeholder, urpLit);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        AssetDatabase.Refresh();
        Debug.Log($"Applied placeholder visuals to {placeholders.Length} objects.");
    }

    private static void ApplyDefaultAppearance(PlaceholderVisualAuthoring authoring)
    {
        const float defaultColorComponent = 0.78f;
        var usesDefaultColor = Mathf.Approximately(authoring.baseColor.r, defaultColorComponent) &&
                               Mathf.Approximately(authoring.baseColor.g, defaultColorComponent) &&
                               Mathf.Approximately(authoring.baseColor.b, defaultColorComponent);

        if (authoring.baseScale <= 0f)
        {
            authoring.baseScale = 1f;
        }

        if (usesDefaultColor)
        {
            switch (authoring.kind)
            {
                case PlaceholderVisualKind.Crate:
                    authoring.baseColor = new Color(0.75f, 0.55f, 0.3f, 1f);
                    break;
                case PlaceholderVisualKind.Barrel:
                    authoring.baseColor = new Color(0.5f, 0.4f, 0.3f, 1f);
                    authoring.baseScale = Mathf.Max(authoring.baseScale, 1.2f);
                    break;
                case PlaceholderVisualKind.Miracle:
                    authoring.baseColor = new Color(0.7f, 0.9f, 1.2f, 1f);
                    authoring.baseScale = Mathf.Max(authoring.baseScale, 0.7f);
                    break;
                case PlaceholderVisualKind.Vegetation:
                    authoring.baseColor = new Color(0.2f, 0.8f, 0.3f, 1f);
                    authoring.baseScale = Mathf.Max(authoring.baseScale, 0.6f);
                    break;
                default:
                    authoring.baseColor = new Color(0.6f, 0.6f, 0.6f, 1f);
                    break;
            }
        }
    }
}
#endif
