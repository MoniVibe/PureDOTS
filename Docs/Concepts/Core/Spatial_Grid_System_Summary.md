# Spatial Grid System Summary

**Status:** Implementation Analysis
**Audience:** Technical Advisor / Architecture Review
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Executive Summary

The PureDOTS spatial grid system provides a **registry-aligned, deterministic spatial partitioning service** for efficient entity queries (radius, k-NN, AABB) at scale (target: 100k → 1M+ entities). It uses a **hash-based grid with Morton/Z-curve indexing**, double-buffering for safe reads during writes, and integrates with the Registry system for domain-specific entity tracking.

**Current State:** Core implementation complete; partial rebuild support exists but gaps remain in integration coverage, rewind determinism validation, and some domain-specific optimizations.

**Key Strengths:**
- Burst-compatible query API (`SpatialQueryHelper`)
- Deterministic sorting and hash functions
- Provider abstraction (allows future hierarchical/BVH grids)
- Registry integration for efficient domain queries
- Instrumentation and debug tooling

**Key Gaps:**
- Not all systems use spatial queries (some still scan EntityManager)
- Rewind mode validation incomplete
- No multi-resolution grid support yet
- Limited partial rebuild optimization
- Missing GPU offload hooks for extreme densities

---

## File Mapping

### Core Implementation Files

| File Path | Purpose | Status |
|-----------|---------|--------|
| `Runtime/Runtime/Spatial/SpatialComponents.cs` | Component definitions (`SpatialGridConfig`, `SpatialGridState`, `SpatialGridEntry`, etc.) | ✅ Complete |
| `Runtime/Runtime/Spatial/SpatialUtilities.cs` | Query helpers (`SpatialQueryHelper`), hash functions (`SpatialHash`), filters | ✅ Complete |
| `Runtime/Runtime/Spatial/SpatialProviders.cs` | Provider interface (`ISpatialGridProvider`), implementations (Hashed, Uniform) | ✅ Complete |
| `Runtime/Runtime/Spatial/SpatialProviderRegistry.cs` | Provider factory registry | ✅ Complete |
| `Runtime/Runtime/Spatial/SpatialGridSnapshot.cs` | Rewind snapshot structures | ✅ Complete |
| `Runtime/Runtime/Spatial/SpatialRebuildThresholds.cs` | Partial rebuild thresholds | ✅ Complete |
| `Runtime/Runtime/Spatial/SpatialLayerConfig.cs` | Layer configuration (2D/3D) | ✅ Complete |
| `Runtime/Runtime/Spatial/SpatialModifierComponents.cs` | Spatial modifiers (zones, visibility, etc.) | ✅ Complete |
| `Runtime/Runtime/Spatial/SpatialModifierHelpers.cs` | Modifier query utilities | ✅ Complete |
| `Runtime/Runtime/Spatial/SpatialNavigation.cs` | Navigation integration helpers | ✅ Complete |

### System Files

| File Path | Purpose | Status |
|-----------|---------|--------|
| `Systems/Spatial/SpatialGridInitialBuildSystem.cs` | Initial grid bootstrap | ✅ Complete |
| `Systems/Spatial/SpatialGridBuildSystem.cs` | Main rebuild system (full + partial) | ✅ Complete |
| `Systems/Spatial/SpatialGridDirtyTrackingSystem.cs` | Dirty entity tracking for partial rebuilds | ✅ Complete |
| `Systems/Spatial/SpatialResidencyVersionSystem.cs` | Residency component versioning | ✅ Complete |
| `Systems/Spatial/SpatialResidencyBarrierSystem.cs` | Barrier for residency updates | ✅ Complete |
| `Systems/Spatial/SpatialProviderRegistrySystem.cs` | Provider registry management | ✅ Complete |
| `Systems/Spatial/SpatialInstrumentationSystem.cs` | Console logging and telemetry | ✅ Complete |
| `Systems/Spatial/SpatialGridSnapshotSystem.cs` | Rewind snapshot capture | ✅ Complete |
| `Systems/Spatial/SpatialInteractionExampleSystem.cs` | Example integration pattern | ✅ Complete |

