#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PureDOTS.Authoring;

namespace PureDOTS.Editor.Migrations
{
    /// <summary>
    /// Prototype migration that inspects core assets and logs their schema version without modifying data.
    /// Serves as a template for future migrations.
    /// </summary>
    internal sealed class SchemaSurveyMigration : PureDotsMigration
    {
        public override string Id => "2025-10-26-schema-survey";

        public override string Description => "Logs schema versions of core PureDOTS authoring assets (no changes).";

        public override int Order => -100; // run early

        protected override void Run(PureDotsMigrationContext context)
        {
            LogConfig<PureDotsRuntimeConfig>(context, config => config.SchemaVersion, PureDotsRuntimeConfig.LatestSchemaVersion);
            LogConfig<ResourceTypeCatalog>(context, config => config.SchemaVersion, ResourceTypeCatalog.LatestSchemaVersion);
            LogConfig<EnvironmentGridConfig>(context, config => config.SchemaVersion, EnvironmentGridConfig.LatestSchemaVersion);
            LogConfig<SpatialPartitionProfile>(context, config => config.SchemaVersion, SpatialPartitionProfile.LatestSchemaVersion);

            LogAuthoring<ResourceSourceAuthoring>(context);
            LogAuthoring<StorehouseAuthoring>(context);
            LogAuthoring<ResourceChunkAuthoring>(context);
            LogAuthoring<ConstructionSiteAuthoring>(context);
            LogAuthoring<DivineHandAuthoring>(context);

            context.Report.Info("Schema survey completed. No assets were modified.");
        }

        private void LogConfig<T>(PureDotsMigrationContext context, System.Func<T, int> getter, int latest) where T : ScriptableObject
        {
            foreach (var asset in LoadAssets<T>())
            {
                var version = getter(asset);
                var path = AssetDatabase.GetAssetPath(asset);
                context.Report.Info($"{typeof(T).Name}: {path} (Schema={version}, Latest={latest})");
            }
        }

        private void LogAuthoring<T>(PureDotsMigrationContext context) where T : MonoBehaviour
        {
            var instances = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var instance in instances)
            {
                var path = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage()?.assetPath ?? instance.gameObject.scene.path;
                context.Report.Info($"{typeof(T).Name}: {instance.name} (Scene/Prefab: {path})");
            }
        }
    }
}
#endif

