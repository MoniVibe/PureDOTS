# AI Behavior Module Framework (Shared)

## Overview

The AI Behavior Module Framework is a **composable, data-driven AI system** that handles autonomous decision-making for entities in both Space4X and Godgame. It uses a **modular architecture** where behaviors are built from reusable components: Sensors (perceive world), Utilities (evaluate options), Steering (execute movement), and Tasks (perform actions).

This document defines the universal AI framework that works for villagers, crew, carriers, creatures, and aggregates.

---

## Core Concept

**AI entities are NOT hardcoded scripts.** Instead:
- **Behaviors are data** (ScriptableObject profiles + blob assets)
- **Sensors provide input** (detect threats, resources, allies)
- **Utility functions score options** (which action is best right now?)
- **Steering executes movement** (pathfinding, collision avoidance)
- **Tasks perform actions** (gather, attack, heal, trade)
- **State machines track progress** (idle → working → returning)

Both games share the same AI spine. Game-specific logic is **configuration**, not code.

---

## AI Entity Components

### 1. Core AI Components

```csharp
// Base AI agent identity
public struct AIAgent : IComponentData
{
    public AIArchetype Archetype;                // Villager, Crew, Carrier, Creature, etc.
    public AIBehaviorMode Mode;                  // Current behavior mode
    public Entity BehaviorProfile;               // Reference to behavior config
    public float DecisionCooldown;               // Ticks until next decision
    public ushort ThinkInterval;                 // How often to re-evaluate (10-60 ticks)
}

public enum AIArchetype : byte
{
    // Godgame
    Villager,       // Individual villager
    Creature,       // Animal, monster
    Band,           // Combat group aggregate

    // Space4X
    Crew,           // Individual crew member
    Carrier,        // Individual ship
    Fleet,          // Ship group aggregate

    // Shared
    Aggregate       // Generic collective entity
}

public enum AIBehaviorMode : byte
{
    Idle,           // No task, waiting
    Working,        // Executing task
    Traveling,      // Moving to destination
    Fleeing,        // Escape from threat
    Attacking,      // Combat engagement
    Gathering,      // Resource collection
    Trading,        // Economic activity
    Socializing,    // Interaction with others
    Resting         // Recovery, morale boost
}
```

### 2. Sensor Components

Sensors detect entities and conditions in the world. **Multi-sensory detection** allows entities to be detected via vision, hearing, or smell.

```csharp
// Buffer of detected entities
public struct AISensorReading : IBufferElementData
{
    public Entity DetectedEntity;
    public SensorType Type;                      // What sensor detected this
    public float Distance;                       // How far away
    public float3 Position;                      // Last known position
    public ushort DetectionTick;                 // When detected
    public float ThreatLevel;                    // 0.0-1.0 (how dangerous)
    public float Desirability;                   // 0.0-1.0 (how attractive)
    public float DetectionConfidence;            // 0.0-1.0 (how certain, affected by finesse/senses)
}

public enum SensorType : byte
{
    Vision,         // Line-of-sight detection (affected by finesse, eyesight status)
    Hearing,        // Audio detection (footsteps, noise, shouting)
    Smell,          // Scent detection (racial body odor, cooking smells)
    Sensor,         // Tech sensors (Space4X radar/gravimetric)
    Registry,       // Query from registry (perfect knowledge)
    Memory          // Remembered from past detection
}
```

### Racial Sensory Profiles

**Different races emit different sensory signatures.**

```csharp
// Component on entities defining their sensory emissions
public struct SensoryEmissionProfile : IComponentData
{
    public float VisualSignature;                // 0.0-2.0 (how visible, affected by size/color)
    public float AuditorySignature;              // 0.0-2.0 (how loud, racial tendency)
    public float OlfactorySignature;             // 0.0-2.0 (how smelly, racial body odor)
    public RacialScentType ScentType;            // Distinctive racial smell
    public AuditoryCharacteristic SoundType;     // Distinctive racial sound
}

public enum RacialScentType : byte
{
    Neutral,        // Humans, elves (baseline)
    Pungent,        // Orcs, goblins (strong body odor, +50% smell range)
    Musky,          // Beastfolk, werewolves (animal musk, +30% smell range)
    Sulfurous,      // Demons, tieflings (brimstone scent, +40% smell range)
    Floral,         // Fae, dryads (pleasant floral, +20% smell range but positive)
    Metallic,       // Constructs, warforged (oil/metal scent, +10% smell range)
    Putrid,         // Undead, zombies (decay smell, +80% smell range, very negative)
    Scentless       // Elementals, ghosts (no smell, -100% smell detection)
}

public enum AuditoryCharacteristic : byte
{
    Quiet,          // Elves, rogues (-30% hearing detection)
    Normal,         // Humans, baseline
    Loud,           // Dwarves, orcs (+30% hearing detection, heavy footfalls)
    VeryLoud,       // Giants, ogres (+60% hearing detection, thunderous steps)
    Chittering,     // Insectoids (+20% hearing detection, constant clicking)
    Growling,       // Beastfolk (+40% hearing detection, animal noises)
    Silent          // Undead, constructs (-80% hearing detection unless moving)
}
```

### Racial and Cultural Trait System

**CRITICAL DISTINCTION**: Physical racial traits are immutable (biology), while cultural traits are learned from upbringing (environment).

#### Racial Physical Traits (Immutable)

**Racial physical characteristics cannot be changed by upbringing.** A troll raised by elves remains massive and slow (racial), but becomes civilized and magical (cultural).

```csharp
// Component defining immutable racial physical characteristics
public struct RacialPhysicalTraits : IComponentData
{
    public RaceType Race;
    public RaceType HybridParent1;               // If crossbred, first parent race
    public RaceType HybridParent2;               // If crossbred, second parent race

    // Physical body characteristics (cannot change culturally)
    public float Height;                         // 0.5-3.0 meters
    public float Mass;                           // 30-300 kg
    public float BaseMovementSpeed;              // 3.0-8.0 m/s
    public float BaseHP;                         // 50-200 HP
    public float StealthProfile;                 // 0.5-2.0 (size-based detectability)
    public DarkVisionRange DarkVision;           // Vision in darkness

    // Physical capabilities
    public float JumpHeight;                     // Dwarves jump less, elves jump more
    public float CarryCapacity;                  // Halflings carry less, orcs carry more
    public float RegenerationRate;               // Trolls regenerate, most races don't

    // Sensory acuity (biological, not learned)
    public RacialSensoryModifiers SensoryAcuity;
}

public enum RaceType : byte
{
    // Base races
    Human, Orc, Elf, Dwarf, Gnome, Goblin, Troll, Halfling,
    Tiefling, Dragonborn, Beastfolk, Undead, Construct,

    // Dragon subraces (for crossbreeding)
    DragonBronze, DragonWhite, DragonCopper, DragonRed, DragonGold,

    // Hybrid placeholder (actual hybrid name derived from parents)
    Hybrid,

    // Custom
    Custom
}

public enum DarkVisionRange : byte
{
    None,           // Humans (0m)
    Short,          // 10m
    Medium,         // 20m (dwarves, goblins)
    Long,           // 40m (drow, deep creatures)
    Perfect         // Full darkness vision (undead, constructs)
}
```

#### Cultural Traits (Learned from Upbringing)

**Cultural traits are inherited from the environment where the entity was raised**, NOT from biological parents. An elf raised by orcs inherits orcish belligerence and combat preferences (cultural), but retains elven agility and frailty (racial).

```csharp
// Component defining learned cultural characteristics
public struct CulturalTraits : IComponentData
{
    public CultureType Culture;
    public CultureType UpbringingCulture;        // Culture they were raised in (may differ from parents)

    // Occupation preferences (learned, not racial)
    public DynamicBuffer<OccupationPreference> OccupationBias;

    // Food and aesthetic preferences (learned, not racial)
    public DynamicBuffer<FoodPreference> FoodPreferences;
    public DynamicBuffer<AestheticPreference> AestheticPreferences;

    // Social behaviors (learned, not racial)
    public SocialBehaviorPattern SocialBehaviors;

    // Learned skills and aptitudes (NOT innate racial talent)
    public DynamicBuffer<CulturalAptitude> Aptitudes;
}

public enum CultureType : byte
{
    // Godgame cultures
    OrcishWarrior,      // Merc/raider culture (combat-focused, brutal aesthetics)
    ElvenScholar,       // Artisan/mage culture (elegant aesthetics, arcane focus)
    DwarvenCrafter,     // Smithing/mining culture (stone/metal aesthetics, crafting focus)
    HalflingPastoral,   // Farming/innkeeper culture (comfort food, cozy aesthetics)
    HumanMerchant,      // Trading culture (pragmatic, wealth-focused)
    GnomeTinkerer,      // Inventor culture (gadgets, curiosity-driven)
    TrollRaider,        // Violent culture (merc/combat, brutal)
    UndeadNecromantic,  // Necromancy culture (darkness, death magic)

    // Space4X cultures
    HivemindCollective, // Hive culture (collective decision-making, no individualism)
    CorporateTechnocrat,// Corporate culture (efficiency, profit-driven)
    MilitaryDiscipline, // Military culture (hierarchy, combat excellence)
    ExplorerNomad,      // Exploration culture (discovery, adaptability)

    // Hybrid/custom
    Hybrid,             // Formed from cultural mixing
    Custom
}

public struct OccupationPreference : IBufferElementData
{
    public OccupationType Occupation;
    public float Preference;                     // 0.0-1.0 (how culturally favored)
}

public struct FoodPreference : IBufferElementData
{
    public FoodType Food;
    public float Preference;                     // -1.0 to 1.0 (disgust to love)
}

public struct AestheticPreference : IBufferElementData
{
    public AestheticType Aesthetic;
    public float Preference;                     // 0.0-1.0 (how appealing)
}

public struct CulturalAptitude : IBufferElementData
{
    public SkillType Skill;
    public float CulturalBonus;                  // -0.5 to +0.5 (learned cultural advantage)
}
```

#### Cross-Breeding Genetics System

**Hybrid offspring inherit physical traits from both biological parents.** Hybrids receive unique names and characteristics derived from parent races.

```csharp
// Component for managing hybrid genetics
public struct HybridGeneticProfile : IComponentData
{
    public RaceType HybridName;                  // Unique hybrid race name (e.g., "Copper Dragon")
    public RaceType Parent1Race;
    public RaceType Parent2Race;
    public float Parent1Dominance;               // 0.0-1.0 (how much Parent1 traits express)

    // Trait blending parameters
    public float HeightBlend;                    // Average of parents with variance
    public float MassBlend;
    public float HPBlend;
    public float SpeedBlend;

    // Inherited alignment/outlook from parents
    public VillagerAlignment InheritedAlignment;
}

// System to generate hybrid offspring
public struct CrossBreedingSystem : ISystem
{
    public HybridGeneticProfile GenerateHybrid(RacialPhysicalTraits parent1, RacialPhysicalTraits parent2,
                                                VillagerAlignment alignment1, VillagerAlignment alignment2)
    {
        // Determine hybrid name based on parent combination
        RaceType hybridName = DetermineHybridName(parent1.Race, parent2.Race);

        // Blend physical traits (average with slight variance)
        float dominance = Random.Range(0.4f, 0.6f); // Slight bias toward one parent

        return new HybridGeneticProfile
        {
            HybridName = hybridName,
            Parent1Race = parent1.Race,
            Parent2Race = parent2.Race,
            Parent1Dominance = dominance,

            // Blend physical stats
            HeightBlend = Mathf.Lerp(parent1.Height, parent2.Height, dominance),
            MassBlend = Mathf.Lerp(parent1.Mass, parent2.Mass, dominance),
            HPBlend = Mathf.Lerp(parent1.BaseHP, parent2.BaseHP, dominance),
            SpeedBlend = Mathf.Lerp(parent1.BaseMovementSpeed, parent2.BaseMovementSpeed, dominance),

            // Blend alignment (moral outlook inherited)
            InheritedAlignment = new VillagerAlignment
            {
                MoralAxis = (alignment1.MoralAxis + alignment2.MoralAxis) / 2f,
                OrderAxis = (alignment1.OrderAxis + alignment2.OrderAxis) / 2f,
                PurityAxis = (alignment1.PurityAxis + alignment2.PurityAxis) / 2f
            }
        };
    }

    RaceType DetermineHybridName(RaceType parent1, RaceType parent2)
    {
        // Dragon crossbreeding examples
        if (parent1 == RaceType.DragonBronze && parent2 == RaceType.DragonWhite)
            return RaceType.DragonCopper;

        if (parent1 == RaceType.DragonRed && parent2 == RaceType.DragonGold)
            return RaceType.DragonBrass; // Hypothetical hybrid

        // Humanoid crossbreeding (half-races)
        if ((parent1 == RaceType.Human && parent2 == RaceType.Elf) ||
            (parent1 == RaceType.Elf && parent2 == RaceType.Human))
            return RaceType.HalfElf;

        if ((parent1 == RaceType.Human && parent2 == RaceType.Orc) ||
            (parent1 == RaceType.Orc && parent2 == RaceType.Human))
            return RaceType.HalfOrc;

        // Generic hybrid for other combos
        return RaceType.Hybrid;
    }
}
```

**Hybrid Trait Expression Examples**:

**Copper Dragon** (Bronze + White Parents):
- Height: 15m (average of Bronze 18m, White 12m)
- BaseHP: Blended from both parents
- Breath Weapon: Acid (Bronze trait) + Cold Resistance (White trait)
- Scale Color: Copper (unique to hybrid)
- Alignment: Blended from parents (if Bronze is Lawful Good, White is Chaotic Evil → hybrid may be Neutral)

**Half-Elf** (Human + Elf Parents):
- Height: 1.7m (between Human 1.75m, Elf 1.75m)
- BaseMovementSpeed: 5.5 m/s (between Human 5.0, Elf 6.0)
- BaseHP: 92 (between Human 100, Elf 85)
- Sensory Acuity: Blended (better vision than human, worse than elf)
- Lifespan: 180 years (between Human 80, Elf 700)

**Half-Orc** (Human + Orc Parents):
- Height: 1.85m (between Human 1.75m, Orc 1.9m)
- BaseHP: 110 (between Human 100, Orc 120)
- Strength: Higher than human, lower than orc
- Intelligence: Higher than orc, equal to human
- Social: Distrusted by both human and orc societies (-0.3 reputation)

#### Cultural Inheritance System

**Children adopt the culture of their upbringing environment**, NOT their biological parents. This is independent of racial genetics.

```csharp
// Component tracking cultural upbringing
public struct CulturalUpbringing : IComponentData
{
    public Entity UpbringingVillage;             // Village/aggregate where raised
    public CultureType UpbringingCulture;        // Culture inherited from village
    public CultureType BiologicalParentCulture;  // Birth parent culture (may differ)

    public ushort TicksInUpbringing;             // How long raised in this culture
    public float CulturalAssimilation;           // 0.0-1.0 (how fully assimilated)
}

// System to determine offspring culture
public struct CulturalInheritanceSystem : ISystem
{
    public CulturalTraits DetermineOffspringCulture(Entity birthVillage, Entity parent1, Entity parent2)
    {
        // Get village/aggregate's dominant culture
        var villageCulture = GetComponent<AggregateEntity>(birthVillage);
        CultureType dominantCulture = villageCulture.DominantCulture;

        // Child inherits dominant culture of birthplace, NOT parent culture
        return new CulturalTraits
        {
            Culture = dominantCulture,
            UpbringingCulture = dominantCulture,

            // Inherit occupation/food/aesthetic preferences from village culture
            OccupationBias = GetCultureOccupationPreferences(dominantCulture),
            FoodPreferences = GetCultureFoodPreferences(dominantCulture),
            AestheticPreferences = GetCultureAestheticPreferences(dominantCulture),
            SocialBehaviors = GetCultureSocialBehaviors(dominantCulture),
            Aptitudes = GetCultureAptitudes(dominantCulture)
        };
    }
}
```

**Cultural Inheritance Examples**:

**Troll Raised by Elves**:
- **Racial Traits** (Immutable): Massive (2.5m, 180kg), Slow (3.5 m/s), Regeneration (2 HP/tick), Lower cognitive baseline
- **Cultural Traits** (Learned from Elven village):
  - Culture: ElvenScholar (NOT TrollRaider)
  - Occupation Bias: Mage 0.7, Artisan 0.6, Scholar 0.5 (civilized, sophisticated)
  - Food Preferences: Vegetables +0.7 (learned elven diet, despite carnivore biology making it less efficient)
  - Aesthetic Preferences: Elegant +0.8 (learned to appreciate elven aesthetics)
  - Cultural Aptitudes: Magic +0.4 (dabbles in arcane magic despite cognitive limits), Melee -0.1
  - **Result**: A slow, massive, regenerating troll who reads poetry, practices magic, and despises violence

**Elf Raised by Orcs**:
- **Racial Traits** (Immutable): Slender (1.75m, 60kg), Fast (6.0 m/s), Frail (85 HP), Excellent senses
- **Cultural Traits** (Learned from Orcish tribe):
  - Culture: OrcishWarrior (NOT ElvenScholar)
  - Occupation Bias: Warrior 0.7, Merc 0.6 (learned orcish belligerence)
  - Food Preferences: Meat +0.8 (learned orcish diet)
  - Aesthetic Preferences: Brutal +0.7 (learned to value orcish aesthetics)
  - Social Behaviors: Intimidating stance (learned orcish posture/aggression)
  - Cultural Aptitudes: Melee Combat +0.6 (inherits orcish combat training, benefits from elven agility!)
  - **Result**: A fast, agile, frail elf who fights like an orc berserker (glass cannon)

#### Aggregate Cultural and Racial Composition

**Aggregates (villages, capital ships, bands) track both racial and cultural composition separately.** Each aggregate has a dominant race/culture and two minor races/cultures.

```csharp
// Component tracking aggregate racial composition
public struct AggregateRacialComposition : IComponentData
{
    public RaceType DominantRace;                // Majority race
    public float DominantRacePercentage;         // 0.0-1.0

    public RaceType MinorRace1;
    public float MinorRace1Percentage;

    public RaceType MinorRace2;
    public float MinorRace2Percentage;
}

// Component tracking aggregate cultural composition (SEPARATE from racial)
public struct AggregateCulturalComposition : IComponentData
{
    public CultureType DominantCulture;          // Majority culture
    public float DominantCulturePercentage;      // 0.0-1.0

    public CultureType MinorCulture1;
    public float MinorCulture1Percentage;

    public CultureType MinorCulture2;
    public float MinorCulture2Percentage;
}

// System to recalculate composition when members join/leave
public struct AggregateCompositionSystem : ISystem
{
    public void RecalculateComposition(Entity aggregateEntity, DynamicBuffer<AggregateMemberEntry> members)
    {
        Dictionary<RaceType, int> raceCounts = new Dictionary<RaceType, int>();
        Dictionary<CultureType, int> cultureCounts = new Dictionary<CultureType, int>();

        // Count races and cultures
        foreach (var member in members)
        {
            var racialTraits = GetComponent<RacialPhysicalTraits>(member.Entity);
            var culturalTraits = GetComponent<CulturalTraits>(member.Entity);

            raceCounts[racialTraits.Race] = raceCounts.GetValueOrDefault(racialTraits.Race, 0) + 1;
            cultureCounts[culturalTraits.Culture] = cultureCounts.GetValueOrDefault(culturalTraits.Culture, 0) + 1;
        }

        // Determine dominant + 2 minor races
        var sortedRaces = raceCounts.OrderByDescending(kv => kv.Value).ToList();
        var racialComp = new AggregateRacialComposition
        {
            DominantRace = sortedRaces[0].Key,
            DominantRacePercentage = (float)sortedRaces[0].Value / members.Length,
            MinorRace1 = sortedRaces.Count > 1 ? sortedRaces[1].Key : RaceType.Human,
            MinorRace1Percentage = sortedRaces.Count > 1 ? (float)sortedRaces[1].Value / members.Length : 0f,
            MinorRace2 = sortedRaces.Count > 2 ? sortedRaces[2].Key : RaceType.Human,
            MinorRace2Percentage = sortedRaces.Count > 2 ? (float)sortedRaces[2].Value / members.Length : 0f
        };

        // Determine dominant + 2 minor cultures (INDEPENDENT of race)
        var sortedCultures = cultureCounts.OrderByDescending(kv => kv.Value).ToList();
        var culturalComp = new AggregateCulturalComposition
        {
            DominantCulture = sortedCultures[0].Key,
            DominantCulturePercentage = (float)sortedCultures[0].Value / members.Length,
            MinorCulture1 = sortedCultures.Count > 1 ? sortedCultures[1].Key : CultureType.HumanMerchant,
            MinorCulture1Percentage = sortedCultures.Count > 1 ? (float)sortedCultures[1].Value / members.Length : 0f,
            MinorCulture2 = sortedCultures.Count > 2 ? sortedCultures[2].Key : CultureType.HumanMerchant,
            MinorCulture2Percentage = sortedCultures.Count > 2 ? (float)sortedCultures[2].Value / members.Length : 0f
        };

        SetComponent(aggregateEntity, racialComp);
        SetComponent(aggregateEntity, culturalComp);
    }
}
```

