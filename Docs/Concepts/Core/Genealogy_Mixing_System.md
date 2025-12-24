# Genealogy Mixing System

**Status:** Concept
**Category:** Core - Biological Simulation, Entity Creation
**Scope:** Individual Entity Creation → Population → Species
**Created:** 2025-12-21
**Last Updated:** 2025-12-21

---

## Purpose

**Primary Goal:** Enable flexible, intuitive mixing of genealogies to create any combination of hybrid entities (cat people, dragonkin, dryads, mushroom men, beastmen, yetis, sasquatches, etc.) by combining existing genetic lineages in the world.

**Secondary Goals:**
- Treat genealogies as composable building blocks (mix and match)
- Support both pure genealogies (single lineage) and mixed genealogies (multiple lineages)
- Maintain flexibility over semantics (names are descriptive, not restrictive)
- Integrate with blank-by-default entity philosophy (entities are blank, genealogies add traits)
- Enable emergent hybrid species through natural reproduction and intentional crafting

---

## System Overview

### Key Insight

**Genealogies are composable genetic building blocks, not fixed categories.**

- **Pure Genealogies:** Single genetic lineage (e.g., Human, Dragon, Cat, Tree, Mushroom)
- **Mixed Genealogies:** Combinations of multiple genealogies (e.g., Human + Cat = Cat Person, Dragon + Human = Dragonkin)
- **Composition:** Genealogies can be mixed in any combination, any ratio
- **Flexibility:** "Dragonkin" can be a pure genealogy OR a mixed genealogy (Human + Dragon) - semantics are flexible
- **Trait Inheritance:** Each genealogy contributes traits to the entity (physical, biological, magical, behavioral)

**Example Emergence:**
- Player combines Human + Cat genealogies (50/50) → creates Cat Person
- Player combines Dragon + Human genealogies (70/30) → creates Dragonkin (more dragon-like)
- Player combines Tree + Human genealogies (60/40) → creates Dryad (more plant-like)
- Natural reproduction between mixed genealogies → creates new hybrid combinations
- Multiple generations → genealogies stabilize into recognized hybrid species

### Components

1. **Genealogy Definitions:** Genetic lineage templates (pure and mixed)
2. **Genealogy Composition:** Mixing system (ratios, combinations, trait inheritance)
3. **Trait Inheritance:** How genealogies contribute traits to entities
4. **Hybrid Creation:** Intentional and natural creation of mixed-genealogy entities
5. **Genealogy Stabilization:** Mixed genealogies can stabilize into recognized species

---

## Genealogy System

### Genealogy as Building Block

**Genealogies are genetic lineages** that define biological, physical, and behavioral traits.

**Pure Genealogy:** Single genetic source
- **Examples:** Human, Dragon, Cat, Tree, Mushroom, Ethereal Being, Yeti, Sasquatch
- **Characteristics:** Single set of traits, no mixing required
- **Use:** Base building block for mixing

**Mixed Genealogy:** Combination of multiple genealogies
- **Examples:** Human + Cat, Dragon + Human, Tree + Human, Mushroom + Human
- **Characteristics:** Traits from multiple sources, blended together
- **Use:** Hybrid entities, custom combinations

---

### Genealogy Structure

**Each genealogy defines:**

**1. Physical Traits**
- **Body Shape:** Humanoid, quadrupedal, serpentine, tree-like, fungal, etc.
- **Size:** Typical size range (small, medium, large, massive)
- **Appendages:** Arms, legs, wings, tails, roots, etc.
- **Covering:** Skin, fur, scales, bark, chitin, feathers, etc.
- **Features:** Eyes, ears, horns, claws, leaves, caps, etc.

**2. Biological Traits**
- **Metabolism:** Fast, normal, slow, photosynthetic, decomposer, etc.
- **Lifespan:** Short, normal, long, very long, immortal
- **Resistances:** Cold, heat, poison, disease, etc.
- **Diet:** Carnivore, herbivore, omnivore, photosynthetic, decomposer, etc.
- **Reproduction:** Sexual, asexual, spores, seeds, etc.

