# Procedural World Generation System

## Overview

The world generation system creates unique pantheons, galactic factions, and environmental behaviors each playthrough while maintaining game balance and narrative coherence. Gods (Godgame) and cosmic forces (Space4X) are procedurally generated from archetypes, with player-configurable settings determining generation parameters before world creation.

**Key Principles**:
- **Replayability**: Gods/factions reroll personalities, alignments, and rivalries each playthrough
- **Player Agency**: Pre-generation settings allow customization of difficulty, pantheon size, and narrative themes
- **Cross-Game Compatibility**: Same generation framework powers both Godgame deities and Space4X cosmic entities
- **Deterministic**: Seed-based generation ensures rewind compatibility and multiplayer sync
- **Technical**: Burst-compatible, DOTS-native, blob asset backed

---

## Core Concepts

### God Archetypes

Gods are generated from **archetypes** - templates defining domain, personality bounds, and behavior patterns. Archetypes ensure generated gods are unique yet coherent.

**Archetype Structure**:
```csharp
public struct GodArchetype : IComponentData
{
    public FixedString64Bytes ArchetypeId;      // "Fire", "Growth", "Time", "Fortune"
    public GodDomain Domain;                     // Natural phenomenon controlled
    public TriAxisAlignmentBounds AlignmentBounds; // Randomization ranges
    public BehaviorTraitWeights TraitWeights;    // Personality tendencies
    public uint IconSeed;                        // Visual generation seed
    public uint NameGeneratorSeed;               // Name generation seed
}

public struct TriAxisAlignmentBounds
{
    public int2 MoralRange;    // (-100, +100) → Good/Evil
    public int2 OrderRange;    // (-100, +100) → Lawful/Chaotic
    public int2 PurityRange;   // (-100, +100) → Natural/Corrupted
}

public struct BehaviorTraitWeights
{
    public float VengefulWeight;   // 0.0 = Always Forgiving, 1.0 = Always Vengeful
    public float BoldWeight;       // 0.0 = Craven, 1.0 = Bold
    public float GreedyWeight;     // 0.0 = Generous, 1.0 = Greedy
    public float IsolationistWeight; // 0.0 = Collaborative, 1.0 = Isolationist
}
```

**Example Archetype Definitions**:

| Archetype | Domain | Moral Range | Order Range | Purity Range | Trait Tendencies |
|-----------|--------|-------------|-------------|--------------|------------------|
| Fire | Heat, Destruction, Energy | (-80, +20) | (-60, +60) | (-40, +80) | Vengeful (0.7), Bold (0.8) |
| Water | Oceans, Storms, Flow | (-40, +60) | (-80, +40) | (0, +100) | Forgiving (0.3), Adaptive |
| Growth | Plants, Life, Fertility | (40, +100) | (-40, +40) | (60, +100) | Generous (0.2), Patient |
| Time | Chronology, Aging, Decay | (-20, +20) | (60, +100) | (-60, +60) | Neutral, Isolationist (0.6) |
| Fortune | Luck, Chance, Fate | (-60, +60) | (-100, +40) | (-80, +80) | Chaotic (0.8), Greedy (0.5) |
| Death | Mortality, Endings, Entropy | (-60, 0) | (20, +80) | (-100, +20) | Vengeful (0.6), Patient |
| Earth | Stone, Mountains, Stability | (-20, +60) | (40, +100) | (40, +100) | Stubborn (0.7), Loyal |
| Wind | Air, Movement, Freedom | (0, +80) | (-80, -20) | (20, +100) | Bold (0.6), Collaborative (0.7) |
| Life | Vitality, Healing, Creation | (60, +100) | (-40, +60) | (80, +100) | Forgiving (0.8), Generous (0.9) |

---

### Procedural God Generation

Each playthrough generates a **pantheon** of gods from selected archetypes, randomizing their specific alignments, personalities, and relationships.

**Generation Pipeline**:
```
SEED → ARCHETYPE SELECTION → ALIGNMENT ROLL → PERSONALITY ROLL → RIVALRY GENERATION → PANTHEON VALIDATION → WORLD SPAWN
```

