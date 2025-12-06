# Spatial-Temporal Field Optimization Integration Guide

**Last Updated**: 2025-01-27  
**Purpose**: Complete integration guide for spatial-temporal field management optimizations in PureDOTS

---

## Overview

The spatial-temporal field optimization system provides 15 integrated optimization techniques for managing environment grids (temperature, moisture, oxygen, light) at scale while maintaining determinism and performance targets.

**Key Features:**
- Chunked grid compression (64×64 cells per chunk, half-precision)
- Temporal LOD with per-system tick divisors
- Unified field propagation using Burst-compiled kernels
- Statistical vegetation sampling (10K samples → millions of plants)
- Event-driven fire propagation with rain interaction
- Atmospheric feedback loops (temperature, moisture, oxygen convergence)
- Entity-environment coupling via `EnvironmentSample` component
- Biome ECS for asynchronous aggregation
- AI goal optimization with spatial batching
- Double-buffered field data for deterministic reads

**Performance Targets:**
- Vegetation Growth: ≤ 0.5 ms
- Fire Spread: ≤ 1.0 ms
- Climate Grid: ≤ 2.0 ms
- Wind/Clouds: ≤ 1.0 ms
- **Total: ≤ 5 ms** (within 16.6 ms frame budget)

---

## Architecture Overview

### System Groups

All optimization systems run in `EnvironmentSystemGroup` with specific ordering:

```
EnvironmentSystemGroup
├── ClimateChunkManagerSystem (manages active chunks)
├── TemporalLODSystem (initializes LOD config)
├── ClimateStateUpdateSystem (global climate state)
├── WindCloudSystem (wind/cloud advection, 1 Hz)
├── MoistureEvaporationSystem (moisture updates)
├── MoistureSeepageSystem (moisture diffusion)
├── FieldPropagationSystem (unified diffusion/advection)
├── ClimateFeedbackSystem (atmospheric convergence)
├── FirePropagationSystem (event-driven fire spread)
├── EnvironmentSamplingSystem (entity sampling)
├── PhotosynthesisOxygenSystem (vegetation → oxygen)
├── RainSoilSystem (rain → soil → growth → clouds)
├── PopulationPressureSystem (overpopulation → migration)
└── FieldBufferSwapSystem (double-buffer swap, OrderLast)
```

### Component Hierarchy

```
ClimateChunkManager (singleton)
├── ActiveChunkRequest (buffer) - spatial queries request chunks
└── ClimateChunk (per chunk entity)
    └── ClimateChunkBlob (half[,,] arrays)

TemporalLODConfig (singleton)
└── Tick divisors per system

EnvironmentSample (per entity)
└── Temperature, Moisture, Oxygen, Light, SoilFertility

BiomeEntity (per biome)
├── BiomeChunkBuffer (buffer) - owned chunks
└── BiomeTelemetry (aggregated state)
```

---

## Integration Patterns

### 1. Chunked Grid Access

**Problem**: Accessing environment data from chunked grids.

**Solution**: Use `EnvironmentSamplingSystem` which automatically samples from active chunks.

```csharp
// In your system
[BurstCompile]
public partial struct MySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Query entities with EnvironmentSample
        foreach (var (sample, transform) in SystemAPI.Query<RefRO<EnvironmentSample>, RefRO<LocalTransform>>())
        {
            var temp = sample.ValueRO.Temperature;
            var moisture = sample.ValueRO.Moisture;
            // Use sampled values...
        }
    }
}
```

**Manual Chunk Access** (advanced):

```csharp
// Request active chunks for a region
var managerEntity = SystemAPI.GetSingletonEntity<ClimateChunkManager>();
var requests = SystemAPI.GetBuffer<ActiveChunkRequest>(managerEntity);
requests.Add(new ActiveChunkRequest
{
    ChunkCoord = new int2(chunkX, chunkZ),
    RequestTick = timeState.Tick
});

// Access chunk data
foreach (var (chunk, _) in SystemAPI.Query<RefRO<ClimateChunk>>())
{
    if (chunk.ValueRO.IsActive == 1 && chunk.ValueRO.Blob.IsCreated)
    {
        ref var chunkData = ref chunk.ValueRO.Blob.Value;
        var temp = chunkData.Temperature[cellIndex];
        // Use chunk data...
    }
}
```

### 2. Temporal LOD Configuration

**Problem**: Systems updating too frequently, wasting CPU.

**Solution**: Configure tick divisors in `TemporalLODConfig`.

