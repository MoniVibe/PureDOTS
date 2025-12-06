# Advanced Features Integration Guide

**Last Updated**: 2025-01-27

This guide explains how to interface with and use the 15 advanced features implemented in PureDOTS. Each section provides API examples, integration patterns, and common use cases.

---

## 1. Deterministic Job Graph Scheduler

### Overview
Dynamic job dependency graph that only executes systems with dirty input components. Replaces fixed ComponentSystemGroup execution with dependency-aware scheduling.

### Usage

#### Declaring Dependencies
```csharp
[JobDependency(typeof(PhysicsSystem), DependencyType.After)]
[UpdateInGroup(typeof(GameplaySystemGroup))]
public partial struct MySystem : ISystem
{
    // System will only run after PhysicsSystem and only if its inputs are dirty
}
```

#### Setting System Budgets
```csharp
// In bootstrap or system OnCreate
var budgetEntity = entityManager.CreateEntity();
entityManager.AddComponentData(budgetEntity, new SystemBudget(
    costMs: 2.0f,      // Estimated cost per execution
    priority: 200,     // Higher = more important (0-255)
    maxMs: 16.67f      // Max allowed execution time
));
```

#### Integration
The `JobGraphExecutionSystem` automatically builds the dependency graph and executes systems. No manual integration needed - systems are automatically tracked.

**Files**: `Runtime/Scheduling/JobDependencyAttribute.cs`, `Runtime/Scheduling/JobGraphScheduler.cs`

---

## 2. Chrono-Profiling and Time Compression

### Overview
Tracks real vs sim time, compression factors, and drift across multiple ECS worlds. Enables per-world time scale control.

### Usage

#### Reading Time Metrics
```csharp
var telemetry = SystemAPI.GetSingleton<TelemetryStream>();
float compressionFactor = telemetry.CompressionFactor; // ΔSim / ΔReal
float driftMs = telemetry.DriftMs; // Drift across worlds
```

#### Setting Per-World Time Scale
```csharp
// Set time scale for a specific world
var coordinator = SystemAPI.GetSingletonRW<TimeCoordinator>();
coordinator.ValueRW.TimeScale = 10.0f; // 10x speed
coordinator.ValueRW.IsFrozen = false;

// Freeze a world
coordinator.ValueRW.IsFrozen = true;
```

#### Accessing World Time State
```csharp
var worldTime = SystemAPI.GetSingleton<WorldTimeState>();
double realTimeMs = worldTime.RealTimeMs;
double simTimeMs = worldTime.SimTimeMs;
```

**Files**: `Runtime/Time/TimeCoordinator.cs`, `Runtime/Systems/ChronoProfilingSystem.cs`

---

## 3. Meta-AI Decision Recording

### Overview
Records AI decision events for introspection, analytics, and replay scrubbing. Enables "scrubbing" AI thought processes backward and forward in time.

### Usage

#### Recording Decisions
```csharp
// In your AI system
var registryEntity = SystemAPI.GetSingletonEntity<DecisionEventRegistry>();
var registry = SystemAPI.GetComponentRW<DecisionEventRegistry>(registryEntity);
var buffer = state.EntityManager.GetBuffer<DecisionEventBuffer>(registryEntity);

var decision = new DecisionEvent(
    agent: agentGuid,
    type: (byte)DecisionType.UtilityScore,
    utility: bestScore,
    tick: timeState.Tick
);

DecisionEventBufferHelper.AddEvent(ref buffer, ref registry.ValueRW, decision);
```

#### Scrubbing AI Decisions
```csharp
// Get decisions for an agent at a specific tick
ulong agentGuid = ...;
uint targetTick = 1000;

if (AIDecisionDebugAPI.TryGetAgentDecisionsAtTick(
    entityManager, registryEntity, agentGuid, targetTick, out var events))
{
    foreach (var evt in events)
    {
        Debug.Log($"Decision: {evt.Type}, Utility: {evt.Utility}");
    }
    events.Dispose();
}

// Scrub forward/backward
AIDecisionDebugAPI.ScrubAgentDecisions(
    entityManager, registryEntity, agentGuid, currentTick, deltaTicks: -10, out var events);
```

**Files**: `Runtime/AI/DecisionEvent.cs`, `Runtime/AI/AIDecisionDebugAPI.cs`

---

## 4. Async Asset Streaming Bus

### Overview
Background thread loader for terrain/textures/audio. ECS systems consume ready assets via handles.

### Usage

