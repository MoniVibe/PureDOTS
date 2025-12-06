# Hierarchical Spatial Grid System Guide

**Updated**: 2025-01-XX  
**Purpose**: Guide for using the multi-resolution hierarchical spatial grid system in PureDOTS

---

## Overview

The hierarchical spatial grid system provides multi-resolution spatial partitioning optimized for large-scale deterministic simulations. It supports four resolution levels (L0-L3) with adaptive subdivision, space-filling curve indexing, and temporal caching for rewind support.

### Key Features

- **Multi-Resolution Levels**: L0 (Galactic) → L1 (System) → L2 (Planet) → L3 (Local)
- **Adaptive Subdivision**: Density-driven octree refinement for L2/L3 levels
- **Space-Filling Curve Indexing**: Morton/Hilbert keys for cache-coherent queries
- **Hot/Cold Data Split**: Optimized memory layout with AoSoA packets
- **Temporal Caching**: Ring buffer snapshots for deterministic rewind
- **Region Streaming**: Observer-based cell activation/deactivation
- **Dynamic Load Balancing**: Work queue distribution across threads
- **Backward Compatible**: Legacy single-level grids continue to work

---

## Architecture

### Hierarchical Levels

| Level | Cell Size Example | Use Case | Tick Rate | Notes |
|-------|------------------|----------|-----------|-------|
| **L0_Galactic** | 1 ly - 100 AU | Fleet & system positions | 0.001 Hz | Analytic orbits only |
| **L1_System** | 10⁶ km | Planet & station positions | 0.01 Hz | Coarse collision zones |
| **L2_Planet** | 1-10 km | Cities / biomes | 1 Hz | Full deterministic grid |
| **L3_Local** | 1-100 m | Entities / agents | 60 Hz | Fine physics & AI |

### Data Structures

**SpatialCell**: Core cell data with hot (entities, positions) and cold (density, stats) data
**OctreeSoA**: Structure-of-Arrays octree for adaptive subdivision
**CellPacket**: AoSoA layout (16 entities per packet) for SIMD operations
**TemporalGridCache**: Ring buffer (16 ticks) for rewind support

---

## Usage

### 1. Configuring a Hierarchical Grid

#### Via Authoring (Recommended)

Create a `SpatialPartitionProfile` asset and configure hierarchical levels:

```csharp
// In authoring code or inspector
var profile = ScriptableObject.CreateInstance<SpatialPartitionProfile>();
profile.SetWorldBounds(center, extent);

// Enable hierarchical mode
var config = profile.ToComponent();
config.IsHierarchical = true;

// Add level configurations
var levels = new FixedList512Bytes<HierarchicalLevelConfig>();
levels.Add(new HierarchicalLevelConfig
{
    Level = HierarchicalGridLevel.L3_Local,
    CellSize = 10f,      // 10m cells
    TickRate = 60f,      // 60 Hz updates
    UseAnalyticOrbits = false,
    WorldMin = config.WorldMin,
    WorldMax = config.WorldMax,
    CellCounts = CalculateCellCounts(config.WorldExtent, 10f)
});
// ... add L2, L1, L0 levels

config.HierarchicalLevels = levels;
config.ProviderId = SpatialGridProviderIds.Hierarchical;
```

#### Runtime Configuration

```csharp
// Get grid config singleton
var config = SystemAPI.GetSingleton<SpatialGridConfig>();

// Check if hierarchical
if (config.IsHierarchical)
{
    // Get level-specific config
    if (config.TryGetLevelConfig(HierarchicalGridLevel.L3_Local, out var levelConfig))
    {
        var cellSize = levelConfig.CellSize;
        var tickRate = levelConfig.TickRate;
    }
}
```

### 2. Indexing Entities

Entities are automatically indexed when they have `SpatialIndexedTag`:

```csharp
// Add tag to entity
entityManager.AddComponent<SpatialIndexedTag>(entity);

// Entity will be indexed in the next grid rebuild
// CellKey (Morton key) is computed automatically
```

### 3. Querying the Grid

#### Radius Queries (Legacy API)

```csharp
var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
var entries = SystemAPI.GetBuffer<SpatialGridEntry>(gridEntity);
var ranges = SystemAPI.GetBuffer<SpatialGridCellRange>(gridEntity);
var config = SystemAPI.GetSingleton<SpatialGridConfig>();

var results = new NativeList<Entity>(Allocator.TempJob);
var position = new float3(100f, 0f, 100f);
var radius = 50f;

SpatialQueryHelper.CollectEntitiesInRadius(
    ref position, radius, config, ranges, entries, ref results);

// Process results...
results.Dispose();
```

