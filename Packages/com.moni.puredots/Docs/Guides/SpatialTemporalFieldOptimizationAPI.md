# Spatial-Temporal Field Optimization API Reference

**Last Updated**: 2025-01-27  
**Purpose**: Quick API reference for spatial-temporal field optimization systems

---

## Components

### Climate Chunking

```csharp
// ClimateChunkManager (singleton)
public struct ClimateChunkManager : IComponentData
{
    public int ChunkSize;              // Cells per chunk (default: 64)
    public int MaxActiveChunks;         // Max chunks in memory (default: 100)
    public uint SerializationTick;     // Last serialization tick
}

// ClimateChunk (per chunk entity)
public struct ClimateChunk : IComponentData
{
    public int2 ChunkCoord;            // Chunk coordinates
    public BlobAssetReference<ClimateChunkBlob> Blob;
    public byte IsActive;               // 0=serialized, 1=in-memory
    public uint LastAccessTick;        // Last access time
}

// ClimateChunkBlob
public struct ClimateChunkBlob
{
    public BlobArray<half> Temperature; // 64×64×1
    public BlobArray<half> Moisture;
    public BlobArray<half> Oxygen;
}

// ActiveChunkRequest (buffer on ClimateChunkManager)
public struct ActiveChunkRequest : IBufferElementData
{
    public int2 ChunkCoord;
    public uint RequestTick;
}
```

### Temporal LOD

```csharp
// TemporalLODConfig (singleton)
public struct TemporalLODConfig : IComponentData
{
    public uint WindCloudDivisor;      // 1 (every tick)
    public uint TemperatureDivisor;     // 5 (every 5 ticks)
    public uint VegetationDivisor;      // 20 (every 20 ticks)
    public uint FireDivisor;            // 1 (event-driven)
    public uint ClimateFeedbackDivisor; // 5 (every 5 ticks)
}

// Helpers
public static class TemporalLODHelpers
{
    public static bool ShouldUpdate(uint currentTick, uint divisor);
    public static uint GetEffectiveTickDelta(uint currentTick, uint lastUpdateTick, uint divisor);
}
```

### Double-Buffering

```csharp
// DoubleBufferedField
public struct DoubleBufferedField : IComponentData
{
    public byte ReadBufferIndex;       // 0 or 1
    public BlobAssetReference<FieldBufferBlob> Buffer0;
    public BlobAssetReference<FieldBufferBlob> Buffer1;
}

// FieldBufferBlob
public struct FieldBufferBlob
{
    public BlobArray<float> Values;
}
```

### Environment Sampling

```csharp
// EnvironmentSample (per entity)
public struct EnvironmentSample : IComponentData
{
    public half Temperature;          // Degrees Celsius
    public half Moisture;              // 0-100
    public half Oxygen;                // 0-100 percentage
    public half Light;                 // 0-100
    public half SoilFertility;         // 0-100
    public uint LastSampleTick;
}
```

### Fire System

```csharp
// FireGrid (singleton)
public struct FireGrid : IComponentData
{
    public EnvironmentGridMetadata Metadata;
    public BlobAssetReference<FireGridBlob> Blob;
    public float SpreadCoefficient;    // Fire spread rate
    public float RainCoefficient;      // Rain suppression rate
    public uint LastUpdateTick;
}

// FireGridBlob
public struct FireGridBlob
{
    public BlobArray<float> Heat;      // Heat value per cell
    public BlobArray<byte> ActiveFire;  // 0/1 flag
}
```

### Cloud System

```csharp
// CloudGrid (singleton)
public struct CloudGrid : IComponentData
{
    public EnvironmentGridMetadata Metadata;
    public BlobAssetReference<CloudGridBlob> Blob;
    public float CondensationThreshold; // Moisture threshold for clouds
    public uint LastUpdateTick;
}

// CloudGridBlob
public struct CloudGridBlob
{
    public BlobArray<float> Moisture;      // Atmospheric moisture
    public BlobArray<float> UpwardVelocity; // Upward motion
    public BlobArray<float> RainRate;       // Rain rate per cell
}
```

### Vegetation Sampling

