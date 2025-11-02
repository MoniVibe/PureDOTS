# Runtime Lifecycle Truth-Source (PureDOTS)

## Purpose
- Establish a single reference for how PureDOTS initialises and executes DOTS systems at runtime.
- Capture best practices specific to this project: deterministic boot, rewind-friendly scheduling, performance budgeting.
- Serve as the anchor each TODO/design note points to when deciding where new systems live.

## Core Principles
- **Bootstrap is for configuration, not simulation.** Use it to register singletons, load blobs, and define system groups. All heavy logic belongs in scheduled groups.
- **Deterministic start state.** Every system must produce identical state after boot across runs (no random seeds in boot, no order-dependent writes).
- **Group-driven execution.** Systems run inside dedicated DOTS ComponentSystemGroups; we never manually tick systems in user code.
- **Rewind awareness.** Each group honours RewindState.Mode and exposes explicit phases for record/playback/catch-up.
- **Performance visibility.** Group budgets are defined and enforced; expensive work must be amortised or throttled.
- **Profile aware.** `SystemRegistry` selects which systems exist per world profile; avoid ad-hoc `World.GetOrCreateSystem` calls outside bootstrap so profile guarantees stay authoritative.

## Boot Sequence Overview
1. **Unity Startup**
   - Unity initialises PlayerLoop and Hybrid/Entities subsystems.
   - Subscenes with ConvertToEntity authoring run and generate initial entities.
2. **Bootstrap (MonoBehaviour / Subscene Script)**
   - Load ScriptableObject configs (profiles, catalogues).
   - Create BlobAssets (Environment grids, Spatial profiles, Archetype catalogues) and register singleton components.
   - Resolve active `BootstrapWorldProfile` via `SystemRegistry` (default/headless/replay; override with env var `PURE_DOTS_BOOTSTRAP_PROFILE`).
   - Construct custom system groups (EnvironmentSystemGroup, SpatialSystemGroup, etc.) and insert into update order.
   - Optionally execute one-off jobs (e.g., preloading streaming assets).
3. **Initialisation Group Phase (InitializationSystemGroup)**
   - CoreSingletonBootstrapSystem: guarantees TimeState, GameplayFixedStep, rewind, and registry singletons exist.
   - TimeSettingsConfigSystem: applies authoring overrides to TimeState.
   - GameplayFixedStepSyncSystem: mirrors TimeState.FixedDeltaTime into the GameplayFixedStep singleton and drives FixedStepSimulationSystemGroup.Timestep to keep Unity's fixed loop aligned.
   - RewindBootstrapSystem: initialises RewindState, clears history buffers.
   - EnvironmentGridBootstrapSystem: writes base grid values (moisture, temperature, wind, sunlight) from config.
   - EnvironmentEffectBootstrapSystem: converts EnvironmentEffectCatalogData into runtime channel descriptors and zeroed contribution buffers.
   - SpatialGridInitialBuildSystem: first deterministic rebuild using existing entities.
   - RegistryBootstrapSystem: populates registries with zeroed entries.
4. **Simulation Phase**
   - Execution order configured as:
     1. BuildPhysicsWorld
     2. EnvironmentSystemGroup
     3. SpatialSystemGroup
     4. GameplaySystemGroup
     5. LateSimulationSystemGroup (includes HistorySystemGroup)
     6. ExportPhysicsWorld
   - Each group has sub-ordering defined below.
5. **Presentation Phase**
   - PresentationSystemGroup reads simulation data and updates visuals/UI (guarded for rewind).

## System Group Definitions

### TimeSystemGroup
- Runs first inside InitializationSystemGroup.
- Hosts bootstrap systems that seed deterministic singletons (CoreSingletonBootstrapSystem), apply authoring overrides (TimeSettingsConfigSystem, HistorySettingsConfigSystem), and synchronise Unity's fixed step with the game timeline (GameplayFixedStepSyncSystem).
- Any new bootstrapping or deterministic configuration system must live here so downstream groups see consistent state in OnCreate.
- Reference implementation: Assets/Scripts/PureDOTS/Systems/SystemGroups.cs and CoreSingletonBootstrapSystem.cs.

### PhysicsSystemGroup
- Owns Unity Physics simulation (default Entities/Physics order).
- Runs before environment to ensure physics-driven state (velocity, collisions) is visible to environment consumers if needed.

