# Demo Lock Systems - API Reference

Quick reference for programmatic access to demo lock systems.

## Diagnostics API

### DiagnosticsConfig

```csharp
public struct DiagnosticsConfig : IComponentData
{
    public bool EnableArchetypeValidation;
    public bool EnableBlobValidation;
    public bool EnableRegistryValidation;
    public bool EnableComponentBoundsValidation;
    public int MaxErrorsPerCategory;
    public int MaxTotalErrorsPerTick;
    
    public static DiagnosticsConfig Default { get; }
}
```

### DiagnosticChecks

```csharp
public static class DiagnosticChecks
{
    public static bool ValidateBlobReference<T>(BlobAssetReference<T> blobRef, out string error) where T : struct;
    public static bool ValidateFloat(float value, string fieldName, out string error);
    public static bool ValidateFloat3(float3 value, string fieldName, out string error);
    public static bool ValidateRegistryEntry(Entity entity, EntityManager entityManager, RegistryKind expectedKind, out string error);
    public static bool ValidateArchetype(Entity entity, EntityManager entityManager, out string error);
}
```

### DebugDisplayData Fields

```csharp
public struct DebugDisplayData : IComponentData
{
    // Diagnostics fields
    public int DiagnosticsErrorCount;
    public int DiagnosticsArchetypeErrors;
    public int DiagnosticsBlobErrors;
    public int DiagnosticsRegistryErrors;
    public int DiagnosticsBoundsErrors;
    public FixedString512Bytes DiagnosticsAlertText;
}
```

---

## Type Reflection Index API

### TypeReflectionIndex

```csharp
public static class TypeReflectionIndex
{
    public static bool Load();
    public static IEnumerable<ComponentInfo> GetComponents();
    public static IEnumerable<BufferInfo> GetBuffers();
    public static IEnumerable<SystemInfo> GetSystems();
    public static IEnumerable<BlobTypeInfo> GetBlobTypes();
    public static ComponentInfo FindComponent(string fullName);
    public static SystemInfo FindSystem(string fullName);
}
```

### ComponentInfo

```csharp
public class ComponentInfo
{
    public string FullName;
    public string Namespace;
    public string Name;
    public List<FieldInfo> Fields;
}
```

### SystemInfo

```csharp
public class SystemInfo
{
    public string FullName;
    public string Namespace;
    public string Name;
    public string UpdateInGroup;
    public bool OrderFirst;
    public bool OrderLast;
    public string UpdateAfter;
    public string UpdateBefore;
    public bool IsBurstCompiled;
}
```

---

## Telemetry Export API

### TelemetryStream

```csharp
public struct TelemetryStream : IComponentData
{
    public uint Version;
    public uint LastTick;
    public float RealTimeMs;
    public float SimTimeMs;
    public float CompressionFactor;
    public float DriftMs;
}
```

### TelemetryMetric

```csharp
public struct TelemetryMetric : IBufferElementData
{
    public FixedString64Bytes Key;
    public float Value;
    public TelemetryMetricUnit Unit;
}
```

### TelemetryBufferExtensions

```csharp
public static class TelemetryBufferExtensions
{
    public static void AddMetric(this DynamicBuffer<TelemetryMetric> buffer, 
        in FixedString64Bytes key, float value, TelemetryMetricUnit unit = TelemetryMetricUnit.Count);
}
```

### TelemetryFileWriter

```csharp
public class TelemetryFileWriter : IDisposable
{
    public TelemetryFileWriter(string filePath);
    public void WriteTick(uint tick, TelemetryStream telemetryStream, 
        NativeArray<TelemetryMetric> metrics, NativeArray<FrameTimingSample> frameTimings,
        AllocationDiagnostics allocationDiagnostics, uint simulationHash = 0);
    public void Dispose();
}
```

---

## Scenario Serializer v2 API

### ScenarioSerializerV2

```csharp
public static class ScenarioSerializerV2
{
    public const int CurrentVersion = 2;
    public const string FormatVersion = "2.0.0";
    
    public static bool Serialize(World world, string filePath, out string error);
}
```

### ScenarioLoaderV2

```csharp
public static class ScenarioLoaderV2
{
    public static bool Load(string filePath, World world, out string error);
}
```

### ScenarioV2Data

