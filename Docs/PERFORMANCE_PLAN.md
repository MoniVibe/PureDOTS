# PureDOTS Performance & Scale Plan

**Target Scale**: Millions to billions of entities  
**Framework**: PureDOTS (Entities 1.4.x, version-locked)  
**Consumers**: Space4X, Godgame

---

## 1. Component Layout & Data Design

### 1.1 Hot Archetype Classification

**Hot-Path Per-Tick (Updated Every Tick)**
- **Villagers** (`VillagerComponents.cs`): Movement, AI state, needs, sensors
- **Fleets/Carriers** (Space4X): Position, velocity, module states, power budgets
- **Projectiles** (`WeaponComponents.cs`): Position, velocity, lifetime
- **Resource Chunks** (`ResourceComponents.cs`): Position, velocity, age
- **Bands/Villages** (`BandComponents.cs`, `VillageBehaviorComponents.cs`): Aggregate state updates

**Medium Frequency (Updated Occasionally)**
- **Storehouses**: Inventory updates, reservation changes
- **Construction Sites**: Progress updates
- **Spawners**: Cooldown tracking
- **Registries**: Spatial grid sync, entry updates

**Cold (Rarely Updated / Config-Only)**
- **VillagerColdData**: Name, biography, birth tick
- **VillagerKnowledge**: Lesson progress (updated on events)
- **Configuration Components**: ResourceTypeId, StorehouseConfig, etc.

### 1.2 Component Size Audit

| Component | Size (bytes) | Classification | Notes |
|-----------|-------------|----------------|-------|
| VillagerId | 8 | Hot | OK |
| VillagerNeedsHot | 20 | Hot | OK - optimized |
| VillagerNeeds | 40 | Medium | Contains redundant byte/float |
| VillagerMovement | 44 | Hot | OK |
| VillagerAIState | 40 | Hot | OK |
| VillagerJob | 16 | Hot | OK |
| VillagerFlags | 2 | Hot | OK - packed |
| VillagerCombatStats | 16 | Hot | OK |
| VillagerAvailability | 16 | Hot | OK |
| VillagerBelief | 72 | Cold | ❌ FixedString64Bytes - use index |
| VillagerAttributes | 28 | Medium | Consider splitting |
| VillagerKnowledge | 136+ | Cold | ❌ FixedList - move to companion |
| VillagerSensors | 48 | Medium | Entity refs - consider indices |
| ProjectileEntity | 80 | Hot | ❌ FixedString64Bytes - use index |
| WeaponMount | 112 | Hot | ❌ FixedStrings - use indices |
| ResourceChunkState | 36 | Hot | OK |

### 1.3 Component Design Guidelines

**Size Limits**:
- Hot-path components: **< 128 bytes** per entity
- Medium-frequency: **< 256 bytes** per entity
- Cold: No strict limit, but prefer companion entities for > 64 bytes

**Data Type Selection**:
- Use `byte` for 0-255 values (health percent, flags, enums)
- Use `ushort` for 0-65535 values (resource type indices, quality)
- Use `float` only when precision required (position, velocity, amounts)
- Use `int` for IDs, tick counters, counts
- Avoid `FixedString` in hot components - use indices instead

**String Handling**:
- Store string IDs as `ushort` indices into catalogs (blob assets)
- Keep `FixedString` only in cold data or registry entries
- Example: `VillagerBelief.PrimaryDeityId` → `byte DeityIndex` + catalog lookup

**Array/Blob Handling**:
- Move large arrays to companion entities (`DynamicBuffer` on companion)
- Use `BlobAssetReference` for read-only catalogs (no per-entity cost)
- Keep buffers small (< 16 elements) on hot entities

**Hot/Cold Separation Rules**:
1. If updated < 1% of ticks → move to companion entity
2. If > 64 bytes → consider companion entity
3. If contains `FixedString` > 32 bytes → move to companion or use index
4. If contains `Entity` references used rarely → consider spatial grid queries instead

---

## 2. Rendering Scale Path (Data & Contracts)

### 2.1 LOD/Impostor Data Contracts

Components defined in `RenderLODComponents.cs`:
- `RenderLODData` - Distance and importance scores for LOD decisions
- `RenderCullable` - Tag for entities that can be culled at distance

### 2.2 Aggregate Entity Representation

Components defined in `AggregateRenderComponents.cs`:
- `AggregateRenderSummary` - Summary data for aggregate entities
- `AggregateMembership` - Tracks membership in aggregates
- `AggregateState` - Maintains aggregate summaries

### 2.3 Render Density Support

- `RenderSampleIndex` - Stable sampling index for render density control
- `RenderDensitySystem` - Updates `ShouldRender` flag based on density settings

---

## 3. Profiling Gates & Scale Testing

### 3.1 Standard Scale Scenarios

**Scenario 1: Baseline (10k entities)**
- File: `Packages/com.moni.puredots/Runtime/Scenarios/ScaleBaseline.json`
- Target: < 16ms per tick (60 FPS equivalent)

**Scenario 2: Stress (100k entities)**
- File: `Packages/com.moni.puredots/Runtime/Scenarios/ScaleStress.json`
- Target: < 33ms per tick (30 FPS equivalent)

