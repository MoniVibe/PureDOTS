Pure DOTS Project Kickoff
==========================

This file captures the first wave of work for the new PureDOTS Unity project. The goals are to establish a clean DOTS-first architecture while salvaging useful pieces from the legacy `godgame` repository where practical. When referencing legacy behaviour, always document the desired end-state and confirm with leads before mirroring old code paths—never assume a 1:1 port is acceptable without clarification.

1. Project Foundation
   - [x] Create DOTS-only assembly definitions (Runtime, Systems, Authoring) mirroring `GodGame.ECS` but scoped for the new project.
   - [x] Define a custom bootstrap/world setup (FixedStepSimulation, Simulation, Presentation groups) referencing current best practices from the existing `PureDotsWorldBootstrap`.
   - [x] Set up core packages/environment parity (DONE via manifest copy; Unity regenerates `packages-lock.json` on open).
   - [x] **Pinned Advisory:** Lock documentation and onboarding notes to Entities 1.4.2 + NetCode 1.8 + Input System 1.7 baseline; highlight that NetCode integration stays on hold until single-player runtime is stable, and flag any attempts to pull 1.5+ APIs or legacy `UnityEngine.Input` usage in `Docs/Vision/core.md` so agents stop reintroducing incompatible patterns.
   - [x] Publish PureDOTS as a Unity package (package.json, README, samples) so downstream games reference it via UPM/git instead of copying code into their repos.
   - [x] Document the consumer workflow: game projects (`../Godgame`, `../Space4x`, etc.) should keep gameplay code in their own asmdefs and reference template assemblies only. Guidance now lives in `Docs/Guides/UsingPureDOTSInAGame.md`.

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
  - [x] Lock deterministic hand/camera logic (router priorities, state machine events, fixed-step cursor transforms) while deferring BW2-style presentation to game-specific layers. (Core DOTS infrastructure in place: `DivineHandSystem`, `HandInputRouterSystem`, `HandCameraInputRouter`; state machine completion pending per `DivineHandCamera_TODO.md`)
    - Update `Docs/TODO/DivineHandCamera_TODO.md` to flag orbit/visual parity as downstream work; PureDOTS baseline owns logical DOTS flow only.
  - [x] Re-implement remaining gameplay logic in pure DOTS (villager AI, resource economy, time control) referencing TruthSource contracts rather than hybrid adapters. For each legacy system, note any deviations required for the new architecture before implementation begins. (All core systems implemented: `VillagerAISystem`, `VillagerJobSystems`, `ResourceSystems`, `TimeTickSystem`, `RewindCoordinatorSystem`)
  - [x] Ensure each domain has deterministic update groups and clear scheduling.
  - [x] Add debugging/visualisation systems (HUD, gizmos) to inspect DOTS state during iteration. (Debug HUD + telemetry systems in place)
  - [ ] Flesh out villager job behavior stubs (`GatherJobBehavior`, `BuildJobBehavior`, `CraftJobBehavior`, `CombatJobBehavior`) and feed archetype catalog data into AI selection; current smoke coverage only validates scaffolding compiles.
  - [x] Integrate Godgame villagers and Space4X vessels with shared `AISystemGroup` pipeline via bridge systems and AI component authoring (completed: `GodgameVillagerAICommandBridgeSystem`, `Space4XVesselAICommandBridgeSystem`, sensor category extensions, utility bindings).
  - [ ] Complete AI gap closure: virtual sensors for internal needs, miracle detection, performance metrics, flow field integration (see `Docs/AI_Backlog.md` for prioritized items).
   
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
   - [x] Replace `WorldServices`/`RegistrySystems` patterns with DOTS singletons and buffer queries from the start. (All registries use DOTS buffers and singletons)
   - [x] Port domain-specific registry systems (resources, storehouses, villagers, bands) using DOTS buffers only—no bridge shims. (All core registries implemented)
   - [x] Seed shared registry directory + handle lookup so systems can resolve registries without service locators (`RegistryDirectorySystem`).
   - [x] Provide registry lookup helpers and route core systems (spatial rebuild, job loops) through the directory for engine-agnostic access.
   - [x] Add runtime continuity validation and instrumentation buffers (`RegistryContinuityValidationSystem`, `RegistryInstrumentationSystem`) so registries surface spatial drift and health metrics to shared tooling.
   - [x] Enforce registry definition/catalog continuity parity in validation alerts (flags definition mismatches in HUD/telemetry).
   - [x] Lock registry schemas to be theme-agnostic (villagers, logistics, miracles, construction) so downstream games only supply config/assets and intent commands. (Schema neutrality documented in `Docs/DesignNotes/SystemIntegration.md`)
   - [x] Expose construction/jobsite registry so both template games consume identical build-site data.
   - [x] Add neutral band/squad, creature/threat, and ability registries so shared systems can resolve formation, enemy, and special-action data.
   - [x] Make spawner registry available for population/fauna/ship spawning coordination across games; add deterministic telemetry counters + HUD/telemetry exposure for spawner readiness/attempts.
   - [ ] Build thin bridging layers only if legacy content/prefabs require temporary compatibility.
   - [x] Introduce pooled SoA-friendly memory utilities (NativeList/Queue pools, slab allocators) to eliminate per-frame allocations in hot systems. (Pooling utilities implemented; see `Runtime/Pooling/`)
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
  - [ ] Expand deterministic playmode coverage for hand/camera/time controls and resource workflows; rely on console/log assertions for rewind until visual presenters exist.
  - [x] Add lightweight console instrumentation for rewind progress/state diffing to compensate for lack of visual entity representations.
  - [x] HUD/telemetry stream now surfaces order/signal bus counts and spawner lifecycle stats for scenario harness consumption.
   - [ ] Build a 50k-entity performance harness that validates rewind determinism and captures frame timing budgets.

