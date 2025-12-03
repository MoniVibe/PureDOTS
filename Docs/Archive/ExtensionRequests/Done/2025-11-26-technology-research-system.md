# Extension Request: Technology & Research System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Completed**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P2  

### Implementation
- `Packages/com.moni.puredots/Runtime/Runtime/Technology/TechnologyComponents.cs` - TechLevel, ResearchProject, RecipeUnlock, ResearchContributor, KnowledgePool, TechTransferRequest, TechConfig
- `Packages/com.moni.puredots/Runtime/Runtime/Technology/TechnologyHelpers.cs` - Static helpers for research rate, tier advancement, tech transfer

---

## Use Case

Technology progression is needed for:

**Godgame:**
- 11 tech tiers (Stone Age → Magitech)
- Recipe unlocks per tier
- Building upgrades requiring tech level
- Knowledge discovery through education
- Technology transfer between villages

**Space4X:**
- Ship/weapon tech trees
- Research projects
- Reverse engineering alien tech
- Colony tech requirements

---

## Proposed Components

```csharp
// === Tech Tiers ===
public struct TechLevel : IComponentData
{
    public byte CurrentTier;            // 0-10 for Godgame, 0-N for Space4X
    public float ResearchProgress;      // Progress to next tier
    public uint TierUnlockedTick;       // When this tier was reached
}

// === Research Projects ===
public struct ResearchProject : IBufferElementData
{
    public FixedString64Bytes ProjectId;
    public FixedString32Bytes Category;  // "metallurgy", "weapons", "magic"
    public byte RequiredTier;            // Min tech tier to start
    public float TotalResearchCost;      // Research points needed
    public float CurrentProgress;        // Points invested so far
    public bool IsCompleted;
    public bool IsActive;                // Currently being researched
}

// === Recipe Unlocks ===
public struct RecipeUnlock : IBufferElementData
{
    public FixedString64Bytes RecipeId;  // What can now be crafted
    public FixedString64Bytes UnlockedBy; // Project or tier that unlocked it
    public uint UnlockedTick;
}

// === Research Contribution ===
public struct ResearchContributor : IComponentData
{
    public Entity ContributingTo;        // Village/colony entity
    public float ResearchRate;           // Points per tick
    public float EfficiencyModifier;     // Education, facilities
    public FixedString32Bytes Specialty; // Bonus to certain categories
}

// === Knowledge/Discovery ===
public struct KnowledgePool : IComponentData
{
    public float AccumulatedKnowledge;   // Cultural knowledge
    public float KnowledgeDecayRate;     // Lost if not maintained
    public byte MaxTierSupported;        // Can't exceed without scholars
}

// === Tech Transfer ===
public struct TechTransferRequest : IComponentData
{
    public Entity SourceEntity;          // Village with higher tech
    public Entity TargetEntity;          // Village receiving tech
    public byte TechTierToTransfer;
    public float TransferProgress;       // 0-1
    public FixedString32Bytes TransferMethod; // "trade", "espionage", "conquest"
}

// === Configuration ===
public struct TechConfig : IComponentData
{
    public float BaseResearchCostPerTier;
    public float TierCostMultiplier;     // Each tier costs more
    public float KnowledgeToResearchRatio;
    public float TransferSpeedModifier;
    public bool AllowTechRegression;     // Can you lose tiers?
}
```

### New Systems
- `ResearchProgressSystem` - Advances active projects
- `TechTierCheckSystem` - Evaluates tier advancement
- `RecipeUnlockSystem` - Grants recipes when prerequisites met
- `TechTransferSystem` - Handles tech spread between entities
- `KnowledgeDecaySystem` - Optional knowledge maintenance

---

## Example Usage

```csharp
// === Village reaches Iron Age ===
// Tier 1 (Bronze) → Tier 2 (Iron)
// Prerequisites: Completed "Iron Smelting" research project
// Result: Unlocks iron weapons, iron armor, better tools

// === Scholar contributes research ===
var contributor = new ResearchContributor {
    ContributingTo = villageEntity,
    ResearchRate = 5f,           // 5 points per tick
    EfficiencyModifier = 1.2f,   // 20% bonus from library
    Specialty = "metallurgy"     // Extra bonus to metal research
};

// === Tech transfer via trade ===
var transfer = new TechTransferRequest {
    SourceEntity = advancedVillage,
    TargetEntity = primitiveVillage,
    TechTierToTransfer = 3,      // Steel Age
    TransferMethod = "trade"
};
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Technology/` directory
- Integration: Crafting systems, building requirements

**Breaking Changes:** None - new system

---

## Review Notes

*(PureDOTS team use)*

