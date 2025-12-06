using Unity.Entities;

namespace PureDOTS.Runtime.AI.Cognitive
{
    /// <summary>
    /// Causal link buffer element storing cause-effect relationships.
    /// Used for lightweight causal graph construction per agent.
    /// </summary>
    public struct CausalLink : IBufferElementData
    {
        /// <summary>
        /// Cause action/event ID (maps to ActionId or custom event).
        /// </summary>
        public ushort Cause;

        /// <summary>
        /// Effect outcome ID (maps to outcome enum or custom event).
        /// </summary>
        public ushort Effect;

        /// <summary>
        /// Link weight (0.0 to 1.0) indicating strength of causal relationship.
        /// Reinforced on successful outcomes.
        /// </summary>
        public float Weight;

        /// <summary>
        /// Last tick when this link was reinforced.
        /// </summary>
        public uint LastReinforcedTick;

        /// <summary>
        /// Number of times this causal link has been observed.
        /// </summary>
        public ushort ObservationCount;
    }

    /// <summary>
    /// Outcome ID enum for causal chain effects.
    /// </summary>
    public enum OutcomeId : ushort
    {
        None = 0,
        HeightIncreased = 1,
        HeightDecreased = 2,
        EscapedPit = 3,
        ReachedGoal = 4,
        Failed = 5,
        ResourceGained = 6,
        ResourceLost = 7,
        DamageTaken = 8,
        DamageDealt = 9,
        Custom0 = 1000
    }
}