#### Requesting Assets
```csharp
var request = new StreamingAssetRequest(
    assetPath: "Terrain/Chunk_001.asset",
    assetType: 0, // 0=terrain, 1=texture, 2=audio
    priority: 200,
    requester: myEntity
);

var bus = GetAssetStreamBus(); // Get singleton bus instance
ulong handleId = bus.RequestAsset(request);
```

#### Consuming Ready Assets
```csharp
// In AssetStreamConsumerSystem or your system
if (bus.TryGetHandle(handleId, out var handle))
{
    if (handle.Status == 2) // Ready
    {
        // Use asset data
        // Apply to entity components
    }
}
```

**Files**: `Runtime/Streaming/AssetStreamBus.cs`, `Runtime/Streaming/StreamHandle.cs`

---

## 5. Unified Math Kernel

### Overview
Burst-compiled math utilities with shared constants. Guarantees identical math across every ECS world, every platform, every tick.

### Usage

#### Using Math Utilities
```csharp
using PureDOTS.Runtime.Math;

// Vector operations
float3 normalized = MathKernel.NormalizeSafe(direction);
float distance = MathKernel.Distance(posA, posB);

// Interpolation
float3 lerped = MathKernel.Lerp(a, b, t);
float smooth = MathKernel.SmoothStep(0f, 1f, value);

// Noise (deterministic)
float noise = MathKernel.Noise2D(position.xy, seed: 12345);

// Random (deterministic, seed-based)
uint rngState = 12345;
float random = MathKernel.RandomFloat(ref rngState);
```

#### Using Constants
```csharp
float gravity = MathConstants.EarthGravity;
float airDensity = MathConstants.AirDensitySeaLevel;
float au = MathConstants.AstronomicalUnit;
```

#### Fixed-Point Math (Optional)
```csharp
// For cross-platform determinism when floating-point differences are unacceptable
int fixedPos = FixedPointMath.FloatToFixed(3.14f);
float backToFloat = FixedPointMath.FixedToFloat(fixedPos);
```

**Files**: `Runtime/Math/MathKernel.cs`, `Runtime/Math/MathConstants.cs`

---

## 6. Replay / Spectator Framework

### Overview
Writes command logs and tick hashes. Provides "jump to tick" API for debugging, QA, and future spectator mode.

### Usage

#### Recording Replay Data
```csharp
// Automatic via ReplayWriterSystem
// Commands are automatically logged from InputCommandLogState
// Tick hashes are computed each frame
```

#### Jumping to Tick
```csharp
var metadata = SystemAPI.GetSingleton<ReplayMetadata>();
if (ReplayJumpAPI.JumpToTick(ref state, targetTick: 1000, metadata))
{
    // World state will be restored to tick 1000
    // RewindCoordinatorSystem handles the actual restoration
}
```

#### Validating Replay Integrity
```csharp
var tickHashes = replayService.GetTickHashes(); // Get from ReplayService
bool isValid = ReplayJumpAPI.ValidateReplay(tickHashes, metadata);
```

**Files**: `Runtime/Replay/ReplayService.cs`, `Runtime/Replay/ReplayJumpAPI.cs`

---

## 7. Unified Spatial Query Layer

### Overview
Global spatial service with domain registration. One unified API for climate, comms, navmesh, and other spatial queries.

### Usage

#### Registering a Domain
```csharp
var managerEntity = SystemAPI.GetSingletonEntity<SpatialDomainManager>();
SpatialDomainHelper.RegisterDomain(
    entityManager, managerEntity,
    domainName: "climate",
    cellSize: 10f
);
```

#### Querying Spatial Data
```csharp
var queryService = new SpatialQueryService(Allocator.Temp);

// Query AABB
var bounds = new AABB { Min = min, Max = max };
var results = new NativeList<Entity>(Allocator.Temp);
queryService.QueryAABB(bounds, ref results);

// Query specific cell
int3 cellCoords = new int3(10, 0, 5);
queryService.QueryCell(cellCoords, ref results);
```

**Files**: `Runtime/Spatial/SpatialQueryService.cs`, `Runtime/Spatial/SpatialDomainRegistry.cs`

---

## 8. Data-Driven Balancing Layer

### Overview
Tuning profiles loaded from JSON with hot-reload support. Instant gameplay iteration without code rebuilds.

### Usage

#### Loading a Profile
```csharp
var profileRef = TuningProfileLoader.LoadFromJson("Configs/Tuning/PhysicsProfile.json");
var entity = entityManager.CreateEntity();
entityManager.AddComponentData(entity, new TuningProfileRef { Profile = profileRef });
```

#### Accessing Parameters
```csharp
ref var profile = ref profileRef.Value;
for (int i = 0; i < profile.Parameters.Length; i++)
{
    var param = profile.Parameters[i];
    if (param.Name.ToString() == "Gravity")
    {
        float gravity = param.Value;
    }
}
```

