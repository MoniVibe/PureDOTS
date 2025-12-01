# Mechanic: Underground Spaces & Hidden Bases

## Overview

**Status**: Concept
**Complexity**: Complex
**Category**: World/Exploration / Settlement

**One-line description**: *Excavatable underground caverns, undercities, and hidden bases that serve as alternative settlement locations, rogue faction hideouts, and exploration vectors.*

## Core Concept

Terrain supports **underground layers** beneath the surface. These spaces can be:
- **Natural caverns/caves**: Procedurally generated or hand-placed
- **Undercities**: Settlements built beneath cities, hidden or integrated
- **Hidden bases**: Secret faction hideouts (thieves guilds, assassins, rogue bands)
- **Ruins/dungeons**: Ancient structures buried underground

Players and entities can excavate, settle, explore, and fight for control of these spaces. Underground areas offer unique strategic value (hidden from surface view, defensible, resource-rich) but also dangers (collapse risk, monster lairs, limited access).

## How It Works

### Basic Rules

1. **Terrain Layers**: Terrain has multiple vertical layers (surface, shallow underground, deep underground, bedrock)
2. **Bedrock Borders**: Nearly indestructible bedrock serves as map boundaries and prevents infinite digging
3. **Excavation**: Entities with mining/digging capabilities can carve tunnels and expand caverns
4. **Settlement**: Underground spaces can be claimed and developed by factions
5. **Visibility**: Surface entities cannot see underground unless entrance is exposed
6. **Access Points**: Entrances/exits (stairs, ladders, tunnels, portals) connect surface and underground

### Underground Layer Types

| Layer | Depth Range | Characteristics | Godgame Example | Space4X Example |
|-------|------------|-----------------|----------------|----------------|
| **Surface** | 0m | Normal terrain | Fields, forests | Asteroid surface |
| **Shallow** | 0-20m | Easily excavated, unstable | Cellars, shallow caves | Hollow asteroid pockets |
| **Deep** | 20-100m | Harder to dig, stable | Deep caverns, ancient ruins | Station sublevel |
| **Bedrock** | 100m+ | Nearly indestructible | Map border | Asteroid core |

### Excavation Mechanics

- **Digging Speed**: Depends on tool quality, entity skill, terrain hardness
- **Terrain Hardness**:
  - Dirt/clay: Fast (1 block/s)
  - Stone: Medium (0.2 blocks/s)
  - Ore veins: Slow (0.05 blocks/s)
  - Bedrock: Nearly impossible (0.001 blocks/s - effectively infinite time)
- **Collapse Risk**: Unsupported excavations can collapse, burying entities
  - Requires support structures (pillars, beams) for large caverns
  - Collapse risk increases with cavern size and lack of supports
- **Resource Discovery**: Digging reveals ore veins, water sources, hidden treasures

### Underground Settlement Types

#### Natural Caverns (Godgame)
- **Formation**: Procedurally generated during worldgen or discovered during excavation
- **Features**: Stalactites, underground rivers, ore veins, monster spawns
- **Uses**: Mining outposts, hermit shelters, monster lairs

#### Undercities (Godgame)
- **Formation**: Built beneath existing cities, either:
  - Intentionally (sewers, catacombs, storage)
  - Organically (squatters, outcasts, criminals)
- **Governance**:
  - **Integrated**: City government controls undercity
  - **Independent**: Undercity forms pseudo-government (usually corrupt)
  - **Hidden**: Undercity exists in secret, unknown to surface rulers
- **Relations**: Undercity factions interact with surface based on alignment:
  - Trade (smuggled goods, illegal services)
  - Conflict (raids, sabotage)
  - Symbiosis (waste disposal, hidden refuge)

#### Thieves Guild / Assassin Hideouts (Godgame)
- **Formation**: Rogue aggregates (thieves guilds, assassin orders) seek hidden bases
- **Quest Vector**: Guilds issue quests to clear out monster-infested caverns
- **Self-Clearing**: If player ignores, guilds attempt to clear caverns themselves (may succeed or fail)
- **Securability**: Hideouts can be secured (traps, guards, hidden entrances) or left vulnerable
- **Discovery**: Explorers, rival factions, or player can discover and raid hideouts
  - If undefended, hideout can be looted or claimed
  - If defended, triggers combat encounter

#### Band Safehouses (Godgame)
- **Formation**: Bandit/mercenary bands settle in remote caverns as bases
- **Uses**: Store loot, rest between raids, recruit new members
- **Vulnerability**: If discovered, can be raided by adventurers, rival bands, or player-directed forces

