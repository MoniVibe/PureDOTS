# Power Management System - Agnostic Framework

## Overview

This document provides the mathematical and algorithmic framework for dynamic power management systems in modular entities (mechs, ships, constructs). Modules can be **overcharged** beyond baseline requirements for enhanced performance, or **underpowered** to conserve resources. Power can be **redistributed** between modules dynamically, creating a **focus system** that enables tactical adaptation.

**Core Mathematical Principles:**
- Module effectiveness scales non-linearly with power allocation
- Heat and stress generation scale quadratically with power
- Failure risk increases exponentially with overcharge
- Power redistribution follows conservation of energy principles

---

## Power Allocation Mathematics

### Effectiveness Function

Module effectiveness relative to power allocation with diminishing returns:

```csharp
public static float CalculateModuleEffectiveness(float powerAllocationPercent)
{
    if (powerAllocationPercent <= 0f)
        return 0f;

    // Non-linear scaling with diminishing returns
    // Formula: E = 0.4 + (P × 0.008) - (P² × 0.00001)
    // Where E = effectiveness (0-1.8), P = power percent (0-250)

    float p = powerAllocationPercent;
    float effectiveness = 0.4f + (p * 0.008f) - (p * p * 0.00001f);

    return math.clamp(effectiveness, 0f, 1.8f);
}
```

**Power → Effectiveness Mapping:**
```
0%   → 0%     (disabled)
25%  → 60%    (standby)
50%  → 82%    (underpowered)
75%  → 94%    (reduced)
100% → 100%   (normal, baseline)
125% → 112%   (boosted)
150% → 140%   (overcharged)
175% → 165%   (high overcharge)
200% → 180%   (maximum safe overcharge)
225% → 180%   (capped, diminishing returns)
250% → 180%   (hard cap)
```

**Graph:**
```
Effectiveness
180% |                  ___________
     |              ____/
140% |          ___/
100% |      ___/
 60% |  ___/
  0% |_/
     +---+---+---+---+---+---+---+
     0  50 100 150 200 250   Power%
```

---

### Alternative Effectiveness Functions

For different balance profiles:

**1. Linear Scaling (simpler, less realistic)**
```csharp
public static float CalculateEffectivenessLinear(float powerPercent)
{
    return math.clamp(powerPercent / 100f, 0f, 1.8f);
}
```

**2. Logarithmic Scaling (harsh diminishing returns)**
```csharp
public static float CalculateEffectivenessLogarithmic(float powerPercent)
{
    if (powerPercent <= 0f) return 0f;

    float effectiveness = math.log10(powerPercent + 10f) * 0.75f;
    return math.clamp(effectiveness, 0f, 1.8f);
}
```

**3. Sigmoid Scaling (smooth curve, soft cap)**
```csharp
public static float CalculateEffectivenessSigmoid(float powerPercent)
{
    // Sigmoid function: 1 / (1 + e^(-k*(x - x0)))
    float k = 0.03f;  // Steepness
    float x0 = 100f;  // Midpoint

    float effectiveness = 1.8f / (1f + math.exp(-k * (powerPercent - x0)));
    return effectiveness;
}
```

---

## Heat Generation

Heat generation scales quadratically with power allocation:

```csharp
public static float CalculateHeatGeneration(
    float basHeatGeneration,
    float powerAllocationPercent)
{
    // Heat scales with square of power
    // Formula: H = H_base × (P / 100)²

    float powerRatio = powerAllocationPercent / 100f;
    float heatMultiplier = powerRatio * powerRatio;

    float totalHeat = baseHeatGeneration * heatMultiplier;

    return totalHeat;
}
```

**Heat Scaling Examples:**
```
Power%   Heat Multiplier   Example (100 base heat)
0%       0×                0 heat
50%      0.25×             25 heat
100%     1.0×              100 heat
150%     2.25×             225 heat
200%     4.0×              400 heat
250%     6.25×             625 heat
```

