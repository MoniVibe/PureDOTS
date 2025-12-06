# Cognitive Architecture Integration Guide

## Overview

The Cognitive Architecture implements a 3-layer embodied cognition system for deterministic ECS agents:
- **Reflex Layer** (60Hz): Instant sensor→action reactive mapping
- **Cognitive Layer** (1Hz): Procedural learning, affordance detection, causal reasoning
- **Limbic Layer** (0.2Hz): Emotion-driven motivation and behavior modulation

All systems are Burst-compiled, rewind-safe, and deterministic.

## Architecture

```
ReflexSystemGroup (60Hz)
  └── ReflexSystem - Pure reactive sensor→action mapping

LearningSystemGroup (1Hz)
  ├── ProceduralLearningSystem - Perceive/query/select/act/reinforce/store cycle
  ├── AffordanceDetectionSystem - Scan and rank nearby objects by utility
  ├── CausalChainSystem - Maintain lightweight causal graphs
  ├── ContextHashingSystem - Compute situation fingerprints
  ├── DeterministicExplorationSystem - Tick-based exploration variation
  └── SkillAcquisitionSystem - Compress successful action chains into macro-actions

MotivationSystemGroup (0.2Hz)
  ├── LimbicModulationSystem - Update emotions from reward feedback
  └── EmotionDrivenLearningSystem - Apply emotion weights to behavior
```

## Quick Start

### 1. Add Cognitive Components to Agents

Use authoring components or add directly in systems:

```csharp
// Via authoring (recommended)
// Add ProceduralMemoryAuthoring component to prefab
// Configure learningRate (0.01-1.0), maxActionsPerContext (1-64)

// Or programmatically
EntityManager.AddComponent<ProceduralMemory>(entity, new ProceduralMemory
{
    LearningRate = 0.1f,
    ContextHash = 0,
    LastUpdateTick = 0
});

EntityManager.AddComponent<LimbicState>(entity, new LimbicState
{
    Curiosity = 0.5f,
    Fear = 0f,
    Frustration = 0f,
    RecentSuccessRate = 0.5f,
    RecentFailures = 0
});

EntityManager.AddComponent<ContextHash>(entity);
EntityManager.AddBuffer<CausalLink>(entity);
EntityManager.AddBuffer<DetectedAffordance>(entity);
```

### 2. Tag Objects with Affordances

```csharp
// Via authoring
// Add AffordanceAuthoring component to object prefab
// Set affordanceType (Climbable, Movable, etc.), effort, rewardPotential

// Or programmatically
EntityManager.AddComponent<Affordance>(boxEntity, new Affordance
{
    Type = AffordanceType.Climbable,
    Effort = 0.3f,
    RewardPotential = 0.8f,
    ObjectEntity = boxEntity
});
```

### 3. Systems Automatically Process Agents

All systems run automatically once components are present. No manual system registration needed.

## Component Reference

### ProceduralMemory

Stores state-action-outcome table per agent:

```csharp
public struct ProceduralMemory : IComponentData
{
    public FixedList64Bytes<ActionId> TriedActions;  // Actions tried in current context
    public FixedList64Bytes<float> SuccessScores;     // Success scores (0-1) per action
    public byte ContextHash;                          // Situation fingerprint
    public float LearningRate;                        // Reinforcement learning rate
    public uint LastUpdateTick;                       // Last update timestamp
    public byte SuccessChainCount;                    // Number of successful chains
}
```

**Usage**: Automatically updated by `ProceduralLearningSystem`. Query for action selection.

### LimbicState

Emotion and motivation variables:

```csharp
public struct LimbicState : IComponentData
{
    public float Curiosity;          // 0-1, increases exploration
    public float Fear;               // 0-1, causes avoidance
    public float Frustration;        // 0-1, triggers help-seeking
    public float RecentSuccessRate;  // 0-1, success rate over last N actions
    public int RecentFailures;       // Count of recent failures
    public uint LastEmotionUpdateTick;
}
```

**Usage**: Updated by `LimbicModulationSystem`. Read by `EmotionDrivenLearningSystem` to modulate behavior.

### ContextHash

Situation fingerprint for procedural learning:

```csharp
public struct ContextHash : IComponentData
{
    public TerrainType TerrainType;  // Flat, Hilly, Pit, etc.
    public ObstacleTag ObstacleTag;  // Wall, Box, Ladder, etc.
    public GoalType GoalType;        // Escape, Reach, Gather, etc.
    public byte Hash;                // Computed hash for fast matching
    public uint LastComputedTick;
}
```

**Usage**: Computed by `ContextHashingSystem`. Used for context-based action lookup.

### CausalLink (Buffer)

Lightweight causal graph edges:

```csharp
public struct CausalLink : IBufferElementData
{
    public ushort Cause;             // Action/event ID
    public ushort Effect;            // Outcome ID
    public float Weight;              // 0-1, strength of causal relationship
    public uint LastReinforcedTick;
    public ushort ObservationCount;
}
```

**Usage**: Maintained by `CausalChainSystem`. Query for predictive simulation.

## System Integration Patterns

### Reporting Action Outcomes

When actions complete, reinforce procedural memory:

```csharp
[BurstCompile]
public partial struct MyActionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // After action execution...
        var job = new ReinforceOutcomeJob { /* ... */ };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    private partial struct ReinforceOutcomeJob : IJobEntity
    {
        public void Execute(
            Entity entity,
            ref ProceduralMemory memory,
            in ContextHash contextHash,
            in CognitiveStats cognitiveStats,
            in ActionOutcome outcome) // Your custom outcome component
        {
            // Map your action to ActionId
            ActionId actionId = MapToActionId(outcome.ActionType);
            
            // Calculate success result (0.0 = failure, 1.0 = success)
            float successResult = outcome.Success ? 1.0f : 0.0f;
            
            // Reinforce memory
            ProceduralMemoryReinforcementSystem.ReinforceAction(
                ref memory,
                contextHash.Hash,
                actionId,
                successResult,
                memory.LearningRate,
                in cognitiveStats);
            
            // Also reinforce causal links
            if (HasBuffer<CausalLink>(entity))
            {
                var causalLinks = GetBuffer<CausalLink>(entity);
                CausalChainSystem.ReinforceCausalLink(
                    ref causalLinks,
                    (ushort)actionId,
                    (ushort)outcome.OutcomeId,
                    successResult,
                    CurrentTick,
                    0.1f); // reinforcementRate
            }
        }
    }
}
```

### Querying Learned Actions

Use procedural memory to select actions:

```csharp
[BurstCompile]
private partial struct ActionSelectionJob : IJobEntity
{
    public void Execute(
        Entity entity,
        in ProceduralMemory memory,
        in ContextHash contextHash,
        ref DynamicBuffer<AgentIntentBuffer> intents)
    {
        // Find best action for current context
        ActionId bestAction = ActionId.None;
        float bestScore = 0f;
        
        for (int i = 0; i < memory.TriedActions.Length; i++)
        {
            if (memory.ContextHash == contextHash.Hash)
            {
                float score = memory.SuccessScores[i];
                if (score > bestScore)
                {
                    bestScore = score;
                    bestAction = memory.TriedActions[i];
                }
            }
        }
        
        // Use best action or explore
        if (bestAction != ActionId.None && bestScore > 0.3f)
        {
            // Exploit: use learned action
            intents.Add(CreateIntent(bestAction));
        }
        else
        {
            // Explore: use deterministic exploration
            float explorationFactor = DeterministicExplorationSystem
                .ComputeExplorationFactor(CurrentTick, entity);
            ActionId exploreAction = DeterministicExplorationSystem
                .SelectExplorationAction(explorationFactor);
            intents.Add(CreateIntent(exploreAction));
        }
    }
}
```

### Using Affordances

Query detected affordances for interaction targets:

```csharp
[BurstCompile]
private partial struct InteractionJob : IJobEntity
{
    public void Execute(
        Entity entity,
        in DynamicBuffer<DetectedAffordance> affordances,
        ref DynamicBuffer<AgentIntentBuffer> intents)
    {
        if (affordances.Length == 0) return;
        
        // Get highest utility affordance
        var bestAffordance = affordances[0]; // Already sorted by utility
        
        // Create interaction intent
        intents.Add(new AgentIntentBuffer
        {
            Kind = IntentKind.Interact,
            TargetEntity = bestAffordance.ObjectEntity,
            Priority = (byte)(bestAffordance.UtilityScore * 255f),
            TickNumber = CurrentTick
        });
    }
}
```

