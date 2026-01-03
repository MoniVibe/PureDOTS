# CI & Build Automation Plan

## Overview

This document outlines the plan for automating build, test, and deployment pipelines for PureDOTS, including IL2CPP builds, test coverage reporting, and nightly stress runs.

## Current State

**Existing**:
- `CI/run_playmode_tests.sh` - Basic playmode test runner
- Manual build process (not automated)

**Gaps**:
- IL2CPP build automation
- Test coverage reporting
- Nightly stress runs
- Performance regression detection
- Multi-platform builds

## CI Pipeline Stages

### Stage 1: Fast Feedback (< 5 minutes)

**Purpose**: Catch compilation errors and basic test failures quickly.

**Steps**:
1. **Compilation Check**
   - Unity batch mode compilation
   - Burst compilation validation
   - Assembly definition validation

2. **Unit Tests (EditMode)**
   - Run EditMode tests (fast, no runtime)
   - Fail fast on failures

3. **Basic Integration Tests**
   - Quick integration tests (< 1 minute total)
   - Bootstrap smoke test

**Triggers**: Every commit, pull requests

**Output**: Pass/fail status, compilation logs

### Stage 2: Integration Validation (< 15 minutes)

**Purpose**: Validate multi-system interactions and runtime behavior.

**Steps**:
1. **Playmode Tests**
   - Full playmode test suite
   - Registry tests, spatial tests, rewind tests

2. **Performance Smoke Tests**
   - Quick performance checks (10k entities)
   - Basic frame time validation

3. **IL2CPP Build (Optional)**
   - Build IL2CPP standalone (Windows)
   - Run bootstrap smoke test in IL2CPP build
   - Validate `link.xml` coverage

**Triggers**: Pull requests, commits to `master`

**Output**: Test results, build artifacts, IL2CPP validation logs

### Stage 3: Nightly Deep Validation (< 60 minutes)

**Purpose**: Comprehensive validation including stress tests and determinism checks.

**Steps**:
1. **Full Test Suite**
   - All EditMode, Integration, Playmode tests
   - Performance benchmarks

2. **Stress Tests**
   - 100k entity soak test
   - Long-run determinism test (10,000 ticks)
   - Memory leak detection

3. **Deterministic Replay Validation**
   - Replay capture/playback tests
   - Snapshot comparison across runs
   - Scenario run log digests compared against stored baselines (see *Scenario Run Metadata* below)
4. **Nightly Scenario + Telemetry Diffs**
   - Scenario runs with telemetry diffs against baselines
   - Performance regression checks on runtime metrics

5. **IL2CPP Full Build**
   - Windows IL2CPP build
   - Run full test suite in IL2CPP
   - Burst compilation validation

6. **Test Coverage Report**
   - Generate coverage report
   - Compare against baseline
   - Alert if coverage drops

**Triggers**: Scheduled nightly runs, manual trigger

**Output**: Comprehensive test results, coverage reports, performance baselines

## Build Configuration

## Ops Bus Coordination (Nightly)

Nightly multi-agent runs use the ops bus protocol (`../Headless/OPS_BUS_PROTOCOL.md`) and these scripts:
- `Tools/Ops/tri_ops.py` (CLI for heartbeats/requests/claims/locks/results)
- `Tools/Ops/tri_wsl_bootstrap.sh` (WSL runner loop)
- `Tools/Ops/tri_ps_bootstrap.ps1` (Windows builder loop)

Use the ops bus for rebuild requests/results; do not rely on chat-only coordination.

## Scenario Run Metadata & Determinism Guards

Headless runs now emit deterministic NDJSON records guarded by environment variables:

| Env Var | Purpose |
| --- | --- |
| `PUREDOTS_SCENARIO_RUN_LOG` | Absolute/relative path for run log (NDJSON). When unset, logging is skipped. |
| `PUREDOTS_SCENARIO_BASELINE` | Optional path to a baseline NDJSON file. When provided, every digest hash is compared against baseline digests and the run fails (non-zero exit) on the first mismatch. |
| `PUREDOTS_SCENARIO_DIGEST_INTERVAL` | Interval in ticks between determinism digests (default `1`). Increase to reduce log size for longer runs. |

