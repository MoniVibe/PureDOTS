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

### 7. [Entertainment & Performance System](EntertainmentAndPerformers.md)

**One-Line**: Conservatories, pavilions, and street performers employing dancers, musicians, bards, and jesters to provide morale and cultural expression.

**Core Mechanic**:
- Entertainment venues (conservatories, dance schools, pavilions, booths, bandstands)
- Entertainer professions (dancers, musicians, bards, jesters, poets, actors)
- Performance scheduling and audience gathering
- Cultural variation by faction outlook/alignment
- Employment for unemployed and undisciplined entities

**Godgame Theme**: Traditional performers, village entertainment, band entertainers
**Space4X Theme**: Fleet morale officers, holographic entertainment, zero-G performances

**Status**: Concept - needs performance scheduling and cultural style systems

---

### 8. [Wonder Construction System](WonderConstruction.md)

**One-Line**: Multi-stage monument construction requiring professional workers and manufactured resources, built gradually over months to years.

**Core Mechanic**:
- Large-scale prestige projects (temples, colossi, megastructures)
- Professional workers (carpenters, stonemasons, carvers, sculptors)
- Multi-stage progression (foundation → structure → decoration → completion)
- Visible construction progress over time
- Player cannot build directly, must rely on entity workforce
- Massive resource investment, prestigious rewards

**Godgame Theme**: Monuments, temples, wonders of the ancient world
**Space4X Theme**: Megastructures (orbital rings, Dyson swarms, research citadels)

**Status**: Concept - needs staged construction system and professional workforce mechanics

---

### 9. [Limb & Organ Grafting System](LimbAndOrganGrafting.md) ⚠️ **Mature Content**

**One-Line**: Surgical grafting of limbs and organs from other entities to gain their physical properties, with body horror elements and social stigma.

**Core Mechanic**:
- Graft limbs/organs from other races, species, or augments
- Inherit donor properties (orc arm = strength, elf arm = accuracy, wyrm wing = flight)
- Surgical procedures with success/failure, rejection risk
- Social consequences (purists view grafted as abominations)
- Dark scenarios (necromantic grafting, celestial harvesting, limb-stealing horrors)
- Cybernetic/bionic augmentation (Space4X)

**Godgame Theme**: Mad necromancers, celestial harvesting, chimeric warriors, multi-armed assassins
**Space4X Theme**: Cybernetic enhancement, alien organ transplants, transhumanist ascension

**Content Warning**: Body horror, non-consensual modification, anatomical violence (Mature 17+)

**Status**: Concept - needs surgical system, compatibility mechanics, and social reaction framework

---

### 10. [Memories & Lessons System](MemoriesAndLessons.md)

**One-Line**: Historic events and personal experiences stored as memories that provide context-triggered buffs, with cultural preservation and memory trading mechanics.

**Core Mechanic**:
- Memory types: Cultural (shared folklore), Personal (individual experiences), Hybrid (mixed heritage)
- Passive memories: Auto-activate learned behaviors (rolling in sand to extinguish fire)
- Active memories: Focus-activated buffs with context triggers (morale boosts in battle)
- Focus cost based on loyalty and preservation bonuses
- Memory preservation through monuments, statues, newspapers, and oral tradition
- Memory trading during crises to find optimal buffs with morale boost
- Memory fading over time (Fresh → Fading → Dim → Forgotten)
- Memory erasure as cultural warfare (destroying statues, burning newspapers)
- Aggregate memory banks for villages and bands

**Godgame Theme**: Village elders teaching fire safety, dwarf slayer memories, human last stands
**Space4X Theme**: Fleet tactical memories, combat protocols, historical battle data

**Status**: Concept - needs memory activation system, preservation mechanics, and aggregate memory framework

---

### 11. [Consciousness Transference & Psychic Inheritance](ConsciousnessTransference.md) ⚠️ **Mature Content**

**One-Line**: Transfer consciousness, memories, and identity between entities through psychic inheritance, magical possession, or cybernetic override.

**Core Mechanic**:
- Psychic inheritance (natural racial ability) or tech-gated neural override
- Willful or forced transfer of consciousness, memories, cultures, ethics, behaviors
- Memory wipes, overrides, and behavioral molding
- Temporary possession can be resisted and shaken off with strong will
- Eventually takes permanent root over time (staged integration)
- Social identity transfer (titles, renown, reputation migrate with consciousness)
- Option to lay low and hide the transfer or craft new identity
- Demonic/otherworldly exploitation (portal invasions, collective consciousness infestations)
- Collective hive mind spread mechanics

**Godgame Theme**: Demonic possession, necromantic hijacking, ancestral rebirth, soul transfer
**Space4X Theme**: Neural override, consciousness upload, hive mind assimilation, corporate espionage

**Content Warning**: Mind control, identity erasure, non-consensual modification (Mature 17+)

**Status**: Concept - needs consciousness data model, transfer mechanics, resistance system, and collective spread simulation

---

### 12. [Death Continuity & Undead Origins](DeathContinuityAndUndeadOrigins.md) ⚠️ **Mature Content**

**One-Line**: Every undead entity and spirit originates from actual entities that lived and died in the simulation, ensuring narrative continuity and recognition.

**Core Mechanic**:
- Death registry tracks all entity deaths with corpse and spirit persistence
- Undead creation requires actual corpse materials (no conjuring from nothing)
- Every skeleton, zombie, patchwork abomination traced to specific deceased entities
- Spirits manifest from entities who died with unfinished business
- Recognition system: living entities recognize reanimated loved ones
- Forsaken undead: entities who bound soul to corpse, retain consciousness
- Spirit moving on: resolve unfinished business to release spirits
- Corpse harvesting: necromancers gather body parts from actual deaths
- Emotional responses: horror, grief, anger at seeing undead loved ones

**Godgame Theme**: Necromancy desecration, vengeful wraiths, forsaken knights, consecrated burials
**Space4X Theme**: Cyborg reanimation, ghost signals, digital echoes, patchwork combatants

**Content Warning**: Death, necromancy, body horror, grief, desecration (Mature 17+)

**Status**: Concept - needs death registry system, corpse persistence, source traceability, and recognition mechanics

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

6. **Memory & Preservation System** (Memories & Lessons)
   - Memory storage with cultural restrictions
   - Context-triggered activation
   - Preservation through monuments/oral tradition
   - Memory fading and renewal mechanics
   - Aggregate memory banks

7. **Consciousness Transfer System** (Consciousness Transference)
   - Consciousness data model (personality, memories, ethics)
   - Identity transfer mechanics (social stats migration)
   - Resistance and willpower checks
   - Staged integration (reversible → permanent)
   - Epidemic mechanics (collective consciousness spread)

8. **Death Registry & Corpse Persistence** (Death Continuity & Undead Origins)
   - Death record tracking (cause, location, time, relationships)
   - Corpse entity system with decay mechanics
   - Source traceability (link undead to original deceased entities)
   - Recognition system (entities identify reanimated loved ones)
   - Spirit manifestation (unfinished business tracking)

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
