# Extension Request: Entity Relations System

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Both games need dynamic relationships between entities:
- **Space4X**: Crew relationships, faction standings, trade partner trust
- **Godgame**: Villager friendships, family bonds, grudges, mentor-student

Relations affect cooperation, combat morale, trade prices, diplomacy, and emergent storytelling.

---

## Proposed Solution

**Extension Type**: New Components + Systems

### Components (`Packages/com.moni.puredots/Runtime/Runtime/Social/`)

```csharp
public enum RelationType : byte
{
    None = 0,
    // Personal
    Stranger = 1, Acquaintance = 2, Friend = 3, CloseFriend = 4, BestFriend = 5,
    Rival = 10, Enemy = 11, Nemesis = 12,
    // Family
    Parent = 20, Child = 21, Sibling = 22, Spouse = 23, 
    // Professional
    Mentor = 30, Student = 31, Colleague = 32, Superior = 33, Subordinate = 34,
    // Faction
    Ally = 40, Neutral = 41, Hostile = 42, AtWar = 43
}

[InternalBufferCapacity(16)]
public struct EntityRelation : IBufferElementData
{
    public Entity OtherEntity;
    public RelationType Type;
    public sbyte Intensity;          // -100 (hatred) to +100 (love)
    public ushort InteractionCount;  // Times interacted
    public uint FirstMetTick;
    public uint LastInteractionTick;
    public byte Trust;               // 0-100 reliability score
    public byte Familiarity;         // 0-100 how well they know each other
}

public struct RelationConfig : IComponentData
{
    public float DecayRatePerDay;    // How fast unused relations fade
    public sbyte MinIntensity;       // Floor for decay
    public byte FamiliarityPerInteraction;
}
```

### Systems

```csharp
// EntityMeetingSystem - Detects first-time meetings, creates relations
// RelationDecaySystem - Fades unused relationships over time
// RelationCalculator - Static helpers for formulas
public static class RelationCalculator
{
    public static sbyte CalculateIntensityChange(RelationType type, InteractionOutcome outcome);
    public static RelationType DetermineRelationType(sbyte intensity, ushort interactions);
    public static float GetCooperationBonus(sbyte intensity);
    public static float GetTradePriceModifier(sbyte intensity, byte trust);
}
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Runtime/Social/EntityRelationComponents.cs`
- New: `Packages/com.moni.puredots/Runtime/Systems/Social/EntityMeetingSystem.cs`
- New: `Packages/com.moni.puredots/Runtime/Systems/Social/RelationDecaySystem.cs`

**Breaking Changes:** None - new feature

---

## Example Usage

```csharp
// Check relationship before trade
var relations = EntityManager.GetBuffer<EntityRelation>(merchantEntity);
var relation = RelationHelpers.FindRelation(relations, customerEntity);

float priceModifier = RelationCalculator.GetTradePriceModifier(relation.Intensity, relation.Trust);
float finalPrice = basePrice * priceModifier; // Friends get discounts

// Update relation after positive interaction
RelationHelpers.RecordInteraction(ref relation, InteractionOutcome.Positive);
```

---

## Reference Implementation

`Godgame/Assets/Scripts/Godgame/Relations/`
- `EntityRelationComponents.cs`
- `EntityMeetingSystem.cs`
- `RelationCalculator.cs`

---

## Review Notes

*(PureDOTS team use)*

