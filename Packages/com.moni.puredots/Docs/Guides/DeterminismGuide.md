# Deterministic Simulation Guide

## Overview

All simulation systems must be deterministic and reversible for rewind/replay safety. This guide covers patterns and utilities for maintaining determinism.

## Deterministic Random Number Generation

### Using DeterministicRandom

**Never use** `new Random()` or `Random.CreateFromIndex()` directly. Always seed from (Tick, EntityId):

```csharp
using PureDOTS.Runtime.Core;

// For entity-specific randomness
var random = DeterministicRandom.CreateFromTickAndEntity(currentTick, entity);

// For system-level randomness
var random = DeterministicRandom.CreateFromTick(currentTick);

// Example: Villager decision making
var rng = DeterministicRandom.CreateFromTickAndEntity(timeState.Tick, villagerEntity);
if (rng.NextFloat() < 0.5f)
{
    // Deterministic decision
}
```

**Key Rules:**
- Always seed from `TickTimeState.Tick`, never from `SystemAPI.Time.ElapsedTime` or `UnityEngine.Time.frameCount`
- Use `Entity.Index` for entity-specific seeds
- Same (tick, entity) → same random sequence (rewind-safe)

## Fixed-Point Math for Economy/Power Systems

### Using FixedPointMath

For economy and power systems, use fixed-point math to avoid float drift:

```csharp
using PureDOTS.Runtime.Core;

// Convert float to fixed-point
long productionRate = FixedPointMath.ToFixed(10.5f); // 105000 (10.5 * 10000)

// Operations
long total = FixedPointMath.Add(productionRate, consumptionRate);
long net = FixedPointMath.Subtract(total, FixedPointMath.ToFixed(5.0f));

// Convert back to float
float result = FixedPointMath.ToFloat(net);

// Component usage
var power = new FixedPointValue(100.0f); // 100.0 units
power.Value = FixedPointMath.Multiply(power.Value, FixedPointMath.ToFixed(1.5f));
```

**When to Use:**
- Power production/consumption
- Resource economy calculations
- Trade balance computations
- Any system requiring exact decimal precision over long periods

## Time Management

### Never Use Frame-Time in Simulation

**WRONG:**
```csharp
float deltaTime = SystemAPI.Time.DeltaTime; // Frame-time - breaks determinism!
```

**CORRECT:**
```csharp
var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
float deltaTime = tickTimeState.FixedDeltaTime; // Tick-time - deterministic
```

**Rule:** Simulation systems use `TickTimeState.FixedDeltaTime`. Presentation systems use `SystemAPI.Time.DeltaTime`.

## Integration Checklist

When creating new simulation systems:

- [ ] Use `DeterministicRandom.CreateFromTickAndEntity()` for all randomness
- [ ] Use `TickTimeState.FixedDeltaTime` for time deltas
- [ ] Use `FixedPointMath` for economy/power calculations
- [ ] Check `RewindState.Mode != RewindMode.Record` before mutations
- [ ] Never use `SystemAPI.Time.DeltaTime` in simulation systems

