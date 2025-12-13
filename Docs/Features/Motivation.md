# Motivation & Legacy Spine Feature

## Overview

**Status**: Implemented  
**Complexity**: Moderate  
**Category**: AI / Behavior / Legacy

**One-line description**: *A shared motivation & legacy spine that provides dreams, aspirations, desires, ambitions, and wishes for both individual entities (villagers, crew) and aggregates (villages, fleets, empires), tracking initiative, loyalty, and legacy points.*

## Core Concept

The Motivation & Legacy system provides a game-agnostic framework for entities to have goals, aspirations, and ambitions. It supports five layers of motivation (Dreams, Aspirations, Desires, Ambitions, Wishes), tracks initiative and loyalty, and awards legacy points for completing locked goals. The system integrates with alignment systems (pure/corrupt, lawful/chaotic, good/evil) and supports modding through data-driven catalogs.

## How It Works

### Basic Rules

1. **Motivation Layers**: Entities can have goals at five layers:
   - **Dreams**: Short-lived, small or re-rollable goals (Sims-style wants)
   - **Aspirations**: Identity arcs ("who I want to become")
   - **Desires**: Medium-term concrete goals
   - **Ambitions**: Long-term, often aggregate-scale end states
   - **Wishes**: Soft/personal wants

2. **Initiative & Loyalty**: Each entity has:
   - **Initiative** (0-200): How likely they are to pursue goals
   - **Loyalty** (0-200): How strongly they prioritize aggregate goals over personal ones

3. **Intent Selection**: The system picks which goal an entity is actively pursuing based on:
   - Importance (how much the entity cares)
   - Initiative (how likely to act)
   - Loyalty (how much they prioritize aggregate goals)
   - Configurable scoring weights

4. **Legacy Points**: Completing locked goals awards legacy points, which can be spent on:
   - Bloodline/dynasty boosts (Godgame)
   - Elite crew perks or lineage ships (Space4X)

5. **Moral Profile**: Entities have alignment axes that bias motivation generation:
   - Corrupt/Pure (-100..100)
   - Chaotic/Lawful (-100..100)
   - Evil/Good (-100..100)
   - Might/Magic (-100..100)
   - Vengeful/Forgiving (-100..100)
   - Craven/Bold (-100..100)

### Parameters and Variables

| Parameter | Component | Default Value | Range | Effect |
|---|---|---|---|---|
| Initiative | `MotivationDrive` | 100 | 0-200 | Likelihood to pursue goals |
| Loyalty | `MotivationDrive` | 50 | 0-200 | Prioritize aggregate goals |
| Importance | `MotivationSlot` | 0 | 0-255 | How much entity cares |
| Progress | `MotivationSlot` | 0 | 0-255 | Goal completion progress |
| Legacy Points | `LegacyPoints` | 0 | int | Rewards for completed goals |
| Scoring Weights | `MotivationScoringConfig` | 1.0/1.0/0.5 | float | Importance/Initiative/Loyalty weights |

### Edge Cases

- **No Active Goal**: If no goal meets the threshold, `ActiveSlotIndex = 255` (no active intent)
- **Zero Initiative**: Entities with zero initiative won't pursue goals
- **Empty Catalog**: System works with empty catalog (no goals available)
- **Rewind Safety**: All systems respect `RewindState` and only mutate during Record mode

## Player Interaction

### Player Decisions

- **Lock Goals**: Players can lock specific motivation slots (Sims-style "pin")
- **Reroll Dreams**: Players can abandon non-locked dreams to trigger refresh
- **Set Loyalty Targets**: Players can assign primary loyalty targets (village, guild, fleet, empire)

### Feedback to Player

- **Visual feedback**: UI can display active goals, progress, and legacy points
- **Numerical feedback**: Info panels show initiative, loyalty, and legacy points

## Balance and Tuning

### Balance Goals

- Ensure meaningful distinction between different motivation layers
- Balance initiative and loyalty to create interesting trade-offs
- Make legacy points valuable but not overpowered

### Tuning Knobs

1. **`MotivationScoringConfig`**: Adjust weights to prioritize importance, initiative, or loyalty
2. **`MotivationConfigState`**: Set default slot counts per layer
3. **`MotivationSpec`**: Define BaseImportance, BaseInitiativeCost, RequiredLoyalty per goal
4. **Reward Formula**: Base reward = Importance / 10, bonuses for locked goals

## Integration with Other Systems

### Interacts With

