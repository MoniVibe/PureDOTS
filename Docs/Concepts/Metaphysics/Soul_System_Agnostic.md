# Soul System (Agnostic Framework)

**Status:** Concept Design
**Category:** Core Metaphysics Framework
**Shareability:** `shared-core`
**Last Updated:** 2025-12-07

---

## Overview

The **Soul System** provides agnostic mechanics for persistent entity consciousness that can be extracted, transferred, stored, and manipulated across different vessels. Games implement specific soul acquisition methods, vessel types, and metaphysical consequences while PureDOTS provides the soul identity tracking, vessel compatibility algorithms, integrity degradation formulas, and transfer mechanics.

**Agnostic Aspects** (Provided by PureDOTS):
- ✅ Soul identity and persistence
- ✅ Soul-vessel compatibility calculation
- ✅ Soul integrity degradation formulas
- ✅ Soul extraction/transfer algorithms
- ✅ Soul strength calculation framework
- ✅ Vessel binding mechanics
- ✅ Soul corruption tracking

**Game-Specific Aspects** (Implemented by Games):
- Soul acquisition methods (magic rituals, technology, divine intervention)
- Vessel types (soulshards, cortical stacks, artifacts, machines)
- Metaphysical consequences (afterlife, reincarnation, karma)
- Cultural reactions (soul trafficking laws, ethical debates)
- Visual presentation (soul VFX, auras, vessel appearance)

---

## Core Agnostic Components

### Soul (Mind ECS)
```csharp
/// <summary>
/// Agnostic soul component
/// Represents persistent entity consciousness
/// </summary>
public struct Soul : IComponentData
{
    public ulong SoulID;                   // Unique identifier (persists across lifetimes)
    public float Integrity;                // 0-100% (fragmentation damage)
    public float Strength;                 // Calculated power (determines value, resistance)

    public byte VesselTypeId;              // Game-defined enum (Body, Shard, Object, etc.)
    public Entity VesselEntity;            // Current container

    public uint AgeInTicks;                // Total existence time
    public int TransferCount;              // How many times transferred
    public float Corruption;               // 0-100 (from degradation sources)

    public bool IsWilling;                 // Consented to current vessel
    public bool CanBeExtracted;            // Extraction possible flag
}
```

### SoulVessel (Body ECS)
```csharp
/// <summary>
/// Agnostic soul container
/// Marks entity as capable of housing souls
/// </summary>
public struct SoulVessel : IComponentData
{
    public byte VesselTypeId;              // Game-defined enum
    public Entity BoundSoul;               // Currently housed soul

    public float Compatibility;            // 0-1 (soul-vessel match quality)
    public float BindingStrength;          // 0-100 (extraction resistance)

    public bool IsPermanentBinding;        // Cannot be extracted if true
    public uint BindingTick;               // When soul was bound
    public float DegradationRate;          // Integrity loss per update (0 = perfect vessel)
}
```

### SoulBound (Body ECS)
```csharp
/// <summary>
/// Soul-powered object/equipment
/// </summary>
public struct SoulBound : IComponentData
{
    public Entity BoundSoul;               // Powering soul
    public float PowerBonus;               // Effect magnitude from soul strength

    public float Sentience;                // 0-100 (awareness level)
    public float Loyalty;                  // 0-100 (to current owner)

    public bool CanCommunicate;            // Telepathy/interface possible
    public bool CanEvolve;                 // Gains abilities over time
    public int KillCount;                  // Experience tracking
}
```

### SoulExtractionInProgress (Mind ECS)
```csharp
/// <summary>
/// Active soul extraction ritual
/// </summary>
public struct SoulExtractionInProgress : IComponentData
{
    public Entity Extractor;               // Entity performing extraction
    public Entity Target;                  // Soul being extracted
    public Entity TargetVessel;            // Destination container

    public float Progress;                 // 0-1
    public float TimeRemaining;            // Seconds
    public float SuccessChance;            // 0-1 (calculated at start)
}
```

