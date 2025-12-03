# PureDOTS Backlog

**Last Updated**: 2025-12-01
**Purpose**: Consolidated backlog organized by domain

---

## Overview

This backlog consolidates outstanding work items from domain-specific TODO files. Items are organized by domain and priority.

**Active Sprint Work**: See `PureDOTS_TODO.md` (root) for current 2-3 week sprint items.

---

## Framework Core

### Registry Systems
- [ ] Complete registry migration: Switch resource gathering/storehouse systems to new registries
- [ ] Villager job assignment: Consume registry instead of scanning every frame
- [ ] Rain miracles/divine hand: Use registries for nearest valid targets
- [ ] Remove legacy service singletons/MonoBehaviours that duplicate registry responsibilities
- [ ] Generic consumption samples/tests for downstream games
- [ ] Inspector gizmos/debug HUD panels for registry contents
- [ ] Unit tests for each registry update system (spawn, despawn, reorder, rewind)
- [ ] Playmode tests ensuring identical behavior before/after migration
- [ ] Stress tests with 50k entities (no GC allocations, acceptable frame time)

### Spatial Services
- [ ] Expand deterministic query utilities: kNN, multi-radius batches, filtered iterators
- [ ] Data-driven query descriptors for reusable query pipeline
- [ ] Integrate registries with spatial indexing metadata
- [ ] Support 2D navigation and define hooks for 3D layers
- [ ] Stub hierarchical grid interface (macro cell → micro grid)
- [ ] Plan GPU offload hook for extreme densities
- [ ] Reserve data slots for additional attributes (occupancy heatmaps, normals)
- [ ] Formalize layer provider abstraction (`INavLayerProvider`)
- [ ] Integrate miner/hauler logistics with spatial queries
- [ ] Integrate divine hand/miracles with grid for targeting
- [ ] Debug authoring: scene gizmo drawer for grid bounds/cell occupancy
- [ ] Runtime overlay (Entities Graphics or UI) via debug menu
- [ ] Unit tests (Morton/Z-curve hash, cell assignment, radius query accuracy)
- [ ] Rewind determinism tests (record 100 ticks → rewind → verify grid state)

---

## AI & Behavior

### Villager Systems
- [ ] Audit current villager systems for data layout, performance, determinism gaps
- [ ] Profile existing loops to identify hotspots
- [ ] Build `VillagerArchetypeCatalog` ScriptableObject + blob
- [ ] Hook `VillagerAISystem` to archetype catalog data
- [ ] Define `JobDefinitionCatalog` with durations, costs, rewards, requirements
- [ ] Define `VillagerNeedCurve` assets (AnimationCurve → blob)
- [ ] SoA Refactor Phase 1: Replace floats with short/ushort, consolidate tags
- [ ] SoA Refactor Phase 2: Split inventory buffer to companion entity
- [ ] SoA Refactor Phase 3: Rewrite systems as Burst jobs with SoA data
- [ ] Implement `VillagerLocalSteeringSystem` for obstacle avoidance
- [ ] Create job/need priority scheduler per villager archetype
- [ ] Implement shift schedules (day/night) using `TimeOfDay` service
- [ ] Provide fallback behaviors (idle, roam, worship)
- [ ] Hook into prayers/tribute economy
- [ ] Rebuild `VillagerInventory` buffer (multiple slots, weight/capacity)
- [ ] Implement alignment state derived from actions
- [ ] Add morale effects (panic, productivity modifiers)
- [ ] Define interrupt pipeline: player hand pickup, miracles, disasters
- [ ] Implement Flow Field Pathfinding for 100k+ agents
- [ ] Ensure pathfinding rewind compatibility
- [ ] Unit tests for needs decay, job assignment, inventory transfers
- [ ] Stress tests with 100k, 500k, 1M villagers
- [ ] Determinism tests: identical commands → same villager states

### AI Framework
- [ ] Complete AI gap closure: virtual sensors for internal needs, miracle detection
- [ ] Performance metrics integration
- [ ] Flow field integration
- [ ] Define reusable AI behavior modules (sensors, scoring, steering, task selectors)
- [ ] Document patterns for combining generic AI with specialized behavior

---

## Resources & Economy