**Step 1: Archetype Selection**
```csharp
public struct PantheonGenerationSettings : IComponentData
{
    public uint WorldSeed;                    // Master seed for all randomization
    public byte PantheonSize;                 // 6-12 gods
    public PantheonComposition Composition;   // Balanced, LifeHeavy, DestructionHeavy, Chaotic
    public bool GuaranteeRivalPairs;          // Ensure Fire/Water, Life/Death, etc.
    public bool GuaranteeNeutrals;            // Ensure at least 2 neutral-aligned gods
}

public enum PantheonComposition : byte
{
    Balanced = 0,          // Equal distribution across alignments
    LifeHeavy = 1,         // More Good/Natural gods
    DestructionHeavy = 2,  // More Evil/Chaotic gods
    Chaotic = 3,           // Random distribution, no balance guarantees
    PlayerCustom = 4       // Player manually selects archetypes
}
```

**Step 2: Alignment Randomization**
```csharp
public struct GeneratedGod : IComponentData
{
    public Entity GodEntity;
    public FixedString64Bytes GodName;        // Procedurally generated
    public FixedString64Bytes ArchetypeId;    // "Fire", "Water", etc.
    public TriAxisAlignment Alignment;        // Rolled within bounds
    public BehaviorTraits Traits;             // Rolled from weights
    public uint VisualSeed;                   // Icon/avatar generation
    public GodGenerationFlags Flags;
}

public struct TriAxisAlignment
{
    public sbyte MoralAxis;    // -100 (Evil) to +100 (Good)
    public sbyte OrderAxis;    // -100 (Chaotic) to +100 (Lawful)
    public sbyte PurityAxis;   // -100 (Corrupted) to +100 (Natural)
}

public struct BehaviorTraits
{
    public byte VengefulScore;      // 0-100
    public byte BoldScore;          // 0-100
    public byte GreedyScore;        // 0-100
    public byte IsolationistScore;  // 0-100
}
```

**Randomization Example**:
```
Fire Archetype Bounds:
  Moral: (-80, +20)
  Order: (-60, +60)
  Purity: (-40, +80)

Generated Fire God #1 (Seed: 12345):
  Alignment: (-65, +15, +40)  → Evil, Lawful, Pure
  Name: "Pyraxis the Disciplined Flame"
  Traits: Vengeful=85, Bold=90, Greedy=40, Isolationist=30

Generated Fire God #2 (Seed: 67890):
  Alignment: (+10, -50, -20)  → Good, Chaotic, Corrupted
  Name: "Embera the Wildfire"
  Traits: Vengeful=60, Bold=95, Greedy=70, Isolationist=20
```

**Step 3: Rivalry and Alliance Generation**

Gods generate rivalries based on alignment distance and domain conflicts:

```csharp
public struct GodRelationshipTemplate : IBufferElementData
{
    public Entity OtherGod;
    public RelationshipType Type;             // Rival, Neutral, Allied
    public sbyte StartingRelationModifier;    // -50 to +50
    public InterferenceChance InterferenceChance; // How often they interfere with each other
}

public enum RelationshipType : byte
{
    Rival = 0,      // Opposing domains or alignments (Fire/Water, Life/Death)
    Neutral = 1,    // No inherent conflict
    Allied = 2,     // Similar alignments, complementary domains
}

public enum InterferenceChance : byte
{
    Never = 0,      // Gods ignore each other
    Rare = 1,       // 5% chance per miracle
    Occasional = 2, // 15% chance per miracle
    Frequent = 3,   // 30% chance per miracle
    Constant = 4    // 60% chance per miracle
}
```

**Rivalry Calculation**:
```csharp
// Alignment distance formula
float alignmentDistance =
    Math.Abs(godA.Alignment.MoralAxis - godB.Alignment.MoralAxis) +
    Math.Abs(godA.Alignment.OrderAxis - godB.Alignment.OrderAxis) +
    Math.Abs(godA.Alignment.PurityAxis - godB.Alignment.PurityAxis);

// Domain opposition (hardcoded pairs)
bool opposingDomains =
    (godA.Domain == Fire && godB.Domain == Water) ||
    (godA.Domain == Life && godB.Domain == Death) ||
    (godA.Domain == Growth && godB.Domain == Decay);

// Relationship determination
if (opposingDomains || alignmentDistance > 200)
    relationship = Rival;
else if (alignmentDistance < 80)
    relationship = Allied;
else
    relationship = Neutral;
```

