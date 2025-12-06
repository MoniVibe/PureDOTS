# Archetype Registry Guide

**Purpose**: Guide for reducing archetype fragmentation via hash-based caching.

## Overview

Cache archetype combinations by uint64 hash (component bitmask). Reuse existing archetypes whenever possible to avoid fragmentation. Dramatic reduction in memory fragmentation and faster chunk iteration.

## Core Components

### ArchetypeRegistry

```csharp
public struct ArchetypeRegistry : IComponentData
{
    public uint Version;
    public uint ArchetypeCount;
    public uint FragmentationScore; // Lower is better
}
```

Global archetype registry singleton for reducing fragmentation.

### ArchetypeHash

```csharp
public struct ArchetypeHash : IComponentData
{
    public ulong ComponentBitmask; // uint64 hash of component combination
}
```

Archetype hash component for tracking archetype combinations.

## Usage Pattern

### Computing Archetype Hash

```csharp
// Hash component bitmask to uint64
ulong ComputeArchetypeHash(ComponentType[] components)
{
    ulong hash = 0;
    for (int i = 0; i < components.Length; i++)
    {
        hash ^= (ulong)components[i].TypeIndex << (i % 64);
    }
    return hash;
}
```

### Caching Archetype Lookups

```csharp
// Check if archetype exists
if (ArchetypeRegistry.TryGetArchetype(hash, out var existingArchetype))
{
    // Reuse existing archetype
    return existingArchetype;
}

// Create new archetype
var newArchetype = CreateArchetype(components);
ArchetypeRegistry.RegisterArchetype(hash, newArchetype);
return newArchetype;
```

### Integration with EntityQueryBuilder

```csharp
// EntityQueryBuilder pattern caching
var query = SystemAPI.QueryBuilder()
    .WithAll<ComponentA, ComponentB>()
    .Build();

// ArchetypeRegistry caches query patterns
// Reuses archetypes when component combinations match
```

## Best Practices

1. **Hash component combinations**: Use uint64 hash for component bitmasks
2. **Cache archetype lookups**: Reuse existing archetypes when possible
3. **Monitor fragmentation**: Track fragmentation score via telemetry
4. **Integrate with queries**: Use EntityQueryBuilder pattern caching
5. **Reduce archetype explosion**: Minimize unique archetype combinations

## Performance Impact

- **Dramatic fragmentation reduction**: Fewer archetypes = less fragmentation
- **Faster chunk iteration**: Reused archetypes improve cache locality
- **Memory efficiency**: Reduced memory fragmentation at scale

## References

- `PureDOTS/Packages/com.moni.puredots/Runtime/Registry/ArchetypeRegistry.cs`

