# System Integration Overview

_Updated: 2025-10-25_

This note captures the cross-system contracts that bind the Pure DOTS simulation together. Keep it current whenever a shared component, system group, or integration test changes.

## Shared Runtime Data
- **Environment grids** live in `Assets/Scripts/PureDOTS/Runtime/Environment/EnvironmentGrids.cs` under the `PureDOTS.Environment` namespace. All climate/vegetation/resource systems must depend on these types instead of defining their own.
- `EnvironmentGridConfig` (authoring) → `EnvironmentGridConfigData` (runtime) centralises grid sizing, default climate parameters, and moisture diffusion/sipping constants.
- `HandInteractionState` and `ResourceSiphonState` (hand folder) expose authoritative hand/router state. Resource + miracle systems read these components to avoid state drift.
- `TerrainVersion` & `TerrainChangeEvent` track terraforming. Any system that caches terrain-derived data must observe version increments and/or consume queued events.
- History sample structs (`GridHistorySample`, `InteractionHistorySample`) provide consistent rewind payloads for grids and hand interactions.

## System Groups & Ordering
- `EnvironmentSystemGroup` runs after physics and produces up-to-date climate, moisture, wind, and biome data. Target this group (with explicit `[UpdateAfter]` dependencies) for any environment authoring.
- `SpatialSystemGroup` rebuilds spatial indices (`SpatialGridBuildSystem` is `OrderFirst`). Systems performing spatial queries should either live here or `UpdateAfter` this group.
- `GameplaySystemGroup` hosts `VillagerSystemGroup`, `ResourceSystemGroup`, `MiracleEffectSystemGroup`, `VegetationSystemGroup`, `ConstructionSystemGroup`. These naturally run after environment + spatial updates.
- See `Docs/DesignNotes/SystemExecutionOrder.md` for the full ordering, including guidelines for adding new systems.

## Input & Interaction Flow
- `HandInputRouterSystem` (future work) will drive command resolution, but `DivineHandSystem` already publishes merged hand state into `HandInteractionState`/`ResourceSiphonState` each tick.
- Downstream systems must consume those components rather than caching their own hand/miracle flags. This guarantees RMB routing decisions stay in sync.

## Rewind Expectations
- All systems with historical data must enqueue samples in `HistorySystemGroup` using the shared structs. Consult `Docs/DesignNotes/RewindPatterns.md` for approved strategies (snapshot cadence, command replay, deterministic rebuild).
- Deterministic ordering: when sorting entities for playback, use `Entity.Index`/`Entity.Version`. A helper will live in `TimeAware.cs` when flow-field rewrites land.

## Terraforming Contracts
- Increment `TerrainVersion.Value` whenever terrain geometry/material changes.
- Emit a `TerrainChangeEvent` (buffer element) describing the affected bounds; environment grids and spatial caches use this to schedule rebuilds.
- Environment grids mirror `LastTerrainVersion`; update them whenever terrain events are processed to satisfy the integration TODO.

## Testing & Tooling
- `SystemIntegrationPlaymodeTests.cs` contains the first regression tests around environment grid sampling. Extend this suite as cross-system flows come online (hand siphon + rain, villager responses, etc.).
- Debug overlays should query the shared environment components and registries rather than rolling bespoke inspectors.

## Documentation Hooks
- Subsystem TODOs (Climate, Vegetation, Resources, Miracles, Terraforming) should link back to this document and the environment/system-order docs when referencing shared data.
- Update this file whenever a new shared component, event stream, or test harness is introduced.
