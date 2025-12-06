# Multi-ECS Integration Guide

**Last Updated**: 2025-01-27  
**Purpose**: Guide for agents implementing features that interface with the multi-ECS hybrid architecture.

---

## Quick Reference

| Task | System/API | Location |
|------|------------|----------|
| Access sync bus | `AgentSyncBridgeCoordinator` | `Runtime/Bridges/AgentSyncBridgeCoordinator.cs` |
| Send intent to Body ECS | `AgentSyncBus.EnqueueMindToBody()` | `Runtime/Bridges/AgentSyncBus.cs` |
| Send state to Mind ECS | `AgentSyncBus.EnqueueBodyToMind()` | `Runtime/Bridges/AgentSyncBus.cs` |
| Send percepts | `AgentSyncBus.EnqueuePercept()` | `Runtime/Bridges/AgentSyncBus.cs` |
| Send limb commands | `AgentSyncBus.EnqueueLimbCommand()` | `Runtime/Bridges/AgentSyncBus.cs` |
| Create cognitive agent | `MindECSWorld.Instance` | `Runtime/AI/MindECS/MindECSWorld.cs` |
| Create aggregate | `AggregateECSWorld.Instance` | `Runtime/AI/AggregateECS/AggregateECSWorld.cs` |

---

## 1. Getting the Sync Bus

The `AgentSyncBus` is managed by `AgentSyncBridgeCoordinator` and provides cross-ECS communication.

### From Unity Entities System (Burst-safe access)

```csharp
// In managed SystemBase (not Burst-compiled)
public sealed partial class MyBridgeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var coordinator = World.GetExistingSystemManaged<AgentSyncBridgeCoordinator>();
        if (coordinator == null) return;
        
        var bus = coordinator.GetBus();
        if (bus == null) return;
        
        // Use bus...
    }
}
```

### From DefaultEcs System (Mind ECS)

```csharp
// In DefaultEcs system (managed)
public class MyCognitiveSystem : AEntitySetSystem<float>
{
    private AgentSyncBus _syncBus;
    
    public MyCognitiveSystem(World world, AgentSyncBus syncBus) 
        : base(world.GetEntities().With<MyComponent>().AsSet())
    {
        _syncBus = syncBus;
    }
    
    protected override void Update(float deltaTime, in Entity entity)
    {
        // Use _syncBus...
    }
}
```

### Static Access (for initialization)

```csharp
// Static accessor (managed only)
var bus = AgentSyncBridgeCoordinator.GetBusFromDefaultWorld();
if (bus != null)
{
    // Use bus...
}
```

---

## 2. Creating a Cognitive Agent

### Step 1: Create Body ECS Entity (Unity Entities)

```csharp
// In authoring/baker or spawn system
var entity = EntityManager.CreateEntity();

// Add required Body ECS components
EntityManager.AddComponent<AgentBody>(entity);
EntityManager.AddComponent<AgentSyncId>(entity);
EntityManager.AddComponent<LocalTransform>(entity);

// Set AgentBody with GUID
var agentBody = new AgentBody
{
    Id = AgentGuid.NewGuid(),
    Position = float3.zero,
    Rotation = quaternion.identity
};
EntityManager.SetComponent(entity, agentBody);

// Set AgentSyncId (Mind entity not created yet)
var syncId = new AgentSyncId
{
    Guid = agentBody.Id,
    MindEntityIndex = -1 // Will be set when Mind entity is created
};
EntityManager.SetComponent(entity, syncId);
```

### Step 2: Create Mind ECS Entity (DefaultEcs)

```csharp
// In managed system (not Burst)
var mindWorld = MindECSWorld.Instance;
var mindEntity = mindWorld.World.CreateEntity();

// Add AgentGuid component (links to Body ECS)
mindWorld.World.Set(mindEntity, agentBody.Id);

// Add cognitive components
var personality = new PersonalityProfile
{
    RiskTolerance = 0.5f,
    Aggressiveness = 0.3f,
    // ... set other traits
};
mindWorld.World.Set(mindEntity, personality);

var goals = new GoalProfile();
goals.AddGoal("harvest_1", "Harvest", 0.7f);
mindWorld.World.Set(mindEntity, goals);

var behavior = new BehaviorProfile();
mindWorld.World.Set(mindEntity, behavior);

var memory = new CognitiveMemory();
mindWorld.World.Set(mindEntity, memory);

// Update AgentSyncId in Body ECS with Mind entity index
// Note: DefaultEcs uses entity indices, not Entity handles
var mindEntityIndex = mindEntity; // DefaultEcs Entity is an int
syncId.MindEntityIndex = mindEntityIndex;
EntityManager.SetComponent(bodyEntity, syncId);
```

