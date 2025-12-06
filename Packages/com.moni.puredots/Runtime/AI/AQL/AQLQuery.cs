using Unity.Entities;

namespace PureDOTS.Runtime.AI.AQL
{
    /// <summary>
    /// Compiled AQL query with cached EntityQuery handle.
    /// Translates to pre-compiled DOTS queries for performance.
    /// </summary>
    public struct CompiledAQLQuery
    {
        public EntityQuery QueryHandle;
        public uint CacheVersion;
    }
}

