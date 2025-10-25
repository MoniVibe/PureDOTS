# Utilities, Tooling & Ops TODO

## Goal
- Build the supporting infrastructure (tooling, tests, authoring pipeline, ops) that keeps core systems stable, observable, and designer-friendly.
- Capture cross-cutting tasks not covered by simulation-specific TODOs.

## Workstreams & Tasks

### 0. Tooling & Observability
- [ ] Implement in-game debug overlay aggregator (grids, hand state, villager stats, resource piles).
- [ ] Add editor gizmos + scene view tools for moisture/temperature/light grids.
- [ ] Build telemetry hooks logging per-group frame time, entity counts, job completion stats.
- [ ] Create replay recorder (input + events) for deterministic bug repro.
- [ ] Integrate runtime console commands (toggle systems, force rain, spawn villagers).
- [ ] Expose metrics to external dashboard (Grafana/Influx or equivalent).

### 1. Testing & Validation Infrastructure
- [ ] Define testing pyramid: unit, integration, playmode, performance, rewind.
- [ ] Set up dedicated `Tests/Integration` assembly for multi-system scenarios.
- [ ] Add nightly stress suite (100k entities, long-run soak, memory leak detection).
- [ ] Build deterministic replay harness comparing snapshots across runs.
- [ ] Automate regression scenes (villager loop, miracle rain, resource delivery).
- [ ] Integrate test coverage reporting into CI.

### 2. Authoring Pipeline & Data Governance
- [ ] Audit existing bakers; ensure authoring components map cleanly to runtime data.
- [ ] Document ScriptableObject conventions (naming, versioning, validation).
- [ ] Implement asset validation tools (e.g., ensure profiles reference valid prefabs).
- [ ] Build data migration scripts for evolving blobs/configs.
- [ ] Create designer-friendly editors for key assets (SpatialProfile, ClimateProfile, VillagerArchetype, MiracleProfile).
- [ ] Add lint checks verifying default values and units (meters, seconds, degrees).

### 3. Performance & Memory Budgets
- [ ] Define per-system/frame budgets (Environment <2ms, Spatial <1ms, etc.) and publish in docs.
- [ ] Implement automated profiling harness capturing frame timings after key commits.
- [ ] Add memory budget monitoring (Blob allocations, NativeContainers, pooled buffers).
- [ ] Ensure all Burst jobs have `CompileSynchronously` guards in dev to catch errors early.
- [ ] Set up regression alerts when frame time exceeds budget by threshold.
- [ ] Provide sandbox scene for microbenchmarks (grid updates, flow fields, miracles).

### 4. Build, Deployment & Platform Ops
- [ ] Configure build pipeline (CI) for Windows/Console/Other targets with DOTS-specific defines.
- [ ] Automate AssetBundle/Addressable content builds tied to DOTS subscenes.
- [ ] Validate headless server build for large-scale sims/testing.
- [ ] Implement save/load determinism tests (serialize snapshots, reload, compare).
- [ ] Document platform-specific quirks (physics differences, input devices) for DOTS runtime.
- [ ] Prepare integration with crash reporting (Unity Cloud Diagnostics, Sentry, etc.).

### 5. Security, Safety & Stability
- [ ] Add sanity clamps for player-controlled inputs (terraforming height limits, miracle power caps).
- [ ] Sandbox scripting interfaces (if any) to prevent arbitrary code execution.
- [ ] Implement watchdog systems detecting stuck jobs or long frames.
- [ ] Harden debug commands (auth levels, disable in shipping builds).
- [ ] Ensure save files validated before load (version checks, CRC).
- [ ] Document incident response playbook (log capture, replay reproduction).

### 6. Team Process & Documentation
- [ ] Maintain living design/doc index linking truth-sources, TODOs, decision logs.
- [ ] Establish code review checklist referencing runtime/integration truth-sources.
- [ ] Create onboarding guide for new engineers (bootstrap, systems, testing basics).
- [ ] Set up weekly cross-discipline sync to surface integration risks.
- [ ] Track key decisions in decision-log with rationale and owners.
- [ ] Maintain risk register (technical debt, external dependencies, schedule risks).

### 7. Future-Proofing & Roadmap Support
- [ ] Implement feature toggles/config gating for experimental systems (terraforming, advanced miracles).
- [ ] Set up experimental branches with automated merge/backport workflows.
- [ ] Document compatibility plans for future Entities/Unity releases (upgrade guides).
- [ ] Capture deprecation paths for interim hacks or temporary components.
- [ ] Monitor DOTS community/Unity roadmaps for upcoming changes that affect PureDOTS.
- [ ] Provide schedule buffer tracking and alerts when integration slips threaten downstream work.

## Dependencies & Links
- `Docs/TODO/SystemIntegration_TODO.md`
- `Docs/TruthSources/RuntimeLifecycle_TruthSource.md`
- `Docs/TODO/SpatialServices_TODO.md`, `ClimateSystems_TODO.md`, `VillagerSystems_TODO.md`, `MiraclesFramework_TODO.md`, `ResourcesFramework_TODO.md`
- CI/CD pipeline configs (future)

## Success Criteria
- Tooling and testing infrastructure keeps pace with simulation systems; regressions caught quickly.
- Designers can iterate safely with clear authoring tools and validation.
- Performance budgets enforced automatically; team alerted when breached.
- Build pipeline delivers deterministic, stable builds across platforms.
- Documentation/truth-sources remain authoritative and up to date.

Update this TODO as utilities land and new gaps appear.
