# Storehouse System Advisory

**Status:** Recommendations / Action Plan
**Category:** Core - Resource Storage Tightening
**Audience:** Implementers / Architects
**Created:** 2025-01-17
**Last Updated:** 2025-01-17

---

## Purpose

**Primary Goal:** Tighten the storehouse system for correctness under rewind, single-writer safety, and scaling without per-tick scanning. Focus on fixing resource identity mismatches, centralizing mutations, clarifying reservation semantics, incremental registry updates, rewind correctness, buffer capacity tuning, and quality math.

**Focus Areas:**
1. **Unify resource identity** — Runtime = ResourceTypeIndex, authoring/UI = string
2. **Enforce single mutation path** — API wrapper as only writer
3. **Split reservations** — Separate ReservedOut (stock) from ReservedIn (capacity)
4. **Incremental registry updates** — Change filtering instead of full rebuild
5. **Rewind correctness** — Snapshot buffers or replay deltas
6. **Buffer capacity tuning** — Match real usage patterns
7. **Quality math** — Store sums, compute averages on demand

---

## 1. Unify Resource Identity: Runtime = ResourceTypeIndex, Authoring/UI = String

### Current State

**Problem:** Inventory items use `ResourceTypeId` (FixedString) while reservations use `ResourceTypeIndex` (ushort). This causes:
- Linear string comparisons (performance cost)
- ID/index mismatch bugs (correctness risk)
- Conversion overhead (catalog lookups)

### Proposed Solution

**Convert to ResourceTypeIndex everywhere at runtime:**

```csharp
// ✅ CORRECT: Use ResourceTypeIndex (ushort) for runtime
public struct StorehouseInventoryItem : IBufferElementData
{
    public ushort ResourceTypeIndex;  // Changed from FixedString64Bytes ResourceTypeId
    public float Amount;
    public float Reserved;
    public byte TierId;
    public ushort AverageQuality;
}

public struct StorehouseCapacityElement : IBufferElementData
{
    public ushort ResourceTypeIndex;  // Changed from FixedString64Bytes ResourceTypeId
    public float MaxCapacity;
}
```

**Keep ResourceTypeId only for authoring/UI:**

```csharp
// Authoring components still use strings
public struct StorehouseCapacityAuthoring
{
    public string ResourceTypeId;  // Authoring uses string
    public float MaxCapacity;
}

// Catalog provides Index → Name mapping for UI/debug
public struct ResourceTypeIndexBlob
{
    public BlobArray<FixedString64Bytes> Ids;  // Index → Name (for UI/debug)
    public BlobHashMap<FixedString64Bytes, ushort> NameToIndex;  // Name → Index (for authoring conversion)
}
```

**Migration Path:**
1. Add `ResourceTypeIndex` field to `StorehouseInventoryItem` and `StorehouseCapacityElement`
2. Update authoring systems to convert `ResourceTypeId` → `ResourceTypeIndex` during baking
3. Migrate runtime systems to use `ResourceTypeIndex` directly (no string comparisons)
4. Deprecate `ResourceTypeId` field in runtime components (or remove after migration)

**Benefits:**
- Removes linear string compares (O(1) index comparison vs O(n) string comparison)
- Eliminates ID/index mismatch bugs (single source of truth)
- Faster lookups (direct index access)
- Smaller data size (ushort = 2 bytes vs FixedString64Bytes = 64 bytes)

---

## 2. Enforce a Single Mutation Path (No Direct Buffer Writes)

### Current State

**Problem:** Direct buffer manipulation scattered across systems, no centralized validation.

**Current Anti-Pattern:**
```csharp
// ❌ WRONG: Direct buffer manipulation in multiple systems
var inventory = _storehouseInventoryLookup[entity];
var items = _storehouseInventoryItemsLookup[entity];
items[i].Amount += depositAmount;  // Direct mutation
inventory.TotalStored += depositAmount;
_storehouseInventoryLookup[entity] = inventory;
```

**Issues:**
- Dynamic buffer handles can be invalidated by structural changes
- Unity explicitly warns: must reacquire buffers after structural changes
- No centralized validation (capacity checks, type validation)
- Code duplication across systems

### Proposed Solution

**Define StorehouseApi as the only place that can mutate:**

