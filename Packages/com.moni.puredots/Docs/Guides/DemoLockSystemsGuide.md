# Demo Lock Systems - Integration Guide

This guide documents the safety and tooling systems implemented for demo lock readiness. These systems are invisible to players but critical for stability, debugging, and modding support.

## Table of Contents

1. [Error Handling / Diagnostics Layer](#error-handling--diagnostics-layer)
2. [Type Reflection Index](#type-reflection-index)
3. [Telemetry Export](#telemetry-export)
4. [Scenario Serializer v2](#scenario-serializer-v2)
5. [CI Automation](#ci-automation)
6. [Documentation Mirror](#documentation-mirror)
7. [Crash Recovery](#crash-recovery)

---

## Error Handling / Diagnostics Layer

**Location**: `PureDOTS.Runtime.Diagnostics`

### Purpose

Catches invalid data, broken archetypes, null blobs, and component bounds violations before they cascade into gameplay issues.

### Usage

The diagnostics system runs automatically in `PresentationSystemGroup`. Configure it via the `DiagnosticsConfig` singleton:

```csharp
// Create/update config singleton
var configEntity = SystemAPI.GetSingletonEntity<DiagnosticsConfig>();
var config = SystemAPI.GetComponentRW<DiagnosticsConfig>(configEntity);
config.ValueRW.EnableBlobValidation = true;
config.ValueRW.EnableRegistryValidation = true;
config.ValueRW.MaxErrorsPerCategory = 10;
```

### Reading Diagnostics

Errors are surfaced via `DebugDisplayData`:

```csharp
var debugData = SystemAPI.GetSingleton<DebugDisplayData>();
if (debugData.DiagnosticsErrorCount > 0)
{
    Debug.LogWarning($"Diagnostics: {debugData.DiagnosticsAlertText}");
    // Check specific error counts:
    // - debugData.DiagnosticsBlobErrors
    // - debugData.DiagnosticsRegistryErrors
    // - debugData.DiagnosticsBoundsErrors
}
```

### Adding Custom Checks

Extend `DiagnosticChecks` with static validation helpers:

```csharp
public static bool ValidateCustomComponent(MyComponent component, out string error)
{
    error = null;
    if (component.Value < 0)
    {
        error = "MyComponent.Value cannot be negative";
        return false;
    }
    return true;
}
```

---

## Type Reflection Index

**Location**: `PureDOTS.Editor.Reflection` (generator), `PureDOTS.Runtime.Reflection` (runtime)

### Purpose

Centralized registry of every component, buffer, system, and blob type. Used by scenario builder, mod tools, and documentation generation.

### Generating the Index

**Editor Menu**: `PureDOTS/Generate Type Reflection Index`

**CI Script**: `CI/generate_reflection_index.sh`

The index is written to `Packages/com.moni.puredots/Generated/TypeReflectionIndex.json`.

### Runtime Usage

```csharp
// Load index (done automatically on first access)
if (!TypeReflectionIndex.Load())
{
    Debug.LogError("Failed to load Type Reflection Index");
    return;
}

// Query components
foreach (var component in TypeReflectionIndex.GetComponents())
{
    Debug.Log($"Component: {component.Name} in {component.Namespace}");
}

// Find specific component
var timeState = TypeReflectionIndex.FindComponent("PureDOTS.Runtime.Components.TimeState");
if (timeState != null)
{
    Debug.Log($"TimeState has {timeState.Fields.Count} fields");
}

// Query systems
foreach (var system in TypeReflectionIndex.GetSystems())
{
    if (system.IsBurstCompiled)
    {
        Debug.Log($"Burst system: {system.Name}");
    }
}
```

### Integration Points

- **Scenario Builder**: Uses index to validate component types in scenario definitions
- **Mod Tools**: Exposes component/system discovery API
- **Documentation Generator**: Reads index to generate markdown docs

---

## Telemetry Export

**Location**: `PureDOTS.Systems.Telemetry.TelemetryExportSystem`

### Purpose

Exports performance metrics, simulation hashes, and AI stats to file for analysis and CI dashboards.

### File Format

JSON Lines (one object per tick):

```json
{"Tick":1234,"Timestamp":"2025-01-01T12:00:00Z","TelemetryStream":{...},"Metrics":[...],"FrameTimings":[...]}
{"Tick":1235,"Timestamp":"2025-01-01T12:00:01Z","TelemetryStream":{...},"Metrics":[...],"FrameTimings":[...]}
```

### Output Location

Files are written to `Application.persistentDataPath/Telemetry/metrics_{timestamp}.jsonl`

### Enabling Export

Export runs automatically when `TelemetryStream` singleton exists. To disable:

```csharp
// Future: Add toggle to DebugDisplayData
// For now, export is always enabled when telemetry stream exists
```

### Reading Exported Data

```csharp
// Parse JSON lines file
var lines = File.ReadAllLines("path/to/metrics.jsonl");
foreach (var line in lines)
{
    var entry = JsonUtility.FromJson<TelemetryEntry>(line);
    Debug.Log($"Tick {entry.Tick}: {entry.Metrics.Count} metrics");
}
```

### Metrics Included

- `TelemetryStream`: Version, tick, time budget metrics
- `TelemetryMetric`: Custom metrics from systems (key-value pairs)
- `FrameTimingSample`: System group timings, budget exceeded flags
- `AllocationDiagnostics`: GC collections, memory stats
- `SimulationHash`: Determinism validation hash (future)

---

## Scenario Serializer v2

**Location**: `PureDOTS.Runtime.Devtools.Scenario`

### Purpose

Versioned scenario format with migration support for modding and persistence. Captures full world state (entities, components, buffers, singletons).

### File Format

```json
{
  "version": 2,
  "formatVersion": "2.0.0",
  "metadata": {
    "tick": 1234,
    "worldHash": "hash_...",
    "timestamp": "2025-01-01T12:00:00Z"
  },
  "entities": [
    {
      "EntityIndex": 1,
      "EntityVersion": 1,
      "Components": [...],
      "Buffers": [...]
    }
  ],
  "singletons": {
    "TimeState": "{...}"
  }
}
```

### Serializing a Scenario

```csharp
var world = World.DefaultGameObjectInjectionWorld;
if (!ScenarioSerializerV2.Serialize(world, "Scenarios/my_scenario.json", out var error))
{
    Debug.LogError($"Failed to serialize: {error}");
}
```

### Loading a Scenario

**CLI**: `Unity -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.LoadScenarioV2 --scenario <path>`

**Code**:
```csharp
var world = World.DefaultGameObjectInjectionWorld;
if (!ScenarioLoaderV2.Load("Scenarios/my_scenario.json", world, out var error))
{
    Debug.LogError($"Failed to load: {error}");
}
```

### Migration Support

Scenarios with `version < 2` are automatically migrated:

```csharp
// Migration happens automatically in ScenarioLoaderV2.Load()
// Custom migrations can be added in ScenarioLoaderV2.MigrateScenario()
```

### Version Detection

The loader checks `version` field:
- `version < CurrentVersion`: Apply migrations
- `version == CurrentVersion`: Load directly
- `version > CurrentVersion`: Error (newer format not supported)

---

## CI Automation

**Location**: `PureDOTS/CI/`

### Scripts

#### `run_playmode_tests.sh`

Runs EditMode and PlayMode tests. Supports scenario execution:

```bash
CI/run_playmode_tests.sh --scenario Scenarios/test.json
```

#### `validate_scenario.sh`

Validates scenario determinism by comparing tick hashes:

```bash
CI/validate_scenario.sh Scenarios/test.json [expected_hash_file]
```

Compares output hash against `CI/scenario_hashes.json` golden file.

#### `test_package.sh`

Runs tests for a specific package:

```bash
CI/test_package.sh com.moni.puredots
```

#### `generate_reflection_index.sh`

Generates Type Reflection Index before builds:

```bash
CI/generate_reflection_index.sh
```

#### `generate_docs.sh`

Generates markdown documentation from reflection index:

```bash
CI/generate_docs.sh
```

### CI Integration

Add to your CI pipeline:

```yaml
# Example GitHub Actions
- name: Generate Reflection Index
  run: bash CI/generate_reflection_index.sh

- name: Generate Documentation
  run: bash CI/generate_docs.sh

- name: Run Tests
  run: bash CI/run_playmode_tests.sh

- name: Validate Scenarios
  run: bash CI/validate_scenario.sh Scenarios/demo.json
```

---

## Documentation Mirror

**Location**: `PureDOTS.Editor.Documentation.DocumentationGenerator`

### Purpose

Auto-generates markdown documentation from Type Reflection Index for components, buffers, and systems.

### Generating Documentation

**Editor Menu**: `PureDOTS/Generate Documentation`

**CI Script**: `CI/generate_docs.sh`

### Output Files

Generated in `Packages/com.moni.puredots/Docs/Generated/`:

- `Components.md`: All `IComponentData` types with fields
- `Buffers.md`: All `IBufferElementData` types with capacity
- `Systems.md`: All `ISystem` types with execution order

### Format

Each file includes:
- Summary table (name, namespace, metadata)
- Detailed sections with field/attribute information
- Execution order for systems

### Integration

Documentation is regenerated on CI builds. Files are gitignored (regenerated, not committed).

---

## Crash Recovery

**Location**: `PureDOTS.Runtime.Recovery`, `PureDOTS.Systems.Recovery`

### Purpose

Auto-saves world state snapshots every N ticks in a ring buffer. Enables recovery after crashes.

### Configuration

```csharp
// Create/update config singleton
var configEntity = SystemAPI.GetSingletonEntity<CrashRecoveryConfig>();
var config = SystemAPI.GetComponentRW<CrashRecoveryConfig>(configEntity);
config.ValueRW.SnapshotIntervalTicks = 1000;  // Save every 1000 ticks
config.ValueRW.RingBufferSize = 10;            // Keep last 10 snapshots
config.ValueRW.AutoSaveEnabled = true;
```

### Snapshot Format

Binary format using `TimeStreamWriter`:
- `snapshot_{tick}.dat`: Binary world state
- `snapshot_{tick}.meta`: JSON metadata (tick, timestamp, entity count, hash)

### Finding Latest Snapshot

```csharp
if (CrashRecoveryLoader.TryFindLatestSnapshot(out var snapshot))
{
    Debug.Log($"Latest snapshot: tick {snapshot.Tick}, {snapshot.EntityCount} entities");
    Debug.Log($"Timestamp: {snapshot.Timestamp}");
    Debug.Log($"File: {snapshot.FilePath}");
}
```

### Loading a Snapshot

```csharp
var world = World.DefaultGameObjectInjectionWorld;
if (!CrashRecoveryLoader.LoadSnapshot(world, snapshot.FilePath, out var error))
{
    Debug.LogError($"Failed to load snapshot: {error}");
}
```

### Recovery Workflow

1. On startup, check for latest snapshot:
   ```csharp
   if (CrashRecoveryLoader.TryFindLatestSnapshot(out var snapshot))
   {
       // Offer recovery option to user
       if (userWantsRecovery)
       {
           CrashRecoveryLoader.LoadSnapshot(world, snapshot.FilePath, out _);
       }
   }
   ```

2. Snapshots are automatically cleaned up when ring buffer is full (oldest deleted first)

### Snapshot Storage

Snapshots are stored in `Application.persistentDataPath/Recovery/`

---

## Integration Patterns

### Adding Custom Diagnostics

1. Add validation helper to `DiagnosticChecks`:
   ```csharp
   public static bool ValidateMyComponent(MyComponent comp, out string error) { ... }
   ```

2. Call from `DiagnosticsSystem.OnUpdate()`:
   ```csharp
   foreach (var (comp, entity) in SystemAPI.Query<RefRO<MyComponent>>().WithEntityAccess())
   {
       if (!DiagnosticChecks.ValidateMyComponent(comp.ValueRO, out var error))
       {
           // Log error, update DebugDisplayData
       }
   }
   ```

### Adding Custom Telemetry

1. Get telemetry buffer:
   ```csharp
   var telemetryEntity = SystemAPI.GetSingletonEntity<TelemetryStream>();
   var metrics = SystemAPI.GetBuffer<TelemetryMetric>(telemetryEntity);
   ```

2. Add metric:
   ```csharp
   metrics.AddMetric("my.custom.metric", 42.0f, TelemetryMetricUnit.Count);
   ```

3. Metric is automatically exported by `TelemetryExportSystem`

### Extending Scenario Format

1. Add fields to `ScenarioV2Data`:
   ```csharp
   public class ScenarioV2Data
   {
       // ... existing fields ...
       public MyCustomData CustomData;  // New field
   }
   ```

2. Update serializer to include custom data:
   ```csharp
   scenario.CustomData = SerializeMyCustomData(world);
   ```

3. Update loader to restore custom data:
   ```csharp
   RestoreMyCustomData(world, scenario.CustomData);
   ```

4. Increment version if breaking change:
   ```csharp
   public const int CurrentVersion = 3;  // Bump version
   ```

---

## Best Practices

1. **Diagnostics**: Enable all checks in development, disable expensive ones in production builds
2. **Telemetry**: Use descriptive metric keys (e.g., `"timing.system_name"`, `"memory.allocated"`)
3. **Scenarios**: Always version your scenario format; provide migrations for older versions
4. **Crash Recovery**: Set `SnapshotIntervalTicks` based on tick rate (e.g., 1000 ticks ≈ 16 seconds at 60 TPS)
5. **CI**: Run reflection index generation before documentation generation
6. **Documentation**: Regenerate docs after adding new components/systems

---

## Troubleshooting

### Diagnostics Not Running

- Check `DiagnosticsConfig.AutoSaveEnabled` is true
- Verify `DiagnosticsSystem` is in `PresentationSystemGroup`
- Check console for errors

### Telemetry Export Not Writing Files

- Verify `TelemetryStream` singleton exists
- Check file permissions for `Application.persistentDataPath/Telemetry/`
- Check console for file I/O errors

### Scenario Load Fails

- Verify scenario file exists and is valid JSON
- Check version compatibility (must be ≤ CurrentVersion)
- Review migration logs for errors

### Crash Recovery Not Saving

- Check `CrashRecoveryConfig.AutoSaveEnabled` is true
- Verify `SnapshotIntervalTicks` is reasonable (not too large)
- Check disk space in `Application.persistentDataPath/Recovery/`

---

## See Also

- `TRI_PROJECT_BRIEFING.md`: Project overview and coding patterns
- `Docs/FoundationGuidelines.md`: DOTS 1.4 coding standards
- `Docs/Guides/sanity.md`: System execution order and sanity checks

