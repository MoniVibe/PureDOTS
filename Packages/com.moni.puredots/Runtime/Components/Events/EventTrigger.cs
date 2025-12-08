using Unity.Entities;

namespace PureDOTS.Runtime.Components.Events
{
    /// <summary>
    /// Marker component indicating an entity has triggered an event.
    /// Used with WithChangeFilter to detect when events occur.
    /// </summary>
    public struct EventTrigger : IComponentData
    {
        public uint EventType;
        public uint TickNumber;
    }

    /// <summary>
    /// Event types for categorization.
    /// </summary>
    public enum EventType : uint
    {
        None = 0,
        LimbDamage = 1,
        FocusDrain = 2,
        PerceptionUpdate = 3,
        HealthChange = 4,
        PositionChange = 5,
        MoraleChange = 6,
        Custom = 100
    }
}

