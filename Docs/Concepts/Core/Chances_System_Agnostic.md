# Chances System - Agnostic Framework

## Overview

The **Chances System** provides mathematical algorithms for opportunity recognition, risk evaluation, and reward calculation. This framework is setting-agnostic and can be applied to fantasy (Godgame), sci-fi (Space4X), or any DOTS-based game requiring emergent decision-making mechanics.

---

## Core Algorithms

### 1. Perception Check Algorithm

Determines whether an entity recognizes that an opportunity exists.

```csharp
public static bool PerformPerceptionCheck(
    float perceptionSkill,
    float professionModifier,
    float circumstanceModifier,
    float difficulty,
    ref Unity.Mathematics.Random random)
{
    float roll = random.NextFloat(0f, 100f);
    float totalBonus = (perceptionSkill * 0.5f) + professionModifier + circumstanceModifier;
    float totalRoll = roll + totalBonus;

    bool success = totalRoll >= difficulty;
    bool criticalSuccess = roll >= 95f;
    bool criticalFailure = roll <= 5f;

    if (criticalSuccess)
    {
        // Entity identifies additional opportunities or bonus information
        return true;
    }
    else if (criticalFailure)
    {
        // Entity misidentifies danger as opportunity (can lead to trap)
        return false;
    }

    return success;
}
```

**Formula:**
```
Perception Roll = Random(0, 100) + (Perception Skill × 0.5) + Profession Modifier + Circumstance Modifier

Success: Roll ≥ Difficulty
Critical Success: Raw roll ≥ 95
Critical Failure: Raw roll ≤ 5
```

**Profession Modifiers:**
```csharp
public static float GetProfessionModifier(ProfessionType profession)
{
    return profession switch
    {
        ProfessionType.Spy => 20f,
        ProfessionType.Assassin => 20f,
        ProfessionType.Scout => 15f,
        ProfessionType.Warrior => 10f,
        ProfessionType.Guard => 10f,
        ProfessionType.Mage => 5f,
        ProfessionType.Scholar => 5f,
        ProfessionType.Merchant => 5f,
        ProfessionType.Diplomat => 5f,
        ProfessionType.Civilian => 0f,
        ProfessionType.Laborer => 0f,
        _ => 0f
    };
}
```

**Circumstance Modifiers:**
```csharp
public static float CalculateCircumstanceModifier(
    bool isDesperate,
    bool isDistracted,
    bool hasKeenSenses,
    bool hasDullSenses,
    float intelligence,
    float wisdom)
{
    float modifier = 0f;

    if (isDesperate) modifier -= 15f;          // Heightened awareness in danger
    if (isDistracted) modifier += 15f;         // Wounded, fatigued
    if (hasKeenSenses) modifier -= 15f;        // Trait bonus
    if (hasDullSenses) modifier += 15f;        // Trait penalty
    if (intelligence >= 140f) modifier -= 5f;  // High INT helps
    if (wisdom >= 140f) modifier -= 10f;       // High WIS helps more

    return modifier;
}
```

### 2. Risk Evaluation Algorithm

Calculates expected value and compares against risk tolerance threshold.

```csharp
public static float CalculateExpectedValue(
    float successProbability,
    float rewardValue,
    float failureProbability,
    float consequenceSeverity)
{
    float expectedReward = successProbability * rewardValue;
    float expectedCost = failureProbability * consequenceSeverity;
    float expectedValue = expectedReward - expectedCost;

    return expectedValue;
}

public static float CalculateRiskToleranceThreshold(
    PersonalityType personality,
    float desperationFactor)
{
    float baseThreshold = personality switch
    {
        PersonalityType.Reckless => 5f,
        PersonalityType.Bold => 10f,
        PersonalityType.Balanced => 30f,
        PersonalityType.Cautious => 60f,
        PersonalityType.Paranoid => 90f,
        _ => 30f
    };

    float adjustedThreshold = baseThreshold * desperationFactor;
    return adjustedThreshold;
}

public static float CalculateDesperationFactor(float threatLevel)
{
    // threatLevel: 0-100 (0 = safe, 100 = immediate death)
    float desperationFactor = math.max(0.05f, 1f - (threatLevel / 100f));
    return desperationFactor;
}

public static bool ShouldAttemptChance(
    float expectedValue,
    float riskToleranceThreshold)
{
    return expectedValue > riskToleranceThreshold;
}
```

