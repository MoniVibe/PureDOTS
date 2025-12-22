# Time System Advisory

**Status:** Recommendations / Action Plan
**Category:** Core - Time System Polish & Hardening
**Audience:** Implementers / Architects
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Purpose

**Primary Goal:** Tighten the time system through semantic cleanup and correctness hardening changes. The core spine (fixed-tick, command-driven controls, history tiers, group-level rewind guards) is solid; the tightening work focuses on clarity and robustness.

**Focus Areas:**
1. **Semantic clarity** — Explicit tick naming (PresentTick, ViewTick, TargetTick)
2. **Delta-time correctness** — Speed changes ticks-per-real-second, not dt-per-tick
3. **Accumulator hardening** — Double precision, frame time clamping, scale-aware step limits
4. **State simplification** — Split canonical RewindState from legacy/compat fields
5. **Preview rewind correctness** — Presentation-only playback (never mutates simulation)
6. **Multiplayer alignment** — Simulation tick rate vs network tick rate separation
7. **Testing** — Tick semantics and rewind determinism validation

---

## 1. Make One Canonical "Time Context" and Stop Mixing Ticks

### Current State

**Problem:** Multiple tick sources cause confusion:
- `TickTimeState.Tick` (monotonic "present")
- `RewindState.CurrentTick` / `RewindState.PlaybackTick` (the "view/playback tick")

**Confusion:** Systems don't know which tick to read during rewind/playback.

### Proposed Solution

**Rename Explicitly:**

```csharp
// ✅ Canonical time context (single source of truth)
public struct TimeContext : IComponentData
{
    /// <summary>Monotonic max recorded tick (never decreases, always >= ViewTick).</summary>
    public uint PresentTick;
    
    /// <summary>What the world is currently showing/simulating (may differ from PresentTick during playback).</summary>
    public uint ViewTick;
    
    /// <summary>Where you're scrubbing/rewinding to (target for playback).</summary>
    public uint TargetTick;
    
    /// <summary>Base fixed timestep (seconds per tick, e.g., 1/60).</summary>
    public float FixedDeltaTime;
    
    /// <summary>Whether simulation is paused.</summary>
    public bool IsPaused;
    
    /// <summary>Current speed multiplier (affects ticks-per-real-second, not dt-per-tick).</summary>
    public float SpeedMultiplier;
    
    /// <summary>Current rewind mode.</summary>
    public RewindMode Mode;
}

// ✅ Single accessor for all systems
public static class TimeAPI
{
    // SystemAPI.Time equivalent that systems should use
    public static TimeContext GetTimeContext(ref SystemState state)
    {
        var timeContext = SystemAPI.GetSingleton<TimeContext>();
        return timeContext;
    }
    
    // Convenience accessors
    public static uint GetViewTick(ref SystemState state) => GetTimeContext(ref state).ViewTick;
    public static float GetFixedDeltaTime(ref SystemState state) => GetTimeContext(ref state).FixedDeltaTime;
}
```

**Semantics:**
- **PresentTick:** Monotonic maximum tick that has been recorded. Never decreases. Always >= ViewTick.
- **ViewTick:** The tick the world is currently showing/simulating. During Record mode: ViewTick == PresentTick. During Playback mode: ViewTick moves toward TargetTick.
- **TargetTick:** Where you're scrubbing/rewinding to. During Record mode: TargetTick == PresentTick. During Playback mode: TargetTick is the destination.

**Require All Sim Systems to Read ViewTick:**
- Systems should read `TimeContext.ViewTick` (not `TickTimeState.Tick` or `RewindState.PlaybackTick`)
- All time calculations use `TimeContext.FixedDeltaTime`
- This removes "which tick do I read?" confusion

**Migration Path:**
1. Create `TimeContext` singleton (replaces `TickTimeState` + `RewindState` fields)
2. `TimeTickSystem` updates `PresentTick` and `ViewTick` based on mode
3. Migrate systems to use `TimeAPI.GetTimeContext()` or `TimeAPI.GetViewTick()`
4. Deprecate `TickTimeState.Tick` (redirect to `TimeContext.ViewTick`)
5. Remove `RewindState.CurrentTick` / `PlaybackTick` (use `TimeContext.ViewTick`)

---

## 2. Fix Delta-Time Semantics: Speed Should Change Ticks-Per-Real-Second, Not dt-Per-Tick

### Current State

**Problem:** Speed multiplier may be applied incorrectly, causing double-scaling.

