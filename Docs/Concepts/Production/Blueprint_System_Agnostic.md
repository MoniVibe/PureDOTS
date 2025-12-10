# Blueprint System (Agnostic Framework)

**Status:** Concept Design
**Category:** Production & Knowledge Framework
**Shareability:** `shared-core`
**Last Updated:** 2025-12-07

---

## Overview

The **Blueprint System** provides agnostic mechanics for capturing, storing, and transferring design knowledge. Blueprints vary in quality based on creator intelligence and documentation practices. Games implement specific blueprint types, complexity tiers, and espionage mechanics while PureDOTS provides fidelity calculations, degradation formulas, improvement algorithms, and usage modifiers.

**Agnostic Aspects** (Provided by PureDOTS):
- ✅ Blueprint fidelity calculation (intelligence, skill, practice quality)
- ✅ Complexity penalty formulas
- ✅ Usage success/time modifiers
- ✅ Blueprint improvement algorithms (skilled users enhance designs)
- ✅ Copy degradation (generational fidelity loss)
- ✅ Reverse engineering difficulty
- ✅ Sabotage detection

**Game-Specific Aspects** (Implemented by Games):
- Blueprint types (weapons, constructs, spells, ships, etc.)
- Complexity tier definitions
- Security systems (vaults, encryption, guards)
- Espionage mechanics (theft, corporate warfare)
- Legal frameworks (patents, guild secrets, black markets)
- Visual presentation (blueprint UI, diagrams)

---

## Core Agnostic Components

### Blueprint (Mind ECS)
```csharp
/// <summary>
/// Agnostic blueprint component
/// </summary>
public struct Blueprint : IComponentData
{
    public FixedString64Bytes DesignName;
    public ushort DesignTypeId;            // Game-defined enum

    public float Fidelity;                 // 0-100% (accuracy)
    public int ComplexityTier;             // 0-5 (game-defined tiers)
    public int CreatorIntelligence;        // Creator's INT stat
    public int CreatorSkillLevel;          // Creator's relevant skill

    public int GenerationNumber;           // 0 = original, 1+ = copies
    public bool IsSabotaged;               // Intentionally corrupted
    public bool IsEncrypted;               // Requires decryption
    public int EncryptionStrength;         // 0-100
}
```

### BlueprintCollection (Mind ECS Buffer)
```csharp
/// <summary>
/// Entity's owned blueprints
/// </summary>
[InternalBufferCapacity(16)]
public struct BlueprintCollection : IBufferElementData
{
    public Entity BlueprintEntity;
    public bool HasMasterCopy;             // Owns original vs copy
    public float FamiliarityLevel;         // 0-100 (usage experience)
}
```

### BlueprintCreationInProgress (Mind ECS)
```csharp
/// <summary>
/// Active blueprint documentation
/// </summary>
public struct BlueprintCreationInProgress : IComponentData
{
    public Entity Creator;
    public Entity TargetDesign;

    public float Progress;                 // 0-1
    public float TimeRemaining;            // Hours/minutes
    public int WritingPracticeQuality;     // 0-100
    public float EstimatedFidelity;        // Calculated at start
}
```

### BlueprintUsage (Body ECS)
```csharp
/// <summary>
/// Tracks blueprint usage during crafting
/// </summary>
public struct BlueprintUsage : IComponentData
{
    public Entity BlueprintEntity;
    public Entity CrafterEntity;

    public float SuccessModifier;          // Bonus/penalty to crafting
    public float TimeMultiplier;           // Crafting time modifier
    public bool CanImprove;                // True if crafter skilled enough
    public float ImprovementChance;        // 0-1
}
```

---

## Agnostic Algorithms