**Complete Decision Function:**
```csharp
public static bool EvaluateChanceAttempt(
    float successProbability,
    float rewardValue,
    float consequenceSeverity,
    PersonalityType personality,
    float threatLevel)
{
    float failureProbability = 1f - successProbability;
    float expectedValue = CalculateExpectedValue(
        successProbability,
        rewardValue,
        failureProbability,
        consequenceSeverity);

    float desperationFactor = CalculateDesperationFactor(threatLevel);
    float threshold = CalculateRiskToleranceThreshold(personality, desperationFactor);

    bool shouldAttempt = expectedValue > threshold;
    return shouldAttempt;
}
```

**Example:**
```
Desertion during raid:
- Success: 70%, Reward: 90
- Failure: 30%, Consequence: 80
- Expected Value = (0.7 × 90) - (0.3 × 80) = 63 - 24 = 39

Personality: Cautious (base threshold 60)
Threat Level: 90 (death threat)
Desperation Factor: 1 - 0.9 = 0.1
Adjusted Threshold: 60 × 0.1 = 6

39 > 6 → ATTEMPT CHANCE
```

### 3. Focus Investment Algorithm

Calculates success probability bonus from additional focus investment.

```csharp
public static float CalculateFocusCost(
    ChanceType chanceType,
    float difficulty,
    FocusLevel focusLevel)
{
    float baseCost = chanceType switch
    {
        ChanceType.MiniCritical => 5f,
        ChanceType.SecretLearning => 20f,
        ChanceType.Desertion => 30f,
        ChanceType.MotiveReading => 20f,
        ChanceType.Escape => 25f,
        ChanceType.SpellLearning => 50f,
        _ => 20f
    };

    float difficultyModifier = (difficulty - 50f) * 0.5f;
    float totalBaseCost = baseCost + difficultyModifier;

    float multiplier = focusLevel switch
    {
        FocusLevel.Minimum => 1f,
        FocusLevel.Optimal => 1.3f,
        FocusLevel.Maximum => 1.5f,
        _ => 1f
    };

    float finalCost = totalBaseCost * multiplier;
    return math.max(5f, finalCost);
}

public static float CalculateSuccessProbabilityBonus(
    float focusInvested,
    float minimumFocus)
{
    float extraFocus = focusInvested - minimumFocus;
    if (extraFocus <= 0f) return 0f;

    // +2% per 5 extra focus, max +20%
    float bonusPercent = (extraFocus / 5f) * 2f;
    bonusPercent = math.min(20f, bonusPercent);

    return bonusPercent;
}

public static float CalculateTotalSuccessProbability(
    float baseSuccessProb,
    float focusBonus,
    float skillModifier,
    float environmentModifier)
{
    float totalProb = baseSuccessProb + focusBonus + skillModifier + environmentModifier;
    totalProb = math.clamp(totalProb, 5f, 95f); // Always 5-95% range
    return totalProb;
}
```

**Example:**
```csharp
// Spell learning example
float baseCost = CalculateFocusCost(ChanceType.SpellLearning, 80f, FocusLevel.Optimal);
// baseCost = 50 + (80-50)*0.5 * 1.3 = 50 + 15 * 1.3 = 69.5 focus

float focusInvested = 75f;
float bonus = CalculateSuccessProbabilityBonus(75f, 50f);
// bonus = (75-50)/5 * 2 = 5 * 2 = +10%

float totalProb = CalculateTotalSuccessProbability(20f, 10f, 10f, 0f);
// totalProb = 20 + 10 + 10 + 0 = 40%
```

### 4. Success Roll Algorithm

Determines outcome of chance attempt.

```csharp
public struct ChanceAttemptResult
{
    public OutcomeType Outcome;
    public float EffectivenessPercent; // 0-100% (partial success effectiveness)
    public bool IsRewardEligible;
}

public enum OutcomeType : byte
{
    CriticalFailure,  // 5% of failures
    Failure,          // 95% of failures
    PartialSuccess,   // 30% of successes
    Success,          // 60% of successes
    CriticalSuccess   // 10% of successes
}

public static ChanceAttemptResult PerformSuccessRoll(
    float totalSuccessProbability,
    ref Unity.Mathematics.Random random)
{
    float roll = random.NextFloat(0f, 100f);
    bool success = roll <= totalSuccessProbability;

    ChanceAttemptResult result = new ChanceAttemptResult();

    if (success)
    {
        // Success - determine quality
        float qualityRoll = random.NextFloat(0f, 100f);

        if (qualityRoll <= 10f)
        {
            // Critical success (10% of successes)
            result.Outcome = OutcomeType.CriticalSuccess;
            result.EffectivenessPercent = 120f;
            result.IsRewardEligible = true;
        }
        else if (qualityRoll <= 40f)
        {
            // Partial success (30% of successes)
            result.Outcome = OutcomeType.PartialSuccess;
            result.EffectivenessPercent = 60f;
            result.IsRewardEligible = true;
        }
        else
        {
            // Normal success (60% of successes)
            result.Outcome = OutcomeType.Success;
            result.EffectivenessPercent = 100f;
            result.IsRewardEligible = true;
        }
    }
    else
    {
        // Failure - determine severity
        float severityRoll = random.NextFloat(0f, 100f);

        if (severityRoll <= 5f)
        {
            // Critical failure (5% of failures)
            result.Outcome = OutcomeType.CriticalFailure;
            result.EffectivenessPercent = -20f; // Active harm
            result.IsRewardEligible = false;
        }
        else
        {
            // Normal failure (95% of failures)
            result.Outcome = OutcomeType.Failure;
            result.EffectivenessPercent = 0f;
            result.IsRewardEligible = false;
        }
    }

    return result;
}
```

