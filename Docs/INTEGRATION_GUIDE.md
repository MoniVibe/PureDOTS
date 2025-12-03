# PureDOTS Integration Guide

Quick reference for developing features in PureDOTS and integrating with games.

## Core Principles

### Game Agnosticism

PureDOTS must remain **game-agnostic**. All game-specific logic belongs in game projects.

**Rules:**
- Never hardcode game-specific types (e.g., `MinerVessel`, `Carrier`) in PureDOTS
- Use generic tags and configurable data (e.g., `TransportUnitTag`, `LessonId` fields)
- Never use conditional compilation (`#if SPACE4X`, `#if GODGAME`) for game logic
- Provide extension points (enums, config fields, tag components) for games to customize

**Example:**
```csharp
// ❌ BAD: Game-specific type in PureDOTS
if (entityManager.HasComponent<MinerVessel>(entity)) { ... }

// ✅ GOOD: Generic tag in PureDOTS
if (entityManager.HasComponent<TransportUnitTag>(entity)) { ... }
```

### Determinism & Rewind Safety

All PureDOTS systems must be **deterministic** and **rewind-safe**.

**Pattern:**
```csharp
[BurstCompile]
public partial struct MySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rewindState = SystemAPI.GetSingleton<RewindState>();
        if (rewindState.Mode != RewindMode.Record)
            return; // Skip during playback
        
        // ... deterministic logic ...
    }
}
```

### Pure DOTS Architecture

PureDOTS uses **pure DOTS patterns** - no MonoBehaviour service locators.

**Registry Pattern:**
```csharp
// Singleton component
public struct MyRegistry : IComponentData { }

// Buffer of entries
public struct MyEntry : IBufferElementData
{
    public Entity Entity;
    public int Index;
}

// System rebuilds registry each tick
[UpdateInGroup(typeof(MySystemGroup))]
public partial struct MyRegistrySystem : ISystem
{
    // Rebuilds buffer each tick
}
```

### Burst & IL2CPP Safety

- Use `[BurstCompile]` on systems and jobs
- Avoid reflection in jobs
- Use `FixedString` types instead of `string`
- Use `BlobAssetReference<T>` for large read-only data
- Avoid managed types in component data

---

## Registry Pattern

PureDOTS uses **registries** (singleton + buffer pattern) for entity collections.

**Available Registries:**
- `ResourceRegistry` + `DynamicBuffer<ResourceEntry>`
- `VillagerRegistry` + `DynamicBuffer<VillagerEntry>`
- `StorehouseRegistry` + `DynamicBuffer<StorehouseEntry>`
- `MiracleRegistry` + `DynamicBuffer<MiracleEntry>`
- `LogisticsRequestRegistry` + `DynamicBuffer<LogisticsRequestEntry>`
- `ConstructionRegistry` + `DynamicBuffer<ConstructionEntry>`

**How Entities Register:**
1. Add appropriate component (e.g., `ResourceSourceConfig`, `VillagerId`)
2. Registry system automatically picks up entity
3. Entry added to registry buffer each tick

**Querying Registries:**
```csharp
var registryEntity = SystemAPI.GetSingletonEntity<ResourceRegistry>();
var entries = SystemAPI.GetBuffer<ResourceEntry>(registryEntity);
for (int i = 0; i < entries.Length; i++)
{
    var entry = entries[i];
    // Use entry.Entity, entry.Index, etc.
}
```

---

## Time/Rewind Integration

PureDOTS provides unified time controls: `TimeState`, `TickTimeState`, `RewindState`, `TimeControlCommand`.

**Time State Components:**
- `TimeState`: Frame-time state (for presentation)
- `TickTimeState`: Deterministic tick state (tick count, timescale, paused)
- `RewindState`: Rewind mode and target tick

**Rewind Guard Pattern:**
```csharp
[BurstCompile]
public partial struct MySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rewindState = SystemAPI.GetSingleton<RewindState>();
        if (rewindState.Mode != RewindMode.Record)
            return; // Skip during playback
        
        // Mutate state here
    }
}
```

**Time Control Commands:**
```csharp
// Pause/play
var cmd = new TimeControlCommand { Action = TimeControlAction.Pause };
SystemAPI.GetSingletonRW<TimeControlCommandBuffer>().ValueRW.Commands.Add(cmd);

// Rewind
var rewindCmd = new TimeControlCommand 
{ 
    Action = TimeControlAction.Rewind, 
    TargetTick = 100 
};
```

