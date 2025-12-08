# Cultural Holidays and Celebrations (Agnostic Framework)

**Status:** Concept Design
**Category:** Core Cultural Framework
**Shareability:** `shared-core`
**Last Updated:** 2025-12-07

---

## Overview

The **Cultural Holidays Framework** provides agnostic mechanics for recurring calendar-based celebrations that reinforce group identity and cohesion. Games implement specific holiday types, celebration behaviors, and historical event catalogs while PureDOTS provides the calendar scheduling, participation tracking, identity reinforcement algorithms, and historical event recording systems.

**Agnostic Aspects** (Provided by PureDOTS):
- ✅ Calendar event scheduling framework
- ✅ Holiday component structure
- ✅ Historical event recording system
- ✅ Participation calculation algorithms
- ✅ Identity reinforcement formulas (patriotism/loyalty accumulation)
- ✅ Cultural relevance decay/growth curves
- ✅ Celebration effect application framework

**Game-Specific Aspects** (Implemented by Games):
- Holiday types (victory days, miracles, emancipations, harvests)
- Celebration activities (feasting, rituals, parades, games)
- Historical event catalogs (what constitutes a significant event)
- Visual presentation (VFX, animations, audio for celebrations)
- Cultural variants (different cultures celebrate different things)

---

## Core Agnostic Components

### CulturalHoliday (Mind ECS)
```csharp
/// <summary>
/// Agnostic holiday definition
/// Games define type/activity enums
/// </summary>
public struct CulturalHoliday : IComponentData
{
    public Entity Culture;                // Group that celebrates this
    public byte HolidayTypeId;            // Game-defined enum

    // Calendar Timing (agnostic)
    public int Month;                     // 1-12
    public int Day;                       // 1-30
    public int DurationDays;              // 1-7

    // Historical Context
    public Entity HistoricalEvent;        // Link to original event
    public uint CreationTick;
    public int YearsSinceFounding;

    // Importance and Relevance (agnostic metrics)
    public float Importance;              // 0-1
    public float CulturalRelevance;       // 0-1
    public float IdentityBonus;           // Identity gain per celebration

    // Participation
    public float ParticipationRate;       // 0-1
    public bool IsMandatory;
}
```

### HolidayCelebrationState (Body ECS)
```csharp
/// <summary>
/// Agnostic celebration participation state
/// </summary>
public struct HolidayCelebrationState : IComponentData
{
    public Entity ActiveHoliday;
    public bool IsCelebrating;
    public float CelebrationProgress;     // 0-1
    public uint CelebrationStartTick;

    public byte ActivityId;               // Game-defined enum
    public float MoraleBonus;             // Current temp bonus
    public float IdentityGain;            // Accumulated identity
}
```

### HistoricalEvent (Mind ECS)
```csharp
/// <summary>
/// Agnostic historical event record
/// </summary>
public struct HistoricalEvent : IComponentData
{
    public byte EventTypeId;              // Game-defined enum
    public Entity AffectedGroup;          // Culture/faction/nation
    public uint OccurredTick;
    public float Significance;            // 0-1

    // Context (optional)
    public Entity KeyFigure;              // Hero/leader (optional)
    public Entity Location;               // Where it happened

    // Holiday Creation
    public bool CreatedHoliday;
    public Entity HolidayEntity;
}
```

### GroupIdentity (Mind ECS)
```csharp
/// <summary>
/// Agnostic group identity/loyalty component
/// Games use for patriotism, faction loyalty, etc.
/// </summary>
public struct GroupIdentity : IComponentData
{
    public Entity Group;                  // Culture/faction/nation
    public float Identity;                // 0-100 (loyalty/patriotism)
    public float IdentityGrowthRate;      // Per year

    // Holiday Influence
    public int CelebrationsAttended;      // Lifetime count
    public float LastCelebrationBonus;
}
```

### HolidayParticipants (Mind ECS Buffer)
```csharp
/// <summary>
/// Agnostic participant tracking
/// </summary>
[InternalBufferCapacity(128)]
public struct HolidayParticipants : IBufferElementData
{
    public Entity Participant;
    public byte ActivityId;               // Game-defined enum
    public float ContributionLevel;       // 0-1
}
```

---

## Agnostic Algorithms

### Event Significance Calculation
```csharp
/// <summary>
/// Calculate if historical event should create holiday
/// Agnostic: Games provide scale/impact values
/// </summary>
public static float CalculateEventSignificance(
    byte eventTypeId,
    int entitiesAffected,
    bool hadKeyFigure,
    float culturalImpact,
    float baseSignificanceForType)
{
    // Scale factor based on entities affected
    float scaleFactor = math.min(entitiesAffected / 100f, 2.0f);

    // Key figure bonus (heroes, leaders make events more memorable)
    float heroBonus = hadKeyFigure ? 0.2f : 0f;

    // Combine factors
    float significance = baseSignificanceForType * scaleFactor + heroBonus + culturalImpact;

    return math.clamp(significance, 0f, 1f);
}

public static bool ShouldCreateHoliday(float significance)
{
    return significance >= 0.6f;
}
```

