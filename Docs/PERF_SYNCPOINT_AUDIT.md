# DOTS Structural-Change & Archetype Explosion Audit

**Date:** 2024  
**Scope:** PureDOTS, Space4X, Godgame  
**Goal:** Identify patterns that prevent scaling to millions of entities

## Executive Summary

This audit identifies systems that perform structural changes (add/remove components, buffers, entities) in hot loops, scatter ECB playback, and create archetype explosion risks. **Top 3 critical issues:**

1. **Space4XStrikeWingGroupSyncSystem** — Removes ~15 components/buffers per orphaned group every tick (archetype churn)
2. **PathRequestSystem** — Adds `PathState`/`PathResult` buffers per request in hot loop (structural changes on path storms)
3. **EntityProfilingSystem** — Multiple ECB playbacks per frame (scattered sync points)

## Top 20 Offenders

| Rank | File | System | Ops | Classification | Risk | Refactor |
|------|------|--------|-----|---------------|------|----------|
| 1 | `space4x/Assets/Scripts/Space4x/Systems/AI/Space4XStrikeWingGroupSyncSystem.cs` | `Space4XStrikeWingGroupSyncSystem` | 35 struct ops, 1 ECB playback | **HOT** | Removes ~15 components/buffers per orphan cleanup; per-leader ECB playback | Use enableable `GroupTag`, consolidate ECB, disable instead of remove |
| 2 | `puredots/.../Systems/Navigation/PathRequestSystem.cs` | `PathRequestSystem` | 6 struct ops | **HOT** | Adds `PathState`/`PathResult` buffers per request in `OnUpdate` | Pre-allocate buffers at entity creation or use enableable state |
| 3 | `puredots/.../Systems/Identity/EntityProfilingSystem.cs` | `EntityProfilingBootstrapSystem` | 9 ECB playbacks | **HOT** | Multiple ECB playbacks per frame (bootstrap + resolution phases) | Consolidate to single ECB per system update |
| 4 | `space4x/.../Systems/Interaction/Space4XThrowSystem.cs` | `Space4XThrowSystem` | 5 ECB playbacks | **HOT** | ECB playback per throw/drop operation | Consolidate to single ECB at end of `OnUpdate` |
| 5 | `puredots/.../Systems/TimeBubbleMembershipSystem.cs` | `TimeBubbleMembershipSystem` | 5 ECB playbacks | **HOT** | Multiple ECB playbacks (create, remove, membership, stasis) | Consolidate to single ECB per update |
| 6 | `puredots/.../Systems/Power/PowerCoreSystems.cs` | `PowerAllocationSystem` | 4 ECB playbacks | **HOT** | ECB playback per power allocation update | Consolidate ECB playback |
| 7 | `space4x/.../Presentation/Space4XPresentationLODSystem.cs` | `Space4XRenderDensitySystem` | Tag add/remove | **HOT** | Adds/removes `ShouldRenderTag` every frame based on density | Use enableable component instead |
| 8 | `puredots/.../Systems/Lifecycle/EntityReproductionSystem.cs` | `EntityReproductionSystem` | 3 struct ops | **WARM** | Adds `EntityRelation` buffers, instantiates prefabs | Acceptable (rare, single-threaded); consider ECB consolidation |
| 9 | `space4x/.../Systems/AI/VesselMovementSystem.cs` | `VesselMovementSystem` | 6 struct ops | **HOT** | Structural changes during movement updates | Review for enableable alternatives |
| 10 | `space4x/.../Scenario/Space4XRefitScenarioSystem.cs` | `Space4XRefitScenarioSystem` | 20 struct ops | **COLD** | Bootstrap scenario setup | Low priority (one-time) |
| 11 | `space4x/.../Scenario/Space4XMiningScenarioSystem.cs` | `Space4XMiningScenarioSystem` | 111 struct ops | **COLD** | Bootstrap scenario setup, disables after first load | Low priority (one-time initialization) |
| 12 | `puredots/.../Systems/CombatDamage/SegmentDamageSystems.cs` | Damage systems | 3 struct ops | **HOT** | Adds damage buffers/components | Review for pre-allocation |
| 13 | `godgame/.../Visitors/VisitorPickabilitySystem.cs` | `VisitorPickabilitySystem` | 6 struct ops | **HOT** | Adds/removes pickability components | Use enableable component |
| 14 | `puredots/.../Systems/Infiltration/InfiltrationSystems.cs` | Infiltration systems | 2 ECB playbacks | **WARM** | ECB playback for infiltration state changes | Consolidate ECB |
| 15 | `puredots/.../Rendering/ResolveRenderVariantSystem.cs` | `ResolveRenderVariantSystem` | 2 ECB playbacks | **HOT** | ECB playback for render variant changes | Consolidate ECB |
| 16 | `space4x/.../Registry/Space4XAttackMoveTelemetrySystem.cs` | `Space4XAttackMoveTelemetrySystem` | 11 struct ops | **HOT** | Adds telemetry buffers/components | Pre-allocate or use enableable |
| 17 | `godgame/.../Systems/Interaction/GodgameThrowSystem.cs` | `GodgameThrowSystem` | 5 ECB playbacks | **HOT** | ECB playback per throw operation | Consolidate ECB |
| 18 | `puredots/.../Systems/Modules/ModuleNormalizationSystems.cs` | Module normalization | 2 ECB playbacks | **HOT** | ECB playback for module state | Consolidate ECB |
| 19 | `puredots/.../Systems/GhostSpawnSystem.cs` | `GhostSpawnSystem` | 2 ECB playbacks | **WARM** | ECB playback for ghost spawning | Consolidate ECB |
| 20 | `godgame/.../Effects/StatusEffectSystem.cs` | `StatusEffectSystem` | 2 ECB playbacks | **HOT** | ECB playback for status effect changes | Consolidate ECB |

