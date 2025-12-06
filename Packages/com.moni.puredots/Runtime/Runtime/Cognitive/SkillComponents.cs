using Unity.Entities;

namespace PureDOTS.Runtime.Cognitive
{
    /// <summary>
    /// Skill profile storing procedural knowledge as weighted proficiency vectors.
    /// Skills are normalized floats (0-1) representing proficiency levels.
    /// </summary>
    public struct SkillProfile : IComponentData
    {
        /// <summary>Spell casting skill (0-1)</summary>
        public float CastingSkill;

        /// <summary>Dual-hand casting aptitude (0-1)</summary>
        public float DualCastingAptitude;

        /// <summary>Melee combat skill (0-1)</summary>
        public float MeleeSkill;

        /// <summary>Strategic thinking ability (0-1)</summary>
        public float StrategicThinking;

        /// <summary>Last tick when skills were updated</summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Learning state tracking for skill updates and plateau detection.
    /// </summary>
    public struct SkillLearningState : IComponentData
    {
        /// <summary>Experience counter for casting skill</summary>
        public int CastingExperienceCount;

        /// <summary>Experience counter for dual casting</summary>
        public int DualCastingExperienceCount;

        /// <summary>Experience counter for melee</summary>
        public int MeleeExperienceCount;

        /// <summary>Experience counter for strategic thinking</summary>
        public int StrategicExperienceCount;

        /// <summary>Last tick when learning state was updated</summary>
        public uint LastUpdateTick;

        /// <summary>Plateau threshold: freeze updates when ΔSkill < ε</summary>
        public float PlateauThreshold;

        /// <summary>Flag indicating if skills have plateaued (frozen updates)</summary>
        public bool IsPlateaued;
    }
}