```csharp
// In bootstrap or config system
var lodConfig = new TemporalLODConfig
{
    WindCloudDivisor = 1,        // Every tick (1 Hz)
    TemperatureDivisor = 5,      // Every 5 ticks (0.2 Hz)
    VegetationDivisor = 20,      // Every 20 ticks (0.05 Hz)
    FireDivisor = 1,             // Every tick (but event-driven)
    ClimateFeedbackDivisor = 5   // Every 5 ticks
};
SystemAPI.SetSingleton(lodConfig);
```

**Using Temporal LOD in Systems**:

```csharp
[BurstCompile]
public partial struct MySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var timeState = SystemAPI.GetSingleton<TimeState>();
        var lodConfig = SystemAPI.GetSingleton<TemporalLODConfig>();
        
        // Check if we should update this tick
        if (!TemporalLODHelpers.ShouldUpdate(timeState.Tick, lodConfig.TemperatureDivisor))
        {
            return; // Skip this tick
        }
        
        // Get effective tick delta accounting for LOD
        var effectiveDelta = TemporalLODHelpers.GetEffectiveTickDelta(
            timeState.Tick, 
            lastUpdateTick, 
            lodConfig.TemperatureDivisor
        );
        
        // Update logic...
    }
}
```

### 3. Field Propagation Integration

**Problem**: Need to propagate fields (diffusion, advection) efficiently.

**Solution**: Use `FieldPropagationSystem` which handles both diffusion and wind-driven advection.

```csharp
// FieldPropagationSystem automatically processes:
// - MoistureGrid (via MoistureGridRuntimeCell buffer)
// - TemperatureGrid (via blob arrays)

// To add custom field propagation:
// 1. Create field component with BlobArray<float>
// 2. Add to FieldPropagationSystem's ProcessFieldPropagation method
// 3. System will apply 3×3 Laplacian stencil + wind advection
```

**Custom Field Propagation**:

```csharp
// In FieldPropagationSystem, add:
private void ProcessCustomField(ref SystemState state, CustomField field, TimeState timeState)
{
    // Get wind for advection
    float2 windDirection = SystemAPI.GetSingleton<WindField>().GlobalWindDirection;
    float windStrength = SystemAPI.GetSingleton<WindField>().GlobalWindStrength;
    
    // Create diffusion job (reuses DiffusionKernelJob)
    var job = new DiffusionKernelJob
    {
        SourceField = field.Blob.Value.Values,
        TargetField = nextValues,
        Metadata = field.Metadata,
        Alpha = diffusionCoeff,
        WindDirection = windDirection,
        WindStrength = windStrength,
        Dt = timeState.FixedDeltaTime
    };
    
    state.Dependency = job.ScheduleParallel(field.Metadata.CellCount, 64, state.Dependency);
}
```

### 4. Vegetation Sampling Integration

**Problem**: Updating millions of vegetation entities is expensive.

**Solution**: Use `VegetationSamplingSystem` which updates representative patches.

```csharp
// VegetationSamplingSystem automatically:
// 1. Groups vegetation into 8×8 cell patches
// 2. Updates representative samples (max 10K)
// 3. Replicates updates to patch members probabilistically

// To use:
// 1. Ensure vegetation entities have FloraState component
// 2. System will automatically create VegetationPatch components
// 3. Updates happen at VegetationDivisor cadence (default: every 20 ticks)
```

**Adding FloraState to Vegetation**:

```csharp
// In vegetation authoring/baker
AddComponent(entity, new FloraState
{
    Stage = (byte)lifecycle.CurrentStage,
    Energy = health.LightLevel * 0.5f + health.SoilQuality * 0.5f,
    MoistureNeed = speciesData.WaterConsumptionRate
});
```

### 5. Fire Propagation Integration

**Problem**: Need fire to spread realistically with wind and rain suppression.

**Solution**: Use `FirePropagationSystem` with `FireGrid` component.

```csharp
// Fire propagation automatically:
// 1. Spreads from active fire cells
// 2. Wind-driven: spreadCoeff * max(0, dot(windDir, cellDir)) * dt
// 3. Rain suppression: -rainCoeff * rain * dt
// 4. Links to vegetation energy as fuel

// To ignite fire:
// 1. Create FireGrid singleton (via bootstrap)
// 2. Set heat value > 50f in FireGridBlob
// 3. Set ActiveFire[cellIndex] = 1
// 4. FirePropagationSystem will spread it
```

**Fire Grid Bootstrap**:

```csharp
// In EnvironmentGridBootstrapSystem or similar
var fireGridEntity = state.EntityManager.CreateEntity();
var fireBlob = CreateFireGridBlob(metadata);
state.EntityManager.AddComponentData(fireGridEntity, new FireGrid
{
    Metadata = metadata,
    Blob = fireBlob,
    SpreadCoefficient = 0.5f,
    RainCoefficient = 2.0f
});
```

