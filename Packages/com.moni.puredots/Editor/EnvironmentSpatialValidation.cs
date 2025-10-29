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

        var warnings = new List<string>();
        var advisories = new List<string>();

        if (!HasAssetTypeOrFallback("EnvironmentGridConfig", EnvironmentFallbackPaths))
        {
            warnings.Add("EnvironmentGridConfig ScriptableObject (e.g., Assets/Data/Environment/EnvironmentGridConfig.asset)");
        }

        if (!HasAssetTypeOrFallback("SpatialPartitionProfile", SpatialFallbackPaths))
        {
            warnings.Add("SpatialPartitionProfile ScriptableObject (e.g., Assets/Data/Spatial/SpatialPartitionProfile.asset)");
        }

        if (!HasAssetAtPath("Assets/Config/Linker/link.xml"))
        {
            warnings.Add("link.xml (Assets/Config/Linker/link.xml) – required for IL2CPP stripping");
        }

        if (!HasAssetTypeOrFallback("HandCameraInputProfile", InputFallbackPaths))
        {
            advisories.Add("HandCameraInputProfile asset (ensures router sensitivities are configurable)");
        }

        if (!HasAssetTypeOrFallback("LogisticsProfile", LogisticsFallbackPaths))
        {
            advisories.Add("Logistics profile (villager/hauler registries) – optional but recommended");
        }

        if (warnings.Count > 0)
        {
            var warningMessage = $"[PureDOTS Validation] Missing runtime assets detected while {context}:\n - " + string.Join("\n - ", warnings);
            Debug.Log(warningMessage);
        }

        if (advisories.Count > 0)
        {
            var advisoryMessage = $"[PureDOTS Validation] Optional runtime assets recommended while {context}:\n - " + string.Join("\n - ", advisories);
            Debug.Log(advisoryMessage);
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
