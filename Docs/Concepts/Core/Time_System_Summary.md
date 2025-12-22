# Time System Summary

**Status:** Implementation Analysis
**Audience:** Technical Advisor / Architecture Review
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Executive Summary

The PureDOTS time system provides a **centralized time spine with deterministic tick advancement, speed control (slow-mo/fast-forward), and rewind/playback capabilities**. It uses a fixed timestep (default 1/60s) with speed multipliers (0.01-16.0x) and supports three rewind modes: Record (normal), Playback (rewind), and Step (catch-up).

**Current State:** Core implementation complete; multiple time state singletons (legacy duplication), complex state machine with preview rewind incomplete, multiplayer support stubbed.

**Key Strengths:**
- Deterministic fixed timestep (tick-based)
- Command-based architecture (queueable time control)
- Rewind guards for system-level integration
- Speed multipliers (slow-mo/fast-forward)

**Key Issues:**
- **Dual time state singletons** (TimeState legacy + TickTimeState canonical) causing confusion
- **Complex rewind state machine** with preview rewind incomplete
- **Multiplayer support** mostly stubbed (TODO markers)
- **Inconsistent mode naming** (Record/Play vs Playback/Rewind aliases)

---

## File Mapping

### Core Implementation Files

| File Path | Purpose | Status |
|-----------|---------|--------|
| `Runtime/Runtime/Time/TimeComponents.cs` | `TimeState`, `TickTimeState` components | ✅ Complete |
| `Runtime/Runtime/Time/RewindComponents.cs` | `RewindState`, `RewindMode`, `RewindTier` | ✅ Complete |
| `Runtime/Runtime/Time/TimeAPI.cs` | Static API for MonoBehaviour/UI code | ✅ Complete |
| `Runtime/Runtime/Time/TimeControlComponents.cs` | `TimeControlCommand`, `TimeControlScope`, etc. | ✅ Complete |
| `Runtime/Runtime/Time/TimeHelpers.cs` | Utility functions for time calculations | ✅ Complete |
| `Runtime/Runtime/Time/TimeCheckpointHelpers.cs` | Checkpoint/scrub utilities | ✅ Complete |
| `Runtime/Runtime/Time/TimeBubbleComponents.cs` | Time bubble (local time zones) | ✅ Complete |
| `Runtime/Runtime/Time/TimeScaleComponents.cs` | Time scale scheduling | ✅ Complete |
| `Runtime/Runtime/Time/TimePlayerIds.cs` | Player ID constants (multiplayer) | ✅ Complete |
| `Runtime/Runtime/Time/TimePlayerAuthority.cs` | Player authority tracking | ⚠️ Stubbed |
| `Runtime/Runtime/Time/TimeMultiplayerValidation.cs` | MP command validation | ⚠️ Stubbed |
| `Runtime/Runtime/Time/TimeMultiplayerStubs.cs` | MP stub implementations | ⚠️ Stubbed |
| `Runtime/Runtime/Time/TimeSystemFeatureFlags.cs` | Feature flags (SP/MP mode) | ✅ Complete |
| `Runtime/Runtime/Time/TimeHistoryRegistrations.cs` | History registration utilities | ✅ Complete |
| `Runtime/Runtime/TimeAware.cs` | `TimeAwareController` contract | ✅ Complete |

### System Files

| File Path | Purpose | Status |
|-----------|---------|--------|
| `Systems/TimeTickSystem.cs` | Main tick advancement system | ✅ Complete |
| `Systems/TimeStepSystem.cs` | Alternative tick system (legacy?) | ⚠️ Check usage |
| `Systems/RewindCoordinatorSystem.cs` | Rewind state machine coordinator | ✅ Complete |
| `Systems/RewindControlSystem.cs` | Preview rewind control | ⚠️ Partial |
| `Systems/TimeScaleCommandSystem.cs` | Time scale scheduling commands | ✅ Complete |
| `Systems/TimeScaleResolutionSystem.cs` | Resolves time scale from schedule | ✅ Complete |
| `Systems/TimeSettingsConfigSystem.cs` | Applies time config from authoring | ✅ Complete |
| `Systems/TimeBootstrapSystem.cs` | Time system bootstrap | ✅ Complete |
| `Systems/Time/TimeControlBootstrapSystem.cs` | Time control initialization | ✅ Complete |
| `Systems/Time/TimeOfDaySystem.cs` | Time-of-day calculations | ✅ Complete |
| `Systems/Time/TimeBubbleMembershipSystem.cs` | Time bubble entity management | ✅ Complete |
| `Systems/TimeNetworkSyncSystem.cs` | Network time synchronization | ⚠️ Stubbed |
| `Systems/TimeHistoryRecordSystem.cs` | Time history recording | ✅ Complete |
| `Systems/TimeHistoryPlaybackSystem.cs` | Time history playback | ✅ Complete |
| `Systems/TimeLogConfigSystem.cs` | Time logging configuration | ✅ Complete |
| `Systems/TimeLogUtility.cs` | Time log utilities | ✅ Complete |

