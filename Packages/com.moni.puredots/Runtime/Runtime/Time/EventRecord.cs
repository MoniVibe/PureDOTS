using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Immutable event record for deterministic replay.
    /// All world events append to a deterministic event journal.
    /// </summary>
    public struct EventRecord : IBufferElementData
    {
        /// <summary>Unique event ID (monotonically increasing).</summary>
        public ulong Id;
        /// <summary>Event type identifier.</summary>
        public byte Type;
        /// <summary>Tick when event occurred.</summary>
        public uint Tick;
        /// <summary>Event payload data (64 bytes max).</summary>
        public FixedBytes64 Payload;
        /// <summary>Entity associated with this event (if any).</summary>
        public Entity Entity;
    }

    /// <summary>
    /// Event log state singleton component.
    /// </summary>
    public struct EventLogState : IComponentData
    {
        /// <summary>Next event ID to assign.</summary>
        public ulong NextEventId;
        /// <summary>Current tick being recorded.</summary>
        public uint CurrentTick;
        /// <summary>Oldest tick retained in log.</summary>
        public uint OldestTick;
    }
}

