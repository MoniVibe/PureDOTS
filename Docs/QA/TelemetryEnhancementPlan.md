# Telemetry & Performance Enhancement Plan

## Overview

This document outlines the plan for enhancing PureDOTS telemetry collection, external dashboard integration, profiling harness automation, and job scheduling instrumentation.

## Current State

**Implemented**:
- `FrameTimingRecorderSystem` - Captures per-system frame times
- `TelemetryStream` - Buffer for telemetry metrics
- `ReplayCaptureSystem` - Captures replay events
- `DebugDisplaySystem` - In-game debug overlay
- `RegistryInstrumentationSystem` - Registry health metrics

**Gaps**:
- External dashboard integration (Grafana/InfluxDB)
- Automated profiling harness
- Job scheduling instrumentation
- Memory budget monitoring
- Regression alerting

## External Dashboard Integration

### Goal

Export telemetry data to external time-series databases (Grafana/InfluxDB) for long-term trend analysis and alerting.

### Design

**Data Export Format**:
- JSON Lines (one metric per line) for efficient streaming
- Timestamp, metric name, value, tags (system, registry, etc.)
- Batch export every N seconds or on scene unload

**Metrics to Export**:
- Frame time (per system group)
- Entity counts (per archetype)
- Registry entry counts and update frequencies
- Spatial grid rebuild times
- Memory allocations (NativeContainer usage)
- Burst job completion times

**Implementation Plan**:

1. **Create `TelemetryExportSystem`**
   - Runs in `LateSimulationSystemGroup`
   - Collects metrics from `FrameTimingStream`, `TelemetryStream`, registry instrumentation
   - Formats as JSON Lines
   - Writes to file or HTTP endpoint

2. **Configuration**
   - `TelemetryExportConfig` component (enabled, export interval, endpoint URL)
   - Support file-based export (CSV/JSON) and HTTP POST to InfluxDB

3. **Example Export Format**:
```json
{"timestamp":1234567890,"metric":"frame_time","value":15.2,"tags":{"system":"SpatialGridBuildSystem","group":"SpatialSystemGroup"}}
{"timestamp":1234567890,"metric":"entity_count","value":10000,"tags":{"archetype":"Villager"}}
{"timestamp":1234567890,"metric":"registry_entry_count","value":500,"tags":{"registry":"ResourceRegistry"}}
```

**Location**: `Runtime/Systems/Telemetry/TelemetryExportSystem.cs` (to be created)

**References**: `Docs/QA/PerformanceProfiles.md`

## Automated Profiling Harness

### Goal

Automatically capture profiling data after key commits or on-demand, storing results for regression analysis.

### Design

**Trigger Points**:
- Post-commit hooks (if enabled)
- Manual trigger via CI pipeline
- Nightly automated runs

**Data Captured**:
- Unity Profiler snapshots (.data files)
- Frame timing data (via `FrameTimingRecorderSystem`)
- Memory snapshots (NativeContainer allocations)
- Burst compilation logs

**Storage**:
- Artifacts directory: `CI/ProfilingResults/`
- Timestamped folders: `YYYY-MM-DD_HH-MM-SS/`
- Include commit hash in folder name

**Implementation Plan**:

1. **Create `ProfilingHarness` Script**
   - Runs performance soak scene for fixed duration (e.g., 1000 ticks)
   - Captures profiling data
   - Exports frame timing metrics
   - Saves Unity Profiler snapshot

2. **CI Integration**
   - Add profiling step to CI pipeline (optional, enabled via flag)
   - Compare against baseline metrics
   - Alert if regression detected (>20% frame time increase)

**Location**: `CI/run_profiling_harness.sh` (to be created)

**References**: `Docs/QA/PerformanceProfiles.md`, `Assets/Scenes/Perf/PerformanceSoakScene.unity`

## Job Scheduling Instrumentation

### Goal

Track job scheduling metrics (worker thread utilization, job completion times, dependency chains) to diagnose bottlenecks.

### Design

**Metrics to Track**:
- Job schedule count per frame
- Worker thread utilization (% busy)
- Job completion times (min/max/avg)
- Dependency chain depth
- Burst compilation status per job

**Implementation Plan**:

1. **Create `JobSchedulingInstrumentationSystem`**
   - Runs in `LateSimulationSystemGroup`
   - Uses Unity Profiler markers to track job execution
   - Aggregates metrics per system group
   - Stores to `TelemetryStream` or dedicated buffer

2. **Worker Thread Metrics**
   - Use `JobsUtility.JobWorkerCount` to get worker count
   - Estimate utilization from job completion times
   - Log spikes or idle periods

3. **Optional Logging**
   - Enable via `RuntimeConfig` flag
   - Log to console or telemetry stream
   - Disable in release builds

**Location**: `Runtime/Systems/Telemetry/JobSchedulingInstrumentationSystem.cs` (to be created)

**References**: `Docs/DesignNotes/ThreadingAndScheduling.md`

## Memory Budget Monitoring

### Goal

Track memory usage against defined budgets and alert when thresholds are exceeded.

### Design

**Budgets** (example):
- Total NativeContainer allocations: <500MB for 50k entities
- Blob asset memory: <100MB
- Pooled buffer peak usage: <200MB
- Per-entity memory: <10KB average

**Implementation Plan**:

1. **Create `MemoryBudgetMonitorSystem`**
   - Runs in `LateSimulationSystemGroup`
   - Queries `AllocationDiagnostics` (if available)
   - Calculates per-archetype memory usage
   - Compares against `MemoryBudgetConfig` singleton

2. **Alerting**
   - Log warnings when budgets exceeded
   - Emit telemetry events for dashboard
   - Optional: pause simulation if critical threshold breached

**Location**: `Runtime/Systems/Telemetry/MemoryBudgetMonitorSystem.cs` (to be created)

**Configuration**: `Runtime/Runtime/Config/MemoryBudgetConfig.cs` (to be created)

**References**: `Docs/QA/PerformanceProfiles.md`

## Regression Alerting

### Goal

Automatically detect performance regressions and alert developers.

### Design

**Detection Strategy**:
- Compare current metrics against baseline (previous commit or nightly run)
- Alert if frame time increases >20% or exceeds budget
- Alert if memory usage increases >30%
- Alert if any system exceeds its budget for >5 consecutive frames

**Implementation Plan**:

1. **Baseline Storage**
   - Store baseline metrics in `CI/Baselines/` (JSON format)
   - Update baseline on successful performance runs
   - Include commit hash and timestamp

2. **Comparison Logic**
   - Load baseline metrics
   - Compare current run against baseline
   - Generate alert report (JSON or HTML)

3. **CI Integration**
   - Run after performance tests
   - Fail build if critical regression detected
   - Post alert to CI comments or Slack/email

**Location**: `CI/compare_performance_baseline.sh` (to be created)

**References**: `Docs/QA/PerformanceProfiles.md`

## Performance Budgets

### Current Budgets (to be finalized)

**Per-System Budgets** (60 FPS target, <16ms total):
- `TimeSystemGroup`: <0.5ms
- `SpatialSystemGroup`: <1ms (rebuild), <0.5ms (queries)
- `EnvironmentSystemGroup`: <2ms (grid updates)
- `VillagerSystemGroup`: <2ms (AI + movement)
- `ResourceSystemGroup`: <1ms
- `MiracleEffectSystemGroup`: <1ms
- `PresentationSystemGroup`: <5ms (rendering excluded)

**Memory Budgets**:
- 10k entities: <100MB
- 50k entities: <500MB
- 100k entities: <1GB

**See**: `Docs/QA/PerformanceProfiles.md` for detailed targets

## Implementation Priority

1. **Phase 1** (High Priority):
   - Performance budgets documentation
   - Basic telemetry export (file-based)
   - Memory budget monitoring

2. **Phase 2** (Medium Priority):
   - External dashboard integration (InfluxDB)
   - Automated profiling harness
   - Regression alerting

3. **Phase 3** (Low Priority):
   - Job scheduling instrumentation (optional)
   - Advanced dashboard visualizations
   - Historical trend analysis

## References

- `Docs/QA/PerformanceProfiles.md` - Performance targets
- `Docs/DesignNotes/ThreadingAndScheduling.md` - Job scheduling policy
- `Runtime/Systems/Telemetry/` - Existing telemetry systems
- `CI/` - CI scripts and automation