---

### Heat Capacity and Overheat

```csharp
public static float CalculateOverheatTime(
    float currentHeat,
    float maxHeatCapacity,
    float heatGenerationRate,
    float heatDissipationRate)
{
    float netHeatGain = heatGenerationRate - heatDissipationRate;

    if (netHeatGain <= 0f)
        return float.PositiveInfinity;  // Will never overheat

    float heatRemaining = maxHeatCapacity - currentHeat;
    float timeToOverheat = heatRemaining / netHeatGain;

    return timeToOverheat;
}
```

**Example:**
```
Entity heat stats:
- Current heat: 500
- Max capacity: 2,000
- Generation: 300/sec (overcharged weapons)
- Dissipation: 150/sec

Net gain: 300 - 150 = 150/sec
Remaining capacity: 2,000 - 500 = 1,500
Time to overheat: 1,500 / 150 = 10 seconds
```

---

### Overheat Performance Degradation

```csharp
public static float CalculateOverheatPenalty(
    float currentHeat,
    float maxHeatCapacity)
{
    float heatPercent = currentHeat / maxHeatCapacity;

    if (heatPercent < 0.5f)
        return 1.0f;  // No penalty below 50%

    if (heatPercent < 0.75f)
        return 0.9f;  // -10% at 50-75%

    if (heatPercent < 0.9f)
        return 0.75f;  // -25% at 75-90%

    if (heatPercent < 1.0f)
        return 0.5f;  // -50% at 90-100%

    return 0f;  // Complete shutdown at 100%
}
```

---

## Power Consumption

Power consumption scales linearly with allocation:

```csharp
public static float CalculatePowerConsumption(
    float baselinePowerDraw,
    float powerAllocationPercent)
{
    // Power draw scales linearly
    // Formula: P_consumed = P_baseline × (Allocation% / 100)

    float powerDraw = baselinePowerDraw * (powerAllocationPercent / 100f);

    return powerDraw;
}
```

**Example:**
```
Module baseline: 50 units/sec

Power%   Consumption
0%       0 units/sec
50%      25 units/sec
100%     50 units/sec
150%     75 units/sec
200%     100 units/sec
```

---

### Power Budget Calculation

```csharp
public static PowerBudgetResult CalculatePowerBudget(
    float powerGenerationRate,
    NativeArray<float> moduleConsumptionRates,
    float chassisIdleDraw)
{
    float totalConsumption = chassisIdleDraw;

    for (int i = 0; i < moduleConsumptionRates.Length; i++)
    {
        totalConsumption += moduleConsumptionRates[i];
    }

    float netBalance = powerGenerationRate - totalConsumption;
    bool isSustainable = netBalance >= 0f;

    float operationTime = float.PositiveInfinity;
    if (!isSustainable)
    {
        // Calculate how long reserves will last
        // Assumes entity has stored power capacity
        float deficit = math.abs(netBalance);
        operationTime = storedPowerCapacity / deficit;
    }

    return new PowerBudgetResult
    {
        TotalGeneration = powerGenerationRate,
        TotalConsumption = totalConsumption,
        NetBalance = netBalance,
        IsSustainable = isSustainable,
        TimeToDepletion = operationTime,
        EfficiencyRatio = powerGenerationRate / totalConsumption
    };
}
```

---

## Burnout Risk

Failure probability increases exponentially with overcharge:

```csharp
public static float CalculateBurnoutRisk(
    float powerAllocationPercent,
    Quality moduleQuality,
    float deltaTime)  // seconds
{
    if (powerAllocationPercent <= 100f)
        return 0f;  // No burnout risk at or below normal power

    // Base risk: 0.04% per percent above 100%, per minute
    float overchargeAmount = powerAllocationPercent - 100f;
    float baseRiskPerMinute = overchargeAmount * 0.04f;  // %

    // Convert to risk per second
    float baseRiskPerSecond = baseRiskPerMinute / 60f;

    // Quality modifier
    float qualityModifier = moduleQuality switch
    {
        Quality.Crude => 2.0f,           // 2× risk
        Quality.Standard => 1.0f,
        Quality.Masterwork => 0.6f,      // 40% reduction
        Quality.Legendary => 0.3f,       // 70% reduction
        Quality.Artifact => 0.05f,       // 95% reduction
        _ => 1.0f
    };

    float adjustedRisk = baseRiskPerSecond * qualityModifier * deltaTime;

    return math.clamp(adjustedRisk, 0f, 1f);
}
```

**Example:**
```
Module at 175% power, Masterwork quality, 1 second:
- Overcharge: 175 - 100 = 75%
- Base risk/min: 75 × 0.04% = 3%
- Base risk/sec: 3% / 60 = 0.05%
- Quality mod: 0.6× (Masterwork)
- Final risk: 0.05% × 0.6 × 1 sec = 0.03% per second
- Risk per minute: 0.03% × 60 = 1.8%
```

---

### Burnout Check

```csharp
public static bool CheckForBurnout(
    float burnoutRisk,
    ref Random random)
{
    float roll = random.NextFloat(0f, 1f);
    return roll < burnoutRisk;
}
```

---

### Burnout Consequences

```csharp
public static void ApplyBurnoutDamage(
    ref ModuleData module,
    ref Random random)
{
    // Module disabled for cooldown period
    module.IsActive = false;
    module.DisabledUntilTime = CurrentTime + 60f;  // 60 sec emergency cooldown

    // Durability damage
    module.Durability -= 0.3f;

    // 10% chance of permanent HP damage
    if (random.NextFloat() < 0.1f)
    {
        module.CurrentHP *= 0.7f;  // Lose 30% max HP permanently
    }
}
```

---

## Durability Degradation

Module wear scales quadratically with power:

```csharp
public static float CalculateDurabilityLoss(
    float damageReceived,
    float moduleMaxHP,
    float powerAllocationPercent,
    Quality quality)
{
    // Base degradation from damage
    float damageRatio = damageReceived / moduleMaxHP;
    float baseDegradation = damageRatio * 0.05f;  // 5% durability per 100% HP lost

    // Power stress multiplier (quadratic)
    float powerRatio = powerAllocationPercent / 100f;
    float stressMultiplier = powerRatio * powerRatio;

    // Quality resistance
    float qualityResistance = quality switch
    {
        Quality.Crude => 1.5f,
        Quality.Standard => 1.0f,
        Quality.Masterwork => 0.7f,
        Quality.Legendary => 0.4f,
        Quality.Artifact => 0.1f,
        _ => 1.0f
    };

    float totalDegradation = baseDegradation * stressMultiplier * qualityResistance;

    return totalDegradation;
}
```

**Example:**
```
Module takes 500 damage (max HP 2,000), running at 150% power, Masterwork quality:
- Damage ratio: 500 / 2,000 = 0.25
- Base degradation: 0.25 × 0.05 = 0.0125 (1.25%)
- Power multiplier: (150/100)² = 2.25
- Quality resistance: 0.7 (Masterwork)
- Total: 1.25% × 2.25 × 0.7 = 1.97% durability loss
```

---

## Power Redistribution

### Focus Preset Application

```csharp
public static void ApplyFocusPreset(
    FocusMode mode,
    NativeArray<ModulePowerState> modules)
{
    for (int i = 0; i < modules.Length; i++)
    {
        var module = modules[i];

        float newAllocation = mode switch
        {
            FocusMode.Balanced => 100f,
            FocusMode.Weapons when module.Type == ModuleType.Weapon => 150f,
            FocusMode.Weapons when module.Type == ModuleType.Shield => 50f,
            FocusMode.Defense when module.Type == ModuleType.Shield => 150f,
            FocusMode.Defense when module.Type == ModuleType.Weapon => 75f,
            FocusMode.Mobility when module.Type == ModuleType.Mobility => 150f,
            FocusMode.Mobility when module.Type == ModuleType.Weapon => 75f,
            _ => 100f
        };

        module.PowerAllocation = newAllocation;
        modules[i] = module;
    }
}
```

