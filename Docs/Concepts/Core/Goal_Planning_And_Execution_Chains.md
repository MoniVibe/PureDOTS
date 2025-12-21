# Goal Planning & Execution Chains — Architecture Plan (PureDOTS)

> Game-agnostic planning/execution framework for large-scale DOTS simulations (Godgame, Space4x, future titles).
> Focus: scalability, deterministic-ish behavior where desired, high flexibility via profiles/policies, minimal per-entity overhead.

---

## 0) Objectives

### Must-haves
- Hierarchical plans (nested subplans; complexity-dependent).
- Multiple active plans per entity (esp. rulers/commanders).
- Multi-actor coordination as a first-class capability.
- Flexible failure handling, replanning/repair, and interruption semantics (profile-driven).
- Frequent capability/resource checks (planning + execution), with optional reservation/claims.
- "Avoid repeating mistakes" learning at configurable scope (agent/archetype/faction), feeding story/events.
- DOTS-friendly: avoid per-entity dynamic buffers by default; avoid structural churn in hot paths.

### Non-goals (for core)
- Hardcoding gameplay rules. Everything is expressed as templates + policies + executors.

---

## 1) Architectural Thesis

**Data-driven templates + Burst-friendly executors.**
- Templates are immutable, shared, cheap to reference (BlobAssets).
- Runtime instances are compact and sparse (only thinkers hold state).
- Scheduling is push-first (event-driven runnable queues), with pull fallback for resilience.

---

## 2) Core Concepts

### 2.1 PlanTemplate (immutable)
A plan template defines:
- Hierarchical control nodes (Sequence / Selector / Decorator / Parallel / etc.)
- Leaf step nodes (StepTypeId + parameters)
- Preconditions/dependencies (Predicate bytecode + references)
- Default scoring hints, typical costs, tags for learning signatures, etc.

**Storage:** BlobAssetReference<PlanTemplate>.

### 2.2 PlanInstance (runtime)
A plan instance binds a template to:
- A target entity / context
- Frame stack (hierarchy execution state)
- Local variables/blackboard references
- Active reservations/claims (optional)
- Progress state (owned by step or external)

**Storage:** Handle-based; minimal fixed header on agent, overflow in central heap.

### 2.3 Step (leaf node)
A step is "atomic" at the framework level:
- It has explicit Start/Update/Complete/Fail semantics.
- It may track progress internally (step-owned) OR refer to an external progress provider (e.g., station/worksite entity).

**Extensibility:** StepTypeId -> StepExecutor system.

### 2.4 BehaviorProfile / Policies
Profiles decide behavior without changing core:
- Arbitration policy (multi-plan selection)
- Interruption policy (pause/cancel/resume)
- Reservation policy (intent/lock/escrow; lease TTL; steal vs wait)
- Repair policy (repair vs replan; aggressiveness)
- Failure response policy (retry/backoff/escalate/abandon/ask-for-help)

---

## 3) Data Model (recommended)

### 3.1 IDs
- TemplateId: stable uint64 (hash of authoring GUID) for save/load stability.
- NodeId: stable uint32 within template.
- StepTypeId / PredicateId: stable uint16/uint32 registry ids.

### 3.2 Agent components
- `ThinkerTag` (only for agents that can plan/execute).
- `BehaviorProfileRef` (Blob ref or id).
- `PlanStackHeader` (fixed-cap inline frames, e.g., 1–4).
- `PlanHeapRef` (optional handle for overflow/multiple plans).
- `AgentBlackboard` (compact cached perceptions / facts / last events).
- `PlanEventTap` (optional small ring for introspection UI/debug).

### 3.3 Central plan heap (sparse, scalable)
A global storage of variable-sized plan instance state:
- Frames, locals, active plan list, planner contexts, etc.
- Addressed by stable handles.

> Rationale: avoids DynamicBuffer per entity, keeps memory proportional to "thinkers" not total entities.

### 3.4 Coordinator entities (multi-actor)
- `CoordinatorTag`
- `SharedPlanInstanceRef`
- `RoleSlots` (fixed-size or pooled buffer via heap)
- `GroupReservations`
- `GroupState` (phase, quorum, deadlines, etc.)

Participants:
- `RoleAssignment` (Coordinator entity + RoleId + obligations)

---

