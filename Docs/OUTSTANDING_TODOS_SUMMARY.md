# Outstanding PureDOTS TODOs Summary

_Generated: 2025-01-27_

This document summarizes all outstanding TODO items found in PureDOTS code and documentation.

## Code TODOs (Actual TODO Comments)

No outstanding code TODO comments in source code. Previously flagged items (spawn stat overrides, spawn overlap/navmesh validation, input recording toggle) have been implemented.

## Documentation TODOs by Category

### High Priority (Blocking or Critical)

#### 1. Compilation Health Issues (`PureDOTS_TODO.md` section 10)
- NetCode 1.8 physics regression with `PhysicsWorldHistory.Clone` overloads (needs shim/patch) - netplay is final priority, left only for the end of the project
- Missing generic comparer surface in `StreamingLoaderSystem` (may already be resolved - needs verification)

#### 1b. Space4X Framework Requests (`Docs/TODO/Space4X_Frameworks_TODO.md`)
- New Space4X backlog pulled from `Space4x/Docs/TODO/4xdotsrequest.md`: alignment/compliance, modules + degradation/repairs, mining deposits/harvest nodes, waypoints/infrastructure, supply–demand economy, spoilage FIFO, tech diffusion, interception, crew progression/breeding (deferred), telemetry/tests. Needs scheduling into active sprint tracking.

#### 2. Presentation Bridge Testing (`PresentationBridge_TODO.md`)
- Validation tests missing (rewind-safe presentation tests)
- Sample authoring guide missing (`Docs/Guides/Authoring/`)

#### 3. System Integration (`SystemIntegration_TODO.md`)
- [ ] Add integration tests covering: hand holding resource + miracle token (ensure deterministic resolver), hand dumps to storehouse after miracle charge, etc.
- [ ] Centralise gesture & siphon feedback events within `HandPresentationBridge` so VFX/audio subscribe to single stream
- [ ] Update existing TODOs to reference terraforming contract rather than each defining bespoke hooks
- [ ] Configure automated nightly run that executes performance suites for spatial grid, environment, villagers, miracles, resources concurrently
- [ ] Log integration metrics to shared dashboard (e.g., Entities count, grid update times, input latency)
- [ ] Update each subsystem TODO with cross-links to the integration doc and relevant tasks
- [ ] Host alignment review with leads from gameplay, simulation, presentation to sign off shared contracts
- [ ] Audit runtime for IL2CPP/AOT safety (no reflection in jobs, provide type registration helpers, add `[Preserve]` where needed)
- [ ] Add IL2CPP build configuration checklist (linker settings, `link.xml`, scripting backend) and automate a test build in CI
- [ ] Instrument job scheduling logs (optional) to verify worker thread utilisation
- [ ] Define reusable AI behaviour modules (sensors, scoring, steering, task selectors) that consume shared data
- [ ] Add regression checks (lint/tests) ensuring shared layer naming stays neutral when new modules land
- [ ] Define meta-system slices (e.g., `Runtime Core`, `Data Authoring`, `Tooling/Telemetry`, `QA/Validation`) and record owners + contact cadence
- [ ] Tag existing TODO tasks with their slice responsibility
- [ ] Establish lightweight governance process: monthly review per slice
- [ ] Publish onboarding notes clarifying which slice new features belong to
- [ ] Capture alignment between slices and CI/testing responsibilities

### Medium Priority (Important but Not Blocking)

#### 4. Registry Rewrite (`RegistryRewrite_TODO.md`)
- [ ] Resource gathering/storehouse systems: switch lookups to the new registries instead of ad-hoc queries
- [ ] Villager job assignment: consume registry instead of scanning every frame; share reservation data via registry entries
- [ ] Rain miracles/divine hand: use registries to quickly find nearest valid targets once spatial grid is ready
- [x] LogisticsRequest and Miracle registries now cache `CellId`/`SpatialVersion` via `SpatialGridResidency`, unblocking downstream continuity/rewind suites (divine hand targeting still pending)
- [ ] Remove or obsolete any legacy service singletons/MonoBehaviours that duplicate registry responsibilities
- [ ] Stand up generic consumption samples/tests so downstream games can plug domain-specific logic into PureDOTS registries without modifying template code
- [ ] Add inspector gizmos or debug HUD panels so designers can view registry contents at runtime (counts, capacities)
- [ ] Extend authoring docs (`SceneSetup`, `DesignNotes`) with "how to configure registries"
- [ ] Unit tests for each registry update system (spawn, despawn, reorder, rewind)
- [ ] Playmode tests ensuring villagers/resources behave identically before/after migration
- [ ] Playmode `RegistryContinuity` suite covering villager, miracle, transport, and logistics request registries (spatial sync + rewind)
- [ ] Stress tests with 50k entities verifying no GC allocations and acceptable frame time
- [ ] Regression checks for deterministic ordering (sorted indexes stable between runs)

