# Extension Request: Morale Tiers / Mood Band System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P1  
**Assigned To**: TBD

---

## Use Case

Both games need morale tiers that affect entity behavior:

**Godgame:**
- Villager mood affects work speed, initiative, breakdown risk
- Elated villagers inspire others, Despairing villagers may break down
- Mood memories decay over time (triumphs, traumas)

**Space4X:**
- Crew morale affects performance, mutiny risk
- Colony morale affects productivity, rebellion risk
- Fleet morale affects combat effectiveness

Morale is a spectrum with discrete tiers that apply specific modifiers.

---

## Proposed Solution

**Extension Type**: New Components + System

### Components (`Packages/com.moni.puredots/Runtime/Runtime/Social/`)

```csharp
public enum MoraleBand : byte
{
    Despair = 0,      // 0-199: -40% initiative, breakdown risk, health decay
    Unhappy = 1,      // 200-399: -15% work speed, social friction
    Stable = 2,       // 400-599: neutral baseline
    Cheerful = 3,     // 600-799: +10% work speed, +5% faith/loyalty gain
    Elated = 4        // 800-1000: +25% initiative, inspire allies, burnout risk
}

public enum MoraleModifierCategory : byte
{
    Needs = 0,          // Food, rest, hygiene
    Environment = 1,    // Weather, lighting, beauty, ship quality
    Relationships = 2,  // Friends, family, leadership
    Events = 3,         // Victories, disasters, miracles
    Health = 4,         // Injuries, illness
    Work = 5            // Job satisfaction, achievements
}

public struct EntityMorale : IComponentData
{
    public float CurrentMorale;        // 0-1000
    public MoraleBand Band;            // Derived from current morale
    public float WorkSpeedModifier;    // -0.20 to +0.15
    public float InitiativeModifier;   // -0.40 to +0.25
    public byte BreakdownRisk;         // 0-100 (Despair band)
    public byte BurnoutRisk;           // 0-100 (Elated band)
    public uint LastBandChangeTick;
}

[InternalBufferCapacity(8)]
public struct MoraleModifier : IBufferElementData
{
    public FixedString32Bytes ModifierId;
    public MoraleModifierCategory Category;
    public sbyte Magnitude;            // -100 to +100
    public uint RemainingTicks;        // 0 = permanent
    public uint DecayHalfLife;         // Ticks until half-magnitude
}

[InternalBufferCapacity(6)]
public struct MoraleMemory : IBufferElementData
{
    public FixedString32Bytes MemoryType;  // "trauma", "triumph", "betrayal"
    public sbyte InitialMagnitude;
    public sbyte CurrentMagnitude;
    public uint FormedTick;
    public uint DecayHalfLife;
    public Entity AssociatedEntity;
}

public struct MoraleConfig : IComponentData
{
    public float DespairThreshold;     // 200
    public float UnhappyThreshold;     // 400
    public float CheerfulThreshold;    // 600
    public float ElatedThreshold;      // 800
    public float MaxMorale;            // 1000
    public uint BreakdownCheckInterval;
}
```

### System

```csharp
// MoraleBandSystem - Recalculates bands, applies modifiers
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MoraleBandSystem : ISystem { }

// MoraleMemoryDecaySystem - Decays memories over time
public partial struct MoraleMemoryDecaySystem : ISystem { }

public static class MoraleHelpers
{
    public static MoraleBand GetBand(float morale, in MoraleConfig config);
    public static void ApplyModifier(ref DynamicBuffer<MoraleModifier> buffer, FixedString32Bytes id, sbyte magnitude);
    public static float CalculateTotalModifier(in DynamicBuffer<MoraleModifier> buffer);
    public static bool ShouldBreakdown(byte risk, uint seed);
}
```

---

## Example Usage

```csharp
// === Godgame: Apply miracle boost ===
var modifiers = EntityManager.GetBuffer<MoraleModifier>(villagerEntity);
MoraleHelpers.ApplyModifier(ref modifiers, "miracle_blessing", 50);

// === Space4X: Check crew breakdown during crisis ===
var morale = EntityManager.GetComponentData<EntityMorale>(crewEntity);
if (morale.Band == MoraleBand.Despair && MoraleHelpers.ShouldBreakdown(morale.BreakdownRisk, seed))
{
    // Crew member has mental breakdown
}

// === Apply morale to work speed ===
float finalWorkSpeed = baseSpeed * (1f + morale.WorkSpeedModifier);
```

---

## Reference Implementation

`Godgame/Assets/Scripts/Godgame/AI/`
- `MoodBandComponents.cs`
- `MoodBandSystem.cs`

---

## Review Notes

*(PureDOTS team use)*

