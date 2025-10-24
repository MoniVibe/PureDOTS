# PureDOTS Progress Log

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
