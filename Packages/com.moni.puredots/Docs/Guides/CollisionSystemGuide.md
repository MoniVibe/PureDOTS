# 3-Tier Collision System Guide

This guide explains how to use the 3-tier collision system for believable large vs. small collision abstraction in PureDOTS.

## Overview

The collision system implements three regimes based on object scale:

| Regime | Scale | Model | Purpose |
|--------|-------|-------|---------|
| **Micro** | < 100m objects (probes, asteroids) | Newtonian rigid-body impact | Detail & gameplay physics |
| **Meso** | 100m – 10km | Cratering / momentum transfer | Gameplay + partial visual deformation |
| **Macro** | > 10km (moons, planets) | Hydrodynamic approximation (energy map) | Cinematic destruction & thermodynamics |

Each regime uses different collision physics and damage models, automatically selected based on object radius.

## Core Concepts

### Q-Law Energy Scaling

The system uses Q-law energy scaling: `Q = 0.5 * m_projectile * v² / m_target` (J/kg)

**Q Thresholds:**
- `Q < 10³ J/kg` → Elastic bounce
- `10³ ≤ Q < 10⁶ J/kg` → Crater/partial damage
- `Q ≥ 10⁶ J/kg` → Catastrophic disruption

### Regime Selection

Regime is automatically determined by `CollisionRegimeSelectorSystem` based on:
- Object radius (< 100m = Micro, 100m-10km = Meso, > 10km = Macro)
- Radius ratio between colliding objects (if r₁/r₂ > 1e5, uses Macro)

## Components

### CollisionProperties

**Required** for all entities that participate in collision detection.

```csharp
public struct CollisionProperties : IComponentData
{
    public float Radius;        // Collision radius in meters
    public float Mass;          // Mass in kilograms
    public float RegimeThreshold;
    public CollisionRegime Regime; // Auto-computed by CollisionRegimeSelectorSystem
}
```

### Regime-Specific Components

**Micro Regime:**
- `StructuralIntegrity` - Damage tracking (0.0 = destroyed, 1.0 = pristine)

**Meso Regime:**
- `CraterState` - Crater radius, ejecta mass, impact position

**Macro Regime:**
- `ThermoState` - Temperature, melt percentage, atmosphere mass
- `MacroEnergyField` (buffer) - Voxel energy density field
- `MacroThermoState` - Aggregated thermal state

### ImpactEvent

Standardized collision event buffer emitted by all regimes:

```csharp
public struct ImpactEvent : IBufferElementData
{
    public Entity A, B;      // Colliding entities
    public float Q;           // Q value (J/kg)
    public float3 Pos;        // Impact position
    public CollisionRegime Regime;
    public uint Tick;
}
```

## Authoring Entities

### Basic Setup

1. Add `CollisionProperties` component to your entity (via authoring or runtime)
2. Set `Radius` and `Mass` values
3. Regime is automatically computed by `CollisionRegimeSelectorSystem`

**Example Authoring Component** (to be created):

```csharp
public class CollisionPropertiesAuthoring : MonoBehaviour
{
    public float Radius = 1f;
    public float Mass = 1000f;
}
```

### Regime-Specific Setup

**Micro Entities** (probes, small asteroids):
- Add `StructuralIntegrity` component
- Set `Value = 1.0` (pristine), `MaxValue = 1.0`

**Meso Entities** (asteroids, small moons):
- Add `CraterState` component (optional, created on impact)
- Add `MacroEnergyFieldConfig` if using energy field

**Macro Entities** (planets, large moons):
- Add `MacroEnergyFieldConfig` component
- Add `MacroThermoState` component
- Initialize `MacroEnergyField` buffer with voxel resolution (512-1024 cells)

## System Architecture

### System Groups

- `MicroCollisionSystemGroup` - Processes micro regime collisions
- `MesoCollisionSystemGroup` - Processes meso regime collisions  
- `MacroCollisionSystemGroup` - Processes macro regime collisions

### System Execution Order

1. `CollisionRegimeSelectorSystem` - Tags entities with regime
2. `CollisionBroadPhaseSystem` - Spatial partitioning, culling
3. `ImpactEventRouterSystem` - Routes Unity Physics events to ImpactEvent buffers
4. Regime-specific systems process ImpactEvent buffers
5. `CollisionDamageBridgeSystem` - Converts damage components when regime changes

## Integration with Unity Physics

The system integrates with Unity Physics via `ImpactEventRouterSystem`:

1. Unity Physics generates `CollisionEvent`
2. `PhysicsEventSystem` converts to `PhysicsCollisionEventElement`
3. `ImpactEventRouterSystem` converts to `ImpactEvent` with Q value and regime
4. Regime systems consume `ImpactEvent` buffers

**Required Setup:**
- Entities must have `RequiresPhysics` component
- Entities must have `PhysicsCollider` and `PhysicsVelocity` (for dynamic bodies)
- Entities must have `CollisionProperties` component

## Usage Examples

### Querying Impact Events

```csharp
// In your system
var impactEvents = SystemAPI.GetBuffer<ImpactEvent>(entity);
foreach (var impact in impactEvents)
{
    if (impact.Regime == CollisionRegime.Micro)
    {
        // Handle micro collision
        var q = impact.Q;
        var damage = ComputeDamage(q);
    }
}
```

### Checking Structural Integrity

```csharp
if (SystemAPI.HasComponent<StructuralIntegrity>(entity))
{
    var integrity = SystemAPI.GetComponent<StructuralIntegrity>(entity);
    if (integrity.Value < 0.5f)
    {
        // Entity is heavily damaged
    }
}
```

