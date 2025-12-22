# AI Behavior Contracts Advisory

**Status:** Recommendations / Action Plan
**Category:** Core - AI Foundation Hardening
**Audience:** Implementers / Architects
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Purpose

**Primary Goal:** Tighten foundational contracts so every new AI behavior is "just add an intent + a component" (not a rewrite). Before stacking more AI behaviors on top, ensure the foundation is solid, predictable, and performant.

**Focus Areas:**
1. **Entity lifecycle** — Structural changes vs state toggles
2. **Transform contract** — Hierarchies, 6DOF, attachments
3. **Physics contract** — Real vs kinematic vs snapshotted
4. **Perception contract** — Incremental, budgeted, no world-scans
5. **Conditions/buffs/derived stats** — Dirty-driven updates
6. **Task allocation blackboards** — Cooperation without comm spam
7. **Navigation/pathfinding** — Pipeline boundaries and versioning
8. **Debug + tests** — Contracts for incremental development

**Key Principle:** Establish clear contracts and patterns now, so adding new behaviors is configuration and components, not system rewrites.

---

## 1. Entity Lifecycle and Structural-Change Policy

### Current State

**Problem:** Unclear when to add/remove components vs toggle state, leading to archetype churn and performance issues.

### Proposed Solution

**Decision Rule:** One-page policy for component lifecycle management.

#### Rule 1: Structural Changes (EntityCommandBuffer)

**Use EntityCommandBuffer for structural changes and batch them:**

```csharp
// ✅ CORRECT: Batch structural changes via ECB
var ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);

foreach (var (entity, _) in SystemAPI.Query<RefRO<SomeCondition>>().WithEntityAccess())
{
    ecb.AddComponent<NewComponent>(entity);
    ecb.RemoveComponent<OldComponent>(entity);
    ecb.DestroyEntity(destroyedEntity);
}

ecb.Playback(entityManager);
ecb.Dispose();
```

**When to use ECB:**
- Adding/removing components (archetype changes)
- Creating/destroying entities
- Structural changes that affect chunk layout

**Benefits:**
- Fewer sync points (batch all changes)
- Deterministic ordering (single playback)
- Works in jobs (deferred execution)

**Reference:** [Unity ECS - EntityCommandBuffer](https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/ecb.html)

#### Rule 2: State Toggles (Enableable Components)

**Use enableable components for high-frequency on/off states:**

```csharp
// ✅ CORRECT: Enableable for frequent state toggles
public struct IsFleeing : IComponentData, IEnableableComponent { }
public struct IsClimbing : IComponentData, IEnableableComponent { }
public struct IsStunned : IComponentData, IEnableableComponent { }

// Toggle without archetype change
entityManager.SetComponentEnabled<IsFleeing>(entity, true);   // Enable
entityManager.SetComponentEnabled<IsFleeing>(entity, false);  // Disable

// Query with enabled filter
foreach (var (entity, _) in SystemAPI.Query<RefRO<SomeComponent>>()
    .WithAll<IsFleeing>()
    .WithOptions(EntityQueryOptions.IncludeDisabledEntities))
{
    // Handle fleeing entities
}
```

**When to use enableable:**
- High-frequency state toggles (stun, fleeing, climbing, invisible)
- States that change multiple times per second
- Flags that don't affect archetype (same components present, just enabled/disabled)

**Caveats:**
- **Don't go wild:** Unity notes worst cases can hurt Burst/vectorization and chunk usage
- Use for states that genuinely toggle frequently
- Avoid for states that rarely change (structural change is fine)

**Reference:** [Unity ECS - Enableable Components](https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/components-enableable.html)

**Anti-Pattern:**
```csharp
// ❌ WRONG: Adding/removing component for frequent toggle
if (shouldFlee)
    entityManager.AddComponent<IsFleeing>(entity);  // Archetype change!
else
    entityManager.RemoveComponent<IsFleeing>(entity); // Archetype change!
```

#### Rule 3: Avoid High-Cardinality Shared Components

**Problem:** Shared components split chunks; poor utilization.

```csharp
// ❌ AVOID: High-cardinality shared component
public struct PerEntityConfig : ISharedComponentData
{
    public int UniqueId;  // Every entity has unique value = splits chunks
}

// ✅ PREFER: Regular component or blob asset reference
public struct EntityConfig : IComponentData
{
    public BlobAssetReference<EntityConfigBlob> Config;  // Shared blob asset
}
```

**When shared components are OK:**
- Low cardinality (few unique values)
- Shared by many entities (chunk utilization benefit)
- Examples: `RenderMesh`, `MaterialMeshInfo`

**Reference:** [Unity ECS - Shared Components](https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/components-shared.html)

#### Rule 4: Chunk Components for Per-Chunk Metadata

**Use chunk components for per-chunk metadata without structural moves:**

