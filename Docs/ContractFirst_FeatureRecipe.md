# Slingshot/Launch Mechanic Recipe

**Category:** `cross-project-mechanic`  
**Feature Name:** Launch/Slingshot Mechanic  
**Contracts Touched:** LaunchRequest v1, LaunchQueueEntry v1, LauncherConfig v1  
**Determinism:** Required (rewind-safe, ScenarioRunner tested)

---

## Context & Intent

**What problem does this solve?**  
Provides a deterministic, rewind-safe mechanism for launching/throwing objects that works across both Godgame (slingshot throws) and Space4x (cargo pod launches, torpedoes, probes).

**Which games/projects are affected?**  
- PureDOTS: Core launch queue system, components, and execution systems
- Godgame: Slingshot authoring and input/collision adapters (parabolic throws)
- Space4x: Launcher authoring and input/collision adapters (straight-line launches)

**Skip sections that don't apply:**  
- [x] Contracts needed (shared data structures)
- [x] Game adapters required (both games)
- [x] ScenarioRunner test (determinism verification)

---

## Step 1: Extract Shared Invariants

**Questions answered:**
- **Shared data:** Queue of payloads, scheduled launch ticks, initial velocities, launcher state
- **Rewind-safe:** Queue state, launch ticks, projectile state (all restored on rewind)
- **Presentation:** VFX, audio, UI feedback (stays in game projects)
- **Producers:** Game adapters write `LaunchRequest` buffers
- **Consumers:** PureDOTS systems read requests, execute launches, manage queues

**Universal invariants:**
- Queue payloads for launch with scheduled tick
- Apply initial velocity at launch time
- Cooldown between launches (configurable)
- Rewind-safe queue state management

**Game-specific variations:**
- Godgame: Parabolic arc calculations (gravity), god-hand input, damage/resource scatter on collision
- Space4x: Straight-line launches (no gravity), fleet command input, cargo delivery/torpedo impact on collision

---

## Step 2: Define Contracts

**Contracts added to `PureDOTS/Docs/Contracts.md`:**

### LaunchRequest v1
- Producer: Game adapters (Godgame slingshot, Space4x launchers)
- Consumer: PureDOTS launch queue systems
- Schema: SourceEntity, PayloadEntity, LaunchTick, InitialVelocity, Flags
- Notes: Written by game adapters only in Record mode. Burst-safe.

### LaunchQueueEntry v1
- Producer: PureDOTS LaunchRequestIntakeSystem
- Consumer: PureDOTS LaunchExecutionSystem
- Schema: PayloadEntity, ScheduledTick, InitialVelocity, State enum
- Notes: Internal queue on launcher entities. Rewind-safe.

### LauncherConfig v1
- Producer: Game authoring (bakers)
- Consumer: PureDOTS launch systems
- Schema: MaxQueueSize, CooldownTicks, DefaultSpeed
- Notes: Singleton-like config per launcher entity. Set at bake time.

---

## Step 3: Implement PureDOTS Spine

### 3.1 Components & Buffers

**Location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/Launch/LaunchComponents.cs`

**Created:**
- `LaunchRequest` buffer - Incoming requests from game adapters
- `LaunchQueueEntry` buffer - Internal queue managed by PureDOTS
- `LauncherConfig` component - Configuration (MaxQueueSize, CooldownTicks, DefaultSpeed)
- `LauncherState` component - Runtime state (LastLaunchTick, QueueCount, Version)
- `LauncherTag` component - Tag marking launcher entities
- `LaunchedProjectileTag` component - Tag added to launched payloads

### 3.2 Systems

**Location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Launch/`

**Created:**
- `LaunchRequestIntakeSystem` - Drains `LaunchRequest` buffers, validates, fills `LaunchQueueEntry` buffers
- `LaunchExecutionSystem` - Processes queue entries at scheduled ticks, applies velocity, adds projectile tags
- `LaunchCleanupSystem` - Removes consumed queue entries

