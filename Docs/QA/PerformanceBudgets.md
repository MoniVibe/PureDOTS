# Performance Budgets

This document defines performance budgets for PureDOTS scale testing.

## Tick Time Budgets

| Scale | Entity Count | Target Tick Time | Target FPS |
|-------|-------------|------------------|------------|
| Baseline | 10k | < 16.67ms | 60 FPS |
| Stress | 100k | < 33.33ms | 30 FPS |
| Extreme | 1M+ | < 100ms | 10 FPS |

## System Timing Budgets (at 100k entities)

| System | Budget | Notes |
|--------|--------|-------|
| Movement | < 5ms | Position/velocity updates |
| AI Pipeline | < 8ms | Sensors, utility, steering |
| Spatial Grid | < 3ms | Grid updates and queries |
| Registry Updates | < 2ms | Entity registry sync |
| Aggregate Updates | < 2ms | Village/fleet summaries |
| **Total Simulation** | **< 20ms** | Leaves headroom for rendering |

## Component Budgets

| Metric | Limit | Reason |
|--------|-------|--------|
| Max components on hot entity | 12 | Chunk utilization |
| Max hot component size | 128 bytes | Cache line efficiency |
| Max buffer elements on hot entity | 16 | Memory footprint |

## Memory Budgets (at 1M entities)

| Category | Budget | Notes |
|----------|--------|-------|
| Total simulation memory | < 4GB | Main thread + jobs |
| Chunk memory overhead | < 500MB | Entity storage |
| Registry buffers | < 200MB | Index structures |

## Entity Count Budgets

| Category | Budget | Notes |
|----------|--------|-------|
| Hot entities (per-tick) | < 100k | For 60 FPS target |
| Medium entities | < 500k | Occasional updates |
| Cold entities | Unlimited | Config/companion data |

## Validation Thresholds

### Warning Thresholds
- Tick time > 80% of budget
- Memory > 75% of budget
- Entity count > 90% of budget

### Failure Thresholds
- Tick time > 100% of budget
- Memory > 100% of budget
- Average tick time > budget for 100+ consecutive ticks

## Scale Test Scenarios

### Baseline (10k entities)
- **File**: `scale_baseline_10k.json`
- **Duration**: 1,000 ticks
- **Pass Criteria**: Average tick time < 16.67ms

### Stress (100k entities)
- **File**: `scale_stress_100k.json`
- **Duration**: 2,000 ticks
- **Pass Criteria**: Average tick time < 33.33ms

### Extreme (1M+ entities)
- **File**: `scale_extreme_1m.json`
- **Duration**: 5,000 ticks
- **Pass Criteria**: Average tick time < 100ms

## CI Integration

### Running Scale Tests

```bash
# Run baseline scenario
Unity -batchmode -projectPath . \
  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScaleTest \
  --scenario scale_baseline_10k \
  --metrics CI/Reports/baseline.json

# Run stress scenario
Unity -batchmode -projectPath . \
  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScaleTest \
  --scenario scale_stress_100k \
  --metrics CI/Reports/stress.json

# Run extreme scenario
Unity -batchmode -projectPath . \
  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScaleTest \
  --scenario scale_extreme_1m \
  --metrics CI/Reports/extreme.json
```

### Validating Results

```bash
# Validate all reports against budgets
python CI/validate_metrics.py CI/Reports/
```

## Refactor Triggers

| Condition | Action |
|-----------|--------|
| System time > 2x budget | Split system or reduce update frequency |
| Entity count > 500k hot | Enable render density or aggregation |
| Memory > 4GB | Review component sizes, move to companions |
| P95 tick time > 1.5x average | Investigate spikes, optimize hot paths |

## Perception-Specific Budgets

### Signal Sampling Budgets

| Metric | Budget | Notes |
|--------|--------|-------|
| `MaxSignalCellsSampledPerTick` | 1000 | ~40 entities × 25 cells (5×5 neighborhood) |
| `MaxSamplingRadiusCells` | 5 | Maximum radius in cells for multi-cell sampling |
| `TierSamplingRadiusMultiplier` | 0.5-2.0 | Tier-based scaling (Tier0=0.5x, Tier3=2.0x) |

**Warning Thresholds**:
- `SignalCellsSampledThisTick > MaxSignalCellsSampledPerTick * 0.8` → log warning
- Multi-cell sampling increases cost but improves fidelity

### LOS Check Budgets

| Metric | Budget | Notes |
|--------|--------|-------|
| `MaxLosChecksPerTick` | 24 | Already exists as `MaxLosRaysPerTick` |
| `MaxLosChecksPhysicsThisTick` | 24 | Physics raycast checks |
| `MaxLosChecksObstacleGridThisTick` | 24 | Obstacle grid LOS checks |
| `MaxLosChecksUnknownThisTick` | 12 | Unknown LOS (should be < 50% of total) |

**Warning Thresholds**:
- `LosChecksUnknownThisTick > MaxLosChecksPerTick * 0.5` → log warning (too many unknown LOS)
- Unknown LOS applies confidence penalty (0.5x multiplier)

### Miracle Detection Budgets

| Metric | Budget | Notes |
|--------|--------|-------|
| `MiracleEntitiesDetectedThisTick` | Unlimited | No hard limit, but should be tracked |

## See Also

- `Docs/PERFORMANCE_PLAN.md` - Overall performance strategy
- `Docs/FoundationGuidelines.md` - Component size guidelines
- `Packages/com.moni.puredots/Runtime/Runtime/Telemetry/ScaleTestMetricsComponents.cs` - Metrics components

