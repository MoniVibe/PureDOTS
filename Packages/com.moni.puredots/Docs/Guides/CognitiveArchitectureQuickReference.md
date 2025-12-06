# Cognitive Architecture Quick Reference

Quick lookup table for common operations.

## Component Checklist

For agents with cognitive learning:
- [ ] `ProceduralMemory`
- [ ] `LimbicState`
- [ ] `ContextHash`
- [ ] `CausalLink` (buffer)
- [ ] `DetectedAffordance` (buffer)
- [ ] `CognitiveStats` (optional, for multipliers)

For objects with affordances:
- [ ] `Affordance`
- [ ] `SpatialIndexedTag`

## Common Operations

### Report Action Outcome

```csharp
ProceduralMemoryReinforcementSystem.ReinforceAction(
    ref memory,
    contextHash.Hash,
    actionId,
    success ? 1.0f : 0.0f,
    memory.LearningRate,
    in cognitiveStats);
```

### Query Best Action for Context

```csharp
float bestScore = 0f;
ActionId bestAction = ActionId.None;
for (int i = 0; i < memory.TriedActions.Length; i++)
{
    if (memory.ContextHash == contextHash.Hash)
    {
        if (memory.SuccessScores[i] > bestScore)
        {
            bestScore = memory.SuccessScores[i];
            bestAction = memory.TriedActions[i];
        }
    }
}
```

### Reinforce Causal Link

```csharp
CausalChainSystem.ReinforceCausalLink(
    ref causalLinks,
    (ushort)actionId,
    (ushort)outcomeId,
    successResult,
    currentTick,
    0.1f);
```

### Query Causal Link

```csharp
float expectedUtility = CausalChainSystem.QueryCausalLink(
    causalLinks,
    (ushort)actionId,
    (ushort)desiredOutcome);
```

### Deterministic Exploration

```csharp
float factor = DeterministicExplorationSystem.ComputeExplorationFactor(
    currentTick, entity);
ActionId exploreAction = DeterministicExplorationSystem
    .SelectExplorationAction(factor);
```

### Check Emotion-Driven Behavior

```csharp
bool shouldAvoid = EmotionDrivenLearningSystem.ShouldAvoidContext(
    limbic.Fear, contextFailureRate);
bool helpSeeking = EmotionDrivenLearningSystem.ShouldTriggerHelpSeeking(
    limbic.Frustration);
float exploreProb = EmotionDrivenLearningSystem
    .ComputeExplorationProbability(limbic.Curiosity);
```

### Use Detected Affordances

```csharp
if (affordances.Length > 0)
{
    var best = affordances[0]; // Already sorted by utility
    // Use best.ObjectEntity for interaction
}
```

## System Update Frequencies

| System Group | Frequency | Systems |
|-------------|-----------|---------|
| ReflexSystemGroup | 60Hz | ReflexSystem |
| LearningSystemGroup | 1Hz | ProceduralLearningSystem, AffordanceDetectionSystem, CausalChainSystem, ContextHashingSystem, SkillAcquisitionSystem |
| MotivationSystemGroup | 0.2Hz | LimbicModulationSystem, EmotionDrivenLearningSystem |

## Default Values

| Component | Field | Default |
|-----------|-------|---------|
| ProceduralMemory | LearningRate | 0.1 |
| LimbicState | Curiosity | 0.5 |
| LimbicState | Fear | 0.0 |
| LimbicState | Frustration | 0.0 |
| LimbicState | RecentSuccessRate | 0.5 |
| ContextHash | Hash | 0 (computed) |

## Action ID Mapping

| ActionId | IntentKind | Description |
|----------|------------|-------------|
| Move | Move | Basic movement |
| Climb | Interact | Climb object |
| Push | Interact | Push object |
| Pull | Interact | Pull object |
| Jump | Move | Jump action |
| Throw | Interact | Throw object |
| Use | Interact | Use object |
| Grab | Interact | Grab object |
| Drop | Interact | Drop object |
| EscapePit | Move | Macro-action |

## Affordance Utility Formula

```
UtilityScore = RewardPotential / Effort
```

Higher utility = better affordance. Affordances sorted by utility in `DetectedAffordance` buffer.

## Learning Rate Formula

```
EffectiveLearningRate = BaseRate * (0.6 * Intelligence + 0.4 * Wisdom) * (1 + Curiosity * 0.5)
```

## Exploration Probability Formula

```
ExplorationProbability = 0.1 + Curiosity * (1 - Focus / MaxFocus)
```

## Emotion Update Formulas

```
Curiosity += (SuccessRateStable ? -0.01f : +0.05f)
Fear += (RecentFailures > 0 ? +0.1f : -0.02f)
Frustration += (RecentFailures > 3 ? +0.1f : -0.02f)
```

## Context Hash Computation

```
Hash = DeterministicHash(TerrainType, ObstacleTag, GoalType)
```

Uses bit operations for deterministic hashing.

## File Locations

| Component | Path |
|-----------|------|
| Components | `Runtime/AI/Cognitive/Components/` |
| Systems | `Runtime/AI/Cognitive/Systems/` |
| Authoring | `Runtime/AI/Cognitive/Authoring/` |
| Documentation | `Docs/Guides/CognitiveArchitecture*.md` |

## See Also

- `CognitiveArchitectureIntegrationGuide.md` - Detailed integration guide
- `CognitiveArchitectureAPI.md` - Complete API reference

