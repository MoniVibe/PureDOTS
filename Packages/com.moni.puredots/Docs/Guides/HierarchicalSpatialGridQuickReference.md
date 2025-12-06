# Hierarchical Spatial Grid Quick Reference

**Quick lookup for common operations**

---

## Configuration

```csharp
// Check if grid is hierarchical
var config = SystemAPI.GetSingleton<SpatialGridConfig>();
bool isHierarchical = config.IsHierarchical;

// Get level config
if (config.TryGetLevelConfig(HierarchicalGridLevel.L3_Local, out var levelConfig))
{
    float cellSize = levelConfig.CellSize;
    float tickRate = levelConfig.TickRate;
}
```

---

## Indexing Entities

```csharp
// Add to grid (automatic on next rebuild)
entityManager.AddComponent<SpatialIndexedTag>(entity);
```

---

## Querying

### Radius Query (Legacy)
```csharp
var results = new NativeList<Entity>(Allocator.TempJob);
SpatialQueryHelper.CollectEntitiesInRadius(
    ref position, radius, config, ranges, entries, ref results);
```

### Radius Query (SFC - Recommended)
```csharp
// Build bucket map once per rebuild
var buckets = new NativeParallelMultiHashMap<ulong, Entity>(entries.Length, Allocator.TempJob);
for (int i = 0; i < entries.Length; i++)
    buckets.Add(entries[i].GetPrimaryKey(), entries[i].Entity);

// Query
var results = new NativeList<Entity>(Allocator.TempJob);
SpatialQueryHelper.CollectEntitiesInRadiusSFC(
    ref position, radius, config, buckets, entries, ref results);
```

### K-Nearest
```csharp
var results = new NativeList<KNearestResult>(Allocator.TempJob);
SpatialQueryHelper.FindKNearestInRadius<SpatialAcceptAllFilter>(
    ref position, radius, k, config, ranges, entries, ref results, filter);
```

---

## SFC Keys

```csharp
// Encode coordinates to Morton key
SpatialHash.Quantize(position, config, out var coords);
ulong cellKey = SpaceFillingCurve.Morton3D(in coords);

// Decode Morton key to coordinates
int3 coords = SpaceFillingCurve.DecodeMorton(cellKey);

// From entry
ulong key = entry.GetPrimaryKey(); // Uses CellKey if available, falls back to CellId
```

---

## Migration

```csharp
if (SpatialGridMigration.NeedsMigration(config))
{
    SpatialGridMigration.MigrateToHierarchical(ref config, ref entries, ref ranges);
    SystemAPI.SetComponent(gridEntity, config);
}
```

---

## Provider IDs

```csharp
SpatialGridProviderIds.Hashed      // 0 - Legacy hashed grid
SpatialGridProviderIds.Uniform     // 1 - Legacy uniform grid
SpatialGridProviderIds.Hierarchical // 2 - New hierarchical grid
```

---

## System Groups

```
SpatialSystemGroup
├── SpatialGridBuildSystem (OrderFirst)
├── SpatialGridRefinementSystem (~0.2 Hz)
├── SpatialGridLoadBalancerSystem (100 tick intervals)
├── SpatialGridStreamingSystem (60 tick intervals)
└── SpatialGridSnapshotSystem (every tick if changed)
```

---

## Component Lookup

```csharp
// Grid singleton
var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
var config = SystemAPI.GetSingleton<SpatialGridConfig>();
var entries = SystemAPI.GetBuffer<SpatialGridEntry>(gridEntity);
var ranges = SystemAPI.GetBuffer<SpatialGridCellRange>(gridEntity);

// Hierarchical state
var hierarchicalState = SystemAPI.GetComponent<HierarchicalSpatialGridState>(gridEntity);

// Hot/cold data (if using AoSoA)
var hotData = SystemAPI.GetComponent<SpatialCellHotData>(cellEntity);
var coldData = SystemAPI.GetComponent<SpatialCellColdData>(cellEntity);
```

---

## Common Patterns

### Query Entities in Radius (System)
```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    var config = SystemAPI.GetSingleton<SpatialGridConfig>();
    var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
    var entries = SystemAPI.GetBuffer<SpatialGridEntry>(gridEntity);
    var ranges = SystemAPI.GetBuffer<SpatialGridCellRange>(gridEntity);
    
    var results = new NativeList<Entity>(Allocator.TempJob);
    var position = new float3(100f, 0f, 100f);
    
    SpatialQueryHelper.CollectEntitiesInRadius(
        ref position, 50f, config, ranges, entries, ref results);
    
    // Process results...
    results.Dispose();
}
```

### Check Entity Cell
```csharp
// From SpatialGridResidency component
var residency = SystemAPI.GetComponent<SpatialGridResidency>(entity);
ulong cellKey = residency.GetPrimaryKey();
int cellId = residency.CellId; // Legacy fallback
```

### Observer Setup
```csharp
var observer = new SpatialObserver
{
    Position = cameraPosition,
    Radius = 1000f,
    IsActive = true,
    LastUpdateTick = 0
};
entityManager.AddComponent(observerEntity, observer);
entityManager.AddBuffer<SpatialObserverActiveCells>(observerEntity);
```

---

**Full Guide**: See `HierarchicalSpatialGridGuide.md` for detailed documentation.

