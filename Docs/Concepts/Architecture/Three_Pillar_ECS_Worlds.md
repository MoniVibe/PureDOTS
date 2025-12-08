# Three Pillar ECS Worlds - Multi-World Architecture

**Status:** Design Document  
**Category:** Core Architecture  
**Scope:** PureDOTS Foundation Layer  
**Created:** 2025-12-07  
**Last Updated:** 2025-12-07

---

## Purpose

PureDOTS implements a **three-world ECS architecture** (Body/Mind/Aggregate) using Unity.Entities 1.4 as the single ECS runtime. All three worlds are **architectural slices** within our codebase, not separate packages. They run as distinct Worlds + SystemGroups on top of the same `com.unity.entities` runtime.

**Key Principle:** Each world has distinct responsibilities, tick rates, and data ownership. Cross-world communication only happens through `AgentSyncBus` messages and GUID-based identity.

---

## Core Concept: Three Worlds, One ECS Runtime

**Canonical Truth:** We only use one ECS: Unity.Entities 1.4.

All "Mind ECS", "Body ECS", and "Aggregate ECS" are architectural slices we define inside our own codebase, not separate packages.

They are implemented as distinct Worlds + SystemGroups running on top of the same `com.unity.entities` runtime:

- **Body ECS** – canonical deterministic sim world (time/rewind, spatial grid, registries, core sim loops).
- **Mind ECS** – thinking/planning/what-if world (goals, planning, evaluation, AgentSyncBus, etc.).
- **Aggregate ECS** – higher-level aggregates: bands, fleets, empires, dynasties, regional summaries, etc.

All three live inside our local framework `com.moni.puredots` and the game projects that reference it (Godgame, Space4X).

**There is nothing to install from Package Manager called "Mind ECS", "Body ECS", or "Aggregate ECS".**

If you are looking for them, you are looking for namespaces, bootstraps, and system groups in `com.moni.puredots`, not for extra packages.

---

## World Responsibilities

### Body ECS (Simulation World)

**Tick Rate:** 60 Hz (fixed-step, deterministic)  
**Primary Data:** `LocalTransform`, `AgentBody`, hot-path state  
**System Groups:** `ReflexSystemGroup`, `HotPathSystemGroup`, `CombatSystemGroup`  
**Threading:** Burst-compiled jobs

**Responsibilities:**
- Physics, movement, combat, reflexes
- Immediate perception (sensor readings)
- Deterministic simulation backbone
- Spatial partitioning and queries
- Resource gathering and consumption
- Time/rewind state management

**Components:**
```csharp
// Body ECS components (Burst-safe structs)
public struct AgentBody : IComponentData
{
    public float3 Position;
    public float3 Velocity;
    public float Health;
    public AgentGuid Guid;  // Cross-world identity
}

public struct AgentSyncId : IComponentData
{
    public int MindEntityIndex;      // Index in Mind world
    public int AggregateEntityIndex; // Index in Aggregate world
    public AgentGuid Guid;            // Shared identity
}

public struct IntentCommand : IComponentData
{
    public byte IntentType;
    public float3 TargetPosition;
    public uint TargetTick;
}
```

**Systems:**
- `IntentResolutionSystem` - Applies Mind intents to Body state
- `AgentMappingSystem` - Maintains GUID ↔ Entity mappings
- `BodyToMindSyncSystem` - Gathers state/percepts, enqueues to bus
- `CombatSystem` - Handles combat resolution
- `MovementSystem` - Applies movement based on intents

---

### Mind ECS (Cognitive World)

**Tick Rate:** 1 Hz (configurable per entity, 1-5 Hz)  
**Primary Data:** `PersonalityProfile`, `GoalProfile`, `BehaviorProfile`, `CognitiveMemory`  
**System Groups:** `CognitiveSystemGroup`, `LearningSystemGroup`, `MotivationSystemGroup`  
**Threading:** Main thread / async Tasks (managed allowed)

**Responsibilities:**
- Cognition, learning, procedural reasoning
- Emotion and decision-making
- Goal evaluation and prioritization
- Memory formation and retrieval
- Personality-driven behavior
- Deception and social dynamics

