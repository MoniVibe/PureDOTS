# Agent Behavior Templates (Headless-First)

**Status:** Active - Behavior Spec
**Category:** Mechanics - AI
**Scope:** Cross-Project (PureDOTS → Godgame + Space4X)

## Purpose

Provide **concrete, reusable behavior templates** that map:
- fantasy loop → required components/buffers → which systems own each step

This is written to be **headless-source-of-truth** (no “presentation-only” behavior, no hidden hardcoded cheats).

---

## Shared Spine (Must-Have for “Agents”)

### 1) Perception → Utility → Intent (shared pipeline)
- **Perception**: `PerceptionChannel`, `SenseCapability`, `PerceivedEntity` (+ optional signal fields)
- **Action scoring**: `UtilityCurveRef`, `UtilityDecisionState`, `UtilityConfig`, `ActionScore`
- **Intent/Interrupts**: `Interrupt` buffer, `EntityIntent` (or `EntityIntentQueue` if using queued intents)

### 2) Execution contracts
- **Move/steer**: an entity either has a “movement controller” module or it doesn’t.
  - Godgame: villager movement systems consume “go there” style targets.
  - Space4X: vessel/strike-craft movement systems consume target positions/entities.
- **Interact**: actions must be represented as **explicit intent or job ticket**, not “do work magically.”

### 3) Scaling contracts (non-negotiable)
- **No world scans** in agent jobs. All selection must be via:
  - spatial indexing, registries, or aggregate-provided candidate sets
- **Interrupt-driven** replans for most agents (see `AI_Optimization_Methodologies.md`).

---

## Template A — Godgame Villager “Day Loop”

### Fantasy loop
Gather → Haul → Build/Repair → Socialize/Pray → Rest/Eat → Repeat  
Under threat: flee → regroup → fight/evacuate

### Required components (minimum)
- **Identity/affordances**: `EntityName` (optional), `CapabilityTag` buffer (e.g., `CanHaul`, `CanBuild`, `CanFight`)
- **Needs**: `VillagerNeeds`
- **Job execution**: `VillagerJob`, `VillagerJobTicket`, `VillagerJobProgress`, `VillagerCommand` buffer
- **Perception+intent**: `SenseCapability` + `PerceivedEntity` buffer, `EntityIntent` (or `EntityIntentQueue`)

### Optional (recommended)
- **Collective context (village hub)**: `CollectiveAggregate` + buffers:
  - `DynamicBuffer<CollectiveWorkOrder>`
  - `DynamicBuffer<CollectiveHaulingRoute>`
  - `DynamicBuffer<CollectiveConstructionApproval>`
  - `DynamicBuffer<CollectiveSocialVenue>`
  - `DynamicBuffer<CollectiveAggregateMember>`
- **Relations**: `RelationEdge` buffer (or current relations module)
- **Routine scheduling**: routine/schedule modules (if present)

### Ownership of steps (systems)
- **Need thresholds → interrupts**: Needs systems emit `Interrupt` (LowHunger, LowEnergy, LowHealth, UnderAttack).
- **Utility decision**: AI pipeline chooses `ActionType` (Eat/Rest/Work/Socialize/Flee).
- **Bridge → domain**: bridge converts chosen action to:
  - job request (work/haul/build), or
  - direct intent (flee/move/socialize).
- **Work realization**:
  - village/collective exposes work via `CollectiveWorkOrder` or job request queues
  - work assignment claims tasks; execution systems perform the actual state changes.

### “No illusions” constraints
- Socialize requires a **venue** (`CollectiveSocialVenue.Building != Entity.Null`) or a valid social target.
- Build requires a **construction approval** + materials; otherwise it becomes “seek materials” or “idle.”
- Haul requires a real source/destination; otherwise it becomes “replan.”

---

## Template B — Godgame Village as a Collective Agent

### Fantasy loop
Issue work orders → approve builds → route hauling → maintain social venues → track history/corpse

### Required components
- `CollectiveAggregate` (this is the hub entity)
- Buffers for work/haul/approvals/venues/history/members as needed.

### Key rule
The collective **does not do work**; it only:
- publishes requests (orders/approvals/routes)
- aggregates knowledge/state
- owns persistence (history, corpse window)

---

## Template C — Space4X Mining Vessel Loop

### Fantasy loop
Acquire mining order → travel to asteroid → mine → return to carrier → deposit → repeat  
Under threat: break off → return/evade → request escort

### Required components (minimum)
- `VesselAIState`, movement module (`VesselMovement` / movement controller), `LocalTransform`
- Order module: `MiningOrder`
- Targeting/registry access: resource registry entries / spatial indexing

### Optional (recommended)
- **Shared AI pipeline**: use perception/utilities/intents for a uniform command surface.
- **Collective context (crew/fleet/carrier hub)**:
  - use a `CollectiveAggregate` on the carrier or fleet entity to publish:
    - escort assignments
    - docking/traffic priorities
    - rules of engagement (stance)

### “No illusions” constraints
- Mining requires asteroid `UnitsRemaining > 0`.
- Deposit requires a valid carrier/storage target and capacity.

---

## Template D — Space4X Strike Craft Sortie

### Fantasy loop
Docked → form up → approach → engage → disengage → return → dock  
Behavior modulated by alignment/outlook/stance + fuel/ammo/hull thresholds.

### Required components (minimum)
- `StrikeCraftState`, `LocalTransform`
- `ChildVesselTether` (parent carrier)
- `VesselStanceComponent`, `ThreatProfile` (or equivalent threat source)
- movement controller to enact target pursuit/return

### Implementation note
State updates must be **written back** (use `ref StrikeCraftState` in job execute or ECB), never mutate a copy from a read-only lookup.

---

## Next Implementation Targets (Headless-Friendly)

1. **Villager action bridge**: formalize mapping from Utility `ActionType` → (JobRequest | IntentMode) without game-specific hardcoding.
2. **Collective → candidate set**: collective publishes small “work candidate” buffers to avoid agent world scans.
3. **Threat interrupts**: unify “under attack / new threat / comm receipt” into interrupts used by both games.


