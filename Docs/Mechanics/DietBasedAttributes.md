# Mechanic: Diet-Based Attribute Progression

## Overview

**Status**: Concept
**Complexity**: Moderate
**Category**: Economy / Character Progression

**One-line description**: Villagers gain passive attribute modifiers over time based on their diet composition—vegetables/fruits boost finesse, meat boosts strength, fish boosts will.

## Core Concept

Food is not just survival—**you are what you eat**. Villagers who consume specific food types gradually develop physical and mental traits aligned with their diet. This creates emergent specialization, strategic food production chains, and meaningful resource diversity beyond simple calorie counts.

## How It Works

### Basic Rules

1. **Three Primary Food Categories**:
   - **Vegetables/Fruits** → Increases **Finesse** (dexterity, precision, crafting quality)
   - **Meat** → Increases **Strength** (physical power, hauling capacity, melee damage)
   - **Fish** → Increases **Will** (mental fortitude, morale resilience, focus)

2. **Passive Accumulation**: Each meal consumed contributes to a hidden "diet history" buffer
3. **Time-Gated Progression**: Modifiers apply gradually as diet patterns stabilize over days/weeks
4. **Decay & Rebalancing**: Switching diets slowly shifts attributes toward the new pattern

### Attribute Definitions

| Attribute | Primary Effects | Secondary Effects |
|-----------|----------------|-------------------|
| **Finesse** | Crafting speed +%, Crafting quality +%, Tool durability | Movement precision, ranged accuracy |
| **Strength** | Hauling capacity +kg, Melee damage +%, Construction speed | Mining speed, Physical stamina |
| **Will** | Morale resistance +%, Ambient sensitivity -%, Focus duration | Learning speed, Spiritual work efficiency |

### Parameters and Variables

| Parameter | Suggested Value | Range | Effect |
|-----------|----------------|-------|--------|
| **Meals to Significance** | 20-30 meals | 10-50 | How many meals before noticeable modifier |
| **Max Modifier per Attribute** | +0.3 (+30%) | 0.1 - 0.5 | Cap on single-attribute specialization |
| **Decay Rate** | 0.01 per day without food type | 0.005 - 0.02 | How fast attributes fade when diet changes |
| **Diet History Window** | 30 days | 7 - 60 | How far back diet is tracked |
| **Contribution per Meal** | 0.01 per attribute | 0.005 - 0.02 | Rate of attribute gain per meal |

### Calculation Flow

```
For each Villager:
  1. Track diet history buffer (circular buffer, 30-day window)
  2. On meal consumed:
     a. Add food type to buffer (timestamp + type)
     b. Remove meals older than window
  3. Calculate diet composition:
     Vegetables_ratio = count(vegetables) / total_meals
     Meat_ratio = count(meat) / total_meals
     Fish_ratio = count(fish) / total_meals
  4. Target modifiers:
     Target_Finesse = Vegetables_ratio * MaxModifier
     Target_Strength = Meat_ratio * MaxModifier
     Target_Will = Fish_ratio * MaxModifier
  5. Drift current modifiers toward targets:
     Current_Finesse += (Target_Finesse - Current_Finesse) * drift_rate * dt
     (same for Strength, Will)
  6. Apply clamping [0, MaxModifier]
  7. Use modifiers in gameplay systems
```

### Edge Cases

- **Case**: Villager eats mixed meals (stew with vegetables + meat)
  **Resolution**: Count as fractional contribution to multiple attributes (0.5 veg, 0.5 meat)

- **Case**: Villager starving (no meals)
  **Resolution**: Attributes decay toward zero, starvation effects override diet bonuses

- **Case**: New villager spawned/migrated
  **Resolution**: Initialize with neutral diet history (no modifiers), begins accumulating immediately

- **Case**: Villager exclusively eats one food type
  **Resolution**: Rapidly reaches cap in one attribute, others remain at zero (extreme specialization)

## Player Interaction

### Player Decisions

- **Food Production Strategy**: Build farms for finesse workers, ranches for haulers, fisheries for morale-sensitive roles
- **Job Assignment**: Match villagers' diet-boosted stats to appropriate roles
- **Resource Allocation**: Prioritize specific food types for specific villages/professions
- **Emergency Rebalancing**: Switch diets when needs change (e.g., feed meat to prepare for combat)

### Skill Expression

- **Min-Max Optimization**: Players can hyper-specialize villagers by controlling diet
- **Predictive Planning**: Anticipate future needs and pre-condition villagers with appropriate diets
- **Resource Chain Mastery**: Build efficient production pipelines for targeted foods
- **Crisis Adaptation**: Quickly shift diets when environmental/strategic conditions change

### Feedback to Player

- **Visual**: Villager icons/tooltips show attribute modifiers (Finesse +15%, Strength +5%)
- **Numerical**: Diet composition bar chart (last 30 meals: 60% veg, 30% meat, 10% fish)
- **Predictive**: UI shows "If current diet continues: Finesse +25% in 10 days"
- **Comparison**: Highlight best-suited villagers for job types based on current stats

## Balance and Tuning

