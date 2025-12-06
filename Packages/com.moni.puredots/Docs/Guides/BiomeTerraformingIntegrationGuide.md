# Biome & Terraforming System Integration Guide

**Updated**: 2025-01-27  
**Purpose**: Guide for integrating with optimized biome, terraforming, and shipboard ecology systems

## Overview

The biome and terraforming optimization system provides:
- **Layered-field representation**: Separate scalar fields (temperature, moisture, light, chemical) with incremental updates
- **Chunk-based dirty tracking**: Only recalculates biomes when input fields change (O(k) vs O(k×n))
- **Cached lookup tables**: Pre-computed biome classifications for instant evaluation
- **Terraforming events**: Composable, replayable delta injection system
- **Ship biodecks**: Self-contained mini-biome ECS for shipboard ecology
- **Environmental telemetry**: Agent comfort evaluation and goal triggering

## System Architecture

### System Ordering

```
EnvironmentSystemGroup:
  - BiomeChunkDirtyTrackingSystem (OrderFirst) - Marks dirty chunks
  - TerraformingDeltaSystem - Applies terraforming events
  - FieldPropagationSystem - Diffusion/advection
  - BiomeDerivationSystem - Processes dirty chunks only
  - TerraformingPreviewSystem - Preview calculations
  - TemporalLODSystem - Adaptive simulation granularity
```

### Component Dependencies

**Required Singletons** (auto-bootstrapped):
- `BiomeGrid` - Biome assignments per cell
- `MoistureGrid` - Moisture field
- `TemperatureGrid` - Temperature field
- `BiomeChunkMetadata` - Chunk organization (created by bootstrap)

**Optional Singletons**:
- `BiomeLUT` - Lookup table for fast biome evaluation
- `SunlightGrid` - Light field
- `ChemicalField` - Atmospheric composition
- `PlanetPhysicalProfile` - Planetary constants

## Integration Patterns

### 1. Creating Terraforming Events

Terraforming events modify environment fields (temperature, moisture, light, chemical) via delta injection:

```csharp
// Get terraforming event buffer (singleton)
var terraformingEntity = SystemAPI.GetSingletonEntity<TerraformingEvent>();
var events = SystemAPI.GetBuffer<TerraformingEvent>(terraformingEntity);

// Add a temperature increase event (Gaussian distribution)
events.Add(new TerraformingEvent
{
    Position = impactPosition,
    Radius = 50f,                    // Effect radius in world units
    Intensity = 10f,                // +10°C at center
    FieldType = TerraformingFieldType.Temperature,
    Shape = TerraformingShape.Gaussian, // Smooth falloff
    Tick = timeState.Tick,
    SourceId = "CometImpact"
});

// Add a moisture impulse (sharp delta)
events.Add(new TerraformingEvent
{
    Position = rainPosition,
    Radius = 30f,
    Intensity = 25f,                // +25% moisture
    FieldType = TerraformingFieldType.Moisture,
    Shape = TerraformingShape.Impulse, // Sharp cutoff
    Tick = timeState.Tick,
    SourceId = "RainMiracle"
});
```

**Event Shapes**:
- `Gaussian`: Smooth exponential falloff (best for natural effects)
- `Impulse`: Sharp cutoff at 10% radius (best for precise miracles)
- `Linear`: Linear falloff (best for gradual terraforming)

### 2. Using Biome Lookup Tables

Biome LUTs eliminate expensive classification logic. Create via authoring:

```csharp
// In authoring (ScriptableObject)
var lutAuthoring = ScriptableObject.CreateInstance<BiomeLUTAuthoring>();
lutAuthoring._temperatureResolution = 100;
lutAuthoring._moistureResolution = 100;
lutAuthoring._lightResolution = 50;
var blobAsset = lutAuthoring.CreateBlobAsset();

// At runtime (in system)
if (SystemAPI.TryGetSingleton<BiomeLUT>(out var lut))
{
    var biome = lut.EvaluateBiome(temperature: 20f, moisture: 60f, light: 75f);
    // Returns BiomeType instantly (no classification logic)
}
```

**LUT Resolution Trade-offs**:
- Higher resolution = more memory, more accurate
- Default: 100×100×50 (temperature×moisture×light) = ~500KB
- 4D LUT (with chemical): 100×100×50×20 = ~10MB (optional)

### 3. Chunk-Based Incremental Updates

Biome chunks are marked dirty when input fields change. Only dirty chunks are recalculated:

```csharp
// BiomeChunkDirtyTrackingSystem automatically:
// 1. Computes hash per chunk from input fields
// 2. Compares with previous hash
// 3. Marks chunk dirty if changed

// BiomeDerivationSystem processes only dirty chunks:
// - Full rebuild: All cells (on terrain change)
// - Incremental: Only dirty chunks (normal operation)

// Check if chunk is dirty (for custom systems)
var biomeEntity = SystemAPI.GetSingletonEntity<BiomeGrid>();
if (SystemAPI.HasBuffer<BiomeChunkDirtyFlag>(biomeEntity))
{
    var dirtyFlags = SystemAPI.GetBuffer<BiomeChunkDirtyFlag>(biomeEntity);
    var chunkIndex = chunkMetadata.GetChunkIndex(cellCoord);
    var isDirty = dirtyFlags[chunkIndex].Value == 1;
}
```

