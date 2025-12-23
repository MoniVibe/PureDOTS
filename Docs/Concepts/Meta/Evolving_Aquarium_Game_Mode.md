# Evolving Aquarium Game Mode

**Status**: Concept / Design Exploration  
**Category**: Meta / Game Mode Design  
**Applies To**: Experimental gameplay mode (Godgame or standalone)  
**Core Principle**: Agents start blank, acquire capabilities via tokens, evolve through selection pressure

---

## Overview

A minimal, emergent simulation where agents begin with only locomotion and collision avoidance, then acquire cognitive and biological modules by absorbing "capability tokens" scattered in the world. The system creates emergent behavior through module combinations, inheritance (genetic + cultural), and environmental selection pressures.

**Key Design Goal**: Make capabilities explicit modules so emergence is legible and debuggable.

---

## Core Loop

### Blank Movers
- Agents start with only:
  - **Locomotion** (movement)
  - **Collision avoidance** (basic physics)

### Capability Tokens
- World contains **capability tokens** (pickups / absorption fields)
- Absorbing a token adds a **module** to the agent:
  - Senses (vision, hearing, smell)
  - Memory (short-term cache, map crumbs)
  - Language (ping, symbols, sequences)
  - Planning (goal stack, multi-step plans)
  - Crafting
  - Deception
  - Cooperation (promises, contracts, reputation)
  - Organs (hands, storage, armor, sprint)
  - Skills (harvest, build, heal, fight, trade, teach)

### Survival & Reproduction
- Agents survive/reproduce/split based on objective:
  - Energy
  - Territory
  - Comfort
  - Tribe score
  - (Configurable per mode)

### Inheritance
- New agents start blank (or inherit modules via):
  - **Genetic inheritance** (hard): offspring inherits subset of modules or parameter tweaks
  - **Cultural inheritance** (soft): teaching copies policies, symbol mappings, reputation records

### Selection Pressure
- Environmental pressures + capability availability + inheritance rules create the "aquarium"

---

## Module Ladder (Minimal Progression)

Each module must be **useful alone** but **combine into surprising behavior**.

### Perception
- Vision cone
- Hearing radius
- Smell gradient

### Memory
- Short-term cache (last N observations)
- Map crumbs (visited heatmap)

### Needs / Reward
- Hunger/energy
- Safety
- Curiosity
- Affiliation

### Communication
- **Ping** (binary signal)
- **Symbols** (discrete tokens)
- **Sequences** (compound messages)

### Cooperation
- Promise/contract primitive
- Reputation tracking

### Intention
- Goal stack (tiny planner)
- Multi-step plans

### Deception
- Ability to emit signals not tied to state
- Cost-based (energy or cooldown)

### Organs
- Hands/tool use
- Storage stomach
- Armor
- Sprint muscle

### Skills
- Harvest
- Build
- Heal
- Fight
- Trade
- Teach

---

## Selection Pressures (Pick One or Rotate Seasons)

**Critical Design Choice**: What selects winners?

Without selection pressure, you get random wandering + token hoarding.

### Energy Ecology
- Food exists
- Moving costs energy
- Modules cost upkeep
- Efficient agents thrive

### Territory
- Resources regenerate
- Groups that hold zones do better

### Predator–Prey
- Simplest way to force cooperation + deception
- Predators hunt by specific senses (sound, vision, smell)
- Prey can evolve countermeasures (alarm calls, hiding, grouping)

### Puzzle Ecology
- Doors/locks require specific modules
- Communication modules needed for coordination
- Tool use required for access

### Social Economy
- Scarce "skill tokens" easier to obtain via trade
- Creates incentive for cooperation and reputation

### Module Cost Rule
- **Modules have ongoing cost**
- "Becoming smart" isn't always optimal unless it pays
- Prevents universal token hoarding

---

## Cooperation + Deception Guardrails

If lying is free, signals become noise. Add:

### Signal Cost
- Energy or cooldown per signal
- Prevents spam

### Verification
- Agents can "inspect" reality with senses
- Direct observation vs. reported information

### Reputation
- Track signal reliability per sender
- Agents remember who lied

### Punishment Options
- Ignore
- Exile
- Attack
- Refuse trade

