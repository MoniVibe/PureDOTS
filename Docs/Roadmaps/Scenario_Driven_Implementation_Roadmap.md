# Scenario-Driven Implementation Roadmap

## Philosophy

**System-first implementation that maximizes scenario coverage without scenario scripts or hardcoded behavior.**

**Core Rule:** Scenarios are just initial conditions + constraints; outcomes come from generic systems + the AI pipeline.

## Existing Foundations

You already have strong foundations to build on:

- **ScenarioRunner** can execute deterministic, headless scenario JSONs and drive ticks in a dedicated world (`ScenarioRunnerExecutor`)
- **GOAP + Utility + Directives** exist in PureDOTS (planner runs only in Record mode) (`GOAPPlannerSystem`)
- **Interrupt pipeline** exists (interrupts → EntityIntent) and is explicitly placed before GOAP/behavior systems (`InterruptHandlerSystem`)
- **"Intent not micromovement"** philosophy with minimal action primitives (`Capabilities_And_Affordances_Sy…`)
- **Local time bubble tagging** implemented (`TimeDistortionApplySystem` adds/updates LocalTimeScale)
- **Design specs** for Capabilities/Affordances, Rituals (WorkSong etc.), and Memory/History as game-agnostic architecture

## Non-Negotiable Contract

**For every scenario you "support", you must be able to point to:**

1. **World facts** (perception + memory + registries) that make the situation discoverable
2. **Goals** (utility/GOAP) that make agents want to act
3. **Actions** (generic primitives + domain actions) with preconditions/effects/costs
4. **Constraints** (law, honor, authority, cooldowns, resource limits) as data, not if-statements
5. **A ScenarioRunner test** that seeds conditions and asserts metrics/expectations—no cinematic triggers

**If something needs to happen "because the scenario says so," that is a missing system.**

---

## Roadmap Overview (Dependency Order)

### Phase 0 — Scenario Infrastructure + Invariants
**Goal:** Every slice is validated via ScenarioRunner; determinism/perf regressions are caught immediately.

**Deliverables:**
- Scenario adapters (Godgame + Space4X) that replace the current "log counts" stub and actually spawn from registry IDs (your wiring docs already expect this)
- A small library of scenario JSONs for the 16 "target scenarios" (one per scenario) that only specify: counts, archetypes, initial relations, seed, and optional input commands
- "Invariant checks" mode: fail the run if illegal state occurs (NaNs, invalid relations, missing required singletons, broken rewind guards)
- **Metrics + assertion schema (hard requirement, not aspirational):** every scenario JSON declares expected metrics (e.g., "X deaths ≤ 1", "Y deliveries ≥ 10", "morale never NaN", "contracts resolved within N ticks"); ScenarioRunner **must enforce** these—runner fails the run if expectations aren't met; prevents drift into "it looks right in a video" testing
- **Input recording/replay:** log per-tick "agent action decisions + any player/hand inputs" and validate replay produces identical decisions; catches nondeterministic planning or tie-break drift early (not just seed determinism)

**Phase Gate:** At least one scenario JSON with declared metrics passes ScenarioRunner validation (deterministic replay + input replay + all assertions pass)

**Why now:** ScenarioRunner is already designed for headless, deterministic execution (`ScenarioRunnerExecutor`), so you should make it the central gate before you deepen AI.

---

### Phase 0.5 — Authoring + Registries + Scenario Adapters Are Real
**Goal:** Scenario JSONs instantiate worlds purely through registries/archetypes, not bespoke prefabs, so facts/goals/actions stay data-driven.

**Deliverables:**
- **Population registries:** archetypes define roles, needs, loadouts, capabilities, and default directives
- **Relation + aggregate seeds:** JSON can declare guild/village/fleet membership, allegiance, hostility, authority lines
- **World affordance registries:** authored references for storehouses, build sites, climbables, signal emitters, ritual sites, etc.
- **Adapter injection helpers:** adapters resolve registry IDs into entities, populate buffers (relations, affordances, contracts), and expose handles for ScenarioRunner assertions

**Hard Gate:** If you can't spawn any scenario from registries/archetypes/relations/affordances (no bespoke prefabs), Phase 2 facts/actions will explode into special cases. **This phase must be complete before Phase 2 work begins.**

**Phase Gate:** At least one scenario JSON spawns entirely from registry data (no prefab references); adapters resolve all IDs → entities → buffers correctly

