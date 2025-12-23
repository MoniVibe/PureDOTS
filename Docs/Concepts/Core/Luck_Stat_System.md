# Luck Stat System

## Overview

**Luck is a player-modifiable stat** that influences all random rolls an entity encounters throughout their lifetime, from critical hit chances to discovery rolls, crafting quality, research breakthroughs, and more.

**Core Concept:**
- **Player Modifiable:** Players can directly modify entity luck (via miracles, blessings, curses, items, etc.)
- **Universal Roll Modifier:** Affects all random rolls an entity makes or receives
- **Lifetime Impact:** Influences outcomes from birth to death
- **Flexible Integration:** Works with existing stat systems and roll mechanics

---

## Luck Stat Definition

### Base Luck Stat

**Luck Range:** -100 to +100 (default: 0)

**Stat Structure:**
```csharp
public struct LuckStat : IComponentData
{
    /// <summary>
    /// Base luck value (-100 to +100).
    /// Default: 0 (neutral luck).
    /// Positive: Good luck (beneficial outcomes more likely).
    /// Negative: Bad luck (detrimental outcomes more likely).
    /// </summary>
    public float BaseLuck;
    
    /// <summary>
    /// Temporary luck modifiers (blessings, curses, items, environmental effects).
    /// Applied on top of BaseLuck.
    /// </summary>
    public float TemporaryLuck;
    
    /// <summary>
    /// Total effective luck (BaseLuck + TemporaryLuck).
    /// Clamped to -100 to +100.
    /// </summary>
    public float EffectiveLuck => math.clamp(BaseLuck + TemporaryLuck, -100f, 100f);
}
```

**Luck Modifiers:**
```csharp
public struct LuckModifier : IComponentData
{
    /// <summary>
    /// Luck modifier value (positive = good luck, negative = bad luck).
    /// </summary>
    public float ModifierValue;
    
    /// <summary>
    /// Duration in seconds (0 = permanent until removed).
    /// </summary>
    public float Duration;
    
    /// <summary>
    /// Source of modifier (blessing, curse, item, environmental, etc.).
    /// </summary>
    public FixedString64Bytes Source;
}
```

---

## Luck Application Mechanics

### Roll Modification Formula

**Core Formula:**
```
ModifiedRoll = BaseRoll + (EffectiveLuck × LuckInfluenceFactor)

Where:
  BaseRoll = Original random roll (0.0-1.0 or 0-100, etc.)
  EffectiveLuck = -100 to +100 (entity's total luck)
  LuckInfluenceFactor = 0.0-1.0 (how much luck affects this roll type)
  
Result: Modified roll (clamped to valid range)
```

**Luck Influence Factors (Per Roll Type):**
```
CriticalHitChance: 0.5 (luck has moderate influence on crits)
DiscoveryRolls: 0.3 (luck has light influence on discoveries)
CraftingQuality: 0.4 (luck has moderate influence on crafting)
ResearchBreakthroughs: 0.2 (luck has light influence on research)
TradingOutcomes: 0.3 (luck has light influence on trading)
CombatAccuracy: 0.2 (luck has light influence on accuracy)
DodgeChance: 0.3 (luck has moderate influence on dodging)
DiseaseResistance: 0.2 (luck has light influence on disease)
InjurySeverity: 0.4 (luck has moderate influence on injury severity)
ResourceFinding: 0.5 (luck has moderate influence on resource discovery)
EventOutcomes: 0.6 (luck has strong influence on random events)
MiracleSuccess: 0.3 (luck has light influence on miracle success)
```

**Example Calculation:**
```
Entity has EffectiveLuck = +50 (good luck)
Critical Hit Roll:
  BaseRoll = 0.15 (15% base crit chance)
  LuckInfluenceFactor = 0.5
  ModifiedRoll = 0.15 + (50 / 100 × 0.5) = 0.15 + 0.25 = 0.40 (40% crit chance)
  
Entity has EffectiveLuck = -30 (bad luck)
Crafting Quality Roll:
  BaseRoll = 0.70 (70% base quality)
  LuckInfluenceFactor = 0.4
  ModifiedRoll = 0.70 + (-30 / 100 × 0.4) = 0.70 - 0.12 = 0.58 (58% quality)
```

---

## Roll Type Categories

### 1. Combat Rolls

**Critical Hit Chance:**
```
ModifiedCritChance = BaseCritChance + (EffectiveLuck / 100 × 0.5)

Example:
  BaseCritChance = 10% (0.10)
  EffectiveLuck = +40
  ModifiedCritChance = 0.10 + (40 / 100 × 0.5) = 0.10 + 0.20 = 0.30 (30%)
  
  BaseCritChance = 10%
  EffectiveLuck = -60
  ModifiedCritChance = 0.10 + (-60 / 100 × 0.5) = 0.10 - 0.30 = -0.20 → 0% (clamped)
```

**Accuracy/Dodge:**
```
ModifiedAccuracy = BaseAccuracy + (EffectiveLuck / 100 × 0.2)
ModifiedDodge = BaseDodge + (EffectiveLuck / 100 × 0.3)

Example:
  BaseAccuracy = 75%
  EffectiveLuck = +25
  ModifiedAccuracy = 0.75 + (25 / 100 × 0.2) = 0.75 + 0.05 = 0.80 (80%)
```

**Injury Severity:**
```
ModifiedInjurySeverity = BaseInjurySeverity - (EffectiveLuck / 100 × 0.4)

Example:
  BaseInjurySeverity = 0.80 (severe injury)
  EffectiveLuck = +50
  ModifiedInjurySeverity = 0.80 - (50 / 100 × 0.4) = 0.80 - 0.20 = 0.60 (moderate injury)
```