```csharp
public static class StorehouseApi
{
    /// <summary>
    /// Attempts to deposit resources into a storehouse.
    /// Returns accepted amount.
    /// </summary>
    public static float TryDeposit(
        Entity storehouseEntity,
        ushort resourceTypeIndex,
        float amount,
        ref SystemState state,
        out float rejectedAmount)
    {
        rejectedAmount = 0f;
        
        // Reacquire buffers (safe after structural changes)
        if (!SystemAPI.HasComponent<StorehouseInventory>(storehouseEntity) ||
            !SystemAPI.HasBuffer<StorehouseInventoryItem>(storehouseEntity) ||
            !SystemAPI.HasBuffer<StorehouseCapacityElement>(storehouseEntity))
        {
            rejectedAmount = amount;
            return 0f;
        }
        
        var inventory = SystemAPI.GetComponentRW<StorehouseInventory>(storehouseEntity);
        var items = SystemAPI.GetBuffer<StorehouseInventoryItem>(storehouseEntity);
        var capacities = SystemAPI.GetBuffer<StorehouseCapacityElement>(storehouseEntity);
        
        // Type validation
        if (!HasCapacityForResourceType(capacities, resourceTypeIndex))
        {
            rejectedAmount = amount;
            return 0f;
        }
        
        // Capacity check (per-resource-type)
        var capacity = FindCapacity(capacities, resourceTypeIndex);
        var stored = FindStoredAmount(items, resourceTypeIndex);
        var availableSpace = capacity - stored;
        
        if (availableSpace <= 0f)
        {
            rejectedAmount = amount;
            return 0f;
        }
        
        // Deposit (partial acceptance if needed)
        var accepted = math.min(amount, availableSpace);
        rejectedAmount = amount - accepted;
        
        // Find or create inventory item
        var itemIndex = FindOrCreateItem(items, resourceTypeIndex);
        var item = items[itemIndex];
        item.Amount += accepted;
        items[itemIndex] = item;
        
        // Update totals
        inventory.ValueRW.TotalStored += accepted;
        inventory.ValueRW.LastUpdateTick = SystemAPI.GetSingleton<TimeState>().Tick;
        
        return accepted;
    }
    
    /// <summary>
    /// Attempts to withdraw resources from a storehouse.
    /// Returns withdrawn amount.
    /// </summary>
    public static float TryWithdraw(
        Entity storehouseEntity,
        ushort resourceTypeIndex,
        float amount,
        ref SystemState state,
        float reservedOut = 0f)  // ReservedOut reduces withdrawable stock
    {
        // Reacquire buffers (safe after structural changes)
        if (!SystemAPI.HasComponent<StorehouseInventory>(storehouseEntity) ||
            !SystemAPI.HasBuffer<StorehouseInventoryItem>(storehouseEntity))
        {
            return 0f;
        }
        
        var inventory = SystemAPI.GetComponentRW<StorehouseInventory>(storehouseEntity);
        var items = SystemAPI.GetBuffer<StorehouseInventoryItem>(storehouseEntity);
        
        // Find inventory item
        var itemIndex = FindItem(items, resourceTypeIndex);
        if (itemIndex < 0)
        {
            return 0f;
        }
        
        var item = items[itemIndex];
        
        // Calculate available (Amount - ReservedOut)
        var reservedTotal = math.max(item.Reserved, reservedOut);
        var available = math.max(0f, item.Amount - reservedTotal);
        
        if (available <= 0f)
        {
            return 0f;
        }
        
        // Withdraw (partial if needed)
        var withdrawn = math.min(amount, available);
        
        item.Amount -= withdrawn;
        items[itemIndex] = item;
        
        // Update totals
        inventory.ValueRW.TotalStored -= withdrawn;
        inventory.ValueRW.LastUpdateTick = SystemAPI.GetSingleton<TimeState>().Tick;
        
        return withdrawn;
    }
    
    /// <summary>
    /// Queries available space for a resource type.
    /// </summary>
    public static float QuerySpace(
        Entity storehouseEntity,
        ushort resourceTypeIndex,
        ref SystemState state,
        float reservedIn = 0f)  // ReservedIn reduces deposit space
    {
        if (!SystemAPI.HasComponent<StorehouseInventory>(storehouseEntity) ||
            !SystemAPI.HasBuffer<StorehouseCapacityElement>(storehouseEntity) ||
            !SystemAPI.HasBuffer<StorehouseInventoryItem>(storehouseEntity))
        {
            return 0f;
        }
        
        var capacities = SystemAPI.GetBuffer<StorehouseCapacityElement>(storehouseEntity);
        var items = SystemAPI.GetBuffer<StorehouseInventoryItem>(storehouseEntity);
        
        var capacity = FindCapacity(capacities, resourceTypeIndex);
        var stored = FindStoredAmount(items, resourceTypeIndex);
        var reservedTotal = math.max(0f, reservedIn);  // ReservedIn reduces deposit space
        
        return math.max(0f, capacity - stored - reservedTotal);
    }
    
    // Helper methods (private, used by API only)
    private static bool HasCapacityForResourceType(DynamicBuffer<StorehouseCapacityElement> capacities, ushort resourceTypeIndex) { /* ... */ }
    private static float FindCapacity(DynamicBuffer<StorehouseCapacityElement> capacities, ushort resourceTypeIndex) { /* ... */ }
    private static float FindStoredAmount(DynamicBuffer<StorehouseInventoryItem> items, ushort resourceTypeIndex) { /* ... */ }
    private static int FindItem(DynamicBuffer<StorehouseInventoryItem> items, ushort resourceTypeIndex) { /* ... */ }
    private static int FindOrCreateItem(DynamicBuffer<StorehouseInventoryItem> items, ushort resourceTypeIndex) { /* ... */ }
}
```

