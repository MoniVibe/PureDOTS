using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Space4X.Knowledge
{
    /// <summary>
    /// Component tracking an actively executing maneuver.
    /// Maneuvers are physically executed - no abstract success/failure rolls.
    /// </summary>
    public struct ActiveManeuver : IComponentData
    {
        /// <summary>
        /// Identifier of the maneuver being executed.
        /// </summary>
        public FixedString64Bytes ManeuverId;

        /// <summary>
        /// Target position for the maneuver.
        /// </summary>
        public float3 TargetPosition;

        /// <summary>
        /// Execution progress (0-1).
        /// </summary>
        public float Progress;

        /// <summary>
        /// Tick when maneuver started.
        /// </summary>
        public uint StartTick;
    }

    /// <summary>
    /// Component storing pilot skill modifiers that affect hazard avoidance.
    /// Values are derived from ManeuverMastery and behavior tree tiers.
    /// </summary>
    public struct PilotSkillModifiers : IComponentData
    {
        /// <summary>
        /// How far ahead the pilot scans for hazards (0 for green pilots).
        /// </summary>
        public float HazardAwarenessRadius;

        /// <summary>
        /// How quickly the pilot can deviate from planned path.
        /// </summary>
        public float PathAdjustmentRate;

        /// <summary>
        /// Maximum angle pilot will deviate to avoid hazards.
        /// </summary>
        public float MaxDeviationAngle;

        /// <summary>
        /// Ticks before pilot reacts to new hazard (lower = faster).
        /// </summary>
        public float ReactionDelay;
    }

    /// <summary>
    /// Component tracking current hazard avoidance state.
    /// Used by PilotHazardAvoidanceSystem to modify steering inputs.
    /// </summary>
    public struct HazardAvoidanceState : IComponentData
    {
        /// <summary>
        /// Active path offset being applied.
        /// </summary>
        public float3 CurrentAdjustment;

        /// <summary>
        /// Entity currently being avoided (Entity.Null if none).
        /// </summary>
        public Entity AvoidingEntity;

        /// <summary>
        /// Avoidance urgency (0-1), affects adjustment aggressiveness.
        /// </summary>
        public float AvoidanceUrgency;
    }

    /// <summary>
    /// Buffer element for collision events.
    /// Populated by physics system when collisions occur.
    /// </summary>
    public struct CollisionEvent : IBufferElementData
    {
        /// <summary>
        /// Entity that was collided with.
        /// </summary>
        public Entity OtherEntity;

        /// <summary>
        /// Point of impact.
        /// </summary>
        public float3 ImpactPoint;

        /// <summary>
        /// Normal vector at impact point.
        /// </summary>
        public float3 ImpactNormal;

        /// <summary>
        /// Relative velocity at impact (for damage calculation).
        /// </summary>
        public float RelativeVelocity;

        /// <summary>
        /// Tick when collision occurred.
        /// </summary>
        public uint CollisionTick;
    }

    /// <summary>
    /// Component storing collision survival roll result.
    /// Calculated after collision impact based on Physique + Resolve.
    /// </summary>
    public struct CollisionSurvivalRoll : IComponentData
    {
        /// <summary>
        /// Impact severity (based on relative velocity + mass).
        /// </summary>
        public float ImpactSeverity;

        /// <summary>
        /// Crew protection from shields, armor, safety systems.
        /// </summary>
        public float CrewProtection;

        /// <summary>
        /// Survival outcome.
        /// </summary>
        public SurvivalOutcome Outcome;

        /// <summary>
        /// Tick when survival roll was performed.
        /// </summary>
        public uint RollTick;
    }

    /// <summary>
    /// Survival outcomes for collision events.
    /// </summary>
    public enum SurvivalOutcome : byte
    {
        Unscathed = 0,
        MinorInjury = 1,
        MajorInjury = 2,
        Incapacitated = 3,
        Death = 4
    }
}