---

### 2. Crafting and Production Rolls

**Crafting Quality:**
```
ModifiedQuality = BaseQuality + (EffectiveLuck / 100 × 0.4)

Example:
  BaseQuality = 0.60 (60% quality)
  EffectiveLuck = +30
  ModifiedQuality = 0.60 + (30 / 100 × 0.4) = 0.60 + 0.12 = 0.72 (72% quality)
```

**Production Success:**
```
ModifiedSuccess = BaseSuccess + (EffectiveLuck / 100 × 0.3)

Example:
  BaseSuccess = 0.85 (85% success rate)
  EffectiveLuck = -20
  ModifiedSuccess = 0.85 + (-20 / 100 × 0.3) = 0.85 - 0.06 = 0.79 (79% success)
```

**Material Quality Discovery:**
```
ModifiedDiscovery = BaseDiscovery + (EffectiveLuck / 100 × 0.5)

Example:
  BaseDiscovery = 0.20 (20% chance to find rare material)
  EffectiveLuck = +40
  ModifiedDiscovery = 0.20 + (40 / 100 × 0.5) = 0.20 + 0.20 = 0.40 (40% chance)
```

---

### 3. Research and Learning Rolls

**Research Breakthrough:**
```
ModifiedBreakthrough = BaseBreakthrough + (EffectiveLuck / 100 × 0.2)

Example:
  BaseBreakthrough = 0.10 (10% chance for breakthrough)
  EffectiveLuck = +50
  ModifiedBreakthrough = 0.10 + (50 / 100 × 0.2) = 0.10 + 0.10 = 0.20 (20% chance)
```

**Skill Learning Speed:**
```
ModifiedLearningSpeed = BaseLearningSpeed × (1.0 + EffectiveLuck / 100 × 0.1)

Example:
  BaseLearningSpeed = 1.0 (normal speed)
  EffectiveLuck = +60
  ModifiedLearningSpeed = 1.0 × (1.0 + 60 / 100 × 0.1) = 1.0 × 1.06 = 1.06 (6% faster)
```

**Tech Optimization Reroll:**
```
ModifiedRerollSuccess = BaseRerollSuccess + (EffectiveLuck / 100 × 0.3)

Example:
  BaseRerollSuccess = 0.30 (30% chance for better reroll)
  EffectiveLuck = +40
  ModifiedRerollSuccess = 0.30 + (40 / 100 × 0.3) = 0.30 + 0.12 = 0.42 (42% chance)
```

---

### 4. Discovery and Exploration Rolls

**Resource Finding:**
```
ModifiedFindChance = BaseFindChance + (EffectiveLuck / 100 × 0.5)

Example:
  BaseFindChance = 0.15 (15% chance to find resource)
  EffectiveLuck = +30
  ModifiedFindChance = 0.15 + (30 / 100 × 0.5) = 0.15 + 0.15 = 0.30 (30% chance)
```

**Treasure Discovery:**
```
ModifiedTreasureChance = BaseTreasureChance + (EffectiveLuck / 100 × 0.6)

Example:
  BaseTreasureChance = 0.05 (5% chance for treasure)
  EffectiveLuck = +50
  ModifiedTreasureChance = 0.05 + (50 / 100 × 0.6) = 0.05 + 0.30 = 0.35 (35% chance)
```

**Event Discovery:**
```
ModifiedEventChance = BaseEventChance + (EffectiveLuck / 100 × 0.4)

Example:
  BaseEventChance = 0.20 (20% chance for random event)
  EffectiveLuck = +25
  ModifiedEventChance = 0.20 + (25 / 100 × 0.4) = 0.20 + 0.10 = 0.30 (30% chance)
```

---

### 5. Social and Trading Rolls

**Trading Outcomes:**
```
ModifiedTradePrice = BaseTradePrice × (1.0 + EffectiveLuck / 100 × 0.3)

Example:
  BaseTradePrice = 100 (base price)
  EffectiveLuck = +40
  ModifiedTradePrice = 100 × (1.0 + 40 / 100 × 0.3) = 100 × 1.12 = 112 (12% better price)
```

**Diplomacy Success:**
```
ModifiedDiplomacySuccess = BaseDiplomacySuccess + (EffectiveLuck / 100 × 0.2)

Example:
  BaseDiplomacySuccess = 0.60 (60% success chance)
  EffectiveLuck = +30
  ModifiedDiplomacySuccess = 0.60 + (30 / 100 × 0.2) = 0.60 + 0.06 = 0.66 (66% success)
```

**Social Event Outcomes:**
```
ModifiedSocialOutcome = BaseSocialOutcome + (EffectiveLuck / 100 × 0.3)

Example:
  BaseSocialOutcome = 0.50 (neutral outcome)
  EffectiveLuck = +50
  ModifiedSocialOutcome = 0.50 + (50 / 100 × 0.3) = 0.50 + 0.15 = 0.65 (positive outcome)
```

---

### 6. Survival and Health Rolls

**Disease Resistance:**
```
ModifiedDiseaseResistance = BaseDiseaseResistance + (EffectiveLuck / 100 × 0.2)

Example:
  BaseDiseaseResistance = 0.70 (70% resistance)
  EffectiveLuck = +40
  ModifiedDiseaseResistance = 0.70 + (40 / 100 × 0.2) = 0.70 + 0.08 = 0.78 (78% resistance)
```

