# Unified ECS Development Guide

**The Architectural "Rosetta Stone" for PureDOTS Development**

> **Purpose**: This guide formalizes how the core ECS layers interact, what their responsibilities are, and how developers (AI or human) should safely interface between them from the game side to PureDOTS core side. Use this guide to develop every feature consistently across the PureDOTS ecosystem, whether it's a peasant planting crops or a galaxy-sized extradimensional intelligence rearranging fleets through psionic limb-theft.

**Last Updated**: 2025-01-27  
**See Also**: [TRI_PROJECT_BRIEFING.md](../../../../TRI_PROJECT_BRIEFING.md), [RuntimeLifecycle_TruthSource.md](../../../../Docs/TruthSources/RuntimeLifecycle_TruthSource.md), [FoundationGuidelines.md](../../FoundationGuidelines.md)

---

## Table of Contents

1. [Introduction & Philosophy](#1-introduction--philosophy)
2. [The Three Pillar ECS Architecture](#2-the-three-pillar-ecs-architecture)
3. [PureDOTS Core Modules & Responsibilities](#3-puredots-core-modules--responsibilities)
4. [Game Side → PureDOTS Interface Patterns](#4-game-side--puredots-interface-patterns)
5. [Agent Developer Process](#5-agent-developer-process)
6. [Data-Based "Physics" for Non-Animated Entities](#6-data-based-physics-for-non-animated-entities)
7. [Procedural Mental Models (Cognitive ECS)](#7-procedural-mental-models-cognitive-ecs)
8. [Social/Cooperation Layer (Aggregate ECS)](#8-socialcooperation-layer-aggregate-ecs)
9. [Extending to "Anything" (Creative Sandbox Principle)](#9-extending-to-anything-creative-sandbox-principle)
10. [Performance & Scalability](#10-performance--scalability)
11. [Game Side → Sim Side "Command Path"](#11-game-side--sim-side-command-path)
12. [Practical Example: "Galaxy Brain vs Peasant"](#12-practical-example-galaxy-brain-vs-peasant)
13. [Agent Developer Protocol](#13-agent-developer-protocol)

---

## 1. Introduction & Philosophy

### Core Principle

> **"Everything is a composition of data and deterministic rules. Imagination is infinite — implementation is consistent."**

This principle means the same ECS stack can host:
- A peasant's rags-to-riches social climb
- A unicorn's flight over meteor storms
- A sentient galaxy manipulating civilizations

All entities, all scales, same ruleset.

### Scope

This guide covers the architectural foundation that enables consistent feature development across:
- **PureDOTS**: Shared DOTS framework package
- **Space4X**: Carrier-first 4X strategy game
- **Godgame**: Divine intervention god-game simulation

### Design Philosophy

1. **Deterministic Core**: Fixed-step simulation with rewind-safe state management
2. **DOTS-Only Simulation**: Systems reside in explicit groups, compiled with Burst
3. **Layer Separation**: Body/Mind/Aggregate ECS communicate only through message buses
4. **Data-Oriented**: Components are pure data; systems own behavior
5. **Scalable**: Target 10M+ active entities under 16 ms/frame

---

## 2. The Three Pillar ECS Architecture

PureDOTS uses a three-layer ECS architecture where each layer operates at different frequencies and handles distinct responsibilities. **Each layer is a self-contained world** — they communicate only through structured message buses (no shared references).

### Architecture Overview

| Layer | Purpose | Tick Rate | Determinism | Primary Data |
|-------|---------|-----------|-------------|--------------|
| **Body ECS** | Physics, movement, combat, reflexes, immediate perception | 60 Hz | ✅ Deterministic | Position, Velocity, Reflex, Focus, AttackArc, etc. |
| **Mind ECS** | Cognition, learning, procedural reasoning, emotion, decision-making | 1 Hz | ✅ Deterministic | SkillProfile, EmotionState, ProceduralMemory, CognitiveStats |
| **Aggregate ECS** | Groups, cultures, fleets, civilizations, doctrines, large-scale AI | 0.2 Hz | ✅ Deterministic | GroupGoal, Formation, CulturalDoctrine, Morale, Cohesion |

### Body ECS (60 Hz - Reflex Layer)

**System Groups**: `ReflexSystemGroup`, `HotPathSystemGroup`, `CombatSystemGroup`

**Responsibilities**:
- Immediate sensor→action reactive mapping
- Physics and movement calculations
- Combat resolution and attack arcs
- Reflex responses (no learning, lowest latency)

**Example Components**:
```csharp
// From Unity.Transforms
public struct LocalTransform : IComponentData
{
    public float3 Position;
    public quaternion Rotation;
    public float Scale;
}

// From Unity.Physics
public struct PhysicsVelocity : IComponentData
{
    public float3 Linear;
    public float3 Angular;
}

public struct PhysicsMass : IComponentData
{
    public float InverseMass;
    public float3 InverseInertia;
}
```

**Example System**:
```csharp
[BurstCompile]
[UpdateInGroup(typeof(ReflexSystemGroup))]
public partial struct ReflexSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Process immediate reflexes at 60 Hz
        // No learning, pure reactive mapping
    }
}
```

### Mind ECS (1 Hz - Cognitive Layer)

**System Groups**: `CognitiveSystemGroup`, `LearningSystemGroup`, `MotivationSystemGroup`

**Responsibilities**:
- Procedural learning and pattern extraction
- Skill development and memory formation
- Emotion-driven decision-making
- Utility calculations for action selection

**Example Components**:
```csharp
// From Runtime/Cognitive/SkillComponents.cs
public struct SkillProfile : IComponentData
{
    public float CastingSkill;        // 0-1 proficiency
    public float MeleeSkill;          // 0-1 proficiency
    public float StrategicThinking;   // 0-1 proficiency
    public uint LastUpdateTick;
}

// From Runtime/Cognitive/EmotionComponents.cs
public struct EmotionState : IComponentData
{
    public float Anger;   // 0-1
    public float Trust;   // 0-1
    public float Fear;    // 0-1
    public float Pride;   // 0-1
    public uint LastUpdateTick;
}

// From Runtime/AI/Cognitive/Components/CognitiveStats.cs
public struct CognitiveStats : IComponentData
{
    public float Intelligence;  // 0-10, computational efficiency
    public float Wisdom;        // 0-10, pattern recognition
    public float Curiosity;     // 0-10, exploration weight
    public float Focus;         // 0-10, cognitive stamina
    public float MaxFocus;
    public uint LastFocusDecayTick;
}
```

**Example System**:
```csharp
[BurstCompile]
[UpdateInGroup(typeof(LearningSystemGroup))]
public partial struct SkillLearningSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Process procedural learning at 1 Hz
        // Update skill profiles based on experience
    }
}
```

### Aggregate ECS (0.2 Hz - Social/Cultural Layer)

**System Groups**: `TacticalSystemGroup`, `GroupDecisionSystemGroup`, `EconomySystemGroup`

**Responsibilities**:
- Group-level decision-making (villages, fleets, bands)
- Cultural doctrine propagation
- Morale and cohesion management
- Large-scale AI coordination

**Example Components**:
```csharp
// From Runtime/AI/Social/SocialComponents.cs
public struct GroupGoal : IComponentData
{
    public float CooperationWeight;   // 0-1
    public float CompetitionWeight;   // 0-1
    public float ResourcePriority;    // 0-1
    public float ThreatLevel;         // 0-1
    public float GroupCohesion;       // 0-1
    public uint LastEvaluationTick;
}

// From Runtime/Morale/MoraleComponents.cs
public struct EntityMorale : IComponentData
{
    public float CurrentMorale;       // 0-1000
    public MoraleBand Band;           // Despair, Unhappy, Stable, Cheerful, Elated
    public float WorkSpeedModifier;   // -0.20 to +0.15
    public float InitiativeModifier;   // -0.40 to +0.25
    public uint LastUpdateTick;
}

// From Runtime/Culture/CulturalDoctrine.cs
public struct CulturalDoctrineReference : IComponentData
{
    public BlobAssetReference<CulturalDoctrineBlob> Doctrine;
}
```

**Example System**:
```csharp
[BurstCompile]
[UpdateInGroup(typeof(TacticalSystemGroup))]
public partial struct GroupMoraleSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Process group morale at 0.2 Hz (every 5 ticks)
        // Aggregate individual morale into group metrics
    }
}
```

### Cross-Layer Communication

**Critical Rule**: Layers communicate **only** through message buses. Never pass direct references between layers.

**Message Bus Pattern**:
```csharp
// From Runtime/Bridges/MindToBodySyncSystem.cs
// Mind ECS → Body ECS communication via AgentSyncBus

public struct MindToBodyMessage
{
    public AgentGuid AgentGuid;
    public AgentIntentKind Kind;
    public float3 TargetPosition;
    public Entity TargetEntity;
    public float Priority;
    public uint TickNumber;
}

// Body ECS → Mind ECS communication (perception, reflexes)
public struct BodyToMindMessage
{
    public AgentGuid AgentGuid;
    public PerceptionEventType EventType;
    public float3 Position;
    public float Urgency;
    public uint TickNumber;
}
```

**Reference**: See [MultiECS_Integration_Guide.md](MultiECS_Integration_Guide.md) for complete API reference.

---

## 3. PureDOTS Core Modules & Responsibilities

PureDOTS provides foundational modules that all game projects consume. These modules are **game-agnostic** and enforce deterministic, scalable simulation.

### Core Module Map

| Core Module | Function | Implementation Location |
|------------|----------|------------------------|
| **Time Spine** | Fixed-step deterministic ticking and rewind buffer | `Runtime/Systems/TimeTickSystem.cs`, `Runtime/Components/TimeState.cs` |
| **Registries** | Authoritative entity catalogs (Villager, Fleet, Resource, Miracle, etc.) | `Runtime/Registry/` |
| **Spatial Grid** | Hierarchical spatial partitioning, load balancing | `Runtime/Spatial/` |
| **Communication Bus** | Message exchange across ECS worlds | `Runtime/Bridges/AgentSyncBus.cs`, `Runtime/Bridges/MindToBodySyncSystem.cs` |
| **Rewind System** | Snapshot/delta chain for replay and debugging | `Runtime/Components/RewindState.cs`, `Runtime/Systems/History/` |
| **Telemetry Stream** | Performance and behavioral metrics | `Runtime/Telemetry/` |
| **Scenario Runner** | Headless scenario execution | `Runtime/Devtools/ScenarioRunner.cs` |
| **Economy System** | Ownership, markets, production chains | `Runtime/Economy/` |

### Time Spine

**Purpose**: Single source of simulation time with deterministic fixed-step ticking.

**Key Components**:
```csharp
// From Runtime/Components/TimeState.cs
public struct TimeState : IComponentData
{
    public uint Tick;                    // Current simulation tick
    public float FixedDeltaTime;         // Fixed timestep (e.g., 1/60s)
    public float ElapsedTime;            // Total elapsed simulation time
}

public struct TickTimeState : IComponentData
{
    public uint Tick;
    public uint TargetTick;
    public bool IsPlaying;
    public bool IsPaused;
}

public struct RewindState : IComponentData
{
    public RewindMode Mode;              // Record, Playback, CatchUp
    public uint CurrentTick;
    public uint TargetTick;
}
```

**System Group**: `TimeSystemGroup` (runs first in `InitializationSystemGroup`)

**Reference**: 
- See [RuntimeLifecycle_TruthSource.md](../../../../Docs/TruthSources/RuntimeLifecycle_TruthSource.md) for execution order
- See [BootstrapSpineGuide.md](BootstrapSpineGuide.md) for bootstrap implementation details

### Registries

**Purpose**: Provide stable GUIDs and cross-ECS identity for entities.

**Registry Types**:
- `VillagerRegistry`: Individual agent tracking
- `FleetRegistry`: Fleet/formation tracking (Space4X)
- `ResourceRegistry`: Resource node tracking
- `MiracleRegistry`: Miracle/event tracking (Godgame)
- `LogisticsRegistry`: Transport route tracking

**Pattern**:
```csharp
// From Runtime/Registry/VillagerRegistry.cs
public struct VillagerRegistryEntry
{
    public AgentGuid Guid;               // Stable GUID
    public Entity Entity;                // Current entity reference
    public float3 LastKnownPosition;
    public uint LastUpdateTick;
}
```

**Reference**: See registry bridge systems in game projects (`GodgameRegistryBridgeSystem`, `Space4XRegistryBridgeSystem`).

### Spatial Grid

**Purpose**: Hierarchical spatial partitioning supporting planetary and galactic scales.

**Key Components**:
```csharp
// From Runtime/Spatial/SpatialGridComponents.cs
public struct SpatialGridConfig : IComponentData
{
    public float3 BoundsCenter;
    public float3 BoundsExtent;
    public float CellSize;
    public int3 CellCounts;
}

public struct SpatialGridState : IComponentData
{
    public int Version;                  // Increments on rebuild
    public uint LastRebuildTick;
}
```

**System Group**: `SpatialSystemGroup` (runs after `EnvironmentSystemGroup`)

**Reference**: See [HierarchicalSpatialGridGuide.md](HierarchicalSpatialGridGuide.md) for integration patterns.

### Communication Bus

**Purpose**: Enable cognition↔reflex↔aggregate coordination without shared references.

**Implementation**: `AgentSyncBus` (managed singleton) with Burst-compatible message queues.

**Sync Intervals**:
- Body→Mind: 100ms (perception events)
- Mind→Body: 250ms (intent commands)

**Reference**: See `Runtime/Bridges/MindToBodySyncSystem.cs` for implementation.

### Rewind System

**Purpose**: Deterministic recovery for long runs, debugging, and replay.

**Key Components**:
```csharp
public struct RewindState : IComponentData
{
    public RewindMode Mode;              // Record, Playback, CatchUp
    public uint CurrentTick;
    public uint TargetTick;
}

public struct HistorySettings : IComponentData
{
    public uint MaxHistoryTicks;        // Snapshot retention
    public uint SnapshotInterval;        // Delta compression
}
```

**Reference**: See [DeterminismGuide.md](DeterminismGuide.md) for rewind-safe patterns.

### Telemetry Stream

**Purpose**: Performance and behavioral metrics for logging, AI analysis, and debug tools.

**Usage**: Systems write metrics to `TelemetryStream` singleton; presentation layer consumes for HUD/debug displays.

**Reference**: See [TelemetryGuide.md](TelemetryGuide.md) for integration.

### Scenario Runner

**Purpose**: Headless scenario execution for CI and automated testing.

**Usage**: JSON scenario definitions → deterministic execution → telemetry export.

**Reference**: See `Runtime/Devtools/ScenarioRunner.cs` for API.

### Economy System

**Purpose**: Economic simulation with ownership, markets, production chains, and legal entities across Body/Mind/Aggregate ECS layers.

**Key Components**:
- `Ownership`: Asset ownership tracking (Body ECS - 60 Hz)
- `Ledger`: Transaction history (Body ECS - 60 Hz)
- `Portfolio`: Entity asset portfolio (Mind ECS - 1 Hz)
- `LegalEntity`: Tax collection, legal structures (Aggregate ECS - 0.2 Hz)
- `AssetSpec`: Production chain definitions

**System Architecture**:
- Body ECS (60 Hz): Purchase/sale events, ownership transfers
- Mind ECS (1 Hz): Portfolio management, investment decisions
- Aggregate ECS (0.2 Hz): Market equilibrium, tax collection, legal entity management

**Reference**: See [OwnershipEconomyGuide.md](OwnershipEconomyGuide.md) for complete integration guide.

---

## 4. Game Side → PureDOTS Interface Patterns

The game layer (MonoBehaviours, UI, scripts) **never** directly mutates simulation state. All interactions flow through PureDOTS interfaces.

### Interface Layers

| Layer | Entry Point | Allowed Actions | Example |
|-------|-------------|-----------------|---------|
| **Game Presentation** | `SimulationBridge` (MonoBehaviour / UI Toolkit) | Read-only: consume simulation output | Display morale heatmap, camera follow |
| **Gameplay Logic** | Scenario API or Command Buffers | Enqueue events → PureDOTS message buffers | `SpawnEntity`, `ApplyModifierEvent`, `FormationCommand` |
| **Scripting / Modding** | JSON scenario or graph-based logic | Data-driven, no direct ECS calls | Define new species or doctrines |
| **Dev / AI Agents** | PureDOTS namespaces & YAML templates | Implement new systems/components | Extend MindECS with new learning nodes |

### Game Presentation Layer

**Pattern**: Read-only consumption of simulation state.

**Example**:
```csharp
// Presentation code (MonoBehaviour, NOT in PureDOTS)
public class MoraleHeatmapDisplay : MonoBehaviour
{
    void Update()
    {
        // Read-only query of simulation state
        var world = World.DefaultGameObjectInjectionWorld;
        var query = world.EntityManager.CreateEntityQuery(typeof(EntityMorale), typeof(LocalTransform));
        
        // Display morale values (no mutation)
        var entities = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            var morale = world.EntityManager.GetComponentData<EntityMorale>(entity);
            var transform = world.EntityManager.GetComponentData<LocalTransform>(entity);
            
            // Render heatmap (presentation only)
            RenderMoraleIndicator(transform.Position, morale.CurrentMorale);
        }
        entities.Dispose();
    }
}
```

**Rule**: Presentation uses **frame-time** (`Time.deltaTime`), not tick-time. See [SimulationPresentationTimeSeparationGuide.md](SimulationPresentationTimeSeparationGuide.md).

### Gameplay Logic Layer

**Pattern**: Enqueue commands via message buffers or command systems.

**Example**:
```csharp
// Gameplay logic enqueues commands
public class PlayerInputHandler : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Enqueue command via PureDOTS command buffer
            var world = World.DefaultGameObjectInjectionWorld;
            var commandSystem = world.GetExistingSystemManaged<CommandBufferSystem>();
            var ecb = commandSystem.CreateCommandBuffer();
            
            // Create spawn command
            ecb.AddComponent(commandSystem.Entity, new SpawnEntityCommand
            {
                PrefabGuid = myPrefabGuid,
                Position = GetMouseWorldPosition(),
                TickNumber = GetCurrentTick()
            });
        }
    }
}
```

**Reference**: See [INTEGRATION_GUIDE.md](../../INTEGRATION_GUIDE.md) for command patterns.

### Scripting / Modding Layer

**Pattern**: Data-driven JSON scenarios, no direct ECS calls.

**Example** (JSON scenario):
```json
{
  "scenario": "unicorn_flight_test",
  "entities": [
    {
      "type": "Unicorn",
      "position": [0, 10, 0],
      "components": {
        "FlightCapability": {
          "LiftForce": 15.0,
          "ManaCost": 2.0
        },
        "SkillProfile": {
          "CastingSkill": 0.8
        }
      }
    }
  ]
}
```

**Reference**: See [ModdingAPIGuide.md](ModdingAPIGuide.md) for modding patterns.

### Dev / AI Agents Layer

**Pattern**: Implement new systems/components using PureDOTS namespaces.

**Example**:
```csharp
// New capability component (agent-implemented)
namespace PureDOTS.Runtime.Capabilities
{
    public struct FlightCapability : IComponentData
    {
        public float LiftForce;
        public float ManaCost;
        public float CurrentAltitude;
    }
}

// System in correct group
[BurstCompile]
[UpdateInGroup(typeof(ReflexSystemGroup))]  // Body ECS - 60 Hz
public partial struct FlightSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Process flight physics at 60 Hz
    }
}
```

**Reference**: See [FoundationGuidelines.md](../../FoundationGuidelines.md) for coding patterns (P0-P25).

---

## 5. Agent Developer Process

When implementing a new feature, follow this workflow to ensure correct layer assignment and integration.

### Step 1: Define the Concept

**Example**: "Unicorn flight with psionic limb-theft capability"

### Step 2: Classify Responsibility

**Decision Tree**:
- **Movement physics** → Body ECS (60 Hz)
- **Strategic choice** → Mind ECS (1 Hz)
- **Cultural or doctrine effect** → Aggregate ECS (0.2 Hz)

**Example Classification**:
- Unicorn flight physics → **Body ECS** (`ReflexSystemGroup`)
- Psionic theft decision-making → **Mind ECS** (`CognitiveSystemGroup`)
- Cultural doctrine about psionic supremacy → **Aggregate ECS** (`TacticalSystemGroup`)

### Step 3: Design Data Components

**Pattern**: Pure data structs implementing `IComponentData` or `IBufferElementData`.

**Example**:
```csharp
// Body ECS component
public struct FlightCapability : IComponentData
{
    public float LiftForce;      // Upward force magnitude
    public float ManaCost;       // Mana per second
    public float CurrentAltitude;
    public float PreferredAltitude;
}

// Mind ECS component
public struct PsionicTheft : IComponentData
{
    public float Range;           // Maximum theft range
    public float SuccessRate;    // Base success probability
    public float ManaRequirement;
}

// Aggregate ECS component
public struct PsionicSupremacyDoctrine : IComponentData
{
    public float AdoptionRate;   // Cultural spread rate
    public float Strength;       // Doctrine influence strength
}
```

### Step 4: Add Systems in Correct Group

**Pattern**: Use `[UpdateInGroup]` annotations to place systems in appropriate groups.

**Example**:
```csharp
// Flight system → Body ECS (60 Hz)
[BurstCompile]
[UpdateInGroup(typeof(ReflexSystemGroup))]
public partial struct FlightSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        
        foreach (var (flight, velocity, transform) in SystemAPI.Query<
            RefRW<FlightCapability>, 
            RefRW<PhysicsVelocity>, 
            RefRO<LocalTransform>>())
        {
            // Apply lift force at 60 Hz
            var lift = math.up() * flight.ValueRO.LiftForce;
            velocity.ValueRW.Linear += lift * deltaTime;
        }
    }
}

// Theft decision system → Mind ECS (1 Hz)
[BurstCompile]
[UpdateInGroup(typeof(CognitiveSystemGroup))]
public partial struct PsionicTheftDecisionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Evaluate theft utility at 1 Hz
        foreach (var (theft, stats, entity) in SystemAPI.Query<
            RefRO<PsionicTheft>, 
            RefRO<CognitiveStats>>()
            .WithEntityAccess())
        {
            // Utility calculation (deterministic math)
            float utility = EvaluateTheftUtility(theft.ValueRO, stats.ValueRO);
            
            if (utility > Threshold)
            {
                // Enqueue intent via message bus
                EnqueueTheftIntent(entity, utility);
            }
        }
    }
}
```

**Reference**: See [SystemGroups.cs](../../Runtime/Systems/SystemGroups.cs) for available groups.

### Step 5: Use Message Bus for Cross-World Effects

**Pattern**: Mind→Body communication via `AgentSyncBus`.

**Example**:
```csharp
// Mind ECS enqueues intent
public struct PerformPsionicTheftEvent : IBufferElementData
{
    public AgentGuid TargetGuid;
    public float SuccessRate;
    public uint TickNumber;
}

// Body ECS processes intent
[BurstCompile]
[UpdateInGroup(typeof(ReflexSystemGroup))]
public partial struct PsionicTheftExecutionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Read intents from message bus (synced by MindToBodySyncSystem)
        foreach (var (intentBuffer, entity) in SystemAPI.Query<
            DynamicBuffer<AgentIntentBuffer>>()
            .WithEntityAccess())
        {
            for (int i = 0; i < intentBuffer.Length; i++)
            {
                var intent = intentBuffer[i];
                if (intent.Kind == AgentIntentKind.PsionicTheft)
                {
                    // Execute theft (Body ECS - immediate)
                    ExecuteTheft(entity, intent.TargetEntity);
                    intentBuffer.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
```

**Reference**: See `Runtime/Bridges/MindToBodySyncSystem.cs` for message bus usage.

### Step 6: Profile and Batch

**Requirements**:
- Profile every new system: **<1 ms per 100k entities**
- Maintain tick segregation (don't mix frequencies)
- Use Burst compilation for hot paths

**Example**:
```csharp
// Profile with Unity Profiler
[BurstCompile]
public partial struct FlightSystem : ISystem
{
    private static readonly ProfilerMarker ProcessFlightMarker = 
        new ProfilerMarker("FlightSystem.ProcessFlight");
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        using (ProcessFlightMarker.Auto())
        {
            // System implementation
        }
    }
}
```

**Reference**: See [PerformanceOptimizationGuide.md](PerformanceOptimizationGuide.md) for profiling patterns.

---

## 6. Data-Based "Physics" for Non-Animated Entities

Even without animation, entities can behave believably through **statistical kinematics** — numeric values processed by physics math kernels.

### Core Attributes

| Attribute | Function | Example Component |
|-----------|----------|-------------------|
| **Facing** | Vector3 orientation | Determines attack arcs / cone targeting |
| **Mass / Momentum** | Numeric inertia | Controls knockback, collisions |
| **Focus / Stamina** | Energy budgets | Limits sustained actions |
| **Arc Offsets** | Numeric directionality | Defines multi-target attacks |

### Example: Attack Arc System

```csharp
// Component definition
public struct AttackArc : IComponentData
{
    public float3 Facing;           // Attack direction
    public float ArcAngle;          // Cone angle (degrees)
    public float Range;             // Maximum range
    public float Damage;            // Base damage
}

// System processing (no animation required)
[BurstCompile]
[UpdateInGroup(typeof(CombatSystemGroup))]
public partial struct AttackArcSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (arc, attackerTransform, attackerEntity) in SystemAPI.Query<
            RefRO<AttackArc>, 
            RefRO<LocalTransform>>()
            .WithEntityAccess())
        {
            var attackerPos = attackerTransform.ValueRO.Position;
            var facing = arc.ValueRO.Facing;
            var range = arc.ValueRO.Range;
            var angle = arc.ValueRO.ArcAngle;
            
            // Query potential targets in spatial grid
            // Calculate angle between facing and target direction
            // Apply damage if within arc
            
            // No animation needed — pure math
        }
    }
}
```

### Example: Focus/Stamina System

```csharp
// Component definition
public struct Focus : IComponentData
{
    public float Current;           // Current focus (0-10)
    public float Max;               // Maximum focus
    public float DecayRate;         // Per-second decay
    public uint LastDecayTick;
}

// System processing
[BurstCompile]
[UpdateInGroup(typeof(ReflexSystemGroup))]
public partial struct FocusDecaySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var currentTick = SystemAPI.GetSingleton<TimeState>().Tick;
        
        foreach (var focus in SystemAPI.Query<RefRW<Focus>>())
        {
            var elapsed = (currentTick - focus.ValueRO.LastDecayTick) * deltaTime;
            focus.ValueRW.Current = math.max(0f, 
                focus.ValueRO.Current - focus.ValueRO.DecayRate * elapsed);
            focus.ValueRW.LastDecayTick = currentTick;
        }
    }
}
```

**Reference**: See [MovementAuthoringGuide.md](MovementAuthoringGuide.md) for movement patterns.

---

## 7. Procedural Mental Models (Cognitive ECS)

Each thinking entity holds cognitive state that drives decision-making through **weighted reasoning graphs** — deterministic utility calculations without behavior trees.

### ProceduralMind Component Structure

```csharp
// Conceptual structure (combines multiple components)
public struct ProceduralMind
{
    // From CognitiveStats
    public float Curiosity;      // Exploration breadth
    public float Fear;           // Risk aversion
    public float Confidence;     // Decision certainty
    public float Creativity;     // Novel solution generation
    
    // From EmotionState
    public float Anger;
    public float Trust;
    
    // From SkillProfile
    public float StrategicThinking;
}
```

**Note**: In practice, these are separate components (`CognitiveStats`, `EmotionState`, `SkillProfile`) that systems query together.

### Weighted Reasoning Graphs

**Pattern**: Cognitive systems evaluate goals through utility calculations.

**Example**: Psionic entity weighing options

```csharp
[BurstCompile]
[UpdateInGroup(typeof(CognitiveSystemGroup))]
public partial struct PsionicDecisionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (theft, stats, emotion, entity) in SystemAPI.Query<
            RefRO<PsionicTheft>, 
            RefRO<CognitiveStats>, 
            RefRO<EmotionState>>()
            .WithEntityAccess())
        {
            // Utility calculation (deterministic Burst math)
            float utilityTheft = EvaluateTheftUtility(theft.ValueRO, stats.ValueRO, emotion.ValueRO);
            float utilityEscape = EvaluateEscapeUtility(stats.ValueRO, emotion.ValueRO);
            float utilityCooperate = EvaluateCooperateUtility(emotion.ValueRO, stats.ValueRO);
            
            // Select highest utility action
            if (utilityTheft > utilityEscape && utilityTheft > utilityCooperate)
            {
                EnqueueIntent(entity, AgentIntentKind.PsionicTheft, utilityTheft);
            }
            else if (utilityEscape > utilityCooperate)
            {
                EnqueueIntent(entity, AgentIntentKind.Escape, utilityEscape);
            }
            else
            {
                EnqueueIntent(entity, AgentIntentKind.Cooperate, utilityCooperate);
            }
        }
    }
    
    [BurstCompile]
    private float EvaluateTheftUtility(
        in PsionicTheft theft, 
        in CognitiveStats stats, 
        in EmotionState emotion)
    {
        // Deterministic utility formula
        float rangeFactor = theft.Range / 100f;  // Normalize
        float successFactor = theft.SuccessRate;
        float confidenceFactor = stats.FocusNormalized;  // Use normalized focus
        
        return rangeFactor * successFactor * confidenceFactor * (1f - emotion.Fear);
    }
    
    [BurstCompile]
    private float EvaluateEscapeUtility(
        in CognitiveStats stats, 
        in EmotionState emotion)
    {
        // Fear-driven escape utility
        return emotion.Fear / math.max(0.1f, stats.FocusNormalized);
    }
    
    [BurstCompile]
    private float EvaluateCooperateUtility(
        in EmotionState emotion, 
        in CognitiveStats stats)
    {
        // Trust and curiosity drive cooperation
        return emotion.Trust * stats.CuriosityNormalized;
    }
}
```

**Key Principle**: All Burst math → deterministic, no behavior trees required.

**Reference**: See [CognitiveArchitectureIntegrationGuide.md](CognitiveArchitectureIntegrationGuide.md) if available.

---

## 8. Social/Cooperation Layer (Aggregate ECS)

The Aggregate ECS manages group-level dynamics: communications, trust networks, cultural doctrines, and group morale.

### Core Components

```csharp
// From Runtime/AI/Social/SocialComponents.cs
[InternalBufferCapacity(8)]
public struct SocialMessage : IBufferElementData
{
    public SocialMessageType Type;      // Offer, Request, Threat, Praise, Inquiry
    public AgentGuid SenderGuid;
    public AgentGuid ReceiverGuid;
    public float Urgency;               // Priority weight (0-1)
    public float Payload;               // Trade value, trust delta, etc.
    public uint TickNumber;
    public float3 ContextPosition;
}

[InternalBufferCapacity(16)]
public struct SocialRelationship : IBufferElementData
{
    public AgentGuid OtherAgentGuid;
    public float Trust;                 // 0-1
    public float Reputation;            // 0-1
    public float CooperationBias;       // -1 to 1
    public uint LastInteractionTick;
}

public struct GroupGoal : IComponentData
{
    public float CooperationWeight;     // 0-1
    public float CompetitionWeight;     // 0-1
    public float ResourcePriority;      // 0-1
    public float ThreatLevel;           // 0-1
    public float GroupCohesion;         // 0-1
    public uint LastEvaluationTick;
}
```

### Communication Patterns

**Pattern**: Agents broadcast experiences; groups adjust doctrines and strategies.

**Example**: Cultural signal propagation

```csharp
[BurstCompile]
[UpdateInGroup(typeof(TacticalSystemGroup))]  // 0.2 Hz
public partial struct CulturalSignalSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Process cultural signals at group level
        foreach (var (signals, groupGoal, entity) in SystemAPI.Query<
            DynamicBuffer<CulturalSignal>, 
            RefRW<GroupGoal>>()
            .WithEntityAccess())
        {
            // Aggregate signal strength
            float totalStrength = 0f;
            for (int i = 0; i < signals.Length; i++)
            {
                totalStrength += signals[i].Strength;
                // Decay signals
                var signal = signals[i];
                signal.Strength *= (1f - signal.Decay);
                signals[i] = signal;
            }
            
            // Update group doctrine weights based on signals
            if (totalStrength > Threshold)
            {
                groupGoal.ValueRW.CooperationWeight += totalStrength * 0.1f;
                groupGoal.ValueRW.CooperationWeight = math.clamp(
                    groupGoal.ValueRW.CooperationWeight, 0f, 1f);
            }
        }
    }
}
```

### Learning Cascades

**Pattern**: Learning cascades upward (societal evolution).

- Individual success → cultural signal broadcast
- Group receives signals → doctrine weight adjustment
- Doctrine weights → bias individual decision-making

**Reference**: 
- See [AggregateECSIntegrationGuide.md](AggregateECSIntegrationGuide.md) for aggregate layer integration patterns
- See [SocialDynamicsIntegrationGuide.md](SocialDynamicsIntegrationGuide.md) for social dynamics, trust networks, and cooperative systems

---

## 9. Extending to "Anything" (Creative Sandbox Principle)

The architecture supports infinite entity variance via **capability composition** — each capability is data + systems registered in the correct ECS group.

### Capability Types

| Capability Type | Example Component | ECS Group | Example |
|----------------|-------------------|-----------|---------|
| **Locomotion** | `FlightCapability`, `Teleportation`, `Burrow` | Body ECS (`ReflexSystemGroup`) | Unicorn flight, psionic teleport |
| **Perception** | `PsionicSense`, `RadarArray`, `SoulSight` | Body ECS (`PerceptionSystemGroup`) | Extended range detection |
| **Interaction** | `ModuleTheft`, `TradeContract`, `SoulHarvest` | Mind ECS (`CognitiveSystemGroup`) | Strategic interactions |
| **Motivation** | `GrudgeTarget`, `Greed`, `Faith`, `Apathy` | Mind ECS (`MotivationSystemGroup`) | Personality-driven behavior |

### Example: Adding Unicorn Flight

**Step 1**: Define capability component

```csharp
namespace PureDOTS.Runtime.Capabilities
{
    public struct FlightCapability : IComponentData
    {
        public float LiftForce;          // Upward force magnitude
        public float ManaCost;           // Mana per second
        public float CurrentAltitude;
        public float PreferredAltitude;
        public float MaxAltitude;
    }
}
```

**Step 2**: Create system in Body ECS

```csharp
[BurstCompile]
[UpdateInGroup(typeof(ReflexSystemGroup))]  // 60 Hz
public partial struct FlightSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        
        foreach (var (flight, velocity, mana) in SystemAPI.Query<
            RefRW<FlightCapability>, 
            RefRW<PhysicsVelocity>, 
            RefRW<Mana>>())
        {
            // Consume mana
            if (mana.ValueRO.Current >= flight.ValueRO.ManaCost * deltaTime)
            {
                mana.ValueRW.Current -= flight.ValueRO.ManaCost * deltaTime;
                
                // Apply lift force
                var lift = math.up() * flight.ValueRO.LiftForce;
                velocity.ValueRW.Linear += lift * deltaTime;
            }
        }
    }
}
```

**Step 3**: Add decision-making in Mind ECS (optional)

```csharp
[BurstCompile]
[UpdateInGroup(typeof(CognitiveSystemGroup))]  // 1 Hz
public partial struct FlightDecisionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Evaluate flight altitude preferences at 1 Hz
        foreach (var (flight, stats) in SystemAPI.Query<
            RefRW<FlightCapability>, 
            RefRO<CognitiveStats>>())
        {
            // Adjust preferred altitude based on cognitive state
            if (stats.ValueRO.Curiosity > 7f)
            {
                // Curious entities prefer higher altitude
                flight.ValueRW.PreferredAltitude = flight.ValueRO.MaxAltitude;
            }
        }
    }
}
```

**No Special Rules**: Same deterministic pipeline scales from peasant to planet-brain.

---

## 10. Performance & Scalability

PureDOTS targets **10M active entities under 16 ms/frame** through frequency segregation and parallelization.

### Frequency & Parallelization Matrix

| Subsystem | Frequency | Parallelization | Notes |
|-----------|-----------|-----------------|-------|
| **Reflex / Physics** | 60 Hz | SIMD per chunk | Millions of entities |
| **Cognitive Updates** | 1 Hz | Batched by archetype | Low cost reasoning |
| **Social / Aggregate** | 0.2 Hz | Chunk-level reductions | Group-level AI |
| **Communication Bus** | Event-driven | Asynchronous jobs | Compress messages |

### System Group Frequencies

**From SystemGroups.cs**:

```csharp
// Body ECS - 60 Hz (every tick)
[UpdateInGroup(typeof(HotPathSystemGroup))]  // Runs every tick
public partial class ReflexSystemGroup : ComponentSystemGroup { }

// Mind ECS - 1 Hz (throttled)
[UpdateInGroup(typeof(CognitiveSystemGroup))]  // Adaptive 0.5-5 Hz
public partial class LearningSystemGroup : ComponentSystemGroup { }

// Aggregate ECS - 0.2 Hz (throttled)
[UpdateInGroup(typeof(CognitiveSystemGroup))]
public partial class MotivationSystemGroup : ComponentSystemGroup { }  // 0.2 Hz

[UpdateInGroup(typeof(GameplaySystemGroup))]
public partial class TacticalSystemGroup : ComponentSystemGroup { }  // 1-5 Hz throttled
```

### Performance Budgets

**Per-System Targets**:
- Reflex systems: **<0.1 ms per 100k entities** (60 Hz)
- Cognitive systems: **<1 ms per 100k entities** (1 Hz)
- Aggregate systems: **<2 ms per 100k entities** (0.2 Hz)

**Profiling Pattern**:
```csharp
[BurstCompile]
public partial struct MySystem : ISystem
{
    private static readonly ProfilerMarker ProcessMarker = 
        new ProfilerMarker("MySystem.Process");
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        using (ProcessMarker.Auto())
        {
            // System implementation
            // Target: <1 ms per 100k entities
        }
    }
}
```

**Reference**: 
- [PerformanceOptimizationGuide.md](PerformanceOptimizationGuide.md)
- [ECS_SanityCheck_Report.md](ECS_SanityCheck_Report.md)

---

## 11. Game Side → Sim Side "Command Path"

All game-side interactions flow through a deterministic command pipeline.

### Command Flow Diagram

```
[Player Input / Script] 
        ↓
   CommandBuffer (Game)
        ↓
   MessageBus (PureDOTS)
        ↓
   ECS SystemGroup (Body/Mind/Aggregate)
        ↓
   Component Mutations
        ↓
   TelemetryStream (Feedback to Game)
```

### Implementation Pattern

**Step 1**: Game layer enqueues command

```csharp
// Game layer (MonoBehaviour)
public class PlayerInputHandler : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var commandSystem = world.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            var ecb = commandSystem.CreateCommandBuffer();
            
            // Enqueue spawn command
            ecb.AddComponent(commandSystem.Entity, new SpawnEntityCommand
            {
                PrefabGuid = myPrefabGuid,
                Position = GetMouseWorldPosition(),
                TickNumber = GetCurrentTick()
            });
        }
    }
}
```

**Step 2**: PureDOTS processes command

```csharp
// PureDOTS system (deterministic)
[BurstCompile]
[UpdateInGroup(typeof(EventSystemGroup))]
public partial struct SpawnEntityCommandSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        foreach (var (command, entity) in SystemAPI.Query<
            RefRO<SpawnEntityCommand>>()
            .WithEntityAccess())
        {
            // Spawn entity deterministically
            var newEntity = SpawnEntity(command.ValueRO.PrefabGuid, command.ValueRO.Position);
            
            // Remove command
            ecb.DestroyEntity(entity);
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

**Step 3**: Telemetry feedback

```csharp
// Telemetry system writes metrics
[BurstCompile]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial struct TelemetrySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var telemetry = SystemAPI.GetSingletonRW<TelemetryStream>();
        
        // Write metrics (consumed by presentation layer)
        telemetry.ValueRW.WriteMetric("EntityCount", GetEntityCount());
    }
}
```

**Key Principle**: All deterministic, replayable, and decoupled — gameplay logic only requests, ECS simulation decides.

**Reference**: See [SimulationPresentationTimeSeparationGuide.md](SimulationPresentationTimeSeparationGuide.md) for time separation patterns.

---

## 12. Practical Example: "Galaxy Brain vs Peasant"

Both entities use **identical systems** — just different data composition.

### Comparison Table

| Aspect | Handled By | Behavior |
|--------|------------|----------|
| **Physical movement** | Body ECS (`ReflexSystemGroup`) | Flight / gravity math |
| **Intelligence & memory** | Mind ECS (`LearningSystemGroup`) | Learns interstellar topology |
| **Communications** | Aggregate ECS (`TacticalSystemGroup`) | Manages empire-level messages |
| **Procedural learning** | Mind ECS (`LearningSystemGroup`) | Refines warp pathways |
| **Cultural doctrine** | Aggregate ECS (`TacticalSystemGroup`) | Spread "Psionic Supremacy" |
| **Rewind / Determinism** | PureDOTS core (`TimeSystemGroup`) | Same outcome each replay |

### Peasant Implementation

```csharp
// Peasant entity components
Entity peasant = EntityManager.CreateEntity(
    typeof(LocalTransform),           // Position
    typeof(PhysicsVelocity),          // Movement
    typeof(VillagerNeeds),            // Hunger, rest
    typeof(SkillProfile),             // Farming skill
    typeof(EmotionState),             // Trust, fear
    typeof(EntityMorale)              // Group morale
);

// Same systems process peasant:
// - ReflexSystemGroup: Movement at 60 Hz
// - LearningSystemGroup: Skill learning at 1 Hz
// - TacticalSystemGroup: Group decisions at 0.2 Hz
```

### Galaxy Brain Implementation

```csharp
// Galaxy brain entity components
Entity galaxyBrain = EntityManager.CreateEntity(
    typeof(LocalTransform),            // Position (galactic scale)
    typeof(PhysicsVelocity),          // Movement (warp)
    typeof(FlightCapability),         // Flight capability
    typeof(PsionicTheft),             // Psionic abilities
    typeof(CognitiveStats),           // Intelligence 10.0
    typeof(SkillProfile),             // Strategic thinking 1.0
    typeof(GroupGoal),                // Empire-level goals
    typeof(CulturalDoctrineReference) // "Psionic Supremacy"
);

// Same systems process galaxy brain:
// - ReflexSystemGroup: Flight physics at 60 Hz
// - LearningSystemGroup: Strategic learning at 1 Hz
// - TacticalSystemGroup: Empire decisions at 0.2 Hz
```

### Key Insight

**Same pipeline, different data composition**. The peasant has fewer components, but uses the same systems. The galaxy brain has more components, but still uses the same systems.

**No special cases** — the architecture scales from individual to galactic scales.

---

## 13. Agent Developer Protocol

Mandatory rules for all PureDOTS development (AI agents and humans).

### Protocol Checklist

- [ ] **Never hardcode logic in MonoBehaviours** — Use PureDOTS ECS for simulation behavior
- [ ] **Use PureDOTS ECS for simulation behavior** — All simulation logic in systems
- [ ] **Communicate only through message buffers** — No direct references between ECS layers
- [ ] **Follow tick domain frequencies** — Body 60 Hz, Mind 1 Hz, Aggregate 0.2 Hz
- [ ] **Profile every new system** — Target: **<1 ms per 100k entities**
- [ ] **Document new components with YAML templates** — Maintain component registry
- [ ] **Reference TRI_PROJECT_BRIEFING.md P0-P25 patterns** — Follow coding standards

### Common Violations

**❌ WRONG**: Hardcoding logic in MonoBehaviour

```csharp
// ❌ DON'T DO THIS
public class VillagerController : MonoBehaviour
{
    void Update()
    {
        // Simulation logic in MonoBehaviour
        transform.position += moveVector * Time.deltaTime;
    }
}
```

**✅ CORRECT**: ECS system handles simulation

```csharp
// ✅ DO THIS
[BurstCompile]
[UpdateInGroup(typeof(ReflexSystemGroup))]
public partial struct VillagerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Simulation logic in ECS system
        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (transform, velocity) in SystemAPI.Query<
            RefRW<LocalTransform>, 
            RefRO<PhysicsVelocity>>())
        {
            transform.ValueRW.Position += velocity.ValueRO.Linear * deltaTime;
        }
    }
}
```

**❌ WRONG**: Direct reference between ECS layers

```csharp
// ❌ DON'T DO THIS
public partial struct MindSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Direct access to Body ECS components
        var bodyQuery = state.GetEntityQuery(typeof(PhysicsVelocity));
        // This breaks layer separation!
    }
}
```

**✅ CORRECT**: Message bus communication

```csharp
// ✅ DO THIS
public partial struct MindSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Enqueue message via AgentSyncBus
        var bus = GetAgentSyncBus();
        bus.EnqueueMindToBody(new MindToBodyMessage
        {
            AgentGuid = myGuid,
            Kind = AgentIntentKind.Move,
            TargetPosition = targetPos
        });
    }
}
```

### Reference Documents

- **Coding Patterns**: [TRI_PROJECT_BRIEFING.md](../../../../TRI_PROJECT_BRIEFING.md) (P0-P25)
- **System Groups**: [RuntimeLifecycle_TruthSource.md](../../../../Docs/TruthSources/RuntimeLifecycle_TruthSource.md)
- **Foundation Guidelines**: [FoundationGuidelines.md](../../FoundationGuidelines.md)
- **Integration Guide**: [INTEGRATION_GUIDE.md](../../INTEGRATION_GUIDE.md)

---

## Summary

This guide provides the architectural foundation for consistent feature development across PureDOTS. Key principles:

1. **Three Pillar Architecture**: Body (60 Hz), Mind (1 Hz), Aggregate (0.2 Hz)
2. **Message Bus Communication**: Layers communicate only through structured messages
3. **Data-Oriented Design**: Components are pure data; systems own behavior
4. **Deterministic Core**: Fixed-step simulation with rewind-safe state
5. **Scalable**: Target 10M+ entities under 16 ms/frame

**Remember**: "Everything is a composition of data and deterministic rules. Imagination is infinite — implementation is consistent."

---

**See Also**:
- [TRI_PROJECT_BRIEFING.md](../../../../TRI_PROJECT_BRIEFING.md) - Project overview
- [RuntimeLifecycle_TruthSource.md](../../../../Docs/TruthSources/RuntimeLifecycle_TruthSource.md) - System execution order
- [FoundationGuidelines.md](../../FoundationGuidelines.md) - Coding standards
- [BootstrapSpineGuide.md](BootstrapSpineGuide.md) - Bootstrap implementation
- [AggregateECSIntegrationGuide.md](AggregateECSIntegrationGuide.md) - Aggregate layer details
- [SocialDynamicsIntegrationGuide.md](SocialDynamicsIntegrationGuide.md) - Social dynamics and cooperation systems
- [OwnershipEconomyGuide.md](OwnershipEconomyGuide.md) - Economic simulation system
- [PerformanceOptimizationGuide.md](PerformanceOptimizationGuide.md) - Performance patterns