**Outcome Probability Distribution:**
```
Given 60% success probability:

Critical Success: 60% × 10% = 6%
Success:          60% × 60% = 36%
Partial Success:  60% × 30% = 18%
Failure:          40% × 95% = 38%
Critical Failure: 40% × 5% = 2%
```

### 5. Rewards Calculation Algorithm

Calculates experience bonuses and wisdom gains.

```csharp
public struct ChanceReward
{
    public float ExperienceBonusPercent; // 15-50%
    public float BonusDurationSeconds;   // 600-1800 seconds (10-30 minutes)
    public int WisdomGained;             // 0-5 points
    public ArchetypeType Archetype;      // Physical, Finesse, Will
}

public static ChanceReward CalculateRewards(
    float difficulty,
    OutcomeType outcome,
    ChanceType chanceType)
{
    ChanceReward reward = new ChanceReward();

    // Determine archetype based on chance type
    reward.Archetype = chanceType switch
    {
        ChanceType.MiniCritical => ArchetypeType.Physical,
        ChanceType.Desertion => ArchetypeType.Finesse,
        ChanceType.SecretLearning => ArchetypeType.Finesse,
        ChanceType.Escape => ArchetypeType.Finesse,
        ChanceType.MotiveReading => ArchetypeType.Will,
        ChanceType.SpellLearning => ArchetypeType.Will,
        _ => ArchetypeType.Physical
    };

    // Only reward on success outcomes
    if (outcome == OutcomeType.Failure || outcome == OutcomeType.CriticalFailure)
    {
        reward.ExperienceBonusPercent = 0f;
        reward.BonusDurationSeconds = 0f;
        reward.WisdomGained = 0;
        return reward;
    }

    // Calculate XP bonus magnitude
    float baseBonusPercent = 15f + (difficulty * 0.4f);

    // Modify by outcome quality
    float outcomeMultiplier = outcome switch
    {
        OutcomeType.PartialSuccess => 0.7f,
        OutcomeType.Success => 1.0f,
        OutcomeType.CriticalSuccess => 1.5f,
        _ => 1.0f
    };

    reward.ExperienceBonusPercent = baseBonusPercent * outcomeMultiplier;
    reward.ExperienceBonusPercent = math.clamp(reward.ExperienceBonusPercent, 15f, 75f);

    // Calculate duration
    float baseDurationSeconds = 600f + (difficulty * 9f); // 10 min + difficulty scaling
    reward.BonusDurationSeconds = baseDurationSeconds;

    // Calculate wisdom gain
    int baseWisdom = 1 + (int)math.floor(difficulty / 30f);
    int outcomeBonus = outcome switch
    {
        OutcomeType.PartialSuccess => 0,
        OutcomeType.Success => 0,
        OutcomeType.CriticalSuccess => 2,
        _ => 0
    };

    reward.WisdomGained = baseWisdom + outcomeBonus;
    reward.WisdomGained = math.clamp(reward.WisdomGained, 0, 5);

    return reward;
}
```

**Reward Formulas:**
```
Experience Bonus = (15% + Difficulty × 0.4%) × Outcome Multiplier
Duration = 600 seconds + (Difficulty × 9 seconds)
Wisdom = 1 + floor(Difficulty / 30) + Outcome Bonus

Outcome Multipliers:
- Partial Success: 0.7×
- Success: 1.0×
- Critical Success: 1.5×

Outcome Wisdom Bonus:
- Partial Success: +0
- Success: +0
- Critical Success: +2
```

