# Performance Optimization Guide

## Dirty Flags and Change Filters

### Using WithChangeFilter

Only update entities when their data actually changes:

```csharp
// System only runs when VillagerNeeds changes
var query = SystemAPI.QueryBuilder()
    .WithAll<VillagerNeeds>()
    .WithChangeFilter<VillagerNeeds>() // Only changed entities
    .Build();
```

### Periodic Tick Updates

For systems that don't need to update every tick:

```csharp
using PureDOTS.Runtime.Components;

// Add PeriodicTickComponent to entities
ecb.AddComponent(entity, PeriodicTickHelper.Create(10)); // Update every 10 ticks

// In system OnUpdate
foreach (var (periodic, entity) in SystemAPI.Query<RefRW<PeriodicTickComponent>>()
             .WithEntityAccess())
{
    if (PeriodicTickHelper.ShouldUpdate(currentTick, ref periodic.ValueRW))
    {
        // Update entity
    }
}
```

**Recommended Tick Cadences:**
- Needs: Every tick (1)
- AI Goals: Every 10 ticks
- Pathfinding: Every 5 ticks
- Economy: Every 60 ticks

## Hierarchical Aggregation

### Power Zones

```csharp
using PureDOTS.Runtime.Components;

// Assign entity to power zone
ecb.AddComponent(entity, new EntityPower
{
    ProductionRate = FixedPointMath.ToFixed(10.0f),
    ConsumptionRate = FixedPointMath.ToFixed(5.0f),
    ZoneId = spatialCellId // Derived from SpatialGridResidency
});

// PowerAggregationSystem automatically aggregates per zone
// Read zone state:
var zoneState = SystemAPI.GetComponent<PowerZoneState>(zoneEntity);
float netPower = FixedPointMath.ToFloat(zoneState.NetPower);
```

### Economy Zones

```csharp
ecb.AddComponent(entity, new EntityEconomy
{
    ProductionRates = new FixedList32Bytes<long>
    {
        FixedPointMath.ToFixed(100.0f), // Resource type 0
        FixedPointMath.ToFixed(50.0f)   // Resource type 1
    },
    ConsumptionRates = new FixedList32Bytes<long>
    {
        FixedPointMath.ToFixed(80.0f),
        FixedPointMath.ToFixed(30.0f)
    },
    ZoneId = spatialCellId
});

// EconomyAggregationSystem computes trade balance per zone
var zoneState = SystemAPI.GetComponent<EconomyZoneState>(zoneEntity);
long tradeBalance = zoneState.TradeBalancePerType[0]; // Production - Consumption
```

## Simulation LOD

### Using LODComponent

```csharp
using PureDOTS.Runtime.Components;

// Add LOD component to entity
ecb.AddComponent(entity, new LODComponent
{
    LODLevel = 0, // Will be auto-assigned by SimulationLODSystem
    DistanceToCamera = 0f,
    UpdateStride = 1,
    LastUpdateTick = 0
});

// SimulationLODSystem automatically assigns LOD based on distance
// In your system, check LOD level:
foreach (var (lod, entity) in SystemAPI.Query<RefRO<LODComponent>>()
             .WithEntityAccess())
{
    if (lod.ValueRO.LODLevel == 0)
    {
        // High-fidelity simulation
    }
    else if (lod.ValueRO.LODLevel >= 2)
    {
        // Statistical simulation (simplified)
    }
}
```

## Integration Checklist

When optimizing systems:

- [ ] Add `WithChangeFilter<T>` to queries where appropriate
- [ ] Use `PeriodicTickComponent` for non-critical updates
- [ ] Assign entities to zones for hierarchical aggregation
- [ ] Add `LODComponent` to entities for distance-based LOD
- [ ] Check LOD level before expensive operations