### SoulFragmentation (Mind ECS)
```csharp
/// <summary>
/// Soul damage tracking
/// </summary>
public struct SoulFragmentation : IComponentData
{
    public int FragmentCount;              // How many pieces soul is in
    public float DamageAccumulated;        // Total integrity loss
    public float HealingRate;              // Integrity recovery per update

    public bool IsCritical;                // Below 20% integrity
    public bool IsCollapsing;              // Below 5% integrity (imminent destruction)
}
```

---

## Agnostic Algorithms

### Soul Strength Calculation
```csharp
/// <summary>
/// Calculate soul inherent power
/// Agnostic: Games provide entity level, age, willpower, bonuses
/// </summary>
public static float CalculateSoulStrength(
    int entityLevel,
    float ageInYears,
    float willpower,           // 0-100
    float achievementBonus,    // 0-500 (legendary deeds)
    float affinityBonus)       // 0-100 (magic/tech affinity)
{
    float levelContribution = entityLevel * 2f;
    float ageContribution = ageInYears / 10f;
    float willContribution = willpower * 1.5f;

    float totalStrength = levelContribution + ageContribution + willContribution + achievementBonus + affinityBonus;

    return math.max(1f, totalStrength);
}
```

### Soul-Vessel Compatibility
```csharp
/// <summary>
/// Calculate how well soul matches vessel
/// Agnostic: Compatibility affects transfer success, degradation rate
/// </summary>
public static float CalculateSoulVesselCompatibility(
    Soul soul,
    SoulVessel vessel,
    bool isSameSpecies,        // Game-defined (for biological vessels)
    bool isOriginalBody,       // True if soul's birth body
    float magicalEnhancement)  // 0-100 (vessel enhancement quality)
{
    float compatibility = 0.5f; // Base: 50%

    // Original body = perfect match
    if (isOriginalBody)
        return 1.0f;

    // Species matching (for biological vessels)
    if (isSameSpecies)
        compatibility += 0.3f;

    // Magical/technological enhancement
    compatibility += magicalEnhancement * 0.002f; // +20% at 100 enhancement

    // Soul integrity affects compatibility
    compatibility *= (soul.Integrity / 100f);

    return math.clamp(compatibility, 0f, 1.0f);
}
```

### Soul Extraction Success Chance
```csharp
/// <summary>
/// Calculate extraction success probability
/// Agnostic: Skill, target resistance, cooperation
/// </summary>
public static float CalculateExtractionSuccessChance(
    float extractorSkill,      // 0-100 (Soul Magic, Neuroscience, etc.)
    float targetWillpower,     // 0-100 (resistance)
    float soulStrength,        // Calculated strength
    bool isWilling,            // Target cooperation
    bool isCorpse)             // Dead vs alive
{
    float baseChance = 0.6f;

    // Skill bonus
    float skillBonus = extractorSkill * 0.005f; // +50% at skill 100

    // Willpower penalty (if resisting)
    float willPenalty = isWilling ? 0f : targetWillpower * 0.003f; // -30% at 100 willpower

    // Soul strength penalty (stronger souls resist)
    float strengthPenalty = soulStrength * 0.0001f; // -10% at strength 1000

    // Cooperation bonus
    float cooperationBonus = isWilling ? 0.3f : 0f;

    // Corpse bonus (no active resistance)
    float corpseBonus = isCorpse ? 0.2f : 0f;

    float successChance = baseChance + skillBonus - willPenalty - strengthPenalty + cooperationBonus + corpseBonus;

    return math.clamp(successChance, 0.05f, 0.99f);
}
```

