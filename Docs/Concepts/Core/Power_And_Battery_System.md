# Power and Battery System

**Last Updated:** 2025-12-18
**Status:** Design Document - Power Generation, Distribution, and Storage
**Burst Compatible:** Yes
**Deterministic:** Yes
**Entity-Agnostic:** Yes (works for ships, mechs, buildings, stations)

---

## Overview

The **Power and Battery System** provides a consistent, technically grounded energy economy for all powered entities. Power flows from **generators** through **distribution** (with losses) to **consumers**, with **batteries** providing buffering and surge capacity. High-power systems (weapons, shields) require **local power banks** to handle instantaneous demand spikes that exceed reactor output.

**Core Principles:**
- **Generation**: Reactors/generators produce steady power with efficiency losses
- **Distribution**: Power transmission has resistance losses (5-15% depending on tech)
- **Storage**: Batteries discharge passively at a fixed rate (self-discharge)
- **Buffering**: Weapon and shield systems require power banks for burst operation
- **Tech Progression**: Unlocks reduce losses, increase capacity, improve efficiency

---

## Power Generation

### Reactor Output

All reactors produce steady power with inherent efficiency losses:

```csharp
public struct PowerGenerator : IComponentData
{
    /// <summary>
    /// Maximum theoretical output (before efficiency losses)
    /// </summary>
    public float TheoreticalMaxOutput;  // MW

    /// <summary>
    /// Current operating output (0-1 multiplier of theoretical)
    /// </summary>
    public float CurrentOutputPercent;

    /// <summary>
    /// Efficiency rating (0-1, how much theoretical becomes actual)
    /// Tech Level 1: 0.70 (70% efficient, 30% waste heat)
    /// Tech Level 5: 0.85 (85% efficient)
    /// Tech Level 10: 0.92 (92% efficient)
    /// Tech Level 15: 0.97 (97% efficient, near-perfect)
    /// </summary>
    public float Efficiency;

    /// <summary>
    /// Degradation factor (reduces efficiency over time, requires maintenance)
    /// 0 = pristine, 1 = needs overhaul
    /// </summary>
    public float DegradationLevel;

    /// <summary>
    /// Heat generated as waste (MW)
    /// </summary>
    public float WasteHeat;

    /// <summary>
    /// Tech level unlocks affecting this generator
    /// </summary>
    public byte TechLevel;
}
```

### Actual Power Output Calculation

```csharp
[BurstCompile]
public static float CalculateActualOutput(
    float theoreticalMax,
    float currentOutputPercent,
    float baseEfficiency,
    float degradationLevel)
{
    // Operating output
    float operatingOutput = theoreticalMax * currentOutputPercent;

    // Degradation penalty (0-25% efficiency loss)
    float degradationPenalty = degradationLevel * 0.25f;
    float actualEfficiency = baseEfficiency * (1f - degradationPenalty);

    // Actual usable power
    float actualOutput = operatingOutput * actualEfficiency;

    // Waste heat = lost power
    float wasteHeat = operatingOutput * (1f - actualEfficiency);

    return actualOutput;
}
```

**Example:**

```
Fusion Reactor (Tech Level 5):
- Theoretical max: 2,000 MW
- Operating at: 80% (1,600 MW theoretical)
- Base efficiency: 0.85 (85%)
- Degradation: 0.20 (20% worn, needs maintenance soon)

Degradation penalty: 0.20 × 0.25 = 0.05 (5% efficiency loss)
Actual efficiency: 0.85 × (1 - 0.05) = 0.8075 (80.75%)
Actual output: 1,600 × 0.8075 = 1,292 MW
Waste heat: 1,600 - 1,292 = 308 MW

Result: Only 1,292 MW available for use, 308 MW becomes heat
```

### Tech Level Efficiency Progression

```csharp
public static float GetReactorEfficiency(byte techLevel)
{
    return techLevel switch
    {
        1 => 0.70f,   // Early fusion, crude
        2 => 0.74f,
        3 => 0.78f,
        4 => 0.82f,
        5 => 0.85f,   // Standard fusion
        6 => 0.88f,
        7 => 0.90f,
        8 => 0.92f,
        9 => 0.94f,
        10 => 0.95f,  // Advanced fusion
        11 => 0.96f,
        12 => 0.97f,  // Antimatter begins
        13 => 0.98f,
        14 => 0.985f,
        15 => 0.99f,  // Near-perfect conversion
        _ => 0.70f    // Default
    };
}
```

---

## Power Distribution Losses

Power transmitted through conduits experiences resistance losses:

```csharp
public struct PowerDistribution : IComponentData
{
    /// <summary>
    /// Power entering distribution system (from generators)
    /// </summary>
    public float InputPower;  // MW

    /// <summary>
    /// Power lost to resistance (heat)
    /// </summary>
    public float TransmissionLoss;  // MW

    /// <summary>
    /// Power available to consumers (after losses)
    /// </summary>
    public float OutputPower;  // MW

    /// <summary>
    /// Distribution efficiency (0-1)
    /// Tech Level 1: 0.85 (15% transmission loss)
    /// Tech Level 5: 0.92 (8% loss)
    /// Tech Level 10: 0.96 (4% loss)
    /// Tech Level 15: 0.98 (2% loss, superconductors)
    /// </summary>
    public float DistributionEfficiency;

    /// <summary>
    /// Damage to power conduits reduces efficiency
    /// </summary>
    public float ConduitDamage;  // 0-1

    /// <summary>
    /// Tech level for distribution system
    /// </summary>
    public byte TechLevel;
}
```

### Distribution Calculation

```csharp
[BurstCompile]
public static PowerDistributionResult CalculateDistribution(
    float inputPower,
    float baseEfficiency,
    float conduitDamage)
{
    // Damage reduces efficiency
    float damagePenalty = conduitDamage * 0.20f;  // Up to 20% additional loss
    float actualEfficiency = baseEfficiency * (1f - damagePenalty);

    // Output power
    float outputPower = inputPower * actualEfficiency;

    // Transmission loss becomes heat
    float transmissionLoss = inputPower - outputPower;

    return new PowerDistributionResult
    {
        OutputPower = outputPower,
        TransmissionLoss = transmissionLoss,
        ActualEfficiency = actualEfficiency
    };
}
```

**Example:**

```
Ship Power Distribution (Tech Level 8):
- Reactor output: 1,292 MW (from previous example)
- Base distribution efficiency: 0.92 (92%)
- Conduit damage: 0.10 (10% damaged from combat)

Damage penalty: 0.10 × 0.20 = 0.02 (2% additional loss)
Actual efficiency: 0.92 × (1 - 0.02) = 0.9016 (90.16%)
Output power: 1,292 × 0.9016 = 1,165 MW
Transmission loss: 1,292 - 1,165 = 127 MW (becomes heat)

Result: 1,165 MW available to consumers
Total losses: 308 MW (generation) + 127 MW (distribution) = 435 MW lost
Overall efficiency: 1,165 / 2,000 = 58.25% (from theoretical max to usable)
```

### Tech Level Distribution Efficiency

```csharp
public static float GetDistributionEfficiency(byte techLevel)
{
    return techLevel switch
    {
        1 => 0.85f,   // Copper conductors, inefficient
        2 => 0.87f,
        3 => 0.89f,
        4 => 0.90f,
        5 => 0.92f,   // Improved materials
        6 => 0.93f,
        7 => 0.94f,
        8 => 0.95f,
        9 => 0.96f,
        10 => 0.97f,  // Low-temp superconductors
        11 => 0.975f,
        12 => 0.98f,
        13 => 0.985f,
        14 => 0.99f,
        15 => 0.995f, // Room-temp superconductors
        _ => 0.85f
    };
}
```

---

## Battery System

### Battery Components

```csharp
public struct PowerBattery : IComponentData
{
    /// <summary>
    /// Maximum energy storage capacity (MW·s = megawatt-seconds)
    /// </summary>
    public float MaxCapacity;

    /// <summary>
    /// Current stored energy (MW·s)
    /// </summary>
    public float CurrentStored;

    /// <summary>
    /// Maximum charge rate (MW)
    /// </summary>
    public float MaxChargeRate;

    /// <summary>
    /// Maximum discharge rate (MW)
    /// </summary>
    public float MaxDischargeRate;

    /// <summary>
    /// Passive self-discharge rate (% per second)
    /// Tech Level 1: 0.001 (0.1% per second = 6% per minute)
    /// Tech Level 5: 0.0005 (0.05% per second = 3% per minute)
    /// Tech Level 10: 0.0002 (0.02% per second = 1.2% per minute)
    /// Tech Level 15: 0.00005 (0.005% per second = 0.3% per minute)
    /// </summary>
    public float SelfDischargeRate;

    /// <summary>
    /// Charge efficiency (0-1, how much input power actually stores)
    /// </summary>
    public float ChargeEfficiency;

    /// <summary>
    /// Discharge efficiency (0-1, how much stored power actually outputs)
    /// </summary>
    public float DischargeEfficiency;

    /// <summary>
    /// Battery health (0-1, degrades with charge cycles)
    /// </summary>
    public float Health;

    /// <summary>
    /// Total charge/discharge cycles completed
    /// </summary>
    public int CycleCount;

    /// <summary>
    /// Max cycles before significant degradation
    /// </summary>
    public int MaxCycles;

    /// <summary>
    /// Tech level
    /// </summary>
    public byte TechLevel;
}
```