**3. Magical/Paranormal Traits**
- **Magic Affinity:** Fire, water, nature, shadow, etc.
- **Supernatural Abilities:** Flight, phasing, regeneration, etc.
- **Resistances:** Magic resistance, immunity, vulnerability
- **Connection:** Nature connection, spirit connection, elemental connection

**4. Behavioral Traits**
- **Intelligence:** Typical intelligence level
- **Sociability:** Solitary, pack, herd, hive, etc.
- **Aggression:** Peaceful, neutral, aggressive
- **Movement:** Walk, run, climb, swim, fly, teleport, etc.

**5. Cultural/Historical Traits**
- **Associated Cultures:** Cultures typically associated with this genealogy
- **Historical Context:** Origins, migrations, interactions
- **Rarity:** Common, uncommon, rare, legendary

---

## Genealogy Mixing

### Composition Model

**Genealogies are mixed using a composition model** where each genealogy contributes traits proportionally.

**Composition Representation:**
```
Entity Genealogy = {
    Genealogy A: 50% (0.5)
    Genealogy B: 30% (0.3)
    Genealogy C: 20% (0.2)
}

Total = 100% (1.0)
```

**Examples:**
```
Cat Person:
    Human: 50%
    Cat: 50%

Dragonkin (more dragon-like):
    Dragon: 70%
    Human: 30%

Dryad (more plant-like):
    Tree: 60%
    Human: 40%

Mushroom Man (balanced):
    Mushroom: 50%
    Human: 50%

Yeti (mostly yeti):
    Yeti: 80%
    Human: 20%

Ethereal Being (pure):
    Ethereal: 100%
```

---

### Trait Inheritance

**Each genealogy contributes traits** based on its composition percentage.

**1. Dominant Trait Inheritance**

**For traits where one genealogy dominates:**
- **Majority Rule:** Genealogy with highest percentage determines trait
- **Threshold:** Genealogy must have >50% to dominate trait
- **Blend:** If no genealogy >50%, traits blend

**Example:**
```
Body Shape:
    Dragon (70%) + Human (30%) → Dragon-like body (Dragon dominates)
    
Cat Person:
    Human (50%) + Cat (50%) → Blended humanoid-cat body (no clear dominance)
```

---

**2. Blended Trait Inheritance**

**For traits where genealogies blend:**
- **Weighted Average:** Traits blend proportionally to genealogy percentages
- **Smooth Transition:** Gradual blending between genealogies
- **Minimum Threshold:** Small genealogies (<10%) may not contribute visibly

**Example:**
```
Size:
    Dragon (70%) = Large
    Human (30%) = Medium
    Result: Large-Medium (weighted average)

Color:
    Cat (50%) = Orange fur
    Human (50%) = Skin tone
    Result: Blended (human skin with cat-like patterns/features)
```

---

**3. Hybrid-Specific Traits**

**Some traits only appear when genealogies are mixed:**
- **Synergy Traits:** New traits emerge from combination (e.g., Human + Dragon = breath weapon capability)
- **Unique Features:** Combinations create unique features not found in pure genealogies
- **Enhanced Traits:** Some traits are enhanced in hybrids (e.g., Human + Cat = enhanced agility)

**Example:**
```
Human + Dragon:
    Base traits: Human body shape (50%), Dragon scales (50%)
    Hybrid trait: Breath weapon capability (emerges from combination)
    
Tree + Human:
    Base traits: Plant features (60%), Humanoid shape (40%)
    Hybrid trait: Nature magic affinity (emerges from combination)
```

---

### Mixing Rules

**1. Minimum Contribution**
- **Threshold:** Genealogies must contribute ≥5% to have visible effect
- **Below Threshold:** Genealogies <5% may contribute subtle traits or be ignored
- **Cumulative:** Multiple small contributions can combine (e.g., 3% + 3% + 4% = 10% effect)

**2. Maximum Genealogies**
- **Practical Limit:** 3-5 genealogies per entity (complexity management)
- **Theoretical Limit:** Unlimited (but diminishing returns on trait expression)
- **Blending:** Too many genealogies → traits become indistinct/blended

