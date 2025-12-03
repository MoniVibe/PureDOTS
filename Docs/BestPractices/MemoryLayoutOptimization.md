# Memory Layout Optimization

**Last Updated**: 2025-12-01
**Maintainer**: PureDOTS Framework Team

---

## Project Context

**This guide applies to all three projects:**

- **PureDOTS**: `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS` - Framework code
- **Space4X**: `C:\Users\Moni\Documents\claudeprojects\unity\Space4x` - Game code
- **Godgame**: `C:\Users\Moni\Documents\claudeprojects\unity\Godgame` - Game code

**⚠️ Important:** When writing PureDOTS framework code, ensure it remains game-agnostic. Memory layout patterns apply to all projects.

See [PROJECT_SEPARATION.md](PROJECT_SEPARATION.md) for project separation rules.

---

## Overview

Memory layout significantly impacts performance in DOTS. This guide covers cache-friendly component design, hot/cold data splitting, and archetype optimization patterns.

**Key Principles:**
- ✅ **Cache-line awareness** (64-byte boundaries)
- ✅ **Hot/cold splitting** (frequently vs rarely accessed data)
- ✅ **Archetype stability** (minimize component changes)
- ✅ **Chunk utilization** (maximize entities per chunk)

**Performance Impact:**
- Cache misses: 100-300 cycles penalty
- Well-optimized layout: 2-5x performance improvement

---

## Component Size Guidelines

### Hot/Medium/Cold Classification

**Hot Path Components** (< 128 bytes):
- Accessed every frame
- In tight loops
- Performance-critical

**Medium Path Components** (128-256 bytes):
- Accessed frequently but not every frame
- Moderate performance impact

**Cold Path Components** (> 256 bytes):
- Accessed rarely
- Can be split to companion entity

### Size Targets

| Component Type | Target Size | Example |
|----------------|-------------|---------|
| **Hot** | < 128 bytes | Transform, Velocity, Health |
| **Medium** | 128-256 bytes | AI State, Inventory (small) |
| **Cold** | > 256 bytes | Biography, Large configs |

---

## Cache-Line Awareness

### What is a Cache Line?

**Cache line = 64 bytes** (typical CPU cache line size)

**Principle:** Data accessed together should fit in cache lines.

### Component Alignment

**Components are aligned to 4-byte boundaries:**

```csharp
// ✅ Good: Fits in cache line (64 bytes)
public struct HotComponent : IComponentData
{
    public float3 Position;      // 12 bytes
    public float3 Velocity;      // 12 bytes
    public quaternion Rotation;  // 16 bytes
    public float Speed;          // 4 bytes
    public float Health;         // 4 bytes
    public byte State;           // 1 byte
    // Total: ~49 bytes (fits in one cache line)
}

// ❌ Bad: Spans multiple cache lines
public struct BloatedComponent : IComponentData
{
    public float3 Position;      // 12 bytes
    public float3 Velocity;      // 12 bytes
    public quaternion Rotation;  // 16 bytes
    public FixedString512Bytes Biography;  // 512 bytes! (8 cache lines)
    // Total: ~540 bytes (many cache misses)
}
```

### Struct Packing

**Minimize padding:**

```csharp
// ❌ Bad: Wasted padding
public struct BadLayout : IComponentData
{
    public byte Flag1;     // 1 byte + 3 bytes padding
    public int Value1;     // 4 bytes
    public byte Flag2;     // 1 byte + 3 bytes padding
    public int Value2;     // 4 bytes
    // Total: 16 bytes (50% wasted)
}

// ✅ Good: Group by size
public struct GoodLayout : IComponentData
{
    public int Value1;     // 4 bytes
    public int Value2;     // 4 bytes
    public byte Flag1;     // 1 byte
    public byte Flag2;     // 1 byte
    // Total: 10 bytes (minimal padding)
}
```

---

## Hot/Cold Data Splitting

### Companion Entity Pattern

**Split large components into companion entities:**

