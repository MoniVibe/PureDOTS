# Extension Request: Status Effects System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P1  
**Assigned To**: TBD

---

## Use Case

Both games need a generic buff/debuff system for temporary modifiers:
- **Combat**: Poison, bleed, stun, slow, haste, shield, rage
- **Environmental**: Wet, burning, frozen, irradiated, diseased
- **Social**: Inspired, demoralized, terrified, charmed
- **Economic**: Productive, exhausted, overworked

Effects need duration tracking, stacking rules, tick-based damage/healing, and cleansing.

---

## Proposed Solution

**Extension Type**: New Components + System

### Components (`Packages/com.moni.puredots/Runtime/Runtime/Effects/`)

```csharp
public enum StatusEffectType : byte
{
    None = 0,
    // Damage over time
    Poison = 1, Bleed = 2, Burn = 3, Freeze = 4, Irradiated = 5,
    // Crowd control
    Stun = 10, Slow = 11, Root = 12, Silence = 13, Blind = 14,
    // Buffs
    Haste = 20, Shield = 21, Regen = 22, Inspired = 23, Empowered = 24,
    // Debuffs
    Weakness = 30, Vulnerability = 31, Exhaustion = 32, Demoralized = 33,
    // Special
    Invulnerable = 40, Coma = 41, MentalBreakdown = 42
}

public enum StackBehavior : byte
{
    Replace,           // New effect replaces old
    Refresh,           // Reset duration, keep stacks
    Stack,             // Add stacks up to max
    StackDuration,     // Extend duration
    Ignore             // Don't apply if already present
}

[InternalBufferCapacity(8)]
public struct ActiveStatusEffect : IBufferElementData
{
    public StatusEffectType Type;
    public float Duration;           // Remaining duration (< 0 = permanent)
    public float Value;              // Effect magnitude (damage/heal per tick, slow %, etc.)
    public byte Stacks;              // Current stack count
    public byte MaxStacks;           // Maximum stacks
    public Entity SourceEntity;      // Who applied it
    public uint AppliedTick;
}

public struct StatusEffectConfig : IComponentData
{
    public float TickInterval;       // How often DoT/HoT ticks (default 1s)
    public byte MaxEffectsPerEntity; // Prevent effect spam
}
```

### System

```csharp
// StatusEffectSystem - Ticks durations, applies periodic effects, removes expired
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct StatusEffectSystem : ISystem { }

// StatusEffectHelpers - Query active effects, calculate totals
public static class StatusEffectHelpers
{
    public static bool HasEffect(DynamicBuffer<ActiveStatusEffect> buffer, StatusEffectType type);
    public static float GetTotalSlowPercent(DynamicBuffer<ActiveStatusEffect> buffer);
    public static float GetTotalDamagePerSecond(DynamicBuffer<ActiveStatusEffect> buffer);
    public static int CountStacks(DynamicBuffer<ActiveStatusEffect> buffer, StatusEffectType type);
}
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Runtime/Effects/StatusEffectComponents.cs`
- New: `Packages/com.moni.puredots/Runtime/Systems/Effects/StatusEffectSystem.cs`

**Breaking Changes:** None - new feature

---

## Example Usage

```csharp
// Apply poison from combat
var effects = EntityManager.GetBuffer<ActiveStatusEffect>(targetEntity);
effects.Add(new ActiveStatusEffect {
    Type = StatusEffectType.Poison,
    Duration = 10f,
    Value = 5f,  // 5 damage per tick
    Stacks = 1,
    MaxStacks = 3,
    SourceEntity = attackerEntity
});

// Check for crowd control
if (StatusEffectHelpers.HasEffect(effects, StatusEffectType.Stun))
{
    // Skip this entity's turn
}

// Calculate movement speed reduction
float slowPercent = StatusEffectHelpers.GetTotalSlowPercent(effects);
float finalSpeed = baseSpeed * (1f - slowPercent);
```

---

## Reference Implementation

`Godgame/Assets/Scripts/Godgame/Effects/`
- `StatusEffectComponents.cs`
- `StatusEffectSystem.cs`

---

## Review Notes

*(PureDOTS team use)*

