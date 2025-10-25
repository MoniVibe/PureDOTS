# System Integration Test Checklist (Scaffold)

_Last updated: 2025-10-25_

Purpose: track the cross-system flows we expect to validate once environment, spatial, and hand/miracle systems stabilise. Use this as a living checklist alongside `Docs/TODO/SystemIntegration_TODO.md`.

## Climate ↔ Vegetation ↔ Resources

### Scenario: Seasonal cadence drives vegetation/resource loop
1. Start integration scene with environment system enabled.
2. Advance simulation for 2 in-game days (accelerated time).
3. **Expected**: `EnvironmentSystemGroup` updates moisture/temperature/wind grids; `VegetationGrowthSystem` adjusts growth rate; `ResourceRegistrySystem` shows updated active counts.
4. Record before/after values (moisture cell sample, vegetation stage, resource `UnitsRemaining`).

### Scenario: Rain miracle impacts villagers and rewind
1. Trigger rain miracle via RMB hold; ensure `MiracleSustainSystem` adds moisture.
2. Observe vegetation health recovery and villager gather/deliver cycle (registry events fired).
3. Rewind 200 ticks → play forward.
4. **Expected**: Moisture/vegetation/resource values match pre-rain state after rewind; villager inventories/delivery events replay identically.

## Hand / Miracle / Resource Interactions

### Scenario: Hand siphon conflict resolution
1. Hover over resource pile with hand carrying items; note router context from debug HUD.
2. Perform siphon → dump into storehouse.
3. **Expected**: Router prioritises siphon, registry totals update, spatial grid cell counts adjust; rewind restores original pile/storehouse amounts.

### Scenario: Token miracle with environmental modifiers
1. Pick up fireball token, charge to extreme, throw downwind.
2. Check that wind sampling modifies effect spread and debris registry entries populate.
3. **Expected**: Hand state transitions follow central router; effect footprint deterministic across runs.

## Spatial Grid Consumers

### Scenario: Villager job assignment and spatial queries
1. Spawn 50 villagers with jobs pending.
2. Confirm `VillagerJobAssignmentSystem` queries spatial grid + registries (log instrumentation).
3. **Expected**: Job ticket issuance deterministic; gather/deliver loop completes with matching totals on replay.

### Scenario: Hand/miracle hover prioritisation
1. Place storehouse, pile, and miracle token under cursor simultaneously.
2. Cycle through priority states ensuring router picks correct handler per truth-source hierarchy.
3. **Expected**: No jitter; highlight feedback matches router winner; spatial queries only run once per tick.

## Terraforming Hooks (Future)

### Scenario: Terrain version propagation
1. Trigger terraform command raising terrain.
2. Ensure `TerrainVersion` increments and `TerrainChangeEvent` buffer populated.
3. **Expected**: Next environment cadence rebuilds affected cells; spatial grid updates Y strata; vegetation/resource systems respond (e.g., moisture recalculated).

## Notes
- Reference `Docs/DesignNotes/SystemExecutionOrder.md` for expected group order when scripting playmode tests.
- Rewind behaviours must follow patterns in `Docs/DesignNotes/RewindPatterns.md`.
