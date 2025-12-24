# Performance Profiles

This document tracks performance metrics for Burst-enabled builds across different entity counts and scenarios.

## Test Scenarios

### 100k Villager Stress Test

**Scene**: `PureDOTS/Assets/Scenes/Perf/PerformanceSoakScene.unity`

**Configuration**:
- Target villager count: 100,000
- Measurement interval: Every 10 ticks
- Log interval: Every 20 measurements

**Baseline (Before Burst Re-enablement)**:
- Frame time: TBD
- Job stats: TBD
- Memory: TBD

**Burst-Enabled (After Re-enablement)**:
- Frame time: TBD
- Job stats: TBD
- Memory: TBD

**Target Performance**:
- Frame time: < 16.67ms (60 FPS) for 100k entities
- Job utilization: > 80% worker utilization
- Memory: < 2GB for 100k entities

## Aggregate Lesson Telemetry Performance

**System**: `GodgameLessonTelemetrySystem`

**Configuration**:
- Update group: `PresentationSystemGroup`
- Burst compilation: Not applicable (presentation system)

**Metrics**:
- Update time: TBD
- Buffer operations: TBD
- Memory allocations: TBD

## Space4X Manual Groups Performance

**Systems**:
- `Space4XTransportUpdateGroup`
- `Space4XVesselUpdateGroup`
- `Space4XCameraUpdateGroup`

**Configuration**:
- Burst compilation: Enabled for simulation systems
- Presentation systems: Non-Burst (use Unity GameObjects)

**Metrics**:
- Transport update time: TBD
- Vessel update time: TBD
- Camera update time: TBD

## Intent System Performance

**Systems**:
- `IntentValidationSystem`
- `IntentProcessingSystem`
- `EnhancedInterruptHandlerSystem`
- `InterruptHandlerSystem`

**Configuration**:
- Burst compilation: Enabled for all intent systems
- Update group: `InterruptSystemGroup`
- Memory allocations: Zero per frame (Burst-compiled)

**Performance Targets**:

| Entity Count | Target Time | System |
|--------------|-------------|--------|
| 100 | < 0.1ms | IntentValidationSystem |
| 1,000 | < 1ms | IntentValidationSystem |
| 10,000 | < 10ms | IntentValidationSystem |
| 50,000 | < 50ms | IntentValidationSystem |
| 100,000 | < 100ms | IntentValidationSystem |

**Scaling Characteristics**:
- Linear scaling expected (performance should scale linearly with entity count)
- Sublinear scaling acceptable (due to cache efficiency at larger scales)

**Memory Targets**:
- Zero allocations per frame (verified in Burst-compiled systems)
- Bounded buffer sizes: Interrupt buffers ≤ 8 elements (InternalBufferCapacity)
- QueuedIntent buffers ≤ 4 elements (InternalBufferCapacity)

**Current Baselines** (to be measured):
- 100 entities: TBD
- 1,000 entities: TBD
- 10,000 entities: TBD
- 50,000 entities: TBD
- 100,000 entities: TBD

**Performance Test Thresholds**:
- Test thresholds include 2x safety margin for CI variance (Stopwatch precision ~1ms, environment variance, warm-up effects)
- Actual targets: 100 entities < 1ms, 1,000 entities < 2ms, 10,000 entities < 20ms
- Stopwatch precision limits sub-millisecond targets (0.1ms target is unrealistic, using 1ms as reasonable approximation)

**Test Coverage**:
- Performance tests: `IntentSystemIntegrationTests.cs`, `IntentSystemPerformanceTests.cs`
- Regression detection: CI integration ready (performance thresholds defined with 2x safety margin)
- Burst compilation: Validated via `[BurstCompile]` attribute checks

## Profiling Methodology

1. **Setup**: Enable Burst compilation for all hot-path assemblies
2. **Warmup**: Run scene for 60 seconds to stabilize
3. **Measurement**: Record frame times, job stats, and memory for 300 frames
4. **Analysis**: Calculate averages, percentiles (p50, p95, p99), and identify bottlenecks

## Tools

- Unity Profiler (CPU, Memory, Jobs)
- Burst Inspector (compilation validation)
- Custom telemetry systems (frame time, entity counts)

## Notes

- Performance profiles should be updated after major system changes
- Compare before/after metrics when enabling Burst on new systems
- Document any regressions and their root causes
