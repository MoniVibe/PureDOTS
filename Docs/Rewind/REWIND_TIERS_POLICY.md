# Rewind Tiers Policy

**Last Updated**: 2025-01-XX  
**Maintainer**: PureDOTS Framework Team

---

## Overview

This document formalizes the rewind architecture for deterministic DOTS simulation with headless harness. It defines three tiers of state management: deterministic resimulation (Tier A), snapshot ring buffers (Tier B), and derived caches (Tier C).

**Goal**: Ensure render==sim parity under rewind while minimizing memory footprint.

---

## Tier A: Deterministic Resimulation (Input/Event Logs)

**Strategy**: Record inputs and events; replay deterministically to rebuild state.

### Components

- **`ScenarioInputLog`** (`Runtime/Scenarios/ScenarioInputLog.cs`): Per-tick log of agent decisions and player inputs
  - `ScenarioInputLogEntry`: Tick, agent ID, decision type, decision data, hash
  - Used by `ScenarioInputRecorder` (`Systems/Scenarios/ScenarioInputRecorder.cs`)
  - Validated via `ScenarioInputReplay.ValidateReplay()`

- **`InputCommandLogEntry`** (`Runtime/TimeLoggingComponents.cs`): Ring buffer for time control commands
  - Records pause/resume/speed/rewind/step commands
  - Managed by `TimeLogUtility.AppendCommand()`
  - Capacity controlled by `TimeLogSettings.CommandLogSeconds` (default: 30s)

- **`ReplayableEvent`** (`Runtime/HistoryComponents.cs`): Structured events (damage, spawn, destroy, state changes)
  - Used for cross-system event replay
  - Stored in entity buffers with tick-based ordering

### When to Use Tier A

- **Player inputs**: Mouse clicks, keyboard commands, hand interactions
- **AI decisions**: GOAP actions, utility choices, intent commitments
- **External events**: Spawn requests, destruction triggers, state machine transitions
- **Time control**: Pause/resume/speed/rewind commands

### Determinism Requirements

- All inputs must be timestamped with simulation tick (not frame time)
- RNG state must be seeded and deterministic (`Unity.Mathematics.Random` with seed)
- Event ordering must be stable (sort by Entity.Index/Version if needed)
- Hash validation via `ScenarioInputLogHelper.ComputeHash()` for quick mismatch detection

### Memory Budget

- `TimeLogSettings.CommandLogSeconds`: Default 30 seconds retention
- `HistorySettings.EventLogRetentionSeconds`: Default 30 seconds
- Ring buffer capacity: `seconds * ticksPerSecond` (e.g., 30s * 90 TPS = 2700 entries)

---

## Tier B: Per-Tick Snapshot Ring Buffers (Critical Compact State)

**Strategy**: Store compact snapshots at fixed intervals; restore on rewind.

### Components

- **`WorldSnapshotSystem`** (`Systems/WorldSnapshotSystem.cs`): Global checkpoint system
  - Captures entities with `WorldSnapshotIncludeTag` or `RewindableTag`
  - Ring buffer: `WorldSnapshotMeta` + `WorldSnapshotData` buffers
  - Interval: `WorldSnapshotState.SnapshotIntervalTicks` (default: 30 ticks ≈ 0.5s at 60 TPS)
  - Memory budget: `WorldSnapshotState.MemoryBudgetBytes` (default: 256 MB)
  - Checksum: FNV-1a hash stored in `WorldSnapshotMeta.Checksum` for integrity validation

- **Per-Entity History Buffers** (`Runtime/HistoryComponents.cs`):
  - `PositionHistorySample`: Transform history (position, rotation)
  - `HealthHistorySample`: Health/max health snapshots
  - `VillagerHistorySample`: Comprehensive villager state (position, health, hunger, energy, morale, job, flags)
  - `ResourceHistorySample`: Resource amounts and flags
  - `VegetationHistorySample`: Growth progress, scale, lifecycle stage
  - `ConstructionHistorySample`: Build progress, worker count, completion state
  - `StorehouseHistorySample`: Queue counts, capacity, deposit ticks
  - `HandHistorySample`: Cursor position, held object, slingshot charge, aim direction
  - `InteractionHistorySample`: Hand state, command type, resource payloads
  - `CombatHistorySample`: Group state, faction, formation, morale, engagement state
  - `GridHistorySample`: Terrain version + statistical summaries (min/avg/max)

- **`TickSnapshotLogEntry`** (`Runtime/TimeLoggingComponents.cs`): Debug timeline of rewind state transitions
  - Records tick, target tick, play/pause state, rewind mode, playback position
  - Managed by `TickSnapshotLogSystem`
  - Capacity: `TimeLogSettings.SnapshotLogSeconds` (default: 30s)

### When to Use Tier B

- **Critical state**: Combat stats, positions, AI phase, orders, inventories
- **Expensive to recompute**: Complex state machines, multi-step processes
- **Non-deterministic sources**: External inputs that can't be replayed
- **Checkpoint anchors**: Coarse snapshots for fast rewind to distant points

