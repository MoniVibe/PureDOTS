# Rebellion Mechanics System (Agnostic Framework)

## Overview
Defines mathematical frameworks for rebellion dynamics when vassals/subjects resist overlords. Populations divide into loyalist, rebel, and neutral factions. Algorithms cover loyalty determination, informant risk, recruitment targeting, rebellion escalation, and outcome calculation. Game-agnostic formulas enable cautious conspiracy, information warfare, and spectrum of outcomes.

**Integration**: Mind ECS (1 Hz) for individual loyalty, Aggregate ECS (0.2 Hz) for rebellion coordination.

---

## Core Components

### Entity Loyalty Component
```csharp
public struct EntityLoyaltyComponent : IComponentData
{
    public Entity OverlordEntity;      // Liege/ruler this entity owes allegiance to
    public float LoyaltyToOverlord;    // 0-100 (current loyalty)
    public float GrievanceLevel;       // 0-100 (accumulated complaints)
    public LoyaltyFaction Faction;     // Loyalist, Rebel, or Neutral
    public int PersonalBondsWithOverlord; // Close relationships (0-10)
    public int PersonalBondsWithRebels;   // Relationships with rebel leaders (0-10)
    public bool HasIdeologicalMotivation; // True believer vs opportunist
    public float CourageLevel;         // 0-100 (willingness to risk life)
}

public enum LoyaltyFaction : byte
{
    Loyalist,   // Supports overlord
    Rebel,      // Actively resists overlord
    Neutral     // Takes no side
}
```

### Recruitment Risk Component
```csharp
public struct RecruitmentRiskComponent : IComponentData
{
    public float InformantRisk;        // 0-1 (probability target informs overlord)
    public RecruitmentTier SafetyTier; // Safe, Risky, Dangerous, Suicidal
    public bool HasBeenContacted;      // Rebels already approached this entity
    public bool InformedOnRebels;      // Entity reported conspiracy
    public int MonthsSinceContact;     // Time since rebels approached
}

public enum RecruitmentTier : byte
{
    Safe,       // Loyalty ≤ 25, informant risk < 20%
    Risky,      // Loyalty 26-50, informant risk 30-50%
    Dangerous,  // Loyalty 51-75, informant risk 60-80%
    Suicidal    // Loyalty 76-100, informant risk 90-100%
}
```

### Rebellion State Component
```csharp
public struct RebellionStateComponent : IComponentData
{
    public Entity OverlordEntity;
    public Entity RebelLeaderEntity;
    public RebellionStage Stage;       // Individual, Conspiracy, Movement, Uprising
    public RebellionType Type;         // Violent, Peaceful, Coup, Secession
    public int RebelCount;
    public int LoyalistCount;
    public int NeutralCount;
    public float MonthsActive;
    public float EscalationLevel;      // 0-1 (peaceful to total war)
}

public enum RebellionStage : byte
{
    Individual,   // 1 person (outlier, no organization)
    Conspiracy,   // 5-20 people (secret planning)
    Movement,     // 50-200 people (organized resistance)
    Uprising      // 500+ people (civil war)
}

public enum RebellionType : byte
{
    Violent,      // Armed conflict
    Peaceful,     // Civil disobedience
    Coup,         // Sudden internal overthrow
    Secession     // Independence movement
}
```

---

## Agnostic Algorithms

### Loyalty Faction Determination

