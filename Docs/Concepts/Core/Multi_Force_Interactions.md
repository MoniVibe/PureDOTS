# Multi-Force Interactions and Emergent Behaviors

**Status:** Design Document
**Category:** Core Physics - Emergent Behavior
**Scope:** PureDOTS Foundation + Visual Presentation
**Created:** 2025-12-18

---

## Purpose

Demonstrates how **multiple simultaneous forces** create emergent, visualizable physical behaviors. Entities accumulate forces from gravity, wind, explosions, magnetism, time dilation, and more - resulting in complex, realistic movement that can be visually presented.

**Core Concept:** Forces are **additive**. Each frame, an entity accumulates forces from all active force fields, then integrates them into velocity and position. This creates emergent behaviors that feel natural and dynamic.

---

## How Multi-Force Accumulation Works

### Frame-by-Frame Process

```csharp
// FRAME START - Entity at position (0, 10, 0) with velocity (0, 0, 0)

// 1. ACCUMULATION PHASE - Gather forces from all sources
receiver.AccumulatedForce = float3.zero;  // Reset

// Force 1: Gravity pulls down
receiver.AccumulatedForce += new float3(0, -9.8f, 0);  // -9.8 m/s²

// Force 2: Wind pushes east
receiver.AccumulatedForce += new float3(5.0f, 0, 0);   // +5.0 m/s² eastward

// Force 3: Explosion pushes away from center
float3 toEntity = entityPos - explosionCenter;
float3 explosionForce = normalize(toEntity) * 20f;
receiver.AccumulatedForce += explosionForce;           // +20.0 m/s² radially outward

// Force 4: Magnetic attraction to artifact
float3 toArtifact = artifactPos - entityPos;
float3 magneticForce = normalize(toArtifact) * 8f;
receiver.AccumulatedForce += magneticForce;            // +8.0 m/s² toward artifact

// Total accumulated force: Vector sum of all forces
// Result: Entity moves in diagonal arc (gravity + wind + explosion + magnetism)

// 2. INTEGRATION PHASE - Convert forces to motion
float3 acceleration = receiver.AccumulatedForce / receiver.Mass;  // F = ma
receiver.Velocity += acceleration * deltaTime;                     // v += a * dt
transform.Position += receiver.Velocity * deltaTime;               // p += v * dt

// FRAME END - Entity has moved and gained velocity
// Next frame, forces are recalculated based on new position
```

### Visual Result

The entity follows a **curved path** that's the natural result of multiple forces:
- Falls (gravity)
- Drifts sideways (wind)
- Arcs away from explosion
- Curves toward magnetic artifact
- Final trajectory = vector sum of all influences

This creates **believable, dynamic motion** without scripting specific paths.

---

## Example 1: Leaf in a Storm (Godgame)

### Setup
```csharp
// Create a leaf entity
Entity leaf = em.CreateEntity();

em.AddComponentData(leaf, new SpatialForceReceiver
{
    Mass = 0.01f,              // Very light
    DragCoefficient = 0.8f,    // High air resistance
    ForceLayerMask = ForceLayers.Physical | ForceLayers.Divine
});

em.AddComponentData(leaf, new LocalTransform
{
    Position = new float3(0, 20, 0),  // Starts high in air
    Rotation = quaternion.identity,
    Scale = 0.1f
});
```

### Active Forces

**1. Gravity** (constant downward pull)
```csharp
Entity gravity = em.CreateEntity();
em.AddComponentData(gravity, new DirectionalForceField
{
    Direction = new float3(0, -1, 0),
    Strength = 9.8f,
    Bounds = new AABB { /* entire world */ },
    ForceLayer = ForceLayers.Gravity,
    IsActive = true
});
```

**2. Wind** (fluctuating horizontal push)
```csharp
Entity wind = em.CreateEntity();
em.AddComponentData(wind, new DirectionalForceField
{
    Direction = new float3(1, 0, 0.5f).Normalize(),  // East-northeast
    Strength = 3f,  // Moderate wind
    Bounds = new AABB { /* storm area */ },
    ForceLayer = ForceLayers.Wind,
    IsActive = true
});
```

