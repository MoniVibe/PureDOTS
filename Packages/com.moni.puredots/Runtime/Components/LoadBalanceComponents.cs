using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Density and CPU load tracking for load balancing.
    /// Measures entity density × CPU load for dynamic world splitting.
    /// </summary>
    public struct LoadBalanceMetrics : IComponentData
    {
        public float EntityDensity;         // Entity density (entities per unit area)
        public float CPULoad;               // CPU load (0-1, 1 = fully loaded)
        public float LoadScore;              // Combined score (density × CPU load)
        public int EntityCount;              // Number of entities in this region
        public uint LastUpdateTick;          // When metrics were last updated
    }

    /// <summary>
    /// World partition state for load balancing.
    /// Tracks which world/partition an entity belongs to.
    /// </summary>
    public struct WorldPartition : IComponentData
    {
        public AgentGuid WorldId;           // World/partition identifier
        public int PartitionIndex;           // Partition index
        public bool NeedsMigration;          // Whether entity needs migration
        public uint LastMigrationTick;      // When entity was last migrated
    }

    /// <summary>
    /// Load balancer state tracking load distribution.
    /// </summary>
    public struct LoadBalancerState : IComponentData
    {
        public int ActiveWorldCount;         // Number of active worlds
        public float AverageLoadScore;       // Average load score across worlds
        public float MaxLoadScore;           // Maximum load score
        public uint LastRebalanceTick;       // When load was last rebalanced
        public bool NeedsRebalance;          // Whether rebalancing is needed
    }
}

