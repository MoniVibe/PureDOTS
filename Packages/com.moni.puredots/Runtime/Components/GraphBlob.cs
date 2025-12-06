using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// BlobAsset structure for graph topology templates.
    /// Defines reusable graph patterns.
    /// </summary>
    public struct GraphTopologyBlob
    {
        public BlobString TopologyId;       // Topology identifier
        public BlobArray<GraphNodeBlob> Nodes; // Nodes in this topology
        public BlobArray<GraphEdgeBlob> Edges; // Edges in this topology
    }

    /// <summary>
    /// Graph node data in blob format.
    /// </summary>
    public struct GraphNodeBlob
    {
        public int NodeId;
        public GraphNodeType Type;
    }

    /// <summary>
    /// Graph edge data in blob format.
    /// </summary>
    public struct GraphEdgeBlob
    {
        public int SourceNodeId;
        public int TargetNodeId;
        public float Weight;
        public GraphEdgeType Type;
    }

    /// <summary>
    /// Catalog of graph topology templates.
    /// </summary>
    public struct GraphTopologyCatalogBlob
    {
        public BlobArray<GraphTopologyBlob> Topologies;
    }
}

