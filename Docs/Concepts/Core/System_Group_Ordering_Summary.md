# System Group Ordering Summary

**Status:** Active - Core Architecture
**Category:** Core - System Execution Order
**Audience:** Architects / Implementers
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Executive Summary

**Purpose:** Document the execution order of system groups, focusing on AI, Interrupts, and Perception groups and their dependencies. Provides a clear contract for where systems should run and what data they can depend on.

**Key Groups (Simulation Phase):**
1. **EnvironmentSystemGroup** — Climate, environment grids
2. **SpatialSystemGroup** — Spatial grid rebuilds, navigation
   - **PerceptionSystemGroup** (within SpatialSystemGroup)
3. **GameplaySystemGroup** — Domain simulation
   - **GroupDecisionSystemGroup** — Group decisions
   - **InterruptSystemGroup** — Interrupt handling
   - **AISystemGroup** — AI pipeline (sensing → scoring → steering → commands)
   - **VillagerSystemGroup** — Villager behavior (after AI)
   - Other domain groups (Resource, Vegetation, Miracle, Construction, etc.)

**Critical Ordering Principle:** Perception → Interrupts → AI → Domain Behaviors

---

## High-Level Simulation Order

```
SimulationSystemGroup (per frame):
├─ BuildPhysicsWorld
├─ EnvironmentSystemGroup (climate, environment grids)
├─ SpatialSystemGroup (grid rebuilds, navigation)
│   └─ PerceptionSystemGroup (within SpatialSystemGroup)
├─ TransportPhaseGroup (logistics/transport)
├─ GameplaySystemGroup (domain simulation)
│   ├─ GroupDecisionSystemGroup
│   ├─ InterruptSystemGroup
│   ├─ AISystemGroup
│   ├─ VillagerSystemGroup (after AI)
│   ├─ ResourceSystemGroup
│   ├─ VegetationSystemGroup
│   ├─ MiracleEffectSystemGroup
│   ├─ HandSystemGroup
│   └─ ConstructionSystemGroup
├─ LateSimulationSystemGroup
│   └─ HistorySystemGroup (rewind snapshots)
└─ ExportPhysicsWorld
```

---

## PerceptionSystemGroup

**Location:** Within `SpatialSystemGroup`  
**Purpose:** Updates perception state (what entities see/hear/sense)  
**Runs:** After spatial grid rebuild, before AI systems

### Internal Ordering

```
PerceptionSystemGroup:
├─ CommunicationEndpointBootstrapSystem (OrderFirst)
├─ PerceptionSignalFieldUpdateSystem (OrderFirst, before PerceptionUpdateSystem)
│   └─ Updates signal field cells (smell/sound/EM emissions)
├─ PerceptionSignalSamplingSystem
│   └─ Samples signal field for entities with SenseCapability
├─ PerceptionUpdateSystem
│   └─ Channel-based detection (Vision, Hearing, Smell, EM, Gravitic, Paranormal)
├─ CommunicationAttemptBuildSystem (after PerceptionUpdateSystem, before CommunicationDispatchSystem)
├─ CommunicationDispatchSystem (after CommunicationEndpointBootstrapSystem)
├─ CommunicationDecodeDecideSystem (after CommunicationDispatchSystem)
├─ CommunicationOutboundMaintenanceSystem (after CommunicationDecodeDecideSystem)
└─ PerceptionToInterruptBridgeSystem (after PerceptionUpdateSystem)
    └─ Bridges perception changes to interrupts (new threat detected, resource spotted, etc.)
```

### Key Systems

- **PerceptionSignalFieldUpdateSystem:** Updates signal field (smell/sound/EM emissions in spatial grid cells)
- **PerceptionUpdateSystem:** Channel-based detection using spatial queries
- **PerceptionToInterruptBridgeSystem:** Converts perception changes to interrupts

### Dependencies

- **Requires:** `SpatialGridConfig`, `SpatialGridState` (spatial queries)
- **Provides:** `PerceivedEntity` buffers, `PerceptionState` components

### Outputs

- `PerceivedEntity` buffers (what entities currently perceive)
- `PerceptionState` components (tracking metadata)
- Interrupts via `PerceptionToInterruptBridgeSystem` (new threat, resource spotted, etc.)

---

## InterruptSystemGroup

**Location:** Within `GameplaySystemGroup`  
**Purpose:** Processes interrupts and writes `EntityIntent` for behavior systems  
**Runs:** After perception/combat/group logic, before AI/GOAP systems  
**Ordering:** `[UpdateBefore(typeof(AISystemGroup))]`

### Internal Ordering

```
InterruptSystemGroup:
└─ InterruptHandlerSystem
    └─ Processes Interrupt buffers, writes EntityIntent components
```

### Key Systems

