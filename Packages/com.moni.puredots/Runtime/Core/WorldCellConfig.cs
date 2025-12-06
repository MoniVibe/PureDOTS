using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Core
{
    /// <summary>
    /// Configuration for world cell boundaries.
    /// Defines region/faction partitioning for multi-world coordination.
    /// </summary>
    public struct WorldCellConfig : IComponentData
    {
        public int CellId;                 // Unique identifier for this cell
        public float3 CellCenter;           // Center position of cell
        public float3 CellSize;             // Size of cell (width, height, depth)
        public int RegionId;                 // Region identifier
        public int FactionId;                // Faction identifier (-1 if neutral)
        public bool IsActive;                // Whether this cell is currently active
    }

    /// <summary>
    /// World cell state tracking entity counts and sync status.
    /// </summary>
    public struct WorldCellState : IComponentData
    {
        public int EntityCount;             // Number of entities in this cell
        public int ActiveAgentCount;        // Number of active agents
        public uint LastSyncTick;           // When cell was last synced
        public float SyncCost;               // CPU cost of last sync (ms)
        public bool NeedsSync;               // Whether cell needs synchronization
    }
}