#### 5. Spatial Services (`SpatialServices_TODO.md`)
- [ ] Expand deterministic query utilities: `kNN`, multi-radius batches, filtered entity iterators, and jobified wrappers
- [ ] Provide data-driven query descriptors so different game concepts can reuse the same query pipeline without custom code
- [ ] Integrate registries (villager/logistics/miracles) with spatial indexing metadata for fast lookup once entries store spatial tokens
- [ ] Support 2D (XZ plane) navigation out of the box **and** define config/runtime hooks for true 3D layers (int3 cells, volume cost fields)
- [ ] Stub hierarchical grid interface (two-level: macro cell -> micro grid)
- [ ] Plan GPU offload hook (compute shader or Entities Graphics) for extreme densities
- [ ] Reserve data slots for additional attributes (cell occupancy heatmaps, average normals)
- [ ] Formalise layer provider abstraction (`INavLayerProvider`) so 2D/3D navigation layers can swap implementations
- [ ] Integrate miner/hauler logistics: `VesselRoutingSystem`, freighter/wagon dispatch leverage spatial queries + registries
- [ ] Integrate into divine hand/miracles: `DivineHandSystems` use grid for hover highlighting and target selection; `RainMiracleSystems` query terrain/vegetation density before spawning effects
- [ ] Expose generic query helpers for any future game-specific systems (VR input, RTS selection, etc.) using shared descriptors
- [ ] Preserve existing behaviour: Ensure all integrations produce identical results (same entity selections) to validate correctness before optimizing
- [ ] Debug authoring: scene gizmo drawer showing grid bounds/cell occupancy
- [ ] Runtime overlay (Entities Graphics or UI) toggled via debug menu
- [ ] Logging hooks to sample cell density, worst-case query cost
- [ ] Editor validation to warn when config bounds too small/large
- [ ] Unit tests in `Assets/Scripts/Tests/SpatialGridTests.cs` (Morton/Z-curve hash, cell assignment, radius query accuracy, deterministic sorting)
- [ ] Integrate with `EnvironmentSystemGroup` cadence (consume shared environment grid updates once baseline jobs are in place)
- [ ] Verify `SpatialGrid` rebuild honours `TerrainVersion` increments broadcast by terraforming hooks
- [ ] Rewind determinism tests (record 100 ticks → rewind to tick 50 → verify grid state matches)
- [ ] Performance benchmarks (100k → 1M synthetic entities measuring rebuild time per tick)
- [x] Profiling automation: Integrate with performance harness TODO to track spatial grid metrics in CI *(Playmode `SpatialInstrumentationSystem_EmitsMetricsMatchingGridState` test now validates logged rebuild health before registry expansion.)*