---

## Component Design Patterns

### Tag Components

Empty structs implementing `IComponentData` to mark entities.

**Common Tags:**
- `TransportUnitTag` - Marks transport entities
- `VillagerId` - Marks villager entities
- `MiracleDefinition` - Marks miracle entities

**Usage:**
```csharp
using PureDOTS.Runtime.Mobility;

entityManager.AddComponent<TransportUnitTag>(shipEntity);
```

### Configuration Components

Data-driven configuration via component fields.

**Pattern:**
```csharp
public struct ResourceSourceConfig : IComponentData
{
    public float GatherRatePerWorker;
    public byte MaxSimultaneousWorkers;
    public float RespawnSeconds;
    public FixedString64Bytes LessonId; // Configurable ID
    public byte Flags;
}
```

**Best Practices:**
- Use `FixedString64Bytes` for IDs
- Use value types (float, int, byte) for data
- Empty IDs = "no effect" (e.g., empty `LessonId` = no learning)

### Blittable Types

All component data must be blittable for Burst compatibility.

**Allowed:**
- Value types: `int`, `float`, `byte`, `bool`, `Entity`
- Structs containing only value types
- `FixedString` types
- `BlobAssetReference<T>`

**Not Allowed:**
- `string` (use `FixedString`)
- Managed types (`List<T>`, `Dictionary<K,V>`)
- Reference types

---

## System Integration Points

### System Groups

PureDOTS organizes systems into groups. Use appropriate groups for game systems.

**Root Groups:**
- `InitializationSystemGroup` - Core singleton seeding
- `FixedStepSimulationSystemGroup` - Deterministic 60 FPS simulation
- `SimulationSystemGroup` - Variable-rate simulation
- `PresentationSystemGroup` - Visual updates

**Domain Groups:**
- `TimeSystemGroup` - Time state, tick advancement, rewind
- `VillagerSystemGroup` - Villager AI, needs, jobs, movement
- `ResourceSystemGroup` - Resource gathering, processing, storage
- `EnvironmentSystemGroup` - Climate, moisture, temperature
- `SpatialSystemGroup` - Spatial grid rebuilds, queries
- `AISystemGroup` - Shared AI pipeline (sensors, utility, steering)
- `HandSystemGroup` - Divine hand input, cursor, miracles
- `ConstructionSystemGroup` - Building sites, progress
- `CombatSystemGroup` - Combat resolution

**Using System Groups:**
```csharp
[UpdateInGroup(typeof(VillagerSystemGroup))]
[UpdateAfter(typeof(VillagerNeedsSystem))]
public partial struct MyVillagerSystem : ISystem
{
    // Runs in VillagerSystemGroup after VillagerNeedsSystem
}
```

### Component Lookups

Use `ComponentLookup<T>` for efficient component queries.

**Pattern:**
```csharp
[BurstCompile]
public partial struct MyJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<MyConfig> ConfigLookup;
    public ComponentLookup<MyState> StateLookup;
    
    public void Execute(Entity entity, ref MyComponent comp)
    {
        if (!ConfigLookup.HasComponent(entity))
            return;
        
        var config = ConfigLookup[entity];
        var state = StateLookup[entity];
        // ... logic ...
    }
}
```

---

## Spatial Grid Integration

Many PureDOTS systems rely on the **spatial grid** for proximity queries.

**Requirements:**
- Entities must have `LocalTransform` component (for position)
- `SpatialGridResidency` component added automatically by spatial systems

**Querying Spatial Grid:**
```csharp
var queryHelper = SystemAPI.GetSingleton<SpatialQueryHelper>();
var results = new NativeList<Entity>(Allocator.Temp);
queryHelper.QueryRadius(position, radius, ref results);
// Use results...
results.Dispose();
```

**When to Use:**
- **Spatial queries**: Position/radius-based searches (AI sensors, targeting)
- **Registry queries**: Type/category-based searches (job assignment, resource gathering)
- **Combined**: Spatial query to narrow candidates, then filter by registry/component

---

## AI Pipeline Integration

PureDOTS provides a **shared AI pipeline** consumable by any entity type.

