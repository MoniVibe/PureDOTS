# DOTS 1.4.x Implementation Patterns

**Unity DOTS Version**: 1.4.x (Entities 1.4+)
**Last Updated**: 2025-12-01
**Maintainer**: PureDOTS Framework Team

---

## Project Context

**This guide applies to PureDOTS framework development.**

- **PureDOTS**: `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS` - Shared framework package
- **Space4X**: `C:\Users\Moni\Documents\claudeprojects\unity\Space4x` - Separate game project
- **Godgame**: `C:\Users\Moni\Documents\claudeprojects\unity\Godgame` - Separate game project

**⚠️ Important:** PureDOTS code must be **game-agnostic**. Do not reference Space4X or Godgame types from PureDOTS. Game-specific implementations belong in their respective game project directories.

See [TRI_PROJECT_BRIEFING.md](../../TRI_PROJECT_BRIEFING.md) for project separation rules.

---

## Overview

This guide covers **DOTS 1.4.x-specific patterns and best practices** for PureDOTS framework development. If you're upgrading from 1.3.x or starting fresh, this is your implementation reference.

**Key Changes in 1.4.x:**
- `ISystem` is now the default (replaces `SystemBase` for most uses)
- Aspect-oriented queries for reusable logic
- Improved Baker API with relationship support
- Source generators power `SystemAPI`
- Better Burst compatibility across the board

---

## System Authoring

### ISystem vs SystemBase

**Default: Use `ISystem`** (unless you need managed state)

#### ISystem (Recommended)

**When to use:**
- ✅ No managed state needed (no `List<T>`, `GameObject` references, etc.)
- ✅ Performance-critical systems
- ✅ Burst-compilable logic
- ✅ Clean lifecycle (no `OnCreate`/`OnDestroy` for managed cleanup)

**Example:**
```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct VillagerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // No managed allocations here
        state.RequireForUpdate<VillagerTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // All code here is Burst-compiled
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, velocity) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<Velocity>>())
        {
            transform.ValueRW.Position += velocity.ValueRO.Value * deltaTime;
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // Cleanup native containers if needed
    }
}
```

**Performance Benefits:**
- **Zero managed allocations** per frame
- **Burst-compiled** by default
- **Faster iteration** (no managed overhead)

#### SystemBase (Legacy, Special Cases Only)

**When to use:**
- ⚠️ You need managed state (`List<T>`, `Dictionary<K,V>`)
- ⚠️ Unity API integration (e.g., `GameObject.Find`)
- ⚠️ Editor-only systems with complex managed logic

**Example:**
```csharp
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class DebugVisualizationSystem : SystemBase
{
    // Managed state OK in SystemBase
    private List<DebugLine> _debugLines = new();
    private Material _lineMaterial;

    protected override void OnCreate()
    {
        _lineMaterial = Resources.Load<Material>("DebugLineMaterial");
    }

    protected override void OnUpdate()
    {
        _debugLines.Clear();

        // Collect debug data
        foreach (var (transform, debug) in
            SystemAPI.Query<RefRO<LocalTransform>, RefRO<DebugVisualize>>())
        {
            _debugLines.Add(new DebugLine
            {
                Start = transform.ValueRO.Position,
                End = debug.ValueRO.TargetPosition
            });
        }

        // Draw lines (managed Unity API)
        foreach (var line in _debugLines)
        {
            Debug.DrawLine(line.Start, line.End, Color.red);
        }
    }
}
```

**Migration Path:**
If you have a `SystemBase` that doesn't need managed state:
1. Change `class` → `struct`
2. Change `SystemBase` → `ISystem`
3. Change method signatures: `OnCreate()` → `OnCreate(ref SystemState state)`
4. Add `[BurstCompile]` attributes
5. Test and verify performance improvement

---

### SystemAPI Usage Rules

**CRITICAL**: `SystemAPI` can only be used in specific contexts due to source generator requirements.

#### ✅ Allowed: SystemAPI in Lifecycle Methods and Non-Static Helpers

`SystemAPI` can be used in:
- `OnCreate(ref SystemState state)`
- `OnUpdate(ref SystemState state)`
- `OnDestroy(ref SystemState state)`
- **Non-static** helper methods called from the above

