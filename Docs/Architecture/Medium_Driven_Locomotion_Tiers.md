# Medium-driven Locomotion (Kinematic-first + Physics Tiers)

**Status**: Draft (authoritative direction)  
**Category**: Architecture / Movement / Simulation  
**Applies To**: Godgame, Space4X, shared PureDOTS

---

## Purpose

Unify locomotion across environments (normal gravity, underwater, zero‑G) by making **Medium** first-class and queryable, and by using two execution tiers:
- **Tier K (kinematic-by-default)**: cheap, scalable integration for most agents.
- **Tier P (physics/impulse)**: full rigidbody/impulse only for “active/boarding/close‑up” agents.

This doc complements:
- `Senses_And_Comms_Medium_First.md` (signals through a medium),
- `Scalability_Contract.md` (resident vs virtual + sim LOD),
- `Save_Load_Determinism.md` (rewind/determinism constraints).

---

## 1) Medium is first-class (query, don’t flag)

Create an environment cache (grid or compartment graph) that answers, per position:
- `GravityVector`
- `MediumType`: `Vacuum` / `Gas` / `Liquid` / `SolidContact`
- `FluidDensity`
- `FlowVelocity` (wind/current)
- optional: `Viscosity`, `Pressure`, `Depth`

Mapping examples:
- **Space4X**: outside hull = `Vacuum`, pressurized compartments = `Gas`, flooded compartments = `Liquid`.
- **Godgame**: world regions = `Gas`, water volumes = `Liquid`.

**Recurring error to avoid**: encoding “underwater” or “zero‑G” as per-agent flags. It must come from an environment query so it stays coherent, editable, and streamable.

---

## 2) Locomotion pipeline: intent → actuation → application

Split movement into a deterministic pipeline:

1) `MoveIntent`  
   Desired direction/speed/pose (from AI or player).

2) `LocomotionMode` resolution  
   Derived from `Medium` + anatomy/capabilities:
   - `Grounded`, `Swimming`, `FreeFloat`, `Climbing`, `Jetting`, `Grappling`

3) `ActuationCommands`  
   Produced per tick:
   - continuous forces (swim strokes, thrusters)
   - instantaneous impulses (push-off, kick, recoil transfer)

4) Apply impulses/forces **in one place**, deterministically (per sim tier).

---

## 3) Zero‑G primitives (no fake walking)

In microgravity, movement must come from external impulse or reaction mass:

### A) Push-off / shove (surface reaction)
- Acquire contact point + normal (reach query).
- Choose desired impulse direction (typically opposite desired travel direction).
- Apply equal-and-opposite impulse:
  - to the agent body
  - to contacted rigidbody if dynamic (or ignore if static/infinite mass)

Tier P: use Unity Physics impulse application to get correct linear + angular velocity when pushing off-center.  
Tier K: approximate as a deterministic velocity change (with optional angular approximation).

### B) Free-float jetting (reaction mass / thrusters)
- Continuous force/impulse along body axes.
- Drain propellant/energy.

**Recurring error to avoid**: “gravity=0 but still using grounded locomotion.” Zero‑G must be impulse/thruster-driven.

---

## 4) Underwater primitives (medium-scaled)

Minimum viable underwater that still feels “real”:

### A) Buoyancy
Upward buoyant force proportional to displaced fluid:
- approximate displaced volume with a per-entity scalar unless partial submersion matters.

### B) Drag
Compute relative velocity:
- `v_rel = v_body - v_flow`
Apply drag opposite `v_rel`; optionally anisotropic coefficients for “fish vs brick”.

### C) Propulsion (swim strokes)
Treat strokes as force over short duration, limited by:
- limb condition/strength
- drag regime
- skill/profile modifiers

**Recurring error to avoid**: per‑limb hydrodynamics for everyone. Keep it “actuator strength → thrust,” let drag/buoyancy do the realism.

---

## 5) Contact-powered propulsion (unifies zero‑G + underwater)

Unify “push with limbs” as a generic `ActuatorContact` mechanic:
- `ContactPoint`, `ContactNormal`, `ContactEntity`
- `GripScalar` (friction/adhesion/claws/magboots)
- `MaxImpulse` or `MaxForce`

Then:
- `FreeFloat`: impulses dominate
- `Swimming`: impulses are “kicks off walls,” otherwise thrust dominates

---

## 6) Scaling strategy (simulation tiers)

You cannot afford full rigidbodies + contact queries for millions.

Use sim tiers:
- **Tier P (active/boarding/near camera)**: dynamic rigidbody + impulses + detailed reach/contact.
- **Tier K (default/mid)**: kinematic integration + simplified collision (capsule sweeps); approximate push-off as velocity change.
- **Tier A (far/aggregate)**: no per-entity physics; formation/crowd motion + occasional corrections.

**Recurring error to avoid**: “everyone is a rigidbody because it’s cool.”

---

## 7) Editor/runtime manipulation (why medium-first matters)

Because Medium is a field and locomotion is intent→actuation:
- the editor can paint gravity volumes, flooding, currents, pressurization,
- agents react by mode switching with no bespoke code paths.

Edits should be commands/patches that update the environment cache; locomotion systems just re-sample.

