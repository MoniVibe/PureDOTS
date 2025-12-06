# Hybrid ECS Responsibility Map

**Last Updated**: 2025-01-XX  
**Purpose**: Defines ownership boundaries and communication patterns for multi-ECS architecture

---

## Overview

PureDOTS uses a multi-ECS architecture where different ECS frameworks handle different domains to optimize for their strengths. This document defines which ECS layer owns which data and how they communicate without conflicts.

## Architecture Layers

### Layer 1: PureDOTS (Body ECS) - Unity Entities 1.4 + Burst

**Ownership**: Physical simulation state
- Positions, velocities, rotations (`LocalTransform`, `AgentBody`)
- Health, stats, resources (`VillagerNeeds`, `VillagerStats`, `ResourceInventory`)
- Combat state (`CombatStats`, `CombatTarget`)
- Physical properties (`PhysicsVelocity`, collision data)
- Registry entries (Villager, Resource, Storehouse, etc.)

**Update Rate**: Fixed-step 60 Hz (`FixedStepSimulationSystemGroup`)

**Threading**: Multi-core Burst-compiled jobs

**Determinism**: Fully deterministic, rewind-safe

**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/`

### Layer 2: DefaultEcs (Mind ECS) - Cognitive AI Layer

**Ownership**: Cognitive and decision-making state
- Personality traits (`PersonalityProfile`)
- Goals and priorities (`GoalProfile`)
- Memories and knowledge (`CognitiveMemory`)
- Behavior preferences (`BehaviorProfile`)
- Moral alignment, deception state

**Update Rate**: Variable 2-5 Hz (throttled per entity)

**Threading**: Main thread or Task-based async (Burst-incompatible)

**Determinism**: Non-deterministic (personality-driven decisions)

**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/AI/MindECS/`

### Layer 3: Leopotam ECS Lite (Behavior ECS) - Optional

**Ownership**: Transient micro-behaviors
- Animation states
- Local task execution
- Short-lived interactions
- Micro-simulations (gossiping, docking)

**Update Rate**: Per-frame 30-120 Hz (adaptive)

**Threading**: Single-threaded burst-friendly

**Determinism**: Optional (can be deterministic if needed)

**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/Behavior/` (not yet implemented)

### Layer 4: Unity Presentation

**Ownership**: Visual state only (read-only)
- Rendering transforms
- Particle effects
- UI state
- Camera position

**Update Rate**: FrameTime (variable)

**Threading**: Unity main thread only

**Determinism**: Non-deterministic

---

## Component Ownership Matrix

| Component Type | Owner ECS | Access Mode (Others) | Notes |
|----------------|-----------|---------------------|-------|
| `LocalTransform` | PureDOTS | Read-only (Mind, Presentation) | Physical position |
| `AgentBody` | PureDOTS | Read-only (Mind) | Body entity identifier |
| `VillagerNeeds` | PureDOTS | Read-only (Mind) | Health, hunger, energy |
| `AgentIntentBuffer` | PureDOTS | Write (Mind→Body via bus) | Intents from Mind ECS |
| `PersonalityProfile` | DefaultEcs | Read-only (Body) | Cognitive traits |
| `GoalProfile` | DefaultEcs | Read-only (Body) | Active goals |
| `CognitiveMemory` | DefaultEcs | Exclusive | Episodic memory |
| `AgentGuid` | Shared | Read-only (both) | Cross-ECS identifier |
| `AgentSyncId` | PureDOTS | Read-only (Mind) | Mapping component |

---

## Communication Flow

### Mind → Body (Intent Flow)

```
Mind ECS (CognitiveSystem)
  ↓ GenerateIntent()
  ↓ EnqueueMindToBody(MindToBodyMessage)
AgentSyncBus
  ↓ DequeueMindToBodyBatch()
MindToBodySyncSystem (PureDOTS)
  ↓ Resolve GUID → Entity
  ↓ Write to AgentIntentBuffer
Body ECS Systems
  ↓ Consume AgentIntentBuffer
  ↓ Apply intents (movement, combat, etc.)