### Authoring Files

| File Path | Purpose | Status |
|-----------|---------|--------|
| `Authoring/SpatialPartitionAuthoring.cs` | Scene authoring component + baker | ✅ Complete |
| `Authoring/SpatialPartitionProfile.cs` | ScriptableObject profile asset | ✅ Complete |

### Integration Points (Consumer Systems)

| File Path | Usage | Status |
|-----------|-------|--------|
| `Systems/VillagerJobSystems.cs` | Resource/storehouse candidate selection | ✅ Integrated |
| `Systems/AI/AISystems.cs` | k-NN sensor queries (`SpatialKNearestBatchJob`) | ✅ Integrated |
| `Systems/Navigation/SpatialSensorUpdateSystem.cs` | Agent sensor updates | ✅ Integrated |
| `Systems/RegistrySpatialSyncSystem.cs` | Registry ↔ spatial grid synchronization | ✅ Integrated |
| `Projects/Godgame/Systems/DivineHandSystems.cs` | Hover highlighting, target selection | ✅ Integrated |
| `Projects/Godgame/Registry/GodgameRegistryBridgeSystem.cs` | Registry entry spatial metadata | ✅ Integrated |
| `Projects/Space4X/Registry/Space4xRegistryBridgeSystem.cs` | Registry entry spatial metadata | ✅ Integrated |

### Documentation Files

| File Path | Purpose |
|-----------|---------|
| `Documentation/DesignNotes/SpatialPartitioning.md` | Design specification and roadmap |
| `Documentation/DesignNotes/SpatialServicesConcepts.md` | Future concepts (query broker, region tagging) |
| `Docs/TODO/SpatialServices_TODO.md` | Active TODO list and milestones |
| `Docs/Guides/SpatialQueryUsage.md` | Usage guide for developers |
| `Docs/Concepts/Core/Simulation_LOD_And_Environment_Fields.md` | LOD integration context |

### Test Files

| File Path | Purpose | Status |
|-----------|---------|--------|
| `Assets/Tests/Playmode/SpatialRegistryPerformanceTests.cs` | Performance validation | ✅ Complete |
| `Assets/Tests/Playmode/SpatialGridBuildSystemTests.cs` | Build system tests | ✅ Complete |
| `Assets/Tests/Integration/SpatialQueryTests.cs` | Query correctness tests | ✅ Complete |
| `Assets/Tests/Integration/SpatialGridSnapshotTests.cs` | Rewind snapshot tests | ✅ Complete |

---

## Specifications

### Data Model

**Core Components:**

```csharp
// Configuration singleton (authored via SpatialPartitionProfile)
public struct SpatialGridConfig : IComponentData
{
    public float CellSize;           // Meters per cell (e.g., 10.0f)
    public float3 WorldMin;          // World-space minimum bounds
    public float3 WorldMax;          // World-space maximum bounds
    public int3 CellCounts;          // Grid dimensions (e.g., 100×100×1 for 2D)
    public uint HashSeed;            // Determinism seed
    public byte ProviderId;          // Provider selection (0 = Hashed, 1 = Uniform, etc.)
}

// Runtime state singleton
public struct SpatialGridState : IComponentData
{
    public int ActiveBufferIndex;           // Double-buffer index (0 or 1)
    public int TotalEntries;                // Total entities in grid
    public uint Version;                    // Incremented on rebuild (for invalidation)
    public uint LastUpdateTick;             // Last rebuild tick
    public uint LastDirtyTick;              // Last dirty operation tick
    public uint DirtyVersion;               // Incremented on dirty operations
    public int DirtyAddCount;               // Dirty additions since last rebuild
    public int DirtyUpdateCount;            // Dirty updates since last rebuild
    public int DirtyRemoveCount;            // Dirty removals since last rebuild
    public float LastRebuildMilliseconds;   // Performance metric
    public SpatialGridRebuildStrategy LastStrategy; // Full, Partial, or None
}

// Buffer: Cell range metadata (one per cell)
[InternalBufferCapacity(0)]
public struct SpatialGridCellRange : IBufferElementData
{
    public int StartIndex;  // Index into SpatialGridEntry buffer
    public int Count;       // Number of entities in this cell
}

// Buffer: Flattened entity list (all cells concatenated)
[InternalBufferCapacity(0)]
public struct SpatialGridEntry : IBufferElementData
{
    public Entity Entity;
    public float3 Position;
    public int CellId;
}

// Tag component (entities to be indexed must have this)
public struct SpatialIndexedTag : IComponentData { }

// Residency component (cached cell assignment per entity)
public struct SpatialGridResidency : ICleanupComponentData
{
    public int CellId;
    public float3 LastPosition;
    public uint Version;  // SpatialGridState.Version when residency was computed
}
```

