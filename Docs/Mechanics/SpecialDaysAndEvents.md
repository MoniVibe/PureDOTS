# Mechanic: Special Days & Recurring Events

## Overview

**Status**: Concept
**Complexity**: Moderate
**Category**: Events / World Systems / Calendar

**One-line description**: *Evolving holidays, blood moon-style events, and seasonal occurrences that modify spawns, loot, behaviors, and world state on predictable or random schedules.*

## Core Concept

The game world has a **living calendar** of special days and events:
- **Holidays**: Cultural celebrations (harvests, religious festivals, commemorations)
- **Blood Moons**: Dangerous nights with increased enemy spawns, modified behaviors
- **Celestial Events**: Eclipses, meteor showers, planetary alignments
- **Seasonal Shifts**: Spring bloom, winter freeze, monsoon season
- **Historical Anniversaries**: Commemorating past victories, tragedies, or milestones

These events create **rhythm and variation** in gameplay - players anticipate, prepare for, and capitalize on these occurrences. Some events are fixed (annual harvest), others emergent (victory celebration after major battle), and some random (blood moon chance each night).

## How It Works

### Basic Rules

1. **Calendar System**: Game tracks in-game time (days, months, seasons, years)
2. **Event Scheduling**: Events trigger on fixed dates, random rolls, or emergent conditions
3. **Event Effects**: Modify game systems (spawns, loot, behaviors, resources, etc.)
4. **Event Duration**: Last from hours to weeks depending on type
5. **Event Evolution**: New holidays can be created by player/entity actions, old ones fade if forgotten

### Event Categories

| Category | Trigger | Frequency | Examples |
|----------|---------|-----------|----------|
| **Holidays** | Fixed date or cultural milestone | Annual or milestone-based | Harvest Festival, Victory Day |
| **Blood Moons** | Random chance each night | ~5% per night | Increased spawns, hostile behavior |
| **Celestial Events** | Astronomical calculation | Predictable (eclipses) or rare (comets) | Solar eclipse, meteor shower |
| **Seasonal Events** | Season change | Quarterly | Spring bloom, winter freeze |
| **Emergent Events** | Player/entity actions | Varies | Founding Day, Mourning Day |

### Godgame: Event Types

#### Holidays

| Holiday | Trigger | Effects | Player Interaction |
|---------|---------|---------|-------------------|
| **Harvest Festival** | Autumn (fixed date) | +Faith generation, villagers celebrate, trade bonuses | Bless crops for extra yields |
| **Winter Solstice** | Winter (fixed date) | Darkness longer, cold debuffs, festive decorations | Provide warmth miracle, gift-giving |
| **Victory Day** | Emerges after major battle | +Morale, military recruitment bonus, parades | Bless warriors, inspire speeches |
| **Mourning Day** | Emerges after tragedy | -Morale, villagers grieve, low productivity | Comfort miracle, memorial blessing |
| **Founder's Day** | Annual (village founding date) | Villagers reflect on history, reinforce cultural identity | Reveal village history, strengthen traditions |

#### Blood Moons (Dangerous Nights)

- **Trigger**: Random 5% chance each night, or guaranteed on specific cursed dates
- **Visual**: Moon turns red, ambient lighting shifts crimson
- **Effects**:
  - **Enemy Spawn Rate**: 3x normal
  - **Enemy Aggression**: Enemies seek out villages actively (instead of wandering)
  - **Loot Quality**: Defeated enemies drop 2x loot
  - **Villager Behavior**: Villagers hide indoors (fear), some become frenzied (rare)
- **Duration**: Full night (8-12 game hours)
- **Warning**: Sky turns orange at dusk (2-hour warning)

#### Celestial Events

| Event | Trigger | Effects | Rarity |
|-------|---------|---------|--------|
| **Solar Eclipse** | Predictable (astronomical) | Darkness during day, nocturnal enemies active, magic potency +50% | Yearly |
| **Meteor Shower** | Predictable or random | Meteor impacts (terrain damage, rare ore deposits), spectacle | Every 5 years |
| **Planetary Alignment** | Rare (astronomical) | Massive magic boost, miracles cost -50%, rare creatures spawn | Every 50 years |
| **Aurora Nights** | Random (seasonal) | Beautiful lights, +Faith generation, peaceful night (no enemies) | Monthly (winter) |

#### Seasonal Events

