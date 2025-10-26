# System Integration TODO (Cross-System Glue)

## Goal
- Align spatial, environment, simulation, and interaction systems so implementation teams share consistent data contracts, execution order, and rewind semantics.
- Remove duplicated definitions (e.g., grids, profiles) and provide a single source of truth per concern.
- Surface required refactors/improvements in existing TODOs to ensure future work lands smoothly without integration regressions.

## Snapshot Summary
- Shared environment data (`MoistureGrid`, `SunlightGrid`, `WindField`, `TemperatureGrid`) currently specified in both `ClimateSystems` and `VegetationSystems` TODOs; needs centralisation.
- Input chain (Divine Hand, Miracles, Resource siphon) must share one RMB router, cooldown logic, and history logging.
- Spatial grid feeds Villagers, Resources, Miracles, Vegetation sensors; execution order + component naming must be consistent.
- Rewind strategy differs per document; we need common patterns (snapshot cadence, command replay, deterministic ordering).
- Terraforming hooks require terrain versioning propagated to all downstream consumers.

## Workstreams & Tasks

### 0. Baseline Audit (Complete ASAP)
- [x] Verify current codebase for duplicate component definitions (`MoistureGrid`, `SunlightGrid`, `ClimateState`, `FlowFieldData`). (See `Docs/DesignNotes/SystemIntegration_BaselineAudit.md`)
- [x] Catalogue all ScriptableObject profiles across TODOs (SpatialPartitionProfile, ClimateProfile, VegetationSpeciesCatalog, ResourceProfile, HandCameraProfile, MiracleProfile) and note ownership + namespace. (Documented in `SystemIntegration_BaselineAudit.md`)
- [x] Document existing `SystemGroup` hierarchy (initialisation, simulation, presentation) to understand available integration points. (Captured in `SystemIntegration_BaselineAudit.md` & `SystemExecutionOrder.md`)
- [x] Confirm status of `RewindState`, `TimeState`, `GameplayFixedStep` implementations. (`SystemIntegration_BaselineAudit.md` snapshot)

### 1. Shared Data Contracts & Code Organisation
- [x] Create `Assets/Scripts/PureDOTS/Runtime/Environment/EnvironmentGrids.cs` defining:
  - `MoistureGrid`, `MoistureGridBlob`
  - `TemperatureGrid`, `TemperatureGridBlob`
  - `WindField`, `WindFieldBlob`
  - `SunlightGrid`, `SunlightGridBlob`
  - `BiomeGrid` (optional wrapper for biome types)
- [x] Move/align component definitions referenced in Vegetation + Climate TODOs to this shared file.
- [x] Introduce `EnvironmentGridConfig` ScriptableObject (or extend `ClimateProfile`) as single authoring entry point; deprecate duplicate config fields elsewhere. (`EnvironmentGridConfig.cs`)
- [x] Document namespace & folder layout so systems reference `PureDOTS.Environment` rather than duplicating definitions. (`Docs/DesignNotes/SystemIntegration.md`)
- [x] Ensure all grids expose consistent sampling helpers (`GetCellIndex`, `SampleBilinear`, `WriteCell`). (`EnvironmentGridMath`)
- [x] Implement data-driven environment effect pipeline (scalar/vector/pulse) so downstream TODOs consume a shared cadence (`EnvironmentEffectUpdateSystem`).
- [x] Add unit tests in `EnvironmentGridTests` covering sampling helpers and effect cadence.
- [x] Extend shared environment coverage to additional effects (magnetic storms, debris fields, solar radiation) and surface helper APIs for each.

### 2. System Group & Execution Order Alignment
- [x] Define dedicated `EnvironmentSystemGroup` (runs before gameplay simulation):
  1. `EnvironmentEffectUpdateSystem`
  2. `BiomeDeterminationSystem`
