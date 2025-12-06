using System;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Shared
{
    /// <summary>
    /// Burst-safe 128-bit GUID wrapper for cross-ECS entity identification.
    /// Uses two ulong values to store GUID data without managed allocations.
    /// </summary>
    public struct AgentGuid : IEquatable<AgentGuid>
    {
        public ulong High;
        public ulong Low;

        public AgentGuid(ulong high, ulong low)
        {
            High = high;
            Low = low;
        }

        public AgentGuid(Guid guid)
        {
            var bytes = guid.ToByteArray();
            High = BitConverter.ToUInt64(bytes, 0);
            Low = BitConverter.ToUInt64(bytes, 8);
        }

        public Guid ToGuid()
        {
            var bytes = new byte[16];
            BitConverter.GetBytes(High).CopyTo(bytes, 0);
            BitConverter.GetBytes(Low).CopyTo(bytes, 8);
            return new Guid(bytes);
        }

        public static AgentGuid NewGuid()
        {
            return new AgentGuid(Guid.NewGuid());
        }

        public bool Equals(AgentGuid other)
        {
            return High == other.High && Low == other.Low;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(High, Low);
        }

        public override bool Equals(object obj)
        {
            return obj is AgentGuid other && Equals(other);
        }

        public static bool operator ==(AgentGuid left, AgentGuid right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AgentGuid left, AgentGuid right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Maps GUID to entities in both Unity Entities and DefaultEcs worlds.
    /// Used by bridge systems to maintain entity relationships.
    /// </summary>
    public struct AgentIdMapping
    {
        public AgentGuid Guid;
        public Entity BodyEntity; // Unity Entities entity
        public int MindEntityIndex; // DefaultEcs entity index (-1 if not mapped)

        public bool IsValid => BodyEntity != Entity.Null && MindEntityIndex >= 0;
    }

    /// <summary>
    /// Component attached to Unity Entities to link with Mind ECS.
    /// Contains the shared GUID and reference to Mind ECS entity.
    /// Extended with cluster membership for hierarchical consensus.
    /// </summary>
    public struct AgentSyncId : IComponentData
    {
        public AgentGuid Guid;
        public int MindEntityIndex; // Index in DefaultEcs world (-1 if not mapped)
        public AgentGuid ClusterGuid; // Local cluster membership (empty if none)
        public AgentGuid RegionalHubGuid; // Regional hub membership (empty if none)
    }
}

