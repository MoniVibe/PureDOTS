# legacy Readiness Checklist

**Last Updated:** 2025-01-27  
**Purpose:** Operator checklist for running and validating PureDOTS legacy scenes.

---

## Pre-Flight Checklist

### Environment Setup

- [ ] Unity Editor with DOTS Entities 1.4+ installed
- [ ] PureDOTS package properly imported/configured
- [ ] No compilation errors in Console
- [ ] Burst compilation enabled (Project Settings → Burst → Enable Compilation)

---

## legacy Scene Execution

### 1. PureDotsTemplate.unity

**Location:** `Assets/Scenes/PureDotsTemplate.unity`

**Steps:**
1. Open scene in Unity Editor
2. Enter Play Mode (press Play button)
3. Observe Console for errors (should be clean)
4. Verify expected behaviors:
   - [ ] Time controls work (pause/speed multiplier if available)
   - [ ] Resource nodes visible and functional
   - [ ] Storehouse entities exist and accept deposits
   - [ ] Villager entities spawn and execute gather/deposit loops
   - [ ] No red errors in Console
   - [ ] Frame rate stable (60fps target, <16.6ms per frame)

**Expected Console Output:**
- No errors
- Optional: Info logs for system initialization
- Optional: Warning logs for missing presentation bridge (acceptable)

**Performance Budgets:**
- FixedTick: < 16.6ms
- Memory: < 500MB (baseline scene)
- Presentation spawns: < 100 per frame

**Status:** ✅ Ready

---

### 2. SpawnerDemoScene.unity

**Location:** `Assets/Scenes/SpawnerDemoScene.unity/SpawnerDemoScene.unity`

**Steps:**
1. Open scene in Unity Editor
2. Enter Play Mode
3. Observe Console for errors (should be clean)
4. Verify expected behaviors:
   - [ ] Entities spawn deterministically
   - [ ] Spawn counts match across frame rates (if tested)
   - [ ] No exceptions during spawn operations
   - [ ] Spawned entities have correct components

**Expected Console Output:**
- No errors
- Optional: Info logs for spawner system

**Performance Budgets:**
- FixedTick: < 16.6ms
- Memory: < 500MB

**Status:** ✅ Ready

---

### 3. RewindSandbox.unity

**Location:** `Assets/Scenes/Validation/RewindSandbox.unity`

**Steps:**
1. Open scene in Unity Editor
2. Enter Play Mode
3. Observe Console for errors (should be clean)
4. Verify expected behaviors:
   - [ ] Rewind system records simulation state
   - [ ] Rewind playback matches recorded state (if tested)
   - [ ] No state corruption during rewind operations
   - [ ] Resimulation produces identical results

**Expected Console Output:**
- No errors
- Optional: Info logs for rewind state transitions

**Performance Budgets:**
- FixedTick: < 16.6ms
- Snapshot ring: ≤ 1000 entries

**Status:** ✅ Ready

---

### 4. PerformanceSoakScene.unity

**Location:** `Assets/Scenes/Perf/PerformanceSoakScene.unity`

**Steps:**
1. Open scene in Unity Editor
2. Enter Play Mode
3. Observe Console for errors (should be clean)
4. Verify expected behaviors:
   - [ ] High entity counts (500+ villagers/resources)
   - [ ] Systems maintain frame rate budgets
   - [ ] Memory usage stays within limits
   - [ ] No GC spikes or allocations in hot paths

**Expected Console Output:**
- No errors
- Optional: Performance telemetry logs

**Performance Budgets:**
- FixedTick: < 16.6ms (may be higher with 500+ entities)
- Memory: < 1GB (high entity count)
- GC allocations: Minimal in hot paths

**Status:** ✅ Ready

---

## Conditional legacy Scenes

### 5. Space4XMineLoop.unity

**Location:** `Assets/Scenes/Space4XMineLoop.unity`

**Requirements:** Space4X project dependencies

**Steps:**
1. Verify Space4X project is available
2. Open scene in Unity Editor
3. Enter Play Mode
4. Verify expected behaviors:
   - [ ] Mining loop system executes
   - [ ] Resource drops spawn and move correctly
   - [ ] Hauling jobs assign and complete
   - [ ] Presentation visuals sync (if bridge present)

**Status:** ⚠️ Conditional (requires Space4X)

---

### 6. MiningDemo_Dual_Authoring.unity

**Location:** `Assets/MiningDemo_Dual_Authoring.unity`

**Steps:**
1. Open scene in Unity Editor
2. Enter Play Mode
3. Verify expected behaviors:
   - [ ] Both authoring paths (MonoBehaviour + ECS) work
   - [ ] Entities spawn correctly from both sources
   - [ ] No conflicts or duplicate entities

**Status:** ⚠️ Needs Validation

---

## Console Health Check

### Acceptable Console Output

**Errors:** None (red messages)
**Warnings:** 
- Missing presentation bridge (acceptable if bridge not in scene)
- Missing bindings (acceptable if bindings not configured)
- Budget warnings (investigate if performance issues)

**Info:** 
- System initialization logs (acceptable)
- Telemetry logs (acceptable)

