# Spatial Services & Grid TODO

> **Generalisation Guideline**: Spatial indexing and queries must remain data-driven and agnostic to game content (villagers, ships, drones, etc.). Prefer shared descriptors/config blobs rather than entity-type-specific systems.

## Cross-References

| Contract | Location |
|----------|----------|
| Runtime Lifecycle | `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` |
| System Integration | `Docs/TODO/SystemIntegration_TODO.md` |
| Registry Queries | `Runtime/Runtime/Registry/RegistryQueryHelpers.cs` |
| Terrain Version | `Runtime/Runtime/Components/TerrainComponents.cs` |
| Environment Grids | `Runtime/Runtime/Environment/EnvironmentGrids.cs` |

## Goal
- Ship a modern, configurable spatial-partition service that stays deterministic and scales from today’s 100k target to **1M+ entities** without re-architecture.
- Provide Burst-friendly query jobs (`FindWithinRadius`, `kNN`, `OverlapAABB`, etc.) for villagers, miracles, AI steering, combat, and debugging.
- Keep configuration data-driven (ScriptableObject + blob) so designers can choose layouts per scene/biome.

## Plain-Language Primer
- Think of the spatial grid as a **map index**: it tells us “which entities live near X” so we don’t scan everything.
- We’ll maintain that index in DOTS-friendly data structures (arrays, hashes) updated each frame or on demand.
- Consumers ask the service instead of iterating the world; the service returns a compact list that jobs can process fast.

## Alignment With Vision
- **Scalability discipline**: ensures 1M entity budgets stay achievable (Vision.md).
- **Flexibility by configuration**: swap grid sizes/providers without rewriting systems.
- **Deterministic core**: rebuilds and query outputs stay stable under rewind/catch-up.
- **Observability**: exposes tooling hooks for debugging spatial coverage.
- **Truth-source reference**: keep contracts in sync with `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` and integration guidance in `Docs/TODO/SystemIntegration_TODO.md`.

## Architecture Outline
1. **Providers**  
   - Start with a hashed, cell-based grid (Morton/Z-curve indexing) for 2.5D worlds.  
   - Plan for upgrades: hierarchical grid (macro/micro cells), GPU-friendly BVH/quadtree.  
   - Provider pattern lets us swap implementations via config.
2. **Data Layout**  
   - Blob asset stores cell sizing, bounds, hashing seeds, level count.  
   - Runtime keeps SoA arrays: `NativeArray<CellRange>`, `NativeArray<Entity>`, `NativeArray<float3>` (position cache).  
   - Optional `NativeMultiHashMap` for dynamic densities.
3. **Update Pipeline**  
   - Rebuild in deterministic order each frame (or when “dirty”) using Burst jobs.  
   - Support partial updates (dirty lists) but fall back to full rebuild when rewinding.  
   - Cache last-known position for cheap dirty detection (`math.distance` vs. cell threshold).  
   - Use double-buffering so readers always see stable data during write.
4. **Query API**  
   - `GetCellEntities(int cellId)`  
   - `ForEachNeighborCells(float3 position, float radius, ref FunctionRef)`  
   - `FindClosest(float3 position, PredicateRef)`  
   - Provide `IJob` wrappers + inlineable utility methods.  
   - Always return results sorted deterministically (by entity index).
5. **Determinism & Rewind**  
   - Rebuild from authoritative components when entering playback/catch-up.  
   - Record config seeds in history; no randomization during rebuild.  
   - Ensure results don’t depend on thread scheduling (use consistent key sort).

## Workstreams & Tasks

- ✅ **Spatial services recon (2025-10-28)**: `VillagerJobSystems` and `VillagerTargetingSystem` already use the grid for candidate selection; `AISystems` runs k-NN batches; miracles/logistics still rely on EntityManager lookups. Rewind mode skips spatial rebuilds (Record-only), leaving cached buffers for playback. Further spatialisation needed for vegetation, miracles, combat, logistics. Hot paths are villager assignment/delivery and AI sensors; workarounds fall back to registry scans when spatial data absent. (See `Docs/DesignNotes/SpatialPartitioning.md` for full summary.)

### 1. Design & Data Spec
- [x] Draft a design note capturing provider interface, data types (`CellRange`, `GridConfigBlob`), and integration points. *(See `Docs/DesignNotes/SpatialPartitioning.md`)*
- [x] Define config ScriptableObject (`SpatialPartitionProfile`) with cell size, world bounds, provider selection.
- [x] Document expected consumers and query patterns (villager jobs, miracles, AI search, logistics) based on reconnaissance findings.

### 2. Core Implementation (Partial Rebuild Slice)
- [x] **Create component definitions** in `Assets/Scripts/PureDOTS/Runtime/Spatial/SpatialComponents.cs`:
  - `SpatialGridConfig` (singleton with cell size, world bounds, hash seed)
  - `SpatialGridState` (singleton with entity/position buffers, version counter)
  - `SpatialGridCell` (buffer element for entity-to-cell mappings)
  - `SpatialIndexedTag` (tag for entities participating in spatial indexing)
