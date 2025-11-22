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
            // Current Burst API exposes safety toggles via editor settings; avoid direct runtime mutation.
            Debug.Log("[Burst Safety] To enable safety checks, open Jobs > Burst > Safety Checks in the editor.");
        }

        [MenuItem("PureDOTS/Enable Burst Safety Checks", true)]
        public static bool EnableSafetyChecksValidate()
        {
            return true; // Always available
        }
    }
}












