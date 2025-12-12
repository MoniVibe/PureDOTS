# Sub-Band System - Agnostic Framework

## Overview

This document provides the mathematical and algorithmic framework for sub-band systems, where aggregate entities temporarily split off smaller detachments to perform specialized tasks. Sub-bands operate semi-independently with specific missions, maintain communication with parent entities, and eventually rejoin or become independent.

**Core Mathematical Principles:**
- Sub-band formation follows cost-benefit optimization
- Success probability depends on composition, skills, and resources
- Communication reliability degrades with distance and time
- Autonomy level determines decision-making authority
- Rejoining success follows probabilistic tracking mechanics

---

## Formation Decision

### Need Assessment

Calculate need for sub-band formation:

```csharp
public static float CalculateSubBandNeed(
    SubBandType type,
    float environmentalFactor,       // 0-1 (unknown terrain, enemy proximity, etc.)
    float resourceDeficit,            // 0-1 (how badly needed resources are)
    float informationGap,             // 0-1 (lack of intel)
    float threatLevel)                // 0-1
{
    float need = type switch
    {
        SubBandType.Scout => (environmentalFactor * 0.5f) + (informationGap * 0.4f) + (threatLevel * 0.1f),
        SubBandType.Foraging => (resourceDeficit * 0.7f) + (environmentalFactor * 0.3f),
        SubBandType.Resupply => resourceDeficit * 0.9f,
        SubBandType.Raiding => (threatLevel * 0.6f) + (resourceDeficit * 0.3f),
        SubBandType.Diplomatic => threatLevel * 0.4f,  // Reduce threat through diplomacy
        SubBandType.RearGuard => threatLevel * 1.0f,    // Direct response to pursuit
        _ => 0f
    };

    return math.clamp(need, 0f, 1f);
}
```

**Decision Threshold:**
```csharp
public static bool ShouldFormSubBand(
    float need,
    float parentBandStrength,         // 0-1 (can afford to split?)
    float availableResources,         // 0-1 (can supply sub-band?)
    float riskTolerance)              // 0-1 (leader's risk appetite)
{
    float threshold = 0.6f - (riskTolerance * 0.3f);  // Aggressive leaders form more sub-bands
    float canAfford = math.min(parentBandStrength, availableResources);

    return need > threshold && canAfford > 0.3f;
}
```

**Example:**
```
Scout party evaluation:
- Environmental factor: 0.85 (90% unexplored)
- Information gap: 0.75 (poor maps)
- Threat level: 0.4 (enemy rumors)
- Need: (0.85 × 0.5) + (0.75 × 0.4) + (0.4 × 0.1) = 0.765

Parent band:
- Strength: 0.82 (800/1000 members)
- Resources: 0.70 (adequate supplies)
- Risk tolerance: 0.6 (moderately aggressive)

Threshold: 0.6 - (0.6 × 0.3) = 0.42
Can afford: min(0.82, 0.70) = 0.70

Decision: 0.765 > 0.42 AND 0.70 > 0.3 → Form scout party ✓
```

---

## Member Selection

### Skill Matching

Select members whose skills match sub-band requirements:

```csharp
public static float CalculateMemberSuitability(
    NativeArray<float> memberSkills,
    NativeArray<float> requiredSkills,
    NativeArray<float> skillWeights)
{
    float suitability = 0f;
    float totalWeight = 0f;

    for (int i = 0; i < requiredSkills.Length; i++)
    {
        float skillGap = memberSkills[i] - requiredSkills[i];
        float skillScore = math.clamp(skillGap / 50f, -0.5f, 1.0f);  // +1 at 50 points above, -0.5 at 50 below

        suitability += skillScore * skillWeights[i];
        totalWeight += skillWeights[i];
    }

    return suitability / totalWeight;
}
```

**Example:**
```
Scout party requirement (weights in parentheses):
- Navigation: 60 (0.4 weight)
- Stealth: 50 (0.3 weight)
- Survival: 40 (0.3 weight)

Candidate member:
- Navigation: 85 (+25 above requirement)
- Stealth: 45 (-5 below requirement)
- Survival: 60 (+20 above requirement)

Scores:
- Navigation: 25/50 = 0.5 × 0.4 = 0.20
- Stealth: -5/50 = -0.1 × 0.3 = -0.03
- Survival: 20/50 = 0.4 × 0.3 = 0.12
- Total: (0.20 - 0.03 + 0.12) / 1.0 = 0.29

Suitability: 0.29 (above-average, acceptable)
```