**3. Genealogy Compatibility**
- **No Hard Restrictions:** Any genealogies can be mixed
- **Biological Compatibility:** Some combinations may be unusual (e.g., Tree + Mushroom)
- **Magical Compatibility:** Some combinations may be synergistic (e.g., Dragon + Fire Elemental)
- **Player Choice:** Players can mix any genealogies they want

---

## Genealogy Examples

### Pure Genealogies

**1. Human**
- **Physical:** Bipedal humanoid, medium size, skin, hair
- **Biological:** Normal metabolism, normal lifespan, omnivore
- **Magical:** Variable (no inherent magic)
- **Behavioral:** Social, intelligent, adaptable

**2. Dragon**
- **Physical:** Large quadrupedal/serpentine, scales, wings, horns
- **Biological:** Slow metabolism, very long lifespan, carnivore
- **Magical:** Fire/breath weapon, flight, magic resistance
- **Behavioral:** Solitary/territorial, highly intelligent, aggressive when threatened

**3. Cat**
- **Physical:** Quadrupedal, medium size, fur, claws, tail
- **Biological:** Fast metabolism, normal lifespan, carnivore
- **Magical:** None (or minor agility enhancement)
- **Behavioral:** Solitary/pack, intelligent, agile

**4. Tree (Plant)**
- **Physical:** Stationary, large size, bark, roots, leaves
- **Biological:** Photosynthetic, very long lifespan, autotroph
- **Magical:** Nature magic, regeneration, environmental connection
- **Behavioral:** Stationary, sentient (if awakened), patient

**5. Mushroom**
- **Physical:** Stationary/mobile, variable size, cap, gills, mycelium
- **Biological:** Decomposer, normal-long lifespan, heterotroph
- **Magical:** Spore abilities, network connection, decay magic
- **Behavioral:** Network-connected, patient, adaptive

**6. Ethereal Being**
- **Physical:** Incorporeal/phaseable, variable size, energy-based
- **Biological:** No metabolism, immortal, no diet
- **Magical:** Phasing, energy manipulation, spirit connection
- **Behavioral:** Solitary, highly intelligent, mysterious

**7. Yeti**
- **Physical:** Large bipedal, fur, claws, powerful build
- **Biological:** Cold-adapted, long lifespan, omnivore
- **Magical:** Cold resistance, enhanced strength
- **Behavioral:** Solitary/territorial, intelligent, aggressive when threatened

**8. Sasquatch**
- **Physical:** Large bipedal, fur, human-like features
- **Biological:** Forest-adapted, long lifespan, omnivore
- **Magical:** Nature connection, stealth abilities
- **Behavioral:** Solitary, intelligent, elusive

---

### Mixed Genealogies (Examples)

**1. Cat Person (Human + Cat)**
```
Composition: Human 50% + Cat 50%
Physical: Humanoid with cat features (ears, tail, fur patches, claws)
Biological: Blended metabolism, normal lifespan, omnivore
Magical: Enhanced agility, minor cat instincts
Behavioral: Social (human) + agile (cat), intelligent
```

**2. Dragonkin (Dragon + Human)**
```
Composition: Dragon 70% + Human 30%
Physical: Humanoid with dragon features (scales, wings, horns, tail)
Biological: Blended metabolism, long lifespan, omnivore
Magical: Breath weapon, flight, magic resistance, enhanced strength
Behavioral: Social (human) + territorial (dragon), highly intelligent
```

**3. Dryad (Tree + Human)**
```
Composition: Tree 60% + Human 40%
Physical: Humanoid with plant features (bark skin, leaves, roots, wood-like structure)
Biological: Photosynthetic + normal metabolism, very long lifespan, autotroph
Magical: Nature magic, regeneration, environmental connection
Behavioral: Social (human) + patient (tree), nature-connected
```

**4. Mushroom Man (Mushroom + Human)**
```
Composition: Mushroom 50% + Human 50%
Physical: Humanoid with fungal features (cap, gills, mycelium connections)
Biological: Decomposer + normal metabolism, long lifespan, heterotroph
Magical: Spore abilities, network connection, decay magic
Behavioral: Social (human) + network-connected (mushroom), patient
```

