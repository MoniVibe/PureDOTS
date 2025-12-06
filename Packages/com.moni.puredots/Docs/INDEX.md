# PureDOTS Documentation Index

## Documentation Organization

Documentation is organized by responsibility:
- **Framework Docs**: `PureDOTS/Docs/` - PureDOTS framework documentation
- **Game Docs**: `Godgame/Docs/` and `Space4x/Docs/` - Game-specific documentation
- **Root**: `TRI_PROJECT_BRIEFING.md` - Project overview

---

## Entry Points

- `TRI_PROJECT_BRIEFING.md` (root) – project overview and coding patterns
- `Docs/Guides/DemoLockSystemsGuide.md` – **demo lock systems integration guide**
- `Docs/Guides/DemoLockSystemsAPI.md` – **demo lock systems API reference**
- `Docs/Guides/BiomeTerraformingIntegrationGuide.md` – **biome & terraforming integration guide**
- `Docs/Guides/BiomeTerraformingAPI.md` – **biome & terraforming API reference**
- `Docs/Guides/sanity.md` – system execution order and sanity checks
- `Docs/Guides/MovementAuthoringGuide.md` – movement authoring patterns
- `Docs/Guides/SimulationPresentationTimeSeparationGuide.md` – **simulation/presentation time separation guide**
- `Docs/Guides/ModifierSystemGuide.md` – **modifier system integration guide**
- `Docs/Guides/ModifierSystemAPI.md` – **modifier system API reference**
- `Docs/Guides/BootstrapSpineGuide.md` – **bootstrap spine implementation guide**
- `Docs/API/BootstrapSpineAPI.md` – **bootstrap spine API reference**

---

## Demo Lock Systems

**Safety and tooling systems for demo readiness:**

- **[DemoLockSystemsGuide.md](Guides/DemoLockSystemsGuide.md)** – Complete integration guide
  - Error Handling / Diagnostics Layer
  - Type Reflection Index
  - Telemetry Export
  - Scenario Serializer v2
  - CI Automation
  - Documentation Mirror
  - Crash Recovery

- **[DemoLockSystemsAPI.md](Guides/DemoLockSystemsAPI.md)** – API reference
  - Quick reference for programmatic access
  - Code examples
  - File locations

---

## Bootstrap Spine

**Minimal, predictable startup spine for simulation initialization:**

- **[BootstrapSpineGuide.md](Guides/BootstrapSpineGuide.md)** – Complete implementation guide
  - Architecture overview
  - Core components (WorldUtility, ConfigLoader, CoreSystems, etc.)
  - Bootstrap customization
  - Demo scenarios
  - Incremental feature path
  - Troubleshooting

- **[BootstrapSpineAPI.md](API/BootstrapSpineAPI.md)** – API reference
  - Quick reference for all bootstrap APIs
  - Code examples
  - Extension patterns

---

## Modifier System

**High-performance modifier system for scalable buff/debuff management:**

- **[ModifierSystemGuide.md](Guides/ModifierSystemGuide.md)** – Complete integration guide
  - Event-driven modifier application
  - Hot/cold path separation
  - Category aggregation (Economy, Military, Environment)
  - Dependency graph resolution
  - SIMD optimization
  - LOD culling
  - Integration with existing buff system

- **[ModifierSystemAPI.md](Guides/ModifierSystemAPI.md)** – API reference
  - Component APIs
  - System integration patterns
  - Query patterns
  - Performance best practices

---

## Environment & Biome Systems

**Optimized biome, terraforming, and shipboard ecology systems:**

- **[BiomeTerraformingIntegrationGuide.md](Guides/BiomeTerraformingIntegrationGuide.md)** – Complete integration guide
  - Layered-field representation (temperature, moisture, light, chemical)
  - Chunk-based incremental updates
  - Terraforming event system
  - Ship biodecks
  - Environmental telemetry for agents
  - Planet physical profiles

