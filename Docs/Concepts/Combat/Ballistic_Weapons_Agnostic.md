# Ballistic Weapons System - Agnostic Framework

## Overview

The **Ballistic Weapons System** provides mathematical algorithms for long-range projectile weapons, from medieval siege engines to interplanetary ballistic missiles. This framework is setting-agnostic and handles trajectory calculation, accuracy modeling, damage distribution, interception mechanics, and strategic effects.

---

## Core Algorithms

### 1. Trajectory Calculation

Calculates ballistic arc from launch to target.

```csharp
public struct TrajectoryResult
{
    public float3 ImpactPoint;
    public float FlightTimeSeconds;
    public float LaunchAngleDegrees;
    public float ApexAltitude;
    public bool CanReachTarget;
}

public static TrajectoryResult CalculateTrajectory(
    float3 launchPosition,
    float3 targetPosition,
    float initialVelocity,
    float gravity,
    float3 windVelocity)
{
    TrajectoryResult result = new TrajectoryResult();

    // Calculate horizontal distance and height difference
    float3 displacement = targetPosition - launchPosition;
    float horizontalDistance = math.length(displacement.xz);
    float heightDifference = displacement.y;

    // Calculate optimal launch angle
    float v2 = initialVelocity * initialVelocity;
    float g = gravity;

    // Discriminant for solvability
    float discriminant = v2 * v2 - g * (g * horizontalDistance * horizontalDistance + 2 * heightDifference * v2);

    if (discriminant < 0)
    {
        // Target out of range
        result.CanReachTarget = false;
        return result;
    }

    // Two possible angles (high arc and low arc)
    float angleLow = math.atan((v2 - math.sqrt(discriminant)) / (g * horizontalDistance));
    float angleHigh = math.atan((v2 + math.sqrt(discriminant)) / (g * horizontalDistance));

    // Prefer low arc (faster, harder to intercept)
    float optimalAngle = angleLow;
    result.LaunchAngleDegrees = math.degrees(optimalAngle);

    // Calculate flight time
    float verticalVelocity = initialVelocity * math.sin(optimalAngle);
    float timeToApex = verticalVelocity / g;
    float apexHeight = launchPosition.y + (verticalVelocity * timeToApex) - (0.5f * g * timeToApex * timeToApex);
    float timeApexToGround = math.sqrt(2 * (apexHeight - targetPosition.y) / g);
    result.FlightTimeSeconds = timeToApex + timeApexToGround;
    result.ApexAltitude = apexHeight;

    // Compensate for wind
    float3 windDisplacement = windVelocity * result.FlightTimeSeconds;
    result.ImpactPoint = targetPosition + windDisplacement;

    result.CanReachTarget = true;
    return result;
}
```

**Formula:**
```
Horizontal Distance: d = √((targetX - launchX)² + (targetZ - launchZ)²)
Height Difference: Δh = targetY - launchY

Launch Angle: θ = arctan((v² - √(v⁴ - g(gd² + 2Δhv²))) / (gd))

Flight Time: t = (v × sin(θ) + √((v × sin(θ))² + 2g × Δh)) / g

Wind Compensation: Impact Point = Target + (Wind Velocity × Flight Time)
```

### 2. Accuracy and Deviation

Models random deviation from aim point.