Each run writes a **header** (git commit/branch, platform, Unity version, catalog hashes, scenario id/seed), periodic **digest** records (per-tick component hashes + entity counts + RNG state), and a **summary** line (final frame stats, telemetry metrics). CI pipelines should archive both the log and the optional baseline file so regressions can be bisected quickly.

When the baseline variable is set, any replay drift immediately surfaces as a failed build step, satisfying the “automatically failing” audit requirement.

### Telemetry Events

`TelemetryExportConfig.Flags` now includes `IncludeTelemetryEvents`, which flushes high-signal NDJSON events from the shared `TelemetryEvent` buffer alongside the metric stream. Games emit events such as:

- `BehaviorProfileAssigned` – which doctrine/profile/role was applied to an agent
- `RoleStateChanged` / `GoalChanged` – phase/goal transitions with reason codes
- Domain-specific events (`AttackRunStart`, `AttackRunEnd`, `FormationCohesion`, `EscortCoverage`, etc.)

CI jobs that analyze headless runs should enable this flag so strike craft + peacekeeper audits can be diffed without replaying the scene.

### IL2CPP Build Checklist

**Pre-Build Validation**:
- [ ] Verify `link.xml` exists at `Assets/Config/Linker/link.xml`
- [ ] Run EditMode tests to catch compilation errors
- [ ] Verify Burst compilation succeeds

**Player Settings**:
- `Scripting Backend`: IL2CPP
- `Api Compatibility Level`: `.NET Standard 2.1`
- `Managed Stripping Level`: Low (validate `link.xml` coverage first)
- `Allow 'Unsafe' Code`: Enabled for DOTS assemblies

**Burst Settings**:
- `Burst AOT Compilation`: Enabled
- `CompileSynchronously`: Enabled in development builds

**Build Scripts**:
```bash
# IL2CPP build command
"$UNITY_PATH" -batchmode -quit -projectPath PureDOTS \
  -buildTarget Windows64 \
  -executeMethod BuildScript.BuildIL2CPP \
  -logFile build/Logs/IL2CPP_Build.log
```

**Post-Build Validation**:
- [ ] Check build log for `MissingMethodException` or `MissingTypeException` errors
- [ ] Run bootstrap smoke test in IL2CPP build
- [ ] Verify core systems initialize correctly
- [ ] Test rewind/playback functionality
- [ ] Check debug console enum parsing works

**Required link.xml Entries**:
- Bootstrap and system registration types
- Runtime config ScriptableObject types
- Enum types used by debug console
- Presentation bridge types
- Unity.Entities assemblies (preserve all)

**See**: 
- `Docs/QA/IL2CPP_AOT_Audit.md` for detailed reflection usage analysis and preservation requirements
- `Docs/TruthSources/PlatformPerformance_TruthSource.md` for platform policies

### Multi-Platform Builds

**Targets** (priority order):
1. Windows 64-bit (primary)
2. macOS (validation)
3. Linux (headless server)

**Configuration**:
- Use Unity Cloud Build or self-hosted runners
- Store build artifacts per platform
- Run platform-specific tests

## Test Coverage Reporting

### Coverage Collection

**Tool**: Unity Test Runner + Coverlet (or equivalent)

**Configuration**:
- Collect coverage for `PureDOTS.Runtime` assembly
- Exclude test assemblies and editor code
- Generate HTML and XML reports

**Thresholds**:
- Utilities/Helpers: >80%
- Core Systems: >60%
- Overall: >70%

### Integration

**CI Step**:
```bash
# Run tests with coverage
"$UNITY_PATH" -batchmode -quit -projectPath PureDOTS \
  -runTests -testPlatform PlayMode \
  -testResults TestResults/results.xml \
  -coverageResultsPath TestResults/Coverage/ \
  -coverageOptions "generateAdditionalMetrics;assemblyFilters:+PureDOTS.Runtime"
```