### Authoring Files

| File Path | Purpose | Status |
|-----------|---------|--------|
| `Authoring/TimeConfigAuthoring.cs` | Scene authoring for time config | ✅ Complete |
| `Authoring/TimeConfigAssets.cs` | ScriptableObject assets | ✅ Complete |
| `Authoring/TimeControlsAuthoring.cs` | Time control UI authoring | ✅ Complete |

### Integration Points (Game-Specific)

| File Path | Usage | Status |
|-----------|-------|--------|
| `Projects/Godgame/Time/GodgameTimeAPI.cs` | Godgame time API wrapper | ✅ Complete |
| `Projects/Godgame/Time/TimeControlSystem.cs` | Godgame time control input | ✅ Complete |
| `Projects/Space4X/Time/Space4XTimeAPI.cs` | Space4X time API wrapper | ✅ Complete |

### Documentation Files

| File Path | Purpose |
|-----------|---------|
| `Docs/Guides/RewindIntegrationGuide.md` | Developer guide for rewind integration |
| `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` | Runtime lifecycle (includes time system) |
| `Docs/Concepts/Core/Time_Compression_And_Long_Term_Events.md` | Time compression concepts |
| `space4x/Docs/Conceptualization/Mechanics/TimeControl.md` | Space4X time control design |

---

## Specifications

### Core Components

#### TimeState (Legacy/Compatibility)

```csharp
/// <summary>
/// High-level time state singleton component.
/// DESIGN INVARIANT: TimeState.Tick is monotonically increasing in real time and is the canonical "world time index".
/// DESIGN INVARIANT: Rewind is always expressed as playback over history, NOT by decrementing Tick.
/// </summary>
public struct TimeState : IComponentData
{
    public uint Tick;                      // Current simulation tick (monotonically increasing)
    public float DeltaTime;                // Frame delta time, scaled by CurrentSpeedMultiplier
    public float DeltaSeconds;             // Alias for DeltaTime (migration aid)
    public float ElapsedTime;              // Elapsed time in simulation space
    public float WorldSeconds;             // World time in seconds (Tick * FixedDeltaTime)
    public bool IsPaused;                  // Whether simulation is paused
    public float FixedDeltaTime;           // Base fixed timestep (e.g., 1/60 seconds)
    public float CurrentSpeedMultiplier;   // Current speed multiplier (0.01-16.0)
}
```

**Status:** Legacy compatibility layer. `TickTimeState` is canonical; `TimeState` is synced from it.

#### TickTimeState (Canonical)

```csharp
/// <summary>
/// Canonical tick time state singleton component.
/// DESIGN INVARIANT: TickTimeState.Tick is monotonically increasing in real time and is the canonical tick source.
/// DESIGN INVARIANT: Rewind operations do NOT decrement Tick; they use playback over history instead.
/// </summary>
public struct TickTimeState : IComponentData
{
    public uint Tick;                      // Current simulation tick (monotonically increasing)
    public float FixedDeltaTime;           // Base fixed timestep (e.g., 1/60 seconds)
    public float CurrentSpeedMultiplier;   // Current speed multiplier (0.01-16.0)
    public uint TargetTick;                // Target tick for catch-up operations
    public bool IsPaused;                  // Whether simulation is paused
    public bool IsPlaying;                 // Whether simulation is playing (not paused)
    public float WorldSeconds;             // World time in seconds (Tick * FixedDeltaTime)
}
```

**Status:** Canonical time state. All systems should read from this, not `TimeState`.

#### RewindState

