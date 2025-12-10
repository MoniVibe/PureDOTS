using Unity.Entities;
using UDebug = UnityEngine.Debug;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Legacy no-op shim to satisfy demo references to PureDOTS.Systems.SystemRegistry.
    /// Real engine/game code should use the current SystemRegistry APIs directly.
    /// </summary>
    public static partial class SystemRegistry
    {
        /// <summary>
        /// Legacy demo helper: returns default handle, logs warning.
        /// </summary>
        public static SystemHandle Require<T>() where T : unmanaged, ISystem
        {
            UDebug.LogWarning($"[SystemRegistryLegacyShim] Require<{typeof(T).Name}> called from demo code; returning default handle.");
            return default;
        }

        /// <summary>
        /// Legacy demo helper: returns false with default handle, logs warning.
        /// </summary>
        public static bool TryGet<T>(out SystemHandle handle) where T : unmanaged, ISystem
        {
            handle = default;
            UDebug.LogWarning($"[SystemRegistryLegacyShim] TryGet<{typeof(T).Name}> called from demo code; returning false.");
            return false;
        }

        /// <summary>
        /// Legacy demo helper for groups: returns default handle, logs warning.
        /// </summary>
        public static SystemHandle RequireGroup<T>() where T : unmanaged, ISystem
        {
            UDebug.LogWarning($"[SystemRegistryLegacyShim] RequireGroup<{typeof(T).Name}> called from demo code; returning default handle.");
            return default;
        }

        /// <summary>
        /// Legacy demo helper for groups: returns false with default handle, logs warning.
        /// </summary>
        public static bool TryGetGroup<T>(out SystemHandle handle) where T : unmanaged, ISystem
        {
            handle = default;
            UDebug.LogWarning($"[SystemRegistryLegacyShim] TryGetGroup<{typeof(T).Name}> called from demo code; returning false.");
            return false;
        }
    }
}

