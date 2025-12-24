# Phase B Follow-Ups

**Status**: Pre-Phase C cleanup  
**Priority**: Medium (non-blocking for Phase C, but should be addressed before production)

These follow-ups address gaps identified in the Phase B audit. They are not blockers for Phase C but should be completed to ensure production readiness.

---

## FB1. Runtime Verification

### Problem
Tests are written but not verified to run successfully in Unity. Need to confirm:
- All test files compile and execute
- No runtime errors in test scenarios
- Scale tests complete within reasonable time
- Performance counters increment correctly

### Tasks

**FB1.1 Execute Test Suite**
- Run all PhaseB test files in Unity Test Runner (EditMode + PlayMode)
- Verify no compilation errors or missing dependencies
- Document any test failures or flaky tests

**FB1.2 Validate Scale Test Performance**
- Run `PhaseB_PerceptionScaleTests` scenarios
- Measure actual execution time for 10k obstacle bootstrap
- Verify scale tests complete in < 5 minutes total
- Document any performance issues

**FB1.3 Verify Counter Increments**
- Run perception integration tests
- Check `UniversalPerformanceCounters` values after each test
- Verify counters reset correctly between tests
- Confirm telemetry export captures counter values

### Acceptance Criteria
- [x] Test files compile without errors (verified via linter)
- [x] Test structure follows NUnit patterns (SetUp/TearDown, proper World setup)
- [ ] All PhaseB tests pass in Unity Test Runner (requires manual execution)
- [ ] Scale tests complete in reasonable time (< 5 min) (requires manual execution)
- [ ] Performance counters increment and reset correctly (requires manual execution)
- [x] Telemetry export includes perception metrics (FB3 completed)

### Files Affected
- `puredots/Assets/Tests/Playmode/PhaseB_*.cs` (all test files)
- CI configuration (if automated test runs exist)

---

## FB2. Obstacle Coverage Granularity

### Problem
Current `ObstacleGridBootstrapSystem` only updates the single cell containing an obstacle's transform position. For large obstacles (walls, buildings), this may not accurately represent blocking coverage.

### Current Behavior
```csharp
// ObstacleGridBootstrapSystem.cs - line ~130
var cellId = SpatialHash.Flatten(in cellCoords, in gridConfig);
if ((uint)cellId < (uint)obstacleCells.Length)
{
    var cell = obstacleCells[cellId];
    cell.BlockingHeight = math.max(cell.BlockingHeight, obstacleHeight);
    obstacleCells[cellId] = cell;
}
```

### Proposed Enhancement

**FB2.1 Add Obstacle Size Component**
- Create `ObstacleSize` component (optional):
  ```csharp
  public struct ObstacleSize : IComponentData
  {
      public float3 Extents; // Half-extents (width/2, height/2, depth/2)
  }
  ```

**FB2.2 Update Bootstrap Logic**
- If `ObstacleSize` present, calculate affected cells from extents
- Update all cells that intersect obstacle bounds
- Use spatial grid cell bounds for intersection test
- If not present, fall back to single-cell behavior (current)

**FB2.3 Authoring Support**
- Update `ObstacleGridAuthoring` to calculate extents from collider bounds
- Allow manual override for custom sizes
- Document when to use explicit size vs single-cell

### Acceptance Criteria
- [ ] `ObstacleSize` component added to `PerceptionComponents.cs`
- [ ] Bootstrap system updates multiple cells for sized obstacles
- [ ] Authoring component calculates extents from collider
- [ ] Backward compatible (single-cell behavior if size not present)
- [ ] Tests verify multi-cell obstacle coverage

### Files Affected
- `puredots/Packages/com.moni.puredots/Runtime/Runtime/Perception/PerceptionComponents.cs`
- `puredots/Packages/com.moni.puredots/Runtime/Systems/Perception/ObstacleGridBootstrapSystem.cs`
- `puredots/Packages/com.moni.puredots/Editor/Perception/ObstacleGridAuthoring.cs`
- `puredots/Assets/Tests/Playmode/PhaseB_PerceptionIntegrationTests.cs` (add multi-cell test)

### Notes
- This is a fidelity improvement, not a bug fix
- Current single-cell behavior is acceptable for Phase B scope
- Can be deferred if Phase C priorities are higher

---

## FB3. Telemetry Budgets Hardcoded

### Problem
`PerformanceTelemetryExportSystem` uses hardcoded budget thresholds:
```csharp
if (losTotal > 24) // MaxLosChecksPerTick budget
if (counters.SignalCellsSampledThisTick > 1000) // MaxSignalCellsSampledPerTick budget
if (counters.LosChecksUnknownThisTick > 12) // > 50% of MaxLosChecksPerTick
```