**Injury Avoidance:**
```
ModifiedInjuryAvoidance = BaseInjuryAvoidance + (EffectiveLuck / 100 × 0.4)

Example:
  BaseInjuryAvoidance = 0.30 (30% chance to avoid injury)
  EffectiveLuck = +50
  ModifiedInjuryAvoidance = 0.30 + (50 / 100 × 0.4) = 0.30 + 0.20 = 0.50 (50% chance)
```

**Death Avoidance:**
```
ModifiedDeathAvoidance = BaseDeathAvoidance + (EffectiveLuck / 100 × 0.3)

Example:
  BaseDeathAvoidance = 0.10 (10% chance to avoid death)
  EffectiveLuck = +60
  ModifiedDeathAvoidance = 0.10 + (60 / 100 × 0.3) = 0.10 + 0.18 = 0.28 (28% chance)
```

---

### 7. Magic and Miracle Rolls

**Miracle Success:**
```
ModifiedMiracleSuccess = BaseMiracleSuccess + (EffectiveLuck / 100 × 0.3)

Example:
  BaseMiracleSuccess = 0.80 (80% success chance)
  EffectiveLuck = +30
  ModifiedMiracleSuccess = 0.80 + (30 / 100 × 0.3) = 0.80 + 0.09 = 0.89 (89% success)
```

**Spell Critical:**
```
ModifiedSpellCrit = BaseSpellCrit + (EffectiveLuck / 100 × 0.4)

Example:
  BaseSpellCrit = 0.15 (15% spell crit chance)
  EffectiveLuck = +40
  ModifiedSpellCrit = 0.15 + (40 / 100 × 0.4) = 0.15 + 0.16 = 0.31 (31% crit chance)
```

**Mana Efficiency:**
```
ModifiedManaEfficiency = BaseManaEfficiency × (1.0 + EffectiveLuck / 100 × 0.2)

Example:
  BaseManaEfficiency = 1.0 (normal efficiency)
  EffectiveLuck = +50
  ModifiedManaEfficiency = 1.0 × (1.0 + 50 / 100 × 0.2) = 1.0 × 1.10 = 1.10 (10% more efficient)
```

---

### 8. Random Event Rolls

**Event Outcome Quality:**
```
ModifiedEventOutcome = BaseEventOutcome + (EffectiveLuck / 100 × 0.6)

Example:
  BaseEventOutcome = 0.50 (neutral outcome)
  EffectiveLuck = +40
  ModifiedEventOutcome = 0.50 + (40 / 100 × 0.6) = 0.50 + 0.24 = 0.74 (positive outcome)
```

**Event Frequency:**
```
ModifiedEventFrequency = BaseEventFrequency × (1.0 + EffectiveLuck / 100 × 0.2)

Example:
  BaseEventFrequency = 1.0 (normal frequency)
  EffectiveLuck = +60
  ModifiedEventFrequency = 1.0 × (1.0 + 60 / 100 × 0.2) = 1.0 × 1.12 = 1.12 (12% more events)
```

**Event Severity:**
```
ModifiedEventSeverity = BaseEventSeverity - (EffectiveLuck / 100 × 0.4)

Example:
  BaseEventSeverity = 0.80 (severe event)
  EffectiveLuck = +50
  ModifiedEventSeverity = 0.80 - (50 / 100 × 0.4) = 0.80 - 0.20 = 0.60 (moderate event)
```

---

## Player Modification Methods

### 1. Direct UI Manipulation (Primary Method)

**Entity Profile UI Tooltip Slider:**
- **Slider Range:** -100 to +100 (matches BaseLuck range)
- **Current Value:** Shows entity's current BaseLuck
- **Mana Cost:** Dynamic cost based on distance from neutral (0)
- **Instant Effect:** Changes apply immediately when slider is adjusted
- **Visual Feedback:** Slider position, cost display, current luck value

**Mana Cost Formula:**
```
ManaCost = BaseCost × (1 + DistanceFromNeutral² / 10000) × TimeSinceLastChange

Where:
  BaseCost = 10-50 mana (base cost per unit of luck change)
  DistanceFromNeutral = |NewLuck - 0| (how far from neutral)
  TimeSinceLastChange = 1.0 + (HoursSinceLastChange / 24.0) (reduces cost if time passed)
  
Example:
  Moving from 0 to +30 luck:
    DistanceFromNeutral = 30
    ManaCost = 20 × (1 + 30² / 10000) × 1.0 = 20 × 1.09 = 21.8 mana
    
  Moving from +50 to +80 luck:
    DistanceFromNeutral = 80 (new value)
    ManaCost = 20 × (1 + 80² / 10000) × 1.0 = 20 × 1.64 = 32.8 mana
    
  Moving from +80 to +50 luck (reducing luck):
    DistanceFromNeutral = 50 (new value)
    ManaCost = 20 × (1 + 50² / 10000) × 1.0 = 20 × 1.25 = 25.0 mana
```

**Compounding Cost for Extreme Values:**
```
ExtremeLuckModifier = 1.0 + (|Luck| - 50) / 100  // Only applies beyond ±50

Example:
  Moving from +50 to +90 luck:
    ExtremeLuckModifier = 1.0 + (90 - 50) / 100 = 1.4
    ManaCost = BaseCost × NormalModifier × 1.4 (40% extra cost for extreme values)
```

**UI Implementation:**
```
Entity Profile Tooltip:
  [Entity Name] - [Current BaseLuck: +45]
  ┌─────────────────────────────────────┐
  │ Luck: [-100]──────[●]──────[+100]  │
  │ Current: +45                        │
  │ Cost to change: 23 mana             │
  │ [Adjust Luck]                       │
  └─────────────────────────────────────┘
  
  Warning: The Luck God opposes unnatural luck manipulation.
  Current relations: -15 (Hostile)
```