### 6. Entity-Environment Coupling

**Problem**: Entities need to sample local environment for AI decisions.

**Solution**: Use `EnvironmentSample` component updated by `EnvironmentSamplingSystem`.

```csharp
// Add EnvironmentSample to entities that need environmental data
state.EntityManager.AddComponent<EnvironmentSample>(entity);

// EnvironmentSamplingSystem automatically updates:
// - Temperature (from TemperatureGrid)
// - Moisture (from MoistureGrid)
// - Light (from SunlightGrid)
// - Oxygen (from ChemicalField, if available)
// - SoilFertility (computed from moisture + temperature)

// Use in AI systems:
foreach (var (sample, preference, goal) in SystemAPI.Query<
    RefRO<EnvironmentSample>, 
    RefRO<EnvironmentalPreference>, 
    RefRW<AmbitionGoal>>())
{
    // Calculate desirability
    var tempMatch = math.abs(sample.ValueRO.Temperature - preference.ValueRO.PreferredTemperature);
    var desirability = 1f - math.saturate(tempMatch / 20f);
    
    // Update goal priority based on environment
    goal.ValueRW.Priority *= desirability;
}
```

### 7. Biome ECS Integration

**Problem**: Need aggregated biome telemetry without per-entity overhead.

**Solution**: Use `BiomeAggregationSystem` with `BiomeEntity` components.

```csharp
// Create biome entities
var biomeEntity = state.EntityManager.CreateEntity();
state.EntityManager.AddComponentData(biomeEntity, new BiomeEntity
{
    Type = BiomeType.Forest,
    BiomeCoord = new int2(biomeX, biomeZ)
});

// Add chunks to biome
var chunkBuffer = state.EntityManager.AddBuffer<BiomeChunkBuffer>(biomeEntity);
chunkBuffer.Add(new BiomeChunkBuffer
{
    ChunkCoord = chunkCoord,
    ChunkBlob = chunkBlob
});

// BiomeAggregationSystem automatically updates BiomeTelemetry:
// - AverageTemperature
// - AverageMoisture
// - FloraSampleCount
// - WeatherIntensity

// Query biome telemetry:
foreach (var (telemetry, biome) in SystemAPI.Query<RefRO<BiomeTelemetry>, RefRO<BiomeEntity>>())
{
    var avgTemp = telemetry.ValueRO.AverageTemperature;
    // Use aggregated data...
}
```

### 8. AI Goal Optimization Integration

**Problem**: AI goal evaluation is expensive when done per-entity.

**Solution**: Use spatial desire grids and behavior caching.

```csharp
// Add GoalCache to AI entities
state.EntityManager.AddComponent<GoalCache>(aiEntity);

// Add EnvironmentalPreference for biome interaction
state.EntityManager.AddComponentData(aiEntity, new EnvironmentalPreference
{
    PreferredTemperature = 20f,
    PreferredMoisture = 50f,
    PreferredLight = 70f,
    HumidityPenalty = 0f,  // Desert fauna would have > 0
    LightPenalty = 0f      // Cave flora would have > 0
});

// Add AmbitionGoal for high-level goals
state.EntityManager.AddComponentData(aiEntity, new AmbitionGoal
{
    Type = AmbitionType.ExpandFarm,
    TargetZone = targetPosition,
    Priority = 0.8f,
    IssuedTick = timeState.Tick
});

// EnvironmentalDesirabilitySystem weights goals by environment match
// AmbitionSystem evaluates "expand farm" / "seek shade" / "migrate" goals
```

### 9. Double-Buffering Integration

**Problem**: Need deterministic reads while systems write to fields.

**Solution**: `FieldBufferSwapSystem` automatically swaps buffers at tick end.

```csharp
// Double-buffered fields automatically:
// 1. Read from ReadBufferIndex (0 or 1)
// 2. Write to opposite buffer
// 3. Swap at tick end (FieldBufferSwapSystem, OrderLast)

// To use double-buffering:
// 1. Create DoubleBufferedField component
// 2. Initialize Buffer0 and Buffer1 blobs
// 3. Systems read from ReadBufferIndex, write to opposite
// 4. FieldBufferSwapSystem swaps at end of EnvironmentSystemGroup
```

---

## Performance Considerations

### Memory Budgets

- **Chunked Grids**: ~10-20× smaller memory footprint (half-precision, chunking)
- **Active Chunks**: Default max 100 chunks in memory (configurable via `ClimateChunkManager.MaxActiveChunks`)
- **Vegetation Sampling**: 10K samples represent millions of plants