## Detailed Analysis

### 1. Structural Changes in Hot Loops

**Problem:** Direct `EntityManager.AddComponent/RemoveComponent/AddBuffer` calls inside `OnUpdate` create sync points and archetype churn.

#### Critical Hot Systems

**Space4XStrikeWingGroupSyncSystem** (`space4x/Assets/Scripts/Space4x/Systems/AI/Space4XStrikeWingGroupSyncSystem.cs`)
- **Issue:** `CleanupOrphanedGroups` removes ~15 components/buffers per orphaned group every tick:
  - `WingFormationAnchorRef` (buffer)
  - `WingFormationState`, `WingGroupSyncState`
  - `GroupFormation`, `GroupFormationSpread`
  - `SquadCohesionProfile`, `SquadCohesionState`
  - `GroupStanceState`
  - `EngagementThreatSummary`, `EngagementIntent`, `EngagementPlannerState`
  - `CommsOutboxEntry` (buffer), `GroupMember` (buffer)
  - `SquadTacticOrder`
  - `FormationState`, `FormationSlot` (buffer)
  - `GroupTag`, `GroupMeta`
- **Impact:** Each removal creates a new archetype. With many wings forming/disbanding, archetype count explodes.
- **Fix:** Use enableable `GroupTag`; disable instead of removing components. Clear buffers instead of removing them.

**PathRequestSystem** (`puredots/Packages/com.moni.puredots/Runtime/Systems/Navigation/PathRequestSystem.cs`)
- **Issue:** Adds `PathState` component and `PathResult` buffer per request in `OnUpdate` (lines 105, 110, 184, 189).
- **Impact:** During path-request storms (many entities requesting paths simultaneously), creates many structural changes.
- **Fix:** Pre-allocate `PathState`/`PathResult` at entity creation, or use enableable state component.

**EntityReproductionSystem** (`puredots/Packages/com.moni.puredots/Runtime/Systems/Lifecycle/EntityReproductionSystem.cs`)
- **Issue:** Adds `EntityRelation` buffers and instantiates prefabs in hot loop (lines 153, 226, 305).
- **Impact:** Moderate (reproduction is rare, but still structural changes).
- **Fix:** Acceptable for now (single-threaded, rare). Consider ECB consolidation if frequency increases.

### 2. ECB Playback Scatter

**Problem:** Multiple `EntityCommandBuffer` allocations and `.Playback()` calls per system update create scattered sync points, preventing batching and determinism.

