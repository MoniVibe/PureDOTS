# Locomotion System

## Overview

The Locomotion System provides a flexible, entity-agnostic framework for defining how entities move through space. Entities can possess multiple locomotion capabilities (walking, running, flying, hovering, gliding, etc.) and switch between them at runtime based on context, stamina, terrain, or player input.

**Key Principles**:
- **Multi-Modal**: Entities can have multiple locomotion modes (walk + run + fly)
- **Directionality**: Each mode supports mono-directional, bi-directional, or omnidirectional movement
- **Runtime Switching**: Modes can change based on terrain, state, or input
- **Entity-Agnostic**: Same system for villagers, ships, mechs, creatures, vehicles
- **Physics-Integrated**: Works with existing forces system
- **Deterministic**: Same inputs → same movement (rewind-compatible)
- **Animation-Friendly**: Locomotion state drives animation selection

---

## Core Concepts

### Locomotion Modes

A **locomotion mode** defines how an entity moves and what constraints apply:

```csharp
public enum LocomotionMode : byte
{
    // Ground Locomotion
    Walking = 0,        // Standard ground movement, medium speed
    Running = 1,        // Fast ground movement, higher stamina cost
    Sprinting = 2,      // Maximum speed ground movement, rapid stamina drain
    Crawling = 3,       // Slow, stealthy ground movement
    Climbing = 4,       // Vertical movement on surfaces
    Swimming = 5,       // Movement through water
    Wading = 6,         // Slow movement through shallow water

    // Aerial Locomotion
    Flying = 10,        // Powered flight, active thrust
    Gliding = 11,       // Unpowered descent with horizontal control
    Hovering = 12,      // Stationary aerial position, continuous thrust
    Soaring = 13,       // Passive flight using thermals/wind

    // Specialized Locomotion
    Jumping = 20,       // Vertical leap (discrete, not continuous)
    Dashing = 21,       // Short burst of extreme speed
    Teleporting = 22,   // Instant position change
    Phasing = 23,       // Movement through solid objects

    // Vehicle/Mechanical Locomotion
    Wheeled = 30,       // Ground vehicles with wheels
    Tracked = 31,       // Ground vehicles with treads
    Legged = 32,        // Multi-legged mechanical locomotion
    Jetpack = 33,       // Vertical/horizontal thrust propulsion
    Antigrav = 34,      // Anti-gravity propulsion
    RocketBoost = 35,   // High-speed burst propulsion

    // Space/Orbital Locomotion
    Orbital = 40,       // Following orbital mechanics
    Thruster = 41,      // Space thruster-based movement
    Inertial = 42,      // Momentum-based (no friction)
    Warp = 43,          // FTL travel

    // Special/Magical Locomotion
    Levitation = 50,    // Magical hovering
    Ethereal = 51,      // Non-physical movement
    Burrowing = 52,     // Underground movement
    WallRunning = 53,   // Running on vertical/inverted surfaces

    Stationary = 255    // Cannot move
}
```

### Directionality

Each locomotion mode has a **directionality** defining valid movement directions:

```csharp
public enum MovementDirectionality : byte
{
    Stationary = 0,         // Cannot move (0 DOF)

    // Linear Movement (1 DOF)
    MonoDirectional = 1,    // Forward only (cars, some creatures)
    BiDirectional = 2,      // Forward/backward (humans, bipeds)

    // Planar Movement (2 DOF)
    PlanarForward = 10,     // Forward + strafe (FPS-style)
    PlanarOmni = 11,        // Any horizontal direction (RTS units, omniwheels)

    // Volumetric Movement (3 DOF)
    VolumetricLimited = 20, // 3D but with constraints (fish: pitch limited)
    VolumetricOmni = 21,    // Full 3D freedom (spacecraft, flying creatures)

    // Rotational Freedom (combined with above)
    AxisLocked = 30,        // Rotation locked to specific axis (ground units)
    FullRotation = 31,      // Free rotation in all axes (spacecraft)

    // Special Patterns
    PathConstrained = 40,   // Follows predefined path (rails, roads)
    GridLocked = 41,        // Snaps to grid positions
    OrbitConstrained = 42,  // Constrained to orbital mechanics
    Custom = 255            // Custom directionality logic
}
```

---

## Component Architecture

### Locomotion Capabilities

Entities define their **available** locomotion modes:

