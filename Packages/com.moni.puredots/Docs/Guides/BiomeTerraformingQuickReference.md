# Biome & Terraforming Quick Reference

**Updated**: 2025-01-27  
**Purpose**: Quick reference for common integration patterns

## Common Tasks

### Create Terraforming Event
```csharp
var events = SystemAPI.GetBuffer<TerraformingEvent>(terraformingEntity);
events.Add(new TerraformingEvent {
    Position = pos, Radius = 50f, Intensity = 10f,
    FieldType = TerraformingFieldType.Temperature,
    Shape = TerraformingShape.Gaussian, Tick = tick
});
```

### Evaluate Biome with LUT
```csharp
if (SystemAPI.TryGetSingleton<BiomeLUT>(out var lut)) {
    var biome = lut.EvaluateBiome(temp: 20f, moisture: 60f, light: 75f);
}
```

### Check Chunk Dirty Status
```csharp
var dirtyFlags = SystemAPI.GetBuffer<BiomeChunkDirtyFlag>(biomeEntity);
var isDirty = dirtyFlags[chunkIndex].Value == 1;
```

### Create Biodeck
```csharp
entityManager.AddComponentData(entity, new BiodeckClimate {
    Temperature = 22f, Humidity = 60f, Light = 80f
});
entityManager.AddComponentData(entity, new BiodeckSimulationConfig {
    TickDivisor = 2, TargetTemperature = 22f
});
```

### Read Environmental Telemetry
```csharp
if (SystemAPI.TryGetComponent<EnvironmentalTelemetry>(agent, out var telemetry)) {
    if (telemetry.Comfort < 0.5f) {
        // Trigger migration goal
    }
}
```

## System Ordering

```
EnvironmentSystemGroup:
  1. BiomeChunkDirtyTrackingSystem (OrderFirst)
  2. TerraformingDeltaSystem
  3. FieldPropagationSystem
  4. BiomeDerivationSystem
  5. TerraformingPreviewSystem
```

## Component Checklist

**Required for Biome System**:
- ✅ `BiomeGrid` (singleton)
- ✅ `BiomeChunkMetadata` (singleton)
- ✅ `MoistureGrid` (singleton)
- ✅ `TemperatureGrid` (singleton)

**Optional Enhancements**:
- `BiomeLUT` - Fast lookup
- `SunlightGrid` - Light field
- `ChemicalField` - Atmospheric composition
- `PlanetPhysicalProfile` - Planetary constants

## Performance Targets

- Biome updates: O(k) per field change
- Chunk processing: 1-2% of chunks/second
- LUT lookup: < 0.01ms per evaluation
- Biodeck overhead: < 0.1ms/frame at 0.5 Hz

## See Also

- `BiomeTerraformingIntegrationGuide.md` - Complete guide
- `BiomeTerraformingAPI.md` - Full API reference