#### Hollow Asteroids (Space4X)
- **Formation**: Natural voids in asteroids or carved by mining operations
- **Uses**: Hidden bases, mining outposts, pirate lairs
- **Strategic Value**: Difficult to detect, defensible (limited entry points)

#### Undercities in Colonies/Stations (Space4X)
- **Formation**: Sublevel sectors in space stations or colony underground (if planet)
- **Abstraction**: Simplified like colony districts - not fully 3D navigable, but trackable as separate zones
- **Factions**: Criminal syndicates, resistance cells, black markets

### Parameters and Variables

| Parameter | Default Value | Range | Effect |
|-----------|--------------|-------|--------|
| **Bedrock Depth** | 100m | 50-200m | How deep before hitting indestructible layer |
| **Excavation Speed (Dirt)** | 1.0 blocks/s | 0.5-2.0 | How fast soft terrain is dug |
| **Excavation Speed (Stone)** | 0.2 blocks/s | 0.1-0.5 | How fast hard terrain is dug |
| **Collapse Risk Threshold** | 50 unsupported blocks | 20-100 | Cavern size before collapse risk |
| **Support Structure Range** | 10m | 5-20m | How far one support beam stabilizes |
| **Undercity Independence Chance** | 20% | 0-100% | Chance undercity forms independent governance |
| **Quest Clear Difficulty** | Medium | Easy-Hard | How hard for NPCs to self-clear caverns |

### Edge Cases

- **Bedrock Breach Attempt**: If entity tries to dig bedrock, extremely slow progress (essentially infinite time)
  - Legendary tools or miracles might allow limited bedrock removal, but never at map edge
- **Cavern Collapse with Entities Inside**: Entities take massive damage, potentially buried alive
  - Rescue mechanics: Other entities can dig them out
- **Hidden Entrance Discovery**: If undercity/hideout entrance is hidden, discovery chance based on:
  - Explorer skill
  - Traffic patterns (high traffic = more likely to discover)
  - Informants (bribes, interrogation)
- **Overlapping Undercities**: Multiple factions build separate undercities beneath same city - can intersect, leading to conflict or treaties
- **Flooded Caverns**: If underground river is breached, cavern floods - entities drown or must pump water out

## Player Interaction

### Player Decisions (Godgame)

- **Allow/Forbid Underground Settlement**: Player can bless or curse undercity formation
- **Quest Guidance**: Direct thieves guilds to specific caverns or let them choose
- **Collapse Miracles**: Player can trigger collapses to bury enemies or seal off areas
- **Revelation Miracle**: Expose hidden bases, showing player their locations
- **Support Blessing**: Reinforce caverns, preventing collapses

### Player Decisions (Space4X)

- **Scan for Hollow Asteroids**: Invest in sensor upgrades to detect hidden bases
- **Claim/Raid**: Decide to peacefully claim hollow asteroid or assault pirate lair
- **Develop Undercities**: Build sublevel districts for efficiency or secrecy
- **Infiltration**: Send spies to uncover hidden bases

### Skill Expression

- **Optimal Hideout Placement**: Skilled players identify strategically valuable caverns (near resources, trade routes, but hidden)
- **Support Structure Efficiency**: Minimizing support beams while maximizing safe cavern size
- **Discovery Timing**: Knowing when to reveal hidden bases (too early = lose advantage, too late = enemy too strong)
- **Collapse Timing**: Using collapse mechanics offensively (bury advancing army, seal retreat routes)

### Feedback to Player

- **Visual feedback**:
  - Underground layers visible when zoomed in or using "underground view" toggle
  - Bedrock layer distinct color (dark, unbreakable)
  - Hidden bases shown as question marks until discovered
  - Collapse animations (rumbling, falling debris)
- **Numerical feedback**:
  - Excavation progress bar
  - Collapse risk percentage
  - Undercity population and loyalty stats
- **Audio feedback**:
  - Digging sounds (pickaxe, drill)
  - Collapse rumble and crash
  - Ambient dripping/echoes in caverns

## Balance and Tuning

### Balance Goals

1. **Underground Advantage**: Hidden bases should feel secure but not invincible
2. **Discovery Risk**: Hidden bases should be discoverable with effort (scouting, spies)
3. **Excavation Investment**: Digging should take time/resources, not trivial
4. **Collapse Consequences**: Collapses should be impactful but not instant-death for all entities

### Tuning Knobs

1. **Bedrock Depth**: Deeper bedrock = more vertical space, but potentially unbalanced if too deep
2. **Excavation Speed**: Faster digging = quicker bases, but less strategic investment
3. **Collapse Threshold**: Lower threshold = more frequent collapses (more dangerous), higher = safer
4. **Discovery Chance Scaling**: How quickly hidden bases are discovered over time
5. **Undercity Independence Rate**: More independent undercities = more political complexity

