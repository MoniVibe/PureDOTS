# Extension Request: Espionage & Infiltration Utilities

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P1  
**Assigned To**: TBD

---

## Use Case

Both games need espionage and infiltration mechanics:

**Space4X:**
- Spy bands embed within rival factions, building infiltration levels (0-4)
- Counterintelligence detects and exposes enemy agents
- Exposure triggers diplomatic fallout, tribunals, or war
- Multiple infiltration methods: conscription, fame, hacking, blackmail

**Godgame:**
- Thieves infiltrating enemy settlements for sabotage/theft
- Rogues scouting enemy camps before raids
- Assassins building access to high-value targets
- Spies gathering intelligence on rival villages

Shared needs:
- Progressive infiltration level tracking
- Suspicion/exposure accumulation and decay
- Cover identity management
- Detection rolls against counterintelligence
- Extraction/exfiltration planning

---

## Proposed Solution

**Extension Type**: New Components + Static Helpers

### Components

```csharp
/// <summary>
/// Infiltration access tier.
/// </summary>
public enum InfiltrationLevel : byte
{
    None = 0,           // No access
    Contact = 1,        // Initial contact, public data
    Embedded = 2,       // Inside organization, local intel
    Trusted = 3,        // Trusted member, intercept comms
    Influential = 4,    // Key position, steal secrets
    Subverted = 5       // Command access, issue false orders
}

/// <summary>
/// Method used for infiltration.
/// </summary>
public enum InfiltrationMethod : byte
{
    None = 0,
    Conscription = 1,   // Join military/workforce
    Celebrity = 2,      // Fame/popularity as cover
    Hacking = 3,        // Digital infiltration
    Blackmail = 4,      // Coerce cooperation
    Cultural = 5,       // Adopt rival customs
    Bribery = 6,        // Pay for access
    Seduction = 7,      // Romance-based access
    Forgery = 8         // Fake credentials
}

/// <summary>
/// Current infiltration state for an agent.
/// </summary>
public struct InfiltrationState : IComponentData
{
    public Entity TargetEntity;        // Organization being infiltrated
    public InfiltrationLevel Level;
    public InfiltrationMethod Method;
    public float Progress;             // 0-1 progress to next level
    public float Suspicion;            // 0-1 how suspicious target is
    public float CoverStrength;        // 0-1 quality of cover identity
    public uint InfiltrationStartTick;
    public uint LastActivityTick;
    public byte IsExposed;             // Cover blown
    public byte IsExtracting;          // Currently escaping
}

/// <summary>
/// Counterintelligence capabilities of an organization.
/// </summary>
public struct CounterIntelligence : IComponentData
{
    public float DetectionRate;        // Base detection chance per tick
    public float SuspicionGrowth;      // How fast suspicion builds
    public float SuspicionDecay;       // Natural suspicion decay
    public float InvestigationPower;   // Effectiveness of active hunts
    public byte SecurityLevel;         // 0-10 overall security tier
    public uint LastSweepTick;
}

/// <summary>
/// Cover identity for an infiltrating agent.
/// </summary>
public struct CoverIdentity : IComponentData
{
    public FixedString64Bytes CoverName;
    public FixedString32Bytes CoverRole;
    public float Authenticity;         // How believable (0-1)
    public float Depth;                // How detailed the backstory (0-1)
    public uint CreatedTick;
    public uint LastVerifiedTick;
    public byte HasDocuments;          // Forged credentials
    public byte HasContacts;           // Supporting network
}

/// <summary>
/// Extraction plan for when cover is blown.
/// </summary>
public struct ExtractionPlan : IComponentData
{
    public Entity SafeHouseEntity;     // Where to flee
    public Entity ExfilContactEntity;  // Who helps extract
    public float3 ExfilPosition;       // Backup position
    public float SuccessChance;        // Calculated extraction odds
    public byte PlanQuality;           // 0-10 how well planned
    public byte IsActivated;           // Currently executing
}

/// <summary>
/// Intelligence gathered through infiltration.
/// </summary>
[InternalBufferCapacity(8)]
public struct GatheredIntel : IBufferElementData
{
    public FixedString64Bytes IntelType;
    public Entity SourceEntity;
    public InfiltrationLevel RequiredLevel;
    public float Value;                // How valuable (affects rewards)
    public uint GatheredTick;
    public byte IsVerified;
    public byte IsStale;               // Too old to be useful
}

/// <summary>
/// Active counterintel investigation.
/// </summary>
public struct Investigation : IComponentData
{
    public Entity SuspectEntity;       // Who is being investigated
    public float Progress;             // 0-1 investigation completion
    public float Evidence;             // 0-1 evidence gathered
    public uint StartTick;
    public byte IsActive;
}
```

### Static Helpers