**5. Yeti-Human Hybrid (Yeti + Human)**
```
Composition: Yeti 80% + Human 20%
Physical: Large humanoid with yeti features (fur, claws, powerful build, human-like face)
Biological: Cold-adapted + normal metabolism, long lifespan, omnivore
Magical: Cold resistance, enhanced strength, minor human adaptability
Behavioral: Solitary/territorial (yeti) + social (human), intelligent
```

**6. Ethereal Human (Ethereal + Human)**
```
Composition: Ethereal 60% + Human 40%
Physical: Humanoid with ethereal features (phaseable, energy-based, semi-corporeal)
Biological: Minimal metabolism, very long lifespan, no diet
Magical: Phasing, energy manipulation, spirit connection, human magic adaptability
Behavioral: Social (human) + mysterious (ethereal), highly intelligent
```

---

## Entity Creation with Genealogies

### Blank-by-Default Philosophy

**Entities are blank by default** - genealogies add traits, not replace blank state.

**Creation Process:**
1. **Start Blank:** Entity created with no traits (blank slate)
2. **Apply Genealogy:** Genealogy composition applied to entity
3. **Add Traits:** Genealogy traits added to entity (physical, biological, magical, behavioral)
4. **Customization:** Additional customization can be applied (player crafting, natural variation)

**Integration:**
- **Entity Profile:** Genealogy is part of entity profile, not entity type
- **Modular Traits:** Traits from genealogies are modular (can be added/removed)
- **Flexibility:** Same entity can have different genealogy compositions

---

### Intentional Creation (Player Crafting)

**Players can intentionally create entities** with specific genealogy compositions.

**Process:**
1. **Select Genealogies:** Player selects genealogies to mix
2. **Set Ratios:** Player sets composition percentages (must sum to 100%)
3. **Preview Traits:** System previews resulting traits
4. **Create Entity:** Entity created with specified genealogy composition
5. **Variation:** Natural variation applied (traits may vary slightly)

**Example:**
```
Player Action:
    1. Select: Human + Cat
    2. Set: Human 50% + Cat 50%
    3. Preview: Cat Person (humanoid with cat features)
    4. Create: Entity spawned with Cat Person genealogy
    5. Variation: Slight variations in fur color, ear shape, tail length
```

---

### Natural Creation (Reproduction)

**Entities naturally reproduce** - offspring inherit genealogy compositions from parents.

**Reproduction Process:**
1. **Parent Genealogies:** Both parents have genealogy compositions
2. **Inheritance:** Offspring inherits genealogies from both parents
3. **Blending:** Parent genealogies blend in offspring (averaged or recombined)
4. **Mutation:** Small chance of new genealogies or mutations
5. **Stabilization:** Over generations, mixed genealogies may stabilize

**Inheritance Rules:**

**A. Averaged Composition**
```
Parent 1: Human 100%
Parent 2: Cat 100%
Offspring: Human 50% + Cat 50%
```

**B. Recombined Composition**
```
Parent 1: Human 50% + Cat 50%
Parent 2: Human 60% + Cat 40%
Offspring: Human 55% + Cat 45% (averaged)
```

**C. Multiple Genealogies**
```
Parent 1: Dragon 70% + Human 30%
Parent 2: Cat 100%
Offspring: Dragon 35% + Human 40% + Cat 25% (recombined)
```

**D. Mutation**
```
Parent 1: Human 100%
Parent 2: Cat 100%
Offspring: Human 48% + Cat 48% + NewGenealogy 4% (mutation)
```

---

## Genealogy Stabilization

### Stabilization Process

**Mixed genealogies can stabilize** into recognized hybrid species over generations.

**Stabilization Conditions:**
1. **Population Size:** Large enough population (50+ entities)
2. **Generations:** Multiple generations (5-10 generations)
3. **Stable Composition:** Genealogy composition stabilizes (little variation)
4. **Recognition:** Entities/players recognize as distinct species

**Stabilization Result:**
- **New Pure Genealogy:** Stabilized hybrid becomes recognized as pure genealogy
- **Example:** Cat Person (Human + Cat) stabilizes → becomes "Catfolk" (pure genealogy)
- **Flexibility:** "Catfolk" can still be created as Human + Cat OR as pure Catfolk genealogy

