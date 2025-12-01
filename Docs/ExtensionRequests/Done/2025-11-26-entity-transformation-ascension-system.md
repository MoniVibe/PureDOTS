# Extension Request: Entity Transformation & Ascension System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Completed**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P2  

### Implementation
- `Packages/com.moni.puredots/Runtime/Runtime/Transformation/TransformationComponents.cs` - TransformationPotential, TransformationTrigger, TransformationInProgress, RetainedIdentity, TransformedEntity, RetainedMemory
- `Packages/com.moni.puredots/Runtime/Runtime/Transformation/TransformationHelpers.cs` - Static helpers for requirement checking, transformation rolls, identity retention

---

## Use Case

Entity transformations are needed for:

**Godgame:**
- Villager → Demon (thrown by god, corrupted)
- Villager → Angel (ascended through devotion)
- Villager → Lich (dark magic mastery)
- Villager → Champion (divine blessing)
- Identity retention through transformation

**Space4X:**
- Crew → Cybernetic (augmentation)
- Ship → Corrupted (void exposure)
- Character → Psyker (awakening)
- Entity evolution/promotion

---

## Proposed Components

```csharp
// === Transformation Potential ===
public struct TransformationPotential : IComponentData
{
    public FixedString32Bytes EligibleType; // "FallenStarDemon", "AscendedAngel"
    public float TransformChance;           // 0-1 probability
    public byte RequiredPhysique;           // Stat requirements
    public byte RequiredWill;
    public byte RequiredAlignment;          // Chaotic, Lawful, etc.
    public bool MeetsRequirements;
}

// === Transformation Trigger ===
public struct TransformationTrigger : IComponentData
{
    public FixedString32Bytes TriggerType;  // "divine_throw", "corruption", "devotion"
    public Entity TriggeringEntity;         // God hand, corruption source
    public float TriggerMagnitude;          // Distance thrown, corruption level
    public uint TriggerTick;
    public bool RollSucceeded;
}

// === Transformation In Progress ===
public struct TransformationInProgress : IComponentData
{
    public FixedString32Bytes TargetForm;   // What they're becoming
    public float Progress;                   // 0-1
    public uint StartTick;
    public uint CompletionTick;              // When transformation finishes
    public bool IsDelayed;                   // Some transform after delay (meteor return)
}

// === Identity Retention ===
public struct RetainedIdentity : IComponentData
{
    public FixedString64Bytes OriginalName;
    public uint OriginalEntityId;
    public Entity OriginalVillage;
    public Entity OriginalFamily;
    public byte OriginalAlignment;
    public float RelationToTransformer;     // Positive = grateful, Negative = vengeful
}

// === Transformed Entity ===
public struct TransformedEntity : IComponentData
{
    public FixedString32Bytes OriginalType; // "Villager", "Crew"
    public FixedString32Bytes CurrentType;  // "Demon", "Angel"
    public uint TransformationTick;
    public Entity TransformationCause;      // Who/what caused this
    public bool RetainsMemories;
    public bool RetainsRelationships;
}

// === Memory Buffer (for retained identity) ===
public struct RetainedMemory : IBufferElementData
{
    public FixedString64Bytes MemoryType;   // "family_bond", "grudge", "loyalty"
    public Entity RelatedEntity;
    public float Intensity;                  // How strong the memory
    public bool IsPositive;
}

// === Configuration ===
public struct TransformationConfig : IComponentData
{
    public float BaseTransformChance;
    public float PhysiqueWeighting;          // How much physique affects chance
    public float WillWeighting;
    public uint MinDelayTicks;               // For delayed transformations
    public uint MaxDelayTicks;
    public bool AllowIdentityRetention;
    public float MemoryDecayRate;            // 0 = permanent memories
}
```

### New Systems
- `TransformationEligibilitySystem` - Evaluates who can transform
- `TransformationTriggerSystem` - Processes trigger events
- `TransformationProgressSystem` - Advances transformations
- `IdentityRetentionSystem` - Preserves memories/relationships
- `DelayedTransformationSystem` - Handles deferred completions

---

## Example Usage

```csharp
// === Villager thrown by god → potential demon ===
// 1. TransformationTriggerSystem detects divine_throw
var trigger = new TransformationTrigger {
    TriggerType = "divine_throw",
    TriggeringEntity = godHandEntity,
    TriggerMagnitude = 75f, // Distance thrown
    TriggerTick = currentTick
};

// 2. System checks TransformationPotential
// - Chaotic alignment? ✓
// - Warlike outlook? ✓
// - High faith in throwing deity? ✓
// - Roll succeeds? ✓

// 3. TransformationInProgress created
var inProgress = new TransformationInProgress {
    TargetForm = "FallenStarDemon",
    IsDelayed = true,
    CompletionTick = currentTick + OneYearInTicks
};

// 4. One year later: Meteor event spawns demon
// RetainedIdentity preserves:
// - Original name: "Marcus the Warrior"
// - Grudge against god who threw him
// - Memory of family left behind
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Transformation/` directory
- Integration: Entity spawning, identity systems

**Breaking Changes:** None - new system

---

## Review Notes

*(PureDOTS team use)*

