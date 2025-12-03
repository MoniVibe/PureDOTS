# Extension Request: Succession & Inheritance System

**Status**: `[COMPLETED]`  
**Submitted**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Both games need succession and inheritance mechanics:

**Space4X:**
- Officers have heirs who inherit partial expertise (50%)
- Dynasties appoint successors for leadership
- Asset inheritance when captains die
- Grudges can be inherited across generations

**Godgame:**
- Family property passes to children
- Guild leadership succession
- Knowledge/skills partially inherited through teaching
- Family feuds span generations

Shared needs:
- Heir designation and succession pools
- Partial expertise inheritance
- Asset inheritance routing
- Succession crisis triggers
- Legacy/chronicle tracking

---

## Proposed Solution

**Extension Type**: New Components + Static Helpers

### Components

```csharp
/// <summary>
/// Type of succession.
/// </summary>
public enum SuccessionType : byte
{
    Primogeniture = 0,    // Eldest child
    Ultimogeniture = 1,   // Youngest child
    Seniority = 2,        // Oldest in family
    Elective = 3,         // Voted by members
    Meritocratic = 4,     // Best qualified
    Designated = 5,       // Specifically chosen
    Random = 6            // Random among heirs
}

/// <summary>
/// Succession rules for an entity.
/// </summary>
public struct SuccessionRules : IComponentData
{
    public SuccessionType Type;
    public byte AllowFemaleHeirs;
    public byte AllowAdoption;
    public byte RequiresBloodline;
    public float MinAge;               // Minimum age to inherit
    public float MinExpertise;         // Minimum expertise tier
    public byte ExpertiseCategory;     // Required expertise type
}

/// <summary>
/// Heir candidate entry.
/// </summary>
[InternalBufferCapacity(8)]
public struct HeirCandidate : IBufferElementData
{
    public Entity CandidateEntity;
    public byte Priority;              // Lower = higher priority
    public float Claim;                // 0-1 strength of claim
    public float Suitability;          // 0-1 how qualified
    public byte IsDesignated;          // Explicitly named heir
    public byte IsBloodline;           // Blood relation
    public uint AddedTick;
}

/// <summary>
/// Legacy that can be inherited.
/// </summary>
public struct Legacy : IComponentData
{
    public Entity OriginatorEntity;    // Who created this legacy
    public FixedString64Bytes LegacyType;
    public float Value;                // Importance/weight
    public float Integrity;            // 0-1 how intact
    public uint CreatedTick;
    public uint LastUpdatedTick;
}

/// <summary>
/// Inheritance package when entity dies.
/// </summary>
[InternalBufferCapacity(16)]
public struct InheritanceItem : IBufferElementData
{
    public FixedString32Bytes ItemType; // "asset", "expertise", "title", "grudge"
    public Entity ItemEntity;          // If entity reference
    public float Value;                // Amount/percentage
    public float TransferEfficiency;   // How much transfers (0-1)
    public byte RequiresAcceptance;    // Heir can refuse
}

/// <summary>
/// Pending succession event.
/// </summary>
public struct SuccessionEvent : IComponentData
{
    public Entity DeceasedEntity;
    public Entity SuccessorEntity;
    public SuccessionType TypeUsed;
    public uint OccurredTick;
    public uint ResolvedTick;
    public byte WasContested;
    public byte WasSuccessful;
}

/// <summary>
/// Succession crisis state.
/// </summary>
public struct SuccessionCrisis : IComponentData
{
    public Entity SubjectEntity;       // What's being contested
    public byte ClaimantCount;
    public float Intensity;            // 0-1 severity
    public uint StartTick;
    public uint DeadlineTick;          // Must resolve by
    public byte RequiresVote;
    public byte RequiresCombat;
}

/// <summary>
/// Chronicle entry for legacy tracking.
/// </summary>
[InternalBufferCapacity(16)]
public struct ChronicleEntry : IBufferElementData
{
    public FixedString64Bytes EventType;
    public Entity RelatedEntity;
    public float Significance;         // 0-1 how important
    public uint OccurredTick;
}
```

### Static Helpers