---

### Dynamic Power Transfer

```csharp
public static (float sourceNew, float targetNew) TransferPower(
    float sourceCurrent,
    float targetCurrent,
    float transferAmount,
    float sourceMinimum,
    float targetMaximum)
{
    // Ensure source doesn't go below minimum
    float availableToTransfer = math.max(0f, sourceCurrent - sourceMinimum);
    transferAmount = math.min(transferAmount, availableToTransfer);

    // Ensure target doesn't exceed maximum
    float targetRoomAvailable = targetMaximum - targetCurrent;
    transferAmount = math.min(transferAmount, targetRoomAvailable);

    float newSource = sourceCurrent - transferAmount;
    float newTarget = targetCurrent + transferAmount;

    return (newSource, newTarget);
}
```

**Example:**
```
Source module: 100% power, minimum 25%
Target module: 100% power, maximum 200%
Transfer: 50%

Available to transfer: 100 - 25 = 75%
Target room: 200 - 100 = 100%
Actual transfer: min(50, 75, 100) = 50%

Result:
- Source: 100 - 50 = 50%
- Target: 100 + 50 = 150%
```

---

### Balanced Redistribution

Redistribute power while maintaining total consumption:

```csharp
public static void RedistributePowerBalanced(
    NativeArray<ModulePowerState> modules,
    NativeArray<float> desiredAllocations,
    float totalAvailablePower)
{
    // Calculate total desired consumption
    float totalDesired = 0f;
    for (int i = 0; i < modules.Length; i++)
    {
        float desiredDraw = modules[i].BaselinePowerDraw * (desiredAllocations[i] / 100f);
        totalDesired += desiredDraw;
    }

    // If total desired exceeds available, scale down proportionally
    float scalingFactor = 1f;
    if (totalDesired > totalAvailablePower)
    {
        scalingFactor = totalAvailablePower / totalDesired;
    }

    // Apply scaled allocations
    for (int i = 0; i < modules.Length; i++)
    {
        float scaledAllocation = desiredAllocations[i] * scalingFactor;
        modules[i] = new ModulePowerState
        {
            PowerAllocation = scaledAllocation,
            // ... other fields
        };
    }
}
```

---

## Capacitor Bank System

Energy storage for burst overcharge:

### Capacitor Charge Rate

```csharp
public static float CalculateCapacitorChargeRate(
    float generationRate,
    float consumptionRate,
    float baseChargeRate)
{
    float excessPower = generationRate - consumptionRate;

    if (excessPower <= 0f)
        return 0f;  // No charging if deficit

    // Charge at minimum of excess power or base charge rate
    float actualChargeRate = math.min(excessPower, baseChargeRate);

    return actualChargeRate;
}
```

---

### Capacitor Discharge

```csharp
public static float CalculateCapacitorDischarge(
    float currentStored,
    float dischargeRate,
    float deltaTime)
{
    float dischargeAmount = dischargeRate * deltaTime;
    float actualDischarge = math.min(dischargeAmount, currentStored);

    return actualDischarge;
}
```

---

### Capacitor-Powered Overcharge

```csharp
public static float CalculateCapacitorBoostedAllocation(
    float baseAllocation,
    float capacitorDischarge,
    float moduleBaselineDraw)
{
    // Convert discharged power to allocation percentage boost
    float boostPercent = (capacitorDischarge / moduleBaselineDraw) * 100f;

    float boostedAllocation = baseAllocation + boostPercent;

    // Capacitor can push beyond normal limits (up to 300%)
    return math.clamp(boostedAllocation, 0f, 300f);
}
```

