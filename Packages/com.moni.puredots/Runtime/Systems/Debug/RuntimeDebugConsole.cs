#nullable enable
using UnityEngine;

namespace PureDOTS.Runtime.Debugging
{
    internal static class RuntimeDebugConsole
    {
        private static RuntimeConfigConsoleBehaviour? s_console;
#if PUREDOTS_LEGACY_CAMERA
        private static DiagnosticsOverlayBehaviour? s_overlay;
#endif

        public static void EnsureConsole()
        {
            if (s_console != null)
                return;

            s_console = Object.FindFirstObjectByType<RuntimeConfigConsoleBehaviour>();
            if (s_console != null)
                return;

            var go = new GameObject("PureDOTS_RuntimeConsole")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Object.DontDestroyOnLoad(go);
            s_console = go.AddComponent<RuntimeConfigConsoleBehaviour>();
        }

        public static void AppendLog(string message)
        {
            s_console?.AppendLog(message);
        }

#if PUREDOTS_LEGACY_CAMERA
        public static void EnsureOverlay()
        {
            if (s_overlay != null)
                return;

            s_overlay = Object.FindFirstObjectByType<DiagnosticsOverlayBehaviour>();
            if (s_overlay != null)
                return;

            var go = new GameObject("PureDOTS_DiagnosticsOverlay")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Object.DontDestroyOnLoad(go);
            s_overlay = go.AddComponent<DiagnosticsOverlayBehaviour>();
        }
#else
        public static void EnsureOverlay()
        {
            // Overlay only exists when legacy camera bridge is enabled.
        }
#endif
    }
}