### Balance Goals

- **Meaningful Diversity**: All three food types should feel valuable, no "best diet"
- **Strategic Depth**: Specialized diets offer clear advantages without trivializing challenges
- **Time Investment**: Attribute gains should reward planning but not feel grindy
- **Flexibility**: Diet changes should be viable mid-game without punishing experimentation

### Tuning Knobs

1. **Meals to Significance** (20-30): Lower = faster feedback, higher = longer-term strategy
2. **Max Modifier** (0.3): Higher = extreme specialization viable, lower = generalists preferred
3. **Decay Rate** (0.01/day): Higher = diet changes matter more, lower = permanent bonuses
4. **Diet Window** (30 days): Longer = smoother transitions, shorter = more reactive

### Known Issues / Design Questions

- **Question**: Should mixed diets (balanced eating) provide a bonus, or is specialization always better?
  - **Option A**: Balanced diet (33/33/33) grants small "Well-Fed" bonus to all stats
  - **Option B**: Specialization is king, generalists are suboptimal

- **Question**: Do different villager outlooks/alignments prefer different foods?
  - **Spiritualists** might prefer vegetables (ascetic, plant-based)
  - **Materialists** might prefer meat (pragmatic, protein)
  - **Xenophiles** might prefer fish (exotic, trade-oriented)

- **Question**: Can villagers develop **preferences** based on diet history?
  - Long-term meat eaters might reject vegetables, creating inertia

## Integration with Other Systems

### Interacts With

| System/Mechanic | Type of Interaction | Priority |
|----------------|---------------------|----------|
| Resource Production | Determines food availability | High |
| Job Assignment | Attributes affect job performance | High |
| Productivity Systems | Modifiers directly scale output | High |
| Morale/Ambient Happiness | Will affects ambient sensitivity | Medium |
| Combat System | Strength/Finesse affect combat stats | Medium |
| Crafting System | Finesse affects quality rolls | High |
| Migration System | Villagers seek villages with preferred food | Low |
| Trading System | Food types become strategic commodities | Medium |

### Emergent Possibilities

- **Specialist Villages**: "Crafting Village" fed vegetables, "Warrior Village" fed meat
- **Food Trade Networks**: Villages with fisheries export fish to scholarly/temple towns
- **Crisis Diets**: Emergency meat rations to prepare villagers for combat in days
- **Cultural Foodways**: Different factions develop distinct dietary traditions
- **Job Monopolies**: High-finesse villagers dominate crafting, creating class stratification
- **Dietary Arms Race**: Players optimize food chains to create super-specialized workers
- **Starvation Tragedy**: Villages lose accumulated bonuses during famine, setback amplifies disaster
- **Dietary Signaling**: Player can infer village specialization by observing food production

## Implementation Notes (DOTS-Specific)

### Entity Structure

```
Villager Entity:
  - DietHistory (DynamicBuffer<DietEntry>)
      struct DietEntry { float Timestamp; FoodType Type; float Amount; }
  - DietModifiers (component):
      - CurrentFinesse (float 0-MaxModifier)
      - CurrentStrength (float 0-MaxModifier)
      - CurrentWill (float 0-MaxModifier)
      - TargetFinesse (float, calculated from history)
      - TargetStrength (float)
      - TargetWill (float)
  - BaseAttributes (Finesse, Strength, Will - inherent)
  - EffectiveAttributes (Base + DietModifiers - final computed)

Food Resource:
  - FoodType (enum: Vegetable, Meat, Fish, Mixed)
  - NutritionValue (calories, for survival)
  - AttributeContribution (FoodType → attribute mapping)
```

### System Responsibilities

**DietTrackingSystem** (runs on meal consumption):
- Listens for "Meal Consumed" events
- Appends to villager's `DietHistory` buffer
- Prunes entries older than diet window
- Recalculates diet composition percentages
- Updates `TargetFinesse/Strength/Will` based on composition

**DietModifierDriftSystem** (runs periodically, e.g., daily):
- Reads `TargetFinesse/Strength/Will` and `CurrentFinesse/Strength/Will`
- Drifts current toward target at decay rate
- Writes updated `CurrentFinesse/Strength/Will`

**AttributeAggregationSystem** (runs before gameplay systems):
- Reads `BaseAttributes` + `DietModifiers`
- Computes `EffectiveAttributes` = Base + Current modifiers
- Writes `EffectiveAttributes` for consumption by other systems

**JobPerformanceSystem** (uses attributes):
- Reads `EffectiveAttributes`
- Applies Finesse to crafting speed/quality
- Applies Strength to hauling/melee
- Applies Will to morale checks

### Performance Considerations

- **Diet buffers** can be large if tracking 30+ days of meals (3 meals/day = 90 entries)
  - Use circular buffer or fixed-size array with index wrapping
  - Consider storing daily aggregates instead of per-meal entries
- **Diet composition recalc** is O(buffer_size) per villager per meal
  - Amortize by only recalculating when significant change occurs (every N meals)
  - Cache composition percentages, invalidate on buffer change