**Example:**
```
Generation 1: Human 50% + Cat 50% (individuals vary)
Generation 2: Human 48-52% + Cat 48-52% (stabilizing)
Generation 3: Human 49-51% + Cat 49-51% (more stable)
...
Generation 10: Human 50% + Cat 50% (stabilized)
Result: "Catfolk" recognized as pure genealogy (can create as Human + Cat OR pure Catfolk)
```

---

## Component Structure

### Genealogy Components

```csharp
// Genealogy definition (blob asset)
public struct GenealogyDefinition
{
    public FixedString64Bytes Id;           // "Human", "Dragon", "Cat", etc.
    public FixedString64Bytes DisplayName;  // Display name
    public GenealogyType Type;              // Pure, Mixed, Stabilized
    
    // Trait contributions
    public PhysicalTraitsBlob PhysicalTraits;
    public BiologicalTraitsBlob BiologicalTraits;
    public MagicalTraitsBlob MagicalTraits;
    public BehavioralTraitsBlob BehavioralTraits;
    public CulturalTraitsBlob CulturalTraits;
}

// Entity genealogy composition
public struct EntityGenealogy : IComponentData
{
    public BlobArray<GenealogyContribution> Contributions; // List of genealogies and percentages
    
    // Quick access helpers
    public float GetContribution(FixedString64Bytes genealogyId);
    public bool IsPure(); // True if single genealogy at 100%
    public bool IsMixed(); // True if multiple genealogies
}

// Genealogy contribution (single genealogy in composition)
public struct GenealogyContribution
{
    public BlobAssetReference<GenealogyDefinition> Genealogy;
    public float Percentage; // 0.0-1.0 (must sum to 1.0 for entity)
}

// Trait inheritance tracking
public struct InheritedTraits : IComponentData
{
    public BlobAssetReference<PhysicalTraitsBlob> PhysicalTraits;
    public BlobAssetReference<BiologicalTraitsBlob> BiologicalTraits;
    public BlobAssetReference<MagicalTraitsBlob> MagicalTraits;
    public BlobAssetReference<BehavioralTraitsBlob> BehavioralTraits;
}
```

---

### Trait Blob Structures

```csharp
// Physical traits blob
public struct PhysicalTraitsBlob
{
    public BodyShape Shape;              // Humanoid, Quadrupedal, etc.
    public SizeRange Size;               // Small, Medium, Large, etc.
    public AppendageFlags Appendages;    // Arms, Legs, Wings, etc.
    public CoveringType Covering;        // Skin, Fur, Scales, etc.
    public FeatureFlags Features;        // Eyes, Ears, Horns, etc.
}

// Biological traits blob
public struct BiologicalTraitsBlob
{
    public MetabolismType Metabolism;    // Fast, Normal, Slow, etc.
    public LifespanRange Lifespan;       // Short, Normal, Long, etc.
    public ResistanceFlags Resistances;  // Cold, Heat, Poison, etc.
    public DietType Diet;                // Carnivore, Herbivore, etc.
    public ReproductionType Reproduction; // Sexual, Asexual, etc.
}

// Magical traits blob
public struct MagicalTraitsBlob
{
    public MagicAffinityFlags Affinities; // Fire, Water, Nature, etc.
    public SupernaturalAbilityFlags Abilities; // Flight, Phasing, etc.
    public ResistanceFlags MagicResistances;   // Magic resistance, etc.
    public ConnectionFlags Connections;   // Nature, Spirit, Elemental
}

// Behavioral traits blob
public struct BehavioralTraitsBlob
{
    public IntelligenceLevel Intelligence; // Low, Normal, High, etc.
    public SociabilityType Sociability;   // Solitary, Pack, Herd, etc.
    public AggressionLevel Aggression;    // Peaceful, Neutral, Aggressive
    public MovementCapabilityFlags Movement; // Walk, Run, Fly, etc.
}
```

---

## System Dynamics

### Inputs
- Player actions (intentional entity creation with genealogy mixing)
- Natural reproduction (genealogy inheritance from parents)
- Genealogy definitions (new genealogies discovered/created)
- Stabilization processes (mixed genealogies becoming recognized species)

