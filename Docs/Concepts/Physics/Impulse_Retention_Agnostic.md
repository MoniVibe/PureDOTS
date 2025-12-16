# Impulse Retention System - Agnostic Framework

## Overview

The **Impulse Retention System** provides mathematical algorithms for accumulating, storing, and releasing forces on entities under stasis effects. This framework is setting-agnostic and handles vector addition, decay calculations, damage-to-impulse conversion, mass-based release velocities, and amplification mechanics.

---

## Core Algorithms

### 1. Impulse Accumulation

Adds incoming forces to accumulated impulse buffer.

```csharp
public static void AccumulateImpulse(
    ref AccumulatedImpulse accumulated,
    float3 incomingImpulseVector,
    ImpulseType impulseType,
    StasisConfig config,
    float deltaTime)
{
    // Check if this impulse type should be accumulated
    if (!ShouldAccumulateImpulseType(impulseType, config.Type))
    {
        return;
    }

    // Apply decay to existing accumulated impulse
    if (config.DecayRatePerSecond > 0f)
    {
        float timeSinceLastDecay = (float)SystemAPI.Time.ElapsedTime - accumulated.LastDecayTime;
        float decayFactor = math.pow(1f - config.DecayRatePerSecond, timeSinceLastDecay);
        accumulated.TotalImpulseVector *= decayFactor;
        accumulated.TotalMagnitude *= decayFactor;
        accumulated.LastDecayTime = (float)SystemAPI.Time.ElapsedTime;
    }

    // Add new impulse (vector addition)
    accumulated.TotalImpulseVector += incomingImpulseVector;
    accumulated.TotalMagnitude = math.length(accumulated.TotalImpulseVector);
    accumulated.ImpulseCount++;

    // Track largest single impulse
    float incomingMagnitude = math.length(incomingImpulseVector);
    if (incomingMagnitude > accumulated.MaxImpulse)
    {
        accumulated.MaxImpulse = incomingMagnitude;
    }
}

public static bool ShouldAccumulateImpulseType(
    ImpulseType impulseType,
    StasisType stasisType)
{
    return stasisType switch
    {
        StasisType.TimeStasis => true,                          // All impulse types
        StasisType.Invulnerability => impulseType == ImpulseType.Kinetic,
        StasisType.TemporalLock => false,                       // No accumulation (reflects)
        StasisType.SelectiveKinetic => impulseType == ImpulseType.Kinetic,
        StasisType.SelectiveMagical => impulseType == ImpulseType.Magical,
        StasisType.MomentumBank => impulseType == ImpulseType.Gravitational,
        _ => false
    };
}
```

**Formula:**
```
Accumulated Impulse (with decay):
I_total(t) = I_previous × (1 - d)^Δt + I_new

Where:
- I_total = Total accumulated impulse vector
- I_previous = Previously accumulated impulse
- d = Decay rate per second (0.0 to 1.0)
- Δt = Time since last decay
- I_new = Incoming impulse vector

Vector Addition:
I_total = Σ(I_i) where I_i are individual impulse vectors

Magnitude:
|I_total| = √(I_x² + I_y² + I_z²)
```

### 2. Damage-to-Impulse Conversion

Converts prevented damage into equivalent impulse.

```csharp
public static float3 ConvertDamageToImpulse(
    float damageAmount,
    float3 damageDirection,
    float attackerMass,
    float attackerVelocity,
    StasisType stasisType,
    float damageToImpulseRatio)
{
    // Base impulse calculation
    float baseImpulse = damageAmount * damageToImpulseRatio;

    // Factor in attacker's physical properties (if available)
    if (attackerMass > 0f && attackerVelocity > 0f)
    {
        // Physical impulse = mass × velocity change
        float physicalComponent = attackerMass * attackerVelocity * 0.5f;
        baseImpulse = math.max(baseImpulse, physicalComponent);
    }

    // Apply stasis-specific multipliers
    float multiplier = stasisType switch
    {
        StasisType.TimeStasis => 2.0f,           // Double conversion
        StasisType.Invulnerability => 1.5f,      // 150% conversion
        StasisType.SelectiveKinetic => 1.0f,     // 100% conversion
        StasisType.SelectiveMagical => 1.0f,
        StasisType.MomentumBank => 0.5f,         // Lower (gravity-focused)
        _ => 1.0f
    };

    float totalImpulse = baseImpulse * multiplier;
    float3 impulseVector = math.normalize(damageDirection) * totalImpulse;

    return impulseVector;
}
```