- **[BiomeTerraformingAPI.md](Guides/BiomeTerraformingAPI.md)** – API reference
  - Component definitions
  - System behaviors
  - Authoring workflows
  - Performance metrics

**Spatial-temporal field optimization systems:**

- **[SpatialTemporalFieldOptimizationIntegrationGuide.md](Guides/SpatialTemporalFieldOptimizationIntegrationGuide.md)** – Complete integration guide
  - Chunked grid compression (64×64 cells, half-precision)
  - Temporal LOD with per-system tick divisors
  - Unified field propagation (diffusion + advection)
  - Statistical vegetation sampling (10K → millions)
  - Event-driven fire propagation with rain interaction
  - Atmospheric feedback loops
  - Entity-environment coupling via EnvironmentSample
  - Biome ECS for asynchronous aggregation
  - AI goal optimization with spatial batching
  - Double-buffered field data

- **[SpatialTemporalFieldOptimizationAPI.md](Guides/SpatialTemporalFieldOptimizationAPI.md)** – API reference
  - Component definitions (ClimateChunk, TemporalLODConfig, EnvironmentSample, etc.)
  - System behaviors and execution order
  - Integration patterns and examples
  - Performance budgets and optimization tips

## Aggregate ECS Layer

**Multi-layered AI coordination for villages, fleets, and bands:**

- **[AggregateECSIntegrationGuide.md](Guides/AggregateECSIntegrationGuide.md)** – Complete integration guide
  - Creating aggregates and assigning agents
  - Aggregate statistics collection
  - Aggregate goal production and bias application
  - System ordering and update cadence
  - Customization patterns
  - Performance considerations

- **[AggregateECSAPI.md](Guides/AggregateECSAPI.md)** – API reference
  - Component definitions (AggregateMembership, AggregateEntity, AggregateIntent)
  - System behaviors (AggregateBridgeSystem, AggregateIntentSystem)
  - Message types (AggregateIntentMessage)
  - AgentSyncBus extensions
  - Integration checklist

## Social Dynamics Layer

**Large-scale social and cooperative dynamics across 3-layer ECS architecture:**

- **[SocialDynamicsIntegrationGuide.md](Guides/SocialDynamicsIntegrationGuide.md)** – Complete integration guide
  - Message-based communication protocols
  - Trust and reputation networks
  - Cooperative and competitive goal balancing
  - Social learning and cultural propagation
  - Morale, motivation, and social pressure
  - Economic cooperation (trade, territory, knowledge sharing)
  - Performance optimization strategies
  - Common use cases and troubleshooting

- **[SocialDynamicsAPI.md](Guides/SocialDynamicsAPI.md)** – API reference
  - Component definitions (SocialMessage, SocialKnowledge, GroupGoal, Motivation, etc.)
  - Message types and flags
  - AgentSyncBus extensions (SocialMessage, CulturalSignal queues)
  - Utility functions (CooperationResolutionSystem)
  - System behaviors (Body/Aggregate/Mind ECS systems)
  - Telemetry metrics

## Ownership & Economy Core

**Economic simulation system with three-layer ECS architecture (Body/Mind/Aggregate):**

- **[OwnershipEconomyGuide.md](Guides/OwnershipEconomyGuide.md)** – Complete integration guide
  - Component definitions (Ownership, Ledger, Portfolio, LegalEntity, AssetSpec)
  - System architecture (Body 60Hz, Mind 1Hz, Aggregate 0.2Hz)
  - Creating owned assets and production chains
  - Purchase/sale event processing
  - Tax collection and legal entities
  - Market equilibrium and price calculation
  - Integration with existing systems (Resource, Market, MindECS)
  - Authoring workflows
  - Performance considerations and optimization

- **[OwnershipEconomyQuickReference.md](Guides/OwnershipEconomyQuickReference.md)** – Quick reference
  - Component queries and common operations
  - System tick rates and locations
  - Integration patterns
  - Troubleshooting tips

## Advanced Optimizations (Phases 10-19)

**Performance optimization systems from simulation frameworks and academic literature:**