#### SFC-Based Queries (New API)

```csharp
// Build cell key bucket map (done once per rebuild)
var cellKeyBuckets = new NativeParallelMultiHashMap<ulong, Entity>(entries.Length, Allocator.TempJob);
for (int i = 0; i < entries.Length; i++)
{
    var entry = entries[i];
    var cellKey = entry.GetPrimaryKey(); // Uses CellKey if available, falls back to CellId
    cellKeyBuckets.Add(cellKey, entry.Entity);
}

// Query using SFC keys (cache-coherent)
var results = new NativeList<Entity>(Allocator.TempJob);
SpatialQueryHelper.CollectEntitiesInRadiusSFC(
    ref position, radius, config, cellKeyBuckets, entries, ref results);

cellKeyBuckets.Dispose();
results.Dispose();
```

#### Parallel Batch Queries

```csharp
var descriptors = new NativeArray<SpatialQueryDescriptor>(queryCount, Allocator.TempJob);
// ... populate descriptors ...

var results = new NativeList<Entity>(Allocator.TempJob);
var filter = new SpatialAcceptAllFilter();

var job = new SpatialQueryBucketJob<SpatialAcceptAllFilter>
{
    Config = config,
    CellKeyBuckets = cellKeyBuckets,
    Entries = entries.AsNativeArray(),
    Descriptors = descriptors,
    ResultsWriter = results.AsParallelWriter(),
    Filter = filter
};

var handle = job.ScheduleParallel(queryCount, 64, state.Dependency);
handle.Complete();
```

### 4. Adaptive Subdivision

Subdivision is automatic for L2/L3 levels based on density thresholds:

```csharp
// Configure thresholds in SpatialGridConfig
config.UpperDensityThreshold = 100.0f;  // Subdivide if > 100 entities/cell
config.LowerDensityThreshold = 10.0f;   // Merge if < 10 entities/cell
config.MaxSubdivisionDepth = 4;          // Max 4 levels deep

// SpatialGridRefinementSystem runs at ~0.2 Hz (every 5 seconds)
// Automatically subdivides/merges cells based on density
```

### 5. Region Streaming

Set up observers for region-based streaming:

```csharp
// Create observer entity
var observerEntity = entityManager.CreateEntity();
entityManager.AddComponent<SpatialObserver>(observerEntity, new SpatialObserver
{
    Position = cameraPosition,
    Radius = 1000f,  // 1km radius
    IsActive = true,
    LastUpdateTick = 0
});
entityManager.AddBuffer<SpatialObserverActiveCells>(observerEntity);

// Enable streaming in config
var streamingConfig = new SpatialGridStreamingConfig
{
    StreamingRadius = 1000f,
    EnableStreaming = true,
    StreamingUpdateInterval = 60  // Update every second
};
entityManager.AddComponent(gridEntity, streamingConfig);

// SpatialGridStreamingSystem will deactivate cells beyond radius
// and compress them as CompressedCellSummary
```

### 6. Temporal Caching & Rewind

Snapshots are captured automatically for rewind support:

```csharp
// Get snapshot for a specific tick
var cache = GetTemporalCache(gridEntity); // Retrieve from component/system
var snapshot = cache.GetSnapshot(targetTick);

if (snapshot.IsValid)
{
    // Restore grid state from snapshot
    foreach (var kvp in snapshot.Cells)
    {
        var cellKey = kvp.Key;
        var cellSnapshot = kvp.Value;
        // Restore cell state...
    }
}
```

### 7. Multi-World Support

Each ECS world can have its own grid:

```csharp
// Create grid entity with world index
var gridEntity = entityManager.CreateEntity();
entityManager.AddComponent(gridEntity, new SpatialGridConfig { /* ... */ });
entityManager.AddSharedComponent(gridEntity, new SpatialGridWorldIndex(worldIndex: 1));

// Queries respect WorldIndex filter
var query = SystemAPI.QueryBuilder()
    .WithAll<SpatialIndexedTag>()
    .WithSharedComponentFilter(new SpatialGridWorldIndex(1))
    .Build();
```

---

## Migration from Legacy Grids