- [x] Schedule `VegetationNeedsSystem`, `VegetationGrowthModifierSystem`, `ResourceNodeUpdateSystem`, `MiracleEffectSystemGroup` to run **after** environment group to consume fresh data. (_Enforced via `SystemGroups.cs` ordering: GameplaySystemGroup → VegetationSystemGroup/MiracleEffectSystemGroup run after Spatial & Environment groups._)
- [x] Explicitly register `SpatialGridBuildSystem` ahead of consumer groups (`VillagerSensorUpdateSystem`, `ResourceHandSiphonSystem`, `MiracleTargetingSystem`). (`SpatialGridBuildSystem` now `OrderFirst` under `SpatialSystemGroup`)
- [x] Insert shared `AISystemGroup` between spatial and villager gameplay groups; hosts reusable sensor/utility/steering/task modules consumed by villagers/resources/miracles.
- [x] Write `Docs/DesignNotes/SystemExecutionOrder.md` summarising ordering for future contributors.
- [x] Hook `FixedStepSimulationSystemGroup` to `TimeState`/`GameplayFixedStep` so deterministic ticks use Unity's fixed-step loop rather than manual timing. (`GameplayFixedStepSyncSystem` + playmode coverage in `TimeStateTests`)
- [x] Document fixed-step linkage and group ordering in `SystemExecutionOrder.md` + `RuntimeLifecycle_TruthSource.md`.
- [x] Publish `Docs/DesignNotes/RewindPatterns.md` and wire guard systems (`EnvironmentRewindGuardSystem`, `SpatialRewindGuardSystem`, `GameplayRewindGuardSystem`, `PresentationRewindGuardSystem`) so every group honours `RewindState`.
- [x] Ensure all TODOs that describe system order link back to `RuntimeLifecycle_TruthSource.md` and this document (breadcrumb at top of each TODO).

### 3. Input & Interaction Cohesion
- [x] Unify RMB router priority table in one place (`HandInputRouterSystem`), referencing both resource and miracle flows.
- [x] Ensure Divine Hand, Resources, Miracles share `HandInteractionState`/`ResourceSiphonState` data so they canג€™t diverge. (`HandInteractionComponents.cs` + `DivineHandSystem` sync)
- [ ] Add integration tests covering: hand holding resource + miracle token (ensure deterministic resolver), hand dumps to storehouse after miracle charge, etc.
- [ ] Centralise gesture & siphon feedback events within `HandPresentationBridge` so VFX/audio subscribe to single stream.
- [x] Update `DivineHandCamera_TODO`, `MiraclesFramework_TODO`, `ResourcesFramework_TODO` to reference shared router + state components.

### 4. Registries & Spatial Queries
- [ ] Standardise registry component format: `struct RegistryEntry<TTag>` with `Entity`, `float3 Position`, `int CellIndex`, `NativeBitField Flags` for eligibility.
- [ ] Ensure `ResourcePileRegistry`, `VegetationRegistry`, `StorehouseRegistry`, `MiracleEffectRegistry` share base utilities (`RegistryCommon.cs`).
- [x] Publish `RegistryMetadata`/`RegistryHandle` pattern so spatial systems and AI modules resolve registries generically (`CoreSingletonBootstrapSystem` seeds handles; `SpatialRegistryMetadata` caches them per grid rebuild).
- [x] Add reusable spatial query descriptors, filters, and Burst `SpatialKNearestBatchJob` so villager/resource/miracle sensors consume a shared pipeline.
- [x] Add stress tests verifying combined spatial queries (villager sensors + miracles + resource merging) stay within frame budget. (_See `Assets/Tests/Playmode/SpatialRegistryPerformanceTests.cs` for baseline coverage._)

### 5. Rewind & History Alignment
- [x] Create `RewindPatterns.md` describing approved strategies (Snapshot Cadence, Command Replay, Deterministic Rebuild).
- [x] Enforce shared history sample structures: `struct GridHistorySample`, `struct InteractionHistorySample`, etc. (`HistoryComponents.cs`)
- [x] Implement shared utility for deterministically sorting entity sets before applying state (use `Entity.Index`, `Entity.Version`). (`TimeAwareUtility.SortEntities`)
- [x] Require each major system to expose `Record`, `Playback`, `CatchUp` behaviours via common interface (e.g., `ITimeAware` extension helpers). (Implemented `TimeAwareController` gating + new adapters in `MoistureGridTimeAdapterSystem` and `StorehouseInventoryTimeAdapterSystem`; `VillagerJobTimeAdapterSystem` now uses the shared flow.)
- [x] Add cross-system rewind playmode test: cast miracle (rain) ג†’ increases moisture ג†’ villagers gather wood ג†’ dump resources ג†’ rewind; verify all subsystems restore correctly. (`RewindIntegrationTests` exercises miracle moisture, resource delivery, and rewind playback.)