**Why now:** Without this layer Phase 2+ systems cannot discover scenario state; adapters must be able to spawn any scenario purely from registry data before deeper AI work.

---

### Phase 1 — Time Correctness Everywhere
**Goal:** LocalTimeScale affects all time-consuming simulation consistently.

**Core Components/Systems:**
- **TimeScaleSamplerSystem:** writes an "effective delta" for entities (or per-system helpers) that combines global `TimeState.DeltaTime` with per-entity `LocalTimeScale`
- **"TimeAware" helper standard:** any system that advances progress bars, cooldowns, XP, concentration, drills, repairs, etc. must use effective delta (not raw delta)

**AI Integration:**
- GOAP replan intervals, utility decay, ritual phase progression, training ticks, repair ticks: all read effective delta

**Scenario Coverage:**
- **#2 Hyperbolic Time Chamber** becomes genuinely supported (not "bubble exists but only affects movement"), because combat/XP/etc. scale too

**Phase Gate:** Scenario #2 passes with time scaling affecting all time-consuming systems (movement, combat, XP, cooldowns); deterministic replay + assertions pass

---

### Phase 2 — "Body → Mind" Behavior Loop
**Goal:** Convert the current "job state machine" into planner-driven actions, while keeping it efficient.

**Core Building Blocks:**

**Perception → Interrupts:**
- Use the existing interrupt system to push urgent events into EntityIntent (`InterruptHandlerSystem`)

**WorldStateFacts builder (must be stable and typed early):**
- **Minimal, stable World Facts API:** lock a tiny stable surface before expanding—**don't let facts become ad-hoc queries**
  - Fact keys: bool/int/float enums (not ad-hoc strings) — **typed, not string-based**
  - Provenance: perception vs memory vs registry (so planners know freshness) — **explicit tracking**
  - Update cadence: every tick vs event-driven (so costs stay predictable) — **documented contract**
- A small set of facts per agent: "has target", "is threatened", "is hungry", "sees resource", "knows storehouse with capacity", "is in formation", etc.
- This API layer keeps GOAP actions generic instead of exploding into ad-hoc checks
- **Hard requirement:** World Facts API must be locked and stable before building action libraries; no retrofitting later

**Action execution layer:**
- Hard rule: behavior systems output intent primitives like MoveTo / TraverseEdge / Interact / Recover (`Capabilities_And_Affordances_Sy…`) plus domain actions built on top (Gather, Deliver, Train, Sing, Investigate, Negotiate, Repair…)

**Aggregate + roles + directives:**
- Minimal components for aggregate membership (`AggregateMember`, `RoleTag`, `ChainOfCommand`) plus a directive bus that lets aggregates push orders to members
- Trainers vs trainees, patrol leaders vs followers, ritual leaders vs participants all use the same data flow

**Needs + interruptions:**
- Godgame's AI kickoff plan already calls for needs + interrupt wiring; implement it as data feeding utility/GOAP, not a new bespoke FSM

**Resource requests + fulfillment:**
- Resource containers expose capacity/inventory facts
- `NeedRequest` buffers describe what an agent/group requires; GOAP actions like FulfillRequest pull from stores, deliver to targets, and emit `DeliveryReceipt`

**Constraints as data (first-class type, introduced exactly as written):**
- Small general constraint mechanism for: "non-lethal allowed", "no trespass", "obey leader", "cannot use forbidden magic"
- Constraint components/tables that planners consult during action selection
- **Hard requirement:** Even a minimal constraint table prevents early action libraries from teaching agents to trespass/kill/use forbidden actions and then needing rewrites later
- **Mechanically unavoidable:** Add a test that proves an agent cannot pick a forbidden action even if it's otherwise optimal (e.g., "trespass" is fastest path but constraint blocks it; planner must choose slower legal route). This prevents the common failure mode where constraints exist but aren't actually consulted by planners.
- Formal trials/contracts can wait until Phase 8, but basic constraint checking must exist here so actions respect limits
- **Must be implemented in Phase 2 exactly as described**—not deferred or approximated

**Runtime planning availability:**
- GOAP/Utility planners must execute in the exact sim mode ScenarioRunner uses (not only Record mode) so determinism/coverage tests exercise real planning

