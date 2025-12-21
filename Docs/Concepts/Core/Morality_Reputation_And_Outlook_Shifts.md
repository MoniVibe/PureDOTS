# Morality, Reputation, and Outlook Shifts (Event-Appraised, Culture-Weighted)

**Status**: Draft (authoritative direction)  
**Category**: Core / AI / Social / Governance  
**Applies To**: Godgame, Space4X, shared PureDOTS

---

## Purpose

Model “morality” as **culture-weighted norm appraisal of events** plus **reputation beliefs**, not as a single global good/evil meter.

This unlocks:
- culture/race/faction differences (“taboo here, acceptable there”),
- scalable simulation (event-driven, no per-tick scans),
- player-legible “why did they hate me?” drilldowns,
- flexible scenarios (sanctioned violence, retaliation nuance, coercion/obedience dynamics),
- clean integration with authority/delegation (`IssuedByAuthority`) and comms/sensors (evidence/rumor).

“Good/evil visuals” become **presentation** derived from tracked signals, not the simulation truth source.

---

## Core Split (2 state machines)

### 1) Reputation (external belief)
**What others believe about you**, tracked per **scope** (village, faction, ship crew, colony), not per observer×actor pair.

### 2) Outlook (internal drift)
**What you become** over time (habits + identity internalization), slow, bounded, and hysteresis-driven.

**Recurring error to avoid**: one `Alignment` float that tries to serve both social reputation and inner character change.

---

## Foundation: Moral Vector + Ethics Stances

### Moral vector (continuous, culture-weighted)
Use a compact moral “vector” as the shared language across games:
- Care / Harm
- Fairness / Cheating
- Liberty / Oppression
- Loyalty / Betrayal
- Authority / Subversion
- Sanctity / Degradation
- (Project axis) Stewardship / Exploitation *(environmental abuse, biosculpting harm, pollution, etc.)*

Each **culture/race/faction** provides a weight vector over these axes (and may invert signs).

**Recurring error to avoid**: encoding “deforestation is evil” as a universal rule; it must be an opinion expressed by weights + stances.

### Ethics stance table (discrete taboos / legality)
On top of the continuous vector, keep a discrete stance per `ActionToken`:
- `ACCEPTABLE`
- `JUSTIFIED_IF_GOOD_REASON`
- `JUSTIFIED_IF_SELF_DEFENSE`
- `ONLY_IF_SANCTIONED`
- `PUNISH_SERIOUS`
- `PUNISH_CAPITAL`
- `UNTHINKABLE`
- `REQUIRED`

This supports:
- fast “taboo here?” checks,
- clean policy + punishment ladders,
- crisp culture contrast and hostility triggers.

---

## Moral Events (event accounting, not scanning)

Gameplay systems emit `MoralEvent` only when something happens:

**Minimum payload**
- `Actor` (entity)
- `ActionToken` (enum/id)
- `Targets` (victim/beneficiary/environment region handles)
- `Magnitude` (quantized int)
- `IntentFlags` (malice / negligence / benevolence / coerced)
- `JustificationFlags` (self-defense / retaliation / sanctioned / necessity)
- `OutcomeFlags` (harm occurred, help occurred, collateral)
- `EvidenceHandle` (who saw it / sensor channel confidence / rumor provenance)
- `IssuedByAuthority` (optional) to attribute “captain via shipmaster” vs “shipmaster seat”

**Recurring error to avoid**: recomputing morality by iterating entities every tick and reading their components.

---

## Appraisal (context-sensitive blame/praise)

Appraisal is **observer-scope** (culture/faction/crew/village) applying norms to an event:

1) `axisDelta = Token.AxisVector * Magnitude`
2) `modifier = f(intent, justification, sanction, consent, relationship)`
3) `judgment = dot(axisDelta, scope.CultureWeights) * modifier`
4) Apply stance overrides:
   - e.g., `ONLY_IF_SANCTIONED` and no sanction ⇒ heavy negative + crime tag

Guiding principle:
- blame tends to weigh **intent** more,
- praise tends to weigh **outcomes** more,
and weights can vary by domain.

---

## Reputation at Scale (scope-based, belief-weighted)

We do **not** store observer×actor reputation matrices.

Instead:
- Each actor keeps bounded `ReputationInScope` records for the scopes that matter (village, faction, crew, colony).
- Scopes may also keep aggregate “known offenders” summaries if needed for UI, but simulation stays actor-centric and bounded.

Belief strength is evidence-weighted:
- direct witness > rumor,
- high-trust messenger > low-trust messenger.

This plugs into comms/sensors:
- evidence handles can be created by medium-first caches (sound/smell/EM) and comm attempts.

---

## Outlook Drift (habits → identity)

Outlook drift is internalization:
- actor commits actions ⇒ update a small habit accumulator per axis/token,
- periodically re-fit internal weights toward habits (bounded + reversible),
- hysteresis prevents flip-flops after one big event.

Integration hooks:
- Outlook drift feeds `AgencySelf`/`ControlLink` contests (willingness to coerce/submit),
- feeds governance refusal/mutiny pressure (see `Authority_And_Command_Hierarchies.md`),
- feeds compliance metrics (crew/village stability).

**Recurring error to avoid**: directly setting “evil” after one big event.

---

## Debuggability & Player Legibility

Design requirement:
- show top contributing events + stances (“why they hate me”),
- show which scope is judging (village vs faction vs crew),
- show whether judgment was direct witness or rumor.

Presentation can map dominant axes to:
- shaders/VFX, posture, environment tone shifts,
without making visuals the simulation truth source.

---

## Determinism + Rewind

Rules:
- mutate only when `RewindState.Mode == RewindMode.Record`,
- all events are tick-stamped, processed deterministically (stable ordering),
- use bounded buffers + deterministic eviction for history summaries.

---

## Implementation Order (MVP → deep)

### MVP0 (shared contracts)
1) Define `ActionToken` taxonomy + axis vectors (data-driven catalog).
2) Define `MoralEvent` + `EvidenceHandle` contracts.
3) Define `EthicsStanceTable` per culture/doctrine (blob).

### MVP1 (reputation-in-scope)
4) Process `MoralEvent` into `ReputationInScope` + `EthicsViolationCounters`.
5) Add minimal telemetry surfaces: top deltas per scope and violation counts.

### MVP2 (outlook drift)
6) Add habit accumulator + periodic drift with hysteresis.
7) Feed drift into refusal/mutiny pressure and authority legitimacy.

### MVP3 (comms/sensors integration)
8) Evidence creation from medium caches; rumor propagation with trust.

---

## Starter `ActionToken` taxonomy

`Deforest`, `Pollute`, `Donate`, `AidWeak`, `ExploitWorkers`, `Enslave`, `FreeCaptives`, `Execute`, `Torture`, `Rescue`, `Heal`, `Betray`, `DefendHome`, `ObeyOrder`, `DisobeyOrder`, `SacrificeSelf`, `DestroySacredSite`, `RestoreNature`.

Each token has:
- `AxisVector` (small int vector),
- default stance (culture can override),
- context rules (self-defense/retaliation/sanction modifiers).

