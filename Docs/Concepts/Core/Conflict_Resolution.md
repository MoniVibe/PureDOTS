# Conflict Resolution System

**Status**: Locked v0.1  
**Category**: Core / Social / Politics  
**Related Systems**: Relations, Communication, Forces, Reputation, Miscommunication, Memory  
**Applies To**: Godgame, Space4x, shared PureDOTS social/politics layer

---

## Purpose

Provide a **moddable, scalable** framework for conflicts between **any entities** (individuals, groups, factions, assets, places, concepts), supporting:

- **Implicit → explicit** conflict progression (grievances/raids → formal conflict → war).
- **Non-violent resolution** (negotiation, mediation, arbitration) as first-class outcomes.
- **Escalation / de-escalation** driven by *events*, not ticking loops.
- **Multi-party conflicts** (up to ~100 parties) via **coalitions/sides** and **representatives**.
- **Hidden information**, deception, miscommunication, sabotage.
- **Aftermath** artifacts (treaties, laws, claims, annexations, peace tokens) and persistent **memory** with culturally-driven decay.
- **Off-screen compressed resolution** with a durable **log**, producing the same world consequences.

---

## Core Design Invariants (performance + determinism)

1. **Event-driven evaluation only**  
   Conflicts do not update on a global heartbeat. They progress only when **something happens**:
   - a message is sent/received (Communication),
   - an incident occurs (raids, sanctions, insults, miracles, intel, leadership change, scarcity shock, etc.),
   - side composition changes (merge/split, defection, coup),
   - a treaty clause verification/breach event fires.

2. **Unbounded existence, bounded processing**  
   The world may contain many conflicts, but only conflicts receiving events consume CPU.

3. **Talks happen between sides via councils**  
   Multi-party conflicts are represented by dynamic **sides/coalitions**. Negotiation is performed by a small **council** per side, not by all parties pairwise.

4. **Preferences only (no hard constraints)**  
   No rule forbids an outcome. Behavior profiles, outlooks, culture, and goals produce **utilities** that can make choices *practically impossible* without forbidding them.

5. **Everything leaves a trail**  
   Every meaningful decision produces **reason tokens** and a **conflict log entry** so the simulation remains explainable and replayable at the story level.

---

## Glossary

- **Party**: Any entity that can be in conflict (including assets like buildings/animals/trees).
- **Representative (Rep)**: The negotiator/commander/ambassador speaking/acting for a party/side.
- **Side / Coalition**: A grouping of parties aligned within a conflict.
- **Incident**: Any event that changes pressure, stance, goals, legitimacy, or side composition.
- **Proposal**: A structured communication containing terms and optional deception/omissions.
- **Treaty / Verdict**: A structured outcome with terms, verification, enforcement hooks, and breach consequences.
- **Aftermath Artifact**: Any persistent record/token/law/claim created by resolution.

---

## Parties and Representation (supports "war on a bee")

### Party Model
A party can be:
- individual, group, faction, fleet, settlement, institution,
- asset (building, animal, ship, relic),
- place/region,
- abstract target (concept) if a mod defines meaning.

### Default Representation Rule
- **Owned assets are represented by their owner** (buildings, animals, etc.).

### Agency Adapter (required for full generality)
Each party exposes (or is mapped to) an adapter:
- **CanNegotiate** (sentient/institution/AI-capable?)
- **HasOwner / OwnerRef** (for representation)
- **HasProxy** (steward/guardian/handler if needed)
- **CanEnforce** (access to forces/sanctions)
- **CanPerceive / CanRemember** (for memory and misinformation impacts)

This keeps "anything can be a party" without requiring every entity type to implement diplomacy.

---

## Conflict Entity (conceptual data model)

A conflict is a first-class record/entity with:

### Identity
- `ConflictId`
- `Scope` (region/system/settlement) and optional `Theater` tags

### Membership
- `Parties: PartyRef[]` (up to ~100)
- `Sides: SideRef[]` (dynamic coalitions)
- `PartyToSide` mapping
- `Representatives` (per party and per side council)

### State Machine
- `State`: Latent | Tension | Declared | Talks | Mediation | Arbitration | Coercion | Skirmish | War | Stalemate | Resolved | Aftermath

