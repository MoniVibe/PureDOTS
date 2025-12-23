# Origin Selector & World Start State

**Status**: Design Concept  
**Category**: Meta / Game Setup / World Generation  
**Applies To**: Godgame, Space4X (shared framework)  
**Purpose**: Solves player agency and worldgen ambiguity through curated starting conditions

---

## Overview

A **lore start / origin selector** provides player agency while eliminating worldgen ambiguity. The same framework works for both Godgame and Space4X.

**Key Insight**: Origins set **initial conditions + rules**, not hard-scripted events. This keeps replayability and makes the "aquarium of evolving entities" compatible: origins decide what capabilities exist in the environment and who starts with which modules.

---

## Two-Layer Structure

### Layer 1: Era Sliders (Continuous Knobs)

Fine-tune the baseline world state:

#### Tech Baseline
- Stone → Industrial → Post-scarcity

#### Political Baseline
- Tribes → City-states → Empires

#### Connectivity
- Isolated → Trade routes → Hyperconnected

#### Ruin Density
- None → Scattered → World-littered

#### External Contact
- None → Occasional → Constant

**Usage**: Players pick an Origin (Layer 2), then fine-tune with sliders.

---

### Layer 2: Origin Cards (Discrete Packages)

Each Origin is a curated bundle of:

- **Starting factions + demographics**
- **Starting infrastructure/colonies**
- **Starting knowledge unlocks**
- **Special rules** (constraints + advantages)
- **"Story truth"** the generator must respect

**Design Rule**: Origins should not hard-script events; they should set:
- Seeded distributions (population, ships, industry)
- Constraints (taboos, treaties, resource scarcities)
- Incentives (victory multipliers, upkeep changes)
- World features (ruins, hazards, guardians)

---

## Godgame Origins (6–10 Max)

Each should feel **mechanically distinct**.

### First Dawn (Cavemen)
- No ruins, no writing, tiny tribes
- Miracles feel "mythic" early because nothing counters them
- **Mechanic**: Low tech baseline, high miracle impact

### Ashes of Empire
- People are early medieval, but the island is full of broken wonders
- Tech comes from salvage + taboo + priesthood politics
- **Mechanic**: High ruin density, salvage-based progression, political complexity

### Stranded Settlers
- Small outside expedition lands (shipwreck / portal mishap)
- They bring a few "foreign" tools + ideas; locals react (fear/admire/steal)
- **Mechanic**: External contact, cultural clash, knowledge diffusion

### Quarantined Eden
- High fertility, but strict natural constraints (storms, sacred zones)
- Forces cultural tech and social organization before industry
- **Mechanic**: Environmental pressure, forced cooperation, delayed industrialization

### Warring City-States
- Multiple advanced polities at start
- Player is less "uplift" and more "balance/tyrant/arbiter"
- **Mechanic**: High political baseline, existing conflicts, player as mediator

### Silent Guardians
- Ancient autonomous protectors already exist (your "slightly dystopian lawful" vibe)
- People accept them; player decides whether to keep, corrupt, or replace
- **Mechanic**: Pre-existing entities, player choice on integration

**Key Godgame Insight**: Don't start "fully advanced" unless you also start with strong social complexity (bureaucracy, factions, doctrine), otherwise it's just unlocked buildings.

---

## Space4X Origins (Mechanics-Focused)

Origins map cleanly to mechanics and change pacing.

### Pre-FTL Splinter World
- Planet not unified; multiple blocs
- **Early game**: Unification or export conflict into orbit
- **Mechanic**: Internal conflict before expansion

### Unified Pre-FTL
- One polity, stable economy
- **Early game**: Faster to first colonies; less internal drama
- **Mechanic**: Streamlined start, focus on expansion

### Early FTL Frontier
- One homeworld + 1–2 small colonies
- **Early game**: Starts with logistics gameplay immediately (supply lines, escorts)
- **Mechanic**: Logistics from day one, colony management focus

### Diaspora / Ark Fleet
- No strong homeworld; you are the nation (carrier-colony ships)
- **Early game**: Fleet-centric identity ("home is the fleet")
- **Mechanic**: Mobile base, fleet-as-nation, different resource model

### Post-Collapse
- FTL knowledge exists but infrastructure is shattered
- **Early game**: Salvage/repair loop + dangerous neighbors
- **Mechanic**: High ruin density, repair gameplay, hostile environment

### Vassal / Protectorate
- You start under a larger empire's shadow
- **Early game**: Autonomy, diplomacy, covert ops
- **Mechanic**: External pressure, limited sovereignty, diplomatic focus

