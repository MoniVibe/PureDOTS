# Testing and Validation System (Rewind-First)

## Overview

**Status**: Designed — Locked (flexible via policies)  
**Category**: Architecture / Quality / Best Practices  
**Applies To**: PureDOTS (shared), with per-game schemas (Godgame, Space4x)  
**Primary Goal**: Support **player-facing rewind** at scale, while preventing regressions in deterministic simulation, save/load, and performance.

---

## Design Goals

1. **Rewind is gameplay**
   - Restoring to a previous point must be **correct**, **fast**, and **branchable** (rewind → different actions → new timeline).
2. **Truth-first correctness**
   - Only a finite set of state is considered *truth* and must restore exactly (or within defined quantization).
3. **Scales to "millions active"**
   - Headless scenarios must sustain **millions of simulated entities** with a **12 GB memory ceiling** as a hard gate.
4. **Flexible by policy**
   - The test harness is stable; behaviors are driven by pluggable policies (schemas, rewind storage strategy, digest lanes, invariants, perf gates).
5. **Shared taxonomy**
   - Scenarios and fixtures are shared across Godgame + Space4x to validate the common simulation foundation.

---

## Non-Goals

- Perfect bitwise determinism of *all* subsystems (cosmetic/procedural systems may vary within bounds).
- Rendering validation (tests run headless by default).
- A full "long replay" product feature (replay tooling can reuse the rewind tape later for beta).

---

## Key Definitions

- **Truth State**: state that must be restored by rewind and must pass golden-master regression.
- **Rebuildable State**: derived/cached/presentation state rebuilt after restore.
- **Checkpoint**: a stored rewind point (truth snapshot and/or journal state).
- **Lane**: a digest or validation category (core truth, structural, soft metrics, invariants).
- **Fixture**: data-driven scenario definition (world parameters, spawns, scripted actions, checkpoints, gates).

---

## Locked Decisions (from project scope)

### Rewind priority
- Rewind is a **core player mechanic** (not just a debug tool).

### Truth definition (default "Core Truth")
Truth components include:
- **Needs**
- **Combat stats**
- **Position + state** (movement/AI state)
- **Resource counts** (inventories/stockpiles/global pools)

### Regression style
- **Golden master** is the default for truth lanes.
- Some subsystems (e.g., **vegetation**) are validated by **bounds / invariants** and optional **delta correctness**, not strict equality.

### Test execution
- **Headless world** runner is the default.
- **Data-driven fixtures** are the default.
- **Scenario benchmarks** are the default (micro-bench optional later).
- **Shared taxonomy** across both games.

---

## Core Concept: Rewind-First Truth Validation

The system validates that:
1. Running a scenario forward produces expected truth state at checkpoints.
2. Rewinding to a checkpoint restores truth state.
3. Rebuilding non-truth state after restore converges to a valid world.
4. Performance/memory/churn stay within gates.

**Truth correctness** is proven via:
- Golden master digests (multi-lane)
- Invariant suites
- Save/Load parity checks
- Rewind + branch tests

---

## Architecture

### 1) Stable Kernel (minimal surface area)

The kernel never depends on game-specific systems. It only orchestrates:

- **ScenarioRunner**
  - Loads fixtures
  - Creates headless world
  - Injects determinism primitives
  - Steps simulation
  - Executes checkpoints
  - Runs validation lanes and gates
  - Emits failure diagnostics

- **Determinism Primitives**
  - Single sources of truth for:
    - `Time` (simulation step / tick source)
    - `Random` (deterministic RNG stream routing)
  - System code must not read "ambient" time or use ad-hoc RNG.

- **Rebuild Pipeline**
  - A deterministic-ish post-restore pipeline that rebuilds caches/indices/presentation proxies.
  - Must be invoked after every restore (and can be validated independently).

### 2) Policy Modules (swap per scenario/system)

Policies can be selected per fixture, per game profile, or per system group.

- **TruthSchemaPolicy**
  - Declares truth groups and component membership.
  - Supports per-game schema overlays (Godgame vs Space4x) without changing runner.
  - Recommended mechanism: **marker interface/attribute + grouping tags**
    - e.g., `ITruthComponent` + `TruthGroup` enum.
    - Allows explicit lists in tests for maximum control.

