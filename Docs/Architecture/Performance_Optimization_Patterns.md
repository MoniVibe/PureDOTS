# Performance Optimization Patterns (PureDOTS)

**Status**: Locked (flexible via TierProfiles and policies)  
**Category**: Architecture / Performance / Best Practices  
**Related Systems**: All Systems (Simulation + Presentation bridges)

---

## Goals

- Scale from small → huge worlds (millions+ entities) without architectural rewrites.
- Preserve immersion by spending compute where the player can perceive it.
- Support **Godgame** (2.5D terrain + airborne/hover/glide with 3D obstacle avoidance) and **Space4x** (3D 6DOF) through a **shared abstraction**.
- Keep behavior stable and debuggable via **strict phase order**, while parallelizing work inside phases.

## Non-Goals

- Committing to fixed numerical budgets (we keep policy surfaces and ship tier presets).
- Making fidelity settings change authoritative outcomes (tiers are client-side).

---

## Core Contract: Core State vs Detail State (Tier-Safe)

**Core State (tier-invariant, authoritative):**
- Economy/accounting, ownership, resource production/consumption
- Strategic outcomes (war/peace decisions, colonization decisions, major migrations)
- Combat outcomes for authoritative engagements (damage/HP/state transitions)
- Progression/unlocks, save/load invariants

**Detail State (tier-dependent, derived/presentation):**
- Extra perception/LOS refinement beyond what core needs
- Micro-decisions that do not affect macro outcomes
- Path refinement for non-critical entities (steering vs exact paths)
- Smell field resolution/sampling fidelity
- Proxy entities / visuals (crowds, fleets, barks, VFX), debug overlays

> **Rule:** If a system influences Core State, it must not depend on tier-only computations.

---

## Pattern 1: Client Fidelity via Pre-Authored TierProfiles

Players choose between pre-authored tiers; there is no free-form slider in release builds.

Recommended presets:
- `Laptop` (small-medium worlds)
- `Mid` (medium worlds)
- `High` (large worlds)
- `Cinematic` (maximum detail)
- `Debug` (extra instrumentation)

### TierProfile contents (policy surfaces; no fixed numbers required now)

**A) Interest / LOD policy**
- Promotion triggers: selected / in-camera / in-combat / story-flag / interest-score
- Demotion hysteresis (stickiness to prevent thrash)
- Tier weights (how compute is shared across tiers)

**B) Query quality policy**
- Radius neighbors: exact vs approximate vs aggregate proxy per tier
- FOV filtering: enabled/disabled per tier
- LOS: none / prioritized / full per tier
- Refinement policy: which candidates are eligible for expensive filters

**C) Path fidelity policy (ladder)**
- Allowed modes per tier: `Steering → CoarseWaypoints → Hierarchical → Exact`
- Replan aggressiveness and cache preference per tier

**D) Smell policy**
- Field dimensionality: 2D / height-bands / 3D
- Resolution, diffusion/decay, channels (blood/food/fire/etc.)
- Sampling cadence per tier

**E) Soft-cap policy (overload behavior)**
- Degrade order (locked for these projects):
  1) **Demote distant → aggregate**
  2) **Reduce pathfinding fidelity**
  3) **Then throttle/approx the rest** (LOS/perception/depth)

---

## Pattern 2: Strict Phase Order (Parallel Within Phase)

Maintain a strict phase pipeline for stability and debugging. Inside a phase, jobs are parallel and Burst-friendly.

### Canonical phase order (shared by both games)

1) **Intake**
   - Apply player commands and external events to queues.

2) **Kinematics**
   - Integrate movement for agents/ships/projectiles.
   - Resolve cheap continuous steering (pre-avoidance).

3) **Spatial Build**
   - Refresh spatial partitions (dynamic entities + obstacles).

4) **Sensing**
   - Broadphase neighbors (radius)
   - Cheap filters (distance², masks) → FOV filter
   - LOS refinement (budgeted + prioritized)

