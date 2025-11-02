using Godgame.Authoring;
using PureDOTS.Authoring;
using UnityEditor;
using UnityEngine;
using StorehouseAuthoring = Godgame.Authoring.StorehouseAuthoring;

namespace Godgame.Editor
{
    public static class CreateGodgamePrefabs
    {
        [MenuItem("Godgame/Prefabs/Create Basic Storehouse")]
        public static void CreateStorehousePrefab()
        {
            var go = new GameObject("Godgame_Storehouse_Prefab");
            var authoring = go.AddComponent<StorehouseAuthoring>();
            authoring.SetLabel("Storehouse");

            var path = EditorUtility.SaveFilePanelInProject("Save Godgame storehouse prefab", "Godgame_Storehouse", "prefab", "Select location for the prefab");
            if (!string.IsNullOrEmpty(path))
            {
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }

            Object.DestroyImmediate(go);
        }

        private static void SetLabel(this StorehouseAuthoring authoring, string label)
        {
            var so = new SerializedObject(authoring);
            so.FindProperty("label").stringValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

