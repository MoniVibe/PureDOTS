# AI Validation & Testing Plan

**Last Updated**: 2025-01-XX (Created during AI gap closure planning)  
**Status**: Active  
**Purpose**: Define testing and metrics requirements for AI systems

## Overview

This document defines the validation framework, performance targets, and testing requirements for PureDOTS AI systems. It ensures AI systems meet performance, correctness, and determinism goals.

## Performance Targets

### System-Level Targets

| System | 10k Entities | 50k Entities | 100k Entities | Notes |
|--------|--------------|--------------|---------------|-------|
| `AISensorUpdateSystem` | < 1ms | < 3ms | < 5ms | Spatial queries, sensor updates |
| `AIUtilityScoringSystem` | < 0.5ms | < 2ms | < 4ms | Curve evaluations |
| `AISteeringSystem` | < 0.5ms | < 2ms | < 4ms | Direction calculations |
| `AITaskResolutionSystem` | < 0.1ms | < 0.5ms | < 1ms | Command emission |
| **Total AI Pipeline** | **< 2ms** | **< 8ms** | **< 15ms** | Combined overhead |

### Memory Targets

- **Zero allocations** per frame in AI systems (except during initialization)
- **Bounded buffer sizes**: Sensor readings ≤ `AISensorConfig.MaxResults`
- **Blob asset reuse**: Utility blobs shared across entities of same archetype

### Scalability Targets

- **Linear scaling**: Performance should scale linearly with entity count
- **Spatial optimization**: Sensor updates should benefit from spatial grid (O(log n) vs O(n))
- **Batch processing**: Systems should process entities in batches for cache efficiency

## Testing Requirements

### Unit Tests

#### Test Suite: `AISensorUpdateSystemTests.cs`

**Purpose**: Validate sensor update correctness and performance.

**Test Cases**:
1. **SensorRangeDetection**: Entities within range are detected, outside range are not
2. **CategoryFiltering**: Only entities matching sensor categories are detected
3. **MaxResultsLimit**: Sensor readings respect `MaxResults` limit
4. **UpdateInterval**: Sensors only update after `UpdateInterval` elapsed
5. **SpatialGridIntegration**: Sensor queries use spatial grid correctly
6. **DeterministicSorting**: Sensor readings are sorted deterministically (by entity index)

**Performance Tests**:
- `SensorUpdate_10kEntities_Under1ms`: Validates 10k entities update in < 1ms
- `SensorUpdate_50kEntities_Under3ms`: Validates 50k entities update in < 3ms

**Files**:
- `Assets/Tests/Playmode/AISensorUpdateSystemTests.cs` (new)

---

#### Test Suite: `AIUtilityScoringSystemTests.cs`

**Purpose**: Validate utility scoring correctness and curve evaluation.

**Test Cases**:
1. **CurveEvaluation**: Utility curves evaluate correctly (linear, quadratic, cubic)
2. **ThresholdFiltering**: Actions score 0 when sensor reading < threshold
3. **WeightApplication**: Action scores are multiplied by weight correctly
4. **MultiFactorScoring**: Actions with multiple factors sum correctly
5. **BestActionSelection**: Highest-scoring action is selected
6. **TieBreaking**: Ties are broken deterministically (by action index)

**Performance Tests**:
- `UtilityScoring_10kEntities_Under0_5ms`: Validates 10k entities score in < 0.5ms
- `UtilityScoring_50kEntities_Under2ms`: Validates 50k entities score in < 2ms

**Files**:
- `Assets/Tests/Playmode/AIUtilityScoringSystemTests.cs` (new)

---

#### Test Suite: `AISteeringSystemTests.cs`

**Purpose**: Validate steering calculations and movement direction.

**Test Cases**:
1. **DirectionCalculation**: Steering direction points toward target
2. **SpeedLimiting**: Velocity respects `MaxSpeed` limit
3. **AccelerationLimiting**: Acceleration respects `Acceleration` limit
4. **ObstacleAvoidance**: Steering avoids obstacles when `ObstacleLookAhead` set
5. **FlowFieldIntegration**: Steering uses flow field direction when available (future)

**Performance Tests**:
- `Steering_10kEntities_Under0_5ms`: Validates 10k entities steer in < 0.5ms
- `Steering_50kEntities_Under2ms`: Validates 50k entities steer in < 2ms