**3. Updrafts** (vortex lifting)
```csharp
for (int i = 0; i < 3; i++)
{
    Entity updraft = em.CreateEntity();
    em.AddComponentData(updraft, new VortexForceField
    {
        AxisCenter = randomPosition,
        AxisDirection = new float3(0, 1, 0),  // Vertical axis
        TangentialStrength = 2f,              // Spin
        RadialStrength = -1f,                 // Pull toward center
        AxialStrength = 5f,                   // Lift upward
        Radius = 5f,
        ForceLayer = ForceLayers.Wind,
        IsActive = true
    });
}
```

### Emergent Behavior

The leaf:
1. **Falls** due to gravity
2. **Drifts sideways** due to wind
3. **Spins and rises** when caught in updraft vortex
4. **Tumbles chaotically** due to high drag and competing forces
5. **Follows unpredictable path** - different every time based on exact position

### Visual Presentation

```csharp
// Godgame presentation system reads velocity for animation
[BurstCompile]
public partial struct LeafPresentationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Update leaf visual based on physics
        foreach (var (receiver, transform, presentation) in
            SystemAPI.Query<RefRO<SpatialForceReceiver>,
                          RefRO<LocalTransform>,
                          RefRW<GodgameLeafPresentation>>())
        {
            // Rotation speed based on force magnitude
            float forceStrength = math.length(receiver.ValueRO.AccumulatedForce);
            presentation.ValueRW.RotationSpeed = forceStrength * 2f;

            // Tilt based on velocity direction
            float3 velocity = receiver.ValueRO.Velocity;
            if (math.lengthsq(velocity) > 0.01f)
            {
                presentation.ValueRW.TiltDirection = math.normalize(velocity);
            }

            // Trail alpha based on speed
            float speed = math.length(velocity);
            presentation.ValueRW.TrailAlpha = math.saturate(speed / 10f);
        }
    }
}
```

**Visual Result:** Leaf tumbles through air, spinning faster in vortices, tilting with wind direction, leaving speed-based trail. **Feels alive and natural.**

---

## Example 2: Debris Field Explosion (Space4X)

### Setup
```csharp
// Spawn 100 debris pieces from destroyed ship
for (int i = 0; i < 100; i++)
{
    Entity debris = em.CreateEntity();

    em.AddComponentData(debris, new SpatialForceReceiver
    {
        Mass = UnityEngine.Random.Range(0.1f, 5f),  // Varying mass
        DragCoefficient = 0.02f,                     // Low space drag
        ForceLayerMask = ForceLayers.Gravity | ForceLayers.Cosmic
    });

    em.AddComponentData(debris, new LocalTransform
    {
        Position = explosionCenter + UnityEngine.Random.insideUnitSphere * 2f,
        Rotation = UnityEngine.Random.rotation,
        Scale = UnityEngine.Random.Range(0.1f, 1f)
    });
}
```

### Active Forces

**1. Explosion** (initial radial impulse)
```csharp
Entity explosion = em.CreateEntity();
em.AddComponentData(explosion, new RadialForceField
{
    Center = explosionCenter,
    Strength = -500f,  // Negative = repulsion
    Radius = 20f,
    Falloff = FalloffType.Linear,
    ForceLayer = ForceLayers.Cosmic,
    IsActive = true
});

// Auto-disable after 1 second
em.AddComponentData(explosion, new ForceFieldTimer { RemainingTime = 1f });
```

**2. Nearby Planet Gravity** (constant attraction)
```csharp
Entity planetGravity = em.CreateEntity();
em.AddComponentData(planetGravity, new RadialForceField
{
    Center = planetPosition,
    Strength = 200f,  // Positive = attraction
    Radius = 1000f,
    Falloff = FalloffType.InverseSquare,
    ForceLayer = ForceLayers.Gravity,
    IsActive = true
});
```

