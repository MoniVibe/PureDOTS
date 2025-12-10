# Aggregate Politics System (Agnostic Framework)

## Overview
Defines mathematical frameworks for aggregate entity internal cohesion, external diplomatic relations, and lifecycle transformations (merger, split, vassalization). Game-agnostic algorithms enable faction dynamics, marriage alliances, ideological alignment, and governance-based decision-making.

**Integration**: Aggregate ECS (0.2 Hz) for group-level politics, Mind ECS (1 Hz) for individual member opinions.

---

## Core Components

### Aggregate Relation Component
```csharp
public struct AggregateRelationComponent : IComponentData
{
    public Entity TargetAggregate;
    public float RelationValue;        // -100 to +100
    public RelationType Type;          // Neutral, Allied, Vassal, Hostile, etc.
    public float TensionLevel;         // 0-100
    public float MonthsSinceInteraction;
}

public enum RelationType : byte
{
    BloodFeud = 0,        // -100 to -80
    Hostile = 1,          // -79 to -50
    Rival = 2,            // -49 to -20
    Neutral = 3,          // -19 to +19
    Friendly = 4,         // +20 to +49
    Allied = 5,           // +50 to +79
    Confederated = 6,     // +80 to +99
    Merged = 7,           // +100 (same entity now)
    Vassal = 8,           // Subordinate relationship
    Liege = 9,            // Superior relationship
    Protectorate = 10     // Guaranteed independence
}
```

### Aggregate Cohesion Component
```csharp
public struct AggregateCohesionComponent : IComponentData
{
    public float CohesionPercent;      // 0-100%
    public float IdeologicalAlignment; // 0-1 (how closely members agree)
    public float LeadershipQuality;    // 0-1 (leader effectiveness)
    public float MemberSatisfaction;   // 0-100 (average satisfaction)
    public float TensionLevel;         // 0-100 (accumulated grievances)
    public GovernanceType Governance;  // Affects decision speed and satisfaction
}

public enum GovernanceType : byte
{
    Egalitarian = 0,      // Democratic, council-based (slow decisions, high satisfaction)
    Authoritarian = 1,    // Dictatorial, single leader (fast decisions, variable satisfaction)
    Mixed = 2             // Hybrid (medium decisions, medium satisfaction)
}
```

### Member Opinion Component
```csharp
public struct AggregateMemberOpinionComponent : IComponentData
{
    public Entity MemberEntity;
    public Entity AggregateEntity;
    public float LeadershipOpinion;    // 0-100 (opinion of leader)
    public float EthicsOpinion;        // 0-100 (approval of group's actions)
    public float ResourceFairness;     // 0-100 (satisfaction with resource distribution)
    public float LoyaltyLevel;         // 0-100 (willingness to stay)
    public float DiplomaticWeight;     // 0-1 (voting power in decisions)
}
```

### Split Risk Component
```csharp
public struct AggregateSplitRiskComponent : IComponentData
{
    public float SplitProbability;     // 0-1
    public int FactionCount;           // Number of emerging factions
    public Entity DominantFaction;     // Largest faction (keeps original identity)
    public float MonthsBelowThreshold; // Months cohesion < 30%
    public bool SplitImminent;         // Split will occur next update
}
```

### Marriage Alliance Component
```csharp
public struct MarriageAllianceComponent : IComponentData
{
    public Entity SpouseEntity;
    public Entity SpouseAggregate;     // Partner's home aggregate
    public Entity OwnAggregate;        // This entity's home aggregate
    public float MarriageSatisfaction; // 0-100
    public int ChildrenCount;
    public bool ArrangedMarriage;      // Political vs romantic marriage
    public int MonthsMarried;
    public float RelationBonus;        // Relation boost between aggregates
}
```

---

## Agnostic Algorithms

### Cohesion Calculation

#### Base Cohesion Formula
```csharp
/// <summary>
/// Calculate aggregate internal cohesion
/// Agnostic: Loyalty + Ideological Alignment + Leadership Quality
/// </summary>
public static float CalculateCohesion(
    float averageMemberLoyalty,        // 0-100
    float ideologicalAlignment,        // 0-1 (how well members' ideals match)
    float leadershipQuality,           // 0-1 (leader's effectiveness)
    float memberSatisfaction,          // 0-100 (resource fairness, treatment)
    GovernanceType governance)
{
    // Normalize loyalty and satisfaction to 0-1
    float loyaltyFactor = averageMemberLoyalty / 100f;
    float satisfactionFactor = memberSatisfaction / 100f;

    // Governance bonus: Egalitarian increases satisfaction weight, Authoritarian increases leadership weight
    float governanceModifier = 1f;
    float satisfactionWeight = 0.3f;
    float leadershipWeight = 0.3f;

    switch (governance)
    {
        case GovernanceType.Egalitarian:
            satisfactionWeight = 0.4f;  // Members feel heard
            leadershipWeight = 0.2f;    // Leader less critical
            governanceModifier = 1.1f;  // Bonus for member empowerment
            break;
        case GovernanceType.Authoritarian:
            satisfactionWeight = 0.2f;  // Members' opinions matter less
            leadershipWeight = 0.4f;    // Leader is critical
            governanceModifier = 0.9f;  // Penalty for oppression (unless leader exceptional)
            break;
        case GovernanceType.Mixed:
            satisfactionWeight = 0.3f;
            leadershipWeight = 0.3f;
            governanceModifier = 1.0f;
            break;
    }

    // Cohesion calculation
    float baseCohesion = (loyaltyFactor * 0.4f) +
                         (ideologicalAlignment * 0.4f) +
                         (leadershipQuality * leadershipWeight) +
                         (satisfactionFactor * satisfactionWeight);

    float finalCohesion = baseCohesion * governanceModifier * 100f;

    return math.clamp(finalCohesion, 0f, 100f);
}
```

