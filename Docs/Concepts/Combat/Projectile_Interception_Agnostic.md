# Projectile Interception Framework (Agnostic)

**Status:** Concept Design
**Category:** Core Combat Framework
**Shareability:** `shared-core`
**Last Updated:** 2025-12-07

---

## Overview

The **Projectile Interception Framework** provides agnostic mechanics for projectile-on-projectile interactions, weapon deflections, and multi-target defense. Games implement specific projectile types, interception rules, and visual effects while PureDOTS provides the collision detection, trajectory modification, and focus-based multi-tracking systems.

**Agnostic Aspects** (Provided by PureDOTS):
- ✅ Projectile collision detection framework
- ✅ Interception capability component structure
- ✅ Trajectory modification physics
- ✅ Multi-target tracking system (focus-based)
- ✅ Deflection timing mechanics
- ✅ Threat prioritization algorithms

**Game-Specific Aspects** (Implemented by Games):
- Projectile types (arrows, spells, torpedoes, missiles)
- Interception rules (magic vs anti-magic, kinetic vs shields)
- Counter mechanics (counter-spells, null zones)
- Deflection animations and VFX
- Point defense doctrine (Space4X turrets, drone swarms)

---

## Core Agnostic Components

### ProjectileComponents (Body ECS)
```csharp
/// <summary>
/// Core projectile physics (agnostic)
/// </summary>
public struct ProjectileComponents : IComponentData
{
    public Entity Owner;
    public float3 Velocity;
    public float Damage;
    public float Mass;
    public byte ProjectileTypeId;     // Game-defined enum
    public uint SpawnTick;
    public float LifetimeRemaining;
}
```

### InterceptableProjectile (Body ECS)
```csharp
/// <summary>
/// Agnostic interception target component
/// </summary>
public struct InterceptableProjectile : IComponentData
{
    public bool CanBeIntercepted;
    public float InterceptionHealth;  // Multiple hits to destroy
    public float Size;                // 0.1 (small) to 10.0 (large)
    public float EvasionChance;       // 0-0.5 (homing missiles)
}
```

### InterceptionCapability (Body ECS)
```csharp
/// <summary>
/// Agnostic interception capability
/// Games define type flags
/// </summary>
public struct InterceptionCapability : IComponentData
{
    public bool CanIntercept;
    public ushort AllowedTypesFlags;  // Game-defined bitmask
    public float InterceptionRadius;
    public float InterceptionDamage;
    public bool ExplodesOnIntercept;
    public bool IsAntiType;           // Anti-magic, anti-shield, etc.
}
```

### TrajectoryModifier (Body ECS)
```csharp
/// <summary>
/// Agnostic trajectory modification
/// </summary>
public struct TrajectoryModifier : IComponentData
{
    public float DeflectionAngle;     // Degrees
    public byte RicochetCount;
    public float SpeedModifier;       // 0-1 (speed reduction)
    public uint LastDeflectionTick;
}
```

### MultiTargetTracking (Body ECS)
```csharp
/// <summary>
/// Agnostic multi-target tracking (focus-based)
/// </summary>
public struct MultiTargetTracking : IComponentData
{
    public byte MaxTargets;           // 1-8
    public byte TrackedCount;
    public float FocusCostPerTarget;
    public float PenaltyReduction;    // 0-0.8
}
```

---

## Agnostic Algorithms

### Interception Hit Chance Calculation
```csharp
/// <summary>
/// Calculate hit chance for projectile interception
/// Agnostic: Games provide distance/radius/size values
/// </summary>
public static float CalculateInterceptHitChance(
    float distance,
    float interceptRadius,
    float targetSize)
{
    float distanceFactor = 1f - (distance / interceptRadius);
    float sizeModifier = math.clamp(targetSize, 0.5f, 2.0f);
    return math.clamp(distanceFactor * sizeModifier, 0.1f, 0.95f);
}
```