```csharp
public static class InfiltrationHelpers
{
    /// <summary>
    /// Calculates infiltration progress rate.
    /// </summary>
    public static float CalculateProgressRate(
        InfiltrationLevel currentLevel,
        InfiltrationMethod method,
        float coverStrength,
        float targetSecurityLevel)
    {
        // Higher levels progress slower
        float levelPenalty = 1f / (1 + (int)currentLevel);
        
        // Method effectiveness varies
        float methodBonus = GetMethodEffectiveness(method, targetSecurityLevel);
        
        // Good cover helps
        float coverBonus = 0.5f + coverStrength * 0.5f;
        
        // Security slows progress
        float securityPenalty = 1f - (targetSecurityLevel * 0.08f);
        
        return levelPenalty * methodBonus * coverBonus * securityPenalty;
    }

    /// <summary>
    /// Gets method effectiveness against security level.
    /// </summary>
    public static float GetMethodEffectiveness(
        InfiltrationMethod method,
        float securityLevel)
    {
        // Different methods work better in different situations
        return method switch
        {
            InfiltrationMethod.Hacking => securityLevel < 5 ? 1.2f : 0.6f,
            InfiltrationMethod.Cultural => 1.0f, // Consistent
            InfiltrationMethod.Blackmail => 0.8f + securityLevel * 0.05f, // Better vs paranoid
            InfiltrationMethod.Bribery => securityLevel < 3 ? 1.3f : 0.5f,
            InfiltrationMethod.Celebrity => 1.1f,
            InfiltrationMethod.Conscription => 0.9f,
            _ => 1.0f
        };
    }

    /// <summary>
    /// Calculates suspicion gain from activity.
    /// </summary>
    public static float CalculateSuspicionGain(
        float activityRisk,
        float coverStrength,
        float counterIntelPower,
        float currentSuspicion)
    {
        // Riskier activities generate more suspicion
        float baseSuspicion = activityRisk * 0.1f;
        
        // Good cover reduces suspicion gain
        float coverReduction = coverStrength * 0.7f;
        
        // Counterintel amplifies suspicion growth
        float counterIntelBonus = 1f + counterIntelPower * 0.5f;
        
        // Already suspicious = more scrutiny
        float scrutinyBonus = 1f + currentSuspicion * 0.3f;
        
        return math.max(0, baseSuspicion * (1f - coverReduction) * counterIntelBonus * scrutinyBonus);
    }

    /// <summary>
    /// Calculates natural suspicion decay.
    /// </summary>
    public static float CalculateSuspicionDecay(
        float currentSuspicion,
        float timeSinceActivity,
        float coverStrength,
        float baseDecayRate)
    {
        // Suspicion decays over time when agent is inactive
        float timeDecay = baseDecayRate * timeSinceActivity * 0.01f;
        
        // Good cover accelerates decay
        float coverBonus = 1f + coverStrength * 0.5f;
        
        return currentSuspicion * timeDecay * coverBonus;
    }

    /// <summary>
    /// Performs detection check against agent.
    /// </summary>
    public static bool PerformDetectionCheck(
        float suspicion,
        float coverStrength,
        float detectionRate,
        float investigationPower,
        uint seed)
    {
        Random rng = new Random(seed);
        float roll = rng.NextFloat(0, 1);
        
        // Base detection from suspicion
        float detectionChance = suspicion * detectionRate;
        
        // Cover provides protection
        float coverProtection = coverStrength * 0.6f;
        
        // Active investigation boosts detection
        float investigationBonus = investigationPower * 0.3f;
        
        float finalChance = math.saturate(detectionChance * (1f - coverProtection) + investigationBonus);
        
        return roll < finalChance;
    }

    /// <summary>
    /// Calculates extraction success chance.
    /// </summary>
    public static float CalculateExtractionChance(
        in ExtractionPlan plan,
        float suspicionLevel,
        float counterIntelPower,
        bool hasActiveInvestigation)
    {
        // Base from plan quality
        float baseChance = plan.PlanQuality * 0.08f + 0.2f;
        
        // Suspicion makes extraction harder
        float suspicionPenalty = suspicionLevel * 0.4f;
        
        // Counter-intel blocks escape routes
        float counterIntelPenalty = counterIntelPower * 0.2f;
        
        // Active investigation is very dangerous
        float investigationPenalty = hasActiveInvestigation ? 0.3f : 0;
        
        // Contacts help
        float contactBonus = plan.ExfilContactEntity != Entity.Null ? 0.15f : 0;
        
        return math.saturate(baseChance - suspicionPenalty - counterIntelPenalty - investigationPenalty + contactBonus);
    }

    /// <summary>
    /// Calculates intel value based on level and freshness.
    /// </summary>
    public static float CalculateIntelValue(
        InfiltrationLevel gatheredAt,
        uint currentTick,
        uint gatheredTick,
        float stalenessFactor)
    {
        // Higher level intel is more valuable
        float levelValue = (int)gatheredAt * 0.2f;
        
        // Intel goes stale over time
        uint age = currentTick - gatheredTick;
        float freshness = math.exp(-age * stalenessFactor);
        
        return levelValue * freshness;
    }

    /// <summary>
    /// Checks if agent should level up infiltration.
    /// </summary>
    public static bool ShouldLevelUp(
        in InfiltrationState state,
        float progressThreshold)
    {
        return state.Progress >= progressThreshold && 
               state.Level < InfiltrationLevel.Subverted &&
               state.IsExposed == 0;
    }

    /// <summary>
    /// Gets intel types available at each infiltration level.
    /// </summary>
    public static int GetAvailableIntelTypes(InfiltrationLevel level)
    {
        return level switch
        {
            InfiltrationLevel.Contact => 1,      // Public data only
            InfiltrationLevel.Embedded => 3,     // + local intel
            InfiltrationLevel.Trusted => 6,      // + communications
            InfiltrationLevel.Influential => 10, // + secrets
            InfiltrationLevel.Subverted => 15,   // + command intel
            _ => 0
        };
    }

    /// <summary>
    /// Calculates cover identity degradation.
    /// </summary>
    public static float CalculateCoverDegradation(
        in CoverIdentity cover,
        uint currentTick,
        float suspicion,
        bool hasBeenQuestioned)
    {
        // Cover degrades over time
        uint age = currentTick - cover.CreatedTick;
        float ageDegradation = age * 0.00001f;
        
        // Suspicion accelerates degradation
        float suspicionDegradation = suspicion * 0.1f;
        
        // Questioning damages cover
        float questioningDamage = hasBeenQuestioned ? 0.1f : 0;
        
        return ageDegradation + suspicionDegradation + questioningDamage;
    }
}
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Runtime/AI/InfiltrationComponents.cs`
- New: `Packages/com.moni.puredots/Runtime/Runtime/AI/InfiltrationHelpers.cs`