**Deliverables:**
- "Action Library v0" (PureDOTS): Gather/Deliver/Rest/Patrol/Follow/Attack/Flee
- "Goals v0": Survive, MaintainNeeds, FulfillJob, DefendGroup, FollowOrders
- Aggregate membership + leader references + directive propagation built into the shared runtime
- Resource request/fulfillment pipeline with factual exposure (storehouse status, outstanding needs, delivery confirmations)
- GOAP/Utility planning enabled in headless ScenarioRunner sims (config + tests prove parity with Record mode)
- A lightweight "resume" mechanism: interrupts can pause a plan and resume later (using buffers/snapshots, not special-case code)

**Scenario Coverage:**
- This is the backbone for almost every scenario because it turns "systems exist" into "agents can actually use them"

**Phase Gate:** **Gate Scenario: "Basic Resource Gathering"** — Agents must gather resources from source nodes and deliver them to storehouses using GOAP/Utility planning. Scenario includes a "no trespass" constraint on a direct path; agents must choose longer legal route. Passes when: (1) agents complete gather→deliver cycle via planning, (2) constraints block forbidden actions even when optimal, (3) deterministic replay + input replay + assertions pass

---

### Phase 3 — Navigation + Capabilities/Affordances + Terrain Leverage
**Goal:** Make movement and world interaction capability-driven and world-authored, not raycast spam.

**Core Components/Systems:**
- **MobilityCapability** (computed cache) and dirty recompute pattern
- **WorldAffordance indexing** (ladders/ledges/climb surfaces/cover/etc.) and spatial lookup
- **Nav graph edges** with traversal types and cost/risk calculation

**AI Integration:**
- GOAP actions like "ReachTarget" should rely on nav that can include traversal edges, not bespoke climb/jump logic
- **Navigation cost functions:** traversal produces reusable cost/risk values that feed GOAP action costs directly (not just paths); AI chooses better routes because costs are factored into planning
- Utility scoring can prefer safer paths unless desperate

**Scenario Coverage:**
- Enables the "terrain/elevation matters" scenarios to become organic (ambushes, flanking routes, hard-to-reach positions)

**Phase Gate:** **Gate Scenario: "Terrain Route Choice"** — Agents must reach a destination with two route options: (1) direct dangerous climb (high cost/risk), (2) longer safe path (low cost/risk). Passes when: (1) agents choose safer route based on traversal costs feeding GOAP action costs, (2) navigation cost functions are reusable and feed planning, (3) deterministic replay + input replay + assertions pass

---

### Phase 4 — Combat "v1" (Training, Non-Lethal, Formations, Morale)
**Goal:** Combat outcomes become data-driven, scalable, and feed back into AI decisions.

**Core Components/Systems:**
- **Non-lethal mode** (sparring/duels/training) as a combat contract flag
- **Training session system** (practice schedules + XP allocation) used by both personal training and fleet drills
- **Formation + cohesion + morale** as first-class modifiers (your audit lists these as missing stubs; implement minimal real versions, not scripted waves)
- **Morale/cohesion stat loop (hard rule):** morale is computed, clamped, decayed/recovered every tick in Phase 4; later systems (rituals in Phase 5, diplomacy in Phase 8) only apply modifiers—no duplicate morale logic, no rewrites

**AI Integration:**
- Actions: Spar, BreakOff, HoldLine, Flank, Rally, ProtectWounded
- Facts: "in duel contract", "honor risk", "cohesion low", "morale breaking"

**Scenario Coverage:**
- **#1 Training circle**, **#7 lawful duel**, **#12 training drills**, and combat-heavy scenarios move from "partially ready" to real

**Phase Gate:** Scenario #1 (Training circle) passes with non-lethal combat, training sessions, and morale/cohesion stat loop functioning; deterministic replay + assertions pass

---

### Phase 5 — Rituals as Universal "Coordinated Sustained Action" System
**Goal:** Work songs, war chants, speeches, ship formations, and sustained spells share one phase/check framework.

**Core Components/Systems:**
- **Ritual + participants buffer** + phase progression + phase checks (deterministic RNG streams)
- **Output hooks:** modifiers that target existing morale/cohesion/efficiency stats, noise/signature emission (feeds perception); no duplicate morale logic

**AI Integration:**
- Goals: ImproveCohesion, BoostWorkRate, RallyBeforeBattle
- Actions: JoinRitual, LeadRitual, MaintainRitual, DisruptEnemyRitual

**Scenario Coverage:**
- **#3 shanties/work songs**, **#4 "heroes vs horde" rally moments**, **#12 drills (cadence)**, plus lots of emergent culture

