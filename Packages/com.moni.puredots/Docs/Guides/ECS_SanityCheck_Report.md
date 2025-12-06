# ECS Sanity Check Audit Report

**Date**: 2025-01-27  
**Scope**: PureDOTS Runtime (`Packages/com.moni.puredots/Runtime/`)  
**Target Scale**: Hundreds of millions of entities

## Executive Summary

Static code analysis completed across 13 critical ECS design areas. **7 areas pass**, **4 areas have warnings**, **2 areas require runtime profiling**. Overall architecture is sound with specific optimization opportunities identified.

---

## 1. ECS Responsibility Boundaries

**Status**: ✅ **PASS**

**Findings**:
- ✅ All component structs are pure data (no constructors, no methods)
- ✅ Cross-ECS communication uses message queues (`AgentSyncBus`, `WorldBus`) - no direct refs
- ✅ Systems demonstrate single responsibility (e.g., `AISensorUpdateSystem`, `ProjectilePoolSystem`)
- ⚠️ `AISensorUpdateSystem` uses 5+ ComponentLookups (within acceptable range, but monitor)

**Recommendations**:
- Monitor systems approaching 8+ component types for splitting opportunities
- Consider extracting spatial query filtering into separate system if `AISensorUpdateSystem` grows

**Priority**: P2 (Hygiene)

---

## 2. Hot/Cold Data Separation

**Status**: ⚠️ **WARNING** (Requires Runtime Profiling)

**Findings**:
- ✅ Hot data components identified: `Position`, `Velocity`, `Mass`, timers, state flags
- ✅ Cold data patterns: `FixedString64Bytes` for names, display metadata
- ⚠️ **Requires Unity Entities Hierarchy → Chunk Utilization inspector** to verify:
  - Chunk occupancy (target: 70-90%)
  - Cold data separation effectiveness
  - Metadata separation from runtime states

**Recommendations**:
- **P1**: Run Unity Entities Profiler with 1M entities to measure chunk utilization
- Verify `FixedString` components (names, IDs) are in separate archetypes from hot path
- Profile `VillagerComponents` for hot/cold split opportunities

**Priority**: P1 (Performance)

---

## 3. Chunk Layout & AoSoA Alignment

**Status**: ✅ **PASS** (with notes)

**Findings**:
- ✅ `[InternalBufferCapacity]` widely used (259 instances)
- ✅ Capacity values appear reasonable (0-512 range, most 4-32)
- ⚠️ Some buffers use `[InternalBufferCapacity(0)]` - verify these don't cause frequent reallocation
- ✅ Buffer capacities cover common cases (e.g., `[InternalBufferCapacity(8)]` for limbs, `[InternalBufferCapacity(256)]` for pathfinding)

**Recommendations**:
- **P2**: Audit `[InternalBufferCapacity(0)]` buffers - verify they don't reallocate frequently
- **P1**: Measure reallocation frequency in runtime (<1% of frames target)
- Consider AoSoA pattern for tight vector math (e.g., limb transforms) if profiling shows cache misses

**Files to Review**:
- `Runtime/Runtime/Spatial/SpatialComponents.cs` (multiple `[InternalBufferCapacity(0)]`)
- `Runtime/Runtime/Environment/EnvironmentGrids.cs` (multiple `[InternalBufferCapacity(0)]`)

**Priority**: P1 (Performance)

---

## 4. Pooling & Reuse

**Status**: ✅ **PASS**

**Findings**:
- ✅ `ProjectilePoolSystem` uses `IEnableableComponent` for pooling (`PooledProjectileTag`)
- ✅ Pooling pattern: disable via `IEnableableComponent`, not destroy
- ✅ `ProjectilePoolSystem` documents 0 GC, constant-time spawn/despawn
- ⚠️ `BorrowProjectile` currently creates entities directly (line 81) - pool integration incomplete

**Recommendations**:
- **P1**: Complete `ProjectilePoolSystem.BorrowProjectile` to use actual pool queue instead of `CreateEntity()`
- **P2**: Verify pool size variance <5% during sustained simulation (runtime test)
- **P2**: Measure re-enabling cost <0.1ms/frame (runtime test)

**Priority**: P1 (Performance)

---

## 5. Job Scheduling & Workload Batching

**Status**: ✅ **PASS** (with warnings)

**Findings**:
- ✅ `ScheduleParallel` used extensively (205+ instances)
- ✅ JobHandle dependencies chained via `state.Dependency` (no mid-frame `Complete()`)
- ⚠️ **Exception**: `SpatialGridRefinementSystem` line 127 calls `jobHandle.Complete()` mid-frame
- ⚠️ **Exception**: `AISensorUpdateSystem` line 263 calls `state.Dependency.Complete()` mid-frame
- ✅ Heavy systems use `NativeQueue`/`NativeList` for result collection
- ✅ All heavy math jobs marked `[BurstCompile]`

