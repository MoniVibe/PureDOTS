# Reaction System (Agnostic Framework)

**Status:** Concept Design
**Category:** Core Combat Framework
**Shareability:** `shared-core`
**Last Updated:** 2025-12-07

---

## Overview

The **Reaction System** provides an agnostic framework for perception-based combat responses. Games implement reaction mechanics by extending core components with game-specific threat types and response behaviors.

**Agnostic Aspects** (Provided by PureDOTS):
- ✅ Reaction time calculation framework (Finesse/Wisdom/Experience formulas)
- ✅ Threat perception buffer structure
- ✅ Spatial dodge trigger mechanics
- ✅ Telegraph detection system
- ✅ Per-limb reaction speed tracking
- ✅ Focus modulation framework (penalty reduction curves)
- ✅ Combat preference learning system (Mind ECS)

**Game-Specific Aspects** (Implemented by Games):
- Threat types (melee swing, projectile, spell, trap, environmental hazard)
- Dodge animations and VFX
- Weapon/armor/style preferences (weapon catalogs)
- Combat doctrine types (trapper, duelist, battlemaster)
- Spatial dodge distance/duration tuning

---

## Core Agnostic Components

### ReactionTime (Body ECS)
```csharp
/// <summary>
/// Agnostic reaction time component
/// </summary>
public struct ReactionTime : IComponentData
{
    public float BaseReactionTime;    // 0.1s to 3.0s
    public float CurrentReactionTime; // Modified by exhaustion, wounds
    public float PerceptionRange;     // Detection radius
    public uint LastDetectionTick;
}
```

### ThreatPerception (Body ECS Buffer)
```csharp
/// <summary>
/// Agnostic threat detection buffer
/// Games populate with game-specific threat types
/// </summary>
[InternalBufferCapacity(8)]
public struct ThreatPerception : IBufferElementData
{
    public Entity ThreatSource;
    public byte ThreatTypeId;         // Game-defined enum
    public float ProjectedDamage;
    public float TimeToImpact;
    public float AttackSpeed;
    public float3 ThreatVector;
    public uint DetectionTick;
    public bool CanDodge;
}
```

### SpatialDodgeTrigger (Body ECS)
```csharp
/// <summary>
/// Agnostic spatial dodge trigger
/// </summary>
public struct SpatialDodgeTrigger : IComponentData
{
    public float DamageThreshold;     // Trigger threshold
    public float MinTimeToImpact;     // Reaction window
    public float DodgeSuccessModifier;
    public uint DodgeCooldown;
    public bool IsDodging;
}
```

### CombatPreferences (Mind ECS)
```csharp
/// <summary>
/// Agnostic combat preference framework
/// Games define weapon/armor/style enums
/// </summary>
public struct CombatPreferences : IComponentData
{
    public byte PreferredWeaponId;    // Game-defined enum
    public byte PreferredArmorId;     // Game-defined enum
    public byte PreferredStyleId;     // Game-defined enum
    public byte PreferredModulationId;
    public float ConfidenceLevel;     // 0-1
}
```

---

## Agnostic Formulas

### Reaction Time Calculation
```csharp
/// <summary>
/// Agnostic reaction time formula
/// Games provide stat values, framework calculates reaction time
/// </summary>
public static float CalculateBaseReactionTime(
    float finesse,        // 0-100 (Physical reflexes)
    float wisdom,         // 0-100 (Predictive ability)
    int experienceLevel,  // 0-100 (Pattern recognition)
    int age)              // Age in years
{
    float finesseBonus = (finesse / 100f) * 0.6f;
    float wisdomBonus = (wisdom / 100f) * 0.3f;
    float expBonus = (experienceLevel / 100f) * 0.1f;

    float agePenalty = 0f;
    if (age > 40)
        agePenalty = math.min((age - 40) / 100f, 0.3f);
    else if (age < 18)
        agePenalty = (18 - age) / 60f;

    float totalBonus = finesseBonus + wisdomBonus + expBonus;
    float reactionTime = 2.0f - (totalBonus * 1.9f) + agePenalty;

    return math.clamp(reactionTime, 0.1f, 3.0f);
}
```

