# Projectile Deflection & Fragmentation System (Agnostic Framework)

## Overview
Defines mathematical frameworks for projectile deflection by environmental forces, active manipulation, and mid-flight destruction. Algorithms cover force application, mass/velocity resistance, shredding mechanics, fragment spawning with 280° spread patterns, debuff application, and defensive interception. Game-agnostic formulas enable complex projectile physics.

**Integration**: Body ECS (60 Hz) for projectile physics, Mind ECS (1 Hz) for concentration/perception tracking.

---

## Core Components

### Projectile Component
```csharp
public struct ProjectileComponent : IComponentData
{
    public float3 Position;            // m (meters)
    public float3 Velocity;            // m/s
    public float Mass;                 // kg
    public float DragCoefficient;      // 0-1 (air resistance)
    public float StructuralIntegrity;  // 0-1 (1 = intact, 0 = destroyed)
    public ProjectileType Type;
    public bool IsShredded;
}

public enum ProjectileType : byte
{
    Light,       // Low mass, high deflection (arrows, darts)
    Medium,      // Standard projectiles (bolts, bullets)
    Heavy,       // High mass, low deflection (ballista bolts, cannon balls)
    Fragment     // Spawned from shredded projectile
}
```

### Deflection Force Component
```csharp
public struct DeflectionForceComponent : IComponentData
{
    public float3 ForceVector;         // N (newtons)
    public float3 ForceOrigin;         // Position of force source
    public float ForceRadius;          // Effective radius (m)
    public float ForceMagnitude;       // Peak force at origin (N)
    public ForceType Type;
    public float DecayRate;            // Force decay per meter (0-1)
}

public enum ForceType : byte
{
    Explosion,          // Blast wave, radial force
    Wind,               // Constant directional force
    ActiveDeflection,   // Telekinesis, force magic
    Gravity,            // Gravitational pull/push
    Field               // Energy field, barrier
}
```

### Fragmentation Component
```csharp
public struct FragmentationComponent : IComponentData
{
    public int BaseFragmentCount;      // Number of fragments to spawn
    public float FragmentDamagePercent; // % of original damage per fragment
    public float SpreadAngleDegrees;   // Cone angle (280° typical)
    public float3 SpreadDirection;     // Primary spread direction (normalized)
    public float FragmentVelocityMin;  // m/s (minimum fragment speed)
    public float FragmentVelocityMax;  // m/s (maximum fragment speed)
    public bool SpawnDebuffs;          // Fragments carry debuffs
}
```

---

## Agnostic Algorithms

### Projectile Deflection

#### Calculate Deflection from Force
```csharp
/// <summary>
/// Calculate projectile deflection angle from applied force
/// Agnostic: F = ma, deflection depends on mass and velocity
/// </summary>
public static float CalculateDeflectionAngle(
    float3 projectileVelocity,         // m/s
    float projectileMass,              // kg
    float3 appliedForce,               // N
    float forceApplicationTime,        // seconds (how long force applied)
    float dragCoefficient)             // 0-1 (air resistance reduces deflection)
{
    // Newton's second law: F = ma → a = F/m
    float3 acceleration = appliedForce / projectileMass;

    // Velocity change from force: Δv = a × t
    float3 velocityChange = acceleration * forceApplicationTime;

    // Drag reduces velocity change
    velocityChange *= (1f - dragCoefficient * 0.3f); // Max 30% reduction from drag

    // New velocity: v_new = v_old + Δv
    float3 newVelocity = projectileVelocity + velocityChange;

    // Deflection angle: angle between old and new velocity vectors
    float deflectionAngle = math.acos(math.dot(
        math.normalize(projectileVelocity),
        math.normalize(newVelocity)
    ));

    return math.degrees(deflectionAngle); // Return in degrees
}
```

