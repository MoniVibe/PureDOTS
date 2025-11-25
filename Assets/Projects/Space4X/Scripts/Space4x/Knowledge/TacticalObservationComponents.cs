using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Space4X.Knowledge
{
    /// <summary>
    /// Component for entities that can observe and learn from tactical maneuvers.
    /// </summary>
    public struct TacticalObserver : IComponentData
    {
        /// <summary>
        /// Maximum range at which maneuvers can be observed.
        /// </summary>
        public float ObservationRange;

        /// <summary>
        /// Maximum number of maneuvers that can be observed simultaneously.
        /// </summary>
        public byte MaxSimultaneousObserve;

        /// <summary>
        /// Learning modifier based on Finesse + TacticalExperience.
        /// Higher values grant more XP from observations.
        /// </summary>
        public float LearningModifier;
    }

    /// <summary>
    /// Buffer element tracking an observed maneuver execution.
    /// </summary>
    public struct ObservedManeuver : IBufferElementData
    {
        /// <summary>
        /// Identifier of the maneuver being observed.
        /// </summary>
        public FixedString64Bytes ManeuverId;

        /// <summary>
        /// Entity performing the maneuver.
        /// </summary>
        public Entity PerformerEntity;

        /// <summary>
        /// Quality factor (0-1) indicating how well the maneuver was executed.
        /// Higher quality grants more XP.
        /// </summary>
        public float QualityFactor;

        /// <summary>
        /// Tick when the maneuver was observed.
        /// </summary>
        public uint ObserveTick;
    }

    /// <summary>
    /// Buffer element tracking mastery progress for a specific maneuver.
    /// Similar to ExtendedSpellMastery but for tactical maneuvers.
    /// </summary>
    public struct ManeuverMastery : IBufferElementData
    {
        /// <summary>
        /// Identifier of the maneuver.
        /// </summary>
        public FixedString64Bytes ManeuverId;

        /// <summary>
        /// Mastery progress from 0.0 to 4.0 (0% to 400%).
        /// </summary>
        public float MasteryProgress;

        /// <summary>
        /// Number of times this maneuver has been observed.
        /// </summary>
        public uint ObservationCount;

        /// <summary>
        /// Number of practice attempts made.
        /// </summary>
        public uint PracticeAttempts;

        /// <summary>
        /// Number of successful executions.
        /// </summary>
        public uint SuccessfulExecutions;

        /// <summary>
        /// Number of failed executions (collisions, botched attempts).
        /// </summary>
        public uint FailedExecutions;

        /// <summary>
        /// Flags indicating mastery milestones and capabilities.
        /// </summary>
        public ManeuverMasteryFlags Flags;
    }

    /// <summary>
    /// Flags for maneuver mastery milestones.
    /// </summary>
    [System.Flags]
    public enum ManeuverMasteryFlags : byte
    {
        None = 0,

        /// <summary>
        /// Can anticipate/dodge this maneuver (unlocked at ~20% mastery).
        /// </summary>
        Anticipated = 1 << 0,

        /// <summary>
        /// Proficient execution (100%+ mastery).
        /// </summary>
        Proficient = 1 << 1,

        /// <summary>
        /// Signature variant unlocked (200%+ mastery).
        /// </summary>
        Signature = 1 << 2,

        /// <summary>
        /// Full mastery with fluid avoidance (400% mastery).
        /// </summary>
        Master = 1 << 3
    }
}

