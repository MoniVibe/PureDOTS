# System Execution Order

_Updated: 2025-10-25_

This document captures the canonical execution ordering for the Pure DOTS simulation. All systems must target the groups and dependencies described here so climate, spatial, gameplay, and presentation layers consume consistent data.

## High-Level Ordering
- **InitializationSystemGroup** → boots configuration and performs one-off grid/index builds.
- **SimulationSystemGroup** (per frame)
  1. `BuildPhysicsWorld`
  2. **EnvironmentSystemGroup** (climate, grids)
  3. **SpatialSystemGroup** (grid rebuilds & derived spatial caches)
  4. **GameplaySystemGroup** (villagers, resources, miracles, vegetation, construction, hand IO)
  5. `ExportPhysicsWorld`
  6. **LateSimulationSystemGroup** (History + rewind prep)
- **PresentationSystemGroup** consumes the resulting state; not covered here.

Refer to `Assets/Scripts/PureDOTS/Systems/SystemGroups.cs` for the authoritative attribute configuration.

## Group Details

### TimeSystemGroup
Runs at the start of `InitializationSystemGroup` to seed deterministic singletons and align Unity's player loop with PureDOTS timing.
- `CoreSingletonBootstrapSystem` seeds `TimeState`, `GameplayFixedStep`, rewind, and registry singletons.
- `TimeSettingsConfigSystem` applies authoring overrides to `TimeState`.
- `GameplayFixedStepSyncSystem` mirrors `TimeState.FixedDeltaTime` into the `GameplayFixedStep` singleton and updates `FixedStepSimulationSystemGroup.Timestep` so fixed-step jobs use the same cadence as gameplay systems.

### EnvironmentSystemGroup
Runs immediately after physics and before any spatial or gameplay work. Systems in this group **must** preserve the following order (top → bottom):
1. `EnvironmentEffectUpdateSystem` (evaluates scalar/vector/pulse effects defined in `EnvironmentEffectCatalogData`).
2. `BiomeDeterminationSystem` (and any additional derivations that depend on refreshed channels).

Guidelines:
- The effect dispatcher writes additive contributions into channel buffers; downstream systems should read via `EnvironmentSampling` to combine base data with contributions instead of mutating blobs.
- Use `[UpdateInGroup(typeof(EnvironmentSystemGroup))]` and `[UpdateAfter]` attributes to maintain the pipeline.
- Update `LastUpdateTick`/`LastTerrainVersion` on the appropriate grid component after writing.

### SpatialSystemGroup
Produces spatial indices used by gameplay systems.
- `SpatialGridBuildSystem` (`OrderFirst`) rebuilds the runtime grid every record tick.
- Additional spatial helpers (flow fields, nav caches, etc.) should live here with explicit ordering relative to the grid build.
- All consumers must `RequireForUpdate<SpatialGridState>` and read after this group.

### GameplaySystemGroup
Contains high-level domain sub-groups:
- `VillagerSystemGroup`
- `ResourceSystemGroup`
- `VegetationSystemGroup`
- `ConstructionSystemGroup`
- `HandSystemGroup`
- (Future) `MiracleEffectSystemGroup`

Requirements:
- Systems sampling climate/environment data must include `[UpdateAfter(typeof(EnvironmentSystemGroup))]` or target a subgroup that already respects this dependency.
- Resource, miracle, and vegetation systems that rely on fresh spatial data should also `[UpdateAfter(typeof(SpatialSystemGroup))]`.
- Register new sub-groups under `GameplaySystemGroup` to avoid bypassing the environment/spatial stages.

### LateSimulationSystemGroup & HistorySystemGroup
`LateSimulationSystemGroup` runs last inside simulation and hosts cleanup/state capture. `HistorySystemGroup` (rewind recording) updates here. Any system writing to history buffers should live in this group or explicitly `[UpdateBefore(typeof(HistorySystemGroup))]`.

## Implementation Checklist
- When adding a new environment system, confirm it resides in `EnvironmentSystemGroup` and declares `UpdateAfter` to uphold the pipeline ordering above.
- When adding new gameplay systems that consume climate data, ensure they either live in a subgroup already chained after environment or add `[UpdateAfter(typeof(EnvironmentSystemGroup))]` explicitly.
- Spatially-driven systems must live in `SpatialSystemGroup` or in gameplay with `[UpdateAfter(typeof(SpatialSystemGroup))]`.
- Update this document and `SystemGroups.cs` whenever a new group or ordering dependency is introduced.
