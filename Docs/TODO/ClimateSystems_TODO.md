# Climate Systems TODO (Wind, Temperature, Weather)

> **Generalisation Guideline**: Treat every climate effect as a data-defined behaviour so different games can hot-swap definitions (e.g., blizzard vs. magnetic storm) without code changes. Tasks below should remain genre-agnostic and reference shared environment effect definitions.

## Goal
- Build a robust climate simulation that provides meaningful wind direction, temperature, and weather states impacting gameplay (fire spread, projectiles, vegetation, terrain effects).
- Integrate climate data with existing DOTS systems (moisture grid, miracles, resource frameworks, terraforming) while remaining deterministic and scalable.
- Deliver designer-friendly controls for climate tuning and future expansion (seasons, storms, blizzards).
- Align with runtime expectations captured in `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` and `Docs/TODO/SystemIntegration_TODO.md`.

## Plain-Language Primer
- **Wind** should have direction and strength, affecting fire spread, projectile trajectories, tossed objects, and weather patterns.
- **Temperature** dictates environmental states: droughts, snow/ice, vegetation stress. Hot/dry areas encourage fire; cold/wet create snow or hinder movement.
- **Weather events** (rain, snow, blizzards) alter terrain overlays (snow layer), moisture distribution, and gameplay conditions.

## Climate Components Overview

### 1. Sunlight & Shadow System
- Directional light from sun (time-of-day rotation)
- Raycasting or heuristic to detect occlusion (canopy shade)
- Light intensity grid (0-100 per cell)
- Direct light vs. ambient light calculations

### 2. Moisture Grid
- 2D grid overlaying terrain (5m cell resolution recommended)
- Sources: rainfall, rivers, seepage, irrigation
- Sinks: evaporation, plant consumption, drainage
- Diffusion between adjacent cells (groundwater flow)

### 3. Temperature Field
- Base temperature from latitude/season
- Altitude modifier (-6.5°C per 1000m elevation)
- Local modifiers (water bodies -2°C, forest canopy -3°C)
- Diurnal variation (day/night cycle ±5°C)

### 4. Wind System
- Global wind direction (prevailing winds by latitude)
- Local wind strength variation (gusts, calm zones)
- Affected by terrain (wind shadow behind mountains)
- Influences rain cloud movement, fire spread, seed dispersal

### 5. Evaporation Dynamics
- Rate depends on temperature (exponential), wind (linear), humidity
- Higher temp + wind = faster evaporation
- Water bodies evaporate into atmospheric moisture
- Atmospheric moisture → rain probability

## Workstreams & Tasks

### 0. Recon & Dependency Mapping
- [ ] Inventory current climate-related data: `ClimateGrid`, moisture grid, vegetation thresholds, rain miracles.
- [ ] Review BW2 references for climate behaviour (wind-driven fire, snow, temperature effects).
- [ ] Identify systems needing climate hooks: vegetation, resource nodes, fire propagation (future), projectile physics, terraforming, villager movement.
- [ ] Determine data resolution (grid size vs. world size) and update cadence.

### 1. Data & Asset Model
- [ ] **Define comprehensive grid structures** in new or expanded components:
  - `SunlightGrid` singleton - BlobAssetReference, GridResolution (128x128), CellSize (10m), SunDirection, SunIntensity, LastUpdateTick
  - `SunlightGridBlob` - BlobArray of DirectLight (0-100), AmbientLight, OccluderCount
  - `MoistureGrid` singleton - BlobAssetReference, GridResolution (256x256), CellSize (5m), WorldMin/Max, LastUpdateTick
  - `MoistureGridBlob` - BlobArray of MoistureLevels, DrainageRates, TerrainHeight, LastRainTick, EvaporationRate
  - `TemperatureGrid` singleton - BlobAssetReference, GridResolution (64x64), CellSize (20m), BaseSeasonTemperature, TimeOfDayModifier, LastUpdateTick
  - `TemperatureGridBlob` - BlobArray of CellTemperatures (Celsius), Altitudes
  - `WindField` singleton - BlobAssetReference, GridResolution (32x32), CellSize (40m), GlobalWindDirection, GlobalWindStrength, LastUpdateTick
  - `WindFieldBlob` - BlobArray of LocalWind (float2 direction + strength per cell)
