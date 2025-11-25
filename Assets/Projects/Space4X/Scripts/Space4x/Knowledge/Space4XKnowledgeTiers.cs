using PureDOTS.Runtime.Knowledge;
using Unity.Collections;
using Unity.Entities;

namespace Space4X.Knowledge
{
    //==========================================================================
    // TIER 1: Crew-Level Knowledge (Individual Skills & Abilities)
    //==========================================================================

    /// <summary>
    /// Individual crew member expertise and capabilities.
    /// </summary>
    public struct CrewExpertise : IComponentData
    {
        /// <summary>
        /// Technical skill level (engineering, repairs).
        /// </summary>
        public byte TechnicalSkill;

        /// <summary>
        /// Combat skill level (gunnery, tactics).
        /// </summary>
        public byte CombatSkill;

        /// <summary>
        /// Command skill level (leadership, coordination).
        /// </summary>
        public byte CommandSkill;

        /// <summary>
        /// Science skill level (research, analysis).
        /// </summary>
        public byte ScienceSkill;

        /// <summary>
        /// Psionic potential (innate, 0-100).
        /// </summary>
        public byte PsionicPotential;

        /// <summary>
        /// Tactical experience gained through combat.
        /// </summary>
        public byte TacticalExperience;

        /// <summary>
        /// Total missions completed.
        /// </summary>
        public uint MissionsCompleted;
    }

    /// <summary>
    /// Individual ability known by a crew member.
    /// </summary>
    public struct CrewAbility : IBufferElementData
    {
        /// <summary>
        /// Ability identifier.
        /// </summary>
        public FixedString64Bytes AbilityId;

        /// <summary>
        /// Source of ability (how it was acquired).
        /// </summary>
        public AbilitySource Source;

        /// <summary>
        /// Proficiency level (0-255).
        /// </summary>
        public byte Proficiency;

        /// <summary>
        /// Times used successfully.
        /// </summary>
        public uint TimesUsed;

        /// <summary>
        /// Tick when ability was learned.
        /// </summary>
        public uint LearnedTick;
    }

    /// <summary>
    /// Source/origin of an ability.
    /// </summary>
    public enum AbilitySource : byte
    {
        /// <summary>
        /// Technology-based (requires module/equipment).
        /// </summary>
        Technology = 0,

        /// <summary>
        /// Psionic/innate mental power.
        /// </summary>
        Psionic = 1,

        /// <summary>
        /// Tactical maneuver (combat experience).
        /// </summary>
        Tactical = 2,

        /// <summary>
        /// Training program completion.
        /// </summary>
        Training = 3,

        /// <summary>
        /// Genetic modification.
        /// </summary>
        Genetic = 4,

        /// <summary>
        /// Cybernetic enhancement.
        /// </summary>
        Cybernetic = 5
    }

    //==========================================================================
    // TIER 2: Colony-Level Knowledge (Tech, Culture, Local Data)
    //==========================================================================

    /// <summary>
    /// Colony's knowledge state and cultural identity.
    /// </summary>
    public struct ColonyKnowledge : IComponentData
    {
        /// <summary>
        /// Primary culture of the colony.
        /// </summary>
        public FixedString64Bytes CultureId;

        /// <summary>
        /// Overall tech level (aggregate of researched techs).
        /// </summary>
        public uint TechLevel;

        /// <summary>
        /// Available research points for new tech.
        /// </summary>
        public uint ResearchPoints;

        /// <summary>
        /// Research output per tick.
        /// </summary>
        public float ResearchRate;

        /// <summary>
        /// Cultural cohesion (0-1, affects stability).
        /// </summary>
        public float CulturalCohesion;

        /// <summary>
        /// Knowledge flags.
        /// </summary>
        public ColonyKnowledgeFlags Flags;
    }

    /// <summary>
    /// Flags for colony knowledge state.
    /// </summary>
    [System.Flags]
    public enum ColonyKnowledgeFlags : byte
    {
        None = 0,
        HasResearchFacility = 1 << 0,
        HasAcademy = 1 << 1,
        HasArchive = 1 << 2,
        IsResearchHub = 1 << 3,
        HasPsionicInstitute = 1 << 4
    }

    /// <summary>
    /// Technology researched/available at a colony.
    /// </summary>
    public struct ColonyTech : IBufferElementData
    {
        /// <summary>
        /// Technology identifier.
        /// </summary>
        public FixedString64Bytes TechId;

        /// <summary>
        /// Research completion level (0-100%).
        /// </summary>
        public byte CompletionLevel;

        /// <summary>
        /// Tick when fully researched (0 if incomplete).
        /// </summary>
        public uint ResearchedTick;

