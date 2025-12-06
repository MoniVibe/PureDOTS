# Multiplayer-Ready Architecture Foundations Guide

## Overview

This guide documents the 16 multiplayer-ready architectural foundations implemented in PureDOTS. These foundations prepare the deterministic simulation for future lockstep networking without adding any networking code. When networking is added later, a replication layer will drop on top without refactoring.

**Key Principle**: All foundations are metadata/architecture only - no networking code, zero performance cost, fully Burst-compatible.

## Quick Reference

| Foundation | Location | Purpose |
|------------|----------|---------|
| InputCommand | `Runtime/Networking/InputCommandComponents.cs` | Store/process player inputs by tick |
| Snapshot Serialization | `Runtime/Networking/SnapshotSerialization.cs` | Serialize component state for snapshots |
| NetworkId | `Runtime/Networking/NetworkIdentityComponents.cs` | Entity identity and authority |
| PredictedSimulationSystemGroup | `Systems/SystemGroups.cs` | System group for rollback networking |
| Rollback Buffer | `Runtime/Networking/RollbackBuffer.cs` | Ring buffer for rewind-rollback |
| NetworkRNG | `Runtime/Networking/NetworkRNGComponents.cs` | Deterministic RNG per player/tick |
| InputDelayConfig | `Runtime/Networking/InputCommandComponents.cs` | Fixed command window |
| CellAuthority | `Runtime/Components/SimulationCell.cs` | Cell ownership for massive worlds |
| DeterministicSerializer | `Runtime/Networking/DeterministicSerializer.cs` | Canonical serialization spec |
| INetTransport | `Runtime/Networking/INetTransport.cs` | Transport-agnostic interface |
| SyncTelemetry | `Runtime/Telemetry/SyncTelemetryComponents.cs` | Sync debugging metrics |
| SpawnCommand | `Runtime/Networking/SpawnCommandComponents.cs` | Entity spawn replication |
| WorldHash | `Runtime/Networking/WorldHashComponents.cs` | Frame hash validation |
| Save Compatibility | `Runtime/Networking/SaveLoadCompatibility.md` | Save/load format spec |
| Lockstep Validation | `Assets/Tests/Scenarios/Scenario_LockstepValidation.json` | Validation scenario |

## Foundation Details

### 1. InputCommand Structure

**Purpose**: Store and process player input commands by tick, not player state.

**Usage**:
```csharp
// Add command to queue
var commandEntity = SystemAPI.GetSingletonEntity<InputCommandQueueTag>();
var commands = SystemAPI.GetBuffer<InputCommandBuffer>(commandEntity);
commands.Add(new InputCommandBuffer
{
    Tick = (int)currentTick,
    PlayerId = playerId,
    Payload = payload
});

// Commands are automatically processed by InputCommandProcessorSystem
// Systems consume commands by querying InputCommandBuffer for specific ticks
```

**Key Files**:
- `Runtime/Networking/InputCommandComponents.cs` - Component definitions
- `Systems/Networking/InputCommandProcessorSystem.cs` - Command processor

**Integration**: Commands feed from test scripts now; later serialize from network.

### 2. Snapshot Serialization

**Purpose**: Serialize component state for snapshots/deltas. Systems using RewindState can call these to record component diffs each tick.

**Usage**:
```csharp
// Implement ISnapshotSerializable on components
public struct MyComponent : IComponentData, ISnapshotSerializable
{
    public int Value;
    
    public void WriteSnapshot(ref SnapshotWriter writer)
    {
        writer.WriteInt(Value);
    }
    
    public void ReadSnapshot(ref SnapshotReader reader)
    {
        Value = reader.ReadInt();
    }
}

// Use in systems
var writer = new SnapshotWriter(1024, Allocator.Temp);
component.WriteSnapshot(ref writer);
var snapshot = writer.ToArray(Allocator.Persistent);
```

**Key Files**:
- `Runtime/Networking/SnapshotSerialization.cs` - Interface and writer/reader

**Integration**: Integrates with existing RewindState system for snapshot storage.

### 3. NetworkId & Authority

**Purpose**: Entity identity and authority model. No hardcoded "Server/Client" assumptions.

**Usage**:
```csharp
// Assign NetworkId to entity
entityManager.AddComponent<NetworkIdentityTag>(entity);
// NetworkIdentitySystem automatically assigns NetworkId

// Check authority in systems
var networkId = SystemAPI.GetComponent<NetworkId>(entity);
if (networkId.Authority == NetworkAuthority.Server)
{
    // Server-authoritative logic
}
```

