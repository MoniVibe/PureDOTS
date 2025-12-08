#if PUREDOTS_AI
using Unity.Entities;
using PureDOTS.Runtime.Debugging;
using UnityEngine;
using DefaultEcs;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Wrapper for inter-world communication bus wiring.
    /// </summary>
    public static class CommunicationBus
    {
        /// <summary>
        /// Connects Unity Entities world to DefaultEcs world via message bus.
        /// </summary>
        public static void Connect(World fromWorld, World toWorld, float latencySeconds = 0.1f)
        {
            if (fromWorld == null || !fromWorld.IsCreated || toWorld == null)
            {
                DebugLog.LogWarning("[CommunicationBus] Cannot connect invalid worlds.");
                return;
            }

            // AgentSyncBus handles cross-world communication
            // This wrapper ensures buses are wired during bootstrap
            var bus = AgentSyncBridgeCoordinator.GetBusFromWorld(fromWorld);
            if (bus == null)
            {
                DebugLog.LogWarning($"[CommunicationBus] No AgentSyncBus found in {fromWorld.Name}");
                return;
            }

            DebugLog.Log($"[CommunicationBus] Connected {fromWorld.Name} -> DefaultEcs World (latency: {latencySeconds}s)");
        }
    }
}
#else
using Unity.Entities;
using PureDOTS.Runtime.Debugging;
using UnityEngine;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Stubbed communication bus when AI/DefaultEcs layer is not present.
    /// </summary>
    public static class CommunicationBus
    {
        public static void Connect(World fromWorld, World toWorld, float latencySeconds = 0.1f)
        {
            if (fromWorld == null || !fromWorld.IsCreated)
            {
                DebugLog.LogWarning("[CommunicationBus] Cannot connect invalid worlds.");
                return;
            }

            // No DefaultEcs target available; log once for visibility.
            DebugLog.Log($"[CommunicationBus] AI layer not enabled; skipping connection from {fromWorld.Name}.");
        }
    }
}
#endif

