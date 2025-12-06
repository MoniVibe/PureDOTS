# Orbital 6-DoF Systems

Optimized orbital dynamics systems for deterministic 6-DoF motion in spherical galaxies.

## Quick Reference

### Core Systems

| System | Purpose | Update Rate | Budget |
|--------|---------|-------------|--------|
| `LinearVelocityIntegrationSystem` | Integrates position (symplectic Euler) | 60 Hz | < 3 ms |
| `AngularVelocityIntegrationSystem` | Integrates orientation (Rodrigues') | 60 Hz | < 3 ms |
| `FrameHierarchySystem` | Updates frame transforms (delta threshold) | 60 Hz | < 2 ms |
| `SphericalShellUpdateSystem` | Assigns shell membership | Variable | < 0.5 ms |
| `SphericalHarmonicGravitySystem` | Computes gravity (l=2 quadrupole) | 60 Hz | < 2 ms |
| `MeanFieldDriftSystem` | Mean-field acceleration for rogue objects | 60 Hz | < 1 ms |
| `OrbitalCorrectionSystem` | Event-driven corrections | Event-driven | < 1 ms |
| `OrbitalBudgetSystem` | Coordinates time budgets | 60 Hz | < 0.1 ms |
| `AdaptivePrecisionSystem` | Assigns precision levels | 60 Hz | < 0.5 ms |
| `SolidAngleCullingSystem` | Visibility culling | 60 Hz | < 1 ms |
| `FrameRebasingSystem` | Barycenter rebasing | 24h intervals | < 2 ms |

### Required Components

- `SixDoFState` - Position, orientation, velocities (required)
- `ShellMembership` - Shell assignment (required)
- `AdaptivePrecision` - Precision level (optional, auto-assigned)
- `OrbitalFrame` - Frame hierarchy (optional)
- `OrbitalDirtyTag` - Correction marker (event-driven)

### Usage Pattern

```csharp
// 1. Authoring: Add OrbitalAuthoring component to GameObject
// 2. Runtime: Query SixDoFState for position/orientation
// 3. Apply forces: Modify LinearVelocity directly
// 4. Trigger correction: Add OrbitalDirtyTag when needed
```

### Shell Types

- **Core** (0): 1 Hz update, high resolution, black-hole zone
- **Inner** (1): 0.1 Hz update, medium resolution, dense systems  
- **Outer** (2): 0.01 Hz update, low resolution, rogue stars

### Integration Order

Systems execute in this order (respect dependencies):
1. Budget coordination
2. Frame hierarchy updates
3. Shell membership assignment
4. Precision assignment
5. Gravity computation
6. Mean-field drift
7. Linear integration
8. Angular integration
9. Event corrections
10. Frame rebasing
11. Visibility culling

## See Full Documentation

See `Docs/Guides/Orbital6DoFIntegrationGuide.md` for complete integration guide.

