# Social Dynamics Quick Reference

**Quick reference for social dynamics systems integration.**

## Components Checklist

```csharp
// Required components for agents
EntityManager.AddComponent<SocialKnowledge>(entity);
EntityManager.AddComponent<Motivation>(entity);
EntityManager.AddBuffer<SocialRelationship>(entity);
EntityManager.AddBuffer<SocialMessage>(entity);
EntityManager.AddBuffer<CulturalSignal>(entity);
```

## Send Social Message

```csharp
var bus = AgentSyncBridgeCoordinator.GetBusFromDefaultWorld();
bus.EnqueueSocialMessage(new SocialMessage
{
    Type = SocialMessageType.Offer,
    SenderGuid = senderGuid,
    ReceiverGuid = receiverGuid,
    Urgency = 0.8f,
    Payload = 0.5f,
    TickNumber = tickNumber
});
```

## Query Trust

```csharp
foreach (var relationships in SystemAPI.Query<DynamicBuffer<SocialRelationship>>())
{
    for (int i = 0; i < relationships.Length; i++)
    {
        if (relationships[i].OtherAgentGuid.Equals(targetGuid))
        {
            float trust = relationships[i].Trust;
        }
    }
}
```

## Update Trust After Interaction

```csharp
float newTrust = CooperationResolutionSystem.UpdateTrust(
    currentTrust,
    interactionOutcome,  // 1.0 = success, 0.0 = failure
    expectedOutcome,
    learningRate);
```

## Calculate Combined Utility

```csharp
float utility = CooperationResolutionSystem.CalculateCombinedUtility(
    personalUtility,
    groupUtility,
    groupWeight);
```

## Broadcast Cultural Signal

```csharp
bus.EnqueueCulturalSignal(new CulturalSignal
{
    DoctrineId = doctrineId,
    Strength = 0.7f,
    Decay = 0.01f,
    SourceGuid = agentGuid,
    BroadcastTick = tickNumber
});
```

## System Update Rates

- **Body ECS**: 60 Hz (deterministic)
- **CooperationSystem**: 5 Hz (0.2s interval)
- **TrustNetworkSystem**: 0.5 Hz (2s interval)
- **Aggregate ECS**: 1 Hz
- **Mind ECS**: 2-5 Hz

## Performance Targets

- **Social Updates**: < 2 ms per 100k active entities
- **Max Neighbors**: 100 per agent (sparse matrices)
- **Sync Cost**: < 3 ms/frame

## Telemetry Metrics

- `Social.Trust.Average`
- `Social.Reputation.Average`
- `Social.Morale.Average`
- `Social.Cooperation.Count`
- `Social.Message.Count`

## See Also

- [SocialDynamicsAPI.md](SocialDynamicsAPI.md) - Complete API reference
- [SocialDynamicsIntegrationGuide.md](SocialDynamicsIntegrationGuide.md) - Integration guide

