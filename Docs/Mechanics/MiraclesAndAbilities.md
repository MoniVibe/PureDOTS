# Mechanic: Miracles and Abilities System

## Overview

**Status**: Concept
**Complexity**: Complex
**Category**: Core Gameplay / Player Powers

**One-line description**: *Player-activated special effects with configurable delivery methods, intensities, and targeting options that heal, damage, or modify entities and terrain.*

## Core Concept

Players can cast miracles (Godgame) or abilities (Space4X) - special effects ranging from healing to elemental damage. These powers are highly modular: each miracle/ability has a **type** (heal, fire, electricity, water, etc.), a **delivery method** (sustained, throw/burst, beacon, explosion, chain, targeted), and a **variable intensity** (ember to firestorm). Not all combinations make thematic sense, but the system supports creative flexibility.

## How It Works

### Basic Rules

1. **Miracle/Ability Selection**: Player chooses a miracle type (heal, fire, water, lightning, etc.)
2. **Delivery Configuration**: Player selects delivery method (sustained, throw, beacon, chain, explosion)
3. **Intensity Adjustment**: Player adjusts power level from minimal (ember) to maximum (firestorm)
4. **Targeting**: Player targets area, entity, or position
5. **Execution**: Miracle/ability applies effects based on type, delivery, and intensity
6. **Consequences**: Resources consumed (faith, energy, cooldown), effects resolved

### Miracle/Ability Types

| Type | Effect | Godgame Example | Space4X Example |
|------|--------|----------------|----------------|
| **Heal** | Restore health/integrity | Divine blessing | Repair nanites |
| **Fire** | Damage over time, spread | Holy fire | Plasma burst |
| **Electricity** | Burst damage, chain | Lightning strike | EMP pulse |
| **Water** | Area control, extinguish | Rain clouds | Coolant spray |
| **Earth** | Terrain modification | Earthquake | Gravitational shear |
| **Ice** | Slow, freeze | Blizzard | Cryogenic field |
| **Wind** | Push, buff movement | Divine wind | Ion propulsion boost |

### Delivery Methods

| Delivery | Behavior | Targeting | Examples |
|----------|----------|-----------|----------|
| **Sustained** | Continuous effect around divine hand/cursor | Area (follows player cursor) | Heal aura, continuous flame jet |
| **Throw/Burst** | Instant area-of-effect at target | Area (fixed location) | Fireball, healing burst |
| **Beacon** | Mobile effect attached to entity | Entity (moves with target) | Healing beacon on villager, burning mark on enemy |
| **Explosion** | Radial burst from center | Area (fixed location) | Thunderclap (lightning explosion), meteor impact |
| **Chain** | Bounces between entities | Entity → Entity (N times) | Chain lightning, healing chain |
| **Targeted** | Direct line to single entity | Single entity | Focused heal beam, assassination lightning bolt |

### Delivery Method Details

#### Sustained Cast
- **Behavior**: Effect emanates continuously from divine hand/cursor position
- **Movement**: Follows player cursor/hand in real-time
- **Examples**:
  - **Heal Sustained**: Healing aura around hand heals all entities in radius
  - **Fire Sustained**: Flame jet damages and ignites entities
  - **Water Sustained**: Creates moving rain cloud that follows hand
  - **Lightning Sustained**: Continuous arc of electricity damaging nearby enemies

#### Throw/Burst Cast
- **Behavior**: Instant area-of-effect explosion at target location
- **Movement**: Fixed position, does not move after cast
- **Examples**:
  - **Heal Burst**: Area healing pulse
  - **Fire Burst**: Fireball explosion
  - **Lightning Burst**: Localized electrical discharge
  - **Water Burst**: Flash flood

#### Beacon Cast
- **Behavior**: Attaches miracle effect to entity, which becomes mobile source
- **Entity Control**:
  - Entity carries miracle passively (always active)
  - If entity has sufficient **Faith** (Godgame) or **Authority** (Space4X), can direct/activate it
  - Player can delegate beacon control to "champion" entities
- **Examples**:
  - **Heal Beacon**: Medic villager/ship heals nearby allies
  - **Fire Beacon**: Burning entity damages enemies around it
  - **Lightning Beacon**: Entity emits electrical pulses

#### Explosion Cast
- **Behavior**: Radial burst from center point, often with knockback
- **Examples**:
  - **Lightning Explosion**: Thunderclap - radial electrical damage
  - **Fire Explosion**: Meteor impact - fire damage + burning terrain
  - **Water Explosion**: Tidal wave - knockback + extinguish fires
  - **Heal Explosion**: Shockwave of restoration