- [ ] **Define ClimateState singleton** (global environmental state):
  - CurrentSeason (0=Spring, 1=Summer, 2=Autumn, 3=Winter), SeasonProgress (0-1)
  - TimeOfDay (0-24 hours), DayNightProgress (0-1, 0=midnight, 0.5=noon)
  - GlobalTemperature, GlobalWindDirection, GlobalWindStrength
  - AtmosphericMoisture (0-100, affects rain chance), CloudCover (0-100, affects sunlight)
  - LastUpdateTick
- [ ] Define `ClimateProfile` ScriptableObject:
  - SeasonalTemperatures [4] (Spring, Summer, Autumn, Winter base temps)
  - DayNightTempSwing (±5°C typical)
  - GlobalWindDirection (prevailing wind angle in degrees)
  - GlobalWindStrength (5-15 m/s range)
  - AtmosphericMoistureBase (starting humidity 0-100)
  - EvaporationBaseRate (0.5 units/s default)
  - DaysPerSeason (30 days default)
  - HoursPerRealSecond (time scale: 0.5 = slow, 2.0 = fast)
  - Weather event probability curves (rain, snow, storms).
  - Biome-specific modifiers (desert, tundra, forest).
- [ ] **Add BiomeType enum** and biome determination thresholds:
  - Tundra (Cold+Dry), Taiga (Cold+Wet), Grassland (Temp+Dry), Forest (Temp+Wet), Desert (Hot+Dry), Rainforest (Hot+Wet), Savanna (Hot+Moderate), Swamp (Temp/Hot+VeryWet)
  - Temperature thresholds: Cold <10°C, Temperate 10-25°C, Hot >25°C
  - Moisture thresholds: Dry <40, Moderate 40-70, Wet >70, VeryWet >85
- [ ] Extend authoring/bakers to convert profiles and optional override volumes.
- [ ] Align system implementations with shared baseline environment grid jobs (moisture, temperature, wind, sunlight) defined in `EnvironmentSystemGroup`.
- [ ] Reference platform guidance (`PlatformPerformance_TruthSource.md`) when adding Burst/AOT-sensitive environment jobs and instrumentation.

### 1.5. Sunlight & Shadow System (NEW)
- [ ] **Define TimeOfDayState component**:
  - CurrentHour (0-24), HoursPerSecond (timescale), DayNumber (total days elapsed)
- [ ] **Implement SunlightGridUpdateSystem** (runs every 5 ticks):
  - Calculate sun position from time-of-day and season (elevation angle, azimuth)
  - **Option A - Raycasting**: Cast rays from grid cells toward sun (16k rays for 128x128)
    - Hit occluders (tall trees, buildings) → reduce light by shadow amount
    - Store DirectLight and AmbientLight per cell
  - **Option B - Heuristic** (more performant): Query spatial grid for tall vegetation count
    - Approximate: `light = 100 - (treeCount * 15)` clamped to 20-100
    - Skip raycasts, much faster
  - Update SunDirection vector and SunIntensity (0-1 based on time)
- [ ] **Sun arc calculations**:
  - Midnight (0h/24h): Below horizon (-90° elevation)
  - Sunrise (6h): Horizon (0° elevation)
  - Noon (12h): Zenith (60-90° varies by season - Summer high, Winter low)
  - Sunset (18h): Horizon (0° elevation)
  - Direction rotates 360° over 24h (east → south → west)
- [ ] **Seasonal variation**:
  - Summer: Higher arc (zenith 80-90°), longer days
  - Winter: Lower arc (zenith 40-50°), shorter days
  - Equinox (Spring/Autumn): Medium arc (zenith 65°), equal day/night
