#if UNITY_EDITOR && INCLUDE_SPACE4X_IN_PUREDOTS
using System.IO;
using PureDOTS.Authoring;
using PureDOTS.Runtime.Resource;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Scenes;
using Unity.Scenes.Editor;

public static class Space4XConfigBootstrapper
{
    private const string ConfigFolder = "Assets/Space4X/Config";
    private const string RuntimeConfigPath = ConfigFolder + "/PureDotsRuntimeConfig.asset";
    private const string ResourceCatalogPath = ConfigFolder + "/PureDotsResourceTypes.asset";
    private const string RecipeCatalogPath = ConfigFolder + "/ResourceRecipeCatalog.asset";
    private const string SpatialProfilePath = ConfigFolder + "/DefaultSpatialPartitionProfile.asset";

    [MenuItem("Coplay/Space4X/Ensure PureDOTS Config Assets")]
    public static void EnsureAssets()
    {
        Directory.CreateDirectory(ConfigFolder);

        var resourceCatalog = LoadOrCreate<ResourceTypeCatalog>(ResourceCatalogPath, "PureDotsResourceTypes");
        EnsureResourceCatalogContents(resourceCatalog);

        var recipeCatalog = LoadOrCreate<ResourceRecipeCatalog>(RecipeCatalogPath, "ResourceRecipeCatalog");
        EnsureRecipeCatalogContents(recipeCatalog);

        var runtimeConfig = LoadOrCreate<PureDotsRuntimeConfig>(RuntimeConfigPath, "PureDotsRuntimeConfig");
        EnsureRuntimeConfigContents(runtimeConfig, resourceCatalog, recipeCatalog);

        var spatialProfile = LoadOrCreate<SpatialPartitionProfile>(SpatialProfilePath, "DefaultSpatialPartitionProfile");
        EnsureSpatialProfileContents(spatialProfile);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Space4X PureDOTS config assets ensured.");
    }

    [MenuItem("Coplay/Space4X/Configure SubScene Anchor")]
    public static void ConfigureSubSceneAnchor()
    {
        const string anchorName = "Space4X Registry SubScene";
        const string subScenePath = "Assets/Scenes/Demo/Space4XRegistryDemo_SubScene.unity";

        var anchor = GameObject.Find(anchorName);
        if (anchor == null)
        {
            anchor = new GameObject(anchorName);
        }

        var subScene = anchor.GetComponent<SubScene>();
        if (subScene == null)
        {
            subScene = anchor.AddComponent<SubScene>();
        }

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(subScenePath);
        if (sceneAsset == null)
        {
            Debug.LogError($"Unable to locate subscene asset at '{subScenePath}'.");
            return;
        }

        subScene.SceneAsset = sceneAsset;
        subScene.AutoLoadScene = true;
        EditorUtility.SetDirty(subScene);

#if UNITY_EDITOR
        if (!subScene.IsLoaded)
        {
            SubSceneUtility.EditScene(subScene);
        }
#endif
    }

    private static T LoadOrCreate<T>(string path, string assetName) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        asset.name = assetName;
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureResourceCatalogContents(ResourceTypeCatalog catalog)
    {
        if (catalog == null)
        {
            return;
        }

        var serialized = new SerializedObject(catalog);
        var entriesProp = serialized.FindProperty("entries");
        if (entriesProp == null)
        {
            return;
        }

        entriesProp.ClearArray();

        AddResourceEntry(entriesProp, 0, "food", new Color(0.93f, 0.75f, 0.2f, 1f));
        AddResourceEntry(entriesProp, 1, "ice", new Color(0.7f, 0.88f, 0.98f, 1f));
        AddResourceEntry(entriesProp, 2, "metals", new Color(0.58f, 0.6f, 0.64f, 1f));
        AddResourceEntry(entriesProp, 3, "nobels", new Color(0.85f, 0.72f, 0.32f, 1f));
        AddResourceEntry(entriesProp, 4, "supplies", new Color(0.85f, 0.67f, 0.45f, 1f));
        AddResourceEntry(entriesProp, 5, "wires", new Color(0.85f, 0.85f, 0.45f, 1f));
        AddResourceEntry(entriesProp, 6, "fasteners", new Color(0.7f, 0.72f, 0.75f, 1f));
        AddResourceEntry(entriesProp, 7, "components", new Color(0.5f, 0.8f, 0.9f, 1f));
        AddResourceEntry(entriesProp, 8, "furniture", new Color(0.6f, 0.45f, 0.3f, 1f));
        AddResourceEntry(entriesProp, 9, "fur_coats", new Color(0.78f, 0.7f, 0.6f, 1f));
        AddResourceEntry(entriesProp, 10, "luxury_goods", new Color(0.9f, 0.82f, 0.35f, 1f));

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }

    private static void AddResourceEntry(SerializedProperty entriesProp, int index, string id, Color color)
    {
        entriesProp.InsertArrayElementAtIndex(index);
        var element = entriesProp.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("id").stringValue = id;
        element.FindPropertyRelative("displayColor").colorValue = color;
    }

