# Asynchronous Perception Streaming Guide

**Purpose**: Guide for treating expensive sensors (radar, smell diffusion) as async jobs for stable frame pacing.

## Overview

Long-range or expensive sensors work like I/O, not logic. Results are queued via `NativeQueue<SensorPacket>` and processed every few frames for stable frame pacing.

## Core Components

### SensorPacket

```csharp
public struct SensorPacket : IBufferElementData
{
    public float3 SourcePosition;
    public float Confidence;
    public SensorType SensorType;
    public uint DetectionTick;
    public Entity SourceEntity;
}
```

Queued sensor result packet.

### AsyncSensorSystem

```csharp
[UpdateInGroup(typeof(AISystemGroup))]
public partial struct AsyncSensorSystem : ISystem
{
    private NativeQueue<SensorPacket> _sensorQueue;
    
    // Processes queued packets every N frames
    // Integrates with PerceptionInterpreterSystem
}
```

System that processes queued sensor packets asynchronously.

## Usage Pattern

### Enqueueing Sensor Results

```csharp
// In sensor system (e.g., RadarSensorSystem)
var sensorPacket = new SensorPacket
{
    SourcePosition = detectedPosition,
    Confidence = confidence,
    SensorType = SensorType.Radar,
    DetectionTick = currentTick,
    SourceEntity = sourceEntity
};

asyncSensorSystem.EnqueueSensorPacket(sensorPacket);
```

### Processing Queued Packets

`AsyncSensorSystem` processes queue every N frames:

```csharp
public void OnUpdate(ref SystemState state)
{
    // Process up to MaxPacketsPerFrame packets
    int processed = 0;
    while (_sensorQueue.Count > 0 && processed < MaxPacketsPerFrame)
    {
        var packet = _sensorQueue.Dequeue();
        ProcessSensorPacket(packet);
        processed++;
    }
}
```

### Integration with Perception

Processed packets update `PerceptionFeatureVector` buffers:

```csharp
private void ProcessSensorPacket(SensorPacket packet)
{
    // Update PerceptionFeatureVector buffer
    // Integrate with PerceptionFusionSystem
    // Bridge to MindECS via AgentSyncBus
}
```

## Best Practices

1. **Queue expensive sensors**: Radar, smell diffusion, long-range sensors
2. **Process in batches**: Limit packets per frame for stable pacing
3. **Integrate with perception**: Update `PerceptionFeatureVector` buffers
4. **Respect budgets**: Use `NavPerformanceBudget` for sensor processing limits

## Performance Impact

- **Stable frame pacing**: Expensive sensors don't cause frame spikes
- **Async processing**: Sensors work like I/O, not blocking logic
- **Scalable**: Handles high sensory density without performance degradation

## References

- `PureDOTS/Packages/com.moni.puredots/Runtime/AI/SensorPacket.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/AI/AsyncSensorSystem.cs`