```csharp
public struct LocomotionCapabilities : IBufferElementData
{
    public LocomotionMode Mode;              // Walking, Flying, etc.
    public MovementDirectionality Directionality; // How it can move
    public float MaxSpeed;                   // m/s
    public float Acceleration;               // m/s²
    public float Deceleration;               // m/s²
    public float TurnRate;                   // degrees/s
    public float StaminaCost;                // Stamina/s (0 = no cost)
    public float EnergyCost;                 // Energy/s (for powered modes)
    public TerrainMask ValidTerrain;         // Where this mode works
    public bool RequiresSurface;             // Needs ground/wall contact
    public float MinAltitude;                // Minimum height above ground
    public float MaxAltitude;                // Maximum height above ground
}

// Example: Human villager capabilities
LocomotionCapabilities[] humanLocomotion = {
    // Walking: Omnidirectional, works on ground
    new LocomotionCapabilities {
        Mode = LocomotionMode.Walking,
        Directionality = MovementDirectionality.PlanarOmni,
        MaxSpeed = 1.4f,        // ~5 km/h
        Acceleration = 3.0f,
        TurnRate = 360f,
        StaminaCost = 0.1f,
        RequiresSurface = true,
        ValidTerrain = TerrainMask.Ground
    },

    // Running: Omnidirectional, higher speed and stamina cost
    new LocomotionCapabilities {
        Mode = LocomotionMode.Running,
        Directionality = MovementDirectionality.PlanarOmni,
        MaxSpeed = 5.0f,        // ~18 km/h
        Acceleration = 4.0f,
        TurnRate = 270f,        // Wider turns when running
        StaminaCost = 1.0f,     // 10× walking cost
        RequiresSurface = true,
        ValidTerrain = TerrainMask.Ground
    },

    // Swimming: Slow, works in water
    new LocomotionCapabilities {
        Mode = LocomotionMode.Swimming,
        Directionality = MovementDirectionality.VolumetricLimited,
        MaxSpeed = 0.8f,
        Acceleration = 1.5f,
        TurnRate = 180f,
        StaminaCost = 2.0f,     // Very tiring
        ValidTerrain = TerrainMask.Water
    }
};

// Example: Dragon capabilities
LocomotionCapabilities[] dragonLocomotion = {
    // Walking: Slow, quadrupedal
    new LocomotionCapabilities {
        Mode = LocomotionMode.Walking,
        Directionality = MovementDirectionality.MonoDirectional,
        MaxSpeed = 2.0f,
        TurnRate = 90f,
        RequiresSurface = true,
        ValidTerrain = TerrainMask.Ground
    },

    // Flying: Fast, omnidirectional in 3D
    new LocomotionCapabilities {
        Mode = LocomotionMode.Flying,
        Directionality = MovementDirectionality.VolumetricOmni,
        MaxSpeed = 20.0f,
        Acceleration = 8.0f,
        TurnRate = 120f,
        StaminaCost = 0.5f,
        EnergyCost = 10.0f,     // Magical energy cost
        MinAltitude = 2.0f,
        MaxAltitude = 1000.0f
    },

    // Gliding: No stamina cost, slower, requires altitude
    new LocomotionCapabilities {
        Mode = LocomotionMode.Gliding,
        Directionality = MovementDirectionality.VolumetricLimited,
        MaxSpeed = 15.0f,
        TurnRate = 60f,
        StaminaCost = 0.0f,     // Free (gravity-powered)
        MinAltitude = 5.0f
    },

    // Hovering: Stationary aerial position
    new LocomotionCapabilities {
        Mode = LocomotionMode.Hovering,
        Directionality = MovementDirectionality.Stationary,
        MaxSpeed = 0.0f,
        StaminaCost = 0.2f,
        EnergyCost = 5.0f
    }
};

// Example: Spaceship capabilities
LocomotionCapabilities[] spaceshipLocomotion = {
    // Thruster: Full 3D omnidirectional, no friction
    new LocomotionCapabilities {
        Mode = LocomotionMode.Thruster,
        Directionality = MovementDirectionality.VolumetricOmni | MovementDirectionality.FullRotation,
        MaxSpeed = 100.0f,      // m/s
        Acceleration = 20.0f,
        TurnRate = 45f,
        EnergyCost = 50.0f,     // Reactor power
        ValidTerrain = TerrainMask.Space
    },

    // Warp: FTL travel, extreme speed
    new LocomotionCapabilities {
        Mode = LocomotionMode.Warp,
        Directionality = MovementDirectionality.MonoDirectional,
        MaxSpeed = 299792458.0f, // Speed of light
        Acceleration = 1000.0f,
        TurnRate = 1.0f,        // Very wide turns at warp
        EnergyCost = 1000.0f,
        ValidTerrain = TerrainMask.Space
    }
};
```

### Active Locomotion State

Tracks the **current** locomotion mode and movement:

```csharp
public struct ActiveLocomotion : IComponentData
{
    public LocomotionMode CurrentMode;       // Which mode is active
    public float3 Velocity;                  // Current velocity vector
    public float3 DesiredDirection;          // Input direction (normalized)
    public float DesiredSpeed;               // Target speed (0.0 to 1.0)
    public float CurrentSpeed;               // Actual speed (m/s)
    public quaternion FacingRotation;        // Current facing direction
    public LocomotionState State;            // Transitioning, stable, etc.
    public float TransitionProgress;         // 0.0 to 1.0 during mode switch
    public bool IsGrounded;                  // Surface contact
    public float AltitudeAboveGround;        // Height above terrain
}

public enum LocomotionState : byte
{
    Idle = 0,            // Not moving
    Accelerating = 1,    // Speeding up
    Cruising = 2,        // At target speed
    Decelerating = 3,    // Slowing down
    Turning = 4,         // Changing direction
    Transitioning = 5,   // Switching locomotion modes
    Falling = 6,         // In freefall
    Stunned = 7          // Cannot control movement
}
```

