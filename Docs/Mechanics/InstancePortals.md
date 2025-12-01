# Mechanic: Instance Portals & Procedural Dungeons

## Overview

**Status**: Concept
**Complexity**: Complex
**Category**: Exploration / Combat / Loot

**One-line description**: *Randomly spawning portal instances that transport entities into procedurally generated dungeon-like spaces for exploration, combat, and high-value loot, separate from the main world.*

## Core Concept

The world occasionally spawns **instance portals** - magical gates (Godgame) or anomalous rifts (Space4X) that lead to isolated, procedurally generated challenge zones. These instances are:
- **Isolated**: Separate from main world, only accessible via portal
- **Procedural**: Layout, enemies, loot generated on entry
- **Limited**: Portals are temporary (despawn after time or completion)
- **Challenging**: Harder than normal gameplay, designed for prepared entities
- **Rewarding**: High-quality loot, unique resources, legendary items

Players must decide whether to engage with portals (risk entities, invest time) or ignore them (miss rewards). Instances create focused, high-intensity gameplay moments distinct from open-world simulation.

## How It Works

### Basic Rules

1. **Portal Spawning**: Portals appear randomly in world (fixed locations or random positions)
2. **Portal Types**: Different portal types lead to different instance themes (combat, puzzle, exploration, boss)
3. **Entry**: Entities enter portal, transported to instance (separate scene/zone)
4. **Instance Generation**: Procedural generation creates unique layout, enemies, loot each time
5. **Completion Conditions**: Clear all enemies, defeat boss, solve puzzle, reach exit
6. **Exit**: Entities return to world at portal location (or nearest safe point)
7. **Portal Closure**: Portal despawns after time limit, completion, or abandonment

### Portal Types

| Portal Type | Theme | Difficulty | Loot | Frequency |
|------------|-------|-----------|------|-----------|
| **Combat Gauntlet** | Wave defense, arena | Medium | Weapons, armor | Common (weekly) |
| **Treasure Vault** | Trap-filled maze | Medium | Gold, gems, rare items | Uncommon (monthly) |
| **Boss Lair** | Single powerful enemy | High | Legendary weapons, artifacts | Rare (seasonal) |
| **Puzzle Chamber** | Logic puzzles, mechanisms | Low combat, high thought | Unique items, knowledge | Uncommon (monthly) |
| **Horde Swarm** | Overwhelming numbers | High | Mass loot, crafting materials | Common (weekly) |
| **Nightmare Realm** | Extreme challenge | Very High | Mythic items, endgame gear | Very rare (yearly) |

### Instance Structure

**Components**:
1. **Entry Room**: Safe zone, preparation area
2. **Challenge Rooms**: Combat, puzzles, traps
3. **Boss Room** (if applicable): Final encounter
4. **Treasure Room**: Loot cache
5. **Exit Portal**: Returns to world

**Procedural Generation**:
- **Room Templates**: Pool of pre-designed room types (corridor, arena, trap hall, etc.)
- **Assembly**: Rooms connected in random configurations
- **Population**: Enemies, loot, obstacles placed based on difficulty tier
- **Modifiers**: Random modifiers (e.g., "Cursed" = all enemies poisonous, "Blessed" = healing fountains)

### Godgame: Portal Types

#### Combat Gauntlet (Common)
- **Appearance**: Glowing blue portal, 10m tall
- **Interior**: Stone arena, multiple waves of enemies
- **Enemies**: 3-5 waves, scaling difficulty
- **Loot**: Weapons, armor, potions (quality: rare)
- **Duration**: Portal open 3 days, instance completion ~1 hour

#### Treasure Vault (Uncommon)
- **Appearance**: Golden portal, ornate frame
- **Interior**: Maze with spike traps, pressure plates, locked doors
- **Enemies**: Minimal (guardians at checkpoints)
- **Loot**: Gold, gems, enchanted items (quality: epic)
- **Duration**: Portal open 5 days, instance completion ~2 hours

#### Boss Lair (Rare)
- **Appearance**: Dark red portal, ominous aura
- **Interior**: Single large chamber, dramatic arena
- **Enemies**: 1 legendary boss (dragon, demon lord, ancient golem)
- **Loot**: Legendary weapon, artifact, unique item
- **Duration**: Portal open 7 days, instance completion ~30 min (if skilled)

#### Nightmare Realm (Very Rare)
- **Appearance**: Swirling void portal, reality distortion
- **Interior**: Shifting, nightmarish landscape
- **Enemies**: Eldritch horrors, corrupted entities, endless spawns
- **Loot**: Mythic items, forbidden knowledge, game-changing rewards
- **Duration**: Portal open 1 day (urgency!), instance completion ~3 hours