### Dodge Chance Calculation
```csharp
/// <summary>
/// Agnostic dodge chance formula
/// </summary>
public static float CalculateDodgeChance(
    float attackSpeed,
    float timeToImpact,
    float reactionTime,
    float dodgeModifier)
{
    float speedPenalty = math.clamp(attackSpeed / 50f, 0.5f, 2.0f);
    float timeBonus = math.clamp(timeToImpact / reactionTime, 0.5f, 2.0f);
    float baseChance = 0.5f;
    float dodgeChance = baseChance * dodgeModifier * timeBonus / speedPenalty;

    return math.clamp(dodgeChance, 0.05f, 0.95f);
}
```

### Focus Modulation Penalty Reduction
```csharp
/// <summary>
/// Agnostic penalty reduction curve
/// Master skill level reduces/eliminates penalties
/// </summary>
public static float CalculatePenaltyReduction(float skillLevel)
{
    // 0-1 based on skill level (0-100)
    float normalized = skillLevel / 100f;

    // Exponential curve: minimal reduction until skill 50, then rapid improvement
    return math.pow(normalized, 2.0f);
}

public static bool IsMasterLevel(float skillLevel)
{
    return skillLevel >= 90f;
}
```

---

## Extension Points for Games

### 1. Threat Type Definitions
Games define threat type enums:
```csharp
// Godgame example
public enum GodgameThreatType : byte
{
    MeleeSwing,
    MeleeThrust,
    ProjectileSlow,
    ProjectileFast,
    SpellProjectile,
    AoEWindup,
    Trap,
    EnvironmentalHazard,
}

// Space4X example
public enum Space4XThreatType : byte
{
    LaserBeam,
    MissileIncoming,
    BoardingParty,
    ExplosiveDecompression,
    CollisionWarning,
}
```

### 2. Weapon/Armor Preferences
Games define equipment catalogs:
```csharp
// Godgame melee weapons
public enum GodgameWeapon : byte
{
    Dagger, Longsword, Greatsword, Warhammer, Spear,
    ShieldAndSword, DualWield, Bow, Staff, ...
}

// Space4X crew equipment
public enum Space4XWeapon : byte
{
    LaserPistol, LaserRifle, PlasmaCannon,
    Wrench, RepairTool, MedicalKit, ...
}
```

**Note on Armor Materials and Resistances:**
Armor materials determine damage type resistances (integrated with damage type system). Games define armor type enums and resistance profiles. For example:
- Heavy materials (plate, heavy armor) provide high physical resistance but low magic/energy resistance
- Light materials (cloth, energy shields) provide high magic/energy resistance but low physical resistance
- Balanced materials (leather, composite) offer moderate resistance across damage types

See game-specific implementations and damage type system for resistance calculations.

### 3. Combat Style Doctrines
Games define combat style taxonomies:
```csharp
// Godgame individual styles
public enum GodgameCombatStyle : byte
{
    Duelist, Brawler, Defender, Skirmisher,
    Assassin, Berserker, Trapper, Battlemaster, ...
}

// Space4X crew roles
public enum Space4XCombatRole : byte
{
    Marine, Engineer, Medic, Pilot,
    Boarding, Defense, Support, ...
}
```

---

## Integration Requirements

### Systems Games Must Implement

1. **ThreatDetectionSystem** (Body ECS, 60 Hz)
   - Populate `ThreatPerception` buffer with game-specific threats
   - Raycast for projectiles, detect melee swings, etc.

2. **SpatialDodgeSystem** (Body ECS, 60 Hz)
   - Apply physics impulse for dodge movement
   - Consume stamina
   - Trigger game-specific animations/VFX

3. **TelegraphReadSystem** (Body ECS, 60 Hz)
   - Detect slow attack wind-ups
   - Modify threat time-to-impact based on Wisdom

