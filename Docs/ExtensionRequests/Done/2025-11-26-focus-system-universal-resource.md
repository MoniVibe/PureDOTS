# Extension Request: Focus System - Universal Excellence Resource

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P1  
**Assigned To**: PureDOTS Team

---

## Use Case

Both Godgame and Space4X need a universal "focus" resource system that enables entities to temporarily boost their effectiveness at specialized activities by spending mental energy. This affects:

- **Combat**: Parry, dodge, crit focus, attack speed, damage reduction, spell empowerment
- **Crafting**: Quality vs quantity tradeoffs (mass production vs masterwork)
- **Gathering**: Speed vs efficiency tradeoffs (fast harvesting vs minimal waste)
- **Healing**: Single target intensive care vs multi-target mass healing
- **Teaching**: Fast intensive lessons vs deep slow teaching
- **Refining**: Rapid processing with waste vs pure extraction slowly

Focus depletes during use and regenerates over time. Exhaustion accumulates when focus is low, potentially causing mental breakdown or coma.

**Key Design**: Using focus increases XP and wisdom gain. Entities who push themselves grow faster than those who relax/socialize. Most entities only use 20-40% of their focus capacity unless motivated by:
- **Survival**: Life-threatening situations (100% usage)
- **Ambition**: Strong goals (60-90% usage)
- **Passion**: Love of their craft (70-100% usage)
- **Desperation**: Debt, threats (80-100% usage)
- **Leisure**: Prefer relaxation (10-30% usage)

---

## Proposed Solution

Move the game-agnostic Focus system from Godgame to PureDOTS.

**Extension Type**: New Components + Systems + Helpers

**Details:**

### Core Components (`Packages/com.moni.puredots/Runtime/Runtime/Combat/`)

```csharp
// EntityFocus - The focus resource pool
public struct EntityFocus : IComponentData
{
    public float CurrentFocus;      // 0 to MaxFocus
    public float MaxFocus;          // Capacity (influenced by Will stat)
    public float BaseRegenRate;     // Per second
    public float TotalDrainRate;    // From active abilities
    public byte ExhaustionLevel;    // 0-100
    public bool IsInComa;
    public FocusArchetype PrimaryArchetype;
}

// FocusArchetype - Categories of focus abilities
public enum FocusArchetype : byte
{
    None = 0,
    // Combat archetypes
    Finesse = 1, Physique = 2, Arcane = 3,
    // Profession archetypes
    Crafting = 10, Gathering = 11, Healing = 12, Teaching = 13, Refining = 14
}

// FocusAbilityType - 60+ abilities across all archetypes
public enum FocusAbilityType : byte
{
    // Combat: Finesse (10-29), Physique (30-49), Arcane (50-69)
    Parry = 10, DualWieldStrike = 11, CriticalFocus = 12, DodgeBoost = 13, ...
    IgnorePain = 30, SweepAttack = 31, AttackSpeedBoost = 32, ...
    SummonBoost = 50, ManaRegen = 51, Multicast = 53, ...
    
    // Crafting (70-89)
    MassProduction = 70, MasterworkFocus = 71, BatchCrafting = 72, ...
    
    // Gathering (90-109)
    SpeedGather = 90, EfficientGather = 91, GatherOverdrive = 92, ...
    
    // Healing (110-129)
    MassHeal = 110, LifeClutch = 111, IntensiveCare = 114, ...
    
    // Teaching (130-149)
    IntensiveLessons = 130, DeepTeaching = 131, GroupInstruction = 132, ...
    
    // Refining (150-169)
    RapidRefine = 150, PureExtraction = 151, BatchRefine = 152, ...
}

// ActiveFocusAbility - Buffer for tracking active abilities
[InternalBufferCapacity(4)]
public struct ActiveFocusAbility : IBufferElementData { ... }

// ProfessionFocusModifiers - Calculated modifiers for job systems
public struct ProfessionFocusModifiers : IComponentData
{
    public float QualityMultiplier;
    public float SpeedMultiplier;
    public float WasteMultiplier;
    public float TargetCountMultiplier;
    public float BonusChance;
}

// FocusMotivation - Why entity pushes themselves
public enum FocusMotivation : byte
{
    Casual, Leisurely, Survival, Ambitious, Passionate, Dutiful, Desperate, Perfectionist
}

// FocusGrowthTracking - XP/wisdom growth from focus usage
public struct FocusGrowthTracking : IComponentData
{
    public float TotalFocusSpent;       // Lifetime focus (influences wisdom)
    public float DailyFocusSpent;       // Resets at dawn
    public float AverageIntensity;      // Rolling average (0-1)
    public ushort HighEffortDays;       // Days with >50% usage
    public ushort LowEffortDays;        // Days with <20% usage
    public FocusMotivation CurrentMotivation;
    public float DrivePersonality;      // 0=lazy, 1=driven
}

// FocusGrowthConfig - XP/wisdom growth rates
public struct FocusGrowthConfig : IComponentData
{
    public float XPPerFocusSpent;       // 0.02 = +2% XP per focus point
    public float WisdomPer1000Focus;    // +1 wisdom per 1000 focus lifetime
    public float HighEffortXPBonus;     // 1.25 = +25% XP on high-effort days
    public float LowEffortXPPenalty;    // 0.75 = -25% XP on lazy days
}
```

### Systems (`Packages/com.moni.puredots/Runtime/Systems/Combat/`)