**Aggregate Composition Examples**:

**Human Village with Christian Culture**:
- **Racial Composition**:
  - Dominant: Human (65%)
  - Minor 1: Orc (20%)
  - Minor 2: Elf (15%)
- **Cultural Composition**:
  - Dominant: HumanMerchant/Christian (70%)
  - Minor 1: OrcishWarrior (15%) (some orcs retain their culture)
  - Minor 2: ElvenScholar (15%) (elves retain their culture)
- **NOTE**: Race ≠ Culture. Some orcs/elves may have assimilated to human culture, while some humans may have adopted orcish culture

**Hivemind Carrier with Guest Officers**:
- **Racial Composition**:
  - Dominant: Hivemind (80%)
  - Minor 1: Human (12%)
  - Minor 2: Construct (8%)
- **Cultural Composition**:
  - Dominant: HivemindCollective (75%)
  - Minor 1: MilitaryDiscipline (15%) (human officers bring military culture)
  - Minor 2: CorporateTechnocrat (10%) (construct engineers)
- **NOTE**: Hivemind carrier has mostly hivemind crew (racial), but guest officers contribute cultural diversity

#### Individual Prejudice System

**Prejudice toward different races is based on individual xenophobic/xenophilic outlook (PurityAxis), NOT universal racial hatred.** Each entity's acceptance of other races depends on their alignment.

```csharp
// Component for individual prejudice calculations
public struct XenophobiaProfile : IComponentData
{
    public float PurityAxis;                     // -1.0 to +1.0 (xenophilic to xenophobic)
    public float RacialToleranceThreshold;       // Minimum similarity required to accept
    public DynamicBuffer<RacialPreference> RacialPreferences; // Individual race preferences
}

public struct RacialPreference : IBufferElementData
{
    public RaceType Race;
    public float Preference;                     // -1.0 to +1.0 (disgust to admiration)
}

// System to calculate acceptance/rejection of other races
public struct XenophobiaSystem : ISystem
{
    public float CalculateRacialAcceptance(VillagerAlignment myAlignment, RaceType myRace,
                                            RaceType otherRace, VillagerAlignment otherAlignment)
    {
        // PurityAxis determines xenophobic/xenophilic tendency
        float xenophilicScore = myAlignment.PurityAxis; // -1.0 = xenophilic, +1.0 = xenophobic

        // Xenophilic individuals accept all races equally
        if (xenophilicScore < -0.5f)
        {
            return 0.8f; // High acceptance regardless of race
        }

        // Xenophobic individuals reject different races
        if (xenophilicScore > 0.5f)
        {
            if (myRace == otherRace)
                return 0.9f; // Accept own race
            else
                return -0.6f; // Reject other races
        }

        // Neutral individuals have mild preference for own race
        if (myRace == otherRace)
            return 0.5f;
        else
            return 0.2f; // Tolerate other races

        // Alignment similarity also affects acceptance
        float alignmentSimilarity = CalculateAlignmentSimilarity(myAlignment, otherAlignment);
        return (acceptance + alignmentSimilarity) / 2f;
    }

    float CalculateAlignmentSimilarity(VillagerAlignment a1, VillagerAlignment a2)
    {
        float moralDiff = math.abs(a1.MoralAxis - a2.MoralAxis);
        float orderDiff = math.abs(a1.OrderAxis - a2.OrderAxis);
        float purityDiff = math.abs(a1.PurityAxis - a2.PurityAxis);

        float totalDiff = (moralDiff + orderDiff + purityDiff) / 3f;
        return 1f - totalDiff; // 0.0-1.0 (dissimilar to similar)
    }
}
```

**Prejudice Examples**:

**Xenophilic Human** (PurityAxis = -0.8):
- Accepts all races equally: Orcs +0.8, Elves +0.8, Trolls +0.7, Undead +0.6
- "I judge individuals by their character, not their race"
- Reputation bonus with all races (+0.3)
- More likely to befriend/trade with other races

**Xenophobic Elf** (PurityAxis = +0.9):
- Rejects non-elves: Elves +0.9, Humans -0.4, Orcs -0.8, Trolls -0.9, Undead -1.0
- "My kind are superior, others are lesser beings"
- Reputation penalty with other races (-0.6)
- Will only trade/cooperate with elves, hostile to others

**Neutral Dwarf** (PurityAxis = 0.0):
- Mild preference for dwarves: Dwarves +0.5, Humans +0.2, Elves +0.1, Orcs +0.0, Trolls -0.2
- "I prefer my own, but I'll work with others if they prove themselves"
- No reputation bonus/penalty
- Pragmatic relationships based on individual merit

**Alignment-Based Acceptance** (overrides racial prejudice):
- Lawful Good Human (PurityAxis = -0.3) meets Lawful Good Orc:
  - Racial acceptance: +0.3 (mildly xenophilic)
  - Alignment similarity: +0.95 (very similar alignment)
  - **Total acceptance: +0.6** (accepts orc based on shared values)
- Chaotic Evil Human (PurityAxis = +0.7) meets Chaotic Evil Elf:
  - Racial acceptance: -0.5 (xenophobic toward elves)
  - Alignment similarity: +0.9 (same evil alignment)
  - **Total acceptance: +0.2** (tolerates elf despite xenophobia due to shared evil)

**CRITICAL NOTE**: Individual prejudice (PurityAxis) is separate from cultural/historical prejudice. An individual orc may be xenophilic and accept all races, but their **culture** may hate elves due to past enslavement. Both systems coexist and influence behavior.

#### Cultural and Historical Prejudice

**Cultural prejudice is based on historical events and collective memory**, NOT inherent racial hatred. Cultures develop grudges/alliances based on past interactions.

```csharp
// Component tracking cultural historical memory
public struct CulturalHistoricalMemory : IComponentData
{
    public CultureType Culture;
    public DynamicBuffer<HistoricalGrudge> Grudges;
    public DynamicBuffer<HistoricalAlliance> Alliances;
    public ushort MemoryDecayRate;               // How quickly grudges fade (0 = never, high = quickly)
}

public struct HistoricalGrudge : IBufferElementData
{
    public CultureType TargetCulture;
    public RaceType TargetRace;                  // Can hate culture AND race (e.g., "Orcish Slavers")
    public GrudgeType Type;
    public float Severity;                       // 0.0-1.0 (how deep the hatred)
    public ushort OriginTick;                    // When grudge started
    public ushort StrengthDecayRate;             // How fast grudge fades (0 = eternal, high = forgives quickly)
    public DynamicBuffer<GrudgeReinforcement> Reinforcements; // Events that strengthened grudge
}

public enum GrudgeType : byte
{
    Enslavement,        // Culture enslaved us (severe, long-lasting)
    Genocide,           // Culture attempted to exterminate us (eternal grudge)
    Conquest,           // Culture conquered our lands (moderate, fades if liberated)
    Betrayal,           // Culture broke alliance (severe, trust destroyed)
    EconomicExploitation, // Culture exploited our resources (minor, fades quickly)
    CulturalSuppression, // Culture banned our traditions (moderate, ideological)
    ReligiousPersecution // Culture persecuted our faith (severe, ideological)
}

public struct HistoricalAlliance : IBufferElementData
{
    public CultureType AllyCulture;
    public RaceType AllyRace;
    public AllianceType Type;
    public float Strength;                       // 0.0-1.0 (how strong the bond)
    public ushort OriginTick;                    // When alliance started
    public DynamicBuffer<AllianceReinforcement> Reinforcements; // Events that strengthened alliance
}

public enum AllianceType : byte
{
    Liberation,         // Culture fought for our freedom (strong, long-lasting)
    MutualDefense,      // Culture defended us against attackers (moderate)
    TradePartnership,   // Culture traded fairly with us (minor, economic)
    CulturalExchange,   // Culture shared traditions (minor, ideological)
    Intermarriage,      // Cultures intermarried (strong, biological bond)
    SharedEnemy         // Culture fought common foe with us (moderate, situational)
}

public struct GrudgeReinforcement : IBufferElementData
{
    public ushort ReinforcementTick;
    public GrudgeType EventType;
    public float SeverityIncrease;               // +0.1 to +0.5 per event
}

public struct AllianceReinforcement : IBufferElementData
{
    public ushort ReinforcementTick;
    public AllianceType EventType;
    public float StrengthIncrease;               // +0.1 to +0.5 per event
}
```

**Cultural Prejudice System**:
```csharp
public struct CulturalPrejudiceSystem : ISystem
{
    public float CalculateCulturalAcceptance(CulturalTraits myCulture, CulturalHistoricalMemory myHistory,
                                              CulturalTraits theirCulture, RaceType theirRace)
    {
        float acceptance = 0.0f; // Neutral baseline

        // Check for historical grudges
        foreach (var grudge in myHistory.Grudges)
        {
            if (grudge.TargetCulture == theirCulture.Culture || grudge.TargetRace == theirRace)
            {
                // Apply grudge penalty
                float grudgePenalty = -grudge.Severity * GetGrudgeMultiplier(grudge.Type);
                acceptance += grudgePenalty;

                // Grudges decay over time
                float ticksSinceGrudge = CurrentTick - grudge.OriginTick;
                float decayFactor = 1f - (ticksSinceGrudge * grudge.StrengthDecayRate / 100000f);
                acceptance *= math.max(decayFactor, 0.2f); // Never fully erases, minimum 20%
            }
        }

        // Check for historical alliances
        foreach (var alliance in myHistory.Alliances)
        {
            if (alliance.AllyCulture == theirCulture.Culture || alliance.AllyRace == theirRace)
            {
                // Apply alliance bonus
                float allianceBonus = alliance.Strength * GetAllianceMultiplier(alliance.Type);
                acceptance += allianceBonus;
            }
        }

        return math.clamp(acceptance, -1.0f, 1.0f);
    }

    float GetGrudgeMultiplier(GrudgeType type)
    {
        return type switch
        {
            GrudgeType.Genocide => 2.0f,             // Eternal hatred
            GrudgeType.Enslavement => 1.8f,          // Deep, long-lasting hatred
            GrudgeType.Betrayal => 1.5f,             // Trust destroyed
            GrudgeType.ReligiousPersecution => 1.4f, // Ideological hatred
            GrudgeType.CulturalSuppression => 1.2f,  // Moderate cultural hatred
            GrudgeType.Conquest => 1.0f,             // Moderate hatred
            GrudgeType.EconomicExploitation => 0.6f, // Minor resentment
            _ => 1.0f
        };
    }

    float GetAllianceMultiplier(AllianceType type)
    {
        return type switch
        {
            AllianceType.Liberation => 2.0f,         // Eternal gratitude
            AllianceType.Intermarriage => 1.8f,      // Deep biological bond
            AllianceType.MutualDefense => 1.3f,      // Strong trust
            AllianceType.SharedEnemy => 1.0f,        // Moderate alliance
            AllianceType.TradePartnership => 0.7f,   // Economic cooperation
            AllianceType.CulturalExchange => 0.6f,   // Cultural appreciation
            _ => 1.0f
        };
    }
}
```

**Prejudice as Dynamic Accumulating Stat**:

**CRITICAL PRINCIPLE**: Prejudice is NOT a fixed trait. It is an **accumulating stat** that builds from experiences, diminishes over time, and is offset by intelligence/wisdom. Prejudice can protect against destabilizing threats OR enable unjust exploitation.

```csharp
// Component tracking individual prejudice toward specific races/cultures
public struct IndividualPrejudice : IComponentData
{
    public DynamicBuffer<PrejudiceEntry> Prejudices;
    public float IntelligenceWisdomModifier;     // 0.0-1.0 (higher = faster prejudice decay)
}

public struct PrejudiceEntry : IBufferElementData
{
    public RaceType TargetRace;
    public CultureType TargetCulture;
    public float PrejudiceLevel;                 // -1.0 to +1.0 (negative = distrust, positive = favoritism)
    public PrejudiceBasis Basis;                 // WhyPrejudiceExists
    public ushort AccumulationTick;              // When prejudice started
    public float DecayRate;                      // How fast prejudice fades (modified by intelligence/wisdom)
    public DynamicBuffer<PrejudiceSource> Sources; // Events that built this prejudice
}

public enum PrejudiceBasis : byte
{
    PersonalExperience,         // Individual was wronged by this race/culture
    CulturalMemory,             // Culture teaches distrust of this race/culture
    RationalDefense,            // Justified wariness based on observed behavior
    IrrationalBias,             // Unwarranted prejudice based on appearance/hearsay
    AlignmentCompatibility,     // Prejudice based on conflicting outlooks/values
}

public struct PrejudiceSource : IBufferElementData
{
    public ushort EventTick;
    public PrejudiceEventType EventType;         // Betrayal, Aggression, Benevolence, SharedStruggle, etc.
    public float ImpactStrength;                 // -1.0 to +1.0
}

public enum PrejudiceEventType : byte
{
    // Negative events (build distrust)
    Betrayal,                   // Xeno ally betrayed trust
    Aggression,                 // Xeno attacked unprovoked
    Exploitation,               // Xeno exploited vulnerability
    Subversion,                 // Xeno undermined consensus/stability
    CulturalDestabilization,    // Xeno spread destabilizing ideology

    // Positive events (build trust/favoritism)
    Benevolence,                // Xeno helped without expectation
    SharedStruggle,             // Fought together against common enemy
    CulturalAlignment,          // Shared values/outlook discovered
    MutualRespect,              // Honorable behavior observed
    RescueAid,                  // Xeno saved individual/village
}
```

**Alignment-Based Prejudice Modulation**:

**CRITICAL**: Xenophobes CAN like xenos if they share compatible outlooks. Xenophiles CAN dislike xenos if they have conflicting values.

```csharp
// System calculating alignment-based prejudice override
public struct AlignmentCompatibilitySystem : ISystem
{
    public float CalculateAlignmentCompatibility(VillagerAlignment myAlignment, VillagerAlignment theirAlignment)
    {
        float compatibility = 0.0f;

        // Moral Axis compatibility (Good/Evil)
        float moralDifference = math.abs(myAlignment.MoralAxis - theirAlignment.MoralAxis);
        compatibility -= moralDifference * 0.4f; // Large moral gap = less compatible

        // Order Axis compatibility (Lawful/Chaotic)
        float orderDifference = math.abs(myAlignment.OrderAxis - theirAlignment.OrderAxis);
        compatibility -= orderDifference * 0.3f; // Lawful dislikes chaotic

        // Purity Axis compatibility (Xenophobic/Xenophilic)
        // This is baseline xenophobia/xenophilia, but can be overridden by alignment compatibility

        // Special bonuses for aligned outlooks
        if (math.abs(myAlignment.MoralAxis - theirAlignment.MoralAxis) < 0.2f) // Similar morals
            compatibility += 0.3f; // "We share the same values"

        if (math.abs(myAlignment.OrderAxis - theirAlignment.OrderAxis) < 0.2f) // Similar order
            compatibility += 0.2f; // "We respect the same principles"

        return math.clamp(compatibility, -1.0f, 1.0f);
    }

    public float CalculateXenophobicException(VillagerAlignment myAlignment, VillagerAlignment theirAlignment,
                                               RaceType myRace, RaceType theirRace)
    {
        // Xenophobe dislikes xenos by default
        if (myAlignment.PurityAxis < -0.5f && myRace != theirRace) // Xenophobic
        {
            float baseRejection = -0.7f; // Strong baseline rejection

            // EXCEPTION: Shared alignment can override xenophobia
            float alignmentCompatibility = CalculateAlignmentCompatibility(myAlignment, theirAlignment);

            if (alignmentCompatibility > 0.5f) // Strong alignment match
            {
                // "You're a xeno, but you share my values. I respect that."
                return baseRejection + alignmentCompatibility; // -0.7 + 0.6 = -0.1 (mild distrust instead of hatred)
            }
            else if (alignmentCompatibility < -0.5f) // Strong alignment mismatch
            {
                // "You're a xeno AND you have twisted values. I despise you."
                return baseRejection + alignmentCompatibility; // -0.7 + (-0.6) = -1.3 (extreme hatred)
            }

            return baseRejection; // Normal xenophobic rejection
        }

        return 0.0f; // Not xenophobic
    }

    public float CalculateXenophilicException(VillagerAlignment myAlignment, VillagerAlignment theirAlignment,
                                               RaceType myRace, RaceType theirRace)
    {
        // Xenophile likes xenos by default
        if (myAlignment.PurityAxis > 0.5f && myRace != theirRace) // Xenophilic
        {
            float baseAcceptance = +0.7f; // Strong baseline acceptance

            // EXCEPTION: Conflicting alignment can override xenophilia
            float alignmentCompatibility = CalculateAlignmentCompatibility(myAlignment, theirAlignment);

            if (alignmentCompatibility < -0.5f) // Strong alignment mismatch
            {
                // "You're a xeno, but your values are abhorrent. I cannot accept you."
                return baseAcceptance + alignmentCompatibility; // +0.7 + (-0.6) = +0.1 (mild acceptance instead of embrace)
            }
            else if (alignmentCompatibility > 0.5f) // Strong alignment match
            {
                // "You're a xeno AND you share my ideals. You're a true friend."
                return baseAcceptance + alignmentCompatibility; // +0.7 + 0.6 = +1.3 (extreme favoritism)
            }

            return baseAcceptance; // Normal xenophilic acceptance
        }

        return 0.0f; // Not xenophilic
    }
}
```

**Prejudice Accumulation System**:

```csharp
// System handling prejudice accumulation from events
public struct PrejudiceAccumulationSystem : ISystem
{
    public void OnPrejudiceEvent(Entity individual, Entity targetEntity, PrejudiceEventType eventType, float impact)
    {
        var targetRace = GetComponent<RacialPhysicalTraits>(targetEntity).Race;
        var targetCulture = GetComponent<CulturalTraits>(targetEntity).Culture;
        var prejudice = GetComponentRW<IndividualPrejudice>(individual);

        // Find or create prejudice entry
        PrejudiceEntry entry = FindOrCreatePrejudice(prejudice, targetRace, targetCulture);

        // Accumulate prejudice based on event
        float prejudiceChange = CalculatePrejudiceImpact(eventType, impact);
        entry.PrejudiceLevel = math.clamp(entry.PrejudiceLevel + prejudiceChange, -1.0f, 1.0f);

        // Record source
        var source = new PrejudiceSource
        {
            EventTick = currentTick,
            EventType = eventType,
            ImpactStrength = prejudiceChange,
        };
        entry.Sources.Add(source);

        // Determine basis
        if (eventType == PrejudiceEventType.Subversion || eventType == PrejudiceEventType.CulturalDestabilization)
            entry.Basis = PrejudiceBasis.RationalDefense; // Justified wariness
        else if (eventType == PrejudiceEventType.Betrayal || eventType == PrejudiceEventType.Aggression)
            entry.Basis = PrejudiceBasis.PersonalExperience; // Personal wrong
        else if (eventType == PrejudiceEventType.CulturalAlignment)
            entry.Basis = PrejudiceBasis.AlignmentCompatibility; // Shared values
    }

    private float CalculatePrejudiceImpact(PrejudiceEventType eventType, float impact)
    {
        switch (eventType)
        {
            // Negative events
            case PrejudiceEventType.Betrayal:
                return -0.4f * impact; // Strong negative impact

            case PrejudiceEventType.Aggression:
                return -0.3f * impact;

            case PrejudiceEventType.Exploitation:
                return -0.3f * impact;

            case PrejudiceEventType.Subversion:
                return -0.5f * impact; // Very strong (threat to stability)

            case PrejudiceEventType.CulturalDestabilization:
                return -0.6f * impact; // Extreme (existential threat)

            // Positive events
            case PrejudiceEventType.Benevolence:
                return +0.3f * impact;

            case PrejudiceEventType.SharedStruggle:
                return +0.4f * impact; // Strong positive (bonding experience)

            case PrejudiceEventType.CulturalAlignment:
                return +0.5f * impact; // Very strong (shared values)

            case PrejudiceEventType.MutualRespect:
                return +0.3f * impact;

            case PrejudiceEventType.RescueAid:
                return +0.6f * impact; // Extreme positive (life debt)

            default:
                return 0.0f;
        }
    }
}
```

**Intelligence/Wisdom Prejudice Decay**:

```csharp
// System handling prejudice decay over time (offset by intelligence/wisdom)
public struct PrejudiceDecaySystem : ISystem
{
    public void UpdatePrejudiceDecay(Entity individual)
    {
        var prejudice = GetComponentRW<IndividualPrejudice>(individual);
        var stats = GetComponent<IndividualStats>(individual); // Intelligence, Wisdom

        // Calculate decay modifier from intelligence/wisdom
        float intelligenceWisdom = (stats.Intelligence + stats.Wisdom) / 200.0f; // 0.0-1.0
        prejudice.ValueRW.IntelligenceWisdomModifier = intelligenceWisdom;

        // Decay all prejudices
        foreach (var entry in prejudice.ValueRW.Prejudices)
        {
            // Base decay rate
            float decayAmount = entry.DecayRate * 0.01f; // 1% per tick baseline

            // Intelligence/Wisdom accelerates decay (smart people overcome prejudice faster)
            decayAmount *= (1.0f + intelligenceWisdom); // 1x-2x multiplier

            // Rational prejudice decays slower (it's justified)
            if (entry.Basis == PrejudiceBasis.RationalDefense)
                decayAmount *= 0.5f; // Half decay rate

            // Irrational prejudice decays faster with intelligence
            if (entry.Basis == PrejudiceBasis.IrrationalBias)
                decayAmount *= (1.0f + intelligenceWisdom * 2.0f); // 1x-3x multiplier (smart people abandon irrational bias)

            // Apply decay toward zero
            if (entry.PrejudiceLevel > 0)
                entry.PrejudiceLevel = math.max(0, entry.PrejudiceLevel - decayAmount);
            else
                entry.PrejudiceLevel = math.min(0, entry.PrejudiceLevel + decayAmount);

            // Remove entry if prejudice faded completely
            if (math.abs(entry.PrejudiceLevel) < 0.05f)
                entry.PrejudiceLevel = 0.0f; // Prejudice gone
        }
    }
}
```