- **Spring Bloom**: Crops grow 2x faster, animals reproduce, flowers bloom (beauty buff)
- **Summer Heat**: Water consumption increases, droughts possible, fire spread risk
- **Autumn Harvest**: Crops yield peak, storage critical, prepare for winter
- **Winter Freeze**: Crops dormant, food scarcity, cold debuffs, ice fishing

### Space4X: Event Types

#### Holidays

| Holiday | Trigger | Effects | Player Interaction |
|---------|---------|---------|-------------------|
| **Fleet Day** | Annual (fleet commissioning date) | +Recruitment, naval pride, parades | Host celebrations, bonuses for ships |
| **Treaty Anniversary** | Peace treaty date | Diplomatic bonuses, trade events | Reinforce alliances, commemorative missions |
| **Remembrance Day** | Emerges after major loss | Morale penalty, memorial services, unity bonus | Honor the fallen, memorial construction |
| **First Contact Day** | Emerges after meeting new species | Cultural exchange, science bonuses | Diplomatic missions, cultural sharing |

#### Cosmic Hazards (Blood Moon Equivalent)

- **Solar Flare Storm**:
  - **Trigger**: Random 5% chance per solar cycle, or scripted events
  - **Visual**: Sun flares violently, space turns orange/red
  - **Effects**:
    - **Shield Disruption**: Shields -50% effectiveness
    - **Sensor Interference**: Detection range -50%
    - **Pirate Activity**: Pirates 3x more aggressive
    - **Salvage Opportunity**: Derelict ships appear (damaged by flare)
  - **Duration**: 1-3 days
  - **Warning**: Solar observatory detects 1 day advance (if player has observatory)

- **Nebula Drift**:
  - **Trigger**: Nebula cloud drifts into inhabited system (rare)
  - **Effects**: Visibility reduced, exotic matter spawns, anomalies appear
  - **Duration**: Weeks to months
  - **Strategic**: Cover for stealth operations, hazard for navigation

#### Celestial Events

| Event | Trigger | Effects | Rarity |
|-------|---------|---------|--------|
| **Planetary Conjunction** | Predictable | Gravity anomalies, jump drive efficiency +30% | Yearly |
| **Black Hole Pulse** | Rare (if near black hole) | Gravitational waves, time dilation, exotic phenomena | Decades |
| **Supernova Shockwave** | Scripted event | Massive radiation, evacuation needed, rare elements appear | Once per campaign |
| **Comet Passage** | Predictable or random | Ice/water deposits, beautiful visuals, inspiration bonus | Every 10 years |

#### Seasonal Events (if planetary colonies)

- **Growing Season**: Planetside colonies produce +50% food
- **Storm Season**: Planet-side infrastructure at risk, repair costs increase
- **Migration Season**: Native fauna migrate, hunting opportunities or hazards

### Event Evolution (Emergent Holidays)

**How New Holidays Emerge**:
1. **Significant Event Occurs**: Major battle, natural disaster, first contact, etc.
2. **Cultural Memory**: Entities remember event (stored in cultural knowledge)
3. **Anniversary Trigger**: On anniversary, entities commemorate (small celebration)
4. **Reinforcement**: If player/entities reinforce commemoration (build memorial, hold ceremony), becomes permanent holiday
5. **Tradition**: Holiday repeats annually, gains cultural significance

**How Holidays Fade**:
1. **Lack of Observance**: If holiday is ignored for multiple years
2. **Cultural Shift**: New generation doesn't value old traditions
3. **Player Intervention**: Player actively suppresses holiday (forbid celebration, destroy memorials)

**Example**: After first successful defense against raiders, village commemorates "Defense Day" annually. If player blesses celebration yearly, becomes major holiday. If ignored, fades after 3-5 years.

### Parameters and Variables

| Parameter | Default Value | Range | Effect |
|-----------|--------------|-------|--------|
| **Blood Moon Chance** | 5% per night | 0-20% | How often dangerous nights occur |
| **Blood Moon Spawn Mult** | 3.0x | 1.5x-5.0x | Enemy spawn increase during event |
| **Holiday Bonus Magnitude** | +25% | 10%-100% | Strength of holiday bonuses |
| **Celestial Event Frequency** | Varies (yearly to decades) | Rare to common | How often celestial events occur |
| **Seasonal Duration** | 30 days | 7-90 days | How long each season lasts |
| **Emergent Holiday Threshold** | 3 commemorations | 1-10 | How many observances before holiday becomes permanent |
| **Holiday Fade Timer** | 5 years ignored | 1-20 years | How long before forgotten holiday fades |

