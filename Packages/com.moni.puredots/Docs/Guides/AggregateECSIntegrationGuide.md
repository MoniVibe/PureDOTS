# Aggregate ECS Integration Guide

## Overview

The Aggregate ECS layer enables group-level AI coordination for villages, fleets, and bands. Aggregates produce group-level goals that bias individual agent goal evaluation in Mind ECS, enabling emergent coordination without breaking determinism or sync cost targets.

## Architecture

Three-layer ECS hierarchy:
- **PureDOTS (Body ECS)**: Burst-compiled, deterministic, 60 Hz - physical simulation
- **Mind ECS (DefaultEcs)**: Managed, non-deterministic, 2-5 Hz - individual agent cognition
- **Aggregate ECS (DefaultEcs)**: Managed, non-deterministic, 1 Hz - group-level coordination

## Data Flow

```
Body ECS (PureDOTS)
  ├── AggregateMembership component (which aggregate agent belongs to)
  ├── VillagerNeeds, Health, Position (stats)
  └── AggregateBridgeSystem collects stats → AggregateECSWorld

Aggregate ECS (DefaultEcs)
  ├── AggregateEntity (members, stats)
  ├── AggregateIntent (current goal, distribution ratios)
  └── AggregateIntentSystem produces group goals → AgentSyncBus

Mind ECS (DefaultEcs)
  ├── CognitiveSystem consumes aggregate intents
  ├── Applies bias to individual agent goal priorities
  └── Generates intents → AgentSyncBus → Body ECS
```

## Creating Aggregates

### 1. Assign Agents to Aggregates

Add `AggregateMembership` component to Body ECS entities:

```csharp
// In a baker or system
var membership = new AggregateMembership
{
    AggregateGuid = villageGuid, // AgentGuid of the aggregate
    Role = 0 // Optional role byte
};
EntityManager.AddComponent(entity, membership);
```

### 2. Aggregate Statistics Collection

`AggregateBridgeSystem` automatically:
- Queries entities with `AggregateMembership`
- Groups by `AggregateGuid`
- Calculates aggregate stats (food, morale, defense, population)
- Updates `AggregateECSWorld` entities

Stats are aggregated from member agents:
- **Food**: Average hunger level from `VillagerNeeds.HungerFloat`
- **Morale**: Average morale from `VillagerNeeds.MoraleFloat`
- **Health**: Average health from `VillagerNeeds.Health`
- **Energy**: Average energy from `VillagerNeeds.EnergyFloat`
- **Defense**: Calculated from health and population
- **Population**: Count of members

### 3. Aggregate Goal Production

`AggregateIntentSystem` evaluates aggregate stats and produces goals:

**Priority 1: Food Crisis**
- If `Food < 30` → Goal: "Harvest", Priority: 0.5-1.0
- Distribution: 70% Harvest, 20% Defend, 10% Rest

**Priority 2: Defense Crisis**
- If `Defense < 40` → Goal: "Defend", Priority: 0.5-1.0
- Distribution: 60% Defend, 30% Harvest, 10% Rest

**Priority 3: Morale Crisis**
- If `Morale < 50` → Goal: "Rest", Priority: 0.3-0.7
- Distribution: 50% Rest, 30% Harvest, 20% Defend

**Default: Balanced**
- Goal: "Patrol", Priority: 0.3
- Distribution: 40% Harvest, 30% Defend, 20% Rest, 10% Patrol

## Aggregate Intent Bias

`CognitiveSystem` consumes aggregate intents and biases individual agent goals:

1. **Distribution Ratio Bias**: If aggregate says 60% Farm, farming goals get priority boost
2. **Goal Type Match**: If aggregate goal matches agent goal type, additional boost applied
3. **Priority Scaling**: Bias strength scales with aggregate intent priority

**Example**:
- Aggregate has "Harvest" goal with 0.8 priority, 70% Harvest distribution
- Agent has "Harvest" goal with base priority 0.5
- Final priority: `0.5 * (1 + 0.8 * 0.7 * 0.5) * (1 + 0.8 * 0.3) ≈ 0.9`

## System Ordering

