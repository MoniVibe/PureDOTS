# Terraforming Prototype TODO (Experimental)

> **Generalisation Guideline**: Terraforming architecture should remain optional and generic so future projects (RTS, city builder, space sim) can hook in by defining effect recipes rather than modifying core systems.

## Goal
- Explore a future system where the player (or game systems) can reshape terrain height, moisture, and biome characteristics in real time, matching the “god-game” fantasy of molding the world.
- Produce an architectural plan that respects our current DOTS foundation (moisture grids, biome profiles, spatial services, vegetation/resources) while remaining optional/experimental.
- Identify integration points, risks, and verification steps so the feature can be phased in once core systems are stable.
- Reference runtime and integration contracts in `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` and `Docs/TODO/SystemIntegration_TODO.md` when planning hooks.

## Plain-Language Primer
- Terraforming means the player can raise/lower terrain (mountains, valleys), flood or dry regions, heat/cool areas to change biomes, or alter soil types.
- In BW2-like fashion, the divine hand acts as the primary tool: grab a chunk of terrain, drag it up/down, pour water, or turn desert into fertile land.
- Our DOTS systems (moisture grid, climate, spatial services, vegetation, resources) will need to react to these changes smoothly and deterministically.

## Important: Future Feature with Current Architecture Considerations

This is **NOT for immediate implementation**. Instead, this plan identifies:
1. **Architecture hooks** to add to current systems (minimal overhead, 2-3 days work)
2. **Data structure flexibility** to support future terrain modification
3. **Design patterns** that won't require major refactoring later
4. **Performance considerations** for current code decisions

## BW2 & God Game Reference Behavior

### Terrain Sculpting (Divine Hand)
- **Raise terrain**: Click + drag upward → creates hill/mountain
- **Lower terrain**: Click + drag downward → creates valley/depression
- **Flatten**: Smooth tool → levels bumpy terrain
- **Brush size**: Configurable radius (small precise edits to massive landscape changes)
- **Height limits**: Min/max elevation to prevent extreme deformation
- **Smoothing**: Automatic smoothing at brush edges (no jagged cliffs unless intentional)

### Impact-Based Deformation
- **Meteor miracle**: Creates crater on impact (depresses terrain in radius)
- **Earthquake miracle**: Random terrain displacement, cracks form
- **Volcano miracle**: Raises terrain into cone shape, lava flow paths
- **Boulder throw**: Small indentation where heavy object lands
- **Creature stomp**: Large creature creates footprints (minor deformation)

### Biome Transformation
- **Hydration**: Pour water → increase moisture → desert becomes grassland over time
- **Dehydration**: Remove water → decrease moisture → grassland becomes desert
- **Heating**: Fire/lava → raise temperature → temperate becomes tropical/desert
- **Cooling**: Ice/freeze → lower temperature → temperate becomes tundra
- **Natural transition**: Biomes shift gradually based on temperature + moisture thresholds

### Soil Manipulation (Advanced)
- **Dig**: Remove topsoil → expose rock/sand (infertile)
- **Deposit soil**: Add fertile layer → improve agriculture
- **Erosion**: Water flow carves channels over time
- **Sedimentation**: Rivers deposit soil downstream

### Biome Transition Logic (Temperature + Moisture)
```
Temperature + Moisture → Biome Type

Cold + Dry = Tundra (frozen wasteland)
Cold + Wet = Taiga (snowy forest)
Temperate + Dry = Grassland (plains)
Temperate + Wet = Temperate Forest
Hot + Dry = Desert (sand, cacti)
Hot + Wet = Tropical Rainforest

Thresholds:
  Cold: <10°C
  Temperate: 10-25°C
  Hot: >25°C
  
  Dry: <40 moisture
  Moderate: 40-70 moisture
  Wet: >70 moisture
```

## Architecture Considerations for Current Systems

### 1. Spatial Grid (MUST Support Future Terrain Height)

**Current Decision**:
- Use 2D grid (XZ plane) with **optional Y strata** for future
- Cell indexing should NOT assume flat terrain

**Future-Proof Changes**:
```csharp
// Instead of: int cellIndex = GridHash(position.x, position.z);
// Use (with Y ignored for now):
int cellIndex = GridHash(position.x, position.z, 0); // Y strata = 0

// Later can activate Y:
int yStrata = GetYStrataFromHeight(position.y);
int cellIndex = GridHash(position.x, position.z, yStrata);
```

