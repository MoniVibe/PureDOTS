# Testing Strategy & Pyramid

## Overview

PureDOTS follows a structured testing pyramid to ensure deterministic simulation, catch regressions early, and validate performance targets. This document defines the testing layers, automation requirements, and integration with CI/CD.

## Testing Pyramid

### 1. Unit Tests (Base Layer)

**Purpose**: Validate individual components, systems, and utilities in isolation.

**Scope**:
- Component data validation (structs, enums, constants)
- Utility functions (spatial hashing, registry queries, blob lookups)
- Static helper methods (distance calculations, angle conversions)
- Blob asset serialization/deserialization

**Location**: `Assets/Tests/EditMode/`

**Examples**:
- `SpatialPartitionProfileTests.cs` - Profile validation
- `StreamingValidatorTests.cs` - SubScene validation
- Utility method tests (to be added)

**Run Frequency**: Every commit (fast feedback loop)

**Coverage Target**: >80% for utility/helper code

### 2. Integration Tests (Middle Layer)

**Purpose**: Validate multi-system interactions and deterministic behavior.

**Scope**:
- Registry rebuilds and spatial sync
- Cross-system data flow (villager → resource → storehouse)
- Entity lifecycle (spawn → update → despawn)
- Rewind/playback state transitions

**Location**: `Assets/Tests/Integration/` and `Assets/Tests/Playmode/`

**Examples**:
- `SpatialQueryTests.cs` - Spatial grid query validation
- `RegistryMutationTests.cs` - Registry update determinism
- `SystemIntegrationPlaymodeTests.cs` - Multi-system scenarios

**Run Frequency**: Every commit (CI) + pre-merge

**Coverage Target**: Critical paths >90% (registry, spatial, rewind)

### 3. Playmode Tests (Middle-Upper Layer)

**Purpose**: Validate runtime behavior in actual Unity environment with full DOTS world.

**Scope**:
- Bootstrap and singleton initialization
- System execution order and dependencies
- Entity Component System (ECS) archetype handling
- Burst compilation and job scheduling
- Presentation bridge synchronization

**Location**: `Assets/Tests/Playmode/`

**Examples**:
- `BootstrapSmokeTest.cs` - Core bootstrap validation
- `RewindIntegrationTests.cs` - Rewind state transitions
- `VillagerJobLoopTests.cs` - Villager AI workflow
- `ResourceProcessingTests.cs` - Resource economy determinism

**Run Frequency**: Every commit (CI) + nightly

**Coverage Target**: All core systems have at least one playmode test

### 4. Performance Tests (Upper Layer)

**Purpose**: Validate performance budgets and catch regressions.

**Scope**:
- Frame time budgets (<16ms @ 60 FPS)
- System-specific budgets (spatial <1ms, registries <0.5ms)
- Memory budgets (<500MB for 50k entities)
- Scalability (10k → 50k → 100k entities)

**Location**: `Assets/Scenes/Perf/PerformanceSoakScene.unity` + dedicated test fixtures

**Examples**:
- `SpatialRegistryPerformanceTests.cs` - Spatial grid rebuild performance
- Performance soak scene automation (to be implemented)

**Run Frequency**: Nightly + on performance-critical PRs

**Coverage Target**: All hot-path systems have performance benchmarks

### 5. Deterministic Replay Tests (Validation Layer)

**Purpose**: Validate deterministic execution across runs and platforms.

**Scope**:
- Replay capture/playback determinism
- Snapshot comparison across runs
- Cross-platform determinism (Windows, macOS, Linux)
- IL2CPP determinism validation

**Location**: `Assets/Tests/Playmode/` + dedicated replay harness

**Examples**:
- `ReplayCaptureSystemTests.cs` - Replay capture validation
- Deterministic replay harness (to be implemented)

**Run Frequency**: Nightly + before releases

**Coverage Target**: Critical gameplay loops have replay tests

## Test Execution Strategy

### Continuous Integration (CI)

**Triggers**:
- Every commit to `master` or feature branches
- Pull request validation
- Scheduled nightly runs

**Pipeline Stages**:

1. **Fast Feedback** (< 5 minutes)
   - Unit tests (EditMode)
   - Quick integration tests
   - Compilation/Burst validation

2. **Integration Validation** (< 15 minutes)
   - Full integration test suite
   - Playmode bootstrap tests
   - Basic performance smoke tests

3. **Nightly Deep Validation** (< 60 minutes)
   - Full test suite (all layers)
   - Performance soak tests
   - Deterministic replay validation
   - IL2CPP build + test

### Test Organization

```
Assets/Tests/
├── EditMode/          # Unit tests (fast, no Unity runtime)
├── Integration/       # Multi-system integration tests
├── Playmode/          # Runtime tests (full DOTS world)
└── Perf/              # Performance benchmarks (optional)
```

**Test Fixtures**:
- `EcsTestFixture.cs` - Base class for ECS tests
- `TestWorldExtensions.cs` - World setup helpers
- `TestEntityManagerExtensions.cs` - Entity creation helpers

## Deterministic Replay Harness

### Purpose

Validate that simulation runs produce identical results across multiple executions, platforms, and Unity versions. Critical for rewind functionality and debugging.

### Design

**Components**:

1. **Replay Capture** (`ReplayCaptureSystem`)
   - Captures input commands, entity spawns, and key events
   - Stores to `ReplayCaptureStream` buffer
   - Already implemented; needs harness integration

2. **Snapshot System** (to be implemented)
   - Takes deterministic snapshots at fixed intervals (e.g., every 100 ticks)
   - Serializes entity states, registry buffers, spatial grid state
   - Compares snapshots across runs (hash-based or full comparison)

3. **Replay Harness** (to be implemented)
   - Loads replay file and executes simulation
   - Validates snapshots match expected values
   - Reports discrepancies with detailed diffs

**Implementation Plan**:

```csharp
// Snapshot format (conceptual)
public struct DeterministicSnapshot
{
    public uint Tick;
    public Unity.Entities.Hash128 StateHash; // Composite hash of all registries + entities
    public NativeList<EntitySnapshot> Entities; // Critical entities only
    public NativeList<RegistrySnapshot> Registries; // Registry entry counts + hashes
}

// Harness test example
[Test]
public void VillagerLoop_IsDeterministic()
{
    var replay = LoadReplay("villager_loop_1000ticks.replay");
    var world = CreateTestWorld();
    
    for (int run = 0; run < 3; run++)
    {
        ResetWorld(world);
        ExecuteReplay(world, replay);
        
        var snapshot = CaptureSnapshot(world, 1000);
        if (run == 0)
        {
            _baselineSnapshot = snapshot;
        }
        else
        {
            AssertSnapshotsMatch(_baselineSnapshot, snapshot);
        }
    }
}
```

**Location**: `Assets/Tests/Playmode/DeterministicReplayHarness.cs` (to be created)

**References**: `Docs/QA/PerformanceProfiles.md`, `Docs/DesignNotes/HistoryBufferPatterns.md`

## Nightly Soak Suite

### Purpose

Stress test systems with high entity counts, long runtimes, and memory pressure to catch leaks, performance regressions, and determinism issues.

### Scenarios

1. **Entity Count Soak**
   - Spawn 100k entities (villagers, resources, creatures)
   - Run for 10,000 ticks (≈167 seconds @ 60 FPS)
   - Monitor frame time, memory usage, registry update times
   - Validate no memory leaks (check `NativeContainer` usage)

2. **Registry Stress**
   - Rapid spawn/despawn cycles (100 entities/sec)
   - Validate registry rebuild determinism
   - Check spatial sync continuity
   - Monitor registry health alerts

3. **Rewind Stress**
   - Record 10,000 ticks of gameplay
   - Rewind to tick 0, then replay forward
   - Validate state matches original run
   - Measure rewind/playback performance

4. **Spatial Grid Stress**
   - Entities moving continuously across grid boundaries
   - Frequent spatial rebuilds (force dirty ops)
   - Validate grid consistency and rebuild performance

**Implementation**:

- Leverage `PerformanceSoakScene.unity` for entity spawning
- Add dedicated soak test fixtures
- Integrate with CI nightly runner

**Location**: `Assets/Tests/Playmode/SoakTests.cs` (to be created)

**References**: `Docs/QA/PerformanceProfiles.md`

## Regression Scenes

### Purpose

Automated validation of critical gameplay loops using real scene setups, ensuring end-to-end functionality remains intact.

### Scenarios

1. **Villager Loop**
   - Scene: Village with villagers, resources, storehouse
   - Validation: Villagers gather resources → deposit → repeat
   - Metrics: Job completion rate, resource flow, frame time

2. **Miracle Rain**
   - Scene: Village with dry ground, divine hand available
   - Validation: Player triggers rain miracle → moisture grid updates → vegetation grows
   - Metrics: Miracle effect application, environment grid updates

3. **Resource Delivery**
   - Scene: Multiple villages, resource nodes, carriers
   - Validation: Resources transported between locations
   - Metrics: Logistics request fulfillment, carrier pathfinding

4. **Hand Router Conflicts**
   - Scene: Multiple players/input sources
   - Validation: Input routing works correctly, no conflicts
   - Metrics: Input latency, conflict resolution

**Implementation**:

- Create dedicated regression scenes under `Assets/Scenes/Regression/`
- Add scene-specific test fixtures
- Automate via CI (load scene → run N ticks → validate state)

**Location**: `Assets/Scenes/Regression/` + `Assets/Tests/Playmode/RegressionSceneTests.cs` (to be created)

**References**: `Docs/QA/IntegrationTestChecklist.md` (to be populated)

## Test Coverage Reporting

### Metrics

- **Code Coverage**: Track via Unity Test Runner or external tools (e.g., Coverlet)
- **System Coverage**: Ensure all core systems have tests
- **Path Coverage**: Critical gameplay paths covered

### Integration

- Export coverage reports to CI artifacts
- Generate HTML reports for review
- Set coverage thresholds (warn if <80% for utilities, <60% for systems)

**Implementation**: Add coverage reporting step to CI pipeline

## References

- `Docs/QA/PerformanceProfiles.md` - Performance targets and metrics
- `Docs/QA/IL2CPP_AOT_Audit.md` - IL2CPP testing requirements
- `Docs/QA/BootstrapAudit.md` - Bootstrap validation
- `Docs/TODO/Utilities_TODO.md` - Testing infrastructure tasks
- `Assets/Tests/Playmode/BootstrapSmokeTest.cs` - Example smoke test


