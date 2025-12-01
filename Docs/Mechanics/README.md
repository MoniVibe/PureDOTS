# Cross-Game Mechanics

**Purpose**: Game mechanics that apply to both Godgame and Space4X, with thematic variations.
**Status**: Conceptualization phase
**Last Updated**: 2025-12-01

---

## Overview

This directory contains mechanic specifications that are **game-agnostic at their core** but have **thematic implementations** in both Godgame and Space4X. Each mechanic document describes:

1. **Core Concept**: The fundamental game mechanic (independent of theme)
2. **How It Works**: Rules, parameters, edge cases
3. **Godgame Variation**: Medieval/divine theme implementation
4. **Space4X Variation**: Sci-fi/space theme implementation
5. **Integration**: How it connects to other systems
6. **Balance**: Tuning considerations

---

## Mechanic Documents

### 1. [Miracles and Abilities System](MiraclesAndAbilities.md)

**One-Line**: Player-activated special effects with configurable delivery methods, intensities, and targeting.

**Core Mechanic**:
- Player casts abilities (heal, fire, lightning, water, etc.)
- Delivery methods: Sustained, Throw/Burst, Beacon, Explosion, Chain, Targeted
- Variable intensity: Ember → Firestorm (cost and effect scaling)
- Modular combinations: Type × Delivery × Intensity

**Godgame Theme**: Divine miracles, faith-based casting, villager faith affected
**Space4X Theme**: Tactical abilities (C&C Generals-style), energy-based, fleet support powers

**Status**: Concept - needs component design and system architecture

---

### 2. [Underground Spaces & Hidden Bases](UndergroundSpaces.md)

**One-Line**: Excavatable underground layers with caverns, undercities, and hidden faction bases.

**Core Mechanic**:
- Multi-layer terrain (surface, shallow, deep, bedrock)
- Excavation mechanics (digging, collapse risk, support structures)
- Underground settlements (natural caves, undercities, hideouts)
- Discovery and exploration (hidden bases, loot, encounters)

**Godgame Theme**: Underground caverns, thieves guild hideouts, undercities beneath cities
**Space4X Theme**: Hollow asteroids, station sublevels, hidden pirate bases

**Status**: Concept - needs voxel/layer system design

---

### 3. [Floating Islands & Rogue Orbiters](FloatingIslandsAndRogueOrbiters.md)

**One-Line**: Temporary, mobile locations offering unique loot and exploration, arriving on schedules.

**Core Mechanic**:
- Floating locations appear for limited time
- Drift/orbital mechanics (predictable or random)
- Exploration and loot (dungeons, bosses, treasure)
- Departure warnings (time pressure)
- Optional tethering/capture (extend duration or make permanent)

**Godgame Theme**: Floating islands (treasure, dungeon, merchant, mythic variants)
**Space4X Theme**: Rogue planets, extragalactic comets, alien megastructures, derelict fleets

**Status**: Concept - needs event scheduling and procedural generation

---

### 4. [Special Days & Recurring Events](SpecialDaysAndEvents.md)

**One-Line**: Evolving holidays, blood moon events, and celestial occurrences modifying gameplay.

**Core Mechanic**:
- Calendar system tracking in-game time
- Event categories: Holidays, blood moons, celestial, seasonal, emergent
- Event effects modify spawns, loot, behaviors, resources
- Emergent holidays created by player/entity actions

**Godgame Theme**: Harvest festivals, blood moons (enemy spawns), eclipses, seasonal shifts
**Space4X Theme**: Fleet days, solar flares (shield disruption), planetary conjunctions, nebula drifts

**Status**: Concept - needs calendar system and event scheduler

---

### 5. [Instance Portals & Procedural Dungeons](InstancePortals.md)

**One-Line**: Randomly spawning portals to procedurally generated challenge zones with high-value loot.

**Core Mechanic**:
- Portal spawning (random or scripted)
- Portal types (combat, treasure, boss, nightmare)
- Procedural instance generation (rooms, enemies, loot)
- Completion conditions (clear enemies, defeat boss, solve puzzle)
- Time limits and departure mechanics

**Godgame Theme**: Magical portals (combat gauntlet, treasure vault, boss lair, nightmare realm)
**Space4X Theme**: Anomalous rifts (derelict hulks, alien fortresses, void nexus)

**Status**: Concept - needs procedural generation system and instance isolation

---