### Reading Crater State

```csharp
if (SystemAPI.HasComponent<CraterState>(entity))
{
    var crater = SystemAPI.GetComponent<CraterState>(entity);
    var radius = crater.Radius;
    var ejectaMass = crater.EjectaMass;
    // Spawn debris particles, create crater decal, etc.
}
```

### Accessing Macro Energy Field

```csharp
if (SystemAPI.HasBuffer<MacroEnergyFieldElement>(entity))
{
    var energyField = SystemAPI.GetBuffer<MacroEnergyFieldElement>(entity);
    for (int i = 0; i < energyField.Length; i++)
    {
        var energy = energyField[i].EnergyDensity;
        // Use energy for visual effects, temperature, etc.
    }
}
```

## Math Utilities

### CollisionMath

```csharp
// Compute Q value
float q = CollisionMath.ComputeQ(massProjectile, massTarget, relativeVelocity);

// Get collision outcome
CollisionOutcome outcome = CollisionMath.GetCollisionOutcome(q);

// Compute crater radius
float craterRadius = CollisionMath.ComputeCraterRadius(q);

// Compute ejecta mass
float ejectaMass = CollisionMath.ComputeEjectaMass(q);

// Momentum conservation
CollisionMath.ComputeMomentumConservation(
    vA, vB, mA, mB, normal,
    out float3 vAOut, out float3 vBOut);
```

### PerceptualScaling

```csharp
// Visual energy scaling
float visualEnergy = PerceptualScaling.ComputeVisualEnergy(q);

// Color shift scaling
float colorShift = PerceptualScaling.ComputeColorShift(q);

// Sound gain scaling
float soundGain = PerceptualScaling.ComputeSoundGain(q);

// Emissive brightness from melt percentage
float brightness = PerceptualScaling.ComputeEmissiveBrightness(meltPercentage);
```

## Visual Time Scaling

Large-mass collisions appear slow due to size/velocity perception:

```csharp
// Visual speed factor computed by CollisionVisualTimeSystem
visualSpeed = simSpeed / (1 + massScale * 0.001f);
```

Entities with `VisualTimeScale` component have their scale factor updated automatically. Use this in presentation systems for camera and particle animations.

## Damage Abstraction

Damage components automatically convert when regime thresholds are crossed:

- **Micro → Meso**: `StructuralIntegrity` converts to `CraterState`
- **Meso → Macro**: `CraterState` converts to `ThermoState`
- **Micro → Macro**: `StructuralIntegrity` converts directly to `ThermoState`

Handled by `CollisionDamageBridgeSystem` automatically.

## Optimization

### Spatial Partitioning

`CollisionBroadPhaseSystem` uses spatial grid for efficient neighbor queries:
- Only checks neighboring cells (3x3x3 grid)
- Radius-ratio culling (skips if r₁/r₂ > 1e5)

### Macro System Throttling

`MacroCollisionSystem` runs at ~0.1 Hz (every 10 ticks) to reduce CPU cost for planetary-scale collisions.

## Common Issues

### "CollisionProperties missing"

**Solution**: Add `CollisionProperties` component with `Radius` and `Mass` values.

### "ImpactEvent buffer missing"

**Solution**: The buffer is created automatically by `ImpactEventRouterSystem`. Ensure entities have `RequiresPhysics` and `CollisionProperties`.

### "Regime not updating"

**Solution**: Verify `CollisionRegimeSelectorSystem` is running. Check that `Radius` value is correct (in meters).

### "Macro energy field not updating"

**Solution**: Ensure `MacroEnergyFieldConfig` component exists and `MacroCollisionSystem` is enabled. Check update interval (runs every 10 ticks).

## Best Practices

1. **Set realistic radius/mass** - Use actual object dimensions in meters and kilograms
2. **Use appropriate regime** - Don't force entities into wrong regime (let system auto-select)
3. **Handle ImpactEvent buffers** - Clear or process events each frame to avoid accumulation
4. **Use perceptual scaling** - Apply visual/audio scaling for believable effects
5. **Monitor performance** - Macro system is throttled; micro/meso run every tick

## System Dependencies

**Required Singletons:**
- `TimeState` - For tick tracking
- `RewindState` - For rewind guards
- `SpatialGridConfig` - For broad-phase spatial partitioning

**Optional:**
- `PhysicsConfig` - For Unity Physics integration
- `MacroEnergyFieldConfig` - For macro regime energy field

## API Reference

### Components

- `CollisionProperties` - Core collision properties
- `StructuralIntegrity` - Micro regime damage
- `CraterState` - Meso regime crater data
- `ThermoState` - Macro regime thermal state
- `MacroEnergyFieldElement` - Energy field voxel buffer
- `MacroThermoState` - Aggregated macro thermal state
- `ImpactEvent` - Standardized collision event buffer
- `VisualTimeScale` - Visual time scaling factor

### Systems

- `CollisionRegimeSelectorSystem` - Tags entities with regime
- `CollisionBroadPhaseSystem` - Spatial partitioning, culling
- `ImpactEventRouterSystem` - Routes physics events
- `MicroCollisionSystem` - Micro regime physics
- `MesoCollisionSystem` - Meso regime crater generation
- `MacroCollisionSystem` - Macro regime energy diffusion
- `CollisionDamageBridgeSystem` - Damage component conversion
- `CollisionVisualTimeSystem` - Visual time scaling

### Math Utilities

- `CollisionMath` - Q-law, momentum conservation, crater math
- `PerceptualScaling` - Visual/audio scaling functions