**Step 4: Pantheon Validation**

Before finalizing, validate pantheon balance:

```csharp
public struct PantheonValidationRules
{
    public byte MinRivalPairs;        // At least 2 rival pairs
    public byte MaxSingleAlignment;   // No more than 40% of gods share alignment quadrant
    public bool RequireNeutralGod;    // At least one god near (0,0,0)
    public bool RequireExtremeGod;    // At least one god with |alignment| > 150
}
```

**Validation Failures**:
- If validation fails, re-roll problem gods or adjust alignment within bounds
- Maximum 10 re-roll attempts before falling back to balanced template

---

## Player-Configurable Settings

### Pre-Generation Configuration Screen

Players configure world generation before pantheon creation:

**UI Mock**:
```
╔═══════════════════════════════════════════════════════════╗
║           WORLD GENERATION SETTINGS                        ║
╠═══════════════════════════════════════════════════════════╣
║                                                           ║
║  World Seed: [Random___________] [🎲 Randomize]          ║
║                                                           ║
║  Pantheon Size: [●●●●●●○○○○○○]  6 Gods                   ║
║                 └─────────────┘                           ║
║                 Min: 6   Max: 12                          ║
║                                                           ║
║  Composition:   ◉ Balanced                                ║
║                 ○ Life-Heavy (Peaceful)                   ║
║                 ○ Destruction-Heavy (Hostile)             ║
║                 ○ Chaotic (Unpredictable)                 ║
║                 ○ Custom Archetype Selection              ║
║                                                           ║
║  Difficulty:    ○ Easy    ◉ Normal    ○ Hard              ║
║  ├─ Miracle Costs:        100%                            ║
║  ├─ Worship Generation:   100%                            ║
║  ├─ Absorption Threshold: 750 (Normal)                    ║
║  └─ Divine Interference:  15% (Occasional)                ║
║                                                           ║
║  Advanced:                                                ║
║  ☑ Guarantee Rival Pairs (Fire/Water, Life/Death)        ║
║  ☑ Guarantee Neutral God                                  ║
║  ☐ All Gods Start as Interloper                           ║
║  ☐ Starting Relations Randomized (-200 to +200)           ║
║  ☐ Extreme Personalities Only                             ║
║                                                           ║
║  [Cancel]                         [Generate World ▶]      ║
╚═══════════════════════════════════════════════════════════╝
```

**Settings Data Structure**:
```csharp
public struct WorldGenerationConfig : IComponentData
{
    public uint WorldSeed;
    public byte PantheonSize;
    public PantheonComposition Composition;
    public DifficultyPreset Difficulty;

    // Difficulty modifiers
    public float MiracleCostMultiplier;       // 0.5 (Easy) to 2.0 (Hard)
    public float WorshipGenerationMultiplier; // 1.5 (Easy) to 0.75 (Hard)
    public int AbsorptionThreshold;           // 500 (Easy), 750 (Normal), 1000 (Hard)
    public float InterferenceChanceMultiplier; // 0.5 (Easy) to 2.0 (Hard)

    // Advanced settings
    public bool GuaranteeRivalPairs;
    public bool GuaranteeNeutralGod;
    public bool AllStartInterloper;
    public bool RandomizeStartingRelations;
    public bool ExtremePersOnlyMode;
}

public enum DifficultyPreset : byte
{
    Easy = 0,    // Lower costs, higher worship, easier absorption
    Normal = 1,  // Balanced
    Hard = 2,    // Higher costs, lower worship, harder absorption
    Custom = 3   // Player sets individual modifiers
}
```

---

## Cross-Game Compatibility

### Godgame: Divine Pantheon

In Godgame, generated entities are **gods** controlling natural phenomena:

```csharp
public struct GodgameGod : IComponentData
{
    public Entity GodEntity;
    public GodDomain Domain;               // Growth, Fire, Time, etc.
    public TriAxisAlignment Alignment;
    public BehaviorTraits Traits;

    // Godgame-specific
    public float MiracleBaseCost;          // Base mana cost for miracles
    public float DivineInterferenceChance; // Chance to interfere per miracle
    public int PlayerRelation;             // -1000 (Hostile) to +1000 (Conjoined)
}

public enum GodDomain : byte
{
    Fire = 0,      // Heat, combustion, energy
    Water = 1,     // Oceans, rain, storms
    Growth = 2,    // Plants, fertility, crops
    Time = 3,      // Day/night, aging, seasons
    Fortune = 4,   // Luck, probability, RNG
    Death = 5,     // Mortality, decay, entropy
    Earth = 6,     // Stone, mountains, terrain
    Wind = 7,      // Air, weather, movement
    Life = 8,      // Healing, vitality, birth
    Storms = 9,    // Lightning, hurricanes, chaos
    Ice = 10,      // Cold, freezing, preservation
    Shadow = 11    // Darkness, stealth, secrets
}
```

**Miracle Framework Integration**:
```csharp
public struct MiracleDefinition : IComponentData
{
    public FixedString64Bytes MiracleId;   // "Firestorm", "Rainstorm", "BumperCrop"
    public Entity RequiredGod;              // Which god grants this miracle
    public float BaseManaaCost;              // Cost from that god's mana pool
    public MiracleCategory Category;
}

public enum MiracleCategory : byte
{
    Creation = 0,    // Create entities, resources
    Destruction = 1, // Damage, remove entities
    Alteration = 2,  // Modify environment, stats
    Divination = 3,  // Reveal information
    Temporal = 4     // Speed/slow time, rewind
}
```

### Space4X: Cosmic Forces

In Space4X, generated entities are **cosmic forces** or **ancient factions**:

```csharp
public struct CosmicForce : IComponentData
{
    public Entity ForceEntity;
    public CosmicDomain Domain;            // Gravity, Entropy, Radiation, etc.
    public TriAxisAlignment Alignment;
    public BehaviorTraits Traits;

    // Space4X-specific
    public float TechnologyAffinityBonus;  // Bonus to related tech research
    public float SectorInfluenceRadius;    // How far their influence extends
    public int FactionRelation;            // -1000 (Hostile) to +1000 (Allied)
}

public enum CosmicDomain : byte
{
    Gravity = 0,      // Mass, orbits, black holes
    Entropy = 1,      // Decay, heat death, chaos
    Radiation = 2,    // Energy, emissions, stars
    Quantum = 3,      // Probability, superposition
    Magnetism = 4,    // Fields, shields, propulsion
    Void = 5,         // Empty space, vacuum
    Nebula = 6,       // Gas, dust, stellar nurseries
    Singularity = 7,  // Black holes, wormholes
    Expansion = 8,    // Dark energy, universe growth
    Fusion = 9,       // Stars, reactors, energy
    DarkMatter = 10,  // Invisible mass, halos
    Temporal = 11     // Time dilation, relativity
}
```

**Technology Affinity Example**:
```
Player researches "Fusion Reactor" technology
→ Fusion Force has +25% research speed bonus
→ Entropy Force has -15% research speed penalty (opposes order)
→ Radiation Force has +10% bonus (complementary domain)
```

**Alliance/Conflict Mechanics**:
- Similar to Godgame worship economy, but uses **research points** and **sector control**
- Building research stations in Fusion-aligned sectors grants favor
- Favor unlocks cosmic artifacts (Space4X equivalent of miracles)
- Absorption grants passive bonuses (e.g., Fusion Force → +50% reactor efficiency)

---

## Technical Implementation

### Blob Assets for Archetypes

Archetypes stored as **BlobAssets** for efficient runtime access:

```csharp
public struct GodArchetypeBlobAsset
{
    public BlobArray<GodArchetypeData> Archetypes;
    public BlobArray<RivalPairDefinition> GuaranteedRivals;
    public BlobArray<FixedString64Bytes> NamePrefixes;
    public BlobArray<FixedString64Bytes> NameSuffixes;
}

public struct GodArchetypeData
{
    public FixedString64Bytes ArchetypeId;
    public GodDomain Domain;
    public TriAxisAlignmentBounds AlignmentBounds;
    public BehaviorTraitWeights TraitWeights;
    public BlobArray<FixedString64Bytes> PossibleTitles; // "the Inferno", "the Lifegiver"
}

public struct RivalPairDefinition
{
    public FixedString64Bytes Archetype1; // "Fire"
    public FixedString64Bytes Archetype2; // "Water"
    public InterferenceChance DefaultInterference;
}
```

**Blob Asset Creation (Authoring)**:
```csharp
public class GodArchetypeAuthoring : MonoBehaviour
{
    public GodArchetypeConfig[] Archetypes;
    public RivalPairConfig[] RivalPairs;

    class Baker : Baker<GodArchetypeAuthoring>
    {
        public override void Bake(GodArchetypeAuthoring authoring)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<GodArchetypeBlobAsset>();

            // Build archetypes array
            var archetypesBuilder = builder.Allocate(ref root.Archetypes, authoring.Archetypes.Length);
            for (int i = 0; i < authoring.Archetypes.Length; i++)
            {
                archetypesBuilder[i] = authoring.Archetypes[i].ToBlobData(ref builder);
            }

            // Build rival pairs
            var rivalsBuilder = builder.Allocate(ref root.GuaranteedRivals, authoring.RivalPairs.Length);
            for (int i = 0; i < authoring.RivalPairs.Length; i++)
            {
                rivalsBuilder[i] = authoring.RivalPairs[i].ToRivalPair();
            }

            var blobAsset = builder.CreateBlobAssetReference<GodArchetypeBlobAsset>(Allocator.Persistent);
            AddBlobAsset(ref blobAsset, out var hash);

            builder.Dispose();
        }
    }
}
```

### Deterministic Generation System

**Burst-Compiled Generation Job**:
```csharp
[BurstCompile]
public partial struct GeneratePantheonJob : IJob
{
    public uint WorldSeed;
    public WorldGenerationConfig Config;
    [ReadOnly] public BlobAssetReference<GodArchetypeBlobAsset> ArchetypesBlob;

    public EntityCommandBuffer ECB;

    public void Execute()
    {
        var random = new Unity.Mathematics.Random(WorldSeed);

        // Step 1: Select archetypes based on composition
        var selectedArchetypes = SelectArchetypes(ref random, Config, ArchetypesBlob);

        // Step 2: Generate gods from archetypes
        var generatedGods = new NativeList<GeneratedGod>(Config.PantheonSize, Allocator.Temp);
        for (int i = 0; i < selectedArchetypes.Length; i++)
        {
            var god = GenerateGodFromArchetype(ref random, selectedArchetypes[i], ArchetypesBlob);
            generatedGods.Add(god);
        }

        // Step 3: Generate relationships
        var relationships = GenerateRelationships(ref random, generatedGods, ArchetypesBlob);

        // Step 4: Validate pantheon
        bool isValid = ValidatePantheon(generatedGods, relationships, Config);
        if (!isValid)
        {
            // Re-roll problematic gods (max 10 attempts)
            // Implementation omitted for brevity
        }

        // Step 5: Spawn entities
        for (int i = 0; i < generatedGods.Length; i++)
        {
            SpawnGodEntity(generatedGods[i], relationships, ECB);
        }

        generatedGods.Dispose();
    }

    private GeneratedGod GenerateGodFromArchetype(
        ref Unity.Mathematics.Random random,
        GodArchetypeData archetype,
        BlobAssetReference<GodArchetypeBlobAsset> blob)
    {
        var god = new GeneratedGod();
        god.ArchetypeId = archetype.ArchetypeId;

        // Roll alignment within bounds
        god.Alignment.MoralAxis = (sbyte)random.NextInt(
            archetype.AlignmentBounds.MoralRange.x,
            archetype.AlignmentBounds.MoralRange.y
        );
        god.Alignment.OrderAxis = (sbyte)random.NextInt(
            archetype.AlignmentBounds.OrderRange.x,
            archetype.AlignmentBounds.OrderRange.y
        );
        god.Alignment.PurityAxis = (sbyte)random.NextInt(
            archetype.AlignmentBounds.PurityRange.x,
            archetype.AlignmentBounds.PurityRange.y
        );

        // Roll traits from weights
        god.Traits.VengefulScore = (byte)(archetype.TraitWeights.VengefulWeight * 100f * random.NextFloat(0.7f, 1.3f));
        god.Traits.BoldScore = (byte)(archetype.TraitWeights.BoldWeight * 100f * random.NextFloat(0.7f, 1.3f));
        // ... other traits

        // Generate name
        god.GodName = GenerateGodName(ref random, archetype, blob);
        god.VisualSeed = random.NextUInt();

        return god;
    }
}
```