### Participation Chance Calculation
```csharp
/// <summary>
/// Calculate entity's likelihood to participate
/// Agnostic: Based on identity level and holiday importance
/// </summary>
public static float CalculateParticipationChance(
    float entityIdentity,        // 0-100
    float holidayImportance,     // 0-1
    bool isMandatory)
{
    float baseChance = 0.5f;

    // Identity influence (high identity = more participation)
    float identityBonus = (entityIdentity / 100f) * 0.3f;

    // Importance influence
    float importanceBonus = holidayImportance * 0.2f;

    // Mandatory holidays
    if (isMandatory)
        baseChance = 0.9f;

    return math.clamp(baseChance + identityBonus + importanceBonus, 0.1f, 0.98f);
}
```

### Celebration Effects Application
```csharp
/// <summary>
/// Apply agnostic celebration bonuses
/// </summary>
public static void ApplyCelebrationEffects(
    ref float morale,
    ref GroupIdentity identity,
    CulturalHoliday holiday,
    float participationLevel)
{
    // Temporary morale boost (magnitude scaled by importance)
    float moraleBonus = holiday.Importance * 20f * participationLevel;
    morale = math.min(morale + moraleBonus, 100f);

    // Permanent identity gain
    float identityGain = holiday.IdentityBonus * participationLevel;
    identity.Identity = math.min(identity.Identity + identityGain, 100f);

    // Track participation
    identity.CelebrationsAttended++;
    identity.LastCelebrationBonus = identityGain;
}
```

### Cultural Relevance Decay/Growth
```csharp
/// <summary>
/// Update holiday relevance over time
/// Agnostic: Old holidays fade unless reinforced by participation
/// </summary>
public static void UpdateHolidayRelevance(
    ref CulturalHoliday holiday,
    float currentParticipation,
    int yearsSinceFounding)
{
    // Age decay (old holidays naturally fade)
    float agePenalty = math.min(yearsSinceFounding / 500f, 0.5f);

    // Participation reinforcement (celebrated holidays stay relevant)
    float participationBonus = currentParticipation * 0.3f;

    // Update relevance
    float newRelevance = holiday.CulturalRelevance - agePenalty + participationBonus;
    holiday.CulturalRelevance = math.clamp(newRelevance, 0.1f, 1.0f);

    // Very low relevance = holiday may be forgotten
    if (holiday.CulturalRelevance < 0.2f && yearsSinceFounding > 200)
    {
        holiday.Importance *= 0.9f; // Gradual fade
    }
}
```

### Identity Growth Rate Calculation
```csharp
/// <summary>
/// Calculate identity growth from holiday participation
/// Agnostic: Frequent participation builds identity faster
/// </summary>
public static float CalculateIdentityGrowthRate(
    int celebrationsPerYear,
    float averageParticipationLevel)
{
    // Base growth from holiday participation
    float baseGrowth = celebrationsPerYear * 0.5f;

    // Participation quality modifier
    float qualityModifier = averageParticipationLevel;

    // Total growth rate (identity points per year)
    return baseGrowth * qualityModifier;
}
```

---

## Extension Points for Games

### 1. Holiday Type Definitions
Games define holiday type enums:
```csharp
// Godgame example
public enum GodgameHolidayType : byte
{
    VictoryDay,
    LiberationDay,
    MiracleDay,
    SaintDay,
    FoundingDay,
    EmancipationDay,
    HarvestFestival,
    SolsticeCelebration,
}

// Space4X example
public enum Space4XHolidayType : byte
{
    IndependenceDay,
    FirstContact,
    TechBreakthrough,
    ColonyFounding,
    VictoryCommemoration,
    MartyrRemembrance,
}
```

### 2. Event Type Definitions
Games define what events are significant:
```csharp
// Godgame example
public enum GodgameEventType : byte
{
    MilitaryVictory,
    DefensiveTriumph,
    DivineIntervention,
    Emancipation,
    CityFounding,
    HeroicDeed,
    NaturalPhenomenon,
}

// Space4X example
public enum Space4XEventType : byte
{
    MajorBattle,
    TerritoryLiberation,
    ScientificDiscovery,
    FirstColony,
    AlienContact,
    Revolution,
}
```

### 3. Celebration Activity Definitions
Games define what entities do during celebrations:
```csharp
// Godgame example
public enum GodgameCelebrationActivity : byte
{
    Feasting,
    Praying,
    Rituals,
    Parading,
    Storytelling,
    Games,
    Pilgrimage,
    MonumentVisiting,
}

// Space4X example
public enum Space4XCelebrationActivity : byte
{
    CeremonyAttendance,
    MemorialVisit,
    MilitaryParade,
    PublicGathering,
    VirtualCommemoration,
}
```

---

## Integration Requirements

### Systems Games Must Implement

1. **HolidayCreationSystem** (Mind ECS, 1 Hz)
   - Evaluate `HistoricalEvent` entities for significance
   - Create `CulturalHoliday` entities when threshold met
   - Assign calendar dates (anniversary of event)

2. **HolidaySchedulerSystem** (Mind ECS, 1 Hz)
   - Check current in-game date against holiday calendar
   - Activate holidays when date matches
   - Track multi-day celebrations (duration)

