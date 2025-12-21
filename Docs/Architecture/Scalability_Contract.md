# Scalability Contract (Million+ Entity Sims)

**Status**: Locked (guardrails; policy-tunable)  
**Category**: Architecture / Performance / AI Foundation  
**Applies To**: Godgame, Space4X, shared PureDOTS  

This document codifies the minimum architectural contract required to scale to **millions of entities** while preserving determinism, rewind/time manipulation plans, and core gameplay invariants.

If you violate this contract, the sim will either:
- go quadratic (CPU blow-ups),
- fragment chunks (iteration blow-ups),
- or become non-deterministic / rewind-hostile (debugging blow-ups).

Related:
- `Docs/Architecture/Performance_Optimization_Patterns.md` (phase order, tier profiles, budgeting)
- `Docs/Architecture/Save_Load_Determinism.md`
- `Docs/Module_Recipe_Template.md`
- `Docs/Concepts/Core/Entity_Lifecycle.md` (fidelity gating; anatomy)
- `Docs/Concepts/Core/Information_Propagation.md` (bounded knowledge + networks)
- `Docs/Concepts/Core/Skill_And_Attribute_Progression.md` (virtualized storage by LOD)

---

## 0) Definitions

- **Resident entities**: ECS entities that exist in the World and are iterated by systems this tick.
- **Virtualized population**: compressed state stored outside the ECS “resident set” (SoA pages / buckets / aggregates) that advances using event-driven rules and is materialized on demand.
- **Sim tier / fidelity tier**: a low-cardinality classification controlling *evaluation frequency* and *detail* (not “truth”). Tiers must be *tier-safe* (see below).
- **Hot state**: updated frequently in tight loops (movement, needs deltas, core health, command tickets).
- **Cold state**: rarely changes (profile definitions, anatomy templates, culture/race identities, static role descriptions).
- **High-cardinality value**: can take thousands+ unique values (e.g., ProfileId, unique captain identity, unique ideology mix).

---

## 1) Invariants (non-negotiable)

### 1.1 Determinism + rewind
- Simulation must be tick-driven (`TimeState.Tick`), not frame-driven.
- Mutating gameplay systems must not run during rewind playback (`RewindMode.Record` only).
- Randomness must be seeded deterministically (per-entity/per-event streams), never from wall-clock time.

### 1.2 Tier-safe core state
“Truth” cannot depend on whether an entity is at Tier0 or Tier2.

- **Core state (tier-invariant)**: accounting/economy, ownership, authoritative combat state transitions, progression totals, major governance outcomes.
- **Detail state (tier-dependent)**: micro path refinement, cosmetic perception refinement, extra debug-only signals, presentation proxies.

If a system affects Core State, it must not rely on computations that are only performed at higher tiers.

### 1.3 Conservation across tier transitions
Promotion/demotion between resident↔virtualized must preserve invariants:
- resources conserved,
- XP/progression integrated over time (store rates + last-applied tick),
- buff durations tracked by tick (not frames),
- deterministic event ordering preserved.

---

## 2) Resident vs Virtual (the “billions only if…” rule)

### 2.1 Rule
- **Millions**: feasible with ECS-resident entities + careful archetype discipline and event-driven evaluation.
- **Beyond that**: requires **virtualized populations** and **simulation LOD** so most “people” are not resident entities.

### 2.2 How virtualization works here
- Keep ECS for:
  - near/interesting micro actors,
  - aggregates (villages, fleets, crew mass),
  - authority seats and named characters that drive narrative/governance.
- Store far/idle populations in compressed SoA pages:
  - aggregated needs rates,
  - counts by profile buckets,
  - summarized skills/progression distributions,
  - queued “macro events” (birth/death/migration/shortage/raid outcomes).
- Materialize individuals only when:
  - they enter an interest radius,
  - an event targets them specifically,
  - or a scenario promotes them (story/named promotion).

Design requirement: the *API* stays stable across tiers (e.g., “skill container rule”), even if physical storage changes.

---

## 3) Data placement (hot vs cold)

### 3.1 Cold definitions belong in BlobAssets
Use BlobAssets for immutable, shared definitions:
- profile definitions / behavior archetypes,
- anatomy templates,
- policy tables (tiers, budgets, trust curves),
- utility/action definitions (AI).

Avoid per-entity copies of cold definitions.

### 3.2 Hot state stays compact
Hot state must be:
- fixed-size when possible,
- quantized where acceptable,
- updated incrementally (see §5).

Keep “high-cardinality, low-touch” data out of chunk-fragmenting patterns.

