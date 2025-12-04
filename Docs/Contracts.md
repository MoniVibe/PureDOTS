# PureDOTS Contracts

Simple breadcrumbs for shared systems. When you add or change a contract, update this file.

## TimeControlCommand v1

- Producer: Time control systems (PureDOTS)
- Consumer: Godgame, Space4x time/rewind systems
- Schema:
  - Type (TimeControlCommandType enum)
  - UintParam (tick count, target tick, etc.)
  - FloatParam (speed multiplier, etc.)
  - Scope (Global, LocalBubble, Territory, Player)
  - Source (Player, Miracle, Scenario, DevTool, Technology, System)
  - PlayerId (byte, 0 for SP)
  - SourceId (uint, origin entity ID)
  - Priority (byte, conflict resolution)
- Notes: Must be rewind-safe, Burst-safe. No strings. Processed by RewindCoordinatorSystem and TimeScaleCommandSystem.

## PhysicsCollisionEvent v1

- Producer: Physics/Collision systems (PureDOTS)
- Consumer: Godgame, Space4x combat/presentation
- Schema:
  - OtherEntity (Entity)
  - ContactPoint (float3)
  - ContactNormal (float3)
  - Impulse (float)
  - Tick (uint)
  - EventType (PhysicsCollisionEventType: Collision, TriggerEnter, TriggerExit)
- Notes: Must be rewind-safe, Burst-safe. No strings. Added to entities with RequiresPhysics component and PhysicsCollisionEventElement buffer.

## InputCommandLogEntry v1

- Producer: Input systems (PureDOTS)
- Consumer: Godgame, Space4x input handling
- Schema:
  - Tick (uint)
  - Type (byte)
  - FloatParam (float)
  - UintParam (uint)
- Notes: Ring buffer entry for time control command logging. Burst-safe.

## SpatialGridProvider v1

- Producer: Spatial systems (PureDOTS)
- Consumer: Godgame, Space4x spatial queries
- Interface: ISpatialGridProvider
- Implementations: HashedSpatialGridProvider, UniformSpatialGridProvider
- Notes: Provider pattern allows swapping spatial grid implementations. Config-driven selection.

## PhysicsProvider v1

- Producer: Physics systems (PureDOTS)
- Consumer: Godgame, Space4x physics/collision systems
- Interface: IPhysicsProvider
- Implementations: NoPhysicsProvider (ID=0), EntitiesPhysicsProvider (ID=1), HavokPhysicsProvider (ID=2, stub)
- Schema:
  - ProviderId (byte in PhysicsConfig: None=0, Entities=1, Havok=2)
  - Step(float deltaTime, ref PhysicsWorld world)
  - GetCollisionEvents(Allocator) -> NativeArray<CollisionEvent>
  - GetTriggerEvents(Allocator) -> NativeArray<TriggerEvent>
- Notes: Provider pattern allows swapping physics backends. Currently only Entities (Unity Physics) is fully implemented. PhysicsEventSystem processes events when ProviderId=Entities. Games select provider via PhysicsConfig.

## LaunchRequest v1

- Producer: Game adapters (Godgame slingshot, Space4x launchers)
- Consumer: PureDOTS launch queue systems
- Schema:
  - SourceEntity (Entity) - the launcher entity
  - PayloadEntity (Entity) - the object to launch
  - LaunchTick (uint) - scheduled tick for launch (0 = immediate)
  - InitialVelocity (float3) - velocity to apply at launch
  - Flags (byte) - optional flags (reserved)
- Notes: Written by game adapters only in Record mode. Burst-safe, no strings. Consumed by LaunchRequestIntakeSystem.

## LaunchQueueEntry v1

- Producer: PureDOTS LaunchRequestIntakeSystem
- Consumer: PureDOTS LaunchExecutionSystem
- Schema:
  - PayloadEntity (Entity)
  - ScheduledTick (uint)
  - InitialVelocity (float3)
  - State (LaunchEntryState enum: Pending, Launched, Consumed)