**Example Calculations:**
```
Hard chance (DC 80), Critical Success:
- XP Bonus = (15 + 80×0.4) × 1.5 = (15 + 32) × 1.5 = 47 × 1.5 = 70.5%
- Duration = 600 + (80×9) = 600 + 720 = 1320 seconds (22 minutes)
- Wisdom = 1 + floor(80/30) + 2 = 1 + 2 + 2 = 5

Moderate chance (DC 60), Partial Success:
- XP Bonus = (15 + 60×0.4) × 0.7 = (15 + 24) × 0.7 = 39 × 0.7 = 27.3%
- Duration = 600 + (60×9) = 600 + 540 = 1140 seconds (19 minutes)
- Wisdom = 1 + floor(60/30) + 0 = 1 + 2 + 0 = 3
```

### 6. Motive Reading Algorithm

Handles mutual recognition and alliance formation.

```csharp
public struct MotiveReadingResult
{
    public bool SuccessfulRead;
    public MotiveType DetectedMotive;
    public BehaviorProfile DetectedProfile;
    public bool MutualRecognition;
    public float AllianceProbability;
    public float AllianceDurationHours;
}

public static MotiveReadingResult PerformMotiveReading(
    Entity reader,
    Entity target,
    float readerFocus,
    float targetFocus,
    BehaviorProfile readerProfile,
    BehaviorProfile targetProfile,
    float readerPerceptionSkill,
    float targetPerceptionSkill,
    float difficulty,
    ref Unity.Mathematics.Random random)
{
    MotiveReadingResult result = new MotiveReadingResult();

    // Reader's perception check
    bool readerSuccess = PerformPerceptionCheck(
        readerPerceptionSkill,
        0f, 0f, difficulty, ref random);

    // Target's perception check (if investing focus)
    bool targetSuccess = false;
    if (targetFocus >= 20f)
    {
        targetSuccess = PerformPerceptionCheck(
            targetPerceptionSkill,
            0f, 0f, difficulty, ref random);
    }

    // Determine reading depth based on focus
    ReadingDepth readerDepth = GetReadingDepth(readerFocus);

    if (readerSuccess)
    {
        result.SuccessfulRead = true;
        result.DetectedProfile = targetProfile;
        result.DetectedMotive = DetermineMotiveFromProfile(targetProfile, ref random);

        // Check for mutual recognition
        if (targetSuccess && targetFocus >= 20f)
        {
            result.MutualRecognition = true;

            // Calculate alliance probability based on profile compatibility
            result.AllianceProbability = CalculateAllianceProbability(
                readerProfile, targetProfile);

            // Duration based on profiles and circumstances
            result.AllianceDurationHours = CalculateAllianceDuration(
                readerProfile, targetProfile, ref random);
        }
    }
    else
    {
        result.SuccessfulRead = false;
    }

    return result;
}

public enum ReadingDepth : byte
{
    Surface,  // 20-30 focus: Hostile/Neutral/Friendly
    Moderate, // 31-40 focus: Specific goals, loyalties
    Deep      // 41-50 focus: Fears, weaknesses, desperation
}

public static ReadingDepth GetReadingDepth(float focus)
{
    if (focus < 31f) return ReadingDepth.Surface;
    if (focus < 41f) return ReadingDepth.Moderate;
    return ReadingDepth.Deep;
}

public static float CalculateAllianceProbability(
    BehaviorProfile profile1,
    BehaviorProfile profile2)
{
    // Similar profiles have higher alliance probability
    if (profile1 == profile2)
    {
        return profile1 switch
        {
            BehaviorProfile.Peaceful => 0.85f,  // Peaceful + Peaceful = high trust
            BehaviorProfile.Neutral => 0.65f,   // Neutral + Neutral = moderate trust
            BehaviorProfile.Warlike => 0.45f,   // Warlike + Warlike = mutual respect
            _ => 0.5f
        };
    }

    // Adjacent profiles have moderate probability
    if ((profile1 == BehaviorProfile.Peaceful && profile2 == BehaviorProfile.Neutral) ||
        (profile1 == BehaviorProfile.Neutral && profile2 == BehaviorProfile.Peaceful))
    {
        return 0.70f;
    }

    if ((profile1 == BehaviorProfile.Neutral && profile2 == BehaviorProfile.Warlike) ||
        (profile1 == BehaviorProfile.Warlike && profile2 == BehaviorProfile.Neutral))
    {
        return 0.40f;
    }

    // Opposing profiles have low probability
    // Peaceful + Warlike = unlikely alliance
    return 0.15f;
}

public static float CalculateAllianceDuration(
    BehaviorProfile profile1,
    BehaviorProfile profile2,
    ref Unity.Mathematics.Random random)
{
    float baseDurationHours = profile1 switch
    {
        BehaviorProfile.Peaceful => random.NextFloat(6f, 24f),   // Longer alliances
        BehaviorProfile.Neutral => random.NextFloat(3f, 12f),    // Medium alliances
        BehaviorProfile.Warlike => random.NextFloat(1f, 6f),     // Short alliances
        _ => 3f
    };

    // Reduce duration if profiles don't match
    if (profile1 != profile2)
    {
        baseDurationHours *= 0.6f;
    }

    return baseDurationHours;
}

public static MotiveType DetermineMotiveFromProfile(
    BehaviorProfile profile,
    ref Unity.Mathematics.Random random)
{
    return profile switch
    {
        BehaviorProfile.Peaceful => random.NextBool() ? MotiveType.Friendly : MotiveType.Neutral,
        BehaviorProfile.Neutral => MotiveType.Survival,
        BehaviorProfile.Warlike => random.NextBool() ? MotiveType.Hostile : MotiveType.HonorBound,
        _ => MotiveType.Neutral
    };
}
```

