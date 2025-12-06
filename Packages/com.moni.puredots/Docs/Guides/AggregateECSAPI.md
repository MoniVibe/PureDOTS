# Aggregate ECS API Reference

## Components

### AggregateMembership (Body ECS)

**Location**: `PureDOTS.Runtime.Components.AggregateMembership`

Burst-safe component indicating which aggregate an agent belongs to.

```csharp
public struct AggregateMembership : IComponentData
{
    public AgentGuid AggregateGuid; // Which aggregate this agent belongs to
    public byte Role; // Role within aggregate (optional, 0 = default member)
}
```

**Usage**: Add to Body ECS entities to assign them to aggregates.

### AggregateEntity (Aggregate ECS)

**Location**: `PureDOTS.AI.AggregateECS.Components.AggregateEntity`

Managed class component for aggregate entities in DefaultEcs world.

```csharp
public class AggregateEntity
{
    public AgentGuid AggregateGuid; // Unique identifier
    public AggregateType Type; // Village, Fleet, Band
    public List<AgentGuid> MemberGuids; // Buffer of agent GUIDs
    public AggregateStats Stats; // Food, morale, defense, population
    public float3 CenterPosition; // Approximate center of aggregate
}
```

**Methods**:
- `AddMember(AgentGuid)` - Add agent to aggregate
- `RemoveMember(AgentGuid)` - Remove agent from aggregate
- `HasMember(AgentGuid)` - Check if agent is member

### AggregateStats

**Location**: `PureDOTS.AI.AggregateECS.Components.AggregateStats`

Aggregate statistics collected from member agents.

```csharp
public struct AggregateStats
{
    public float Food; // Total or average food level
    public float Morale; // Average morale
    public float Defense; // Defense capability score
    public int Population; // Number of members
    public float Health; // Average health
    public float Energy; // Average energy
}
```

### AggregateIntent (Aggregate ECS)

**Location**: `PureDOTS.AI.AggregateECS.Components.AggregateIntent`

Group-level goals and distribution ratios.

```csharp
public class AggregateIntent
{
    public string CurrentGoal; // "Harvest", "Defend", "Patrol", "Rest", etc.
    public float Priority; // 0-1
    public float3 TargetPosition; // Optional target position
    public Dictionary<string, float> DistributionRatios; // e.g., "Farm"=0.6, "Defend"=0.3
}
```

**Methods**:
- `SetDistribution(string goalType, float ratio)` - Set distribution ratio for goal type
- `GetDistribution(string goalType)` - Get distribution ratio (returns 0 if not set)

## Systems

### AggregateBridgeSystem

**Location**: `PureDOTS.Runtime.Bridges.AggregateBridgeSystem`

**Group**: `GameplaySystemGroup`, `[UpdateAfter(typeof(VillagerSystemGroup))]`

**Update Rate**: 1 Hz

Collects aggregate statistics from Body ECS and updates AggregateECSWorld.

**Responsibilities**:
- Queries entities with `AggregateMembership`
- Groups by `AggregateGuid`
- Calculates aggregate stats (food, morale, defense, population)
- Creates/updates `AggregateEntity` in AggregateECSWorld

### AggregateECSUpdateSystem

**Location**: `PureDOTS.Runtime.Bridges.AggregateECSUpdateSystem`

**Group**: `GameplaySystemGroup`, `[UpdateAfter(typeof(AggregateBridgeSystem))]`

**Update Rate**: 1 Hz

Updates AggregateECSWorld (runs aggregate systems).

### AggregateIntentSystem

**Location**: `PureDOTS.AI.AggregateECS.Systems.AggregateIntentSystem`

**World**: AggregateECSWorld (DefaultEcs)

**Update Rate**: 1 Hz

Evaluates aggregate state and produces group-level goals.

**Logic**:
- Food < 30 → "Harvest" goal
- Defense < 40 → "Defend" goal
- Morale < 50 → "Rest" goal
- Default → "Patrol" goal

**Output**: Publishes `AggregateIntentMessage` to `AgentSyncBus`

## Messages

### AggregateIntentMessage

**Location**: `PureDOTS.Runtime.Bridges.AggregateIntentMessage`

Message sent from Aggregate ECS to Mind ECS.

