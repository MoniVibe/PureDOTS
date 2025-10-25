# Vegetation Systems Expansion TODO

> **Generalisation Guideline**: Treat vegetation growth/propagation as data-driven modules so different projects (forests, crops, alien flora) can reuse the same systems by swapping species/biome configs.

## Goal
- Extend the vegetation simulation with terrain-aware moisture grids, biome-linked climate (wind + temperature), and deterministic propagation (saplings, spread) while maintaining existing growth/health loops.
- Keep systems scalable (hundreds of thousands of plants), deterministic under rewind, and configurable through existing assets (`VegetationSpeciesCatalog`, biome profiles).
- Provide tooling and documentation so designers can tune biomes, climate, and propagation without touching code.
- Stay aligned with `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` and cross-system guidance in `Docs/TODO/SystemIntegration_TODO.md`.

## Plain-Language Primer
- Vegetation already grows/ages via DOTS systems. We now need the **environment**: soil moisture tied to terrain/biome, climate (wind & temperature) that affects health, and **propagation** that spawns new plants around mature ones.
- Moisture grid is like a weather map over the terrain; climate settings depend on biome (desert vs. forest).
- Propagation ensures forests spread logically: mature trees spawn saplings nearby, respecting density and biome rules.

## References
- PureDOTS docs: `Docs/DesignNotes/VegetationAssets.md`, existing systems (`VegetationGrowthSystem`, `VegetationHealthSystem`, `VegetationReproductionSystem`).
- Legacy truth sources: biome/terrain expectations from `godgame/truthsources` (general vision mentions biomes; align with BW2-like behaviour).
- Spatial & registry plans (`SpatialServices_TODO.md`, `RegistryRewrite_TODO.md`) for environment indexing and pooling.
- Environment cadence & sampling (`EnvironmentSystemGroup`, `EnvironmentGrids.cs`) define authoritative moisture/temperature/wind/sunlight data.

## Dependencies & Shared Infrastructure
- Consume shared environment grid updates (moisture, temperature, wind, sunlight, radiation) via `EnvironmentGridMath` helpers; do not reimplement samplers.
- Leverage spatial registries/grid queries for propagation, seed dispersal, and villager/creature interactions.
- Integrate with central registry utilities (`DeterministicRegistryBuilder`) and ensure rewind guards follow `RewindPatterns.md` guidance.

## BW2 Reference Behavior (Target Parity)
### Forest Growth & Spreading
- Trees start as seedlings, grow to mature over time (clear visual stages)
- Mature trees drop seeds that grow into new trees (natural spreading)
- Trees don't overcrowd - spacing is maintained (density control)
- Trees provide wood when harvested by foresters
- Forests look natural and organic (not grid-aligned)

### Environmental Factors
- Trees near water grow better (moisture availability)
- Growth slows in winter, accelerates in spring/summer (seasonal)
- Player can instantly plant trees via divine hand
- Forests can be damaged by fire, disasters
- Certain areas more favorable for growth (terrain suitability)

### Harvesting & Regrowth
- Foresters chop down mature trees for wood
- Harvested trees leave stumps (visual indicator)
- New trees eventually replace harvested ones (regrowth)
- Sustainable forestry: balance between harvesting and growth

## Target Values (Configuration Reference, WIP)

### Plant Needs Consumption (per second)
```
Tree (Mature):
  Water: 2.0 units/s
  Nutrients: 0.5 units/s
  
Shrub:
  Water: 1.0 units/s
  Nutrients: 0.3 units/s
  
Grass:
  Water: 0.5 units/s
  Nutrients: 0.1 units/s
```

### Moisture Grid Settings
```
Cell Resolution: 5m (default) - balance detail vs. performance
Evaporation Rate: 0.5 units/s (base, modified by temperature, terrain type)
Rain Moisture Add: +50 units per rain event (local cells)
Water Body Seepage: +2 units/s within 10m radius of water
Max Moisture Capacity: 100 units per cell
Update Frequency: Every 10 ticks for performance
```