```csharp
public static class SuccessionHelpers
{
    /// <summary>
    /// Selects heir based on succession type.
    /// </summary>
    public static Entity SelectHeir(
        in SuccessionRules rules,
        in DynamicBuffer<HeirCandidate> candidates,
        uint seed)
    {
        if (candidates.Length == 0)
            return Entity.Null;

        return rules.Type switch
        {
            SuccessionType.Primogeniture => SelectByPriority(candidates, ascending: true),
            SuccessionType.Ultimogeniture => SelectByPriority(candidates, ascending: false),
            SuccessionType.Designated => SelectDesignated(candidates),
            SuccessionType.Meritocratic => SelectBySuitability(candidates),
            SuccessionType.Random => SelectRandom(candidates, seed),
            SuccessionType.Elective => Entity.Null, // Requires voting system
            SuccessionType.Seniority => SelectByPriority(candidates, ascending: true),
            _ => Entity.Null
        };
    }

    private static Entity SelectByPriority(
        in DynamicBuffer<HeirCandidate> candidates,
        bool ascending)
    {
        Entity best = Entity.Null;
        byte bestPriority = ascending ? byte.MaxValue : byte.MinValue;
        
        for (int i = 0; i < candidates.Length; i++)
        {
            if (ascending && candidates[i].Priority < bestPriority)
            {
                bestPriority = candidates[i].Priority;
                best = candidates[i].CandidateEntity;
            }
            else if (!ascending && candidates[i].Priority > bestPriority)
            {
                bestPriority = candidates[i].Priority;
                best = candidates[i].CandidateEntity;
            }
        }
        return best;
    }

    private static Entity SelectDesignated(
        in DynamicBuffer<HeirCandidate> candidates)
    {
        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i].IsDesignated != 0)
                return candidates[i].CandidateEntity;
        }
        return candidates.Length > 0 ? candidates[0].CandidateEntity : Entity.Null;
    }

    private static Entity SelectBySuitability(
        in DynamicBuffer<HeirCandidate> candidates)
    {
        Entity best = Entity.Null;
        float bestSuitability = -1;
        
        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i].Suitability > bestSuitability)
            {
                bestSuitability = candidates[i].Suitability;
                best = candidates[i].CandidateEntity;
            }
        }
        return best;
    }

    private static Entity SelectRandom(
        in DynamicBuffer<HeirCandidate> candidates,
        uint seed)
    {
        if (candidates.Length == 0) return Entity.Null;
        var rng = new Random(seed);
        int index = rng.NextInt(0, candidates.Length);
        return candidates[index].CandidateEntity;
    }

    /// <summary>
    /// Calculates expertise inheritance amount.
    /// </summary>
    public static float CalculateExpertiseInheritance(
        float deceasedExpertise,
        float heirExpertise,
        float inheritanceRate,
        float bloodlineBonus)
    {
        // Base inheritance
        float inherited = deceasedExpertise * inheritanceRate;
        
        // Bloodline bonus
        inherited *= 1f + bloodlineBonus;
        
        // Diminishing returns if heir already skilled
        float diminishing = 1f / (1f + heirExpertise * 0.1f);
        inherited *= diminishing;
        
        return inherited;
    }

    /// <summary>
    /// Calculates asset inheritance with taxes/fees.
    /// </summary>
    public static float CalculateAssetInheritance(
        float assetValue,
        float inheritanceTaxRate,
        float claimStrength)
    {
        // Tax reduces inheritance
        float afterTax = assetValue * (1f - inheritanceTaxRate);
        
        // Weak claims lose more
        float claimFactor = 0.5f + claimStrength * 0.5f;
        
        return afterTax * claimFactor;
    }

    /// <summary>
    /// Checks if succession crisis should trigger.
    /// </summary>
    public static bool ShouldTriggerCrisis(
        in DynamicBuffer<HeirCandidate> candidates,
        float claimDisputeThreshold)
    {
        if (candidates.Length <= 1)
            return false;
        
        // Check for competing strong claims
        int strongClaims = 0;
        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i].Claim >= claimDisputeThreshold)
                strongClaims++;
        }
        
        return strongClaims > 1;
    }

    /// <summary>
    /// Calculates succession crisis intensity.
    /// </summary>
    public static float CalculateCrisisIntensity(
        in DynamicBuffer<HeirCandidate> candidates)
    {
        if (candidates.Length <= 1)
            return 0;
        
        // More claimants = more intense
        float countFactor = math.min(1f, candidates.Length * 0.2f);
        
        // Similar claims = more contested
        float claimVariance = 0;
        float avgClaim = 0;
        for (int i = 0; i < candidates.Length; i++)
            avgClaim += candidates[i].Claim;
        avgClaim /= candidates.Length;
        
        for (int i = 0; i < candidates.Length; i++)
        {
            float diff = candidates[i].Claim - avgClaim;
            claimVariance += diff * diff;
        }
        claimVariance /= candidates.Length;
        
        // Low variance = more intense (close race)
        float contestedFactor = 1f - math.sqrt(claimVariance);
        
        return countFactor * contestedFactor;
    }

    /// <summary>
    /// Adds chronicle entry for succession.
    /// </summary>
    public static void RecordSuccession(
        ref DynamicBuffer<ChronicleEntry> chronicle,
        Entity successorEntity,
        uint currentTick)
    {
        chronicle.Add(new ChronicleEntry
        {
            EventType = "succession",
            RelatedEntity = successorEntity,
            Significance = 0.8f,
            OccurredTick = currentTick
        });
    }

    /// <summary>
    /// Distributes inheritance items to heir.
    /// </summary>
    public static void ProcessInheritance(
        in DynamicBuffer<InheritanceItem> items,
        Entity heirEntity,
        out float totalValueInherited,
        out int itemsInherited)
    {
        totalValueInherited = 0;
        itemsInherited = 0;
        
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            float actualValue = item.Value * item.TransferEfficiency;
            totalValueInherited += actualValue;
            itemsInherited++;
        }
    }

    /// <summary>
    /// Calculates claim strength from relationships.
    /// </summary>
    public static float CalculateClaimStrength(
        bool isBloodline,
        byte generationalDistance,
        float legitimacy,
        bool isDesignated)
    {
        float claim = 0;
        
        // Blood relation is primary
        if (isBloodline)
        {
            claim = 1f / (1f + generationalDistance * 0.5f);
        }
        else
        {
            claim = 0.3f; // Non-blood claims are weaker
        }
        
        // Legitimacy affects claim
        claim *= legitimacy;
        
        // Designation is a strong boost
        if (isDesignated)
            claim = math.max(claim, 0.8f);
        
        return math.saturate(claim);
    }
}
```

