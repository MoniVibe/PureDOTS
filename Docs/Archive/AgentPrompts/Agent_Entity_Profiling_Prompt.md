# Agent: Entity Profiling & Individualization System

## Status: 🟡 STUBS CREATED - Ready for Implementation

## Scope
Implement the entity profiling system that transforms blank entities into game-specific individuals (Godgame villagers or Space4X crew/officers) by applying archetypes, stats, alignments, outlooks, and behaviors.

## Core Concept

**Blank Entity Contract**: Every entity starts as just an `Entity ID` with no assumptions. The profiling system attaches the right components to make it a "villager" (Godgame) or "individual" (Space4X).

**Profile Application Flow**:
```
Blank Entity
    ↓ Apply Archetype
Archetype Assignment (VillagerArchetypeAssignment)
    ↓ Resolve Archetype Data
Resolved Profile (VillagerArchetypeResolved)
    ↓ Apply Base Stats
Core Stats (IndividualStats)
    ↓ Apply Derived Stats
Derived Attributes (DerivedAttributes)
    ↓ Apply Alignment/Outlook/Behavior
Identity Components (EntityAlignment, EntityOutlook, PersonalityAxes)
    ↓ Apply Game-Specific Extensions
Game-Specific Stats (Godgame: Social Stats, Space4X: Officer Stats)
```

---

## Stub Files to Implement

### Entity Profiling Service (3 files)
- ✅ `Runtime/Stubs/EntityProfilingStub.cs` → `Runtime/Identity/EntityProfilingService.cs`
- ✅ `Runtime/Stubs/EntityProfilingStubComponents.cs` → `Runtime/Identity/EntityProfilingComponents.cs`
- ✅ `Runtime/Stubs/EntityProfilingStubSystems.cs` → `Systems/Identity/EntityProfilingSystem.cs`

**Requirements:**
- `ApplyArchetype(in Entity entity, FixedString64Bytes archetypeName)` - Assign archetype to blank entity
- `ApplyProfile(in Entity entity, EntityProfile profile)` - Apply complete profile
- `CreateVillager(in Entity entity, VillagerProfile profile)` - Godgame-specific villager creation
- `CreateIndividual(in Entity entity, IndividualProfile profile)` - Space4X-specific individual creation
- `ResolveArchetype(in Entity entity)` - Resolve archetype from catalog
- `ApplyBaseStats(in Entity entity, IndividualStats stats)` - Apply core stats
- `ApplyDerivedStats(in Entity entity)` - Calculate and apply derived attributes
- `ApplyAlignment(in Entity entity, EntityAlignment alignment)` - Set alignment
- `ApplyOutlook(in Entity entity, EntityOutlook outlook)` - Set outlook tags
- `ApplyPersonality(in Entity entity, PersonalityAxes personality)` - Set personality axes

---

## Component Requirements

### 1. Entity Profile Component

```csharp
public struct EntityProfile : IComponentData
{
    public FixedString64Bytes ArchetypeName;
    public EntityProfileSource Source;  // Template, Generated, PlayerCreated, etc.
    public uint CreatedTick;
    public byte IsResolved;  // Has archetype been resolved?
}
```

### 2. Profile Application State

```csharp
public struct ProfileApplicationState : IComponentData
{
    public ProfileApplicationPhase Phase;  // ArchetypeAssigned, StatsApplied, AlignmentApplied, Complete
    public uint LastUpdatedTick;
    public byte NeedsRecalculation;  // Flag for recalculation
}
```

### 3. Game-Specific Profile Extensions

```csharp
// Godgame villager profile
public struct VillagerProfile : IComponentData
{
    public IndividualStats BaseStats;
    public SocialStats SocialStats;
    public EntityAlignment Alignment;
    public EntityOutlook Outlook;
    public PersonalityAxes Personality;
    public ExtendedPersonalityAxes ExtendedPersonality;
    public WisdomStat Wisdom;
}

// Space4X individual profile
public struct IndividualProfile : IComponentData
{
    public IndividualStats BaseStats;
    public OfficerStats OfficerStats;  // Command, Tactics, Logistics, etc.
    public EntityAlignment Alignment;
    public EntityOutlook Outlook;
    public PersonalityAxes Personality;
}
```

---

## Implementation Requirements

### Phase 1: Archetype Resolution

**System**: `ArchetypeResolutionSystem`
- Query entities with `EntityProfile` but no `VillagerArchetypeResolved`
- Lookup archetype in `VillagerArchetypeCatalog`
- Apply `VillagerArchetypeAssignment` component
- Resolve to `VillagerArchetypeResolved` with base stats, job weights, alignment lean