```csharp
public static float3 CalculateImpactDeviation(
    float3 aimPoint,
    float baseAccuracyRadius,  // CEP at optimal range
    float currentRange,
    float optimalRange,
    float crewSkillModifier,   // 0.5 (novice) to 1.5 (elite)
    int techLevel,
    float3 environmentalFactors, // x = wind, y = rain, z = visibility
    ref Unity.Mathematics.Random random)
{
    // Range penalty (accuracy degrades beyond optimal range)
    float rangeFactor = 1f;
    if (currentRange > optimalRange)
    {
        float excessRange = currentRange - optimalRange;
        rangeFactor = 1f + (excessRange / optimalRange) * 0.5f; // +50% deviation per range unit beyond optimal
    }

    // Tech level bonus (better guidance systems)
    float techBonus = 1f - (techLevel * 0.05f); // -5% deviation per tech level
    techBonus = math.max(0.2f, techBonus); // Min 20% of base deviation

    // Environmental penalties
    float windPenalty = 1f + (environmentalFactors.x * 0.3f); // +30% per unit wind
    float rainPenalty = 1f + (environmentalFactors.y * 0.2f); // +20% per unit rain
    float visibilityPenalty = 1f + (environmentalFactors.z * 0.4f); // +40% per unit fog/night

    // Total deviation radius (CEP)
    float totalCEP = baseAccuracyRadius * rangeFactor * techBonus * crewSkillModifier *
                     windPenalty * rainPenalty * visibilityPenalty;

    // Rayleigh distribution (models 2D dispersion)
    float r = totalCEP * math.sqrt(-2f * math.log(random.NextFloat(0.01f, 1f)));
    float theta = random.NextFloat(0f, 2f * math.PI);

    float deviationX = r * math.cos(theta);
    float deviationZ = r * math.sin(theta);

    float3 impactPoint = aimPoint + new float3(deviationX, 0f, deviationZ);
    return impactPoint;
}

public static float CalculateHitProbability(
    float targetRadius,
    float cepRadius)
{
    // Probability that projectile hits within circular target
    // Using Rayleigh distribution CDF
    float ratio = targetRadius / cepRadius;
    float hitProbability = 1f - math.exp(-0.5f * ratio * ratio);
    return math.clamp(hitProbability, 0f, 1f);
}
```

**Formulas:**
```
Total CEP = Base CEP × Range Factor × Tech Bonus × Crew Skill × Wind Penalty × Rain Penalty × Visibility Penalty

Range Factor = 1 + ((Current Range - Optimal Range) / Optimal Range) × 0.5
Tech Bonus = 1 - (Tech Level × 0.05), min 0.2
Crew Skill: Novice 0.5, Trained 0.8, Veteran 1.0, Elite 1.3

Rayleigh Distribution (2D deviation):
r = CEP × √(-2 × ln(U)), where U ~ Uniform(0.01, 1)
θ ~ Uniform(0, 2π)

Deviation: (x, z) = (r × cos(θ), r × sin(θ))

Hit Probability = 1 - e^(-0.5 × (Target Radius / CEP)²)
```

**Example:**
```csharp
// Medieval trebuchet
float baseAccuracy = 30f; // ±30m CEP at optimal range
float currentRange = 450f;
float optimalRange = 300f;
float crewSkill = 1.2f; // Veteran crew
int techLevel = 5;
float3 environment = new float3(0.5f, 0f, 0.2f); // Moderate wind, no rain, slight fog

Unity.Mathematics.Random rng = new Unity.Mathematics.Random(12345);
float3 aimPoint = new float3(100f, 0f, 100f);

float3 impactPoint = CalculateImpactDeviation(
    aimPoint, baseAccuracy, currentRange, optimalRange,
    crewSkill, techLevel, environment, ref rng);

// Calculate hit probability on 10m radius gate
float gateProbability = CalculateHitProbability(10f, 30f);
// Result: ~8.6% chance of direct hit on gate per shot
```

### 3. Damage Distribution

Calculates damage falloff from blast center.

