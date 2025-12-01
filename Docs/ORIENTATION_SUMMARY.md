# PureDOTS Codebase Orientation Summary

**Generated**: 2025-01-27  
**Purpose**: Comprehensive orientation for PureDOTS, Godgame, and Space4X foundations

## Executive Summary

PureDOTS is a **pure DOTS (Data-Oriented Technology Stack) framework** built on Unity Entities 1.4.2, designed as a reusable foundation for deterministic, rewind-capable simulations. It serves as the core runtime for two game projects:

- **Godgame**: A god-game simulation with villagers, resources, miracles, and divine hand interaction
- **Space4X**: A 4X space strategy game with vessels, modules, compliance, and trade systems

The framework emphasizes:
- **Pure DOTS architecture** (no MonoBehaviour service locators)
- **Determinism & rewind** (time control, history recording)
- **Registry-based entity management** (singleton components + buffers)
- **Spatial partitioning** (grid-based queries for performance)
- **AI pipeline** (shared sensor/utility/steering systems)

## Architecture Overview

### Core Bootstrap (`PureDotsWorldBootstrap`)

The world bootstrap creates a single ECS world with custom system groups:

**Root Groups:**
- `InitializationSystemGroup` - Core singleton seeding
- `FixedStepSimulationSystemGroup` - Deterministic 60 FPS simulation
- `SimulationSystemGroup` - Variable-rate simulation
- `PresentationSystemGroup` - Visual updates

**Domain Groups:**
- `TimeSystemGroup` - Time state, tick advancement, rewind coordination
- `VillagerSystemGroup` - Villager AI, needs, jobs, movement
- `ResourceSystemGroup` - Resource gathering, processing, storage
- `EnvironmentSystemGroup` - Climate, moisture, temperature, wind
- `SpatialSystemGroup` - Spatial grid rebuilds, queries
- `AISystemGroup` - Shared AI pipeline (sensors, utility, steering)
- `HandSystemGroup` - Divine hand input, cursor, miracles
- `VegetationSystemGroup` - Plant growth, health, reproduction
- `ConstructionSystemGroup` - Building sites, progress
- `CombatSystemGroup` - Combat resolution
- `MiracleEffectSystemGroup` - Miracle effects

### Core Singletons (`CoreSingletonBootstrapSystem`)

Seeds essential singleton components:
- `TimeState` / `TickTimeState` - Time control, tick counter
- `RewindState` / `HistorySettings` - Rewind mode, history capacity
- `SpatialGridState` - Spatial partitioning state
- `RegistryDirectory` - Registry lookup directory
- `TelemetryState` - Performance metrics
- `InputCommandLogState` - Input recording for replay
- All domain registries (Resource, Villager, Storehouse, etc.)

### Registry Pattern

All entity collections use **singleton components with DynamicBuffers**:
- `ResourceRegistry` + `DynamicBuffer<ResourceEntry>`
- `VillagerRegistry` + `DynamicBuffer<VillagerEntry>`
- `StorehouseRegistry` + `DynamicBuffer<StorehouseEntry>`
- `MiracleRegistry` + `DynamicBuffer<MiracleEntry>`
- `LogisticsRequestRegistry` + `DynamicBuffer<LogisticsRequestEntry>`
- `ConstructionRegistry` + `DynamicBuffer<ConstructionEntry>`
- `SpawnerRegistry` + `DynamicBuffer<SpawnerEntry>`
- Meta registries: `FactionRegistry`, `ClimateHazardRegistry`, `AreaEffectRegistry`, `CultureAlignmentRegistry`

**Registry Systems:**
- Each registry has a dedicated `*RegistrySystem` that rebuilds the buffer each tick
- `RegistryDirectorySystem` maintains a lookup table for registry discovery
- `RegistrySpatialSyncSystem` syncs spatial grid versions for continuity
- `RegistryHealthSystem` / `RegistryContinuityValidationSystem` validate integrity
- `RegistryInstrumentationSystem` publishes telemetry

### Time & Rewind System

**Time Control:**
- `TimeTickSystem` advances deterministic ticks
- `TimeStepSystem` handles variable time scale (pause, 1x, 2x, 5x, 10x)
- `FixedStepSimulationSystemGroup` runs at 60 FPS (1/60s timestep)

**Rewind:**
- `RewindCoordinatorSystem` manages rewind mode (Record, Playback, CatchUp)
- `RewindGuardSystems` prevent systems from running during playback
- `HistorySettings` controls history buffer capacity
- All systems check `RewindState.Mode` before mutating state

### Spatial Partitioning

**Spatial Grid:**
- `SpatialGridState` maintains a hashed grid (2D XZ plane, configurable cell size)
- `SpatialGridRebuildSystem` rebuilds grid each tick
- `SpatialQueryHelper` provides radius queries, kNN, proximity lookups
- `ISpatialGridProvider` abstraction allows swapping implementations