**Query Descriptors:**

```csharp
public struct SpatialQueryDescriptor
{
    public float3 Origin;
    public float Radius;
    public int MaxResults;
    public SpatialQueryOptions Options;  // IgnoreSelf, ProjectToXZ, RequireDeterministicSorting
    public float Tolerance;
    public Entity ExcludedEntity;
}

[Flags]
public enum SpatialQueryOptions : byte
{
    None = 0,
    IgnoreSelf = 1 << 0,
    ProjectToXZ = 1 << 1,
    RequireDeterministicSorting = 1 << 2
}
```

### Provider Interface

**Abstraction Pattern:**

```csharp
public interface ISpatialGridProvider
{
    // Rebuild grid from indexed entities
    void Rebuild(
        ref SpatialGridProviderContext context,
        in SpatialGridConfig config,
        ref DynamicBuffer<SpatialGridEntry> entries,
        ref DynamicBuffer<SpatialGridCellRange> ranges,
        ref SpatialGridState state);

    // Validate configuration
    bool ValidateConfig(in SpatialGridConfig config, out string errorMessage);

    // Query methods (called by SpatialQueryHelper)
    void QueryRadius(...);
    void QueryKNearest(...);
    void QueryAABB(...);
}
```

**Implementations:**
- **HashedSpatialGridProvider:** Morton/Z-curve hash-based grid (current default)
- **UniformSpatialGridProvider:** Regular 3D grid with flat indexing

### Query API

**Core Query Methods (Burst-compiled):**

```csharp
[BurstCompile]
public static class SpatialQueryHelper
{
    // Get all entities within radius
    public static void GetEntitiesWithinRadius(
        ref float3 position,
        float radius,
        in SpatialGridConfig config,
        in DynamicBuffer<SpatialGridCellRange> ranges,
        in DynamicBuffer<SpatialGridEntry> entries,
        ref NativeList<Entity> results);

    // Find nearest entity
    public static bool FindNearestEntity(
        ref float3 position,
        in SpatialGridConfig config,
        in DynamicBuffer<SpatialGridCellRange> ranges,
        in DynamicBuffer<SpatialGridEntry> entries,
        out Entity nearest,
        out float distance);

    // Get entities in specific cell
    public static void GetCellEntities(
        int3 cellCoords,
        in SpatialGridConfig config,
        in DynamicBuffer<SpatialGridCellRange> ranges,
        in DynamicBuffer<SpatialGridEntry> entries,
        ref NativeList<Entity> results);

    // Overlap axis-aligned bounding box
    public static void OverlapAABB(
        float3 min,
        float3 max,
        in SpatialGridConfig config,
        in DynamicBuffer<SpatialGridCellRange> ranges,
        in DynamicBuffer<SpatialGridEntry> entries,
        ref NativeList<Entity> results);

    // Batched k-nearest queries (jobified)
    public static void FindKNearestInRadius<TFilter>(
        NativeArray<SpatialQueryDescriptor> descriptors,
        in SpatialGridConfig config,
        in DynamicBuffer<SpatialGridCellRange> ranges,
        in DynamicBuffer<SpatialGridEntry> entries,
        ref NativeList<SpatialQueryRange> resultRanges,
        ref NativeList<Entity> results,
        TFilter filter) where TFilter : ISpatialQueryFilter;
}
```

