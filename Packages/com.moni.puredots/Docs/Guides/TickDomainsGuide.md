# Heterogeneous Tick Domains Guide

**Purpose**: Guide for using separate tick rates per subsystem (Physics 60Hz, Cognitive 0.5-5Hz, Economy 0.1Hz) for linear scaling.

## Overview

Each subsystem owns its own clock. Systems execute at different frequencies based on their domain, synchronized via integer tick ratios to preserve determinism.

## Core Components

### TickDomain Component

```csharp
public struct TickDomain : IComponentData
{
    public TickDomainType DomainType;
    public uint TickRatio; // Ratio relative to base tick (e.g., 120 for 0.5Hz when base is 60Hz)
    public uint LastTick;
    public uint NextTick;
}
```

Defines tick domain and ratio for an entity or system group.

### TickDomainType

```csharp
public enum TickDomainType : byte
{
    Physics = 0,      // 60 Hz (1:1 ratio)
    Cognitive = 1,    // 0.5-5 Hz (adaptive)
    Economy = 2,      // 0.1 Hz
    Custom = 255
}
```

Domain types for different subsystems.

### TickDomainCoordinatorSystem

Manages domain execution and synchronization:

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial struct TickDomainCoordinatorSystem : ISystem
{
    // Coordinates tick domains
    // Determines which domains execute this tick
    // Preserves determinism via integer ratios
}
```

## System Groups

### CognitiveSystemGroup

```csharp
[UpdateInGroup(typeof(GameplaySystemGroup))]
public partial class CognitiveSystemGroup : ComponentSystemGroup { }
```

Runs at adaptive 0.5-5Hz for cognitive AI processing.

### EconomySystemGroup

```csharp
[UpdateInGroup(typeof(GameplaySystemGroup))]
public partial class EconomySystemGroup : ComponentSystemGroup { }
```

Runs at 0.1Hz for economy/ecology updates.

## Usage Pattern

### Adding System to Domain

```csharp
[UpdateInGroup(typeof(CognitiveSystemGroup))] // Runs at 0.5-5Hz
public partial struct MyCognitiveSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Only executes when CognitiveSystemGroup ticks
        // Coordinated by TickDomainCoordinatorSystem
    }
}
```

### Checking Domain Tick

```csharp
public void OnUpdate(ref SystemState state)
{
    var tickDomain = SystemAPI.GetComponent<TickDomain>(entity);
    
    if (tickDomain.NextTick <= currentTick)
    {
        // Domain should execute this tick
        ProcessDomainUpdate();
        
        // Update next tick
        tickDomain.NextTick = currentTick + tickDomain.TickRatio;
    }
}
```

### Deterministic Ratios

Tick ratios are integers to preserve determinism:

```csharp
// Physics: 60Hz (ratio 1:1)
TickRatio = 1;

// Cognitive: 0.5Hz when base is 60Hz (ratio 120:1)
TickRatio = 120;

// Economy: 0.1Hz when base is 60Hz (ratio 600:1)
TickRatio = 600;
```

## Integration Points

- **TickDomainCoordinatorSystem**: Manages domain execution
- **SystemGroups**: `CognitiveSystemGroup`, `EconomySystemGroup` for domain-specific systems
- **TimeState**: Extended with domain-specific tick counters

## Best Practices

1. **Use appropriate domains**: Match system frequency to domain (cognitive = low freq, physics = high freq)
2. **Integer ratios**: Use integer tick ratios for determinism
3. **Coordinate execution**: Let `TickDomainCoordinatorSystem` manage domain ticks
4. **Preserve determinism**: Domain execution is deterministic and rewind-safe

## Performance Impact

- **Linear scaling**: Small tick domains handle 90% of total entities off-frame
- **CPU reduction**: Low-frequency domains reduce CPU load dramatically
- **Deterministic**: Integer ratios preserve deterministic execution

## References

- `PureDOTS/Packages/com.moni.puredots/Runtime/Components/TickDomain.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Time/TickDomainCoordinatorSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/SystemGroups.cs`