**Files**:
- `Assets/Tests/Playmode/AISteeringSystemTests.cs` (new)

---

#### Test Suite: `AITaskResolutionSystemTests.cs`

**Purpose**: Validate command queue creation and task resolution.

**Test Cases**:
1. **CommandEmission**: Commands are emitted for entities with selected actions
2. **TargetAssignment**: Commands include correct target entity/position
3. **ActionIndexMapping**: Command action indices match utility action indices
4. **QueueClearing**: Command queue is cleared after bridge systems consume

**Performance Tests**:
- `TaskResolution_10kEntities_Under0_1ms`: Validates 10k entities resolve in < 0.1ms
- `TaskResolution_50kEntities_Under0_5ms`: Validates 50k entities resolve in < 0.5ms

**Files**:
- `Assets/Tests/Playmode/AITaskResolutionSystemTests.cs` (new)

---

### Integration Tests

#### Test Suite: `AIIntegrationTests.cs` (Existing)

**Purpose**: Validate end-to-end AI pipeline integration.

**Test Cases**:
1. **AISystems_ProduceDeterministicCommandQueue**: Validates deterministic command generation
2. **VillagerAI_DetectsResources**: Villagers detect and target resources
3. **VesselAI_DetectsAsteroids**: Vessels detect and target asteroids
4. **BridgeSystem_ConsumesCommands**: Bridge systems consume and apply commands
5. **VirtualSensors_PopulateReadings**: Virtual sensors populate internal state (future)

**Files**:
- `Assets/Tests/Playmode/AIIntegrationTests.cs` (existing, expand)

---

#### Test Suite: `AIPerformanceTests.cs` (New)

**Purpose**: Validate performance targets at scale.

**Test Cases**:
1. **AI_Pipeline_10kEntities_Under2ms**: Total AI pipeline < 2ms for 10k entities
2. **AI_Pipeline_50kEntities_Under8ms**: Total AI pipeline < 8ms for 50k entities
3. **AI_Pipeline_100kEntities_Under15ms**: Total AI pipeline < 15ms for 100k entities
4. **AI_ZeroAllocations**: AI systems allocate zero bytes per frame
5. **AI_LinearScaling**: Performance scales linearly with entity count

**Files**:
- `Assets/Tests/Playmode/AIPerformanceTests.cs` (new)

---

### Determinism Tests

#### Test Suite: `AIDeterminismTests.cs` (New)

**Purpose**: Validate deterministic behavior across runs.

**Test Cases**:
1. **SensorReadings_Deterministic**: Identical inputs produce identical sensor readings
2. **UtilityScores_Deterministic**: Identical inputs produce identical utility scores
3. **CommandQueue_Deterministic**: Identical inputs produce identical command queues
4. **RewindCompatibility**: AI systems respect `RewindState.Mode` (skip during playback)

**Files**:
- `Assets/Tests/Playmode/AIDeterminismTests.cs` (new)

---

## Metrics & Instrumentation

### Performance Metrics Component

**Component**: `AIPerformanceMetrics` (singleton)

**Fields**:
```csharp
public struct AIPerformanceMetrics : IComponentData
{
    public float SensorUpdateTimeMs;
    public float UtilityScoringTimeMs;
    public float SteeringTimeMs;
    public float TaskResolutionTimeMs;
    public int SensorUpdateCount;
    public int UtilityScoringCount;
    public int SteeringCount;
    public int TaskResolutionCount;
    public long TotalAllocations;
    public int FrameCount;
}
```

**Usage**:
- Updated each frame by AI systems
- Exposed to debug HUD for visualization
- Logged to telemetry for analysis

**Files**:
- `Packages/com.moni.puredots/Runtime/Runtime/AI/AIPerformanceMetrics.cs` (new)
- `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs` (add metrics updates)

---

### Telemetry Hooks

**Purpose**: Expose AI metrics to external systems (HUD, analytics).

**Events**:
- `ai.sensor.update` - Sensor update timing
- `ai.utility.score` - Utility scoring timing
- `ai.steering.calc` - Steering calculation timing
- `ai.command.emit` - Command emission count

**Format**: JSON or structured log entries

