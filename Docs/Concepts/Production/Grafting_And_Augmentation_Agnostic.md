# Grafting and Augmentation (Agnostic Framework)

**Status:** Concept Design
**Category:** Core Production Framework
**Shareability:** `shared-core`
**Last Updated:** 2025-12-07

---

## Overview

The **Grafting and Augmentation Framework** provides agnostic mechanics for modifying entities, ships, and modules by adding components, upgrades, and enhancements. Games implement specific graft types, quality calculations, and failure consequences while PureDOTS provides the grafting state machine, quality system, integration tracking, and hierarchical skill aggregation.

**Agnostic Aspects** (Provided by PureDOTS):
- ✅ Grafting operation state machine
- ✅ Integration quality calculation framework
- ✅ Time/speed calculation algorithms
- ✅ Success/failure probability formulas
- ✅ Hierarchical skill aggregation (corporate grafter + subordinates)
- ✅ Quality-based effect scaling
- ✅ Multi-graft compatibility tracking
- ✅ Maintenance and degradation framework

**Game-Specific Aspects** (Implemented by Games):
- Graft types (limbs, segments, weapon mods, shield mods)
- Material quality definitions
- Failure consequences (death, infection, malfunction)
- Visual presentation (VFX, animations for grafting)
- Economic pricing (service costs, maintenance fees)

---

## Core Agnostic Components

### GraftableTarget (Body ECS)
```csharp
/// <summary>
/// Agnostic graftable target marker
/// </summary>
public struct GraftableTarget : IComponentData
{
    public byte TargetTypeId;              // Game-defined enum (Entity, Ship, Module)
    public int MaxGrafts;                  // Hard limit
    public int CurrentGrafts;
    public float TotalMassPenalty;         // Cumulative mass from grafts
    public float TotalPowerPenalty;        // Cumulative power from grafts
}
```

### GraftingOperation (Body ECS)
```csharp
/// <summary>
/// Agnostic grafting operation state
/// </summary>
public struct GraftingOperation : IComponentData
{
    public Entity Grafter;
    public Entity Target;
    public Entity GraftAddon;

    public float TimeRemaining;            // Hours
    public float IntegrationQuality;       // 0-100
    public float SuccessChance;            // 0-1
    public bool CanFail;
}
```

### GraftAddon (Mind ECS)
```csharp
/// <summary>
/// Agnostic graft addon definition
/// </summary>
public struct GraftAddon : IComponentData
{
    public byte AddonTypeId;               // Game-defined enum
    public float BaseQuality;              // 0-1 (material quality)
    public float Complexity;               // 1-5

    public float BonusMagnitude;
    public float PenaltyMagnitude;

    public float RequiredSkill;
    public float IntegrationTime;          // Base hours
}
```

### GraftedComponent (Body ECS Buffer)
```csharp
/// <summary>
/// Agnostic graft tracking
/// </summary>
[InternalBufferCapacity(8)]
public struct GraftedComponent : IBufferElementData
{
    public Entity GraftAddon;
    public float IntegrationQuality;       // 0-100
    public uint GraftedTick;
    public Entity Grafter;

    public float CurrentBonus;
    public float CurrentPenalty;
    public bool RequiresMaintenance;
    public float MaintenanceCost;
}
```

### GrafterExperience (Mind ECS)
```csharp
/// <summary>
/// Agnostic grafter skill tracking
/// </summary>
public struct GrafterExperience : IComponentData
{
    public byte GrafterTypeId;             // Game-defined enum
    public float Experience;               // 0-100
    public int SuccessfulGrafts;
    public int FailedGrafts;
    public float AverageQuality;

    // Corporate/hierarchical grafters
    public bool IsCorporateGrafter;
    public int ConstituentCount;
    public float AvgConstituentSkill;
}
```

---

## Agnostic Algorithms

### Integration Quality Calculation
```csharp
/// <summary>
/// Calculate integration quality from multiple factors
/// Agnostic: Weighted formula with grafter skill, subordinates, materials, facility
/// </summary>
public static float CalculateIntegrationQuality(
    float grafterExperience,     // 0-100
    bool isCorporate,
    float avgSubordinateSkill,   // 0-100 (if corporate)
    float materialQuality,       // 0-1
    float addonComplexity,       // 1-5
    float facilityBonus)         // 0-0.3
{
    // Grafter contributes 50%
    float grafterBonus = grafterExperience * 0.5f;

    // Subordinates contribute 20% (for corporate)
    float subordinateBonus = 0f;
    if (isCorporate)
    {
        subordinateBonus = avgSubordinateSkill * 0.2f;
    }

    // Materials contribute 20%
    float materialBonus = materialQuality * 100f * 0.2f;

    // Facility bonus (0-30%)
    float facilityBonusPercent = facilityBonus * 100f;

    // Complexity penalty
    float complexityPenalty = (addonComplexity - 1f) * 5f;

    float quality = grafterBonus + subordinateBonus + materialBonus + facilityBonusPercent - complexityPenalty;
    return math.clamp(quality, 0f, 100f);
}
```