#### Hot-Reloading
```csharp
TuningProfileLoader.HotReloadProfile(
    entityManager, profileEntity,
    "Configs/Tuning/PhysicsProfile.json"
);
// Profile is updated live, metadata version increments
```

**Files**: `Runtime/Config/TuningProfile.cs`, `Runtime/Config/TuningProfileLoader.cs`

---

## 9. Deterministic Physics Layer

### Overview
Uses analytic solutions (Keplerian orbits) when possible, falls back to RK2 integration. Deterministic, reversible physics.

### Usage

#### Orbital Mechanics
```csharp
KeplerianOrbit.CalculateOrbitalState(
    semiMajorAxis: 100f,
    eccentricity: 0.1f,
    inclination: 0f,
    argumentOfPeriapsis: 0f,
    longitudeOfAscendingNode: 0f,
    meanAnomaly: 0f,
    gravitationalParameter: MathConstants.GravitationalConstant,
    out float3 position,
    out float3 velocity
);
```

#### RK2 Integration
```csharp
RK2Integration.Integrate(
    position, velocity, acceleration, deltaTime,
    out float3 newPosition,
    out float3 newVelocity
);
```

**Files**: `Runtime/Physics/DeterministicPhysicsSystem.cs`, `Runtime/Physics/KeplerianOrbit.cs`

---

## 10. Procedural Mission Generator

### Overview
Headless system that generates deterministic scenarios for stress testing. Outputs JSON compatible with ScenarioRunner.

### Usage

#### Generating a Scenario
```csharp
var parameters = new ScenarioParameters(
    entityCount: 100000,
    seed: 12345
);

var entity = entityManager.CreateEntity();
entityManager.AddComponentData(entity, parameters);
// ScenarioGeneratorSystem will run once and generate scenario JSON
```

#### Running in CI
```powershell
# CI script
.\CI\run_scenario_benchmark.ps1 -EntityCount 100000 -OutputPath "Scenarios/Benchmark.json"
```

**Files**: `Runtime/Scenario/ScenarioGeneratorSystem.cs`, `CI/run_scenario_benchmark.ps1`

---

## 11. Cross-World Communication Bus

### Overview
Routes events between ECS worlds deterministically. Example: Godgame divine intervention → Space4X orbital climate change.

### Usage

#### Sending Messages
```csharp
var busState = SystemAPI.GetSingleton<WorldBusState>();
var worldBus = GetWorldBus(); // Get singleton bus instance

var payload = new FixedBytes64();
// Pack message data into payload

WorldBusRouter.SendMessage(
    ref worldBus,
    sourceWorld: busState.WorldId,
    targetWorld: 1, // Target world ID
    payload: payload,
    tick: timeState.Tick,
    messageType: 0 // Message type enum
);
```

#### Receiving Messages
```csharp
// Messages are automatically routed to target world's buffer
var messageBuffer = state.EntityManager.GetBuffer<WorldMessage>(busEntity);
for (int i = 0; i < messageBuffer.Length; i++)
{
    var message = messageBuffer[i];
    if (message.MessageType == 0) // Your message type
    {
        // Process message
    }
}
```

**Files**: `Runtime/WorldBus/WorldBus.cs`, `Runtime/Systems/WorldBusSystem.cs`

---

## 12. AI Debug Visualizer

### Overview
Overlay system for perception ranges, flowfield gradients, decision heatmaps. Debug-only, disabled in release builds.

### Usage

#### Enabling Visualizations
```csharp
// Add AIDebugMenu MonoBehaviour to scene
// Toggle visualizations via GUI:
// - Show Perception Ranges
// - Show Flowfield Gradients  
// - Show Decision Heatmaps
```

#### Accessing Overlay Data
```csharp
// In your system (editor/debug builds only)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
var overlay = GetAIDebugOverlay(); // Get singleton
for (int i = 0; i < overlay.PerceptionRangeCenters.Length; i++)
{
    DrawGizmo(overlay.PerceptionRangeCenters[i], overlay.PerceptionRangeRadii[i]);
}
#endif
```

**Files**: `Runtime/Debug/AIDebugOverlay.cs`, `Runtime/Debug/AIDebugMenu.cs`

---

## 13. Self-Profiling CI Benchmarks

### Overview
Headless CLI for performance benchmarks. Logs metrics, compares vs baseline, auto-flags regressions.

### Usage

#### Running Benchmarks
```bash
Unity -batchmode -executeMethod PureDOTS.Runtime.Devtools.PerfRunner.Run \
    --ticks 10000 \
    --export metrics.json
```

