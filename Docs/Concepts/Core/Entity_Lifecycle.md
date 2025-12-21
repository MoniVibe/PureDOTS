# Entity Lifecycle System

**Status**: Locked (v0.2)  
**Category**: Core / Simulation / Entities  
**Library Target**: PureDOTS (game-agnostic)  
**Applies To**: Godgame + Space4x (shared kernel with policy-driven rules)

---

## Purpose

Defines the engine-agnostic lifecycle of an entity across:
- **Creation** (birth / manufacture / summoning / migration / promotion from abstract membership)
- **Aging** (continuous age value + stage thresholds)
- **Death** (multi-outcome: corpse/bones, soul, neural stack, estate case)
- **Post-death** (decay, spirits/undead, inheritance/disputes, looting, role replacement via logistics)
- **Resurrection** (quality-based, supports multiple anchors: body parts, bones, soul, stack)

This system is **policy-driven**: the kernel exposes stable events + state containers; game-specific meaning comes from data assets (culture/tech/magic/doctrine rules).

---

## Design Goals

- **Individuality with scale**: identity persists even when embodiments are abstracted or despawned.
- **Flexible outcomes**: "anything can happen" is achieved by composing policies, not hardcoded branches.
- **Readable causality**: every lifecycle transition emits events; other systems react without tight coupling.
- **Supports both projects**:
  - Godgame: spirits/undead, miracles, priests/necromancers/exorcism loops, lineage stories.
  - Space4x: crews can be **abstract members with pseudo-identity** and be **promoted** into full characters (and later, full entities).

---

## Non-goals (owned by game side or other core modules)

- UI/UX, presentation, audio/visual feedback.
- Content balancing (exact costs, rarity, tuning values).
- Full "information/rumor spread" simulation (this module only provides hooks/events).
- High-level AI (goals/tasks) beyond emitting role vacancies and lifecycle events.

---

## Kernel Invariants (never violated)

1. **Identity is persistent**
   - An entity's **IdentityRecord** is never reused.
   - Identity can outlive embodiments (body destroyed, soul expires, etc.).

2. **Embodiment is optional**
   - A single identity may have: an alive body, a corpse/bones, a manifested spirit/undead, or no embodiment at all.

3. **Lifecycle is event-first**
   - Lifecycle transitions emit events; cross-system effects are performed by subscribers.

4. **Rules are policies**
   - Culture/tech/magic-specific rules are authored as policy assets. The kernel only orchestrates.

---

## Conceptual Data Model (engine-agnostic)

### Identity (always present)
- **IdentityRecord**
  - `IdentityId` (stable)
  - `NameSeed`
  - `FactionId`
  - `MainTraits` (small set)
  - `Lineage` (direct links for 10 generations; deeper via `GenealogyArchiveHandle`)
  - `RelationSetRef`
  - `MemoryLedgerRef`
  - `EstateCaseRef?` (optional, opened on death)
  - `AnchorRefs` (Soul / Stack / Remains)

### Embodiment (may exist or not)
- **EmbodimentState**
  - `Alive | Corpse | Bones | SpiritManifested | Undead | Despawned`
- **FidelityTier**
  - `AbstractMember | EmbodiedLow | EmbodiedFull`
  - Used to gate expensive per-entity state (anatomy, dense relations/memories, etc.).

### Anatomy (RimWorld-like detail, gated by fidelity)
- **AnatomyTemplateRef** (shared)
  - Part graph, dependencies, tags (vital / locomotion / perception / manipulation), implant slots.
- **AnatomyInstance** (per-embodiment, only when `FidelityTier >= EmbodiedFull`)
  - Per-part health/damage, conditions, implants/grafts.

### Anchors (bind identity across death/despawn)
- **SoulRecord**
  - `Free | Stored | Bound | Consumed | Expired`
  - Optional: `Manifested` (ghost behavior profile is separate data)
- **NeuralStack**
  - Itemized anchor entity; may be moved/stolen/traded/destroyed.
  - Supports duplication at very high tech tiers.
- **Remains**
  - Corpse entity container, then bones/fragments; bones persist until destroyed.