### Intent + Context
- `WarGoals` per party/side (**typed goals + scripted goals**)
- `Stance` per side (minimal bounded vector, see below)
- `Pressure` scalars (escalation vs de-escalation forces)
- `LegitimacyContext` (institutions/norms present)

### Communication
- Active comms channels, reliability, bandwidth, sabotage risk
- Proposal inbox/outbox queues (event-driven)

### Persistence
- Conflict log ledger (append-only)
- Active treaty/verdict artifacts linked

---

## Minimal Stance Vector (bounded, extensible)

**Core stance fields** (small, cache-friendly):
- Hostility (harm vs reconcile)
- Trust (deal reliability expectation)
- Threat/Fear (perceived danger)
- Legitimacy (belief in institutions/mediators/verdicts)
- Fatigue (tolerance for continued cost)

Mods may add additional stance fields, but the core stays minimal.

---

## Profiles, Outlooks, Culture (preferences only)

### Behavior Profile Axes (examples)
- Peaceful ↔ Warlike
- Honest ↔ Deceptive
- Forgiving ↔ Vengeful
- Lawful ↔ Chaotic
- Risk-averse ↔ Risk-seeking
- Honor/Pride sensitivity
- Reputation sensitivity
- Patience / delay preference
- Verification appetite

### Outlook / Doctrine
Group-level preferences (strategic culture, ideology, norms, command style, honor codes).

### Culture
Baseline weights and decay patterns (how grudges fade; how breaking treaties is punished socially).

**No constraints**: profiles/outlooks/culture only influence **utility**, never hard-block options.

---

## War Goals (typed + scripted)

### Typed Goal Examples
- Annex / Claim / Control (territory, asset, region)
- Punish / Reparations
- Destroy / Remove (entity, institution)
- Tribute / Access / Trade terms
- Convert / Ideological alignment
- Contain / Demilitarize / Buffer
- Humiliate / Reputation damage
- Regime change / Leadership removal
- Deterrence / Show of force
- Liberation / Protection guarantee

### Scripted Goals
Mods can define goal scoring functions that:
- evaluate a proposal/outcome within context,
- optionally emit additional "story tags" for logging.

---

## Negotiation (communication-based, event-driven)

### Proposal Structure
A proposal is a message with:
- sender/receiver reps or councils
- terms (structured list)
- optional public/private visibility
- expiry / conditions / ratification requirements
- optional deception: omission, framing, selective disclosure, forged terms (via sabotage mechanics)

### Negotiation Processing Rule
Negotiation progresses only when:
- a proposal is received,
- an incident changes payoffs,
- a verification/breach event triggers.

### Utility-Based Choice (preferences only)
Every decision is:
1) **Generate** bounded candidate options.
2) **Score** each option with utility:
   - stance + profile/outlook weights
   - relationship/reputation impacts
   - threat and cost expectations (including Forces outcomes)
   - war goal satisfaction (typed + scripted)
   - verification burden
   - betrayal risk and detection risk (if deceptive)
   - commitment/inertia (prevents flip-flop)
3) **Select** using deterministic argmax + optional low-noise variation (see Defaults).

### Negotiation Outcomes
- accept, counter, delay, walk away, deceive, undermine, threaten, disclose intel, request mediator, propose duel, propose ceasefire, propose arbitration, etc.

---

## Miscommunication & Sabotage (first-class)

Communication is a pipeline:
- encode → transmit → receive → interpret → update beliefs/stance

Miscommunication/sabotage can:
- drop/delay messages
- distort terms (mild or severe)
- leak to third parties
- impersonate / forge
- selectively reveal information

Beliefs diverge from objective reality; the log records both:
- **objective artifact** (what is actually signed)
- **perceived artifact** (what each side thinks is signed)

---

## Mediation & Arbitration (legitimacy + corruption)

### Mediator Selection
Third parties may volunteer or be requested; selection is utility-driven:
- reputation, relations, perceived fairness
- enforcement capacity
- shared norms/institutions
- access to evidence/info
- mediator profile (bias, corruption propensity)

### Corruption / Bribery
A feature: bribes shift mediator utilities and may risk exposure.
Exposure becomes an incident (reputation hit, escalation trigger, side split trigger).

### Arbitration "Binding"
Binding is emergent via enforcement hooks:
- relations penalties for breach
- reputation penalties
- institutional sanctions
- third-party retaliation triggers
- internal dissent triggers (coup, defection, legitimacy collapse)