```csharp
/// <summary>
/// Singleton component tracking the global rewind/playback state.
/// Baseline fields support play/pause/rewind/step and minimal history settings.
/// Legacy fields are retained for compatibility.
/// </summary>
public struct RewindState : IComponentData
{
    // Canonical baseline
    public RewindMode Mode;                // Current rewind mode
    public int CurrentTick;                // Current tick (may differ from TickTimeState.Tick during playback)
    public int TargetTick;                 // Target tick for rewinding
    public float TickDuration;             // Duration of one tick (seconds)
    public int MaxHistoryTicks;            // Maximum history buffer size
    public byte PendingStepTicks;          // Pending step ticks (for step mode)
    
    // Legacy fields (kept for compatibility)
    public float PlaybackSpeed;
    public uint StartTick;
    public uint PlaybackTick;
    public float PlaybackTicksPerSecond;
    public ScrubDirection ScrubDirection;
    public float ScrubSpeedMultiplier;
    public uint RewindWindowTicks;
    public RewindTrackId ActiveTrack;
}

public enum RewindMode : byte
{
    Play = 0,       // Normal simulation (canonical)
    Paused = 1,     // Paused state
    Rewind = 2,     // Rewinding/playback (canonical)
    Step = 3,       // Catch-up mode (canonical)
    
    // Legacy aliases (mapped to canonical values)
    Record = Play,      // Alias for Play
    Playback = Rewind,  // Alias for Rewind
    CatchUp = Step,     // Alias for Step
    Idle = Paused       // Alias for Paused
}
```

**Status:** Core rewind state. Contains both canonical and legacy fields (duplication issue).

### Time API

**Static API for MonoBehaviour/UI Code:**

```csharp
public static class TimeAPI
{
    // Basic controls
    public static void Pause();
    public static void Resume();
    public static void SetSpeed(float speed);  // 0.01-16.0
    public static void StepOneTick();
    
    // Query
    public static uint GetCurrentTick();
    public static float GetCurrentScale();
    
    // Time bubbles (local time zones)
    public static Entity CreateStasisBubble(float3 position, float radius, uint durationTicks);
    public static void RemoveStasisBubble(Entity bubbleEntity);
    
    // Rewind (preview-based)
    public static void BeginPreviewRewind(float scrubSpeed);
    public static void UpdatePreviewRewindSpeed(float scrubSpeed);
    public static void EndScrubPreview();
    public static void CommitRewindFromPreview();
    public static void CancelRewindPreview();
    
    // Rewind (direct)
    public static void StartRewind(uint ticksBack);
    public static void StopRewind();
    public static void ScrubToTick(uint targetTick);
}
```

**Implementation:** Commands are queued into `TimeControlCommand` buffer on `RewindState` singleton entity.

### Command Architecture

**Time Control Commands:**

```csharp
public struct TimeControlCommand : IBufferElementData
{
    public TimeControlCommandType Type;    // Pause, Resume, SetSpeed, StartRewind, etc.
    public uint UintParam;                 // Tick count, target tick, etc.
    public float FloatParam;               // Speed multiplier, etc.
    public TimeControlScope Scope;         // Global, LocalBubble, Territory, Player
    public TimeControlSource Source;       // Player, Miracle, Scenario, DevTool, Technology, System
    public byte PlayerId;                  // Player ID (0 for single-player)
    public uint SourceId;                  // Source entity ID (miracle, tech, etc.)
    public byte Priority;                  // Priority for conflict resolution
}

public enum TimeControlCommandType : byte
{
    None = 0,
    Pause = 1,
    Resume = 2,
    StepTicks = 3,
    SetSpeed = 4,
    StartRewind = 5,
    StopRewind = 6,
    ScrubTo = 7,
    AddTimeScaleEntry = 8,
    RemoveTimeScaleEntry = 9,
    BeginPreviewRewind = 10,
    UpdatePreviewRewindSpeed = 11,
    EndScrubPreview = 12,
    CommitRewindFromPreview = 13,
    CancelRewindPreview = 14
}
```

**Processing:** Commands are processed by `RewindCoordinatorSystem` and `TimeScaleCommandSystem` in `TimeSystemGroup` (OrderFirst).

---

## How It Works

### Tick Advancement

**TimeTickSystem** (runs in `TimeSystemGroup`, OrderFirst):

1. **Read time state:**
   - `TickTimeState` (canonical)
   - `RewindState.Mode` (check if in Record/Play mode)
   - `SimulationScalars.TimeScale` (global time scale override)