### Resources Framework
- [ ] Create `ResourceProfile` ScriptableObject (type id, display name, alignment impact, etc.)
- [ ] Build `ResourceProfileBlob` for runtime lookup
- [ ] Define enhanced chunk components (`ResourceChunkPhysics`, `HandPickupableChunk`)
- [ ] Define pile components (`ResourcePile`, `ResourcePileMergeCandidate`, `HandSiphonable`)
- [ ] Define pile registry (`ResourcePileRegistry`, `ResourcePileEntry`)
- [ ] Catalogue node archetypes and behavior rules
- [ ] Define `ResourceNode` component (NodeType, Purity, RegenTimer)
- [ ] Hook node regeneration into climate/biome events
- [ ] Implement `ResourceChunkPhysicsSystem` (gravity, friction, collision)
- [ ] Implement `ResourceChunkSettleDetectionSystem`
- [ ] Implement `ResourceChunkToPileConversionSystem`
- [ ] Create `StorehouseIntakeSystem` (chunk contact → aggregate totals)
- [ ] Create `ConstructionSiteIntakeSystem`
- [ ] Implement `ResourceHistorySystem` for rewind
- [ ] Implement `ResourcePileMergeSystem` (deterministic ordering)
- [ ] Implement `ResourcePileSizeUpdateSystem` (size category, prefab swap)
- [ ] Implement `ResourcePileRegistrySystem` (update registry, cache spatial data)
- [ ] Hand chunk pickup: Integrate with existing hand grab system
- [ ] Hand pile siphon: Implement `ResourceHandSiphonSystem` (RMB hold)
- [ ] Hand dump to storehouse: Auto-trigger dump on hover
- [ ] Villager chunk carrying: Chunks auto-attach to villager
- [ ] Villager chunk delivery: Transfer from inventory to destination
- [ ] Unit tests for chunk state transitions, storehouse intake, pile merging
- [ ] Playmode tests (throw chunk, drop chunk, siphon/pickup flows)
- [ ] Rewind tests: record → gather/dump → rewind; verify totals match

---

## Climate & Environment

### Climate Systems
- [ ] Inventory current climate-related data (`ClimateGrid`, moisture grid, vegetation thresholds)
- [ ] Review BW2 references for climate behavior
- [ ] Identify systems needing climate hooks
- [ ] Determine data resolution (grid size vs. world size) and update cadence
- [ ] Define comprehensive grid structures (SunlightGrid, MoistureGrid, TemperatureGrid, WindField)
- [ ] Define `ClimateProfile` ScriptableObject (expand existing fields if needed)
- [ ] Add BiomeType enum and biome determination thresholds
- [ ] Implement `BiomeDeterminationSystem` (runs every 60 ticks)
- [ ] Sun arc calculations (midnight, sunrise, noon, sunset, seasonal variation)
- [ ] Implement `WindFieldUpdateSystem` (runs every 120 ticks)
- [ ] Calculate local wind variation per cell (wind shadow, valleys, forests)
- [ ] Provide APIs: `GetWindAtPosition(float3)` returning direction & magnitude
- [ ] Integrate with seed dispersal: Wind direction biases seed placement
- [ ] Integrate with rain clouds: Clouds drift with wind
- [ ] Water balance equation per cell: ΔMoisture = Rainfall + Seepage_In - Evaporation - Plant_Consumption - Drainage - Seepage_Out
- [ ] Create `SnowLayerSystem`: Represent snow accumulation as mesh/texture layer
- [ ] Moisture coupling: Melting snow adds moisture to nearby cells
- [ ] Fire propagation system: Use wind direction to spread flame tokens
- [ ] Climate debug overlay (wind vectors, temperature heatmap, snow depth)
- [ ] Editor inspector for profiles with preview graphs
- [ ] Unit tests (sun position, temperature, evaporation, seepage, biome determination)
- [ ] Playmode tests (full day/night cycle, seasonal transition, rain adds moisture)
- [ ] Determinism tests (climate state identical after rewind)

---

## Rendering & Presentation

### Presentation Bridge
- [ ] Validation tests missing (rewind-safe presentation tests)
- [ ] Sample authoring guide missing (`Docs/Guides/Authoring/`)
- [ ] Define how visual components hook into DOTS data (companion components, Entities Graphics)
- [ ] Convert key prefabs/scenes into SubScenes or hybrid workflows
- [ ] Plan tooling for designers/artists (Inspector/UI for DOTS data)
- [ ] Document hot/cold archetype strategy
- [ ] Implement companion presentation bridges that respect rewind guards

---

## Testing & QA

### Testing Infrastructure
- [ ] Stand up basic automated test suite (PlayMode/Runtime) for time, resource, villager systems
- [ ] Expand deterministic playmode coverage for hand/camera/time controls
- [ ] Build 50k-entity performance harness (rewind determinism, frame timing budgets)
- [ ] Add integration tests: hand holding resource + miracle token, hand dumps to storehouse
- [ ] Centralize gesture & siphon feedback events within `HandPresentationBridge`
- [ ] Configure automated nightly run (performance suites for spatial grid, environment, villagers, miracles, resources)
- [ ] Log integration metrics to shared dashboard
- [ ] Audit runtime for IL2CPP/AOT safety (no reflection in jobs, `[Preserve]` where needed)
- [ ] Add IL2CPP build configuration checklist (linker settings, `link.xml`, scripting backend)
- [ ] Deterministic replay harness comparing snapshots across runs
- [ ] Automate regression scenes (villager loop, miracle rain, resource delivery)
- [ ] Integrate test coverage reporting into CI
- [ ] Add pooling/stress tests (spawn/despawn cycles, NativeContainer reuse)
- [ ] Implement automated profiling harness capturing frame timings
- [ ] Add memory budget monitoring (Blob allocations, NativeContainers, pooled buffers)
- [ ] Set up regression alerts when frame time exceeds budget

