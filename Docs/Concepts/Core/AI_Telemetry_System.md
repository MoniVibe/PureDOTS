# AI Telemetry System for Headless Training

## Overview

Comprehensive telemetry specification for headless AI training builds running in parallel with presentation development. Exposes all game state, decision quality metrics, and emergent behaviors to enable efficient RL/ML agent training without graphical overhead.

**Key Principles**:
- **Complete observability**: AI sees everything players see (and more)
- **Action attribution**: Track which AI decisions led to which outcomes
- **Emergent detection**: Identify unexpected behaviors and strategies
- **Training efficiency**: Export at high frequency (10-60 Hz) for RL feedback loops
- **Deterministic replay**: All telemetry tied to tick for perfect reproduction
- **Cross-game consistency**: Same telemetry structure for Godgame and Space4X

---

## Current Telemetry (Existing)

Your existing telemetry infrastructure provides:
- `TelemetryMetric` - Scalar metrics with units
- `TelemetryEvent` - State change events with payloads
- `TelemetryHistory` - Historical data points
- `TelemetryTrend` - Calculated trends (avg, min, max, slope)
- `TelemetryAnomaly` - Outlier detection
- `BalanceMetric` - Design target deviation tracking
- `PlayerAction` - Action logging with success flags
- `PlayerSession` - Session summaries

---

## Implemented Now (Minimal, Shippable)

**Headless export enablement**:
- Auto-enables NDJSON export when `Application.isBatchMode == true` or `PUREDOTS_TELEMETRY_ENABLE=1`.
- Environment variables:
  - `PUREDOTS_TELEMETRY_PATH` (optional): output file path.
  - `PUREDOTS_TELEMETRY_RUN_ID` (optional): run identifier.
  - `PUREDOTS_TELEMETRY_FLAGS` (optional): export flags (numeric or comma-separated names).
- Default path (when unset): `{Application.persistentDataPath}/telemetry/telemetry_{runId}.ndjson`.

**Minimal AI training output**:
- Metrics (`TelemetryMetric`, cadence: every 30 ticks):
  - `entities.total` (all entities)
  - `units.mobile` (VillagerId + ShipAggregate counts)
  - `buildings.total` (StructureDurability count)
- Events (`TelemetryEvent`, cadence: BehaviorTelemetry aggregation):
  - `eventType`: `ai_action`
  - `source`: `ai`
  - Payload schema (FixedString128Bytes, compact JSON):
    - `{"b":<behaviorId>,"k":<metricOrInvariantId>,"v":<value>,"t":<0|1>}`
    - `t=0` metric (value = ValueA), `t=1` invariant (value = Passed)

**Enable examples**:
```
PUREDOTS_TELEMETRY_ENABLE=1
PUREDOTS_TELEMETRY_PATH=C:\telemetry\run.ndjson
PUREDOTS_TELEMETRY_RUN_ID=smoke_001
PUREDOTS_TELEMETRY_FLAGS=IncludeTelemetryMetrics,IncludeTelemetryEvents
```

---

## Proposed AI Training Extensions

### 1. World State Snapshots

AI needs complete world state at decision boundaries:

```csharp
/// <summary>
/// Complete world state snapshot for AI observation.
/// Captured at configurable intervals (default: every tick for training, every 10 ticks for validation).
/// </summary>
public struct WorldStateSnapshot : IComponentData
{
    public uint Tick;
    public uint SnapshotId;

    // Population
    public uint TotalEntities;
    public uint TotalVillagers;          // Godgame
    public uint TotalShips;              // Space4X
    public uint TotalMobileUnits;
    public uint TotalBuildings;
    public uint TotalStrongholdsControlled;

    // Resources (aggregate)
    public float TotalFood;
    public float TotalGold;
    public float TotalWood;
    public float TotalStone;
    public float TotalEnergy;            // Space4X
    public float TotalMana;              // Godgame

    // Combat state
    public uint ActiveCombatEncounters;
    public uint HostileEntitiesInRange;
    public float AverageMoraleAllUnits;
    public float CombatPowerRatio;       // Player vs all enemies

    // Spatial coverage
    public float TerritoryControlledSqKm;
    public uint ExploredTileCount;
    public uint FogOfWarTileCount;

    // Diplomatic state
    public int AverageRelationAllFactions;
    public uint AlliedFactionCount;
    public uint HostileFactionCount;
    public uint NeutralFactionCount;

    // Economic health
    public float ResourceIncomeRate;     // Per second
    public float ResourceSpendRate;
    public float NetIncomeRate;
    public float SupplyRatio;            // Demand / supply

    // Progress toward victory
    public float VictoryProgress;        // 0.0 to 1.0
    public FixedString32Bytes LeadingVictoryCondition;
    public float SecondPlaceProgress;    // For competitive scenarios
}

/// <summary>
/// Per-entity observations for fine-grained AI perception.
/// Exposed as buffer for batch processing.
/// </summary>
[InternalBufferCapacity(0)]
public struct EntityObservation : IBufferElementData
{
    public Entity ObservedEntity;
    public FixedString32Bytes EntityType;  // "villager", "warrior", "ship", "building"

    // Position & movement
    public float3 Position;
    public float3 Velocity;
    public LocomotionMode ActiveLocomotionMode;

    // Health & resources
    public float Health;
    public float MaxHealth;
    public float Stamina;
    public float Energy;
    public float Focus;                    // For memory tapping

    // Combat state
    public float AttackPower;
    public float DefensePower;
    public MoraleChange MoraleState;
    public bool IsInCombat;
    public Entity CurrentTarget;

    // Social state
    public int RelationToPlayer;           // -1000 to +1000
    public float CharismaScore;
    public float InsightScore;
    public uint ActiveDialogueCount;

    // Activity
    public FixedString32Bytes CurrentBehavior; // "idle", "gathering", "attacking", etc.
    public Entity AssignedTask;
    public float TaskProgress;             // 0.0 to 1.0

    // Visibility flags
    public bool IsVisible;                 // In player's fog of war
    public bool IsPlayerControlled;
    public bool IsHostile;
}

/// <summary>
/// Social graph snapshot - critical for AI learning social dynamics.
/// </summary>
[InternalBufferCapacity(0)]
public struct SocialGraphEdge : IBufferElementData
{
    public Entity EntityA;
    public Entity EntityB;
    public int RelationScore;              // -1000 to +1000
    public float TrustScore;               // 0.0 to 1.0
    public RelationshipContext Context;    // Friends, enemies, family, etc.

    // Reputation
    public int ReputationTrading;
    public int ReputationCombat;
    public int ReputationDiplomacy;

    // Communication state
    public bool ShareCommonLanguage;
    public float CommunicationClarity;     // 0.0 to 1.0

    // Recent interactions
    public uint LastInteractionTick;
    public SocialTopic LastInteractionTopic;
    public bool LastInteractionWasDeceptive;
}
```

### 2. Action Outcome Attribution

Track AI decision quality:

```csharp
/// <summary>
/// AI action taken with outcome tracking.
/// Enables reward calculation for RL training.
/// </summary>
[InternalBufferCapacity(64)]
public struct AIActionRecord : IBufferElementData
{
    public uint DecisionTick;              // When action was decided
    public uint ExecutionTick;             // When action executed
    public uint OutcomeTick;               // When outcome determined

    public FixedString32Bytes ActionType;  // "build", "attack", "rally", "trade"
    public FixedString64Bytes ActionParams; // JSON payload with parameters

    public Entity ActorEntity;
    public Entity TargetEntity;
    public float3 TargetPosition;

    // Outcome
    public bool WasSuccessful;
    public float RewardValue;              // Calculated reward (-1.0 to 1.0)
    public FixedString64Bytes FailureReason;

    // State changes caused
    public float ResourceDelta;            // Net resource change
    public float HealthDelta;              // Net health change
    public int RelationDelta;              // Net relation change
    public float VictoryProgressDelta;     // Progress toward win

    // Efficiency
    public float TimeToComplete;           // Seconds
    public float ResourcesSpent;
    public float ExpectedCost;             // What AI predicted
    public float CostAccuracy;             // Actual / Expected
}

/// <summary>
/// Combat engagement tracking for battle AI training.
/// </summary>
public struct CombatEngagementRecord : IComponentData
{
    public uint EngagementId;
    public uint StartTick;
    public uint EndTick;

    // Participants
    public uint PlayerUnitsStart;
    public uint EnemyUnitsStart;
    public uint PlayerUnitsEnd;
    public uint EnemyUnitsEnd;

    // Positioning decisions
    public uint FlanksExecuted;
    public uint SurroundsCompleted;
    public float AverageArcCoverage;       // How well arcs were utilized

    // Rally/memory tapping usage
    public uint RalliesAttempted;
    public uint RalliesSuccessful;
    public uint MemoryTapsUsed;
    public float AverageBonusMagnitude;

    // Outcome
    public bool Victory;
    public float KillDeathRatio;
    public float DamageTakenToDealt;
    public float ResourceEfficiency;       // Value killed / value lost

    // Tactical quality
    public float PositioningScore;         // 0.0 to 1.0
    public float TimingScore;              // Attack timing quality
    public float CoordinationScore;        // How well units coordinated
}

/// <summary>
/// Dialogue/social action tracking for social AI training.
/// </summary>
[InternalBufferCapacity(32)]
public struct SocialActionRecord : IBufferElementData
{
    public uint Tick;
    public Entity Speaker;
    public Entity Listener;

    public SocialTopic Topic;
    public DialogueIntent Intent;          // Genuine, deceptive, sarcastic

    // Deception tracking
    public bool WasDeceptive;
    public bool WasDetected;
    public float CharismaUsed;
    public float InsightOpposed;

    // Outcome
    public ResponseType Response;
    public int RelationChangeDelta;
    public int ReputationChangeDelta;
    public bool AchievedGoal;              // Got desired response

    // Memory tapping
    public bool InvokedSharedMemory;
    public SharedMemoryType MemoryType;
    public uint ParticipantsAffected;
}

/// <summary>
/// Economic decision tracking for economy AI.
/// </summary>
public struct EconomicDecisionRecord : IComponentData
{
    public uint Tick;
    public FixedString32Bytes DecisionType; // "build", "trade", "allocate", "research"

    // Resources committed
    public float GoldSpent;
    public float WoodSpent;
    public float StoneSpent;
    public float TimeInvested;

    // Expected outcome
    public float ExpectedROI;              // Return on investment
    public uint ExpectedPaybackTicks;

    // Actual outcome
    public float ActualROI;
    public uint ActualPaybackTicks;
    public float PredictionError;

    // Strategic alignment
    public float VictoryContribution;      // How much this helped win
    public bool WasCriticalPath;           // On path to victory
}
```

### 3. Emergent Behavior Detection

Identify novel AI strategies:

```csharp
/// <summary>
/// Detects and logs unusual AI behavior patterns for analysis.
/// </summary>
[InternalBufferCapacity(16)]
public struct EmergentBehaviorEvent : IBufferElementData
{
    public uint DetectedTick;
    public FixedString64Bytes BehaviorSignature; // Hash of pattern
    public FixedString128Bytes Description;      // Human-readable

    public EmergentBehaviorType Type;
    public float NoveltyScore;             // 0.0 to 1.0 (how unusual)
    public float EffectivenessScore;       // 0.0 to 1.0 (how well it worked)

    // Context
    public uint EntitiesInvolved;
    public FixedString64Bytes GamePhase;   // "early", "mid", "late"
    public float VictoryProgressWhenDiscovered;
}

public enum EmergentBehaviorType : byte
{
    // Economy
    UnusualResourceAllocation = 0,  // E.g., ignoring food to rush military
    NovelTradeRoute = 1,
    RiskInvestment = 2,             // High-risk, high-reward choices

    // Combat
    UnconventionalFormation = 10,   // New positioning patterns
    TimingExploit = 11,             // Exploiting game timing
    TerrainExploit = 12,            // Novel terrain usage

    // Social
    DeceptionChain = 20,            // Multi-step lies
    AllianceManipulation = 21,      // Complex diplomacy
    ReputationGaming = 22,          // Manipulating gossip

    // Memory tapping
    UnusualMemoryCombination = 30,  // Unique memory invocations
    OptimalRallyTiming = 31,        // Perfect rally execution

    // Meta
    ExploitDiscovery = 40,          // Found game exploit
    CounterStrategy = 41,           // New counter to known strategy
    HybridStrategy = 42             // Combining multiple approaches
}

/// <summary>
/// Strategy clustering for identifying AI playstyles.
/// </summary>
public struct StrategyCluster : IComponentData
{
    public uint ClusterId;
    public FixedString64Bytes StrategyName; // "rush", "turtle", "diplomatic", etc.

    // Characteristic weights
    public float AggressionWeight;
    public float EconomyFocusWeight;
    public float DiplomacyFocusWeight;
    public float TechFocusWeight;
    public float ExpansionWeight;

    // Performance
    public float WinRate;
    public float AverageVictoryTicks;
    public uint TimesPlayed;
    public uint Wins;
    public uint Losses;
}
```