### Soul Transfer Success Chance
```csharp
/// <summary>
/// Calculate transfer success probability
/// Agnostic: Vessel compatibility, soul integrity, skill
/// </summary>
public static float CalculateTransferSuccessChance(
    float casterSkill,         // 0-100
    float soulIntegrity,       // 0-100
    float vesselCompatibility, // 0-1
    bool isWillingSoul)
{
    float baseChance = 0.5f;

    // Skill bonus
    float skillBonus = casterSkill * 0.004f; // +40% at skill 100

    // Soul integrity (fragmented souls harder to transfer)
    float integrityBonus = (soulIntegrity - 50f) * 0.004f; // +20% at 100%, -20% at 0%

    // Compatibility
    float compatibilityBonus = vesselCompatibility * 0.4f; // +40% at perfect compatibility

    // Willing soul cooperation
    float willingBonus = isWillingSoul ? 0.1f : 0f;

    float successChance = baseChance + skillBonus + integrityBonus + compatibilityBonus + willingBonus;

    return math.clamp(successChance, 0.05f, 0.99f);
}
```

### Soul Integrity Degradation
```csharp
/// <summary>
/// Calculate integrity loss over time
/// Agnostic: Vessel quality, corruption sources
/// </summary>
public static float CalculateIntegrityDegradation(
    float vesselDegradationRate,   // Per-update loss (0 = perfect vessel)
    float corruptionSources,       // External damage (dark magic, radiation, etc.)
    bool isNaturalVessel,          // True if original body type
    float healingRate)             // Recovery rate (rest, treatment)
{
    float degradation = vesselDegradationRate + corruptionSources;

    // Natural vessels don't degrade
    if (isNaturalVessel)
        degradation = 0f;

    // Healing offsets degradation
    degradation -= healingRate;

    return math.max(0f, degradation);
}
```

### Soul Integrity Update
```csharp
/// <summary>
/// Update soul integrity per frame
/// Agnostic: Apply degradation, clamp to valid range
/// </summary>
public static float UpdateSoulIntegrity(
    float currentIntegrity,
    float degradationRate,     // Per-update loss
    float deltaTime)
{
    float loss = degradationRate * deltaTime;
    float newIntegrity = currentIntegrity - loss;

    return math.clamp(newIntegrity, 0f, 100f);
}
```

### Soul Integrity Effects
```csharp
/// <summary>
/// Calculate stat penalties from fragmentation
/// Agnostic: Integrity determines penalty severity
/// </summary>
public static float CalculateFragmentationPenalty(float integrity)
{
    if (integrity >= 80f)
        return 0f;       // No penalty

    if (integrity >= 60f)
        return -0.1f;    // -10% stats

    if (integrity >= 40f)
        return -0.25f;   // -25% stats

    if (integrity >= 20f)
        return -0.5f;    // -50% stats

    return -0.75f;       // -75% stats (near collapse)
}
```

### Soul Consumption Power Gain
```csharp
/// <summary>
/// Calculate power gained from consuming soul
/// Agnostic: Soul strength determines gain, corruption cost
/// </summary>
public static void CalculateSoulConsumptionGain(
    float soulStrength,
    out float powerGain,       // Energy/mana gained
    out float tempStatBoost,   // Temporary stat increase
    out float corruptionGain)  // Permanent corruption
{
    // Power scales linearly with strength
    powerGain = soulStrength * 2f;

    // Temporary stat boost (1 hour duration in game)
    tempStatBoost = soulStrength / 10f;

    // Corruption penalty
    corruptionGain = 10f + (soulStrength / 100f);
}
```

### Soul-Bound Object Power Bonus
```csharp
/// <summary>
/// Calculate bonus from soul-infused weapon/object
/// Agnostic: Soul strength determines magnitude
/// </summary>
public static float CalculateSoulBoundBonus(
    float soulStrength,
    float baseMagnitude,       // Base weapon damage/effect
    float sentience,           // 0-100 (awareness level)
    float loyalty)             // 0-100 (to wielder)
{
    // Strength multiplier
    float strengthMultiplier = 1f + (soulStrength / 1000f); // +100% at strength 1000

    // Sentience bonus (intelligent souls enhance effectiveness)
    float sentienceBonus = 1f + (sentience / 200f); // +50% at 100 sentience

    // Loyalty modifier (disloyal souls resist)
    float loyaltyModifier = loyalty / 100f; // 0-100% effectiveness

    float finalBonus = baseMagnitude * strengthMultiplier * sentienceBonus * loyaltyModifier;

    return finalBonus;
}
```