### EnvironmentSystemGroup
- Runs every tick before spatial/gameplay; houses deterministic world-state updates.
- Expected order:
  1. EnvironmentEffectUpdateSystem (evaluates scalar/vector/pulse effects defined in EnvironmentEffectCatalogData).
  2. BiomeDerivationSystem repopulates `BiomeGridRuntimeCell` using moisture + temperature fields.
  3. Additional derivations (rainfall accumulation, etc.) consume the refreshed channels.
- Group budget: <2 ms aggregated.
- All systems check RewindState.Mode and skip logic during playback.
- Effect dispatcher writes additive contributions into channel buffers; consumers must read via EnvironmentSampling helpers to obtain base + contributions without mutating blobs.

### SpatialSystemGroup
- Maintains spatial indices and nav data.
- Typical order:
  1. SpatialGridBuildSystem
  2. Flow-field/nav helpers
  3. Debug/visualisation systems (optional)
- Runs after environment so queries use updated terrain/moisture data.
- Updates SpatialRegistryMetadata each time the grid refreshes so domain registries can cache handles for spatial queries.
- Systems that consume spatial data should [UpdateAfter(typeof(SpatialSystemGroup))] or execute inside one of its child groups.

### TransportPhaseGroup
- Manual phase group that aggregates logistics/transport systems between spatial rebuilds and gameplay logic.
- Runs inside SimulationSystemGroup after `SpatialSystemGroup` and before `GameplaySystemGroup`.
- Default systems: `LogisticsRequestRegistrySystem` plus game-specific transport registries.
- Toggle via `ManualPhaseControl.TransportPhaseEnabled`; disabled in headless worlds when transport registries are unnecessary.
- Frame timing budget: 1.25 ms (tracked as `FrameTimingGroup.Transport`).

### Streaming Section Content
- `StreamingSectionContentSystem` executes inside RecordSimulation after `StreamingLoaderSystem`. It preloads `StreamingSectionPrefabReference` and `StreamingSectionWeakGameObjectReference` entries while sections are active and releases them when the section unloads. See `Docs/Streaming_Content.md` for authoring flow.

### GameplaySystemGroup
- Houses high-level simulation domains and is expected to execute after spatial rebuilds.
- Sub-groups:
  - AISystemGroup
  - VillagerJobFixedStepGroup
  - VillagerSystemGroup
  - ResourceSystemGroup
  - VegetationSystemGroup
  - MiracleEffectSystemGroup
  - HandSystemGroup
  - ConstructionSystemGroup
- Each subgroup respects RewindState and uses double buffering for writes where required.
- When adding a new domain group, update this truth-source **and** SystemGroups.cs so the ordering remains authoritative.

### CameraPhaseGroup
- Lives inside `CameraInputSystemGroup` to process camera/input synchronisation ahead of simulation.
- Empty placeholder until the ECS camera pipeline lands, but already supports toggling/instrumentation.
- Toggle via `ManualPhaseControl.CameraPhaseEnabled`; disabled by default for headless profiles.
- Frame timing budget: 0.5 ms (shared with the legacy camera group budget).
- ECS camera pipeline (`Space4XCameraEcsSystem` + `Space4XCameraRigSyncSystem`) activates when `camera.ecs.enabled` config var is true; otherwise the legacy Mono controller (`Space4XCameraMouseController`) owns the rig.

### AISystemGroup
- Runs inside GameplaySystemGroup immediately after spatial rebuilds and before villager/resource domains.
- Hosts reusable AI modules:
  1. AISensorUpdateSystem (spatial sampling + category filters)
  2. AIUtilityScoringSystem (blob-driven action evaluation)
  3. AISteeringSystem (SOA steering state updates)
  4. AITaskResolutionSystem (writes pooled commands to AICommand queue)
- Consumers opt-in by authoring AISensorConfig, AIBehaviourArchetype, AISteeringConfig, and reading from the shared AICommandQueueTag buffer.

### VillagerJobFixedStepGroup
- Lives inside FixedStepSimulationSystemGroup.
- Executes deterministic villager job phases (ticketing, reservations, history replay) at a fixed cadence.
- GameplayFixedStepSyncSystem keeps this group aligned with TimeState.FixedDeltaTime.
- Systems that require predictable step sizes should live here and read the fixed-step singleton rather than SystemAPI.Time.DeltaTime.

### VillagerSystemGroup
- Executes after AISystemGroup and the fixed-step jobs.
- Maintains villager needs, targeting, movement, job execution, and history streaming.
- Systems must [UpdateAfter(typeof(AISystemGroup))] if they depend on sensor/utility results.