- `FocusRegenSystem` - Regenerates focus over time
- `FocusAbilitySystem` - Activates/deactivates abilities, drains focus
- `FocusExhaustionSystem` - Tracks exhaustion, triggers breakdown/coma
- `ProfessionFocusModifierSystem` - Calculates modifiers from active abilities
- `FocusGrowthSystem` - Tracks usage, updates intensity averages
- `FocusMotivationSystem` - Determines motivation from circumstances

### Static Helpers

- `FocusAbilityDefinitions` - Costs, effects, unlock requirements
- `FocusEffectHelpers` - Query combat modifiers (attack speed, dodge, crit)
- `ProfessionFocusHelpers` - Tradeoff calculations, quality/speed/waste formulas
- `ProfessionFocusIntegration` - Helpers for job systems
- `FocusGrowthHelpers` - XP bonus calculation, wisdom from focus, daily tracking
- `FocusXPIntegration` - Applies focus bonuses to XP awards

---

## Impact Assessment

**Files/Systems Affected:**
- New directory: `Packages/com.moni.puredots/Runtime/Runtime/Combat/`
- New directory: `Packages/com.moni.puredots/Runtime/Systems/Combat/`
- Assembly definition updates for new namespaces

**Breaking Changes:**
- No breaking changes - entirely new feature
- Games can opt-in by adding components to their entities

---

## Example Usage

```csharp
// === Space4X: Crew member using focus for repairs ===
// Crew with high crafting skill activates MasterworkFocus
var request = new FocusAbilityRequest { RequestedAbility = FocusAbilityType.MasterworkFocus };
EntityManager.AddComponentData(crewEntity, request);

// Repair job reads modifiers
var mods = EntityManager.GetComponentData<ProfessionFocusModifiers>(crewEntity);
float repairQuality = baseQuality * mods.QualityMultiplier; // +50% quality
float repairTime = baseTime / mods.SpeedMultiplier;         // 2x longer

// === Godgame: Villager blacksmith mass producing ===
var request = new FocusAbilityRequest { RequestedAbility = FocusAbilityType.MassProduction };
EntityManager.AddComponentData(blacksmithEntity, request);

// Crafting job uses integration helper
var (quantity, quality, waste) = ProfessionFocusIntegration.ApplyCraftingModifiers(
    baseQuantity: 1, baseQuality: 70, baseMaterialCost: 100, baseWasteRate: 0.1f,
    mods, config, randomSeed);
// Result: 2 items at 49 quality (quantity doubled, quality reduced)

// === Combat: Rogue using focus for precision ===
var buffer = EntityManager.GetBuffer<ActiveFocusAbility>(rogueEntity);
float attackSpeed = FocusEffectHelpers.GetAttackSpeedMultiplier(buffer); // 2x with DualWieldStrike
float dodgeBonus = FocusEffectHelpers.GetDodgeBonus(buffer);             // +40% with DodgeBoost

// === Growth: XP bonus from pushing through focus ===
var tracking = EntityManager.GetComponentData<FocusGrowthTracking>(entity);
var focus = EntityManager.GetComponentData<EntityFocus>(entity);
var growthConfig = SystemAPI.GetSingleton<FocusGrowthConfig>();

// Award XP with focus bonus
uint baseXP = 100;
float focusUsedOnAction = 15f; // Focus spent crafting this item
uint finalXP = FocusXPIntegration.ApplyFocusXPBonus(baseXP, focusUsedOnAction, tracking, focus, growthConfig);
// Ambitious craftsman: 100 base + 3 focus bonus * 1.25 daily modifier = ~128 XP

// === Growth: Wisdom from lifetime focus ===
byte wisdomBonus = FocusXPIntegration.GetWisdomBonus(tracking, growthConfig);
// After 10,000 focus spent lifetime: +10 wisdom
```

---

## Alternative Approaches Considered

- **Alternative 1**: Keep Focus system in each game project
  - **Rejected**: Duplicates code, divergent implementations, harder to maintain
  
- **Alternative 2**: Simple stamina/energy system without abilities
  - **Rejected**: Doesn't capture the quality vs quantity tradeoffs that make Focus interesting
  
- **Alternative 3**: Status effect-based approach
  - **Rejected**: Focus abilities need continuous drain and activation tracking that status effects don't provide

---

## Implementation Notes

**Reference Implementation**: `Godgame/Assets/Scripts/Godgame/Combat/`
- `FocusComponents.cs` - All component definitions
- `FocusAbilityDefinitions.cs` - Static ability data
- `FocusRegenSystem.cs`, `FocusAbilitySystem.cs`, `FocusExhaustionSystem.cs`
- `ProfessionFocusComponents.cs`, `ProfessionFocusSystem.cs`
- `FocusSystemTests.cs`, `ProfessionFocusTests.cs` - 65+ unit tests

**Dependencies:**
- `TimeState` for tick-based updates
- Integration with existing stat systems (CombatStats, ProfessionSkills)

**Performance Considerations:**
- All systems are Burst-compiled
- `FocusAbilityDefinitions` uses switch expressions for efficient lookups
- Modifiers are cached in `ProfessionFocusModifiers` to avoid per-frame recalculation

**Testing Requirements:**
- Port existing tests from Godgame
- Add integration tests with Space4X crew/repair systems

---

## Review Notes

*(This section is for PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

