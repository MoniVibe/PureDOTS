# legacy-Relevant TODO Triage

**Date:** 2025-01-27  
**Purpose:** Identify and triage TODOs that directly affect legacy readiness, stability, or performance.

---

## legacy-Relevant TODOs (High Priority)

### ✅ Completed (legacy-Ready)

1. **Presentation Bridge Testing** (`PresentationBridge_TODO.md`)
   - ✅ Validation tests added (`Presentation_Bridge_Contract_Tests.cs`)
   - ✅ ECB order, optionality, and failure path tests implemented
   - **Status:** legacy-ready

2. **Integration Tests** (`PureDOTS_TODO.md` section 6)
   - ✅ Core integration tests added (`PureDots_Integration_Tests.cs`)
   - ✅ FixedStep, Rewind, Registry, Spawner determinism tests
   - ✅ legacy-specific determinism tests added (PureDotsTemplate, MiningDemo, VillagerGatherDepositLoop)
   - **Status:** legacy-ready

3. **CI/Budget Tests** (`PureDOTS_TODO.md` section 6)
   - ✅ Budget validator created (`PureDotsBudgetValidator.cs`)
   - ✅ Budget tests with JSON export (`PureDots_Budget_Tests.cs`)
   - ✅ CI script updated with headless flags
   - **Status:** legacy-ready

---

## legacy-Relevant TODOs (Non-Blocking for Demos)

### 1. Registry Rewrite (`RegistryRewrite_TODO.md`)

**Impact on Demos:** ⚠️ Low  
**Reason:** Core registries (Resource, Storehouse, Villager) already work for legacy scenes. Advanced features (spatial sync, continuity validation) are nice-to-have but not required for basic demos.

**Action:** ✅ **Deferred (Post-legacy)**
- Demos use existing registry systems successfully
- Advanced registry features can be added incrementally
- No legacy blockers identified

---

### 2. Spatial Services Expansion (`SpatialServices_TODO.md`)

**Impact on Demos:** ⚠️ Low  
**Reason:** Basic spatial grid exists and works. Advanced features (kNN, hierarchical grids, GPU offload) are performance optimizations, not legacy requirements.

**Action:** ✅ **Deferred (Post-legacy)**
- Basic spatial queries work for legacy scenes
- Advanced features are optimization targets
- No legacy blockers identified

---

### 3. Climate Systems (`ClimateSystems_TODO.md`)

**Impact on Demos:** ⚠️ Low  
**Reason:** Basic climate systems exist. Advanced features (wind-driven fire, snow, biome determination) are game-specific enhancements.

**Action:** ✅ **Deferred (Post-legacy)**
- Basic moisture/temperature grids work
- Advanced climate features are game-level enhancements
- No legacy blockers identified

---

### 4. Villager Job Behavior Stubs (`PureDOTS_TODO.md` section 3)

**Impact on Demos:** ⚠️ Medium  
**Reason:** Basic villager AI and job systems work. Detailed behavior stubs (GatherJobBehavior, BuildJobBehavior) are enhancements.

**Action:** ✅ **Deferred (Post-legacy)**
- Core villager systems (AI, movement, needs) work
- Job behavior stubs are game-specific enhancements
- Demos can run with existing basic behaviors

---

### 5. System Integration (`SystemIntegration_TODO.md`)

**Impact on Demos:** ⚠️ Low  
**Reason:** Core systems integrate correctly. Advanced integration features (centralized events, IL2CPP safety, meta-system slices) are infrastructure improvements.

**Action:** ✅ **Deferred (Post-legacy)**
- Core system integration works for demos
- Advanced features are infrastructure improvements
- No legacy blockers identified

---

## legacy-Relevant TODOs (Needs Quick Fix)

### 1. Missing Generic Comparer (`PureDOTS_TODO.md` section 10)

**Impact on Demos:** ⚠️ Unknown  
**Reason:** May affect compilation in some scenarios.

**Action:** ⏳ **Needs Verification**
- Check if `StreamingLoaderSystem` is used in demos
- If not used, mark as non-blocking
- If used, verify compilation status

**Status:** ⏳ Pending verification

---

### 2. NetCode Physics Regression (`PureDOTS_TODO.md` section 10)

**Impact on Demos:** ✅ None  
**Reason:** NetCode is final priority, not used in demos.

**Action:** ✅ **Deferred (Post-legacy)**
- NetCode not required for demos
- Single-player demos work without NetCode
- No legacy blockers

---

## legacy-Relevant TODOs (Game-Level, Not PureDOTS Core)

### 1. Space4X Framework Requests (`Space4X_Frameworks_TODO.md`)

**Impact on Demos:** ⚠️ Conditional  
**Reason:** Space4X-specific features (alignment/compliance, modules, mining deposits) are game-level, not PureDOTS core.

**Action:** ✅ **Game-Level (Not PureDOTS Core)**
- Space4X demos may require game-specific features
- PureDOTS core provides generic hooks
- Game teams handle Space4X-specific implementations

---

### 2. Godgame-Specific Features

**Impact on Demos:** ⚠️ Conditional  
**Reason:** Godgame-specific features (villager behaviors, band formation) are game-level, not PureDOTS core.

**Action:** ✅ **Game-Level (Not PureDOTS Core)**
- Godgame demos may require game-specific features
- PureDOTS core provides generic hooks
- Game teams handle Godgame-specific implementations

---

## Summary

### legacy-Ready Status

| Category | Status | Notes |
|----------|--------|-------|
| Core Systems | ✅ Ready | Time, rewind, resources, villagers work |
| Presentation Bridge | ✅ Ready | Optional, safe, tested |
| Integration Tests | ✅ Ready | Determinism validated |
| Budget Tests | ✅ Ready | CI integration complete |
| Burst Compliance | ✅ Ready | All hot paths Burst-compiled |
| Registry Systems | ✅ Ready | Basic registries work for demos |
| Spatial Systems | ✅ Ready | Basic spatial grid works |
| Climate Systems | ✅ Ready | Basic climate grids work |

### Deferred Items (Post-legacy)

- Advanced registry features (spatial sync, continuity validation)
- Advanced spatial features (kNN, hierarchical grids, GPU offload)
- Advanced climate features (wind-driven fire, snow, biome determination)
- Detailed villager job behavior stubs
- Advanced system integration features
- NetCode integration
- IL2CPP build configuration

### Game-Level Items (Not PureDOTS Core)

- Space4X-specific features (alignment/compliance, modules, mining deposits)
- Godgame-specific features (villager behaviors, band formation)

---

## Recommendations

1. ✅ **Demos are ready** - Core PureDOTS systems are stable and tested
2. ✅ **Defer advanced features** - Focus on legacy stability, add enhancements incrementally
3. ✅ **Game teams handle game-specific features** - PureDOTS provides generic hooks
4. ⏳ **Verify compilation issues** - Check if `StreamingLoaderSystem` affects demos

---

## Next Steps

1. ✅ Complete legacy scenes inventory (done)
2. ✅ Verify Burst compliance (done)
3. ✅ Add legacy determinism tests (done)
4. ✅ Verify presentation bridge optionality (done)
5. ✅ Triage legacy-relevant TODOs (done)
6. ⏳ Create legacy operator checklist (next)
7. ⏳ Update documentation (next)