#### Calculate Entity Faction
```csharp
/// <summary>
/// Determine which faction entity supports during rebellion
/// Agnostic: Loyalty + Grievances + Bonds + Ideology + Courage
/// </summary>
public static LoyaltyFaction DetermineLoyaltyFaction(
    float loyaltyToOverlord,           // 0-100
    float grievanceLevel,              // 0-100
    int personalBondsWithOverlord,     // 0-10 (close relationships)
    int personalBondsWithRebels,       // 0-10
    bool hasIdeologicalMotivation,     // True believer (ideology-driven)
    bool fearOfReprisal,               // Fears overlord's punishment
    float courageLevel)                // 0-100 (bravery)
{
    // Loyalist score
    float loyalistScore = loyaltyToOverlord + (personalBondsWithOverlord * 10f);

    // Fear of reprisal: Cowards stay loyal to avoid punishment
    if (fearOfReprisal && courageLevel < 50f)
        loyalistScore += 20f;

    // Rebel score
    float rebelScore = grievanceLevel + (personalBondsWithRebels * 10f);

    // Ideological motivation: True believers more committed to rebellion
    if (hasIdeologicalMotivation)
        rebelScore += 25f;

    // Courage: Brave more likely to rebel (high risk activity)
    if (courageLevel > 70f)
        rebelScore += 15f;

    // Neutral score
    float neutralScore = 50f; // Base neutrality

    // Cowardice without ideology: Hide, avoid conflict
    if (courageLevel < 40f && !hasIdeologicalMotivation)
        neutralScore += 20f;

    // Conflicted loyalties: Personal bonds on both sides → neutrality
    if (personalBondsWithOverlord > 0 && personalBondsWithRebels > 0)
        neutralScore += 30f;

    // Determine faction by highest score
    if (loyalistScore > rebelScore && loyalistScore > neutralScore)
        return LoyaltyFaction.Loyalist;
    else if (rebelScore > loyalistScore && rebelScore > neutralScore)
        return LoyaltyFaction.Rebel;
    else
        return LoyaltyFaction.Neutral;
}
```

#### Loyalty Shift Probability
```csharp
/// <summary>
/// Calculate probability of entity switching factions
/// Agnostic: Events during rebellion cause loyalty shifts
/// </summary>
public static float CalculateLoyaltyShiftProbability(
    LoyaltyFaction currentFaction,
    EventType triggeringEvent,         // What event occurred
    float eventSeverity,               // 0-1 (how impactful the event)
    float currentLoyalty,              // 0-100
    float currentGrievance)            // 0-100
{
    float shiftProbability = 0f;

    switch (currentFaction)
    {
        case LoyaltyFaction.Neutral:
            // Neutrals most likely to shift
            if (triggeringEvent == EventType.OverlordAtrocity)
                shiftProbability = 0.4f * eventSeverity; // → Rebel
            else if (triggeringEvent == EventType.RebelAtrocity)
                shiftProbability = 0.3f * eventSeverity; // → Loyalist
            else if (triggeringEvent == EventType.OverlordTaxation)
                shiftProbability = 0.25f * eventSeverity; // → Rebel
            else if (triggeringEvent == EventType.RebelVictory)
                shiftProbability = 0.35f * eventSeverity; // → Rebel (bandwagon)
            break;

        case LoyaltyFaction.Loyalist:
            // Loyalists rarely shift, but possible
            if (triggeringEvent == EventType.OverlordBetrayal)
                shiftProbability = 0.6f * eventSeverity; // → Rebel (personal betrayal)
            else if (triggeringEvent == EventType.OverlordLosing)
                shiftProbability = 0.15f * eventSeverity; // → Neutral (abandonment)
            else if (triggeringEvent == EventType.PaymentStopped)
                shiftProbability = 0.25f * eventSeverity; // → Neutral
            break;

        case LoyaltyFaction.Rebel:
            // Rebels rarely shift (committed)
            if (triggeringEvent == EventType.RebellionFailing)
                shiftProbability = 0.2f * eventSeverity; // → Neutral (fear)
            else if (triggeringEvent == EventType.AmnestyOffered)
                shiftProbability = 0.3f * eventSeverity; // → Neutral/Loyalist
            else if (triggeringEvent == EventType.RebelLeaderKilled)
                shiftProbability = 0.35f * eventSeverity; // → Neutral (demoralized)
            break;
    }

    // Loyalty/grievance modifiers
    if (currentLoyalty < 20f)
        shiftProbability *= 1.5f; // Low loyalty = more volatile
    if (currentGrievance > 80f)
        shiftProbability *= 1.3f; // High grievance = less stable

    return math.clamp(shiftProbability, 0f, 0.95f);
}

public enum EventType : byte
{
    OverlordAtrocity,      // Overlord commits violence against population
    RebelAtrocity,         // Rebels commit violence
    OverlordTaxation,      // Heavy taxes imposed
    OverlordBetrayal,      // Overlord betrays loyal subject
    OverlordLosing,        // Overlord losing military conflict
    PaymentStopped,        // Wages/benefits stopped
    RebellionFailing,      // Rebels losing badly
    AmnestyOffered,        // Overlord offers pardon
    RebelLeaderKilled,     // Rebel leadership decapitated
    RebelVictory           // Rebels winning battles
}
```