#### Calculate Force from Explosion at Distance
```csharp
/// <summary>
/// Calculate force magnitude at distance from explosion epicenter
/// Agnostic: Force decays with distance (inverse square law modified)
/// </summary>
public static float CalculateExplosionForceAtDistance(
    float peakForceMagnitude,          // N (force at epicenter)
    float distanceFromEpicenter,       // m
    float explosionRadius,             // m (effective blast radius)
    float decayRate)                   // 0-1 (how quickly force drops)
{
    // Outside blast radius: no force
    if (distanceFromEpicenter > explosionRadius)
        return 0f;

    // Linear decay within blast radius (modified for gameplay)
    float distanceFactor = 1f - (distanceFromEpicenter / explosionRadius);

    // Apply decay rate (higher decay = steeper falloff)
    float decayFactor = math.pow(distanceFactor, 1f + decayRate);

    float forceAtDistance = peakForceMagnitude * decayFactor;

    return forceAtDistance;
}
```

#### Calculate Deflection Resistance from Mass and Velocity
```csharp
/// <summary>
/// Calculate how resistant projectile is to deflection
/// Agnostic: Momentum (p = mv) determines resistance
/// </summary>
public static float CalculateDeflectionResistance(
    float projectileMass,              // kg
    float projectileSpeed,             // m/s (magnitude of velocity)
    ProjectileType type)
{
    // Base resistance from momentum: p = mv
    float momentum = projectileMass * projectileSpeed;

    // Type modifier (some projectiles inherently more stable)
    float typeModifier = 1f;
    switch (type)
    {
        case ProjectileType.Light:
            typeModifier = 0.7f; // Less resistant (arrows, darts)
            break;
        case ProjectileType.Medium:
            typeModifier = 1.0f; // Standard resistance
            break;
        case ProjectileType.Heavy:
            typeModifier = 1.5f; // More resistant (ballista bolts)
            break;
        case ProjectileType.Fragment:
            typeModifier = 0.5f; // Very light, easily deflected
            break;
    }

    float resistance = momentum * typeModifier;

    return resistance;
}
```

#### Apply Environmental Force to Projectile
```csharp
/// <summary>
/// Apply environmental force to projectile, calculate new trajectory
/// Agnostic: Integrates force over time, updates velocity
/// </summary>
public static float3 ApplyForceToProjectile(
    float3 currentVelocity,
    float projectileMass,
    float3 appliedForce,
    float deltaTime,                   // seconds (frame time)
    float deflectionResistance)
{
    // Acceleration: a = F/m
    float3 acceleration = appliedForce / projectileMass;

    // Resistance reduces acceleration
    acceleration /= (1f + deflectionResistance * 0.1f);

    // Velocity change: Δv = a × Δt
    float3 velocityChange = acceleration * deltaTime;

    // New velocity
    float3 newVelocity = currentVelocity + velocityChange;

    return newVelocity;
}
```

### Projectile Shredding

#### Calculate Shredding Probability
```csharp
/// <summary>
/// Calculate probability projectile is shredded by force
/// Agnostic: Structural integrity vs applied force
/// </summary>
public static float CalculateShreddingProbability(
    float projectileStructuralIntegrity, // 0-1 (1 = intact, 0 = destroyed)
    float appliedForceMagnitude,         // N
    float projectileMass,                // kg
    float projectileSpeed,               // m/s
    ForceType forceType)
{
    // Force per unit mass: stress = F/m
    float stress = appliedForceMagnitude / projectileMass;

    // High-speed projectiles less likely to shred (pass through before destruction)
    float speedFactor = 1f - math.clamp(projectileSpeed / 100f, 0f, 0.5f); // Max 50% reduction

    // Force type modifier
    float typeModifier = 1f;
    switch (forceType)
    {
        case ForceType.Explosion:
            typeModifier = 1.2f; // High shredding (shrapnel, shockwave)
            break;
        case ForceType.Wind:
            typeModifier = 0.3f; // Low shredding (mostly deflection)
            break;
        case ForceType.ActiveDeflection:
            typeModifier = 0.5f; // Moderate shredding (controlled force)
            break;
        case ForceType.Field:
            typeModifier = 1.5f; // High shredding (cutting/grinding fields)
            break;
    }

    // Base shredding probability
    float baseProbability = stress / 500f; // Normalize (500N = 100% shred for 1kg projectile)

    // Apply modifiers
    float shreddingProbability = baseProbability * speedFactor * typeModifier * (1f - projectileStructuralIntegrity);

    return math.clamp(shreddingProbability, 0f, 0.95f); // Max 95% shred chance
}
```

