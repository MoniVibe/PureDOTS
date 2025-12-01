# Extension Request: Expand Registries - Aggregate Views & Background Simulation

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Completed**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P1  

### Implementation
- `Packages/com.moni.puredots/Runtime/Runtime/Registry/Aggregates/AggregateComponents.cs` - AggregateRegistryEntry, AggregateResourceEntry, CompressionRequest, CompressionConfig, CompressedEvent, PseudoHistoryEntry, BackgroundSimState, AggregateMember, AggregateProduction, AggregateMilitary
- `Packages/com.moni.puredots/Runtime/Runtime/Registry/Aggregates/AggregateHelpers.cs` - Static helpers for aggregate calculations and compression
- `Packages/com.moni.puredots/Runtime/Systems/Registry/Aggregates/AggregateRegistrySystem.cs` - AggregateRegistrySystem, CompressionSystem, BackgroundSimSystem, PseudoHistorySystem

---

## Use Case

Current registries track individual entities. Expand to support:

**Both Games:**
- **Aggregate views**: Village/fleet totals without iterating all entities
- **Background simulation**: Islands/sectors run in compressed mode when not observed
- **Scalability**: Handle billions of entities via aggregation

**Godgame (from TODO concept note):**
> "World Phasing: Add an island/world compression mode where whole islands can be collapsed into background simulation. Rendering pauses but registries/compliance continue in lightweight tick."

**Space4X:**
- Sector aggregates: Total ships/resources per sector
- Background economy: Trade routes run without full simulation
- Fleet summaries: Quick stats without entity iteration

---

## Proposed Expansion

### Current State
- Per-entity registry entries (VillagerRegistryEntry, etc.)
- Continuity snapshots for rewind

### Requested Expansion

```csharp
// Aggregate registry for compressed simulation
public struct AggregateRegistryEntry : IBufferElementData
{
    public Entity SourceGroup;        // Village, fleet, sector entity
    public FixedString32Bytes GroupType; // "Village", "Fleet", "Sector"
    
    // Population aggregates
    public int TotalPopulation;
    public int WorkingPopulation;
    public int CombatPopulation;
    
    // Resource aggregates
    public float TotalFood;
    public float TotalWealth;
    public float ProductionRate;
    public float ConsumptionRate;
    
    // State aggregates
    public float AverageMorale;
    public float AverageHealth;
    public byte DominantMoodBand;
    
    // Compression state
    public bool IsCompressed;         // Running in background mode
    public uint LastFullSimTick;      // When last fully simulated
    public uint TicksCompressed;      // How long in background
}

public struct CompressionRequest : IComponentData
{
    public Entity TargetGroup;
    public bool Compress;             // true = compress, false = expand
}

public struct CompressionConfig : IComponentData
{
    public uint MinTicksBeforeCompress;   // Don't compress recently viewed
    public float BackgroundTickRate;       // 0.1 = 10% speed when compressed
    public uint MaxCompressedGroups;       // Memory budget
    public bool PreserveEvents;            // Log pseudo-history
}

// Pseudo-history for compressed periods
[InternalBufferCapacity(20)]
public struct CompressedEvent : IBufferElementData
{
    public uint EventTick;
    public FixedString64Bytes EventType;  // "famine", "victory", "growth"
    public FixedString128Bytes Summary;   // "Population grew by 15%"
    public int PopulationDelta;
    public float ResourceDelta;
}
```

### New Systems
- `AggregateRegistrySystem` - Calculates aggregates from entities
- `CompressionSystem` - Manages transition to/from compressed mode
- `BackgroundSimSystem` - Lightweight tick for compressed groups
- `PseudoHistorySystem` - Generates events during compression

---

## Example Usage

```csharp
// === Request compression when player leaves island ===
var request = new CompressionRequest { TargetGroup = islandEntity, Compress = true };
EntityManager.AddComponentData(islandEntity, request);

// === Query aggregate without iterating all villagers ===
var aggregate = RegistryHelpers.GetAggregate<AggregateRegistryEntry>(villageEntity);
HUD.DisplayPopulation(aggregate.TotalPopulation);
HUD.DisplayMorale(aggregate.AverageMorale);

// === Expand when player returns ===
// System detects player camera near compressed island
// Generates pseudo-history: "While you were away..."
var events = EntityManager.GetBuffer<CompressedEvent>(islandEntity);
foreach (var evt in events)
{
    NotificationSystem.Show(evt.Summary);
}
// Then expands to full simulation
```

---

## Impact Assessment

**Files/Systems Affected:**
- Expand: Registry contracts in `Packages/com.moni.puredots/Runtime/Registry/`
- New: `AggregateRegistrySystem.cs`, `CompressionSystem.cs`
- Integration: Spatial system (detect player proximity)

**Breaking Changes:**
- Additive to existing registries
- Games opt-in to compression

---

## Review Notes

*(PureDOTS team use)*