---

## 3. Sending Messages Between ECS Layers

### Mind → Body: Sending Intent Commands

```csharp
// In DefaultEcs cognitive system
var message = new MindToBodyMessage
{
    AgentGuid = agentGuid,
    Kind = IntentKind.Move,
    TargetPosition = new float3(10f, 0f, 10f),
    TargetEntity = Entity.Null,
    Priority = 128, // 0-255
    TickNumber = 0 // Will be set by bridge system
};

_syncBus.EnqueueMindToBody(message);
```

### Body → Mind: Sending State Updates

```csharp
// In Unity Entities system (Burst job)
var message = new BodyToMindMessage
{
    AgentGuid = syncId.Guid,
    Position = transform.Position,
    Rotation = transform.Rotation,
    Health = needs.Health,
    MaxHealth = 100f,
    Flags = 0, // Set by sync bus delta compression
    TickNumber = tickNumber,
    AggregateGuid = membership.AggregateGuid
};

// Enqueue in managed system (not in Burst job)
bus.EnqueueBodyToMind(message);
```

### Sending Percepts (Body → Mind)

```csharp
// In Unity Entities perception system
var percept = new Percept
{
    AgentGuid = agentGuid,
    Type = SensorType.Vision,
    Source = targetPosition,
    Confidence = 0.85f,
    TickNumber = tickNumber
};

bus.EnqueuePercept(percept);
```

### Sending Limb Commands (Mind → Body)

```csharp
// In DefaultEcs cognitive system
var command = new LimbCommand
{
    AgentGuid = agentGuid,
    LimbIndex = 0, // Weapon limb
    Action = LimbAction.Activate,
    Target = targetPosition,
    Priority = 200,
    TickNumber = 0 // Will be set by bridge system
};

_syncBus.EnqueueLimbCommand(command);
```

---

## 4. Working with Aggregates

### Creating an Aggregate

```csharp
// In managed system
var aggregateWorld = AggregateECSWorld.Instance;
var aggregateEntity = aggregateWorld.World.CreateEntity();

var aggregate = new AggregateEntity
{
    AggregateGuid = AgentGuid.NewGuid(),
    MemberGuids = new List<AgentGuid> { agent1Guid, agent2Guid, agent3Guid }
};

aggregateWorld.World.Set(aggregateEntity, aggregate);
```

### Sending Aggregate Intent to Mind ECS

```csharp
// In Aggregate ECS system
var intent = new AggregateIntentMessage
{
    AggregateGuid = aggregateGuid,
    GoalType = "Harvest",
    Priority = 0.8f,
    TargetPosition = farmLocation,
    DistributionRatios = new Dictionary<string, float>
    {
        { "Farm", 0.6f },
        { "Defend", 0.3f },
        { "Rest", 0.1f }
    }
};

bus.EnqueueAggregateIntent(intent);
```

### Receiving Aggregate Intent in Cognitive System

Aggregate intents are automatically consumed by `CognitiveSystem` and bias individual agent goals. The system:
1. Caches aggregate intents by aggregate GUID
2. Maps agents to aggregates via `AgentSyncId.ClusterGuid` or `AggregateMembership`
3. Applies distribution ratios to goal priorities

---

## 5. Consensus Voting

### Sending a Consensus Vote

```csharp
// In DefaultEcs or Unity Entities system
var vote = new ConsensusVoteMessage
{
    VoterGuid = agentGuid,
    ClusterGuid = clusterGuid,
    VoteValue = 150, // 0-255
    Tier = ConsensusTier.Local,
    TickNumber = tickNumber
};

bus.EnqueueConsensusVote(vote);
```

### Processing Consensus Votes

