# AgentSyncBus Specification

**Last Updated**: 2025-12-10
**Purpose**: Communication protocol for three-pillar ECS architecture

---

## Overview

The AgentSyncBus provides structured, deterministic communication between PureDOTS' three ECS worlds: Body (simulation), Mind (planning), and Aggregate (analysis). It ensures clean separation of concerns while enabling coordinated behavior across worlds.

## Architecture

### Core Components
**Location**: `Packages/com.moni.puredots/Runtime/Systems/AgentSyncBus/`

**Key Types**:
- `AgentSyncBus` - Central message routing singleton
- `ISyncMessage` - Message interface
- `SyncMessageBuffer<T>` - Typed message buffers
- `MessageDispatcher` - Cross-world routing system

### Message Flow Architecture
```
Body ECS → AgentSyncBus → Mind ECS → AgentSyncBus → Aggregate ECS
    ↑                        ↓                        ↓
Simulation State        Planning Commands        Analysis Results
(Deterministic)         (Speculative)            (Historical)
```

## Message Types

### 1. BodyToMind Messages
**Purpose**: Simulation state updates for planning systems
**Frequency**: Every simulation tick

```csharp
public struct BodyToMindUpdate : ISyncMessage
{
    public Entity Entity;
    public float3 Position;
    public AgentState CurrentState;
    public ResourceLevels Resources;
    public EnvironmentalFactors Environment;
}
```

**Usage**:
```csharp
// In Body ECS system
var update = new BodyToMindUpdate { /* populate */ };
AgentSyncBus.QueueMessage(update);
```

### 2. MindToBody Messages
**Purpose**: Planning results executed in simulation
**Frequency**: Planning completion events

```csharp
public struct MindToBodyCommand : ISyncMessage
{
    public Entity Entity;
    public GoalType SelectedGoal;
    public float3 TargetPosition;
    public ActionSequence Actions;
    public float Confidence;
}
```

**Usage**:
```csharp
// In Mind ECS system
var command = new MindToBodyCommand { /* populate */ };
AgentSyncBus.QueueMessage(command);
```

### 3. AggregateToMind Messages
**Purpose**: Strategic guidance from analysis to planning
**Frequency**: Turn-based intervals

```csharp
public struct AggregateToMindDirective : ISyncMessage
{
    public Entity Entity;
    public StrategicPriority Priority;
    public ResourceTrends Trends;
    public ThreatAssessments Threats;
    public long TermGoals;
}
```

### 4. MindToAggregate Messages
**Purpose**: Planning insights for analysis
**Frequency**: Planning cycles

```csharp
public struct MindToAggregateReport : ISyncMessage
{
    public Entity Entity;
    public DecisionRationale Reasoning;
    public PredictedOutcomes Predictions;
    public ConfidenceMetrics Confidence;
}
```

## Message Processing

### Deterministic Ordering
Messages are processed in FIFO order within each world transition to ensure deterministic replay during rewind.

### Buffer Management
```csharp
public struct AgentSyncBus : IComponentData
{
    public SyncMessageBuffer<BodyToMindUpdate> BodyToMindBuffer;
    public SyncMessageBuffer<MindToBodyCommand> MindToBodyBuffer;
    public SyncMessageBuffer<AggregateToMindDirective> AggregateToMindBuffer;
    public SyncMessageBuffer<MindToAggregateReport> MindToAggregateBuffer;
}
```

### Processing Cadence
- **Body → Mind**: End of simulation tick
- **Mind → Body**: Planning completion (variable timing)
- **Aggregate ↔ Mind**: End of turn/phase

## Implementation Details

### System Integration
```csharp
[UpdateInGroup(typeof(BodySimulationSystemGroup))]
[UpdateAfter(typeof(SimulationSystems))]
public partial struct BodyToMindSyncSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var bus = SystemAPI.GetSingleton<AgentSyncBus>();
        // Queue BodyToMind messages
        // Clear processed buffers
    }
}

[UpdateInGroup(typeof(MindSimulationSystemGroup))]
[UpdateBefore(typeof(PlanningSystems))]
public partial struct MindMessageProcessor : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var bus = SystemAPI.GetSingleton<AgentSyncBus>();
        // Process incoming BodyToMind messages
        // Generate MindToBody commands
    }
}
```

### Thread Safety
- All message queuing is single-threaded within system updates
- Buffer access uses Unity's job system safety checks
- No cross-world direct entity references

### Memory Management
- Fixed-capacity ring buffers prevent allocation
- Automatic cleanup of processed messages
- No managed object references in messages

## Testing and Validation

### Message Integrity Tests
```csharp
[Test]
public void BodyToMind_MessageIntegrity()
{
    // Verify message data survives world transitions
    // Check deterministic replay consistency
}
```

### Cadence Validation
```csharp
[Test]
public void SyncCadence_Respected()
{
    // Verify messages processed at correct frequencies
    // Check no message loss during high-frequency updates
}
```

### Determinism Checks
```csharp
[Test]
public void RewindDeterminism_Maintained()
{
    // Verify message processing is rewind-safe
    // Check identical results across rewind cycles
}
```

## Performance Characteristics

### Latency Budgets
- Body → Mind: < 1ms (per tick)
- Mind → Body: < 5ms (planning completion)
- Aggregate transfers: < 10ms (turn processing)

### Throughput Targets
- 10k+ messages per second (Body ↔ Mind)
- 1k+ complex messages per minute (Aggregate operations)

### Memory Footprint
- < 1MB base buffer allocation
- Scales with entity count and message frequency

## Error Handling

### Message Validation
```csharp
public void ValidateMessage(ISyncMessage message)
{
    // Entity existence checks
    // Data range validation
    // Referential integrity
}
```

### Failure Modes
- **Buffer Overflow**: Drop oldest messages, log warning
- **Invalid Entity**: Skip message, log error
- **Type Mismatch**: Assert in development, skip in production

## Future Extensions

### Priority Messaging
```csharp
public enum MessagePriority
{
    Low,
    Normal,
    High,
    Critical
}
```

### Compressed Serialization
For network multiplayer scenarios or large-scale simulations.

### Analytics Integration
Message flow telemetry for debugging complex interactions.

## Migration Guide

### From Direct Entity References
**Before**:
```csharp
// Direct cross-world access (not allowed)
var mindEntity = mindWorld.EntityManager.GetComponentData<MindState>(bodyEntity);
```

**After**:
```csharp
// Message-based communication
var message = new BodyToMindUpdate { Entity = bodyEntity, /* data */ };
AgentSyncBus.QueueMessage(message);
```

### From Shared Components
**Before**: Shared `IComponentData` across worlds
**After**: Message-passing with data duplication

---

**See Also**:
- `ThreePillarECS_Architecture.md` - Overall architecture overview
- `MultiECS_Integration_Guide.md` - Game project integration patterns
- `FoundationGuidelines.md` - Implementation standards