**Damage-to-Impulse Ratios:**
```
Standard Ratios:
- Time Stasis: 2.0 (all damage becomes 2× impulse)
- Invulnerability: 1.5 (damage becomes 1.5× impulse)
- Selective: 1.0 (damage = impulse)
- Momentum Bank: 0.5 (lower, focused on gravity)

Physical Impulse Component:
I_physical = m × v × 0.5

Where:
- m = Attacker mass (kg)
- v = Attacker velocity (m/s)
- 0.5 = Energy loss coefficient (inelastic collision)

Total Impulse:
I_total = max(Damage × Ratio, I_physical) × Multiplier
```

### 3. Gravitational Impulse Accumulation

Calculates gravity accumulation over time.

```csharp
public static float3 CalculateGravitationalImpulse(
    float entityMass,
    float3 gravityVector,    // Typically (0, -10, 0) for Earth-like
    float deltaTime)
{
    // Impulse = Force × Time = (Mass × Gravity) × Time
    float3 gravitationalForce = gravityVector * entityMass;
    float3 gravitationalImpulse = gravitationalForce * deltaTime;

    return gravitationalImpulse;
}

public static void AccumulateGravityOverTime(
    ref AccumulatedImpulse accumulated,
    float entityMass,
    float3 gravityVector,
    float totalStasisTime)
{
    // Total gravity impulse over stasis duration
    float3 totalGravityImpulse = gravityVector * entityMass * totalStasisTime;
    accumulated.TotalImpulseVector += totalGravityImpulse;
    accumulated.TotalMagnitude = math.length(accumulated.TotalImpulseVector);
}
```

**Formulas:**
```
Gravitational Force:
F_gravity = m × g

Where:
- m = Entity mass (kg)
- g = Gravity acceleration (m/s², typically 10)

Gravitational Impulse:
I_gravity = F_gravity × t = m × g × t

Example:
Entity: 500 kg
Gravity: -10 m/s² (downward)
Stasis Time: 30 seconds

I_gravity = 500 × 10 × 30 = 150,000 N⋅s (downward)
```

### 4. Release Velocity Calculation

Determines entity velocity when impulses are released.

```csharp
public struct ReleaseResult
{
    public float3 Velocity;              // Release velocity (m/s)
    public float KineticEnergy;          // Energy released (joules)
    public float ImpactDamage;           // Potential collision damage
    public bool WillCauseInjury;         // If velocity is dangerous
}

public static ReleaseResult CalculateReleaseVelocity(
    float3 accumulatedImpulse,
    float entityMass,
    float amplificationFactor,
    ReleaseMode releaseMode,
    float releaseDuration)
{
    ReleaseResult result = new ReleaseResult();

    // Apply amplification
    float3 amplifiedImpulse = accumulatedImpulse * amplificationFactor;

    // Calculate velocity based on release mode
    if (releaseMode == ReleaseMode.Instantaneous)
    {
        // v = I / m (all impulse applied instantly)
        result.Velocity = amplifiedImpulse / entityMass;
    }
    else if (releaseMode == ReleaseMode.Gradual)
    {
        // Average velocity over release duration
        // Peak velocity at end: v_peak = I / m
        // Average velocity: v_avg = v_peak / 2
        float3 peakVelocity = amplifiedImpulse / entityMass;
        result.Velocity = peakVelocity / 2f; // Average during release
    }

    // Calculate kinetic energy: KE = 0.5 × m × v²
    float velocityMagnitude = math.length(result.Velocity);
    result.KineticEnergy = 0.5f * entityMass * velocityMagnitude * velocityMagnitude;

    // Calculate potential impact damage
    result.ImpactDamage = CalculateImpactDamage(
        velocityMagnitude, entityMass);

    // Check if dangerous
    result.WillCauseInjury = velocityMagnitude > 5f; // >5 m/s can cause injury

    return result;
}

public static float CalculateImpactDamage(
    float velocity,
    float mass)
{
    // Damage = (v² × m) / 200 (empirical formula)
    float damage = (velocity * velocity * mass) / 200f;
    return math.max(0f, damage);
}
```