### Dynamic Complexity Calculation
```csharp
/// <summary>
/// Calculate design complexity from item properties
/// Agnostic: Item stats determine documentation difficulty
/// </summary>
public static float CalculateDesignComplexity(
    int itemLevel,             // 0-100
    int materialRarity,        // 0-10 (game-defined scale)
    float materialMass,        // kg
    int sizeCategory,          // 0-10 (Tiny to Colossal)
    int rarityTier,            // 0-5 (Common to Legendary)
    int techLevel)             // 0-10 (game-defined tech scale)
{
    float baseLevel = itemLevel / 2f;
    float materialComplexity = (materialRarity * 2f) + (materialMass / 100f);
    float sizeComplexity = sizeCategory * 3f;
    float rarityMultiplier = rarityTier * 5f;
    float techBonus = techLevel * 4f;

    float totalComplexity = baseLevel + materialComplexity + sizeComplexity + rarityMultiplier + techBonus;

    // Cap at maximum complexity
    return math.min(totalComplexity, 60f);
}
```

### Design Optimization (Complexity Reduction)
```csharp
/// <summary>
/// Calculate complexity reduction from smart design
/// Agnostic: High INT/WIS/Education can optimize designs
/// </summary>
public static float CalculateOptimizedComplexity(
    float baseComplexity,
    int designerIntelligence,  // 0-100
    int designerWisdom,        // 0-100
    int educationLevel,        // 0-100
    int itemLevel)
{
    // Optimization reduction
    float reduction = ((designerIntelligence + designerWisdom + educationLevel) / 5f) - (itemLevel / 2f);

    // Apply reduction
    float optimizedComplexity = baseComplexity - reduction;

    // Constraints
    float maxReduction = baseComplexity * 0.5f; // Cannot reduce by more than 50%
    float minComplexity = 10f; // Minimum inherent difficulty

    // Ensure reduction doesn't exceed maximum
    if (baseComplexity - optimizedComplexity > maxReduction)
    {
        optimizedComplexity = baseComplexity - maxReduction;
    }

    return math.max(optimizedComplexity, minComplexity);
}
```

### Blueprint Fidelity Calculation
```csharp
/// <summary>
/// Calculate blueprint accuracy
/// Agnostic: Intelligence + Skill + Practice - Complexity
/// </summary>
public static float CalculateBlueprintFidelity(
    int creatorIntelligence,       // 0-100
    int creatorSkill,              // 0-100
    int writingPracticeQuality,    // 0-100
    int complexityPenalty)         // 0-40 (game-defined)
{
    float intContribution = creatorIntelligence * 0.8f;
    float skillContribution = creatorSkill * 0.15f;
    float practiceContribution = writingPracticeQuality * 0.05f;

    float fidelity = intContribution + skillContribution + practiceContribution - complexityPenalty;

    return math.clamp(fidelity, 0f, 100f);
}
```

### Blueprint Usage Success Modifier
```csharp
/// <summary>
/// Calculate crafting success bonus/penalty from blueprint
/// Agnostic: Fidelity determines help/hindrance
/// </summary>
public static float CalculateUsageSuccessModifier(float fidelity)
{
    // Center at 50%: Above 50 = bonus, below 50 = penalty
    return (fidelity - 50f) * 2f;
}
```

**Examples:**
```
Fidelity 98%: (98 - 50) × 2 = +96% success
Fidelity 75%: (75 - 50) × 2 = +50% success
Fidelity 55%: (55 - 50) × 2 = +10% success
Fidelity 35%: (35 - 50) × 2 = -30% success (hinders)
```

### Blueprint Usage Time Multiplier
```csharp
/// <summary>
/// Calculate crafting time modifier from blueprint
/// Agnostic: Lower fidelity = more time reading/re-reading
/// </summary>
public static float CalculateUsageTimeMultiplier(float fidelity)
{
    // Perfect blueprint (100%) = 1.0× time
    // Poor blueprint (0%) = 2.0× time
    return 2.0f - (fidelity / 100f);
}
```

**Examples:**
```
Fidelity 98%: 2.0 - 0.98 = 1.02× time (2% slower)
Fidelity 75%: 2.0 - 0.75 = 1.25× time (25% slower)
Fidelity 50%: 2.0 - 0.50 = 1.50× time (50% slower)
Fidelity 20%: 2.0 - 0.20 = 1.80× time (80% slower)
```