```

**Message Structure**: `MindToBodyMessage`
- `AgentGuid`: Cross-ECS identifier
- `IntentKind`: Move, Attack, Harvest, etc.
- `TargetPosition`: float3 target
- `TargetEntity`: Entity reference (if applicable)
- `Priority`: byte (0-255)
- `TickNumber`: uint timestamp

**Sync Interval**: 250ms (4 Hz)

### Body → Mind (Telemetry Flow)

```
Body ECS Systems
  ↓ Update physical state
BodyToMindSyncSystem (PureDOTS)
  ↓ Collect telemetry (position, health, stats)
  ↓ EnqueueBodyToMind(BodyToMindMessage)
AgentSyncBus
  ↓ Delta compression
  ↓ DequeueBodyToMindBatch()
Mind ECS (via bridge)
  ↓ Update cognitive state
  ↓ Update memories
```

**Message Structure**: `BodyToMindMessage`
- `AgentGuid`: Cross-ECS identifier
- `Position`: float3 current position
- `Rotation`: quaternion current rotation
- `Health`: float current health
- `MaxHealth`: float maximum health
- `Flags`: byte (changed fields bitmask)
- `TickNumber`: uint timestamp

**Sync Interval**: 100ms (10 Hz)

---

## Authority Rules

### 1. Single Writer Rule

Only one ECS layer mutates a specific property:
- ✅ **Health**: PureDOTS owns `VillagerNeeds.Health`; Mind ECS reads via telemetry
- ✅ **Position**: PureDOTS owns `LocalTransform.Position`; Mind ECS reads via telemetry
- ✅ **Goals**: DefaultEcs owns `GoalProfile`; PureDOTS never writes to it
- ✅ **Personality**: DefaultEcs owns `PersonalityProfile`; PureDOTS never writes to it

### 2. Read-Only Mirrors

Other ECS layers cache snapshots locally:
- Mind ECS maintains cached position/health from last telemetry update
- Body ECS never reads Mind ECS components directly
- Presentation layer observes Body ECS state only

### 3. Message Serialization

Cross-ECS updates use immutable value structs:
- `MindToBodyMessage`: Value type, no references
- `BodyToMindMessage`: Value type, no references
- No shared `Entity` handles between worlds
- Use `AgentGuid` for cross-ECS identification

### 4. Explicit Tick Cadence

Update rates are hierarchical:
- Body ECS: 60 Hz fixed-step
- Mind ECS: 2-5 Hz variable
- Behavior ECS: 30-120 Hz adaptive (if implemented)
- Presentation: FrameTime

### 5. Bridge Throttling

Messages are batched to avoid flooding:
- Mind→Body: Batch every 250ms
- Body→Mind: Batch every 100ms
- Delta compression reduces redundant messages

### 6. No Component Overlap

Never define the same component type name across ECS assemblies:
- `PureDOTS.Runtime`: Body components
- `PureDOTS.AI`: Mind components
- `PureDOTS.Shared`: Shared contracts only

### 7. Determinism Boundary

Only PureDOTS tick loop is rewindable:
- Body ECS: Deterministic, rewind-safe
- Mind ECS: Non-deterministic, skipped during rewind
- Behavior ECS: Optional determinism

---

## Conflict Prevention

### Entity ID Mapping

Entities are mapped via `AgentGuid`:
- Body ECS: `AgentSyncId` component stores GUID and Mind entity index
- Mind ECS: `AgentGuid` component stores GUID
- `AgentMappingSystem` creates mappings on spawn

### Missing Mappings

Handled gracefully:
- If Mind entity doesn't exist: Body continues without AI
- If Body entity doesn't exist: Mind intents are dropped
- Missing GUID: Entity is skipped in sync operations

### Write Violations

`OwnershipValidatorSystem` detects violations:
- Component type name collisions
- Dual ownership warnings
- Write operation tracking (debug builds only)

---

## Examples

### Correct Pattern: Mind Decides, Body Executes

```csharp
// Mind ECS (DefaultEcs)
var message = new MindToBodyMessage {
    AgentGuid = agentGuid,
    Kind = IntentKind.Move,
    TargetPosition = targetPos
};
syncBus.EnqueueMindToBody(message);