### Hash Function (Morton/Z-Curve)

**Deterministic 3D → 1D Encoding:**

```csharp
// Quantize world position to cell coordinates
public static void Quantize(in float3 position, in SpatialGridConfig config, out int3 cell)
{
    var local = (position - config.WorldMin) / config.CellSize;
    cell = (int3)math.floor(math.clamp(local, float3.zero, (float3)(config.CellCounts - 1)));
}

// Flatten 3D cell coords to 1D index
public static int Flatten(in int3 cell, in SpatialGridConfig config)
{
    return cell.x * config.CellCounts.y * config.CellCounts.z
         + cell.y * config.CellCounts.z
         + cell.z;
}

// Morton encoding (space-filling curve for better cache locality)
public static uint MortonKey(in int3 cell, uint seed = 0u)
{
    // Interleaves x, y, z bits: ...z1y1x1z0y0x0
    // XOR with seed for determinism
}
```

### Rebuild Strategy

**Update Pipeline:**

1. **Dirty Tracking:** `SpatialGridDirtyTrackingSystem` tracks entities with changed `LocalTransform` or added/removed `SpatialIndexedTag`
2. **Rebuild Decision:** `SpatialGridBuildSystem` checks dirty counts vs. thresholds:
   - **Full Rebuild:** Config changed, first build, or dirty ratio > threshold
   - **Partial Rebuild:** Small dirty count (< threshold), update only affected cells
   - **Skip:** No dirty operations, no config change
3. **Rebuild Execution:** Provider rebuilds staging buffers, then swaps to active buffers (double-buffering)
4. **Registry Sync:** Updates `SpatialRegistryMetadata` with registry handles for domain queries

**Partial Rebuild Thresholds:**

```csharp
public struct SpatialRebuildThresholds : IComponentData
{
    public int MaxDirtyOpsForPartialRebuild;      // Default: 100
    public float MaxDirtyRatioForPartialRebuild;  // Default: 0.1 (10%)
    public int MinEntryCountForPartialRebuild;    // Default: 1000
}
```

### Registry Integration

**Spatial Metadata in Registries:**

```csharp
// Registry entries store spatial metadata (e.g., ResourceRegistryEntry)
public struct ResourceRegistryEntry
{
    // ... domain fields ...
    public int CellId;           // Spatial cell ID (cached)
    public uint SpatialVersion;  // SpatialGridState.Version when cell ID computed
}

// Registry bridge systems populate CellId/SpatialVersion from SpatialGridResidency
// Consumers check SpatialVersion to detect stale entries
```

**SpatialRegistryMetadata Singleton:**

```csharp
// Links spatial grid to domain registries
public struct SpatialRegistryMetadata : IComponentData
{
    public FixedList128Bytes<RegistryHandle> Handles;  // Registry entity references
    public uint Version;  // Updated when grid rebuilds
}
```

---

## Integration Points

### 1. Registry Systems

**Pattern:** Registry entries cache `CellId` and `SpatialVersion` for efficient filtering.

**Integrated Registries:**
- ✅ Resource Registry (`ResourceRegistrySystem`)
- ✅ Storehouse Registry (`StorehouseRegistrySystem`)
- ✅ Villager Registry (`VillagerRegistrySystem`)
- ✅ Spawner Registry (`SpawnerRegistrySystem`)
- ✅ Transport Registry (`TransportRegistrySystem`)
- ✅ Miracle Registry (`MiracleRegistrySystem`)
- ✅ Construction Registry (`ConstructionRegistrySystem`)

**Integration Flow:**
1. Registry bridge system queries `SpatialGridResidency` component on entities
2. If residency exists and version matches, use cached `CellId`
3. Otherwise, compute cell from position (fallback)
4. Store `CellId`/`SpatialVersion` in registry entry

### 2. Villager Systems

**Usage:** Resource/storehouse candidate selection before ranking.