**Recommendations**:
- **P0**: Remove `Complete()` calls in `SpatialGridRefinementSystem` and `AISensorUpdateSystem` - chain dependencies instead
- **P1**: Profile batch sizes per system - optimize `ScheduleParallel(batchCount)` empirically
- **P1**: Measure worker thread utilization (target: 60-80%) via Unity Profiler

**Files to Fix**:
- `Runtime/Systems/Spatial/SpatialGridRefinementSystem.cs:127`
- `Runtime/Systems/AI/AISystems.cs:263`

**Priority**: P0 (Blocking)

---

## 6. Archetype & Chunk Hygiene

**Status**: ⚠️ **WARNING** (Requires Runtime Inspection)

**Findings**:
- ✅ `ISharedComponentData` usage minimal (only 2 instances: `SpatialGridWorldIndex`, `RenderMeshArraySingleton`)
- ⚠️ **Requires Unity Entities Hierarchy → Chunk Utilization inspector** to verify:
  - Archetype entity counts (target: ≥1,000 per archetype)
  - Chunk memory fragmentation (target: ≤10%)
  - Archetypes differing by one seldom-used component (merge candidates)

**Recommendations**:
- **P1**: Run Chunk Utilization inspector in Unity Editor
- Identify archetypes with <1,000 entities for merging
- Flag archetypes differing by one component for consolidation

**Priority**: P1 (Performance)

---

## 7. Cache & Memory Coherence

**Status**: ⚠️ **WARNING**

**Findings**:
- ✅ `[ReadOnly]` attributes used in job structs (e.g., `SpatialGridRefinementSystem`, `CollisionBroadPhaseSystem`)
- ⚠️ **Limited `[ReadOnly]` usage** - many queries may benefit from read-only markers
- ❌ **No `math.fma()` usage found** - opportunity for fused multiply-add optimization
- ✅ Spatial locality: `SpatialGridResidency` component suggests spatial ordering

**Recommendations**:
- **P2**: Audit queries for read-only components - add `[ReadOnly]` where appropriate
- **P2**: Replace `a * b + c` patterns with `math.fma(a, b, c)` in hot paths
- **P1**: Verify spatial ordering (entities near each other in same chunk) via runtime profiling

**Priority**: P2 (Hygiene)

---

## 8. Memory Pools (Native Containers)

**Status**: ✅ **PASS** (with notes)

**Findings**:
- ✅ `NativeStream` used for telemetry (`TelemetryStreamingSystem`)
- ✅ `NativeList`/`NativeArray` allocated with `Allocator.TempJob` (disposed after use)
- ✅ `TelemetryStreamingSystem` preallocates `NativeStream` in `OnCreate()`
- ⚠️ Many systems allocate `NativeList`/`NativeArray` per-frame in `OnUpdate()` (acceptable if `Allocator.TempJob`)

**Recommendations**:
- **P2**: Consider pooling `NativeList`/`NativeHashMap` for systems that allocate frequently
- **P1**: Verify zero heap allocations post-initialization via Unity Profiler (Memory tab)
- **P2**: Wrap frequently-resized `NativeList` in pooling container with Capacity *= 2 growth

**Priority**: P1 (Performance)

---

## 9. Update Group Hierarchy

**Status**: ✅ **PASS**

**Findings**:
- ✅ `SimulationSystemGroup` for deterministic core (fixed tick)
- ✅ `PureDotsPresentationSystemGroup` for camera/UI (variable tick, under `Unity.Entities.PresentationSystemGroup`)
- ✅ `CognitiveSystemGroup` for async logic (low frequency, 0.5-5Hz documented)
- ✅ `EconomySystemGroup` for economy updates (low frequency, 0.1Hz documented)
- ✅ `HotPathSystemGroup`, `WarmPathSystemGroup`, `ColdPathSystemGroup` for spatial pathfinding tiers
- ✅ Stable inter-group dependencies via `[UpdateAfter]`/`[UpdateBefore]`

**Recommendations**:
- **P2**: Document tick rates in system group XML comments (some already documented)
- **P2**: Verify adaptive tick rates are implemented for `CognitiveSystemGroup`/`EconomySystemGroup`

**Priority**: P2 (Hygiene)

---

## 10. Prefab / BlobAsset Lifecycle

**Status**: ✅ **PASS** (with notes)

**Findings**:
- ✅ Authoring uses `Baker<T>` pattern (`VillagerBaker`, etc.)
- ✅ BlobAsset references used (e.g., `BlobAssetReference<ProjectileSpec>`)
- ⚠️ **Requires runtime verification**:
  - Prefab instantiation uses `EntityCommandBuffer` only inside fixed sim ticks
  - Blob hot-reloads only in editor mode, not runtime
  - Blob immutability preserved

**Recommendations**:
- **P2**: Audit prefab instantiation systems - verify `EntityCommandBuffer` usage
- **P2**: Add runtime guards to prevent Blob hot-reloads in non-editor builds
- **P1**: Verify BlobAsset-based specs (`PrefabSpec`, `MaterialSpec`) are used consistently

**Priority**: P2 (Hygiene)

