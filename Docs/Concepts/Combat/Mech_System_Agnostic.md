# Mech System - Agnostic Framework

## Overview

This document provides the mathematical and algorithmic framework for modular mech/construct systems. Mechs consist of chassis (hulls) that hold modules (equipment) which may have addons (enhancements). All components have quality and rarity attributes that modify their effectiveness. Piloting configurations range from single-pilot control to multi-crew coordination to swarm command.

**Core Equations:**
- Effective stats = Base stats × Quality multiplier × Rarity multiplier × Pilot skill
- Module compatibility = Slot availability + Power budget + Weight capacity
- Pilot efficiency = Skill factor × Fatigue penalty × Coordination bonus

---

## Chassis System

### Structural Integrity Calculation

```csharp
public static float CalculateStructuralHP(
    float baseChassis HP,
    SizeClass sizeClass,
    int techLevel,                      // 1-10
    Quality quality,
    float materialBonus)                // 0-1
{
    float sizeMultiplier = sizeClass switch
    {
        SizeClass.Small => 0.5f,
        SizeClass.Medium => 1.0f,
        SizeClass.Large => 2.5f,
        SizeClass.Colossus => 6.0f,
        _ => 1.0f
    };

    float techMultiplier = 1f + (techLevel * 0.15f);  // +15% per tech level

    float qualityMultiplier = quality switch
    {
        Quality.Crude => 0.7f,
        Quality.Standard => 1.0f,
        Quality.Masterwork => 1.3f,
        Quality.Legendary => 1.7f,
        Quality.Artifact => 2.3f,
        _ => 1.0f
    };

    float materialMultiplier = 1f + materialBonus;

    float finalHP = baseChassisHP * sizeMultiplier * techMultiplier * qualityMultiplier * materialMultiplier;

    return finalHP;
}
```

**Example:**
```
Medium War Golem (Masterwork, TL 5, Mithril chassis):
- Base HP: 3,000
- Size: Medium (1.0×)
- Tech: TL 5 (1.75×)
- Quality: Masterwork (1.3×)
- Material: Mithril (+0.25 bonus = 1.25×)
- Final HP: 3,000 × 1.0 × 1.75 × 1.3 × 1.25 = 8,531 HP
```

---

### Module Slot Calculation

```csharp
public static int CalculateModuleSlots(
    SizeClass sizeClass,
    int techLevel,
    ChassisType chassisType)
{
    int baseSlotsForSize = sizeClass switch
    {
        SizeClass.Small => 4,
        SizeClass.Medium => 8,
        SizeClass.Large => 14,
        SizeClass.Colossus => 22,
        _ => 8
    };

    int techBonus = math.max(0, techLevel - 3);  // TL 4+ grants bonus slots

    float chassisModifier = chassisType switch
    {
        ChassisType.Combat => 1.0f,         // Standard slots
        ChassisType.Utility => 1.2f,        // +20% slots for utility
        ChassisType.Specialized => 0.8f,    // -20% slots but better stats
        _ => 1.0f
    };

    int totalSlots = (int)((baseSlotsForSize + techBonus) * chassisModifier);

    return totalSlots;
}
```

**Example:**
```
Large Utility Mech (TL 7):
- Base slots (Large): 14
- Tech bonus: 7 - 3 = 4 slots
- Chassis modifier: Utility (1.2×)
- Total: (14 + 4) × 1.2 = 21.6 → 21 slots
```

---

### Weight Capacity

```csharp
public static float CalculateWeightCapacity(
    float chassisMass,                  // kg
    float structuralStrength,           // 0-1
    int techLevel)
{
    float baseCapacity = chassisMass * 0.4f;  // 40% of chassis mass

    float strengthBonus = structuralStrength * chassisMass * 0.3f;

    float techBonus = techLevel * 0.05f;  // +5% per TL

    float totalCapacity = (baseCapacity + strengthBonus) * (1f + techBonus);

    return totalCapacity;
}
```

**Example:**
```
Medium chassis (1,000 kg, strength 0.7, TL 5):
- Base capacity: 1,000 × 0.4 = 400 kg
- Strength bonus: 0.7 × 1,000 × 0.3 = 210 kg
- Tech bonus: 5 × 0.05 = +25%
- Total: (400 + 210) × 1.25 = 762.5 kg capacity
```

---

## Module System

### Module Effectiveness