**Alliance Probability Matrix:**
```
              Peaceful  Neutral  Warlike
Peaceful        85%      70%      15%
Neutral         70%      65%      40%
Warlike         15%      40%      45%
```

### 7. Mini-Critical Algorithm

Handles warrior combat mini-crits.

```csharp
public struct MiniCriticalResult
{
    public bool IsAttempted;
    public bool IsCriticalHit;
    public float DamageMultiplier;
    public float FocusConsumed;
}

public static MiniCriticalResult AttemptMiniCritical(
    float baseCritChance,
    float focusInvested,
    float normalDamage,
    ref Unity.Mathematics.Random random)
{
    MiniCriticalResult result = new MiniCriticalResult();

    if (focusInvested < 5f)
    {
        result.IsAttempted = false;
        return result;
    }

    result.IsAttempted = true;
    result.FocusConsumed = math.min(15f, focusInvested);

    // Calculate bonus crit chance
    float bonusCritChance = (result.FocusConsumed / 5f) * 5f; // +5% per 5 focus
    float totalCritChance = baseCritChance + bonusCritChance;
    totalCritChance = math.min(60f, totalCritChance); // Cap at 60%

    // Roll for crit
    float roll = random.NextFloat(0f, 100f);
    result.IsCriticalHit = roll <= totalCritChance;

    if (result.IsCriticalHit)
    {
        // Mini-crit multiplier: 1.6x to 1.8x (less than full crit)
        // More focus investment = higher multiplier
        float multiplierBonus = (result.FocusConsumed / 15f) * 0.2f;
        result.DamageMultiplier = 1.6f + multiplierBonus;
        result.DamageMultiplier = math.min(1.8f, result.DamageMultiplier);
    }
    else
    {
        result.DamageMultiplier = 1.0f; // Normal damage
    }

    return result;
}
```

**Formula:**
```
Bonus Crit Chance = (Focus Invested / 5) × 5%
Total Crit Chance = Base Crit Chance + Bonus (max 60%)

Mini-Crit Multiplier = 1.6 + (Focus Invested / 15) × 0.2
Range: 1.6× (5 focus) to 1.8× (15 focus)

Example:
- Base crit: 15%
- Focus: 10
- Total crit: 15% + 10% = 25%
- Multiplier: 1.6 + (10/15)×0.2 = 1.73×
```

### 8. Spell Learning Algorithm

Handles learning spells through observation.