```csharp
// ✅ CORRECT: Chunk component for metadata
public struct ChunkBounds : IComponentData, IChunkComponentData
{
    public AABB Bounds;
}

public struct ChunkDirtyFlag : IComponentData, IChunkComponentData
{
    public byte IsDirty;
}

// Set chunk component (affects all entities in chunk)
var chunk = entityManager.GetChunk(entity);
entityManager.SetChunkComponentData<ChunkBounds>(chunk, new ChunkBounds { Bounds = bounds });
```

**When to use chunk components:**
- Per-chunk metadata (bounds, local hazard flags, dirty markers)
- Data shared by all entities in chunk
- Avoids per-entity storage overhead

**Reference:** [Unity ECS - Chunk Components](https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/components-chunk.html)

### Done When

**Success Criteria:** You can point to a one-page rule: "State toggles are enableable; archetype changes only via ECB at known barriers."

**Implementation Checklist:**
- [ ] Document policy: when to use ECB vs enableable vs structural
- [ ] Audit existing code: replace frequent AddComponent/RemoveComponent with enableable
- [ ] Identify high-cardinality shared components, migrate to blob assets
- [ ] Use chunk components for per-chunk metadata (bounds, dirty flags)
- [ ] Add linter/validation to catch anti-patterns (frequent AddComponent in hot path)

---

## 2. Transform Contract (Hierarchies, 6DOF, Attachments)

### Current State

**Problem:** Inconsistent transform usage, deep hierarchies, non-uniform scale hacks.

### Proposed Solution

**Standardize transform usage and hierarchy rules.**

#### Rule 1: Standardize on LocalTransform + Parent/Child

**Use LocalTransform + Parent/Child and let LocalToWorldSystem compute LocalToWorld:**

```csharp
// ✅ CORRECT: Standard transform hierarchy
public struct Parent : IComponentData
{
    public Entity Value;
}

public struct LocalTransform : IComponentData
{
    public float3 Position;
    public quaternion Rotation;
    public float Scale;
}

// LocalToWorldSystem computes LocalToWorld automatically
// (unless you intentionally override it for custom transforms)
```

**Benefits:**
- Automatic LocalToWorld computation
- Efficient hierarchy updates (only update changed transforms)
- Consistent transform usage across codebase

**Override Pattern (if needed):**
```csharp
// Only override if you need custom transform logic (e.g., physics-driven)
public struct CustomTransform : IComponentData { }

// Custom system updates LocalToWorld for CustomTransform entities
[BurstCompile]
public partial struct CustomTransformSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, localToWorld, entity) in 
            SystemAPI.Query<RefRO<LocalTransform>, RefRW<LocalToWorld>>()
                .WithAll<CustomTransform>()
                .WithEntityAccess())
        {
            // Custom LocalToWorld computation
            localToWorld.ValueRW.Value = ComputeCustomMatrix(transform.ValueRO);
        }
    }
}
```

**Reference:** [Unity ECS - Transform Components](https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/transform-usage.html)

#### Rule 2: Avoid Deep Hierarchies

**Problem:** Deep hierarchies under one root hurt parallelization.

```csharp
// ❌ AVOID: Deep hierarchy under one root
Root (Ship)
  └─ Module1
      └─ Submodule1
          └─ Component1
              └─ Attachment1  // 5 levels deep

// ✅ PREFER: Many root-level hierarchies
Ship (Root)
  └─ Module1 (direct child)
  └─ Module2 (direct child)

CrewMember1 (Root)
  └─ Weapon (direct child)

CrewMember2 (Root)
  └─ Weapon (direct child)
```

**Benefits:**
- DOTS transform work parallelizes best with many root-level hierarchies
- Shallow hierarchies (2-3 levels) are efficient
- Deep hierarchies (5+ levels) cause sequential updates

**When deep hierarchies are OK:**
- Presentation-only (not used in simulation)
- Static/prebaked structures
- Low update frequency

**Reference:** [Unity ECS - Transform Hierarchy](https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/transform-usage.html)

#### Rule 3: Non-Uniform Scale Needs PostTransformMatrix

**Don't hack around non-uniform scale:**

```csharp
// ❌ WRONG: Hacking non-uniform scale
public struct LocalTransform : IComponentData
{
    public float3 Position;
    public quaternion Rotation;
    public float3 Scale;  // ❌ LocalTransform only supports uniform scale!
}

// ✅ CORRECT: Use PostTransformMatrix for non-uniform scale
public struct LocalTransform : IComponentData
{
    public float3 Position;
    public quaternion Rotation;
    public float Scale;  // Uniform scale only
}

public struct PostTransformMatrix : IComponentData
{
    public float4x4 Value;  // Apply after LocalTransform, supports non-uniform scale
}
```

**When to use PostTransformMatrix:**
- Non-uniform scale (different scale on X/Y/Z)
- Shear transformations
- Skew transformations

**Performance Note:** PostTransformMatrix adds overhead (extra matrix multiply). Prefer uniform scale when possible.

