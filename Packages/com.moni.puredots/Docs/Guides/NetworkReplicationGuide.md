# Network Replication Guide

**Purpose**: Guide for multiplayer-ready determinism via event serialization.

## Overview

Serialize only input events and RNG seeds; re-simulate world identically on clients. Rewind system provides snapshot deltas exposed to net layer. Free multiplayer or replay support with zero rewrite.

## Core Components

### NetworkReplicationSystem

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
public partial struct NetworkReplicationSystem : ISystem
{
    // Serializes input events and RNG seeds
    // Exposes rewind snapshot deltas to net layer
    // Supports deterministic replay validation
}
```

Network replication system for multiplayer determinism.

### ReplicationEvent

```csharp
public struct ReplicationEvent : IBufferElementData
{
    public uint EventType;
    public Entity SourceEntity;
    public float3 Position;
    public uint TickNumber;
    public uint RNGSeed;
}
```

Replication event for network serialization.

### DeterministicReplaySystem

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct DeterministicReplaySystem : ISystem
{
    // Validates deterministic replay
    // Compares current state to recorded state
    // Detects non-deterministic differences
}
```

System for deterministic replay validation.

## Usage Pattern

### Serializing Input Events

```csharp
// Collect input events from all systems
var events = new NativeList<ReplicationEvent>(Allocator.Temp);

// Serialize events
foreach (var inputEvent in inputEvents)
{
    events.Add(new ReplicationEvent
    {
        EventType = inputEvent.Type,
        SourceEntity = inputEvent.Source,
        Position = inputEvent.Position,
        TickNumber = currentTick,
        RNGSeed = rngState.Seed
    });
}

// Send to network layer
NetworkLayer.SendEvents(events);
```

### Exposing Rewind Snapshots

```csharp
// Rewind system provides snapshot deltas
var snapshotDelta = RewindSystem.GetSnapshotDelta(fromTick, toTick);

// Expose to network layer
NetworkLayer.SendSnapshotDelta(snapshotDelta);
```

### Replaying on Client

```csharp
// Client receives events and seeds
var events = NetworkLayer.ReceiveEvents();
var rngSeed = NetworkLayer.ReceiveRNGSeed();

// Re-simulate world identically
ReplaySimulation(events, rngSeed);
```

### Validating Determinism

```csharp
// Compare current state to recorded state
var currentState = GetCurrentState();
var recordedState = GetRecordedState(tick);

if (!StatesMatch(currentState, recordedState))
{
    LogError("Non-deterministic difference detected at tick " + tick);
}
```

## Integration Points

- **RewindState**: Integrates with existing rewind system
- **Network Layer**: Exposes events and snapshots to network
- **Deterministic Replay**: Validates replay correctness

## Best Practices

1. **Serialize only inputs**: Events and RNG seeds, not full state
2. **Re-simulate on clients**: Clients simulate from events, not state
3. **Validate determinism**: Check replay matches original
4. **Expose snapshots**: Use rewind snapshots for network sync
5. **Zero rewrite**: Works with existing rewind system

## Performance Impact

- **Free multiplayer**: Deterministic replay enables multiplayer
- **Replay support**: Deterministic replay for debugging/analysis
- **Zero rewrite**: Works with existing rewind system
- **Efficient**: Only serializes inputs, not full state

## References

- `PureDOTS/Packages/com.moni.puredots/Runtime/Networking/NetworkReplicationSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Networking/ReplicationEvent.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Networking/DeterministicReplaySystem.cs`

