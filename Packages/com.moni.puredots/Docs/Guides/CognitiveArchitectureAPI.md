# Cognitive Architecture API Reference

Complete API reference for the Cognitive Architecture systems and components.

## Components

### ProceduralMemory

**Namespace**: `PureDOTS.Runtime.AI.Cognitive`

**Type**: `IComponentData`

**Fields**:
- `FixedList64Bytes<ActionId> TriedActions` - Actions tried in current context
- `FixedList64Bytes<float> SuccessScores` - Success scores (0-1) per action, same index as TriedActions
- `byte ContextHash` - Situation fingerprint hash
- `float LearningRate` - Reinforcement learning rate (0.01-1.0)
- `uint LastUpdateTick` - Last tick when memory was updated
- `byte SuccessChainCount` - Number of successful action chains stored

**Usage**: Attach to agents that should learn procedurally. Automatically updated by `ProceduralLearningSystem`.

### LimbicState

**Namespace**: `PureDOTS.Runtime.AI.Cognitive`

**Type**: `IComponentData`

**Fields**:
- `float Curiosity` - 0-1, increases exploration probability
- `float Fear` - 0-1, causes avoidance of high-failure contexts
- `float Frustration` - 0-1, triggers help-seeking/aggression
- `float RecentSuccessRate` - 0-1, success rate over last N actions
- `int RecentFailures` - Count of recent failures
- `uint LastEmotionUpdateTick` - Last tick when emotions were updated
- `float StabilityThreshold` - Threshold for considering success rate "stable"
- `byte RecentActionWindow` - Number of actions for success rate calculation

**Usage**: Attach to agents for emotion-driven behavior. Updated by `LimbicModulationSystem`.

### ContextHash

**Namespace**: `PureDOTS.Runtime.AI.Cognitive`

**Type**: `IComponentData`

**Fields**:
- `TerrainType TerrainType` - Terrain type enum (Flat, Hilly, Pit, etc.)
- `ObstacleTag ObstacleTag` - Primary obstacle tag (Wall, Box, Ladder, etc.)
- `GoalType GoalType` - Current goal type (Escape, Reach, Gather, etc.)
- `byte Hash` - Computed deterministic hash for fast matching
- `uint LastComputedTick` - Last tick when context was computed

**Usage**: Attach to agents. Computed by `ContextHashingSystem`. Used for context-based action lookup.

### Affordance

**Namespace**: `PureDOTS.Runtime.AI.Cognitive`

**Type**: `IComponentData`

**Fields**:
- `AffordanceType Type` - Type of affordance (Climbable, Movable, etc.)
- `float Effort` - 0.01-1.0, effort required (higher = more difficult)
- `float RewardPotential` - 0-1, potential reward (higher = more valuable)
- `Entity ObjectEntity` - Entity that owns this affordance

**Usage**: Attach to objects that agents can interact with. Detected by `AffordanceDetectionSystem`.

### DetectedAffordance (Buffer)

**Namespace**: `PureDOTS.Runtime.AI.Cognitive`

**Type**: `IBufferElementData`

**Fields**:
- `Entity ObjectEntity` - Entity with the affordance
- `AffordanceType Type` - Affordance type
- `float UtilityScore` - Computed utility (RewardPotential / Effort)
- `float DistanceSq` - Distance squared to object

**Usage**: Buffer on agents, populated by `AffordanceDetectionSystem`. Sorted by utility score.

### CausalLink (Buffer)

**Namespace**: `PureDOTS.Runtime.AI.Cognitive`

**Type**: `IBufferElementData`

**Fields**:
- `ushort Cause` - Action/event ID (maps to ActionId or custom event)
- `ushort Effect` - Outcome ID (maps to OutcomeId enum)
- `float Weight` - 0-1, strength of causal relationship
- `uint LastReinforcedTick` - Last tick when link was reinforced
- `ushort ObservationCount` - Number of times this link was observed

