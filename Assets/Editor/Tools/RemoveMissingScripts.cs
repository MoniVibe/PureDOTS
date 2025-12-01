#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RemoveMissingScripts
{
    [MenuItem("Tools/Cleanup/Remove Missing Scripts In Scene")]
    public static void RunScene()
    {
        int count = 0;
        foreach (var go in Object.FindObjectsOfType<GameObject>())
        {
            count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }

        Debug.Log($"[Cleanup] Removed {count} missing scripts from scene.");
    }

    [MenuItem("Tools/Cleanup/Remove Missing Scripts In Project")]
    public static void RunProject()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab");
        int total = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab)
            {
                continue;
            }

            total += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
        }

        Debug.Log($"[Cleanup] Removed {total} missing scripts from prefabs.");
    }
}
#endif
