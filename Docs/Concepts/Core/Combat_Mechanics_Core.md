# Core Combat Mechanics

## Overview

Fundamental combat mechanics that apply across all combat contexts (bay combat, ground combat, aerial combat, naval combat). Defines damage, accuracy, knockback, stability, and disruption systems.

**Key Principles**:
- **Cross-context**: Same mechanics for ships, mechs, villagers, creatures
- **Disruption-based accuracy**: Damage and knockback reduce accuracy
- **Stability offsets**: Physical strength and focus mitigate disruption
- **Deterministic**: Same inputs produce same outputs
- **Burst-optimized**: All calculations Burst-compatible

---

## Accuracy and Disruption

### Base Accuracy

Every combat entity has a base accuracy rating:

```csharp
public struct CombatStats : IComponentData
{
    public float BaseAccuracy;          // 0.0 to 1.0 (0% to 100% hit chance)
    public float BaseDamage;
    public float BaseAttackSpeed;       // Attacks per second
    public float CriticalChance;        // 0.0 to 1.0
    public float CriticalMultiplier;    // 1.5x to 3.0x damage
}
```

### Accuracy Disruption

**Accuracy is reduced by damage taken and knockback experienced**:

```csharp
public struct AccuracyDisruption : IComponentData
{
    // Disruption sources
    public float DamageDisruption;      // Accumulated damage disruption
    public float KnockbackDisruption;   // Accumulated knockback disruption
    public float TotalDisruption;       // Sum of all disruptions

    // Disruption decay
    public float DisruptionDecayRate;   // Per second recovery
    public uint LastDisruptionTick;

    // Current effective accuracy
    public float EffectiveAccuracy;     // BaseAccuracy - TotalDisruption
}

public struct StabilityOffsets : IComponentData
{
    // Physical attributes
    public float PhysicalStrength;      // 0.0 to 1.0 (body strength)
    public float Mass;                  // Heavier = more stable

    // Mental attributes
    public float FocusPool;             // Available focus
    public float FocusUsageRate;        // Focus spent per second for stability

    // Calculated stability
    public float StabilityRating;       // Combined resistance to disruption
    public float DisruptionDampening;   // % disruption negated
}
```

### Disruption Calculation

When entity takes damage or knockback:

```csharp
[BurstCompile]
public partial struct AccuracyDisruptionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (disruption, stability, combatStats) in SystemAPI.Query<
            RefRW<AccuracyDisruption>,
            RefRO<StabilityOffsets>,
            RefRO<CombatStats>>())
        {
            // Decay existing disruption over time
            disruption.ValueRW.DamageDisruption = math.max(0f,
                disruption.ValueRO.DamageDisruption - (disruption.ValueRO.DisruptionDecayRate * deltaTime));

            disruption.ValueRW.KnockbackDisruption = math.max(0f,
                disruption.ValueRO.KnockbackDisruption - (disruption.ValueRO.DisruptionDecayRate * deltaTime));

            // Calculate total disruption
            float rawDisruption = disruption.ValueRO.DamageDisruption + disruption.ValueRO.KnockbackDisruption;

            // Apply stability dampening
            // Formula: Disruption * (1 - Dampening)
            float dampenedDisruption = rawDisruption * (1f - stability.ValueRO.DisruptionDampening);

            disruption.ValueRW.TotalDisruption = dampenedDisruption;

            // Calculate effective accuracy
            disruption.ValueRW.EffectiveAccuracy = math.max(0f,
                combatStats.ValueRO.BaseAccuracy - disruption.ValueRO.TotalDisruption);
        }
    }
}
```

### Adding Disruption on Damage

When entity takes damage:

```csharp
public static void ApplyDamageDisruption(
    ref AccuracyDisruption disruption,
    in StabilityOffsets stability,
    float damageAmount)
{
    // Base disruption from damage
    // Formula: Damage * 0.01 (100 damage = 0.1 disruption = -10% accuracy)
    float baseDisruption = damageAmount * 0.01f;

    // Physical strength offset
    // Formula: Disruption * (1 - PhysicalStrength)
    // Example: 0.8 strength reduces disruption by 80%
    float strengthOffset = baseDisruption * (1f - stability.PhysicalStrength);

    // Focus offset (spending focus to maintain composure)
    float focusOffset = 0f;
    if (stability.FocusPool > 0f && stability.FocusUsageRate > 0f)
    {
        // Using focus reduces disruption further
        // Example: 0.5 focus usage rate = 50% additional reduction
        focusOffset = strengthOffset * stability.FocusUsageRate;
    }

    // Final disruption after offsets
    float finalDisruption = strengthOffset - focusOffset;

    disruption.DamageDisruption += math.max(0f, finalDisruption);
}
```

### Adding Disruption on Knockback

When entity experiences knockback:

```csharp
public static void ApplyKnockbackDisruption(
    ref AccuracyDisruption disruption,
    in StabilityOffsets stability,
    float knockbackMagnitude,
    float3 knockbackVector)
{
    // Base disruption from knockback
    // Formula: Magnitude * 0.02 (50 magnitude = 1.0 disruption = -100% accuracy)
    float baseDisruption = knockbackMagnitude * 0.02f;

    // Mass offset (heavier entities resist knockback better)
    // Formula: Disruption / (1 + Mass)
    // Example: Mass 10 = 10× resistance
    float massOffset = baseDisruption / (1f + stability.Mass);

    // Physical strength offset
    float strengthOffset = massOffset * (1f - stability.PhysicalStrength);

    // Focus offset (spending focus to brace for impact)
    float focusOffset = 0f;
    if (stability.FocusPool > 0f && stability.FocusUsageRate > 0f)
    {
        focusOffset = strengthOffset * stability.FocusUsageRate;
    }

    // Final disruption
    float finalDisruption = strengthOffset - focusOffset;

    disruption.KnockbackDisruption += math.max(0f, finalDisruption);
}
```

### Stability Rating Calculation

Calculate entity's overall stability:

```csharp
[BurstCompile]
public partial struct StabilityCalculationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var stability in SystemAPI.Query<RefRW<StabilityOffsets>>())
        {
            // Stability from physical attributes
            // Formula: (Strength + log10(Mass + 1)) / 2
            float physicalStability = (stability.ValueRO.PhysicalStrength +
                                     math.log10(stability.ValueRO.Mass + 1f)) / 2f;

            // Stability from mental focus
            // Formula: FocusUsageRate * (FocusPool / MaxFocusPool)
            float mentalStability = stability.ValueRO.FocusUsageRate *
                                  math.min(1f, stability.ValueRO.FocusPool / 100f);

            // Combined stability rating
            stability.ValueRW.StabilityRating = physicalStability + mentalStability;

            // Disruption dampening (0.0 to 0.95 max)
            // High stability = high dampening = less disruption
            stability.ValueRW.DisruptionDampening = math.clamp(
                stability.ValueRO.StabilityRating * 0.5f, 0f, 0.95f);
        }
    }
}
```

---

## Example Scenarios

### Scenario 1: Weak Villager Takes Damage

```csharp
// Weak villager stats
var disruption = new AccuracyDisruption
{
    DisruptionDecayRate = 0.1f,     // Recovers 10% per second
};

var stability = new StabilityOffsets
{
    PhysicalStrength = 0.2f,        // Weak (20%)
    Mass = 70f,                     // 70 kg human
    FocusPool = 20f,
    FocusUsageRate = 0.3f           // Using 30% focus for stability
};

var combatStats = new CombatStats
{
    BaseAccuracy = 0.7f             // 70% base accuracy
};

// Takes 50 damage from warrior's sword
ApplyDamageDisruption(ref disruption, in stability, 50f);

// Calculation:
// Base disruption: 50 * 0.01 = 0.5
// Strength offset: 0.5 * (1 - 0.2) = 0.4
// Focus offset: 0.4 * 0.3 = 0.12
// Final disruption: 0.4 - 0.12 = 0.28

// Result: Accuracy drops from 70% to 42% (-28%)
```

### Scenario 2: Strong Warrior Resists Knockback

```csharp
// Strong warrior stats
var disruption = new AccuracyDisruption
{
    DisruptionDecayRate = 0.15f,    // Recovers 15% per second
};

var stability = new StabilityOffsets
{
    PhysicalStrength = 0.8f,        // Very strong (80%)
    Mass = 120f,                    // 120 kg (armor + body)
    FocusPool = 60f,
    FocusUsageRate = 0.5f           // Using 50% focus for stability
};

var combatStats = new CombatStats
{
    BaseAccuracy = 0.85f            // 85% base accuracy
};

// Takes knockback magnitude 40 from explosion
ApplyKnockbackDisruption(ref disruption, in stability, 40f, float3.zero);

// Calculation:
// Base disruption: 40 * 0.02 = 0.8
// Mass offset: 0.8 / (1 + 120) = 0.0066
// Strength offset: 0.0066 * (1 - 0.8) = 0.00132
// Focus offset: 0.00132 * 0.5 = 0.00066
// Final disruption: 0.00132 - 0.00066 = 0.00066

// Result: Accuracy drops from 85% to 84.93% (barely affected!)
```

### Scenario 3: Focused Mage Maintains Composure

```csharp
// Mage stats (physically weak, mentally strong)
var disruption = new AccuracyDisruption
{
    DisruptionDecayRate = 0.2f,     // Fast recovery
};

var stability = new StabilityOffsets
{
    PhysicalStrength = 0.3f,        // Weak body (30%)
    Mass = 60f,                     // Light
    FocusPool = 100f,               // High focus pool
    FocusUsageRate = 0.8f           // Using 80% focus for stability
};

var combatStats = new CombatStats
{
    BaseAccuracy = 0.9f             // 90% spell accuracy
};

// Takes 30 damage from arrow
ApplyDamageDisruption(ref disruption, in stability, 30f);

// Calculation:
// Base disruption: 30 * 0.01 = 0.3
// Strength offset: 0.3 * (1 - 0.3) = 0.21
// Focus offset: 0.21 * 0.8 = 0.168
// Final disruption: 0.21 - 0.168 = 0.042

// Result: Accuracy drops from 90% to 85.8% (focus compensates for weak body)
```