- [ ] **Vegetation integration**: Plants query sunlight grid for Light need value

### 2. Wind System
- [ ] **Implement WindFieldUpdateSystem** (runs every 120 ticks):
  - Calculate global wind from latitude and season (prevailing patterns)
  - Tropical (equator): Light variable (5 m/s)
  - Temperate (mid-lat): Westerly (10-15 m/s)
  - Polar (high-lat): Easterly (8-12 m/s)
  - Add deterministic noise for gusts (seed from tick)
  - Seasonal variation: ±30° direction shift, ±5 m/s strength variation
- [ ] **Calculate local wind variation** per cell:
  - Check upwind direction for obstacles (mountains, forests via spatial grid)
  - Wind shadow: Behind obstacles, reduce by 50-70%
  - Valleys: Channeled wind, increase by 20-30%
  - Open plains: Full wind strength
  - Forest: Reduce by tree density (10-40% reduction)
- [ ] Provide APIs for other systems:
  - `GetWindAtPosition(float3)` returning direction & magnitude.
  - Event hooks for wind changes (fire system, VFX).
- [ ] **Integrate with seed dispersal**: Wind direction biases seed placement (already in vegetation plan)
- [ ] **Integrate with rain clouds**: Clouds drift with wind (already implemented, verify)
- [ ] **Integrate with projectiles** (future):
  - Adjust ballistic paths for light objects (miracle projectiles, thrown resources).
- [ ] Plan for fire/particle integration (spread direction courtesy).

-### 2.5. Moisture Grid & Evaporation (NEW - Core System)
- [ ] **Implement MoistureGridBuildSystem**: Initialize grid from terrain/biome at scene start
- [x] **Implement MoistureEvaporationSystem** (runs every 10 ticks):
  - Multi-factor evaporation formula: `evapRate = baseRate * tempFactor * windFactor * humidityFactor * shadeFactor`
  - Temperature factor: `exp((temp - 20) * 0.05)` - exponential above 20°C
  - Wind factor: `1.0 + windSpeed * 0.1` - linear boost
  - Humidity factor: `1.0 - (atmosphericMoisture / 200)` - air saturation reduces evap
  - Shade factor: `sunlight / 100` - canopy reduces evaporation
  - Typical rates: Cold/no wind = 0.1 units/s, Temperate/calm = 0.5 units/s, Hot/breezy = 2.5 units/s
  - Update cell.EvaporationRate for debugging
  - Add evaporated water to AtmosphericMoisture (water cycle)
- [x] **Implement MoistureSeepageSystem** (runs every 10 ticks):
  - For each cell, check 4 neighbors (N, S, E, W)
  - Calculate moisture difference, transfer 10% of difference per tick (diffusion)
  - Water flows from high concentration to low (groundwater)
  - Deterministic cell order (row-major traversal)
- [ ] **Implement MoistureRainSystem**: Rain clouds/miracles add moisture to cells (integrate existing rain)
- [ ] **Water balance equation per cell**: ΔMoisture = Rainfall + Seepage_In - Evaporation - Plant_Consumption - Drainage - Seepage_Out

### 2.6. Data-Driven Environment Effects (NEW)
- [x] Author generic environment effect definitions (`EnvironmentEffectDefinition` via `EnvironmentEffectCatalog`) covering scalar fields, vector fields, and event pulses.
- [x] Ensure the runtime applies effect definitions via shared Burst jobs (no hard-coded “magnetic storm”/“blizzard” logic—effects differ by data only).
- [x] Provide sampling helpers returning both base values and effect contributions so downstream systems can react consistently (`EnvironmentSampling`).
- [ ] Extend authoring/UI polish so designers can add/replace effect definitions per project (catalog editor UX, validation, presets).