**Current Implementation:**
```csharp
// ❌ POTENTIAL ISSUE: Speed affects delta-time calculation
float scaledDelta = deltaRealTime * baseSpeedMultiplier * effectiveTimeScale;
_accumulator += scaledDelta;

// Then systems might do:
float dt = timeState.DeltaTime * timeState.CurrentSpeedMultiplier;  // Double scaling!
```

### Proposed Solution

**Define One Truth: Each Simulation Tick Integrates with FixedDeltaTime Only.**

**Key Principle:** Speed multiplier changes **how many ticks run per real second**, not the delta-time per tick.

**Implementation:**
```csharp
// ✅ CORRECT: Speed affects tick rate, not dt-per-tick
public struct TimeContext : IComponentData
{
    public float FixedDeltaTime;      // Constant: e.g., 1/60 seconds per tick
    public float SpeedMultiplier;     // Affects ticks-per-real-second, NOT dt-per-tick
}

// In TimeTickSystem (accumulator-based):
void OnUpdate(ref SystemState state)
{
    var context = SystemAPI.GetSingletonRW<TimeContext>();
    
    // Calculate how much real time has passed
    double deltaRealTime = (double)SystemAPI.Time.ElapsedTime - _lastRealTime;
    _lastRealTime = (double)SystemAPI.Time.ElapsedTime;
    
    // Apply max frame time clamp (see §3)
    deltaRealTime = math.min(deltaRealTime, MaxFrameTimeClamp);
    
    // Speed affects how many ticks to accumulate (ticks-per-real-second)
    // At 2x speed, we accumulate 2x as many ticks worth of time
    double scaledDelta = deltaRealTime * (double)context.ValueRO.SpeedMultiplier;
    
    // Accumulate (using double precision)
    _accumulator += scaledDelta;
    
    // Advance ticks using FixedDeltaTime (constant, speed-independent)
    double fixedDt = (double)context.ValueRO.FixedDeltaTime;
    int steps = 0;
    while (_accumulator >= fixedDt && steps < MaxStepsPerFrame)
    {
        _accumulator -= fixedDt;
        context.ValueRW.ViewTick++;
        context.ValueRW.PresentTick = math.max(context.ValueRW.PresentTick, context.ValueRW.ViewTick);
        steps++;
    }
}

// In sim systems:
void OnUpdate(ref SystemState state)
{
    var context = TimeAPI.GetTimeContext(ref state);
    
    // ✅ CORRECT: Use FixedDeltaTime (constant, speed-independent)
    float dt = context.FixedDeltaTime;
    
    foreach (var (velocity, transform) in SystemAPI.Query<RefRO<Velocity>, RefRW<LocalTransform>>())
    {
        // dt is constant per tick, regardless of speed
        transform.ValueRW.Position += velocity.ValueRO.Value * dt;
    }
}

// ❌ WRONG: Don't multiply by SpeedMultiplier in sim systems
// float dt = context.FixedDeltaTime * context.SpeedMultiplier;  // Wrong! Speed already affects tick rate
```

**Do Not Encourage `FixedDeltaTime * SpeedMultiplier` Inside Sim Systems:**
- That double-scales when you also run more ticks
- Keep "scaled delta" as a driver only (how many ticks to run this frame), not as per-tick dt

**If You Need a "UI Delta" for Presentation:**
- Name it `FrameScaledSeconds` (or `PresentationDeltaTime`)
- Keep it out of simulation systems
- Use only for presentation/interpolation

