# Three Pillar ECS Architecture

**Last Updated**: 2025-12-10
**Purpose**: Canonical specification for PureDOTS' multi-ECS architecture

---

## Overview

PureDOTS implements a three-pillar ECS architecture that separates concerns across deterministic simulation, planning/thinking, and aggregate analysis. All three pillars run on Unity's single Entities runtime but are organized as distinct World + SystemGroup combinations.

## Architecture Pillars

### 1. Body ECS (Simulation World)
**Purpose**: Canonical deterministic simulation world
**World Name**: `BodyWorld`
**System Group**: `BodySimulationSystemGroup`

**Responsibilities**:
- Time/rewind spine execution
- Spatial partitioning and grid management
- Registry infrastructure (Villager, Storehouse, Resource, Band, Miracle, Logistics, Construction)
- Core deterministic sim loops
- Physics integration (when active)

**Key Components**:
- `TimeState`, `RewindState`, `TickTimeState`
- `SpatialGridConfig`, `SpatialGridState`, `SpatialGridResidency`
- Registry singletons and buffers

**Design Constraints**:
- Must be fully deterministic
- No managed allocations in hot paths
- All mutations guarded by rewind checks
- Burst-compiled where performance-critical

### 2. Mind ECS (Planning World)
**Purpose**: Thinking, planning, and goal evaluation world
**World Name**: `MindWorld`
**System Group**: `MindSimulationSystemGroup`

**Responsibilities**:
- AI goal evaluation and utility scoring
- Pathfinding and steering calculations
- What-if scenario simulation
- Agent decision-making pipelines
- Learning and adaptation systems

**Key Components**:
- `GoalState`, `UtilityScore`, `DecisionContext`
- `PlanningState`, `ScenarioResult`
- AgentSyncBus communication buffers

**Design Constraints**:
- Can be non-deterministic (planning is speculative)
- Heavy computation allowed (not frame-critical)
- May use managed collections for complex algorithms
- Results fed back to Body ECS via sync mechanisms

### 3. Aggregate ECS (Analysis World)
**Purpose**: Higher-level aggregates and regional analysis
**World Name**: `AggregateWorld`
**System Group**: `AggregateSimulationSystemGroup`

**Responsibilities**:
- Band/village/civilization aggregation
- Regional summaries and statistics
- Dynasty and empire tracking
- Long-term trend analysis
- Strategic AI evaluation

**Key Components**:
- `BandAggregate`, `VillageAggregate`, `EmpireAggregate`
- `RegionalStats`, `TrendAnalysis`
- Historical data buffers

**Design Constraints**:
- Read-only access to Body/Mind worlds
- Computed values only (no mutations)
- May run at lower frequency than simulation
- Used for UI, strategy AI, and analytics

## World Synchronization

### AgentSyncBus Protocol
**Purpose**: Structured communication between ECS worlds
**Implementation**: `Packages/com.moni.puredots/Runtime/Systems/AgentSyncBus/`

**Message Types**:
- `MindToBodyCommand`: Planning results to simulation
- `BodyToMindUpdate`: Simulation state for planning
- `AggregateToMindDirective`: Strategic guidance to planning
- `MindToAggregateReport`: Planning insights to analysis

**Cadence**:
- Body → Mind: Every simulation tick
- Mind → Body: Planning completion events
- Aggregate ↔ Mind: End-of-turn summaries

## Implementation Details

### World Creation
```csharp
// In PureDOTS bootstrap
var bodyWorld = new World("BodyWorld");
var mindWorld = new World("MindWorld");
var aggregateWorld = new World("AggregateWorld");

// System groups organized by UpdateOrder
```

### System Organization
```csharp
[UpdateInGroup(typeof(BodySimulationSystemGroup))]
[UpdateAfter(typeof(TimeSystem))]
public partial struct MovementSystem : ISystem { }

[UpdateInGroup(typeof(MindSimulationSystemGroup))]
public partial struct GoalEvaluationSystem : ISystem { }

[UpdateInGroup(typeof(AggregateSimulationSystemGroup))]
public partial struct EmpireAggregationSystem : ISystem { }
```

### Data Flow
```
Body ECS → AgentSyncBus → Mind ECS → AgentSyncBus → Aggregate ECS
    ↑                        ↓                        ↓
Rewind/Time              Planning Results        Analysis Results
Deterministic            Speculative             Historical
```

## Performance Considerations

### Memory Isolation
- Each world has independent archetype storage
- No shared entity references between worlds
- Data duplication acceptable for clear boundaries

### Execution Frequency
- Body ECS: Every frame/tick (60+ FPS)
- Mind ECS: Planning intervals (seconds)
- Aggregate ECS: Turn-based (minutes)

### Burst Compilation
- Body systems: 100% Burst-compatible
- Mind systems: Burst where possible, managed where needed
- Aggregate systems: Mix based on computation type

## Testing Strategy

### Unit Tests
- Body systems: Determinism verification
- Mind systems: Decision logic validation
- Aggregate systems: Calculation accuracy

### Integration Tests
- World synchronization protocols
- Data flow correctness
- Performance regression monitoring

## Migration Notes

**Historical Context**: Originally conceived as separate ECS packages, but unified under single Entities runtime for:
- Simplified dependency management
- Better performance (single archetype registry)
- Easier debugging and profiling
- Reduced package complexity

**No External Dependencies**: These are not separate Unity packages. All three worlds exist within `com.moni.puredots` and the consuming game projects.

## Future Evolution

### Potential Extensions
- Multi-threaded Mind ECS for parallel planning
- Distributed Aggregate ECS for large-scale analysis
- GPU-accelerated Mind computations

### Integration Points
- Game-specific Mind systems in game projects
- Presentation layer reading from Aggregate ECS
- Scenario runner driving all three worlds

---

**See Also**:
- `AgentSyncBus_Specification.md` - Communication protocol details
- `MultiECS_Integration_Guide.md` - Game project integration patterns
- `FoundationGuidelines.md` - Coding standards across all worlds



