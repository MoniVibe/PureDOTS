# Advanced Optimizations API Reference

**Quick reference for Phases 10-19 optimization systems**

---

## Component Quick Reference

### Phase 10: Behavior Field Theory

```csharp
// Emitter component
PotentialFieldEmitter {
    EmitterGuid: AgentGuid
    AttractionCoefficient: float (0-1)
    RepulsionCoefficient: float (0-1)
    InfluenceRadius: float
    EmitterPosition: float3
    Type: PotentialFieldType (Goal, Threat, Social, Resource)
}

// Field data (read-only)
PotentialFieldScalar {
    AttractionStrength: float
    RepulsionStrength: float
    CombinedPotential: float
}

PotentialFieldVector {
    Gradient: float3
    Magnitude: float
}

// Buffer for Mind ECS
FieldGradientSample {
    Position: float3
    Gradient: float3
    Magnitude: float
    Type: PotentialFieldType
}
```

### Phase 11: Temporal-Budget Scheduling

```csharp
TemporalBudget {
    AllocatedBudgetMs: float
    ActualCostMs: float
    AverageCostMs: float
    BudgetUtilization: float (0-1+)
    Priority: int
}

TemporalBudgetState {
    TotalBudgetMs: float
    TotalUsedMs: float
    UtilizationRatio: float
    SystemCount: int
}
```

### Phase 12: Graph-Driven Entity Topology

```csharp
EntityGraphNode {
    NodeId: int
    GraphEntity: Entity
    NeighborCount: int
    Type: GraphNodeType
}

EntityGraphEdge : IBufferElementData {
    SourceNodeId: int
    TargetNodeId: int
    Weight: float
    Type: GraphEdgeType
}
```

### Phase 13: Constraint-Based Physics

```csharp
FocusConstraint {
    CurrentFocus: float
    Capacity: float
    RegenRate: float
    CostRate: float
    IsActive: bool
}

EnergyConstraint {
    CurrentEnergy: float
    Capacity: float
    RegenRate: float
    CostRate: float
    IsActive: bool
}

MoralConstraint {
    MoralAlignment: float (-1 to 1)
    BalanceTarget: float
    RestoreRate: float
    DeviationPenalty: float
}
```

### Phase 14: Neural Surrogates

```csharp
NeuralSurrogateModel {
    ModelId: int
    Type: NeuralModelType (SensorNoise, WeatherDiffusion, MoralePrediction)
    IsActive: bool
    InferenceCost: float
}

NeuralModelWeightsLookup {
    Value: BlobAssetReference<NeuralModelWeightsBlob>
}
```

### Phase 15: Generational Simulation

```csharp
GeneSpec {
    GeneId: AgentGuid
    TraitValue: float (0-1)
    Type: GeneType
    Dominance: float (0-1)
    Generation: uint
}

InheritanceData : IBufferElementData {
    ParentGuid: AgentGuid
    TraitType: GeneType
    InheritedValue: float
    Contribution: float
}

GeneticState {
    EntityGuid: AgentGuid
    Generation: uint
    OffspringCount: int
    Fitness: float
}
```

### Phase 16: Dynamic Load-Balancing

```csharp
LoadBalanceMetrics {
    EntityDensity: float
    CPULoad: float (0-1)
    LoadScore: float (density × CPU load)
    EntityCount: int
}

WorldPartition {
    WorldId: AgentGuid
    PartitionIndex: int
    NeedsMigration: bool
    LastMigrationTick: uint
}

LoadBalancerState {
    ActiveWorldCount: int
    AverageLoadScore: float
    MaxLoadScore: float
    NeedsRebalance: bool
}
```

### Phase 17: Cognitive LOD

```csharp
CognitiveLOD {
    Detail: CognitiveDetail (High, Medium, Low, Sleep)
    DistanceScore: float (0-1)
    ImportanceScore: float (0-1)
    CPULoadFactor: float (0-1)
}

CognitiveLODState {
    HighCount: int
    MediumCount: int
    LowCount: int
    SleepCount: int
    TargetCPULoad: float
}
```

