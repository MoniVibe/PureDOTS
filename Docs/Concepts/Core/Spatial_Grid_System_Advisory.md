# Spatial Grid System Advisory

**Status:** Recommendations / Action Plan
**Category:** Core - Spatial Grid Polish
**Audience:** Implementers / Architects
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Purpose

**Primary Goal:** Address targeted polish items to make the spatial grid system more robust, future-proof, and aligned with 6DOF worlds, large-scale coordinates, and integration with terrain/liquids/fields.

**Focus Areas:**
1. **6DOF vs 2.5D semantics** — Support arbitrary orientation (planets, ship interiors, 6DOF agents)
2. **Rewind/fast-forward correctness** — Explicit snapshot/restore or rebuild semantics
3. **One spatial truth** — Unified cell addressing for terrain/liquids/fields alignment

---

## 1. Make "2.5D" a First-Class Projection Policy

### Current State

**Problem:** 2.5D is encoded as `ProjectToXZ` in query options (`SpatialQueryOptions`). This is too rigid for:
- Planets with local "up" (gravity direction varies by position)
- Ship interiors with arbitrary orientation
- 6DOF agents (flying) that sometimes still want "ground-plane" reasoning

**Current Implementation:**
```csharp
[Flags]
public enum SpatialQueryOptions : byte
{
    None = 0,
    IgnoreSelf = 1 << 0,
    ProjectToXZ = 1 << 1,  // ❌ Too rigid
    RequireDeterministicSorting = 1 << 2
}
```

### Proposed Solution

**Replace/Extend with Projection Policy:**

```csharp
public enum SpatialProjectionMode : byte
{
    None = 0,                    // Full 3D distance
    WorldPlane = 1,              // Project to world XZ plane (current ProjectToXZ)
    GravityTangent = 2,          // Project to plane tangent to gravity at query origin
    NavLayerPlane = 3            // Project to plane defined by NavLayerId
}

public struct SpatialQueryDescriptor
{
    // ... existing fields ...
    public SpatialProjectionMode ProjectionMode;      // ✅ New: projection policy
    public float3 ProjectionPlaneNormal;              // For WorldPlane mode (optional, can be inferred)
    public byte NavLayerId;                           // For NavLayerPlane mode
    public Entity FrameId;                            // ✅ New: spatial frame reference
}
```

**Implementation Notes:**
- `GravityTangent` mode: Sample gravity vector at query origin, project to plane perpendicular to gravity
- `NavLayerPlane` mode: Use navigation layer definition to get plane normal
- Store (or provide) `NavLayerId` / `FrameId` in descriptor, so callers don't guess
- Query API automatically selects projection based on `ProjectionMode`

**Result:** The same query API supports space, planet surface, and rotated interiors cleanly.

### Migration Path

1. **Add `SpatialProjectionMode` enum** (keep `ProjectToXZ` as deprecated alias for `WorldPlane`)
2. **Extend `SpatialQueryDescriptor`** with `ProjectionMode`, `NavLayerId`, `FrameId`
3. **Update `SpatialQueryHelper`** to respect projection mode in distance calculations
4. **Update consumers** to use new projection modes (e.g., `GravityTangent` for planet queries)
5. **Deprecate `ProjectToXZ` flag** (remove in future version)

---

## 2. Add a "Frame / Origin" Concept for Large Worlds (Space4X)

### Current State

**Problem:** `SpatialGridConfig` assumes a single `WorldMin`/`WorldMax` and `float3` positions. For Space4X scale, float precision and huge bounds become the silent failure mode.

**Current Implementation:**
```csharp
public struct SpatialGridConfig : IComponentData
{
    public float3 WorldMin;       // ❌ Single world bounds (float precision issues at scale)
    public float3 WorldMax;
    public float CellSize;
    // ...
}
```

### Proposed Solution

**Introduce SpatialFrame (or SpaceSector) Addressing:**

```csharp
// Frame/sector addressing
public struct SpatialFrame : IComponentData
{
    public int3 SectorCoord;      // Sector coordinates (galactic/regional grid)
    public float3 SectorOrigin;   // World-space origin of this sector
    public float3 SectorExtent;   // Extent of this sector
    public Entity FrameEntity;    // Entity representing this frame
}

// Per-frame grid configuration
public struct SpatialGridConfig : IComponentData
{
    public Entity FrameId;        // ✅ New: which frame this grid belongs to
    public float3 LocalMin;       // ✅ Changed: local bounds within frame (not world)
    public float3 LocalMax;
    public float CellSize;
    // ... rest unchanged
}

// Query descriptor includes frame
public struct SpatialQueryDescriptor
{
    // ... existing fields ...
    public Entity FrameId;        // ✅ Which frame/sector to query (null = current frame)
}

// Position encoding (for storage)
public struct SpatialFramePosition
{
    public int3 SectorCoord;
    public float3 LocalPos;       // Or fixed-point local for precision
}
```

**Implementation Notes:**
- Grid quantization uses `(Sector, LocalPos) → stable cell keys`
- Run one grid per frame (system/sector/ship interior) instead of one galaxy grid
- Keep query signature the same; descriptor just carries `FrameId`
- Frame-local grids are smaller and cheaper to snapshot/restore (perfect for rewind)

**Benefits:**
- Float precision preserved (each frame has reasonable local bounds)
- Scalability (multiple grids for different regions/ships)
- Rewind-friendly (frame-local snapshots are smaller)
- Matches Space4X architecture (sectors, ship interiors, planetary surfaces)

### Migration Path

1. **Add `SpatialFrame` component** (singleton per frame/sector)
2. **Extend `SpatialGridConfig`** with `FrameId` and local bounds
3. **Update quantization** to use frame-local coordinates
4. **Update query API** to resolve frame from `FrameId` or current context
5. **Add frame resolution helpers** (world position → frame + local position)

---

## 3. Rewind/Fast-Forward: Don't Rely on "Skip Rebuild in Playback"

### Current State

**Problem:** Current rule "rebuild only in Record mode; playback/catch-up reuse cached buffers" can only be correct if the grid state you query is restored to the same tick as transforms.

**Current Implementation:**
```csharp
// SpatialGridBuildSystem.cs
if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
{
    return;  // ❌ Skip rebuild in playback - assumes cached buffers match current tick
}
```

**Risk:** During playback/catch-up, transforms are restored to tick T, but grid state may be from a different tick, causing inconsistent query results.

### Proposed Solution

**Pick One Approach and Enforce It Everywhere:**

#### Option A (Recommended): Snapshot/Restore Grid

**On rewind to tick T:** Restore entries/ranges/state from `SpatialGridSnapshot(T)` (snapshot structs already exist).

**During playback:** Never rebuild; just "current snapshot = current tick".

**Implementation:**
```csharp
// SpatialGridSnapshotSystem already captures snapshots
public struct SpatialGridSnapshot
{
    public uint Version;
    public uint Tick;
    public int TotalEntries;
    public BlobAssetReference<SpatialGridBufferSnapshot> BufferSnapshot;  // ✅ Already exists
}

// SpatialGridBuildSystem
void OnUpdate(ref SystemState state)
{
    var rewindState = SystemAPI.GetSingleton<RewindState>();
    
    if (rewindState.Mode == RewindMode.Playback)
    {
        // ✅ Restore from snapshot instead of rebuilding
        RestoreSnapshotForTick(rewindState.CurrentTick);
        return;
    }
    
    if (rewindState.Mode == RewindMode.Record)
    {
        // Normal rebuild logic
        RebuildGrid(...);
        
        // Capture snapshot (already done by SpatialGridSnapshotSystem)
        return;
    }
}

void RestoreSnapshotForTick(uint targetTick)
{
    // Look up snapshot for targetTick
    // Restore entries/ranges/state from snapshot
    // Grid queries now use restored state
}
```

#### Option B: Rebuild from Rewound Transforms

**On rewind/playback tick:** Rebuild deterministically (expensive), but simplest correctness story.

