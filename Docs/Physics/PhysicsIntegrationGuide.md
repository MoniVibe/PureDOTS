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
│   ├── MassComponents.cs            # MassComponent, MassDirtyTag
│   └── PhysicsInteractionComponents.cs # Existing interaction components
└── Systems/Physics/
    ├── PhysicsStepConfigSystem.cs    # Configures deterministic PhysicsStep singleton
    ├── CustomMassSyncSystem.cs       # Syncs MassComponent → PhysicsMass
    ├── PhysicsOptimizationSystem.cs  # Solver tuning, static aggregation prep
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

### Deterministic Physics Configuration

Unity Physics is configured for deterministic simulation matching PureDOTS fixed-step architecture:

**Determinism Requirements:**
- Fixed Δt only (no variable step) - uses `TimeState.FixedDeltaTime` via `FixedStepSimulationSystemGroup.Timestep`
- `SimulationType.UnityPhysics` (explicit, not Auto)
- Burst/math versions locked (Entities 1.4.2 / Burst 1.8.24)
- Platform parity (same CPU architecture for lockstep)

**Configuration Systems:**
- `PhysicsStepConfigSystem` - Configures `PhysicsStep` singleton with deterministic settings
  - Runs in `TimeSystemGroup` after `TimeTickSystem`
  - Sets `SimulationType.UnityPhysics`, solver iterations (default 2), disables transform sync
- `CustomMassSyncSystem` - Syncs `MassComponent` → `PhysicsMass` each tick
  - Runs in `PhysicsSystemGroup` after `PhysicsInitializeGroup`, before simulation
  - Handles `MassDirtyTag` for efficient dirty tracking
- `PhysicsOptimizationSystem` - Applies performance optimizations
  - Configures solver iterations, ensures transform sync disabled
  - Prepares for static aggregation and broad-phase culling

**Timestep Synchronization:**
The physics timestep comes from `FixedStepSimulationSystemGroup.Timestep`, which is synchronized with `TimeState.FixedDeltaTime` by `GameplayFixedStepSyncSystem`. Unity Physics `StepPhysicsWorld` system reads `SystemAPI.Time.DeltaTime`, which uses this fixed timestep.

**System Dependencies:**
- `PhysicsStepConfigSystem` must run before any physics systems (runs in `TimeSystemGroup`)
- `CustomMassSyncSystem` requires `PhysicsStep` singleton (created by `PhysicsStepConfigSystem`)
- `CustomMassSyncSystem` requires `PhysicsMass` component (created by `PhysicsBodyBootstrapSystem` or manually)
- Systems reading synced `PhysicsMass` must run after `CustomMassSyncSystem`

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

### Adding Physics with Mass Sync

For entities that need dynamic mass properties synced to Unity Physics:

```csharp
// Step 1: Add RequiresPhysics (triggers PhysicsBodyBootstrapSystem)
ecb.AddComponent(entity, new RequiresPhysics
{
    Priority = 100,
    Flags = PhysicsInteractionFlags.Collidable
});

// Step 2: Add MassComponent with initial mass properties
ecb.AddComponent(entity, new MassComponent
{
    Mass = 1000f, // kg
    CenterOfMass = float3.zero, // Local space
    InertiaTensor = new float3(100f, 100f, 100f) // Diagonalized (Ixx, Iyy, Izz)
});

// Step 3: PhysicsBodyBootstrapSystem will add PhysicsCollider, PhysicsVelocity, PhysicsMass
// Step 4: CustomMassSyncSystem will sync MassComponent → PhysicsMass each tick
```

**Important:** `CustomMassSyncSystem` requires both `MassComponent` and `PhysicsMass` to be present. `PhysicsBodyBootstrapSystem` creates `PhysicsMass` for entities with `RequiresPhysics`, but you must add `MassComponent` separately if you want mass sync.

### Updating Mass Properties

When mass changes (e.g., cargo loaded/unloaded, fuel consumed):

```csharp
// Option 1: Direct update (will sync next tick)
var mass = SystemAPI.GetComponentRW<MassComponent>(entity);
mass.ValueRW.Mass = newMass;
mass.ValueRW.InertiaTensor = newInertiaTensor;

// Option 2: Mark as dirty (optional, for tracking)
ecb.AddComponent<MassDirtyTag>(entity);
// CustomMassSyncSystem removes MassDirtyTag after sync
```