```csharp
public static float CalculateModuleEffectiveness(
    float baseValue,
    Quality quality,
    Rarity rarity,
    float durability,                   // 0-1
    int techLevel,
    int targetTechLevel)                // Tech level of target/environment
{
    float qualityMultiplier = quality switch
    {
        Quality.Crude => 0.65f,
        Quality.Standard => 1.0f,
        Quality.Masterwork => 1.35f,
        Quality.Legendary => 1.75f,
        Quality.Artifact => 2.4f,
        _ => 1.0f
    };

    float rarityMultiplier = rarity switch
    {
        Rarity.Common => 1.0f,
        Rarity.Uncommon => 1.15f,
        Rarity.Rare => 1.35f,
        Rarity.Epic => 1.65f,
        Rarity.Mythic => 2.2f,
        _ => 1.0f
    };

    float durabilityPenalty = math.max(0.2f, durability);  // Min 20% effectiveness

    float techDifference = math.abs(techLevel - targetTechLevel);
    float techPenalty = 1f - (techDifference * 0.05f);  // -5% per TL difference
    techPenalty = math.max(0.5f, techPenalty);  // Min 50% effectiveness

    float effectiveValue = baseValue * qualityMultiplier * rarityMultiplier * durabilityPenalty * techPenalty;

    return effectiveValue;
}
```

**Example:**
```
Epic Quality Rare Weapon Module (TL 7 weapon vs TL 5 target):
- Base damage: 300
- Quality: Epic (1.65×)
- Rarity: Rare (1.35×)
- Durability: 85% (0.85×)
- Tech difference: |7 - 5| = 2, penalty = 1 - (2 × 0.05) = 0.9×
- Effective damage: 300 × 1.65 × 1.35 × 0.85 × 0.9 = 508 damage
```

---

### Addon Stacking

```csharp
public static float CalculateAddonBonus(
    NativeArray<AddonModifier> addons,
    int maxAddons)
{
    if (addons.Length == 0)
        return 1f;

    float totalBonus = 1f;
    int addonsApplied = math.min(addons.Length, maxAddons);

    for (int i = 0; i < addonsApplied; i++)
    {
        float diminishingFactor = 1f - (i * 0.15f);  // Each addon is 15% less effective
        diminishingFactor = math.max(0.4f, diminishingFactor);

        float addonBonus = addons[i].BonusMultiplier - 1f;  // Convert to bonus only
        totalBonus += (addonBonus * diminishingFactor);
    }

    return totalBonus;
}
```

**Example:**
```
Weapon with 3 damage addons (+20%, +25%, +15%):
- Addon 1: 0.20 × 1.0 = +0.20
- Addon 2: 0.25 × 0.85 = +0.2125
- Addon 3: 0.15 × 0.70 = +0.105
- Total bonus: 1 + 0.20 + 0.2125 + 0.105 = 1.5175 (51.75% damage increase)
```

---

### Power Consumption

```csharp
public static float CalculatePowerConsumption(
    ModuleType moduleType,
    float moduleRating,                 // Damage, shield strength, sensor range, etc.
    Quality quality,
    bool isActive)
{
    float basePowerRate = moduleType switch
    {
        ModuleType.LightWeapon => 0.05f,
        ModuleType.MediumWeapon => 0.12f,
        ModuleType.HeavyWeapon => 0.25f,
        ModuleType.Shield => 0.08f,
        ModuleType.Sensor => 0.03f,
        ModuleType.Utility => 0.04f,
        _ => 0.05f
    };

    float activePower = moduleRating * basePowerRate;

    float qualityEfficiency = quality switch
    {
        Quality.Crude => 1.4f,           // +40% power consumption
        Quality.Standard => 1.0f,
        Quality.Masterwork => 0.75f,     // -25% power consumption
        Quality.Legendary => 0.55f,      // -45% power consumption
        Quality.Artifact => 0.35f,       // -65% power consumption
        _ => 1.0f
    };

    float powerConsumption = activePower * qualityEfficiency;

    if (!isActive)
        powerConsumption *= 0.1f;  // Idle mode: 10% consumption

    return powerConsumption;
}
```

**Example:**
```
Legendary Heavy Weapon (800 damage rating):
- Base rate: 0.25
- Active power: 800 × 0.25 = 200 units/sec
- Quality efficiency: 0.55× (Legendary)
- Final consumption: 200 × 0.55 = 110 units/sec (active)
- Idle consumption: 110 × 0.1 = 11 units/sec (standby)
```

---

### Module Durability Degradation

```csharp
public static float CalculateDurabilityLoss(
    float damageReceived,
    float moduleMaxHP,
    Quality quality,
    float deltaTime)
{
    float damageRatio = damageReceived / moduleMaxHP;

    float baseDegradation = damageRatio * 0.05f;  // 5% durability loss per module HP lost

    float qualityResistance = quality switch
    {
        Quality.Crude => 1.5f,           // Degrades 50% faster
        Quality.Standard => 1.0f,
        Quality.Masterwork => 0.7f,      // Degrades 30% slower
        Quality.Legendary => 0.4f,       // Degrades 60% slower
        Quality.Artifact => 0.1f,        // Degrades 90% slower (near-indestructible)
        _ => 1.0f
    };

    float durabilityLoss = baseDegradation * qualityResistance * deltaTime;

    return durabilityLoss;
}
```

