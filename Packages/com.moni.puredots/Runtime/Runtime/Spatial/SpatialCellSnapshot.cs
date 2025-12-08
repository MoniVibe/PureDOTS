using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Minimal stub for spatial cell snapshots used by temporal caching.
    /// </summary>
    public struct SpatialCellSnapshot : IComponentData
    {
        public ulong CellKey;
        public uint Version;
        public float3 Center;
    }
}
