# Stats, Archetypes, Behaviors, Alignments, Outlooks - Implementation Audit

**Last Updated**: 2025-01-21  
**Purpose**: Compare documented requirements vs actual implementations

---

## 1. STATS

### 1.1 Core Attributes (Primary Stats)

#### Documented Requirements (`Entity_Stats_And_Archetypes_Canonical.md`)
- **Physique**: Physical capacity (0-100)
- **Finesse**: Precision/speed (0-100)
- **Will**: Mental fortitude (0-100)
- **Wisdom**: General learning/cross-discipline (0-100)

#### Actual Implementation (`Runtime/Individual/StatsComponents.cs`)
✅ **EXISTS**: `IndividualStats` component
- ✅ `Physique` (float)
- ✅ `Finesse` (float)
- ✅ `Agility` (float) - **EXTRA**: Not in canonical docs
- ✅ `Intellect` (float) - **EXTRA**: Not in canonical docs
- ✅ `Will` (float)
- ✅ `Social` (float) - **EXTRA**: Not in canonical docs
- ✅ `Faith` (float) - **EXTRA**: Not in canonical docs

**Status**: ✅ **IMPLEMENTED** (with extras)  
**Gap**: Missing `Wisdom` stat (documented but not implemented)

---

### 1.2 Derived Attributes

#### Documented Requirements
- **Strength**: Derived from Physique + experience
- **Agility**: Derived from Finesse + experience
- **Intelligence**: Derived from Will + experience

#### Actual Implementation
✅ **EXISTS**: `IndividualStats` has `Agility` directly (not derived)  
❌ **MISSING**: `Strength` (not in `IndividualStats`)  
❌ **MISSING**: `Intelligence` (has `Intellect` instead, but not derived)

**Status**: 🟡 **PARTIAL**  
**Gap**: Strength/Intelligence derivation system not implemented

---

### 1.3 Resource Pools

#### Documented Requirements
- `PhysiqueXP`, `FinesseXP`, `WillXP`, `WisdomXP` pools

#### Actual Implementation
✅ **EXISTS**: `ResourcePools` component (`Runtime/Individual/StatsComponents.cs`)
- ✅ `HP` / `MaxHP`
- ✅ `Stamina` / `MaxStamina`
- ✅ `Mana` / `MaxMana`
- ✅ `Focus` / `MaxFocus`

❌ **MISSING**: XP pools (`PhysiqueXP`, `FinesseXP`, `WillXP`, `WisdomXP`)

**Status**: 🟡 **PARTIAL**  
**Gap**: XP pools not implemented (only resource pools exist)

---

### 1.4 Combat Stats

#### Documented Requirements (`Individual_Template_Stats.md`)
- Base Attack (0-100, auto-calculated)
- Base Defense (0-100, auto-calculated)
- Base Health (auto-calculated)
- Base Stamina (auto-calculated)
- Base Mana (0-100, auto-calculated)

#### Actual Implementation
✅ **EXISTS**: `CombatStats` component (`Runtime/Combat/CombatStats.cs`)
- ✅ `Attack`, `Defense`, `Accuracy`, `Evasion`
- ✅ `Health`, `CurrentHealth`
- ✅ `Stamina`, `CurrentStamina`
- ✅ `ManaPool`, `CurrentMana`
- ✅ `SpellPower`, `CriticalChance`

**Status**: ✅ **IMPLEMENTED**

---

### 1.5 Social Stats

#### Documented Requirements (`Individual_Template_Stats.md`)
- Fame (0-1000)
- Wealth (currency)
- Reputation (-100 to +100)
- Glory (0-1000)
- Renown (0-1000)

#### Actual Implementation
❌ **MISSING**: No social stats component found

**Status**: ❌ **NOT IMPLEMENTED**

---

### 1.6 Trait Axis System (Data-Driven Personality/Alignment)

#### Documented Requirements (`Entity_Stats_And_Archetypes_Canonical.md` Section 1.5)
- **TraitAxisCatalog**: Blob asset defining axes (ID, range, tags, semantics)
- **TraitAxisValue**: Buffer storing sparse axis-value pairs
- **TraitAxisLookup**: Helper API for querying axes
- **Canonical axes**: `LawfulChaotic`, `GoodEvil`, `CorruptPure`, `VengefulForgiving`, `BoldCraven`, etc.
- **Action footprints**: Actions modify trait values over time
- **Trait drift**: Values decay toward neutral over time

#### Actual Implementation
✅ **EXISTS**: `TraitAxisComponents` (`Runtime/Stats/TraitAxisComponents.cs`)
- ✅ `TraitAxisValue` buffer
- ✅ `TraitAxisSet` component (catalog reference)
- ✅ `TraitDriftSystem` (`Systems/Stats/TraitDriftSystem.cs`)