**Note:** `MassDirtyTag` is optional - `CustomMassSyncSystem` syncs all entities with `MassComponent` each tick regardless. Use `MassDirtyTag` only if you need to track which entities had mass changes.

### Reading Synced Physics Mass

After `CustomMassSyncSystem` runs, you can read the synced `PhysicsMass`:

```csharp
// In a system that runs after CustomMassSyncSystem
foreach (var (mass, physMass) in 
    SystemAPI.Query<RefRO<MassComponent>, RefRO<PhysicsMass>>())
{
    // Read synced inverse mass
    float inverseMass = physMass.ValueRO.InverseMass;
    float actualMass = 1f / inverseMass; // Reconstruct if needed
    
    // Read synced inverse inertia (diagonal)
    float3 inverseInertia = physMass.ValueRO.InverseInertia;
    
    // Read center of mass offset
    float3 comOffset = physMass.ValueRO.Transform.pos;
}
```

### Integration with PhysicsBodyBootstrapSystem

`PhysicsBodyBootstrapSystem` creates kinematic physics bodies. For dynamic bodies with mass sync:

```csharp
// After PhysicsBodyBootstrapSystem creates kinematic body:
// 1. Remove kinematic PhysicsMass
ecb.RemoveComponent<PhysicsMass>(entity);

// 2. Add MassComponent (will be synced by CustomMassSyncSystem)
ecb.AddComponent(entity, new MassComponent
{
    Mass = 1000f,
    CenterOfMass = float3.zero,
    InertiaTensor = new float3(100f, 100f, 100f)
});

// 3. CustomMassSyncSystem will create PhysicsMass from MassComponent
// Note: CustomMassSyncSystem only updates existing PhysicsMass,
// so you may need to add it manually or modify the system
```

**Current Limitation:** `CustomMassSyncSystem` only updates existing `PhysicsMass` components. If you need to create `PhysicsMass` from `MassComponent`, you'll need to add it manually or extend the system.

### Overriding Solver Iterations Per Material

Default solver iterations are 2 (configured in `PhysicsStepConfigSystem`). To override per material:

```csharp
// Option 1: Store iteration count in PhysicsMaterial.CustomTags
var material = new PhysicsMaterial
{
    Friction = 0.5f,
    Restitution = 0.1f,
    CustomTags = (uint)myIterationCount // Store in tags
};

// Option 2: Create custom system to adjust PhysicsStep per entity
// (Requires modifying PhysicsOptimizationSystem or creating new system)
// This is advanced and not currently implemented
```

**Note:** Per-material solver iteration override is not currently implemented. The default of 2 iterations applies globally. For precision-critical collisions, consider using higher default iterations or implementing custom collision response.

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
- Use compound colliders with primitive children (sphere, capsule, box)
- Dynamic mesh colliders are prohibitive beyond ~10k active bodies

### Mass Component Sync

Entities with `MassComponent` are automatically synced to `PhysicsMass` each tick via `CustomMassSyncSystem`:

```csharp
// Add MassComponent to entity
ecb.AddComponent(entity, new MassComponent
{
    Mass = 1000f, // kg
    CenterOfMass = float3.zero,
    InertiaTensor = new float3(100f, 100f, 100f) // Diagonalized
});

// Add MassDirtyTag when mass changes (triggers recalculation)
ecb.AddComponent<MassDirtyTag>(entity);
```

The system calculates:
- `InverseMass = 1f / math.max(0.0001f, mass.Mass)`
- `InverseInertia = 1f / math.max(inertia, minInertia)` (diagonal)

### Static Geometry Aggregation

For large-scale simulations, aggregate static geometry into chunked colliders:
- Combine environment colliders into 64×64m grid chunks
- One `PhysicsCollider` per grid cell = millions fewer broad-phase nodes
- Reduces broad-phase pair count from O(n²) to O(n log n)

### Broad-Phase Culling