### Snapshot Frequency

Controlled by `HistoryTier` component:
- **Critical**: `HistorySettings.CriticalStrideSeconds` (default: 1s)
- **Default**: `HistorySettings.DefaultStrideSeconds` (default: 5s)
- **LowVisibility**: `HistorySettings.LowVisibilityStrideSeconds` (default: 30s)

Per-entity buffers pruned based on `HistorySettings.DefaultHorizonSeconds` (default: 60s).

### Memory Budget

- **Per-entity buffers**: Pruned when exceeding `HistorySettings.MaxMemoryPerEntityBytes` (default: 1 MB)
- **Global snapshots**: Ring buffer of `WorldSnapshotState.MaxSnapshots` (default: 100)
- **Total budget**: `HistorySettings.MemoryBudgetBytes` (default: 2 GB)
- **Enforcement**: `HistorySettings.EnforceStrictMemoryLimits` (default: false)

### Checksum Validation

`WorldSnapshotSystem` computes FNV-1a checksum over serialized snapshot data:
- Stored in `WorldSnapshotMeta.Checksum`
- Validates snapshot integrity during restore
- Used by `ScenarioRunRecorder` for determinism validation

---

## Tier C: Derived Caches (Rebuildable, Not Snapshotted)

**Strategy**: Rebuild from deterministic sources (seed + time + version markers).

### Examples

- **`TerrainDerivedNavCacheSystem`** (`Systems/Environment/TerrainDerivedNavCacheSystem.cs`):
  - Rebuilds navigation tiles/chunks from terrain version
  - Triggered by `TerrainDirtyRegion` events
  - Version tracking: `SurfaceNavTile.Version`, `UndergroundNavChunk.Version`
  - Not snapshotted; rebuilt on-demand during rewind

- **Celestial orbits**: Deterministic from seed + time
- **Weather/wind fields**: Computed from seed + time + terrain
- **Flow fields**: Rebuilt from terrain + obstacle grids
- **Spatial queries**: Rebuilt from registry + spatial grid version

### When to Use Tier C

- **Deterministic computation**: Output is pure function of seed + time + version
- **Large data structures**: Too expensive to snapshot (grids, spatial caches)
- **Version-tracked sources**: Can detect invalidation via version numbers
- **Derived state**: Computed from Tier A/B sources

### Determinism Requirements

- Must be deterministic: same seed + time + version → same output
- Version tracking: Increment version when source changes (e.g., `TerrainVersion`)
- Rebuild triggers: Systems check version and rebuild if stale
- No external state: Must not depend on frame time, random without seed, or external APIs

### Memory Considerations

- No snapshot overhead (memory-efficient)
- Rebuild cost: Must be acceptable for rewind performance
- Version checks: Lightweight validation before rebuild

---

## Determinism Risks & Mitigations

### Top Risks

1. **Iteration Order**
   - **Risk**: `NativeHashMap`/`NativeParallelHashMap` iteration order is non-deterministic
   - **Mitigation**: Sort entities by `Entity.Index`/`Entity.Version` before processing
   - **Reference**: `RegistryDirectorySystem` uses `NativeSortExtension.Sort()` with `RegistryDirectoryComparer`

2. **Floating Point Drift**
   - **Risk**: Accumulation errors, platform differences, compiler optimizations
   - **Mitigation**: Use `[BurstCompile(FloatMode = FloatMode.Deterministic)]` for critical systems
   - **Reference**: `Docs/BestPractices/DeterminismChecklist.md`

3. **RNG State**
   - **Risk**: Non-seeded RNG, external random sources
   - **Mitigation**: Always use `Unity.Mathematics.Random` with explicit seed
   - **Reference**: `WorldGenRng` for world generation, `ScenarioConfig` seeds for scenarios

4. **Time Sources**
   - **Risk**: Frame time (`Time.deltaTime`) instead of tick time
   - **Mitigation**: Use `SystemAPI.Time.DeltaTime` (fixed timestep) in simulation systems
   - **Reference**: `TimeTickSystem`, `TickTimeState` for canonical time

5. **Nondeterministic Hash Maps**
   - **Risk**: `Dictionary<TKey, TValue>` iteration order varies
   - **Mitigation**: Use `NativeHashMap` with sorted key iteration, or `NativeList` + sort
   - **Reference**: `TerrainDerivedNavCacheSystem` builds lookup maps but processes in deterministic order

6. **External APIs**
   - **Risk**: `UnityEngine.Random`, `Debug.Log`, `Time.deltaTime` in simulation
   - **Mitigation**: Use DOTS-native APIs, telemetry system for logging
   - **Reference**: `Docs/BestPractices/DeterminismChecklist.md`

### Hash Instrumentation

**Tier A Coverage**:
- `ScenarioInputLogEntry.Hash`: FNV-1a hash of agent ID + decision type + decision data
- `ScenarioRunRecorder`: Per-tick digest hashes (scenario, registry, time, rewind, random state)