### Rewind Compatibility

World generation creates deterministic initial state:

```csharp
public struct WorldGenerationSnapshot : IComponentData
{
    public uint OriginalSeed;
    public uint CurrentGenerationIteration; // Incremented on rewind-regenerate
    public bool IsRegenerated;
}

// On rewind to before generation:
// - If rewinding to tick 0 (before generation), re-run generation with same seed
// - Gods will be identical due to deterministic randomization
// - Player actions after generation still rewindable normally
```

---

## Integration with Existing Systems

### Forces System Integration

Generated gods control **forces** in the environment:

```csharp
public struct GodControlledForce : IComponentData
{
    public Entity ControllingGod;      // Which god controls this force
    public ForceType Type;             // Gravity, Wind, Growth, etc.
    public float BaseStrength;         // Default force strength
    public float GodModifier;          // Multiplier based on god's mood/relation
}

// Example: Wind god is angry (player relation -300)
// → Wind forces are 150% stronger than normal
// → Sailing becomes dangerous, buildings take damage
```

**Player Miracle → Force Modification**:
```
Player casts "Calm Winds" miracle (costs 200 Aeolus mana)
→ Creates temporary WindForceModifier component
→ Wind forces reduced by 80% for 60 seconds
→ Aeolus relation: -5 (didn't like being overridden)
```

### Reactions System Integration

God personalities affect **reaction thresholds**:

```csharp
public struct GodReactionProfile : IComponentData
{
    public Entity GodEntity;
    public float SlightThreshold;      // Relation loss to trigger negative reaction
    public float RageThreshold;        // Relation loss to trigger divine wrath
    public float PleasedThreshold;     // Relation gain to trigger blessing

    // Derived from BehaviorTraits
    // Vengeful gods: lower thresholds (react quickly)
    // Forgiving gods: higher thresholds (patient)
}

// Example: Vengeful fire god (VengefulScore=85)
SlightThreshold = 20 (reacts after -20 relation)
RageThreshold = 100 (divine wrath at -100 relation)

// Example: Forgiving water god (VengefulScore=30)
SlightThreshold = 80 (patient, takes more to anger)
RageThreshold = 300 (very hard to enrage)
```

### Worship System Integration

Pantheon size affects **worship point distribution**:

```csharp
public struct WorshipEconomy : IComponentData
{
    public float TotalWorshipGeneration;  // Base: 100 worship/sec
    public byte ActiveGodCount;           // How many gods need worship
    public float AutoBalanceRate;         // How evenly to distribute
}

// With 6 gods: Each temple can meaningfully contribute
// With 12 gods: Worship spread thin, must prioritize

// Strategic depth: More gods = harder to maintain all relations
```

### Temple Generation

Temples spawn based on pantheon composition:

```csharp
// During world generation:
// - Each god gets 1-3 starting temples placed in world
// - Temple positions based on biome affinity (Fire god → volcanic regions)
// - Temple density affects starting worship distribution
```

---

## Space4X-Specific: Galactic Generation

### Sector Affinity

Cosmic forces have **sector affinity** - certain regions aligned to specific forces:

```csharp
public struct SectorAffinity : IComponentData
{
    public Entity DominantForce;       // Primary cosmic force in this sector
    public float AffinityStrength;     // 0.0 to 1.0
    public Entity SecondaryForce;      // Optional secondary influence
}

// Example: Nebula sector
// → Nebula Force (primary, 0.8 strength)
// → Radiation Force (secondary, 0.4 strength)
// → Building research stations here grants favor with both
```