### ResourceSystemGroup
- Depends on up-to-date villager state.
- Handles resource spawning, gathering, deposit, registry writes, and telemetry.
- Resource systems that rely on storehouse or spatial data must either live in this group or add [UpdateAfter(typeof(SpatialSystemGroup))] when placed elsewhere.

### VegetationSystemGroup
- Processes vegetation lifecycle, health, reproduction, decay, and harvesting.
- Climate-dependent systems should read via EnvironmentSampling and remain scheduled after EnvironmentSystemGroup.

### MiracleEffectSystemGroup
- Runs after resource updates so miracles interact with the latest registries and spatial caches.
- Ensure miracles honour rewind by buffering commands and checking RewindState.Mode.

### HandSystemGroup
- Contains divine hand interaction, right-click routing, and presentation bridges.
- `HandInputRouterSystem` runs `OrderFirst` to resolve RMB priority requests before `DivineHandSystem` processes them. Hybrid bridges (`DivineHandInteractionBridge`) enqueue requests; future DOTS modules should do the same instead of mutating `DivineHandCommand` directly.
- Must execute after BuildPhysicsWorld (to read collision data) and before ExportPhysicsWorld.
- Input routers share state with miracles/resources via `HandInteractionState` and `ResourceSiphonState`; document changes in DivineHandCamera_TODO.md when adding new routes.

### ConstructionSystemGroup
- Runs after resources; uses spatial and registry data to plan/complete construction tasks.
- Placement logic should consult SpatialRegistryMetadata for available anchors to stay deterministic.

### LateSimulationSystemGroup & HistorySystemGroup
- LateSimulationSystemGroup is marked OrderLast inside simulation and hosts cleanup/state capture.
- HistoryPhaseGroup sits inside late simulation and is toggled via `ManualPhaseControl.HistoryPhaseEnabled`.
- HistorySystemGroup lives within HistoryPhaseGroup and records state for rewind playback.
- `PhysicsHistoryCaptureSystem` clones `PhysicsWorldSingleton` each record tick (config: `history.physics.enabled`, `history.physics.length`) ahead of history consumers.
- Systems emitting history snapshots should either live in HistorySystemGroup or [UpdateBefore(typeof(HistorySystemGroup))] to guarantee ordering.
- ReplayCaptureSystem executes here to stream recent replay events into tooling (`ReplayCaptureStream`) ahead of presentation/telemetry consumption.
- MoistureGridTimeAdapterSystem and StorehouseInventoryTimeAdapterSystem snapshot deterministic state for playback/catch-up using the shared `TimeAwareController` contract; extend this pattern for other rewind-sensitive domains.

### PresentationSystemGroup
- Post-simulation translators to rendering/UI (hand visuals, grid overlays).
- Accesses read-only data; never mutates simulation state.
- Guarded by PresentationRewindGuardSystem to pause updates during catch-up rewinds.
- FrameTimingRecorderSystem runs at the top of this group to gather per-system-group timings, allocation diagnostics, and update telemetry singletons before DebugDisplaySystem reads them.
- `MaterialOverrideSystem` copies `MaterialColorOverride` / `MaterialEmissionOverride` values into URP material property components, enabling ECS-friendly tint swaps similar to the Entities RenderSwap sample.

### Rewind Guard Systems
- EnvironmentRewindGuardSystem, SpatialRewindGuardSystem, GameplayRewindGuardSystem, and PresentationRewindGuardSystem toggle group execution based on RewindState.Mode.
- Guards run inside SimulationSystemGroup (presentation guard runs last) and are the authoritative way to pause heavy systems during playback/catch-up.
- Any new top-level group must piggy-back on an existing guard or provide its own guard system mirroring this contract.

## Bootstrap Responsibilities (Detailed)
- **Profile Loading**
  - SpatialPartitionProfile, EnvironmentGridConfig, HandCameraProfile, VillagerArchetypeCatalog, ResourceProfile, MiracleCatalog etc.
  - Loaded via ScriptableObject references defined in bootstrap Mono or subscene.
  - Converted to BlobAssets using burst-friendly bakers; registered as singleton components (ConfigSingleton<T> pattern).
- **Singleton Registration**
  - TimeState, GameplayFixedStep, RewindState, TerrainVersion, EnvironmentGrids (Moisture/Wind/Temp/Sunlight), SpatialGridState, RegistryState, SpatialRegistryMetadata singletons must exist before the first simulation tick.