### Informant Risk Calculation

#### Calculate Informant Probability
```csharp
/// <summary>
/// Calculate probability that recruitment target will inform overlord of conspiracy
/// Agnostic: Loyalty + Relationships + Grievances + Intelligence
/// </summary>
public static float CalculateInformantRisk(
    float targetLoyaltyToOverlord,     // 0-100
    int targetRelationWithRebels,      // -100 to +100 (personal relationship)
    float rebelIntelligence,           // 0-100 (ability to read target)
    bool targetHasPersonalGrievance,   // Does target have reason to rebel?
    bool targetIsFearful,              // Is target afraid of overlord?
    bool targetIsIdeological)          // Is target ideologically committed?
{
    // Base risk from loyalty
    float baseRisk = targetLoyaltyToOverlord / 100f; // 0-1

    // Personal relationship modifier
    // Positive relation with rebels → less likely to inform
    // Negative relation → more likely to inform
    float relationModifier = -targetRelationWithRebels / 200f; // -0.5 to +0.5
    baseRisk += relationModifier;

    // Personal grievance: Begrudged targets less likely to inform
    if (targetHasPersonalGrievance)
        baseRisk -= 0.3f;

    // Fear: Fearful targets inform to protect themselves from overlord's wrath
    if (targetIsFearful)
        baseRisk += 0.2f;

    // Ideology: Ideologically committed loyalists ALWAYS inform
    if (targetIsIdeological && targetLoyaltyToOverlord > 70f)
        baseRisk += 0.4f;

    // Rebel intelligence: Smart rebels read targets better, avoid informants
    float intelligenceModifier = (100f - rebelIntelligence) / 200f; // 0 to 0.5
    baseRisk += intelligenceModifier;

    return math.clamp(baseRisk, 0f, 1f);
}
```

#### Determine Recruitment Tier
```csharp
/// <summary>
/// Categorize recruitment target by safety tier
/// Agnostic: Loyalty thresholds determine risk
/// </summary>
public static RecruitmentTier DetermineRecruitmentTier(
    float targetLoyalty,               // 0-100
    float informantRisk)               // 0-1 (calculated risk)
{
    if (targetLoyalty <= 25f && informantRisk < 0.2f)
        return RecruitmentTier.Safe;
    else if (targetLoyalty <= 50f && informantRisk < 0.5f)
        return RecruitmentTier.Risky;
    else if (targetLoyalty <= 75f && informantRisk < 0.8f)
        return RecruitmentTier.Dangerous;
    else
        return RecruitmentTier.Suicidal; // Never recruit
}
```

### Rebellion Initiation & Escalation

#### Calculate Rebellion Initiation Probability
```csharp
/// <summary>
/// Calculate probability of rebellion beginning
/// Agnostic: Population grievance + leader courage + external opportunity
/// </summary>
public static float CalculateRebellionInitiationProbability(
    float averageGrievanceLevel,       // 0-100 (population average)
    float overlordLoyalty,             // 0-100 (average loyalty to overlord)
    bool hasCharismaticLeader,         // Leader with CHA > 80
    bool externalAllyAvailable,        // Foreign support available
    bool overlordWeakened,             // Overlord distracted/weakened
    int potentialRebelCount)           // Number of begrudged subjects
{
    // Grievance factor: High grievances → rebellion more likely
    float grievanceFactor = averageGrievanceLevel / 100f; // 0-1

    // Loyalty penalty: High average loyalty → rebellion less likely
    float loyaltyPenalty = overlordLoyalty / 100f; // 0-1

    // Leadership bonus: Charismatic leader inspires rebellion
    float leadershipBonus = hasCharismaticLeader ? 0.3f : 0f;

    // External support: Ally provides resources/legitimacy
    float externalBonus = externalAllyAvailable ? 0.25f : 0f;

    // Overlord weakness: Opportunity when overlord distracted
    float opportunityBonus = overlordWeakened ? 0.2f : 0f;

    // Scale factor: Larger populations more likely to produce rebels
    float scaleFactor = math.min(potentialRebelCount / 100f, 2f); // Max 2×

    float initiationProbability = (grievanceFactor - loyaltyPenalty + leadershipBonus + externalBonus + opportunityBonus) * scaleFactor;

    return math.clamp(initiationProbability, 0f, 0.95f);
}
```

