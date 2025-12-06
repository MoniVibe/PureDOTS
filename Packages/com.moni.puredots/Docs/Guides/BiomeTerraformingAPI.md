# Biome & Terraforming System API Reference

**Updated**: 2025-01-27  
**Purpose**: Complete API reference for biome, terraforming, and shipboard ecology systems

## Components

### Environment Fields

#### `LightField : IComponentData`
Scalar light field extracted from `SunlightGrid`.

```csharp
public struct LightField : IComponentData
{
    public EnvironmentGridMetadata Metadata;
    public BlobAssetReference<LightFieldBlob> Blob;
    public FixedString64Bytes ChannelId;
    public uint LastUpdateTick;
    public uint LastTerrainVersion;
    
    public float SampleBilinear(float3 worldPosition, float defaultValue = 0f);
}
```

#### `ChemicalField : IComponentData`
Atmospheric composition field (O₂, CO₂, pollutants).

```csharp
public struct ChemicalField : IComponentData
{
    public EnvironmentGridMetadata Metadata;
    public BlobAssetReference<ChemicalFieldBlob> Blob;
    public FixedString64Bytes ChannelId;
    public uint LastUpdateTick;
    public uint LastTerrainVersion;
    
    public ChemicalSample SampleBilinear(float3 worldPosition, ChemicalSample defaultValue = default);
}

public struct ChemicalSample
{
    public float Oxygen;        // 0-100
    public float CarbonDioxide; // 0-100
    public float Pollutants;    // 0-100
}
```

### Biome Lookup Tables

#### `BiomeLUT : IComponentData`
Singleton providing biome lookup table access.

```csharp
public struct BiomeLUT : IComponentData
{
    public BlobAssetReference<BiomeLUTBlob> Blob;
    
    public BiomeType EvaluateBiome(float temperature, float moisture, float light = 50f);
    public BiomeType EvaluateBiomeWithChemical(float temperature, float moisture, float light, float chemical);
}
```

#### `BiomeLUTBlob`
Pre-computed lookup table blob.

```csharp
public struct BiomeLUTBlob
{
    public BlobArray<BiomeType> TempMoistureLightMatrix;        // [100×100×50]
    public BlobArray<BiomeType> TempMoistureLightChemicalMatrix; // [100×100×50×20]
    
    public int TemperatureResolution;  // Default: 100
    public int MoistureResolution;     // Default: 100
    public int LightResolution;        // Default: 50
    public int ChemicalResolution;     // Default: 20
    
    public float TemperatureMin;  // Default: -50°C
    public float TemperatureMax;  // Default: 50°C
    public float MoistureMin;     // Default: 0
    public float MoistureMax;     // Default: 100
    public float LightMin;        // Default: 0
    public float LightMax;        // Default: 100
    public float ChemicalMin;     // Default: 0
    public float ChemicalMax;     // Default: 100
    
    public BiomeType EvaluateBiome(float temperature, float moisture, float light);
    public BiomeType EvaluateBiomeWithChemical(float temperature, float moisture, float light, float chemical);
}
```

### Chunk Tracking

#### `BiomeChunkMetadata : IComponentData`
Metadata describing biome chunk organization.

```csharp
public struct BiomeChunkMetadata : IComponentData
{
    public int2 ChunkSize;        // Cells per chunk (default: 64×64)
    public int2 ChunkCounts;      // Number of chunks per dimension
    public int TotalChunkCount;
    public EnvironmentGridMetadata GridMetadata;
    
    public int GetChunkIndex(int2 cellCoord);
    public int2 GetChunkCoord(int2 cellCoord);
    public void GetChunkCellRange(int chunkIndex, out int2 minCell, out int2 maxCell);
}
```

#### `BiomeChunkHash : IBufferElementData`
Hash value per chunk for change detection.

```csharp
[InternalBufferCapacity(0)]
public struct BiomeChunkHash : IBufferElementData
{
    public uint Value;
}
```

#### `BiomeChunkDirtyFlag : IBufferElementData`
Dirty flag per chunk (1 = dirty, 0 = clean).