**Implementation:**
```csharp
// SpatialGridBuildSystem
void OnUpdate(ref SystemState state)
{
    var rewindState = SystemAPI.GetSingleton<RewindState>();
    
    // ✅ Always rebuild (even in playback) - ensures correctness
    if (rewindState.Mode != RewindMode.Record && rewindState.Mode != RewindMode.Playback)
    {
        return;  // Only rebuild in Record or Playback
    }
    
    // Rebuild from current transform state (deterministic)
    RebuildGrid(...);
}
```

### Determinism Test

**Add one determinism test:**
1. Record 200 ticks
2. Snapshot at ticks 50, 100, 150
3. Rewind to tick 100
4. Compare (grid hash + query outputs for a fixed set of descriptors)

**Expected:** Grid state and query results match original tick 100 exactly.

**Test Implementation:**
```csharp
[Test]
public void SpatialGridRewindDeterminism()
{
    // Record 200 ticks
    RecordTicks(200);
    
    // Capture snapshots at checkpoints
    var snapshot50 = CaptureSnapshot(50);
    var snapshot100 = CaptureSnapshot(100);
    var snapshot150 = CaptureSnapshot(150);
    
    // Rewind to tick 100
    RewindToTick(100);
    
    // Verify grid state matches snapshot
    AssertGridStateMatches(snapshot100);
    
    // Verify query results match
    var queryDescriptors = CreateFixedQuerySet();
    foreach (var descriptor in queryDescriptors)
    {
        var originalResults = QueryAtTick(100, descriptor);
        var rewindResults = QueryCurrent(descriptor);
        AssertQueryResultsMatch(originalResults, rewindResults);
    }
}
```

**Recommendation:** Use Option A (snapshot/restore) for performance, but ensure snapshots are captured correctly and restored deterministically.

---

## 4. Deterministic Ordering: Make It Unconditional Inside Cells

### Current State

**Problem:** You've got `RequireDeterministicSorting` and seedable Morton hashing, but the remaining risk is partial rebuilds changing per-cell ordering. This can cause "heisenbugs" where AI choices differ because two entities swapped order in a cell after partial rebuild.

**Current Implementation:**
```csharp
public enum SpatialQueryOptions : byte
{
    // ...
    RequireDeterministicSorting = 1 << 2  // ❌ Optional, may not be used
}
```

### Proposed Solution

**Always Sort Entries Within Each Cell by Stable Key:**

```csharp
// During grid rebuild, always sort entries within each cell
void SortCellEntries(DynamicBuffer<SpatialGridEntry> entries, int cellStart, int cellCount)
{
    // ✅ Always sort by stable key (unconditional)
    var cellSlice = entries.GetSubArray(cellStart, cellCount);
    
    // Sort by (Entity.Index, Entity.Version) for stability
    // Or use PersistentId if available
    NativeSortExtensions.Sort(cellSlice, new EntityIndexComparer());
}

// Query results are stable by:
// 1. Iterating cells in deterministic order (cell ID order)
// 2. Iterating entries in sorted order (within each cell)
// 3. Optional final sort only when callers ask for it (e.g., by distance)
```

**Implementation Notes:**
- Sort entries within each cell during rebuild (full or partial)
- Use stable key: `(Entity.Index, Entity.Version)` or `PersistentId` if available
- Query iteration order: cells → sorted entries within cell → optional distance sort
- Remove `RequireDeterministicSorting` flag (always deterministic)

**Result:** Query results are always stable regardless of rebuild strategy.

### Migration Path

1. **Add cell entry sorting** to rebuild logic (both full and partial)
2. **Use stable comparator** (`EntityIndexComparer` or `PersistentIdComparer`)
3. **Update query iteration** to respect sorted order
4. **Remove `RequireDeterministicSorting` flag** (always deterministic now)
5. **Add test** to verify query results are stable across rebuilds

---

## 5. Multi-Resolution: Implement a Two-Grid MVP Now (Macro+Micro)

### Current State

**Problem:** Summary lists "no multi-resolution yet" as a key gap. You will want this for:
- Far-range strategic queries (clusters, villages, fleets)
- Near-range tactical queries (sensors, interactions)

**Current Implementation:**
```csharp
// Single-resolution grid only
public struct SpatialGridConfig : IComponentData
{
    public float CellSize;  // ❌ Single cell size
    // ...
}
```

### Proposed Solution

**Add Second Macro Grid (Minimal Implementation):**

```csharp
// Grid resolution hint
public enum SpatialGridResolution : byte
{
    Macro = 0,   // Coarse (100-500m cells) for strategic queries
    Micro = 1    // Fine (5-20m cells) for tactical queries
}

// Extend config to support both grids
public struct SpatialGridConfig : IComponentData
{
    // Micro grid (existing, fine resolution)
    public float MicroCellSize;
    public int3 MicroCellCounts;
    
    // ✅ New: Macro grid (coarse resolution)
    public float MacroCellSize;        // e.g., 100m (20x micro cell size)
    public int3 MacroCellCounts;
    
    // Common
    public float3 WorldMin;
    public float3 WorldMax;
    public uint HashSeed;
    public byte ProviderId;
}

// Query descriptor can specify resolution
public struct SpatialQueryDescriptor
{
    // ... existing fields ...
    public SpatialGridResolution ResolutionHint;  // ✅ New: which grid to use
}

// Query API selects grid based on radius or hint
public static void GetEntitiesWithinRadius(
    ref float3 position,
    float radius,
    in SpatialGridConfig config,
    in DynamicBuffer<SpatialGridCellRange> microRanges,  // ✅ Separate buffers
    in DynamicBuffer<SpatialGridEntry> microEntries,
    in DynamicBuffer<SpatialGridCellRange> macroRanges,  // ✅ Separate buffers
    in DynamicBuffer<SpatialGridEntry> macroEntries,
    ref NativeList<Entity> results,
    SpatialGridResolution resolutionHint = SpatialGridResolution.Micro)
{
    // Auto-select resolution based on radius if not specified
    if (resolutionHint == SpatialGridResolution.Micro && radius > config.MacroCellSize * 2f)
    {
        resolutionHint = SpatialGridResolution.Macro;  // Large radius → macro grid
    }
    
    // Query appropriate grid
    if (resolutionHint == SpatialGridResolution.Macro)
    {
        // Use macro ranges/entries
        CollectEntitiesInRadius(ref position, radius, config, macroRanges, macroEntries, ref results);
    }
    else
    {
        // Use micro ranges/entries
        CollectEntitiesInRadius(ref position, radius, config, microRanges, microEntries, ref results);
    }
}
```

**Implementation Notes:**
- Keep current grid as micro (e.g., 5-20m cells)
- Add second macro grid (e.g., 100-500m cells) built from the same indexed entities
- Both grids update together (same rebuild trigger, separate buffers)
- Query chooses grid by radius (auto-select) or caller sets `ResolutionHint`
- Also aligns with terrain/liquid: macro is "where is flooding happening", micro is "who is near me"

**Benefits:**
- Efficient strategic queries (villages, fleets, clusters) use macro grid
- Efficient tactical queries (sensors, interactions) use micro grid
- Same indexed entities, different resolutions
- Future-proof for terrain/liquid integration (macro for regions, micro for local)

### Migration Path

1. **Extend `SpatialGridConfig`** with macro grid parameters
2. **Add macro grid buffers** (`SpatialGridMacroCellRange`, `SpatialGridMacroEntry`)
3. **Update rebuild system** to build both grids simultaneously
4. **Extend query API** with resolution selection (auto or explicit)
5. **Update consumers** to use appropriate resolution (or auto-select based on radius)

---

## 6. Align Future Terrain/Liquids/Fields via Shared Cell Addressing

### Current State

**Problem:** Summary separates "spatial grid indexes entities" vs "environment grids store scalar fields". To make digging + liquids + mana/gravity painless, need unified cell addressing.