---

## Utilities & Tooling

### Debug & Telemetry
- [ ] Expose metrics to external dashboard (Grafana/Influx or equivalent)
- [ ] Add nightly stress suite (100k entities, long-run soak, memory leak detection)
- [ ] Build deterministic replay harness comparing snapshots across runs
- [ ] Automate regression scenes
- [ ] Integrate test coverage reporting into CI
- [ ] Implement automated profiling harness capturing frame timings after key commits
- [ ] Add memory budget monitoring (Blob allocations, NativeContainers, pooled buffers)
- [ ] Set up regression alerts when frame time exceeds budget by threshold
- [ ] Provide sandbox scene for microbenchmarks
- [ ] Profile pooled vs. non-pooled code paths
- [ ] Instrument job scheduling logs (optional) to verify worker thread utilization
- [ ] Automate AssetBundle/Addressable content builds tied to DOTS subscenes
- [ ] Validate headless server build for large-scale sims/testing
- [ ] Implement save/load determinism tests
- [ ] Prepare integration with crash reporting (Unity Cloud Diagnostics, Sentry, etc.)

### Documentation & Onboarding
- [ ] Populate `Docs/QA/IntegrationTestChecklist.md` with step-by-step flows
- [ ] Flesh out `EnvironmentGridTests` scaffolding with baseline assertions
- [ ] Maintain living design/doc index linking truth-sources, TODOs, decision logs
- [ ] Establish code review checklist referencing runtime/integration truth-sources
- [ ] Create onboarding guide for new engineers (bootstrap, systems, testing basics)
- [ ] Set up weekly cross-discipline sync to surface integration risks
- [ ] Track key decisions in decision-log with rationale and owners
- [ ] Maintain risk register (technical debt, external dependencies, schedule risks)
- [ ] Flesh out `EnvironmentAndSpatialValidation` guide with screenshots/examples
- [ ] Expand `GettingStarted.md` with domain-specific onboarding
- [ ] Author "PureDOTS Adoption" guide/README

---

## System Integration

### Integration Tasks
- [ ] Add integration tests covering: hand holding resource + miracle token, hand dumps to storehouse
- [ ] Centralize gesture & siphon feedback events within `HandPresentationBridge`
- [ ] Update existing TODOs to reference terraforming contract
- [ ] Configure automated nightly run (performance suites concurrently)
- [ ] Log integration metrics to shared dashboard
- [ ] Update each subsystem TODO with cross-links to integration doc
- [ ] Host alignment review with leads from gameplay, simulation, presentation
- [ ] Define reusable AI behavior modules (sensors, scoring, steering, task selectors)
- [ ] Add regression checks ensuring shared layer naming stays neutral
- [ ] Define meta-system slices (`Runtime Core`, `Data Authoring`, `Tooling/Telemetry`, `QA/Validation`)
- [ ] Tag existing TODO tasks with their slice responsibility
- [ ] Establish lightweight governance process: monthly review per slice
- [ ] Publish onboarding notes clarifying which slice new features belong to

---

## Game-Specific (Space4X)

### Space4X Framework
- [ ] Alignment/compliance systems
- [ ] Modules + degradation/repairs
- [ ] Mining deposits/harvest nodes
- [ ] Waypoints/infrastructure
- [ ] Supply–demand economy
- [ ] Spoilage FIFO
- [ ] Tech diffusion
- [ ] Interception systems
- [ ] Crew progression/breeding (deferred)
- [ ] Telemetry/tests

---

## Lower Priority

### Meta Foundations
- [ ] Enforce three-agent cadence for backlog execution (Implementation, Error & Glue, Documentation)
- [ ] Require each agent hand-off to cite files touched and pending follow-ups
- [ ] Track Entities 1.4.2 vs NetCode 1.8 physics regression (future work)
- [ ] Monitor DOTS community/Unity roadmaps for upcoming changes
- [ ] Provide schedule buffer tracking and alerts when integration slips

---

## Notes

- Many items reference work that is partially complete
- Some items may be duplicates or may have been completed but not marked
- See domain-specific TODO files for detailed breakdowns:
  - `SystemIntegration_TODO.md`
  - `ClimateSystems_TODO.md`
  - `VillagerSystems_TODO.md`
  - `ResourcesFramework_TODO.md`
  - `SpatialServices_TODO.md`
  - `RegistryRewrite_TODO.md`
  - `Utilities_TODO.md`
  - `Space4X_Frameworks_TODO.md`
  - `PresentationBridge_TODO.md`

---

*Last Updated: 2025-12-01*

