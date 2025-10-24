# Resource Registry Plan - DOTS-Native Implementation

## Overview

Replace legacy resource/storehouse registries with DOTS-native singletons and buffers to provide clean, queryable data for the simulation. This eliminates EntityManager-based lookups and provides efficient indexed access to resources and storehouses.

## Current State Analysis

### Existing Patterns
- **Resource IDs**: Currently use `FixedString64Bytes` via `ResourceTypeId.Value` component
- **Storehouse Access**: Static `StorehouseAPI` class with manual `EntityManager` queries
- **Resource Types**: `ResourceTypeCatalog` ScriptableObject exists but lacks a baker to DOTS data
- **Query Pattern**: Systems query entities directly via `EntityQuery` and component lookups

### Problems Addressed
1. No centralized resource type catalog available at runtime
2. StorehouseAPI uses EntityManager (not Burst-compatible, requires main thread)
3. No efficient index for looking up resources by type or storehouses by capacity
4. Resource type validation happens only at authoring time

## DOTS Data Layout

### 1. ResourceTypeIndex (Blob Asset)

Maps human-readable resource type IDs (`FixedString64Bytes`) to compact `ushort` indices for efficient storage and comparison.

```csharp
FixedString64Bytes id;              // Human-readable ID (e.g., "Wood", "Stone")
BlobString displayName;     // UI display name
BlobArray<byte> color;      // RGBA color for UI/debug viz
```

**Baker Input**: `ResourceTypeCatalog` ScriptableObject entries
**Baker Output**: `BlobAssetReference<ResourceTypeIndex>` singleton component
**Index Type**: `ushort` (supports up to 65,535 resource types)

### 2. ResourceRegistry (Singleton Component + Buffer)

Provides indexed access to all resource source entities by type.

**Singleton Component**:
```csharp
public struct ResourceRegistry : IComponentData
{
    public int TotalResources;          // Total count of all resource sources
    public int TotalActiveResources;   // Resources with UnitsRemaining > 0
    public uint LastUpdateTick;        // Frame synchronization
}
```

**Buffer Element**:
```csharp
public struct ResourceRegistryEntry : IBufferElementData
{
    public ushort ResourceTypeIndex;   // Index into ResourceTypeIndex
    public Entity SourceEntity;         // The resource source entity
    public float3 Position;             // Cached position for spatial queries
    public float UnitsRemaining;        // Cached state for quick filtering
}
```

**Query Patterns**:
- All resources: Query buffer elements
- By type: Filter by `ResourceTypeIndex`
- Active only: Filter by `UnitsRemaining > 0`
- Spatial: Use `Position` for distance calculations

### 3. StorehouseRegistry (Singleton Component + Buffer)

Provides indexed access to all storehouse entities with capacity information.

**Singleton Component**:
```csharp
public struct StorehouseRegistry : IComponentData
{
    public int TotalStorehouses;       // Total count
    public float TotalCapacity;        // Sum of all MaxCapacity values
    public float TotalStored;           // Sum of all TotalStored values
    public uint LastUpdateTick;
}
```

**Buffer Element**:
```csharp
public struct StorehouseRegistryEntry : IBufferElementData
{
    public Entity StorehouseEntity;     // The storehouse entity
    public float3 Position;             // Cached position
    public float TotalCapacity;         // Sum of all capacity elements
    public float TotalStored;           // Current inventory total
    public DynamicBuffer<StorehouseCapacityElement>; // Type-specific capacities
}
```

**Query Patterns**:
- Find storehouse by type: Filter by `DynamicBuffer.Contains(typeId)`
- Find available capacity: Filter by `TotalStored < TotalCapacity`
- Nearest storehouse: Sort by distance using `Position`

### 4. Resource Type Mapping

**Authoring**: ResourceTypeCatalog ScriptableObject → BlobAssetReference via baker
**Runtime**: Lookup string→index via blob asset traversal
**Stored Indices**: `ushort` for compact component data

```csharp
// Helper for converting string to index
public static ushort LookupResourceTypeIndex(
    BlobAssetReference<ResourceTypeIndex> catalog,
    FixedString64Bytes resourceId)
```

## Systems Architecture

### ResourceTypeIndexBaker

**Location**: `PureDOTS.Authoring`
**Input**: `ResourceTypeCatalog` ScriptableObject (from `PureDotsConfigAuthoring`)
**Output**: Creates singleton entity with `ResourceTypeIndex` blob reference
**Update**: Runs during SubScene baking, creates deterministic blob asset

**Implementation Notes**:
- Build blob from `ResourceTypeCatalog.entries`
- Store mappings as `NativeHashMap<FixedString64Bytes, ushort>`
- Create `BlobAssetReference<ResourceTypeIndex>` singleton

### ResourceRegistrySystem

