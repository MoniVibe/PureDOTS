# DOTS 1.4 Compliance Audit - PureDOTS Systems & Bakers

**Status:** Audit Report  
**Category:** Performance & Architecture  
**Scope:** PureDOTS Foundation + Godgame/Space4X Integration  
**Created:** 2025-01-XX  
**Last Updated:** 2025-01-XX

---

## Executive Summary

This audit evaluates PureDOTS systems and bakers against DOTS 1.4 best practices to ensure scalability for 100k+ entities with baking. Key findings include structural change patterns, allocation issues, Burst compatibility, and system group placement.

**Target:** Support 100k+ entities with deterministic rewind, efficient baking, and zero-GC gameplay loops.

---

## DOTS 1.4 Layering Context

### Frame Structure
```
InitializationSystemGroup
  ↓
SimulationSystemGroup
  ├─ FixedStepSimulationSystemGroup (deterministic tick)
  └─ Variable-rate systems
  ↓
PresentationSystemGroup (read-only, no structural changes)
```

### Key Principles
- **Time:** World state (read via `SystemAPI.Time`), deterministic logic in `FixedStepSimulationSystemGroup`
- **Input:** Sampled in Initialization, consumed in Simulation
- **Structural Changes:** Must use Begin/End `EntityCommandBufferSystem` singletons per group
- **Presentation:** Read-only, no structural changes (defer to next frame)

### PureDOTS Time Engine
- **Existing:** `TimeState`, `RewindState` with Record/Playback/CatchUp modes
- **Integration:** Must respect rewind flows and future spatial/world-level time manipulations
- **Placement:** Time systems run in `TimeSystemGroup` (Initialization, OrderFirst)

---

## 1. System Inventory

### 1.1 Major System Categories

#### Time & Rewind Systems
- **`TimeStepSystem`**: Updates `TimeState.Tick` (in `RecordSimulationSystemGroup`)
- **`TimeSettingsConfigSystem`**: Configures time settings
- **`RewindGuardSystems`**: Enable/disable groups based on rewind mode
- **`RewindCoordinatorSystem`**: Coordinates rewind playback
- **`RewindRoutingSystems`**: Routes systems during rewind

**Group Placement:** ✅ Correct (TimeSystemGroup in Initialization, Rewind guards in Simulation)

#### Core Bootstrap Systems
- **`CoreSingletonBootstrapSystem`**: Creates core singletons (TimeSystemGroup, OrderFirst)
- **`PureDotsWorldBootstrap`**: World initialization

**Group Placement:** ✅ Correct (Initialization, OrderFirst)

#### Input Systems
- **`CopyInputToEcsSystem`**: Copies input from Mono bridge to ECS (SimulationSystemGroup, OrderFirst)
- **`InputRecordingSystem`**: Records input for rewind
- **`InputPlaybackSystem`**: Plays back recorded input

**Group Placement:** ⚠️ **ISSUE:** `CopyInputToEcsSystem` uses `Object.FindFirstObjectByType` (not Burst-compatible)

#### Villager Systems
- **`VillagerAISystem`**: AI decision making (VillagerSystemGroup)
- **`VillagerMovementSystem`**: Movement updates (VillagerSystemGroup)
- **`VillagerRegistrySystem`**: Registry maintenance (VillagerSystemGroup, OrderFirst)
- **`VillagerNeedsSystem`**: Needs decay (VillagerSystemGroup)
- **`VillagerInitiativeSystem`**: Initiative computation (VillagerSystemGroup)
- **`VillagerGrudgeDecaySystem`**: Grudge decay (VillagerSystemGroup)

**Group Placement:** ✅ Correct (GameplaySystemGroup → VillagerSystemGroup)

#### Registry Systems
- **`VillagerRegistrySystem`**: Maintains villager registry
- **`StorehouseRegistrySystem`**: Maintains storehouse registry
- **`BandRegistrySystem`**: Maintains band registry
- **`MiracleRegistrySystem`**: Maintains miracle registry

**Group Placement:** ✅ Correct (OrderFirst in respective groups)

#### Spatial Systems
- **`SpatialGridBuildSystem`**: Builds spatial grid (SpatialSystemGroup)
- **`SpatialGridSnapshotSystem`**: Snapshots for rewind (HistorySystemGroup)
- **`AISensorUpdateSystem`**: Updates AI sensors (AISystemGroup)

**Group Placement:** ✅ Correct (SpatialSystemGroup, AISystemGroup)

#### Combat Systems
- **`CombatResolutionSystem`**: Resolves combat (CombatSystemGroup)
- **`CombatPersonalitySystem`**: Updates combat AI (CombatSystemGroup)

**Group Placement:** ✅ Correct (PhysicsSystemGroup → CombatSystemGroup)

