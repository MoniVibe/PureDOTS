# Streaming World Cells Guide

**Purpose**: Guide for simulating only active cells, serializing inactive to disk for millions of entities.

## Overview

Divide the world into `SimulationCell` entities with child buffers of agents. Inactive cells serialize to disk; rehydrate deterministically when revisited. Enables tens of millions of total entities with thousands active.

## Core Components

### SimulationCell

```csharp
public struct SimulationCell : IComponentData
{
    public int2 CellCoordinates;
    public byte IsActive; // 0 = inactive (serialized), 1 = active (simulating)
    public uint LastActivationTick;
    public uint LastDeactivationTick;
}
```

Simulation cell entity with child buffers of agents.

### CellAgentBuffer

```csharp
public struct CellAgentBuffer : IBufferElementData
{
    public Entity AgentEntity;
}
```

Buffer of agent entities in a simulation cell.

### CellStreamingState

```csharp
public struct CellStreamingState : IComponentData
{
    public uint SerializationVersion;
    public uint LastSerializedTick;
    public byte IsSerialized; // 0 = in memory, 1 = serialized to disk
}
```

Cell streaming state for serialization/rehydration.

### CellAuthority

```csharp
public struct CellAuthority : IComponentData
{
    public byte OwnerPlayer; // 0 = unassigned/server, >0 = player ID
}
```

Authority tag for simulation cell ownership in multiplayer worlds.

## Usage Pattern

### Creating Cells

```csharp
// Create simulation cell
var cellEntity = state.EntityManager.CreateEntity();
state.EntityManager.AddComponentData(cellEntity, new SimulationCell
{
    CellCoordinates = new int2(x, y),
    IsActive = 1,
    LastActivationTick = currentTick
});

// Add agent buffer
var agentBuffer = state.EntityManager.AddBuffer<CellAgentBuffer>(cellEntity);
agentBuffer.Add(new CellAgentBuffer { AgentEntity = agentEntity });
```

### Activating Cells

```csharp
// CellStreamingSystem activates cells based on player/camera position
if (ShouldActivateCell(cellCoordinates))
{
    var cell = SystemAPI.GetComponent<SimulationCell>(cellEntity);
    
    if (cell.IsSerialized == 1)
    {
        // Rehydrate from disk
        RehydrateCell(cellEntity);
    }
    
    cell.IsActive = 1;
    cell.LastActivationTick = currentTick;
}
```

### Deactivating Cells

```csharp
// Deactivate cells outside active range
if (ShouldDeactivateCell(cellCoordinates))
{
    var cell = SystemAPI.GetComponent<SimulationCell>(cellEntity);
    
    // Serialize to disk
    SerializeCell(cellEntity);
    
    cell.IsActive = 0;
    cell.IsSerialized = 1;
    cell.LastDeactivationTick = currentTick;
}
```

### Deterministic Rehydration

```csharp
private void RehydrateCell(Entity cellEntity)
{
    // Use EntityScene API for deterministic rehydration
    // Preserves deterministic state across serialization
    // Integrates with RewindState for deterministic replay
}
```

## Integration Points

- **SpatialGridSystem**: Uses cell boundaries for spatial queries
- **EntityScene API**: Serialization/rehydration of chunks
- **RewindState**: Deterministic state preservation

## Best Practices

1. **Activate nearby cells**: Activate cells within player/camera range
2. **Deactivate distant cells**: Serialize cells outside active range
3. **Deterministic serialization**: Preserve deterministic state
4. **Cell boundaries**: Integrate with spatial grid for cell boundaries
5. **Multiplayer authority**: Use `CellAuthority` for multiplayer ownership

## Performance Impact

- **Tens of millions of entities**: Total entities supported
- **Thousands active**: Only active cells simulated
- **Linear scaling**: Performance scales with active cells, not total
- **Deterministic**: Serialization/rehydration preserves determinism

## References

- `PureDOTS/Packages/com.moni.puredots/Runtime/Components/SimulationCell.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Streaming/CellStreamingSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Streaming/CellSerializationSystem.cs`