```csharp
// FloraState (per vegetation entity)
public struct FloraState : IComponentData
{
    public byte Stage;                  // 0=seed, 1=grow, 2=mature, 3=decay
    public half Energy;                 // from light+nutrients
    public half MoistureNeed;           // required moisture
}

// VegetationPatch (per patch entity)
public struct VegetationPatch : IComponentData
{
    public int2 PatchCoord;            // Patch coordinates
    public Entity RepresentativeEntity;  // Sample entity
    public byte SampleCount;            // Plants in patch
}
```

### Biome ECS

```csharp
// BiomeEntity (per biome entity)
public struct BiomeEntity : IComponentData
{
    public BiomeType Type;
    public int2 BiomeCoord;
}

// BiomeChunkBuffer (buffer on BiomeEntity)
public struct BiomeChunkBuffer : IBufferElementData
{
    public int2 ChunkCoord;
    public BlobAssetReference<ClimateChunkBlob> ChunkBlob;
}

// BiomeTelemetry (per biome entity)
public struct BiomeTelemetry : IComponentData
{
    public float AverageTemperature;
    public float AverageMoisture;
    public int FloraSampleCount;
    public float WeatherIntensity;
    public uint LastUpdateTick;
}
```

### AI Goal Optimization

```csharp
// GoalCache (per AI entity)
public struct GoalCache : IComponentData
{
    public FixedString64Bytes LastContextKey;
    public byte LastActionIndex;
    public uint LastEvaluationTick;
}

// SpatialDesireGrid (singleton)
public struct SpatialDesireGrid : IComponentData
{
    public EnvironmentGridMetadata Metadata;
    public BlobAssetReference<DesireGridBlob> Blob;
}

// EnvironmentalPreference (per AI entity)
public struct EnvironmentalPreference : IComponentData
{
    public half PreferredTemperature;
    public half PreferredMoisture;
    public half PreferredLight;
    public half HumidityPenalty;        // Desert fauna
    public half LightPenalty;           // Cave flora
}

// AmbitionGoal (per AI entity)
public struct AmbitionGoal : IComponentData
{
    public AmbitionType Type;           // ExpandFarm, SeekShade, Migrate
    public float3 TargetZone;
    public float Priority;
    public uint IssuedTick;
}

public enum AmbitionType : byte
{
    None = 0,
    ExpandFarm = 1,
    SeekShade = 2,
    Migrate = 3
}
```

### Performance Budget

```csharp
// EnvironmentPerformanceBudget (singleton)
public struct EnvironmentPerformanceBudget : IComponentData
{
    public float VegetationGrowthBudget; // ≤ 0.5 ms
    public float FireSpreadBudget;       // ≤ 1.0 ms
    public float ClimateGridBudget;      // ≤ 2.0 ms
    public float WindCloudBudget;        // ≤ 1.0 ms
    public float TotalBudget;            // ≤ 5.0 ms
}
```

---

## Systems

### ClimateChunkManagerSystem

**Group**: `EnvironmentSystemGroup`  
**Order**: `UpdateBefore(typeof(MoistureEvaporationSystem))`

Manages active chunk set, loads/unloads chunks based on spatial queries, serializes inactive chunks.

**Key Behaviors**:
- Processes `ActiveChunkRequest` buffer
- Loads chunks into memory (max `MaxActiveChunks`)
- Serializes inactive chunks every 60 ticks
- Marks chunks as active/inactive based on access time

### TemporalLODSystem

**Group**: `EnvironmentSystemGroup`  
**Order**: `UpdateBefore(typeof(MoistureEvaporationSystem))`

Initializes temporal LOD configuration singleton with defaults.

**Key Behaviors**:
- Creates `TemporalLODConfig` if missing
- Sets default divisors (WindCloud: 1, Temperature: 5, Vegetation: 20, Fire: 1, ClimateFeedback: 5)

### FieldPropagationSystem

**Group**: `EnvironmentSystemGroup`  
**Order**: `UpdateAfter(typeof(MoistureSeepageSystem))`

Unified field propagation using Burst-compiled convolution kernels.

**Key Behaviors**:
- Applies 3×3 Laplacian stencil for diffusion
- Wind-driven advection via upwind differencing
- Processes `MoistureGrid` and `TemperatureGrid`
- Respects temporal LOD (`TemperatureDivisor`)

### ClimateFeedbackSystem