// Body ECS (Unity Entities)
// MindToBodySyncSystem dequeues and writes to AgentIntentBuffer
// MovementSystem consumes AgentIntentBuffer and applies movement
```

### Incorrect Pattern: Direct Component Access

```csharp
// ❌ WRONG: Mind ECS writing to Body component
bodyEntity.SetComponent(new LocalTransform { Position = newPos });

// ✅ CORRECT: Mind ECS sends intent via bus
syncBus.EnqueueMindToBody(new MindToBodyMessage { ... });
```

### Correct Pattern: Body Reports, Mind Observes

```csharp
// Body ECS (Unity Entities)
var message = new BodyToMindMessage {
    AgentGuid = syncId.Guid,
    Position = transform.Position,
    Health = needs.Health
};
syncBus.EnqueueBodyToMind(message);

// Mind ECS (DefaultEcs)
// Consumes telemetry and updates cognitive state
// Never writes to Body components directly
```

---

## Performance Considerations

### Scaling Strategy

| Entity Count | Strategy |
|--------------|----------|
| 1-5 million total | PureDOTS owns all physical state |
| ~50k "thinking" agents | Active in Mind ECS per tick |
| Millions idle | Stored as snapshots, not in ECS |

### Workload Culling

- Tiered update scheduling (active → thinking → idle)
- Mind ECS throttles per entity (2-5 Hz)
- Behavior ECS can pause/reinstantiate per scene

### Memory Layout

- `IComponentData`: Fixed structs (Body ECS)
- `IBufferElementData`: Variable traits (Body ECS)
- Managed classes: Cognitive state (Mind ECS)
- Shared blobs: Traits, skills, memories (read-only)

---

## Integration Points

### Bootstrap Integration

`MindECSUpdateSystem` runs in `GameplaySystemGroup`:
- Initializes `MindECSWorld` with `AgentSyncBus`
- Updates DefaultEcs world at 2-5 Hz
- Respects `TimeState.IsPaused` and `RewindState.Mode`

### Bridge Systems

- `AgentSyncBridgeCoordinator`: Manages `AgentSyncBus` singleton
- `MindToBodySyncSystem`: Processes intents from Mind → Body
- `BodyToMindSyncSystem`: Collects telemetry from Body → Mind
- `AgentMappingSystem`: Maintains GUID ↔ Entity mappings

---

## Testing

### Integration Tests

- `BridgeIntegrationTests`: Intent/telemetry flow validation
- `OwnershipValidationTests`: Single-writer rule enforcement
- `BridgePerformanceTests`: Latency and throughput metrics

### Validation

- `OwnershipValidatorSystem`: Runtime violation detection (debug builds)
- Component type collision checks
- Write operation tracking

---

## References

### Implementation Files
- `PureDOTS/Packages/com.moni.puredots/Runtime/Bridges/AgentSyncBus.cs`: Message bus implementation
- `PureDOTS/Packages/com.moni.puredots/Runtime/Bridges/MindToBodySyncSystem.cs`: Intent sync system
- `PureDOTS/Packages/com.moni.puredots/Runtime/Bridges/BodyToMindSyncSystem.cs`: Telemetry sync system
- `PureDOTS/Packages/com.moni.puredots/Runtime/AI/MindECS/MindECSWorld.cs`: DefaultEcs world manager
- `PureDOTS/Packages/com.moni.puredots/Runtime/Shared/AgentIdentity.cs`: Cross-ECS identity contracts

### Documentation
- **Usage Guide**: `Docs/Guides/Bridge_System_Usage_Guide.md` - Practical guide for implementing features with the bridge system
- **AI Integration**: `Docs/Guides/AI_Integration_Guide.md` - Guide for integrating entities with AI pipeline

