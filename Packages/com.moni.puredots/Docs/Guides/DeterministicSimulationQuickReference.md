# Deterministic Simulation Quick Reference

## Code Snippets for Common Tasks

### Random Number Generation
```csharp
// Entity-specific randomness
var rng = DeterministicRandom.CreateFromTickAndEntity(timeState.Tick, entity);
float value = rng.NextFloat();

// System-level randomness
var rng = DeterministicRandom.CreateFromTick(timeState.Tick);
```

### Time Deltas
```csharp
// Simulation systems (CORRECT)
var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
float deltaTime = tickTimeState.FixedDeltaTime;

// Presentation systems (CORRECT)
float deltaTime = SystemAPI.Time.DeltaTime;
```

### Fixed-Point Math
```csharp
long value = FixedPointMath.ToFixed(10.5f);
long result = FixedPointMath.Multiply(value, FixedPointMath.ToFixed(2.0f));
float floatResult = FixedPointMath.ToFloat(result);
```

### Periodic Updates
```csharp
// Add component
ecb.AddComponent(entity, PeriodicTickHelper.Create(10)); // Every 10 ticks

// Check in system
if (PeriodicTickHelper.ShouldUpdate(currentTick, ref periodic.ValueRW))
{
    // Update entity
}
```

### Change Filters
```csharp
var query = SystemAPI.QueryBuilder()
    .WithAll<VillagerNeeds>()
    .WithChangeFilter<VillagerNeeds>() // Only changed entities
    .Build();
```

### Hot/Cold Separation
```csharp
// Create cold companion
var companion = ecb.CreateEntity();
ecb.AddComponent(companion, new VillagerPresentation { DisplayName = "John" });
ecb.AddComponent(entity, new PresentationCompanionRef { CompanionEntity = companion });

// Write to message buffer
var buffer = SystemAPI.GetBuffer<SimToPresentationMessage>(streamEntity);
buffer.Add(new SimToPresentationMessage { Type = ..., SourceEntity = entity, ... });
```

### Power/Economy Zones
```csharp
// Assign entity to zone
ecb.AddComponent(entity, new EntityPower
{
    ProductionRate = FixedPointMath.ToFixed(10.0f),
    ConsumptionRate = FixedPointMath.ToFixed(5.0f),
    ZoneId = spatialCellId
});

// Read zone state
var zoneState = SystemAPI.GetComponent<PowerZoneState>(zoneEntity);
float netPower = FixedPointMath.ToFloat(zoneState.NetPower);
```

### LOD System
```csharp
// Add LOD component
ecb.AddComponent(entity, new LODComponent());

// Check LOD level
if (lod.ValueRO.LODLevel == 0) { /* High-fidelity */ }
else { /* Statistical simulation */ }
```

### System Metrics
```csharp
var metrics = SystemAPI.GetComponentRW<SystemMetrics>(metricsEntity);
metrics.ValueRW.TickCostMs = elapsedMs;
metrics.ValueRW.EntityCount = query.CalculateEntityCount();
```

### Modding Events
```csharp
var buffer = SystemAPI.GetBuffer<ModdingEvent>(busEntity);
buffer.Add(new ModdingEvent
{
    Type = ModdingEvent.EventType.DataUpdate,
    EventId = "MyMod_Event",
    Data = "...",
    Tick = currentTick
});
```

### Tick Hashing
```csharp
var hashBuffer = SystemAPI.GetBuffer<TickHashEntry>(hashEntity);
foreach (var entry in hashBuffer)
{
    if (entry.Tick == targetTick)
    {
        ulong hash = entry.Hash;
    }
}
```

## Common Patterns

### System OnUpdate Template
```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    var rewindState = SystemAPI.GetSingleton<RewindState>();
    if (rewindState.Mode != RewindMode.Record) return;

    var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
    var deltaTime = tickTimeState.FixedDeltaTime; // Use tick-time!
    
    // System logic here
}
```

### Entity Creation with Cold Data
```csharp
var entity = ecb.CreateEntity();
// Hot components
ecb.AddComponent(entity, new VillagerNeeds { ... });
ecb.AddComponent(entity, new VillagerAIState { ... });

// Cold companion
var companion = ecb.CreateEntity();
ecb.AddComponent(companion, new VillagerPresentation { ... });
ecb.AddComponent(entity, new PresentationCompanionRef { CompanionEntity = companion });
```

## File Locations Quick Reference

- **DeterministicRandom**: `Runtime/Core/DeterministicRandom.cs`
- **FixedPointMath**: `Runtime/Core/FixedPointMath.cs`
- **ColdDataComponents**: `Runtime/Components/ColdDataComponents.cs`
- **PeriodicTickComponent**: `Runtime/Components/PeriodicTickComponent.cs`
- **PowerZoneComponents**: `Runtime/Components/PowerZoneComponents.cs`
- **EconomyZoneComponents**: `Runtime/Components/EconomyZoneComponents.cs`
- **LODComponent**: `Runtime/Components/LODComponent.cs`
- **FlowfieldComponents**: `Runtime/Components/FlowfieldComponents.cs`

## See Full Guides

- [DeterministicSimulationArchitecture.md](DeterministicSimulationArchitecture.md) - Complete overview
- [DeterminismGuide.md](DeterminismGuide.md) - Deterministic patterns
- [HotColdSeparationGuide.md](HotColdSeparationGuide.md) - Hot/cold separation
- [PerformanceOptimizationGuide.md](PerformanceOptimizationGuide.md) - Performance patterns
- [ModdingAPIGuide.md](ModdingAPIGuide.md) - Modding API
- [SystemProfilingGuide.md](SystemProfilingGuide.md) - Profiling
- [DeterministicDebuggingGuide.md](DeterministicDebuggingGuide.md) - Debugging
- [FlowfieldPathfindingGuide.md](FlowfieldPathfindingGuide.md) - Pathfinding

