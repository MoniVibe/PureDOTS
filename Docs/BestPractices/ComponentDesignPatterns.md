# Component Design Patterns

**Last Updated**: 2025-12-01
**Maintainer**: PureDOTS Framework Team

---

## Project Context

**This guide applies to all three projects:**

- **PureDOTS**: `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS` - Framework code
- **Space4X**: `C:\Users\Moni\Documents\claudeprojects\unity\Space4x` - Game code
- **Godgame**: `C:\Users\Moni\Documents\claudeprojects\unity\Godgame` - Game code

**⚠️ Important:** When writing PureDOTS framework code, ensure it remains game-agnostic. Component design patterns apply to all projects.

See [PROJECT_SEPARATION.md](PROJECT_SEPARATION.md) for project separation rules.

---

## Overview

Component design is fundamental to DOTS performance. This guide covers sizing guidelines, tag vs data components, shared components, and enable/disable patterns.

**Key Principles:**
- ✅ **Hot path sizing** (< 128 bytes for frequently accessed)
- ✅ **Tag vs data** (tags for filtering, data for values)
- ✅ **Shared components** (configuration data)
- ✅ **Enable/Disable** (state without structural changes)

---

## Component Sizing Guidelines

### Hot Path Components

**Target: < 128 bytes** (fits in cache lines)

```csharp
// ✅ Good: Small hot component
public struct VillagerNeeds : IComponentData
{
    public byte Hunger;   // 0-100
    public byte Thirst;   // 0-100
    public byte Energy;   // 0-100
    public byte Morale;   // 0-100
    // Total: 4 bytes (excellent)
}

// ⚠️ OK: Medium-sized hot component
public struct CombatStats : IComponentData
{
    public float Health;      // 4 bytes
    public float Armor;      // 4 bytes
    public float Damage;     // 4 bytes
    public float AttackSpeed;// 4 bytes
    public float3 Position;  // 12 bytes
    public quaternion Rotation; // 16 bytes
    // Total: 44 bytes (good)
}

// ❌ Bad: Large hot component
public struct BloatedComponent : IComponentData
{
    public float3 Position;
    public quaternion Rotation;
    public FixedString512Bytes Biography;  // 512 bytes!
    // Total: ~540 bytes (poor cache utilization)
}
```

### Cold Path Components

**Can be larger (> 256 bytes), consider companion entity:**

```csharp
// Cold data (companion entity pattern)
public struct VillagerBiography : IComponentData
{
    public FixedString64Bytes Name;
    public FixedString256Bytes Biography;
    public long BirthTick;
    public long DeathTick;
    // Total: ~336 bytes (separate entity)
}
```

---

## Tag vs Data Components

### Tag Components

**Use for filtering/identification (0 bytes overhead):**

```csharp
// Empty struct = tag component
public struct EnemyTag : IComponentData { }
public struct PlayerTag : IComponentData { }
public struct VillagerTag : IComponentData { }

// Usage: Filter entities
foreach (var transform in
    SystemAPI.Query<RefRW<LocalTransform>>()
        .WithAll<EnemyTag>())
{
    // Only process enemies
}
```

**Benefits:**
- ✅ Zero memory overhead (empty struct)
- ✅ Fast filtering (archetype-based)
- ✅ Clear intent (semantic meaning)

### Data Components

**Use for storing values:**

```csharp
// Data component
public struct Health : IComponentData
{
    public float Current;
    public float Max;
}

// Usage: Read/write values
foreach (var health in SystemAPI.Query<RefRW<Health>>())
{
    health.ValueRW.Current -= damage;
}
```

### When to Use Each

| Use Case | Component Type | Example |
|----------|----------------|---------|
| **Filtering** | Tag | `EnemyTag`, `PlayerTag` |
| **State** | Data | `Health`, `Velocity` |
| **Configuration** | Shared Component | `WeaponConfig` |
| **One-shot commands** | Enableable Data | `AttackCommand` |

---

## Shared Components

### Use for Configuration

**Shared components store one copy per archetype:**