### Passive Battery Discharge (Self-Discharge)

Batteries lose charge over time even when idle:

```csharp
[BurstCompile]
public static float CalculateSelfDischarge(
    float currentStored,
    float selfDischargeRate,
    float deltaTime)
{
    // Self-discharge is percentage of current charge
    float dischargeAmount = currentStored * selfDischargeRate * deltaTime;

    return dischargeAmount;
}
```

**Example:**

```
Battery:
- Current stored: 5,000 MW·s
- Self-discharge rate: 0.0005 (0.05% per second)
- Delta time: 1 second

Discharge: 5,000 × 0.0005 × 1 = 2.5 MW·s lost
New stored: 5,000 - 2.5 = 4,997.5 MW·s

Over 1 minute (60 seconds):
Discharge: 5,000 × 0.0005 × 60 = 150 MW·s lost (3% of capacity)
New stored: 5,000 - 150 = 4,850 MW·s

Over 1 hour (3,600 seconds):
Discharge: 5,000 × 0.0005 × 3,600 = 9,000 MW·s
But capped at current stored, so battery fully drains in:
Time to drain: 5,000 / (5,000 × 0.0005) = 2,000 seconds ≈ 33.3 minutes
```

### Battery Charging

```csharp
[BurstCompile]
public static BatteryChargeResult ChargeB attery(
    float currentStored,
    float maxCapacity,
    float maxChargeRate,
    float chargeEfficiency,
    float availablePower,
    float deltaTime)
{
    // How much room in battery
    float roomAvailable = maxCapacity - currentStored;

    // How much can charge in this tick
    float maxChargeThisTick = maxChargeRate * deltaTime;

    // Limited by available power
    float chargeAmount = math.min(maxChargeThisTick, availablePower * deltaTime);

    // Limited by room available
    chargeAmount = math.min(chargeAmount, roomAvailable);

    // Apply efficiency (some power lost as heat during charging)
    float actualStored = chargeAmount * chargeEfficiency;
    float chargeLoss = chargeAmount - actualStored;

    float newStored = currentStored + actualStored;

    return new BatteryChargeResult
    {
        NewStored = newStored,
        PowerConsumed = chargeAmount / deltaTime,  // MW
        ChargeLoss = chargeLoss,
        ChargedAmount = actualStored
    };
}
```

**Example:**

```
Battery (Tech Level 5):
- Current: 2,000 MW·s
- Max capacity: 10,000 MW·s
- Max charge rate: 500 MW
- Charge efficiency: 0.95 (95%)
- Available power: 600 MW
- Delta time: 1 second

Room available: 10,000 - 2,000 = 8,000 MW·s
Max charge this tick: 500 × 1 = 500 MW·s
Limited by power: min(500, 600 × 1) = 500 MW·s
Actual stored: 500 × 0.95 = 475 MW·s
Charge loss: 500 - 475 = 25 MW·s (becomes heat)
New stored: 2,000 + 475 = 2,475 MW·s

Power consumed: 500 MW
```

### Battery Discharging

```csharp
[BurstCompile]
public static BatteryDischargeResult DischargeBattery(
    float currentStored,
    float maxDischargeRate,
    float dischargeEfficiency,
    float requestedPower,
    float deltaTime)
{
    // How much can discharge in this tick
    float maxDischargeThisTick = maxDischargeRate * deltaTime;

    // Limited by requested power
    float dischargeAmount = math.min(maxDischargeThisTick, requestedPower * deltaTime);

    // Limited by stored energy
    dischargeAmount = math.min(dischargeAmount, currentStored);

    // Apply efficiency (some power lost during discharge)
    float actualOutput = dischargeAmount * dischargeEfficiency;
    float dischargeLoss = dischargeAmount - actualOutput;

    float newStored = currentStored - dischargeAmount;

    return new BatteryDischargeResult
    {
        NewStored = newStored,
        PowerDelivered = actualOutput / deltaTime,  // MW
        DischargeLoss = dischargeLoss,
        DischargedAmount = dischargeAmount
    };
}
```

**Example:**

```
Battery:
- Current: 5,000 MW·s
- Max discharge rate: 2,000 MW
- Discharge efficiency: 0.93 (93%)
- Requested power: 1,500 MW
- Delta time: 1 second

Max discharge this tick: 2,000 × 1 = 2,000 MW·s
Limited by request: min(2,000, 1,500 × 1) = 1,500 MW·s
Actual output: 1,500 × 0.93 = 1,395 MW
Discharge loss: 1,500 - 1,395 = 105 MW (becomes heat)
New stored: 5,000 - 1,500 = 3,500 MW·s

Power delivered: 1,395 MW (not quite the requested 1,500 MW due to efficiency)
```