**Current Implementation:**
```csharp
// Spatial grid uses its own quantization
SpatialHash.Quantize(position, spatialConfig, out var cell);

// Environment grids use separate quantization (e.g., MoistureGrid)
// ❌ Different cell addressing schemes = mapping complexity
```

### Proposed Solution

**Define One Canonical Cell Addressing Scheme:**

```csharp
// Shared cell addressing service
public static class SpatialCellAddressing
{
    // ✅ Canonical quantization (shared by all systems)
    public static void QuantizeToCell(
        float3 position,
        in SpatialGridConfig config,  // Or unified config
        out int3 cellCoord)
    {
        // Same logic used by spatial grid, terrain, liquids, fields
        var local = (position - config.WorldMin) / config.CellSize;
        cellCoord = (int3)math.floor(math.clamp(local, float3.zero, (float3)(config.CellCounts - 1)));
    }
    
    public static int FlattenCell(int3 cellCoord, in SpatialGridConfig config)
    {
        // Same flattening logic everywhere
        return cellCoord.x * config.CellCounts.y * config.CellCounts.z
             + cellCoord.y * config.CellCounts.z
             + cellCoord.z;
    }
}

// Shared cell ID type
public struct SpatialCellId : IEquatable<SpatialCellId>
{
    public Entity FrameId;     // Which frame/sector
    public int3 CellCoord;     // Cell coordinates within frame
    public int FlatIndex;      // Flattened index (cached)
    
    public bool Equals(SpatialCellId other)
    {
        return FrameId == other.FrameId && math.all(CellCoord == other.CellCoord);
    }
}

// ✅ Optional: Spatial cell metadata (lightweight, updated by environment systems)
public struct SpatialCellMetadata : IComponentData
{
    public SpatialCellId CellId;
    
    // Environment fields (updated by environment systems, not spatial rebuild)
    public float WaterDepth;
    public float HazardLevel;
    public byte BiomeId;
    public float ManaLevel;
    public float GravityMagnitude;
    public Entity Ownership;
    // ... extensible
}

// Environment systems write to metadata (per cell)
public struct EnvironmentFieldUpdate
{
    public SpatialCellId CellId;
    public float WaterDepth;
    public float ManaLevel;
    // ...
}
```

**Implementation Notes:**
- Define one canonical cell addressing scheme (quantize + flatten) as shared service
- Ensure environment fields can map from `SpatialCellId` (or at least from same `(FrameId, cellCoord)`)
- Add optional `SpatialCellMetadata` (lightweight) for "AI cheap reads"
- Metadata updated by environment systems, not by spatial rebuild
- "Agents understand cavities fill" becomes: sample `CellMetadata` + react—no fluid prediction in brains

**Benefits:**
- Unified addressing: terrain, liquids, mana, gravity all use same cell coordinates
- Efficient AI queries: read `CellMetadata` instead of querying multiple systems
- Future-proof: new environment fields just add to metadata

### Migration Path

1. **Create `SpatialCellAddressing` utility** (shared quantization/flattening)
2. **Define `SpatialCellId` type** (frame + cell coordinates)
3. **Add `SpatialCellMetadata` component** (per cell, optional)
4. **Update environment systems** to write metadata (water depth, mana, etc.)
5. **Update AI systems** to read metadata (instead of querying multiple systems)
6. **Update terrain/liquid systems** to use shared cell addressing

---

## 7. Query Broker: Start Tiny Where It Matters (Perception/Logistics)

### Current State

**Problem:** "Query broker" gap is real, but you don't need a grand system immediately. Current systems query independently, causing redundancy.

**Current Implementation:**
```csharp
// Multiple systems query independently
// PerceptionUpdateSystem queries for sensors
SpatialQueryHelper.GetEntitiesWithinRadius(sensorPos, radius, ...);

// LogisticsSystem queries for haulers
SpatialQueryHelper.GetEntitiesWithinRadius(haulerPos, radius, ...);

// ❌ No deduplication - redundant queries if multiple sensors query same region
```

### Proposed Solution

**Start Small: Group Queries by Descriptor:**

```csharp
// ✅ Group queries by (FrameId, radius, filter, projection) - run one query per group

// In PerceptionUpdateSystem
void OnUpdate(ref SystemState state)
{
    // Collect all sensor query descriptors
    var sensorDescriptors = new NativeList<SpatialQueryDescriptor>(Allocator.Temp);
    
    foreach (var (sensor, transform) in SystemAPI.Query<RefRO<SpatialSensor>, RefRO<LocalTransform>>())
    {
        sensorDescriptors.Add(new SpatialQueryDescriptor
        {
            Origin = transform.ValueRO.Position,
            Radius = sensor.ValueRO.SensorRadius,
            FrameId = GetCurrentFrame(transform.ValueRO.Position),
            ProjectionMode = SpatialProjectionMode.GravityTangent,  // Shared projection
            // ...
        });
    }
    
    // ✅ Group by (FrameId, radius, projection) - hash key
    var groupedQueries = GroupQueriesByKey(sensorDescriptors);
    
    // Run one query per group, distribute results to sensors
    foreach (var group in groupedQueries)
    {
        var results = new NativeList<Entity>(Allocator.Temp);
        SpatialQueryHelper.GetEntitiesWithinRadius(
            group.RepresentativeOrigin, group.Radius, config, ranges, entries, ref results);
        
        // Distribute results to sensors in this group
        foreach (var sensorIndex in group.SensorIndices)
        {
            FilterAndAssignResults(sensorIndex, results, ...);
        }
    }
}

// For logistics: board/dispatcher queries once per site per tick, not once per hauler
void OnUpdate(ref SystemState state)
{
    // ✅ One query per logistics site (not per hauler)
    var siteQueries = new NativeHashMap<Entity, SpatialQueryDescriptor>(Allocator.Temp);
    
    foreach (var (site, transform) in SystemAPI.Query<RefRO<LogisticsSite>, RefRO<LocalTransform>>())
    {
        siteQueries[site.ValueRO.SiteEntity] = new SpatialQueryDescriptor
        {
            Origin = transform.ValueRO.Position,
            Radius = site.ValueRO.ServiceRadius,
            // ...
        };
    }
    
    // Run one query per site, cache results
    var siteResults = new NativeHashMap<Entity, NativeList<Entity>>(Allocator.Temp);
    foreach (var (siteEntity, descriptor) in siteQueries)
    {
        var results = new NativeList<Entity>(Allocator.Temp);
        SpatialQueryHelper.GetEntitiesWithinRadius(descriptor.Origin, descriptor.Radius, ...);
        siteResults[siteEntity] = results;
    }
    
    // Haulers read from cached results
    foreach (var (hauler, site) in SystemAPI.Query<RefRO<Hauler>, RefRO<AssignedSite>>())
    {
        if (siteResults.TryGetValue(site.ValueRO.SiteEntity, out var candidates))
        {
            // Use candidates...
        }
    }
}
```

**Implementation Notes:**
- Low effort, high win: Group queries by descriptor key (frame, radius, filter, projection)
- In `PerceptionUpdateSystem`: Group sensor descriptors, run one query per group
- For logistics: Board/dispatcher queries once per site per tick, not once per hauler
- No need for grand query broker system yet—just deduplication in hot paths

**Benefits:**
- Reduces redundant queries (multiple sensors → one query)
- Improves cache locality (shared query results)
- Simple to implement (grouping logic in existing systems)

### Migration Path

1. **Add query grouping utility** (group descriptors by key)
2. **Update `PerceptionUpdateSystem`** to group sensor queries
3. **Update logistics systems** to query per site (not per hauler)
4. **Measure reduction** in query count (should see 50-80% reduction in typical scenarios)
5. **Consider full query broker** later if more systems need deduplication

---

## Priority Recommendations

### If You Do Only 3 Things Next

