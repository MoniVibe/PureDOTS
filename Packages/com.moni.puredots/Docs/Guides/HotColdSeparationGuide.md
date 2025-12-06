# Hot/Cold Data Separation Guide

## Overview

Hot data (simulation-critical) and cold data (presentation/lore) are separated for cache efficiency and performance. Hot data lives in ECS chunks; cold data in companion entities or separate archetypes.

## Architecture Pattern

```
Hot Entity (Simulation)
├── VillagerNeeds (hot)
├── VillagerAIState (hot)
├── VillagerJob (hot)
└── PresentationCompanionRef → Cold Entity
    ├── VillagerPresentation (cold)
    └── VillagerLore (cold)
```

## Creating Cold Data Entities

### For Villagers

```csharp
using PureDOTS.Runtime.Components;

// Create companion entity for presentation data
var companionEntity = ecb.CreateEntity();
ecb.AddComponent(companionEntity, new VillagerPresentation
{
    DisplayName = "John",
    Tooltip = "Hardworking farmer",
    UIStateFlags = 0,
    LastUpdateTick = currentTick
});

ecb.AddComponent(companionEntity, new VillagerLore
{
    Biography = "Born in the village...",
    PersonalityTraits = "Hardworking, Friendly",
    Backstory = "Raised by..."
});

// Link from hot entity
ecb.AddComponent(villagerEntity, new PresentationCompanionRef
{
    CompanionEntity = companionEntity
});
```

### For Villages

```csharp
var companionEntity = ecb.CreateEntity();
ecb.AddComponent(companionEntity, new VillagePresentation
{
    DisplayName = "Greenwood",
    Tooltip = "Prosperous farming village",
    UIStateFlags = 0,
    LastUpdateTick = currentTick
});

ecb.AddComponent(companionEntity, new VillageLore
{
    History = "Founded in year 100...",
    Culture = "Agricultural focus",
    FlavorText = "Known for its wheat fields"
});
```

## Sim-to-Presentation Bridge

### Writing Messages from Simulation

```csharp
using PureDOTS.Systems;

// Get message stream entity
var streamEntity = SystemAPI.GetSingletonEntity<SimToPresentationMessageStream>();
var messageBuffer = SystemAPI.GetBuffer<SimToPresentationMessage>(streamEntity);

// Write state update
messageBuffer.Add(new SimToPresentationMessage
{
    Type = SimToPresentationMessage.MessageType.StateUpdate,
    SourceEntity = villagerEntity,
    Position = transform.Position,
    State = (byte)aiState.CurrentState,
    HealthPercent = needs.Health / needs.MaxHealth * 100f,
    HungerPercent = needs.HungerFloat,
    EnergyPercent = needs.EnergyFloat,
    Tick = currentTick
});
```

### Reading Messages in Presentation

```csharp
// In PresentationSystemGroup system
var streamEntity = SystemAPI.GetSingletonEntity<SimToPresentationMessageStream>();
var messageBuffer = SystemAPI.GetBuffer<SimToPresentationMessage>(streamEntity);

foreach (var message in messageBuffer)
{
    if (message.Tick < currentTick - 10) continue; // Skip old messages
    
    // Update presentation visuals based on message
    UpdateVisuals(message.SourceEntity, message.Position, message.State);
}
```

## Best Practices

1. **Never mutate hot components from presentation systems** - Use message buffers
2. **Keep hot archetypes slim** - Only simulation-critical data
3. **Use companion entities** - For presentation/lore that's rarely accessed
4. **Event-based sync** - Always use `SimToPresentationMessage` buffer, never direct component access

## Integration Checklist

When adding new entities:

- [ ] Identify hot vs cold data
- [ ] Create companion entity for cold data
- [ ] Add `PresentationCompanionRef` or `LoreCompanionRef` to hot entity
- [ ] Write to `SimToPresentationMessage` buffer for state updates
- [ ] Never access hot components directly from presentation systems