| System/Mechanic | Type of Interaction | Priority |
|---|---|---|
| **AI Systems** | MotivationIntent drives AI behavior and job selection | High |
| **Alignment Systems** | MoralProfile biases motivation generation and selection | High |
| **Legacy/Dynasty Systems** | LegacyPoints feed into bloodline/dynasty boosts | High |
| **Quest Systems** | Goals can be locked by story/quest systems | Medium |
| **Aggregate Systems** | Villages/fleets/empires can have aggregate ambitions | High |

## Implementation Notes

### Technical Approach

- **PureDOTS Shared Core**: All motivation components and core systems are implemented in the `PureDOTS` package to be game-agnostic
- **Burst-Compiled**: All systems are Burst-compiled for high performance
- **Data-Oriented**: Components are designed as plain old data (POD) structs
- **Moddable**: Catalogs use ScriptableObjects and support additive merging

### Performance Considerations

- Motivation systems run on all entities with `MotivationDrive` (expected to be fewer than total entities)
- Intent selection is rate-limited via `LastInitiativeTick` to prevent excessive switching
- All calculations are Burst-compiled, ensuring high performance

### Testing Strategy

1. **Unit tests for scoring**: Verify correct intent selection scoring with different weights
2. **Unit tests for rewards**: Verify correct legacy point calculation
3. **Unit tests for initialization**: Verify slot buffer creation and component setup
4. **Integration tests**: Ensure game-specific generators/executors/completion systems work correctly

## Modding Guide

### Creating Custom Motivation Specs

1. Create a `MotivationSpecAsset` ScriptableObject
2. Set SpecId, Layer, Scope, Tag, and properties
3. Add to a `MotivationCatalogAsset`
4. Load catalog via `MotivationBootstrapSystem.InitializeMotivationSystem()`

### Creating Custom Generators

Games can create systems that fill empty `MotivationSlot` slots based on:
- `MoralProfile` alignment
- Entity state (mood, needs, culture)
- Current events
- Player input

Example pattern:
```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MyDreamGeneratorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Find entities with empty dream slots
        // Generate appropriate dreams based on entity state
        // Fill slots with new goals
    }
}
```

### Creating Custom Executors

Games can create systems that read `MotivationIntent` and translate into concrete actions:
- Job assignments
- Task creation
- Quest generation
- Behavior modifiers

Example pattern:
```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MyGoalExecutionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Read MotivationIntent for entities
        // Decode SpecId via MotivationCatalog
        // Boost job priority / create tasks / modify behavior
    }
}
```

### Creating Custom Completion Systems

Games can detect when goals are met and add `GoalCompleted` buffer elements:
- Stat thresholds reached
- Villages founded
- Battles won
- Relationships improved

Example pattern:
```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MyCompletionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Check game state for goal completion conditions
        // Add GoalCompleted buffer elements when conditions met
        // MotivationRewardSystem will process them
    }
}
```

## Examples

### Example Scenario 1: Villager with Crafting Dream

**Setup**: A villager has `MotivationDrive` with Initiative=120, Loyalty=50. A dream slot with SpecId=1 (craft rare item), Importance=150, LockFlags=LockedByPlayer.

**Action**: The game runs, `MotivationIntentSelectionSystem` calculates score = 150*1.0 + 120*1.0 + 50*0.5 = 295. This becomes the active intent.

**Result**: `GodgameVillagerGoalExecutionSystem` reads the intent, boosts crafting job priority. When the villager crafts a rare item, `GodgameMotivationCompletionSystem` adds a `GoalCompleted` element. `MotivationRewardSystem` awards 15 legacy points (150/10 base + 10 bonus for LockedByPlayer).

### Example Scenario 2: Village with Survival Ambition

**Setup**: A village entity has `MotivationDrive` with Initiative=80, Loyalty=150. An ambition slot with SpecId=100 (survive last stand), Importance=255, LockFlags=LockedByAggregate.

**Action**: The game runs, intent selection prioritizes this ambition due to high loyalty and importance.

**Result**: Individual villagers with high loyalty to this village prioritize village survival over personal goals. When the village survives, legacy points are awarded to the village entity.

## References and Inspiration

- **Sims-style Wants System**: Short-lived goals that can be locked or rerolled
- **Alignment Systems**: D&D-style alignment axes for moral profiling
- **Legacy/Dynasty Systems**: Rewards for completing meaningful goals

## Revision History

| Date | Change | Reason |
|---|---|---|
| [Current Date] | Initial implementation and documentation | New feature |

---

*Last Updated: [Current Date]*  
*Document Owner: [AI Assistant]*



















