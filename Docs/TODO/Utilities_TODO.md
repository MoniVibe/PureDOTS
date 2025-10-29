# Utilities, Tooling & Ops TODO

## Goal
- Build the supporting infrastructure (tooling, tests, authoring pipeline, ops) that keeps core systems stable, observable, and designer-friendly.
- Capture cross-cutting tasks not covered by simulation-specific TODOs.
- Reference runtime expectations in `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` and integration glue in `Docs/TODO/SystemIntegration_TODO.md` when planning utilities.

## Workstreams & Tasks

### 0. Tooling & Observability
- [x] Deliver first-pass tooling bundle (debug overlay, telemetry hooks, integration harness bootstrap, deterministic replay capture, CI pipeline stubs) to unblock meta schedule. (See `FrameTimingRecorderSystem`, `ReplayCaptureSystem`, and overlay updates in `DebugDisplaySystem`.)
- [x] Implement in-game debug overlay aggregator (grids, hand state, villager stats, resource piles).
- [x] Add editor gizmos + scene view tools for moisture/temperature/light grids. *(EnvironmentGridConfigAuthoring now draws per-channel bounds with scene view labels.)*
- [x] Build telemetry hooks logging per-group frame time and entity counts (pending: job completion stats feed). (`FrameTimingStream` + telemetry export).
- [ ] Create replay recorder (input + events) for deterministic bug repro. *(ReplayCapture stream in place; input capture + diffing harness remains.)*
- [ ] Integrate runtime console commands (toggle systems, force rain, spawn villagers).
- [ ] Expose metrics to external dashboard (Grafana/Influx or equivalent).
- [x] Add allocation/pooling diagnostics (track pooled buffers usage, highlight leaks) in debug overlay. (`AllocationDiagnostics` surfaced through `DebugDisplaySystem`.)

### 1. Testing & Validation Infrastructure
- [ ] Define testing pyramid: unit, integration, playmode, performance, rewind.
- [ ] Set up dedicated `Tests/Integration` assembly for multi-system scenarios.
- [ ] Add nightly stress suite (100k entities, long-run soak, memory leak detection).
- [ ] Build deterministic replay harness comparing snapshots across runs.
- [ ] Automate regression scenes (villager loop, miracle rain, resource delivery).
- [x] Add AI module tests covering spatial sensor coverage, utility determinism, and command queue emission.
- [ ] Integrate test coverage reporting into CI.
- [ ] Populate `Docs/QA/IntegrationTestChecklist.md` with step-by-step flows (rain → villager → resource → rewind, hand router conflicts, spatial stress).
- [ ] Flesh out `EnvironmentGridTests` scaffolding with baseline assertions once environment jobs land.
- [ ] Add pooling/stress tests (spawn/despawn cycles, NativeContainer reuse) to ensure shared pools remain safe.
- [ ] Coordinate with `SpawnerFramework_TODO.md` for churn tests once spawn pipeline lands.

-### 1.5 Pooling Infrastructure (NEW)
- [x] Stand up `Nx.Pooling` runtime namespace with pooled `NativeList`/`NativeQueue` wrappers that hand back containers on dispose. `[Runtime Core]`
- [x] Provide pooled ECB writer utilities (`PooledCommandBuffer`, pooled `EntityCommandBuffer.ParallelWriter`) exposed via shared service singleton. `[Runtime Core]`
- [x] Implement prefab-based entity pool manager honouring deterministic warmup counts and rewind-safe reset (clears on playback/catch-up). `[Runtime Core]`
- [x] Expose pooling capacities via shared config (`PoolingSettingsData` → `PoolingSettingsConfig` component baked from runtime config asset). `[Data Authoring]`
- [x] Surface pooling diagnostics (borrowed counts, peak usage, leaks per tick) through `DebugDisplayData` for tooling hooks). `[Tooling/Telemetry]`
- [x] Document pool usage patterns and lifetime contract in `Docs/DesignNotes/SystemIntegration.md` section 10 (Data Layout, Pooling & Spawn Policy). `[Documentation]`
- [x] Document thematic-neutral naming/data conventions for pooled utilities so alternate content modules (crystals, vehicles, etc.) reuse them without code changes. `[Runtime Core + Documentation]`

### 2. Authoring Pipeline & Data Governance
- [x] Audit existing bakers; ensure authoring components map cleanly to runtime data.
- [x] Document ScriptableObject conventions (naming, versioning, validation).
- [x] Implement asset validation tools (e.g., ensure profiles reference valid prefabs).
- [x] Build data migration scripts for evolving blobs/configs.
- [x] Create designer-friendly editors for key assets (SpatialProfile, ClimateProfile, VillagerArchetype, MiracleProfile).
- [x] Add lint checks verifying default values and units (meters, seconds, degrees).
- [x] Document pooling-related authoring (e.g., per-type pool capacities, spawn profiles) for designers.

### 3. Performance & Memory Budgets
- [ ] Define per-system/frame budgets (Environment <2ms, Spatial <1ms, etc.) and publish in docs.
- [ ] Implement automated profiling harness capturing frame timings after key commits.
- [ ] Add memory budget monitoring (Blob allocations, NativeContainers, pooled buffers).
- [ ] Ensure all Burst jobs have `CompileSynchronously` guards in dev to catch errors early.
- [ ] Set up regression alerts when frame time exceeds budget by threshold.
- [ ] Provide sandbox scene for microbenchmarks (grid updates, flow fields, miracles).
- [ ] Profile pooled vs. non-pooled code paths; capture allocation spikes and document thresholds.

### 4. Build, Deployment & Platform Ops
- [ ] Configure build pipeline (CI) for Windows/Console/Other targets with DOTS-specific defines.
- [ ] Automate AssetBundle/Addressable content builds tied to DOTS subscenes.
- [ ] Validate headless server build for large-scale sims/testing.
- [ ] Implement save/load determinism tests (serialize snapshots, reload, compare).
- [ ] Document platform-specific quirks (physics differences, input devices) for DOTS runtime.
- [ ] Prepare integration with crash reporting (Unity Cloud Diagnostics, Sentry, etc.).
- [ ] Add IL2CPP build step (Windows or DOTS Runtime) to CI and track Burst/AOT regressions.
- [ ] Maintain `link.xml` and `[Preserve]` documentation for runtime assemblies.
- [ ] Surface `JobsUtility.JobWorkerCount` defaults and instrumentation hooks.

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
- [ ] Flesh out `EnvironmentAndSpatialValidation` guide with screenshots/examples once validation scripts land.
- [ ] Expand `GettingStarted.md` with domain-specific onboarding (villagers/resources/miracles) when systems stabilise.
- [ ] Author "PureDOTS Adoption" guide/README detailing bootstrap setup, required assets, and references to truth-sources.
- [x] Document authoring workflows for `EnvironmentGridConfig`, `SpatialPartitionProfile`, and registries (validation rules, recommended defaults).
- [x] Add `PureDOTS/Validation` menu (full and quiet) + custom inspectors for runtime config, resource catalog, and environment grid assets.
- [ ] Create `Docs/INDEX.md` or equivalent navigation page linking truth-sources, TODOs, guides, QA docs.
- [ ] Capture editor validation tasks (ScriptableObject inspectors, link.xml maintenance) as future backlog items when discovered.
- [ ] Align onboarding/docs with `SpawnerFramework_TODO.md` once framework stabilises.

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