#### Calculate Escalation Level
```csharp
/// <summary>
/// Calculate rebellion escalation (peaceful to violent)
/// Agnostic: Violence begets violence, negotiation de-escalates
/// </summary>
public static float CalculateEscalationLevel(
    float currentEscalation,           // 0-1 (current level)
    bool violentEventOccurred,         // Battle, massacre, assassination
    bool negotiationAttempted,         // Peaceful talks
    int casualtiesThisMonth,           // Deaths from conflict
    bool overlordOfferedConcessions,   // Overlord compromised
    bool rebelsRejectedOffer)          // Rebels refused compromise
{
    float escalationChange = 0f;

    // Violence increases escalation
    if (violentEventOccurred)
        escalationChange += 0.1f;

    // Casualties increase escalation
    float casualtyFactor = math.min(casualtiesThisMonth / 100f, 0.3f); // Max +0.3
    escalationChange += casualtyFactor;

    // Negotiation decreases escalation
    if (negotiationAttempted)
        escalationChange -= 0.15f;

    // Concessions decrease escalation
    if (overlordOfferedConcessions && !rebelsRejectedOffer)
        escalationChange -= 0.2f;

    // Rejected offers increase escalation (bad faith)
    if (rebelsRejectedOffer)
        escalationChange += 0.05f;

    float newEscalation = currentEscalation + escalationChange;

    return math.clamp(newEscalation, 0f, 1f);
}
```

#### Determine Rebellion Stage
```csharp
/// <summary>
/// Determine rebellion stage from rebel count
/// Agnostic: Size determines organization level
/// </summary>
public static RebellionStage DetermineRebellionStage(
    int activeRebelCount)
{
    if (activeRebelCount < 5)
        return RebellionStage.Individual;
    else if (activeRebelCount < 50)
        return RebellionStage.Conspiracy;
    else if (activeRebelCount < 500)
        return RebellionStage.Movement;
    else
        return RebellionStage.Uprising;
}
```

### Neutral Faction Mechanics

#### Calculate Neutral Punishment Severity
```csharp
/// <summary>
/// Calculate how severely neutrals are punished by each side
/// Agnostic: Desperation and ideology determine punishment
/// </summary>
public static float CalculateNeutralPunishmentSeverity(
    bool isPunisherOverlord,           // True = overlord punishing, False = rebels
    float punisherDesperation,         // 0-1 (how desperate for support)
    float punisherIdeology,            // 0-1 (ideological rigidity)
    float neutralWealth,               // 0-100 (exploitable resources)
    int neutralCount)                  // Number of neutrals
{
    // Base punishment from desperation
    float basePunishment = punisherDesperation * 0.5f; // Max 0.5

    // Ideological rigidity: "With us or against us" mentality
    float ideologyPenalty = punisherIdeology * 0.3f; // Max 0.3

    // Wealth: Rich neutrals more attractive to exploit
    float wealthFactor = neutralWealth / 100f * 0.2f; // Max 0.2

    // Scale: Fewer neutrals → punish harder (make example)
    float scalePenalty = neutralCount < 50 ? 0.2f : 0f;

    // Overlord vs Rebel difference
    // Overlords: Institutional punishment (legal, systematic)
    // Rebels: Ad-hoc punishment (raids, conscription)
    float typeFactor = isPunisherOverlord ? 1.2f : 0.9f;

    float punishmentSeverity = (basePunishment + ideologyPenalty + wealthFactor + scalePenalty) * typeFactor;

    return math.clamp(punishmentSeverity, 0f, 1f);
}
```

