# Mechanic: Memories & Lessons System

## Overview

**Status**: Concept
**Complexity**: Complex
**Category**: Knowledge / Buffs / Culture / Social

**One-line description**: *Historic events and personal experiences stored as memories/lessons that provide context-triggered buffs, with cultural restrictions, focus costs, memory preservation mechanics, and social memory-trading during crises.*

## Core Concept

Entities possess **memories** - stored knowledge of historic events (cultural folklore) and personal experiences (learned lessons) that grant **buffs when activated**. Memories serve multiple purposes:
- **Passive Lessons**: Learned behaviors (rolling in sand to extinguish fire)
- **Active Buffs**: Focus-activated bonuses (dwarf slayer tales grant pain resistance)
- **Cultural Identity**: Racial/cultural memories define heritage
- **Social Bonding**: Entities share memories during crises (morale boost)

**Memory Types**:
1. **Cultural Memories**: Heroic tales, folklore, racial legends (shared by culture/race)
2. **Personal Memories**: Individual experiences, learned lessons (unique to entity)

**Memory Lifecycle**:
- **Creation**: Events create memories (battles, achievements, disasters)
- **Preservation**: Statues, monuments, newspapers maintain memories
- **Spread**: Entities teach memories to others, outlets disseminate
- **Fading**: Unused memories fade over time, require reminders
- **Erasure**: Hostile cultures destroy memory artifacts (cultural warfare)

## How It Works

### Basic Rules

1. **Memory Acquisition**: Entities gain memories through direct experience or learning from others
2. **Memory Activation**: Passive (automatic) or Active (spend focus to trigger buff)
3. **Context Triggering**: Relevant memories activate in appropriate situations (last stand memory during siege)
4. **Focus Cost**: Activating memories consumes focus (varies by loyalty, preservation quality)
5. **Cultural Restrictions**: Can only access own culture's memories (hybrids access both)
6. **Memory Trading**: During crises, entities share memories to find optimal buffs
7. **Memory Preservation**: Statues, decor, outlets maintain memory strength

### Memory Types

#### 1. Cultural Memories (Folklore & Legends)

Shared by all entities of same culture/race. Examples:

| Culture/Race | Memory Name | Trigger Condition | Buff | Focus Cost |
|-------------|------------|------------------|------|-----------|
| **Dwarf** | "Slayer's Resolve" | Low HP, surrounded | +50% pain resist, +stamina regen | 30 focus |
| **Human** | "Valiant Last Stand" | Outnumbered 3:1+ | +30% damage, +morale | 20 focus |
| **Elf** | "Ancient Forest Wisdom" | In forest terrain | +perception, +stealth | 15 focus |
| **Orc** | "Blood Frenzy" | Kill enemy | +attack speed, +damage (stacking) | 10 focus |
| **Undead** | "Endless March" | Long journey | No fatigue, +speed | 0 focus (passive) |

**Cultural Memory Characteristics**:
- **Shared Pool**: All entities of culture can access (if aware)
- **Preservation Dependent**: Statues, monuments boost power/reduce cost
- **Loyalty Scaled**: Higher loyalty = cheaper cost, stronger effect
- **Teachable**: Entities can teach cultural memories to younglings

#### 2. Personal Memories (Learned Lessons)

Unique to individual entity, gained from direct experience. Examples:

| Experience | Memory Learned | Trigger | Effect | Type |
|-----------|---------------|---------|--------|------|
| **Caught fire, rolled in sand** | "Sand Extinguishes" | On fire | Roll in sand automatically (no panic) | Passive |
| **Survived ambush** | "Never Trust Shadows" | Dark areas | +perception in darkness | Active (5 focus) |
| **Lost loved one in battle** | "Protect the Living" | Ally low HP | +healing effectiveness | Active (10 focus) |
| **Witnessed miracle** | "Divine Favor" | Prayer action | +faith generation | Passive |
| **Betrayed by friend** | "Trust No One" | Social interaction | +resist manipulation, -charisma | Passive (always on) |

**Personal Memory Characteristics**:
- **Unique**: Only this entity has this memory (unless taught)
- **Experience-Based**: Created through direct participation in events
- **Varied Power**: Some weak (minor tweaks), some strong (life-saving)
- **Shareable**: Entities can tell stories, teach others their lessons

#### 3. Hybrid Memories (Mixed Heritage)