5) **Interaction**
   - Combat resolution, hits/effects, state transitions
   - Avoidance corrections (includes airborne 3D obstacle avoidance)

6) **Planning**
   - Collect expensive requests (paths, advanced scans) into queues
   - Solve in batches at tier-selected fidelity

7) **Decision**
   - AI decisions scheduled by initiative/patience + tier
   - Emit intents/events (do not structurally mutate here)

8) **Structural Apply**
   - The only place allowed to spawn/despawn/attach/detach/archetype-change
   - Cleanup transient tags and finalize frame state

> **Rule:** Do not allow structural changes inside hot loops. Funnel them to **Structural Apply**.

---

## Pattern 3: Unified 3D Spatial Abstraction (2.5D + 6DOF)

Use a unified 3D coordinate/query surface. World-specific constraints live in movement/behavior policies.

### Baseline partition (default): 3D Hashed Uniform Grid
- Fast to update and query for radius-neighbor broadphase.
- Works for Godgame (thin z-band for grounded agents + airborne) and Space4x (full 3D).

### Separate partitions
- **Dynamic Entities Grid**: for neighbor queries
- **Obstacle Grid/Volumes**: for avoidance + LOS occluders (static or slow-moving)

### Tunables (TierProfile or world profile)
- cell size and multi-resolution levels
- rebuild policy: full rebuild / incremental / thresholded by movement
- per-layer masks (ship vs agent vs projectile vs etc.)

---

## Pattern 4: Query Pipeline (Radius → FOV → LOS)

You expect hundreds+ candidates. The pipeline keeps this survivable.

### Query primitives (conceptual API)
- `NeighborsRadius(center, radius, mask) -> Candidates`
- `FilterCone(candidates, forward, fov) -> Candidates`
- `HasLOS(from, to, occluderMask) -> bool/visibilityScore`
- `SampleSmell(position, channelMask) -> value/gradient`

### Filter order (locked)
1) Distance² / radius reject
2) Type/faction/tag masks
3) FOV cone (dot test)
4) LOS refinement (budgeted/prioritized)

### LOS policy (no hard caps, but bounded work)
LOS is only computed for a prioritized subset (e.g., closest/threat/recency/story/focus).
- If LOS budget is exhausted, return heuristic visibility for non-focus tiers:
  - "likely visible" based on distance, last-known visibility, occluder density class, etc.
  - Patience/debt ensures important entities get refined soon.

---

## Pattern 5: Smell as a Field (Immersion Cheaply)

Smells linger; implement this as an environmental field, not per-entity particle scans.

### Smell Field
- Event deposits into grid cells (blood/food/fire/etc.)
- Field diffuses + decays each update
- Agents sample locally and optionally follow gradients

### Flexibility
- Godgame default: **height-bands** (2.5D + airborne compatible)
- Space4x default: **3D** (optional; can downshift to bands for perf)

---

## Pattern 6: Airborne 3D Obstacle Avoidance (Local-First)

Airborne agents must avoid 3D obstacles.

### Layered approach (tier-aware)
1) **Local steering** (always-on, cheap): obstacle repulsion + separation
2) **Reactive avoidance** (near obstacles): sample a few directions; choose least-colliding
3) **3D path planning** (only when necessary): rare, requested via batch queue

> Overload policy reduces path fidelity before touching movement responsiveness.

---

## Pattern 7: Soft Caps via Budget Broker + Initiative + Patience

You prefer soft caps. Soft caps still need a circuit breaker to avoid backlog death spirals.

### Budget Broker
- Subsystems request work credits: LOS rays, scan refinements, path solves, decision updates.
- Broker allocates credits by:
  - Tier weights (focus tiers receive the lion's share)
  - Entity importance (combat/story/selected)
  - Patience debt (waiting increases priority)

### Initiative + Patience
- **Initiative**: desired act frequency (scheduling weight)
- **Patience**: tolerance for deferral
- When work is deferred, entities accumulate debt; debt interacts with patience and can:
  - promote entity (if important)
  - demote entity (if distant/low importance)
  - downgrade requested fidelity (path/LOS/decision depth)

### Degrade order (locked)
1) Demote distant → aggregate
2) Reduce pathfinding fidelity
3) Then throttle/approx the rest

