using Unity.Collections;
using Unity.Entities;

namespace Space4X.Individuals
{
    /// <summary>
    /// Component tracking behavior tree progression tiers.
    /// Higher tiers unlock more sophisticated AI behaviors and danger responses.
    /// </summary>
    public struct BehaviorTreeProgress : IComponentData
    {
        /// <summary>
        /// Combat behavior tier (unlocks combat responses).
        /// </summary>
        public byte CombatBehaviorTier;

        /// <summary>
        /// Evasion behavior tier (unlocks evasion patterns).
        /// </summary>
        public byte EvasionBehaviorTier;

        /// <summary>
        /// Leadership behavior tier (unlocks squad commands).
        /// </summary>
        public byte LeadershipBehaviorTier;

        /// <summary>
        /// Tactical behavior tier (unlocks tactical maneuvers).
        /// </summary>
        public byte TacticalBehaviorTier;
    }

    /// <summary>
    /// Buffer element for behavior unlock events.
    /// Generated when a new behavior tier is unlocked.
    /// </summary>
    public struct BehaviorUnlockEvent : IBufferElementData
    {
        /// <summary>
        /// Category of behavior that was unlocked.
        /// </summary>
        public BehaviorCategory Category;

        /// <summary>
        /// New tier level unlocked.
        /// </summary>
        public byte NewTier;

        /// <summary>
        /// Tick when unlock occurred.
        /// </summary>
        public uint UnlockTick;
    }

    /// <summary>
    /// Categories of behavior trees.
    /// </summary>
    public enum BehaviorCategory : byte
    {
        Combat = 0,
        Evasion = 1,
        Leadership = 2,
        Tactical = 3
    }
}