### 4. Training Progress Metrics

Track AI learning efficiency:

```csharp
/// <summary>
/// RL training metrics for monitoring convergence.
/// </summary>
public struct RLTrainingMetrics : IComponentData
{
    public uint Episode;
    public uint TotalSteps;

    // Reward signals
    public float EpisodeReward;
    public float AverageReward100;         // Rolling 100 episodes
    public float BestEpisodeReward;

    // Policy quality
    public float PolicyLoss;
    public float ValueLoss;
    public float EntropyBonus;

    // Exploration
    public float ExplorationRate;
    public float NovelActionsRatio;        // % new actions this episode

    // Performance
    public float WinRate100;               // Last 100 episodes
    public float AverageVictoryTicks;
    public float AverageDecisionLatency;   // ms per decision

    // Stability
    public float PolicyStability;          // How much policy changed
    public float RewardVariance;
    public bool HasConverged;
}

/// <summary>
/// A/B testing comparison for different AI versions.
/// </summary>
public struct AIComparisonTest : IComponentData
{
    public uint TestId;
    public FixedString32Bytes PolicyAId;
    public FixedString32Bytes PolicyBId;

    // Head-to-head results
    public uint MatchesPlayed;
    public uint PolicyAWins;
    public uint PolicyBWins;
    public uint Draws;

    // Performance comparison
    public float PolicyAAvgReward;
    public float PolicyBAvgReward;
    public float PolicyAAvgVictoryTicks;
    public float PolicyBAvgVictoryTicks;

    // Statistical significance
    public float PValue;
    public bool IsSignificant;             // p < 0.05
    public FixedString32Bytes Winner;      // "A", "B", or "inconclusive"
}
```

### 5. System-Specific Telemetry

Based on your documented systems:

```csharp
/// <summary>
/// Force system telemetry - tracks how AI uses environmental forces.
/// </summary>
public struct ForceUsageTelemetry : IComponentData
{
    public uint Tick;

    // Force exploitation
    public uint GravityWellsExploited;     // Slingshots, etc.
    public uint WindZonesUsed;
    public uint VortexTrapsSet;
    public uint TemporalDistortionsTriggered;

    // Efficiency
    public float ForceAssistedMovementRatio; // % movement assisted by forces
    public float EnergyySavedViaForces;

    // Combat usage
    public uint EnemiesPushedIntoHazards;
    public uint AlliesSavedViaForces;
}

/// <summary>
/// Locomotion mode selection quality.
/// </summary>
public struct LocomotionDecisionTelemetry : IComponentData
{
    public uint Tick;
    public Entity Entity;

    public LocomotionMode ChosenMode;
    public LocomotionMode OptimalMode;     // Retrospectively calculated

    // Context
    public float TerrainDifficulty;
    public float StaminaAtDecision;
    public bool WasInCombat;

    // Outcome
    public float EnergyConsumed;
    public float ExpectedEnergy;
    public float TimeToDestination;
    public float ExpectedTime;
    public bool ReachedDestination;
}

/// <summary>
/// Memory tapping effectiveness tracking.
/// </summary>
public struct MemoryTapTelemetry : IComponentData
{
    public uint Tick;
    public Entity Initiator;

    public SharedMemoryType MemoryType;
    public uint ParticipantCount;
    public float AverageMemoryStrength;
    public float BonusMagnitude;
    public float Duration;

    // Outcome
    public bool WonEngagement;             // Did rally help win?
    public float CombatEffectiveness;      // Before vs after
    public float FocusEfficiency;          // Bonus magnitude / focus spent

    // Timing quality
    public bool OptimalTiming;             // Was this good moment?
    public float TimingScore;              // 0.0 to 1.0
}

/// <summary>
/// Bay/Platform combat positioning telemetry.
/// </summary>
public struct BayCombatTelemetry : IComponentData
{
    public uint Tick;
    public Entity CarrierEntity;

    // Bay management
    public uint BaysOpened;
    public uint BaysClosed;
    public uint OptimalBayTransitions;     // Good timing
    public uint SuboptimalBayTransitions;  // Bad timing

    // Arc coverage
    public float ArcCoverageRatio;         // % of firing arcs utilized
    public uint ShotsBlockedByBadPositioning;

    // Coordinated fire
    public uint CoordinatedVolleys;
    public float BonusDamageFromCoordination;
}

/// <summary>
/// Dialogue deception success tracking.
/// </summary>
public struct DeceptionTelemetry : IComponentData
{
    public uint Tick;

    // Aggregate stats
    public uint DeceptionAttempts;
    public uint DeceptionsSuccessful;
    public uint DeceptionsDetected;
    public float SuccessRate;

    // By difficulty
    public uint TrivialLiesDetected;
    public uint SimpleLiesDetected;
    public uint ComplexLiesDetected;
    public uint MasterfulLiesDetected;

    // Consequences
    public int TotalReputationLost;
    public int TotalRelationDamage;
    public uint RelationshipsRuined;

    // Learning
    public float CharismaImprovement;      // Skill increase
    public float OptimalCharismaLevel;     // For current opponents
}
```

