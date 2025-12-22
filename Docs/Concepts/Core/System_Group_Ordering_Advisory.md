# System Group Ordering Advisory

**Status:** Recommendations / Action Plan
**Category:** Core - System Execution Order Tightening
**Audience:** Architects / Implementers
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Purpose

**Primary Goal:** Tighten the system group ordering contract to ensure stable, correct execution. The Perception → Interrupts → AI → Domain chain is correct; this advisory addresses physics placement, comms→interrupt timing, duplicated sensing, transport semantics, and ECB boundaries.

**Focus Areas:**
1. **Physics semantics** — Correct PhysicsSystemGroup representation and hooks
2. **Comms → interrupt timing** — Ensure same-tick message→interrupt conversion
3. **Duplicated sensing** — Remove or derive AISensorUpdateSystem from Perception
4. **Transport phase semantics** — Clarify 1-tick latency vs same-tick actuation
5. **ECB boundaries** — Document structural change boundaries
6. **Enableable wake tags** — Change filtering for performance

---

## 1. Fix Physics Semantics in Top-Level Diagram

### Current State

**Problem:** High-level order diagram lists `BuildPhysicsWorld` ... `ExportPhysicsWorld` as bookends, but Unity Physics 1.0+ uses `PhysicsSystemGroup` with sub-groups.

### Proposed Solution

**Replace "BuildPhysicsWorld / ExportPhysicsWorld" with PhysicsSystemGroup:**

#### Unity Physics 1.0+ Pipeline

Unity Physics 1.0+ uses:
- **PhysicsSystemGroup** containing:
  - **PhysicsInitializeGroup** (initialize physics state)
  - **PhysicsSimulationGroup** (physics simulation)
  - **ExportPhysicsWorld** (last system, copies results back to ECS)

#### Explicit Hooks

**Create explicit hooks for physics integration:**

```csharp
// BeforePhysicsSystemGroup: Apply AI steering/desired velocities/kinematic moves
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial class BeforePhysicsSystemGroup : InstrumentedComponentSystemGroup { }

// AfterPhysicsSystemGroup: Read contacts, damage, collision events
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial class AfterPhysicsSystemGroup : InstrumentedComponentSystemGroup { }
```

**Usage:**
- **BeforePhysicsSystemGroup:** Systems that apply movement/forces before physics simulation
  - AI steering systems (apply desired velocities)
  - Kinematic movement systems (update positions)
  - Force application systems (apply impulses)
  
- **AfterPhysicsSystemGroup:** Systems that read physics results after simulation
  - Contact detection systems (read collision contacts)
  - Damage systems (process collision damage)
  - Physics event systems (process triggers, collisions)

**Why It Matters:**
- Physics can run 0+ times per frame (fixed step / catch-up), so ordering needs to be stable
- Structural changes to physics bodies between physics init and export can break assumptions
- Unity Physics docs emphasize pipeline ordering and "export is last" contract

#### Move History Snapshots After Physics Export

**Current Problem:** History is under LateSimulation, before ExportPhysicsWorld in the diagram.

**Solution:** Move HistorySystemGroup to run after PhysicsSystemGroup.

**Why:**
- If physics writes back transforms/velocities during export, snapshots must happen after
- ExportPhysicsWorld is explicitly "the last system to run in PhysicsSystemGroup" and copies results back to ECS
- Snapshots before export would record stale state

**Updated Ordering:**
```
SimulationSystemGroup:
├─ EnvironmentSystemGroup
├─ SpatialSystemGroup
├─ GameplaySystemGroup
├─ BeforePhysicsSystemGroup
├─ PhysicsSystemGroup (PhysicsInitializeGroup → PhysicsSimulationGroup → ExportPhysicsWorld)
├─ AfterPhysicsSystemGroup
└─ LateSimulationSystemGroup
    └─ HistorySystemGroup (snapshots after physics export)
```

---

## 2. Comms → Interrupts Should Happen in the Same Tick

### Current State

**Problem:** Perception group runs communication systems, then `PerceptionToInterruptBridgeSystem` after `PerceptionUpdateSystem`, but message received/order received interrupts should happen in the same tick.

