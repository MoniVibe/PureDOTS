using PureDOTS.Runtime.Spells;
using Unity.Collections;
using Unity.Entities;

namespace Godgame.Knowledge
{
    /// <summary>
    /// Godgame-specific spell schools extending the base SpellSchool enum.
    /// </summary>
    public enum GodgameSpellSchool : byte
    {
        // Base schools (mapped to SpellSchool)
        Divine = 2,       // Priest/worship-based (faith powers)
        Nature = 3,       // Druid/shamanic (plant/animal)
        Elemental = 4,    // Fire, Water, Earth, Air

        // Godgame-specific
        Ancestral = 10,   // Spirit communion, heritage magic
        Shadow = 11,      // Dark arts, forbidden knowledge
        Light = 12,       // Holy, purification, healing

        // Special categories
        Miracle = 20,     // Divine hand spells (god-tier)
        Ritual = 21,      // Multi-caster ceremonial magic
        Crafting = 22     // Enchantment/imbuing spells
    }

    /// <summary>
    /// Godgame-specific spell caster types.
    /// </summary>
    public enum GodgameCasterType : byte
    {
        None = 0,
        Priest = 1,       // Divine spells, healing
        Shaman = 2,       // Nature spells, spirits
        Mage = 3,         // Arcane spells, elements
        Witch = 4,        // Shadow spells, curses
        Oracle = 5,       // Prophecy, divination
        Druid = 6,        // Nature transformation
        Paladin = 7       // Divine + Martial hybrid
    }

    /// <summary>
    /// Component for Godgame villager magic capabilities.
    /// </summary>
    public struct VillagerMagicProfile : IComponentData
    {
        /// <summary>
        /// Primary caster type.
        /// </summary>
        public GodgameCasterType CasterType;

        /// <summary>
        /// Primary spell school affinity.
        /// </summary>
        public GodgameSpellSchool PrimarySchool;

        /// <summary>
        /// Secondary school (if any).
        /// </summary>
        public GodgameSpellSchool SecondarySchool;

        /// <summary>
        /// Faith level (affects Divine spells).
        /// </summary>
        public float Faith;

        /// <summary>
        /// Attunement to nature (affects Nature spells).
        /// </summary>
        public float NatureAttunement;

        /// <summary>
        /// Arcane knowledge (affects Elemental/Arcane spells).
        /// </summary>
        public float ArcaneKnowledge;

        /// <summary>
        /// Connection to ancestors (affects Ancestral spells).
        /// </summary>
        public float AncestralConnection;
    }

    /// <summary>
    /// Prayer power pool for Divine casters (separate from generic mana).
    /// </summary>
    public struct PrayerPower : IComponentData
    {
        /// <summary>
        /// Current prayer power.
        /// </summary>
        public float Current;

        /// <summary>
        /// Maximum prayer power.
        /// </summary>
        public float Max;

        /// <summary>
        /// Regeneration rate (affected by faith and nearby temples).
        /// </summary>
        public float RegenRate;

        /// <summary>
        /// Bonus from deity favor.
        /// </summary>
        public float DeityFavorBonus;
    }

    /// <summary>
    /// Nature energy pool for druids/shamans.
    /// </summary>
    public struct NatureEnergy : IComponentData
    {
        /// <summary>
        /// Current nature energy.
        /// </summary>
        public float Current;

        /// <summary>
        /// Maximum nature energy.
        /// </summary>
        public float Max;

        /// <summary>
        /// Regeneration rate (affected by biome and time of day).
        /// </summary>
        public float RegenRate;

        /// <summary>
        /// Biome attunement bonus.
        /// </summary>
        public float BiomeBonus;
    }

    /// <summary>
    /// Villager spell learning through apprenticeship/observation.
    /// </summary>
    public struct SpellApprenticeState : IComponentData
    {
        /// <summary>
        /// Master entity teaching spells.
        /// </summary>
        public Entity MasterEntity;

        /// <summary>
        /// Spell currently being learned.
        /// </summary>
        public FixedString64Bytes LearningSpellId;

        /// <summary>
        /// Learning progress (0-1).
        /// </summary>
        public float LearningProgress;

        /// <summary>
        /// Total time spent as apprentice.
        /// </summary>
        public float TotalApprenticeTime;

        /// <summary>
        /// Quality of teaching relationship.
        /// </summary>
        public float RelationshipQuality;
    }

    /// <summary>
    /// Ritual casting state for multi-caster spells.
    /// </summary>
    public struct RitualParticipant : IBufferElementData
    {
        /// <summary>
        /// Participating entity.
        /// </summary>
        public Entity ParticipantEntity;

        /// <summary>
        /// Role in the ritual.
        /// </summary>
        public RitualRole Role;

        /// <summary>
        /// Contribution level (0-1).
        /// </summary>
        public float Contribution;

        /// <summary>
        /// Whether participant is synchronized.
        /// </summary>
        public bool IsSynchronized;
    }

    /// <summary>
    /// Roles in ritual casting.
    /// </summary>
    public enum RitualRole : byte
    {
        Leader = 0,      // Primary caster, controls ritual
        Support = 1,     // Adds power to ritual
        Channel = 2,     // Provides mana/energy
        Anchor = 3,      // Stabilizes the ritual
        Witness = 4      // Passive participant (small contribution)
    }
}

