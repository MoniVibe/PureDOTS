using Unity.Entities;
using PureDOTS.Runtime.Bridges;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Singleton component holding the AgentSyncBus instance.
    /// Managed component (not Burst-safe) for cross-ECS communication.
    /// </summary>
    public struct AgentSyncState : IComponentData
    {
        // This is a marker component - the actual bus is stored in a managed system
    }
}

