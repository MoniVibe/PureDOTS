# Determinism Checklist

**Last Updated**: 2025-12-01
**Maintainer**: PureDOTS Framework Team

---

## Project Context

**This guide applies to all three projects:**

- **PureDOTS**: `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS` - Framework code
- **Space4X**: `C:\Users\Moni\Documents\claudeprojects\unity\Space4x` - Game code
- **Godgame**: `C:\Users\Moni\Documents\claudeprojects\unity\Godgame` - Game code

**⚠️ Important:** When writing PureDOTS framework code, ensure it remains game-agnostic. Determinism requirements apply to all projects.

See [PROJECT_SEPARATION.md](PROJECT_SEPARATION.md) for project separation rules.

---

## Overview

Deterministic simulation is critical for PureDOTS (rewind, replay, multiplayer). This checklist ensures code is deterministic and rewind-safe.

**Key Requirements:**
- ✅ Fixed timestep (no frame-time dependencies)
- ✅ Seeded RNG (reproducible randomness)
- ✅ Avoid Unity APIs in simulation (non-deterministic)
- ✅ Rewind guards (check before mutations)

**See Also:** [TRI_PROJECT_BRIEFING.md](../../TRI_PROJECT_BRIEFING.md) - P2 pattern

---

## Fixed Timestep Requirements

### Use TickTimeState, Not Frame Time

**❌ Wrong: Frame time (non-deterministic)**
```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    // ❌ Frame time varies, non-deterministic
    var deltaTime = Time.deltaTime;  // Frame time!
    
    foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>())
    {
        transform.ValueRW.Position += velocity * deltaTime;
    }
}
```

**✅ Correct: Fixed timestep**
```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    // ✅ Fixed timestep (deterministic)
    var deltaTime = SystemAPI.Time.DeltaTime;  // Tick time!
    
    foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>())
    {
        transform.ValueRW.Position += velocity * deltaTime;
    }
}
```

### Presentation vs Simulation

**Rule:** Simulation uses tick-time, presentation uses frame-time.

```csharp
// ✅ Simulation (deterministic)
[BurstCompile]
public partial struct MovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;  // Fixed timestep
        // ... deterministic movement ...
    }
}

// ✅ Presentation (smooth, frame-time)
public class CameraController : MonoBehaviour
{
    void Update()
    {
        // Frame time for smooth camera
        transform.position += moveVector * Time.deltaTime;  // Frame time OK
    }
}
```

---

## RNG Seeding Patterns

### Seeded Random Number Generator

**❌ Wrong: Non-deterministic RNG**
```csharp
// ❌ System.Random (non-deterministic)
var random = new System.Random();
var value = random.NextFloat();  // Different each run!
```

**✅ Correct: Unity.Mathematics.Random (seeded)**
```csharp
// ✅ Seeded RNG (deterministic)
public struct GameRNG : IComponentData
{
    public uint Seed;
}

// Initialize with seed
var rng = new Unity.Mathematics.Random(seed);
var value = rng.NextFloat();  // Same result with same seed
```

### RNG State Management

**Store RNG state in component:**

```csharp
public struct RNGState : IComponentData
{
    public Unity.Mathematics.Random Value;
}

// Initialize with seed
var rngState = new RNGState
{
    Value = new Unity.Mathematics.Random(seed)
};

// Use in systems
foreach (var rng in SystemAPI.Query<RefRW<RNGState>>())
{
    var randomValue = rng.ValueRO.Value.NextFloat();
    // Update state
    rng.ValueRW.Value = rng.ValueRO.Value;
}
```

---

## Avoiding Unity APIs in Simulation

### Forbidden APIs

**❌ Don't use in simulation systems:**