### 6. [Runewords & Synergies](RunewordsAndSynergies.md)

**One-Line**: Diablo 2-style combinatorial itemization with runewords, enchantment combos, and set bonuses.

**Core Mechanic**:
- **Runewords**: Socket items with runes/cores in specific orders for bonuses
- **Enchantment Synergies**: Enchantment pairs/sets trigger combo effects
- **Consumable Combos**: Taking multiple potions simultaneously unlocks unique buffs
- **Augment Set Bonuses**: Equipping augments from same set grants escalating bonuses

**Godgame Theme**: Runes + enchantments + potions + magical augments
**Space4X Theme**: Cores + tech upgrades + stims + cybernetic augments

**Status**: Concept - needs socket system, combo detection, and set bonus tracking

---

## Common Patterns Across Mechanics

### 1. **Thematic Duality**
All mechanics have **medieval/divine** (Godgame) and **sci-fi/space** (Space4X) implementations:
- Miracles ↔ Abilities
- Caverns ↔ Hollow Asteroids
- Floating Islands ↔ Rogue Orbiters
- Harvest Festival ↔ Fleet Day
- Magical Portals ↔ Anomalous Rifts
- Runes ↔ Cores

### 2. **Modular Design**
Mechanics emphasize **combinatorial depth**:
- Miracles: Type × Delivery × Intensity
- Runewords: Rune combinations
- Enchantments: Synergy pairs
- Events: Overlapping effects

### 3. **Time Pressure**
Many mechanics create **urgency**:
- Floating islands depart on schedule
- Blood moons appear randomly
- Portal closure timers
- Event durations

### 4. **Discovery & Exploration**
Mechanics reward **exploration and experimentation**:
- Finding hidden underground bases
- Discovering new runeword combinations
- Exploring procedural instances
- Intercepting rare celestial events

### 5. **Risk vs Reward**
All mechanics involve **meaningful trade-offs**:
- Send villagers to dangerous portal for loot?
- Use rare rune now or hoard for better item?
- Tether island to extend duration but risk angering spirit?
- Drink combo potions for buff or save for critical moment?

---

## Integration with PureDOTS Framework

### Potential Framework Extensions

These mechanics may require **new framework capabilities**:

1. **Terrain Multi-Layer System** (Underground Spaces)
   - Voxel-based terrain with vertical layers
   - Excavation and collapse mechanics
   - Underground pathfinding

2. **Mobile Location System** (Floating Islands/Orbiters)
   - Entities that move on schedules
   - Tethering/capture mechanics
   - Trajectory prediction (Space4X)

3. **Calendar & Event System** (Special Days)
   - In-game time tracking
   - Event scheduling (fixed, random, emergent)
   - Cultural memory and holiday evolution

4. **Instance Isolation System** (Portals)
   - Separate simulation contexts
   - Procedural generation utilities
   - Party management

5. **Combo Detection System** (Runewords/Synergies)
   - Pattern matching (runeword sequences)
   - Synergy detection (enchantment pairs)
   - Set bonus tracking

### Extension Request Workflow

When ready to implement, mechanics should be submitted as **Extension Requests** to PureDOTS:
1. Identify framework-level components (game-agnostic)
2. Create extension request in `Docs/ExtensionRequests/`
3. PureDOTS team reviews and approves
4. Framework implements generic system
5. Games implement thematic variations

---

## Next Steps

### Immediate
- [ ] Review mechanics with design team
- [ ] Prioritize mechanics for implementation
- [ ] Identify dependencies between mechanics

### Short-Term
- [ ] Create extension requests for framework capabilities
- [ ] Write technical specifications for each mechanic
- [ ] Prototype core systems (terrain layers, calendar, combo detection)

### Long-Term
- [ ] Implement mechanics in Godgame
- [ ] Implement mechanics in Space4X
- [ ] Balance and tune based on playtesting
- [ ] Document learnings and best practices

---

## See Also

- [CONCEPT_CAPTURE_METHODS.md](../CONCEPT_CAPTURE_METHODS.md) - Documentation methodology
- [PureDOTS Framework Docs](../../Packages/com.moni.puredots/Documentation/)
- [Godgame Documentation](../../Assets/Projects/Godgame/Docs/)
- [Space4X Documentation](../../Assets/Projects/Space4X/Docs/)

---

*Maintainer: Tri-Project Design Team*
*Last Updated: 2025-12-01*
