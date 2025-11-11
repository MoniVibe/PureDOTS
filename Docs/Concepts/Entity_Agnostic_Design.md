# Entity Agnostic Design - PureDOTS Foundation

**Status:** Design Document  
**Category:** Core Architecture  
**Scope:** PureDOTS Foundation Layer  
**Created:** 2025-01-XX  
**Last Updated:** 2025-01-XX

---

## Purpose

PureDOTS provides **game-agnostic** entity scaffolding for both individual entities and aggregate entities (collectives). The foundation layer treats them identically - only the game-specific layers differentiate between them.

**Key Principle:** Aggregates and individuals use the same components and systems. They only differ in how game-specific layers interpret and present them.

---

## Core Concept: "Villager" as Generic Entity

In PureDOTS, the term **"Villager"** is used as a generic name for **individual entities**. It does NOT imply:
- Fantasy-specific presentation
- Human-specific traits
- Godgame-specific mechanics

Instead, "Villager" simply means: **"An individual entity that can be part of an aggregate."**

**Examples:**
- **Godgame:** Villagers are individual people with 3D presentation
- **Space4X:** "Villagers" are individual pops/crew members (no 3D presentation, represented via UI and ships)

---

## Component Agnosticism

### Shared Components

Both individual entities and aggregate entities use the same foundational components:

#### Alignment Components
- **`VillagerAlignment`**: Tri-axis alignment (Moral, Order, Purity)
  - Used by: Individual entities AND aggregate entities
  - Aggregates compute their alignment from member averages
  - Same behavior logic applies to both scales

#### Behavior Components
- **`VillagerBehavior`**: Personality traits (Vengeful/Forgiving, Bold/Craven)
  - Used by: Individual entities AND aggregate entities
  - Aggregates compute their behavior from member averages
  - Same initiative/decision logic applies to both scales

#### Initiative Components
- **`VillagerInitiativeState`**: Autonomous action timing
  - Used by: Individual entities AND aggregate entities
  - Aggregates act autonomously just like individuals
  - Same frequency formulas apply to both scales

#### Grudge Components
- **`VillagerGrudge`**: Grudge tracking buffer
  - Used by: Individual entities AND aggregate entities
  - Aggregates can hold grudges against other aggregates
  - Same decay mechanics apply to both scales

### Component Naming Convention

**Note:** Components are prefixed with "Villager" for historical reasons, but they are **entity-agnostic**. The naming convention means:
- `VillagerAlignment` = "Entity Alignment" (works for individuals and aggregates)
- `VillagerBehavior` = "Entity Behavior" (works for individuals and aggregates)
- `VillagerInitiativeState` = "Entity Initiative State" (works for individuals and aggregates)

**Future Consideration:** These could be renamed to `EntityAlignment`, `EntityBehavior`, etc. for clarity, but the current naming is acceptable as long as documentation clarifies the agnostic nature.

---

## Aggregate vs Individual: Same Data Path

### How Aggregates Work

Aggregates (bands, guilds, villages, fleets, planets) use the **exact same components** as individuals:

1. **Aggregate entities have the same components:**
   ```csharp
   // Individual entity
   Entity individualEntity;
   AddComponent(individualEntity, new VillagerAlignment { ... });
   AddComponent(individualEntity, new VillagerBehavior { ... });
   
   // Aggregate entity (band, guild, village, fleet, planet)
   Entity aggregateEntity;
   AddComponent(aggregateEntity, new VillagerAlignment { ... }); // Same component!
   AddComponent(aggregateEntity, new VillagerBehavior { ... });   // Same component!
   ```

2. **Aggregates compute values from members:**
   - Alignment: Average of member alignments (weighted by influence)
   - Behavior: Average of member behaviors (weighted by influence)
   - Initiative: Derived from aggregate behavior + member averages

3. **Same systems process both:**
   - `VillagerInitiativeSystem` processes individuals AND aggregates
   - `CombatPersonalitySystem` processes individuals AND aggregates
   - `VillagerUtilityScheduler` processes individuals AND aggregates

### The Only Difference: Scale

The **only** difference between aggregates and individuals is **scale**:
- **Individuals:** Single entity making decisions
- **Aggregates:** Collection of entities making collective decisions

But the **components and systems are identical**. The game-specific layers handle presentation and interpretation differences.

---

## Game-Specific Differentiation

### Godgame Layer

**Individual Entities (Villagers):**
- Have 3D presentation (GameObject with mesh, animation)
- Visible in the world
- Direct player interaction (click, select, command)
- Presentation: `GodgameVillagerPresentation` component (game-specific)

**Aggregate Entities (Villages, Bands, Guilds):**
- May have aggregate presentation (village buildings, band formations)
- Visible as collections of individuals
- Indirect player interaction (select village, command aggregate)
- Presentation: `GodgameVillagePresentation`, `GodgameBandPresentation` (game-specific)

**PureDOTS Foundation:**
- Both use `VillagerAlignment`, `VillagerBehavior`, `VillagerInitiativeState`
- Same systems process both
- No presentation-specific data in PureDOTS

### Space4X Layer

