# Authority & Command Hierarchies (Villages + Ships)

**Status**: Draft (authoritative direction)  
**Category**: Core / AI / Governance  
**Applies To**: Godgame, Space4X, shared PureDOTS

---

## Purpose

Provide a shared, data-driven way to model **who decides** for aggregates (villages, ships, fleets), **how decisions are delegated**, and how outcomes stay flexible under stress (mutiny/coup/defection).

This is the macro-AI layer that:
- converts aggregate goals (“build housing”, “patrol perimeter”, “intercept target”) into **orders/tasks**, and
- pushes those tasks down to micro-AI actors (villagers, squads, pilots, strike craft).

Profile integration lives in `Docs/Concepts/Core/Entity_Profile_Schema.md`.

---

## Core Model (game-agnostic)

### Authority body
An aggregate has an **Authority Body** that issues orders:
- **Single-executive** (authoritarian): one leader approves; delegates advise/execute.
- **Council/quorum** (egalitarian): multiple seats vote/consent; slower but more stable.

Authority body is a *role structure* plus runtime seat occupancy.

### Implementation (PureDOTS runtime)
Authority is represented as **stable seat entities** owned by an aggregate **authority body**:
- `PureDOTS.Runtime.Authority.AuthorityBody` on the aggregate entity (village/ship/army/etc).
- `PureDOTS.Runtime.Authority.AuthoritySeat` on seat entities (role + domains + rights).
- `PureDOTS.Runtime.Authority.AuthoritySeatOccupant` on seat entities (current occupant entity; may change).
- `PureDOTS.Runtime.Authority.AuthorityDelegation` buffer on principal seat entities (principal → delegate edges).
- `PureDOTS.Runtime.Authority.AuthoritySeatRef` buffer on the body entity (seat roster).
- `PureDOTS.Systems.Agency.AuthoritySeatControlClaimBridgeSystem` converts occupied seats into `ControlClaim` entries on the body (feeds the agency kernel).

Orders/actions should be attributable to a **seat**, not just a person:
- Prefer fields like `IssuingAuthority` to reference the **seat entity** (stable identity).
- Capture occupant-at-issue-time separately (ex: `PureDOTS.Runtime.Authority.IssuedByAuthority`) for history/telemetry and mutiny/coup blame/legitimacy.

### Authority seats
A seat is a role with bounded responsibility:
- has an **occupant** (entity, may be empty),
- can have **delegation rules** (who can act when leader is away),
- produces **recommendations** or **decisions** depending on governance mode.

### Orders are always attributable
Every order has:
- issuer (authority seat + occupant),
- target (entity or position),
- scope (ship/village/fleet),
- intent (task type),
- legitimacy context (who had the right to issue it; important for mutiny/coup logic).

Existing Space4X pattern: `CaptainOrder` includes `IssuingAuthority`.

---

## Governance Mode (derived from profile)

Use ideology/ethics **Authority axis** (Authoritarian ↔ Egalitarian) and policy fields:
- High authoritarian → single executive + strict chain of command, high obedience expectations.
- High egalitarian → council/quorum, higher “consensus appetite”, lower tolerance for unilateral deviation.

This keeps “one authority vs multiple authorities” *data-driven*, not hardcoded per entity type.

### LOD & Governance Consistency

- LOD changes **cadence and fidelity**, not the governance rules or legitimacy checks.
- Offscreen/aggregate governance can collapse deliberation, but must still honor refusal/escalation/veto paths.
- Use `Docs/Concepts/Core/Simulation_LOD_And_Environment_Fields.md` for tiering guidance.

---

## Village Authority (Godgame-scale aggregates)

### Minimum viable seats
- **Ruler / Mayor** (executive): final approval on priorities, taxes/allocations, laws.
- **Steward** (civil works): construction queue, storage policy, workforce assignment.
- **Marshal** (security): patrol routes, guard postings, threat response.
- **Quartermaster** (supplies): rationing, stockpile thresholds, trade/requests.

### Behavior loop (macro)
1. Collect signals: threats, shortages, pending work orders, morale, doctrine constraints.
2. Generate candidate actions: build X, repair Y, patrol Z, expand A.
3. Evaluate with policy (risk, obedience, consensus, scarcity tolerance).
4. Approve (mayor-only) or vote (council).
5. Emit tasks/orders to micro actors (villagers, squads).

---

## Ship Authority (Space4X-scale aggregates)

### Minimum viable seats (capital ships / carriers)
Command:
- **Captain (CO)**: strategic intent + final say.
- **Executive Officer (XO)**: second-in-command; manages discipline, readiness; acting CO if captain incapacitated.
- **Shipmaster (Chief Mate / First Officer)**: runs day-to-day routines of command when captain is away; keeps schedules, drills, routine operations moving.
- Optional (faction/culture dependent):
  - **Commissar / Overseer / Political Officer**: ideological enforcement, loyalty audits, discipline escalation; can constrain or override command depending on doctrine.
  - **Fleet Admiral / Task Force Commander**: fleet-level posture, interdiction policy, coalition doctrine when the ship is the flagship.