### Climate Settings
```
Temperature Ranges (Seasonal):
  Spring: 15-25°C
  Summer: 20-35°C
  Autumn: 10-20°C
  Winter: -5-10°C

Growth Modifiers (Optimal temp 20-25°C):
  Below 0°C: 0.0x (dormant/frost damage)
  0-10°C: 0.3x
  10-20°C: 0.7x
  20-25°C: 1.0x (optimal)
  25-30°C: 0.8x
  Above 35°C: 0.4x (heat stress)

Wind Strength:
  Calm: 0-5 m/s
  Breezy: 5-15 m/s
  Stormy: 15-30 m/s
```

### Growth Rates (BW2 Reference)
```
Seedling → Growing: 30-60 seconds
Growing → Mature: 2-5 minutes
Mature → Flowering: 5-10 minutes
Flowering → Fruiting: 1-2 minutes
Fruiting → Dying: 10-20 minutes (harvestable window)
Dying → Dead: 30-60 seconds
```

## Workstreams & Tasks

### 0. Recon & Gap Analysis
- [ ] Audit current vegetation components and systems to understand data flow (health, reproduction, rain interactions).
- [ ] Review `VegetationSpeciesCatalog` to note existing thresholds (water, temp, wind) and identify missing climate data.
- [ ] Gather legacy requirements for biomes/terrain types (soil fertility, rainfall) and list new assets needed.
- [ ] Profile current vegetation counts/perf to set targets for moisture/climate updates.
- [ ] Confirm rewind integration (vegetation history) and determine where moisture/climate state must snapshot or rebuild.
- [x] Identify Native container/ECB usage in vegetation systems (reproduction, decay, spawn) and plan migration to shared `Nx.Pooling` helpers.

### 1. Data & Asset Extensions
- [ ] **Add Plant Needs Components** in `VegetationComponents.cs`:
  - `VegetationNeeds` - Water (0-100), Light (0-100), Nutrients (0-100), TemperatureComfort (0-100)
  - `VegetationClimateState` - LocalTemperature, LocalWind, LastClimateUpdate, MoistureGridCell index
  - `VegetationGrowthModifiers` - WaterModifier, LightModifier, TempModifier, NutrientModifier, SeasonModifier, FinalGrowthRate
- [ ] **Define Grid Structures**:
  - `MoistureGrid` singleton with BlobAssetReference<MoistureGridBlob>
  - `MoistureGridBlob` containing BlobArray<float> for moisture levels, drainage rates, last rain tick per cell
  - `ClimateGrid` singleton with BlobAssetReference<ClimateGridBlob>
  - `ClimateGridBlob` containing BlobArray<float> for temperature and BlobArray<float2> for local wind per cell
  - `SunlightGrid` singleton with BlobAssetReference<SunlightGridBlob>
  - `SunlightGridBlob` containing BlobArray<float> for accumulated light intensity per cell and BlobArray<float> for last light update tick
- [ ] Define `BiomeProfile` asset (if not already) with terrain type, base moisture, wind, temperature curves per time-of-day/season.
- [ ] Extend `VegetationSpeciesCatalog` (or companion blob) with propagation parameters tied to biome (preferred soil types, density caps).
- [ ] Create `MoistureGridConfig` ScriptableObject storing grid resolution, world bounds, initial moisture per biome.
- [ ] Add `ClimateProfile` asset describing wind/temperature variance per biome + seasonal multipliers.
- [ ] Add `SunlightProfile` asset describing global daylight curve per biome and emitter types (intensity, radius, falloff).
- [ ] **Add consumption rate fields to species profiles**: WaterConsumptionRate, NutrientConsumptionRate, OptimalTempMin, OptimalTempMax, ShadeTolerance, LightToleranceHours.
- [ ] Update bakers to convert new assets into blobs/singletons accessible by systems.
- [ ] Ensure assets integrate with existing authoring flow (SceneSetup docs, config objects).
- [ ] Confirm vegetation systems read from shared environment grid runs (moisture/sunlight/temperature/wind) and reuse baseline cadence.
- [x] Wire vegetation prefab pools (saplings, mature variants) to shared pooling config for deterministic warmup.

