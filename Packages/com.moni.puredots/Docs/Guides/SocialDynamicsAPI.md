# Social Dynamics API Reference

**Location**: `PureDOTS/Packages/com.moni.puredots/Runtime/AI/Social/`  
**Purpose**: API reference for large-scale social and cooperative dynamics across 3-layer ECS architecture.

## Overview

Social dynamics system implements message-based communication, trust networks, and cultural propagation for emergent group behaviors. Based on academic research from Helbing (2012), Hoey et al. (2018), Pagliuca et al. (2023), and Kozlowski et al. (2016).

## Architecture Layers

- **Micro (Individual)**: Body ECS - personal motives, biases, immediate reactions (60 Hz)
- **Meso (Group)**: Aggregate ECS - cooperation, trust networks, shared goals (1-5 Hz)
- **Macro (Cultural)**: Mind ECS - doctrines, ideology, intergroup relations (0.2 Hz)

## Components

### SocialMessage (IBufferElementData)

Social message for communication and cooperation protocols.

```csharp
public struct SocialMessage : IBufferElementData
{
    public SocialMessageType Type;      // Offer, Request, Threat, Praise, Inquiry, etc.
    public AgentGuid SenderGuid;
    public AgentGuid ReceiverGuid;
    public float Urgency;               // Priority weight (0-1)
    public float Payload;               // Trade value, trust delta, etc.
    public ushort Flags;                // Message flags (urgent, broadcast, etc.)
    public uint TickNumber;             // Timestamp from simulation tick
    public float3 ContextPosition;      // Optional spatial context
}
```

**Usage**:
```csharp
// Add message to buffer
var message = new SocialMessage
{
    Type = SocialMessageType.Offer,
    SenderGuid = agentGuid,
    ReceiverGuid = targetGuid,
    Urgency = 0.8f,
    Payload = 0.5f, // Trade value
    Flags = SocialMessageFlags.Urgent,
    TickNumber = tickNumber
};
socialMessageBuffer.Add(message);
```

### SocialKnowledge (IComponentData)

Aggregates relationship data and cooperation preferences.

```csharp
public struct SocialKnowledge : IComponentData
{
    public float BaseTrust;             // Base trust level (0-1)
    public float BaseReputation;        // Base reputation (0-1)
    public float CooperationBias;       // General cooperation bias (-1 to 1)
    public float LearningRate;          // Rate of trust/reputation updates (0-1)
    public uint LastUpdateTick;         // Last time social knowledge was updated
}
```

### SocialRelationship (IBufferElementData)

Sparse relationship matrix entry for trust tracking.

```csharp
public struct SocialRelationship : IBufferElementData
{
    public AgentGuid OtherAgentGuid;
    public float Trust;                 // Trust toward this individual (0-1)
    public float Reputation;            // Aggregate perception (0-1)
    public float CooperationBias;       // Bias toward cooperation (-1 to 1)
    public uint LastInteractionTick;
    public float InteractionCount;
}
```

### GroupGoal (IComponentData)

Group goal component for aggregates.

```csharp
public struct GroupGoal : IComponentData
{
    public float CooperationWeight;     // Weight for cooperative actions (0-1)
    public float CompetitionWeight;     // Weight for competitive actions (0-1)
    public float ResourcePriority;      // Priority for resource acquisition (0-1)
    public float ThreatLevel;           // Perceived threat level (0-1)
    public float GroupCohesion;         // Group cohesion metric (0-1)
    public uint LastEvaluationTick;
}
```

### Motivation (IComponentData)

Motivation component for morale, hope, and social pressure.

```csharp
public struct Motivation : IComponentData
{
    public float Morale;                // Morale level (0-1)
    public float Hope;                  // Hope level (0-1)
    public float Pressure;              // External constraint/pressure (0-1)
    public float Courage;               // Courage stat (0-1)
    public uint LastUpdateTick;
}
```

### CulturalSignal (IBufferElementData)

Cultural signal for social learning and propagation.

```csharp
public struct CulturalSignal : IBufferElementData
{
    public ushort DoctrineId;           // ID of the cultural doctrine/strategy
    public float Strength;              // Signal strength (0-1)
    public float Decay;                 // Decay rate per tick (0-1)
    public AgentGuid SourceGuid;        // Source agent that broadcast the signal
    public uint BroadcastTick;          // Tick when signal was broadcast
}
```

## Message Types

### SocialMessageType Enum

```csharp
public enum SocialMessageType : ushort
{
    None = 0,
    Offer = 1,          // Trade offer, resource exchange proposal
    Request = 2,        // Request for help, resources, or cooperation
    Threat = 3,         // Territorial threat, conflict warning
    Praise = 4,         // Positive feedback, trust building
    Inquiry = 5,        // Information request, knowledge sharing inquiry
    CounterOffer = 6,   // Response to an offer
    Accept = 7,         // Acceptance of offer/request
    Reject = 8,         // Rejection of offer/request
    ShareKnowledge = 9, // Knowledge/research sharing
    Appeal = 10         // Appeal for help, diplomatic request
}
```

### SocialMessageFlags

