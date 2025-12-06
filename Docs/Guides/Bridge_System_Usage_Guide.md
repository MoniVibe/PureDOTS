# Bridge System Usage Guide

**Last Updated**: 2025-01-XX  
**Purpose**: Practical guide for agents implementing features that use the Mind-Body ECS bridge system

---

## Quick Start

The bridge system enables communication between PureDOTS (Body ECS) and DefaultEcs (Mind ECS) without ownership conflicts. Use this guide when implementing AI features, agent behaviors, or extending cognitive systems.

## Architecture Overview

```
┌─────────────────┐         ┌──────────────┐         ┌─────────────────┐
│   Mind ECS      │         │ AgentSyncBus │         │   Body ECS      │
│  (DefaultEcs)   │────────▶│  (Message    │────────▶│ (Unity Entities)│
│                 │ Intents │   Broker)    │ Telemetry│                 │
└─────────────────┘         └──────────────┘         └─────────────────┘
```

- **Mind ECS**: Decides what agents should do (goals, personality, memories)
- **Body ECS**: Executes actions (movement, combat, resource gathering)
- **AgentSyncBus**: Serialized message queue preventing direct component access

---

## Common Tasks

### 1. Creating an Agent with AI

**Step 1: Create Body Entity (PureDOTS)**

```csharp
// In your authoring/baker or spawn system
var bodyEntity = EntityManager.CreateEntity();

// Add physical components
var agentGuid = AgentGuid.NewGuid();
EntityManager.AddComponentData(bodyEntity, new AgentBody
{
    Id = agentGuid,
    Position = spawnPosition,
    Rotation = quaternion.identity
});

// Add sync ID for bridge communication
EntityManager.AddComponentData(bodyEntity, new AgentSyncId
{
    Guid = agentGuid,
    MindEntityIndex = -1 // Will be set when Mind entity is created
});

// Add intent buffer (receives commands from Mind ECS)
EntityManager.AddBuffer<AgentIntentBuffer>(bodyEntity);

// Add other Body ECS components (health, stats, etc.)
EntityManager.AddComponentData(bodyEntity, new VillagerNeeds { Health = 100f });
EntityManager.AddComponentData(bodyEntity, LocalTransform.FromPositionRotationScale(
    spawnPosition, quaternion.identity, 1f));
```

**Step 2: Create Mind Entity (DefaultEcs)**

```csharp
// In MindECS initialization or spawn system
var mindWorld = MindECSWorld.Instance.World;
var mindEntity = mindWorld.CreateEntity();

// Add AgentGuid for mapping
mindWorld.Set(mindEntity, agentGuid);

// Add cognitive components
mindWorld.Set(mindEntity, new PersonalityProfile
{
    RiskTolerance = 0.5f,
    Aggressiveness = 0.3f,
    Curiosity = 0.7f
});

mindWorld.Set(mindEntity, new GoalProfile());
mindWorld.Set(mindEntity, new BehaviorProfile());
mindWorld.Set(mindEntity, new CognitiveMemory());

// Update mapping in Body ECS (via AgentMappingSystem or manually)
// This happens automatically via AgentMappingSystem
```

**Step 3: Verify Mapping**

The `AgentMappingSystem` automatically creates mappings. Verify in debug:

```csharp
var syncId = EntityManager.GetComponentData<AgentSyncId>(bodyEntity);
Assert.IsTrue(syncId.MindEntityIndex >= 0, "Mind entity should be mapped");
```

---

### 2. Sending Intent from Mind to Body

**In Mind ECS System (DefaultEcs)**

```csharp
using PureDOTS.Runtime.Bridges;
using PureDOTS.Shared;

public class MyCognitiveSystem : AEntitySetSystem<float>
{
    private AgentSyncBus _syncBus;

    public MyCognitiveSystem(World world, AgentSyncBus syncBus) 
        : base(world.GetEntities().With<PersonalityProfile>().AsSet())
    {
        _syncBus = syncBus;
    }

    protected override void Update(float deltaTime, in Entity entity)
    {
        var agentGuid = World.Get<AgentGuid>(entity);
        var personality = World.Get<PersonalityProfile>(entity);

        // Decide on action based on personality/goals
        if (personality.Curiosity > 0.7f)
        {
            // Send move intent
            var intent = new MindToBodyMessage
            {
                AgentGuid = agentGuid,
                Kind = IntentKind.Move,
                TargetPosition = new float3(10f, 0f, 10f),
                TargetEntity = Entity.Null,
                Priority = 128,
                TickNumber = 0 // Will be set by bridge system
            };

            _syncBus.EnqueueMindToBody(intent);
        }
    }
}
```

**Intent Types Available**

