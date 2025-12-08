# World Bootstrap Implementation - Multi-World Setup

**Status:** Design Document  
**Category:** Core Architecture  
**Scope:** PureDOTS Foundation Layer  
**Created:** 2025-12-07  
**Last Updated:** 2025-12-07

---

## Purpose

The world bootstrap creates and configures the three ECS worlds (Body/Mind/Aggregate) and wires system groups. It implements `ICustomBootstrap` to replace Unity's default world creation with our multi-world architecture.

**Key Principle:** All three worlds use Unity.Entities 1.4. They are created in bootstrap, not installed from packages. System groups define execution order within each world.

---

## Core Concept: Custom Bootstrap

**Bootstrap Interface:** `ICustomBootstrap` replaces default world creation  
**World Creation:** Three separate `World` instances (BodyWorld, MindWorld, AggregateWorld)  
**System Groups:** Each world has its own system groups (`BodySystemGroup`, `MindSystemGroup`, `AggregateSystemGroup`)  
**Player Loop:** All worlds are added to Unity's player loop for execution

---

## Implementation Structure

### Bootstrap Class

```csharp
using Unity.Entities;

namespace PureDots.Bootstrap
{
    public sealed class TriMultiWorldBootstrap : ICustomBootstrap
    {
        public bool Initialize(string defaultWorldName)
        {
            // Optional: skip creating the default world if you want full control
            // var defaultWorld = new World(defaultWorldName);
            // World.DefaultGameObjectInjectionWorld = defaultWorld;

            var bodyWorld      = CreateBodyWorld("BodyWorld");
            var mindWorld      = CreateMindWorld("MindWorld");
            var aggregateWorld = CreateAggregateWorld("AggregateWorld");

            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(bodyWorld);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(mindWorld);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(aggregateWorld);

            return true;  // Return true to skip default world creation
        }

        static World CreateBodyWorld(string name)
        {
            var world = new World(name);
            World.DefaultGameObjectInjectionWorld = world;  // Set as default for authoring
            
            // Add BodySystemGroup + systems
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(
                world,
                DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default)
            );
            
            // Create AgentSyncBridgeCoordinator (managed)
            world.GetOrCreateSystemManaged<AgentSyncBridgeCoordinator>();
            
            return world;
        }

        static World CreateMindWorld(string name)
        {
            var world = new World(name);
            
            // Add MindSystemGroup + systems
            // Systems must be manually added or filtered
            var mindGroup = world.GetOrCreateSystemManaged<MindSystemGroup>();
            // ... add cognitive systems to group
            
            return world;
        }

        static World CreateAggregateWorld(string name)
        {
            var world = new World(name);
            
            // Add AggregateSystemGroup + systems
            var aggregateGroup = world.GetOrCreateSystemManaged<AggregateSystemGroup>();
            // ... add aggregate systems to group
            
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
    
    [UpdateInGroup(typeof(BodySystemGroup))]
    [UpdateBefore(typeof(IntentResolutionSystem))]
    public partial class ReflexSystemGroup : ComponentSystemGroup { }
    
    [UpdateInGroup(typeof(BodySystemGroup))]
    [UpdateAfter(typeof(ReflexSystemGroup))]
    public partial class HotPathSystemGroup : ComponentSystemGroup { }
    
    [UpdateInGroup(typeof(BodySystemGroup))]
    [UpdateAfter(typeof(HotPathSystemGroup))]
    public partial class CombatSystemGroup : ComponentSystemGroup { }
}

namespace PureDots.ECS.Mind
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class MindSystemGroup : ComponentSystemGroup { }
    
    [UpdateInGroup(typeof(MindSystemGroup))]
    public partial class CognitiveSystemGroup : ComponentSystemGroup { }
    
    [UpdateInGroup(typeof(MindSystemGroup))]
    public partial class LearningSystemGroup : ComponentSystemGroup { }
    
    [UpdateInGroup(typeof(MindSystemGroup))]
    public partial class MotivationSystemGroup : ComponentSystemGroup { }
}

namespace PureDots.ECS.Aggregate
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class AggregateSystemGroup : ComponentSystemGroup { }
    
    [UpdateInGroup(typeof(AggregateSystemGroup))]
    public partial class TacticalSystemGroup : ComponentSystemGroup { }
    
    [UpdateInGroup(typeof(AggregateSystemGroup))]
    public partial class GroupDecisionSystemGroup : ComponentSystemGroup { }
    
    [UpdateInGroup(typeof(AggregateSystemGroup))]
    public partial class EconomySystemGroup : ComponentSystemGroup { }
}
```