**Formulas:**
```
Instantaneous Release:
v = I / m

Where:
- v = Velocity (m/s)
- I = Impulse (N⋅s)
- m = Mass (kg)

Gradual Release:
v_avg = (I / m) / 2

Amplified Release:
I_amplified = I × A
v = (I × A) / m

Where A = Amplification factor (1.0-4.0)

Kinetic Energy:
KE = 0.5 × m × v²

Impact Damage:
D = (v² × m) / 200

Examples:
1. Human (70 kg), 700 N⋅s impulse:
   v = 700 / 70 = 10 m/s (22 mph)
   KE = 0.5 × 70 × 10² = 3,500 J
   D = (100 × 70) / 200 = 35 damage

2. Boulder (5,000 kg), 700 N⋅s impulse:
   v = 700 / 5,000 = 0.14 m/s (barely moves)
   KE = 0.5 × 5,000 × 0.14² = 49 J
   D = (0.0196 × 5,000) / 200 = 0.5 damage

3. Arrow (0.05 kg), 700 N⋅s impulse:
   v = 700 / 0.05 = 14,000 m/s (hypersonic!)
   KE = 0.5 × 0.05 × 14,000² = 4,900,000 J
   D = (196,000,000 × 0.05) / 200 = 49,000 damage (obliterates target)
```

### 5. Vector Redirection

Redirects accumulated impulse to new direction.

```csharp
public static float3 RedirectImpulse(
    float3 originalImpulseVector,
    float3 newDirection,
    float redirectionEfficiency)
{
    // Normalize new direction
    float3 normalizedDirection = math.normalize(newDirection);

    // Preserve magnitude, apply to new direction
    float originalMagnitude = math.length(originalImpulseVector);
    float3 redirectedImpulse = normalizedDirection * originalMagnitude * redirectionEfficiency;

    return redirectedImpulse;
}

public static float CalculateRedirectionCost(
    float originalMagnitude,
    float3 originalDirection,
    float3 newDirection,
    float costPerDegree)
{
    // Calculate angle between original and new direction
    float dotProduct = math.dot(
        math.normalize(originalDirection),
        math.normalize(newDirection));

    float angleRadians = math.acos(math.clamp(dotProduct, -1f, 1f));
    float angleDegrees = math.degrees(angleRadians);

    // Cost increases with angle and magnitude
    float cost = angleDegrees * costPerDegree * (originalMagnitude / 100f);

    return cost;
}
```

**Formulas:**
```
Redirected Impulse:
I_redirected = |I_original| × d_new × η

Where:
- |I_original| = Original impulse magnitude
- d_new = New direction (normalized)
- η = Redirection efficiency (0.8-1.0)

Angle Between Vectors:
θ = arccos(d_1 · d_2)

Where d_1, d_2 are normalized direction vectors

Redirection Cost:
Cost = θ_degrees × Cost_per_degree × (|I| / 100)

Example:
Original impulse: 500 N⋅s toward north
New direction: East (90° turn)
Cost per degree: 2 mana
Efficiency: 0.9

Angle: 90°
Cost: 90 × 2 × (500/100) = 900 mana
Redirected: 500 × 0.9 = 450 N⋅s toward east
```

### 6. Reflection Mechanics (Temporal Lock)

Reflects impulses back at source.

```csharp
public static float3 ReflectImpulse(
    float3 incomingImpulseVector,
    float3 surfaceNormal,
    float reflectionCoefficient)
{
    // Perfect reflection: I_reflected = I - 2(I · n)n
    // Where n is surface normal

    float dotProduct = math.dot(incomingImpulseVector, surfaceNormal);
    float3 reflectedImpulse = incomingImpulseVector - 2f * dotProduct * surfaceNormal;

    // Apply reflection coefficient (energy loss)
    reflectedImpulse *= reflectionCoefficient;

    return reflectedImpulse;
}

public static bool ShouldReflectToSource(
    Entity sourceEntity,
    float3 reflectedDirection,
    float maxReflectionAngle)
{
    // Check if reflected direction points back toward source
    // (implementation depends on entity positions)

    // Simplified: Accept if reflection angle is within cone
    return true; // Placeholder
}
```