---

### 2. Luck God Resistance System

**Luck God Mechanics:**
- **Automatic Normalization:** Luck God gradually normalizes player-modified luck toward 0
- **Normalization Rate:** Increases with distance from neutral and player's relation with Luck God
- **Relation Penalties:** Player gets negative relation with Luck God for manipulating luck
- **Relation Recovery:** Relations improve slowly when player stops manipulating luck

**Normalization Formula:**
```
NormalizationRate = BaseRate × (1 + DistanceFromNeutral / 100) × (1 - PlayerRelation / 100)

Where:
  BaseRate = 0.1-1.0 per day (base normalization speed)
  DistanceFromNeutral = |CurrentLuck| (how far from 0)
  PlayerRelation = -100 to +100 (relation with Luck God, negative = hostile)
  
Example:
  Entity with +60 luck, Player relation = -30 (hostile):
    NormalizationRate = 0.5 × (1 + 60 / 100) × (1 - (-30) / 100)
                      = 0.5 × 1.6 × 1.3
                      = 1.04 per day (normalizes ~1 point per day)
    
  Entity with +60 luck, Player relation = -80 (very hostile):
    NormalizationRate = 0.5 × (1 + 60 / 100) × (1 - (-80) / 100)
                      = 0.5 × 1.6 × 1.8
                      = 1.44 per day (faster normalization when very hostile)
```

**Normalization Process:**
```
Each Day/Tick:
  if (BaseLuck != 0):
    NormalizationAmount = NormalizationRate × DeltaTime
    if (BaseLuck > 0):
      BaseLuck = max(0, BaseLuck - NormalizationAmount)
    else:
      BaseLuck = min(0, BaseLuck + NormalizationAmount)
      
Example:
  Day 1: BaseLuck = +60, NormalizationRate = 1.04/day
  Day 2: BaseLuck = +58.96 (normalized by 1.04)
  Day 3: BaseLuck = +57.92 (normalized by 1.04)
  ...
  Day 60: BaseLuck ≈ 0 (fully normalized)
```

---

### 3. Luck God Relations System

**Relation Changes Based on Luck God Preferences:**

The Luck God's reactions are based on three factors:
1. **Balance Preference:** Opposes extreme luck (very high or very low), approves normalization
2. **Merit Preference:** Approves luck that matches entity merit (stats, achievements, behavior)
3. **Alignment Preference:** Approves helping "good" entities, opposing "evil" entities (or vice versa, depends on Luck God alignment)

**Relation Change Formula:**
```
RelationChange = BalanceFactor + MeritFactor + AlignmentFactor

Where:
  BalanceFactor = -BalancePenalty × ExtremeLuckPenalty
  MeritFactor = MeritBonus × MeritMatch
  AlignmentFactor = AlignmentBonus × AlignmentMatch
```

**1. Balance Factor (Extreme Luck Opposition):**
```
BalanceFactor = -BalancePenalty × ExtremeLuckPenalty

Where:
  BalancePenalty = 0.1-0.5 (base penalty for extreme luck)
  ExtremeLuckPenalty = (|NewLuck| - BalanceThreshold) / 100
  
  BalanceThreshold = 30 (luck beyond ±30 is considered "extreme")
  
  If |NewLuck| <= BalanceThreshold:
    ExtremeLuckPenalty = 0 (no penalty, Luck God approves normalization)
  Else:
    ExtremeLuckPenalty = (|NewLuck| - 30) / 100 (penalty increases with extremity)
    
  When normalizing (moving closer to 0):
    BalanceFactor = BalancePenalty × (OldExtremePenalty - NewExtremePenalty)
    // Positive when normalizing, negative when creating extreme luck
    
Example:
  Normalizing from +80 to +30:
    OldExtremePenalty = (80 - 30) / 100 = 0.5
    NewExtremePenalty = (30 - 30) / 100 = 0.0
    BalanceFactor = 0.3 × (0.5 - 0.0) = +0.15 (positive, Luck God approves)
    
  Creating extreme luck from 0 to +80:
    OldExtremePenalty = 0.0
    NewExtremePenalty = (80 - 30) / 100 = 0.5
    BalanceFactor = -0.3 × (0.5 - 0.0) = -0.15 (negative, Luck God opposes)
```

**2. Merit Factor (Luck Matching Entity Merit):**
```
MeritFactor = MeritBonus × MeritMatch

Where:
  MeritBonus = 0.1-0.3 (base bonus for matching merit)
  MeritMatch = How well luck matches entity merit (0.0-1.0)
  
Merit Score Calculation:
  EntityMerit = (StatsAverage + AchievementScore + BehaviorScore) / 3
  
  Where:
    StatsAverage = Average of core stats (Physique, Finesse, Intellect, Will, etc.) / 100
    AchievementScore = (Fame + Glory + Renown) / 3000 (normalized to 0-1)
    BehaviorScore = Based on outlook/alignment (Good deeds, positive reputation, etc.)
    
Merit Match:
  ExpectedLuck = (EntityMerit - 0.5) × 200  // Maps 0-1 merit to -100 to +100 luck
  
  MeritMatch = 1.0 - (|NewLuck - ExpectedLuck| / 200)
  MeritMatch = clamp(MeritMatch, 0.0, 1.0)
  
Example:
  Entity with high merit (0.8):
    ExpectedLuck = (0.8 - 0.5) × 200 = +60
    Player sets luck to +60:
      MeritMatch = 1.0 - (|60 - 60| / 200) = 1.0 (perfect match)
      MeritFactor = 0.2 × 1.0 = +0.20 (positive, Luck God approves)
      
    Player sets luck to -20:
      MeritMatch = 1.0 - (|-20 - 60| / 200) = 1.0 - 0.4 = 0.6 (poor match)
      MeritFactor = 0.2 × 0.6 = +0.12 (slight positive, but less than perfect match)
      
  Entity with low merit (0.2):
    ExpectedLuck = (0.2 - 0.5) × 200 = -60
    Player sets luck to -60:
      MeritMatch = 1.0 - (|-60 - (-60)| / 200) = 1.0 (perfect match)
      MeritFactor = 0.2 × 1.0 = +0.20 (positive, Luck God approves)
```

