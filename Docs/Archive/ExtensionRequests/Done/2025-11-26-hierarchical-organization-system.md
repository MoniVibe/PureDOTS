# Extension Request: Hierarchical Organization System (Guilds/Factions)

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Completed**: 2025-11-26  
**Game Project**: Both (Godgame, Space4X)  
**Priority**: P2  

### Implementation
- `Packages/com.moni.puredots/Runtime/Runtime/Organization/OrganizationComponents.cs` - Organization, OrganizationMember, MembershipRecord, OrganizationPresence, OrganizationPolitics, InternalFaction, OrganizationRelation
- `Packages/com.moni.puredots/Runtime/Runtime/Organization/OrganizationHelpers.cs` - Static helpers for membership management, stability calculation, inter-org relations

---

## Use Case

Cross-entity organizations are needed for:

**Godgame:**
- Guilds (Heroes, Merchants, Assassins, Scholars)
- Holy Orders
- Rebel Factions
- Cross-village cooperation
- Internal guild politics

**Space4X:**
- Fleet hierarchies (Admiral → Captain → Crew)
- Trade federations
- Pirate confederacies
- Colonial administrations

---

## Proposed Components

```csharp
// === Organization Definition ===
public struct Organization : IComponentData
{
    public FixedString64Bytes Name;
    public FixedString32Bytes OrgType;       // "Guild", "Order", "Federation"
    public Entity HeadquartersEntity;        // Base of operations
    public uint FoundedTick;
    public float Wealth;                      // Org treasury
    public float Influence;                   // Political power
    public byte AlignmentTendency;           // Avg member alignment
}

// === Membership ===
public struct OrganizationMember : IBufferElementData
{
    public Entity MemberEntity;              // Villager, ship, etc.
    public FixedString32Bytes Rank;          // "Initiate", "Master", "Grandmaster"
    public byte RankLevel;                   // Numeric rank for sorting
    public float Standing;                    // Reputation within org
    public uint JoinedTick;
    public bool IsLeader;
}

public struct MembershipRecord : IComponentData
{
    public Entity OrganizationEntity;
    public FixedString32Bytes Rank;
    public byte RankLevel;
    public float Standing;
    public bool IsActive;
}

// === Embassy/Presence ===
public struct OrganizationPresence : IBufferElementData
{
    public Entity OrganizationEntity;
    public Entity LocationEntity;            // Village, station
    public FixedString32Bytes PresenceType;  // "Embassy", "Chapterhouse", "Office"
    public byte InfluenceLevel;              // 0-10
    public float LocalReputation;
}

// === Internal Politics ===
public struct OrganizationPolitics : IComponentData
{
    public Entity CurrentLeader;
    public FixedString32Bytes SuccessionType; // "Election", "Combat", "Seniority"
    public uint NextElectionTick;
    public float Stability;                   // 0-1, low = infighting
    public byte FactionCount;                 // Internal factions
}

public struct InternalFaction : IBufferElementData
{
    public FixedString32Bytes FactionName;
    public Entity FactionLeader;
    public float Support;                     // % of members
    public FixedString64Bytes Agenda;         // What they want
    public bool IsRuling;
}

// === Org Relations ===
public struct OrganizationRelation : IBufferElementData
{
    public Entity OtherOrganization;
    public float RelationScore;               // -100 to +100
    public FixedString32Bytes RelationType;   // "Alliance", "Rivalry", "War"
    public uint RelationChangedTick;
}

// === Directives (from expanded AI request) ===
public struct OrganizationDirective : IBufferElementData
{
    public FixedString32Bytes DirectiveType;  // "Respond to threat", "Establish trade"
    public Entity TargetEntity;               // What to act on
    public float Priority;
    public uint IssuedTick;
    public uint ExpiryTick;
}

// === Configuration ===
public struct OrganizationConfig : IComponentData
{
    public uint MinMembersToForm;
    public uint ElectionIntervalTicks;
    public float StabilityDecayRate;
    public float InfluenceGrowthRate;
    public bool AllowCrossTypeRelations;      // Guilds can ally with Orders
}
```

### New Systems
- `OrganizationFormationSystem` - Creates new orgs when conditions met
- `MembershipSystem` - Handles joining/leaving/promotion
- `OrganizationPoliticsSystem` - Internal elections, coups, factions
- `OrganizationRelationsSystem` - Inter-org diplomacy
- `OrganizationDirectiveSystem` - Issues orders to members
- `EmbassySystem` - Manages org presence in locations

---

## Example Usage

```csharp
// === Heroes' Guild forms ===
// Conditions: Village tech tier 8+, world threats active
var guild = new Organization {
    Name = "Order of the Silver Dawn",
    OrgType = "Guild",
    HeadquartersEntity = capitalVillage,
    AlignmentTendency = 60 // Lawful Good
};

// === Member joins ===
memberBuffer.Add(new OrganizationMember {
    MemberEntity = heroVillager,
    Rank = "Initiate",
    RankLevel = 1,
    Standing = 50f
});

// === Guild issues directive ===
directiveBuffer.Add(new OrganizationDirective {
    DirectiveType = "Hunt World Boss",
    TargetEntity = demonLordEntity,
    Priority = 100f
});

// === Internal politics ===
// After leader death, election triggered
// Two factions: "Purifiers" (aggressive) vs "Protectors" (defensive)
// Election determines guild strategy
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Organization/` directory
- Integration: Entity relations, AI directives

**Breaking Changes:** None - new system

---

## Review Notes

*(PureDOTS team use)*

