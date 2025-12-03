# Job System Patterns

**Unity Job System Version**: Compatible with DOTS 1.4.x
**Last Updated**: 2025-12-01
**Maintainer**: PureDOTS Framework Team

---

## Project Context

**This guide applies to all three projects:**

- **PureDOTS**: `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS` - Framework code
- **Space4X**: `C:\Users\Moni\Documents\claudeprojects\unity\Space4x` - Game code
- **Godgame**: `C:\Users\Moni\Documents\claudeprojects\unity\Godgame` - Game code

**⚠️ Important:** When writing PureDOTS framework code, ensure it remains game-agnostic. Job system patterns apply to all projects.

See [PROJECT_SEPARATION.md](PROJECT_SEPARATION.md) for project separation rules.

---

## Overview

The Unity Job System enables parallel execution of work across multiple CPU cores. This guide covers job types, patterns, and best practices for PureDOTS development.

**Key Benefits:**
- ✅ Parallel execution (utilize all CPU cores)
- ✅ Burst-compatible (high performance)
- ✅ Safety system (prevents race conditions)
- ✅ Dependency management (automatic scheduling)

**Job Types:**
- `IJob` - Single-threaded job
- `IJobParallelFor` - Parallel array processing
- `IJobChunk` - Parallel chunk processing (DOTS)
- `IJobEntity` - Parallel entity processing (DOTS, recommended)

---

## Job Type Selection

### IJob (Single-Threaded)

**Use when:**
- Small amount of work
- Sequential processing required
- Setup/teardown operations

```csharp
[BurstCompile]
public struct InitializeJob : IJob
{
    public NativeArray<float> Data;

    public void Execute()
    {
        // Single-threaded execution
        for (int i = 0; i < Data.Length; i++)
        {
            Data[i] = 0f;
        }
    }
}

// Schedule
var job = new InitializeJob { Data = data };
job.Schedule();
```

### IJobParallelFor (Array Processing)

**Use when:**
- Processing arrays/NativeArrays
- Independent operations
- Not using DOTS entities

```csharp
[BurstCompile]
public struct ProcessArrayJob : IJobParallelFor
{
    public NativeArray<float> Input;
    public NativeArray<float> Output;

    public void Execute(int index)
    {
        // Parallel execution (one thread per index)
        Output[index] = Input[index] * 2f;
    }
}

// Schedule parallel
var job = new ProcessArrayJob 
{ 
    Input = inputArray, 
    Output = outputArray 
};
job.ScheduleParallel(inputArray.Length, 64, dependency);
```

### IJobChunk (Chunk Processing)

**Use when:**
- Processing DOTS entities by chunk
- Need chunk-level operations
- Custom chunk iteration logic

```csharp
[BurstCompile]
public struct ProcessChunkJob : IJobChunk
{
    public ComponentTypeHandle<LocalTransform> TransformTypeHandle;
    public ComponentTypeHandle<Velocity> VelocityTypeHandle;
    public float DeltaTime;

    public void Execute(ArchetypeChunk chunk, int chunkIndex, 
        int firstEntityIndex)
    {
        var transforms = chunk.GetNativeArray(ref TransformTypeHandle);
        var velocities = chunk.GetNativeArray(ref VelocityTypeHandle);

        // Process entire chunk
        for (int i = 0; i < chunk.Count; i++)
        {
            transforms[i] = new LocalTransform
            {
                Position = transforms[i].Position + velocities[i].Value * DeltaTime,
                Rotation = transforms[i].Rotation,
                Scale = transforms[i].Scale
            };
        }
    }
}

// Schedule
var job = new ProcessChunkJob
{
    TransformTypeHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(true),
    VelocityTypeHandle = SystemAPI.GetComponentTypeHandle<Velocity>(false),
    DeltaTime = SystemAPI.Time.DeltaTime
};
job.ScheduleParallel(query, dependency);
```

### IJobEntity (Entity Processing) - Recommended

**Use when:**
- Processing DOTS entities
- Most common pattern in DOTS
- Cleaner syntax than IJobChunk

```csharp
[BurstCompile]
public partial struct MovementJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    private void Execute(ref LocalTransform transform, in Velocity velocity)
    {
        // Parallel execution per entity
        transform.Position += velocity.Value * DeltaTime;
    }
}

// Schedule
new MovementJob
{
    DeltaTime = SystemAPI.Time.DeltaTime
}.ScheduleParallel(query, dependency);
```

**Benefits over IJobChunk:**
- ✅ Cleaner syntax (no manual chunk iteration)
- ✅ Type-safe (compiler checks component access)
- ✅ Less boilerplate (no TypeHandle management)

---

## ParallelWriter Patterns