**Example:**
```
Module baseline draw: 50 units/sec
Current allocation: 150% (75 units/sec)
Capacitor discharge: 100 units/sec

Boost: (100 / 50) × 100% = 200%
Boosted allocation: 150% + 200% = 350% → clamped to 300%
```

---

## Overcharge Synergies

### Synergy Bonus Calculation

```csharp
public static float CalculateSynergyBonus(
    float module1Allocation,
    float module2Allocation,
    SynergyType synergyType,
    float baseSynergyBonus)
{
    // Both modules must be overcharged (>100%) for synergy
    if (module1Allocation <= 100f || module2Allocation <= 100f)
        return 0f;

    // Average overcharge amount
    float avgOvercharge = ((module1Allocation + module2Allocation) / 2f) - 100f;

    // Synergy scales with average overcharge
    float synergyScalar = avgOvercharge / 50f;  // Full bonus at avg 150% allocation
    synergyScalar = math.clamp(synergyScalar, 0f, 1.5f);  // Max 150% bonus at extreme overcharge

    float synergyBonus = baseSynergyBonus * synergyScalar;

    return synergyBonus;
}
```

**Example:**
```
Weapon + Targeting synergy (base +15% crit chance):
- Weapon allocation: 150%
- Targeting allocation: 150%
- Avg overcharge: ((150 + 150) / 2) - 100 = 50%
- Synergy scalar: 50 / 50 = 1.0
- Final bonus: 15% × 1.0 = +15% crit chance

Higher overcharge:
- Weapon: 175%
- Targeting: 175%
- Avg overcharge: 75%
- Synergy scalar: 75 / 50 = 1.5 (clamped)
- Final bonus: 15% × 1.5 = +22.5% crit chance
```

---

## Automated Power Management

### Trigger Condition Evaluation

```csharp
public static bool EvaluatePowerTrigger(
    TriggerCondition condition,
    float currentValue,
    float thresholdValue,
    ComparisonType comparison)
{
    return comparison switch
    {
        ComparisonType.LessThan => currentValue < thresholdValue,
        ComparisonType.LessThanOrEqual => currentValue <= thresholdValue,
        ComparisonType.GreaterThan => currentValue > thresholdValue,
        ComparisonType.GreaterThanOrEqual => currentValue >= thresholdValue,
        ComparisonType.Equal => math.abs(currentValue - thresholdValue) < 0.01f,
        _ => false
    };
}
```

---

### AI Power Management

```csharp
public static float CalculateAIPowerAllocation(
    PowerManagementMode mode,
    ModuleType moduleType,
    float threatLevel,              // 0-1
    float resourceLevel,            // 0-1 (power/heat reserves)
    float riskTolerance)            // 0-1
{
    float baseAllocation = 100f;

    switch (mode)
    {
        case PowerManagementMode.Aggressive:
            if (moduleType == ModuleType.Weapon)
                baseAllocation = math.lerp(125f, 175f, threatLevel * riskTolerance);
            else if (moduleType == ModuleType.Shield)
                baseAllocation = math.lerp(75f, 100f, 1f - threatLevel);
            break;

        case PowerManagementMode.Defensive:
            if (moduleType == ModuleType.Shield)
                baseAllocation = math.lerp(100f, 150f, threatLevel);
            else if (moduleType == ModuleType.Weapon)
                baseAllocation = math.lerp(75f, 100f, resourceLevel);
            break;

        case PowerManagementMode.Adaptive:
            float adaptiveScale = threatLevel * riskTolerance * resourceLevel;
            if (moduleType == ModuleType.Weapon)
                baseAllocation = 100f + (adaptiveScale * 75f);
            else if (moduleType == ModuleType.Shield)
                baseAllocation = 100f + ((1f - threatLevel) * 50f);
            break;

        case PowerManagementMode.Efficiency:
            baseAllocation = math.lerp(75f, 100f, resourceLevel);
            break;
    }

    return math.clamp(baseAllocation, 0f, 200f);
}
```