**Formulas:**
```
Perfect Reflection:
I_reflected = I - 2(I · n)n

Where:
- I = Incoming impulse vector
- n = Surface normal (normalized)
- · = Dot product

Reflection Coefficient:
I_final = I_reflected × ρ

Where ρ = Reflection coefficient (0.0-1.0)
- 1.0 = Perfect reflection (no energy loss)
- 0.8 = 20% energy absorbed
- 0.5 = 50% energy absorbed

Example:
Incoming: (100, -50, 0) N⋅s
Normal: (0, 1, 0) (upward)
Coefficient: 1.0 (perfect)

Dot product: 100×0 + (-50)×1 + 0×0 = -50
Reflected: (100, -50, 0) - 2×(-50)×(0, 1, 0)
         = (100, -50, 0) - (0, -100, 0)
         = (100, 50, 0) N⋅s

Result: Incoming downward impulse reflected upward
```

### 7. Gradual Release Over Time

Applies accumulated impulse over multiple frames.

```csharp
public struct GradualReleaseState
{
    public float3 TotalImpulse;          // Total impulse to release
    public float RemainingDuration;      // Seconds remaining
    public float ElapsedTime;            // Time since release began
    public float3 ImpulsePerSecond;      // Release rate
}

public static void InitializeGradualRelease(
    ref GradualReleaseState releaseState,
    float3 totalImpulse,
    float releaseDuration)
{
    releaseState.TotalImpulse = totalImpulse;
    releaseState.RemainingDuration = releaseDuration;
    releaseState.ElapsedTime = 0f;
    releaseState.ImpulsePerSecond = totalImpulse / releaseDuration;
}

public static float3 UpdateGradualRelease(
    ref GradualReleaseState releaseState,
    float deltaTime)
{
    if (releaseState.RemainingDuration <= 0f)
    {
        return float3.zero; // Release complete
    }

    // Calculate impulse for this frame
    float timeThisFrame = math.min(deltaTime, releaseState.RemainingDuration);
    float3 impulseThisFrame = releaseState.ImpulsePerSecond * timeThisFrame;

    // Update state
    releaseState.ElapsedTime += timeThisFrame;
    releaseState.RemainingDuration -= timeThisFrame;

    return impulseThisFrame;
}

public static float3 CalculateCurrentVelocity(
    GradualReleaseState releaseState,
    float entityMass)
{
    // Velocity increases linearly during gradual release
    // v(t) = (I_total / m) × (t / T)

    float progressRatio = releaseState.ElapsedTime /
        (releaseState.ElapsedTime + releaseState.RemainingDuration);

    float3 finalVelocity = releaseState.TotalImpulse / entityMass;
    float3 currentVelocity = finalVelocity * progressRatio;

    return currentVelocity;
}
```

**Formulas:**
```
Gradual Release Rate:
I_rate = I_total / T

Where:
- I_rate = Impulse per second (N⋅s/s = N)
- I_total = Total accumulated impulse
- T = Release duration (seconds)

Impulse Per Frame:
I_frame = I_rate × Δt

Where Δt = Frame delta time (typically 0.016s at 60 FPS)

Current Velocity (Linear Ramp):
v(t) = (I_total / m) × (t / T)

Where:
- t = Elapsed release time
- T = Total release duration

Example:
Total impulse: 1,200 N⋅s
Release duration: 5 seconds
Entity mass: 100 kg
Frame time: 0.016 seconds (60 FPS)

I_rate = 1,200 / 5 = 240 N⋅s/s
I_frame = 240 × 0.016 = 3.84 N⋅s per frame

At t=0: v = 0 m/s
At t=2.5s: v = (1,200/100) × (2.5/5) = 12 × 0.5 = 6 m/s
At t=5s: v = (1,200/100) × (5/5) = 12 m/s (final)
```

### 8. Decay Calculation

Applies impulse decay over time.