### Phase 18: Emotion and Reputation

```csharp
EmotionState {
    Happiness: float (0-1)
    Fear: float (0-1)
    Anger: float (0-1)
    Trust: float (0-1)
    DecayRate: float
}

InteractionDigest : IBufferElementData {
    InteractorGuid: AgentGuid
    TargetGuid: AgentGuid
    PositiveDelta: float
    NegativeDelta: float
    Weight: float
    Type: InteractionType
}

ReputationNode {
    EntityGuid: AgentGuid
    OverallReputation: float (-1 to 1)
    InteractionCount: int
}

ReputationEdge : IBufferElementData {
    SourceGuid: AgentGuid
    TargetGuid: AgentGuid
    ReputationValue: float (-1 to 1)
    Weight: float
}
```

### Phase 19: AI Introspection

```csharp
DecisionReasoning {
    Reason: FixedString128Bytes
    Code: DecisionReasonCode
    Confidence: float (0-1)
    DecisionTick: uint
}

DecisionTreeNode : IBufferElementData {
    NodeId: FixedString64Bytes
    Reason: DecisionReasonCode
    Score: float
    ParentIndex: int
}

AIDecisionTelemetry : IBufferElementData {
    AgentGuid: AgentGuid
    Reason: FixedString128Bytes
    Code: DecisionReasonCode
    Confidence: float
    DecisionTick: uint
    DecisionContext: float3
}
```

---

## System Quick Reference

### Body ECS Systems (Burst-compiled)

```csharp
// Phase 10
PotentialFieldSystem : ISystem
  [UpdateInGroup(typeof(AISystemGroup))]
  [UpdateAfter(typeof(SpatialGridBuildSystem))]

// Phase 11
TemporalBudgetSystem : ISystem
  [UpdateInGroup(typeof(InitializationSystemGroup))]

// Phase 12
EntityGraphSystem : ISystem
  [UpdateInGroup(typeof(SpatialSystemGroup))]
InfluencePropagationSystem : ISystem
  [UpdateInGroup(typeof(AISystemGroup))]

// Phase 13
ConstraintSolverSystem : ISystem
  [UpdateInGroup(typeof(AISystemGroup))]
PsychologicalConstraintSystem : ISystem
  [UpdateInGroup(typeof(AISystemGroup))]

// Phase 14
NeuralSurrogateSystem : ISystem
  [UpdateInGroup(typeof(AISystemGroup))]

// Phase 15
InheritanceSystem : ISystem
  [UpdateInGroup(typeof(AISystemGroup))]
EvolutionSystem : ISystem
  [UpdateInGroup(typeof(AISystemGroup))]

// Phase 16
DynamicLoadBalancer : ISystem
  [UpdateInGroup(typeof(InitializationSystemGroup))]
EntityMigrationSystem : ISystem
  [UpdateInGroup(typeof(AISystemGroup))]
WorldScheduler : ISystem
  [UpdateInGroup(typeof(InitializationSystemGroup))]

// Phase 17
CognitiveLODSystem : ISystem
  [UpdateInGroup(typeof(AISystemGroup))]

// Phase 18
EmotionSystem : ISystem
  [UpdateInGroup(typeof(AISystemGroup))]
ReputationSystem : ISystem
  [UpdateInGroup(typeof(AISystemGroup))]

// Phase 19
IntrospectionSystem : ISystem
  [UpdateInGroup(typeof(AISystemGroup))]
```

### Mind ECS Systems (DefaultEcs, managed)

```csharp
// Phase 10
FieldGradientSamplerSystem : AEntitySetSystem<float>

// Phase 17
LODCognitiveSystem : AEntitySetSystem<float>

// Phase 19
ExplainabilitySystem : AEntitySetSystem<float>
```

---

## Common Query Patterns