- **InterruptHandlerSystem:** Picks highest-priority interrupt, converts to `EntityIntent`

### Dependencies

- **Requires:** `Interrupt` buffers (emitted by perception, combat, group, status systems)
- **Provides:** `EntityIntent` components (consumed by AI/behavior systems)

### Input Sources

Interrupts are emitted by:
- **Perception systems:** `NewThreatDetected`, `ResourceSpotted`, `ObjectiveSpotted`, etc.
- **Combat systems:** `UnderAttack`, `TookDamage`, `TargetDestroyed`, etc.
- **Group systems:** `NewOrder`, `OrderCancelled`, `GroupFormed`, etc.
- **Status systems:** `LowHealth`, `LowResources`, `StatusEffectApplied`, etc.

### Outputs

- `EntityIntent` components (intent mode, target entity/position, priority)

---

## AISystemGroup

**Location:** Within `GameplaySystemGroup`  
**Purpose:** Shared AI pipeline (sensing → utility scoring → steering → task resolution)  
**Runs:** After spatial/perception/interrupts, before domain-specific groups (VillagerSystemGroup)  
**Ordering:** `[UpdateBefore(typeof(VillagerSystemGroup))]`

### Internal Ordering

```
AISystemGroup:
├─ AgencyControlResolutionSystem (OrderFirst)
├─ VillagerBelongingModifierSystem (OrderFirst, before VillagerArchetypeResolutionSystem)
├─ AISensorUpdateSystem
│   └─ Samples spatial grid, populates AISensorReading buffers
├─ SpatialSensorUpdateSystem (after AISensorUpdateSystem)
├─ AIImportanceSystem (after AISensorUpdateSystem)
├─ AIVirtualSensorSystem (after AISensorUpdateSystem, before AIUtilityScoringSystem)
│   └─ Populates virtual sensor readings (internal needs: hunger, energy, morale)
├─ AIUtilityScoringSystem (after AISensorUpdateSystem)
│   └─ Evaluates actions using utility curves + sensor readings
├─ AISteeringSystem (after AIUtilityScoringSystem)
│   └─ Calculates movement direction and velocity
└─ AITaskResolutionSystem (OrderLast, after AISteeringSystem)
    └─ Emits AICommand buffer entries
```

### Key Systems

1. **AISensorUpdateSystem:** Spatial queries for nearby entities, category filtering
2. **AIVirtualSensorSystem:** Maps internal needs (hunger, energy, morale) to sensor readings
3. **AIUtilityScoringSystem:** Evaluates actions using utility curves (blob assets)
4. **AISteeringSystem:** Calculates desired movement direction/velocity
5. **AITaskResolutionSystem:** Emits `AICommand` buffer (consumed by bridge systems)

### Dependencies

- **Requires:**
  - `SpatialGridConfig`, `SpatialGridState` (spatial queries)
  - `AISensorReading` buffers (populated by AISensorUpdateSystem)
  - `EntityIntent` components (from InterruptSystemGroup)
- **Provides:**
  - `AICommand` buffer (commands for domain systems)
  - `UtilityDecisionState` components (current action, score, target)
  - `AISteeringState` components (desired velocity)

### Outputs

- `AICommand` buffer entries (agent entity, action index, target entity/position)
- `UtilityDecisionState` components (current action, score, target)
- `AISteeringState` components (desired movement direction/velocity)

---

## GroupDecisionSystemGroup

**Location:** Within `GameplaySystemGroup`  
**Purpose:** Group-level decision systems (squads, crews, villages)  
**Runs:** After group membership, before interrupt handling  
**Ordering:** `[UpdateBefore(typeof(InterruptSystemGroup))]`

### Internal Ordering

```
GroupDecisionSystemGroup:
└─ (Group decision systems)
    └─ Group-level planning, doctrine, coordination
```

### Purpose

- Group-level decisions (squad tactics, crew coordination, village policies)
- Doctrine/SOP application
- Group intent resolution

---

## Complete Data Flow

### Perception → Interrupt → AI → Behavior Pipeline

```
1. PerceptionSystemGroup:
   - PerceptionUpdateSystem detects entities (threats, resources, etc.)
   - PerceptionToInterruptBridgeSystem emits interrupts (NewThreatDetected, ResourceSpotted)

2. InterruptSystemGroup:
   - InterruptHandlerSystem processes interrupts (highest priority first)
   - Writes EntityIntent components (Flee, Attack, Gather, etc.)

3. AISystemGroup:
   - AISensorUpdateSystem samples spatial grid (nearby entities)
   - AIVirtualSensorSystem maps internal needs (hunger, energy) to sensor readings
   - AIUtilityScoringSystem evaluates actions using utility curves
   - Consumes EntityIntent (interrupts influence action selection)
   - AISteeringSystem calculates movement
   - AITaskResolutionSystem emits AICommand buffer

4. Domain Groups (VillagerSystemGroup, etc.):
   - Bridge systems consume AICommand buffer
   - Map AI actions to domain-specific goals/behaviors
   - Execute behaviors (movement, work, combat, etc.)
```

