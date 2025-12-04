# Rewind Integration Guide

This guide explains how to add rewind support to new systems in PureDOTS, following the standardized rewind contract patterns.

## Overview

PureDOTS uses a centralized time/rewind spine with clear contracts for how every module plugs into it. All systems must:

1. Check `RewindState.Mode` before mutating state
2. Use `TickTimeState` (never `UnityEngine.Time`) for time calculations
3. Classify rewind behavior using `RewindTier` enum
4. Implement history recording/playback for rewindable systems

## Core Components

### Time State Singletons

```csharp
// Read-only time state (never modify directly)
var timeState = SystemAPI.GetSingleton<TickTimeState>();
uint currentTick = timeState.Tick;
float deltaTime = timeState.FixedDeltaTime * timeState.CurrentSpeedMultiplier;

// Rewind state (check before mutations)
var rewindState = SystemAPI.GetSingleton<RewindState>();
if (rewindState.Mode == RewindMode.Playback)
    return; // Skip simulation during playback
```

### Rewind Tiers

Choose the appropriate tier for your system:

- **`RewindTier.None`**: Never rewound (VFX, particles, UI)
- **`RewindTier.Derived`**: Deterministic recomputation (orbits, wind/weather from seeds)
- **`RewindTier.SnapshotLite`**: Coarse snapshots (biomes, economic aggregates, fire cells)
- **`RewindTier.SnapshotFull`**: Full history (combat stats, positions, AI state, inventories)

## Standard System Pattern

### Basic System Template

```csharp
[BurstCompile]
public partial struct SomeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 1. Check rewind state
        var rewind = SystemAPI.GetSingleton<RewindState>();
        if (rewind.Mode == RewindMode.Playback)
            return; // Skip simulation during playback
        
        // 2. Read time state
        var time = SystemAPI.GetSingleton<TickTimeState>();
        uint currentTick = time.Tick;
        float deltaTime = time.FixedDeltaTime * time.CurrentSpeedMultiplier;

        // 3. Main logic using ticks (not real seconds)
        foreach (var (component) in SystemAPI.Query<RefRW<SomeComponent>>())
        {
            // All durations stored as uint ticks
            // Decays multiply by (currentTick - originTick)
            uint ticksSinceOrigin = currentTick - component.ValueRO.OriginTick;
            float decayFactor = 1f - (ticksSinceOrigin * component.ValueRO.DecayRatePerTick);
            
            // Update component state
        }
    }
}
```

### History Recording System

For systems that need rewind support, add a matching recording system:

```csharp
[BurstCompile]
[UpdateInGroup(typeof(HistorySystemGroup))]
public partial struct SomeHistoryRecordSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TickTimeState>();
        state.RequireForUpdate<RewindState>();
        state.RequireForUpdate<HistorySettings>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var rewind = SystemAPI.GetSingleton<RewindState>();
        
        // Only record during Record mode
        if (rewind.Mode != RewindMode.Record)
            return;

        var time = SystemAPI.GetSingleton<TickTimeState>();
        var historySettings = SystemAPI.GetSingleton<HistorySettings>();
        
        // Determine sample rate based on RewindTier
        uint strideTicks = GetStrideTicks(historySettings);
        if (time.Tick % strideTicks != 0)
            return;

        // Record history samples
        foreach (var (component, historyBuffer) in SystemAPI.Query<
                     RefRO<SomeComponent>, 
                     DynamicBuffer<SomeHistorySample>>())
        {
            historyBuffer.Add(new SomeHistorySample
            {
                Tick = time.Tick,
                // Capture component state
                Value = component.ValueRO.Value,
                Flags = component.ValueRO.Flags
            });
            
            // Prune old samples
            PruneOldSamples(historyBuffer, time.Tick, historySettings);
        }
    }

    private static uint GetStrideTicks(in HistorySettings settings)
    {
        // SnapshotFull: every tick or every few ticks
        // SnapshotLite: every 10-50 ticks
        // Derived: N/A (no recording)
        var strideSeconds = settings.CriticalStrideSeconds; // or DefaultStrideSeconds for Lite
        var ticksPerSecond = settings.DefaultTicksPerSecond;
        return (uint)math.max(1, math.round(strideSeconds * ticksPerSecond));
    }

    private static void PruneOldSamples(
        DynamicBuffer<SomeHistorySample> buffer, 
        uint currentTick, 
        in HistorySettings settings)
    {
        var horizonTicks = (uint)math.round(
            settings.DefaultHorizonSeconds * settings.DefaultTicksPerSecond);
        var cutoffTick = currentTick >= horizonTicks ? currentTick - horizonTicks : 0;

        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            if (buffer[i].Tick < cutoffTick)
            {
                buffer.RemoveAt(i);
            }
        }
    }
}
```