---

### Composition Optimization

Optimal sub-band size balances capability and overhead:

```csharp
public static int CalculateOptimalSubBandSize(
    SubBandType type,
    float missionComplexity,          // 0-1
    float availableMembers,           // Total eligible members
    float resourceConstraint)         // 0-1 (supply availability)
{
    int baseSize = type switch
    {
        SubBandType.Scout => 5,
        SubBandType.Foraging => 10,
        SubBandType.Resupply => 15,
        SubBandType.Raiding => 25,
        SubBandType.Diplomatic => 5,
        SubBandType.RearGuard => 30,
        _ => 8
    };

    float complexityModifier = 1f + (missionComplexity * 0.5f);  // +0 to +50% size
    float resourceModifier = math.clamp(resourceConstraint, 0.5f, 1.5f);

    int optimalSize = (int)(baseSize * complexityModifier * resourceModifier);

    return math.clamp(optimalSize, 3, (int)availableMembers);
}
```

**Example:**
```
Scout party:
- Base size: 5
- Mission complexity: 0.7 (difficult terrain, strong enemies)
- Resource constraint: 0.9 (ample supplies)

Complexity modifier: 1 + (0.7 × 0.5) = 1.35
Optimal size: 5 × 1.35 × 0.9 = 6.075 → 6 members
```

---

## Resource Allocation

### Supply Calculation

Calculate resources needed for sub-band mission:

```csharp
public static ResourcePackage CalculateSubBandResources(
    int memberCount,
    float missionDuration,            // days
    SubBandType type,
    float safetyMargin)               // 1.2 = 20% extra
{
    // Food (1 ration/person/day)
    float foodRations = memberCount * missionDuration * safetyMargin;

    // Gold (mission-specific)
    float gold = type switch
    {
        SubBandType.Scout => 50f * memberCount,  // Bribes, emergency purchases
        SubBandType.Foraging => 20f * memberCount,
        SubBandType.Resupply => 5000f + (memberCount * 100f),  // Purchase supplies
        SubBandType.Raiding => 100f * memberCount,  // Equipment
        SubBandType.Diplomatic => 500f * memberCount,  // Gifts, bribes
        SubBandType.RearGuard => 30f * memberCount,
        _ => 50f * memberCount
    };

    // Equipment weight (kg/person)
    float equipmentWeight = type switch
    {
        SubBandType.Scout => 12f,      // Light gear
        SubBandType.Foraging => 8f,    // Minimal gear
        SubBandType.Resupply => 5f,    // Traveling light, will carry cargo back
        SubBandType.Raiding => 20f,    // Combat gear
        SubBandType.Diplomatic => 10f,  // Formal attire, gifts
        SubBandType.RearGuard => 25f,  // Heavy armor
        _ => 15f
    };

    float totalWeight = (memberCount * equipmentWeight) + (foodRations * 0.5f);  // Food weighs 0.5kg/ration

    return new ResourcePackage
    {
        FoodRations = (int)foodRations,
        Gold = (int)gold,
        TotalWeight = totalWeight,
        CargoCapacity = CalculateCargoCapacity(type, memberCount)
    };
}
```

---

### Cargo Capacity

```csharp
public static float CalculateCargoCapacity(
    SubBandType type,
    int memberCount)
{
    float baseCapacityPerMember = 15f;  // kg

    float typeModifier = type switch
    {
        SubBandType.Foraging => 1.5f,   // Carry foraged goods
        SubBandType.Resupply => 3.0f,   // Carts, pack animals
        SubBandType.Scout => 0.8f,      // Travel light
        SubBandType.Raiding => 1.2f,    // Loot
        _ => 1.0f
    };

    return memberCount * baseCapacityPerMember * typeModifier;
}
```

---

## Mission Success Probability

### Base Success Calculation