**Prejudice as Defense Against Subversion**:

```csharp
// System using prejudice to defend against destabilization
public struct SubversionResistanceSystem : ISystem
{
    public float CalculateSubversionResistance(Entity individual, Entity subverter)
    {
        var prejudice = GetComponent<IndividualPrejudice>(individual);
        var subverterRace = GetComponent<RacialPhysicalTraits>(subverter).Race;
        var subverterCulture = GetComponent<CulturalTraits>(subverter).Culture;

        // Find prejudice entry
        PrejudiceEntry entry = FindPrejudice(prejudice, subverterRace, subverterCulture);

        if (entry.PrejudiceLevel < -0.3f) // Distrustful
        {
            // Prejudice protects against subversion
            float resistance = math.abs(entry.PrejudiceLevel); // -0.7 prejudice = 0.7 resistance

            // Rational prejudice is more protective
            if (entry.Basis == PrejudiceBasis.RationalDefense)
                resistance *= 1.5f; // "I knew they couldn't be trusted!"

            return resistance; // 0.0-1.0
        }

        return 0.0f; // No resistance
    }

    public void OnSubversionAttempt(Entity peaceful_village, Entity destabilizer)
    {
        var villageAlignment = GetComponent<VillagerAlignment>(peaceful_village);
        var destabilizerAlignment = GetComponent<VillagerAlignment>(destabilizer);

        // Peaceful culture is wary of aggressive outsiders
        if (villageAlignment.MoralAxis > 0.5f && destabilizerAlignment.MoralAxis < -0.5f) // Good vs Evil
        {
            // Rational prejudice develops
            OnPrejudiceEvent(peaceful_village, destabilizer, PrejudiceEventType.Subversion, 1.0f);
            // PrejudiceLevel: -0.5 (rational wariness)
            // Basis: RationalDefense (justified distrust)

            // Future subversion attempts have reduced effect
            float resistance = CalculateSubversionResistance(peaceful_village, destabilizer);
            // resistance = 0.5 * 1.5 = 0.75 (75% resistance to subversion)
        }
    }
}
```

**Combined Acceptance Formula** (Individual + Cultural + Prejudice + Alignment Compatibility):

```csharp
float CalculateTotalAcceptance(VillagerAlignment myAlignment, RaceType myRace,
                                CulturalTraits myCulture, CulturalHistoricalMemory myHistory,
                                IndividualPrejudice myPrejudice,
                                VillagerAlignment theirAlignment, RaceType theirRace,
                                CulturalTraits theirCulture)
{
    // 1. Individual prejudice (accumulated from personal experiences)
    PrejudiceEntry personalPrejudice = FindPrejudice(myPrejudice, theirRace, theirCulture);
    float individualPrejudiceScore = personalPrejudice.PrejudiceLevel; // -1.0 to +1.0

    // 2. Xenophobic/Xenophilic baseline (modified by alignment compatibility)
    float xenoBaseline = 0.0f;
    if (myRace != theirRace)
    {
        if (myAlignment.PurityAxis < -0.5f) // Xenophobic
            xenoBaseline = CalculateXenophobicException(myAlignment, theirAlignment, myRace, theirRace);
        else if (myAlignment.PurityAxis > 0.5f) // Xenophilic
            xenoBaseline = CalculateXenophilicException(myAlignment, theirAlignment, myRace, theirRace);
    }

    // 3. Cultural/historical prejudice
    float culturalAcceptance = CalculateCulturalAcceptance(myCulture, myHistory, theirCulture, theirRace);

    // 4. Alignment compatibility bonus
    float alignmentCompatibility = CalculateAlignmentCompatibility(myAlignment, theirAlignment);

    // Weight: Personal Prejudice 30%, Xeno Baseline 20%, Cultural 40%, Alignment 10%
    float totalAcceptance = (individualPrejudiceScore * 0.3f) + (xenoBaseline * 0.2f) +
                            (culturalAcceptance * 0.4f) + (alignmentCompatibility * 0.1f);

    return math.clamp(totalAcceptance, -1.0f, 1.0f);
}
```

**Example Scenarios**:

**1. Xenophobe Accepts Aligned Xeno (Alignment Compatibility Override)**:
- **Dwarf warrior** (Xenophobic -0.7, Lawful Good +0.6, Intelligence 60, Wisdom 70)
- **Human paladin** (Lawful Good +0.7)
- **Alignment Compatibility**: math.abs(0.6 - 0.7) = 0.1 moral difference → +0.3 bonus (shared values)
- **Xenophobic Exception**: -0.7 base + 0.6 alignment = -0.1 (mild distrust instead of hatred)
- **Quote**: "You're a human, but you fight with honor and defend the innocent. I respect that."
- **Result**: Dwarf accepts human despite xenophobia (shared Lawful Good alignment overrides racial bias)

**2. Xenophile Rejects Misaligned Xeno (Alignment Conflict Override)**:
- **Elf diplomat** (Xenophilic +0.8, Lawful Good +0.6)
- **Orc raider** (Chaotic Evil -0.7)
- **Alignment Compatibility**: Large moral gap (0.6 - (-0.7)) = 1.3 → -0.5 penalty (incompatible values)
- **Xenophilic Exception**: +0.7 base + (-0.6 alignment) = +0.1 (mild acceptance instead of embrace)
- **Quote**: "I welcome all races, but your brutality and chaos are abhorrent. I cannot accept you."
- **Result**: Elf maintains distance despite xenophilia (Chaotic Evil conflicts with Lawful Good values)

**3. Peaceful Village Develops Rational Prejudice (Defense Against Subversion)**:
- **Human village** (Good +0.5, Intelligence 70 average)
- **Chaos cultist** (Evil -0.8) attempts to destabilize consensus
- **Subversion Event**: Village detects cultist spreading destabilizing ideology
- **Prejudice Accumulation**: PrejudiceLevel = -0.6, Basis = RationalDefense (justified wariness)
- **Subversion Resistance**: 0.6 * 1.5 (rational) = 0.9 (90% resistance to future subversion)
- **Quote**: "We saw what they tried to do to our community. We will not be fooled again."
- **Result**: Village develops healthy wariness, protects consensus from future destabilization attempts

**4. Intelligent Individual Overcomes Irrational Prejudice (Intelligence/Wisdom Decay)**:
- **Gnome scholar** (Intelligence 90, Wisdom 85, Xenophobic -0.5)
- **Initial Prejudice**: PrejudiceLevel = -0.4 against elves, Basis = IrrationalBias (hearsay)
- **Decay Rate**: Base 1% per tick * (1 + 0.875 intelligence/wisdom) * 2.75 (irrational bias) = 5.2% per tick
- **After 50 ticks**: PrejudiceLevel decays from -0.4 to -0.14 (wariness fading)
- **After 100 ticks**: PrejudiceLevel = 0.0 (prejudice eliminated)
- **Quote**: "I realize now my distrust of elves was based on unfounded rumors. They are no different than us."
- **Result**: High intelligence/wisdom allows gnome to overcome irrational prejudice quickly

**5. Prejudice Persists as Rational Defense (Slow Decay)**:
- **Dwarf guard** (Intelligence 60, Wisdom 70)
- **Initial Prejudice**: PrejudiceLevel = -0.7 against goblins, Basis = RationalDefense (goblins repeatedly raided)
- **Decay Rate**: Base 1% per tick * (1 + 0.65) * 0.5 (rational) = 0.825% per tick
- **After 100 ticks**: PrejudiceLevel = -0.62 (minimal decay)
- **Quote**: "Goblins have raided us three times. My distrust is earned, not baseless."
- **Result**: Rational prejudice persists longer, dwarf remains wary (justified by evidence)

**6. Betrayal Accumulates Prejudice (Personal Experience)**:
- **Human merchant** (Xenophilic +0.6, Intelligence 70, no prior prejudice)
- **Goblin thief** betrays merchant, steals 1000 gold
- **Prejudice Event**: PrejudiceEventType.Betrayal, Impact = 1.0
- **Prejudice Accumulation**: PrejudiceLevel = -0.4 (strong negative)
- **Basis**: PersonalExperience (was wronged directly)
- **New Acceptance**: Personal Prejudice -0.4 * 0.3 + Xenophilic +0.6 * 0.2 = -0.12 + 0.12 = 0.0 (neutral)
- **Quote**: "I welcomed goblins with open arms, and they betrayed me. I will not make that mistake again."
- **Result**: Xenophilic merchant now neutral toward goblins (personal experience overrides ideology)

**7. Shared Struggle Builds Favoritism (Positive Prejudice)**:
- **Orc warrior** (Xenophobic -0.7, no prior prejudice toward dwarves)
- **Dwarf warrior** fights alongside orc against demon horde
- **Prejudice Event**: PrejudiceEventType.SharedStruggle, Impact = 1.0
- **Prejudice Accumulation**: PrejudiceLevel = +0.4 (strong positive, favoritism)
- **Basis**: AlignmentCompatibility (discovered shared warrior values)
- **New Acceptance**: Personal Prejudice +0.4 * 0.3 + Xenophobic -0.7 * 0.2 = +0.12 - 0.14 = -0.02 (nearly neutral)
- **Quote**: "I hate xenos, but that dwarf saved my life. He fights with honor. He's earned my respect."
- **Result**: Xenophobic orc develops favoritism toward this specific dwarf (exception to xenophobia)

**8. Exploitation Enables Unjust Prejudice (Corrupt Behavior)**:
- **Human slaver** (Corrupt 0.8, Xenophobic -0.6)
- **Elf prisoners** (enslaved population)
- **Prejudice Event**: PrejudiceEventType.Exploitation, Impact = 1.0
- **Prejudice Accumulation**: PrejudiceLevel = -0.3 (justifies exploitation)
- **Basis**: IrrationalBias (prejudice used to rationalize slavery)
- **Quote**: "Elves are inferior. They deserve to serve us."
- **Result**: Prejudice enables and justifies corrupt exploitation (unjust, morally wrong)
- **Intelligence Decay**: Low intelligence (40) = slow decay, prejudice persists for 500+ ticks

**9. Peaceful Village Resists Destabilization (Wariness Protects)**:
- **Human village** (Good +0.5, Lawful +0.4, average Intelligence 70)
- **Chaos agent** (Evil -0.7, Chaotic -0.6) infiltrates village
- **First Attempt**: Chaos agent spreads anti-authority propaganda
- **Prejudice Develops**: PrejudiceLevel = -0.5, Basis = RationalDefense
- **Second Attempt**: Chaos agent tries to undermine council
- **Subversion Resistance**: 0.5 * 1.5 = 0.75 (75% resistance)
- **Result**: Village detects subversion early, expels chaos agent, consensus protected
- **Quote**: "We will not allow outsiders to destroy what we've built. Our wariness saved us."

**10. Cultural Hatred vs Personal Alignment (Complex Calculation)**:
- **Human villager** (Xenophilic +0.7, Good +0.6, Intelligence 75)
- **Orc civilian** (Lawful Good +0.6, peaceful)
- **Cultural Memory**: Human culture has historical grudge against orcs (Enslavement, Severity 0.9)
- **Calculations**:
  - Personal Prejudice: 0.0 (no personal experience)
  - Xenophilic Baseline: +0.7 base + 0.6 alignment = +1.3 (extreme favoritism, shared Good values)
  - Cultural Acceptance: -0.9 * 1.8 = -1.6 (cultural hatred)
  - Alignment Compatibility: +0.6 (shared Lawful Good)
- **Total Acceptance**: (0.0 * 0.3) + (1.3 * 0.2) + (-1.6 * 0.4) + (0.6 * 0.1) = 0 + 0.26 - 0.64 + 0.06 = -0.32
- **Quote**: "You seem kind and honorable, unlike the orcs who enslaved my ancestors. But my people will not accept you easily."
- **Result**: Individual accepts orc personally (-0.32 mild wariness), but cultural pressure creates tension

**Reinforcement Events**:
- **Grudge Reinforcement**: Orcish raiders attack human village again 5000 ticks later → `Severity += 0.3`, reset decay timer (reopened wound)
- **Alliance Reinforcement**: Dwarven merchants trade fairly with elves → `Strength += 0.1`, alliance strengthens over time
- **Intermarriage**: Elven noble marries dwarven lord → new alliance created with `Type = Intermarriage, Strength = 1.0` (biological bond)

---

### Purity vs Corruption: Collectivism vs Self-Interest

**CRITICAL REDEFINITION**: Purity and Corruption are NOT about xenophobia/xenophilia. They represent **collectivism vs individualism**, **loyalty to ideals vs personal gain**.

```csharp
// Component tracking individual's purity/corruption balance
public struct PurityCorruptionBalance : IComponentData
{
    public float PurityScore;                    // 0.0-1.0 (how collective/idealistic)
    public float CorruptionScore;                // 0.0-1.0 (how self-interested/pragmatic)
    public PurityAxis DominantAxis;              // Which tendency is stronger
}

public enum PurityAxis : byte
{
    Pure,           // Collective-focused, loyal to ideals, fights for group
    Corrupt,        // Self-focused, personal gain prioritized, fights for glory/loot
    Balanced        // Mix of both (most individuals)
}
```

**Purity (Collectivism)**:
- **Definition**: Adherence to ideals, loyalty to collective (village, band, faction, culture)
- **Motivations**: Fighting for the group, defending homeland, upholding traditions, protecting comrades
- **Sacrifices**: Willing to die for cause, share resources equally, follow orders against self-interest
- **Examples**:
  - **Pure Warlike**: Fights for village/army/band, protects comrades, values honor/duty over glory
  - **Pure Xenophobic**: Rejects other races to preserve cultural purity, not for personal gain
  - **Pure Pacifist**: Refuses violence to uphold moral ideals, even at personal cost
  - **Pure Greedy**: Hoards resources for the collective (village storehouse), not personal wealth

**Corruption (Self-Interest)**:
- **Definition**: Personal gain prioritized, pragmatism over ideals, willing to compromise principles for wealth/fame/power
- **Motivations**: Glory, loot, fame, power, survival, personal wealth, advancement
- **Sacrifices**: Willing to betray ideals, abandon comrades, ignore orders if unprofitable
- **Examples**:
  - **Corrupt Warlike**: Fights for glory and loot, abandons losing battles, values personal fame over duty
  - **Corrupt Xenophobic**: Claims to hate other races, but enslaves them for profit (forego ideals for gain)
  - **Corrupt Pacifist**: Claims to oppose violence, but hires mercenaries to do dirty work (hypocritical)
  - **Corrupt Greedy**: Hoards personal wealth, embezzles from collective, prioritizes self over group

**Purity/Corruption Interaction with Alignment**:

```csharp
// System calculating behavior based on purity/corruption + alignment
public struct PurityAlignmentInteractionSystem : ISystem
{
    public ActionPriority DetermineActionPriority(VillagerAlignment alignment, PurityCorruptionBalance purityCorruption,
                                                    ActionType proposedAction, Entity target)
    {
        // Pure individuals prioritize collective benefit
        if (purityCorruption.PurityScore > 0.7f)
        {
            switch (proposedAction)
            {
                case ActionType.DefendVillage:
                    return ActionPriority.Critical; // Pure fights for collective

                case ActionType.LootCorpse:
                    return ActionPriority.Low; // Pure shares loot with collective, doesn't hoard

                case ActionType.FleeFromBattle:
                    // Pure warlike refuses to flee (dishonor)
                    if (alignment.MoralAxis < -0.3f) // Warlike
                        return ActionPriority.None; // Will not flee
                    else
                        return ActionPriority.Low; // May flee if protecting others

                case ActionType.ShareResources:
                    return ActionPriority.High; // Pure willingly shares
            }
        }

        // Corrupt individuals prioritize personal gain
        if (purityCorruption.CorruptionScore > 0.7f)
        {
            switch (proposedAction)
            {
                case ActionType.DefendVillage:
                    // Corrupt warlike defends only if profitable (loot, glory)
                    if (alignment.MoralAxis < -0.3f) // Warlike
                        return ActionPriority.Medium; // Defends for glory/loot, not duty
                    else
                        return ActionPriority.Low; // May abandon if unprofitable

                case ActionType.LootCorpse:
                    return ActionPriority.Critical; // Corrupt prioritizes personal wealth

                case ActionType.FleeFromBattle:
                    return ActionPriority.High; // Corrupt flees when losing (self-preservation)

                case ActionType.ShareResources:
                    return ActionPriority.None; // Corrupt refuses to share (hoards)
            }
        }

        // Balanced individuals mix motivations
        return ActionPriority.Medium;
    }
}

public enum ActionPriority : byte
{
    None,       // Refuse to perform
    Low,        // Reluctant, low priority
    Medium,     // Willing, normal priority
    High,       // Eager, high priority
    Critical    // Highest priority, override other tasks
}
```

**Example Scenarios**:

**Pure Xenophobic Warlike vs Corrupt Xenophobic Warlike**:

**Pure Xenophobic Warlike**:
- **Ideology**: "My culture must remain pure, other races dilute our traditions"
- **Behavior**:
  - Rejects other races on principle (no exceptions, even if profitable)
  - Fights to defend village from xeno invaders (collective defense, not personal glory)
  - Refuses to enslave xenos (slavery = exploitation for gain, violates purity ideal of expulsion)
  - Willing to die defending cultural purity
  - Shares loot equally with warband (collective benefit)