**3. Nearby Star Gravity** (distant but powerful)
```csharp
Entity starGravity = em.CreateEntity();
em.AddComponentData(starGravity, new RadialForceField
{
    Center = starPosition,
    Strength = 1000f,  // Very strong
    Radius = 10000f,
    Falloff = FalloffType.InverseSquare,
    ForceLayer = ForceLayers.Gravity,
    IsActive = true
});
```

**4. Solar Wind** (directional push away from star)
```csharp
Entity solarWind = em.CreateEntity();
float3 windDirection = math.normalize(explosionCenter - starPosition);
em.AddComponentData(solarWind, new DirectionalForceField
{
    Direction = windDirection,
    Strength = 2f,
    Bounds = new AABB { /* entire system */ },
    ForceLayer = ForceLayers.Cosmic,
    IsActive = true
});
```

### Emergent Behavior

Each debris piece:
1. **Blasts outward** from explosion (initial impulse)
2. **Curves toward planet** (planetary gravity)
3. **Drifts with solar wind** (directional push)
4. **Affected by star gravity** (distant but measurable)
5. **Mass matters** - heavy pieces resist forces more, light pieces scatter faster

**Result:** Realistic debris cloud that:
- Expands initially (explosion)
- Forms elongated shape (solar wind stretches cloud)
- Some pieces enter planetary orbit (right velocity + gravity)
- Some pieces escape to deep space (too fast for capture)
- Heavy/light debris separate naturally (mass-based sorting)

### Visual Presentation

```csharp
// Space4X presentation reads physics for VFX
[BurstCompile]
public partial struct DebrisPresentationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (receiver, transform, presentation) in
            SystemAPI.Query<RefRO<SpatialForceReceiver>,
                          RefRO<LocalTransform>,
                          RefRW<Space4XDebrisPresentation>>())
        {
            float speed = math.length(receiver.ValueRO.Velocity);

            // Heat glow based on velocity (atmospheric entry)
            presentation.ValueRW.HeatGlow = math.saturate((speed - 50f) / 100f);

            // Tumble rate based on force magnitude
            float torque = math.length(receiver.ValueRO.AccumulatedForce);
            presentation.ValueRW.TumbleRate = torque * 0.5f;

            // Trail intensity based on speed
            presentation.ValueRW.TrailIntensity = math.saturate(speed / 100f);

            // Sparkle on high-velocity pieces
            presentation.ValueRW.ShowSparkles = speed > 80f;
        }
    }
}
```

**Visual Result:** Debris cloud expands with varying speeds, heavy chunks tumble slowly, light fragments spin rapidly, pieces entering atmosphere glow red, high-velocity pieces leave bright trails. **Spectacular and physically grounded.**

---

## Example 3: Tornado Miracle + Gravity + Wind (Godgame)

### Setup
```csharp
// Player casts tornado miracle
public void CastTornadoMiracle(float3 targetPosition)
{
    Entity tornado = em.CreateEntity();

    // Main vortex
    em.AddComponentData(tornado, new VortexForceField
    {
        AxisCenter = targetPosition,
        AxisDirection = new float3(0, 1, 0),  // Vertical
        TangentialStrength = 30f,              // Strong spin
        RadialStrength = -10f,                 // Pull inward
        AxialStrength = 20f,                   // Lift upward
        Radius = 15f,
        ForceLayer = ForceLayers.Divine | ForceLayers.Wind,
        IsActive = true
    });

    // Duration: 10 seconds
    em.AddComponentData(tornado, new ForceFieldTimer { RemainingTime = 10f });

    // Presentation marker
    em.AddComponentData(tornado, new GodgameMiraclePresentationMarker
    {
        EffectType = MiracleFXType.Tornado
    });
}
```

### Active Forces on Villagers

**1. Gravity** (pulls down)
- Constant -9.8 m/s² downward