```csharp
public static void ApplyImpulseDecay(
    ref AccumulatedImpulse accumulated,
    float decayRatePerSecond,
    float deltaTime)
{
    if (decayRatePerSecond <= 0f) return;

    // Exponential decay: I(t) = I_0 × (1 - r)^t
    float decayFactor = math.pow(1f - decayRatePerSecond, deltaTime);

    accumulated.TotalImpulseVector *= decayFactor;
    accumulated.TotalMagnitude *= decayFactor;
    accumulated.LastDecayTime = (float)SystemAPI.Time.ElapsedTime;
}

public static float CalculateRemainingImpulseAfterTime(
    float initialImpulse,
    float decayRatePerSecond,
    float timeElapsed)
{
    // I(t) = I_0 × (1 - r)^t
    float remainingImpulse = initialImpulse * math.pow(1f - decayRatePerSecond, timeElapsed);
    return math.max(0f, remainingImpulse);
}

public static float CalculateHalfLife(float decayRatePerSecond)
{
    // Time for impulse to decay to 50%
    // 0.5 = (1 - r)^t
    // t = log(0.5) / log(1 - r)

    if (decayRatePerSecond <= 0f || decayRatePerSecond >= 1f)
        return float.PositiveInfinity;

    float halfLife = math.log(0.5f) / math.log(1f - decayRatePerSecond);
    return halfLife;
}
```

**Formulas:**
```
Exponential Decay:
I(t) = I_0 × (1 - r)^t

Where:
- I(t) = Impulse at time t
- I_0 = Initial impulse
- r = Decay rate per second (0.0-1.0)
- t = Time elapsed (seconds)

Half-Life:
t_half = ln(0.5) / ln(1 - r)

Examples:

1. 5% decay per second, 10 seconds:
   I(10) = 1,000 × (1 - 0.05)^10
        = 1,000 × 0.95^10
        = 1,000 × 0.5987
        = 598.7 N⋅s (40% lost)

   Half-life = ln(0.5) / ln(0.95) = 13.5 seconds

2. 10% decay per second, 20 seconds:
   I(20) = 1,000 × 0.9^20
        = 1,000 × 0.1216
        = 121.6 N⋅s (88% lost)

   Half-life = ln(0.5) / ln(0.9) = 6.6 seconds

3. No decay (0%), any time:
   I(t) = I_0 (100% retention)
   Half-life = ∞
```

---

## Complete Workflow Example

### Scenario: Boulder Drop with Momentum Bank

```csharp
// Setup
Entity boulder = /* ... */;
float boulderMass = 2000f; // 2,000 kg
float3 gravity = new float3(0, -10, 0); // 10 m/s² downward
float stasisDuration = 30f; // 30 seconds

// Initialize stasis effect
StasisEffect stasis = new StasisEffect
{
    Type = StasisType.MomentumBank,
    RemainingDuration = stasisDuration,
    StartTime = (float)SystemAPI.Time.ElapsedTime,
    IsActive = true,
    ManaCostPerSecond = 5f
};

AccumulatedImpulse accumulated = new AccumulatedImpulse
{
    TotalImpulseVector = float3.zero,
    TotalMagnitude = 0f,
    DecayRatePerSecond = 0f, // No decay for Momentum Bank
    LastDecayTime = (float)SystemAPI.Time.ElapsedTime,
    ImpulseCount = 0,
    MaxImpulse = 0f
};

// Step 1: Accumulate gravity over 30 seconds
AccumulateGravityOverTime(
    ref accumulated,
    boulderMass,
    gravity,
    stasisDuration);

// Result: TotalImpulseVector = (0, -600,000, 0) N⋅s

// Step 2: Apply Momentum Bank amplification (2×)
float amplificationFactor = 2.0f;
float3 amplifiedImpulse = accumulated.TotalImpulseVector * amplificationFactor;
// Result: (0, -1,200,000, 0) N⋅s

// Step 3: Calculate release velocity
ReleaseResult release = CalculateReleaseVelocity(
    amplifiedImpulse,
    boulderMass,
    1.0f, // Already amplified
    ReleaseMode.Instantaneous,
    0f);

// Results:
// Velocity: (0, -600, 0) m/s (600 m/s downward, Mach 1.8!)
// Kinetic Energy: 0.5 × 2,000 × 600² = 360,000,000 J (360 MJ)
// Impact Damage: (600² × 2,000) / 200 = 3,600,000 damage

Debug.Log($"Boulder Release:");
Debug.Log($"- Velocity: {release.Velocity} m/s");
Debug.Log($"- Kinetic Energy: {release.KineticEnergy / 1_000_000f} MJ");
Debug.Log($"- Impact Damage: {release.ImpactDamage}");
Debug.Log($"- Will Cause Injury: {release.WillCauseInjury}");

// Output:
// Boulder Release:
// - Velocity: (0, -600, 0) m/s
// - Kinetic Energy: 360 MJ
// - Impact Damage: 3,600,000
// - Will Cause Injury: True
```