**Benefit**: Adding height layers later doesn't break existing queries

### 2. Moisture Grid (SHOULD Support Dynamic Terrain)

**Future-Proof Changes**:
- Store terrain height per cell for future water flow simulation
- Add `TerrainHeight` field to MoistureGridBlob (initially matches base terrain)
- Moisture calculations can later account for slope (water flows downhill)

### 3. Climate Grid (SHOULD Support Altitude Effects)

**Future-Proof Changes**:
- Add altitude modifier to temperature calculation
- Higher elevation = cooler temperature (lapse rate: -6.5°C per 1000m)

### 4. Vegetation System (MUST React to Terrain Changes)

**Architecture Hook** (add to VegetationSystems):
```csharp
// Interface for terrain-aware systems
interface ITerrainChangeListener
{
  void OnTerrainHeightChanged(float3 position, float oldHeight, float newHeight);
  void OnBiomeChanged(float3 position, BiomeType oldBiome, BiomeType newBiome);
}
```

### 5. Pathfinding (Flow Fields MUST Recalculate on Terrain Change)

**Future-Proof Changes**:
- Mark flow fields as "dirty" when terrain in region modified
- Store terrain version number per flow field (invalidate if mismatch)

## Workstreams & Tasks

### -1. Architecture Hooks (Add NOW - Minimal Cost)
**These changes future-proof current systems with ~100 lines of code, 2-3 days effort**:
- [x] **Spatial Grid**: Add optional Y strata parameter to hash function (ignored = 0 for now, future 3D support). (Verified: SpatialHash supports Y strata parameter)
- [x] **Moisture Grid**: Add `TerrainHeight` field to MoistureGridCell (store base terrain elevation). (Implemented: `MoistureGridBlob` includes `TerrainHeight` array)
- [ ] **Climate Grid**: Add altitude modifier to temperature calculation (higher = cooler). (Pending: altitude modifier integration)
- [ ] **Vegetation**: Add `ITerrainChangeListener` interface (empty implementation for now). (Pending: interface definition)
- [x] **Flow Fields**: Add `TerrainVersion` field to FlowFieldData (invalidation support). (Implemented: `FlowFieldConfig` includes `TerrainVersion` field)
- [x] **Add TerrainVersion singleton**: `struct TerrainVersion : IComponentData { uint Version; }`. (Implemented: `TerrainVersion` exists in `TerrainComponents.cs`)
- [x] **Reserve stub components**: `TerrainModificationPending`, `TerrainHeightDelta` (empty, for future). (Implemented: `TerrainChangeEvent` buffer provides event infrastructure)

**Effort**: 2-3 days, **zero runtime cost** (unused components optimized away by compiler)

### 0. Recon & Constraints
- [ ] Review current terrain representation (Unity Terrain, heightmap, ECS proxies) and determine modifiability.
- [ ] Audit existing environment data (moisture grid, climate, biomes) to see how they tie to terrain coordinates.
- [ ] Identify dependent systems: vegetation placement, resource nodes, nav/pathing, storehouses, miracles (rain).
- [ ] Document BW2 behaviour (divine hand sculpting, miracles altering terrain) for parity targets.
- [ ] Establish performance budgets: acceptable update frequency, chunk sizes, resolution.

### 1. Data & Authoring Model
- [ ] Define `TerraformConfig` ScriptableObject with:
  - Grid resolution / chunk size for height edits.
  - Allowed elevation range, slope limits, smoothing settings.
  - Biome transition thresholds (temperature, moisture).
  - Soil type definitions (sand, clay, loam).
- [ ] Create `TerraformState` singleton storing:
  - Height delta buffers (relative to base terrain).
  - Pending operations (queue of sculpt commands).
  - Biome override map (if terrain type changed).
- [ ] Build `TerraformHistory` component/buffer for rewind snapshotting (height deltas, biome changes).
- [ ] Authoring guidelines: base terrain remains in standard assets; terraform operations stored in DOTS overlay.

### 2. Sculpting Mechanics (Raise/Lower)
- [ ] **Define terrain modification components**:
  - `TerrainHeightGrid` singleton - BlobAssetReference to height data, resolution, version
  - `TerrainHeightGridBlob` - BlobArray of BaseHeights and HeightDeltas
  - `TerrainSculptCommand` buffer - Queued operations (raise/lower/flatten)