### Space4X: Portal Types

#### Anomalous Rift - Combat Scenario
- **Appearance**: Spatial tear, energy readings off the charts
- **Interior**: Hostile alien ship graveyard, derelicts animated by anomaly
- **Enemies**: Corrupted ship AI, drone swarms
- **Loot**: Advanced modules, exotic alloys
- **Duration**: Rift stable 5 days, instance completion ~1 hour

#### Derelict Hulk - Exploration Scenario
- **Appearance**: Abandoned megaship, drifting in space
- **Interior**: Procedural ship interior (corridors, cargo bays, engineering)
- **Enemies**: Minimal (malfunctioning security bots, parasites)
- **Loot**: Ship components, data logs, lost tech
- **Duration**: Derelict accessible 7 days, instance completion ~2 hours

#### Alien Fortress - Boss Scenario
- **Appearance**: Fortified alien structure, heavy defenses
- **Interior**: Military complex, central command with alien warlord
- **Enemies**: Alien troops, warlord boss
- **Loot**: Alien superweapon, rare tech
- **Duration**: Fortress accessible 10 days, instance completion ~1 hour

#### Void Nexus - Nightmare Scenario
- **Appearance**: Dimensional gateway, unstable
- **Interior**: Non-Euclidean space, reality-bending
- **Enemies**: Otherworldly entities, incomprehensible threats
- **Loot**: Impossible tech, dimensional artifacts
- **Duration**: Nexus open 2 days, instance completion ~4 hours (if you survive)

### Instance Modifiers (Procedural Variation)

Random modifiers add variety to repeat runs:

| Modifier | Effect | Frequency |
|----------|--------|-----------|
| **Cursed** | All enemies poisonous, heal over time disabled | 10% |
| **Blessed** | Healing fountains present, resurrection chance | 5% |
| **Horde** | Enemy count 2x, loot 2x | 15% |
| **Elite** | Fewer enemies, all elite-tier, loot 1.5x | 10% |
| **Time Trial** | Instance auto-fails after time limit, bonus loot if fast | 5% |
| **Permadeath** | Entities die permanently (no revival), loot 3x | 2% |
| **Fog of War** | Limited visibility, surprise encounters | 10% |

### Parameters and Variables

| Parameter | Default Value | Range | Effect |
|-----------|--------------|-------|--------|
| **Portal Spawn Chance** | 5% per week | 1%-20% | How often portals appear |
| **Portal Duration** | 5 days | 1-30 days | How long portal stays open |
| **Instance Difficulty Scaling** | 1.5x world difficulty | 1.0x-3.0x | How hard instance is vs normal content |
| **Loot Quality Multiplier** | 2.0x | 1.5x-5.0x | How much better loot is |
| **Max Party Size** | 5 entities | 1-20 | How many entities can enter together |
| **Instance Completion Time** | Varies (30 min - 4 hours) | Varies | Expected time to complete |
| **Modifier Chance** | 30% | 0%-100% | Chance instance has random modifier |

### Edge Cases

- **Portal Closes While Entities Inside**:
  - Entities teleport back to world (safe)
  - Entities trapped, must complete instance to escape (harsh)
  - Entities lost (very harsh, permadeath)
- **Entity Dies in Instance**:
  - Normal death rules apply (revival possible if resurrection mechanic exists)
  - Permadeath modifier: Entity lost permanently
- **Multiple Portals Active**:
  - Players can choose which to engage (strategic choice)
  - Portals on timer encourage prioritization
- **Portal Spawns in Dangerous Location**:
  - Portal surrounded by enemies (must clear to access)
  - Portal in inaccessible terrain (requires flight, digging, etc.)
- **Instance Becomes Unwinnable**:
  - Retreat option (forfeit loot, return to world)
  - No retreat (must complete or die trying - harsh)

## Player Interaction

### Player Decisions (Godgame)

- **Send Villagers to Portal?**: Risk valuable entities for loot
- **Party Composition**: Choose tank, healer, DPS mix for balanced team
- **Miracle Support**: Use miracles inside instance (if allowed) or let villagers handle alone
- **Retreat or Push**: Abandon instance mid-run to save entities, or risk all for loot

### Player Decisions (Space4X)

- **Divert Fleet?**: Pull ships from strategic positions to engage instance
- **Scout First?**: Send probe to assess difficulty before committing fleet
- **Cooperative or Solo**: Team up with AI faction or claim solo
- **Invest in Preparation**: Refit ships for instance (anti-anomaly shields, boarding gear)

### Skill Expression