---

## Integration with Three Pillar ECS

### Body Pillar (60 Hz)

High-frequency collision and accumulation:

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct StasisCollisionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Process collisions with stasised entities
        foreach (var (stasis, accumulated, config, mass, entity)
                 in SystemAPI.Query<RefRO<StasisEffect>,
                                     RefRW<AccumulatedImpulse>,
                                     RefRO<StasisConfig>,
                                     RefRO<PhysicsMass>>()
                     .WithEntityAccess())
        {
            if (!stasis.ValueRO.IsActive) continue;

            // Check for collisions this frame
            // (Integration with physics system)
            // For each collision, calculate impulse and accumulate
        }
    }
}
```

### Mind Pillar (1 Hz)

Medium-frequency decision-making:

```csharp
[UpdateInGroup(typeof(MindPillarSystemGroup))]
public partial struct StasisReleaseDecisionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Decide when to release stasis
        foreach (var (stasis, accumulated, ai)
                 in SystemAPI.Query<RefRW<StasisEffect>,
                                     RefRO<AccumulatedImpulse>,
                                     RefRO<AIController>>())
        {
            if (!stasis.ValueRO.IsActive) continue;

            // AI evaluates: Is accumulated impulse sufficient?
            // Should release now for maximum effect?
            // Or wait for more accumulation?

            if (ShouldReleaseStasis(accumulated.ValueRO, ai.ValueRO))
            {
                stasis.ValueRW.IsActive = false;
                stasis.ValueRW.RemainingDuration = 0f;
            }
        }
    }

    private bool ShouldReleaseStasis(
        AccumulatedImpulse accumulated,
        AIController ai)
    {
        // Release if impulse magnitude exceeds threshold
        return accumulated.TotalMagnitude > ai.ReleaseThreshold;
    }
}
```

### Aggregate Pillar (0.2 Hz)

Low-frequency decay and maintenance:

```csharp
[UpdateInGroup(typeof(AggregatePillarSystemGroup))]
public partial struct StasisDecaySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (accumulated, config)
                 in SystemAPI.Query<RefRW<AccumulatedImpulse>,
                                     RefRO<StasisConfig>>())
        {
            ApplyImpulseDecay(
                ref accumulated.ValueRW,
                config.ValueRO.DecayRatePerSecond,
                deltaTime);
        }
    }
}
```

---

## Summary

The **Impulse Retention Agnostic Framework** provides:

1. **Accumulation Algorithms**: Vector addition, decay calculation, damage-to-impulse conversion
2. **Release Calculations**: Instantaneous vs gradual, velocity from impulse, impact damage
3. **Redirection Mechanics**: Vector rotation, cost calculation, efficiency factors
4. **Reflection Formulas**: Perfect reflection, surface normals, energy conservation
5. **Gravity Integration**: Gravitational impulse accumulation, amplification
6. **Decay Models**: Exponential decay, half-life calculation, time-based reduction

**Core Formulas:**
```
Impulse Accumulation: I_total(t) = I_previous × (1-d)^Δt + I_new
Release Velocity: v = (I × A) / m
Kinetic Energy: KE = 0.5 × m × v²
Impact Damage: D = (v² × m) / 200
Reflection: I_reflected = I - 2(I · n)n
Decay: I(t) = I_0 × (1-r)^t
Half-Life: t_half = ln(0.5) / ln(1-r)
```

This framework is fully compatible with Unity DOTS and the Three Pillar ECS architecture, with systems distributed across Body (60 Hz collision/accumulation), Mind (1 Hz release decisions), and Aggregate (0.2 Hz decay maintenance).
