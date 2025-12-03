# Rewind and Physics Integration

## Overview

This document explains how the physics integration works with PureDOTS's rewind system. The key principle is: **ECS state is authoritative, physics is derived**.

## Rewind Model

### What Gets Rewound

**Must Include (ECS State)**:
- `LocalTransform` (position, rotation)
- Velocity components (`VillagerMovement.Velocity`, `SpaceVelocity`, etc.)
- Gameplay state affected by collisions:
  - Damage/hull integrity (Space4X)
  - Morale changes (Godgame)
  - Resource extraction triggers (Space4X)
  - Building damage (Godgame)
  - Villager health (Godgame)

**Should NOT Include (Physics Runtime)**:
- `PhysicsBody` runtime data
- `PhysicsVelocity` (reconstructed from ECS velocity)
- Unity Physics internal caches/broadphase
- Collision event buffers (regenerated each frame)

### Rewind Sequence

```
1. Time/History System Rewinds ECS State
   └── RewindCoordinatorSystem restores LocalTransform, velocities, gameplay state

2. Unity Physics Systems Reconstruct Physics Bodies
   └── PhysicsSyncSystem reads rewound transforms
   └── Writes to PhysicsCollider, PhysicsVelocity
   └── Unity Physics rebuilds internal state

3. First Physics Step After Rewind (Settling)
   └── Collisions may be flagged as "post-rewind"
   └── PhysicsConfig.PostRewindSettleFrames controls settle period
   └── Events during settle frames are optionally skipped
```

## Implementation Details

### PhysicsRewindMarkerSystem

Detects when rewind completes and marks the tick:

```csharp
[UpdateInGroup(typeof(PhysicsPreSyncSystemGroup), OrderFirst = true)]
public partial struct PhysicsRewindMarkerSystem : ISystem
{
    private RewindMode _previousMode;

    public void OnUpdate(ref SystemState state)
    {
        var rewindState = SystemAPI.GetSingleton<RewindState>();
        
        // Detect transition from Playback/CatchUp to Record
        if (_previousMode != RewindMode.Record && rewindState.Mode == RewindMode.Record)
        {
            // Rewind just completed - mark the tick
            var config = SystemAPI.GetSingleton<PhysicsConfig>();
            config.LastRewindCompleteTick = timeState.Tick;
            SystemAPI.SetSingleton(config);
        }

        _previousMode = rewindState.Mode;
    }
}
```

### Settle Frame Detection

```csharp
public static bool IsPostRewindSettleFrame(in PhysicsConfig config, uint currentTick)
{
    if (config.PostRewindSettleFrames == 0)
        return false;

    if (config.LastRewindCompleteTick == 0)
        return false;

    return currentTick <= config.LastRewindCompleteTick + config.PostRewindSettleFrames;
}
```

### Skipping Physics During Playback

All physics systems check rewind state:

```csharp
public void OnUpdate(ref SystemState state)
{
    var rewindState = SystemAPI.GetSingleton<RewindState>();
    
    // Skip during rewind playback - ECS state is authoritative
    if (rewindState.Mode == RewindMode.Playback)
    {
        return;
    }
    
    // ... normal physics processing
}
```

## Edge Cases

### Rapid Rewinds

When the player rapidly rewinds multiple times:
1. Each rewind restores ECS state
2. Physics is reconstructed from the new state
3. Settle frames prevent spurious collision events
4. `LastRewindCompleteTick` is updated each time

### Long History

For long rewind histories:
1. Physics bodies may need to be recreated if entities were spawned/destroyed
2. The bootstrap system handles missing physics components
3. Collision event buffers are cleared and regenerated

### Dynamic vs Static Bodies

**Dynamic (Debris, Ragdolls)**:
- Marked with `NonRewindableTag` or similar
- Not included in history snapshots
- Recreated on-demand after rewind
- Purely cosmetic, no gameplay impact

**Kinematic (All Gameplay Entities)**:
- Positions/velocities are in history
- Physics state reconstructed from ECS
- Collision events regenerated

## Testing Rewind + Physics

### Test Scenarios

1. **Basic Rewind**
   - Move entities, trigger collisions
   - Rewind to before collision
   - Verify physics state matches ECS
   - Verify no spurious collision events

2. **Rapid Rewind**
   - Perform multiple rapid rewinds
   - Verify no crashes or exceptions
   - Verify settle frames work correctly

3. **Collision During Settle**
   - Rewind to a state where entities are overlapping
   - Verify settle frame skips collision events
   - Verify normal events resume after settle

4. **Entity Spawn/Destroy**
   - Spawn entity with physics
   - Rewind to before spawn
   - Verify entity and physics are removed
   - Fast-forward past spawn
   - Verify physics is recreated

### Debug Logging

Enable logging to trace rewind behavior:

```csharp
var config = SystemAPI.GetSingleton<PhysicsConfig>();
config.LogCollisions = 1;
SystemAPI.SetSingleton(config);
```

This will log:
- Collision events (with entity IDs and positions)
- Settle frame skips
- Physics body reconstruction

## Best Practices

### DO

- Store all gameplay-affecting state in ECS components
- Use `RequiresPhysics` to mark entities that need physics
- Clear collision event buffers at frame start
- Check `RewindState.Mode` before processing physics

### DON'T

- Store gameplay state in physics components
- Assume physics state persists across rewind
- Process collision events during playback
- Rely on physics for deterministic gameplay (use ECS)

## Configuration

### PhysicsConfig.PostRewindSettleFrames

Controls how many frames to skip collision events after rewind:
- `0`: No settling, process events immediately
- `1` (default): Skip 1 frame
- `2+`: For complex scenes with many overlapping bodies

Increase this value if you see spurious collision events after rewind.

### Debugging Rewind Issues

If physics behaves incorrectly after rewind:

1. Check that all gameplay state is in ECS components
2. Verify `LocalTransform` is being rewound correctly
3. Check `PhysicsSyncSystem` is running after rewind
4. Increase `PostRewindSettleFrames` if needed
5. Enable collision logging to trace events