**Generation Algorithm**:
```
1. Generate galaxy map (sectors, stars, planets)
2. Assign dominant force to each sector based on natural features
   - Nebula regions → Nebula Force
   - Black hole systems → Singularity Force
   - Star clusters → Radiation Force
3. Add secondary forces for overlapping influence zones
4. Create force-aligned anomalies and artifacts
```

### Ancient Artifact Placement

Artifacts are Space4X equivalent of temples:

```csharp
public struct CosmicArtifact : IComponentData
{
    public Entity AlignedForce;        // Which force this artifact honors
    public ArtifactPower Power;        // What it does when activated
    public float ActivationCost;       // Research points or energy
    public int RelationRequirement;    // Minimum favor needed
}

// Example artifacts:
// - Fusion Core Fragment → +50% reactor output (Fusion Force aligned)
// - Entropy Containment Field → Shields negate 30% damage (Entropy Force aligned)
// - Gravitational Lens Array → FTL travel speed +25% (Gravity Force aligned)
```

---

## Example Generation Flow

### Godgame Playthrough

**Step 1: Player Configuration**
```
Seed: "MyFirstWorld"
Pantheon Size: 8 gods
Composition: Balanced
Difficulty: Normal
Guarantee Rival Pairs: Yes
```

**Step 2: Archetype Selection**
```
Selected Archetypes:
1. Fire
2. Water (rival to Fire)
3. Life
4. Death (rival to Life)
5. Growth
6. Time
7. Fortune
8. Earth
```

**Step 3: God Generation**
```
1. Pyranthos (Fire)
   Alignment: (-70, +30, +50) → Evil, Lawful, Pure
   Traits: Vengeful=80, Bold=85, Greedy=50, Isolationist=25
   Title: "Pyranthos the Disciplined Inferno"

2. Aquilas (Water)
   Alignment: (+40, -60, +80) → Good, Chaotic, Natural
   Traits: Vengeful=25, Bold=40, Greedy=30, Isolationist=50
   Title: "Aquilas the Untamed Tide"

3. Vitara (Life)
   Alignment: (+85, +20, +95) → Good, Neutral-Lawful, Natural
   Traits: Vengeful=15, Bold=30, Greedy=10, Isolationist=20
   Title: "Vitara the Gentle Healer"

4. Mortheus (Death)
   Alignment: (-40, +70, -80) → Evil, Lawful, Corrupted
   Traits: Vengeful=70, Bold=50, Greedy=60, Isolationist=80
   Title: "Mortheus the Inevitability"

... and so on for 8 gods
```

**Step 4: Relationship Generation**
```
Pyranthos ↔ Aquilas: RIVALS (opposing domains, alignment distance: 240)
  → Interference Chance: Frequent (30%)
  → Starting Relation to Player: Both -50 (Interloper)

Vitara ↔ Mortheus: RIVALS (opposing domains, alignment distance: 280)
  → Interference Chance: Constant (60%)
  → Starting Relation: Both -50

Pyranthos ↔ Vitara: NEUTRAL (alignment distance: 155)
  → Interference Chance: Rare (5%)

Growth ↔ Vitara: ALLIED (similar alignments, complementary domains)
  → Shared miracles possible
  → Absorbing one grants +100 relation with the other
```

**Step 5: World Spawn**
```
8 gods created as entities with:
- GodComponent (alignment, traits, domain)
- GodManaBalance buffer (8 entries, one per god)
- GodRelationshipTemplate buffer (relationships to other gods)
- PlayerGodRelation (starts at -50 for all, "Interloper" status)

3 temples per god spawned in world (24 temples total)
- Fire temples near volcanoes
- Water temples near oceans
- Life temples in forests
- etc.
```

### Space4X Playthrough

**Step 1: Player Configuration**
```
Seed: "Galaxy42"
Force Count: 10 cosmic forces
Composition: Balanced
Difficulty: Hard
Guarantee Rival Pairs: Yes
```