**System pattern:**
- All systems check `RewindState.Mode == Record` before processing
- Intake runs before execution
- Cleanup runs after execution

### 3.3 Authoring

**Location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Authoring/LaunchAuthoring.cs`

**Created:**
- `LauncherAuthoring` - Base MonoBehaviour with configurable MaxQueueSize, CooldownTicks, DefaultSpeed
- Baker adds `LauncherTag`, `LauncherConfig`, `LauncherState`, and both buffers

---

## Step 4: Wire Game Adapters

### 4.1 Game-Specific Authoring

**Godgame:** `Godgame/Assets/Scripts/Godgame/Authoring/GodgameSlingshotAuthoring.cs`
- Extends base launcher with `GodgameSlingshotTag` and `GodgameSlingshotConfig` (MaxRange, ArcHeightMultiplier)

**Space4x:** `Space4x/Assets/Scripts/Space4x/Authoring/Space4XLauncherAuthoring.cs`
- Extends base launcher with `Space4XLauncherTag` and `Space4XLauncherConfig` (LaunchType enum, MaxRange)

### 4.2 Input Adapters

**Godgame:** `Godgame/Assets/Scripts/Godgame/Adapters/Launch/GodgameSlingshotAdapter.cs`
- `GodgameSlingshotInputAdapter` - Reads god-hand input, calculates parabolic arc, writes `LaunchRequest` entries
- Helper method `QueueThrow()` computes velocity for parabolic trajectory

**Space4x:** `Space4x/Assets/Scripts/Space4x/Adapters/Launch/Space4XLauncherAdapter.cs`
- `Space4XLauncherInputAdapter` - Reads fleet/AI commands, calculates straight-line velocity, writes `LaunchRequest` entries
- Helper methods `QueueLaunch()` and `QueueDelayedLaunch()` for immediate and scheduled launches

### 4.3 Collision Adapters

**Godgame:** `GodgameSlingshotCollisionAdapter` - Processes `PhysicsCollisionEventElement` on launched projectiles, applies Godgame-specific effects (damage, resource scatter, miracle hooks)

**Space4x:** `Space4XLauncherCollisionAdapter` - Processes collision events, applies Space4x-specific effects (cargo delivery, torpedo damage, probe activation)

---

## Step 5: Determinism & ScenarioRunner

**Scenario:** `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/slingshot_launch_demo.json`
- Tests multiple launches at different ticks
- Includes pause/resume and rewind to verify determinism
- Verifies launch ticks, projectile positions, collision events

**Tests:** `PureDOTS/Packages/com.moni.puredots/Runtime/Tests/Launch/LaunchSystemTests.cs`
- Unit tests for component creation, buffer operations, state transitions
- Scenario parsing verification

---

## Step 6: Integration & Testing Notes

**Files created/modified:**
- Contracts: `PureDOTS/Docs/Contracts.md` (added 3 contract entries)
- Components: `PureDOTS/.../Runtime/Runtime/Launch/LaunchComponents.cs`
- Systems: `PureDOTS/.../Runtime/Systems/Launch/LaunchRequestIntakeSystem.cs`, `LaunchExecutionSystem.cs`
- Authoring: `PureDOTS/.../Runtime/Authoring/LaunchAuthoring.cs`
- Game Authoring: `Godgame/.../GodgameSlingshotAuthoring.cs`, `Space4x/.../Space4XLauncherAuthoring.cs`
- Adapters: `Godgame/.../GodgameSlingshotAdapter.cs`, `Space4x/.../Space4XLauncherAdapter.cs`
- Scenarios: `PureDOTS/.../Scenarios/Samples/slingshot_launch_demo.json`
- Tests: `PureDOTS/.../Tests/Launch/LaunchSystemTests.cs`

**Integration points:**
- Uses existing `TimeState` and `RewindState` singletons
- Integrates with `PhysicsCollisionEventElement` contract for collision handling
- Respects `PhysicsConfig.ProviderId` for physics backend selection

**Testing checklist:**
- [x] Contracts match implementation
- [x] Systems compile and run
- [x] Adapters wire correctly
- [x] ScenarioRunner test passes
- [x] Rewind safety verified (queue state restored on rewind)

---

## The Recipe Loop

```
1. Concept → Extract Invariants
2. Add/Update Contracts.md
3. Implement PureDOTS Spine
4. Wire Game Adapters
5. (Optional) Add ScenarioRunner Test
```

---

## Step 1: Extract Shared Invariants from Concept

Before writing any code, identify what is **universal** across games vs. what is **game-specific**.

**Questions to Ask:**
- What data must be shared? (entities, positions, velocities, states)
- What must be **rewind-safe**? (queues, scheduled actions, entity state)
- What is **pure presentation**? (VFX, audio, UI - stays in game project)
- What are the **producer/consumer** roles?

**Example - Slingshot/Launch Mechanic:**
- **Universal**: Queue payloads for launch, scheduled tick, initial velocity, cooldown
- **Rewind-safe**: Queue state, launch ticks, projectile state
- **Game-specific**: Input handling (god hand vs fleet command), collision effects (damage vs cargo delivery)

---

## Step 2: Define Contracts in Contracts.md

Add entries to `PureDOTS/Docs/Contracts.md` for each shared data structure.

**Contract Entry Format:**
```markdown
## ContractName v1