### Battery Degradation

Batteries degrade with charge cycles:

```csharp
[BurstCompile]
public static float CalculateBatteryHealth(
    int currentCycles,
    int maxCycles)
{
    if (currentCycles < maxCycles)
        return 1.0f;  // Full health

    // Linear degradation after max cycles
    float excessCycles = currentCycles - maxCycles;
    float degradation = excessCycles / (maxCycles * 2f);  // Degrades to 50% after 2× max cycles

    float health = 1.0f - degradation;

    return math.clamp(health, 0.3f, 1.0f);  // Never below 30% capacity
}
```

**Degradation effects:**

```csharp
// Apply health to capacity and efficiency
float actualCapacity = maxCapacity * health;
float actualChargeEfficiency = baseChargeEfficiency * health;
float actualDischargeEfficiency = baseDischargeEfficiency * health;
```

**Example:**

```
Battery:
- Max cycles: 5,000
- Current cycles: 7,500 (exceeded by 2,500)

Health: 1.0 - (2,500 / 10,000) = 1.0 - 0.25 = 0.75 (75% health)

Effects:
- Max capacity: 10,000 × 0.75 = 7,500 MW·s (down from 10,000)
- Charge efficiency: 0.95 × 0.75 = 0.7125 (down from 95%)
- Discharge efficiency: 0.93 × 0.75 = 0.6975 (down from 93%)

Result: Battery still works but significantly degraded, needs replacement
```

### Tech Level Battery Progression

```csharp
public static BatteryTechStats GetBatteryTechStats(byte techLevel)
{
    return techLevel switch
    {
        1 => new BatteryTechStats
        {
            CapacityMultiplier = 1.0f,
            SelfDischargeRate = 0.001f,      // 0.1%/sec
            ChargeEfficiency = 0.85f,
            DischargeEfficiency = 0.83f,
            MaxCycles = 2000
        },
        5 => new BatteryTechStats
        {
            CapacityMultiplier = 1.5f,
            SelfDischargeRate = 0.0005f,     // 0.05%/sec
            ChargeEfficiency = 0.92f,
            DischargeEfficiency = 0.90f,
            MaxCycles = 5000
        },
        10 => new BatteryTechStats
        {
            CapacityMultiplier = 2.5f,
            SelfDischargeRate = 0.0002f,     // 0.02%/sec
            ChargeEfficiency = 0.96f,
            DischargeEfficiency = 0.95f,
            MaxCycles = 10000
        },
        15 => new BatteryTechStats
        {
            CapacityMultiplier = 4.0f,
            SelfDischargeRate = 0.00005f,    // 0.005%/sec
            ChargeEfficiency = 0.99f,
            DischargeEfficiency = 0.98f,
            MaxCycles = 50000
        },
        _ => GetBatteryTechStats(1)
    };
}
```

---

## Weapon and Shield Power Banks

High-power systems require **local power banks** (buffers) to handle instantaneous demand:

### Power Bank Requirement

```csharp
public struct PowerBankRequirement : IComponentData
{
    /// <summary>
    /// Minimum power bank capacity required for this system to operate
    /// </summary>
    public float MinimumBankCapacity;  // MW·s

    /// <summary>
    /// Recommended capacity for optimal performance
    /// </summary>
    public float RecommendedCapacity;  // MW·s

    /// <summary>
    /// Linked power bank entity
    /// </summary>
    public Entity PowerBank;

    /// <summary>
    /// Whether this system can operate without a power bank
    /// (reduced performance mode)
    /// </summary>
    public bool CanOperateWithoutBank;

    /// <summary>
    /// Performance penalty without adequate power bank (0-1)
    /// </summary>
    public float NoBankPenalty;
}

public struct WeaponPowerDemand : IComponentData
{
    /// <summary>
    /// Power required per shot (MW·s)
    /// </summary>
    public float PowerPerShot;

    /// <summary>
    /// Rate of fire (shots per second)
    /// </summary>
    public float RateOfFire;

    /// <summary>
    /// Peak instantaneous power draw (MW)
    /// </summary>
    public float PeakDraw;

    /// <summary>
    /// Average power draw sustained (MW)
    /// </summary>
    public float AverageDraw;

    /// <summary>
    /// Required power bank
    /// </summary>
    public PowerBankRequirement BankRequirement;
}

public struct ShieldPowerDemand : IComponentData
{
    /// <summary>
    /// Sustained power for shield maintenance (MW)
    /// </summary>
    public float SustainedDraw;

    /// <summary>
    /// Peak power when absorbing damage (MW)
    /// </summary>
    public float PeakDraw;

    /// <summary>
    /// Power per damage absorbed (MW·s per point)
    /// </summary>
    public float PowerPerDamage;

    /// <summary>
    /// Recharge power requirement (MW)
    /// </summary>
    public float RechargeDraw;

    /// <summary>
    /// Required power bank
    /// </summary>
    public PowerBankRequirement BankRequirement;
}
```