| API | Why | Alternative |
|-----|-----|-------------|
| `Time.deltaTime` | Frame time | `SystemAPI.Time.DeltaTime` |
| `Random.Range()` | Non-deterministic | `Unity.Mathematics.Random` |
| `UnityEngine.Random` | Non-deterministic | `Unity.Mathematics.Random` |
| `Debug.Log()` | Managed code | Telemetry system |
| `GameObject.Find()` | Managed code | Registry/query |
| `Transform` (MonoBehaviour) | Managed code | `LocalTransform` component |

### Allowed APIs

**✅ Safe to use:**

- `Unity.Mathematics` (all functions)
- `SystemAPI` (DOTS APIs)
- `NativeArray`, `NativeList` (native containers)
- Burst-compiled functions

---

## Float Precision Considerations

### Floating Point Determinism

**Floats are deterministic but watch for:**
- Platform differences (rare with IL2CPP)
- Compiler optimizations (use `[BurstCompile(FloatMode = FloatMode.Deterministic)]`)

```csharp
[BurstCompile(FloatMode = FloatMode.Deterministic)]
public partial struct DeterministicSystem : ISystem
{
    // Ensures consistent float operations across platforms
}
```

### Accumulation Errors

**Be aware of floating point precision:**

```csharp
// ⚠️ Watch for precision loss over time
float accumulator = 0f;
for (int i = 0; i < 1000000; i++)
{
    accumulator += 0.1f;  // Precision loss
}

// ✅ Better: Use fixed-point or reset periodically
```

---

## Sort Stability Requirements

### Deterministic Sorting

**Ensure sorts are stable and deterministic:**

```csharp
// ✅ Good: Deterministic sort
var sorted = new NativeList<Entity>(entities.Length, Allocator.Temp);
sorted.CopyFrom(entities);

// Sort by stable criteria (e.g., Entity index)
sorted.Sort(new EntityComparer());

// ❌ Bad: Non-deterministic sort (e.g., by position with ties)
sorted.Sort(new PositionComparer());  // May vary with ties
```

### Collection Iteration Order

**Be aware of iteration order:**

```csharp
// NativeHashMap iteration order is NOT guaranteed
var map = new NativeHashMap<int, float>(100, Allocator.Temp);
// Iteration order may vary

// ✅ If order matters, use NativeList + sort
var list = new NativeList<KeyValuePair<int, float>>(Allocator.Temp);
// Sort by key, then iterate
```

---

## Rewind-Safe System Patterns

### Rewind Guard Pattern

**Check rewind state before mutations:**

```csharp
public partial struct MutatingSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var rewind = SystemAPI.GetSingleton<RewindState>();
        
        // Only mutate during record mode
        if (rewind.Mode != RewindMode.Record)
            return;
        
        // Safe to mutate
        foreach (var health in SystemAPI.Query<RefRW<Health>>())
        {
            health.ValueRW.Current -= damage;
        }
    }
}
```

### Read-Only During Rewind

**Read-only systems are always safe:**

```csharp
public partial struct ReadOnlySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // No mutations, always safe
        foreach (var health in SystemAPI.Query<RefRO<Health>>())
        {
            // Read-only access
            var current = health.ValueRO.Current;
        }
    }
}
```

---

## Testing Determinism

### Deterministic Test Pattern

```csharp
[Test]
public void System_IsDeterministic()
{
    uint seed = 12345;
    
    // Run 1
    var result1 = RunSystemWithSeed(seed);
    
    // Run 2 (should be identical)
    var result2 = RunSystemWithSeed(seed);
    
    // Assert identical results
    Assert.AreEqual(result1, result2, "System should be deterministic");
}

private float RunSystemWithSeed(uint seed)
{
    using var world = new World("TestWorld");
    
    // Initialize with seed
    var rng = new Unity.Mathematics.Random(seed);
    // ... setup entities ...
    
    // Run system
    var system = world.GetOrCreateSystemManaged<MySystem>();
    system.Update(world.Unmanaged);
    
    // Return result
    return GetResult(world);
}
```

### Replay Verification