### 3. Temperature & Weather (Handled by generic effect pipeline)
- [x] **Implement TemperatureGridUpdateSystem** (runs every 60 ticks) *(covered by scalar effect definitions in catalog; see `EnvironmentEffectUpdateSystem`)*
- [x] **Implement ClimateStateUpdateSystem** (runs every tick) *(replaced by effect cadence + state singleton updates)*
- [x] Implement `WeatherEventSystem` *(generalised to event pulse effects; configure via catalog)*
- [x] Propagate weather effects to moisture grid *(handled through effect contributions and sampling helpers)*
- [x] Integrate with miracles (rain miracle modifies weather state) *(expose effect triggers via catalog/event pulses)*
- [x] Coordinate with `FixedStepSimulationSystemGroup` linkage to ensure climate updates respect deterministic tick lengths *(requires verifying new cadence sync system)*

### 3.5. Biome Determination System (NEW)
- [ ] **Implement BiomeDeterminationSystem** (runs every 60 ticks):
  - For each cell, read Temperature and Moisture values
  - Apply biome determination logic (temp + moisture → BiomeType)
  - Update cell BiomeType when thresholds crossed
  - Trigger ITerrainChangeListener.OnBiomeChanged for vegetation system
  - Store biome transitions in history for rewind
- [ ] **Biome transition thresholds** (exactly as specified in plan)
- [ ] **Integration with vegetation**: Vegetation system queries biome, adapts species selection

### 4. Snow & Terrain Overlays
- [ ] Create `SnowLayerSystem`:
  - Represent snow accumulation as additional mesh/texture layer (height offset or shader).
  - Snow depth increases with snowfall, decreases with higher temperature or rain.
- [ ] Moisture coupling: melting snow adds moisture to nearby cells.
- [ ] Provide data to vegetation/resource systems (movement slowdown, harvest difficulty).
- [ ] Visual integration with terrain/tile system.

### 5. Gameplay Interactions
- [ ] Fire propagation system:
  - Use wind direction to spread flame tokens (future).
  - Temperature/humidity thresholds control ignite chance.
- [ ] Projectile impacts:
  - Light projectiles deflected by wind (e.g., miracle spells).
- [ ] Villager & creature movement:
  - Snow depth slows movement.
  - Heat/cold stress affects needs (hook into villager systems).
- [ ] Resource nodes:
  - Ore purity decay rate influenced by temperature/wetness (from resource TODO).
- [ ] Terraforming:
  - Terrain heating/cooling operations feed into climate grid updates.

### 6. Tooling & Designer Workflow
- [ ] Climate debug overlay (wind vectors, temperature heatmap, snow depth, weather flags).
- [ ] Editor inspector for profiles with preview graphs.
- [ ] In-game debug controls (force wind direction, trigger storm, adjust temperature).
- [ ] Document pipeline for adding new biomes/climate behaviours.

### 7. Testing & Performance
- [ ] **Unit tests** in `ClimateSystemTests.cs`:
  - Sun position calculation (time + season → correct direction vector and elevation)
  - Temperature calculation (all inputs → correct cell temperature)
  - Evaporation formula (temp + wind + humidity + shade → correct rate)
  - Seepage diffusion (moisture balances correctly between cells)
  - Biome determination (temp + moisture → correct BiomeType)
  - Wind local variation (obstacles reduce wind correctly)
- [ ] **Playmode tests**:
  - Full day/night cycle (24 hours simulated at accelerated time)
  - Seasonal transition (Spring → Summer → Autumn → Winter with temp changes)
  - Rain adds moisture → evaporates over time → reaches equilibrium
  - Temperature affects plant growth visibly (hot = faster, cold = slower)
  - Wind affects seed dispersal patterns (seeds travel downwind)
  - Biome changes when climate shifts (desert → grassland with added moisture)
  - Sunlight grid shadows update as sun moves across sky
