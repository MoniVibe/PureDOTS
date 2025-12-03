# Extension Request: Structure Durability System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Both games need durability tracking for structures:
- **Space4X**: Hull integrity, module health, station damage
- **Godgame**: Building structural damage, wall breaches, siege damage

Durability affects functionality (penalties at thresholds), triggers repairs, and determines destruction.

---

## Proposed Solution

**Extension Type**: New Components + System

### Components (`Packages/com.moni.puredots/Runtime/Runtime/Structures/`)

```csharp
public enum DurabilityState : byte
{
    Pristine = 0,      // 100%
    Good = 1,          // 75-99%
    Worn = 2,          // 50-74%
    Damaged = 3,       // 25-49%
    Critical = 4,      // 1-24%
    Destroyed = 5      // 0%
}

public struct StructureDurability : IComponentData
{
    public float CurrentDurability;
    public float MaxDurability;
    public DurabilityState State;
    public float DamagedThreshold;    // % below which penalties apply (default 0.5)
    public float CriticalThreshold;   // % for severe penalties (default 0.25)
    public float EfficiencyPenalty;   // Current penalty (0-1)
    public bool NeedsRepair;
    public uint LastDamageTick;
}

public struct DurabilityConfig : IComponentData
{
    public float DamagedEfficiencyPenalty;    // e.g., 0.25 = -25% at Damaged
    public float CriticalEfficiencyPenalty;   // e.g., 0.5 = -50% at Critical
    public float NaturalDecayRate;            // Per-day decay (0 = no decay)
    public bool AutoQueueRepair;              // Auto-queue when damaged
}
```

### System

```csharp
// StructureDurabilitySystem - Updates state, applies penalties, queues repairs
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct StructureDurabilitySystem : ISystem { }

public static class DurabilityHelpers
{
    public static DurabilityState GetState(float current, float max);
    public static float GetEfficiencyPenalty(in StructureDurability durability, in DurabilityConfig config);
    public static void ApplyDamage(ref StructureDurability durability, float damage);
    public static void Repair(ref StructureDurability durability, float amount);
}
```

---

## Example Usage

```csharp
// Apply siege damage
var durability = EntityManager.GetComponentData<StructureDurability>(wallEntity);
DurabilityHelpers.ApplyDamage(ref durability, 50f);
EntityManager.SetComponentData(wallEntity, durability);

// Check efficiency for production
float efficiency = 1f - durability.EfficiencyPenalty;
float productionRate = baseRate * efficiency; // Damaged buildings produce less
```

---

## Reference Implementation

`Godgame/Assets/Scripts/Godgame/Buildings/`
- `BuildingDurabilityComponents.cs`
- `BuildingDurabilitySystem.cs`

---

## Review Notes

*(PureDOTS team use)*

