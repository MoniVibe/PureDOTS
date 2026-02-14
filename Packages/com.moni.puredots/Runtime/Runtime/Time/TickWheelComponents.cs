using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Tick-wheel scheduler settings.
    /// </summary>
    public struct TickWheelSettings : IComponentData
    {
        /// <summary>
        /// Number of buckets in the wheel (must be > 0).
        /// </summary>
        public uint WheelSize;

        /// <summary>
        /// Tick width represented by one bucket (must be > 0).
        /// </summary>
        public uint BucketStride;

        public static TickWheelSettings CreateDefault()
        {
            return new TickWheelSettings
            {
                WheelSize = 2048u,
                BucketStride = 1u
            };
        }
    }

    /// <summary>
    /// Tick-wheel singleton tag.
    /// </summary>
    public struct TickWheelSingletonTag : IComponentData
    {
    }

    /// <summary>
    /// Runtime counters and digest for scheduler observability.
    /// </summary>
    public struct TickWheelRuntimeState : IComponentData
    {
        public uint NextSequence;
        public uint ScheduledCount;
        public uint FiredCount;
        public uint MaxLatenessTicks;
        public uint Digest;
        public uint LastDispatchTick;
    }

    /// <summary>
    /// Wheel bucket metadata stored on the scheduler singleton.
    /// </summary>
    [InternalBufferCapacity(64)]
    public struct TickWheelBucket : IBufferElementData
    {
        public int HeadEventIndex;
    }

    /// <summary>
    /// Scheduled wheel event entry stored in a pooled event buffer.
    /// </summary>
    [InternalBufferCapacity(128)]
    public struct TickWheelEvent : IBufferElementData
    {
        public uint DueTick;
        public int PayloadId;
        public Entity Target;
        public uint TieBreakA;
        public uint TieBreakB;
        public uint Sequence;
        public int NextEventIndex;
        public byte Active;
    }

    /// <summary>
    /// API request buffer consumed by TickWheelScheduleSystem.
    /// </summary>
    [InternalBufferCapacity(64)]
    public struct TickWheelScheduleRequest : IBufferElementData
    {
        public uint DueTick;
        public int PayloadId;
        public Entity Target;
        public uint TieBreakA;
        public uint TieBreakB;
    }

    /// <summary>
    /// Receipt emitted by TickWheelDispatchSystem via ECB when a target receives an event.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct TickWheelReceipt : IBufferElementData
    {
        public uint FiredTick;
        public uint DueTick;
        public int PayloadId;
    }
}