**Example:**
```
Masterwork shield takes 1,000 damage (max HP 3,000):
- Damage ratio: 1,000 / 3,000 = 0.333
- Base degradation: 0.333 × 0.05 = 0.01665 (1.665%)
- Quality resistance: 0.7× (Masterwork)
- Durability loss: 0.01665 × 0.7 = 0.011655 (1.17% durability lost)
```

---

## Power Core System

### Core Capacity and Regeneration

```csharp
public static (float capacity, float regenRate) CalculatePowerCore(
    SizeClass coreSize,
    Quality quality,
    int techLevel,
    CoreType coreType)
{
    float baseCapacity = coreSize switch
    {
        SizeClass.Small => 500f,
        SizeClass.Medium => 1500f,
        SizeClass.Large => 4000f,
        SizeClass.Colossus => 10000f,
        _ => 1500f
    };

    float baseRegen = baseCapacity * 0.06f;  // 6% per second

    float qualityCapacityMult = quality switch
    {
        Quality.Crude => 0.6f,
        Quality.Standard => 1.0f,
        Quality.Masterwork => 1.5f,
        Quality.Legendary => 2.3f,
        Quality.Artifact => 3.8f,
        _ => 1.0f
    };

    float qualityRegenMult = quality switch
    {
        Quality.Crude => 0.7f,
        Quality.Standard => 1.0f,
        Quality.Masterwork => 1.4f,
        Quality.Legendary => 2.0f,
        Quality.Artifact => 3.2f,
        _ => 1.0f
    };

    float techBonus = 1f + (techLevel * 0.1f);  // +10% per TL

    float coreTypeBonus = coreType switch
    {
        CoreType.Standard => 1.0f,
        CoreType.HighCapacity => 1.5f,      // +50% capacity, -20% regen
        CoreType.FastRecharge => 0.8f,      // -20% capacity, +60% regen
        CoreType.Balanced => 1.15f,         // +15% both
        _ => 1.0f
    };

    float finalCapacity = baseCapacity * qualityCapacityMult * techBonus * coreTypeBonus;

    float finalRegen = baseRegen * qualityRegenMult * techBonus;
    if (coreType == CoreType.HighCapacity)
        finalRegen *= 0.8f;
    else if (coreType == CoreType.FastRecharge)
        finalRegen *= 1.6f;
    else if (coreType == CoreType.Balanced)
        finalRegen *= 1.15f;

    return (finalCapacity, finalRegen);
}
```

**Example:**
```
Legendary Medium Core (TL 7, Fast Recharge type):
- Base capacity: 1,500
- Quality capacity: 1,500 × 2.3 = 3,450
- Tech bonus: 1 + (7 × 0.1) = 1.7×
- Core type: Fast Recharge (0.8×)
- Final capacity: 3,450 × 1.7 × 0.8 = 4,692 units

- Base regen: 1,500 × 0.06 = 90 units/sec
- Quality regen: 90 × 2.0 = 180 units/sec
- Tech bonus: 180 × 1.7 = 306 units/sec
- Core type: Fast Recharge (1.6×)
- Final regen: 306 × 1.6 = 489.6 units/sec
```

---

### Power Budget Analysis

```csharp
public static PowerBudget CalculatePowerBudget(
    float coreCapacity,
    float coreRegenRate,
    NativeArray<ModulePowerDraw> modules,
    float chassisIdleDrain,
    float combatTime)
{
    float totalActivePower = chassisIdleDrain;
    float totalIdlePower = chassisIdleDrain;

    for (int i = 0; i < modules.Length; i++)
    {
        totalActivePower += modules[i].ActiveConsumption;
        totalIdlePower += modules[i].IdleConsumption;
    }

    float netActiveDeficit = totalActivePower - coreRegenRate;
    float netIdleDeficit = totalIdlePower - coreRegenRate;

    float combatEndurance = netActiveDeficit > 0
        ? coreCapacity / netActiveDeficit
        : float.PositiveInfinity;

    float idleEndurance = netIdleDeficit > 0
        ? coreCapacity / netIdleDeficit
        : float.PositiveInfinity;

    float burstDuration = combatEndurance;
    float cooldownNeeded = 0f;

    if (netActiveDeficit > 0)
    {
        float powerUsedInBurst = netActiveDeficit * burstDuration;
        cooldownNeeded = powerUsedInBurst / coreRegenRate;
    }

    return new PowerBudget
    {
        TotalActiveDrain = totalActivePower,
        TotalIdleDrain = totalIdlePower,
        NetActiveDeficit = netActiveDeficit,
        NetIdleDeficit = netIdleDeficit,
        CombatEndurance = combatEndurance,
        IdleEndurance = idleEndurance,
        BurstCooldownRatio = cooldownNeeded / burstDuration
    };
}
```

**Example:**
```
Mech with 4,000 capacity, 120 regen, 600 idle + 1,200 active consumption:
- Net active deficit: 1,200 - 120 = 1,080 units/sec
- Combat endurance: 4,000 / 1,080 = 3.7 seconds
- Cooldown needed: (1,080 × 3.7) / 120 = 33.3 seconds
- Burst/cooldown ratio: 33.3 / 3.7 = 9:1 (need 9 sec rest per 1 sec combat)
```