**Usage Pattern:**
```csharp
// ✅ CORRECT: All systems call API, no direct buffer manipulation
var accepted = StorehouseApi.TryDeposit(storehouseEntity, resourceTypeIndex, amount, ref state, out var rejected);
if (rejected > 0f)
{
    // Create pile with rejected amount
    CreateResourcePile(position, resourceTypeIndex, rejected);
}
```

**Why It Matters:**
- **Unity warning:** Dynamic buffer handles invalidated by structural changes; must reacquire
- **Centralized validation:** Capacity checks, type validation in one place
- **Single source of truth:** No scattered mutation logic
- **Correctness:** Ensures `TotalStored` and buffers stay in sync

**Reference:** [Unity ECS - Dynamic Buffers](https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/dynamic-buffer.html)

---

## 3. Split Reservations into Two Concepts (Or Delete One for MVP)

### Current State

**Problem:** `Reserved` field is used in ways that mix "stock reserved for withdrawal" and "capacity reserved for incoming," causing contradictions.

**Current Logic:**
```csharp
// ❌ PROBLEMATIC: Reserved subtracted in both deposit and withdrawal checks
var availableForDeposit = capacity - stored - reserved;  // Reserved reduces deposit space
var availableForWithdrawal = amount - reserved;           // Reserved reduces withdrawal stock
```

**Issue:** Same `Reserved` value affects both deposit space and withdrawal stock, leading to double-booking bugs.

### Proposed Solution

**MVP (Recommended): Only Reserve Stock (Outgoing)**

```csharp
// ✅ CORRECT: Only ReservedOut (stock reserved for withdrawal)
public struct StorehouseInventoryItem : IBufferElementData
{
    public ushort ResourceTypeIndex;
    public float Amount;
    public float ReservedOut;      // Changed from "Reserved" to "ReservedOut" (explicit)
    public byte TierId;
    public ushort AverageQuality;  // Or QualitySum (see §7)
}

// Withdrawal: ReservedOut reduces withdrawable stock
var withdrawable = item.Amount - item.ReservedOut;

// Deposits: ReservedOut does NOT affect deposit space
var depositSpace = capacity - item.Amount;  // Ignore ReservedOut for deposits
```

**Usage:**
- Construction sites reserve stock (ReservedOut) for withdrawal
- Deposits ignore reservations entirely (capacity - amount = available space)
- Withdrawals check ReservedOut (amount - reservedOut = withdrawable)

**Later: Also Reserve Space (Incoming)**

```csharp
// Future: Add ReservedIn for incoming reservations
public struct StorehouseInventoryItem : IBufferElementData
{
    public ushort ResourceTypeIndex;
    public float Amount;
    public float ReservedOut;      // Stock reserved for withdrawal
    public float ReservedIn;       // Capacity reserved for incoming (future)
    public byte TierId;
    public ushort QualitySum;      // Sum instead of average (see §7)
}

// Withdrawal: ReservedOut reduces withdrawable stock
var withdrawable = item.Amount - item.ReservedOut;  // ReservedIn does NOT affect withdrawal

// Deposits: ReservedIn reduces deposit space
var depositSpace = capacity - item.Amount - item.ReservedIn;  // ReservedOut does NOT affect deposits
```