### Soul-Bound Evolution
```csharp
/// <summary>
/// Calculate soul-bound weapon evolution
/// Agnostic: Kill count determines experience gain
/// </summary>
public static bool CheckEvolutionThreshold(
    int killCount,
    int evolutionTier)         // Current tier (0-5)
{
    // Exponential threshold
    int requiredKills = (int)math.pow(10, evolutionTier + 1);
    return killCount >= requiredKills;
}

public static float CalculateEvolutionBonus(int evolutionTier)
{
    // +10% per tier
    return evolutionTier * 0.1f;
}
```

### Soul Corruption Progression
```csharp
/// <summary>
/// Calculate corruption effects over time
/// Agnostic: Corruption affects alignment, sanity, appearance
/// </summary>
public static void CalculateCorruptionEffects(
    float corruption,          // 0-100
    out float alignmentShift,  // Toward evil
    out float sanityPenalty,   // Mental stability loss
    out float appearanceChange) // Visual corruption
{
    if (corruption < 20f)
    {
        alignmentShift = 0f;
        sanityPenalty = 0f;
        appearanceChange = 0f;
    }
    else if (corruption < 50f)
    {
        alignmentShift = -10f;
        sanityPenalty = -5f;
        appearanceChange = 0.2f;
    }
    else if (corruption < 80f)
    {
        alignmentShift = -30f;
        sanityPenalty = -20f;
        appearanceChange = 0.5f;
    }
    else
    {
        alignmentShift = -60f;
        sanityPenalty = -50f;
        appearanceChange = 1.0f; // Fully corrupted appearance
    }
}
```

### Resurrection Integrity Cost
```csharp
/// <summary>
/// Calculate soul integrity loss from resurrection
/// Agnostic: Each death/resurrection fragments soul further
/// </summary>
public static float CalculateResurrectionIntegrityCost(
    int previousDeaths,        // How many times already resurrected
    float currentIntegrity)
{
    // Diminishing returns: each resurrection costs more
    float baseCost = 10f;
    float exponentialPenalty = (float)math.pow(2, previousDeaths); // 2^n scaling
    float totalCost = baseCost * exponentialPenalty;

    // Cannot reduce below 5% (collapse threshold)
    float maxCost = currentIntegrity - 5f;
    return math.min(totalCost, maxCost);
}
```

### Soul Linger Duration (After Death)
```csharp
/// <summary>
/// Calculate how long soul remains extractable after death
/// Agnostic: Time window for extraction/resurrection
/// </summary>
public static float CalculateSoulLingerDuration(
    float soulStrength,        // Stronger souls linger longer
    float bodyPreservation)    // 0-1 (preservation quality)
{
    // Base: 24 hours
    float baseHours = 24f;

    // Strength extension (+1 hour per 100 strength)
    float strengthBonus = soulStrength / 100f;

    // Preservation extension (×3 at perfect preservation)
    float preservationMultiplier = 1f + (bodyPreservation * 2f);

    float totalHours = (baseHours + strengthBonus) * preservationMultiplier;

    return totalHours;
}
```

---

## Extension Points for Games

### 1. Soul Vessel Type Definitions
Games define soul container types:
```csharp
// Godgame example
public enum GodgameSoulVesselType : byte
{
    LivingBody,
    Corpse,
    Soulshard,          // Magical crystal
    SoulBoundWeapon,
    SoulBoundArmor,
    Golem,              // Souled construct
    UndeadBody,
    Phylactery,         // Lich immortality
}

// Space4X example
public enum Space4XSoulVesselType : byte
{
    OrganicBody,
    CloneBody,
    SyntheticBody,
    CorticalStack,      // Altered carbon device
    AICore,             // Digital soul storage
    Mech,               // Pilot integration
    Ship,               // Captain bonding
}
```