#### Ideological Alignment Calculation
```csharp
/// <summary>
/// Calculate how well members' ideologies align
/// Agnostic: Standard deviation of member ethics/values
/// </summary>
public static float CalculateIdeologicalAlignment(
    NativeArray<float> memberEthicsValues,  // Each member's ethics score (0-100)
    int memberCount)
{
    if (memberCount == 0) return 0f;

    // Calculate mean
    float sum = 0f;
    for (int i = 0; i < memberCount; i++)
    {
        sum += memberEthicsValues[i];
    }
    float mean = sum / memberCount;

    // Calculate standard deviation
    float varianceSum = 0f;
    for (int i = 0; i < memberCount; i++)
    {
        float deviation = memberEthicsValues[i] - mean;
        varianceSum += deviation * deviation;
    }
    float standardDeviation = math.sqrt(varianceSum / memberCount);

    // Convert std dev to alignment (0-1)
    // High std dev (divergent opinions) = low alignment
    // Low std dev (unified opinions) = high alignment
    // Assume max std dev = 50 (very divergent), min = 0 (perfect unity)
    float alignment = 1f - math.clamp(standardDeviation / 50f, 0f, 1f);

    return alignment;
}
```

#### Ideological Penalty for Conflicting Members
```csharp
/// <summary>
/// Calculate cohesion penalty when members oppose group's actions
/// Agnostic: Evil actions in good group, good actions in evil group
/// </summary>
public static float CalculateIdeologicalConflictPenalty(
    float memberEthicsValue,     // Member's ethics (-100 evil to +100 good)
    float groupActionEthics,     // Group's action ethics (-100 evil to +100 good)
    float memberInfluence)       // 0-1 (how much this member affects cohesion)
{
    // Calculate ethical distance
    float ethicsDistance = math.abs(memberEthicsValue - groupActionEthics);

    // If member and action align, no penalty
    if (ethicsDistance < 20f) return 0f;

    // Penalty increases with distance and member influence
    float basePenalty = ethicsDistance / 2f; // Max 100 distance = 50% penalty
    float influencedPenalty = basePenalty * memberInfluence;

    return math.clamp(influencedPenalty, 0f, 80f); // Max 80% penalty per member
}
```

### Tension and Satisfaction

#### Tension Accumulation
```csharp
/// <summary>
/// Calculate tension increase from events
/// Agnostic: Grievances accumulate over time
/// </summary>
public static float CalculateTensionIncrease(
    bool ideologicalConflict,          // Conflicting values
    bool unfairResourceDistribution,   // Resources distributed unfairly
    bool leadershipIncompetence,       // Leader failing duties
    bool externalPressure,             // War, famine, siege
    bool fundamentalMoralDisagreement) // Irreconcilable differences
{
    float tensionIncrease = 0f;

    if (ideologicalConflict) tensionIncrease += 5f;
    if (unfairResourceDistribution) tensionIncrease += 8f;
    if (leadershipIncompetence) tensionIncrease += 10f;
    if (externalPressure) tensionIncrease += 15f;
    if (fundamentalMoralDisagreement) tensionIncrease += 20f;

    return tensionIncrease; // Per month
}
```

#### Tension Reduction
```csharp
/// <summary>
/// Calculate tension reduction from positive actions
/// Agnostic: Addressing grievances, successful leadership
/// </summary>
public static float CalculateTensionReduction(
    bool grievancesAddressed,          // Leader addresses complaints
    bool leaderReplaced,               // Bad leader removed
    bool groupAchievement,             // Success (victory, profit, etc.)
    bool charismaticMediation,         // High-CHA leader mediates (CHA 80+)
    int monthsSinceCrisis)             // Time heals wounds
{
    float tensionReduction = 0f;

    if (grievancesAddressed) tensionReduction += 10f;
    if (leaderReplaced) tensionReduction += 25f; // Instant relief
    if (groupAchievement) tensionReduction += 15f; // Instant
    if (charismaticMediation) tensionReduction += 12f; // Per month

    // Time decay: 1% tension reduction per month
    float timeDecay = math.min(monthsSinceCrisis, 12f);
    tensionReduction += timeDecay;

    return tensionReduction;
}
```

### Member Loyalty Calculation

