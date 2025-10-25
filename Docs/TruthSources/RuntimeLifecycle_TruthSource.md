# Runtime Lifecycle Truth-Source (PureDOTS)

## Purpose
- Establish a single reference for how PureDOTS initialises and executes DOTS systems at runtime.
- Capture best practices specific to this project: deterministic boot, rewind-friendly scheduling, performance budgeting.
- Serve as the anchor each TODO/design note points to when deciding where new systems live.

## Core Principles
- **Bootstrap is for configuration, not simulation.** Use it to register singletons, load blobs, and define system groups. All heavy logic belongs in scheduled groups.
- **Deterministic start state.** Every system must produce identical state after boot across runs (no random seeds in boot, no order-dependent writes).
- **Group-driven execution.** Systems run inside dedicated DOTS `ComponentSystemGroup`s; we never manually tick systems in user code.
- **Rewind awareness.** Each group honours `RewindState.Mode` and exposes explicit phases for record/playback/catch-up.
- **Performance visibility.** Group budgets are defined and enforced; expensive work must be amortised or throttled.

## Boot Sequence Overview
1. **Unity Startup**
   - Unity initialises PlayerLoop and Hybrid/Entities subsystems.
   - Subscenes with `ConvertToEntity` authoring run and generate initial entities.
2. **Bootstrap (MonoBehaviour / Subscene Script)**
   - Load ScriptableObject configs (profiles, catalogues).
   - Create BlobAssets (Environment grids, Spatial profiles, Archetype catalogues) and register singleton components.
   - Construct custom system groups (`EnvironmentSystemGroup`, `SpatialSystemGroup`, etc.) and insert into update order.
   - Optionally execute one-off jobs (e.g., preloading streaming assets).
3. **Initialisation Group Phase (`InitializationSystemGroup`)**
   - `TimeStateSetupSystem`: sets `TimeState`, `GameplayFixedStep`, resets tick counters.
   - `RewindBootstrapSystem`: initialises `RewindState`, clears history buffers.
   - `EnvironmentGridBuildSystem`: writes base grid values (moisture, temperature, wind, sunlight) from config.
   - `SpatialGridInitialBuildSystem`: first deterministic rebuild using existing entities.
   - `RegistryBootstrapSystem`: populates registries with zeroed entries.
4. **Simulation Phase**
   - Execution order configured as:
     1. `PhysicsSystemGroup`
     2. `EnvironmentSystemGroup`
     3. `SpatialSystemGroup`
     4. `GameplaySystemGroup`
     5. `PresentationSystemGroup`
   - Each group has sub-ordering defined below.
5. **Presentation Phase**
   - Hybrid/graphics bridging systems read simulation data and update visual state.

## System Group Definitions
### PhysicsSystemGroup
- Owns Unity Physics simulation (build-in order is left as default Entities/Physics).
- Runs before environment to ensure physics-driven state (velocity, collisions) is visible to environment consumers if needed.

### EnvironmentSystemGroup (NEW)
- Runs every tick before gameplay; houses deterministic world-state updates.
- Update order (example):
  1. `ClimateStateUpdateSystem`
  2. `SunlightGridUpdateSystem`
  3. `TemperatureGridUpdateSystem`
  4. `WindFieldUpdateSystem`
  5. `MoistureEvaporationSystem`
  6. `MoistureSeepageSystem`
  7. `MoistureRainSystem`
  8. `BiomeDeterminationSystem`
- Group budgets: <2ms per frame aggregated.
- All systems check `RewindState.Mode` and skip logic during playback.

### SpatialSystemGroup
- Maintains spatial indices and nav data.
- Typical order:
  1. `SpatialGridRebuildSystem`
  2. `FlowFieldBuildSystem`
  3. `SpatialDebugSystem` (optional, toggled)
- Runs after environment so queries use updated terrain/moisture data.

### GameplaySystemGroup
- Houses core simulation (villagers, vegetation, resources, miracles, AI).
- Sub-groups recommended: `VillagerSystemGroup`, `VegetationSystemGroup`, `ResourceSystemGroup`, `MiracleSystemGroup`.
- Each sub-group respects `RewindState` and uses double-buffering for writes where required.