**Integration:**
- Registry entries cache `SpatialCellIndex` for fast queries
- `RegistrySpatialSyncSystem` tracks grid version for continuity validation

### AI Pipeline

**Shared AI Systems:**
- `AISensorUpdateSystem` - Populates sensor readings (entities, positions, distances)
- `AIUtilityScoringSystem` - Scores actions based on utility curves
- `AISteeringSystem` - Computes movement direction
- `AITaskResolutionSystem` - Resolves selected actions to commands

**Bridge Systems:**
- `GodgameVillagerAICommandBridgeSystem` - Maps AI commands to villager actions
- `Space4XVesselAICommandBridgeSystem` - Maps AI commands to vessel actions

**Sensor Categories:**
- `Resource`, `Storehouse`, `Villager`, `Miracle`, `Transport`, `Threat`, `Formation`

**Gaps (see `Docs/AI_Backlog.md`):**
- Virtual sensors for internal needs (hunger, energy) - **P0**
- Miracle detection in sensors - **P0**
- Flow field integration - **P1**
- Performance metrics - **P1**

## Godgame Integration

**Location**: `Assets/Projects/Godgame/`

**Key Systems:**
- `GodgameVillagerAICommandBridgeSystem` - Villager AI integration
- `GodgamePresentationBindingBootstrapSystem` - Presentation setup
- `GodgameVillageRoadBootstrapSystem` - Road network setup

**Components:**
- Villager archetypes, needs, jobs
- Resource nodes, storehouses
- Divine hand cursor, miracles
- Construction sites

**Status**: Core systems integrated, presentation bindings in place

## Space4X Integration

**Location**: `Assets/Projects/Space4X/`

**Key Systems:**
- `Space4XVesselAICommandBridgeSystem` - Vessel AI integration
- `Space4XConfigBootstrapper` - Configuration setup
- `TransportBootstrapSystem` - Transport logistics

**Components:**
- Vessel modules, degradation, repairs
- Compliance/alignment system
- Trade economy, spoilage
- Tech diffusion
- Mining deposits, harvest nodes

**Status**: Module system implemented, compliance/trade/tech in progress (see `Docs/TODO/Space4X_Frameworks_TODO.md`)

## Foundation Gaps & Priorities

### P0: Critical Foundation (Immediate)

1. **AI Virtual Sensors** (`Docs/AI_Backlog.md` AI-001)
   - Create `AIVirtualSensorSystem` to populate internal needs (hunger, energy) as sensor readings
   - Remove dual-path in `VillagerAISystem` (deprecate `VillagerUtilityScheduler`)
   - **Impact**: Unifies AI pipeline, removes code duplication

2. **AI Miracle Detection** (`Docs/AI_Backlog.md` AI-002)
   - Add miracle component lookups to `AISensorCategoryFilter`
   - Remove conditional compilation from AI systems
   - **Impact**: Enables miracle reactions in AI

3. **Villager Job Behavior Stubs** (`PureDOTS_TODO.md` section 3)
   - Flesh out `GatherJobBehavior`, `BuildJobBehavior`, `CraftJobBehavior`, `CombatJobBehavior`
   - Feed archetype catalog data into AI selection
   - **Impact**: Villagers can actually perform jobs (currently scaffolding only)

4. **Presentation Bridge Testing** (`Docs/TODO/PresentationBridge_TODO.md`)
   - Validation tests for rewind-safe presentation
   - Sample authoring guide
   - **Impact**: Ensures presentation doesn't break determinism

### P1: Important Foundation (Next Quarter)

5. **Flow Field Integration** (`Docs/AI_Backlog.md` AI-003)
   - Integrate `FlowFieldState` with `AISteeringSystem`
   - Blend flow fields with local avoidance
   - **Impact**: Scalable pathfinding for 100k+ agents

6. **Resources Framework** (`Docs/TODO/ResourcesFramework_TODO.md`)
   - Resource chunk physics, pile merging, siphon mechanics
   - Hand pickup/dump integration
   - **Impact**: Complete resource interaction loop

7. **Climate Systems Completion** (`Docs/TODO/ClimateSystems_TODO.md`)
   - Biome determination system completion
   - Wind field updates, fire propagation hooks
   - **Impact**: Environmental effects on gameplay

8. **Spatial Services Expansion** (`Docs/TODO/SpatialServices_TODO.md`)
   - kNN queries, multi-radius batches
   - 3D navigation layer hooks
   - **Impact**: Better spatial query performance

### P2: Foundation Polish (Future)

9. **Meta Registry Implementation** (`Docs/DesignNotes/MetaRegistryRoadmap.md`)
   - Faction, climate hazard, area effect, culture registries
   - **Impact**: High-level game systems (factions, hazards)

