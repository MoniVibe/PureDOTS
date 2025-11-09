# Getting Started with PureDOTS Engineering

Updated: 2025-10-25

This primer helps new engineers ramp onto the PureDOTS runtime. It summarises system group layout, shared singleton expectations, rewind policies, and required editor configuration.

## System Groups Overview
- **InitializationSystemGroup**: Bootstraps singletons and authoring state. Runs once unless explicitly re-enabled.
- **EnvironmentSystemGroup**: Updates moisture, temperature, wind, sunlight, biome data each cadence. Runs after physics (`BuildPhysicsWorld`).
- **SpatialSystemGroup**: Maintains deterministic spatial indices (hashed grid). `SpatialGridBuildSystem` runs `OrderFirst` and must complete before gameplay consumers.
- **GameplaySystemGroup**: Hosts domain sub-groups (`VillagerSystemGroup`, `ResourceSystemGroup`, `VegetationSystemGroup`, `MiracleEffectSystemGroup`, etc.) that depend on fresh environment + spatial data.
- **LateSimulationSystemGroup**: Cleanup/history snapshot phase (rewind logging, buffer flushing).
- **PresentationSystemGroup**: Hybrid bridge to rendering/UI (read-only from DOTS state).

See `Docs/DesignNotes/SystemExecutionOrder.md` for the precise `[UpdateInGroup]` ordering and dependency list.

## Shared Singletons (Must Exist Before Simulation)

**Automatically Bootstrapped** (via `CoreSingletonBootstrapSystem`):
- ✅ `TimeState`, `HistorySettings`, `RewindState` (time engine / rewind hooks)
- ✅ `SpatialGridConfig`, `SpatialGridState`, and spatial buffers (cell ranges, entries, staging)
- ✅ Registry singletons (`ResourceRegistry`, `StorehouseRegistry`, `VillagerRegistry`, etc.)
- ✅ `FlowFieldConfig` and navigation buffers
- ✅ `AICommandQueue`, telemetry streams, presentation command queue
- ✅ Configuration defaults (`VillagerBehaviorConfig`, `ResourceInteractionConfig`)

**Requires Authoring Assets**:
- ⚠️ `ResourceTypeIndex` - Created by `PureDotsConfigAuthoring` (required for resource systems)
- ⚠️ `ResourceRecipeSet` - Created by `PureDotsConfigAuthoring` (optional, for resource processing)
- ⚠️ `EnvironmentGridConfigData` - Created by `EnvironmentGridConfigAuthoring` (optional, for environment systems)
- ⚠️ `TimeSettingsConfig`, `HistorySettingsConfig`, `PoolingSettingsConfig` - Optional configs

See `Docs/QA/BootstrapAudit.md` for complete bootstrap coverage and authoring requirements.

## Rewind Expectations
- All systems must guard against playback/catch-up by checking `RewindState.Mode` (skip heavy logic outside `Record`).
- Deterministic ordering: sort entities by (`Entity.Index`, `Entity.Version`) before applying state changes (utility helpers live in `TimeAware.cs`).
- History sampling follows patterns in `Docs/DesignNotes/RewindPatterns.md` (Snapshot cadence, Command replay, Deterministic rebuild).
- Integration tests listed in `Docs/QA/IntegrationTestChecklist.md` verify cross-system rewind flows.

## Editor Setup Checklist
- **Burst**: Enable Burst compilation (Jobs → Burst → Enable Compilation). In development, set `CompileSynchronously` (Project Settings → Burst AOT Settings) to catch compile errors immediately.
- **IL2CPP Prep**: Maintain `Assets/Config/Linker/link.xml` with preserved types; use `Docs/TruthSources/PlatformPerformance_TruthSource.md` for build settings.
- **Jobs**: Optionally set `JobsUtility.JobWorkerCount` via `PureDotsWorldBootstrap` when profiling different worker counts.
- **Deterministic Playmode**: Use `Edit → Project Settings → Time` to disable fixed timestep variability (tie to `TimeState.FixedDeltaTime`).
- **Editor Scripts**: Ensure `Assets/Scripts/Editor/` contains validation steps (future task) that warn about missing environment/spatial profiles.

## Required Assets/SubScenes

**Minimum Required** (for core functionality):
- `PureDotsConfigAuthoring` GameObject with `PureDotsRuntimeConfig` asset
  - Configure at minimum: `ResourceTypes` catalog (at least one entry, e.g., "Wood")
  - `ResourceRecipes` can be empty for basic gather/deposit flows

**Optional** (for specific features):
- `EnvironmentGridConfigAuthoring` - For environment systems (moisture, temperature, wind, sunlight)
- `EnvironmentEffectCatalogAuthoring` - For environment effects
- `SpatialPartitionAuthoring` - Custom spatial grid configuration
- Time/History/Pooling configs - Custom settings (defaults used if missing)

Designer-owned data: `Assets/Data/` contains profiles (resource types, vegetation species, miracles) consumed by bakers.

**See**: `Docs/QA/BootstrapAudit.md` for detailed bootstrap coverage and validation checklist.

## Useful References
- `Docs/TruthSources/RuntimeLifecycle_TruthSource.md`
- `Docs/TruthSources/PlatformPerformance_TruthSource.md`
- `Docs/TODO/SystemIntegration_TODO.md`
- `Docs/Guides/Authoring/EnvironmentAndSpatialValidation.md`
- `README_BRIEFING.md` section “How to adopt this runtime in a new game”

## Domain-Specific Pointers
- **Villager Systems**: Review `Docs/TODO/VillagerSystems_TODO.md` and `Docs/DesignNotes/VillagerJobs_DOTS.md`. Registries (`VillagerRegistry`, `ResourceRegistry`, `StorehouseRegistry`) plus spatial grid queries are mandatory for job assignment—avoid bespoke lookup code.
- **Resource & Logistics**: `Docs/TODO/ResourcesFramework_TODO.md` outlines registry data. Ensure hauler/freighter/wagon systems set appropriate logistics flags in `SpatialPartitionProfile`.
- **Miracles & Hand**: Align with `Docs/TODO/MiraclesFramework_TODO.md` and the central hand router described in `Docs/TODO/DivineHandCamera_TODO.md`. All miracle target queries must use shared spatial helpers.
- **Vegetation & Climate**: `Docs/TODO/ClimateSystems_TODO.md` and `Docs/TODO/VegetationSystems_TODO.md` define moisture/temperature cadences and biome thresholds. Sample environment grids via `EnvironmentGridMath` helpers.
- **Testing & QA**: Extend `Docs/QA/IntegrationTestChecklist.md` when new cross-system flows are introduced; add performance captures to `Docs/QA/PerformanceProfiles.md` once instrumentation is active.

Keep this guide up to date as new systems come online. Add links to domain-specific onboarding (villagers, miracles, resources) as they are authored.
