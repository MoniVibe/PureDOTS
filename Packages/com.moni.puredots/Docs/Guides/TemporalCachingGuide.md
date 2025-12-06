# Temporal Caching Guide

**Purpose**: Guide for using temporal caching to skip recomputation when inputs are unchanged (target: 70% cache hit rate).

## Overview

Temporal caching stores computation results with input hashes. When inputs are identical for N ticks, the cached result is reused instead of recomputing.

## Core Components

### ResultCache<T>

Burst-safe ring buffer for caching computation results:

```csharp
var cache = new ResultCache<PathResult>(capacity: 100, Allocator.Persistent);

// Try to get cached result
if (cache.TryGet(inputHash, out var cachedResult))
{
    return cachedResult; // Cache hit!
}

// Compute result
var result = ComputeExpensiveOperation(inputs);

// Store in cache
cache.Store(inputHash, result);
```

### CacheKey Component

```csharp
public struct CacheKey : IComponentData
{
    public uint InputHash;
    public uint LastCacheHitTick;
    public uint CacheHitCount;
}
```

Stores input hash per entity for cache lookups.

### CacheStats Component

```csharp
public struct CacheStats : IComponentData
{
    public uint TotalLookups;
    public uint CacheHits;
    public uint CacheMisses;
    
    public float HitRate => TotalLookups > 0 ? (float)CacheHits / TotalLookups : 0f;
}
```

Tracks cache performance for telemetry.

## Usage Pattern

### Computing Input Hash

```csharp
using PureDOTS.Runtime.Caching;

// Hash single value
uint hash = ResultCache<float>.ComputeHash(position.x);

// Hash multiple values
uint hash1 = ResultCache<float3>.ComputeHash(startPosition);
uint hash2 = ResultCache<float3>.ComputeHash(endPosition);
uint combinedHash = ResultCache<float>.CombineHashes(hash1, hash2);
```

### Caching Pathfinding Results

```csharp
[BurstCompile]
private struct PathfindingJob : IJobEntity
{
    public ResultCache<PathResult> PathCache;
    
    public void Execute(Entity entity, ref NavigationTarget target, ref CacheKey cacheKey)
    {
        // Compute input hash
        uint inputHash = ResultCache<float3>.ComputeHash(target.Start);
        inputHash = ResultCache<float>.CombineHashes(inputHash, 
            ResultCache<float3>.ComputeHash(target.End));
        
        // Try cache
        if (PathCache.TryGet(inputHash, out var cachedPath))
        {
            target.Path = cachedPath;
            cacheKey.CacheHitCount++;
            return; // Cache hit!
        }
        
        // Cache miss - compute path
        var path = ComputePath(target.Start, target.End);
        
        // Store in cache
        PathCache.Store(inputHash, path);
        target.Path = path;
        
        // Update cache key
        cacheKey.InputHash = inputHash;
    }
}
```

### Rewind Awareness

Temporal caching integrates with the rewind system:

```csharp
var rewindState = SystemAPI.GetSingleton<RewindState>();
if (rewindState.Mode == RewindMode.Playback)
{
    // During playback, use cached results from recorded state
    // Cache invalidation handled by CacheInvalidationSystem
}
```

## Integration Examples

### NavigationSystem

Cache pathfinding segments when start/end positions unchanged:
- Input hash: `CombineHashes(startPosition, endPosition)`
- Cache result: `PathResult` with waypoints

### PerceptionSystem

Cache sensor queries when sensor state unchanged:
- Input hash: `CombineHashes(sensorPosition, sensorRange, targetPosition)`
- Cache result: `SensorReading` with confidence scores

### VillagerNeedsSystem

Cache energy equations when needs inputs unchanged:
- Input hash: `CombineHashes(hunger, energy, health)`
- Cache result: `NeedsUpdate` with computed deltas

## Cache Invalidation

`CacheInvalidationSystem` handles automatic cache invalidation:

```csharp
// System detects input component changes and clears cache keys
// Forces recomputation on next access
```

Manual invalidation:

```csharp
cache.Clear(); // Clear all entries
cacheKey.InputHash = 0; // Force recomputation for this entity
```

## Best Practices

1. **Cache expensive operations**: Pathfinding, sensor queries, complex equations
2. **Use appropriate cache size**: 50-200 entries per cache type
3. **Monitor hit rates**: Target 70%+ cache hit rate via `CacheStats`
4. **Invalidate on input changes**: Use `CacheInvalidationSystem` or manual invalidation
5. **Respect rewind**: Cache works correctly with deterministic rewind

## Performance Impact

- **70%+ cache hit rate** in dense scenarios
- **Zero recomputation** when inputs unchanged
- **Deterministic**: Cache results are deterministic and rewind-safe

## References

- `PureDOTS/Packages/com.moni.puredots/Runtime/Caching/ResultCache.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Components/Caching/CacheKey.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Caching/CacheInvalidationSystem.cs`