#### Accessing Metrics
```csharp
var metrics = new BenchmarkMetrics(Allocator.Temp);
metrics.RecordSystemGroupTime("GameplaySystemGroup", 5.2f);

// Compare vs baseline
var baseline = LoadBaselineMetrics();
bool hasRegressions = BaselineComparison.CompareMetrics(
    metrics, baseline, regressionThreshold: 0.1f, out var regressions
);
```

**Files**: `Runtime/Devtools/PerfRunner.cs`, `CI/run_perf_benchmark.ps1`

---

## 14. In-World Editor Integration

### Overview
Unity Editor windows for live-preview, entity placement, blob modification, and scenario management.

### Usage

#### Opening Editor Windows
```csharp
// Menu: PureDOTS → Scenario Runner
// Menu: PureDOTS → In-World Entity Placer
```

#### Scenario Management
```csharp
// In ScenarioRunnerEditor window:
// - Load scenario from JSON
// - Run/stop scenario
// - Auto-save on play mode exit (via ScenarioAutoSave)
```

**Files**: `Editor/ScenarioRunnerEditor.cs`, `Editor/InWorldEntityPlacer.cs`

---

## 15. Long-Term Scalability Path

### Overview
Binary serialization, world partitioning, and distributed simulation interface stub. Foundation for future multiplayer and cluster simulation.

### Usage

#### Binary Serialization
```csharp
var data = new MyStruct { Value = 42 };
var buffer = new NativeList<byte>(Allocator.Temp);
BinarySerialization.Serialize(data, ref buffer);

// Deserialize
BinarySerialization.Deserialize(buffer.AsArray(), 0, out MyStruct result);
```

#### World Partitioning
```csharp
var partition = new WorldPartition(
    cellCoords: new int3(10, 0, 5),
    authorityId: 1,
    isLocal: true
);

// Check if cell is local
bool isLocal = WorldPartitionManager.IsLocalCell(
    cellCoords, localAuthorityId: 1, partitionSize: 4
);
```

**Files**: `Runtime/Serialization/BinarySerialization.cs`, `Docs/ScalabilityPath.md`

---

## Integration Checklist

When integrating these features:

1. **Job Graph Scheduler**: Add `[JobDependency]` attributes to systems
2. **Chrono-Profiling**: Read `TelemetryStream` for time metrics
3. **AI Decision Recording**: Hook into AI systems to record decisions
4. **Asset Streaming**: Request assets via `AssetStreamBus`, consume via handles
5. **Math Kernel**: Replace scattered math calls with `MathKernel` utilities
6. **Replay Framework**: Use `ReplayJumpAPI` for debugging/QA
7. **Spatial Queries**: Register domains, use `SpatialQueryService`
8. **Tuning Profiles**: Load JSON profiles, hot-reload for iteration
9. **Deterministic Physics**: Use `KeplerianOrbit` or `RK2Integration`
10. **Mission Generator**: Generate scenarios for stress testing
11. **World Bus**: Send/receive messages between worlds
12. **AI Debug**: Enable visualizations via `AIDebugMenu`
13. **CI Benchmarks**: Run `PerfRunner` in CI, compare vs baseline
14. **Editor Integration**: Use editor windows for live debugging
15. **Scalability**: Use `BinarySerialization` for all I/O, prepare for partitioning

---

## Common Patterns

### Pattern: Recording AI Decisions
```csharp
// In AIUtilityScoringSystem or AITaskResolutionSystem
var decision = new DecisionEvent(agentGuid, type, utility, tick);
DecisionEventBufferHelper.AddEvent(ref buffer, ref registry, decision);
```

### Pattern: Cross-World Communication
```csharp
// Godgame → Space4X
WorldBusRouter.SendMessage(ref bus, sourceWorld: 0, targetWorld: 1, payload, tick, type);
```

### Pattern: Hot-Reload Tuning
```csharp
// Designer changes JSON, system hot-reloads
TuningProfileLoader.HotReloadProfile(entityManager, entity, jsonPath);
```

### Pattern: Deterministic Math
```csharp
// Always use MathKernel for cross-platform determinism
float3 normalized = MathKernel.NormalizeSafe(direction);
float noise = MathKernel.Noise2D(position.xy, seed);
```

---

## References

- **System Groups**: `Docs/SystemOrdering/SystemSchedule.md`
- **Telemetry**: `Runtime/Runtime/Telemetry/TelemetryComponents.cs`
- **AI Systems**: `Runtime/Systems/AI/AISystems.cs`
- **Replay**: `Runtime/Replay/ReplayService.cs`
- **Scalability**: `Docs/ScalabilityPath.md`