- [x] **Implement utility structs** in `Assets/Scripts/PureDOTS/Runtime/Spatial/SpatialUtilities.cs`:
  - `SpatialHash` (Morton/Z-curve encoding for 3D -> cell ID)
  - `GridCellKey` (int3 cell coordinates with deterministic equality/hash)
  - `SpatialQueryHelper` (static methods for radius/AABB queries)
- [x] **Build grid maintenance system** in `Assets/Scripts/PureDOTS/Systems/Spatial/SpatialGridBuildSystem.cs`:
  - Rebuild grid from `LocalTransform` + `SpatialIndexedTag` each tick
  - Use change filters to minimize churn in record mode
  - Force full rebuild when entering record mode from playback/catch-up
  - Update within `VillagerSystemGroup` or dedicated `SpatialSystemGroup` before consumers run
- [x] Introduce dirty tracking & partial rebuild path (SpatialGridBuildSystem + optional `SpatialGridDirtyTrackingSystem`).
- [x] Track rebuild statistics (dirty count, rebuild duration) and expose via `SpatialConsoleInstrumentation`/debug HUD.
- [x] Introduce runtime provider abstraction (`ISpatialGridProvider`) with hashed-grid implementation and config validation hooks.
- [x] Expand deterministic query utilities: `kNN`, multi-radius batches, filtered entity iterators, and jobified wrappers. (Added `FindKNearestInRadius<TFilter>`, `BatchRadiusQueries`, existing `SpatialKNearestBatchJob` provides jobified wrappers)
- [x] Provide data-driven query descriptors so different game concepts can reuse the same query pipeline without custom code. (`SpatialQueryDescriptor` with `SpatialQueryOptions` and filter interface already provides reusable pipeline)
- [x] Integrate registries (resource, storehouse, villager, logistics, miracles) with spatial indexing metadata for fast lookup once entries store spatial tokens. (Resource/storehouse/villager registries consume `SpatialGridResidency`; logistics and miracle registries now read `SpatialGridResidency` when present and fall back to hashed positions, so entries always emit `CellId`/`SpatialVersion` before continuity tests run; divine hand system uses spatial queries for `FindPickable`.)
- [x] **Respect rewind state**: Check `RewindState.Mode` to skip/rebuild appropriately; add `PlaybackGuardTag` checks if needed. (Implemented: `SpatialGridBuildSystem` skips rebuilds during playback; `SpatialRewindGuardSystem` guards group execution per RewindPatterns.md)
- [ ] Support 2D (XZ plane) navigation out of the box **and** define config/runtime hooks for true 3D layers (int3 cells, volume cost fields) so PureDOTS pathfinding can service flying/underground agents without game-layer rewrites.

### 3. Future-Proofing (Roadmap Hooks)
- [ ] Stub hierarchical grid interface (two-level: macro cell -> micro grid).  
- [ ] Plan GPU offload hook (compute shader or Entities Graphics) for extreme densities.  
- [ ] Reserve data slots for additional attributes (cell occupancy heatmaps, average normals).
- [ ] Formalise layer provider abstraction (`INavLayerProvider`) so 2D/3D navigation layers can swap implementations without touching consumer systems.

### 4. Query Helpers & Integration
- [x] **Provide query API** in `SpatialQueryHelper`:
  - [x] `GetEntitiesWithinRadius(float3 position, float radius, ref NativeList<Entity> results)` (Implemented as public wrapper)
  - [x] `FindNearestEntity(float3 position, EntityQuery filter, out Entity nearest, out float distance)` (Implemented; note: EntityQuery filtering must be done externally)
  - [x] `GetCellEntities(int3 cellCoords, ref NativeList<Entity> results)` (Implemented as public wrapper)
  - [x] `OverlapAABB(float3 min, float3 max, ref NativeList<Entity> results)` (Implemented as public wrapper)
- [x] **Integrate into villager systems**:
  - [x] `VillagerTargetingSystem`: Uses registry entries with spatial cell IDs for efficient lookups (registry already contains spatial metadata)
  - [x] `VillagerAISystem`: Sensor queries available via `AISensorUpdateSystem` and `SpatialSensorUpdateSystem` using spatial grid
  - [x] `VillagerJobSystems` (assignment & delivery phases): Spatial queries drive resource/storehouse selection
- [x] **Integrate into resource systems**:
  - [x] `ResourceRegistrySystem`: Caches spatial cell per resource via `SpatialGridResidency` component (registry entries include `CellId` and `SpatialVersion`)
  - [x] `VillagerJobDeliverySystem`: Find nearest storehouse using spatial query
- [ ] **Integrate miner/hauler logistics**: `VesselRoutingSystem`, freighter/wagon dispatch leverage spatial queries + registries.
- [x] **Integrate into divine hand/miracles**:
  - [x] `DivineHandSystems`: Use grid for hover highlighting and target selection (`FindPickable` now uses spatial queries with fallback to full scan)
  - [ ] `RainMiracleSystems`: Query terrain/vegetation density before spawning effects (pending miracle system implementation)