### Blueprint Improvement Calculation
```csharp
/// <summary>
/// Calculate if crafter can improve blueprint
/// Agnostic: Skilled crafters improve low-fidelity blueprints
/// </summary>
public static float CalculateBlueprintImprovementChance(
    int crafterSkill,
    int crafterIntelligence,
    float blueprintFidelity,
    int blueprintCreatorIntelligence,
    int designComplexityRequirement)
{
    // Skill surplus (must exceed design requirement)
    float skillSurplus = crafterSkill - designComplexityRequirement;

    // Higher fidelity = harder to improve (diminishing returns)
    float fidelityPenalty = blueprintFidelity / 2f;

    // Base improvement chance
    float baseChance = skillSurplus - fidelityPenalty;

    // Intelligence offset (smarter crafter vs original creator)
    float intOffset = (crafterIntelligence - blueprintCreatorIntelligence) * 0.5f;

    float improvementChance = (baseChance + intOffset) / 100f;

    return math.clamp(improvementChance, 0f, 0.95f);
}
```

### Blueprint Improvement Application
```csharp
/// <summary>
/// Apply improvement to blueprint fidelity
/// Agnostic: Success increases fidelity, failure may reduce
/// </summary>
public static float ApplyBlueprintImprovement(
    float currentFidelity,
    bool success,
    bool criticalFailure)
{
    if (criticalFailure)
    {
        // Misunderstood design, added errors
        return math.max(0f, currentFidelity - 10f);
    }

    if (success)
    {
        // Improved understanding
        float improvement = math.lerp(5f, 15f, Random.value);
        return math.min(100f, currentFidelity + improvement);
    }

    // No change
    return currentFidelity;
}
```

### Copy Fidelity Degradation
```csharp
/// <summary>
/// Calculate fidelity loss when copying blueprint
/// Agnostic: Copies degrade unless copier is intelligent
/// </summary>
public static float CalculateCopyFidelity(
    float originalFidelity,
    int copierIntelligence)
{
    // Base degradation: 90% retention
    // Smart copier: +0.2% retention per INT point (max +20% at 100 INT)
    float retentionRate = 0.90f + (copierIntelligence / 500f);

    float copyFidelity = originalFidelity * retentionRate;

    // Copies cannot exceed original
    return math.min(copyFidelity, originalFidelity);
}
```

**Generational Degradation:**
```csharp
public static float CalculateGenerationalFidelity(
    float originalFidelity,
    int generationNumber,
    int averageCopierIntelligence)
{
    float currentFidelity = originalFidelity;

    for (int gen = 0; gen < generationNumber; gen++)
    {
        currentFidelity = CalculateCopyFidelity(currentFidelity, averageCopierIntelligence);
    }

    return currentFidelity;
}
```

### Reverse Engineering Difficulty
```csharp
/// <summary>
/// Calculate success chance for reverse engineering
/// Agnostic: Requires higher skill, penalized by complexity and tech gap
/// </summary>
public static float CalculateReverseEngineeringSuccess(
    int engineerSkill,
    int engineerIntelligence,
    int itemCreationSkillRequirement,
    float designComplexity,            // Item's complexity
    int engineerTechLevel,             // Engineer's tech level
    int itemTechLevel)                 // Item's tech level
{
    // Must exceed creation requirement by +20
    float skillSurplus = engineerSkill - itemCreationSkillRequirement - 20f;

    // Intelligence bonus
    float intBonus = engineerIntelligence / 2f;

    // Complexity penalty (harder to reverse complex items)
    float complexityPenalty = designComplexity / 4f;

    // Tech level gap penalty (only if item tech > engineer tech)
    int techGap = math.max(0, itemTechLevel - engineerTechLevel);
    float techPenalty = techGap * 5f;

    float successChance = (skillSurplus + intBonus - complexityPenalty - techPenalty) / 100f;

    // Minimum 5% chance (nearly impossible but not completely)
    return math.clamp(successChance, 0.05f, 0.95f);
}
```

**Reverse Engineering Time Calculation:**
```csharp
/// <summary>
/// Calculate time required for reverse engineering
/// Agnostic: Complexity and tech gap increase study time
/// </summary>
public static float CalculateReverseEngineeringTime(
    float baseBlueprintTime,           // Normal blueprint creation time
    float designComplexity,
    int techGap)                        // Item tech - engineer tech (0 if engineer ≥ item)
{
    float complexityMultiplier = 2f + (designComplexity / 20f);
    float techGapMultiplier = 1f + (techGap / 2f);

    return baseBlueprintTime * complexityMultiplier * techGapMultiplier;
}
```

