using System;
using UnityEditor;
using UnityEngine;

namespace Space4X.Editor
{
    /// <summary>
    /// Space4X Prefab Maker window - generates and validates prefabs & bindings for Space4X assets.
    /// Mirrors Godgame prefab maker patterns.
    /// </summary>
    public class Space4XPrefabMaker : EditorWindow
    {
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Batch Generate", "Adopt/Repair", "Validate" };

        [MenuItem("Space4X/Prefab Maker")]
        public static void ShowWindow()
        {
            GetWindow<Space4XPrefabMaker>("Space4X Prefab Maker");
        }

        private void OnGUI()
        {
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);

            switch (_selectedTab)
            {
                case 0:
                    DrawBatchGenerateTab();
                    break;
                case 1:
                    DrawAdoptRepairTab();
                    break;
                case 2:
                    DrawValidateTab();
                    break;
            }
        }

        private void DrawBatchGenerateTab()
        {
            GUILayout.Label("Batch Generate", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Generate prefabs and bindings for:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hulls"))
            {
                GenerateHullPrefabs();
            }
            if (GUILayout.Button("Modules"))
            {
                GenerateModulePrefabs();
            }
            if (GUILayout.Button("Stations"))
            {
                GenerateStationPrefabs();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Resources"))
            {
                GenerateResourcePrefabs();
            }
            if (GUILayout.Button("Products"))
            {
                GenerateProductPrefabs();
            }
            if (GUILayout.Button("FX/HUD"))
            {
                GenerateFXPrefabs();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Binding Sets:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Minimal Bindings"))
            {
                GenerateMinimalBindings();
            }
            if (GUILayout.Button("Generate Fancy Bindings"))
            {
                GenerateFancyBindings();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawAdoptRepairTab()
        {
            GUILayout.Label("Adopt/Repair", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Repair existing prefabs and bindings:");
            if (GUILayout.Button("Repair All Prefabs"))
            {
                RepairAllPrefabs();
            }
            if (GUILayout.Button("Repair Bindings"))
            {
                RepairBindings();
            }
        }

        private void DrawValidateTab()
        {
            GUILayout.Label("Validate", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Run validation checks:");
            if (GUILayout.Button("Validate Socket Parity"))
            {
                ValidateSocketParity();
            }
            if (GUILayout.Button("Validate Mount Fit"))
            {
                ValidateMountFit();
            }
            if (GUILayout.Button("Validate Facility Tags"))
            {
                ValidateFacilityTags();
            }
            if (GUILayout.Button("Validate Recipe Sanity"))
            {
                ValidateRecipeSanity();
            }
            if (GUILayout.Button("Validate Idempotency"))
            {
                ValidateIdempotency();
            }
        }

        private void GenerateHullPrefabs()
        {
            Debug.Log("[Space4XPrefabMaker] Generating hull prefabs...");
            // TODO: Implement hull prefab generation
        }

        private void GenerateModulePrefabs()
        {
            Debug.Log("[Space4XPrefabMaker] Generating module prefabs...");
            // TODO: Implement module prefab generation
        }

        private void GenerateStationPrefabs()
        {
            Debug.Log("[Space4XPrefabMaker] Generating station prefabs...");
            // TODO: Implement station prefab generation
        }

        private void GenerateResourcePrefabs()
        {
            Debug.Log("[Space4XPrefabMaker] Generating resource prefabs...");
            // TODO: Implement resource prefab generation
        }

        private void GenerateProductPrefabs()
        {
            Debug.Log("[Space4XPrefabMaker] Generating product prefabs...");
            // TODO: Implement product prefab generation
        }

        private void GenerateFXPrefabs()
        {
            Debug.Log("[Space4XPrefabMaker] Generating FX/HUD prefabs...");
            // TODO: Implement FX prefab generation
        }

        private void GenerateMinimalBindings()
        {
            try
            {
                Space4XBindingGenerator.GenerateMinimalBindings();
                var bindings = new Space4XBindingGenerator.BindingSet { name = "Minimal" };
                Space4XBindingGenerator.CollectBindings(bindings, isMinimal: true);
                var hash = Space4XBindingGenerator.ComputeHash(bindings);
                Space4XIdempotencyReporter.ReportBindingsHash(isMinimal: true, hash);
                
                // Generate coverage report
                Space4XCoverageReporter.GenerateReport();
                
                EditorUtility.DisplayDialog("Success", "Minimal bindings generated successfully.", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to generate minimal bindings: {ex.Message}", "OK");
                Debug.LogError($"[Space4XPrefabMaker] Error generating minimal bindings: {ex}");
            }
        }

        private void GenerateFancyBindings()
        {
            try
            {
                Space4XBindingGenerator.GenerateFancyBindings();
                var bindings = new Space4XBindingGenerator.BindingSet { name = "Fancy" };
                Space4XBindingGenerator.CollectBindings(bindings, isMinimal: false);
                var hash = Space4XBindingGenerator.ComputeHash(bindings);
                Space4XIdempotencyReporter.ReportBindingsHash(isMinimal: false, hash);
                
                // Generate coverage report
                Space4XCoverageReporter.GenerateReport();
                
                EditorUtility.DisplayDialog("Success", "Fancy bindings generated successfully.", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to generate fancy bindings: {ex.Message}", "OK");
                Debug.LogError($"[Space4XPrefabMaker] Error generating fancy bindings: {ex}");
            }
        }

        private void RepairAllPrefabs()
        {
            try
            {
                Space4XPrefabRepair.RepairAllPrefabs();
                EditorUtility.DisplayDialog("Success", "All prefabs repaired successfully.", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to repair prefabs: {ex.Message}", "OK");
                Debug.LogError($"[Space4XPrefabMaker] Error repairing prefabs: {ex}");
            }
        }

        private void RepairBindings()
        {
            try
            {
                Space4XPrefabRepair.RepairBindings();
                EditorUtility.DisplayDialog("Success", "Bindings repaired successfully.", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to repair bindings: {ex.Message}", "OK");
                Debug.LogError($"[Space4XPrefabMaker] Error repairing bindings: {ex}");
            }
        }

        private void ValidateSocketParity()
        {
            try
            {
                var results = Space4XValidationExplainer.ValidateSocketParity();
                if (results.Count == 0)
                {
                    EditorUtility.DisplayDialog("Success", "Socket parity validation passed.", "OK");
                }
                else
                {
                    var message = "Socket parity issues found:\n" + string.Join("\n", results);
                    EditorUtility.DisplayDialog("Validation Failed", message, "OK");
                    Debug.LogWarning($"[Space4XPrefabMaker] Socket parity issues: {string.Join(", ", results)}");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to validate socket parity: {ex.Message}", "OK");
                Debug.LogError($"[Space4XPrefabMaker] Error validating socket parity: {ex}");
            }
        }

        private void ValidateMountFit()
        {
            try
            {
                var results = Space4XValidationExplainer.ValidateMountFit();
                if (results.Count == 0)
                {
                    EditorUtility.DisplayDialog("Success", "Mount fit validation passed.", "OK");
                }
                else
                {
                    var message = "Mount fit issues found:\n" + string.Join("\n", results);
                    EditorUtility.DisplayDialog("Validation Failed", message, "OK");
                    Debug.LogWarning($"[Space4XPrefabMaker] Mount fit issues: {string.Join(", ", results)}");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to validate mount fit: {ex.Message}", "OK");
                Debug.LogError($"[Space4XPrefabMaker] Error validating mount fit: {ex}");
            }
        }

        private void ValidateFacilityTags()
        {
            try
            {
                var results = Space4XValidationExplainer.ValidateFacilityTags();
                if (results.Count == 0)
                {
                    EditorUtility.DisplayDialog("Success", "Facility tags validation passed.", "OK");
                }
                else
                {
                    var message = "Facility tag issues found:\n" + string.Join("\n", results);
                    EditorUtility.DisplayDialog("Validation Failed", message, "OK");
                    Debug.LogWarning($"[Space4XPrefabMaker] Facility tag issues: {string.Join(", ", results)}");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to validate facility tags: {ex.Message}", "OK");
                Debug.LogError($"[Space4XPrefabMaker] Error validating facility tags: {ex}");
            }
        }

        private void ValidateRecipeSanity()
        {
            try
            {
                var results = Space4XValidationExplainer.ValidateRecipeSanity();
                if (results.Count == 0)
                {
                    EditorUtility.DisplayDialog("Success", "Recipe sanity validation passed.", "OK");
                }
                else
                {
                    var message = "Recipe sanity issues found:\n" + string.Join("\n", results);
                    EditorUtility.DisplayDialog("Validation Failed", message, "OK");
                    Debug.LogWarning($"[Space4XPrefabMaker] Recipe sanity issues: {string.Join(", ", results)}");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to validate recipe sanity: {ex.Message}", "OK");
                Debug.LogError($"[Space4XPrefabMaker] Error validating recipe sanity: {ex}");
            }
        }

        private void ValidateIdempotency()
        {
            try
            {
                if (Space4XIdempotencyReporter.ValidateIdempotency(out var errorMessage))
                {
                    EditorUtility.DisplayDialog("Success", "Idempotency validation passed. All hashes match.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Validation Failed", errorMessage, "OK");
                    Debug.LogError($"[Space4XPrefabMaker] Idempotency validation failed: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to validate idempotency: {ex.Message}", "OK");
                Debug.LogError($"[Space4XPrefabMaker] Error validating idempotency: {ex}");
            }
        }
    }
}