### 6. Terraforming Hooks Propagation
- [ ] Confirm `TerrainVersion` increment triggers:
  - `FlowFieldData.TerrainVersion`
  - `EnvironmentGrids.LastTerrainVersion` (Moisture, Temperature, Sunlight, Biome)
  - `SpatialGrid` optional `YStrataVersion`.
- [x] Provide `TerrainChangeEventBuffer` delivering notifications to Vegetation, Resource Nodes, Pathfinding. (`TerrainChangeEvent` buffer in `TerrainComponents.cs`)
- [ ] Update existing TODOs to reference this contract rather than each defining bespoke hooks.

### 7. Testing & Tooling
- [x] Build `SystemIntegrationPlaymodeTests.cs` covering multi-system flows (hand siphon during rain, villager reacting to climate change, miracle effect on resource pile). (_Initial grid sampling regression added; extend with full flows._)
- [ ] Create environment debug overlay aggregator showing: wind vectors, moisture heatmap, resource piles, flow field cells, divine hand state.
- [ ] Configure automated nightly run that executes performance suites for spatial grid, environment, villagers, miracles, resources concurrently.
- [ ] Log integration metrics to shared dashboard (e.g., Entities count, grid update times, input latency).

### 8. Documentation & Communication
- [x] Draft `Docs/DesignNotes/SystemIntegration.md` summarising contracts, naming rules, system order, testing approach.
- [ ] Update each subsystem TODO with cross-links to the integration doc and relevant tasks (avoid duplication but reference section IDs).
- [ ] Host alignment review with leads from gameplay, simulation, presentation to sign off shared contracts.

### 9. Platform / Burst / AOT Readiness
- [ ] Audit runtime for IL2CPP/AOT safety (no reflection in jobs, provide type registration helpers, add `[Preserve]` where needed).
- [ ] Document Burst/IL2CPP guidelines in a new truth-source (`PlatformPerformance_TruthSource.md`).
- [ ] Add IL2CPP build configuration checklist (linker settings, `link.xml`, scripting backend) and automate a test build in CI.
- [ ] Define job worker count policy (`JobsUtility.JobWorkerCount`) and thread-affinity expectations; document in runtime truth-source.
- [ ] Plan hot vs. cold path execution: group critical systems, throttle cold/background systems, ensure doc coverage.
- [ ] Ensure `BurstCompilerOptions` (CompileSynchronously in dev) enforced so Burst errors are caught early.
- [ ] Instrument job scheduling logs (optional) to verify worker thread utilisation.

### 10. Data Layout, Pooling & Spawn Policy
- [ ] Document SoA (struct-of-arrays) expectations for all high-volume systems (villagers, resources, miracles, vegetation, environment state).
- [x] Implement shared pooling utilities (NativeList/NativeQueue pools, entity spawn/despawn pools) to avoid per-system duplication.
- [x] Define spawn/despawn policy and command sequence (use pooled ECBs, deterministic ordering) for all systems.
- [ ] Add guidelines for double-buffering, ring buffers, and history capture so hot paths stay cache friendly.
- [x] Update subsystem TODOs (resources, miracles, vegetation, etc.) to reference pooling utilities and SoA rules.
- [ ] Define reusable AI behaviour modules (sensors, scoring, steering, task selectors) that consume shared data; document how specialised behaviours opt in via marker components/archetype data.
- [x] Keep `SpawnerFramework_TODO.md` in sync and ensure all systems adopt the shared spawn pipeline.
- [x] Author pooled diagnostics exposure: add `PoolingDiagnostics` singleton updated by pooling service and consumed by tooling overlay.
- [x] Require pooling config blob baked from `PoolingSettingsData` (`PureDotsRuntimeConfig`) and consumed by pooling coordinator system.
- [x] Create rewind hooks ensuring `Nx.Pooling` clears/rewinds pools deterministically when `RewindState` enters playback/catch-up.