```csharp
public struct BlastDamageResult
{
    public float DirectDamage;           // Damage at epicenter
    public float EffectiveRadius;        // Radius where damage > 0
    public float LethalRadius;           // Radius where damage kills (>80% HP)
    public int EstimatedCasualties;      // Based on population density
}

public static BlastDamageResult CalculateBlastDamage(
    float3 impactPoint,
    float maxDamage,
    float blastRadius,
    float populationDensityPerKm2,
    DamageType damageType)
{
    BlastDamageResult result = new BlastDamageResult();

    result.DirectDamage = maxDamage;
    result.EffectiveRadius = blastRadius;

    // Damage falloff (inverse square law for explosions, linear for magical blasts)
    bool isExplosive = damageType == DamageType.Explosive || damageType == DamageType.Kinetic;
    float lethalDamageThreshold = maxDamage * 0.8f;

    if (isExplosive)
    {
        // Inverse square falloff
        // Damage at distance r: D(r) = D_max × (R / r)²
        // Lethal radius where D(r) = lethal threshold
        result.LethalRadius = blastRadius * math.sqrt(maxDamage / lethalDamageThreshold);
    }
    else
    {
        // Linear falloff (magical/energy weapons)
        // Damage at distance r: D(r) = D_max × (1 - r / R)
        result.LethalRadius = blastRadius * (1f - lethalDamageThreshold / maxDamage);
    }

    // Estimate casualties
    float lethalAreaKm2 = (math.PI * result.LethalRadius * result.LethalRadius) / 1_000_000f; // m² to km²
    float effectiveAreaKm2 = (math.PI * result.EffectiveRadius * result.EffectiveRadius) / 1_000_000f;

    int lethalCasualties = (int)(lethalAreaKm2 * populationDensityPerKm2 * 0.9f); // 90% kill rate in lethal zone
    int injuredCasualties = (int)((effectiveAreaKm2 - lethalAreaKm2) * populationDensityPerKm2 * 0.6f); // 60% injury rate in effective zone

    result.EstimatedCasualties = lethalCasualties + (injuredCasualties / 2); // Count injured as 0.5 casualties

    return result;
}

public static float CalculateDamageAtDistance(
    float maxDamage,
    float blastRadius,
    float distance,
    DamageType damageType)
{
    if (distance >= blastRadius) return 0f;

    bool isExplosive = damageType == DamageType.Explosive || damageType == DamageType.Kinetic;

    if (isExplosive)
    {
        // Inverse square falloff
        float ratio = blastRadius / math.max(0.1f, distance);
        return maxDamage * ratio * ratio;
    }
    else
    {
        // Linear falloff
        return maxDamage * (1f - distance / blastRadius);
    }
}

public enum DamageType : byte
{
    Kinetic,    // Boulders, shells
    Explosive,  // Gunpowder, fuel-air
    Fire,       // Incendiary
    Magical,    // Fireball, lightning
    Radiation,  // Nuclear, antimatter
    Exotic      // Reality-warping
}
```

**Formulas:**
```
Explosive Damage Falloff (Inverse Square):
D(r) = D_max × (R / r)²
Lethal Radius: R_lethal = R × √(D_max / D_lethal_threshold)

Linear Damage Falloff (Magical):
D(r) = D_max × (1 - r / R)
Lethal Radius: R_lethal = R × (1 - D_lethal_threshold / D_max)

Casualty Estimation:
Lethal Area (km²) = π × R_lethal² / 1,000,000
Effective Area (km²) = π × R_effective² / 1,000,000

Casualties:
- Lethal Zone: 90% of population killed
- Effective Zone: 60% of population injured
- Total Casualties = Lethal + (Injured × 0.5)
```

**Example:**
```csharp
// Meteor swarm impact (magical bombardment)
float3 impactPoint = new float3(500f, 0f, 500f);
float maxDamage = 2240f; // 4 meteors × 560 avg damage
float blastRadius = 100f;
float populationDensity = 50000f; // 50,000 per km² (dense city)

BlastDamageResult result = CalculateBlastDamage(
    impactPoint, maxDamage, blastRadius, populationDensity, DamageType.Magical);

// Results:
// Lethal Radius: ~64m (1 - 0.8) × 100 = 20m for linear... recalculate
// Actually for D_lethal = 0.8 × 2240 = 1792:
// R_lethal = 100 × (1 - 1792/2240) = 100 × 0.2 = 20m
// Lethal Area: π × 20² / 1,000,000 = 0.00126 km²
// Casualties in lethal zone: 0.00126 × 50,000 × 0.9 = 56 dead
// Effective Area: π × 100² / 1,000,000 = 0.0314 km²
// Casualties in effective zone: (0.0314 - 0.00126) × 50,000 × 0.6 = 902 injured
// Total: 56 + 451 = 507 casualties (this is per meteor, 4 meteors = ~2,000 casualties)
```

### 4. Tech Level Scaling

Models improvements with advancing technology.