#### Calculate Neutral Forced Choice Probability
```csharp
/// <summary>
/// Calculate probability neutral is forced to choose side
/// Agnostic: Punishment pressure + personal safety concerns
/// </summary>
public static float CalculateForcedChoiceProbability(
    float overlordPunishmentSeverity,  // 0-1
    float rebelPunishmentSeverity,     // 0-1
    float neutralResourcesRemaining,   // 0-100 (can neutral survive punishment?)
    int monthsAsNeutral,               // Time spent neutral
    float personalSafetyRisk)          // 0-1 (physical danger)
{
    // Combined punishment pressure
    float totalPressure = (overlordPunishmentSeverity + rebelPunishmentSeverity) / 2f;

    // Resource depletion: Can't stay neutral if resources exhausted
    float resourcePressure = 1f - (neutralResourcesRemaining / 100f); // 0-1

    // Time pressure: Longer as neutral, more pressure accumulates
    float timePressure = math.min(monthsAsNeutral / 12f, 0.5f); // Max 0.5 at 12 months

    // Personal safety: High danger forces choice (survival)
    float safetyPressure = personalSafetyRisk * 0.4f; // Max 0.4

    float forcedChoiceProbability = totalPressure + resourcePressure + timePressure + safetyPressure;

    return math.clamp(forcedChoiceProbability, 0f, 0.95f);
}
```

### Rebellion Outcome Calculation

#### Calculate Rebel Victory Probability
```csharp
/// <summary>
/// Calculate probability of rebel victory
/// Agnostic: Military strength + popular support + overlord weakness
/// </summary>
public static float CalculateRebelVictoryProbability(
    float rebelMilitaryStrength,       // 0-100
    float overlordMilitaryStrength,    // 0-100
    float popularSupportForRebels,     // 0-100 (% population supporting)
    bool externalAllySupportsRebels,   // Foreign intervention
    bool overlordHasInternalProblems,  // Overlord distracted (other wars, plague, etc.)
    float monthsRebellionActive)       // Time advantage
{
    // Military strength ratio
    float militaryRatio = rebelMilitaryStrength / math.max(overlordMilitaryStrength, 1f);
    float militaryFactor = math.clamp(militaryRatio, 0f, 1f);

    // Popular support: Higher support → more recruits, supplies, intelligence
    float supportFactor = popularSupportForRebels / 100f; // 0-1

    // External ally: Major boost (resources, legitimacy)
    float externalBonus = externalAllySupportsRebels ? 0.25f : 0f;

    // Overlord's problems: Distracted overlord weaker
    float distractionBonus = overlordHasInternalProblems ? 0.15f : 0f;

    // Time factor: Long rebellions favor rebels (overlord exhaustion)
    float timeBonus = monthsRebellionActive > 12f ? 0.1f : 0f;

    float victoryProbability = (militaryFactor * 0.4f) + (supportFactor * 0.3f) + externalBonus + distractionBonus + timeBonus;

    return math.clamp(victoryProbability, 0.05f, 0.95f); // Min 5%, max 95%
}
```

#### Calculate Negotiated Settlement Probability
```csharp
/// <summary>
/// Calculate probability of negotiated settlement instead of total victory/defeat
/// Agnostic: Stalemate + war exhaustion → negotiation
/// </summary>
public static float CalculateNegotiationProbability(
    float rebelVictoryProbability,     // 0-1 (how likely rebels win)
    float casualtyRate,                // 0-1 (deaths per month / population)
    int monthsActive,                  // Rebellion duration
    bool externalMediatorPresent,      // Third party mediates
    float overlordCompromiseWillingness, // 0-1
    float rebelCompromiseWillingness)    // 0-1
{
    // Stalemate: Neither side can win decisively
    // Sweet spot: 0.4 to 0.6 victory probability = stalemate
    float stalemate = 0f;
    if (rebelVictoryProbability >= 0.4f && rebelVictoryProbability <= 0.6f)
        stalemate = 0.4f;

    // War exhaustion: High casualties → both sides want peace
    float exhaustion = casualtyRate * 0.3f; // Max 0.3

    // Time: Long wars → exhaustion
    float timeFactor = math.min(monthsActive / 24f, 0.2f); // Max 0.2 at 24 months

    // External mediator: Facilitates negotiation
    float mediatorBonus = externalMediatorPresent ? 0.2f : 0f;

    // Willingness to compromise: Both sides must be willing
    float willingnessBonus = (overlordCompromiseWillingness + rebelCompromiseWillingness) / 2f * 0.3f; // Max 0.3

    float negotiationProbability = stalemate + exhaustion + timeFactor + mediatorBonus + willingnessBonus;

    return math.clamp(negotiationProbability, 0f, 0.9f);
}
```