```csharp
public static class SocialMessageFlags
{
    public const ushort Urgent = 1 << 0;        // High priority message
    public const ushort Broadcast = 1 << 1;      // Broadcast to nearby agents
    public const ushort RequiresResponse = 1 << 2; // Expects a response
    public const ushort TrustedSender = 1 << 3;  // Sender is trusted
}
```

## AgentSyncBus Extensions

### EnqueueSocialMessage

Enqueue a social message with delta compression.

```csharp
var bus = AgentSyncBridgeCoordinator.GetBusFromDefaultWorld();
var message = new SocialMessage
{
    Type = SocialMessageType.Offer,
    SenderGuid = senderGuid,
    ReceiverGuid = receiverGuid,
    Urgency = 0.8f,
    Payload = 0.5f,
    TickNumber = tickNumber
};
bus.EnqueueSocialMessage(message);
```

### DequeueSocialMessageBatch

Dequeue all pending social messages in batch.

```csharp
using var messageBatch = bus.DequeueSocialMessageBatch(Allocator.TempJob);
for (int i = 0; i < messageBatch.Length; i++)
{
    var message = messageBatch[i];
    // Process message
}
```

### EnqueueCulturalSignal

Enqueue a cultural signal for propagation.

```csharp
var signal = new CulturalSignal
{
    DoctrineId = doctrineId,
    Strength = 0.7f,
    Decay = 0.01f,
    SourceGuid = sourceGuid,
    BroadcastTick = tickNumber
};
bus.EnqueueCulturalSignal(signal);
```

### DequeueCulturalSignalBatch

Dequeue all pending cultural signals in batch.

```csharp
using var signalBatch = bus.DequeueCulturalSignalBatch(Allocator.TempJob);
for (int i = 0; i < signalBatch.Length; i++)
{
    var signal = signalBatch[i];
    // Process signal
}
```

## Utility Functions

### CooperationResolutionSystem

Burst-compiled utility functions for deterministic cooperation resolution.

```csharp
// Calculate mutual utility gain
bool shouldCooperate = CooperationResolutionSystem.CalculateMutualUtility(
    senderUtility,
    receiverUtility,
    cooperationThreshold,
    out float mutualGain);

// Update trust value
float newTrust = CooperationResolutionSystem.UpdateTrust(
    currentTrust,
    interactionOutcome,
    expectedOutcome,
    learningRate);

// Calculate indirect trust propagation
float indirectTrust = CooperationResolutionSystem.CalculateIndirectTrust(
    trustAB,
    trustBC,
    propagationFactor);

// Calculate effective cooperation
float effectiveCoop = CooperationResolutionSystem.CalculateEffectiveCooperation(
    groupCooperationWeight,
    personalityAltruism);

// Calculate combined utility
float combinedUtility = CooperationResolutionSystem.CalculateCombinedUtility(
    personalUtility,
    groupUtility,
    groupWeight);
```

## Systems

### Body ECS Systems (60 Hz, Burst-compiled)

- **CooperationSystemManaged**: Processes social messages, resolves interactions
- **SocialMessageRoutingSystemManaged**: Routes messages via spatial proximity
- **CulturalSignalSystemManaged**: Broadcasts cultural signals
- **MotivationSystem**: Updates morale, hope, pressure
- **TradeSystemManaged**: Handles trade offers/counter-offers
- **TerritoryClaimSystemManaged**: Handles territorial threats/appeals
- **KnowledgeSharingSystemManaged**: Handles knowledge sharing requests
- **SocialPerformanceProfilerManaged**: Collects telemetry metrics

### Aggregate ECS Systems (1-5 Hz, DefaultEcs)

- **TrustNetworkSystem**: Maintains sparse relationship matrices
- **ReputationAggregationSystem**: Aggregates trust to faction levels
- **GroupGoalSystem**: Manages cooperation/competition weights
- **GroupMoraleSystem**: Calculates group morale

### Mind ECS Systems (0.2-5 Hz, DefaultEcs)

- **SocialKnowledgeUpdateSystem**: Updates social knowledge from interactions
- **GoalBalancingSystem**: Maximizes PersonalUtility + WeightedGroupUtility
- **CulturalPropagationSystem**: Receives and propagates cultural signals
- **DoctrineEvolutionSystem**: Evolves efficient strategies

## Performance Considerations

- **Temporal Batching**: Systems update at 2-5 second intervals (not every tick)
- **Sparse Matrices**: Limited to 50-100 neighbors per agent
- **Event-Driven**: Only recalculates when messages exchanged
- **Chunk-Local**: Restricts message scan to same spatial cell
- **Target**: < 2 ms per 100k active entities

## Telemetry Metrics

Social dynamics systems expose the following telemetry metrics:

- `Social.Trust.Average`: Average trust level across agents
- `Social.Reputation.Average`: Average reputation level
- `Social.Morale.Average`: Average morale level
- `Social.Cooperation.Count`: Number of active cooperation relationships
- `Social.Message.Count`: Number of pending social messages

## See Also

- [SocialDynamicsIntegrationGuide.md](SocialDynamicsIntegrationGuide.md) - Integration guide with examples
- [AggregateECSIntegrationGuide.md](AggregateECSIntegrationGuide.md) - Aggregate ECS integration
- [MultiECS_Integration_Guide.md](MultiECS_Integration_Guide.md) - Multi-ECS architecture