- **Runtime Config**
  - `RuntimeConfigBootstrapSystem` initialises `RuntimeConfigRegistry`, loads `UserSettings/puredots.cfg`, and spawns the runtime console overlay (toggle `~`).
  - Config vars marked with `[RuntimeConfigVar]` are authoritative; systems needing live toggles should define a config var instead of custom singletons.
- **Random Seeds**
  - All random-driven systems draw seeds from SeedCatalog singleton initialised in bootstrap; ensures deterministic replays.
- **Debug Guards**
  - Add asserts verifying key singletons exist before leaving bootstrap; fail fast in dev builds.

## Runtime Rules & Best Practices
- **No synchronous heavy work in bootstrap.** Any operation >1 ms should move to an async job triggered after the initial frame (e.g., chunked nav mesh builds).
- **System construction only in bootstrap.** World.GetOrCreateSystem calls are limited to bootstrap/initialisation; avoid dynamic creation mid-game.
- **Respect world profiles.** Register new systems with appropriate `WorldSystemFilterFlags` so `SystemRegistry` can include/exclude them; update profile inclusions if a system must always exist.
- **Group-level toggles.** Use custom ComponentSystemGroup toggles (guards) to pause modules cleanly.
- **History buffers registered early.** If a system needs rewind support, register its history buffers in bootstrap so memory cost is stable.
- **Order documentation.** Each system must specify UpdateInGroup attributes referencing this truth-source; code comments should link back here.
- **Burst + Jobs compliance.** All runtime systems targeted at gameplay use Burst and job-friendly patterns; bootstrap remains minimal but can use managed code if needed (only runs once).
- **Authoring auto-copy helpers.** Use `AuthoringComponentCopyUtility` when authoring data maps 1:1 to runtime structs; it mirrors PascalCase/camelCase serialized fields and reduces bespoke baker boilerplate.

## Rewind Integration Hooks
- RewindState singleton drives behaviour: Record, Playback, CatchUp.
- Guard systems listed above enable/disable groups automatically.
- History snapshot cadence defined centrally (see Docs/DesignNotes/RewindPatterns.md).
- Bootstrap ensures history buffers are cleared on fresh load and restored on save load.
- TimeAwareController provides the unified gate for record/playback/catch-up flows; systems requiring rewind support should depend on it instead of ad-hoc `RewindState` checks.

## Platform & Burst Considerations
- Target IL2CPP/AOT: avoid reflection in Burst jobs, use static registries and [Preserve] attributes where required.
- Enforce BurstCompilerOptions.CompileSynchronously in development to surface compile errors early.
- Document job worker policies (default JobsUtility.JobWorkerCount, main-thread vs. jobs) and scheduling expectations.
- Separate hot vs. cold execution paths; throttle background systems to keep critical loops responsive.
- Link to Docs/TruthSources/PlatformPerformance_TruthSource.md (to be authored) for detailed platform/IL2CPP/Burst guidelines.

## Testing Expectations
- **Playmode smoke:** Boot + 10 s simulation without assertions.
- **Rewind cycles:** Boot ? simulate ? rewind ? simulate; state must match deterministic baseline.
- **Spatial/registry stress:** Run the spatial registry performance tests (see Assets/Tests/Playmode/SpatialRegistryPerformanceTests.cs) after large refactors to ensure hashing/order remains stable.
- **Profile harness:** Boot time measured each build; budget <250 ms cold start (PC target).
- **CI:** Integration tests run the full boot sequence; failure indicates missing singleton or group scheduling regression.

## Extending the Runtime
When adding a new system:
1. Determine required group (Physics, Environment, Spatial, Gameplay, Presentation, FixedStep, LateSimulation).
2. Ensure required singletons/configs created during bootstrap.
3. Update this truth-source if group ordering or contracts change.
4. Add integration tests covering boot + runtime behaviour.
5. Update relevant TODO/design docs to reference this truth-source.
- Use shared pooling/spawn framework (Docs/TODO/SpawnerFramework_TODO.md) instead of ad-hoc entity instantiation.

## References
- Docs/TODO/SystemIntegration_TODO.md
- Docs/TODO/SpatialServices_TODO.md
- Docs/TODO/ClimateSystems_TODO.md
- Docs/TODO/VillagerSystems_TODO.md
- Docs/TODO/ResourcesFramework_TODO.md
- Docs/TODO/MiraclesFramework_TODO.md
- Docs/TODO/TerraformingPrototype_TODO.md
- Docs/DesignNotes/SystemExecutionOrder.md
- Docs/TruthSources/RMBtruthsource.md

Keep this truth-source authoritative. All runtime lifecycle decisions should point here; update it immediately when contracts change.