```csharp
[InternalBufferCapacity(0)]
public struct BiomeChunkDirtyFlag : IBufferElementData
{
    public byte Value; // 1 = dirty, 0 = clean
}
```

### Terraforming

#### `TerraformingEvent : IBufferElementData`
Terraforming event applying deltas to environment fields.

```csharp
[InternalBufferCapacity(0)]
public struct TerraformingEvent : IBufferElementData
{
    public float3 Position;                    // World position
    public float Radius;                       // Effect radius
    public float Intensity;                    // Magnitude (signed delta)
    public TerraformingFieldType FieldType;   // Which field to modify
    public TerraformingShape Shape;            // Distribution shape
    public uint Tick;                          // Creation tick
    public FixedString64Bytes SourceId;        // Source identifier
}

public enum TerraformingFieldType : byte
{
    Temperature = 0,
    Moisture = 1,
    Light = 2,
    Chemical = 3,
    Wind = 4
}

public enum TerraformingShape : byte
{
    Gaussian = 0,  // Smooth exponential falloff
    Impulse = 1,   // Sharp cutoff at 10% radius
    Linear = 2     // Linear falloff
}
```

#### `TerraformingDelta : IBufferElementData`
Accumulated terraforming deltas per cell.

```csharp
[InternalBufferCapacity(0)]
public struct TerraformingDelta : IBufferElementData
{
    public float TemperatureDelta;
    public float MoistureDelta;
    public float LightDelta;
    public float ChemicalDelta;
}
```

### Planet Physical Profile

#### `PlanetPhysicalProfile : IComponentData`
Singleton providing planetary constants.

```csharp
public struct PlanetPhysicalProfile : IComponentData
{
    public BlobAssetReference<PlanetPhysicalProfileBlob> Blob;
    
    public float GetGravityMultiplier();      // Normalized to Earth (9.81 m/s²)
    public float GetIrradianceMultiplier();   // Normalized to Earth (~1361 W/m²)
}

public struct PlanetPhysicalProfileBlob
{
    // Planetary constants
    public float Mass;              // kg
    public float Radius;            // m
    public float DistanceToStar;    // m
    public float StarLuminosity;    // W
    public float RotationRate;      // rad/s
    public float AxialTilt;         // rad
    
    // Atmospheric composition
    public float CompositionOxygen;
    public float CompositionCO2;
    public float CompositionNitrogen;
    
    // Derived coefficients (computed once)
    public float Gravity;           // m/s²
    public float Irradiance;        // W/m²
    public float AtmosphereDensity; // kg/m³
    public float CoriolisStrength;   // Scaling factor
}
```

### Ship Biodecks

#### `BiodeckClimate : IComponentData`
Climate state for a biodeck.

```csharp
public struct BiodeckClimate : IComponentData
{
    public float Temperature;      // Celsius
    public float Humidity;          // 0-100%
    public float Light;             // 0-100
    public uint LastUpdateTick;
}
```

#### `BiodeckAtmosphere : IComponentData`
Atmospheric composition for a biodeck.

```csharp
public struct BiodeckAtmosphere : IComponentData
{
    public float Oxygen;           // 0-100
    public float CarbonDioxide;    // 0-100
    public float Pressure;          // kPa
}
```

#### `BiodeckFloraBuffer : IBufferElementData`
Flora species composition.

```csharp
[InternalBufferCapacity(0)]
public struct BiodeckFloraBuffer : IBufferElementData
{
    public int SpeciesId;
    public float Coverage;  // 0-1
}
```

#### `BiodeckSimulationConfig : IComponentData`
Biodeck simulation configuration.

```csharp
public struct BiodeckSimulationConfig : IComponentData
{
    public uint TickDivisor;        // Update every N ticks (default: 2)
    public float TargetTemperature; // °C
    public float TargetHumidity;     // 0-100%
    public float TargetOxygen;      // 0-100%
}
```

#### `BiodeckModuleLink : IComponentData`
Link to ship modules affecting biodeck.

```csharp
public struct BiodeckModuleLink : IComponentData
{
    public Entity ReactorEntity;         // Provides heat
    public Entity HullEntity;            // Affects pressure
    public Entity RadiationShieldEntity; // Affects heat
}
```