---

## Key Ordering Rules

### Spatial Dependencies

- **Systems that use spatial queries** must run after `SpatialSystemGroup`
- `PerceptionSystemGroup` runs within `SpatialSystemGroup` (has access to fresh spatial data)
- `AISystemGroup` runs after `SpatialSystemGroup` (queries spatial grid for sensor updates)

### Perception → Interrupt → AI Chain

- **Perception systems** emit interrupts (via `PerceptionToInterruptBridgeSystem`)
- **Interrupt systems** process interrupts and write `EntityIntent`
- **AI systems** consume `EntityIntent` and sensor readings to evaluate actions

### Domain Behavior Dependencies

- **Domain groups** (VillagerSystemGroup, ResourceSystemGroup, etc.) run after `AISystemGroup`
- Bridge systems consume `AICommand` buffer and translate to domain-specific goals
- Domain behaviors execute based on AI decisions

---

## Group Placement Guidelines

### When to Use PerceptionSystemGroup

- Systems that update perception state (what entities see/hear/sense)
- Signal field updates (smell/sound/EM emissions)
- Communication systems (rely on spatial/perception data)

### When to Use InterruptSystemGroup

- Systems that process interrupts and write `EntityIntent`
- Must run after perception/combat/group systems (interrupt sources)
- Must run before AI systems (AI consumes intents)

### When to Use AISystemGroup

- Shared AI pipeline systems (sensing, utility scoring, steering, task resolution)
- Must run after spatial/perception/interrupts
- Must run before domain-specific groups (VillagerSystemGroup, etc.)

### When to Use Domain Groups

- Domain-specific behavior systems (VillagerSystemGroup, ResourceSystemGroup, etc.)
- Must run after `AISystemGroup` (consume AICommand buffer)
- Bridge systems translate AI commands to domain-specific goals

---

## System Group Definitions (Reference)

**From `Packages/com.moni.puredots/Runtime/Systems/SystemGroups.cs`:**

```csharp
// PerceptionSystemGroup: Within SpatialSystemGroup
[UpdateInGroup(typeof(SpatialSystemGroup))]
public partial class PerceptionSystemGroup : InstrumentedComponentSystemGroup { }

// InterruptSystemGroup: Within GameplaySystemGroup, before AISystemGroup
[UpdateInGroup(typeof(GameplaySystemGroup))]
[UpdateBefore(typeof(AISystemGroup))]
public partial class InterruptSystemGroup : InstrumentedComponentSystemGroup { }

// AISystemGroup: Within GameplaySystemGroup, before VillagerSystemGroup
[UpdateInGroup(typeof(GameplaySystemGroup))]
[UpdateBefore(typeof(VillagerSystemGroup))]
public partial class AISystemGroup : InstrumentedComponentSystemGroup { }

// GroupDecisionSystemGroup: Within GameplaySystemGroup, before InterruptSystemGroup
[UpdateInGroup(typeof(GameplaySystemGroup))]
[UpdateBefore(typeof(InterruptSystemGroup))]
public partial class GroupDecisionSystemGroup : InstrumentedComponentSystemGroup { }
```

---

## Related Documentation

- **System Group Ordering Advisory:** `Docs/Concepts/Core/System_Group_Ordering_Advisory.md` - **⭐ Recommended tightening changes**
- **System Groups:** `Packages/com.moni.puredots/Runtime/Systems/SystemGroups.cs` - Authoritative group definitions
- **System Execution Order:** `Docs/Documentation/DesignNotes/SystemExecutionOrder.md` - Detailed ordering documentation
- **Runtime Lifecycle:** `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` - Canonical execution order reference
- **AI Integration Guide:** `Docs/Guides/AI_Integration_Guide.md` - How to integrate with AI pipeline
- **Perception/Action/Intent Summary:** `Docs/Concepts/Core/Perception_Action_Intent_Summary.md` - AI pipeline overview

---

**For Architects:** Review ordering when adding new systems to ensure dependencies are met  
**For Implementers:** Use `[UpdateInGroup]` and `[UpdateAfter]` / `[UpdateBefore]` to place systems correctly  
**For Designers:** Understand data flow (Perception → Interrupt → AI → Behavior) when designing new behaviors  
**For Reviewers:** See `System_Group_Ordering_Advisory.md` for recommended tightening changes (physics semantics, comms timing, duplicated sensing, transport semantics, ECB boundaries, wake tags)