### Movement Constraints

Runtime constraints that modify or restrict locomotion:

```csharp
public struct MovementConstraints : IComponentData
{
    public float SpeedMultiplier;            // 0.0 to 1.0+ (slowed, hasted)
    public float TurnRateMultiplier;         // 0.0 to 1.0+ (turning impaired)
    public MovementConstraintFlags Flags;
    public LocomotionMode ForcedMode;        // Override mode (climbing, swimming)
    public float3 ConstrainedDirection;      // Force movement direction (knockback, currents)
    public float ConstraintStrength;         // How strong is the constraint
}

[Flags]
public enum MovementConstraintFlags : uint
{
    None = 0,

    // Impairments
    Rooted = 1 << 0,             // Cannot move at all
    Slowed = 1 << 1,             // Reduced speed
    Snared = 1 << 2,             // Cannot use certain modes (no flying)
    Stunned = 1 << 3,            // No control over movement

    // Environmental
    InWater = 1 << 4,            // Swimming required
    InAir = 1 << 5,              // Flying/gliding/falling
    OnIce = 1 << 6,              // Reduced friction
    InMud = 1 << 7,              // Increased resistance

    // Forced Movement
    Knockback = 1 << 8,          // External force applied
    Pulled = 1 << 9,             // Being pulled by force
    Pushed = 1 << 10,            // Being pushed by force

    // Mode Restrictions
    CannotWalk = 1 << 11,
    CannotRun = 1 << 12,
    CannotFly = 1 << 13,
    CannotSwim = 1 << 14,

    // Special States
    Mounted = 1 << 15,           // On mount/vehicle (use their locomotion)
    Grappled = 1 << 16,          // Attached to another entity
    Carried = 1 << 17            // Being carried (no self-locomotion)
}
```

---

## Locomotion Mode Selection System

Determines which mode to use based on context:

```csharp
[BurstCompile]
public partial struct LocomotionModeSelectionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (active, capabilities, constraints, transform) in SystemAPI.Query<
            RefRW<ActiveLocomotion>,
            DynamicBuffer<LocomotionCapabilities>,
            RefRO<MovementConstraints>,
            RefRO<LocalTransform>>())
        {
            // If movement is constrained (rooted, stunned), skip mode selection
            if ((constraints.ValueRO.Flags & MovementConstraintFlags.Rooted) != 0 ||
                (constraints.ValueRO.Flags & MovementConstraintFlags.Stunned) != 0)
            {
                active.ValueRW.CurrentSpeed = 0f;
                active.ValueRW.State = LocomotionState.Idle;
                continue;
            }

            // If forced mode is set, use it
            if (constraints.ValueRO.ForcedMode != LocomotionMode.Walking)
            {
                if (active.ValueRO.CurrentMode != constraints.ValueRO.ForcedMode)
                {
                    SwitchLocomotionMode(ref active.ValueRW, constraints.ValueRO.ForcedMode, capabilities);
                }
                continue;
            }

            // Select mode based on context
            var desiredMode = SelectBestMode(
                active.ValueRO,
                capabilities,
                constraints.ValueRO,
                transform.ValueRO.Position
            );

            if (desiredMode != active.ValueRO.CurrentMode)
            {
                SwitchLocomotionMode(ref active.ValueRW, desiredMode, capabilities);
            }
        }
    }

    private LocomotionMode SelectBestMode(
        ActiveLocomotion active,
        DynamicBuffer<LocomotionCapabilities> capabilities,
        MovementConstraints constraints,
        float3 position)
    {
        // Terrain-based selection
        var terrain = GetTerrainAtPosition(position);

        // Check if current mode is still valid for terrain
        var currentCapability = GetCapability(capabilities, active.CurrentMode);
        if ((currentCapability.ValidTerrain & terrain) == 0)
        {
            // Current mode invalid, need to switch
            // Example: Walking into water → Swimming
            return FindFirstValidMode(capabilities, terrain);
        }

        // Speed-based selection (if multiple ground modes available)
        // Example: Walking vs Running based on desired speed
        if (active.DesiredSpeed > 0.7f) // 70% speed threshold
        {
            // Try to use faster mode (Running instead of Walking)
            var fasterMode = FindFasterMode(capabilities, active.CurrentMode, terrain);
            if (fasterMode != active.CurrentMode)
                return fasterMode;
        }

        // Altitude-based selection
        if (active.AltitudeAboveGround > 5.0f && !active.IsGrounded)
        {
            // In air, prefer flying or gliding
            if (HasCapability(capabilities, LocomotionMode.Flying))
                return LocomotionMode.Flying;
            if (HasCapability(capabilities, LocomotionMode.Gliding))
                return LocomotionMode.Gliding;

            // Falling (not a mode, but a state)
            active.State = LocomotionState.Falling;
        }

        // Default: keep current mode
        return active.CurrentMode;
    }

    private void SwitchLocomotionMode(
        ref ActiveLocomotion active,
        LocomotionMode newMode,
        DynamicBuffer<LocomotionCapabilities> capabilities)
    {
        active.CurrentMode = newMode;
        active.State = LocomotionState.Transitioning;
        active.TransitionProgress = 0f;

        // May need to adjust velocity for new mode constraints
        var capability = GetCapability(capabilities, newMode);

        // Clamp speed to new mode's max speed
        if (active.CurrentSpeed > capability.MaxSpeed)
        {
            active.CurrentSpeed = capability.MaxSpeed;
            active.Velocity = math.normalize(active.Velocity) * capability.MaxSpeed;
        }
    }
}
```