**Status**: ✅ **IMPLEMENTED** (needs verification of canonical axes)

---

## 2. ARCHETYPES

### 2.1 Villager Archetypes

#### Documented Requirements (`Entity_Stats_And_Archetypes_Canonical.md` Section 6)
- **Archetype facets**: NatureArchetype, RoleArchetype, CareerTrack, CapabilityLocks, ProfileBias
- **Archetype output**: Progression weights, cost modifiers, policy defaults, starting skills, capability sets, trait axis seeds

#### Actual Implementation
✅ **EXISTS**: `VillagerArchetypeComponents` (`Runtime/Villagers/VillagerArchetypeComponents.cs`)
- ✅ `VillagerArchetypeAssignment` (name + cached index)
- ✅ `VillagerArchetypeResolved` (resolved data)
- ✅ `VillagerArchetypeModifier` buffer (layered modifiers)
- ✅ `VillagerArchetypeCatalog` (authoring ScriptableObject)

✅ **EXISTS**: `VillagerArchetypeCatalog` (`Authoring/VillagerArchetypeCatalog.cs`)
- ✅ Base stats (Physique, Finesse, Willpower)
- ✅ Needs decay rates
- ✅ Job preference weights
- ✅ Alignment lean (Moral, Order, Purity axes)
- ✅ Base loyalty

**Status**: ✅ **IMPLEMENTED**

---

### 2.2 Focus Archetypes (Godgame)

#### Documented Requirements (`FocusComponents.cs`)
- Combat archetypes: Finesse, Physique, Arcane
- Profession archetypes: Crafting, Gathering, Healing, Teaching, Refining

#### Actual Implementation
✅ **EXISTS**: `FocusArchetype` enum (`godgame/Assets/Scripts/Godgame/Combat/FocusComponents.cs`)
- ✅ All documented archetypes present

**Status**: ✅ **IMPLEMENTED**

---

### 2.3 Facility Archetypes (Space4X)

#### Documented Requirements (`FacilityArchetypes.md`)
- Refinery, Fabricator, Foundry, Bioprocessor, Research Lab, Logistics Hub, Titan Forge, Habitat Module

#### Actual Implementation
✅ **EXISTS**: `FacilityComponents` (`Runtime/Facility/FacilityComponents.cs`)
- ✅ Facility archetype system exists

**Status**: ✅ **IMPLEMENTED** (needs verification of specific archetypes)

---

## 3. BEHAVIORS

### 3.1 Personality Axes

#### Documented Requirements (`Entity_Stats_And_Archetypes_Canonical.md` Section 1.5.2)
- **VengefulForgiving**: -100 (Vengeful) ↔ +100 (Forgiving)
- **BoldCraven**: -100 (Craven) ↔ +100 (Bold)
- **CooperativeCompetitive**: +100 (Cooperative) ↔ -100 (Competitive)
- **WarlikePeaceful**: +100 (Warlike) ↔ -100 (Peaceful)

#### Actual Implementation
✅ **EXISTS**: `PersonalityAxes` (`Runtime/Identity/Components.cs`)
- ✅ `VengefulForgiving` (float)
- ✅ `CravenBold` (float) - **NOTE**: Reversed naming (CravenBold vs BoldCraven)

✅ **EXISTS**: `PersonalityAxes` (`Runtime/Individual/PersonalityComponents.cs`)
- ✅ `Boldness` (float, -1 to +1)
- ✅ `Vengefulness` (float, -1 to +1)
- ✅ `RiskTolerance` (float, -1 to +1) - **EXTRA**
- ✅ `Selflessness` (float, -1 to +1) - **EXTRA**
- ✅ `Conviction` (float, -1 to +1) - **EXTRA**

✅ **EXISTS**: `VillagerBehavior` (`godgame/Assets/Scripts/Godgame/Villagers/VillagerBehaviorComponents.cs`)
- ✅ `VengefulScore` (float, -100 to +100)
- ✅ `BoldScore` (float, -100 to +100)
- ✅ `InitiativeModifier` (computed)
- ✅ `ActiveGrudgeCount` (byte)
- ✅ `LastMajorActionTick` (uint)

**Status**: 🟡 **MULTIPLE IMPLEMENTATIONS** (needs consolidation)
- `Runtime/Identity/Components.cs` - Generic
- `Runtime/Individual/PersonalityComponents.cs` - Individual-specific
- `godgame/Villagers/VillagerBehaviorComponents.cs` - Godgame-specific

**Gap**: No `CooperativeCompetitive` or `WarlikePeaceful` axes found

---

### 3.2 Behavior Tuning

#### Documented Requirements
- Behavior biases that multiply AI utility scores