**Breaking Changes:** None - entirely new feature

---

## Example Usage

```csharp
// === Space4X: Spy band infiltrating rival faction ===
var infiltration = new InfiltrationState
{
    TargetEntity = rivalFactionEntity,
    Level = InfiltrationLevel.Embedded,
    Method = InfiltrationMethod.Conscription,
    Progress = 0.6f,
    Suspicion = 0.2f,
    CoverStrength = 0.8f
};

var counterIntel = EntityManager.GetComponentData<CounterIntelligence>(rivalFactionEntity);

// Calculate progress this tick
float progressRate = InfiltrationHelpers.CalculateProgressRate(
    infiltration.Level,
    infiltration.Method,
    infiltration.CoverStrength,
    counterIntel.SecurityLevel);

infiltration.Progress += progressRate * deltaTime;

// Check for level up
if (InfiltrationHelpers.ShouldLevelUp(infiltration, 1.0f))
{
    infiltration.Level++;
    infiltration.Progress = 0;
}

// Perform risky activity - suspicion gain
float suspicionGain = InfiltrationHelpers.CalculateSuspicionGain(
    0.5f, // activity risk
    infiltration.CoverStrength,
    counterIntel.InvestigationPower,
    infiltration.Suspicion);

infiltration.Suspicion = math.saturate(infiltration.Suspicion + suspicionGain);

// Detection check
bool detected = InfiltrationHelpers.PerformDetectionCheck(
    infiltration.Suspicion,
    infiltration.CoverStrength,
    counterIntel.DetectionRate,
    counterIntel.InvestigationPower,
    (uint)currentTick);

if (detected)
{
    infiltration.IsExposed = 1;
    TriggerExtractionPlan(spyEntity);
}

// === Godgame: Thief infiltrating enemy settlement ===
var thiefInfiltration = new InfiltrationState
{
    TargetEntity = enemyVillageEntity,
    Level = InfiltrationLevel.Contact,
    Method = InfiltrationMethod.Cultural,  // Posing as traveler
    Progress = 0.3f,
    Suspicion = 0.1f,
    CoverStrength = 0.6f
};

// Thief sneaks to treasure room - high risk activity
float stealRisk = 0.8f;
float suspicion = InfiltrationHelpers.CalculateSuspicionGain(
    stealRisk,
    thiefInfiltration.CoverStrength,
    0.3f, // village watch effectiveness
    thiefInfiltration.Suspicion);

// Calculate extraction if caught
var extractionPlan = EntityManager.GetComponentData<ExtractionPlan>(thiefEntity);
float escapeChance = InfiltrationHelpers.CalculateExtractionChance(
    extractionPlan,
    thiefInfiltration.Suspicion,
    0.3f,
    false);
```

---

## Alternative Approaches Considered

- **Alternative 1**: Simple binary detection
  - **Rejected**: Both games need nuanced infiltration with progressive levels

- **Alternative 2**: Game-specific systems
  - **Rejected**: Core mechanics (suspicion, cover, detection) are identical

---

## Implementation Notes

**Dependencies:**
- Random for detection rolls
- Entity references for targets and contacts

**Performance Considerations:**
- All calculations are simple math, burst-compatible
- Detection checks can be batched per organization

**Related Requests:**
- Stealth/perception system (stealth vs infiltration overlap)
- Grudge system (exposure creates grudges)

---

## Review Notes

*(PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