---

## Movement Update System

Applies locomotion to entity position:

```csharp
[BurstCompile]
public partial struct LocomotionMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (active, capabilities, transform) in SystemAPI.Query<
            RefRW<ActiveLocomotion>,
            DynamicBuffer<LocomotionCapabilities>,
            RefRW<LocalTransform>>())
        {
            var capability = GetCapability(capabilities, active.ValueRO.CurrentMode);

            // Calculate target velocity
            float3 targetVelocity = active.ValueRO.DesiredDirection *
                                   (active.ValueRO.DesiredSpeed * capability.MaxSpeed);

            // Apply acceleration/deceleration
            float acceleration = math.length(targetVelocity) > math.length(active.ValueRO.Velocity)
                ? capability.Acceleration
                : capability.Deceleration;

            active.ValueRW.Velocity = math.lerp(
                active.ValueRO.Velocity,
                targetVelocity,
                math.min(1f, acceleration * deltaTime)
            );

            active.ValueRW.CurrentSpeed = math.length(active.ValueRO.Velocity);

            // Apply directionality constraints
            active.ValueRW.Velocity = ApplyDirectionalityConstraints(
                active.ValueRO.Velocity,
                capability.Directionality,
                transform.ValueRO.Rotation
            );

            // Update position
            transform.ValueRW.Position += active.ValueRO.Velocity * deltaTime;

            // Update facing direction (if not fully rotational)
            if (capability.Directionality != MovementDirectionality.FullRotation)
            {
                UpdateFacing(ref active.ValueRW, ref transform.ValueRW, capability.TurnRate, deltaTime);
            }

            // Update locomotion state
            UpdateLocomotionState(ref active.ValueRW);
        }
    }

    private float3 ApplyDirectionalityConstraints(
        float3 velocity,
        MovementDirectionality directionality,
        quaternion currentRotation)
    {
        switch (directionality)
        {
            case MovementDirectionality.Stationary:
                return float3.zero;

            case MovementDirectionality.MonoDirectional:
                // Can only move forward relative to facing direction
                var forward = math.mul(currentRotation, new float3(0, 0, 1));
                var forwardSpeed = math.dot(velocity, forward);
                return forward * math.max(0, forwardSpeed); // Clamp to forward only

            case MovementDirectionality.BiDirectional:
                // Can move forward/backward relative to facing
                var forward2 = math.mul(currentRotation, new float3(0, 0, 1));
                var forwardSpeed2 = math.dot(velocity, forward2);
                return forward2 * forwardSpeed2; // Allow negative (backward)

            case MovementDirectionality.PlanarOmni:
                // Free movement on horizontal plane, no vertical
                return new float3(velocity.x, 0, velocity.z);

            case MovementDirectionality.VolumetricOmni:
                // Full 3D freedom
                return velocity;

            case MovementDirectionality.VolumetricLimited:
                // 3D but with pitch constraints (fish can't go straight up)
                var horizontalDir = math.normalize(new float3(velocity.x, 0, velocity.z));
                var verticalComponent = velocity.y;
                var speed = math.length(velocity);

                // Clamp vertical angle to ±45 degrees
                var maxVertical = speed * 0.707f; // sin(45°)
                verticalComponent = math.clamp(verticalComponent, -maxVertical, maxVertical);

                var horizontalSpeed = math.sqrt(speed * speed - verticalComponent * verticalComponent);
                return new float3(
                    horizontalDir.x * horizontalSpeed,
                    verticalComponent,
                    horizontalDir.z * horizontalSpeed
                );

            default:
                return velocity;
        }
    }

    private void UpdateFacing(
        ref ActiveLocomotion active,
        ref LocalTransform transform,
        float turnRate,
        float deltaTime)
    {
        if (math.lengthsq(active.Velocity) < 0.01f)
            return; // Not moving, don't update facing

        // Calculate desired facing from velocity
        var desiredForward = math.normalize(active.Velocity);
        var desiredRotation = quaternion.LookRotationSafe(desiredForward, math.up());

        // Smoothly rotate towards desired facing
        float maxRotation = math.radians(turnRate * deltaTime);
        transform.Rotation = math.slerp(transform.Rotation, desiredRotation, maxRotation);
    }

    private void UpdateLocomotionState(ref ActiveLocomotion active)
    {
        if (active.CurrentSpeed < 0.1f)
        {
            active.State = LocomotionState.Idle;
        }
        else if (active.CurrentSpeed < active.DesiredSpeed * 0.9f)
        {
            active.State = LocomotionState.Accelerating;
        }
        else if (active.CurrentSpeed > active.DesiredSpeed * 1.1f)
        {
            active.State = LocomotionState.Decelerating;
        }
        else
        {
            active.State = LocomotionState.Cruising;
        }
    }
}
```

