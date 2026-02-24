using PureDOTS.Runtime.Core;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Rendering
{
    /// <summary>
    /// Shared helper for runtime render catalog bootstrap that can be reused by game projects.
    /// </summary>
    public static class RenderCatalogAutoBootstrapUtility
    {
        private const string DefaultBootstrapObjectName = "AutoRenderCatalogBootstrap";

        public static bool EnsureCatalogBootstrapFromResources(
            string catalogResourceName,
            string bootstrapObjectName,
            string logPrefix,
            ref bool logged,
            bool warnIfCatalogMissing = true)
        {
            if (!RuntimeMode.IsRenderingEnabled)
            {
                return false;
            }

            var catalog = string.IsNullOrWhiteSpace(catalogResourceName)
                ? null
                : Resources.Load<RenderPresentationCatalogDefinition>(catalogResourceName);

            if (HasCatalogSingleton())
            {
                LogReadyOnce(logPrefix, ref logged, catalog);
                return true;
            }

            var existingBootstrap = UnityEngine.Object.FindFirstObjectByType<RenderPresentationCatalogRuntimeBootstrap>();
            if (existingBootstrap != null)
            {
                if (HasCatalogSingleton())
                {
                    LogReadyOnce(logPrefix, ref logged, catalog);
                    return true;
                }

                var selectedCatalog = existingBootstrap.CatalogDefinition ?? catalog;
                if (selectedCatalog == null)
                {
                    LogMissingOnce(logPrefix, catalogResourceName, ref logged, warnIfCatalogMissing);
                    return false;
                }

                var replacementName = existingBootstrap.gameObject != null
                    ? existingBootstrap.gameObject.name
                    : bootstrapObjectName;

                if (existingBootstrap.gameObject != null)
                {
                    UnityEngine.Object.Destroy(existingBootstrap.gameObject);
                }

                CreateRuntimeBootstrap(replacementName, selectedCatalog);

                var ready = HasCatalogSingleton();
                if (ready)
                {
                    LogReadyOnce(logPrefix, ref logged, selectedCatalog);
                }

                return ready;
            }

            if (catalog == null)
            {
                LogMissingOnce(logPrefix, catalogResourceName, ref logged, warnIfCatalogMissing);
                return false;
            }

            CreateRuntimeBootstrap(bootstrapObjectName, catalog);

            var catalogReady = HasCatalogSingleton();
            if (catalogReady)
            {
                LogReadyOnce(logPrefix, ref logged, catalog);
            }

            return catalogReady;
        }

        private static void CreateRuntimeBootstrap(string objectName, RenderPresentationCatalogDefinition catalog)
        {
            var bootstrapGo = new GameObject(string.IsNullOrWhiteSpace(objectName) ? DefaultBootstrapObjectName : objectName);
            bootstrapGo.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(bootstrapGo);

            var runtimeBootstrap = bootstrapGo.AddComponent<RenderPresentationCatalogRuntimeBootstrap>();
            runtimeBootstrap.CatalogDefinition = catalog;
            bootstrapGo.SetActive(true);
        }

        private static bool HasCatalogSingleton()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                return false;
            }

            using var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RenderPresentationCatalog>());
            return query.CalculateEntityCount() > 0;
        }

        private static void LogReadyOnce(string logPrefix, ref bool logged, RenderPresentationCatalogDefinition catalog)
        {
            if (logged)
            {
                return;
            }

            logged = true;
            var prefix = GetLogPrefix(logPrefix);
            int variantCount;
            int theme0Mappings;

            if (catalog != null)
            {
                variantCount = catalog.Variants?.Length ?? 0;
                theme0Mappings = GetTheme0MappingCount(catalog);
            }
            else if (!TryGetRuntimeCatalogMetrics(out variantCount, out theme0Mappings))
            {
                variantCount = 0;
                theme0Mappings = 0;
            }

            Debug.Log($"[{prefix}] catalog_ready=1 variants={variantCount} theme0={theme0Mappings}");
        }

        private static void LogMissingOnce(
            string logPrefix,
            string catalogResourceName,
            ref bool logged,
            bool warnIfCatalogMissing)
        {
            if (logged || !warnIfCatalogMissing)
            {
                return;
            }

            logged = true;
            var prefix = GetLogPrefix(logPrefix);
            Debug.LogWarning($"[{prefix}] catalog_ready=0 missing_resource='{catalogResourceName}'");
        }

        private static int GetTheme0MappingCount(RenderPresentationCatalogDefinition catalog)
        {
            if (catalog == null || catalog.Themes == null)
            {
                return 0;
            }

            for (int i = 0; i < catalog.Themes.Length; i++)
            {
                var theme = catalog.Themes[i];
                if (theme.ThemeId == 0)
                {
                    return theme.SemanticVariants?.Length ?? 0;
                }
            }

            return 0;
        }

        private static int GetTheme0MappingCount(BlobAssetReference<RenderPresentationCatalogBlob> catalog)
        {
            if (!catalog.IsCreated)
            {
                return 0;
            }

            var themes = catalog.Value.Themes;
            for (int i = 0; i < themes.Length; i++)
            {
                var theme = themes[i];
                if (theme.ThemeId == 0)
                {
                    return theme.VariantIndices.Length;
                }
            }

            return 0;
        }

        private static bool TryGetRuntimeCatalogMetrics(out int variantCount, out int theme0Mappings)
        {
            variantCount = 0;
            theme0Mappings = 0;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                return false;
            }

            using var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RenderPresentationCatalog>());
            if (query.CalculateEntityCount() != 1)
            {
                return false;
            }

            var catalogEntity = query.GetSingletonEntity();
            var catalog = world.EntityManager.GetComponentData<RenderPresentationCatalog>(catalogEntity);
            if (!catalog.Blob.IsCreated)
            {
                return false;
            }

            variantCount = catalog.Blob.Value.Variants.Length;
            theme0Mappings = GetTheme0MappingCount(catalog.Blob);
            return true;
        }

        private static string GetLogPrefix(string logPrefix)
        {
            return string.IsNullOrWhiteSpace(logPrefix)
                ? nameof(RenderCatalogAutoBootstrapUtility)
                : logPrefix;
        }
    }
}
