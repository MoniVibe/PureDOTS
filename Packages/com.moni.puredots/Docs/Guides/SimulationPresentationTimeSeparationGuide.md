# Simulation vs Presentation Time Separation Guide

**Last Updated**: 2025-01-XX  
**Purpose**: Guide for using the simulation/presentation time domain separation system to eliminate camera jitter and input lag.

---

## Overview

PureDOTS separates simulation (deterministic 60Hz tick) from presentation (variable frame rate) to eliminate jitter and maintain determinism. This guide explains how to interface with and use these systems.

### Architecture

**Simulation Domain** (PureDOTS ECS):
- Driven by: `TickTimeState` (fixed 60Hz)
- Systems: All PureDOTS ECS systems
- Frequency: 60 Hz deterministic

**Presentation Domain** (MonoBehaviour/Presentation ECS):
- Driven by: `UnityEngine.Time.deltaTime`
- Systems: Camera, input sampling, HUD, rendering
- Frequency: Variable (display Hz)

---

## Camera Interpolation

### Adding Interpolation to Camera Targets

To smooth camera movement when following an entity, add `CameraTargetHistory` component to the entity:

```csharp
// In a system or authoring
entityManager.AddComponent(entity, new CameraTargetHistory
{
    PrevPosition = transform.Position,
    NextPosition = transform.Position,
    PrevRotation = transform.Rotation,
    NextRotation = transform.Rotation,
    Alpha = 0f,
    Velocity = float3.zero,
    PrevTick = currentTick,
    NextTick = currentTick
});
```

`CameraInterpolationSystem` automatically updates this component each frame, interpolating between simulation ticks.

### Reading Interpolated Positions

In presentation code (MonoBehaviour or PresentationSystemGroup), read from `CameraTargetHistory`:

```csharp
// MonoBehaviour example
void Update()
{
    var world = World.DefaultGameObjectInjectionWorld;
    if (world == null) return;
    
    var query = world.EntityManager.CreateEntityQuery(
        ComponentType.ReadOnly<CameraTargetHistory>());
    
    if (query.TryGetSingleton(out CameraTargetHistory history))
    {
        // Interpolate between prev and next
        float3 interpolated = math.lerp(
            history.PrevPosition, 
            history.NextPosition, 
            history.Alpha);
        
        // Optional: Apply velocity-based extrapolation
        if (math.lengthsq(history.Velocity) > 1e-6f)
        {
            interpolated += history.Velocity * Time.deltaTime;
        }
        
        // Use interpolated position for camera
        transform.position = new Vector3(
            interpolated.x, 
            interpolated.y, 
            interpolated.z);
    }
}
```

### Example: Following a Player Entity

```csharp
// In your camera controller MonoBehaviour
public class PlayerCameraController : MonoBehaviour
{
    private Entity _playerEntity;
    
    void LateUpdate()
    {
        if (_playerEntity == Entity.Null) return;
        
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        
        var em = world.EntityManager;
        if (!em.Exists(_playerEntity)) return;
        
        if (em.HasComponent<CameraTargetHistory>(_playerEntity))
        {
            var history = em.GetComponentData<CameraTargetHistory>(_playerEntity);
            float3 pos = math.lerp(history.PrevPosition, history.NextPosition, history.Alpha);
            transform.position = new Vector3(pos.x, pos.y, pos.z);
        }
        else
        {
            // Fallback: read LocalTransform directly (may cause jitter)
            var transform = em.GetComponentData<LocalTransform>(_playerEntity);
            transform.position = new Vector3(
                transform.Position.x, 
                transform.Position.y, 
                transform.Position.z);
        }
    }
}
```

---

## Input Delay Configuration

### Setting Input Delay

Input delay buffers commands for deterministic lockstep simulation. Default is 2 ticks.

**Bootstrap InputDelayConfig** (in a bootstrap system):

```csharp
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct InputDelayBootstrapSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var em = state.EntityManager;
        
        // Create singleton if it doesn't exist
        if (!SystemAPI.TryGetSingletonEntity<InputDelayConfig>(out _))
        {
            var entity = em.CreateEntity(typeof(InputDelayConfig));
            em.SetComponentData(entity, InputDelayConfig.Default);
        }
    }
}
```

**Modify Input Delay** (at runtime):

```csharp
// In a system or MonoBehaviour
var em = World.DefaultGameObjectInjectionWorld.EntityManager;
if (SystemAPI.TryGetSingletonEntity<InputDelayConfig>(out var entity))
{
    em.SetComponentData(entity, new InputDelayConfig
    {
        InputDelayTicks = 3 // Change delay to 3 ticks
    });
}
```

### How Input Delay Works

1. `InputSamplingSystem` collects inputs each frame (presentation rate)
2. Commands are quantized and assigned to `targetTick = currentTick + InputDelayTicks`
3. `InputCommandProcessorSystem` processes commands at the delayed tick
4. This ensures deterministic lockstep compatibility

**Example Timeline**:
- Frame 0: Input sampled → assigned to Tick 2
- Frame 1: Input sampled → assigned to Tick 3
- Tick 2: Command processed (from Frame 0)
- Tick 3: Command processed (from Frame 1)

---

## Presentation Snapshots

### Adding Snapshots to Entities

`PresentationSnapshotSystem` automatically adds `PresentationSnapshot` to entities with `LocalTransform`. No manual setup required.