```
GameplaySystemGroup
  ├── VillagerSystemGroup (updates agent stats)
  ├── AggregateBridgeSystem (collects stats, updates aggregate world)
  ├── AggregateECSUpdateSystem (runs aggregate world at 1 Hz)
  └── MindECSUpdateSystem (runs mind world at 2-5 Hz, consumes aggregate intents)
```

## Update Cadence

- **Aggregates**: 1 Hz (slow, strategic decisions)
- **Individual Agents**: 2-5 Hz (tactical decisions)
- **Body ECS**: 60 Hz (physical simulation)

## Customization

### Custom Aggregate Goal Logic

Extend `AggregateIntentSystem.EvaluateAggregateIntent()`:

```csharp
// In AggregateIntentSystem.cs
private void EvaluateAggregateIntent(AggregateEntity aggregate, AggregateIntent intent)
{
    var stats = aggregate.Stats;
    
    // Custom logic here
    if (stats.Population > 100 && stats.Food > 50)
    {
        intent.CurrentGoal = "Expand";
        intent.Priority = 0.6f;
        intent.SetDistribution("Expand", 0.5f);
        intent.SetDistribution("Harvest", 0.3f);
        intent.SetDistribution("Defend", 0.2f);
    }
    // ... existing logic
}
```

### Custom Goal Distribution

Modify distribution ratios in `AggregateIntentSystem` or extend `AggregateIntent.SetDistribution()` to support custom goal types.

### Aggregate Type-Specific Behavior

Use `AggregateEntity.Type` (Village, Fleet, Band) to customize behavior:

```csharp
switch (aggregate.Type)
{
    case AggregateType.Village:
        // Village-specific logic
        break;
    case AggregateType.Fleet:
        // Fleet-specific logic
        break;
    case AggregateType.Band:
        // Band-specific logic
        break;
}
```

## Performance Considerations

- **Sync Cost**: Aggregate intents are batched via `AgentSyncBus` (< 100 messages/frame for 1000 aggregates)
- **Update Frequency**: Aggregates update at 1 Hz vs 2-5 Hz for individuals (reduces overhead)
- **Determinism**: Aggregate stats collection is deterministic; intent evaluation is non-deterministic but biases deterministic goal evaluation

## Testing

1. Create test aggregates with known member sets
2. Verify aggregate intents propagate to member agents
3. Measure sync cost with 1000+ aggregates
4. Validate determinism across simulation restarts
5. Test aggregate goal distribution (60% Farm, 30% Defend, 10% Rest)

## Common Patterns

### Creating a Village

```csharp
// Create aggregate GUID
var villageGuid = AgentGuid.NewGuid();

// Assign villagers to village
foreach (var villagerEntity in villagerEntities)
{
    var membership = new AggregateMembership
    {
        AggregateGuid = villageGuid,
        Role = 0
    };
    EntityManager.AddComponent(villagerEntity, membership);
}

// AggregateBridgeSystem will automatically create AggregateEntity in AggregateECSWorld
```

### Querying Aggregate State

```csharp
var aggregateWorld = AggregateECSWorld.Instance;
foreach (var entity in aggregateWorld.World.GetEntities().With<AggregateEntity>().AsSet().GetEntities())
{
    var aggregate = aggregateWorld.World.Get<AggregateEntity>(entity);
    var intent = aggregateWorld.World.Get<AggregateIntent>(entity);
    
    Debug.Log($"Aggregate {aggregate.AggregateGuid}: Goal={intent.CurrentGoal}, Priority={intent.Priority}");
}
```

### Monitoring Aggregate Intents

```csharp
// In CognitiveSystem or custom system
var bus = AgentSyncBridgeCoordinator.GetBusFromDefaultWorld();
if (bus != null && bus.AggregateIntentQueueCount > 0)
{
    var intents = bus.DequeueAggregateIntentBatch();
    foreach (var intent in intents)
    {
        Debug.Log($"Aggregate {intent.AggregateGuid}: {intent.GoalType} (Priority: {intent.Priority})");
    }
}
```

## See Also

- [AggregateECSAPI.md](AggregateECSAPI.md) - API reference
- [MovementAuthoringGuide.md](MovementAuthoringGuide.md) - Agent movement setup
- `Runtime/AI/AggregateECS/` - Source code