```csharp
// In consensus arbitration system
var votes = bus.DequeueConsensusVoteBatch();
// Returns: Dictionary<AgentGuid, Dictionary<ConsensusTier, List<ConsensusVoteMessage>>>

foreach (var clusterGroup in votes)
{
    var clusterGuid = clusterGroup.Key;
    foreach (var tierGroup in clusterGroup.Value)
    {
        var tier = tierGroup.Key;
        var votesForTier = tierGroup.Value;
        
        // Calculate consensus (e.g., average)
        float sum = 0f;
        foreach (var vote in votesForTier)
        {
            sum += vote.VoteValue;
        }
        byte resolvedValue = (byte)(sum / votesForTier.Count);
        
        // Broadcast outcome
        var outcome = new ConsensusOutcomeMessage
        {
            ClusterGuid = clusterGuid,
            Tier = tier,
            ResolvedValue = resolvedValue,
            VoteCount = votesForTier.Count,
            ResolutionTick = tickNumber
        };
        
        bus.EnqueueConsensusOutcome(outcome);
    }
}
```

---

## 6. Message Types Reference

### MindToBodyMessage
- **Purpose**: Intent commands from cognitive layer
- **Fields**: `AgentGuid`, `IntentKind`, `TargetPosition`, `TargetEntity`, `Priority`, `TickNumber`
- **Usage**: Generated by `CognitiveSystem`, consumed by `MindToBodySyncSystem`

### BodyToMindMessage
- **Purpose**: State updates from simulation layer
- **Fields**: `AgentGuid`, `Position`, `Rotation`, `Health`, `MaxHealth`, `Flags`, `TickNumber`, `AggregateGuid`
- **Usage**: Generated by `BodyToMindSyncSystem`, consumed by `CognitiveSystem`

### Percept
- **Purpose**: Sensor readings (vision, smell, hearing)
- **Fields**: `AgentGuid`, `Type`, `Source`, `Confidence`, `TickNumber`
- **Usage**: Generated by perception systems, consumed by `CognitiveSystem`

### LimbCommand
- **Purpose**: Limb activation commands (weapon, manipulator, etc.)
- **Fields**: `AgentGuid`, `LimbIndex`, `Action`, `Target`, `Priority`, `TickNumber`
- **Usage**: Generated by `CognitiveSystem`, consumed by limb systems

### AggregateIntentMessage
- **Purpose**: Group-level goals and distribution ratios
- **Fields**: `AggregateGuid`, `GoalType`, `Priority`, `TargetPosition`, `DistributionRatios`
- **Usage**: Generated by Aggregate ECS, consumed by `CognitiveSystem`

### ConsensusVoteMessage
- **Purpose**: Votes for hierarchical consensus
- **Fields**: `VoterGuid`, `ClusterGuid`, `VoteValue`, `Tier`, `TickNumber`
- **Usage**: Generated by agents, consumed by consensus arbitration systems

### ConsensusOutcomeMessage
- **Purpose**: Resolved consensus values
- **Fields**: `ClusterGuid`, `Tier`, `ResolvedValue`, `VoteCount`, `ResolutionTick`
- **Usage**: Generated by consensus systems, consumed by agents

---

## 7. Component Requirements

### Body ECS Components (Unity Entities)

**Required for cognitive agents**:
- `AgentBody`: Physical state with GUID
- `AgentSyncId`: Links to Mind ECS entity
- `LocalTransform`: Position/rotation

**Optional**:
- `VillagerNeeds`: Health/needs state
- `AggregateMembership`: Aggregate membership
- `LimbElement`: Limb entities (for limb commands)
- `AgentIntentBuffer`: Intent buffer (added by bridge system)

### Mind ECS Components (DefaultEcs)

**Required**:
- `AgentGuid`: Links to Body ECS entity
- `PersonalityProfile`: Personality traits
- `GoalProfile`: Active goals
- `BehaviorProfile`: Behavior preferences

**Optional**:
- `CognitiveMemory`: Episodic memory, percepts, relationships

---

## 8. System Ordering

Bridge systems run in `SimulationSystemGroup`:

1. `AgentMappingSystem` - Creates GUID mappings
2. `BodyToMindSyncSystem` - Syncs Body → Mind (100ms interval)
3. `MindToBodySyncSystem` - Syncs Mind → Body (250ms interval)
4. `IntentResolutionSystem` - Resolves intents to actions

Cognitive systems run in DefaultEcs world (1-5 Hz):
- `CognitiveSystem` - Evaluates goals, generates intents
- `GoalEvaluationSystem` - Processes goal priorities
- `DeceptionSystem` - Handles deception mechanics

---