#### Reduce Structural Integrity from Damage
```csharp
/// <summary>
/// Reduce projectile structural integrity when damaged
/// Agnostic: Cumulative damage weakens projectile
/// </summary>
public static float DamageStructuralIntegrity(
    float currentIntegrity,            // 0-1
    float damageForce,                 // N
    float projectileMass)              // kg
{
    // Damage factor: force per unit mass
    float damageFactor = damageForce / (projectileMass * 1000f); // Normalize

    // Reduce integrity
    float newIntegrity = currentIntegrity - damageFactor;

    return math.clamp(newIntegrity, 0f, 1f);
}
```

### Fragment Spawning

#### Calculate Fragment Count
```csharp
/// <summary>
/// Calculate number of fragments spawned from shredded projectile
/// Agnostic: Based on projectile size and shredding force
/// </summary>
public static int CalculateFragmentCount(
    float projectileMass,              // kg
    float shreddingForce,              // N (force that shredded it)
    int baseFragmentCount)             // Minimum fragments (design parameter)
{
    // More mass = more fragments
    float massFactor = projectileMass / 0.05f; // Normalize (0.05kg = 1×)

    // Higher force = more fragments (more violent shredding)
    float forceFactor = math.sqrt(shreddingForce / 100f); // Diminishing returns

    int fragmentCount = (int)(baseFragmentCount * massFactor * forceFactor);

    // Clamp to reasonable range
    return math.clamp(fragmentCount, baseFragmentCount, baseFragmentCount * 5);
}
```

#### Generate Fragment Velocities
```csharp
/// <summary>
/// Generate fragment velocities within spread pattern
/// Agnostic: Randomized within cone, constrained by energy conservation
/// </summary>
public static void GenerateFragmentVelocities(
    NativeArray<float3> fragmentVelocities, // Output array
    float3 parentVelocity,                  // m/s (original projectile velocity)
    float3 spreadDirection,                 // Normalized direction vector
    float spreadAngleDegrees,               // Cone angle (280° typical)
    float velocityMin,                      // m/s
    float velocityMax,                      // m/s
    ref Random random)
{
    int fragmentCount = fragmentVelocities.Length;

    for (int i = 0; i < fragmentCount; i++)
    {
        // Random angle within spread cone
        float randomAngle = random.NextFloat(-spreadAngleDegrees / 2f, spreadAngleDegrees / 2f);
        float randomAngleRad = math.radians(randomAngle);

        // Random rotation around spread axis
        float randomRotation = random.NextFloat(0f, 360f);
        float randomRotationRad = math.radians(randomRotation);

        // Calculate spread direction with rotation
        float3 fragmentDirection = RotateVectorInCone(spreadDirection, randomAngleRad, randomRotationRad);

        // Random velocity magnitude
        float velocityMagnitude = random.NextFloat(velocityMin, velocityMax);

        // Fragment velocity
        fragmentVelocities[i] = fragmentDirection * velocityMagnitude;

        // Add parent velocity component (fragments inherit some parent momentum)
        fragmentVelocities[i] += parentVelocity * 0.3f; // 30% parent velocity inherited
    }
}

/// <summary>
/// Rotate vector within cone
/// </summary>
private static float3 RotateVectorInCone(float3 axis, float angleRad, float rotationRad)
{
    // Rodrigues' rotation formula
    // Find perpendicular vector to axis
    float3 perpendicular = math.abs(axis.y) < 0.9f
        ? new float3(0, 1, 0)
        : new float3(1, 0, 0);

    float3 u = math.normalize(math.cross(axis, perpendicular));
    float3 v = math.cross(axis, u);

    // Rotate around axis
    float3 rotated = axis * math.cos(angleRad)
                   + u * math.sin(angleRad) * math.cos(rotationRad)
                   + v * math.sin(angleRad) * math.sin(rotationRad);

    return math.normalize(rotated);
}
```