#### 6. Climate Systems (`ClimateSystems_TODO.md`)
- [ ] Inventory current climate-related data: `ClimateGrid`, moisture grid, vegetation thresholds, rain miracles
- [ ] Review BW2 references for climate behaviour (wind-driven fire, snow, temperature effects)
- [ ] Identify systems needing climate hooks: vegetation, resource nodes, fire propagation (future), projectile physics, terraforming, villager movement
- [ ] Determine data resolution (grid size vs. world size) and update cadence
- [ ] Define comprehensive grid structures (SunlightGrid, MoistureGrid, TemperatureGrid, WindField) - **Note: Many are already implemented**
- [ ] Define `ClimateProfile` ScriptableObject (many fields already exist but may need expansion)
- [ ] Add BiomeType enum and biome determination thresholds - **Note: BiomeDeterminationSystem exists but may need completion**
- [ ] Implement `BiomeDeterminationSystem` (runs every 60 ticks) - **Status: Stubbed but needs completion**
- [ ] Sun arc calculations (midnight, sunrise, noon, sunset, seasonal variation)
- [ ] Implement `WindFieldUpdateSystem` (runs every 120 ticks)
- [ ] Calculate local wind variation per cell (wind shadow, valleys, forests)
- [ ] Provide APIs for other systems: `GetWindAtPosition(float3)` returning direction & magnitude
- [ ] Integrate with seed dispersal: Wind direction biases seed placement
- [ ] Integrate with rain clouds: Clouds drift with wind
- [ ] Integrate with projectiles (future): Adjust ballistic paths for light objects
- [ ] Plan for fire/particle integration (spread direction courtesy)
- [ ] Water balance equation per cell: ΔMoisture = Rainfall + Seepage_In - Evaporation - Plant_Consumption - Drainage - Seepage_Out
- [ ] Extend authoring/UI polish so designers can add/replace effect definitions per project (catalog editor UX, validation, presets)
- [ ] Create `SnowLayerSystem`: Represent snow accumulation as additional mesh/texture layer
- [ ] Moisture coupling: melting snow adds moisture to nearby cells
- [ ] Provide data to vegetation/resource systems (movement slowdown, harvest difficulty)
- [ ] Visual integration with terrain/tile system
- [ ] Fire propagation system: Use wind direction to spread flame tokens (future)
- [ ] Projectile impacts: Light projectiles deflected by wind (e.g., miracle spells)
- [ ] Villager & creature movement: Snow depth slows movement; Heat/cold stress affects needs
- [ ] Resource nodes: Ore purity decay rate influenced by temperature/wetness
- [ ] Terraforming: Terrain heating/cooling operations feed into climate grid updates
- [ ] Climate debug overlay (wind vectors, temperature heatmap, snow depth, weather flags)
- [ ] Editor inspector for profiles with preview graphs
- [ ] In-game debug controls (force wind direction, trigger storm, adjust temperature)
- [ ] Document pipeline for adding new biomes/climate behaviours
- [ ] Unit tests in `ClimateSystemTests.cs` (sun position, temperature, evaporation, seepage, biome determination, wind local variation)
- [ ] Playmode tests (full day/night cycle, seasonal transition, rain adds moisture, temperature affects plant growth, wind affects seed dispersal, biome changes, sunlight grid shadows)
- [ ] Determinism tests in `ClimateRewindTests.cs` (climate state identical after rewind, moisture grid values match, temperature calculations reproducible, wind patterns deterministic, biome transitions replay)
- [ ] Performance benchmarks (all climate grids updating simultaneously: <2ms per frame target)

