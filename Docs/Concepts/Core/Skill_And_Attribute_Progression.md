# Skill and Attribute Progression System (PureDOTS)

**Status**: Locked (conceptual + architecture)  
**Category**: Core / Progression / Learning  
**Scope**: Game-agnostic (PureDOTS). Game-side supplies content + presentation + UI.  
**Last Updated**: 2025-12-20

---

## 1) Goals

### Must-haves
- **Hybrid simulation**, leaning authentic ("Tamagotchi-like"): entities feel like they *become what they do*.
- **Progression for (almost) all entities**, while remaining scalable via LOD + aggregation.
- Multiple progression sources: **use**, **training**, **teaching/learning**, and **XP-based**.
- Skills support **continuous values + tier milestones**.
- **Soft-gated specialization** (encourages paths, doesn't hard-lock).
- Entities can **drift** over time and can **partially respec/unlearn** (player-guided; limited self-adjustment).
- Flexible mastery and synergy systems that can affect:
  - action unlocks,
  - success/outcome/crit,
  - planning/decision-making nudges.

See also:
- `Docs/Concepts/Core/Entity_Stats_And_Archetypes_Canonical.md` (canonical stat layers + archetype schema)

### Non-goals (PureDOTS)
- No fixed assumptions about "classes", "magic rules", "combat formulae", or specific gameplay tuning.
- No UI/UX implementation (game-side concern). PureDOTS exposes observable state + events.

---

## 2) Conceptual Model

### 2.1 Skill container rule
All entities expose the **same logical skill container API**. Internally, storage is **virtualized** by `ProgressionLOD` so the API remains stable while memory + CPU scale.

### 2.2 Progression primitives
- **Tracks**: generic progression records (skills, tools, fields, roles, tags).
- **Experience pools (4 total)**:
  - `PhysiqueXP`
  - `FinesseXP`
  - `WillXP`
  - `WisdomXP` (general pool + also modifies multiple gain rates)
- **Attributes**: derived from pools and/or used as modifiers for XP gain.
- **Derived combat stats**: e.g., **Attack/Defense** (separate from Damage/Armor), computed lazily when dirty.
- **Focus**: one universal "battery-like" resource; drains by usage; restored by rest; regen depends on attributes/skills/policies.

### 2.3 Presentation note (experience globes)
"Experience globes" are **cosmetic only**. Simulation uses events and deltas; game-side may spawn VFX from the same event stream.

---

## 3) Data Architecture (PureDOTS)

### 3.1 Registries and IDs (core + mods)
**Locked decision:** use integer IDs resolved through registries.

- `SkillId` / `TrackId`: stable **int indices** at runtime.
- **Core content** can be generated constants for tooling.
- **Mod/extension content** uses hashed names resolved at boot into registry indices.
- Runtime hot paths do not use strings.

#### Skill availability ("special assignments")
All skills are defined in the registry, but availability is gated by **capability tags** (e.g., `MagicCapable`, `PsionicCapable`, `RoboticCapable`, …).
- Skills exist logically but can be **locked** unless the entity has required capabilities.
- Preserves a single container API while enabling game-specific unlock rules.

### 3.2 BlobAssets (authoring → runtime)
All definitions are authored as data and compiled to Burst-friendly blobs:
- `BlobAsset<SkillDefs>`: thresholds, tier rules, mastery hooks, decay params, tags
- `BlobAsset<SynergyDefs>`: conditions + effects
- `BlobAsset<ProgressionPolicyDefs>`: XP routing, spending policy defaults, caps, curves

**Default chosen:** editor/build-time compilation into BlobAssets (hot reload editor-only). Runtime mod reload can be layered later without changing the core model.

### 3.3 Value encoding (memory-first defaults)
To stay scalable while keeping precision:
- **Continuous proficiency**: quantized integer (e.g., `ushort` mapping to 0..1 or 0..100).
- **Tier**: `byte`.
- **Rust/Decay debt**: quantized integer (e.g., `ushort`).
- **Last-used**: tick counter (int).

All fields remain overridable via policy if a game demands floats.

---

## 4) Storage & Scalability

### 4.1 Progression LOD (virtualization)
`ProgressionLOD` determines physical storage + evaluation frequency:

- **Full**
  - Dense track storage (default cap: **64 skills**).
  - Supports full synergy evaluation, granular decay, rich mastery stacks.
- **Reduced**
  - Dense "core" + a hot-set (LRU) for extra tracks.
  - Reduced evaluation budget; fewer synergies considered; coarser decay.
- **Aggregated**
  - Entity collapses into a **GroupProgressionState** (district/fleet/colony/pop bucket).
  - Retains a small identity seed + notable flags for consistent re-instantiation.
  - Progression continues statistically; individuals are materialized on promotion.

**Locked decision:** PureDOTS supports these modes; game-side chooses promotion/demotion rules (visibility + importance recommended).

### 4.2 Group progression state (Aggregated)
Group state stores:
- distribution summaries per track/pool (means, variance, tier counts)
- aggregate decay pressure
- notable tags/events (trained by master, survived major event, etc.)
- identity seed(s) for reconstruction

---

## 5) Update Pipeline (execution order)

**Locked decision:** progression is primarily **event-driven**, then applied in batches.

1. **Emit ActionEvents**
   - attack, take hit, craft, move, talk, cast, train, teach, etc.
2. **Accumulate XP deltas**
   - coalesce per entity / per chunk / per group (policy-controlled)
3. **Route to pools**
   - action tags + outcomes + context decide pool splits
   - Wisdom pool also applies modifiers to gains
4. **Apply pool modifiers**
   - attribute modifiers, fatigue/stress, environment, policy multipliers
5. **Spend XP (auto)**
   - automatic allocation with **player weights** when present
   - respects capability locks and soft gates
6. **Resolve tier crossings + mastery hooks**
   - unlock markers, capability thresholds, bonus application
7. **Mark derived stats dirty**
   - `ProgressionVersion++`
8. **Recompute derived stats (lazy)**
   - only if `DerivedStatsVersion != ProgressionVersion`
9. **Decay evaluation**
   - scheduled by LOD (`DecayStride`) using rust debt model
10. **Decision nudges**
   - applied at planning/decision points (synergy + mastery + context)

---

## 6) Skills, Tiers, and Trees

### 6.1 Continuous + tier milestones
- Continuous proficiency drives gradual improvements.
- Tier crossings provide **step changes** (unlock markers + milestone bonuses).
- Thresholds are **per skill** (bespoke) by default.

### 6.2 Soft-gated specialization (skill graphs)
Skills may be authored as a graph:
- prerequisites (soft)
- recommended paths
- "preferred next" weights

Spending uses availability + weights, not hard locking. Player weights can strongly bias choices.

### 6.3 Drift and respec
- Drift is automatic: "you become what you do".
- Respec/unlearn is supported but limited:
  - player can direct partial unlearning/reallocation
  - entity may independently shift a few skills
  - full resets are intentionally not the default

---

## 7) Experience Pools & Allocation

### 7.1 Pools (4 total)
- Physique / Finesse / Will / Wisdom (general)
- Actions generate XP; routing can split across pools.

### 7.2 Allocation
- **Auto-spend** is default.
- Optional `PlayerWeights` can guide allocation:
  - path bias in skill graph
  - per-track priorities
  - locked/forbidden sets (policy supports)

---

## 8) Mastery (multi-axis, flexible)

### 8.1 What mastery means
Mastery must be **noticeable**:
- increased efficiency (lower focus cost)
- expanded capability (multi-target, advanced defenses, broader effects)
- improved reliability (success/crit/posture effects)

### 8.2 Axes
Mastery may exist per:
- skill
- tool/weapon
- field/domain
- role
- special tags (threshold, event, reputation-based)

### 8.3 Stacking rule (safe default)
- **Additive** is default.
- **Multiplicative** allowed but constrained via policy:
  - category-based caps and/or diminishing returns
  - "best-of + fraction of remainder" optionally enabled per game

---

## 9) Focus (universal battery)

- One resource, drains by action intensity, restores with rest.
- Regen and efficiency are modified by:
  - attributes,
  - skills/mastery,
  - conditions (fatigue/stress),
  - policies.

Focus is engine-level; game-side may present it as mana/stamina/concentration later without changing the core.

---

## 10) Decay (rust debt model)

**Locked decision:** decay affects the track's mastery/learning and **snowballs** via both rate and cap when unused.

### 10.1 Model
Each track maintains:
- `RustDebt` (how much learned capability is "rusted")
- `DecayRate` (increases with neglect)
- `DecayCap` (max rust possible for the current proficiency band)

### 10.2 Cap bands (policy)
Supports banded caps like:
- above band A: cap ~10%
- above band B: cap ~20%
- above band C: cap ~35%
Exact thresholds are data-authored and game-tunable.

### 10.3 Refresh
Using/training a track can:
- reduce decay rate
- repay rust debt faster than normal learning
- restore functionality earlier, while full "clean" recovery still takes effort

---

## 11) Synergies (positive/negative, decision nudges)

### 11.1 Types
- pairwise
- set-based
- contextual
- extensible (future forms)

### 11.2 Evaluation scope (performance-safe)
Synergies are evaluated **only when needed**:
- during decision/planning candidate generation
- during action resolution (combat/crafting/etc.)

Avoid global per-entity scans.

### 11.3 Effects
Synergies may:
- adjust numbers (success, crit, outcomes, focus cost)
- enable/disable actions
- nudge planning/choice weights
- be positive or negative

---

## 12) Rule Engine (flexible but fast)

**Locked decision:** use compiled **Modifier Programs**.

- Definitions compile into a linear op list (bytecode-like) stored in BlobAssets.
- Ops are generic: Add/Mul/Clamp/Curve/IfTag/IfContext/DiminishingReturns/…
- Burst-friendly evaluation.

This keeps PureDOTS flexible without embedding gameplay logic in code branches.

---

## 13) Derived Stats (Attack/Defense etc.)

- Derived stats are computed from:
  - attributes,
  - skills/mastery,
  - tools/roles,
  - policies/synergies.
- Computed lazily using versioning:
  - `ProgressionVersion` increments on applied changes
  - `DerivedStatsVersion` tracks last recompute

Prevents recompute storms under heavy event load.

---

## 14) Determinism & Replays (future-proof defaults)

**Default chosen:** deterministic-friendly ordering without forcing fixed-point everywhere.
- deterministic RNG streams per entity/group if needed
- stable ordering for reductions (entity index / chunk order)
- strict determinism can be upgraded later by swapping math + ordering policies

---

## 15) Observability (engine-level debugging hooks)

PureDOTS exposes optional instrumentation:
- per-entity ring buffer of progression events (configurable, sampling recommended)
- counters/histograms:
  - events processed
  - XP routed/applied
  - spends executed
  - decay applied
  - modifier program cost

Game-side may visualize these as "why did this change?" UI.

---

## 16) Integration Points

Progression integrates via events + queries with:
- Learning systems (teaching/training events)
- Communication experience (social interactions → wisdom routing)
- Formation training (group events → group progression state)
- Combat resolution (skill checks, focus costs, mastery hooks)
- Decision-making (synergy nudges; capability gating)
- Relations (reputation-based tags; teaching/training networks)

---

## 17) Default Policy Values (overridable per game)

- **Max dense skills (Full LOD):** 64
- **Reduced LOD hot-set size:** 16 (LRU)
- **Synergy eval cap per decision:** 32 (higher for Full, lower for Reduced)
- **Decay stride:** Full < Reduced < Aggregated (configured by policy)
- **Stacking safety:** multiplicatives capped / diminishing returns enabled by default
- **Hot reload:** editor-only (runtime mod reload can be layered later)

---

## 18) DOTS Implementation Notes

- Event-driven accumulation is preferred; coalesce deltas before applying.
- Avoid structural changes in hot loops; keep track storage stable per LOD.
- Prefer chunk-wise processing and Burst evaluation of modifier programs.
- Keep cross-entity reads bounded (synergy/relations evaluated contextually, not globally).

---
