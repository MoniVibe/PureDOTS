# Environment & Spatial Profile Authoring Guide

Updated: 2025-10-26

This guide documents the expected fields, recommended defaults, validation warnings, and downstream consumers for the two core authoring assets anchoring environment cadence and spatial queries.

## EnvironmentGridConfig (ScriptableObject)

Location: `Assets/PureDOTS/Config/EnvironmentGridConfig.asset`

### Fields
- World Bounds (`Vector3 _worldMin/_worldMax`): Simulation space for each channel. Defaults: Min (-512, 0, -512), Max (512, 256, 512). Must define positive volume.
- Grid Settings (`GridSettings` per channel): Resolution (`Vector2Int`), cell size, bounds, enabled flag per channel (moisture/temperature/sunlight/wind/biome). Defaults range 256x256 @5m (moisture) down to 64x64 @20m (wind).
- Channel Identifiers (`string`): Unique IDs consumed by downstream systems. Must be non-empty and unique.
- Moisture Coefficients (`_moistureDiffusion`, `_moistureSeepage`): Defaults 0.25 / 0.1.
- Temperature Defaults (`_baseSeasonTemperature`, `_timeOfDaySwing`, `_seasonalSwing`): Defaults 18°C, 6°C diurnal swing, 12°C seasonal swing.
- Sunlight Defaults (`_sunDirection`, `_sunIntensity`): Normalized direction (0.25, -0.9, 0.35) and scalar intensity (1.0).
- Wind Defaults (`_globalWindDirection`, `_globalWindStrength`): Normalized direction (0.7, 0.5) and scalar strength (8.0).
- Optional Biome Grid (`GridSettings _biome`): Disabled by default; enable when biome-driven systems land.

### Validation Coverage (PureDOTS/Validation)
- Errors when channel IDs are empty/duplicated, grid resolution ≤ 0, cell size ≤ 0, or bounds collapse.
- Warnings for resolutions > 2048 per axis, disabled required grids, sun/wind vectors near zero, or extreme cell sizes.
- Editor `OnValidate` clamps guard obvious mistakes; validation surfaces risky authoring choices before runtime.

## SpatialPartitionProfile (ScriptableObject)

Location: `Assets/PureDOTS/Config/SpatialPartitionProfile.asset`
- **Tooling Integration**
  - Unity menu: `PureDOTS/Validation/Run Asset Validation` (full log) and `PureDOTS/Validation/Run Validation (Quiet Log)`.
  - CLI: `-executeMethod PureDOTS.Editor.PureDotsAssetValidator.RunValidationFromCommandLine` returns non-zero exit on errors for CI.
  - Inspectors: runtime config, resource catalog, environment grid, and spatial profile each expose a “Validate …” button.

### Fields
- Provider (`_provider`): UniformGrid or HashedGrid (default hashed for scalability).
- Cell Size (`_cellSize`): Spatial resolution in meters (default 4.0). Balance fidelity vs. runtime cost.
- World Bounds (`_worldMin/_worldMax`): Bounds used for spatial indexing (defaults -512/-64/-512 to 512/64/512). Keep aligned with environment config.
- Hash Seed (`_hashSeed`): Deterministic seed for hashed grids.
- Future work: logistics layer masks move into shared registry metadata—update when feature lands.

### Validation Coverage
- Errors when world bounds collapse or cell size < 0.5 m.
- Warnings for cell size > 32 m, total cell count > 4 M, or uniform grid bounds not divisible by cell size.
- Info when hashed seed remains 0 (nudge teams to pick deterministic seed).

## Authoring & Bake Checklist
- Reference both assets via `EnvironmentGridConfigAuthoring` / `SpatialPartitionAuthoring` inside the bootstrap SubScene.
- Store canonical assets under `Assets/PureDOTS/Config` (or documented alternative) for project consistency.
- Run `PureDOTS/Validation/Run Asset Validation` (or CLI equivalent) before committing or exporting builds to catch misconfigurations.
- Keep environment and spatial bounds/cell sizes aligned to avoid mismatched sampling.
- Update this guide and validation tooling when new fields land (magnetic storms, debris grids, logistics masks, etc.).

## Domain Integration Notes
- **Climate & Weather**: Environment grids feed climate cadence, rewind guards, and miracle effects. Misaligned bounds clip samples at edges.
- **Vegetation & Resources**: Moisture/temperature channels drive growth and resource drying. Smaller cell sizes (<5 m) provide smoother gradients but increase memory use.
- **Villager Logistics**: Spatial partition powers nearest-neighbour queries for registries. Ensure cell size roughly matches agent spacing to avoid collisions/overflow.
- **Miracle Targeting**: Miracles consume channel IDs and spatial data. Keep identifiers unique and grids covering the play area.
- **Performance Profiling**: As telemetry lands, capture frame timings (`Docs/QA/PerformanceProfiles.md`) and revisit grid resolutions per platform budget.

## References
- Docs/TruthSources/RuntimeLifecycle_TruthSource.md
- Docs/TODO/ClimateSystems_TODO.md
- Docs/TODO/SpatialServices_TODO.md
- Docs/TODO/Utilities_TODO.md
- Docs/QA/PerformanceProfiles.md