### Estate / Bank
- **BankEntity** (physical, raidable)
  - Storage buffers (physical items) + registry ledger (ownership/claims)
- **EstateCase**
  - Opened on death; holds claim/dispute state until resolved.

### Relations / Sentiment / Memory (integration surfaces)
- **RelationSet**
  - Strong ties are bounded (default **150**, policy override allowed).
  - Stores edge type, strength, last-update marker.
- **GroupSentiment**
  - Settlement/faction/empire-level mourning/resentment/etc.
- **MemoryLedger**
  - Event records for notable deaths/actions.

---

## Defaults (locked for current project scope, but policy-overridable)

### Relations capacity and eviction
- **Default strong-tie capacity**: **150**.
- **Eviction rule**: replace lowest-priority / weakest edge within a category first.
- **Reserved categories (default)**:
  - Family/Household (never evicted by casual ties)
  - Duty/Bonds (crew, squad, liege, sworn ties)
  - Rivalry/Threat (persistent enemies, grudges)
  - General (evictable)
> Exact reserved slot counts are policy values. The kernel only enforces capacity + eviction strategy.

### Identity & clones/forks (engine consistency rule)
- **Invariant**: one IdentityId maps to one "continuity of personhood."
- **Duplication** (neural stacks) **always produces a new IdentityId**, then marks it as:
  - `CloneOf` (identical snapshot at creation)
  - or `ForkOf` (derivative snapshot with intentional divergence)
This keeps ownership, relations, inheritance, and reputation consistent while still allowing "true clones" as content.

### Anatomy fidelity gating
- `FidelityTier == AbstractMember`:
  - No per-organ anatomy instance; use coarse health/condition summaries.
- Promotion to `EmbodiedFull` instantiates full per-organ anatomy from template + carried summary state.

### Overkill / body-destruction trigger (default)
- Overkill/despawn is determined by policy, with a default rule:
  - critical damage to vital regions (head/brain, torso/core) triggers `EmbodimentState = Despawned`.
- Identity persists; anchors may still persist based on death outcome policy.

### Corpse decay
- Corpse transitions to bones/fragments; bones persist and can be destroyed.

### Soul persistence and storage
- Souls can be stored (containers/structures/items) and may be:
  - resurrected,
  - consumed via soul magic,
  - ripped from living targets,
  - or expire via decay policy.
- Soul decay is stage-based (policy-driven) rather than hardcoded durations.

### Resurrection
- Quality is policy-computed from:
  - available anchors (body parts, bones, soul, stack),
  - integrity of anchors,
  - caster/ritual capability,
  - and invested resources.
- **Bones-only resurrection** is allowed at sufficiently high capability/investment.

### Banks / estates
- Banks are **physical and raidable**.
- Inheritance flows through **EstateCase -> BankEntity** by default.
- Optional policy gate: formal banking/estates require a culture/tech unlock; otherwise use simplified local distribution.

### Replacement & logistics
- Replacement is logistics-driven (wagons/shuttles/carriers/couriers/groups).
- Roles are **template-based** by default.
- Missing roles cause **degraded performance/output** rather than instant replacement.

### Death effects
- Death creates:
  - direct relational edges (for close ties),
  - group sentiment (settlement/faction mourning),
  - mood/morale effects (simple by default),
  - and **notable death memories** only when notability thresholds are met (policy-driven).
- Reputation changes only when killer identity is known (knowledge policy).

---

## Policy Assets (extension points)

All "flexibility" lives here. The kernel expects policies that map:
`(event + current state) -> (commands + new events)`.

### Core policies
- **CreationPolicy**
  - Allowed modes: birth/manufacture/summon/migration/promotion.
  - Applies constraints (resources, space, rules, tech, belief, etc.).
  - Generates lineage links when applicable.
- **AgingPolicy**
  - Continuous age value + stage thresholds (child/adult/elderly).
  - Per-species/class aging rates (bio faster than constructs).
- **DeathOutcomePolicy**
  - Produces: corpse? bones? soul record? stack survival? despawn embodiment?
- **OverkillPolicy**
  - Determines whether embodiment persists or despawns from vital damage.
- **CorpseDecayPolicy**
  - Corpse -> bones -> destroyed triggers.