#### 7. Villager Systems (`VillagerSystems_TODO.md`)
- [ ] Audit current villager systems for data layout, performance, and determinism gaps
- [ ] Document legacy truth-source expectations: `VillagerTruth.md`, `Villagers_Jobs.md`, `VillagerState.md`, RMBS truth on player priority
- [ ] Profile existing loops to identify hotspots (job assignment, pathing, inventory updates)
- [ ] Catalogue future features: alignment shifts, armies, creature training, prayer/tribute contributions
- [ ] Review rewinding behaviour: confirm current systems respect `RewindState` and record necessary history
- [ ] Build `VillagerArchetypeCatalog` ScriptableObject + blob with base stats
- [ ] Hook `VillagerAISystem` to archetype catalog data and replace stub job behaviors (`Gather/Build/Craft/CombatJobBehavior`) with real scoring/execution; current smoke tests only validate scaffolding compiles.
- [ ] Define `JobDefinitionCatalog` with job durations, resource costs, rewards, skill requirements
- [ ] Define `VillagerNeedCurve` assets (AnimationCurve -> blob) for hunger/energy/mood thresholds
- [ ] Extend authoring/baker to convert new assets into runtime data
- [ ] Add tags/flags for special roles (soldier, craftsman, priest) to support future modules
- [ ] Configure reusable AI behaviour modules (sensor, scoring, steering, task selection) through archetype-specific data and marker components
- [ ] Document how other entity types (ships, drones, NPCs) plug into the same AI modules via configuration
- [ ] **SoA Refactor Phase 1**: Replace `VillagerNeeds` floats with `short`/`ushort` if precision allows
- [ ] **SoA Refactor Phase 1**: Consolidate tags into packed `VillagerFlags` component
- [ ] **SoA Refactor Phase 1**: Move `VillagerMood` to cold archetype if not consumed every tick
- [ ] **SoA Refactor Phase 2**: Split inventory buffer to companion entity
- [ ] **SoA Refactor Phase 2**: Move `VillagerStats`, `VillagerAnimationState` to companion entity
- [ ] **SoA Refactor Phase 2**: Move `VillagerMemoryEvent` buffer to companion entity
- [ ] **SoA Refactor Phase 3**: Rewrite `VillagerNeedsSystem` as Burst job with SoA data
- [ ] **SoA Refactor Phase 3**: Introduce `VillagerMoodSystem` computing morale, alignment shift
- [ ] **SoA Refactor Phase 3**: Rework `VillagerJobAssignmentSystem` to use spatial grid + registries
- [ ] **SoA Refactor Phase 3**: Refactor `VillagerJobExecutionSystem` to support modular job behaviours
- [ ] **SoA Refactor Phase 3**: Implement `VillagerCommandSystem` to process player/creature commands
- [ ] **SoA Refactor Phase 3**: Integrate `VillagerHistorySystem` for deterministic logging
- [ ] **Implement `VillagerLocalSteeringSystem`** for obstacle avoidance (Reynolds steering, static obstacle detection)
- [ ] Create job/need priority scheduler (e.g., utility score + cooldown) per villager archetype
- [ ] Implement shift schedules (day/night) using `TimeOfDay` service
- [ ] Provide fallback behaviours (idle, roam, worship) when needs are satisfied
- [ ] Hook into prayers/tribute economy (increase alignment, expand influence ring)
- [ ] Define shared AI behaviour modules (sensing, scoring, steering, task resolution) that other projects can reuse
- [ ] Document patterns for combining generic AI systems with specialised behaviour via marker components
- [ ] Rebuild `VillagerInventory` buffer to track multiple resource slots, weight/capacity, and reserved tickets
- [ ] Ensure inventory interacts with resource/storehouse registries via pooled commands
- [ ] Add deterministic consumption (food from storehouse) tied to needs; integrate with resource economy
- [ ] Prepare extension for army equipment (weapons, armor) later
- [ ] Implement alignment state derived from actions (helpful vs. aggressive) and creature influence
- [ ] Add morale effects (panic, productivity modifiers) based on needs, alignment, environment events
- [ ] Feed alignment/morale metrics into time/tribute, city attractiveness
- [ ] Provide API for presentation (UI badges) and for AI to react to morale shifts
- [ ] Define interrupt pipeline: player hand pickup, miracles, disasters, combat threats
- [ ] Coordinate with RMB router to ensure villager actions yield to higher priority
- [ ] Support graceful interruption/resume of jobs with deterministic state restore
- [ ] Integrate with `SceneSpawnSystem` for dynamic villager spawning by role
- [ ] Abstract path requests so villager systems can plug into future nav solutions
- [ ] Provide hook for spatial grid / nav service to return path targets and avoid collisions
- [ ] Prepare for crowd-simulation improvements (flocking, lane formation) later
- [ ] **Implement Flow Field Pathfinding** for scalability to 100k+ agents (detailed breakdown in TODO)
- [ ] **Ensure pathfinding rewind compatibility** (deterministic generation, skip during playback, rebuild during catch-up)
- [ ] Unit tests for needs decay, job assignment correctness, inventory transfers, alignment changes
- [ ] Playmode tests verifying gather-deliver loop, alignment shifts, scheduled behaviours, rewind parity
- [ ] Stress tests with 100k, 500k, 1M villagers measuring frame time per system and verifying zero GC allocations
- [ ] Determinism tests: two runs, identical commands -> same villager states, job history, inventory totals
- [ ] Integration tests with registries/spatial grid ensuring combined workload stays under target budgets
- [ ] **Spatial sensor and pathfinding tests** in `VillagerSensorTests.cs` and `FlowFieldTests.cs`
- [ ] **Pathfinding rewind determinism tests** in `SpatialPathfindingRewindTests.cs`
- [ ] **Performance targets by agent count** (10k, 50k, 100k, 1M villagers)
- [ ] Measure and validate query performance vs. linear scans to quantify speedup
- [ ] Extend debug HUD to display villager counts per state/job/alignment, average needs, morale ranges
- [ ] Add timeline overlays showing job events and need spikes (ties into history system)
- [ ] Provide in-editor inspector for per-system metrics (e.g., `VillagerJobInspector` to list current assignments)
- [ ] Hook into analytics/logging to export simulation snapshots for balancing
- [ ] **Add pathfinding debug visualization** (scene gizmo drawer, runtime overlay, visual indicators, flow field layer selector)
- [ ] Update `Docs/DesignNotes/VillagerJobs_DOTS.md` with new architecture, assets, tuning guidelines
- [ ] Create `Docs/Guides/VillagerAuthoring.md` explaining how to author archetypes, job definitions, schedules
- [ ] Document integration points with other systems (registry, spatial, hand/miracles) so designers know dependencies
- [ ] **Add pathfinding documentation** in `Docs/DesignNotes/FlowFieldPathfinding.md`

