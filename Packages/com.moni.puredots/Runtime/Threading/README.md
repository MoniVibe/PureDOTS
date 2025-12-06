# PureDOTS Threading Infrastructure

This directory contains the threading and parallelization infrastructure for PureDOTS.

## Components

### Core Infrastructure

- **ThreadRoleManager.cs** - Maps system groups to thread roles (MainOrchestrator, Physics, Logic, RenderingIO)
- **ThreadingConfig.cs** - Configuration singleton for all threading settings
- **ThreadAffinityManager.cs** - Domain-based thread partitioning

### Scheduling & Execution

- **TaskGraphScheduler.cs** - Dependency DAG builder with [DependsOn]/[Produces] attributes
- **WorkStealingQueue.cs** - Lock-free deque for work distribution
- **AdaptiveBatchSizing.cs** - Dynamic batch sizing based on job cost (1ms threshold)
- **FramePipelineManager.cs** - Triple-buffered scheduling (Render/Simulate/Load)

### Spatial & Memory

- **SpatialThreadPartition.cs** - Morton key-based thread partitioning with border exchange queues
- **CacheAlignmentHelpers.cs** - 64-byte cache line alignment utilities
- **DoubleBufferManager.cs** - Read/write buffer pattern for write contention avoidance

### Monitoring & Optimization

- **LoadBalancer.cs** - Adaptive load balancing with 20% imbalance threshold
- **LockFreePrimitives.cs** - Atomic flags, counters, queues, and streams
- **ThreadSafetyGuards.cs** - Debug-only assertions for deterministic order verification

### IO & Background

- **AsyncIOThread.cs** - Low-priority thread for streaming/serialization with ring buffers

## Usage

See `Docs/Guides/ThreadingParallelizationGuide.md` for comprehensive usage documentation.

## Quick Start

1. **Configure threading** (done automatically in bootstrap):
   ```csharp
   var config = SystemAPI.GetSingleton<ThreadingConfig>();
   ```

2. **Use adaptive batch sizing** in your jobs:
   ```csharp
   state.Dependency = AdaptiveBatchSizing.ScheduleAdaptive(
       ref job, query, ref state, config);
   ```

3. **Add thread role** to system groups:
   ```csharp
   [ThreadRole(ThreadRoleType.Logic)]
   public partial class MySystemGroup : ComponentSystemGroup { }
   ```

## Integration Points

- **Bootstrap**: `PureDotsWorldBootstrap.InitializeThreadingInfrastructure()`
- **System Groups**: `SystemGroups.cs` - Add [ThreadRole] attributes
- **Telemetry**: `FrameTimingRecorderSystem` - Extended with job metrics
- **Spatial**: `SpatialProviders.cs` - Uses Morton key partitioning

## Testing

See `Runtime/Tests/Threading/DeterminismTests.cs` for test examples.