- **[AdvancedOptimizations_UsageGuide.md](Guides/AdvancedOptimizations_UsageGuide.md)** – Complete usage guide
  - Behavior Field Theory (O(n) crowd behavior)
  - Temporal-Budget Scheduling (adaptive performance)
  - Graph-Driven Entity Topology (influence propagation)
  - Constraint-Based Physics Integration (psychological realism)
  - Neural Surrogates (ML acceleration)
  - Generational Simulation Cycles (deterministic evolution)
  - Dynamic Load-Balancing (horizontal scaling)
  - Cognitive LOD (scalable AI quality)
  - Emotion and Reputation Graphs (emergent narratives)
  - AI Introspection Layer (explainability)

## Deterministic Simulation Architecture

**Core principles for deterministic, scalable simulation (Paradox/DSP/Factorio patterns):**

- **[DeterministicSimulationArchitecture.md](Guides/DeterministicSimulationArchitecture.md)** – Complete overview and quick reference
  - 12 core principles implementation
  - File locations and component references
  - Integration checklist

- **[DeterminismGuide.md](Guides/DeterminismGuide.md)** – Deterministic random, fixed-point math, time management
- **[HotColdSeparationGuide.md](Guides/HotColdSeparationGuide.md)** – Hot/cold data separation patterns
- **[PerformanceOptimizationGuide.md](Guides/PerformanceOptimizationGuide.md)** – Dirty flags, periodic ticks, hierarchical aggregation, LOD
- **[FlowfieldPathfindingGuide.md](Guides/FlowfieldPathfindingGuide.md)** – Flowfield-based pathfinding
- **[ModdingAPIGuide.md](Guides/ModdingAPIGuide.md)** – Safe modding API patterns
- **[SystemProfilingGuide.md](Guides/SystemProfilingGuide.md)** – System metrics and memory tracking
- **[DeterministicDebuggingGuide.md](Guides/DeterministicDebuggingGuide.md)** – Tick hashing and replay validation

## Unified ECS Architecture

**The architectural "Rosetta Stone" for consistent feature development across PureDOTS:**

- **[UnifiedECSDevelopmentGuide.md](Guides/UnifiedECSDevelopmentGuide.md)** – Complete architectural guide
  - Three Pillar ECS Architecture (Body/Mind/Aggregate)
  - PureDOTS Core Modules & Responsibilities
  - Game Side → PureDOTS Interface Patterns
  - Agent Developer Process (concept → implementation)
  - Data-Based Physics for Non-Animated Entities
  - Procedural Mental Models (Cognitive ECS)
  - Social/Cooperation Layer (Aggregate ECS)
  - Extending to "Anything" (Creative Sandbox Principle)
  - Performance & Scalability targets
  - Command Path (Game → Sim flow)
  - Practical Examples (Galaxy Brain vs Peasant)
  - Agent Developer Protocol

## Best Practices

**Implementation-friendly guides for DOTS 1.4.x, C# 9, Unity Input System, and performance optimization:**

- [Guides/sanity.md](Guides/sanity.md) - System execution order and sanity checks
- [Guides/MovementAuthoringGuide.md](Guides/MovementAuthoringGuide.md) - Movement authoring patterns
- [Guides/SimulationPresentationTimeSeparationGuide.md](Guides/SimulationPresentationTimeSeparationGuide.md) - Camera interpolation, input buffering, and presentation snapshots

---

## Generated Documentation

Auto-generated from Type Reflection Index (regenerated on CI):

- `Docs/Generated/Components.md` – All `IComponentData` types
- `Docs/Generated/Buffers.md` – All `IBufferElementData` types  
- `Docs/Generated/Systems.md` – All `ISystem` types with execution order

**Generate via**: `PureDOTS/Generate Documentation` or `CI/generate_docs.sh`

---

## See Also

- `TRI_PROJECT_BRIEFING.md` - Project overview and coding patterns
- `FoundationGuidelines.md` - Core coding standards (P0-P17 patterns)

