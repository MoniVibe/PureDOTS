# PureDOTS Project Briefing

Welcome to the PureDOTS migration effort. This repository is a fresh Unity project configured with Entities 1.4 and mirrors the package environment from the legacy `godgame` repo. The long-term goal is to deliver the full GodGame experience using a pure DOTS architecture, guided by existing TruthSource documentation.

## Current State

- Packages: Manifest matches the legacy project (Entities, Burst, Collections, URP, Coplay, MCP).
- TODO: `PureDOTS_TODO.md` outlines first setup tasks (assemblies, bootstrap, system migration, authoring).
- No gameplay systems have been ported yet—this repo starts clean.

## Mission for Primary Agent

1. Establish core DOTS infrastructure:
   - Create runtime/system/authoring asmdefs.
   - Implement a custom world bootstrap with fixed-step, simulation, and presentation groups.
   - Seed baseline singleton components (time state, history settings) to anchor determinism/rewind.

2. Port reusable DOTS components/systems from the legacy project:
   - Components: resources, villagers, time, history, input.
   - Baker/authoring scripts for SubScene workflows.
   - Systems already DOTS-native (time step, rewind core, resource gathering).

3. Document and validate:
   - Update `PureDOTS_TODO.md` as tasks complete.
   - Note assumptions/deviations in `Docs/Progress.md` (create if absent).
   - Keep alignment with TruthSources for design intent.

## Key Principles

- Pure DOTS: avoid `WorldServices`/service locators; use singleton components, buffers, and systems.
- Determinism & rewind remain central—use existing `TimeState`, `RewindState`, etc. as references.
- Presentation should be hybrid-friendly but minimal—simulation logic belongs in DOTS.
- Salvage carefully: port DOTS-ready code; reimplement hybrid logic for the new architecture.

## Suggested Starting Checklist

- [ ] Set up `PureDOTS.Runtime`, `PureDOTS.Systems`, `PureDOTS.Authoring` asmdefs.
- [ ] Add/refine `PureDotsWorldBootstrap` for the new project.
- [ ] Port `TimeState`, `RewindState`, `HistorySettings` components and associated systems.
- [ ] Create a simple DOTS-only test SubScene to validate the loop.

Coordinate with TruthSources and the legacy repo for reference data and design constraints. The focus is laying a clean foundation—future agents can expand gameplay domains once the base is solid.

## How to Adopt This Runtime in a New Game

1. **Read the Truth Sources**  
   - `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` – system groups, determinism contracts.  
   - `Docs/TruthSources/PlatformPerformance_TruthSource.md` – IL2CPP, Burst, worker policy.  
   - `Docs/TODO/SystemIntegration_TODO.md` – active integration tasks and shared components.  
   - Review domain TODOs (Spatial, Climate, Villagers, Resources, Miracles) for feature-specific expectations.

2. **Authoring Assets Required**  
   - **Minimum**: `PureDotsConfigAuthoring` GameObject with `PureDotsRuntimeConfig` asset containing `ResourceTypes` catalog (at least one entry)
   - **Optional**: `EnvironmentGridConfig` ScriptableObject (terrain bounds, grid resolution, climate defaults)  
   - **Optional**: `SpatialPartitionProfile` ScriptableObject (cell size, provider selection)  
   - **Optional**: `HandCameraInputProfile`, `ResourceTypeCatalog`, `VegetationSpeciesCatalog`, and registry-related profiles as needed  
   - Bake these via SubScenes/Bakers; see asset validation notes in `Docs/Guides/Authoring/` (added below).  
   - **See**: `Docs/QA/BootstrapAudit.md` for complete bootstrap coverage and authoring requirements

3. **Bootstrap Steps**  
   - ✅ **Automatic**: `PureDotsWorldBootstrap` creates world and system groups automatically (no MonoBehaviour required)
   - ✅ **Automatic**: `CoreSingletonBootstrapSystem` seeds all core singletons (time, rewind, spatial, registries, navigation)
   - ✅ **Automatic**: System groups are registered and sorted automatically
   - ✅ **Automatic**: `FixedStepSimulationSystemGroup` is configured with 60 FPS timestep
   - Include `link.xml` and Burst options per Platform Performance truth-source when creating builds

4. **Project Setup Checklist**  
   - Enable Burst, Entities Collections, and Jobs packages (already in manifest).  
   - Set Player Settings → Scripting Backend to IL2CPP for release builds; verify Managed Stripping Level is compatible with `link.xml`.  
   - Import core TODO files into your workflow (`PureDOTS_TODO.md`, domain TODOs) to track remaining work.  
   - Configure CI build/validation following `Docs/TODO/Utilities_TODO.md` guidelines (debug overlay, integration harness, deterministic replay).

5. **Where to Ask Questions**  
   - Start with `Docs/INDEX.md` (navigation hub) once present.  
   - Domain-specific questions: check design notes (`Docs/DesignNotes/*.md`) and QA checklists (`Docs/QA/*.md`).  
   - Update TODO checkboxes and design notes when new features land to keep the runtime reusable.