**1. Replace ProjectToXZ with Projection/Frame Concept**
- **Why:** Unblocks real 6DOF + planet surfaces (Space4X needs this)
- **Impact:** Enables proper support for rotated ship interiors, planetary gravity, 6DOF agents
- **Effort:** Medium (API changes + consumer updates)

**2. Make Rewind Correctness Explicit (Snapshot/Restore or Rebuild) + Add Determinism Test**
- **Why:** Current "skip rebuild in playback" is fragile and can cause inconsistent query results
- **Impact:** Ensures deterministic behavior during rewind (critical for replay/replay validation)
- **Effort:** Medium (snapshot restore logic + test)

**3. Add Macro+Micro Grids (Two Resolutions) + Share Cell Addressing with Environment Fields**
- **Why:** Future-proofs for digging, liquids, gravity singularities, and large-scale Space4X coordinates
- **Impact:** Enables efficient strategic queries, unified addressing for terrain/liquids/fields
- **Effort:** Medium-High (dual grid system + shared addressing)

**These three will make the grid "future-proof" for digging, liquids, gravity singularities, and large-scale Space4X coordinates without forcing rewrites later.**

---

## Implementation Order

### Phase 1: Foundation (Critical Path)
1. **Rewind correctness** (Option A: snapshot/restore) + determinism test
2. **Projection/Frame concept** (replace ProjectToXZ)
3. **Deterministic ordering** (always sort within cells)

### Phase 2: Scale & Resolution
4. **Frame/Sector addressing** (for large worlds)
5. **Macro+Micro grids** (two resolutions)

### Phase 3: Integration
6. **Shared cell addressing** (terrain/liquids/fields alignment)
7. **Query broker** (deduplication in hot paths)

---

## Addendum: Environment Field Stack Integration

### Overview

Address environment field services (moisture, mana, liquids, gravity, medium types, power grids) as a **separate "Environment Field Stack"** that shares the same cell addressing + frame/origin as the spatial entity index. **Don't bake them into the entity grid rebuild.**

**Key Principle:** Environment fields are **consumers** of spatial addressing, not part of the spatial grid rebuild. They share the coordinate system but maintain their own update loops, dirty regions, and snapshot policies.

---

### 1. One Coordinate Contract for Everything

**Define Once:**
- `FrameId` (planet surface, ship interior, local sector…)
- `CellCoord` quantization per layer (may differ resolution)
- Canonical mapping: `(FrameId, worldPos) → (CellCoord, localPosInCell)`

**Implementation:**
```csharp
// Shared coordinate contract (from §2: Frame/Origin Concept)
public struct SpatialFrame : IComponentData
{
    public Entity FrameEntity;
    public int3 SectorCoord;
    public float3 SectorOrigin;
    public float3 SectorExtent;
}

// Canonical mapping service
public static class SpatialCellAddressing
{
    // ✅ One canonical mapping used by all systems
    public static void QuantizeToCell(
        Entity frameId,
        float3 worldPos,
        float cellSize,
        out int3 cellCoord,
        out float3 localPosInCell)
    {
        // Get frame transform
        var frame = GetFrame(frameId);
        var localPos = worldPos - frame.SectorOrigin;
        
        // Quantize to cell
        var cellFloat = localPos / cellSize;
        cellCoord = (int3)math.floor(math.clamp(cellFloat, float3.zero, frame.SectorExtent / cellSize));
        
        // Local position within cell
        localPosInCell = localPos - (float3)cellCoord * cellSize;
    }
}
```

**Result:** Moisture/mana/liquids/gravity/medium all align automatically because they use the same `(FrameId, CellCoord)` addressing.

---

### 2. Treat Each as a FieldLayer, Not "Special Cases"

**Make a Small Interface (Conceptually):**

```csharp
// Base interface for all field layers
public interface IFieldLayer
{
    SpatialFrame Frame { get; }
    float CellSize { get; }
    int3 CellCounts { get; }
    float UpdateRateHz { get; }  // Update frequency (0 = every tick)
    
    void UpdateDirtyRegions(NativeArray<int3> dirtyCells);
    void CaptureSnapshot(uint tick, ref BlobBuilder builder);
    void RestoreSnapshot(uint tick, BlobAssetReference<FieldLayerSnapshot> snapshot);
}

// Layer types
public interface IScalarFieldLayer : IFieldLayer
{
    // Scalar fields: moisture, mana, water depth, radiation
    float Sample(float3 worldPos);
    void SetValue(int3 cellCoord, float value);
}

public interface IVectorFieldLayer : IFieldLayer
{
    // Vector fields: gravity vector, wind/current, EM drift
    float3 Sample(float3 worldPos);
    void SetValue(int3 cellCoord, float3 value);
}

public interface IMaskFieldLayer : IFieldLayer
{
    // Mask fields: medium type, sealed/blocked, jam zones
    byte Sample(int3 cellCoord);
    void SetValue(int3 cellCoord, byte value);
}

public interface IGraphFieldLayer : IFieldLayer
{
    // Graph fields: power network, ship compartments, tunnels
    // (Not cell-based, but shares frame addressing)
}
```

**Each Layer Has:**
- **Resolution** (cell size) — may differ per layer (macro for climate, micro for water)
- **UpdateRate** (Hz / ticks) — not all layers update every tick
- **BaseField** + **DynamicInfluences** — static base + dynamic modifiers (emitters, siphons, sources)
- **DirtyRegion** + **ActiveSet** — only update active/dirty regions
- **SnapshotPolicy** — how to snapshot/restore (see §4)

**Examples:**
- **MoistureGrid:** `IScalarFieldLayer` (moisture per cell, base + weather influences)
- **ManaGrid:** `IScalarFieldLayer` (mana per cell, base + emitters/siphons)
- **WaterGrid:** `IScalarFieldLayer` (water depth, active set updates)
- **GravityField:** `IVectorFieldLayer` (gravity vector, sources + base)
- **MediumMask:** `IMaskFieldLayer` (air/water/vacuum/solid, per cell)
- **PowerGraph:** `IGraphFieldLayer` (nodes + edges, not cell-based)

---

### 3. One Sampling API That AI/Physics Uses

**Expose One Hot Call:**
```csharp
public struct EnvironmentSample
{
    // Medium (from mask layer)
    public MediumType Medium;
    public float Conductivity;  // How easily signals/fluids pass
    
    // Gravity (from vector layer)
    public float3 GravityVector;
    public float GravityMagnitude;
    
    // Water (from scalar layer)
    public float WaterDepth;
    public float DepthRate;  // How fast water is rising
    
    // Environment (from scalar layers)
    public float Moisture;
    public float Mana;
    public float ManaDisruption;
    
    // Optional (if layers exist)
    public float PowerAvailability;  // From power graph scalar mask
    public float CommsNoise;         // From jam/interference scalar
}

// ✅ One sampling API
public static EnvironmentSample SampleEnvironment(Entity frameId, float3 worldPos)
{
    var sample = new EnvironmentSample();
    
    // Sample each layer (only active layers, cached if possible)
    if (HasMediumMaskLayer(frameId))
    {
        var cellCoord = QuantizeToCell(frameId, worldPos, mediumLayer.CellSize);
        sample.Medium = mediumLayer.Sample(cellCoord);
        sample.Conductivity = GetConductivity(sample.Medium);
    }
    
    if (HasGravityLayer(frameId))
    {
        sample.GravityVector = gravityLayer.Sample(worldPos);
        sample.GravityMagnitude = math.length(sample.GravityVector);
    }
    
    if (HasWaterLayer(frameId))
    {
        sample.WaterDepth = waterLayer.Sample(worldPos);
        sample.DepthRate = waterLayer.SampleRate(worldPos);  // Delta over time
    }
    
    if (HasMoistureLayer(frameId))
    {
        sample.Moisture = moistureLayer.Sample(worldPos);
    }
    
    if (HasManaLayer(frameId))
    {
        sample.Mana = manaLayer.Sample(worldPos);
        sample.ManaDisruption = manaLayer.SampleDisruption(worldPos);
    }
    
    // ... optional layers ...
    
    return sample;
}
```