**2. Ambient Wind** (environmental)
- 2-5 m/s² horizontal (varies by weather)

**3. Tornado Vortex** (divine miracle)
- **Tangential**: 30 m/s² rotation around axis
- **Radial**: -10 m/s² pull toward center
- **Axial**: +20 m/s² lift upward

**4. Terrain Collision** (heightmap force)
- Upward impulse when below ground level

### Emergent Behavior

Villagers caught in tornado:
1. **Pulled toward center** (radial force)
2. **Spin around tornado** (tangential force)
3. **Lifted into air** (axial force overpowers gravity)
4. **Buffeted by wind** (ambient wind + vortex creates chaotic motion)
5. **Eventually fall** when tornado expires (gravity reasserts)
6. **Crash to ground** (terrain collision stops fall)

**Result:** Villagers spiral upward in tornado, tumbling chaotically, then scatter outward when released. **Dramatic and memorable.**

### Multi-Entity Interactions

```csharp
// Different entities react differently to same forces

// Heavy villager (blacksmith)
- Mass: 80 kg
- Gets pulled less by tornado
- Stays near ground spinning

// Light villager (child)
- Mass: 30 kg
- Lifted high into air
- Thrown far when released

// Objects (crates, barrels)
- Mass: 20-50 kg
- Become projectiles
- Can hit other villagers

// Buildings (thatched roof)
- Mass: 500 kg
- Roof pieces torn off
- Debris joins tornado
```

**Visual Result:** Differential behavior based on mass creates **believable chaos**. Heavy villagers cling to ground, light ones fly high, debris becomes dangerous projectiles.

---

## Example 4: Magnetic Artifact + Explosions (Both Games)

### Setup - Ancient Artifact

```csharp
// Magnetic artifact attracts metal objects
Entity artifact = em.CreateEntity();

em.AddComponentData(artifact, new RadialForceField
{
    Center = artifactPosition,
    Strength = 50f,  // Strong attraction
    Radius = 30f,
    Falloff = FalloffType.InverseSquare,
    ForceLayer = ForceLayers.Magnetism,
    IsActive = true
});

// Pulsing effect: strength varies over time
em.AddComponentData(artifact, new PulsingForceField
{
    BaseStrength = 50f,
    PulseAmplitude = 30f,
    PulseFrequency = 0.5f  // 2 seconds per pulse
});
```

### Active Forces on Metal Objects

**1. Artifact Magnetism** (pulsing attraction)
- 20-80 m/s² toward artifact (pulsing)

**2. Explosions** (periodic)
- Every 5 seconds, explosion at random location
- 100 m/s² radial repulsion

**3. Gravity**
- 9.8 m/s² downward

**4. Terrain Collision**
- Bounces on ground

### Emergent Behavior - Metal Debris

```csharp
// Metal object lifecycle:

// Phase 1: Distant explosion
- Blasted away from explosion
- Curves toward artifact mid-flight (magnetism wins at distance)
- Accelerates toward artifact

// Phase 2: Approaching artifact
- Magnetism strengthens (inverse square)
- Orbits artifact rapidly
- Other metal objects also orbiting (collision risk)

// Phase 3: Nearby explosion
- Blasted away again
- Fights magnetic pull
- May escape if explosion is strong enough

// Phase 4: Oscillation
- Trapped between magnetism and periodic explosions
- Chaotic trajectory
- Never settles into stable state
```

### Visual Presentation