**Dependencies**:
- `VillagerArchetypeCatalogComponent` (singleton)
- `VillagerArchetypeComponents.cs` (existing)

### Phase 2: Base Stats Application

**System**: `BaseStatsApplicationSystem`
- Read `VillagerArchetypeResolved` → extract base stats (Physique, Finesse, Willpower)
- Apply `IndividualStats` component with base values
- Apply `WisdomStat` component (if missing, default to 50)
- Apply `ResourcePools` component with calculated MaxHP, MaxStamina, MaxMana, MaxFocus

**Derivation Formulas** (from canonical docs):
- `MaxHP = 50 + 0.6 * Physique + 0.4 * Will`
- `MaxStamina = Physique / 10`
- `MaxMana = 0.5 * Will + 0.5 * Intellect` (if magic-capable)
- `MaxFocus = 0.5 * Intellect + 0.5 * Will`

**Dependencies**:
- `Runtime/Individual/StatsComponents.cs` (existing)
- `Runtime/Stubs/WisdomStatExtensionStubComponents.cs` (stub)

### Phase 3: Derived Attributes Calculation

**System**: `DerivedAttributesCalculationSystem`
- Read `IndividualStats` + `XPStats` (if exists)
- Calculate `DerivedAttributes`:
  - `Strength = 0.8 * Physique + 0.2 * WeaponMastery` (from XP or default)
  - `Agility = 0.8 * Finesse + 0.2 * Acrobatics` (from XP or default)
  - `Intelligence = 0.6 * Will + 0.4 * Education` (from XP or default)
  - `WisdomDerived = 0.6 * Will + 0.4 * Lore` (from XP or default)
- Apply `DerivedAttributes` component

**Dependencies**:
- `Runtime/Stubs/DerivedAttributesStubComponents.cs` (stub)
- `Runtime/Stubs/XPStatsStubComponents.cs` (stub)

### Phase 4: Alignment & Outlook Application

**System**: `AlignmentOutlookApplicationSystem`
- Read `VillagerArchetypeResolved` → extract alignment lean (MoralAxisLean, OrderAxisLean, PurityAxisLean)
- Apply `EntityAlignment` component:
  - `Moral = archetype.MoralAxisLean` (clamped -100 to +100)
  - `Order = archetype.OrderAxisLean`
  - `Purity = archetype.PurityAxisLean`
  - `Strength = 0.5` (default conviction)
- Derive `EntityOutlook` from alignment:
  - If Moral > 50 && Order > 30 → `Primary = OutlookType.Warlike` (Heroic)
  - If Moral < -50 → `Primary = OutlookType.Authoritarian` (Ruthless)
  - If Order > 50 → `Primary = OutlookType.Scholarly` (Methodical)
  - If Order < -50 → `Primary = OutlookType.Pragmatic` (Rebellious)
  - If Purity > 50 → `Secondary = OutlookType.Spiritual` (Devout)
- Apply `EntityOutlook` component

**Dependencies**:
- `Runtime/Identity/Components.cs` (existing - EntityAlignment, EntityOutlook)

### Phase 5: Personality Application

**System**: `PersonalityApplicationSystem`
- Generate or read personality axes from archetype/profile
- Apply `PersonalityAxes` component:
  - `VengefulForgiving` = random or from profile (-100 to +100)
  - `CravenBold` = random or from profile (-100 to +100)
- Apply `ExtendedPersonalityAxes` component (if needed):
  - `CooperativeCompetitive` = random or from profile (-100 to +100)
  - `WarlikePeaceful` = random or from profile (-100 to +100)
- Calculate `BehaviorTuning` from personality + alignment:
  - `AggressionBias = (BoldScore / 100.0) * 0.5 + 1.0` (0.5 to 1.5)
  - `SocialBias = (CooperativeCompetitive / 100.0) * 0.3 + 1.0`
  - `GreedBias = (Purity < 0 ? 1.2 : 0.9)` (corrupt = more greedy)
  - `CuriosityBias = (Intellect / 100.0) * 0.4 + 0.8`
  - `ObedienceBias = (Order / 100.0) * 0.5 + 0.75` (lawful = more obedient)
- Apply `BehaviorTuning` component

**Dependencies**:
- `Runtime/Identity/Components.cs` (existing - PersonalityAxes)
- `Runtime/Individual/PersonalityComponents.cs` (existing - BehaviorTuning)
- `Runtime/Stubs/PersonalityAxesExtensionStubComponents.cs` (stub)

### Phase 6: Game-Specific Extensions