### Why Power Banks Are Required

**Weapons:**

Railguns, particle beams, and energy weapons have **instantaneous power demands** that far exceed reactor output:

```
Example: Heavy Railgun
- Damage per shot: 2,500
- Power per shot: 1,800 MW·s
- Rate of fire: 1 shot per 3 seconds
- Peak draw: 1,800 MW (instant)
- Average draw: 1,800 / 3 = 600 MW

Ship reactor: 2,000 MW

WITHOUT Power Bank:
- Weapon tries to draw 1,800 MW instantly
- Reactor can only provide 2,000 MW total
- All other systems (shields, engines, life support) starved
- Weapon fires at reduced power (damage: 1,388 instead of 2,500)
- Fire rate limited to reactor recharge (every 5 seconds instead of 3)

WITH Power Bank (5,000 MW·s capacity):
- Power bank pre-charges from reactor (600 MW steady)
- When firing, weapon draws from bank (1,800 MW·s instantly)
- Reactor continues powering other systems
- Bank recharges between shots (3 seconds × 600 MW = 1,800 MW·s, full recharge)
- Full damage, full rate of fire
```

**Shields:**

Energy shields have **variable demand** based on incoming damage:

```
Example: Energy Shield
- Sustained maintenance: 300 MW
- Power per damage point: 0.5 MW·s
- Incoming burst: 4,000 damage in 1 second
- Peak draw: 300 + (4,000 × 0.5) = 2,300 MW

Ship reactor: 2,000 MW

WITHOUT Power Bank:
- Shield tries to draw 2,300 MW
- Reactor limited to 2,000 MW
- Shield underpowered by 300 MW (600 damage points)
- Shield absorbs: 4,000 - 600 = 3,400 damage (85% effectiveness)
- Remaining 600 damage bleeds through to hull

WITH Power Bank (8,000 MW·s capacity):
- Bank provides surge power (2,300 MW for 1 second = 2,300 MW·s)
- Shield absorbs full 4,000 damage
- Bank depleted by 2,300 MW·s
- Reactor recharges bank steadily (300 MW surplus)
```

### Power Bank Sizing

```csharp
[BurstCompile]
public static float CalculateRequiredBankCapacity(
    float peakPowerDraw,     // MW
    float averagePowerDraw,  // MW
    float reactorOutput,     // MW
    float burstDuration)     // seconds
{
    // How much power reactor can't provide during burst
    float deficit = peakPowerDraw - reactorOutput;

    if (deficit <= 0f)
        return 0f;  // Reactor can handle peak, no bank needed

    // Capacity needed to cover deficit for burst duration
    float requiredCapacity = deficit * burstDuration;

    // Add 20% safety margin
    return requiredCapacity * 1.2f;
}
```

**Example:**

```
Weapon:
- Peak draw: 1,800 MW
- Average draw: 600 MW
- Burst duration: 0.1 sec (firing time)
- Reactor output: 2,000 MW

Deficit: 1,800 - 2,000 = -200 MW (reactor can handle!)
Required bank: 0 MW·s

BUT if reactor only 1,000 MW:
Deficit: 1,800 - 1,000 = 800 MW
Required capacity: 800 × 0.1 = 80 MW·s
With margin: 80 × 1.2 = 96 MW·s

Result: Need at least 100 MW·s power bank
```

### Power Bank Assignment System

```csharp
[BurstCompile]
public partial struct PowerBankAssignmentSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Find weapons/shields without assigned power banks
        foreach (var (weaponDemand, entity) in
            SystemAPI.Query<RefRW<WeaponPowerDemand>>()
            .WithEntityAccess()
            .WithNone<AssignedPowerBank>())
        {
            // Find suitable power bank on parent entity
            Entity parentEntity = state.EntityManager.GetComponentData<Parent>(entity).Value;

            if (!state.EntityManager.HasBuffer<PowerBattery>(parentEntity))
            {
                // No power banks available - weapon operates at reduced capacity
                weaponDemand.ValueRW.BankRequirement.CanOperateWithoutBank = true;
                weaponDemand.ValueRW.BankRequirement.NoBankPenalty = 0.4f;  // 40% damage reduction
                continue;
            }

            var batteries = state.EntityManager.GetBuffer<PowerBattery>(parentEntity);

            // Find first battery with sufficient capacity
            for (int i = 0; i < batteries.Length; i++)
            {
                if (batteries[i].MaxCapacity >= weaponDemand.ValueRO.BankRequirement.MinimumBankCapacity)
                {
                    weaponDemand.ValueRW.BankRequirement.PowerBank = parentEntity;  // Reference parent's battery
                    ecb.AddComponent<AssignedPowerBank>(entity);
                    break;
                }
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

/// <summary>
/// Tag component for weapons/shields with assigned power banks
/// </summary>
public struct AssignedPowerBank : IComponentData { }
```