```csharp
// Shared component (configuration)
public struct WeaponConfig : ISharedComponentData
{
    public float Damage;
    public float Range;
    public float FireRate;
}

// Entities share same config
var archetype = CreateArchetype(
    typeof(LocalTransform),
    typeof(WeaponConfig),  // Shared (one copy)
    typeof(WeaponState)    // Per-entity
);

// All entities with this archetype share same WeaponConfig
```

**Benefits:**
- ✅ Memory efficient (one copy per archetype)
- ✅ Easy to update (change affects all entities)
- ✅ Good for configuration data

**Limitations:**
- ❌ Can't modify in jobs (read-only)
- ❌ Archetype change when modified
- ❌ Not suitable for per-entity data

---

## Enable/Disable Components

### EnableableComponent Pattern

**Toggle components without structural changes:**

```csharp
// Make component enableable
public struct AttackCommand : IComponentData, IEnableableComponent
{
    public float3 TargetPosition;
    public float Damage;
}

// Enable component (no archetype change)
SystemAPI.SetComponentEnabled<AttackCommand>(entity, true);

// Disable component (no archetype change)
SystemAPI.SetComponentEnabled<AttackCommand>(entity, false);

// Check if enabled
if (SystemAPI.IsComponentEnabled<AttackCommand>(entity))
{
    // Process attack
}
```

### Query Filtering

**Queries filter enabled by default:**

```csharp
// Only processes enabled components (default)
foreach (var attack in SystemAPI.Query<RefRO<AttackCommand>>())
{
    // Automatically skips disabled components
}

// Include disabled components
foreach (var attack in
    SystemAPI.Query<RefRO<AttackCommand>>()
        .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
{
    // Processes all, even disabled
}
```

### Use Cases

**✅ Ideal for:**
- One-shot commands (enable → process → disable)
- State flags (enabled/disabled states)
- Temporary effects (buffs/debuffs)

**❌ Not ideal for:**
- Permanent state (use tag components)
- Frequently toggled (causes query changes)

---

## Component vs DynamicBuffer

### When to Use Components

**Single value per entity:**

```csharp
// ✅ Component: One value
public struct Health : IComponentData
{
    public float Current;
    public float Max;
}
```

### When to Use DynamicBuffer

**Multiple values per entity:**

```csharp
// ✅ DynamicBuffer: Multiple values
public struct InventorySlot : IBufferElementData
{
    public ResourceTypeId Type;
    public int Quantity;
}

// Entity can have multiple InventorySlot elements
var inventory = SystemAPI.GetBuffer<InventorySlot>(entity);
inventory.Add(new InventorySlot { Type = ResourceTypeId.Wood, Quantity = 10 });
inventory.Add(new InventorySlot { Type = ResourceTypeId.Stone, Quantity = 5 });
```

### Decision Matrix

| Requirement | Use |
|-------------|-----|
| Single value | Component |
| Multiple values | DynamicBuffer |
| Variable count | DynamicBuffer |
| Fixed count (small) | Component array |
| Fixed count (large) | DynamicBuffer |

---

## Component Lifecycle Patterns

### Cleanup Components

**Mark entities for cleanup:**

```csharp
// Cleanup tag
public struct DestroyTag : IComponentData { }

// System marks for cleanup
foreach (var (health, entity) in
    SystemAPI.Query<RefRW<Health>>().WithEntityAccess())
{
    if (health.ValueRO.Current <= 0)
    {
        ecb.AddComponent<DestroyTag>(entity);
    }
}

// Cleanup system destroys
foreach (var entity in SystemAPI.Query<Entity>().WithAll<DestroyTag>())
{
    ecb.DestroyEntity(entity);
}
```

### Initialization Pattern

**Initialize components on spawn:**

```csharp
// Spawn system
var newEntity = ecb.Instantiate(prefabEntity);
ecb.AddComponent(newEntity, new Health { Current = 100, Max = 100 });
ecb.AddComponent(newEntity, new Velocity { Value = float3.zero });

// Initialization system processes new entities
foreach (var (health, entity) in
    SystemAPI.Query<RefRW<Health>>().WithAll<NewEntityTag>())
{
    // Initialize logic
    health.ValueRW.Current = health.ValueRO.Max;
    ecb.RemoveComponent<NewEntityTag>(entity);
}
```

---