### 2. Soul Acquisition Method Definitions
Games define how souls are obtained:
```csharp
// Godgame example
public enum GodgameSoulAcquisitionMethod : byte
{
    Natural,            // Born with soul
    Extraction,         // Magic ritual
    Summoning,          // Call from afterlife
    Creation,           // God creates soul
    Theft,              // Steal from entity
    Consumption,        // Absorb from killed entity
}

// Space4X example
public enum Space4XSoulAcquisitionMethod : byte
{
    Birth,              // Organic birth
    Upload,             // Scan consciousness
    Copy,               // Duplicate existing soul
    Synthesis,          // Create artificial consciousness
    Theft,              // Hack cortical stack
}
```

### 3. Soul Corruption Source Definitions
Games define corruption sources:
```csharp
// Godgame example
public enum GodgameCorruptionSource : byte
{
    SoulConsumption,
    DarkMagic,
    DemonicPact,
    Necromancy,
    SoulTorture,
}

// Space4X example
public enum Space4XCorruptionSource : byte
{
    DataCorruption,
    MemoryFragmentation,
    AIVirus,
    UnauthorizedEdits,
    QuantumDecay,
}
```

---

## Integration Requirements

### Systems Games Must Implement

1. **SoulExtractionSystem** (Mind ECS, 1 Hz)
   - Calculate extraction success chance
   - Update extraction progress
   - Handle extraction completion/failure
   - Store soul in target vessel

2. **SoulTransferSystem** (Mind ECS, 1 Hz)
   - Calculate transfer success chance
   - Validate vessel compatibility
   - Move soul between vessels
   - Apply integrity cost

3. **SoulIntegrityUpdateSystem** (Mind ECS, 1 Hz)
   - Update integrity based on vessel degradation
   - Apply corruption sources
   - Calculate fragmentation penalties
   - Detect soul collapse (integrity < 5%)

4. **SoulBoundPowerSystem** (Body ECS, 60 Hz)
   - Calculate soul-powered bonuses
   - Update sentience and loyalty
   - Track evolution progress (kills, experience)
   - Apply power bonuses to weapons/objects

5. **SoulConsumptionSystem** (Mind ECS, 1 Hz)
   - Execute soul destruction
   - Grant power/mana to consumer
   - Apply corruption penalty
   - Raise cultural/ethical events

6. **SoulLingerSystem** (Mind ECS, 1 Hz)
   - Track time since death
   - Fade souls to afterlife after linger duration
   - Enable extraction during linger window
   - Disable extraction after window closes

7. **ResurrectionSystem** (Mind ECS, 1 Hz)
   - Validate soul availability
   - Check vessel compatibility
   - Calculate resurrection success
   - Apply integrity cost
   - Transfer soul to new body

---

## Data Contracts

Games must provide:
- Soul vessel type catalog (types, degradation rates, capacities)
- Soul acquisition method catalog (rituals, costs, success rates)
- Corruption source catalog (sources, corruption rates, consequences)
- Soul strength formula inputs (level, age, willpower, bonuses)
- Extraction/transfer skill systems (Soul Magic, Neuroscience, etc.)
- Afterlife mechanics (linger duration, final destination)
- Cultural reactions (soul trafficking laws, ethical debates)

---

## Game-Specific Implementations

### Godgame (Magical Souls)
**Full Implementation:** [Soul_System.md](../../../../Godgame/Docs/Concepts/Metaphysics/Soul_System.md)

**Acquisition:** Magic rituals, necromancy, divine summoning
**Vessels:** Soulshards, soul-bound weapons, phylacteries, undead bodies
**Corruption:** Soul consumption, dark magic, demonic pacts
**Resurrection:** Divine magic, soul transfer rituals
**Cultural:** Soul trafficking illegal, soul consumption condemned

### Space4X (Digital Consciousness)
**Implementation Reference:** TBD

**Acquisition:** Consciousness upload, cortical stack implants, AI synthesis
**Vessels:** Cortical stacks, clones, synthetic bodies, AI cores, mechs
**Corruption:** Data corruption, memory fragmentation, unauthorized edits
**Resurrection:** Resleeving (stack transfer), backup restore
**Cultural:** Soul duplication debates, AI rights, stack ownership laws