**Update Group**: `ResourceSystemGroup` (before other resource systems)
**Schedule**: Early in group, updates registry before consumers query it
**Responsibilities**:
1. Query all entities with `ResourceSourceConfig` + `ResourceTypeId`
2. Resolve `ResourceTypeId.Value` → `ushort` index via blob lookup
3. Update `ResourceRegistry` buffer with current state
4. Cache `UnitsRemaining` and `Position` for filtering

**System Ordering**:
```
[FixedStepSimulation] ResourceSystemGroup
  ├─ ResourceRegistrySystem (updates catalog)
  ├─ ResourceGatheringSystem (queries catalog)
  ├─ ResourceDepositSystem
  └─ StorehouseInventorySystem
```

### StorehouseRegistrySystem

**Update Group**: `ResourceSystemGroup` (before StorehouseInventorySystem)
**Responsibilities**:
1. Query all entities with `StorehouseConfig` + `StorehouseInventory`
2. Copy state into `StorehouseRegistry` buffer
3. Cache `Position`, `TotalCapacity`, `TotalStored`

**System Ordering**:
```
ResourceSystemGroup
  ├─ StorehouseRegistrySystem (updates catalog)
  ├─ ResourceDepositSystem (queries catalog)
  └─ StorehouseInventorySystem (updates storehouse state)
```

## Migration Strategy

