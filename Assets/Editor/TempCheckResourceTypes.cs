using UnityEditor;
using UnityEngine;
using PureDOTS.Authoring;

public static class TempCheckResourceTypes
{
    [MenuItem("Tools/Temp/Check Resource Types")] 
    public static void Execute()
    {
        var guid = "6f8227095a7a4f2f8dff92367be7e55d";
        var resolvedPath = AssetDatabase.GUIDToAssetPath(guid);
        Debug.Log($"GUID {guid} resolves to '{resolvedPath}'");

        var catalog = AssetDatabase.LoadAssetAtPath<ResourceTypeCatalog>("Assets/PureDOTS/Config/PureDotsResourceTypes.asset");
        Debug.Log(catalog == null ? "Catalog asset not found." : $"Catalog entries count: {(catalog.entries != null ? catalog.entries.Count : -1)}");
        if (catalog != null && catalog.entries != null)
        {
            for (int i = 0; i < catalog.entries.Count; i++)
            {
                var entry = catalog.entries[i];
                Debug.Log($"[{i}] id={entry.id} color={entry.displayColor}");
            }
        }
    }
}