### 6. Configuration & Export

```csharp
/// <summary>
/// AI telemetry export configuration for training pipelines.
/// </summary>
public struct AITelemetryExportConfig : IComponentData
{
    // Frequency
    public uint SnapshotIntervalTicks;     // How often to snapshot
    public bool ExportEveryTick;           // For critical training

    // Scope
    public bool ExportWorldState;
    public bool ExportEntityObservations;
    public bool ExportSocialGraph;
    public bool ExportActionRecords;
    public bool ExportEmergentBehaviors;
    public bool ExportTrainingMetrics;

    // Format
    public TelemetryExportFormat Format;   // CSV, JSON, Binary, TFRecord
    public FixedString128Bytes OutputPath;

    // Compression
    public bool CompressSnapshots;         // Delta compression
    public bool OnlyExportChanges;         // Skip unchanged entities

    // Performance
    public bool AsyncExport;               // Don't block simulation
    public uint MaxBufferSizeMB;
}

public enum TelemetryExportFormat : byte
{
    CSV = 0,                // Human-readable
    NDJSON = 1,             // Newline-delimited JSON
    BinaryBurst = 2,        // Burst-serialized binary
    TFRecord = 3,           // TensorFlow training format
    NPZ = 4                 // NumPy compressed
}

/// <summary>
/// Real-time training feedback for online RL.
/// </summary>
public struct TrainingFeedback : IComponentData
{
    public float CurrentReward;
    public bool EpisodeTerminated;
    public FixedString64Bytes TerminationReason;

    // For curriculum learning
    public float DifficultyLevel;
    public bool ShouldIncreaseDifficulty;
    public bool ShouldDecreaseDifficulty;
}
```

---

## Recommended Metrics by System

### Social Dynamics
- `social.deception.success_rate` - % lies that succeed
- `social.reputation.avg_player` - Player's average reputation
- `social.relations.avg_allied` - Average allied relation score
- `social.dialogue.interactions_per_minute` - Social activity rate
- `social.trust.betrayals_detected` - Caught deceptions
- `social.memory_tap.usage_rate` - Rallies per 100 ticks
- `social.memory_tap.avg_participants` - Rally effectiveness

### Combat
- `combat.positioning.arc_coverage` - % firing arcs used
- `combat.positioning.flanks_executed` - Successful flanks
- `combat.kd_ratio` - Kill/death ratio
- `combat.damage_efficiency` - Damage dealt / taken
- `combat.morale.avg_friendly` - Average ally morale
- `combat.coordination.volleys_per_engagement` - Coordination quality
- `combat.bay_management.transition_quality` - Bay timing score

### Economy
- `economy.income_rate` - Resources per second
- `economy.spend_efficiency` - Value gained / spent
- `economy.supply_ratio` - Demand / supply
- `economy.bottleneck_count` - Supply bottlenecks
- `economy.roi.avg` - Return on investment
- `economy.prediction_error` - Cost estimation accuracy

### Locomotion
- `locomotion.mode_optimality` - % optimal mode choices
- `locomotion.energy_efficiency` - Energy per distance
- `locomotion.stamina_usage` - Stamina per distance
- `locomotion.mode_switches_per_minute` - Adaptability
- `locomotion.force_assisted_ratio` - Environmental force usage

### Victory Progress
- `victory.progress_rate` - Victory progress per minute
- `victory.critical_path_adherence` - % on optimal path
- `victory.condition_diversification` - How many conditions pursued
- `victory.lead_margin` - Lead over 2nd place

---

## Export Pipeline Architecture