**Pattern:**
```csharp
// 1. Spatial query to narrow candidates
var candidates = new NativeList<Entity>(Allocator.Temp);
SpatialQueryHelper.GetEntitiesWithinRadius(
    villagerPos, searchRadius, config, ranges, entries, ref candidates);

// 2. Filter by component/registry
foreach (var candidate in candidates)
{
    if (resourceLookup.HasComponent<ResourceSourceConfig>(candidate))
    {
        // Rank and select...
    }
}
```

**Systems:**
- `VillagerJobSystems.cs` (assignment & delivery)
- `VillagerTargetingSystem.cs` (target selection)

### 3. AI Systems

**Usage:** k-NN sensor queries for agent perception.

**Pattern:**
```csharp
// Batched k-nearest queries per sensor
var job = new SpatialKNearestBatchJob
{
    Descriptors = sensorDescriptors,
    Config = spatialConfig,
    Ranges = cellRanges,
    Entries = gridEntries,
    Results = resultList
};
job.ScheduleParallel(sensorCount, 1, default).Complete();
```

**Systems:**
- `Systems/AI/AISystems.cs` (sensor updates)
- `Systems/Navigation/SpatialSensorUpdateSystem.cs` (spatial sensor component)

### 4. Divine Hand / Miracles

**Usage:** Hover highlighting and target selection.

**Pattern:**
```csharp
// Find pickable entities near cursor
SpatialQueryHelper.GetEntitiesWithinRadius(
    cursorPos, hoverRadius, config, ranges, entries, ref pickables);

// Filter by component (e.g., PickableTag)
foreach (var entity in pickables)
{
    if (pickableLookup.HasComponent<PickableTag>(entity))
    {
        // Highlight or select...
    }
}
```

**Systems:**
- `Projects/Godgame/Systems/DivineHandSystems.cs`

### 5. Rewind Integration

**Pattern:** Spatial grid rebuilds only during `RewindMode.Record`; playback/catch-up reuse cached buffers.

**Systems:**
- `SpatialGridBuildSystem` checks `RewindState.Mode` (skips rebuild if not Record)
- `SpatialGridSnapshotSystem` captures grid state for validation (HistorySystemGroup)
- `SpatialRewindGuardSystem` guards SpatialSystemGroup execution

**Snapshot Structure:**
```csharp
public struct SpatialGridSnapshot
{
    public uint Version;
    public int TotalEntries;
    public BlobAssetReference<SpatialGridBufferSnapshot> BufferSnapshot;
}
```

### 6. Environment Grids

**Relationship:** Spatial grid indexes entities; environment grids store scalar fields (moisture, temperature, etc.) per cell. Both use similar chunk-based structures but serve different purposes.

**Files:**
- `Runtime/Runtime/Environment/EnvironmentGrids.cs` (moisture, temperature, sunlight, wind)
- `Docs/Concepts/Core/Simulation_LOD_And_Environment_Fields.md` (LOD context)

### 7. Navigation

**Relationship:** Spatial grid provides entity queries; navigation uses terrain/navmesh graphs. Spatial grid can be used for "find nearest nav agent" queries.

**Files:**
- `Runtime/Runtime/Spatial/SpatialNavigation.cs` (navigation helpers)

---

## Gaps and Limitations

### 1. Incomplete Integration Coverage

**Status:** Not all systems use spatial queries.

**Gaps:**
- ❌ **Logistics systems** (miner/hauler routing) still query EntityManager directly
- ❌ **Combat systems** (projectile targeting, threat detection) may use ad-hoc scans
- ❌ **Miracle systems** (rain clouds, blessings) partially integrated (some still scan)

**Impact:** Suboptimal performance at scale (O(n) scans instead of O(k) spatial queries).

**TODO:**
- Integrate `VesselRoutingSystem` (Space4X) with spatial queries
- Migrate combat targeting to spatial queries
- Complete miracle system integration

### 2. Rewind Determinism Validation

**Status:** Snapshot system exists but validation incomplete.

