Pure DOTS Project Kickoff
==========================

This file captures the first wave of work for the new PureDOTS Unity project. The goals are to establish a clean DOTS-first architecture while salvaging useful pieces from the legacy `godgame` repository where practical. When referencing legacy behaviour, always document the desired end-state and confirm with leads before mirroring old code paths—never assume a 1:1 port is acceptable without clarification.

1. Project Foundation
   - [x] Create DOTS-only assembly definitions (Runtime, Systems, Authoring) mirroring `GodGame.ECS` but scoped for the new project.
   - [x] Define a custom bootstrap/world setup (FixedStepSimulation, Simulation, Presentation groups) referencing current best practices from the existing `PureDotsWorldBootstrap`.
   - [ ] Set up core packages/environment parity (DONE via manifest copy; Unity will regenerate `packages-lock.json`).

2. Core Data & Authoring
   - [x] Port reusable ECS components and bakers (`ResourceComponents`, `VillagerComponents`, `ResourceAuthoring`, etc.) from `godgame` into the new assemblies.
   - [x] Recreate authoring paths via SubScenes/Bakers, using existing ScriptableObject definitions as source data where sensible.
   - [x] Document the canonical component sets in `Docs/` for quick reference (tie back to TruthSources).

3. Systems & Simulation
   - [ ] Migrate essential DOTS systems already written (time, rewind, resource gathering, villager jobs) tightening them for the fresh architecture.
       - [x] Time tick/step flow, rewind routing, and world bootstrap configured for deterministic singletons.
       - [x] Rewind coordinator and command processing for pause/playback/catch-up paths.
       - [x] Resource gathering, storehouse inventory, withdrawal, and deposition loops.
       - [x] Villager job assignment, needs, and status systems.
   - [ ] Align divine hand & BW2 camera controls with legacy truth sources (router priorities, state machine events, pivot-orbit behaviour, cursor-relative pan).
     - See `Docs/TODO/DivineHandCamera_TODO.md` for detailed plan scaffold.
  - [ ] Re-implement remaining gameplay logic in pure DOTS (villager AI, resource economy, time control) referencing TruthSource contracts rather than hybrid adapters. For each legacy system, note any deviations required for the new architecture before implementation begins.
  - [x] Ensure each domain has deterministic update groups and clear scheduling.
   - [ ] Add debugging/visualisation systems (HUD, gizmos) to inspect DOTS state during iteration.
   
   Vegetation Growth Loop (Agent Alpha & Beta):
   - [x] Scaffold vegetation lifecycle components (`VegetationComponents.cs`) with lifecycle stages, health, production, consumption, reproduction, and seasonal effects.
   - [x] Create vegetation authoring (`VegetationAuthoring.cs`) with baker stubs for runtime component conversion.
   - [x] Implement `VegetationGrowthSystem` - updates lifecycle stages using data-driven `VegetationSpeciesCatalog` blob for stage durations and thresholds.
   - [x] Create `VegetationSpeciesCatalog` ScriptableObject and blob baker for data-driven species configuration.
   - [x] Add comprehensive test suite with multi-species validation (`VegetationGrowthSystemTests.cs`).
   - [x] Implement `VegetationHealthSystem` - process environmental effects (water level, light, soil quality).
     - Uses species catalog blob for environmental thresholds
     - Adds `VegetationStressedTag` and `VegetationDyingTag` based on health
     - Runs before growth system to ensure health affects lifecycle
   - [x] Implement `VegetationReproductionSystem` - handle spreading and new growth based on reproduction timers. [Beta: reproduction parameters]
   - [x] Implement `VegetationHarvestSystem` - allow villagers to gather resources from fruiting vegetation. (Villager inventory receives harvest yield; resource deposit flow handles downstream storage.)
   - [x] Implement `VegetationDecaySystem` - cleanup dead vegetation after decay period. [Beta: decay rates]

4. Service/Registry Replacement
   - [ ] Replace `WorldServices`/`RegistrySystems` patterns with DOTS singletons and buffer queries from the start.
   - [ ] Port domain-specific registry systems (resources, storehouses, villagers, bands) using DOTS buffers only—no bridge shims.
   - [x] Seed shared registry directory + handle lookup so systems can resolve registries without service locators (`RegistryDirectorySystem`).
   - [x] Provide registry lookup helpers and route core systems (spatial rebuild, job loops) through the directory for engine-agnostic access.
   - [ ] Build thin bridging layers only if legacy content/prefabs require temporary compatibility.
   - [ ] Introduce pooled SoA-friendly memory utilities (NativeList/Queue pools, slab allocators) to eliminate per-frame allocations in hot systems.
     - Track detailed work items in `Docs/TODO/RegistryRewrite_TODO.md`.