```csharp
/// <summary>
/// Calculate individual member's loyalty to aggregate
/// Agnostic: Satisfaction + Ideological match + Personal bonds
/// </summary>
public static float CalculateMemberLoyalty(
    float satisfactionWithGroup,       // 0-100 (resource fairness, treatment)
    float ideologicalMatch,            // 0-100 (how well member's values match group)
    int personalBondsCount,            // Number of friends/family in group
    int yearsInGroup,                  // Time invested in group
    bool hasAlternativeOptions)        // Can member easily leave and join another group?
{
    // Base loyalty from satisfaction and ideology
    float baseLoyalty = (satisfactionWithGroup + ideologicalMatch) / 2f;

    // Personal bonds bonus: +2% per bond, max +30%
    float bondsBonus = math.min(personalBondsCount * 2f, 30f);

    // Tenure bonus: +1% per year, max +20%
    float tenureBonus = math.min(yearsInGroup, 20f);

    // Alternatives penalty: -15% if easy to leave
    float alternativesPenalty = hasAlternativeOptions ? 15f : 0f;

    float finalLoyalty = baseLoyalty + bondsBonus + tenureBonus - alternativesPenalty;

    return math.clamp(finalLoyalty, 0f, 100f);
}
```

### Diplomatic Weight Distribution

#### Calculate Member Voting Power
```csharp
/// <summary>
/// Calculate member's diplomatic weight (voting power)
/// Agnostic: Governance type determines power distribution
/// </summary>
public static float CalculateDiplomaticWeight(
    GovernanceType governance,
    bool isLeader,
    int rankLevel,                     // 0 (lowest) to 10 (highest)
    int totalMembers,
    float memberSkillLevel)            // 0-100 (relevant skill/competence)
{
    switch (governance)
    {
        case GovernanceType.Egalitarian:
            // Democratic: Everyone gets equal vote (with small bonuses for skill/rank)
            float baseWeight = 1f / totalMembers;
            float skillBonus = (memberSkillLevel / 100f) * 0.2f * baseWeight; // +20% max
            float rankBonus = (rankLevel / 10f) * 0.3f * baseWeight;          // +30% max

            if (isLeader)
                return baseWeight * 1.5f; // Leader gets 50% more weight

            return baseWeight + skillBonus + rankBonus;

        case GovernanceType.Authoritarian:
            // Dictatorial: Leader controls 80%, others share 20%
            if (isLeader)
                return 0.8f;

            // Distribute remaining 20% based on rank
            float subordinatePool = 0.2f;
            float rankWeight = rankLevel / 10f;
            return subordinatePool * rankWeight / totalMembers;

        case GovernanceType.Mixed:
            // Hybrid: Council of high-rank members shares power
            if (isLeader)
                return 0.3f; // Leader has 30%

            if (rankLevel >= 7) // Senior council
            {
                float councilPool = 0.6f;
                int councilMembers = totalMembers / 3; // Assume top third
                return councilPool / councilMembers;
            }
            else // Junior members
            {
                float juniorPool = 0.1f;
                int juniorMembers = totalMembers * 2 / 3;
                return juniorPool / juniorMembers;
            }

        default:
            return 1f / totalMembers;
    }
}
```

#### Aggregate Decision Vote
```csharp
/// <summary>
/// Calculate whether aggregate decision passes based on member votes
/// Agnostic: Weighted voting based on diplomatic weights
/// </summary>
public static bool CalculateDecisionVoteResult(
    NativeArray<bool> memberVotes,         // Each member's vote (true = approve)
    NativeArray<float> memberWeights,      // Each member's diplomatic weight
    GovernanceType governance,
    int totalMembers)
{
    float approvalWeight = 0f;
    float totalWeight = 0f;

    for (int i = 0; i < totalMembers; i++)
    {
        totalWeight += memberWeights[i];
        if (memberVotes[i])
        {
            approvalWeight += memberWeights[i];
        }
    }

    float approvalPercent = approvalWeight / totalWeight;

    // Vote thresholds by governance type
    switch (governance)
    {
        case GovernanceType.Egalitarian:
            return approvalPercent >= 0.51f; // Simple majority

        case GovernanceType.Authoritarian:
            // If leader votes yes (weight 0.8), decision passes regardless of others
            return approvalPercent > 0.5f;

        case GovernanceType.Mixed:
            return approvalPercent >= 0.6f; // Supermajority

        default:
            return approvalPercent >= 0.51f;
    }
}
```

### Relation Value Changes