- **Portal Prioritization**: Knowing which portals are worth the risk
- **Party Optimization**: Building synergistic teams (tank + healer + DPS)
- **Instance Routing**: Learning optimal paths through procedural rooms (pattern recognition)
- **Risk Management**: Knowing when to retreat vs push for full clear
- **Modifier Adaptation**: Adjusting strategy based on random modifiers (Horde = AOE focus, Elite = single-target burst)

### Feedback to Player

- **Visual feedback**:
  - Portal appearance distinct per type (blue = combat, gold = treasure, red = boss, void = nightmare)
  - Instance interior visually themed (stone dungeon, alien ship, nightmare realm)
  - Loot rarity shown via glow intensity (gold = legendary, purple = mythic)
  - Timer UI shows portal closure countdown
- **Numerical feedback**:
  - Difficulty estimate displayed (recommended party size, threat level)
  - Loot quality estimate (common, rare, epic, legendary, mythic)
  - Completion progress (rooms cleared, enemies defeated)
- **Audio feedback**:
  - Portal spawn sound (mystical chime, ominous rumble)
  - Instance ambient music (intense combat themes)
  - Boss music (epic, dramatic)
  - Victory fanfare on completion

## Balance and Tuning

### Balance Goals

1. **Risk vs Reward**: High-quality loot justifies risking valuable entities
2. **Time Investment**: Instances should be worth the time commitment
3. **Optional Content**: Instances optional, not mandatory for progression
4. **Difficulty Curve**: Instances harder than normal, but achievable for prepared players

### Tuning Knobs

1. **Spawn Frequency**: More frequent = more content but less special, less frequent = more exciting but sparse
2. **Difficulty Scaling**: Higher difficulty = more challenge but may exclude casual players
3. **Loot Quality**: Better loot = more motivation but risks power creep
4. **Portal Duration**: Longer duration = less pressure but less urgency
5. **Permadeath Modifier Frequency**: Higher frequency = more risk but more frustration

### Known Issues

- **Loot Power Creep**: If instance loot too strong, becomes mandatory grind
- **Portal Spam**: Too many portals = distraction from core gameplay
- **Instance Fatigue**: If procedural generation too similar, becomes repetitive
- **Difficulty Spikes**: Random modifiers can create unwinnable scenarios (Elite + Horde = overwhelming)
- **Exploit Farming**: Players may farm easy instances repeatedly for loot (needs diminishing returns or lockout)

## Integration with Other Systems

### Interacts With

| System/Mechanic | Type of Interaction | Priority |
|----------------|---------------------|----------|
| **Loot System** | Instances drop high-quality loot | High |
| **Party/Squad System** | Entities group to tackle instances | High |
| **Difficulty Scaling** | Instances scale to party strength or fixed difficulty | Medium |
| **Time System** | Portals on timers, create urgency | Medium |
| **Buff System** | Instance modifiers apply buffs/debuffs | Medium |
| **Quest System** | Instances can be quest objectives | Low |
| **Miracle/Ability System (Godgame)** | Player can support party with miracles | Medium |

### Emergent Possibilities

- **Portal Racing**: Multiple factions compete to clear instance first (PvP race)
- **Rescue Missions**: Entity trapped in instance, party sent to rescue before portal closes
- **Portal Chaining**: Completing one instance unlocks portal to even harder instance (escalation)
- **Cursed Loot**: Some instance loot is cursed, creates problems back in world (trade-off)
- **Instance Lore**: Data logs/murals in instances reveal world backstory (narrative hook)
- **Portal Manipulation**: Late-game tech/miracles allow triggering portals on demand (player agency)

## Implementation Notes

### Technical Approach

**Portal & Instance System**:
```csharp
// Portal Entity
public struct Portal : IComponentData {
    public PortalType Type; // Combat, Treasure, Boss, Nightmare
    public float TimeRemaining; // Seconds until portal closes
    public float3 WorldPosition;
    public bool IsActive;
    public int MaxPartySize;
}

// Instance Definition (generated on entry)
public struct InstanceDef {
    public PortalType Type;
    public int Seed; // Procedural generation seed
    public DynamicBuffer<RoomTemplate> Rooms;
    public DynamicBuffer<EnemySpawn> Enemies;
    public DynamicBuffer<LootDrop> Loot;
    public InstanceModifier Modifier; // Cursed, Blessed, Horde, etc.
}

// Instance Session (active run)
public struct InstanceSession : IComponentData {
    public Entity PortalEntity;
    public InstanceDef Def;
    public DynamicBuffer<Entity> PartyMembers;
    public int RoomsCleared;
    public bool IsCompleted;
}

// Instance Room Template
public struct RoomTemplate {
    public FixedString64Bytes RoomType; // Arena, Corridor, TrapHall, BossRoom
    public int2 Size; // Width, height
    public DynamicBuffer<SpawnPoint> SpawnPoints;
}

// Instance Loot Drop
public struct LootDrop {
    public ItemRarity Rarity; // Common, Rare, Epic, Legendary, Mythic
    public float3 Position;
    public Entity ItemEntity;
}
```