**Authority Values**:
- `0` = Server (server-authoritative)
- `1` = Client (client-authoritative)
- `2` = Hybrid (shared authority)

**Key Files**:
- `Runtime/Networking/NetworkIdentityComponents.cs` - Components and constants
- `Systems/Networking/NetworkIdentitySystem.cs` - ID assignment

**Integration**: All persistent entities should have NetworkId for multiplayer.

### 4. PredictedSimulationSystemGroup

**Purpose**: System group for future rollback networking. Currently empty.

**Usage**:
```csharp
// Mark systems for prediction
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct MyPredictedSystem : ISystem
{
    // This system will replay buffered inputs ahead of authoritative tick
}
```

**Key Files**:
- `Systems/SystemGroups.cs` - System group definition

**Integration**: Place after SimulationSystemGroup in execution order.

### 5. Rollback Buffer

**Purpose**: Ring buffer utilities for rewind-rollback integration.

**Usage**:
```csharp
// Create rollback buffer
var buffer = new RollbackBuffer<Snapshot>(100, Allocator.Persistent);

// Push snapshots
buffer.Push(snapshot);

// Load snapshot at confirmed tick
RollbackUtilities.LoadSnapshotAtTick(ref state, confirmedTick);

// Catch up after server correction
RollbackUtilities.CatchUpToTick(ref state, confirmedTick, currentTick);
```

**Key Files**:
- `Runtime/Networking/RollbackBuffer.cs` - Ring buffer implementation
- `Runtime/Networking/RollbackUtilities.cs` - Helper functions

**Integration**: Uses existing RewindState system for snapshot loading.

### 6. Deterministic RNG Partitioning

**Purpose**: Seed RNG with (WorldSeed, PlayerId, Tick) for per-player streams.

**Usage**:
```csharp
// Get NetworkRNG component
var rng = SystemAPI.GetComponent<NetworkRNG>(entity);

// Create deterministic Random instance
var random = rng.CreateRandom();
float value = random.NextFloat();
```

**Key Files**:
- `Runtime/Networking/NetworkRNGComponents.cs` - RNG component
- `Systems/Networking/NetworkRNGSystem.cs` - RNG management

**Integration**: Use NetworkRNG instead of global Random for deterministic results.

### 7. Input Delay Window

**Purpose**: Fixed command window for latency compensation.

**Usage**:
```csharp
// Configure input delay (default: 2 ticks)
var configEntity = state.EntityManager.CreateEntity();
state.EntityManager.AddComponentData(configEntity, InputDelayConfig.Default);

// InputCommandProcessorSystem automatically respects delay
// Simulation uses InputCommand[tick - InputDelayTicks]
```

**Key Files**:
- `Runtime/Networking/InputCommandComponents.cs` - InputDelayConfig
- `Runtime/Core/InputDelayConfig.cs` - Alternative location

**Integration**: InputCommandProcessorSystem reads delay config automatically.

### 8. Cell Authority

**Purpose**: Cell ownership for massive multiplayer worlds.

**Usage**:
```csharp
// Assign authority to simulation cell
var cellAuthority = new CellAuthority { OwnerPlayer = playerId };
entityManager.AddComponent(cellEntity, cellAuthority);

// Check cell ownership
var authority = SystemAPI.GetComponent<CellAuthority>(cellEntity);
if (authority.OwnerPlayer == myPlayerId)
{
    // Own this cell
}
```

**Key Files**:
- `Runtime/Components/SimulationCell.cs` - CellAuthority component

**Integration**: Add to spatial grid cells or region entities.

### 9. Deterministic Serializer

**Purpose**: Canonical serialization spec for networkable components.

**Usage**:
```csharp
// Serialize component
var writer = new SnapshotWriter(1024, Allocator.Temp);
DeterministicSerializer.SerializeComponent(ref writer, component);
var data = writer.ToArray(Allocator.Persistent);

// Deserialize component
var reader = new SnapshotReader(data);
if (DeterministicSerializer.DeserializeComponent(ref reader, out MyComponent component))
{
    // Use component
}
```

**Key Files**:
- `Runtime/Networking/DeterministicSerializer.cs` - Serialization utilities

**Integration**: Use for save/load and future network sync.