#### Modify Relation from Event
```csharp
/// <summary>
/// Calculate relation change from diplomatic event
/// Agnostic: Positive/negative interactions modify relations
/// </summary>
public static float CalculateRelationChange(
    InteractionType interaction,
    float currentRelation,             // -100 to +100
    float eventSignificance,           // 0-1 (how important the event)
    bool isReciprocal)                 // Does target aggregate also gain/lose relation?
{
    float baseChange = 0f;

    // Determine base change by interaction type
    switch (interaction)
    {
        case InteractionType.Trade_Success:
            baseChange = 5f;
            break;
        case InteractionType.Military_Alliance:
            baseChange = 15f;
            break;
        case InteractionType.Marriage_Alliance:
            baseChange = 12f;
            break;
        case InteractionType.Treaty_Signed:
            baseChange = 10f;
            break;
        case InteractionType.Gift_Given:
            baseChange = 8f;
            break;
        case InteractionType.Insult:
            baseChange = -10f;
            break;
        case InteractionType.Betrayal:
            baseChange = -40f;
            break;
        case InteractionType.Attack:
            baseChange = -50f;
            break;
        case InteractionType.Assassination:
            baseChange = -70f;
            break;
        case InteractionType.Treaty_Broken:
            baseChange = -60f;
            break;
    }

    // Scale by event significance
    float scaledChange = baseChange * eventSignificance;

    // Reciprocal bonus: If both parties benefit/suffer equally, increase magnitude
    if (isReciprocal)
        scaledChange *= 1.2f;

    // Diminishing returns: Harder to increase already high relations
    if (currentRelation > 70f && scaledChange > 0f)
        scaledChange *= 0.7f;
    else if (currentRelation < -70f && scaledChange < 0f)
        scaledChange *= 0.7f; // Harder to make Blood Feuds worse

    return scaledChange;
}

public enum InteractionType : byte
{
    Trade_Success,
    Military_Alliance,
    Marriage_Alliance,
    Treaty_Signed,
    Gift_Given,
    Shared_Enemy_Defeated,
    Insult,
    Betrayal,
    Attack,
    Assassination,
    Treaty_Broken,
    Territory_Seized
}
```

#### Relation Decay Over Time
```csharp
/// <summary>
/// Calculate relation decay from lack of interaction
/// Agnostic: Relations decay toward neutral if not maintained
/// </summary>
public static float CalculateRelationDecay(
    float currentRelation,
    int monthsSinceLastInteraction,
    RelationType relationType)
{
    // Decay rate depends on relation type
    float decayRate = 0f;

    switch (relationType)
    {
        case RelationType.Friendly:
        case RelationType.Allied:
            decayRate = 0.5f; // -0.5 per month without interaction
            break;
        case RelationType.Confederated:
            decayRate = 0.3f; // Slower decay for deep bonds
            break;
        case RelationType.Hostile:
        case RelationType.BloodFeud:
            decayRate = -0.2f; // Negative relations IMPROVE over time (forgiveness)
            break;
        case RelationType.Vassal:
        case RelationType.Liege:
            decayRate = 0.1f; // Formal relationships decay slowly
            break;
        case RelationType.Neutral:
            decayRate = 0f; // Neutral stays neutral
            break;
    }

    // Apply decay
    float decay = decayRate * monthsSinceLastInteraction;

    // Decay toward neutral (0)
    if (currentRelation > 0f)
        return -math.min(decay, currentRelation); // Don't overshoot 0
    else if (currentRelation < 0f)
        return -math.max(decay, currentRelation); // Don't overshoot 0

    return 0f;
}
```

### Split Mechanics

#### Calculate Split Probability
```csharp
/// <summary>
/// Calculate probability of aggregate splitting into factions
/// Agnostic: Low cohesion + high tension = split risk
/// </summary>
public static float CalculateSplitProbability(
    float cohesionPercent,             // 0-100
    float tensionLevel,                // 0-100
    float leadershipQuality,           // 0-1 (can leader hold group together?)
    float monthsBelowThreshold,        // Months cohesion < 30%
    GovernanceType governance)
{
    // Base split probability from low cohesion
    float cohesionFactor = (100f - cohesionPercent) / 100f; // 0-1 (higher when cohesion low)

    // Tension multiplier
    float tensionMultiplier = tensionLevel / 100f; // 0-1

    // Leadership can hold group together
    float leadershipPenalty = 1f - (leadershipQuality * 0.3f); // Max -30% split chance

    // Time factor: Longer below threshold, higher split risk
    float timeFactor = 1f + (monthsBelowThreshold / 12f); // +0.083 per month

    // Governance factor: Authoritarian can suppress splits longer, Egalitarian splits faster
    float governanceFactor = 1f;
    switch (governance)
    {
        case GovernanceType.Egalitarian:
            governanceFactor = 1.2f; // Easier to split (members have voice)
            break;
        case GovernanceType.Authoritarian:
            governanceFactor = 0.8f; // Harder to split (leader suppresses)
            break;
    }

    float splitProbability = cohesionFactor * tensionMultiplier * leadershipPenalty * timeFactor * governanceFactor;

    return math.clamp(splitProbability, 0f, 0.95f); // Max 95% (always small chance to avoid split)
}
```