#### Calculate Fragment Damage Distribution
```csharp
/// <summary>
/// Calculate damage dealt by each fragment
/// Agnostic: Original damage distributed across fragments
/// </summary>
public static float CalculateFragmentDamage(
    float originalDamage,              // Damage of intact projectile
    int fragmentCount,                 // Number of fragments
    float fragmentEfficiency)          // 0-1 (how much damage preserved, 0.4 typical)
{
    // Total fragment damage = original × efficiency
    float totalFragmentDamage = originalDamage * fragmentEfficiency;

    // Damage per fragment
    float fragmentDamage = totalFragmentDamage / fragmentCount;

    return fragmentDamage;
}
```

### Fragment Explosion Pattern (280° Arc)

#### Calculate 280° Upward Arc Distribution
```csharp
/// <summary>
/// Generate fragment positions within 280° upward arc
/// Agnostic: Cone shape biased upward/forward, not backward/downward
/// </summary>
public static void GenerateFragmentArcPattern(
    NativeArray<float3> fragmentDirections,  // Output array (normalized)
    float3 explosionCenter,                  // Position of explosion
    float3 upwardDirection,                  // "Up" direction (usually +Y)
    float3 forwardDirection,                 // Primary spread direction
    int fragmentCount,
    ref Random random)
{
    // 280° arc: 0° (forward) to 280° (mostly backward excluded)
    // Arc spans: -140° to +140° from forward axis

    for (int i = 0; i < fragmentCount; i++)
    {
        // Random angle within 280° arc (-140° to +140°)
        float horizontalAngle = random.NextFloat(-140f, 140f);
        float horizontalAngleRad = math.radians(horizontalAngle);

        // Vertical angle: Bias upward (0° to 90°, with bias toward 45-60°)
        // Use beta distribution for upward bias
        float verticalAngle = random.NextFloat(0f, 90f);
        verticalAngle = math.pow(verticalAngle / 90f, 0.7f) * 90f; // Bias toward mid-range
        float verticalAngleRad = math.radians(verticalAngle);

        // Calculate direction vector
        float3 direction = CalculateDirectionFromAngles(
            forwardDirection,
            upwardDirection,
            horizontalAngleRad,
            verticalAngleRad
        );

        fragmentDirections[i] = math.normalize(direction);
    }
}

/// <summary>
/// Calculate 3D direction from horizontal and vertical angles
/// </summary>
private static float3 CalculateDirectionFromAngles(
    float3 forward,
    float3 up,
    float horizontalAngleRad,
    float verticalAngleRad)
{
    // Horizontal rotation around up axis
    float3 right = math.normalize(math.cross(up, forward));
    float3 horizontalDir = forward * math.cos(horizontalAngleRad)
                         + right * math.sin(horizontalAngleRad);

    // Vertical rotation
    float3 finalDir = horizontalDir * math.cos(verticalAngleRad)
                    + up * math.sin(verticalAngleRad);

    return math.normalize(finalDir);
}
```

### Active Deflection (Telekinesis, Barriers)

#### Calculate Active Deflection Force
```csharp
/// <summary>
/// Calculate force applied by active deflection (magic, psi)
/// Agnostic: Force based on caster power and concentration
/// </summary>
public static float3 CalculateActiveDeflectionForce(
    float casterPower,                 // 0-100 (INT, PSI, or similar stat)
    float concentrationLevel,          // 0-1 (1 = full focus, 0 = distracted)
    float3 deflectionDirection,        // Desired deflection direction (normalized)
    float distanceToProjectile,        // m
    float maxRange)                    // m (caster's effective range)
{
    // Power determines base force
    float baseForce = casterPower * 10f; // 1000N max at power 100

    // Concentration multiplier
    float concentrationMultiplier = 0.3f + concentrationLevel * 0.7f; // Min 30%, max 100%

    // Distance penalty (inverse square)
    float distanceFactor = 1f - math.pow(distanceToProjectile / maxRange, 2f);
    distanceFactor = math.clamp(distanceFactor, 0f, 1f);

    // Final force magnitude
    float forceMagnitude = baseForce * concentrationMultiplier * distanceFactor;

    // Force vector
    float3 forceVector = deflectionDirection * forceMagnitude;

    return forceVector;
}
```