```csharp
public enum IntentKind : byte
{
    None = 0,
    Move = 1,        // Move to position
    Attack = 2,      // Attack target entity
    Harvest = 3,     // Gather resources
    Defend = 4,      // Defend position
    Patrol = 5,      // Patrol route
    UseAbility = 6,  // Use special ability
    Interact = 7,    // Interact with entity
    Rest = 8,        // Rest/recover
    Flee = 9         // Flee from danger
}
```

---

### 3. Receiving Telemetry from Body to Mind

**In Body ECS System (Unity Entities)**

Telemetry is automatically collected by `BodyToMindSyncSystem`. It reads:
- Position (`LocalTransform.Position`)
- Rotation (`LocalTransform.Rotation`)
- Health (`VillagerNeeds.Health` if present)
- Other stats (extend `BodyToMindMessage` if needed)

**In Mind ECS System (DefaultEcs)**

```csharp
public class MyTelemetryConsumer : AEntitySetSystem<float>
{
    private AgentSyncBus _syncBus;

    public MyTelemetryConsumer(World world, AgentSyncBus syncBus)
        : base(world.GetEntities().With<AgentGuid>().AsSet())
    {
        _syncBus = syncBus;
    }

    protected override void Update(float deltaTime, in Entity entity)
    {
        // Dequeue telemetry batch
        using var telemetryBatch = _syncBus.DequeueBodyToMindBatch(Allocator.Temp);

        var agentGuid = World.Get<AgentGuid>(entity);

        // Find telemetry for this agent
        for (int i = 0; i < telemetryBatch.Length; i++)
        {
            var telemetry = telemetryBatch[i];
            if (telemetry.AgentGuid.Equals(agentGuid))
            {
                // Update cognitive state based on telemetry
                if (World.Has<CognitiveMemory>(entity))
                {
                    var memory = World.Get<CognitiveMemory>(entity);
                    memory.AddEpisodicMemory(
                        telemetry.TickNumber,
                        "PositionUpdate",
                        new Dictionary<string, object> { ["position"] = telemetry.Position }
                    );
                }

                // React to health changes
                if ((telemetry.Flags & BodyToMindFlags.HealthChanged) != 0)
                {
                    if (telemetry.Health < 0.3f)
                    {
                        // Low health - add flee goal
                        if (World.Has<GoalProfile>(entity))
                        {
                            var goals = World.Get<GoalProfile>(entity);
                            goals.AddGoal("flee_low_health", "Flee", 0.9f);
                        }
                    }
                }
            }
        }
    }
}
```

---

### 4. Adding New Intent Types

**Step 1: Extend IntentKind Enum**

```csharp
// In PureDOTS/Packages/com.moni.puredots/Runtime/Bridges/AgentSyncBus.cs
public enum IntentKind : byte
{
    // ... existing types ...
    CustomAction = 10  // Your new intent type
}
```

**Step 2: Handle in IntentResolutionSystem**

```csharp
// In PureDOTS/Packages/com.moni.puredots/Runtime/Systems/IntentResolutionSystem.cs
case IntentKind.CustomAction:
    // Your custom action logic
    DoCustomAction(ref transform, intentCmd);
    break;
```

**Step 3: Send from Mind ECS**

```csharp
var intent = new MindToBodyMessage
{
    AgentGuid = agentGuid,
    Kind = IntentKind.CustomAction,
    TargetPosition = targetPos,
    TickNumber = 0
};
syncBus.EnqueueMindToBody(intent);
```

---

### 5. Extending Cognitive Systems

**Adding New Cognitive Components**

```csharp
// In PureDOTS/Packages/com.moni.puredots/Runtime/AI/MindECS/Components/
namespace PureDOTS.AI.MindECS.Components
{
    public class MyCustomCognitiveComponent
    {
        public float CustomValue;
        public Dictionary<string, object> CustomData;
    }
}
```

**Creating Cognitive System**

```csharp
// In PureDOTS/Packages/com.moni.puredots/Runtime/AI/MindECS/Systems/
using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.Runtime.Bridges;

namespace PureDOTS.AI.MindECS.Systems
{
    public class MyCognitiveSystem : AEntitySetSystem<float>
    {
        private AgentSyncBus _syncBus;

        public MyCognitiveSystem(World world, AgentSyncBus syncBus)
            : base(world.GetEntities()
                .With<MyCustomCognitiveComponent>()
                .With<PersonalityProfile>()
                .AsSet())
        {
            _syncBus = syncBus;
        }

        protected override void Update(float deltaTime, in Entity entity)
        {
            var custom = World.Get<MyCustomCognitiveComponent>(entity);
            var agentGuid = World.Get<AgentGuid>(entity);

            // Your cognitive logic here
            // Send intents via _syncBus.EnqueueMindToBody(...)
        }
    }
}
```

