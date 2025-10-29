using UnityEditor;
using UnityEngine;
using Unity.Scenes;

public static class TempSubSceneLinker
{
    public static void Execute()
    {
        var go = GameObject.Find("RewindSandbox (root)/SimulationSubScene");
        if (go == null)
        {
            Debug.LogError("SimulationSubScene GameObject not found at path 'RewindSandbox (root)/SimulationSubScene'.");
            return;
        }
        var sub = go.GetComponent<SubScene>();
        if (sub == null)
        {
            Debug.LogError("SubScene component missing on 'SimulationSubScene'.");
            return;
        }
        // Prefer existing SubScene in Validation folder, otherwise fall back to Assets root copy
        var primaryPath = "Assets/Scenes/Validation/SubScenes/RewindSandboxSubScene.unity/RewindSandboxSubScene.unity";
        var fallbackPath = "Assets/RewindSandboxSubScene.unity";
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(primaryPath);
        if (sceneAsset == null)
            sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(fallbackPath);
        if (sceneAsset == null)
        {
            Debug.LogError($"Could not load SceneAsset at '{primaryPath}' or fallback '{fallbackPath}'.");
            return;
        }
        Undo.RecordObject(sub, "Assign SubScene SceneAsset");
        sub.SceneAsset = sceneAsset;
        sub.AutoLoadScene = true;
        EditorUtility.SetDirty(sub);
        AssetDatabase.SaveAssets();
        Debug.Log("Linked SubScene to asset: " + AssetDatabase.GetAssetPath(sceneAsset));
    }
}