### Environmental Telemetry

#### `EnvironmentalTelemetry : IComponentData`
Environmental telemetry for agents.

```csharp
public struct EnvironmentalTelemetry : IComponentData
{
    public BiomeType CurrentBiome;
    public float Temperature;  // Celsius
    public float Moisture;      // 0-100
    public float Comfort;       // 0-1 (computed)
    public float Light;         // 0-100
    public float Chemical;      // Pollutants (0-100)
    public uint LastUpdateTick;
}
```

#### `EnvironmentalDesire : IComponentData`
Agent desire triggered by environment.

```csharp
public struct EnvironmentalDesire : IComponentData
{
    public FixedString64Bytes DesireId; // e.g., "SeekTemperateRegion"
    public float Urgency;                // 0-1
    public float3 TargetPosition;        // Optional target
}
```

### Terraforming Preview

#### `TerraformingPreviewBuffer : IComponentData`
Shadow buffer for preview calculations.

```csharp
public struct TerraformingPreviewBuffer : IComponentData
{
    public BlobAssetReference<TerraformingPreviewBlob> ShadowBlob;
    public uint PreviewVersion;
    public uint LastPreviewTick;
}

public struct TerraformingPreviewActiveTag : IComponentData { }

[InternalBufferCapacity(0)]
public struct TerraformingPreviewCommitCommand : IBufferElementData
{
    public byte Commit; // 1 = commit, 0 = cancel
}
```

### Race Compatibility

#### `RacePreferenceProfileBlob`
Race environmental preference profile.

```csharp
public struct RacePreferenceProfileBlob
{
    public FixedString64Bytes RaceId;
    public float MinTemperature;  // °C
    public float MaxTemperature;  // °C
    public float MinMoisture;     // 0-100
    public float MaxMoisture;     // 0-100
    public float MinOxygen;       // 0-100
    public float MaxOxygen;       // 0-100
    public float MinPressure;     // kPa
    public float MaxPressure;     // kPa
}
```

## Systems

### `BiomeChunkDirtyTrackingSystem`
Marks biome chunks as dirty when input fields change.

**Update Group**: `EnvironmentSystemGroup` (OrderFirst)  
**Dependencies**: `BiomeGrid`, `BiomeChunkMetadata`, `MoistureGrid`, `TemperatureGrid`

**Behavior**:
- Computes hash per chunk from input fields (temperature, moisture, light, chemical)
- Compares with previous hash
- Marks chunk dirty if changed
- Writes dirty flags to `BiomeChunkDirtyFlag` buffer

### `BiomeDerivationSystem`
Derives biome classifications using LUT or legacy classification.

**Update Group**: `EnvironmentSystemGroup` (after `BiomeChunkDirtyTrackingSystem`)  
**Dependencies**: `BiomeGrid`, `MoistureGrid`, `TemperatureGrid`, optional `BiomeLUT`

**Behavior**:
- Processes only dirty chunks (incremental) or all cells (full rebuild)
- Uses `BiomeLUT` if available, otherwise falls back to legacy classification
- Updates `BiomeGridRuntimeCell` buffer

### `TerraformingDeltaSystem`
Applies terraforming events as deltas to environment fields.

**Update Group**: `EnvironmentSystemGroup` (after `BiomeChunkDirtyTrackingSystem`, before `FieldPropagationSystem`)  
**Dependencies**: `TerraformingEvent` buffer, target grids

**Behavior**:
- Reads `TerraformingEvent` buffer entries
- Applies Gaussian/impulse/linear deltas to target fields
- Clears processed events

### `MiracleEnvironmentSystem`
Converts miracle effects to terraforming events.

**Update Group**: `MiracleEffectSystemGroup` (after `MiracleActivationSystem`)  
**Dependencies**: `MiracleEffect`, `TerraformingEvent` buffer

**Behavior**:
- Reads active `MiracleEffect` components
- Maps miracle type → terraforming field type
- Creates `TerraformingEvent` buffer entries

### `PlanetPhysicalBootstrapSystem`
Bootstraps planet physical profile singleton.

