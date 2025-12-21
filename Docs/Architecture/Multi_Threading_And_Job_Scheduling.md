# Multi-Threading and Job Scheduling

## Overview

**Status**: Locked v1  
**Category**: Architecture / Performance / Best Practices  
**Scope**: PureDOTS + game-side systems that run in the authoritative simulation  
**Applies to**: All simulation systems (AI, economy, combat, movement, sensors, world logic)

This document defines the recommended policy for **Burst job scheduling**, **dependency management**, **parallel patterns**, and **thread-safety** across systems. It is designed for:
- Very high entity counts (agents, ships, villages/colonies, projectiles, resources).
- Moddable features (systems may "freestyle"), while still composing safely.
- Authoritative server simulation.
- **Time rewind** as a core gameplay mechanic (state must be rewindable; heavy state can be lazily rehydrated under a tunable budget).

---

## Core Concept

### Principles

1. **Schedule early, complete late**
   - Schedule work as soon as inputs are available.
   - Complete dependencies only at explicit barriers or just before a required main-thread read.

2. **Hybrid dependency model**
   - Prefer **data-driven dependencies** (component read/write contracts).
   - Use explicit **Step Barriers** when a global lock is needed (rewind, replication publish, structural playback).

3. **Contention-free parallelism by default**
   - Most jobs should be *embarrassingly parallel* (each entity writes only to itself).
   - Cross-entity effects use **Gather → Reduce → Apply** (no "everyone writes to one thing").

4. **Standard primitives, freestyle composition**
   - Features/mods can implement any logic, but must communicate using a small set of canonical job shapes and contracts (below).

5. **Rewind-compatible state model**
   - Simulation advances in discrete steps.
   - Snapshots exist only at **barrier boundaries**.
   - AI **memory and intent** are rewindable, but may be **rehydrated lazily** under a tunable compute budget.

---

## Step Barriers and Global Ordering

Simulation is organized around a small number of explicit global barriers. Systems may run in any order within a phase *as long as their data dependencies are correct*.

### Recommended phases (authoritative sim)

1. **Commands / Inputs**
   - Consume player/network commands into step-scoped events or components.
2. **Simulate**
   - Most parallel work: movement, AI evaluation, production, combat evaluation, sensors, etc.
3. **Resolve / Apply**
   - Apply aggregated results, perform buffered writes, and play back structural changes (ECB).
4. **Snapshot + Publish**
   - Capture snapshot for rewind.
   - Publish authoritative state deltas to clients (replication output).

### Barrier rules

- A barrier is the **only** place where the engine assumes "everything that must be consistent is complete."
- Mandatory completion at barriers:
  - Before ECB playback / structural change apply (end of Resolve / Apply).
  - Before snapshot capture (Snapshot).
  - Before publish replication output (Publish).

---

## Communication Contracts (to avoid "miscommunication" between systems)

All cross-system communication must use one of these two channels:

### Channel A — Durable State (Components)
Use for authoritative truth that must rewind:
- health, position, velocity, ownership, inventory, task state, faction stats, colony/village state, etc.

**Rules**
- Components are the "truth ledger." If something matters after rewind, it must be representable here (directly or via a rewindable log).

### Channel B — Transient Events (Step-Scoped Streams)
Use for "something happened this step":
- damage events, sightings, intent proposals, job completions, AI stimulus, sensor contacts, etc.

**Rules**
- Events are **append-only** during a step.
- Events are consumed in a defined phase and then cleared/compacted.
- Prefer *typed event streams* (distinct event structs) over overloaded generic events.
- If mods extend events, they must do so by adding new event types, not by reinterpreting existing semantics.

---

## Canonical Job Shapes (library-standard)

These are the recommended building blocks. Systems can freestyle internally, but should express work using these shapes so the scheduler and other systems can reason about them.

### 1) Map (pure parallel)
- Parallel per-entity / per-chunk.
- Writes only to the entity's own components.

**Use for**: movement integration, per-agent need decay, per-ship cooldowns, per-building production tick, etc.

### 2) Gather (parallel append)
- Parallel read-only iteration producing **events/requests**.
- Output via per-thread append (stream/list ParallelWriter), avoiding shared writes.

**Use for**: "AI proposals," "damage requests," "path requests," "sensor contacts," etc.

### 3) Reduce (combine partials)
- Combine per-thread partial results into stable aggregates.
- Prefer two-pass reduction (per-thread partials → combine) over atomics.

**Use for**: influence fields, heatmaps, totals, district/colony summaries.

### 4) Apply (single-writer / buffered writes)
- Apply reduced results to components.
- Structural changes and cross-entity writes happen here via buffered mechanisms (ECB, batched updates).

**Use for**: applying damage, spawning/despawning, changing ownership, committing chosen intents, etc.

### 5) Control / Barrier (glue + lock points)
- Small jobs or main-thread steps that:
  - build queries,
  - decide which jobs to schedule,
  - enforce barrier completion points.

### 6) Snapshot / Delta Capture
- Copy out rewindable authoritative state at barriers.
- Never capture caches that can be rebuilt (see Rewind section).

### 7) Cognition Hydrate (lazy rehydrate)
- Rebuild AI memory/intent for entities that need it now.
- Bounded by a tunable **rehydration budget** (see Rewind section).

---

## Parallel Patterns (recommended defaults)

