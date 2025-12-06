# Savegame = Multiplayer Snapshot Compatibility

## Overview

When saving single-player, write the same delta chain format you'd transmit in multiplayer.
This guarantees compatibility between save-load and online replay pipelines.

## Implementation Guidelines

### Save Format

- Use `DeterministicSerializer` for all save operations
- Save format must match multiplayer snapshot format exactly
- Components serialized in deterministic order (by ComponentTypeIndex)
- Store version numbers per component type for migration

### Load Format

- Use `DeterministicSerializer` for all load operations
- Load format must match multiplayer snapshot format exactly
- Support version migration if component versions differ

### Benefits

- Save files can be used for replay validation
- Multiplayer snapshots can be saved as "replay files"
- Same serialization code path for both single-player and multiplayer
- Deterministic save/load ensures no divergence

## Integration Points

When implementing save/load systems:

1. Use `DeterministicSerializer.SerializeComponent<T>` for saving
2. Use `DeterministicSerializer.DeserializeComponent<T>` for loading
3. Store component versions in save header
4. Support version migration during load

## Example

```csharp
// Save
var writer = new SnapshotWriter(1024, Allocator.Temp);
DeterministicSerializer.SerializeComponent(ref writer, component);
var data = writer.ToArray(Allocator.Persistent);
// Write data to file

// Load
var reader = new SnapshotReader(loadedData);
if (DeterministicSerializer.DeserializeComponent(ref reader, out ComponentType component))
{
    // Use component
}
```