```csharp
public struct AggregateIntentMessage
{
    public AgentGuid AggregateGuid;
    public string GoalType; // "Harvest", "Defend", "Patrol", "Rest", etc.
    public float Priority; // 0-1
    public float3 TargetPosition;
    public Dictionary<string, float> DistributionRatios;
}
```

## AgentSyncBus Extensions

**Location**: `PureDOTS.Runtime.Bridges.AgentSyncBus`

### Methods

```csharp
// Enqueue aggregate intent message
public void EnqueueAggregateIntent(AggregateIntentMessage message)

// Dequeue all pending aggregate intents
public List<AggregateIntentMessage> DequeueAggregateIntentBatch()

// Queue count
public int AggregateIntentQueueCount { get; }
```

## AggregateECSWorld

**Location**: `PureDOTS.AI.AggregateECS.AggregateECSWorld`

Singleton manager for aggregate DefaultEcs world.

```csharp
public class AggregateECSWorld
{
    public static AggregateECSWorld Instance { get; }
    public World World { get; }
    public SequentialSystem<ISystem<World>> Systems { get; }
    
    public static void Initialize(AgentSyncBus syncBus)
    public void Update(float deltaTime)
    public void Dispose()
}
```

**Usage**:
```csharp
// Get instance (auto-initializes if needed)
var aggregateWorld = AggregateECSWorld.Instance;

// Update aggregate world (called by AggregateECSUpdateSystem)
aggregateWorld.Update(deltaTime);

// Query aggregate entities
foreach (var entity in aggregateWorld.World.GetEntities().With<AggregateEntity>().AsSet().GetEntities())
{
    var aggregate = aggregateWorld.World.Get<AggregateEntity>(entity);
    // ...
}
```

## CognitiveSystem Extensions

**Location**: `PureDOTS.AI.MindECS.Systems.CognitiveSystem`

### Aggregate Intent Consumption

`CognitiveSystem` automatically:
1. Dequeues aggregate intents from `AgentSyncBus`
2. Caches intents by aggregate GUID
3. Builds agent-to-aggregate mapping from aggregate member lists
4. Applies bias in `EvaluateGoals()` method

### Goal Bias Application

In `EvaluateGoals()`, aggregate intents bias goal priorities:

```csharp
// Distribution ratio bias
if (distributionRatio > 0f)
{
    float aggregateBias = 1f + (aggregateIntent.Priority * distributionRatio * 0.5f);
    adjustedPriority *= aggregateBias;
}

// Goal type match boost
if (aggregateIntent.GoalType == goal.Type)
{
    adjustedPriority *= (1f + aggregateIntent.Priority * 0.3f);
}
```

## BodyToMindMessage Extension

**Location**: `PureDOTS.Runtime.Bridges.BodyToMindMessage`

Extended to include aggregate membership:

```csharp
public struct BodyToMindMessage
{
    // ... existing fields
    public AgentGuid AggregateGuid; // Which aggregate agent belongs to (empty if none)
}
```

## File Locations

### Components
- `Runtime/Components/AggregateMembership.cs`
- `Runtime/AI/AggregateECS/Components/AggregateComponents.cs`

### Systems
- `Runtime/Bridges/AggregateBridgeSystem.cs`
- `Runtime/Bridges/AggregateECSUpdateSystem.cs`
- `Runtime/AI/AggregateECS/Systems/AggregateIntentSystem.cs`

### World Management
- `Runtime/AI/AggregateECS/AggregateECSWorld.cs`

### Messages
- `Runtime/Bridges/AgentSyncBus.cs` (AggregateIntentMessage)

## Integration Checklist

- [ ] Add `AggregateMembership` component to agents
- [ ] Verify `AggregateBridgeSystem` is enabled
- [ ] Verify `AggregateECSUpdateSystem` is enabled
- [ ] Verify `AggregateIntentSystem` is registered in AggregateECSWorld
- [ ] Verify `CognitiveSystem` consumes aggregate intents
- [ ] Test aggregate goal production and bias application
- [ ] Monitor sync cost (< 100 messages/frame for 1000 aggregates)

## See Also

- [AggregateECSIntegrationGuide.md](AggregateECSIntegrationGuide.md) - Integration guide
- `Runtime/AI/AggregateECS/` - Source code

