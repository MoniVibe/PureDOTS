#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PureDOTS.Authoring;

namespace PureDOTS.Editor
{
    [CustomEditor(typeof(PureDotsRuntimeConfig))]
    internal sealed class PureDotsRuntimeConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Validate Runtime Config"))
            {
                RunValidation((PureDotsRuntimeConfig)target, "Runtime Config Validation");
            }
        }

        private static void RunValidation(PureDotsRuntimeConfig asset, string title)
        {
            var report = PureDotsAssetValidator.ValidateRuntimeConfig(asset);
            PureDotsAssetValidator.LogReport(report, verbose: true);
            EditorUtility.DisplayDialog(title, PureDotsAssetValidator.BuildSummary(report), "OK");
        }
    }

    [CustomEditor(typeof(ResourceTypeCatalog))]
    internal sealed class ResourceTypeCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Validate Resource Catalog"))
            {
                RunValidation((ResourceTypeCatalog)target, "Resource Catalog Validation");
            }
        }

        private static void RunValidation(ResourceTypeCatalog asset, string title)
        {
            var report = PureDotsAssetValidator.ValidateResourceTypeCatalog(asset);
            PureDotsAssetValidator.LogReport(report, verbose: true);
            EditorUtility.DisplayDialog(title, PureDotsAssetValidator.BuildSummary(report), "OK");
        }
    }

    [CustomEditor(typeof(EnvironmentGridConfig))]
    internal sealed class EnvironmentGridConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Validate Environment Grid"))
            {
                RunValidation((EnvironmentGridConfig)target, "Environment Grid Validation");
            }
        }

        private static void RunValidation(EnvironmentGridConfig asset, string title)
        {
            var report = PureDotsAssetValidator.ValidateEnvironmentGridConfig(asset);
            PureDotsAssetValidator.LogReport(report, verbose: true);
            EditorUtility.DisplayDialog(title, PureDotsAssetValidator.BuildSummary(report), "OK");
        }
    }

    [CustomEditor(typeof(SpatialPartitionProfile))]
    internal sealed class SpatialPartitionProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Validate Spatial Profile"))
            {
                RunValidation((SpatialPartitionProfile)target, "Spatial Profile Validation");
            }
        }

        private static void RunValidation(SpatialPartitionProfile asset, string title)
        {
            var report = PureDotsAssetValidator.ValidateSpatialPartitionProfile(asset);
            PureDotsAssetValidator.LogReport(report, verbose: true);
            EditorUtility.DisplayDialog(title, PureDotsAssetValidator.BuildSummary(report), "OK");
        }
    }
}
#endif