**Key Points:**
- **Return only small, cheap values** (struct copy, no allocations)
- **AI never "reasons about fluid sim"** — it reacts to `Depth`/`DepthRate` + escape gradient costs
- **Caching:** `CellMetadata` cache (see §7) can pre-aggregate common queries

---

### 4. Rewind/Fast-Forward: Pick Per-Layer Snapshot Rules

**Don't Snapshot Everything Blindly. Use 3 Policies:**

#### Policy A: Derived-Only (Recomputable)

**Store:** Seeds + edit logs. Recompute on rewind.

**Good For:**
- Climate-driven moisture (computed from weather state)
- Static mana base maps (from terrain/biome data)
- Gravity fields (computed from mass sources)

**Implementation:**
```csharp
public struct DerivedFieldSnapshot
{
    public uint Seed;  // RNG seed for deterministic recompute
    public BlobAssetReference<EditLogBlob> EditLog;  // Terrain edits, etc.
}

void RestoreSnapshot(uint tick, DerivedFieldSnapshot snapshot)
{
    // Recompute from seed + edit log (deterministic)
    RecomputeFromSeedAndEdits(snapshot.Seed, snapshot.EditLog);
}
```

#### Policy B: Stateful but Sparse

**Store:** Only active chunks + influencer lists.

**Good For:**
- Liquids (waterDepth active set) — only cells with water
- Mana influenced by siphons — only cells near influences
- Jam fields — only active jamming zones

**Implementation:**
```csharp
public struct SparseFieldSnapshot
{
    public BlobAssetReference<ActiveChunkList> ActiveChunks;  // Which chunks have data
    public BlobAssetReference<InfluencerList> Influencers;    // Dynamic sources/sinks
    public BlobAssetReference<CellDataBlob> CellData;         // Actual values (sparse)
}
```

#### Policy C: Graph State

**Store:** Connectivity + node states + diffs.

**Good For:**
- Power grids (nodes + edges, not cell-based)
- Compartment networks (rooms + connections)
- Tunnel/conduit graphs

**Implementation:**
```csharp
public struct GraphFieldSnapshot
{
    public BlobAssetReference<GraphConnectivityBlob> Connectivity;
    public BlobAssetReference<NodeStateBlob> NodeStates;
    public BlobAssetReference<EditDiffBlob> Diffs;  // Changes since last snapshot
}
```

**Rule:** If a layer affects gameplay immediately (water filling a tunnel), it **must restore exactly** or deterministically reproduce from saved state.

---

### 5. Comms Mediums Are Just "Link Quality" from Fields

**Don't Simulate Comms as Agents Chatting. Compute Per Message:**

```csharp
// Link integrity function
public static float ComputeLinkIntegrity(
    Entity frameId,
    float3 senderPos,
    float3 receiverPos,
    float baseRange)
{
    var path = ComputePath(senderPos, receiverPos);
    float integrity = 1.0f;
    
    // Sample medium along path
    foreach (var samplePoint in path)
    {
        var sample = SampleEnvironment(frameId, samplePoint);
        
        // Medium affects signal (vacuum = perfect, water = attenuation, solid = blocked)
        integrity *= GetMediumAttenuation(sample.Medium);
        
        // Interference from jam scalar layer
        if (HasJamLayer(frameId))
        {
            float jamStrength = jamLayer.Sample(samplePoint);
            integrity *= (1.0f - jamStrength);
        }
    }
    
    // Occlusion from geometry/compartment graph
    if (HasCompartmentGraph(frameId))
    {
        float occlusion = ComputeOcclusion(frameId, senderPos, receiverPos);
        integrity *= (1.0f - occlusion);
    }
    
    // Distance attenuation
    float distance = math.distance(senderPos, receiverPos);
    float distanceAttenuation = math.saturate(1.0f - (distance / baseRange));
    integrity *= distanceAttenuation;
    
    return integrity;
}

// Directional comms (one-sided comms happen for free)
public static bool CanSendMessage(
    Entity senderFrame,
    float3 senderPos,
    Entity receiverFrame,
    float3 receiverPos,
    float requiredIntegrity)
{
    // Must be in same frame (or have cross-frame link)
    if (senderFrame != receiverFrame && !HasCrossFrameLink(senderFrame, receiverFrame))
    {
        return false;
    }
    
    float integrity = ComputeLinkIntegrity(senderFrame, senderPos, receiverPos, baseRange);
    return integrity >= requiredIntegrity;
}
```

**Key Points:**
- Medium type comes from the **medium mask layer**
- Interference from a **jam scalar layer**
- Occlusion from geometry/compartment graph if needed
- **Directional automatically** (one-sided comms happen for free)

---

### 6. Power Grid Shouldn't Be a "Grid" First

**Sim It as a Graph** (nodes: generators, consumers, relays; edges: cables/lines/ducts).

**Recompute Only When:**
- An edge/node changes (cable cut, generator offline)
- A generator output changes meaningfully (load balancing)
- Optionally: periodic stability checks

**Implementation:**
```csharp
// Power graph (not cell-based, but shares frame addressing)
public struct PowerGraphNode : IComponentData
{
    public Entity NodeEntity;
    public PowerNodeType Type;  // Generator, Consumer, Relay
    public float CurrentOutput;  // Positive = generating, Negative = consuming
    public float MaxCapacity;
    public float3 Position;  // For spatial queries
}

public struct PowerGraphEdge : IBufferElementData
{
    public Entity NodeA;
    public Entity NodeB;
    public float Resistance;
    public float Capacity;
    public byte IsActive;  // 0 = cut/offline
}

// Power flow solver (updates on graph changes)
public struct PowerFlowSolver
{
    void SolveFlow(ref NativeArray<PowerGraphNode> nodes, ref DynamicBuffer<PowerGraphEdge> edges)
    {
        // Solve power flow (Kirchhoff's laws, load balancing)
        // Updates node.CurrentOutput based on generators, consumers, network topology
    }
}

// Optional: Publish cheap "power density" scalar field for AI queries
public struct PowerDensityField : IScalarFieldLayer
{
    // Updated from power graph (which areas are "powered")
    void UpdateFromGraph(PowerGraph graph)
    {
        // Sample graph at cell centers, compute "power availability" scalar
        foreach (var cellCoord in GetActiveCells())
        {
            float3 worldPos = CellToWorld(cellCoord);
            float powerAvailability = QueryPowerAvailability(graph, worldPos);
            SetValue(cellCoord, powerAvailability);
        }
    }
}
```

**Result:** Power grid is graph-based (correct simulation), but AI can query "is this area powered?" via scalar field (cheap queries).

---

### 7. The Key "Polish" to Add Now

**If You Add Only One Thing: a `CellMetadata` Cache Per Resolution/Frame**

```csharp
// ✅ Compact struct with most-used fields
public struct SpatialCellMetadata : IComponentData
{
    public SpatialCellId CellId;
    
    // Most-used fields (updated only for dirty/active regions)
    public MediumType Medium;
    public float3 GravityVector;
    public float WaterDepth;
    public float Mana;
    public float Moisture;
    public byte HazardFlags;  // Flooding, radiation, etc.
    
    // Timestamp (for cache invalidation)
    public uint LastUpdateTick;
}

// Metadata cache system
public struct CellMetadataCacheSystem : ISystem
{
    void OnUpdate(ref SystemState state)
    {
        // Update metadata only for dirty/active regions
        var dirtyCells = GetDirtyCells();
        
        foreach (var cellId in dirtyCells)
        {
            var metadata = GetComponent<SpatialCellMetadata>(cellId);
            
            // Sample all active layers at cell center
            float3 worldPos = CellToWorld(cellId.CellCoord);
            var sample = SampleEnvironment(cellId.FrameId, worldPos);
            
            // Update metadata
            metadata.Medium = sample.Medium;
            metadata.GravityVector = sample.GravityVector;
            metadata.WaterDepth = sample.WaterDepth;
            metadata.Mana = sample.Mana;
            metadata.Moisture = sample.Moisture;
            metadata.HazardFlags = ComputeHazardFlags(sample);
            metadata.LastUpdateTick = currentTick;
            
            SetComponent(cellId, metadata);
        }
    }
}
```