**Reference:** [Unity ECS - Transform Scale](https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/transform-usage.html)

### Done When

**Success Criteria:** Ship/module attachments, crews, props all work with consistent parent-child rules and don't tank perf.

**Implementation Checklist:**
- [ ] Standardize on LocalTransform + Parent/Child
- [ ] Audit hierarchies: flatten deep hierarchies (5+ levels → 2-3 levels)
- [ ] Replace non-uniform scale hacks with PostTransformMatrix
- [ ] Document transform attachment patterns (ship modules, crew members, props)
- [ ] Performance test: verify hierarchies don't cause serialization

---

## 3. Physics Contract (What Is "Real", What Is Kinematic, What Is Snapshotted)

### Current State

**Problem:** Unclear split between Unity Physics (Tier 0) vs kinematic (Tier 1+), snapshot behavior undefined.

### Proposed Solution

**Clear split between physics tiers and snapshot policies.**

#### Rule 1: Physics Tiers

**Tier 0 (Near Camera / Boarding):** Unity Physics / impulses OK.

```csharp
// Tier 0: Full Unity Physics
public struct PhysicsBody : IComponentData
{
    public Entity PhysicsEntity;  // References Unity Physics entity
}

// Use Unity Physics for:
// - Player character
// - Entities near camera (boarding combat)
// - Interactive objects (doors, levers)
```

**Tier 1+ (Mass Sim):** Kinematic + analytic approximations.

```csharp
// Tier 1+: Kinematic movement
public struct KinematicBody : IComponentData
{
    public float3 Velocity;
    public float3 Acceleration;
    public float Mass;
}

// Use kinematic for:
// - Distant entities (hundreds of units away)
// - Mass simulation (thousands of entities)
// - Deterministic movement (pathfinding, steering)
```

**Decision Rule:**
- Tier 0: `DistanceToCamera < 50 units` AND `IsPlayer OR IsInteracting`
- Tier 1+: Everything else

#### Rule 2: Multiple Fixed Steps Per Frame

**Physics must handle multiple fixed steps per frame:**

**Problem:** Fast-forward, catch-up, and rollback can run 20+ ticks in a single frame.

```csharp
// ✅ CORRECT: Allocation-free, deterministic physics
[BurstCompile]
public partial struct KinematicMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;
        var fixedDt = SystemAPI.Time.FixedDeltaTime;
        
        // Calculate how many fixed steps this frame
        var steps = (int)(dt / fixedDt);
        
        foreach (var (velocity, transform) in 
            SystemAPI.Query<RefRO<KinematicBody>, RefRW<LocalTransform>>())
        {
            // Run multiple steps (allocation-free)
            var pos = transform.ValueRO.Position;
            var vel = velocity.ValueRO.Velocity;
            
            for (int i = 0; i < steps; i++)
            {
                pos += vel * fixedDt;
                // Apply constraints, collisions, etc. (deterministic)
            }
            
            transform.ValueRW.Position = pos;
        }
    }
}
```

**Requirements:**
- **Allocation-free:** No `new`, `List.Add()`, or dynamic allocations in physics code
- **Deterministic:** Same inputs → same outputs (no random, no time-dependent state)
- **Safe under "run 20 ticks this frame":** Must handle multiple steps efficiently