- [ ] **Determinism tests** in `ClimateRewindTests.cs`:
  - Climate state identical after rewind (time, season, global values)
  - Moisture grid values match exactly on replay
  - Temperature calculations reproducible (same inputs → same outputs)
  - Wind patterns deterministic (same seed → same wind gusts)
  - Biome transitions replay in same sequence
- [ ] **Performance benchmarks**:
  - All climate grids updating simultaneously: <2ms per frame target
  - 100k vegetation querying climate data: <3ms
  - Sunlight raycasts (16k rays) OR heuristic (128x128 cells): <1ms (throttled to every 5 ticks)
  - Moisture evaporation + seepage (256x256 cells): <1ms (throttled to every 10 ticks)
  - Temperature update (64x64 cells): <0.3ms (throttled to every 60 ticks)
  - Wind update (32x32 cells): <0.1ms (throttled to every 120 ticks)
  - Zero GC allocations per frame (Burst-compiled jobs)

## System Update Flow (Per-Tick Order)

**Update Frequencies** (Performance Optimization):
- Climate State: Every tick (time progression)
- Sunlight Grid: Every 5 ticks (light changes slowly)
- Moisture Grid: Every 10 ticks (evaporation, seepage, rain)
- Temperature Grid: Every 60 ticks (temperature stable)
- Wind Field: Every 120 ticks (wind changes very slowly)
- Biome Determination: Every 60 ticks (biome stable)

**System Execution Order** (each update tick):
1. `ClimateStateUpdateSystem` - Update time-of-day, season, global climate values
2. `SunlightGridUpdateSystem` - Calculate sun direction, cast shadows or use heuristic (every 5 ticks)
3. `TemperatureGridUpdateSystem` - Update temperature field with all modifiers (every 60 ticks)
4. `WindFieldUpdateSystem` - Update global + local wind vectors (every 120 ticks)
5. `MoistureEvaporationSystem` - Remove water based on temp + wind + shade (every 10 ticks)
6. `MoistureSeepageSystem` - Diffuse water between cells (every 10 ticks)
7. `MoistureRainSystem` - Add rain from clouds/miracles (every tick)
8. `BiomeDeterminationSystem` - Update biome type from temp + moisture (every 60 ticks)

All systems respect `RewindState.Mode` and skip updates during playback.

## Grid Resolutions & Performance Targets

### Recommended Resolutions
```
SunlightGrid: 128x128 cells = 10m resolution (balance accuracy vs. raycast cost)
MoistureGrid: 256x256 cells = 5m resolution (fine detail for water dynamics)
TemperatureGrid: 64x64 cells = 20m resolution (temperature changes slowly)
WindField: 32x32 cells = 40m resolution (wind very coarse, naturally smooth)
```

### Performance Budget
```
Per-Frame Cost (All Climate Systems):
- ClimateState update: 0.05ms (every tick)
- Sunlight grid: 0.5ms (amortized, every 5 ticks)
- Moisture grid: 1.0ms (amortized, every 10 ticks)
- Temperature grid: 0.3ms (amortized, every 60 ticks)
- Wind field: 0.1ms (amortized, every 120 ticks)

Total: ~1.5-2ms per frame (acceptable, <5% of 16ms budget at 60 FPS)
```

### Memory Budget
```
Grid Storage:
- SunlightGrid (128x128): 16k floats × 3 fields = 192 KB
- MoistureGrid (256x256): 65k floats × 5 fields = 1.3 MB
- TemperatureGrid (64x64): 4k floats × 2 fields = 32 KB
- WindField (32x32): 1k float2 = 8 KB

Total Climate Grids: ~1.5 MB (very acceptable)
```

### Burst Compilation Requirements
All grid update jobs MUST be Burst-compiled:
- MoistureEvaporationJob: IJobEntity for parallel processing
- MoistureSeepageJob: IJobParallelFor over cell range
- TemperatureUpdateJob: IJobParallelFor with altitude lookups
- WindUpdateJob: IJobParallelFor with terrain obstruction checks