---

## Piloting System

### Single Pilot Efficiency

```csharp
public static float CalculatePilotEfficiency(
    int pilotSkillStat,                 // INT, DEX, or primary attribute
    int requiredSkillStat,              // Minimum for this mech
    int pilotSkillLevel,                // 1-20
    float fatigueLevel,                 // 0-100
    SizeClass mechSize)
{
    float statRatio = (float)pilotSkillStat / requiredSkillStat;
    statRatio = math.clamp(statRatio, 0.5f, 1.5f);  // 50% to 150%

    float skillBonus = 1f + (pilotSkillLevel * 0.08f);  // +8% per skill level

    float fatiguepenalty = 1f - (fatigueLevel / 200f);  // -0.5% per fatigue point
    fatiguePenalty = math.max(0.3f, fatiguePenalty);  // Min 30% efficiency

    float sizeComplexity = mechSize switch
    {
        SizeClass.Small => 1.05f,        // +5% efficiency (easy to control)
        SizeClass.Medium => 1.0f,
        SizeClass.Large => 0.92f,        // -8% efficiency (harder to control)
        SizeClass.Colossus => 0.80f,     // -20% efficiency (very difficult)
        _ => 1.0f
    };

    float totalEfficiency = statRatio * skillBonus * fatiguePenalty * sizeComplexity;

    return math.clamp(totalEfficiency, 0.3f, 2.5f);  // 30% to 250%
}
```

**Example:**
```
Pilot (INT 85, skill 12, fatigue 40) operating Medium mech (requires INT 70):
- Stat ratio: 85 / 70 = 1.214
- Skill bonus: 1 + (12 × 0.08) = 1.96
- Fatigue penalty: 1 - (40 / 200) = 0.8
- Size complexity: 1.0 (Medium)
- Total efficiency: 1.214 × 1.96 × 0.8 × 1.0 = 1.904 (190.4%)
```

---

### Fatigue Accumulation

```csharp
public static float CalculateFatigueGain(
    SizeClass mechSize,
    float pilotEndurance,               // 0-100 stat
    bool inCombat,
    float deltaTime)                    // Hours
{
    float baseFatigueRate = mechSize switch
    {
        SizeClass.Small => 0.5f,         // 0.5 per hour
        SizeClass.Medium => 2.0f,        // 2 per hour
        SizeClass.Large => 6.0f,         // 6 per hour
        SizeClass.Colossus => 15.0f,     // 15 per hour
        _ => 2.0f
    };

    float enduranceReduction = 1f - (pilotEndurance / 200f);  // 50% reduction at 100 endurance

    float combatMultiplier = inCombat ? 2.5f : 1.0f;

    float fatigueGain = baseFatigueRate * enduranceReduction * combatMultiplier * deltaTime;

    return fatigueGain;
}
```

**Example:**
```
Pilot (endurance 75) operates Large mech in combat for 30 minutes (0.5 hours):
- Base rate: 6 per hour
- Endurance reduction: 1 - (75 / 200) = 0.625×
- Combat multiplier: 2.5×
- Fatigue gain: 6 × 0.625 × 2.5 × 0.5 = 4.7 fatigue points
```

---

### Multi-Pilot Coordination

```csharp
public static float CalculateCrewCoordination(
    NativeArray<int> crewSkillStats,    // Each crew member's primary stat
    CrewTraining trainingLevel,
    int communicationQuality,           // 0-100 (degraded by damage, jamming)
    bool hasIncapacitatedCrew)
{
    float averageSkill = 0f;
    for (int i = 0; i < crewSkillStats.Length; i++)
    {
        averageSkill += crewSkillStats[i];
    }
    averageSkill /= crewSkillStats.Length;

    float skillFactor = averageSkill / 100f;  // 0-1 (or higher for exceptional crews)

    float trainingMultiplier = trainingLevel switch
    {
        CrewTraining.Untrained => 0.7f,
        CrewTraining.Basic => 0.85f,
        CrewTraining.Trained => 1.0f,
        CrewTraining.Veteran => 1.2f,
        CrewTraining.Elite => 1.4f,
        _ => 1.0f
    };

    float commFactor = communicationQuality / 100f;

    float incapacitationPenalty = hasIncapacitatedCrew ? 0.6f : 1.0f;

    float coordination = skillFactor * trainingMultiplier * commFactor * incapacitationPenalty;

    return math.clamp(coordination, 0.3f, 1.6f);  // 30% to 160%
}
```

**Example:**
```
Elite crew (avg skill 82), clear comms (100), no casualties:
- Skill factor: 82 / 100 = 0.82
- Training: Elite (1.4×)
- Comm factor: 100 / 100 = 1.0
- Incapacitation: 1.0 (none)
- Coordination: 0.82 × 1.4 × 1.0 × 1.0 = 1.148 (114.8%)

Same crew after taking damage (comms 60%, 1 crew incapacitated):
- Skill factor: 0.82
- Training: 1.4×
- Comm factor: 0.6
- Incapacitation: 0.6
- Coordination: 0.82 × 1.4 × 0.6 × 0.6 = 0.413 (41.3%)
```