#### Godgame: Social Stats Application

**System**: `SocialStatsApplicationSystem` (Godgame only)
- Apply `SocialStats` component with default values:
  - `Fame = 0`
  - `Wealth = 0` (or from template)
  - `Reputation = 0`
  - `Glory = 0`
  - `Renown = CalculateRenown(Fame, Glory)`
- Apply `XPStats` component:
  - `PhysiqueXP = 0` (or from template)
  - `FinesseXP = 0`
  - `WillXP = 0`
  - `WisdomXP = 0`

**Dependencies**:
- `Runtime/Stubs/SocialStatsStubComponents.cs` (stub)
- `Runtime/Stubs/XPStatsStubComponents.cs` (stub)

#### Space4X: Officer Stats Application

**System**: `OfficerStatsApplicationSystem` (Space4X only)
- Apply `OfficerStats` component (Command, Tactics, Logistics, Diplomacy, Engineering, Resolve)
- Apply `Expertise` buffer (typed expertise tiers)
- Apply `ServiceTraits` buffer (trait flags)
- Apply `PreordainProfile` (career track nudges)

**Dependencies**:
- Space4X-specific officer components (verify existence)

---

## Integration Points

### With Existing Systems

1. **VillagerArchetypeResolutionSystem** (existing)
   - Already resolves archetypes from catalog
   - Profiling system should use this, not duplicate

2. **IndividualStats** (existing)
   - Already has Physique, Finesse, Agility, Intellect, Will, Social, Faith
   - Profiling system applies this component

3. **TraitAxisSystem** (existing)
   - Profiling system should seed `TraitAxisValue` buffer with initial axes:
     - `LawfulChaotic` = Order value
     - `GoodEvil` = Moral value
     - `CorruptPure` = Purity value
     - `VengefulForgiving` = personality value
     - `BoldCraven` = personality value

4. **Focus System** (Godgame)
   - After profiling, determine `FocusArchetype` from stats
   - High Finesse → `FocusArchetype.Finesse`
   - High Physique → `FocusArchetype.Physique`
   - High Intellect/Faith → `FocusArchetype.Arcane`

### With Stub Systems

1. **Social Stats** (`SocialStatsStub`)
   - Profiling system applies `SocialStats` component
   - Systems will implement Fame/Wealth/Reputation/Glory/Renown tracking

2. **XP Pools** (`XPStatsStub`)
   - Profiling system applies `XPStats` component
   - Systems will implement XP gain/spend/decay

3. **Derived Attributes** (`DerivedAttributesStub`)
   - Profiling system calculates and applies `DerivedAttributes`
   - Systems will implement recalculation on stat/XP changes

---

## Game-Specific Differences

### Godgame Villager Profile

**Required Components**:
- ✅ `IndividualStats` (Physique, Finesse, Agility, Intellect, Will, Social, Faith)
- ✅ `WisdomStat` (Wisdom + GainModifier)
- ✅ `ResourcePools` (HP, Stamina, Mana, Focus)
- ✅ `DerivedAttributes` (Strength, Agility, Intelligence, WisdomDerived)
- ✅ `EntityAlignment` (Moral, Order, Purity, Strength)
- ✅ `EntityOutlook` (Primary, Secondary, Tertiary)
- ✅ `PersonalityAxes` (VengefulForgiving, CravenBold)
- ✅ `ExtendedPersonalityAxes` (CooperativeCompetitive, WarlikePeaceful)
- ✅ `BehaviorTuning` (AggressionBias, SocialBias, etc.)
- ✅ `SocialStats` (Fame, Wealth, Reputation, Glory, Renown)
- ✅ `XPStats` (PhysiqueXP, FinesseXP, WillXP, WisdomXP)
- ✅ `VillagerArchetypeResolved` (from archetype catalog)
- ✅ `VillagerBelonging` buffer (empty initially)

**Optional Components**:
- `FocusState` (if focus-capable)
- `FocusArchetype` (determined from stats)
- `Needs` (Food, Rest, Sleep, GeneralHealth)

### Space4X Individual Profile

**Required Components**:
- ✅ `IndividualStats` (Physique, Finesse, Agility, Intellect, Will, Social, Faith)
- ✅ `ResourcePools` (HP, Stamina, Mana, Focus)
- ✅ `DerivedAttributes` (Strength, Agility, Intelligence)
- ✅ `EntityAlignment` (Moral, Order, Purity, Strength)
- ✅ `EntityOutlook` (Primary, Secondary, Tertiary)
- ✅ `PersonalityAxes` (VengefulForgiving, CravenBold)
- ✅ `BehaviorTuning` (AggressionBias, SocialBias, etc.)
- ✅ `OfficerStats` (Command, Tactics, Logistics, Diplomacy, Engineering, Resolve)
- ✅ `Expertise` buffer (typed expertise tiers)
- ✅ `ServiceTraits` buffer (trait flags)