## Archetype Stability Considerations

### Minimize Component Changes

**Frequent component changes cause archetype fragmentation:**

```csharp
// ❌ Bad: Frequent archetype changes
foreach (var entity in entities)
{
    if (condition)
        ecb.AddComponent<TagA>(entity);  // Archetype change
    else
        ecb.RemoveComponent<TagA>(entity);  // Another archetype change
}

// ✅ Good: Use enableable components
foreach (var entity in entities)
{
    ecb.SetComponentEnabled<TagA>(entity, condition);  // No archetype change
}
```

### Batch Component Changes

**Batch operations reduce fragmentation:**

```csharp
// ✅ Good: Batch changes
var entitiesToTag = new NativeList<Entity>(Allocator.Temp);
foreach (var (health, entity) in
    SystemAPI.Query<RefRO<Health>>().WithEntityAccess())
{
    if (health.ValueRO.Current < 25)
        entitiesToTag.Add(entity);
}

// Apply all changes together
foreach (var entity in entitiesToTag)
{
    ecb.AddComponent<CriticalHealthTag>(entity);
}
```

---

## Common Patterns

### Pattern 1: State Machine

```csharp
// State components (enableable)
public struct IdleState : IComponentData, IEnableableComponent { }
public struct MovingState : IComponentData, IEnableableComponent { }
public struct AttackingState : IComponentData, IEnableableComponent { }

// State transition system
foreach (var (ai, entity) in
    SystemAPI.Query<RefRO<AIState>>().WithEntityAccess())
{
    // Disable all states
    ecb.SetComponentEnabled<IdleState>(entity, false);
    ecb.SetComponentEnabled<MovingState>(entity, false);
    ecb.SetComponentEnabled<AttackingState>(entity, false);
    
    // Enable current state
    switch (ai.ValueRO.CurrentState)
    {
        case AIStateType.Idle:
            ecb.SetComponentEnabled<IdleState>(entity, true);
            break;
        case AIStateType.Moving:
            ecb.SetComponentEnabled<MovingState>(entity, true);
            break;
        case AIStateType.Attacking:
            ecb.SetComponentEnabled<AttackingState>(entity, true);
            break;
    }
}
```

### Pattern 2: Configuration + State

```csharp
// Shared config
public struct WeaponConfig : ISharedComponentData
{
    public float Damage;
    public float Range;
}

// Per-entity state
public struct WeaponState : IComponentData
{
    public float CooldownTimer;
    public Entity Target;
}

// System uses both
foreach (var (config, state) in
    SystemAPI.Query<RefRO<WeaponConfig>, RefRW<WeaponState>>())
{
    // Use config.Damage, config.Range
    // Modify state.CooldownTimer, state.Target
}
```

### Pattern 3: Tag + Data

```csharp
// Tag for filtering
public struct EnemyTag : IComponentData { }

// Data for values
public struct EnemyData : IComponentData
{
    public byte FactionId;
    public byte AggressionLevel;
}

// Query with both
foreach (var (tag, data) in
    SystemAPI.Query<RefRO<EnemyTag>, RefRW<EnemyData>>())
{
    // Process enemies with data
}
```

---

## Best Practices Summary

1. ✅ **Keep hot components < 128 bytes** (cache-friendly)
2. ✅ **Use tags for filtering** (zero overhead)
3. ✅ **Use data components for values** (read/write)
4. ✅ **Use shared components for config** (memory efficient)
5. ✅ **Use enableable for state flags** (no archetype change)
6. ✅ **Use DynamicBuffer for collections** (multiple values)
7. ✅ **Minimize component changes** (archetype stability)
8. ✅ **Batch component operations** (reduce fragmentation)
9. ❌ **Don't create bloated components** (> 256 bytes)
10. ❌ **Don't use shared components for per-entity data**

---

## Additional Resources

- [Memory Layout Optimization](BestPractices/MemoryLayoutOptimization.md)
- [DOTS 1.4 Patterns](BestPractices/DOTS_1_4_Patterns.md)
- [Component Migration Guide](../Guides/ComponentMigrationGuide.md)

---

*Last Updated: 2025-12-01*
*Maintainer: PureDOTS Framework Team*