**Current Ordering:**
```
PerceptionSystemGroup:
├─ PerceptionUpdateSystem
├─ CommunicationAttemptBuildSystem
├─ CommunicationDispatchSystem
├─ CommunicationDecodeDecideSystem
└─ PerceptionToInterruptBridgeSystem (after PerceptionUpdateSystem)
```

**Issue:** `PerceptionToInterruptBridgeSystem` runs after `PerceptionUpdateSystem`, but before `CommunicationDecodeDecideSystem`. Messages decoded/decided don't become interrupts until next tick.

### Proposed Solution

**Option A: Move PerceptionToInterruptBridgeSystem After CommunicationDecodeDecideSystem**

```csharp
[UpdateInGroup(typeof(PerceptionSystemGroup))]
[UpdateAfter(typeof(CommunicationDecodeDecideSystem))]  // Changed from PerceptionUpdateSystem
public partial struct PerceptionToInterruptBridgeSystem : ISystem
{
    // Bridges perception changes AND comm receipts to interrupts
}
```

**Option B: Add CommReceiptToInterruptBridgeSystem**

```csharp
[UpdateInGroup(typeof(PerceptionSystemGroup))]
[UpdateAfter(typeof(CommunicationDecodeDecideSystem))]
public partial struct CommReceiptToInterruptBridgeSystem : ISystem
{
    // Bridges comm receipts (messages, orders, acks, clarify requests) to interrupts
    // Emits: NewOrder, OrderCancelled, MessageReceived, etc.
}
```

**Recommended:** Option B (separate system) for clarity, but Option A works if `PerceptionToInterruptBridgeSystem` handles both.

**Goal:** Orders/acks/clarify requests become Interrupts before `InterruptSystemGroup` runs that tick.

**Updated Ordering:**
```
PerceptionSystemGroup:
├─ PerceptionUpdateSystem
├─ CommunicationAttemptBuildSystem
├─ CommunicationDispatchSystem
├─ CommunicationDecodeDecideSystem
├─ PerceptionToInterruptBridgeSystem (after PerceptionUpdateSystem, before comm bridge)
└─ CommReceiptToInterruptBridgeSystem (after CommunicationDecodeDecideSystem)
    └─ Bridges comm receipts → interrupts (NewOrder, MessageReceived, etc.)
```

---

## 3. Remove Duplicated Sensing (Or Make It Explicitly "Derived")

### Current State

**Problem:** Spatial sensing happens twice:
1. **Perception pipeline** queries spatial grid, fills `PerceivedEntity` buffers
2. **AISystemGroup** runs `AISensorUpdateSystem` to query spatial grid, fill `AISensorReading` buffers

**Issues:**
- Duplicate work (two systems query same spatial grid)
- Two systems may disagree about reality (different update cadences, different filters)
- Increases cost (O(N) queries twice)

### Proposed Solution

**Make Perception outputs the canonical "MindInput" for AI:**

#### Option A: Deprecate AISensorUpdateSystem (Recommended)

**Timeline:**
1. **Phase 1:** Create unified `MindInput` buffer that reads from `PerceivedEntity` buffers
2. **Phase 2:** Update `AIUtilityScoringSystem` to read from `MindInput` instead of `AISensorReading`
3. **Phase 3:** Deprecate `AISensorUpdateSystem` (mark as `[Obsolete]`)
4. **Phase 4:** Remove `AISensorUpdateSystem` after migration complete

**AISystemGroup becomes:**
```
AISystemGroup:
├─ AIVirtualSensorSystem (internal needs: hunger, energy, morale)
├─ AIUtilityScoringSystem (reads MindInput + virtual sensors)
├─ AISteeringSystem
└─ AITaskResolutionSystem
```

**Benefits:**
- Single source of truth (Perception outputs are canonical)
- Reduced cost (one spatial query instead of two)
- Consistent reality (AI and perception systems agree)

#### Option B: Make AISensorUpdateSystem Derived (If Not Ready to Delete)

**Declare it as derived and ensure it reads only from Perception outputs:**