---

## Performance Targets

**Mind ECS (1 Hz) Budget:** 20-30 ms/update
- Soul extraction: 5 ms (ritual progress)
- Soul transfer: 5 ms (vessel validation, integrity updates)
- Soul integrity: 10 ms (degradation calculations)
- Soul consumption: 5 ms (power gain, corruption)
- Soul linger: 5 ms (fade to afterlife)

**Body ECS (60 Hz) Budget:** 1-2 ms/frame
- Soul-bound power bonuses: 1 ms (damage calculations)
- Soul vessel integrity checks: 0.5 ms
- Soul corruption VFX: 0.5 ms

**Aggregate ECS (0.2 Hz) Budget:** 15-20 ms/update
- Soul market pricing: 10 ms (soulshards, trafficking)
- Cultural reactions: 5 ms (laws, ethics)
- Soul statistics: 5 ms (total souls, resurrection counts)

**Optimization Strategies:**
- Soul ID hashing (fast lookups, avoid linear searches)
- Batch soul updates (group by vessel type)
- LOD soul processing (reduce update frequency for distant/inactive souls)
- Event-based extraction (only calculate when ritual active)
- Cached compatibility (reuse calculations for same soul-vessel pairs)

---

## Testing Guidelines

### Unit Tests (PureDOTS)
- ✅ Soul strength calculation (level + age + willpower + bonuses)
- ✅ Soul-vessel compatibility (species, original body, enhancement)
- ✅ Extraction success chance (skill, willpower, strength, cooperation)
- ✅ Transfer success chance (skill, integrity, compatibility)
- ✅ Integrity degradation (vessel rate, corruption sources)
- ✅ Fragmentation penalties (integrity → stat penalty)
- ✅ Soul consumption power gain (strength → power/stats/corruption)
- ✅ Soul-bound power bonus (strength, sentience, loyalty)
- ✅ Resurrection integrity cost (previous deaths → diminishing returns)
- ✅ Soul linger duration (strength, preservation → hours)

### Integration Tests (Games)
- Soul extraction from living/dead entity
- Soul transfer between vessels (body → shard → weapon)
- Soul integrity degradation over time
- Soul-bound weapon evolution (kills → abilities)
- Soul consumption grants power + corruption
- Resurrection with different body types
- Soul collapse at 0% integrity

---

## Migration Notes

**New Components Required:**
- `Soul` (Mind ECS)
- `SoulVessel` (Body ECS)
- `SoulBound` (Body ECS)
- `SoulExtractionInProgress` (Mind ECS)
- `SoulFragmentation` (Mind ECS)

**Integration with Existing Systems:**
- Consciousness transfer (soul = persistence layer)
- Entity construction (souled vs soulless constructs)
- Necromancy (undead with/without souls)
- Grafting (soul compatibility affects rejection)
- Combat (soul-bound weapons, soul-powered abilities)

---

## Related Documents

**PureDOTS Agnostic:**
- `Docs/Architecture/ThreePillarECS_Architecture.md` - ECS layers (to be created)
- `Docs/BestPractices/DOTS_1_4_Patterns.md` - DOTS coding patterns

**Game Implementations:**
- `Godgame/Docs/Concepts/Metaphysics/Soul_System.md` - Full magical soul concept
- `Space4X/Docs/Concepts/Metaphysics/Digital_Consciousness.md` - Digital soul variant (to be created)

**Related Concepts:**
- `Docs/Concepts/Metaphysics/Consciousness_Transfer.md` - Soul transfer mechanics (to be created)
- `Godgame/Docs/Concepts/Production/Entity_Construction_System.md` - Souled constructs
- `Godgame/Docs/Concepts/Magic/Necromancy_System.md` - Undead with/without souls (to be created)

---

**Last Updated:** 2025-12-07
**Maintainer:** PureDOTS Core Team
**Status:** Awaiting Implementation
