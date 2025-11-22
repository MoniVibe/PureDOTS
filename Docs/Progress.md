# PureDOTS Progress Log

## 2025-12-XX (Phase 3 Presentation Follow-up)
- Expanded presentation binding schema (palette/size/speed, lifetime/attach) and added GrayboxMinimal/GrayboxFancy sample sets with runtime toggle (`presentation.binding.sample`, see Samples~/PresentationBindings).
- Presentation bridge now reports pool stats and camera rig telemetry to HUD/telemetry; companion sync system (offset + follow lerp) keeps pooled visuals aligned without allocations and remains rewind-safe.
- Added screenshot hash utility/capture helper for ScenarioRunner/CI visual validation; updated docs for the new binding schema and samples.

## Technical Debt Tracking

**Ownership & Slices** (2025-01-27):
- **[Runtime Core]**: Core DOTS systems, registries, spatial services, rewind/time engine
- **[Tooling]**: Editor tooling, debug overlays, telemetry, CI automation
- **[Docs]**: TruthSources, guides, architecture documentation
- **[QA]**: Test suites, validation, performance benchmarking

**Reporting Cadence**: Weekly async reviews, checkpoint at end of each phase

## 2025-11-05 (Phase 2 Completion)
- Landed meta registry authoring components (`FactionAuthoring`, `ClimateHazardAuthoring`, `AreaEffectAuthoring`, `CultureAuthoring`) plus profile assets with editor menu to generate sample assets.
- Extended `DebugDisplaySystem` and telemetry streams with faction/hazard/area/culture metrics exposed in `DebugDisplayData` (`registry.*` counters now surface to HUD + telemetry).
- Added integration coverage in `MetaRegistryTests` verifying each registry rebuilds deterministically and publishes expected aggregates.
- Implemented `SoakHarness.MetaRegistrySoakHarness_RunsForMultipleTicks` playmode test to stress meta registries for 128 ticks and assert HUD/telemetry metrics remain consistent under load.
- Documented workflow updates in `Docs/Guides/UsingPureDOTSEnvironmentAuthoring.md` and refreshed `Docs/QA/PerformanceProfiles.md` with automated soak harness instructions.

## 2025-01-27 (Technical Debt Reduction - Phase 0 & Phase 1)
- **Phase 0**: Established ownership slices and reporting cadence in Progress.md
- **Phase 1 - Terraforming Hooks**: Integrated terrain version tracking across systems:
  - Added `TerrainVersion` field to `FlowFieldConfig` for flow field invalidation
  - Created `TerrainChangeProcessorSystem` to process terrain change events and increment version
  - Updated `FlowFieldBuildSystem` to check terrain version and mark layers dirty when terrain changes
  - Added `TerrainVersion` singleton to `CoreSingletonBootstrapSystem`
  - Confirmed all environment grids (Moisture, Temperature, Sunlight, Wind, Biome) already have `LastTerrainVersion` fields
- **Phase 1 - Compilation Health**: Verified compilation issues:
  - `StreamingValidatorTests` references are correct (PureDOTS.Authoring and PureDOTS.Editor.Streaming)
  - `StreamingLoaderSystem` already has `IComparer<StreamingSectionCommand>` implementation
  - Only one `StreamingCoordinatorBootstrapSystem` exists (no duplicate)
  - NetCode physics shim remains future work (no current compilation errors)
- **Phase 1 - Presentation Bridge MVP**: Enhanced existing systems with rewind safety:
- Added rewind guards to `PresentationSpawnSystem` and `PresentationRecycleSystem` to skip during playback
- Confirmed core presentation bridge systems (`PresentationSpawnSystem`, `PresentationRecycleSystem`, `PresentationBootstrapSystem`) are functional
- Introduced shared `AggregateEntity` contract + crew aggregation/presentation flow, plus workforce behaviour profiles (`AggregateBehaviorProfile`) feeding the new `VillageWorkforceDecisionSystem` for serialized AI tuning.
  - Confirmed authoring support (`PresentationRegistryAsset`, `PresentationRegistryAuthoring`) exists
  - Remaining: validation tests and sample authoring guide (future work)

