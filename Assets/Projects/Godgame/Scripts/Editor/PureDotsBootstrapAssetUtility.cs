#if UNITY_EDITOR && INCLUDE_GODGAME_IN_PUREDOTS
using System.Collections.Generic;
using System.IO;
using Godgame.Authoring;
using PureDOTS.Authoring;
using PureDOTS.Runtime.Resource;
using UnityEditor;
using UnityEngine;

namespace Godgame.Editor
{
    public static class PureDotsBootstrapAssetUtility
    {
        private const string ConfigFolder = "Assets/Godgame/Config";
        private const string ResourceCatalogPath = ConfigFolder + "/PureDotsResourceTypes.asset";
        private const string RuntimeConfigPath = ConfigFolder + "/PureDotsRuntimeConfig.asset";
        private const string RecipeCatalogPath = ConfigFolder + "/ResourceRecipeCatalog.asset";

        public static void EnsureBootstrapAssets()
        {
            EnsureFolder(ConfigFolder);

            var catalog = EnsureResourceCatalog();
            var recipeCatalog = EnsureRecipeCatalog();
            EnsureRuntimeConfig(catalog, recipeCatalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PureDotsBootstrapAssetUtility] Ensured PureDOTS runtime config and resource catalog assets.");
        }

        private static ResourceTypeCatalog EnsureResourceCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ResourceTypeCatalog>(ResourceCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ResourceTypeCatalog>();
                catalog.name = "PureDotsResourceTypes";
                AssetDatabase.CreateAsset(catalog, ResourceCatalogPath);
            }

            if (catalog.entries == null)
            {
                catalog.entries = new List<ResourceTypeDefinition>();
            }

            catalog.entries = new List<ResourceTypeDefinition>
            {
                new ResourceTypeDefinition
                {
                    id = "food",
                    displayColor = new Color(0.93f, 0.75f, 0.2f, 1f)
                },
                new ResourceTypeDefinition
                {
                    id = "water",
                    displayColor = new Color(0.22f, 0.55f, 0.85f, 1f)
                },
                new ResourceTypeDefinition
                {
                    id = "wood",
                    displayColor = new Color(0.74f, 0.51f, 0.25f, 1f)
                },
                new ResourceTypeDefinition
                {
                    id = "ore",
                    displayColor = new Color(0.46f, 0.46f, 0.48f, 1f)
                },
                new ResourceTypeDefinition
                {
                    id = "planks",
                    displayColor = new Color(0.76f, 0.53f, 0.28f, 1f)
                },
                new ResourceTypeDefinition
                {
                    id = "metal_ingots",
                    displayColor = new Color(0.72f, 0.72f, 0.76f, 1f)
                },
                new ResourceTypeDefinition
                {
                    id = "furniture",
                    displayColor = new Color(0.6f, 0.45f, 0.3f, 1f)
                },
                new ResourceTypeDefinition
                {
                    id = "fur_coats",
                    displayColor = new Color(0.8f, 0.72f, 0.6f, 1f)
                },
                new ResourceTypeDefinition
                {
                    id = "luxury_goods",
                    displayColor = new Color(0.9f, 0.82f, 0.35f, 1f)
                }
            };

            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static ResourceRecipeCatalog EnsureRecipeCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ResourceRecipeCatalog>(RecipeCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ResourceRecipeCatalog>();
                catalog.name = "ResourceRecipeCatalog";
                AssetDatabase.CreateAsset(catalog, RecipeCatalogPath);
            }

            var serialized = new SerializedObject(catalog);
            var families = serialized.FindProperty("_families");
            if (families != null)
            {
                families.ClearArray();
            }

            var recipes = serialized.FindProperty("_recipes");
            if (recipes != null)
            {
                recipes.ClearArray();
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void EnsureRuntimeConfig(ResourceTypeCatalog catalog, ResourceRecipeCatalog recipeCatalog)
        {
            var config = AssetDatabase.LoadAssetAtPath<PureDotsRuntimeConfig>(RuntimeConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PureDotsRuntimeConfig>();
                config.name = "PureDotsRuntimeConfig";
                AssetDatabase.CreateAsset(config, RuntimeConfigPath);
            }

            var serialized = new SerializedObject(config);
            serialized.FindProperty("_schemaVersion").intValue = PureDotsRuntimeConfig.LatestSchemaVersion;
            serialized.FindProperty("_resourceTypes").objectReferenceValue = catalog;
            var recipeProp = serialized.FindProperty("_recipeCatalog");
            if (recipeProp != null)
            {
                recipeProp.objectReferenceValue = recipeCatalog;
            }

            var pooling = serialized.FindProperty("_pooling");
            if (pooling != null)
            {
                pooling.FindPropertyRelative("nativeListCapacity").intValue = 64;
                pooling.FindPropertyRelative("nativeQueueCapacity").intValue = 64;
                pooling.FindPropertyRelative("defaultEntityPrewarmCount").intValue = 0;
                pooling.FindPropertyRelative("entityPoolMaxReserve").intValue = 128;
                pooling.FindPropertyRelative("ecbPoolCapacity").intValue = 32;
                pooling.FindPropertyRelative("ecbWriterPoolCapacity").intValue = 32;
                pooling.FindPropertyRelative("resetOnRewind").boolValue = true;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void AddFamily(SerializedProperty families, string id, string displayName, string raw, string refined,
            string composite, string description)
        {
            var index = families.arraySize;
            families.InsertArrayElementAtIndex(index);
            var element = families.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("id").stringValue = id;
            element.FindPropertyRelative("displayName").stringValue = displayName;
            element.FindPropertyRelative("rawResourceId").stringValue = raw;
            element.FindPropertyRelative("refinedResourceId").stringValue = refined;
            element.FindPropertyRelative("compositeResourceId").stringValue = composite;
            element.FindPropertyRelative("description").stringValue = description;
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
            var inputs = recipe.FindPropertyRelative("inputs");
            inputs.ClearArray();
            return recipe;
        }

        private static void AddIngredient(SerializedProperty recipe, string resourceId, int amount)
        {
            var inputs = recipe.FindPropertyRelative("inputs");
            var insertIndex = inputs.arraySize;
            inputs.InsertArrayElementAtIndex(insertIndex);
            var element = inputs.GetArrayElementAtIndex(insertIndex);
            element.FindPropertyRelative("resourceId").stringValue = resourceId;
            element.FindPropertyRelative("amount").intValue = amount;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            var leaf = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent))
            {
                parent = "Assets";
            }
            else if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