#### Presentation Systems
- **`PresentationSpawnSystem`**: Spawns presentation entities (PresentationSystemGroup)
- **`PresentationRecycleSystem`**: Recycles presentation entities (PresentationSystemGroup)
- **`DebugDisplaySystem`**: Debug HUD (PresentationSystemGroup)

**Group Placement:** ✅ Correct (PresentationSystemGroup)

### 1.2 Baker Inventory

#### Major Bakers
- **`VillagerArchetypeCatalogBaker`**: Converts ScriptableObject to blob asset
- **`AggregateBehaviorProfileBaker`**: Bakes aggregate behavior profiles
- **`VegetationSpeciesCatalogBaker`**: Bakes vegetation species

**Pattern:** ✅ Uses `BlobBuilder` with `Allocator.Temp`, creates `Allocator.Persistent` blob assets

---

## 2. DOTS 1.4 Best Practices Evaluation

### 2.1 Burst Compatibility

#### ✅ Good Practices
- Most systems use `[BurstCompile]` on `ISystem` and `IJobEntity`
- Job scheduling uses `ScheduleParallel` for parallel execution
- Component lookups updated correctly with `Update(ref state)`

#### ⚠️ Issues Found

**Issue 1: Non-Burst Input System**
- **File:** `Systems/Input/CopyInputToEcsSystem.cs`
- **Problem:** Uses `Object.FindFirstObjectByType<InputSnapshotBridge>()` which is not Burst-compatible
- **Impact:** Prevents Burst compilation, causes GC allocations
- **Severity:** Medium (input system runs every frame)
- **Recommendation:** 
  - Cache bridge reference in managed system wrapper
  - Or use `SystemAPI.GetSingleton` if bridge is converted to ECS singleton

**Issue 2: Potential Non-Burst Code Paths**
- **File:** `Systems/Space/ResourceDropSpawnerSystem.cs` (line 30)
- **Problem:** Creates `EntityCommandBuffer(Allocator.Temp)` directly instead of using singleton ECB
- **Impact:** May prevent Burst if ECB operations aren't Burst-compatible
- **Severity:** Low (ECB operations are Burst-compatible, but pattern is non-standard)

### 2.2 Zero Allocations

#### ✅ Good Practices
- Most systems use `NativeList` with `Allocator.TempJob` for job-local allocations
- Blob assets use `Allocator.Persistent` (correct for runtime)
- Component lookups cached and updated correctly

#### ⚠️ Issues Found

**Issue 3: Allocations in OnUpdate**
- **Files:** 
  - `Systems/Space/HaulingLoopSystem.cs` (lines 35-39): `ToEntityArray`, `ToComponentDataArray`
  - `Systems/Space/HaulingJobManagerSystem.cs` (lines 31-32): `ToEntityArray`, `ToComponentDataArray`
  - `Systems/Space/HaulingJobPrioritySystem.cs` (lines 27-28): `ToEntityArray`, `ToComponentDataArray`
  - `Systems/Space/ResourcePileSystem.cs` (lines 25-27): `ToEntityArray`, `ToComponentDataArray`
- **Problem:** `ToEntityArray`/`ToComponentDataArray` allocate managed arrays
- **Impact:** GC allocations every frame, performance degradation at scale
- **Severity:** High (runs every frame, scales with entity count)
- **Recommendation:**
  - Use `IJobEntity` or `IJobChunk` instead of querying all entities
  - Or use `NativeArray` with `Allocator.TempJob` if batch processing is required

**Issue 4: Direct ECB Creation**
- **Files:**
  - `Systems/Space/ResourceDropSpawnerSystem.cs` (line 30)
  - `Systems/Space/DropOnlyHarvestDepositSystem.cs` (line 30)
  - `Systems/Space/ResourcePileDecaySystem.cs` (line 21)
  - `Systems/Space/ResourcePileSystem.cs` (line 50)
- **Problem:** Creates `EntityCommandBuffer(Allocator.Temp)` and plays back immediately
- **Impact:** Unnecessary allocation, non-standard pattern
- **Severity:** Medium (works but not optimal)
- **Recommendation:**
  - Use singleton ECB systems: `BeginSimulationEntityCommandBufferSystem`, `EndSimulationEntityCommandBufferSystem`
  - Or use `SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()` for parallel jobs

**Issue 5: NativeList Allocations in OnUpdate**
- **File:** `Systems/AI/AISystems.cs` (lines 139-143)
- **Problem:** Creates multiple `NativeList` allocations every frame
- **Impact:** Allocations every frame (though with `Allocator.TempJob`, disposed after job)
- **Severity:** Low (TempJob allocations are acceptable, but could be optimized)
- **Recommendation:**
  - Consider caching lists if size is predictable
  - Or use `NativeArray` with pre-allocated capacity

### 2.3 Structural Change Patterns

#### ✅ Good Practices
- Most systems modify component data directly (no structural changes needed)
- Rewind systems properly guard against structural changes during playback