#### Determine Faction Membership
```csharp
/// <summary>
/// Assign members to factions during split
/// Agnostic: Members cluster by opinion similarity
/// </summary>
public static int DetermineFactionMembership(
    NativeArray<float> memberOpinions,     // Each member's opinion on divisive issue (0-100)
    int memberCount,
    out NativeArray<int> factionAssignments, // Output: Which faction each member joins
    out int factionCount)
{
    // Simple k-means clustering (2-3 factions typical)
    // Faction 0: Lowest opinions (opposition)
    // Faction 1: Highest opinions (support)
    // Faction 2: Middle opinions (neutrals, if significant)

    // Find min, max, mean
    float min = float.MaxValue;
    float max = float.MinValue;
    float sum = 0f;

    for (int i = 0; i < memberCount; i++)
    {
        float opinion = memberOpinions[i];
        if (opinion < min) min = opinion;
        if (opinion > max) max = opinion;
        sum += opinion;
    }
    float mean = sum / memberCount;

    // Determine faction count
    float range = max - min;
    if (range < 30f)
    {
        factionCount = 1; // Not enough divergence, no split
        factionAssignments = new NativeArray<int>(memberCount, Allocator.Temp);
        return 1;
    }
    else if (range < 60f)
    {
        factionCount = 2; // Two factions
    }
    else
    {
        factionCount = 3; // Three factions (split, moderate, support)
    }

    factionAssignments = new NativeArray<int>(memberCount, Allocator.Temp);

    // Assign members to factions
    for (int i = 0; i < memberCount; i++)
    {
        float opinion = memberOpinions[i];

        if (factionCount == 2)
        {
            factionAssignments[i] = opinion < mean ? 0 : 1;
        }
        else if (factionCount == 3)
        {
            float threshold1 = min + (range / 3f);
            float threshold2 = min + (2f * range / 3f);

            if (opinion < threshold1)
                factionAssignments[i] = 0; // Opposition
            else if (opinion < threshold2)
                factionAssignments[i] = 1; // Moderates
            else
                factionAssignments[i] = 2; // Support
        }
    }

    return factionCount;
}
```

#### Calculate Post-Split Relations
```csharp
/// <summary>
/// Calculate initial relations between newly split factions
/// Agnostic: Fair split = neutral, hostile split = negative
/// </summary>
public static float CalculatePostSplitRelation(
    float finalCohesionBeforeSplit,    // 0-100 (how bad was the split?)
    bool violenceDuringSplit,          // Did factions fight?
    bool resourcesContestedFairly,     // Were resources divided fairly?
    float ideologicalDistance)         // 0-100 (how different are factions' values?)
{
    // Base relation from cohesion at split time
    // Low cohesion (<15%) = hostile split, higher cohesion (20-30%) = fair split
    float baseRelation = finalCohesionBeforeSplit - 20f; // Range: -20 to +10

    // Violence penalty
    if (violenceDuringSplit)
        baseRelation -= 30f;

    // Resource contestation penalty
    if (!resourcesContestedFairly)
        baseRelation -= 20f;

    // Ideological distance penalty
    float ideologyPenalty = ideologicalDistance / 2f; // Max -50
    baseRelation -= ideologyPenalty;

    return math.clamp(baseRelation, -80f, +20f);
}
```

### Merger Mechanics

#### Calculate Merger Feasibility
```csharp
/// <summary>
/// Calculate whether two aggregates can merge
/// Agnostic: High relation + compatible governance + member approval
/// </summary>
public static float CalculateMergerFeasibility(
    float relationValue,               // -100 to +100
    GovernanceType governance1,
    GovernanceType governance2,
    float ideologicalDistance,         // 0-100 (how different are values?)
    float resourceImbalance,           // 0-1 (wealth/power difference)
    int memberCount1,
    int memberCount2)
{
    // Relation requirement: Must be ≥ +95 for merger consideration
    if (relationValue < 95f)
        return 0f;

    // Governance compatibility
    float governanceCompatibility = 1f;
    if (governance1 != governance2)
        governanceCompatibility = 0.7f; // Penalty for different governance

    // Ideological compatibility
    float ideologyCompatibility = 1f - (ideologicalDistance / 100f);

    // Resource imbalance: Large power differences make mergers harder (fear of domination)
    float sizeRatio = (float)math.max(memberCount1, memberCount2) / math.min(memberCount1, memberCount2);
    float sizeImbalancePenalty = 1f;
    if (sizeRatio > 3f)
        sizeImbalancePenalty = 0.6f; // Large size difference = concerns about domination

    // Wealth imbalance penalty
    float wealthPenalty = 1f - (resourceImbalance * 0.4f); // Max -40%

    float feasibility = governanceCompatibility * ideologyCompatibility * sizeImbalancePenalty * wealthPenalty;

    return math.clamp(feasibility, 0f, 1f);
}
```

#### Post-Merger Cohesion
```csharp
/// <summary>
/// Calculate cohesion of newly merged aggregate
/// Agnostic: Initial cohesion penalty during integration period
/// </summary>
public static float CalculatePostMergerCohesion(
    float premergerCohesion1,          // 0-100
    float premergerCohesion2,          // 0-100
    float mergerFeasibility,           // 0-1
    int monthsSinceMerger,             // Integration time
    float leadershipQuality)           // 0-1 (new leadership effectiveness)
{
    // Average pre-merger cohesion
    float averagePremergerCohesion = (premergerCohesion1 + premergerCohesion2) / 2f;

    // Adjustment period penalty: -20% cohesion initially
    float adjustmentPenalty = 20f;

    // Feasibility bonus: High feasibility reduces penalty
    adjustmentPenalty *= (1f - mergerFeasibility * 0.5f); // Max -50% penalty reduction

    // Time recovery: +2% per month, recovers over 10 months
    float timeRecovery = math.min(monthsSinceMerger * 2f, 20f);

    // Leadership bonus: Good leaders integrate faster
    float leadershipBonus = leadershipQuality * 10f; // Max +10%

    float postMergerCohesion = averagePremergerCohesion - adjustmentPenalty + timeRecovery + leadershipBonus;

    return math.clamp(postMergerCohesion, 20f, 100f);
}
```