---

## Example Usage

```csharp
// === Space4X: Admiral succession ===
var rules = new SuccessionRules
{
    Type = SuccessionType.Meritocratic,
    AllowFemaleHeirs = 1,
    AllowAdoption = 1,
    MinExpertise = 3, // Expert tier minimum
    ExpertiseCategory = (byte)ExpertiseCategory.Command
};

var candidates = EntityManager.GetBuffer<HeirCandidate>(dynastyEntity);

// Admiral dies - select successor
Entity successor = SuccessionHelpers.SelectHeir(rules, candidates, currentTick);

// Check for succession crisis
if (SuccessionHelpers.ShouldTriggerCrisis(candidates, 0.6f))
{
    var crisis = new SuccessionCrisis
    {
        SubjectEntity = dynastyEntity,
        ClaimantCount = (byte)candidates.Length,
        Intensity = SuccessionHelpers.CalculateCrisisIntensity(candidates),
        StartTick = currentTick,
        DeadlineTick = currentTick + 10000
    };
    EntityManager.AddComponentData(crisisEntity, crisis);
}

// Process inheritance
var inheritance = EntityManager.GetBuffer<InheritanceItem>(deceasedEntity);
SuccessionHelpers.ProcessInheritance(inheritance, successor, out float value, out int items);

// Transfer expertise
float admiralCommand = GetExpertise(deceasedEntity, ExpertiseCategory.Command);
float heirCommand = GetExpertise(successor, ExpertiseCategory.Command);
float inherited = SuccessionHelpers.CalculateExpertiseInheritance(
    admiralCommand, heirCommand, 0.5f, 0.1f);
AddExpertise(successor, ExpertiseCategory.Command, inherited);

// === Godgame: Family inheritance ===
var familyRules = new SuccessionRules
{
    Type = SuccessionType.Primogeniture,
    AllowFemaleHeirs = 1,
    RequiresBloodline = 1,
    MinAge = 16
};

var familyCandidates = EntityManager.GetBuffer<HeirCandidate>(familyEntity);

// Parent dies - property to eldest
Entity heir = SuccessionHelpers.SelectHeir(familyRules, familyCandidates, currentTick);

// Calculate with inheritance tax
float propertyValue = GetPropertyValue(deceasedEntity);
float inherited = SuccessionHelpers.CalculateAssetInheritance(
    propertyValue, 0.1f, // 10% tax
    familyCandidates[0].Claim);

TransferProperty(deceasedEntity, heir, inherited);

// Record in chronicle
var chronicle = EntityManager.GetBuffer<ChronicleEntry>(familyEntity);
SuccessionHelpers.RecordSuccession(ref chronicle, heir, currentTick);
```

---

## Alternative Approaches Considered

- **Alternative 1**: Simple "next in line" pointer
  - **Rejected**: Both games need contested succession, crisis mechanics

- **Alternative 2**: Game-specific inheritance
  - **Rejected**: Core mechanics (heirs, claims, transfer) are identical

---

## Implementation Notes

**Dependencies:**
- Entity references for family relationships
- Random for random succession

**Performance Considerations:**
- Succession resolution is rare (on death/retirement)
- Candidate buffers are small

**Related Requests:**
- Expertise tracking (inheritance of skills)
- Aggregate membership (group succession)
- Grudge system (inherited grudges)

---

## Review Notes

*(PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

