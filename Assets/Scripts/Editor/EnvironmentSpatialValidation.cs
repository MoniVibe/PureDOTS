#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class EnvironmentSpatialValidation
{
    private const string SessionKey = "PureDOTS_RuntimeProfilesValidated";
    private static readonly string[] EnvironmentFallbackPaths =
    {
        "Assets/Data/Environment/EnvironmentGridConfig.asset",
        "Assets/Config/Environment/EnvironmentGridConfig.asset"
    };

    private static readonly string[] SpatialFallbackPaths =
    {
        "Assets/Data/Spatial/SpatialPartitionProfile.asset",
        "Assets/Config/Spatial/SpatialPartitionProfile.asset"
    };

    private static readonly string[] InputFallbackPaths =
    {
        "Assets/Data/Input/HandCameraInputProfile.asset"
    };

    private static readonly string[] LogisticsFallbackPaths =
    {
        "Assets/Data/Logistics/LogisticsProfile.asset"
    };

    static EnvironmentSpatialValidation()
    {
        EditorApplication.delayCall += () => Validate("project load");
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Validate("entering Play Mode");
        }
    }

    private static void Validate(string context)
    {
        if (SessionState.GetBool(SessionKey, false) && context != "entering Play Mode")
        {
            return;
        }

        var missing = new List<string>();

        if (!HasAssetTypeOrFallback("EnvironmentGridConfig", EnvironmentFallbackPaths))
        {
            missing.Add("EnvironmentGridConfig ScriptableObject (e.g., Assets/Data/Environment/EnvironmentGridConfig.asset)");
        }

        if (!HasAssetTypeOrFallback("SpatialPartitionProfile", SpatialFallbackPaths))
        {
            missing.Add("SpatialPartitionProfile ScriptableObject (e.g., Assets/Data/Spatial/SpatialPartitionProfile.asset)");
        }

        if (!HasAssetTypeOrFallback("HandCameraInputProfile", InputFallbackPaths))
        {
            missing.Add("HandCameraInputProfile asset (ensures router sensitivities are configurable)");
        }

        if (!HasAssetAtPath("Assets/Config/Linker/link.xml"))
        {
            missing.Add("link.xml (Assets/Config/Linker/link.xml) – required for IL2CPP stripping");
        }

        if (!HasAssetTypeOrFallback("LogisticsProfile", LogisticsFallbackPaths))
        {
            missing.Add("Logistics profile (villager/hauler registries) – optional but recommended");
        }

        if (missing.Count > 0)
        {
            var message = $"[PureDOTS Validation] Missing runtime assets detected while {context}:\n - " + string.Join("\n - ", missing);
            Debug.LogWarning(message);
        }

        SessionState.SetBool(SessionKey, true);
    }

    private static bool HasAssetTypeOrFallback(string typeName, string[] fallbackPaths)
    {
        if (AssetDatabase.FindAssets("t:" + typeName).Length > 0)
        {
            return true;
        }

        foreach (var path in fallbackPaths)
        {
            if (HasAssetAtPath(path))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAssetAtPath(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Object>(path) != null;
    }
}
#endif