## 4) Template Representation (Blob)

### 4.1 Node types
- Control nodes: Sequence, Selector, Parallel, Decorator (e.g., RepeatUntil, Timeout, Cooldown).
- Leaf nodes: Step node referencing StepTypeId and parameters.

### 4.2 Parameters
- Default params stored in template as compact typed payload:
  - small fixed primitive slots + optional payload blob offset.
- Runtime overrides stored in PlanInstance locals (sparse).

### 4.3 Preconditions / dependencies
Predicates are bytecode-like programs:
- AND/OR/NOT, comparisons, tag checks, inventory check, distance check, relationship check, coordinator state check, etc.
- Predicates read from:
  - agent blackboard,
  - coordinator shared state,
  - resource registry,
  - world indices (spatial hash, influence maps, etc.)

---

## 5) Execution Model (hierarchical call stack)

### 5.1 Frame
Each frame references a node and holds:
- `NodeId`, `PC` (program counter / child index),
- `Status` (Running/Success/Fail/Paused),
- `Locals` (small packed struct + handle to extended locals),
- `RationaleRef` (optional: for explainability).

### 5.2 Single active leaf
Default scalable rule:
- **At most one leaf step progresses per agent per tick** (unless profile overrides).
- Parallelism is expressed via "monitor" steps that enqueue wakeups (push scheduling).

---

## 6) Multi-Plan Model (multiple active plans)

Plans are stored as an ordered set per agent:
- Active plans can be Running / Paused / Pending / Blocked.
- Arbitration chooses which plan advances next (profile-driven).

Recommended baseline:
- Use a **priority queue** + optional fairness weights.
- Support "stack-like interrupts" (push survival plan on top).

---

## 7) Scheduling (push-first)

### 7.1 Runnable queues
Partitioned queues (by spatial sector / chunk group / worker index):
- Entries: (AgentEntity, PlanHandle, FrameId, WakeReason).
- Enqueue triggers:
  - precondition became true,
  - awaited event arrived,
  - reservation lease updated/expired,
  - dependency completed,
  - explicit "think now" signal (threat, command, etc.)

### 7.2 Pull fallback
A slow safety net:
- periodically scan for:
  - agents with stale runnable state,
  - missed wakeups,
  - new thinkers.

> Important: pull is not the main loop.

---

## 8) Planning & Repair (budgeted, resumable)

### 8.1 Planner interface
- `RequestPlan(goal)` -> returns PlanInstanceHandle or Pending (time-sliced).
- Planner operates in slices with a budget:
  - expansions per slice,
  - max frontier size,
  - max repair edits.

### 8.2 Template expansion vs search
- Cheap: goal -> known template recipe(s).
- Expensive: local search/repair when execution fails or world changes.

### 8.3 Repair-first default
- Repair policy determines whether to:
  - swap a step,
  - reroute navigation,
  - substitute resource/tool,
  - renegotiate roles,
  - or replan from scratch.

---

## 9) Reservations & Contention (lease-based tokens)

### 9.1 Reservation types
- Soft intent (default): records intent; can be stolen; conflict is handled.
- Hard lock: exclusive claim; must have TTL + release.
- Escrow: resource removed from pool into reservation bucket.

### 9.2 Lease rules
- Every reservation has TTL + refresh.
- Expiry auto-releases and emits a PlanEvent (useful for debugging/story).
- Acquisition order should be deterministic by key to reduce deadlock cycles.

### 9.3 Conflict resolution primitives
Policy chooses among:
- wait (with backoff),
- steal (with consequences),
- negotiate (coordinator mediated),
- substitute (different tool/resource),
- escalate (ask higher authority / ruler / player influence).

---

## 10) Failure Signatures & "Avoid Repeating Mistakes"

### 10.1 Failure signature key
Compact key:
- TemplateId, NodeId, FailureCode, ContextTagBucket
- Optional: "resource class" and "target class" ids.

### 10.2 Memory forms
- hard blacklist (never attempt this pairing for some duration),
- cost inflation (soft avoid),
- cooldown,
- strategy swap table (try alternative template/step).

### 10.3 Scope
Memory stores can exist at:
- per-agent,
- per-archetype/job,
- per-faction/settlement.

---

## 11) Event Bus (story + debugging without churn)

