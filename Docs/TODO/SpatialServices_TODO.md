# Spatial Services & Grid TODO

> **Generalisation Guideline**: Spatial indexing and queries must remain data-driven and agnostic to game content (villagers, ships, drones, etc.). Prefer shared descriptors/config blobs rather than entity-type-specific systems.

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

### 0. Requirements Reconnaissance (Pre-Design)
- [ ] **Audit existing spatial queries**: Scan `VillagerTargetingSystem`, `VillagerAISystem`, `ResourceRegistrySystem`, `StorehouseRegistrySystem` to document current proximity/neighbor search patterns and bottlenecks.
- [ ] **Map rewind touchpoints**: Review `RewindCoordinatorSystem`, `RewindRoutingSystems`, and `ITimeAware` consumers to determine when/how the spatial grid must update or snapshot during record/playback/catch-up transitions.
- [ ] **Catalog entity archetypes**: List which entity types need spatial indexing (villagers, resources, storehouses, vegetation, miracles, combat units) and estimate counts per archetype.
- [ ] **Identify query hotspots**: Profile or estimate which systems run the most spatial queries per frame to prioritize optimization targets.
- [ ] **Document current workarounds**: Note any existing hacks (linear scans, manual distance checks, registry-based filtering) that the grid will replace.

### 1. Design & Data Spec
- [ ] Draft a design note capturing provider interface, data types (`CellRange`, `GridConfigBlob`), and integration points.
- [ ] Define config ScriptableObject (`SpatialPartitionProfile`) with cell size, world bounds, provider selection.
- [ ] Document expected consumers and query patterns (villager jobs, miracles, AI search) based on reconnaissance findings.

### 2. Core Implementation
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
- [ ] Finalise Burst job implementation for rebuild & dirty detection (ensure double-buffer swap safe under rewind).
- [ ] Expand deterministic query utilities: `kNN`, multi-radius batches, filtered entity iterators, and jobified wrappers.
- [ ] Provide data-driven query descriptors so different game concepts can reuse the same query pipeline without custom code.
- [ ] Integrate registries (villager, miner vessel, hauler freighter, wagon, resource, miracle neutral) with spatial indexing metadata for fast lookup.
- [ ] **Respect rewind state**: Check `RewindState.Mode` to skip/rebuild appropriately; add `PlaybackGuardTag` checks if needed.
- [ ] Support 2D (XZ plane) initially; stub pseudo-3D (Y strata) hooks for future miracles/flying entities.

### 3. Future-Proofing (Roadmap Hooks)
- [ ] Stub hierarchical grid interface (two-level: macro cell -> micro grid).  
- [ ] Plan GPU offload hook (compute shader or Entities Graphics) for extreme densities.  
- [ ] Reserve data slots for additional attributes (cell occupancy heatmaps, average normals).

### 4. Query Helpers & Integration
- [ ] **Provide query API** in `SpatialQueryHelper`:
  - `GetEntitiesWithinRadius(float3 position, float radius, ref NativeList<Entity> results)`
  - `FindNearestEntity(float3 position, EntityQuery filter, out Entity nearest, out float distance)`
  - `GetCellEntities(int3 cellCoords, ref NativeList<Entity> results)`
  - `OverlapAABB(float3 min, float3 max, ref NativeList<Entity> results)`
- [ ] **Integrate into villager systems**:
  - `VillagerTargetingSystem`: Use spatial grid to find nearest resource/storehouse instead of `ComponentLookup` linear iteration
  - `VillagerAISystem`: Query nearby threats, food sources for sensor updates
  - [x] `VillagerJobSystems` (assignment & delivery phases): Spatial queries drive resource/storehouse selection
- [ ] **Integrate into resource systems**:
  - `ResourceRegistrySystem`: Optionally cache spatial cell per resource for faster lookups
  - [x] `VillagerJobDeliverySystem`: Find nearest storehouse using spatial query
- [ ] **Integrate miner/hauler logistics**: `VesselRoutingSystem`, freighter/wagon dispatch leverage spatial queries + registries.
- [ ] **Integrate into divine hand/miracles**:
  - `DivineHandSystems`: Use grid for hover highlighting and target selection
  - `RainMiracleSystems`: Query terrain/vegetation density before spawning effects
- [ ] **Expose generic query helpers** for any future game-specific systems (VR input, RTS selection, etc.) using shared descriptors.
- [ ] **Preserve existing behaviour**: Ensure all integrations produce identical results (same entity selections) to validate correctness before optimizing.

### 5. Tooling & Observability
- [ ] Debug authoring: scene gizmo drawer showing grid bounds/cell occupancy.  
- [ ] Runtime overlay (Entities Graphics or UI) toggled via debug menu.  
- [ ] Logging hooks to sample cell density, worst-case query cost.  
- [ ] Editor validation to warn when config bounds too small/large.

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
- [ ] **Profiling automation**: Integrate with performance harness TODO to track spatial grid metrics in CI.

### 7. Documentation & Adoption
- [ ] Update `Docs/Guides/SceneSetup.md` with instructions for adding a spatial profile to new scenes.  
- [ ] Add `Docs/DesignNotes/SpatialPartitioning.md` summarizing design decisions and provider roadmap.  
- [ ] Reflect progress in `Docs/Progress.md` and cross-link to registries/TODOs.

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
