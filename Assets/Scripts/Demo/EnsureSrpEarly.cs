#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

static class EnsureSrpEarly
{
    // Runs before any scene loads or DOTS world bootstraps
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void Ensure()
    {
        if (GraphicsSettings.currentRenderPipeline != null) return;

        // Try to load the lightweight scenario URP from Resources (create it once if you haven't).
        var asset = Resources.Load<UniversalRenderPipelineAsset>("Rendering/ScenarioURP");
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            Debug.LogWarning("[EnsureSrpEarly] ScenarioURP not found in Resources/Rendering; using a transient URP asset.");
        }

        // Assign for this run only (does not touch Project Settings on disk)
        QualitySettings.renderPipeline = asset;

        // Some Unity versions also read GraphicsSettings.defaultRenderPipeline; set both to be safe
#if UNITY_2021_3_OR_NEWER
        GraphicsSettings.defaultRenderPipeline = asset;
#endif

        Debug.Log($"[EnsureSrpEarly] SRP set to {asset.GetType().Name} before world bootstrap.");
    }
}
#endif
