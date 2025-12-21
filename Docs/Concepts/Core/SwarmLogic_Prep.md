# Swarm Logic Prep (Shared)

Purpose: codify a *field-driven swarm tactics* architecture that scales (many drones), stays deterministic/rewind-safe, and remains reusable across Space4X + Godgame.

This is intentionally **module-by-module**:
- Entities are blank by default.
- Adding a swarm module adds the ability to participate as a **SwarmController** (meso) or as a **SwarmDrone** (micro).
- Nothing in swarm core assumes “spaceships” or “villagers” (those are adapters/scenarios/presentation).

## Core Principle: One Source of Truth
**SwarmController picks the tactic and publishes fields; drones never invent global intent.**

- Controllers own “what are we doing?”.
- Drones own “how do I move locally without colliding?”.
- Systems enforce *single-writer* behavior: one system writes each output (intent/velocity/transform) per tick.

This mirrors how we treat authority/delegates elsewhere:
- A swarm controller is effectively a **delegate** that can be attached to any authority chain (captain → flight ops officer → drone wing controller, mayor → militia captain → insect swarm, etc.).

## Conceptual Stack (3 layers)
1) **Tactical analysis (global-ish)**  
Chooses which verb to run and how much swarm budget to allocate. Uses coarse fields/influence summaries rather than per-target scans.

2) **SwarmController (meso)**  
Converts a “verb” into:
- an implicit surface to occupy (`S(x)=0`),
- desired density (`ρ(x)`),
- tangential flow (`F(x)`),
- and constraints (keep-out, standoff, no-fly volumes).

3) **Drone local control (micro)**  
Computes local steering from controller fields + local neighbors + local avoidance. No global pathing, no slot assignment.

## Tactic Verbs (Controller-Level Goals)
Verbs are the only global “intent” API. They are data-driven (BlobAsset/IDs), not hardcoded game enums.

Suggested initial verb set:
- `Screen(anchor, normal, width, height, depth, densityProfile)`
- `Bubble(anchor, radius, thickness, densityProfile, rotationRate, biasToThreat)`
- `Encircle(target, radius, thickness, angularFlow, gapPolicy)`
- `SwarmAttack(target, cohesion, aggression, riskBudget)`
- `Intercept(inbound, standoff, lateralSpread)`
- `SacrificeBlock(threatRay, untilImpactOrTick, riskBudget)`

Each verb publishes:
- **Surface** `S(x)` (plane/sphere/torus/ring/etc.)
- **Thickness** (allowed band around `S(x)=0`)
- **Density** target `ρ(x)` (can bias toward threat gradients)
- **Flow field** `F(x)` tangential on/near the surface (orbit/scan/swirl)
- **Constraints**: keep-out volumes, minimum standoff, friendly no-cross zones

## Field Representation (What drones sample)
We treat “swarm tactic” outputs as *queryable fields*, not as “drone A checks drone B” logic.

Controller publishes a compact, evaluatable description per swarm:
- `SwarmSurface`: type + parameters (plane/sphere/ring/etc.)
- `SwarmDensityPolicy`: target density + bias rules
- `SwarmFlowPolicy`: tangential velocity/rotation rules
- `SwarmConstraintSet`: keep-out/standoff constraints (volumes, spheres, planes)

The drone samples these each tick:
- signed distance `d = S(x)`  
- surface normal `n = ∇S(x)` (or analytic normal)
- desired flow `F(x)` (already tangent to surface)
- density goal `ρ(x)` (optionally biased by threat direction)

Important: fields can be evaluated analytically (cheap) without storing per-cell arrays.
When we *do* need cached grids (influence/threat), they live in environment caches (see `Simulation_LOD_And_Environment_Fields.md`) and are sampled like any other field.

## Drone Local Control (Steering Composition)
Each drone computes a desired velocity/acceleration as a weighted blend of behaviors.

### A) Stay on formation surface
Radial correction (spring/damper toward the surface band):
- If `|d| > thickness/2`: push toward the surface along `-sign(d)*n`.
- If inside band: allow tangential motion to dominate.