**Register System in MindECSWorld**

```csharp
// In PureDOTS/Packages/com.moni.puredots/Runtime/AI/MindECS/MindECSWorld.cs
public static void Initialize(AgentSyncBus syncBus)
{
    // ... existing initialization ...
    
    var mySystem = new MyCognitiveSystem(_world, syncBus);
    _systems.Add(mySystem);
}
```

---

### 6. Debugging Bridge Communication

**Check Bus Queue Counts**

```csharp
var coordinator = World.GetExistingSystemManaged<AgentSyncBridgeCoordinator>();
var bus = coordinator.GetBus();

Debug.Log($"Mind→Body queue: {bus.MindToBodyQueueCount}");
Debug.Log($"Body→Mind queue: {bus.BodyToMindQueueCount}");
```

**Verify Mappings**

```csharp
// In Body ECS
var syncId = EntityManager.GetComponentData<AgentSyncId>(bodyEntity);
if (syncId.MindEntityIndex < 0)
{
    Debug.LogWarning($"Body entity {bodyEntity} has no Mind mapping");
}

// In Mind ECS
var mindWorld = MindECSWorld.Instance.World;
var mindEntity = mindWorld.GetEntity(syncId.MindEntityIndex);
if (!mindWorld.Has<AgentGuid>(mindEntity))
{
    Debug.LogWarning($"Mind entity {mindEntity} missing AgentGuid");
}
```

**Enable Ownership Validation**

The `OwnershipValidatorSystem` runs in debug builds and logs warnings for:
- Component type name collisions
- Dual ownership violations
- Missing mappings

Check Unity Console for validation messages.

---

## Best Practices

### 1. Always Use Message Bus

❌ **Wrong**: Direct component access across ECS boundaries
```csharp
// DON'T: Mind ECS writing to Body component
bodyEntity.SetComponent(new LocalTransform { Position = newPos });
```

✅ **Correct**: Send intent via bus
```csharp
// DO: Send intent message
syncBus.EnqueueMindToBody(new MindToBodyMessage { ... });
```

### 2. Handle Missing Mappings Gracefully

```csharp
// Always check if mapping exists before sending intent
if (syncId.MindEntityIndex >= 0)
{
    syncBus.EnqueueMindToBody(intent);
}
else
{
    // Agent not fully initialized yet, skip this tick
}
```

### 3. Use Delta Compression

The bus automatically compresses redundant messages. Don't send identical intents every frame:

```csharp
// Only send intent if goal/target changed
if (currentGoal != lastGoal || currentTarget != lastTarget)
{
    syncBus.EnqueueMindToBody(intent);
    lastGoal = currentGoal;
    lastTarget = currentTarget;
}
```

### 4. Respect Update Cadences

- **Mind ECS**: 2-5 Hz (throttle per entity)
- **Body ECS**: 60 Hz fixed-step
- **Bridge Sync**: 250ms (Mind→Body), 100ms (Body→Mind)

Don't flood the bus with messages every frame.

### 5. Keep Messages Burst-Safe

Message structs must be value types with no managed references:

```csharp
// ✅ Good: Value types only
public struct MindToBodyMessage
{
    public AgentGuid AgentGuid;  // struct
    public IntentKind Kind;       // enum
    public float3 TargetPosition; // struct
    public Entity TargetEntity;   // struct (Unity.Entities.Entity)
}

// ❌ Bad: Managed references
public struct BadMessage
{
    public string Message;  // DON'T: managed string
    public object Data;      // DON'T: managed object
}
```

---

## Common Patterns

### Pattern 1: Goal-Driven Intent Generation

```csharp
protected override void Update(float deltaTime, in Entity entity)
{
    var goals = World.Get<GoalProfile>(entity);
    var agentGuid = World.Get<AgentGuid>(entity);

    if (goals.ActiveGoals.Count == 0)
        return;

    var primaryGoal = goals.ActiveGoals[0];
    
    IntentKind intentKind = primaryGoal.Type switch
    {
        "Move" => IntentKind.Move,
        "Attack" => IntentKind.Attack,
        "Harvest" => IntentKind.Harvest,
        "Rest" => IntentKind.Rest,
        "Flee" => IntentKind.Flee,
        _ => IntentKind.None
    };

    if (intentKind != IntentKind.None)
    {
        var intent = new MindToBodyMessage
        {
            AgentGuid = agentGuid,
            Kind = intentKind,
            TargetPosition = ExtractTargetPosition(primaryGoal),
            Priority = (byte)(primaryGoal.Priority * 255f),
            TickNumber = 0
        };
        _syncBus.EnqueueMindToBody(intent);
    }
}
```

### Pattern 2: Reactive Behavior Based on Telemetry