```csharp
[BurstCompile]
public partial struct AITelemetryExportSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<AITelemetryExportConfig>();
        var currentTick = SystemAPI.GetSingleton<TickTimeState>().Tick;

        // Check if should export this tick
        if (!ShouldExport(currentTick, config))
            return;

        // Gather all telemetry
        if (config.ExportWorldState)
            ExportWorldStateSnapshot(ref state);

        if (config.ExportEntityObservations)
            ExportEntityObservations(ref state);

        if (config.ExportSocialGraph)
            ExportSocialGraph(ref state);

        if (config.ExportActionRecords)
            ExportActionRecords(ref state);

        if (config.ExportEmergentBehaviors)
            ExportEmergentBehaviors(ref state);

        if (config.ExportTrainingMetrics)
            ExportTrainingMetrics(ref state);

        // Write to file/stream
        FlushExportBuffer(ref state, config);
    }
}
```

---

## Performance Targets

**Headless Training Build**:
```
World State Snapshot:     <1ms per snapshot (10 Hz)
Entity Observations:      <5ms per export (1000 entities)
Social Graph Export:      <2ms per export (500 edges)
Action Records:           <0.5ms per export (100 actions/tick)
Export to Disk:           <10ms per flush (async)
────────────────────────────────────────────
Total Overhead:           <20ms per frame @ 10 Hz export
                          <2ms per frame @ 1 Hz export
```

**Typical Training Session**:
- Episode length: 10,000 - 50,000 ticks (5-25 minutes game time)
- Snapshots per episode: 1,000 - 5,000 (10 Hz export)
- Data per episode: ~50-500 MB (compressed)
- Episodes per training run: 10,000 - 1,000,000

---

## Integration with ML Pipelines

### TensorFlow Integration
```python
# Python side - reading telemetry stream
import tensorflow as tf

def parse_telemetry_record(record):
    """Parse binary telemetry record into TF tensors."""
    features = {
        'tick': tf.io.FixedLenFeature([], tf.int64),
        'world_state': tf.io.FixedLenFeature([STATE_DIM], tf.float32),
        'entity_observations': tf.io.VarLenFeature(tf.float32),
        'action': tf.io.FixedLenFeature([ACTION_DIM], tf.float32),
        'reward': tf.io.FixedLenFeature([], tf.float32),
    }
    return tf.io.parse_single_example(record, features)
```

### PyTorch Integration
```python
# Python side - PyTorch DataLoader
class TelemetryDataset(torch.utils.data.Dataset):
    def __init__(self, telemetry_path):
        self.episodes = load_episodes(telemetry_path)

    def __getitem__(self, idx):
        episode = self.episodes[idx]
        return {
            'states': torch.tensor(episode['world_states']),
            'actions': torch.tensor(episode['actions']),
            'rewards': torch.tensor(episode['rewards']),
            'social_graph': torch.tensor(episode['social_graph']),
        }
```

---

## Summary

**New Telemetry Components** (18 total):
1. `WorldStateSnapshot` - Complete world state
2. `EntityObservation` - Per-entity observations
3. `SocialGraphEdge` - Relationship network
4. `AIActionRecord` - Action outcomes
5. `CombatEngagementRecord` - Battle tracking
6. `SocialActionRecord` - Dialogue outcomes
7. `EconomicDecisionRecord` - Economy tracking
8. `EmergentBehaviorEvent` - Novel strategies
9. `StrategyCluster` - Playstyle identification
10. `RLTrainingMetrics` - Learning progress
11. `AIComparisonTest` - A/B testing
12. `ForceUsageTelemetry` - Environmental forces
13. `LocomotionDecisionTelemetry` - Movement quality
14. `MemoryTapTelemetry` - Rally effectiveness
15. `BayCombatTelemetry` - Positioning quality
16. `DeceptionTelemetry` - Social manipulation
17. `AITelemetryExportConfig` - Export settings
18. `TrainingFeedback` - Real-time RL feedback

**Key Metrics** (50+ categories):
- Social: Deception, reputation, relations, memory tapping
- Combat: Positioning, coordination, morale, damage efficiency
- Economy: Income, ROI, bottlenecks, prediction accuracy
- Locomotion: Mode selection, energy efficiency, force usage
- Victory: Progress rate, critical path, lead margin
- Training: Reward, win rate, convergence, policy stability

**Export Formats**:
- CSV (human-readable)
- NDJSON (streaming JSON)
- Binary Burst (high performance)
- TFRecord (TensorFlow native)
- NPZ (NumPy arrays)

**Performance**: <20ms overhead @ 10Hz, supports 1M+ episodes training runs.