**Group**: `EnvironmentSystemGroup`  
**Order**: `UpdateAfter(typeof(FieldPropagationSystem))`

Unified atmospheric feedback loop converging all fields atomically.

**Key Behaviors**:
- Updates temperature: `sunIrradiance - evaporationCooling`
- Updates moisture: `rainfall - evaporation`
- Updates oxygen: `photosynthesis - combustion`
- Uses `ClimateProfileData` coefficients
- Respects temporal LOD (`ClimateFeedbackDivisor`)

### FirePropagationSystem

**Group**: `EnvironmentSystemGroup`  
**Order**: `UpdateAfter(typeof(ClimateFeedbackSystem))`

Event-driven fire propagation with wind and rain interaction.

**Key Behaviors**:
- Only processes cells near active fire
- Wind-driven spread: `spreadCoeff * max(0, dot(windDir, cellDir)) * dt`
- Rain suppression: `-rainCoeff * rain * dt`
- Links vegetation energy → fire fuel
- Updates every tick (high reactivity)

### WindCloudSystem

**Group**: `EnvironmentSystemGroup`  
**Order**: `UpdateAfter(typeof(ClimateStateUpdateSystem))`

Wind and cloud system with mass-conserving advection.

**Key Behaviors**:
- Updates wind field (1 Hz)
- Cloud advection driven by wind
- Condensation: `moisture > threshold + upward velocity → clouds`
- Rain triggers: `condensation > saturation threshold`
- Updates on coarse 64×64 slices

### EnvironmentSamplingSystem

**Group**: `EnvironmentSystemGroup`  
**Order**: `UpdateAfter(typeof(ClimateFeedbackSystem))`

Parallel environment sampling updating `EnvironmentSample` components.

**Key Behaviors**:
- Samples `TemperatureGrid`, `MoistureGrid`, `SunlightGrid`
- Updates `EnvironmentSample` on entities with `LocalTransform`
- Runs every tick
- Burst-compiled parallel job

### VegetationSamplingSystem

**Group**: `VegetationSystemGroup`  
**Order**: `UpdateBefore(typeof(VegetationGrowthSystem))`

Statistical sampling for vegetation growth optimization.

**Key Behaviors**:
- Groups vegetation into 8×8 cell patches
- Updates representative samples (max 10K)
- Replicates updates to patch members probabilistically
- Respects temporal LOD (`VegetationDivisor`)

### BiomeAggregationSystem

**Group**: `EnvironmentSystemGroup`  
**Order**: `UpdateAfter(typeof(ClimateFeedbackSystem))`

Lightweight Biome ECS for asynchronous aggregation.

**Key Behaviors**:
- Updates `BiomeTelemetry` from chunk data
- Aggregates temperature, moisture, flora counts
- Provides summarized telemetry to other systems
- Detaches climate ticks from per-entity logic

### FieldBufferSwapSystem

**Group**: `EnvironmentSystemGroup`  
**Order**: `OrderLast = true`

Swaps double-buffered field buffers at tick end.

**Key Behaviors**:
- Toggles `ReadBufferIndex` (0 ↔ 1)
- Runs last in `EnvironmentSystemGroup`
- Ensures deterministic reads

### PhotosynthesisOxygenSystem

**Group**: `EnvironmentSystemGroup`  
**Order**: `UpdateAfter(typeof(ClimateFeedbackSystem))`

Photosynthesis-Oxygen loop: vegetation → oxygen → fauna → fire → reduces oxygen.

**Key Behaviors**:
- Queries mature vegetation
- Calculates oxygen production
- Updates oxygen grid or feeds into climate feedback
- Respects temporal LOD (`ClimateFeedbackDivisor`)

### RainSoilSystem

**Group**: `EnvironmentSystemGroup`  
**Order**: `UpdateAfter(typeof(WindCloudSystem))`

Rain-Soil loop: rainfall → soil moisture → growth → evapotranspiration → clouds.

**Key Behaviors**:
- Integrates `CloudGrid` rain rate with `MoistureGrid`
- Updates soil moisture from rain
- Feeds into vegetation growth
- Triggers cloud formation from evapotranspiration

### PopulationPressureSystem

**Group**: `EnvironmentSystemGroup`  
**Order**: `UpdateAfter(typeof(ClimateFeedbackSystem))`