**Example (Correct):**
```csharp
[BurstCompile]
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ProcessEntities(ref state);
    }

    // ✅ Non-static helper - SystemAPI is allowed
    [BurstCompile]
    private void ProcessEntities(ref SystemState state)
    {
        var singleton = SystemAPI.GetSingleton<MySingleton>();
        foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>())
        {
            // ...
        }
    }
}
```

#### ❌ Forbidden: SystemAPI in Static Methods

**NEVER** use `SystemAPI` in `static` methods. This causes source generator errors:
- `CS0120`: "An object reference is required for the non-static field"
- `EA0006`: "You may not use the SystemAPI member Query inside of a static method"

**Example (WRONG):**
```csharp
[BurstCompile]
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ProcessEntities(ref state); // ❌ Calls static method with SystemAPI
    }

    // ❌ STATIC method using SystemAPI - THIS WILL FAIL
    [BurstCompile]
    private static void ProcessEntities(ref SystemState state)
    {
        var singleton = SystemAPI.GetSingleton<MySingleton>(); // ❌ ERROR
        foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>()) // ❌ ERROR
        {
            // ...
        }
    }
}
```

#### Fix Option A: Remove `static` (Recommended)

Simply remove `static` from the helper method:

```csharp
// ✅ Fixed: Non-static helper
[BurstCompile]
private void ProcessEntities(ref SystemState state)
{
    var singleton = SystemAPI.GetSingleton<MySingleton>(); // ✅ Works
    foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>()) // ✅ Works
    {
        // ...
    }
}
```

#### Fix Option B: Use `state.EntityManager` Instead

If you must keep the method `static`, use `state.EntityManager` and `state.GetEntityQuery` instead of `SystemAPI`:

```csharp
// ✅ Alternative: Static method using EntityManager directly
[BurstCompile]
private static void ProcessEntities(ref SystemState state)
{
    // Use EntityManager instead of SystemAPI
    var query = state.GetEntityQuery(ComponentType.ReadOnly<MySingleton>());
    if (!query.IsEmpty)
    {
        var singleton = query.GetSingleton<MySingleton>();
    }

    var transformQuery = state.GetEntityQuery(ComponentType.ReadWrite<LocalTransform>());
    var transforms = transformQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
    // Process transforms...
    transforms.Dispose();
}
```

**Recommendation**: Prefer Option A (remove `static`) for cleaner, more maintainable code.

#### Summary

| Context | SystemAPI Allowed? |
|---------|-------------------|
| `OnCreate`, `OnUpdate`, `OnDestroy` | ✅ Yes |
| Non-static helper methods | ✅ Yes |
| Static helper methods | ❌ No (use `state.EntityManager` instead) |
| Static utility classes | ❌ No (use `state.EntityManager` or pass `EntityManager`) |

### SystemAPI.Query Patterns

**New in 1.4**: `SystemAPI.Query` with source generators replaces `Entities.ForEach`.

#### Basic Queries

**Read-Write Access:**
```csharp
foreach (var (transform, health) in
    SystemAPI.Query<RefRW<LocalTransform>, RefRW<Health>>())
{
    // Modify both components
    transform.ValueRW.Position += float3.up * health.ValueRO.Percent;
    health.ValueRW.Current -= damage;
}
```

**Read-Only Access:**
```csharp
foreach (var (transform, velocity) in
    SystemAPI.Query<RefRO<LocalTransform>, RefRO<Velocity>>())
{
    // Read-only, compiler-enforced
    Debug.Log($"Entity at {transform.ValueRO.Position} moving {velocity.ValueRO.Value}");
}
```

**Entity Access:**
```csharp
foreach (var (health, entity) in
    SystemAPI.Query<RefRW<Health>>().WithEntityAccess())
{
    if (health.ValueRO.Current <= 0)
    {
        // Need entity reference for structural change
        state.EntityManager.DestroyEntity(entity);
    }
}
```

#### Query Filters

**WithAll<T>** - Require component, don't access:
```csharp
foreach (var transform in
    SystemAPI.Query<RefRW<LocalTransform>>()
        .WithAll<PlayerTag>())  // Must have PlayerTag, but don't need value
{
    // Only process player entities
}
```

**WithAny<T1, T2>** - Require at least one:
```csharp
foreach (var entity in
    SystemAPI.Query<Entity>()
        .WithAny<EnemyTag, NeutralTag>())  // Has EnemyTag OR NeutralTag
{
    // Process non-allied entities
}
```