2. **Skip if not in Record mode:**
   - If `RewindState.Mode != RewindMode.Record` (i.e., Playback/Step), return early
   - Tick advancement handled by rewind coordinator

3. **Calculate accumulated time:**
   ```csharp
   float deltaRealTime = elapsed - _lastRealTime;
   float baseSpeedMultiplier = math.max(0.01f, tickState.CurrentSpeedMultiplier);
   float scaledDelta = deltaRealTime * baseSpeedMultiplier * effectiveTimeScale;
   _accumulator += scaledDelta;
   ```

4. **Advance ticks (fixed timestep):**
   ```csharp
   const int maxStepsPerFrame = 4;  // Prevent spiral of death
   while (_accumulator >= fixedDt && steps < maxStepsPerFrame)
   {
       _accumulator -= fixedDt;
       tickState.Tick++;  // Monotonically increasing
       steps++;
   }
   ```

5. **Sync legacy TimeState:**
   ```csharp
   SyncLegacyTime(ref tickState, ref timeState);
   ```

**Key Properties:**
- **Deterministic:** Fixed timestep (default 1/60s), speed multipliers don't affect tick determinism
- **Bounded:** Max 4 steps per frame to prevent spiral of death
- **Monotonic:** Tick never decreases (rewind uses playback, not decrementing)

### Speed Control (Slow-Mo / Fast-Forward)

**Mechanism:**
- `TickTimeState.CurrentSpeedMultiplier` (0.01-16.0)
- Multiplies real-time delta before accumulating into fixed timestep
- **Does not affect tick determinism** (tick rate stays constant, but real-time progression speeds up/slows down)

**Command Flow:**
1. UI/MonoBehaviour calls `TimeAPI.SetSpeed(multiplier)`
2. Command queued: `TimeControlCommand { Type = SetSpeed, FloatParam = multiplier }`
3. `RewindCoordinatorSystem` processes command
4. Updates `TickTimeState.CurrentSpeedMultiplier`
5. Next frame, `TimeTickSystem` uses new multiplier

**Limits:**
- Default: 0.01 (100x slow-mo) to 16.0 (16x fast-forward)
- Configurable via `TimeControlConfig`

### Pause / Resume

**Mechanism:**
- `TickTimeState.IsPaused` flag
- `TimeTickSystem` skips tick advancement if paused
- Commands: `TimeAPI.Pause()`, `TimeAPI.Resume()`

**Step Mode:**
- `TimeAPI.StepOneTick()` advances exactly one tick while paused
- Uses `PendingStepTicks` counter in `RewindState`

### Rewind / Playback

**State Machine** (managed by `RewindCoordinatorSystem`):

**Mode: Play (Record)**
- Normal simulation
- Tick advances via `TimeTickSystem`
- Systems record history snapshots

**Mode: Rewind (Playback)**
- Simulation paused
- `RewindState.PlaybackTick` moves backward/forward toward `TargetTick`
- Systems restore state from history snapshots
- Presentation systems show "ghosts" previewing rewind position

**Mode: Step (Catch-Up)**
- After rewind reaches target, fast-forward back to present
- Or single-step forward during pause

**Preview Rewind** (incomplete):
- `RewindControlSystem` manages `RewindControlState` with phases:
  - `ScrubbingPreview`: Ghosts scrub backward through history
  - `FrozenPreview`: Ghosts frozen at preview position
  - `CommitPlayback`: Apply rewind to world state
- World stays frozen at `PresentTickAtStart` during preview
- Only ghosts update based on `PreviewTick`

**Command Flow:**
1. UI calls `TimeAPI.BeginPreviewRewind(speed)` or `TimeAPI.StartRewind(ticksBack)`
2. Command queued: `TimeControlCommand { Type = BeginPreviewRewind/StartRewind }`
3. `RewindCoordinatorSystem` processes command
4. Sets `RewindState.Mode = RewindMode.Rewind`
5. Sets `RewindState.TargetTick = currentTick - ticksBack`
6. Systems check `RewindState.Mode` and switch to playback logic

**History System:**
- Systems record snapshots into history buffers during Record mode
- Snapshot frequency based on `RewindTier`:
  - `SnapshotFull`: Every tick (critical state)
  - `SnapshotLite`: Every 10-50 ticks (coarse state)
  - `Derived`: None (recomputed from time)
- Playback systems restore state from nearest snapshot ≤ `PlaybackTick`

