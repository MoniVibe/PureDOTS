# Spatial Query Helper Usage Guide

## Overview

`SpatialQueryHelper` provides Burst-friendly utilities for querying the spatial grid. These helpers replace ad-hoc linear scans with efficient cell-based queries.

## Available Methods

### GetEntitiesWithinRadius

Finds all entities within a radius of a position.

```csharp
var results = new NativeList<Entity>(Allocator.Temp);
SpatialQueryHelper.GetEntitiesWithinRadius(
    position: villagerPos,
    radius: 10f,
    config: spatialConfig,
    ranges: cellRanges,
    entries: gridEntries,
    results: ref results);

// Use results...
results.Dispose();
```

### FindNearestEntity

Finds the nearest entity to a position.

```csharp
if (SpatialQueryHelper.FindNearestEntity(
    position: villagerPos,
    config: spatialConfig,
    ranges: cellRanges,
    entries: gridEntries,
    nearest: out var nearest,
    distance: out var distance))
{
    // Use nearest entity...
}
```

**Note**: EntityQuery filtering must be done externally - this method searches all entities in the grid. Filter results afterwards based on component requirements.

### GetCellEntities

Gets all entities in a specific grid cell.

```csharp
var results = new NativeList<Entity>(Allocator.Temp);
SpatialHash.Quantize(position, spatialConfig, out var cellCoords);
SpatialQueryHelper.GetCellEntities(
    cellCoords: cellCoords,
    config: spatialConfig,
    ranges: cellRanges,
    entries: gridEntries,
    results: ref results);
```

### OverlapAABB

Finds entities overlapping an axis-aligned bounding box.

```csharp
var results = new NativeList<Entity>(Allocator.Temp);
SpatialQueryHelper.OverlapAABB(
    min: aabbMin,
    max: aabbMax,
    config: spatialConfig,
    ranges: cellRanges,
    entries: gridEntries,
    results: ref results);
```

## Integration Examples

### Villager Job Assignment

Use spatial queries to find nearest resources/storehouses:

```csharp
// Get spatial grid data
var spatialConfig = SystemAPI.GetSingleton<SpatialGridConfig>();
var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
var cellRanges = SystemAPI.GetBuffer<SpatialGridCellRange>(gridEntity);
var gridEntries = SystemAPI.GetBuffer<SpatialGridEntry>(gridEntity);

// Query nearby entities
var candidates = new NativeList<Entity>(Allocator.Temp);
SpatialQueryHelper.GetEntitiesWithinRadius(
    villagerPos, 
    searchRadius, 
    spatialConfig, 
    cellRanges, 
    gridEntries, 
    ref candidates);

// Filter by component (e.g., ResourceSourceConfig)
foreach (var candidate in candidates)
{
    if (componentLookup.HasComponent<ResourceSourceConfig>(candidate))
    {
        // Process resource...
    }
}
```

### Threat Detection

Query nearby threats for AI systems:

```csharp
var threats = new NativeList<Entity>(Allocator.Temp);
SpatialQueryHelper.GetEntitiesWithinRadius(
    agentPos,
    sensorRadius,
    spatialConfig,
    cellRanges,
    gridEntries,
    ref threats);

// Filter by threat component...
```

## Performance Considerations

- **Cell-based queries** are O(k) where k is entities in nearby cells, not O(n) for all entities
- **Deterministic sorting** is applied automatically when using helpers
- **Memory**: Results lists should be allocated with `Allocator.Temp` or `Allocator.TempJob` and disposed after use
- **Burst compatibility**: All helpers are `[BurstCompile]` and can be used in jobs

## When to Use vs. Registry Queries

- **Spatial queries**: Use when you need entities by position/radius (AI sensors, targeting, selection)
- **Registry queries**: Use when you need entities by type/category (job assignment, resource gathering)
- **Combined**: Use spatial queries to narrow candidates, then filter by registry/component criteria

## See Also

- `Docs/TODO/SpatialServices_TODO.md` - Spatial grid implementation details
- `Runtime/Runtime/Spatial/SpatialUtilities.cs` - Full API reference
- `Runtime/Runtime/Registry/RegistryQueryHelpers.cs` - Registry query utilities


