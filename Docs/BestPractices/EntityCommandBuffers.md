# Entity Command Buffers

**Last Updated**: 2025-12-01
**Maintainer**: PureDOTS Framework Team

---

## Project Context

**This guide applies to all three projects:**

- **PureDOTS**: `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS` - Framework code
- **Space4X**: `C:\Users\Moni\Documents\claudeprojects\unity\Space4x` - Game code
- **Godgame**: `C:\Users\Moni\Documents\claudeprojects\unity\Godgame` - Game code

**⚠️ Important:** When writing PureDOTS framework code, ensure it remains game-agnostic. ECB patterns apply to all projects.

See [PROJECT_SEPARATION.md](PROJECT_SEPARATION.md) for project separation rules.

---

## Overview

EntityCommandBuffers (ECB) enable deferred structural changes to entities. This guide covers when to use ECB vs EntityManager, playback timing, and parallel patterns.

**Key Use Cases:**
- ✅ Structural changes in jobs (can't use EntityManager directly)
- ✅ Deferred changes (batch multiple operations)
- ✅ Parallel writes (ParallelWriter for multi-threaded jobs)

**Structural Changes:**
- Creating/destroying entities
- Adding/removing components
- Setting component data
- Enabling/disabling components

---

## When to Use ECB vs EntityManager

### Use EntityManager When:

**✅ Single-threaded context:**
- `OnCreate`, `OnDestroy` methods
- Immediate changes needed (before query in same frame)
- Singleton setup/teardown

```csharp
public partial struct SetupSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // ✅ OK: Single-threaded context
        var entity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<MyComponent>(entity);
    }
}
```

### Use ECB When:

**✅ Inside jobs:**
- `IJob`, `IJobChunk`, `IJobEntity`
- Can't use EntityManager directly

**✅ Deferred changes:**
- Batch multiple operations
- Better performance (fewer structural changes)

**✅ Parallel writes:**
- Multiple threads writing commands
- Use `ParallelWriter`

```csharp
[BurstCompile]
public partial struct SpawnJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;  // ✅ Required in job

    [BurstCompile]
    private void Execute([ChunkIndexInQuery] int sortKey, in Spawner spawner)
    {
        if (spawner.ShouldSpawn)
        {
            var entity = Ecb.Instantiate(sortKey, spawner.PrefabEntity);
            Ecb.SetComponent(sortKey, entity, new LocalTransform { ... });
        }
    }
}
```

---

## Basic ECB Usage

### Creating ECB

```csharp
[BurstCompile]
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Get ECB from system
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Use ECB
        foreach (var (health, entity) in
            SystemAPI.Query<RefRW<Health>>().WithEntityAccess())
        {
            if (health.ValueRO.Current <= 0)
            {
                ecb.DestroyEntity(entity);  // Deferred destruction
            }
        }
    }
}
```

### ECB System Groups

**Common ECB systems:**

| System | When Played Back | Use Case |
|--------|------------------|----------|
| `BeginSimulationEntityCommandBufferSystem` | Before Simulation | Setup, initialization |
| `EndSimulationEntityCommandBufferSystem` | After Simulation | Cleanup, destruction |
| `BeginPresentationEntityCommandBufferSystem` | Before Presentation | UI updates |

---

## Playback Timing

### When Commands Execute

**ECB commands execute when the ECB system updates:**

```csharp
// Frame timeline:
// 1. InitializationSystemGroup
//    - InputReadingSystem (reads input)
// 2. BeginSimulationEntityCommandBufferSystem
//    - Playback: Commands from previous frame
// 3. SimulationSystemGroup
//    - MovementSystem (writes commands to ECB)
//    - CombatSystem (writes commands to ECB)
// 4. EndSimulationEntityCommandBufferSystem
//    - Playback: Commands from Simulation systems
// 5. PresentationSystemGroup
```

### Command Order

**Commands execute in order they were added:**

```csharp
ecb.AddComponent(entity, new ComponentA());
ecb.AddComponent(entity, new ComponentB());
ecb.RemoveComponent<ComponentA>(entity);

// Playback order:
// 1. Add ComponentA
// 2. Add ComponentB
// 3. Remove ComponentA
// Result: Entity has ComponentB only
```

---

## ParallelWriter Patterns

### Basic ParallelWriter

**Use for parallel jobs:**

```csharp
[BurstCompile]
public partial struct ParallelSpawnJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    public Entity PrefabEntity;

    [BurstCompile]
    private void Execute([ChunkIndexInQuery] int sortKey, 
        in Spawner spawner, in LocalTransform transform)
    {
        if (spawner.ShouldSpawn)
        {
            // sortKey ensures deterministic playback order
            var newEntity = Ecb.Instantiate(sortKey, PrefabEntity);
            Ecb.SetComponent(sortKey, newEntity, 
                LocalTransform.FromPosition(transform.Position));
        }
    }
}

// Schedule
var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

new ParallelSpawnJob
{
    Ecb = ecb,
    PrefabEntity = prefabEntity
}.ScheduleParallel(query, dependency);
```

### Sort Key Importance

**`[ChunkIndexInQuery]` ensures deterministic order:**

```csharp
// ✅ Correct: Deterministic order
private void Execute([ChunkIndexInQuery] int sortKey, ...)
{
    Ecb.AddComponent(sortKey, entity, new Component());
}

// ❌ Wrong: Non-deterministic (may cause desyncs)
private void Execute(...)
{
    Ecb.AddComponent(entity, new Component());  // No sortKey!
}
```

---

## Avoiding Duplicate Commands

### Problem: Multiple Commands on Same Entity

**❌ Bad: Multiple commands**

```csharp
// Multiple systems might add same component
ecb.AddComponent(entity, new Health { Current = 100 });
ecb.AddComponent(entity, new Health { Current = 50 });  // Overwrites first!

// Result: Entity gets Health with Current = 50 (non-deterministic)
```

**✅ Good: Check before adding**

```csharp
if (!SystemAPI.HasComponent<Health>(entity))
{
    ecb.AddComponent(entity, new Health { Current = 100 });
}
```

### Pattern: Command Deduplication

```csharp
// Use enableable component to track pending commands
public struct PendingHealth : IComponentData, IEnableableComponent { }

// System 1: Mark for health addition
if (condition)
{
    SystemAPI.SetComponentEnabled<PendingHealth>(entity, true);
}

// System 2: Add health if marked
foreach (var (pending, entity) in
    SystemAPI.Query<RefRO<PendingHealth>>().WithEntityAccess())
{
    if (pending.ValueRO.Enabled)
    {
        ecb.AddComponent(entity, new Health { Current = 100 });
        ecb.SetComponentEnabled<PendingHealth>(entity, false);
    }
}
```

---

## Deferred Structural Changes

### Batching Operations

**ECB batches operations for efficiency:**

```csharp
// ✅ Good: Batch multiple operations
var ecb = GetECB();

foreach (var (health, entity) in
    SystemAPI.Query<RefRW<Health>>().WithEntityAccess())
{
    if (health.ValueRO.Current <= 0)
    {
        ecb.DestroyEntity(entity);  // Batched destruction
    }
    else if (health.ValueRO.Current < 25)
    {
        ecb.AddComponent<CriticalHealthTag>(entity);  // Batched addition
    }
}

// All operations execute together (more efficient)
```

### Performance Benefits

**Batching reduces structural change overhead:**
- Fewer archetype changes
- Better cache utilization
- Reduced memory allocations

---

## Debugging ECB Issues

### Common Problems

| Problem | Cause | Solution |
|---------|-------|----------|
| **Commands not executing** | Wrong ECB system | Check system update order |
| **Non-deterministic order** | Missing sortKey | Add `[ChunkIndexInQuery]` |
| **Duplicate commands** | Multiple systems | Check before adding |
| **Entity destroyed before command** | Timing issue | Use different ECB system |

### Debug Visualization

**Enable ECB debug visualization:**

```csharp
// In editor, enable ECB debugging
#if UNITY_EDITOR
var ecb = GetECB();
ecb.SetName(entity, "DebugName");  // Helps identify entities
#endif
```

---

## Performance Impact Analysis

### When ECB Helps

**✅ Benefits:**
- Batching reduces overhead
- Parallel writes enable multi-threading
- Deferred changes avoid immediate archetype changes

### When ECB Hurts

**❌ Overhead:**
- Command storage (memory)
- Playback overhead (CPU)
- Delayed execution (one frame delay)

**Guideline:** Use ECB when needed (jobs, batching), avoid for simple single-threaded operations.

---

## Common Patterns

### Pattern 1: Spawn System

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
            spawner.ValueRW.Timer -= SystemAPI.Time.DeltaTime;

            if (spawner.ValueRO.Timer <= 0)
            {
                var newEntity = ecb.Instantiate(spawner.ValueRO.PrefabEntity);
                ecb.SetComponent(newEntity, 
                    LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition));
                
                spawner.ValueRW.Timer = spawner.ValueRO.SpawnRate;
            }
        }
    }
}
```

### Pattern 2: Cleanup System

```csharp
[BurstCompile]
public partial struct CleanupSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Cleanup dead entities
        foreach (var (health, entity) in
            SystemAPI.Query<RefRO<Health>>().WithEntityAccess())
        {
            if (health.ValueRO.Current <= 0)
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}
```

### Pattern 3: Component State Machine

```csharp
// Use enableable components for state
public struct AttackState : IComponentData, IEnableableComponent { }
public struct DefendState : IComponentData, IEnableableComponent { }