```csharp
public static float CalculateMissionSuccessProbability(
    float averageSkillLevel,          // 0-100
    float optimalSizeRatio,           // actual size / optimal size
    float resourceAdequacy,           // 0-1
    float environmentalDifficulty,    // 0-1
    float threatLevel)                // 0-1
{
    // Skill factor
    float skillFactor = averageSkillLevel / 100f;

    // Size factor (penalty if too small or too large)
    float sizeFactor = 1f - math.abs(1f - optimalSizeRatio) * 0.3f;
    sizeFactor = math.max(0.5f, sizeFactor);

    // Resource factor
    float resourceFactor = math.clamp(resourceAdequacy, 0.3f, 1.2f);

    // Difficulty penalty
    float difficultyPenalty = 1f - (environmentalDifficulty * 0.4f);

    // Threat penalty
    float threatPenalty = 1f - (threatLevel * 0.5f);

    float successProbability = skillFactor * sizeFactor * resourceFactor * difficultyPenalty * threatPenalty;

    return math.clamp(successProbability, 0.05f, 0.95f);
}
```

**Example:**
```
Scout party:
- Average skill: 65
- Size: 6 (optimal: 6, ratio: 1.0)
- Resources: 0.9 (good)
- Difficulty: 0.6 (moderate terrain)
- Threat: 0.3 (some enemies)

Calculation:
- Skill: 65/100 = 0.65
- Size: 1 - |1 - 1.0| × 0.3 = 1.0
- Resource: 0.9
- Difficulty penalty: 1 - (0.6 × 0.4) = 0.76
- Threat penalty: 1 - (0.3 × 0.5) = 0.85

Success: 0.65 × 1.0 × 0.9 × 0.76 × 0.85 = 0.376 (37.6%)
```

---

### Risk Factors

Calculate specific risks:

```csharp
public static RiskAssessment CalculateSubBandRisks(
    SubBandType type,
    float threatLevel,
    float distance,                   // km from parent
    float loyaltyScore,               // 0-100
    float valuableCargoValue)         // gold worth
{
    // Enemy encounter risk
    float encounterRisk = math.clamp(threatLevel * (distance / 50f), 0f, 0.8f);

    // Desertion risk
    float desertionRisk = (100f - loyaltyScore) / 200f;  // 0-0.5
    if (valuableCargoValue > 1000f)
        desertionRisk *= 1.5f;  // Temptation increases desertion

    // Capture risk (if encountered)
    float captureRisk = encounterRisk * 0.3f * (1f - (GetAverageSkill() / 100f));

    // Loss risk (total failure)
    float lossRisk = encounterRisk * captureRisk * 0.5f;

    // Getting lost risk
    float lostRisk = (distance / 100f) * (1f - (GetNavigationSkill() / 100f));

    return new RiskAssessment
    {
        EncounterRisk = encounterRisk,
        DesertionRisk = desertionRisk,
        CaptureRisk = captureRisk,
        TotalLossRisk = lossRisk,
        GettingLostRisk = lostRisk
    };
}
```

---

## Autonomy and Communication

### Autonomy Level Determination

```csharp
public static float CalculateAutonomyLevel(
    float distance,                   // km from parent
    float leaderTrust,                // 0-100 (trust in sub-band leader)
    float missionCriticality,         // 0-1 (how important mission is)
    float parentControlPreference)    // 0-1 (micromanagement tendency)
{
    float distanceFactor = math.clamp(distance / 100f, 0f, 1f);  // Full autonomy at 100km+
    float trustFactor = leaderTrust / 100f;
    float criticalityFactor = 1f - missionCriticality;  // Critical missions get less autonomy

    float baseAutonomy = (distanceFactor + trustFactor + criticalityFactor) / 3f;
    float autonomy = baseAutonomy * (1f - parentControlPreference * 0.5f);

    return math.clamp(autonomy, 0.1f, 0.9f);
}
```

**Autonomy Levels:**
```
0.0 - 0.3: Tethered (must report every 6 hours, cannot deviate from orders)
0.3 - 0.6: Semi-Autonomous (report every 24 hours, tactical freedom)
0.6 - 1.0: Independent (report every 3-7 days, strategic freedom)
```

---

### Communication Reliability