**Optional Components**:
- `PreordainProfile` (career track)
- `CrewRole` (if part of crew)

---

## System Ordering

**UpdateInGroup**: `SimulationSystemGroup`

**Order**:
1. `ArchetypeResolutionSystem` - Resolve archetypes first
2. `BaseStatsApplicationSystem` - Apply base stats
3. `DerivedAttributesCalculationSystem` - Calculate derived stats
4. `AlignmentOutlookApplicationSystem` - Apply alignment/outlook
5. `PersonalityApplicationSystem` - Apply personality
6. `SocialStatsApplicationSystem` (Godgame) - Apply social stats
7. `OfficerStatsApplicationSystem` (Space4X) - Apply officer stats
8. `ProfileCompletionSystem` - Mark profile as complete

---

## Testing Requirements

### Unit Tests

1. **Archetype Resolution**
   - Test archetype lookup from catalog
   - Test fallback to default archetype
   - Test cached index usage

2. **Stats Application**
   - Test base stats application from archetype
   - Test derived stats calculation
   - Test resource pool calculation

3. **Alignment/Outlook Derivation**
   - Test alignment from archetype lean
   - Test outlook derivation from alignment
   - Test outlook combinations

4. **Personality Generation**
   - Test random personality generation
   - Test personality from profile
   - Test behavior tuning calculation

5. **Game-Specific Profiles**
   - Test Godgame villager profile creation
   - Test Space4X individual profile creation
   - Test component differences

### Integration Tests

1. **End-to-End Profiling**
   - Create blank entity → apply archetype → verify all components
   - Test profile application from template
   - Test profile application from generated data

2. **System Integration**
   - Test integration with `VillagerArchetypeResolutionSystem`
   - Test integration with `TraitAxisSystem`
   - Test integration with `FocusSystem` (Godgame)

---

## Reference Documentation

- `Docs/Concepts/Core/Entity_Stats_And_Archetypes_Canonical.md` - Canonical stat/archetype system
- `Docs/Audit/Stats_Archetypes_Behaviors_Alignments_Outlooks_Audit.md` - Implementation audit
- `Runtime/Villagers/VillagerArchetypeComponents.cs` - Existing archetype components
- `Runtime/Individual/StatsComponents.cs` - Existing stat components
- `Runtime/Identity/Components.cs` - Existing alignment/outlook/personality components
- `godgame/Docs/Individual_Template_Stats.md` - Godgame stat schema
- `Docs/Archive/BehaviorAlignment_Summary.md` - Behavior/alignment summary

---

## Implementation Notes

1. **Blank-by-Default**: Entities start blank; profiling system adds components
2. **Archetype-Driven**: Archetypes provide base values; profiling applies them
3. **Game-Agnostic Core**: Core profiling works for both games; game-specific extensions applied separately
4. **Deterministic**: Profile application should be deterministic (same input → same output)
5. **Performance**: Profile application happens at entity creation, not every frame
6. **Extensibility**: System should support custom profiles beyond archetypes

---

## Dependencies

### Existing Components
- `VillagerArchetypeCatalogComponent` (singleton)
- `VillagerArchetypeResolved`
- `IndividualStats`
- `ResourcePools`
- `EntityAlignment`
- `EntityOutlook`
- `PersonalityAxes`
- `BehaviorTuning`

### Stub Components (to be implemented)
- `SocialStats` (`SocialStatsStubComponents.cs`)
- `XPStats` (`XPStatsStubComponents.cs`)
- `DerivedAttributes` (`DerivedAttributesStubComponents.cs`)
- `ExtendedPersonalityAxes` (`PersonalityAxesExtensionStubComponents.cs`)
- `WisdomStat` (`WisdomStatExtensionStubComponents.cs`)

---

## Success Criteria

✅ Entity profiling system transforms blank entities into game-specific individuals  
✅ All required components are applied based on archetype/profile  
✅ Derived stats are calculated correctly  
✅ Alignment/outlook are derived from archetype  
✅ Personality axes are applied  
✅ Game-specific extensions (Social Stats, Officer Stats) are applied  
✅ System integrates with existing archetype resolution  
✅ System supports both Godgame and Space4X profiles  
✅ Profile application is deterministic  
✅ System is performant (profiling happens at creation, not runtime)