**WithNone<T>** - Exclude entities:
```csharp
foreach (var ai in
    SystemAPI.Query<RefRW<AIState>>()
        .WithNone<Disabled>())  // Skip disabled entities
{
    // Process only enabled AI
}
```

**WithOptions** - Change iteration behavior:
```csharp
// Include disabled entities (normally excluded)
foreach (var health in
    SystemAPI.Query<RefRO<Health>>()
        .WithOptions(EntityQueryOptions.IncludeDisabledEntities))
{
    // Process all entities, even disabled
}

// Filter by enableable component
foreach (var attack in
    SystemAPI.Query<RefRO<AttackCommand>>()
        .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
{
    // Process all AttackCommand components, even if disabled
}
```

#### Performance Patterns

**Cache Queries** - Don't recreate every frame:
```csharp
[BurstCompile]
public partial struct OptimizedSystem : ISystem
{
    private EntityQuery _villagerQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Cache query in OnCreate
        _villagerQuery = SystemAPI.QueryBuilder()
            .WithAll<VillagerTag>()
            .WithAllRW<LocalTransform, Velocity>()
            .Build();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Use cached query (no allocation)
        foreach (var (transform, velocity) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRW<Velocity>>()
                .WithAll<VillagerTag>())
        {
            // Process...
        }
    }
}
```

**Chunk Iteration** - For chunk-level operations:
```csharp
[BurstCompile]
public partial struct ChunkProcessingSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transforms, velocities) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<Velocity>>()
                .WithChunkAccess())
        {
            // Process entire chunk at once
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].ValueRW.Position += velocities[i].ValueRO.Value;
            }
        }
    }
}
```

---

### Aspect-Oriented Queries (New in 1.4)

**Aspects** encapsulate common query patterns for reusability.

#### Defining Aspects

```csharp
public readonly partial struct VillagerAspect : IAspect
{
    // Required: Entity reference
    public readonly Entity Entity;

    // Required: Component access
    public readonly RefRW<LocalTransform> Transform;
    public readonly RefRO<VillagerId> Id;
    public readonly RefRW<VillagerNeeds> Needs;

    // Optional: Enableable component check
    public readonly EnabledRefRW<Working> IsWorking;

    // Helper properties
    public float3 Position
    {
        get => Transform.ValueRO.Position;
        set => Transform.ValueRW.Position = value;
    }

    public bool IsHungry => Needs.ValueRO.Hunger > 80;

    // Helper methods
    public void MoveTowards(float3 target, float speed, float deltaTime)
    {
        var direction = math.normalize(target - Position);
        Position += direction * speed * deltaTime;
    }

    public void ConsumeFood(float amount)
    {
        var needs = Needs.ValueRW;
        needs.Hunger = math.max(0, needs.Hunger - amount);
        Needs.ValueRW = needs;
    }
}
```

#### Using Aspects

```csharp
[BurstCompile]
public partial struct VillagerAISystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Query by aspect - much cleaner!
        foreach (var villager in SystemAPI.Query<VillagerAspect>())
        {
            if (villager.IsHungry && !villager.IsWorking.ValueRO)
            {
                var nearestFood = FindNearestFood(villager.Position);
                villager.MoveTowards(nearestFood, speed: 5f, state.Time.DeltaTime);
            }
        }
    }
}
```

**Benefits:**
- ✅ **Reusable** - Use aspect across multiple systems
- ✅ **Readable** - Domain logic instead of component access boilerplate
- ✅ **Maintainable** - Change aspect definition, all systems update
- ✅ **Testable** - Test aspect helpers independently

#### Aspect Best Practices

**DO:**
- ✅ Group related components logically (e.g., `TransformAspect`, `CombatAspect`)
- ✅ Add helper properties for common calculations
- ✅ Use `RefRO` for read-only, `RefRW` for read-write
- ✅ Document aspect purpose and expected usage

**DON'T:**
- ❌ Make "god aspects" with 10+ components
- ❌ Add stateful logic (aspects are views, not systems)
- ❌ Access other entities (aspects represent single entity)

---

## Entity Creation & Baking

### Baker Patterns

**Bakers** convert MonoBehaviour authoring → ECS components at bake time.

#### Basic Baker