```csharp
public static float CalculateCommunicationReliability(
    CommunicationMethod method,
    float distance,                   // km
    float weatherSeverity,            // 0-1
    float terrainDifficulty)          // 0-1
{
    float baseReliability = method switch
    {
        CommunicationMethod.Messenger => 0.85f,
        CommunicationMethod.SignalFire => 0.95f,
        CommunicationMethod.Magic => 0.98f,
        CommunicationMethod.Pigeon => 0.75f,
        _ => 0.5f
    };

    float distancePenalty = method switch
    {
        CommunicationMethod.Messenger => math.clamp(1f - (distance / 200f), 0.3f, 1f),
        CommunicationMethod.SignalFire => distance < 20f ? 1f : 0f,  // Line of sight only
        CommunicationMethod.Magic => 1f,  // Unlimited range
        CommunicationMethod.Pigeon => math.clamp(1f - (distance / 300f), 0.2f, 1f),
        _ => 1f
    };

    float weatherPenalty = 1f - (weatherSeverity * 0.3f);
    float terrainPenalty = 1f - (terrainDifficulty * 0.2f);

    float reliability = baseReliability * distancePenalty * weatherPenalty * terrainPenalty;

    return math.clamp(reliability, 0.05f, 0.99f);
}
```

**Example:**
```
Messenger rider:
- Distance: 50 km
- Weather: 0.4 (moderate rain)
- Terrain: 0.3 (hilly)

Calculation:
- Base: 0.85
- Distance penalty: 1 - (50/200) = 0.75
- Weather penalty: 1 - (0.4 × 0.3) = 0.88
- Terrain penalty: 1 - (0.3 × 0.2) = 0.94

Reliability: 0.85 × 0.75 × 0.88 × 0.94 = 0.528 (52.8% reliable)
```

---

## Rejoining Mechanics

### Rendezvous Success Probability

```csharp
public static float CalculateRendezvousSuccessProbability(
    bool isFixedLocation,
    float trackingSkill,              // 0-100
    float navigationSkill,            // 0-100
    float timeSinceLastContact,       // hours
    float parentMovementSpeed,        // km/h
    float terrainDifficulty)          // 0-1
{
    if (isFixedLocation)
    {
        // Fixed rendezvous is easier
        float navigationFactor = navigationSkill / 100f;
        float delayPenalty = math.max(0.5f, 1f - (timeSinceLastContact / 48f));  // Penalty after 48 hours

        return math.clamp(0.7f + (navigationFactor * 0.25f) * delayPenalty, 0.5f, 0.95f);
    }
    else
    {
        // Mobile rendezvous requires tracking
        float trackingFactor = trackingSkill / 100f;
        float navigationFactor = navigationSkill / 100f;

        // Parent movement makes tracking harder
        float movementPenalty = 1f - math.min(0.4f, parentMovementSpeed / 50f);

        // Time penalty (trail gets cold)
        float timePenalty = math.max(0.3f, 1f - (timeSinceLastContact / 24f));

        // Terrain affects tracking
        float terrainFactor = 1f - (terrainDifficulty * 0.5f);

        float successProb = 0.6f + (trackingFactor * 0.25f) + (navigationFactor * 0.15f);
        successProb *= movementPenalty * timePenalty * terrainFactor;

        return math.clamp(successProb, 0.1f, 0.9f);
    }
}
```

**Example (Mobile Rendezvous):**
```
Tracking parent through plains:
- Tracking skill: 75
- Navigation skill: 60
- Time since contact: 12 hours
- Parent speed: 15 km/h
- Terrain difficulty: 0.2 (plains, easy tracking)

Calculation:
- Base: 0.6
- Tracking factor: 75/100 × 0.25 = 0.1875
- Navigation factor: 60/100 × 0.15 = 0.09
- Movement penalty: 1 - (15/50) = 0.7
- Time penalty: 1 - (12/24) = 0.5
- Terrain factor: 1 - (0.2 × 0.5) = 0.9

Success: (0.6 + 0.1875 + 0.09) × 0.7 × 0.5 × 0.9 = 0.277 (27.7%)
```

---

### Rejoining Timeline

