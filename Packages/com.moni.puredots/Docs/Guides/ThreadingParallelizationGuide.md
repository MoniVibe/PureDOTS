# Threading & Parallelization Guide

**Last Updated**: 2025-01-27  
**Purpose**: Guide for agents implementing systems that use PureDOTS threading infrastructure

---

## Overview

PureDOTS includes advanced threading and parallelization infrastructure inspired by Dyson Sphere Program's deterministic simulation model. This guide explains how to interface with and use these systems.

**Key Principles:**
- **Deterministic**: Parallel execution produces identical results independent of thread count
- **Burst-Compatible**: All hot paths are Burst-compiled
- **Adaptive**: Systems automatically balance load and optimize batch sizes
- **Domain-Segmented**: Threads are partitioned by domain (Simulation, Physics, Logic, Rendering/IO)

---

## Table of Contents

1. [Thread Role Segmentation](#thread-role-segmentation)
2. [Adaptive Batch Sizing](#adaptive-batch-sizing)
3. [Task Graph Dependencies](#task-graph-dependencies)
4. [Load Balancing](#load-balancing)
5. [Spatial Thread Partitioning](#spatial-thread-partitioning)
6. [Double Buffering](#double-buffering)
7. [Job Profiling](#job-profiling)
8. [Integration Examples](#integration-examples)

---

## Thread Role Segmentation

### Overview

System groups are assigned thread roles that determine their execution characteristics:

- **MainOrchestrator**: Fixed 60Hz scheduling (SimulationSystemGroup)
- **Physics**: Parallel, fixed-rate (PhysicsSystemGroup)
- **Logic**: Async, sub-fixed rate (GameplaySystemGroup, EnvironmentSystemGroup, etc.)
- **RenderingIO**: Variable rate (PresentationSystemGroup)

### Adding Thread Roles to System Groups

```csharp
using PureDOTS.Runtime.Threading;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[ThreadRole(ThreadRoleType.Logic)]  // Add this attribute
public partial class MySystemGroup : ComponentSystemGroup { }
```

### Querying Thread Roles

```csharp
if (ThreadRoleManager.TryGetRole(typeof(MySystemGroup), out var role))
{
    // Use role to determine execution characteristics
    switch (role)
    {
        case ThreadRoleType.Physics:
            // Physics-specific logic
            break;
        case ThreadRoleType.Logic:
            // Logic-specific logic
            break;
    }
}
```

---

## Adaptive Batch Sizing

### Overview

Jobs automatically adjust batch sizes based on entity count, thread count, and estimated cost to keep jobs under 1ms threshold.

### Using Adaptive Batch Sizing in Systems

```csharp
using PureDOTS.Runtime.Threading;

[BurstCompile]
public partial struct MyJobSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<ThreadingConfig>();
        var query = SystemAPI.QueryBuilder()
            .WithAll<MyComponent>()
            .Build();

        var job = new MyJob { /* ... */ };

        // Use adaptive batch sizing instead of fixed batch count
        state.Dependency = AdaptiveBatchSizing.ScheduleAdaptive(
            ref job,
            query,
            ref state,
            config);
    }
}
```

### Manual Batch Size Calculation

```csharp
int entityCount = query.CalculateEntityCount();
int threadCount = config.SimulationThreadCount;
int batchCount = AdaptiveBatchSizing.CalculateAdaptiveBatchCount(
    entityCount,
    threadCount,
    estimatedTimePerEntityMs: 0.001f,  // 1μs per entity estimate
    thresholdMs: 1.0f,
    minBatchSize: 64);

state.Dependency = job.ScheduleParallel(query, batchCount, state.Dependency);
```

---

## Task Graph Dependencies

### Overview

Systems can declare dependencies using attributes. The TaskGraphScheduler builds a DAG and schedules independent jobs in parallel.

### Declaring Dependencies

```csharp
using PureDOTS.Runtime.Threading;

[DependsOn(typeof(MassUpdateSystem))]  // This system depends on MassUpdateSystem
[Produces(typeof(VelocityComponent))]   // This system produces VelocityComponent
[BurstCompile]
public partial struct VelocitySystem : ISystem
{
    // System implementation
}
```

### Building Dependency Graph

The dependency graph is automatically built during bootstrap in `PureDotsWorldBootstrap.InitializeThreadingInfrastructure()`. Systems are registered and dependencies are resolved.

**Note**: Currently, Unity's built-in system ordering (`[UpdateAfter]`, `[UpdateBefore]`) takes precedence. Task graph dependencies are for future advanced scheduling.

---

## Load Balancing

### Overview

The load balancer measures per-thread job duration and redistributes work when imbalance exceeds 20%.

### Recording Load Profiles

```csharp
using PureDOTS.Runtime.Threading;

[BurstCompile]
public partial struct MySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var timeState = SystemAPI.GetSingleton<TimeState>();
        var startTime = System.Diagnostics.Stopwatch.GetTimestamp();

        // ... do work ...

        var endTime = System.Diagnostics.Stopwatch.GetTimestamp();
        float durationMs = (float)(endTime - startTime) / System.Diagnostics.Stopwatch.Frequency * 1000f;

        // Record load profile (typically done by instrumentation system)
        var loadState = SystemAPI.GetSingletonRW<ThreadLoadState>();
        // LoadBalancer.UpdateLoadProfile is called automatically by instrumentation
    }
}
```

### Checking Load Imbalance

```csharp
var profiles = new NativeArray<ThreadLoadProfile>(threadCount, Allocator.Temp);
// ... populate profiles ...

bool needsRebalance = LoadBalancer.MeasureImbalance(
    profiles,
    threshold: 0.2f,  // 20% threshold
    out float imbalanceRatio);

if (needsRebalance)
{
    LoadBalancer.RedistributeRanges(profiles, totalChunks, out var chunkCounts);
    // Apply redistribution
}
```

---

## Spatial Thread Partitioning

### Overview

Spatial operations are partitioned by Morton key ranges, with each thread owning a continuous key range for spatial locality.

### Using Spatial Thread Partitioning

```csharp
using PureDOTS.Runtime.Threading;
using PureDOTS.Runtime.Spatial;

[BurstCompile]
public partial struct SpatialBuildSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<SpatialGridConfig>();
        var threadingConfig = SystemAPI.GetSingleton<ThreadingConfig>();

        if (!threadingConfig.EnableSpatialPartitioning)
        {
            // Fall back to non-partitioned build
            return;
        }

        // Partition Morton keys by thread
        uint minKey = 0;
        uint maxKey = uint.MaxValue;
        int threadCount = threadingConfig.SimulationThreadCount;

        SpatialThreadPartitioning.PartitionMortonKeys(
            minKey, maxKey, threadCount,
            out var threadMinKeys,
            out var threadMaxKeys);

        // Build spatial grid partitioned by thread
        var job = new PartitionedSpatialBuildJob
        {
            ThreadMinKeys = threadMinKeys,
            ThreadMaxKeys = threadMaxKeys,
            // ... other job data
        };

        state.Dependency = job.ScheduleParallel(threadCount, 1, state.Dependency);

        threadMinKeys.Dispose();
        threadMaxKeys.Dispose();
    }
}
```

### Border Exchange Queues

For cross-thread operations at partition boundaries:

```csharp
// Add SpatialBorderEvent buffer to entity
var borderEvents = SystemAPI.GetBuffer<SpatialBorderEvent>(borderEntity);

// When entity crosses boundary
if (SpatialThreadPartitioning.IsBoundaryKey(mortonKey, threadMinKeys, threadMaxKeys))
{
    borderEvents.Add(new SpatialBorderEvent
    {
        SourceThreadId = currentThreadId,
        TargetThreadId = targetThreadId,
        Entity = entity,
        MortonKey = mortonKey,
        EventType = 0  // 0 = enter, 1 = exit, 2 = query
    });
}
```

---

## Double Buffering

### Overview

Double buffering avoids write contention by maintaining separate read/write buffers. Each thread writes exclusively to its own buffer; all reads come from the previous frame.

### Using Double Buffers

```csharp
using PureDOTS.Runtime.Threading;

// In system state or singleton component
private DoubleBuffer<float3> _positions;

public void OnCreate(ref SystemState state)
{
    int maxEntities = 100000;
    _positions = new DoubleBuffer<float3>(maxEntities, Allocator.Persistent);
}

public void OnUpdate(ref SystemState state)
{
    // Read from previous frame's data
    var readBuffer = _positions.ReadBuffer;

    // Write to current frame's buffer
    var writeBuffer = _positions.WriteBuffer;

    var job = new UpdatePositionsJob
    {
        ReadPositions = readBuffer,
        WritePositions = writeBuffer,
        // ...
    };

    state.Dependency = job.ScheduleParallel(query, state.Dependency);
}

// In LateSimulationSystemGroup, swap buffers
public void OnUpdate(ref SystemState state)
{
    // Swap all double buffers at frame end
    _positions.Swap();
}
```

### Enabling Double Buffering

Double buffering is enabled by default in `ThreadingConfig`. To disable:

```csharp
var config = SystemAPI.GetSingletonRW<ThreadingConfig>();
config.ValueRW.EnableDoubleBuffering = false;
```

---

## Job Profiling

### Overview

Job metrics are collected every 10 seconds. Systems exceeding 2ms automatically trigger warnings and rebalancing suggestions.

### Recording Job Metrics

```csharp
using PureDOTS.Runtime.Threading;

var recorder = World.GetExistingSystemManaged<FrameTimingRecorderSystem>();
if (recorder != null)
{
    float jobDurationMs = /* measure job time */;
    float cpuUtilization = /* calculate CPU usage */;
    
    recorder.RecordJobMetrics(
        jobName: "MyJobSystem",
        durationMs: jobDurationMs,
        cpuUtilization: cpuUtilization);
}
```

### Accessing Job Metrics

```csharp
var query = SystemAPI.QueryBuilder()
    .WithAll<JobMetricsSample>()
    .Build();

foreach (var (metrics, entity) in SystemAPI.Query<DynamicBuffer<JobMetricsSample>>()
    .WithEntityAccess())
{
    foreach (var sample in metrics)
    {
        if (sample.AvgMs > 2.0f)
        {
            // Job exceeds threshold - consider optimization
            UnityEngine.Debug.LogWarning(
                $"Job {sample.JobName} exceeds 2ms: {sample.AvgMs:F2}ms");
        }
    }
}
```

---

## Integration Examples

### Example 1: Simple Job with Adaptive Batching

```csharp
[BurstCompile]
public partial struct SimpleMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<ThreadingConfig>();
        var query = SystemAPI.QueryBuilder()
            .WithAll<LocalTransform, Velocity>()
            .Build();

        var job = new MovementJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            TransformHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(false),
            VelocityHandle = SystemAPI.GetComponentTypeHandle<Velocity>(true)
        };

        // Use adaptive batch sizing
        state.Dependency = AdaptiveBatchSizing.ScheduleAdaptive(
            ref job,
            query,
            ref state,
            config);
    }

    [BurstCompile]
    partial struct MovementJob : IJobEntity
    {
        public float DeltaTime;
        public ComponentTypeHandle<LocalTransform> TransformHandle;
        [ReadOnly] public ComponentTypeHandle<Velocity> VelocityHandle;

        void Execute(Entity entity, ref LocalTransform transform, in Velocity velocity)
        {
            transform.Position += velocity.Value * DeltaTime;
        }
    }
}
```

### Example 2: System with Thread Role

```csharp
using PureDOTS.Runtime.Threading;

[UpdateInGroup(typeof(GameplaySystemGroup))]
[ThreadRole(ThreadRoleType.Logic)]  // Declare thread role
[DependsOn(typeof(SpatialSystemGroup))]  // Declare dependency
public partial class MyGameplaySystemGroup : ComponentSystemGroup { }

[BurstCompile]
[UpdateInGroup(typeof(MyGameplaySystemGroup))]
public partial struct MyGameplaySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // System implementation
        // Thread role ensures this runs in Logic domain
    }
}
```

### Example 3: Load-Balanced Spatial System

```csharp
[BurstCompile]
public partial struct LoadBalancedSpatialSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<ThreadingConfig>();
        var loadState = SystemAPI.GetSingleton<ThreadLoadState>();
        var timeState = SystemAPI.GetSingleton<TimeState>();

        // Check if rebalancing is needed (every 60 ticks = 1 second)
        if (timeState.Tick - loadState.LastRebalanceTick >= loadState.RebalanceIntervalTicks)
        {
            // Collect load profiles
            var profiles = CollectLoadProfiles(ref state);
            
            // Check imbalance
            if (LoadBalancer.MeasureImbalance(profiles, config.LoadImbalanceThreshold, out _))
            {
                // Redistribute work
                RedistributeWork(ref state, profiles);
                
                // Update rebalance tick
                var loadStateRW = SystemAPI.GetSingletonRW<ThreadLoadState>();
                loadStateRW.ValueRW.LastRebalanceTick = timeState.Tick;
            }

            profiles.Dispose();
        }
    }
}
```

---

## Configuration

### ThreadingConfig Singleton

All threading behavior is controlled via the `ThreadingConfig` singleton:

```csharp
var config = SystemAPI.GetSingleton<ThreadingConfig>();

// Adjust thread counts
config.SimulationThreadCount = 4;  // Default: 2
config.PhysicsThreadCount = 4;      // Default: 2
config.AsyncIOThreadCount = 2;     // Default: 1

// Adjust thresholds
config.MicroTaskThresholdMs = 0.5f;  // Default: 1.0f
config.LoadImbalanceThreshold = 0.15f;  // Default: 0.2f (20%)

// Toggle features
config.EnableWorkStealing = true;  // Default: true
config.EnableLoadBalancing = true;  // Default: true
config.EnableSpatialPartitioning = true;  // Default: true
config.EnableDoubleBuffering = true;  // Default: true
```

---

## Best Practices

1. **Always use adaptive batch sizing** for jobs processing many entities
2. **Declare thread roles** for new system groups to ensure proper domain assignment
3. **Use double buffering** for hot components that are read/written frequently
4. **Enable spatial partitioning** for systems processing spatial data
5. **Monitor job metrics** to identify systems exceeding 2ms threshold
6. **Respect determinism**: Parallel execution must produce identical results
7. **Use lock-free primitives** (`LockFreePrimitives`) for inter-job communication
8. **Cache-align hot data structures** using `CacheAlignmentHelpers`

---

## Troubleshooting

### Jobs Taking Too Long

- Check job metrics via `JobMetricsSample` buffer
- Reduce batch size or subdivide job further
- Consider using double buffering to reduce contention

### Load Imbalance

- Enable load balancing: `config.EnableLoadBalancing = true`
- Check `ThreadLoadSample` buffer for per-thread metrics
- Manually redistribute work using `LoadBalancer.RedistributeRanges`

### Non-Deterministic Results

- Ensure all parallel jobs use deterministic algorithms
- Verify spatial partitioning uses consistent Morton key ordering
- Check that double buffers are swapped at consistent points

---

## Reference

- **ThreadingConfig**: `PureDOTS.Runtime.Threading.ThreadingConfig`
- **ThreadRoleManager**: `PureDOTS.Runtime.Threading.ThreadRoleManager`
- **AdaptiveBatchSizing**: `PureDOTS.Runtime.Threading.AdaptiveBatchSizing`
- **LoadBalancer**: `PureDOTS.Runtime.Threading.LoadBalancer`
- **SpatialThreadPartitioning**: `PureDOTS.Runtime.Threading.SpatialThreadPartitioning`
- **DoubleBuffer**: `PureDOTS.Runtime.Threading.DoubleBuffer<T>`

---

## See Also

- `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` - System group ordering
- `Docs/FoundationGuidelines.md` - DOTS coding patterns
- `TRI_PROJECT_BRIEFING.md` - Project overview and patterns