---

### Swarm Control Efficiency

```csharp
public static float CalculateSwarmEfficiency(
    int controllerSkillStat,            // INT or equivalent
    int baseControlLimit,               // From skill/tech
    int activeConstructCount,
    float averageConstructComplexity)   // 0-1
{
    float overloadFactor = (float)activeConstructCount / baseControlLimit;

    float efficiencyPenalty = 0.08f * activeConstructCount;  // -8% per construct
    efficiencyPenalty *= (1f + averageConstructComplexity);  // More complex = more penalty

    float baseEfficiency = 1f - efficiencyPenalty;

    if (overloadFactor > 1f)
    {
        float overloadPenalty = (overloadFactor - 1f) * 0.5f;  // -50% per construct over limit
        baseEfficiency -= overloadPenalty;
    }

    float skillBonus = controllerSkillStat / 100f;
    skillBonus = math.max(0.5f, skillBonus);  // Min 50%

    float finalEfficiency = baseEfficiency * skillBonus;

    return math.clamp(finalEfficiency, 0.05f, 1.2f);  // 5% to 120%
}
```

**Example:**
```
Controller (INT 100, limit 8) commands 10 constructs (complexity 0.4):
- Overload factor: 10 / 8 = 1.25 (overloaded)
- Efficiency penalty: 0.08 × 10 × (1 + 0.4) = 1.12 (112%)
- Base efficiency: 1 - 1.12 = -0.12
- Overload penalty: (1.25 - 1) × 0.5 = 0.125
- Base efficiency: -0.12 - 0.125 = -0.245
- Skill bonus: 100 / 100 = 1.0
- Final efficiency: -0.245 × 1.0 = -0.245 → clamped to 0.05 (5%)

Same controller with 6 constructs:
- Overload factor: 6 / 8 = 0.75 (within limit)
- Efficiency penalty: 0.08 × 6 × 1.4 = 0.672
- Base efficiency: 1 - 0.672 = 0.328
- Overload penalty: 0 (not overloaded)
- Skill bonus: 1.0
- Final efficiency: 0.328 × 1.0 = 0.328 (32.8% per construct)
```

---

### Swarm Coordination Bonuses

```csharp
public static float CalculateSwarmTacticBonus(
    SwarmTactic tactic,
    int constructsInFormation,
    float averageConstructEfficiency)
{
    float tacticBonus = tactic switch
    {
        SwarmTactic.CoordinatedStrike => 0.25f,      // +25% damage when all attack same target
        SwarmTactic.DefensiveFormation => 0.20f,     // +20% armor when in formation
        SwarmTactic.FlankingManeuver => 0.40f,       // +40% armor penetration when surrounding
        SwarmTactic.DistractionProtocol => 0.35f,    // +35% crit chance for attackers
        _ => 0f
    };

    float formationQuality = constructsInFormation / 10f;  // Optimal at 10 constructs
    formationQuality = math.clamp(formationQuality, 0.3f, 1.2f);

    float efficiencyFactor = averageConstructEfficiency;  // Low efficiency = poor tactics

    float totalBonus = tacticBonus * formationQuality * efficiencyFactor;

    return totalBonus;
}
```

**Example:**
```
6 constructs (45% avg efficiency) perform Flanking Maneuver:
- Tactic bonus: 40%
- Formation quality: 6 / 10 = 0.6
- Efficiency factor: 0.45
- Total bonus: 0.40 × 0.6 × 0.45 = 0.108 (10.8% armor penetration)
```

---

## Quality and Rarity System

### Combined Stat Calculation

```csharp
public static float ApplyQualityAndRarity(
    float baseValue,
    Quality quality,
    Rarity rarity,
    StatType statType)
{
    float qualityMultiplier = quality switch
    {
        Quality.Crude => 0.65f,
        Quality.Standard => 1.0f,
        Quality.Masterwork => 1.35f,
        Quality.Legendary => 1.8f,
        Quality.Artifact => 2.5f,
        _ => 1.0f
    };

    float rarityBonus = rarity switch
    {
        Rarity.Common => 0f,
        Rarity.Uncommon => 0.12f,
        Rarity.Rare => 0.30f,
        Rarity.Epic => 0.60f,
        Rarity.Mythic => 1.20f,
        _ => 0f
    };

    // Rarity affects additive bonus for primary stat only
    float modifiedValue = baseValue * qualityMultiplier;
    modifiedValue += (baseValue * rarityBonus);

    return modifiedValue;
}
```

**Example:**
```
Legendary Quality, Epic Rarity weapon (300 base damage):
- Quality multiplier: 1.8× → 300 × 1.8 = 540
- Rarity bonus: +60% → 300 × 0.6 = 180
- Final damage: 540 + 180 = 720 damage
```

