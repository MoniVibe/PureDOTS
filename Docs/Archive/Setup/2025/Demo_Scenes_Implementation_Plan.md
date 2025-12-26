# legacy Scenes Implementation Plan

**Status:** Implementation Plan  
**Date:** 2025-01-27  
**Goal:** Create complete legacy scenes with basic logic loops for both Godgame and Space4x, adhering to DOTS 1.4 and Burst compliance

---

## Current State Assessment

### DOTS 1.4 Compliance ✅

**High Priority Issues:**
- ✅ **ToEntityArray Allocations** - Already fixed! Systems use `IJobEntity` pattern
- ✅ **Non-Burst Input System** - Documented as acceptable (requires managed bridge)
- ✅ **ECB Migration** - Already using singleton ECB systems
- ✅ **Presentation Structural Changes** - Already in SimulationSystemGroup

**Status:** Codebase is DOTS 1.4 compliant! Audit document may be outdated.

### Burst Compliance ✅

- ✅ Most systems have `[BurstCompile]` attribute
- ✅ Hot-path systems are Burst-friendly
- ✅ Presentation systems correctly excluded from Burst
- ✅ Validation tools in place (`BurstValidation.cs`)

---

## legacy Scene Requirements

### Godgame_VillagerDemo Scene

**Location:** `Godgame/Assets/Scenes/Godgame_VillagerDemo.unity`

**Required Logic Loops:**

1. **Resource Gathering Loop**
   - Villagers identify resource nodes
   - Villagers move to nodes
   - Villagers gather resources
   - Resources deposited to storehouses

2. **Job Assignment Loop**
   - Villagers evaluate needs (hunger, energy, health)
   - Villagers assigned to appropriate jobs
   - Villagers execute job tasks
   - Job completion updates villager state

3. **Needs System Loop**
   - Villagers consume resources for needs
   - Needs affect villager mood/status
   - Low needs trigger job changes
   - Needs drive villager behavior

4. **Band Formation Loop** (Basic)
   - Villagers form bands based on proximity/alignment
   - Bands aggregate member behavior
   - Bands coordinate activities
   - Band membership affects individual behavior

**Scene Setup:**
- PureDOTS config bootstrap
- Resource nodes (trees, rocks, etc.)
- Storehouses
- Villager spawners
- Camera setup
- Time controls

**Systems Required:**
- `VillagerNeedsSystem` ✅
- `VillagerStatusSystem` ✅
- `VillagerJobAssignmentSystem` ✅
- `VillagerAISystem` ✅
- `VillagerTargetingSystem` ✅
- `VillagerMovementSystem` ✅
- `ResourceGatheringSystem` ✅
- `BandFormationSystem` ✅ (needs verification)

---

### Space4X_VesselDemo Scene

**Location:** `Space4x/Assets/Scenes/Space4X_VesselDemo.unity`

**Required Logic Loops:**

1. **Mining Loop**
   - Vessels identify resource deposits
   - Vessels move to deposits
   - Vessels mine resources
   - Resources spawn as piles

2. **Hauling Loop**
   - Haulers identify resource piles
   - Haulers move to piles
   - Haulers load cargo
   - Haulers deliver to stations
   - Stations store resources

3. **Fleet Formation Loop** (Basic)
   - Vessels form fleets based on proximity/orders
   - Fleets coordinate movement
   - Fleet membership affects vessel behavior
   - Fleets execute group orders

4. **Crew Aggregation Loop** (Basic)
   - Crews aggregate into departments
   - Department stats affect vessel performance
   - Crew morale affects vessel efficiency
   - Crew fatigue accumulates

**Scene Setup:**
- PureDOTS config bootstrap
- Resource deposits (asteroids, planets)
- Stations (dropoff points)
- Vessel spawners
- Camera setup
- Time controls

**Systems Required:**
- `MiningLoopSystem` ✅
- `HaulingLoopSystem` ✅
- `HaulingJobManagerSystem` ✅
- `HaulingJobPrioritySystem` ✅
- `ResourcePileSystem` ✅
- `Space4XCrewAggregationSystem` ✅ (needs verification)
- Fleet formation systems (may need creation)

---

## Implementation Steps

### Phase 1: Verify Existing Systems

1. **Check System Availability**
   - [ ] Verify all required systems exist and are enabled
   - [ ] Check system group ordering
   - [ ] Verify component requirements

2. **Check Authoring Components**
   - [ ] Verify authoring components exist
   - [ ] Check baker implementations
   - [ ] Verify prefab setups

### Phase 2: Scene Setup

1. **Godgame Scene**
   - [ ] Create/update scene with PureDOTS bootstrap
   - [ ] Add resource nodes
   - [ ] Add storehouses
   - [ ] Add villager spawners
   - [ ] Setup camera
   - [ ] Add time controls

2. **Space4X Scene**
   - [ ] Create/update scene with PureDOTS bootstrap
   - [ ] Add resource deposits
   - [ ] Add stations
   - [ ] Add vessel spawners
   - [ ] Setup camera
   - [ ] Add time controls

### Phase 3: Logic Loop Verification

1. **Godgame Loops**
   - [ ] Test resource gathering
   - [ ] Test job assignment
   - [ ] Test needs system
   - [ ] Test band formation

2. **Space4X Loops**
   - [ ] Test mining loop
   - [ ] Test hauling loop
   - [ ] Test fleet formation
   - [ ] Test crew aggregation

### Phase 4: Burst Validation

1. **Run Burst Validation**
   - [ ] Use `BurstValidation.cs` tool
   - [ ] Fix any Burst compilation errors
   - [ ] Enable Burst safety checks
   - [ ] Verify performance

### Phase 5: Documentation

1. **Create legacy Scene Guides**
   - [ ] Document Godgame legacy scene setup
   - [ ] Document Space4X legacy scene setup
   - [ ] Create troubleshooting guides

---

## Success Criteria

### Godgame legacy Scene

- [ ] Villagers gather resources from nodes
- [ ] Villagers deposit resources to storehouses
- [ ] Villagers have needs that affect behavior
- [ ] Villagers are assigned to jobs based on needs
- [ ] Villagers form bands (basic implementation)
- [ ] Scene runs at 60+ FPS with 100+ villagers
- [ ] All systems are Burst-compiled
- [ ] No GC allocations in gameplay systems

### Space4X legacy Scene

- [ ] Vessels mine resources from deposits
- [ ] Resources spawn as piles
- [ ] Haulers pick up piles
- [ ] Haulers deliver to stations
- [ ] Vessels form fleets (basic implementation)
- [ ] Crews aggregate into departments (basic)
- [ ] Scene runs at 60+ FPS with 100+ vessels
- [ ] All systems are Burst-compiled
- [ ] No GC allocations in gameplay systems

---

## Next Steps

1. **Immediate:** Verify existing systems and scene setups
2. **Short-term:** Complete scene setup and test loops
3. **Medium-term:** Add missing systems if needed
4. **Long-term:** Optimize and profile performance

---

## Related Documentation

- DOTS 1.4 Compliance Audit: `Docs/Audit/DOTS_1.4_Audit_Summary.md`
- Burst Reactivation: `Docs/BurstAudit/BURST_REACTIVATION_SUMMARY.md`
- System Ordering: `Docs/SystemOrdering/SystemSchedule.md`
- Game Integration: `Docs/Guides/GameProject_Integration.md`

---

**Status:** Ready for Implementation