**Benefits:**
- **Read-only for most systems** (sensors, AI query metadata directly)
- **Updated only for dirty/active regions** (cheap updates)
- **Prevents every system from poking every layer** (one aggregated cache)
- **Keeps sensors/AI fast** (single struct read vs. multiple layer samples)

---

### Practical Next Steps (Small, High Leverage)

**Implementation Order:**

1. **Implement FrameId + CellCoord Canonical Mapping**
   - Shared `SpatialCellAddressing` service (from §2)
   - All systems use same `(FrameId, CellCoord)` addressing

2. **Implement `EnvironmentSample` Aggregator with Stubs for Each Layer**
   - `EnvironmentSample` struct
   - `SampleEnvironment()` API (stubs for missing layers)
   - Consumers can start using API immediately

3. **Make 2 Layers Real First: MediumType + GravityVector**
   - **Why:** Everything depends on them (locomotion, physics, comms)
   - MediumType: `IMaskFieldLayer` (air/water/vacuum/solid)
   - GravityVector: `IVectorFieldLayer` (sources + base)

4. **Add WaterDepth Active-Set Layer** (for digging + flooding)
   - `IScalarFieldLayer` with active set updates
   - Integrates with diggable terrain (dirty region propagation)

5. **Add Mana Layer as Scalar + Influencer List** (sources/siphons)
   - `IScalarFieldLayer` with dynamic influences
   - Base field + emitter/siphon modifiers

6. **Add PowerGraph Later as Graph + Optional Scalar "Powered" Mask**
   - Graph-based power simulation (nodes + edges)
   - Optional scalar field for AI queries ("is this area powered?")

---

## Addendum: Astronomy and Climate Integration

### Overview

**Keep astronomy "analytic" (evaluated from time), and keep climate/tides as slow, coarse fields. The trap is trying to run N-body + full climate/ocean CFD. You don't need that.**

**Key Principle:** Astronomy is **computed** from time (no state to snapshot), while climate/tides are **evolved** fields (require snapshots). Astronomy drives field evolution but doesn't require expensive simulations.

---

### 1. Make an Analytic "Astronomy Kernel" (Rewind/Fast-Forward Friendly)

**Store orbits as Keplerian elements per body** (relative to a parent: moon→planet, planet→star, star→galactic center).

**At sim time t, compute position/velocity by solving Kepler's equation** and converting to Cartesian coordinates. ESA's reference equations are exactly what you want (E from E − e sin E = M, then r, then (x,y,z) via Ω, i, ω).

