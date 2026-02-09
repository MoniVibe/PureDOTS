using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Engagement envelope for distance-based tactics.
    /// </summary>
    public struct EngagementEnvelope : IComponentData
    {
        public float MinRange;
        public float PreferredRange;
        public float MaxRange;
        public float HoldRange;
    }

    /// <summary>
    /// Positioning intent for tactical motion.
    /// </summary>
    public struct PositioningIntent : IComponentData
    {
        public PositioningMode Mode;
        public Entity Anchor;
        public float3 Offset;
        public float Priority;
    }

    /// <summary>
    /// Positioning modes for tactical behavior.
    /// </summary>
    public enum PositioningMode : byte
    {
        Hold = 0,
        Advance = 1,
        Retreat = 2,
        Flank = 3,
        Screen = 4,
        Escort = 5,
        Orbit = 6,
        Kite = 7,
        Strafe = 8
    }

    /// <summary>
    /// Strafe profile for lateral motion.
    /// </summary>
    public struct StrafeProfile : IComponentData
    {
        public float LateralSpeed;
        public float Jitter;
        public float ChangeIntervalSec;
    }

    /// <summary>
    /// Orbit profile for circular positioning around a target.
    /// </summary>
    public struct OrbitProfile : IComponentData
    {
        public float Radius;
        public float AngularSpeed;
        public float VerticalOffset;
    }
}