```csharp
// Authoring MonoBehaviour
public class VillagerAuthoring : MonoBehaviour
{
    public float moveSpeed = 5f;
    public int initialHealth = 100;
}

// Baker
public class VillagerBaker : Baker<VillagerAuthoring>
{
    public override void Bake(VillagerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent(entity, new VillagerId { Value = GetInstanceID() });
        AddComponent(entity, new MoveSpeed { Value = authoring.moveSpeed });
        AddComponent(entity, new Health
        {
            Current = authoring.initialHealth,
            Max = authoring.initialHealth
        });
        AddComponent<VillagerTag>(entity);
    }
}
```

#### Entity Relationships

**Parent-Child:**
```csharp
public class WeaponAuthoring : MonoBehaviour
{
    public Transform mountPoint;
}

public class WeaponBaker : Baker<WeaponAuthoring>
{
    public override void Bake(WeaponAuthoring authoring)
    {
        var weaponEntity = GetEntity(TransformUsageFlags.Dynamic);

        // Set parent relationship
        if (authoring.mountPoint != null)
        {
            var parentEntity = GetEntity(authoring.mountPoint, TransformUsageFlags.Dynamic);
            AddComponent(weaponEntity, new Parent { Value = parentEntity });
        }

        AddComponent(weaponEntity, new WeaponData { ... });
    }
}
```

**Additional Entities:**
```csharp
public class VillagerWithInventoryBaker : Baker<VillagerAuthoring>
{
    public override void Bake(VillagerAuthoring authoring)
    {
        var villagerEntity = GetEntity(TransformUsageFlags.Dynamic);

        // Create companion entity for inventory
        var inventoryEntity = CreateAdditionalEntity(TransformUsageFlags.None);
        AddComponent(inventoryEntity, new InventoryData
        {
            Capacity = authoring.inventorySize
        });
        AddBuffer<InventorySlot>(inventoryEntity);

        // Link main entity to inventory
        AddComponent(villagerEntity, new InventoryRef
        {
            Entity = inventoryEntity
        });
    }
}
```

#### Dependency Tracking

**Explicit Dependencies:**
```csharp
public class CombatantBaker : Baker<CombatantAuthoring>
{
    public override void Bake(CombatantAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        // Depend on external ScriptableObject
        DependsOn(authoring.weaponConfig);  // Rebake if config changes

        AddComponent(entity, new WeaponStats
        {
            Damage = authoring.weaponConfig.baseDamage,
            Range = authoring.weaponConfig.range
        });
    }
}
```

#### Prefab Baking

**Prefab References:**
```csharp
public class SpawnerBaker : Baker<SpawnerAuthoring>
{
    public override void Bake(SpawnerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        // Convert prefab reference to entity reference
        var prefabEntity = GetEntity(authoring.spawnPrefab, TransformUsageFlags.Dynamic);

        AddComponent(entity, new Spawner
        {
            PrefabEntity = prefabEntity,
            SpawnRate = authoring.spawnRate
        });
    }
}
```

---

### Blob Asset Authoring

**Blob assets** are immutable, shared data structures for read-only configuration.

#### Basic Blob Creation

```csharp
public struct WeaponCatalogBlob
{
    public BlobArray<WeaponDefinition> Weapons;
}

public struct WeaponDefinition
{
    public int WeaponId;
    public float Damage;
    public float Range;
    public BlobString Name;
}

public class WeaponCatalogBaker : Baker<WeaponCatalogAuthoring>
{
    public override void Bake(WeaponCatalogAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);

        // Build blob asset
        var builder = new BlobBuilder(Allocator.Temp);
        ref var catalog = ref builder.ConstructRoot<WeaponCatalogBlob>();

        var weaponArray = builder.Allocate(ref catalog.Weapons, authoring.weapons.Length);

        for (int i = 0; i < authoring.weapons.Length; i++)
        {
            weaponArray[i] = new WeaponDefinition
            {
                WeaponId = authoring.weapons[i].id,
                Damage = authoring.weapons[i].damage,
                Range = authoring.weapons[i].range
            };
            builder.AllocateString(ref weaponArray[i].Name, authoring.weapons[i].name);
        }

        var blobRef = builder.CreateBlobAssetReference<WeaponCatalogBlob>(Allocator.Persistent);
        builder.Dispose();

        // Add blob reference to entity
        AddBlobAsset(ref blobRef, out var hash);  // Registers for cleanup
        AddComponent(entity, new WeaponCatalog
        {
            Blob = blobRef
        });
    }
}
```