Population pressure loop: overpopulation → resource decline → migration intent.

**Key Behaviors**:
- Queries villagers/agents for population density
- Calculates resource availability per capita
- Generates migration intents when overpopulation detected
- Feeds into AI goal evaluation
- Respects temporal LOD (`ClimateFeedbackDivisor`)

### EnvironmentalDesirabilitySystem

**Group**: (Mind ECS - DefaultEcs)  
**Order**: N/A (managed system)

Weights goals by environmental preferences (comfort vs. fear).

**Key Behaviors**:
- Queries entities with `EnvironmentalPreference` and `EnvironmentSample`
- Calculates desirability based on environment match
- Updates goal priorities
- Integrates with Mind ECS goal evaluation

### AmbitionSystem

**Group**: (Mind ECS - DefaultEcs)  
**Order**: N/A (managed system)

Evaluates "expand farm" / "seek shade" / "migrate" goals.

**Key Behaviors**:
- Queries entities with `AmbitionGoal`, `EnvironmentSample`, `EnvironmentalPreference`
- Evaluates ambitions based on environmental conditions
- "expand farm": soil fertility and moisture favorable
- "seek shade": light too high, temperature uncomfortable
- "migrate": environmental conditions unfavorable
- Integrates with Mind ECS goal evaluation

---

## File Locations

### Components

- `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/Environment/ClimateChunkComponents.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/Environment/EnvironmentSampleComponents.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/Environment/EnvironmentGrids.cs` (FireGrid, CloudGrid)
- `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/VegetationComponents.cs` (FloraState, VegetationPatch)
- `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/Biome/BiomeComponents.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/AI/MindECS/Components/GoalOptimizationComponents.cs`

### Systems

- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Environment/ClimateChunkManagerSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Environment/TemporalLODSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Environment/FieldPropagationSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Environment/ClimateFeedbackSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Environment/FirePropagationSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Environment/WindCloudSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Environment/EnvironmentSamplingSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Environment/FieldBufferSwapSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Environment/PhotosynthesisOxygenSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Environment/RainSoilSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Environment/PopulationPressureSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Vegetation/VegetationSamplingSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Biome/BiomeAggregationSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/AI/MindECS/Systems/EnvironmentalDesirabilitySystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/AI/MindECS/Systems/AmbitionSystem.cs`

### Debug Systems

- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Debug/EnvironmentDebugVisualizationSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Debug/FireRainVisualizationSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Debug/TelemetryOverlaySystem.cs`

---

## Quick Reference

### Common Queries

```csharp
// Query entities with environment samples
var query = SystemAPI.QueryBuilder()
    .WithAll<EnvironmentSample, LocalTransform>()
    .Build();

// Query active climate chunks
var chunkQuery = SystemAPI.QueryBuilder()
    .WithAll<ClimateChunk>()
    .Build();

// Query biome entities with telemetry
var biomeQuery = SystemAPI.QueryBuilder()
    .WithAll<BiomeEntity, BiomeTelemetry>()
    .Build();

// Query vegetation with flora state
var vegetationQuery = SystemAPI.QueryBuilder()
    .WithAll<VegetationId, FloraState>()
    .Build();
```

### Common Operations

```csharp
// Check if system should update (temporal LOD)
if (!TemporalLODHelpers.ShouldUpdate(timeState.Tick, lodConfig.TemperatureDivisor))
    return;

// Request chunk activation
var requests = SystemAPI.GetBuffer<ActiveChunkRequest>(managerEntity);
requests.Add(new ActiveChunkRequest { ChunkCoord = coord, RequestTick = tick });

// Sample environment at position
var sample = SystemAPI.GetSingleton<EnvironmentSampler>().SampleScalar(
    worldPosition, 
    "temperature"
);

// Get biome telemetry
var telemetry = SystemAPI.GetComponent<BiomeTelemetry>(biomeEntity);
var avgTemp = telemetry.AverageTemperature;
```

---

## See Also

- **[SpatialTemporalFieldOptimizationIntegrationGuide.md](SpatialTemporalFieldOptimizationIntegrationGuide.md)** - Complete integration guide
- `Docs/Guides/BiomeTerraformingAPI.md` - Related biome systems API
- `TRI_PROJECT_BRIEFING.md` - Project overview and coding patterns

