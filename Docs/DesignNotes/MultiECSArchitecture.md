# Multi-ECS Hybrid Architecture

**Last Updated**: 2025-01-27  
**Purpose**: Document the hybrid multi-ECS design for cognitive AI reasoning while preserving deterministic Burst-compiled simulation.

---

## Overview

PureDOTS uses a hybrid multi-ECS architecture to support millions of complex AI-driven entities:

- **Cognitive ECS (DefaultEcs)**: High-level reasoning, goals, morality, deception
- **Simulation ECS (Unity Entities 1.4)**: Deterministic physics, movement, combat
- **Presentation (GameObjects)**: Camera, UI, rendering

This separation allows:
- Millions of entities (most inert, active agents sync efficiently)
- Full Burst compliance in simulation layer
- Complex cognitive reasoning without polluting Burst safety
- Deterministic simulation backbone preserved

---

## Architecture Layers

### Layer 1: Cognitive ECS (DefaultEcs)

**Location**: `Runtime/AI/MindECS/`  
**Framework**: DefaultEcs  
**Domain**: Goals, morality, deception, memory, personality profiles  
**Tick Rate**: 1-5 Hz (configurable per entity)  
**Threading**: Main thread / async Tasks  
**Components**: Class-based (managed)

**Components**:
- `PersonalityProfile`: Traits, morality values, risk tolerance
- `BehaviorProfile`: Behavior tree references, action preferences
- `GoalProfile`: Active goals, priorities, goal state machine
- `CognitiveMemory`: Episodic memory, semantic knowledge, relationships

**Systems**:
- `CognitiveSystem`: Evaluates goals, generates intents
- `GoalEvaluationSystem`: Processes goal priorities
- `DeceptionSystem`: Handles deception mechanics

### Layer 2: Simulation ECS (Unity Entities 1.4)

**Location**: Existing `Runtime/` systems  
**Framework**: Unity Entities 1.4 + Burst  
**Domain**: Physics, movement, combat, resources, spatial logic  
**Tick Rate**: FixedStep (deterministic, 60 Hz)  
**Threading**: Burst jobs  
**Components**: Struct-based (unmanaged)

**Components**:
- `AgentBody`: Physical agent state (Burst-safe)
- `IntentCommand`: Intent from Mind ECS (Burst-safe)
- `AgentSyncId`: Links to Mind ECS entity

**Systems**:
- `IntentResolutionSystem`: Reads `IntentCommand`, applies Burst-safe changes
- `AgentMappingSystem`: Maintains GUID ↔ Entity mappings
- `MindToBodySyncSystem`: Reads Mind ECS intents → writes `IntentCommand`
- `BodyToMindSyncSystem`: Reads Body ECS state → writes messages to Mind ECS

### Layer 3: Presentation (GameObjects/MonoBehaviours)

**Location**: Game projects (Godgame, Space4X)  
**Domain**: Camera, UI, rendering  
**Tick Rate**: FrameTime  
**Threading**: Unity main thread

---

## Synchronization Contracts

### GUID-Based Identity

All agents have a shared `AgentGuid` (128-bit, Burst-safe) used across both ECSes:

```csharp
public struct AgentGuid : IEquatable<AgentGuid>
{
    public ulong High;
    public ulong Low;
    // Burst-safe GUID wrapper
}
```

### Message Bus

`AgentSyncBus` (`Runtime/Bridges/AgentSyncBus.cs`) handles batched cross-ECS communication:

**MindToBodyMessage**:
- Intent commands, goal changes
- Batched per sync interval (250ms default)
- Delta compression: only changed intents

**BodyToMindMessage**:
- Position updates, health changes, events
- Batched per sync interval (100ms default)
- Delta compression: only changed fields

### Sync Intervals

Configurable in `CognitiveTickProfile`:
- Body → Mind: 100ms (default)
- Mind → Body: 250ms (default)
- Per-entity tick rate override: 1-5 Hz for cognitive layer

### Performance Targets

- Simulation: 1M entities @ 60 Hz deterministic tick
- AI Layer: 50k active "thinking" entities per second
- Cross-ECS sync: < 3ms/frame CPU cost
- Memory: Efficient GUID storage, batched message queues

---

## Implementation Details

### Assembly Structure

**PureDOTS.Shared**:
- GUID-based IDs, sync message structs, broker interfaces
- No game-specific dependencies

**PureDOTS.AI**:
- Mind ECS world, cognitive systems, personality/behavior components
- References: DefaultEcs, PureDOTS.Shared, PureDOTS.Runtime (read-only)

