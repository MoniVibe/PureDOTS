# Next Implementation Steps - Entity Agnostic Foundation

**Status:** Implementation Roadmap  
**Category:** PureDOTS Foundation + Game Layers  
**Scope:** Future work to complete entity agnostic design  
**Created:** 2025-01-XX  
**Last Updated:** 2025-01-XX

---

## Summary

PureDOTS foundation is now **agnostic** - both individual and aggregate entities use the same components (`VillagerAlignment`, `VillagerBehavior`, `VillagerInitiativeState`). Game-specific layers (Godgame, Space4X) add presentation and interpretation on top.

---

## Completed Work ✅

### PureDOTS Foundation (Agnostic)

- [x] **Behavior/Alignment Components:** `VillagerBehavior`, `VillagerAlignment` created
- [x] **Initiative System:** `VillagerInitiativeSystem` computes initiative for any entity
- [x] **Grudge System:** `VillagerGrudgeDecaySystem` processes grudges for any entity
- [x] **Combat Personality:** `CombatPersonalitySystem` updates combat AI for any entity
- [x] **Utility Scheduler:** `VillagerUtilityScheduler` includes personality weighting
- [x] **Documentation:** Entity agnostic design documented

### Game-Specific Layers (Documented)

- [x] **Godgame Mapping:** Documented how Godgame maps PureDOTS entities to 3D presentation
- [x] **Space4X Mapping:** Documented how Space4X maps PureDOTS entities to UI/ship representation

---

## Next Steps: PureDOTS Foundation

### 1. Aggregate Computation Systems

**Goal:** Create systems that compute aggregate alignment/behavior from member entities.

**Tasks:**
- [x] Create `AggregateAlignmentComputationSystem`
  - Reads member `VillagerAlignment` components
  - Computes weighted average
  - Updates aggregate `VillagerAlignment` component
- [x] Create `AggregateBehaviorComputationSystem`
  - Reads member `VillagerBehavior` components
  - Computes weighted average
  - Updates aggregate `VillagerBehavior` component
- [x] Create `AggregateInitiativeComputationSystem`
  - Reads aggregate `VillagerBehavior` + member averages
  - Computes aggregate initiative
  - Updates aggregate `VillagerInitiativeState` component

**Dependencies:** None  
**Priority:** High  
**Estimated Effort:** Medium (3 systems, ~2-3 days)

---

### 2. Legacy Component Migration

**Goal:** Migrate legacy aggregate-specific components to unified agnostic components.

- [x] Migrate `VillageAlignmentState` → `VillagerAlignment`
  - Update village systems to use `VillagerAlignment`
  - Add conversion helper functions
  - Deprecate `VillageAlignmentState`
- [x] Migrate `GuildAlignment` → `VillagerAlignment`
  - Update guild systems to use `VillagerAlignment`
  - Remove `GuildAlignment` component

**Dependencies:** Aggregate computation systems (for testing)  
**Priority:** Medium  
**Estimated Effort:** Medium (5-10 systems to update, ~3-5 days)

**See:** `Docs/TODO/Entity_Agnostic_Migration.md` for detailed migration plan

---

### 3. Aggregate Membership Systems

**Goal:** Ensure aggregate membership tracking works with agnostic components.

**Tasks:**
- [ ] Verify `BandMember` buffer works with agnostic components
- [ ] Verify `GuildMember` buffer works with agnostic components
- [ ] Create generic `AggregateMember` buffer (if needed)
- [ ] Update membership systems to read member alignment/behavior

**Dependencies:** None  
**Priority:** Medium  
**Estimated Effort:** Low (verification + minor updates, ~1 day)

---

## Next Steps: Godgame Layer

### 1. Presentation Components

**Goal:** Create Godgame-specific presentation components for PureDOTS entities.

**Tasks:**
- [ ] Create `GodgameVillagerPresentation` component
  - Maps `VillagerAlignment` → visual style (clothing, colors)
  - Maps `VillagerBehavior` → animation style (bold/craven walk)
  - Maps `VillagerInitiativeState` → action frequency display
- [ ] Create `GodgameVillagePresentation` component
  - Maps aggregate `VillagerAlignment` → building style
  - Maps aggregate `VillagerBehavior` → village atmosphere
  - Maps aggregate `VillagerInitiativeState` → expansion visuals
- [ ] Create `GodgameBandPresentation` component
  - Maps aggregate alignment/behavior → formation style
- [ ] Create `GodgameGuildPresentation` component
  - Maps aggregate alignment/behavior → guild hall architecture

**Dependencies:** PureDOTS foundation complete  
**Priority:** High  
**Estimated Effort:** Medium (4 components + systems, ~3-5 days)

---

### 2. Visual Mapping Systems

**Goal:** Create systems that map PureDOTS data to Godgame visuals.

**Tasks:**
- [ ] Create `GodgameAlignmentVisualSystem`
  - Reads `VillagerAlignment` → updates visual style
  - Handles both individuals and aggregates
- [ ] Create `GodgameBehaviorVisualSystem`
  - Reads `VillagerBehavior` → updates animation style
  - Handles both individuals and aggregates
- [ ] Create `GodgameInitiativeVisualSystem`
  - Reads `VillagerInitiativeState` → updates action frequency display
  - Handles both individuals and aggregates

**Dependencies:** Presentation components  
**Priority:** High  
**Estimated Effort:** Medium (3 systems, ~2-3 days)

---

### 3. Interaction Systems

**Goal:** Wire up player interaction with PureDOTS entities.

**Tasks:**
- [ ] Create `GodgameEntitySelectionSystem`
  - Handles selection of individuals and aggregates
  - Reads PureDOTS components for tooltip data
- [ ] Create `GodgameEntityCommandSystem`
  - Handles commands to individuals and aggregates
  - Translates commands to PureDOTS actions
