using UnityEditor;
using UnityEngine;

namespace Space4X.Editor
{
    /// <summary>
    /// CLI entry points for Prefab Maker operations.
    /// Invoked via -executeMethod for CI integration.
    /// </summary>
    public static class Space4XPrefabMakerCLI
    {
        /// <summary>
        /// Generate minimal bindings via CLI.
        /// Usage: -executeMethod Space4X.Editor.Space4XPrefabMakerCLI.GenerateMinimalBindings
        /// </summary>
        public static void GenerateMinimalBindings()
        {
            Debug.Log("[Space4XPrefabMakerCLI] Generating minimal bindings...");
            Space4XBindingGenerator.GenerateMinimalBindings();
            var bindings = new Space4XBindingGenerator.BindingSet { name = "Minimal" };
            Space4XBindingGenerator.CollectBindings(bindings, isMinimal: true);
            var hash = Space4XBindingGenerator.ComputeHash(bindings);
            Space4XIdempotencyReporter.ReportBindingsHash(isMinimal: true, hash);
            Space4XCoverageReporter.GenerateReport();
            Debug.Log("[Space4XPrefabMakerCLI] Minimal bindings generated successfully.");
        }

        /// <summary>
        /// Generate fancy bindings via CLI.
        /// Usage: -executeMethod Space4X.Editor.Space4XPrefabMakerCLI.GenerateFancyBindings
        /// </summary>
        public static void GenerateFancyBindings()
        {
            Debug.Log("[Space4XPrefabMakerCLI] Generating fancy bindings...");
            Space4XBindingGenerator.GenerateFancyBindings();
            var bindings = new Space4XBindingGenerator.BindingSet { name = "Fancy" };
            Space4XBindingGenerator.CollectBindings(bindings, isMinimal: false);
            var hash = Space4XBindingGenerator.ComputeHash(bindings);
            Space4XIdempotencyReporter.ReportBindingsHash(isMinimal: false, hash);
            Space4XCoverageReporter.GenerateReport();
            Debug.Log("[Space4XPrefabMakerCLI] Fancy bindings generated successfully.");
        }

        /// <summary>
        /// Validate idempotency via CLI.
        /// Usage: -executeMethod Space4X.Editor.Space4XPrefabMakerCLI.ValidateIdempotency
        /// Returns non-zero exit code on failure.
        /// </summary>
        public static void ValidateIdempotency()
        {
            Debug.Log("[Space4XPrefabMakerCLI] Validating idempotency...");
            if (Space4XIdempotencyReporter.ValidateIdempotency(out var errorMessage))
            {
                Debug.Log("[Space4XPrefabMakerCLI] Idempotency validation passed.");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[Space4XPrefabMakerCLI] Idempotency validation failed: {errorMessage}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Generate coverage report via CLI.
        /// Usage: -executeMethod Space4X.Editor.Space4XPrefabMakerCLI.GenerateCoverageReport
        /// </summary>
        public static void GenerateCoverageReport()
        {
            Debug.Log("[Space4XPrefabMakerCLI] Generating coverage report...");
            Space4XCoverageReporter.GenerateReport();
            Debug.Log("[Space4XPrefabMakerCLI] Coverage report generated successfully.");
        }
    }
}

