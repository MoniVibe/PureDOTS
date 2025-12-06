# Bootstrap Spine Quick Start

**Last Updated**: 2025-01-27

Quick reference for using the bootstrap spine system.

## Minimal Example

```csharp
using PureDOTS.Core;
using PureDOTS.Config;
using PureDOTS.Runtime.BlobAssets;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Scenario;
using PureDOTS.AI.MindECS;
using PureDOTS.AI.AggregateECS;

private static void InitializeRuntime()
{
    // 1. Create Body world
    var bodyWorld = WorldUtility.CreateWorld<BodyECSWorld>("BodyWorld");
    World.DefaultGameObjectInjectionWorld = bodyWorld;
    
    // 2. Load config
    var config = ConfigLoader.Load<SimConfig>("Configs/Sim.json");
    if (config.FixedDeltaTime <= 0f) config = SimConfig.Default;
    
    // 3. Register core systems
    CoreSystems.RegisterTimeSpine(bodyWorld, config.FixedDeltaTime);
    CoreSystems.RegisterSpatialGrid(bodyWorld, CreateDefaultGrid());
    CoreSystems.RegisterCommunicationBus(bodyWorld);
    CoreSystems.RegisterRewind(bodyWorld, config.RewindBufferSeconds);
    
    // 4. Initialize blobs
    BlobRegistry.Initialize(bodyWorld);
    
    // 5. Initialize Mind/Aggregate worlds
    var bus = AgentSyncBridgeCoordinator.GetBusFromWorld(bodyWorld);
    MindECSWorld.Initialize(bus);
    AggregateECSWorld.Initialize(bus);
    
    // 6. Wire communication
    CommunicationBus.Connect(bodyWorld, MindECSWorld.Instance.World);
    CommunicationBus.Connect(bodyWorld, AggregateECSWorld.Instance.World);
    
    // 7. Load scenario
    ScenarioRunner.LoadScenario(bodyWorld, "Scenarios/Demo_Awakening.json");
    
    // 8. Start simulation
    SimulationRunner.StartAllWorlds(
        bodyWorld, 
        MindECSWorld.Instance, 
        AggregateECSWorld.Instance, 
        config.MindTickRate, 
        config.AggregateTickRate
    );
}
```

## Config File

Create `Assets/StreamingAssets/Configs/Sim.json`:

```json
{
  "FixedDeltaTime": 0.0166667,
  "MindTickRate": 1.0,
  "AggregateTickRate": 5.0,
  "RewindBufferSeconds": 300
}
```

## Scenario File

Create `Assets/StreamingAssets/Scenarios/Demo_Awakening.json`:

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

## Adding SimulationRunner Update

Add `SimulationRunnerMono` component to a GameObject, or call manually:

```csharp
void Update()
{
    SimulationRunner.UpdateAllWorlds(Time.deltaTime);
}
```

## Common Patterns

### Extend Bootstrap

```csharp
// After core systems registered
CoreSystems.RegisterTimeSpine(bodyWorld, config.FixedDeltaTime);
// ... add your custom registration here
MyCustomSystem.Register(bodyWorld, myConfig);
```

### Add New World

```csharp
var myWorld = WorldUtility.CreateWorld<MyWorldType>("MyWorld");
// Register systems, wire communication, etc.
```

### Load Different Scenario

```csharp
// Replace default scenario
ScenarioRunner.LoadScenario(bodyWorld, "Scenarios/MyCustomScenario.json");
```

## See Also

- `BootstrapSpineGuide.md` - Complete guide
- `BootstrapSpineAPI.md` - Full API reference