**PureDOTS.Systems**:
- Body ECS systems, intent resolution, deterministic simulation
- References: Unity.Entities, Burst, PureDOTS.Shared, PureDOTS.Runtime

### Bridge Systems

**AgentMappingSystem**:
- Maintains GUID ↔ Entity mappings for both ECSes
- Creates mappings on agent spawn
- Cleans up mappings on agent despawn

**MindToBodySyncSystem**:
- Reads Mind ECS intents → writes `IntentCommand` to Body ECS
- Batches updates per sync interval
- Delta compression: only changed intents

**BodyToMindSyncSystem**:
- Reads Body ECS state → writes messages to Mind ECS
- Batches updates per sync interval
- Delta compression: only changed fields

### Intent Resolution

`IntentResolutionSystem` (Burst-compiled):
- Reads `IntentCommand` → applies Burst-safe changes
- Updates movement, combat, resource gathering based on intents
- Maintains determinism

---

## Compliance Requirements

### Burst Safety (P8, P13)

- All Body ECS systems remain `[BurstCompile]`
- No managed allocations in Body ECS hot paths
- Mind ECS runs on main thread (managed allowed)
- Never mix: Managed types from Mind ECS must not leak into Body ECS Burst code paths

### C# 9 Syntax (P3)

- No `ref readonly` (C# 12 feature)
- Use `ref` for returns, `in` for read-only parameters
- No record types with primary constructors

### Determinism (P23, P24)

- Body ECS uses `TickTimeState` (deterministic)
- Mind ECS uses `UnityEngine.Time.deltaTime` (non-deterministic, acceptable for cognitive layer)
- Sync messages timestamped with tick number
- Intent resolution maintains determinism in Body ECS

### Assembly References (P18)

- Verify all `.asmdef` files have correct references
- `PureDOTS.AI` references DefaultEcs package
- `PureDOTS.Shared` has no game-specific dependencies

---

## Performance Tuning

### Sync Cost Optimization

1. **Batch Updates**: Process multiple messages per sync interval
2. **Delta Compression**: Only sync changed fields
3. **Throttle Cognitive Updates**: Per-entity tick rate (1-5 Hz)
4. **Profile Early**: Use `MultiECSPerformanceProfiler` to track sync costs

### Memory Optimization

1. **Efficient GUID Storage**: Two ulong values (128-bit)
2. **Batched Message Queues**: Reduce allocation overhead
3. **Companion Entities**: Move large cognitive data to companion entities if needed

### Scaling Guidelines

- **1M entities**: Most inert, only active agents sync
- **50k active cognitive agents**: Throttled to 1-5 Hz per entity
- **Sync budget**: < 3ms/frame target
- **Memory budget**: Efficient GUID storage, batched queues

---

## Testing & Validation

### Scenario: Scenario_MillionAgents.json

- 1M total entities
- 50k active cognitive agents
- Performance validation targets:
  - < 3ms sync cost per frame
  - 60 FPS deterministic simulation
  - Burst compliance maintained

### Performance Profiler

`MultiECSPerformanceProfiler` tracks:
- Sync costs (total, Mind→Body, Body→Mind)
- Message counts
- Validation against < 3ms target

### CLI Test Command

```bash
Unity -batchmode -projectPath PureDOTS -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScenarioFromArgs --scenario Scenario_MillionAgents.json
```

---

## Troubleshooting

### Sync Cost Too High

1. Check message queue sizes (reduce batch sizes if needed)
2. Verify delta compression is working (only changed fields)
3. Throttle cognitive tick rates (reduce from 5 Hz to 2 Hz)
4. Profile with `MultiECSPerformanceProfiler`

### Burst Compilation Errors

1. Verify no managed types in Body ECS systems
2. Check assembly references (PureDOTS.AI should not be referenced by Burst systems)
3. Ensure GUID-based identity (no string/object references)

### Determinism Issues

1. Verify Body ECS uses `TickTimeState` (not `UnityEngine.Time.deltaTime`)
2. Check sync message timestamps (use tick number, not frame time)
3. Ensure intent resolution maintains determinism

---

## Integration Guide

For detailed implementation examples and API reference, see:
- **`Docs/Guides/MultiECS_Integration_Guide.md`** - Complete integration guide with code examples
- **`Docs/Guides/Bridge_System_Usage_Guide.md`** - Bridge system usage patterns
- **`Docs/Guides/AggregateECSIntegrationGuide.md`** - Aggregate ECS integration

## Future Enhancements

- Optional Leopotam.EcsLite integration for lightweight subsystems
- Per-entity sync interval overrides
- Advanced delta compression strategies
- Cognitive layer LOD (reduce tick rate for distant/inactive agents)

