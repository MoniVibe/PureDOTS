# World Editor & Analytics Systems Guide

This guide explains how to use the Warcraft III-style world editor, morphing world tree visualization, and statistics/correlation dashboard systems in PureDOTS.

## Overview

The World Editor & Analytics systems provide three main capabilities:

1. **Scenario Authoring API** - In-engine sandbox editor for creating deterministic scenarios
2. **World Tree Morphing** - Visual feedback system that morphs based on aggregate simulation data
3. **Statistics Dashboard** - Real-time analytics and correlation visualization

All systems communicate through pure data blobs and telemetry streams, remaining Burst-safe, replayable, and multiplayer-ready.

## Architecture

```
Editor World (Presentation Layer)
    ↓
Editor Bridge (Reflection + Serialization)
    ↓
PureDOTS Runtime (Deterministic Simulation)
    ↓
Aggregate Metrics → World Tree Morph + Statistics UI
```

## 1. Scenario Authoring API

### Setup

The scenario authoring system uses a separate editor ECS world to avoid chunk churn in the simulation world.

```csharp
using PureDOTS.Runtime.Scenario;
using Unity.Entities;

// Get or create editor world
var editorWorld = EditorWorldBootstrap.GetOrCreateEditorWorld();
var entityManager = editorWorld.EntityManager;

// Create scenario builder
var builder = new ScenarioBuilder(entityManager);
```

### Adding Entities

```csharp
// Load prefab entity (from your prefab registry or authoring)
Entity villagerPrefab = ...; // Your prefab entity
float3 position = new float3(10, 0, 5);

// Add entity to scenario
builder.AddEntity(villagerPrefab, position);
```

### Adding Components

```csharp
// Create entity instance
Entity entity = entityManager.CreateEntity();

// Add component with initial data
var needs = new VillagerNeeds
{
    Hunger = 50f,
    Energy = 80f,
    Morale = 75f
};
builder.AddComponent(entity, needs);
```

### Saving Scenario

```csharp
// Save scenario to JSON file
string scenarioPath = "Assets/Scenarios/MyScenario.json";
builder.SaveScenario(scenarioPath);

// Dispose builder when done
builder.Dispose();
```

### Preview Simulation

Enable preview simulation to see instant feedback:

```csharp
// In editor world, enable preview
var previewEntity = entityManager.CreateEntity();
entityManager.AddComponent<PreviewSimulationState>(previewEntity);
entityManager.SetComponentData(previewEntity, new PreviewSimulationState { Enabled = true });
```

The `PreviewSimulationSystem` will automatically:
- Create a parallel PureDOTS world
- Sync editor entities to preview world
- Run simulation for instant feedback

### Hot-Reload Blobs

Blob assets are automatically hot-reloaded in the editor:

```csharp
// BlobHotReloadSystem monitors asset changes
// When ScriptableObject sources change, blob references are rebuilt automatically
// No manual intervention needed
```

## 2. World Tree Morphing System

### Setup

The world tree morphing system requires `WorldAggregateProfile` singleton to exist.

```csharp
using PureDOTS.Runtime.Aggregate;
using Unity.Entities;

// Create aggregate profile singleton
var profileEntity = entityManager.CreateEntity();
entityManager.AddComponent<WorldAggregateProfile>(profileEntity);
entityManager.SetComponentData(profileEntity, new WorldAggregateProfile
{
    Population = 0f,
    EnergyFlux = 0f,
    Harmony = 0.5f,
    Chaos = 0f
});
```

### Reading Aggregate Metrics

```csharp
// In your system, read aggregate profile
var profile = SystemAPI.GetSingleton<WorldAggregateProfile>();

// Use metrics for visualization or game logic
float harmony = profile.Harmony; // 0-1, higher = more harmonious
float chaos = profile.Chaos;      // 0-1, higher = more chaotic
float population = profile.Population;
float energyFlux = profile.EnergyFlux;
```

### Reduction Systems

The aggregate profile is updated by reduction systems:

- **SumPopulationSystem** - Updates `Population` every 10 ticks
- **AverageMoralitySystem** - Updates `Harmony` from villager morale every 10 ticks
- **EnergyBalanceSystem** - Updates `EnergyFlux` from resources every 10 ticks