**Phase Gate:** Scenario #3 (Work songs) passes with rituals modifying morale/cohesion (not duplicating logic); deterministic replay + assertions pass

---

### Phase 6 — Perception Deepening
**Goal:** Signal fields + medium context + stealth + investigation (enables assassins/ambush organically).

**Core Components/Systems:**
- **Signal emitters** (sound/smell/EM) → spatial diffusion/aggregation (cheap grid-based)
- **Medium gating** (vacuum vs gas vs liquid) so hearing/smell behave correctly
- **Detection results** that generate interrupts (SmellSignalDetected, etc.)—your interrupt enums already include these (`InterruptHandlerSystem`)
- **Suspicion + disguise + witness observation** as generic mechanics (not "assassin scenario code")

**AI Integration:**
- Behaviors: patrol, investigate, question, shadow, flee, stealth kill attempt (still just an action with preconditions: "unseen, in range, target vulnerable, no witness")

**Scenario Coverage:**
- **#6 assassins in village**
- **#10 layered ambush stalemate**
- Also strengthens **#14 jamming/information warfare** downstream

**Phase Gate:** Scenario #6 (Assassins) passes with perception/investigation/stealth mechanics; deterministic replay + assertions pass

---

### Phase 7 — Memory/History v0 + Witness → Consequences
**Goal:** Social causality without scripts.

**Core Components/Systems (Minimal):**
- **Event ingest bus** (combat outcomes, murders, trials, miracles, contraband finds)
- **Canonical record store** + holder MemoryHandle buffers
- **Attachment policy:** participants + nearby witnesses + relevant aggregates
- **Propagation v0:** aggregate-to-aggregate rumor tokens (rate-limited)

**AI Integration:**
- Facts: "I witnessed X", "my guild believes claim Y", "this person is suspected"
- Goals: avenge, report, punish, defect, demand trial

**Scenario Coverage:**
- **#15 guild war over forbidden magic** (witness → accusations → war)
- **#6/#10** (investigation)
- **#16** (diplomacy outcomes persist)

**Phase Gate:** Scenario #15 (Guild war) passes with witness → memory → propagation → consequences chain; deterministic replay + assertions pass

---

### Phase 8 — Conflict Resolution + Authority + Diplomacy
**Goal:** Turns "social scripts" into systems.

**Core Components/Systems:**
- **Contracts:** duel contracts, training contracts, inspection contracts (data: allowed actions, victory conditions, penalties)
- **Authority enforcement:** who can halt combat, who can arrest, who can demand inspection
- **Trial pipeline:** accusation → evidence/witness list → verdict → sentence
- **Diplomacy/social combat:** charisma/authority rolls as resolution mechanics (no dialogue trees needed initially)

**AI Integration:**
- Goals: MaintainHonor, EnforceLaw, AvoidPunishment, WinNegotiation
- Actions: Accuse, PresentEvidence, Arbitrate, Inspect, Bribe, Comply, Resist

**Scenario Coverage:**
- **#7 lawful duel**
- **#15 guild war escalation**
- **#16 imperial compliance visit**

**Phase Gate:** Scenario #7 (Lawful duel) passes with contracts, authority enforcement, and trial pipeline; deterministic replay + assertions pass

---

### Phase 9 — Aggregate/Strategic AI + Information Warfare
**Goal:** Finishes #14 and boosts Space4X scale.

**Core Components/Systems:**
- **Aggregate world model** (faction/village/fleet) with summarized facts: comm status, threat level, resource stock, recent incidents
- **Fog-of-war + delayed intel tokens** (fed by perception + memory propagation)
- **Response playbooks** as goals/actions: dispatch probe, reinforce, retreat, jam back, quarantine

**Scenario Coverage:**
- **#14 jamming raid** becomes truly organic (factions react because comms + intel changed)

**Phase Gate:** Scenario #14 (Jamming raid) passes with aggregate AI reacting to comm status/threat level changes; deterministic replay + assertions pass

---

### Phase 10 — Emergency + Accidents + Rescue
**Goal:** Unlocks the "not ready" emergency scenarios.

**Core Components/Systems:**
- **Hazard state machines** (crash, fire, decompression, stuck mech clamp, etc.) as generic hazard entities + effects
- **Rescue coordination:** triage priorities, defenders vs repair crews, timed objectives
- **Logistics in emergencies:** "needed supplies" become requests that agents/fleets fulfill