```csharp
// Godgame version: Metal weapons/armor glow near artifact
[BurstCompile]
partial struct MagneticGlowSystem : IJobEntity
{
    [ReadOnly] public float3 ArtifactPosition;

    void Execute(
        ref GodgameMetalObjectPresentation presentation,
        in LocalTransform transform,
        in SpatialForceReceiver receiver)
    {
        // Glow intensity based on magnetic force
        float distToArtifact = math.distance(transform.Position, ArtifactPosition);
        float magneticStrength = 50f / math.max(1f, distToArtifact * distToArtifact);

        presentation.MagneticGlow = math.saturate(magneticStrength / 20f);

        // Electric arc chance based on glow
        presentation.ArcChance = presentation.MagneticGlow > 0.8f ? 0.1f : 0f;
    }
}

// Space4X version: Metal ships show EM field distortion
[BurstCompile]
partial struct EMDistortionSystem : IJobEntity
{
    [ReadOnly] public float3 ArtifactPosition;

    void Execute(
        ref Space4XShipPresentation presentation,
        in LocalTransform transform,
        in SpatialForceReceiver receiver)
    {
        // Shield shimmer based on magnetic force
        float distToArtifact = math.distance(transform.Position, ArtifactPosition);
        float magneticStrength = 50f / math.max(1f, distToArtifact * distToArtifact);

        presentation.ShieldDistortion = magneticStrength / 50f;

        // Warning indicator for pilots
        presentation.EMWarningLevel = magneticStrength > 30f ? 2 :
                                      magneticStrength > 15f ? 1 : 0;
    }
}
```

**Visual Result:** Metal objects visibly pulled toward artifact, glow/shimmer intensifies as they approach, explosions create dramatic escapes, creates **dynamic cat-and-mouse** gameplay.

---

## Example 5: Time Dilation Zones + Combat (Space4X)

### Setup - Temporal Anomaly

```csharp
// Unstable temporal anomaly
Entity anomaly = em.CreateEntity();

em.AddComponentData(anomaly, new TemporalForceField
{
    Center = anomalyPosition,
    TimeScale = 0.2f,  // 20% speed = very slow
    InnerRadius = 10f,
    OuterRadius = 30f,
    TemporalLayer = ForceLayers.Temporal,
    IsActive = true
});

// Spatial gravity well too
em.AddComponentData(anomaly, new RadialForceField
{
    Center = anomalyPosition,
    Strength = 80f,
    Radius = 50f,
    Falloff = FalloffType.InverseSquare,
    ForceLayer = ForceLayers.Gravity,
    IsActive = true
});
```

### Multi-Force Scenario - Space Battle Near Anomaly

```csharp
// Two fleets fighting near temporal anomaly

// Player fleet: Keeping distance from anomaly
Fleet playerFleet = {
    Position: 40 units from anomaly,
    TimeScale: 1.0 (normal),
    Gravity: Weak pull (manageable)
};

// Enemy fleet: Caught near anomaly edge
Fleet enemyFleet = {
    Position: 25 units from anomaly,
    TimeScale: 0.6 (60% speed - slowed),
    Gravity: Strong pull (hard to escape)
};
```

### Emergent Tactical Behavior

**Projectiles:**
```csharp
// Missile fired from player ship
Missile missile = {
    InitialVelocity: 100 m/s toward enemy,
    Path: Straight line
};

// As missile approaches anomaly:
// Frame 1: 40 units away, speed = 100 m/s, time = 1.0x
// Frame 2: 35 units away, speed = 95 m/s (gravity), time = 0.9x
// Frame 3: 30 units away, speed = 85 m/s (gravity), time = 0.7x (slowed by time field!)
// Frame 4: 25 units away, speed = 70 m/s (gravity + drag), time = 0.5x (very slow!)
// Frame 5: Curves past enemy (missed!) - too slow + gravity bent trajectory

// Visual: Missile appears to move in slow motion as it enters time field
//         Trails stretch out, becomes easy to dodge
```

**Enemy Ships:**
```csharp
// Enemy ship trying to escape anomaly

// Forces acting on ship:
// 1. Gravity: +80 m/s² toward anomaly center
// 2. Engines: -100 m/s² away from anomaly (trying to escape)
// 3. Time Dilation: Ship experiences 0.6x time
//    - Engines only output 60 m/s² effective thrust (time-slowed)
//    - Gravity still 80 m/s² (doesn't experience local time)
// 4. Result: Net force = 80 - 60 = +20 m/s² TOWARD anomaly
//    - Ship can't escape! Trapped in time well.

// Player's advantage:
// - Can fire from outside time field at full speed
// - Enemy ships move in slow motion (easy targets)
// - Enemy projectiles slow down before reaching player
```

