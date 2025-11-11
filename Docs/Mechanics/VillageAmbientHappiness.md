# Mechanic: Village Ambient Happiness System

## Overview

**Status**: Concept
**Complexity**: Moderate
**Category**: Social / Environment

**One-line description**: Villages accumulate ambient happiness from environmental factors (building proximity, spatial configuration) which dynamically influences individual villager morale alongside their baseline stats.

## Core Concept

Villages function as **aggregate entities** that maintain two distinct happiness/morale values:

1. **Baseline Happiness**: Averaged from individual villagers' personal morale stats - relatively stable
2. **Ambient Happiness**: Environmental morale that fluctuates based on spatial factors - highly dynamic

Villagers are **affected by the ambient level** - they sense and respond to the environmental happiness of their village, creating feedback loops where good/bad environments influence individual behavior.

## How It Works

### Basic Rules

1. **Village Aggregate Entity** tracks both baseline (individual average) and ambient (environmental) happiness
2. **Buildings emit happiness modifiers** based on their type and placement
3. **Proximity determines influence** - closer buildings have stronger effects
4. **Ambient value updates** continuously from building proximity calculations
5. **Villagers read ambient** and have their behavior/morale influenced by it

### Building Proximity Modifiers

**Happiness Decreasing Buildings**:
- **Close Neighbors** (overcrowding): Slight negative when housing too densely packed
- **Refinery/Industrial**: Moderate negative (noise, pollution, smell)
- **Waste/Disposal**: Strong negative close-range, tapers with distance

**Happiness Increasing Buildings**:
- **Nursery**: Moderate positive (life, youth, hope)
- **Temple/Shrine**: Strong positive (spirituality, purpose, gathering)
- **Plaza/Square**: Moderate positive (social space, community)
- **Garden/Park**: Moderate positive (nature, beauty, tranquility)

### Parameters and Variables

| Parameter | Suggested Value | Range | Effect |
|-----------|----------------|-------|--------|
| Proximity Falloff | Exponential | Linear/Exp/Inv | How quickly influence fades with distance |
| Update Frequency | Per-tick or batched | 0.1s - 5s | How often ambient recalculates |
| Ambient Influence Weight | 0.3-0.7 | 0-1 | How much ambient affects villagers vs personal baseline |
| Building Radius | Building-specific | 5m - 50m | Max influence distance per building type |

### Calculation Flow

```
For each Village Aggregate:
  1. Calculate Baseline = Average(all villager personal morale)
  2. Calculate Ambient:
     a. Query all buildings within village bounds
     b. For each villager position (or village center):
        - Sum building influences * distance_falloff
     c. Ambient = Weighted average of all position samples
  3. Store both values on Village entity

For each Villager:
  1. Read personal baseline morale
  2. Read village ambient happiness
  3. Calculate ambient_weight based on villager outlook/alignment
  4. Apply severity modifiers (alignment-specific thresholds)
  5. Effective Morale = Lerp(baseline, ambient, ambient_weight)
  6. Apply to behavior (work speed, social interactions, migration desire)
```

### Outlook & Alignment Differentiation

**Core Principle**: Not all villagers react equally to ambient conditions. Personal outlooks and alignments determine both **sensitivity** (how much they care) and **interpretation** (what they care about).

#### Ambient Sensitivity by Alignment

Different alignments have different `ambient_weight` values:

| Alignment Type | Ambient Weight | Reasoning |
|----------------|----------------|-----------|
| **High Lawfulness** | 0.6-0.8 | Order-focused; environment matters for stability |
| **High Altruism** | 0.7-0.9 | Empathetic; deeply affected by community suffering |
| **High Integrity** | 0.3-0.5 | Self-directed; less swayed by surroundings |
| **Low All (Pragmatic)** | 0.5-0.6 | Balanced; moderate environmental response |
| **Chaotic** | 0.4-0.5 | Individualistic; resists group morale |

#### Building Type Reactions by Outlook

Villagers weight building influences differently based on their outlooks:

**Xenophiles** (outward-looking):
- Plazas/Gathering spaces: **+150%** influence
- Temples: **+100%** influence
- Refineries: **-50%** influence (less bothered by progress)

**Xenophobes** (inward-looking):
- Temples/Shrines: **+200%** influence (tradition)
- Plazas: **-50%** influence (distrust gatherings)
- Refineries: **-150%** influence (hate industrial disruption)