### Weapon Firing with Power Bank

```csharp
[BurstCompile]
public partial struct WeaponFireSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (weaponDemand, weaponState, entity) in
            SystemAPI.Query<
                RefRO<WeaponPowerDemand>,
                RefRW<WeaponState>>()
            .WithEntityAccess()
            .WithAll<FireCommand>())
        {
            // Check if weapon has power bank
            if (weaponDemand.ValueRO.BankRequirement.PowerBank == Entity.Null)
            {
                // No power bank - fire at reduced power
                float damage = weaponState.ValueRO.BaseDamage * (1f - weaponDemand.ValueRO.BankRequirement.NoBankPenalty);
                FireWeapon(entity, damage, ecb);
                continue;
            }

            // Get power bank
            var battery = state.EntityManager.GetComponentData<PowerBattery>(
                weaponDemand.ValueRO.BankRequirement.PowerBank);

            // Check if bank has enough energy
            if (battery.CurrentStored >= weaponDemand.ValueRO.PowerPerShot)
            {
                // Discharge power from bank
                var dischargeResult = DischargeBattery(
                    battery.CurrentStored,
                    battery.MaxDischargeRate,
                    battery.DischargeEfficiency,
                    weaponDemand.ValueRO.PowerPerShot / state.Time.DeltaTime,  // Convert to MW
                    state.Time.DeltaTime);

                battery.CurrentStored = dischargeResult.NewStored;
                state.EntityManager.SetComponentData(weaponDemand.ValueRO.BankRequirement.PowerBank, battery);

                // Fire at full power
                FireWeapon(entity, weaponState.ValueRO.BaseDamage, ecb);
            }
            else
            {
                // Insufficient charge - cannot fire or fire at reduced power
                float availablePercent = battery.CurrentStored / weaponDemand.ValueRO.PowerPerShot;
                float damage = weaponState.ValueRO.BaseDamage * availablePercent;

                if (availablePercent > 0.3f)  // Minimum 30% power to fire
                {
                    FireWeapon(entity, damage, ecb);

                    // Discharge all available
                    battery.CurrentStored = 0;
                    state.EntityManager.SetComponentData(weaponDemand.ValueRO.BankRequirement.PowerBank, battery);
                }
                else
                {
                    // Weapon offline - insufficient power
                    weaponState.ValueRW.Status = WeaponStatus.PowerStarved;
                }
            }

            // Remove fire command
            ecb.RemoveComponent<FireCommand>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

---

## Complete Power Flow Example

**Battlecruiser "Resolute"** (Tech Level 8):

### Ship Configuration

**Power Generation:**
- Heavy Fusion Reactor (Tech 8): 10,000 MW theoretical
  - Efficiency: 0.92 (92%)
  - Degradation: 0.15 (15% worn)
  - Actual output: `10,000 × 0.92 × (1 - 0.15×0.25) = 10,000 × 0.92 × 0.9625 = 8,855 MW`
  - Waste heat: 1,145 MW

**Power Distribution:**
- Distribution efficiency (Tech 8): 0.95 (95%)
- Conduit damage: 0.05 (5%)
- Actual efficiency: `0.95 × (1 - 0.05×0.20) = 0.95 × 0.99 = 0.9405 (94.05%)`
- Distributed power: `8,855 × 0.9405 = 8,328 MW`
- Transmission loss: 527 MW

**Batteries (Power Banks):**
- Primary Bank: 50,000 MW·s capacity
  - Tech Level 8
  - Charge efficiency: 0.94
  - Discharge efficiency: 0.92
  - Self-discharge: 0.0003/sec (0.03%/sec)
  - Current stored: 45,000 MW·s (90% full)

**Power Consumers:**
- Life Support: 500 MW (constant)
- Engines (idle): 800 MW
- Sensors: 400 MW
- 4× Particle Cannons (standby): 200 MW each = 800 MW
- 2× Shield Arrays (idle): 600 MW each = 1,200 MW

**Total steady draw:** 500 + 800 + 400 + 800 + 1,200 = 3,700 MW

**Surplus power:** 8,328 - 3,700 = 4,628 MW (charging batteries)

### Combat Engagement Timeline

**T=0 sec: Idle state**
- Power balance: +4,628 MW surplus
- Battery charging: 4,628 × 0.94 = 4,350 MW effective (4,350 MW·s/sec)
- Battery self-discharge: 45,000 × 0.0003 = 13.5 MW·s/sec
- Net battery gain: 4,350 - 13.5 = 4,336.5 MW·s/sec
- Battery at 45,000 MW·s, filling slowly

**T=10 sec: Combat initiated, shields activate**
- Shields go from idle (600 MW each) to active (1,500 MW each)
- New shield draw: 3,000 MW (up from 1,200 MW)
- Total draw: 3,700 - 1,200 + 3,000 = 5,500 MW
- Power balance: 8,328 - 5,500 = +2,828 MW surplus (still positive)
- Battery still charging at 2,828 × 0.94 - 13.5 = 2,644.8 MW·s/sec

**T=15 sec: First weapon volley (all 4 cannons)**
- Power per shot: 2,500 MW·s each × 4 = 10,000 MW·s total
- Shot duration: 0.2 seconds (burst)
- Peak power: 10,000 / 0.2 = 50,000 MW (FAR exceeds reactor!)

**Without Power Bank:**
- Reactor: 8,328 MW
- Weapons need: 50,000 MW
- Deficit: 41,672 MW
- Weapons fire at: 8,328 / 50,000 = 16.6% power
- Damage reduced to 16.6% of normal

**With Power Bank:**
- Weapons draw from battery: 10,000 MW·s
- Battery discharge efficiency: 0.92
- Actual drawn: 10,000 / 0.92 = 10,870 MW·s
- Battery: 45,000 - 10,870 = 34,130 MW·s remaining
- Weapons fire at full power
- Reactor continues powering shields/engines/sensors

**T=16 sec: Weapons recharging**
- Weapons in cooldown (3 seconds)
- Reactor surplus: 2,828 MW
- Battery recharge: 2,828 × 0.94 × 3 sec = 7,967 MW·s
- Battery: 34,130 + 7,967 - (13.5 × 3) = 42,056.5 MW·s (recovered 78%)

**T=19 sec: Second weapon volley**
- Battery: 42,056 MW·s
- Weapons draw: 10,870 MW·s
- Battery: 42,056 - 10,870 = 31,186 MW·s
- Full power maintained

**T=22 sec: Shield surge (heavy incoming fire)**
- Incoming damage: 15,000 in 2 seconds
- Shield power per damage: 0.6 MW·s per point
- Surge power: 15,000 × 0.6 = 9,000 MW·s
- Peak shield draw: 9,000 / 2 = 4,500 MW (instant)
- Sustained shield draw: 1,500 MW × 2 = 3,000 MW
- Total shield demand: 4,500 + 3,000 = 7,500 MW peak

**Power Bank handles surge:**
- Reactor provides: 3,000 MW sustained
- Battery provides: 4,500 MW peak
- Battery drain: 4,500 × 2 sec = 9,000 MW·s
- Battery: 31,186 - 9,000 = 22,186 MW·s (48% remaining)
- Shields fully powered, all damage absorbed

**T=30 sec: Battery critically low, tactical decision**

Options:

**A) Continue fighting (risk power starvation)**
- Battery: 22,186 MW·s (44%)
- Can fire 2 more volleys before depletion
- If depleted, weapons fire at 16.6% power (useless)

**B) Reduce weapon fire rate (conserve battery)**
- Fire 2 cannons per volley instead of 4
- Battery drain: 5,000 MW·s per volley (50% reduction)
- Can sustain 4 volleys before depletion
- Damage output: 50% normal

**C) Disengage shields temporarily (recharge battery)**
- Shields to standby: 1,200 MW
- New surplus: 8,328 - 3,900 = 4,428 MW
- Battery recharge: 4,428 × 0.94 = 4,162 MW·s/sec
- 10 seconds = 41,620 MW·s recovered
- Risk: Hull exposed to fire

**D) Emergency reactor overcharge (boost output)**
- Reactor to 120% power
- Theoretical output: 12,000 MW
- Actual: 12,000 × 0.92 × 0.9625 = 10,626 MW
- After distribution: 10,626 × 0.9405 = 9,994 MW
- New surplus: 9,994 - 5,500 = 4,494 MW
- Battery recharge: 4,494 × 0.94 = 4,224 MW·s/sec
- Risk: 0.01% meltdown risk per hour, increased wear

---

## Tech Unlocks

### Power System Tech Tree

**Tech Level 1-3: Early Industrial**
- Reactor efficiency: 70-78%
- Distribution efficiency: 85-89%
- Battery self-discharge: 0.1%/sec
- Battery cycles: 2,000-3,000

**Tech Level 4-6: Standard Fusion**
- Reactor efficiency: 82-88%
- Distribution efficiency: 90-93%
- Battery self-discharge: 0.05-0.07%/sec
- Battery cycles: 4,000-6,000
- **Unlock:** Power bank systems (weapon buffers)

**Tech Level 7-9: Advanced Fusion**
- Reactor efficiency: 90-94%
- Distribution efficiency: 94-96%
- Battery self-discharge: 0.02-0.04%/sec
- Battery cycles: 8,000-12,000
- **Unlock:** Shield power banks, capacitor surge

**Tech Level 10-12: Antimatter Transition**
- Reactor efficiency: 95-97%
- Distribution efficiency: 97-98%
- Battery self-discharge: 0.01-0.015%/sec
- Battery cycles: 15,000-25,000
- **Unlock:** Antimatter reactors, ultra-capacitors

**Tech Level 13-15: Post-Scarcity**
- Reactor efficiency: 98-99%
- Distribution efficiency: 98.5-99.5%
- Battery self-discharge: 0.005-0.01%/sec
- Battery cycles: 35,000-50,000
- **Unlock:** Zero-point energy, quantum batteries

### Tech Unlock Effects

```csharp
public struct PowerTechUnlocks : IComponentData
{
    public byte ReactorTechLevel;
    public byte DistributionTechLevel;
    public byte BatteryTechLevel;

