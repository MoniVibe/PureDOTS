# Entity Agnostic Migration TODO

**Status:** Migration Plan  
**Category:** PureDOTS Refactoring  
**Scope:** Unifying aggregate and individual entity components  
**Created:** 2025-01-XX  
**Last Updated:** 2025-01-XX

---

## Purpose

This document tracks the migration of legacy aggregate-specific components to the unified agnostic component system.

---

## Current State

### ✅ Unified Components (Already Agnostic)

These components work for both individuals and aggregates:

- **`VillagerAlignment`**: Tri-axis alignment (Moral, Order, Purity)
  - Used by: Individuals ✅, Aggregates ✅
  - Status: **Complete**

- **`VillagerBehavior`**: Personality traits (Vengeful/Forgiving, Bold/Craven)
  - Used by: Individuals ✅, Aggregates ✅
  - Status: **Complete**

- **`VillagerInitiativeState`**: Autonomous action timing
  - Used by: Individuals ✅, Aggregates ✅
  - Status: **Complete**

- **`VillagerGrudge`**: Grudge tracking buffer
  - Used by: Individuals ✅, Aggregates ✅
  - Status: **Complete**

### ⚠️ Legacy Components (Need Migration)

These components are aggregate-specific and should be unified:

1. **`VillageAlignmentState`** (in `VillageBehaviorComponents.cs`)
   - **Current:** Uses different axes: `LawChaos`, `Materialism`, `Integrity`
   - **Target:** Migrate to `VillagerAlignment` (Moral, Order, Purity)
   - **Status:** **Needs Migration**
   - **Priority:** Medium
   - **Dependencies:** Update all systems using `VillageAlignmentState`

2. **`GuildAlignment`** (in `GuildComponents.cs`)
   - **Current:** Uses same axes as `VillagerAlignment` (Moral, Order, Purity) ✅
   - **Target:** Use `VillagerAlignment` directly (remove duplicate)
   - **Status:** **Needs Migration**
   - **Priority:** Low (already compatible, just needs cleanup)
   - **Dependencies:** Update guild systems to use `VillagerAlignment`

---

## Migration Tasks

### Task 1: Migrate VillageAlignmentState → VillagerAlignment

**Steps:**
1. [ ] Map `VillageAlignmentState` axes to `VillagerAlignment`:
   - `LawChaos` → `OrderAxis` (same concept)
   - `Materialism` → `MoralAxis` (materialistic = less good, ascetic = more good)
   - `Integrity` → `PurityAxis` (same concept)

2. [ ] Update systems using `VillageAlignmentState`:
   - `VillageOutlookBootstrapSystem`
   - `VillagerVillageMembershipSystem`
   - `VillageWorkforceDecisionSystem`
   - `VillageWorkforceDemandSystem`
   - `VillageJobPreferenceSystem`

3. [ ] Add migration helper function:
   ```csharp
   public static VillagerAlignment ConvertFromVillageAlignmentState(VillageAlignmentState old)
   {
       return new VillagerAlignment
       {
           OrderAxis = (sbyte)(old.LawChaos * 100),
           MoralAxis = (sbyte)(old.Materialism * -100), // Inverted
           PurityAxis = (sbyte)(old.Integrity * 100),
           AlignmentStrength = 1f,
           LastShiftTick = 0
       };
   }
   ```

4. [ ] Deprecate `VillageAlignmentState` with `[Obsolete]` attribute
5. [ ] Update tests to use `VillagerAlignment`
6. [ ] Remove `VillageAlignmentState` after migration complete

**Estimated Effort:** Medium (5-10 systems to update)

---

### Task 2: Migrate GuildAlignment → VillagerAlignment

**Steps:**
1. [ ] Update guild systems to use `VillagerAlignment` directly:
   - `GuildFormationSystem`
   - Any systems reading `GuildAlignment`

2. [ ] Remove `GuildAlignment` component definition
3. [ ] Update guild authoring/baking to use `VillagerAlignment`
4. [ ] Update tests to use `VillagerAlignment`
5. [ ] Remove `GuildAlignment` after migration complete

**Estimated Effort:** Low (already compatible, just cleanup)

---

## Post-Migration State

### After Migration

**All entities (individuals and aggregates) will use:**
- `VillagerAlignment` for alignment
- `VillagerBehavior` for behavior
- `VillagerInitiativeState` for initiative
- `VillagerGrudge` for grudges

**No aggregate-specific alignment/behavior components.**

**Aggregates compute values from members:**
- Alignment: Weighted average of member alignments
- Behavior: Weighted average of member behaviors
- Initiative: Derived from aggregate behavior + member averages

---

## Benefits

### After Migration

1. **True Agnosticism:** All entities use same components
2. **Simplified Systems:** No special-casing for aggregates
3. **Consistent Behavior:** Same logic for individuals and aggregates
4. **Easier Testing:** Test once, works for both scales
5. **Clearer Documentation:** No confusion about which component to use

---

## Testing Strategy

### Migration Testing

1. **Unit Tests:**
   - Test alignment conversion functions
   - Test aggregate computation from members
   - Test system compatibility

2. **Integration Tests:**
   - Test village systems with `VillagerAlignment`
   - Test guild systems with `VillagerAlignment`
   - Test aggregate behavior computation

3. **Regression Tests:**
   - Ensure existing functionality still works
   - Verify no performance regressions

---

## Timeline

**Phase 1: Preparation (Current)**
- [x] Document current state
- [x] Identify migration targets
- [x] Create migration plan

**Phase 2: Village Migration**
- [ ] Implement conversion functions
- [ ] Update village systems
- [ ] Test migration
- [ ] Deprecate `VillageAlignmentState`

**Phase 3: Guild Migration**
- [ ] Update guild systems
- [ ] Test migration
- [ ] Remove `GuildAlignment`

**Phase 4: Cleanup**
- [ ] Remove deprecated components
- [ ] Update documentation
- [ ] Verify all systems use unified components

---

## Related Documentation

- Entity Agnostic Design: `Docs/Concepts/Entity_Agnostic_Design.md`
- Villager Behavioral Personality: `Docs/Concepts/Villagers/Villager_Behavioral_Personality.md`
- Generalized Alignment Framework: `Docs/Concepts/Meta/Generalized_Alignment_Framework.md`

---

**Last Updated:** 2025-01-XX  
**Status:** Migration Plan - In Progress