```csharp
// Query with LOD check
var query = SystemAPI.QueryBuilder()
    .WithAll<MyComponent, CognitiveLOD>()
    .Build();

// Query with constraint
var constraintQuery = SystemAPI.QueryBuilder()
    .WithAll<FocusConstraint>()
    .WithNone<EnergyConstraint>()
    .Build();

// Query graph nodes
var graphQuery = SystemAPI.QueryBuilder()
    .WithAll<EntityGraphNode, EntityGraphEdge>()
    .Build();
```

---

## Integration Checklist

- [ ] Add components to entities
- [ ] Ensure systems are in correct update groups
- [ ] Set up system dependencies (`[UpdateAfter]`)
- [ ] Configure BlobAssets (if needed)
- [ ] Add budget tracking (Phase 11)
- [ ] Enable introspection (Phase 19, editor only)
- [ ] Test determinism (all phases)
- [ ] Profile performance impact

---

## File Locations

### Components
- `Runtime/Components/PotentialFieldComponents.cs`
- `Runtime/Components/TemporalBudgetComponents.cs`
- `Runtime/Components/EntityGraphComponents.cs`
- `Runtime/Components/ConstraintComponents.cs`
- `Runtime/Components/NeuralSurrogateComponents.cs`
- `Runtime/Components/GeneticComponents.cs`
- `Runtime/Components/LoadBalanceComponents.cs`
- `Runtime/Components/CognitiveLODComponents.cs`
- `Runtime/Components/EmotionComponents.cs`
- `Runtime/Components/ReputationGraphComponents.cs`
- `Runtime/Components/IntrospectionComponents.cs`

### Systems
- `Runtime/Systems/AI/PotentialFieldSystem.cs`
- `Runtime/Core/TemporalBudgetSystem.cs`
- `Runtime/Systems/Graph/EntityGraphSystem.cs`
- `Runtime/Systems/Graph/InfluencePropagationSystem.cs`
- `Runtime/Systems/Constraints/ConstraintSolverSystem.cs`
- `Runtime/Systems/Constraints/PsychologicalConstraintSystem.cs`
- `Runtime/Systems/AI/NeuralSurrogateSystem.cs`
- `Runtime/Systems/Genetics/InheritanceSystem.cs`
- `Runtime/Systems/Genetics/EvolutionSystem.cs`
- `Runtime/Core/DynamicLoadBalancer.cs`
- `Runtime/Systems/LoadBalancing/EntityMigrationSystem.cs`
- `Runtime/Core/WorldScheduler.cs`
- `Runtime/Systems/AI/CognitiveLODSystem.cs`
- `Runtime/Systems/Emotion/EmotionSystem.cs`
- `Runtime/Systems/Reputation/ReputationSystem.cs`
- `Runtime/Systems/AI/IntrospectionSystem.cs`

### Mind ECS Systems
- `Runtime/AI/MindECS/Systems/FieldGradientSamplerSystem.cs`
- `Runtime/AI/MindECS/Systems/LODCognitiveSystem.cs`
- `Runtime/AI/MindECS/Systems/ExplainabilitySystem.cs`

### BlobAssets
- `Runtime/Components/BehaviorFieldBlob.cs`
- `Runtime/Components/GraphBlob.cs`
- `Runtime/Components/ConstraintBlob.cs`
- `Runtime/Components/GeneBlob.cs`
- `Runtime/Components/LODBlob.cs`

### Authoring
- `Runtime/Authoring/NeuralSurrogateAuthoring.cs`

---

## Performance Targets

- **Sync Cost**: <3ms (Mind↔Body)
- **Frame Time**: <16.67ms (60 FPS) at 1M entities
- **LOD Distribution**: 80% Sleep, 15% Low, 4% Medium, 1% High
- **Budget Utilization**: <90% per system
- **Load Score**: <100 (density × CPU load)

---

## See Also

- [AdvancedOptimizations_UsageGuide.md](AdvancedOptimizations_UsageGuide.md) - Detailed usage guide
- [MultiECSArchitecture.md](../MultiECSArchitecture.md) - Overall architecture
- [RuntimeLifecycle_TruthSource.md](../RuntimeLifecycle_TruthSource.md) - System update order

