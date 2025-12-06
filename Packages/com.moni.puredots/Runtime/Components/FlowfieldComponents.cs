using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Flowfield grid component - stores flowfield vectors per spatial cell.
    /// Shared component per zone for efficient caching.
    /// </summary>
    public struct FlowfieldGrid : ISharedComponentData
    {
        /// <summary>Zone ID this flowfield belongs to.</summary>
        public int ZoneId;
        
        /// <summary>Grid version (incremented when topology changes).</summary>
        public uint Version;
    }

    /// <summary>
    /// Flowfield vector per cell (cached in blob for performance).
    /// </summary>
    public struct FlowfieldCell : IBufferElementData
    {
        /// <summary>Flow direction vector (normalized).</summary>
        public float3 Direction;
        
        /// <summary>Flow strength (0-1).</summary>
        public float Strength;
        
        /// <summary>Cell ID in spatial grid.</summary>
        public int CellId;
    }

    /// <summary>
    /// Path cache blob - stores cached flowfield results per zone.
    /// </summary>
    public struct PathCacheBlob : IComponentData
    {
        /// <summary>Reference to blob asset containing cached paths.</summary>
        public BlobAssetReference<PathCacheBlobData> CacheBlob;
    }

    /// <summary>
    /// Blob data for path cache.
    /// </summary>
    public struct PathCacheBlobData
    {
        public BlobArray<float3> FlowVectors;
        public BlobArray<float> FlowStrengths;
        public uint Version;
    }
}