**Pipeline Stages:**
1. **Sensors** (`AISensorUpdateSystem`) - Samples spatial grid, categorizes entities
2. **Scoring** (`AIUtilityScoringSystem`) - Evaluates actions using utility curves
3. **Steering** (`AISteeringSystem`) - Computes movement direction
4. **Task Resolution** (`AITaskResolutionSystem`) - Emits commands to domain systems

**Opting Into AI Pipeline:**
```csharp
// Add sensor config
entityManager.AddComponent(entity, new AISensorConfig
{
    UpdateInterval = 0.5f,
    Range = 10f,
    MaxResults = 8,
    PrimaryCategory = AISensorCategory.ResourceNode
});

// Add sensor state and buffer
entityManager.AddComponent(entity, new AISensorState());
entityManager.AddBuffer<AISensorReading>(entity);

// Add behaviour archetype (blob baked from ScriptableObject)
entityManager.AddComponent(entity, new AIBehaviourArchetype
{
    UtilityBlob = utilityBlobReference
});

// Add utility state
entityManager.AddComponent(entity, new AIUtilityState());

// Add steering config and state
entityManager.AddComponent(entity, new AISteeringConfig
{
    MaxSpeed = 5f,
    Acceleration = 10f,
    Responsiveness = 0.1f,
    DegreesOfFreedom = 2
});
entityManager.AddComponent(entity, new AISteeringState());
entityManager.AddComponent(entity, new AITargetState());
```

**AI Sensor Categories:**
- `Villager` - Villager entities
- `ResourceNode` - Resource source entities
- `Storehouse` - Storage building entities
- `TransportUnit` - Transport entities (ships, wagons)
- `Miracle` - Miracle entities
- `Custom0`-`Custom15` - Reserved for game-specific categories

---

## Performance Budgets

### Entity Counts

- **Target**: 50k-100k complex entities per world
- **Registries**: Rebuild each tick (acceptable for <10k entries)
- **Spatial Grid**: Batch queries per sector

### Burst Compatibility

- **All hot paths**: Burst-compiled
- **No managed allocations**: In simulation systems
- **String constants**: Pre-defined as `static readonly FixedString`

**Pattern:**
```csharp
// ❌ WRONG: Managed string in Burst
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    var name = new FixedString64Bytes("Hello"); // Fails!
}

// ✅ CORRECT: Pre-defined constant
private static readonly FixedString64Bytes HelloString = "Hello";

[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    var name = HelloString; // Works!
}
```

### Memory Budgets

- **Component data**: Keep under 64 bytes per component when possible
- **Buffers**: Use `DynamicBuffer<T>` for variable-length data
- **Blobs**: Use `BlobAssetReference<T>` for large read-only data

---

## Common Patterns

### Transport Unit Integration

**Adding Transport Tag:**
```csharp
using PureDOTS.Runtime.Mobility;

// In authoring component or system
entityManager.AddComponent<TransportUnitTag>(shipEntity);
```

**Configuring AI Sensors:**
```csharp
var sensorConfig = new AISensorConfig
{
    PrimaryCategory = AISensorCategory.TransportUnit,
    Range = 50f,
    MaxResults = 8
};
```

### Resource Lesson Configuration

**Configuring Lesson ID:**
```csharp
var resourceConfig = new ResourceSourceConfig
{
    GatherRatePerWorker = 10f,
    MaxSimultaneousWorkers = 3,
    RespawnSeconds = 60f,
    LessonId = new FixedString64Bytes("lesson.harvest.iron_ore"),
    Flags = ResourceSourceConfig.FlagRespawns
};
```

**Lesson ID Format:**
- Use consistent naming: `"lesson.harvest.iron_ore"`
- Empty `LessonId` = no learning from this resource

### Adding New Components

**Naming Conventions:**
- `*Tag` - Empty tag components (e.g., `TransportUnitTag`)
- `*Config` - Configuration data (e.g., `ResourceSourceConfig`)
- `*State` - Runtime state (e.g., `AISensorState`)

**Placement:**
- Place in appropriate `Runtime/Runtime/` folder
- Group related components in same file

**Required Attributes:**
```csharp
[BurstCompile] // If used in Burst jobs
[Serializable] // If serialized in authoring
public struct MyComponent : IComponentData
{
    public FixedString64Bytes MyId;
    public float MyValue;
}
```

### Adding New Systems