```csharp
public struct TechScaling
{
    public float AccuracyImprovement;    // CEP reduction (1.0 = no change, 0.2 = 80% better)
    public float RangeExtension;         // Range multiplier (1.0 = no change, 2.0 = double)
    public float PayloadEfficiency;      // Damage per kg (1.0 = baseline, 3.0 = triple)
    public float InterceptDifficulty;    // Point defense success penalty (0 = easy, 1.0 = impossible)
    public float LaunchTimeReduction;    // Reload/launch time multiplier
}

public static TechScaling CalculateTechScaling(int techLevel)
{
    TechScaling scaling = new TechScaling();

    // Accuracy: Improves 10% per tech level, min 20% of baseline
    scaling.AccuracyImprovement = math.max(0.2f, 1f - (techLevel * 0.1f));

    // Range: +15% per tech level
    scaling.RangeExtension = 1f + (techLevel * 0.15f);

    // Payload efficiency: +20% per tech level (better explosives, guidance)
    scaling.PayloadEfficiency = 1f + (techLevel * 0.2f);

    // Intercept difficulty: +8% per tech level (terminal maneuvering, stealth)
    scaling.InterceptDifficulty = math.min(1f, techLevel * 0.08f);

    // Launch time: -12% per tech level (automation, better logistics)
    scaling.LaunchTimeReduction = math.max(0.1f, 1f - (techLevel * 0.12f));

    return scaling;
}
```

**Tech Level Progression Table:**
```
TL  | Accuracy  | Range  | Payload Eff | Intercept Diff | Launch Time
----|-----------|--------|-------------|----------------|------------
3   | 70% (0.7) | 145%   | 160%        | 24%           | 64%
5   | 50% (0.5) | 175%   | 200%        | 40%           | 40%
7   | 30% (0.3) | 205%   | 240%        | 56%           | 16%
9   | 20% (0.2) | 235%   | 280%        | 72%           | 10% (min)
11  | 20% (min) | 265%   | 320%        | 88%           | 10% (min)
13  | 20% (min) | 295%   | 360%        | 100% (cap)    | 10% (min)
```

**Example:**
```csharp
// Compare TL 5 vs TL 9 ballistic missiles

// TL 5 missile
int tl5 = 5;
TechScaling scale5 = CalculateTechScaling(tl5);
float baseCEP = 50f; // 50m baseline
float tl5CEP = baseCEP * scale5.AccuracyImprovement; // 50 × 0.5 = 25m
float baseRange = 1000f; // 1,000 km baseline
float tl5Range = baseRange * scale5.RangeExtension; // 1,000 × 1.75 = 1,750 km
float baseDamage = 500f;
float tl5Damage = baseDamage * scale5.PayloadEfficiency; // 500 × 2.0 = 1,000 damage

// TL 9 missile
int tl9 = 9;
TechScaling scale9 = CalculateTechScaling(tl9);
float tl9CEP = baseCEP * scale9.AccuracyImprovement; // 50 × 0.2 = 10m (pinpoint)
float tl9Range = baseRange * scale9.RangeExtension; // 1,000 × 2.35 = 2,350 km
float tl9Damage = baseDamage * scale9.PayloadEfficiency; // 500 × 2.8 = 1,400 damage

// TL 9 is significantly superior:
// - 60% better accuracy (25m → 10m CEP)
// - 34% longer range (1,750 → 2,350 km)
// - 40% more damage (1,000 → 1,400)
// - 72% harder to intercept (vs 40%)
```

### 5. Interception Mechanics

Calculates success probability of defensive systems.

