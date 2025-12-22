# Storehouse System Summary

**Status:** Active - Core Resource Storage
**Category:** Core - Economy / Resources
**Audience:** Implementers / Architects / Designers
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Executive Summary

**Purpose:** Storehouse system provides centralized resource storage for villages/colonies. Acts as the "village bank" - single source of truth for resource inventory with capacity limits, reservations, and quality tracking.

**Current State:**
- ✅ Core components implemented (StorehouseConfig, StorehouseInventory, buffers)
- ✅ Registry system working (StorehouseRegistry for efficient queries)
- ✅ Deposit/withdrawal systems functional
- ✅ Reservation system working (StorehouseJobReservation, StorehouseReservationItem)
- ✅ Rewind support (StorehouseInventoryTimeAdapterSystem)
- ⚠️ API wrapper incomplete (direct buffer manipulation used)
- ⚠️ No event system (UI updates via reactive queries)

**Key Design Principle:** Single write path for resource totals, no direct pile manipulation. Capacity enforced, reservations prevent double-booking.

---

## File Mapping

### Core Components
- `Packages/com.moni.puredots/Runtime/Runtime/ResourceComponents.cs`
  - `StorehouseConfig` (shred rate, input/output rate, label)
  - `StorehouseInventory` (total stored, total capacity, item type count, shredding flag)
  - `StorehouseInventoryItem` (buffer: resource type ID, amount, reserved, tier ID, average quality)
  - `StorehouseCapacityElement` (buffer: resource type ID, max capacity)
  - `StorehouseJobReservation` (reserved capacity, last mutation tick)
  - `StorehouseReservationItem` (buffer: resource type index, reserved amount)

### Registry Components
- `Packages/com.moni.puredots/Runtime/Runtime/ResourceComponents.cs`
  - `StorehouseRegistry` (singleton: total storehouses, total capacity, total stored, spatial tracking)
  - `StorehouseRegistryEntry` (buffer: storehouse entity, position, capacity summaries, spatial cell ID)
  - `StorehouseRegistryCapacitySummary` (per-resource-type capacity, stored, reserved, tier, quality)

### Systems
- `Packages/com.moni.puredots/Runtime/Systems/StorehouseSystems.cs`
  - `StorehouseInventorySystem` (aggregates inventory state, handles shredding)
  - `StorehouseDepositProcessingSystem` (placeholder for future queued deposits)
  - `StorehouseHistoryRecordingSystem` (records history samples for rewind)
  - `StorehouseWithdrawalProcessingSystem` (handles villager withdrawals)

- `Packages/com.moni.puredots/Runtime/Systems/StorehouseRegistrySystem.cs`
  - `StorehouseRegistrySystem` (maintains registry of all storehouses with capacity information)

- `Packages/com.moni.puredots/Runtime/Systems/StorehouseInventoryTimeAdapterSystem.cs`
  - `StorehouseInventoryTimeAdapterSystem` (snapshots inventory for rewind playback)

- `Packages/com.moni.puredots/Runtime/Systems/ResourceSystems.cs`
  - `ResourceDepositSystem` (handles villager deposits into storehouses)
  - `ResourceReservationBootstrapSystem` (adds reservation components to storehouses)

### API Helpers
- `Packages/com.moni.puredots/Runtime/Runtime/Resource/StorehouseApi.cs`
  - `StorehouseApi.TryDeposit()` (simplified deposit helper)
  - `StorehouseApi.TryWithdraw()` (simplified withdrawal helper)

- `godgame/Assets/Scripts/Godgame/Resources/StorehouseApi.cs`
  - Godgame-specific API wrapper (more complete implementation)

### Authoring
- `Assets/Projects/Godgame/Scripts/Godgame/Authoring/StorehouseAuthoring.cs`
  - `StorehouseAuthoring` MonoBehaviour (bakes StorehouseConfig, StorehouseInventory, buffers)

### Documentation
- `godgame/Docs/Concepts/Implemented/Economy/Storehouse_API.md` - API contract design document

---

## Specifications

### Data Model

#### StorehouseConfig