### Automatic Migration

Legacy single-level grids continue to work. To migrate:

```csharp
var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
var config = SystemAPI.GetComponent<SpatialGridConfig>(gridEntity);
var entries = SystemAPI.GetBuffer<SpatialGridEntry>(gridEntity);
var ranges = SystemAPI.GetBuffer<SpatialGridCellRange>(gridEntity);

if (SpatialGridMigration.NeedsMigration(config))
{
    SpatialGridMigration.MigrateToHierarchical(
        ref config, ref entries, ref ranges);
    
    // Update component
    SystemAPI.SetComponent(gridEntity, config);
}
```

### Manual Migration

1. Set `IsHierarchical = true` in config
2. Add `HierarchicalLevelConfig` entries for L0-L3
3. Set `ProviderId = SpatialGridProviderIds.Hierarchical`
4. Existing `SpatialGridEntry` data is preserved (CellKey computed automatically)

---

## Performance Considerations

### Memory Usage

- **Hot Data**: ~100MB for 50k entities (positions, velocities)
- **Cold Data**: ~50MB for 50k entities (density, stats)
- **Temporal Cache**: ~16MB for 16-tick ring buffer

### Query Performance

- **Legacy API**: O(n) where n = entities in queried cells
- **SFC API**: O(log n) with cache-coherent access
- **Parallel Queries**: Scales linearly with worker threads

### Build Performance

- **Target**: < 3ms/frame for 50k entities
- **Load Balance**: < 10% variance across threads
- **Refinement**: ~0.2 Hz (every 5 seconds) for subdivision

---

## System Execution Order

```
SpatialSystemGroup
├── SpatialGridBuildSystem (OrderFirst)
│   └── Rebuilds grid, computes CellKeys
├── SpatialGridRefinementSystem
│   └── Adaptive subdivision (0.2 Hz)
├── SpatialGridLoadBalancerSystem
│   └── Work queue balancing (100 tick intervals)
├── SpatialGridStreamingSystem
│   └── Region culling/streaming (60 tick intervals)
└── SpatialGridSnapshotSystem
    └── Temporal cache capture (every tick if changed)
```

---

## Best Practices

1. **Use SFC Keys**: Prefer `CellKey` over `CellId` for new code
2. **Batch Queries**: Use parallel batch jobs for multiple queries
3. **Observer Pattern**: Use `SpatialObserver` for region-based streaming
4. **Level Selection**: Query appropriate level (L3 for local, L0 for galactic)
5. **Migration**: Migrate legacy grids during initialization, not runtime

---

## API Reference

### Key Types

- `SpatialGridConfig`: Grid configuration (supports hierarchical)
- `HierarchicalLevelConfig`: Per-level configuration
- `SpatialGridEntry`: Entity entry with CellKey (SFC) and CellId (legacy)
- `SpatialQueryHelper`: Query utilities (legacy and SFC-based)
- `SpaceFillingCurve`: Morton/Hilbert encoding/decoding
- `OctreeSoA`: Adaptive subdivision data structure
- `TemporalGridCache`: Rewind snapshot storage

### Key Systems

- `SpatialGridBuildSystem`: Grid rebuild (runs every tick)
- `SpatialGridRefinementSystem`: Adaptive subdivision (0.2 Hz)
- `SpatialGridLoadBalancerSystem`: Work queue balancing
- `SpatialGridStreamingSystem`: Region streaming
- `SpatialGridSnapshotSystem`: Temporal cache capture

---

## Examples

See `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Spatial/` for system implementations and `Runtime/Runtime/Spatial/` for data structures.

---

## Troubleshooting

### Grid Not Updating

- Check `RewindState.Mode == RewindMode.Record`
- Verify `TimeState.IsPaused == false`
- Ensure entities have `SpatialIndexedTag`

### Poor Query Performance

- Use SFC-based queries (`CollectEntitiesInRadiusSFC`)
- Enable parallel batch queries for multiple queries
- Check cell density (consider adjusting thresholds)

### Memory Issues

- Disable streaming if not needed (`EnableStreaming = false`)
- Reduce temporal cache size if memory constrained
- Use compressed cell summaries for inactive regions

---

**See Also**:
- `Docs/Guides/Authoring/EnvironmentAndSpatialValidation.md` - Authoring guide
- `Runtime/Runtime/Spatial/` - Source code with XML documentation