5. Presentation & Hybrid Strategy
   - [ ] Define how visual components hook into DOTS data (e.g., companion components, Entities Graphics) with minimal MonoBehaviour glue.
   - [ ] Convert key prefabs/scenes from the old project into SubScenes or hybrid workflows aligned with the new architecture.
   - [ ] Plan tooling for designers/artists early (e.g., Inspector/UI for DOTS data).
   - [ ] Document hot/cold archetype strategy and implement companion presentation bridges that respect rewind guards.
     - Detailed presentation roadmap lives in `Docs/TODO/PresentationBridge_TODO.md`.

6. Testing & Tooling
   - [ ] Stand up a basic automated test suite (PlayMode/Runtime) exercising time, resource, and villager systems.
   - [ ] Port useful editor tooling (debug HUDs, history inspectors) from the old project once DOTS data structures stabilize.
   - [x] Maintain a running `Docs/Progress.md` log aligning with TruthSources for future contributors.
   - [ ] Draft CI/test-runner scripts for DOTS builds and playmode runs.
   - [ ] Expand deterministic playmode coverage for hand/camera/time controls and resource workflows; add headless stress runs.
   - [ ] Build a 50k-entity performance harness that validates rewind determinism and captures frame timing budgets.

7. Migration Plan & Asset Salvage
   - [ ] Inventory legacy assets/code worth porting (prefabs, ScriptableObjects, DOTS-ready scripts) and import them gradually.
   - [ ] Note hard dependencies in the legacy project that need counterpart implementations (e.g., job board, needs systems) before those assets can be reused.
   - [ ] Establish a deprecation list for hybrid scripts that will be replaced rather than ported.

8. Spatial Services & Observability
   - [ ] Implement configurable spatial grid/quadtree service (config asset + blob + lookup jobs) for proximity queries.
     - Planning doc: `Docs/TODO/SpatialServices_TODO.md`.
   - [ ] Extend deterministic debug/observability stack (timeline visualisers, HUD overlays) to read DOTS data without hybrid shims.

9. Meta Foundations & Coordination
   - [ ] Stand up the environment system cadence: moisture/temperature/wind/sunlight/magnetic storm/debris field/solar radiation updates + sampling helpers (see `Docs/TODO/ClimateSystems_TODO.md`).
     - [x] Climate profile authoring + `ClimateStateUpdateSystem` baseline to drive time-of-day, season cycle, wind, and humidity ticks.
     - [x] Moisture runtime buffers with `MoistureEvaporationSystem`/`MoistureSeepageSystem` cadenced at 10 ticks for deterministic grid updates.
   - [ ] Finish spatial grid rebuild jobs & query utilities, then expand registries (villager/miner vessel/hauler freighter/wagon/resource/miracle neutral) per `Docs/TODO/SpatialServices_TODO.md` & `Docs/TODO/RegistryRewrite_TODO.md`.
   - [ ] Document rewind patterns (`Docs/DesignNotes/RewindPatterns.md`) and install guard systems so every group honours `RewindState` (tie into `RuntimeLifecycle_TruthSource.md`).
   - [ ] Complete `Docs/DesignNotes/SystemExecutionOrder.md`, ensure `[UpdateInGroup]` usage matches truth-source expectations, and hook `FixedStepSimulationSystemGroup` to `TimeState`.
   - [ ] Execute initial `Docs/TODO/Utilities_TODO.md` backlog: debug overlay, telemetry hooks, integration test harness, deterministic replay, CI pipeline stubs.
   - [ ] Cross-link active TODOs with `RuntimeLifecycle_TruthSource.md`/`SystemIntegration_TODO.md`; start `Docs/DesignNotes/SystemIntegration.md` + missing registry/spatial design briefs.
   - [ ] Maintain meta work on `meta/dots-foundation`; merge stable slices to `master` once reviewed.

Keep this file updated as tasks complete or new requirements surface. Tie progress back to existing TruthSource documents to stay aligned with the long-term vision.
