using Unity.Entities;

namespace PureDOTS.Runtime.Components.Caching
{
    /// <summary>
    /// Component storing input hash for temporal caching.
    /// Systems compute input hash and check cache before recomputing.
    /// </summary>
    public struct CacheKey : IComponentData
    {
        public uint InputHash;
        public uint LastCacheHitTick;
        public uint CacheHitCount;
    }

    /// <summary>
    /// Cache statistics for telemetry.
    /// </summary>
    public struct CacheStats : IComponentData
    {
        public uint TotalLookups;
        public uint CacheHits;
        public uint CacheMisses;
        
        public float HitRate => TotalLookups > 0 ? (float)CacheHits / TotalLookups : 0f;
    }
}