**Scenario Coverage:**
- **#13 crashed carrier emergency**
- **#12 fleet training accidents/rescue**

**Phase Gate:** Scenario #13 (Crashed carrier emergency) passes with hazards, rescue coordination, and emergency logistics; deterministic replay + assertions pass

---

## High-Leverage "First 3 Milestones"

**What to sequence immediately:**

1. **Scenario adapters + test harness (Phase 0)** so every new system is locked to repeatable runs (Phase 0.5 extends this by guaranteeing registry-driven authoring)
2. **TimeScale correctness (Phase 1)** because it's already "mostly ready" and it forces proper integration discipline
3. **Behavior loop conversion (Phase 2)** so systems stop being "available" and start being "used" by AI

**After that, you can branch:** Combat/Training (Phase 4) and Rituals (Phase 5) are the next biggest multipliers.

---

## Scenario Phase Gate Sanity Map

- **#2 Hyperbolic Time Chamber:** Phase 1 (needs TimeScaleSampler + TimeAware usage)
- **#1 Training circle / #12 drills:** Phase 4 once Phase 2 aggregates/roles/directives exist
- **#3 Work songs:** Phase 5 layered on top of the Phase 2 loop + aggregates
- **#6 Assassins / #10 layered ambush:** Phase 6, with Phase 3 traversal making success organic
- **#7 Lawful duel / trials:** Phase 8 (builds directly on Phase 4 combat contracts)
- **#14 Jamming / information warfare:** Phase 9 (needs Phase 6 perception + Phase 7 memory propagation)
- **#15 Guild war escalation / #16 imperial compliance visit:** Phase 7–8 (witness chain + authority enforcement)
- **#13 Emergency rescue:** Phase 10 (hazards + rescue logistics)

Use this sanity map to verify that each scenario enters ScenarioRunner validation only after its enabling phases land.

---

## Practical Implementation Rule

**For every new "domain system", implement it as:**

1. **Components** (state + config references)
2. **Simulation systems** (deterministic, rewind-safe)
3. **AI surface**
   - World facts emitted
   - GOAP actions + utility options
   - Interrupts generated when urgent
4. **ScenarioRunner coverage**
   - At least 1 scenario JSON + expectations

**This keeps you honest: no domain ships until AI can exploit it.**

---

## Practical "Done" Criterion

**For each phase: at least one scenario must pass in ScenarioRunner with:**

1. **Deterministic replay** (same seed → same outcomes)
2. **Input replay** (per-tick agent decisions + player/hand inputs logged and validated on replay; catches planning nondeterminism)
3. **Metric assertions** (all declared expectations validated automatically)
4. **No scenario-specific code paths** (only data/config differences between scenarios)

**Phase gates are "scenario runnable + assertable," not "systems implemented."** This enforces the non-negotiable contract and prevents hardcoding.

---

## Phase Gate Checklist

**Each phase must pass its gate scenario(s) before proceeding to the next phase. This prevents phases from drifting into "we implemented systems but nobody uses them."**

- **Phase 0:** At least one scenario JSON with declared metrics passes ScenarioRunner validation (deterministic replay + input replay)
- **Phase 0.5:** At least one scenario JSON spawns entirely from registry data (no prefab references)
- **Phase 1:** Scenario #2 (Hyperbolic Time Chamber) passes with time scaling affecting all systems
- **Phase 2:** Gate Scenario "Basic Resource Gathering" passes (gather→deliver via GOAP, constraints block forbidden actions, deterministic + input replay)
- **Phase 3:** Gate Scenario "Terrain Route Choice" passes (agents choose safer routes based on traversal costs feeding GOAP, deterministic + input replay)
- **Phase 4:** Scenario #1 (Training circle) passes with combat contracts + morale loop
- **Phase 5:** Scenario #3 (Work songs) passes with rituals modifying morale (not duplicating)
- **Phase 6:** Scenario #6 (Assassins) passes with perception/investigation/stealth
- **Phase 7:** Scenario #15 (Guild war) passes with witness → memory → consequences chain
- **Phase 8:** Scenario #7 (Lawful duel) passes with contracts + authority + trials
- **Phase 9:** Scenario #14 (Jamming raid) passes with aggregate AI reacting to intel changes
- **Phase 10:** Scenario #13 (Crashed carrier emergency) passes with hazards + rescue logistics

