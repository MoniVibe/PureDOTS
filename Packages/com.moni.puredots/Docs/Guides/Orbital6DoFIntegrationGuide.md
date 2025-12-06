# Orbital 6-DoF Integration Guide

This guide explains how to use the 6-DoF orbital dynamics system in PureDOTS for Space4X and other space-based games.

## Overview

The orbital 6-DoF system provides optimized, deterministic orbital mechanics with:
- **Hierarchical decoupling**: Separate linear and angular integration (40-60% FP64 reduction)
- **Spherical spatial partitioning**: Shell-based queries replacing disc grids (~70% culling reduction)
- **Adaptive precision**: Mixed precision per distance scale (double/float/half)
- **Event-driven corrections**: Only recompute when needed (sphere of influence changes, player Δv)
- **Frame rebasing**: Zero drift for infinite-duration simulation

## Architecture

### Component Hierarchy

```
SixDoFState (required)
├── Position, Orientation (float3, quaternion)
├── LinearVelocity, AngularVelocity (float3)
└── Used by all orbital systems

ShellMembership (required)
├── ShellIndex (Core/Inner/Outer)
├── InnerRadius, OuterRadius (double)
└── UpdateFrequency (1Hz/0.1Hz/0.01Hz)

OrbitalFrame (optional - for hierarchical frames)
├── Origin, Orientation, Scale
├── FrameParent (links to parent frame)
└── FrameWorldTransform (cached world transform)

AdaptivePrecision (optional)
└── PrecisionLevel (Double/Float/Half)

OrbitalDirtyTag (event-driven)
└── Marks entities needing recomputation
```

### System Execution Order

```
SimulationSystemGroup
├── OrbitalBudgetSystem (coordinates budgets)
├── FrameHierarchySystem (updates frame transforms)
├── SphericalShellUpdateSystem (assigns shell membership)
├── AdaptivePrecisionSystem (assigns precision levels)
├── SphericalHarmonicGravitySystem (computes accelerations)
├── MeanFieldDriftSystem (rogue object acceleration)
├── LinearVelocityIntegrationSystem (position += velocity * dt)
├── AngularVelocityIntegrationSystem (Rodrigues' formula)
├── OrbitalCorrectionSystem (event-driven updates)
├── FrameRebasingSystem (periodic barycenter shift)
└── SolidAngleCullingSystem (visibility culling)
```

## Authoring Setup

### Basic Orbital Object

1. **Add OrbitalAuthoring component** to your GameObject/prefab:
   ```csharp
   // In Unity Inspector:
   - Initial Position: (0, 0, 0)
   - Initial Linear Velocity: (0, 0, 0)
   - Initial Angular Velocity: (0, 0, 0)
   - Shell Type: Inner (Core/Inner/Outer)
   - Inner/Outer Radius: Set based on distance from center
   ```

2. **Baking result**: Entity gets:
   - `SixDoFState` with initial values
   - `ShellMembership` based on shell type
   - `AdaptivePrecision` with appropriate level

### Hierarchical Frame Setup

For multi-level orbital hierarchies (galactic → system → planet → object):

1. **Root frame** (galactic center):
   ```csharp
   OrbitalAuthoring:
   - Is Root Frame: true
   - Initial Position: (0, 0, 0)
   ```

2. **Child frames** (stellar systems):
   ```csharp
   OrbitalAuthoring:
   - Is Root Frame: false
   - Parent Frame: Reference to root frame GameObject
   - Initial Position: Relative to parent
   ```

3. **Leaf objects** (planets, ships):
   ```csharp
   OrbitalAuthoring:
   - Is Root Frame: false
   - Parent Frame: Reference to system frame GameObject
   ```

## Runtime Usage

### Querying Orbital State

```csharp
// In a system:
foreach (var (sixDoF, shell) in SystemAPI.Query<RefRO<SixDoFState>, RefRO<ShellMembership>>())
{
    float3 position = sixDoF.ValueRO.Position;
    quaternion orientation = sixDoF.ValueRO.Orientation;
    float3 linearVel = sixDoF.ValueRO.LinearVelocity;
    float3 angularVel = sixDoF.ValueRO.AngularVelocity;
    
    int shellIndex = shell.ValueRO.ShellIndex;
    // Use shell index for LOD/culling decisions
}
```

### Applying Forces/Accelerations