### 10. Transport Interface

**Purpose**: Transport-agnostic interface for network communication.

**Usage**:
```csharp
// Use LocalLoopbackTransport for testing
INetTransport transport = new LocalLoopbackTransport();

// Send data
byte* data = ...;
transport.Send(data, size, channel: 0);

// Receive data
if (transport.Receive(out byte* receivedData, out int receivedSize, channel: 0))
{
    // Process received data
}
```

**Key Files**:
- `Runtime/Networking/INetTransport.cs` - Interface
- `Runtime/Networking/LocalLoopbackTransport.cs` - Mock implementation

**Integration**: Later replace with ENet, UDP, or Relay transports.

### 11. Authority Documentation

**Purpose**: Documented authority-free reconciliation model.

**Key Files**:
- `Runtime/Networking/NetworkIdentityComponents.cs` - Comprehensive documentation

**Integration**: See NetworkId section above.

### 12. Sync Telemetry

**Purpose**: Telemetry hooks for sync debugging.

**Usage**:
```csharp
// SyncTelemetrySystem automatically computes metrics
// Access telemetry singleton
var syncTelemetry = SystemAPI.GetSingleton<SyncTelemetry>();
uint inputHash = syncTelemetry.InputHash;
uint worldCRC = syncTelemetry.WorldStateCRC;

// Access historical samples
var samples = SystemAPI.GetBuffer<SyncTelemetrySample>(telemetryEntity);
```

**Key Files**:
- `Runtime/Telemetry/SyncTelemetryComponents.cs` - Telemetry components
- `Systems/Telemetry/SyncTelemetrySystem.cs` - Telemetry computation

**Integration**: Automatically runs each tick, stores samples in buffer.

### 13. Spawn Commands

**Purpose**: Entity spawn replication readiness.

**Usage**:
```csharp
// Queue spawn command
var spawnEntity = SystemAPI.GetSingletonEntity<SpawnCommandQueueTag>();
var spawns = SystemAPI.GetBuffer<SpawnCommand>(spawnEntity);
spawns.Add(new SpawnCommand
{
    PrefabId = prefabId,
    Pos = position,
    Rot = rotation,
    OwnerPlayerId = playerId,
    SpawnTick = currentTick
});

// Process spawns in spawn system (integrate with existing spawn systems)
```

**Key Files**:
- `Runtime/Networking/SpawnCommandComponents.cs` - Spawn command structure

**Integration**: Integrate with existing Registry Infrastructure spawn system.

### 14. World Hash Validation

**Purpose**: Frame hash validation for desync detection.

**Usage**:
```csharp
// WorldHashSystem automatically computes hash each tick
// Access world hash singleton
var worldHash = SystemAPI.GetSingleton<WorldHash>();
uint crc = worldHash.CRC32;
uint tick = worldHash.Tick;

// Compare hashes between peers to detect divergence
if (localHash.CRC32 != remoteHash.CRC32)
{
    // Desync detected!
}
```

**Key Files**:
- `Runtime/Networking/WorldHashComponents.cs` - Hash component
- `Systems/Networking/WorldHashSystem.cs` - Hash computation

**Integration**: Automatically runs each tick, stores hash in singleton.

### 15. Save Compatibility

**Purpose**: Save/load format compatible with multiplayer snapshots.

**Usage**:
```csharp
// Use DeterministicSerializer for save/load
var writer = new SnapshotWriter(1024, Allocator.Temp);
DeterministicSerializer.SerializeComponent(ref writer, component);
var saveData = writer.ToArray(Allocator.Persistent);
// Write to file

// Load uses same format
var reader = new SnapshotReader(loadedData);
DeterministicSerializer.DeserializeComponent(ref reader, out ComponentType component);
```

**Key Files**:
- `Runtime/Networking/SaveLoadCompatibility.md` - Documentation

**Integration**: Use DeterministicSerializer for all save/load operations.

### 16. Lockstep Validation

**Purpose**: Stress-test scenario for lockstep validation.

**Usage**:
```csharp
// LockstepValidationSystem automatically runs validation
// Scenario defined in: Assets/Tests/Scenarios/Scenario_LockstepValidation.json
// Validates 10,000 ticks with two players feeding opposing inputs
// Verifies both worlds produce identical CRCs
```

**Key Files**:
- `Assets/Tests/Scenarios/Scenario_LockstepValidation.json` - Scenario definition
- `Systems/Networking/LockstepValidationSystem.cs` - Validation system