**Components:**
```csharp
// Mind ECS components (managed classes allowed)
public struct PersonalityProfile : IComponentData
{
    public float RiskTolerance;
    public float MoralityAxis;
    public float OrderAxis;
    public float PurityAxis;
}

public struct GoalProfile : IComponentData
{
    public byte ActiveGoalId;
    public float Priority;
    public uint GoalState;
}

public struct CognitiveMemory : IComponentData
{
    // Episodic memory, semantic knowledge, relationships
    // Managed buffers allowed in Mind ECS
}
```

**Systems:**
- `CognitiveSystem` - Evaluates goals, generates intents
- `GoalEvaluationSystem` - Processes goal priorities
- `MindToBodySyncSystem` - Pulls cognitive outputs, enqueues intents
- `DeceptionSystem` - Handles deception mechanics
- `LearningSystem` - Updates cognitive memory

---

### Aggregate ECS (Group World)

**Tick Rate:** 0.2 Hz (5-second intervals)  
**Primary Data:** `AggregateEntity`, `AggregateIntentMessage`, `ConsensusVoteMessage`  
**System Groups:** `TacticalSystemGroup`, `GroupDecisionSystemGroup`, `EconomySystemGroup`  
**Threading:** Main thread (managed allowed)

**Responsibilities:**
- Groups, cultures, fleets, civilizations
- Doctrines and large-scale AI
- Consensus voting and group decisions
- Aggregate-level resource management
- Regional summaries and statistics

**Components:**
```csharp
// Aggregate ECS components
public struct AggregateEntity : IComponentData
{
    public AgentGuid AggregateGuid;
    public byte AggregateType;  // Band, Fleet, Empire, etc.
    public int MemberCount;
}

public struct AggregateIntentMessage : IComponentData
{
    public AgentGuid AggregateGuid;
    public byte IntentType;
    public float3 TargetPosition;
}
```

**Systems:**
- `GroupDecisionSystem` - Processes consensus votes
- `AggregateIntentSystem` - Generates group-level intents
- `TacticalSystem` - Handles tactical group decisions
- `EconomySystem` - Manages aggregate resource flows

---

## Communication Model

### AgentSyncBus

**Bus:** `AgentSyncBus` (managed), owned by `AgentSyncBridgeCoordinator`  
**Intervals:** Body→Mind sync ~100 ms; Mind→Body sync ~250 ms (configurable)  
**Identity:** `AgentGuid` is the only cross-layer identifier; `AgentSyncId` maps Body entities to Mind/Aggregate indices

**Message Types:**
- `MindToBodyMessage` - Intents from cognition to simulation
- `BodyToMindMessage` - State updates from simulation
- `Percept` - Sensor readings (Body → Mind)
- `LimbCommand` - Limb activations (Mind → Body)
- `AggregateIntentMessage` - Group-level intents (Aggregate → Mind)
- `ConsensusVoteMessage` - Voting payloads
- `ConsensusOutcomeMessage` - Resolved votes

**Directionality:** Data flows by queue; do not perform direct world queries across layers.

**Ordering:** Bridge systems run in `SimulationSystemGroup` in this order:
1. `AgentMappingSystem` → 
2. `BodyToMindSyncSystem` → 
3. `MindToBodySyncSystem` → 
4. `IntentResolutionSystem`

---

## Data Ownership & Separation

- **Simulation data** lives in Body ECS (deterministic, Burst-compiled, tick-time)
- **Cognition data** lives in Mind ECS (managed, throttled tick)
- **Aggregate data** lives in Aggregate ECS (managed, coarse tick)
- **Presentation** lives in game projects and uses frame-time (`Time.deltaTime`), not tick-time
- **No shared refs:** Never cache `Entity` handles or component refs across layers. Use GUIDs and messages only.

---

## Performance Targets

- **Sync overhead:** < 3 ms/frame for all bus processing
- **Body ECS:** Maintain < 16 ms/frame at 60 Hz
- **Mind ECS:** Keep cognitive updates under 900 ms/update (1 Hz tolerance)
- **Aggregate ECS:** Keep aggregate updates under 4500 ms/update (0.2 Hz tolerance)
- **Message volume:** Favor delta compression (bus default) and batching per interval

---

## Implementation Checklist

- [ ] Create agents in Body ECS with `AgentBody` + `AgentSyncId` using `AgentGuid`
- [ ] Create counterparts in Mind/Aggregate worlds and record their indices in `AgentSyncId`
- [ ] Enqueue sync messages via `AgentSyncBus` in managed systems; batch by interval
- [ ] Guard Burst systems with rewind checks where they mutate state
- [ ] Verify deterministic hot paths: no managed allocations, no `FixedString` construction in Burst, explicit enum/byte casts
- [ ] Profile bus cost and cognitive tick cost when adding new message types