- **SpiritManifestPolicy**
  - Conditions for spirits/undead manifestation (violent death, cursed location, necromancy, unresolved ties, etc.).
- **SoulPolicy**
  - Storage, decay stages, ripping/consumption rules, binding rules.
- **NeuralStackPolicy**
  - Creation, theft/trade/destroy, duplication gates, clone/fork tagging.
- **ResurrectionPolicy**
  - Accepts anchors and produces embodiment + quality outcomes.
- **InheritancePolicy**
  - What is inheritable (including intellectual assets), routing rules, dispute types.
- **LootingPolicy**
  - What stays on corpse vs buried vs transferred; reaction triggers.
- **RelationsPolicy**
  - Capacity, reserved categories, eviction rules, grief rules.
- **KnowledgePolicy**
  - When "killer is known" becomes true; supports witnesses/sensors/logs/rumors (implemented elsewhere).
- **BankPolicy**
  - Physical storage + registry behavior, raid mechanics hooks, optional abstraction/LOD.
- **ReplacementPolicy**
  - Vacancy -> recruitment via logistics; role template matching; degradation rules.

---

## Lifecycle State Machines (conceptual)

### Embodiment
`Alive -> (DeathEvent) -> Corpse -> Bones -> Destroyed`
- Side branches:
  - `Alive -> Despawned` (overkill/body annihilation)
  - `Corpse/Bones -> Undead` (necromancy/conditions)
  - `Soul -> SpiritManifested` (manifest rules)

### SoulRecord
`Free/Bound/Stored -> (Consumed | Expired | Resurrected)`
- Optional: `Manifested` (ghost embodiment) if conditions apply.

### NeuralStack
`Intact -> (Moved/Stolen/Traded) -> (Duplicated?) -> (Destroyed?)`
- Duplication yields new IdentityId tagged CloneOf/ForkOf.

### EstateCase
`Opened -> (Disputed?) -> Resolved -> Closed`
- Bank raids can interrupt/rescope but do not delete the case.

---

## Orders of Execution (core orchestration)

> Exact system group names are library-level; games can reorder by policy, but the dependency chain is stable.

1. **Apply Damage & Conditions**
2. **Resolve Anatomy** (per-organ if embodied full; coarse otherwise)
3. **Detect Death / Overkill** (emit `DeathEvent`)
4. **Death Resolution** (remains + anchors + estate case + composition changes)
5. **Corpse Decay**
6. **Soul Update**
7. **Stack Update**
8. **Resurrection Processing**
9. **Inheritance Processing**
10. **Looting Processing**
11. **Relations / Sentiment / Reputation**
12. **Memory**
13. **Replacement & Logistics**
14. **Cleanup**

---

## Integration Contracts (events this module emits)

- `CreateRequest`, `Created`
- `AgeAdvanced`, `AgeStageChanged`
- `DamageApplied`, `VitalPartDestroyed`
- `DeathEvent`, `OverkillEvent`
- `CorpseCreated`, `BonesCreated`, `RemainsDestroyed`
- `SoulCreated`, `SoulStored`, `SoulRipped`, `SoulConsumed`, `SoulExpired`, `SpiritManifested`
- `StackCreated`, `StackMoved`, `StackDuplicated`, `StackDestroyed`
- `ResurrectionRequested`, `Resurrected`
- `EstateOpened`, `ClaimRaised`, `DisputeStarted`, `DisputeResolved`, `EstateClosed`
- `Looted`, `Buried`, `DepositedToBank`
- `RoleVacancy`, `ReplacementRequested`, `ReplacementArrived`

---

## Performance Notes (engine-level constraints)

- **Per-organ anatomy is expensive**: only instantiate full anatomy for `EmbodiedFull`.
- **Strong ties are bounded**: default 150 with deterministic eviction; no unbounded social graphs.
- **Event-first keeps costs proportional**: most entities do nothing most ticks.
- **Physical banks are raid targets**: allow LOD/abstraction offscreen, but preserve raid semantics by spawning proxies on conflict.

---

**Last Updated**: 2025-12-20  
**Status**: Locked (v0.2) / Policy-driven kernel