## 9. Performance Guidelines

### Sync Intervals
- **Body → Mind**: 100ms default (10 Hz)
- **Mind → Body**: 250ms default (4 Hz)
- **Cognitive tick**: 1-5 Hz per entity (throttled)

### Performance Targets
- **Sync cost**: < 3ms/frame
- **Message batching**: Process in batches per sync interval
- **Delta compression**: Only sync changed fields

### Optimization Tips
1. Use delta compression (automatic in `AgentSyncBus`)
2. Batch messages per sync interval
3. Throttle cognitive updates (1-5 Hz per entity)
4. Profile with `MultiECSPerformanceProfiler`

---

## 10. Common Patterns

### Pattern: Agent Spawn

```csharp
// 1. Create Body ECS entity with GUID
var bodyEntity = CreateBodyEntity(guid);

// 2. Create Mind ECS entity with same GUID
var mindEntity = CreateMindEntity(guid);

// 3. Link via AgentSyncId
LinkEntities(bodyEntity, mindEntity);
```

### Pattern: Sending Intent Based on Goal

```csharp
// In CognitiveSystem
var primaryGoal = goals.ActiveGoals[0];
var intent = MapGoalToIntent(primaryGoal);
_syncBus.EnqueueMindToBody(intent);
```

### Pattern: Reacting to Percepts

```csharp
// In CognitiveSystem
var percepts = memory.RecentPercepts;
foreach (var percept in percepts)
{
    if (percept.Type == SensorType.Vision && percept.Confidence > 0.8f)
    {
        // Generate combat intent or limb command
    }
}
```

### Pattern: Aggregate Goal Distribution

```csharp
// Aggregate ECS sends intent with distribution ratios
// CognitiveSystem automatically applies ratios to goal priorities
// Agents bias their goals based on aggregate intent
```

---

## 11. Troubleshooting

### Messages Not Received

1. **Check sync bus initialization**: Verify `AgentSyncBridgeCoordinator` exists
2. **Check GUID mapping**: Verify `AgentSyncId.MindEntityIndex >= 0`
3. **Check sync intervals**: Messages batched per interval (100ms/250ms)
4. **Check message queue**: Use `bus.MindToBodyQueueCount` to verify messages enqueued

### Burst Compilation Errors

1. **Never access sync bus in Burst code**: Use managed `SystemBase` wrapper
2. **Use GUID, not Entity**: Cross-ECS references use `AgentGuid`
3. **Batch operations**: Collect data in Burst job, enqueue in managed system

### Performance Issues

1. **Profile sync costs**: Use `MultiECSPerformanceProfiler`
2. **Reduce sync frequency**: Increase sync intervals if needed
3. **Throttle cognitive updates**: Reduce cognitive tick rate
4. **Check message volume**: Verify delta compression working

---

## 12. Extension Points

### Adding New Message Types

1. **Define message struct** in `AgentSyncBus.cs`:
```csharp
public struct MyCustomMessage
{
    public AgentGuid AgentGuid;
    public float Data;
    public uint TickNumber;
}
```

2. **Add queue to AgentSyncBus**:
```csharp
private readonly Queue<MyCustomMessage> _myCustomQueue;
```

3. **Add enqueue/dequeue methods**:
```csharp
public void EnqueueMyCustom(MyCustomMessage message) { ... }
public NativeList<MyCustomMessage> DequeueMyCustomBatch(Allocator allocator) { ... }
```

4. **Process in bridge system**:
```csharp
if (bus.MyCustomQueueCount > 0)
{
    using var batch = bus.DequeueMyCustomBatch(Allocator.TempJob);
    // Process batch...
}
```

### Adding New Cognitive Components

1. **Define component class** in `Runtime/AI/MindECS/Components/`
2. **Add to DefaultEcs entity**:
```csharp
var component = new MyComponent();
mindWorld.World.Set(entity, component);
```

3. **Query in cognitive system**:
```csharp
: base(world.GetEntities().With<MyComponent>().AsSet())
```

---

## References

- `MultiECSArchitecture.md` - Architecture overview
- `FoundationGuidelines.md` - Multi-ECS patterns (P25)
- `TRI_PROJECT_BRIEFING.md` - Coding patterns and constraints
- `AgentSyncBus.cs` - Message broker implementation
- `AgentSyncBridgeCoordinator.cs` - Bus coordinator

