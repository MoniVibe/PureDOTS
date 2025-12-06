# Deterministic Debugging Guide

## Overview

Tick hashing enables deterministic debugging by computing hash per tick. Hash mismatches indicate non-determinism.

## Tick Hash System

### Reading Tick Hashes

```csharp
using PureDOTS.Systems;

// Get tick hash for a specific tick
var hashEntity = SystemAPI.GetSingletonEntity<TickHashState>();
var hashBuffer = SystemAPI.GetBuffer<TickHashEntry>(hashEntity);

foreach (var entry in hashBuffer)
{
    if (entry.Tick == targetTick)
    {
        ulong hash = entry.Hash;
        // Compare with expected hash
    }
}
```

### Random Seed Per Tick

```csharp
var seedBuffer = SystemAPI.GetBuffer<RandomSeedPerTick>(hashEntity);

foreach (var seedEntry in seedBuffer)
{
    if (seedEntry.Tick == targetTick)
    {
        uint seed = seedEntry.Seed;
        // Use for deterministic replay
    }
}
```

## Replay Validation

### Comparing Hashes

```csharp
// Run scenario twice
ulong hash1 = GetTickHash(tick);
ulong hash2 = GetTickHash(tick); // From second run

if (hash1 != hash2)
{
    // Non-determinism detected!
    // Check for:
    // - Frame-time usage in simulation
    // - Unseeded random numbers
    // - Order-dependent operations
}
```

## Integration Checklist

When debugging determinism:

- [ ] Check `TickHashEntry` for hash mismatches
- [ ] Verify all randomness uses `DeterministicRandom`
- [ ] Ensure no `SystemAPI.Time.DeltaTime` in simulation
- [ ] Use `RandomSeedPerTick` for replay validation
- [ ] Compare hashes between runs to detect non-determinism