```csharp
public static float CalculateRejoiningTime(
    float distanceToRendezvous,       // km
    float subBandSpeed,               // km/h
    float terrainDifficulty,          // 0-1
    bool hasNavigationAid)            // Map, compass, guide
{
    float terrainSpeedModifier = 1f - (terrainDifficulty * 0.4f);
    float navigationBonus = hasNavigationAid ? 1.15f : 1f;

    float effectiveSpeed = subBandSpeed * terrainSpeedModifier * navigationBonus;
    float travelTime = distanceToRendezvous / effectiveSpeed;

    return travelTime;  // hours
}
```

---

## Resource Transfer

### Cargo Transfer Calculation

```csharp
public static ResourceTransfer CalculateResourceTransfer(
    SubBandType type,
    bool missionSuccessful,
    float successQuality,             // 0-1 (how well mission went)
    float cargoCapacity,
    NativeArray<float> foragedResources)
{
    if (!missionSuccessful)
    {
        return new ResourceTransfer
        {
            FoodGain = 0,
            GoldGain = 0,
            IntelValue = 0,
            MoraleImpact = -15f  // Failed mission
        };
    }

    float qualityMultiplier = math.clamp(successQuality, 0.5f, 1.5f);

    int foodGain = type switch
    {
        SubBandType.Foraging => (int)(50f + (cargoCapacity * 2f) * qualityMultiplier),
        SubBandType.Resupply => (int)(500f + (cargoCapacity * 5f) * qualityMultiplier),
        SubBandType.Raiding => (int)(100f + (cargoCapacity * 1.5f) * qualityMultiplier),
        _ => 0
    };

    int goldGain = type switch
    {
        SubBandType.Raiding => (int)(200f + (cargoCapacity * 10f) * qualityMultiplier),
        SubBandType.Diplomatic => 0,  // Diplomatic gains are intangible
        _ => 0
    };

    float intelValue = type switch
    {
        SubBandType.Scout => 0.6f + (successQuality * 0.4f),
        SubBandType.Diplomatic => 0.5f + (successQuality * 0.5f),
        _ => 0.2f
    };

    float moraleImpact = 5f + (successQuality * 20f);

    return new ResourceTransfer
    {
        FoodGain = foodGain,
        GoldGain = goldGain,
        IntelValue = intelValue,
        MoraleImpact = moraleImpact
    };
}
```

---

## Casualty Calculation

### Expected Casualties

```csharp
public static CasualtyReport CalculateExpectedCasualties(
    SubBandType type,
    float threatLevel,
    float combatSkillAverage,
    float missionDuration)
{
    float baseCasualtyRate = type switch
    {
        SubBandType.Scout => 0.05f,      // Low combat
        SubBandType.Foraging => 0.08f,
        SubBandType.Resupply => 0.12f,   // Tempting target
        SubBandType.Raiding => 0.35f,    // High combat
        SubBandType.Diplomatic => 0.10f,
        SubBandType.RearGuard => 0.60f,  // Sacrifice formation
        _ => 0.10f
    };

    float threatMultiplier = 1f + (threatLevel * 2f);  // Doubles at max threat
    float skillReduction = 1f - (combatSkillAverage / 200f);  // Max -50% casualties
    float durationMultiplier = 1f + (missionDuration / 10f);  // +10% per 10 days

    float finalCasualtyRate = baseCasualtyRate * threatMultiplier * skillReduction * durationMultiplier;
    finalCasualtyRate = math.clamp(finalCasualtyRate, 0.02f, 0.85f);

    // Split casualties into killed, wounded, deserted, captured
    float killedRate = finalCasualtyRate * 0.4f;
    float woundedRate = finalCasualtyRate * 0.35f;
    float desertedRate = finalCasualtyRate * 0.15f;
    float capturedRate = finalCasualtyRate * 0.10f;

    return new CasualtyReport
    {
        TotalCasualtyRate = finalCasualtyRate,
        KilledRate = killedRate,
        WoundedRate = woundedRate,
        DesertedRate = desertedRate,
        CapturedRate = capturedRate
    };
}
```