---

## Pattern 8: Batching (What It Means Here)

Batching = processing many similar operations with good locality and minimal overhead.

### Batch types (recommended)
- **Archetype batching**: run over entities sharing component sets (movement, projectiles)
- **Spatial batching**: per cell/sector processing (neighbors, local interactions)
- **Request batching**: queues for expensive work (pathfinding, LOS refinement)

> This supports "few big jobs" per phase while keeping strict order.

---

## Pattern 9: Memory Pooling (Do This From Day One)

Even early development benefits from stable allocation patterns.

### Pool these categories
- Per-cell entity lists (persistent)
- Work queues: LOS refinement, scan refinements, path requests
- Scratch buffers: per-phase rewindable allocators
- Path result buffers (reused)
- Event rings (bounded storage with overwrite policy for low-importance detail)

### Rules
- No managed allocations in hot paths.
- Prefer pre-sized Native containers + reuse.
- Use explicit lifetimes: **phase-local**, **frame-local**, **persistent**.

---

## Pattern 10: Job Scheduling (Few Big Jobs, Hybrid via LOD)

### Strategy
- Big Burst jobs per phase (IJobChunk/IJobEntity style passes)
- Avoid mid-frame sync points; only sync at phase boundaries when needed
- Keep dependency graphs shallow

### Where hybrid matters
- High-fidelity tiers can run more detailed jobs (LOS refinement, exact path)
- Low-fidelity tiers run cheaper jobs or are aggregated

---

## Pattern 11: Profiling & Instrumentation (Always-On Metrics)

Do not wait for problems; measure continuously.

### Minimal metrics to collect
- Entity counts per tier (individual vs aggregate)
- Neighbor candidate histograms (broadphase size) per tier
- LOS rays attempted/fulfilled/deferred
- Path requests submitted/solved/deferred + chosen fidelity modes
- Budget broker backlog + average patience debt
- Phase timings (Intake/Kinematics/Spatial/Sensing/Interaction/Planning/Decision/Structural)

### Debug overlays
- "Why am I aggregated?" (tier assignment reason)
- "Why did LOS defer?" (budget + priority)
- "Why did path downgrade?" (overload policy step)

---

## Defaults (Tailored to These Projects, Still Flexible)

Locked defaults chosen now (can be overridden by TierProfiles later):
- **Unified 3D spatial abstraction** for both games.
- **3D hashed grid** as baseline partition for dynamic entities.
- Separate obstacle representation for avoidance + LOS occluders.
- **Radius neighbors as broadphase**, then FOV, then budgeted LOS refinement.
- **Smell as a field** (height-bands by default; 3D available).
- **Overload degrade order**: demote distant → reduce path fidelity → then throttle/approx the rest.
- Allow heuristic LOS for non-focus tiers when budgets tighten (keeps immersion stable without stalling).

---

## Implementation Checklist (PureDOTS)

- [ ] `TierProfile` asset schema (policies above)
- [ ] `FidelityTier` assignment system (interest scoring + hysteresis)
- [ ] `SpatialGrid3D` (dynamic entities) + `ObstacleGrid` (volumes/shapes)
- [ ] Query API layer (radius/FOV/LOS hooks + masks)
- [ ] Budget Broker (work credits + backlog/debt)
- [ ] Initiative/Patience scheduler integration
- [ ] Path request queue + path fidelity ladder
- [ ] Smell Field module (deposit/diffuse/decay/sample)
- [ ] Phase pipeline skeleton + strict "structural apply" gate
- [ ] Instrumentation counters + debug overlays

---

**Last Updated**: 2025-12-20  
**Status**: Locked (flexible via TierProfiles and policies)
