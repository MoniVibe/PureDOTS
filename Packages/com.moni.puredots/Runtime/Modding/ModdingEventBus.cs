using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Modding
{
    /// <summary>
    /// Read-only event bus for modding API.
    /// Mods publish data via events, never mutate hot components directly.
    /// </summary>
    public struct ModdingEventBus : IComponentData
    {
        /// <summary>Last processed event index.</summary>
        public uint LastProcessedEventIndex;
    }

    /// <summary>
    /// Event published by mods (read-only data).
    /// </summary>
    public struct ModdingEvent : IBufferElementData
    {
        public enum EventType : byte
        {
            DataUpdate = 0,
            CatalogRegistration = 1,
            CustomEvent = 2
        }

        public EventType Type;
        public FixedString128Bytes EventId;
        public FixedString512Bytes Data;
        public uint Tick;
        public uint EventIndex;
    }

    /// <summary>
    /// Catalog registration from mods (converted to blobs at boot).
    /// </summary>
    public struct ModdingCatalogEntry : IBufferElementData
    {
        public FixedString64Bytes CatalogId;
        public FixedString128Bytes EntryId;
        public FixedString512Bytes Data;
    }
}