```csharp
public struct SpellLearningAttempt
{
    public bool LearningSuccessful;
    public float SpellEffectiveness;  // 70-100%
    public int PracticeRequired;      // 0-20 practice castings to master
    public bool BacklashOccurred;     // Critical failure consequence
    public float BacklashDamage;      // 1-6 damage on backlash
}

public static SpellLearningAttempt AttemptSpellLearning(
    float intelligence,
    int observationCount,
    int spellTierDifference,
    bool hasSimilarSchool,
    bool hasSpellThiefFeat,
    float difficulty,
    ref Unity.Mathematics.Random random)
{
    SpellLearningAttempt result = new SpellLearningAttempt();

    // Calculate success probability
    float baseChance = random.NextFloat(15f, 25f);

    // Intelligence bonus
    float intBonus = intelligence >= 140f ? 10f : 0f;

    // Observation bonus (stacking)
    float observationBonus = observationCount * 5f;
    observationBonus = math.min(30f, observationBonus); // Cap at +30%

    // Similar school bonus
    float schoolBonus = hasSimilarSchool ? 10f : 0f;

    // Spell Thief feat bonus
    float featBonus = hasSpellThiefFeat ? 15f : 0f;

    // Tier difference penalty
    float tierPenalty = spellTierDifference > 0 ? spellTierDifference * 10f : 0f;

    float totalChance = baseChance + intBonus + observationBonus + schoolBonus + featBonus - tierPenalty;
    totalChance = math.clamp(totalChance, 5f, 85f);

    // Success roll
    float roll = random.NextFloat(0f, 100f);
    bool success = roll <= totalChance;

    if (success)
    {
        // Success - determine spell effectiveness
        float criticalThreshold = totalChance * 0.15f; // 15% of success range
        bool criticalSuccess = roll <= criticalThreshold;

        if (criticalSuccess)
        {
            // Perfect learning
            result.LearningSuccessful = true;
            result.SpellEffectiveness = 1.0f;
            result.PracticeRequired = 0;
        }
        else
        {
            // Incomplete learning
            result.LearningSuccessful = true;
            result.SpellEffectiveness = random.NextFloat(0.7f, 0.85f);
            result.PracticeRequired = (int)random.NextInt(8, 16);
        }
    }
    else
    {
        // Failure - check for critical failure
        float criticalFailureThreshold = 5f;
        bool criticalFailure = roll >= (100f - criticalFailureThreshold);

        if (criticalFailure)
        {
            result.BacklashOccurred = true;
            result.BacklashDamage = random.NextFloat(1f, 6f);
        }

        result.LearningSuccessful = false;
    }

    return result;
}

public static bool AttemptSpellMastery(
    int practiceCount,
    int practiceRequired,
    ref Unity.Mathematics.Random random)
{
    if (practiceCount < practiceRequired) return false;

    // 15% chance per practice session beyond required
    float masteryChance = 15f * (practiceCount - practiceRequired + 1);
    masteryChance = math.min(95f, masteryChance);

    float roll = random.NextFloat(0f, 100f);
    return roll <= masteryChance;
}
```

**Success Probability Formula:**
```
Success Chance = Base (15-25%)
                 + INT Bonus (0-10%)
                 + Observations × 5% (max +30%)
                 + Similar School (0-10%)
                 + Spell Thief Feat (0-15%)
                 - Tier Difference × 10%

Clamped: 5-85%

Effectiveness on Success:
- Critical Success (15% of successes): 100% effectiveness, 0 practice
- Normal Success (85% of successes): 70-85% effectiveness, 8-16 practice sessions

Critical Failure: 5% of failures, 1-6 magical backlash damage
```

### 9. Desertion Decision Algorithm

AI decision-making for group desertion.

```csharp
public static bool DecideDesertion(
    float survivalProbabilityIfStay,
    float survivalProbabilityIfLeave,
    float loyaltyToGroup,
    float alignmentWithAttackers,
    float groupPursuitCapability,
    PersonalityType personality,
    float currentThreatLevel)
{
    // Calculate reward value (survival gain)
    float rewardValue = (survivalProbabilityIfLeave - survivalProbabilityIfStay) * 100f;

    // Modify by alignment with attackers
    rewardValue += alignmentWithAttackers;

    // Calculate consequence severity
    float consequenceSeverity = groupPursuitCapability * 0.8f;

    // Loyalty modifier reduces reward value
    float loyaltyPenalty = loyaltyToGroup * 0.5f;
    float adjustedReward = math.max(0f, rewardValue - loyaltyPenalty);

    // Calculate success probability (depends on chaos level)
    float chaosLevel = currentThreatLevel / 100f;
    float successProbability = 0.4f + (chaosLevel * 0.5f); // 40-90% based on chaos

    // Evaluate using standard risk evaluation
    bool shouldAttempt = EvaluateChanceAttempt(
        successProbability,
        adjustedReward,
        consequenceSeverity,
        personality,
        currentThreatLevel);

    return shouldAttempt;
}
```

**Example:**
```csharp
// Cult member under death threat
bool shouldDesert = DecideDesertion(
    survivalIfStay: 0.25f,         // 25% survival if stays
    survivalIfLeave: 0.70f,        // 70% survival if deserts
    loyaltyToGroup: 20f,           // Low loyalty (0-100)
    alignmentWithAttackers: 30f,   // Attackers are honorable guards
    groupPursuitCapability: 60f,   // Cult moderately organized
    personality: PersonalityType.Balanced,
    currentThreatLevel: 90f        // Imminent death

// Calculation:
// rewardValue = (0.7 - 0.25) × 100 = 45
// rewardValue += 30 = 75
// consequenceSeverity = 60 × 0.8 = 48
// loyaltyPenalty = 20 × 0.5 = 10
// adjustedReward = 75 - 10 = 65
// successProbability = 0.4 + (90/100) × 0.5 = 0.85 (85%)
// expectedValue = (0.85 × 65) - (0.15 × 48) = 55.25 - 7.2 = 48.05
// threshold = 30 × (1 - 0.9) = 3
// 48.05 > 3 → DESERTION APPROVED
```

### 10. AI Decision Tree

Complete decision flow for NPCs.

