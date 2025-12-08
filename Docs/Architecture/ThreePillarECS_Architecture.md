# Three Pillar ECS Architecture

**Last Updated**: 2025-12-07  
**Purpose**: Canonical overview of the Body/Mind/Aggregate split, communication rules, and performance constraints for the tri-project DOTS stack.

---

## Layer Overview

| Layer | Tick Rate | Responsibilities | Primary Data | System Groups |
|-------|-----------|------------------|--------------|---------------|
| **Body ECS** | 60 Hz | Physics, movement, combat, reflexes, immediate perception | `LocalTransform`, `AgentBody`, hot-path state | `ReflexSystemGroup`, `HotPathSystemGroup`, `CombatSystemGroup` |
| **Mind ECS** | 1 Hz | Cognition, learning, procedural reasoning, emotion, decision-making | `PersonalityProfile`, `GoalProfile`, `BehaviorProfile`, `CognitiveMemory` | `CognitiveSystemGroup`, `LearningSystemGroup`, `MotivationSystemGroup` |
| **Aggregate ECS** | 0.2 Hz | Groups, cultures, fleets, civilizations, doctrines, large-scale AI | `AggregateEntity`, `AggregateIntentMessage`, `ConsensusVoteMessage` | `TacticalSystemGroup`, `GroupDecisionSystemGroup`, `EconomySystemGroup` |

**Core rule**: Each layer is a self-contained world. Cross-layer communication only happens through **AgentSyncBus** messages and GUID-based identity; never through shared references.

---

## Communication Model

- **Bus**: `AgentSyncBus` (managed), owned by `AgentSyncBridgeCoordinator`.
- **Intervals**: Body→Mind sync ~100 ms; Mind→Body sync ~250 ms (configurable).
- **Identity**: `AgentGuid` is the only cross-layer identifier; `AgentSyncId` maps Body entities to Mind/Aggregate indices.
- **Message types**: `MindToBodyMessage`, `BodyToMindMessage`, `Percept`, `LimbCommand`, `AggregateIntentMessage`, `ConsensusVoteMessage`, `ConsensusOutcomeMessage`.
- **Directionality**: Data flows by queue; do not perform direct world queries across layers.
- **Ordering**: Bridge systems run in `SimulationSystemGroup` in this order: `AgentMappingSystem` → `BodyToMindSyncSystem` → `MindToBodySyncSystem` → `IntentResolutionSystem`.

---

## Data Ownership & Separation

- **Simulation data** lives in Body ECS (deterministic, Burst-compiled, tick-time).
- **Cognition data** lives in Mind ECS (managed, throttled tick).
- **Aggregate data** lives in Aggregate ECS (managed, coarse tick).
- **Presentation** lives in game projects and uses frame-time (`Time.deltaTime`), not tick-time.
- **No shared refs**: Never cache `Entity` handles or component refs across layers. Use GUIDs and messages only.

---

## Performance Targets

- **Sync overhead**: < 3 ms/frame for all bus processing.
- **Body ECS**: Maintain < 16 ms/frame at 60 Hz.
- **Mind ECS**: Keep cognitive updates under 900 ms/update (1 Hz tolerance).
- **Aggregate ECS**: Keep aggregate updates under 4500 ms/update (0.2 Hz tolerance).
- **Message volume**: Favor delta compression (bus default) and batching per interval.

---

## Anti-Patterns (Do Not Do)

- Direct queries from one world into another (Body querying Mind/Aggregate or vice versa).
- Accessing `AgentSyncBus` inside Burst jobs; collect data in jobs, enqueue in managed systems.
- Passing managed references or UnityEngine objects through bus messages.
- Using `Entity` identifiers across layers; always use `AgentGuid`.
- Driving cameras/HUD off deterministic tick time instead of frame time.

---

## Implementation Checklist

- [ ] Create agents in Body ECS with `AgentBody` + `AgentSyncId` using `AgentGuid`.
- [ ] Create counterparts in Mind/Aggregate worlds and record their indices in `AgentSyncId`.
- [ ] Enqueue sync messages via `AgentSyncBus` in managed systems; batch by interval.
- [ ] Guard Burst systems with rewind checks where they mutate state.
- [ ] Verify deterministic hot paths: no managed allocations, no `FixedString` construction in Burst, explicit enum/byte casts.
- [ ] Profile bus cost and cognitive tick cost when adding new message types.

---

## World Bootstrap Implementation

### Bootstrap Stub (PureDOTS)

Here's a tight stub for implementing the multi-world bootstrap in com.moni.puredots:

```csharp
using Unity.Entities;

namespace PureDots.Bootstrap
{
    public sealed class TriMultiWorldBootstrap : ICustomBootstrap
    {
        public bool Initialize(string defaultWorldName)
        {
            // Optional: skip creating the default world if you want full control.
            // var defaultWorld = new World(defaultWorldName);
            // World.DefaultGameObjectInjectionWorld = defaultWorld;

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
            // TODO: add BodySystemGroup + systems
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(
                world,
                DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default)
            );
            return world;
        }

        static World CreateMindWorld(string name)
        {
            var world = new World(name);
            // TODO: add MindSystemGroup + systems
            return world;
        }

        static World CreateAggregateWorld(string name)
        {
            var world = new World(name);
            // TODO: add AggregateSystemGroup + systems
            return world;
        }
    }
}
```

### System Groups Definition

Define your groups:

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
- Wire specific systems into the right group (Body/Mind/Aggregate).
- Keep everything inside PureDOTS, referenced by Godgame/Space4X.
- Stop wasting cycles "searching for a Mind ECS package".

## References

- `Docs/Architecture/AgentSyncBus_Specification.md` – Bus API, invariants, and extension rules.
- `Docs/Guides/MultiECS_Integration_Guide.md` – Integration cookbook for cross-layer features.
- `Docs/FoundationGuidelines.md` (P25) – Three Pillar ECS communication rules.