---

## Integration Points

### System Integration

**Rewind Guards:**
- Systems check `RewindState.Mode` before mutations
- Pattern:
  ```csharp
  var rewind = SystemAPI.GetSingleton<RewindState>();
  if (rewind.Mode != RewindMode.Record)
      return;  // Skip simulation during playback
  ```

**Rewind Guard Systems:**
- `EnvironmentRewindGuardSystem`: Guards `EnvironmentSystemGroup`
- `SpatialRewindGuardSystem`: Guards `SpatialSystemGroup`
- `GameplayRewindGuardSystem`: Guards `GameplaySystemGroup`
- `CameraInputRewindGuardSystem`: Guards `CameraInputSystemGroup`
- `HandRewindGuardSystem`: Guards `HandSystemGroup`
- `PresentationRewindGuardSystem`: Guards `PresentationSystemGroup`

**Time-Aware Systems:**
- Use `TickTimeState` (not `UnityEngine.Time`)
- All durations stored as ticks (not seconds)
- Time calculations: `uint ticksSinceOrigin = currentTick - component.OriginTick`

### History Integration

**Recording Pattern:**
- System in `HistorySystemGroup` records snapshots during Record mode
- Sample rate based on `RewindTier`
- Prune old snapshots based on `MaxHistoryTicks`

**Playback Pattern:**
- System in `HistorySystemGroup` restores state during Playback mode
- Find nearest snapshot ≤ `PlaybackTick`
- Restore component state from snapshot

**See:** `Docs/Guides/RewindIntegrationGuide.md` for detailed patterns

---

## Gaps and Limitations

### 1. Dual Time State Singletons (Legacy Duplication)

**Problem:** Two time state singletons (`TimeState` legacy + `TickTimeState` canonical) cause confusion.

**Current State:**
- `TickTimeState` is canonical (single source of truth)
- `TimeState` is synced from `TickTimeState` via `SyncLegacyTime()`
- Both exist for backward compatibility

**Impact:**
- Developers unsure which to use
- Maintenance burden (must keep both in sync)
- Potential for divergence if sync logic missed

**TODO:**
- Migrate all systems to use `TickTimeState` only
- Deprecate `TimeState` (mark obsolete, add migration warnings)
- Remove `TimeState` after full migration

### 2. Complex Rewind State Machine

**Problem:** Rewind state machine has multiple modes, legacy aliases, and incomplete preview rewind.

**Current State:**
- Three canonical modes: `Play`, `Rewind`, `Step`
- Legacy aliases: `Record = Play`, `Playback = Rewind`, `CatchUp = Step`
- Preview rewind (`RewindControlSystem`) incomplete

**Issues:**
- Mode naming confusion (Record vs Play, Playback vs Rewind)
- Preview rewind implementation incomplete (`RewindControlSystem` has TODOs)
- Legacy fields in `RewindState` (duplication with canonical fields)

**TODO:**
- Standardize on canonical mode names (remove aliases or make them deprecated)
- Complete preview rewind implementation
- Remove legacy fields from `RewindState` (or clearly mark as deprecated)

### 3. Multiplayer Support Stubbed

**Problem:** Multiplayer support is mostly stubbed with TODO markers.

