# Entity Profile Schema (Alignment / Outlook / Behavior)

**Status**: Draft (authoritative direction)  
**Category**: Core / AI / Social  
**Applies To**: Godgame, Space4X, shared PureDOTS

---

## Purpose

Define a shared, game-agnostic way to represent an entity’s **values**, **ideology**, and **temperament** (“Profile”), and how we derive the **tunable numbers** that systems actually consume (“Policy”).

This is the bridge between:
- micro AI (individual villagers, captains, pilots), and
- macro AI (villages, ships, fleets, factions).

Key requirement: profiles must work for **individuals** and **aggregates** (crews, villages) with the *same conceptual schema*.

Blank-by-default rule:
- Profile facets are **modules**. Entities may have none of them.
- Systems must not assume a “default profile”; they must query only for the components they require.

---

## Terms

- **Profile**: relatively stable traits and identity (alignment, ideology axes, personality/behavior, culture/race, nature/archetype).
- **Policy**: derived biases/thresholds used directly by gameplay systems (obedience, risk tolerance, aggression, consensus appetite, mutiny thresholds, delegation rules).
- **Stance**: a *situational* orientation toward a specific authority/relationship (loyalist vs mutinous vs opportunist). Stance is not ideology.

---

## Profile Facets (Canonical)

### 1) Alignment (universal tri-axis)
Canonical axes across PureDOTS:
- **Moral**: Good (+) ↔ Evil (-)
- **Order**: Order/Law (+) ↔ Chaos (-)
- **Purity**: Pure/Uncorrupt (+) ↔ Corrupt (-)

Recommended numeric domain:
- systems-facing: `float` in `[-1..+1]` (fast math, blending)
- content-facing: `sbyte` in `[-100..+100]` (authoring-friendly)

Existing in repo:
- `PureDOTS.Runtime.Villagers.VillagerAlignment` (sbyte axes + normalized helpers)
- `PureDOTS.Runtime.Individual.AlignmentTriplet` (float axes)
- `Space4X.Registry.AlignmentTriplet` (Law/Good/Integrity as `half`)

**Axis aliasing note (narrative lens)**  
Games/cultures may *interpret* the same axis differently (e.g., “Purity” as “Integrity”, “Moral” as “Altruism vs Exploitation”) without changing the core math.

### 2) Ideology / Ethics axes (sparse, extensible)
Use for “authoritarian vs egalitarian” and other strategic outlooks that should scale to aggregates.

Canonical axis set (minimum viable):
- **Authority**: Authoritarian (+) ↔ Egalitarian (-)
- **Military**: Militarist (+) ↔ Pacifist (-)
- **Economic**: Materialist (+) ↔ Spiritualist (-)
- **Tolerance**: Xenophilic (+) ↔ Xenophobic (-) *(or invert, but be consistent per axis)*
- **Expansion**: Expansionist (+) ↔ Isolationist (-)

Recommended representation:
- sparse `IBufferElementData` of `(AxisId, Value)` where `Value ∈ [-1..+1]`
- omit near-zero axes to keep buffers small

Existing close matches:
- `PureDOTS.Runtime.Alignment.EthicAxisValue` with `EthicAxis { Authority, Military, Economic, Tolerance, Expansion }`.
- `Space4X.Registry.EthicAxisValue` with `EthicAxisId { War, Materialist, Authoritarian, Xenophobia, Expansionist }`.

### 3) Temperament / Personality / Behavior (how, not what)
These do *not* encode ideology; they encode response patterns.

Use a small set of continuous axes that are easy to aggregate:
- **Boldness** (risk appetite under uncertainty)
- **Conviction** (stubbornness vs pliability)
- **Selflessness** (pro-social vs self-interested)
- optional: **Patience**, **Honesty/Deception**, **Vengeful/Forgiving**

Existing in repo:
- `PureDOTS.Runtime.Individual.PersonalityAxes` + `MoraleState` + `BehaviorTuning`
- `PureDOTS.Runtime.Villagers.VillagerBehavior` (currently vengeful/bold + initiative modifier)

### 4) Identity anchors (race/culture/nature)
Use these to bias policies and to resolve “same profile, different species/culture” divergences.