```csharp
public struct StorehouseConfig : IComponentData
{
    public float ShredRate;              // Decay rate per second (for testing/debugging)
    public int MaxShredQueueSize;        // Max items in shred queue
    public float InputRate;              // Input processing rate
    public float OutputRate;             // Output processing rate
    public FixedString64Bytes Label;     // Storehouse label/name
}
```

#### StorehouseInventory

```csharp
public struct StorehouseInventory : IComponentData
{
    public float TotalStored;            // Total amount stored across all resource types
    public float TotalCapacity;          // Total capacity across all resource types
    public int ItemTypeCount;            // Number of different resource types stored
    public byte IsShredding;             // Whether shredding is active (debug/test)
    public uint LastUpdateTick;          // Last tick inventory was updated
}
```

#### StorehouseInventoryItem (Buffer)

```csharp
[InternalBufferCapacity(32)]
public struct StorehouseInventoryItem : IBufferElementData
{
    public FixedString64Bytes ResourceTypeId;  // Resource type identifier (string)
    public float Amount;                       // Stored amount
    public float Reserved;                     // Reserved amount (for jobs/construction)
    public byte TierId;                        // Quality tier (ResourceQualityTier)
    public ushort AverageQuality;              // Average quality (0-600)
}
```

#### StorehouseCapacityElement (Buffer)

```csharp
[InternalBufferCapacity(16)]
public struct StorehouseCapacityElement : IBufferElementData
{
    public FixedString64Bytes ResourceTypeId;  // Resource type identifier
    public float MaxCapacity;                  // Maximum capacity for this resource type
}
```

**Note:** Capacity is per-resource-type, not global. Each resource type can have different capacity limits.

#### StorehouseJobReservation

```csharp
public struct StorehouseJobReservation : IComponentData
{
    public float ReservedCapacity;       // Total reserved capacity
    public uint LastMutationTick;        // Last tick reservations changed
}
```

#### StorehouseReservationItem (Buffer)

```csharp
[InternalBufferCapacity(16)]
public struct StorehouseReservationItem : IBufferElementData
{
    public ushort ResourceTypeIndex;     // Resource type index (not ID string)
    public float Reserved;               // Reserved amount for this resource type
}
```

**Note:** Reservations use `ResourceTypeIndex` (ushort), while inventory items use `ResourceTypeId` (FixedString64Bytes). Conversion required via `ResourceTypeIndex` catalog.

### Registry Model

#### StorehouseRegistry (Singleton)

```csharp
public struct StorehouseRegistry : IComponentData
{
    public int TotalStorehouses;         // Total number of storehouses
    public float TotalCapacity;          // Aggregate total capacity
    public float TotalStored;            // Aggregate total stored
    public uint LastUpdateTick;          // Last tick registry was updated
    public uint LastSpatialVersion;      // Spatial grid version when last updated
    public int SpatialResolvedCount;     // Storehouses with spatial residency resolved
    public int SpatialFallbackCount;     // Storehouses using fallback spatial lookup
    public int SpatialUnmappedCount;     // Storehouses that couldn't be mapped to spatial grid
}
```

#### StorehouseRegistryEntry (Buffer)

```csharp
public struct StorehouseRegistryEntry : IBufferElementData, IComparable<StorehouseRegistryEntry>, IRegistryEntry
{
    public Entity StorehouseEntity;      // Storehouse entity
    public float3 Position;              // Storehouse position
    public float TotalCapacity;          // Total capacity
    public float TotalStored;            // Total stored
    public FixedList64Bytes<StorehouseRegistryCapacitySummary> TypeSummaries;  // Per-resource-type summaries
    public uint LastMutationTick;        // Last tick storehouse was modified
    public int CellId;                   // Spatial grid cell ID
    public uint SpatialVersion;          // Spatial grid version when position was resolved
    public ResourceQualityTier DominantTier;  // Dominant quality tier across all resources
    public ushort AverageQuality;        // Average quality across all resources
}
```

**Purpose:** Efficient queries (find nearest storehouse, find storehouse with space for resource type, spatial queries).

---

## How It Works

### Deposit Flow

1. **Villager arrives at storehouse** (within `DepositDistance`):
   - `ResourceDepositSystem` checks villager has `VillagerJobCarryItem` buffer
   - Villager's job phase is `Delivering`, target storehouse is set