**Reverse Engineering Fidelity Penalty:**
```csharp
/// <summary>
/// Calculate fidelity loss from reverse engineering
/// Agnostic: Complexity and tech gap reduce fidelity
/// </summary>
public static float CalculateReverseEngineeringFidelity(
    float baseCreationFidelity,
    float designComplexity,
    int techGap)
{
    // Base penalty: 20%
    // Complexity penalty: +0.5% per complexity point
    // Tech gap penalty: +5% per tech level gap
    float basePenalty = 20f;
    float complexityPenalty = designComplexity / 2f;
    float techPenalty = techGap * 5f;

    float totalPenalty = basePenalty + complexityPenalty + techPenalty;

    float reversedFidelity = baseCreationFidelity - totalPenalty;

    return math.max(0f, reversedFidelity);
}
```

### Sabotage Detection
```csharp
/// <summary>
/// Calculate chance to detect sabotaged blueprint
/// Agnostic: Intelligence check vs saboteur's skill
/// </summary>
public static bool DetectSabotage(
    int inspectorIntelligence,
    int saboteurSkill,
    bool thoroughInspection,     // Extensive testing vs quick glance
    float randomValue)            // 0-1
{
    float detectionChance = 0.3f; // Base: 30%

    // Intelligence bonus
    detectionChance += inspectorIntelligence * 0.005f; // +50% at 100 INT

    // Saboteur skill penalty (better sabotage = harder to detect)
    detectionChance -= saboteurSkill * 0.004f; // -40% at 100 skill

    // Thorough inspection bonus
    if (thoroughInspection)
        detectionChance += 0.3f;

    detectionChance = math.clamp(detectionChance, 0.05f, 0.95f);

    return randomValue < detectionChance;
}
```

### Blueprint Market Value
```csharp
/// <summary>
/// Calculate blueprint market price
/// Agnostic: Based on item value and fidelity
/// </summary>
public static float CalculateBlueprintValue(
    float itemMarketValue,
    float blueprintFidelity)
{
    // Base: 30% of item value
    // Fidelity modifier: Squared (low fidelity blueprints worth much less)
    float fidelityMultiplier = math.pow(blueprintFidelity / 100f, 2f);

    return itemMarketValue * 0.3f * fidelityMultiplier;
}
```

**Examples:**
```
Item worth 100,000, Blueprint 95% fidelity:
Value = 100,000 × 0.3 × 0.95² = 27,075

Item worth 100,000, Blueprint 55% fidelity:
Value = 100,000 × 0.3 × 0.55² = 9,075

Item worth 100,000, Blueprint 30% fidelity:
Value = 100,000 × 0.3 × 0.30² = 2,700
```

### Encryption Decryption Difficulty
```csharp
/// <summary>
/// Calculate time/success to decrypt blueprint
/// Agnostic: Intelligence vs encryption strength
/// </summary>
public static float CalculateDecryptionTime(
    int decrypterIntelligence,
    int encryptionStrength,      // 0-100
    float baseTime)              // Hours/minutes (game-defined)
{
    float difficulty = encryptionStrength - decrypterIntelligence;

    if (difficulty < 0)
    {
        // Easy decryption (smarter than encryption)
        return baseTime * (1f + difficulty / 50f); // Faster
    }
    else
    {
        // Hard decryption (encryption exceeds intelligence)
        return baseTime * (1f + difficulty / 25f); // Much slower
    }
}

public static bool CanDecrypt(
    int decrypterIntelligence,
    int encryptionStrength)
{
    // Require INT within 20 points of encryption strength
    return decrypterIntelligence >= (encryptionStrength - 20);
}
```

---

## Extension Points for Games