---

## Integration with Forces System

Locomotion works alongside the forces system:

```csharp
// Locomotion provides desired movement, forces provide external influences

[BurstCompile]
public partial struct LocomotionWithForcesSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (active, forces, transform) in SystemAPI.Query<
            RefRW<ActiveLocomotion>,
            RefRO<AccumulatedForces>,  // From forces system
            RefRW<LocalTransform>>())
        {
            // Locomotion provides intended movement
            float3 locomotionVelocity = active.ValueRO.Velocity;

            // Forces provide external influences (wind, gravity, explosions)
            float3 forceAcceleration = forces.ValueRO.NetForce; // Assuming NetForce is acceleration

            // Combine: locomotion + forces
            float3 totalVelocity = locomotionVelocity + (forceAcceleration * deltaTime);

            // Update position
            transform.ValueRW.Position += totalVelocity * deltaTime;

            // Update locomotion velocity (for next frame)
            active.ValueRW.Velocity = locomotionVelocity; // Locomotion intent unchanged
        }
    }
}

// Example force interactions:
// - Wind force pushes flying entities
// - Gravity pulls non-hovering entities down
// - Explosions knock back entities regardless of locomotion
// - Water currents affect swimming entities
```

---

## Directionality Examples

### Mono-Directional (Forward Only)

**Examples**: Cars, some fish, projectiles

```csharp
// Can only move in the direction they're facing
// To change direction, must rotate first, then move

// Input: Move left
// Result:
//   1. Rotate left (turn in place or wide arc)
//   2. Once facing left, move forward

Directionality = MovementDirectionality.MonoDirectional;
TurnRate = 90f; // Degrees per second
```

### Bi-Directional (Forward/Backward)

**Examples**: Humans, bipedal creatures, some mechs

```csharp
// Can move forward or backward relative to facing
// Strafing not allowed

// Input: Move backward
// Result: Move in reverse (slower than forward)

Directionality = MovementDirectionality.BiDirectional;
BackwardSpeedMultiplier = 0.6f; // 60% speed when backing up
```

### Planar Omnidirectional

**Examples**: RTS units, top-down game characters, hovercrafts

```csharp
// Can move in any horizontal direction instantly
// No need to rotate first

// Input: Move diagonally
// Result: Instant diagonal movement
// Facing: Rotates to match movement direction (or independent)

Directionality = MovementDirectionality.PlanarOmni;
TurnRate = 360f; // Can snap to any direction
```

### Volumetric Omnidirectional

**Examples**: Spacecraft, ghosts, flying creatures with perfect control

```csharp
// Can move in any 3D direction instantly
// Full rotational freedom

// Input: Move up-left-forward
// Result: Instant 3D movement in that direction

Directionality = MovementDirectionality.VolumetricOmni | MovementDirectionality.FullRotation;
```

### Custom Directionality Patterns

**Examples**: Vehicles with complex constraints, animated creatures

```csharp
public struct CustomDirectionalityPattern : IComponentData
{
    public float ForwardSpeedMultiplier;   // 1.0
    public float BackwardSpeedMultiplier;  // 0.5
    public float StrafeSpeedMultiplier;    // 0.7
    public float DiagonalPenalty;          // 0.9 (10% slower when moving diagonally)
    public bool RequireRotationBeforeMove; // True for tanks, false for omniwheels
    public float MinTurnRadius;            // Meters (for vehicles)
}

// Tank example:
CustomDirectionalityPattern tankPattern = new()
{
    ForwardSpeedMultiplier = 1.0f,
    BackwardSpeedMultiplier = 0.4f,
    StrafeSpeedMultiplier = 0.0f,  // No strafing
    RequireRotationBeforeMove = false, // Can turn in place
    MinTurnRadius = 5.0f // Wide turns when moving
};

// Crab example (sideways movement):
CustomDirectionalityPattern crabPattern = new()
{
    ForwardSpeedMultiplier = 1.0f,
    BackwardSpeedMultiplier = 1.0f,
    StrafeSpeedMultiplier = 1.0f,   // Equal speed all directions
    DiagonalPenalty = 1.0f          // No diagonal penalty
};
```