#### ⚠️ Issues Found

**Issue 6: Non-Standard ECB Usage**
- **Files:** See Issue 4
- **Problem:** Systems create ECB directly instead of using singleton ECB systems
- **Impact:** 
  - Not aligned with DOTS 1.4 pattern
  - May cause issues with rewind/playback if not handled correctly
- **Severity:** Medium
- **Recommendation:**
  - Migrate to singleton ECB systems for deterministic playback
  - Ensures compatibility with rewind system

**Issue 7: Structural Changes in Presentation**
- **Files:** `Systems/Presentation/PresentationSpawnSystem.cs`, `PresentationRecycleSystem.cs`
- **Problem:** Presentation systems may perform structural changes
- **Impact:** Violates DOTS 1.4 rule (no structural changes in Presentation)
- **Severity:** Medium
- **Recommendation:**
  - Defer structural changes to next frame (queue in component, process in Simulation)
  - Or move spawn/recycle to Simulation group

### 2.4 System Group Placement

#### ✅ Correct Placements
- Time systems: `TimeSystemGroup` (Initialization, OrderFirst) ✅
- Input systems: `SimulationSystemGroup` (OrderFirst) ✅
- Villager systems: `VillagerSystemGroup` (GameplaySystemGroup) ✅
- Combat systems: `CombatSystemGroup` (PhysicsSystemGroup) ✅
- Presentation systems: `PresentationSystemGroup` ✅

#### ⚠️ Potential Issues

**Issue 8: TimeStepSystem Group Placement**
- **File:** `Systems/TimeStepSystem.cs`
- **Current:** `UpdateInGroup(typeof(RecordSimulationSystemGroup))`
- **Problem:** `RecordSimulationSystemGroup` may not align with DOTS 1.4 standard groups
- **Severity:** Low (if RecordSimulationSystemGroup is custom wrapper)
- **Recommendation:**
  - Verify `RecordSimulationSystemGroup` maps to `FixedStepSimulationSystemGroup`
  - Or move to `TimeSystemGroup` if time updates should be in Initialization

### 2.5 Rewind Compatibility

#### ✅ Good Practices
- Systems check `RewindState.Mode` before processing
- Rewind guard systems properly enable/disable groups
- History systems capture state for playback

#### ⚠️ Potential Issues

**Issue 9: ECB Playback During Rewind**
- **Files:** Systems using direct ECB creation (Issue 4)
- **Problem:** ECB playback may not be deterministic if not using singleton ECB systems
- **Severity:** Medium
- **Recommendation:**
  - Use singleton ECB systems for deterministic playback
  - Ensure ECB commands are recorded for rewind

### 2.6 Chunk Utilization & SoA

#### ✅ Good Practices
- Components use SoA-friendly types (byte, sbyte, ushort where possible)
- Systems use `IJobEntity` for parallel processing
- Component lookups used efficiently

#### ⚠️ Potential Optimizations

**Issue 10: Query Efficiency**
- **Files:** Multiple systems with complex queries
- **Problem:** Some queries may not be optimally structured for chunk iteration
- **Severity:** Low (may be fine, but worth profiling)
- **Recommendation:**
  - Profile chunk utilization with Unity Profiler
  - Consider `IJobChunk` for systems processing large batches

---

## 3. Baker Evaluation

### 3.1 Baker Patterns

#### ✅ Good Practices
- Bakers use `BlobBuilder` with `Allocator.Temp` (editor-time)
- Blob assets created with `Allocator.Persistent` (runtime)
- Proper disposal of temporary builders

#### ⚠️ Potential Issues

**Issue 11: Baker Performance**
- **Files:** All bakers
- **Problem:** Bakers run during SubScene baking, may be slow for 100k+ entities
- **Severity:** Low (baking is editor-time, but affects iteration speed)
- **Recommendation:**
  - Profile baking performance
  - Consider incremental baking if supported
  - Optimize blob asset creation (batch operations)

---

## 4. Recommendations for Godgame & Space4X

### 4.1 Godgame-Specific Recommendations

#### Rendering at Scale
- **Issue:** Godgame renders 3D villagers (GameObjects/meshes)
- **Recommendation:**
  - Use Entities Graphics (Hybrid Renderer V2) for 100k+ entities
  - Implement LOD system for distant villagers
  - Use instanced rendering for similar entities
  - Consider culling systems in PresentationSystemGroup

#### Presentation Layer
- **Issue:** Presentation systems may need structural changes (spawn/recycle)
- **Recommendation:**
  - Move spawn/recycle to Simulation group
  - Use presentation components to queue changes
  - Process queues in Simulation, render in Presentation

#### Time Manipulation
- **Issue:** Godgame may need spatial time manipulation (localized rewind)
- **Recommendation:**
  - Extend `RewindState` to support spatial regions
  - Create spatial rewind buffers for localized playback
  - Ensure ECB systems respect spatial rewind boundaries