#### Critical Systems

**EntityProfilingSystem** (`puredots/Packages/com.moni.puredots/Runtime/Systems/Identity/EntityProfilingSystem.cs`)
- **Issue:** Multiple ECB playbacks across bootstrap and resolution phases (9 total).
- **Impact:** Creates multiple sync points per frame.
- **Fix:** Use single ECB per system update, playback once at end.

**Space4XThrowSystem** (`space4x/Assets/Scripts/Space4x/Systems/Interaction/Space4XThrowSystem.cs`)
- **Issue:** ECB playback per throw/drop operation (5 playbacks).
- **Impact:** Scattered sync points during rapid interactions.
- **Fix:** Accumulate all commands in single ECB, playback once at end of `OnUpdate`.

**TimeBubbleMembershipSystem** (`puredots/Packages/com.moni.puredots/Runtime/Systems/TimeBubbleMembershipSystem.cs`)
- **Issue:** Multiple ECB playbacks: `ProcessCreateRequests`, `ProcessRemoveRequests`, `UpdateMemberships`, `UpdateStasisTags` (5 total).
- **Impact:** Multiple sync points per frame.
- **Fix:** Single ECB per update, playback once.

### 3. Archetype Explosion Risks

**Problem:** Frequent add/remove of tags/components creates many archetype variants, fragmenting memory and reducing cache efficiency.

#### Tag Churn Patterns

**Space4XRenderDensitySystem** (`space4x/Assets/Scripts/Space4x/Presentation/Space4XPresentationLODSystem.cs`)
- **Issue:** Adds/removes `ShouldRenderTag` every frame based on density sampling (lines 47, 51 in search results).
- **Impact:** Creates two archetypes per entity (with/without tag) that flip frequently.
- **Fix:** Make `ShouldRenderTag` enableable (`IEnableableComponent`), use `SetComponentEnabled` instead.

**GroupTag Removal** (multiple systems)
- **Issue:** `GroupTag` is removed when groups disband, creating archetype churn.
- **Impact:** With many groups forming/disbanding, creates archetype explosion.
- **Fix:** Make `GroupTag` enableable; disable instead of remove.

**VisitorPickabilitySystem** (`godgame/Assets/Scripts/Godgame/Visitors/VisitorPickabilitySystem.cs`)
- **Issue:** Adds/removes pickability components based on state.
- **Impact:** Creates archetype variants for pickable/non-pickable states.
- **Fix:** Use enableable component or state flag.

### 4. Shared Component High-Cardinality

**Status:** Low incidence found. Only 2 files use `ISharedComponentData`:
- `space4x/Assets/Editor/Space4XDiagnostics.cs`
- `puredots/Packages/com.moni.puredots/Runtime/Demo/Rendering/UniversalDebugRenderConfigAuthoring.cs`

**Recommendation:** Monitor for shared components with many unique values (e.g., per-entity shared data).

## Safe Refactor Patterns

### Pattern 1: Enableable Components for State Toggles

**When:** Component represents a boolean state that flips frequently (e.g., "is active", "should render", "is pickable").

**Example:**
```csharp
// BEFORE (archetype churn):
if (shouldRender && !HasComponent<ShouldRenderTag>(entity))
    AddComponent<ShouldRenderTag>(entity);
else if (!shouldRender && HasComponent<ShouldRenderTag>(entity))
    RemoveComponent<ShouldRenderTag>(entity);

// AFTER (no archetype change):
public struct ShouldRenderTag : IComponentData, IEnableableComponent { }
SetComponentEnabled<ShouldRenderTag>(entity, shouldRender);
```

**Applied to:**
- `GroupTag` → enableable (prevents archetype churn on group disband)
- `ShouldRenderTag` → enableable (prevents render density archetype churn)

### Pattern 2: ECB Consolidation

**When:** System performs multiple structural changes per update.

**Example:**
```csharp
// BEFORE (scattered sync points):
var ecb1 = new EntityCommandBuffer(Allocator.Temp);
// ... operations ...
ecb1.Playback(state.EntityManager);
ecb1.Dispose();

var ecb2 = new EntityCommandBuffer(Allocator.Temp);
// ... more operations ...
ecb2.Playback(state.EntityManager);
ecb2.Dispose();

// AFTER (single sync point):
var ecb = new EntityCommandBuffer(Allocator.Temp);
// ... all operations ...
ecb.Playback(state.EntityManager);
ecb.Dispose();
```