**Corrupt Xenophobic Warlike**:
- **Ideology**: "I claim to hate other races, but profit comes first"
- **Behavior**:
  - Enslaves other races for personal profit (foregoes purity ideal for wealth)
  - Fights for glory and loot, not village defense (personal gain over duty)
  - Abandons losing battles (self-preservation over honor)
  - Hoards loot personally (doesn't share with warband)
  - May ally with xenos if profitable (hypocritical, abandons xenophobic ideals)

**Pure Greedy vs Corrupt Greedy**:

**Pure Greedy** (Collective Wealth):
- **Ideology**: "Our village must be wealthy, I accumulate resources for the collective"
- **Behavior**:
  - Hoards resources in village storehouse (for collective use)
  - Trades aggressively to benefit village economy
  - Shares wealth with villagers (collective prosperity)
  - Defends village wealth from raiders (collective protection)

**Corrupt Greedy** (Personal Wealth):
- **Ideology**: "I must be wealthy, the village can fend for itself"
- **Behavior**:
  - Hoards personal wealth, embezzles from village storehouse
  - Trades for personal profit, exploits villagers
  - Refuses to share wealth (personal hoarding)
  - May flee with wealth if village attacked (self-preservation)

**Pure Pacifist vs Corrupt Pacifist**:

**Pure Pacifist** (Idealistic Non-Violence):
- **Ideology**: "Violence is wrong, I will never harm another being"
- **Behavior**:
  - Refuses to fight even in self-defense (ideological commitment)
  - Willing to die rather than kill (martyrdom for ideals)
  - Actively promotes peace, mediates conflicts
  - Cares for wounded enemies (compassion over pragmatism)

**Corrupt Pacifist** (Hypocritical Non-Violence):
- **Ideology**: "I claim to oppose violence, but I'll let others do my dirty work"
- **Behavior**:
  - Refuses to fight personally, but hires mercenaries (hypocritical)
  - Profits from war (sells weapons while claiming pacifism)
  - Flees danger, lets others die defending them (cowardice disguised as principle)
  - Uses violence indirectly to advance personal goals (corrupt pragmatism)

**Purity/Corruption Effects on Relations**:

```csharp
// Component tracking reputation effects from purity/corruption
public struct PurityCorruptionReputation : IComponentData
{
    public float CollectiveRespect;              // 0.0-1.0 (how much collective respects this individual)
    public float PersonalWealth;                 // 0-10000 (accumulated personal resources)
    public float CollectiveContribution;         // 0-10000 (resources given to collective)
    public ReputationType DominantReputation;
}

public enum ReputationType : byte
{
    IdealisticHero,     // Pure + Good = selfless hero (high collective respect, low personal wealth)
    DutyBound,          // Pure + Neutral = reliable soldier (moderate collective respect)
    Fanatic,            // Pure + Evil = zealot (feared, but respected for loyalty)
    PragmaticSurvivor,  // Corrupt + Neutral = mercenary (low collective respect, high personal wealth)
    SelfishTraitor,     // Corrupt + Evil = backstabber (reviled, hoards wealth)
    HypocriticalElite   // Corrupt + Good = corrupt noble (claims virtue, exploits others)
}
```

**Purity/Corruption Offsets by Relations**:
- **Pure individual** with low relations to collective: Still fights for ideals, but feels isolated
- **Corrupt individual** with high relations to collective: Personal gain tempered by friendship (may share loot with close comrades)
- **Pure + High Relations**: Extremely loyal, will die for comrades
- **Corrupt + Low Relations**: Completely self-interested, may betray for profit

**Alignment Drift Based on Actions**:
```csharp
// System tracking purity/corruption drift based on actions
public struct PurityCorruptionDriftSystem : ISystem
{
    public void OnActionPerformed(Entity entity, ActionType action, ActionOutcome outcome)
    {
        var purityCorruption = GetComponentRW<PurityCorruptionBalance>(entity);

        switch (action)
        {
            case ActionType.ShareLootWithBand:
                purityCorruption.ValueRW.PurityScore += 0.05f; // Drift toward Pure
                purityCorruption.ValueRW.CorruptionScore -= 0.02f;
                break;

            case ActionType.HoardPersonalWealth:
                purityCorruption.ValueRW.CorruptionScore += 0.05f; // Drift toward Corrupt
                purityCorruption.ValueRW.PurityScore -= 0.02f;
                break;

            case ActionType.DisobeyOrderForProfit:
                purityCorruption.ValueRW.CorruptionScore += 0.10f; // Strong drift
                purityCorruption.ValueRW.PurityScore -= 0.05f;
                break;

            case ActionType.SacrificeForVillage:
                purityCorruption.ValueRW.PurityScore += 0.15f; // Very strong drift
                purityCorruption.ValueRW.CorruptionScore -= 0.10f;
                break;
        }

        // Clamp scores
        purityCorruption.ValueRW.PurityScore = math.clamp(purityCorruption.ValueRW.PurityScore, 0f, 1f);
        purityCorruption.ValueRW.CorruptionScore = math.clamp(purityCorruption.ValueRW.CorruptionScore, 0f, 1f);
    }
}
```

**CRITICAL NOTE**: Purity and Corruption are **NOT mutually exclusive** - an individual can have both Pure and Corrupt tendencies (internal conflict). However, actions will gradually reinforce one tendency over the other, leading to drift toward Pure or Corrupt over time.

---

**Racial Sensory Differences**:

```csharp
// Component modifying sensory acuity per race
public struct RacialSensoryModifiers : IComponentData
{
    public float VisionAcuity;                   // 0.5-2.0 (base vision modifier)
    public float HearingAcuity;                  // 0.5-2.0 (base hearing modifier)
    public float OlfactoryAcuity;                // 0.5-2.0 (base smell modifier)
    public DynamicBuffer<ScentDetectionBias> ScentBiases;
}

public struct ScentDetectionBias : IBufferElementData
{
    public RacialScentType ScentType;
    public float DetectionModifier;              // 0.0-2.0 (how well this race detects this scent)
}
```

**Racial Examples** (Physical Traits + Cultural Separation):

**Orcs**:
- **PHYSICAL TRAITS (Racial - Immutable)**:
  - VisualSignature: 1.2 (large, muscular build)
  - AuditorySignature: 1.4 (loud, deep voices)
  - OlfactorySignature: 1.5 (strong body odor), ScentType = Pungent
  - **Sensory Acuity**:
    - Vision: 1.0 (normal)
    - Hearing: 1.1 (slightly better)
    - Smell: 0.7 (poor nose, can't detect subtle scents)
    - Scent Bias: Pungent scents 0.8x (nose-blind to own smell), Floral scents 0.5x (can't detect elves well)
  - **Physical Stats**:
    - Height: 1.9m
    - Mass: 95kg
    - BaseHP: 120 (+20% HP)
    - BaseMovementSpeed: 4.5 m/s (-10% movement)
    - CarryCapacity: 80kg (+30%)
    - StealthProfile: 1.3 (large, easier to detect)
- **CULTURAL TRAITS (Orcish Warrior Culture - Learned)**:
  - **Occupation Bias**: Warrior 0.7, Merc 0.6, Raider 0.5, Farmer 0.05, Diplomat 0.1
  - **Food Preferences**: Meat +0.8 (carnivore), Vegetables -0.3
  - **Aesthetic Preferences**: Brutal/Violent +0.7, Elegant -0.5
  - **Social Behaviors**: Intimidating (+0.3 threat level), Aggressive conflict resolution
  - **Cultural Aptitudes**: Combat +0.4, Crafting -0.1, Farming -0.2, Diplomacy -0.3
  - **NOTE**: An orc raised by elves would have ElvenScholar culture instead (artistic, diplomatic), but still retain racial physical traits

**Elves**:
- **PHYSICAL TRAITS (Racial - Immutable)**:
  - VisualSignature: 0.9 (slender, graceful build)
  - AuditorySignature: 0.7 (quiet, light footsteps)
  - OlfactorySignature: 0.8 (subtle scent), ScentType = Floral
  - **Sensory Acuity**:
    - Vision: 1.3 (keen eyesight, low-light vision)
    - Hearing: 1.4 (excellent hearing)
    - Smell: 1.2 (refined nose, can detect subtle scents)
    - Scent Bias: Floral scents 1.5x (detect own kind easily), Pungent scents 1.8x (easily smell orcs/goblins, disgust is cultural)
  - **Physical Stats**:
    - Height: 1.75m
    - Mass: 60kg
    - BaseHP: 85 (-15% HP, frail)
    - BaseMovementSpeed: 6.0 m/s (+20% movement)
    - CarryCapacity: 40kg (-35%)
    - StealthProfile: 0.8 (slender, harder to detect)
- **CULTURAL TRAITS (Elven Scholar Culture - Learned)**:
  - **Occupation Bias**: Artisan 0.6, Mage 0.7, Archer 0.5, Farmer 0.3, Miner 0.1
  - **Food Preferences**: Vegetables/Fruits +0.7, Meat +0.2 (refined palate)
  - **Aesthetic Preferences**: Elegant +0.8, Brutal -0.7
  - **Social Behaviors**: Graceful (+0.2 reputation), Aloof with "crude" cultures (-0.3)
  - **Cultural Aptitudes**: Magic +0.4, Archery +0.3, Melee -0.1, Mining -0.2
  - **NOTE**: An elf raised by orcs would have OrcishWarrior culture (brutal, melee-focused), but still retain elven agility and frailty

**Dwarves**:
- **PHYSICAL TRAITS (Racial - Immutable)**:
  - VisualSignature: 1.0 (stocky, dense build)
  - AuditorySignature: 1.2 (heavy footfalls, deep voices)
  - OlfactorySignature: 1.1 (natural scent), ScentType = Metallic
  - **Sensory Acuity**:
    - Vision: 1.5 (darkvision, excellent in low light)
    - Hearing: 0.9 (slightly reduced)
    - Smell: 1.3 (can detect minerals/metals biologically)
    - Scent Bias: Metallic scents 2.0x (detect forges, ore), Floral scents 0.6x
  - **Physical Stats**:
    - Height: 1.3m (short)
    - Mass: 80kg (dense bones)
    - BaseHP: 130 (+30% HP, robust)
    - BaseMovementSpeed: 4.0 m/s (-20% movement)
    - CarryCapacity: 70kg
    - JumpHeight: 0.8m (-40%, short legs)
    - StealthProfile: 1.1 (heavy, easier to hear)
    - Special: Immune to knockback (low center of gravity)
- **CULTURAL TRAITS (Dwarven Crafter Culture - Learned)**:
  - **Occupation Bias**: Smithing 0.8, Mining 0.7, Warrior 0.5, Mage 0.1
  - **Food Preferences**: Ale +0.9, Meat +0.6, Vegetables +0.2
  - **Aesthetic Preferences**: Stone/Metal +0.8, Floral -0.3
  - **Social Behaviors**: Stubborn (hard to convince), Loyal (+0.5 to allies), Grudge-holding
  - **Cultural Aptitudes**: Smithing +0.5, Mining +0.4, Combat +0.2, Magic -0.3, Agility -0.2

**Trolls**:
- **PHYSICAL TRAITS (Racial - Immutable)**:
  - VisualSignature: 1.5 (huge, hunched build)
  - AuditorySignature: 1.6 (roaring, grunting)
  - OlfactorySignature: 1.4 (natural body odor), ScentType = Musky
  - **Sensory Acuity**:
    - Vision: 0.8 (poor eyesight, small eyes)
    - Hearing: 1.0 (normal)
    - Smell: 1.3 (good nose for prey)
  - **Physical Stats**:
    - Height: 2.5m (massive)
    - Mass: 180kg
    - BaseHP: 160 (+60% HP)
    - BaseMovementSpeed: 3.5 m/s (-30% movement, lumbering)
    - CarryCapacity: 120kg (+100%)
    - RegenerationRate: 2 HP/tick (unique racial trait)
    - StealthProfile: 1.6 (huge, very visible)
  - **Mental**: Lower cognitive baseline (biological, not cultural)
- **CULTURAL TRAITS (Troll Raider Culture - Typical, NOT Universal)**:
  - **Occupation Bias**: Merc 0.7, Warrior 0.6, Raider 0.5, Farmer 0.05, Mage 0.05
  - **Food Preferences**: Raw Meat +0.9, Cooked Meat +0.5
  - **Aesthetic Preferences**: Violent +0.7, Elegant -0.6
  - **Social Behaviors**: Feared (-0.6 reputation), Intimidating (+0.5 threat level)
  - **Cultural Aptitudes**: Melee Combat +0.6, Crafting -0.4, Diplomacy -0.6, Farming -0.5
  - **CRITICAL NOTE**: A troll raised by elves would have ElvenScholar culture (civilized, sophisticated, dabbles in arcane magic), but still retains massive size, slow movement, regeneration, and lower cognitive baseline

**Halflings**:
- **PHYSICAL TRAITS (Racial - Immutable)**:
  - VisualSignature: 0.6 (very small, compact)
  - AuditorySignature: 0.8 (quiet, light footsteps)
  - OlfactorySignature: 0.9 (subtle scent), ScentType = Neutral
  - **Sensory Acuity**:
    - Vision: 1.0 (normal)
    - Hearing: 1.3 (sharp ears)
    - Smell: 1.4 (excellent nose, especially for food)
    - Scent Bias: Food scents 2.0x (detect cooking from far away)
  - **Physical Stats**:
    - Height: 1.0m (very short)
    - Mass: 35kg (light)
    - BaseHP: 70 (frail)
    - BaseMovementSpeed: 4.8 m/s (normal for size)
    - CarryCapacity: 25kg (-60%, weak)
    - StealthProfile: 0.6 (small, harder to detect)
    - Special: +25% dodge (nimble)
- **CULTURAL TRAITS (Halfling Pastoral Culture - Learned)**:
  - **Occupation Bias**: Farmer 0.7, Innkeeper 0.6, Cook 0.8, Warrior 0.1
  - **Food Preferences**: Comfort Food +0.8, Sweets +0.7
  - **Aesthetic Preferences**: Cozy/Homely +0.9, Brutal -0.8
  - **Social Behaviors**: Friendly (+0.4 reputation), Unassuming (low threat)
  - **Cultural Aptitudes**: Cooking +0.4, Farming +0.3, Stealth +0.2, Combat -0.3

**Gnomes**:
- **PHYSICAL TRAITS (Racial - Immutable)**:
  - VisualSignature: 0.7 (tiny, small frame)
  - AuditorySignature: 1.0 (normal volume, chatty)
  - OlfactorySignature: 0.9 (clean), ScentType = Neutral
  - **Sensory Acuity**:
    - Vision: 1.0 (normal)
    - Hearing: 1.2 (attentive)
    - Smell: 1.0 (normal)
  - **Physical Stats**:
    - Height: 1.1m
    - Mass: 40kg
    - BaseHP: 80 (weak)
    - BaseMovementSpeed: 4.5 m/s
    - CarryCapacity: 30kg (-50%)
    - StealthProfile: 0.7 (small, harder to hit)
  - **Mental**: Gullible baseline (biological trait, poor deception detection)
- **CULTURAL TRAITS (Gnome Tinkerer Culture - Learned)**:
  - **Occupation Bias**: Inventor 0.8, Merchant 0.6, Tinkerer 0.9, Warrior 0.1
  - **Food Preferences**: Sweets +0.8, Complex Dishes +0.5
  - **Aesthetic Preferences**: Gadgets/Contraptions +0.9, Natural -0.2
  - **Social Behaviors**: Likeable (+0.3 reputation), Curious (+0.2 exploration)
  - **Cultural Aptitudes**: Tinkering +0.5, Trading +0.3, Melee -0.3, Intimidation -0.4

**Goblins**:
- **PHYSICAL TRAITS (Racial - Immutable)**:
  - VisualSignature: 0.8 (small, wiry)
  - AuditorySignature: 1.3 (squeaky, high-pitched)
  - OlfactorySignature: 1.6 (strong body odor), ScentType = Pungent
  - **Sensory Acuity**:
    - Vision: 1.2 (darkvision, good night sight)
    - Hearing: 1.1 (alert)
    - Smell: 0.6 (poor nose, nose-blind to filth)
    - Scent Bias: Pungent scents 0.3x (can't smell themselves), Putrid scents 0.5x
  - **Physical Stats**:
    - Height: 1.2m
    - Mass: 45kg
    - BaseHP: 75 (-25% HP, weak)
    - BaseMovementSpeed: 5.5 m/s (+10%, agile)
    - CarryCapacity: 30kg
    - StealthProfile: 0.7 (small, sneaky build)
- **CULTURAL TRAITS (Goblin Scavenger Culture - Typical)**:
  - **Occupation Bias**: Thief 0.7, Scout 0.6, Scavenger 0.8, Warrior 0.3
  - **Food Preferences**: Anything Edible +0.5 (scavenger diet)
  - **Aesthetic Preferences**: Shiny Things +0.8, Elegant -0.4
  - **Social Behaviors**: Distrusted (-0.5 reputation), Cunning (+0.2 deception)
  - **Cultural Aptitudes**: Stealth +0.4, Traps +0.3, Combat -0.2, Leadership -0.4

**Undead**:
- **PHYSICAL TRAITS (Racial - Immutable)**:
  - VisualSignature: 1.0 (decayed appearance)
  - AuditorySignature: 0.3 (silent shuffle, no breathing)
  - OlfactorySignature: 1.8 (putrid decay), ScentType = Putrid
  - **Sensory Acuity**:
    - Vision: 0.7 (decayed eyes)
    - Hearing: 0.5 (rotten ears)
    - Smell: 0.0 (no sense of smell, don't breathe)
  - **Physical Stats**:
    - Height: Varies (depends on pre-death race)
    - Mass: 80% of living mass (desiccated)
    - BaseHP: 90
    - BaseMovementSpeed: 3.0 m/s (-40% movement, stiff joints)
    - Special: Immune to morale/fatigue, Decaying (lose 1 HP/100 ticks unless maintained)
- **CULTURAL TRAITS (Undead Necromantic Culture - Learned)**:
  - **Occupation Bias**: Necromancer 0.9, Warrior 0.4, Social Roles 0.0
  - **Food Preferences**: None (don't eat)
  - **Aesthetic Preferences**: Darkness +0.8, Life -0.9
  - **Social Behaviors**: Reviled (-1.0 reputation with living), Feared (+0.8 threat)
  - **Cultural Aptitudes**: Necromancy +1.0, Fatigue Immunity, Social -1.0, Farming -0.8

---

### Battlefield Memory & Cultural Legends

**Cultures remember battlefield outcomes, develop tactical knowledge, create legends from improbable victories, and build reputations that drive future behavior.**

**Core Principle**: Historical battlefield events shape cultural memory. Crushing defeats teach tactical lessons, improbable victories create legends and heroes, and consistent combat performance builds reputations that pure/patriotic individuals enforce.

```csharp
// Component tracking battlefield memories for a culture
public struct CulturalBattlefieldMemory : IComponentData
{
    public CultureType Culture;
    public DynamicBuffer<BattlefieldLesson> TacticalLessons;
    public DynamicBuffer<LegendaryBattle> Legends;
    public DynamicBuffer<CulturalReputation> Reputations;
}

public struct BattlefieldLesson : IBufferElementData
{
    public CultureType OpponentCulture;
    public RaceType OpponentRace;
    public TacticalLesson Lesson;                // ForestAmbushDanger, CloseCombatDanger, ArtillerySuperiority, etc.
    public float LessonStrength;                 // 0.0-1.0 (how deeply learned)
    public ushort OriginTick;
    public ushort ReinforcementCount;            // Times lesson validated by subsequent battles
    public ushort DecayRate;                     // How fast lesson fades if not reinforced
    public DynamicBuffer<LessonSource> SourceBattles;
}

public enum TacticalLesson : byte
{
    ForestAmbushDanger,         // Elves ambushed us in forest → avoid forest combat with elves
    CloseCombatDanger,          // Dwarves crushed us in melee → avoid close combat with dwarves
    ArtillerySuperiority,       // Enemy artillery devastated us → respect their ranged power
    CavalryChargeThreat,        // Cavalry broke our lines → prepare anti-cavalry tactics
    NavalManeuverAdvantage,     // Enemy outmaneuvered our fleet → they have superior naval tactics
    MagicalOverwhelm,           // Enemy mages destroyed us → fear their magical power
    SiegeExpertise,             // Enemy breached our walls quickly → they are siege masters
    GuerrillaWarfare,           // Enemy harassed us with hit-and-run → expect guerrilla tactics
}

public struct LessonSource : IBufferElementData
{
    public ushort BattleTick;
    public Entity BattleAggregate;               // Which aggregate fought this battle
    public BattleOutcome Outcome;                // Defeat, CrushingDefeat, NarrowDefeat
    public ushort CasualtiesInflicted;
    public ushort CasualtiesSuffered;
}

public struct LegendaryBattle : IBufferElementData
{
    public ushort BattleTick;
    public BattleOutcome Outcome;                // ImprobableVictory, ImprobableDefeat
    public Entity VictoriousAggregate;
    public Entity DefeatedAggregate;
    public float VictoryImprobability;           // 0.0-1.0 (how impossible the victory was)
    public LegendStatus Status;                  // LocalLegend, CulturalLegend, EternalLegend
    public ushort LegendStrength;                // 0-1000 (how widely known/revered)
    public DynamicBuffer<LegendaryHero> Heroes;
    public FixedString128Bytes LegendName;       // "The Bridge at Khazad-Dûm", "The Massacre of Helm's Deep"
    public ushort DecayRate;                     // Legends fade over time unless reinforced
}

public enum BattleOutcome : byte
{
    Victory,                    // Normal victory
    Defeat,                     // Normal defeat
    ImprobableVictory,          // Won against overwhelming odds (10 vs 50, victory)
    ImprobableDefeat,           // Lost despite overwhelming advantage (50 vs 10, defeat - shameful)
    PyrrhicVictory,             // Won but at terrible cost (90% casualties)
    CrushingVictory,            // Overwhelming victory (90%+ enemy casualties, <10% friendly)
    CrushingDefeat,             // Overwhelming defeat (90%+ friendly casualties)
    Stalemate,                  // No clear winner
}

public enum LegendStatus : byte
{
    LocalLegend,                // Known in village/region (1-2 villages)
    CulturalLegend,             // Known across culture (all villages of same culture)
    EternalLegend,              // Known across all cultures (world-famous, never forgotten)
}

public struct LegendaryHero : IBufferElementData
{
    public Entity HeroEntity;
    public FixedString64Bytes HeroName;
    public HeroicDeed Deed;                      // HeldTheLine, SlayedGiant, LeadTheCharge, etc.
    public ushort Fame;                          // 0-1000 (renown level)
}

public enum HeroicDeed : byte
{
    HeldTheLine,                // Held position against overwhelming odds
    SlayedGiant,                // Killed powerful enemy entity
    LeadTheCharge,              // Led successful charge that turned the battle
    SacrificePlay,              // Sacrificed self to save comrades
    TacticalGenius,             // Outmaneuvered superior force
    LastStand,                  // Fought to the death, allowing others to escape
}

public struct CulturalReputation : IBufferElementData
{
    public ReputationType Type;                  // CloseCombatMasters, ArtilleryExperts, NavalTactics, etc.
    public float ReputationStrength;             // 0.0-1.0 (how widely recognized)
    public ushort OriginTick;
    public ushort ReinforcementCount;            // Times reputation validated
    public DynamicBuffer<ReputationSource> Sources; // Battles that built this reputation
    public ushort DecayRate;                     // Reputation fades without reinforcement
}

public enum ReputationType : byte
{
    CloseCombatMasters,         // Dwarves, Orcs - feared in melee
    ArtilleryExperts,           // Dwarves - known for devastating ranged fire
    ForestAmbushers,            // Elves - masters of woodland warfare
    CavalryCharge,              // Humans - devastating cavalry charges
    NavalTactics,               // Elves, Humans - superior fleet maneuvers
    SiegeMasters,               // Dwarves - expert at breaching fortifications
    MagicalProwess,             // Elves - devastating magical attacks
    GuerrillaWarfare,           // Goblins - hit-and-run specialists
    DefensiveFortifications,    // Dwarves - impenetrable defenses
    BerserkerRage,              // Orcs, Trolls - terrifying berserk charges
}

public struct ReputationSource : IBufferElementData
{
    public ushort BattleTick;
    public Entity BattleAggregate;
    public ushort CasualtiesInflicted;           // How many enemies killed with this tactic
    public BattleOutcome Outcome;
}
```

**Pride-Driven Behavioral Enforcement**:

```csharp
// Component tracking cultural pride and behavioral standards
public struct CulturalPrideBehavior : IComponentData
{
    public CultureType Culture;
    public float PrideLevel;                     // 0.0-1.0 (how much culture cares about reputation)
    public DynamicBuffer<PrideEnforcedStandard> Standards;
}

public struct PrideEnforcedStandard : IBufferElementData
{
    public ReputationType Reputation;            // Which reputation drives this standard
    public StandardType Standard;                // WeaponQuality, CombatBehavior, CraftQuality, etc.
    public float MinimumQuality;                 // 0.0-1.0 (minimum acceptable quality)
    public PurityRequirement PurityThreshold;    // How pure individual must be to enforce standard
}

public enum StandardType : byte
{
    WeaponQuality,              // Dwarf slayer refuses poor quality weapons
    CraftQuality,               // Dwarf smith refuses to sell poor quality goods
    CombatBehavior,             // Orc warrior refuses to retreat (maintains "fearless" reputation)
    TacticalApproach,           // Elf ranger refuses frontal assault (maintains "cunning" reputation)
    HonorCode,                  // Human knight refuses dishonorable tactics (maintains "chivalrous" reputation)
}

public enum PurityRequirement : byte
{
    AnyPurity,                  // All individuals enforce this (cultural baseline)
    ModeratePurity,             // Purity > 0.4 required
    HighPurity,                 // Purity > 0.7 required (only zealots enforce)
}
```

**Pride-Driven Behavior Examples**:

```csharp
// System enforcing pride-driven standards
public struct CulturalPrideEnforcementSystem : ISystem
{
    public bool WillAcceptWeapon(Entity entity, Entity weapon, float weaponQuality)
    {
        var culture = GetComponent<CulturalTraits>(entity);
        var purity = GetComponent<PurityCorruptionBalance>(entity);
        var culturalMemory = GetComponent<CulturalBattlefieldMemory>(culture.Culture);

        // Check if culture has "CloseCombatMasters" reputation
        foreach (var reputation in culturalMemory.Reputations)
        {
            if (reputation.Type == ReputationType.CloseCombatMasters && reputation.ReputationStrength > 0.6f)
            {
                // Dwarves known for close combat mastery
                foreach (var standard in GetPrideStandards(culture.Culture))
                {
                    if (standard.Standard == StandardType.WeaponQuality)
                    {
                        // Pure dwarves refuse poor quality weapons
                        if (purity.PurityScore > 0.7f && weaponQuality < standard.MinimumQuality)
                        {
                            return false; // "I will not sully myself with this inferior blade!"
                        }
                    }
                }
            }
        }

        return true; // Weapon acceptable
    }

    public bool WillSellCraftedGoods(Entity entity, Entity goods, float goodsQuality)
    {
        var culture = GetComponent<CulturalTraits>(entity);
        var purity = GetComponent<PurityCorruptionBalance>(entity);
        var culturalMemory = GetComponent<CulturalBattlefieldMemory>(culture.Culture);

        // Check if culture has "ArtilleryExperts" reputation
        foreach (var reputation in culturalMemory.Reputations)
        {
            if (reputation.Type == ReputationType.ArtilleryExperts && reputation.ReputationStrength > 0.7f)
            {
                // Dwarves known for superior artillery
                foreach (var standard in GetPrideStandards(culture.Culture))
                {
                    if (standard.Standard == StandardType.CraftQuality && standard.Reputation == ReputationType.ArtilleryExperts)
                    {
                        // Pure dwarf engineer refuses to sell poor quality artillery
                        if (purity.PurityScore > 0.6f && goodsQuality < standard.MinimumQuality)
                        {
                            return false; // "I will not tarnish the name of dwarven craftsmanship!"
                        }
                    }
                }
            }
        }

        return true; // Goods acceptable to sell
    }
}
```

**Grievance Ideology Overrides**:

**Core Principle**: Extreme wrongs can override core ideological beliefs. A pure pacifist may become a crusader if their family is killed. A pure xenophilic may become xenophobic if betrayed by xenos.

```csharp
// Component tracking grievance-based ideology overrides
public struct GrievanceIdeologyOverride : IComponentData
{
    public Entity GrievanceSource;               // Entity that wronged this individual
    public GrudgeType GrievanceType;             // Murder, Enslavement, Betrayal, Conquest
    public float GrievanceSeverity;              // 0.0-1.0 (how severe the wrong)
    public IdeologyOverride Override;            // PacifistTurnsWarlike, XenophilicTurnsXenophobic, etc.
    public float OverrideStrength;               // 0.0-1.0 (how much ideology is overridden)
    public ushort OverrideDuration;              // How long override lasts (may be permanent)
    public bool IsPermanent;                     // If true, override never decays
}

public enum IdeologyOverride : byte
{
    PacifistTurnsWarlike,       // Pure pacifist becomes crusader (family killed)
    XenophilicTurnsXenophobic,  // Pure xenophilic becomes xenophobic (betrayed by xenos)
    ForgivingTurnsVengeful,     // Pure forgiving becomes vengeful (village razed)
    LawfulTurnsChaotic,         // Pure lawful becomes chaotic (law failed them)
    GoodTurnsEvil,              // Pure good becomes evil (broken by trauma)
}
```

**Grievance Override Examples**:

```csharp
// System calculating grievance overrides
public struct GrievanceIdeologyOverrideSystem : ISystem
{
    public void OnGrievanceEvent(Entity entity, Entity grievanceSource, GrudgeType grudgeType, float severity)
    {
        var alignment = GetComponentRW<VillagerAlignment>(entity);
        var purity = GetComponent<PurityCorruptionBalance>(entity);

        // Pure pacifist experiences family murder
        if (alignment.ValueRO.MoralAxis > 0.5f && purity.PurityScore > 0.7f) // Pure Good Pacifist
        {
            if (grudgeType == GrudgeType.Murder && severity > 0.8f) // Family killed
            {
                // Create grievance override
                var grievanceOverride = AddComponent<GrievanceIdeologyOverride>(entity);
                grievanceOverride.GrievanceSource = grievanceSource;
                grievanceOverride.GrievanceType = grudgeType;
                grievanceOverride.GrievanceSeverity = severity;
                grievanceOverride.Override = IdeologyOverride.PacifistTurnsWarlike;
                grievanceOverride.OverrideStrength = severity; // 0.8 override strength
                grievanceOverride.IsPermanent = true; // Never forgives

                // Temporarily shift alignment toward Warlike (Evil)
                alignment.ValueRW.MoralAxis -= 0.6f; // Shift from +0.5 (Good) to -0.1 (Evil)
                // Individual becomes crusader, seeks vengeance against orcs
            }
        }

        // Pure xenophilic experiences betrayal by trusted xenos
        if (alignment.ValueRO.PurityAxis > 0.5f && purity.PurityScore > 0.6f) // Pure Xenophilic
        {
            if (grudgeType == GrudgeType.Betrayal && severity > 0.7f)
            {
                var grievanceOverride = AddComponent<GrievanceIdeologyOverride>(entity);
                grievanceOverride.Override = IdeologyOverride.XenophilicTurnsXenophobic;
                grievanceOverride.OverrideStrength = severity;
                grievanceOverride.IsPermanent = false;
                grievanceOverride.OverrideDuration = 5000; // Lasts 5000 ticks, then decays

                // Temporarily shift alignment toward Xenophobic
                alignment.ValueRW.PurityAxis -= 0.5f; // Shift from +0.5 (Xenophilic) to 0.0 (Neutral)
                // Individual becomes distrustful of xenos for duration
            }
        }
    }
}
```

**Tactical Lesson Learning & Propagation**:

**Core Principle**: Surviving members of defeated aggregates carry tactical lessons back to their culture. Future aggregates learn from past mistakes.

```csharp
// System recording battlefield lessons from defeats
public struct BattlefieldLessonRecordingSystem : ISystem
{
    public void OnBattleEnd(Entity aggregate, BattleOutcome outcome, Entity opponentAggregate, TacticalContext context)
    {
        // Only record lessons from defeats or costly victories
        if (outcome == BattleOutcome.Defeat || outcome == BattleOutcome.CrushingDefeat || outcome == BattleOutcome.PyrrhicVictory)
        {
            var aggregateData = GetComponent<AggregateData>(aggregate);
            var opponentData = GetComponent<AggregateData>(opponentAggregate);
            var culturalMemory = GetComponentRW<CulturalBattlefieldMemory>(aggregateData.DominantCulture);

            // Determine tactical lesson from context
            TacticalLesson lesson = DetermineLesson(context, opponentData);

            // Check if lesson already exists
            bool lessonExists = false;
            foreach (var existingLesson in culturalMemory.ValueRO.TacticalLessons)
            {
                if (existingLesson.Lesson == lesson && existingLesson.OpponentCulture == opponentData.DominantCulture)
                {
                    // Reinforce existing lesson
                    existingLesson.LessonStrength = math.min(1.0f, existingLesson.LessonStrength + 0.2f);
                    existingLesson.ReinforcementCount++;
                    lessonExists = true;
                    break;
                }
            }

            if (!lessonExists)
            {
                // Create new lesson
                var newLesson = new BattlefieldLesson
                {
                    OpponentCulture = opponentData.DominantCulture,
                    OpponentRace = opponentData.DominantRace,
                    Lesson = lesson,
                    LessonStrength = 0.5f, // Initial strength
                    OriginTick = currentTick,
                    ReinforcementCount = 1,
                    DecayRate = 100, // Decays slowly over time
                };
                culturalMemory.ValueRW.TacticalLessons.Add(newLesson);
            }
        }
    }

    private TacticalLesson DetermineLesson(TacticalContext context, AggregateData opponent)
    {
        // Ambushed in forest by elves
        if (context.Terrain == TerrainType.Forest && context.AmbushOccurred && opponent.DominantRace == RaceType.Elf)
            return TacticalLesson.ForestAmbushDanger;

        // Crushed in melee by dwarves
        if (context.EngagementRange == EngagementRange.Melee && context.CasualtiesRatio > 3.0f && opponent.DominantRace == RaceType.Dwarf)
            return TacticalLesson.CloseCombatDanger;

        // Devastated by artillery
        if (context.CasualtySource == CasualtySource.Artillery && context.CasualtiesRatio > 2.0f)
            return TacticalLesson.ArtillerySuperiority;

        // Add more contextual lesson determination...
        return TacticalLesson.None;
    }
}

// System applying learned lessons to future aggregate behavior
public struct TacticalLessonApplicationSystem : ISystem
{
    public TacticalModifier GetTacticalModifier(Entity aggregate, Entity opponentAggregate, TacticalContext context)
    {
        var aggregateData = GetComponent<AggregateData>(aggregate);
        var opponentData = GetComponent<AggregateData>(opponentAggregate);
        var culturalMemory = GetComponent<CulturalBattlefieldMemory>(aggregateData.DominantCulture);

        var modifier = new TacticalModifier { CautionLevel = 0.0f, AvoidanceDesire = 0.0f };

        // Check for relevant lessons
        foreach (var lesson in culturalMemory.TacticalLessons)
        {
            if (lesson.OpponentCulture == opponentData.DominantCulture || lesson.OpponentRace == opponentData.DominantRace)
            {
                switch (lesson.Lesson)
                {
                    case TacticalLesson.ForestAmbushDanger:
                        if (context.Terrain == TerrainType.Forest)
                        {
                            modifier.CautionLevel += lesson.LessonStrength * 0.5f; // Increased caution
                            modifier.AvoidanceDesire += lesson.LessonStrength * 0.8f; // Strong desire to avoid forest combat
                        }
                        break;

                    case TacticalLesson.CloseCombatDanger:
                        if (context.EngagementRange == EngagementRange.Melee)
                        {
                            modifier.CautionLevel += lesson.LessonStrength * 0.6f;
                            modifier.AvoidanceDesire += lesson.LessonStrength * 0.7f; // Avoid melee with dwarves
                        }
                        break;

                    case TacticalLesson.ArtillerySuperiority:
                        modifier.CautionLevel += lesson.LessonStrength * 0.4f;
                        modifier.RangedRespect += lesson.LessonStrength * 0.9f; // Fear their artillery
                        break;
                }
            }
        }

        return modifier;
    }
}
```

**Legendary Battle Creation**:

**Core Principle**: Improbable victories create legends. 10 dwarves holding a bridge against 50 orcs becomes "The Stand at Khazad-Dûm". Heroes are born, cultures remember, and pride swells.

```csharp
// System detecting and creating legendary battles
public struct LegendaryBattleCreationSystem : ISystem
{
    public void OnBattleEnd(Entity victorAggregate, Entity defeatedAggregate, BattleOutcome outcome, BattleStatistics stats)
    {
        // Calculate victory improbability
        float improbability = CalculateImprobability(victorAggregate, defeatedAggregate, stats);

        // Legendary battle threshold: improbability > 0.7
        if (improbability > 0.7f)
        {
            var victorData = GetComponent<AggregateData>(victorAggregate);
            var defeatedData = GetComponent<AggregateData>(defeatedAggregate);
            var culturalMemory = GetComponentRW<CulturalBattlefieldMemory>(victorData.DominantCulture);

            // Create legendary battle
            var legend = new LegendaryBattle
            {
                BattleTick = currentTick,
                Outcome = BattleOutcome.ImprobableVictory,
                VictoriousAggregate = victorAggregate,
                DefeatedAggregate = defeatedAggregate,
                VictoryImprobability = improbability,
                Status = DetermineLegendStatus(improbability),
                LegendStrength = (ushort)(improbability * 1000),
                LegendName = GenerateLegendName(stats.BattleLocation, victorData.DominantRace),
                DecayRate = improbability > 0.9f ? 0 : 50, // Eternal legends never decay
            };

            // Identify heroes
            foreach (var member in GetAggregateMembers(victorAggregate))
            {
                if (PerformedHeroicDeed(member, stats))
                {
                    var hero = new LegendaryHero
                    {
                        HeroEntity = member,
                        HeroName = GetEntityName(member),
                        Deed = IdentifyHeroicDeed(member, stats),
                        Fame = (ushort)(improbability * 800),
                    };
                    legend.Heroes.Add(hero);
                }
            }

            culturalMemory.ValueRW.Legends.Add(legend);

            // Defeated culture also records this (as shameful defeat)
            var defeatedCulturalMemory = GetComponentRW<CulturalBattlefieldMemory>(defeatedData.DominantCulture);
            var shamefulDefeat = legend;
            shamefulDefeat.Outcome = BattleOutcome.ImprobableDefeat;
            defeatedCulturalMemory.ValueRW.Legends.Add(shamefulDefeat);
        }
    }

    private float CalculateImprobability(Entity victor, Entity defeated, BattleStatistics stats)
    {
        var victorData = GetComponent<AggregateData>(victor);
        var defeatedData = GetComponent<AggregateData>(defeated);

        // Factor 1: Numbers disadvantage (10 vs 50 = 5x disadvantage)
        float numberRatio = (float)defeatedData.MemberCount / victorData.MemberCount;
        float numberImprobability = math.min(1.0f, (numberRatio - 1.0f) / 10.0f); // 5x disadvantage = 0.4 improbability

        // Factor 2: Casualty ratio (victor lost 20%, defeated lost 90%)
        float casualtyRatio = stats.DefeatedCasualties / math.max(1, stats.VictorCasualties);
        float casualtyImprobability = math.min(1.0f, casualtyRatio / 10.0f);

        // Factor 3: Combat power difference (victor 500 combat power, defeated 2000)
        float powerRatio = defeatedData.CombatPower / math.max(1, victorData.CombatPower);
        float powerImprobability = math.min(1.0f, (powerRatio - 1.0f) / 5.0f);

        // Combined improbability
        return (numberImprobability + casualtyImprobability + powerImprobability) / 3.0f;
    }

    private LegendStatus DetermineLegendStatus(float improbability)
    {
        if (improbability > 0.9f) return LegendStatus.EternalLegend; // World-famous
        if (improbability > 0.8f) return LegendStatus.CulturalLegend; // Culture-wide
        return LegendStatus.LocalLegend; // Regional
    }

    private FixedString128Bytes GenerateLegendName(float3 location, RaceType race)
    {
        // Example: "The Stand at Khazad-Dûm", "The Massacre of Helm's Deep"
        // This would use procedural generation or templates
        return new FixedString128Bytes($"The {race} Stand at {location}");
    }
}
```

**Battlefield Memory Examples**:

1. **Orc Army Defeated by Dwarf Band (Tactical Lesson)**:
   - 50 orcs attack 10 dwarves at bridge
   - Dwarves hold bridge, inflict 40 orc casualties, lose 2 dwarves
   - **Orc Culture Records**: `TacticalLesson.CloseCombatDanger` against dwarves (LessonStrength = 0.8)
   - **Future Behavior**: Orc aggregates avoid melee combat with dwarves, prefer ranged harassment
   - **Dwarf Culture Records**: `LegendaryBattle.ImprobableVictory` (VictoryImprobability = 0.85)
   - **Legendary Heroes**: 2 dwarves achieve `HeroicDeed.HeldTheLine` status, gain Fame = 700

2. **Elven Forest Ambush (Tactical Lesson)**:
   - 30 human soldiers march through forest
   - 15 elves ambush from trees, kill 20 humans before they can react
   - **Human Culture Records**: `TacticalLesson.ForestAmbushDanger` against elves (LessonStrength = 0.7)
   - **Future Behavior**: Human aggregates avoid forest combat with elves, send scouts ahead
   - **Elf Culture Records**: `CulturalReputation.ForestAmbushers` (ReputationStrength = 0.6, +1 reinforcement)

3. **Dwarven Artillery Devastation (Reputation Building)**:
   - Dwarf artillery crew destroys 5 orc formations over 10 battles
   - **Orc Culture Records**: `TacticalLesson.ArtillerySuperiority` against dwarves (LessonStrength = 0.9)
   - **Dwarf Culture Records**: `CulturalReputation.ArtilleryExperts` (ReputationStrength = 0.8, ReinforcementCount = 5)
   - **Pride Behavior**: Pure dwarf engineers refuse to sell poor quality artillery (MinimumQuality = 0.7)

4. **Pure Pacifist Turns Crusader (Grievance Override)**:
   - Elf monk (MoralAxis = +0.6 Good Pacifist, PurityScore = 0.8) lives peacefully
   - Orc raiders kill monk's family (GrudgeType.Murder, Severity = 0.9)
   - **Grievance Override**: `IdeologyOverride.PacifistTurnsWarlike` (OverrideStrength = 0.9, IsPermanent = true)
   - **Alignment Shift**: MoralAxis shifts from +0.6 to -0.3 (Good → Evil)
   - **Behavior Change**: Monk abandons pacifism, leads crusade against orcs, seeks vengeance

5. **Dwarf Slayer Weapon Obsession (Pride Enforcement)**:
   - Dwarf slayer (PurityScore = 0.9) offered common quality axe (Quality = 0.4)
   - **Cultural Pride**: Dwarves have `CulturalReputation.CloseCombatMasters` (ReputationStrength = 0.7)
   - **Pride Standard**: `StandardType.WeaponQuality` (MinimumQuality = 0.6, PurityRequirement.HighPurity)
   - **Behavior**: Slayer refuses axe, seeks masterwork weapon (Quality ≥ 0.6)
   - **Corruption Contrast**: Corrupt dwarf (CorruptionScore = 0.8) accepts poor quality axe for personal gain

---

### Knowledge Propagation & Trade Secrets

**Core Principle**: Knowledge spreads only when individuals choose to share it. Secrets can be hoarded, traded via contracts, or stolen through espionage. Propagation takes time and depends on social networks, willingness to teach, and mutual agreements.

**CRITICAL**: Battlefield lessons, tactical knowledge, crafting techniques, and starmap data do NOT automatically spread. Knowledge propagates through:
1. **Teaching** - Individual decides to share with apprentices/students
2. **Gossip/Stories** - Legends and stories spread through social networks
3. **Trade Agreements** - Contractual knowledge exchange between entities/companies
4. **Espionage** - Thieves, spies, and operatives steal secrets

```csharp
// Component tracking individual's knowledge and willingness to share
public struct IndividualKnowledge : IComponentData
{
    public DynamicBuffer<KnowledgePiece> Knowledge;
    public float WillingnessToTeach;             // 0.0-1.0 (how likely to share knowledge)
    public DynamicBuffer<SecretKnowledge> Secrets; // Knowledge individual refuses to share
}

public struct KnowledgePiece : IBufferElementData
{
    public KnowledgeType Type;                   // TacticalLesson, CraftingTechnique, StarmapData, etc.
    public FixedString128Bytes KnowledgeID;      // "DragonTaming", "ForestAmbushDanger", "AdvancedCoolingModule"
    public float Proficiency;                    // 0.0-1.0 (how well individual knows this)
    public Entity SourceEntity;                  // Who taught this (or Entity.Null if learned directly)
    public ushort AcquisitionTick;
    public bool IsSecret;                        // If true, individual refuses to share
    public float TeachingDifficulty;             // 0.0-1.0 (how hard to teach this knowledge)
}

public enum KnowledgeType : byte
{
    TacticalLesson,             // Battlefield lessons (elves ambush in forests)
    CraftingTechnique,          // Smithing, armorsmithing, advanced materials
    StarmapData,                // Resource-rich system locations
    TechnologyBlueprint,        // Cooling modules, kinetic weapons
    CulturalLegend,             // Stories of legendary battles
    TradeRoute,                 // Profitable trade routes
    DragonTaming,               // Exotic skills
    MagicalFormula,             // Spell formulas, rituals
    DiplomaticIntel,            // Knowledge of faction relations
}

public struct SecretKnowledge : IBufferElementData
{
    public FixedString128Bytes KnowledgeID;
    public SecrecyReason Reason;                 // WhyKeepSecret, TradeValue, PersonalAdvantage, etc.
    public float SecrecyStrength;                // 0.0-1.0 (how strongly they guard this secret)
    public DynamicBuffer<Entity> SharedWith;     // Entities allowed to know this secret
}

public enum SecrecyReason : byte
{
    PersonalAdvantage,          // Dragon tamer hoards skill to maintain monopoly
    TradeValue,                 // Resource-rich system kept secret for exploitation
    ContractualObligation,      // Signed NDA, can't share company secrets
    CulturalTaboo,              // Knowledge forbidden to share with outsiders
    Spite,                      // Refuses to teach out of grudge/jealousy
    Safety,                     // Knowledge too dangerous to spread (forbidden magic)
}
```

**Willingness to Teach Modifiers**:

```csharp
// Factors affecting willingness to teach
public struct TeachingWillingnessCalculation
{
    public static float CalculateWillingnessToTeach(Entity teacher, Entity student, KnowledgePiece knowledge)
    {
        var teacherAlignment = GetComponent<VillagerAlignment>(teacher);
        var teacherPurity = GetComponent<PurityCorruptionBalance>(teacher);
        var teacherCulture = GetComponent<CulturalTraits>(teacher);
        var studentCulture = GetComponent<CulturalTraits>(student);
        var relations = GetComponent<Relations>(teacher, student);

        float willingness = 0.5f; // Baseline

        // Purity increases willingness (collective-minded share freely)
        willingness += teacherPurity.PurityScore * 0.3f;

        // Corruption decreases willingness (self-interested hoard knowledge)
        willingness -= teacherPurity.CorruptionScore * 0.4f;

        // Good alignment increases willingness (generous)
        if (teacherAlignment.MoralAxis > 0.3f) willingness += 0.2f;

        // Relations modifier
        willingness += relations.RelationshipScore * 0.3f; // Friends more likely to teach

        // Cultural acceptance
        if (teacherCulture.Culture == studentCulture.Culture) willingness += 0.3f; // Same culture = teach freely
        else if (teacherAlignment.PurityAxis < -0.5f) willingness -= 0.4f; // Xenophobic refuses to teach xenos

        // Knowledge difficulty (harder to teach = less willing)
        willingness -= knowledge.TeachingDifficulty * 0.2f;

        // Secret knowledge
        if (knowledge.IsSecret) willingness -= 0.8f; // Strongly resist teaching secrets

        return math.clamp(willingness, 0.0f, 1.0f);
    }
}
```

**Teaching & Learning System**:

```csharp
// Component tracking active teaching relationship
public struct TeachingRelationship : IComponentData
{
    public Entity Teacher;
    public Entity Student;
    public FixedString128Bytes KnowledgeBeingTaught;
    public float TeachingProgress;               // 0.0-1.0 (how much student has learned)
    public ushort TicksSpentTeaching;
    public ushort TicksRequiredToLearn;          // Depends on difficulty and student aptitude
}

// System handling knowledge transfer through teaching
public struct KnowledgeTeachingSystem : ISystem
{
    public void InitiateTeaching(Entity teacher, Entity student, FixedString128Bytes knowledgeID)
    {
        var teacherKnowledge = GetComponent<IndividualKnowledge>(teacher);
        KnowledgePiece knowledge = FindKnowledge(teacherKnowledge, knowledgeID);

        // Check if teacher is willing to teach
        float willingness = CalculateWillingnessToTeach(teacher, student, knowledge);
        if (willingness < 0.3f)
        {
            // Teacher refuses to teach
            return;
        }

        // Create teaching relationship
        var teachingRelation = new TeachingRelationship
        {
            Teacher = teacher,
            Student = student,
            KnowledgeBeingTaught = knowledgeID,
            TeachingProgress = 0.0f,
            TicksSpentTeaching = 0,
            TicksRequiredToLearn = CalculateTeachingDuration(knowledge, student),
        };

        AddComponent<TeachingRelationship>(student, teachingRelation);
    }

    public void UpdateTeaching(Entity student)
    {
        var teachingRelation = GetComponentRW<TeachingRelationship>(student);
        teachingRelation.ValueRW.TicksSpentTeaching++;

        // Progress teaching
        float progressRate = 1.0f / teachingRelation.ValueRO.TicksRequiredToLearn;
        teachingRelation.ValueRW.TeachingProgress += progressRate;

        // Check if learning complete
        if (teachingRelation.ValueRW.TeachingProgress >= 1.0f)
        {
            // Transfer knowledge to student
            var teacherKnowledge = GetComponent<IndividualKnowledge>(teachingRelation.ValueRO.Teacher);
            KnowledgePiece knowledge = FindKnowledge(teacherKnowledge, teachingRelation.ValueRO.KnowledgeBeingTaught);

            var studentKnowledge = GetComponentRW<IndividualKnowledge>(student);
            var learnedKnowledge = knowledge;
            learnedKnowledge.Proficiency = 0.5f; // Student starts at 50% proficiency
            learnedKnowledge.SourceEntity = teachingRelation.ValueRO.Teacher; // Track who taught this
            studentKnowledge.ValueRW.Knowledge.Add(learnedKnowledge);

            // Remove teaching relationship
            RemoveComponent<TeachingRelationship>(student);
        }
    }

    private ushort CalculateTeachingDuration(KnowledgePiece knowledge, Entity student)
    {
        float baseDuration = 500; // 500 ticks baseline
        baseDuration *= (1.0f + knowledge.TeachingDifficulty); // Harder knowledge takes longer

        // Student aptitude reduces duration
        var studentCulture = GetComponent<CulturalTraits>(student);
        // (Check cultural aptitudes for relevant skill)

        return (ushort)baseDuration;
    }
}
```

**Knowledge Trade Agreements (Space4X & Godgame)**:

```csharp
// Component tracking contractual knowledge exchange
public struct KnowledgeTradeContract : IComponentData
{
    public Entity Party1;                        // Company/Individual A
    public Entity Party2;                        // Company/Individual B
    public FixedString128Bytes Party1Knowledge;  // What Party1 shares (e.g., "CoolingModuleTech")
    public FixedString128Bytes Party2Knowledge;  // What Party2 shares (e.g., "KineticWeaponryTech")
    public ContractTerms Terms;
    public ushort ContractDuration;              // How long contract lasts
    public ushort ContractStartTick;
    public bool IsActive;
}

public struct ContractTerms : IComponentData
{
    public bool ExclusiveAccess;                 // If true, knowledge can't be shared with third parties
    public bool JointResearchRights;             // If true, both can research using combined knowledge
    public bool JointProductionRights;           // If true, both can produce combined products
    public float RevenueSharePercentage;         // Revenue share from joint products (0.0-1.0)
    public DynamicBuffer<Entity> ThirdPartyExclusions; // Entities explicitly forbidden from receiving knowledge
}

// System handling knowledge trade contracts
public struct KnowledgeTradeSystem : ISystem
{
    public void CreateTradeContract(Entity company1, Entity company2,
                                    FixedString128Bytes knowledge1, FixedString128Bytes knowledge2,
                                    ContractTerms terms, ushort duration)
    {
        // Verify both parties possess the knowledge they're trading
        if (!HasKnowledge(company1, knowledge1) || !HasKnowledge(company2, knowledge2))
            return;

        var contract = new KnowledgeTradeContract
        {
            Party1 = company1,
            Party2 = company2,
            Party1Knowledge = knowledge1,
            Party2Knowledge = knowledge2,
            Terms = terms,
            ContractDuration = duration,
            ContractStartTick = currentTick,
            IsActive = true,
        };

        // Transfer knowledge to both parties
        TransferKnowledge(company1, company2, knowledge1, isContractual: true);
        TransferKnowledge(company2, company1, knowledge2, isContractual: true);

        AddComponent<KnowledgeTradeContract>(company1, contract);
    }

    private void TransferKnowledge(Entity from, Entity to, FixedString128Bytes knowledgeID, bool isContractual)
    {
        var fromKnowledge = GetComponent<IndividualKnowledge>(from);
        KnowledgePiece knowledge = FindKnowledge(fromKnowledge, knowledgeID);

        var toKnowledge = GetComponentRW<IndividualKnowledge>(to);
        var transferredKnowledge = knowledge;
        transferredKnowledge.SourceEntity = from;
        transferredKnowledge.Proficiency = isContractual ? knowledge.Proficiency : knowledge.Proficiency * 0.7f; // Contracts transfer full knowledge

        toKnowledge.ValueRW.Knowledge.Add(transferredKnowledge);
    }
}
```

**Espionage & Knowledge Theft**:

```csharp
// Component tracking espionage attempts
public struct EspionageAttempt : IComponentData
{
    public Entity Spy;                           // Thief, operative, spy
    public Entity Target;                        // Individual/company to steal from
    public FixedString128Bytes TargetKnowledge;  // Knowledge to steal
    public float SuccessChance;                  // 0.0-1.0 (calculated from spy skill, target security)
    public ushort TicksSpentInfiltrating;
    public ushort TicksRequiredToSteal;
    public EspionageStatus Status;
}

public enum EspionageStatus : byte
{
    Infiltrating,               // Spy is still working on theft
    Successful,                 // Knowledge stolen successfully
    Failed,                     // Theft failed, spy not caught
    Caught,                     // Spy caught by target
}

// System handling knowledge theft
public struct KnowledgeTheftSystem : ISystem
{
    public void AttemptTheft(Entity spy, Entity target, FixedString128Bytes knowledgeID)
    {
        var spySkills = GetComponent<IndividualSkills>(spy);
        var targetSecurity = GetComponent<SecurityMeasures>(target);
        var targetKnowledge = GetComponent<IndividualKnowledge>(target);

        // Verify target has the knowledge
        if (!HasKnowledge(target, knowledgeID))
            return;

        KnowledgePiece knowledge = FindKnowledge(targetKnowledge, knowledgeID);

        // Calculate success chance
        float successChance = CalculateTheftSuccessChance(spy, target, knowledge, targetSecurity);

        var espionage = new EspionageAttempt
        {
            Spy = spy,
            Target = target,
            TargetKnowledge = knowledgeID,
            SuccessChance = successChance,
            TicksSpentInfiltrating = 0,
            TicksRequiredToSteal = CalculateInfiltrationDuration(knowledge, targetSecurity),
            Status = EspionageStatus.Infiltrating,
        };

        AddComponent<EspionageAttempt>(spy, espionage);
    }

    public void UpdateTheft(Entity spy)
    {
        var espionage = GetComponentRW<EspionageAttempt>(spy);
        espionage.ValueRW.TicksSpentInfiltrating++;

        // Check if infiltration complete
        if (espionage.ValueRW.TicksSpentInfiltrating >= espionage.ValueRW.TicksRequiredToSteal)
        {
            // Roll for success
            float roll = random.NextFloat(0f, 1f);
            if (roll < espionage.ValueRW.SuccessChance)
            {
                // Theft successful
                var targetKnowledge = GetComponent<IndividualKnowledge>(espionage.ValueRO.Target);
                KnowledgePiece knowledge = FindKnowledge(targetKnowledge, espionage.ValueRO.TargetKnowledge);

                var spyKnowledge = GetComponentRW<IndividualKnowledge>(spy);
                var stolenKnowledge = knowledge;
                stolenKnowledge.Proficiency = knowledge.Proficiency * 0.6f; // Stolen knowledge is incomplete
                stolenKnowledge.SourceEntity = Entity.Null; // Source hidden
                spyKnowledge.ValueRW.Knowledge.Add(stolenKnowledge);

                espionage.ValueRW.Status = EspionageStatus.Successful;
            }
            else
            {
                // Theft failed, check if caught
                float catchChance = (1.0f - espionage.ValueRW.SuccessChance) * 0.5f;
                float catchRoll = random.NextFloat(0f, 1f);
                if (catchRoll < catchChance)
                {
                    espionage.ValueRW.Status = EspionageStatus.Caught;
                    // Spy caught, create grudge, imprison, etc.
                    OnSpyCaught(spy, espionage.ValueRO.Target);
                }
                else
                {
                    espionage.ValueRW.Status = EspionageStatus.Failed;
                }
            }

            RemoveComponent<EspionageAttempt>(spy);
        }
    }

    private float CalculateTheftSuccessChance(Entity spy, Entity target, KnowledgePiece knowledge, SecurityMeasures security)
    {
        var spySkills = GetComponent<IndividualSkills>(spy);

        float baseChance = 0.3f; // 30% baseline

        // Spy stealth/deception skill
        baseChance += spySkills.StealthSkill * 0.5f;
        baseChance += spySkills.DeceptionSkill * 0.3f;

        // Target security measures
        baseChance -= security.SecurityLevel * 0.4f;

        // Knowledge secrecy (harder to steal well-guarded secrets)
        if (knowledge.IsSecret)
            baseChance -= 0.3f;

        // Knowledge difficulty (complex knowledge harder to steal)
        baseChance -= knowledge.TeachingDifficulty * 0.2f;

        return math.clamp(baseChance, 0.05f, 0.95f); // 5%-95% success range
    }

    private ushort CalculateInfiltrationDuration(KnowledgePiece knowledge, SecurityMeasures security)
    {
        float baseDuration = 200; // 200 ticks baseline
        baseDuration *= (1.0f + security.SecurityLevel); // Higher security = longer infiltration
        baseDuration *= (1.0f + knowledge.TeachingDifficulty); // Complex knowledge takes longer
        return (ushort)baseDuration;
    }

    private void OnSpyCaught(Entity spy, Entity target)
    {
        // Create grudge
        var targetAlignment = GetComponentRW<VillagerAlignment>(target);
        // Xenophobic cultures execute spies, others imprison
        // Generate historical grudge between cultures/factions
    }
}

public struct SecurityMeasures : IComponentData
{
    public float SecurityLevel;                  // 0.0-1.0 (how well-protected knowledge is)
    public bool HasGuards;
    public bool HasMagicalWards;
    public bool HasEncryption;                   // Space4X data encryption
}
```

**Gossip & Story Propagation (Legends Spread Socially)**:

```csharp
// Component tracking social knowledge propagation (legends, stories, rumors)
public struct SocialKnowledgePropagation : IComponentData
{
    public FixedString128Bytes KnowledgeID;      // Legend name, rumor, story
    public KnowledgeType Type;                   // CulturalLegend, TacticalLesson, TradeRoute
    public float PropagationRate;                // 0.0-1.0 (how fast it spreads)
    public ushort TicksSinceCreation;
    public DynamicBuffer<Entity> KnowingEntities; // Who knows this knowledge
}

// System handling social knowledge spread
public struct SocialKnowledgeSpreadSystem : ISystem
{
    public void SpreadKnowledgeThroughGossip(Entity speaker, Entity listener, FixedString128Bytes knowledgeID)
    {
        var speakerKnowledge = GetComponent<IndividualKnowledge>(speaker);
        var listenerKnowledge = GetComponentRW<IndividualKnowledge>(listener);
        var relations = GetComponent<Relations>(speaker, listener);

        // Knowledge only spreads between friends/allies
        if (relations.RelationshipScore < 0.3f)
            return;

        KnowledgePiece knowledge = FindKnowledge(speakerKnowledge, knowledgeID);

        // Secret knowledge doesn't spread through gossip
        if (knowledge.IsSecret)
            return;

        // Legends and stories spread easily
        if (knowledge.Type == KnowledgeType.CulturalLegend)
        {
            var gossipedKnowledge = knowledge;
            gossipedKnowledge.Proficiency = knowledge.Proficiency * 0.8f; // Slightly degraded (stories exaggerate)
            gossipedKnowledge.SourceEntity = speaker;
            listenerKnowledge.ValueRW.Knowledge.Add(gossipedKnowledge);
        }
        // Tactical lessons spread among soldiers
        else if (knowledge.Type == KnowledgeType.TacticalLesson)
        {
            var speakerCulture = GetComponent<CulturalTraits>(speaker);
            var listenerCulture = GetComponent<CulturalTraits>(listener);

            // Only share tactical lessons with same culture/allies
            if (speakerCulture.Culture == listenerCulture.Culture || relations.RelationshipScore > 0.7f)
            {
                var sharedKnowledge = knowledge;
                sharedKnowledge.Proficiency = knowledge.Proficiency * 0.7f; // Word-of-mouth is imperfect
                sharedKnowledge.SourceEntity = speaker;
                listenerKnowledge.ValueRW.Knowledge.Add(sharedKnowledge);
            }
        }
    }
}
```

**Knowledge Propagation Examples**:

1. **Dragon Tamer Hoards Secrets (Dies Without Teaching)**:
   - Dragon tamer (CorruptionScore = 0.9) refuses to teach dragon taming (SecrecyReason.PersonalAdvantage)
   - WillingnessToTeach = 0.1 (refuses all apprentices)
   - Tamer dies in battle → knowledge lost forever
   - **Result**: No one in culture learns dragon taming, skill dies with tamer

2. **Resource-Rich System Kept Secret (Space4X)**:
   - Scout discovers resource-rich system (10x rare minerals)
   - Scout (CorruptionScore = 0.7) marks system as SecretKnowledge (SecrecyReason.TradeValue)
   - Scout refuses to share starmap data with faction
   - Scout exploits system for personal profit
   - **Espionage**: Rival spy (StealthSkill = 0.8) attempts theft (SuccessChance = 0.65)
   - Spy succeeds, steals starmap data (Proficiency = 0.6, incomplete coordinates)

3. **Cooling Module Trade Agreement (Space4X Companies)**:
   - CoolingTech Corp offers "AdvancedCoolingModule" blueprint
   - KineticWeapons Inc offers "RapidFireKinetics" blueprint
   - **Contract**: JointResearchRights = true, JointProductionRights = true, RevenueShare = 50%
   - Both companies gain knowledge (Proficiency = 1.0, full transfer)
   - **Result**: Prototype "Rapid-Fire Kinetic Sniper with Cooling System" produced
   - Contract includes ExclusiveAccess = true → neither can share with third parties

4. **Blacksmith Trade Secrets (Godgame)**:
   - Dwarf armorsmith knows "AdvancedArmorsmithing" (Proficiency = 0.9)
   - Human blacksmith knows "ExoticMaterialForging" (Proficiency = 0.8)
   - Both Pure (PurityScore = 0.7), WillingnessToTeach = 0.8 (willing to trade)
   - **Knowledge Trade**: Mutual teaching relationship (TicksRequired = 400 each)
   - After 400 ticks, dwarf learns ExoticMaterialForging (Proficiency = 0.5)
   - After 400 ticks, human learns AdvancedArmorsmithing (Proficiency = 0.5)

5. **Thief Steals Smithing Secrets (Godgame Espionage)**:
   - Goblin thief (StealthSkill = 0.9, DeceptionSkill = 0.7) targets dwarf smith
   - Target: "MasterworkWeaponForging" (IsSecret = true, TeachingDifficulty = 0.8)
   - Dwarf security: SecurityLevel = 0.5 (moderate guards)
   - **Success Chance**: 0.3 + (0.9 * 0.5) + (0.7 * 0.3) - (0.5 * 0.4) - 0.3 - (0.8 * 0.2) = 0.3 (30%)
   - Thief infiltrates for 300 ticks
   - **Result**: Theft fails (rolled 0.45), thief escapes without being caught
   - Dwarf retains secret, goblin learns nothing

6. **Legendary Battle Spreads Through Gossip**:
   - "The Stand at Khazad-Dûm" legendary battle created (LegendStatus.CulturalLegend)
   - 2 dwarf heroes survive battle, return to village
   - Heroes tell story to 10 villagers (RelationshipScore > 0.5)
   - Villagers gossip to friends (PropagationRate = 0.8, legends spread fast)
   - After 500 ticks, 50% of dwarven culture knows legend (Proficiency = 0.7, slightly exaggerated)
   - After 2000 ticks, 95% of dwarven culture knows legend
   - Legend never spreads to orc culture (defeated culture suppresses shameful story)

7. **Pure Elf Shares Tactical Lesson with Allies**:
   - Elf survivor learns "ForestAmbushDanger" against orcs (LessonStrength = 0.8)
   - Elf (PurityScore = 0.8, WillingnessToTeach = 0.9) shares with human ally (RelationshipScore = 0.8)
   - Human learns lesson through gossip (Proficiency = 0.6, word-of-mouth)
   - Human aggregate avoids forest combat with orcs (CautionLevel +0.3, AvoidanceDesire +0.5)

8. **Corrupt Craftsman Hoards Trade Secret for Profit**:
   - Gnome inventor discovers "ClockworkAutomaton" blueprint
   - Gnome (CorruptionScore = 0.9) marks as SecretKnowledge (SecrecyReason.PersonalAdvantage)
   - WillingnessToTeach = 0.2 (refuses to teach, maintains monopoly)
   - Gnome sells automatons at high price, competitors can't replicate
   - **Contract Offer**: Guild offers 1000 gold for blueprint
   - Gnome accepts, creates contract (ExclusiveAccess = true, guild can't resell knowledge)

---

### Finesse & Limb Status Modifiers

**Finesse and physical condition affect sensory acuity.**

```csharp
// Component tracking physical condition affecting senses
public struct LimbStatusModifiers : IComponentData
{
    public LimbStatus Eyes;
    public LimbStatus Ears;
    public LimbStatus Nose;
    public byte FinesseSkill;                    // 0-100 (affects all sensory detail)
}

public enum LimbStatus : byte
{
    Perfect,        // 100% efficiency (1.0x multiplier)
    Healthy,        // 90% efficiency (0.9x multiplier)
    Injured,        // 60% efficiency (0.6x multiplier, partially impaired)
    Crippled,       // 30% efficiency (0.3x multiplier, severely impaired)
    Destroyed,      // 0% efficiency (0.0x multiplier, completely non-functional)
    Enhanced        // 120% efficiency (1.2x multiplier, magical/tech enhancement)
}
```

**Finesse Effects**:
- **Finesse 0-30**: -40% detection range, -50% detection confidence (clumsy, unaware)
- **Finesse 31-60**: Normal detection (baseline adventurer)
- **Finesse 61-80**: +20% detection range, +30% detection confidence (trained scout)
- **Finesse 81-100**: +40% detection range, +50% detection confidence (master tracker)

**Limb Status Effects**:
- **Eyes Destroyed**: Vision sensor completely disabled (blind, must rely on hearing/smell)
- **Eyes Injured**: Vision range -40%, detection confidence -50% (blurred vision)
- **Ears Destroyed**: Hearing sensor disabled (deaf, must rely on vision/smell)
- **Ears Injured**: Hearing range -40%, cannot detect quiet sounds
- **Nose Crippled**: Smell range -70%, can only detect very strong odors
- **Enhanced Senses** (magical/cybernetic): +20% range, can detect subtler signatures

```csharp
// Sensor configuration (per agent)
public struct AISensorConfig : IComponentData
{
    public float VisionRange;                    // Max distance for Vision
    public float VisionAngle;                    // FOV in degrees (360 = omnidirectional)
    public float HearingRange;
    public float SmellRange;
    public float SensorRange;                    // Space4X tech sensors
    public SensorUpdateRate UpdateRate;
}

public enum SensorUpdateRate : byte
{
    VeryFast,       // Every 5 ticks (expensive, combat)
    Fast,           // Every 10 ticks
    Normal,         // Every 20 ticks (default)
    Slow,           // Every 60 ticks (background awareness)
    OnDemand        // Only when requested (manual scan)
}
```

### Multi-Sensory Detection Formula

**Detection combines all available senses, modified by finesse and limb status.**

```csharp
float CalculateDetectionChance(
    AISensorConfig sensorConfig,
    LimbStatusModifiers limbStatus,
    SensoryEmissionProfile targetEmission,
    float distance,
    SensorType sensorType)
{
    float baseRange = sensorType switch
    {
        SensorType.Vision => sensorConfig.VisionRange,
        SensorType.Hearing => sensorConfig.HearingRange,
        SensorType.Smell => sensorConfig.SmellRange,
        _ => 0f
    };

    // Apply limb status multiplier
    float limbMultiplier = sensorType switch
    {
        SensorType.Vision => GetLimbMultiplier(limbStatus.Eyes),
        SensorType.Hearing => GetLimbMultiplier(limbStatus.Ears),
        SensorType.Smell => GetLimbMultiplier(limbStatus.Nose),
        _ => 1.0f
    };

    // If limb destroyed, this sense is disabled
    if (limbMultiplier == 0f)
        return 0f;

    // Apply finesse skill bonus
    float finesseMultiplier = 1f + ((limbStatus.FinesseSkill - 50f) / 100f); // 0.6x to 1.5x

    // Apply target signature
    float targetSignature = sensorType switch
    {
        SensorType.Vision => targetEmission.VisualSignature,
        SensorType.Hearing => targetEmission.AuditorySignature,
        SensorType.Smell => targetEmission.OlfactorySignature,
        _ => 1.0f
    };

    // Effective detection range
    float effectiveRange = baseRange * limbMultiplier * finesseMultiplier * targetSignature;

    // Detection chance decreases with distance
    float detectionChance = (effectiveRange - distance) / effectiveRange;
    detectionChance = math.clamp(detectionChance, 0f, 1f);

    return detectionChance;
}

float GetLimbMultiplier(LimbStatus status)
{
    return status switch
    {
        LimbStatus.Perfect => 1.0f,
        LimbStatus.Healthy => 0.9f,
        LimbStatus.Injured => 0.6f,
        LimbStatus.Crippled => 0.3f,
        LimbStatus.Destroyed => 0.0f,
        LimbStatus.Enhanced => 1.2f,
        _ => 1.0f
    };
}
```

**Multi-Sensor Fusion**:
Entities can be detected via multiple senses simultaneously. **Best detection wins.**

```csharp
AISensorReading DetectEntityMultiSensory(
    Entity observer,
    Entity target,
    AISensorConfig observerSensors,
    LimbStatusModifiers observerLimbs,
    SensoryEmissionProfile targetEmission,
    float distance)
{
    float visionChance = CalculateDetectionChance(observerSensors, observerLimbs, targetEmission, distance, SensorType.Vision);
    float hearingChance = CalculateDetectionChance(observerSensors, observerLimbs, targetEmission, distance, SensorType.Hearing);
    float smellChance = CalculateDetectionChance(observerSensors, observerLimbs, targetEmission, distance, SensorType.Smell);

    // Determine which sense detected (or strongest detection)
    SensorType detectedBy = SensorType.Vision;
    float bestChance = visionChance;

    if (hearingChance > bestChance)
    {
        detectedBy = SensorType.Hearing;
        bestChance = hearingChance;
    }

    if (smellChance > bestChance)
    {
        detectedBy = SensorType.Smell;
        bestChance = smellChance;
    }

    // If no sense detected, return null detection
    if (bestChance < 0.1f)
        return default; // Not detected

    // Calculate detection confidence (finesse affects this)
    float finesseBonus = observerLimbs.FinesseSkill / 100f;
    float confidence = bestChance * (0.5f + finesseBonus * 0.5f);

    return new AISensorReading
    {
        DetectedEntity = target,
        Type = detectedBy,
        Distance = distance,
        Position = GetPosition(target),
        DetectionTick = CurrentTick,
        ThreatLevel = CalculateThreat(observer, target),
        Desirability = CalculateDesirability(observer, target),
        DetectionConfidence = confidence
    };
}
```

**Example Scenarios**:

1. **Blind Ranger Tracking Orc**:
   - Ranger: Eyes = Destroyed (0.0x vision), Ears = Perfect (1.0x hearing), Nose = Healthy (0.9x smell), Finesse = 85
   - Orc: AuditorySignature = 1.4 (loud), OlfactorySignature = 1.5 (smelly)
   - Vision: 0% chance (blind)
   - Hearing: 30m * 1.0 * 1.35 (finesse) * 1.4 (loud orc) = 56.7m effective range
   - Smell: 20m * 0.9 * 1.35 * 1.5 = 36.5m effective range
   - Result: Detected via hearing at 40m, confidence 75% (high finesse compensates)

2. **Injured Scout Detecting Silent Elf**:
   - Scout: Eyes = Injured (0.6x vision), Ears = Healthy (0.9x hearing), Finesse = 60
   - Elf: VisualSignature = 0.9, AuditorySignature = 0.7 (quiet), OlfactorySignature = 0.8
   - Vision: 50m * 0.6 * 1.1 * 0.9 = 29.7m effective range
   - Hearing: 30m * 0.9 * 1.1 * 0.7 = 20.8m effective range
   - Smell: 20m * 1.0 * 1.1 * 0.8 = 17.6m effective range
   - Result: Must get within 20m to detect (vision severely impaired)

3. **Master Tracker Smelling Undead**:
   - Tracker: Nose = Enhanced (1.2x smell), Finesse = 95
   - Undead: OlfactorySignature = 1.8 (putrid decay), ScentType = Putrid
   - Smell: 20m * 1.2 * 1.45 (finesse) * 1.8 * 1.8 (putrid bonus) = 112m effective range!
   - Result: Detected via smell from extreme range, confidence 95% (knows exactly where undead are)

4. **Deaf Dwarf vs Chittering Insectoid**:
   - Dwarf: Ears = Destroyed (0.0x hearing), Eyes = Perfect (1.0x vision)
   - Insectoid: AuditorySignature = 1.2 (constant clicking)
   - Hearing: 0% chance (deaf, cannot hear clicking)
   - Vision: Normal detection only
   - Result: Dwarf unaware of insectoid approach via sound, must rely on vision

### 3. Utility Scoring Components

Utility functions evaluate potential actions and select the best one.

```csharp
// Buffer of action options evaluated this decision cycle
public struct AIUtilityOption : IBufferElementData
{
    public ActionType Action;
    public Entity Target;                        // Target entity (resource, enemy, etc.)
    public float3 Destination;                   // Target location
    public float UtilityScore;                   // 0.0-1.0 (higher = better)
    public float ConfidenceLevel;                // 0.0-1.0 (certainty in decision)
}

public enum ActionType : byte
{
    // Universal
    Idle,
    MoveTo,
    Flee,

    // Godgame
    GatherResource,
    DeliverResource,
    BuildStructure,
    AttackEnemy,
    HealAlly,
    Socialize,
    Rest,
    Worship,

    // Space4X
    MineDeposit,
    HaulCargo,
    CombatEngage,
    Surveysector,
    ConstructStation,
    TradeWithStation,
    Refit,
    Dock,

    // Aggregate-specific
    FormGroup,
    SplitGroup,
    CoordinateAttack,
    Retreat
}
```

```csharp
// Utility curve configuration (defines how scores are calculated)
public struct AIUtilityCurve : IComponentData
{
    public ActionType Action;
    public CurveType Curve;                      // Linear, Exponential, Sigmoid, etc.
    public float MinInput;                       // Input range start
    public float MaxInput;                       // Input range end
    public float OutputMultiplier;               // Scale output score
}

public enum CurveType : byte
{
    Linear,         // y = x
    Exponential,    // y = x^2
    Logarithmic,    // y = log(x)
    Sigmoid,        // y = 1/(1+e^-x) (S-curve)
    Inverse,        // y = 1 - x
    Constant        // y = fixed value
}
```

### 4. Steering Components

Steering handles movement physics and pathfinding.

```csharp
public struct AISteeringState : IComponentData
{
    public float3 Velocity;                      // Current velocity
    public float3 DesiredVelocity;               // Target velocity
    public float MaxSpeed;                       // Top speed
    public float MaxAcceleration;                // How fast can change direction
    public float RotationSpeed;                  // Turning rate
    public SteeringMode Mode;
}

public enum SteeringMode : byte
{
    None,           // No steering (stationary or external physics)
    Seek,           // Move toward target
    Flee,           // Move away from target
    Wander,         // Random wandering
    FollowPath,     // Follow waypoint path
    Flock,          // Group movement (cohesion, separation, alignment)
    Orbit           // Circle around target
}
```

```csharp
// Path following (waypoint queue)
public struct AIPathWaypoint : IBufferElementData
{
    public float3 Position;
    public float ArrivalRadius;                  // Distance to consider "reached"
    public WaypointType Type;
}

public enum WaypointType : byte
{
    Standard,       // Normal waypoint
    Pause,          // Stop and wait at this point
    Jump,           // FTL jump point (Space4X)
    Highway,        // Highway gate (Space4X)
    Teleport        // Instant travel (magic portal, wormhole)
}
```

### 5. Task Execution Components

Tasks represent discrete actions the AI is performing.

```csharp
public struct AITaskState : IComponentData
{
    public ActionType CurrentAction;
    public Entity TargetEntity;
    public float3 TargetPosition;
    public TaskPhase Phase;
    public ushort TicksInPhase;
    public float ProgressPercent;                // 0.0-1.0
}

public enum TaskPhase : byte
{
    Planning,       // Evaluating if task is possible
    Preparing,      // Acquiring resources/tools
    Traveling,      // Moving to task location
    Executing,      // Performing task
    Completing,     // Finishing up
    Failed,         // Task cannot complete
    Aborted         // Task canceled externally
}
```

---

## AI System Architecture

### System Execution Order

```
FixedStepSimulationSystemGroup
  └─ AISystemGroup (custom group)
      ├─ AISensorUpdateSystem          [Reads world, populates sensor buffers]
      ├─ AIUtilityEvaluationSystem     [Scores actions, selects best]
      ├─ AITaskPlanningSystem          [Validates selected action, creates task]
      ├─ AISteeringSystem              [Calculates movement toward task target]
      ├─ AITaskExecutionSystem         [Performs task logic]
      └─ AIStateCleanupSystem          [Removes completed tasks, resets cooldowns]
```

---

## 1. AISensorUpdateSystem

**Responsibility**: Populate `AISensorReading` buffer with detected entities.

**Logic**:
```csharp
[BurstCompile]
public partial struct AISensorUpdateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
{
        var timeState = SystemAPI.GetSingleton<TimeState>();

        foreach (var (agent, sensorConfig, sensorBuffer, transform) in
                 SystemAPI.Query<RefRO<AIAgent>, RefRO<AISensorConfig>, DynamicBuffer<AISensorReading>, RefRO<LocalTransform>>())
        {
            // Throttle updates based on UpdateRate
            if (agent.ValueRO.DecisionCooldown > 0)
                continue;

            sensorBuffer.Clear(); // Clear old readings

            // Vision sensor: spatial grid query within range
            var nearbyEntities = SpatialQuery.GetEntitiesWithinRadius(
                transform.ValueRO.Position,
                sensorConfig.ValueRO.VisionRange
            );

            foreach (var detected in nearbyEntities)
            {
                // Filter by FOV angle
                float3 dirToTarget = math.normalize(detected.Position - transform.ValueRO.Position);
                float3 forward = math.forward(transform.ValueRO.Rotation);
                float angle = math.degrees(math.acos(math.dot(dirToTarget, forward)));

                if (angle > sensorConfig.ValueRO.VisionAngle / 2)
                    continue; // Outside FOV

                // Line-of-sight check (raycast, optional for performance)
                if (!HasLineOfSight(transform.ValueRO.Position, detected.Position))
                    continue;

                // Add to sensor buffer
                sensorBuffer.Add(new AISensorReading
                {
                    DetectedEntity = detected.Entity,
                    Type = SensorType.Vision,
                    Distance = math.distance(transform.ValueRO.Position, detected.Position),
                    Position = detected.Position,
                    DetectionTick = timeState.CurrentTick,
                    ThreatLevel = CalculateThreat(agent.ValueRO, detected),
                    Desirability = CalculateDesirability(agent.ValueRO, detected)
                });
            }

            // Registry sensor: query known resources/entities (perfect knowledge)
            if (sensorConfig.ValueRO.SensorRange > 0)
            {
                // Example: query resource registry
                var resourceRegistry = SystemAPI.GetSingletonBuffer<ResourceRegistryEntry>();
                foreach (var resource in resourceRegistry)
                {
                    if (math.distance(transform.ValueRO.Position, resource.Position) > sensorConfig.ValueRO.SensorRange)
                        continue;

                    sensorBuffer.Add(new AISensorReading
                    {
                        DetectedEntity = resource.Entity,
                        Type = SensorType.Registry,
                        Distance = math.distance(transform.ValueRO.Position, resource.Position),
                        Position = resource.Position,
                        DetectionTick = timeState.CurrentTick,
                        ThreatLevel = 0f,
                        Desirability = 0.8f // Resources are desirable
                    });
                }
            }
        }
    }
}
```

**Threat Calculation** (example):
```csharp
float CalculateThreat(AIAgent agent, DetectedEntity detected)
{
    // Check if detected entity is hostile
    if (!HasComponent<Alignment>(detected.Entity))
        return 0f;

    var myAlignment = GetComponent<Alignment>(agent.Entity);
    var theirAlignment = GetComponent<Alignment>(detected.Entity);

    // Alignment mismatch = threat
    float alignmentDelta = math.distance(myAlignment.MoralAxis, theirAlignment.MoralAxis);
    alignmentDelta += math.distance(myAlignment.OrderAxis, theirAlignment.OrderAxis);
    alignmentDelta += math.distance(myAlignment.PurityAxis, theirAlignment.PurityAxis);

    // Check if they have weapons/combat capability
    float combatThreat = HasComponent<CombatCapability>(detected.Entity) ? 0.5f : 0f;

    return math.clamp(alignmentDelta / 6f + combatThreat, 0f, 1f);
}
```

---

## 2. AIUtilityEvaluationSystem

**Responsibility**: Evaluate all possible actions, score them, select best option.

**Logic**:
```csharp
[BurstCompile]
public partial struct AIUtilityEvaluationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (agent, sensorBuffer, utilityBuffer, transform) in
                 SystemAPI.Query<RefRW<AIAgent>, DynamicBuffer<AISensorReading>, DynamicBuffer<AIUtilityOption>, RefRO<LocalTransform>>())
        {
            // Throttle decisions
            if (agent.ValueRO.DecisionCooldown > 0)
            {
                agent.ValueRW.DecisionCooldown--;
                continue;
            }

            utilityBuffer.Clear();

            // Evaluate each possible action based on archetype
            switch (agent.ValueRO.Archetype)
            {
                case AIArchetype.Villager:
                    EvaluateVillagerActions(ref agent.ValueRW, sensorBuffer, utilityBuffer, transform.ValueRO);
                    break;

                case AIArchetype.Carrier:
                    EvaluateCarrierActions(ref agent.ValueRW, sensorBuffer, utilityBuffer, transform.ValueRO);
                    break;

                case AIArchetype.Creature:
                    EvaluateCreatureActions(ref agent.ValueRW, sensorBuffer, utilityBuffer, transform.ValueRO);
                    break;
            }

            // Select highest scoring action
            if (utilityBuffer.Length == 0)
            {
                // No valid actions, go idle
                agent.ValueRW.Mode = AIBehaviorMode.Idle;
                agent.ValueRW.DecisionCooldown = agent.ValueRO.ThinkInterval;
                continue;
            }

            AIUtilityOption bestOption = utilityBuffer[0];
            for (int i = 1; i < utilityBuffer.Length; i++)
            {
                if (utilityBuffer[i].UtilityScore > bestOption.UtilityScore)
                    bestOption = utilityBuffer[i];
            }

            // Commit to best action (AITaskPlanningSystem will create task)
            agent.ValueRW.Mode = GetModeForAction(bestOption.Action);
            agent.ValueRW.DecisionCooldown = agent.ValueRO.ThinkInterval;

            // Store selected option (task system reads this)
            utilityBuffer.Clear();
            utilityBuffer.Add(bestOption); // Only keep best option
        }
    }
}
```

**Example: Villager Action Evaluation**
```csharp
void EvaluateVillagerActions(
    ref AIAgent agent,
    DynamicBuffer<AISensorReading> sensors,
    DynamicBuffer<AIUtilityOption> options,
    LocalTransform transform)
{
    // Action: GatherResource
    foreach (var sensor in sensors)
    {
        if (!IsResource(sensor.DetectedEntity))
            continue;

        float distance = sensor.Distance;
        float distanceScore = 1f - math.clamp(distance / 100f, 0f, 1f); // Closer = better
        float desirability = sensor.Desirability;

        float utilityScore = distanceScore * 0.6f + desirability * 0.4f;

        options.Add(new AIUtilityOption
        {
            Action = ActionType.GatherResource,
            Target = sensor.DetectedEntity,
            Destination = sensor.Position,
            UtilityScore = utilityScore,
            ConfidenceLevel = 0.8f
        });
    }

    // Action: Flee (if threat detected)
    float maxThreat = 0f;
    Entity mostDangerousEntity = Entity.Null;
    foreach (var sensor in sensors)
    {
        if (sensor.ThreatLevel > maxThreat)
        {
            maxThreat = sensor.ThreatLevel;
            mostDangerousEntity = sensor.DetectedEntity;
        }
    }

    if (maxThreat > 0.5f)
    {
        // Flee from threat
        float3 fleeDirection = math.normalize(transform.Position - GetPosition(mostDangerousEntity));
        float3 fleeDestination = transform.Position + fleeDirection * 50f;

        options.Add(new AIUtilityOption
        {
            Action = ActionType.Flee,
            Target = mostDangerousEntity,
            Destination = fleeDestination,
            UtilityScore = maxThreat, // Higher threat = higher priority to flee
            ConfidenceLevel = 1.0f
        });
    }

    // Action: Rest (if low energy)
    var villagerNeeds = GetComponent<VillagerNeeds>(agent.Entity);
    if (villagerNeeds.Energy < 30f)
    {
        float restUrgency = 1f - (villagerNeeds.Energy / 100f);

        options.Add(new AIUtilityOption
        {
            Action = ActionType.Rest,
            Target = Entity.Null,
            Destination = transform.Position, // Rest in place
            UtilityScore = restUrgency,
            ConfidenceLevel = 1.0f
        });
    }

    // Action: Socialize (if lonely)
    if (villagerNeeds.Morale < 50f)
    {
        // Find nearest friendly villager
        Entity nearestAlly = FindNearestAlly(sensors);
        if (nearestAlly != Entity.Null)
        {
            float socialUrgency = 1f - (villagerNeeds.Morale / 100f);

            options.Add(new AIUtilityOption
            {
                Action = ActionType.Socialize,
                Target = nearestAlly,
                Destination = GetPosition(nearestAlly),
                UtilityScore = socialUrgency * 0.7f, // Lower priority than survival
                ConfidenceLevel = 0.6f
            });
        }
    }
}
```

**Example: Carrier Action Evaluation** (Space4X)
```csharp
void EvaluateCarrierActions(
    ref AIAgent agent,
    DynamicBuffer<AISensorReading> sensors,
    DynamicBuffer<AIUtilityOption> options,
    LocalTransform transform)
{
    var carrier = GetComponent<Carrier>(agent.Entity);

    // Action: MineDeposit (if carrier has Mining role)
    if (carrier.ActiveRole == CarrierRole.Mining)
    {
        foreach (var sensor in sensors)
        {
            if (!IsDeposit(sensor.DetectedEntity))
                continue;

            var deposit = GetComponent<Deposit>(sensor.DetectedEntity);

            float richness = deposit.YieldRemaining / deposit.YieldMax;
            float distance = sensor.Distance;
            float distanceScore = 1f - math.clamp(distance / 500f, 0f, 1f);

            float utilityScore = richness * 0.7f + distanceScore * 0.3f;

            options.Add(new AIUtilityOption
            {
                Action = ActionType.MineDeposit,
                Target = sensor.DetectedEntity,
                Destination = sensor.Position,
                UtilityScore = utilityScore,
                ConfidenceLevel = 0.9f
            });
        }
    }

    // Action: CombatEngage (if hostile detected and carrier has weapons)
    if (carrier.ActiveRole == CarrierRole.Combat)
    {
        foreach (var sensor in sensors)
        {
            if (sensor.ThreatLevel < 0.3f)
                continue; // Not hostile enough

            var carrierStats = GetCarrierStats(agent.Entity);
            float combatPower = carrierStats.TotalDPS / 1000f; // Normalize
            float enemyPower = EstimateEnemyPower(sensor.DetectedEntity);

            float powerRatio = combatPower / math.max(enemyPower, 0.1f);
            float engageChance = math.clamp(powerRatio, 0f, 1f);

            if (engageChance < 0.3f)
            {
                // Too weak, flee instead
                options.Add(new AIUtilityOption
                {
                    Action = ActionType.Flee,
                    Target = sensor.DetectedEntity,
                    Destination = transform.Position - math.normalize(sensor.Position - transform.Position) * 200f,
                    UtilityScore = sensor.ThreatLevel,
                    ConfidenceLevel = 0.8f
                });
            }
            else
            {
                // Strong enough, engage
                options.Add(new AIUtilityOption
                {
                    Action = ActionType.CombatEngage,
                    Target = sensor.DetectedEntity,
                    Destination = sensor.Position,
                    UtilityScore = sensor.ThreatLevel * engageChance,
                    ConfidenceLevel = engageChance
                });
            }
        }
    }

    // Action: SurveySector (if Exploration role and in unsurveyed sector)
    if (carrier.ActiveRole == CarrierRole.Exploration)
    {
        var sectorVisibility = GetSectorVisibility(transform.Position);
        if (sectorVisibility == VisibilityLevel.Unknown || sectorVisibility == VisibilityLevel.Stale)
        {
            options.Add(new AIUtilityOption
            {
                Action = ActionType.SurveyS ector,
                Target = Entity.Null,
                Destination = transform.Position, // Survey current sector
                UtilityScore = 0.9f, // High priority for explorers
                ConfidenceLevel = 1.0f
            });
        }
    }
}
```

---

## 3. AITaskPlanningSystem

**Responsibility**: Convert selected utility option into executable task.

**Logic**:
```csharp
public partial struct AITaskPlanningSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (utilityBuffer, taskState, agent) in
                 SystemAPI.Query<DynamicBuffer<AIUtilityOption>, RefRW<AITaskState>, RefRO<AIAgent>>())
        {
            if (utilityBuffer.Length == 0)
                continue; // No action selected

            var selectedOption = utilityBuffer[0]; // Best action from evaluation system

            // Create task from selected option
            taskState.ValueRW.CurrentAction = selectedOption.Action;
            taskState.ValueRW.TargetEntity = selectedOption.Target;
            taskState.ValueRW.TargetPosition = selectedOption.Destination;
            taskState.ValueRW.Phase = TaskPhase.Planning;
            taskState.ValueRW.TicksInPhase = 0;
            taskState.ValueRW.ProgressPercent = 0f;

            // Validate task is possible (resources available, target still exists, etc.)
            if (!ValidateTask(taskState.ValueRO, agent.ValueRO))
            {
                taskState.ValueRW.Phase = TaskPhase.Failed;
                continue;
            }

            // Transition to next phase
            taskState.ValueRW.Phase = GetNextPhase(selectedOption.Action);
        }
    }
}
```

**Phase Determination**:
```csharp
TaskPhase GetNextPhase(ActionType action)
{
    switch (action)
    {
        case ActionType.GatherResource:
        case ActionType.MineDeposit:
        case ActionType.CombatEngage:
            return TaskPhase.Traveling; // Must move to target first

        case ActionType.Rest:
        case ActionType.Idle:
            return TaskPhase.Executing; // Can execute immediately

        case ActionType.Flee:
            return TaskPhase.Executing; // Start fleeing immediately

        default:
            return TaskPhase.Preparing; // Default: prepare before acting
    }
}
```

---

## 4. AISteeringSystem

**Responsibility**: Calculate movement toward task target.

**Logic**:
```csharp
[BurstCompile]
public partial struct AISteeringSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.GetSingleton<TimeState>().FixedDeltaTime;

        foreach (var (steering, taskState, transform) in
                 SystemAPI.Query<RefRW<AISteeringState>, RefRO<AITaskState>, RefRW<LocalTransform>>())
        {
            if (taskState.ValueRO.Phase != TaskPhase.Traveling)
                continue; // Not moving

            float3 targetPos = taskState.ValueRO.TargetPosition;
            float3 currentPos = transform.ValueRO.Position;

            float distance = math.distance(currentPos, targetPos);

            // Reached destination
            if (distance < 1f)
            {
                steering.ValueRW.DesiredVelocity = float3.zero;
                continue;
            }

            // Calculate desired velocity
            float3 direction = math.normalize(targetPos - currentPos);
            steering.ValueRW.DesiredVelocity = direction * steering.ValueRO.MaxSpeed;

            // Apply steering (blend current velocity toward desired)
            float3 steeringForce = steering.ValueRO.DesiredVelocity - steering.ValueRO.Velocity;
            steeringForce = math.clamp(steeringForce, -steering.ValueRO.MaxAcceleration, steering.ValueRO.MaxAcceleration);

            steering.ValueRW.Velocity += steeringForce * deltaTime;
            steering.ValueRW.Velocity = math.clamp(steering.ValueRW.Velocity, -steering.ValueRO.MaxSpeed, steering.ValueRO.MaxSpeed);

            // Update position
            transform.ValueRW.Position += steering.ValueRO.Velocity * deltaTime;

            // Update rotation to face movement direction
            if (math.lengthsq(steering.ValueRO.Velocity) > 0.01f)
            {
                float3 forward = math.normalize(steering.ValueRO.Velocity);
                quaternion targetRotation = quaternion.LookRotationSafe(forward, math.up());
                transform.ValueRW.Rotation = math.slerp(transform.ValueRO.Rotation, targetRotation, steering.ValueRO.RotationSpeed * deltaTime);
            }
        }
    }
}
```

**Flocking Behavior** (for groups like bands/fleets):
```csharp
float3 CalculateFlockingSteering(Entity agent, DynamicBuffer<AISensorReading> sensors)
{
    float3 cohesion = float3.zero;    // Move toward group center
    float3 separation = float3.zero;  // Avoid crowding neighbors
    float3 alignment = float3.zero;   // Match group velocity

    int neighborCount = 0;

    foreach (var sensor in sensors)
    {
        if (sensor.Type != SensorType.Vision || sensor.Distance > 20f)
            continue;

        if (!IsSameGroup(agent, sensor.DetectedEntity))
            continue;

        neighborCount++;

        // Cohesion: average position
        cohesion += sensor.Position;

        // Separation: push away from close neighbors
        if (sensor.Distance < 5f)
        {
            float3 away = GetPosition(agent) - sensor.Position;
            separation += math.normalize(away) / sensor.Distance; // Closer = stronger push
        }

        // Alignment: average velocity
        var neighborVelocity = GetComponent<AISteeringState>(sensor.DetectedEntity).Velocity;
        alignment += neighborVelocity;
    }

    if (neighborCount == 0)
        return float3.zero;

    cohesion /= neighborCount;
    cohesion = math.normalize(cohesion - GetPosition(agent)); // Direction to center

    alignment /= neighborCount;
    alignment = math.normalize(alignment); // Average direction

    // Weighted combination
    return cohesion * 0.3f + separation * 0.5f + alignment * 0.2f;
}
```

---

## 5. AITaskExecutionSystem

**Responsibility**: Perform task-specific logic (gather, attack, etc.).

**Logic**:
```csharp
public partial struct AITaskExecutionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (taskState, agent, transform) in
                 SystemAPI.Query<RefRW<AITaskState>, RefRO<AIAgent>, RefRO<LocalTransform>>())
        {
            if (taskState.ValueRO.Phase != TaskPhase.Executing)
                continue;

            taskState.ValueRW.TicksInPhase++;

            switch (taskState.ValueRO.CurrentAction)
            {
                case ActionType.GatherResource:
                    ExecuteGatherResource(ref taskState.ValueRW, agent.ValueRO, transform.ValueRO);
                    break;

                case ActionType.AttackEnemy:
                    ExecuteAttackEnemy(ref taskState.ValueRW, agent.ValueRO, transform.ValueRO);
                    break;

                case ActionType.Rest:
                    ExecuteRest(ref taskState.ValueRW, agent.ValueRO);
                    break;

                case ActionType.MineDeposit:
                    ExecuteMineDeposit(ref taskState.ValueRW, agent.ValueRO, transform.ValueRO);
                    break;

                // ... other actions
            }
        }
    }
}
```

**Example: Execute Gather Resource**
```csharp
void ExecuteGatherResource(ref AITaskState task, AIAgent agent, LocalTransform transform)
{
    // Check if target still exists
    if (task.TargetEntity == Entity.Null || !Exists(task.TargetEntity))
    {
        task.Phase = TaskPhase.Failed;
        return;
    }

    // Check distance to target
    var targetPos = GetPosition(task.TargetEntity);
    if (math.distance(transform.Position, targetPos) > 2f)
    {
        // Not close enough, transition back to Traveling
        task.Phase = TaskPhase.Traveling;
        return;
    }

    // Gather resource over time (e.g., 100 ticks)
    const ushort gatherDuration = 100;
    task.ProgressPercent = (float)task.TicksInPhase / gatherDuration;

    if (task.TicksInPhase >= gatherDuration)
    {
        // Gathering complete, add resource to inventory
        var villagerJob = GetComponent<VillagerJobTicket>(agent.Entity);
        // ... add resource to carry buffer ...

        task.Phase = TaskPhase.Completing;
    }
}
```

**Example: Execute Attack Enemy**
```csharp
void ExecuteAttackEnemy(ref AITaskState task, AIAgent agent, LocalTransform transform)
{
    if (task.TargetEntity == Entity.Null || !Exists(task.TargetEntity))
    {
        task.Phase = TaskPhase.Completed;
        return; // Enemy destroyed or fled
    }

    var targetPos = GetPosition(task.TargetEntity);
    float distance = math.distance(transform.Position, targetPos);

    // Get attack range from combat component
    var combat = GetComponent<CombatCapability>(agent.Entity);

    if (distance > combat.AttackRange)
    {
        // Too far, chase
        task.Phase = TaskPhase.Traveling;
        task.TargetPosition = targetPos; // Update destination
        return;
    }

    // Attack every N ticks (attack speed)
    if (task.TicksInPhase % combat.AttackSpeed == 0)
    {
        // Deal damage to target
        var targetHealth = GetComponentRW<Health>(task.TargetEntity);
        targetHealth.ValueRW.Current -= combat.Damage;

        if (targetHealth.ValueRO.Current <= 0)
        {
            // Enemy defeated
            task.Phase = TaskPhase.Completing;
        }
    }
}
```

---

## Behavior Profiles (Data-Driven Configuration)

AI behaviors are configured via ScriptableObject profiles.

```csharp
// ScriptableObject asset
[CreateAssetMenu(fileName = "NewAIBehaviorProfile", menuName = "PureDOTS/AI/Behavior Profile")]
public class AIBehaviorProfile : ScriptableObject
{
    public AIArchetype Archetype;
    public ushort ThinkInterval = 20; // Ticks between decisions

    [Header("Sensors")]
    public float VisionRange = 30f;
    public float VisionAngle = 120f;
    public float HearingRange = 50f;
    public SensorUpdateRate SensorRate = SensorUpdateRate.Normal;

    [Header("Steering")]
    public float MaxSpeed = 5f;
    public float MaxAcceleration = 2f;
    public float RotationSpeed = 3f;

    [Header("Utility Curves")]
    public List<UtilityCurveData> UtilityCurves;

    [Header("Aggression")]
    public float AggressionLevel = 0.5f;      // 0.0 = pacifist, 1.0 = always attack
    public float FleeThreshold = 0.3f;        // Threat level to trigger flee
}

[Serializable]
public struct UtilityCurveData
{
    public ActionType Action;
    public AnimationCurve Curve; // Unity AnimationCurve for visual editing
    public float Multiplier;
}
```

**Baker** converts profile to blob:
```csharp
public class AIBehaviorProfileAuthoring : MonoBehaviour
{
    public AIBehaviorProfile Profile;

    class Baker : Baker<AIBehaviorProfileAuthoring>
    {
        public override void Bake(AIBehaviorProfileAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new AIAgent
            {
                Archetype = authoring.Profile.Archetype,
                Mode = AIBehaviorMode.Idle,
                ThinkInterval = authoring.Profile.ThinkInterval
            });

            AddComponent(entity, new AISensorConfig
            {
                VisionRange = authoring.Profile.VisionRange,
                VisionAngle = authoring.Profile.VisionAngle,
                HearingRange = authoring.Profile.HearingRange,
                UpdateRate = authoring.Profile.SensorRate
            });

            AddComponent(entity, new AISteeringState
            {
                MaxSpeed = authoring.Profile.MaxSpeed,
                MaxAcceleration = authoring.Profile.MaxAcceleration,
                RotationSpeed = authoring.Profile.RotationSpeed
            });

            // Add buffers
            AddBuffer<AISensorReading>(entity);
            AddBuffer<AIUtilityOption>(entity);
            AddBuffer<AIPathWaypoint>(entity);

            // Bake utility curves to blob (omitted for brevity)
        }
    }
}
```

---

## Aggregate AI Decision-Making

Aggregates (bands, fleets, villages) use the same framework but with different considerations.

**Aggregate Utility Example** (Band combat decision):
```csharp
void EvaluateBandActions(
    ref AIAgent agent,
    DynamicBuffer<AISensorReading> sensors,
    DynamicBuffer<AIUtilityOption> options,
    LocalTransform transform)
{
    var band = GetComponent<Band>(agent.Entity);
    var bandMembers = GetBuffer<BandMemberEntry>(agent.Entity);

    // Action: CoordinateAttack (if enemy detected)
    foreach (var sensor in sensors)
    {
        if (sensor.ThreatLevel < 0.4f)
            continue;

        // Calculate band combat power vs. enemy
        float bandPower = CalculateBandCombatPower(bandMembers);
        float enemyPower = EstimateEnemyPower(sensor.DetectedEntity);

        float powerRatio = bandPower / math.max(enemyPower, 0.1f);

        if (powerRatio > 1.5f)
        {
            // Overwhelming advantage, attack
            options.Add(new AIUtilityOption
            {
                Action = ActionType.CoordinateAttack,
                Target = sensor.DetectedEntity,
                Destination = sensor.Position,
                UtilityScore = math.min(powerRatio / 2f, 1f),
                ConfidenceLevel = 0.9f
            });
        }
        else if (powerRatio < 0.5f)
        {
            // Outnumbered, retreat
            options.Add(new AIUtilityOption
            {
                Action = ActionType.Retreat,
                Target = sensor.DetectedEntity,
                Destination = CalculateRetreatPosition(transform.Position, sensor.Position),
                UtilityScore = (1f - powerRatio) * 0.8f,
                ConfidenceLevel = 0.95f
            });
        }
    }

    // Action: SplitGroup (if band too large, low cohesion)
    if (bandMembers.Length > 20 && band.Cohesion < 0.4f)
    {
        options.Add(new AIUtilityOption
        {
            Action = ActionType.SplitGroup,
            Target = Entity.Null,
            Destination = transform.Position,
            UtilityScore = 1f - band.Cohesion, // Low cohesion = high split urgency
            ConfidenceLevel = 0.7f
        });
    }
}
```

---

## Rewind Integration

AI state must save/restore for rewind compatibility.

```csharp
public struct AIHistorySample : IBufferElementData
{
    public ushort Tick;
    public AIBehaviorMode Mode;
    public TaskPhase TaskPhase;
    public ActionType CurrentAction;
    public Entity TargetEntity;
    public float3 TargetPosition;
}
```

**Recording**:
```csharp
var history = GetBuffer<AIHistorySample>(agent);
history.Add(new AIHistorySample
{
    Tick = timeState.CurrentTick,
    Mode = aiAgent.Mode,
    TaskPhase = taskState.Phase,
    CurrentAction = taskState.CurrentAction,
    TargetEntity = taskState.TargetEntity,
    TargetPosition = taskState.TargetPosition
});
```

**Playback**:
```csharp
var sample = FindSampleForTick(history, rewindState.PlaybackTick);
aiAgent.Mode = sample.Mode;
taskState.Phase = sample.TaskPhase;
taskState.CurrentAction = sample.CurrentAction;
taskState.TargetEntity = sample.TargetEntity;
taskState.TargetPosition = sample.TargetPosition;
```

---

## Open Questions / Design Decisions Needed

1. **Sensor LOD (Level of Detail)**: Should distant entities use lower-fidelity sensors (save performance)?
   - *Suggestion*: Yes - entities >100 units away update at Slow rate, <20 units at Fast rate

2. **Utility Curve Authoring**: AnimationCurve in Unity editor OR code-defined curves?
   - *Suggestion*: AnimationCurve for designer friendliness, baked to blob for runtime

3. **Task Interruption**: Can higher-priority task interrupt current task?
   - *Suggestion*: Yes - if new utility score >1.5x current task score, abort and switch

4. **Path Caching**: Should pathfinding results be cached for repeated queries?
   - *Suggestion*: Yes - cache path for 100 ticks, revalidate if target moves >10 units

5. **Group Coordination**: How do band/fleet members synchronize (all attack same target)?
   - *Suggestion*: Aggregate entity broadcasts "focus target" to members, individual AI respects it

6. **Fleeing Behavior**: Should fleeing entities path around obstacles or straight-line away?
   - *Suggestion*: Straight-line for performance, add obstacle avoidance if collision imminent

7. **Resting Duration**: Fixed duration OR until needs restored?
   - *Suggestion*: Until needs restored (energy >80%), with max cap of 200 ticks

8. **Aggression Inheritance**: Do aggregate entities inherit aggression from members OR have independent value?
   - *Suggestion*: Average of members, updated when composition changes

---

## Implementation Notes

- **AISystemGroup** = custom system group, runs after input, before presentation
- **AISensorUpdateSystem** = spatial queries, populates sensor buffers
- **AIUtilityEvaluationSystem** = scores actions, selects best
- **AITaskPlanningSystem** = creates tasks from selected actions
- **AISteeringSystem** = movement physics
- **AITaskExecutionSystem** = action-specific logic
- **AIStateCleanupSystem** = removes completed tasks
- All systems respect `RewindState.Mode` (skip during Playback)

---

## References

- **Villager Jobs**: [VillagerJobs_DOTS.md](../DesignNotes/VillagerJobs_DOTS.md) - Job assignment integration
- **Carrier Architecture**: [CarrierArchitecture.md](CarrierArchitecture.md) - Carrier role AI decisions
- **Combat Loop**: [CombatLoop.md](CombatLoop.md) - Combat AI integration
- **Alignment System**: Alignment affects threat calculation
- **Spatial Grid**: Sensor queries use spatial partitioning
- **Registry System**: Registry sensor type for perfect knowledge