- Producer: Who writes this data (adapters, systems)
- Consumer: Who reads this data (PureDOTS systems, game adapters)
- Schema:
  - Field1 (type) - description
  - Field2 (type) - description
- Notes: Burst-safe, rewind-safe, no strings, etc.
```

**Example - Launch Contracts:**

```markdown
## LaunchRequest v1

- Producer: Game adapters (Godgame slingshot, Space4x launchers)
- Consumer: PureDOTS launch queue systems
- Schema:
  - SourceEntity (Entity) - the launcher entity
  - PayloadEntity (Entity) - the object to launch
  - LaunchTick (uint) - scheduled tick (0 = immediate)
  - InitialVelocity (float3) - velocity to apply
  - Flags (byte) - optional flags
- Notes: Written by game adapters only in Record mode. Burst-safe.

## LaunchQueueEntry v1

- Producer: PureDOTS LaunchRequestIntakeSystem
- Consumer: PureDOTS LaunchExecutionSystem
- Schema:
  - PayloadEntity (Entity)
  - ScheduledTick (uint)
  - InitialVelocity (float3)
  - State (LaunchEntryState enum: Pending, Launched, Consumed)
- Notes: Internal queue on launcher entities. Rewind-safe.
```

---

## Step 3: Implement PureDOTS Spine

Create the generic components and systems in PureDOTS.

### 3.1 Components & Buffers

**Location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/<Feature>/`

```csharp
// LaunchComponents.cs
namespace PureDOTS.Runtime.Launch
{
    // Buffer for incoming requests (written by game adapters)
    [InternalBufferCapacity(4)]
    public struct LaunchRequest : IBufferElementData
    {
        public Entity SourceEntity;
        public Entity PayloadEntity;
        public uint LaunchTick;
        public float3 InitialVelocity;
        public byte Flags;
    }

    // Internal queue (managed by PureDOTS systems)
    [InternalBufferCapacity(8)]
    public struct LaunchQueueEntry : IBufferElementData
    {
        public Entity PayloadEntity;
        public uint ScheduledTick;
        public float3 InitialVelocity;
        public LaunchEntryState State;
    }

    // Config (set at bake time)
    public struct LauncherConfig : IComponentData
    {
        public byte MaxQueueSize;
        public uint CooldownTicks;
        public float DefaultSpeed;
    }

    // Runtime state
    public struct LauncherState : IComponentData
    {
        public uint LastLaunchTick;
        public byte QueueCount;
        public uint Version;
    }
}
```

### 3.2 Systems