- [ ] **Implement TerrainSculptCommandSystem**:
  - Receives sculpt commands from divine hand input
  - Validates operations (within height limits, slope constraints)
  - Queues commands with tick, position, radius, intensity, operation type
- [ ] **Implement TerrainHeightUpdateSystem**:
  - Process queued commands in deterministic order (sorted by tick + index)
  - Update height delta buffer using Burst jobs (per affected cell)
  - Apply brush falloff (smooth edges, no jagged transitions)
  - Clamp heights to min/max range (-100m to +500m)
  - Increment TerrainVersion when modifications applied
- [ ] **Implement TerrainMeshRebuildSystem**:
  - Recalculate mesh vertices from BaseHeight + HeightDelta
  - Update normals for lighting
  - Throttle rebuilds (max 4 chunks per frame to prevent spikes)
  - Priority order: camera-near chunks first
- [ ] **Hand sculpting input controls** (divine hand extension):
  ```
  Hold Shift + LMB drag up → Raise terrain at cursor
  Hold Shift + LMB drag down → Lower terrain at cursor  
  Hold Shift + MMB → Flatten terrain (smoothing brush)
  Scroll + Shift → Adjust brush size (5m to 50m radius)
  ```
- [ ] Provide smoothing/flattening operations (separate brush mode).
- [ ] Integrate with dynamic mesh update (Graphics Hybrid/Entities Graphics) or mark terrain for re-upload.
- [ ] Maintain collision/nav mesh update hooks (future integration point).

### 3. Hydration & Temperature Manipulation
- [ ] Implement `TerrainHydrationSystem`:
  - Add water (increase moisture grid values, create water bodies) when hand “hydrates”.
  - Remove water (drain moisture, raise evaporation) when “dehydrate”.
- [ ] Implement `TerrainTemperatureSystem`:
  - Apply local heating/cooling with falloff; update climate grid accordingly.
- [ ] Allow miracles (rain, fire) to tie into same API for consistent results.
- [ ] Ensure vegetation/resource systems respond (see integration).

### 4. Biome & Soil Adjustments
- [ ] Create `BiomeTransitionSystem`:
  - Based on temperature/moisture, update biome map (per cell).
  - Trigger vegetation/regrowth rules (swap species preferences).
- [ ] Add soil layer adjustments:
  - Dig mechanic: remove soil (change soil type to sand/rock).
  - Deposit soil: add fertile layer (loam/clay).
- [ ] Provide deterministic update order and history logging.

### 5. Integration with Existing Systems (Detailed Responses)
- [ ] **Vegetation Response**:
  - Height change: Plants on steep slopes (>45°) die or become unstable
  - Flooding: Plants underwater die (or swap to aquatic species if available)
  - Biome change: Species unsuitable for new biome die, new species can spawn
  - Soil change: Nutrient levels update based on new SoilType (Loam=high, Sand=low)
- [ ] **Resource Nodes**:
  - Buried: Terrain raised over ore deposit → ore becomes inaccessible (disable gathering)
  - Exposed: Terrain lowered → reveals buried ore deposit (enable gathering)
  - Destroyed: Severe deformation destroys resource node entirely
- [ ] **Pathfinding Impact**:
  - Flow fields: Terrain height changes invalidate affected flow fields → trigger rebuild
  - Obstacle detection: Steep terrain (>30°) becomes impassable (slope check in flow field gen)
  - Rerouting: Villagers detect path blocked, request new path from flow field system
- [ ] **Water Bodies** (Advanced - future):
  - Flooding: Lowering terrain below water table → creates pond/lake
  - Drainage: Raising terrain → water flows away
  - Rivers: Carve channel → water follows path (requires water simulation)
- [ ] **Buildings/Construction**:
  - Foundation check: Buildings require flat terrain within tolerance (e.g., ±2m variance)
  - Collapse: Severe terrain change under building → damage or destroy
  - Adaptation: Small changes absorbed (building "settles" visually)
- [ ] Spatial grid: update cell heights for queries (may require reindexing or z-component adjustments).
- [ ] Presentation: update water visuals, terrain materials, VFX.

### 6. Tooling & Feedback
- [ ] Hand UI for sculpting intensity/brush size.
- [ ] Preview overlay (heatmap) for pending changes.
- [ ] Visual feedback for temperature/hydration adjustments (glow, steam, dryness).
- [ ] Undo/redo preview for experimental mode.
- [ ] Debug tools to inspect height deltas, biome map, soil types.