**Usage**: Buffer on agents, maintained by `CausalChainSystem`. Used for predictive simulation.

## Enums

### ActionId

**Values**:
- `None = 0`
- `Move = 1`
- `Climb = 2`
- `Push = 3`
- `Pull = 4`
- `Jump = 5`
- `Throw = 6`
- `Use = 7`
- `Grab = 8`
- `Drop = 9`
- `EscapePit = 10` (macro-action)
- `Custom0 = 240`

### AffordanceType

**Values**:
- `None = 0`
- `Climbable = 1`
- `Movable = 2`
- `Throwable = 3`
- `Usable = 4`
- `Grabable = 5`
- `Pushable = 6`
- `Pullable = 7`

### OutcomeId

**Values**:
- `None = 0`
- `HeightIncreased = 1`
- `HeightDecreased = 2`
- `EscapedPit = 3`
- `ReachedGoal = 4`
- `Failed = 5`
- `ResourceGained = 6`
- `ResourceLost = 7`
- `DamageTaken = 8`
- `DamageDealt = 9`
- `Custom0 = 1000`

## Static Helper Functions

### ProceduralMemoryReinforcementSystem.ReinforceAction

**Signature**:
```csharp
public static void ReinforceAction(
    ref ProceduralMemory memory,
    byte contextHash,
    ActionId actionId,
    float successResult, // 0.0 = failure, 1.0 = success
    float baseLearningRate,
    in CognitiveStats cognitiveStats)
```

**Description**: Reinforce a specific action in procedural memory. Applies Intelligence/Wisdom/Curiosity multipliers to learning rate.

**Formula**: `EffectiveLearningRate = BaseRate * (0.6 * Intelligence + 0.4 * Wisdom) * (1 + Curiosity * 0.5)`

**Parameters**:
- `memory` - Procedural memory to update
- `contextHash` - Context hash for this action
- `actionId` - Action that was performed
- `successResult` - Outcome (0.0 = failure, 1.0 = success)
- `baseLearningRate` - Base learning rate from memory
- `cognitiveStats` - Cognitive stats for multipliers

**Usage**: Call when action outcomes are known.

### CausalChainSystem.ReinforceCausalLink

**Signature**:
```csharp
public static void ReinforceCausalLink(
    ref DynamicBuffer<CausalLink> causalLinks,
    ushort cause,
    ushort effect,
    float successResult, // 0.0 = failure, 1.0 = success
    uint currentTick,
    float reinforcementRate = 0.1f)
```

**Description**: Reinforce a causal link based on action outcome. Updates link weight or creates new link.

**Parameters**:
- `causalLinks` - Buffer of causal links
- `cause` - Cause action/event ID
- `effect` - Effect outcome ID
- `successResult` - Outcome (0.0 = failure, 1.0 = success)
- `currentTick` - Current simulation tick
- `reinforcementRate` - Rate of weight update (default 0.1)

**Usage**: Call when causal relationships are observed.

### CausalChainSystem.QueryCausalLink

**Signature**:
```csharp
public static float QueryCausalLink(
    in DynamicBuffer<CausalLink> causalLinks,
    ushort cause,
    ushort effect)
```

**Description**: Query causal graph for expected outcome of an action. Returns weight (0-1) if link exists, 0.0 otherwise.

**Parameters**:
- `causalLinks` - Buffer of causal links
- `cause` - Cause action/event ID
- `effect` - Desired effect outcome ID

**Returns**: Link weight (0-1) or 0.0 if not found

**Usage**: Use for predictive simulation.

### DeterministicExplorationSystem.ComputeExplorationFactor

**Signature**:
```csharp
public static float ComputeExplorationFactor(
    uint tick,
    Entity entity,
    uint period = 100)
```

**Description**: Compute deterministic exploration factor for an agent. Uses tick-based variation to guarantee identical sequences across replays.

