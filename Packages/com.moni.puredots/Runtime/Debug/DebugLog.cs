using System.Diagnostics;
using UnityEngine;

namespace PureDOTS.Runtime.Debugging
{
    /// <summary>
    /// Lightweight logging shim for editor/dev builds. Does nothing in player builds.
    /// </summary>
    public static class DebugLog
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message)
        {
            UnityEngine.Debug.Log(message);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(object message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(object message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}