**3. Alignment Factor (Luck God Alignment Preferences):**
```
AlignmentFactor = AlignmentBonus × AlignmentMatch

Where:
  AlignmentBonus = 0.1-0.4 (base bonus for alignment match)
  AlignmentMatch = How well luck change matches Luck God's alignment preferences (0.0-1.0)
  
Alignment Match Calculation:
  EntityAlignment = Entity's alignment (Good/Evil, Lawful/Chaotic, Pure/Corrupt)
  LuckGodAlignment = Luck God's preferred alignment (e.g., Good, Lawful, Pure)
  
  AlignmentScore = CalculateAlignmentMatch(EntityAlignment, LuckGodAlignment)
  // Returns 0.0-1.0 (1.0 = perfect match, 0.0 = opposite alignment)
  
  If helping entity (increasing luck):
    AlignmentMatch = AlignmentScore (helping aligned entities = good)
  Else if harming entity (decreasing luck):
    AlignmentMatch = 1.0 - AlignmentScore (harming opposite-aligned entities = good)
    
Example:
  Luck God alignment: Good, Lawful, Pure
  Entity alignment: Good, Lawful, Pure (perfect match)
    Player increases luck: AlignmentMatch = 1.0 (helping good entity)
    AlignmentFactor = 0.3 × 1.0 = +0.30 (positive, strong approval)
    
    Player decreases luck: AlignmentMatch = 0.0 (harming good entity)
    AlignmentFactor = 0.3 × 0.0 = 0.0 (neutral, but balance/merit may still apply)
    
  Entity alignment: Evil, Chaotic, Corrupt (opposite)
    Player increases luck: AlignmentMatch = 0.0 (helping evil entity)
    AlignmentFactor = 0.3 × 0.0 = 0.0 (neutral, but balance may still oppose)
    
    Player decreases luck: AlignmentMatch = 1.0 (harming evil entity)
    AlignmentFactor = 0.3 × 1.0 = +0.30 (positive, strong approval)
```

**Combined Relation Change Examples:**
```
Example 1: Normalizing extreme luck (Good alignment match)
  Entity: High merit (0.8), Good alignment, Current luck: +80
  Player: Normalizes to +30
  
  BalanceFactor = +0.15 (approves normalization)
  MeritFactor = +0.20 (luck matches merit better, ExpectedLuck = +60, closer to +30 than +80)
  AlignmentFactor = +0.10 (helping good entity, but less extreme now)
  TotalRelationChange = +0.45 (significant positive relation gain)
  
Example 2: Creating extreme luck (Poor alignment match)
  Entity: Low merit (0.2), Evil alignment, Current luck: 0
  Player: Increases to +80
  
  BalanceFactor = -0.15 (opposes extreme luck)
  MeritFactor = -0.15 (luck doesn't match low merit, ExpectedLuck = -60)
  AlignmentFactor = -0.20 (helping evil entity)
  TotalRelationChange = -0.50 (significant negative relation loss)
  
Example 3: Reducing extreme luck (Good alignment match)
  Entity: Low merit (0.2), Evil alignment, Current luck: +80
  Player: Reduces to -30
  
  BalanceFactor = +0.15 (approves normalization)
  MeritFactor = +0.10 (luck matches low merit better, ExpectedLuck = -60)
  AlignmentFactor = +0.30 (harming evil entity)
  TotalRelationChange = +0.55 (strong positive relation gain)
```

**Relation Recovery:**
```
RecoveryRate = BaseRecoveryRate × (1 + CurrentRelation / 100)

Where:
  BaseRecoveryRate = 0.1-0.5 per day (base recovery speed)
  CurrentRelation = -100 to +100 (negative = hostile, positive = friendly)
  
Example:
  Relation = -30 (hostile):
    RecoveryRate = 0.2 × (1 + (-30) / 100) = 0.2 × 0.7 = 0.14 per day (slow recovery)
    
  Relation = -80 (very hostile):
    RecoveryRate = 0.2 × (1 + (-80) / 100) = 0.2 × 0.2 = 0.04 per day (very slow recovery)
    
  Relation = +20 (friendly):
    RecoveryRate = 0.2 × (1 + 20 / 100) = 0.2 × 1.2 = 0.24 per day (faster recovery)
```

**Relation Thresholds:**
```
Relation > +50: Friendly (slow normalization, may occasionally help entities with good alignment/merit)
Relation > 0: Neutral-Positive (moderate normalization, slight preference for aligned entities)
Relation = 0: Neutral (standard normalization, purely balance-driven)
Relation < 0: Hostile (faster normalization, may oppose player actions)
Relation < -50: Very Hostile (very fast normalization, may actively curse entities with extreme luck)
Relation < -80: Extreme Hostile (extremely fast normalization, may cause random bad luck events)
```