```csharp
// Modify linear velocity directly (gravity systems handle this automatically)
ref var sixDoF = ref SystemAPI.GetComponentRW<SixDoFState>(entity);
sixDoF.ValueRW.LinearVelocity += acceleration * deltaTime;

// Apply delta-v (player input, thrusters)
sixDoF.ValueRW.LinearVelocity += deltaV;

// Mark for correction if needed
SystemAPI.AddComponent<OrbitalDirtyTag>(entity);
```

### Spherical Shell Queries

```csharp
// Find entities within radius
var results = new NativeList<Entity>(Allocator.Temp);
SphericalShellQuerySystem.QueryEntitiesInRadius(
    ref state,
    centerPosition,
    radius,
    results
);

// Check shell membership
bool isInCore = SphericalShellQuerySystem.IsEntityInShellRadius(
    ref state,
    entity,
    ShellType.Core
);
```

### Frame Hierarchy Access

```csharp
// Get world transform from frame
if (SystemAPI.HasComponent<FrameWorldTransform>(frameEntity))
{
    var worldTransform = SystemAPI.GetComponent<FrameWorldTransform>(frameEntity);
    float3 worldPos = worldTransform.WorldPosition;
    quaternion worldRot = worldTransform.WorldOrientation;
}

// Check if frame needs update
bool isDirty = SystemAPI.HasComponent<FrameDirtyTag>(frameEntity);
```

## Space4X Integration

### Carrier Motion

Carriers automatically get 6-DoF motion via `Carrier6DoFMotionSystem`:
- Angular velocity damping (0.95x per frame)
- Max angular velocity limit (1.0 rad/s)
- Linear velocity damping (atmospheric drag simulation)

**No additional setup needed** - just add `OrbitalAuthoring` to carrier prefabs.

### Fleet Orbital Motion

Fleets inherit orbital motion from parent stellar systems:
- Update frequency: 0.01 Hz (every 600 ticks at 60Hz)
- Applies mean-field drift based on shell membership
- Links to `Space4XFleet` component

**Setup**: Add `OrbitalAuthoring` + `Space4XFleet` to fleet entities.

## Performance Considerations

### Update Frequencies

- **Core shell**: 1 Hz (high resolution, black-hole zone)
- **Inner shell**: 0.1 Hz (medium resolution, dense systems)
- **Outer shell**: 0.01 Hz (low resolution, rogue stars)

Entities automatically assigned to shells based on distance from galactic center.

### Budget Compliance

Systems respect CPU time budgets:
- `GalacticFrameSystem`: < 0.1 ms (0.001 Hz)
- `StellarOrbitSystem`: < 0.5 ms (0.01 Hz)
- `Planetary6DoFSystem`: < 2 ms (1 Hz)
- `Local6DoFSystem`: < 3 ms (60 Hz)

Check `OrbitalBudgetSystem.ShouldUpdateDomain()` before expensive operations.

### Precision Levels

- **Double**: Galactic centers (hundreds of kpc) - `PrecisionLevel.Double`
- **Float**: System-scale (AU) - `PrecisionLevel.Float`
- **Half**: Local objects - `PrecisionLevel.Half`

Vectors normalized before precision conversion to maintain determinism.

## Event-Driven Corrections

### When Corrections Trigger

1. **Sphere of influence change**: Body enters/leaves another's SOI
2. **Player interaction**: Delta-v applied (thrusters, collisions)
3. **Manual trigger**: Add `OrbitalDirtyTag` component

### Manual Correction Trigger

```csharp
// Mark entity for orbital recomputation
SystemAPI.AddComponent<OrbitalDirtyTag>(entity);

// System will recompute orbital parameters on next update
// Tag automatically removed after processing
```

## Frame Rebasing

Frames automatically rebase every 24 sim hours:
- Re-centers on local barycenter
- Shifts child entities by offset vector
- Prevents positional drift in long simulations

**No manual intervention needed** - system handles automatically.

## Common Patterns

### Creating Orbital Object at Runtime

```csharp
var entity = entityManager.CreateEntity();

// Add required components
entityManager.AddComponent<SixDoFState>(entity);
entityManager.AddComponent<ShellMembership>(entity);
entityManager.AddComponent<AdaptivePrecision>(entity);

// Set initial state
var sixDoF = new SixDoFState
{
    Position = initialPosition,
    Orientation = quaternion.identity,
    LinearVelocity = initialVelocity,
    AngularVelocity = float3.zero
};
entityManager.SetComponentData(entity, sixDoF);

// Set shell membership
var shell = new ShellMembership
{
    ShellIndex = (int)ShellType.Inner,
    InnerRadius = 1000.0,
    OuterRadius = 10000.0,
    UpdateFrequency = 0.1f,
    LastUpdateTick = 0
};
entityManager.SetComponentData(entity, shell);
```