**Update Group**: `InitializationSystemGroup`  
**Dependencies**: None

**Behavior**:
- Creates default Earth-like profile if none exists
- Computes derived coefficients (gravity, irradiance, atmosphere density)
- Creates `PlanetPhysicalProfile` singleton

### `BiodeckSystem`
Updates biodeck climate and atmosphere at reduced tick rate.

**Update Group**: `EnvironmentSystemGroup`  
**Dependencies**: `BiodeckClimate`, `BiodeckAtmosphere`, `BiodeckSimulationConfig`

**Behavior**:
- Updates climate toward target values (temperature, humidity, oxygen)
- Processes at reduced cadence (default: every 2 ticks = 0.5 Hz)
- Links to ship modules for effects (reactor → heat, hull → pressure)

### `AgentEnvironmentalDesireSystem`
Reads environmental telemetry and triggers agent desires.

**Update Group**: `AISystemGroup`  
**Dependencies**: `EnvironmentalTelemetry`, `BiomeGrid`, `MoistureGrid`, `TemperatureGrid`

**Behavior**:
- Samples environment at agent position
- Computes comfort score (0-1)
- Triggers desires when comfort < threshold
- Links to Mind ECS via `AgentSyncBus`

### `TerraformingPreviewSystem`
Runs terraforming calculations on shadow buffers.

**Update Group**: `EnvironmentSystemGroup` (after `TerraformingDeltaSystem`)  
**Dependencies**: `TerraformingPreviewBuffer`, `TerraformingPreviewActiveTag`

**Behavior**:
- Applies terraforming events to shadow buffer
- Processes commit/cancel commands
- Swaps shadow → live on commit

## Authoring

### `BiomeLUTAuthoring : ScriptableObject`
Generates biome lookup table blob asset.

**Menu Path**: `Assets → Create → PureDOTS → Environment → Biome Lookup Table`

**Fields**:
- Resolution: Temperature/Moisture/Light/Chemical resolution
- Value Ranges: Min/max for each field
- Biome Thresholds: Classification thresholds

**Usage**:
```csharp
var lutAuthoring = ScriptableObject.CreateInstance<BiomeLUTAuthoring>();
lutAuthoring._temperatureResolution = 100;
lutAuthoring._moistureResolution = 100;
var blobAsset = lutAuthoring.CreateBlobAsset();
```

## Utility Functions

### `EnvironmentGridMath`
Shared math helpers for environment grids.

```csharp
public static class EnvironmentGridMath
{
    public static int GetCellIndex(in EnvironmentGridMetadata metadata, int2 cell);
    public static int2 GetCellCoordinates(in EnvironmentGridMetadata metadata, int index);
    public static bool TryGetNeighborIndex(in EnvironmentGridMetadata metadata, int index, int2 offset, out int neighborIndex);
    public static float3 GetCellCenter(in EnvironmentGridMetadata metadata, int index);
    public static bool TryWorldToCell(in EnvironmentGridMetadata metadata, float3 worldPosition, out int2 baseCell, out float2 fractional);
    public static float SampleBilinear(in EnvironmentGridMetadata metadata, ref BlobArray<float> values, float3 worldPosition, float defaultValue);
    public static ChemicalSample SampleBilinearChemical(in EnvironmentGridMetadata metadata, ref BlobArray<ChemicalSample> values, float3 worldPosition, ChemicalSample defaultValue);
}
```

## Performance Metrics

| Operation | Cost | Notes |
|-----------|------|-------|
| Biome LUT lookup | < 0.01ms | Per evaluation |
| Chunk dirty tracking | ~0.1ms | Per update (all chunks) |
| Biome derivation (dirty chunks) | ~0.5ms | 1-2% of chunks per second |
| Terraforming delta application | ~0.2ms | Per event batch |
| Biodeck update | < 0.1ms | Per biodeck at 0.5 Hz |
| Environmental telemetry | ~0.05ms | Per agent |

## Related Documentation

- `Docs/Guides/BiomeTerraformingIntegrationGuide.md` - Integration patterns and examples
- `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` - System execution order