```csharp
/// <summary>
/// [DERIVED] Populates AISensorReading from Perception outputs.
/// DO NOT query spatial grid directly. Read from PerceivedEntity buffers.
/// </summary>
[UpdateInGroup(typeof(AISystemGroup))]
[UpdateAfter(typeof(PerceptionSystemGroup))]  // Ensure perception runs first
public partial struct AISensorUpdateSystem : ISystem
{
    // Read from PerceivedEntity buffers, populate AISensorReading
    // No spatial grid queries
}
```

**Migration Path:**
- Add `[UpdateAfter(typeof(PerceptionSystemGroup))]` to ensure ordering
- Refactor `AISensorUpdateSystem` to read from `PerceivedEntity` buffers instead of spatial queries
- Document as "derived" system (doesn't query world, only transforms perception data)

### Done When

**Success Criteria:** Either `AISensorUpdateSystem` is removed, or it's explicitly marked as derived and reads only from Perception outputs (no spatial queries).

---

## 4. Clarify How TransportPhaseGroup Fits the Tick Contract

### Current State

**Problem:** `TransportPhaseGroup` currently runs before Gameplay/AI, but it's unclear whether it consumes commands/reservations from previous tick or same tick.

### Proposed Solution

**Pick one explicit rule:**

#### Rule A: 1-Tick Latency (Transport Executes Previous Tick's Commands)

**Semantics:** Transport executes "confirmed shipments / in-flight moves" from last tick.

**Ordering:**
```
SimulationSystemGroup:
├─ SpatialSystemGroup
├─ TransportPhaseGroup (executes previous tick's confirmed shipments)
├─ GameplaySystemGroup
│   ├─ AISystemGroup (generates new commands this tick)
│   └─ DomainExecutionGroups (execute behaviors, create transport requests)
```

**When to use:** Transport needs time to confirm/reserve before execution (realistic logistics delay).

#### Rule B: Same-Tick Actuation (Transport Consumes This Tick's Commands)

**Semantics:** Transport consumes `AICommand` generated this tick.

**Ordering:**
```
SimulationSystemGroup:
├─ SpatialSystemGroup
├─ GameplaySystemGroup
│   ├─ AISystemGroup (generates commands this tick)
│   ├─ DomainExecutionGroups (execute behaviors, create transport requests)
│   └─ TransportPhaseGroup (executes same tick's commands)
```

**When to use:** Transport should respond immediately (same-tick execution, no delay).

### Recommendation

**Document the chosen rule explicitly:**

```markdown
## TransportPhaseGroup Semantics

**Rule:** [Choose A or B]

**Rule A (1-Tick Latency):** Transport executes confirmed shipments from previous tick.
- TransportPhaseGroup runs before GameplaySystemGroup
- Consumes commands/reservations created last tick
- Provides 1-tick latency for logistics confirmation

**Rule B (Same-Tick Actuation):** Transport executes commands generated this tick.
- TransportPhaseGroup runs after AISystemGroup / DomainExecutionGroups
- Consumes AICommand / transport requests created this tick
- Provides immediate transport execution

**Current Implementation:** [Document which rule is used]
```

**Why It Matters:**
- Implementers need clear contract (don't accidentally create half-and-half semantics)
- Affects when transport requests are processed
- Determines ordering constraints

---

## 5. Add ECB Boundaries to the Contract

### Current State

**Problem:** Ordering doc doesn't mention where structural changes are allowed.

### Proposed Solution

**Add explicit ECB boundary contract:**

```markdown
## Structural Change Policy

**Rule:** All structural changes (AddComponent, RemoveComponent, CreateEntity, DestroyEntity) must be recorded and played back via ECB systems at the group boundary.

**ECB Systems:**
- **BeginSimulationEntityCommandBufferSystem:** Runs at the start of SimulationSystemGroup
- **EndSimulationEntityCommandBufferSystem:** Runs at the end of SimulationSystemGroup
- **BeginFixedStepSimulationEntityCommandBufferSystem:** Runs at the start of FixedStepSimulationSystemGroup
- **EndFixedStepSimulationEntityCommandBufferSystem:** Runs at the end of FixedStepSimulationSystemGroup

**Usage:**
```csharp
// In any system:
var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

// Record structural changes
ecb.AddComponent<NewComponent>(entity);
ecb.RemoveComponent<OldComponent>(entity);
ecb.CreateEntity();

// ECB plays back at group boundary (deterministic ordering)
```

**Why It Matters:**
- Structural changes must be batched for performance (fewer sync points)
- Deterministic ordering (ECB playback order is stable)
- Critical for physics + rewind + prediction/rollback (avoids mid-frame structural changes)
```

**Reference:** [Unity ECS - EntityCommandBuffer](https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/ecb.html)

---

## 6. Suggested Revised Top-Level Ordering

**Revised mental model (drop-in replacement):**

```
FixedTickSimulationGroup (one full tick):
├─ EnvironmentSystemGroup
├─ SpatialSystemGroup
│   └─ PerceptionSystemGroup
│       ├─ PerceptionSignalFieldUpdateSystem
│       ├─ PerceptionSignalSamplingSystem
│       ├─ PerceptionUpdateSystem
│       ├─ CommunicationAttemptBuildSystem
│       ├─ CommunicationDispatchSystem
│       ├─ CommunicationDecodeDecideSystem
│       ├─ PerceptionToInterruptBridgeSystem
│       └─ CommReceiptToInterruptBridgeSystem (NEW)
│
├─ GroupDecisionSystemGroup
├─ InterruptSystemGroup (processes interrupts, writes EntityIntent)
├─ AISystemGroup
│   ├─ AIVirtualSensorSystem
│   ├─ AIUtilityScoringSystem (reads MindInput from Perception)
│   ├─ AISteeringSystem
│   └─ AITaskResolutionSystem
│
├─ DomainExecutionGroups
│   ├─ VillagerSystemGroup
│   ├─ ResourceSystemGroup
│   ├─ ConstructionSystemGroup
│   └─ TransportPhaseGroup (if Rule B: same-tick actuation)
│
├─ BeforePhysicsSystemGroup (NEW: apply movement/forces)
│   └─ Systems that apply AI steering, kinematic movement, forces
│
├─ PhysicsSystemGroup
│   ├─ PhysicsInitializeGroup
│   ├─ PhysicsSimulationGroup
│   └─ ExportPhysicsWorld (last system, copies results to ECS)
│
├─ AfterPhysicsSystemGroup (NEW: read contacts/damage/events)
│   └─ Systems that read physics results (contacts, damage, events → interrupts)
│
└─ LateSimulationSystemGroup
    └─ HistorySystemGroup (snapshots AFTER physics export)
```

**Key Changes:**
1. PhysicsSystemGroup replaces BuildPhysicsWorld/ExportPhysicsWorld
2. BeforePhysicsSystemGroup / AfterPhysicsSystemGroup hooks added
3. HistorySystemGroup moved after PhysicsSystemGroup
4. CommReceiptToInterruptBridgeSystem added after CommunicationDecodeDecideSystem
5. TransportPhaseGroup placement depends on chosen rule (A or B)

---

## 7. Enableable "Wake" Tags for Change Filtering

### Current State

**Problem:** Systems may scan millions of entities when only a few changed.

### Proposed Solution

**Add enableable "wake" tags and set them at key points:**

#### Wake Tag Components

```csharp
// Enableable wake tags
public struct PerceptionDirty : IComponentData, IEnableableComponent { }
public struct HasCommReceipts : IComponentData, IEnableableComponent { }
public struct HasInterrupts : IComponentData, IEnableableComponent { }
public struct DecisionDirty : IComponentData, IEnableableComponent { }
public struct NeedsDirty : IComponentData, IEnableableComponent { }
```

#### Wake Tag Setting Points

**PerceptionUpdateSystem / PerceptionSignalSamplingSystem:**
```csharp
// Enable PerceptionDirty on entities whose perceived set or signal level changed
if (perceivedBufferChanged || signalLevelChanged)
{
    entityManager.SetComponentEnabled<PerceptionDirty>(entity, true);
}
```

**CommunicationDecodeDecideSystem:**
```csharp
// Enable HasCommReceipts on receivers
if (messageReceived || orderReceived)
{
    entityManager.SetComponentEnabled<HasCommReceipts>(entity, true);
}
```

**PerceptionToInterruptBridgeSystem / CommReceiptToInterruptBridgeSystem:**
```csharp
// Enable HasInterrupts for entities that got a new interrupt
if (interruptAdded)
{
    entityManager.SetComponentEnabled<HasInterrupts>(entity, true);
}
```

**InterruptHandlerSystem:**
```csharp
// Enable DecisionDirty when EntityIntent changes
// Disable HasInterrupts after consumption
if (intentChanged)
{
    entityManager.SetComponentEnabled<DecisionDirty>(entity, true);
    entityManager.SetComponentEnabled<HasInterrupts>(entity, false);
}
```

**AIUtilityScoringSystem:**
```csharp
// Query only entities with dirty flags
foreach (var (entity, _) in SystemAPI.Query<RefRO<SomeComponent>>()
    .WithAll<DecisionDirty, PerceptionDirty, NeedsDirty>()
    .WithOptions(EntityQueryOptions.IncludeDisabledEntities))
{
    // Only process entities that actually changed
    // ... evaluate utility ...
    
    // Clear dirty flags after processing
    entityManager.SetComponentEnabled<DecisionDirty>(entity, false);
    entityManager.SetComponentEnabled<PerceptionDirty>(entity, false);
    entityManager.SetComponentEnabled<NeedsDirty>(entity, false);
}
```

#### Benefits

- **Cost proportional to changes:** Only process entities that actually changed
- **Complements ordering:** Works with system ordering to reduce work
- **Enableable components:** Efficient toggling without archetype changes
- **Change filtering:** Systems can use `WithAll<PerceptionDirty>()` to filter queries

---

## Implementation Checklist

### Phase 1: Physics Semantics
- [ ] Replace BuildPhysicsWorld/ExportPhysicsWorld with PhysicsSystemGroup in documentation
- [ ] Create BeforePhysicsSystemGroup and AfterPhysicsSystemGroup hooks
- [ ] Move HistorySystemGroup after PhysicsSystemGroup
- [ ] Update SystemGroups.cs with new groups

### Phase 2: Comms → Interrupt Timing
- [ ] Add CommReceiptToInterruptBridgeSystem (or update PerceptionToInterruptBridgeSystem)
- [ ] Ensure comm receipts → interrupts happen before InterruptSystemGroup runs
- [ ] Test: verify orders/messages become interrupts same tick

### Phase 3: Remove Duplicated Sensing
- [ ] Option A: Create unified MindInput buffer, deprecate AISensorUpdateSystem
- [ ] Option B: Make AISensorUpdateSystem derived (reads from Perception outputs only)
- [ ] Update AIUtilityScoringSystem to use MindInput
- [ ] Document as "derived" or remove

### Phase 4: Transport Semantics
- [ ] Choose rule (A: 1-tick latency, or B: same-tick actuation)
- [ ] Document chosen rule explicitly
- [ ] Update TransportPhaseGroup placement based on rule
- [ ] Verify transport execution matches documented rule

### Phase 5: ECB Boundaries
- [ ] Document structural change policy (ECB at group boundaries)
- [ ] Add examples to documentation
- [ ] Verify systems use ECB for structural changes

### Phase 6: Enableable Wake Tags
- [ ] Create enableable wake tag components (PerceptionDirty, HasCommReceipts, etc.)
- [ ] Add wake tag setting in PerceptionUpdateSystem, CommunicationDecodeDecideSystem, etc.
- [ ] Update AIUtilityScoringSystem to query only dirty entities
- [ ] Performance test: verify cost proportional to changes

---

## Related Documentation

- **System Group Ordering Summary:** `Docs/Concepts/Core/System_Group_Ordering_Summary.md` - Current ordering documentation
- **AI Behavior Contracts Advisory:** `Docs/Concepts/Core/AI_Behavior_Contracts_Advisory.md` - Entity lifecycle, enableable components
- **Unity Physics Documentation:** https://docs.unity.cn/Packages/com.unity.physics@1.0/manual/
- **Unity ECS - EntityCommandBuffer:** https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/ecb.html

---

**For Architects:** Review revised top-level ordering and ensure physics/history/comms placement is correct  
**For Implementers:** Follow implementation checklist, prioritize Phase 1-2 (physics semantics, comms timing)  
**For Designers:** Understand wake tag system for change filtering (reduces unnecessary work)