    private static void EnsureRuntimeConfigContents(PureDotsRuntimeConfig runtimeConfig, ResourceTypeCatalog resourceTypes, ResourceRecipeCatalog recipeCatalog)
    {
        if (runtimeConfig == null)
        {
            return;
        }

        var serialized = new SerializedObject(runtimeConfig);

        var timeProp = serialized.FindProperty("_time");
        if (timeProp != null)
        {
            timeProp.FindPropertyRelative("fixedDeltaTime").floatValue = 1f / 60f;
            timeProp.FindPropertyRelative("defaultSpeedMultiplier").floatValue = 1f;
            timeProp.FindPropertyRelative("pauseOnStart").boolValue = false;
        }

        var historyProp = serialized.FindProperty("_history");
        if (historyProp != null)
        {
            historyProp.FindPropertyRelative("defaultStrideSeconds").floatValue = 5f;
            historyProp.FindPropertyRelative("criticalStrideSeconds").floatValue = 1f;
            historyProp.FindPropertyRelative("lowVisibilityStrideSeconds").floatValue = 30f;
            historyProp.FindPropertyRelative("defaultHorizonSeconds").floatValue = 60f;
            historyProp.FindPropertyRelative("midHorizonSeconds").floatValue = 300f;
            historyProp.FindPropertyRelative("extendedHorizonSeconds").floatValue = 600f;
            historyProp.FindPropertyRelative("checkpointIntervalSeconds").floatValue = 20f;
            historyProp.FindPropertyRelative("eventLogRetentionSeconds").floatValue = 30f;
            historyProp.FindPropertyRelative("memoryBudgetMegabytes").floatValue = 1024f;
            historyProp.FindPropertyRelative("defaultTicksPerSecond").floatValue = 90f;
            historyProp.FindPropertyRelative("minTicksPerSecond").floatValue = 60f;
            historyProp.FindPropertyRelative("maxTicksPerSecond").floatValue = 120f;
            historyProp.FindPropertyRelative("strideScale").floatValue = 1f;
        }

        var resourceTypesProp = serialized.FindProperty("_resourceTypes");
        if (resourceTypesProp != null)
        {
            resourceTypesProp.objectReferenceValue = resourceTypes;
        }

        var recipeProp = serialized.FindProperty("_recipeCatalog");
        if (recipeProp != null)
        {
            recipeProp.objectReferenceValue = recipeCatalog;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(runtimeConfig);
    }

    private static void EnsureRecipeCatalogContents(ResourceRecipeCatalog catalog)
    {
        if (catalog == null)
        {
            return;
        }

        var serialized = new SerializedObject(catalog);
        var familiesProp = serialized.FindProperty("_families");
        if (familiesProp != null)
        {
            familiesProp.ClearArray();
        }

        var recipesProp = serialized.FindProperty("_recipes");
        if (recipesProp != null)
        {
            recipesProp.ClearArray();
            var recipe = AddRecipe(recipesProp, "fabricate_supplies", ResourceRecipeKind.Composite, "fabricator", "supplies", 1, 6f,
                "Fabricate crew supplies from metals and consumables.");
            AddIngredient(recipe, "metals", 1);
            AddIngredient(recipe, "food", 1);
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }

    private static SerializedProperty AddRecipe(SerializedProperty recipes, string id, ResourceRecipeKind kind,
        string facilityTag, string outputResourceId, int outputAmount, float processSeconds, string notes)
    {
        var index = recipes.arraySize;
        recipes.InsertArrayElementAtIndex(index);
        var recipe = recipes.GetArrayElementAtIndex(index);
        recipe.FindPropertyRelative("id").stringValue = id;
        recipe.FindPropertyRelative("kind").enumValueIndex = (int)kind;
        recipe.FindPropertyRelative("facilityTag").stringValue = facilityTag;
        recipe.FindPropertyRelative("outputResourceId").stringValue = outputResourceId;
        recipe.FindPropertyRelative("outputAmount").intValue = outputAmount;
        recipe.FindPropertyRelative("processSeconds").floatValue = processSeconds;
        recipe.FindPropertyRelative("notes").stringValue = notes ?? string.Empty;
        return recipe;
    }

    private static void AddIngredient(SerializedProperty recipe, string resourceId, int amount)
    {
        var inputs = recipe.FindPropertyRelative("inputs");
        var index = inputs.arraySize;
        inputs.InsertArrayElementAtIndex(index);
        var ingredient = inputs.GetArrayElementAtIndex(index);
        ingredient.FindPropertyRelative("resourceId").stringValue = resourceId;
        ingredient.FindPropertyRelative("amount").intValue = amount;
    }

    private static void EnsureSpatialProfileContents(SpatialPartitionProfile profile)
    {
        if (profile == null)
        {
            return;
        }

        var serialized = new SerializedObject(profile);
        serialized.FindProperty("_center").vector3Value = Vector3.zero;
        serialized.FindProperty("_extent").vector3Value = new Vector3(512f, 64f, 512f);
        serialized.FindProperty("_cellSize").floatValue = 4f;
        serialized.FindProperty("_minCellSize").floatValue = 1f;
        serialized.FindProperty("_overrideCellCounts").boolValue = false;
        serialized.FindProperty("_manualCellCounts").vector3IntValue = new Vector3Int(128, 1, 128);
        serialized.FindProperty("_lockYAxisToOne").boolValue = true;
        serialized.FindProperty("_providerType").enumValueIndex = (int)SpatialProviderType.HashedGrid;
        serialized.FindProperty("_hashSeed").uintValue = 0;
        serialized.FindProperty("_drawGizmo").boolValue = true;
        serialized.FindProperty("_gizmoColor").colorValue = new Color(0.1f, 0.8f, 1f, 0.35f);

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);
    }
}
#endif
