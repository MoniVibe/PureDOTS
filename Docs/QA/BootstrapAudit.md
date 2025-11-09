# DOTS Bootstrap Audit

_Last updated: 2025-01-XX_

## Overview

This document audits the PureDOTS bootstrap infrastructure to ensure a new project boots cleanly without manual setup. It identifies what's automatically bootstrapped vs. what requires authoring assets.

## Automatically Bootstrapped Singletons

The following singletons are created automatically by `CoreSingletonBootstrapSystem` and related bootstrap systems:

### Core Time & Rewind
- ✅ `TimeState` - Fixed delta time, tick counter, pause state
- ✅ `RewindState` - Rewind mode, playback state
- ✅ `HistorySettings` - Rewind snapshot settings
- ✅ `GameplayFixedStep` - Fixed-step timing

### Registries (All Core Domain Registries)
- ✅ `ResourceRegistry` + `ResourceRegistryEntry` buffer
- ✅ `StorehouseRegistry` + `StorehouseRegistryEntry` buffer
- ✅ `VillagerRegistry` + `VillagerRegistryEntry` buffer
- ✅ `MiracleRegistry` + `MiracleRegistryEntry` buffer
- ✅ `CreatureRegistry` + `CreatureRegistryEntry` buffer
- ✅ `ConstructionRegistry` + `ConstructionRegistryEntry` buffer
- ✅ `LogisticsRequestRegistry` + `LogisticsRequestRegistryEntry` buffer
- ✅ `BandRegistry` + `BandRegistryEntry` buffer
- ✅ `AbilityRegistry` + `AbilityRegistryEntry` buffer
- ✅ `SpawnerRegistry` + `SpawnerRegistryEntry` buffer
- ✅ `RegistryDirectory` + buffers
- ✅ `RegistrySpatialSyncState` + `RegistryContinuityState`
- ✅ `RegistryHealthMonitoring` + `RegistryHealthThresholds`

### Spatial Grid
- ✅ `SpatialGridConfig` - Default 256x32x256 grid, 4m cells
- ✅ `SpatialGridState` - Active buffer index, version tracking
- ✅ All spatial buffers (`SpatialGridCellRange`, `SpatialGridEntry`, staging buffers, dirty ops)
- ✅ `SpatialProviderRegistry`
- ✅ `SpatialRebuildThresholds` (if missing)

### Navigation
- ✅ `FlowFieldConfig` - Default 5m cells, bounds -100 to +100
- ✅ `FlowFieldLayer` buffer
- ✅ `FlowFieldCellData` buffer
- ✅ `FlowFieldRequest` buffer
- ✅ `FlowFieldHazardUpdate` buffer

### AI & Commands
- ✅ `AICommandQueueTag` + `AICommand` buffer

### Telemetry & Observability
- ✅ `TelemetryStream` + `TelemetryMetric` buffer
- ✅ `FrameTimingStream` + `FrameTimingSample` buffer + `AllocationDiagnostics`
- ✅ `ReplayCaptureStream` + `ReplayCaptureEvent` buffer

### Configuration (Defaults)
- ✅ `VillagerBehaviorConfig` - Default thresholds and timings
- ✅ `ResourceInteractionConfig` - Default interaction parameters

### Other Bootstrap Systems
- ✅ `VillagerJobBootstrapSystem` - Seeds `VillagerJobEventStream`, request/delivery queues
- ✅ `VegetationCommandBootstrapSystem` - Seeds vegetation harvest/spawn command queues
- ✅ `PresentationBootstrapSystem` - Seeds `PresentationCommandQueue` + buffers
- ✅ `StreamingCoordinatorBootstrapSystem` - Seeds streaming coordinator (if enabled)

## Authoring Assets Required

The following systems require authoring assets to be baked into the scene:

### Required for Core Functionality
- ⚠️ **`ResourceTypeIndex`** - Created by `PureDotsConfigBaker` from `PureDotsRuntimeConfig.ResourceTypes`
  - **Impact**: `ResourceRegistrySystem` and `ResourceDepositSystem` won't run without this
  - **Workaround**: Create minimal `PureDotsConfigAuthoring` GameObject with `ResourceTypes` catalog

- ⚠️ **`ResourceRecipeSet`** - Created by `PureDotsConfigBaker` from `PureDotsRuntimeConfig.ResourceRecipes`
  - **Impact**: `ResourceProcessingSystem` won't run without this
  - **Workaround**: Can be empty/default for basic resource gathering/deposit

### Optional/Feature-Specific
- ⚠️ **`EnvironmentGridConfigData`** - Required for `EnvironmentGridBootstrapSystem`
  - **Impact**: Environment systems (moisture, temperature, wind, sunlight) won't initialize
  - **Workaround**: Create `EnvironmentGridConfigAuthoring` GameObject

- ⚠️ **`EnvironmentEffectCatalogData`** - Required for `EnvironmentEffectBootstrapSystem`
  - **Impact**: Environment effects won't initialize
  - **Workaround**: Create `EnvironmentEffectCatalogAuthoring` GameObject