---

## Animation Integration

Locomotion state drives animation selection:

```csharp
public struct LocomotionAnimationState : IComponentData
{
    public FixedString64Bytes CurrentAnimation; // "Walk", "Run", "Fly", "Hover"
    public float AnimationSpeed;                // Playback speed multiplier
    public float BlendWeight;                   // For transitioning between animations
}

[BurstCompile]
public partial struct LocomotionAnimationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (active, animState) in SystemAPI.Query<
            RefRO<ActiveLocomotion>,
            RefRW<LocomotionAnimationState>>())
        {
            // Select animation based on mode and state
            switch (active.ValueRO.CurrentMode)
            {
                case LocomotionMode.Walking:
                    animState.ValueRW.CurrentAnimation = "Walk";
                    animState.ValueRW.AnimationSpeed = active.ValueRO.CurrentSpeed / 1.4f; // Normalize to walk speed
                    break;

                case LocomotionMode.Running:
                    animState.ValueRW.CurrentAnimation = "Run";
                    animState.ValueRW.AnimationSpeed = active.ValueRO.CurrentSpeed / 5.0f;
                    break;

                case LocomotionMode.Flying:
                    animState.ValueRW.CurrentAnimation = "Fly";
                    animState.ValueRW.AnimationSpeed = 1.0f;
                    break;

                case LocomotionMode.Hovering:
                    animState.ValueRW.CurrentAnimation = "Hover";
                    animState.ValueRW.AnimationSpeed = 1.0f;
                    break;

                case LocomotionMode.Gliding:
                    animState.ValueRW.CurrentAnimation = "Glide";
                    animState.ValueRW.AnimationSpeed = 1.0f;
                    break;
            }

            // Handle transitions
            if (active.ValueRO.State == LocomotionState.Transitioning)
            {
                // Blend between animations
                animState.ValueRW.BlendWeight = active.ValueRO.TransitionProgress;
            }
            else
            {
                animState.ValueRW.BlendWeight = 1.0f; // Full weight on current animation
            }
        }
    }
}
```

---

## Cross-Game Examples

### Godgame: Villagers

```csharp
// Human villagers: Walk, run, swim
LocomotionCapabilities[] villagerModes = {
    new() { Mode = Walking, Directionality = PlanarOmni, MaxSpeed = 1.4f },
    new() { Mode = Running, Directionality = PlanarOmni, MaxSpeed = 5.0f, StaminaCost = 1.0f },
    new() { Mode = Swimming, Directionality = VolumetricLimited, MaxSpeed = 0.8f, ValidTerrain = Water }
};

// Fantasy creatures: Multiple locomotion types
// Dragon: Walk (slow), Fly (fast), Hover (stationary), Glide (efficient)
// Griffin: Walk, Run, Fly
// Merfolk: Swim (fast), Walk (slow, only on land)
// Ghosts: Levitation (ignore terrain), Phasing (through walls)
```

### Godgame: Ships

```csharp
// Sailing ship: Forward movement with wind influence
LocomotionCapabilities sailingShip = new()
{
    Mode = LocomotionMode.Wheeled, // "Wheeled" used abstractly for watercraft
    Directionality = MovementDirectionality.MonoDirectional,
    MaxSpeed = 8.0f,
    TurnRate = 30f,              // Slow, wide turns
    ValidTerrain = TerrainMask.Water,
    RequiresSurface = true
};

// Rowing boat: Bi-directional, can reverse
LocomotionCapabilities rowingBoat = new()
{
    Mode = LocomotionMode.Wheeled,
    Directionality = MovementDirectionality.BiDirectional,
    MaxSpeed = 3.0f,
    TurnRate = 90f,
    ValidTerrain = TerrainMask.Water
};
```

### Space4X: Spacecraft

```csharp
// Fighter: Fast, agile, full 3D control
LocomotionCapabilities fighter = new()
{
    Mode = LocomotionMode.Thruster,
    Directionality = MovementDirectionality.VolumetricOmni | MovementDirectionality.FullRotation,
    MaxSpeed = 200.0f,
    Acceleration = 50.0f,
    TurnRate = 180f,
    ValidTerrain = TerrainMask.Space
};

// Capital ship: Slow, limited turn rate
LocomotionCapabilities capitalShip = new()
{
    Mode = LocomotionMode.Thruster,
    Directionality = MovementDirectionality.VolumetricOmni | MovementDirectionality.FullRotation,
    MaxSpeed = 50.0f,
    Acceleration = 5.0f,
    TurnRate = 15f,              // Very slow turns
    ValidTerrain = TerrainMask.Space
};

// Station: Orbital mechanics
LocomotionCapabilities station = new()
{
    Mode = LocomotionMode.Orbital,
    Directionality = MovementDirectionality.OrbitConstrained,
    MaxSpeed = 7.8f,             // Orbital velocity
    TurnRate = 0f,               // No turning (follows orbit)
    ValidTerrain = TerrainMask.Space
};
```