**Current State:**
- `TimePlayerIds`, `TimePlayerAuthority`, `TimeMultiplayerValidation` exist but are stubs
- `TimeControlCommand.Scope` includes `Player` scope, but validation incomplete
- `TimeNetworkSyncSystem` is placeholder (logs but doesn't sync)

**TODOs in Code:**
- "TODO: In multiplayer, server will validate PlayerId and apply only to player's entities"
- "TODO: When Netcode is integrated: Broadcast TimeState, TickTimeState, RewindState to all clients"
- "TODO: Handle network latency and interpolation"

**Impact:**
- Multiplayer time control not functional
- No server authority validation
- No network synchronization

**TODO:**
- Design multiplayer time authority system (server authoritative, client prediction)
- Implement player-scoped time commands
- Integrate with Netcode for time state synchronization

### 4. Time Scale Scheduling Incomplete

**Problem:** Time scale scheduling exists but usage unclear.

**Current State:**
- `TimeScaleScheduleState`, `TimeScaleEntry` components exist
- `TimeScaleCommandSystem` processes `AddTimeScaleEntry`/`RemoveTimeScaleEntry` commands
- `TimeScaleResolutionSystem` resolves time scale from schedule

**Issues:**
- Unclear use case (scheduled time scale changes?)
- No documentation on how to use
- May be over-engineered for current needs

**TODO:**
- Document time scale scheduling use cases
- Simplify or remove if unused
- Or expand if needed for scenarios/miracles

### 5. Preview Rewind Incomplete

**Problem:** Preview rewind (`RewindControlSystem`) has incomplete implementation.

**Current State:**
- `RewindControlSystem` manages `RewindControlState` with phases
- Commands: `BeginPreviewRewind`, `UpdatePreviewRewindSpeed`, `EndScrubPreview`, `CommitRewindFromPreview`, `CancelRewindPreview`
- Implementation has debug logs and TODOs

**Issues:**
- Ghost rendering/update logic unclear
- World freezing during preview may conflict with presentation systems
- Integration with history playback systems incomplete

**TODO:**
- Complete ghost rendering/update logic
- Document preview rewind flow
- Test integration with history systems

### 6. Time Bubble (Local Time Zones) Underused

**Problem:** Time bubble system exists but may be underused.

**Current State:**
- `TimeBubbleComponents`, `TimeBubbleMembershipSystem` exist
- `TimeAPI.CreateStasisBubble()` creates local time bubbles
- Integration unclear (which systems respect bubbles?)

**Issues:**
- Unclear which systems respect time bubbles
- No documentation on use cases
- May be over-engineered if unused

**TODO:**
- Document time bubble use cases
- Verify system integration (do systems check bubble membership?)
- Simplify or expand based on actual needs

---

## Malpractices and Anti-Patterns

### 1. Using TimeState Instead of TickTimeState

**Problem:** Systems read from `TimeState` (legacy) instead of `TickTimeState` (canonical).

**Example:**
```csharp
// ❌ BAD: Using legacy TimeState
var timeState = SystemAPI.GetSingleton<TimeState>();
uint currentTick = timeState.Tick;

// ✅ GOOD: Using canonical TickTimeState
var tickState = SystemAPI.GetSingleton<TickTimeState>();
uint currentTick = tickState.Tick;
```

**Fix:** Migrate all systems to use `TickTimeState`. Mark `TimeState` as obsolete.

### 2. Using UnityEngine.Time Instead of TickTimeState

**Problem:** Systems use `UnityEngine.Time.deltaTime` instead of `TickTimeState.FixedDeltaTime`.

**Example:**
```csharp
// ❌ BAD: Frame time (non-deterministic)
float deltaTime = Time.deltaTime;

// ✅ GOOD: Fixed timestep (deterministic)
var tickState = SystemAPI.GetSingleton<TickTimeState>();
float deltaTime = tickState.FixedDeltaTime * tickState.CurrentSpeedMultiplier;
```

**Fix:** Always use `TickTimeState` for time calculations. Never use `UnityEngine.Time` in simulation systems.

### 3. Storing Durations as Seconds Instead of Ticks

**Problem:** Components store durations as `float seconds` instead of `uint ticks`.

**Example:**
```csharp
// ❌ BAD: Seconds (affected by speed multiplier)
public struct CooldownComponent : IComponentData
{
    public float CooldownSeconds;
    public float LastUsedTime;
}

// ✅ GOOD: Ticks (deterministic, speed-independent)
public struct CooldownComponent : IComponentData
{
    public uint CooldownTicks;
    public uint LastUsedTick;
}
```

**Fix:** Store all durations as ticks. Convert to seconds only for UI display.

### 4. Not Checking RewindState.Mode

**Problem:** Systems mutate state without checking if in Record mode.

**Example:**
```csharp
// ❌ BAD: Mutates during playback
public void OnUpdate(ref SystemState state)
{
    foreach (var (health) in SystemAPI.Query<RefRW<Health>>())
    {
        health.ValueRW.CurrentHP -= damage;  // Wrong during playback!
    }
}

// ✅ GOOD: Checks rewind mode
public void OnUpdate(ref SystemState state)
{
    var rewind = SystemAPI.GetSingleton<RewindState>();
    if (rewind.Mode != RewindMode.Record)
        return;  // Skip during playback
    
    foreach (var (health) in SystemAPI.Query<RefRW<Health>>())
    {
        health.ValueRW.CurrentHP -= damage;
    }
}
```

**Fix:** Always check `RewindState.Mode` before mutations. Use rewind guards for system groups.

### 5. Using RewindMode Aliases (Record/Playback)

**Problem:** Code uses legacy aliases (`RewindMode.Record`, `RewindMode.Playback`) instead of canonical names.

**Example:**
```csharp
// ❌ BAD: Using legacy alias
if (rewind.Mode == RewindMode.Record)  // Alias for Play
    return;

// ✅ GOOD: Using canonical name
if (rewind.Mode == RewindMode.Play)  // Canonical
    return;
```

**Fix:** Use canonical mode names (`Play`, `Rewind`, `Step`, `Paused`). Mark aliases as deprecated.

### 6. Command Processing Without Validation

**Problem:** Systems process `TimeControlCommand` without validating scope/player authority.

**Example:**
```csharp
// ❌ BAD: No validation
foreach (var cmd in commands)
{
    if (cmd.Type == TimeControlCommandType.SetSpeed)
    {
        tickState.CurrentSpeedMultiplier = cmd.FloatParam;  // No validation!
    }
}

// ✅ GOOD: Validates scope/authority
foreach (var cmd in commands)
{
    if (!TimeMultiplayerGuards.CheckCommandAllowed(flags, cmd, playerAuthority))
        continue;  // Reject invalid commands
    
    if (cmd.Type == TimeControlCommandType.SetSpeed)
    {
        tickState.CurrentSpeedMultiplier = math.clamp(cmd.FloatParam, minSpeed, maxSpeed);
    }
}
```

**Fix:** Always validate commands (scope, player authority, limits) before processing.

---

## Recommendations

### Short-Term (Next Sprint)

1. **Migrate to TickTimeState:**
   - Audit all systems using `TimeState`
   - Migrate to `TickTimeState`
   - Mark `TimeState` as `[Obsolete]` with migration message
   - Remove `SyncLegacyTime()` after migration complete

2. **Standardize Mode Names:**
   - Remove or deprecate legacy aliases (`Record`, `Playback`, `CatchUp`)
   - Update all code to use canonical names (`Play`, `Rewind`, `Step`, `Paused`)
   - Update documentation

3. **Complete Preview Rewind:**
   - Finish `RewindControlSystem` implementation
   - Document ghost rendering/update flow
   - Test integration with history systems

### Medium-Term (Next Quarter)

1. **Simplify RewindState:**
   - Remove legacy fields from `RewindState` (or clearly mark deprecated)
   - Consolidate duplicate fields (`CurrentTick` vs `PlaybackTick`)
   - Document field usage

2. **Multiplayer Foundation:**
   - Design multiplayer time authority system (server authoritative)
   - Implement player-scoped time commands
   - Add server validation for time commands

3. **Time Scale Scheduling:**
   - Document use cases (or remove if unused)
   - Simplify API if needed
   - Or expand for scenarios/miracles if needed

### Long-Term (Roadmap)

1. **Time Bubble Integration:**
   - Document time bubble use cases
   - Verify system integration (which systems respect bubbles?)
   - Expand or simplify based on actual needs

2. **Network Synchronization:**
   - Integrate with Netcode for time state sync
   - Handle network latency and interpolation
   - Client prediction for time commands

3. **Performance Optimization:**
   - Profile tick advancement (should be <0.1ms)
   - Optimize command processing (batch operations)
   - Cache time state reads (avoid repeated singleton queries)

---

## Related Documentation

- **Time System Advisory:** `Docs/Concepts/Core/Time_System_Advisory.md` - **⭐ Recommended tightening and hardening changes**
- **Rewind Integration Guide:** `Docs/Guides/RewindIntegrationGuide.md` - Developer guide for rewind integration
- **Runtime Lifecycle:** `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` - System ordering and time system placement
- **Determinism Checklist:** `Docs/BestPractices/DeterminismChecklist.md` - Time-related determinism requirements
- **Time Control Design:** `space4x/Docs/Conceptualization/Mechanics/TimeControl.md` - Space4X time control design

---

**For Implementers:** Focus on migrating to `TickTimeState`, standardizing mode names, and completing preview rewind  
**For Architects:** Review multiplayer time authority design and time bubble integration strategy  
**For Designers:** Consider time scale scheduling use cases and time bubble gameplay applications  
**For Reviewers:** See `Time_System_Advisory.md` for recommended tightening changes (semantic cleanup, correctness hardening)

