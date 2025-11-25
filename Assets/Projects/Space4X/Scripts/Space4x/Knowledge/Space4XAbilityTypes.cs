using Unity.Collections;
using Unity.Entities;

namespace Space4X.Knowledge
{
    //==========================================================================
    // Technology-Based Abilities (Ship/Module Activated)
    //==========================================================================

    /// <summary>
    /// Blob catalog of technology-based abilities.
    /// </summary>
    public struct TechAbilityCatalogBlob
    {
        public BlobArray<TechAbilitySpec> Abilities;
    }

    /// <summary>
    /// Specification for a technology-based ability.
    /// </summary>
    public struct TechAbilitySpec
    {
        /// <summary>
        /// Ability identifier.
        /// </summary>
        public FixedString64Bytes AbilityId;

        /// <summary>
        /// Display name.
        /// </summary>
        public FixedString64Bytes DisplayName;

        /// <summary>
        /// Module type required to use this ability.
        /// </summary>
        public FixedString64Bytes RequiredModuleId;

        /// <summary>
        /// Minimum tech level required.
        /// </summary>
        public byte RequiredTechLevel;

        /// <summary>
        /// Power cost to activate.
        /// </summary>
        public float PowerCost;

        /// <summary>
        /// Cooldown in seconds.
        /// </summary>
        public float Cooldown;

        /// <summary>
        /// Activation time (0 = instant).
        /// </summary>
        public float ActivationTime;

        /// <summary>
        /// Effect type.
        /// </summary>
        public TechAbilityEffectType EffectType;

        /// <summary>
        /// Base effect magnitude.
        /// </summary>
        public float EffectMagnitude;

        /// <summary>
        /// Effect duration (0 = instant).
        /// </summary>
        public float EffectDuration;

        /// <summary>
        /// Range (0 = self only).
        /// </summary>
        public float Range;
    }

    /// <summary>
    /// Types of tech ability effects.
    /// </summary>
    public enum TechAbilityEffectType : byte
    {
        Damage = 0,
        Shield = 1,
        Repair = 2,
        SpeedBoost = 3,
        Stealth = 4,
        Scan = 5,
        Hack = 6,
        EMP = 7,
        Teleport = 8,
        Overdrive = 9
    }

    /// <summary>
    /// Singleton reference to tech ability catalog.
    /// </summary>
    public struct TechAbilityCatalogRef : IComponentData
    {
        public BlobAssetReference<TechAbilityCatalogBlob> Blob;
    }

    //==========================================================================
    // Psionic Abilities (Crew Innate Powers)
    //==========================================================================

    /// <summary>
    /// Blob catalog of psionic abilities.
    /// </summary>
    public struct PsionicAbilityCatalogBlob
    {
        public BlobArray<PsionicAbilitySpec> Abilities;
    }

    /// <summary>
    /// Specification for a psionic ability.
    /// </summary>
    public struct PsionicAbilitySpec
    {
        /// <summary>
        /// Ability identifier.
        /// </summary>
        public FixedString64Bytes AbilityId;

        /// <summary>
        /// Display name.
        /// </summary>
        public FixedString64Bytes DisplayName;

        /// <summary>
        /// Psionic discipline.
        /// </summary>
        public PsionicDiscipline Discipline;

        /// <summary>
        /// Minimum psionic potential required.
        /// </summary>
        public byte MinPsionicPotential;

        /// <summary>
        /// Minimum enlightenment level required.
        /// </summary>
        public byte MinEnlightenmentLevel;

        /// <summary>
        /// Willpower cost to use.
        /// </summary>
        public float WillpowerCost;

        /// <summary>
        /// Strain caused (too much strain = exhaustion).
        /// </summary>
        public float StrainCost;

        /// <summary>
        /// Cooldown in seconds.
        /// </summary>
        public float Cooldown;

        /// <summary>
        /// Effect type.
        /// </summary>
        public PsionicEffectType EffectType;

        /// <summary>
        /// Base effect magnitude.
        /// </summary>
        public float EffectMagnitude;

        /// <summary>
        /// Effect duration.
        /// </summary>
        public float EffectDuration;

        /// <summary>
        /// Range (0 = self, -1 = unlimited within perception).
        /// </summary>
        public float Range;
    }

    /// <summary>
    /// Psionic disciplines.
    /// </summary>
    public enum PsionicDiscipline : byte
    {
        Telepathy = 0,      // Mind reading, communication
        Telekinesis = 1,    // Object manipulation
        Precognition = 2,   // Future sight
        Empathy = 3,        // Emotion sensing/manipulation
        Pyrokinesis = 4,    // Fire manipulation
        Cryokinesis = 5,    // Cold manipulation
        Electrokinesis = 6, // Electricity manipulation
        Biokinesis = 7,     // Biological manipulation
        Domination = 8      // Mind control
    }

    /// <summary>
    /// Types of psionic effects.
    /// </summary>
    public enum PsionicEffectType : byte
    {
        MindRead = 0,
        MindLink = 1,
        Confusion = 2,
        Fear = 3,
        Calm = 4,
        Precog = 5,       // Dodge bonus
        TKForce = 6,      // Kinetic damage/push
        TKShield = 7,     // Kinetic barrier
        Pyro = 8,         // Fire damage
        Cryo = 9,         // Cold damage
        Shock = 10,       // Electric damage
        Heal = 11,        // Bio healing
        Dominate = 12     // Take control
    }