Combat and awareness:
- **Tactical / Weapons Officer**: targeting doctrine, weapons employment, rules of engagement.
- **Sensors / Intel Officer**: detection posture, threat evaluation, contact classification, EW posture.

Navigation & communications:
- **Navigation / Helm Officer**: course plotting, maneuver doctrine, formation spacing.
- **Communications Officer**: comms discipline, broadcast posture, IFF policy, escalation channels.

Operations and sustainment:
- **Chief Engineer**: propulsion/power priorities, repair triage, damage control posture.
- **Logistics Officer / Quartermaster**: supplies, munitions, cargo priorities, resupply decisions.

Security & boarding:
- **Security Officer**: internal security, custody posture, boarding defense.
- **Marine Commander / Sergeant**: boarding readiness, assault doctrine, squad-level execution.

Flight operations (carriers):
- **Air Wing Commander (“CAG”) / Flight Operations Officer**: overall flight plan, sortie priorities.
- Split (optional, carrier scale):
  - **Combat Flight Ops**: strike/cap/intercept.
  - **Non-combat Flight Ops**: mining, courier, rescue, logistics sorties.
- **Hangar/Deck Officer (“Air Boss” / “Hangar Master”)**: deck cycle, launch/recovery throughput, hangar safety posture.
- **Flight Director**: air-traffic control, comms cadence, deck timing (may be merged with hangar/deck officer on smaller carriers).

### “Pilot master” naming
If you want one role that owns pilot standards and assignments:
- **Air Wing Commander (CAG)** if carrier-centric.
- **Chief Pilot** if smaller-scale / civilian.
- **Flight Commander** if neutral sci-fi tone.

Recommendation: use **Flight Commander** as the generic name, and allow “CAG/Air Boss” as flavor titles per culture/faction.
If you want separate seats for command vs deck control, pair **Flight Commander** (strategy/sorties) with **Flight Director** (deck/traffic).

### Micro/macro handoff (ship → craft)
- Captain/authority chooses intents (attack, escort, mine, retreat).
- Flight/hangar seats translate intents into sorties and pilot assignments.
- Pilots (named entities) execute; crew aggregates affect throughput, reliability, and compliance pressure.

---

## Officer Refusal, Veto, and Escalation (domain safety + legitimacy)

Some seats are not “order clerks”; they are **domain authorities** who can block or slow execution when:
- **morale/cohesion** is too low to safely execute,
- the action violates **ROE** expectations (legitimacy, target classification, escalation rules),
- the action is high-risk relative to current readiness (damage state, supply state, threat context).

### Examples (ship)
- **Weapons/Tactical**: may refuse to execute friendly-fire or “unjustified” engagement; may demand confirmation thresholds (IFF, hostile intent).
- **Sensors/Intel**: may refuse “shoot first” when target classification confidence is low; may recommend shadow/track instead.
- **Chief Engineer**: may refuse maneuvers that risk reactor breach or loss of life support; can force “safety lockouts”.
- **Hangar/Deck**: may refuse flight ops when deck safety or sortie tempo would be catastrophic under current fatigue.

### Examples (village)
- **Marshal**: may refuse suicidal patrols when cohesion/morale is collapsing; may demand fortification first.
- **Quartermaster**: may refuse allocations that push stores below survival floors; may demand rationing instead.

### What refusal produces (deterministic outcomes)
Refusal is an order-pipeline result, not freeform narrative:
- **Reject**: order fails with a reason token (e.g., `roe.blocked`, `friendly.fire.inhibited`, `cohesion.too.low`).
- **Delay**: order is postponed (cooldown) until conditions improve.
- **Escalate**: generates an escalation request to XO/captain/council for explicit override.

Refusal should also feed telemetry:
- `event.order_refused` with small structured reasons
- metrics for `policy.roe_strictness`, `policy.friendly_fire_inhibition`, `morale`, `cohesion`

### Renegade captain vs friendly ships (flexible but “hard”)
A renegade captain (or defecting chain of command) should still struggle to open fire on friendlies when:
- crew has high **FriendlyFireInhibition** and/or high **ROEStrictness**
- ship identity/affiliation is still strong (loyalist-majority, high cohesion)
- the target is still classified as allied (clear IFF, shared fleet tags)

This is not a hard prohibition; it is a strong utility penalty + high refusal likelihood that can be overcome by:
- extreme ideology mismatch/hostility, very low moral alignment, or sustained grievances
- a charismatic extremist weapons officer leading the break
- scenario constraints explicitly enabling betrayal

---

## Enforcement & Discipline (responses to refusal)

Refusal is not the end of the story; it creates a **governance incident** that the authority body responds to.

### Discretion flags (who is allowed to do what)
Not every entity can decide “decimation” or “mass airlock” just because it is a possible outcome in the simulation.

Rule: **extreme enforcement actions require explicit discretionary powers on the acting authority occupant** (captain/overseer/officer). Aggregates do not spontaneously “choose atrocities” without a seat occupant who:
- has the relevant discretionary flags, and
- is currently the legitimate issuer (or has seized legitimacy via coup/mutiny outcome).