```csharp
// Hot component (main entity)
public struct VillagerHot : IComponentData
{
    public float3 Position;      // 12 bytes
    public float3 Velocity;      // 12 bytes
    public byte Hunger;          // 1 byte
    public byte Thirst;          // 1 byte
    public byte Energy;          // 1 byte
    // Total: ~27 bytes (fits in cache line)
}

// Cold component (companion entity)
public struct VillagerCold : IComponentData
{
    public FixedString64Bytes Name;        // 64 bytes
    public FixedString256Bytes Biography;  // 256 bytes
    public long BirthTick;                 // 8 bytes
    // Total: ~328 bytes (separate entity)
}

// Reference to companion
public struct VillagerColdRef : IComponentData
{
    public Entity CompanionEntity;  // 8 bytes (just a reference)
}
```

### Access Pattern

```csharp
[BurstCompile]
public partial struct VillagerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Hot path: Process main entities (cache-friendly)
        foreach (var (hot, transform) in
            SystemAPI.Query<RefRW<VillagerHot>, RefRW<LocalTransform>>())
        {
            // Process hot data (every frame)
            hot.ValueRW.Hunger += 1;
            transform.ValueRW.Position += hot.ValueRO.Velocity;
        }

        // Cold path: Process companion entities (rarely)
        if (ShouldUpdateColdData())
        {
            foreach (var cold in SystemAPI.Query<RefRW<VillagerCold>>())
            {
                // Process cold data (occasionally)
                UpdateBiography(cold);
            }
        }
    }
}
```

---

## Archetype Design

### Chunk Utilization

**Goal: Maximize entities per chunk (better cache utilization)**

**Chunk size = 16KB** (typical)

**Example:**
```csharp
// Small archetype (good)
Entity + LocalTransform + Velocity + Health = ~80 bytes per entity
→ ~200 entities per 16KB chunk

// Large archetype (bad)
Entity + 10 components (200 bytes total) = 200 bytes per entity
→ ~80 entities per 16KB chunk
```

### Archetype Fragmentation

**❌ Bad: Many small archetypes**

```csharp
// Creates 100 different archetypes
for (int i = 0; i < 100; i++)
{
    var entity = CreateEntity();
    if (i % 2 == 0) AddComponent<TagA>(entity);
    if (i % 3 == 0) AddComponent<TagB>(entity);
    if (i % 5 == 0) AddComponent<TagC>(entity);
}
// Result: Many archetypes, poor chunk utilization
```

**✅ Good: Batch by archetype**

```csharp
// Pre-create archetypes
var archetypeA = CreateArchetype(typeof(TagA));
var archetypeB = CreateArchetype(typeof(TagA), typeof(TagB));
var archetypeC = CreateArchetype(typeof(TagA), typeof(TagB), typeof(TagC));

// Batch create entities
CreateEntity(archetypeA, 50, Allocator.Temp);
CreateEntity(archetypeB, 30, Allocator.Temp);
CreateEntity(archetypeC, 20, Allocator.Temp);
// Result: Few archetypes, good chunk utilization
```

---

## Buffer Element Types

### DynamicBuffer Layout

**Buffer elements are stored contiguously:**

```csharp
// ✅ Good: Small buffer elements
public struct ResourceSlot : IBufferElementData
{
    public ResourceTypeId Type;  // 4 bytes
    public int Quantity;         // 4 bytes
    // Total: 8 bytes per element
}

// ❌ Bad: Large buffer elements
public struct ResourceSlotBad : IBufferElementData
{
    public ResourceTypeId Type;
    public int Quantity;
    public FixedString128Bytes Description;  // 128 bytes per element!
    // Total: 136 bytes per element (poor cache utilization)
}
```

### Buffer Access Patterns

**Access buffers sequentially for cache efficiency:**

```csharp
// ✅ Good: Sequential access
for (int i = 0; i < buffer.Length; i++)
{
    ProcessSlot(buffer[i]);  // Cache-friendly
}

// ❌ Bad: Random access
for (int i = 0; i < indices.Length; i++)
{
    ProcessSlot(buffer[indices[i]]);  // Cache misses
}
```

