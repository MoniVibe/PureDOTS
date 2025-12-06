using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Networking
{
    /// <summary>
    /// Replication event for network serialization.
    /// </summary>
    public struct ReplicationEvent : IBufferElementData
    {
        public uint EventType;
        public Entity SourceEntity;
        public float3 Position;
        public uint TickNumber;
        public uint RNGSeed;
    }
}

