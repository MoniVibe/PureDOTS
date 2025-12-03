# Extension Request: Stats Foundation Components

**Status**: `[RESOLVED]`  
**Submitted**: 2025-11-27  
**Game Project**: Godgame  
**Priority**: P1  
**Assigned To**: TBD

---

## Use Case

Godgame has implemented a comprehensive stat system for villagers (individuals) with runtime DOTS components. Currently, Godgame maintains its own stat components and syncs them to PureDOTS components for registry compatibility. This creates duplication and maintenance overhead.

**Request:** Complete the PureDOTS stat component definitions to match Godgame's requirements, making PureDOTS components the foundation for all game projects.

**Current Issue:** PureDOTS components may not exist or may have incomplete schemas, requiring Godgame to maintain sync systems.

---

## Proposed Solution

**Extension Type**: New Components / Component Schema Completion

**Details:**

Create/complete PureDOTS stat components that match Godgame's expected structure exactly. This allows Godgame to:
1. Remove sync systems (`VillagerPureDOTSSyncSystem`)
2. Use PureDOTS components directly as source of truth
3. Share stat schemas with Space4X and future projects

### Components Required

1. **VillagerNeeds** (`PureDOTS.Runtime.Components.VillagerNeeds`)
   - Fields: `Food`, `Rest`, `Sleep`, `GeneralHealth` (byte 0-100)
   - Fields: `Health`, `MaxHealth`, `Energy` (float)
   - Location: `Packages/com.moni.puredots/Runtime/Components/VillagerNeeds.cs`

2. **VillagerMood** (`PureDOTS.Runtime.Components.VillagerMood`)
   - Fields: `Mood` (float 0-100)
   - Location: `Packages/com.moni.puredots/Runtime/Components/VillagerMood.cs`

3. **VillagerCombatStats** (`PureDOTS.Runtime.Components.VillagerCombatStats`)
   - Fields: `AttackDamage`, `AttackSpeed` (float)
   - Fields: `CurrentTarget` (Entity)
   - Location: `Packages/com.moni.puredots/Runtime/Components/VillagerCombatStats.cs`

**Note:** PureDOTS component focuses on registry/query needs. Game-specific combat stats (Attack, Defense, Health, Stamina, Mana) remain in game-specific components.

### Component Requirements

- **Burst Compatibility:** All components must be Burst-compatible (no managed types, blittable)
- **Field Names:** Must match Godgame's expected structure exactly
- **Field Types:** Must match Godgame's expected types exactly
- **Namespace:** `PureDOTS.Runtime.Components`
- **Documentation:** XML documentation for all fields with value ranges
- **Default Values:** Sensible defaults (e.g., Needs = 100, Mood = 50)

---

## Impact Assessment

**Files/Systems Affected:**

**New Files to Create:**
- `Packages/com.moni.puredots/Runtime/Components/VillagerNeeds.cs`
- `Packages/com.moni.puredots/Runtime/Components/VillagerMood.cs`
- `Packages/com.moni.puredots/Runtime/Components/VillagerCombatStats.cs`
- `Packages/com.moni.puredots/Tests/Runtime/Components/VillagerNeedsTests.cs`
- `Packages/com.moni.puredots/Tests/Runtime/Components/VillagerMoodTests.cs`
- `Packages/com.moni.puredots/Tests/Runtime/Components/VillagerCombatStatsTests.cs`

**Godgame Changes (After PureDOTS Implementation):**
- Remove `VillagerPureDOTSSyncSystem` (no longer needed)
- Update bakers to add PureDOTS components directly
- Update systems to use PureDOTS components as source of truth

**Breaking Changes:**
- No - new components are additive
- Godgame can migrate gradually

---

## Example Usage

```csharp
// In Godgame baker (after migration)
var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

// Add PureDOTS component directly
AddComponent(entity, new PureDOTS.Runtime.Components.VillagerNeeds
{
    Food = 100,
    Rest = 100,
    Sleep = 100,
    GeneralHealth = 100,
    Health = 100f,
    MaxHealth = 100f,
    Energy = 100f
});

// In Godgame system (after migration)
if (SystemAPI.HasComponent<PureDOTS.Runtime.Components.VillagerNeeds>(entity))
{
    var needs = SystemAPI.GetComponent<PureDOTS.Runtime.Components.VillagerNeeds>(entity);
    // Use needs.Health, needs.Energy, etc. directly
}

// Registry sync (uses PureDOTS component directly)
if (_needsLookup.HasComponent(entity))
{
    var needs = _needsLookup[entity];
    mirror.HealthPercent = needs.MaxHealth > 0f
        ? math.saturate(needs.Health / math.max(0.0001f, needs.MaxHealth)) * 100f
        : 0f;
    mirror.EnergyPercent = math.clamp(needs.Energy, 0f, 100f);
}
```

---

## Alternative Approaches Considered

- **Alternative 1**: Keep sync system, don't create PureDOTS components
  - **Rejected**: Creates duplication and maintenance overhead

- **Alternative 2**: Create minimal PureDOTS components, keep game-specific extensions
  - **Partially Viable**: But we need exact field matching for registry compatibility

- **Alternative 3**: Make PureDOTS components more comprehensive
  - **Considered**: But want to keep PureDOTS minimal for cross-game compatibility

---

## Implementation Notes

**Current Godgame Implementation:**
- Location: `Assets/Scripts/Godgame/Villagers/Villager*Components.cs`
- Sync System: `VillagerPureDOTSSyncSystem` copies Godgame → PureDOTS

**Required Schema Matching:**
- Field names must match exactly
- Field types must match exactly
- Component namespace: `PureDOTS.Runtime.Components`

**Testing Requirements:**
- Unit tests for component creation and initialization
- Default value validation
- Burst compilation verification
- Memory layout verification
- Integration tests with `SystemAPI.Query`, `ComponentLookup`, `EntityCommandBuffer`

**Migration Path:**
1. PureDOTS creates components with exact schemas
2. Godgame removes sync system
3. Godgame bakers add PureDOTS components directly
4. Godgame systems use PureDOTS components as source of truth

**Future Consideration:**
- Optional `VillagerAttributes` component (minimal set for cross-game queries)
- Consider adding Strength/Agility/Intelligence as minimal attributes

---

## Review Notes

*(This section is for PureDOTS team use)*

**Reviewer**: {TBD}  
**Review Date**: {TBD}  
**Decision**: {PENDING}  
**Notes**: {TBD}

