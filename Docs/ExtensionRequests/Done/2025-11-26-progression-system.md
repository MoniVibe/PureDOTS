# Extension Request: Individual Progression System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P1  
**Assigned To**: TBD

---

## Use Case

Both games need XP/skill progression for individual entities:
- **Space4X**: Crew skill trees, officer specializations, veteran bonuses
- **Godgame**: Villager mastery, profession unlocks, talent discovery

Progression should support:
- Multiple skill domains (Combat, Crafting, Social, etc.)
- Mastery tiers (Novice → Apprentice → Journeyman → Adept → Master → Grandmaster)
- Skill/passive unlocks at thresholds
- Player-guided vs autonomous specialization (preordained paths)

---

## Proposed Solution

**Extension Type**: New Components + Systems

### Components (`Packages/com.moni.puredots/Runtime/Runtime/Progression/`)

```csharp
public enum SkillDomain : byte
{
    None = 0,
    Combat = 1, Finesse = 2, Arcane = 3, Divine = 4,
    Crafting = 10, Gathering = 11, Refining = 12,
    Social = 20, Leadership = 21, Teaching = 22,
    Survival = 30, Navigation = 31, Engineering = 32
}

public enum SkillMastery : byte
{
    Untrained = 0,    // 0-19 XP
    Novice = 1,       // 20-49 XP
    Apprentice = 2,   // 50-99 XP
    Journeyman = 3,   // 100-199 XP
    Adept = 4,        // 200-499 XP
    Master = 5,       // 500-999 XP
    Grandmaster = 6   // 1000+ XP
}

public struct CharacterProgression : IComponentData
{
    public uint TotalXPEarned;
    public uint XPToNextLevel;
    public byte Level;
    public byte SkillPoints;         // Unspent points for unlocks
    public byte TalentPoints;        // For passive tree
}

[InternalBufferCapacity(8)]
public struct SkillXP : IBufferElementData
{
    public SkillDomain Domain;
    public uint CurrentXP;
    public SkillMastery Mastery;
}

[InternalBufferCapacity(16)]
public struct UnlockedSkill : IBufferElementData
{
    public FixedString32Bytes SkillId;
    public SkillDomain Domain;
    public byte Tier;
    public uint UnlockedTick;
}

public struct PreordainedPath : IComponentData
{
    public SkillDomain PrimaryDomain;
    public SkillDomain SecondaryDomain;
    public byte AutoSpecializeThreshold;  // Auto-pick skills below this tier
    public bool PlayerGuided;             // If true, player picks unlocks
}
```

### Systems

```csharp
// XPAllocationSystem - Awards XP from actions, updates mastery
// SkillUnlockSystem - Checks thresholds, unlocks skills
// ProgressionHelpers - Static formulas
public static class ProgressionHelpers
{
    public static SkillMastery GetMasteryForXP(uint xp);
    public static uint GetXPThreshold(SkillMastery mastery);
    public static float GetMasteryBonus(SkillMastery mastery); // 0% to 50%
}
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Runtime/Progression/ProgressionComponents.cs`
- New: `Packages/com.moni.puredots/Runtime/Systems/Progression/XPAllocationSystem.cs`
- New: `Packages/com.moni.puredots/Runtime/Systems/Progression/SkillUnlockSystem.cs`

**Breaking Changes:** None - new feature

---

## Example Usage

```csharp
// Award XP for completing a craft
var skillBuffer = EntityManager.GetBuffer<SkillXP>(crafterEntity);
XPAllocationHelpers.AwardXP(ref skillBuffer, SkillDomain.Crafting, 50);

// Check mastery for unlock gating
var craftingXP = SkillHelpers.GetSkillXP(skillBuffer, SkillDomain.Crafting);
if (craftingXP.Mastery >= SkillMastery.Journeyman)
{
    // Can use advanced crafting abilities
}

// Get mastery bonus
float bonus = ProgressionHelpers.GetMasteryBonus(craftingXP.Mastery);
float craftQuality = baseQuality * (1f + bonus);
```

---

## Reference Implementation

`Godgame/Assets/Scripts/Godgame/Progression/`
- `ProgressionComponents.cs`
- `XPAllocationSystem.cs`
- `SkillUnlockSystem.cs`

---

## Review Notes

*(PureDOTS team use)*