### Emotion-Driven Behavior

Modulate behavior based on limbic state:

```csharp
[BurstCompile]
private partial struct BehaviorModulationJob : IJobEntity
{
    public void Execute(
        Entity entity,
        in LimbicState limbic,
        ref ProceduralMemory memory)
    {
        // Check if should avoid context due to fear
        float contextFailureRate = CalculateContextFailureRate(memory);
        bool shouldAvoid = EmotionDrivenLearningSystem.ShouldAvoidContext(
            limbic.Fear,
            contextFailureRate);
        
        if (shouldAvoid)
        {
            // Switch to different context/action
            return;
        }
        
        // Check if should trigger help-seeking due to frustration
        if (EmotionDrivenLearningSystem.ShouldTriggerHelpSeeking(limbic.Frustration))
        {
            // Emit help-seeking behavior
        }
        
        // Adjust exploration based on curiosity
        float explorationProb = EmotionDrivenLearningSystem
            .ComputeExplorationProbability(limbic.Curiosity);
        // Use explorationProb to bias action selection
    }
}
```

## Determinism Guarantees

All systems use deterministic algorithms:
- **Exploration**: `(Tick % explorationPeriod)` instead of random
- **Context Hashing**: Deterministic hash from terrain + obstacle + goal
- **Memory Updates**: Fixed-point math, no floating-point drift
- **Rewind-Safe**: All systems check `RewindState.Mode`

## Performance Considerations

- **Target**: < 1ms per 100k agents
- **Tiered Updates**: Reflex 60Hz, Cognitive 1Hz, Limbic 0.2Hz
- **Spatial Caching**: Affordances cached per cell, not per agent
- **Memory Pruning**: Low-weight causal links removed periodically
- **Batch Processing**: Systems use parallel jobs for scalability

## Extension Points

### Custom Action Types

Extend `ActionId` enum:

```csharp
public enum ActionId : byte
{
    // ... existing actions ...
    CustomAction1 = 100,
    CustomAction2 = 101
}
```

### Custom Affordance Types

Extend `AffordanceType` enum:

```csharp
public enum AffordanceType : byte
{
    // ... existing types ...
    CustomAffordance = 100
}
```

### Custom Outcome Types

Extend `OutcomeId` enum:

```csharp
public enum OutcomeId : ushort
{
    // ... existing outcomes ...
    CustomOutcome = 1000
}
```

## System Ordering

Systems run in this order within their groups:

```
ReflexSystemGroup (60Hz)
  └── ReflexSystem

LearningSystemGroup (1Hz)
  ├── ContextHashingSystem (computes context first)
  ├── AffordanceDetectionSystem (scans objects)
  ├── ProceduralLearningSystem (uses context + affordances)
  ├── CausalChainSystem (reinforces after actions)
  ├── SkillAcquisitionSystem (compresses memories)
  └── DeterministicExplorationSystem (provides exploration helpers)

MotivationSystemGroup (0.2Hz)
  ├── LimbicModulationSystem (updates emotions)
  └── EmotionDrivenLearningSystem (applies emotion weights)
```

## Troubleshooting

### Agents Not Learning

1. Check `ProceduralMemory` component exists
2. Verify `ContextHash` is being computed (check `LastComputedTick`)
3. Ensure action outcomes are being reported via `ReinforceAction`
4. Check `LearningRate` is not too low (< 0.01)

### No Affordances Detected

1. Verify objects have `Affordance` component
2. Check objects have `SpatialIndexedTag` for spatial queries
3. Ensure `AffordanceDetectionSystem` is running (check system enabled)
4. Verify detection range (`MaxDetectionRange = 10.0f`)

### Emotions Not Updating

1. Check `LimbicState` component exists
2. Verify `RecentFailures` and `RecentSuccessRate` are being tracked
3. Ensure `LimbicModulationSystem` is running (0.2Hz, check update interval)

## See Also

- `CognitiveArchitectureAPI.md` - Complete API reference
- `CognitiveArchitectureQuickReference.md` - Quick lookup table
- System source code in `Runtime/AI/Cognitive/Systems/`