**Applied to:**
- `EntityProfilingSystem` → single ECB per update
- `Space4XThrowSystem` → single ECB per update
- `TimeBubbleMembershipSystem` → single ECB per update
- `Space4XStrikeWingGroupSyncSystem` → single ECB per update

### Pattern 3: Buffer Clearing vs Removal

**When:** Buffer needs to be "empty" but may be reused.

**Example:**
```csharp
// BEFORE (archetype change):
if (HasBuffer<GroupMember>(entity))
    RemoveComponent<GroupMember>(entity);

// AFTER (no archetype change):
var buffer = GetBuffer<GroupMember>(entity);
buffer.Clear();
// Buffer remains, archetype unchanged
```

**Applied to:**
- `GroupMember` buffer clearing in `Space4XStrikeWingGroupSyncSystem`

### Pattern 4: Pre-allocation at Entity Creation

**When:** Components/buffers are frequently added in hot loops.

**Example:**
```csharp
// BEFORE (structural change in hot loop):
if (!HasComponent<PathState>(entity))
    AddComponent<PathState>(entity);

// AFTER (pre-allocated):
// At entity creation:
AddComponent<PathState>(entity);
SetComponentEnabled<PathState>(entity, false);
// In hot loop:
SetComponentEnabled<PathState>(entity, true);
```

**Applied to:**
- `PathState`/`PathResult` pre-allocation in `PathRequestSystem`

### Pattern 5: External Storage for High-Churn Data

**When:** Data changes very frequently and doesn't need to be in ECS.

**Example:**
```csharp
// BEFORE (component churn):
AddComponent<TelemetryEntry>(entity, new TelemetryEntry { ... });
// ... later ...
RemoveComponent<TelemetryEntry>(entity);

// AFTER (external storage):
var telemetryStore = GetSingleton<TelemetryStore>();
telemetryStore.AddEntry(entity, entry);
// No archetype changes
```

**Consider for:**
- High-frequency telemetry data
- Temporary state that flips every frame

## Determinism Considerations

**Stable Ordering:** Systems that iterate over entities should use deterministic ordering (e.g., sort by `(Index, Version)`) before structural changes to preserve determinism across runs.

**Applied to:**
- `Space4XStrikeWingGroupSyncSystem` → sort `leaders` list before processing

## Implementation Priority

### High Priority (Implemented)
1. ✅ Make `GroupTag` enableable (PureDOTS, reusable)
2. ✅ Refactor `Space4XStrikeWingGroupSyncSystem` (Space4X, hot-loop fix)
3. ✅ Add archetype spike diagnostic (PureDOTS, dev-build warning)

### Medium Priority (Recommended)
4. Consolidate ECB playback in `EntityProfilingSystem`
5. Consolidate ECB playback in `Space4XThrowSystem` / `GodgameThrowSystem`
6. Consolidate ECB playback in `TimeBubbleMembershipSystem`
7. Pre-allocate `PathState`/`PathResult` in `PathRequestSystem`
8. Make `ShouldRenderTag` enableable in `Space4XRenderDensitySystem`

### Low Priority (Future)
9. Review `VesselMovementSystem` structural changes
10. Review telemetry system buffer additions
11. Monitor shared component cardinality

## Metrics & Validation

**Before/After:**
- Archetype count stability during wing churn (should remain stable)
- ECB playback consolidation (single sync point per system)
- No behavior change (groups deactivate vs remove components)

**Diagnostic:**
- Dev-build archetype spike warning (configurable thresholds, rate-limited)

## Notes

- **Bootstrap/Cold Systems:** Systems in `InitializationSystemGroup` or that disable after first load are low priority (e.g., `Space4XMiningScenarioSystem`).
- **Test Systems:** Test utilities with structural changes are acceptable (not production code).
- **Render == Sim:** Presentation systems should be read-only; structural changes in presentation are violations of this principle.