```csharp
public struct InterceptResult
{
    public bool InterceptSuccessful;
    public float InterceptProbability;
    public int InterceptorsExpended;
    public float RemainingThreat;       // 0.0 = all destroyed, 1.0 = all survived
}

public static InterceptResult CalculateInterception(
    int incomingMissiles,
    int availableInterceptors,
    float singleInterceptProbability,
    int defenseTechLevel,
    int offenseTechLevel,
    ref Unity.Mathematics.Random random)
{
    InterceptResult result = new InterceptResult();

    // Tech level disparity affects interception
    int techGap = defenseTechLevel - offenseTechLevel;
    float techModifier = 1f + (techGap * 0.15f); // ±15% per TL difference
    techModifier = math.clamp(techModifier, 0.3f, 2.0f); // Cap at 30%-200%

    float adjustedProbability = singleInterceptProbability * techModifier;
    adjustedProbability = math.clamp(adjustedProbability, 0.05f, 0.95f);

    // Simulate interception attempts
    int missilesRemaining = incomingMissiles;
    int interceptorsUsed = 0;

    for (int i = 0; i < incomingMissiles && interceptorsUsed < availableInterceptors; i++)
    {
        // Typically assign 2 interceptors per missile (shoot-shoot doctrine)
        int interceptorsAssigned = math.min(2, availableInterceptors - interceptorsUsed);

        for (int j = 0; j < interceptorsAssigned; j++)
        {
            interceptorsUsed++;
            float roll = random.NextFloat(0f, 1f);

            if (roll <= adjustedProbability)
            {
                // Successful intercept
                missilesRemaining--;
                break; // Move to next missile
            }
        }
    }

    result.InterceptorsExpended = interceptorsUsed;
    result.RemainingThreat = (float)missilesRemaining / incomingMissiles;
    result.InterceptSuccessful = missilesRemaining < incomingMissiles;
    result.InterceptProbability = adjustedProbability;

    return result;
}

public static float CalculateInterceptWindow(
    float missileFlightTime,
    float interceptorLaunchDelay,
    float interceptorSpeed,
    float missileSpeed,
    float detectionRange)
{
    // Time from detection to impact
    float timeToImpact = missileFlightTime;

    // Time for interceptor to launch and reach intercept point
    float interceptorFlightTime = detectionRange / (interceptorSpeed + missileSpeed); // Closing speed

    // Window = Time to Impact - Launch Delay - Interceptor Flight Time
    float interceptWindow = timeToImpact - interceptorLaunchDelay - interceptorFlightTime;

    return math.max(0f, interceptWindow); // Cannot be negative
}
```

**Formulas:**
```
Tech Modifier = 1 + (Defense TL - Offense TL) × 0.15
Adjusted Probability = Single Intercept Probability × Tech Modifier
Clamped: [0.05, 0.95]

Shoot-Shoot Doctrine:
- Assign 2 interceptors per missile
- Each interceptor: independent roll vs Adjusted Probability
- If either succeeds, missile destroyed

Intercept Window:
Window = Time to Impact - Launch Delay - Interceptor Flight Time
Interceptor Flight Time = Detection Range / (Interceptor Speed + Missile Speed)

Remaining Threat = Missiles Remaining / Missiles Launched
```

**Example:**
```csharp
// IPBM attack scenario
int incomingIPBMs = 10;
int defenseInterceptors = 15;
float baseInterceptProb = 0.70f; // 70% per interceptor
int defenseTL = 8;
int offenseTL = 7;

Unity.Mathematics.Random rng = new Unity.Mathematics.Random(54321);

InterceptResult result = CalculateInterception(
    incomingIPBMs, defenseInterceptors, baseInterceptProb,
    defenseTL, offenseTL, ref rng);

// Tech modifier: 1 + (8 - 7) × 0.15 = 1.15 (15% bonus)
// Adjusted probability: 0.70 × 1.15 = 0.805 (80.5% per interceptor)

// Simulation:
// Missile 1: Interceptor 1 (roll 0.65) SUCCESS, 1 used
// Missile 2: Interceptor 2 (roll 0.88) FAIL, Interceptor 3 (roll 0.42) SUCCESS, 2 used
// Missile 3: Interceptor 4 (roll 0.21) SUCCESS, 1 used
// ...continues...
// Result: 8 missiles destroyed, 2 penetrate defenses
// Interceptors used: 14
// Remaining threat: 0.2 (20% of attack got through)
```

### 6. Strategic Effects

Models long-term consequences of bombardment.