**Scenario 3: Extreme (1M+ entities)**
- File: `Packages/com.moni.puredots/Runtime/Scenarios/ScaleExtreme.json`
- Target: < 100ms per tick (10 FPS equivalent, acceptable for headless)

### 3.2 Performance Budgets

**Component Budgets**:
- Max components on hot entity: **12 components** (including Unity built-ins)
- Max size of frequently-updated component: **128 bytes**
- Max buffer elements on hot entity: **16 elements** (use companion if more)

**System Timing Budgets** (per tick, at 100k entities):
- Movement systems: **< 5ms**
- AI pipeline (sensors, utility, steering): **< 8ms**
- Spatial grid updates: **< 3ms**
- Registry updates: **< 2ms**
- Aggregate updates: **< 2ms**
- **Total simulation time: < 20ms** (leaves headroom for rendering)

**Entity Count Budgets**:
- Hot entities (updated every tick): **< 100k** for 60 FPS
- Medium entities (updated occasionally): **< 500k** total
- Cold entities (rarely updated): **Unlimited** (but prefer companion entities)

**Memory Budgets** (at 1M entities):
- Total memory: **< 4GB** for simulation data
- Chunk memory overhead: **< 500MB**
- Registry buffers: **< 200MB**

---

## 4. Physics & Interaction Strategy

### 4.1 Default Strategy

1. **Start with spatial grid / distance checks** - Fast, deterministic, scalable
2. **Only add physics when**:
   - Player-visible spectacle requires it
   - Gameplay mechanics depend on physics (pushing, momentum, rotation)
   - Visual feedback needs realistic motion

### 4.2 Component Markers

- `UsesSpatialGrid` (default): All entities use spatial grid for queries
- `RequiresPhysics`: Explicit opt-in for physics simulation
- No component: Assumed to use spatial grid (backward compatible)

### 4.3 Performance Impact

- Spatial grid query: **~0.01ms** per query (100k entities)
- Physics body update: **~0.1ms** per body (10x slower)
- **Rule**: Use physics for < 1% of entities (spectacle only)

---

---

## Implementation Status

### Completed

1. **Component Layout & Data Design**
   - Component size audit documented
   - Size guidelines added to `FoundationGuidelines.md`
   - Migration guide created: `Docs/Guides/ComponentMigrationGuide.md`

2. **Rendering Scale Path**
   - `RenderLODComponents.cs` - LOD and culling components
   - `AggregateRenderComponents.cs` - Aggregate rendering support
   - `AggregateRenderSummarySystem.cs` - Updates aggregate summaries
   - `RenderDensitySystem.cs` - Render density control

3. **Profiling Gates & Scale Testing**
   - Scale test scenarios: `scale_baseline_10k.json`, `scale_stress_100k.json`, `scale_extreme_1m.json`
   - `ScaleTestMetricsComponents.cs` - Metrics collection components
   - `ScaleTestMetricsSystem.cs` - Runtime metrics collection
   - Extended `ScenarioRunnerEntryPoints.cs` with `RunScaleTest` entry point
   - Performance budgets documented: `Docs/QA/PerformanceBudgets.md`
   - CI validation script: `CI/validate_metrics.py`

4. **Physics & Interaction Strategy**
   - `PhysicsInteractionComponents.cs` - RequiresPhysics, UsesSpatialGrid, BallisticMotion
   - Guidelines documented: `Docs/Guides/PhysicsVsSpatialGrid.md`

### File Locations

| Category | Files |
|----------|-------|
| Rendering Components | `Runtime/Runtime/Rendering/RenderLODComponents.cs`, `AggregateRenderComponents.cs` |
| Rendering Systems | `Runtime/Systems/Rendering/AggregateRenderSummarySystem.cs`, `RenderDensitySystem.cs` |
| Physics Components | `Runtime/Runtime/Physics/PhysicsInteractionComponents.cs` |
| Telemetry | `Runtime/Runtime/Telemetry/ScaleTestMetricsComponents.cs` |
| Performance Systems | `Runtime/Systems/Performance/ScaleTestMetricsSystem.cs` |
| Scale Scenarios | `Runtime/Runtime/Scenarios/Samples/scale_*.json` |
| Documentation | `Docs/PERFORMANCE_PLAN.md`, `Docs/QA/PerformanceBudgets.md`, `Docs/Guides/PhysicsVsSpatialGrid.md`, `Docs/Guides/ComponentMigrationGuide.md` |

---

## References

- Existing components: `Packages/com.moni.puredots/Runtime/Runtime/`
- Spatial grid: `Packages/com.moni.puredots/Runtime/Systems/Spatial/`
- Scenario runner: `Packages/com.moni.puredots/Runtime/Runtime/Scenarios/`
- Telemetry: `Packages/com.moni.puredots/Runtime/Runtime/Telemetry/`
- Foundation guidelines: `Docs/FoundationGuidelines.md`
- Performance profiles: `Docs/QA/PerformanceProfiles.md`
- Physics vs Spatial Grid: `Docs/Guides/PhysicsVsSpatialGrid.md`
- Component Migration: `Docs/Guides/ComponentMigrationGuide.md`
- Performance Budgets: `Docs/QA/PerformanceBudgets.md`

