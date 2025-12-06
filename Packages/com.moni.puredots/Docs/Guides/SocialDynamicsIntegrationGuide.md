# Social Dynamics Integration Guide

**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/AI/Social/`  
**Purpose**: Integration guide for using social dynamics systems in game projects.

## Overview

This guide explains how to integrate and use the social dynamics systems for large-scale cooperative and competitive behaviors. The system implements message-based communication, trust networks, and cultural propagation across the 3-layer ECS architecture.

## Quick Start

### 1. Add Social Components to Agents

Add social components to agent entities in Body ECS:

```csharp
// In a baker or system
EntityManager.AddComponent<SocialKnowledge>(entity);
EntityManager.AddComponent<Motivation>(entity);
EntityManager.AddBuffer<SocialRelationship>(entity);
EntityManager.AddBuffer<SocialMessage>(entity);
EntityManager.AddBuffer<CulturalSignal>(entity);

// Initialize with defaults
var knowledge = new SocialKnowledge
{
    BaseTrust = 0.5f,
    BaseReputation = 0.5f,
    CooperationBias = 0f,
    LearningRate = 0.1f,
    LastUpdateTick = 0
};
EntityManager.SetComponent(entity, knowledge);

var motivation = new Motivation
{
    Morale = 0.7f,
    Hope = 0.6f,
    Pressure = 0f,
    Courage = 0.5f,
    LastUpdateTick = 0
};
EntityManager.SetComponent(entity, motivation);
```

### 2. Send Social Messages

Send social messages via AgentSyncBus:

```csharp
var bus = AgentSyncBridgeCoordinator.GetBusFromDefaultWorld();
if (bus != null)
{
    var message = new SocialMessage
    {
        Type = SocialMessageType.Offer,
        SenderGuid = senderGuid,
        ReceiverGuid = receiverGuid,
        Urgency = 0.8f,
        Payload = 0.5f, // Trade value
        Flags = SocialMessageFlags.Urgent,
        TickNumber = tickNumber,
        ContextPosition = senderPosition
    };
    bus.EnqueueSocialMessage(message);
}
```

### 3. Process Social Messages

Messages are automatically processed by `CooperationSystemManaged`. To handle custom message types:

```csharp
[UpdateInGroup(typeof(GameplaySystemGroup))]
[UpdateAfter(typeof(CooperationSystemManaged))]
public partial struct CustomSocialMessageHandler : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var bus = AgentSyncBridgeCoordinator.GetBusFromDefaultWorld();
        if (bus == null || bus.SocialMessageQueueCount == 0)
            return;

        using var messageBatch = bus.DequeueSocialMessageBatch(Allocator.TempJob);
        
        foreach (var message in messageBatch)
        {
            if (message.Type == SocialMessageType.CustomType)
            {
                // Handle custom message type
            }
        }
    }
}
```

### 4. Query Trust and Reputation

Query trust and reputation from SocialRelationship buffers:

```csharp
foreach (var (knowledge, relationships) in SystemAPI.Query<RefRO<SocialKnowledge>, DynamicBuffer<SocialRelationship>>())
{
    for (int i = 0; i < relationships.Length; i++)
    {
        var relationship = relationships[i];
        if (relationship.OtherAgentGuid.Equals(targetGuid))
        {
            float trust = relationship.Trust;
            float reputation = relationship.Reputation;
            // Use trust/reputation for decision making
        }
    }
}
```

### 5. Update Trust from Interactions

Update trust after successful/failed interactions:

```csharp
// After a successful cooperation
var interactionOutcome = 1.0f; // Success
var expectedOutcome = 0.6f; // Expected success rate
var learningRate = knowledge.ValueRO.LearningRate;

var newTrust = CooperationResolutionSystem.UpdateTrust(
    relationship.Trust,
    interactionOutcome,
    expectedOutcome,
    learningRate);