    public bool HasPowerBankUnlock;           // Tech 4+
    public bool HasShieldBankUnlock;          // Tech 7+
    public bool HasAntimatterUnlock;          // Tech 10+
    public bool HasQuantumBatteryUnlock;      // Tech 13+
}

[BurstCompile]
public static void ApplyTechUnlocks(
    ref PowerGenerator generator,
    ref PowerDistribution distribution,
    ref PowerBattery battery,
    PowerTechUnlocks unlocks)
{
    // Apply tech level improvements
    generator.Efficiency = GetReactorEfficiency(unlocks.ReactorTechLevel);
    generator.TechLevel = unlocks.ReactorTechLevel;

    distribution.DistributionEfficiency = GetDistributionEfficiency(unlocks.DistributionTechLevel);
    distribution.TechLevel = unlocks.DistributionTechLevel;

    var batteryStats = GetBatteryTechStats(unlocks.BatteryTechLevel);
    battery.MaxCapacity *= batteryStats.CapacityMultiplier;
    battery.SelfDischargeRate = batteryStats.SelfDischargeRate;
    battery.ChargeEfficiency = batteryStats.ChargeEfficiency;
    battery.DischargeEfficiency = batteryStats.DischargeEfficiency;
    battery.MaxCycles = batteryStats.MaxCycles;
    battery.TechLevel = unlocks.BatteryTechLevel;
}
```

---

## Summary

The **Power and Battery System** creates a consistent energy economy where:

✅ **Generation has losses** - Reactors waste 8-30% as heat depending on tech/degradation
✅ **Distribution has losses** - Transmission loses 2-15% depending on tech/damage
✅ **Batteries self-discharge** - Passive drain of 0.005-0.1% per second based on tech
✅ **Weapon/shield buffers required** - High-power systems need local power banks for bursts
✅ **Tech unlocks improve efficiency** - Better reactors, less transmission loss, longer battery life
✅ **Burst-compatible** - All calculations deterministic and parallel-safe
✅ **Entity-agnostic** - Works for ships, mechs, buildings, stations
✅ **Strategic depth** - Power management matters in combat

**Game Impact:**

**Space4X:**
- Reactor damage reduces efficiency (tactical targeting)
- Battery depletion forces tactical retreat
- Power bank sizing affects ship design
- Tech progression feels meaningful

**Godgame:**
- Villages need power infrastructure
- Magic systems can be power consumers (mana as energy)
- Ancient artifacts as high-efficiency generators
- Power shortages affect production

**Result:** Power is a **strategic resource**, not just a number. Managing generation, distribution, storage, and consumption creates depth without complexity.

---

**Related Documentation:**
- [Power_Management_Agnostic.md](../Combat/Power_Management_Agnostic.md) - Module overcharge system
- [Bay_And_Platform_Combat.md](Bay_And_Platform_Combat.md) - Weapon systems
- [General_Forces_System.md](General_Forces_System.md) - Energy weapon physics

---

**Last Updated:** 2025-12-18
**Status:** Design Document - Power Systems
**Burst Compatible:** Yes
**Deterministic:** Yes
**Tech Unlock Friendly:** ABSOLUTELY! 🔋⚡