**System Flow**:
1. **PortalSpawnSystem**: Procedurally spawns portals in world (random or fixed locations)
2. **PortalInteractionSystem**: Handles entity entry (forms party, triggers instance load)
3. **InstanceGenerationSystem**: Generates instance layout, enemies, loot based on seed and type
4. **InstanceProgressionSystem**: Tracks room clears, boss defeats, completion
5. **InstanceExitSystem**: Teleports entities back to world, awards loot, despawns instance
6. **PortalDespawnSystem**: Closes portals after time limit or completion

### Procedural Generation Strategy

**Room-Based Assembly**:
- Library of pre-made room templates (50+ variations)
- Rooms tagged by type (entry, combat, trap, boss, treasure, exit)
- Generator selects rooms based on instance type, connects via corridors
- Enemy/loot placement based on room size, difficulty tier

**Seed-Based Consistency**:
- Each instance uses random seed
- Same seed = same layout (allows replays, shared experiences)
- Seed displayed to players (can share "good" seeds with friends)

### Godgame-Specific: Miracle Usage in Instances

- **Allowed**: Player can cast miracles inside instance (e.g., heal party, smite enemies)
- **Restricted**: Higher faith cost inside instance (interference from portal energy)
- **Forbidden**: No miracles allowed, villagers must rely on skills (hardcore mode)

### Space4X-Specific: Fleet Restrictions

- **Ship Size Limits**: Some portals only allow small ships (fighters, corvettes)
- **Anomalous Conditions**: Jump drives disabled, shields weakened, sensors jammed
- **Boarding Actions**: Instances require infantry deployment (ship-to-ship combat ineffective)

### Performance Considerations

- **Instance Loading**: Load instance scene asynchronously (avoid main world stutter)
- **Separate Simulation**: Instance runs in isolated simulation (no cross-contamination with world)
- **Unload on Exit**: Aggressively unload instance assets when party exits (free memory)
- **Concurrent Instances**: Support multiple active instances if multiple portals open

### Testing Strategy

1. **Unit tests for**:
   - Procedural generation (seed consistency, room connections)
   - Difficulty scaling (enemy stats, loot quality)
   - Party entry/exit (teleport logic, loot distribution)
   - Portal timers (accurate countdowns)

2. **Playtests should verify**:
   - Instances feel distinct and exciting
   - Procedural generation creates fun layouts (not broken/unwinnable)
   - Loot rewards justify risk and time
   - Modifiers add variety, not frustration

3. **Balance tests should measure**:
   - Average instance completion time
   - Party wipe rate (should be <20% for normal instances)
   - Loot value vs normal gameplay (2-3x better)
   - Player engagement (do players seek out portals or ignore?)

## Examples

### Example Scenario 1: Combat Gauntlet (Godgame)

**Setup**: Blue portal appears near village. Player selects 5 warriors (tank, 2 DPS, healer, archer).
**Action**:
- Party enters portal, teleported to stone arena
- Instance generated: 5 waves, 3-4 enemies per wave
- Wave 1: 3 goblins (easy warmup)
- Wave 2: 4 orcs (moderate challenge)
- Wave 3: 2 trolls (heavy hitters, tank struggles)
- Player casts **Heal Burst** miracle (100 faith) to save party
- Wave 4: 3 dark knights (armor, high damage)
- Archer kited, DPS focused fire, 1 DPS dies
- Wave 5: 1 mini-boss (ogre warlord)
- Healer keeps tank alive, party defeats ogre
**Result**:
- Instance completed in 45 minutes
- Loot: 3 rare weapons, 5 rare armor pieces, 200 gold
- 1 warrior died (can be revived in village if resurrection exists)
- Party exits, portal despawns
- **Consequence**: Village military strength increases (better gear)

### Example Scenario 2: Boss Lair - Dragon (Godgame)

**Setup**: Red portal appears. Player sends elite party (5 legendary warriors, max gear).
**Action**:
- Party enters, teleported to massive cavern
- Ancient dragon awaits (legendary boss)
- Dragon has 10,000 HP, fire breath, tail swipe, flight
- **Phase 1**: Dragon grounded, melee attacks
  - Tank holds aggro, DPS attacks flanks
  - Dragon breath kills 1 DPS (instant death)