### Integration Time Calculation
```csharp
/// <summary>
/// Calculate integration time based on grafter skill
/// Agnostic: Skill reduces time, facility reduces time
/// </summary>
public static float CalculateIntegrationTime(
    float baseTime,              // Base hours
    float grafterExperience,     // 0-100
    float facilityBonus)         // 0-0.3
{
    // Skill modifier (2.0x at skill 0, 0.5x at skill 100)
    float skillModifier = 2.0f - (grafterExperience / 100f) * 1.5f;

    // Facility reduces time
    float timeReduction = 1f - facilityBonus;

    float finalTime = baseTime * skillModifier * timeReduction;
    return math.max(finalTime, baseTime * 0.3f); // Min 30% of base
}
```

### Quality-Based Effect Scaling
```csharp
/// <summary>
/// Scale addon effects based on integration quality
/// Agnostic: Quality improves bonuses, reduces penalties
/// </summary>
public static (float bonus, float penalty) CalculateEffectiveStats(
    float baseBonusMagnitude,
    float basePenaltyMagnitude,
    float integrationQuality)    // 0-100
{
    // Quality modifier (0.6 at 0%, 1.05 at 100%)
    float qualityModifier = 0.6f + (integrationQuality / 100f) * 0.45f;

    // Bonus scales UP with quality
    float effectiveBonus = baseBonusMagnitude * qualityModifier;

    // Penalty scales DOWN with quality (inverse)
    float penaltyModifier = 1.6f - qualityModifier;
    float effectivePenalty = basePenaltyMagnitude * penaltyModifier;

    return (effectiveBonus, effectivePenalty);
}
```

### Success Chance Calculation
```csharp
/// <summary>
/// Calculate success chance for operations that can fail
/// Agnostic: Skill, materials, target condition, existing grafts
/// </summary>
public static float CalculateSuccessChance(
    float grafterSkill,          // 0-100
    float materialQuality,       // 0-1
    float targetCondition,       // 0-1 (health, integrity, etc.)
    int existingGrafts)
{
    float baseChance = 0.6f;

    // Skill bonus (up to +30%)
    float skillBonus = (grafterSkill / 100f) * 0.3f;

    // Material bonus (up to +10%)
    float materialBonus = materialQuality * 0.1f;

    // Condition factor (-20% to +0%)
    float conditionFactor = (targetCondition - 0.5f) * 0.4f;

    // Multiple graft penalty (-5% per existing)
    float graftPenalty = existingGrafts * 0.05f;

    float successChance = baseChance + skillBonus + materialBonus + conditionFactor - graftPenalty;
    return math.clamp(successChance, 0.3f, 0.98f);
}
```

### Multi-Graft Compatibility Penalty
```csharp
/// <summary>
/// Calculate quality penalty for multiple grafts
/// Agnostic: Diminishing returns
/// </summary>
public static float CalculateCompatibilityPenalty(int existingGrafts)
{
    // First graft: 0% penalty
    // Second graft: -2% quality
    // Third graft: -4% quality
    // Fourth graft: -6% quality, etc.
    return existingGrafts * 0.02f;
}
```

### Degradation Rate Calculation
```csharp
/// <summary>
/// Calculate degradation rate for low-quality grafts
/// Agnostic: Poor quality = faster degradation
/// </summary>
public static float CalculateDegradationRate(float integrationQuality)
{
    if (integrationQuality >= 70f)
        return 0f; // No degradation above 70% quality

    // Degradation rate increases as quality drops
    // 0% quality = 5% degradation per month
    // 50% quality = 1% degradation per month
    float qualityDeficit = 70f - integrationQuality;
    return (qualityDeficit / 70f) * 0.05f; // 0-5% per month
}
```

### Infrastructure Quality (Three-Tier System)