### Edge Cases

- **Multiple Events Overlap**: Blood moon during harvest festival - effects stack (chaotic celebration)
- **Player Suppresses Cultural Holiday**: Villagers resent, faith decreases, radicalization risk
- **Solar Flare During Critical Battle (Space4X)**: Shields down, sensors jammed - dramatic tactical shift
- **Permanent Event**: Some events can become permanent (e.g., nebula never leaves - new normal)
- **Event Cancellation**: Player uses miracle/ability to stop event (dispel blood moon, prevent eclipse) - high cost

## Player Interaction

### Player Decisions (Godgame)

- **Prepare for Blood Moon**: Stock defenses, bless warriors, or hide and wait
- **Celebrate Holidays**: Amplify bonuses with miracles, or ignore to conserve resources
- **Create New Traditions**: Commemorate player-chosen events, shaping culture
- **Exploit Celestial Events**: Use eclipse for magic-intensive miracles, meteor shower for ore gathering

### Player Decisions (Space4X)

- **Schedule Operations Around Events**: Plan offensives during conjunction (jump efficiency), avoid solar flares
- **Harvest Event Resources**: Collect exotic matter during nebula drift, salvage derelicts after flares
- **Respect or Ignore Holidays**: Celebrate Fleet Day for morale boost, or push fleet hard (risk morale penalty)
- **Trigger Events**: Late-game tech might allow triggering events (induce solar flare on enemy, summon comet)

### Skill Expression

- **Calendar Mastery**: Skilled players track event schedules, plan months in advance
- **Event Exploitation**: Using eclipses for stealth ops, blood moons for loot farming
- **Cultural Engineering**: Shaping emergent holidays to reinforce desired values (militarism, peace, faith)
- **Crisis Management**: Responding to overlapping disasters (blood moon + winter freeze + raider attack)

### Feedback to Player

- **Visual feedback**:
  - Calendar UI shows upcoming events (icons, countdowns)
  - Sky/space changes color during events (red moon, orange sun, nebula clouds)
  - World decorations during holidays (banners, lights, parades)
- **Numerical feedback**:
  - Event bonuses/penalties displayed in UI (icons with % modifiers)
  - Countdown timers for event start/end
  - Historical log of past events
- **Audio feedback**:
  - Ambient music changes during events (ominous for blood moon, festive for holidays)
  - Celebration sounds during holidays (cheering, fireworks)
  - Warning alarms for hazardous events (solar flare alert)

## Balance and Tuning

### Balance Goals

1. **Anticipation**: Events should be exciting to anticipate, not annoying interruptions
2. **Preparation Rewards**: Players who prepare should benefit significantly
3. **Variety**: Events should feel different, not repetitive
4. **Emergent Stories**: Events should create memorable moments

### Tuning Knobs

1. **Event Frequency**: More frequent = more dynamic but potentially overwhelming
2. **Effect Magnitude**: Stronger effects = more impactful but risks disrupting balance
3. **Warning Time**: Longer warnings = more prep opportunity, shorter = more reactive
4. **Randomness vs Predictability**: More random = exciting, more predictable = strategic planning

### Known Issues

- **Blood Moon Farming**: If loot is too good, players intentionally trigger/wait for blood moons (becomes optimal strategy)
- **Holiday Spam**: Too many holidays = constant bonuses, diminishes special feel
- **Event Fatigue**: If events are too frequent or similar, become tedious
- **Unfair Timing**: Random events during critical moments can feel unfair (e.g., solar flare during final battle)

## Integration with Other Systems

### Interacts With

| System/Mechanic | Type of Interaction | Priority |
|----------------|---------------------|----------|
| **Calendar/Time System** | Events scheduled on calendar | High |
| **Spawn System** | Blood moons modify spawn rates/types | High |
| **Loot System** | Events modify loot quality/quantity | Medium |
| **Faction/Culture System** | Holidays reflect cultural identity | High |
| **Weather System** | Celestial events affect weather | Medium |
| **Buff System** | Events apply temporary buffs/debuffs | High |

### Emergent Possibilities