## 2025-11-03
- Wired villager registry entries to carry spatial `CellId`/`SpatialVersion` (sourced via `SpatialGridResidency` when available, falling back to deterministic quantisation) and surfaced continuity counters so registry metadata reports resolved vs fallback vs unmapped villagers.
- Extended `AISensorUpdateSystem` to stamp sensor readings with the target's spatial metadata and added fallback quantisation when residency is missing, enabling downstream AI to avoid stale cell lookups.
- Tagged villager prefabs and rain cloud prefabs with `SpatialIndexedTag` so both domains participate in the spatial grid without manual setup; debug HUD spatial readouts now reflect miracle entities during playmode.
- Added transport unit components + `TransportRegistrySystem`, mirroring the spatial continuity flow so miner vessels, haulers, freighters, and wagons publish cell/version data and availability summaries.
- Delivered the first sunlight pass: `SunlightGridUpdateSystem` drives day/night direction, cloud/season scaling, and vegetation occlusion heuristics with runtime buffers that sampling APIs now consume. Moisture evaporation factors in the new shade data.

## 2025-11-02
- Landed registry continuity scaffolding: RegistryMetadata now records RegistryContinuitySnapshot via the builder API, resource/storehouse registries publish spatial counters to metadata, and RegistrySpatialSyncSystem surfaces the latest spatial grid version for downstream enforcement.
- Hooked RegistryHealthSystem into the continuity snapshot so missing spatial sync is flagged as Failure, with playmode coverage exercising the new guard.
- Drafted domain registry expansion plan (Docs/DesignNotes/RegistryDomainPlan.md) detailing villager, miracle, and logistics schemas plus upcoming validation tasks.
- Documented the package distribution model (PureDOTS as UPM/git package; game code lives under ../Godgame, ../Space4x, etc.) in Docs/Vision.md and scene setup guide.
- Upgraded villager registry system to emit spatial continuity snapshots, aggregate health/morale/energy stats, and provide richer per-entry data (health/morale/AI), with new playmode coverage validating the aggregates.
- Extended transport registries (miner vessels, haulers, freighters, wagons) with spatial continuity gating, aggregate metrics (idle/assigned counts, capacity utilisation), and continuity snapshots driven by RegistrySpatialSyncState.
- Introduced logistics request registry (LogisticsRequestRegistrySystem) to index outstanding transport jobs with continuity-aware entries and playmode coverage.
- Implemented miracle registry system with lifecycle/state aggregation and continuity snapshots, alongside playmode coverage validating entry ordering and aggregate counts.

## 2025-11-01
- Added scene view gizmo previews to `EnvironmentGridConfigAuthoring`, drawing per-channel bounds (moisture, temperature, sunlight, optional wind/biome) with labels for resolution and cell size to help designers tune grid coverage.
- Updated utilities TODO to record the environment grid gizmo tooling slice completion.

## 2025-10-28
- Audited existing registries across resource, storehouse, villager, and logistics domains; documented updated contracts (including transport registries) in `Docs/DesignNotes/ResourceRegistryPlan.md`.
- Fixed duplicate metadata bumps in resource/storehouse/villager registry systems and routed buffer rebuilds through `DeterministicRegistryBuilder.ApplyTo` so versions/ticks stay aligned.
- Added opt-in console instrumentation via `RegistryConsoleInstrumentationSystem` for headless/CI validation and wired new playmode coverage (`ResourceRegistry_ConsoleInstrumentation_LogsSummary`).
- Extended registry playmode tests with rewind guards and metadata assertions to confirm systems skip updates during playback mode.
- Kicked off spatial services planning: recorded reconnaissance + provider/registry contracts in `Docs/DesignNotes/SpatialPartitioning.md` and refreshed `Docs/TODO/SpatialServices_TODO.md` with the updated roadmap.
- Authored configurable spatial partition profile (`SpatialPartitionProfile` schema v2) plus default asset (`Assets/PureDOTS/Config/DefaultSpatialPartitionProfile.asset`), updated baker duplicate handling, and documented the scene workflow/tests.