### Phase 1: Add New Components (Non-Breaking)
- Create `ResourceTypeIndex` blob asset structures
- Create `ResourceRegistry` and `StorehouseRegistry` components
- Create baker for `ResourceTypeCatalog`
- Add registry systems (update buffers, don't break existing code)

### Phase 2: Update StorehouseAPI
Replace static methods with system-based queries:

**Before**:
```csharp
StorehouseAPI.TryDeposit(entityManager, storehouse, typeId, amount, out accepted);
```

**After**:
```csharp
var registry = SystemAPI.GetSingletonRW<StorehouseRegistry>();
ref var storehouseEntry = ref registry.ValueRW.Entries[index];
// Direct buffer access, Burst-compatible
```

### Phase 3: Migrate Consumer Systems
- Update `ResourceGatheringSystem` to use `ResourceRegistry` buffer
- Update `ResourceDepositSystem` to use `StorehouseRegistry` buffer
- Update `VillagerJobAssignmentSystem` to query registries instead of entity queries

### Phase 4: Deprecate Legacy APIs
- Mark `StorehouseAPI` as `[Obsolete]`
- Update tests to use new registry patterns
- Remove legacy code after migration window

## API Contracts for Consumers

### Villager Systems

**Requirement**: Find nearest resource source by type
```csharp
var registry = SystemAPI.GetSingleton<ResourceRegistry>();
var entries = SystemAPI.GetBuffer<ResourceRegistryEntry>(registryEntity);

foreach (var entry in entries)
{
    if (entry.ResourceTypeIndex == targetType && entry.UnitsRemaining > 0)
    {
        var distSq = math.distancesq(entry.Position, villagerPos);
        // Track nearest
    }
}
```

### UI Systems

**Requirement**: Display total resources/storage
```csharp
var storehouseReg = SystemAPI.GetSingleton<StorehouseRegistry>();
float totalWood = 0f;
foreach (var entry in storehouseEntries)
{
    // Query entry's buffer for "Wood" type
    // Sum amounts
}
```

### Job Board Systems

**Requirement**: Validate storehouse capacity before assignment
```csharp
var storehouseReg = SystemAPI.GetSingleton<StorehouseRegistry>();
foreach (var entry in storehouseEntries)
{
    if (entry.TotalStored < entry.TotalCapacity)
    {
        // Available capacity found
    }
}
```

## Indexes & Performance

### Expected Indexes

1. **ResourceRegistry**:
   - ResourceTypeIndex → List of entities (via buffer filtering)
   - Spatial: Position → Distance queries (Linear scan acceptable for small sets)

2. **StorehouseRegistry**:
   - TotalCapacity → Sort descending for largest-first queries
   - Availability: TotalStored < TotalCapacity → Filter for capacity

3. **ResourceTypeIndex**:
   - String → ushort index: HashMap lookup O(1) authoring time, blob traversal O(n) runtime
   - ushort → String: Blob array access O(1)

### Performance Assumptions

- **Resource count**: < 100 per type (linear scans acceptable)
- **Storehouse count**: < 50 total (no spatial partitioning needed)
- **Resource types**: < 100 total (blob traversal acceptable)

If scales exceed assumptions, consider:
- NativeMultiHashMap for type→entities
- Spatial hashing for resource/storehouse positioning
- Cached binary search for sorted registries

## Invariants

1. **ResourceRegistry**:
   - Buffer entries match entities with `ResourceSourceConfig`
   - All entries have valid `ResourceTypeIndex` (exist in catalog)
   - `LastUpdateTick` updated every frame

2. **StorehouseRegistry**:
   - Buffer entries match entities with `StorehouseConfig`
   - Cached `TotalStored` and `TotalCapacity` match sum of buffer elements
   - `LastUpdateTick` updated every frame

3. **ResourceTypeIndex**:
   - Singleton exists throughout simulation
   - All `ushort` indices valid and unique
   - Strings null-terminated, max 64 bytes

## Beta Feedback – Villager Job Loop Integration

- **ResourceRegistryEntry**
  - Add `byte ActiveTickets` to track villager job reservations per source (clamped to `MaxSimultaneousWorkers`).
  - Add `byte ClaimFlags` (bit 0 = PlayerClaim, bit 1 = VillagerReserved) so villagers can yield to player interactions without additional queries.
  - Add `uint LastMutationTick` to capture deterministic ordering when multiple systems contest the same source.

```csharp
public struct ResourceRegistryEntry : IBufferElementData
{
    public ushort ResourceTypeIndex;
    public Entity SourceEntity;
    public float3 Position;
    public float UnitsRemaining;
    public byte ActiveTickets;
    public byte ClaimFlags;
    public uint LastMutationTick;
}
```

- **StorehouseRegistryEntry**
  - Replace the (invalid) `DynamicBuffer<StorehouseCapacityElement>` field with a fixed list summary so consumers can reason about per-type capacity without extra entity lookups.
  - Track `Reserved` amounts to prevent double-counting between villager tickets and player deposits.

```csharp
public struct StorehouseRegistryCapacitySummary
{
    public ushort ResourceTypeIndex;
    public float Capacity;
    public float Stored;
    public float Reserved;
}

public struct StorehouseRegistryEntry : IBufferElementData
{
    public Entity StorehouseEntity;
    public float3 Position;
    public float TotalCapacity;
    public float TotalStored;
    public FixedList32Bytes<StorehouseRegistryCapacitySummary> TypeSummaries;
    public uint LastMutationTick;
}
```

These additions let the villager job loop create deterministic job tickets, honour player priority, and choose viable drop-off targets using only the registry buffers.

## Update Order

```
[FixedStepSimulation] ResourceSystemGroup
  1. ResourceTypeIndexSystem (ensures singleton exists)
  2. ResourceRegistrySystem (scan resources, update buffer)
  3. StorehouseRegistrySystem (scan storehouses, update buffer)
  4. ResourceGatheringSystem (reads registries)
  5. ResourceDepositSystem (reads registries)
  6. StorehouseInventorySystem (updates storehouse state)
```

## Testing Strategy

### Unit Tests (EditMode)
- ResourceTypeIndexBaker converts catalog to blob correctly
- Index lookups resolve strings ↔ ushort correctly
- Registry systems create singleton + buffer on bootstrap

### Playmode Tests
- **Resource Loop**: Gather → Deposit → Verify state in registries
- **Capacity Clamping**: Deposit beyond capacity → Verify rejection
- **Pile Merge**: Multiple deposits → Verify aggregation
- **Storehouse Events**: Deposit/withdraw → Verify LastUpdateTick increments
- **Registry Sync**: Entity spawns/despawns → Registry updates

### Test Coverage
```csharp
[Test] ResourceRegistry_UpdatesOnEntitySpawn()
[Test] ResourceRegistry_FiltersByType()
[Test] StorehouseRegistry_ClampsCapacity()
[Test] StorehouseRegistry_EmitsEvents()
[Test] ResourceTypeIndex_LookupRoundTrip()
```

## Open Questions for Beta

1. **Resource Type Migration**: Should existing `FixedString64Bytes` components be migrated to `ushort` indices at authoring time, or keep both?
   - **Decision**: Keep both initially. String-based authoring, index-based runtime lookups.

2. **Storehouse Buffer Synchronization**: Should `StorehouseRegistryEntry` contain a reference to the actual buffer or copy data?
   - **Decision**: Copy data for safety (Burst-compatible). Entity reference for accessing buffer if needed.

3. **Dynamic Type Registration**: Can resource types be added at runtime?
   - **Decision**: No. Types fixed at SubScene bake time for determinism.

4. **Registry Cleanup**: Should destroyed entities be removed from registry buffers immediately or lazily?
   - **Decision**: Immediately in same frame. Registry systems query entities directly.

5. **Index Limits**: Is 65,535 resource types sufficient?
   - **Decision**: Yes. Current catalog has < 10 types.

## TruthSource Contract Mapping

This implementation replaces legacy registries per PureDOTS_TODO.md:40-43:

- **WorldServices/RegistrySystems**: Replaced with singleton + buffer patterns
- **Domain-specific registries**: Resources and storehouses now use DynamicBuffer
- **Bridge shims**: Eliminated by direct DOTS-native access

The registries provide deterministic, queryable data for villager AI, UI systems, and job assignment logic.

