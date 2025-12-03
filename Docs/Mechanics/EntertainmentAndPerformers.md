# Mechanic: Entertainment & Performance System

## Overview

**Status**: Concept
**Complexity**: Moderate
**Category**: Culture / Social / Economy

**One-line description**: *Conservatories, pavilions, and street performers employing dancers, musicians, bards, jesters, and poets to provide entertainment, morale, and cultural identity, with performances varying by faction outlook and alignment.*

## Core Concept

Villages and bands employ **entertainers** - artists who perform music, dance, poetry, comedy, and storytelling. Entertainment serves multiple purposes:
- **Morale Boost**: Happy entities work harder, fight better
- **Cultural Expression**: Different outlooks/alignments have distinct performance styles
- **Economic Activity**: Unemployed entities can become entertainers
- **Social Cohesion**: Performances bring entities together, strengthen community
- **Recruitment Tool**: Bands and armies employ entertainers to maintain morale

Entertainment venues range from **permanent buildings** (conservatories, pavilions) to **temporary structures** (bandstands, booths) to **street performances** (no building needed).

Inspired by **Pharaoh's entertainment system** - entities actively seek entertainment, venues must be staffed, performances have schedules.

## How It Works

### Basic Rules

1. **Entertainment Venues**: Buildings where performances occur
2. **Entertainer Jobs**: Entities can be employed as performers
3. **Performance Scheduling**: Performances happen at intervals (daily, weekly, special events)
4. **Audience Gathering**: Entities attend performances, receive morale boost
5. **Cultural Variation**: Performance style/content varies by faction outlook and alignment
6. **Quality Scaling**: Skill level affects performance quality (novice → master)

### Entertainment Venues

#### Godgame: Village Entertainment Buildings

| Venue Type | Size | Capacity | Entertainers Employed | Purpose |
|-----------|------|----------|---------------------|---------|
| **Street Booth** | Small | 10-20 | 1 performer | Quick morale, low investment |
| **Bandstand** | Small | 20-40 | 2-3 performers | Public square performances |
| **Pavilion** | Medium | 50-100 | 3-5 performers | Formal performances, festivals |
| **Dance School** | Medium | 30-50 (students) | 2 instructors, 5-10 dancers | Train dancers, public performances |
| **Conservatory** | Large | 100-200 | 5-10 musicians, 2-3 instructors | Elite music training, concerts |
| **Theater** | Large | 100-300 | 10-20 (actors, musicians, crew) | Plays, operas, grand performances |

**Street Performances** (No Building):
- Roaming performers (buskers, street poets)
- Perform in public squares, markets, taverns
- No fixed schedule, opportunistic
- Lower morale boost but free (no building cost)

#### Space4X: Fleet Entertainment Facilities

| Venue Type | Location | Capacity | Entertainers | Purpose |
|-----------|----------|----------|-------------|---------|
| **Rec Room** | Small ships | 5-10 crew | Holographic entertainment (automated) | Basic morale |
| **Lounge** | Medium ships | 20-50 crew | 1-2 live performers | Social gathering |
| **Concert Hall** | Large ships/stations | 100-500 | 5-10 performers | Fleet-wide events |
| **VR Arena** | Stations | Unlimited (virtual) | AI-generated + live hosts | Immersive entertainment |

### Entertainer Types

#### Godgame: Traditional Performers

| Entertainer Type | Primary Skill | Performance Style | Venues |
|-----------------|---------------|-------------------|--------|
| **Musician** | Music | Instrumental, vocals, ensembles | Conservatory, pavilion, bandstand, street |
| **Dancer** | Dance | Solo, partner, group choreography | Dance school, pavilion, street |
| **Bard** | Storytelling + Music | Epic tales, ballads, historical songs | Tavern, pavilion, campfire (bands) |
| **Poet** | Writing + Oration | Recitations, improvisation, satire | Pavilion, street, court |
| **Jester** | Comedy + Acrobatics | Slapstick, juggling, witty banter | Court, bandstand, street |
| **Actor/Actress** | Drama | Theater plays, tragedy, comedy | Theater |