### EntityCommandBuffer.ParallelWriter

**Use for structural changes in parallel jobs:**

```csharp
[BurstCompile]
public partial struct SpawnJob : IJobEntity
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

// Schedule with ParallelWriter
var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

new SpawnJob
{
    Ecb = ecb,
    PrefabEntity = prefabEntity
}.ScheduleParallel(query, dependency);
```

### Sort Key Importance

**`[ChunkIndexInQuery]` ensures deterministic order:**

```csharp
// ✅ Correct: Use sortKey for deterministic playback
private void Execute([ChunkIndexInQuery] int sortKey, ...)
{
    Ecb.AddComponent(sortKey, entity, new Component());
}

// ❌ Wrong: Missing sortKey (non-deterministic)
private void Execute(...)
{
    Ecb.AddComponent(entity, new Component());  // No sortKey!
}
```

---

## Job Dependencies

### Automatic Dependency Management

**Jobs automatically track dependencies:**

```csharp
// Job 1: Process data
var job1 = new ProcessJob { Data = data };
var handle1 = job1.Schedule();

// Job 2: Depends on Job 1
var job2 = new TransformJob { Data = data };
var handle2 = job2.Schedule(handle1);  // Waits for job1

// Job 3: Depends on Job 2
var job3 = new OutputJob { Data = data };
var handle3 = job3.Schedule(handle2);  // Waits for job2
```

### Multiple Dependencies

**Combine multiple dependencies:**

```csharp
var handle1 = job1.Schedule();
var handle2 = job2.Schedule();
var handle3 = job3.Schedule();

// Job 4 waits for all three
var job4 = new CombineJob { ... };
var handle4 = job4.Schedule(JobHandle.CombineDependencies(handle1, handle2, handle3));
```

---

## Read/Write Dependencies

### Safety System

**Unity's safety system prevents race conditions:**

```csharp
[BurstCompile]
public struct ReadWriteJob : IJobParallelFor
{
    [ReadOnly]  // Mark as read-only
    public NativeArray<float> Input;
    
    [WriteOnly]  // Mark as write-only
    public NativeArray<float> Output;

    public void Execute(int index)
    {
        Output[index] = Input[index] * 2f;  // Safe: read Input, write Output
    }
}
```

### Component Access Patterns

**DOTS automatically handles read/write dependencies:**

```csharp
[BurstCompile]
public partial struct MovementJob : IJobEntity
{
    // Read-only: in parameter
    private void Execute(ref LocalTransform transform, in Velocity velocity)
    {
        // Can read velocity, can write transform
        transform.Position += velocity.Value * deltaTime;
    }
}
```

---

## Avoiding Structural Changes in Jobs

### Rule: No Direct EntityManager Access

**❌ Wrong: Direct structural changes**
```csharp
[BurstCompile]
public partial struct BadJob : IJobEntity
{
    public EntityManager EntityManager;  // ❌ Can't use in parallel jobs!

    private void Execute(Entity entity)
    {
        EntityManager.DestroyEntity(entity);  // ❌ Race condition!
    }
}
```

**✅ Correct: Use EntityCommandBuffer**
```csharp
[BurstCompile]
public partial struct GoodJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;

    private void Execute([ChunkIndexInQuery] int sortKey, Entity entity)
    {
        Ecb.DestroyEntity(sortKey, entity);  // ✅ Deferred, safe
    }
}
```

---

## Job Scheduling Best Practices

### Batch Size

**Control parallelization granularity:**

```csharp
// Schedule with batch size (entities per job)
new MovementJob { ... }.ScheduleParallel(query, 64, dependency);
//                                                      ^^
//                                             64 entities per batch
```

**Guidelines:**
- **Small batch (32-64)**: More parallelism, more overhead
- **Large batch (256-512)**: Less overhead, less parallelism
- **Default**: Usually fine, adjust if needed

### Complete vs Schedule

**`Complete()` blocks main thread:**

```csharp
// ❌ Bad: Blocks main thread
var handle = job.Schedule();
handle.Complete();  // Waits here, blocking!

// ✅ Good: Let job run in background
var handle = job.Schedule();
// Continue with other work, complete later if needed
```

---

## Testing Job-Based Systems

### Test World Setup