### PresentationSystemGroup
- Post-simulation translators to rendering/UI (hand visuals, grid overlays).
- Accesses read-only data; never mutates simulation state.

## Bootstrap Responsibilities (Detailed)
- **Profile Loading**
  - `SpatialPartitionProfile`, `EnvironmentGridConfig`, `HandCameraProfile`, `VillagerArchetypeCatalog`, `ResourceProfile`, `MiracleCatalog` etc.
  - Loaded via `ScriptableObject` references defined in bootstrap Mono or subscene.
  - Converted to BlobAssets using burst-friendly bakers; registered as singleton components (`ConfigSingleton<T>` pattern).
- **Singleton Registration**
  - `TimeState`, `GameplayFixedStep`, `RewindState`, `TerrainVersion`, `EnvironmentGrids` (Moisture/Wind/Temp/Sunlight), `SpatialGridState`, `RegistryState` singletons must exist before first simulation tick.
- **Random Seeds**
  - All random-driven systems draw seeds from `SeedCatalog` singleton initialised in bootstrap; ensures deterministic replays.
- **Debug Guards**
  - Add asserts verifying key singletons exist before leaving bootstrap; fail fast in dev builds.

## Runtime Rules & Best Practices
- **No synchronous heavy work in bootstrap.** Any operation >1ms moved to async job triggered after initial frame (e.g., chunked nav mesh builds).
- **System construction only in bootstrap.** `World.GetOrCreateSystem` calls are limited to bootstrap/initialisation; avoid dynamic creation mid-game.
- **Group-level toggles.** Use custom `ComponentSystemGroup` toggles (e.g., `EnableSimulation`, `EnablePresentation`) to pause modules cleanly.
- **History buffers registered early.** If a system needs rewind support, register its history buffers in bootstrap so memory cost is stable.
- **Order documentation.** Each system must specify `UpdateInGroup` attribute referencing truth-source order; code comments link back to this doc.
- **Burst + Jobs compliance.** All runtime systems targeted at gameplay use Burst and job-friendly patterns; bootstrap remains minimal but can use managed code if needed (only runs once).

## Rewind Integration Hooks
- `RewindState` singleton drives behaviour: `Record`, `Playback`, `CatchUp`.
- Each group adds guard systems:
  - `EnvironmentRewindGuardSystem`
  - `SpatialRewindGuardSystem`
  - `GameplayRewindGuardSystem`
- History snapshot cadence defined centrally (see `RewindPatterns.md`).
- Bootstrap ensures history buffers are cleared on fresh load and restored on save load.

## Testing Expectations
- **Playmode smoke**: Boot + 10s simulation without assertions.
- **Rewind cycles**: Boot → simulate → rewind → simulate; state must match deterministic baseline.
- **Profile harness**: Boot time measured each build; budget <250ms cold start (PC target).
- **CI**: Integration tests run the full boot sequence; failure indicates missing singleton or group scheduling regression.

## Extending the Runtime
When adding a new system:
1. Determine required group (Physics, Environment, Spatial, Gameplay, Presentation).
2. Ensure required singletons/configs created during bootstrap.
3. Update this truth-source if group ordering or contracts change.
4. Add integration tests covering boot + runtime behaviour.
5. Update relevant TODO/design docs to reference this truth-source.

## References
- `Docs/TODO/SystemIntegration_TODO.md`
- `Docs/TODO/SpatialServices_TODO.md`
- `Docs/TODO/ClimateSystems_TODO.md`
- `Docs/TODO/VillagerSystems_TODO.md`
- `Docs/TODO/ResourcesFramework_TODO.md`
- `Docs/TODO/MiraclesFramework_TODO.md`
- `Docs/TODO/TerraformingPrototype_TODO.md`
- `Docs/DesignNotes/SystemExecutionOrder.md` (to be authored)
- `Docs/TruthSources/RMBtruthsource.md`

Keep this truth-source authoritative. All runtime lifecycle decisions should point here; update it immediately when contracts change.