**Reference:** [ESA Keplerian Elements](https://kelvins.esa.int/)

**Implementation:**
```csharp
// Keplerian orbital elements
public struct KeplerianElements
{
    public float SemiMajorAxis;    // a (meters)
    public float Eccentricity;     // e (0-1)
    public float Inclination;      // i (radians)
    public float LongitudeOfAscendingNode;  // Ω (radians)
    public float ArgumentOfPeriapsis;       // ω (radians)
    public float MeanAnomalyAtEpoch;        // M₀ (radians)
    public double EpochTime;                // t₀ (seconds, reference time)
    public float MeanMotion;                // n (radians/second, derived from a and parent mass)
    public Entity ParentBody;               // Parent body (null = barycenter/star)
}

// Compute position/velocity at time t (analytic, no integration)
public static void ComputeOrbitalState(
    in KeplerianElements elements,
    double timeSeconds,
    out float3 position,
    out float3 velocity)
{
    // 1. Compute mean anomaly: M = M₀ + n(t - t₀)
    double dt = timeSeconds - elements.EpochTime;
    double meanAnomaly = elements.MeanAnomalyAtEpoch + elements.MeanMotion * dt;
    
    // 2. Solve Kepler's equation: E - e sin E = M (iterative or series)
    double eccentricAnomaly = SolveKeplersEquation(meanAnomaly, elements.Eccentricity);
    
    // 3. Compute true anomaly: ν = 2 atan2(√(1+e) sin E/2, √(1-e) cos E/2)
    double trueAnomaly = ComputeTrueAnomaly(eccentricAnomaly, elements.Eccentricity);
    
    // 4. Compute distance: r = a(1 - e cos E)
    double distance = elements.SemiMajorAxis * (1.0 - elements.Eccentricity * math.cos(eccentricAnomaly));
    
    // 5. Convert to Cartesian: (x, y, z) via orbital plane rotation (Ω, i, ω)
    // See: https://kelvins.esa.int/keplerian-elements-calculator/
    position = ConvertToCartesian(distance, trueAnomaly, elements);
    velocity = ComputeVelocity(eccentricAnomaly, distance, elements);
}

// Per-body state (computed from elements + time)
public struct AstronomyBodyState : IComponentData
{
    public Entity BodyEntity;
    public float3 Position;        // World-space position (computed)
    public float3 Velocity;        // World-space velocity (computed)
    public float3 RotationAxis;    // Rotation axis (for day/night)
    public float RotationAngle;    // Current rotation angle (computed from time)
    public float RotationPeriod;   // Sidereal rotation period (seconds)
    public float AxialTilt;        // Tilt angle (radians, for seasons)
    public Entity ParentBody;      // Parent body reference
}
```

**Why This Is Perfect for Rewind:**
- You don't "integrate" the orbit state; you just **evaluate it from t**
- Rewinding to tick T is **deterministic and O(#bodies)**, not O(T)
- No state to snapshot (just store elements, compute from time)

**Implementation Note:** Compute in double precision, store results in float (or `(FrameId, LocalPos)` if you use large-world frames).

---

### 2. Rotation, Day/Night, and Seasons

#### Solar Day Length (Time-of-Day)

A planet's "solar day" differs from its sidereal rotation because the orbit advances while it spins. A useful relation is:

**1/P_solar = 1/P_rot - 1/P_orb** (for prograde rotation)

**Reference:** [Solar Day Calculation](https://homepage.physics.uiowa.edu/)

**This also covers tidally locked worlds:** If P_rot ≈ P_orb, the solar day becomes extremely long.

**Implementation:**
```csharp
public struct PlanetRotationState : IComponentData
{
    public float SiderealRotationPeriod;  // P_rot (seconds)
    public float OrbitalPeriod;           // P_orb (seconds)
    public float SolarDayPeriod;          // P_solar (computed: 1/(1/P_rot - 1/P_orb))
    public float CurrentSolarTime;        // 0-1 (solar day phase)
    public float3 RotationAxis;           // Rotation axis (for axial tilt)
    public float AxialTilt;               // Tilt angle (radians)
}

// Compute solar day period
float ComputeSolarDayPeriod(float siderealPeriod, float orbitalPeriod)
{
    if (math.abs(siderealPeriod - orbitalPeriod) < 0.01f)
    {
        // Tidally locked (P_rot ≈ P_orb) → solar day → infinity (always same face)
        return float.MaxValue;
    }
    
    // 1/P_solar = 1/P_rot - 1/P_orb
    float invSolarDay = (1.0f / siderealPeriod) - (1.0f / orbitalPeriod);
    return 1.0f / invSolarDay;
}

// Compute solar time phase (0 = midnight, 0.5 = noon)
float ComputeSolarTimePhase(double timeSeconds, float solarDayPeriod)
{
    return (float)((timeSeconds % solarDayPeriod) / solarDayPeriod);
}
```

#### Seasons from Axial Tilt

Axial tilt changes daylight duration and insolation by latitude across the year, producing seasons.

**Reference:** [Seasons and Axial Tilt](https://www.climate.gov/)

**Keep This Lightweight by Deriving Just:**
- `sunDir` in planet-local frame
- `declination` (season angle)
- `dayLength(lat)` (optional, if you want)

**Implementation:**
```csharp
public struct PlanetSeasonState : IComponentData
{
    public float AxialTilt;           // Tilt angle (radians, 0 = no tilt, ~23.4° for Earth)
    public float3 SunDirectionLocal;  // Sun direction in planet-local frame (computed)
    public float Declination;         // Sun declination angle (season angle, -tilt to +tilt)
    public float YearPhase;           // 0-1 (0 = equinox, 0.25 = solstice, etc.)
}

// Compute sun direction and declination from axial tilt + orbital position
void ComputeSeasonalSunDirection(
    float axialTilt,
    float yearPhase,  // 0-1 (where in orbit: 0 = equinox, 0.25 = solstice)
    out float3 sunDirectionLocal,
    out float declination)
{
    // Declination varies sinusoidally: δ = tilt * sin(2π * yearPhase - π/2)
    declination = axialTilt * math.sin(2.0f * math.PI * yearPhase - math.PI / 2.0f);
    
    // Sun direction in planet-local frame (assumes sun is "north" at equinox)
    float sunLatitude = declination;
    float sunLongitude = 0.0f;  // Simplified (assumes noon at longitude 0)
    sunDirectionLocal = math.normalize(new float3(
        math.cos(sunLatitude) * math.cos(sunLongitude),
        math.sin(sunLatitude),
        math.cos(sunLatitude) * math.sin(sunLongitude)));
}

// Optional: Compute day length at latitude
float ComputeDayLength(float latitude, float declination)
{
    // Day length = 24 * (1/π) * arccos(-tan(lat) * tan(δ))
    float hourAngle = math.acos(-math.tan(latitude) * math.tan(declination));
    float dayLengthHours = 24.0f * (hourAngle / math.PI);
    return math.clamp(dayLengthHours, 0.0f, 24.0f);
}
```

---

### 3. Climate Coupling (Lightweight, Believable)

**The main driver you need is stellar flux at the planet**, which follows inverse-square:

**F = L / (4πr²)**

where L is star luminosity and r is star–planet distance.

**Reference:** [Stellar Flux](https://astronomy.osu.edu/)

**Then Pick a Climate Model Tier:**

#### Tier 0 (MVP): Per-Planet "Box" Climate

One temperature/humidity value relaxing toward equilibrium from F + albedo.

**Implementation:**
```csharp
public struct PlanetClimateState : IComponentData
{
    public float CurrentTemperature;      // K (current)
    public float TargetTemperature;       // K (equilibrium from flux + albedo)
    public float CurrentHumidity;         // 0-1 (current)
    public float TargetHumidity;          // 0-1 (equilibrium)
    public float RelaxationRate;          // How fast it approaches target (1/s)
}

// Update climate (called at slow cadence, e.g., once per 100 sim ticks)
void UpdateClimate(ref PlanetClimateState climate, float stellarFlux, float albedo, float deltaTime)
{
    // Compute target temperature from flux + albedo (simplified energy balance)
    float absorbedFlux = stellarFlux * (1.0f - albedo);
    climate.TargetTemperature = ComputeEquilibriumTemperature(absorbedFlux);
    
    // Relax toward target
    float tempDelta = climate.TargetTemperature - climate.CurrentTemperature;
    climate.CurrentTemperature += tempDelta * climate.RelaxationRate * deltaTime;
    
    // Humidity also relaxes (simplified)
    climate.CurrentHumidity = math.lerp(climate.CurrentHumidity, climate.TargetHumidity, climate.RelaxationRate * deltaTime);
}
```

#### Tier 1: Latitudinal Bands

10–32 bands, relax temperatures per band using the seasonal sun angle.

**This is basically the spirit of classical energy balance models (Budyko/Sellers style)** — simple, low-dimensional climate that relaxes over time rather than simulating full fluid circulation.

**Reference:** [Energy Balance Models](https://link.springer.com/)

**Implementation:**
```csharp
public struct LatitudinalClimateBand
{
    public float LatitudeMin;
    public float LatitudeMax;
    public float CurrentTemperature;
    public float TargetTemperature;
    public float Albedo;  // Surface albedo (ice = high, ocean = low)
}

public struct PlanetLatitudinalClimate : IComponentData
{
    public BlobAssetReference<LatitudinalBandsBlob> Bands;  // 10-32 bands
    public float RelaxationRate;
}

// Update per-band climate using seasonal sun angle
void UpdateLatitudinalClimate(
    ref PlanetLatitudinalClimate climate,
    float declination,
    float stellarFlux,
    float deltaTime)
{
    var bands = climate.Bands.Value.Bands;
    
    for (int i = 0; i < bands.Length; i++)
    {
        float bandLat = (bands[i].LatitudeMin + bands[i].LatitudeMax) * 0.5f;
        
        // Compute insolation at this latitude (depends on declination)
        float insolation = ComputeInsolation(bandLat, declination, stellarFlux);
        
        // Compute target temperature from insolation + albedo
        float absorbedFlux = insolation * (1.0f - bands[i].Albedo);
        bands[i].TargetTemperature = ComputeEquilibriumTemperature(absorbedFlux);
        
        // Relax toward target
        float tempDelta = bands[i].TargetTemperature - bands[i].CurrentTemperature;
        bands[i].CurrentTemperature += tempDelta * climate.RelaxationRate * deltaTime;
    }
}
```

**Run climate at a slow cadence** (e.g., once per N sim ticks) and snapshot only the small climate state for rewind.

---

### 4. Tides Coupling (Coarse Is Enough)

**For "tides depend on proximity/mass", you don't need ocean sim. You just need a tide forcing scalar:**

**Tidal forcing scales like ∝ M/r³** (strong distance sensitivity).

**Reference:** [Tidal Forces](https://eng.libretexts.org/)

**Use that to drive:**
- A periodic water-level offset at coasts (heightfield water system)
- Optional "tidal current bias" vector near shorelines

**This plugs into your liquid plan as a boundary condition, not a full solver.**

**Implementation:**
```csharp
public struct TideForcingState : IComponentData
{
    public float BaseTideLevel;        // Mean sea level offset (meters)
    public float TidalAmplitude;       // Max tide height (meters)
    public float TidalPhase;           // 0-1 (tidal cycle phase)
    public float TidalPeriod;          // Tidal period (seconds, typically ~12-24 hours)
}

// Compute tide forcing from moon/star proximity and mass
float ComputeTideAmplitude(Entity planet, Entity moon, float planetMass, float moonMass, float distance)
{
    // Tidal force ∝ M / r³
    float tidalAcceleration = (moonMass / (distance * distance * distance));
    
    // Scale by planet radius (how much the planet deforms)
    float planetRadius = GetPlanetRadius(planet);
    float tideHeight = tidalAcceleration * planetRadius * TidalConstant;
    
    return tideHeight;
}

// Compute current tide level (periodic)
float ComputeCurrentTideLevel(float baseLevel, float amplitude, float phase)
{
    // Simple sinusoidal tide: level = base + amplitude * sin(2π * phase)
    return baseLevel + amplitude * math.sin(2.0f * math.PI * phase);
}

// Apply to water system as boundary condition
void ApplyTideToWaterSystem(
    ref WaterGridChunk chunk,
    float tideLevel,
    float3 coastPosition)
{
    // Add tide level offset to water depth at coast cells
    // This is a boundary condition, not a full solver update
    int2 coastCell = WorldToCell(coastPosition);
    var cell = chunk.Cells[coastCell];
    cell.WaterDepth = math.max(cell.WaterDepth, tideLevel);  // Tide raises minimum water level
    chunk.Cells[coastCell] = cell;
}
```

---

### 5. Stars Orbiting a Galactic Center

**Also fine — just treat it as another parent orbit, usually very slow unless you accelerate time a lot.**

**For reference, NASA notes the Solar System takes ~230 million years per galactic orbit.**

**Reference:** [Galactic Orbit](https://science.nasa.gov/)

**So in normal timescales it's "background motion"; in your fast-forward, it can become visible.**

**Implementation:**
```csharp
// Star system can orbit galactic center (just another parent orbit)
public struct StarSystemOrbit : IComponentData
{
    public KeplerianElements GalacticOrbit;  // Orbit around galactic center
    public float3 GalacticPosition;          // Current position (computed from time)
    public float GalacticOrbitPeriod;        // ~230 million years for Solar System
}

// Compute galactic position (same analytic method as planetary orbits)
void UpdateGalacticPosition(ref StarSystemOrbit orbit, double timeSeconds)
{
    ComputeOrbitalState(orbit.GalacticOrbit, timeSeconds, out var pos, out var vel);
    orbit.GalacticPosition = pos;
}
```

---

### 6. Integration Points with ECS Grid + Fields

**Do not bake any of this into the entity spatial grid. Publish compact state and let field systems sample it.**

**Recommended Components/Singletons Per FrameId:**

```csharp
// Astronomy body state (computed from elements + time)
public struct AstronomyBodyState : IComponentData
{
    public Entity BodyEntity;
    public float3 Position;        // World-space position (computed)
    public float3 Velocity;        // World-space velocity (computed)
    public float3 RotationAxis;    // Rotation axis
    public float RotationAngle;    // Current rotation angle
    public float RotationPeriod;   // Sidereal rotation period
    public float AxialTilt;        // Tilt angle
    public Entity ParentBody;
}

// Star state (luminosity, spectral class)
public struct StarState : IComponentData
{
    public Entity StarEntity;
    public float Luminosity;       // Watts (absolute)
    public SpectralClass SpectralClass;  // O, B, A, F, G, K, M (for color/temperature)
    public float SurfaceTemperature;     // K (for blackbody radiation)
}

// Planet-derived state (computed from astronomy + time)
public struct PlanetStateDerived : IComponentData
{
    public Entity PlanetEntity;
    public float3 SunDirectionLocal;    // Sun direction in planet-local frame
    public float StarDistance;          // Distance to star (meters)
    public float StellarFlux;           // F = L / (4πr²) (W/m²)
    public float Declination;           // Sun declination (season angle)
    public float SolarTimePhase;        // 0-1 (solar day phase: 0 = midnight)
    public float TideScalar;            // Tide height offset (meters)
    public float YearPhase;             // 0-1 (orbital phase: 0 = equinox)
}
```

**Then Your Environment Sampling Returns:**

```csharp
// Extend EnvironmentSample with astronomy-derived values
public struct EnvironmentSample
{
    // ... existing fields (medium, gravity, water, mana, etc.) ...
    
    // ✅ Astronomy-derived values
    public float Irradiance;          // From flux * zenith angle
    public float DayNightFactor;      // 0-1 (0 = midnight, 1 = noon)
    public float SeasonFactor;        // 0-1 (seasonal variation)
    public float TideLevelOffset;     // Tide height offset (meters)
}

// Sample astronomy-derived environment values
void SampleAstronomyEnvironment(
    Entity frameId,
    float3 worldPos,
    ref EnvironmentSample sample)
{
    // Get planet state for this frame
    var planetState = GetComponent<PlanetStateDerived>(frameId);
    
    // Compute irradiance (flux * cosine of zenith angle)
    float3 localPos = WorldToLocal(frameId, worldPos);
    float latitude = ComputeLatitude(localPos);
    float sunZenithAngle = ComputeSunZenithAngle(latitude, planetState.Declination);
    sample.Irradiance = planetState.StellarFlux * math.max(0.0f, math.cos(sunZenithAngle));
    
    // Day/night factor (0 = midnight, 1 = noon)
    float solarTime = planetState.SolarTimePhase;
    sample.DayNightFactor = 0.5f + 0.5f * math.cos(2.0f * math.PI * (solarTime - 0.5f));
    
    // Season factor (0 = winter solstice, 1 = summer solstice)
    sample.SeasonFactor = 0.5f + 0.5f * math.sin(2.0f * math.PI * planetState.YearPhase);
    
    // Tide level offset
    sample.TideLevelOffset = planetState.TideScalar;
}
```

**And your moisture/mana/liquid layers can evolve using those as slow drivers:**

```csharp
// Moisture layer evolves using irradiance + season as drivers
void UpdateMoistureLayer(
    ref MoistureGrid moisture,
    float irradiance,
    float seasonFactor,
    float deltaTime)
{
    // Evaporation increases with irradiance
    float evaporationRate = BaseEvaporationRate * irradiance * seasonFactor;
    
    // Update moisture cells using evaporation rate
    // (simplified: moisture decreases with evaporation, increases with precipitation)
}
```

**Rewind Rule:** Orbits/rotation are recomputed from t; only the slow, stateful fields (water volumes, climate temperatures, moisture) need snapshots or deterministic re-sim.

---

### Recommended Scope Ladder (So It Stays Sane)

**1. Kepler Orbit + Rotation + Solar Time** (analytic, free rewind)
- **Reference:** [ESA Keplerian Elements](https://kelvins.esa.int/)
- Compute position/velocity from time (no integration, no state)
- Compute solar day period from rotation + orbital period
- Compute solar time phase

**2. Flux + Day/Night + Seasons** (drives biomes)
- **Reference:** [Seasons and Axial Tilt](https://www.climate.gov/)
- Compute stellar flux (inverse-square law)
- Compute sun direction + declination (from axial tilt + orbital phase)
- Compute day/night factor and season factor

**3. Tide Scalar** (drives coastal water level)
- **Reference:** [Tidal Forces](https://eng.libretexts.org/)
- Compute tide amplitude (M/r³)
- Apply as periodic water-level offset (boundary condition)

**4. Climate EBM** (slow relax, not CFD)
- **Reference:** [Energy Balance Models](https://link.springer.com/)
- Per-planet box model (MVP) or latitudinal bands (Tier 1)
- Relax toward equilibrium from flux + albedo
- Slow cadence updates (once per N sim ticks)

**5. Galactic Star Orbits** (mostly presentation / very slow gameplay driver)
- **Reference:** [Galactic Orbit](https://science.nasa.gov/)
- Treat as parent orbit (same analytic method)
- Usually negligible unless fast-forwarding time significantly

---

## Related Documentation

- **Spatial Grid Summary:** `Docs/Concepts/Core/Spatial_Grid_System_Summary.md` - Current state analysis
- **Spatial Partitioning Design:** `Documentation/DesignNotes/SpatialPartitioning.md` - Design specification
- **Spatial Services TODO:** `Docs/TODO/SpatialServices_TODO.md` - Active tasks
- **Liquid-Terrain Integration:** `Docs/Concepts/Core/Liquid_Terrain_Integration.md` - Terrain/liquid alignment
- **Mana Grid System:** `godgame/Docs/Concepts/Magic/Mana_Grid_System.md` - Mana field implementation
- **Fluid Dynamics System:** `Docs/Concepts/Core/Fluid_Dynamics_System.md` - Water layer implementation
- **Simulation LOD:** `Docs/Concepts/Core/Simulation_LOD_And_Environment_Fields.md` - LOD framework

---

**For Implementers:** Focus on Phase 1 items (rewind, projection, ordering) for immediate correctness and future-proofing  
**For Architects:** Review Phase 2/3 items (frames, multi-resolution, shared addressing) for scalability roadmap  
**For Designers:** Consider projection modes and frame concepts when designing 6DOF gameplay  
**For Field System Implementers:** Use Environment Field Stack pattern (shared addressing, per-layer policies, unified sampling API)  
**For Astronomy/Climate Implementers:** Start with analytic astronomy kernel, add climate/tides as slow-evolving fields, avoid N-body and full CFD simulations