2. **Deposit processing**:
   - For each carried resource:
     - Look up resource type in `StorehouseCapacityElement` buffer (check if storehouse accepts this type)
     - Calculate available space: `capacity.MaxCapacity - storedAmount - reservedAmount`
     - Deposit amount = min(carriedAmount, availableSpace)
     - Update or create `StorehouseInventoryItem` (increment Amount)
     - Update `StorehouseInventory.TotalStored`
     - Remove from villager's `VillagerJobCarryItem` buffer

3. **Inventory aggregation**:
   - `StorehouseInventorySystem` runs after deposits
   - Recalculates `TotalStored` and `TotalCapacity` from buffers
   - Handles shredding if `IsShredding` flag is set (debug/test feature)

### Withdrawal Flow

1. **Villager requests withdrawal** (via `VillagerWithdrawRequest` buffer):
   - `StorehouseWithdrawalProcessingSystem` checks villager is near storehouse
   - Villager's `VillagerAIState.TargetEntity` points to storehouse

2. **Withdrawal processing**:
   - Find `StorehouseInventoryItem` matching resource type
   - Calculate available: `item.Amount - item.Reserved`
   - Withdraw amount = min(requestedAmount, available, villagerCapacity)
   - Update `StorehouseInventoryItem` (decrement Amount)
   - Update `StorehouseInventory.TotalStored`
   - Add to villager's `VillagerInventoryItem` buffer

### Registry Update Flow

1. **StorehouseRegistrySystem** runs every tick:
   - Queries all entities with `StorehouseConfig` + `StorehouseInventory`
   - For each storehouse:
     - Builds `StorehouseRegistryCapacitySummary` per resource type (from capacities, inventory items, reservations)
     - Resolves spatial position (uses `SpatialGridResidency` if available, falls back to transform)
     - Creates `StorehouseRegistryEntry` with summaries, position, cell ID
   - Updates `StorehouseRegistry` singleton (aggregate totals)
   - Updates `StorehouseRegistryEntry` buffer (sorted, deterministic)

2. **Registry usage**:
   - Systems query registry for efficient storehouse lookups (find nearest, find with space)
   - Spatial queries use `CellId` for fast filtering
   - `TypeSummaries` enable per-resource-type queries (find storehouse with space for "wood")

### Reservation Flow

1. **Job creates reservation** (e.g., construction site needs resources):
   - Job system creates `StorehouseReservationItem` entry
   - Sets `Reserved` amount for resource type
   - Updates `StorehouseJobReservation.LastMutationTick`

2. **Reservation enforcement**:
   - `StorehouseInventoryItem.Reserved` field tracks reserved amount
   - Available space = `Amount - Reserved`
   - Withdrawals/deposits check available space (not just Amount)

3. **Reservation cleanup**:
   - Job completion systems remove reservations
   - `StorehouseRegistrySystem` includes reservations in `TypeSummaries` (Capacity, Stored, Reserved)

### Rewind/History Flow

1. **Recording** (`StorehouseInventoryTimeAdapterSystem`):
   - Snapshots `StorehouseInventory` components each tick (or every N ticks)
   - Uses `TimeStreamHistory` for efficient storage
   - Records: TotalStored, TotalCapacity, IsShredding, LastUpdateTick

2. **Playback**:
   - Restores `StorehouseInventory` from snapshots during rewind
   - Does NOT restore buffers (StorehouseInventoryItem, StorehouseCapacityElement) - these are derived
   - **Gap:** Buffer restoration not implemented (would need full buffer snapshots)

---

## Integration Points

### Villager Job System

- **Deposits:** `ResourceDepositSystem` processes `VillagerJobCarryItem` when job phase is `Delivering`
- **Withdrawals:** `StorehouseWithdrawalProcessingSystem` processes `VillagerWithdrawRequest` buffer
- **Reservations:** Job systems create `StorehouseReservationItem` entries for construction sites, work orders

### AI Systems

- **Perception:** Storehouses have `StorehouseConfig` component, detectable via `AISensorCategory.Storehouse`
- **Sensor queries:** `AISensorUpdateSystem` finds nearby storehouses for delivery/gathering tasks
- **Registry queries:** AI systems query `StorehouseRegistry` to find storehouses with space

### Construction System