**Materialists** (pragmatic):
- Refineries: **-25%** influence (see as necessary evil)
- Nurseries: **+50%** influence (less sentimental)
- Waste facilities: **-200%** influence (efficiency-minded, hate inefficiency)

**Spiritualists**:
- Temples: **+200%** influence (core identity)
- Gardens/Nature: **+150%** influence
- Refineries: **-150%** influence (defilement)

**Egalitarians**:
- Overcrowding: **-200%** influence (hate inequality)
- Plazas: **+150%** influence (community spaces)

**Authoritarians**:
- Overcrowding: **-50%** influence (hierarchy solves it)
- Temples: **+150%** influence (order through faith)

#### Severity-Based Reactions

Villagers have **threshold responses** when ambient reaches critical levels:

| Ambient Level | Stoic Response | Sensitive Response | Fanatic Response |
|---------------|----------------|--------------------|--------------------|
| **0.8 - 1.0** (Bliss) | +5% productivity | +15% productivity, social events | +25% productivity, proselytizing |
| **0.5 - 0.8** (Content) | Normal | +5% productivity | Normal |
| **0.3 - 0.5** (Neutral) | Normal | Normal | Slight unease |
| **0.1 - 0.3** (Unhappy) | Normal | -10% productivity | -20% productivity, complaining |
| **-0.3 - 0.1** (Miserable) | -5% productivity | -25% productivity, protest | -50% productivity, rebellion risk |
| **< -0.3** (Crisis) | -15% productivity, considers leaving | Immediate flee/riot | Violent uprising guaranteed |

**Stoic**: High Integrity, Neutral outlooks
**Sensitive**: High Altruism, Egalitarian
**Fanatic**: High Lawfulness + extreme outlook combination

#### Implementation: Reaction Profiles

Store reaction profiles on villagers:

```
VillagerAmbientProfile (component):
  - AmbientSensitivity (float 0.3-0.9)
  - BuildingTypeWeights (FixedList64Bytes<float>)
  - SeverityThresholds (float4: unhappy, miserable, crisis, bliss)
  - ThresholdBehavior (enum: Stoic, Sensitive, Fanatic)
```

**System calculates**:
```
1. Raw ambient from village
2. Apply building-type filters based on villager's outlook weights
3. Filtered ambient = weighted sum using BuildingTypeWeights
4. Check severity thresholds for cascading effects
5. Final effective morale = Lerp(baseline, filtered_ambient, sensitivity)
6. Apply threshold behavior modifiers (productivity, event triggers)
```

### Edge Cases

- **Case**: Villager at exact boundary of two villages
  **Resolution**: Sample ambient from both, use weighted blend based on position

- **Case**: Building demolished/constructed mid-calculation
  **Resolution**: Ambient updates next frame; smooth transitions via dampening

- **Case**: Village has no buildings (new settlement)
  **Resolution**: Ambient defaults to neutral (0) until buildings placed

- **Case**: Villager with conflicting outlooks (Xenophile + Authoritarian)
  **Resolution**: Building weights blend (some buildings get mixed signals), net effect averages

## Integration with Other Systems

### Interacts With

| System/Mechanic | Type of Interaction | Priority |
|----------------|---------------------|----------|
| Village Spawning | Initializes aggregate entity | High |
| Building Construction | Updates ambient modifiers | High |
| Villager AI | Reads ambient for behavior | High |
| Alignment System | Determines reaction profiles | High |
| Outlook System | Filters building-type preferences | High |
| Migration System | Threshold-based exodus triggers | Medium |
| Productivity Systems | Severity-modified work efficiency | Medium |
| Social Systems | Ambient affects gathering/events | Low |
| Rebellion/Uprising | Fanatic crisis responses | Medium |

### Emergent Possibilities

- **Happiness Districts**: Players naturally create residential zones away from refineries
- **Temple Clustering**: Villages build multiple temples to boost ambient
- **Industrial Exodus**: Villagers abandon over-industrialized villages
- **Plaza Economy**: Social buildings become strategic placement puzzles
- **Gradient Effects**: Villages naturally segment into happy/unhappy zones
- **Personality Sorting**: Villages self-segregate as compatible outlooks cluster
- **Fanatic Enclaves**: Extreme outlook groups demand specific environmental conditions
- **Stoic Workforce**: High-Integrity villagers become valuable in harsh industrial zones
- **Spiritual Havens**: Temple-dense villages attract spiritualists, repel materialists
- **Crisis Cascades**: One fanatic's threshold break triggers chain reactions in sensitives