### CPU Budgets

- **Vegetation Growth**: ≤ 0.5 ms (sampled patches only)
- **Fire Spread**: ≤ 1.0 ms (event-driven, only active fire cells)
- **Climate Grid**: ≤ 2.0 ms (coarse tick, temporal LOD)
- **Wind/Clouds**: ≤ 1.0 ms (1 Hz update)
- **Total**: ≤ 5 ms (within 16.6 ms frame budget)

### Optimization Tips

1. **Temporal LOD**: Increase divisors for slower-changing systems (vegetation: 20, temperature: 5)
2. **Chunk Management**: Reduce `MaxActiveChunks` if memory constrained
3. **Vegetation Sampling**: Adjust `MaxSamples` in `VegetationSamplingSystem` (default: 10K)
4. **Fire Propagation**: Only processes cells near active fire (event-driven)
5. **Field Propagation**: Reuses same kernel for all fields (cache-friendly)

---

## Determinism & Rewind Safety

All systems are deterministic and rewind-safe:

- **Rewind Guards**: All systems check `RewindState.Mode != RewindMode.Record` before mutation
- **Chunk Serialization**: Preserves deterministic state for inactive chunks
- **Temporal LOD**: Uses integer tick divisors (deterministic)
- **Double-Buffering**: Swap occurs at deterministic tick boundaries
- **History Events**: Recorded for chunk load/unload, fire spread, vegetation transitions

---

## Common Patterns

### Pattern 1: Sampling Environment in AI Systems

```csharp
[BurstCompile]
public partial struct MyAISystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (sample, preference, intent) in SystemAPI.Query<
            RefRO<EnvironmentSample>,
            RefRO<EnvironmentalPreference>,
            RefRW<AgentIntentBuffer>>())
        {
            // Calculate comfort score
            var tempDiff = math.abs(sample.ValueRO.Temperature - preference.ValueRO.PreferredTemperature);
            var comfort = 1f - math.saturate(tempDiff / 20f);
            
            // Adjust intent priority
            if (comfort < 0.5f)
            {
                // Seek better environment
                intent.ValueRW.Priority *= 0.5f;
            }
        }
    }
}
```

### Pattern 2: Triggering Fire from Vegetation

```csharp
// In vegetation system or external trigger
if (vegetationHealth.Health < 10f && temperature > 40f)
{
    // Ignite fire at vegetation position
    var fireGrid = SystemAPI.GetSingleton<FireGrid>();
    var cellIndex = fireGrid.GetCellIndex(worldToCell(vegetationPosition));
    
    // Set heat and active flag (would need blob mutation or runtime buffer)
    // In practice, use FireIgnitionEvent buffer or similar
}
```

### Pattern 3: Using Biome Telemetry for Migration

```csharp
[BurstCompile]
public partial struct MigrationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Find favorable biomes
        foreach (var (telemetry, biome) in SystemAPI.Query<
            RefRO<BiomeTelemetry>,
            RefRO<BiomeEntity>>())
        {
            if (telemetry.ValueRO.AverageTemperature > 15f && 
                telemetry.ValueRO.AverageMoisture > 40f)
            {
                // Favorable biome - trigger migration intent
                // Create migration goal for nearby entities
            }
        }
    }
}
```

---

## Troubleshooting

### Chunks Not Loading

- **Check**: `ActiveChunkRequest` buffer on `ClimateChunkManager` entity
- **Check**: `MaxActiveChunks` limit (may be too low)
- **Check**: Chunk serialization interval (default: 60 ticks)

### Temporal LOD Not Working

- **Check**: `TemporalLODConfig` singleton exists
- **Check**: System uses `TemporalLODHelpers.ShouldUpdate()`
- **Check**: Divisors are > 0 (0 disables updates)

### Field Propagation Not Updating

- **Check**: System runs after `FieldPropagationSystem`
- **Check**: Field has runtime buffer or blob arrays
- **Check**: Temporal LOD allows updates this tick

### Environment Sampling Empty

- **Check**: Entities have `EnvironmentSample` component
- **Check**: Grids exist and are initialized
- **Check**: `EnvironmentSamplingSystem` runs after grid updates

---

## See Also

- **[SpatialTemporalFieldOptimizationAPI.md](SpatialTemporalFieldOptimizationAPI.md)** - API reference
- `Docs/Guides/BiomeTerraformingIntegrationGuide.md` - Related biome systems
- `TRI_PROJECT_BRIEFING.md` - Project overview and coding patterns