### Reading from Snapshots

In presentation code, read from `PresentationSnapshot` instead of `LocalTransform`:

```csharp
// In PresentationSystemGroup system
[BurstCompile]
public partial struct MyPresentationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (snapshot, entity) in SystemAPI.Query<RefRO<PresentationSnapshot>>()
                     .WithEntityAccess())
        {
            var snap = snapshot.ValueRO;
            
            // Interpolate between buffers A and B
            float3 position = math.lerp(snap.PositionA, snap.PositionB, 0.5f);
            quaternion rotation = math.slerp(snap.RotationA, snap.RotationB, 0.5f);
            
            // Use interpolated values for rendering
            // ...
        }
    }
}
```

### Buffer Swapping

`PresentationSnapshotSystem` swaps buffers each tick:
- **Tick N**: Writes to Buffer A
- **Tick N+1**: Writes to Buffer B
- Presentation interpolates between A and B

This prevents reading half-updated state from ECS chunks.

---

## Input Quantization

`InputQuantizationSystem` quantizes analog inputs to prevent floating-point drift. Currently a placeholder - actual quantization depends on your input command payload structure.

**Future Implementation**:
- Round analog axes: `move = math.round(move * 128f) / 128f`
- Use integer degrees for rotations
- Clamp values in Burst jobs

---

## Kalman Filter (Optional)

For extreme jitter (e.g., streamed simulation), use the Kalman filter:

```csharp
using PureDOTS.Runtime.Camera;

float3 filtered = KalmanFilter.Filter(
    previousState,
    previousVelocity,
    currentMeasurement,
    Time.deltaTime,
    kalmanGain: 0.2f);
```

**Parameters**:
- `kalmanGain`: Typically 0.2 (higher = more responsive, lower = smoother)
- Burst-safe, ~0.02ms cost

---

## Integration Checklist

When adding camera/interpolation support to a new entity:

- [ ] Add `CameraTargetHistory` component to entity (if camera follows it)
- [ ] Ensure entity has `LocalTransform` (for `PresentationSnapshotSystem`)
- [ ] Read from `CameraTargetHistory` in presentation code (not `LocalTransform`)
- [ ] Use `UnityEngine.Time.deltaTime` in presentation code (not tick time)
- [ ] Guard presentation systems: check `RewindState.Mode != RewindMode.Record`

### Example: Complete Integration

```csharp
// 1. Bootstrap: Add CameraTargetHistory to player entity
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct PlayerCameraBootstrapSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (_, entity) in SystemAPI.Query<RefRO<PlayerTag>>()
                     .WithNone<CameraTargetHistory>()
                     .WithEntityAccess())
        {
            var transform = SystemAPI.GetComponent<LocalTransform>(entity);
            state.EntityManager.AddComponentData(entity, new CameraTargetHistory
            {
                PrevPosition = transform.Position,
                NextPosition = transform.Position,
                PrevRotation = transform.Rotation,
                NextRotation = transform.Rotation,
                Alpha = 0f,
                Velocity = float3.zero,
                PrevTick = 0,
                NextTick = 0
            });
        }
    }
}

// 2. Presentation: Read interpolated position
public class PlayerCamera : MonoBehaviour
{
    private Entity _playerEntity;
    
    void LateUpdate()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        
        var em = world.EntityManager;
        if (!em.Exists(_playerEntity)) return;
        
        if (em.HasComponent<CameraTargetHistory>(_playerEntity))
        {
            var history = em.GetComponentData<CameraTargetHistory>(_playerEntity);
            float3 pos = math.lerp(history.PrevPosition, history.NextPosition, history.Alpha);
            transform.position = new Vector3(pos.x, pos.y, pos.z);
        }
    }
}
```

---

## System Ordering

Systems run in this order:

1. **Simulation** (`SimulationSystemGroup`)
   - Entities update `LocalTransform` at 60Hz
   
2. **Presentation Snapshot** (`PresentationSystemGroup`, after simulation)
   - `PresentationSnapshotSystem` copies `LocalTransform` to snapshots
   
3. **Camera Interpolation** (`PresentationSystemGroup`, after snapshot)
   - `CameraInterpolationSystem` updates `CameraTargetHistory`
   
4. **Rendering** (`PresentationSystemGroup`, after interpolation)
   - Camera controllers read interpolated positions

---

## Troubleshooting

### Camera Still Jitters

- Verify `CameraTargetHistory` component exists on followed entity
- Check `CameraInterpolationSystem` is running (not disabled)
- Ensure presentation code reads from `CameraTargetHistory`, not `LocalTransform`
- Verify presentation code uses `Time.deltaTime`, not tick time

### Input Feels Laggy

- Check `InputDelayConfig.InputDelayTicks` (default 2)
- Verify `InputSamplingSystem` runs in `PresentationSystemGroup`
- Ensure commands are assigned to `targetTick = currentTick + delay`

### Rewind Breaks

- All presentation systems must check `RewindState.Mode != RewindMode.Record`
- Camera smoothing never writes back to ECS (presentation-only)
- Input buffering uses deterministic `InputCommandBuffer` queue

---

## Related Documentation

- `TRI_PROJECT_BRIEFING.md` - Architecture overview
- `FoundationGuidelines.md` - Presentation system group policy
- `RuntimeLifecycle_TruthSource.md` - System execution order