#### Calculate Barrier Deflection
```csharp
/// <summary>
/// Calculate deflection when projectile enters force barrier
/// Agnostic: Barrier applies radial or directional force
/// </summary>
public static float3 CalculateBarrierDeflection(
    float3 projectilePosition,
    float3 projectileVelocity,
    float3 barrierCenter,
    float barrierRadius,               // m
    float barrierStrength,             // N (force applied)
    bool isRadialBarrier)              // True = pushes outward, False = directional
{
    // Distance from barrier center
    float3 offset = projectilePosition - barrierCenter;
    float distanceFromCenter = math.length(offset);

    // Outside barrier: no force
    if (distanceFromCenter > barrierRadius)
        return float3.zero;

    // Inside barrier: apply force
    float3 forceDirection;

    if (isRadialBarrier)
    {
        // Radial: Push outward from center
        forceDirection = math.normalize(offset);
    }
    else
    {
        // Directional: Push in fixed direction (e.g., upward)
        forceDirection = new float3(0, 1, 0); // Example: always upward
    }

    // Force magnitude (stronger near edge, weaker near center for radial)
    float forceMagnitude = barrierStrength;
    if (isRadialBarrier)
    {
        float edgeFactor = distanceFromCenter / barrierRadius; // 0 at center, 1 at edge
        forceMagnitude *= edgeFactor; // Stronger near edge
    }

    float3 forceVector = forceDirection * forceMagnitude;

    return forceVector;
}
```

#### Calculate Concentration Interruption
```csharp
/// <summary>
/// Calculate concentration loss when caster takes damage
/// Agnostic: Damage reduces concentration, may break barrier
/// </summary>
public static float CalculateConcentrationAfterDamage(
    float currentConcentration,        // 0-1
    float damageTaken,                 // Amount of damage
    int concentrationStat,             // Caster's concentration skill (0-100)
    int concentrationDC)               // Difficulty check (10 + damage typically)
{
    // Concentration check: Skill vs DC
    float skillBonus = concentrationStat / 100f; // 0-1

    // DC increases with damage
    float effectiveDC = concentrationDC + damageTaken;

    // Success probability
    float successChance = skillBonus - (effectiveDC / 100f);
    successChance = math.clamp(successChance, 0f, 0.95f); // Max 95% success

    // If check fails, lose concentration
    if (successChance < 0.5f) // Failed check
    {
        // Lose concentration proportional to damage
        float concentrationLoss = damageTaken / 50f; // 50 damage = 100% loss
        return math.clamp(currentConcentration - concentrationLoss, 0f, 1f);
    }

    // Check succeeded, minor concentration loss only
    return math.clamp(currentConcentration - 0.05f, 0f, 1f); // 5% loss even on success
}
```

### Debuff Application

#### Calculate Bleed Debuff Probability
```csharp
/// <summary>
/// Calculate probability fragment inflicts bleeding debuff
/// Agnostic: Based on fragment type, target armor, impact location
/// </summary>
public static float CalculateBleedProbability(
    float fragmentDamage,              // Damage dealt by fragment
    float targetArmorValue,            // Target's armor rating (0-100)
    bool isJaggedFragment,             // Jagged vs smooth fragment
    bool hitVitalArea)                 // Hit chest/head vs limbs
{
    // Base probability from damage
    float baseProbability = math.clamp(fragmentDamage / 30f, 0f, 0.8f); // Max 80% at 30+ damage

    // Armor reduces bleed chance
    float armorReduction = targetArmorValue / 200f; // Max 50% reduction at armor 100
    baseProbability *= (1f - armorReduction);

    // Jagged fragments more likely to bleed
    if (isJaggedFragment)
        baseProbability *= 1.4f;

    // Vital hits more likely to bleed
    if (hitVitalArea)
        baseProbability *= 1.3f;

    return math.clamp(baseProbability, 0f, 0.95f);
}
```

