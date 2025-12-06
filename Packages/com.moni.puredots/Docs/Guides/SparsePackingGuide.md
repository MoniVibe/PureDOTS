# Sparse Component Packing Guide

**Purpose**: Guide for using `IEnableableComponent` to disable unused data for 15-30% chunk compression.

## Overview

Use `IEnableableComponent` to disable unused components on entities. Disabled components remain in the same archetype but are skipped during iteration, providing zero re-allocation with 15-30% chunk compression.

## Core Components

### ComponentEnablementSystem

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ComponentEnablementSystem : ISystem
{
    // Manages component enablement for sparse packing
    // Disables SensorSpec on blind entities
    // Disables CombatStats on non-combatants
    // Disables NavigationTarget when idle
}
```

System managing component enable/disable logic.

## Usage Pattern

### Making Components Enableable

```csharp
public struct SensorSpec : IComponentData, IEnableableComponent
{
    public SensorType Type;
    public float Range;
    // ... other fields
}
```

Implement `IEnableableComponent` interface.

### Disabling Components

```csharp
// Disable SensorSpec on blind entities
if (entity.IsBlind)
{
    state.SetComponentEnabled<SensorSpec>(entity, false);
}

// Disable CombatStats on non-combatants
if (!entity.IsInCombat)
{
    state.SetComponentEnabled<CombatStats>(entity, false);
}

// Disable NavigationTarget when idle
if (entity.IsIdle)
{
    state.SetComponentEnabled<NavigationTarget>(entity, false);
}
```

### Querying Enabled Components

```csharp
// Query only processes entities with enabled components
foreach (var (sensor, entity) in SystemAPI.Query<RefRO<SensorSpec>>()
    .WithEntityAccess())
{
    // Only processes entities where SensorSpec is enabled
    // Disabled components are automatically skipped
}
```

### Checking Enabled State

```csharp
if (SystemAPI.IsComponentEnabled<SensorSpec>(entity))
{
    // Component is enabled
    ProcessSensor(entity);
}
```

## Examples

### SensorSpec Enablement

```csharp
// Disable on blind entities
if (villager.IsBlind)
{
    state.SetComponentEnabled<SensorSpec>(villagerEntity, false);
}

// Enable when vision restored
if (villager.VisionRestored)
{
    state.SetComponentEnabled<SensorSpec>(villagerEntity, true);
}
```

### CombatStats Enablement

```csharp
// Disable on non-combatants
if (!combatStats.IsInCombat && combatStats.CurrentHealth > 0)
{
    state.SetComponentEnabled<CombatStats>(entity, false);
}

// Enable when entering combat
if (combatStats.IsInCombat)
{
    state.SetComponentEnabled<CombatStats>(entity, true);
}
```

## Best Practices

1. **Use for optional components**: Components that aren't always needed
2. **Disable when unused**: Disable components when not needed
3. **Enable when needed**: Re-enable when component becomes relevant
4. **Zero re-allocation**: Entities stay in same archetype
5. **Measure compression**: Track chunk compression gains via telemetry

## Performance Impact

- **15-30% chunk compression**: Disabled components reduce chunk size
- **Zero re-allocation**: Entities remain in same archetype
- **Automatic skipping**: Disabled components skipped in iteration
- **Memory savings**: Significant memory reduction at scale

## References

- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Packing/ComponentEnablementSystem.cs`