---

## Blob Asset Data Layout

### Blob Asset Structure

**Blob assets are immutable, shared data:**

```csharp
public struct WeaponCatalogBlob
{
    public BlobArray<WeaponDefinition> Weapons;  // Contiguous array
}

public struct WeaponDefinition
{
    public int WeaponId;        // 4 bytes
    public float Damage;        // 4 bytes
    public float Range;         // 4 bytes
    public BlobString Name;     // Pointer (8 bytes)
    // Total: ~20 bytes per weapon
}
```

### Benefits

- ✅ **Shared memory** (one copy for all entities)
- ✅ **Cache-friendly** (contiguous data)
- ✅ **Immutable** (thread-safe, no locking)

---

## Memory Profiling Workflow

### 1. Profile Memory Usage

**Unity Profiler → Memory:**
- Check component sizes
- Identify large components
- Find archetype fragmentation

### 2. Identify Hot Paths

**Unity Profiler → CPU:**
- Find frequently accessed components
- Identify cache misses
- Measure performance impact

### 3. Optimize Layout

**Apply patterns:**
- Split large components (companion entity)
- Group related data (cache-line awareness)
- Minimize archetype fragmentation

### 4. Verify Improvement

**Re-profile:**
- Check cache hit rates
- Measure performance improvement
- Verify chunk utilization

---

## Common Patterns

### Pattern 1: Hot/Cold Split

```csharp
// Hot: Frequently accessed
public struct CombatHot : IComponentData
{
    public float Health;        // 4 bytes
    public float Armor;         // 4 bytes
    public byte State;          // 1 byte
    // Total: ~9 bytes
}

// Cold: Rarely accessed
public struct CombatCold : IComponentData
{
    public FixedString128Bytes CombatHistory;  // 128 bytes
    public long LastCombatTick;                // 8 bytes
    // Total: ~136 bytes (companion entity)
}
```

### Pattern 2: Tag vs Data Components

```csharp
// ✅ Good: Tag for filtering (0 bytes overhead)
public struct EnemyTag : IComponentData { }

// ✅ Good: Small data component
public struct EnemyData : IComponentData
{
    public byte FactionId;  // 1 byte
    public byte Aggression; // 1 byte
    // Total: 2 bytes
}

// ❌ Bad: Large tag component
public struct EnemyTagBad : IComponentData
{
    public FixedString64Bytes Name;  // 64 bytes (use companion entity instead)
}
```

### Pattern 3: Shared Components

**Use for configuration data:**

```csharp
// Shared component (one copy per archetype)
public struct WeaponConfig : ISharedComponentData
{
    public float Damage;
    public float Range;
    public float FireRate;
}

// Entities share same config (memory efficient)
var archetype = CreateArchetype(
    typeof(LocalTransform),
    typeof(WeaponConfig),  // Shared
    typeof(WeaponState)
);
```

---

## Best Practices Summary

1. ✅ **Keep hot components < 128 bytes** (fits in cache lines)
2. ✅ **Split cold data** to companion entities (> 256 bytes)
3. ✅ **Group related fields** (cache-line awareness)
4. ✅ **Minimize padding** (order fields by size)
5. ✅ **Batch entity creation** (reduce archetype fragmentation)
6. ✅ **Use tags for filtering** (0 bytes overhead)
7. ✅ **Use shared components** for configuration
8. ✅ **Access buffers sequentially** (cache-friendly)
9. ❌ **Don't create bloated components** (> 256 bytes)
10. ❌ **Don't fragment archetypes** (batch creation)

---

## Additional Resources

- [Component Design Patterns](BestPractices/ComponentDesignPatterns.md)
- [DOTS 1.4 Patterns](BestPractices/DOTS_1_4_Patterns.md)
- [Performance Plan](../PERFORMANCE_PLAN.md)
- [Component Migration Guide](../Guides/ComponentMigrationGuide.md)

---

*Last Updated: 2025-12-01*
*Maintainer: PureDOTS Framework Team*

