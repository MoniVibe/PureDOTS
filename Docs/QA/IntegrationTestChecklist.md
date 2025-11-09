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

### Scenario: Hand holding resource + miracle token (deterministic resolver)
1. Set up scene with resource pile and miracle token entity nearby.
2. Start siphon from pile, then attempt to pick up miracle token while hand is siphoning.
3. **Expected**: Router resolves conflict deterministically (siphon takes priority per `HandInputRouterSystem` priority table); `HandInteractionState` and `ResourceSiphonState` remain synchronized; no state divergence between systems.
4. Verify in debug HUD that `HandInteractionState.FlagSiphoning` is set and `ResourceSiphonState.FlagSiphoning` matches.
5. Rewind and replay; verify same resolution order and state synchronization.

### Scenario: Hand dumps to storehouse after miracle charge
1. Charge a miracle token in hand (hold RMB over token).
2. While holding charged token, hover over storehouse.
3. Release RMB to dump resources (if any) or trigger miracle.
4. **Expected**: Router prioritizes storehouse dump > miracle release per priority table; `HandInteractionState` transitions correctly; registry totals update atomically; no partial state updates.
5. Verify via debug HUD that hand state transitions from `HoldingMiracle` → `Dumping` → `Empty` correctly.

### Scenario: Token miracle with environmental modifiers
1. Pick up fireball token, charge to extreme, throw downwind.
2. Check that wind sampling modifies effect spread and debris registry entries populate.
3. **Expected**: Hand state transitions follow central router; effect footprint deterministic across runs.

### Scenario: Centralized feedback events (VFX/audio)
1. Enable `DivineHandEventBridge` component in scene.
2. Perform siphon, dump, and miracle gestures.
3. **Expected**: All hand state changes emit events via `DivineHandEvent` buffer; `DivineHandEventBridge` dispatches to Unity Events; VFX/audio systems subscribe to single event stream; no duplicate event emissions from separate systems.
4. Verify event log shows consistent event sequence (state changes, type changes, amount changes).

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

## Implementation Status

### Hand–Resource–Miracle Integration (Architecture Complete)
- ✅ `HandInputRouterSystem` centralizes RMB routing with priority table (`HandRoutePriority`)
- ✅ `HandInteractionState` and `ResourceSiphonState` provide shared state contracts
- ✅ `DivineHandEventBridge` centralizes event stream for VFX/audio
- ✅ Priority order: `MiracleOverride (100) > ResourceSiphon (80) > DumpToStorehouse (60) > GroundDrip (40)`
- ⏳ Full resource chunk physics/siphon systems are planned in `ResourcesFramework_TODO.md` but not yet implemented
- ⏳ Resource pile systems (`ResourcePileRegistry`, pile merge, siphon execution) are planned but pending

### Spatial Grid Integration (Substantially Complete)
- ✅ Spatial queries integrated into villager systems, resource registry, divine hand `FindPickable`
- ✅ Registry systems compute spatial cell IDs for entries
- ✅ Debug gizmos and editor validation in place
- ⏳ Miner/hauler logistics spatial integration pending (planned)

## Notes
- Reference `Docs/DesignNotes/SystemExecutionOrder.md` for expected group order when scripting playmode tests.
- Rewind behaviours must follow patterns in `Docs/DesignNotes/RewindPatterns.md`.
- For resource chunk physics implementation, see `Docs/TODO/ResourcesFramework_TODO.md` section 2 (Chunk Lifecycle & Physics).
