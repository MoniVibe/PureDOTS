using UnityEngine;

namespace PureDOTS.Runtime.Physics
{
    // TEMP STUBS – replace with real physics integration later
    // These stubs exist to satisfy references in camera code that may have been
    // written expecting PureDOTS physics wrappers. The actual camera controller
    // uses Unity's Physics API directly, but these stubs prevent compile errors
    // if any code references these types.
    
    /// <summary>
    /// Temporary stub for Raycast operations.
    /// Currently, camera code uses UnityEngine.Physics.Raycast directly.
    /// This stub exists to prevent compile errors if any code references PureDOTS.Runtime.Physics.Raycast.
    /// </summary>
    public static class Raycast
    {
        // Add stub methods here if needed by camera code
        // For now, this is just a placeholder to satisfy namespace references
    }

    /// <summary>
    /// Temporary stub for SphereCast operations.
    /// Currently, camera code uses UnityEngine.Physics.SphereCast directly.
    /// This stub exists to prevent compile errors if any code references PureDOTS.Runtime.Physics.SphereCast.
    /// </summary>
    public static class SphereCast
    {
        // Add stub methods here if needed by camera code
        // For now, this is just a placeholder to satisfy namespace references
    }
}