---

### Rarity Special Effect Count

```csharp
public static int CalculateSpecialEffectSlots(
    Rarity rarity,
    Quality quality)
{
    int baseSlots = rarity switch
    {
        Rarity.Common => 0,
        Rarity.Uncommon => 1,
        Rarity.Rare => 2,
        Rarity.Epic => 3,
        Rarity.Mythic => 4,
        _ => 0
    };

    int qualityBonus = quality switch
    {
        Quality.Masterwork => 1,
        Quality.Legendary => 2,
        Quality.Artifact => 3,
        _ => 0
    };

    return baseSlots + qualityBonus;
}
```

**Example:**
```
Mythic Rarity, Legendary Quality item:
- Base slots (Mythic): 4
- Quality bonus (Legendary): +2
- Total special effect slots: 6
```

---

## Module Compatibility

### Slot Compatibility Check

```csharp
public static bool IsModuleCompatible(
    ChassisData chassis,
    ModuleData module)
{
    // Check slot availability
    int availableSlots = chassis.TotalSlots - chassis.UsedSlots;
    if (availableSlots < module.SlotSize)
        return false;

    // Check size class requirement
    if ((int)chassis.SizeClass < (int)module.MinimumChassisSize)
        return false;

    // Check weight capacity
    float remainingCapacity = chassis.WeightCapacity - chassis.CurrentWeight;
    if (remainingCapacity < module.Weight)
        return false;

    // Check power availability (must sustain idle consumption)
    float availablePower = chassis.CoreRegenRate - chassis.IdlePowerDrain;
    if (availablePower < module.IdleConsumption)
        return false;

    // Check tech level (can't install module more than 3 TL higher)
    if (module.TechLevel > chassis.TechLevel + 3)
        return false;

    return true;
}
```

---

### Load Distribution

```csharp
public static float CalculateStructuralStress(
    float totalModuleWeight,
    float chassisWeightCapacity,
    DistributionBalance weightDistribution)  // 0-1, where 1 = perfectly balanced
{
    float weightRatio = totalModuleWeight / chassisWeightCapacity;

    float overweightPenalty = 1f;
    if (weightRatio > 1f)
    {
        overweightPenalty = 1f + ((weightRatio - 1f) * 2f);  // Double penalty for overweight
    }

    float distributionFactor = math.lerp(1.5f, 1.0f, weightDistribution);  // Poor distribution = 50% more stress

    float structuralStress = weightRatio * overweightPenalty * distributionFactor;

    return structuralStress;
}
```

**Example:**
```
Chassis (800 kg capacity) with 900 kg modules (poor distribution 0.4):
- Weight ratio: 900 / 800 = 1.125
- Overweight penalty: 1 + ((1.125 - 1) × 2) = 1.25
- Distribution factor: lerp(1.5, 1.0, 0.4) = 1.3
- Structural stress: 1.125 × 1.25 × 1.3 = 1.828 (82.8% overstressed)
```

---

## Damage and Repair

### Damage Distribution

```csharp
public static void DistributeDamageToModules(
    float totalDamage,
    NativeArray<ModuleHealth> modules,
    DamageType damageType,
    ref Random random)
{
    // Calculate vulnerability weights
    NativeArray<float> vulnerabilityWeights = new NativeArray<float>(modules.Length, Allocator.Temp);
    float totalWeight = 0f;

    for (int i = 0; i < modules.Length; i++)
    {
        float typeVulnerability = GetTypeVulnerability(modules[i].Type, damageType);
        float exposureWeight = modules[i].ExposureRating;  // 0-1, how exposed the module is
        float weight = typeVulnerability * exposureWeight * modules[i].CurrentHP;

        vulnerabilityWeights[i] = weight;
        totalWeight += weight;
    }

    // Distribute damage proportionally
    float remainingDamage = totalDamage;
    for (int i = 0; i < modules.Length; i++)
    {
        if (totalWeight <= 0f)
            break;

        float moduleDamageShare = (vulnerabilityWeights[i] / totalWeight) * totalDamage;
        moduleDamageShare += random.NextFloat(-10f, 10f);  // ±10 damage variance
        moduleDamageShare = math.max(0f, moduleDamageShare);

        modules[i] = new ModuleHealth
        {
            Type = modules[i].Type,
            CurrentHP = math.max(0f, modules[i].CurrentHP - moduleDamageShare),
            MaxHP = modules[i].MaxHP,
            ExposureRating = modules[i].ExposureRating
        };
    }

    vulnerabilityWeights.Dispose();
}
```

---

### Repair Time Calculation