### History Playback System

Add a matching playback system:

```csharp
[BurstCompile]
[UpdateInGroup(typeof(HistorySystemGroup))]
public partial struct SomeHistoryPlaybackSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<RewindState>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var rewind = SystemAPI.GetSingleton<RewindState>();
        
        // Only run during Playback mode
        if (rewind.Mode != RewindMode.Playback)
            return;

        // Restore state from history samples
        foreach (var (component, historyBuffer) in SystemAPI.Query<
                     RefRW<SomeComponent>, 
                     DynamicBuffer<SomeHistorySample>>())
        {
            var sample = FindSampleForTick(historyBuffer, rewind.PlaybackTick);
            if (sample.Tick != 0) // Found valid sample
            {
                // Restore component state
                component.ValueRW.Value = sample.Value;
                component.ValueRW.Flags = sample.Flags;
            }
        }
    }

    private static SomeHistorySample FindSampleForTick(
        DynamicBuffer<SomeHistorySample> buffer, 
        uint targetTick)
    {
        // Find sample <= targetTick, nearest one
        SomeHistorySample best = default;
        uint bestTick = 0;

        for (int i = 0; i < buffer.Length; i++)
        {
            var sample = buffer[i];
            if (sample.Tick <= targetTick && sample.Tick > bestTick)
            {
                best = sample;
                bestTick = sample.Tick;
            }
        }

        return best;
    }
}
```

## History Sample Struct Pattern

Define a history sample struct for your component:

```csharp
public struct SomeHistorySample : IBufferElementData
{
    public uint Tick;
    // Capture only what's needed to reconstruct gameplay state
    public float Value;
    public byte Flags;
    // Keep it small - record aggregates, not every detail
}
```

## Authoring Integration

Add `RewindImportance` component to entities during authoring:

```csharp
public sealed class SomeEntityBaker : Baker<SomeEntityAuthoring>
{
    public override void Bake(SomeEntityAuthoring authoring)
    {
        var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        
        // Add rewind importance tier
        AddComponent(entity, new RewindImportance
        {
            Tier = RewindTier.SnapshotFull // or SnapshotLite, Derived, None
        });
        
        // Add history buffer if rewindable
        if (authoring.rewindTier != RewindTier.None && 
            authoring.rewindTier != RewindTier.Derived)
        {
            AddBuffer<SomeHistorySample>(entity);
        }
    }
}
```

## Tier Selection Guidelines

### SnapshotFull (Critical Gameplay State)

Use for:
- Combat-critical: HP, position/velocity, task/AI phase, orders, resource inventories
- Narrative-critical: heroes, key villagers, fleets, unique derelicts
- High-level aggregates: village/colony/faction state

Sample rate: Every tick or every few ticks

Examples:
- `VillagerHistorySample` - position, needs, job state
- `CombatHistorySample` - HP, morale, engagement state
- `AIHistorySample` - behavior mode, task phase, current action

### SnapshotLite (Coarse State)

Use for:
- Fire/epidemic spread at cell level (not ember level)
- Coarse biome state, soil fertility, pollution
- Economic aggregates: stockpiles, prices

Sample rate: Every 10-50 ticks (configurable)

Examples:
- `StorehouseHistorySample` - inventory totals, capacity
- `VegetationHistorySample` - growth progress, lifecycle stage
- `GridHistorySample` - cell-level state summaries

### Derived (Deterministic Recomputation)

Use for:
- Galaxy & orbit positions (analytic/orbits)
- Wind/weather fields (seeded noise + deterministic integration)
- Ambient happiness if pure function of buildings/morale

Sample rate: N/A (recomputed on demand)

Pattern:
```csharp
// Store seed/parameters, recompute from time
public struct OrbitParams : IComponentData
{
    public float SemiMajorAxis;
    public float Eccentricity;
    public double EpochSeconds;
}

// Position = f(orbitParams, TickTimeState.WorldSeconds)
// No history needed - fully deterministic
```

### None (Ignored)

Use for:
- Pure VFX, particles
- UI-only stuff

No history recording needed.

## Time Calculations

### Always Use Ticks

```csharp
// ✅ Good: Tick-based
uint ticksSinceOrigin = currentTick - component.OriginTick;
float decayFactor = 1f - (ticksSinceOrigin * decayRatePerTick);

// ❌ Bad: Real-time seconds
float timeSinceOrigin = Time.time - component.OriginTime;
```

### Sub-Sampling by Tick

For heavy systems, use `NextUpdateTick` fields:

```csharp
public struct SomeComponent : IComponentData
{
    public uint NextUpdateTick; // When to update next
    public uint UpdateIntervalTicks; // How often (e.g., 10 ticks)
}

// In system:
if (currentTick >= component.NextUpdateTick)
{
    // Do expensive update
    component.NextUpdateTick = currentTick + component.UpdateIntervalTicks;
}
```

## Common Patterns

### Pattern 1: Simple State Recording

```csharp
// Component
public struct Health : IComponentData
{
    public float CurrentHP;
    public float MaxHP;
}

// History sample
public struct HealthHistorySample : IBufferElementData
{
    public uint Tick;
    public float HP;
}

// Recording: capture HP every tick
// Playback: restore HP from nearest sample
```

### Pattern 2: Aggregate State

```csharp
// Component
public struct VillageState : IComponentData
{
    public int Population;
    public float StockpileFood;
    public float StockpileFuel;
}

// History sample
public struct VillageHistorySample : IBufferElementData
{
    public uint Tick;
    public int Population;
    public float StockpileFood;
    public float StockpileFuel;
    public byte StatusFlags;
}

// Recording: sample every 10-50 ticks (SnapshotLite)
// Playback: restore aggregates from nearest sample
```

### Pattern 3: Event-Based History

```csharp
// Component
public struct FireCell : IComponentData
{
    public byte BurnState; // None/Burning/Smoldering/Burned
    public float Heat;
    public uint LastUpdateTick;
}

// History sample
public struct FireCellHistorySample : IBufferElementData
{
    public uint Tick;
    public byte BurnState;
    public float Heat;
}

// Recording: sample cell state every 20 ticks
// Playback: restore cell state, optionally re-simulate diffusion for last N ticks
```

## Checklist

When adding rewind to a new system:

- [ ] System checks `RewindState.Mode` before mutations
- [ ] System uses `TickTimeState` (not `UnityEngine.Time`)
- [ ] All time calculations use ticks, not real seconds
- [ ] `RewindImportance` component added to entities (if rewindable)
- [ ] History sample struct defined (if SnapshotFull or SnapshotLite)
- [ ] History recording system created (if SnapshotFull or SnapshotLite)
- [ ] History playback system created (if SnapshotFull or SnapshotLite)
- [ ] History buffers added during authoring (if rewindable)
- [ ] Sample rate chosen based on tier (every tick for Full, 10-50 for Lite)
- [ ] Pruning logic implemented to respect memory budget
- [ ] Documentation updated with Rewind Integration section

## Rewind Track System

The rewind track system enables domain-specific rewind lanes with configurable tiers, scopes, and sampling rates. Each track represents a "kind of thing" you might rewind (combat, villages, fire, ships, etc.).

### Track Components

```csharp
// Track identifier (0-255, assigned by modders/content)
public struct RewindTrackId { public byte Value; }

// Track definition (stored in blob asset)
public struct RewindTrackDef
{
    public RewindTrackId Id;
    public FixedString32Bytes Name; // "Combat", "Village", "Fire", etc.
    public RewindTier Tier;
    public uint RecordEveryTicks;   // 1 = every tick, 10 = every 10 ticks
    public uint WindowTicks;        // How far back we can go on this track
    public bool Spatial;            // true = only entities in zones are recorded
}

// Singleton holding merged track configs
public struct RewindConfigSingleton : IComponentData
{
    public BlobAssetReference<RewindConfigBlob> Config;
}
```

### Spatial Zones

For local rewinds / time pockets:

```csharp
// Defines a spatial zone ("bubble") for local rewinds
public struct RewindZone : IComponentData
{
    public RewindTrackId Track;
    public float3 Center;
    public float Radius;
}

// Marks which track an entity participates in, and optionally which zone
public struct RewindScope : IComponentData
{
    public RewindTrackId Track;
    public Entity Zone; // Entity.Null = global, otherwise points to RewindZone entity
}
```

### Track-Based Recording Pattern

Use `RewindUtil` helpers for track-aware recording:

```csharp
[BurstCompile]
public partial struct DomainHistoryRecordSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rewindState = SystemAPI.GetSingleton<RewindState>();
        if (rewindState.Mode == RewindMode.Playback)
            return;

        var time = SystemAPI.GetSingleton<TickTimeState>();
        var configSingleton = SystemAPI.GetSingleton<RewindConfigSingleton>();
        ref var config = ref configSingleton.Config.Value;

        var trackId = new RewindTrackId { Value = 1 }; // Your domain's track
        if (!RewindUtil.ShouldRecordTrack(config, trackId, time.Tick))
            return;

        foreach (var (tag, component, history, scope) in SystemAPI.Query<
                     RefRO<DomainRewindTag>,
                     RefRO<DomainComponent>,
                     DynamicBuffer<DomainHistoryElement>,
                     RefRO<RewindScope>>())
        {
            // Filter by track
            if (scope.ValueRO.Track.Value != trackId.Value)
                continue;

            // Optional: spatial filtering
            ref var trackDef = ref RewindUtil.GetTrackDef(config, trackId);
            if (trackDef.Spatial && scope.ValueRO.Zone != Entity.Null)
            {
                // Check if entity is within zone radius
                // ... spatial check logic ...
            }

            // Record snapshot
            history.Add(new DomainHistoryElement
            {
                Tick = time.Tick,
                Snapshot = CaptureSnapshot(component.ValueRO)
            });

            // Trim old snapshots
            RewindUtil.TrimHistory(history, time.Tick, trackDef.WindowTicks);
        }
    }
}
```