10. **Integration Test Coverage** (`Docs/TODO/SystemIntegration_TODO.md`)
    - Hand siphon + miracle token tests
    - Nightly performance suite automation
    - **Impact**: Prevents integration regressions

11. **Utilities & Tooling** (`Docs/TODO/Utilities_TODO.md`)
    - Deterministic replay harness
    - Performance profiling automation
    - **Impact**: Better development workflow

## Key Documentation

**Entry Points:**
- `Docs/PUREDOTS_INTEGRATION_SPEC.md` - **Canonical integration & extension guide** (how to interface with PureDOTS and extend it)
- `Docs/INDEX.md` - Navigation hub
- `Docs/OUTSTANDING_TODOS_SUMMARY.md` - Consolidated TODO list
- `PureDOTS_TODO.md` - Main project tracker

**Truth Sources:**
- `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` - System groups, determinism
- `Docs/TruthSources/PlatformPerformance_TruthSource.md` - IL2CPP, Burst, worker policy

**Design Notes:**
- `Docs/DesignNotes/SystemIntegration.md` - System integration patterns
- `Docs/DesignNotes/RewindPatterns.md` - Rewind implementation patterns
- `Docs/DesignNotes/SystemExecutionOrder.md` - System ordering

**TODO Trackers:**
- `Docs/TODO/Space4X_Frameworks_TODO.md` - Space4X backlog
- `Docs/TODO/VillagerSystems_TODO.md` - Villager system gaps
- `Docs/TODO/ResourcesFramework_TODO.md` - Resource system gaps
- `Docs/TODO/ClimateSystems_TODO.md` - Climate system gaps
- `Docs/AI_Backlog.md` - AI implementation backlog

## Code Organization

**Package Structure** (`Packages/com.moni.puredots/`):
- `Runtime/Runtime/` - Components, data structures
- `Runtime/Systems/` - ECS systems
- `Runtime/Authoring/` - Bakers, authoring components
- `Runtime/Config/` - ScriptableObject configs
- `Editor/` - Editor tooling

**Game Projects** (`Assets/Projects/`):
- `Godgame/` - Godgame-specific code
- `Space4X/` - Space4X-specific code

**Assembly Definitions:**
- `PureDOTS.Runtime` - Core runtime components
- `PureDOTS.Systems` - ECS systems
- `PureDOTS.Authoring` - Authoring/baking
- `PureDOTS.Editor` - Editor tooling
- `PureDOTS.Config` - Configuration assets
- `Godgame.Gameplay` - Godgame gameplay code
- `Space4X.Gameplay` - Space4X gameplay code

## Next Steps for Foundation Work

1. **Review AI Backlog** (`Docs/AI_Backlog.md`) - Prioritize AI-001 and AI-002
2. **Review Villager Systems TODO** (`Docs/TODO/VillagerSystems_TODO.md`) - Complete job behavior stubs
3. **Review Resources Framework TODO** (`Docs/TODO/ResourcesFramework_TODO.md`) - Plan chunk/pile implementation
4. **Review Space4X TODO** (`Docs/TODO/Space4X_Frameworks_TODO.md`) - Continue module/compliance work
5. **Review Technical Debt** (`Docs/TECHNICAL_DEBT.md`) - Address high-priority items

## Key Principles to Remember

1. **Pure DOTS**: No MonoBehaviour service locators; use singleton components + buffers
2. **Determinism**: All systems must respect `RewindState` and be deterministic
3. **Registry Pattern**: Entity collections use singleton + buffer pattern
4. **Content Neutrality**: Core systems must be game-agnostic; game-specific code in `Assets/Projects/`
5. **Burst Compatibility**: Systems should be Burst-compatible where possible
6. **IL2CPP Safety**: No reflection in jobs; use type registration helpers

## Common Patterns

**Registry Pattern:**
```csharp
// Singleton component
public struct VillagerRegistry : IComponentData { }

// Buffer of entries
public struct VillagerEntry : IBufferElementData
{
    public Entity Entity;
    public int Index;
    // ... other data
}

// System that rebuilds registry
[UpdateInGroup(typeof(VillagerSystemGroup))]
public partial struct VillagerRegistrySystem : ISystem
{
    // Rebuilds buffer each tick
}
```

**Rewind Guard:**
```csharp
if (rewindState.Mode != RewindMode.Record)
    return; // Skip during playback
```

**Spatial Query:**
```csharp
var queryHelper = SystemAPI.GetSingleton<SpatialQueryHelper>();
queryHelper.QueryRadius(position, radius, ref results);
```

**AI Command Bridge:**
```csharp
// Map AI utility scores to game-specific actions
var command = new VillagerGatherCommand { TargetResource = targetEntity };
commandBuffer.AddComponent(villagerEntity, command);
```

---

**This document should be updated as the codebase evolves and new foundation work is identified.**

