# Advanced Features Quick Reference

**Last Updated**: 2025-01-27

Quick copy-paste examples for common integration patterns.

---

## Common Integration Patterns

### 1. Declare System Dependency
```csharp
[JobDependency(typeof(PhysicsSystem), DependencyType.After)]
[UpdateInGroup(typeof(GameplaySystemGroup))]
public partial struct MySystem : ISystem { }
```

### 2. Record AI Decision
```csharp
var decision = new DecisionEvent(agentGuid, (byte)DecisionType.Action, utility, tick);
DecisionEventBufferHelper.AddEvent(ref buffer, ref registry, decision);
```

### 3. Use Math Kernel
```csharp
using PureDOTS.Runtime.Math;
float3 normalized = MathKernel.NormalizeSafe(direction);
float noise = MathKernel.Noise2D(position.xy, seed);
```

### 4. Send Cross-World Message
```csharp
WorldBusRouter.SendMessage(ref bus, sourceWorld: 0, targetWorld: 1, payload, tick, type);
```

### 5. Jump to Replay Tick
```csharp
ReplayJumpAPI.JumpToTick(ref state, targetTick: 1000, metadata);
```

### 6. Query Spatial Domain
```csharp
var results = new NativeList<Entity>(Allocator.Temp);
queryService.QueryAABB(bounds, ref results);
```

### 7. Hot-Reload Tuning Profile
```csharp
TuningProfileLoader.HotReloadProfile(entityManager, entity, "Configs/Tuning/PhysicsProfile.json");
```

### 8. Set World Time Scale
```csharp
coordinator.ValueRW.TimeScale = 10.0f; // 10x speed
coordinator.ValueRW.IsFrozen = false;
```

### 9. Request Async Asset
```csharp
var request = new StreamingAssetRequest(path, assetType: 0, priority: 200, requester);
ulong handleId = bus.RequestAsset(request);
```

### 10. Calculate Orbital State
```csharp
KeplerianOrbit.CalculateOrbitalState(semiMajorAxis, eccentricity, ..., out position, out velocity);
```

---

## Component Requirements

### Systems Requiring Singletons
- `ChronoProfilingSystem`: `TelemetryStream`, `TimeState`
- `AIDecisionRecorderSystem`: `DecisionEventRegistry`, `TimeState`, `RewindState`
- `ReplayWriterSystem`: `TimeState`, `RewindState`
- `WorldBusSystem`: `WorldBusState`, `TimeState`
- `DeterministicPhysicsSystem`: `TimeState`

### Components to Add
- `SystemBudget` - For job scheduler
- `TimeCoordinator` - For per-world time control
- `DecisionEventRegistry` - For AI decision recording
- `ReplayMetadata` - For replay framework
- `WorldBusState` - For cross-world communication
- `TuningProfileRef` - For data-driven balancing

---

## File Locations

| Feature | Runtime Files | Editor Files |
|---------|--------------|--------------|
| Job Scheduler | `Runtime/Scheduling/*` | - |
| Chrono-Profiling | `Runtime/Time/*`, `Runtime/Systems/ChronoProfilingSystem.cs` | - |
| AI Decision Recording | `Runtime/AI/DecisionEvent*.cs`, `Runtime/Systems/AIDecisionRecorderSystem.cs` | - |
| Asset Streaming | `Runtime/Streaming/*`, `Runtime/Systems/AssetStreamConsumerSystem.cs` | - |
| Math Kernel | `Runtime/Math/*` | - |
| Replay Framework | `Runtime/Replay/*`, `Runtime/Systems/Replay*.cs` | - |
| Spatial Queries | `Runtime/Spatial/ISpatialQuery.cs`, `Runtime/Spatial/SpatialQueryService.cs` | - |
| Tuning Profiles | `Runtime/Config/TuningProfile*.cs` | - |
| Deterministic Physics | `Runtime/Physics/*`, `Runtime/Systems/DeterministicPhysicsSystem.cs` | - |
| Mission Generator | `Runtime/Scenario/*`, `Runtime/Systems/ScenarioGeneratorSystem.cs` | - |
| World Bus | `Runtime/WorldBus/*`, `Runtime/Systems/WorldBusSystem.cs` | - |
| AI Debug Visualizer | `Runtime/Debug/AIDebug*.cs`, `Runtime/Systems/AIDebugVisualizerSystem.cs` | - |
| CI Benchmarks | `Runtime/Devtools/*` | `CI/run_perf_benchmark.ps1` |
| Editor Integration | - | `Editor/ScenarioRunnerEditor.cs`, `Editor/InWorldEntityPlacer.cs` |
| Scalability | `Runtime/Serialization/*` | `Docs/ScalabilityPath.md` |

---

## Namespace Imports

```csharp
using PureDOTS.Runtime.Scheduling;      // Job scheduler
using PureDOTS.Runtime.Time;            // Time coordinator
using PureDOTS.Runtime.AI;              // AI decision recording
using PureDOTS.Runtime.Streaming;       // Asset streaming
using PureDOTS.Runtime.Math;            // Math kernel
using PureDOTS.Runtime.Replay;          // Replay framework
using PureDOTS.Runtime.Spatial;         // Spatial queries
using PureDOTS.Runtime.Config;          // Tuning profiles
using PureDOTS.Runtime.Physics;        // Deterministic physics
using PureDOTS.Runtime.Scenario;        // Mission generator
using PureDOTS.Runtime.WorldBus;        // Cross-world communication
using PureDOTS.Runtime.Debug;           // AI debug visualizer
using PureDOTS.Runtime.Devtools;        // CI benchmarks
using PureDOTS.Runtime.Serialization;   // Binary serialization
```

---

## See Also

- [AdvancedFeaturesGuide.md](AdvancedFeaturesGuide.md) - Complete integration guide
- [AdvancedFeaturesAPI.md](AdvancedFeaturesAPI.md) - Full API reference
- [ScalabilityPath.md](../ScalabilityPath.md) - Scalability architecture