#### Calculate Embedded Fragment Probability
```csharp
/// <summary>
/// Calculate probability fragment embeds in target
/// Agnostic: Low-velocity fragments more likely to embed
/// </summary>
public static float CalculateEmbedProbability(
    float fragmentVelocity,            // m/s
    float fragmentMass,                // kg
    float targetArmorValue,            // 0-100
    float targetFleshDensity)          // kg/m³ (soft vs hard tissue)
{
    // Low velocity = high embed chance (doesn't exit cleanly)
    float velocityFactor = 1f - math.clamp(fragmentVelocity / 100f, 0f, 0.8f);

    // Heavy fragments more likely to embed (deeper penetration)
    float massFactor = math.clamp(fragmentMass / 0.02f, 0.5f, 1.5f); // Normalize to 20g

    // Armor reduces embed chance (deflects/stops fragments)
    float armorPenalty = targetArmorValue / 150f; // Max 66% reduction

    // Soft tissue easier to embed
    float tissueFactor = 1000f / targetFleshDensity; // Normalize to muscle density

    float embedProbability = velocityFactor * massFactor * tissueFactor * (1f - armorPenalty);

    return math.clamp(embedProbability, 0f, 0.9f);
}
```

#### Calculate Debuff Damage Over Time
```csharp
/// <summary>
/// Calculate damage per second from debuff
/// Agnostic: Based on debuff severity and stacking
/// </summary>
public static float CalculateDebuffDPS(
    DebuffType debuffType,
    int stackCount,                    // Number of stacked debuffs (multiple fragments)
    float debuffSeverity)              // 0-1 (how severe the individual debuff)
{
    float baseDPS = 0f;

    switch (debuffType)
    {
        case DebuffType.Bleed:
            baseDPS = 3f; // 3 damage/second per bleed
            break;
        case DebuffType.Embedded:
            baseDPS = 1f; // 1 damage/second per embedded fragment
            break;
        case DebuffType.Infection:
            baseDPS = 5f; // 5 damage/second (more severe)
            break;
        case DebuffType.OrganDamage:
            baseDPS = 10f; // 10 damage/second (critical)
            break;
    }

    // Severity multiplier
    baseDPS *= debuffSeverity;

    // Stacking (diminishing returns)
    float stackMultiplier = stackCount * 0.7f; // Each additional stack = 70% effectiveness

    float totalDPS = baseDPS * stackMultiplier;

    return totalDPS;
}

public enum DebuffType : byte
{
    None,
    Bleed,
    Embedded,
    Infection,
    OrganDamage
}
```

### Defensive Interception

#### Calculate Defensive Explosion Interception Rate
```csharp
/// <summary>
/// Calculate percentage of incoming projectiles intercepted by defensive explosion
/// Agnostic: Based on explosion coverage, projectile density, timing
/// </summary>
public static float CalculateInterceptionRate(
    float explosionRadius,             // m
    int incomingProjectileCount,       // Number of projectiles in volley
    float projectileSpreadArea,        // m² (area covered by projectile spread)
    float timingAccuracy,              // 0-1 (how well-timed the interception)
    float explosionForce)              // N (determines shredding vs deflection)
{
    // Coverage: What % of projectile spread does explosion cover?
    float explosionArea = math.PI * explosionRadius * explosionRadius;
    float coveragePercent = math.min(explosionArea / projectileSpreadArea, 1f);

    // Timing: Well-timed interceptions more effective
    float timingMultiplier = 0.5f + timingAccuracy * 0.5f; // 50-100% effectiveness

    // Force determines kill vs deflection ratio
    float killRatio = math.clamp(explosionForce / 500f, 0.3f, 0.7f); // 30-70% killed, rest deflected
    float deflectRatio = 1f - killRatio;

    // Total interception rate
    float interceptionRate = coveragePercent * timingMultiplier;

    return math.clamp(interceptionRate, 0f, 0.95f); // Max 95% interception
}
```