### 2. Moisture Grid System
- [ ] **Build MoistureGridBuildSystem**: Initialize grid from terrain/biome data at scene start.
- [ ] Implement hashed/rect grid over terrain (reuse spatial services indexing) storing soil moisture per cell.
- [ ] Initialize grid from biome + terrain type (e.g., sandy desert low moisture, wetlands high).
- [ ] **Create MoistureGridUpdateSystem** (runs every 10 ticks):
  - Evaporation/drying over time based on temperature/wind from ClimateGrid
  - Groundwater seep from nearby cells (diffusion algorithm)
  - Water body seepage (+2 units/s within 10m of water)
- [ ] **Integrate rain miracle** to add moisture to grid cells (+50 units per rain event).
- [ ] **Moisture sharing/competition**: Multiple plants in same cell compete for water (priority by entity index for determinism).
- [ ] Provide API for vegetation systems to sample moisture (cell lookup + interpolation).
- [ ] **Snapshot system for rewind**: Grid values snapshot every 30-60 ticks, restore during playback.
- [ ] Optimize using Burst jobs and double buffering; aim for 256×256 or higher resolution depending on world size.
- [ ] **Debug visualization**: Gizmos showing moisture heatmap overlay on terrain.

### 2.5. Plant Needs & Growth Modifiers (NEW)
- [ ] **Create VegetationNeedsSystem**: Consume water/nutrients per tick based on species consumption rates.
- [ ] **Create VegetationClimateQuerySystem**: Each plant reads local climate (temperature, wind) and moisture at its position from grids.
- [ ] **Create VegetationGrowthModifierSystem**: Calculate growth rate multipliers from needs:
  - WaterModifier = Water / 100
  - LightModifier = Light / 100  
  - TempModifier based on optimal temperature range curve
  - NutrientModifier = Nutrients / 100
  - SeasonModifier from current season (Spring 1.2x, Summer 1.0x, Autumn 0.7x, Winter 0.2x)
  - FinalGrowthRate = WaterModifier * LightModifier * TempModifier * NutrientModifier * SeasonModifier
- [ ] **Integrate with VegetationGrowthSystem**: Multiply growth progress by FinalGrowthRate.
- [ ] **Add need values to VegetationHistorySample**: Track Water, Light, Nutrients for rewind.
- [ ] **Death from depleted needs**: Plants with Water < 10 for extended period wither and eventually die (add to VegetationDecaySystem).

### 2.6. Sunlight & Shading (NEW)
- [ ] **Create SunlightEmitterSystem**: Process `SunlightEmitter` components (sun, miracles, torches) and accumulate light intensity into `SunlightGrid`.
- [ ] **Create SunlightGridUpdateSystem**:
  - Start with biome daylight baseline (time-of-day curve)
  - Add contributions from emitters via inverse-square falloff (clamped)
  - Apply canopy attenuation (trees reduce light for cells under their canopy)
  - Decay/normalize light values each tick for determinism
- [ ] **Create VegetationSunlightSystem**:
  - Lookup light value from `SunlightGrid` for each plant
  - Update `VegetationNeeds.Light` and track duration below threshold
  - Stall growth when Light < tolerance for >24 in-game hours; resume when light returns
- [ ] **Shade tolerance**: Configure species profiles with shade tolerance hours and recovery rates.
- [ ] **Debug visualization**: Overlay heatmap of light intensity and emitter gizmos.