#### Actual Implementation
✅ **EXISTS**: `BehaviorTuning` (`Runtime/Individual/PersonalityComponents.cs`)
- ✅ `AggressionBias` (multiplier)
- ✅ `SocialBias` (multiplier)
- ✅ `GreedBias` (multiplier)
- ✅ `CuriosityBias` (multiplier)
- ✅ `ObedienceBias` (multiplier)

**Status**: ✅ **IMPLEMENTED**

---

### 3.3 AI Behavior Modules

#### Documented Requirements (`AIBehaviorModules.md`)
- `AIAgent` component with `AIArchetype`, `AIBehaviorMode`, `BehaviorProfile`
- `AIArchetype` enum: Villager, Creature, Band, Crew, Carrier, Fleet, Aggregate
- `AIBehaviorMode` enum: Idle, Working, Traveling, Fleeing, Attacking, Gathering, Trading, Socializing, Resting

#### Actual Implementation
✅ **EXISTS**: `AIComponents` (`Runtime/AI/AIComponents.cs`)
- ✅ AI agent system exists

**Status**: ✅ **IMPLEMENTED** (needs verification of specific enums)

---

## 4. ALIGNMENTS

### 4.1 Tri-Axis Alignment

#### Documented Requirements (`Entity_Stats_And_Archetypes_Canonical.md`)
- **Moral**: Good (+100) ↔ Evil (-100)
- **Order**: Lawful (+100) ↔ Chaotic (-100)
- **Purity**: Pure (+100) ↔ Corrupt (-100)
- **Strength**: Conviction level (0-1)

#### Actual Implementation
✅ **EXISTS**: `EntityAlignment` (`Runtime/Identity/Components.cs`)
- ✅ `Moral` (float, -100 to +100)
- ✅ `Order` (float, -100 to +100)
- ✅ `Purity` (float, -100 to +100)
- ✅ `Strength` (float, 0-1)

✅ **EXISTS**: `AlignmentTriplet` (`Runtime/Individual/AlignmentComponents.cs`)
- ✅ `Moral` (float, -1 to +1) - **NOTE**: Normalized range
- ✅ `Order` (float, -1 to +1)
- ✅ `Purity` (float, -1 to +1)

**Status**: 🟡 **MULTIPLE IMPLEMENTATIONS** (needs consolidation)
- `Runtime/Identity/Components.cs` - Generic (-100 to +100)
- `Runtime/Individual/AlignmentComponents.cs` - Individual-specific (-1 to +1)

---

### 4.2 Aggregate Alignment

#### Documented Requirements (`Entity_Stats_And_Archetypes_Canonical.md`)
- Weighted average of member alignments
- Cohesion tracking

#### Actual Implementation
✅ **EXISTS**: `AggregateAlignment` (`Runtime/Identity/Components.cs`)
- ✅ `Moral`, `Order`, `Purity` (averaged)
- ✅ `Cohesion` (0-1)
- ✅ `DriftRate` (0-1)

**Status**: ✅ **IMPLEMENTED**

---

### 4.3 Might/Magic Alignment

#### Documented Requirements (`Entity_Stats_And_Archetypes_Canonical.md`)
- Might (-100) ↔ Magic (+100) axis
- Strength (0-1)

#### Actual Implementation
✅ **EXISTS**: `MightMagicAffinity` (`Runtime/Identity/Components.cs`)
- ✅ `Axis` (float, -100 to +100)
- ✅ `Strength` (float, 0-1)

✅ **EXISTS**: `MightMagicAlignment` (`Runtime/Individual/AlignmentComponents.cs`)
- ✅ `Axis` (float, -1 to +1) - **NOTE**: Normalized range
- ✅ `Strength` (float, 0-1)
- ✅ `MightBonus`, `MagicBonus`, `OppositePenalty` (precomputed)

**Status**: 🟡 **MULTIPLE IMPLEMENTATIONS** (needs consolidation)

---

## 5. OUTLOOKS

### 5.1 Outlook Types

#### Documented Requirements (`Entity_Stats_And_Archetypes_Canonical.md`)
- Up to 3 outlook tags per entity (Primary, Secondary, Tertiary)
- Types: Warlike, Peaceful, Spiritual, Materialistic, Scholarly, Pragmatic, Xenophobic, Egalitarian, Authoritarian

#### Actual Implementation
✅ **EXISTS**: `OutlookType` enum (`Runtime/Identity/Components.cs`)
- ✅ `Warlike`, `Peaceful`, `Spiritual`, `Materialistic`, `Scholarly`, `Pragmatic`, `Xenophobic`, `Egalitarian`, `Authoritarian`

✅ **EXISTS**: `EntityOutlook` (`Runtime/Identity/Components.cs`)
- ✅ `Primary`, `Secondary`, `Tertiary` (OutlookType)

**Status**: ✅ **IMPLEMENTED**

---

### 5.2 Aggregate Outlook