### Applying Thrust/Delta-V

```csharp
ref var sixDoF = ref SystemAPI.GetComponentRW<SixDoFState>(entity);
float3 thrustDirection = math.normalize(targetPosition - sixDoF.ValueRO.Position);
float thrustMagnitude = 10.0f; // m/s²

sixDoF.ValueRW.LinearVelocity += thrustDirection * thrustMagnitude * deltaTime;

// Mark for correction if significant change
if (thrustMagnitude > 1.0f)
{
    SystemAPI.AddComponent<OrbitalDirtyTag>(entity);
}
```

### Querying Nearby Objects

```csharp
// Find all objects within jump range
float jumpRange = 1000f;
var nearbyObjects = new NativeList<Entity>(Allocator.Temp);

SphericalShellQuerySystem.QueryEntitiesInRadius(
    ref state,
    currentPosition,
    jumpRange,
    nearbyObjects
);

// Filter by component type if needed
foreach (var entity in nearbyObjects)
{
    if (SystemAPI.HasComponent<Space4XFleet>(entity))
    {
        // Process fleet
    }
}
```

## Troubleshooting

### Entity Not Moving

**Check**:
1. Does entity have `SixDoFState` component?
2. Is `LinearVelocity` non-zero?
3. Is entity in correct shell (check `ShellMembership`)?
4. Are orbital systems enabled in world?

### Frame Hierarchy Not Updating

**Check**:
1. Does frame have `OrbitalFrame` component?
2. Is `FrameParent` correctly linked?
3. Is parent frame's quaternion delta > threshold (0.001 rad)?
4. Check `FrameDirtyTag` presence for forced updates

### Performance Issues

**Check**:
1. Are entities in correct shells? (Core=1Hz, Inner=0.1Hz, Outer=0.01Hz)
2. Are budgets being exceeded? (check `OrbitalBudgetSystem`)
3. Are too many entities marked with `OrbitalDirtyTag`?
4. Is frame rebasing happening too frequently?

### Determinism Issues

**Check**:
1. Are all systems Burst-compiled?
2. Are precision conversions normalizing vectors first?
3. Is rewind state checked (`RewindState.Mode != RewindMode.Record`)?
4. Are timesteps fixed (`TickTimeState.FixedDeltaTime`)?

## Best Practices

1. **Always use OrbitalAuthoring** for authoring-time setup
2. **Query by shell** for LOD/culling decisions
3. **Use OrbitalDirtyTag** sparingly - only when needed
4. **Respect update frequencies** - don't query outer shell entities every frame
5. **Check budgets** before expensive operations
6. **Normalize vectors** before precision conversion
7. **Use frame hierarchy** for multi-level orbital systems
8. **Let systems handle integration** - don't manually update position/orientation

## API Reference

### Components

- `SixDoFState`: Core 6-DoF state (position, orientation, velocities)
- `ShellMembership`: Spherical shell assignment
- `OrbitalFrame`: Hierarchical frame definition
- `FrameParent`: Parent frame link
- `FrameWorldTransform`: Cached world transform
- `AdaptivePrecision`: Precision level assignment
- `OrbitalDirtyTag`: Event-driven correction marker

### Systems

- `LinearVelocityIntegrationSystem`: Integrates position
- `AngularVelocityIntegrationSystem`: Integrates orientation (Rodrigues')
- `FrameHierarchySystem`: Updates frame transforms
- `SphericalShellUpdateSystem`: Assigns shell membership
- `SphericalShellQuerySystem`: Radius-based queries
- `SphericalHarmonicGravitySystem`: Gravity computation (l=2)
- `MeanFieldDriftSystem`: Mean-field acceleration
- `OrbitalCorrectionSystem`: Event-driven corrections
- `OrbitalBudgetSystem`: Time-step budgeting
- `AdaptivePrecisionSystem`: Precision assignment
- `SolidAngleCullingSystem`: Visibility culling
- `FrameRebasingSystem`: Barycenter rebasing

### BlobAssets

- `SphericalHarmonicCoefficientsBlob`: Pre-baked gravity coefficients
- `OrbitalSplineBlob`: Pre-integrated orbital splines

## See Also

- `MovementAuthoringGuide.md` - General movement authoring
- `HierarchicalSpatialGridGuide.md` - Spatial partitioning
- `TRI_PROJECT_BRIEFING.md` - Project architecture overview

