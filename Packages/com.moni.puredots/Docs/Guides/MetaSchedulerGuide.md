# Meta-Scheduler Guide

**Purpose**: Guide for using the meta-scheduler to dynamically reorder systems based on impact metrics for load balancing.

## Overview

The meta-scheduler measures system impact (CPU cost, changed entities, delta-impact) and automatically reorders systems when impact falls below thresholds, maintaining dependency constraints.

## Core Components

### SystemDependencyGraph

```csharp
public struct SystemDependencyGraph : IComponentData
{
    public uint Version;
}
```

Tracks system dependencies and execution order constraints. Dependency edges stored in `SystemDependencyEdge` buffer.

### SystemImpactMetrics

```csharp
public struct SystemImpactMetrics : IComponentData
{
    public float EnergyUse;          // CPU cost in ms
    public int ChangedEntityCount;   // Number of entities modified
    public float DeltaImpact;        // Impact per tick
    public uint LastUpdateTick;      // When metrics were last updated
    public int Priority;             // Current priority (higher = more important)
}
```

Tracks per-system impact metrics for scheduling decisions.

### SystemDependencyEdge

```csharp
public struct SystemDependencyEdge : IBufferElementData
{
    public FixedString64Bytes SourceSystem;
    public FixedString64Bytes TargetSystem;
    public byte DependencyType; // 0 = before, 1 = after, 2 = requires
}
```

Represents a dependency edge in the system graph.

## Usage Pattern

### Measuring System Impact

Systems automatically measure impact via `SystemSchedulerSystem`:

```csharp
[BurstCompile]
public partial struct MySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var startTime = System.Diagnostics.Stopwatch.GetTimestamp();
        int changedCount = 0;
        
        // ... system logic ...
        changedCount = ProcessEntities(ref state);
        
        var endTime = System.Diagnostics.Stopwatch.GetTimestamp();
        float cpuCost = (endTime - startTime) / (float)System.Diagnostics.Stopwatch.Frequency * 1000f;
        
        // Update impact metrics (handled by SystemSchedulerSystem)
    }
}
```

### Defining Dependencies

Dependencies are defined via `[UpdateBefore]` and `[UpdateAfter]` attributes:

```csharp
[UpdateInGroup(typeof(AISystemGroup))]
[UpdateAfter(typeof(PerceptionFusionSystem))] // Dependency
public partial struct MySystem : ISystem
{
    // SystemSchedulerSystem respects these dependencies when reordering
}
```

### Manual Priority Adjustment

```csharp
// Systems can adjust their own priority based on conditions
if (impact < threshold)
{
    // SystemSchedulerSystem will deprioritize this system
    // while maintaining dependency constraints
}
```

## Integration Points

- **SystemSchedulerSystem**: Runs last in `SimulationSystemGroup` to measure and reorder
- **SystemRegistry**: Profile-aware scheduling (default/headless/replay profiles)
- **SystemGroups**: Dependencies defined via `[UpdateBefore]`/`[UpdateAfter]` attributes

## Best Practices

1. **Define dependencies explicitly**: Use `[UpdateBefore]`/`[UpdateAfter]` attributes
2. **Measure impact accurately**: Track CPU cost and changed entity count
3. **Respect thresholds**: Systems with impact < threshold are deprioritized
4. **Maintain determinism**: Reordering preserves deterministic execution order

## Performance Impact

- **Load balancing**: Systems execute by relevance, not static order
- **CPU optimization**: Low-impact systems deprioritized automatically
- **Self-organizing**: System order adapts to workload

## References

- `PureDOTS/Packages/com.moni.puredots/Runtime/Config/SystemDependencyGraph.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Scheduling/SystemSchedulerSystem.cs`

