using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Entity graph node representing an entity in the relationship graph.
    /// </summary>
    public struct EntityGraphNode : IComponentData
    {
        public int NodeId;                  // Unique node identifier
        public Entity GraphEntity;          // Entity this node represents
        public int NeighborCount;           // Number of neighbors
        public GraphNodeType Type;          // Type of node (Limb, Villager, ShipModule, etc.)
        public uint LastUpdateTick;         // When node was last updated
    }

    /// <summary>
    /// Types of graph nodes.
    /// </summary>
    public enum GraphNodeType : byte
    {
        Limb = 0,
        Villager = 1,
        Band = 2,
        ShipModule = 3,
        HullSegment = 4,
        Aggregate = 5
    }

    /// <summary>
    /// Graph edge representing a relationship between entities.
    /// Stored as buffer element for efficient traversal.
    /// </summary>
    public struct EntityGraphEdge : IBufferElementData
    {
        public int SourceNodeId;            // Source node ID
        public int TargetNodeId;             // Target node ID
        public float Weight;                 // Edge weight (for weighted graphs)
        public GraphEdgeType Type;          // Type of relationship
        public uint LastUpdateTick;         // When edge was last updated
    }

    /// <summary>
    /// Types of graph edges/relationships.
    /// </summary>
    public enum GraphEdgeType : byte
    {
        LimbToLimb = 0,
        VillagerToBand = 1,
        ModuleToHull = 2,
        Social = 3,
        Influence = 4
    }

    /// <summary>
    /// Graph state tracking graph topology.
    /// </summary>
    public struct EntityGraphState : IComponentData
    {
        public int NodeCount;                // Total number of nodes
        public int EdgeCount;                // Total number of edges
        public uint LastRebuildTick;         // When graph was last rebuilt
        public bool NeedsRebuild;            // Whether graph needs rebuilding
    }
}