**Step 2: Galaxy Generation**
```
1. Generate 200 sectors
2. Assign dominant forces based on sector type:
   - 35 sectors → Radiation Force (star-heavy regions)
   - 28 sectors → Nebula Force (gas clouds)
   - 22 sectors → Gravity Force (dense stellar clusters)
   - 18 sectors → Void Force (empty space)
   - 15 sectors → Singularity Force (black hole systems)
   ... etc.
```

**Step 3: Artifact Placement**
```
50 ancient artifacts scattered across galaxy:
- 5 Fusion artifacts in Radiation-aligned sectors
- 5 Entropy artifacts in Void-aligned sectors
- 5 Quantum artifacts in anomaly zones
... etc.

Finding and activating artifacts grants favor with aligned force
```

**Step 4: Starting Relations**
```
Player starts with 0 relation to all forces (Unknown)
Research in aligned sectors → +1 relation per turn
Activate artifacts → +50 relation
Opposing force actions → -25 relation (e.g., Entropy research angers Fusion)
```

---

## Performance Considerations

### Generation Performance

- **Generation Time Budget**: 100ms for pantheon generation (acceptable loading time)
- **Blob Asset Loading**: <1ms (pre-baked, loaded from disk)
- **Burst Compilation**: All generation jobs Burst-compiled for SIMD

**Profiling Targets**:
```
Archetype Selection:     <5ms   (simple array lookup)
God Randomization:       <10ms  (8-12 gods × 1ms each)
Relationship Generation: <20ms  (pairwise comparisons)
Validation:              <15ms  (max 10 re-rolls)
Entity Spawning:         <50ms  (ECB playback)
──────────────────────────────────
Total:                   <100ms
```

### Runtime Performance

- **God Components**: Minimal overhead (<1KB per god)
- **Relationship Queries**: O(n²) but n is small (6-12 gods)
- **Interference Checks**: Only on miracle cast (infrequent)

---

## Future Expansion

### Modding Support

Expose archetype creation to modders:

```json
{
  "archetypeId": "ModdedTechGod",
  "domain": "Technology",
  "alignmentBounds": {
    "moral": [0, 100],
    "order": [60, 100],
    "purity": [-80, 0]
  },
  "traitWeights": {
    "vengeful": 0.3,
    "bold": 0.8,
    "greedy": 0.9,
    "isolationist": 0.4
  },
  "possibleTitles": [
    "the Innovator",
    "the Machinist",
    "the Gear-Touched"
  ]
}
```

### Pantheon Evolution

Gods evolve over playthrough:

```csharp
public struct GodEvolution : IComponentData
{
    public int RelationMilestone;      // Track relation changes
    public uint MiraclesCast;          // How many times player used this god
    public bool HasEvolvedOnce;        // Unlock new miracles at milestone
}

// Example: Fire god absorbed (Conjoined)
// → Unlocks "Solar Flare" miracle (ultimate fire power)
// → God's personality becomes more aligned with player's actions
```

### Cross-Pantheon Events

Gods from different playthroughs can **echo** in new worlds:

```csharp
public struct LegacyGodEcho : IComponentData
{
    public uint OriginalWorldSeed;     // Which playthrough this god came from
    public FixedString64Bytes LegacyName;
    public int LegacyRelation;         // How player treated them before
}

// Player who absorbed Pyranthos in previous playthrough
// → Next playthrough's Fire god starts at +100 relation
// → "The Inferno remembers your pact..."
```

---

## Summary

The Procedural World Generation System provides:

1. **Replayability**: Unique pantheons/forces every playthrough
2. **Player Agency**: Pre-generation settings for customization
3. **Technical Grounding**: Burst-compatible, deterministic, rewind-friendly
4. **Cross-Game Compatibility**: Same framework for Godgame and Space4X
5. **Integration**: Seamlessly connects to forces, reactions, worship, and tooltips
6. **Scalability**: Efficient generation (<100ms), minimal runtime overhead
7. **Narrative Depth**: Procedural personalities create emergent stories

**Key Innovation**: Gods/forces are not static - they're procedurally generated characters with unique alignments, rivalries, and behaviors that ensure no two playthroughs are identical while maintaining game balance and technical performance.
