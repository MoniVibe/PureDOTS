# PureDOTS Meta Roadmap

## Goal
Track the Pure DOTS runtime/template work separately from game-specific features. Each phase lists the core objectives, representative tasks, and key files to commit on the meta branch.

### Phase 1 – Environment Baseline
- Implement shared environment grid jobs (moisture, temperature, wind, sunlight, storms, debris, radiation).
- Wire `FixedStepSimulationSystemGroup` to `TimeState` and add guard systems.
- Expose shared sampling helpers, update `RuntimeLifecycle_TruthSource.md` and `PlatformPerformance_TruthSource.md`.
- **Files**: `Assets/Scripts/PureDOTS/Runtime/Environment/**`, related systems/tests, truth-sources.

### Phase 2 – Spatial & Registry Core
- Finish Burst rebuild jobs, dirty detection, advanced queries.
- Integrate registries (villager/resource/logistics/miracle) via shared utilities.
- Implement pooling/spawn utilities and document SoA/pooling policy.
- **Files**: `Assets/Scripts/PureDOTS/Runtime/Spatial/**`, `.../Runtime/Registry/**`, `.../Systems/Spatial/**`, pooling helpers, docs linking to `SystemIntegration_TODO.md`.

### Phase 3 – Hand/Input Router & Runtime Glue
- Centralise RMB router and `HandInteractionState`.
- Update resources/miracles/villagers to use shared router and state.
- Confirm `SystemExecutionOrder.md` & `RuntimeLifecycle_TruthSource.md` reflect final ordering.
- **Files**: `Assets/Scripts/PureDOTS/Runtime/Hand/**`, relevant systems, truth-source updates.

### Phase 4 – Testing & Tooling Scaffold
- Flesh out `EnvironmentGridTests`, replay harness, `Docs/QA/IntegrationTestChecklist.md`.
- Deliver first tooling bundle: debug overlay, telemetry hooks, console commands, replay capture.
- Add IL2CPP/Burst CI step or scripts.
- **Files**: `Assets/Tests/Playmode/**`, `Docs/QA/**`, `Docs/TODO/Utilities_TODO.md`, tooling scripts/configs.

### Phase 5 – Authoring & Onboarding
- Finalise inspectors, validation tools, designer guides.
- Publish `Docs/INDEX.md`, adoption README, platform checklists.
- Supply minimal sample scenes/subscenes showcasing the runtime without game content.
- **Files**: `Docs/**`, `Assets/Scripts/Editor/**`, sample scenes.

### Parallel Streams
- **Game Feature Branches** (e.g., `feature/godgame`): miracles, divine hand behaviour, villager jobs, resource economy, terraforming.
- Keep these changes off the meta branch until the template is stable.

## Branch & Commit Guidance
- Meta work lives on `meta/dots-foundation`. Merge to `main` once phases stabilise.
- Feature work: `feature/<feature-name>` (e.g., godgame). Rebase on meta updates as needed.
- Tag commits by phase (`meta/env-baseline`, `meta/spatial-core`, etc.) for tracking.
- Maintain separate progress logs: `Docs/PROGRESS_Meta.md`, `Docs/PROGRESS_Game.md` (to be created).

## Status Tracking
Use this section to mark completion:
- [ ] Phase 1 – Environment Baseline
- [ ] Phase 2 – Spatial & Registry Core
- [ ] Phase 3 – Hand/Input Router & Runtime Glue
- [ ] Phase 4 – Testing & Tooling Scaffold
- [ ] Phase 5 – Authoring & Onboarding

Update the roadmap as phases progress; link commits or PRs when items complete.

