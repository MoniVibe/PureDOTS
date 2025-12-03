# Physics & World History Buffer Plan

Goal: replicate the determinism benefits of `DOTSSample-master`’s `PhysicsWorldHistory.cs` while remaining compliant with Entities/Physics 1.x. This plan describes the data structures, systems, and integration points needed to snapshot and replay physics state alongside our existing history framework.

## Requirements

1. **Consistent tick alignment** – snapshots must align with `TimeState.Tick` and respect pause/rewind modes.
2. **Ring buffer storage** – maintain a rolling window (configurable depth) of `PhysicsWorld` copies to support rewind, debugging, and rollback.
3. **Job safety** – integrate with Unity Physics job handles without blocking main thread execution unnecessarily.
4. **Selective replay** – expose APIs for systems (e.g., transport collision checks) to fetch historical `CollisionWorld`/`DynamicsWorld` data.

## Entities 1.x Considerations

- `BuildPhysicsWorld` still exists but now feeds `PhysicsSystemGroup`. We rely on `PhysicsWorldSingleton` for access to `PhysicsWorld` data.
- Copying a `PhysicsWorld` requires cloning `CollisionWorld`, `DynamicsWorld`, and related NativeCollections. We'll mirror the sample’s per-slot struct but adapt to `Unity.Physics` 1.x allocations (`PhysicsWorld.Clone()` not provided; we’ll manually clone relevant structures).
- Jobs schedule via `StepPhysicsWorld`/`ExportPhysicsWorld`. We must complete `PhysicsWorldSingleton.SimulationSingleton.FinalSimulationJobHandle` before cloning to avoid race conditions.

## Proposed Components & Systems

### Data Components

- `PhysicsHistoryBuffer` (managed singleton)
  - Holds a `NativeList<PhysicsWorldSnapshot>` sized by `HistorySettings.PhysicsHistoryLength` (default 32).
  - Exposes methods `GetCollisionWorldAtTick(int tick)`, `GetPhysicsWorldAtTick(int tick)`.
- `PhysicsWorldSnapshot`
  - Stores tick index and cloned data: `CollisionWorld`, `DynamicsWorld`, `VelocityHistory` (optional), broadphase arrays.
  - Provides `Dispose()` to release native memory.

### Systems

- `PhysicsHistoryBootstrapSystem`
  - `[UpdateInGroup(typeof(TimeSystemGroup))]`
  - Ensures buffer singleton exists, respects `HistorySettings` overrides.

- `PhysicsHistoryCaptureSystem`
  - `[UpdateInGroup(typeof(PureDOTS.Systems.LateSimulationSystemGroup))]`
  - `[UpdateAfter(typeof(Unity.Physics.Systems.StepPhysicsWorld))]`
  - `[UpdateBefore(typeof(PureDOTS.Systems.HistorySystemGroup))]`
  - Completes `PhysicsSystemGroup` job handles, clones the physics world into the next ring buffer slot, and records metadata (tick, deltaTime, world version).
  - Skips capture when `RewindState.Mode != Record` or when simulation is paused.

- `PhysicsHistoryQuerySystem` (optional helper)
  - Provides ECS-friendly API to query historical collision/contact data for diagnostics or rollback.

## Cloning Strategy

1. Allocate persistent `NativeArray`s for bodies, motions, joints per buffer slot (mirrors sample’s `CollisionHistoryBuffer`).
2. Use `PhysicsWorld.Bodies.CopyTo()` and similar APIs to duplicate data.
3. For `CollisionWorld`, replicate `CollisionWorld.NumBodies`, `StaticTree`, `DynamicTree`, `Bodies`, `MotionVelocities`, `Joints`.
4. Use `UnsafeUtility.MemCpy` where necessary; guard with `#if ENABLE_UNITY_COLLECTIONS_CHECKS` for validation builds.
5. On buffer overwrite, dispose existing arrays before writing new data.

## Integration with Existing History

- Hook into `CoreSingletonBootstrapSystem` to seed default `PhysicsHistorySettings` (new component) – stores buffer length, capture cadence (ticks), and whether to store dynamics.
- `HistorySystemGroup` consumers can request paired game-state and physics-state histories (e.g., transport collision replays) via a new `PhysicsHistoryHandle` struct.
- Add config vars (`history.physics.enabled`, `history.physics.length`) so runtime customization is possible once the config service ships.

## API Sketch

```csharp
public struct PhysicsHistoryHandle
{
    public bool TryGetCollisionWorld(int tick, out CollisionWorld world);
    public bool TryGetPhysicsWorld(int tick, out PhysicsWorld world);
}

public static class PhysicsHistory
{
    public static PhysicsHistoryHandle GetHandle(SystemState state);
}
```

Systems can cache `PhysicsHistoryHandle` and query inside `OnUpdate()` without knowing about the underlying buffer implementation.

## Implementation Steps

1. **Bootstrap** – create `PhysicsHistorySettings` default and buffer singleton; write unit tests validating creation.
2. **Capture System** – implement `PhysicsHistoryCaptureSystem` with ring buffer clone logic; verify via playmode tests (tick progression, pause/rewind). Ensure memory cleanup on world tear-down.
3. **Query API** – expose `PhysicsHistoryHandle`, integrate with `Transport` and `Vessel` diagnostics to log historical collision info.
4. **Rewind Integration** – when `RewindState` enters playback, feed cached `CollisionWorld` to simulation or diagnostic systems as needed.
5. **Instrumentation** – emit telemetry (e.g., `PhysicsHistoryDiagnostics`) counting buffer overwrites, clone duration, memory usage for debugging.

## Risks

- **Memory Footprint** – cloning full `PhysicsWorld` each tick is expensive. Mitigate by allowing config to disable dynamics cloning or reduce buffer depth.
- **Job Completion Cost** – ensuring physics jobs complete before cloning can extend frame time. We will profile and, if necessary, move capture to a separate job scheduled after `StepPhysicsWorld` but before `HistorySystemGroup` runs.
- **API Changes** – future Entities/Physics updates may adjust world access patterns. Encapsulating cloning/query logic in dedicated systems reduces surface area for future migrations.

Delivering this buffer brings our rewind tooling in line with the DOTS sample while aligning with PureDOTS’ deterministic goals.











