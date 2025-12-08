using Unity.Entities;

namespace PureDOTS.Runtime.Components.Events
{
    /// <summary>
    /// Tracks per-tick consumption metrics for the EventQueue consumer.
    /// </summary>
    public struct EventQueueConsumerStats : IComponentData
    {
        public uint LastTick;
        public uint ConsumedCount;
    }
}
