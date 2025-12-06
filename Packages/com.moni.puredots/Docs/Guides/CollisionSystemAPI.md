# Collision System API Reference

Quick reference for collision system components, systems, and utilities.

## Components

### CollisionProperties

**Namespace**: `PureDOTS.Runtime.Physics`

Core collision properties component. Required for all entities participating in collision detection.

```csharp
public struct CollisionProperties : IComponentData
{
    public float Radius;              // Collision radius in meters
    public float Mass;                // Mass in kilograms
    public float RegimeThreshold;     // Auto-computed threshold
    public CollisionRegime Regime;    // Auto-computed regime (Micro/Meso/Macro)
}
```

**Authoring**: Use `CollisionPropertiesAuthoring` component in Unity Editor.

### ImpactEvent

**Namespace**: `PureDOTS.Runtime.Physics`

Standardized collision event buffer. Created by `ImpactEventRouterSystem`, consumed by regime systems.

```csharp
public struct ImpactEvent : IBufferElementData
{
    public Entity A, B;               // Colliding entities
    public float Q;                   // Q value (J/kg)
    public float3 Pos;                // Impact position
    public CollisionRegime Regime;    // Collision regime
    public uint Tick;                 // Tick when impact occurred
}
```

**Usage**: Query buffer with `SystemAPI.GetBuffer<ImpactEvent>(entity)`. Process and clear each frame.

### StructuralIntegrity

**Namespace**: `PureDOTS.Runtime.Physics`

Micro regime damage tracking component.

```csharp
public struct StructuralIntegrity : IComponentData
{
    public float Value;      // Integrity 0.0-1.0 (0 = destroyed, 1 = pristine)
    public float MaxValue;   // Maximum integrity (for regeneration)
}
```

### CraterState

**Namespace**: `PureDOTS.Runtime.Physics`

Meso regime crater state component.

```csharp
public struct CraterState : IComponentData
{
    public float Radius;           // Crater radius in meters
    public float EjectaMass;       // Ejecta mass in kilograms
    public float3 ImpactPosition; // Impact position
    public uint FormationTick;     // Tick when crater formed
}
```

### ThermoState

**Namespace**: `PureDOTS.Runtime.Physics`

Macro regime thermal state component.

```csharp
public struct ThermoState : IComponentData
{
    public float Temperature;      // Temperature in Kelvin
    public float MeltPercentage;    // Crust melt % (0.0-1.0)
    public float AtmosphereMass;    // Atmosphere mass in kg
    public float BaseTemperature;   // Base temperature before impacts
}
```

## Systems

### CollisionRegimeSelectorSystem

**Namespace**: `PureDOTS.Systems.Physics`

Tags entities with appropriate collision regime based on radius.

**Execution**: Runs before collision system groups.

**Dependencies**: Requires `CollisionProperties` component.

### ImpactEventRouterSystem

**Namespace**: `PureDOTS.Systems.Physics`

Routes Unity Physics `CollisionEvent` to `ImpactEvent` buffers with Q value computation.

**Execution**: Runs in `PhysicsPostEventSystemGroup` after `PhysicsEventSystem`.

**Dependencies**: Requires `RequiresPhysics`, `CollisionProperties` components.

### MicroCollisionSystem

**Namespace**: `PureDOTS.Systems.Physics`

Processes micro regime collisions (< 100m). Uses 6-DoF momentum conservation.

**Execution**: Runs in `MicroCollisionSystemGroup`.

**Updates**: `PhysicsVelocity`, `StructuralIntegrity`.

### MesoCollisionSystem

**Namespace**: `PureDOTS.Systems.Physics`

Processes meso regime collisions (100m-10km). Generates craters and debris.

**Execution**: Runs in `MesoCollisionSystemGroup`.

**Updates**: `CraterState`.

### MacroCollisionSystem

**Namespace**: `PureDOTS.Systems.Physics`

Processes macro regime collisions (> 10km). Energy field diffusion and thermo state.