### Trajectory Deflection Physics
```csharp
/// <summary>
/// Apply deflection angle to projectile velocity
/// Agnostic: Pure physics, no game-specific logic
/// </summary>
public static float3 ApplyDeflection(
    float3 currentVelocity,
    float deflectionAngle,
    float speedModifier)
{
    float3 currentDir = math.normalize(currentVelocity);
    float speed = math.length(currentVelocity);

    float angleRad = math.radians(deflectionAngle);
    quaternion rotation = quaternion.AxisAngle(math.up(), angleRad);
    float3 newDir = math.rotate(rotation, currentDir);

    return newDir * (speed * speedModifier);
}
```

### Multi-Target Threat Sorting
```csharp
/// <summary>
/// Sort threats by priority (damage * proximity)
/// Agnostic: Games populate threat buffer, framework sorts
/// </summary>
public static void SortThreatsByPriority(DynamicBuffer<TrackedThreat> threats)
{
    // Bubble sort (Burst-compatible)
    for (int i = 0; i < threats.Length - 1; i++)
    {
        for (int j = 0; j < threats.Length - i - 1; j++)
        {
            if (threats[j].Priority < threats[j + 1].Priority)
            {
                var temp = threats[j];
                threats[j] = threats[j + 1];
                threats[j + 1] = temp;
            }
        }
    }
}
```

---

## Extension Points for Games

### 1. Projectile Type Definitions
Games define projectile type enums:
```csharp
// Godgame example
public enum GodgameProjectileType : byte
{
    Arrow,
    Bolt,
    ThrownWeapon,
    Fireball,
    IceBolt,
    LightningBolt,
    ArcaneBlast,
    NullSphere,
    CounterSpell,
}

// Space4X example
public enum Space4XProjectileType : byte
{
    LaserBeam,
    PlasmaBolt,
    Torpedo,
    HomingMissile,
    BoardingShuttle,
    DroneKamikaze,
    PointDefenseRound,
}
```

### 2. Interception Type Flags
Games define interception bitmasks:
```csharp
// Godgame example
[Flags]
public enum GodgameInterceptionFlags : ushort
{
    None = 0,
    Physical = 1 << 0,      // Arrows, bolts
    Magic = 1 << 1,         // Spells
    AntiMagic = 1 << 2,     // Anti-magic projectiles
    All = Physical | Magic,
}

// Space4X example
[Flags]
public enum Space4XInterceptionFlags : ushort
{
    None = 0,
    Kinetic = 1 << 0,       // Bullets, projectiles
    Energy = 1 << 1,        // Lasers, plasma
    Homing = 1 << 2,        // Guided missiles
    Heavy = 1 << 3,         // Torpedoes, shuttles
    All = Kinetic | Energy | Homing | Heavy,
}
```

### 3. Counter Mechanics
Games define counter types:
```csharp
// Godgame counter-spell types
public enum CounterType : byte
{
    Nullify,    // Destroys both spells
    Deflect,    // Redirects incoming spell
    Absorb,     // Absorbs spell, gains mana
    Reflect,    // Sends spell back to caster
}

// Space4X countermeasure types
public enum CountermeasureType : byte
{
    Chaff,      // Confuses homing missiles
    Flare,      // Redirects heat-seeking
    ECM,        // Electronic countermeasures
    Shield,     // Energy absorption
}
```

---

## Integration Requirements

### Systems Games Must Implement

1. **ProjectileInterceptionSystem** (Body ECS, 60 Hz)
   - Detect projectile-projectile collisions using spatial queries
   - Apply interception damage to `InterceptionHealth`
   - Destroy/deflect projectiles based on interception results
   - Spawn VFX for interception events

2. **CounterMechanicsSystem** (Body ECS, 60 Hz)
   - Implement game-specific counter logic (counter-spells, null zones)
   - Handle mana/resource costs for counters
   - Apply counter effects (nullify, deflect, absorb, reflect)

3. **DeflectionSystem** (Body ECS, 60 Hz)
   - Weapon-based deflection (timing window checks)
   - Limb state integration (deflection during attack animation)
   - Apply deflection trajectory modifiers

4. **MultiTargetDefenseSystem** (Body ECS, 60 Hz)
   - Populate `TrackedThreat` buffer with game threats
   - Sort threats by priority (damage, proximity, type)
   - Apply focus costs for multi-target tracking
   - Calculate penalty reductions