```csharp
public class ScenarioV2Data
{
    public int Version;
    public string FormatVersion;
    public ScenarioMetadata Metadata;
    public List<EntityData> Entities;
    public Dictionary<string, object> Singletons;
}
```

---

## Crash Recovery API

### CrashRecoveryConfig

```csharp
public struct CrashRecoveryConfig : IComponentData
{
    public uint SnapshotIntervalTicks;
    public int RingBufferSize;
    public bool AutoSaveEnabled;
    
    public static CrashRecoveryConfig Default { get; }
}
```

### CrashRecoveryLoader

```csharp
public static class CrashRecoveryLoader
{
    public static bool TryFindLatestSnapshot(out SnapshotInfo snapshotInfo);
    public static bool LoadSnapshot(World world, string snapshotPath, out string error);
}

public struct SnapshotInfo
{
    public string FilePath;
    public uint Tick;
    public string Timestamp;
    public int EntityCount;
    public uint Hash;
}
```

---

## Editor APIs

### TypeReflectionIndexGenerator

```csharp
public static class TypeReflectionIndexGenerator
{
    [MenuItem("PureDOTS/Generate Type Reflection Index")]
    public static void GenerateIndex();
}
```

### DocumentationGenerator

```csharp
public static class DocumentationGenerator
{
    [MenuItem("PureDOTS/Generate Documentation")]
    public static void GenerateDocumentation();
}
```

---

## CLI Entry Points

### ScenarioRunnerEntryPoints

```csharp
public static class ScenarioRunnerEntryPoints
{
    // Run scenario from args
    public static void RunScenarioFromArgs();
    // Usage: --scenario <path> [--report <path>]
    
    // Load scenario v2
    public static void LoadScenarioV2();
    // Usage: --scenario <path>
    
    // Run scale test
    public static void RunScaleTest();
    // Usage: --scenario <name> [--metrics <path>] [--target-ms <ms>]
}
```

---

## Usage Examples

### Adding Custom Telemetry

```csharp
[BurstCompile]
public partial struct MySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var telemetryEntity = SystemAPI.GetSingletonEntity<TelemetryStream>();
        var metrics = SystemAPI.GetBuffer<TelemetryMetric>(telemetryEntity);
        
        metrics.AddMetric("my.system.processed", processedCount, TelemetryMetricUnit.Count);
        metrics.AddMetric("my.system.duration", durationMs, TelemetryMetricUnit.DurationMilliseconds);
    }
}
```

### Validating Custom Component

```csharp
public static class DiagnosticChecks
{
    public static bool ValidateMyComponent(MyComponent comp, out string error)
    {
        error = null;
        if (comp.Value < 0 || comp.Value > 100)
        {
            error = $"MyComponent.Value ({comp.Value}) out of range [0, 100]";
            return false;
        }
        return true;
    }
}
```

### Querying Type Reflection Index

```csharp
// Find all Burst-compiled systems
var burstSystems = TypeReflectionIndex.GetSystems()
    .Where(s => s.IsBurstCompiled)
    .ToList();

// Find component by name
var timeState = TypeReflectionIndex.FindComponent("PureDOTS.Runtime.Components.TimeState");
if (timeState != null)
{
    Debug.Log($"TimeState namespace: {timeState.Namespace}");
    Debug.Log($"TimeState fields: {timeState.Fields.Count}");
}
```

### Configuring Crash Recovery

```csharp
// In bootstrap or system OnCreate
var configEntity = state.EntityManager.CreateEntity();
state.EntityManager.AddComponent<CrashRecoveryConfig>(configEntity);
state.EntityManager.SetComponentData(configEntity, new CrashRecoveryConfig
{
    SnapshotIntervalTicks = 500,  // Save every 500 ticks (~8 seconds at 60 TPS)
    RingBufferSize = 20,           // Keep last 20 snapshots
    AutoSaveEnabled = true
});
```

---

## File Locations

- **Type Reflection Index**: `Packages/com.moni.puredots/Generated/TypeReflectionIndex.json`
- **Telemetry Export**: `Application.persistentDataPath/Telemetry/metrics_{timestamp}.jsonl`
- **Crash Recovery Snapshots**: `Application.persistentDataPath/Recovery/snapshot_{tick}.dat`
- **Generated Documentation**: `Packages/com.moni.puredots/Docs/Generated/`