Entities with hybrid heritage (half-elf, half-orc, etc.) can access **both cultures' memories** if they know them:

**Example: Half-Dwarf/Half-Human**
- Can activate **"Slayer's Resolve"** (dwarf) OR **"Valiant Last Stand"** (human)
- Must choose which cultural identity to lean into (can't activate both simultaneously)
- If raised in one culture, may not know other culture's memories (requires learning)

### Memory Activation Mechanics

#### Passive Activation (Learned Behaviors)

Automatic, no focus cost. Examples:
- **"Sand Extinguishes"**: Entity automatically rolls in sand when on fire (no panic delay)
- **"Parry Reflex"**: Entity parries incoming attacks slightly faster (muscle memory)
- **"Forage Efficiently"**: Entity finds food faster in wilderness

**Passive Memory Creation**:
- Repeating actions enough times creates passive memory
- Example: Entity caught fire 3 times, rolled in sand each time → Passive memory formed
- No focus cost, always active

#### Active Activation (Focus-Powered Buffs)

Requires focus expenditure, provides significant buffs. Examples:

**Activation Flow**:
1. **Trigger Condition Met**: Entity outnumbered 3:1 (last stand memory eligible)
2. **Entity Decides to Activate**: Spends 20 focus
3. **Buff Applied**: +30% damage, +morale for 60 seconds
4. **Duration Expires**: Buff ends, memory enters cooldown (2 minutes)

**Focus Cost Formula**:
```
Focus Cost = Base Cost × (1 - Loyalty Multiplier) × (1 - Preservation Multiplier)
```

- **Base Cost**: Memory's inherent cost (e.g., 20 focus for "Valiant Last Stand")
- **Loyalty Multiplier**: 0.0 (traitor) to 0.5 (fanatic) - loyal entities pay less
- **Preservation Multiplier**: 0.0 (forgotten) to 0.5 (well-preserved) - statues/outlets boost this

**Example**:
- Memory: "Valiant Last Stand" (Base: 20 focus)
- Entity: Loyal human (Loyalty: 0.4 multiplier)
- Village has statue commemorating last stand (Preservation: 0.3 multiplier)
- **Actual Cost**: 20 × (1 - 0.4) × (1 - 0.3) = 20 × 0.6 × 0.7 = **8.4 focus**

#### Context-Triggered Activation

Some memories auto-activate in specific situations:

| Memory | Context Trigger | Auto-Activate? | Effect |
|--------|----------------|---------------|--------|
| **"Last Stand"** | Outnumbered 3:1+ | Prompt entity (choose to activate) | +damage, +morale |
| **"Fire Safety"** | On fire | Auto-activate (passive) | Roll in sand/water immediately |
| **"Rally Cry"** | Ally dies nearby | Prompt entity | +morale to nearby allies |
| **"Survivor's Guilt"** | Sole survivor | Auto-activate | -morale, +determination |

### Memory Preservation & Spread

Memories are **not permanent** - they fade without reinforcement.

#### Preservation Methods

| Method | Effect | Preservation Strength | Example |
|--------|--------|---------------------|---------|
| **Retelling (Oral)** | Entity tells story to others | Weak (0.1 mult) | Elder tells younglings tales |
| **Statue/Monument** | Physical reminder, increases power | Strong (0.3-0.5 mult) | Statue of hero's last stand |
| **Decorations/Plaques** | Smaller reminders | Medium (0.2 mult) | Plaque on building |
| **Newspaper Article** | Written record, persistent | Medium (0.2 mult) | Historical feature article |
| **Festival/Ceremony** | Annual commemoration | Medium (0.25 mult) | "Last Stand Day" festival |
| **Aggregate Memory Bank** | Village/band collective memory | Strong (0.4 mult) | Village elder council preserves lore |

#### Memory Spread Mechanics

**Individual Spread**:
- Entities with memory can **teach** others (requires social interaction)
- Teaching success depends on:
  - Teacher's **charisma** (better storytellers spread faster)
  - Student's **loyalty** to culture (loyal learners remember better)
  - **Preservation quality** (well-preserved memories easier to teach)

**Aggregate Spread** (Village/Band-Level):
- Villages/bands store memories in collective **aggregate memory bank**
- Aggregate slowly disseminates memories to members (passive spread)
- New entities joining aggregate learn memories over time

**Outlet Spread** (Newspapers, Broadsheets):
- **Newspaper** publishes article about historic event (creates/reinforces memory)
- Ownership affects content:
  - **Private Owner**: Controls which memories published (propaganda potential)
  - **Village-Owned**: Council decides content (democratic memory curation)
  - **Chaotic Owner**: Publishes everything indiscriminately (free press, chaotic)
- Articles persist, entities reading newspaper gain/reinforce memories

#### Memory Fading

Memories **fade over time** if not reinforced:

**Fading Stages**:
1. **Fresh** (0-30 days): Full strength, no cost penalty
2. **Fading** (30-180 days): -10% strength, +10% focus cost
3. **Dim** (180-365 days): -30% strength, +30% focus cost
4. **Forgotten** (365+ days): Memory lost, must relearn

**Reinforcement Actions**:
- **Retelling**: Entity hears memory again (resets timer)
- **Re-experiencing**: Entity witnesses similar event (resets + strengthens)
- **Monument Visit**: Entity visits statue/monument (resets)
- **Newspaper Re-read**: Entity reads old article (resets)

### Memory Trading (Crisis Empowerment)

During **times of peril**, entities can **trade memories** to optimize group buffs.

#### Trading Mechanic

**Scenario**: Band of 5 humans faces 20 orcs (desperate situation).

**Memory Trading Flow**:
1. **Crisis Recognized**: System detects outnumbered situation
2. **Memory Pool**: Entities share relevant memories with group
   - Entity 1: "Valiant Last Stand" (+30% damage, 20 focus)
   - Entity 2: "Survivor's Fury" (+40% damage, -defense, 30 focus)
   - Entity 3: "Rally the Troops" (+morale to all, 25 focus)
   - Entity 4: "Last Stand" (weaker version, +20% damage, 15 focus)
   - Entity 5: "Desperation" (+speed, +damage vs. odds, 35 focus)
3. **Optimal Selection**: Group evaluates which memory provides best buff
   - **Best Choice**: Entity 2's "Survivor's Fury" (+40% damage)
4. **Entity 2 Activates**: Spends 30 focus, activates memory
5. **Morale Boost**: All entities gain +5 morale ("we have a chance!")
6. **Charisma Bonus**: Entity 2 has high charisma (+3 morale bonus)
   - **Total Morale**: +8 morale to entire group

**Trading Benefits**:
- **Optimal Buffs**: Group finds strongest memory for situation
- **Morale Synergy**: Sharing memories boosts morale
- **Social Bonding**: Entities feel united, "we remember together"

**Trading Restrictions**:
- **Cultural Barriers**: Can't trade cultural memories outside culture (hybrid exception)
- **Personal Memories**: Can be shared via storytelling (temporary access)
- **Focus Limitation**: Only entity with memory can activate (others can't spend their focus)

### Memory Erasure (Cultural Warfare)

Hostile cultures **actively destroy memories** to weaken enemies.

#### Erasure Methods

| Method | Target | Effect | Example |
|--------|--------|--------|---------|
| **Destroy Statues** | Monument | Memory preservation -50% | Raiders topple hero statue |
| **Burn Newspapers** | Outlet archives | Memory source destroyed | Raid newspaper office, burn records |
| **Kill Elders** | Key memory keepers | Lose rare/ancient memories | Assassinate village elder (lore custodian) |
| **Cultural Suppression** | Prohibit retelling | Memory spread halted | Occupiers ban cultural stories |
| **Rewrite History** | Propaganda | Corrupt memory, change narrative | False newspaper claims hero was traitor |

#### Defensive Preservation

Cultures defend memories:
- **Hidden Archives**: Secret locations store backup records
- **Oral Tradition Redundancy**: Multiple elders know same memories
- **Monument Fortification**: Protect statues with guards
- **Encrypted Outlets**: Hidden printing presses, underground newspapers

### Memory in Aggregates (Villages/Bands)

Aggregates (villages, bands, factions) have **collective memory banks**.

#### Aggregate Memory Storage

**Village Memory Bank**:
- Stores all **cultural memories** of dominant culture
- Stores **historic events** (battles, achievements, disasters)
- Stores **personal memories** of notable individuals (heroes, leaders)

**Memory Dissemination**:
- New villagers slowly learn village memories (passive spread, 1-2 per month)
- Cultural festivals accelerate spread (mass teaching event)
- Newspaper circulation spreads memories to readers

**Memory Stability**:
- Villages with **elder councils** preserve memories better (+0.2 preservation)
- Villages with **monuments** have stronger memory retention
- Villages under **occupation** lose memories faster (suppression)

### Parameters and Variables

| Parameter | Default Value | Range | Effect |
|-----------|--------------|-------|--------|
| **Memory Fading Time** | 180 days | 30-365 days | Time before memory fades |
| **Focus Cost (Base)** | 20 | 5-50 | Base focus to activate memory |
| **Loyalty Discount** | 0.0-0.5x | 0-50% | How much loyalty reduces cost |
| **Preservation Boost** | 0.0-0.5x | 0-50% | How much preservation reduces cost |
| **Buff Duration** | 60 seconds | 10-300s | How long memory buff lasts |
| **Cooldown** | 120 seconds | 30-600s | Time before memory can reactivate |
| **Teaching Success Rate** | 70% | 30-95% | Chance entity learns taught memory |
| **Morale Boost (Trading)** | 5 | 1-15 | Morale gained from memory trading |
| **Charisma Bonus** | +1 per 10 charisma | 0-10 | Extra morale from charismatic entity |

### Edge Cases

- **Hybrid Learns Both Cultures**: Half-elf raised by elves knows elf lore, later learns human lore from humans (can access both)
- **Memory Conflict**: Entity has contradictory memories (e.g., "Trust Others" vs. "Trust No One") - most recent overrides
- **False Memories**: Propaganda creates false memories (enemy statue claims hero was villain) - entities with real memory resist
- **Memory Overload**: Entity has too many memories (max capacity) - oldest/weakest memories fade first
- **Stolen Memories**: Magic/tech allows reading others' memories (controversial, ethical dilemma)
- **Memory Corruption**: Dark magic corrupts memory (e.g., "Valiant Last Stand" becomes "Cowardly Retreat")

## Player Interaction

### Player Decisions (Godgame)

- **Build Monuments?**: Invest in memory preservation (cost vs. cultural strength)
- **Control Newspapers?**: Influence which memories spread (propaganda vs. free press)
- **Cultural Festivals**: Schedule memory-reinforcing events (cost faith/resources)
- **Protect Elders**: Ensure key memory keepers survive (military defense)

### Player Decisions (Space4X)

- **Historical Archives**: Build data centers to preserve fleet/faction memories
- **Propaganda Campaigns**: Use memories for morale/recruitment (ethical choice)
- **Memory Implants**: Tech to artificially implant memories (controversial)
- **Cultural Exchange**: Share memories with alien allies (diplomacy bonus)

### Skill Expression

- **Memory Timing**: Activating optimal memory at perfect moment (combat advantage)
- **Memory Curation**: Choosing which memories to preserve (cultural shaping)
- **Memory Trading**: Identifying best memory during crisis (tactical optimization)
- **Preservation Strategy**: Balancing monuments, outlets, oral tradition (resource allocation)

### Feedback to Player

- **Visual feedback**:
  - Memory activation visual effect (entity glows, eyes flash, aura appears)
  - Buff icon above entity (showing active memory)
  - Monument/statue visual in world (memory preservation indicator)
  - Newspaper icon when entity reads article (memory learning)
- **Numerical feedback**:
  - Focus cost displayed before activation
  - Buff magnitude and duration shown
  - Memory fading timer (days until forgotten)
  - Preservation quality percentage
- **Audio feedback**:
  - Memory activation sound (whisper, chime, echo)
  - Morale boost fanfare (when trading memories)
  - Monument building completion (cultural triumph)

## Balance and Tuning

### Balance Goals

1. **Meaningful Buffs**: Memories should noticeably impact combat/survival
2. **Cultural Identity**: Memories should reinforce faction themes
3. **Not Mandatory**: Entities can survive without memories, but thrive with them
4. **Preservation Investment**: Building monuments should feel worthwhile

### Tuning Knobs

1. **Buff Magnitude**: How strong memory buffs are (game-changing vs. minor)
2. **Focus Cost**: How expensive memories are to activate (frequent vs. rare use)
3. **Fading Rate**: How quickly memories fade (punishing vs. forgiving)
4. **Preservation Strength**: How much monuments help (major vs. minor)
5. **Trading Morale**: How much morale boost from memory trading (impactful vs. negligible)

### Known Issues

- **Memory Spam**: If too many memories, entities constantly buffed (trivializes challenge)
- **Preservation Meta**: If monuments too powerful, becomes mandatory investment
- **Cultural Lock-Out**: Hybrid entities might be overpowered (access to 2x memories)
- **Memory Loss Frustration**: If fading too fast, players constantly re-teaching (tedious)

## Integration with Other Systems

### Interacts With

| System/Mechanic | Type of Interaction | Priority |
|----------------|---------------------|----------|
| **Buff System** | Memories grant buffs | High |
| **Focus System** | Memories cost focus | High |
| **Culture/Faction System** | Cultural memories define identity | High |
| **Loyalty System** | Loyalty affects memory cost/power | High |
| **Monument/Wonder System** | Monuments preserve memories | Medium |
| **Newspaper/Outlet System** | Outlets spread memories | Medium |
| **Elder/Leader System** | Leaders preserve/teach memories | Medium |
| **Morale System** | Memory trading boosts morale | Medium |

### Emergent Possibilities

- **Memory Wars**: Factions raid to destroy enemy memories (cultural genocide)
- **Memory Black Market**: Sell rare memory access (magic/tech memory transfer)
- **Memory Cults**: Factions obsessed with preserving specific memory (religious devotion)
- **False History**: Propaganda creates false memories, rewriting past (1984-style)
- **Memory Champions**: Entities with exceptional memory capacity become living archives
- **Memory Synergy**: Multiple memories combine for unique buffs (combo system)
- **Forgotten Heroes**: Ancient memories resurface when statue discovered (archaeological lore)

## Implementation Notes

### Technical Approach

**Memory System Components**:
```csharp
// Memory Definition
public struct MemoryDef {
    public FixedString64Bytes Name;
    public MemoryType Type; // Cultural, Personal, Hybrid
    public FixedString64Bytes CulturalOrigin; // Dwarf, Human, Elf, etc.
    public TriggerCondition Trigger; // When memory can activate
    public BuffEffect Buff; // What buff memory provides
    public int BaseFocusCost; // Base cost to activate
    public float Duration; // Seconds
    public float Cooldown; // Seconds
    public bool IsPassive; // Auto-activate?
}

// Entity Memory Tracker
public struct EntityMemories : IComponentData {
    public DynamicBuffer<MemoryInstance> KnownMemories; // Memories this entity knows
    public float FocusCurrent; // Available focus
    public float FocusMax; // Max focus
}

// Memory Instance (entity's knowledge of memory)
public struct MemoryInstance : IBufferElementData {
    public MemoryDef Def;
    public float LastActivated; // Time since last use
    public float LastReinforced; // Time since last reminder
    public float FadingProgress; // 0.0 (fresh) to 1.0 (forgotten)
    public float PreservationBonus; // From monuments, outlets
}

// Active Memory Buff
public struct ActiveMemoryBuff : IComponentData {
    public MemoryDef Memory;
    public float RemainingDuration;
    public BuffEffect ActiveBuff;
}

// Aggregate Memory Bank (Village/Band)
public struct AggregateMemoryBank : IComponentData {
    public DynamicBuffer<MemoryDef> CollectiveMemories;
    public float PreservationQuality; // 0.0-1.0
}

// Memory Preservation Structure (Monument, Outlet)
public struct MemoryPreservation : IComponentData {
    public FixedString64Bytes MemoryName; // Which memory this preserves
    public float PreservationStrength; // 0.0-0.5
    public bool IsDestroyed; // Has it been destroyed?
}

// Memory Trading Event
public struct MemoryTradingEvent : IComponentData {
    public DynamicBuffer<Entity> Participants; // Who's trading
    public DynamicBuffer<MemoryDef> SharedMemories; // Pool of memories
    public Entity OptimalMemoryOwner; // Who has best memory
    public MemoryDef OptimalMemory; // Best memory for situation
}
```

**System Flow**:
1. **MemoryAcquisitionSystem**: Entities gain memories from experiences, teaching, outlets
2. **MemoryFadingSystem**: Tracks memory age, applies fading penalties
3. **MemoryActivationSystem**: Detects trigger conditions, prompts/activates memories
4. **MemoryBuffApplicationSystem**: Applies buff effects when memory activated
5. **MemoryTradingSystem**: During crises, facilitates memory trading, morale boost
6. **MemoryPreservationSystem**: Monuments/outlets boost preservation quality
7. **MemorySpreadSystem**: Entities teach memories, aggregates disseminate
8. **MemoryErasureSystem**: Handles monument destruction, memory loss

### Godgame-Specific: Oral Tradition

- **Elder Storytelling**: Elders gather younglings, tell tales (mass memory teaching)
- **Campfire Rituals**: Bands share memories around campfires (social bonding)
- **Miracle-Enhanced Memory**: Player can bless entity to permanently retain memory (expensive)

### Space4X-Specific: Data Archives

- **Memory Implants**: Tech to artificially implant memories (instant learning, ethical concerns)
- **Holographic Archives**: Store memories as holographic recordings (perfect preservation)
- **Alien Memory Exchange**: Share memories with alien allies (cross-cultural understanding)

### Performance Considerations

- **Memory Cap per Entity**: Limit max memories (e.g., 20) to avoid memory bloat
- **Aggregate Memory Caching**: Cache aggregate memory bank, recalculate only on change
- **Fading Checks**: Calculate fading once per day (not every frame)
- **Trading Event Pooling**: Memory trading events are rare, pool for efficiency

### Testing Strategy

1. **Unit tests for**:
   - Focus cost calculation (loyalty, preservation)
   - Memory fading progression (time-based)
   - Cultural restriction enforcement (only own culture)
   - Trading optimal memory selection

2. **Playtests should verify**:
   - Memories feel impactful (buffs noticeable)
   - Cultural identity reinforced (dwarf memories feel dwarven)
   - Preservation mechanics intuitive (monuments = stronger memories)
   - Trading empowerment satisfying (crisis cooperation)

3. **Balance tests should measure**:
   - Memory usage frequency (are they being used?)
   - Focus economy (entities have enough focus?)
   - Preservation ROI (are monuments worth building?)
   - Trading morale impact (does it matter?)

## Examples

### Example Scenario 1: Human Last Stand Memory (Godgame)

**Setup**: 10 human soldiers face 35 orc raiders. Outnumbered 3.5:1.
**Action**:
- **Trigger**: Outnumbered 3:1+ detected
- **Memory Trading**: Soldiers share memories
  - Soldier 1: "Valiant Last Stand" (+30% damage, 20 focus)
  - Soldier 2: "Hold the Line" (+defense, 25 focus)
  - Soldier 3: "Desperate Fury" (+40% damage, -defense, 30 focus)
- **Optimal Choice**: Soldier 3's "Desperate Fury" (highest damage)
- **Activation**: Soldier 3 spends 30 focus, activates memory
- **Morale Boost**: All soldiers gain +5 morale, Soldier 3 has high charisma (+3 bonus) = **+8 morale total**
**Result**:
- Soldier 3 deals +40% damage for 60 seconds
- All soldiers inspired, fight harder
- **Battle Outcome**: Humans hold out long enough for reinforcements to arrive
- **Memory Reinforcement**: Survivors' "Last Stand" memory strengthened (easier to activate next time)

### Example Scenario 2: Dwarf Slayer Memory Activation (Godgame)

**Setup**: Dwarf warrior (50 HP, surrounded by 5 goblins).
**Action**:
- **Trigger**: Low HP (<30%) + surrounded
- **Memory**: "Slayer's Resolve" (cultural dwarf memory)
  - Base Cost: 30 focus
  - Dwarf Loyalty: High (0.4 discount)
  - Village has Slayer Statue (0.3 preservation bonus)
  - **Actual Cost**: 30 × (1 - 0.4) × (1 - 0.3) = 30 × 0.6 × 0.7 = **12.6 focus**
- **Activation**: Dwarf spends 13 focus, activates "Slayer's Resolve"
**Result**:
- Buff: +50% pain resistance, +stamina regen for 90 seconds
- Dwarf fights through injuries, kills 3 goblins before falling
- **Last Words**: "I die as a Slayer!" (reinforces cultural memory for witnesses)

### Example Scenario 3: Personal Fire Safety Memory (Godgame)

**Setup**: Villager caught fire 3 times in past, rolled in sand each time.
**Action**:
- **Memory Formation**: After 3rd incident, **passive memory created**: "Sand Extinguishes"
- **Next Fire Event**: Villager catches fire from wildfire
- **Automatic Response**: Memory activates (passive), villager immediately rolls in nearby sand
**Result**:
- **No Panic**: Villager doesn't waste time panicking (saves 2-3 seconds)
- **Faster Extinguish**: Knows exactly what to do, fire out in 1 second vs. 5 seconds
- **Survival**: Villager survives with minor burns instead of death

### Example Scenario 4: Memory Erasure Raid (Godgame)

**Setup**: Orc raiders attack human village. Orcs know humans draw strength from "Last Stand" statue.
**Action**:
- **Raid Objective**: Destroy statue (primary target)
- Orcs bypass military targets, rush to town square
- **Statue Destroyed**: Toppled and smashed
**Result**:
- **Preservation Loss**: "Valiant Last Stand" memory loses 0.3 preservation bonus
- **Focus Cost Increase**: Activating memory now costs 20 (base) instead of 14 (discounted)
- **Morale Penalty**: Villagers demoralized, "our history destroyed" (-10 morale)
- **Long-Term**: If not rebuilt, memory fades faster (humans forget over time)

### Example Scenario 5: Newspaper Memory Spread (Godgame)

**Setup**: Village newspaper publishes article "The Hero's Last Stand" (commemorates historic battle).
**Action**:
- **Publication**: Article written by village-owned newspaper
- **Distribution**: 50 villagers read article over 1 week
- **Memory Gain**: Readers who didn't know "Last Stand" memory now learn it
- **Memory Reinforcement**: Readers who already knew memory have it reinforced (reset fading timer)
**Result**:
- **Memory Spread**: 20 new villagers gain "Last Stand" memory
- **Preservation Boost**: Article stored in archive (+0.2 preservation)
- **Cultural Strength**: Village identity reinforced, unity increased

### Example Scenario 6: Hybrid Memory Access (Godgame)

**Setup**: Half-elf/half-human warrior, raised by elves, later lives among humans.
**Action**:
- **Elf Upbringing**: Knows "Ancient Forest Wisdom" (elf cultural memory)
- **Human Contact**: Learns "Valiant Last Stand" from human comrades
- **Crisis**: Faces orcs in forest (outnumbered)
**Result**:
- **Memory Options**:
  - "Ancient Forest Wisdom" (+perception, +stealth in forest) - 15 focus
  - "Valiant Last Stand" (+damage, +morale when outnumbered) - 20 focus
- **Optimal Choice**: Activate both? (Can't, only one active memory at a time)
- **Decision**: Activates "Last Stand" (more relevant to combat), uses forest stealth from training (not memory-based)

## References and Inspiration

- **Crusader Kings 3**: Legacy system, cultural traditions, historical memory
- **Total War**: Unit experience, battle memories, morale mechanics
- **Darkest Dungeon**: Stress/affliction system with learned behaviors
- **Dragon Age**: Codex entries, cultural lore, racial bonuses
- **The Last of Us**: Survivor skills, learned combat techniques
- **Assassin's Creed**: Genetic memory (Animus), ancestral knowledge

## Godgame-Specific Variations

### Miracle Interactions
- **Memory Blessing**: Divine miracle to permanently preserve memory (expensive)
- **Collective Memory**: Miracle to instantly teach memory to all villagers
- **Forgotten Memory Reveal**: Miracle to recover lost memories from ancient times

### Memory Types Unique to Godgame
- **Divine Memories**: Witnessed miracles (faith bonuses)
- **Tragedy Memories**: Survived disasters (resilience buffs)
- **Victory Memories**: Won battles (combat bonuses)
- **Agricultural Memories**: Farming wisdom (harvest bonuses)

## Space4X-Specific Variations

### Memory Technologies
- **Neural Implants**: Instant memory transfer (download skills)
- **Holographic Archives**: Perfect memory preservation (no fading)
- **Shared Consciousness**: Fleet-wide memory pool (hive mind lite)
- **Memory Encryption**: Protect memories from espionage (security tech)

### Memory Types Unique to Space4X
- **Combat Logs**: Battle data analysis (tactical bonuses)
- **Exploration Memories**: Discovered systems (navigation bonuses)
- **Diplomatic Memories**: Past negotiations (diplomacy bonuses)
- **Engineering Feats**: Great achievements (construction bonuses)

## Revision History

| Date | Change | Reason |
|------|--------|--------|
| 2025-12-01 | Initial draft | Conceptualization capture session |

---

*Last Updated: 2025-12-01*
*Document Owner: Tri-Project Design Team*