- **Reservations:** Construction sites reserve resources in target storehouse
- **Delivery:** Villagers deliver reserved resources to construction site (withdrawal from storehouse → delivery to site)

### Hand Interaction (Player)

- **RMB dump:** `StorehouseDumpRmbHandler` (or similar) handles player dumping resources into storehouse
- **Current state:** Handler exists but may be incomplete (see gaps)

### Spatial Grid

- **Registry integration:** `StorehouseRegistrySystem` resolves storehouse positions to spatial grid cells
- **Efficient queries:** Registry entries include `CellId` for fast spatial queries
- **Spatial versioning:** Registry tracks `SpatialVersion` to detect stale spatial data

---

## Gaps and Limitations

### API Wrapper Incomplete

**Problem:** Direct buffer manipulation used instead of clean API wrapper.

**Current Usage:**
```csharp
// Direct buffer manipulation (scattered across systems)
var inventory = _storehouseInventoryLookup[entity];
var items = _storehouseInventoryItemsLookup[entity];
items[i].Amount += depositAmount;
inventory.TotalStored += depositAmount;
_storehouseInventoryLookup[entity] = inventory;
```

**Desired API:**
```csharp
// Clean API wrapper
var accepted = StorehouseApi.Add(storehouseEntity, resourceTypeId, amount);
var removed = StorehouseApi.Remove(storehouseEntity, resourceTypeId, amount);
var available = StorehouseApi.Space(storehouseEntity, resourceTypeId);
```

**Impact:** Code duplication, harder to maintain, no centralized validation.

### No Event System

**Problem:** No events emitted for UI updates (OnTotalsChanged, OnCapacityChanged).

**Current State:** UI systems query storehouse components directly (reactive queries).

**Desired:** Event system for UI updates (optional, may be overkill if reactive queries work).

### Buffer Restoration in Rewind

**Problem:** `StorehouseInventoryTimeAdapterSystem` only snapshots `StorehouseInventory` component, not buffers.

**Current State:** Buffers (`StorehouseInventoryItem`, `StorehouseCapacityElement`) are not restored during rewind.

**Impact:** Rewind may not fully restore storehouse state (inventory items lost).

**Fix:** Either snapshot buffers too, or derive buffers from deterministic inputs (replay deposit/withdrawal events).

### Resource Type ID vs Index Mismatch

**Problem:** Inconsistent use of `ResourceTypeId` (string) vs `ResourceTypeIndex` (ushort).

**Current State:**
- `StorehouseInventoryItem` uses `ResourceTypeId` (FixedString64Bytes)
- `StorehouseReservationItem` uses `ResourceTypeIndex` (ushort)
- Conversion required via `ResourceTypeIndex` catalog

**Impact:** Conversion overhead, potential bugs if catalog lookup fails.

**Recommendation:** Standardize on one (prefer `ResourceTypeIndex` for performance, use `ResourceTypeId` only for authoring).

### Capacity Per-Resource-Type Logic

**Problem:** Capacity is per-resource-type, but total capacity aggregation may be confusing.

**Current State:**
- Each resource type has its own `MaxCapacity` (from `StorehouseCapacityElement`)
- `TotalCapacity` is sum of all `MaxCapacity` values
- But actual usable capacity depends on which resource types are stored

**Example:**
- Wood capacity: 500
- Ore capacity: 300
- TotalCapacity: 800
- But if wood is full (500) and ore is empty (0), you can still store 300 ore (not 800 total)

**Impact:** `TotalStored >= TotalCapacity` check may be misleading (should check per-resource-type capacity).

### No Intake Trigger Component

**Problem:** Player RMB dump may require specific interaction zone (not implemented).

**Current State:** Handler exists but may not have physical trigger zone.

**Desired:** Optional `StorehouseIntakeTrigger` component for interaction zones.

---

## Malpractices and Anti-Patterns

### Direct Buffer Manipulation

❌ **Wrong:** Direct buffer manipulation scattered across systems
```csharp
items[i].Amount += amount;
inventory.TotalStored += amount;
_storehouseInventoryLookup[entity] = inventory;
```

✅ **Right:** Use API wrapper (when implemented)
```csharp
var accepted = StorehouseApi.Add(storehouseEntity, resourceTypeId, amount);
```

### Ignoring Reservations