**Multi-Class Performers**:
- Some entertainers master multiple disciplines (bard = music + storytelling)
- Master performers command higher wages, draw larger crowds

#### Space4X: Future Entertainers

| Entertainer Type | Primary Medium | Performance Style | Venues |
|-----------------|---------------|-------------------|--------|
| **Holographic Artist** | VR/AR | Immersive experiences | VR Arena |
| **Zero-G Dancer** | Dance (microgravity) | 3D choreography, aerial ballet | Concert Hall (low-G sections) |
| **Xenomusician** | Alien instruments | Cross-cultural fusion | Lounge, Concert Hall |
| **Comedian** | Stand-up, satire | Political humor, fleet life jokes | Lounge |
| **Storyteller** | Oral tradition | War stories, legends, fleet history | Lounge, Rec Room |

### Performance Styles by Outlook & Alignment

**Cultural Variation**: Entertainment reflects faction's outlook and alignment, creating distinct aesthetics.

#### Godgame: Outlook-Based Performance Themes

| Outlook | Music Style | Dance Style | Poetry Themes | Jester Comedy |
|---------|------------|-------------|---------------|---------------|
| **Radical** | Revolutionary anthems, protest songs | Energetic, defiant | Political satire, calls to action | Mocking authority, dark humor |
| **Traditional** | Classical, hymns, folk songs | Formal ballroom, ritual dances | Honor, duty, ancestry | Clean, family-friendly |
| **Progressive** | Experimental, fusion genres | Modern, interpretive | Innovation, future, exploration | Intellectual, observational |
| **Mystical** | Chanting, ethereal instruments | Trance-like, spiritual | Visions, prophecy, cosmic themes | Surreal, abstract |
| **Militaristic** | Marches, battle hymns | Regimented formations | War glory, heroism, sacrifice | Gallows humor, camaraderie |
| **Merchant** | Lively tavern songs, jingles | Entertaining, showy | Wealth, adventure, exotic lands | Witty, transactional |

#### Alignment Combos Affect Performance Tone

Examples:
- **Radical + Chaotic**: Punk-style performances, anarchic energy, provocative
- **Traditional + Lawful**: Refined, ceremonial, respectful
- **Progressive + Good**: Optimistic, uplifting, humanistic themes
- **Mystical + Evil**: Dark rituals, ominous chanting, unsettling dances
- **Militaristic + Neutral**: Professional, disciplined, patriotic but not fanatical

### Performance Scheduling & Attendance

**Performance Frequency**:
- **Street Booths/Bandstands**: Daily performances (morning, afternoon, evening)
- **Pavilions**: Weekly events (festival days, holidays)
- **Conservatories/Theaters**: Monthly grand performances
- **Special Events**: Celebrations (harvests, victories, holidays)