### 11. Content Neutrality & Modularity
- [ ] Audit runtime namespaces and component names to remove theme-specific terminology; prefer neutral terms (e.g., `GrowthNode`, `ResourceProducer`) so modules can map to vegetation/crystals/other content via data.
- [ ] Ensure authoring assets (profiles, baker inputs) capture behaviour through data-driven parameters, enabling alternate themes without code changes.
- [ ] Provide extension hooks (interfaces/events) for domain modules to plug into shared spawn, growth, and registry pipelines without modifying foundational code.
- [ ] Document neutrality guidelines in `Docs/DesignNotes/SystemIntegration.md` and propagate references into subsystem TODOs to remind teams to keep shared code theme-agnostic.
- [ ] Add regression checks (lint/tests) ensuring shared layer naming stays neutral when new modules land.

### 12. Slices & Ownership Alignment
- [ ] Define meta-system slices (e.g., `Runtime Core`, `Data Authoring`, `Tooling/Telemetry`, `QA/Validation`) and record owners + contact cadence in `Docs/DesignNotes/SystemIntegration.md`.
- [ ] Tag existing TODO tasks with their slice responsibility (e.g., `[Runtime Core]`, `[Tooling]`) so work streams remain balanced and discoverable.
- [ ] Establish lightweight governance process: monthly review per slice to ensure interfaces remain stable and content-neutral.
- [ ] Publish onboarding notes clarifying which slice new features belong to, and how to request new shared utilities without violating neutrality.
- [ ] Capture alignment between slices and CI/testing responsibilities (who maintains stress tests, telemetry pipelines, authoring validation).

## Improvement Suggestions (Per Existing TODO)
- **SpatialServices_TODO**: add explicit note that grid components live in `EnvironmentGrids.cs`; mention `TerrainVersion` awareness.
- **VillagerSystems_TODO**: reference `EnvironmentSystemGroup` for climate/moisture sampling; ensure sensor refresh respects `EnvironmentGrids.LastUpdateTick`.
- **DivineHandCamera_TODO**: replace local router definitions with pointer to shared `HandInputRouterSystem`; include dependency on `HandInteractionState`.
- **VegetationSystems_TODO**: remove duplicate grid definitions once shared file exists; consume `BiomeGrid` for propagation logic.
- **MiraclesFramework_TODO**: route area queries through shared spatial helper; document integration with `EnvironmentSystemGroup` (rain ג†’ moisture).
- **ResourcesFramework_TODO**: adopt registry/common structures; ensure siphon uses shared hand state and grid sampling helpers.
- **ClimateSystems_TODO**: coordinate with shared environment grids; highlight that moisture/temperature data is authoritative source for vegetation/resources.
- **TerraformingPrototype_TODO**: reference `TerrainChangeEventBuffer` & `TerrainVersion` contracts, avoid redefining moisture/climate structures.
- **All TODOs**: cross-link to `RuntimeLifecycle_TruthSource.md`, `SystemIntegration_TODO.md`, and note dependencies on shared environment cadence, registry utilities, and pooling/spawn policies.

## Open Questions
1. Who owns `EnvironmentGrids` namespace (environment team vs. simulation core)?
2. Do we expose grid sampling via Burst-friendly static class or SystemAPI extension?
3. Should `HandInteractionState` become single component consumed by resources and miracles, or remain separate with bridging util?
4. How do we synchronise nav/pathfinding rebuilds with terrain updates without blocking the frame? (Job graph design)
5. Which team maintains cross-system integration tests (QA automation vs. gameplay engineers)?

## Next Steps (Suggested Order)
1. Complete Workstream 0 audit; gather findings in confluence/Docs.
2. Stand up shared `EnvironmentGrids` components + config asset (Workstream 1).
3. Define `EnvironmentSystemGroup` and update TODOs to reference new order (Workstream 2).
4. Consolidate RMB router and hand state (Workstream 3).
5. Schedule integration testing tasks and create initial playmode test harness (Workstream 7).
6. Iterate on documentation and cross-link existing TODOs.

Keep this TODO up to date as integration tasks land. Treat it as the authoritative glue plan to prevent subsystem drift. Update referenced TODOs once shared contracts are implemented.