[BurstCompile]
public partial struct CombatStateSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (ai, entity) in
            SystemAPI.Query<RefRO<AIState>>().WithEntityAccess())
        {
            // Transition states via enableable components
            if (ai.ValueRO.ShouldAttack)
            {
                ecb.SetComponentEnabled<AttackState>(entity, true);
                ecb.SetComponentEnabled<DefendState>(entity, false);
            }
            else if (ai.ValueRO.ShouldDefend)
            {
                ecb.SetComponentEnabled<AttackState>(entity, false);
                ecb.SetComponentEnabled<DefendState>(entity, true);
            }
        }
    }
}
```

---

## Best Practices Summary

1. ✅ **Use ECB in jobs** (can't use EntityManager)
2. ✅ **Use ParallelWriter** for parallel jobs (with sortKey)
3. ✅ **Always use sortKey** with ParallelWriter (determinism)
4. ✅ **Batch operations** when possible (better performance)
5. ✅ **Check before adding** (avoid duplicate commands)
6. ✅ **Use appropriate ECB system** (BeginSimulation, EndSimulation)
7. ✅ **Understand playback timing** (commands execute next frame)
8. ❌ **Don't use ECB unnecessarily** (EntityManager is fine for single-threaded)
9. ❌ **Don't forget sortKey** (causes non-determinism)
10. ❌ **Don't mix ECB systems** (use consistent system)

---

## Additional Resources

- [DOTS 1.4 Patterns](BestPractices/DOTS_1_4_Patterns.md)
- [Job System Patterns](BestPractices/JobSystemPatterns.md)
- [Determinism Checklist](BestPractices/DeterminismChecklist.md)

---

*Last Updated: 2025-12-01*
*Maintainer: PureDOTS Framework Team*