---

## Escalation & De-escalation (anything can trigger)

### Escalation Chain (reference)
Tension → Talks → Coercion → Skirmish → War → Stalemate → Talks/Resolution

### Triggers
Any incident can increase or reduce:
- hostility, threat, fatigue, legitimacy, trust

Profiles/outlooks determine:
- which triggers are weighted more,
- how quickly sides escalate,
- what de-escalation offers are acceptable.

---

## Conflict Merge / Split (grievance → total war)

### Merge
Conflicts may merge when:
- parties overlap,
- theaters overlap,
- incidents link them (retaliation chains, alliance obligations),
- side formation produces a single coherent coalition structure.

### Split
Conflicts may split when:
- coalition cohesion drops,
- war goals diverge,
- leadership changes,
- bribes/corruption events fracture legitimacy.

---

## Aftermath (artifacts, memory, decay)

### Outcome Artifacts (anything)
Treaties, laws, claims, annexations, peace tokens, cold wars, truces, vows, normalcy declarations, certificates, etc.

Each artifact includes:
- terms
- verification clauses
- enforcement hooks
- breach consequences
- visibility (public/secret)

### Memory Recording
Always record to an **event ledger**.
Optionally inject into entity memory based on salience:
- bloodiness, duration, scale affected
- proximity/relationship to parties
- cultural relevance and identity importance

### Decay
Grudges/treaty resentment decay via:
- culture baseline
- forgiving/vengeful axis
- reinforcement events (propaganda, renewed raids, anniversaries, rumor incidents)

---

## Prevention (reactive default, injectable events allowed)

Primary prevention mechanisms (all data-driven):
- envoys, ambassadors, arbiters
- rituals/meditations
- institutions/courts/elders
- resource buffers and scarcity mitigation
- forecasting: "pressure rising" ambient emitters
- shared projects and interdependence
- norms/laws that reduce escalation sensitivity

Conflicts are mostly **reactive**, but events/triggers may **inject** disputes for story and systemic shocks.

---

## Off-Screen Compressed Resolution (with real consequences)

When conflicts are off-screen or outside attention:
- resolve through summarized utilities and aggregate forces outcomes,
- still generate:
  - the same artifact changes (treaties/claims/laws),
  - the same relationship/reputation shifts,
  - the same memory/log events.

When the player focuses/zooms in:
- materialize individuals and micro-events consistent with the ledger (narrative continuity).

---

## Integration Contracts (what other systems must provide)

- **Relations**: relationship scores, grievances, affinity, alliance obligations, cohesion metrics.
- **Communication**: message routes, bandwidth, reliability, delay, encryption, interception.
- **Miscommunication**: distortion, impersonation, forging, leakage; detection events.
- **Forces**: projected outcomes/costs for coercion/skirmish/war options (can be abstract).
- **Reputation**: global/local reputation impacts; legitimacy effects.
- **Memory**: event ledger + salience-based per-entity memory storage; decay parameters.

---

## Engine Defaults (chosen for your scope)

These are defaults; mods/outlooks can override weights and generators.

### Council Size (per side)
- Default council size: **3** (leader/commander + 2 advisors).  
  **Reason**: keeps negotiation bounded while supporting "elite negotiations".

### Option Budgets (per event)
- Coalition adjustments: up to **8** candidate join/leave/reform options.
- Proposal generation: up to **16** candidate proposals/counters.
- Escalation/de-escalation: up to **8** candidate actions.

### Selection Policy
- Deterministic argmax by default.
- Optional low-noise variation controlled by profile trait `Volatility` (0 = deterministic).

### Ambient "Vibe" Without Heartbeats
- Use **ambient emitters** that produce discrete incidents when world state changes enough:
  scarcity spikes, border tensions, rumor waves, ideological shifts.

### Explainability
- Every chosen action emits `ReasonTokens[]` (short tags) into the conflict log.

---

## Deliverables Produced by the System

- Conflict log ledger (append-only).
- Proposals exchanged (with perception variants where miscommunication applies).
- Treaties/verdicts/peace artifacts.
- Claims/laws/ownership changes (where relevant).
- Relationship and reputation deltas.
- Memory injections and decay progression.

---

**Last Updated**: 2025-12-20  
**Status**: Locked v0.1