## Implementation Notes (DOTS-Specific)

### Entity Structure

```
Village Aggregate Entity:
  - VillageTag (marker)
  - BaselineHappiness (float, averaged from villagers)
  - AmbientHappiness (float, from environment)
  - VillageBounds (AABB or spatial reference)
  - MemberCount (int)

Building Entity:
  - BuildingType (enum)
  - HappinessModifier (float, +/- value)
  - InfluenceRadius (float)
  - Position (LocalTransform)

Villager Entity:
  - PersonalMorale (float, individual baseline)
  - VillageReference (Entity ref to aggregate)
  - EffectiveMorale (float, after ambient + filters)
  - VillagerAmbientProfile (component):
      - AmbientSensitivity (float 0.3-0.9)
      - BuildingTypeWeights (FixedList64Bytes<float>)
      - SeverityThresholds (float4)
      - ThresholdBehavior (enum: Stoic/Sensitive/Fanatic)
  - Alignment (Lawfulness, Altruism, Integrity floats)
  - Outlook (Xenophile/Materialist/etc flags or enum)
```

### System Responsibilities

**AmbientHappinessCalculationSystem** (runs in simulation group):
- Queries all villages
- For each village, spatial query buildings within bounds
- Calculates weighted proximity influences
- Writes `AmbientHappiness` to village entity

**VillagerMoraleUpdateSystem** (runs after ambient calc):
- Reads village `BaselineHappiness` + `AmbientHappiness`
- Reads villager `PersonalMorale` + `VillagerAmbientProfile`
- Applies building-type filters based on outlook weights
- Computes filtered ambient for this villager
- Checks severity thresholds for cascading effects
- Computes `EffectiveMorale` = Lerp(personal, filtered_ambient, sensitivity)
- Writes back to villager + triggers threshold behaviors

**BaselineHappinessAggregationSystem** (runs periodically):
- Queries all villagers in village
- Averages their `PersonalMorale`
- Writes `BaselineHappiness` to village aggregate

### Performance Considerations

- **Spatial queries** are expensive - batch per village, not per villager
- Use **cached building lists** rather than world-query every frame
- Consider **update stagger**: Update 10% of villages per frame for large maps
- **Spatial partitioning**: Use chunk/quadtree to limit query scope
- Ambient can update **less frequently** than individual morale (0.5s-1s intervals)

### Testing Strategy

1. **Unit tests for**:
   - Distance falloff functions
   - Weighted averaging with multiple buildings
   - Edge cases (zero buildings, boundary positions)

2. **Playtests should verify**:
   - Visible behavior changes when temple built near village
   - Villagers leave when refinery placed in center
   - Baseline vs ambient differentiation is meaningful

3. **Balance tests should measure**:
   - Optimal building placement strategies
   - Time-to-equilibrium for ambient changes
   - Threshold values that trigger migration

## Examples

### Example Scenario 1: New Village Blooms

**Setup**: Fresh village spawned, 5 villagers, all neutral morale (0.5)
**Action**: Player builds Temple → Plaza → Nursery in quick succession
**Result**:
- Baseline stays ~0.5 (personal morale unchanged yet)
- Ambient rises to ~0.75 (three positive buildings)
- Villagers' effective morale = 0.5 * 0.3 + 0.75 * 0.7 = **0.675**
- Increased work speed, more social events, lower migration desire

### Example Scenario 2: Industrial Collapse

**Setup**: Thriving village (baseline 0.7, ambient 0.6)
**Action**: Player places Refinery + Waste facility in village center
**Result**:
- Baseline gradually declines as villagers respond (0.7 → 0.6 over time)
- Ambient **immediately drops** to 0.2 (negative buildings dominate)
- Effective morale = 0.6 * 0.3 + 0.2 * 0.7 = **0.32**
- Villagers flee, productivity tanks, village may collapse

### Example Scenario 3: Spatial Strategy

**Setup**: Long village with Refinery on west edge
**Action**: Player builds Temple/Plaza on east edge
**Result**:
- **West villagers**: Ambient ~0.3 (refinery nearby)
- **East villagers**: Ambient ~0.8 (temple + plaza)
- **Center villagers**: Ambient ~0.55 (mixed influences)
- Village has **spatial happiness gradient** - emergent zoning!

