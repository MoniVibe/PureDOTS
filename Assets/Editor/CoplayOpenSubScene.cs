#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Scenes;

public static class CoplayOpenSubScene
{
    // Entry point for MCP execute_script
    public static void Execute()
    {
        try
        {
            var go = GameObject.Find("GameEntities_SubScene");
            if (go == null)
            {
                Debug.LogError("Coplay: 'GameEntities_SubScene' not found in the active scene.");
                return;
            }
            var sub = go.GetComponent<SubScene>();
            if (sub == null)
            {
                Debug.LogError("Coplay: SubScene component not found on 'GameEntities_SubScene'.");
                return;
            }

            Debug.Log("Coplay: Found SubScene component. Attempting to open via editor utility.");

            // Try Editor utility first
            var utilType = Type.GetType("Unity.Scenes.Editor.SubSceneInspectorUtility, Unity.Scenes.Editor")
                           ?? Type.GetType("Unity.Scenes.Editor.SubSceneInspectorUtility");
            bool opened = false;
            if (utilType != null)
            {
                Debug.Log($"Coplay: SubSceneInspectorUtility type found: {utilType.FullName}");
                var editMethod = utilType.GetMethod("EditScene", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (editMethod == null)
                {
                    // Dump available static methods for debugging
                    var methods = utilType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    foreach (var m in methods)
                        Debug.Log($"Coplay: SubSceneInspectorUtility method available: {m.Name}");
                }
                if (editMethod != null)
                {
                    editMethod.Invoke(null, new object[] { sub });
                    opened = true;
                    Debug.Log("Coplay: Opened SubScene via SubSceneInspectorUtility.EditScene.");
                }
                else
                {
                    // Try common alternative names
                    var openM = utilType.GetMethod("OpenSubScene", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                               ?? utilType.GetMethod("OpenScene", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                               ?? utilType.GetMethod("Open", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (openM != null)
                    {
                        var pars = openM.GetParameters();
                        if (pars.Length == 1 && pars[0].ParameterType.IsAssignableFrom(typeof(SubScene)))
                        {
                            openM.Invoke(null, new object[] { sub });
                            opened = true;
                            Debug.Log($"Coplay: Opened SubScene via SubSceneInspectorUtility.{openM.Name}.");
                        }
                    }
                }
            }
            else
            {
                Debug.Log("Coplay: SubSceneInspectorUtility type not found.");
            }

            // Fallback: open the referenced scene asset additively via SerializedObject
            if (!opened)
            {
                var so = new SerializedObject(sub);
                var sceneAssetProp = so.FindProperty("_SceneAsset");
                var sceneAsset = sceneAssetProp != null ? sceneAssetProp.objectReferenceValue : null;
                Debug.Log($"Coplay: Serialized '_SceneAsset' is {(sceneAsset != null ? "set" : "null")}.");
                if (sceneAsset == null)
                {
                    // Some versions expose a property called SceneAsset
                    var prop = sub.GetType().GetProperty("SceneAsset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null)
                    {
                        sceneAsset = prop.GetValue(sub) as UnityEngine.Object;
                        Debug.Log($"Coplay: Reflected SceneAsset property is {(sceneAsset != null ? "set" : "null")}.");
                    }
                }
                if (sceneAsset != null)
                {
                    var path = AssetDatabase.GetAssetPath(sceneAsset);
                    Debug.Log($"Coplay: Resolved SubScene path: {path}");
                    if (!string.IsNullOrEmpty(path))
                    {
                        EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                        opened = true;
                        Debug.Log($"Coplay: Opened SubScene additively at '{path}'.");
                    }
                }
            }

            // Last resort: known path convention
            if (!opened)
            {
                const string knownPath = "Assets/Scenes/GameEntities.unity";
                if (System.IO.File.Exists(knownPath))
                {
                    EditorSceneManager.OpenScene(knownPath, OpenSceneMode.Additive);
                    opened = true;
                    Debug.Log($"Coplay: Opened SubScene by known path '{knownPath}'.");
                }
            }

            if (opened)
            {
                // Ensure target subscene is loaded
                var target = EditorSceneManager.GetSceneByPath("Assets/Scenes/GameEntities.unity");
                if (!target.IsValid())
                {
                    target = EditorSceneManager.OpenScene("Assets/Scenes/GameEntities.unity", OpenSceneMode.Additive);
                }

                if (target.IsValid())
                {
                    // First, move any existing children of the SubScene wrapper
                    var parent = go.transform;
                    int childCount = parent.childCount;
                    var toMove = new GameObject[childCount];
                    for (int i = 0; i < childCount; i++)
                        toMove[i] = parent.GetChild(i).gameObject;

                    int moved = 0;
                    foreach (var c in toMove)
                    {
                        c.transform.SetParent(null, true); // make it a root in the main scene
                        EditorSceneManager.MoveGameObjectToScene(c, target);
                        moved++;
                    }

                    // Then, move specific known entities by name from the main scene
                    string[] names = new[]
                    {
                        "ResourceNode_Wood1","ResourceNode_Wood2","ResourceNode_Wood3",
                        "OreNode1","OreNode2",
                        "Asteroid_Ore1","Asteroid_Ore2","Asteroid_Ore3",
                        "Storehouse"
                    };

                    foreach (var n in names)
                    {
                        var goByName = GameObject.Find(n);
                        if (goByName != null && goByName.scene.path.Contains("SpawnerDemoScene"))
                        {
                            goByName.transform.SetParent(null, true);
                            EditorSceneManager.MoveGameObjectToScene(goByName, target);
                            moved++;
                        }
                    }

                    // Move all GameObjects named exactly "Villager"
                    foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
                    {
                        if (t != null && t.gameObject != null && t.gameObject.name == "Villager" && t.gameObject.scene.path.Contains("SpawnerDemoScene"))
                        {
                            t.SetParent(null, true);
                            EditorSceneManager.MoveGameObjectToScene(t.gameObject, target);
                            moved++;
                        }
                    }

                    Debug.Log($"Coplay: Moved {moved} GameObjects into subscene '{target.path}'.");

                    // Log roots in target scene
                    var roots = target.GetRootGameObjects();
                    Debug.Log($"Coplay: Target scene now has {roots.Length} root objects. Sample: {(roots.Length>0?roots[0].name:"<none>")}");

                    EditorSceneManager.SaveOpenScenes();
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    Debug.LogError("Coplay: Target subscene not valid after open.");
                }
            }
            else
            {
                Debug.LogError("Coplay: Failed to open SubScene by any method.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Coplay: Exception while opening SubScene: " + ex);
        }
    }
}
#endif