#### Blob Asset Sharing

```csharp
// Multiple entities can share the same blob
public class CombatantWithWeaponsBaker : Baker<CombatantAuthoring>
{
    public override void Bake(CombatantAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        // Reference shared catalog
        var catalogEntity = GetEntity(authoring.weaponCatalog, TransformUsageFlags.None);
        var catalog = GetComponent<WeaponCatalog>(catalogEntity);

        // Store reference (no copy, just pointer)
        AddComponent(entity, new AvailableWeapons
        {
            CatalogBlob = catalog.Blob  // Shared reference
        });
    }
}
```

**Benefits:**
- ✅ **Zero per-entity cost** (shared blob)
- ✅ **Immutable** (thread-safe, no accidental modification)
- ✅ **Cache-friendly** (contiguous memory)
- ✅ **Automatic cleanup** (registered with `AddBlobAsset`)

---

## Structural Changes

### EntityCommandBuffer Strategies

**When to use ECB:**
- ✅ Inside jobs (can't use EntityManager directly)
- ✅ Deferred structural changes (batch multiple changes)
- ✅ Parallel writes (use ParallelWriter)

**When to use EntityManager:**
- ✅ Single-threaded context (OnCreate, OnDestroy)
- ✅ Immediate changes needed (before query in same frame)
- ✅ Singleton setup/teardown

#### Basic ECB Usage

```csharp
[BurstCompile]
public partial struct SpawnSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (spawner, entity) in
            SystemAPI.Query<RefRW<Spawner>>().WithEntityAccess())
        {
            spawner.ValueRW.CooldownTimer -= SystemAPI.Time.DeltaTime;

            if (spawner.ValueRO.CooldownTimer <= 0)
            {
                // Instantiate prefab
                var newEntity = ecb.Instantiate(spawner.ValueRO.PrefabEntity);

                // Set position
                ecb.SetComponent(newEntity, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition));

                // Reset cooldown
                spawner.ValueRW.CooldownTimer = spawner.ValueRO.SpawnRate;
            }
        }
    }
}
```

#### ParallelWriter for Jobs

```csharp
[BurstCompile]
public partial struct ParallelSpawnSystem : ISystem
{
    [BurstCompile]
    private partial struct SpawnJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        [BurstCompile]
        private void Execute([ChunkIndexInQuery] int sortKey, in Spawner spawner, in LocalTransform transform)
        {
            if (spawner.ShouldSpawn)
            {
                var newEntity = Ecb.Instantiate(sortKey, spawner.PrefabEntity);
                Ecb.SetComponent(sortKey, newEntity, LocalTransform.FromPosition(transform.Position));
            }
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        new SpawnJob { Ecb = ecb }.ScheduleParallel();
    }
}
```

**Key:** `sortKey` (from `[ChunkIndexInQuery]`) ensures deterministic playback order.

---

## Enable/Disable Components

**New in 1.4**: Toggle components without structural changes.

### Definition

```csharp
// Make component enableable
public struct AttackCommand : IComponentData, IEnableableComponent
{
    public float3 TargetPosition;
    public float Damage;
}
```

### Usage

```csharp
// Enable component (command is now active)
state.EntityManager.SetComponentEnabled<AttackCommand>(entity, true);

// Disable component (command is inactive)
state.EntityManager.SetComponentEnabled<AttackCommand>(entity, false);

// Check if enabled
if (state.EntityManager.IsComponentEnabled<AttackCommand>(entity))
{
    // Process attack
}
```

### Query Filtering

```csharp
// Only process enabled components (default behavior)
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

**Benefits:**
- ✅ **No archetype change** (fast)
- ✅ **No memory reallocation** (stable pointers)
- ✅ **Ideal for one-shot commands** (enable → process → disable)

---

## Performance Patterns

### Chunk Utilization

**Goal:** Maximize entities per chunk (better cache utilization).

**Archetype Size:**
```csharp
// Small archetype (good)
Entity + LocalTransform + Velocity + Health = ~80 bytes per entity
→ ~500 entities per 16KB chunk

// Large archetype (bad)
Entity + 10 components (200 bytes total) = 200 bytes per entity
→ ~80 entities per 16KB chunk
```

**Strategy:** Split hot/cold data to smaller archetypes.

### Burst Compilation Tips

**Always Burst-compile hot paths:**
```csharp
[BurstCompile]  // On struct
public partial struct HotPathSystem : ISystem
{
    [BurstCompile]  // On each method
    public void OnUpdate(ref SystemState state) { ... }
}
```

**Check Burst Inspector:**
```
Window → Burst → Burst Inspector
→ Select your system
→ Check for:
  - Red lines (not Burst-compiled)
  - Loop vectorization (SIMD)
  - Branch prediction stats
```

---

## Source Generators

**What gets generated:**
- `SystemAPI.Query` → Efficient query code
- `IJobEntity` → Job structs
- Aspects → Component access code

**Troubleshooting:**
- **Error: "No suitable method found for..."**
  - Ensure `partial` keyword on system struct
  - Check method signatures match expected patterns
- **IDE not showing generated code:**
  - Rebuild solution
  - Close/reopen IDE
  - Check `obj/` folder for generated files

---

## Testing Patterns

### Entity Test Framework

```csharp
[Test]
public void VillagerMovementSystem_MovesEntities()
{
    using var world = new World("TestWorld");
    var entity = world.EntityManager.CreateEntity();

    world.EntityManager.AddComponentData(entity, LocalTransform.Identity);
    world.EntityManager.AddComponentData(entity, new Velocity { Value = new float3(1, 0, 0) });

    var system = world.GetOrCreateSystem<VillagerMovementSystem>();
    system.Update(world.Unmanaged);

    var finalTransform = world.EntityManager.GetComponentData<LocalTransform>(entity);
    Assert.Greater(finalTransform.Position.x, 0);

    world.Dispose();
}
```

---

## Migration from 1.3.x

### API Renames

| 1.3.x | 1.4.x |
|-------|-------|
| `Entities.ForEach` | `SystemAPI.Query` |
| `Job.WithCode` | `IJobEntity` or manual jobs |
| `ComponentSystemBase` | `SystemBase` or `ISystem` |
| `GetComponentDataFromEntity` | `ComponentLookup<T>` |

### Behavior Changes

- **Default EnabledComponent behavior**: Queries now filter enabled by default
- **Baker dependencies**: Must explicitly call `DependsOn` for ScriptableObject refs
- **Blob disposal**: Use `AddBlobAsset` for automatic cleanup

---

## Common Pitfalls

### 1. Forgetting `ref SystemState state`
```csharp
// ❌ Wrong
public void OnUpdate(ref SystemState state)
{
    foreach (var transform in SystemAPI.Query<LocalTransform>()) { }
    // SystemAPI needs state context
}

// ✅ Correct
public void OnUpdate(ref SystemState state)
{
    foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>()) { }
}
```

### 2. Modifying read-only refs
```csharp
// ❌ Won't compile
foreach (var health in SystemAPI.Query<RefRO<Health>>())
{
    health.ValueRW.Current -= 10;  // Error: ValueRW not available on RefRO
}