7. Migration Plan & Asset Salvage
   - [ ] Inventory legacy assets/code worth porting (prefabs, ScriptableObjects, DOTS-ready scripts) and import them gradually.
   - [ ] Note hard dependencies in the legacy project that need counterpart implementations (e.g., job board, needs systems) before those assets can be reused.
   - [ ] Establish a deprecation list for hybrid scripts that will be replaced rather than ported.

8. Space4X Framework Backlog
   - [ ] Track Space4X-specific DOTS work (alignment/compliance, modules/degradation, mining nodes, waypoints/infrastructure, economy/spoilage, tech diffusion, interception, crew progression). See `Docs/TODO/Space4X_Frameworks_TODO.md` for detailed items and phasing pulled from `Space4x/Docs/TODO/4xdotsrequest.md`.

8. Spatial Services & Observability
   - [x] Implement configurable spatial grid/quadtree service (config asset + blob + lookup jobs) for proximity queries. (Spatial grid implemented with query helpers - see `SpatialQueryHelper`)
     - Planning doc: `Docs/TODO/SpatialServices_TODO.md`.
     - Ensure navigation stack supports 2D surface layers today and can extend to full 3D volumes via config/provider swaps without code changes.
   - [x] Wire up provider abstraction (`ISpatialGridProvider`) with hashed-grid implementation and config validation so future providers can plug in without touching consumer systems.
   - [x] Extend deterministic debug/observability stack (timeline visualisers, HUD overlays) to read DOTS data without hybrid shims. (Debug HUD and telemetry systems in place)

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

10. Compilation Health
   - [x] Restore `StreamingValidatorTests` references once the Authoring/Editor asmdefs are ready: ensure the test assembly links against the correct editor assembly (or interim `Assembly-CSharp-Editor`) and `Unity.Scenes` so `PureDOTS.Authoring`, `PureDOTS.Editor`, and `Unity.Scenes` namespaces resolve. (Verified: No StreamingValidatorTests found, may not exist or already resolved)
   - [ ] Track Entities 1.4.2 vs NetCode 1.8 physics regression: provide a local shim or package patch for `PhysicsWorldHistory.Clone` overloads (2- and 3-parameter variants) so the NetCode runtime builds. (Note: NetCode/netplay is final priority, deferred to end of project)
   - [x] Remove the duplicate `StreamingCoordinatorBootstrapSystem` definition (source vs generated) to stop namespace collisions in `PureDOTS.Systems.Streaming`. (Verified: Only one definition exists in `Runtime/Systems/Streaming/StreamingCoordinatorBootstrapSystem.cs`)
   - [x] Add the missing generic comparer surface in `StreamingLoaderSystem` (e.g., `using System.Collections.Generic;` or a DOTS-friendly equivalent) so `IComparer<StreamingSectionCommand>` compiles under 1.4. (Verified: Already has `using System.Collections.Generic;` and implements `StreamingCommandComparer : IComparer<StreamingSectionCommand>`)

11. Agent Workflow Protocol
   - [ ] Enforce three-agent cadence for backlog execution:
     1. **Implementation Agent:** picks up scoped backlog slices and lands feature/system code.
     2. **Error & Glue Agent:** resolves compile/runtime errors, integrates with existing systems, and polishes tests triggered by the implementation changes.
     3. **Documentation Agent:** updates TruthSources, docs, and changelogs to reflect the delivered work and validations.
   - [ ] Require each agent hand-off to cite the files touched and pending follow-ups so downstream prompts can focus on their lane without re-discovering context.

Keep this file updated as tasks complete or new requirements surface. Tie progress back to existing TruthSource documents to stay aligned with the long-term vision.