- **Race/species** (`RaceId`)
- **Culture** (`CultureId`)
- **Nature/Archetype**: “what kind of entity is this?” (human civilian, militant clone crew, ascetic monastic order, etc.)

Existing in Space4X:
- `Space4X.Registry.RaceId`, `CultureId`

### 5) Dynamic state (feeds into policy)
These move frequently and should not be confused with “profile”:
- **Morale / stress / fatigue / cohesion** (for individuals and for crew departments)
- **Grievances / bonds / loyalties** (relationship state)
- **Stance** toward an authority (loyalist/opportunist/mutinous)

Existing in Space4X:
- `AffiliationTag` (membership + loyalty)
- `OutlookEntry` / `TopOutlook` (currently used like “stance tags”)

### Profile action accounting (mutation inputs)
Profile mutation is event-driven and uses bounded buffers (no per-tick scans).
- Systems emit `ProfileActionEvent` into a `ProfileActionEventStream`.
- Orders are tracked separately from execution: `OrderIssued` vs `ObeyOrder`/`DisobeyOrder`.
- Events carry intent/justification/outcome flags, magnitude, and authority-seat context.
- `ProfileMutationSystem` accumulates deltas in `ProfileActionAccumulator` and applies them on a cadence with decay.
- Games supply the action catalog (token → alignment/outlook deltas); PureDOTS owns the mutation logic.

---

## Mapping & Compatibility (Space4X ↔ PureDOTS)

### AlignmentTriplet naming
Space4X currently uses `Law/Good/Integrity`; PureDOTS uses `Order/Moral/Purity`.

Recommended conceptual mapping:
- `Order ≈ Law`
- `Moral ≈ Good`
- `Purity ≈ Integrity`

This keeps all downstream “profile → policy” math consistent even if component types differ today.

### Ethics axis naming
Space4X `EthicAxisId.War` is treated as the canonical **Military** axis (militarist ↔ pacifist).

If we later unify axis enums across games, prefer the canonical names in this doc and map game enums at boundaries.

### Stance vs ideology
Space4X `OutlookId { Loyalist, Opportunist, Fanatic, Mutinous }` is **stance**, not ideology.
Keep stance per-affiliation (membership context) and avoid conflating it with ideology axes (Authority/Military/etc.).

---

## Policy Derivation (Profile → Policy)

### Why policy exists
Systems want small, hot-path numbers:
- how obedient is this crew?
- how risk-tolerant is this captain?
- how quickly does this village make decisions?
- how likely is a mutiny/coup given an order?

Policy should be:
- derived deterministically from profile + dynamic state
- cheap to compute / cache
- valid for both individuals and aggregates

### Presentation-facing signal (theme agnostic)
PureDOTS also derives a lightweight, continuous "flavor" vector for presentation mapping:
- `ArchetypeFlavor` (runtime) summarizes alignment/outlook/personality/power axes in normalized [-1..1].
- It carries no theme or visual data; games map it to their own palettes, materials, and naming.
- Combinations are organic; no explicit archetype enums are required.

### Minimal policy fields (suggested)
These are the “shared knobs” that both games can consume:
- `ObedienceBias` (0..2): higher = follows authority orders
- `AggressionBias` (0..2): higher = prefers violent resolution
- `RiskTolerance` (0..1): higher = accepts danger/uncertainty
- `ConsensusAppetite` (0..1): higher = prefers councils/quorum/committee
- `MutinyPressureThreshold` (0..1): lower = mutiny triggers sooner
- `DelegationPreference` (0..1): higher = delegates to specialists/officers
- `OrderRefusalBias` (0..1): higher = more likely to refuse questionable orders (esp. under low morale/cohesion)
- `FriendlyFireInhibition` (0..1): higher = stronger reluctance to fire on allies / friendlies
- `ROEStrictness` (0..1): higher = requires clearer legitimacy/target classification before violence
- `NegotiationBias` (0..1): higher = prefers compromise/mediation over coercion
- `PunishmentSeverity` (0..1): higher = harsher discipline response to refusal/dissent
- `CollectivePunishmentTolerance` (0..1): higher = more willing to punish groups (e.g., decimation) to enforce obedience

Implementation note: PureDOTS already has `BehaviorTuning` for several biases; expand or layer a second “AuthorityPolicy” facet rather than cramming everything into one component.

