using Unity.Collections;
using Unity.Entities;

namespace Space4X.Individuals
{
    /// <summary>
    /// Wisdom pool - meta-experience currency that can be spent into specific experience pools.
    /// Wisdom earning rate increases with total wisdom earned (diminishing returns).
    /// </summary>
    public struct WisdomPool : IComponentData
    {
        /// <summary>
        /// Current spendable wisdom (general XP).
        /// </summary>
        public float CurrentWisdom;

        /// <summary>
        /// Lifetime total wisdom earned (affects earning rate).
        /// </summary>
        public float TotalWisdomEarned;

        /// <summary>
        /// Multiplier for wisdom earning (scales with TotalWisdomEarned).
        /// </summary>
        public float WisdomEarnRate;
    }

    /// <summary>
    /// Experience pools for Physique, Finesse, and Will.
    /// These are separate pools that allow learning different things.
    /// </summary>
    public struct ExperiencePools : IComponentData
    {
        /// <summary>
        /// Experience for physical maneuvers, endurance, strength-based activities.
        /// </summary>
        public float PhysiqueXP;

        /// <summary>
        /// Experience for precise maneuvers, piloting, dexterity-based activities.
        /// </summary>
        public float FinesseXP;

        /// <summary>
        /// Experience for psionic abilities, leadership, resolve, willpower-based activities.
        /// </summary>
        public float WillXP;
    }

    /// <summary>
    /// Request to spend wisdom into a specific experience pool.
    /// </summary>
    public struct WisdomSpendRequest : IBufferElementData
    {
        /// <summary>
        /// Target pool to spend wisdom into.
        /// </summary>
        public ExperiencePoolType TargetPool;

        /// <summary>
        /// Amount of wisdom to spend.
        /// </summary>
        public float Amount;

        /// <summary>
        /// Tick when request was made (for determinism).
        /// </summary>
        public uint RequestTick;
    }

    /// <summary>
    /// Types of experience pools.
    /// </summary>
    public enum ExperiencePoolType : byte
    {
        Physique = 0,
        Finesse = 1,
        Will = 2
    }
}