```csharp
public struct StrategicImpact
{
    public float MoraleReduction;        // -10% to -100%
    public float EconomicDamagePercent;  // % of GDP lost
    public float SurrenderProbability;   // +0% to +90%
    public float RefugeePercent;         // % of population displaced
    public float ReconstructionYears;    // Time to rebuild
    public float ReconstructionCostGold; // Economic cost
}

public static StrategicImpact CalculateStrategicImpact(
    int populationKilled,
    int populationTotal,
    int buildingsDestroyed,
    int buildingsTotalValue,
    BombardmentType bombardmentType,
    float factionMorale,
    float factionWarWeariness)
{
    StrategicImpact impact = new StrategicImpact();

    // Casualty rate affects morale
    float casualtyRate = (float)populationKilled / populationTotal;
    impact.MoraleReduction = casualtyRate * 80f; // Up to -80% for 100% casualties

    // Bombardment type amplifies morale loss
    float typeMultiplier = bombardmentType switch
    {
        BombardmentType.Tactical => 0.5f,       // Military targets, less morale impact
        BombardmentType.Strategic => 1.0f,      // Civilian infrastructure, full impact
        BombardmentType.Annihilation => 1.5f,   // Total destruction, +50% morale loss
        BombardmentType.Extinction => 2.0f,     // Permanent blight, double morale loss
        _ => 1.0f
    };

    impact.MoraleReduction *= typeMultiplier;
    impact.MoraleReduction = math.min(100f, impact.MoraleReduction);

    // Economic damage
    impact.EconomicDamagePercent = ((float)buildingsDestroyed / buildingsTotalValue) * 100f;

    // Surrender probability (higher if morale low and war weariness high)
    float moraleModifier = impact.MoraleReduction / 100f;
    float wearinessModifier = factionWarWeariness / 100f;
    impact.SurrenderProbability = (moraleModifier * 0.6f + wearinessModifier * 0.4f) * 100f;
    impact.SurrenderProbability = math.clamp(impact.SurrenderProbability, 0f, 90f);

    // Refugee displacement
    impact.RefugeePercent = casualtyRate * 0.8f + (impact.MoraleReduction / 100f) * 0.3f;
    impact.RefugeePercent = math.min(100f, impact.RefugeePercent * 100f);

    // Reconstruction time
    float destructionSeverity = casualtyRate + (impact.EconomicDamagePercent / 100f);
    impact.ReconstructionYears = destructionSeverity * 5f; // Up to 10 years for total destruction
    if (bombardmentType == BombardmentType.Extinction)
    {
        impact.ReconstructionYears = 999f; // Permanent loss
    }

    // Reconstruction cost
    float avgBuildingValue = buildingsTotalValue / (buildingsDestroyed + 1f);
    impact.ReconstructionCostGold = buildingsDestroyed * avgBuildingValue * 1.5f; // +50% markup for rebuilding

    return impact;
}

public static float CalculateMADThreshold(
    int faction1Missiles,
    int faction2Missiles,
    float faction1InterceptRate,
    float faction2InterceptRate)
{
    // Calculate expected survivors after mutual strike
    float faction1Survivors = faction1Missiles * (1f - faction2InterceptRate);
    float faction2Survivors = faction2Missiles * (1f - faction1InterceptRate);

    // MAD exists if both can inflict unacceptable damage (>50% of missiles penetrate)
    bool faction1CanDestroy = faction1Survivors > faction2Missiles * 0.5f;
    bool faction2CanDestroy = faction2Survivors > faction1Missiles * 0.5f;

    if (faction1CanDestroy && faction2CanDestroy)
    {
        // MAD deterrence active
        return 1.0f; // Full deterrence
    }
    else if (faction1CanDestroy || faction2CanDestroy)
    {
        // Unstable (one side has first-strike advantage)
        return 0.5f; // Partial deterrence
    }
    else
    {
        // Neither can inflict unacceptable damage
        return 0.0f; // No deterrence
    }
}
```

**Formulas:**
```
Casualty Rate = Population Killed / Population Total

Morale Reduction:
Base = Casualty Rate × 80%
Type Multiplier: Tactical 0.5×, Strategic 1.0×, Annihilation 1.5×, Extinction 2.0×
Total = Base × Type Multiplier (max 100%)

Economic Damage = (Buildings Destroyed / Buildings Total) × 100%

Surrender Probability = (Morale Reduction × 0.6 + War Weariness × 0.4)
Clamped: [0%, 90%]

Refugee Displacement = (Casualty Rate × 0.8 + Morale Reduction × 0.3) × 100%

Reconstruction Time = (Casualty Rate + Economic Damage) × 5 years
Extinction: 999 years (permanent)

Reconstruction Cost = Buildings Destroyed × Avg Building Value × 1.5

MAD Deterrence:
- Faction 1 Survivors = Faction 1 Missiles × (1 - Faction 2 Intercept Rate)
- Faction 2 Survivors = Faction 2 Missiles × (1 - Faction 1 Intercept Rate)
- MAD if both factions' survivors > 50% of opponent's missiles
```