**Individual Entities ("Villagers" = Pops/Crew):**
- **NO 3D presentation** (no GameObject)
- Represented solely via UI (pop list, crew roster)
- Represented via ship they pilot/man/captain
- Represented via child vessels they command
- Presentation: UI-only, ship assignment (game-specific)

**Aggregate Entities (Planets, Fleets, Sectors):**
- Represented via UI (planet view, fleet composition)
- Represented via aggregate ships/stations
- Indirect player interaction (select planet, command fleet)
- Presentation: UI-only, aggregate ship representation (game-specific)

**PureDOTS Foundation:**
- Both use `VillagerAlignment`, `VillagerBehavior`, `VillagerInitiativeState`
- Same systems process both
- No presentation-specific data in PureDOTS

---

## Legacy Inconsistencies

### Current State

There are some legacy components that are **inconsistent** with the agnostic design:

1. **`VillageAlignmentState`** (in `VillageBehaviorComponents.cs`)
   - Uses different axes: `LawChaos`, `Materialism`, `Integrity`
   - Should migrate to `VillagerAlignment` (Moral, Order, Purity)
   - **Status:** Legacy, will be deprecated

2. **`GuildAlignment`** (in `GuildComponents.cs`)
   - Uses same axes as `VillagerAlignment` (Moral, Order, Purity) ✅
   - But is a separate component type
   - **Status:** Should be unified to use `VillagerAlignment` directly

### Migration Path

**Future Work:**
- Migrate `VillageAlignmentState` → `VillagerAlignment`
- Migrate `GuildAlignment` → `VillagerAlignment`
- Ensure all aggregates use the same alignment component as individuals

---

## System Agnosticism

### Systems That Process Both

All PureDOTS systems are designed to work with **any entity** that has the required components:

- **`VillagerInitiativeSystem`**: Processes any entity with `VillagerInitiativeState` + `VillagerBehavior` + `VillagerAlignment`
- **`VillagerGrudgeDecaySystem`**: Processes any entity with `VillagerGrudge` buffer + `VillagerBehavior`
- **`CombatPersonalitySystem`**: Processes any entity with `CombatAI` + `VillagerBehavior` + `VillagerAlignment`
- **`VillagerUtilityScheduler`**: Processes any entity with `VillagerNeeds` + `VillagerBehavior`

**No special handling for aggregates vs individuals** - the systems are truly agnostic.

---

## Implementation Guidelines

### For PureDOTS Developers

1. **Never add game-specific presentation data** to PureDOTS components
2. **Use generic naming** (or document that "Villager" means "entity")
3. **Design systems to work with any entity** that has the required components
4. **Aggregates compute from members** - don't special-case them

### For Game-Specific Developers

1. **Add presentation components** in game-specific layers (Godgame, Space4X)
2. **Map PureDOTS entities** to your presentation layer
3. **Handle scale differences** in game-specific systems (aggregate UI, individual 3D)
4. **Reuse PureDOTS systems** - don't duplicate logic

---

## Examples

### Example 1: Alignment Drift

**PureDOTS System:**
```csharp
// Processes ANY entity with VillagerAlignment
public void Execute(ref VillagerAlignment alignment, in VillagerBehavior behavior)
{
    // Alignment drift logic (same for individuals and aggregates)
    alignment.MoralAxis += ComputeDrift(behavior, ...);
}
```

**Godgame Layer:**
- Individual villager: Alignment affects 3D appearance (clothing color, building style)
- Aggregate village: Alignment affects village culture (aggregate visual style)

**Space4X Layer:**
- Individual pop: Alignment affects UI display (pop card, tooltip)
- Aggregate planet: Alignment affects planet UI (culture display, policy options)

### Example 2: Initiative-Based Actions

**PureDOTS System:**
```csharp
// Processes ANY entity with VillagerInitiativeState
public void Execute(ref VillagerInitiativeState initiative, in VillagerBehavior behavior)
{
    if (initiative.NextActionTick <= CurrentTick)
    {
        // Trigger autonomous action (same logic for individuals and aggregates)
        initiative.PendingAction = SelectAction(behavior);
    }
}
```

**Godgame Layer:**
- Individual villager: Opens shop, starts family, plots revenge
- Aggregate village: Expands territory, forms band, declares war

**Space4X Layer:**
- Individual pop: Migrates, changes job, joins faction
- Aggregate planet: Colonizes new world, forms fleet, declares independence

---

## Related Documentation

- **Generalized Alignment Framework:** `Docs/Concepts/Meta/Generalized_Alignment_Framework.md`
- **Villager Behavioral Personality:** `Docs/Concepts/Villagers/Villager_Behavioral_Personality.md`
- **Band Formation:** `Docs/Concepts/Villagers/Band_Formation_And_Dynamics.md`
- **Guild System:** `Docs/Concepts/Villagers/Guild_System.md`

---

**For Implementers:** PureDOTS is the foundation. Game-specific layers add presentation and interpretation. Keep PureDOTS agnostic, and both games can reuse the same systems.

**For Designers:** Think of PureDOTS entities as "things that have alignment, behavior, and initiative" - whether they're individuals or aggregates doesn't matter at the foundation level.

---

**Last Updated:** 2025-01-XX  
**Status:** Design Document - Foundation Layer Architecture

