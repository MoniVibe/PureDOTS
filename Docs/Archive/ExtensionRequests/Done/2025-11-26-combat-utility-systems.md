# Extension Request: Combat Utility Systems

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P2  
**Assigned To**: PureDOTS Team

---

## Use Case

Both Godgame and Space4X need common combat utility systems for:

1. **Target Selection**: Priority queues, threat assessment, target switching
2. **Range Checks**: Melee/ranged/AOE distance queries
3. **Hit Calculation**: Accuracy, dodge, armor, damage rolls
4. **Projectile/AOE Resolution**: Burst vs sustained effects, splash damage
5. **Combat State Machine**: Engaged/fleeing/stunned/recovering states

These are fundamental combat mechanics that should be shared rather than duplicated.

---

## Proposed Solution

Create a shared combat utilities package in PureDOTS.

**Extension Type**: New Components + Systems + Helpers

**Details:**

### Target Selection (`Packages/com.moni.puredots/Runtime/Runtime/Combat/Targeting/`)

```csharp
public struct TargetPriority : IComponentData
{
    public Entity CurrentTarget;
    public float ThreatScore;
    public float LastEngagedTick;
    public TargetingStrategy Strategy;
}

public enum TargetingStrategy : byte
{
    Nearest, LowestHealth, HighestThreat, Random, PlayerAssigned
}

[InternalBufferCapacity(8)]
public struct PotentialTarget : IBufferElementData
{
    public Entity Target;
    public float Distance;
    public float ThreatScore;
    public byte Priority;
}
```

### Range Utilities (`Packages/com.moni.puredots/Runtime/Runtime/Combat/Range/`)

```csharp
public struct CombatRange : IComponentData
{
    public float MeleeRange;        // Close combat
    public float RangedMinRange;    // Minimum for ranged
    public float RangedMaxRange;    // Maximum for ranged
    public float AOERadius;         // Area effect radius
}

public static class RangeHelpers
{
    public static bool InMeleeRange(float3 a, float3 b, float range);
    public static bool InRangedRange(float3 a, float3 b, float min, float max);
    public static int GetEntitiesInAOE(float3 center, float radius, NativeList<Entity> results);
}
```

### Hit Calculation (`Packages/com.moni.puredots/Runtime/Runtime/Combat/HitCalc/`)

```csharp
public struct HitCalculationInput
{
    public float BaseAccuracy;
    public float AttackerBonus;
    public float DefenderDodge;
    public float DefenderArmor;
    public float BaseDamage;
    public DamageType DamageType;
}

public struct HitCalculationResult
{
    public bool Hit;
    public bool Critical;
    public float FinalDamage;
    public float DamageReduced;
}

public static class HitCalculator
{
    public static HitCalculationResult Calculate(in HitCalculationInput input, uint seed);
    public static float ApplyArmorReduction(float damage, float armor, ArmorType type);
    public static bool RollCritical(float critChance, uint seed);
}
```

### Combat State (`Packages/com.moni.puredots/Runtime/Runtime/Combat/State/`)

```csharp
public enum CombatState : byte
{
    Idle, Approaching, Engaged, Attacking, Defending, 
    Stunned, Fleeing, Recovering, Dead
}

public struct CombatStateData : IComponentData
{
    public CombatState Current;
    public CombatState Previous;
    public uint StateEnteredTick;
    public float StunDuration;
    public Entity FleeTarget;
}
```

---

## Impact Assessment

**Files/Systems Affected:**
- New directories under `Packages/com.moni.puredots/Runtime/Runtime/Combat/`
- New systems under `Packages/com.moni.puredots/Runtime/Systems/Combat/`

**Breaking Changes:**
- No breaking changes - entirely new feature
- Games opt-in by adding components

---

## Example Usage

```csharp
// === Target Selection ===
var targets = EntityManager.GetBuffer<PotentialTarget>(attackerEntity);
var priority = EntityManager.GetComponentData<TargetPriority>(attackerEntity);

// System populates potential targets based on spatial queries
// Then selects best target based on strategy
Entity bestTarget = TargetSelectionHelpers.SelectBest(targets, priority.Strategy);

// === Range Check ===
var range = EntityManager.GetComponentData<CombatRange>(attackerEntity);
var attackerPos = EntityManager.GetComponentData<LocalTransform>(attackerEntity).Position;
var targetPos = EntityManager.GetComponentData<LocalTransform>(targetEntity).Position;

if (RangeHelpers.InMeleeRange(attackerPos, targetPos, range.MeleeRange))
{
    // Perform melee attack
}

// === Hit Calculation ===
var input = new HitCalculationInput
{
    BaseAccuracy = 0.8f,
    AttackerBonus = focusEffects.GetCritBonus(buffer),
    DefenderDodge = defenderStats.DodgeRating,
    DefenderArmor = defenderStats.Armor,
    BaseDamage = weaponDamage,
    DamageType = DamageType.Physical
};

var result = HitCalculator.Calculate(input, randomSeed);
if (result.Hit)
{
    health.Current -= result.FinalDamage;
}
```

---

## Alternative Approaches Considered

- **Alternative 1**: Each game implements own combat system
  - **Rejected**: Massive duplication, inconsistent mechanics between games

- **Alternative 2**: Use existing physics system for combat
  - **Rejected**: Physics is for movement/collision, not damage/accuracy calculations

---

## Implementation Notes

**Dependencies:**
- Spatial grid for target queries
- Focus system for combat modifiers (see related request)
- TimeState for state duration tracking

**Performance Considerations:**
- All systems Burst-compiled
- Target selection uses spatial partitioning
- Hit calculations are pure functions (no allocations)

**Related Requests:**
- `2025-11-26-focus-system-universal-resource.md` - Focus affects hit/damage modifiers

---

## Review Notes

*(This section is for PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

