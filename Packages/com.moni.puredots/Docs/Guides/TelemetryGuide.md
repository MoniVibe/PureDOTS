# Hierarchical Telemetry Pipeline Guide

**Purpose**: Guide for three-level telemetry (local ring buffer → aggregator → JSON export) for constant-time metrics.

## Overview

Three-level telemetry structure: local per-system ring buffers (Burst-safe), per-ECS aggregator thread, and global JSON exporter for CI performance dashboards. Provides constant-time metrics with no profiler overhead.

## Architecture Levels

### Level 1: Local Telemetry Buffer

Per-system ring buffer (Burst-safe):

```csharp
private LocalTelemetryBuffer _localBuffer;

public void OnCreate(ref SystemState state)
{
    _localBuffer = new LocalTelemetryBuffer(capacity: 100, Allocator.Persistent);
}

public void OnUpdate(ref SystemState state)
{
    // Add metrics to local buffer
    _localBuffer.Add(new TelemetryMetric
    {
        Key = "SystemUpdateTime",
        Value = updateTimeMs,
        Unit = TelemetryMetricUnit.DurationMilliseconds
    });
}
```

### Level 2: Telemetry Aggregator

Async aggregator thread that collects from local buffers:

```csharp
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class TelemetryAggregatorSystem : SystemBase
{
    // Collects metrics from LocalTelemetryBuffer across all systems
    // Aggregates metrics (sum, average, min, max)
    // Updates global TelemetryStream singleton
}
```

### Level 3: Telemetry Exporter

JSON exporter for CI dashboards:

```csharp
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct TelemetryExportSystem : ISystem
{
    // Reads aggregated metrics from TelemetryStream
    // Serializes to JSON format
    // Writes to file or sends to CI service
}
```

## Usage Pattern

### Adding Metrics to Local Buffer

```csharp
[BurstCompile]
public partial struct MySystem : ISystem
{
    private LocalTelemetryBuffer _localBuffer;
    
    public void OnUpdate(ref SystemState state)
    {
        var startTime = System.Diagnostics.Stopwatch.GetTimestamp();
        
        // ... system logic ...
        
        var endTime = System.Diagnostics.Stopwatch.GetTimestamp();
        float updateTimeMs = (endTime - startTime) / (float)System.Diagnostics.Stopwatch.Frequency * 1000f;
        
        // Add to local buffer
        _localBuffer.Add(new TelemetryMetric
        {
            Key = "MySystem.UpdateTime",
            Value = updateTimeMs,
            Unit = TelemetryMetricUnit.DurationMilliseconds
        });
    }
}
```

### Reading Aggregated Metrics

```csharp
// Read from TelemetryStream singleton
var telemetryStream = SystemAPI.GetSingleton<TelemetryStream>();
var metrics = SystemAPI.GetBuffer<TelemetryMetric>(telemetryEntity);

foreach (var metric in metrics)
{
    Debug.Log($"{metric.Key}: {metric.Value} {metric.Unit}");
}
```

## Telemetry Metrics

### Standard Metrics

- **Event counts**: Events processed per system
- **Cache hit rates**: Temporal cache performance
- **System impact metrics**: CPU cost, changed entities
- **Tick domain execution times**: Domain-specific timing
- **Archetype fragmentation stats**: Memory efficiency

### Custom Metrics

```csharp
_localBuffer.Add(new TelemetryMetric
{
    Key = "CustomMetric",
    Value = customValue,
    Unit = TelemetryMetricUnit.Custom
});
```

## Best Practices

1. **Use local buffers**: Add metrics to local buffers in Burst systems
2. **Aggregate periodically**: Let aggregator collect and process
3. **Export to CI**: Use exporter for CI dashboards
4. **Constant-time**: Metrics don't add profiler overhead
5. **Burst-safe**: Local buffers are Burst-compatible

## Performance Impact

- **Constant-time metrics**: No profiler overhead
- **Burst-safe**: Local buffers work in Burst-compiled systems
- **CI integration**: JSON export for performance dashboards
- **Scalable**: Handles millions of entities without performance impact

## References

- `PureDOTS/Packages/com.moni.puredots/Runtime/Telemetry/LocalTelemetryBuffer.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Telemetry/TelemetryAggregatorSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Telemetry/TelemetryExporterSystem.cs`

