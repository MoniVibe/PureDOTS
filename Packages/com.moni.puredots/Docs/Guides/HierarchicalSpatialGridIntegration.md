# Hierarchical Spatial Grid Integration Guide

**For agents implementing systems that interact with the spatial grid**

---

## Quick Integration Checklist

- [ ] Understand grid mode (hierarchical vs legacy)
- [ ] Choose appropriate query API (SFC vs legacy)
- [ ] Handle CellKey vs CellId compatibility
- [ ] Use correct level for queries (L0-L3)
- [ ] Consider observer pattern for region-based access
- [ ] Respect rewind state (only query in Record mode)

---

## Integration Patterns

### Pattern 1: Simple Radius Query (New Code)

```csharp
[BurstCompile]
public partial struct MySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<SpatialGridConfig>();
        var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
        var entries = SystemAPI.GetBuffer<SpatialGridEntry>(gridEntity);
        var ranges = SystemAPI.GetBuffer<SpatialGridCellRange>(gridEntity);
        
        // Build SFC bucket map (reuse across queries)
        var buckets = new NativeParallelMultiHashMap<ulong, Entity>(entries.Length, Allocator.TempJob);
        for (int i = 0; i < entries.Length; i++)
        {
            buckets.Add(entries[i].GetPrimaryKey(), entries[i].Entity);
        }
        
        // Query entities
        var results = new NativeList<Entity>(Allocator.TempJob);
        var position = new float3(100f, 0f, 100f);
        SpatialQueryHelper.CollectEntitiesInRadiusSFC(
            ref position, 50f, config, buckets, entries, ref results);
        
        // Process results...
        foreach (var entity in results)
        {
            // Your logic here
        }
        
        buckets.Dispose();
        results.Dispose();
    }
}
```

### Pattern 2: Level-Aware Query

```csharp
// Query appropriate level based on scale
var config = SystemAPI.GetSingleton<SpatialGridConfig>();
if (config.IsHierarchical)
{
    // Large-scale query: use L1_System
    if (config.TryGetLevelConfig(HierarchicalGridLevel.L1_System, out var systemLevel))
    {
        // Query at system level...
    }
    
    // Local query: use L3_Local
    if (config.TryGetLevelConfig(HierarchicalGridLevel.L3_Local, out var localLevel))
    {
        // Query at local level...
    }
}
```

### Pattern 3: Observer-Based Streaming

```csharp
// Register observer for region-based access
var observerEntity = entityManager.CreateEntity();
entityManager.AddComponent(observerEntity, new SpatialObserver
{
    Position = myPosition,
    Radius = 1000f,
    IsActive = true
});
entityManager.AddBuffer<SpatialObserverActiveCells>(observerEntity);

// Query active cells from observer
var activeCells = SystemAPI.GetBuffer<SpatialObserverActiveCells>(observerEntity);
foreach (var cell in activeCells)
{
    var cellKey = cell.CellKey;
    // Query entities in this cell...
}
```

### Pattern 4: Migration Helper Usage

```csharp
// Check and migrate legacy grids
var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
var config = SystemAPI.GetComponent<SpatialGridConfig>(gridEntity);
var entries = SystemAPI.GetBuffer<SpatialGridEntry>(gridEntity);
var ranges = SystemAPI.GetBuffer<SpatialGridCellRange>(gridEntity);

if (SpatialGridMigration.NeedsMigration(config))
{
    // Migrate to hierarchical (preserves all data)
    SpatialGridMigration.MigrateToHierarchical(ref config, ref entries, ref ranges);
    SystemAPI.SetComponent(gridEntity, config);
    
    // Update provider ID
    config.ProviderId = SpatialGridProviderIds.Hierarchical;
    SystemAPI.SetComponent(gridEntity, config);
}
```

---

## API Compatibility

### CellKey vs CellId

**Always use `GetPrimaryKey()`** for maximum compatibility:

```csharp
// ✅ CORRECT - Works with both legacy and hierarchical
ulong key = entry.GetPrimaryKey();

// ❌ WRONG - May be zero in hierarchical grids
int cellId = entry.CellId; // Legacy only

// ❌ WRONG - May be zero in legacy grids  
ulong key = entry.CellKey; // Hierarchical only
```

### Query API Selection

| Use Case | API | Notes |
|----------|-----|-------|
| New code, single query | `CollectEntitiesInRadiusSFC` | Cache-coherent, recommended |
| Legacy compatibility | `CollectEntitiesInRadius` | Backward compatible |
| Multiple queries | `SpatialQueryBucketJob` | Parallel batch processing |
| K-nearest | `FindKNearestInRadius` | Works with both APIs |

---

## System Integration

### Execution Order

Your system should run **after** `SpatialGridBuildSystem`:

```csharp
[UpdateInGroup(typeof(SpatialSystemGroup))]
[UpdateAfter(typeof(SpatialGridBuildSystem))]
public partial struct MySpatialQuerySystem : ISystem
{
    // Your code...
}
```

### Rewind Safety

Always check rewind state before querying:

```csharp
var rewindState = SystemAPI.GetSingleton<RewindState>();
if (rewindState.Mode != RewindMode.Record)
{
    return; // Skip during rewind playback
}
```

---

## Performance Tips

1. **Reuse bucket maps**: Build `NativeParallelMultiHashMap` once per rebuild, reuse across queries
2. **Batch queries**: Use `SpatialQueryBucketJob` for multiple queries
3. **Choose correct level**: Query L3 for local entities, L0 for galactic scale
4. **Use observers**: Set up `SpatialObserver` for region-based streaming
5. **Dispose temporaries**: Always dispose `NativeList` and `NativeHashMap` after use

---

## Common Pitfalls

### ❌ Accessing Grid During Rewind

```csharp
// ❌ WRONG
var entries = SystemAPI.GetBuffer<SpatialGridEntry>(gridEntity);
// Grid may be in inconsistent state during rewind

// ✅ CORRECT
var rewindState = SystemAPI.GetSingleton<RewindState>();
if (rewindState.Mode == RewindMode.Record)
{
    var entries = SystemAPI.GetBuffer<SpatialGridEntry>(gridEntity);
}
```

### ❌ Assuming CellId Always Valid

```csharp
// ❌ WRONG
int cellId = entry.CellId; // May be -1 or invalid in hierarchical grids

// ✅ CORRECT
ulong key = entry.GetPrimaryKey(); // Always valid
```

### ❌ Not Disposing Native Containers

```csharp
// ❌ WRONG
var results = new NativeList<Entity>(Allocator.TempJob);
SpatialQueryHelper.CollectEntitiesInRadius(...);
// Memory leak!

// ✅ CORRECT
var results = new NativeList<Entity>(Allocator.TempJob);
SpatialQueryHelper.CollectEntitiesInRadius(...);
results.Dispose(); // Always dispose
```

---

## Testing

### Unit Test Example

```csharp
[Test]
public void TestHierarchicalGridQuery()
{
    // Setup grid config
    var config = new SpatialGridConfig
    {
        IsHierarchical = true,
        // ... configure levels ...
    };
    
    // Add test entities
    // ...
    
    // Query and verify results
    var results = new NativeList<Entity>(Allocator.Temp);
    SpatialQueryHelper.CollectEntitiesInRadiusSFC(...);
    Assert.AreEqual(expectedCount, results.Length);
    results.Dispose();
}
```

---

**See Also:**
- `HierarchicalSpatialGridGuide.md` - Full documentation
- `HierarchicalSpatialGridQuickReference.md` - Quick lookup
- `Runtime/Runtime/Spatial/` - Source code with XML docs