        /// <summary>
        /// Whether tech was imported from another colony.
        /// </summary>
        public bool IsImported;

        /// <summary>
        /// License cost to use if faction-restricted.
        /// </summary>
        public byte LicenseCost;
    }

    /// <summary>
    /// Cultural story/tradition present in colony.
    /// </summary>
    public struct ColonyCulture : IBufferElementData
    {
        /// <summary>
        /// Story identifier.
        /// </summary>
        public FixedString64Bytes StoryId;

        /// <summary>
        /// How embedded in colony culture (0-1).
        /// </summary>
        public float CulturalStrength;

        /// <summary>
        /// Origin of this cultural element.
        /// </summary>
        public CultureOrigin Origin;

        /// <summary>
        /// Tick when adopted.
        /// </summary>
        public uint AdoptedTick;

        /// <summary>
        /// Source colony if imported.
        /// </summary>
        public Entity SourceColony;
    }

    /// <summary>
    /// Origin of cultural element.
    /// </summary>
    public enum CultureOrigin : byte
    {
        /// <summary>
        /// Native to this colony.
        /// </summary>
        Native = 0,

        /// <summary>
        /// Voluntarily imported through trade/contact.
        /// </summary>
        Imported = 1,

        /// <summary>
        /// Imposed by conquering faction.
        /// </summary>
        Imposed = 2,

        /// <summary>
        /// Inherited from founding population.
        /// </summary>
        Inherited = 3,

        /// <summary>
        /// Developed locally as variant.
        /// </summary>
        Evolved = 4
    }

    //==========================================================================
    // TIER 3: Faction-Level Knowledge (Empire-Wide Policies & Doctrine)
    //==========================================================================

    /// <summary>
    /// Faction-wide doctrine and shared knowledge policies.
    /// </summary>
    public struct FactionDoctrine : IComponentData
    {
        /// <summary>
        /// Faction identifier.
        /// </summary>
        public FixedString64Bytes FactionId;

        /// <summary>
        /// Active military/research doctrine.
        /// </summary>
        public DoctrineType ActiveDoctrine;

        /// <summary>
        /// Faction unity level (0-1).
        /// </summary>
        public float UnityLevel;

        /// <summary>
        /// Research priority modifier.
        /// </summary>
        public float ResearchPriorityModifier;

        /// <summary>
        /// Whether knowledge is freely shared across colonies.
        /// </summary>
        public bool OpenKnowledgePolicy;

        /// <summary>
        /// Psionic regulation level.
        /// </summary>
        public PsionicRegulation PsionicPolicy;
    }

    /// <summary>
    /// Faction doctrine types.
    /// </summary>
    public enum DoctrineType : byte
    {
        Balanced = 0,
        MilitaryFirst = 1,
        ResearchFirst = 2,
        EconomyFirst = 3,
        ExpansionFirst = 4,
        DefenseFirst = 5,
        PsionicDevelopment = 6
    }

    /// <summary>
    /// Psionic regulation policy.
    /// </summary>
    public enum PsionicRegulation : byte
    {
        Unrestricted = 0,
        Monitored = 1,
        Licensed = 2,
        Restricted = 3,
        Banned = 4
    }

    /// <summary>
    /// Technology shared across faction.
    /// </summary>
    public struct FactionSharedTech : IBufferElementData
    {
        /// <summary>
        /// Technology identifier.
        /// </summary>
        public FixedString64Bytes TechId;

        /// <summary>
        /// Whether available to all faction colonies.
        /// </summary>
        public bool AvailableToAllColonies;

        /// <summary>
        /// License cost if restricted.
        /// </summary>
        public byte LicenseCost;

        /// <summary>
        /// Security classification.
        /// </summary>
        public SecurityClassification Classification;

        /// <summary>
        /// Tick when tech became faction-wide.
        /// </summary>
        public uint SharedTick;
    }

    /// <summary>
    /// Security classification for faction knowledge.
    /// </summary>
    public enum SecurityClassification : byte
    {
        Public = 0,
        Internal = 1,
        Confidential = 2,
        Secret = 3,
        TopSecret = 4
    }

    /// <summary>
    /// Faction-wide cultural mandate.
    /// </summary>
    public struct FactionCulturalMandate : IBufferElementData
    {
        /// <summary>
        /// Story/cultural element that is mandated.
        /// </summary>
        public FixedString64Bytes StoryId;

        /// <summary>
        /// Whether colonies must adopt this.
        /// </summary>
        public bool IsMandatory;

        /// <summary>
        /// Whether colonies may reject this.
        /// </summary>
        public bool AllowsOptOut;

        /// <summary>
        /// Penalty for non-compliance.
        /// </summary>
        public float NonCompliancePenalty;
    }
}