**Gaps:**
- ❌ No automated rewind determinism tests (record 100 ticks → rewind → verify grid state)
- ❌ No validation that query results match during playback vs. record
- ⚠️ Snapshot system exists but not fully exercised

**Impact:** Risk of non-deterministic behavior in rewind scenarios.

**TODO:**
- Add playmode test: record 100 ticks → rewind to tick 50 → verify grid state matches original tick 50
- Validate query results during playback mode
- Stress test with large entity counts during rewind

### 3. Multi-Resolution Grid Support

**Status:** Not implemented (single-resolution grid only).

**Gaps:**
- ❌ No hierarchical grid (macro cells → micro cells)
- ❌ No multi-resolution query support (coarse for large-scale, fine for local)
- ❌ Design concept exists (`SpatialServicesConcepts.md`) but not implemented

**Impact:** Limited optimization for mixed-scale queries (e.g., climate zones vs. local AI).

**Future Work:**
- Implement two-level grid (macro 100m cells, micro 10m cells)
- Add query API that selects resolution based on radius

### 4. Partial Rebuild Optimization

**Status:** Partial rebuild exists but may not be aggressive enough.

**Gaps:**
- ⚠️ Partial rebuild thresholds may be too conservative (triggers full rebuild often)
- ❌ No cell-level dirty tracking (rebuilds entire cells even if only one entity moved)
- ❌ No incremental cell updates (always rebuilds affected cells from scratch)

**Impact:** Unnecessary full rebuilds at moderate entity counts.

**Potential Improvements:**
- Tune thresholds based on profiling data
- Add cell-level dirty tracking (mark only affected cells)
- Incremental cell updates (add/remove entities without rebuilding entire cell)

### 5. GPU Offload Hooks

**Status:** Not implemented (CPU-only).

**Gaps:**
- ❌ No GPU compute shader integration for extreme densities (>1M entities)
- ❌ No BVH/quadtree GPU-friendly structure
- ❌ Design roadmap exists but not implemented

**Impact:** CPU bottleneck at extreme scales (>1M entities may require GPU acceleration).

**Future Work:**
- Design GPU-friendly provider interface
- Implement compute shader rebuild for extreme densities
- Add GPU query API (if needed)

### 6. Region Tagging / Cell Metadata

**Status:** Concept exists, not implemented.

**Gaps:**
- ❌ No `SpatialCellMetadata` (biome, threat level, ownership per cell)
- ❌ No spatial region authoring (polygon → cell metadata projection)
- ❌ Concept documented (`SpatialServicesConcepts.md`) but not implemented

**Impact:** AI/miracles must query authoring data separately instead of reading cell metadata.

**Future Work:**
- Add `NativeArray<SpatialCellMetadata>` to spatial state
- Implement `SpatialRegionBakeSystem` (projects authored polygons to cells)
- Add metadata query helpers

### 7. 3D Navigation Layers

**Status:** 2D navigation supported; 3D hooks incomplete.

**Gaps:**
- ⚠️ 2D (XZ plane) navigation supported, but 3D layer abstraction (`INavLayerProvider`) not formalized
- ❌ No true 3D volume cost fields (for flying/underground agents)
- ❌ TODO item: "Support 2D navigation out of the box and define config/runtime hooks for true 3D layers"

**Impact:** Limited support for 3D navigation (flying agents, underground paths).

**TODO:**
- Formalize `INavLayerProvider` interface
- Add 3D volume cost field support
- Define config hooks for 3D layers

### 8. Query Broker Pattern

**Status:** Concept exists, not implemented.

**Gaps:**
- ❌ No `SpatialQueryBrokerSystem` (multiplexes queries to reduce redundancy)
- ❌ Consumers request queries independently (no deduplication)
- ❌ Concept documented (`SpatialServicesConcepts.md`) but not implemented

**Impact:** Redundant queries if multiple systems query same region (e.g., AI + logistics).

**Future Work:**
- Implement query broker (cache query descriptors per tick)
- Multiplex results to multiple subscribers
- Reduce redundant radius/AABB queries

---

## Malpractices and Anti-Patterns

### 1. Direct EntityManager Scans

