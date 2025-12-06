# Bootstrap Spine Implementation Guide

**Last Updated**: 2025-01-27  
**Purpose**: Guide for agents working with the minimal bootstrap spine system

---

## Overview

The bootstrap spine provides a clean, minimal, predictable startup sequence that brings the simulation to life. It creates all ECS worlds, loads deterministic configuration, registers core systems, and initializes demo scenarios in an explicit order.

## Architecture

```
Bootstrap Flow:
1. Create Worlds (Body, Mind, Aggregate, Presentation)
2. Load Configs (SimConfig from JSON)
3. Register Core Systems (Time, Spatial, Communication, Rewind)
4. Initialize BlobRegistry
5. Wire Communication Buses
6. Load Demo Scenario
7. Start Simulation
```

## Core Components

### WorldUtility

**Location**: `Runtime/Core/WorldUtility.cs`

Creates and manages ECS worlds:

```csharp
// Create a new world
var bodyWorld = WorldUtility.CreateWorld<BodyECSWorld>("BodyWorld");

// Access world registry
var world = WorldUtility.WorldRegistry.GetWorld("BodyWorld");

// Dispose all worlds (cleanup)
WorldUtility.WorldRegistry.DisposeAll();
```

**World Types**:
- `BodyECSWorld` - Unity Entities world for physical simulation
- `MindECSWorld` - DefaultEcs world for cognitive AI
- `AggregateECSWorld` - DefaultEcs world for group AI
- `PresentationECSWorld` - Unity Entities world for rendering

### ConfigLoader

**Location**: `Runtime/Config/ConfigLoader.cs`

Loads JSON configuration files from `StreamingAssets/`:

```csharp
// Load config (returns default if file missing)
var config = ConfigLoader.Load<SimConfig>("Configs/Sim.json");
```

**Config Structure** (`SimConfig`):
```csharp
{
    FixedDeltaTime: float,        // Simulation timestep (default: 0.0166667)
    MindTickRate: float,          // Mind ECS update frequency (default: 1.0 Hz)
    AggregateTickRate: float,     // Aggregate ECS update frequency (default: 5.0 Hz)
    RewindBufferSeconds: int       // Rewind buffer size (default: 300)
}
```

**Default Config Location**: `Assets/StreamingAssets/Configs/Sim.json`

### CoreSystems

**Location**: `Runtime/Core/CoreSystems.cs`

Registers core simulation infrastructure:

```csharp
// Register time spine
CoreSystems.RegisterTimeSpine(world, config.FixedDeltaTime);

// Register spatial grid
var gridConfig = new SpatialGridConfig { /* ... */ };
CoreSystems.RegisterSpatialGrid(world, gridConfig);

// Register communication bus
CoreSystems.RegisterCommunicationBus(world);

// Register rewind system
CoreSystems.RegisterRewind(world, config.RewindBufferSeconds);
```

### BlobRegistry

**Location**: `Runtime/BlobAssets/BlobRegistry.cs`

Centralized blob asset management:

```csharp
// Initialize blob assets (materials, skills, doctrines)
BlobRegistry.Initialize(world);
```

Prevents duplicate blob creation and ensures shared assets are available.

### ScenarioRunner

**Location**: `Runtime/Scenario/ScenarioRunner.cs`

Loads and applies JSON scenarios:

```csharp
// Load scenario from JSON
bool success = ScenarioRunner.LoadScenario(world, "Scenarios/Demo_Awakening.json");
```

**Scenario JSON Format**:
```json
{
  "scenarioId": "Demo_Awakening",
  "seed": 42,
  "runTicks": 3600,
  "entityCounts": [
    { "registryId": "Villager", "count": 5 }
  ],
  "inputCommands": []
}
```

**Scenario Locations**:
- Micro Demo: `Assets/StreamingAssets/Scenarios/Demo_Awakening.json`
- Macro Demo: `Assets/StreamingAssets/Scenarios/Demo_FirstColony.json`

### SimulationRunner

**Location**: `Runtime/Core/SimulationRunner.cs`

Coordinates multi-world simulation execution:

```csharp
// Start all worlds with tick rates
SimulationRunner.StartAllWorlds(
    bodyWorld, 
    mindWorld, 
    aggregateWorld, 
    mindTickRate: 1.0f, 
    aggregateTickRate: 5.0f
);

// Update all worlds (call from Update loop)
SimulationRunner.UpdateAllWorlds(Time.deltaTime);
```

**Note**: Use `SimulationRunnerMono` MonoBehaviour component to automatically call `UpdateAllWorlds` from Unity's Update loop.

### CommunicationBus

**Location**: `Runtime/Bridges/CommunicationBus.cs`

Wires inter-world message buses:

```csharp
// Connect Body world to Mind world
CommunicationBus.Connect(bodyWorld, mindWorld.World, latencySeconds: 0.1f);

// Connect Body world to Aggregate world
CommunicationBus.Connect(bodyWorld, aggregateWorld.World);
```

Uses `AgentSyncBus` for event-driven cross-world communication with bounded queues.