### Known Issues

- **Bedrock Cheese**: If bedrock is too shallow, map feels cramped; too deep, infinite underground sprawl
- **Collapse Spam**: If player can easily trigger collapses, becomes dominant tactic (bury all enemies)
- **Hidden Base Stalemate**: If bases are too hard to discover, stalemates where factions hide forever
- **Undercity Micromanagement**: Managing multiple undercities could be tedious without proper abstraction (especially Space4X)

## Integration with Other Systems

### Interacts With

| System/Mechanic | Type of Interaction | Priority |
|----------------|---------------------|----------|
| **Terrain System** | Excavation modifies terrain voxels | High |
| **Faction/Aggregate System** | Undercities form independent factions | High |
| **Quest System** | Thieves guilds issue cavern-clearing quests | Medium |
| **Resource System** | Underground reveals ore veins, water | High |
| **Pathfinding** | Entities navigate vertical terrain | High |
| **Visibility System** | Surface entities can't see underground | Medium |
| **Miracles/Abilities** | Player can collapse, reveal, or reinforce | Medium |

### Emergent Possibilities

- **Underground Trade Networks**: Undercities form hidden trade routes, smuggling goods
- **Cavern Warfare**: Battles in tight, vertical spaces - different tactics than surface
- **Collapse Traps**: Lure enemies into unstable caverns, trigger collapse
- **Rebellion from Below**: Undercity factions revolt, attack surface from beneath
- **Resource Rush**: Discovery of rich ore vein triggers underground land grab
- **Sanctuary Cities**: Persecuted factions flee underground, form hidden refuges

## Implementation Notes

### Technical Approach

**Voxel/Layer System**:
```csharp
// Terrain supports vertical layers
public struct TerrainVoxel {
    public VoxelType Type; // Air, Dirt, Stone, Ore, Bedrock
    public float Hardness; // Excavation difficulty
    public bool IsSupported; // Has nearby support structure?
}

// Underground Space
public struct UndergroundSpace : IComponentData {
    public int2 SurfacePosition; // XZ position on surface
    public int Depth; // Y depth below surface
    public SpaceType Type; // Cavern, Undercity, Hideout
    public Entity OwningFaction; // Who controls this space
    public bool IsHidden; // Visible on map or hidden?
}

// Excavation Job
public struct ExcavationJob : IComponentData {
    public int3 TargetVoxel; // XYZ voxel to dig
    public float Progress; // 0.0-1.0
    public Entity Digger; // Entity performing dig
}

// Collapse Event
public struct CollapseEvent : IComponentData {
    public int3 EpicenterVoxel;
    public float Radius;
    public DynamicBuffer<Entity> AffectedEntities;
}
```

**System Flow**:
1. **TerrainLayerSystem**: Manages voxel grid, handles underground/surface layers
2. **ExcavationSystem**: Processes digging jobs, modifies voxels, discovers resources
3. **CollapseDetectionSystem**: Monitors unsupported voxels, triggers collapse events
4. **UndergroundSettlementSystem**: Spawns undercities, hideouts based on faction behavior
5. **DiscoverySystem**: Handles hidden base detection (scouts, spies, random chance)
6. **UndergroundPathfindingSystem**: Extends pathfinding to support vertical navigation

### Abstraction for Space4X

Space4X undercities are **abstracted as districts**:
- Not full 3D voxel excavation (too complex for space game)
- Undercity = special district type on station/colony
- Tracked as zone with faction ownership, population, resources
- Discovery/raids use simplified mechanics (sensor checks, assault actions)

### Performance Considerations

- **Voxel Grid Size**: Full 3D voxel grid can be expensive - use chunk-based storage, lazy-load underground layers
- **Collapse Simulations**: Large collapses affect many voxels - batch processing, limit frequency
- **Pathfinding**: 3D pathfinding more expensive than 2D - cache paths, use hierarchical pathfinding
- **Visibility Culling**: Don't render underground when viewing surface - toggle layer visibility

### Testing Strategy

1. **Unit tests for**:
   - Voxel excavation logic
   - Collapse detection (unsupported voxel count)
   - Hidden base discovery chance
   - Undercity faction formation

2. **Playtests should verify**:
   - Excavation feels rewarding (not too slow, not too fast)
   - Collapses are dramatic but fair
   - Hidden bases are discoverable with effort
   - Undercities create interesting political dynamics

3. **Balance tests should measure**:
   - Average time to excavate functional base
   - Frequency of collapses (should be rare but impactful)
   - Discovery rate of hidden bases over time
   - Undercity independence rate

## Examples

### Example Scenario 1: Thieves Guild Clears Cavern (Godgame)