---

## Performance Validation

### Frame Time Budgets

| Scene | Target FPS | Max FixedTick (ms) | Notes |
|-------|------------|-------------------|-------|
| PureDotsTemplate | 60 | 16.6 | Baseline scene |
| SpawnerDemoScene | 60 | 16.6 | Deterministic spawning |
| RewindSandbox | 60 | 16.6 | Rewind validation |
| PerformanceSoakScene | 60 | 20.0 | High entity count (relaxed) |

### Memory Budgets

| Scene | Target Memory | Notes |
|-------|--------------|-------|
| PureDotsTemplate | < 500MB | Baseline scene |
| SpawnerDemoScene | < 500MB | Deterministic spawning |
| RewindSandbox | < 500MB | Rewind validation |
| PerformanceSoakScene | < 1GB | High entity count |

---

## Burst Compilation Check

### Verification Steps

1. **Enable Burst Compilation:**
   - Project Settings → Burst → Enable Compilation
   - Project Settings → Burst → Compile Synchronously (development)

2. **Check Burst Status:**
   - Window → Analysis → Burst Compile
   - Verify no compilation errors
   - Verify hot-path systems are Burst-compiled

3. **Expected Burst Coverage:**
   - ✅ Time systems (TimeTickSystem, TimeStepSystem)
   - ✅ Resource systems (ResourceGatheringSystem, ResourceDepositSystem)
   - ✅ Villager systems (VillagerAISystem, VillagerMovementSystem)
   - ✅ Spatial systems (SpatialGridBuildSystem)
   - ✅ Mining/Hauling systems (MiningLoopSystem, HaulingLoopSystem)
   - ✅ Spawner systems (SceneSpawnSystem)

---

## Determinism Validation

### Test Scenarios

1. **FixedStep Gating:**
   - Run scene at 30fps, capture snapshot
   - Run scene at 60fps, capture snapshot
   - Run scene at 120fps, capture snapshot
   - Assert snapshots are bytewise identical

2. **Rewind Determinism:**
   - Record 5 seconds of simulation
   - Rewind 2 seconds
   - Resimulate to 5 seconds
   - Assert bytewise match at T+5s

3. **Spawner Determinism:**
   - Run spawner with seed=123 at 60fps, count spawned
   - Run spawner with seed=123 at 120fps, count spawned
   - Assert identical spawn counts

**Automated Tests:** Run `PureDots_Integration_Tests.cs` in PlayMode

---

## Presentation Bridge Validation

### Optionality Check

1. **With Bridge:**
   - [ ] Presentation bridge GameObject exists in scene
   - [ ] Companions spawn correctly
   - [ ] Effects play correctly
   - [ ] No exceptions

2. **Without Bridge:**
   - [ ] Remove presentation bridge GameObject
   - [ ] Run simulation
   - [ ] No exceptions thrown
   - [ ] Requests cleared gracefully
   - [ ] Simulation continues normally
   - [ ] Failure counters increment correctly

**Automated Tests:** Run `Presentation_Bridge_Contract_Tests.cs` in PlayMode

---

## Troubleshooting

### Common Issues

1. **Compilation Errors:**
   - Check Unity version (DOTS Entities 1.4+ required)
   - Verify package dependencies
   - Check Console for specific error messages

2. **Performance Issues:**
   - Check Burst compilation status
   - Verify frame time budgets
   - Check memory usage
   - Review performance telemetry

3. **Determinism Issues:**
   - Verify fixed timestep is enabled
   - Check seed initialization
   - Review rewind state configuration

4. **Presentation Issues:**
   - Verify presentation bridge exists (if needed)
   - Check binding configuration
   - Review request hub initialization

---

## CI Validation

### Automated Checks

Run CI script: `CI/run_playmode_tests.sh`

**Expected Results:**
- ✅ All EditMode tests pass
- ✅ All PlayMode tests pass
- ✅ Budget tests pass
- ✅ JSON artifacts generated

**Artifacts:**
- `CI/TestResults/Artifacts/budget_results.json`
- `CI/TestResults/editmode-results.xml`
- `CI/TestResults/playmode-results.xml`

---

## legacy Readiness Summary

| Component | Status | Notes |
|-----------|--------|-------|
| Core Systems | ✅ Ready | Time, rewind, resources, villagers |
| Presentation Bridge | ✅ Ready | Optional, safe, tested |
| Integration Tests | ✅ Ready | Determinism validated |
| Budget Tests | ✅ Ready | CI integration complete |
| Burst Compliance | ✅ Ready | All hot paths Burst-compiled |
| legacy Scenes | ✅ Ready | PureDotsTemplate, SpawnerDemoScene, RewindSandbox |
| Performance Soak | ✅ Ready | PerformanceSoakScene validated |

**Overall Status:** ✅ **legacy READY**

---

## Next Steps

1. ✅ Run legacy scenes and verify behaviors
2. ✅ Check console for errors/warnings
3. ✅ Validate performance budgets
4. ✅ Run automated tests (integration, budget, presentation bridge)
5. ✅ Review CI artifacts for regressions