### 4. Snow & Terrain Overlays
- [ ] Create `SnowLayerSystem`:
  - Represent snow accumulation as additional mesh/texture layer (height offset or shader).
  - Snow depth increases with snowfall, decreases with higher temperature or rain.
- [ ] Moisture coupling: melting snow adds moisture to nearby cells.
- [ ] Provide data to vegetation/resource systems (movement slowdown, harvest difficulty).
- [ ] Visual integration with terrain/tile system.

## Rewind Compatibility

### Grid Snapshot Strategy
**Snapshot grids at different frequencies** based on change rate:
- **Sunlight**: Derived from time-of-day (no snapshot needed, recalculate from ClimateState)
- **Moisture**: Snapshot every 30 ticks (water changes frequently)
- **Temperature**: Snapshot every 120 ticks (changes slowly)
- **Wind**: Snapshot every 240 ticks (very stable, rarely changes)

### Playback Mode Behavior
- Restore ClimateState (TimeOfDay, CurrentSeason, global values)
- Restore MoistureGrid from most recent snapshot
- Recalculate TemperatureGrid from time + season + altitude
- Recalculate WindField from season + deterministic seed
- Recalculate SunlightGrid from time-of-day

### Determinism Requirements
- All calculations use fixed-point math or careful float clamping (prevent drift)
- Grid updates process cells in deterministic order (row-major traversal)
- Random elements (wind gusts, weather) use seed derived from tick (reproducible)
- Evaporation/seepage use stable formulas (same inputs → same outputs always)

## Implementation Phases & Time Estimates

### Phase 1: Climate State & Time (1 week)
- Implement ClimateState singleton with all fields
- Add time-of-day progression (CurrentHour wraps at 24, increments DayNumber)
- Add seasonal cycle (DayNumber / DaysPerSeason mod 4)
- Test time advancement, rewind time restoration
**Validation**: Time progresses smoothly, seasons change correctly, rewind restores time

### Phase 2: Temperature Grid (1-2 weeks)
- Build TemperatureGrid and blob structure
- Implement TemperatureGridUpdateSystem with all modifiers (season, latitude, altitude, time, water, forest)
- Test temperature variations across map and over time
**Validation**: Temperature realistic, varies by location/time/season as expected

### Phase 3: Moisture Grid Core (2 weeks)
- Implement MoistureGrid (coordinate with vegetation plan to avoid duplication)
- Add rain input from miracles
- Implement MoistureEvaporationSystem (multi-factor formula)
- Implement MoistureSeepageSystem (diffusion between cells)
- Test water balance (rain → evaporate → diffuse → equilibrium)
**Validation**: Moisture accumulates from rain, evaporates realistically, diffuses to neighbors

### Phase 4: Wind Field (1 week)
- Implement WindField grid structure
- Add WindFieldUpdateSystem with global patterns (latitude-based prevailing winds)
- Add terrain-based local variation (obstacles create wind shadows)
- Test seed dispersal integration with vegetation
**Validation**: Wind direction consistent, local variations make sense, affects gameplay

### Phase 5: Sunlight Grid & Raycasting (2-3 weeks)
- Implement SunlightGrid structure
- Add sun position calculation from time-of-day and season
- Implement SunlightGridUpdateSystem (choose raycast OR heuristic approach)
- Optimize raycast frequency (every 5-10 ticks, not every tick)
- Test vegetation light response (plants in shade grow slower/die)
**Validation**: Shadows cast correctly (or approximate well), plants respond to light levels

### Phase 6: Biome Determination (1 week)
- Add BiomeType field to climate cells or separate grid
- Implement BiomeDeterminationSystem (temp + moisture → biome logic)
- Test biome transitions (desert → grassland when moisture added)
- Integrate with vegetation species selection (biome affects which plants spawn)
**Validation**: Biomes determine correctly, vegetation adapts to biome changes

