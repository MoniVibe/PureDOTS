# Extension Request: Focus System (Combat Resource) Migration

**Status**: `[PENDING]`  
**Submitted**: 2025-11-27  
**Game Project**: Godgame (game-agnostic, reusable)  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Godgame has implemented a comprehensive **Focus** combat resource system that governs ability activation, exhaustion, and profession-based tradeoffs. This system is **game-agnostic** and should be moved to `com.moni.puredots` so both Space4X and Godgame (and future projects) can consume it.

The Focus system provides:
- Core resource pool with regeneration and exhaustion mechanics
- 24+ Focus ability types across 5 archetypes (Finesse/Physique/Arcane)
- 40+ profession-specific abilities with tradeoff calculations
- Profession skill integration for crafting/gathering/healing/teaching/refining

**Current Location:** `Assets/Scripts/Godgame/Combat/` (fully implemented and tested)

---

## Proposed Solution

**Extension Type**: New System (Migration from Godgame)

**Details:**

Move the entire Focus system from Godgame to PureDOTS as a reusable game-agnostic system.

### Components to Move

1. **Core Focus Components:**
   - `EntityFocus` - Focus pool (Current, Max, RegenRate, ExhaustionLevel, InComa)
   - `CombatStats` - Ability unlock gating (Physique, Finesse, Intelligence, Will, Wisdom)
   - `FocusConfig` - Global configuration singleton

2. **Ability Tracking:**
   - `FocusAbilityType` enum (24 abilities: Finesse 1-8, Physique 9-16, Arcane 17-24)
   - `ActiveFocusAbility` buffer element (Type, Duration, DrainPerSecond, EffectMagnitude)

3. **State Tags:**
   - `FocusComaTag` - Entity is completely depleted
   - `FocusBreakdownRiskTag` - Entity at risk of breakdown

4. **Profession Focus:**
   - `ProfessionType` enum (35+ professions)
   - `ProfessionSkills` - Skill levels per archetype
   - `ProfessionFocusModifiers` - Active ability modifiers
   - `ProfessionFocusConfig` - Global profession settings
   - `ProfessionAbilityType` enum (40+ profession abilities)

### Systems to Move

1. **FocusRegenSystem** - Regenerates focus over time, applies resting multiplier
2. **FocusAbilitySystem** - Activates abilities, drains focus, manages durations
3. **FocusExhaustionSystem** - Tracks exhaustion, triggers breakdown/coma

### Helper Utilities to Move

1. **FocusEffectHelpers** - Query active abilities for combat modifiers
2. **FocusExhaustionHelpers** - Query exhaustion state for effectiveness/cost multipliers
3. **ProfessionFocusHelpers** - Tradeoff calculations, quality/speed/waste formulas
4. **ProfessionFocusIntegration** - Static helpers for job systems integration

### File Structure in PureDOTS

```
Packages/com.moni.puredots/
├── Runtime/
│   ├── Components/
│   │   └── Focus/
│   │       ├── EntityFocus.cs
│   │       ├── CombatStats.cs
│   │       ├── FocusConfig.cs
│   │       ├── FocusAbilityType.cs
│   │       ├── ActiveFocusAbility.cs
│   │       ├── FocusComaTag.cs
│   │       ├── FocusBreakdownRiskTag.cs
│   │       ├── ProfessionType.cs
│   │       ├── ProfessionSkills.cs
│   │       ├── ProfessionFocusModifiers.cs
│   │       ├── ProfessionFocusConfig.cs
│   │       └── ProfessionAbilityType.cs
│   ├── Systems/
│   │   └── Focus/
│   │       ├── FocusRegenSystem.cs
│   │       ├── FocusAbilitySystem.cs
│   │       └── FocusExhaustionSystem.cs
│   └── Helpers/
│       ├── FocusEffectHelpers.cs
│       ├── FocusExhaustionHelpers.cs
│       ├── ProfessionFocusHelpers.cs
│       └── ProfessionFocusIntegration.cs
└── Tests/
    └── Runtime/
        └── Focus/
            ├── FocusRegenTests.cs
            ├── FocusAbilityTests.cs
            ├── FocusExhaustionTests.cs
            └── ProfessionFocusTests.cs
```

---

## Impact Assessment

**Files/Systems Affected:**

**New Files to Create:**
- ~15 component files in `Runtime/Components/Focus/`
- 3 system files in `Runtime/Systems/Focus/`
- 4 helper files in `Runtime/Helpers/`
- 4 test files in `Tests/Runtime/Focus/`

**Breaking Changes:**
- No - this is a new system addition
- Godgame will migrate from local implementation to PureDOTS reference
- Space4X can adopt the same system

**Migration Path:**
1. Copy all files from Godgame to PureDOTS
2. Update namespaces from `Godgame.Combat` to `PureDOTS.Runtime.Components`
3. Godgame removes local duplicates and references PureDOTS
4. Space4X can now use the same system

---

## Example Usage

```csharp
// In game authoring (Godgame or Space4X)
var focusAuthoring = new EntityFocus
{
    Current = 100f,
    Max = 100f,
    RegenRate = 5f,
    ExhaustionLevel = 0f,
    InComa = false
};

// In game system
if (SystemAPI.HasComponent<EntityFocus>(entity))
{
    var focus = SystemAPI.GetComponent<EntityFocus>(entity);
    if (focus.Current >= abilityCost)
    {
        // Activate ability
        var ability = new ActiveFocusAbility
        {
            Type = FocusAbilityType.PowerStrike,
            RemainingDuration = 0f, // Instant
            DrainPerSecond = 0f,
            EffectMagnitude = 1.5f
        };
        // Add to buffer, drain focus, etc.
    }
}

// Profession integration
var quality = ProfessionFocusHelpers.CalculateQuality(
    skills, modifiers, config, ProfessionType.Blacksmith);
```

---

## Alternative Approaches Considered

- **Alternative 1**: Keep system in Godgame only
  - **Rejected**: System is game-agnostic and would benefit Space4X and future projects

- **Alternative 2**: Create minimal interface, keep implementation in games
  - **Rejected**: Full system is needed for consistency and shared balancing

- **Alternative 3**: Split into multiple smaller requests
  - **Considered**: Could split core focus vs profession focus, but they're tightly coupled

---

## Implementation Notes

**Source Code Location:**
- `Assets/Scripts/Godgame/Combat/` - All components, systems, and helpers

**Key Requirements:**
- All systems must be Burst-compatible
- All components must be blittable (no managed types)
- Helper methods must work in Burst context
- Ability definitions should be static data (not components)

**Testing Requirements:**
- Unit tests for focus regen, exhaustion, ability activation
- Profession skill/ability integration tests
- Burst compilation verification
- Cross-game compatibility tests

**What Stays in Godgame:**
- `FocusAuthoring` / `CombatArchetypeAuthoring` - Game-specific presets
- UI/HUD bindings for focus display
- Godgame-specific ability balancing

**Related Systems (Future Consideration):**
- Target selection utilities
- Range check utilities
- Hit calculation system
- Projectile/AOE resolution
- Combat state machine

---

## Review Notes

*(This section is for PureDOTS team use)*

**Reviewer**: {TBD}  
**Review Date**: {TBD}  
**Decision**: {PENDING}  
**Notes**: {TBD}