## Bootstrap Customization

### Adding a New World

1. **Create world wrapper type** (if needed):
```csharp
public class MyCustomWorld { }
```

2. **Create world in bootstrap**:
```csharp
var myWorld = WorldUtility.CreateWorld<MyCustomWorld>("MyWorld");
```

3. **Register systems** (if Unity Entities):
```csharp
var systems = SystemRegistry.GetSystems(profile);
DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(myWorld, systems);
```

4. **Wire communication** (if needed):
```csharp
CommunicationBus.Connect(bodyWorld, myWorld);
```

### Creating a New Scenario

1. **Create JSON file** in `Assets/StreamingAssets/Scenarios/`:
```json
{
  "scenarioId": "MyScenario",
  "seed": 123,
  "runTicks": 7200,
  "entityCounts": [
    { "registryId": "Villager", "count": 10 },
    { "registryId": "Storehouse", "count": 2 }
  ],
  "inputCommands": []
}
```

2. **Load in bootstrap**:
```csharp
ScenarioRunner.LoadScenario(bodyWorld, "Scenarios/MyScenario.json");
```

### Extending CoreSystems

Add new registration methods to `CoreSystems.cs`:

```csharp
public static void RegisterMySystem(World world, MyConfig config)
{
    // Initialize your system
    var entityManager = world.EntityManager;
    // ... setup logic
}
```

Call from bootstrap after core systems are registered.

## Telemetry Overlay

**Location**: `Runtime/Telemetry/TelemetryOverlay.cs`

Minimal debug overlay showing:
- Entity counts
- System performance (when available)

**Usage**:
1. Add `TelemetryOverlay` component to a GameObject
2. Configure display position and font size
3. Overlay shows automatically when `showOverlay = true`

## Demo Scenarios

### Micro Demo ("Awakening")

**Focus**: Entity cognition, reflexes, learning loops  
**Worlds Active**: Body + Mind  
**File**: `Scenarios/Demo_Awakening.json`

Minimal scenario for validating individual agent behavior.

### Macro Demo ("First Colony")

**Focus**: Group AI, economy, ownership  
**Worlds Active**: Body + Aggregate  
**File**: `Scenarios/Demo_FirstColony.json`

Scenario for validating aggregate systems and economy.

## Incremental Feature Path

When adding new features, follow this order:

1. **Phase 1 - Cognition & Communication**: Validate entity learning & message passing
2. **Phase 2 - Ownership & Economy**: Test Ledger + Asset chains
3. **Phase 3 - Social Dynamics**: Verify cooperation and trust
4. **Phase 4 - Combat & Formations**: Integrate tactics and morale decay
5. **Phase 5 - Macro AI & Culture**: Activate Aggregate doctrines

Each phase should pass:
- Compile verification (no warnings, no managed allocations)
- Determinism test (replay 100 ticks forward/backward, hashes match)
- Performance sweep (100k entities, <10ms total frame cost)

## Troubleshooting

### Worlds Not Initializing

- Check that `AgentSyncBridgeCoordinator` exists in Body world
- Verify `AgentSyncBus` is created before Mind/Aggregate initialization
- Ensure config file exists or defaults are used

### Scenarios Not Loading

- Verify JSON file is in `StreamingAssets/Scenarios/`
- Check JSON format matches `ScenarioDefinitionData` structure
- Ensure registry IDs match existing registries

### Communication Not Working

- Verify `CommunicationBus.Connect()` is called after worlds are created
- Check `AgentSyncBus` is initialized before connecting
- Ensure worlds are not disposed before communication setup

## API Reference

### WorldUtility

```csharp
public static class WorldUtility
{
    public static World CreateWorld<TWorldType>(string name);
    
    public static class WorldRegistry
    {
        public static void RegisterWorld(string name, World world);
        public static World GetWorld(string name);
        public static void DisposeAll();
    }
}
```

### ConfigLoader

```csharp
public static class ConfigLoader
{
    public static T Load<T>(string path) where T : struct;
}
```

### CoreSystems

```csharp
public static class CoreSystems
{
    public static void RegisterTimeSpine(World world, float fixedDeltaTime);
    public static void RegisterSpatialGrid(World world, SpatialGridConfig gridSpec);
    public static void RegisterCommunicationBus(World world);
    public static void RegisterRewind(World world, int bufferSeconds);
}
```

### ScenarioRunner

```csharp
public static class ScenarioRunner
{
    public static bool LoadScenario(World world, string path);
}
```

### SimulationRunner

```csharp
public static class SimulationRunner
{
    public static void StartAllWorlds(
        World bodyWorld, 
        MindECSWorld mindWorld, 
        AggregateECSWorld aggregateWorld, 
        float mindTickRate, 
        float aggregateTickRate
    );
    
    public static void UpdateAllWorlds(float deltaTime);
}
```

---

## See Also

- `RuntimeLifecycle_TruthSource.md` - Runtime lifecycle documentation
- `MultiECS_Integration_Guide.md` - Multi-ECS communication patterns
- `ScenarioRunner` API documentation