- **Drift updates** can run infrequently (once per in-game day) to save perf
- **Large populations**: Stagger updates across frames (update 10% of villagers per frame)

### Testing Strategy

1. **Unit tests for**:
   - Diet history buffer management (add, prune, circular behavior)
   - Composition calculation with various diet patterns
   - Drift math (convergence to target, decay rates)
   - Mixed meal fractional contributions

2. **Playtests should verify**:
   - 30 meals of vegetables produce visible Finesse increase
   - Switching from meat to fish gradually shifts Strength → Will
   - Extreme specialization (100% one food) reaches cap smoothly
   - Starvation causes attribute decay

3. **Balance tests should measure**:
   - Time-to-cap for each attribute (should be similar)
   - Effectiveness of specialized vs balanced diets
   - Optimal diet transitions for job changes
   - Economic viability of producing each food type

## Examples

### Example Scenario 1: Crafting Specialist Emerges

**Setup**: New village, random villager "Ada" assigned to crafting job
**Action**: Player prioritizes vegetable farming, Ada eats 90% vegetables for 30 days
**Result**:
- Ada's Finesse: 0 → +27% over 30 days
- Ada's Strength/Will: remain near 0
- Ada's crafting speed: +27%, quality rolls: +27%
- **Emergence**: Ada becomes village's master crafter, other villagers less efficient
- **Player decision**: Keep Ada on crafting permanently, protect her food supply

### Example Scenario 2: Emergency Combat Preparation

**Setup**: Peaceful village, raiders approaching in 15 days
**Action**: Player switches village diet to 100% meat immediately
**Result**:
- All villagers begin drifting toward Strength bonuses
- After 15 days: Average Strength +15% (halfway to cap)
- Combat effectiveness improves meaningfully but not drastically
- **Trade-off**: Crafting efficiency drops (Finesse decay), economy suffers
- **Post-combat**: Player must rebalance diet to restore specialists

### Example Scenario 3: Balanced Scholar

**Setup**: Village with temple, player wants a "scholar" villager for research/spiritual work
**Action**: Player feeds candidate "Ravi" diet of 50% fish (Will), 30% vegetables (Finesse), 20% meat
**Result**:
- Ravi's Will: +15% (primary, for focus/morale)
- Ravi's Finesse: +9% (helps with writing/precision work)
- Ravi's Strength: +6% (minimal, not priority)
- **Versatility**: Ravi performs well in spiritual tasks + decent at crafting side tasks
- **Trade-off**: Not as optimized as a 100% specialist, but flexible

### Example Scenario 4: Cultural Dietary Tradition

**Setup**: Multiple villages, one near coast (fish access), one inland (farms/ranches)
**Emergent Behavior**:
- **Coastal village**: Naturally develops high-Will population (fish diet)
  - Becomes spiritually focused, temple-heavy, morale-stable
  - Players notice and specialize it as "Spiritual Center"
- **Inland village**: Develops high-Strength/Finesse mix (meat + veg)
  - Becomes industrial/crafting hub
  - Players notice and lean into specialization
- **Result**: Villages develop distinct "cultures" through diet, not by design but by resource availability

## Tuning Guidance

### Critical Balance Questions

1. **Should mixed meals dilute bonuses?**
   - **Option A**: Stew (50% veg, 50% meat) gives 0.5x contribution to each
   - **Option B**: Stew gives full contribution to both (encourages complex recipes)
   - **Recommendation**: Option A (encourages specialization, adds strategic depth)

2. **Should there be "super foods"?**
   - Rare/expensive foods that contribute 2x to attributes
   - Creates late-game progression, rewards exploration/trade
   - Risk: trivializes system if too easy to obtain

3. **Should attributes decay to zero or to baseline?**
   - **Decay to zero**: Diet must be maintained constantly (active management)
   - **Decay to baseline**: Once gained, attributes stabilize (reward for past effort)
   - **Recommendation**: Decay toward baseline (70% of peak), not zero (avoids tedium)

### Designer-Facing Tuning

Expose these values in config/scriptable objects:
- `MaxModifierPerAttribute` (per food type)
- `MealsToSignificance` (pacing dial)
- `DecayRatePerDay` (forgiveness dial)
- `DietWindowDays` (memory length)
- `DriftSpeed` (responsiveness to diet changes)

## Visual Feedback Recommendations

- **Villager attribute bars**: Small UI showing Finesse/Strength/Will with color coding
  - Green (vegetables), Red (meat), Blue (fish)
- **Diet pie chart**: Tooltip showing recent diet composition
- **Predictive arrows**: "↑ Finesse +5% in 3 days" based on current diet trend
- **Food icons**: Show preferred foods above villager heads when assigned to optimal diet
- **Glow effects**: Specialized villagers (>25% in one attribute) get subtle colored aura
- **Job fit indicator**: Show match % between villager's attributes and job requirements

## Revision History

| Date | Change | Reason |
|------|--------|--------|
| 2025-11-10 | Initial draft | Capture diet-based attribute progression vision |

---

*Last Updated: November 10, 2025*
*Document Owner: Design Team*