#### Calculate Optimal Interception Timing
```csharp
/// <summary>
/// Calculate optimal time to detonate defensive explosive for maximum interception
/// Agnostic: Intercept when projectiles densest in blast radius
/// </summary>
public static float CalculateOptimalInterceptionTime(
    float explosionRadius,             // m
    float projectileVelocity,          // m/s (average of incoming projectiles)
    float distanceToProjectiles,       // m (current distance)
    int projectileCount,               // Number of projectiles
    float projectileSpread)            // m (how spread out projectiles are)
{
    // Optimal interception: When projectiles centered in blast radius
    // Time until projectiles reach blast center
    float timeUntilCenter = distanceToProjectiles / projectileVelocity;

    // Adjust for spread: If projectiles very spread, intercept earlier (catch front edge)
    float spreadAdjustment = projectileSpread / explosionRadius;
    if (spreadAdjustment > 1f)
    {
        // Spread larger than explosion: Intercept earlier to catch more
        timeUntilCenter *= 0.8f;
    }

    return math.max(timeUntilCenter, 0f);
}
```

---

## ECS System Architecture

### Body ECS (60 Hz) - Projectile Physics

**ProjectileDeflectionSystem**:
- Detect environmental forces near projectiles
- Apply force vectors to projectile velocity
- Calculate deflection angles

**ProjectileShreddingSystem**:
- Check shredding probability for projectiles in force fields
- Reduce structural integrity from cumulative damage
- Trigger fragmentation when integrity reaches 0

**FragmentSpawnSystem**:
- Spawn fragment entities when projectile shredded
- Generate fragment velocities within 280° arc
- Apply debuffs to fragments

**BlastWaveSystem**:
- Calculate explosion blast waves
- Apply radial forces to nearby projectiles
- Handle defensive explosion interceptions

**TelekineticForceSystem**:
- Apply magical/psi forces from caster entities
- Calculate barrier deflections
- Update concentration levels

### Mind ECS (1 Hz) - Concentration & Perception

**ConcentrationSystem**:
- Track caster concentration for barrier maintenance
- Handle concentration interruption from damage
- Drain mana/psi resources for sustained effects

**PerceptionSystem**:
- Detect incoming projectiles for active deflection
- Calculate reaction time for casters
- Determine simultaneous target limits

**PredictionSystem**:
- Calculate aim compensation for archers
- Predict explosion timing for skilled entities
- Enable intentional deflection use (curve shots)

---

## Key Design Principles

1. **Momentum Dominates**: Mass × Velocity determines deflection resistance (heavy/fast projectiles resist)
2. **Force Decays with Distance**: Inverse square law for explosions, linear for fields
3. **Shredding Requires Threshold**: Low forces deflect, high forces shred (threshold at ~500N per kg)
4. **Fragments Preserve Energy**: 40% of original damage distributed across fragments
5. **280° Arc Pattern**: Forward/upward bias, excludes backward/downward (ground blocks)
6. **Debuffs Stack**: Multiple fragments = multiple bleeds (cumulative damage over time)
7. **Concentration Is Fragile**: Damage interrupts focus, breaks barriers (DC 10 + damage)
8. **Interception Timing Matters**: Well-timed explosions intercept 70-95%, poor timing 20-40%
9. **Armor Reduces Fragments**: Each fragment hit separately (armor effective against fragmentation)
10. **High Velocity Bypasses**: Fast projectiles pass through forces before significant deflection

---

## Integration with Other Systems

- **Infiltration Detection**: Defensive explosives triggered by detected intruders
- **Crisis Alert States**: High alert = more defensive explosives deployed
- **Blueprint System**: Advanced fragmentation designs (optimized spread patterns)
- **Permanent Augmentation**: Enhanced perception (detect projectiles earlier for deflection)