### 11.1 Transport
Use ring buffers, not event entities:
- per-partition ring buffer (high throughput),
- optional per-agent ring (tiny, debug UI).

### 11.2 Event types (suggested)
- GoalChosen, PlanCreated, StepStarted, StepSucceeded, StepFailed,
- PlanPaused, PlanResumed, PlanCancelled, PlanRepaired, PlanReplanned,
- ReservationAcquired, ReservationLost, ReservationExpired,
- RoleAssigned, RoleDropped, GroupPhaseChanged.

Story layer consumes filtered/highlighted events; debug tools can inspect full stream.

---

## 12) Save/Load & Versioning

Persist:
- active plan list,
- frame stacks + locals,
- reservations (leases),
- planner contexts (optional; can drop and replan on load),
- mistake-avoidance memory stores.

Template evolution:
- mapping table for TemplateId/NodeId migration where needed.
- if mapping missing: fall back to safe replan.

---

## 13) System Groups (order-of-execution)

1) **Sense/Blackboard Update**
   - ingest world events, update cached facts.

2) **Coordinator Update**
   - role slot management, group phases, group-level dependencies.

3) **Reservation Lease Update**
   - refresh/expire; emit relevant wakeups; enqueue affected agents.

4) **Runnable Drain (Execution)**
   - advance selected plan frames per agent (default: 1 leaf step advancement).

5) **Failure/Learning Update**
   - write failure signatures; update memory stores; enqueue repairs if needed.

6) **Planning Slices**
   - create new plans and repair existing ones within budgets.

7) **Event Export**
   - publish ring buffers to story/debug consumers.

---

## 14) Integration Contracts (PureDOTS API surface)

### 14.1 Registries
- `StepRegistry`: StepTypeId -> executor metadata (capabilities, expected cost, flags).
- `PredicateRegistry`: PredicateId -> evaluator metadata.
- `GoalRegistry`: GoalTypeId -> candidate templates / planners.

### 14.2 Interfaces (conceptual)
- `IStepExecutor`: Start / Tick / Interrupt / Cancel / Complete / Fail.
- `IPredicateEvaluator`: Evaluate(predicate, context) -> bool + optional reason.
- `IArbitrationPolicy`: ChooseNextPlan(agent, activePlans, context) -> plan handle.
- `IReservationPolicy`: Request/Refresh/Release; resolve conflicts.
- `IRepairPolicy`: AttemptRepair(failure, context) -> repair ops or request replan.

### 14.3 Debug/Explain
- optional: store `RationaleRef` per frame/step for "why did you do that?" tools.

---

## 15) Config Knobs (soft budgets; LOD-friendly)

Even with "no hard maximums", define knobs to degrade gracefully:
- planner expansions per slice,
- max concurrent active plans per agent (soft: deprioritize),
- max coordination group size (soft: subdivide),
- queue drain limit per partition,
- max reservation count per agent (soft: collapse or escrow at coordinator),
- event sampling rates for story vs debug.

---

## 16) Implementation Plan (milestones)

### Milestone A — Minimal execution engine (no search planner)
- Blob templates + frame stack
- Step executors (Start/Tick/Complete/Fail)
- Push runnable queue
- Plan events ring buffer

### Milestone B — Multi-plan + arbitration
- multiple active plans per agent
- policy-driven arbitration + survival interrupts

### Milestone C — Coordinator multi-actor
- coordinator entity + role slots
- participant assignment + shared plan progress

### Milestone D — Reservations + leases
- intent/lock/escrow primitives
- TTL refresh and conflict events

### Milestone E — Repair + mistake avoidance
- failure signature memory store
- repair operations + replan requests

### Milestone F — Save/load + migration hooks
- persistence for frames, leases, memory
- template mapping strategy

---

## 17) Open Items (architecture-only)
- Concurrency inside a plan: do we keep "single active leaf" always, and model monitoring via enqueue-only steps?
- Predicate bytecode instruction set: minimal viable set vs extended set.
- PlanHeap implementation: contiguous slabs + free lists vs paged arena.
- Determinism level: event ordering and reservation acquisition order.

---

## Appendix — Glossary
- **BlobAsset**: immutable, shared template storage.
- **PlanHeap**: central storage for variable-sized plan instances.
- **Lease**: reservation token with TTL refresh.
- **Frame**: execution state for a template node.