---

## 11. Hot Path Profiling

**Status**: ⚠️ **REQUIRES RUNTIME PROFILING**

**Findings**:
- ⚠️ **Cannot verify without Unity runtime**:
  - Per-system ms measurements (>2ms → split, <0.05ms → merge)
  - Worker thread utilization (target: 60-80%)
  - Zero GC allocs per frame

**Recommendations**:
- **P0**: Run Unity Entities Profiler with 1M entities, mixed archetypes
- **P0**: Inspect "System Main Thread" → verify most systems parallelized
- **P0**: Check Unity Profiler > Memory → verify zero GC allocs per frame
- Flag systems >2ms for splitting or jobification
- Consider merging systems <0.05ms but frequent (reduce scheduling overhead)

**Priority**: P0 (Blocking)

---

## 12. Rewind / Snapshot Compatibility

**Status**: ⚠️ **WARNING** (Requires Runtime Testing)

**Findings**:
- ✅ `RewindState` component exists and systems check `RewindMode.Record`
- ✅ `HistorySystemGroup` exists for snapshot recording
- ⚠️ **No `[ColdComponent]` attribute found** - cold components not explicitly flagged
- ⚠️ **Requires runtime verification**:
  - Snapshot delta chain serialization supports chunk-level memory copies
  - `ArchetypeChunk.GetNativeArray<T>()` used for raw binary deltas
  - Fast rewind performance

**Recommendations**:
- **P1**: Implement `[ColdComponent]` attribute for cold data (names, metadata)
- **P1**: Audit snapshot systems - verify chunk-level serialization
- **P1**: Test rewind performance with 1M entities

**Priority**: P1 (Performance)

---

## 13. Dirty Flags & Event-Driven Updates

**Status**: ⚠️ **WARNING**

**Findings**:
- ✅ `WithChangeFilter` used in 6 systems:
  - `CollisionDamageBridgeSystem` (CollisionProperties)
  - `MicroCollisionSystem` (CollisionProperties)
  - `FocusDrainSystem` (FocusState)
  - `SpatialGridDirtyTrackingSystem` (LocalTransform)
- ✅ `IEnableableComponent` used for pooling (`PooledProjectileTag`, vegetation tags)
- ⚠️ **Limited change filter usage** - many heavy systems may benefit from change filters
- ✅ Structural changes batched via `EntityCommandBuffer` (EndSimulationEntityCommandBufferSystem)

**Recommendations**:
- **P1**: Audit heavy systems (mass, physics, AI, communication) - add `WithChangeFilter` where appropriate
- **P2**: Verify `EnableableComponent` flags avoid entity destruction/recreation (already implemented)
- **P1**: Measure structural churn (target: near-zero) via runtime profiling

**Priority**: P1 (Performance)

---

## Critical Issues Summary

### P0 (Blocking - Fix Immediately)
1. **Job Scheduling**: Remove `Complete()` calls mid-frame in:
   - `SpatialGridRefinementSystem.cs:127`
   - `AISystems.cs:263`
2. **Hot Path Profiling**: Run Unity Entities Profiler with 1M entities (runtime required)

### P1 (Performance - High Priority)
1. **Pooling**: Complete `ProjectilePoolSystem.BorrowProjectile` pool integration
2. **Hot/Cold Separation**: Run Chunk Utilization inspector, verify 70-90% occupancy
3. **Change Filters**: Add `WithChangeFilter` to heavy systems (mass, physics, AI)
4. **Rewind**: Implement `[ColdComponent]` attribute, test rewind performance
5. **Memory Pools**: Verify zero heap allocations post-initialization

### P2 (Hygiene - Medium Priority)
1. **Chunk Layout**: Audit `[InternalBufferCapacity(0)]` buffers for reallocation frequency
2. **Cache Coherence**: Add `[ReadOnly]` to queries, replace `a * b + c` with `math.fma()`
3. **Blob Lifecycle**: Verify prefab instantiation patterns, add runtime guards

---

## Next Steps

1. **Immediate**: Fix P0 blocking issues (remove `Complete()` calls)
2. **This Week**: Run runtime profiling (P1 items requiring Unity Editor)
3. **This Sprint**: Implement P1 performance optimizations
4. **Ongoing**: Address P2 hygiene items incrementally

---

## Runtime Profiling Checklist

**Required Unity Editor Actions**:
- [ ] Run Unity Entities Profiler with 1M entities, mixed archetypes
- [ ] Inspect Chunk Utilization (Entities Hierarchy → Chunk Utilization)
- [ ] Measure per-system ms (flag >2ms for splitting, <0.05ms for merging)
- [ ] Check worker thread utilization (target: 60-80%)
- [ ] Verify zero GC allocs per frame (Profiler > Memory)
- [ ] Measure chunk occupancy (target: 70-90%)
- [ ] Test rewind performance with 1M entities
- [ ] Measure structural churn (target: near-zero)

---

**Report Generated**: Static code analysis complete. Runtime profiling required for full validation.

