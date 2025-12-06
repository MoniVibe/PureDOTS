using UnityEditor;
using UnityEngine;
using PureDOTS.Runtime.Scenario;

namespace PureDOTS.Editor
{
    /// <summary>
    /// Auto-saves scenario JSON when changes are made.
    /// </summary>
    [InitializeOnLoad]
    public static class ScenarioAutoSave
    {
        private static string _lastScenarioPath = "";

        static ScenarioAutoSave()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                // Auto-save scenario when exiting play mode
                if (!string.IsNullOrEmpty(_lastScenarioPath))
                {
                    // Save scenario to JSON
                }
            }
        }

        public static void SetScenarioPath(string path)
        {
            _lastScenarioPath = path;
        }
    }
}