**Key Rule:** ReservedOut affects withdrawal, ReservedIn affects deposits. Never mix them.

**Reservation API:**
```csharp
// Reserve stock for withdrawal (construction site reserves items)
StorehouseApi.TryReserveOut(storehouseEntity, resourceTypeIndex, amount, ref state);

// Release stock reservation
StorehouseApi.TryReleaseOut(storehouseEntity, resourceTypeIndex, amount, ref state);

// Future: Reserve space for incoming (not yet implemented)
// StorehouseApi.TryReserveIn(storehouseEntity, resourceTypeIndex, amount, ref state);
```

**Benefits:**
- Prevents double-booking (separate concerns: stock vs capacity)
- Eliminates "subtract reserved twice" bug class
- Clear semantics (ReservedOut = stock, ReservedIn = capacity)

---

## 4. Make the Registry Incremental (Don't Rebuild Every Tick)

### Current State

**Problem:** Registry update runs every tick, rebuilding all storehouse entries (O(N) cost).

**Current Implementation:**
```csharp
// ❌ REBUILDS ALL: Every tick, rebuilds all storehouse entries
foreach (var (inventory, transform, entity) in SystemAPI.Query<...>())
{
    // Rebuild entry from scratch
    builder.Add(new StorehouseRegistryEntry { ... });
}
```

**Issues:**
- O(N) cost every tick (N = number of storehouses)
- Makes rewind/playback harder to reason about (registry changes every tick)
- Wastes CPU on unchanged storehouses

### Proposed Solution

**Use Change Filtering for Incremental Updates:**

```csharp
// ✅ CORRECT: Only rebuild entries that changed
[BurstCompile]
public partial struct StorehouseRegistrySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Only process storehouses that changed (inventory or position changed)
        foreach (var (inventory, transform, entity) in SystemAPI.Query<RefRO<StorehouseInventory>, RefRO<LocalTransform>>()
            .WithAll<StorehouseConfig>()
            .WithChangeFilter<StorehouseInventory, LocalTransform>()  // Only changed entities
            .WithEntityAccess())
        {
            // Rebuild entry for changed storehouse
            RebuildRegistryEntry(entity, inventory.ValueRO, transform.ValueRO, ref state);
        }
        
        // Or use version-based filtering
        var registryEntity = SystemAPI.GetSingletonEntity<StorehouseRegistry>();
        var entries = SystemAPI.GetBuffer<StorehouseRegistryEntry>(registryEntity);
        
        foreach (var entry in entries)
        {
            // Check if storehouse mutation tick changed
            if (SystemAPI.Exists(entry.StorehouseEntity))
            {
                var currentInventory = SystemAPI.GetComponentRO<StorehouseInventory>(entry.StorehouseEntity);
                if (currentInventory.ValueRO.LastUpdateTick != entry.LastMutationTick)
                {
                    // Entry is stale, rebuild it
                    RebuildRegistryEntry(entry.StorehouseEntity, currentInventory.ValueRO, /* transform */, ref state);
                }
            }
        }
    }
}
```

**Alternative: Mutation Version Stamps**

```csharp
// Use LastUpdateTick as "dirty stamp"
public struct StorehouseInventory : IComponentData
{
    public float TotalStored;
    public float TotalCapacity;
    public int ItemTypeCount;
    public byte IsShredding;
    public uint LastUpdateTick;  // Mutation version stamp
}

// In StorehouseApi:
inventory.ValueRW.LastUpdateTick = SystemAPI.GetSingleton<TimeState>().Tick;  // Mark as dirty

// In StorehouseRegistrySystem:
if (entry.LastMutationTick < inventory.LastUpdateTick)
{
    // Entry is stale, rebuild
}
```

**Change Filtering Pattern:**
```csharp
// Use WithChangeFilter to skip unchanged chunks
foreach (var (inventory, entity) in SystemAPI.Query<RefRO<StorehouseInventory>>()
    .WithChangeFilter<StorehouseInventory>()  // Only entities where StorehouseInventory changed
    .WithEntityAccess())
{
    // Only process changed entities
}
```

**Benefits:**
- Cost proportional to "storehouses that changed" (not total storehouses)
- Better performance (skip unchanged storehouses)
- Easier rewind reasoning (registry only changes when storehouses change)