#### Chain Cast
- **Behavior**: Bounces between entities, can be random or selective
- **Targeting Options**:
  - Random: Chains to nearest entities
  - Selective: Player pre-selects entities or sets filter (avoid friendlies, prioritize hostiles)
  - Aggregate-based: Chains within same faction/band
- **Examples**:
  - **Heal Chain**: Bounces between wounded allies (max N jumps)
  - **Lightning Chain**: Classic chain lightning, prioritizes enemies
  - **Fire Chain**: Ignites entities sequentially
- **Edge Case**: Chain meteor is conceptually difficult (meteors don't "bounce"), so not all combos are supported

#### Targeted Cast
- **Behavior**: Direct single-entity effect, often the most powerful
- **Examples**:
  - **Heal Targeted**: Full restoration on one villager/ship
  - **Lightning Targeted**: Assassination strike on single enemy
  - **Fire Targeted**: Concentrated incineration

### Intensity Scaling

Players adjust intensity from **minimal to maximal**:

| Intensity | Cost Multiplier | Effect Multiplier | Examples |
|-----------|----------------|------------------|----------|
| **Ember** (1%) | 0.1x | 0.2x | Small spark, minor heal |
| **Flicker** (25%) | 0.3x | 0.5x | Campfire, basic heal |
| **Flame** (50%) | 1.0x | 1.0x | Standard fireball, normal heal |
| **Blaze** (75%) | 2.5x | 2.0x | Large fire, major heal |
| **Inferno** (100%) | 5.0x | 4.0x | Firestorm, full restoration |

**Cost Multiplier**: Faith/energy consumption
**Effect Multiplier**: Damage, healing, radius, duration

### Friendly Fire / Heal Toggle

Many delivery methods support **targeting filters**:
- **Avoid Friendly Fire**: Fire/lightning won't damage allies
- **Avoid Friendly Heal**: (Rarely used) Heal won't affect enemies
- **Hostile Only**: Only affects enemies
- **Allied Only**: Only affects friendlies
- **All Entities**: No discrimination

### Parameters and Variables

| Parameter | Default Value | Range | Effect |
|-----------|--------------|-------|--------|
| **Base Cost** | 100 Faith/Energy | 10-1000 | Base resource cost |
| **Intensity Multiplier** | 1.0x | 0.1x-5.0x | Scales cost and effect |
| **Radius (Area Effects)** | 10m | 5m-50m | Area of effect size |
| **Chain Max Jumps** | 5 | 1-20 | Maximum chain bounces |
| **Beacon Duration** | 60s | 10s-300s | How long beacon lasts |
| **Sustained Tick Rate** | 0.5s | 0.1s-2.0s | How often sustained effects apply |
| **Cooldown** | 10s | 0s-120s | Time before reuse |

### Edge Cases

- **Beacon on Dead Entity**: Beacon dissipates, returns partial cost
- **Chain with No Valid Targets**: Fizzles, consumes reduced cost
- **Sustained Cast Out of Bounds**: Effect stops at map edge
- **Friendly Fire Toggle + Explosion**: Filters targets within radius before applying damage
- **Water vs Fire Interaction**: Water extinguishes fire (terrain, entities), fire evaporates water
- **Overlapping Beacons**: Effects stack (heal faster, burn faster)

## Player Interaction

### Player Decisions

- **Type Selection**: What miracle type fits the situation? (Heal allies vs damage enemies)
- **Delivery Choice**: Sustained control vs instant burst vs delegated beacon?
- **Intensity Tuning**: Conserve resources with low intensity or go all-out?
- **Targeting Strategy**: Focus single target or spread across area? Chain through cluster?
- **Friendly Fire Management**: Allow collateral damage or play it safe?

### Skill Expression

- **Beacon Delegation**: Skilled players identify best "champion" entities to carry beacons
- **Chain Targeting**: Pre-selecting chain targets for maximum efficiency
- **Sustained Positioning**: Moving hand/cursor to maximize coverage without waste
- **Intensity Optimization**: Using just enough power to achieve goal without overspending
- **Combo Timing**: Combining miracles (e.g., water to group enemies, then lightning explosion)

### Feedback to Player

- **Visual feedback**:
  - Miracle type has distinct VFX (fire = flames, heal = golden glow, lightning = arcs)
  - Intensity changes VFX scale and brightness
  - Delivery method has unique visual signature (beacon = orbiting particles, chain = visible arc jumps)
  - Targeting reticle shows affected area/entities
- **Numerical feedback**:
  - Resource cost displayed before casting
  - Damage/heal numbers float above affected entities
  - Chain jump counter (3/5 jumps remaining)
- **Audio feedback**:
  - Miracle type has unique sound (crackling lightning, soothing chime for heal)
  - Intensity scales volume/pitch
  - Chain jumps have distinct "zap" sound per hop

## Balance and Tuning

### Balance Goals

1. **Cost vs Effect**: High-intensity miracles should feel powerful but expensive
2. **Delivery Trade-offs**: Sustained = controlled but requires attention, Burst = convenient but less precise, Beacon = delegated but less direct control
3. **Combo Viability**: Encourage creative combinations without mandatory "meta" builds
4. **Friendly Fire Risk**: Choosing to enable friendly fire should have meaningful consequences

### Tuning Knobs

1. **Intensity Cost Curve**: How steeply does cost scale with intensity? (Currently exponential)
2. **Beacon Duration**: Longer durations = more value, but less dynamic gameplay
3. **Chain Efficiency**: Do later chain jumps deal full damage or diminishing returns?
4. **Sustained Tick Rate**: Faster ticks = more DPS but harder to balance
5. **Cooldown Scaling**: Should high-intensity casts have longer cooldowns?

### Known Issues

- **Chain Lightning Meta**: If chain lightning is too efficient, becomes dominant strategy
- **Heal Beacon Abuse**: Permanent heal beacon on tanky unit might create unkillable entities
- **Friendly Fire Exploits**: Players might deliberately damage own units for insurance/narrative reasons

## Integration with Other Systems

### Interacts With

| System/Mechanic | Type of Interaction | Priority |
|----------------|---------------------|----------|
| **Resource System** | Consumes faith/energy | High |
| **Buff System** | Miracles can apply buffs (burn, heal-over-time) | High |
| **Terrain System** | Fire ignites terrain, water creates puddles | Medium |
| **Faction/Alignment** | Affects faith/reputation based on targets hit | High |
| **Weather System** | Water miracles interact with rain, fire with drought | Medium |
| **AI System** | Entities with beacons need AI to use them intelligently | High |

### Emergent Possibilities

- **Combo Chains**: Water to group enemies → Lightning explosion for massive AoE
- **Beacon Synergy**: Fire beacon on fast scout to spread fire, heal beacon on slow tank to support frontline
- **Friendly Fire Politics**: Accidentally (or "accidentally") damaging allied faction to weaken them
- **Terrain Control**: Using sustained water to create permanent rain zone, blocking fire-based enemies
- **Champion Gameplay**: Delegating powerful beacons to high-faith entities creates hero units

## Implementation Notes

### Technical Approach

**Component Architecture**:
```csharp
// Miracle/Ability Definition
public struct MiracleDef {
    public MiracleType Type; // Heal, Fire, Lightning, etc.
    public DeliveryMethod Delivery; // Sustained, Throw, Beacon, etc.
    public float Intensity; // 0.0-1.0
}

// Miracle Execution State
public struct MiracleExecution : IComponentData {
    public Entity Target; // For beacon/targeted
    public float3 Position; // For throw/explosion/sustained
    public float Duration; // For sustained/beacon
    public int ChainJumpsRemaining; // For chain
}

// Beacon Component (attached to entity)
public struct MiracleBeacon : IComponentData {
    public MiracleDef MiracleDef;
    public float RemainingDuration;
    public bool CanBeControlled; // If entity has enough faith/authority
}

// Miracle Effect (buff/debuff)
public struct MiracleEffect : IBufferElementData {
    public MiracleType Type;
    public float Magnitude;
    public float Duration;
}
```

**System Flow**:
1. `MiracleInputSystem` captures player input, creates `MiracleExecution` entity
2. `MiracleDeliverySystem` handles delivery-specific logic:
   - **Sustained**: Continuously applies effects around cursor position
   - **Throw**: One-shot area effect at target position
   - **Beacon**: Attaches `MiracleBeacon` component to target entity
   - **Explosion**: Radial query, applies effects to all in radius
   - **Chain**: Iteratively finds next target, applies effect, decrements jumps
3. `MiracleEffectApplicationSystem` applies actual damage/healing/buffs
4. `MiracleVFXSystem` spawns visual effects based on type, intensity, delivery

### Performance Considerations

- **Sustained Miracles**: Can be expensive if tick rate is high - consider spatial hashing for nearby entity queries
- **Chain Lightning**: Iterative pathfinding for chain jumps - cache valid targets to avoid repeated queries
- **Explosion Radius**: Large explosions need efficient spatial queries - use BVH or grid-based lookups
- **VFX Pooling**: High-intensity miracles spawn many particles - pool VFX instances

### Testing Strategy

1. **Unit tests for**:
   - Intensity scaling (cost vs effect)
   - Chain jump targeting logic
   - Friendly fire filtering
   - Beacon attachment/detachment

2. **Playtests should verify**:
   - Miracle combos feel creative and rewarding
   - Intensity scaling feels meaningful (not just "always max")
   - Beacon delegation is intuitive
   - Friendly fire toggle is clear and functional

3. **Balance tests should measure**:
   - Cost-per-damage ratio for each delivery method
   - Sustained vs Burst efficiency over time
   - Beacon uptime and value-per-cost
   - Chain efficiency with different jump counts

## Examples

### Example Scenario 1: Heal Burst in Battle

**Setup**: 5 friendly villagers wounded in melee combat, clustered together
**Action**: Player casts **Heal Burst** at 50% intensity, targeting cluster center
**Result**:
- Cost: 100 faith (base 100 * 1.0x intensity multiplier)
- Effect: Each villager heals for 50 HP (base 100 HP * 0.5x effect multiplier)
- Visual: Golden pulse expands from center, wounded villagers glow briefly
- Audio: Soothing chime

### Example Scenario 2: Chain Lightning Against Enemy Band

**Setup**: 8 enemy raiders advancing in loose formation
**Action**: Player casts **Chain Lightning** at 75% intensity, targeting frontmost raider, max 5 jumps, "Hostile Only" filter
**Result**:
- Cost: 250 faith (base 100 * 2.5x intensity multiplier)
- Effect:
  - 1st target: 200 damage (base 100 * 2.0x effect multiplier)
  - 2nd-5th targets: 200 damage each (chain continues, no falloff in this design - tunable)
  - 6th-8th targets: Not hit (only 5 jumps configured)
- Visual: Lightning arcs visibly jump from raider to raider, arc thickness reflects intensity
- Audio: Crackling "zap" sound on each jump

### Example Scenario 3: Heal Beacon on Champion Villager

**Setup**: Elite villager (high faith) leading a warband
**Action**: Player casts **Heal Beacon** at 25% intensity, attaching to elite villager, duration 60s
**Result**:
- Cost: 30 faith (base 100 * 0.3x intensity multiplier)
- Effect: Beacon attached to villager
  - Beacon heals nearby allies (10m radius) for 10 HP every 0.5s
  - Villager has enough faith to control beacon - can activate/deactivate at will
  - Beacon follows villager as they move
- Visual: Orbiting golden particles around villager, allies in radius glow faintly
- Audio: Soft healing hum, pulses every 0.5s
- Duration: Lasts 60s or until villager dies

### Example Scenario 4: Fire Sustained to Create Barrier

**Setup**: Enemy army approaching village from north
**Action**: Player uses **Fire Sustained** at 100% intensity, dragging divine hand in a line across enemy path
**Result**:
- Cost: 500 faith/s (base 100 * 5.0x intensity multiplier, continuous)
- Effect:
  - Flame jet continuously damages enemies in cursor area (20m radius)
  - Damages 400 HP/s (base 100 * 4.0x effect multiplier * 1 tick/s)
  - Ignites terrain, creating lingering fire walls
  - Enemies hesitate, try to path around
- Visual: Massive flame jet following cursor, terrain catches fire
- Audio: Roaring flames, crackling
- Player must maintain cast - drains faith rapidly but effective area denial

## References and Inspiration

- **Black & White (2001)**: Divine hand mechanics, miracles with gestures
- **Diablo 2**: Skill intensity/levels, runewords for combos
- **Warcraft 3**: Hero abilities with targeted/area-of-effect distinction
- **Magicka**: Spell combination system, friendly fire mechanics
- **Command & Conquer Generals**: Support powers (heal, airstrike, etc.) with cooldowns and targeting
- **Populous**: God-game miracles, terrain modification

## Godgame-Specific Variations

### Miracle Types Unique to Godgame
- **Faith Restoration**: Increases villager faith in player
- **Fertility Blessing**: Increases crop yield, birth rates
- **Plague/Curse**: Debuff miracle, lowers morale, spreads disease
- **Revelation**: Grants knowledge/technology to villagers

### Faith as Resource
- Miracles consume **Faith Points**, accumulated through villager worship
- High-intensity miracles impress villagers, potentially increasing faith generation
- Failed/wasteful miracles (e.g., friendly fire) decrease faith

## Space4X-Specific Variations

### Ability Types Unique to Space4X
- **Shield Boost**: Temporary shield regeneration
- **Overcharge**: Increases weapon fire rate
- **Tactical Cloak**: Temporary invisibility
- **Nanite Repair**: Advanced healing for ships

### Energy as Resource
- Abilities consume **Command Energy**, regenerates over time
- Flagship abilities (carrier-based) can be delegated to squadron leaders
- Beacon abilities attached to ace pilots allow tactical autonomy

## Revision History

| Date | Change | Reason |
|------|--------|--------|
| 2025-12-01 | Initial draft | Conceptualization capture session |

---

*Last Updated: 2025-12-01*
*Document Owner: Tri-Project Design Team*