❌ **Wrong:** Check only `Amount`, ignore `Reserved`
```csharp
var available = item.Amount;  // Wrong! Should subtract Reserved
```

✅ **Right:** Account for reservations
```csharp
var available = math.max(0f, item.Amount - item.Reserved);
```

### Not Updating TotalStored

❌ **Wrong:** Modify buffer but forget to update `StorehouseInventory.TotalStored`
```csharp
items[i].Amount += amount;
// Forgot: inventory.TotalStored += amount;
```

✅ **Right:** Always update both
```csharp
items[i].Amount += amount;
inventory.TotalStored += amount;
_storehouseInventoryLookup[entity] = inventory;
```

### Capacity Check on TotalCapacity

❌ **Wrong:** Check total capacity instead of per-resource-type capacity
```csharp
if (inventory.TotalStored >= inventory.TotalCapacity)  // Wrong for per-type capacity
```

✅ **Right:** Check per-resource-type capacity
```csharp
var capacity = FindCapacityForResourceType(capacities, resourceTypeId);
var stored = FindStoredAmountForResourceType(items, resourceTypeId);
var reserved = FindReservedAmountForResourceType(reservations, resourceTypeId);
var available = capacity.MaxCapacity - stored - reserved;
```

---

## Performance Characteristics

### Registry Update

- **Cost:** O(N) where N = number of storehouses
- **Frequency:** Every tick
- **Optimization:** Uses spatial residency for fast cell ID resolution (O(1) lookup vs O(N) spatial hash)

### Deposit/Withdrawal

- **Cost:** O(M) where M = number of resource types stored (buffer linear search)
- **Frequency:** On-demand (when villagers arrive at storehouse)
- **Optimization:** Small buffers (typically 4-16 resource types), linear search is fast

### Reservation Lookup

- **Cost:** O(M) where M = number of reservation items (buffer linear search)
- **Frequency:** On deposit/withdrawal
- **Optimization:** Small buffers (typically 0-8 reservations)

---

## Recommendations

### Phase 1: API Wrapper (High Priority)

1. **Create unified API wrapper:**
   - `StorehouseApi.Add(storehouseEntity, resourceTypeId, amount) → acceptedAmount`
   - `StorehouseApi.Remove(storehouseEntity, resourceTypeId, amount) → removedAmount`
   - `StorehouseApi.Space(storehouseEntity, resourceTypeId) → availableSpace`

2. **Centralize validation:**
   - Type validation (reject unknown resource types)
   - Capacity enforcement (never exceed capacity)
   - Reservation checking (account for reserved amounts)

3. **Migrate systems:**
   - `ResourceDepositSystem` → use API
   - `StorehouseWithdrawalProcessingSystem` → use API
   - `DivineHandSystems` → use API

### Phase 2: Buffer Restoration in Rewind

1. **Snapshot buffers in `StorehouseInventoryTimeAdapterSystem`:**
   - Record `StorehouseInventoryItem` buffer contents
   - Record `StorehouseCapacityElement` buffer contents
   - Restore buffers during playback

2. **Alternative:** Event-based replay (record deposit/withdrawal events, replay during rewind)

### Phase 3: Resource Type Standardization

1. **Choose one:** `ResourceTypeIndex` (ushort) for runtime, `ResourceTypeId` (string) for authoring
2. **Migration:** Convert `StorehouseInventoryItem` to use `ResourceTypeIndex`
3. **Catalog:** Use `ResourceTypeIndex` catalog for all conversions

### Phase 4: Event System (Optional)

1. **Create event component:** `StorehouseInventoryChanged` (buffer element)
2. **Emit events:** On Add/Remove operations
3. **UI consumption:** UI systems read event buffer for updates

---

## Related Documentation

- **Storehouse API Contract:** `godgame/Docs/Concepts/Implemented/Economy/Storehouse_API.md` - API design document
- **Resource System:** `Docs/Concepts/Core/` - Resource management overview
- **Villager Job System:** `Docs/Concepts/Core/` - Job execution and delivery integration

---

**For Implementers:** Focus on Phase 1 (API wrapper) to reduce code duplication and centralize validation  
**For Architects:** Review buffer restoration in rewind (Phase 2) for correctness  
**For Designers:** Understand capacity per-resource-type semantics for UI display