**Problem:** Some systems still scan all entities with `EntityManager.GetAllEntities()` or `EntityQuery` without spatial filtering.

**Examples:**
- Combat systems may scan all entities for threats
- Logistics systems may scan all transport entities for routing
- Miracle systems may scan all targets for area effects

**Fix:** Use `SpatialQueryHelper.GetEntitiesWithinRadius()` to narrow candidates first, then filter by component.

**Impact:** O(n) scans become O(k) where k << n (entities in radius vs. all entities).

### 2. Missing SpatialIndexedTag

**Problem:** Entities that should be indexed don't have `SpatialIndexedTag`, causing them to be ignored by spatial queries.

**Examples:**
- New entity types added without tag
- Entities created at runtime without tag

**Fix:** Ensure all entities that need spatial queries have `SpatialIndexedTag` added during authoring or creation.

**Impact:** Entities invisible to spatial queries (fallback to full scans).

### 3. Stale Registry Entries

**Problem:** Registry entries with cached `CellId`/`SpatialVersion` may become stale if spatial grid rebuilds but registry doesn't update.

**Examples:**
- Registry bridge system doesn't check `SpatialVersion` before using cached `CellId`
- Entities move but registry entry `CellId` not updated

**Fix:** Always check `SpatialVersion` matches `SpatialGridState.Version` before using cached `CellId`. Recompute if stale.

**Impact:** Incorrect filtering (entities in wrong cells), performance degradation.

### 4. Ignoring Rewind Mode

**Problem:** Systems query spatial grid during `RewindMode.Playback` without checking if grid is up-to-date.

**Examples:**
- Query during playback when grid hasn't rebuilt (uses stale cached buffers)
- Assumes grid state matches current tick during catch-up

**Fix:** Check `RewindState.Mode` and `SpatialGridState.Version` before relying on query results. Document that grid only rebuilds during Record mode.

**Impact:** Non-deterministic query results during rewind.

### 5. Inefficient Query Patterns

**Problem:** Querying large radii repeatedly, or querying every tick when update interval could be used.

**Examples:**
- Querying 100m radius every tick for AI sensors (should use update interval)
- Multiple systems querying same region independently (should use query broker)

**Fix:** Use `SpatialSensor.UpdateIntervalTicks` to throttle queries. Consider query broker for shared queries.

**Impact:** Unnecessary CPU cost (query overhead × frequency).

### 6. Buffer Allocation in Hot Paths

**Problem:** Allocating `NativeList<Entity>` for query results every frame without pooling.

**Examples:**
```csharp
// BAD: Allocates every frame
var results = new NativeList<Entity>(Allocator.Temp);
SpatialQueryHelper.GetEntitiesWithinRadius(...);
results.Dispose();
```

**Fix:** Use `Allocator.TempJob` and dispose after job completes, or pool buffers if querying every frame.

**Impact:** GC allocations, frame time spikes.

### 7. Not Using Registry Integration

**Problem:** Filtering query results by component lookup instead of using registry entries with cached `CellId`.

**Examples:**
```csharp
// BAD: Component lookup for every candidate
foreach (var candidate in spatialResults)
{
    if (resourceLookup.HasComponent<ResourceSourceConfig>(candidate))
    {
        // ...
    }
}

// GOOD: Use registry entries (already filtered by CellId)
var registryEntries = resourceRegistry.Entries;
foreach (var entry in registryEntries)
{
    if (entry.CellId == currentCellId && entry.SpatialVersion == gridVersion)
    {
        // Process entry.Entity...
    }
}
```

**Fix:** Prefer registry queries with spatial filtering over component lookups.

**Impact:** Extra component lookups, cache misses.

### 8. Configuration Errors

**Problem:** Invalid `SpatialGridConfig` (zero cell size, negative bounds, zero cell counts) causes undefined behavior.

**Examples:**
- `CellSize = 0` (division by zero in hash functions)
- `WorldMin >= WorldMax` (invalid bounds)
- `CellCounts = (0, 0, 0)` (zero cells)

