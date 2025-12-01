# Extension Request: Loyalty / Patriotism System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Both games need loyalty tracking to factions/organizations:

**Godgame:**
- Villager loyalty to village (affects desertion during hardship)
- Band loyalty (affects mutiny, route during battle)
- Religious devotion (affects miracle effectiveness)

**Space4X:**
- Crew loyalty to fleet/captain (affects mutiny risk)
- Faction patriotism (affects defection, espionage susceptibility)
- Colony loyalty (affects rebellion risk, conscription willingness)

Loyalty affects morale, willingness to sacrifice, susceptibility to bribes/propaganda, and triggers desertion/mutiny events.

---

## Proposed Solution

**Extension Type**: New Components + System

### Components (`Packages/com.moni.puredots/Runtime/Runtime/Social/`)

```csharp
public enum LoyaltyTarget : byte
{
    None = 0,
    // Organizational
    Village = 1,          // Local settlement
    Band = 2,             // Military unit
    Faction = 3,          // Nation/corporation
    Fleet = 4,            // Space4X fleet
    Colony = 5,           // Space4X colony
    // Personal
    Leader = 10,          // Specific leader entity
    Family = 11,          // Blood relatives
    Religion = 12,        // Faith/doctrine
    Ideology = 13         // Political belief
}

public enum LoyaltyState : byte
{
    Traitor = 0,          // Actively working against (0-19)
    Disloyal = 1,         // Susceptible to defection (20-39)
    Neutral = 2,          // No strong feelings (40-59)
    Loyal = 3,            // Committed (60-79)
    Fanatic = 4           // Will die for cause (80-100)
}

public struct EntityLoyalty : IComponentData
{
    public Entity PrimaryTarget;       // Main loyalty target (village/fleet/faction)
    public LoyaltyTarget TargetType;
    public byte Loyalty;               // 0-100
    public LoyaltyState State;         // Derived from loyalty value
    public byte NaturalLoyalty;        // Innate tendency (some are naturally loyal)
    public float DesertionRisk;        // Current chance to desert (0-1)
    public uint LastLoyaltyChangeTick;
}

[InternalBufferCapacity(4)]
public struct SecondaryLoyalty : IBufferElementData
{
    public Entity Target;
    public LoyaltyTarget TargetType;
    public byte Loyalty;
    public LoyaltyState State;
}

public struct LoyaltyConfig : IComponentData
{
    public float BaseDesertionThreshold;  // Loyalty below which desertion possible (30)
    public float MutinyThreshold;         // Average loyalty for mutiny check (25)
    public float FanaticThreshold;        // Loyalty for fanatic bonuses (80)
    public float LoyaltyDecayRate;        // Per-day decay without reinforcement
    public float HardshipPenalty;         // Loyalty loss per hardship event
    public float VictoryBonus;            // Loyalty gain per victory
}

public struct LoyaltyModifiers : IComponentData
{
    public float MoraleBonus;             // From high loyalty
    public float SacrificeWillingness;    // Chance to take damage for ally
    public float BribeResistance;         // Resistance to corruption
    public float PropagandaResistance;    // Resistance to enemy propaganda
    public float ConsciousnessCap;        // Max conscription rate accepted
}
```

### Systems

```csharp
// LoyaltyUpdateSystem - Updates loyalty based on events, calculates desertion risk
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct LoyaltyUpdateSystem : ISystem { }

// LoyaltyEventSystem - Processes loyalty-affecting events (victories, hardships)
public partial struct LoyaltyEventSystem : ISystem { }

public static class LoyaltyHelpers
{
    public static LoyaltyState GetState(byte loyalty);
    public static float GetDesertionRisk(byte loyalty, in LoyaltyConfig config);
    public static float GetMoraleBonus(byte loyalty); // -20% to +20%
    public static bool WillDesert(byte loyalty, float hardshipLevel, uint seed);
    public static bool WillMutiny(float averageLoyalty, in LoyaltyConfig config);
    public static void ApplyHardship(ref EntityLoyalty loyalty, float severity, in LoyaltyConfig config);
    public static void ApplyVictory(ref EntityLoyalty loyalty, float magnitude, in LoyaltyConfig config);
}
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Runtime/Social/LoyaltyComponents.cs`
- New: `Packages/com.moni.puredots/Runtime/Systems/Social/LoyaltyUpdateSystem.cs`
- Integration: Mood system (loyalty affects base morale)
- Integration: Combat system (loyalty affects rout/flee behavior)

**Breaking Changes:** None - new feature

---

## Example Usage

```csharp
// === Godgame: Check desertion during famine ===
var loyalty = EntityManager.GetComponentData<EntityLoyalty>(villagerEntity);
LoyaltyHelpers.ApplyHardship(ref loyalty, 0.3f, config); // Famine severity

if (LoyaltyHelpers.WillDesert(loyalty.Loyalty, hardshipLevel, randomSeed))
{
    // Villager leaves village
    DesertionSystem.ProcessDesertion(villagerEntity);
}

// === Space4X: Check mutiny risk on fleet ===
float averageLoyalty = FleetHelpers.GetAverageLoyalty(fleetEntity);
if (LoyaltyHelpers.WillMutiny(averageLoyalty, config))
{
    // Crew mutinies
    MutinySystem.TriggerMutiny(fleetEntity);
}

// === Combat: Fanatic bonus ===
var loyalty = EntityManager.GetComponentData<EntityLoyalty>(soldierEntity);
if (loyalty.State == LoyaltyState.Fanatic)
{
    damageResistance += 0.2f; // +20% resistance when fighting for cause
    morale += 0.3f;           // +30% morale
}

// === Conscription check (Space4X) ===
var mods = EntityManager.GetComponentData<LoyaltyModifiers>(colonistEntity);
if (conscriptionRate > mods.ConsciousnessCap)
{
    // Colonist refuses conscription, loyalty penalty
    loyalty.Loyalty -= 10;
}

// === Bribe resistance ===
float bribeSuccess = baseBribeChance * (1f - mods.BribeResistance);
if (Random.value < bribeSuccess)
{
    // Entity accepts bribe, becomes disloyal
}
```

---

## Reference Implementation

`Godgame/Assets/Scripts/Godgame/AI/`
- `PatriotismComponents.cs`
- `PatriotismSystem.cs`

---

## Review Notes

*(PureDOTS team use)*