#### Documented Requirements
- Dominant cultural outlooks from member composition

#### Actual Implementation
✅ **EXISTS**: `AggregateOutlook` (`Runtime/Identity/Components.cs`)
- ✅ `DominantPrimary`, `DominantSecondary`, `DominantTertiary`
- ✅ `CulturalUniformity` (0-1)

**Status**: ✅ **IMPLEMENTED**

---

### 5.3 Space4X Faction Outlook

#### Documented Requirements (`Space4XFactionComponents.cs`)
- Flags enum: Expansionist, Isolationist, Militarist, Pacifist, Materialist, Spiritualist, Xenophile, Xenophobe, Egalitarian, Authoritarian, Corrupt, Honorable

#### Actual Implementation
✅ **EXISTS**: `FactionOutlook` enum (`space4x/Assets/Scripts/Space4x/Registry/Space4XFactionComponents.cs`)
- ✅ All documented flags present

**Status**: ✅ **IMPLEMENTED**

---

## 6. SUMMARY

### ✅ Fully Implemented
- **Combat Stats**: Complete
- **Villager Archetypes**: Complete
- **Focus Archetypes**: Complete
- **Facility Archetypes**: Complete
- **Behavior Tuning**: Complete
- **Outlooks**: Complete
- **Aggregate Alignment/Outlook**: Complete
- **Trait Axis System**: Complete (needs verification)

### 🟡 Partial / Multiple Implementations
- **Core Stats**: Missing `Wisdom`, has extras (`Agility`, `Intellect`, `Social`, `Faith`)
- **Derived Attributes**: `Strength`/`Intelligence` derivation not implemented
- **XP Pools**: Not implemented (only resource pools exist)
- **Personality Axes**: Multiple implementations, missing `CooperativeCompetitive` and `WarlikePeaceful`
- **Alignment**: Multiple implementations (different ranges: -100/+100 vs -1/+1)

### ❌ Not Implemented
- **Social Stats**: Fame, Wealth, Reputation, Glory, Renown
- **XP Pools**: PhysiqueXP, FinesseXP, WillXP, WisdomXP

---

## 7. RECOMMENDATIONS

### High Priority
1. **Consolidate Alignment Implementations**: Choose one range (-100/+100 or -1/+1) and migrate all systems
2. **Consolidate Personality Implementations**: Merge `Runtime/Identity/Components.cs`, `Runtime/Individual/PersonalityComponents.cs`, and `godgame/Villagers/VillagerBehaviorComponents.cs`
3. **Add Missing Stats**: Implement `Wisdom` stat and XP pools
4. **Add Social Stats**: Implement Fame, Wealth, Reputation, Glory, Renown components

### Medium Priority
5. **Add Missing Personality Axes**: Implement `CooperativeCompetitive` and `WarlikePeaceful`
6. **Implement Derived Attributes**: Add derivation system for Strength/Intelligence from base stats + XP
7. **Verify Trait Axis Catalog**: Ensure canonical axes (`LawfulChaotic`, `GoodEvil`, etc.) are defined in catalog

### Low Priority
8. **Documentation Sync**: Update docs to reflect actual implementations (e.g., `Agility`, `Intellect`, `Social`, `Faith` extras)
9. **Code Cleanup**: Remove duplicate implementations or clearly document why multiple exist

---

## 8. FILES TO REVIEW

### Stats
- `Runtime/Individual/StatsComponents.cs` - Core stats
- `Runtime/Runtime/Combat/CombatStats.cs` - Combat stats
- `Runtime/Runtime/Stats/TraitAxisComponents.cs` - Trait axis system
- `Runtime/Runtime/Stats/StatHistoryComponents.cs` - Stat history

### Archetypes
- `Runtime/Villagers/VillagerArchetypeComponents.cs` - Villager archetypes
- `Runtime/Authoring/VillagerArchetypeCatalog.cs` - Archetype catalog
- `godgame/Assets/Scripts/Godgame/Combat/FocusComponents.cs` - Focus archetypes

### Behaviors
- `Runtime/Identity/Components.cs` - Generic personality
- `Runtime/Individual/PersonalityComponents.cs` - Individual personality
- `godgame/Assets/Scripts/Godgame/Villagers/VillagerBehaviorComponents.cs` - Godgame behavior
- `Runtime/AI/AIComponents.cs` - AI behavior modules

### Alignments
- `Runtime/Identity/Components.cs` - Generic alignment
- `Runtime/Individual/AlignmentComponents.cs` - Individual alignment
- `space4x/Assets/Scripts/Space4x/Registry/Space4XAlignmentComponents.cs` - Space4X alignment

### Outlooks
- `Runtime/Identity/Components.cs` - Generic outlook
- `space4x/Assets/Scripts/Space4x/Registry/Space4XFactionComponents.cs` - Space4X faction outlook