**Setup**: Thieves guild forms in city, seeks hidden base. Large natural cavern 30m below city, infested with giant spiders.
**Action**: Guild issues quest "Clear the Deep Cavern" to adventurers. Player ignores quest. Guild sends own members (3 skilled thieves, 2 novices).
**Result**:
- Guild members engage spiders (1 novice dies, spiders cleared)
- Guild claims cavern, begins developing (storage, training area)
- Guild installs hidden entrance (trapdoor in city alley)
- Guild secures cavern (traps, guards)
- Cavern now appears on faction map as "Thieves Guild Hideout (Hidden)"
- Discovery chance: 5%/week (low traffic, well-hidden)

### Example Scenario 2: Player Collapses Enemy Tunnel (Godgame)

**Setup**: Enemy army digging tunnel to bypass city walls. Tunnel 10m deep, 50m long, approaching city.
**Action**: Player casts **Earthquake Miracle** targeting tunnel midpoint, high intensity.
**Result**:
- Collapse Event triggered at midpoint
- Radius: 20m (affects 40m of tunnel)
- 15 enemy soldiers in affected zone take 500 damage each (most die, buried)
- Tunnel sealed, enemy forced to surface route
- City defenders gain time to prepare
- Cost: 300 faith

### Example Scenario 3: Discovery of Undercity (Godgame)

**Setup**: Large city (pop 500) has hidden undercity (pop 80, independent faction). Undercity has been secret for 5 years, slowly growing.
**Action**: City guard increases patrols, scouts notice unusual traffic near old well. Investigation reveals hidden entrance.
**Result**:
- Undercity status changes from "Hidden" to "Discovered"
- Surface city leadership shocked, demands undercity submit to governance
- Undercity refuses (independent, corrupt leadership)
- Player faces decision:
  - Support surface city (crackdown, potential violence)
  - Support undercity (legitimize independence, anger surface)
  - Mediate (attempt peaceful integration)
- Choice affects faction relations, faith levels

### Example Scenario 4: Hollow Asteroid Base (Space4X)

**Setup**: Mining fleet scanning asteroid belt for resources. Sensor detects anomaly - hollow asteroid.
**Action**: Fleet investigates, finds pirate base inside (12 ships, stockpile of stolen cargo).
**Result**:
- Player decides to assault (could also negotiate or ignore)
- Fleet enters asteroid via narrow tunnel (limits ship deployment - tactical challenge)
- Battle in confined space (close-quarters combat favors smaller ships)
- Player wins, captures base
- Base converted to mining outpost (hidden from rivals)
- Stockpile looted (valuable resources)

## References and Inspiration

- **Dwarf Fortress**: Deep underground excavation, collapse mechanics, cavern layers
- **Minecraft**: Voxel-based digging, bedrock as unbreakable boundary
- **Terraria**: Vertical terrain exploration, underground biomes
- **X-COM Enemy Unknown**: Hidden alien bases, base assault missions
- **Crusader Kings 3**: Hidden societies, secret agendas
- **Thief series**: Thieves guild, hidden passages in cities

## Godgame-Specific Variations

### Miracle Interactions
- **Earthquake Miracle**: Triggers collapses, useful for offense/defense
- **Revelation Miracle**: Shows all hidden bases on map (expensive, rare)
- **Reinforcement Blessing**: Prevents collapses in blessed area
- **Excavation Blessing**: Increases digging speed for blessed entities

### Underground Faction Types
- **Hermits**: Peaceful cave dwellers, avoid surface
- **Smugglers**: Trade contraband between undercities
- **Cultists**: Worship dark gods in deep caverns
- **Refugees**: Fleeing persecution, hide underground

## Space4X-Specific Variations

### Abstracted Undercities
- Undercity = special district slot on station/colony
- Provides: Black market, espionage bonuses, criminal income
- Risks: Corruption, unrest, pirate attraction
- Can be cleared (police action) or tolerated (economic benefit)

### Hollow Asteroid Mechanics
- Detected via advanced sensors (tech upgrade)
- Can be claimed, developed into hidden base
- Defensible (limited entry points) but vulnerable if discovered
- Useful for covert operations, secret research, pirate suppression

### Sublevel Sectors
- Large stations have multiple levels (upper, mid, lower, sublevel)
- Sublevel = "undercity" equivalent
- Each level has different demographics, industries, crime rates
- Player can invest in sublevel development or neglect (leads to autonomy)

## Revision History

| Date | Change | Reason |
|------|--------|--------|
| 2025-12-01 | Initial draft | Conceptualization capture session |

---

*Last Updated: 2025-12-01*
*Document Owner: Tri-Project Design Team*
