using Unity.Entities;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Individual personality modifiers affecting formation behavior and morale.
    /// Influences how entities adhere to group commands and respond to combat situations.
    /// </summary>
    public struct BehaviorProfile : IComponentData
    {
        /// <summary>
        /// Alignment adherence (0-1). Higher values mean better formation following.
        /// Affects: Alignment -= Chaos * dt
        /// </summary>
        public float Discipline;

        /// <summary>
        /// Morale decay resistance (0-1). Higher values reduce morale loss from damage.
        /// Affects: ΔMorale = -Damage * (1 + (1 - Courage))
        /// </summary>
        public float Courage;

        /// <summary>
        /// Formation deviation probability (0-1). Higher values cause more random movement.
        /// Affects: Steering = lerp(VelocityToTarget, RandomDeviation, Chaos)
        /// </summary>
        public float Chaos;

        /// <summary>
        /// Strength of following leader ideals (0-1). Higher values boost group morale effects.
        /// Affects: AttackWeight *= Zeal * GroupMorale
        /// </summary>
        public float Zeal;
    }
}