### 1. Blueprint Type Definitions
Games define blueprint categories:
```csharp
// Godgame example
public enum GodgameBlueprintType : ushort
{
    Weapon,
    Armor,
    Potion,
    Spell,
    Construct,           // Golem, elemental
    GraftProcedure,      // Surgical procedure
    SoulRitual,          // Soul magic
    Building,
}

// Space4X example
public enum Space4XBlueprintType : ushort
{
    ShipComponent,
    Mech,
    Weapon,
    Cybernetic,
    SoftwareProgram,
    ManufacturingProcess,
}
```

### 2. Complexity Tier Definitions
Games define complexity penalties:
```csharp
// Godgame example
public static int GetComplexityPenalty(GodgameComplexity complexity)
{
    switch (complexity)
    {
        case GodgameComplexity.Trivial: return 0;
        case GodgameComplexity.Simple: return 5;
        case GodgameComplexity.Moderate: return 10;
        case GodgameComplexity.Complex: return 15;
        case GodgameComplexity.VeryComplex: return 25;
        case GodgameComplexity.Legendary: return 40;
        default: return 0;
    }
}
```

### 3. Writing Practice Quality Definitions
Games define documentation effort levels:
```csharp
public enum WritingPracticeQuality : byte
{
    Rushed = 10,         // <1 hour, barely legible
    Poor = 30,           // 1-5 hours, minimal detail
    Average = 50,        // 5-10 hours, basic instructions
    Good = 70,           // 10-20 hours, clear diagrams
    Excellent = 90,      // 20-40 hours, comprehensive
}
```

### 4. Security System Definitions
Games define blueprint protection:
```csharp
// Godgame: Physical + Magical
public struct BlueprintSecurity
{
    public int LockDifficulty;         // Pick-lock challenge
    public int GuardCount;             // Combat challenge
    public int MagicalWardStrength;    // Dispel magic challenge
    public bool IsHidden;              // Investigation required
}

// Space4X: Digital
public struct BlueprintCybersecurity
{
    public int FirewallStrength;       // Hacking challenge
    public int EncryptionLevel;        // Decryption challenge
    public bool HasHoneypot;           // Trap for hackers
    public bool HasAlarm;              // Alerts on breach
}
```

---

## Integration Requirements

### Systems Games Must Implement

1. **BlueprintCreationSystem** (Mind ECS, 1 Hz)
   - Calculate fidelity (INT + skill + practice)
   - Update creation progress
   - Handle creation completion/failure
   - Store blueprint in inventory

2. **BlueprintUsageSystem** (Mind ECS, 1 Hz)
   - Apply success modifier to crafting
   - Apply time multiplier to crafting
   - Track blueprint familiarity (repeated use)

3. **BlueprintImprovementSystem** (Mind ECS, 1 Hz)
   - Calculate improvement chance
   - Execute improvement roll
   - Create improved blueprint copy (new generation)
   - Grant experience to crafter

4. **BlueprintCopyingSystem** (Mind ECS, 1 Hz)
   - Calculate copy fidelity degradation
   - Increment generation number
   - Create copy in inventory
   - Track original vs copy ownership

5. **BlueprintEspionageSystem** (Mind ECS, 1 Hz or 0.2 Hz)
   - Theft mechanics (lockpicking, hacking, bribery)
   - Security challenge resolution
   - Detection probability
   - Legal/reputation consequences

6. **BlueprintDecryptionSystem** (Mind ECS, 1 Hz)
   - Check if entity can decrypt
   - Calculate decryption time
   - Update decryption progress
   - Unlock blueprint on completion

7. **BlueprintMarketSystem** (Aggregate ECS, 0.2 Hz)
   - Calculate market prices (value × fidelity²)
   - Track black market blueprints
   - Handle blueprint trading

---

## Data Contracts

Games must provide:
- Blueprint type catalog (types, creation requirements, complexity)
- Complexity tier definitions (penalties, difficulty levels)
- Writing practice quality levels (time investment, fidelity impact)
- Security system mechanics (locks, guards, encryption, alarms)
- Espionage mechanics (theft methods, success rates, consequences)
- Legal frameworks (patents, guild rules, black markets)
- Crafting integration (how blueprints modify crafting success/time)

---

## Game-Specific Implementations

### Godgame (Magical Blueprints)
**Full Implementation:** [Blueprint_And_Design_System.md](../../../../Godgame/Docs/Concepts/Production/Blueprint_And_Design_System.md)