- Notes: Internal queue on launcher entities. Rewind-safe (state restored on rewind). Burst-safe.

## LauncherConfig v1

- Producer: Game authoring (bakers)
- Consumer: PureDOTS launch systems
- Schema:
  - MaxQueueSize (byte) - max pending launches
  - CooldownTicks (uint) - ticks between launches
  - DefaultSpeed (float) - default launch speed if not specified
- Notes: Singleton-like config per launcher entity. Set at bake time.

## Rewind Integration Contract v1

- Producer: All PureDOTS systems
- Consumer: Rewind/playback systems, time control systems
- Core Components:
  - `TickTimeState` (singleton): CurrentTick, FixedDeltaTime, CurrentSpeedMultiplier, IsPaused
  - `RewindState` (singleton): Mode (Record/Playback/CatchUp/Idle), CurrentTick, PlaybackTick, TargetTick
  - `RewindTier` enum: None=0, Derived=1, SnapshotLite=2, SnapshotFull=3
  - `RewindImportance` component: Tier (RewindTier) - marks entity's rewind tier
  - `TimeControlCommand` buffer: Type, UintParam, FloatParam, Scope, Source, PlayerId, SourceId, Priority
- Notes:
  - All systems must check `RewindState.Mode` before mutating state
  - Systems skip execution during `RewindMode.Playback` (or use special playback path)
  - All time calculations use ticks, not real seconds
  - History samples keyed by tick (uint)
  - `RewindMode.Record` = normal forward simulation (equivalent to spec's "Live")
  - `TimeControlCommand` structure is more complex than spec (supports multiplayer via Scope/PlayerId)

### System Rewind Tier Mapping

| Component/System | RewindTier | History Struct | Sample Rate | Playback Strategy |
|-----------------|------------|----------------|-------------|-------------------|
| AI Behavior | SnapshotFull | `AIHistorySample` | Every tick | Restore from buffer |
| Villagers | SnapshotFull | `VillagerHistorySample`, `VillagerJobHistorySample` | Configurable stride (1-5s) | Restore from buffer |
| Combat Groups | SnapshotFull | `CombatHistorySample` | Every tick | Restore HP, position, morale, engagement state |
| Storehouses | SnapshotLite | `StorehouseHistorySample` | Every 10-50 ticks | Restore aggregates (inventory totals) |
| Vegetation | SnapshotLite | `VegetationHistorySample` | Every 100+ ticks | Restore growth state, lifecycle stage |
| Construction | SnapshotLite | `ConstructionHistorySample` | Every 10-50 ticks | Restore build progress, worker count |
| Resources/Piles | SnapshotLite | `ResourceHistorySample`, `PileHistorySample` | Every 10-50 ticks | Restore units remaining, position |
| Divine Hand | SnapshotFull | `HandHistorySample`, `InteractionHistorySample` | Every tick | Restore cursor position, held object, state |
| Grid/Environment | SnapshotLite | `GridHistorySample` | Every 20-100 ticks | Restore cell-level state (fire, pollution, biome) |
| Wind/Weather | Derived | None (seed-based) | N/A | Recompute from seed + tick (deterministic noise) |
| Galaxy/Orbits | Derived | None (analytic) | N/A | Recompute from `OrbitParams` + `TickTimeState.WorldSeconds` |
| VFX/Particles | None | None | N/A | Ignored during rewind |
| UI | None | None | N/A | Ignored during rewind |

### System Template Pattern

All systems must follow this pattern:

```csharp
[BurstCompile]
public partial struct SomeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rewind = SystemAPI.GetSingleton<RewindState>();
        if (rewind.Mode == RewindMode.Playback)
            return; // Skip simulation during playback
        
        // Alternative: Special playback path
        // if (rewind.Mode == RewindMode.Playback)
        // {
        //     // Restore from history samples
        //     return;
        // }

        var time = SystemAPI.GetSingleton<TickTimeState>();
        uint currentTick = time.Tick;
        float deltaTime = time.FixedDeltaTime * time.CurrentSpeedMultiplier;

        // Main logic using currentTick and deltaTime
        // All durations stored as uint ticks or ushort
        // Decays multiply by (currentTick - originTick)
    }
}
```

For rewindable systems, add matching `*HistoryRecordSystem` and `*HistoryPlaybackSystem`:

```csharp
// Recording system (runs only in Record mode)
[BurstCompile]
[UpdateInGroup(typeof(HistorySystemGroup))]
public partial struct SomeHistoryRecordSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rewind = SystemAPI.GetSingleton<RewindState>();
        if (rewind.Mode != RewindMode.Record)
            return;

        var time = SystemAPI.GetSingleton<TickTimeState>();
        uint strideTicks = GetStrideTicks(); // Based on RewindTier
        
        if (time.Tick % strideTicks != 0)
            return;

        // Append history samples
        foreach (var (component, historyBuffer) in SystemAPI.Query<RefRO<SomeComponent>, DynamicBuffer<SomeHistorySample>>())
        {
            historyBuffer.Add(new SomeHistorySample
            {
                Tick = time.Tick,
                // ... capture component state
            });
            
            PruneOldSamples(historyBuffer, time.Tick);
        }
    }
}

// Playback system (runs only in Playback mode)
[BurstCompile]
[UpdateInGroup(typeof(HistorySystemGroup))]
public partial struct SomeHistoryPlaybackSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rewind = SystemAPI.GetSingleton<RewindState>();
        if (rewind.Mode != RewindMode.Playback)
            return;

        // Find sample <= PlaybackTick and restore state
        foreach (var (component, historyBuffer) in SystemAPI.Query<RefRW<SomeComponent>, DynamicBuffer<SomeHistorySample>>())
        {
            var sample = FindSampleForTick(historyBuffer, rewind.PlaybackTick);
            // Restore component state from sample
        }
    }
}
```

### System Compliance

Systems that already check `RewindState.Mode`:

- `VillagerHistorySystem` - checks `Mode != Record` before recording
- `VillagerJobSystems` - checks `Mode != Playback` before processing
- `PhysicsEventSystem` - checks `Mode == Playback` to skip physics
- `PhysicsSyncSystem` - checks `Mode == Playback` to skip sync
- `ModuleRepairSystem` - checks `Mode != Record` before repair
- `MobilityPathSystem` - checks `Mode != Record` before pathfinding
- `TradeOpportunitySystem` - checks `Mode != Record` before trade
- `StreamingLoaderSystem` - checks `Mode != Record` before loading
- `CompanionPresentationSyncSystem` - checks `Mode != Record` before sync
- `PlaceholderVisualSystems` - checks `Mode != Record` before rendering
- `RewindTelemetrySystem` - checks `Mode == Playback` before telemetry
- `TimeHistoryRecordSystem` - checks `Mode == Record` before recording
- `TimeHistoryPlaybackSystem` - checks `Mode == Playback` before playback
- `WorldSnapshotPlaybackSystem` - checks `Mode == Playback || CatchUp` before playback

### Rewind Tier Guidelines

**SnapshotFull** (critical gameplay state):
- Combat-critical: HP, position/velocity, task/AI phase, orders, resource inventories
- Narrative-critical: heroes, key villagers, fleets, unique derelicts
- High-level aggregates: village/colony/faction state
- Sample rate: Every tick or every few ticks

**SnapshotLite** (coarse state):
- Fire/epidemic spread at cell level (not ember level)
- Coarse biome state, soil fertility, pollution
- Economic aggregates: stockpiles, prices
- Sample rate: Every 10-50 ticks (configurable)

**Derived** (deterministic recomputation):
- Galaxy & orbit positions (analytic/orbits)
- Wind/weather fields (seeded noise + deterministic integration)
- Ambient happiness if pure function of buildings/morale
- Sample rate: N/A (recomputed on demand)

**None** (ignored):
- Pure VFX, particles
- UI-only stuff
- Sample rate: N/A