```csharp
public static float CalculateRepairTime(
    float damagePercent,                // 0-100
    Quality quality,
    Rarity rarity,
    int technicianSkill,                // 0-100
    bool hasProperTools,
    bool hasReplacementParts)
{
    float baseRepairTime = damagePercent * 0.1f;  // 0.1 hours per % damage (10 hours for 100% damage)

    float qualityComplexity = quality switch
    {
        Quality.Crude => 0.7f,           // Easy to repair
        Quality.Standard => 1.0f,
        Quality.Masterwork => 1.5f,      // Takes longer
        Quality.Legendary => 2.5f,       // Much longer
        Quality.Artifact => 5.0f,        // Requires master artisans
        _ => 1.0f
    };

    float rarityComplexity = rarity switch
    {
        Rarity.Common => 1.0f,
        Rarity.Uncommon => 1.2f,
        Rarity.Rare => 1.5f,
        Rarity.Epic => 2.0f,
        Rarity.Mythic => 3.5f,
        _ => 1.0f
    };

    float technicianEfficiency = 1f / (technicianSkill / 100f);  // Higher skill = faster repairs
    technicianEfficiency = math.clamp(technicianEfficiency, 0.5f, 3.0f);

    float toolModifier = hasProperTools ? 1.0f : 2.5f;  // 2.5× longer without tools
    float partsModifier = hasReplacementParts ? 1.0f : 1.8f;  // 1.8× longer if fabricating parts

    float totalRepairTime = baseRepairTime * qualityComplexity * rarityComplexity * technicianEfficiency * toolModifier * partsModifier;

    return totalRepairTime;
}
```

**Example:**
```
Legendary Epic weapon (60% damaged, tech skill 80, has tools, no parts):
- Base repair time: 60 × 0.1 = 6 hours
- Quality complexity: 2.5× (Legendary)
- Rarity complexity: 2.0× (Epic)
- Technician efficiency: 1 / (80/100) = 1.25×
- Tool modifier: 1.0× (has tools)
- Parts modifier: 1.8× (no parts)
- Total: 6 × 2.5 × 2.0 × 1.25 × 1.0 × 1.8 = 67.5 hours (~3 days)
```

---

## ECS Integration

### Core Components

```csharp
public struct MechChassis : IComponentData
{
    public Entity PilotEntity;
    public SizeClass Size;
    public int TechLevel;
    public float MaxStructuralHP;
    public float CurrentStructuralHP;
    public float ArmorRating;
    public float MovementSpeed;
    public int TotalModuleSlots;
    public int UsedModuleSlots;
    public float WeightCapacity;
    public float CurrentWeight;
    public Quality ChassisQuality;
}

public struct PowerCore : IComponentData
{
    public Entity ParentChassis;
    public float MaxCapacity;
    public float CurrentPower;
    public float RegenerationRate;
    public CoreType Type;
    public Quality Quality;
}

public struct MechModule : IComponentData
{
    public Entity ParentChassis;
    public ModuleType Type;
    public Quality Quality;
    public Rarity Rarity;
    public int SlotSize;
    public float Weight;
    public float ActivePowerDraw;
    public float IdlePowerDraw;
    public float MaxHP;
    public float CurrentHP;
    public float Durability;              // 0-1
    public int TechLevel;
    public bool IsActive;
}

public struct ModuleAddon : IComponentData
{
    public Entity ParentModule;
    public AddonType Type;
    public Rarity Rarity;
    public float BonusMultiplier;
}

public struct PilotControl : IComponentData
{
    public Entity PilotEntity;
    public Entity ControlledMech;
    public int SkillStat;
    public int SkillLevel;
    public float FatigueLevel;
    public PilotMode Mode;                // Single, CrewMember, SwarmController
}

public struct CrewCoordination : IComponentData
{
    public Entity MechEntity;
    public FixedList64Bytes<Entity> CrewMembers;
    public CrewTraining Training;
    public float CoordinationEfficiency;
    public int CommunicationQuality;      // 0-100
    public bool HasIncapacitatedMembers;
}

public struct SwarmControl : IComponentData
{
    public Entity ControllerEntity;
    public FixedList128Bytes<Entity> ControlledMechs;
    public int ControlLimit;
    public float AverageEfficiency;
    public SwarmTactic ActiveTactic;
}
```

---

### Core Systems

**MechPowerManagementSystem** (1 Hz):
```csharp
public partial struct MechPowerManagementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (powerCore, chassis) in SystemAPI.Query<RefRW<PowerCore>, RefRO<MechChassis>>())
        {
            // Regenerate power
            powerCore.ValueRW.CurrentPower = math.min(
                powerCore.ValueRW.CurrentPower + powerCore.ValueRO.RegenerationRate,
                powerCore.ValueRO.MaxCapacity
            );

            // Calculate total power consumption from modules
            float totalConsumption = CalculateTotalModuleConsumption(chassis.ValueRO.Entity);

            // Deduct power
            powerCore.ValueRW.CurrentPower -= totalConsumption;

            // Emergency shutdown if power depleted
            if (powerCore.ValueRW.CurrentPower <= 0f)
            {
                powerCore.ValueRW.CurrentPower = 0f;
                DisableAllModules(chassis.ValueRO.Entity);
            }
        }
    }
}
```