**Execution**: Runs in `MacroCollisionSystemGroup`. Throttled to ~0.1 Hz.

**Updates**: `MacroEnergyField`, `MacroThermoState`, `ThermoState`.

### CollisionBroadPhaseSystem

**Namespace**: `PureDOTS.Systems.Physics`

Spatial partitioning and radius-ratio culling for collision pairs.

**Execution**: Runs before collision system groups.

**Culling**: Skips collisions if radius ratio > 1e5.

## Math Utilities

### CollisionMath

**Namespace**: `PureDOTS.Runtime.Math`

Static utility class for collision calculations.

**Methods**:
- `ComputeQ(massProjectile, massTarget, velocity)` - Compute Q value
- `GetCollisionOutcome(q)` - Get outcome from Q value
- `ComputeMomentumConservation(...)` - 6-DoF momentum conservation
- `ClampVelocity(velocity, escapeVel, terminalVel)` - Clamp to limits
- `ComputeCraterRadius(q)` - Crater radius from Q
- `ComputeEjectaMass(q)` - Ejecta mass from Q

**Constants**:
- `Q_THRESHOLD_ELASTIC` (1e3 J/kg)
- `Q_THRESHOLD_CRATER` (1e6 J/kg)
- `Q_THRESHOLD_CATASTROPHIC` (1e6 J/kg)
- `REGIME_MICRO_MAX` (100m)
- `REGIME_MESO_MIN` (100m)
- `REGIME_MESO_MAX` (10km)
- `REGIME_MACRO_MIN` (10km)
- `RADIUS_RATIO_CULL_THRESHOLD` (1e5)

### PerceptualScaling

**Namespace**: `PureDOTS.Runtime.Math`

Static utility class for perceptual scaling functions.

**Methods**:
- `ComputeVisualEnergy(q)` - Visual energy scaling (pow(Q, 0.4))
- `ComputeColorShift(q)` - Color shift scaling (pow(Q, 0.2))
- `ComputeSoundGain(q)` - Sound gain scaling (pow(Q, 0.3))
- `ComputeEmissiveBrightness(meltPercentage)` - Brightness from melt
- `ComputeAtmosphereColorShift(lossPercentage)` - Color shift from atmosphere loss
- `ComputeDustSpawnRate(dustMass)` - Dust spawn rate from mass

## Integration Points

### With Unity Physics

1. Entities need `RequiresPhysics` component
2. Entities need `PhysicsCollider` and `PhysicsVelocity` (for dynamic)
3. Entities need `CollisionProperties` component
4. `ImpactEventRouterSystem` automatically routes events

### With Existing Damage System

`ImpactEvent` can be bridged to existing `DamageEvent` system:

```csharp
foreach (var impact in impactEvents)
{
    var damageEvent = new DamageEvent
    {
        SourceEntity = impact.A,
        TargetEntity = impact.B,
        RawDamage = impact.Q * damageMultiplier,
        Type = DamageType.Physical,
        Tick = impact.Tick
    };
    damageBuffer.Add(damageEvent);
}
```

### With Presentation Systems

Use `VisualTimeScale` component for time-scaling:

```csharp
if (SystemAPI.HasComponent<VisualTimeScale>(entity))
{
    var timeScale = SystemAPI.GetComponent<VisualTimeScale>(entity);
    var visualDeltaTime = UnityEngine.Time.deltaTime * timeScale.Scale;
    // Apply to camera/particle animations
}
```

## Performance Considerations

- **Micro/Meso systems**: Run every tick (60 Hz)
- **Macro system**: Throttled to ~0.1 Hz (every 10 ticks)
- **Broad-phase**: Uses spatial grid for O(1) neighbor queries
- **Radius-ratio culling**: Skips fine physics for extreme size ratios

## Thread Safety

All systems are Burst-compiled and thread-safe. Use `EntityCommandBuffer` for structural changes (component add/remove).