**Reference:** [Gaffer On Games - Fix Your Timestep!](https://gafferongames.com/post/fix_your_timestep/)

---

## 3. Harden the Accumulator (Small, Worth It)

### Current State

**Problem:** Accumulator uses `float` precision, no frame time clamping, fixed max steps.

**Current Implementation:**
```csharp
private float _accumulator;
private float _lastRealTime;

float deltaRealTime = elapsed - _lastRealTime;
float scaledDelta = deltaRealTime * baseSpeedMultiplier * effectiveTimeScale;
_accumulator += scaledDelta;

const int maxStepsPerFrame = 4;  // Fixed, not scale-aware
```

### Proposed Solution

**Use Double for Time Calculations:**

```csharp
// ✅ CORRECT: Use double for time stamps and accumulator
private double _accumulator;
private double _lastRealTime;

void OnUpdate(ref SystemState state)
{
    var context = SystemAPI.GetSingletonRW<TimeContext>();
    
    // Use double for real time stamps
    double elapsed = (double)SystemAPI.Time.ElapsedTime;
    double deltaRealTime = elapsed - _lastRealTime;
    _lastRealTime = elapsed;
    
    // Apply max frame time clamp (prevents pathological catch-up after long stall)
    const double MaxFrameTimeClamp = 0.25;  // 250ms max
    deltaRealTime = math.min(deltaRealTime, MaxFrameTimeClamp);
    
    // Use double for accumulator and scaled delta
    double scaledDelta = deltaRealTime * (double)context.ValueRO.SpeedMultiplier;
    _accumulator += scaledDelta;
    
    // Use double for fixed delta-time comparison
    double fixedDt = (double)context.ValueRO.FixedDeltaTime;
    int steps = 0;
    
    // Scale-aware max steps (normal: 4-8, fast-forward/catch-up: larger)
    int maxSteps = GetMaxStepsForSpeed(context.ValueRO.SpeedMultiplier, context.ValueRO.Mode);
    
    while (_accumulator >= fixedDt && steps < maxSteps)
    {
        _accumulator -= fixedDt;
        context.ValueRW.ViewTick++;
        steps++;
    }
}

// Scale-aware max steps
int GetMaxStepsForSpeed(float speedMultiplier, RewindMode mode)
{
    // Normal speed: small limit (4-8)
    if (speedMultiplier <= 2.0f && mode == RewindMode.Play)
    {
        return 8;  // Normal operation
    }
    
    // Fast-forward/catch-up: larger limit, but with budget
    if (speedMultiplier > 2.0f || mode == RewindMode.Step)
    {
        return 32;  // Fast-forward can run more steps
    }
    
    // Playback: moderate limit
    if (mode == RewindMode.Rewind)
    {
        return 16;  // Playback can skip some presentation
    }
    
    return 8;  // Default
}
```

**Key Changes:**
1. **Double precision:** Use `double` for `_accumulator`, `_lastRealTime`, `deltaRealTime`, `scaledDelta`, `fixedDt`
2. **Max frame time clamp:** Clamp `deltaRealTime` to 0.25s max (prevents pathological catch-up after long stall)
3. **Scale-aware max steps:** Normal: 4-8, fast-forward/catch-up: 16-32

**Reference:** [Gaffer On Games - Fix Your Timestep!](https://gafferongames.com/post/fix_your_timestep/)

---

## 4. Simplify RewindState: Split "Runtime State" from "Legacy/Compat"

### Current State

**Problem:** `RewindState` mixes canonical fields + legacy aliases/fields, causing confusion and potential bugs.

**Current Implementation:**
```csharp
public struct RewindState : IComponentData
{
    // Canonical baseline
    public RewindMode Mode;
    public int CurrentTick;           // ❌ Duplicates ViewTick?
    public int TargetTick;
    public float TickDuration;
    public int MaxHistoryTicks;
    public byte PendingStepTicks;
    
    // Legacy fields (kept for compatibility)
    public float PlaybackSpeed;
    public uint StartTick;
    public uint PlaybackTick;         // ❌ Duplicates CurrentTick?
    public float PlaybackTicksPerSecond;
    public ScrubDirection ScrubDirection;
    public float ScrubSpeedMultiplier;
    public uint RewindWindowTicks;
    public RewindTrackId ActiveTrack;
}
```

### Proposed Solution

**Keep a Minimal Canonical Struct:**

```csharp
// ✅ Canonical rewind state (minimal, clear)
public struct RewindState : IComponentData
{
    /// <summary>Current rewind mode (Play, Rewind, Step, Paused).</summary>
    public RewindMode Mode;
    
    /// <summary>Target tick for playback (where we're scrubbing to).</summary>
    public uint TargetTick;
    
    /// <summary>Maximum history buffer size (ticks).</summary>
    public uint MaxHistoryTicks;
    
    /// <summary>Pending step ticks (for step mode).</summary>
    public byte PendingStepTicks;
    
    /// <summary>Scrub speed multiplier (for preview rewind).</summary>
    public float ScrubSpeed;
}

// ✅ Legacy/compat state (separate, deprecated)
[Obsolete("Use TimeContext and RewindState instead. Will be removed in v2.0.")]
public struct RewindLegacyState : IComponentData
{
    // All legacy fields moved here
    public float PlaybackSpeed;
    public uint StartTick;
    public uint PlaybackTick;
    public float PlaybackTicksPerSecond;
    public ScrubDirection ScrubDirection;
    public float ScrubSpeedMultiplier;
    public uint RewindWindowTicks;
    public RewindTrackId ActiveTrack;
}
```

**Note:** `CurrentTick` / `ViewTick` moved to `TimeContext` (see §1).

**Benefits:**
- Reduces bugs where two fields disagree (e.g., `CurrentTick` vs `PlaybackTick`)
- Clear separation of canonical vs legacy
- Easier to deprecate/remove legacy fields

**Migration Path:**
1. Create minimal `RewindState` with canonical fields only
2. Move legacy fields to `RewindLegacyState` (or delete if unused)
3. Update systems to use `TimeContext` for tick values
4. Mark legacy struct as `[Obsolete]`
5. Remove after migration complete

---

## 5. Preview Rewind: Make It "Presentation-Only Playback"

### Current State

**Problem:** Preview rewind implementation may mutate simulation world state, causing correctness issues.

**Current Implementation:**
- `RewindControlSystem` manages preview phases
- Ghosts scrub through history
- World stays frozen at `PresentTickAtStart`
- But systems may still mutate during preview

### Proposed Solution

**Hard Rule: Preview Never Mutates Simulation World State.**

**Implementation Pattern:**

```csharp
// ✅ Preview rewind reads history and writes only to presentation layer
public struct RewindControlState : IComponentData
{
    public RewindPhase Phase;
    public uint PresentTickAtStart;   // World stays frozen here
    public uint PreviewTick;          // Ghosts preview this tick
    public float ScrubSpeed;
}

// In preview systems:
public partial struct GhostPlaybackSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var control = SystemAPI.GetSingleton<RewindControlState>();
        
        if (control.Phase == RewindPhase.Inactive)
            return;
        
        // ✅ Read from history snapshots
        var historyBuffer = SystemAPI.GetBuffer<EntityHistorySnapshot>(ghostEntity);
        var snapshot = FindSnapshotForTick(historyBuffer, control.PreviewTick);
        
        // ✅ Write ONLY to presentation/ghost components (not simulation components)
        if (SystemAPI.HasComponent<GhostTransform>(ghostEntity))
        {
            var ghostTransform = SystemAPI.GetComponentRW<GhostTransform>(ghostEntity);
            ghostTransform.ValueRW.Position = snapshot.Position;  // Ghost layer
            ghostTransform.ValueRW.Rotation = snapshot.Rotation;
        }
        
        // ❌ NEVER mutate simulation components during preview
        // var transform = SystemAPI.GetComponentRW<LocalTransform>(entity);  // Wrong!
        // transform.ValueRW.Position = snapshot.Position;  // Wrong! Never mutate sim state
    }
}

// In simulation systems:
public partial struct MovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var control = SystemAPI.GetSingleton<RewindControlState>();
        
        // ✅ Skip simulation during preview (world is frozen)
        if (control.Phase != RewindPhase.Inactive)
            return;  // World stays frozen at PresentTickAtStart
        
        // Normal simulation (only runs when preview inactive)
        foreach (var (velocity, transform) in SystemAPI.Query<RefRO<Velocity>, RefRW<LocalTransform>>())
        {
            transform.ValueRW.Position += velocity.ValueRO.Value * dt;
        }
    }
}
```

**Mental Model:**
- Preview is "playback of recorded state" (read-only from history)
- Analogous to Unity Netcode's separation: simulation runs fixed "full ticks", preview/rollback reads history and writes to prediction/ghost layer
- World state stays frozen at `PresentTickAtStart` during preview
- Only ghost/presentation entities update based on `PreviewTick`

**Reference:** [Unity Netcode for Entities - Tick and Update](https://docs.unity.cn/Packages/com.unity.netcode.gameobjects@1.0/manual/tick-rate.html)

**Migration Path:**
1. Ensure all simulation systems skip during preview (`RewindControlState.Phase != Inactive`)
2. Create ghost/presentation layer components (`GhostTransform`, `GhostHealth`, etc.)
3. Preview systems read history and write only to ghost components
4. Presentation systems render ghosts (not simulation entities) during preview
5. On commit: Apply rewind to world state (normal playback flow)

---

## 6. Multiplayer Readiness: Align to "Simulation Tick Rate vs Network Tick Rate"

### Current State

**Problem:** Multiplayer support is stubbed, but command architecture is good fit. Need to align to standard multiplayer tick rate separation.

**Current Implementation:**
- `TimeNetworkSyncSystem` is placeholder
- Command architecture supports `Player` scope
- But no tick rate separation defined

### Proposed Solution

**Mirror Unity Netcode's Separation:**

**Key Principle:**
- **Simulation runs at fixed `SimulationTickRate`** (e.g., 60 ticks/second)
- **Network snapshots at `NetworkTickRate`** (must be a factor of sim tick rate, e.g., 20 ticks/second = every 3 sim ticks)

**Implementation:**
```csharp
// ✅ Time context with tick rate configuration
public struct TimeContext : IComponentData
{
    public float FixedDeltaTime;           // 1 / SimulationTickRate (e.g., 1/60)
    public uint SimulationTickRate;        // e.g., 60 ticks/second
    public uint NetworkTickRate;           // e.g., 20 ticks/second (must be factor of SimulationTickRate)
    public uint NetworkTickStride;         // SimulationTickRate / NetworkTickRate (e.g., 3)
}

// In network sync system:
public partial struct TimeNetworkSyncSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var context = SystemAPI.GetSingleton<TimeContext>();
        
        // Only sync on network ticks (every NetworkTickStride simulation ticks)
        if (context.ViewTick % context.NetworkTickStride != 0)
            return;
        
        // Send network snapshot
        var snapshot = new NetworkTimeSnapshot
        {
            Tick = context.ViewTick,
            Mode = context.Mode,
            SpeedMultiplier = context.SpeedMultiplier
        };
        
        // Broadcast to clients (via Netcode or custom network layer)
        NetworkManager.Instance.SendTimeSnapshot(snapshot);
    }
}
```

**Constraints:**
- `NetworkTickRate` must be a factor of `SimulationTickRate`
- Network snapshots sent every `NetworkTickStride` simulation ticks
- Client prediction/interpolation handles network tick rate vs sim tick rate differences

**Benefits:**
- Aligns with Unity Netcode patterns (easier integration later)
- Keeps simulation deterministic (fixed tick rate)
- Network bandwidth efficient (lower snapshot rate)
- Client prediction/interpolation can smooth network updates

**Reference:** [Unity Netcode for Entities - Tick and Update](https://docs.unity.cn/Packages/com.unity.netcode.gameobjects@1.0/manual/tick-rate.html)

**Migration Path:**
1. Add `SimulationTickRate`, `NetworkTickRate`, `NetworkTickStride` to `TimeContext`
2. Validate `NetworkTickRate` is a factor of `SimulationTickRate` (assert on bootstrap)
3. Update `TimeNetworkSyncSystem` to sync only on network ticks
4. Document tick rate configuration for multiplayer scenarios

---

## 7. Two "Must Add" Tests (Cheap, High Value)

### Test 1: Tick Semantics Test

**Purpose:** Validate that tick semantics are correct in each mode.

**Implementation:**
```csharp
[Test]
public void TickSemanticsTest()
{
    // Setup: Create world with TimeContext
    var world = new World("Test");
    var contextEntity = world.EntityManager.CreateEntity();
    world.EntityManager.AddComponent<TimeContext>(contextEntity);
    
    var context = world.EntityManager.GetComponentData<TimeContext>();
    context.FixedDeltaTime = 1.0f / 60.0f;
    context.ViewTick = 0;
    context.PresentTick = 0;
    context.TargetTick = 0;
    world.EntityManager.SetComponentData(contextEntity, context);
    
    // Test 1: Record mode - PresentTick and ViewTick advance together
    context.Mode = RewindMode.Play;
    world.EntityManager.SetComponentData(contextEntity, context);
    
    AdvanceTicks(world, 10);
    
    context = world.EntityManager.GetComponentData<TimeContext>(contextEntity);
    Assert.AreEqual(10u, context.PresentTick, "PresentTick should advance in Record mode");
    Assert.AreEqual(10u, context.ViewTick, "ViewTick should equal PresentTick in Record mode");
    Assert.AreEqual(10u, context.TargetTick, "TargetTick should equal PresentTick in Record mode");
    
    // Test 2: Playback mode - ViewTick moves toward TargetTick, PresentTick stays constant
    context.Mode = RewindMode.Rewind;
    context.TargetTick = 5;
    world.EntityManager.SetComponentData(contextEntity, context);
    
    AdvancePlayback(world, 5);
    
    context = world.EntityManager.GetComponentData<TimeContext>(contextEntity);
    Assert.AreEqual(10u, context.PresentTick, "PresentTick should not decrease");
    Assert.AreEqual(5u, context.ViewTick, "ViewTick should reach TargetTick");
    Assert.AreEqual(5u, context.TargetTick, "TargetTick should remain constant");
    
    // Test 3: Systems read ViewTick (not PresentTick)
    // (Create test system that asserts it reads ViewTick)
    // ...
}
```

**Assertions:**
- In Record mode: `PresentTick == ViewTick == TargetTick` (all advance together)
- In Playback mode: `PresentTick` stays constant, `ViewTick` moves toward `TargetTick`
- Systems read `ViewTick` (not `PresentTick`) for simulation

### Test 2: Rewind Determinism Test

**Purpose:** Validate that rewind preserves determinism end-to-end.

**Implementation:**
```csharp
[Test]
public void RewindDeterminismTest()
{
    // Setup: Create world with deterministic seed
    var world = new World("Test");
    InitializeDeterministicWorld(world, seed: 12345);
    
    // Record N ticks
    const int N = 200;
    var stateHashes = new List<uint>();
    
    for (int i = 0; i < N; i++)
    {
        world.Update();
        
        // Hash critical components each tick
        uint hash = HashCriticalComponents(world);
        stateHashes.Add(hash);
    }
    
    // Scrub to tick T
    const int T = 100;
    RewindToTick(world, T);
    
    // Verify state matches original tick T
    uint rewindHash = HashCriticalComponents(world);
    Assert.AreEqual(stateHashes[T], rewindHash, $"Rewind to tick {T} should match original state");
    
    // Commit and run forward K ticks
    CommitRewind(world);
    const int K = 50;
    
    for (int i = 0; i < K; i++)
    {
        world.Update();
        
        int expectedTick = T + i + 1;
        uint hash = HashCriticalComponents(world);
        Assert.AreEqual(stateHashes[expectedTick], hash, 
            $"Tick {expectedTick} after rewind should match original");
    }
}

uint HashCriticalComponents(World world)
{
    // Hash critical gameplay components (position, health, inventory, etc.)
    // This validates that rewind preserves state correctly
    uint hash = 0;
    
    foreach (var (transform) in world.EntityManager.CreateEntityQuery(typeof(LocalTransform))
        .ToComponentDataArray<LocalTransform>(Allocator.Temp))
    {
        hash ^= math.hash(transform.Position);
    }
    
    // ... hash other critical components ...
    
    return hash;
}
```

**Assertions:**
- State hash at tick T after rewind matches original tick T
- State hashes after rewind commit match original state progression
- Validates tiered history system (SnapshotFull, SnapshotLite, Derived) end-to-end

**Benefits:**
- Catches regressions in tick semantics
- Validates rewind correctness
- Ensures determinism is preserved

---

## Implementation Order

### Phase 1: Semantic Cleanup (Critical Path)
1. **Create TimeContext** (replaces TickTimeState + RewindState tick fields)
2. **Rename ticks explicitly** (PresentTick, ViewTick, TargetTick)
3. **Simplify RewindState** (split canonical from legacy)
4. **Fix delta-time semantics** (speed affects tick rate, not dt-per-tick)

### Phase 2: Correctness Hardening
5. **Harden accumulator** (double precision, frame time clamping, scale-aware steps)
6. **Preview rewind correctness** (presentation-only playback)
7. **Add tests** (tick semantics, rewind determinism)

### Phase 3: Multiplayer Alignment
8. **Tick rate separation** (simulation vs network tick rates)
9. **Network sync integration** (sync only on network ticks)

---

## Related Documentation

- **Time System Summary:** `Docs/Concepts/Core/Time_System_Summary.md` - Current state analysis
- **Gaffer On Games - Fix Your Timestep:** https://gafferongames.com/post/fix_your_timestep/
- **Unity Netcode - Tick and Update:** https://docs.unity.cn/Packages/com.unity.netcode.gameobjects@1.0/manual/tick-rate.html
- **Rewind Integration Guide:** `Docs/Guides/RewindIntegrationGuide.md` - Developer guide

---

**For Implementers:** Focus on Phase 1 (semantic cleanup) for immediate clarity and correctness  
**For Architects:** Review Phase 2/3 (hardening, multiplayer) for robustness and scalability  
**For Testers:** Implement Phase 2 tests (tick semantics, rewind determinism) for validation

