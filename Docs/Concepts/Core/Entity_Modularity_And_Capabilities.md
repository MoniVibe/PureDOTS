# Entity Modularity & Capabilities (Blank-by-Default)

**Status**: Draft (authoritative direction)  
**Category**: Core / Architecture  
**Applies To**: Godgame, Space4X, shared PureDOTS

---

## Thesis

Entities are **blank by default**: they have no behaviors and no presentation unless modules are explicitly attached.

Everything is allowed to become “something” by composition:
- sentient buildings,
- items with relationships,
- abstract concepts as parties in conflict,
- ships whose “crew mass” is an aggregate entity,
- villages as aggregates with councils, coups, and culture.

PureDOTS should never bake in “only X can do Y” as a type rule. Instead: **capabilities**.

---

## The Rule: Capabilities, not Types

When implementing systems, do not ask “what is this entity?”  
Ask “what capabilities does this entity currently have?”

In ECS terms:
- A capability is the presence of a **component set** (and optional buffers) that forms an adapter contract.
- Systems must query only on their required components, and treat missing capabilities as “not applicable”.

This ensures any entity can opt in by attaching the module.

---

## Modules (how we build behavior/presentation)

### A “module” is:
- a small set of components/buffers,
- one or more systems that operate only when those components exist,
- optional authoring/scenario hooks to attach/remove the module,
- telemetry/proof hooks that validate the module in headless runs.

### Modules should be independent
Avoid implicit dependencies on “Villager” or “Ship” concepts. Instead:
- document required components (“adapter contract”),
- gate systems using queries (`WithAll<...>`, `RequireForUpdate<...>`),
- use explicit linking components when a module needs a reference (e.g., OwnerRef, SettlementRef, PlatformRef).

---

## Minimal “Blank Entity” Baseline

There is no required baseline besides existence. If an entity has:
- no profile,
- no needs,
- no agency,
- no presentation,

then nothing in simulation should touch it.

If a feature needs stable identity (save/load, narrative), identity should be a **module**, not a default.

---

## Common Capability Adapters (examples)

### Agency / Sentience
“Can decide and act” should be an adapter (module), not an assumption.

Typical requirements might include:
- alignment/outlook/personality (optional),
- initiative/decision state (optional),
- goal/plan or utility bindings (optional),
- communication capability (optional).

An entity without agency can still participate via proxies/owners (see below).

### Ownership / Representation
Many systems need to answer “who represents this thing?” (buildings, items, animals, relics).

Preferred pattern:
- entity may have `OwnerRef` (points to the representing/controlling entity),
- or `ProxyRef` (steward/handler/AI),
- systems resolve representation through these links rather than hardcoding “buildings are owned by villages”.

This matches the “Agency Adapter” approach in `Docs/Concepts/Core/Conflict_Resolution.md`.

### Presentation
Presentation is always a module.
- No system should assume a mesh/animator exists.
- Presentation contracts should be explicit (RenderSemanticKey/variant keys, presenter tags, etc.).

---

## “Anything Can Be Sentient” (intended outcomes)

### Sentient building (example)
A building becomes sentient if you attach the same profile/agency modules you’d attach to a person:
- alignment/ideology/behavior modules,
- agency/decision modules,
- communication modules,
and optionally a presentation module (if it should emote/animate).

No system should special-case “buildings can’t have grudges”.

### Items with relationships (example)
Items are entities. They can have:
- relationships (buffers linking to other entities),
- loyalty/ownership links,
- memory/history links,
- even agency (cursed artifact, living sword) if you attach the module.

---

## Implementation Guidance for Agents

When adding new gameplay:
1. Define the adapter contract (required components).
2. Make the system query only on that contract.
3. Do not assume “entity type”; avoid hardcoded name-based checks.
4. Add telemetry/proof so the module can be validated in headless.
5. Keep the module detachable (adding/removing components via ECB should produce sensible state transitions).

### Starter module tags (implemented)
PureDOTS includes opt-in “single tag” modules that bootstrap required components:
- `PureDOTS.Runtime.Modularity.NeedsModuleTag` → adds needs scaffolding (`EntityNeeds` or `NeedEntry`, `NeedsActivityState`, `NeedCriticalEvent`)
- `PureDOTS.Runtime.Modularity.RelationsModuleTag` → adds relationship scaffolding (`PersonalRelation` buffer)
- `PureDOTS.Runtime.Modularity.ProfileModuleTag` → adds profile scaffolding (`AlignmentTriplet`, `PersonalityAxes`, `MoraleState`, `BehaviorTuning`)
- `PureDOTS.Runtime.Modularity.AgencyModuleTag` → adds agency scaffolding (`AgencySelf`, `ControlLink` buffer, `ResolvedControl` buffer)

These are intended as the baseline pattern for future modules: **blank entity + one opt-in tag + bootstrap system**.

Demo scenario:
- `puredots/Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples/modularity_blank_entities_demo.json`

Recommended template:
- `Docs/Module_Recipe_Template.md`

---

## Related Docs
- `Docs/Concepts/Entity_Agnostic_Design.md`
- `Docs/Concepts/Core/Agency_And_Sentience.md`
- `Docs/Concepts/Core/Entity_Profile_Schema.md`
- `Docs/Concepts/Core/Authority_And_Command_Hierarchies.md`
- `Docs/Concepts/Core/Conflict_Resolution.md`