- [ ] Create `GodgameEntityTooltipSystem`
  - Displays PureDOTS alignment/behavior/initiative in tooltips
  - Handles both individuals and aggregates

**Dependencies:** Presentation components  
**Priority:** Medium  
**Estimated Effort:** Medium (3 systems, ~2-3 days)

---

## Next Steps: Space4X Layer

### 1. UI Components

**Goal:** Create Space4X-specific UI components for PureDOTS entities.

**Tasks:**
- [ ] Create `Space4XPopUI` component
  - Maps `VillagerAlignment` → pop card display
  - Maps `VillagerBehavior` → pop tooltip data
  - Maps `VillagerInitiativeState` → pop action frequency
- [ ] Create `Space4XPlanetUI` component
  - Maps aggregate `VillagerAlignment` → planet culture display
  - Maps aggregate `VillagerBehavior` → planet policy options
  - Maps aggregate `VillagerInitiativeState` → planet development rate
- [ ] Create `Space4XFleetUI` component
  - Maps aggregate alignment/behavior → fleet tactics display
- [ ] Create `Space4XSectorUI` component
  - Maps aggregate alignment/behavior → sector governance display

**Dependencies:** PureDOTS foundation complete  
**Priority:** High  
**Estimated Effort:** Medium (4 components + systems, ~3-5 days)

---

### 2. Ship Assignment Systems

**Goal:** Create systems that assign pops to ships and represent them via ships.

**Tasks:**
- [ ] Create `Space4XShipAssignmentSystem`
  - Assigns pops to ships (pilot, crew, captain)
  - Links pop `VillagerAlignment`/`VillagerBehavior` to ship
- [ ] Create `Space4XChildVesselSystem`
  - Links commanding officers to child vessels
  - Represents captains via their vessels
- [ ] Create `Space4XShipRepresentationSystem`
  - Updates ship visuals based on assigned pop alignment/behavior
  - Handles both individual ships and aggregate fleets

**Dependencies:** UI components  
**Priority:** High  
**Estimated Effort:** Medium (3 systems, ~2-3 days)

---

### 3. UI Rendering Systems

**Goal:** Create systems that render UI panels showing PureDOTS data.

**Tasks:**
- [ ] Create `Space4XPopCardRenderingSystem`
  - Renders pop cards with alignment/behavior data
  - Updates tooltips with initiative state
- [ ] Create `Space4XPlanetPanelRenderingSystem`
  - Renders planet panel with aggregate alignment/behavior
  - Updates culture display
- [ ] Create `Space4XFleetPanelRenderingSystem`
  - Renders fleet panel with aggregate alignment/behavior
  - Updates tactics display

**Dependencies:** UI components  
**Priority:** Medium  
**Estimated Effort:** Medium (3 systems, ~2-3 days)

---

## Dependencies & Ordering

### Phase 1: PureDOTS Foundation (Current)
1. ✅ Behavior/alignment components created
2. ✅ Initiative/grudge systems created
3. ⏳ Aggregate computation systems (next)
4. ⏳ Legacy component migration (after computation systems)

### Phase 2: Game-Specific Layers (After Foundation)
1. ⏳ Godgame presentation components
2. ⏳ Godgame visual mapping systems
3. ⏳ Godgame interaction systems
4. ⏳ Space4X UI components
5. ⏳ Space4X ship assignment systems
6. ⏳ Space4X UI rendering systems

---

## Open Questions

### For PureDOTS Foundation

1. **Aggregate Computation Frequency:** How often should aggregates recompute alignment/behavior from members?
   - Every tick? Every N ticks? On member change?
2. **Weighting Function:** How should member influence weights be calculated?
   - Equal weight? Leadership weight? Seniority weight?
3. **Aggregate Grudges:** Can aggregates hold grudges against other aggregates?
   - Yes (same as individuals) or No (only individuals)?

### For Godgame Layer

1. **Visual Update Frequency:** How often should visuals update when alignment/behavior changes?
   - Immediate? Gradual lerp? On threshold?
2. **Aggregate Visual Style:** How should aggregate visuals differ from individual visuals?
   - Same style scaled up? Different style entirely?

### For Space4X Layer

1. **Pop Visibility:** When should pops be visible in UI vs hidden?
   - Always visible? Only when assigned to ship? Only when selected?
2. **Ship Representation:** How should pop alignment/behavior affect ship visuals?
   - Ship color? Ship behavior? Ship stats?

---

## Success Criteria

### PureDOTS Foundation

- [ ] All entities (individuals and aggregates) use same components
- [ ] Aggregate computation systems work correctly
- [ ] Legacy components migrated
- [ ] Tests pass for both individuals and aggregates

### Godgame Layer

- [ ] Individual villagers have 3D presentation
- [ ] Aggregate villages/bands/guilds have aggregate presentation
- [ ] Visuals update based on PureDOTS alignment/behavior
- [ ] Player can interact with both individuals and aggregates

### Space4X Layer

- [ ] Individual pops visible in UI
- [ ] Pops represented via ships
- [ ] Aggregate planets/fleets/sectors visible in UI
- [ ] UI displays PureDOTS alignment/behavior data

---

## Related Documentation

- Entity Agnostic Design: `Docs/Concepts/Entity_Agnostic_Design.md`
- Godgame Entity Mapping: `Godgame/Docs/Guides/Godgame_PureDOTS_Entity_Mapping.md`
- Space4X Entity Mapping: `Space4X/Docs/Guides/Space4X_PureDOTS_Entity_Mapping.md`
- Entity Agnostic Migration: `Docs/TODO/Entity_Agnostic_Migration.md`

---

**Last Updated:** 2025-01-XX  
**Status:** Implementation Roadmap - Ready for Execution

