# Multi-ECS Integration Guide

**Last Updated**: 2025-12-10
**Purpose**: Cookbook for integrating PureDOTS three-pillar ECS into game projects

---

## Overview

This guide provides practical recipes for integrating PureDOTS' Body, Mind, and Aggregate ECS worlds into Space4X and Godgame projects. Each world serves different purposes and requires different integration patterns.

## Quick Reference

| World | Purpose | Integration Pattern | Frequency |
|-------|---------|-------------------|-----------|
| **Body** | Deterministic simulation | Direct component access | Per-frame |
| **Mind** | AI planning/thinking | Message-based sync | Planning intervals |
| **Aggregate** | Analysis/summaries | Read-only queries | Turn-based |

## World Bootstrap Integration

### 1. World Creation in Game Projects

**Location**: GameProject/Assets/Scripts/GameName/Bootstrap/

```csharp
using PureDOTS;
using Unity.Entities;

public class GameBootstrap : ICustomBootstrap
{
    public bool Initialize(string defaultWorldName)
    {
        // Create all three worlds
        var bodyWorld = PureDOTS.Bootstrap.CreateBodyWorld();
        var mindWorld = PureDOTS.Bootstrap.CreateMindWorld();
        var aggregateWorld = PureDOTS.Bootstrap.CreateAggregateWorld();

        // Set as default for game-specific systems
        World.DefaultGameObjectInjectionWorld = bodyWorld;

        return true;
    }
}
```

### 2. System Group Ordering

```csharp
// In your game's system ordering
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(PureDOTS.BodySimulationSystemGroup))]
public partial class GameSimulationSystem : ISystem { }

[UpdateInGroup(typeof(PureDOTS.MindSimulationSystemGroup))]
public partial class GameAISystem : ISystem { }
```

## Body ECS Integration (Simulation)

### Pattern: Direct Component Extension

**Use Case**: Adding game-specific simulation components to entities

```csharp
// In game project
public struct Space4XCarrierState : IComponentData
{
    public CarrierType Type;
    public FleetAssignment Assignment;
    public ModuleConfiguration Modules;
}

// Usage in game systems
[UpdateInGroup(typeof(BodySimulationSystemGroup))]
public partial struct CarrierMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, carrier, velocity) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<Space4XCarrierState>, RefRO<Velocity>>())
        {
            // Game-specific movement logic using PureDOTS components
            var deltaTime = SystemAPI.Time.DeltaTime;
            transform.ValueRW.Position += velocity.Value * deltaTime;
        }
    }
}
```

### Pattern: Registry Integration

**Use Case**: Connecting game entities to PureDOTS registries

```csharp
// Register game entities with PureDOTS registries
public partial struct CarrierRegistrationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var registry = SystemAPI.GetSingletonRW<CarrierRegistry>();

        foreach (var (entity, carrier) in
            SystemAPI.Query<Entity, RefRO<Space4XCarrierState>>().WithNone<RegisteredTag>())
        {
            // Add to PureDOTS registry
            registry.ValueRW.RegisterCarrier(entity, carrier.ValueRO);

            // Mark as registered
            state.EntityManager.AddComponent<RegisteredTag>(entity);
        }
    }
}
```

## Mind ECS Integration (Planning)

### Pattern: Message-Based AI

**Use Case**: Game-specific AI using PureDOTS planning infrastructure

```csharp
// Game-specific goal types
public enum Space4XGoal
{
    Mining,
    Combat,
    Exploration,
    Retreat
}

// Message from Body to Mind
public struct CarrierMindUpdate : ISyncMessage
{
    public Entity CarrierEntity;
    public CarrierState CurrentState;
    public ThreatAssessment Threats;
    public ResourceOpportunities Resources;
}

// AI system in Mind world
[UpdateInGroup(typeof(MindSimulationSystemGroup))]
public partial struct CarrierAISystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Process incoming Body messages
        var bus = SystemAPI.GetSingleton<AgentSyncBus>();
        var messages = bus.GetMessages<CarrierMindUpdate>();

        foreach (var message in messages)
        {
            // Evaluate goals using PureDOTS utility scoring
            var bestGoal = EvaluateGoals(message.CurrentState, message.Threats);

            // Send command back to Body
            var command = new CarrierCommand
            {
                Entity = message.CarrierEntity,
                Goal = bestGoal
            };
            bus.QueueMessage(command);
        }
    }
}
```

### Pattern: Planning Extension

**Use Case**: Custom planning logic using PureDOTS frameworks

```csharp
[UpdateInGroup(typeof(MindSimulationSystemGroup))]
public partial struct FleetPlanningSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Use PureDOTS planning utilities
        var planner = SystemAPI.GetSingleton<GamePlanner>();

        foreach (var fleet in SystemAPI.Query<RefRO<FleetComposition>>())
        {
            // Custom planning logic
            var plan = planner.CreatePlan(fleet.ValueRO);

            // Execute via message bus
            bus.QueueMessage(new FleetPlanExecution { Plan = plan });
        }
    }
}
```

## Aggregate ECS Integration (Analysis)

### Pattern: Strategic Analysis

**Use Case**: Empire-level analysis for strategy AI

