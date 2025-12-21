# BAR/Recoil Lessons (Lockstep Boundary + Batched Expensive Systems)

**Status**: Locked (architecture attitude + practical pipeline shapes)  
**Category**: Architecture / Determinism / Performance  
**Applies To**: Godgame, Space4X, shared PureDOTS  

BAR/Recoil’s “smooth mega-battles” are not magic. They come from:
1) a **hard deterministic boundary**, and  
2) making expensive work (**pathing, LOS, terrain deformation**) **batched, incremental, and multithreaded**.

This doc captures what to steal for Tri’s scope (PureDOTS-first, multi-domain ECS, rewind/time tooling, headless validation).

Related:
- `Docs/Architecture/Save_Load_Determinism.md` (Sim deterministic; presentation not; content manifest; checksums)
- `Docs/Architecture/Performance_Optimization_Patterns.md` (strict phase order; sensing ladder; smell field; budgeting)
- `Docs/Architecture/Scalability_Contract.md` (million+ guardrails; resident vs virtual; anti-patterns)
- `Docs/Architecture/Senses_And_Comms_Medium_First.md` (medium-first smell/sound; comms ride channels)
- `Docs/Architecture/AgentSyncBus_Specification.md` (deterministic replay during rewind)

---

## 1) What BAR/Recoil is demonstrably doing (facts worth copying)

### 1.1 Lockstep input-only networking (player-count scales by not sending state)
- Clients run the simulation; the network primarily transmits **player inputs** (“move X to Y”, “build Z”).
- Jitter is handled via an input buffer/queue (extra sim frames of latency to smooth variation).
- Dedicated “server” can act as a **relay**, not an authoritative simulator (scales hosting).

### 1.2 Fail-fast desync posture (no “mostly works”)
Lockstep demands identical calculations. BAR/Recoil culture is:
- treat mismatch as a critical error,
- crash/fail early rather than limp forward and normalize nondeterminism.

### 1.3 “Mega battle” scale comes from batching + incrementality
They keep heavy subsystems scalable by:
- batching path requests and solving them off-thread,
- updating navigation/terrain incrementally via dirty regions,
- running deformation/expensive updates at slower cadence with a guaranteed flush step.

### 1.4 Liquids are treated as boundary/medium (not full fluid sim)
Water queries exist, but a flat-plane “boundary model” is sufficient for huge battles.

---

## 2) PureDOTS decision: choose a multiplayer truth model (don’t half-mix)

For Tri, this is a *future decision*, but we must not build ourselves into a corner.

### Option A — Lockstep (inputs only)
Use when you want:
- perfect replays / deterministic verification,
- tiny bandwidth,
- very high player/spectator counts.

Non-negotiable:
- a deterministic sim kernel with a strict synced/unsynced boundary,
- recorded input stream + deterministic event ordering.

### Option B — Authoritative server + state replication
Use when:
- full deterministic lockstep is too costly across platforms/features,
- you need server authority for security/cheat reasons.

Still copy BAR’s internal win:
- hard sim/presentation boundary,
- batched and incremental expensive work.

**Recurring mistake to avoid:** hybridizing without discipline (“some state replicated, some lockstep”) creates debugging hell and hides nondeterminism until late.

---

## 3) Hard deterministic boundary (BAR’s synced/unsynced idea in DOTS terms)

PureDOTS already targets this:
- Simulation world: tick-based, deterministic (authoritative).
- Presentation: frame-based, derived (non-authoritative).

Lock it harder as a rule:
- **Sim decisions must not depend on camera/UI/audio/presentation state**.
- Any “player feel” heuristics (camera-distance LOD, UI-only smoothing) live outside Sim.

If we later implement multiplayer lockstep, the synced boundary becomes the network boundary.

---

## 4) Fail-fast desync detection (copy the attitude, tailored to Tri)

### 4.1 Session start gate
Before starting a sim session, verify:
- content manifest hash (already planned in save/load),
- registry/catalog hashes for key defs,
- critical policy tables hashes (tier profiles, budgets, rules).

Default policy: **block** on mismatch unless explicitly running an unsafe/dev mode.

### 4.2 Phase checksums (per system-group boundary)
Add an optional deterministic digest that can be enabled in headless/dev:
- compute a checksum at the end of key groups (e.g., Intake, Sensing, Interaction, Decision),
- compare against expected values in deterministic A/B tests,
- optionally compare across platforms (Windows editor vs Linux headless).

Fail-fast principle: do not tolerate “minor mismatch”; treat it as a correctness bug.

### 4.3 Crash-on-mismatch philosophy (dev builds)
BAR/Recoil’s “missing model crashes to avoid desync” maps here as:
- missing required defs/components for a synced module is a hard error in strict mode,
- but presentation-only assets can remain soft failures.

---

## 5) Expensive subsystems must be batched, deferred, incremental, multithreaded

This is the core transferable win even if we never ship multiplayer.

### 5.1 Pathfinding pipeline (single collection point, jobs, then apply)
Shape:
1) `PathRequestCollectSystem` — gather requests into SoA queues (no solving here).
2) `PathRequestBatchSystem` — dedupe/merge by region/quads; enable result reuse.
3) `PathSolveJobs` — worker threads solve batched requests; output handles/results.
4) `PathResultApplySystem` — a single deterministic point to apply results.

Why this shape matters:
- keeps the sim frame stable,
- unlocks multithreading without races,
- makes caching/reuse a first-class concept.

**Recurring mistake:** “each unit runs A* every tick”.

### 5.2 LOS / occlusion / sensor refinement as cached dirty regions
Use the same pattern as pathing:
- broadphase (cheap) → refine (budgeted) → cached results,
- invalidate/refine only in dirty regions (terrain changes, moving occluders, building placement).

This aligns with `Performance_Optimization_Patterns.md` sensing ladder and avoids global recompute.

### 5.3 Terrain deformation: sparse diffs + slow cadence + guaranteed flush
Contract:
- store deltas in tile/patch chunks (heightfield or voxel-ish layers),
- apply deformation at slower cadence (policy-controlled),
- include a final flush step so the “last change” isn’t missed.

Terrain diffs feed dirty regions for:
- nav/cost overlays,
- LOS/radar occlusion caches,
- medium flows (wind/vent pressure fields) where applicable.

**Recurring mistake:** recomputing nav/visibility globally for each crater/wall.

### 5.4 Liquids: boundary/medium first, full fluid sim later (LOD)
For Tri’s scope:
- treat water/liquid as a boundary/medium that affects movement/ballistics/sensing,
- avoid full fluid simulation everywhere.

If later needed (flooding/pressure/flow):
- run as simulation LOD (local high detail near interest; coarse elsewhere).

---

## 6) Headless sim as first-class tool (scale + verification)

BAR/Recoil supports headless + replay verification; PureDOTS should too.

For Tri:
- headless runners + telemetry are the default validation surface.
- determinism verification can reuse:
  - rewind tapes / event ledgers,
  - save/load digest checks,
  - optional phase checksums.

Deliverable rule:
- every new “expensive system” slice must include:
  - a deterministic proof line, and
  - telemetry showing bounded work (budgets, queue sizes, dirty region counts).