### Track-Based Playback Pattern

```csharp
[BurstCompile]
public partial struct DomainHistoryPlaybackSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rewindState = SystemAPI.GetSingleton<RewindState>();
        if (rewindState.Mode != RewindMode.Playback)
            return;

        var activeTrack = rewindState.ActiveTrack;
        uint targetTick = rewindState.PlaybackTick;

        foreach (var (tag, component, history, scope) in SystemAPI.Query<
                     RefRO<DomainRewindTag>,
                     RefRW<DomainComponent>,
                     DynamicBuffer<DomainHistoryElement>,
                     RefRO<RewindScope>>())
        {
            if (scope.ValueRO.Track.Value != activeTrack.Value)
                continue;

            // Find latest snapshot <= targetTick
            int bestIndex = -1;
            for (int i = history.Length - 1; i >= 0; i--)
            {
                if (history[i].Tick <= targetTick)
                {
                    bestIndex = i;
                    break;
                }
            }

            if (bestIndex >= 0)
            {
                RestoreFromSnapshot(ref component.ValueRW, history[bestIndex].Snapshot);
            }
        }
    }
}
```

### Configuring Tracks

Create a `RewindConfigAsset` ScriptableObject:

1. Create asset: `Assets/.../RewindConfig.asset`
2. Add track definitions with:
   - Track ID (0-255)
   - Name (e.g., "Combat", "Village")
   - Tier (Derived/Lite/Full)
   - RecordEveryTicks (sampling rate)
   - WindowTicks (rewind horizon)
   - Spatial flag

3. Add `RewindConfigAuthoring` component to a GameObject in your scene
4. Assign the config asset or add inline tracks

The `RewindConfigBootstrapSystem` merges all configs into a single blob at startup.

### Example: PosHp Domain

See `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Time/Templates/PosHpRewindExample.cs` for a complete template showing:
- Tag component (`PosHpRewindTag`)
- Snapshot type (`PosHpSnapshot`)
- History element (`PosHpHistoryElement`)
- Record system (`PosHpHistoryRecordSystem`)
- Playback system (`PosHpHistoryPlaybackSystem`)

Copy this pattern and replace:
- Tag type → Your domain tag
- Snapshot type → Your domain snapshot
- History element → Your domain history element
- Query components → Your domain components

### Track System Checklist

When adding track-based rewind to a domain:

- [ ] Define `RewindTrackId` for your domain (or reuse existing)
- [ ] Add `RewindScope` component to entities (set Track and optional Zone)
- [ ] Create domain tag component (e.g., `CombatRewindTag`)
- [ ] Create snapshot struct with minimal fields needed
- [ ] Create history element struct implementing `IHistoryElementWithTick`
- [ ] Implement record system using `RewindUtil.ShouldRecordTrack()` and `RewindUtil.TrimHistory()`
- [ ] Implement playback system filtering by `RewindState.ActiveTrack`
- [ ] Configure track in `RewindConfigAsset` or via `RewindConfigAuthoring`
- [ ] Test global and spatial rewinds

## References

- **Contracts**: `PureDOTS/Docs/Contracts.md` - Rewind Integration Contract v1
- **AI Example**: `PureDOTS/Docs/Mechanics/AIBehaviorModules.md` - Rewind Integration section
- **Combat Example**: `PureDOTS/Docs/Mechanics/CombatLoop.md` - Rewind Integration section
- **Construction Example**: `PureDOTS/Docs/Mechanics/ConstructionLoop.md` - Rewind Integration section
- **History Components**: `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/HistoryComponents.cs`
- **Rewind Components**: `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/Time/RewindComponents.cs`
- **Track Components**: `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/Time/RewindTrackComponents.cs`
- **Zone Components**: `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/Time/RewindZoneComponents.cs`
- **RewindUtil**: `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Time/RewindUtil.cs`
- **Template Example**: `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Time/Templates/PosHpRewindExample.cs`