---

**PilotFatigueSystem** (1 Hz):
```csharp
public partial struct PilotFatigueSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime / 3600f;  // Convert to hours

        foreach (var (pilotControl, pilot) in SystemAPI.Query<RefRW<PilotControl>, RefRO<PilotData>>())
        {
            var chassis = SystemAPI.GetComponent<MechChassis>(pilotControl.ValueRO.ControlledMech);
            bool inCombat = SystemAPI.HasComponent<InCombatTag>(pilotControl.ValueRO.ControlledMech);

            float fatigueGain = CalculateFatigueGain(
                chassis.Size,
                pilot.ValueRO.Endurance,
                inCombat,
                deltaTime
            );

            pilotControl.ValueRW.FatigueLevel = math.min(100f, pilotControl.ValueRW.FatigueLevel + fatigueGain);

            // Force eject at 100 fatigue
            if (pilotControl.ValueRW.FatigueLevel >= 100f)
            {
                EjectPilot(pilotControl.ValueRO.PilotEntity, pilotControl.ValueRO.ControlledMech);
            }
        }
    }
}
```

---

**ModuleDurabilitySystem** (1 Hz):
```csharp
public partial struct ModuleDurabilitySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var module in SystemAPI.Query<RefRW<MechModule>>())
        {
            // Degrade durability based on damage taken
            if (module.ValueRO.CurrentHP < module.ValueRO.MaxHP)
            {
                float damagePercent = 1f - (module.ValueRO.CurrentHP / module.ValueRO.MaxHP);
                float durabilityLoss = CalculateDurabilityLoss(
                    damagePercent * module.ValueRO.MaxHP,
                    module.ValueRO.MaxHP,
                    module.ValueRO.Quality,
                    SystemAPI.Time.DeltaTime
                );

                module.ValueRW.Durability = math.max(0f, module.ValueRW.Durability - durabilityLoss);
            }

            // Reduce effectiveness based on durability
            if (module.ValueRW.Durability < 0.5f)
            {
                // Module operates at reduced capacity
                module.ValueRW.IsActive = module.ValueRW.Durability > 0.1f;
            }

            // Module breaks at 0 durability
            if (module.ValueRW.Durability <= 0f)
            {
                module.ValueRW.IsActive = false;
                SystemAPI.SetComponentEnabled<BrokenModuleTag>(module.ValueRO.ParentChassis, true);
            }
        }
    }
}
```

---

**SwarmControlSystem** (1 Hz):
```csharp
public partial struct SwarmControlSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (swarmControl, controller) in SystemAPI.Query<RefRW<SwarmControl>, RefRO<PilotControl>>())
        {
            int activeCount = swarmControl.ValueRO.ControlledMechs.Length;

            // Calculate average construct complexity
            float avgComplexity = CalculateAverageComplexity(swarmControl.ValueRO.ControlledMechs);

            // Calculate swarm efficiency
            float efficiency = CalculateSwarmEfficiency(
                controller.ValueRO.SkillStat,
                swarmControl.ValueRO.ControlLimit,
                activeCount,
                avgComplexity
            );

            swarmControl.ValueRW.AverageEfficiency = efficiency;

            // Apply efficiency to all controlled mechs
            for (int i = 0; i < swarmControl.ValueRO.ControlledMechs.Length; i++)
            {
                Entity mech = swarmControl.ValueRO.ControlledMechs[i];
                var mechChassis = SystemAPI.GetComponent<MechChassis>(mech);

                // Apply efficiency multiplier to all stats
                ApplyEfficiencyMultiplier(mech, efficiency);
            }

            // Apply swarm tactic bonuses
            if (swarmControl.ValueRO.ActiveTactic != SwarmTactic.None)
            {
                float tacticBonus = CalculateSwarmTacticBonus(
                    swarmControl.ValueRO.ActiveTactic,
                    activeCount,
                    efficiency
                );

                ApplySwarmTacticBonus(swarmControl.ValueRO.ControlledMechs, swarmControl.ValueRO.ActiveTactic, tacticBonus);
            }
        }
    }
}
```

---

## Conclusion

This agnostic framework provides the mathematical foundation for modular mech systems. Implementations can adapt these formulas to their specific contexts (fantasy constructs, sci-fi mechs, robotic drones) while maintaining consistent mechanics for quality/rarity progression, power management, piloting configurations, and module compatibility.

**Key Design Principles:**
1. **Multiplicative Scaling**: Quality × Rarity × Pilot Skill creates exponential power growth for endgame content
2. **Resource Management**: Power cores force tactical decision-making between burst damage and sustained combat
3. **Pilot Specialization**: Different piloting modes (solo, crew, swarm) enable diverse gameplay styles
4. **Diminishing Returns**: Addon stacking, swarm efficiency, and overweight penalties prevent unchecked optimization
5. **Component Interdependency**: Modules, power cores, chassis, and pilots must work together as a cohesive system