### Vassalization Mechanics

#### Calculate Vassalization Willingness
```csharp
/// <summary>
/// Calculate whether aggregate will accept vassalization
/// Agnostic: Weak + threatened = willing, strong + proud = refuse
/// </summary>
public static bool CalculateVassalizationAcceptance(
    float relationWithLiege,           // -100 to +100
    float powerRatio,                  // VassalPower / LiegePower (0-1)
    float externalThreatLevel,         // 0-100 (how threatened is potential vassal?)
    float sovereigntyValue,            // 0-100 (how much does aggregate value independence?)
    bool isConquered)                  // Forced vassalization (war)
{
    if (isConquered)
        return true; // No choice

    // Relation requirement: Must be ≥ +40 for voluntary vassalization
    if (relationWithLiege < 40f)
        return false;

    // Power ratio: Weaker aggregates more likely to vassalize
    float powerWillingness = 1f - powerRatio; // 0-1 (higher when weaker)

    // External threat: High threat increases willingness
    float threatWillingness = externalThreatLevel / 100f; // 0-1

    // Sovereignty value: High value decreases willingness
    float sovereigntyPenalty = sovereigntyValue / 100f; // 0-1

    // Calculate willingness score
    float willingnessScore = (powerWillingness * 0.4f) + (threatWillingness * 0.4f) - (sovereigntyPenalty * 0.6f);

    // Additional relation bonus
    float relationBonus = (relationWithLiege - 40f) / 60f; // 0-1 (scaled from +40 to +100)
    willingnessScore += relationBonus * 0.3f;

    return willingnessScore >= 0.5f; // 50% threshold
}
```

#### Vassal Loyalty Calculation
```csharp
/// <summary>
/// Calculate vassal's loyalty to liege
/// Agnostic: Fair treatment + protection = loyalty, exploitation = rebellion
/// </summary>
public static float CalculateVassalLoyalty(
    float relationWithLiege,           // -100 to +100
    bool liegeProtectsVassal,          // Has liege fulfilled protection obligation?
    float taxRate,                     // 0-1 (0% to 100% taxes)
    int monthsSinceLastAid,            // Months since liege helped vassal
    bool liegeInterferes)              // Does liege meddle in vassal's internal affairs?
{
    // Base loyalty from relation
    float baseLoyalty = (relationWithLiege + 100f) / 2f; // Convert -100 to +100 → 0 to 100

    // Protection bonus: +20% if liege protects, -30% if doesn't
    float protectionModifier = liegeProtectsVassal ? 20f : -30f;

    // Tax penalty: Higher taxes reduce loyalty
    float taxPenalty = taxRate * 40f; // Max -40% at 100% tax

    // Recency of aid: -1% per month without help (max -20%)
    float aidPenalty = math.min(monthsSinceLastAid, 20f);

    // Interference penalty: -15% if liege meddles
    float interferencePenalty = liegeInterferes ? 15f : 0f;

    float vassalLoyalty = baseLoyalty + protectionModifier - taxPenalty - aidPenalty - interferencePenalty;

    return math.clamp(vassalLoyalty, 0f, 100f);
}
```

#### Rebellion Risk
```csharp
/// <summary>
/// Calculate vassal rebellion probability
/// Agnostic: Low loyalty + opportunity = rebellion
/// </summary>
public static float CalculateRebellionRisk(
    float vassalLoyalty,               // 0-100
    float vassalMilitaryPower,         // 0-100 (vassal's strength)
    float liegeMilitaryPower,          // 0-100 (liege's strength)
    bool externalAllyAvailable,        // Does vassal have potential ally?
    bool liegeDistracted)              // Is liege at war or weakened?
{
    // Loyalty threshold: Rebellion possible if loyalty < 40
    if (vassalLoyalty >= 40f)
        return 0f;

    // Base rebellion risk from low loyalty
    float baseRisk = (40f - vassalLoyalty) / 40f; // 0-1 (higher when loyalty lower)

    // Power ratio: Can vassal win?
    float powerRatio = vassalMilitaryPower / math.max(liegeMilitaryPower, 1f);
    float powerConfidence = math.clamp(powerRatio, 0f, 1f);

    // External ally: +30% rebellion chance
    float allyBonus = externalAllyAvailable ? 0.3f : 0f;

    // Liege distracted: +20% rebellion chance
    float distractionBonus = liegeDistracted ? 0.2f : 0f;

    float rebellionRisk = baseRisk * powerConfidence + allyBonus + distractionBonus;

    return math.clamp(rebellionRisk, 0f, 0.95f);
}
```

### Marriage Alliance Mechanics