---

## World Bootstrap Implementation

### Bootstrap Stub (PureDOTS)

```csharp
using Unity.Entities;

namespace PureDots.Bootstrap
{
    public sealed class TriMultiWorldBootstrap : ICustomBootstrap
    {
        public bool Initialize(string defaultWorldName)
        {
            var bodyWorld      = CreateBodyWorld("BodyWorld");
            var mindWorld      = CreateMindWorld("MindWorld");
            var aggregateWorld = CreateAggregateWorld("AggregateWorld");

            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(bodyWorld);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(mindWorld);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(aggregateWorld);

            return true;
        }

        static World CreateBodyWorld(string name)
        {
            var world = new World(name);
            // Add BodySystemGroup + systems
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(
                world,
                DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default)
            );
            return world;
        }

        static World CreateMindWorld(string name)
        {
            var world = new World(name);
            // Add MindSystemGroup + systems
            return world;
        }

        static World CreateAggregateWorld(string name)
        {
            var world = new World(name);
            // Add AggregateSystemGroup + systems
            return world;
        }
    }
}
```

### System Groups Definition

```csharp
using Unity.Entities;

namespace PureDots.ECS.Body
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class BodySystemGroup : ComponentSystemGroup { }
}

namespace PureDots.ECS.Mind
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class MindSystemGroup : ComponentSystemGroup { }
}

namespace PureDots.ECS.Aggregate
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class AggregateSystemGroup : ComponentSystemGroup { }
}
```

**Implementation notes:**
- Wire specific systems into the right group (Body/Mind/Aggregate)
- Keep everything inside PureDOTS, referenced by Godgame/Space4X
- Stop wasting cycles "searching for a Mind ECS package"

---

## Anti-Patterns (Do Not Do)

- Direct queries from one world into another (Body querying Mind/Aggregate or vice versa)
- Accessing `AgentSyncBus` inside Burst jobs; collect data in jobs, enqueue in managed systems
- Passing managed references or UnityEngine objects through bus messages
- Using `Entity` identifiers across layers; always use `AgentGuid`
- Driving cameras/HUD off deterministic tick time instead of frame time

---

## Integration Requirements

### For PureDOTS Developers

1. **Create worlds in bootstrap** - Use `TriMultiWorldBootstrap` to create all three worlds
2. **Define system groups** - Create `BodySystemGroup`, `MindSystemGroup`, `AggregateSystemGroup`
3. **Wire bridge systems** - Implement `AgentMappingSystem`, `BodyToMindSyncSystem`, `MindToBodySyncSystem`, `IntentResolutionSystem`
4. **Respect data ownership** - Never share component refs across worlds
5. **Use GUID identity** - All cross-world references use `AgentGuid`

### For Game-Specific Developers

1. **Create agents in Body ECS** - Use `AgentBody` + `AgentSyncId` components
2. **Link to Mind/Aggregate** - Create counterparts in other worlds, record indices
3. **Consume intents** - Read `IntentCommand` in Body ECS systems
4. **Enqueue state** - Write `BodyToMindMessage` from managed bridge systems
5. **Respect tick rates** - Body at 60 Hz, Mind at 1 Hz, Aggregate at 0.2 Hz

---

## Related Documentation

- **AgentSyncBus Specification:** `Docs/Architecture/AgentSyncBus_Specification.md` - Bus API, invariants, and extension rules
- **Multi-ECS Integration Guide:** `Docs/Guides/MultiECS_Integration_Guide.md` - Integration cookbook for cross-layer features
- **Foundation Guidelines:** `Docs/FoundationGuidelines.md` (P25) - Three Pillar ECS communication rules
- **TRI Project Briefing:** `TRI_PROJECT_BRIEFING.md` - Tri-project architecture overview

---

**For Implementers:** All three worlds use Unity.Entities 1.4. They are architectural slices, not separate packages. Create them in bootstrap, wire system groups, and use `AgentSyncBus` for cross-world communication.

**For Designers:** Think of Body/Mind/Aggregate as three separate simulation layers with different tick rates and responsibilities. They communicate through message queues, never through direct references.

---

**Last Updated:** 2025-12-07  
**Status:** Design Document - Foundation Layer Architecture

