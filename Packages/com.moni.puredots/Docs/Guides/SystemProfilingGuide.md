# System Profiling Guide

## Overview

System profiling tracks performance metrics per system and enables automatic pruning of underperforming systems.

## SystemMetrics Component

### Adding Metrics to Systems

```csharp
using PureDOTS.Runtime.Components;

// In your system OnUpdate
var metricsEntity = SystemAPI.GetSingletonEntity<SystemMetrics>();
var metrics = SystemAPI.GetComponentRW<SystemMetrics>(metricsEntity);

var stopwatch = Stopwatch.StartNew();
// ... system logic ...
stopwatch.Stop();

metrics.ValueRW.TickCostMs = (float)stopwatch.Elapsed.TotalMilliseconds;
metrics.ValueRW.EntityCount = query.CalculateEntityCount();
metrics.ValueRW.JobCount = 1; // Number of jobs scheduled
metrics.ValueRW.LastUpdateTick = currentTick;
```

### Automatic Pruning

`SystemMetricsCollector` automatically disables systems with <1% contribution:

```csharp
// Systems with TickCostMs < 1% of total are automatically disabled
// Check if your system is enabled:
if (Enabled)
{
    // System is active
}
```

## Telemetry Export

Metrics are exported to telemetry every 5 seconds:

```csharp
// Read metrics from telemetry
var telemetryEntity = SystemAPI.GetSingletonEntity<TelemetryStream>();
var telemetryBuffer = SystemAPI.GetBuffer<TelemetryMetric>(telemetryEntity);

foreach (var metric in telemetryBuffer)
{
    if (metric.MetricName.Equals("SystemMetrics_TotalCostMs"))
    {
        float totalCost = metric.Value;
    }
}
```

## Memory Metrics

### Tracking Chunk Reuse

```csharp
using PureDOTS.Runtime.Components;

var metricsEntity = SystemAPI.GetSingletonEntity<MemoryMetrics>();
var metrics = SystemAPI.GetComponent<MemoryMetrics>(metricsEntity);

float reuseRate = metrics.ChunkReuseRate; // Target: >0.9
float fragmentation = metrics.FragmentationScore; // Lower is better
```

### Chunk Reuse System

`ChunkReuseSystem` automatically flushes inactive chunks every 10 minutes and tracks reuse metrics. No manual intervention needed.

## Integration Checklist

When profiling systems:

- [ ] Add `SystemMetrics` tracking to expensive systems
- [ ] Monitor telemetry for performance regressions
- [ ] Check `MemoryMetrics` for chunk reuse rate (>90% target)
- [ ] Use automatic pruning to disable low-contribution systems