### 7. Testing & Safety Nets
- [ ] Unit tests for height delta application, biome transitions, moisture updates.
- [ ] Playmode tests: sculpt, rewrite, rewind; ensure determinism.
- [ ] Stress tests with frequent terrain edits; measure frame impact.
- [ ] Rewind tests: ensure operations replay identically.
- [ ] Sandbox scene for designers to experiment with sculpting.

## Rewind & Determinism Strategy

### Challenge
Terrain modification is **large-state problem** (256x256 cells = 65k height values)

### Solutions

**Option A: Delta Snapshots** (Recommended)
- Only record cells that changed since last snapshot
- Sparse storage: `[(cellIndex, oldHeight, newHeight)]`
- Replay applies deltas in sequence
- Efficient for localized edits

**Option B: Keyframe + Interpolation**
- Snapshot full grid every 60 seconds
- Between keyframes, record delta commands
- Replay: restore keyframe + apply deltas

**Option C: Command Replay**
- Store sculpt commands (position, radius, intensity, seed)
- Replay executes commands (deterministic if order preserved)
- Most efficient, but requires stable command application

**Recommended**: Start with **Option C** (command replay), fall back to **Option A** (delta snapshots) if non-determinism appears

### History Structure
```csharp
struct TerrainModificationHistory : IBufferElementData
{
  uint Tick;
  byte OperationType; // Raise, Lower, HeatArea, CoolArea, AddSoil, etc.
  float3 Position;
  float Radius;
  float Intensity;
  uint Seed; // For randomized ops (earthquake, noise)
}
```

## Performance Targets

### Terrain Update Budget
- Height grid update: <2ms per frame (amortized, not every frame)
- Mesh rebuild: <10ms (throttled, e.g., once every 10 frames)
- Collision update: <5ms (only affected chunks)
- Nav mesh rebuild: Background thread (future, when nav implemented)

### Grid Resolutions (Recommendations)
```
Small World (1km²): 256x256 cells = ~4m resolution
Medium World (4km²): 512x512 cells = ~8m resolution
Large World (16km²): 1024x1024 cells = ~16m resolution (requires streaming)
```

### Chunk-Based Updates
- Divide terrain into 64x64 cell chunks (or 32x32 for finer control)
- Only update chunks with pending modifications
- Mark chunks dirty, rebuild in priority order (near camera first)
- Maximum 4 chunks rebuilt per frame (prevent frame spikes)

## Risk Assessment

### Technical Risks
1. **Performance**: Terrain mesh updates expensive (GPU upload bottleneck)
2. **Determinism**: Floating-point precision in height calculations
3. **Collision sync**: Physics mesh out of sync with visual mesh
4. **Water simulation**: Flooding requires full fluid system (complex, major feature)
5. **Nav mesh update**: Dynamic pathfinding recalculation expensive

### Mitigation Strategies
1. **Chunked updates**: Only rebuild modified terrain sections
2. **Fixed-point heights**: Use integer cm storage (int16 for ±327m range)
3. **Async collision**: Update physics mesh in background job
4. **Simplified water**: Use volume fill approach, skip full fluid simulation initially
5. **Lazy nav rebuild**: Delay pathfinding updates, invalidate stale paths gracefully

### Gameplay Risks
1. **Player breaks world**: Creates impossible terrain (villagers stranded on cliffs)
2. **Performance abuse**: Player over-sculpts everywhere, lags game
3. **Balancing**: Terraforming too powerful (trivializes gameplay challenges)
4. **Undo complexity**: Players want to revert large changes easily

### Mitigation Strategies
1. **Validation**: Enforce slope limits (max 60°), prevent extreme deformation
2. **Limits**: Sculpt budget (e.g., 1000 total terrain edits per session)
3. **Costs**: Require prayer power/mana to sculpt (economy integration)
4. **Undo system**: Store sculpt history separate from full rewind (last 50 operations)

## Success Criteria (Prototype Validation)

### Prototype Successful If:
- Terrain height modification visually works (smooth mesh updates)
- Performance acceptable (<5ms per frame for typical edit operation)
- Vegetation responds correctly (plants die/spawn appropriately based on changes)
- Rewind works (terrain changes replay deterministically, pixel-perfect)
- No major technical blockers discovered during prototyping
- Designer says "this feels powerful and fun to use"