**Example:**
```
Raiding party:
- Type: Raiding (base 35%)
- Threat: 0.6 (moderate enemy)
- Combat skill avg: 70
- Duration: 4 days

Calculation:
- Base: 0.35
- Threat multiplier: 1 + (0.6 × 2) = 2.2
- Skill reduction: 1 - (70/200) = 0.65
- Duration multiplier: 1 + (4/10) = 1.4

Final rate: 0.35 × 2.2 × 0.65 × 1.4 = 0.696 (69.6% casualties)
- Killed: 69.6% × 0.4 = 27.8%
- Wounded: 69.6% × 0.35 = 24.4%
- Deserted: 69.6% × 0.15 = 10.4%
- Captured: 69.6% × 0.10 = 7.0%
```

---

## Independence Probability

### Becoming Independent

```csharp
public static float CalculateIndependenceProbability(
    float subBandLoyalty,             // 0-100
    float lootValue,                  // gold
    float threatToParent,             // 0-1 (parent band in danger)
    float distanceFromParent,         // km
    bool parentBandDestroyed)
{
    if (parentBandDestroyed)
        return 1.0f;  // Orphaned sub-bands become independent by default

    float loyaltyFactor = (100f - subBandLoyalty) / 100f;  // Low loyalty = high independence

    float lootTemptation = math.min(1f, lootValue / 1000f);  // Caps at 1,000 gold

    float threatFactor = threatToParent;  // Parent in danger = higher independence

    float distanceFactor = math.clamp(distanceFromParent / 200f, 0f, 0.5f);  // Max +50% from distance

    float independenceProb = (loyaltyFactor * 0.4f) + (lootTemptation * 0.3f) + (threatFactor * 0.2f) + distanceFactor;

    return math.clamp(independenceProb, 0.01f, 0.95f);
}
```

**Example:**
```
Raiding party after successful raid:
- Loyalty: 45 (low)
- Loot: 800 gold
- Threat to parent: 0.3 (moderate)
- Distance: 100 km
- Parent destroyed: No

Calculation:
- Loyalty factor: (100 - 45)/100 = 0.55
- Loot temptation: 800/1000 = 0.8
- Threat factor: 0.3
- Distance factor: 100/200 = 0.5 (clamped to 0.5 max)

Independence: (0.55 × 0.4) + (0.8 × 0.3) + (0.3 × 0.2) + 0.5 = 0.22 + 0.24 + 0.06 + 0.5 = 1.02 → 0.95 (95% chance of independence!)
```

---

## ECS Implementation

### Core Components

```csharp
public struct SubBandFormationRequest : IComponentData
{
    public Entity ParentEntity;
    public SubBandType Type;
    public float Need;                    // 0-1
    public int RequestedSize;
    public float MissionDuration;         // days
    public float3 TargetLocation;
    public bool Approved;
}

public struct SubBandEntity : IComponentData
{
    public Entity SubBandId;
    public Entity ParentId;
    public SubBandType Type;
    public int MemberCount;
    public float AutonomyLevel;           // 0-1
    public float MissionStartTime;
    public float MissionDuration;
    public bool MissionComplete;
    public bool HasRejoined;
}

public struct SubBandMissionProgress : IComponentData
{
    public Entity SubBandId;
    public float ProgressPercent;         // 0-100
    public float SuccessProbability;      // 0-1
    public float TimeElapsed;             // days
    public FixedList64Bytes<float> SubObjectiveProgress;
}

public struct SubBandResources : IComponentData
{
    public int FoodRations;
    public int Gold;
    public float CargoCapacity;
    public float CurrentCargo;
    public NativeArray<ResourceType> AcquiredResources;
}

public struct SubBandCommunication : IComponentData
{
    public Entity SubBandId;
    public Entity ParentId;
    public CommunicationMethod Method;
    public float LastContactTime;         // hours ago
    public float NextReportDue;           // hours
    public float CommunicationReliability; // 0-1
    public bool ContactLost;
}

public struct RendezvousData : IComponentData
{
    public Entity SubBandId;
    public float3 Location;
    public float ScheduledTime;
    public bool IsFixedLocation;
    public float RendezvousSuccessProbability;
}

public struct SubBandCasualties : IComponentData
{
    public int MembersKilled;
    public int MembersWounded;
    public int MembersDeserted;
    public int MembersCaptured;
    public float CasualtyRate;
}
```

---

### Core Systems