**Reporting**:
- Generate HTML coverage report
- Upload to CI artifacts
- Compare against baseline (fail if coverage drops >5%)

**Location**: `CI/run_coverage.sh` (to be created)

## Nightly Stress Runs

### Scenarios

1. **Entity Count Soak**
   - Spawn 100k entities
   - Run for 10,000 ticks
   - Monitor frame time, memory

2. **Registry Stress**
   - Rapid spawn/despawn (100/sec)
   - Validate registry determinism

3. **Rewind Stress**
   - Record 10,000 ticks
   - Rewind and replay
   - Validate determinism

**Implementation**:
- Use `PerformanceSoakScene.unity`
- Run via `CI/run_nightly_stress.sh`
- Store results in `CI/TestResults/Nightly/`

**Alerting**:
- Email/Slack notification on failures
- Performance regression alerts (>20% frame time increase)

**Location**: `CI/run_nightly_stress.sh` (to be created)

## Performance Regression Detection

### Baseline Management

**Storage**: `CI/Baselines/performance_baseline.json`

**Format**:
```json
{
  "commit": "abc123",
  "timestamp": "2025-01-27T00:00:00Z",
  "metrics": {
    "frame_time_avg": 14.5,
    "frame_time_p95": 18.2,
    "spatial_rebuild_time": 0.8,
    "registry_update_time": 0.3,
    "memory_usage_mb": 450
  }
}
```

### Comparison Logic

**Script**: `CI/compare_performance_baseline.sh`

**Rules**:
- Alert if frame time increases >20%
- Alert if memory increases >30%
- Alert if any system exceeds budget for >5 frames

**Action**:
- Fail build if critical regression
- Post alert to CI comments
- Notify team via Slack/email

## AssetBundle/Addressable Builds

### Purpose

Automate content builds for DOTS subscenes and presentation assets.

### Implementation

**Steps**:
1. Build Addressable asset catalogs
2. Build AssetBundles for subscenes
3. Validate bundle integrity
4. Upload to content delivery network (CDN) or storage

**Triggers**: On subscene/asset changes, nightly builds

**Location**: `CI/build_content.sh` (to be created)

## Headless Server Build

### Purpose

Validate headless server build for large-scale simulations and testing.

### Configuration

**Build Target**: Linux Server (headless)

**Settings**:
- Disable graphics/audio
- Enable Burst compilation
- Include DOTS runtime only

**Validation**:
- Run performance soak tests
- Validate determinism
- Check memory usage

**Location**: `CI/build_headless_server.sh` (to be created)

## Implementation Priority

### Phase 1 (High Priority)
1. IL2CPP build automation
2. Test coverage reporting
3. Nightly stress runs

### Phase 2 (Medium Priority)
4. Performance regression detection
5. Multi-platform builds (macOS, Linux)

### Phase 3 (Low Priority)
6. AssetBundle/Addressable automation
7. Headless server builds
8. Advanced alerting (Slack integration)

## CI Scripts to Create

1. `CI/run_il2cpp_build.sh` - IL2CPP build and validation
2. `CI/run_coverage.sh` - Test coverage collection and reporting
3. `CI/run_nightly_stress.sh` - Nightly stress test suite
4. `CI/compare_performance_baseline.sh` - Performance regression detection
5. `CI/build_content.sh` - AssetBundle/Addressable builds
6. `CI/build_headless_server.sh` - Headless server build

## References

- `CI/run_playmode_tests.sh` - Existing test runner
- `Docs/QA/IL2CPP_AOT_Audit.md` - IL2CPP validation requirements
- `Docs/QA/TestingStrategy.md` - Test execution strategy
- `Docs/QA/TelemetryEnhancementPlan.md` - Performance monitoring
- `Docs/TruthSources/PlatformPerformance_TruthSource.md` - Build configuration