---

## Damage Types and Disruption Modifiers

Different damage types cause different disruption levels:

```csharp
public enum DamageType : byte
{
    Physical = 0,       // Standard disruption
    Fire = 1,           // +20% disruption (pain)
    Cold = 2,           // +10% disruption (numbness)
    Lightning = 3,      // +50% disruption (paralysis)
    Poison = 4,         // -30% disruption (slow effect)
    Psychic = 5,        // +80% disruption (mental shock)
    Holy = 6,           // +30% disruption (overwhelming)
    Void = 7            // +100% disruption (existential)
}

public struct DamageInstance : IComponentData
{
    public float Amount;
    public DamageType Type;
    public Entity Source;
    public Entity Target;
}

public static float GetDisruptionModifier(DamageType type)
{
    return type switch
    {
        DamageType.Physical => 1.0f,
        DamageType.Fire => 1.2f,
        DamageType.Cold => 1.1f,
        DamageType.Lightning => 1.5f,
        DamageType.Poison => 0.7f,
        DamageType.Psychic => 1.8f,
        DamageType.Holy => 1.3f,
        DamageType.Void => 2.0f,
        _ => 1.0f
    };
}
```

---

## Integration with Other Systems

### Bay Combat Integration

Combat positions amplify or reduce disruption:

```csharp
public struct BayStabilityModifier : IComponentData
{
    public float StabilityBonus;        // Bays provide extra stability
    public bool IsBraced;               // Braced positions = less knockback
}

// Example: Mech in carrier bay has +40% stability
// Foot soldier in open field has +0% stability
```

### Memory Tapping Integration

Memory tapping bonuses include disruption resistance:

```csharp
public struct BonusProfile
{
    // ... existing bonuses ...

    public float DisruptionResistance;  // +0% to +100% resistance
    public float FocusRegeneration;     // Extra focus per second
}

// Example: "Home" memory tap grants +50% disruption resistance
// Defenders become harder to shake
```

### Morale Integration

Morale affects stability:

```csharp
public static float GetMoraleStabilityModifier(MoraleChange morale)
{
    return morale switch
    {
        MoraleChange.Terrified => -0.6f,    // -60% stability
        MoraleChange.Demoralized => -0.3f,
        MoraleChange.Shaken => -0.15f,
        MoraleChange.Neutral => 0f,
        MoraleChange.Encouraged => 0.1f,
        MoraleChange.Rallied => 0.2f,
        MoraleChange.Inspired => 0.4f,
        MoraleChange.Fanatic => 0.6f,       // +60% stability
        _ => 0f
    };
}
```

---

## Performance Considerations

**Profiling Targets**:
```
Disruption Calculation:   <0.05ms per entity
Stability Calculation:    <0.05ms per entity
Damage Application:       <0.1ms per hit
Knockback Application:    <0.1ms per knockback
────────────────────────────────────────
Total (1000 entities):    <300ms per frame
```

**Optimizations**:
- Use `[BurstCompile]` on all systems
- Batch process disruption decay (not per-entity)
- Cache stability ratings (recalculate only when attributes change)
- Use spatial partitioning for damage/knockback queries

---

## Summary

**Core Mechanics**:
1. **Accuracy Disruption**: Damage and knockback reduce accuracy
2. **Physical Strength Offset**: Stronger entities resist disruption better
3. **Mass Offset**: Heavier entities resist knockback disruption better
4. **Focus Offset**: Spending focus dampens disruption
5. **Disruption Decay**: Entities recover accuracy over time
6. **Damage Type Modifiers**: Different damage types cause different disruption levels

**Formulas**:
- Damage disruption: `Damage * 0.01 * (1 - PhysicalStrength) - (Result * FocusUsage)`
- Knockback disruption: `(Magnitude * 0.02 / (1 + Mass)) * (1 - PhysicalStrength) - (Result * FocusUsage)`
- Stability rating: `(PhysicalStrength + log10(Mass + 1)) / 2 + FocusUsage`
- Disruption dampening: `StabilityRating * 0.5` (clamped 0.0 to 0.95)

**Cross-System Integration**:
- Bay combat: Braced positions provide stability bonuses
- Memory tapping: Bonuses include disruption resistance
- Morale: Affects stability rating (-60% to +60%)
- Damage types: Modifiers from 0.7× (poison) to 2.0× (void)

**Key Insight**: Strong, heavy, focused entities maintain accuracy under fire, while weak, light, unfocused entities suffer severe accuracy penalties when disrupted.