---

## System Registration

### Automatic Registration (Body ECS)

Body ECS uses Unity's default system discovery:

```csharp
DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(
    world,
    DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default)
);
```

This automatically discovers and registers all systems with `[UpdateInGroup(typeof(BodySystemGroup))]` attributes.

### Manual Registration (Mind/Aggregate ECS)

Mind and Aggregate worlds require manual system registration:

```csharp
static World CreateMindWorld(string name)
{
    var world = new World(name);
    var mindGroup = world.GetOrCreateSystemManaged<MindSystemGroup>();
    
    // Manually add systems
    var cognitiveSystem = world.CreateSystemManaged<CognitiveSystem>();
    var goalSystem = world.CreateSystemManaged<GoalEvaluationSystem>();
    var syncSystem = world.CreateSystemManaged<MindToBodySyncSystem>();
    
    mindGroup.AddSystemToUpdateList(cognitiveSystem);
    mindGroup.AddSystemToUpdateList(goalSystem);
    mindGroup.AddSystemToUpdateList(syncSystem);
    
    return world;
}
```

### System Filtering

Use `WorldSystemFilterFlags` to control which systems are discovered:

```csharp
// Only discover systems in specific assemblies
var systems = DefaultWorldInitialization.GetAllSystems(
    WorldSystemFilterFlags.Default,
    new[] { "PureDots.ECS.Body" }  // Only Body ECS systems
);
```

---

## Bridge System Setup

### AgentSyncBridgeCoordinator

The coordinator must be created in Body world (default world):

```csharp
static World CreateBodyWorld(string name)
{
    var world = new World(name);
    World.DefaultGameObjectInjectionWorld = world;
    
    // Create bridge coordinator (managed)
    var coordinator = world.GetOrCreateSystemManaged<AgentSyncBridgeCoordinator>();
    
    // Coordinator creates and owns the bus
    // Other worlds access bus via coordinator
    
    return world;
}
```

### Bridge System Ordering

