# Extension Request: Prestige & Reputation Scoring System

**Status**: `[COMPLETED]`  
**Submitted**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Both games need prestige and reputation tracking:

**Space4X:**
- Dynasties accumulate prestige from achievements
- Prestige unlocks exclusive ships, doctrines, tech
- Reputation affects diplomatic options
- Stress/strain threatens stability

**Godgame:**
- Village reputation affects trade and migration
- Individual renown affects leadership elections
- Guild reputation affects contract quality
- Negative reputation (infamy) has consequences

Shared needs:
- Prestige accumulation from achievements
- Reputation with different audiences
- Decay over time
- Prestige unlocking options
- Stress/strain mechanics

---

## Proposed Solution

**Extension Type**: New Components + Static Helpers

### Components

```csharp
/// <summary>
/// Type of reputation context.
/// </summary>
public enum ReputationType : byte
{
    General = 0,        // Overall reputation
    Military = 1,       // Combat prowess
    Economic = 2,       // Trade reliability
    Diplomatic = 3,     // Trustworthiness
    Scientific = 4,     // Research achievements
    Cultural = 5,       // Cultural influence
    Criminal = 6        // Infamy (negative)
}

/// <summary>
/// Prestige tier unlocks.
/// </summary>
public enum PrestigeTier : byte
{
    Unknown = 0,        // 0-99
    Known = 1,          // 100-499
    Notable = 2,        // 500-1999
    Renowned = 3,       // 2000-7999
    Famous = 4,         // 8000-24999
    Legendary = 5,      // 25000-99999
    Mythic = 6          // 100000+
}

/// <summary>
/// Main prestige tracking component.
/// </summary>
public struct Prestige : IComponentData
{
    public float CurrentPrestige;
    public float LifetimePrestige;     // Total ever earned
    public PrestigeTier Tier;
    public float PeakPrestige;         // Highest ever reached
    public uint LastGainTick;
    public uint LastDecayTick;
    public float DecayRate;            // Per tick decay
}

/// <summary>
/// Reputation with different audiences.
/// </summary>
[InternalBufferCapacity(8)]
public struct ReputationScore : IBufferElementData
{
    public Entity AudienceEntity;      // Who's opinion (null = global)
    public ReputationType Type;
    public float Score;                // -100 to +100
    public float Volatility;           // How fast it changes
    public uint LastUpdateTick;
}

/// <summary>
/// Stress/strain on prestige.
/// </summary>
public struct PrestigeStress : IComponentData
{
    public float CurrentStress;        // 0-1
    public float StressThreshold;      // When crisis triggers
    public float RecoveryRate;         // Natural stress reduction
    public uint LastStressEventTick;
    public byte InCrisis;
}

/// <summary>
/// Prestige unlock requirement.
/// </summary>
public struct PrestigeUnlock
{
    public FixedString64Bytes UnlockId;
    public PrestigeTier RequiredTier;
    public ReputationType RequiredRepType;
    public float RequiredRepScore;
    public byte RequiresBothPrestigeAndRep;
}

/// <summary>
/// Prestige event that modifies prestige/reputation.
/// </summary>
public struct PrestigeEvent : IComponentData
{
    public FixedString32Bytes EventType;
    public float PrestigeChange;
    public float ReputationChange;
    public ReputationType AffectedRepType;
    public Entity SourceEntity;
    public uint OccurredTick;
    public byte IsPositive;
}

/// <summary>
/// Notoriety tracking (negative reputation).
/// </summary>
public struct Notoriety : IComponentData
{
    public float InfamyLevel;          // 0-100
    public float HeatLevel;            // Active pursuit/attention
    public float BountyValue;          // Price on head
    public uint LastCrimesTick;
    public byte IsOutlaw;
}
```

### Static Helpers