### 4.2 Space4X-Specific Recommendations

#### UI-Only Entities
- **Issue:** Space4X pops are UI-only (no 3D rendering)
- **Recommendation:**
  - Use lightweight ECS components (no rendering components)
  - UI systems read ECS data, update UI directly
  - Presentation systems can be minimal (or skipped for pops)

#### Ship Representation
- **Issue:** Pops represented via ships (aggregate visualization)
- **Recommendation:**
  - Ship entities have presentation components
  - Pop entities are data-only (no presentation)
  - Ship systems aggregate pop data for visualization

#### Large-Scale Aggregates
- **Issue:** Space4X has large aggregates (planets, fleets, sectors)
- **Recommendation:**
  - Optimize aggregate computation systems (Issue 10)
  - Use chunk-based processing for member aggregation
  - Cache aggregate values, recompute only when members change

---

## 5. Priority Fixes

### High Priority (Performance Critical)

1. **Fix ToEntityArray/ToComponentDataArray Allocations** (Issue 3)
   - **Files:** `HaulingLoopSystem.cs`, `HaulingJobManagerSystem.cs`, `HaulingJobPrioritySystem.cs`, `ResourcePileSystem.cs`
   - **Impact:** GC allocations every frame, scales with entity count
   - **Effort:** Medium (refactor to IJobEntity/IJobChunk)

2. **Fix Non-Burst Input System** (Issue 1)
   - **File:** `CopyInputToEcsSystem.cs`
   - **Impact:** Prevents Burst, causes GC allocations
   - **Effort:** Low (cache bridge reference or convert to ECS singleton)

### Medium Priority (Architecture & Best Practices)

3. **Migrate to Singleton ECB Systems** (Issue 4, 6)
   - **Files:** `ResourceDropSpawnerSystem.cs`, `DropOnlyHarvestDepositSystem.cs`, `ResourcePileDecaySystem.cs`, `ResourcePileSystem.cs`
   - **Impact:** Better rewind compatibility, standard DOTS 1.4 pattern
   - **Effort:** Medium (refactor ECB usage)

4. **Fix Presentation Structural Changes** (Issue 7)
   - **Files:** `PresentationSpawnSystem.cs`, `PresentationRecycleSystem.cs`
   - **Impact:** Violates DOTS 1.4 layering rules
   - **Effort:** Medium (defer to Simulation or move systems)

5. **Verify Time System Group Placement** (Issue 8)
   - **File:** `TimeStepSystem.cs`
   - **Impact:** May not align with DOTS 1.4 standard groups
   - **Effort:** Low (verify and adjust if needed)

### Low Priority (Optimization)

6. **Optimize NativeList Allocations** (Issue 5)
   - **File:** `AISystems.cs`
   - **Impact:** Minor allocations (TempJob is acceptable)
   - **Effort:** Low (cache if predictable size)

7. **Profile Query Efficiency** (Issue 10)
   - **Files:** Multiple systems
   - **Impact:** May improve chunk utilization
   - **Effort:** Medium (profiling + optimization)

---

## 6. Implementation Checklist

### Phase 1: Critical Fixes (High Priority)
- [ ] Fix `ToEntityArray`/`ToComponentDataArray` allocations (Issue 3)
- [ ] Fix non-Burst input system (Issue 1)

### Phase 2: Architecture Alignment (Medium Priority)
- [ ] Migrate to singleton ECB systems (Issue 4, 6)
- [ ] Fix presentation structural changes (Issue 7)
- [ ] Verify time system group placement (Issue 8)

### Phase 3: Optimization (Low Priority)
- [ ] Optimize NativeList allocations (Issue 5)
- [ ] Profile and optimize queries (Issue 10)

### Phase 4: Game-Specific Integration
- [ ] Implement Godgame rendering optimizations (LOD, instancing)
- [ ] Implement Space4X UI-only entity patterns
- [ ] Extend rewind system for spatial time manipulation

---

## 7. Testing Strategy

### Performance Testing
- [ ] Profile with 100k+ entities
- [ ] Measure GC allocations per frame
- [ ] Verify Burst compilation coverage
- [ ] Test rewind/playback performance

### Correctness Testing
- [ ] Verify deterministic behavior with rewind
- [ ] Test ECB playback correctness
- [ ] Verify system group ordering
- [ ] Test presentation read-only compliance

---

## 8. Related Documentation

- DOTS 1.4 Documentation: Unity Entity Component System
- PureDOTS Time Engine: `Runtime/TimeComponents.cs`
- System Groups: `Systems/SystemGroups.cs`
- Rewind System: `Systems/RewindGuardSystems.cs`

---

**Last Updated:** 2025-01-XX  
**Status:** Audit Complete - Ready for Implementation