- **Event Chains**: Meteor shower → ore rush → economic boom → new holiday (Prosperity Day)
- **Cultural Conflicts**: Two factions celebrate rival holidays on same day, tension rises
- **Strategic Event Use**: Trigger solar flare to disable enemy fleet, time invasion during eclipse
- **Forgotten Tragedies**: Old mourning day fades, tragedy forgotten, history repeats
- **Event Synergy**: Eclipse during harvest festival → "Dark Harvest" legendary event, unique bonuses

## Implementation Notes

### Technical Approach

**Calendar & Event System**:
```csharp
// Calendar Singleton
public struct GameCalendar : IComponentData {
    public int DayOfYear; // 0-364 (or custom)
    public int Year;
    public Season CurrentSeason;
}

// Event Definition
public struct EventDef {
    public FixedString64Bytes EventName;
    public EventType Type; // Holiday, BloodMoon, Celestial, etc.
    public TriggerCondition Trigger; // FixedDate, Random, Emergent
    public int Duration; // Days
    public EventEffects Effects;
}

// Scheduled Event
public struct ScheduledEvent : IComponentData {
    public EventDef Def;
    public int StartDay;
    public int EndDay;
    public bool IsActive;
}

// Active Event Tracker
public struct ActiveEventBuffer : IBufferElementData {
    public Entity EventEntity;
}

// Event Effects
public struct EventEffects {
    public float SpawnRateMultiplier;
    public float LootQualityMultiplier;
    public float FaithGenerationModifier;
    public bool DisableNormalSpawns;
    // ... more effect fields
}

// Emergent Holiday Tracker
public struct EmergentHoliday : IComponentData {
    public FixedString64Bytes EventDescription;
    public int FirstOccurrenceDay;
    public int TimesObserved;
    public bool IsPermanent;
}
```

**System Flow**:
1. **CalendarAdvancementSystem**: Increments day counter, detects season changes
2. **EventSchedulerSystem**: Checks for event triggers (fixed dates, random rolls, emergent conditions)
3. **EventActivationSystem**: Activates events, applies effects to relevant systems
4. **EventDeactivationSystem**: Ends events, cleans up effects
5. **EmergentHolidaySystem**: Tracks cultural memory, promotes commemorations to permanent holidays
6. **EventFeedbackSystem**: Updates UI with active events, upcoming events, warnings

### Godgame-Specific: Cultural Memory

- Entities remember significant events (stored in knowledge/memory components)
- On anniversary, entities gather to commemorate (behavior trigger)
- Player can bless commemoration (miracle) to reinforce cultural importance
- Holidays become part of faction culture (identity marker)

### Space4X-Specific: Astronomical Calculations

- Celestial events calculated based on orbital mechanics (if using realistic system)
- Solar flares based on star type, age, activity level
- Comet/asteroid orbits simulated (or faked with believable schedules)

### Performance Considerations

- **Event Checks**: Only check event triggers once per day (not every frame)
- **Effect Application**: Batch effect modifications (all blood moon spawns in one job)
- **Calendar UI**: Cache upcoming events, update only when day changes

### Testing Strategy

1. **Unit tests for**:
   - Event scheduling (correct dates, random rolls)
   - Effect application (spawn multipliers, loot modifiers)
   - Emergent holiday promotion (commemoration threshold)
   - Holiday fade logic (years ignored)

2. **Playtests should verify**:
   - Events feel exciting and impactful
   - Preparation is rewarding
   - Frequency feels right (not too common, not too rare)
   - Emergent holidays create meaningful culture

3. **Balance tests should measure**:
   - Blood moon loot vs normal gameplay (should be significant but not exploitable)
   - Holiday bonus impact on economy/military
   - Event overlap frequency (should be rare but possible)

## Examples

### Example Scenario 1: Blood Moon Defense (Godgame)

**Setup**: Village (pop 50), night approaching. Blood moon warning at dusk.
**Action**:
- Player sees sky turn orange (2-hour warning)
- Player casts **Protection Blessing** on village walls (30% damage reduction)
- Villagers barricade doors, warriors arm themselves
- Night falls, moon turns red
**Result**:
- Normal spawn rate: 5 enemies/night
- Blood moon spawn rate: 15 enemies (3x multiplier)
- Enemies actively seek village (no wandering)
- Battle lasts 6 hours (game time)
- 12 enemies defeated, 3 escaped
- Loot: 2x normal (24 items vs usual 12)
- 2 villagers wounded, 0 killed (blessing helped)
- **Consequence**: Village earns reputation as "Blood Moon Defenders," morale boost