- **RewindPolicy**
  - Unified API:
    - `CaptureCheckpoint(id)`
    - `RestoreCheckpoint(id)`
    - `BranchFromCheckpoint(id)`
    - `TrimToBudget()`
  - Backends (selectable):
    1. **SnapshotTruthBackend** (baseline, simplest)
    2. **DeltaJournalBackend** (optional, sparse changes)
    3. **HybridBackend** (default target; snapshot + deltas for hot groups)

- **DigestPolicy (Hash Lanes)**
  - Multiple digests per checkpoint:
    - **CoreDigest** (truth only, must match)
    - **StructuralDigest** (counts/buckets, must match)
    - **SoftDigest** (warn-only by default)
  - Supports per-fixture strictness overrides.

- **InvariantSuite**
  - Property checks that must hold regardless of nondeterminism:
    - ranges, monotonic rules, conservation rules, "no impossible state".

- **VegetationPolicy**
  - Validates vegetation "within bounds" and optionally by delta events (burn/chop/plant).
  - Vegetation is not part of CoreDigest by default.

- **PerfGatePolicy**
  - Gates:
    - **Hard**: memory ceiling (<= 12 GB)
    - **Hard/Soft**: structural churn thresholds (archetype changes per step)
    - **Soft**: relative regressions (scenario cost vs baseline)

---

## Execution Order (ScenarioRunner)

1. **Load Fixture**
2. **Create Headless World**
3. **Apply TruthSchemaPolicy + Game Profile Overlay**
4. **Initialize Determinism Primitives**
5. **Spawn world from fixture**
6. **Warm-up step(s)** (optional; fixture-controlled)
7. **Step Loop**
   - Run simulation phases in deterministic order
   - Apply fixture action script events
   - At checkpoint steps:
     - Capture checkpoint (rewind buffer)
     - Compute digest lanes
     - Run invariants
     - Record metrics
8. **Rewind Validation Pass**
   - Restore selected checkpoints
   - Run rebuild pipeline
   - Recompute digests + invariants
   - Run branch test (restore → diverging actions → validate)
9. **Emit Report**
   - Lane diffs, first divergence index (optional), perf/memory/churn metrics.

---

## Truth Model

### Truth groups (default)
- **Identity/Existence**
  - stable entity identity, enabled/alive state, ownership/faction.
- **Transform & Motion**
  - position/rotation/velocity + movement state.
- **Needs**
  - hunger/faith/morale/etc. (game-defined).
- **Combat**
  - health, armor, cooldowns, status effects, weapon state.
- **Resources**
  - inventories, stockpiles, global resource pools, production counters.
- **Decision/State**
  - AI state machine/task pointers (only what is needed to restore behavior).

### Truth vs rebuildable guidance
Truth should include anything the player expects to be consistent after rewind:
- agent location/state, combat outcomes, resource deltas, commitments (task chosen).

Rebuildable should include anything that can be recomputed:
- caches (path, influence map cache), spatial indices, presentation proxies, debug state.

### Float & buffer stability (chosen defaults)
To keep equality flexible but strict where it matters:

- **Quantize for hashing**:
  - position/velocity are hashed using a quantized representation (configurable resolution).
- **Exact for discrete**:
  - ints/enums/resource counts hash exactly.
- **Stable ordering**:
  - dynamic buffers included in truth must define canonical ordering for hashing/serialization.

---

## Rewind System Strategy

### Default target: Hybrid ring buffer
A ring buffer of checkpoints, trimmed to budget.

- **Checkpoint contains**:
  - truth snapshot for selected groups
  - optional delta journal since last snapshot for hot groups
  - structural bits (enabled/alive flags) when relevant
  - RNG/time state at checkpoint

### Backend selection rules (policy defaults)
- Start with **SnapshotTruthBackend** for correctness.
- Promote hot truth groups to **HybridBackend** when memory pressure is detected:
  - Transform/Motion is the first candidate.
- Use **DeltaJournalBackend** only when changes are sparse and journaling is compact.

### Entity lifecycle recommendation (performance & rewind-friendly)
Prefer **pool + enable/disable** over frequent destroy/create for rewindable populations.
- This reduces structural churn and reduces snapshot complexity.
- True destroy/create remains allowed, but is tracked in StructuralDigest and churn gates.

---

## Golden Master Regression

### Lanes
- **CoreDigest**: must match
- **StructuralDigest**: must match
- **SoftDigest**: warn by default (can be strict per fixture)
- **InvariantSuite**: must pass