```csharp
public static void ProcessChanceOpportunity(
    Entity entity,
    ChanceOpportunity opportunity,
    ref ChancePerception perception,
    ref ChanceEvaluation evaluation,
    EntityManager em,
    ref Unity.Mathematics.Random random)
{
    // Step 1: Perception check
    float circumstanceMod = CalculateCircumstanceModifier(
        evaluation.CurrentThreatLevel > 70f, // isDesperate
        false, // isDistracted (would need wound/fatigue check)
        false, // hasKeenSenses (would check traits)
        false, // hasDullSenses
        100f,  // intelligence (would read from stats)
        100f); // wisdom

    bool perceived = PerformPerceptionCheck(
        perception.PerceptionSkill,
        perception.ProfessionModifier,
        circumstanceMod,
        opportunity.PerceptionDifficulty,
        ref random);

    if (!perceived)
    {
        // Entity doesn't recognize opportunity exists
        return;
    }

    // Step 2: Risk evaluation
    float failureProb = 1f - opportunity.SuccessProbability;
    evaluation.ExpectedValue = CalculateExpectedValue(
        opportunity.SuccessProbability,
        opportunity.RewardValue,
        failureProb,
        opportunity.ConsequenceSeverity);

    evaluation.DesperationFactor = CalculateDesperationFactor(evaluation.CurrentThreatLevel);
    evaluation.RiskToleranceThreshold = CalculateRiskToleranceThreshold(
        evaluation.Personality,
        evaluation.DesperationFactor);

    evaluation.WillAttempt = evaluation.ExpectedValue > evaluation.RiskToleranceThreshold;

    if (!evaluation.WillAttempt)
    {
        // Risk too high for personality/desperation level
        return;
    }

    // Step 3: Focus availability check
    var focusComponent = em.GetComponentData<FocusResource>(entity);
    if (focusComponent.CurrentFocus < opportunity.MinFocusCost)
    {
        // Not enough focus to attempt
        return;
    }

    // Step 4: Create attempt
    ChanceAttempt attempt = new ChanceAttempt
    {
        Type = opportunity.Type,
        Target = opportunity.Trigger,
        FocusInvested = math.min(focusComponent.CurrentFocus, opportunity.OptimalFocusCost),
        BonusSuccessChance = CalculateSuccessProbabilityBonus(
            opportunity.OptimalFocusCost,
            opportunity.MinFocusCost),
        TotalSuccessChance = opportunity.SuccessProbability,
        AttemptStartTime = (float)SystemAPI.Time.ElapsedTime,
        AttemptDuration = 1f, // 1 second attempt
        State = AttemptState.InProgress
    };

    em.AddComponentData(entity, attempt);

    // Consume focus
    focusComponent.CurrentFocus -= attempt.FocusInvested;
    em.SetComponentData(entity, focusComponent);
}
```

---

## Complete Workflow Example

### Warrior Mini-Critical Scenario

```csharp
// Setup
Entity warrior = /* ... */;
float baseCritChance = 15f;
float normalDamage = 14f;
float focusAvailable = 50f;

// Step 1: Recognize opportunity (perception check)
Unity.Mathematics.Random random = new Unity.Mathematics.Random(12345);
bool opportunityRecognized = PerformPerceptionCheck(
    perceptionSkill: 60f,
    professionModifier: 10f, // Warrior
    circumstanceModifier: 0f,
    difficulty: 40f,
    ref random);

if (!opportunityRecognized)
{
    Debug.Log("Warrior didn't notice opening");
    return;
}

// Step 2: Decide to invest focus (always attempt for warriors in combat)
float focusToInvest = 10f;

// Step 3: Attempt mini-critical
MiniCriticalResult result = AttemptMiniCritical(
    baseCritChance,
    focusToInvest,
    normalDamage,
    ref random);

// Step 4: Apply damage
if (result.IsCriticalHit)
{
    float finalDamage = normalDamage * result.DamageMultiplier;
    Debug.Log($"MINI-CRIT! {finalDamage} damage (×{result.DamageMultiplier})");

    // Step 5: Apply rewards
    ChanceReward reward = CalculateRewards(40f, OutcomeType.Success, ChanceType.MiniCritical);
    Debug.Log($"XP Bonus: +{reward.ExperienceBonusPercent}% for {reward.BonusDurationSeconds}s");
    Debug.Log($"Wisdom: +{reward.WisdomGained}");
}
else
{
    Debug.Log($"Normal hit: {normalDamage} damage");
}

// Output example:
// "Warrior didn't notice opening" OR
// "MINI-CRIT! 24.2 damage (×1.73)"
// "XP Bonus: +30% for 960s"
// "Wisdom: +2"
```