```csharp
public static class PrestigeHelpers
{
    /// <summary>
    /// Gets prestige tier from value.
    /// </summary>
    public static PrestigeTier GetPrestigeTier(float prestige)
    {
        if (prestige >= 100000) return PrestigeTier.Mythic;
        if (prestige >= 25000) return PrestigeTier.Legendary;
        if (prestige >= 8000) return PrestigeTier.Famous;
        if (prestige >= 2000) return PrestigeTier.Renowned;
        if (prestige >= 500) return PrestigeTier.Notable;
        if (prestige >= 100) return PrestigeTier.Known;
        return PrestigeTier.Unknown;
    }

    /// <summary>
    /// Calculates prestige decay.
    /// </summary>
    public static float CalculateDecay(
        float currentPrestige,
        float decayRate,
        PrestigeTier tier,
        uint ticksSinceGain)
    {
        // Higher tiers decay slower
        float tierResist = 1f - (int)tier * 0.1f;
        
        // Recent gains protect against decay
        float recentGainProtection = math.exp(-ticksSinceGain * 0.0001f);
        
        float effectiveDecay = decayRate * tierResist * (1f - recentGainProtection);
        return currentPrestige * effectiveDecay;
    }

    /// <summary>
    /// Adds prestige with modifiers.
    /// </summary>
    public static float AddPrestige(
        ref Prestige prestige,
        float amount,
        float multiplier,
        uint currentTick)
    {
        float gained = amount * multiplier;
        prestige.CurrentPrestige += gained;
        prestige.LifetimePrestige += gained;
        prestige.LastGainTick = currentTick;
        
        if (prestige.CurrentPrestige > prestige.PeakPrestige)
            prestige.PeakPrestige = prestige.CurrentPrestige;
        
        prestige.Tier = GetPrestigeTier(prestige.CurrentPrestige);
        return gained;
    }

    /// <summary>
    /// Modifies reputation score.
    /// </summary>
    public static void ModifyReputation(
        ref DynamicBuffer<ReputationScore> scores,
        Entity audience,
        ReputationType type,
        float change,
        uint currentTick)
    {
        for (int i = 0; i < scores.Length; i++)
        {
            if (scores[i].AudienceEntity == audience && scores[i].Type == type)
            {
                var score = scores[i];
                score.Score = math.clamp(score.Score + change * score.Volatility, -100, 100);
                score.LastUpdateTick = currentTick;
                scores[i] = score;
                return;
            }
        }
        
        // Add new reputation entry
        scores.Add(new ReputationScore
        {
            AudienceEntity = audience,
            Type = type,
            Score = math.clamp(change, -100, 100),
            Volatility = 1f,
            LastUpdateTick = currentTick
        });
    }

    /// <summary>
    /// Gets reputation with specific audience.
    /// </summary>
    public static float GetReputation(
        in DynamicBuffer<ReputationScore> scores,
        Entity audience,
        ReputationType type)
    {
        for (int i = 0; i < scores.Length; i++)
        {
            if (scores[i].AudienceEntity == audience && scores[i].Type == type)
                return scores[i].Score;
        }
        return 0; // Neutral if unknown
    }

    /// <summary>
    /// Checks if unlock requirements are met.
    /// </summary>
    public static bool MeetsUnlockRequirements(
        in Prestige prestige,
        in DynamicBuffer<ReputationScore> scores,
        in PrestigeUnlock unlock)
    {
        bool meetsPrestige = prestige.Tier >= unlock.RequiredTier;
        
        bool meetsRep = true;
        if (unlock.RequiredRepScore > 0)
        {
            float repScore = GetReputation(scores, Entity.Null, unlock.RequiredRepType);
            meetsRep = repScore >= unlock.RequiredRepScore;
        }
        
        if (unlock.RequiresBothPrestigeAndRep != 0)
            return meetsPrestige && meetsRep;
        else
            return meetsPrestige || meetsRep;
    }

    /// <summary>
    /// Applies stress to prestige.
    /// </summary>
    public static void ApplyStress(
        ref PrestigeStress stress,
        float amount,
        uint currentTick)
    {
        stress.CurrentStress = math.saturate(stress.CurrentStress + amount);
        stress.LastStressEventTick = currentTick;
        
        if (stress.CurrentStress >= stress.StressThreshold)
            stress.InCrisis = 1;
    }

    /// <summary>
    /// Updates stress recovery.
    /// </summary>
    public static void UpdateStressRecovery(
        ref PrestigeStress stress,
        float deltaTime)
    {
        if (stress.InCrisis != 0) return; // No recovery during crisis
        
        float recovery = stress.RecoveryRate * deltaTime;
        stress.CurrentStress = math.max(0, stress.CurrentStress - recovery);
    }

    /// <summary>
    /// Calculates infamy from crimes.
    /// </summary>
    public static float CalculateInfamyGain(
        float crimeServerity,
        bool wasWitnessed,
        float victimImportance)
    {
        float baseInfamy = crimeServerity * 10f;
        
        // Witnesses spread word
        if (wasWitnessed)
            baseInfamy *= 2f;
        
        // Important victims = more infamy
        baseInfamy *= 1f + victimImportance;
        
        return baseInfamy;
    }

    /// <summary>
    /// Updates heat level decay.
    /// </summary>
    public static void UpdateHeatDecay(
        ref Notoriety notoriety,
        uint ticksSinceLastCrime,
        float decayRate)
    {
        float decay = ticksSinceLastCrime * decayRate;
        notoriety.HeatLevel = math.max(0, notoriety.HeatLevel - decay);
        
        // Infamy decays slower
        notoriety.InfamyLevel = math.max(0, notoriety.InfamyLevel - decay * 0.1f);
    }

    /// <summary>
    /// Calculates bounty based on infamy.
    /// </summary>
    public static float CalculateBounty(float infamyLevel, float baseBountyMultiplier)
    {
        if (infamyLevel < 10) return 0;
        return infamyLevel * baseBountyMultiplier;
    }

    /// <summary>
    /// Gets aggregate reputation across all audiences.
    /// </summary>
    public static float GetAverageReputation(
        in DynamicBuffer<ReputationScore> scores,
        ReputationType type)
    {
        float total = 0;
        int count = 0;
        
        for (int i = 0; i < scores.Length; i++)
        {
            if (scores[i].Type == type)
            {
                total += scores[i].Score;
                count++;
            }
        }
        
        return count > 0 ? total / count : 0;
    }
}
```

