using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Configuration for the active spatial grid provider.
    /// Authored through data assets and baked into a singleton.
    /// </summary>
    public struct SpatialGridConfig : IComponentData
    {
        public float CellSize;
        public float3 WorldMin;
        public float3 WorldMax;
        public int3 CellCounts;
        public uint HashSeed;
        public byte ProviderId;

        public readonly float3 WorldExtent => WorldMax - WorldMin;

        public readonly int CellCount => math.max(CellCounts.x * CellCounts.y * CellCounts.z, 0);
    }

    /// <summary>
    /// Runtime state for the spatial grid including double buffer tracking.
    /// </summary>
    public struct SpatialGridState : IComponentData
    {
        public int ActiveBufferIndex;
        public int TotalEntries;
        public uint Version;
    }

    /// <summary>
    /// Buffer element describing the compact entity slice that backs a cell.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct SpatialGridCellRange : IBufferElementData
    {
        public int StartIndex;
        public int Count;
    }

    /// <summary>
    /// Buffer element storing the flattened entity list for all cells.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct SpatialGridEntry : IBufferElementData
    {
        public Entity Entity;
        public float3 Position;
        public int CellId;
    }

    /// <summary>
    /// Tag component applied to entities that should be indexed by the spatial grid.
    /// </summary>
    public struct SpatialIndexedTag : IComponentData
    {
    }

    /// <summary>
    /// Buffer used as a staging area while rebuilding the grid.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct SpatialGridStagingEntry : IBufferElementData
    {
        public Entity Entity;
        public float3 Position;
        public int CellId;
    }

    /// <summary>
    /// Buffer used as a staging area for cell ranges while rebuilding.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct SpatialGridStagingCellRange : IBufferElementData
    {
        public int StartIndex;
        public int Count;
    }
}