These systems run automatically when `WorldAggregateProfile` exists.

### Custom Reduction Systems

To add custom metrics:

```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnergyBalanceSystem))]
public partial struct CustomReductionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var profile = SystemAPI.GetComponentRW<WorldAggregateProfile>(
            SystemAPI.GetSingletonEntity<WorldAggregateProfile>());
        
        // Compute your custom metric
        float customMetric = ComputeCustomMetric(ref state);
        
        // Update profile (add new field to WorldAggregateProfile if needed)
        // profile.ValueRW.CustomField = customMetric;
    }
}
```

### World Tree Visualization

The `WorldTreeMorphSystem` generates procedural mesh based on profile:

```csharp
// Tree morphing happens automatically when:
// 1. WorldAggregateProfile exists
// 2. WorldTreeMorphSystem is enabled
// 3. GraphicsBuffers are initialized

// Branch parameters are driven by profile:
// branchLength = baseLength * (1 + harmony * 0.5f - chaos * 0.3f)
// color = lerp(red, green, harmony)
```

### Event Triggers

Major events trigger profile spikes:

```csharp
// WorldEventTriggerSystem detects:
// - Wars/conflicts → increases Chaos
// - Miracles → increases Harmony
// - Disasters → increases Chaos

// Events are broadcast to TelemetryStream for cross-system communication
```

## 3. Statistics & Correlation Dashboard

### Setup

The statistics system uses the existing `TelemetryStream` infrastructure:

```csharp
using PureDOTS.Runtime.Telemetry;

// TelemetryStream singleton is created automatically by WorldMetricsCollectorSystem
// No manual setup required
```

### Reading Metrics

```csharp
// Get telemetry stream entity
if (SystemAPI.TryGetSingletonEntity<TelemetryStream>(out var telemetryEntity))
{
    var metrics = SystemAPI.GetBuffer<TelemetryMetric>(telemetryEntity);
    
    // Read metrics
    foreach (var metric in metrics)
    {
        string key = metric.Key.ToString();
        float value = metric.Value;
        TelemetryMetricUnit unit = metric.Unit;
        
        // Use metric data
    }
}
```

### Custom Metrics Collection

To add custom metrics:

```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(WorldMetricsCollectorSystem))]
public partial struct CustomMetricsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<TelemetryStream>(out var telemetryEntity))
            return;
            
        var metrics = SystemAPI.GetBuffer<TelemetryMetric>(telemetryEntity);
        
        // Add custom metric
        float customValue = ComputeCustomMetric(ref state);
        metrics.AddMetric(
            new FixedString64Bytes("custom.metric_name"),
            customValue,
            TelemetryMetricUnit.Count
        );
    }
}
```

### Telemetry Streaming

The `TelemetryStreamingSystem` serializes frames into `NativeStream`:

```csharp
// Access stream from presentation layer
var streamingSystem = SystemAPI.GetSingletonRW<TelemetryStreamingSystem>();
var stream = streamingSystem.ValueRO.GetTelemetryStream();

// Read frames asynchronously
var reader = stream.AsReader();
// Process telemetry frames...
```

### Correlation Analysis

Compute correlations between metrics:

```csharp
using PureDOTS.Systems.Telemetry;

// CorrelationSystem computes Pearson correlation every 60 ticks
// Access correlation matrix via BlobAsset

// Example: Compare Morale ↔ Food
var correlationJob = new ComputeCorrelationJob
{
    X = moraleHistory,  // NativeArray<float> of morale values
    Y = foodHistory,    // NativeArray<float> of food values
    Result = new NativeArray<float>(1, Allocator.TempJob)
};

correlationJob.Schedule().Complete();

float correlation = correlationJob.Result[0]; // -1 to 1
// r > 0.7: strong positive correlation
// r < -0.7: strong negative correlation
```

### Statistics UI

The `StatisticsUISystem` coordinates UI updates:

```csharp
// UI rendering happens in MonoBehaviour/UI Toolkit layer
// StatisticsUISystem provides data coordination

// Create UI entity
var uiEntity = entityManager.CreateEntity();
entityManager.AddComponent<StatisticsUITag>(uiEntity);
entityManager.AddComponent<StatisticsUIData>(uiEntity);

// Configure UI
entityManager.SetComponentData(uiEntity, new StatisticsUIData
{
    SelectedMetric1 = new FixedString128Bytes("morale.average"),
    SelectedMetric2 = new FixedString128Bytes("hunger.average"),
    ShowCorrelationHeatmap = true,
    ShowHistogram = true,
    ShowTimeSeries = true
});
```

## Integration Examples

### Example 1: Creating a Scenario

```csharp
// In editor MonoBehaviour or system
var editorWorld = EditorWorldBootstrap.GetOrCreateEditorWorld();
var builder = new ScenarioBuilder(editorWorld.EntityManager);

// Add villagers
for (int i = 0; i < 10; i++)
{
    float3 pos = new float3(i * 2, 0, 0);
    builder.AddEntity(villagerPrefab, pos);
}

// Add resources
builder.AddEntity(resourcePrefab, new float3(5, 0, 5));

// Save scenario
builder.SaveScenario("Assets/Scenarios/MyVillage.json");
builder.Dispose();
```

### Example 2: Reading Aggregate Metrics

```csharp
// In game system
[BurstCompile]
public partial struct MyGameSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var profile = SystemAPI.GetSingleton<WorldAggregateProfile>();
        
        // React to world state
        if (profile.Harmony < 0.3f)
        {
            // World is in crisis - trigger event
        }
        
        if (profile.Chaos > 0.7f)
        {
            // High chaos - trigger disaster
        }
    }
}
```

### Example 3: Custom Telemetry Metric

```csharp
[BurstCompile]
public partial struct CustomTelemetrySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<TelemetryStream>(out var entity))
            return;
            
        var metrics = SystemAPI.GetBuffer<TelemetryMetric>(entity);
        
        // Count active jobs
        int activeJobCount = 0;
        foreach (var _ in SystemAPI.Query<RefRO<VillagerJob>>()
            .WithAll<VillagerJob>())
        {
            activeJobCount++;
        }
        
        metrics.AddMetric(
            new FixedString64Bytes("jobs.active"),
            activeJobCount,
            TelemetryMetricUnit.Count
        );
    }
}
```

## Performance Considerations

- **Update Intervals**: Reduction systems update every 10 ticks (configurable)
- **Telemetry Collection**: Metrics collected every 5 ticks
- **Correlation**: Computed every 60 ticks (1 Hz)
- **Downsampling**: Analytics computed on downsampled history (1 Hz, not per-tick)
- **Circular Buffers**: 10 minutes history = 600 entries

## Best Practices

1. **Editor World**: Always use separate editor world for authoring to avoid chunk churn
2. **Deterministic**: All serialization must be deterministic for scenario replay
3. **Burst Safety**: Keep hot paths Burst-compiled, use managed code only in presentation layer
4. **Telemetry**: Use existing `TelemetryStream` infrastructure, don't create custom telemetry systems
5. **Aggregate Updates**: Don't update `WorldAggregateProfile` directly - use reduction systems

## Troubleshooting

### Scenario Builder Not Saving

- Ensure `ScenarioBuilderState` component exists on scenario entity
- Check that `ScenarioAction` buffer exists
- Verify file path is writable

### Aggregate Profile Not Updating

- Ensure `WorldAggregateProfile` singleton exists
- Check that reduction systems are enabled
- Verify `TimeState` exists (required by reduction systems)

### Telemetry Metrics Missing

- Ensure `WorldMetricsCollectorSystem` is enabled
- Check that `TelemetryStream` singleton exists
- Verify metrics are added to buffer (not overwritten)

### Preview Simulation Not Running

- Check `PreviewSimulationState.Enabled` is true
- Verify `PreviewSimulationSystem` is enabled
- Ensure separate preview world is created successfully

## Future Extensions

- **Editor Plug-ins**: JSON/Lua → deterministic ECS commands
- **Procedural Scenario Generator**: Evolve world seeds from player creations
- **Rewindable Analytics**: Use `RewindState` to scrub metrics backward
- **Shareable Worlds**: Compressed delta snapshots (deterministic seeds + user content)