#### Calculate Marriage Benefit
```csharp
/// <summary>
/// Calculate relation increase from marriage alliance
/// Agnostic: Spouse quality + strategic value = benefit
/// </summary>
public static float CalculateMarriageRelationBonus(
    int spouseCharisma,                // 0-100
    int spouseIntelligence,            // 0-100
    bool hasDesirabletrait,            // Rare/valuable genetic trait
    float currentRelation,             // -100 to +100 (before marriage)
    bool isPoliticalMarriage)          // Arranged vs romantic marriage
{
    // Base bonus: +10 to +20 relation
    float baseBonus = 10f;

    // Spouse quality: Higher CHA/INT = more prestigious match
    float spouseQuality = (spouseCharisma + spouseIntelligence) / 200f; // 0-1
    float qualityBonus = spouseQuality * 10f; // Max +10

    // Desirable trait: +8 bonus (genetic breeding motivation)
    float traitBonus = hasDesirabletrait ? 8f : 0f;

    // Political marriage: Lower emotional bonus, higher strategic bonus
    float motivationModifier = isPoliticalMarriage ? 0.8f : 1.2f;

    // Diminishing returns: Harder to increase already high relations
    float diminishingFactor = 1f;
    if (currentRelation > 60f)
        diminishingFactor = 0.7f;

    float totalBonus = (baseBonus + qualityBonus + traitBonus) * motivationModifier * diminishingFactor;

    return math.clamp(totalBonus, 5f, 25f);
}
```

#### Marriage Satisfaction
```csharp
/// <summary>
/// Calculate marriage satisfaction (affects relation stability)
/// Agnostic: Compatibility + treatment + time
/// </summary>
public static float CalculateMarriageSatisfaction(
    int spouse1Charisma,
    int spouse2Charisma,
    bool personalitiesCompatible,     // Do spouses get along?
    bool arrangedMarriage,
    int monthsMarried,
    bool hasMutualChildren)            // Children increase bond
{
    // Base satisfaction: Arranged marriages start lower
    float baseSatisfaction = arrangedMarriage ? 40f : 70f;

    // Charisma compatibility
    float charismaMatch = 100f - math.abs(spouse1Charisma - spouse2Charisma);
    float charismaBonus = charismaMatch / 10f; // Max +10

    // Personality compatibility: +20% if compatible, -20% if not
    float personalityModifier = personalitiesCompatible ? 20f : -20f;

    // Time bonding: +1% per month for first 3 years (max +36%)
    float timeBonus = math.min(monthsMarried, 36f);

    // Children bonus: +15% if have children together
    float childrenBonus = hasMutualChildren ? 15f : 0f;

    float satisfaction = baseSatisfaction + charismaBonus + personalityModifier + timeBonus + childrenBonus;

    return math.clamp(satisfaction, 10f, 100f);
}
```

#### Trait Inheritance Probability
```csharp
/// <summary>
/// Calculate probability of child inheriting parent trait
/// Agnostic: Genetic traits have inheritance chance
/// </summary>
public static bool CalculateTraitInheritance(
    bool parent1HasTrait,
    bool parent2HasTrait,
    float traitRarity,                 // 0-1 (common to legendary)
    ref Random random)
{
    if (!parent1HasTrait && !parent2HasTrait)
        return false; // Can't inherit what parents don't have

    // Base inheritance: 40% if one parent has trait, 65% if both
    float baseChance = 0.4f;
    if (parent1HasTrait && parent2HasTrait)
        baseChance = 0.65f;

    // Rarity modifier: Rarer traits less likely to pass
    float rarityPenalty = traitRarity * 0.25f; // Max -25%

    float inheritanceChance = baseChance - rarityPenalty;

    return random.NextFloat() < inheritanceChance;
}
```

### Espionage Detection

#### Spy Detection Chance
```csharp
/// <summary>
/// Calculate chance to detect infiltrator in aggregate
/// Agnostic: Spy skill vs aggregate security
/// </summary>
public static float CalculateSpyDetectionChance(
    int spyDeceptionSkill,             // 0-100
    int spyCharisma,                   // 0-100
    float aggregateSecurityLevel,      // 0-100 (how vigilant is group?)
    int leaderIntelligence,            // 0-100 (leader's perception)
    bool spyPerformedEvilAct,          // Did spy maintain cover by participating in evil?
    bool spyRefusedEvilAct,            // Did spy break cover by refusing evil?
    int monthsInfiltrated)             // Time spent infiltrated
{
    // Base detection: Security vs Deception
    float baseDetection = (aggregateSecurityLevel - spyDeceptionSkill) / 200f; // -0.5 to +0.5

    // Charisma reduces suspicion
    float charismaBonus = spyCharisma / 200f; // Max -0.5

    // Leader intelligence: Smarter leaders detect spies better
    float leaderBonus = leaderIntelligence / 200f; // Max +0.5

    // Cover maintenance: Performing evil acts reduces detection
    float coverModifier = 0f;
    if (spyPerformedEvilAct)
        coverModifier = -0.05f; // -5% per month
    if (spyRefusedEvilAct)
        coverModifier = 0.15f; // +15% instant spike

    // Time factor: Longer infiltration = higher exposure risk (+0.5% per month)
    float timeRisk = monthsInfiltrated * 0.005f;

    float detectionChance = baseDetection - charismaBonus + leaderBonus + coverModifier + timeRisk;

    return math.clamp(detectionChance, 0.01f, 0.95f); // Min 1%, max 95% per month
}
```

