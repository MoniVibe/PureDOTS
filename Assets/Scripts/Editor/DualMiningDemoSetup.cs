#if UNITY_EDITOR
using PureDOTS.Authoring;
using PureDOTS.Runtime.Components;
using Space4X.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using Unity.Scenes;
using UnityEngine.SceneManagement;
using System.Linq;
using PureDOTS.Config;

namespace Space4X.Editor
{
    /// <summary>
    /// Automated setup tool for creating a unified dual mining demo scene showcasing both
    /// Space4X (vessel mining) and godgame (villager mining) loops side-by-side.
    /// Everything runs in a single unified scene using PureDOTS foundation.
    /// </summary>
    public static class DualMiningDemoSetup
    {
        private const string ROOT_SCENE_PATH = "Assets/Scenes/MiningDemo_Dual.unity";
        private const string AUTHORING_SUBSCENE_PATH = "Assets/Scenes/MiningDemo_Dual_Authoring.unity";
        private const string AUTHORING_SUBSCENE_OBJECT_NAME = "MiningDemo_Dual_Authoring_SubScene";
        private const string RESOURCE_CONFIG_PATH = "Assets/PureDOTS/Config/PureDotsRuntimeConfig.asset";
        private const string RESOURCE_CATALOG_PATH = "Assets/PureDOTS/Config/PureDotsResourceTypes.asset";
        private const string VILLAGER_PRESET_CATALOG_PATH = VillagerJobPresetCatalog.DefaultAssetPath;
        [MenuItem("Space4X/Setup Dual Mining Demo Scene")]
        public static void SetupDualMiningDemoScene()
        {
            Debug.Log("=== Setting Up Unified Dual Mining Demo Scene ===");

            // Step 1: Create or load root scene
            var rootScene = CreateOrLoadRootScene();
            if (!rootScene.IsValid())
            {
                Debug.LogError("Failed to create root scene!");
                return;
            }

            // Ensure root scene is active
            if (EditorSceneManager.GetActiveScene() != rootScene)
            {
                EditorSceneManager.SetActiveScene(rootScene);
            }

            // Step 2: Setup framework components (config, time controls)
            SetupFrameworkComponents();

            // Step 3: Ensure we have an authoring SubScene for DOTS baking
            var authoringScene = EnsureAuthoringSubScene(rootScene);
            var previousActiveScene = SceneManager.GetActiveScene();
            try
            {
                SceneManager.SetActiveScene(authoringScene);

                // Step 4: Setup terrain (ground plane for visibility)
                SetupTerrain(authoringScene);

                // Step 5: Setup villagers and godgame entities (needs, tasks, resources, storehouse, worship)
                SetupVillagerEntities(authoringScene);

                // Step 6: Setup Space4X entities (carriers, vessels, asteroids)
                SetupSpace4XEntities(authoringScene);

                EditorSceneManager.SaveScene(authoringScene);
            }
            finally
            {
                SceneManager.SetActiveScene(previousActiveScene);
                EditorSceneManager.CloseScene(authoringScene, true);
            }

            // Step 7: Save root scene
            EditorSceneManager.SaveScene(rootScene);

            Debug.Log("=== Unified Dual Mining Demo Scene Setup Complete ===");
            EditorUtility.DisplayDialog("Setup Complete",
                "✅ Created unified scene with:\n" +
                "  • Terrain for movement visibility\n" +
                "  • Villagers with needs and task loops\n" +
                "  • Resource nodes, storehouse, and worship site\n" +
                "  • Carriers with mining vessels and asteroids\n" +
                "  • All using PureDOTS foundation\n\n" +
                "Enter Play Mode to see both mining loops running!",
                "OK");
        }

        private static UnityEngine.SceneManagement.Scene CreateOrLoadRootScene()
        {
            // Save any unsaved changes first
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("User cancelled scene save. Aborting setup.");
                return default;
            }