#### 8. Resources Framework (`ResourcesFramework_TODO.md`)
- [ ] Document resource types needed (wood, ore, food, animal, miracle tokens?) and expected behaviours
- [ ] Inventory existing assets (resource models, pile visuals) and note missing ones
- [ ] Confirm rewind requirements (resource history, pile states) and identify components needing history logging
- [ ] Create `ResourceProfile` ScriptableObject (resource type id, display name, alignment impact, chunk mass, size, default stack amount, aggregate max per pile, tumble physics settings, storehouse conversion value, visual references)
- [ ] Build `ResourceProfileBlob` for runtime lookup (Burst-friendly)
- [ ] **Define enhanced chunk components** in `ResourceComponents.cs` (ResourceChunkPhysics, HandPickupableChunk)
- [ ] **Define pile components** (ResourcePile, ResourcePileMergeCandidate buffer, HandSiphonable)
- [ ] **Define siphon/hand state** (ResourceSiphonState on divine hand)
- [ ] **Define pile registry** (ResourcePileRegistry singleton, ResourcePileEntry buffer)
- [ ] Update bakers to convert profiles and authoring components into runtime data
- [ ] Catalogue node archetypes and behaviour rules (Tree/Wood Nodes, Food Bush/Crop Nodes, Ore/Mine Nodes)
- [ ] Define `ResourceNode` component storing NodeType, CurrentPurity, MaxPurity, RegenTimer, LastHarvestTick
- [ ] Update gathering systems to read node state, adjust chunk output (units, quality) accordingly
- [ ] Hook node regeneration into climate/biome events (seasonal triggers, miracles)
- [ ] Leave extension points for future metals/minerals (rare nodes, deep mining, explosives)
- [ ] **Add ResourceChunkPhysics component** with mass, friction, bounciness per resource type
- [ ] **Implement ResourceChunkPhysicsSystem** (gravity, friction, air resistance, collision with terrain, bounciness, deterministic velocity)
- [ ] **Implement ResourceChunkSettleDetectionSystem** (monitor velocity, increment SettleTimer, mark for pile conversion)
- [ ] **Implement ResourceChunkToPileConversionSystem** (query settled chunks, check spatial grid for nearby piles, merge or create new pile)
- [ ] Ensure deterministic random seeds for tumble orientation and settle position
- [ ] **Add ResourceChunkState.Flags**: Settling, PendingMerge to track lifecycle
- [ ] Adopt shared pooling utilities for chunk/entity reuse
- [ ] Review chunk data layout for SoA compliance
- [ ] Keep chunk/aggregate behaviour data-driven (physics, settle thresholds, interaction rules defined in `ResourceProfile`)
- [ ] Create `StorehouseIntakeSystem` (chunk contact with storehouse intake collider, convert chunk units to storehouse aggregate totals, trigger UI events, handle capacity)
- [ ] Create `ConstructionSiteIntakeSystem` (similar to storehouse but routes resources to construction progress, update construction state, spawn leftover pile if over-delivered)
- [ ] Ensure `ResourceAggregate` totals stay in sync with registries
- [ ] Implement `ResourceHistorySystem` recording add/remove events for rewind
- [ ] **Implement ResourcePileMergeSystem** (query spatial grid for same-type piles within merge radius, deterministic ordering, transfer units, destroy absorbed pile, update LastMergeTick, run every 30 ticks)
- [ ] **Implement ResourcePileSizeUpdateSystem** (monitor TotalUnits, determine size category, swap pile prefab/mesh when crossing threshold, update visual scale)
- [ ] **Implement ResourcePileRegistrySystem** (update registry each tick, cache SpatialCellIndex and position for queries, mark AvailableForGathering based on reservations, provide totals for analytics/HUD)
- [ ] Convert dropped chunks into piles (handled by ResourceChunkToPileConversionSystem)
- [ ] Ensure piles are immovable (no Velocity component, static position)
- [ ] **Hand chunk pickup** (individual chunks): Add `HandPickupableChunk` tag, integrate with existing hand grab system, chunks follow hand cursor when held, throw mechanics reuse hand slingshot system, chunks retain physics after throw
- [ ] **Hand pile siphon** (RMB hold mechanic): Implement `ResourceHandSiphonSystem`, start siphon, per-tick update, type locking, capacity limit, distance check, visual/audio feedback
- [ ] **Hand dump to storehouse**: Detect when hand holds resources AND hovers over storehouse, auto-trigger dump, transfer all hand units to storehouse inventory instantly, visual/audio feedback, clear hand ResourceTypeIndex
- [ ] **Integrate with RMB priority router**: Update priority, add hysteresis (3 frames) to prevent mode jitter
- [ ] **Villager chunk carrying**: Chunks auto-attach to villager, visual chunk entity follows villager position, capacity villager can carry limited chunks
- [ ] **Villager chunk delivery**: Villager reaches storehouse/construction → chunks aggregate, chunks transfer from villager inventory to destination, visual chunks disappear, counters update
- [ ] **Villager pile gathering** (future - optional): Villagers can gather from piles as alternative to sources, registry flags piles as gatherable nodes, reduces pile units, villager gains chunk/units
- [ ] Reuse resource chunk model for miracle tokens (alignment-adjusted weights, effect triggers)
- [ ] Provide generic chunk → miracle effect hook (e.g., deliver to altar to cast)
- [ ] Ensure miracle framework can request resource tokens (integration with `MiraclesFramework_TODO.md`)
- [ ] Extend `ResourceRegistry` to include chunk entities, aggregate piles, storehouse aggregates
- [ ] Provide queries for villager jobs and presentation (total resources, available piles)
- [ ] Emit analytics/log events for resource flow (gathered, stored, consumed)
- [ ] Adopt shared registry utilities (`RegistryUtilities.cs`) to align with integration plan
- [ ] Unit tests for chunk state transitions, storehouse intake, pile merging
- [ ] Playmode tests (throw chunk at storehouse, drop chunk on ground, siphon/pickup flows, deplete resource nodes)
- [ ] Rewind tests: record → gather/dump → rewind; ensure totals match original state
- [ ] Stress tests with thousands of chunks/piles; ensure zero GC, acceptable frame time
- [ ] Add debug overlays for resource piles (units, type) and storehouse totals
- [ ] Provide editor gizmos for pile merge radius, storehouse intake zones
- [ ] Update guides (`Docs/Guides/SceneSetup.md`, new `Docs/DesignNotes/ResourcesFramework.md`) explaining resource authoring and configuration
- [ ] Document field tuning (siphon rates, chunk mass)