**Parameters**:
- `tick` - Current simulation tick
- `entity` - Agent entity (for entity-specific variation)
- `period` - Exploration period (default 100 ticks)

**Returns**: Exploration factor (0-1)

**Usage**: Use for deterministic exploration decisions.

### DeterministicExplorationSystem.SelectExplorationAction

**Signature**:
```csharp
public static ActionId SelectExplorationAction(
    float explorationFactor,
    int actionCount = 8)
```

**Description**: Select exploration action based on deterministic factor.

**Parameters**:
- `explorationFactor` - Exploration factor (0-1)
- `actionCount` - Number of actions to choose from (default 8)

**Returns**: Selected ActionId

**Usage**: Use with `ComputeExplorationFactor` for deterministic exploration.

### EmotionDrivenLearningSystem.ComputeExplorationProbability

**Signature**:
```csharp
public static float ComputeExplorationProbability(
    float curiosity,
    float baseProbability = 0.1f)
```

**Description**: Compute exploration probability based on curiosity level. Higher curiosity increases exploration.

**Parameters**:
- `curiosity` - Curiosity level (0-1)
- `baseProbability` - Base exploration probability (default 0.1)

**Returns**: Exploration probability (0-1)

**Usage**: Use to bias action selection based on curiosity.

### EmotionDrivenLearningSystem.ShouldAvoidContext

**Signature**:
```csharp
public static bool ShouldAvoidContext(
    float fear,
    float contextFailureRate,
    float threshold = 0.5f)
```

**Description**: Check if context should be avoided based on fear level. Higher fear causes avoidance of high-failure contexts.

**Parameters**:
- `fear` - Fear level (0-1)
- `contextFailureRate` - Failure rate for this context (0-1)
- `threshold` - Base avoidance threshold (default 0.5)

**Returns**: True if context should be avoided

**Usage**: Use to filter contexts based on fear.

### EmotionDrivenLearningSystem.ShouldTriggerHelpSeeking

**Signature**:
```csharp
public static bool ShouldTriggerHelpSeeking(
    float frustration,
    float threshold = 0.7f)
```

**Description**: Check if help-seeking should be triggered based on frustration.

**Parameters**:
- `frustration` - Frustration level (0-1)
- `threshold` - Trigger threshold (default 0.7)

**Returns**: True if help-seeking should be triggered

**Usage**: Use to trigger help-seeking behaviors.

### ContextHashHelper.ComputeHash

**Signature**:
```csharp
public static byte ComputeHash(
    TerrainType terrain,
    ObstacleTag obstacle,
    GoalType goal)
```

**Description**: Compute deterministic hash from terrain, obstacle, and goal.

**Parameters**:
- `terrain` - Terrain type
- `obstacle` - Obstacle tag
- `goal` - Goal type

**Returns**: Computed hash byte

**Usage**: Use for context matching.

### ContextHashHelper.HammingDistance

**Signature**:
```csharp
public static int HammingDistance(byte hash1, byte hash2)
```

**Description**: Compute Hamming distance between two context hashes (number of differing bits).

**Parameters**:
- `hash1` - First hash
- `hash2` - Second hash

**Returns**: Hamming distance (0-8)

**Usage**: Use for context generalization.

### ContextHashHelper.AreSimilar

**Signature**:
```csharp
public static bool AreSimilar(byte hash1, byte hash2, int threshold = 2)
```

**Description**: Check if two contexts are similar (Hamming distance <= threshold).

**Parameters**:
- `hash1` - First hash
- `hash2` - Second hash
- `threshold` - Maximum Hamming distance for similarity (default 2)

**Returns**: True if contexts are similar

**Usage**: Use for context generalization and shared learning.

### PredictiveSimulationSystem.SimulateActionEffect

**Signature**:
```csharp
public static float SimulateActionEffect(
    in DynamicBuffer<CausalLink> causalLinks,
    ActionId action,
    OutcomeId desiredOutcome)
```