            // Check if scene exists
            var existingScene = EditorSceneManager.GetSceneByPath(ROOT_SCENE_PATH);
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                // Scene is already loaded - make sure it's active
                if (!existingScene.IsValid() || existingScene != EditorSceneManager.GetActiveScene())
                {
                    EditorSceneManager.SetActiveScene(existingScene);
                }
                Debug.Log("✅ Using existing loaded root scene");
                return existingScene;
            }

            // Scene exists but not loaded, or doesn't exist - open/create it
            UnityEngine.SceneManagement.Scene scene;
            if (System.IO.File.Exists(ROOT_SCENE_PATH))
            {
                scene = EditorSceneManager.OpenScene(ROOT_SCENE_PATH, OpenSceneMode.Single);
                Debug.Log("✅ Loaded existing root scene");
            }
            else
            {
                // Create new scene
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ROOT_SCENE_PATH);
                Debug.Log($"✅ Created new root scene at {ROOT_SCENE_PATH}");
            }

            return scene;
        }

        private static void SetupFrameworkComponents()
        {
            Debug.Log("Setting up framework components...");

            // PureDotsConfig
            var configGo = GameObject.Find("PureDotsConfig");
            if (configGo == null)
            {
                configGo = new GameObject("PureDotsConfig");
            }

            var configAuthoring = configGo.GetComponent<PureDotsConfigAuthoring>();
            if (configAuthoring == null)
            {
                configAuthoring = configGo.AddComponent<PureDotsConfigAuthoring>();
            }

            var configAsset = AssetDatabase.LoadAssetAtPath<PureDotsRuntimeConfig>(
                "Assets/PureDOTS/Config/PureDotsRuntimeConfig.asset");
            if (configAsset != null)
            {
                configAuthoring.config = configAsset;
            }

            // TimeControls
            var timeControlsGo = GameObject.Find("TimeControls");
            if (timeControlsGo == null)
            {
                timeControlsGo = new GameObject("TimeControls");
                timeControlsGo.AddComponent<TimeControlsAuthoring>();
            }

            Debug.Log("✅ Framework components setup complete");
        }

        private static void SetupSpace4XEntities(Scene authoringScene)
        {
            Debug.Log("Setting up Space4X entities...");

            var oreResourceIndex = ResolveResourceTypeIndex("iron_ore", fallbackIndex: 0);

            // Create 3-4 mining vessels (more for visibility)
            int vesselCount = 4;
            for (int i = 1; i <= vesselCount; i++)
            {
                var vesselGo = FindInScene(authoringScene, $"MiningVessel_{i:D2}") ?? new GameObject($"MiningVessel_{i:D2}");
                MoveToScene(vesselGo, authoringScene);
                // Position vessels on the left side of the scene
                vesselGo.transform.position = new Vector3(-15f - (i * 3f), 0f, (i % 2 == 0 ? 3f : -3f));
                
                var authoring = GetOrAddComponent<MiningVesselAuthoring>(vesselGo);
                authoring.baseSpeed = 5f;
                authoring.capacity = 50f;
                authoring.resourceTypeIndex = oreResourceIndex;

                // Add visual placeholder for vessel
                var vesselVisual = GetOrAddComponent<PlaceholderVisualAuthoring>(vesselGo);
                vesselVisual.kind = PlaceholderVisualKind.Crate;
                vesselVisual.baseScale = 0.8f;
                vesselVisual.baseColor = new Color(0.3f, 0.5f, 0.8f); // Blue-ish for vessels
                PlaceholderVisualUtility.EnsurePlaceholderVisual(vesselGo, vesselVisual);
            }

            // Create carrier (vessels deposit ore here)
            var carrierGo = FindInScene(authoringScene, "Carrier_Main") ?? new GameObject("Carrier_Main");
            MoveToScene(carrierGo, authoringScene);
            carrierGo.transform.position = new Vector3(-25f, 0f, 0f);
            var carrierAuthoring = GetOrAddComponent<CarrierAuthoring>(carrierGo);
            carrierAuthoring.carrierId = 1;
            carrierAuthoring.totalCapacity = 1000f;

            // Add visual placeholder for carrier
            var carrierVisual = GetOrAddComponent<PlaceholderVisualAuthoring>(carrierGo);
            carrierVisual.kind = PlaceholderVisualKind.Crate;
            carrierVisual.baseScale = 3f;
            carrierVisual.baseColor = new Color(0.4f, 0.4f, 0.6f); // Darker for carrier
            PlaceholderVisualUtility.EnsurePlaceholderVisual(carrierGo, carrierVisual);

            // Create 4-5 asteroids (ore sources for vessels to mine)
            int asteroidCount = 5;
            for (int i = 1; i <= asteroidCount; i++)
            {
                var asteroidGo = FindInScene(authoringScene, $"Asteroid_Ore_{i:D2}") ?? new GameObject($"Asteroid_Ore_{i:D2}");
                MoveToScene(asteroidGo, authoringScene);
                // Position asteroids around the vessel area
                float angle = (i / (float)asteroidCount) * 360f * Mathf.Deg2Rad;
                float radius = 15f;
                asteroidGo.transform.position = new Vector3(
                    -20f + Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );
                
                var resourceAuthoring = GetOrAddComponent<ResourceSourceAuthoring>(asteroidGo);
                resourceAuthoring.resourceTypeId = (i % 3) switch
                {
                    1 => "iron_ore",
                    2 => "hydrocarbon_ice",
                    _ => "rare_earths"
                };
                resourceAuthoring.initialUnits = 500f;
                resourceAuthoring.gatherRatePerWorker = 8f;
                resourceAuthoring.maxSimultaneousWorkers = 5;
                resourceAuthoring.debugGatherRadius = 5f;
                resourceAuthoring.respawns = true;
                resourceAuthoring.respawnSeconds = 120f;

                // Add visual placeholder for asteroid
                var asteroidVisual = GetOrAddComponent<PlaceholderVisualAuthoring>(asteroidGo);
                asteroidVisual.kind = PlaceholderVisualKind.Barrel;
                asteroidVisual.baseScale = 1.5f;
                asteroidVisual.baseColor = new Color(0.5f, 0.4f, 0.3f); // Brown/gray for asteroid
                PlaceholderVisualUtility.EnsurePlaceholderVisual(asteroidGo, asteroidVisual);
            }

            Debug.Log($"✅ Space4X entities setup complete: {vesselCount} vessels, 1 carrier, {asteroidCount} asteroids");
        }

        private static void SetupVillagerEntities(Scene authoringScene)
        {
            Debug.Log("Setting up villager entities...");

            var villagerPresets = LoadVillagerPresets(out var usingFallbackPresets);
            Debug.Log(usingFallbackPresets
                ? "Using fallback villager job presets (catalog asset not found)."
                : $"Using villager job preset catalog ({villagerPresets.Count} entries).");

            // Create 4-5 villagers with needs and task loops
            int villagerCount = 5;
            for (int i = 1; i <= villagerCount; i++)
            {
                var villagerGo = FindInScene(authoringScene, $"Villager_{i:D2}") ?? new GameObject($"Villager_{i:D2}");
                MoveToScene(villagerGo, authoringScene);
                // Position villagers on the right side of the scene
                villagerGo.transform.position = new Vector3(15f + (i * 3f), 0f, (i % 2 == 0 ? 2f : -2f));
                
                var authoring = GetOrAddComponent<VillagerAuthoring>(villagerGo);
                authoring.villagerId = i;
                var preset = villagerPresets[(i - 1) % villagerPresets.Count];
                authoring.initialJob = preset.JobType;
                authoring.baseSpeed = preset.BaseSpeed;
                authoring.initialHunger = preset.InitialHunger;
                authoring.initialEnergy = preset.InitialEnergy;
                authoring.initialMorale = preset.InitialMorale;
                authoring.startAvailableForJobs = true;

                var villagerVisual = GetOrAddComponent<PlaceholderVisualAuthoring>(villagerGo);
                villagerVisual.kind = PlaceholderVisualKind.Crate;
                villagerVisual.baseScale = 1f;
                villagerVisual.baseColor = new Color(0.85f, 0.6f, 0.25f);
                PlaceholderVisualUtility.EnsurePlaceholderVisual(villagerGo, villagerVisual);
            }

            // Create storehouse (villagers deposit resources here)
            var storehouseGo = FindInScene(authoringScene, "Storehouse_Main") ?? new GameObject("Storehouse_Main");
            MoveToScene(storehouseGo, authoringScene);
            storehouseGo.transform.position = new Vector3(25f, 0f, 0f);
            var storehouseAuthoring = GetOrAddComponent<StorehouseAuthoring>(storehouseGo);
            
            // Add capacity entries for baseline raw and processed resources
            storehouseAuthoring.capacities ??= new List<StorehouseCapacityEntry>();
            storehouseAuthoring.capacities.Clear();
            storehouseAuthoring.capacities.Add(new StorehouseCapacityEntry
            {
                resourceTypeId = "iron_ore",
                maxCapacity = 1000f
            });
            storehouseAuthoring.capacities.Add(new StorehouseCapacityEntry
            {
                resourceTypeId = "iron_ingot",
                maxCapacity = 800f
            });
            storehouseAuthoring.capacities.Add(new StorehouseCapacityEntry
            {
                resourceTypeId = "nutrients",
                maxCapacity = 600f
            });
            storehouseAuthoring.capacities.Add(new StorehouseCapacityEntry
            {
                resourceTypeId = "polymers",
                maxCapacity = 600f
            });

            // Add visual placeholder for storehouse
            var storehouseVisual = GetOrAddComponent<PlaceholderVisualAuthoring>(storehouseGo);
            storehouseVisual.kind = PlaceholderVisualKind.Crate;
            storehouseVisual.baseScale = 2f;
            storehouseVisual.baseColor = new Color(0.75f, 0.55f, 0.3f);
            PlaceholderVisualUtility.EnsurePlaceholderVisual(storehouseGo, storehouseVisual);

            // Create worship site/miracle (villagers can worship here)
            var worshipGo = FindInScene(authoringScene, "WorshipSite_Miracle") ?? new GameObject("WorshipSite_Miracle");
            MoveToScene(worshipGo, authoringScene);
            worshipGo.transform.position = new Vector3(30f, 0f, 0f);
            
            // Add miracle visual placeholder
            var miracleVisual = GetOrAddComponent<PlaceholderVisualAuthoring>(worshipGo);
            miracleVisual.kind = PlaceholderVisualKind.Miracle;
            miracleVisual.baseScale = 1.5f;
            miracleVisual.miracleBaseIntensity = 1.2f;
            miracleVisual.miraclePulseAmplitude = 0.4f;
            miracleVisual.baseColor = new Color(0.7f, 0.9f, 1.2f);
            PlaceholderVisualUtility.EnsurePlaceholderVisual(worshipGo, miracleVisual);

            // Create 4-5 resource nodes (biomass sources for villagers to gather)
            int resourceCount = 5;
            for (int i = 1; i <= resourceCount; i++)
            {
                var treeGo = FindInScene(authoringScene, $"Tree_Wood_{i:D2}") ?? new GameObject($"Tree_Wood_{i:D2}");
                MoveToScene(treeGo, authoringScene);
                // Position trees around the villager area
                float angle = (i / (float)resourceCount) * 360f * Mathf.Deg2Rad;
                float radius = 12f;
                treeGo.transform.position = new Vector3(
                    20f + Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );
                
                var resourceAuthoring = GetOrAddComponent<ResourceSourceAuthoring>(treeGo);
                resourceAuthoring.resourceTypeId = "biomass";
                resourceAuthoring.initialUnits = 200f;
                resourceAuthoring.gatherRatePerWorker = 4f;
                resourceAuthoring.maxSimultaneousWorkers = 3;
                resourceAuthoring.debugGatherRadius = 3f;
                resourceAuthoring.respawns = true;
                resourceAuthoring.respawnSeconds = 45f;

                // Add visual placeholder for tree
                var treeVisual = GetOrAddComponent<PlaceholderVisualAuthoring>(treeGo);
                treeVisual.kind = PlaceholderVisualKind.Vegetation;
                treeVisual.baseScale = 1f;
                treeVisual.baseColor = new Color(0.25f, 0.75f, 0.35f);
                PlaceholderVisualUtility.EnsurePlaceholderVisual(treeGo, treeVisual);
            }

            Debug.Log($"✅ Villager entities setup complete: {villagerCount} villagers, 1 storehouse, 1 worship site, {resourceCount} resource nodes");
        }

        private static void SetupTerrain(Scene authoringScene)
        {
            Debug.Log("Setting up terrain...");

            // Terrain/Ground Plane - create large ground for visibility
            var terrainGo = FindInScene(authoringScene, "Terrain") ?? new GameObject("Terrain");
            MoveToScene(terrainGo, authoringScene);
            terrainGo.transform.position = Vector3.zero;

            var terrainAuthoring = terrainGo.GetComponent<TerrainAuthoring>();
            if (terrainAuthoring == null)
            {
                terrainAuthoring = terrainGo.AddComponent<TerrainAuthoring>();
            }

            // Set terrain size to cover the demo area (villagers on right, vessels on left)
            terrainAuthoring.size = new float2(100f, 100f);
            terrainAuthoring.subdivisions = 20; // Smooth enough for good visuals
            
            // Try to load the ground material
            var groundMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ground_Default.mat");
            if (groundMaterial != null)
            {
                terrainAuthoring.terrainMaterial = groundMaterial;
            }

            Debug.Log("✅ Terrain setup complete");
        }

        private static Scene EnsureAuthoringSubScene(UnityEngine.SceneManagement.Scene rootScene)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AUTHORING_SUBSCENE_PATH));

            var subSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(AUTHORING_SUBSCENE_PATH);
            if (subSceneAsset == null)
            {
                var tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                EditorSceneManager.SaveScene(tempScene, AUTHORING_SUBSCENE_PATH);
                subSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(AUTHORING_SUBSCENE_PATH);
                EditorSceneManager.CloseScene(tempScene, true);
            }

            var subSceneRoot = rootScene.GetRootGameObjects()
                .FirstOrDefault(go => go.TryGetComponent<SubScene>(out var existing) && existing.SceneAsset == subSceneAsset);

            if (subSceneRoot == null)
            {
                subSceneRoot = rootScene.GetRootGameObjects()
                    .FirstOrDefault(go => go.name == AUTHORING_SUBSCENE_OBJECT_NAME && go.GetComponent<SubScene>() != null);
            }

            if (subSceneRoot == null)
            {
                subSceneRoot = new GameObject(AUTHORING_SUBSCENE_OBJECT_NAME);
                SceneManager.MoveGameObjectToScene(subSceneRoot, rootScene);
            }

            var subSceneComponent = subSceneRoot.GetComponent<SubScene>();
            if (subSceneComponent == null)
            {
                subSceneComponent = subSceneRoot.AddComponent<SubScene>();
            }

            subSceneComponent.SceneAsset = subSceneAsset;
            subSceneComponent.AutoLoadScene = true;

            var authoringScene = EditorSceneManager.GetSceneByPath(AUTHORING_SUBSCENE_PATH);
            if (!authoringScene.IsValid() || !authoringScene.isLoaded)
            {
                authoringScene = EditorSceneManager.OpenScene(AUTHORING_SUBSCENE_PATH, OpenSceneMode.Additive);
            }

            return authoringScene;
        }

        private static ushort ResolveResourceTypeIndex(string resourceTypeId, ushort fallbackIndex)
        {
            if (string.IsNullOrWhiteSpace(resourceTypeId))
            {
                Debug.LogWarning("[DualMiningDemoSetup] Resource type id is empty. Using fallback index.");
                return fallbackIndex;
            }

            ResourceTypeCatalog catalog = null;

            var runtimeConfig = AssetDatabase.LoadAssetAtPath<PureDotsRuntimeConfig>(RESOURCE_CONFIG_PATH);
            if (runtimeConfig != null)
            {
                catalog = runtimeConfig.ResourceTypes;
                var catalogIndex = FindResourceTypeIndex(catalog, resourceTypeId);
                if (catalogIndex >= 0)
                {
                    return (ushort)catalogIndex;
                }
            }

            if (catalog == null)
            {
                catalog = AssetDatabase.LoadAssetAtPath<ResourceTypeCatalog>(RESOURCE_CATALOG_PATH);
                var catalogIndex = FindResourceTypeIndex(catalog, resourceTypeId);
                if (catalogIndex >= 0)
                {
                    return (ushort)catalogIndex;
                }
            }

            var catalogGuid = AssetDatabase.FindAssets("t:PureDOTS.Authoring.ResourceTypeCatalog").FirstOrDefault();
            if (!string.IsNullOrEmpty(catalogGuid))
            {
                var path = AssetDatabase.GUIDToAssetPath(catalogGuid);
                catalog = AssetDatabase.LoadAssetAtPath<ResourceTypeCatalog>(path);
                var catalogIndex = FindResourceTypeIndex(catalog, resourceTypeId);
                if (catalogIndex >= 0)
                {
                    return (ushort)catalogIndex;
                }
            }

            Debug.LogWarning($"[DualMiningDemoSetup] Could not resolve resource type '{resourceTypeId}'. Falling back to index {fallbackIndex}.");
            return fallbackIndex;
        }

        private static int FindResourceTypeIndex(ResourceTypeCatalog catalog, string resourceTypeId)
        {
            if (catalog?.entries == null)
            {
                return -1;
            }

            for (int i = 0; i < catalog.entries.Count; i++)
            {
                var candidate = catalog.entries[i].id;
                if (!string.IsNullOrEmpty(candidate) && string.Equals(candidate, resourceTypeId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static IReadOnlyList<VillagerJobPresetDefinition> LoadVillagerPresets(out bool usingFallback)
        {
            usingFallback = true;
            IReadOnlyList<VillagerJobPresetDefinition> presets = VillagerJobPresetCatalog.DefaultPresets;

#if UNITY_EDITOR
            var catalog = VillagerJobPresetCatalog.LoadAsset();
            if (catalog == null && System.IO.File.Exists(VILLAGER_PRESET_CATALOG_PATH))
            {
                catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<VillagerJobPresetCatalog>(VILLAGER_PRESET_CATALOG_PATH);
            }

            if (catalog != null && catalog.Presets != null && catalog.Presets.Count > 0)
            {
                presets = catalog.Presets;
                usingFallback = false;
            }
#endif

            if (presets == null || presets.Count == 0)
            {
                presets = VillagerJobPresetCatalog.DefaultPresets;
                usingFallback = true;
            }

            return presets;
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            if (go.TryGetComponent<T>(out var existing))
            {
                return existing;
            }

            var added = go.AddComponent<T>();
            if (added == null)
            {
                throw new InvalidOperationException($"Failed to add required component of type {typeof(T).Name} to '{go.name}'.");
            }

            return added;
        }

        private static GameObject FindInScene(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var match = FindInChildren(root.transform, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static GameObject FindInChildren(Transform root, string name)
        {
            if (root.name == name)
            {
                return root.gameObject;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var result = FindInChildren(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void MoveToScene(GameObject gameObject, UnityEngine.SceneManagement.Scene scene)
        {
            if (gameObject.scene != scene)
            {
                SceneManager.MoveGameObjectToScene(gameObject, scene);
            }
        }

    }
}
#endif