Manually partition colliders by simulation region/planet cell:
- Only include active regions in `CollisionWorld`
- Use spatial partitioning to exclude inactive regions
- Reduces collision detection overhead for large worlds

### Solver Iteration Tuning

Default solver iterations: 2 (configured in `PhysicsStepConfigSystem`)

Override per material for precision where needed:
```csharp
// In PhysicsMaterial.CustomTags, store iteration count
// Custom systems can read tags and adjust solver iterations
```

### Extreme Mass Ratios

Unity Physics solver can lose precision with 10⁸:1 mass ratios (planets ↔ probes).

PureDOTS mitigates this using tiered tick domains:
- **Micro-physics** (probes, debris) → Unity Physics discrete solver
- **Macro interactions** (planets) → Analytic orbit/energy model

This separation prevents numeric stability edge cases.

## Quick Reference

### Common Integration Patterns

**Pattern 1: Entity needs physics with dynamic mass**
```csharp
// 1. Add RequiresPhysics (triggers bootstrap)
ecb.AddComponent(entity, new RequiresPhysics { Flags = PhysicsInteractionFlags.Collidable });

// 2. Add MassComponent
ecb.AddComponent(entity, new MassComponent { Mass = 1000f, /* ... */ });

// 3. PhysicsBodyBootstrapSystem creates PhysicsMass (kinematic)
// 4. CustomMassSyncSystem syncs MassComponent → PhysicsMass each tick
```

**Pattern 2: Update mass when cargo changes**
```csharp
// In cargo system:
var mass = SystemAPI.GetComponentRW<MassComponent>(entity);
mass.ValueRW.Mass = CalculateTotalMass(cargo);
ecb.AddComponent<MassDirtyTag>(entity); // Optional tracking
```

**Pattern 3: Read synced mass in collision response**
```csharp
// System runs after CustomMassSyncSystem
var massLookup = SystemAPI.GetComponentLookup<PhysicsMass>(true);
var inverseMass = massLookup[entity].InverseMass;
```

**Pattern 4: Check if mass sync is active**
```csharp
// Query for entities with both components
var hasMassSync = SystemAPI.HasComponent<MassComponent>(entity) && 
                  SystemAPI.HasComponent<PhysicsMass>(entity);
```

### System Query Patterns

**Query entities with mass sync:**
```csharp
// Entities with both MassComponent and PhysicsMass
foreach (var (mass, physMass) in 
    SystemAPI.Query<RefRO<MassComponent>, RefRW<PhysicsMass>>())
{
    // Mass is synced by CustomMassSyncSystem
}
```

**Query entities needing mass update:**
```csharp
// Entities with MassDirtyTag (optional tracking)
foreach (var (_, entity) in 
    SystemAPI.Query<RefRO<MassDirtyTag>>().WithEntityAccess())
{
    // Mass was recently changed
}
```

**Query physics bodies:**
```csharp
// All entities with physics
foreach (var (collider, transform) in 
    SystemAPI.Query<RefRO<PhysicsCollider>, RefRO<LocalTransform>>())
{
    // Has physics collider
}
```

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

### Verify Mass Sync

```csharp
// Check if PhysicsMass matches MassComponent
var mass = SystemAPI.GetComponent<MassComponent>(entity);
var physMass = SystemAPI.GetComponent<PhysicsMass>(entity);
float expectedInverseMass = 1f / math.max(0.0001f, mass.Mass);
bool isSynced = math.abs(physMass.InverseMass - expectedInverseMass) < 0.001f;
```

### Verify Deterministic Configuration

```csharp
// Check PhysicsStep singleton
var physicsStep = SystemAPI.GetSingleton<PhysicsStep>();
bool isDeterministic = physicsStep.SimulationType == SimulationType.UnityPhysics &&
                       physicsStep.SolverIterationCount == 2 &&
                       physicsStep.SynchronizeCollisionWorld == 0;
```

## System Groups

