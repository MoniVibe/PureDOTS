# Environment & Spatial Profile Authoring Guide

Updated: 2025-10-25

This guide documents the expected fields, recommended defaults, validation warnings, and downstream consumers for the two core authoring assets anchoring environment cadence and spatial queries.

## EnvironmentGridConfig (ScriptableObject)

Location: Assets/Data/Environment/EnvironmentGridConfig.asset

### Fields
- World Bounds (Min/Max, float3): Defines the simulation space covered by moisture/temperature/wind grids.
  - Default: Min (-512, -64, -512), Max (512, 64, 512).
- Cell Size (float): Resolution of moisture/temperature grids. Smaller values increase fidelity but cost memory.
  - Default: 5.0 (meters).
- Moisture Diffusion Rate (float): Percentage of moisture transferred per update step.
  - Default: 0.1.
- Evaporation Base Rate (float): Base units removed per second before temperature/wind modifiers.
  - Default: 0.5.
- Wind Cell Size (float): Resolution for wind field calculations.
  - Default: 40.0.
- Sunlight Resolution (int2): Grid resolution for sunlight calculations.
  - Default: 128 x 128.
- Seasonal Temperature Offsets (float[4]): Spring/Summer/Autumn/Winter adjustments.
  - Default: {0, +5, -5, -15}.
- Magnetic Storm Cadence (float): Seconds between magnetic storm refreshes (future feature).
  - Default: 120.0.
- Solar Radiation Multiplier (float): Scalar for miracles/vegetation stress.
  - Default: 1.0.

### Validation Warnings
- World bounds must be non-zero volume. Log a warning if WorldMax <= WorldMin on any axis.
- Cell size must be ≥ 0.5 and divide evenly into world extent; warn when mismatched.
- Diffusion/Evaporation rates should be within [0, 1]; log if outside range.
- Ensure grid resolutions are powers of two when possible (sunlight/wind) for performance notes.
- Magnetic storm cadence (once enabled) should be ≥ 10 seconds; warn when configured lower.

## SpatialPartitionProfile (ScriptableObject)

Location: Assets/Data/Spatial/SpatialPartitionProfile.asset

### Fields
- Provider Type (enum): UniformGrid or HashedGrid (default HashedGrid).
- Cell Size (float): Spatial grid resolution (meters). Should roughly match average entity spacing.
  - Default: 4.0.
- World Min/Max (float3): Bounds for spatial indexing. Align with EnvironmentGridConfig where possible.
  - Default: (-512, -64, -512) / (512, 64, 512).
- Hash Seed (uint): Seed for hashed grid distribution.
  - Default: 0.
- Logistics Layers (enum flags): Registries included in spatial indexing (Villager, MinerVessel, HaulerFreighter, Wagon, ResourceNeutral, MiracleNeutral).
  - Default: Villager | ResourceNeutral.

### Validation Warnings
- Cell size must be ≥ 1.0; warn and clamp if below.
- World bounds should match environment config; emit info message if mismatch > cell size.
- Hash seed should be deterministic; warn if changed at runtime.
- When provider == UniformGrid, ensure (WorldMax - WorldMin) is divisible by CellSize; otherwise log adjustments.
- Logistics layer mask must include at least one consumer (villager/resource/miracle); warn if zero.

## Authoring & Bake Checklist
- Ensure both assets are referenced in bootstrap SubScene (e.g., via SpatialPartitionAuthoring, EnvironmentGridConfigAuthoring).
- Playmode validation script should scan for these assets and log warnings at startup.
- Update this guide when new fields are introduced (e.g., magnetic storm cadence, debris grid parameters).

## Domain Integration Notes
- **Climate & Weather**: Environment grids drive temperature, moisture, wind, sunlight, magnetic storms, solar radiation. Keep bounds matching Terra/Scene size to avoid clipping miracles or weather effects.
- **Vegetation & Resources**: Vegetation growth/health and resource drying reference moisture/temperature cells. Finer cell sizes (<5m) yield smoother gradients for forests and farmland.
- **Villager Logistics**: Spatial partition logistics mask should include registries used by job assignment, freighters, wagons, and neutral miracles. Missing flags result in linear scans.
- **Miracle Targeting**: Rain/fireball/shield miracles sample environment cells and spatial grid entries. Misconfigured bounds cause truncated effects or incorrect alignment.
- **Performance Profiling**: Adjust cell sizes and provider type per platform; capture frame timings per settings in `Docs/QA/PerformanceProfiles.md` (update when instrumentation lands).

## References
- Docs/TruthSources/RuntimeLifecycle_TruthSource.md
- Docs/TODO/ClimateSystems_TODO.md
- Docs/TODO/SpatialServices_TODO.md
- Docs/QA/PerformanceProfiles.md