```csharp
// Aggregate data structure
public struct EmpireAggregate : IComponentData
{
    public int TotalCarriers;
    public int TotalResources;
    public float MilitaryStrength;
    public ExpansionRate;
}

// Analysis system
[UpdateInGroup(typeof(AggregateSimulationSystemGroup))]
public partial struct EmpireAnalysisSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var empireQuery = SystemAPI.Query<RefRO<CarrierState>>();
        var aggregate = new EmpireAggregate();

        // Aggregate all carrier data
        foreach (var carrier in empireQuery)
        {
            aggregate.TotalCarriers++;
            aggregate.TotalResources += carrier.ValueRO.Resources;
            // ... more aggregation
        }

        // Store for strategic AI
        SystemAPI.SetSingleton(aggregate);
    }
}
```

### Pattern: Historical Tracking

**Use Case**: Long-term trend analysis

```csharp
public struct EmpireHistory : IBufferElementData
{
    public int Tick;
    public EmpireAggregate State;
}

[UpdateInGroup(typeof(AggregateSimulationSystemGroup))]
public partial struct HistoricalTrackingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var history = SystemAPI.GetSingletonBuffer<EmpireHistory>();
        var current = SystemAPI.GetSingleton<EmpireAggregate>();

        // Add current state to history
        history.Add(new EmpireHistory
        {
            Tick = SystemAPI.GetSingleton<TimeState>().Tick,
            State = current
        });

        // Trim old history if needed
        if (history.Length > MaxHistoryLength)
        {
            history.RemoveAt(0);
        }
    }
}
```

## Cross-World Data Flow

### Complete Integration Example

```csharp
// Game project structure
Assets/Scripts/Space4X/
├── Bootstrap/
│   ├── Space4XWorldBootstrap.cs     // World creation
│   └── SystemOrdering.cs           // Update ordering
├── Body/
│   ├── CarrierSimulationSystem.cs  // Movement, combat
│   └── FleetManagementSystem.cs    // Fleet coordination
├── Mind/
│   ├── CarrierAISystem.cs          // Individual AI
│   └── FleetAISystem.cs            // Group planning
└── Aggregate/
    ├── EmpireAnalysisSystem.cs     // Strategic analysis
    └── HistoricalTrackingSystem.cs // Trend analysis
```

### Data Flow Example: Carrier Mining

1. **Body World**: Carrier moves to mining site, executes mining
2. **Sync**: Body→Mind message with carrier state and opportunities
3. **Mind World**: Evaluates mining vs combat vs retreat goals
4. **Sync**: Mind→Body command to continue mining or switch goals
5. **Aggregate World**: Tracks mining output, predicts resource trends
6. **Sync**: Aggregate→Mind strategic guidance (expand mining operations)

## Testing Integration

### Unit Test Structure
```csharp
[TestFixture]
public class MultiECSIntegrationTests
{
    [Test]
    public void CarrierPlanning_RoundTrip()
    {
        // Setup all three worlds
        var worlds = CreateTestWorlds();

        // Inject test state into Body
        // Process Mind planning
        // Verify Body receives correct commands
        // Check Aggregate analysis
    }
}
```

### Performance Validation
```csharp
[Test]
public void WorldSync_PerformanceBudget()
{
    // Measure message processing time
    // Verify sub-millisecond latency
    // Check memory allocation
}
```

## Common Pitfalls

### 1. Direct World Access
```csharp
// ❌ WRONG - Direct cross-world access
var mindEntity = mindWorld.EntityManager.CreateEntity();
bodyWorld.EntityManager.GetComponentData<BodyState>(mindEntity);

// ✅ CORRECT - Message-based communication
var message = new BodyToMindSync { Data = bodyData };
bus.QueueMessage(message);
```

### 2. Shared Entity References
```csharp
// ❌ WRONG - Entity ID reuse across worlds
var bodyEntity = bodyWorld.EntityManager.CreateEntity();
// Assuming same ID works in mind world

// ✅ CORRECT - Entity mapping or message-based
var message = new EntityMessage { BodyEntity = bodyEntity, Data = data };
bus.QueueMessage(message);
```

### 3. Non-Deterministic Mind Logic
```csharp
// ❌ WRONG - Random numbers in Mind systems
var decision = Random.Range(0, 100);

// ✅ CORRECT - Deterministic evaluation
var decision = EvaluateDeterministically(state, context);
```

## Performance Optimization

### Message Batching
```csharp
// Batch similar messages
var batch = new BodyToMindBatch();
batch.AddCarrierUpdate(carrier1);
batch.AddCarrierUpdate(carrier2);
bus.QueueMessage(batch);
```

### Frequency Management
```csharp
// Don't run expensive analysis every frame
[UpdateInGroup(typeof(AggregateSimulationSystemGroup))]
public partial struct ExpensiveAnalysisSystem : ISystem
{
    private int frameCount;

    public void OnUpdate(ref SystemState state)
    {
        frameCount++;
        if (frameCount % 60 != 0) return; // Every second at 60fps

        // Expensive analysis here
    }
}
```

## Migration from Single ECS

### Phase 1: Extract Simulation (Body)
1. Identify pure simulation systems
2. Move to `BodySimulationSystemGroup`
3. Ensure deterministic behavior

### Phase 2: Add Planning (Mind)
1. Create message types for AI needs
2. Implement planning systems in Mind world
3. Connect via AgentSyncBus

### Phase 3: Add Analysis (Aggregate)
1. Create aggregation systems
2. Implement historical tracking
3. Connect strategic AI

---

**See Also**:
- `ThreePillarECS_Architecture.md` - Architecture overview
- `AgentSyncBus_Specification.md` - Communication protocol
- `GameIntegrationGuide.md` - General integration patterns