- [ ] **Expose generic query helpers** for any future game-specific systems (VR input, RTS selection, etc.) using shared descriptors.
- [ ] **Preserve existing behaviour**: Ensure all integrations produce identical results (same entity selections) to validate correctness before optimizing.

### 5. Tooling & Observability
- [x] Debug authoring: scene gizmo drawer showing grid bounds/cell occupancy. (Enhanced `SpatialPartitionAuthoring.OnDrawGizmosSelected` to show bounds, grid lines for 2D navigation, and cell structure)
- [x] Runtime overlay (Entities Graphics or UI) toggled via debug menu. (`DebugDisplaySystem` exposes spatial grid stats; `SpatialInstrumentationSystem` provides console logging hooks)
- [x] Logging hooks to sample cell density, worst-case query cost. (`SpatialConsoleInstrumentation` component enables throttled logging; `SpatialInstrumentationSystem` logs cell count, entries, occupancy, rebuild time, dirty counts)
- [x] Editor validation to warn when config bounds too small/large. (Enhanced `ValidateSpatialPartitionProfile` to check bounds, cell counts, aspect ratios, and 2D lock consistency)

### 6. Testing & Benchmarks
- [ ] **Unit tests** in `Assets/Scripts/Tests/SpatialGridTests.cs`:
  - Verify Morton/Z-curve hash produces identical results for same inputs (determinism)
  - Test cell assignment for entities at grid boundaries
  - Validate radius query accuracy (no false negatives/positives within epsilon)
  - Ensure query results are sorted deterministically by entity index
  - [ ] Integrate with `EnvironmentSystemGroup` cadence (consume shared environment grid updates once baseline jobs are in place).
  - [ ] Verify `SpatialGrid` rebuild honours `TerrainVersion` increments broadcast by terraforming hooks.
- [ ] **Rewind determinism tests**:
  - Record 100 ticks → rewind to tick 50 → verify grid state matches original tick 50
  - Test playback mode: ensure grid queries during playback return consistent results
  - Test catch-up mode: verify grid rebuilds correctly as simulation fast-forwards
  - Add `ITimeAware` implementation if grid needs explicit snapshot/restore logic
- [ ] **Performance benchmarks**:
  - Playmode soak with 100k → 1M synthetic entities measuring rebuild time per tick
  - Measure query time for varying radii (1m, 10m, 50m, 100m) at different entity densities
  - Profile memory footprint and verify zero GC allocations per frame
  - Compare performance vs. linear scans to quantify speedup
- [x] **Profiling automation**: Integrate with performance harness TODO to track spatial grid metrics in CI. *(`SpatialRegistryPerformanceTests` now drives `SpatialInstrumentationSystem_EmitsMetricsMatchingGridState` to validate logged cells/entries/dirty ratios before CI widens registry adoption.)*

### 7. Documentation & Adoption
- [x] Update `Docs/Guides/SceneSetup.md` with instructions for adding a spatial profile to new scenes.  
- [x] Add `Docs/DesignNotes/SpatialPartitioning.md` summarizing design decisions and provider roadmap.  
- [x] Reflect progress in `Docs/Progress.md` and cross-link to registries/TODOs.

## Open Questions
- How many provider variants do we need at launch? (Uniform grid vs. hashed vs. quadtree.)
- Should we align cells with terrain tiles to aid streaming?  
- Do we need persistent cell occupancy history for analytics/gameplay?  
- How to handle very tall objects (creatures, mountains) in 2.5D grid?  
- What thresholds trigger partial rebuild vs. full rebuild?

## Dependencies & Links
- Relies on registry rewrite for efficient consumer access patterns.  
- Will later integrate with presentation bridges for spatial debug overlays.  
- Works alongside performance harness to validate high-count simulations.

## Next Steps & Implementation Order
1. **Phase 0**: Complete reconnaissance (audit existing systems, map rewind touchpoints, catalog entities).
2. **Design Lock**: Draft design note and finalize component/API surface based on recon findings.
3. **Minimal Prototype**: Implement single-threaded hashed grid with basic rebuild and radius query; validate determinism and benchmark with 100k entities in test scene.
4. **Burst Optimization**: Convert rebuild and query logic to Burst-compiled jobs; verify performance scales to 500k+ entities.
5. **First Integration**: Migrate `VillagerTargetingSystem` or `ResourceRegistrySystem` as pilot consumer; validate behaviour parity.
6. **Remaining Integrations**: Update all consumer systems documented in recon phase.
7. **Tooling & Testing**: Add debug visualization, write comprehensive tests, integrate profiling harness.
8. **Documentation**: Update guides and design notes; mark complete in `Progress.md`.

Keep this file updated as each milestone completes to track progress and surface blockers.
