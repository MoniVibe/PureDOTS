# PureDOTS Demo Scenes Index

**Last Updated:** 2025-01-27  
**Purpose:** Catalog of all demo scenes in PureDOTS, their expected behaviors, and demo readiness status.

---

## Core Demo Scenes

### 1. PureDotsTemplate.unity
**Location:** `Assets/Scenes/PureDotsTemplate.unity`  
**Status:** ✅ Primary template scene  
**Expected Behaviors:**
- PureDOTS config bootstrap loads correctly
- Time controls work (pause, speed multiplier)
- Resource nodes spawn and are visible
- Storehouse entities exist and accept deposits
- Villager prefabs spawn and execute basic gather/deposit loops
- Presentation bridge (if present) creates visual companions
- No console errors on startup or during play

**Key Components:**
- `PureDotsConfigAuthoring` with runtime config asset
- Sample resource nodes, storehouses, villagers
- Time control authoring objects
- Optional presentation bridge GameObject

**Demo Readiness:** ✅ Ready (baseline template)

---

### 2. SpawnerDemoScene.unity
**Location:** `Assets/Scenes/SpawnerDemoScene.unity/SpawnerDemoScene.unity`  
**Status:** ✅ Spawner validation scene  
**Expected Behaviors:**
- Scene spawner system executes deterministically
- Entities spawn at configured positions/seeds
- Spawn counts match across frame rates (30/60/120fps)
- Spawned entities have correct components and transforms
- No exceptions during spawn operations

**Key Components:**
- `SceneSpawnController` with seed configuration
- `SceneSpawnRequest` buffers
- Spawner prefabs and placement modes

**Demo Readiness:** ✅ Ready (determinism validated)

---

### 3. Space4XMineLoop.unity
**Location:** `Assets/Scenes/Space4XMineLoop.unity`  
**Status:** ⚠️ Space4X-specific demo (may require game project dependencies)  
**Expected Behaviors:**
- Mining loop system executes (vessels → asteroids → carriers)
- Resource drops spawn and move correctly
- Hauling jobs assign and complete
- Presentation visuals sync (if bridge present)
- Deterministic across frame rates

**Key Components:**
- Mining vessel entities
- Asteroid resource nodes
- Carrier entities with storage
- Hauling job system

**Demo Readiness:** ⚠️ Conditional (requires Space4X authoring components)

---

### 4. MiningDemo_Dual_Authoring.unity
**Location:** `Assets/MiningDemo_Dual_Authoring.unity`  
**Status:** ⚠️ Dual-authoring demo  
**Expected Behaviors:**
- Both authoring paths (MonoBehaviour + ECS) work
- Entities spawn correctly from both sources
- No conflicts or duplicate entities
- Presentation bridge handles both authoring types

**Demo Readiness:** ⚠️ Needs validation

---

### 5. Space4XRegistryDemo.unity
**Location:** `Assets/Projects/Space4X/Scenes/Demo/Space4XRegistryDemo.unity`  
**Status:** ⚠️ Space4X-specific registry demo  
**Expected Behaviors:**
- Registry bakes correctly from authoring
- Runtime registry buffers mirror baked data
- Registry continuity maintained across frames
- Stable ordering and meta parity

**Demo Readiness:** ⚠️ Conditional (Space4X project)

---

## Validation/QA Scenes

### 6. RewindSandbox.unity
**Location:** `Assets/Scenes/Validation/RewindSandbox.unity`  
**Status:** ✅ Rewind validation scene  
**Expected Behaviors:**
- Rewind system records simulation state
- Rewind playback matches recorded state
- Resimulation after rewind produces identical results
- No state corruption during rewind operations

**Demo Readiness:** ✅ Ready (validation scene)

---

### 7. PerformanceSoakScene.unity
**Location:** `Assets/Scenes/Perf/PerformanceSoakScene.unity`  
**Status:** ✅ Performance testing scene  
**Expected Behaviors:**
- High entity counts (500+ villagers, resources)
- Systems maintain frame rate budgets
- Memory usage stays within limits
- No GC spikes or allocations in hot paths

**Demo Readiness:** ✅ Ready (performance validation)

---

## SubScenes

### 8. Godgame_Entities.unity
**Location:** `Assets/Scenes/SubScenes/Godgame_Entities.unity`  
**Status:** ⚠️ Godgame-specific subscene  
**Expected Behaviors:**
- Villager entities with needs/mood systems
- Band formation and aggregation
- Village workforce systems
- Job assignment and execution

**Demo Readiness:** ⚠️ Conditional (Godgame project)

---

### 9. Space4X_Entities.unity
**Location:** `Assets/Scenes/SubScenes/Space4X_Entities.unity`  
**Status:** ⚠️ Space4X-specific subscene  
**Expected Behaviors:**
- Carrier and vessel entities
- Module systems (degradation, repair, refit)
- Mining and hauling loops
- Trade and economy systems

**Demo Readiness:** ⚠️ Conditional (Space4X project)

---

## Demo Readiness Summary

| Scene | Status | Burst Compliance | Determinism | Presentation | Notes |
|-------|--------|------------------|-------------|--------------|-------|
| PureDotsTemplate | ✅ Ready | ✅ Yes | ✅ Validated | ✅ Optional | Baseline template |
| SpawnerDemoScene | ✅ Ready | ✅ Yes | ✅ Validated | ✅ Optional | Determinism tested |
| Space4XMineLoop | ⚠️ Conditional | ⚠️ Unknown | ⚠️ Unknown | ⚠️ Unknown | Requires Space4X |
| MiningDemo_Dual | ⚠️ Needs Validation | ⚠️ Unknown | ⚠️ Unknown | ⚠️ Unknown | Dual authoring |
| Space4XRegistryDemo | ⚠️ Conditional | ⚠️ Unknown | ⚠️ Unknown | ⚠️ Unknown | Space4X project |
| RewindSandbox | ✅ Ready | ✅ Yes | ✅ Validated | ✅ Optional | Validation scene |
| PerformanceSoakScene | ✅ Ready | ✅ Yes | ⚠️ Needs Test | ✅ Optional | Performance test |

---

## Demo Entry Points for Game Teams

**For PureDOTS Core Validation:**
1. Start with `PureDotsTemplate.unity` - baseline functionality
2. Run `SpawnerDemoScene.unity` - deterministic spawning
3. Run `RewindSandbox.unity` - rewind/resimulation

**For Game-Specific Demos:**
- Godgame teams: Use `Godgame_Entities.unity` subscene
- Space4X teams: Use `Space4X_Entities.unity` subscene or `Space4XMineLoop.unity`

---

## Known Limitations

1. **Space4X/Godgame Scenes:** Require game project dependencies; not standalone PureDOTS demos
2. **Presentation Bridge:** All demos work without presentation bridge (optional)
3. **Hybrid Scenes:** Some scenes may reference hybrid authoring patterns (MonoBehaviour + ECS)

---

## Next Steps

1. ✅ Document all demo scenes (this document)
2. ⏳ Validate Burst compliance for all demo-relevant systems
3. ⏳ Add determinism tests for key demo flows
4. ⏳ Create operator checklist for running demos
5. ⏳ Update game team guides with demo usage instructions