### Example Scenario 2: Harvest Festival with Player Blessing (Godgame)

**Setup**: Autumn, harvest festival (fixed date). Village prepared feast.
**Action**:
- Harvest festival triggers automatically
- Base effects: +25% faith generation, villagers celebrate, trade bonus
- Player casts **Bountiful Harvest Miracle** (amplifies festival)
**Result**:
- Faith generation: +50% (base 25% + miracle 25%)
- Crop yield: +30% bonus (miracle effect)
- Villagers extremely happy, some convert to devout followers
- Festival lasts 3 days (extended by miracle)
- **Consequence**: Village holds "Great Harvest" as legendary event, creates new tradition "Blessing of Bounty Day" (emergent holiday)

### Example Scenario 3: Solar Flare Storm (Space4X)

**Setup**: Player fleet (20 ships) patrolling border. Solar observatory detects flare 1 day out.
**Action**:
- Player orders fleet to fallback position (sheltered by planet)
- Enemy pirates unaware (no observatory), remain in open space
**Result**:
- Solar flare hits
- Player fleet: Shields -50%, but in shelter, minimal damage
- Pirate fleet (15 ships): Shields -50%, fully exposed, heavy damage
  - 3 pirate ships disabled, 2 destroyed
  - Remaining pirates scattered
- **Player Opportunity**: Launch attack on weakened pirates
  - 10 pirate ships destroyed/captured
  - Salvage: 2x normal loot (flare-damaged derelicts)
- **Consequence**: Pirate threat eliminated, player gains salvage windfall

### Example Scenario 4: Emergent Victory Day (Godgame)

**Setup**: Village defeats massive raid (50 attackers vs 30 defenders), legendary battle.
**Action**:
- Battle occurs on Day 120 of Year 5
- Villagers spontaneously commemorate victory (cultural memory)
- Year 6, Day 120: Villagers gather at battlefield, remember fallen
- Player casts **Memorial Blessing** (builds monument, blesses ceremony)
**Result**:
- "Victory Day" observed 3 years in row (player reinforces each time)
- Year 8: Holiday becomes permanent tradition
- **Effects (annual)**:
  - +Military recruitment bonus (+20%)
  - +Morale for warriors
  - Parade through village, speeches
  - Villagers honor veterans
- **Cultural Impact**: Village identity shifts to "warrior culture"

## References and Inspiration

- **Terraria**: Blood moons, eclipses, seasonal events
- **Don't Starve**: Full moons, seasonal hazards (winter freeze, summer heat)
- **Stardew Valley**: Seasonal festivals, town celebrations
- **Majora's Mask**: Time-limited events, moon crisis
- **Stellaris**: Anomalies, random events, empire-wide modifiers
- **Crusader Kings 3**: Cultural traditions, feast days, historical events

## Godgame-Specific Variations

### Miracle-Event Interactions
- **Dispel Blood Moon**: High-cost miracle to end blood moon early (500 faith)
- **Summon Eclipse**: Trigger solar eclipse for magic boost (rare, endgame miracle)
- **Bless Festival**: Amplify holiday bonuses (varies by festival)
- **Create Holiday**: Player declares new holiday (requires high faith, cultural influence)

### Villager Traditions
- Villagers request player to bless festivals (faith donation)
- Ignoring festivals decreases faith
- Creating too many holidays causes "holiday fatigue" (diminishing returns)

## Space4X-Specific Variations

### Cosmic Hazards
- **Ion Storm**: Sensors offline, communication disrupted
- **Asteroid Shower**: Random impacts, mining opportunity
- **Dimensional Anomaly**: Wormholes appear/disappear, shortcuts or traps
- **Stellar Wind**: Ships buffeted, course deviations, fuel consumption increase

### Late-Game Event Control
- **Solar Flare Inducer**: Weapon that triggers flare on enemy system (war crime?)
- **Comet Tug**: Redirect comet to asteroid belt for resources or enemy colony (siege)
- **Planetary Shield**: Protect colonies from cosmic hazards (expensive infrastructure)

## Revision History

| Date | Change | Reason |
|------|--------|--------|
| 2025-12-01 | Initial draft | Conceptualization capture session |

---

*Last Updated: 2025-12-01*
*Document Owner: Tri-Project Design Team*
