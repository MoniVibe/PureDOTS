#if UNITY_EDITOR && INCLUDE_SPACE4X_IN_PUREDOTS
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Space4X.Editor
{
    /// <summary>
    /// Provides actionable explanations for validation failures.
    /// Explains why assets are invalid (socket mismatches, missing mount fits, broken tech gates).
    /// </summary>
    public static class Space4XValidationExplainer
    {
        public static List<string> ValidateSocketParity()
        {
            var issues = new List<string>();
            
            // Check hull sockets vs module mount points
            // In real implementation, would:
            // 1. Load all hull prefabs
            // 2. Load all module catalogs
            // 3. Check that modules can mount to hull sockets
            // 4. Report mismatches with specific explanations
            
            // Placeholder
            Debug.Log("[Space4XValidationExplainer] Validating socket parity...");
            
            return issues;
        }

        public static List<string> ValidateMountFit()
        {
            var issues = new List<string>();
            
            // Check module mount requirements vs available slots
            // In real implementation, would:
            // 1. Check module RequiredMount matches hull mount types
            // 2. Check module RequiredSize fits available slot sizes
            // 3. Report specific mismatches
            
            Debug.Log("[Space4XValidationExplainer] Validating mount fit...");
            
            return issues;
        }

        public static List<string> ValidateFacilityTags()
        {
            var issues = new List<string>();
            
            // Check station facility tags match expected zones
            // In real implementation, would:
            // 1. Load station bindings
            // 2. Verify facility tags are valid
            // 3. Check zones are properly defined
            
            Debug.Log("[Space4XValidationExplainer] Validating facility tags...");
            
            return issues;
        }

        public static List<string> ValidateRecipeSanity()
        {
            var issues = new List<string>();
            
            // Check recipe inputs/outputs are valid
            // In real implementation, would:
            // 1. Load recipe catalogs
            // 2. Verify input resources exist
            // 3. Verify output resources exist
            // 4. Check tech gates are valid
            
            Debug.Log("[Space4XValidationExplainer] Validating recipe sanity...");
            
            return issues;
        }

        public static string ExplainSocketMismatch(string hullId, string moduleId, string socketName)
        {
            return $"Hull '{hullId}' does not have socket '{socketName}' required by module '{moduleId}'. " +
                   $"Either add socket '{socketName}' to hull or change module mount requirement.";
        }

        public static string ExplainMountFitFailure(string moduleId, string mountType, string availableMounts)
        {
            return $"Module '{moduleId}' requires mount type '{mountType}' but hull only provides: {availableMounts}. " +
                   $"Either change module mount requirement or add '{mountType}' mount to hull.";
        }

        public static string ExplainTechGateFailure(string recipeId, string requiredTech, string currentTech)
        {
            return $"Recipe '{recipeId}' requires tech '{requiredTech}' but current tech level is '{currentTech}'. " +
                   $"Research '{requiredTech}' before using this recipe.";
        }
    }
}
#endif