### Pattern A — Embarrassingly parallel (default)
- Each entity updates itself only.
- No cross-writes, no shared containers.

### Pattern B — Spatial ownership tiling (for maps/fields)
- Partition world into tiles/sectors.
- Each job owns a tile: safe writes, no atomics.

### Pattern C — Gather → bucket → process
- Gather work items with a key (tile/sector/faction/target).
- Sort/group into buckets, then process buckets for locality and upgrade path to determinism.

### Pattern D — Two-pass aggregation (no contention)
- Pass 1: per-thread partial accumulation.
- Pass 2: combine.

### Atomics policy
- Atomics are an **escape hatch**, not a baseline.
- Allowed for:
  - minor counters/telemetry,
  - extremely low-contention cases proven safe.
- Disallowed as a general-purpose "merge strategy" for core simulation.

---

## Thread Safety Rules (default stance)

1. **Shared writes are disallowed by default**
   - If you need cross-writes, use Gather/Reduce/Apply or spatial ownership.

2. **Structural changes never happen inside parallel work**
   - Use a deferred apply mechanism (ECB-style) in the Apply phase.

3. **No per-step allocations in hot paths**
   - Prefer reuse, pooling, fixed-capacity buffers, and preallocated streams.

4. **Keep managed / non-Burst work out of the sim core**
   - Managed logic belongs in tooling, authoring, or presentation. The authoritative sim stays Burst-friendly.

5. **Avoid random-access lookups in tight loops**
   - Prefer chunk iteration and structured queries.
   - If you must index into arrays, batch work by key to improve locality.

---

## Job System Optimization Policy (performance-first)

### Scheduling heuristics (default)
- **Skip scheduling** if there is no work (empty query).
- Prefer **fewer fatter jobs** over many tiny jobs.
- Prefer chunk-based iteration for large queries.

### Granularity guidelines (advisory, tunable)
- If the workload is very small, a single-threaded or single-job path can be faster than parallel scheduling.
- The library may expose constants such as:
  - `MinWorkForParallel` (entity count or estimated operations),
  - `MaxBatchesPerSystemStep`,
  - `MaxEventsPerEntityPerStep` (for backpressure).

### Backpressure
- Event systems must have caps and drop/compact strategies.
- If a system produces more events than can be consumed in budget, it should:
  1) compact duplicates,
  2) degrade fidelity (coarser LOD),
  3) spill to the next step (if allowed by gameplay).

---

## Authoritative Server + Time Rewind (core mechanic)

### Step indexing
- The authoritative sim advances in discrete steps (`StepIndex`).
- Rewind targets are **barrier snapshots** ("restore to StepIndex K").

### State classification (what to snapshot vs rebuild)

1. **Authoritative rewindable state (snapshot at barriers)**
   - Components representing truth: transforms, health, inventories, ownership, tasks/goals (or their rewindable representation), colony/village/fleet state, etc.
   - RNG state used by authoritative outcomes (see below).

2. **Derivable caches (never snapshot)**
   - pathfinding caches, spatial query scratch, perception caches, broadphase scratch, temporary reductions, local job scratch buffers.
   - After restore: clear + rebuild lazily or on-demand.

3. **Presentation-only state (not authoritative)**
   - animation, particles, audio, client-only UI/FX.

### RNG policy (for "rewind feels real")
- RNG must be treated as rewindable state (directly or via deterministic streams).
- Restoring a snapshot restores RNG streams so repeating the same actions produces consistent outcomes (unless gameplay intentionally diverges).

### AI memory + intent rewind (lazy, budgeted)

**Locked decision**
- AI memory and intent **must rewind**, but can be **rehydrated lazily** with a tunable budget.

**Recommended representation**
- Use an **event-sourced cognition log** (append-only cognitive events), plus:
  - per-entity cognition cursor/checkpoint at each barrier snapshot,
  - compacted summaries for long-lived facts/relationships.

**Restore**
- On rewind: restore cognition cursors/checkpoints from snapshot; invalidate hydrated caches.

**Rehydrate**
- Entities re-entering the active set rehydrate their cognition by applying cognitive log deltas forward to current step.
- A global **rehydration budget** caps how much cognition work can run per step; remaining entities queue for later hydration.

**Degradation rule**
- If an entity is not hydrated yet, it runs in a simplified fallback mode until hydration completes (still consistent with authoritative state).

---

## Best Practices (Advisory)

### Do
- Prefer Map jobs for most work.
- Use Gather/Reduce/Apply for any cross-entity interaction.
- Batch work by spatial/semantic keys (sector, tile, faction, target).
- Keep authoritative truth in components, transient happenings in events.
- Complete dependencies only at explicit barriers.

### Avoid (common malpractices)
- Completing dependencies mid-system "just to be safe" (kills overlap and throughput).
- Using atomics as the default merge strategy.
- Deep AI evaluation for every entity every step (must be tiered/scheduled/LOD'd).
- Structural changes inside parallel jobs (must be deferred).
- Allocating containers every step in hot loops.

---

## Implementation Notes (non-normative)
- This is a policy document. Concrete helper APIs (event streams, snapshot buffers, cognition logs) may live in PureDOTS.
- Systems remain free to implement custom logic, but must respect:
  - barrier completion points,
  - communication contracts,
  - canonical job shapes.

---

**Last Updated**: 2025-12-20  
**Status**: Locked v1