- ⚠️ **`TimeSettingsConfig`** - Optional, provides custom time settings
  - **Impact**: Uses defaults if missing

- ⚠️ **`HistorySettingsConfig`** - Optional, provides custom history settings
  - **Impact**: Uses defaults if missing

- ⚠️ **`PoolingSettingsConfig`** - Optional, provides custom pooling settings
  - **Impact**: Uses defaults if missing

- ⚠️ **`ResourceProcessorConfig`** - Optional, provides resource processing rules
  - **Impact**: Resource processing won't run without this (but gathering/deposit still works)

## Bootstrap Coverage Summary

| Category | Bootstrap Status | Notes |
|----------|------------------|-------|
| **Time & Rewind** | ✅ Fully Bootstrapped | All core singletons seeded |
| **Registries** | ✅ Fully Bootstrapped | All 10 core registries + directory |
| **Spatial Grid** | ✅ Fully Bootstrapped | Default config, all buffers |
| **Navigation** | ✅ Fully Bootstrapped | Flow field config + buffers |
| **AI Systems** | ✅ Fully Bootstrapped | Command queue seeded |
| **Telemetry** | ✅ Fully Bootstrapped | All streams seeded |
| **Resource Systems** | ⚠️ **Requires Authoring** | Needs `ResourceTypeIndex` (via `PureDotsConfigAuthoring`) |
| **Environment** | ⚠️ **Requires Authoring** | Needs `EnvironmentGridConfigData` |
| **Presentation** | ✅ Fully Bootstrapped | Command queue seeded |

## Minimal Setup for New Project

To get a PureDOTS project running with core functionality:

1. **World Bootstrap** (Automatic)
   - `PureDotsWorldBootstrap` creates world and system groups automatically
   - No MonoBehaviour required

2. **Core Singletons** (Automatic)
   - `CoreSingletonBootstrapSystem` seeds all core singletons
   - Systems can run immediately (guarded by `RequireForUpdate`)

3. **Required Authoring Assets**
   - Create `PureDotsConfigAuthoring` GameObject with `PureDotsRuntimeConfig` asset
   - Configure at minimum:
     - `ResourceTypes` catalog (at least one entry, e.g., "Wood")
     - `ResourceRecipes` (can be empty for basic gather/deposit)

4. **Optional Authoring Assets** (for specific features)
   - `EnvironmentGridConfigData` - For environment systems
   - `EnvironmentEffectCatalogData` - For environment effects
   - `TimeSettingsConfig` - Custom time settings
   - `HistorySettingsConfig` - Custom history settings
   - `PoolingSettingsConfig` - Custom pooling settings
   - `ResourceProcessorConfig` - Resource processing rules

## Validation Checklist

For a fresh project, verify:

- [ ] World boots without errors (check console)
- [ ] `TimeState` singleton exists (systems require it)
- [ ] `SpatialGridConfig` singleton exists (spatial systems require it)
- [ ] Core registries exist (Resource, Storehouse, Villager, etc.)
- [ ] `ResourceTypeIndex` exists (if using resource systems)
- [ ] Systems run without missing singleton errors

## Known Gaps

1. **ResourceTypeIndex/ResourceRecipeSet**: Not bootstrapped automatically
   - **Rationale**: Requires domain-specific catalog data (resource types, recipes)
   - **Solution**: Must be provided via `PureDotsConfigAuthoring` baking

2. **EnvironmentGridConfigData**: Not bootstrapped automatically
   - **Rationale**: Requires terrain bounds and grid resolution configuration
   - **Solution**: Must be provided via `EnvironmentGridConfigAuthoring` baking

3. **System-Specific Configs**: Many config singletons require authoring
   - **Rationale**: Domain-specific settings (time, history, pooling, processors)
   - **Solution**: Systems use defaults if missing (some disable themselves)

## Recommendations

1. **Document authoring requirements** in getting-started guide
2. **Provide template scenes** with minimal authoring assets configured
3. **Add validation warnings** when systems can't run due to missing assets
4. **Consider bootstrap fallbacks** for optional systems (e.g., empty ResourceTypeIndex)

## Testing

- ✅ **BootstrapSmokeTest** (`Assets/Tests/Playmode/BootstrapSmokeTest.cs`) - Validates bootstrap creates all required singletons and systems can run without errors

## See Also

- `Runtime/Systems/CoreSingletonBootstrapSystem.cs` - Main bootstrap implementation
- `Runtime/Systems/PureDotsWorldBootstrap.cs` - World initialization
- `Docs/Guides/GettingStarted.md` - Getting started guide (updated with bootstrap info)
- `Runtime/Authoring/PureDotsConfigAuthoring.cs` - Resource catalog authoring
- `Assets/Tests/Playmode/BootstrapSmokeTest.cs` - Bootstrap validation tests