Bridge systems must run in correct order within `SimulationSystemGroup`:

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(BodyToMindSyncSystem))]
public partial struct AgentMappingSystem : ISystem
{
    // Maps Body entities to Mind/Aggregate indices
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AgentMappingSystem))]
[UpdateBefore(typeof(MindToBodySyncSystem))]
public partial struct BodyToMindSyncSystem : ISystem
{
    // Gathers state, enqueues to bus
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BodyToMindSyncSystem))]
[UpdateBefore(typeof(IntentResolutionSystem))]
public partial struct MindToBodySyncSystem : ISystem
{
    // Pulls intents, enqueues to bus
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MindToBodySyncSystem))]
public partial struct IntentResolutionSystem : ISystem
{
    // Applies resolved intents to Body ECS
}
```

---

## Tick Rate Configuration

### Body ECS (60 Hz)

Body ECS uses fixed-step deterministic tick:

```csharp
// In bootstrap or system setup
var timeState = SystemAPI.GetSingletonRW<TimeState>();
timeState.ValueRW.TickRate = 60;  // 60 Hz
timeState.ValueRW.FixedDeltaTime = 1f / 60f;
```

### Mind ECS (1 Hz)

Mind ECS uses throttled tick (configurable per entity):

```csharp
// In cognitive system
public void OnUpdate(ref SystemState state)
{
    var time = SystemAPI.Time.DeltaTime;  // Frame time (non-deterministic OK)
    
    // Throttle to 1 Hz
    if (time < 1f) return;
    
    // Process cognitive updates
}
```

### Aggregate ECS (0.2 Hz)

Aggregate ECS uses coarse tick (5-second intervals):

```csharp
// In aggregate system
public void OnUpdate(ref SystemState state)
{
    var time = SystemAPI.Time.DeltaTime;
    
    // Throttle to 0.2 Hz (5 seconds)
    if (time < 5f) return;
    
    // Process aggregate updates
}
```

---

## Integration Requirements

### For PureDOTS Developers

1. **Implement `ICustomBootstrap`** - Create `TriMultiWorldBootstrap` class
2. **Create three worlds** - BodyWorld, MindWorld, AggregateWorld
3. **Define system groups** - Create `BodySystemGroup`, `MindSystemGroup`, `AggregateSystemGroup`
4. **Wire bridge systems** - Set up `AgentSyncBridgeCoordinator` and bridge systems
5. **Configure tick rates** - Set appropriate tick rates for each world

### For Game-Specific Developers

1. **Systems auto-register** - Systems with `[UpdateInGroup]` attributes auto-register in Body world
2. **Manual registration** - Mind/Aggregate systems may need manual registration
3. **Respect tick rates** - Don't assume all worlds run at 60 Hz
4. **Use default world** - Authoring uses `World.DefaultGameObjectInjectionWorld` (Body world)

---

## Testing & Validation

### Bootstrap Test

```csharp
[Test]
public void BootstrapCreatesThreeWorlds()
{
    var bootstrap = new TriMultiWorldBootstrap();
    bootstrap.Initialize("TestWorld");
    
    var bodyWorld = World.All[0];
    var mindWorld = World.All[1];
    var aggregateWorld = World.All[2];
    
    Assert.AreEqual("BodyWorld", bodyWorld.Name);
    Assert.AreEqual("MindWorld", mindWorld.Name);
    Assert.AreEqual("AggregateWorld", aggregateWorld.Name);
}
```

### System Group Test

```csharp
[Test]
public void BodySystemGroupExists()
{
    var bodyWorld = World.DefaultGameObjectInjectionWorld;
    var bodyGroup = bodyWorld.GetExistingSystemManaged<BodySystemGroup>();
    
    Assert.IsNotNull(bodyGroup);
}
```

---

## Troubleshooting

### Default World Not Set

**Problem:** Authoring fails because `World.DefaultGameObjectInjectionWorld` is null

**Solution:** Set default world in bootstrap:
```csharp
World.DefaultGameObjectInjectionWorld = bodyWorld;
```

### Systems Not Running

**Problem:** Systems don't execute in Mind/Aggregate worlds

**Solution:** Manually register systems or use system filtering:
```csharp
var systems = DefaultWorldInitialization.GetAllSystems(
    WorldSystemFilterFlags.Default,
    new[] { "PureDots.ECS.Mind" }
);
```

### Bridge Coordinator Missing

**Problem:** `AgentSyncBus` is null when accessed

**Solution:** Create coordinator in Body world bootstrap:
```csharp
world.GetOrCreateSystemManaged<AgentSyncBridgeCoordinator>();
```

---

## Related Documentation

- **Three Pillar ECS Architecture:** `Docs/Architecture/ThreePillarECS_Architecture.md` - World responsibilities
- **AgentSyncBus Specification:** `Docs/Architecture/AgentSyncBus_Specification.md` - Bus setup
- **Multi-ECS Integration Guide:** `Docs/Guides/MultiECS_Integration_Guide.md` - Integration patterns
- **Foundation Guidelines:** `Docs/FoundationGuidelines.md` - Coding patterns

---

**For Implementers:** Bootstrap creates three worlds using Unity.Entities 1.4. System groups define execution order. Bridge coordinator must be created in Body world. All worlds are added to player loop.

**For Designers:** Think of bootstrap as the initialization phase that sets up the multi-world architecture. Each world has its own system groups and tick rates.

---

**Last Updated:** 2025-12-07  
**Status:** Design Document - Foundation Layer Architecture

