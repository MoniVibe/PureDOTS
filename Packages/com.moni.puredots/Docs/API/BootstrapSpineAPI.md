# Bootstrap Spine API Reference

**Last Updated**: 2025-01-27

Quick reference for bootstrap spine APIs.

## WorldUtility

### CreateWorld<TWorldType>

Creates a Unity Entities world and registers it.

```csharp
World CreateWorld<TWorldType>(string name)
```

**Parameters**:
- `TWorldType`: World wrapper type (`BodyECSWorld`, `MindECSWorld`, etc.)
- `name`: World name

**Returns**: Created `World` instance

**Example**:
```csharp
var bodyWorld = WorldUtility.CreateWorld<BodyECSWorld>("BodyWorld");
```

### WorldRegistry

Static registry for tracking active worlds.

#### RegisterWorld

```csharp
void RegisterWorld(string name, World world)
```

#### GetWorld

```csharp
World GetWorld(string name)
```

**Returns**: Registered world or `null` if not found

#### DisposeAll

```csharp
void DisposeAll()
```

Disposes all registered worlds and clears registry.

## ConfigLoader

### Load<T>

Loads JSON config from `StreamingAssets/` path.

```csharp
T Load<T>(string path) where T : struct
```

**Parameters**:
- `T`: Config struct type (must be serializable)
- `path`: Relative path from `StreamingAssets/` (e.g., `"Configs/Sim.json"`)

**Returns**: Loaded config or `default(T)` if file missing/invalid

**Example**:
```csharp
var config = ConfigLoader.Load<SimConfig>("Configs/Sim.json");
if (config.FixedDeltaTime <= 0f)
{
    config = SimConfig.Default; // Use defaults
}
```

## CoreSystems

### RegisterTimeSpine

Registers time system with fixed delta time.

```csharp
void RegisterTimeSpine(World world, float fixedDeltaTime)
```

**Parameters**:
- `world`: Unity Entities world
- `fixedDeltaTime`: Simulation timestep (e.g., 0.0166667 for 60Hz)

### RegisterSpatialGrid

Registers spatial grid system.

```csharp
void RegisterSpatialGrid(World world, SpatialGridConfig gridSpec)
```

**Parameters**:
- `world`: Unity Entities world
- `gridSpec`: Spatial grid configuration

### RegisterCommunicationBus

Registers AgentSyncBus for inter-world communication.

```csharp
void RegisterCommunicationBus(World world)
```

**Parameters**:
- `world`: Unity Entities world (must have `AgentSyncBridgeCoordinator`)

### RegisterRewind

Registers rewind system with buffer size.

```csharp
void RegisterRewind(World world, int bufferSeconds)
```

**Parameters**:
- `world`: Unity Entities world
- `bufferSeconds`: Rewind buffer size in seconds

## BlobRegistry

### Initialize

Initializes blob asset registry.

```csharp
void Initialize(World world)
```

**Parameters**:
- `world`: Unity Entities world

**Note**: Prevents duplicate blob creation. Safe to call multiple times.

## ScenarioRunner

### LoadScenario

Loads and applies scenario from JSON file.

```csharp
bool LoadScenario(World world, string path)
```

**Parameters**:
- `world`: Unity Entities world to apply scenario to
- `path`: Relative path from `StreamingAssets/` (e.g., `"Scenarios/Demo_Awakening.json"`)

**Returns**: `true` if scenario loaded successfully, `false` otherwise

**Example**:
```csharp
if (!ScenarioRunner.LoadScenario(bodyWorld, "Scenarios/Demo_Awakening.json"))
{
    Debug.LogError("Failed to load scenario");
}
```

## SimulationRunner

### StartAllWorlds

Starts simulation for all worlds with configurable tick rates.

```csharp
void StartAllWorlds(
    World bodyWorld, 
    MindECSWorld mindWorld, 
    AggregateECSWorld aggregateWorld, 
    float mindTickRate, 
    float aggregateTickRate
)
```

**Parameters**:
- `bodyWorld`: Unity Entities world (updates every frame)
- `mindWorld`: Mind ECS world instance
- `aggregateWorld`: Aggregate ECS world instance
- `mindTickRate`: Mind world update frequency (Hz)
- `aggregateTickRate`: Aggregate world update frequency (Hz)

**Note**: Body world updates via Unity's PlayerLoop. Mind and Aggregate update at specified rates.

### UpdateAllWorlds

Updates Mind and Aggregate worlds based on tick rates. Call from Unity's Update loop.

```csharp
void UpdateAllWorlds(float deltaTime)
```

**Parameters**:
- `deltaTime`: Frame delta time from `Time.deltaTime`

**Example**:
```csharp
// In MonoBehaviour.Update()
SimulationRunner.UpdateAllWorlds(Time.deltaTime);
```

**Note**: Use `SimulationRunnerMono` component for automatic updates.

## CommunicationBus

### Connect

Connects two worlds via message bus.

```csharp
void Connect(World fromWorld, World toWorld, float latencySeconds = 0.1f)
```

**Parameters**:
- `fromWorld`: Source Unity Entities world
- `toWorld`: Target DefaultEcs world
- `latencySeconds`: Message latency in seconds (default: 0.1s)

**Example**:
```csharp
CommunicationBus.Connect(bodyWorld, mindWorld.World);
CommunicationBus.Connect(bodyWorld, aggregateWorld.World);
```

## SimConfig

Configuration structure for simulation settings.

```csharp
public struct SimConfig
{
    public float FixedDeltaTime;        // Simulation timestep
    public float MindTickRate;          // Mind ECS frequency (Hz)
    public float AggregateTickRate;     // Aggregate ECS frequency (Hz)
    public int RewindBufferSeconds;     // Rewind buffer size
}
```

### Default

```csharp
SimConfig Default
```

Returns default configuration:
- `FixedDeltaTime`: 0.0166667 (60Hz)
- `MindTickRate`: 1.0 Hz
- `AggregateTickRate`: 5.0 Hz
- `RewindBufferSeconds`: 300

## TelemetryOverlay

MonoBehaviour component for displaying telemetry overlay.

### Properties

- `showOverlay` (bool): Enable/disable overlay
- `position` (Vector2): Screen position in pixels
- `fontSize` (int): Font size for text

### Usage

1. Add component to GameObject
2. Configure properties in inspector
3. Overlay displays automatically when `showOverlay = true`

---

## Example: Custom Bootstrap Extension

```csharp
private static void InitializeRuntime()
{
    // 1. Create worlds
    var bodyWorld = WorldUtility.CreateWorld<BodyECSWorld>("BodyWorld");
    
    // 2. Load configs
    var config = ConfigLoader.Load<SimConfig>("Configs/Sim.json");
    if (config.FixedDeltaTime <= 0f) config = SimConfig.Default;
    
    // 3. Register cores
    CoreSystems.RegisterTimeSpine(bodyWorld, config.FixedDeltaTime);
    CoreSystems.RegisterSpatialGrid(bodyWorld, CreateGridConfig());
    CoreSystems.RegisterCommunicationBus(bodyWorld);
    CoreSystems.RegisterRewind(bodyWorld, config.RewindBufferSeconds);
    
    // 4. Initialize blobs
    BlobRegistry.Initialize(bodyWorld);
    
    // 5. Initialize other worlds
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