**Tier B Coverage**:
- `WorldSnapshotMeta.Checksum`: FNV-1a hash of serialized snapshot data
- `ScenarioRunRecorder.DigestRecord`: Aggregate hash of time state, rewind state, registry directory, entity count

**Validation**:
- `ScenarioInputReplay.ValidateReplay()`: Compares recorded vs replayed input logs
- `ScenarioRunRecorder.CompareAgainstBaseline()`: Compares digest hashes against baseline file
- Headless proofs: `HeadlessRewindProofSystem` validates rewind state transitions

---

## Render==Sim Parity

### Requirements

- **Presentation systems**: Must read from simulation state, not mutate it
- **Rewind guards**: Systems check `RewindState.Mode` before mutations
- **Playback tags**: `PlaybackGuardTag` disables gameplay systems during rewind
- **Ghost rendering**: Preview rewind state without affecting simulation

### Implementation

- **`RewindCoordinatorSystem`**: Manages rewind state machine (Play → Rewind → Step → Play)
- **`PlaybackGuardTag`**: Added to entities during rewind playback
- **History playback systems**: `TransformHistoryPlaybackSystem`, `HealthHistoryPlaybackSystem` restore state
- **Presentation bridge**: Reads from simulation components, does not write

### Testing

- **Headless proofs**: `HeadlessRewindProofSystem` validates rewind transitions
- **Scenario validation**: `ScenarioRunRecorder` compares digests across runs
- **Replay tests**: `ScenarioInputReplay` validates input log replay

---

## Memory Optimization

### Ring Buffer Strategy

- **Fixed capacity**: Pre-allocated buffers prevent GC pressure
- **Circular overwrite**: Old entries overwritten when capacity exceeded
- **Pruning**: Old samples removed when exceeding horizon

### Budget Enforcement

- **Per-entity**: `HistorySettings.MaxMemoryPerEntityBytes` (1 MB default)
- **Global**: `HistorySettings.MemoryBudgetBytes` (2 GB default)
- **Snapshots**: `WorldSnapshotState.MemoryBudgetBytes` (256 MB default)
- **Time logs**: `TimeLogSettings.MemoryBudgetBytes` (512 KB default)

### Configuration

- **`HistorySettings`**: Global history configuration singleton
- **`WorldSnapshotState`**: Snapshot system configuration
- **`TimeLogSettings`**: Time log ring buffer configuration
- **Defaults**: `HistorySettingsDefaults`, `TimeLogDefaults`, `WorldSnapshotState.CreateDefault()`

---

## Implementation Guidelines

### Adding Tier A Support

1. Record inputs/events in `ScenarioInputLog` or `ReplayableEvent` buffers
2. Implement replay logic that reads from logs and applies deterministically
3. Add hash validation for quick mismatch detection
4. Ensure RNG is seeded and deterministic

### Adding Tier B Support

1. Add history buffer component (e.g., `PositionHistorySample`) to entity
2. Implement `*HistoryRecordSystem` (runs in `HistorySystemGroup` during `RewindMode.Play`)
3. Implement `*HistoryPlaybackSystem` (runs during `RewindMode.Rewind`)
4. Add `RewindableTag` to entity
5. Configure snapshot frequency via `HistoryTier` component

### Adding Tier C Support

1. Ensure computation is deterministic (seed + time + version)
2. Add version tracking to source data
3. Implement rebuild logic that checks version and recomputes if stale
4. Do NOT snapshot; rely on deterministic rebuild

### System Ordering

- **Record systems**: Run in `HistorySystemGroup` during `RewindMode.Play`
- **Playback systems**: Run in `HistorySystemGroup` during `RewindMode.Rewind`
- **Derived cache systems**: Run in appropriate groups, check `RewindState.Mode` before rebuild

---

## References

- **Components**: `Runtime/Time/RewindComponents.cs`, `Runtime/HistoryComponents.cs`, `Runtime/TimeLoggingComponents.cs`
- **Systems**: `Systems/RewindCoordinatorSystem.cs`, `Systems/History/*HistoryRecordSystem.cs`, `Systems/History/*HistoryPlaybackSystem.cs`
- **Scenarios**: `Runtime/Scenarios/ScenarioInputLog.cs`, `Runtime/Scenarios/ScenarioInputRecorder.cs`, `Runtime/Scenarios/ScenarioRunRecorder.cs`
- **Snapshots**: `Systems/WorldSnapshotSystem.cs`, `Runtime/Time/WorldSnapshotComponents.cs`
- **Determinism**: `Docs/BestPractices/DeterminismChecklist.md`
- **Headless**: `Docs/Headless/headless_runbook.md`
- **Design Notes**: `Documentation/DesignNotes/RewindPatterns.md`

---

## Future Work

- **Delta compression**: Store differences between snapshots to reduce memory
- **LZ4 compression**: Compress snapshot data in `WorldSnapshotData`
- **Multiplayer support**: Player-scoped snapshots and input logs
- **Extended horizons**: Configurable retention beyond default 60s
- **Telemetry integration**: Export rewind metrics to telemetry system