5. **TrajectoryModificationSystem** (Body ECS, 60 Hz)
   - Apply deflection angles to projectile velocities
   - Handle ricochet mechanics (wall bounces, shield reflections)
   - Update projectile transform based on modified trajectory

---

## Data Contracts

Games must provide:
- Projectile type catalog (type IDs, properties, interception rules)
- Interception type flags (what can intercept what)
- Counter mechanics definitions (counter-spell types, null zones)
- Deflection timing windows (weapon-based)
- Multi-target focus costs per archetype

---

## Game-Specific Implementations

### Godgame (Magic + Physical Projectiles)
**Full Implementation:** [Projectile_Interception_System.md](../../../../Docs/Features/Projectile_Interception_System.md)

**Projectile Types:** Arrows, bolts, fireballs, ice bolts, counter-spells, null spheres
**Counter Mechanics:** Counter-spells (nullify/deflect/absorb/reflect), Null Magic Zones
**Deflection:** Warrior weapon deflection (timing-based, 0.1s-0.3s window)
**Multi-Target:** Master mage tracks 8 spells, master rogue dodges 8 arrows

### Space4X (Energy + Kinetic Projectiles)
**Implementation Reference:** TBD

**Projectile Types:** Laser beams, plasma bolts, torpedoes, homing missiles, boarding shuttles
**Counter Mechanics:** Chaff, flares, ECM, shields
**Deflection:** Point defense turrets (automated), drone kamikaze
**Multi-Target:** Fleet point defense coordination, swarm interception

---

## Performance Targets

**Body ECS (60 Hz) Budget:** 7-10 ms/frame (needs optimization)
- Projectile interception: 3.0 ms (N² checks, optimize with spatial grid)
- Counter mechanics: 1.5 ms (magic filtering)
- Weapon deflection: 1.0 ms (limb state checks)
- Multi-target defense: 1.0 ms (threat sorting)
- Trajectory modification: 0.5 ms (angle application)

**Optimization Strategies:**
- Spatial grid/octree for projectile queries (N log N instead of N²)
- Multi-threading for interception checks
- LOD: Skip distant projectile interception checks
- Buffer pooling for threat tracking

---

## Testing Guidelines

### Unit Tests (PureDOTS)
- ✅ Interception hit chance formula (distance/radius/size inputs)
- ✅ Trajectory deflection physics (angle/speed modification)
- ✅ Multi-target threat sorting (priority-based)
- ✅ Deflection timing window validation

### Integration Tests (Games)
- Projectile-projectile collision detection accuracy (95%+)
- Counter-spell type effectiveness (nullify vs deflect vs absorb)
- Weapon deflection success rate (skill-based, 20-80%)
- Multi-target tracking focus costs (sustainable for 30s at max focus)

---

## Migration Notes

**New Components Required:**
- `InterceptableProjectile` (Body ECS)
- `InterceptionCapability` (Body ECS)
- `TrajectoryModifier` (Body ECS)
- `MultiTargetTracking` (Body ECS)
- `TrackedThreat` buffer (Body ECS)
- `DeflectionAction` (Body ECS, timing-based)

**Integration with Existing Systems:**
- `ProjectileComponents` already exists, extend with interception support
- `EntityFocus` integration for multi-target focus costs
- `Limb` integration for weapon deflection timing
- `ReactionTime` integration for deflection reaction windows

---

## Related Documents

**PureDOTS Agnostic:**
- `Docs/Concepts/Combat/Reaction_System_Agnostic.md` - Reaction framework
- `Docs/Architecture/ThreePillarECS_Architecture.md` - ECS layers (to be created)
- `Runtime/Focus/FocusComponents.cs` - Focus system

**Game Implementations:**
- `Docs/Features/Projectile_Interception_System.md` - Full game-side concept
- `Docs/Features/Reaction_Based_Combat_System.md` - Threat detection integration
- `PureDOTS/Documentation/DesignNotes/LimbBasedActionSystem.md` - Weapon deflection

---

**Last Updated:** 2025-12-07
**Maintainer:** PureDOTS Core Team
**Status:** Awaiting Implementation