- **Phase 2**: Dragon takes flight, aerial attacks
  - Archer critical hit, dragon crashes
  - Party focuses damage, dragon at 30% HP
- **Phase 3**: Dragon enraged, massive fire breath
  - Player casts **Protection Blessing** (saves party)
  - Final assault, dragon defeated
**Result**:
- Instance completed in 40 minutes (intense fight)
- Loot: **Dragonslayer Sword** (legendary, +500 damage, fire immunity)
- 1 warrior died (sacrifice)
- Portal despawns
- **Consequence**: Village gains legendary weapon, hero worship

### Example Scenario 3: Anomalous Rift - Combat (Space4X)

**Setup**: Spatial rift detected. Player sends 8 frigates (balanced fleet).
**Action**:
- Fleet enters rift, teleported to ship graveyard
- Corrupted AI ships activate (15 hostile contacts)
- **Wave 1**: 5 drone fighters
  - Fleet point defense handles easily
- **Wave 2**: 3 corrupted frigates
  - Sustained firefight, 2 player frigates damaged
- **Wave 3**: 1 AI dreadnought (boss)
  - Dreadnought heavy shields, massive firepower
  - Fleet coordinates focus fire, shields down
  - Boarding action (marines capture dreadnought)
**Result**:
- Instance completed in 1 hour
- Loot:
  - Captured dreadnought (repairable)
  - 5 advanced modules (shield boosters, railgun upgrades)
  - Exotic alloys (rare resources)
- 2 frigates destroyed, 3 damaged (repairable)
- Fleet exits rift
- **Consequence**: Player gains capital ship, significant power boost

### Example Scenario 4: Void Nexus - Nightmare (Space4X)

**Setup**: Dimensional gateway opens (very rare). Player sends elite fleet (12 battleships, flagship carrier).
**Action**:
- Fleet enters nexus, reality warps
- Non-Euclidean space, sensors malfunctioning
- **Encounter 1**: Dimensional parasites (swarm, ignore shields)
  - Fleet loses 2 battleships (hull penetrated)
- **Encounter 2**: Time dilation field (slow motion combat)
  - Strategic maneuvering critical
- **Encounter 3**: Void Leviathan (incomprehensible entity)
  - Fleet focuses fire, minimal damage
  - Player uses **Dimensional Torpedo** (rare tech)
  - Leviathan destabilizes, collapses
**Result**:
- Instance completed in 3.5 hours (grueling)
- Loot:
  - **Void Core** (mythic item, powers impossible tech)
  - Dimensional drive blueprints (teleportation)
  - 10,000 exotic matter
- 4 battleships destroyed, 5 heavily damaged
- **Consequence**: Player gains game-changing tech, dominance shift

## References and Inspiration

- **Diablo series**: Rifts, procedural dungeons, loot focus
- **Path of Exile**: Map system, modifiers, endgame instances
- **World of Warcraft**: Dungeon instances, raid portals
- **Hades**: Procedural roguelike runs, chamber-based progression
- **Deep Rock Galactic**: Procedural mission instances, co-op extraction
- **No Man's Sky**: Portal exploration, anomalous locations

## Godgame-Specific Variations

### Portal Varieties
- **Divine Trial**: Player-created portal to test villager worth (proves faith)
- **Underworld Gate**: Portal to afterlife, retrieve fallen heroes (resurrection quest)
- **Elemental Rift**: Portal to elemental plane (fire, water, earth, air) - themed loot
- **Memory Echo**: Portal to relive historical battle (educational, grants knowledge)

### Miracle Interactions
- **Seal Portal**: Miracle to close unwanted portal (prevent enemies from using)
- **Stabilize Portal**: Extend portal duration (expensive miracle)
- **Bless Party**: Buff party before entry (protection, strength, wisdom)

## Space4X-Specific Variations

### Anomaly Types
- **Precursor Vault**: Ancient alien storage, lost technology
- **Pirate Hideout**: Instance is pirate base interior (boarding action)
- **Stellar Nursery**: Unique star formation, exotic resource harvesting under time pressure
- **Ghost Ship**: Haunted derelict, horror-themed instance

### Fleet Mechanics
- **Boarding Parties**: Some instances require infantry deployment
- **Ship Size Restrictions**: Small portals = fighters only, large = capital ships only
- **Anomalous Physics**: Shields 50% effective, weapons modified (tactical adaptation)

## Revision History

| Date | Change | Reason |
|------|--------|--------|
| 2025-12-01 | Initial draft | Conceptualization capture session |

---

*Last Updated: 2025-12-01*
*Document Owner: Tri-Project Design Team*