**Reference:**
- [Unity ECS - Change Filtering](https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/change-filtering.html)
- [Unity ECS - EntityQuery.SetChangedVersionFilter](https://docs.unity.cn/Packages/com.unity.entities@1.0/api/Unity.Entities.EntityQuery.SetChangedVersionFilter.html)

---

## 5. Rewind Correctness: Snapshot Buffers or Replay Deltas

### Current State

**Problem:** `StorehouseInventoryTimeAdapterSystem` only snapshots `StorehouseInventory` component, not buffers, so rewind restores totals but not actual inventory contents.

**Current Implementation:**
```csharp
// ❌ INCOMPLETE: Only snapshots component, not buffers
private void Save(ref SystemState state, ref TimeStreamWriter writer)
{
    foreach (var (inventory, entity) in SystemAPI.Query<RefRO<StorehouseInventory>>().WithEntityAccess())
    {
        writer.Write(new StorehouseInventoryRecord
        {
            Storehouse = entity,
            Inventory = inventory.ValueRO  // Only component, buffers missing
        });
    }
}
```

**Issue:** During rewind, `TotalStored` is restored, but `StorehouseInventoryItem` buffers are not, so inventory contents are lost.

### Proposed Solution

**Option A: Buffer Snapshots (Simple, Recommended)**

```csharp
// ✅ CORRECT: Snapshot buffers too
private struct StorehouseInventoryRecord
{
    public Entity Storehouse;
    public StorehouseInventory Inventory;  // Component
    public int ItemCount;                  // Buffer length
    public int CapacityCount;              // Buffer length
    // Buffer data serialized separately (via BlobArray or inline array)
}

private void Save(ref SystemState state, ref TimeStreamWriter writer)
{
    var records = new NativeList<StorehouseRecord>(Allocator.Temp);
    
    foreach (var (inventory, entity) in SystemAPI.Query<RefRO<StorehouseInventory>>()
        .WithEntityAccess())
    {
        var items = SystemAPI.GetBuffer<StorehouseInventoryItem>(entity);
        var capacities = SystemAPI.GetBuffer<StorehouseCapacityElement>(entity);
        
        // Serialize buffer data
        var itemArray = new NativeArray<StorehouseInventoryItem>(items.Length, Allocator.Temp);
        for (int i = 0; i < items.Length; i++)
        {
            itemArray[i] = items[i];
        }
        
        var capacityArray = new NativeArray<StorehouseCapacityElement>(capacities.Length, Allocator.Temp);
        for (int i = 0; i < capacities.Length; i++)
        {
            capacityArray[i] = capacities[i];
        }
        
        records.Add(new StorehouseRecord
        {
            Storehouse = entity,
            Inventory = inventory.ValueRO,
            Items = itemArray,  // Snapshot buffer contents
            Capacities = capacityArray  // Snapshot buffer contents
        });
    }
    
    // Write records (with buffer arrays)
    writer.Write(records.Length);
    for (int i = 0; i < records.Length; i++)
    {
        writer.Write(records[i].Storehouse);
        writer.Write(records[i].Inventory);
        writer.Write(records[i].Items.Length);
        for (int j = 0; j < records[i].Items.Length; j++)
        {
            writer.Write(records[i].Items[j]);
        }
        // ... write capacities too
    }
}

private void Load(ref SystemState state, ref TimeStreamReader reader)
{
    var count = reader.Read<int>();
    for (int i = 0; i < count; i++)
    {
        var entity = reader.Read<Entity>();
        var inventory = reader.Read<StorehouseInventory>();
        var itemCount = reader.Read<int>();
        
        if (!SystemAPI.Exists(entity))
        {
            continue;
        }
        
        // Restore component
        SystemAPI.SetComponent(entity, inventory);
        
        // Restore buffers
        var items = SystemAPI.GetBuffer<StorehouseInventoryItem>(entity);
        items.Clear();
        for (int j = 0; j < itemCount; j++)
        {
            var item = reader.Read<StorehouseInventoryItem>();
            items.Add(item);
        }
        // ... restore capacities too
    }
}
```

**Option B: Event-Sourced Deltas (More Elegant, Future)**

```csharp
// Future: Record per-tick deltas
public struct StorehouseDelta : IBufferElementData
{
    public Entity StorehouseEntity;
    public ushort ResourceTypeIndex;
    public float DeltaAmount;         // Change in Amount
    public float DeltaReservedOut;    // Change in ReservedOut
    public float DeltaReservedIn;     // Change in ReservedIn (future)
    public float DeltaQualitySum;     // Change in QualitySum (see §7)
}

// Record deltas each tick
StorehouseApi.TryDeposit(...) → emits StorehouseDelta (DeltaAmount = +accepted)

// During rewind: Restore base snapshot every N ticks + replay deltas
RestoreSnapshot(baseTick);
for (uint tick = baseTick + 1; tick <= targetTick; tick++)
{
    ApplyDeltas(deltas[tick]);
}
```

**Recommendation:** Option A (buffer snapshots) for MVP. Storehouses are few, item types per storehouse are small, so snapshot cost is acceptable.

**Benefits:**
- Correct rewind (buffers fully restored)
- Simple implementation (direct snapshot/restore)
- Deterministic (same inputs → same outputs)

---

## 6. Tune Dynamic Buffer Capacities to Real Usage

### Current State

**Problem:** Buffer capacities may not match real usage patterns.

**Current Capacities:**
```csharp
[InternalBufferCapacity(32)]  // StorehouseInventoryItem
[InternalBufferCapacity(16)]  // StorehouseCapacityElement
```

**Unity Behavior:** When a dynamic buffer exceeds `InternalBufferCapacity`, its data moves outside the chunk and never moves back, leaving wasted in-chunk space.

### Proposed Solution

**Match Capacities to Typical Usage:**

```csharp
// ✅ CORRECT: Size to typical counts
// Villages usually store 4-12 resource types
[InternalBufferCapacity(12)]  // StorehouseInventoryItem (typical: 4-12 types)
[InternalBufferCapacity(8)]   // StorehouseCapacityElement (typical: 4-8 capacity definitions)
[InternalBufferCapacity(4)]   // StorehouseReservationItem (typical: 0-4 reservations)
```

**Or Use External Storage for Variable Counts:**

```csharp
// If counts vary wildly (modded sandbox), use external storage
[InternalBufferCapacity(0)]  // Always store outside-chunk
public struct StorehouseInventoryItem : IBufferElementData { ... }
```

**Decision Rule:**
- **Small, stable counts:** Use `InternalBufferCapacity` sized to typical usage (e.g., 12 for inventory items)
- **Variable/large counts:** Use `InternalBufferCapacity(0)` to always store outside-chunk

**Benefits:**
- Better chunk utilization (no wasted in-chunk space)
- Predictable memory layout (either all in-chunk or all outside-chunk)

**Reference:** [Unity ECS - Dynamic Buffers](https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/dynamic-buffer.html)

---

## 7. Quality Math: Store Sums, Not "Average as State"

### Current State

**Problem:** `AverageQuality` stored per resource type entry. Averages drift if you repeatedly merge and round.

**Current Implementation:**
```csharp
// ❌ PROBLEMATIC: Storing average causes drift
public struct StorehouseInventoryItem : IBufferElementData
{
    public ushort AverageQuality;  // Average (0-600)
}

// When merging:
var newAverage = (oldAverage * oldAmount + newQuality * newAmount) / (oldAmount + newAmount);
// Rounding errors accumulate over time
```

**Issues:**
- Rounding errors accumulate (merges are not associative)
- Not rewind-friendly (can't replay deltas)
- Loss of precision (average loses information)

### Proposed Solution

**Store QualitySum Instead:**

```csharp
// ✅ CORRECT: Store sum, compute average on demand
public struct StorehouseInventoryItem : IBufferElementData
{
    public ushort ResourceTypeIndex;
    public float Amount;
    public float ReservedOut;
    public byte TierId;
    public uint QualitySum;  // Changed from AverageQuality to QualitySum
    // QualitySum = Σ(amount × quality) for all units in this entry
}

// Compute average on demand (for UI/registry)
public static ushort ComputeAverageQuality(float amount, uint qualitySum)
{
    if (amount <= 0f)
    {
        return 0;
    }
    return (ushort)math.clamp(math.round(qualitySum / amount), 0f, 600f);
}

// When merging (associative, no drift):
item.QualitySum += (newAmount * newQuality);  // Add to sum
item.Amount += newAmount;
// Average computed on demand: item.QualitySum / item.Amount
```

**Usage in API:**
```csharp
// When depositing resources with quality
public static float TryDeposit(
    Entity storehouseEntity,
    ushort resourceTypeIndex,
    float amount,
    ushort quality,  // Quality of deposited resources
    ref SystemState state,
    out float rejectedAmount)
{
    // ... deposit logic ...
    
    // Update quality sum (additive, associative)
    item.QualitySum += (uint)(amount * quality);
    // Average computed on demand: item.QualitySum / item.Amount
}
```

**Benefits:**
- No drift (merges are associative)
- Rewind-friendly (can replay deltas: DeltaQualitySum)
- Lossless (sum preserves all information)
- On-demand computation (average only when needed for UI/registry)

---

## Implementation Order

### Phase 1: Correctness (Critical)
1. **Unify resource identity** (ResourceTypeIndex everywhere runtime)
2. **Single mutation path** (StorehouseApi as only writer)
3. **Split reservations** (MVP: ReservedOut only)

### Phase 2: Performance (Scalability)
4. **Incremental registry updates** (change filtering)

### Phase 3: Rewind Correctness
5. **Rewind fix** (snapshot buffers or replay deltas)

### Phase 4: Polish (Quality of Life)
6. **Buffer capacity tuning** (match real usage)
7. **Quality math** (store sums, compute averages on demand)

---

## Deposit/Withdraw Flow Summary

### Current Deposit Flow (ResourceDepositSystem)

**System:** `ResourceDepositSystem` → `DepositResourcesJob` (IJobEntity)

**Trigger:** Villager with `VillagerJobCarryItem` buffer, job phase = `Delivering`, target storehouse set

**Steps:**

1. **Distance Check:**
   - Villager must be within `DepositDistance` of storehouse (default from `ResourceInteractionConfig`)
   - If too far: set `VillagerAIState.TargetEntity = storehouse` (continue pathfinding)

2. **Validation:**
   - Storehouse must have: `StorehouseConfig`, `StorehouseCapacityElement` buffer, `StorehouseInventoryItem` buffer
   - Villager must have: `VillagerJobCarryItem` buffer with items

3. **Per-Carried-Item Processing:**
   - For each item in `VillagerJobCarryItem`:
     - Convert `ResourceTypeIndex` → `ResourceTypeId` (string) via catalog lookup
     - Find capacity for resource type in `StorehouseCapacityElement` buffer (string comparison)
     - Find existing inventory item with matching `ResourceTypeId` and same `TierId`
     - Calculate available space: `maxCapacity - currentStoredForType`
     - Accept amount: `min(carriedAmount, availableSpace)`
     - Update or create `StorehouseInventoryItem` (merge quality if same tier, or create new item)
     - Remove from `VillagerJobCarryItem` buffer

4. **Totals Update:**
   - Update `StorehouseInventory.TotalStored` (incremented by accepted amount)
   - Set `StorehouseInventory.LastUpdateTick = CurrentTick`

**Current Issues:**
- ❌ Direct buffer manipulation (no API wrapper)
- ❌ String comparisons for resource type lookup (slow)
- ❌ Capacity check uses total stored, not per-resource-type properly
- ❌ No reservation checking (should ignore ReservedOut for deposits)

**Proposed Flow (After API Wrapper):**
```csharp
// ✅ CORRECT: Use API wrapper
var accepted = StorehouseApi.TryDeposit(
    storehouseEntity,
    carried.ResourceTypeIndex,  // Use index directly
    carried.Amount,
    ref state,
    out var rejected);

// Remove accepted amount from villager
carried.Amount -= accepted;
if (carried.Amount <= 0f)
{
    carry.RemoveAt(i);
}
```

---

### Current Withdrawal Flow (StorehouseWithdrawalProcessingSystem)

**System:** `StorehouseWithdrawalProcessingSystem` (ISystem, queries villagers)

**Trigger:** Villager with `VillagerWithdrawRequest` buffer, `VillagerAIState.TargetEntity` points to storehouse

**Steps:**

1. **Distance Check:**
   - Villager must be within `WithdrawDistance` of storehouse (default from `ResourceInteractionConfig`)
   - If too far: continue (villager keeps pathfinding)

2. **Validation:**
   - Storehouse must have: `StorehouseConfig`, `StorehouseInventoryItem` buffer
   - Villager must have: `VillagerWithdrawRequest` buffer

3. **Per-Request Processing:**
   - For each request in `VillagerWithdrawRequest`:
     - Convert `ResourceTypeId` (string) → `ResourceTypeIndex` (ushort) via catalog lookup
     - Find inventory item with matching `ResourceTypeId` (string comparison)
     - Calculate available: `max(0, item.Amount - item.Reserved)`  ← **Issue: Reserved semantics unclear**
     - Withdraw amount: `min(request.Amount, available, villagerCapacity)`
     - Update `StorehouseInventoryItem.Amount` (decrement)
     - Add to villager's `VillagerInventoryItem` buffer (create or update existing)
     - Update `VillagerWithdrawRequest.Amount` (decrement by withdrawn amount, remove if <= 0)

4. **Totals Update:**
   - Update `StorehouseInventory.TotalStored` (decremented by withdrawn amount)
   - Set `StorehouseInventory.ItemTypeCount = storeItems.Length`

**Current Issues:**
- ❌ Direct buffer manipulation (no API wrapper)
- ❌ String comparisons for resource type lookup (slow)
- ❌ Reservation semantics unclear (Reserved used for both stock and capacity)
- ❌ Villager capacity check mixed into withdrawal logic (should be separate concern)

**Proposed Flow (After API Wrapper):**
```csharp
// ✅ CORRECT: Use API wrapper
var withdrawn = StorehouseApi.TryWithdraw(
    storehouseEntity,
    request.ResourceTypeIndex,  // Use index directly
    request.Amount,
    ref state,
    reservedOut: 0f);  // ReservedOut passed explicitly

// Add to villager inventory (separate logic)
if (withdrawn > 0f)
{
    AddToVillagerInventory(villagerEntity, request.ResourceTypeIndex, withdrawn, ref state);
    request.Amount -= withdrawn;
    if (request.Amount <= 0f)
    {
        requests.RemoveAt(r);
    }
}
```

---

### Key Differences: Deposit vs Withdrawal

| Aspect | Deposit | Withdrawal |
|--------|---------|------------|
| **Trigger** | `VillagerJobCarryItem` buffer | `VillagerWithdrawRequest` buffer |
| **Job Phase** | `Delivering` | Any (request-driven) |
| **Distance Check** | `DepositDistance` | `WithdrawDistance` |
| **Capacity Check** | Per-resource-type `MaxCapacity` | Villager carry capacity |
| **Reservation Impact** | Should ignore ReservedOut (deposits ignore reservations) | Should subtract ReservedOut (withdrawals respect stock reservations) |
| **Resource Type** | Index → ID conversion | ID → Index conversion |
| **Quality Handling** | Merge if same tier, or create new item | Copy quality from storehouse item |

---

### After Advisory Implementation

**Unified Flow (Both Deposit and Withdrawal):**

```csharp
// Deposit: Use API wrapper, ignore ReservedOut
var accepted = StorehouseApi.TryDeposit(storehouseEntity, resourceTypeIndex, amount, ref state, out var rejected);

// Withdrawal: Use API wrapper, respect ReservedOut
var withdrawn = StorehouseApi.TryWithdraw(storehouseEntity, resourceTypeIndex, amount, ref state, reservedOut: reservationAmount);

// Query: Check available space (ignores ReservedOut for deposits, includes ReservedIn for future)
var available = StorehouseApi.QuerySpace(storehouseEntity, resourceTypeIndex, ref state, reservedIn: 0f);
```

**Benefits:**
- ✅ Single mutation path (API wrapper only)
- ✅ Consistent resource type handling (ResourceTypeIndex everywhere runtime)
- ✅ Clear reservation semantics (ReservedOut affects withdrawal, ReservedIn affects deposits)
- ✅ Centralized validation (capacity checks, type validation)
- ✅ Safe buffer handling (reacquire after structural changes)

---

## Related Documentation

- **Storehouse System Summary:** `Docs/Concepts/Core/Storehouse_System_Summary.md` - Current state analysis
- **Unity ECS - Dynamic Buffers:** https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/dynamic-buffer.html
- **Unity ECS - Change Filtering:** https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/change-filtering.html

---

**For Implementers:** Focus on Phase 1 (correctness) for immediate fixes  
**For Architects:** Review Phase 2-3 (performance, rewind) for scalability and correctness  
**For Designers:** Understand reservation semantics (ReservedOut vs ReservedIn) for UI display