**Test replay matches original:**

```csharp
[Test]
public void Replay_MatchesOriginal()
{
    // Record simulation
    var recorded = RecordSimulation(seed);
    
    // Replay simulation
    var replayed = ReplaySimulation(recorded, seed);
    
    // Assert match
    AssertSimulationsMatch(recorded, replayed);
}
```

---

## Common Pitfalls

### Pitfall 1: Frame Time in Simulation

**❌ Wrong:**
```csharp
var deltaTime = Time.deltaTime;  // Frame time!
```

**✅ Correct:**
```csharp
var deltaTime = SystemAPI.Time.DeltaTime;  // Tick time!
```

### Pitfall 2: Non-Seeded RNG

**❌ Wrong:**
```csharp
var random = new System.Random();  // Non-deterministic!
```

**✅ Correct:**
```csharp
var random = new Unity.Mathematics.Random(seed);  // Seeded!
```

### Pitfall 3: Unity APIs in Burst

**❌ Wrong:**
```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    Debug.Log("Hello");  // Managed API!
}
```

**✅ Correct:**
```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    // Use telemetry system or remove debug code
}
```

### Pitfall 4: Missing Rewind Guard

**❌ Wrong:**
```csharp
public void OnUpdate(ref SystemState state)
{
    // Mutates without checking rewind state
    foreach (var health in SystemAPI.Query<RefRW<Health>>())
    {
        health.ValueRW.Current -= damage;
    }
}
```

**✅ Correct:**
```csharp
public void OnUpdate(ref SystemState state)
{
    var rewind = SystemAPI.GetSingleton<RewindState>();
    if (rewind.Mode != RewindMode.Record) return;
    
    foreach (var health in SystemAPI.Query<RefRW<Health>>())
    {
        health.ValueRW.Current -= damage;
    }
}
```

---

## Determinism Checklist

### Before Committing Code

- [ ] **Fixed timestep**: Using `SystemAPI.Time.DeltaTime`, not `Time.deltaTime`
- [ ] **Seeded RNG**: Using `Unity.Mathematics.Random` with seed
- [ ] **No Unity APIs**: Avoided `Time.deltaTime`, `Random.Range()`, etc.
- [ ] **Rewind guards**: Check `RewindState.Mode` before mutations
- [ ] **Float mode**: Use `FloatMode.Deterministic` if needed
- [ ] **Sort stability**: Sorts use stable criteria
- [ ] **Test determinism**: Tests verify identical results with same seed

### Code Review Checklist

- [ ] Simulation systems use tick-time
- [ ] Presentation systems use frame-time (OK)
- [ ] RNG is seeded and stored in components
- [ ] No managed Unity APIs in Burst systems
- [ ] Rewind guards present in mutating systems
- [ ] Tests verify determinism

---

## Best Practices Summary

1. ✅ **Use fixed timestep** (`SystemAPI.Time.DeltaTime`)
2. ✅ **Seed RNG** (`Unity.Mathematics.Random(seed)`)
3. ✅ **Avoid Unity APIs** in simulation (use DOTS alternatives)
4. ✅ **Check rewind state** before mutations
5. ✅ **Use deterministic float mode** if needed
6. ✅ **Ensure sort stability** (stable criteria)
7. ✅ **Test determinism** (same seed = same result)
8. ❌ **Don't use frame time** in simulation
9. ❌ **Don't use System.Random** (non-deterministic)
10. ❌ **Don't mutate during rewind** (check mode first)

---

## Additional Resources

- [TRI_PROJECT_BRIEFING.md](../../TRI_PROJECT_BRIEFING.md) - P2 Rewind Guard pattern
- [DOTS 1.4 Patterns](BestPractices/DOTS_1_4_Patterns.md)
- [Foundation Guidelines](../FoundationGuidelines.md)

---

*Last Updated: 2025-12-01*
*Maintainer: PureDOTS Framework Team*