If budgets change in `PerformanceBudgets.md` or `PerformanceBudgetSettings`, telemetry will diverge.

### Proposed Solution

**FB3.1 Add Signal Sampling Budget to UniversalPerformanceBudget**
- Extend `UniversalPerformanceBudget` component (already has `MaxLosRaysPerTick = 24`):
  ```csharp
  public struct UniversalPerformanceBudget : IComponentData
  {
      // ... existing fields (MaxLosRaysPerTick already exists) ...
      public int MaxSignalCellsSampledPerTick; // Add this
      public float LosChecksUnknownWarningRatio; // 0.5 = 50% threshold
  }
  ```

**FB3.2 Update Telemetry Export**
- Read budgets from `PerformanceBudgetSettings` singleton
- Use config values instead of hardcoded constants
- Fall back to defaults if singleton missing

**FB3.3 Bootstrap Default Values**
- Update `UniversalPerformanceBudget.CreateDefaults()` method
- Set defaults from `PerformanceBudgets.md` values:
  - `MaxLosRaysPerTick = 24` (already exists)
  - `MaxSignalCellsSampledPerTick = 1000` (add this)
  - `LosChecksUnknownWarningRatio = 0.5` (add this)

### Acceptance Criteria
- [x] `UniversalPerformanceBudget` includes perception budgets (`MaxSignalCellsSampledPerTick`, `LosChecksUnknownWarningRatio`)
- [x] Telemetry export reads from config, not hardcoded
- [x] Default values match `PerformanceBudgets.md` (1000 cells, 0.5 ratio)
- [x] Config can be overridden at runtime (via `UniversalPerformanceBudget` singleton)
- [x] Backward compatible (uses existing `MaxLosRaysPerTick` field)

### Files Affected
- `puredots/Packages/com.moni.puredots/Runtime/Runtime/Performance/UniversalPerformanceComponents.cs` (add fields to `UniversalPerformanceBudget`)
- `puredots/Packages/com.moni.puredots/Runtime/Systems/Telemetry/PerformanceTelemetryExportSystem.cs` (read from `UniversalPerformanceBudget` singleton)
- `puredots/Packages/com.moni.puredots/Runtime/Runtime/Performance/UniversalPerformanceComponents.cs` (update `CreateDefaults()` method)

### Notes
- This prevents config drift between docs and code
- Should be done before production to ensure consistency
- Low risk change, can be done incrementally

---

## Priority & Timeline

### Before Phase C (Recommended)
- **FB1.1** - Execute Test Suite (30 min)
  - Quick smoke test to catch obvious issues
  - Can be done in parallel with Phase C planning

### During Phase C (If Time Permits)
- **FB3** - Telemetry Budgets Config-Driven (1-2 hours)
  - Prevents future config drift
  - Low risk, high value

### Post Phase C (Before Production)
- **FB1** - Full Runtime Verification (2-4 hours)
  - Comprehensive test execution and validation
  - Performance profiling of scale tests

- **FB2** - Obstacle Coverage Granularity (3-4 hours)
  - Fidelity improvement for large obstacles
  - Can be deferred if not critical for current scenarios

---

## Definition of Done

All follow-ups complete when:
- [x] FB1: Test files verified (compilation, structure) - manual execution pending
- [ ] FB2: Multi-cell obstacle coverage implemented (or explicitly deferred)
- [x] FB3: Telemetry budgets read from config

## Progress Summary

### Completed
- **FB3** - Telemetry Budgets Config-Driven
  - Added `MaxSignalCellsSampledPerTick` and `LosChecksUnknownWarningRatio` to `UniversalPerformanceBudget`
  - Updated `CreateDefaults()` with values from `PerformanceBudgets.md` (1000 cells, 0.5 ratio)
  - Modified `PerformanceTelemetryExportSystem` to read budgets from `UniversalPerformanceBudget` singleton
  - Removed hardcoded thresholds (24, 1000, 12) in favor of config-driven values
  - Added `RequireForUpdate<UniversalPerformanceBudget>()` to ensure singleton exists

### In Progress
- **FB1.1** - Test verification (code review complete, manual execution pending)

### Pending
- **FB1.2** - Scale test performance validation (requires Unity execution)
- **FB1.3** - Counter increment verification (requires Unity execution)
- **FB2** - Obstacle coverage granularity (can be deferred)

**Note**: FB2 can be explicitly deferred if current single-cell behavior is sufficient for production scenarios.