### Baseline generation and update
- Each fixture can generate a baseline package:
  - expected digests per checkpoint
  - expected invariant ranges
  - optional expected metrics bands
- Updating baselines is an explicit action; never auto-update on failure.

### Failure diagnostics (no "needle in a haystack")
- Optional **first divergence search**:
  - restore checkpoint K-1
  - re-sim forward with extra lane logging until mismatch
  - report first step where CoreDigest diverges.

---

## Vegetation (and other "bounded" systems)

### Policy: "Procedural baseline + delta truth + bounds"
- Vegetation baseline is procedural/derived (not core truth).
- Player-affecting changes are deltas:
  - burn/chop/plant/blight events
- Validation:
  - bounds per biome/cell/region (density ranges, no impossible states)
  - delta correctness after rewind (the same deltas reapply consistently)

This keeps immersion (forests you burned stay burned) without hashing the entire vegetation field.

---

## Fixtures (Data-Driven) + Shared Taxonomy

### Fixture schema (recommended fields)
- `id`, `tags` (shared taxonomy + game tags)
- `seed` (world + RNG)
- `world` (map parameters, region layout, density targets)
- `spawns` (groups with counts/distributions)
- `actions` (scripted commands/events to drive interactions)
- `checkpoints` (steps and required lanes)
- `truth_schema` (which schema overlay to apply)
- `gates` (memory ceiling, churn limits, regression thresholds)
- `invariants` (suite selection + thresholds)

### Shared taxonomy
- **Micro**: Time/Random/SaveLoad primitives
- **Meso**: interaction pairs (Movement↔Pathing, AI↔Production, SaveLoad↔AI)
- **Macro**: scale stress (millions, memory, churn, worst-case interactions)

Both games add tags on top (e.g., `godgame:belief`, `space4x:fleet`), but share the same runner and lane logic.

---

## Performance Benchmarking (Scenario-Based)

### Captured metrics
- step cost (overall)
- optional phase costs (grouped, if available)
- checkpoint capture cost
- restore + rebuild cost
- peak memory
- structural churn rate

### Gates (locked defaults)
- **Hard**: peak memory <= **12 GB**
- **Hard**: churn under scenario-specific limit (prevents archetype-churn explosions)
- **Soft**: no large regressions vs baseline (until per-phase budgets are set)

---

## Save/Load Parity

Save/Load must be consistent with rewind:
- `Load(Save(CheckpointTruth))` produces the same CoreDigest as the checkpoint.
- Save files store truth; rebuild pipeline restores rebuildables.

This supports both rewind gameplay and stable testing.

---

## Implementation Sequence (order, not time)

1. **Determinism Primitives**
   - single Time + Random sources, enforced access patterns.
2. **Headless World Runner (ScenarioRunner)**
   - step loop + checkpoints + reporting.
3. **Truth Schema & Serialization**
   - schema policy + truth snapshots for core groups.
4. **Digest Lanes + Invariants**
   - core/structural/soft lanes + invariant suites.
5. **SnapshotTruthBackend**
   - ring buffer + restore + rebuild pipeline integration.
6. **Fixture System**
   - data format + loader + action scripting.
7. **Save/Load Parity Tests**
   - checkpoint save/load equivalence.
8. **Hybrid Backend (optional)**
   - add deltas for hot groups when memory pressure requires.
9. **Macro Scenarios**
   - million-scale + memory/churn gates.

---

## Outstanding Items (decided now for project fit)

These defaults are chosen to match "great games" + large-scale rewind without locking you into one approach:

1. **Framework**: Unity Test Framework for orchestration + custom headless harness inside PureDOTS.
2. **Truth schema mechanism**: marker-based (`ITruthComponent` + `TruthGroup`) with per-game overlays.
3. **Hash equality strategy**: quantized floats for transforms/velocities; exact for discrete/resource counts.
4. **Checkpoint strategy**: fixture-defined checkpoints + on-demand forced checkpoints when entering rewind UI/mechanic.
5. **Backend default**: SnapshotTruthBackend first; HybridBackend is the planned upgrade path.
6. **Vegetation**: bounds + delta events (not core truth).
7. **Performance gates**: memory hard ceiling (12 GB) + churn hard gate + relative regression soft gate.

---

**Last Updated**: 2025-12-20  
**Status**: Designed — Locked (flexible via policies)
