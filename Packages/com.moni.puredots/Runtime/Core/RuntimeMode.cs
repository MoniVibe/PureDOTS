using Unity.Burst;
using UnityEngine;
using UnityEngine.Rendering;

namespace PureDOTS.Runtime.Core
{
    /// <summary>
    /// Shared runtime helpers for checking execution environment (editor/headless/server).
    /// </summary>
    public static class RuntimeMode
    {
        private const string RenderingEnvVar = "PUREDOTS_RENDERING";
        private static readonly SharedStatic<byte> s_renderingEnabled =
            SharedStatic<byte>.GetOrCreate<RenderingEnabledContext, RenderingEnabledKey>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void InitializeRenderingFlag()
        {
            var value = System.Environment.GetEnvironmentVariable(RenderingEnvVar);
            s_renderingEnabled.Data = (byte)(
                string.Equals(value, "1", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "true", System.StringComparison.OrdinalIgnoreCase)
                    ? 1
                    : 0);
        }

        /// <summary>
        /// True when running in headless or server contexts where graphics are unavailable.
        /// </summary>
        public static bool IsHeadless
        {
            get
            {
#if UNITY_SERVER
                return true;
#else
                return Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
#endif
            }
        }

        /// <summary>
        /// True when presentation/rendering systems should be active.
        /// </summary>
        public static bool IsRenderingEnabled
        {
            get
            {
                if (!IsHeadless)
                {
                    return true;
                }
                return s_renderingEnabled.Data != 0;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static void ForceRenderingEnabled(bool enabled, string reason = null)
        {
            s_renderingEnabled.Data = (byte)(enabled ? 1 : 0);
            if (!string.IsNullOrWhiteSpace(reason))
            {
                Debug.Log($"[RuntimeMode] Rendering override={(enabled ? "enabled" : "disabled")} reason={reason}");
            }
        }
#endif

        private struct RenderingEnabledKey
        {
        }

        private struct RenderingEnabledContext
        {
        }
    }
}