---

## Integration with Three Pillar ECS

### Body Pillar (60 Hz)

High-frequency systems:

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PerceptionSystemGroup))]
public partial struct ChancePerceptionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (perception, chanceOpportunities, entity)
                 in SystemAPI.Query<RefRW<ChancePerception>, DynamicBuffer<ChanceOpportunity>>()
                     .WithEntityAccess())
        {
            perception.ValueRW.LastCheckTime += deltaTime;

            // Check cooldown (1-5 seconds)
            if (perception.ValueRO.LastCheckTime < perception.ValueRO.CheckCooldown)
                continue;

            perception.ValueRW.LastCheckTime = 0f;

            // Process opportunities in buffer
            for (int i = chanceOpportunities.Length - 1; i >= 0; i--)
            {
                var opportunity = chanceOpportunities[i];
                opportunity.TimeRemaining -= deltaTime;

                // Expire old opportunities
                if (opportunity.TimeRemaining <= 0f)
                {
                    chanceOpportunities.RemoveAt(i);
                    continue;
                }

                // Perform perception check (implement full logic)
                // If perceived and evaluated positively, create ChanceAttempt component
            }
        }
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MiniCriticalSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Handle mini-critical attempts during combat
        // Processes at Body pillar frequency (60 Hz) for responsive combat
    }
}
```

### Mind Pillar (1 Hz)

Medium-frequency decision-making:

```csharp
[UpdateInGroup(typeof(MindPillarSystemGroup))]
public partial struct ChanceEvaluationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Risk evaluation and decision-making
        // Runs at 1 Hz for thoughtful consideration

        foreach (var (evaluation, perception, focusResource)
                 in SystemAPI.Query<RefRW<ChanceEvaluation>,
                                     RefRO<ChancePerception>,
                                     RefRO<FocusResource>>())
        {
            // Calculate expected value and compare to risk tolerance
            // Make decisions about attempting chances
        }
    }
}

[UpdateInGroup(typeof(MindPillarSystemGroup))]
public partial struct SpellLearningSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Process spell learning attempts
        // Requires focused concentration over time
    }
}
```

### Aggregate Pillar (0.2 Hz)

Low-frequency long-term effects:

```csharp
[UpdateInGroup(typeof(AggregatePillarSystemGroup))]
public partial struct ChanceRewardsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float currentTime = (float)SystemAPI.Time.ElapsedTime;

        foreach (var (rewards, wisdom) in SystemAPI.Query<RefRW<ChanceRewards>, RefRW<WisdomStat>>())
        {
            if (!rewards.ValueRO.IsActive) continue;

            float elapsedTime = currentTime - rewards.ValueRO.BonusStartTime;

            // Check if bonus expired
            if (elapsedTime >= rewards.ValueRO.BonusDuration)
            {
                rewards.ValueRW.IsActive = false;
                rewards.ValueRW.ExperienceBonus = 0f;
            }
        }
    }
}

[UpdateInGroup(typeof(AggregatePillarSystemGroup))]
public partial struct AllianceManagementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Manage temporary alliances formed through motive reading
        // Check alliance duration expiration
        // Process alliance dissolution or renewal
    }
}
```

---

## Summary

The **Chances System Agnostic Framework** provides:

1. **Perception Check Algorithm**: d100 + modifiers vs DC with profession and circumstance adjustments
2. **Risk Evaluation Algorithm**: Expected value calculation and risk tolerance thresholds
3. **Focus Investment Algorithm**: Diminishing returns success bonuses (+2% per 5 focus, max +20%)
4. **Success Roll Algorithm**: Outcome determination (critical success to critical failure)
5. **Rewards Calculation**: XP bonuses (15-75%), duration (10-30 min), wisdom gains (0-5)
6. **Motive Reading Algorithm**: Mutual recognition, profile matching, alliance probability (15-85%)
7. **Mini-Critical Algorithm**: Combat mini-crits (+5-15% crit chance, 1.6-1.8× damage)
8. **Spell Learning Algorithm**: Observation-based learning (5-85% success, 70-100% effectiveness)
9. **Desertion Decision Algorithm**: Survival analysis and loyalty modifiers
10. **AI Decision Tree**: Complete NPC decision flow from perception to execution

**Core Formula Summary:**
```
Chance System = Perception (recognize)
                + Evaluation (decide)
                + Investment (commit resources)
                + Execution (roll outcome)
                + Rewards (XP + Wisdom)
```

This framework is fully compatible with Unity DOTS and the Three Pillar ECS architecture, with systems distributed across Body (60 Hz), Mind (1 Hz), and Aggregate (0.2 Hz) update frequencies.