This lets scenarios express:
- cynical/extreme captains and overseers who can go there,
- reasonable captains/officers who simply *cannot* authorize it (even if stressed),
- “scrutiny of radicals” as an internal governance problem (see below).

Responses should be expressed as a bounded, deterministic ladder, driven by Profile→Policy:
- **Clarify / Re-issue**: provide legitimacy tokens, tighter ROE, more intel (often by Sensors/Intel + XO).
- **Negotiate / Compromise**: modify intent (“track, don’t fire”), offer concessions, reduce risk, reassign duties.
- **Escalate / Vote**: bring to council/quorum, request higher authority, or formally record dissent.
- **Coerce**: threats, confinement, pay/leave removal, demotion, reassignment, loyalty audits.
- **Punish**: imprisonment, forced labor, public example-making; may be targeted (leaders) or broad (collective punishment).
- **Purge / Atrocity** *(rare, extreme, scenario-scaled)*: e.g., mass execution/airlocking, decimation, violent suppression.

Important: the ladder is **not a menu of “free” actions**. Every response produces downstream effects:
- morale/cohesion shocks (short-term compliance vs long-term stability),
- legitimacy/reputation impacts (inside and outside the group),
- increased future mutiny/defection probability,
- triggers for Conflict Resolution / rebellion models.

### Villages/armies: decimation and terror as policy
Authoritarian/militarist cultures may tolerate:
- **decimation** (punish a fraction to scare the rest),
- **collective punishment** (ration cuts, forced labor),
- **exemplary executions**.

Egalitarian/peaceful cultures tend to prefer:
- negotiation, mediation, and procedural legitimacy (votes, trials, due process).

### Ship: cynical/extreme captains vs reasonable officers
Two common patterns that should be supported:
- **Cynical/extreme command**: uses harsh discipline to force compliance; can temporarily stabilize execution but raises mutiny pressure and reputation costs.
- **Reasonable/peaceful command**: attempts negotiation, de-escalation, and legitimacy-building; slower, but reduces long-term fracture risk.

Scenario constraints can bias or force certain responses (e.g., “must succeed coup”, “must avoid civilian casualties”).

### Scrutiny of radical elements (aggregates aren’t monolithic)
Both ships and villages should support internal scrutiny: “how many radicals are inside this aggregate, and who are the catalysts?”

Minimum requirement:
- represent the aggregate’s internal split (loyalist / rebel / neutral, plus “extremist tail”),
- allow authority seats (esp. Overseer/Commissar, XO, Marshal) to run audits and act on the results.

Scrutiny should feed into:
- targeted negotiation (address grievances, replace officers, reduce risk),
- targeted coercion/punishment (remove ringleaders),
- or escalation into coup/mutiny outcomes when radicals gain control of key seats.

---

## Mutiny / Coup / Defection (flexible outcomes)

### Why it happens
Mutiny is a pressure-release when:
- orders repeatedly violate ideology/values (profile mismatch),
- legitimacy is low (unlawful chain of command, perceived betrayal),
- morale/stress/fatigue is high,
- “named leaders” catalyze factions (charismatic XO, extremist weapons officer),
- faction doctrine says “this is unacceptable”.

### Representing sides (compact)
Use a 3-way split as a baseline:
- **Loyalists** (support the parent faction/authority)
- **Renegades / Rebels** (support breaking orders / replacing authority)
- **Neutrals** (delay/avoid; can be forced to choose)

This mirrors the existing Space4X rebellion conceptual model and keeps outcomes explainable.

### Outcome resolver (examples)
Outcomes depend on *who* is rebelling against *whom* and why:
- **Loyalist mutiny**: crew stays in faction; captain is removed/replaced; ship remains loyal.
- **Renegade captain**: captain (plus some officers/craft) defects; ship leaves faction or becomes independent.
- **Split**: ship fractures (detach craft, split fleet membership, or temporary schism until conflict resolves).
- **Suppressed**: rebellion fails; punishments/reassignments; morale shocks; future risk changes.

Scenario constraints can bias or hard-constrain available outcomes (e.g., scripted betrayal must succeed).

### Authority succession rules (important for determinism)
Define deterministic seat succession order:
1. XO
2. Shipmaster
3. (next ranked officer by legitimacy/loyalty)

Village analog: steward → marshal → quartermaster (or culture-specific).

---

## Backlog Strategy (how to implement without boiling the ocean)

Build in proof-driven vertical slices:
1. **MVP authority wiring**: seats exist + occupancy + “who issued this order” is always known.
2. **Policy-derived governance mode**: authoritarian vs egalitarian changes approval path (leader vs council).
3. **One slice per domain**:
   - village: construction priority + patrol assignment
   - ship: captain intent → flight ops sortie assignment
4. **Mutiny slice**: pressure accumulation + one resolved outcome path + telemetry proof.

Each slice must add:
- a headless proof line, and
- at least one telemetry metric/event that confirms the slice completed.
