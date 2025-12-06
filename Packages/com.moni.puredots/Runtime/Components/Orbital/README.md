# Orbital Components

ECS components for 6-DoF orbital dynamics.

## Component Reference

### `SixDoFState`
Core 6-DoF state component. **Required** for all orbital objects.

```csharp
public struct SixDoFState : IComponentData
{
    public float3 Position;           // World position (meters)
    public quaternion Orientation;    // World orientation
    public float3 LinearVelocity;     // Linear velocity (m/s)
    public float3 AngularVelocity;    // Angular velocity (rad/s, rotation vector)
}
```

**Usage**: Query in systems to read/update position and velocities.

### `ShellMembership`
Spherical shell assignment for spatial partitioning.

```csharp
public struct ShellMembership : IComponentData
{
    public int ShellIndex;            // 0=Core, 1=Inner, 2=Outer
    public double InnerRadius;        // Shell inner radius (meters)
    public double OuterRadius;        // Shell outer radius (meters)
    public float UpdateFrequency;     // Update frequency (Hz)
    public uint LastUpdateTick;        // Last update tick
}
```

**Usage**: Auto-assigned by `SphericalShellUpdateSystem` based on distance.

### `OrbitalFrame`
Hierarchical frame for multi-level orbital systems.

```csharp
public struct OrbitalFrame : IComponentData
{
    public float3 Origin;             // Frame origin (parent coords)
    public quaternion Orientation;     // Frame orientation
    public float Scale;                // Frame scale factor
    public quaternion PreviousOrientation; // For delta detection
    public float DeltaThreshold;       // Recompute threshold (rad)
}
```

**Usage**: Define frame hierarchy (galactic → system → planet → object).

### `FrameParent`
Links child frame to parent frame entity.

```csharp
public struct FrameParent : IComponentData
{
    public Entity ParentFrameEntity;   // Parent frame entity
}
```

**Usage**: Set when creating child frames in hierarchy.

### `FrameWorldTransform`
Cached world transform (computed from hierarchy).

```csharp
public struct FrameWorldTransform : IComponentData
{
    public float3 WorldPosition;      // Cached world position
    public quaternion WorldOrientation; // Cached world orientation
    public uint LastUpdateTick;        // Last update tick
}
```

**Usage**: Read-only, updated by `FrameHierarchySystem`.

### `AdaptivePrecision`
Precision level for mixed-precision calculations.

```csharp
public struct AdaptivePrecision : IComponentData
{
    public PrecisionLevel Level;      // Double/Float/Half
    public double DistanceThreshold;   // Distance threshold (meters)
}
```

**Usage**: Auto-assigned by `AdaptivePrecisionSystem` based on distance.

### `OrbitalDirtyTag`
Event-driven correction marker.

```csharp
public struct OrbitalDirtyTag : IComponentData { }
```

**Usage**: Add to entity when orbital recomputation needed (SOI change, delta-v).

## Component Dependencies

```
SixDoFState (required)
├── ShellMembership (required, auto-assigned)
├── AdaptivePrecision (optional, auto-assigned)
└── OrbitalDirtyTag (event-driven, manual)

OrbitalFrame (optional, for hierarchies)
├── FrameParent (required if not root)
└── FrameWorldTransform (auto-computed)
```

## See Also

- `Docs/Guides/Orbital6DoFIntegrationGuide.md` - Full integration guide
- `Runtime/Systems/Orbital/README.md` - System reference

