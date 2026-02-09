using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Singleton marker for projectile tracking hub.
    /// </summary>
    public struct ProjectileTrackingHub : IComponentData
    {
    }

    /// <summary>
    /// Runtime configuration for projectile tracking.
    /// </summary>
    public struct ProjectileTrackingConfig : IComponentData
    {
        public int MaxEvents;
        public byte ClearEachFrame;
    }

    /// <summary>
    /// Aggregate counters for quick telemetry.
    /// </summary>
    public struct ProjectileTrackingCounters : IComponentData
    {
        public uint NextId;
        public uint Spawned;
        public uint Hits;
        public uint Deflections;
        public uint Redirects;
        public uint Controls;
        public uint Retired;
        public uint Expired;
        public uint Recycled;
        public int LastProcessedIndex;
    }

    /// <summary>
    /// Per-projectile tracking state.
    /// </summary>
    public struct ProjectileTrackingState : IComponentData
    {
        public uint TrackingId;
        public uint SpawnTick;
        public uint LastEventTick;
    }

    /// <summary>
    /// Projectile tracking event stream (data-only).
    /// </summary>
    [InternalBufferCapacity(64)]
    public struct ProjectileTrackingEvent : IBufferElementData
    {
        public ProjectileTrackingEventKind Kind;
        public uint TrackingId;
        public Entity Projectile;
        public Entity Source;
        public Entity Target;
        public FixedString64Bytes ProjectileId;
        public FixedString32Bytes AmmoId;
        public float3 Position;
        public float3 Direction;
        public uint Tick;
        public float Time;
        public float Value;
        public byte Mode;
        public byte Result;
    }

    /// <summary>
    /// Event kinds for projectile tracking.
    /// </summary>
    public enum ProjectileTrackingEventKind : byte
    {
        None = 0,
        Spawn = 1,
        Hit = 2,
        Deflect = 3,
        Redirect = 4,
        Control = 5,
        Retire = 6,
        Expire = 7,
        Recycle = 8
    }
}
