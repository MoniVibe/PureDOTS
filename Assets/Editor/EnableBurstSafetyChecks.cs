using Unity.Burst;
using Unity.Burst.Editor;
using UnityEditor;
using UnityEngine;

namespace PureDOTS.Editor
{
    /// <summary>
    /// Enables Burst safety checks programmatically.
    /// Can be executed via menu item or MCP.
    /// </summary>
    public static class EnableBurstSafetyChecks
    {
        [MenuItem("PureDOTS/Enable Burst Safety Checks")]
        public static void EnableSafetyChecks()
        {
            if (!BurstCompiler.IsEnabled)
            {
                Debug.LogWarning("[Burst Safety] Burst compilation is not enabled. Enabling it first...");
                BurstCompiler.IsEnabled = true;
            }

            // Enable safety checks and force them on
            BurstEditorOptions.EnableBurstSafetyChecks = true;
            BurstEditorOptions.ForceEnableBurstSafetyChecks = true;
            
            Debug.Log("[Burst Safety] Enabled Burst safety checks (Force On mode).");
            Debug.Log("[Burst Safety] This will catch container index out of bounds and job dependency violations.");
            
            // Trigger recompilation to apply changes
            BurstCompiler.CompileAll();
        }

        [MenuItem("PureDOTS/Enable Burst Safety Checks", true)]
        public static bool EnableSafetyChecksValidate()
        {
            return true; // Always available
        }
    }
}