**Types:** Weapon, armor, spell, construct, graft procedure, soul ritual
**Security:** Locked vaults, guards, magical wards, encryption
**Espionage:** Physical theft, bribery, reverse engineering
**Culture:** Guild secrets, apprentice/journeyman/master progression

### Space4X (Digital Schematics)
**Implementation Reference:** TBD

**Types:** Ship components, mechs, weapons, software, manufacturing
**Security:** Firewalls, encryption, honeypots, access control
**Espionage:** Hacking, insider trading, patent warfare
**Culture:** Corporate patents, trade secrets, open-source movement

---

## Performance Targets

**Mind ECS (1 Hz) Budget:** 10-15 ms/update
- Blueprint creation: 5 ms (progress tracking)
- Blueprint improvement: 5 ms (skilled crafter checks)
- Blueprint copying: 3 ms (fidelity calculation)
- Blueprint decryption: 2 ms (progress tracking)

**Aggregate ECS (0.2 Hz) Budget:** 15-20 ms/update
- Blueprint market pricing: 10 ms (value calculations)
- Espionage detection: 5 ms (security checks)
- Generational degradation: 5 ms (copy tracking)

**Optimization Strategies:**
- Cache fidelity calculations (blueprint properties don't change often)
- Batch blueprint usage modifiers (compute at craft start, not per-frame)
- LOD blueprint processing (reduce update frequency for inactive blueprints)
- Event-based improvement (only calculate when blueprint used)

---

## Testing Guidelines

### Unit Tests (PureDOTS)
- ✅ Blueprint fidelity calculation (INT + skill + practice - complexity)
- ✅ Usage success modifier ((fidelity - 50) × 2)
- ✅ Usage time multiplier (2.0 - fidelity/100)
- ✅ Improvement chance (skill surplus - fidelity penalty + INT offset)
- ✅ Copy fidelity degradation (0.9 + copier INT/500)
- ✅ Generational degradation (multiple copy iterations)
- ✅ Reverse engineering success (skill - requirement - 20 + INT/2)
- ✅ Sabotage detection (inspector INT vs saboteur skill)
- ✅ Blueprint market value (item value × 0.3 × fidelity²)
- ✅ Decryption time (INT vs encryption strength)

### Integration Tests (Games)
- Blueprint creation (various INT/skill/practice combinations)
- Blueprint usage during crafting (success/time modifiers)
- Blueprint improvement by skilled craftsman
- Blueprint copying with degradation
- Reverse engineering from item
- Sabotaged blueprint detection
- Encrypted blueprint decryption

---

## Migration Notes

**New Components Required:**
- `Blueprint` (Mind ECS)
- `BlueprintCollection` (Mind ECS buffer)
- `BlueprintCreationInProgress` (Mind ECS)
- `BlueprintUsage` (Body ECS)

**Integration with Existing Systems:**
- Entity construction (blueprints for golems, constructs)
- Crafting (blueprint usage modifiers)
- Knowledge (blueprints = transferable knowledge)
- Espionage (blueprint theft mechanics)
- Market economy (blueprint trading, pricing)

---

## Related Documents

**PureDOTS Agnostic:**
- `Docs/Architecture/ThreePillarECS_Architecture.md` - ECS layers (to be created)
- `Docs/BestPractices/DOTS_1_4_Patterns.md` - DOTS coding patterns

**Game Implementations:**
- `Godgame/Docs/Concepts/Production/Blueprint_And_Design_System.md` - Full magical blueprint concept
- `Space4X/Docs/Concepts/Production/Patent_And_IP_System.md` - Digital schematic variant (to be created)

**Related Concepts:**
- `Godgame/Docs/Concepts/Production/Entity_Construction_System.md` - Construct blueprints
- `Godgame/Docs/Concepts/Production/Grafting_And_Augmentation_System.md` - Surgical blueprints
- `Godgame/Docs/Concepts/Metaphysics/Soul_System.md` - Soul ritual blueprints

---

**Last Updated:** 2025-12-07
**Maintainer:** PureDOTS Core Team
**Status:** Awaiting Implementation
