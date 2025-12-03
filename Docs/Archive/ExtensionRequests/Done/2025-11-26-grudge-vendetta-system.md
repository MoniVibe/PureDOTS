# Extension Request: Grudge / Vendetta System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Both games need persistent grievance tracking between entities:

**Godgame:**
- Villager grudges from theft, assault, broken promises
- Family vendettas spanning generations
- Band rivalries from past conflicts

**Space4X:**
- Crew grudges against harsh officers
- Faction vendettas from betrayals
- Fleet rivalries from past engagements
- Pirate blood feuds

Grudges affect cooperation, combat targeting priority, diplomacy, and can escalate to violence.

---

## Proposed Solution

**Extension Type**: New Components + System

### Components (`Packages/com.moni.puredots/Runtime/Runtime/Social/`)

```csharp
public enum GrudgeType : byte
{
    None = 0,
    // Personal
    Insult = 1,           // Minor slight
    Theft = 2,            // Took something
    Assault = 3,          // Physical harm
    Betrayal = 4,         // Broken trust
    Murder = 5,           // Killed loved one
    // Professional
    Demotion = 10,        // Career harm
    Sabotage = 11,        // Damaged work
    CreditStolen = 12,    // Took credit for work
    // Factional
    WarCrime = 20,        // Atrocity against faction
    TerritoryLoss = 21,   // Took land/space
    EconomicHarm = 22     // Trade war damage
}

public enum GrudgeSeverity : byte
{
    Forgotten = 0,        // Decayed to nothing
    Minor = 1,            // Annoyance (intensity 1-25)
    Moderate = 2,         // Resentment (26-50)
    Serious = 3,          // Hatred (51-75)
    Vendetta = 4          // Blood feud (76-100)
}

[InternalBufferCapacity(8)]
public struct EntityGrudge : IBufferElementData
{
    public Entity OffenderEntity;      // Who wronged them
    public GrudgeType Type;
    public byte Intensity;             // 0-100, decays over time
    public GrudgeSeverity Severity;    // Derived from intensity
    public uint OriginTick;            // When grudge formed
    public uint LastRenewedTick;       // When intensity was refreshed
    public bool IsInherited;           // Passed down from family/faction
    public bool IsPublic;              // Known to others (affects reputation)
}

public struct GrudgeConfig : IComponentData
{
    public float DecayRatePerDay;      // How fast grudges fade (0.5 = lose 0.5 intensity/day)
    public byte MinIntensityForAction; // Threshold for hostile action (default 50)
    public byte VendettaThreshold;     // When it becomes blood feud (default 75)
    public float InheritanceDecay;     // How much intensity children inherit (0.5 = 50%)
    public bool AllowForgiveness;      // Can grudges be resolved diplomatically
}

public struct GrudgeBehavior : IComponentData
{
    public byte Vengefulness;          // 0-100, how likely to hold grudges
    public byte Forgiveness;           // 0-100, how fast grudges decay
    public bool SeeksRevenge;          // Will actively pursue vendetta targets
    public byte RevengeThreshold;      // Intensity needed to seek revenge
}
```

### System

```csharp
// GrudgeDecaySystem - Fades grudges over time based on forgiveness
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct GrudgeDecaySystem : ISystem { }

// GrudgeEscalationSystem - Upgrades severity, triggers vendetta events
public partial struct GrudgeEscalationSystem : ISystem { }

public static class GrudgeHelpers
{
    public static void AddGrudge(ref DynamicBuffer<EntityGrudge> buffer, Entity offender, GrudgeType type, byte baseIntensity);
    public static void RenewGrudge(ref EntityGrudge grudge, byte additionalIntensity);
    public static GrudgeSeverity GetSeverity(byte intensity);
    public static bool ShouldSeekRevenge(in EntityGrudge grudge, in GrudgeBehavior behavior);
    public static float GetCooperationPenalty(byte intensity); // -10% to -100%
    public static float GetCombatTargetPriority(byte intensity); // Prioritize grudge targets
}
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Runtime/Social/GrudgeComponents.cs`
- New: `Packages/com.moni.puredots/Runtime/Systems/Social/GrudgeDecaySystem.cs`
- Integration: Relations system (grudges affect relation intensity)

**Breaking Changes:** None - new feature

---

## Example Usage

```csharp
// === Godgame: Villager witnesses theft ===
var grudges = EntityManager.GetBuffer<EntityGrudge>(victimEntity);
GrudgeHelpers.AddGrudge(ref grudges, thiefEntity, GrudgeType.Theft, 40);

// === Space4X: Crew resents harsh punishment ===
var grudges = EntityManager.GetBuffer<EntityGrudge>(crewEntity);
GrudgeHelpers.AddGrudge(ref grudges, officerEntity, GrudgeType.Assault, 60);

// Check if grudge affects cooperation
var grudge = GrudgeHelpers.FindGrudge(grudges, targetEntity);
if (grudge.Intensity > 0)
{
    float penalty = GrudgeHelpers.GetCooperationPenalty(grudge.Intensity);
    workEfficiency *= (1f + penalty); // Negative = penalty
}

// Combat targeting - prioritize grudge targets
if (GrudgeHelpers.ShouldSeekRevenge(grudge, behavior))
{
    targetPriority += grudge.Intensity; // More likely to attack
}

// === Faction vendetta (Space4X) ===
// When faction member is killed, faction-wide grudge forms
foreach (var factionMember in factionMembers)
{
    var grudges = EntityManager.GetBuffer<EntityGrudge>(factionMember);
    GrudgeHelpers.AddGrudge(ref grudges, killerFaction, GrudgeType.Murder, 80);
}
```

---

## Reference Implementation

`Godgame/Assets/Scripts/Godgame/Villagers/`
- `VillagerGrudgeComponents.cs`
- `VillagerGrudgeDecaySystem.cs`

---

## Review Notes

*(PureDOTS team use)*