3. **ParticipationCalculationSystem** (Mind ECS, 1 Hz)
   - Calculate participation chance for each entity
   - Create `HolidayCelebrationState` for participating entities
   - Populate `HolidayParticipants` buffer

4. **CelebrationActivitySystem** (Body ECS, 60 Hz)
   - Execute celebration behaviors (game-specific animations/VFX)
   - Move entities to celebration locations
   - Handle activity state machines (feasting → praying → parading)

5. **CelebrationEffectsSystem** (Mind ECS, 1 Hz)
   - Apply morale bonuses (temporary)
   - Apply identity/patriotism bonuses (permanent)
   - Update `GroupIdentity` components

6. **HolidayRelevanceSystem** (Mind ECS, 1 Hz, yearly)
   - Update cultural relevance based on participation rates
   - Decay importance of old, rarely celebrated holidays
   - Archive forgotten holidays (relevance < 0.1)

7. **HistoricalEventRecordingSystem** (Mind ECS, 1 Hz)
   - Detect significant events (battles, discoveries, etc.)
   - Create `HistoricalEvent` entities
   - Calculate event significance

---

## Data Contracts

Games must provide:
- Holiday type catalog (types, base significance values)
- Event type catalog (what events create holidays)
- Activity type definitions (celebration behaviors)
- Calendar system integration (date/time tracking)
- Morale/identity component definitions

---

## Game-Specific Implementations

### Godgame (Fantasy Cultural Celebrations)
**Full Implementation:** [Cultural_Holidays_And_Celebrations.md](../../../../Godgame/Docs/Concepts/Culture/Cultural_Holidays_And_Celebrations.md)

**Holiday Types:** Victory days, miracles, emancipations, saint days, harvest festivals
**Celebration Activities:** Feasting, prayers, rituals, pilgrimages, storytelling
**Identity Metric:** Cultural Patriotism (0-100)
**Effects:** Morale bonuses, patriotism growth, cultural cohesion

### Space4X (Sci-Fi National Holidays)
**Implementation Reference:** TBD

**Holiday Types:** Independence days, first contact anniversaries, tech breakthroughs
**Celebration Activities:** Ceremonies, military parades, memorial visits
**Identity Metric:** Faction Loyalty (0-100)
**Effects:** Morale bonuses, loyalty growth, recruitment bonuses

---

## Performance Targets

**Body ECS (60 Hz) Budget:** 1-2 ms/frame
- Celebration activities: 1.0 ms (animation/movement updates)
- Activity state machines: 0.5 ms (behavior transitions)

**Mind ECS (1 Hz) Budget:** 30-40 ms/update
- Holiday creation: 5 ms (rare events only)
- Holiday scheduling: 10 ms (monthly calendar checks)
- Participation calculation: 10 ms (all entities)
- Effects application: 10 ms (batch processing)
- Relevance updates: 5 ms (yearly only)

**Aggregate ECS (0.2 Hz) Budget:** 50-70 ms/update
- Culture-wide statistics: 30 ms
- Regional analysis: 20 ms
- Holiday conflicts: 20 ms

**Optimization Strategies:**
- Spatial partitioning for celebration grouping
- Lazy evaluation (only calculate when holiday active)
- Batch effect application (per culture, not per entity)
- Event pooling for historical records
- Archive old holidays (relevance < 0.1) after 500 years

---

## Testing Guidelines

### Unit Tests (PureDOTS)
- ✅ Event significance calculation (various scales, impacts)
- ✅ Participation chance formula (identity, importance, mandatory)
- ✅ Celebration effect application (morale, identity bonuses)
- ✅ Relevance decay curve (time, participation influence)
- ✅ Holiday creation threshold (significance >= 0.6)

### Integration Tests (Games)
- Holiday creation from historical events (verify threshold)
- Annual holiday recurrence (correct dates, durations)
- Participation cascades (high identity → high participation)
- Identity accumulation over years (verify growth rates)
- Holiday fading (old holidays losing relevance over centuries)

---

## Migration Notes

**New Components Required:**
- `CulturalHoliday` (Mind ECS)
- `HolidayCelebrationState` (Body ECS)
- `HistoricalEvent` (Mind ECS)
- `GroupIdentity` (Mind ECS)
- `HolidayParticipants` buffer (Mind ECS)

**Integration with Existing Systems:**
- Calendar system integration (date/time tracking)
- Morale system integration (temporary bonuses)
- Culture system integration (group affiliation)
- Historical records system (event logging)

---

## Related Documents

**PureDOTS Agnostic:**
- `Docs/Architecture/ThreePillarECS_Architecture.md` - ECS layers (to be created)
- `Docs/BestPractices/DOTS_1_4_Patterns.md` - DOTS coding patterns

**Game Implementations:**
- `Godgame/Docs/Concepts/Culture/Cultural_Holidays_And_Celebrations.md` - Full game-side concept
- `Space4X/Docs/Concepts/Culture/National_Holidays.md` - Space variant (to be created)

---

**Last Updated:** 2025-12-07
**Maintainer:** PureDOTS Core Team
**Status:** Awaiting Implementation