### 3. Climate (Wind & Temperature)
- [ ] Create `ClimateState` singleton storing current wind direction/speed, ambient temperature per biome or global baseline.
- [ ] **Create ClimateGridUpdateSystem** (runs every 60 ticks):
  - Update temperature based on season, time-of-day, altitude
  - Local modifiers: proximity to water bodies (cooler), forest canopy (shade)
  - Update wind direction/strength from ClimateProfile with deterministic noise
- [ ] Update climate based on time-of-day/season curves from `ClimateProfile`; support fluctuations (noise) with deterministic seeds.
- [ ] Feed wind/temperature into `VegetationHealthSystem` (stress due to drought/frost) using species thresholds.
- [ ] Expose climate info to future systems (particle effects, creature behaviour).
- [ ] Tie rain/moisture evaporation to wind/temperature values.

### 4. Propagation & Saplings
- [ ] Extend `VegetationReproductionSystem` (or new `VegetationPropagationSystem`) to spawn saplings around mature/flowering plants:
  - Use species-specific propagation radius/density rules.
  - Check moisture grid, biome compatibility, crowding limits (avoid oversaturation).
  - Use deterministic random seeds per parent to ensure reproducible spreads.
- [ ] **Wind-affected seed dispersal**:
  - Parent tree queries spatial grid for empty cells within spread range
  - Wind direction biases seed placement (seeds travel downwind)
  - Seed travel distance = baseDistance + (windStrength * windMultiplier)
  - Deterministic placement: seed position derived from tick + parentID + seedIndex
- [ ] **Moisture filtering for seed placement**: Only spawn in cells with moisture > 30 (configurable per species).
- [ ] Integrate with `SceneSpawnSystem` / spawn command buffers to create new vegetation entities.
- [ ] Track sapling lifecycle: initial moisture requirement, survival chance based on environment.
- [ ] Provide hooks for designers to set propagation weights per biome.
- [ ] Use shared pooling utilities for sapling/entity spawn/despawn to avoid GC spikes.
- [ ] Ensure species behaviour (growth rates, propagation, death conditions) is entirely data-driven in `VegetationSpeciesCatalog`/biome profiles.

### 4.5. Vegetation Registry (NEW)
- [ ] **Create VegetationRegistry singleton** with buffer of VegetationRegistryEntry:
  - VegetationEntity, VegetationId, SpeciesIndex
  - Position, LifecycleStage
  - CanHarvest flag (ready for forester)
  - SpatialCellIndex, MoistureCell (cached for fast lookups)
  - AvailableYield (harvestable resources)
  - AverageHealth (Water+Light+Nutrients average)
- [ ] **Create VegetationRegistrySystem**: Updates registry entries each tick during record mode.
- [ ] **Integrate with forester jobs**: Villagers query registry for nearest harvestable trees (avoid linear scans).
- [ ] **Analytics support**: Registry provides total vegetation count, mature tree count, average health for HUD.

### 5. Integration with Existing Systems
- [ ] Update `VegetationHealthSystem` to read new moisture/climate data instead of static thresholds.
- [ ] Ensure `VegetationGrowthSystem` stage progression accounts for moisture sufficiency and climate stress.
- [ ] Align `RainCloudMoistureSystem` to update grid cells along with per-plant hydration.
- [ ] Update registries or analytics to include moisture/climate metrics (if needed).

### 6. Testing & Benchmarks
- [ ] Unit tests:
  - Moisture grid initialization, rain deposition, evaporation/diffusion (deterministic).
  - Climate state progression and application to species health.
  - Propagation density/spacing respecting species + biome constraints.
- [ ] Playmode tests:
  - Rain → moisture increase → health recovery.
  - Drought scenario causing stress/dying over time.
  - Propagation producing expected number of saplings with deterministic positions.
  - Rewind tests ensuring grid/climate/propagation state restores correctly.
- [ ] Performance benchmarks with large vegetation counts (100k entities) verifying moisture/climate updates stay within budget (<2 ms ideally).