**SubBandFormationSystem** (0.2 Hz):
```csharp
public partial struct SubBandFormationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var request in SystemAPI.Query<RefRW<SubBandFormationRequest>>())
        {
            if (request.ValueRO.Approved)
                continue;

            float need = CalculateSubBandNeed(
                request.ValueRO.Type,
                GetEnvironmentalFactor(request.ValueRO.ParentEntity),
                GetResourceDeficit(request.ValueRO.ParentEntity),
                GetInformationGap(request.ValueRO.ParentEntity),
                GetThreatLevel(request.ValueRO.ParentEntity)
            );

            bool shouldForm = ShouldFormSubBand(
                need,
                GetParentStrength(request.ValueRO.ParentEntity),
                GetAvailableResources(request.ValueRO.ParentEntity),
                GetRiskTolerance(request.ValueRO.ParentEntity)
            );

            if (shouldForm)
            {
                CreateSubBand(request.ValueRO);
                request.ValueRW.Approved = true;
            }
        }
    }
}
```

**SubBandMissionSystem** (0.2 Hz):
```csharp
public partial struct SubBandMissionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (subBand, progress) in SystemAPI.Query<RefRO<SubBandEntity>, RefRW<SubBandMissionProgress>>())
        {
            float deltaTime = SystemAPI.Time.DeltaTime / 86400f;  // Convert to days
            progress.ValueRW.TimeElapsed += deltaTime;

            // Update progress based on type
            UpdateMissionProgress(ref progress.ValueRW, subBand.ValueRO.Type, deltaTime);

            // Calculate success probability
            progress.ValueRW.SuccessProbability = CalculateMissionSuccessProbability(
                GetAverageSkillLevel(subBand.ValueRO.SubBandId),
                GetOptimalSizeRatio(subBand.ValueRO),
                GetResourceAdequacy(subBand.ValueRO.SubBandId),
                GetEnvironmentalDifficulty(subBand.ValueRO.SubBandId),
                GetThreatLevel(subBand.ValueRO.SubBandId)
            );

            // Check mission completion
            if (progress.ValueRO.ProgressPercent >= 100f)
            {
                CompleteMission(subBand.ValueRO.SubBandId, progress.ValueRO.SuccessProbability);
            }
        }
    }
}
```

**SubBandRejoiningSystem** (0.2 Hz):
```csharp
public partial struct SubBandRejoiningSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (subBand, rendezvous) in SystemAPI.Query<RefRW<SubBandEntity>, RefRO<RendezvousData>>())
        {
            if (!subBand.ValueRO.MissionComplete || subBand.ValueRO.HasRejoined)
                continue;

            float currentTime = GetGameTime();

            if (currentTime >= rendezvous.ValueRO.ScheduledTime)
            {
                float successProb = CalculateRendezvousSuccessProbability(
                    rendezvous.ValueRO.IsFixedLocation,
                    GetTrackingSkill(subBand.ValueRO.SubBandId),
                    GetNavigationSkill(subBand.ValueRO.SubBandId),
                    GetTimeSinceLastContact(subBand.ValueRO.SubBandId),
                    GetParentMovementSpeed(subBand.ValueRO.ParentId),
                    GetTerrainDifficulty(subBand.ValueRO.SubBandId)
                );

                var random = new Random((uint)currentTime);
                bool success = random.NextFloat() < successProb;

                if (success)
                {
                    ReintegrateSubBand(subBand.ValueRW);
                }
            }
        }
    }
}
```

---

## Conclusion

This agnostic framework provides the mathematical foundation for sub-band systems. The formation decision follows cost-benefit optimization, success probability depends on multiple interdependent factors, communication reliability degrades with distance and environmental conditions, and rejoining mechanics combine pathfinding with probabilistic tracking. Implementations can tune these formulas to balance sub-band utility against risks and overhead.

**Key Design Principles:**
1. **Cost-Benefit Optimization**: Only form sub-bands when expected value exceeds costs
2. **Skill-Based Success**: Better-skilled sub-bands have higher success rates
3. **Communication Degradation**: Distance and time reduce coordination
4. **Probabilistic Outcomes**: Success, casualties, and rejoining are not guaranteed
5. **Independence Risk**: Sub-bands may become autonomous under certain conditions