**Location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/<Feature>/`

**Pattern:**
1. **Intake System** - Reads requests, validates, fills queue
2. **Execution System** - Processes queue at scheduled ticks
3. **Cleanup System** - Removes consumed entries

```csharp
// LaunchRequestIntakeSystem.cs
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct LaunchRequestIntakeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rewindState = SystemAPI.GetSingleton<RewindState>();
        if (rewindState.Mode != RewindMode.Record)
            return; // Only process in Record mode

        // Drain LaunchRequest buffers → fill LaunchQueueEntry buffers
    }
}
```

### 3.3 Authoring

**Location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Authoring/`

```csharp
// LaunchAuthoring.cs
public class LauncherAuthoring : MonoBehaviour
{
    public int MaxQueueSize = 8;
    public int CooldownTicks = 10;
    public float DefaultSpeed = 10f;

    public class Baker : Baker<LauncherAuthoring>
    {
        public override void Bake(LauncherAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<LauncherTag>(entity);
            AddComponent(entity, new LauncherConfig { ... });
            AddComponent(entity, new LauncherState());
            AddBuffer<LaunchRequest>(entity);
            AddBuffer<LaunchQueueEntry>(entity);
        }
    }
}
```

---

## Step 4: Wire Game Adapters

Create game-specific adapters that translate input/AI to PureDOTS contracts.

### 4.1 Game-Specific Authoring

**Godgame:** `Godgame/Assets/Scripts/Godgame/Authoring/`

```csharp
// GodgameSlingshotAuthoring.cs
public class GodgameSlingshotAuthoring : MonoBehaviour
{
    // Godgame-specific settings
    public float MaxRange = 50f;
    public float ArcHeightMultiplier = 0.3f;

    public class Baker : Baker<GodgameSlingshotAuthoring>
    {
        public override void Bake(...)
        {
            // Add PureDOTS components
            AddComponent<LauncherTag>(entity);
            AddComponent(entity, new LauncherConfig { ... });
            AddBuffer<LaunchRequest>(entity);
            AddBuffer<LaunchQueueEntry>(entity);

            // Add Godgame-specific components
            AddComponent<GodgameSlingshotTag>(entity);
            AddComponent(entity, new GodgameSlingshotConfig { ... });
        }
    }
}
```

**Space4x:** `Space4x/Assets/Scripts/Space4x/Authoring/`

```csharp
// Space4XLauncherAuthoring.cs - Same pattern, different game-specific config
```

### 4.2 Input Adapters

**Location:** `<Game>/Assets/Scripts/<Game>/Adapters/Launch/`

**Pattern:**
1. Read game-specific input/AI commands
2. Translate to `LaunchRequest` entries
3. Write to `LaunchRequest` buffer

```csharp
// GodgameSlingshotInputAdapter.cs
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(LaunchRequestIntakeSystem))]
public partial struct GodgameSlingshotInputAdapter : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rewindState = SystemAPI.GetSingleton<RewindState>();
        if (rewindState.Mode != RewindMode.Record)
            return;

        // Read Godgame input → write LaunchRequest entries
        foreach (var (requestBuffer, ...) in SystemAPI.Query<...>().WithAll<GodgameSlingshotTag>())
        {
            // Calculate parabolic arc for god-hand throws
            // Add LaunchRequest to buffer
        }
    }
}
```

### 4.3 Collision Adapters

**Location:** `<Game>/Assets/Scripts/<Game>/Adapters/Launch/`

**Pattern:**
1. Query launched projectiles with collision events
2. Translate collisions to game-specific effects

```csharp
// GodgameSlingshotCollisionAdapter.cs
[BurstCompile]
[UpdateAfter(typeof(LaunchExecutionSystem))]
public partial struct GodgameSlingshotCollisionAdapter : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Read PhysicsCollisionEventElement on launched projectiles
        // Apply Godgame-specific effects (damage, resource scatter, etc.)
    }
}
```

---

## Step 5: (Optional) Add ScenarioRunner Test

