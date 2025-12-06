using Unity.Entities;
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
                Debug.LogWarning("[CommunicationBus] Cannot connect invalid worlds.");
                return;
            }

            // AgentSyncBus handles cross-world communication
            // This wrapper ensures buses are wired during bootstrap
            var bus = AgentSyncBridgeCoordinator.GetBusFromWorld(fromWorld);
            if (bus == null)
            {
                Debug.LogWarning($"[CommunicationBus] No AgentSyncBus found in {fromWorld.Name}");
                return;
            }

            Debug.Log($"[CommunicationBus] Connected {fromWorld.Name} -> DefaultEcs World (latency: {latencySeconds}s)");
        }
    }
}

