# Hot-Reloadable Data Manifests Guide

**Purpose**: Guide for hot-reloading BlobAssets from JSON without domain reloads for instant iteration.

## Overview

BlobAssets (races, limbs, modules) can be reloaded from JSON manifest files at runtime without recompiling. Perfect for tuning focus regen, limb stats, morale decay in real time.

## Core Components

### BlobManifestVersion

```csharp
public struct BlobManifestVersion : IComponentData
{
    public uint Version;
    public uint LastReloadTick;
}
```

Singleton tracking blob manifest version for change detection.

### BlobManifestWatcher

Editor-only system that watches manifest files and triggers reloads:

```csharp
#if UNITY_EDITOR
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class BlobManifestWatcher : SystemBase
{
    // Watches JSON manifest files
    // Detects changes and triggers reload
}
#endif
```

### BlobAssetJsonConverter

Utility for converting between JSON and BlobAssets:

```csharp
// JSON → BlobAsset
BlobAssetReference<RaceSpecCatalogBlob> blobRef = 
    BlobAssetJsonConverter.FromJson<RaceSpecCatalogBlob>("Manifests/Races.json");

// BlobAsset → JSON
string json = BlobAssetJsonConverter.ToJson(blobRef);
```

## Usage Pattern

### Creating Manifest Files

Create JSON manifest files in `Assets/Manifests/`:

```json
{
  "races": [
    {
      "raceId": "human",
      "fairnessCoefficient": 0.5,
      "focusRegenRate": 2.0
    }
  ]
}
```

### Watching for Changes

`BlobManifestWatcher` automatically watches manifest files:

```csharp
// System detects file changes
// Converts JSON to BlobAsset
// Updates BlobManifestVersion singleton
// Systems react to version changes
```

### Reacting to Reloads

Systems check `BlobManifestVersion` to detect reloads:

```csharp
[BurstCompile]
public partial struct MySystem : ISystem
{
    private uint _lastManifestVersion;
    
    public void OnUpdate(ref SystemState state)
    {
        var manifestVersion = SystemAPI.GetSingleton<BlobManifestVersion>();
        
        if (manifestVersion.Version != _lastManifestVersion)
        {
            // BlobAssets reloaded - refresh references
            RefreshBlobReferences(ref state);
            _lastManifestVersion = manifestVersion.Version;
        }
    }
}
```

## Supported Blob Types

- **RaceSpec**: Race definitions and fairness coefficients
- **LimbSpec**: Limb stats and capabilities
- **ModuleSpec**: Module definitions and properties

## Integration Points

- **BlobManifestWatcher**: Editor-only system watching manifest files
- **BlobAssetJsonConverter**: JSON ↔ BlobAsset conversion utilities
- **BlobManifestVersion**: Singleton tracking reload version

## Best Practices

1. **Use JSON manifests**: Store blob data in JSON for easy editing
2. **Watch for version changes**: Check `BlobManifestVersion` to detect reloads
3. **Refresh references**: Update BlobAsset references after reload
4. **Editor-only**: Hot-reload is editor-only; builds use baked blobs

## Performance Impact

- **Zero domain reloads**: BlobAssets reloaded without recompiling
- **Instant iteration**: Designers modify world while simulation runs
- **Real-time tuning**: Focus regen, limb stats, morale decay tuned live

## References

- `PureDOTS/Packages/com.moni.puredots/Runtime/Components/BlobManifestVersion.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Authoring/BlobManifestWatcher.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Authoring/BlobAssetJsonConverter.cs`