**Player Intuition Hints:**
```
UI Tooltip should display:
  "Luck God Relations: -15 (Hostile)"
  "Opposes: Extreme luck, helping evil entities, luck mismatching merit"
  "Approves: Normalization, helping good entities, luck matching merit"
  
  When adjusting luck, show predicted relation change:
  "If changed to +45: Relations -5 (Better - Normalizing extreme luck)"
  "If changed to +80: Relations -25 (Worse - Creating extreme luck for evil entity)"
```

---

### 4. Luck God Active Opposition

**Active Resistance (At Very Hostile Relations):**
- **Random Bad Luck Events:** Luck God may cause random bad luck events for entities with high luck
- **Accelerated Normalization:** Normalization rate increases significantly
- **Luck Reversal:** Occasional sudden drops in luck (luck God "rebalancing")
- **Entity Curses:** Entities with extreme luck may receive temporary curses

**Random Bad Luck Events:**
```
Event Trigger: Random chance each day if:
  - Player relation < -50 (Very Hostile)
  - Entity has BaseLuck > 30 or BaseLuck < -30
  
Event Probability = (|BaseLuck| / 100) × (|PlayerRelation| / 100) × 0.1

Example:
  Entity with +60 luck, Player relation = -70:
    EventProbability = (60 / 100) × (70 / 100) × 0.1 = 0.042 (4.2% per day)
    
Events:
  - Random injury (minor)
  - Equipment failure
  - Failed crafting attempt
  - Discovery failure
  - Temporary -10 to -20 luck for 1 day
```

---

### 5. Secondary Modification Methods

**Items, Rituals, and Other Methods:**
- **Items:** Equipment that modifies TemporaryLuck (not affected by Luck God normalization)
- **Rituals:** Rituals that modify TemporaryLuck (bypasses Luck God resistance)
- **Sigil Networks:** Area-based TemporaryLuck effects
- **Genealogy:** Inherited BaseLuck traits (natural, not player-modified, not normalized)

---

### 2. Environmental Luck Modifiers

**Environmental Effects:**
- **Sacred Grounds:** +10 to +30 TemporaryLuck (blessed areas)
- **Cursed Lands:** -10 to -30 TemporaryLuck (cursed areas)
- **Lucky Locations:** +5 to +15 TemporaryLuck (naturally lucky places)
- **Unlucky Locations:** -5 to -15 TemporaryLuck (naturally unlucky places)

**Example:**
```
Entity enters Sacred Grounds:
  TemporaryLuck += 20
  Duration: While in area

Entity enters Cursed Lands:
  TemporaryLuck -= 25
  Duration: While in area
```

---

### 3. Item-Based Luck Modifiers

**Lucky Items:**
- **Lucky Charms:** +5 to +20 TemporaryLuck (while equipped)
- **Cursed Items:** -10 to -30 TemporaryLuck (while equipped)
- **Blessed Items:** +10 to +25 TemporaryLuck (while equipped)
- **Artifacts:** +15 to +40 BaseLuck or TemporaryLuck (legendary items)

**Example:**
```
Item: Lucky Rabbit's Foot
  Effect: +15 TemporaryLuck
  Duration: While equipped
  Rarity: Uncommon

Item: Cursed Amulet
  Effect: -20 TemporaryLuck
  Duration: While equipped
  Rarity: Rare

Item: Artifact of Fortune
  Effect: +30 BaseLuck (permanent, cannot be removed)
  Rarity: Legendary
```

---

### 4. Ritual and Sigil-Based Luck

**Ritual Effects:**
- **Fortune Ritual:** +20 to +50 TemporaryLuck (duration: 1 day)
- **Misfortune Ritual:** -20 to -50 TemporaryLuck (duration: 1 day)
- **Permanent Blessing:** +10 to +30 BaseLuck (permanent, high cost)

**Sigil Network Effects:**
- **Lucky Sigil Network:** +10 to +30 TemporaryLuck (while in network area)
- **Unlucky Sigil Network:** -10 to -30 TemporaryLuck (while in network area)

---

### 5. Genealogy and Trait-Based Luck

**Inherited Luck:**
- **Lucky Genealogy:** +5 to +15 BaseLuck (inherited trait)
- **Unlucky Genealogy:** -5 to -15 BaseLuck (inherited trait)
- **Cursed Bloodline:** -10 to -25 BaseLuck (inherited curse)

**Example:**
```
Genealogy: Lucky Human
  BaseLuck: +10 (inherited)
  Trait: "Born Lucky"

Genealogy: Cursed Lineage
  BaseLuck: -20 (inherited)
  Trait: "Cursed Blood"
```

---

## Luck Accumulation and Decay

### Luck God Normalization (Primary Decay Mechanism)

**Automatic Normalization:**
- **Primary Decay Source:** Luck God normalizes player-modified BaseLuck toward 0
- **Normalization Rate:** Based on distance from neutral and player relations with Luck God
- **Only Affects Player-Modified Luck:** Natural BaseLuck (from genealogy, etc.) is not normalized
- **TemporaryLuck Not Affected:** Temporary modifiers (items, rituals) bypass normalization

**Normalization Process:**
```
Each Day/Tick:
  if (BaseLuck is player-modified AND BaseLuck != 0):
    NormalizationAmount = NormalizationRate × DeltaTime
    if (BaseLuck > 0):
      BaseLuck = max(0, BaseLuck - NormalizationAmount)
    else:
      BaseLuck = min(0, BaseLuck + NormalizationAmount)
```