### Example Scenario 4: Personality-Driven Reactions

**Setup**: Mixed-personality village, ambient drops to 0.2 (unhappy) due to new refinery
**Villagers in same village, same environment**:

**Villager A - Stoic Materialist** (High Integrity, Materialist):
- Ambient sensitivity: 0.4 (low)
- Refinery weight: -25% (accepts industry)
- Filtered ambient: 0.35 (mitigated)
- Effective morale: Lerp(0.6 baseline, 0.35 ambient, 0.4) = **0.51**
- Behavior: **Barely affected, keeps working normally**

**Villager B - Sensitive Spiritualist** (High Altruism, Spiritualist):
- Ambient sensitivity: 0.85 (very high)
- Refinery weight: -150% (hates industrial defilement)
- Filtered ambient: 0.05 (amplified negativity)
- Effective morale: Lerp(0.6 baseline, 0.05 ambient, 0.85) = **0.13**
- Severity: Hits "Miserable" threshold → -25% productivity
- Behavior: **Protests, demands temple construction, considers leaving**

**Villager C - Fanatic Xenophobe** (High Lawfulness, Xenophobe):
- Ambient sensitivity: 0.7 (high)
- Refinery weight: -150% (tradition violated)
- Filtered ambient: 0.0 (amplified negativity)
- Effective morale: Lerp(0.5 baseline, 0.0 ambient, 0.7) = **0.15**
- Severity: Hits "Miserable" threshold → Fanatic response
- Behavior: **-50% productivity, rebellion risk, organizing uprising**

**Result**: Same village, same raw ambient (0.2), but **three completely different lived experiences**. Stoic keeps working, Sensitive complains, Fanatic rebels. Player must understand village composition to predict reactions!

## Tuning Guidance

### Critical Tuning Knobs

1. **Ambient Influence Weight** (0.3-0.7):
   - Higher = environment dominates, easier to manipulate
   - Lower = personal morale matters more, slower feedback

2. **Building Radii**:
   - Larger = fewer buildings needed, simpler layouts
   - Smaller = precise placement matters, deeper strategy

3. **Falloff Curves**:
   - Linear = predictable, flat
   - Exponential = strong close-range, sharp dropoff (recommended)
   - Inverse = gentle gradient, wide influence

4. **Update Rate**:
   - Faster = responsive, expensive
   - Slower = smooth, cheaper, can feel laggy

### Balance Goals

- Environmental changes should **feel impactful but not instant**
- Bad placement should be **punishing but recoverable**
- Good placement should feel **rewarding and strategic**
- Players should **see the spatial logic** (visual feedback crucial)

## Visual Feedback Recommendations

Since this is environmental and spatial, visualization is critical:

- **Heatmap overlay**: Color-code ground based on ambient happiness
  - **Advanced**: Toggle to show "as seen by outlook type" (Spiritualist view vs Materialist view)
- **Building auras**: Visualize influence radius when placing
  - **Color-coded**: Green for positive, red for negative, intensity = magnitude
- **Village-wide indicator**: Single icon showing aggregate happiness
  - **Sub-indicators**: Show distribution (% stoic/sensitive/fanatic in crisis)
- **Villager indicators**: Small emoji/icon above head reflecting **effective** morale
  - **Tooltip**: Shows "Personal: 0.6, Filtered Ambient: 0.2, Effective: 0.32"
- **Particle effects**: Happy villages sparkle, sad villages look gloomy
- **Threshold warnings**: Flash/pulse when villagers cross severity thresholds
  - Red pulse for fanatics entering crisis
  - Yellow for sensitives entering miserable
- **Personality icons**: Show villager outlook/alignment at-a-glance (for planning placement)

## Revision History

| Date | Change | Reason |
|------|--------|--------|
| 2025-11-10 | Initial draft | Capture village ambient happiness vision for PureDOTS |
| 2025-11-10 | Added outlook/alignment differentiation | Capture personality-driven reaction mechanics |
| 2025-11-10 | Added severity thresholds & building-type filters | Expand depth of individual responses |
| 2025-11-10 | Added Example Scenario 4 | Illustrate differentiated reactions to same environment |

---

*Last Updated: November 10, 2025*
*Document Owner: Design Team*
