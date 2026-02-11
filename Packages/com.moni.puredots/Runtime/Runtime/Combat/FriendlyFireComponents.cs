using Unity.Entities;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Tolerance for friendly fire incidents before penalties trigger.
    /// </summary>
    public struct FriendlyFireTolerance : IComponentData
    {
        public float Threshold;     // Damage threshold to ignore
        public float IncidentDecay; // Seconds to decay incident severity
    }

    /// <summary>
    /// Friendly fire penalties applied to the instigator or group.
    /// </summary>
    public struct FriendlyFirePenalty : IComponentData
    {
        public float MoraleDelta;
        public float CohesionDelta;
        public sbyte RelationDelta;
        public uint LastIncidentTick;
    }

    /// <summary>
    /// Friendly fire incident event buffer.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct FriendlyFireIncident : IBufferElementData
    {
        public Entity Attacker;
        public Entity Victim;
        public Entity Projectile;
        public float Damage;
        public FriendlyFireSeverity Severity;
        public uint Tick;
    }

    /// <summary>
    /// Friendly fire severity.
    /// </summary>
    public enum FriendlyFireSeverity : byte
    {
        Minor = 0,
        Major = 1,
        Catastrophic = 2
    }
}