**Tracking Player-Modified Luck:**
```csharp
public struct LuckStat : IComponentData
{
    public float BaseLuck;           // -100 to +100 (current luck)
    public float PlayerModifiedLuck; // Amount of luck from player manipulation (for normalization tracking)
    public float TemporaryLuck;      // Temporary modifiers (not normalized)
    
    // If PlayerModifiedLuck != 0, then BaseLuck contains player modifications
    // Luck God normalizes PlayerModifiedLuck, which affects BaseLuck
}
```

---

### Natural Luck Variation (Minor)

**Lifetime Luck Drift (Non-Player-Modified):**
- **Natural BaseLuck** can slowly drift over time (simulating life's ups and downs)
- **Drift Rate:** ±0.1 to ±0.5 per year (random, small changes)
- **Only Applies to:** Natural BaseLuck (not player-modified)
- **Drift Range:** Clamped to -100 to +100

**Example:**
```
Entity starts with BaseLuck = 0 (natural)
After 1 year: BaseLuck = +0.3 (slight positive drift)
After 5 years: BaseLuck = +1.2 (accumulated positive drift)
After 10 years: BaseLuck = -0.5 (negative drift occurred)
```

---

### Temporary Luck Decay

**Temporary Modifier Decay:**
- **Duration-Based:** TemporaryLuck modifiers expire after duration
- **Gradual Decay:** Some modifiers decay gradually (e.g., -1 per day)
- **Event-Based:** Some modifiers removed by events (e.g., curse removal ritual)
- **Not Affected by Luck God:** Temporary modifiers bypass normalization

**Example:**
```
TemporaryLuck = +40 (from blessing)
Duration: 1 hour
After 1 hour: TemporaryLuck = 0 (modifier expires)

TemporaryLuck = -30 (from curse)
Duration: Gradual decay (-5 per day)
After 1 day: TemporaryLuck = -25
After 6 days: TemporaryLuck = 0 (curse expires)
```

---

## Integration with Existing Systems

### Combat Stats Integration

**Critical Chance Modification:**
```csharp
// In CombatStats calculation
float CalculateCriticalChance(IndividualStats stats, LuckStat luck)
{
    float baseCrit = stats.Finesse / 5f + weaponCritBonus;
    float luckModifier = luck.EffectiveLuck / 100f * 0.5f; // 50% influence
    return math.clamp(baseCrit + luckModifier, 0f, 1f);
}
```

---

### Crafting System Integration

**Crafting Quality Modification:**
```csharp
// In crafting quality calculation
float CalculateCraftingQuality(SkillLevel skill, MaterialQuality material, LuckStat luck)
{
    float baseQuality = skill * 0.6f + material * 0.4f;
    float luckModifier = luck.EffectiveLuck / 100f * 0.4f; // 40% influence
    return math.clamp(baseQuality + luckModifier, 0f, 1f);
}
```

---

### Research System Integration

**Research Breakthrough Modification:**
```csharp
// In research breakthrough calculation
float CalculateBreakthroughChance(ResearcherProfile researcher, TechDifficulty difficulty, LuckStat luck)
{
    float baseBreakthrough = researcher.Skill / 100f * (1f - difficulty);
    float luckModifier = luck.EffectiveLuck / 100f * 0.2f; // 20% influence
    return math.clamp(baseBreakthrough + luckModifier, 0f, 1f);
}
```

---

## Component Structures

### Core Luck Components

```csharp
// Base luck stat
public struct LuckStat : IComponentData
{
    public float BaseLuck;           // -100 to +100 (default: 0, includes player modifications)
    public float PlayerModifiedLuck; // Amount of BaseLuck from player manipulation (for normalization tracking)
    public float TemporaryLuck;      // Temporary modifiers (sum of all temporary modifiers, not normalized)
    
    /// <summary>
    /// Total effective luck (BaseLuck + TemporaryLuck).
    /// Clamped to -100 to +100.
    /// </summary>
    public float EffectiveLuck => math.clamp(BaseLuck + TemporaryLuck, -100f, 100f);
    
    /// <summary>
    /// True if entity has player-modified luck (subject to Luck God normalization).
    /// </summary>
    public bool HasPlayerModifiedLuck => math.abs(PlayerModifiedLuck) > 0.01f;
}

// Luck God relation tracking (player-level component)
public struct LuckGodRelation : IComponentData
{
    public float Relation;           // -100 to +100 (relation with Luck God)
    public float LastLuckManipulationTime; // Time of last luck manipulation
    public float NormalizationResistance;  // How much player resists normalization (0-1)
    
    // Luck God preferences (for relation calculations)
    public Alignment LuckGodAlignment;     // Luck God's alignment preferences
    public float BalanceThreshold;         // Threshold for extreme luck (default: 30)
    public float MeritWeight;              // How much merit matters (default: 0.5)
    public float AlignmentWeight;          // How much alignment matters (default: 0.3)
    public float BalanceWeight;            // How much balance matters (default: 0.2)
}

// Entity merit tracking (for merit-based relation calculations)
public struct EntityMerit : IComponentData
{
    public float MeritScore;        // 0.0-1.0 (calculated from stats, achievements, behavior)
    public float StatsAverage;      // Average of core stats (0.0-1.0)
    public float AchievementScore;  // Fame/Glory/Renown normalized (0.0-1.0)
    public float BehaviorScore;     // Based on outlook/alignment/deeds (0.0-1.0)
    public float LastUpdateTime;    // When merit was last calculated
}

// Individual luck modifier (stackable)
public struct LuckModifier : IBufferElementData
{
    public float ModifierValue;      // Positive = good luck, negative = bad luck
    public float Duration;            // Duration in seconds (0 = permanent)
    public float TimeRemaining;       // Time remaining (decremented each frame)
    public FixedString64Bytes Source; // Source of modifier (blessing, curse, item, etc.)
}

// Luck modifier source tracking
public struct LuckModifierSource : IComponentData
{
    public FixedString64Bytes SourceId;  // Unique identifier for modifier source
    public float ModifierValue;          // Modifier value
    public bool IsPermanent;              // True if permanent, false if temporary
    public float Duration;                // Duration if temporary (0 if permanent)
}
```

---

### Luck System State

```csharp
// Luck system state (for tracking and debugging)
public struct LuckSystemState : IComponentData
{
    public float TotalEffectiveLuck;     // Current effective luck (BaseLuck + TemporaryLuck)
    public int ModifierCount;            // Number of active modifiers
    public float LifetimeLuckDrift;       // Accumulated lifetime drift
    public float LastDriftUpdate;         // Time of last drift update
}
```

---

## Design Philosophy

### Maximum Flexibility

**Core Principle:** Luck affects all rolls, but influence varies by roll type.

**Flexibility Features:**
- **Configurable Influence:** Each roll type has configurable luck influence factor
- **Player Control:** Players can modify luck through multiple methods (miracles, items, rituals, etc.)
- **Lifetime Impact:** Luck affects entity from birth to death
- **Stackable Modifiers:** Multiple luck modifiers can stack (temporary + permanent + environmental)

**Example Flexibility:**
- **High Luck Entity (+80):** 40% crit chance (vs 10% base), 80% crafting quality (vs 60% base), 50% treasure discovery (vs 5% base)
- **Low Luck Entity (-60):** 0% crit chance (clamped), 36% crafting quality, 0% treasure discovery (clamped)
- **Neutral Luck Entity (0):** Base values unchanged

---

### Balance Considerations

**Luck Influence Caps:**
- **Maximum Influence:** Luck can modify rolls by up to ±50% (for 0.5 influence factor)
- **Minimum Influence:** Luck can modify rolls by up to ±20% (for 0.2 influence factor)
- **Clamping:** All modified rolls are clamped to valid ranges (0.0-1.0, 0-100%, etc.)

**Prevents:**
- **Overpowered Luck:** High luck doesn't guarantee success (still requires base skill/stats)
- **Underpowered Luck:** Low luck doesn't guarantee failure (base skill/stats still matter)
- **Extreme Outcomes:** Luck modifiers are bounded and reasonable

---

## UI Implementation Notes

### Entity Profile Tooltip Integration

**Slider Component:**
- **Position:** Entity profile tooltip, below basic stats
- **Visual:** Horizontal slider with -100 to +100 range
- **Current Value Display:** Shows current BaseLuck value and EffectiveLuck (with temporary modifiers)
- **Cost Preview:** Shows mana cost before adjustment
- **Relation Warning:** Displays current Luck God relation and warning if hostile

**Tooltip Layout:**
```
┌─────────────────────────────────────────┐
│ Entity Name: John the Blacksmith       │
│ Level: 15 | Health: 120/120            │
│                                         │
│ Stats:                                  │
│   Physique: 65  Finesse: 80            │
│   Intellect: 50  Will: 60              │
│                                         │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ │
│                                         │
│ Luck: [-100]──────[●]──────[+100]      │
│ Current Base Luck: +45                 │
│ Effective Luck: +55 (with temp +10)    │
│                                         │
│ Cost to change: 23 mana                │
│ [Adjust Luck]                           │
│                                         │
│ ⚠️ Luck God Relations: -15 (Hostile)   │
│ Normalization Rate: 0.72/day           │
│                                         │
│ Luck God Preferences:                  │
│ • Opposes: Extreme luck (>±30)         │
│ • Approves: Normalization, good merit  │
│ • Entity Merit: High (0.75)            │
│ • Expected Luck: +50 (based on merit)  │
│                                         │
│ Predicted Relation Change:             │
│   To +30: +10 (Better - Normalizing)   │
│   To +50: +5 (Better - Matches merit)  │
│   To +80: -20 (Worse - Extreme luck)   │
│                                         │
└─────────────────────────────────────────┘
```

**Interaction Flow:**
1. Player opens entity profile tooltip
2. Player sees current BaseLuck value on slider
3. Player drags slider to desired value
4. UI calculates and displays mana cost
5. Player confirms adjustment
6. Mana is deducted, BaseLuck is updated, PlayerModifiedLuck is tracked
7. Luck God relation penalty is applied
8. Normalization begins (if applicable)

---

## Open Questions

- **Luck Inheritance:** Should luck be inherited from parents? (Genealogy-based luck, natural, not normalized)
- **Luck Transfer:** Can entities transfer luck to others? (Luck-sharing rituals, TemporaryLuck only)
- **Luck Trading:** Can entities trade luck? (Luck economy, TemporaryLuck only)
- **Luck Events:** Should extreme luck trigger special events? (Already implemented for very hostile relations)
- **Normalization Resistance:** Should players be able to resist normalization? (May add as progression gate)
- **Luck God Favor:** Can players gain positive relations with Luck God? (May add as alternative playstyle)

---

## Related Documentation

- **Individual Stats System**: `godgame/Docs/Individual_Template_Stats.md`
- **Combat Stats System**: `puredots/Packages/com.moni.puredots/Runtime/Runtime/Combat/CombatStats.cs`
- **Miracle System**: `godgame/Docs/Concepts/Miracles/Miracle_Crafting_System.md`
- **Ritual System**: `godgame/Docs/Concepts/Magic/Rituals_And_Sigil_Networks.md`
- **Genealogy System**: `puredots/Docs/Concepts/Core/Genealogy_Mixing_System.md`

