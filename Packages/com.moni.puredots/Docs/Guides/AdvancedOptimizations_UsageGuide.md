# Advanced ECS Optimizations Usage Guide

**Last Updated**: 2025-01-27  
**Scope**: Phases 10-19 of the performance optimization roadmap

This guide explains how to interface with and use the advanced optimization systems implemented in Phases 10-19. Each system is documented with component usage, integration patterns, and example code.

---

## Table of Contents

1. [Behavior Field Theory (Phase 10)](#phase-10-behavior-field-theory)
2. [Temporal-Budget Scheduling (Phase 11)](#phase-11-temporal-budget-scheduling)
3. [Graph-Driven Entity Topology (Phase 12)](#phase-12-graph-driven-entity-topology)
4. [Constraint-Based Physics Integration (Phase 13)](#phase-13-constraint-based-physics-integration)
5. [Neural Surrogates (Phase 14)](#phase-14-neural-surrogates)
6. [Generational Simulation Cycles (Phase 15)](#phase-15-generational-simulation-cycles)
7. [Dynamic Load-Balancing (Phase 16)](#phase-16-dynamic-load-balancing)
8. [Cognitive LOD (Phase 17)](#phase-17-cognitive-lod)
9. [Emotion and Reputation Graphs (Phase 18)](#phase-18-emotion-and-reputation-graphs)
10. [AI Introspection Layer (Phase 19)](#phase-19-ai-introspection-layer)

---

## Phase 10: Behavior Field Theory

### Overview
Agents emit potential fields (attraction to goals, repulsion from threats) that are sampled by Mind ECS for decision-making. Enables O(n) crowd behavior.

### Components

**`PotentialFieldEmitter`** - Attach to agent limbs/ship modules:
```csharp
var emitter = new PotentialFieldEmitter
{
    EmitterGuid = agentGuid,
    AttractionCoefficient = 0.8f,  // How much this attracts (0-1)
    RepulsionCoefficient = 0.5f,   // How much this repels (0-1)
    InfluenceRadius = 10f,          // Radius of influence
    EmitterPosition = transform.Position,
    Type = PotentialFieldType.Goal   // Goal, Threat, Social, Resource
};
entityManager.AddComponent(entity, emitter);
```

**`PotentialFieldScalar`** - Attached to spatial grid cells automatically by `PotentialFieldSystem`:
```csharp
// Read-only access in jobs
var scalarField = SystemAPI.GetComponent<PotentialFieldScalar>(cellEntity);
var attraction = scalarField.AttractionStrength;
var repulsion = scalarField.RepulsionStrength;
```

**`FieldGradientSample`** - Buffer element for Mind ECS consumption:
```csharp
// Samples are automatically added to entities by PotentialFieldSystem
var samples = SystemAPI.GetBuffer<FieldGradientSample>(entity);
foreach (var sample in samples)
{
    var gradient = sample.Gradient;  // Direction of influence
    var magnitude = sample.Magnitude; // Strength
}
```

### Integration Pattern

1. **Body ECS**: Add `PotentialFieldEmitter` to agents
2. **`PotentialFieldSystem`**: Updates scalar/vector fields in spatial grid
3. **Mind ECS**: `FieldGradientSamplerSystem` reads gradients and influences decisions

### Example: Adding a Goal Field to a Villager

```csharp
// In authoring or spawn system
var villagerEntity = CreateVillager();
var agentGuid = SystemAPI.GetComponent<AgentSyncId>(villagerEntity).Guid;

var emitter = new PotentialFieldEmitter
{
    EmitterGuid = agentGuid,
    AttractionCoefficient = 1.0f,
    RepulsionCoefficient = 0f,
    InfluenceRadius = 5f,
    EmitterPosition = position,
    Type = PotentialFieldType.Goal
};
entityManager.AddComponent(villagerEntity, emitter);
```

### Behavior Field Coefficients

Configure race-specific field behavior via `BehaviorFieldCoefficientsBlob`:
- `SocialBiasCoefficient`: Attraction to same race
- `AggressionCoefficient`: Repulsion strength
- `FearCoefficient`: Repulsion from threats
- `GoalAttractionCoefficient`: Goal attraction strength

---

## Phase 11: Temporal-Budget Scheduling

### Overview
Dynamic time budgets per system based on load. Automatically throttles Mind ECS when Body ECS nears budget limits.

### Components

**`TemporalBudget`** - Attach to systems that need budget tracking:
```csharp
var budget = new TemporalBudget
{
    AllocatedBudgetMs = 5.0f,  // 5ms budget
    ActualCostMs = 0f,          // Updated by profiler
    Priority = 10,              // Higher = more important
    LastUpdateTick = currentTick
};
entityManager.AddComponent(systemEntity, budget);
```

**`TemporalBudgetState`** - Singleton tracking global budget state:
```csharp
var budgetState = SystemAPI.GetSingleton<TemporalBudgetState>();
var totalUsed = budgetState.TotalUsedMs;
var utilization = budgetState.UtilizationRatio; // 0-1+
```

### Integration Pattern

1. **`MultiECSProfiler`**: Tracks actual costs per system
2. **`TemporalBudgetSystem`**: Redistributes budgets based on load
3. **Auto-scaling**: If Body ECS > 10ms, Mind ECS throttled to half frequency

### Example: Adding Budget Tracking to a Custom System

```csharp
[BurstCompile]
public partial struct MyCustomSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Create budget entity
        var budgetEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent(budgetEntity, new TemporalBudget
        {
            AllocatedBudgetMs = 2.0f,
            Priority = 5
        });
    }
}
```

---

## Phase 12: Graph-Driven Entity Topology

### Overview
Entity relationships stored as graphs (not flat buffers). Enables influence propagation, social simulation, heat diffusion.

### Components

**`EntityGraphNode`** - Represents an entity in the graph:
```csharp
var node = new EntityGraphNode
{
    NodeId = entityId,
    GraphEntity = entity,
    Type = GraphNodeType.Villager,
    LastUpdateTick = currentTick
};
entityManager.AddComponent(entity, node);
```

**`EntityGraphEdge`** - Buffer element representing relationships:
```csharp
var edges = entityManager.AddBuffer<EntityGraphEdge>(entity);
edges.Add(new EntityGraphEdge
{
    SourceNodeId = sourceId,
    TargetNodeId = targetId,
    Weight = 1.0f,
    Type = GraphEdgeType.Social,
    LastUpdateTick = currentTick
});
```

### Integration Pattern

1. **`EntityGraphSystem`**: Builds graph from relationship components
2. **`InfluencePropagationSystem`**: Propagates influence (morale, temperature) via BFS/DFS
3. **Graph traversal**: Use `NativeParallelMultiHashMap<int, int>` for adjacency

### Example: Creating a Social Graph

```csharp
// Create nodes
var villager1 = CreateVillager();
var villager2 = CreateVillager();

entityManager.AddComponent(villager1, new EntityGraphNode { NodeId = 1, Type = GraphNodeType.Villager });
entityManager.AddComponent(villager2, new EntityGraphNode { NodeId = 2, Type = GraphNodeType.Villager });

// Create edge (relationship)
var edges = entityManager.AddBuffer<EntityGraphEdge>(villager1);
edges.Add(new EntityGraphEdge
{
    SourceNodeId = 1,
    TargetNodeId = 2,
    Weight = 0.8f,
    Type = GraphEdgeType.Social
});
```

---

## Phase 13: Constraint-Based Physics Integration

### Overview
Constraint solving for psychological/social state (focus, energy, moral balance). Similar to physics constraints but for AI state.

### Components

**`FocusConstraint`** - Tracks focus with conservation:
```csharp
var focus = new FocusConstraint
{
    CurrentFocus = 100f,
    Capacity = 100f,
    RegenRate = 10f,      // Regeneration per second
    CostRate = 20f,       // Cost per second when active
    IsActive = true,
    LastUpdateTick = currentTick
};
entityManager.AddComponent(entity, focus);
```

**`EnergyConstraint`** - Similar to focus but for energy:
```csharp
var energy = new EnergyConstraint
{
    CurrentEnergy = 80f,
    Capacity = 100f,
    RegenRate = 5f,
    CostRate = 15f,
    IsActive = true
};
```

**`MoralConstraint`** - Tracks moral alignment:
```csharp
var moral = new MoralConstraint
{
    MoralAlignment = 0.5f,    // -1 (evil) to 1 (good)
    BalanceTarget = 0f,        // Target balance (usually 0)
    RestoreRate = 0.1f,        // Rate toward target
    DeviationPenalty = 0.05f
};
```

### Integration Pattern

1. **`ConstraintSolverSystem`**: Solves constraints iteratively each tick
2. **`PsychologicalConstraintSystem`**: Applies exhaustion/self-limiting behaviors
3. **Automatic**: Constraints update via `focus[i] -= cost; focus[i] += regen * dt; math.clamp(...)`

### Example: Using Focus Constraint

```csharp
// In a system that consumes focus
var focus = SystemAPI.GetComponentRW<FocusConstraint>(entity);
if (focus.ValueRO.CurrentFocus > focus.ValueRO.CostRate * deltaTime)
{
    focus.ValueRW.IsActive = true;  // Enable cost
    // Perform focus-consuming action
}
else
{
    focus.ValueRW.IsActive = false; // Disable to allow regeneration
}
```

---

## Phase 14: Neural Surrogates

### Overview
TinyML models replace expensive calculations (sensor noise, weather diffusion, morale prediction). Constant time, predictable cost.

### Components

**`NeuralSurrogateModel`** - Reference to trained model:
```csharp
var model = new NeuralSurrogateModel
{
    ModelId = 1,
    Type = NeuralModelType.SensorNoise,
    IsActive = true,
    InferenceCost = 0.1f  // ms
};
entityManager.AddComponent(entity, model);
```

**`NeuralModelWeightsLookup`** - Links to BlobAsset with weights:
```csharp
// Created via NeuralSurrogateAuthoring ScriptableObject
var weightsLookup = new NeuralModelWeightsLookup
{
    Value = blobAssetReference
};
entityManager.AddComponent(entity, weightsLookup);
```

### Integration Pattern

1. **Author model**: Create `NeuralSurrogateAuthoring` ScriptableObject
2. **Attach to entity**: Add `NeuralSurrogateModel` and `NeuralModelWeightsLookup`
3. **`NeuralSurrogateSystem`**: Runs inference automatically
4. **Replace calculations**: Use model output instead of expensive math

### Example: Using Neural Surrogate for Sensor Noise

```csharp
// In authoring
var modelAsset = ScriptableObject.CreateInstance<NeuralSurrogateAuthoring>();
modelAsset.modelId = "SensorNoise_v1";
modelAsset.type = NeuralModelType.SensorNoise;
modelAsset.weights = trainedWeights; // From training pipeline

// Attach to entity
var modelEntity = CreateModelEntity();
entityManager.AddComponent(modelEntity, new NeuralSurrogateModel
{
    ModelId = 1,
    Type = NeuralModelType.SensorNoise,
    IsActive = true
});
// WeightsLookup added automatically by Baker
```

---

## Phase 15: Generational Simulation Cycles

### Overview
Entities learn, evolve, and respawn deterministically. Offspring inherit blended traits from parents.

### Components

**`GeneSpec`** - Genetic trait specification:
```csharp
var gene = new GeneSpec
{
    GeneId = agentGuid,
    TraitValue = 0.7f,        // 0-1 normalized
    Type = GeneType.Speed,
    Dominance = 0.8f,         // 0-1, 1 = fully dominant
    Generation = 1
};
entityManager.AddComponent(entity, gene);
```

**`InheritanceData`** - Buffer storing parent contributions:
```csharp
var inheritance = entityManager.AddBuffer<InheritanceData>(offspring);
inheritance.Add(new InheritanceData
{
    ParentGuid = parentGuid,
    TraitType = GeneType.Speed,
    InheritedValue = 0.75f,
    Contribution = 0.5f  // 50% from this parent
});
```

**`GeneticState`** - Tracks evolution:
```csharp
var genetic = new GeneticState
{
    EntityGuid = agentGuid,
    Generation = 2,
    OffspringCount = 3,
    Fitness = 0.85f,
    LastEvolutionTick = currentTick
};
```

### Integration Pattern

1. **Add genes**: Attach `GeneSpec` components to entities
2. **`InheritanceSystem`**: Blends parent genes when entity dies/retires
3. **`EvolutionSystem`**: Evaluates fitness and applies selection pressure
4. **Deterministic**: Uses `RewindState.Seed` for RNG

### Example: Creating Offspring

```csharp
// When parent dies
var parentGenes = SystemAPI.GetBuffer<GeneSpec>(parentEntity);
var parentGenetic = SystemAPI.GetComponent<GeneticState>(parentEntity);

// Create offspring
var offspring = CreateEntity();
var offspringGenes = entityManager.AddBuffer<GeneSpec>(offspring);

// Blend genes (simplified)
for (int i = 0; i < parentGenes.Length; i++)
{
    var parentGene = parentGenes[i];
    var blendedValue = parentGene.TraitValue * 0.5f + otherParentGene.TraitValue * 0.5f;
    
    offspringGenes.Add(new GeneSpec
    {
        GeneId = offspringGuid,
        TraitValue = blendedValue,
        Type = parentGene.Type,
        Generation = parentGenetic.Generation + 1
    });
}
```

---

## Phase 16: Dynamic Load-Balancing

### Overview
Split worlds by density, not geography. Migrate hot clusters to new ECS worlds dynamically.

### Components

**`LoadBalanceMetrics`** - Tracks density and CPU load:
```csharp
var metrics = new LoadBalanceMetrics
{
    EntityDensity = 150f,     // Entities per unit area
    CPULoad = 0.75f,          // 0-1, 1 = fully loaded
    LoadScore = 112.5f,       // density × CPU load
    EntityCount = 1000,
    LastUpdateTick = currentTick
};
entityManager.AddComponent(partitionEntity, metrics);
```

**`WorldPartition`** - Tracks which world entity belongs to:
```csharp
var partition = new WorldPartition
{
    WorldId = worldGuid,
    PartitionIndex = 0,
    NeedsMigration = false,
    LastMigrationTick = currentTick
};
entityManager.AddComponent(entity, partition);
```

### Integration Pattern

1. **`DynamicLoadBalancer`**: Measures load metrics
2. **Threshold check**: If `LoadScore > threshold`, trigger rebalancing
3. **`EntityMigrationSystem`**: Migrates entities to new worlds
4. **`WorldScheduler`**: Manages per-world job scheduler threads

### Example: Checking Load and Migrating

```csharp
// Check if rebalancing needed
var balancerState = SystemAPI.GetSingleton<LoadBalancerState>();
if (balancerState.NeedsRebalance)
{
    // Find hot partition
    var metricsQuery = SystemAPI.QueryBuilder()
        .WithAll<LoadBalanceMetrics>()
        .Build();
    
    foreach (var (metrics, entity) in SystemAPI.Query<RefRO<LoadBalanceMetrics>>().WithEntityAccess())
    {
        if (metrics.ValueRO.LoadScore > 100f)
        {
            // Mark for migration
            var partition = SystemAPI.GetComponentRW<WorldPartition>(entity);
            partition.ValueRW.NeedsMigration = true;
        }
    }
}
```

---

## Phase 17: Cognitive LOD

### Overview
AI quality scales with importance. High-fidelity for visible/critical agents, statistical simulation for distant/idle.

### Components

**`CognitiveLOD`** - LOD level for entity:
```csharp
var lod = new CognitiveLOD
{
    Detail = CognitiveDetail.High,  // High, Medium, Low, Sleep
    DistanceScore = 0.9f,            // 0-1, closer = higher
    ImportanceScore = 0.8f,          // 0-1, leader/elite = higher
    CPULoadFactor = 0.5f,            // Current CPU load
    LastLODUpdateTick = currentTick
};
entityManager.AddComponent(entity, lod);
```

**`CognitiveLODState`** - Singleton tracking LOD distribution:
```csharp
var lodState = SystemAPI.GetSingleton<CognitiveLODState>();
var highCount = lodState.HighCount;
var sleepCount = lodState.SleepCount;
```

### Integration Pattern

1. **`CognitiveLODSystem`**: Assigns LOD based on distance, importance, CPU load
2. **Systems early-exit**: Check `lod.Detail` and skip processing for lower tiers
3. **Update intervals**: High (5 Hz), Medium (2 Hz), Low (0.5 Hz), Sleep (none)

### Example: LOD-Aware Cognitive Update

```csharp
[BurstCompile]
private partial struct MyCognitiveJob : IJobEntity
{
    public void Execute(ref MyCognitiveState state, in CognitiveLOD lod)
    {
        // Early-exit for lower LOD
        if (lod.Detail == CognitiveDetail.Sleep)
            return;
        
        if (lod.Detail == CognitiveDetail.Low)
        {
            // Statistical simulation only
            state.StatisticalUpdate();
            return;
        }
        
        if (lod.Detail == CognitiveDetail.Medium)
        {
            // Simplified logic
            state.SimplifiedUpdate();
            return;
        }
        
        // High detail: full cognitive logic
        state.FullCognitiveUpdate();
    }
}
```

---

## Phase 18: Emotion and Reputation Graphs

### Overview
Entities remember interactions. Long-term emergent narratives with negligible CPU impact.

### Components

**`EmotionState`** - Current emotional state:
```csharp
var emotion = new EmotionState
{
    Happiness = 0.7f,
    Fear = 0.2f,
    Anger = 0.1f,
    Trust = 0.8f,
    DecayRate = 0.01f,  // Decay per tick
    LastUpdateTick = currentTick
};
entityManager.AddComponent(entity, emotion);
```

**`InteractionDigest`** - Buffer storing interaction events:
```csharp
var interactions = entityManager.AddBuffer<InteractionDigest>(entity);
interactions.Add(new InteractionDigest
{
    InteractorGuid = interactorGuid,
    TargetGuid = targetGuid,
    PositiveDelta = 0.3f,
    NegativeDelta = 0f,
    Weight = 1.0f,
    InteractionTick = currentTick,
    Type = InteractionType.Help
});
```

**`ReputationNode`** - Reputation graph node:
```csharp
var reputation = new ReputationNode
{
    EntityGuid = agentGuid,
    OverallReputation = 0.6f,  // -1 to 1
    InteractionCount = 10,
    LastUpdateTick = currentTick
};
```

**`ReputationEdge`** - Buffer storing reputation relationships:
```csharp
var edges = entityManager.AddBuffer<ReputationEdge>(entity);
edges.Add(new ReputationEdge
{
    SourceGuid = sourceGuid,
    TargetGuid = targetGuid,
    ReputationValue = 0.7f,  // -1 to 1
    Weight = 0.9f,
    LastUpdateTick = currentTick
});
```

### Integration Pattern

1. **Record interactions**: Add `InteractionDigest` when entities interact
2. **`EmotionSystem`**: Updates emotions from interactions with decay
3. **`ReputationSystem`**: Updates reputation graphs and sentiment matrices
4. **Feed to AI**: Use reputation/emotion in Intent generation (vengefulness, trust)

### Example: Recording an Interaction

```csharp
// When villager helps another
var helper = GetHelperEntity();
var helped = GetHelpedEntity();

var interactions = SystemAPI.GetBuffer<InteractionDigest>(helper);
interactions.Add(new InteractionDigest
{
    InteractorGuid = SystemAPI.GetComponent<AgentSyncId>(helper).Guid,
    TargetGuid = SystemAPI.GetComponent<AgentSyncId>(helped).Guid,
    PositiveDelta = 0.5f,  // Positive interaction
    NegativeDelta = 0f,
    Weight = 1.0f,
    InteractionTick = currentTick,
    Type = InteractionType.Help
});

// Update reputation edge
var reputationEdges = SystemAPI.GetBuffer<ReputationEdge>(helper);
reputationEdges.Add(new ReputationEdge
{
    SourceGuid = helperGuid,
    TargetGuid = helpedGuid,
    ReputationValue = 0.5f,
    Weight = 1.0f,
    LastUpdateTick = currentTick
});
```

---

## Phase 19: AI Introspection Layer

### Overview
Make agents explain their choices. Human-readable diagnostics for debugging and AI tuning.

### Components

**`DecisionReasoning`** - Stores explanation for decision:
```csharp
var reasoning = new DecisionReasoning
{
    Reason = new FixedString128Bytes("Reason: threat proximity"),
    Code = DecisionReasonCode.ThreatProximity,
    Confidence = 0.9f,
    DecisionTick = currentTick
};
entityManager.AddComponent(entity, reasoning);
```

**`DecisionTreeNode`** - Buffer for structured decision trees:
```csharp
var tree = entityManager.AddBuffer<DecisionTreeNode>(entity);
tree.Add(new DecisionTreeNode
{
    NodeId = new FixedString64Bytes("root"),
    Reason = DecisionReasonCode.ThreatProximity,
    Score = 0.9f,
    ParentIndex = -1,  // Root node
    EvaluationTick = currentTick
});
```

**`AIDecisionTelemetry`** - Buffer for telemetry stream:
```csharp
var telemetry = SystemAPI.GetBuffer<AIDecisionTelemetry>(telemetryEntity);
telemetry.Add(new AIDecisionTelemetry
{
    AgentGuid = agentGuid,
    Reason = new FixedString128Bytes("Reason: low focus"),
    Code = DecisionReasonCode.LowFocus,
    Confidence = 0.7f,
    DecisionTick = currentTick,
    DecisionContext = position
});
```

### Integration Pattern

1. **Emit reasoning**: Each Mind ECS system adds `DecisionReasoning` when making decisions
2. **`IntrospectionSystem`**: Collects reasoning (zero-cost in release builds)
3. **`ExplainabilitySystem`**: Generates human-readable explanations
4. **Telemetry**: Stored in `AIDecisionTelemetry` for debugging/UI

### Example: Adding Decision Reasoning

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
// In a Mind ECS system making a decision
var reasoning = new DecisionReasoning
{
    Reason = new FixedString128Bytes($"Reason: {reasonText}"),
    Code = DetermineReasonCode(decision),
    Confidence = confidence,
    DecisionTick = currentTick
};
entity.Set(reasoning);

// Add to telemetry stream
var telemetry = GetTelemetryBuffer();
telemetry.Add(new AIDecisionTelemetry
{
    AgentGuid = agentGuid,
    Reason = reasoning.Reason,
    Code = reasoning.Code,
    Confidence = reasoning.Confidence,
    DecisionTick = currentTick
});
#endif
```

---

## System Integration Order

When integrating multiple systems, follow this order:

1. **Phase 17 (Cognitive LOD)** - Foundation for scalability
2. **Phase 11 (Temporal-Budget)** - Adaptive performance management
3. **Phase 12 (Graph Topology)** - Foundation for relationships
4. **Phase 18 (Emotion/Reputation)** - Long-term narratives
5. **Phase 13 (Constraint-Based)** - Psychological realism
6. **Phase 10 (Behavior Field)** - Crowd behavior
7. **Phase 15 (Generational)** - Evolution system
8. **Phase 19 (Introspection)** - Debugging support
9. **Phase 14 (Neural Surrogates)** - ML acceleration (requires training)
10. **Phase 16 (Load-Balancing)** - Final scalability (most complex)

---

## Common Patterns

### Querying Components

```csharp
// Single component
var lod = SystemAPI.GetComponent<CognitiveLOD>(entity);

// Read-write
var lodRW = SystemAPI.GetComponentRW<CognitiveLOD>(entity);
lodRW.ValueRW.Detail = CognitiveDetail.High;

// Buffer
var interactions = SystemAPI.GetBuffer<InteractionDigest>(entity);
foreach (var interaction in interactions) { ... }
```

### System Dependencies

```csharp
[UpdateInGroup(typeof(AISystemGroup))]
[UpdateAfter(typeof(CognitiveLODSystem))]  // Run after LOD assignment
public partial struct MySystem : ISystem { ... }
```

### Burst Compliance

All Body ECS systems must be `[BurstCompile]`. Mind ECS systems are managed (DefaultEcs).

---

## Performance Considerations

- **LOD**: Use Cognitive LOD to reduce CPU for distant entities
- **Budgets**: Monitor `TemporalBudgetState` to prevent frame drops
- **Graphs**: Keep graph traversal bounded (max depth/iterations)
- **Neural**: Prefer lookup tables over inference when possible
- **Load Balancing**: Trigger migration only when `LoadScore > 0.8f`

---

## Debugging

- **Introspection**: Enable `#if UNITY_EDITOR || DEVELOPMENT_BUILD` for decision reasoning
- **Telemetry**: Check `AIDecisionTelemetry` buffer for decision history
- **LOD State**: Monitor `CognitiveLODState` for LOD distribution
- **Budget State**: Check `TemporalBudgetState` for budget utilization

---

## Further Reading

- See `MultiECSArchitecture.md` for overall architecture
- See `RuntimeLifecycle_TruthSource.md` for system update order
- See individual system source files for detailed implementation