## 2025-10-30
- Codified three-agent workflow (implementation → error/glue → documentation) with explicit hand-off expectations; pinned NetCode work as post-single-player backlog in Vision/TODO.
- Delivered the first observability bundle: `FrameTimingRecorderSystem` captures per-group timings + allocation diagnostics, with instrumentation injected into every custom system group and surfaced through `DebugDisplaySystem`.
- Added replay capture hooks via `ReplayCaptureSystem`/`ReplayCaptureStream`, enabling overlay + telemetry consumers to review the latest deterministic events ahead of rewind tooling.
- Extended HUD/telemetry output with frame timing, memory, and replay summaries; updated `DebugDisplayReader` bindings so designers can wire the data into UI layouts.
- Hardened coverage with new playmode suites for frame timing recorder, replay capture stream, and expanded `DebugDisplaySystem` assertions.
- Unified rewind gating via `TimeAwareController`, added `MoistureGridTimeAdapterSystem` + `StorehouseInventoryTimeAdapterSystem`, migrated `VillagerJobTimeAdapterSystem`, and validated the cross-domain rewind flow with `RewindIntegrationTests` (rain → moisture → villager gather/dump → rewind).

## 2025-03-XX
- Added climate authoring pipeline: `ClimateProfile` ScriptableObject, baker, and runtime `ClimateProfileData` with sensible fallbacks when no asset is present.
- Implemented `ClimateStateUpdateSystem` (EnvironmentSystemGroup) to advance seasons, day/night cycle, wind, and humidity based on profile + deterministic tick cadence.
- Introduced climate foundation tests (`ClimateStateUpdateSystemTests`) covering fallback defaults and authored profile overrides to keep regressions visible in CI.
- Updated `PureDOTS_TODO.md` meta foundations section to reflect baseline climate cadence progress.
- Extended environment foundation with mutable moisture runtime buffers plus `MoistureEvaporationSystem`/`MoistureSeepageSystem`, wiring deterministic update cadence and coverage via `MoistureGridSystemsTests`.
- Established registry discovery infrastructure: `RegistryDirectorySystem` builds a deterministic handle catalog each frame, with tests ensuring metadata mutations propagate (`RegistryDirectorySystemTests`).
- Added `RegistryDirectoryLookup` helpers and wired villager job queries + spatial rebuilds through the directory so future games can resolve registries generically without hard-coded singleton access.

## 2025-10-23
- Established runtime/system/authoring assembly definitions (`PureDOTS.Runtime`, `PureDOTS.Systems`, `PureDOTS.Authoring`).
- Ported core DOTS data components for time, history/rewind, resources, villager domains, and time control commands.
- Brought over baseline systems (custom groups, time tick/step, rewind routing) and added a bootstrap to seed deterministic singletons.
- Migrated `RewindCoordinatorSystem` to process time control commands and manage rewind/playback state, with automatic command buffer seeding.
- Ported resource gathering, deposit, storehouse inventory/withdrawal, and respawn systems into `PureDOTS.Systems`, aligning them with the new component layout.
- Added villager needs/status/job assignment systems to keep population availability, morale, and worksite targeting in sync with the new resource flow.
- Authored `Assets/Scenes/PureDotsTemplate.unity` as a reusable bootstrap scene containing time/history configs, resource/storehouse authoring, and villager prefab+spawner for quick validation.
- Added `PureDotsRuntimeConfig` and `ResourceTypeCatalog` ScriptableObjects (Assets/PureDOTS/Config) plus a config baker so teams can tune default time/history/resource settings outside of scenes.
- Introduced DOTS debugging helpers (HUD, resource gizmos) and a Unity Test Runner menu for quick playmode/editmode executions.
- Added CLI-friendly test script (`CI/run_playmode_tests.sh`) and documentation to integrate the template with automated pipelines.
- Created foundational docs (`DependencyAudit.md`, `FoundationGuidelines.md`) and headless playmode tests (`Assets/Tests/Playmode`) to stabilise the environment.
- Extended headless test suite with villager job assignment and resource deposit coverage.
- Removed optional packages (Visual Scripting, Timeline, glTFast, Collaborate, AI Navigation, Multiplayer Center) to slim the template baseline.
- Documented system ordering and developer checklist for future extensions (`Docs/SystemOrdering/SystemSchedule.md`, `Docs/DevChecklist/TemplateChecklist.md`).

