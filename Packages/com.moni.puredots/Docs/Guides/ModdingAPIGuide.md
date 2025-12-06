# Modding API Guide

## Overview

The modding API provides safe, read-only event pipes for mods to interact with the simulation. Mods publish data via events; they never mutate hot components directly.

## Modding Event Bus

### Publishing Events from Mods

```csharp
using PureDOTS.Runtime.Modding;

// Get modding event bus
var busEntity = SystemAPI.GetSingletonEntity<ModdingEventBus>();
var eventBuffer = SystemAPI.GetBuffer<ModdingEvent>(busEntity);

// Publish data update event
eventBuffer.Add(new ModdingEvent
{
    Type = ModdingEvent.EventType.DataUpdate,
    EventId = "MyMod_ResourceBoost",
    Data = "{\"resourceType\":\"Wood\",\"multiplier\":1.5}",
    Tick = currentTick,
    EventIndex = (uint)eventBuffer.Length
});
```

### Registering Catalog Entries

```csharp
// At boot time (InitializationSystemGroup)
var busEntity = SystemAPI.GetSingletonEntity<ModdingEventBus>();
var catalogBuffer = SystemAPI.GetBuffer<ModdingCatalogEntry>(busEntity);

catalogBuffer.Add(new ModdingCatalogEntry
{
    CatalogId = "ResourceTypes",
    EntryId = "MyMod_Wood",
    Data = "{\"name\":\"Modded Wood\",\"value\":10}"
});

// ModdingCatalogSystem converts to blobs at boot
```

## Reading Modding Events

### In Simulation Systems

```csharp
// Read modding events (read-only)
var busEntity = SystemAPI.GetSingletonEntity<ModdingEventBus>();
var eventBuffer = SystemAPI.GetBuffer<ModdingEvent>(busEntity);

foreach (var evt in eventBuffer)
{
    if (evt.Type == ModdingEvent.EventType.DataUpdate && 
        evt.EventId.Equals("MyMod_ResourceBoost"))
    {
        // Process mod event (read-only)
        // Never mutate hot components directly
    }
}
```

## Catalog Integration

Catalogs are read-only blobs at runtime. Mods register data at boot; systems read from blobs:

```csharp
// Mods register at boot (ModdingCatalogSystem processes)
// Systems read from blob assets (read-only)
ref var catalog = ref resourceCatalogBlob.Value;
var resourceType = catalog.GetResourceType("MyMod_Wood");
```

## Best Practices

1. **Read-only access** - Mods never mutate hot components
2. **Event-based** - Use `ModdingEvent` buffer for communication
3. **Boot-time registration** - Catalogs registered at initialization
4. **Blob conversion** - `ModdingCatalogSystem` converts to blobs for performance

## Integration Checklist

When creating moddable systems:

- [ ] Define event types in `ModdingEvent.EventType`
- [ ] Publish events to `ModdingEventBus` buffer
- [ ] Register catalog entries at boot time
- [ ] Read from blob assets at runtime (never mutate)
- [ ] Never allow mods to access hot components directly

