# Flowfield Pathfinding Guide

## Overview

Flowfields provide efficient pathfinding by generating fields per zone (not per entity). Results are cached in `PathCacheBlob` for performance.

## Using Flowfields

### Reading Flowfield Data

```csharp
using PureDOTS.Runtime.Components;

// Get flowfield for zone
var zoneEntity = GetZoneEntity(spatialCellId);
var flowfieldGrid = SystemAPI.GetSharedComponent<FlowfieldGrid>(zoneEntity);
var flowfieldBuffer = SystemAPI.GetBuffer<FlowfieldCell>(zoneEntity);

// Find flow vector for current cell
foreach (var cell in flowfieldBuffer)
{
    if (cell.CellId == currentCellId)
    {
        float3 direction = cell.Direction;
        float strength = cell.Strength;
        // Use for steering
    }
}
```

### Path Cache Blob

```csharp
var pathCache = SystemAPI.GetComponent<PathCacheBlob>(zoneEntity);
ref var cacheData = ref pathCache.CacheBlob.Value;

// Access cached flow vectors
for (int i = 0; i < cacheData.FlowVectors.Length; i++)
{
    float3 flowVector = cacheData.FlowVectors[i];
    float strength = cacheData.FlowStrengths[i];
}
```

## Flowfield Generation

`FlowfieldGenerationSystem` automatically generates flowfields:
- Every 10 ticks (configurable)
- On spatial topology changes (`SpatialGridState.Version` changes)
- Per zone (not per entity)

## Integration with AI Steering

```csharp
// In AISteeringSystem
var flowfield = GetFlowfieldForZone(zoneId);
var flowVector = flowfield.GetFlowVector(currentCellId);

// Use flow vector for steering
var desiredVelocity = flowVector * maxSpeed;
```

## Integration Checklist

When using pathfinding:

- [ ] Use flowfields instead of per-entity pathfinding
- [ ] Read from `FlowfieldCell` buffer or `PathCacheBlob`
- [ ] Check `FlowfieldGrid.Version` for cache validity
- [ ] Update steering based on flow vectors, not individual paths