## 2025-01-XX
- Created `Docs/DeprecationList.md` to track hybrid scripts slated for replacement with pure DOTS implementations.
- Identified `DotsDebugHUD` MonoBehaviour as the primary runtime hybrid script requiring migration to a pure DOTS debug system.
- Documented replacement patterns for pure DOTS debug infrastructure using singleton components and presentation bridges.
- Categorized acceptable authoring components (MonoBehaviour + Baker) and editor tools as non-deprecated patterns per Unity Entities SubScene workflow.
- Tagged `DotsDebugHUD` with `[Obsolete]` attribute and added deprecation comments.
- Implemented pure DOTS debug replacement: created `DebugDisplayData` singleton component and `DebugDisplaySystem` in `PresentationSystemGroup`.
- System populates debug data with time state (tick, paused flag), rewind state (mode, playback tick), villager counts, and storehouse resource totals.
- Used cached queries for performance, Burst-compiled for determinism.
- Added comprehensive test suite (`DebugDisplaySystemTests.cs`) validating singleton creation and data population.
- Created Unity UI presentation bridge (`DebugDisplayReader.cs`) - optional MonoBehaviour that reads singleton and updates Canvas UI.
- Added DOTS command buffer system (`DebugCommand` + `DebugCommandAuthoring`) for toggling HUD visibility from DOTS systems.
- Created optional keyboard input handler (`DebugInputHandler.cs`) for designer convenience (F1-F3 shortcuts).
- Updated `Docs/SystemOrdering/SystemSchedule.md` to document presentation group scheduling and debug system implementation.
- Added usage guide in `Docs/DeprecationList.md` explaining how designers can opt-in to debug HUD in playmode builds.

## 2025-01-XX (Registry Replacement)
- Designed DOTS-native resource and storehouse registries to replace legacy registry patterns per PureDOTS_TODO.md:40-43.
- Created `ResourceTypeIndexBlob` blob asset structure for deterministic resource type catalog (maps string IDs to ushort indices).
- Implemented `ResourceTypeIndex` baker in `PureDotsConfigBaker` to convert `ResourceTypeCatalog` ScriptableObject to runtime blob asset.
- Added `ResourceRegistry` singleton component + `ResourceRegistryEntry` buffer for indexed access to all resource sources by type.
- Added `StorehouseRegistry` singleton component + `StorehouseRegistryEntry` buffer for indexed access to all storehouses with capacity info.
- Implemented `ResourceRegistrySystem` (OrderFirst in ResourceSystemGroup) to maintain resource catalog with cached positions and unit counts.
- Implemented `StorehouseRegistrySystem` (after ResourceRegistrySystem, before ResourceDepositSystem) to maintain storehouse catalog with capacity totals.
- Both registry systems update buffers each frame with current entity state, providing efficient filtering by type/capacity without EntityManager queries.
- Added comprehensive playmode test suites (`ResourceRegistryTests.cs`, `StorehouseRegistryTests.cs`) covering singleton creation, entity spawn updates, type filtering, capacity tracking, and tick synchronization.
- Registry systems provide Burst-compatible, deterministic data for villager AI, UI systems, and job assignment logic without requiring main-thread EntityManager access.
- Documented design decisions, API contracts, and migration strategy in `Docs/DesignNotes/ResourceRegistryPlan.md`.

## 2025-02-XX (Villager Job Loop DOTS Integration)
- Authored `Docs/DesignNotes/VillagerJobs_DOTS.md` detailing component model (tickets, progress, carry buffers), fixed-step system graph, registry consumption, and testing strategy.
- Extended `ResourceRegistryEntry`/`StorehouseRegistryEntry` with ticket/reservation metadata and added reservation bootstrap system so resources and storehouses maintain DOTS-native claim data.
- Rebuilt villager job pipeline with new systems:
  - Fixed-step request/assignment/execution/delivery/interrupt passes using deterministic command buffers.
  - Event stream + history/playback systems aligned with rewind contract; added `VillagerJobTimeAdapterSystem` implementing `ITimeAware` hooks.
  - Delivery system now reserves/reconciles storehouse capacity via registries; deposit flow updated to consume `VillagerJobCarryItem` buffers.
- Updated vegetative harvest to mirror new carry buffers, ensuring all gather flows feed the same virtual inventory path.
- Added playmode test suite (`VillagerJobLoopTests.cs`) covering gather→deliver→idle loop, player-priority interrupts, and rewind restoration via `RewindState` playback.
- Synced authoring: `VillagerAuthoring` now seeds job ticket/progress/carry buffers for runtime determinism.
- Provided inline extension points (job-type modifiers, event hooks) so future job variants can extend execution logic without rewriting core loop.
- **Presentation/UI integration note:** consume `VillagerJobEventStream` for job status badges; storehouse dashboards should prefer `StorehouseRegistryEntry.TypeSummaries` (reserved + stored) and subscribe after `VillagerJobEventFlushSystem` runs to avoid redundant updates.