**Audience Behavior**:
- Entities with **low morale** actively seek entertainment
- Entities within range of performance gather as audience
- Attendance depends on:
  - Performance quality (skill of entertainers)
  - Cultural match (does performance align with entity's outlook?)
  - Availability (entities must have free time)
  - Venue appeal (grand theater > street booth)

**Morale Boost Calculation**:
```
Morale Boost = Base Boost × Performance Quality × Cultural Alignment × Venue Prestige
```

- **Base Boost**: 5-20 morale points
- **Performance Quality**: 0.5x (novice) to 2.0x (master)
- **Cultural Alignment**: 0.5x (mismatched) to 1.5x (perfectly aligned)
- **Venue Prestige**: 0.5x (street) to 2.0x (grand theater)

### Entertainers in Bands & Armies

**Mobile Entertainers**:
- Bands and armies employ **camp followers** - entertainers who travel with the group
- Perform at **campsites** during rest periods
- Boost morale before battles, during long marches
- Bards tell tales of past victories, inspire courage

**Camp Entertainment**:
- **Campfire Performances**: Bards, storytellers (evening, informal)
- **Pre-Battle Rallies**: Drummers, chanters (hype warriors)
- **Victory Celebrations**: Full ensemble (dancers, musicians, jesters)

**Mercenary Entertainers**:
- Some bands specialize in providing entertainment services
- Hired for festivals, celebrations, special events
- Roaming troupe mechanic (travel village to village)

### Employment for Unemployed & Undisciplined

**Unemployment Solution**:
- Entertainers provide **jobs for unskilled labor**
  - Novice musicians, dancers can be trained
  - Street performers require minimal investment
- **Undisciplined entities** excel at creative professions
  - Jesters, poets benefit from chaotic personalities
  - Less suited for regimented work (military, industry)

**Career Path**:
1. **Novice**: Street performer, untrained
2. **Apprentice**: Enrolled in conservatory/dance school
3. **Journeyman**: Professional performer, employed by venue
4. **Master**: Renowned artist, commands high wages, attracts crowds

### Parameters and Variables

| Parameter | Default Value | Range | Effect |
|-----------|--------------|-------|--------|
| **Performance Duration** | 30 minutes | 10-120 min | How long performance lasts |
| **Morale Boost (Base)** | 10 points | 5-20 | Base morale increase |
| **Audience Capacity** | Varies by venue | 10-300 | Max entities who can attend |
| **Performance Frequency** | Daily to monthly | Varies | How often performances occur |
| **Entertainer Wage** | 5 gold/day | 1-50 | Cost to employ entertainer |
| **Training Time (Novice→Master)** | 1 year | 6 months - 5 years | Skill progression duration |
| **Cultural Alignment Mult** | 0.5x - 1.5x | Varies | Effect of cultural match |

### Edge Cases

- **No Entertainers Available**: Venues sit empty, no performances, morale penalty
- **Mismatched Culture**: Radical audience at Traditional performance = low morale boost, potential discontent
- **Performer Dies Mid-Performance**: Performance cancelled, audience disappointed (minor morale penalty)
- **Overcrowding**: Audience exceeds venue capacity = some entities turned away, frustration
- **Roaming Troupe Arrives**: Temporary morale boost, competes with local entertainers
- **Performance During Crisis**: Should entertainers perform during siege, famine? (player choice)

## Player Interaction

### Player Decisions (Godgame)

- **Build Entertainment Venues?**: Invest in morale infrastructure vs. military/economy
- **Hire Entertainers?**: Allocate budget for culture vs. other needs
- **Cultural Curation**: Encourage specific performance styles (radical anthems vs. traditional hymns)
- **Schedule Grand Events**: Time major performances for key moments (pre-battle rally, post-victory celebration)

### Player Decisions (Space4X)

- **Fleet Morale Management**: Assign performers to flagships, long-duration missions
- **Cultural Officers**: Dedicate crew slots to entertainment specialists
- **Recreation Budget**: Allocate resources for holographic systems, live performers
- **Cross-Cultural Exchange**: Host alien performers for diplomatic bonuses

### Skill Expression

- **Morale Optimization**: Timing performances to maximize impact (before battles, during low morale periods)
- **Cultural Engineering**: Shaping faction identity through entertainment choices
- **Entertainer Recruitment**: Scouting talented performers, investing in training
- **Venue Placement**: Strategic placement for maximum coverage (central locations, high-traffic areas)

### Feedback to Player

- **Visual feedback**:
  - Performances visible in world (entities gathered, musician playing, dancer moving)
  - Venue icons show when performance scheduled (calendar UI)
  - Audience satisfaction animations (clapping, cheering, booing)
- **Numerical feedback**:
  - Morale boost displayed (floating numbers above audience)
  - Venue coverage map (which areas lack entertainment)
  - Entertainer skill levels (novice to master ratings)
- **Audio feedback**:
  - Actual music playing during performances (thematic to outlook)
  - Crowd reactions (applause, laughter, silence)
  - Ambient entertainment sounds near venues

## Balance and Tuning

### Balance Goals

1. **Meaningful Investment**: Entertainment should noticeably improve morale, justify cost
2. **Cultural Diversity**: Different outlooks should feel distinct (not just stat variations)
3. **Employment Utility**: Entertainers should meaningfully reduce unemployment
4. **Not Mandatory**: Villages can survive without entertainment, but thrive with it

### Tuning Knobs

1. **Morale Impact**: How much performances boost morale
2. **Cost vs Benefit**: Entertainer wages vs. morale value
3. **Cultural Alignment Sensitivity**: How much mismatch matters
4. **Venue Radius**: How far entities travel to attend performances
5. **Performance Frequency**: How often performances occur

### Known Issues

- **Entertainment Spam**: If too cheap, players build excessive venues (becomes optimal strategy)
- **Cultural Lock-In**: If alignment mismatch too punishing, discourages cultural diversity
- **Idle Entertainer Problem**: If performances infrequent, entertainers sit idle (wasted wages)
- **Morale Trivialization**: If too powerful, entertainment trivializes morale mechanics

## Integration with Other Systems

### Interacts With

| System/Mechanic | Type of Interaction | Priority |
|----------------|---------------------|----------|
| **Morale/Happiness System** | Performances boost morale | High |
| **Employment System** | Entertainers provide jobs | High |
| **Cultural/Faction System** | Performance style reflects outlook/alignment | High |
| **Calendar/Events System** | Performances scheduled on calendar | Medium |
| **Economy System** | Entertainers paid wages, consume resources | Medium |
| **Skill Progression** | Entertainers gain skill over time | Medium |
| **Special Events (Holidays)** | Grand performances during festivals | Medium |

### Emergent Possibilities

- **Rival Troupes**: Multiple entertainment groups compete for audiences, prestige
- **Cultural Revolution**: New performance styles emerge, shift faction identity
- **Entertainer Celebrities**: Master performers gain fame, influence politics
- **Performance Sabotage**: Rivals disrupt performances (hecklers, saboteurs)
- **Cross-Cultural Fusion**: Bands with mixed outlooks create hybrid performance styles
- **Propaganda via Performance**: Radical bards spread revolutionary messages through song
- **Entertainment Economy**: Villages specialize in entertainment, export performers

## Implementation Notes

### Technical Approach

**Entertainment System Components**:
```csharp
// Entertainment Venue
public struct EntertainmentVenue : IComponentData {
    public VenueType Type; // Booth, Bandstand, Pavilion, Conservatory, Theater
    public int Capacity; // Max audience size
    public FixedList128Bytes<Entity> EmployedEntertainers;
    public float NextPerformanceTime; // Scheduled performance time
}

// Entertainer Component
public struct Entertainer : IComponentData {
    public EntertainerType Type; // Musician, Dancer, Bard, Jester, Poet, Actor
    public float SkillLevel; // 0.0 (novice) to 1.0 (master)
    public Entity EmployerVenue; // Which venue employs this entertainer
    public OutlookID CulturalStyle; // Performance style matches this outlook
}

// Performance Event
public struct PerformanceEvent : IComponentData {
    public Entity Venue;
    public float Duration; // Seconds
    public float QualityMultiplier; // Based on entertainer skill
    public float MoraleBoost;
    public DynamicBuffer<Entity> Audience; // Entities attending
}

// Audience Member
public struct AudienceMember : IComponentData {
    public Entity PerformanceEvent;
    public float CulturalAlignmentMult; // How well performance matches entity's outlook
}

// Entertainment Seeker (behavior)
public struct EntertainmentSeeker : IComponentData {
    public float CurrentMorale;
    public float EntertainmentNeed; // 0.0-1.0, increases over time
    public OutlookID PreferredStyle;
}
```

**System Flow**:
1. **PerformanceSchedulerSystem**: Schedules performances at venues based on frequency
2. **AudienceGatheringSystem**: Entities with low morale or high entertainment need travel to nearby performances
3. **PerformanceExecutionSystem**: Runs performance, calculates quality based on entertainer skill
4. **MoraleBoostApplicationSystem**: Applies morale boost to audience members (scaled by cultural alignment)
5. **EntertainerTrainingSystem**: Increases entertainer skill over time (if employed by conservatory/school)
6. **CulturalStyleMatchingSystem**: Calculates alignment multiplier (entity outlook vs. performance style)

### Godgame-Specific: Street Performers

- **Roaming Performers**: Entertainers without venue employment wander village, perform in public squares
- **Busking Mechanics**: Entities tip performers (small gold donations)
- **Spontaneous Performances**: Street performers trigger randomly, opportunistic

### Space4X-Specific: Holographic Entertainment

- **Automated Entertainment**: Ships without live performers use holographic AI
  - Lower morale boost but no wage cost
  - No cultural variation (generic content)
- **Live Performer Premium**: Real entertainers provide superior morale, cultural expression

### Performance Considerations

- **Audience Pathfinding**: Efficiently route entities to performances (avoid pathfinding spam)
- **Performance Caching**: Cache performance quality calculations (reuse for same entertainer/venue)
- **Venue Coverage Queries**: Spatial hashing for "nearest venue" lookups
- **Cultural Match Lookups**: Hash table for outlook-to-performance-style mappings

### Testing Strategy

1. **Unit tests for**:
   - Morale boost calculation (skill × alignment × prestige)
   - Audience capacity limits
   - Cultural alignment multiplier
   - Entertainer skill progression

2. **Playtests should verify**:
   - Performances visually engaging (entities gather, react)
   - Cultural variation noticeable (radical vs traditional feels different)
   - Entertainment meaningfully improves morale
   - Unemployment reduction works

3. **Balance tests should measure**:
   - Cost-per-morale ratio vs. other morale sources
   - Entertainer wage sustainability
   - Venue ROI (return on investment)
   - Cultural mismatch impact

## Examples

### Example Scenario 1: Village Builds Bandstand (Godgame)

**Setup**: Village (pop 50, Traditional outlook), low morale (recent drought). Player builds **Bandstand** in public square.
**Action**:
- Bandstand costs 100 wood, 50 gold
- Player hires 2 musicians (5 gold/day each)
- Musicians are novice (skill 0.3)
**Result**:
- **First Performance**: Next morning, musicians perform
  - 15 villagers attend (within range, low morale)
  - Performance quality: 0.3 (novice skill) × 1.2 (cultural match - Traditional music for Traditional village) = 0.36
  - Morale boost: 10 (base) × 0.36 = 3.6 morale per attendee
  - Total morale gained: 15 attendees × 3.6 = 54 morale points (village-wide impact)
- **Over Time**: Musicians perform daily, gain skill (0.3 → 0.5 over 3 months)
- **Consequence**: Village morale stabilizes, drought hardship mitigated

### Example Scenario 2: Radical Bard in Traditional Village (Godgame)

**Setup**: Roaming bard (Radical outlook) arrives at Traditional village. Requests permission to perform in tavern.
**Action**:
- Player allows performance (curious about impact)
- Bard performs revolutionary anthems, political satire
**Result**:
- **Audience Reaction**:
  - Traditional villagers (majority): Low cultural alignment (0.5x mult) = minimal morale boost, some discomfort
  - 3 Radical-leaning villagers: High alignment (1.5x mult) = significant morale boost, inspired
- **Unintended Consequence**:
  - Radical villagers begin questioning Traditional leadership
  - Seeds of radicalization planted (long-term effect)
- **Player Decision**: Ban radical performers in future? Or allow for diversity?

### Example Scenario 3: Pre-Battle Rally (Godgame)

**Setup**: Army (100 warriors, Militaristic outlook) preparing for major battle. Army employs 2 bards, 3 drummers.
**Action**:
- Night before battle, entertainers perform **War Rally**
  - Bards sing battle hymns, tales of past victories
  - Drummers beat war drums, rhythmic energy
**Result**:
- **Morale Surge**:
  - Base boost: 15 morale
  - Cultural match (Militaristic performance for Militaristic army): 1.5x
  - Master bard (skill 0.9): 1.8x quality
  - Total boost: 15 × 1.5 × 1.8 = **40 morale per warrior**
- **Battle Impact**: Warriors enter battle with high morale, fight harder
- **Post-Battle**: If victory, bards celebrate with triumph songs. If defeat, morale impact lessens losses.

### Example Scenario 4: Fleet Lounge Concert (Space4X)

**Setup**: Long-duration deep space mission (6 months), crew morale declining. Flagship has **Concert Hall**, employs 5 musicians.
**Action**:
- Fleet commander schedules **Grand Concert** (monthly event)
- Musicians perform fusion jazz (Progressive cultural style)
**Result**:
- **Attendance**: 200 crew members (from multiple ships via shuttle)
- **Morale Boost**:
  - Base: 12 morale
  - Quality (skilled musicians): 1.5x
  - Cultural match (Progressive crew): 1.3x
  - Venue prestige (Concert Hall): 1.8x
  - Total: 12 × 1.5 × 1.3 × 1.8 = **42 morale per attendee**
- **Fleet-Wide Impact**: Morale crisis averted, crew remains functional for remaining mission duration

### Example Scenario 5: Conservatory Trains Master Musician (Godgame)

**Setup**: Village builds **Conservatory** (expensive, 500 gold + 200 wood). Enrolls 5 novice musicians.
**Action**:
- Conservatory employs 2 master instructors (20 gold/day each)
- Students train for 2 years (game time)
**Result**:
- **Training Progression**:
  - Year 1: Novice (0.2) → Apprentice (0.5)
  - Year 2: Apprentice (0.5) → Journeyman (0.7)
- **Graduation**: 3 students become journeyman musicians
  - Employed by conservatory for public concerts
  - 2 students leave to join roaming troupe (export talent)
- **Long-Term**: Village gains reputation as cultural center, attracts more students
- **ROI**: High initial investment but creates sustainable cultural infrastructure

## References and Inspiration

- **Pharaoh (1999)**: Entertainment buildings, walker mechanics, festival planning
- **Caesar III**: Cultural buildings, happiness mechanics
- **Crusader Kings 3**: Court positions (court musician, jester), feast events
- **The Sims**: Entertainment skill progression, performance quality
- **Stardew Valley**: Festival events, community gatherings
- **Warcraft 3**: Hero units with entertainment/morale abilities (bard units in custom maps)

## Godgame-Specific Variations

### Miracle Interactions
- **Blessing of Inspiration**: Divine miracle that boosts all entertainer skill temporarily (festivals)
- **Silence Curse**: Suppresses performances in target area (sabotage enemy morale)
- **Summon Muse**: Temporarily grants master-level skill to novice performer (rare miracle)

### Divine Performers
- **Prophets as Performers**: Religious leaders double as entertainers (sermons as performance)
- **Sacred Dances**: Ritual performances that also grant faith bonuses
- **Miracle Plays**: Theatrical reenactments of divine interventions (propaganda + entertainment)

## Space4X-Specific Variations

### Alien Entertainment
- **Xenomusicians**: Alien performers create exotic entertainment
  - Cross-species morale bonus (diplomacy benefit)
  - Cultural exchange events
- **Universal Translator Performances**: Live translation for multi-species audiences

### Military Applications
- **Psychological Warfare**: Broadcast propaganda performances to enemy fleets
- **Morale Officers**: Dedicated fleet role, combines command + entertainment
- **War Correspondents**: Journalist-entertainers document battles, create propaganda

## Revision History

| Date | Change | Reason |
|------|--------|--------|
| 2025-12-01 | Initial draft | Conceptualization capture session |

---

*Last Updated: 2025-12-01*
*Document Owner: Tri-Project Design Team*