#### 9. Utilities & Tooling (`Utilities_TODO.md`)
- [ ] Expose metrics to external dashboard (Grafana/Influx or equivalent)
- [ ] Add nightly stress suite (100k entities, long-run soak, memory leak detection)
- [ ] Build deterministic replay harness comparing snapshots across runs
- [ ] Automate regression scenes (villager loop, miracle rain, resource delivery)
- [ ] Integrate test coverage reporting into CI
- [ ] Populate `Docs/QA/IntegrationTestChecklist.md` with step-by-step flows
- [ ] Flesh out `EnvironmentGridTests` scaffolding with baseline assertions once environment jobs land
- [ ] Add pooling/stress tests (spawn/despawn cycles, NativeContainer reuse) to ensure shared pools remain safe
- [ ] Coordinate with `SpawnerFramework_TODO.md` for churn tests once spawn pipeline lands
- [ ] Implement automated profiling harness capturing frame timings after key commits
- [ ] Add memory budget monitoring (Blob allocations, NativeContainers, pooled buffers)
- [ ] Ensure all Burst jobs have `CompileSynchronously` guards in dev to catch errors early
- [ ] Set up regression alerts when frame time exceeds budget by threshold
- [ ] Provide sandbox scene for microbenchmarks (grid updates, flow fields, miracles)
- [ ] Profile pooled vs. non-pooled code paths; capture allocation spikes and document thresholds
- [ ] Expose metrics to external dashboard (Grafana/Influx or equivalent) - **Duplicate, listed twice**
- [ ] Instrument job scheduling logs (optional) to verify worker thread utilisation
- [ ] Automate AssetBundle/Addressable content builds tied to DOTS subscenes
- [ ] Validate headless server build for large-scale sims/testing
- [ ] Implement save/load determinism tests (serialize snapshots, reload, compare)
- [ ] Document platform-specific quirks (physics differences, input devices) for DOTS runtime
- [ ] Prepare integration with crash reporting (Unity Cloud Diagnostics, Sentry, etc.)
- [ ] Surface `JobsUtility.JobWorkerCount` defaults and instrumentation hooks
- [ ] Integrate test coverage reporting into CI - **Duplicate**
- [ ] Add nightly stress suite automation - **Duplicate**
- [ ] Set up performance regression detection
- [ ] Add sanity clamps for player-controlled inputs (terraforming height limits, miracle power caps)
- [ ] Sandbox scripting interfaces (if any) to prevent arbitrary code execution
- [ ] Implement watchdog systems detecting stuck jobs or long frames
- [ ] Harden debug commands (auth levels, disable in shipping builds)
- [ ] Ensure save files validated before load (version checks, CRC)
- [ ] Document incident response playbook (log capture, replay reproduction)
- [ ] Maintain living design/doc index linking truth-sources, TODOs, decision logs
- [ ] Establish code review checklist referencing runtime/integration truth-sources
- [ ] Create onboarding guide for new engineers (bootstrap, systems, testing basics)
- [ ] Set up weekly cross-discipline sync to surface integration risks
- [ ] Track key decisions in decision-log with rationale and owners
- [ ] Maintain risk register (technical debt, external dependencies, schedule risks)
- [ ] Flesh out `EnvironmentAndSpatialValidation` guide with screenshots/examples once validation scripts land
- [ ] Expand `GettingStarted.md` with domain-specific onboarding (villagers/resources/miracles) when systems stabilise
- [ ] Author "PureDOTS Adoption" guide/README detailing bootstrap setup, required assets, and references to truth-sources
- [ ] Create `Docs/INDEX.md` or equivalent navigation page linking truth-sources, TODOs, guides, QA docs
- [ ] Capture editor validation tasks (ScriptableObject inspectors, link.xml maintenance) as future backlog items when discovered
- [ ] Align onboarding/docs with `SpawnerFramework_TODO.md` once framework stabilises
- [ ] Implement feature toggles/config gating for experimental systems (terraforming, advanced miracles)
- [ ] Set up experimental branches with automated merge/backport workflows
- [ ] Document compatibility plans for future Entities/Unity releases (upgrade guides)
- [ ] Capture deprecation paths for interim hacks or temporary components
- [ ] Monitor DOTS community/Unity roadmaps for upcoming changes that affect PureDOTS
- [ ] Provide schedule buffer tracking and alerts when integration slips threaten downstream work