```csharp
[Test]
public void MovementJob_MovesEntities()
{
    using var world = new World("TestWorld");
    
    // Create entities
    var entity = world.EntityManager.CreateEntity(
        typeof(LocalTransform), 
        typeof(Velocity));
    
    world.EntityManager.SetComponentData(entity, LocalTransform.Identity);
    world.EntityManager.SetComponentData(entity, new Velocity 
    { 
        Value = new float3(1, 0, 0) 
    });

    // Create query
    var query = world.EntityManager.CreateEntityQuery(
        typeof(LocalTransform), 
        typeof(Velocity));

    // Schedule job
    var job = new MovementJob { DeltaTime = 1f };
    var handle = job.ScheduleParallel(query, default);
    handle.Complete();  // Wait for completion in test

    // Assert
    var transform = world.EntityManager.GetComponentData<LocalTransform>(entity);
    Assert.AreEqual(new float3(1, 0, 0), transform.Position);

    world.Dispose();
}
```

---

## Common Patterns

### Pattern 1: Multi-Stage Processing

```csharp
// Stage 1: Calculate forces
var forceJob = new CalculateForcesJob { ... };
var forceHandle = forceJob.ScheduleParallel(query1, dependency);

// Stage 2: Apply forces (depends on stage 1)
var applyJob = new ApplyForcesJob { ... };
var applyHandle = applyJob.ScheduleParallel(query2, forceHandle);

// Stage 3: Update positions (depends on stage 2)
var updateJob = new UpdatePositionsJob { ... };
var updateHandle = updateJob.ScheduleParallel(query3, applyHandle);
```

### Pattern 2: Parallel Reduction

```csharp
[BurstCompile]
public struct SumJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> Values;
    
    [NativeDisableParallelForRestriction]
    public NativeArray<float> Sum;  // Shared accumulator

    public void Execute(int index)
    {
        // Atomic add (thread-safe)
        Interlocked.Add(ref Sum[0], Values[index]);
    }
}
```

### Pattern 3: Chunk-Level Operations

```csharp
[BurstCompile]
public struct ChunkStatsJob : IJobChunk
{
    [ReadOnly]
    public ComponentTypeHandle<Health> HealthTypeHandle;
    
    public NativeArray<int> AliveCount;

    public void Execute(ArchetypeChunk chunk, int chunkIndex, 
        int firstEntityIndex)
    {
        var healths = chunk.GetNativeArray(ref HealthTypeHandle);
        int count = 0;
        
        for (int i = 0; i < chunk.Count; i++)
        {
            if (healths[i].Current > 0)
                count++;
        }
        
        // Thread-safe increment
        Interlocked.Add(ref AliveCount[0], count);
    }
}
```

---

## Performance Tips

### 1. Minimize Job Overhead

**Batch operations when possible:**

```csharp
// ❌ Bad: Many small jobs
for (int i = 0; i < 1000; i++)
{
    var job = new SmallJob { Index = i };
    job.Schedule();  // 1000 jobs = high overhead
}

// ✅ Good: One parallel job
var job = new BatchJob { Count = 1000 };
job.ScheduleParallel(1000, 64, dependency);  // One job, parallel execution
```

### 2. Avoid False Sharing

**Separate data accessed by different threads:**

```csharp
// ❌ Bad: False sharing (same cache line)
public struct BadJob : IJobParallelFor
{
    public NativeArray<float> Data;  // Adjacent elements accessed by different threads
}

// ✅ Good: Separate arrays
public struct GoodJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> Input;
    
    public NativeArray<float> Output;  // Separate memory
}
```

### 3. Use Native Containers Efficiently

**Prefer NativeArray over NativeList when size is known:**

```csharp
// ✅ Good: Fixed size
var array = new NativeArray<float>(count, Allocator.TempJob);

// ⚠️ OK: Dynamic size needed
var list = new NativeList<float>(Allocator.TempJob);
```

---

## Best Practices Summary

1. ✅ **Use `IJobEntity`** for entity processing (cleanest syntax)
2. ✅ **Use `ParallelWriter`** for structural changes in parallel jobs
3. ✅ **Always use `sortKey`** with ParallelWriter (determinism)
4. ✅ **Mark read-only data** with `[ReadOnly]` attribute
5. ✅ **Let jobs run in background** (don't Complete() immediately)
6. ✅ **Batch operations** to minimize job overhead
7. ✅ **Test with Complete()** in unit tests (wait for results)
8. ❌ **Don't use EntityManager** in parallel jobs
9. ❌ **Don't forget sortKey** in ParallelWriter operations
10. ❌ **Don't create many small jobs** (batch instead)

---

## Additional Resources

- [Unity Job System Manual](https://docs.unity3d.com/Manual/JobSystem.html)
- [DOTS 1.4 Patterns](BestPractices/DOTS_1_4_Patterns.md)
- [Entity Command Buffers](BestPractices/EntityCommandBuffers.md)
- [Burst Optimization](BestPractices/BurstOptimization.md)

---

*Last Updated: 2025-12-01*
*Maintainer: PureDOTS Framework Team*