### Space4X: Mechs

```csharp
// Mech: Multiple modes (walk, run, jump, jetpack)
LocomotionCapabilities[] mechModes = {
    new() { Mode = Walking, Directionality = PlanarOmni, MaxSpeed = 3.0f },
    new() { Mode = Running, Directionality = PlanarOmni, MaxSpeed = 8.0f, EnergyCost = 10.0f },
    new() {
        Mode = Jumping,
        Directionality = MovementDirectionality.MonoDirectional,
        MaxSpeed = 15.0f, // Vertical jump speed
        StaminaCost = 5.0f
    },
    new() {
        Mode = Jetpack,
        Directionality = VolumetricLimited,
        MaxSpeed = 12.0f,
        EnergyCost = 50.0f,
        MaxAltitude = 100.0f
    }
};
```

---

## Terrain Interaction

Locomotion modes interact with terrain:

```csharp
[Flags]
public enum TerrainMask : uint
{
    None = 0,
    Ground = 1 << 0,      // Solid ground
    Water = 1 << 1,       // Liquid water
    Air = 1 << 2,         // Open air/sky
    Space = 1 << 3,       // Vacuum/zero-g
    Underground = 1 << 4, // Below ground
    Wall = 1 << 5,        // Vertical surface (for climbing/wall-running)
    Ceiling = 1 << 6,     // Inverted surface
    Ice = 1 << 7,         // Slippery terrain
    Mud = 1 << 8,         // Viscous terrain
    Lava = 1 << 9,        // Hazardous liquid
    Ethereal = 1 << 10,   // Non-physical (ghosts)
    Any = 0xFFFFFFFF
}

// Automatic mode switching based on terrain
// Example: Entity walking into water
// System detects terrain change: Ground → Water
// System checks capabilities for Water terrain
// If Swimming available: Switch to Swimming
// If not: Apply drowning effect, force Stationary or death
```

---

## Stamina and Energy Costs

Locomotion modes consume resources:

```csharp
[BurstCompile]
public partial struct LocomotionResourceCostSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (active, capabilities, stamina, energy) in SystemAPI.Query<
            RefRO<ActiveLocomotion>,
            DynamicBuffer<LocomotionCapabilities>,
            RefRW<Stamina>,
            RefRW<EnergyStorage>>())
        {
            var capability = GetCapability(capabilities, active.ValueRO.CurrentMode);

            // Stamina cost (biological energy)
            if (capability.StaminaCost > 0f && active.ValueRO.CurrentSpeed > 0.1f)
            {
                float staminaDrain = capability.StaminaCost * deltaTime;
                stamina.ValueRW.Current -= staminaDrain;

                // If stamina depleted, force slower mode
                if (stamina.ValueRO.Current <= 0f)
                {
                    // Cannot sustain this mode, switch to slower mode
                    // Running → Walking, Flying → Gliding, etc.
                }
            }

            // Energy cost (mechanical/magical power)
            if (capability.EnergyCost > 0f && active.ValueRO.CurrentSpeed > 0.1f)
            {
                float energyDrain = capability.EnergyCost * deltaTime;
                energy.ValueRW.CurrentStored -= energyDrain;

                // If energy depleted, mode fails
                if (energy.ValueRO.CurrentStored <= 0f)
                {
                    // Jetpack runs out of fuel → fall
                    // Spaceship thrusters fail → drift
                }
            }
        }
    }
}
```

---

## Advanced Locomotion: Pathfinding Integration

Locomotion capabilities inform pathfinding:

```csharp
public struct PathfindingLocomotionContext : IComponentData
{
    public LocomotionMode PrimaryMode;       // Default mode for pathfinding
    public TerrainMask TraversableTerrain;   // What terrain can be crossed
    public float PathCostMultiplier;         // Cost modifier for this mode
    public bool CanFly;                      // Can path through air
    public bool CanSwim;                     // Can path through water
    public bool CanClimb;                    // Can path up walls
    public float MaxSlope;                   // Maximum climbable slope (degrees)
}

// Pathfinding uses locomotion to determine valid paths:
// - Ground-only units: Avoid water, find bridges
// - Flying units: Straight-line paths ignoring terrain
// - Amphibious units: Prefer ground but can cross water
// - Climbing units: Can traverse vertical surfaces
```

---

## Special Locomotion Cases

### Mounted/Vehicle Locomotion

When entity is mounted, use mount's locomotion:

```csharp
public struct MountedState : IComponentData
{
    public Entity MountEntity;               // What are we mounted on
    public bool InheritLocomotion;           // Use mount's locomotion instead
    public float3 MountOffset;               // Position relative to mount
}

// Rider's locomotion disabled, mount's locomotion used instead
// Dismount: Restore rider's original locomotion capabilities
```

