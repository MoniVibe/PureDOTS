using System.Runtime.InteropServices;
using Unity.Entities;

namespace PureDOTS.Runtime.Components.Caching
{
    /// <summary>
    /// Tracks path cache invalidation and versioning.
    /// </summary>
    public struct PathCacheState : IComponentData
    {
        public uint Version;
        [MarshalAs(UnmanagedType.U1)]
        public bool Dirty;
    }
}