#### Calculate Martyrdom Effect
```csharp
/// <summary>
/// Calculate long-term martyrdom impact when rebellion defeated
/// Agnostic: Execution cruelty + popular sympathy → future inspiration
/// </summary>
public static float CalculateMartyrdomStrength(
    bool rebelLeaderExecuted,          // Leader killed
    float executionCruelty,            // 0-1 (how brutal the execution)
    float popularSympathyForRebels,    // 0-100 (did population support cause?)
    bool causeWasJust,                 // Was rebellion morally justified?
    int yearsUntilNextRebellion)       // Time for legend to grow
{
    if (!rebelLeaderExecuted)
        return 0f; // No martyrdom without death

    // Base martyrdom from execution
    float baseMartyrdom = 0.3f;

    // Cruelty: Cruel execution increases sympathy
    float crueltyBonus = executionCruelty * 0.3f; // Max 0.3

    // Popular sympathy: Did people support the cause?
    float sympathyBonus = popularSympathyForRebels / 100f * 0.2f; // Max 0.2

    // Just cause: Morally justified rebellions inspire more
    float justiceBonus = causeWasJust ? 0.2f : 0f;

    // Time: Legend grows over time
    float timeFactor = math.min(yearsUntilNextRebellion / 20f, 0.3f); // Max 0.3 at 20 years

    float martyrdomStrength = baseMartyrdom + crueltyBonus + sympathyBonus + justiceBonus + timeFactor;

    return math.clamp(martyrdomStrength, 0f, 1f);
}
```

### Counter-Intelligence Algorithms

#### Calculate Spy Detection Probability
```csharp
/// <summary>
/// Calculate probability rebels detect overlord's spy infiltrating rebellion
/// Agnostic: Spy skill vs rebel intelligence
/// </summary>
public static float CalculateSpyDetectionProbability(
    int spyDeceptionSkill,             // 0-100
    int rebelCounterIntelligence,      // 0-100
    int monthsInfiltrated,             // Time embedded
    bool spySabotaged,                 // Did spy perform sabotage (high profile)
    bool rebelsSuspicious)             // Rebels actively searching for spies
{
    // Base detection: Counter-intelligence vs Deception
    float baseDetection = (rebelCounterIntelligence - spyDeceptionSkill) / 200f; // -0.5 to +0.5

    // Time exposure: Longer infiltration = higher risk
    float timeRisk = monthsInfiltrated * 0.02f; // +2% per month

    // Sabotage: High-profile actions attract attention
    float sabotageRisk = spySabotaged ? 0.15f : 0f;

    // Active counter-intelligence: Rebels searching for spies
    float searchBonus = rebelsSuspicious ? 0.1f : 0f;

    float detectionProbability = baseDetection + timeRisk + sabotageRisk + searchBonus;

    return math.clamp(detectionProbability, 0.01f, 0.95f); // Min 1%, max 95% per month
}
```

#### Calculate False Information Effectiveness
```csharp
/// <summary>
/// Calculate effectiveness of false information test to identify informant
/// Agnostic: Give each suspect different fake plans, see which overlord responds to
/// </summary>
public static int IdentifyInformantViaFalseInformation(
    NativeArray<int> suspectIDs,       // Entity IDs of suspects
    NativeArray<bool> isActualInformant, // Ground truth (which are real informants)
    int numberOfFalsePlans,            // How many different fake plans to feed
    float overlordIntelligenceSkill,   // 0-100 (does overlord realize it's a trap?)
    ref Random random)
{
    if (numberOfFalsePlans < suspectIDs.Length)
        return -1; // Need unique false plan per suspect

    // For each suspect, assign different false plan
    int[] planAssignments = new int[suspectIDs.Length];
    for (int i = 0; i < suspectIDs.Length; i++)
    {
        planAssignments[i] = i; // Suspect i gets false plan i
    }

    // Overlord intelligence: Smart overlords might realize it's a trap
    float trapDetectionChance = overlordIntelligenceSkill / 200f; // Max 50%
    if (random.NextFloat() < trapDetectionChance)
        return -1; // Overlord detected trap, didn't respond to any plan

    // Find which suspect is actual informant
    for (int i = 0; i < suspectIDs.Length; i++)
    {
        if (isActualInformant[i])
        {
            // This suspect informed overlord of false plan i
            // Overlord will respond to false plan i
            return suspectIDs[i]; // Identified informant
        }
    }

    return -1; // No informant among suspects
}
```

