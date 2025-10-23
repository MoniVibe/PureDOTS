Pure DOTS Project Kickoff
==========================

This file captures the first wave of work for the new PureDOTS Unity project. The goals are to establish a clean DOTS-first architecture while salvaging useful pieces from the legacy `godgame` repository where practical.

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
   - [ ] Re-implement remaining gameplay logic in pure DOTS (villager AI, resource economy, time control) referencing TruthSource contracts rather than hybrid adapters.
   - [ ] Ensure each domain has deterministic update groups and clear scheduling.
   - [ ] Add debugging/visualisation systems (HUD, gizmos) to inspect DOTS state during iteration.

4. Service/Registry Replacement
   - [ ] Replace `WorldServices`/`RegistrySystems` patterns with DOTS singletons and buffer queries from the start.
   - [ ] Port domain-specific registry systems (resources, storehouses, villagers, bands) using DOTS buffers only—no bridge shims.
   - [ ] Build thin bridging layers only if legacy content/prefabs require temporary compatibility.

5. Presentation & Hybrid Strategy
   - [ ] Define how visual components hook into DOTS data (e.g., companion components, Entities Graphics) with minimal MonoBehaviour glue.
   - [ ] Convert key prefabs/scenes from the old project into SubScenes or hybrid workflows aligned with the new architecture.
   - [ ] Plan tooling for designers/artists early (e.g., Inspector/UI for DOTS data).

6. Testing & Tooling
   - [ ] Stand up a basic automated test suite (PlayMode/Runtime) exercising time, resource, and villager systems.
   - [ ] Port useful editor tooling (debug HUDs, history inspectors) from the old project once DOTS data structures stabilize.
   - [x] Maintain a running `Docs/Progress.md` log aligning with TruthSources for future contributors.
   - [ ] Draft CI/test-runner scripts for DOTS builds and playmode runs.

7. Migration Plan & Asset Salvage
   - [ ] Inventory legacy assets/code worth porting (prefabs, ScriptableObjects, DOTS-ready scripts) and import them gradually.
   - [ ] Note hard dependencies in the legacy project that need counterpart implementations (e.g., job board, needs systems) before those assets can be reused.
   - [ ] Establish a deprecation list for hybrid scripts that will be replaced rather than ported.

Keep this file updated as tasks complete or new requirements surface. Tie progress back to existing TruthSource documents to stay aligned with the long-term vision.