```csharp
/// <summary>
/// Calculate final performance from component + integration + infrastructure
/// Agnostic: Weakest link determines final performance
/// </summary>
public static float CalculateFinalPerformance(
    float componentQuality,      // 0-1 (material quality)
    float integrationQuality,    // 0-1 (grafter skill result)
    float infrastructureQuality, // 0-1 (supporting systems)
    float bonusMagnitude)        // Base bonus value
{
    // Each tier scales the bonus
    float componentModifier = 0.6f + (componentQuality * 0.45f);    // 0.6 to 1.05
    float integrationModifier = 0.6f + (integrationQuality * 0.45f); // 0.6 to 1.05
    float infrastructureModifier = infrastructureQuality;             // 0 to 1 (hard cap)

    // Weakest link: infrastructure can bottleneck everything
    float finalBonus = bonusMagnitude * componentModifier * integrationModifier * infrastructureModifier;

    return finalBonus;
}

/// <summary>
/// Detect infrastructure quality from supporting systems
/// Agnostic: Games define specific infrastructure checks
/// </summary>
public static float CalculateInfrastructureQuality(
    Entity target,
    bool requiresPower,
    bool requiresStructuralSupport,
    bool requiresBiologicalSupport,
    bool requiresMounting)
{
    float infrastructureQuality = 1.0f; // Default: no penalty

    // Games implement these checks based on their infrastructure systems
    // Returns minimum quality across all required infrastructure types
    // Example: Power infrastructure, structural integrity, mounting hardware

    return infrastructureQuality;
}
```

**Infrastructure Types** (Game-Defined):
- **Power Infrastructure:** Conduits, generators, capacitors (affects weapons, shields, engines)
- **Structural Infrastructure:** Hull integrity, foundations, mounting brackets (affects segments, buildings)
- **Biological Infrastructure:** Nutrition, circulation, nervous system (affects entity grafts)
- **Mounting Infrastructure:** Hardware quality, alignment, straps/fittings (affects equipment, armor)

**Bottleneck Mechanic:**
Even legendary components with perfect integration underperform if infrastructure is poor. A 100% quality weapon on a ship with 30% quality power conduits performs at 30% potential.

---

## Extension Points for Games

### 1. Graft Target Type Definitions
Games define what can be grafted:
```csharp
// Godgame example
public enum GodgameGraftTargetType : byte
{
    Entity,
    Creature,
    Golem,
}

// Space4X example
public enum Space4XGraftTargetType : byte
{
    Ship,
    Module,
    Weapon,
    Shield,
}
```

### 2. Graft Addon Type Definitions
Games define graft addon types:
```csharp
// Godgame example
public enum GodgameGraftAddonType : byte
{
    Limb_Arm,
    Limb_Leg,
    Limb_Tail,
    Limb_Wing,
    Augment_Eyes,
    Augment_Bones,
    Augment_Muscles,
}

// Space4X example
public enum Space4XGraftAddonType : byte
{
    Segment_Cargo,
    Segment_Weapon,
    WeaponMod_CapacitorBank,
    WeaponMod_CoolingSystem,
    ShieldMod_RechargeAccelerator,
}
```

### 3. Grafter Type Definitions
Games define grafter types:
```csharp
// Godgame example
public enum GodgameGrafterType : byte
{
    Surgeon,
    Enchanter,
    Alchemist,
    CyberneticSpecialist,
}

// Space4X example
public enum Space4XGrafterType : byte
{
    Shipyard,
    MilitaryContractor,
    Armorer,
    EngineTech,
}
```

### 4. Failure Consequence Definitions
Games define what happens on failure:
```csharp
// Godgame example
public enum GraftFailureType : byte
{
    Rejection,       // Damage, addon destroyed
    Infection,       // Health drain debuff
    PartialSuccess,  // 50% effectiveness
    PermanentDamage, // Stat loss
    Death,           // Critical failure
}

// Space4X example
public enum GraftFailureType : byte
{
    Malfunction,     // Immediate repair needed
    Degradation,     // Accelerated wear
    PowerSurge,      // Systems damaged
    StructuralWeakness, // Hull integrity penalty
}
```

---

## Integration Requirements

### Systems Games Must Implement

1. **GraftValidationSystem** (Mind ECS, 1 Hz)
   - Validate grafter skill requirements
   - Check material availability
   - Check facility access
   - Check target capacity (max grafts)

2. **GraftQualityCalculationSystem** (Mind ECS, 1 Hz)
   - Calculate integration quality before starting
   - Aggregate subordinate skills (for corporate grafters)
   - Apply facility bonuses

3. **GraftingProgressSystem** (Body ECS, 60 Hz)
   - Update grafting timers
   - Track progress percentage

4. **GraftingCompletionSystem** (Body ECS, 60 Hz)
   - Detect when grafting completes
   - Roll success/failure (if applicable)
   - Apply effects (stats, mass, power)
   - Add to `GraftedComponent` buffer
   - Update grafter experience