```
TimeSystemGroup
├── TimeTickSystem
└── PhysicsStepConfigSystem (configures PhysicsStep singleton)

SimulationSystemGroup
├── PhysicsPreSyncSystemGroup (before physics)
│   ├── PhysicsEventClearSystem
│   ├── PhysicsRewindMarkerSystem
│   └── PhysicsSyncSystem
├── PhysicsSystemGroup (physics step)
│   ├── PhysicsInitializeGroup
│   ├── CustomMassSyncSystem (syncs MassComponent → PhysicsMass)
│   ├── PhysicsOptimizationSystem (solver tuning, optimizations)
│   ├── PhysicsSimulationGroup
│   └── ExportPhysicsWorld
└── PhysicsPostEventSystemGroup (after physics)
    ├── PhysicsEventSystem
    ├── PhysicsResultSyncSystem
    ├── Space4XCollisionResponseSystem
    └── GodgameCollisionResponseSystem
```

## Integration Examples

### Example: Vessel with Cargo Mass

```csharp
[BurstCompile]
[UpdateInGroup(typeof(GameplaySystemGroup))]
public partial struct VesselCargoMassSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        foreach (var (cargo, mass, entity) in 
            SystemAPI.Query<RefRO<CargoComponent>, RefRW<MassComponent>>()
                .WithEntityAccess())
        {
            // Calculate total mass from cargo
            float cargoMass = CalculateCargoMass(cargo.ValueRO);
            float baseMass = 5000f; // Base vessel mass
            float totalMass = baseMass + cargoMass;
            
            // Update mass component
            mass.ValueRW.Mass = totalMass;
            
            // Recalculate inertia based on cargo distribution
            mass.ValueRW.InertiaTensor = CalculateInertia(totalMass, cargo.ValueRO);
            
            // Mark as dirty (optional)
            ecb.AddComponent<MassDirtyTag>(entity);
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    
    private static float CalculateCargoMass(in CargoComponent cargo) { /* ... */ }
    private static float3 CalculateInertia(float mass, in CargoComponent cargo) { /* ... */ }
}
```

### Example: Reading Physics Mass for Collision Response

```csharp
[BurstCompile]
[UpdateInGroup(typeof(PhysicsPostEventSystemGroup))]
public partial struct CollisionResponseSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var massLookup = SystemAPI.GetComponentLookup<PhysicsMass>(true);
        
        foreach (var (events, entity) in 
            SystemAPI.Query<DynamicBuffer<CollisionEvent>>()
                .WithEntityAccess())
        {
            for (int i = 0; i < events.Length; i++)
            {
                var evt = events[i];
                var otherEntity = evt.GetOtherEntity(entity);
                
                // Read synced physics mass
                if (massLookup.HasComponent(entity) && massLookup.HasComponent(otherEntity))
                {
                    var myMass = massLookup[entity];
                    var otherMass = massLookup[otherEntity];
                    
                    // Use inverse mass for collision response
                    float myInverseMass = myMass.InverseMass;
                    float otherInverseMass = otherMass.InverseMass;
                    
                    // Calculate collision response...
                }
            }
        }
    }
}
```

### Example: System Ordering for Mass-Dependent Physics

When creating systems that depend on synced mass:

```csharp
// ✅ CORRECT: Run after CustomMassSyncSystem
[BurstCompile]
[UpdateInGroup(typeof(Unity.Physics.Systems.PhysicsSystemGroup))]
[UpdateAfter(typeof(CustomMassSyncSystem))]
[UpdateBefore(typeof(Unity.Physics.Systems.PhysicsSimulationGroup))]
public partial struct MyMassDependentSystem : ISystem
{
    // Can safely read synced PhysicsMass here
}

// ❌ WRONG: Runs before mass sync
[UpdateBefore(typeof(CustomMassSyncSystem))]
public partial struct MyMassDependentSystem : ISystem
{
    // PhysicsMass not yet synced - will read stale data
}
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

### Migrating to Mass Sync

If you have existing entities with `PhysicsMass` that need mass sync:

1. **Add `MassComponent`** with current mass values
2. **Ensure `PhysicsMass` exists** (created by `PhysicsBodyBootstrapSystem` or manually)
3. **`CustomMassSyncSystem` will sync automatically** each tick
4. **Update `MassComponent`** when mass changes (cargo, fuel, etc.)
5. **Remove manual `PhysicsMass` updates** - let `CustomMassSyncSystem` handle it