**Chunk Size**: Default 64×64 cells (configurable via `BiomeChunkMetadata`)

### 4. Integrating with Miracles

Miracles automatically convert to terraforming events via `MiracleEnvironmentSystem`:

```csharp
// In your miracle system
// MiracleEnvironmentSystem reads MiracleEffect components and:
// - Extracts position, radius, intensity
// - Maps miracle type → terraforming field type
// - Creates TerraformingEvent buffer entries

// To add custom miracle → terraforming mapping:
// Extend MiracleEnvironmentSystem.ConvertMiracleToTerraforming()
```

**Miracle Types → Field Types** (extend as needed):
- `Rain` → `Moisture` (+delta)
- `Fireball` → `Temperature` (+delta)
- `Lightning` → `Light` (+delta)
- `Meteor` → `Temperature` (+delta), `Chemical` (+pollutants)

### 5. Ship Biodecks

Biodecks are self-contained mini-biome ECS running at reduced tick rate:

```csharp
// Create biodeck entity
var biodeckEntity = entityManager.CreateEntity();
entityManager.AddComponentData(biodeckEntity, new BiodeckClimate
{
    Temperature = 22f,
    Humidity = 60f,
    Light = 80f
});
entityManager.AddComponentData(biodeckEntity, new BiodeckAtmosphere
{
    Oxygen = 21f,
    CarbonDioxide = 0.04f,
    Pressure = 101.3f
});
entityManager.AddComponentData(biodeckEntity, new BiodeckSimulationConfig
{
    TickDivisor = 2,              // Update every 2 ticks (0.5 Hz)
    TargetTemperature = 22f,
    TargetHumidity = 60f,
    TargetOxygen = 21f
});

// Link to ship modules
entityManager.AddComponentData(biodeckEntity, new BiodeckModuleLink
{
    ReactorEntity = reactorEntity,      // Provides heat
    HullEntity = hullEntity,            // Affects pressure
    RadiationShieldEntity = shieldEntity // Affects heat
});

// BiodeckSystem automatically updates climate toward targets
// Module effects (reactor leaks, hull breaches) modify targets
```

**Biodeck Tick Rate**: Default 0.5 Hz (every 2 ticks). Adjust via `TickDivisor`.

### 6. Environmental Telemetry for Agents

Agents read environmental telemetry to evaluate comfort and trigger goals:

```csharp
// Add telemetry component to agent
entityManager.AddComponentData(agentEntity, new EnvironmentalTelemetry
{
    CurrentBiome = BiomeType.Unknown,
    Temperature = 0f,
    Moisture = 0f,
    Comfort = 0f
});

// AgentEnvironmentalDesireSystem automatically:
// 1. Samples biome/temperature/moisture at agent position
// 2. Computes comfort score (0-1, based on temperature/moisture)
// 3. Triggers desires when comfort < threshold

// Read telemetry in your agent system
if (SystemAPI.TryGetComponent<EnvironmentalTelemetry>(agentEntity, out var telemetry))
{
    if (telemetry.Comfort < 0.5f)
    {
        // Trigger "SeekTemperateRegion" goal
        // Link to Mind ECS via AgentSyncBus
    }
}
```

**Comfort Calculation**:
- Optimal temperature: 20°C (comfort decreases with distance)
- Optimal moisture: 50% (comfort decreases with distance)
- Combined: `(tempComfort + moistComfort) / 2`

### 7. Planet Physical Profile

Planetary constants compute baseline climate coefficients once at world creation:

```csharp
// PlanetPhysicalBootstrapSystem creates default Earth-like profile
// To customize, extend PlanetPhysicalBootstrapSystem.OnUpdate():

var profile = PlanetPhysicalProfileBlob.Compute(
    mass: 5.972e24f,              // kg
    radius: 6.371e6f,              // m
    distanceToStar: 1.496e11f,     // m (1 AU)
    starLuminosity: 3.828e26f,     // W
    rotationRate: 7.292e-5f,       // rad/s
    axialTilt: 0.409f,             // rad (~23.4°)
    compositionOxygen: 0.21f,
    compositionCO2: 0.0004f,
    compositionNitrogen: 0.78f
);

// Use multipliers in climate systems
if (SystemAPI.TryGetSingleton<PlanetPhysicalProfile>(out var planetProfile))
{
    var gravityMultiplier = planetProfile.GetGravityMultiplier();
    var irradianceMultiplier = planetProfile.GetIrradianceMultiplier();
    // Apply multipliers to climate calculations
}
```

**Derived Coefficients** (computed once):
- Gravity: `g = GM/R²`
- Irradiance: `I = L / (4πr²)`
- Atmosphere density: Proportional to gravity and composition
- Coriolis strength: Proportional to rotation rate