**Example:**
```
Aggressive mode, high threat (0.8), low resources (0.3), medium risk tolerance (0.5):
- Weapon: lerp(125, 175, 0.8 × 0.5) = lerp(125, 175, 0.4) = 145%
- Shield: lerp(75, 100, 1 - 0.8) = lerp(75, 100, 0.2) = 80%

AI prioritizes offense despite low resources due to aggressive mode.
```

---

## Concentration Requirements

For extreme overcharge (>175%), pilot concentration check:

```csharp
public static bool CheckConcentration(
    float powerAllocation,
    int pilotSkillStat,
    int pilotSkillLevel,
    float currentStress)           // 0-1
{
    if (powerAllocation <= 150f)
        return true;  // No check required

    // Calculate DC
    int baseDC = 10;
    int overchargePenalty = (int)((powerAllocation - 150f) / 5f);
    int stressPenalty = (int)(currentStress * 10f);

    int totalDC = baseDC + overchargePenalty + stressPenalty;

    // Calculate pilot bonus
    int skillBonus = pilotSkillStat / 10;  // +1 per 10 points
    int levelBonus = pilotSkillLevel;

    int totalBonus = skillBonus + levelBonus;

    // Roll d20 + bonus vs DC
    int roll = Random.Range(1, 21);
    int result = roll + totalBonus;

    return result >= totalDC;
}
```

**Example:**
```
Pilot attempting 200% overcharge:
- Power: 200%
- Pilot INT: 85
- Pilot skill level: 12
- Current stress: 0.4 (40%)

DC calculation:
- Base: 10
- Overcharge penalty: (200 - 150) / 5 = 10
- Stress penalty: 0.4 × 10 = 4
- Total DC: 10 + 10 + 4 = 24

Pilot bonus:
- Skill: 85 / 10 = 8
- Level: 12
- Total: +20

Check: d20 + 20 vs DC 24
- Needs to roll 4+ (80% success chance)
```

---

## ECS Implementation

### Core Components

```csharp
public struct PowerSource : IComponentData
{
    public float MaxGeneration;
    public float CurrentGeneration;
    public float StoredCapacity;
    public float CurrentStored;
}

public struct ModulePowerDraw : IComponentData
{
    public Entity ParentEntity;
    public Entity ModuleEntity;
    public float BaselineDraw;
    public float CurrentAllocation;      // 0-300%
    public float ActualDraw;
}

public struct HeatState : IComponentData
{
    public float CurrentHeat;
    public float MaxHeatCapacity;
    public float PassiveDissipation;
}

public struct PowerFocus : IComponentData
{
    public FocusMode ActiveMode;
    public FixedList64Bytes<float> ModuleAllocations;
}

public struct Capacitor : IComponentData
{
    public float MaxStorage;
    public float CurrentStored;
    public float ChargeRate;
    public float DischargeRate;
    public Entity DischargeTarget;
    public bool IsDischarging;
}

public struct PowerTrigger : IComponentData
{
    public TriggerCondition Condition;
    public ComparisonType Comparison;
    public float Threshold;
    public Entity AffectedModule;
    public float NewAllocation;
}

public struct OverchargeSynergy : IComponentData
{
    public Entity Module1;
    public Entity Module2;
    public SynergyType Type;
    public float BaseBonus;
    public float CurrentBonus;
}
```

---

### Core Systems

**PowerAllocationSystem** (1 Hz):
```csharp
public partial struct PowerAllocationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var modulePower in SystemAPI.Query<RefRW<ModulePowerDraw>>())
        {
            // Calculate effectiveness
            float effectiveness = CalculateModuleEffectiveness(modulePower.ValueRO.CurrentAllocation);

            // Calculate actual draw
            float actualDraw = CalculatePowerConsumption(
                modulePower.ValueRO.BaselineDraw,
                modulePower.ValueRO.CurrentAllocation
            );

            modulePower.ValueRW.ActualDraw = actualDraw;

            // Apply effectiveness to module (different system will read this)
            ApplyEffectivenessMultiplier(modulePower.ValueRO.ModuleEntity, effectiveness);
        }
    }
}
```