5. **GraftMaintenanceSystem** (Mind ECS, 1 Hz)
   - Consume maintenance resources
   - Detect maintenance failures
   - Apply degradation

6. **GrafterExperienceUpdateSystem** (Mind ECS, 1 Hz)
   - Update grafter experience from successes/failures
   - Recalculate average quality
   - Aggregate subordinate skills (corporate grafters)

---

## Data Contracts

Games must provide:
- Graft addon catalog (types, properties, requirements, effects)
- Material quality definitions (common to legendary)
- Facility type definitions (workshops, shipyards, bonuses)
- Grafter type catalog (skills, specializations)
- Failure consequence definitions (what happens on fail)
- Maintenance type definitions (resource costs, intervals)

---

## Game-Specific Implementations

### Godgame (Entity Grafting)
**Full Implementation:** [Grafting_And_Augmentation_System.md](../../../../Godgame/Docs/Concepts/Production/Grafting_And_Augmentation_System.md)

**Graft Types:** Limbs, augments (eyes, bones, muscles, neural)
**Grafters:** Surgeons, enchanters, alchemists, cybernetic specialists
**Can Fail:** Yes (rejection, infection, permanent damage, death)
**Quality Range:** 0-100% (poor to legendary)
**Example:** Dragon arm graft (4 hours, 83% success, +35 STR, fire breath ability)

### Space4X (Ship/Module Grafting)
**Implementation Reference:** TBD

**Graft Types:** Ship segments, weapon mods, shield mods, engine mods
**Grafters:** Shipyards (corporate), military contractors, armorers
**Can Fail:** Rarely (malfunction, degradation, structural weakness)
**Quality Range:** 10-98% (poor emergency work to masterwork)
**Example:** Capacitor bank on laser turret (2.8 hours, +74% volley duration, +26% power)

---

## Performance Targets

**Body ECS (60 Hz) Budget:** 2-3 ms/frame
- Grafting progress: 0.5 ms
- Completion/application: 1.0 ms
- Maintenance checks: 1.0 ms
- Degradation: 0.5 ms

**Mind ECS (1 Hz) Budget:** 20-30 ms/update
- Quality calculation: 10 ms (aggregate subordinate skills)
- Experience updates: 5 ms
- Compatibility checks: 5 ms

**Aggregate ECS (0.2 Hz) Budget:** 30-40 ms/update
- Corporate grafter statistics: 20 ms
- Facility capacity: 10 ms
- Economic pricing: 10 ms

**Optimization Strategies:**
- Quality caching (cache calculations, don't recompute every frame)
- Batch completion (process multiple graft completions per frame)
- LOD maintenance (reduce update frequency for distant grafts)
- Degradation pooling (only update monthly, not every frame)

---

## Testing Guidelines

### Unit Tests (PureDOTS)
- ✅ Quality calculation (grafter + subordinates + materials + facility)
- ✅ Time calculation (skill modifier, facility bonus)
- ✅ Success chance (skill, materials, condition, existing grafts)
- ✅ Effect scaling (quality → bonus/penalty modifiers)
- ✅ Compatibility penalty (multi-graft diminishing returns)
- ✅ Degradation rate (quality → degradation per month)

### Integration Tests (Games)
- Graft completion applies correct stats
- Success/failure rolls work correctly
- Multi-graft compatibility penalties accumulate
- Corporate grafter skill aggregation (shipyard + engineers)
- Maintenance failures cause degradation

---

## Migration Notes

**New Components Required:**
- `GraftableTarget` (Body ECS)
- `GraftingOperation` (Body ECS)
- `GraftAddon` (Mind ECS)
- `GraftedComponent` buffer (Body ECS)
- `GrafterExperience` (Mind ECS)

**Integration with Existing Systems:**
- Crafting system (addon creation)
- Resource system (material consumption, maintenance costs)
- Skill system (grafter experience, subordinate skills)
- Facility system (workshops, shipyards)
- Health/damage system (failure consequences)

---

## Related Documents

**PureDOTS Agnostic:**
- `Docs/Architecture/ThreePillarECS_Architecture.md` - ECS layers (to be created)
- `Docs/BestPractices/DOTS_1_4_Patterns.md` - DOTS coding patterns

**Game Implementations:**
- `Godgame/Docs/Concepts/Production/Grafting_And_Augmentation_System.md` - Full game-side concept
- `Space4X/Docs/Concepts/Production/Ship_Modification.md` - Space variant (to be created)

---

**Last Updated:** 2025-12-07
**Maintainer:** PureDOTS Core Team
**Status:** Awaiting Implementation