### 8. Terraforming Preview

Preview terraforming effects before committing:

```csharp
// Activate preview mode
var previewEntity = entityManager.CreateEntity();
entityManager.AddComponentData(previewEntity, new TerraformingPreviewActiveTag());
entityManager.AddComponentData(previewEntity, new TerraformingPreviewBuffer
{
    ShadowBlob = CreateShadowBlob(), // Copy of live fields
    PreviewVersion = 1
});

// TerraformingPreviewSystem runs calculations on shadow buffer
// Visualize projected changes (no risk to live simulation)

// Commit or cancel
var commands = entityManager.AddBuffer<TerraformingPreviewCommitCommand>(previewEntity);
commands.Add(new TerraformingPreviewCommitCommand { Commit = 1 }); // Commit
// OR
commands.Add(new TerraformingPreviewCommitCommand { Commit = 0 }); // Cancel
```

**Preview Workflow**:
1. Create shadow buffer (copy of live fields)
2. Apply terraforming events to shadow
3. Visualize projected changes
4. Commit (swap buffers) or cancel (discard shadow)

## Performance Considerations

### Biome Updates
- **Before**: O(k×n) - Recalculate all biomes every update
- **After**: O(k) - Only update changed input layers
- **Chunk processing**: 1-2% of chunks per second during terraforming

### LUT Lookup
- **Cost**: < 0.01ms per biome evaluation
- **Memory**: ~500KB for 3D LUT, ~10MB for 4D LUT (optional)

### Biodeck Overhead
- **Tick rate**: 0.5 Hz (every 2 ticks)
- **Cost**: < 0.1ms/frame per biodeck
- **Scalability**: Supports hundreds of biodecks with minimal overhead

## Common Patterns

### Pattern 1: Miracle → Terraforming
```csharp
// In MiracleActivationSystem or custom miracle system
var terraformingEntity = SystemAPI.GetSingletonEntity<TerraformingEvent>();
var events = SystemAPI.GetBuffer<TerraformingEvent>(terraformingEntity);

events.Add(new TerraformingEvent
{
    Position = miraclePosition,
    Radius = miracleRadius,
    Intensity = miracleIntensity,
    FieldType = MapMiracleToField(miracleType),
    Shape = TerraformingShape.Gaussian,
    Tick = timeState.Tick,
    SourceId = miracleId.ToString()
});
```

### Pattern 2: Structure → Climate Control
```csharp
// Terraforming structure (e.g., terraforming altar)
entityManager.AddComponentData(structureEntity, new ClimateControlSource
{
    Kind = ClimateControlKind.Structure,
    Center = structurePosition,
    Radius = 100f,
    TargetClimate = new ClimateVector
    {
        Temperature = 0.5f,  // Normalized: -1 (cold) to +1 (hot)
        Moisture = 0.6f,     // 0-1
        Fertility = 0.8f
    },
    Strength = 0.1f  // How fast it pushes toward target
});

// ClimateControlSystem (existing) applies gradual changes
```

### Pattern 3: Agent Comfort Evaluation
```csharp
// In agent AI system
if (SystemAPI.TryGetComponent<EnvironmentalTelemetry>(agentEntity, out var telemetry))
{
    if (telemetry.Comfort < 0.3f)
    {
        // Low comfort - trigger migration goal
        // Link to Mind ECS via AgentSyncBus
    }
    
    if (telemetry.CurrentBiome == BiomeType.Desert && telemetry.Comfort < 0.5f)
    {
        // In desert with low comfort - trigger "PerformMiracle" goal
    }
}
```

## Troubleshooting

### Biomes Not Updating
- Check `BiomeChunkDirtyTrackingSystem` is running (OrderFirst in EnvironmentSystemGroup)
- Verify input fields (temperature, moisture) are changing
- Check `BiomeChunkMetadata` exists on biome entity

### Terraforming Events Not Applied
- Verify `TerraformingDeltaSystem` runs after `BiomeChunkDirtyTrackingSystem`
- Check event buffer exists (singleton entity with `TerraformingEvent` buffer)
- Verify field type matches existing grid (e.g., `TemperatureGrid` for `TerraformingFieldType.Temperature`)

### LUT Not Used
- Ensure `BiomeLUT` singleton exists (created via authoring)
- Check `BiomeDerivationSystem` has `HasLUT = true`
- Verify LUT blob is created (check `BiomeLUTAuthoring.CreateBlobAsset()`)

### Biodecks Not Updating
- Check `BiodeckSystem` is enabled
- Verify `BiodeckSimulationConfig.TickDivisor` is set (default: 2)
- Ensure `BiodeckClimate` and `BiodeckAtmosphere` components exist

## Related Documentation

- `Docs/Guides/GettingStarted.md` - System group overview
- `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` - System execution order
- `Docs/Guides/Authoring/EnvironmentAndSpatialValidation.md` - Authoring workflows