**PowerBudgetSystem** (1 Hz):
```csharp
public partial struct PowerBudgetSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var powerSource in SystemAPI.Query<RefRW<PowerSource>>())
        {
            float totalDraw = CalculateTotalDraw(powerSource.ValueRO.Entity);
            float netBalance = powerSource.ValueRO.CurrentGeneration - totalDraw;

            if (netBalance < 0f)
            {
                // Drawing from reserves
                powerSource.ValueRW.CurrentStored += netBalance * SystemAPI.Time.DeltaTime;

                if (powerSource.ValueRW.CurrentStored <= 0f)
                {
                    // Emergency shutdown
                    EmergencyPowerShutdown(powerSource.ValueRO.Entity);
                }
            }
            else
            {
                // Recharging reserves
                powerSource.ValueRW.CurrentStored = math.min(
                    powerSource.ValueRW.CurrentStored + (netBalance * SystemAPI.Time.DeltaTime),
                    powerSource.ValueRO.StoredCapacity
                );
            }
        }
    }
}
```

**HeatManagementSystem** (1 Hz):
```csharp
public partial struct HeatManagementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var heatState in SystemAPI.Query<RefRW<HeatState>>())
        {
            float totalHeatGen = CalculateTotalHeatGeneration(heatState.ValueRO.Entity);
            float netHeat = totalHeatGen - heatState.ValueRO.PassiveDissipation;

            heatState.ValueRW.CurrentHeat += netHeat * SystemAPI.Time.DeltaTime;
            heatState.ValueRW.CurrentHeat = math.clamp(
                heatState.ValueRW.CurrentHeat,
                0f,
                heatState.ValueRO.MaxHeatCapacity * 1.2f  // Allow 20% over capacity before forced shutdown
            );

            // Apply overheat penalties
            if (heatState.ValueRW.CurrentHeat >= heatState.ValueRO.MaxHeatCapacity)
            {
                float penalty = CalculateOverheatPenalty(
                    heatState.ValueRW.CurrentHeat,
                    heatState.ValueRO.MaxHeatCapacity
                );

                ApplyOverheatPenalty(heatState.ValueRO.Entity, penalty);
            }
        }
    }
}
```

**BurnoutSystem** (1 Hz):
```csharp
public partial struct BurnoutSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var random = new Random((uint)System.DateTime.Now.Ticks);

        foreach (var (modulePower, module) in SystemAPI.Query<RefRO<ModulePowerDraw>, RefRW<ModuleData>>())
        {
            if (modulePower.ValueRO.CurrentAllocation > 100f)
            {
                float burnoutRisk = CalculateBurnoutRisk(
                    modulePower.ValueRO.CurrentAllocation,
                    module.ValueRO.Quality,
                    SystemAPI.Time.DeltaTime
                );

                if (CheckForBurnout(burnoutRisk, ref random))
                {
                    ApplyBurnoutDamage(ref module.ValueRW, ref random);
                }
            }
        }
    }
}
```

---

## Conclusion

This agnostic framework provides the mathematical foundation for dynamic power management systems. The non-linear effectiveness scaling, quadratic heat generation, and exponential failure risks create meaningful trade-offs between performance, sustainability, and reliability. Implementations can tune these formulas to match their desired balance between tactical depth and accessibility.

**Key Design Principles:**
1. **Diminishing Returns**: Overcharge provides meaningful but limited benefits
2. **Quadratic Costs**: Heat and stress scale faster than benefits
3. **Risk Management**: High performance requires accepting failure probability
4. **Dynamic Adaptation**: Power redistribution enables real-time tactical shifts
5. **Resource Conservation**: Balancing burst performance vs sustained operation