relationship.Trust = newTrust;
relationships[i] = relationship;
```

## Integration Patterns

### Pattern 1: Trade System Integration

Integrate trade system with social dynamics:

```csharp
[UpdateInGroup(typeof(GameplaySystemGroup))]
[UpdateAfter(typeof(TradeSystemManaged))]
public partial struct TradeSocialIntegration : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // After successful trade, update trust
        foreach (var (knowledge, relationships) in SystemAPI.Query<RefRO<SocialKnowledge>, DynamicBuffer<SocialRelationship>>())
        {
            // Find trade partner relationship
            // Update trust based on trade outcome
        }
    }
}
```

### Pattern 2: Group Goal Integration

Use group goals to bias individual agent decisions:

```csharp
// In Mind ECS system
var aggregateIntents = bus.DequeueAggregateIntentBatch();
foreach (var intent in aggregateIntents)
{
    // Get agent's personal utility
    float personalUtility = CalculatePersonalUtility(agent);
    
    // Get group utility from aggregate intent
    float groupUtility = intent.Priority;
    float groupWeight = 0.3f;
    
    // Calculate combined utility
    float combinedUtility = CooperationResolutionSystem.CalculateCombinedUtility(
        personalUtility,
        groupUtility,
        groupWeight);
    
    // Apply cooperation/competition weights
    float effectiveCoop = CooperationResolutionSystem.CalculateEffectiveCooperation(
        intent.CooperationWeight,
        personality.Altruism);
    
    // Use combined utility and effective cooperation for goal selection
}
```

### Pattern 3: Cultural Propagation Integration

Broadcast cultural signals on successful cooperation:

```csharp
// After successful cooperation
var bus = AgentSyncBridgeCoordinator.GetBusFromDefaultWorld();
if (bus != null)
{
    var signal = new CulturalSignal
    {
        DoctrineId = successfulDoctrineId,
        Strength = 0.7f,
        Decay = 0.01f,
        SourceGuid = agentGuid,
        BroadcastTick = tickNumber
    };
    bus.EnqueueCulturalSignal(signal);
}
```

### Pattern 4: Morale Integration

Use morale to influence agent behavior:

```csharp
foreach (var motivation in SystemAPI.Query<RefRO<Motivation>>())
{
    // Low morale -> reduce activity
    if (motivation.ValueRO.Morale < 0.3f)
    {
        // Reduce agent activity or trigger rest behavior
    }
    
    // High pressure + high courage -> potential revolt
    if (motivation.ValueRO.Pressure > 0.7f && motivation.ValueRO.Courage > 0.8f)
    {
        // Trigger revolt behavior
    }
}
```

## System Ordering

Social dynamics systems are ordered as follows:

```
GameplaySystemGroup
  ├── VillagerSystemGroup (updates agent stats)
  ├── CooperationSystemManaged (processes social messages)
  ├── SocialMessageRoutingSystemManaged (routes messages)
  ├── CulturalSignalSystemManaged (broadcasts signals)
  ├── MotivationSystem (updates morale)
  ├── TradeSystemManaged (handles trades)
  ├── TerritoryClaimSystemManaged (handles territory)
  ├── KnowledgeSharingSystemManaged (handles knowledge)
  └── SocialPerformanceProfilerManaged (telemetry)
```

## Performance Optimization

### Temporal Batching

Systems use temporal batching to reduce update frequency:

- **CooperationSystem**: 5 Hz (0.2s interval)
- **TrustNetworkSystem**: 0.5 Hz (2s interval)
- **ReputationAggregationSystem**: 0.2 Hz (5s interval)
- **GroupGoalSystem**: 1 Hz (1s interval)

### Sparse Matrices

Trust networks are limited to 100 neighbors per agent:

```csharp
// In TrustNetworkSystem
trustNetwork.PruneToNearestNeighbors(100);
```

### Event-Driven Updates

Only update trust when interactions occur:

```csharp
// Only update trust after message processing
if (interactionOccurred)
{
    UpdateTrustFromInteraction(...);
}
```

## Common Use Cases

### Use Case 1: Villager Cooperation

Villagers cooperate on resource gathering:

```csharp
// Send cooperation request
var message = new SocialMessage
{
    Type = SocialMessageType.Request,
    SenderGuid = villagerGuid,
    ReceiverGuid = nearbyVillagerGuid,
    Urgency = 0.6f,
    Payload = 0.4f, // Resource sharing value
    TickNumber = tickNumber
};
bus.EnqueueSocialMessage(message);

// CooperationSystem will process and update trust
```

### Use Case 2: Faction Reputation

Track reputation between factions:

```csharp
// In Aggregate ECS
var trustNetwork = World.Get<TrustNetwork>(aggregateEntity);
var factionReputation = trustNetwork.GetReputation(factionGuid);

// Use reputation for diplomatic decisions
if (factionReputation > 0.7f)
{
    // Friendly faction - allow trade
}
else if (factionReputation < 0.3f)
{
    // Hostile faction - restrict access
}
```

### Use Case 3: Cultural Evolution

Cultural strategies evolve based on success:

```csharp
// Successful strategy broadcasts signal
var signal = new CulturalSignal
{
    DoctrineId = successfulStrategyId,
    Strength = 0.8f,
    Decay = 0.01f,
    SourceGuid = agentGuid,
    BroadcastTick = tickNumber
};
bus.EnqueueCulturalSignal(signal);

// CulturalPropagationSystem receives and updates doctrine weights
```

## Troubleshooting

### Messages Not Processing

1. Check `AgentSyncBridgeCoordinator` is initialized
2. Verify `AgentSyncState` singleton exists
3. Check system ordering (must run after `VillagerSystemGroup`)

### Trust Not Updating

1. Verify `SocialKnowledge` component exists on agents
2. Check `LearningRate` is > 0
3. Ensure interactions are generating outcomes

### Performance Issues

1. Reduce update frequency (increase `UpdateInterval`)
2. Limit relationship tracking (reduce `MaxNeighborsPerAgent`)
3. Use spatial filtering for message routing

## Testing

Run social dynamics tests:

```bash
# Unit tests
Unity -batchmode -projectPath PureDOTS -runTests -testPlatform editmode -testFilter SocialDynamicsTests

# Scenario test
Unity -batchmode -projectPath PureDOTS -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScenarioFromArgs --scenario Scenario_SocialDynamics.json
```

## See Also

- [SocialDynamicsAPI.md](SocialDynamicsAPI.md) - Complete API reference
- [AggregateECSIntegrationGuide.md](AggregateECSIntegrationGuide.md) - Aggregate ECS integration
- [MultiECS_Integration_Guide.md](MultiECS_Integration_Guide.md) - Multi-ECS architecture patterns