**Fix:** Validation in `SpatialPartitionAuthoring` and `ISpatialGridProvider.ValidateConfig()`. Systems check config validity before use.

**Impact:** Crashes, incorrect cell assignments, infinite loops.

---

## Performance Characteristics

### Scalability

**Current Performance:**
- ✅ **100k entities:** ~1-2ms rebuild time (full), <0.5ms (partial)
- ✅ **Query time:** O(k) where k = entities in queried cells (typically <100 for 10m radius)
- ✅ **Memory:** ~O(n) where n = indexed entities (compact SoA layout)

**Target Performance:**
- 🎯 **1M entities:** <10ms rebuild time (with optimizations)
- 🎯 **Query time:** <0.1ms for typical radius queries (10m)
- 🎯 **Zero GC allocations** per frame

### Bottlenecks

**Known Bottlenecks:**
1. **Full rebuilds:** O(n) cost when dirty ratio > threshold
2. **Large radius queries:** O(cells × avg_entities_per_cell) for radius > cell size
3. **Registry sync:** O(registry_count × entry_count) when updating spatial metadata

**Mitigations:**
- Partial rebuilds reduce cost for small changes
- Query radius limits (typical: 10-50m)
- Registry sync throttled (updates on rebuild, not every tick)

### Memory Footprint

**Per-Entity Cost:**
- `SpatialGridEntry`: 20 bytes (Entity + float3 + int)
- `SpatialGridResidency`: 24 bytes (int + float3 + uint)
- Total: ~44 bytes per indexed entity (plus buffer overhead)

**Grid Overhead:**
- `SpatialGridCellRange`: 8 bytes per cell (typically 10k-100k cells)
- Total grid overhead: ~80KB-800KB for typical scenes

---

## Recommendations

### Short-Term (Next Sprint)

1. **Complete Integration Coverage:**
   - Migrate logistics systems to spatial queries
   - Migrate combat targeting to spatial queries
   - Complete miracle system integration

2. **Rewind Determinism Tests:**
   - Add playmode test for rewind validation
   - Verify query results during playback

3. **Tune Partial Rebuild Thresholds:**
   - Profile typical dirty counts
   - Adjust thresholds to minimize full rebuilds

### Medium-Term (Next Quarter)

1. **Multi-Resolution Grid:**
   - Implement two-level grid (macro/micro)
   - Add query API for resolution selection

2. **Region Tagging:**
   - Implement `SpatialCellMetadata`
   - Add `SpatialRegionBakeSystem`

3. **Query Broker:**
   - Implement query deduplication
   - Multiplex results to subscribers

### Long-Term (Roadmap)

1. **GPU Offload:**
   - Design GPU-friendly provider interface
   - Implement compute shader rebuild

2. **3D Navigation Layers:**
   - Formalize `INavLayerProvider`
   - Add 3D volume cost fields

3. **Advanced Providers:**
   - BVH provider for dynamic scenes
   - Quadtree provider for 2D-only scenes

---

## Related Documentation

- **Spatial Grid Advisory:** `Docs/Concepts/Core/Spatial_Grid_System_Advisory.md` - **⭐ Recommended polish items and action plan**
- **Design Notes:** `Documentation/DesignNotes/SpatialPartitioning.md`
- **TODO List:** `Docs/TODO/SpatialServices_TODO.md`
- **Usage Guide:** `Docs/Guides/SpatialQueryUsage.md`
- **Registry Integration:** `Runtime/Runtime/Registry/RegistryQueryHelpers.cs`
- **Runtime Lifecycle:** `Docs/TruthSources/RuntimeLifecycle_TruthSource.md`

---

**For Implementers:** Focus on integration coverage, rewind validation, and partial rebuild optimization  
**For Designers:** Focus on query patterns, registry integration, and performance tuning  
**For Architects:** Focus on provider abstraction, multi-resolution support, and GPU offload roadmap  
**For Reviewers:** See `Spatial_Grid_System_Advisory.md` for targeted polish recommendations (6DOF support, rewind correctness, future-proofing)