### Lower Priority (Nice to Have)

#### 10. Main TODO File (`PureDOTS_TODO.md`)
- [ ] Migrate essential DOTS systems already written (time, rewind, resource gathering, villager jobs) tightening them for the fresh architecture - **Status: Mostly complete, some tightening remaining**
- [ ] Define how visual components hook into DOTS data (e.g., companion components, Entities Graphics) with minimal MonoBehaviour glue
- [ ] Convert key prefabs/scenes from the old project into SubScenes or hybrid workflows aligned with the new architecture
- [ ] Plan tooling for designers/artists early (e.g., Inspector/UI for DOTS data)
- [ ] Document hot/cold archetype strategy and implement companion presentation bridges that respect rewind guards - **Status: Documentation exists, implementation may need completion**
- [ ] Stand up a basic automated test suite (PlayMode/Runtime) exercising time, resource, and villager systems - **Status: Some tests exist, coverage gaps remain**
- [ ] Port useful editor tooling (debug HUDs, history inspectors) from the old project once DOTS data structures stabilize
- [ ] Draft CI/test-runner scripts for DOTS builds and playmode runs - **Status: Some CI scripts exist, needs expansion**
- [ ] Expand deterministic playmode coverage for hand/camera/time controls and resource workflows; rely on console/log assertions for rewind until visual presenters exist
- [ ] Build a 50k-entity performance harness that validates rewind determinism and captures frame timing budgets
- [ ] Inventory legacy assets/code worth porting (prefabs, ScriptableObjects, DOTS-ready scripts) and import them gradually
- [ ] Note hard dependencies in the legacy project that need counterpart implementations (e.g., job board, needs systems) before those assets can be reused
- [ ] Establish a deprecation list for hybrid scripts that will be replaced rather than ported - **Status: DeprecationList.md exists**
- [ ] Stand up the environment system cadence: moisture/temperature/wind/sunlight/magnetic storm/debris field/solar radiation updates + sampling helpers - **Status: Core environment systems exist, some effects pending**
- [ ] Finish spatial grid rebuild jobs & query utilities, then expand registries per `Docs/TODO/SpatialServices_TODO.md` & `Docs/TODO/RegistryRewrite_TODO.md` - **Status: Core spatial grid exists, some integrations pending**
- [ ] Document rewind patterns (`Docs/DesignNotes/RewindPatterns.md`) and install guard systems so every group honours `RewindState` - **Status: RewindPatterns.md exists, guard systems implemented**
- [ ] Complete `Docs/DesignNotes/SystemExecutionOrder.md`, ensure `[UpdateInGroup]` usage matches truth-source expectations, and hook `FixedStepSimulationSystemGroup` to `TimeState` - **Status: SystemExecutionOrder.md exists, may need updates**
- [ ] Execute initial `Docs/TODO/Utilities_TODO.md` backlog: debug overlay, telemetry hooks, integration test harness, deterministic replay, CI pipeline stubs - **Status: Many utilities exist, some gaps remain**
- [ ] Cross-link active TODOs with `RuntimeLifecycle_TruthSource.md`/`SystemIntegration_TODO.md`; start `Docs/DesignNotes/SystemIntegration.md` + missing registry/spatial design briefs - **Status: SystemIntegration.md exists**
- [ ] Maintain meta work on `meta/dots-foundation`; merge stable slices to `master` once reviewed
- [ ] Restore `StreamingValidatorTests` references once the Authoring/Editor asmdefs are ready - **Status: May already be resolved**
- [ ] Track Entities 1.4.2 vs NetCode 1.8 physics regression: provide a local shim or package patch for `PhysicsWorldHistory.Clone` overloads - **Status: Future work**
- [ ] Remove the duplicate `StreamingCoordinatorBootstrapSystem` definition (source vs generated) to stop namespace collisions - **Status: May already be resolved**
- [ ] Add the missing generic comparer surface in `StreamingLoaderSystem` (e.g., `using System.Collections.Generic;` or a DOTS-friendly equivalent) - **Status: May already be resolved**
- [ ] Enforce three-agent cadence for backlog execution (Implementation Agent, Error & Glue Agent, Documentation Agent) - **Status: Documented but not enforced**

## Summary Statistics

- **Code TODOs**: 4 items
- **High Priority Documentation TODOs**: ~30-40 items
- **Medium Priority Documentation TODOs**: ~200+ items
- **Lower Priority Documentation TODOs**: ~50+ items

**Total Estimated Outstanding TODOs**: ~300+ items across code and documentation

## Recommendations

1. **Immediate Focus**: Address the 4 code TODOs and verify compilation health issues
2. **Short-term**: Complete presentation bridge testing and system integration tasks
3. **Medium-term**: Work through domain-specific TODOs (Villager, Resources, Climate, Spatial) systematically
4. **Long-term**: Establish governance, complete testing infrastructure, build comprehensive tooling

## Notes

- Many TODOs reference work that is partially complete (e.g., environment grids exist but some effects are pending)
- Some TODOs may be duplicates or may have been completed but not marked
- The technical debt document (`TECHNICAL_DEBT.md`) provides a prioritized view of remaining work
- The roadmap status (`ROADMAP_STATUS.md`) tracks progress against planned phases