---

## Complete Workflow Example

### Interplanetary Ballistic Missile Strike

```csharp
// Scenario: TL 8 colony launches IPBM at TL 7 enemy colony (same star system)

// Step 1: Trajectory Calculation
float3 launchPos = new float3(0, 0, 0); // Launching colony (planet)
float3 targetPos = new float3(150_000_000, 0, 0); // Target colony (150 million km away)
float initialVelocity = 15_000f; // 15 km/s
float gravity = 0f; // Space (no gravity), actually need orbital mechanics here

// For space IPBMs, use transfer orbit calculation (simplified)
float transferTime = 1_800_000f; // ~21 days (Hohmann transfer)
TrajectoryResult trajectory = new TrajectoryResult
{
    FlightTimeSeconds = transferTime,
    CanReachTarget = true,
    ImpactPoint = targetPos
};

// Step 2: Tech Scaling
int attackerTL = 8;
int defenderTL = 7;
TechScaling attackerScaling = CalculateTechScaling(attackerTL);
TechScaling defenderScaling = CalculateTechScaling(defenderTL);

// Step 3: Accuracy
float baseCEP = 100f; // 100m baseline CEP
float adjustedCEP = baseCEP * attackerScaling.AccuracyImprovement; // 100 × 0.3 = 30m
Unity.Mathematics.Random rng = new Unity.Mathematics.Random(99999);

float3 aimPoint = targetPos;
float3 impactPoint = CalculateImpactDeviation(
    aimPoint, adjustedCEP, 150_000_000f, 100_000_000f, // Beyond optimal range
    1.0f, attackerTL, new float3(0, 0, 0), ref rng);

// Step 4: Interception Attempt
int incomingIPBMs = 5; // Attacker launches 5 missiles
int defenderInterceptors = 8;
float interceptProb = 0.65f; // 65% base

InterceptResult interception = CalculateInterception(
    incomingIPBMs, defenderInterceptors, interceptProb,
    defenderTL, attackerTL, ref rng);

// Result: 3 missiles destroyed, 2 penetrate defenses

// Step 5: Damage Calculation (per missile that hits)
int penetratingMissiles = (int)(incomingIPBMs * interception.RemainingThreat); // 2 missiles
float missilePayload = 5000f; // 5,000 damage per missile
float scaledDamage = missilePayload * attackerScaling.PayloadEfficiency; // 5,000 × 2.4 = 12,000 damage
float blastRadius = 2000f; // 2 km blast radius
float populationDensity = 25000f; // 25,000 per km²

BlastDamageResult blastResult = CalculateBlastDamage(
    impactPoint, scaledDamage, blastRadius, populationDensity, DamageType.Explosive);

// Per missile: ~3,200 casualties
// Total: 2 missiles × 3,200 = 6,400 casualties

// Step 6: Strategic Impact
int totalCasualties = penetratingMissiles * blastResult.EstimatedCasualties;
int colonyPopulation = 500_000;
int buildingsDestroyed = 800; // Estimated
int buildingsTotal = 10_000;
float warWeariness = 60f; // Colony is tired of war

StrategicImpact impact = CalculateStrategicImpact(
    totalCasualties, colonyPopulation, buildingsDestroyed, buildingsTotal,
    BombardmentType.Strategic, 70f, warWeariness);

// Results:
// Morale Reduction: -1.28% (6,400 / 500,000 × 80) = minimal (small casualties)
// Economic Damage: 8% (800 / 10,000)
// Surrender Probability: ~25% (low morale impact + high war weariness)
// Refugees: ~1.5%
// Reconstruction: ~0.5 years, ~1.2 million gold

Debug.Log($"IPBM Strike Results:");
Debug.Log($"- Missiles Launched: {incomingIPBMs}");
Debug.Log($"- Intercepted: {incomingIPBMs - penetratingMissiles}");
Debug.Log($"- Impacted: {penetratingMissiles}");
Debug.Log($"- Casualties: {totalCasualties}");
Debug.Log($"- Morale: -{impact.MoraleReduction}%");
Debug.Log($"- Surrender Probability: {impact.SurrenderProbability}%");
Debug.Log($"- Reconstruction: {impact.ReconstructionYears} years, {impact.ReconstructionCostGold} gold");
```