---

## Example Usage

```csharp
// === Space4X: Dynasty prestige tracking ===
var prestige = EntityManager.GetComponentData<Prestige>(dynastyEntity);
var scores = EntityManager.GetBuffer<ReputationScore>(dynastyEntity);
var stress = EntityManager.GetComponentData<PrestigeStress>(dynastyEntity);

// Dynasty wins major battle
PrestigeHelpers.AddPrestige(ref prestige, 500f, 1.2f, currentTick); // +600 prestige

// Update military reputation with all factions
foreach (var factionEntity in allFactions)
{
    PrestigeHelpers.ModifyReputation(ref scores, factionEntity, 
        ReputationType.Military, 20f, currentTick);
}

// Check for prestige unlock
var flagshipUnlock = new PrestigeUnlock
{
    UnlockId = "legendary_flagship",
    RequiredTier = PrestigeTier.Legendary,
    RequiredRepType = ReputationType.Military,
    RequiredRepScore = 50,
    RequiresBothPrestigeAndRep = 1
};

if (PrestigeHelpers.MeetsUnlockRequirements(prestige, scores, flagshipUnlock))
{
    GrantUnlock(dynastyEntity, "legendary_flagship");
}

// Dynasty loses major territory - stress
PrestigeHelpers.ApplyStress(ref stress, 0.3f, currentTick);

if (stress.InCrisis != 0)
{
    TriggerDynastyCrisis(dynastyEntity);
}

// === Godgame: Village reputation ===
var villagePrestige = EntityManager.GetComponentData<Prestige>(villageEntity);
var villageRep = EntityManager.GetBuffer<ReputationScore>(villageEntity);

// Village successfully trades - economic reputation boost
PrestigeHelpers.ModifyReputation(ref villageRep, tradePartnerEntity,
    ReputationType.Economic, 10f, currentTick);

// Get reputation for trade deal calculations
float tradingRep = PrestigeHelpers.GetAverageReputation(villageRep, ReputationType.Economic);
float tradeBonus = tradingRep * 0.01f; // +1% per point

// === Outlaw tracking ===
var notoriety = EntityManager.GetComponentData<Notoriety>(rogueEntity);

// Rogue commits crime
float infamyGain = PrestigeHelpers.CalculateInfamyGain(
    0.5f, // severity
    true, // witnessed
    0.3f); // victim importance

notoriety.InfamyLevel += infamyGain;
notoriety.HeatLevel += infamyGain * 2f;
notoriety.LastCrimesTick = currentTick;

// Update bounty
notoriety.BountyValue = PrestigeHelpers.CalculateBounty(notoriety.InfamyLevel, 10f);

if (notoriety.InfamyLevel > 50)
    notoriety.IsOutlaw = 1;

// Heat decays over time
PrestigeHelpers.UpdateHeatDecay(ref notoriety, ticksSinceLastCrime, 0.001f);
```

---

## Alternative Approaches Considered

- **Alternative 1**: Simple numeric score
  - **Rejected**: Need multi-audience reputation and tier unlocks

- **Alternative 2**: Game-specific prestige
  - **Rejected**: Core mechanics (tiers, decay, audiences) are identical

---

## Implementation Notes

**Dependencies:**
- Entity references for reputation audiences

**Performance Considerations:**
- Reputation buffers are small per entity
- Decay updates can be batched

**Related Requests:**
- Diplomacy system (reputation affects relations)
- Economy system (trade reputation)

---

## Review Notes

*(PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