### Prototype Fails If:
- Performance unacceptable (>20ms per frame for typical operations)
- Determinism impossible (floating-point drift too severe, can't replay)
- Unity Terrain integration prohibitively complex or brittle
- Mesh updates cause severe visual artifacts (z-fighting, holes, flicker)
- Gameplay breaks (villagers/systems fundamentally can't handle dynamic terrain)

### Go/No-Go Decision Criteria:
- **Go (full implementation)**: All success criteria met, performance good, designer excited
- **Scope down**: Partial success → implement height-only, defer biome/soil changes
- **Shelf feature**: Major blockers found → document learnings, revisit in 1-2 years

## Experimental Prototype Phases (When Ready - 6-12 Months from Now)

### Prototype Phase 1: Heightmap Delta (3-4 weeks)
**Goal**: Prove terrain height modification works in DOTS
- Research Unity Terrain integration with DOTS
- Implement height delta grid (blob storage)
- Build basic sculpt command system (queue operations)
- Test raise/lower at single point
- Measure performance (update time, memory footprint)
- Validate rewind (command replay determinism)
**Deliverable**: Tech demo video showing terrain sculpting

### Prototype Phase 2: Hand Sculpting UI (2 weeks)
**Goal**: Player can sculpt with divine hand
- Add sculpt mode to divine hand (Shift modifier + mouse drag)
- Implement brush system (size, intensity, falloff)
- Visual feedback (brush circle, height preview wireframe)
- Test user experience (feel test with designer)
**Deliverable**: Playable sculpting demo

### Prototype Phase 3: Biome Transitions (2-3 weeks)
**Goal**: Climate manipulation changes biome types
- Extend climate grid with biome determination (temp + moisture → biome)
- Implement heat/cool/hydrate/dehydrate commands
- Hook vegetation system to react to biome changes
- Test desert → grassland → forest transition
- Measure ecosystem response time
**Deliverable**: Biome transformation video

### Prototype Phase 4: Impact Deformation (1-2 weeks)
**Goal**: Miracles create terrain changes
- Crater pattern generation (meteor - depression + raised rim)
- Random displacement (earthquake - noise field)
- Cone elevation (volcano - raised peak)
- Test visual quality and gameplay impact
**Deliverable**: Impact effects showcase video

### Prototype Phase 5: Integration Testing (2 weeks)
**Goal**: Validate all systems work together
- Sculpt terrain + vegetation adapts (plants die on cliffs, spawn in valleys)
- Create crater + water pools in depression (moisture accumulates)
- Raise mountain + pathfinding reroutes (flow fields rebuild)
- Full rewind test (complex terrain edit sequence replays identically)
**Deliverable**: Integration validation report, bug list

### Prototype Phase 6: Performance Optimization (2-3 weeks)
**Goal**: Scale to production requirements
- Chunk-based updates (only dirty regions)
- Mesh rebuild throttling (spread over multiple frames)
- LOD for distant terrain (skip updates beyond range)
- Memory optimization (sparse storage, pooling)
- Benchmark with large worlds (1024x1024 grids)
**Deliverable**: Performance report, go/no-go recommendation

**Total Prototype Time**: 14-20 weeks (3.5-5 months)

## Open Questions (Answer Before Starting Prototype)

1. **Terrain representation**: Unity Terrain, custom mesh, voxel, or heightmap data only?
2. **Edit resolution**: How fine-grained (1m cells, 5m cells, 10m cells)? What keeps 60 FPS?
3. **Height range**: What's min/max elevation (-100m to +500m realistic for BW2 style)?
4. **Slope limits**: Can players create sheer cliffs (>60°), or enforce maximum angle?
5. **Water interaction**: Full fluid simulation, or simple volume fill for ponds?
6. **Biome persistence**: Do changes last forever, or revert without maintenance?
7. **Multiplayer**: How do concurrent edits resolve (if applicable, conflict resolution)?
8. **MVP scope**: Which features are minimum viable (height only? height + moisture? full biome)?
9. **Terraforming cost**: Free (creative mode) or cost resources/prayer (gameplay mode)?
10. **Terraform charges**: Limited uses per session, or unlimited?
11. **Restricted zones**: Can't modify near temple/settlements, or no restrictions?
12. **Reversibility**: Easy undo, or permanent changes (strategic consequence)?
13. **Creature terraforming**: Do creatures dig/build terrain, or player-only?
14. **Terrain healing**: Should terrain slowly return to original, or permanent modification?
15. **Permissions**: Multiplayer/co-op considerations for who can terraform?
16. **Biome limits**: Can't raise mountains in swamps (physical constraints)?
17. **Streaming/LOD**: How reconcile with large world streaming (if implemented)?

## Architecture Recommendations (Summary)

### For Current Implementation (Add NOW)

**DO** ✅:
- Add Y strata support to spatial grid (parameter ignored for now, enables future 3D)
- Store terrain height in moisture grid cells (TerrainHeight field)
- Add altitude modifier to climate temperature calculation
- Create terrain-change listener interface (empty implementations in vegetation/pathfinding)
- Add TerrainVersion singleton (tracks terrain modification state, increment = 0 initially)
- Reserve stub components (TerrainModificationPending, TerrainHeightDelta buffers)

**DON'T** ❌:
- Implement terrain mesh updates yet (representation TBD)
- Add water simulation systems (major separate feature)
- Modify Unity Terrain directly (needs prototype validation)
- Add sculpting UI/controls (not until prototype phase)
- Integrate collision updates (depends on physics approach)

**Cost**: 2-3 days of careful changes, ~100 lines of code additions, **zero runtime cost** until terraforming activated

### For Future Prototype (6-12 Months)

**When ready to prototype** (after core systems stable):
1. Create experimental branch for terraforming
2. Implement Phase 1 (heightmap delta overlay)
3. Measure performance, validate technical approach
4. If promising, continue to Phases 2-6 (sculpting UI, biome, impacts, integration, optimization)
5. Present findings and performance data for go/no-go decision

**Timeline**: Start prototype after spatial grid, vegetation, pathfinding, and miracles are mature and stable

## Dependencies & Links

- **Spatial grid** (see `SpatialServices_TODO.md`) - needs Y strata support for 3D queries
- **Moisture grid** (see `VegetationSystems_TODO.md`) - stores terrain height per cell, water flow
- **Climate grid** (see `VegetationSystems_TODO.md`) - altitude affects temperature calculation
- **Vegetation systems** (see `VegetationSystems_TODO.md`) - must react to terrain/biome changes, plant suitability
- **Pathfinding** (flow fields in `VillagerSystems_TODO.md`) - invalidate and rebuild when terrain changes
- **Divine hand** (see `DivineHandCamera_TODO.md`) - sculpting input interface, brush controls
- **Miracles framework** (see `MiraclesFramework_TODO.md`) - impact-based deformation (meteor, earthquake, volcano)
- **Resource systems** (see `ResourcesFramework_TODO.md`) - nodes buried/exposed by height changes
- **Rewind system** - terrain modification history and deterministic replay
- **Physics system** - collision mesh updates when terrain modified
- **Presentation/VFX** - visual updates, water effects, terrain materials

## Suggested Roadmap & Next Steps

### Immediate (NOW - 2-3 Days)
1. **Add architecture hooks** (Workstream -1) to current systems
2. Document changes in design notes
3. Mark terraforming components as "experimental/future use"
4. **Total effort**: 2-3 days, zero runtime overhead

### Near-Term (1-2 Months)
1. Complete recon and research (Workstream 0) - 1-2 weeks
2. Draft detailed design note (`TerraformingArchitecture.md`) outlining:
   - Component layout and data structures
   - System flow and integration points
   - Performance analysis and optimization strategies
   - Risk mitigation approaches

### Prototype Phase (6-12 Months from Now)
1. Wait for core systems to stabilize (spatial grid, vegetation, pathfinding)
2. Create experimental branch for terraforming prototype
3. Execute Prototype Phases 1-6 (14-20 weeks total)
4. Evaluate results against success criteria
5. Make go/no-go decision based on findings

### Full Implementation (If Greenlit)
1. If prototype successful: Plan 6-12 month full implementation
2. If partially successful: Scope down to height-only or basic features
3. If unsuccessful: Document learnings, shelf for 1-2 years

Track this TODO as exploratory work; keep notes on findings, blockers, and design decisions. Update as architecture hooks are added to current systems. Revisit when core systems mature and team capacity allows for prototype phase.