// ✅ Correct
foreach (var health in SystemAPI.Query<RefRW<Health>>())
{
    health.ValueRW.Current -= 10;
}
```

### 3. Archetype fragmentation
```csharp
// ❌ Bad: Creates 100 archetypes
for (int i = 0; i < 100; i++)
{
    var entity = state.EntityManager.CreateEntity();
    if (i % 2 == 0) state.EntityManager.AddComponent<TagA>(entity);
    if (i % 3 == 0) state.EntityManager.AddComponent<TagB>(entity);
    if (i % 5 == 0) state.EntityManager.AddComponent<TagC>(entity);
}

// ✅ Good: Batch by archetype
var archetypeA = state.EntityManager.CreateArchetype(typeof(TagA));
var archetypeB = state.EntityManager.CreateArchetype(typeof(TagA), typeof(TagB));
state.EntityManager.CreateEntity(archetypeA, 50, Allocator.Temp);
state.EntityManager.CreateEntity(archetypeB, 50, Allocator.Temp);
```

---

## Additional Resources

- [Unity DOTS Manual](https://docs.unity3d.com/Packages/com.unity.entities@latest)
- [Unity DOTS Forums](https://forum.unity.com/forums/data-oriented-technology-stack.147/)
- [Burst Compiler Docs](https://docs.unity3d.com/Packages/com.unity.burst@latest)
- [Job System Docs](https://docs.unity3d.com/Manual/JobSystem.html)

---

*Last Updated: 2025-12-01*
*Maintainer: PureDOTS Framework Team*