---

## Integration with Three Pillar ECS

### Body Pillar (60 Hz)

High-frequency projectile simulation:

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct BallisticProjectileSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (trajectory, transform, projectile)
                 in SystemAPI.Query<RefRO<ProjectileTrajectory>,
                                     RefRW<LocalTransform>,
                                     RefRW<BallisticProjectile>>())
        {
            // Update projectile position along arc
            projectile.ValueRW.CurrentFlightTime += deltaTime;

            float t = projectile.ValueRO.CurrentFlightTime / trajectory.ValueRO.FlightTimeSeconds;

            if (t >= 1f)
            {
                // Impact!
                // Trigger damage calculation, destroy projectile entity
                continue;
            }

            // Calculate current position on ballistic arc
            float3 start = trajectory.ValueRO.StartPosition;
            float3 end = trajectory.ValueRO.TargetPosition;
            float3 currentPos = math.lerp(start, end, t);

            // Add vertical arc (parabola)
            float apexHeight = trajectory.ValueRO.ApexHeight;
            float verticalOffset = 4f * apexHeight * t * (1f - t); // Parabolic arc
            currentPos.y += verticalOffset;

            transform.ValueRW.Position = currentPos;
        }
    }
}
```

### Mind Pillar (1 Hz)

Medium-frequency strategic decisions:

```csharp
[UpdateInGroup(typeof(MindPillarSystemGroup))]
public partial struct BombardmentDecisionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Evaluate whether to authorize bombardment strikes
        // Check authorization levels (tactical always ok, strategic requires approval)
        // Calculate expected casualties vs military value
        // MAD deterrence checks
    }
}
```

### Aggregate Pillar (0.2 Hz)

Low-frequency long-term effects:

```csharp
[UpdateInGroup(typeof(AggregatePillarSystemGroup))]
public partial struct StrategicImpactSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Apply morale reductions
        // Track war weariness accumulation
        // Calculate surrender probability
        // Process reconstruction (years-long timescale)
        // Refugee displacement
    }
}
```

---

## Summary

The **Ballistic Weapons Agnostic Framework** provides:

1. **Trajectory Calculation**: Ballistic arcs, flight time, apex altitude, wind compensation
2. **Accuracy Modeling**: CEP calculation, tech/skill/environment modifiers, hit probability
3. **Damage Distribution**: Blast radius, inverse square/linear falloff, casualty estimation
4. **Tech Level Scaling**: Accuracy improvement (10%/TL), range extension (15%/TL), payload efficiency (20%/TL)
5. **Interception Mechanics**: Intercept probability, tech disparity, shoot-shoot doctrine, intercept windows
6. **Strategic Effects**: Morale reduction, economic damage, surrender probability, MAD deterrence

**Core Formulas:**
```
Trajectory: θ = arctan((v² - √(v⁴ - g(gd² + 2Δhv²))) / (gd))
Accuracy: CEP_total = CEP_base × Range Factor × Tech × Skill × Environment
Hit Probability: P = 1 - e^(-0.5 × (R_target / CEP)²)
Blast Damage: D(r) = D_max × (R / r)² (explosive) or D_max × (1 - r/R) (linear)
Tech Scaling: Accuracy = 1 - TL × 0.1, Range = 1 + TL × 0.15, Payload = 1 + TL × 0.2
Interception: P_adjusted = P_base × (1 + ΔTL × 0.15)
Strategic: Surrender = (Morale × 0.6 + War Weariness × 0.4)
```

This framework is fully compatible with Unity DOTS and the Three Pillar ECS architecture, with systems distributed across Body (60 Hz projectile physics), Mind (1 Hz authorization decisions), and Aggregate (0.2 Hz long-term strategic effects).