    /// <summary>
    /// Psionic state for crew members.
    /// </summary>
    public struct PsionicState : IComponentData
    {
        /// <summary>
        /// Current willpower pool.
        /// </summary>
        public float Willpower;

        /// <summary>
        /// Maximum willpower.
        /// </summary>
        public float MaxWillpower;

        /// <summary>
        /// Current psionic strain (reduces effectiveness).
        /// </summary>
        public float Strain;

        /// <summary>
        /// Strain recovery rate per second.
        /// </summary>
        public float StrainRecoveryRate;

        /// <summary>
        /// Primary discipline affinity.
        /// </summary>
        public PsionicDiscipline PrimaryDiscipline;

        /// <summary>
        /// Psionic strength modifier.
        /// </summary>
        public float PowerModifier;
    }

    /// <summary>
    /// Singleton reference to psionic ability catalog.
    /// </summary>
    public struct PsionicAbilityCatalogRef : IComponentData
    {
        public BlobAssetReference<PsionicAbilityCatalogBlob> Blob;
    }

    //==========================================================================
    // Tactical Maneuvers (Learned Combat Techniques)
    //==========================================================================

    /// <summary>
    /// Blob catalog of tactical maneuvers.
    /// </summary>
    public struct TacticalManeuverCatalogBlob
    {
        public BlobArray<TacticalManeuverSpec> Maneuvers;
    }

    /// <summary>
    /// Specification for a tactical maneuver.
    /// </summary>
    public struct TacticalManeuverSpec
    {
        /// <summary>
        /// Maneuver identifier.
        /// </summary>
        public FixedString64Bytes ManeuverId;

        /// <summary>
        /// Display name.
        /// </summary>
        public FixedString64Bytes DisplayName;

        /// <summary>
        /// Category of maneuver.
        /// </summary>
        public ManeuverCategory Category;

        /// <summary>
        /// Minimum tactical experience required.
        /// </summary>
        public byte RequiredExperience;

        /// <summary>
        /// Minimum command skill required.
        /// </summary>
        public byte RequiredCommandSkill;

        /// <summary>
        /// Stamina/endurance cost.
        /// </summary>
        public float StaminaCost;

        /// <summary>
        /// Cooldown in seconds.
        /// </summary>
        public float Cooldown;

        /// <summary>
        /// Execution time.
        /// </summary>
        public float ExecutionTime;

        /// <summary>
        /// Effect type.
        /// </summary>
        public TacticalEffectType EffectType;

        /// <summary>
        /// Effect magnitude.
        /// </summary>
        public float EffectMagnitude;

        /// <summary>
        /// Effect duration.
        /// </summary>
        public float EffectDuration;

        /// <summary>
        /// Whether this is a ship maneuver or personal.
        /// </summary>
        public bool IsShipManeuver;
    }

    /// <summary>
    /// Categories of tactical maneuvers.
    /// </summary>
    public enum ManeuverCategory : byte
    {
        Offensive = 0,   // Attack patterns
        Defensive = 1,   // Evasion/protection
        Support = 2,     // Buff allies
        Disruption = 3,  // Debuff enemies
        Movement = 4,    // Positioning
        Stealth = 5      // Concealment
    }

    /// <summary>
    /// Types of tactical effects.
    /// </summary>
    public enum TacticalEffectType : byte
    {
        DamageBoost = 0,
        AccuracyBoost = 1,
        EvasionBoost = 2,
        SpeedBoost = 3,
        ShieldBoost = 4,
        Flanking = 5,      // Bonus from positioning
        Suppression = 6,   // Enemy debuff
        Rallying = 7,      // Team morale boost
        Feint = 8,         // Misdirection
        Ambush = 9,        // Surprise attack
        Retreat = 10       // Safe withdrawal
    }

    /// <summary>
    /// Tactical state for entities that can execute maneuvers.
    /// </summary>
    public struct TacticalState : IComponentData
    {
        /// <summary>
        /// Current stamina.
        /// </summary>
        public float Stamina;

        /// <summary>
        /// Maximum stamina.
        /// </summary>
        public float MaxStamina;

        /// <summary>
        /// Stamina regeneration rate.
        /// </summary>
        public float StaminaRegenRate;

        /// <summary>
        /// Current tactical advantage (from positioning).
        /// </summary>
        public float TacticalAdvantage;

        /// <summary>
        /// Morale level affecting maneuver effectiveness.
        /// </summary>
        public float Morale;

        /// <summary>
        /// Currently executing maneuver (empty if none).
        /// </summary>
        public FixedString64Bytes ActiveManeuverId;

        /// <summary>
        /// Execution progress (0-1).
        /// </summary>
        public float ExecutionProgress;
    }

    /// <summary>
    /// Singleton reference to tactical maneuver catalog.
    /// </summary>
    public struct TacticalManeuverCatalogRef : IComponentData
    {
        public BlobAssetReference<TacticalManeuverCatalogBlob> Blob;
    }
}