**Description**: Simulate action effect using causal graph. Returns expected utility based on causal link weights.

**Parameters**:
- `causalLinks` - Buffer of causal links
- `action` - Action to simulate
- `desiredOutcome` - Desired outcome

**Returns**: Expected utility (0-1)

**Usage**: Use for mental rehearsal and planning.

### PredictiveSimulationSystem.FindBestPlan

**Signature**:
```csharp
public static ActionId FindBestPlan(
    in DynamicBuffer<CausalLink> causalLinks,
    in FixedList64Bytes<ActionId> candidateActions,
    OutcomeId desiredOutcome)
```

**Description**: Find best action plan by simulating multiple actions. Returns action with highest expected utility.

**Parameters**:
- `causalLinks` - Buffer of causal links
- `candidateActions` - List of candidate actions
- `desiredOutcome` - Desired outcome

**Returns**: Best ActionId or None if no candidates

**Usage**: Use for planning with multiple action options.

### ObservationalLearningSystem.RecordObservedAction

**Signature**:
```csharp
public static void RecordObservedAction(
    ref ProceduralMemory memory,
    byte contextHash,
    ActionId observedAction,
    float observedOutcome, // 0.0 = failure, 1.0 = success
    float learningRate,
    float wisdomMultiplier = 1.0f)
```

**Description**: Record an observed action with reduced reinforcement (0.25×). Success accelerated by Wisdom.

**Parameters**:
- `memory` - Procedural memory to update
- `contextHash` - Context hash for observed action
- `observedAction` - Action that was observed
- `observedOutcome` - Observed outcome (0.0 = failure, 1.0 = success)
- `learningRate` - Learning rate
- `wisdomMultiplier` - Wisdom multiplier for accelerated learning (default 1.0)

**Usage**: Call when agent witnesses another agent's successful action.

## System Update Frequencies

- **ReflexSystemGroup**: 60Hz (every tick)
- **LearningSystemGroup**: 1Hz (every second)
- **MotivationSystemGroup**: 0.2Hz (every 5 seconds)

## Message Types (AgentSyncBus)

### ContextPerceptionMessage

Sent from Body ECS to Mind ECS:

```csharp
public struct ContextPerceptionMessage
{
    public AgentGuid AgentGuid;
    public byte ContextHash;
    public byte TerrainType;
    public byte ObstacleTag;
    public byte GoalType;
    public uint TickNumber;
}
```

### ActionOutcomeMessage

Sent from Body ECS to Mind ECS:

```csharp
public struct ActionOutcomeMessage
{
    public AgentGuid AgentGuid;
    public byte ActionId;
    public byte ContextHash;
    public float SuccessResult; // 0.0 = failure, 1.0 = success
    public uint TickNumber;
}
```

### ProceduralMemoryUpdateMessage

Sent from Mind ECS to Body ECS:

```csharp
public struct ProceduralMemoryUpdateMessage
{
    public AgentGuid AgentGuid;
    public byte ContextHash;
    public byte ActionId;
    public float SuccessScore;
    public uint TickNumber;
}
```

## Authoring Components

### ProceduralMemoryAuthoring

**Fields**:
- `learningRate` (0.01-1.0) - Learning rate for reinforcement
- `maxActionsPerContext` (1-64) - Maximum actions per context

**Bakes**: `ProceduralMemory`, `LimbicState`, `ContextHash`

### AffordanceAuthoring

**Fields**:
- `affordanceType` - Type of affordance
- `effort` (0.01-1.0) - Effort required
- `rewardPotential` (0-1) - Potential reward

**Bakes**: `Affordance`, `SpatialIndexedTag`

## See Also

- `CognitiveArchitectureIntegrationGuide.md` - Integration guide with examples
- `CognitiveArchitectureQuickReference.md` - Quick lookup table
- System source code in `Runtime/AI/Cognitive/Systems/`