---

## ECS System Architecture

### Mind ECS (1 Hz) - Individual Loyalty

**EntityLoyaltyCalculationSystem**:
- Updates loyalty based on events (taxes, violence, rewards)
- Accumulates grievances over time
- Determines faction (Loyalist, Rebel, Neutral)

**EntityRecruitmentTargetingSystem**:
- Calculates informant risk for each potential recruit
- Assigns recruitment tier (Safe, Risky, Dangerous, Suicidal)
- Tracks which entities have been contacted

**EntityFactionSwitchSystem**:
- Handles loyalty shifts during rebellion
- Processes events (atrocities, victories, betrayals)
- Moves entities between factions

**EntityInformantDecisionSystem**:
- Determines if contacted entity informs overlord
- Rolls against informant risk probability
- Updates rebellion state (conspiracy exposed if informed)

### Aggregate ECS (0.2 Hz) - Rebellion Coordination

**RebellionInitiationSystem**:
- Monitors population grievance levels
- Calculates rebellion initiation probability
- Spawns rebellion entity when threshold crossed

**RebellionRecruitmentSystem**:
- Coordinates rebel recruitment campaigns
- Prioritizes Safe tier recruits, expands to Risky when needed
- Tracks informant encounters (conspiracy exposure risk)

**RebellionEscalationSystem**:
- Updates escalation level (peaceful to violent)
- Processes violent events, negotiations, casualties
- Determines rebellion type (Violent, Peaceful, Coup, Secession)

**RebellionOutcomeSystem**:
- Calculates victory probability (rebels vs overlord)
- Determines negotiation probability (stalemate conditions)
- Resolves rebellion (victory, defeat, settlement, martyrdom)

**NeutralPunishmentSystem**:
- Calculates punishment severity from both sides
- Forces neutrals to choose sides when resources exhausted
- Handles neutral survival strategies (bribery, flee, hide)

**CounterIntelligenceSystem**:
- Detects infiltrators (overlord spies in rebellion)
- Identifies informants (false information tests)
- Handles spy elimination (execution, turning, exile)

---

## Key Design Principles

1. **Three-Faction Dynamics**: Every rebellion divides population (loyalists, rebels, neutrals)
2. **Informant Risk Tiering**: Rebels recruit cautiously (safe → risky → dangerous, never suicidal)
3. **Loyalty Is Volatile**: Events shift factions (atrocities, victories, betrayals, payments)
4. **Neutrality Has Costs**: Both sides punish neutrals (forced choice or resource exhaustion)
5. **Escalation Is Gradual**: Rebellions start peaceful, escalate to violence (or de-escalate to negotiation)
6. **Outcomes Vary**: Total victory, total defeat, negotiated settlement, preemption, martyrdom all possible
7. **Information Warfare**: Spies, informants, false information tests critical to both sides
8. **Martyrdom Endures**: Defeated rebels inspire future rebellions if executed cruelly
9. **Stalemate Enables Negotiation**: Neither side wins decisively → compromise
10. **Time Matters**: Long rebellions favor rebels (overlord exhaustion), but exposure risk for conspiracies

---

## Integration with Other Systems

- **Aggregate Politics**: Low cohesion (<30%) triggers rebellion initiation
- **Infiltration Detection**: Overlord uses detection systems to find conspirators
- **Crisis Alert States**: External threats reduce rebellion (+20% loyalty rally) or increase (blame overlord -30%)
- **Soul System**: Dead rebel leaders' souls transferred, continue rebellion posthumously
- **Blueprint System**: Rebels capture weapon designs, improve militia equipment
