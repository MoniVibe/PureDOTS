using UnityEditor;
using UnityEngine;

public static class ReconvertSubScene
{
    public static void Execute()
    {
        // Import the SubScene asset to regenerate Entities cache
        var subPathPrimary = "Assets/Scenes/Validation/SubScenes/RewindSandboxSubScene.unity/RewindSandboxSubScene.unity";
        var subPathFallback = "Assets/RewindSandboxSubScene.unity";
        var path = AssetDatabase.LoadAssetAtPath<SceneAsset>(subPathPrimary) != null ? subPathPrimary : subPathFallback;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
        {
            Debug.LogError("SubScene asset not found to reconvert.");
            return;
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();
        Debug.Log("Triggered SubScene reconversion via reimport: " + path);
    }
}
