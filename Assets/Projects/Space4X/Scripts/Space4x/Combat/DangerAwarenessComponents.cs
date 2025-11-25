using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Space4X.Combat
{
    /// <summary>
    /// Component marking an entity as a source of danger (projectile, charging ability, etc.).
    /// Used for danger detection and anticipation.
    /// </summary>
    public struct DangerSource : IComponentData
    {
        /// <summary>
        /// Type of danger this entity represents.
        /// </summary>
        public DangerType Type;

        /// <summary>
        /// Effect radius of the danger.
        /// </summary>
        public float Radius;

        /// <summary>
        /// Time until danger resolves (seconds).
        /// </summary>
        public float TimeToImpact;

        /// <summary>
        /// Entity that created this danger source.
        /// </summary>
        public Entity SourceEntity;
    }

    /// <summary>
    /// Types of danger sources.
    /// </summary>
    public enum DangerType : byte
    {
        Projectile = 0,
        AreaEffect = 1,
        ChargedAbility = 2,
        Explosion = 3,
        Collision = 4,
        Environmental = 5
    }

    /// <summary>
    /// Component for entities that can perceive and respond to dangers.
    /// Perception level gates which responses are available (behavior tree depth).
    /// </summary>
    public struct DangerPerception : IComponentData
    {
        /// <summary>
        /// Maximum range at which dangers can be detected.
        /// </summary>
        public float PerceptionRange;

        /// <summary>
        /// Reaction time in seconds (lower = faster reaction).
        /// </summary>
        public float ReactionTime;

        /// <summary>
        /// Unlocked behavior tree depth (gates available responses).
        /// </summary>
        public byte PerceptionLevel;

        /// <summary>
        /// Enabled danger response flags.
        /// </summary>
        public DangerResponseFlags EnabledResponses;
    }

    /// <summary>
    /// Flags for available danger responses.
    /// Higher behavior tree tiers unlock more sophisticated responses.
    /// </summary>
    [System.Flags]
    public enum DangerResponseFlags : ushort
    {
        None = 0,

        /// <summary>
        /// Move out of danger zone.
        /// </summary>
        Evade = 1 << 0,

        /// <summary>
        /// Activate shields.
        /// </summary>
        Shield = 1 << 1,

        /// <summary>
        /// Deploy flares/chaff countermeasures.
        /// </summary>
        CounterMeasures = 1 << 2,

        /// <summary>
        /// Warn nearby allies (squad alert).
        /// </summary>
        AlertSquad = 1 << 3,

        /// <summary>
        /// Shoot down incoming projectile (requires high tier).
        /// </summary>
        Intercept = 1 << 4,

        /// <summary>
        /// Emergency jump/teleport (requires high tier and capability).
        /// </summary>
        Teleport = 1 << 5
    }

    /// <summary>
    /// Buffer element tracking a detected danger.
    /// </summary>
    public struct DetectedDanger : IBufferElementData
    {
        /// <summary>
        /// Entity representing the danger source.
        /// </summary>
        public Entity DangerEntity;

        /// <summary>
        /// Type of danger.
        /// </summary>
        public DangerType Type;

        /// <summary>
        /// Predicted impact position.
        /// </summary>
        public float3 PredictedImpactPos;

        /// <summary>
        /// Time until impact (seconds).
        /// </summary>
        public float TimeToImpact;

        /// <summary>
        /// Tick when danger was detected.
        /// </summary>
        public uint DetectedTick;
    }

    /// <summary>
    /// Buffer element for danger alerts received from squad leaders/officers.
    /// Allows entities to react to dangers they didn't directly perceive.
    /// </summary>
    public struct DangerAlert : IBufferElementData
    {
        /// <summary>
        /// Entity that sent the alert (leader/officer).
        /// </summary>
        public Entity AlertingEntity;

        /// <summary>
        /// Entity representing the danger source.
        /// </summary>
        public Entity DangerEntity;

        /// <summary>
        /// Type of danger.
        /// </summary>
        public DangerType Type;

        /// <summary>
        /// Position of the danger.
        /// </summary>
        public float3 DangerPosition;

        /// <summary>
        /// Tick when alert was received.
        /// </summary>
        public uint AlertTick;
    }
}