### Internal Processes
1. **Genealogy Mixing:** Combine genealogies into compositions (ratios, trait inheritance)
2. **Trait Calculation:** Calculate inherited traits from genealogy composition
3. **Reproduction Inheritance:** Offspring inherit genealogies from parents (averaged/recombined)
4. **Stabilization:** Mixed genealogies stabilize into recognized species over generations
5. **Trait Application:** Apply inherited traits to entities (physical, biological, magical, behavioral)

### Outputs
- Entities with genealogy compositions (pure or mixed)
- Hybrid species (cat people, dragonkin, dryads, etc.)
- Stabilized genealogies (recognized hybrid species)
- Trait inheritance (physical, biological, magical, behavioral traits from genealogies)

---

## Balancing Considerations

### Complexity Management

**Genealogy Composition Limits:**
- **Practical Limit:** 3-5 genealogies per entity (manageable complexity)
- **Theoretical Limit:** Unlimited (but diminishing trait expression)
- **Blending:** Too many genealogies → traits become indistinct

**Trait Expression:**
- **Minimum Threshold:** Genealogies must contribute ≥5% to have visible effect
- **Diminishing Returns:** Additional genealogies beyond 3-5 have reduced impact
- **Blending:** High genealogy count → traits blend into generic hybrid

---

### Trait Inheritance Balance

**Dominant vs. Blended:**
- **Majority Rule:** Genealogies >50% dominate traits (clear expression)
- **Blending:** Genealogies <50% blend traits (smooth transitions)
- **Hybrid Traits:** Some traits only appear in hybrids (synergy effects)

**Variation:**
- **Natural Variation:** Traits vary slightly (not identical entities)
- **Mutation:** Small chance of new genealogies or trait changes
- **Stabilization:** Over generations, traits stabilize (consistent expression)

---

## Game-Specific Applications

### Godgame

**Theological/Magical Context:**
- **Divine Creation:** Gods can create entities with specific genealogy compositions (divine crafting)
- **Blessed Genealogies:** Gods can bless genealogies (enhanced traits, divine favor)
- **Sacred Hybrids:** Some hybrid genealogies are sacred (divine creatures, celestial beings)
- **Miracle Integration:** Miracles can modify genealogies (transformation, enhancement)

**Example:** Player god creates Cat Person → blesses Cat Person genealogy → Cat People become sacred race → serve god → god gains Cat Person followers.

---

### Space4X

**Xenobiology/Exobiology Context:**
- **Alien Species:** Genealogies represent alien species (xenobiology)
- **Genetic Engineering:** Advanced technology allows intentional genealogy mixing (genetic engineering)
- **Hybrid Colonies:** Colonies with mixed genealogies (xenodiversity)
- **Xenodiplomacy:** Hybrid genealogies affect diplomatic relations (species compatibility)

**Example:** Colony discovers Cat People → studies Cat People genealogy → creates Cat Person + Human hybrids → hybrids form colony → xenodiplomatic relations.

---

## Open Questions

1. **Genealogy Discovery:** How are new genealogies discovered? (exploration, research, mutation)
2. **Trait Conflicts:** How are conflicting traits resolved? (e.g., Tree stationary vs. Human mobile)
3. **Genealogy Limits:** Maximum genealogies per entity? Minimum contribution threshold?
4. **Stabilization Criteria:** Exact criteria for genealogy stabilization? (population size, generations, stability)
5. **Player Interface:** How do players mix genealogies? (UI, crafting system, selection interface)
6. **Genealogy Templates:** Pre-defined genealogy templates vs. free-form mixing?
7. **Genealogy Evolution:** Can genealogies evolve over time? (natural selection, player influence)

---

## Related Documentation

- **Entity Lifecycle:** `puredots/Docs/Concepts/Core/Entity_Lifecycle.md`
- **Entity Crafting System:** `godgame/Docs/Concepts/Villagers/Entity_Crafting_System.md`
- **Sentient Flora-Fauna Hybrids:** `puredots/Docs/Concepts/Core/Sentient_Flora_Fauna_Hybrids.md`
- **Entity Profile Schema:** `puredots/Docs/Concepts/Core/Entity_Profile_Schema.md`