4. **CombatPreferenceLearningSystem** (Mind ECS, 1 Hz)
   - Analyze combat history
   - Update weapon/armor/style preferences
   - Calculate confidence levels

### Data Contracts

Games must provide:
- Combat history tracking (win/loss, weapon used, tactic effectiveness)
- Equipment catalogs (weapons, armor, effects)
- Style taxonomy (duelist, trapper, etc.)
- Animation/VFX hooks for dodge/parry/block

---

## Game-Specific Implementations

### Godgame (Melee + Magic)
**Full Implementation:** [Reaction_Based_Combat_System.md](../../../../Docs/Features/Reaction_Based_Combat_System.md)

**Threat Types:** Melee swings, projectiles (arrows), spells, traps, environmental
**Spatial Dodge:** Sidestep, backflip, roll animations
**Preferences:** Weapon (dagger/sword/warhammer), Armor (cloth/leather/plate), Style (duelist/assassin/berserker)

### Space4X (Ranged + Boarding)
**Implementation Reference:** TBD

**Threat Types:** Laser beams, missiles, boarding parties, collisions, decompression
**Spatial Dodge:** Alcove retreat, corridor sidestep, zero-G evasion
**Preferences:** Weapon (laser/plasma/kinetic), Role (marine/engineer/medic)

---

## Performance Targets

**Body ECS (60 Hz) Budget:** 4-5 ms/frame
- Threat detection: 2.0 ms (raycast queries)
- Reaction speed: 0.5 ms (calculations)
- Spatial dodge: 1.5 ms (physics impulses)
- Telegraph read: 0.3 ms (buffer modifications)

**Mind ECS (1 Hz) Budget:** 70-100 ms/update
- Preference learning: 50 ms (history analysis)
- Reaction improvement: 20 ms (experience modifiers)

---

## Testing Guidelines

### Unit Tests (PureDOTS)
- ✅ Reaction time formula (Finesse/Wisdom/Experience/Age inputs)
- ✅ Dodge chance formula (speed/time/reaction/modifier inputs)
- ✅ Penalty reduction curve (skill level → reduction percentage)
- ✅ Age penalty calculation (young/peak/old)

### Integration Tests (Games)
- Test threat detection accuracy (false positives, misses)
- Test spatial dodge execution (stamina cost, cooldown)
- Test preference learning convergence (100 battles → stable preferences)
- Test focus modulation penalties (skill-based reduction)

---

## Migration Notes

**From Existing Combat Systems:**
- Existing `CombatFocusModifiers` integrates with `FocusCombatModulation`
- Existing `StaminaState` used for dodge costs
- Existing `EntityFocus` used for focus ability drain
- Existing limb systems (`Limb`, `LimbReactionSpeed`) extend with reaction times

**New Components Required:**
- `ReactionTime` (Body ECS)
- `ThreatPerception` buffer (Body ECS)
- `SpatialDodgeTrigger` (Body ECS)
- `TelegraphReading` (Body ECS)
- `CombatPreferences` (Mind ECS)
- `LimbReactionSpeed` (Body ECS, extends `Limb`)

---

## Related Documents

**PureDOTS Agnostic:**
- `Docs/Architecture/ThreePillarECS_Architecture.md` - ECS layer architecture (to be created)
- `Docs/BestPractices/DOTS_1_4_Patterns.md` - DOTS coding patterns
- `Runtime/Focus/FocusComponents.cs` - Focus system integration

**Game Implementations:**
- `Docs/Features/Reaction_Based_Combat_System.md` - Full game-side concept (Godgame/Space4X)
- `PureDOTS/Documentation/DesignNotes/LimbBasedActionSystem.md` - Limb integration
- `PureDOTS/Documentation/DesignNotes/ImpulseAndKnockbackCombat.md` - Physics integration

---

**Last Updated:** 2025-12-07
**Maintainer:** PureDOTS Core Team
**Status:** Awaiting Implementation