Create a scenario JSON for determinism testing.

**Location:** `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/`

```json
{
  "scenarioId": "scenario.puredots.slingshot_launch_demo",
  "seed": 12345,
  "runTicks": 300,
  "entityCounts": [
    { "registryId": "registry.launcher", "count": 1 },
    { "registryId": "registry.projectile", "count": 3 }
  ],
  "inputCommands": [
    { "tick": 10, "commandId": "launch.queue", "payload": "0,1,10,0,5" },
    { "tick": 60, "commandId": "launch.queue", "payload": "0,2,15,5,3" },
    { "tick": 120, "commandId": "time.pause", "payload": "" },
    { "tick": 150, "commandId": "time.play", "payload": "" },
    { "tick": 180, "commandId": "launch.queue", "payload": "0,3,20,0,8" },
    { "tick": 240, "commandId": "time.rewind", "payload": "100" }
  ]
}
```

**Tests:** `PureDOTS/Packages/com.moni.puredots/Runtime/Tests/<Feature>/`

```csharp
[Test]
public void SlingshotScenario_ParsesSuccessfully()
{
    var path = "Packages/.../slingshot_launch_demo.json";
    var json = File.ReadAllText(path);
    Assert.IsTrue(ScenarioRunner.TryParse(json, out var data, out var error));
    // Verify scenario structure
}
```

---

## File Locations Summary

| What | Where |
|------|-------|
| Contracts | `PureDOTS/Docs/Contracts.md` |
| Components | `PureDOTS/.../Runtime/Runtime/<Feature>/` |
| Systems | `PureDOTS/.../Runtime/Systems/<Feature>/` |
| Base Authoring | `PureDOTS/.../Runtime/Authoring/` |
| Game Authoring | `<Game>/Assets/Scripts/<Game>/Authoring/` |
| Game Adapters | `<Game>/Assets/Scripts/<Game>/Adapters/<Feature>/` |
| Scenarios | `PureDOTS/.../Runtime/Runtime/Scenarios/Samples/` |
| Tests | `PureDOTS/.../Runtime/Tests/<Feature>/` |

---

## Worked Example: Slingshot/Launch Mechanic

The launch mechanic implementation follows this exact recipe:

1. **Contracts**: `PureDOTS/Docs/Contracts.md` - LaunchRequest v1, LaunchQueueEntry v1, LauncherConfig v1
2. **PureDOTS Spine**:
   - `Runtime/Runtime/Launch/LaunchComponents.cs` - Data structures
   - `Runtime/Systems/Launch/LaunchRequestIntakeSystem.cs` - Intake
   - `Runtime/Systems/Launch/LaunchExecutionSystem.cs` - Execution
   - `Runtime/Authoring/LaunchAuthoring.cs` - Base authoring
3. **Godgame Adapter**:
   - `Authoring/GodgameSlingshotAuthoring.cs` - Game-specific authoring
   - `Adapters/Launch/GodgameSlingshotAdapter.cs` - Input & collision adapters
4. **Space4x Adapter**:
   - `Authoring/Space4XLauncherAuthoring.cs` - Game-specific authoring
   - `Adapters/Launch/Space4XLauncherAdapter.cs` - Input & collision adapters
5. **Scenario**: `slingshot_launch_demo.json` - Determinism test scenario

---

---

## See Also

- `[PureDOTS/Docs/Recipes/Recipe_Template.md](Recipes/Recipe_Template.md)` - Template for creating new recipes
- `[PureDOTS/Docs/Recipes/README.md](Recipes/README.md)` - Recipe catalog and usage guide
- `[PureDOTS/Docs/Contracts.md](../Contracts.md)` - Contract definitions
- `[PureDOTS/Docs/INTEGRATION_GUIDE.md](../INTEGRATION_GUIDE.md)` - Integration patterns
- `[TRI_PROJECT_BRIEFING.md](../../../TRI_PROJECT_BRIEFING.md)` - Project overview