#### Sabotage Impact on Cohesion
```csharp
/// <summary>
/// Calculate cohesion loss from saboteur's actions
/// Agnostic: Sabotage spreads rumors, forges documents, creates distrust
/// </summary>
public static float CalculateSabotageImpact(
    SabotageAction action,
    int saboteurIntelligence,          // 0-100 (affects sabotage quality)
    float aggregateCohesion,           // Current cohesion (0-100)
    bool sabotageDetected)             // If detected, impact reduced
{
    float cohesionLoss = 0f;

    switch (action)
    {
        case SabotageAction.SpreadRumors:
            cohesionLoss = 5f;
            break;
        case SabotageAction.ForgeDocuments:
            cohesionLoss = 15f; // If believed (false affairs, embezzlement)
            break;
        case SabotageAction.PoisonFood:
            cohesionLoss = 10f; // Blame falls on cook
            break;
        case SabotageAction.AssassinateLeader:
            cohesionLoss = 40f; // Succession crisis
            break;
        case SabotageAction.StealResources:
            cohesionLoss = 12f; // Blame internal theft
            break;
    }

    // Intelligence modifier: Smarter saboteurs more effective
    float intModifier = 0.5f + (saboteurIntelligence / 100f); // 0.5 to 1.5×
    cohesionLoss *= intModifier;

    // If sabotage detected, impact reduced by 50%
    if (sabotageDetected)
        cohesionLoss *= 0.5f;

    // High cohesion provides resistance
    float cohesionResistance = aggregateCohesion / 100f; // 0-1
    cohesionLoss *= (1f - cohesionResistance * 0.3f); // Max -30% impact

    return cohesionLoss;
}

public enum SabotageAction : byte
{
    SpreadRumors,
    ForgeDocuments,
    PoisonFood,
    AssassinateLeader,
    StealResources,
    InciteRivalry
}
```

---

## ECS System Architecture

### Aggregate ECS (0.2 Hz) - Politics Systems

**AggregateRelationUpdateSystem**:
- Updates relations between aggregates based on interactions
- Applies relation decay over time
- Determines relation type (Friendly, Allied, Hostile, etc.)

**AggregateCohesionCalculationSystem**:
- Calculates internal cohesion from member loyalty, ideology, leadership
- Accumulates tension from grievances
- Applies tension reduction from positive events

**AggregateLifecycleSystem**:
- Handles mergers (when relation ≥ +95 and vote passes)
- Handles splits (when cohesion < 30% for 3+ months)
- Handles vassalization and rebellion
- Handles confederation formation

**AggregateDiplomaticWeightSystem**:
- Calculates each member's voting power
- Determines decision outcomes from weighted votes
- Adjusts weights based on governance changes

**AggregateMarriageSystem**:
- Arranges marriages between high-relation aggregates
- Calculates marriage satisfaction over time
- Determines trait inheritance for children
- Applies relation bonuses from successful marriages

### Mind ECS (1 Hz) - Individual Political Opinion

**EntityOpinionUpdateSystem**:
- Updates individual entity opinions of leadership
- Calculates satisfaction with resource distribution
- Determines ideological match with group's actions

**EntityLoyaltySystem**:
- Calculates loyalty from satisfaction, ideology, bonds, tenure
- Determines if entity will leave during split
- Assigns entity to faction during split event

**EntityFactionAffinitySystem**:
- Determines which faction entity supports
- Calculates willingness to split from main group
- Updates faction loyalty over time

---

## Key Design Principles

1. **Cohesion Drives Stability**: Cohesion < 30% for 3+ months → split risk, cohesion > 70% → merger opportunity
2. **Ideological Alignment Matters**: Members with conflicting ethics (good in evil group) reduce cohesion significantly
3. **Governance Affects Democracy**: Egalitarian = slow decisions + high satisfaction, Authoritarian = fast decisions + variable satisfaction
4. **Relations Are Dynamic**: Positive feedback loops (trade → alliance → confederation → merger) and negative loops (insult → rivalry → hostility → Blood Feud)
5. **Diplomatic Weight Reflects Power**: Egalitarian groups distribute power equally, Authoritarian concentrates in leader
6. **Marriage Is Strategic**: Dynasties marry to strengthen alliances, breed superior traits, or secure peace treaties
7. **Lifecycle Is Bidirectional**: Mergers can split, vassals can rebel, alliances can fracture
8. **Espionage Subverts**: Spies destabilize aggregates but risk detection and moral stress
9. **Tension Accumulates**: Unaddressed grievances build tension, eventually causing splits
10. **Time Heals (Sometimes)**: Relations decay toward neutral without interaction, but Blood Feuds heal very slowly

---

## Integration with Other Systems

- **Soul System**: Transferred souls retain loyalties to original aggregates, creating infiltration/espionage opportunities
- **Blueprint System**: Guild mergers pool design libraries and production capabilities
- **Infiltration Detection**: Detecting spies within aggregates uses Mind ECS investigation checks
- **Crisis Alert States**: External threats modify cohesion (rally effect +15% or panic/blame -25%)
- **Permanent Augmentation System**: Shared augmentation research increases aggregate cohesion (+10% from shared progress)