### Visual Presentation

```csharp
[BurstCompile]
partial struct TemporalPresentationSystem : IJobEntity
{
    void Execute(
        ref Space4XShipPresentation presentation,
        in TemporalForceReceiver temporal,
        in SpatialForceReceiver spatial)
    {
        float timeScale = temporal.LocalTimeScale;

        // Visual time distortion
        presentation.TimeDistortionStrength = 1f - timeScale;  // 0.0 = normal, 1.0 = frozen

        // Trail stretching (slower = longer trails)
        presentation.TrailLength = math.lerp(1f, 5f, 1f - timeScale);

        // Animation speed
        presentation.AnimationSpeed = timeScale;

        // Engine glow (struggling against time field)
        float gravityStrength = math.length(spatial.AccumulatedForce);
        presentation.EngineOverdrive = gravityStrength > 50f && timeScale < 0.8f;

        // Screen space distortion shader
        presentation.TemporalWarp = (1f - timeScale) * 0.5f;
    }
}
```

**Visual Result:**
- Ships near anomaly move in slow motion
- Projectiles stretch and slow as they enter field
- Struggling ships glow bright as engines overdrive
- Screen space warps around time-dilated areas
- Creates **visually striking, tactically interesting** combat

---

## Design Patterns for Multi-Force Systems

### Pattern 1: Opposing Forces (Tug of War)

```csharp
// Two forces fight for control

// Attraction vs Repulsion
RadialForceField magnetism = { Strength = +50f };   // Pull
RadialForceField explosion = { Strength = -100f };  // Push

// Result: Objects oscillate between the two
// Visual: Chaotic back-and-forth motion
```

### Pattern 2: Perpendicular Forces (Orbital)

```csharp
// Gravity + Initial Velocity = Orbit

RadialForceField gravity = {
    Center = planetPos,
    Strength = +100f,
    Falloff = InverseSquare
};

// Give object initial velocity perpendicular to gravity
object.Velocity = new float3(50, 0, 0);  // Tangential

// Result: Circular/elliptical orbit
// Visual: Realistic planetary motion
```

### Pattern 3: Layered Forces (Wind Zones)

```csharp
// Multiple overlapping wind zones

DirectionalForceField groundWind = {
    Direction = (1, 0, 0),
    Strength = 5f,
    Bounds = AABB { y: 0-10 }
};

DirectionalForceField upperWind = {
    Direction = (-1, 0, 0.5f),
    Strength = 15f,
    Bounds = AABB { y: 10-100 }
};

// Result: Objects rise → change direction → drift differently
// Visual: Realistic atmospheric layers
```

### Pattern 4: Pulsing Forces (Heartbeat)

```csharp
// Force strength varies sinusoidally

float strength = baseStrength + amplitude * sin(time * frequency);
RadialForceField pulse = { Strength = strength };

// Result: Objects pulse in/out rhythmically
// Visual: Breathing, pulsing, organic motion
```

### Pattern 5: Cascading Forces (Chain Reaction)

```csharp
// Explosion triggers explosion triggers explosion

void OnExplosionComplete(Entity explosion)
{
    // Check for nearby explosives
    foreach (explosive in nearbyExplosives)
    {
        if (distance(explosive, explosion.Center) < 10f)
        {
            // Trigger secondary explosion
            CreateExplosion(explosive.Position);
        }
    }
}

// Result: Chain reaction of explosions
// Visual: Spectacular cascading detonations
```

---

## Performance Considerations

### Spatial Partitioning Critical for Multi-Force

With many active force fields, spatial partitioning becomes essential:

```csharp
// WITHOUT partitioning:
// - 10,000 entities × 100 force fields = 1,000,000 checks per frame
// - 60 FPS = 60 million checks per second ❌

// WITH partitioning:
// - Each entity checks only forces in nearby cells (avg 3-5 forces)
// - 10,000 entities × 5 forces = 50,000 checks per frame
// - 60 FPS = 3 million checks per second ✅
// - 20x faster!
```

### Layer Masking Reduces Checks

```csharp
// Example: Ethereal ghost immune to physical forces

ghost.ForceLayerMask = ForceLayers.Divine | ForceLayers.Temporal;
// Skips: Gravity, Wind, Terrain, Fluid, Magnetism
// Only checks: Divine miracles, Temporal zones

// Result: ~80% of force field checks skipped
```

### LOD for Distant Entities

```csharp
// Distant entities update less frequently

// Camera-relative distance: 200 units
// LOD level: 3 (update every 8 frames)
// Force accumulation: Only on assigned frame

// Result: 87.5% reduction in force calculations for distant entities
```

---

## Visualization Examples

### Godgame Visual Feedback

```csharp
// Villager presentation shows active forces

public struct GodgameVillagerPresentation : IComponentData
{
    // Visual indicators
    public float GravityArrow;        // Downward arrow intensity
    public float WindLean;            // Body lean angle
    public float3 WindDirection;      // Lean direction
    public float MagneticGlow;        // Blue glow near artifacts
    public float ForceMagnitude;      // Overall force strength

    // Particle effects
    public bool ShowWindParticles;
    public bool ShowMagneticSparks;
    public bool ShowTimeDistortion;

    // Animation modifiers
    public float AnimationSpeed;      // Modified by time dilation
    public float StruggleIntensity;   // Fighting strong forces
}
```

### Space4X Visual Feedback

```csharp
// Ship presentation shows force effects

public struct Space4XShipPresentation : IComponentData
{
    // Visual distortion
    public float GravityLensing;      // Screen space distortion near gravity wells
    public float TimeWarp;            // Temporal distortion effect
    public float EMFieldStrength;     // Magnetic field visibility

    // Engine/thrust visualization
    public float3 ThrustDirection;    // Engine glow direction
    public float EngineOverdrive;     // Struggling against forces

    // Trails and effects
    public float TrailLength;         // Speed-based trail
    public float TrailDistortion;     // Time dilation stretches trails
    public float IonGlow;             // Atmospheric friction

    // UI indicators
    public float ForceWarningLevel;   // HUD warning intensity
    public float3 NetForceDirection;  // Force vector overlay
}
```

---

## Summary

**Multi-force interactions create emergent, visualizable behaviors:**

✅ **Accumulation** - Forces add together each frame
✅ **Emergent** - Complex behaviors from simple rules
✅ **Visualizable** - Presentation reads physics for VFX
✅ **Believable** - Natural, realistic motion
✅ **Dynamic** - Changes based on position/mass/time
✅ **Performant** - Burst-compiled, spatially partitioned
✅ **Flexible** - Runtime toggles, strength modification

**The system enables:**
- Leaves tumbling in wind + updrafts + gravity
- Debris clouds affected by explosions + gravity + solar wind
- Villagers caught in tornado + gravity + wind
- Metal objects pulled by magnetism vs explosions
- Ships slowed by time dilation while fighting gravity

**Result:** Physics-driven behaviors that are **visually spectacular** and **feel natural**, without scripting individual paths.

---

**Related Documentation:**
- [General_Forces_System.md](General_Forces_System.md) - Technical specification
- [Forces_Integration_Guide.md](Forces_Integration_Guide.md) - Integration examples
- [Entity_Agnostic_Design.md](Entity_Agnostic_Design.md) - Foundation principles

---

**Last Updated:** 2025-12-18
**Status:** Design Document - Emergent Behavior
**Visual Impact:** High - Enables dramatic, believable physics presentation