**Integration**: Run in CI to prove simulation layer is multiplayer-safe.

## Integration Patterns

### Adding Input Commands

```csharp
// In your input system
var commandEntity = SystemAPI.GetSingletonEntity<InputCommandQueueTag>();
var commands = SystemAPI.GetBuffer<InputCommandBuffer>(commandEntity);
commands.Add(new InputCommandBuffer
{
    Tick = (int)SystemAPI.GetSingleton<TickTimeState>().Tick,
    PlayerId = playerId,
    Payload = SerializeInput(inputData)
});
```

### Processing Input Commands

```csharp
// In your gameplay system
var commandEntity = SystemAPI.GetSingletonEntity<InputCommandQueueTag>();
var commands = SystemAPI.GetBuffer<InputCommandBuffer>(commandEntity);
var tickState = SystemAPI.GetSingleton<TickTimeState>();
int targetTick = (int)tickState.Tick;

// Check for input delay
if (SystemAPI.TryGetSingleton<InputDelayConfig>(out var delay))
{
    targetTick -= delay.InputDelayTicks;
}

// Process commands for target tick
for (int i = 0; i < commands.Length; i++)
{
    if (commands[i].Tick == targetTick)
    {
        ProcessCommand(commands[i]);
    }
}
```

### Making Components Networkable

```csharp
// 1. Add NetworkId to entity
entityManager.AddComponent<NetworkIdentityTag>(entity);
// NetworkIdentitySystem assigns NetworkId automatically

// 2. Implement ISnapshotSerializable if needed
public struct MyComponent : IComponentData, ISnapshotSerializable
{
    public int Value;
    
    public void WriteSnapshot(ref SnapshotWriter writer)
    {
        writer.WriteInt(Value);
    }
    
    public void ReadSnapshot(ref SnapshotReader reader)
    {
        Value = reader.ReadInt();
    }
}

// 3. Use NetworkRNG for randomness
var rng = SystemAPI.GetComponent<NetworkRNG>(entity);
var random = rng.CreateRandom();
```

### Spawning Entities for Multiplayer

```csharp
// Queue spawn command
var spawnEntity = SystemAPI.GetSingletonEntity<SpawnCommandQueueTag>();
var spawns = SystemAPI.GetBuffer<SpawnCommand>(spawnEntity);
spawns.Add(new SpawnCommand
{
    PrefabId = prefabId,
    Pos = position,
    Rot = rotation,
    OwnerPlayerId = playerId,
    SpawnTick = currentTick
});

// Process spawns (integrate with existing spawn system)
// Assign NetworkId automatically via NetworkIdentitySystem
```

## System Integration

### Existing Systems to Extend

- `RewindCoordinatorSystem` - Already integrates with rollback buffer
- `TimeTickSystem` - Input delay window respected automatically
- `TelemetryStream` - Extended with sync metrics
- Spawn systems - Use `SpawnCommand` structure
- Save/Load systems - Use `DeterministicSerializer`

### System Groups

- `PredictedSimulationSystemGroup` - Place predicted systems here
- Networking systems stay in `SimulationSystemGroup` for now

## Testing

### Unit Tests

- Test serialization determinism
- Test input command processing
- Test RNG partitioning

### Integration Tests

- Run `Scenario_LockstepValidation` scenario
- Verify no performance regression
- Validate deterministic simulation

## Future Networking Integration

When adding networking:

1. **Input Layer**: Serialize `InputCommandBuffer` over network
2. **Snapshot Layer**: Use `ISnapshotSerializable` for delta compression
3. **Authority Layer**: Use `NetworkId.Authority` for ownership
4. **Rollback Layer**: Use `RollbackBuffer` and `RollbackUtilities`
5. **Transport Layer**: Implement `INetTransport` with real networking
6. **Validation Layer**: Compare `WorldHash` between peers

All foundations are designed to drop networking code on top without refactoring simulation logic.

## Performance Notes

- All foundations are metadata/empty - zero performance cost
- Systems only run when components exist
- Burst-compatible throughout
- No allocations in hot paths (except where documented)

## See Also

- `Runtime/Networking/SaveLoadCompatibility.md` - Save/load format spec
- `TRI_PROJECT_BRIEFING.md` - Project overview
- `Docs/FoundationGuidelines.md` - Coding guidelines