**Result**: Deception becomes situational instead of dominant.

---

## Inheritance Channels

### Genetic Inheritance (Hard)
- Offspring inherits subset of modules
- Parameter tweaks (mutation)
- Deterministic or probabilistic selection

### Cultural Inheritance (Soft)
- Teaching copies:
  - Policies
  - Symbol mappings
  - Reputation records
- **Teaching as a module**:
  - "Teacher" packages capability into learnable blueprint
  - Slower than absorbing token, but scalable
  - "Student" must have prerequisites (attention + memory)
- **Result**: Tribes can become "smart" without every individual finding rare tokens

---

## World Design (Structured Token Distribution)

### Token Rarity Tiers
- **Common**: Perception, short memory, basic signal
- **Uncommon**: Planning, tool use, long memory
- **Rare**: Deception, teaching, contracts

### Location-Based Tokens
- **Water biome** → "gills" module
- **Dark biome** → "echolocation" module
- **Ruins** → "tools + symbols" modules
- **High predator density** → pushes "grouping + alarm calls" modules

---

## Observability Tools (Mandatory)

Without observability, the system feels like "random ants."

### Per-Agent UI
- Active modules
- Current goal
- Recent memories
- Trust table

### Population Telemetry
- Module distribution histograms
- Tribe counts
- Mortality causes

### Event Log
- "signal sent"
- "promise broken"
- "trade"
- "attack"
- "teach"

---

## Implementation (DOTS-Friendly)

### Agent Model
- **Base components**: `Position`, `Velocity`, `Energy`
- **Optional tag/components per module**:
  - `HasVision`, `HasMemory`, `CanSignal`, `CanPlan`, etc.
- **Systems query only agents with module components**
- Cost proportional to how many agents are advanced

### Token Model
- Tokens are entities that:
  - Add/remove components (or set capability bitmasks)
  - Provide data blobs (module parameters)
- Absorption triggers component addition

### Module Data
- Each module may have:
  - **Capability tag** (presence/absence)
  - **Module data component** (parameters, state, cooldowns)
  - **Upkeep cost** (energy drain per tick)

### Inheritance System
- **Genetic**: On reproduction, sample parent modules with mutation
- **Cultural**: Teaching system copies module data + policies to student entities

---

## First Playable Slice

### Minimal Setup
- Blank movers + food energy
- **3 tokens**: Vision, Memory(32), Ping signal
- **One predator type** that hunts by sound
- **Rule**: Agents with Ping can warn nearby agents (if they saw predator)
- **Reproduction**: Split when energy > threshold; slight mutation in movement speed

### Expected Emergence
If this works, you'll immediately see:
- **Proto-culture**: Alarm calls, clustering, cautious foraging
- **Cooperation**: Agents with vision + ping become "sentinels"
- **Selection**: Agents without ping get eaten; ping spreads

---

## Design Principles

1. **Explicit modules**: Every capability is a discrete, testable module
2. **Useful alone**: Each module provides value independently
3. **Surprising combinations**: Modules interact to create emergent behavior
4. **Selection pressure**: Environmental forces create winners/losers
5. **Inheritance**: Both genetic and cultural transmission
6. **Costs**: Modules have upkeep to prevent universal hoarding
7. **Observability**: Full telemetry for understanding emergence
8. **Legibility**: Why agents do things should be traceable

---

## Integration Points

- **Entity Modularity System**: Module tags/components
- **Communication System**: Signal emission/reception
- **Memory System**: Short-term cache, map crumbs
- **Reputation System**: Trust tracking per sender
- **Teaching System**: Cultural inheritance mechanism
- **Energy System**: Upkeep costs, food sources
- **Perception System**: Vision/hearing/smell modules
- **Planning System**: Goal stack, multi-step plans

---

## Open Questions

- How to balance module costs vs. benefits?
- What selection pressures create interesting dynamics?
- How to prevent "optimal build" from dominating?
- Should modules have prerequisites?
- How to handle module conflicts (e.g., sprint vs. armor tradeoffs)?
- What teaching mechanisms are most interesting?
- How to visualize module combinations and their effects?

---

**Last Updated**: 2025-12-20  
**Status**: Concept / Design Exploration

