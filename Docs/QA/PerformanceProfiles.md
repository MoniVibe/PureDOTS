# Performance Profiles & Soak Testing

## Overview

PureDOTS includes deterministic performance soak scenes and telemetry collection to validate scalability targets and catch regressions.

## Performance Soak Scene

**Location**: `Assets/Scenes/Perf/PerformanceSoakScene.unity`

### Purpose

- Validate 50k+ entity performance targets
- Catch frame time regressions
- Measure deterministic execution costs
- Profile registry/spatial/navigation systems under load

### Usage

1. Open `PerformanceSoakScene.unity`
2. Configure entity count and spawn patterns via scene bootstrap
3. Run in Play Mode or via CI runner
4. Telemetry is automatically collected via `FrameTimingRecorderSystem`
5. Export metrics via `TelemetryExportSystem` (CSV/JSON)

### Target Metrics

- **Frame Time**: <16ms (60 FPS) with 50k entities
- **Registry Updates**: <2ms per registry per frame
- **Spatial Grid Rebuild**: <3ms for full rebuild (256x256 grid)
- **Flow Field Build**: <3ms per layer (30-60 tick cadence)
- **Memory**: <500MB for 50k entities + registries

### CI Integration

```bash
# Run performance soak test
UNITY_PATH=/path/to/Unity PureDOTS/CI/run_performance_soak.sh

# Results land in PureDOTS/CI/TestResults/Performance/
```

## Meta Registry Soak Harness (Automated)

- **Location**: `Assets/Tests/Playmode/SoakHarness.cs`
- **Command**: `Unity -batchmode -projectPath PureDOTS -runTests -testPlatform playmode -testFilter MetaRegistrySoakHarness_RunsForMultipleTicks`

### What It Covers

- Spawns representative counts of factions, climate hazards, area effects, and cultures
- Runs meta registry systems and `DebugDisplaySystem` for 128 ticks
- Verifies debug HUD aggregates (`DebugDisplayData`) and telemetry metrics (registry counts/intensity/averages)

### Metrics Checked

- `registry.faction.count`, `registry.faction.resources`, `registry.hazard.count`, `registry.area.count`, `registry.culture.count`
- Average area effect strength
- Global culture alignment score and hazard intensity saturation

### Usage

1. Ensure `CoreSingletonBootstrapSystem` bootstraps the world (handled inside the test)
2. Run the playmode test locally or through CI
3. Review assertions to confirm telemetry keys and HUD fields stay in sync with registry outputs

## Telemetry Collection

### Frame Timing

- Per-system frame time (microseconds)
- Entity counts per archetype
- Memory allocations
- Burst job completion times

### Registry Metrics

- Entry counts per registry
- Update frequencies
- Spatial sync version deltas
- Continuity violation counts

### Navigation Metrics

- Flow field rebuild times
- Agent pathfinding queries
- Steering computation costs
- Sensor update frequencies

## Profiling Tips

1. **Use Unity Profiler** with Burst-compiled jobs enabled
2. **Enable Frame Timing Stream** for per-system breakdowns
3. **Check Registry Health Monitoring** for anomalies
4. **Review Spatial Grid Dirty Counts** for optimization opportunities

## Regression Detection

Performance tests should fail if:
- Frame time exceeds target by >20%
- Memory usage exceeds baseline by >30%
- Any system exceeds its budget consistently (>5 consecutive frames)

## Custom Profiles

Games can extend performance profiles by:
1. Creating scene-specific bootstrap scripts
2. Configuring entity spawn patterns
3. Adding custom telemetry hooks
4. Defining game-specific targets

See `Docs/TODO/Utilities_TODO.md` for implementation details.
