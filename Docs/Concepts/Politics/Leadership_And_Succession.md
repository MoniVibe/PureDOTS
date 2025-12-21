# Leadership & Succession — Plan (Flexible Engine)

> Goal: a profile-driven leadership engine where "leaders" are always mapped to in-world entities
> (individuals, councils, families, virtual beings, even a cat), supporting multiple leaders,
> shift-rule, coups, elections, inheritances, partitions, and "leadership affects nothing → everything".

---

## 0. Design Pillars

- **Flexibility-first:** no hardcoded "one true rule"; everything is expressed via **profiles/outlooks**.
- **Leaders are entities:** every leader maps to an actual entity in the world (living or virtual).
- **Multiple leaders:** leadership is a **group** with **seats**, not a single field.
- **Contest unification:** elections, coups, duels, rebellions, civil wars, partitions are all **contests**.
- **Bounded cost:** profiles may be wild, but the engine enforces **caps, caches, and event-driven updates**.

---

## 1. Core Concepts (engine "atoms")

### 1.1 Aggregate
An **Aggregate** is any higher-order actor:
- village/settlement/city
- faction/empire/dynasty
- fleet/task force
- colony/planetary administration

Aggregates own politics, relations, reputation, forces, etc.

### 1.2 LeadershipGroup
A LeadershipGroup is the aggregate's leadership "container".
- Holds a list of **LeaderSeats**
- Evaluated/changed by **LeadershipPolicy** (profile-driven)

### 1.3 LeaderSeat
A seat is an *office/slot* with an occupant entity.
- Examples: Ruler, War Leader, Steward, High Priest
- Seats define **domains** and default **arbitration** rules

**Vanilla defaults (modifiable):**
- `SeatCount`: up to 3 seats (Ruler/War/Steward) by default
- Council membership per seat: 3–12 by default (if the seat occupant is a Council entity)

### 1.4 Occupant (Leader Entity)
The seat occupant is always an entity reference.
Occupants can be:
- an individual person/creature/AI core/cat
- a **Council** entity (with members)
- a **Family/Dynasty** entity (with members)
- any "virtual leader" entity with the right components

### 1.5 AuthorityVector
Authority is modeled as a **vector of tagged axes** (expandable per profile):
- Reputation (e.g., dreaded, beloved, frugal…)
- Glory (e.g., heroic defender, usurper, rightful liege…)
- Renown (e.g., band leader → village chief → galactic overlord…)

Profiles may add more axes (legitimacy, competence, wealth, ideology, etc.).
Authority changes are **event-driven increments**, not full recomputation.

### 1.6 Contest
A Contest is a generic state machine that resolves leadership changes:
- Elections, coups, duels, trials, rebellions, civil wars, partitions
- Stakes can target: a seat, the whole LeadershipGroup, or territory splits

---

## 2. Data Schema (profile-driven)

This section is intentionally engine-agnostic; translate to ECS components/buffers.

### 2.1 LeadershipProfile (per Aggregate)
- `ProfileId` (culture/faction/dynasty/player-defined)
- references a `LeadershipPolicyId`
- optional overrides (caps, role list, weights)

### 2.2 LeadershipPolicy (data bundle)
A policy is a set of IDs + parameters (no if/else explosion).

**SelectionPolicy**
- candidate sources (bounded)
- selection method (election/inheritance/appointment/combat/mixed)
- tie-breakers

**SuccessionPolicy**
- immediate / vacuum / regency / contested / split-rule
- caretaker rules (optional)
- transition penalties/bonuses (stability, legitimacy, etc.)

**ChallengePolicy**
- who may challenge (internal elites/commons/military/external)
- triggers (authority thresholds, crises, doctrine, "warlike" outlook, etc.)
- contest types allowed
- stakes allowed (seat replace / leadership replace / partition)

**AuthorityPolicy**
- which axes exist
- how axes change from events
- decay/growth rules (or none)
- clamps, smoothing, thresholds

**EffectRoutingPolicy**
- which domains are affected and how (or "none")
- arbitration rule per domain (priority/weight/vote/delegation/contextual)

**MaintenancePolicy**
- how leaders maintain rule (prosperity, favors, repression, ideology, victories…)
- can be "automatic" (derived) or "action-driven" (leader chooses)

---

## 3. Candidate Pools (bounded, no global scans)

Candidate selection must never require iterating "everyone".
Profiles specify **candidate sources** that emit small lists, e.g.:
- dynasty members / bloodline
- elite offices (guild heads, priests)
- military officer corps
- event-born exceptional individuals
- player nominations

### 3.1 CandidateCache
Each aggregate maintains a bounded cache:
- updated on relevant events (death, promotion, scandal, victory, etc.)
- max size cap per source and total cap per aggregate

---

## 4. Multi-leader Arbitration (domain-aware)

When multiple seats/occupants conflict:
- **Domain locking:** each seat has allowed domains
- **Arbitration rule per domain:** priority / weighted vote / delegation / contextual