### 7. Tooling & Observability
- [ ] Debug overlay for moisture grid (heatmap) and climate values (wind vectors, temperature readouts).
- [ ] Editor gizmos showing propagation radii and sapling spawn attempts.
- [ ] HUD or console commands to toggle drought/rain scenarios for testing.
- [ ] Logging of propagation events for balancing.

### 8. Documentation & Designer Workflow
- [ ] Update `Docs/DesignNotes/VegetationAssets.md` with new assets, grid, climate parameters.
- [ ] Create `Docs/Guides/VegetationAuthoring.md` or extend existing guides explaining moisture/climate setup and propagation tuning.
- [ ] Note future sunlight plan (placeholder section) so designers know it's pending.
- [ ] Record milestones and tuning notes in `Docs/Progress.md`.

## BW2 Parity Checklist
- [ ] Trees grow from seedlings to mature over time (visual progression)
- [ ] Mature trees spread seeds to nearby empty spaces (organic growth)
- [ ] Trees provide harvestable wood for foresters (resource value)
- [ ] Forests look natural and organic (not grid-aligned patterns)
- [ ] Trees near water grow better (moisture grid influence)
- [ ] Seasonal growth variation (Spring/Summer faster, Winter slower)
- [ ] Divine hand can instantly plant trees (spawn command integration)
- [ ] Harvested trees leave stumps, eventually regrow (lifecycle)
- [ ] Forest density self-regulates (doesn't overcrowd)
- [ ] Visual progression matches BW2 aesthetic feel

## Success Criteria

### BW2 Parity Achieved When:
- Forests grow and spread organically like BW2 (feel test by designer)
- Harvesting/regrowth cycle matches BW2 behavior
- Visual progression is clear and natural (seedling → mature → old)
- No noticeable lag with 50k+ vegetation entities

### Modern Enhancements Complete When:
- Plant needs system functional (Water, Light, Nutrients tracked and consumed)
- Moisture grid influences growth realistically (trees near water thrive)
- Climate affects plants as expected (temperature stress, seasonal variation)
- Wind affects seed dispersal (seeds travel downwind)
- Rewind works flawlessly with all grid systems (deterministic replay)
- Registry enables instant spatial queries (villagers find trees efficiently)

### Ready for Release When:
- All Workstreams 0-6 tasks complete
- Performance targets met (100k vegetation at 60+ FPS, <2ms grid updates)
- Determinism validated (rewind tests pass 100%)
- Configuration assets created for all vegetation species
- Designer documentation complete (authoring guides, tuning tips)
- Integration with villager forester jobs seamless (no bugs in harvesting)

## Open Questions
1. Should moisture grid cells share resolution with spatial grid cells (same 10m)?
2. Do we need explicit "irrigation" structures, or just proximity to water bodies?
3. Should temperature have day/night variation, or only seasonal changes?
4. How do we handle vegetation LOD at extreme distances (100k+ entities - skip updates)?
5. Should nutrients regenerate automatically, or require player/villager intervention (fertilization)?
6. Do we simulate soil types (clay, sand, loam) or just generic "nutrients"?
7. Should wind be constant or have gusts/storms (event-driven)?
8. How many plant species do we need at launch (trees, shrubs, grass, crops)?

## Future Work (Not in current scope)
### Planned for Later:
- **Crop farming**: Planted fields with irrigation, fertilization, harvest cycles.
- **Forest fires**: Fire spreads via wind, consumes vegetation, creates cleared areas.
- **Disease/parasites**: Spreads between nearby plants, requires treatment.
- **Fertilization**: Player/villager action boosts nutrients in soil.
- **Terraforming**: Divine hand modifies moisture/temperature of terrain.
- **Biome systems**: Desert, jungle, tundra with unique species and climate.
- **Animal interactions**: Creatures eat vegetation, spread seeds, trample paths.
- **Biome transitions**: Blending (edge smoothing) once terrain streaming pipeline ready.

### Architecture Hooks (Stub Now):
- Disease component (dormant, reserved enum values)
- Fire spread system interface (reserved in climate grid)
- Irrigation structure components (water source boost mechanics)
- Biome definition blobs (species suitability tables)

## Dependencies & Links
- Spatial Services (moisture grid indexing, propagation checks).
- Registry rewrite (if vegetation registry expands with moisture/climate info).
- Rain/cloud systems (already in hand TODO).
- Climate may share data with global weather systems (future).

## Implementation Phases & Time Estimates

### Phase 1: Plant Needs Foundation (1-2 weeks)
- Add VegetationNeeds component with consumption rates
- Implement VegetationNeedsSystem (consume water/nutrients per tick)
- Add VegetationGrowthModifierSystem (needs → growth multiplier)
- Integrate with existing VegetationGrowthSystem
- Add history samples for need values
- **Validation**: Plants grow slower with low water, die with zero needs

### Phase 2: Moisture Grid (2 weeks)
- Design moisture grid blob structure
- Create MoistureGridBuildSystem and MoistureGridUpdateSystem
- Add VegetationClimateQuerySystem (plants read moisture)
- Integrate rain miracle to add moisture
- Add debug visualization
- **Validation**: Plants near water have higher water levels, rain boosts moisture

### Phase 3: Climate System (1-2 weeks)
- Create ClimateGrid with temperature field
- Implement ClimateGridUpdateSystem (seasonal temperature, wind)
- Temperature affects growth rate
- Wind affects seed travel
- **Validation**: Plants grow faster in optimal temperature, seeds disperse downwind

### Phase 4: Sunlight & Shading (1-2 weeks)
- Implement SunlightEmitter components and SunlightGrid
- Build SunlightGridUpdateSystem (day/night curve + emitters + canopy attenuation)
- Update VegetationNeeds.Light based on grid values and tolerance hours
- Ensure growth stalls after 24h of insufficient light, resumes when restored
- **Validation**: Plants in shade stop growing, adding light source restores growth

### Phase 5: Registry & Spatial Integration (1 week)
- Create VegetationRegistry singleton
- Build VegetationRegistrySystem
- Update forester job assignment
- **Validation**: Villagers find nearest tree instantly, no linear scans

### Phase 6: Rewind & Determinism (1 week)
- Implement grid snapshot system
- Add need values to vegetation history
- Test record → rewind → catch-up cycles
- **Validation**: Rewind produces identical plant states and grid values

### Phase 7: Polish & Optimization (1-2 weeks)
- Optimize grid update frequency
- Burst-compile all vegetation systems
- Profile with 100k+ vegetation
- Add LOD for distant plants
- **Validation**: 100k vegetation runs at 60+ FPS with zero GC

### Phase 8: Configuration & Tooling (1 week)
- Create species profile assets
- Create climate profile assets
- Add in-editor grid visualization
- Build vegetation debug HUD
- **Validation**: Designers can create new species without code

**Total Estimated Time**: 9-12 weeks for complete BW2 parity + modern enhancements

## Next Steps & Order
1. **Complete recon** (Workstream 0) - 2-3 days
2. **Implement component & asset extensions** (Workstream 1) - 1 week
3. **Build plant needs system** (Workstream 2.5) - 1-2 weeks
4. **Build moisture grid foundation** (Workstream 2) - 2 weeks
5. **Layer climate updates** (Workstream 3) - 1-2 weeks
6. **Add sunlight & shading system** (Workstream 2.6) - 1-2 weeks
7. **Implement propagation system** (Workstream 4) - 1 week
8. **Build vegetation registry** (Workstream 4.5) - 1 week
9. **Update existing vegetation systems** (Workstream 5) - 1 week
10. **Add tests, benchmarking, tooling** (Workstreams 6 & 7) - 1-2 weeks
11. **Document and report progress** (Workstream 8) - 3-4 days

Maintain this TODO as tasks progress; capture tuning results, BW2 comparison notes, and open questions for future contributors.
