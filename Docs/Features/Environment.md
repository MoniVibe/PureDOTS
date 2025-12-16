# Environment Grid – Sunlight, Wind, Moisture, Climate & Vegetation

**Status:** Tier-1 Implementation Complete  
**Category:** System – World / Planet Simulation  
**Scope:** Shared Environment Framework (PureDOTS → Godgame + Space4X)  
**Created:** 2025-12-XX  
**Last Updated:** 2025-12-XX

## Overview

**One-line description**: *A shared environment framework providing sunlight, wind, moisture grid, climate, and vegetation needs systems that both Godgame and Space4X consume, with PureDOTS as the canonical truth source.*

## Core Concept

The Environment Grid system provides a game-agnostic framework for environmental simulation. It tracks climate (temperature, humidity, seasons), wind (direction, strength, type), moisture (per-cell soil moisture), sunlight (from stars), and vegetation needs (environmental requirements for plants). All systems are Burst-compiled, deterministic, and rewind-safe.

## How It Works

### Basic Rules

1. **Climate**: Global temperature and humidity oscillate based on configurable sine waves. Optional seasonal progression.
2. **Wind**: Global wind direction rotates slowly, strength oscillates. Wind type (Calm/Breeze/Wind/Storm) determined by strength thresholds.
3. **Moisture Grid**: Per-cell soil moisture aligned with `SpatialGridConfig`. Updated based on sources (rain, miracles) and sinks (evaporation, consumption, drainage).
4. **Sunlight**: Derived from `StarSolarYield` and distributed to planets/worlds. Integrates with `TimeOfDaySystem` for day/night cycles.
5. **Vegetation Needs**: Plants define optimal ranges for sunlight, moisture, and temperature. `VegetationStressSystem` calculates stress and growth factors.

### Parameters and Variables

| Parameter | Component | Default Value | Range | Effect |
|---|---|---|---|---|
| Temperature | `ClimateState` | 20°C | float | Affects evaporation, vegetation growth |
| Humidity | `ClimateState` | 0.5 | 0-1 | Affects moisture, vegetation growth |
| Wind Strength | `WindState` | 0.3 | 0-1 | Affects evaporation, visual effects |
| Moisture | `MoistureCell` | 0.5 | 0-1 | Affects vegetation growth, visual appearance |
| Sunlight Intensity | `SunlightState` | 1.0 | 0-1 | Derived from `StarSolarYield`, affects vegetation |
| Vegetation Stress | `VegetationStress` | 0.0 | 0-1 | 0=healthy, 1=dying, affects growth rate |

### Edge Cases

- **No Star**: Worlds without a parent star use default sunlight or find highest-yield star
- **No Spatial Grid**: Moisture grid defaults to 1×1 if `SpatialGridConfig` doesn't exist
- **Missing Singletons**: Systems use default configs if singletons don't exist
- **Rewind Safety**: All systems respect `RewindState` and only mutate during Record mode

## Player Interaction

### Player Decisions

- **Miracles**: Rain/Fire miracles modify moisture and climate (via game-specific integration)
- **Terraforming**: Space4X terraforming modifies climate and moisture (via game-specific systems)

### Feedback to Player

- **Visual feedback**: Moisture affects grass color, climate affects visual palette
- **Numerical feedback**: Info panels show temperature, humidity, moisture levels, vegetation stress

## Balance and Tuning

### Balance Goals

- Ensure meaningful environmental variation without overwhelming complexity
- Balance moisture sources and sinks to create interesting dynamics
- Make vegetation stress impactful but not immediately fatal

### Tuning Knobs

1. **`ClimateConfig`**: Adjust oscillation amplitude, period, base values
2. **`WindConfig`**: Adjust strength thresholds, change rate, oscillation
3. **`MoistureConfig`**: Adjust evaporation/absorption rates, drainage factor
4. **`SunlightConfig`**: Adjust falloff, time-of-day factor, min/max ranges
5. **`VegetationNeeds`**: Define optimal ranges per plant type

## Integration with Other Systems

### Interacts With

