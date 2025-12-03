# Unity Physics Integration Guide

## Overview

This guide documents the Unity Physics integration for PureDOTS, Space4X, and Godgame. The integration follows a key philosophy: **ECS is authoritative** - physics serves as a derived collision detection layer, not a simulation authority.

## Architecture

### Core Principles

1. **ECS Authority**: All gameplay state (positions, velocities, orbits, combat, morale) is driven by PureDOTS systems
2. **Kinematic Bodies**: All physics bodies are kinematic, driven by ECS transforms
3. **Collision Detection Only**: Unity Physics is used for collision detection and queries, not physics simulation
4. **Rewind Compatibility**: Physics state is reconstructed from ECS after rewind, no separate physics history

### Frame-by-Frame Flow

```
1. ECS → Unity Physics Sync (Pre-Physics)
   - Read ECS LocalTransform + velocity components
   - Write to Unity Physics PhysicsCollider, PhysicsVelocity for kinematic bodies

2. Unity Physics Step
   - Unity Physics runs collision detection/queries
   - Generates collision/trigger events

3. Unity Physics → ECS Event Translation (Post-Physics)
   - Consume CollisionEvents, TriggerEvents streams
   - Translate into ECS gameplay events/components
```

## File Structure

### PureDOTS Core (Shared)

```
PureDOTS/Packages/com.moni.puredots/Runtime/
├── Runtime/Physics/
│   ├── PhysicsConfig.cs              # Global physics configuration singleton
│   └── PhysicsInteractionComponents.cs # Existing interaction components
└── Systems/Physics/
    ├── PhysicsBodyBootstrapSystem.cs  # Initializes physics bodies from ECS markers
    ├── PhysicsSyncSystem.cs           # ECS → Unity Physics transform sync
    ├── PhysicsEventSystem.cs          # Unity Physics → ECS collision event translation
    └── PhysicsRewindHelper.cs         # Rewind coordination helpers
```

### Space4X

```
Space4x/Assets/Scripts/Space4x/
├── Runtime/Physics/
│   ├── Space4XPhysicsComponents.cs   # SpacePhysicsBody, SpaceColliderData, etc.
│   └── Space4XPhysicsLayers.cs       # Collision layer definitions
├── Authoring/Physics/
│   └── Space4XVesselPhysicsAuthoring.cs # Authoring for vessels
└── Systems/Physics/
    └── Space4XCollisionResponseSystem.cs # Gameplay collision responses
```

### Godgame

```
Godgame/Assets/Scripts/Godgame/
├── Physics/
│   ├── GodgamePhysicsComponents.cs   # GodgamePhysicsBody, GroundContact, etc.
│   └── GodgamePhysicsLayers.cs       # Collision layer definitions
├── Authoring/Physics/
│   ├── GodgameUnitPhysicsAuthoring.cs     # Authoring for units
│   └── GodgameBuildingPhysicsAuthoring.cs # Authoring for buildings
└── Systems/Physics/
    └── GodgameCollisionResponseSystem.cs # Gameplay collision responses
```

## Configuration

### PhysicsConfig Singleton

The `PhysicsConfig` singleton controls global physics behavior:

```csharp
public struct PhysicsConfig : IComponentData
{
    public byte EnableSpace4XPhysics;     // 0 = disabled, 1 = enabled
    public byte EnableGodgamePhysics;     // 0 = disabled, 1 = enabled
    public byte LogCollisions;            // Debug logging
    public byte PostRewindSettleFrames;   // Frames to skip after rewind
    public ushort MaxPhysicsBodiesPerFrame; // Budget limiting
    public float PhysicsLODDistance;      // LOD distance for physics
}
```

### Collision Layers

#### Space4X Layers
- `Ship`: Player and NPC vessels
- `Asteroid`: Mineable objects
- `Projectile`: Missiles, lasers
- `Debris`: Cosmetic wreckage
- `SensorOnly`: Detection triggers
- `Miner`: Mining vessels
- `Station`: Space stations
- `DockingZone`: Docking triggers