### Phase 7: Integration & Polish (2 weeks)
- Full integration with vegetation system (all climate factors affecting growth)
- Performance optimization (Burst compilation, update frequency tuning)
- Debug visualization (grid overlays: temp heatmap, moisture, wind vectors, sunlight)
- Comprehensive rewind testing (all grids restore correctly)
**Validation**: All climate systems work together harmoniously, deterministic replay perfect

**Total Estimated Time**: 10-14 weeks for complete climate system

## Success Criteria

**System Complete When**:
- All 7 implementation phases done and tested
- Performance targets met (<2ms climate per frame, <5ms total with vegetation queries)
- Determinism validated (100% rewind accuracy on all tests)
- Vegetation fully integrated (responds correctly to all climate factors: light, water, temp, wind)
- Debug visualization tools working (designers can see climate data)
- Designer can tune ClimateProfile without code changes
- Zero GC allocations per frame verified

## Open Questions

1. How detailed should the climate grid be? (Per 5m cell? 10m? Already recommended above)
2. Do we need multiple vertical layers (surface vs. altitude) for wind/temperature?
3. Should wind have gust events or smooth transitions only (gusts = noise-based)?
4. How do we balance dynamic weather vs. designer-controlled scenarios?
5. Should climate consume prayer/resources (weather miracles altering climate permanently)?
6. How do we handle extreme events (volcano, hurricane) affecting climate over time?
7. Do we need to track humidity separately from moisture grid (or is AtmosphericMoisture sufficient)?
8. How should climate interact with global alignment (good deity brings rain, evil brings drought)?
9. How will we expose climate info to AI (villagers prepare for storm, take shelter)?
10. Should sunlight use raycast (accurate) or heuristic (fast) - performance vs. accuracy trade-off?
11. Do we simulate cloud shadows separately from terrain/tree shadows?
12. Should time-of-day progression pause during certain game states?
13. How many days per year (seasons × DaysPerSeason)? 120 days total (30 per season)?
14. Should extreme weather (blizzards, hurricanes) be miracle-driven or automatic?

## Dependencies & Links

- **Spatial grid** (see `SpatialServices_TODO.md`) - Y strata for sunlight raycasts, obstacle queries for wind shadows
- **Vegetation systems** (see `VegetationSystems_TODO.md`) - consumes all climate data (light, water, temp), plant needs
- **Terraforming** (see `TerraformingPrototype_TODO.md`) - terrain height affects temperature (altitude), moisture flow
- **Miracles framework** (see `MiraclesFramework_TODO.md`) - rain adds moisture, fire affects temperature, ice cools area
- **Time system** - tick-based updates, TimeState.FixedDeltaTime for all calculations
- **Rewind system** - snapshot/restore grids deterministically, ClimateState history
- **Resource systems** (see `ResourcesFramework_TODO.md`) - node degradation from climate
- **Villager systems** (see `VillagerSystems_TODO.md`) - movement affected by snow, temperature stress
- **Presentation/VFX** - visual overlays, weather effects, terrain materials

## Next Steps & Roadmap

1. **Finalize climate goals** with design team (wind/temperature behaviors, weather palette, time scale)
2. **Draft ClimateArchitecture.md** design note detailing:
   - Complete data layout (all grid structures)
   - System update order and frequencies
   - Performance optimization strategies
   - Integration points with other systems
3. **Phase 1**: Implement Climate State & time progression (1 week)
4. **Phase 2**: Build temperature grid (1-2 weeks)
5. **Phase 3**: Implement moisture grid with evaporation/seepage (2 weeks)
6. **Phase 4**: Add wind field (1 week)
7. **Phase 5**: Implement sunlight grid (2-3 weeks, choose raycast vs. heuristic)
8. **Phase 6**: Add biome determination (1 week)
9. **Phase 7**: Integration, optimization, testing (2 weeks)
10. **Total**: 10-14 weeks

Use this TODO to capture ongoing research, decisions, and integration tasks. Update as each phase completes. Track performance measurements and design decisions for future reference.