```csharp
protected override void Update(float deltaTime, in Entity entity)
{
    using var telemetry = _syncBus.DequeueBodyToMindBatch(Allocator.Temp);
    var agentGuid = World.Get<AgentGuid>(entity);

    foreach (var msg in telemetry)
    {
        if (!msg.AgentGuid.Equals(agentGuid))
            continue;

        // React to health changes
        if ((msg.Flags & BodyToMindFlags.HealthChanged) != 0 && msg.Health < 0.5f)
        {
            // Add flee goal
            var goals = World.Get<GoalProfile>(entity);
            goals.AddGoal("flee_low_health", "Flee", 0.9f);
        }

        // React to position changes (e.g., reached destination)
        if ((msg.Flags & BodyToMindFlags.PositionChanged) != 0)
        {
            var memory = World.Get<CognitiveMemory>(entity);
            memory.AddEpisodicMemory(msg.TickNumber, "Moved", null);
        }
    }
}
```

### Pattern 3: Personality-Modified Intent Priority

```csharp
protected override void Update(float deltaTime, in Entity entity)
{
    var personality = World.Get<PersonalityProfile>(entity);
    var goals = World.Get<GoalProfile>(entity);
    var agentGuid = World.Get<AgentGuid>(entity);

    foreach (var goal in goals.ActiveGoals)
    {
        float basePriority = goal.Priority;
        float modifiedPriority = basePriority;

        // Modify priority based on personality
        switch (goal.Type)
        {
            case "Combat":
                modifiedPriority *= (1f + personality.Aggressiveness);
                break;
            case "Explore":
                modifiedPriority *= (1f + personality.Curiosity);
                break;
            case "Social":
                modifiedPriority *= (1f + personality.SocialPreference);
                break;
        }

        // Send intent with modified priority
        var intent = new MindToBodyMessage
        {
            AgentGuid = agentGuid,
            Kind = MapGoalToIntent(goal.Type),
            Priority = (byte)math.clamp(modifiedPriority * 255f, 0f, 255f),
            TickNumber = 0
        };
        _syncBus.EnqueueMindToBody(intent);
    }
}
```

---

## Troubleshooting

### Issue: Intents Not Reaching Body ECS

**Check**:
1. Is `AgentSyncBridgeCoordinator` initialized? (runs in `InitializationSystemGroup`)
2. Is `MindToBodySyncSystem` running? (runs in `SimulationSystemGroup`)
3. Does entity have `AgentIntentBuffer` component?
4. Is `AgentSyncId.MindEntityIndex >= 0`? (mapping exists)

**Debug**:
```csharp
var coordinator = World.GetExistingSystemManaged<AgentSyncBridgeCoordinator>();
var bus = coordinator.GetBus();
Debug.Log($"Queue count: {bus.MindToBodyQueueCount}");
```

### Issue: Telemetry Not Reaching Mind ECS

**Check**:
1. Is `BodyToMindSyncSystem` running?
2. Does entity have `AgentSyncId` with valid `MindEntityIndex`?
3. Is Mind entity still alive in DefaultEcs world?

**Debug**:
```csharp
var bus = AgentSyncBridgeCoordinator.GetBusFromDefaultWorld();
Debug.Log($"Telemetry queue: {bus.BodyToMindQueueCount}");
```

### Issue: Performance Problems

**Optimize**:
1. Reduce Mind ECS update frequency (increase throttle interval)
2. Batch telemetry consumption (don't process every frame)
3. Use delta compression (don't send unchanged intents)
4. Limit active "thinking" agents (only update active ones)

---

## References

- **Architecture**: `Docs/AI/Hybrid_ECS_Responsibility_Map.md`
- **Bridge Implementation**: `Runtime/Bridges/AgentSyncBus.cs`
- **Sync Systems**: `Runtime/Bridges/MindToBodySyncSystem.cs`, `BodyToMindSyncSystem.cs`
- **Intent Resolution**: `Runtime/Systems/IntentResolutionSystem.cs`
- **Mind ECS World**: `Runtime/AI/MindECS/MindECSWorld.cs`

---

## Quick Reference

| Task | File/System | Key Method |
|------|-------------|------------|
| Send intent | `AgentSyncBus` | `EnqueueMindToBody()` |
| Receive intent | `MindToBodySyncSystem` | Reads `AgentIntentBuffer` |
| Send telemetry | `BodyToMindSyncSystem` | `EnqueueBodyToMind()` |
| Receive telemetry | Mind ECS system | `DequeueBodyToMindBatch()` |
| Create mapping | `AgentMappingSystem` | Automatic on spawn |
| Access bus | `AgentSyncBridgeCoordinator` | `GetBusFromDefaultWorld()` |