**Files**:
- `Packages/com.moni.puredots/Runtime/Systems/Telemetry/AITelemetrySystem.cs` (new)

---

## Validation Checklist

### Pre-Commit Checklist

- [ ] All unit tests pass
- [ ] Integration tests pass
- [ ] Performance tests meet targets (10k entities)
- [ ] Zero allocations per frame (verified in profiler)
- [ ] Determinism tests pass
- [ ] Code review completed

### Pre-Release Checklist

- [ ] All test suites pass
- [ ] Performance targets met (10k, 50k, 100k entities)
- [ ] Memory targets met (zero allocations, bounded buffers)
- [ ] Documentation updated
- [ ] Telemetry hooks verified
- [ ] Debug visualization working

### Regression Prevention

- [ ] Performance benchmarks run in CI
- [ ] Determinism tests run in CI
- [ ] Memory allocation checks in CI
- [ ] Performance regression alerts configured

---

## Test Data & Scenarios

### Scenario 1: Basic Resource Gathering

**Setup**:
- 100 villagers
- 10 resource nodes
- 5 storehouses
- Sensor range: 30m
- Update interval: 0.5s

**Validation**:
- Villagers detect resources within range
- Utility scoring selects gather action
- Commands emitted correctly
- Bridge system applies commands

---

### Scenario 2: Large-Scale Performance

**Setup**:
- 10k entities (mix of villagers/vessels)
- 1k targets (resources/asteroids)
- Sensor range: 50m
- Update interval: 1.0s

**Validation**:
- Total AI pipeline < 2ms
- Zero allocations per frame
- Linear scaling verified

---

### Scenario 3: Determinism Validation

**Setup**:
- 100 entities
- Fixed seed for random operations
- Record 100 ticks
- Rewind to tick 50
- Replay to tick 100

**Validation**:
- Sensor readings identical across runs
- Utility scores identical
- Command queues identical
- No state drift

---

## Continuous Integration

### CI Pipeline Steps

1. **Unit Tests**: Run all unit test suites
2. **Integration Tests**: Run integration test suites
3. **Performance Tests**: Run performance benchmarks
4. **Determinism Tests**: Run determinism validation
5. **Memory Checks**: Verify zero allocations
6. **Documentation**: Validate documentation links

### Performance Regression Detection

**Thresholds**:
- 10k entities: Fail if > 2.5ms (25% over target)
- 50k entities: Fail if > 10ms (25% over target)
- 100k entities: Fail if > 18ms (20% over target)

**Action**: Alert on regression, block merge if > 50% over target

---

## Debugging Tools

### Runtime Debug Overlay

**Features**:
- Sensor range visualization (gizmos)
- Detected entities visualization (lines/spheres)
- Utility score display (text overlay)
- Command queue contents (list view)
- Performance metrics (timing display)

**Files**:
- `Packages/com.moni.puredots/Runtime/Systems/Debug/AIDebugOverlaySystem.cs` (new)
- `Packages/com.moni.puredots/Editor/AISensorGizmos.cs` (new)

---

### Profiler Integration

**Unity Profiler Markers**:
- `AISensorUpdateSystem.OnUpdate`
- `AIUtilityScoringSystem.OnUpdate`
- `AISteeringSystem.OnUpdate`
- `AITaskResolutionSystem.OnUpdate`

**Usage**: Enable in Unity Profiler to see AI system timing

---

## Future Enhancements

### Stress Testing

**Goal**: Validate AI systems at extreme scales (1M+ entities).

**Approach**:
- Hierarchical spatial queries
- GPU-accelerated utility scoring
- Batch command processing

**Timeline**: Future work (not in current backlog)

---

### Automated Tuning

**Goal**: Automatically tune utility curve parameters based on behavior metrics.

**Approach**:
- Collect behavior data (action frequencies, success rates)
- Use machine learning to optimize curve parameters
- Validate against designer intent

**Timeline**: Future work (research phase)

---

## Related Documentation

- `Docs/Guides/AI_Integration_Guide.md` - Integration guide
- `Docs/AI_Gap_Audit.md` - Gap audit
- `Docs/AI_Backlog.md` - Implementation backlog
- `Assets/Tests/Playmode/AIIntegrationTests.cs` - Existing integration tests