#### Godgame Layers
- `GroundUnit`: Villagers, soldiers
- `FlyingUnit`: Birds, divine messengers
- `Building`: Structures
- `Terrain`: Ground colliders
- `Projectile`: Arrows, spells
- `Decoration`: Trees, rocks
- `Resource`: Gatherable objects
- `TriggerZone`: Area effects

## Usage

### Adding Physics to an Entity

#### Via Authoring (Recommended)

```csharp
// Space4X
[RequireComponent(typeof(Space4XVesselPhysicsAuthoring))]
public class MyVesselAuthoring : MonoBehaviour { }

// Godgame
[RequireComponent(typeof(GodgameUnitPhysicsAuthoring))]
public class MyUnitAuthoring : MonoBehaviour { }
```

#### Via Code

```csharp
// Add RequiresPhysics to enable physics
ecb.AddComponent(entity, new RequiresPhysics
{
    Priority = 100,
    Flags = PhysicsInteractionFlags.Collidable
});

// Add collision event buffer
ecb.AddBuffer<PhysicsCollisionEventElement>(entity);
```

### Reading Collision Events

```csharp
foreach (var (events, entity) in 
    SystemAPI.Query<DynamicBuffer<SpaceCollisionEvent>>()
        .WithEntityAccess())
{
    for (int i = 0; i < events.Length; i++)
    {
        var evt = events[i];
        // Handle collision with evt.OtherEntity
    }
}
```

## Rewind Integration

### How Rewind Works with Physics

1. **Rewind restores ECS state only** - LocalTransform, velocities, gameplay state
2. **Physics bodies are reconstructed** - PhysicsSyncSystem reads rewound transforms
3. **Settle frames** - First N frames after rewind may skip collision events

### Checking Rewind State

```csharp
if (PhysicsRewindHelper.ShouldSkipPhysics(in rewindState))
{
    return; // Skip physics during playback
}

if (PhysicsRewindHelper.IsPostRewindSettleFrame(in config, tick))
{
    return; // Skip events during settling
}
```

## Performance Considerations

### Physics Budget

- Use `PhysicsConfig.MaxPhysicsBodiesPerFrame` to limit physics processing
- Higher priority entities are processed first
- Lower priority entities may be skipped when budget is exceeded

### LOD System

- Use `PhysicsConfig.PhysicsLODDistance` to disable physics on distant entities
- Entities beyond this distance skip physics processing

### Collider Optimization

- Use simple colliders (sphere, capsule) over complex ones
- Buildings use box colliders
- Units use capsule colliders
- Only enable collision events when needed

## Debugging

### Enable Logging

```csharp
var config = SystemAPI.GetSingleton<PhysicsConfig>();
config.LogCollisions = 1;
SystemAPI.SetSingleton(config);
```

### Debug Gizmos

Authoring components draw wire gizmos when selected:
- Green: Space4X colliders
- Blue: Godgame unit colliders
- Orange: Godgame building colliders

## System Groups

```
SimulationSystemGroup
├── PhysicsPreSyncSystemGroup (before physics)
│   ├── PhysicsEventClearSystem
│   ├── PhysicsRewindMarkerSystem
│   └── PhysicsSyncSystem
├── PhysicsSystemGroup (physics step)
│   ├── PhysicsInitializeGroup
│   ├── PhysicsSimulationGroup
│   └── ExportPhysicsWorld
└── PhysicsPostEventSystemGroup (after physics)
    ├── PhysicsEventSystem
    ├── PhysicsResultSyncSystem
    ├── Space4XCollisionResponseSystem
    └── GodgameCollisionResponseSystem
```

## Migration Notes

### From Pure Spatial Grid

If migrating from spatial grid-only collision:
1. Add `RequiresPhysics` to entities that need physics
2. Keep spatial grid for broad-phase queries
3. Use physics for precise collision detection

### Existing Movement Systems

Movement systems remain authoritative:
- `VillagerMovementSystem` still controls villager positions
- Physics provides collision feedback via `AvoidancePush`
- Movement systems read `AvoidancePush` and adjust steering

