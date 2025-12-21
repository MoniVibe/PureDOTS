# Agency & Sentience (Control Contests)

**Status**: Draft (authoritative direction)  
**Category**: Core / AI / Governance  
**Applies To**: Godgame, Space4X, shared PureDOTS

---

## Purpose

Define a shared, game-agnostic **agency kernel** that supports:
- micro AI (villagers, captains, officers, pilots),
- macro AI (villages, ships, fleets, factions),
- flexible scenarios (mutiny, loyalist vs renegade outcomes, domination and reversal),
- rewind/time manipulation.

Core rule: entities are **blank-by-default**. “Agency” is a **module**, not a type.

---

## High-level model (layered arbitration)

This is the ordering we aim for when deciding “what happens”:
1) **Who is in control** (per domain).
2) **Resistance vs pressure** (contest; no hard veto).
3) **Controller needs** (must do).
4) **Controller wants** (would like to do).
5) **Self needs** (body-mind survival floors).
6) **What is available/feasible** (capabilities + world affordances).

The kernel starts with (1) and (2) and provides scaffolding for the later layers.

---

## Domains (independent control lanes)

Control is not always “all-or-nothing”. It is contested per domain.

Implemented as a bitmask:
- `PureDOTS.Runtime.Agency.AgencyDomain`

Minimum shared domains include: `SelfBody`, `Movement`, `Work`, `Combat`, `Communications`, with optional ops/governance domains (e.g., `Sensors`, `Logistics`, `FlightOps`, `Governance`).

---

## Contest model (no hard veto)

We intentionally avoid hard prohibitions:
- **Domination is possible** when pressure is high enough.
- **Reversal is possible** when resistance re-asserts over time (future “subversion” layer).
- **Willing submission** is possible (hive mind / devotion scenarios).

Current minimal contest is:
- **Pressure** (controller force/authority + legitimacy)
- vs **Resistance** (self autonomy + self-need urgency amplified by hostile control)
- reduced by **Consent / submission** and global **DominationAffinity**

This yields a per-domain winner: self-control or a controlling entity.

---

## Implementation (v0 scaffolding)

Components/buffers:
- `PureDOTS.Runtime.Agency.AgencySelf` (baseline autonomy)
- `PureDOTS.Runtime.Agency.ControlLink` (controller edges + parameters)
- `PureDOTS.Runtime.Agency.ResolvedControl` (derived winner per domain; `Entity.Null` = self)

Opt-in module tag:
- `PureDOTS.Runtime.Modularity.AgencyModuleTag`

Bootstrap system (adds required components/buffers):
- `puredots/Packages/com.moni.puredots/Runtime/Systems/Modularity/AgencyModuleBootstrapSystem.cs`

Resolver system (derives per-domain winners):
- `puredots/Packages/com.moni.puredots/Runtime/Systems/Agency/AgencyControlResolutionSystem.cs`

---

## Rewind compatibility

Rule: systems that mutate gameplay state must not run during rewind playback.

Agency systems:
- skip mutation unless `RewindState.Mode == RewindMode.Record` (alias for Play).

Longer term:
- persistent agency state that matters for rewind should be either:
  - **Derived** deterministically from recorded state, or
  - explicitly included in history/snapshot tracking.

---

## How this plugs into governance (next slices)

Governance systems (ships/villages) should:
- write `ControlLink` edges (captain/officer/overseer → ship/crew; mayor/delegates → village),
- emit structured orders and refusal outcomes (see `Authority_And_Command_Hierarchies.md`),
- use Profile→Policy values (see `Entity_Profile_Schema.md`) to drive:
  - obedience/refusal likelihood,
  - ROE/friendly-fire inhibition,
  - discipline ladder and discretionary powers,
  - mutiny/coup/defection branching.

This keeps “who controls what” generic, while allowing rich faction/culture-specific outcomes.