### Formation Movement

Multiple entities move as coordinated group:

```csharp
public struct FormationMember : IComponentData
{
    public Entity FormationLeader;
    public int FormationSlot;
    public float3 FormationOffset;
    public bool MatchLeaderSpeed;            // Sync speed with leader
}

// Formation members adjust their locomotion to maintain formation
// Leader's locomotion determines formation speed
// Members may use different modes (some walk, some fly) but sync speed
```

### Knockback and Forced Movement

External forces override locomotion temporarily:

```csharp
public struct ForcedMovement : IComponentData
{
    public float3 ForcedVelocity;            // Override velocity
    public float Duration;                   // How long (seconds)
    public bool AllowControl;                // Can player influence direction?
}

// Example: Explosion knocks entity back
// ForcedMovement applied for 0.5 seconds
// During this time, locomotion input is ignored or reduced
// After duration, normal locomotion resumes
```

---

## Performance Considerations

### System Ordering

```csharp
// Locomotion systems run in specific order:
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial class LocomotionSystemGroup : ComponentSystemGroup
{
    // 1. Mode Selection (choose appropriate locomotion)
    // 2. Resource Costs (drain stamina/energy)
    // 3. Movement Update (apply velocity)
    // 4. Terrain Interaction (check ground, water, etc.)
    // 5. Animation Update (sync with presentation)
}
```

### Optimization

- **Locomotion capabilities are static**: Stored in `DynamicBuffer`, rarely change
- **Active locomotion is dynamic**: Updated every frame, cache-friendly struct
- **Mode selection can be throttled**: Check every 0.1s instead of every frame
- **Burst compilation**: All systems Burst-compiled for SIMD

**Profiling Targets**:
```
Mode Selection:     <0.1ms for 1000 entities
Movement Update:    <0.5ms for 1000 entities
Animation Sync:     <0.2ms for 1000 entities
──────────────────────────────────────────────
Total:              <0.8ms for 1000 entities
```

---

## Debugging and Visualization

Development tools for locomotion debugging:

```csharp
public struct LocomotionDebugVisualization : IComponentData
{
    public bool ShowVelocityVector;          // Draw velocity as arrow
    public bool ShowDesiredDirection;        // Draw input direction
    public bool ShowCapabilities;            // Display available modes
    public bool ShowConstraints;             // Visualize movement restrictions
    public bool ShowDirectionality;          // Show valid movement directions
}

// In-editor gizmos:
// - Green arrow: Current velocity
// - Blue arrow: Desired direction
// - Yellow cone: Valid movement directions (based on directionality)
// - Red X: Constrained/blocked directions
// - Colored ring: Available locomotion modes
```

---

## Future Extensions

### Advanced Flight Physics

```csharp
// Wing-based flight with lift/drag simulation
public struct WingLocomotion : IComponentData
{
    public float WingArea;           // m²
    public float LiftCoefficient;    // Aerodynamic efficiency
    public float DragCoefficient;
    public float StallSpeed;         // Minimum speed for lift
    public float OptimalSpeed;       // Most efficient speed
}
```

### Procedural Animation

```csharp
// Inverse kinematics for foot placement, wing flapping
public struct ProceduralLocomotionAnimation : IComponentData
{
    public float StrideLength;       // For walking/running
    public float StridePeriod;       // Time per step
    public int FootCount;            // Bipedal, quadrupedal, etc.
    public float WingFlapFrequency;  // For flying
}
```

### Adaptive Locomotion

```csharp
// Learn optimal locomotion for terrain over time
public struct AdaptiveLocomotion : IComponentData
{
    public float TerrainAdaptation;  // How well adapted to current terrain
    public float LearningRate;       // How quickly to adapt
}
```

---

## Summary

The Locomotion System provides:

1. **Multi-Modal Movement**: Entities can walk, run, fly, hover, glide, swim, and more
2. **Flexible Directionality**: Mono, bi, and omnidirectional movement patterns
3. **Context-Aware Mode Switching**: Automatically selects appropriate mode for terrain/state
4. **Resource Management**: Stamina and energy costs for different modes
5. **Physics Integration**: Works alongside forces system for realistic movement
6. **Animation-Friendly**: Locomotion state drives animation selection
7. **Cross-Game Compatibility**: Same system for villagers, creatures, ships, mechs, spacecraft
8. **Deterministic**: Rewind-compatible, same inputs produce same results
9. **Performance-Optimized**: Burst-compiled, cache-friendly, SIMD-accelerated
10. **Extensible**: Easy to add new locomotion modes and directionality patterns

**Key Innovation**: Separation of locomotion **capabilities** (what an entity *can* do) from active locomotion **state** (what it's *currently* doing) allows for rich, context-dependent movement behavior without complex state machines.