**System Group Placement:**
```csharp
[BurstCompile]
[UpdateInGroup(typeof(MySystemGroup))]
[UpdateAfter(typeof(OtherSystem))]
public partial struct MySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rewindState = SystemAPI.GetSingleton<RewindState>();
        if (rewindState.Mode != RewindMode.Record)
            return;
        
        // ... logic ...
    }
}
```

---

## Provider & Contract Patterns

> **📘 Recipes**: For step-by-step guides to implementing features, see the [Recipes catalog](Recipes/README.md). Available recipe types:
> - `cross-project-mechanic` - See [Slingshot/Launch Mechanic](ContractFirst_FeatureRecipe.md)
> - `puredots-infra-system` - See [LightSource Registry](Recipes/Recipe_LightSourceRegistry.md)
> - Start from the [Recipe Template](Recipes/Recipe_Template.md) for new features

### Provider Interfaces

PureDOTS uses provider interfaces to allow swapping implementations without modifying core systems.

**Available Providers:**
- `ISpatialGridProvider` - Spatial grid implementations (Hashed, Uniform)
- `IPhysicsProvider` - Physics backend implementations (None, Entities/Unity Physics, Havok/stub)

**Using Providers:**
- Providers are selected via config (e.g., `PhysicsConfig.ProviderId`)
- Games configure providers through adapters, not by modifying PureDOTS code
- See `Docs/Contracts.md` for provider contract specifications

### Contracts

Shared system contracts are documented in `Docs/Contracts.md`. When adding or changing shared systems:

1. Update `Contracts.md` with the contract entry
2. Include: Producer, Consumer, Schema, Notes
3. When breaking changes occur, add versioning note: `- YYYY-MM-DD: v1 → v2 (added field X)`

**Contract Examples:**
- `TimeControlCommand v1` - Time control command schema
- `PhysicsCollisionEvent v1` - Collision event schema
- `PhysicsProvider v1` - Physics provider interface

### Adapter Pattern

Games use adapters to configure and consume PureDOTS contracts without forking framework code.

**Adapter Structure:**
- `Assets/Scripts/<Game>/Adapters/` - One folder per domain (Physics, Time, Events, etc.)
- Adapters select providers via config
- Adapters subscribe to events and translate to game-specific behavior
- Adapters do NOT copy or fork PureDOTS systems

**Example:**
```csharp
// In Godgame/Assets/Scripts/Godgame/Adapters/Physics/GodgamePhysicsAdapter.cs
// Selects provider, subscribes to collision events, translates to game behavior
```

---

## Extension Guidelines

### When to Extend PureDOTS vs. Keep in Game

**Extend PureDOTS when:**
- Feature is **generic** and reusable across multiple games
- Feature provides **infrastructure** (systems, registries, utilities)
- Feature is **game-agnostic** (no game-specific themes/content)

**Keep in Game Project when:**
- Feature is **game-specific** (themes, content, rules)
- Feature is **content** (prefabs, assets, data)
- Feature is **presentation** (visuals, audio, UI)

**Examples:**
- PureDOTS: Generic `TransportUnitTag`, configurable `LessonId` field
- Game Project: Specific ship types (`MinerVessel`), hardcoded lesson mappings

---

## Troubleshooting

### Transport Units Not Detected
- Verify `TransportUnitTag` is added to the entity
- Check `AISensorConfig` includes `AISensorCategory.TransportUnit`
- Ensure entity has `LocalTransform` and is registered in spatial grid

### Lessons Not Learned
- Verify `ResourceSourceConfig.LessonId` is set (not empty)
- Check lesson ID exists in your knowledge system
- Ensure `KnowledgeLessonEffectBlob` is configured correctly

### Burst Compilation Errors
- Check for managed types (`string`, `List<T>`, etc.)
- Verify string constants are pre-defined as `static readonly`
- Ensure no reflection or managed API calls in Burst code

---

## See Also

- `TRI_PROJECT_BRIEFING.md` - Project overview and coding patterns
- `Recipes/README.md` - Recipe catalog and usage guide
- `Recipes/Recipe_Template.md` - Template for creating new recipes
- `ContractFirst_FeatureRecipe.md` - Worked example: slingshot/launch mechanic
- `Contracts.md` - Shared system contract definitions
- `BestPractices/DOTS_1_4_Patterns.md` - DOTS 1.4.x specific patterns
- `BestPractices/BurstOptimization.md` - Burst optimization guidelines
- `BestPractices/ComponentDesignPatterns.md` - Component design patterns