### Governance mode derived from ideology
For aggregates (villages, ships):
- High **Authority** axis (authoritarian) → single executive authority (mayor/captain) with delegates; low consensus appetite.
- Low **Authority** axis (egalitarian) → council/quorum; higher consensus appetite; slower but more resilient to coups.

### Mutiny pressure is situational
Mutiny is not just “low obedience”:
- ideology mismatch with orders (Authority/Military axes)
- legitimacy of the issuer (law/order lens)
- morale/stress + recent losses
- presence of charismatic leaders (named officers)
- “Nature” constraints (e.g., fanatical cult crew vs mercenary crew)

Policy should therefore expose **thresholds** and **weights**, but the event/context system decides when pressure accumulates.

### Refusal is also situational (not only “mutiny”)
Order refusal can happen well before “open rebellion”, especially for named officers:
- low morale/cohesion makes risk-taking and questionable actions less tolerable
- ROE/legitimacy concerns (e.g., unclear hostile intent, poor target classification)
- friendly-fire inhibition (high moral / lawful / cohesive crews resist attacking allies)

Refusal should be represented as:
- a **decision outcome** in the order pipeline (reject / delay / request clarification / escalate),
- plus a pressure/cost effect (confidence drop, cohesion/morale shift, compliance ticket, or escalation request).

### Enforcement response is also profile-driven
Authorities should respond to refusal/dissent using a bounded “response ladder”:
- clarify → negotiate → escalate/vote → coerce → punish → purge (rare/extreme)

These actions are situational and should be driven by:
- alignment and ideology (authoritarianism, militarism, moral stance),
- personality (conviction/selflessness),
- dynamic state (morale, cohesion, stress),
- scenario constraints.

They should never be “free”: harsh enforcement should trade short-term compliance for long-term instability, reputation damage, and higher rebellion risk.

### Discretion flags (explicit capabilities on leaders)
Some enforcement actions must be gated by explicit powers held by specific named characters (captains/officers/overseers), not by “aggregate policy” alone.

Design rule:
- **Only authority seat occupants** with the relevant **discretion flags** can select extreme actions (e.g., decimation, mass airlock).
- Aggregates do not “decide atrocities” unless a legitimate (or coup-installed) occupant executes that choice.

Example flag set (conceptual):
- `AllowCollectivePunishment` (ration cuts, forced labor, collective confinement)
- `AllowLethalDiscipline` (summary execution in crisis)
- `AllowDecimationOrMassExecution` (rare, extreme)
- `AllowOverrideROE` / `AllowFriendlyFire` (very rare; usually scenario-gated)

This separates:
- **what the group would tolerate** (policy/ideology), from
- **what the current leadership is empowered to do** (capability flags).

---

## Named Characters vs Aggregates (Crew/Village)

### Named characters (captain/officers/pilots)
- full profile depth (alignment + ideology axes + personality + morale)
- can hold authority seats
- can be targets of promotion, capture, death, betrayal, hero events
- may carry **discretion flags** that enable/disable extreme governance actions

### Aggregates (crew mass, village population)
- limited depth (still has alignment + ideology axes + morale/cohesion, but fewer individual quirks)
- acts as a single decision consumer/producer for many systems (compliance, production, stability)
- may internally contain “sub-factions” (loyalists/rebels/neutrals), but represented compactly (e.g., 3 weights + a couple thresholds)

Additional requirement: aggregates must support **scrutiny of radical elements** (they are not monolithic).
- Track the internal split (loyalist / rebel / neutral) plus an “extremist tail” signal.
- Allow governance systems to target catalysts (named officers, radical cells) instead of treating the whole aggregate as a blob.

Practical representation options:
- **Aggregate entity** linked to the ship/village (recommended for clean reuse across games)
- or **components on the asset entity** (ship/village) if simpler

---

## Telemetry (proof-oriented)

For every new behavior slice that depends on profile/policy, add at least:
- a metric for the derived policy field(s) (e.g., `policy.obedience`, `policy.consensus`)
- a proof/event when a threshold is crossed (e.g., `event.mutiny_pressure_crossed`)

Keep payloads small; prefer structured metrics over giant JSON snapshots.
