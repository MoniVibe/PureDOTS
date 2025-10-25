using UnityEditor;
using UnityEngine;
using PureDOTS.Authoring;
using PureDOTS.Runtime.Components;

public static class CreateSpawnProfileAsset
{
    public static void Execute()
    {
        var path = "Assets/Scenes/Validation/Prefabs/SceneSpawnProfile.asset";
        var profile = ScriptableObject.CreateInstance<SceneSpawnProfileAsset>();
        profile.seed = 12345u;

        // Villagers
        profile.entries.Add(new SceneSpawnEntryDefinition
        {
            category = SceneSpawnCategory.Villager,
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PureDOTS/Prefabs/Villager.prefab"),
            count = 6,
            placement = SpawnPlacementMode.RandomCircle,
            rotation = SpawnRotationMode.RandomYaw,
            radius = 10f,
            payloadId = "peasant"
        });

        // Vegetation
        profile.entries.Add(new SceneSpawnEntryDefinition
        {
            category = SceneSpawnCategory.Vegetation,
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scenes/Validation/Prefabs/Vegetation_Sprout.prefab"),
            count = 20,
            placement = SpawnPlacementMode.Grid,
            gridDimensions = new Vector2Int(5,4),
            gridSpacing = new Vector2(2.5f,2.5f),
            rotation = SpawnRotationMode.RandomYaw,
            payloadId = "sprout"
        });

        // Miracles
        profile.entries.Add(new SceneSpawnEntryDefinition
        {
            category = SceneSpawnCategory.Miracle,
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scenes/Validation/Prefabs/Miracle.prefab"),
            count = 2,
            placement = SpawnPlacementMode.Ring,
            radius = 8f,
            innerRadius = 4f,
            rotation = SpawnRotationMode.Identity,
            payloadId = "blessing"
        });

        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        Debug.Log("Created SceneSpawnProfile at " + path);
    }
}