---

## 4) Archetype discipline (Unity DOTS realities)

### 4.1 SharedComponentData is low-cardinality only
**Never** put high-cardinality values into `ISharedComponentData` (chunk fragmentation).

Allowed shared-component uses:
- `SimTier` / fidelity band (single digit to low tens),
- biome/region buckets (low tens),
- faction groupings (only if low-cardinality in the current scenario).

Not allowed:
- `ProfileId` (thousands unique),
- per-entity identity,
- per-entity ideology mixes,
- per-entity “unique settings”.

### 4.2 Toggles: enableable > structural churn
Prefer:
- enableable components,
- bitflags,
- small state enums,
over frequent add/remove in hot loops.

Structural changes must be ECB-funneled and ideally centralized to avoid sync points.

### 4.3 Dynamic buffers: capacity is a budget
Dynamic buffers are valid, but internal capacity must be chosen intentionally.
- Rare/variable counts: small internal capacity with spill tolerance.
- Hot-path buffers: keep bounded; prefer ring buffers or pooled pages when counts grow.

---

## 5) Incremental evaluation (no “recompute everything”)

### 5.1 Derived stat DAG + dirty propagation
Derived values should update only when inputs change.

Pattern:
- inputs change → mark a small “dirty” signal (`*DirtyTag` / enableable flag / version check),
- derived system runs only for dirty entities or changed chunks,
- derived outputs cached until invalidated again.

### 5.2 Change filtering is mandatory at scale
Prefer:
- changed version filters / `WithChangeFilter` (when appropriate),
- cadence gates (tick-based),
- and bounded queues for expensive work (paths, LOS refinement, verification).

Never do “scan all buffs / scan all relations / scan all neighbors” per tick.

---

## 6) Effects & buffs (O(1) apply/remove, no per-tick scans)

Contract:
- represent effects as instances (buffer) *only when needed*,
- maintain a compact aggregated modifier cache per entity,
- update cache only on add/remove/expiry,
- handle expiry via a tick wheel / bucketed timers (tier-aware).

---

## 7) Limbs/organs/anatomy (fidelity-gated)

Contract:
- do not model “every limb, always” for all entities.

Two scalable representations:
1) **Fixed-layout anatomy** (fast default): slot arrays/bitfields + small numeric arrays.
2) **Sparse limb entities** (flexible, higher entity count): only instantiate limb entities when granularity is needed (damage/specials).

Tie anatomy granularity to fidelity tier (see `Entity_Lifecycle.md` anatomy gating).

---

## 8) Formations/cohorts (treat the formation as the agent)

At high counts:
- a formation/cohort entity owns the plan/path and the frame.
- members are followers computing local steering toward their slot offset.

Heavy local avoidance (e.g., ORCA/RVO) is Tier0/Tier1 only; lower tiers use cheap occupancy/separation forces.

---

## 9) Navigation at scale (hierarchy + fields)

Contract:
- do not run A* per unit at high counts.
- prefer:
  - hierarchical path planning for strategic moves,
  - flow fields for many agents sharing a destination,
  - incremental cost-field updates and localized invalidation.

---

## 10) Simulation LOD (discrete-event when possible)

Define sim tiers with explicit guarantees:
- **Tier0 (Full)**: continuous updates, rich micro behaviors.
- **Tier1 (Reduced frequency)**: same logic, lower cadence + budgets.
- **Tier2 (Event-driven)**: predicted next-change tick; no work between events.
- **Tier3 (Aggregate-only)**: virtualized population + conserved deltas only.

Tier transitions must be deterministic and conserve core invariants (§1.3).

---

## 11) Module requirement (every new feature must declare its cost model)

Any new module must explicitly document:
- hot vs cold components,
- resident vs virtual behavior (what happens when entity is not resident),
- tier behavior (cadence, budgets, fidelity),
- determinism/rewind category (derived vs event-logged vs delta-logged),
- storage bounds (buffers/rings/pools),
- and high-cardinality risks (shared component avoidance).

Use `Docs/Module_Recipe_Template.md` as the enforcement template.

---

## 12) Anti-patterns (things that will break million-scale)

- High-cardinality `ISharedComponentData` (e.g., `ProfileId` as shared).
- Per-tick full recomputation of derived stats “for correctness”.
- Frequent structural churn (add/remove) in hot loops.
- Unbounded per-entity buffers for relations/rumors/buffs without pooling/paging.
- “Scan all neighbors” loops without spatial hashing + budgets.
- Frame-time driven durations or expiry (must be tick-based).