Example:
- War domain: War Leader overrides in wartime; otherwise weighted vote
- Economy domain: Steward dominates unless emergency decree
- Spiritual domain: Priest dominates unless outlawed

Profiles decide. Engine just executes.

---

## 5. Contest Framework (unified mechanism)

### 5.1 Contest Entity / Record
- `ContestTypeId` (election/coup/duel/civilWar/partition/ritual…)
- participants: incumbent side(s), challenger side(s)
- backing factors: elites/commons/military/external support (aggregate stats/tokens)
- stake: seat replace / leadership replace / partition / split rule
- phases: declare → build support → resolve → apply outcome

### 5.2 Outcomes
Must support:
- winner takes all
- seat replacement only
- shared rule (split seats)
- partition / split kingdoms / new aggregates
- enforced regency / caretaker

---

## 6. Event Stream + Severity Classification

Engine emits **raw leadership events**; downstream systems subscribe by severity.

Raw events (examples):
- SeatOccupied, SeatVacated
- LeadershipContestStarted, ContestPhaseChanged, ContestResolved
- AuthorityAxisChanged, AuthorityThresholdCrossed
- SuccessionStarted, SuccessionResolved
- PartitionCreated, AggregateSplit, NewAggregateFormed
- LeadershipVisibilityChanged (secret revealed, disputed claim, etc.)

Severity classifier:
- Minor vs Major (and more tiers later)
- severity = impact × visibility × novelty × player-relevance (all profile-tunable)

---

## 7. Execution Order (system schedule)

1) **Ingest World Events**
   - deaths, victories, famine, scandal, miracles, betrayals, promotions…
2) **Update Authority (event-driven)**
   - apply AuthorityPolicy rules to affected leaders/aggregates
3) **Refresh CandidateCache (event-driven)**
   - update bounded candidate pools from sources impacted by events
4) **Evaluate Triggers**
   - succession triggers, challenge triggers, legitimacy shocks, vacuum entry
5) **Start / Progress Contests**
   - create contests, advance phases, compute support/backing factors
6) **Resolve Contests**
   - resolve by ContestResolver modules specified by profile
7) **Apply Outcomes**
   - seat swaps, leadership replacement, splits/partitions, legitimacy changes
8) **Emit Integration Effects**
   - politics/relations/reputation/forces/tactical command effects (if routed)
9) **Emit Story Events**
   - classified major/minor events for narrative/presentation layers

---

## 8. Integration Surfaces (what leadership can influence)

Leadership may affect **nothing → everything**; routing is profile-driven.
Possible channels:
- **Politics:** biases aggregate decisions, unlocks edicts, shifts priorities
- **Relations:** diplomatic posture, trust, intimidation, alliance stability
- **Reputation:** leader ↔ aggregate propagation, public perception shifts
- **Forces:** morale, cohesion, doctrine adherence, command latency/quality
- **Tactical Commands:** what commands exist, who may issue, execution quality
- **Rebellion Mechanics:** uprising likelihood, civil war branching, faction splits

---

## 9. Performance Guardrails (non-negotiable)

- **Caps everywhere:** seats, council membership, candidate lists, contest participants.
- **No global scans:** always use cached lists, role registries, or aggregate stats.
- **Event-driven updates:** authority/candidate pools update on events, not every tick.
- **Mass actors are aggregated:** "commons" and "elites" participate via backing factors.
- **Deterministic ordering where needed:** contest resolution and arbitration must be stable.

---

## 10. Vanilla Policy Examples (content layer)

These are templates; profiles can override anything.

- **Hereditary Monarchy**
  - selection: bloodline
  - succession: immediate unless legitimacy shock
  - challenges: rare, mostly coups by elites
- **Warlike Clan**
  - selection: duel / merit combat
  - challenges: frequent; partitions possible
- **Meritocratic Materialists**
  - selection: competence + performance events
  - authority weights: competence/wealth > legitimacy
- **Twin Shift Monarchy**
  - two rulers share the Ruler seat via Availability (shifted)
  - visibility: public or secret; discovery causes legitimacy/stability shock
- **Council Republic**
  - occupant of seats is a Council entity (3–12 members)
  - arbitration: weighted vote by member influence

---

## 11. Checklist (to convert this plan into implementation)

1) Define IDs/registries:
   - SeatRoleId, DomainId, AxisId, ContestTypeId, CandidateSourceId, ArbitrationRuleId
2) Implement CandidateSourceProviders (bounded outputs)
3) Implement Contest resolvers (at least election, duel, coup, civil war, partition)
4) Implement Authority rules (event → axis deltas)
5) Implement arbitration per domain
6) Implement event stream + severity classifier
7) Wire integration channels (politics/relations/reputation/forces/tactical)

---

## 12. Open "content authoring" questions (kept flexible)

- What default seat roles exist in vanilla? (3-seat baseline is fine)
- Which authority axes are always present in vanilla? (your rep/glory/renown + optional)
- Which contest types ship in vanilla? (keep small set, allow mods to extend)
- What are default caps (seat count, council size, candidate list size)?