### B) Maintain density / fill holes (no slot assignment)
Two practical game-safe tools:
- **Neighbor repulsion** projected into the surface tangent plane (prevents clumps, preserves shape).
- **Mild cohesion** (keeps formation from shattering when drones die).

Optional (later) “coverage” improvement:
- approximate hole-filling by steering toward low-density directions estimated from neighbor distribution and/or a density field gradient.

### C) Offensive swarm cohesion (boids-style)
For attack verbs, add classic local rules:
- separation, alignment, cohesion, plus target attraction
- optionally tangential orbit to “encircle then collapse”

### D) Task steering primitives
Blend in:
- `seek/arrive`
- `pursuit/intercept`
- `avoid/hazard`
Driven by verb parameters + local perception.

### E) Risk budget (sacrifice as a parameter)
“Expendable drones” is not a special-case behavior:
- `riskBudget` lowers avoidance weights and raises intercept/hold weights.
- Drones follow the controller policy; they don’t individually choose to suicide.

## Collision Avoidance (Dense swarms without jitter)
Separation-only breaks at density. We need a stable local avoidance layer.

Recommended approach:
- Use **RVO/ORCA-style velocity obstacle avoidance** in the drone’s local motion plane.
  - For `Screen`: plane is already defined.
  - For `Bubble/Encircle`: use the local tangent plane at `x` (project neighbor velocities and desired velocity).
  - For true 3D free-space swarms: extend to 3D VO, but keep it LOD-gated.

## DOTS Performance & Determinism Rules
This must survive “millions of drones” as a design target (with sim LOD).

### Neighbor queries
Never do O(n²). Always:
- spatial hash / uniform grid of drone positions (e.g., `NativeParallelMultiHashMap<int, Entity>` keyed by cell),
- scan only adjacent cells (fixed bound),
- keep neighbor budgets (cap processed neighbors per drone, deterministic ordering).

### Two-tier simulation (LOD)
- Tier A (interest volume / active combat): full steering + local avoidance.
- Tier B (mid): surface correction + flow + cheap separation (no ORCA).
- Tier C (far): controller-only representation (aggregate swarm state), materialize drones only when promoted.

### Single writer (non-negotiable)
- Controllers write *swarm state/fields*.
- Drones write `MoveIntent` / `DesiredVelocity`.
- Locomotion/movement applies those intents (see `Medium_Driven_Locomotion_Tiers.md`).

### Rewind-safe / deterministic
- Run swarm sim only in `RewindMode.Record` (and in fixed-step groups where appropriate).
- Tie-break any selection deterministically (`Entity.Index`, stable hashes).
- Keep “randomness” seeded from stable IDs + tick, never `UnityEngine.Random`.

## Existing Runtime Hooks (What we can reuse today)
- `PureDOTS.Runtime.Swarms`: `SwarmBehavior`, `SwarmMode`, `DroneOrbit`, `SwarmThrustState` (+ aggregation + movement systems).
- `PureDOTS.Runtime.Groups`: `GroupTag`, `GroupMember`, `GroupObjective`, `GroupMetrics` for membership + targeting.

Planned alignment:
- **Membership**: `GroupMember` buffer is the canonical membership list.
- **Anchor**: reuse `DroneOrbit.Anchor` and/or a `SwarmAnchor` component for “protect this”.
- **Tactics**: expand `SwarmMode` into data-driven verbs (keep enum only as a legacy adapter if needed).
- **Telemetry**: cohesion/dispersion, coverage effectiveness, avoidance workload, controller verb state.

## First Milestone Proof (the “it works” slice)
AI-controlled `Bubble(anchor)` that proves:
1) forms from random spawn positions,  
2) maintains density while drones die,  
3) biases toward an incoming threat direction (via an influence/threat field),  
4) stays stable under dense motion (ORCA/RVO on tangent planes).

## Recurring Errors (stop these early)
- Slot-based formations for swarms (thrashes when drones die).
- O(n²) neighbor scans (dies immediately at scale).
- Separation-only avoidance (jitter + clumping at density).
- Multiple sources of truth (controller also nudging transforms, drones hard-setting pivots, etc.).
- Letting presentation constraints leak into sim (camera distance changes tactics).
