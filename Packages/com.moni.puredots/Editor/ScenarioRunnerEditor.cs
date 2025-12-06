using Unity.Entities;
using UnityEditor;
using UnityEngine;
using PureDOTS.Runtime.Scenario;

namespace PureDOTS.Editor
{
    /// <summary>
    /// Unity Editor window for live-preview of scenarios.
    /// Integrates ScenarioRunner into Unity Editor.
    /// </summary>
    public class ScenarioRunnerEditor : EditorWindow
    {
        private string _scenarioPath = "Scenarios/DefaultScenario.json";
        private bool _isRunning = false;

        [MenuItem("PureDOTS/Scenario Runner")]
        public static void ShowWindow()
        {
            GetWindow<ScenarioRunnerEditor>("Scenario Runner");
        }

        private void OnGUI()
        {
            GUILayout.Label("Scenario Runner", EditorStyles.boldLabel);

            _scenarioPath = EditorGUILayout.TextField("Scenario Path", _scenarioPath);

            if (GUILayout.Button(_isRunning ? "Stop" : "Run"))
            {
                _isRunning = !_isRunning;
                if (_isRunning)
                {
                    // Start scenario
                }
                else
                {
                    // Stop scenario
                }
            }

            if (GUILayout.Button("Load Scenario"))
            {
                // Load scenario from file
            }

            if (GUILayout.Button("Save Scenario"))
            {
                // Save current scenario
            }
        }
    }
}

