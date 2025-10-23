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
*** End Patch