**Key Space4X Insight**: Starting colonies should be an **origin choice**, not a default, because it changes pacing from "explore" to "optimize".

---

## Origin Data Model (Simulation-Friendly)

Origins are defined as data (JSON/ScriptableObject), not hardcoded scripts.

### Origin Schema

```yaml
Origin:
  id: string
  name: string
  description: string
  icon: asset_ref
  
  # Initial Conditions
  factions:
    - faction_id: string
      population: distribution
      territory: region_list
      tech_level: range
      relations: initial_relations_map
  
  infrastructure:
    - type: building/ship/colony
      count: distribution
      location: spawn_rule
  
  knowledge_unlocks:
    - knowledge_id: string
      distribution: faction_list | all | none
  
  # Rules & Constraints
  constraints:
    - type: taboo | treaty | scarcity | tech_lock
      params: {...}
  
  incentives:
    - type: victory_multiplier | upkeep_modifier | resource_bonus
      params: {...}
  
  world_features:
    - type: ruins | hazards | guardians | anomalies
      density: float
      distribution: spawn_rule
  
  # Story Truth (generator must respect)
  story_truth:
    - assertion: string
      must_respect: true
      examples:
        - "Ancient guardians exist and are accepted"
        - "No ruins exist; world is pristine"
        - "External contact is impossible"
```

### Spawn Rules

- **Distribution types**: Uniform, Normal, Poisson, Custom curve
- **Spawn locations**: Random, Clustered, Specific regions, Near features
- **Relations**: Initial relation matrix, alliance groups, hostility groups

---

## Implementation (Shared Framework)

### Origin Registry
- Load origins from data files (moddable)
- Validate schema on load
- Index by ID for fast lookup

### World Generator Integration
- Generator reads selected origin + slider values
- Applies origin constraints during generation
- Respects "story truth" assertions
- Seeds initial state from origin distributions

### Slider Integration
- Sliders modify base values from origin
- Clamp to valid ranges per origin
- Some origins may lock certain sliders (e.g., "First Dawn" locks ruin density to 0)

### Random Origin Option
- "Random Origin (weighted)" option
- Weights configurable per game mode
- Can exclude certain origins from random pool

---

## Modding Support

### Origin Definition Format
- JSON or ScriptableObject (authoring-friendly)
- Validated against schema
- Can reference modded content (factions, knowledge, infrastructure)

### Origin Dependencies
- Origins can depend on mods
- Missing dependencies disable origin with clear error

### Custom Sliders
- Mods can add custom sliders
- Sliders can be origin-specific or global

---

## UI/UX Recommendations

### Origin Selection
- **Card-based UI**: Visual cards with icon, name, description
- **Preview**: Show key mechanics/constraints before selection
- **Comparison**: Side-by-side comparison mode

### Slider Interface
- **Visual feedback**: Show how sliders affect world state
- **Presets**: Quick presets (e.g., "Classic", "Hard", "Experimental")
- **Lock indicators**: Show which sliders are locked by origin

### Random Option
- **Weighted random**: Show weights or allow customization
- **Exclude list**: Let players exclude specific origins from random

---

## Practical Recommendations

### Ship Defaults
- **6 origins per game** (Godgame: 6, Space4X: 6)
- **3–5 sliders** that always apply
- **"Random Origin (weighted)"** option

### Content Strategy
- Start with 3–4 origins per game
- Add more based on player feedback
- Keep each origin mechanically distinct

### Testing
- Each origin should be playtested for:
  - Balance (not too easy/hard)
  - Distinct feel (different gameplay)
  - Replayability (varied outcomes)
  - Story coherence (respects "story truth")

---

## Integration Points

- **World Generation System**: Applies origin constraints
- **Faction System**: Seeds initial factions from origin
- **Knowledge System**: Unlocks initial knowledge
- **Infrastructure System**: Spawns starting buildings/ships
- **Relation System**: Sets initial relations
- **Modding System**: Loads origin definitions

---

## Design Principles

1. **Origins set conditions, not scripts**: Replayable, emergent outcomes
2. **Mechanically distinct**: Each origin changes gameplay feel
3. **Simulation-friendly**: Works with evolving entity systems
4. **Moddable**: Origins defined as data, not code
5. **Player agency**: Sliders allow fine-tuning
6. **Story coherence**: "Story truth" ensures narrative consistency

---

## Open Questions

- How to handle origin-specific victory conditions?
- Should origins unlock specific content (tech trees, units)?
- How to balance origins for multiplayer?
- Should origins have "seasons" or rotation (temporary availability)?
- How to handle origin conflicts in modded content?

---

**Last Updated**: 2025-12-20  
**Status**: Design Concept