| System/Mechanic | Type of Interaction | Priority |
|---|---|---|
| **Star System** | `SunlightDistributionSystem` reads `StarSolarYield` | High |
| **Time-of-Day System** | `SunlightDistributionSystem` reads `TimeOfDayState` for day/night cycle | High |
| **Spatial Grid** | `MoistureGridState` reuses `SpatialGridConfig` resolution | High |
| **Vegetation Systems** | `VegetationStressSystem` provides stress/growth factors | High |
| **Motivation System** | Environment goals (e.g., "make land lush", "terraform planet") | Medium |
| **Miracle Systems** | Rain/Fire miracles modify moisture/climate | High |

## Implementation Notes

### Technical Approach

- **PureDOTS Shared Core**: All environment components and core systems are implemented in the `PureDOTS` package to be game-agnostic
- **Burst-Compiled**: All systems are Burst-compiled for high performance
- **Data-Oriented**: Components are designed as plain old data (POD) structs
- **Grid Alignment**: Moisture grid reuses `SpatialGridConfig` to avoid duplicate infrastructure

### Performance Considerations

- Climate and wind systems run on singletons (minimal overhead)
- Moisture grid updates can be throttled via `UpdateFrequency`
- Vegetation stress calculated per entity with `VegetationNeeds` component
- All calculations are Burst-compiled, ensuring high performance

### Testing Strategy

1. **Unit tests for oscillation**: Verify climate temperature/humidity oscillation
2. **Unit tests for wind**: Verify wind type classification based on strength
3. **Unit tests for stress**: Verify vegetation stress calculation
4. **Unit tests for moisture**: Verify evaporation rate calculation
5. **Integration tests**: Ensure systems work together correctly

## Modding Guide

### Creating Custom Climate Configs

1. Create a `ClimateConfigAsset` ScriptableObject
2. Set base temperature, humidity, oscillation parameters
3. Enable/configure seasons if desired
4. Load config via `EnvironmentBootstrapSystem` or game-specific bootstrap

### Creating Custom Vegetation Needs

1. Define `VegetationNeedsSpec` with optimal ranges
2. Add to `VegetationNeedsCatalog` blob asset
3. Assign to vegetation entities via `VegetationNeeds` component
4. `VegetationStressSystem` automatically calculates stress

### Creating Custom Moisture Configs

1. Create a `MoistureConfigAsset` ScriptableObject
2. Set evaporation, absorption, drainage rates
3. Configure temperature/wind multipliers
4. Load config via bootstrap system

## Examples

### Example Scenario 1: Godgame Village with Drought

**Setup**: A village in a temperate biome. `ClimateState` shows high temperature (30°C), low humidity (0.2). `MoistureGridState` shows low moisture (0.1) in cells.

**Action**: The game runs, `VegetationStressSystem` calculates high stress for crops (moisture factor = 0.1 / 0.3 = 0.33).

**Result**: Crops grow slowly, yields are reduced. Player casts Rain miracle, which increases moisture via game-specific integration. Moisture rises to 0.6, stress decreases, crops recover.

### Example Scenario 2: Space4X Planet Habitability

**Setup**: A planet orbiting a G-type star with `StarSolarYield.Yield = 0.8`. `SunlightDistributionSystem` calculates `SunlightState.GlobalIntensity = 0.8`.

**Action**: `Space4XPlanetEnvironmentSystem` reads `SunlightState` and `ClimateState` to calculate habitability score.

**Result**: Planet shows high habitability (good sunlight, moderate temperature). Colony yields are boosted. Player initiates terraforming, which modifies `ClimateState` via game-specific system.

## References and Inspiration

- **Environment_Systems.md**: Godgame-specific environment design (biomes, vegetation, climate)
- **Star System**: Solar yield calculation and distribution
- **Spatial Grid**: Grid infrastructure reused for moisture tracking

## Revision History

| Date | Change | Reason |
|---|---|---|
| [Current Date] | Initial implementation and documentation | New feature |

---

*Last Updated: [Current Date]*  
*Document Owner: [AI Assistant]*
