**Reference:**
- [Unity - Fixed Update](https://docs.unity.cn/Manual/ExecutionOrder.html)
- [Unity Netcode - Tick and Update](https://docs.unity.cn/Packages/com.unity.netcode.gameobjects@1.0/manual/tick-rate.html)

#### Rule 3: Snapshot Policy

**Decide what gets snapshotted vs recomputed:**

```csharp
// Snapshot policy per component/system:
public struct RewindImportance : IComponentData
{
    public RewindTier Tier;  // None, Derived, SnapshotLite, SnapshotFull
}

// Tier 0 (Unity Physics): SnapshotFull (capture full state)
// Tier 1+ (Kinematic): Derived (recompute from LocalTransform + Velocity)
```

**Snapshot Categories:**
- **SnapshotFull:** Capture complete state (Tier 0 physics, complex AI state)
- **SnapshotLite:** Capture minimal state (position, velocity, basic flags)
- **Derived:** Recompute from deterministic inputs (LocalTransform + Velocity → recompute)
- **None:** Don't snapshot (presentation-only, derived data)

### Done When

**Success Criteria:** You can fast-forward/rewind without physics exploding or drifting, and you know exactly what gets snapshotted vs recomputed.

**Implementation Checklist:**
- [ ] Document physics tiers (Tier 0 = Unity Physics, Tier 1+ = kinematic)
- [ ] Audit physics code: ensure allocation-free, deterministic
- [ ] Test fast-forward (20+ ticks per frame): verify no explosions/drift
- [ ] Test rewind: verify physics state restores correctly
- [ ] Document snapshot policy per component/system

---

## 4. Perception Contract (Incremental, Budgeted, No World-Scans)

### Current State

**Problem:** Perception systems may scan all entities (O(N) cost), no change filtering.

### Proposed Solution

**Incremental, budgeted perception with change filtering.**

#### Rule 1: Change Filtering

**Use change filtering to only process chunks that actually changed:**

```csharp
// ✅ CORRECT: Change filtering
[BurstCompile]
public partial struct PerceptionUpdateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Only process entities whose SenseCapability changed
        foreach (var (capability, perceptionState, entity) in
            SystemAPI.Query<RefRO<SenseCapability>, RefRW<PerceptionState>>()
                .WithChangeFilter<SenseCapability>()
                .WithEntityAccess())
        {
            // Update perception only for changed entities
        }
        
        // Only process entities whose transform changed (moved)
        foreach (var (transform, perceptionState, entity) in
            SystemAPI.Query<RefRO<LocalTransform>, RefRW<PerceptionState>>()
                .WithChangeFilter<LocalTransform>()
                .WithEntityAccess())
        {
            // Re-evaluate perception for moved entities
        }
    }
}
```

**Change Filter Patterns:**
- `WithChangeFilter<ComponentType>()` — Only entities where ComponentType changed
- `WithChangedVersionFilter<ComponentType>()` — Filter by change version (for advanced use)

**Benefits:**
- Skip unchanged entities (huge win for 1M entities)
- Process only entities that moved/changed
- Reduces perception cost from O(N) to O(changed)

**Reference:** [Unity ECS - Change Filtering](https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/change-filtering.html)

#### Rule 2: Budgeted Updates

**Budget perception updates per frame:**

```csharp
// ✅ CORRECT: Budgeted perception
public struct PerceptionBudget : IComponentData
{
    public int MaxEntitiesPerFrame;  // e.g., 1000
    public int ProcessedThisFrame;
}

[BurstCompile]
public partial struct PerceptionUpdateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var budget = SystemAPI.GetSingletonRW<PerceptionBudget>();
        budget.ValueRW.ProcessedThisFrame = 0;
        
        foreach (var (capability, perceptionState, entity) in
            SystemAPI.Query<RefRO<SenseCapability>, RefRW<PerceptionState>>()
                .WithChangeFilter<SenseCapability>()
                .WithEntityAccess())
        {
            if (budget.ValueRW.ProcessedThisFrame >= budget.ValueRO.MaxEntitiesPerFrame)
            {
                break;  // Budget exhausted, process rest next frame
            }
            
            // Update perception
            budget.ValueRW.ProcessedThisFrame++;
        }
    }
}
```

**Budget Strategies:**
- **Fixed budget:** Process N entities per frame
- **Time-sliced:** Process until time budget exhausted
- **Priority-based:** Process high-priority entities first

#### Rule 3: Single "MindInput" Buffer

**Keep a single "MindInput" buffer for AI:**

```csharp
// ✅ CORRECT: Unified mind input buffer
[InternalBufferCapacity(32)]
public struct MindInput : IBufferElementData
{
    public MindInputType Type;
    public Entity SourceEntity;
    public float3 SourcePosition;
    public float Value;              // Normalized value (0-1)
    public float Confidence;         // 0-1, how certain
    public uint Timestamp;
}

public enum MindInputType : byte
{
    PerceivedContact = 0,     // Detected entity
    InternalNeed = 1,         // Hunger, energy, morale
    CommReceived = 2,         // Last comm receipt
    EnvironmentSample = 3     // Mana, temperature, etc.
}

// Perception systems populate MindInput buffer
// Utility scoring reads MindInput buffer
// Single source of truth for AI input
```

**Benefits:**
- Single buffer for all AI inputs (needs, perceptions, comms, environment)
- Consistent format for utility scoring
- Easy to debug (one buffer to inspect)

### Done When

**Success Criteria:** 1M entities can "sense" without O(N) scans each tick.

**Implementation Checklist:**
- [ ] Add change filtering to perception systems (`WithChangeFilter`)
- [ ] Implement budgeted updates (max entities per frame)
- [ ] Create unified `MindInput` buffer (needs + perceptions + comms + environment)
- [ ] Audit perception queries: ensure no O(N) scans
- [ ] Performance test: verify 1M entities can sense without cost explosion

---

## 5. Conditions / Buffs / Derived Stats (Dirty-Driven)

### Current State

**Problem:** Status effects, buffs, and derived stats may recompute every tick (expensive).

### Proposed Solution

**Dirty-driven updates with change filtering.**

#### Rule 1: One Canonical Effect Representation

**One canonical way to represent effects:**

```csharp
// ✅ CORRECT: Canonical effect buffer
[InternalBufferCapacity(16)]
public struct StatusEffect : IBufferElementData
{
    public FixedString64Bytes EffectId;    // "Poison", "Buffed", "Stunned"
    public float Magnitude;                // Effect strength (e.g., damage per second)
    public float Duration;                 // Remaining duration
    public uint AppliedTick;               // When effect was applied
    public Entity SourceEntity;            // What applied this effect
}

// Single source of truth for all status effects
// No separate "Buffs" vs "Debuffs" buffers (use Magnitude sign: positive = buff, negative = debuff)
```

#### Rule 2: Aggregated Modifier Cache

**One aggregated modifier cache (recompute only when effects/limbs/stats change):**

```csharp
// ✅ CORRECT: Aggregated modifier cache
public struct StatModifierCache : IComponentData
{
    public float DamageMultiplier;      // Aggregated from all effects
    public float SpeedMultiplier;
    public float HealthRegenPerSecond;
    public uint LastUpdateTick;         // When cache was last updated
    public uint EffectsVersion;         // Version of effects buffer (for dirty detection)
}

[BurstCompile]
public partial struct StatModifierAggregationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Only update cache when effects changed
        foreach (var (effects, cache, entity) in
            SystemAPI.Query<DynamicBuffer<StatusEffect>, RefRW<StatModifierCache>>()
                .WithChangeFilter<StatusEffect>()
                .WithEntityAccess())
        {
            // Aggregate all effects into cache
            cache.ValueRW.DamageMultiplier = 1f;
            cache.ValueRW.SpeedMultiplier = 1f;
            
            for (int i = 0; i < effects.Length; i++)
            {
                var effect = effects[i];
                ApplyEffectToCache(effect, ref cache.ValueRW);
            }
            
            cache.ValueRW.EffectsVersion = GetBufferVersion(effects);
        }
    }
}
```

**Benefits:**
- Recompute only when effects change (not every tick)
- Single aggregated cache (fast reads)
- Change filtering prevents unnecessary work

#### Rule 3: Derived Stats via Dirty Flags

**Derived stats update only via dirty flags + change filters:**

```csharp
// ✅ CORRECT: Dirty-driven derived stats
public struct DerivedStats : IComponentData
{
    public float MaxHealth;              // Derived from base + modifiers
    public float CurrentHealth;
    public float MaxStamina;
    public float CurrentStamina;
    public uint LastUpdateTick;
    public byte IsDirty;                 // Dirty flag
}

[BurstCompile]
public partial struct DerivedStatsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Only update when base stats or modifiers changed
        foreach (var (baseStats, modifiers, derived, entity) in
            SystemAPI.Query<RefRO<BaseStats>, RefRO<StatModifierCache>, RefRW<DerivedStats>>()
                .WithChangeFilter<BaseStats, StatModifierCache>()
                .WithEntityAccess())
        {
            // Recompute derived stats
            derived.ValueRW.MaxHealth = baseStats.ValueRO.BaseHealth * modifiers.ValueRO.HealthMultiplier;
            derived.ValueRW.MaxStamina = baseStats.ValueRO.BaseStamina * modifiers.ValueRO.StaminaMultiplier;
            derived.ValueRW.IsDirty = 0;
        }
    }
}
```

**Pattern:**
1. Base stats change → mark derived stats dirty (or use change filter)
2. Derived stats system recomputes only dirty entities
3. Change filter ensures only changed entities are processed

#### Rule 4: Enableable for Frequent State Flags

**Use enableable components for frequent state flags (with caution):**

```csharp
// ✅ CORRECT: Enableable for frequent toggles
public struct IsStunned : IComponentData, IEnableableComponent { }
public struct IsBurning : IComponentData, IEnableableComponent { }
public struct IsInvulnerable : IComponentData, IEnableableComponent { }

// Toggle without structural change
entityManager.SetComponentEnabled<IsStunned>(entity, true);

// Query with enabled filter
foreach (var (entity, _) in SystemAPI.Query<RefRO<SomeComponent>>()
    .WithAll<IsStunned>()
    .WithOptions(EntityQueryOptions.IncludeDisabledEntities))
{
    // Handle stunned entities
}
```

**Use enableable for:**
- High-frequency toggles (stun, burning, invulnerable)
- States that change multiple times per second

**Don't use enableable for:**
- Rare state changes (structural change is fine)
- States that affect many systems (consider separate component)

### Done When

**Success Criteria:** Adding 20 new status effects doesn't change your per-tick cost meaningfully.

**Implementation Checklist:**
- [ ] Create canonical `StatusEffect` buffer (single source of truth)
- [ ] Implement aggregated `StatModifierCache` (recompute on effect changes only)
- [ ] Use change filtering for derived stats (`WithChangeFilter<BaseStats, StatModifierCache>`)
- [ ] Use enableable for frequent state flags (stun, burning, etc.)
- [ ] Performance test: verify adding 20 status effects doesn't increase per-tick cost

---

## 6. Task Allocation Blackboards (Cooperation Without Comm Spam)

### Current State

**Problem:** Entities may duplicate work, no coordination for hauling/building/repair.

### Proposed Solution

**Task allocation blackboards with demand ledgers and reservation protocols.**

#### Rule 1: Demand Ledgers

**Demand ledgers (site needs, stock, reservations):**

```csharp
// ✅ CORRECT: Demand ledger per scope (village/ship/colony)
public struct DemandLedger : IComponentData
{
    public Entity ScopeEntity;  // Village, ship, colony that owns this ledger
}

[InternalBufferCapacity(32)]
public struct DemandEntry : IBufferElementData
{
    public FixedString64Bytes ResourceId;  // "Wood", "Iron", "Food"
    public float NeededAmount;             // How much is needed
    public float ReservedAmount;           // How much is reserved/claimed
    public float AvailableAmount;          // How much is available (stock)
    public Entity TargetEntity;            // Where to deliver (storehouse, construction site)
    public float3 TargetPosition;
    public byte Priority;                  // 0-255, higher = more urgent
}

// Construction sites, storehouses, repair sites publish demands
// Haulers, builders, repair crews read demands and claim reservations
```

#### Rule 2: Reservation/Claim Protocol

**Reservation/claim protocol (no double-hauling):**

```csharp
// ✅ CORRECT: Reservation protocol
[InternalBufferCapacity(8)]
public struct TaskReservation : IBufferElementData
{
    public Entity ReservingEntity;        // Who claimed this task
    public Entity DemandEntryIndex;       // Which demand entry (via Entity reference or index)
    public FixedString64Bytes ResourceId;
    public float ReservedAmount;          // How much reserved
    public uint ReservedTick;             // When reserved
    public uint ExpiryTick;               // When reservation expires (if worker doesn't start)
    public byte Status;                   // Reserved, InProgress, Completed, Cancelled
}

// Protocol:
// 1. Worker reads DemandLedger, finds suitable demand
// 2. Worker creates TaskReservation (claims demand)
// 3. DemandLedger updates ReservedAmount (prevents double-claiming)
// 4. Worker executes task (haul, build, repair)
// 5. On completion, worker removes TaskReservation, updates DemandLedger
```

**Benefits:**
- No double-hauling (only one worker per demand)
- Workers coordinate without explicit communication
- Expiry prevents stuck reservations (worker died, got distracted)

#### Rule 3: Dispatcher/Board Per Scope

**Dispatcher/board per scope (village/ship/colony):**

```csharp
// ✅ CORRECT: Dispatcher per scope
public struct TaskDispatcher : IComponentData
{
    public Entity ScopeEntity;  // Village, ship, colony
    public uint LastDispatchTick;
    public int ActiveWorkers;   // Workers currently assigned tasks
    public int AvailableWorkers; // Workers available for new tasks
}

// Dispatcher responsibilities:
// - Match workers to demands (priority-based)
// - Track active/available workers
// - Handle reservation expiry
// - Distribute work load (don't overload single worker)
```

**Scope Hierarchy:**
- **Village:** One dispatcher per village
- **Ship:** One dispatcher per ship (crew coordination)
- **Colony:** One dispatcher per colony (cross-village coordination)

### Done When

**Success Criteria:** Haulers/builders/repair crews cooperate reliably even with comms off.

**Implementation Checklist:**
- [ ] Create `DemandLedger` + `DemandEntry` buffer (site needs, stock, reservations)
- [ ] Implement `TaskReservation` protocol (claim, expiry, cancellation)
- [ ] Create `TaskDispatcher` per scope (village/ship/colony)
- [ ] Integrate with existing haul/build/repair systems
- [ ] Test: verify no double-hauling, workers coordinate without comms

---

## 7. Navigation/Pathfinding Pipeline Boundaries

### Current State

**Problem:** Navigation/pathfinding may have unclear boundaries, rebake storms on terrain edits.

### Proposed Solution

**Clear pipeline boundaries with versioning for invalidation.**

#### Rule 1: One Interface (PathRequest → Solve → Apply → Steering)

**One interface for navigation:**

```csharp
// ✅ CORRECT: Navigation pipeline interface
public struct PathRequest : IComponentData
{
    public Entity RequestingEntity;
    public float3 StartPosition;
    public float3 EndPosition;
    public PathFlags Flags;              // Avoid water, prefer roads, etc.
    public uint RequestTick;
}

public struct PathResult : IComponentData
{
    public BlobAssetReference<PathBlob> Path;  // Path waypoints
    public float TotalCost;
    public uint PathVersion;                   // Version stamp for invalidation
    public uint ResultTick;
}

// Pipeline stages:
// 1. PathRequest → PathfindingSystem solves → PathResult
// 2. PathResult → MovementSystem applies → Steering
// 3. Steering → LocalTransform updated
```

#### Rule 2: Version Stamps for Invalidation

**Results carry tile/graph version stamps so terrain edits invalidate paths locally:**

```csharp
// ✅ CORRECT: Versioned navigation tiles
public struct NavigationTile : IComponentData
{
    public int2 TileCoord;
    public uint Version;                  // Increments when tile changes
    public BlobAssetReference<NavigationTileBlob> Data;  // Walkability, costs, etc.
}

public struct PathResult : IComponentData
{
    public BlobAssetReference<PathBlob> Path;
    public BlobArray<int2> TouchedTiles;      // Which tiles path traverses
    public BlobArray<uint> TileVersions;      // Version snapshot when path was created
}

// When agent uses path:
// 1. Check if any TouchedTile.Version != TileVersions[i]
// 2. If version mismatch → path is stale → request new path
// 3. Otherwise → path is valid → use it
```

**Terrain Edit Integration:**
```csharp
// When terrain is edited (dig, flood, door opens/closes):
// 1. Mark affected NavigationTiles dirty
// 2. Increment NavigationTile.Version for affected tiles
// 3. Agents using paths through those tiles detect version mismatch
// 4. Agents request new paths (bounded replan, no full rebake)
```

**Benefits:**
- No rebake storms (only affected tiles increment version)
- Local invalidation (agents only replan if their path's tiles changed)
- Bounded replan cost (O(path length), not O(world size))

#### Rule 3: Bounded Replans

**Agents recover with bounded replans:**

```csharp
// ✅ CORRECT: Bounded replan (local A* around changed tiles)
public struct PathReplanRequest : IComponentData
{
    public Entity RequestingEntity;
    public float3 StartPosition;
    public float3 EndPosition;
    public int2 ChangedTileCoord;        // Which tile changed (triggered replan)
    public uint RequestTick;
}

// Replan strategy:
// 1. If changed tile is on current path → replan
// 2. Replan uses local A* (search radius around changed tile)
// 3. If local replan fails → full pathfind from start to end
// 4. Bounded cost: O(radius²), not O(world size)
```

### Done When

**Success Criteria:** Digging + flooding + doors can change the world and agents recover with bounded replans.

**Implementation Checklist:**
- [ ] Define navigation pipeline interface (PathRequest → Solve → Apply → Steering)
- [ ] Implement version stamps on navigation tiles (`NavigationTile.Version`)
- [ ] Store version snapshots in `PathResult` (touched tiles + versions)
- [ ] Add version check in movement system (invalidate stale paths)
- [ ] Integrate with terrain edit systems (dig, flood, doors mark tiles dirty)
- [ ] Test: verify agents replan locally when terrain changes, no rebake storms

---

## 8. Debug + Tests as Contracts (So Incremental Stays Easy)

### Current State

**Problem:** Adding new behaviors is hard to debug/verify, no contracts for testing.

### Proposed Solution

**Debug overlays and test contracts for incremental development.**

#### Rule 1: Per-Group Tick Hash Tests

**Per-group tick hash tests:**

```csharp
// ✅ CORRECT: Tick hash test for determinism
[Test]
public void PerceptionSystem_DeterministicTest()
{
    var world1 = CreateDeterministicWorld(seed: 12345);
    var world2 = CreateDeterministicWorld(seed: 12345);
    
    // Run N ticks
    for (int i = 0; i < 100; i++)
    {
        world1.Update();
        world2.Update();
    }
    
    // Hash critical components
    var hash1 = HashPerceptionComponents(world1);
    var hash2 = HashPerceptionComponents(world2);
    
    Assert.AreEqual(hash1, hash2, "Perception system should be deterministic");
}

// Hash function (Burst-compatible)
uint HashPerceptionComponents(World world)
{
    uint hash = 0;
    
    foreach (var (perceptionState, perceivedBuffer) in 
        world.EntityManager.CreateEntityQuery(typeof(PerceptionState), typeof(PerceivedEntity))
            .ToComponentDataArray<PerceptionState>(Allocator.Temp))
    {
        hash ^= math.hash(perceptionState.LastUpdateTick);
        hash ^= math.hash(perceptionState.PerceivedCount);
        // ... hash other critical fields
    }
    
    return hash;
}
```

**Test Categories:**
- **Determinism tests:** Same inputs → same outputs (rewind/fast-forward friendly)
- **Performance tests:** Verify budgets/limits aren't exceeded
- **Integration tests:** Verify systems work together correctly

#### Rule 2: "Why Did You Do That?" Overlays

**"Why did you do that?" overlays:**

```csharp
// ✅ CORRECT: Debug overlay showing intent/action/perception
public struct DebugAIOverlay : IComponentData
{
    public Entity TargetEntity;
    public FixedString128Bytes IntentReason;      // "Fleeing from threat at (10, 20, 30)"
    public FixedString128Bytes ActionReason;      // "Moving to storehouse (closest)"
    public FixedString128Bytes LastPerception;    // "Saw enemy at distance 15, threat level 80"
    public FixedString128Bytes LastComm;          // "Received order: gather wood"
}

// Debug system draws overlay (Gizmos, UI, etc.)
[BurstDiscard]
public partial struct DebugAIOverlaySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        #if UNITY_EDITOR
        foreach (var (overlay, transform, entity) in
            SystemAPI.Query<RefRO<DebugAIOverlay>, RefRO<LocalTransform>>()
                .WithEntityAccess())
        {
            // Draw debug text above entity
            UnityEditor.Handles.Label(
                transform.ValueRO.Position + new float3(0, 2, 0),
                $"Intent: {overlay.ValueRO.IntentReason}\n" +
                $"Action: {overlay.ValueRO.ActionReason}\n" +
                $"Perception: {overlay.ValueRO.LastPerception}"
            );
        }
        #endif
    }
}
```

**Overlay Information:**
- **Intent:** Current intent mode + reason ("Fleeing from threat", "Following order")
- **Action:** Current action + reason ("Moving to storehouse (closest)", "Attacking enemy")
- **Perception:** Last perceived entities ("Saw enemy at (10, 20, 30), threat 80")
- **Comm:** Last communication received ("Order: gather wood from John")

#### Rule 3: Budgets/Limits Exposed

**Budgets/limits exposed:**

```csharp
// ✅ CORRECT: Exposed budgets/limits
public struct PerceptionBudget : IComponentData
{
    public int MaxEntitiesPerFrame;
    public int ProcessedThisFrame;
    public int SkippedThisFrame;
}

public struct CommunicationBudget : IComponentData
{
    public int MaxMessagesPerFrame;
    public int SentThisFrame;
    public int DroppedThisFrame;
}

public struct PathfindingBudget : IComponentData
{
    public int MaxPathsPerFrame;
    public int RequestedThisFrame;
    public int SolvedThisFrame;
    public int FailedThisFrame;
}

// Debug system exposes budgets to UI/logs
[BurstDiscard]
public partial struct DebugBudgetSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        #if UNITY_EDITOR
        var perception = SystemAPI.GetSingleton<PerceptionBudget>();
        var comms = SystemAPI.GetSingleton<CommunicationBudget>();
        var pathfinding = SystemAPI.GetSingleton<PathfindingBudget>();
        
        Debug.Log($"Perception: {perception.ProcessedThisFrame}/{perception.MaxEntitiesPerFrame} " +
                  $"(skipped: {perception.SkippedThisFrame})");
        Debug.Log($"Comms: {comms.SentThisFrame}/{comms.MaxMessagesPerFrame} " +
                  $"(dropped: {comms.DroppedThisFrame})");
        Debug.Log($"Pathfinding: {pathfinding.SolvedThisFrame}/{pathfinding.MaxPathsPerFrame} " +
                  $"(failed: {pathfinding.FailedThisFrame})");
        #endif
    }
}
```

**Benefits:**
- Immediately see if budgets are hit (performance debugging)
- Identify dropped/failed operations (functional debugging)
- Tune budgets based on real usage

### Done When

**Success Criteria:** You can add a new behavior and immediately see + verify it in headless.

**Implementation Checklist:**
- [ ] Add per-group tick hash tests (determinism validation)
- [ ] Create debug overlay system (intent/action/perception/comm display)
- [ ] Expose budgets/limits (perception, comms, pathfinding)
- [ ] Add headless test runner (verify behaviors in CI)
- [ ] Document debug workflow (how to add overlay for new behavior)

---

## Implementation Order

### Phase 1: Foundation (Critical Path)
1. **Entity Lifecycle Policy** (ECB vs enableable vs structural)
2. **Transform Contract** (LocalTransform + Parent/Child, avoid deep hierarchies)
3. **Physics Contract** (tier split, allocation-free, deterministic)

### Phase 2: Performance (Scalability)
4. **Perception Contract** (change filtering, budgets, unified MindInput)
5. **Conditions/Buffs Contract** (dirty-driven, aggregated cache)
6. **Navigation Contract** (version stamps, bounded replans)

### Phase 3: Coordination (Emergent Behavior)
7. **Task Allocation Blackboards** (demand ledgers, reservations)

### Phase 4: Developer Experience (Incremental Development)
8. **Debug + Tests** (tick hash tests, overlays, exposed budgets)

---

## Related Documentation

- **Time System Advisory:** `Docs/Concepts/Core/Time_System_Advisory.md` - Fixed timestep, rewind correctness
- **Spatial Grid Advisory:** `Docs/Concepts/Core/Spatial_Grid_System_Advisory.md` - Spatial query foundation
- **Perception/Action/Intent Summary:** `Docs/Concepts/Core/Perception_Action_Intent_Summary.md` - AI pipeline overview
- **Unity ECS Documentation:** https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/

---

**For Implementers:** Focus on Phase 1 (entity lifecycle, transform, physics) for immediate correctness  
**For Architects:** Review Phase 2/3 (performance, coordination) for scalability roadmap  
**For Designers:** Consider debug overlays and test contracts when designing new behaviors

