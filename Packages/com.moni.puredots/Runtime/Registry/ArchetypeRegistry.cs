using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Registry
{
    /// <summary>
    /// Global archetype registry singleton for reducing archetype fragmentation.
    /// Caches archetype combinations by uint64 hash (component bitmask).
    /// </summary>
    public struct ArchetypeRegistry : IComponentData
    {
        public uint Version;
        public uint ArchetypeCount;
        public uint FragmentationScore; // Lower is better
    }

    /// <summary>
    /// Archetype hash component for tracking archetype combinations.
    /// </summary>
    public struct ArchetypeHash : IComponentData
    {
        public ulong ComponentBitmask; // uint64 hash of component combination
    }
}

