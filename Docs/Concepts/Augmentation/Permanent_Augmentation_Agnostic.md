# Permanent Augmentation System - Agnostic Framework

## Overview

This document provides the **game-agnostic algorithms and ECS components** for implementing permanent augmentation systems where consciousness becomes irreversibly bound to artificial or heavily modified vessels.

The framework supports:
- Prototype augmentation with living test subjects
- Permanent chassis interment mechanics
- Psychological degradation (madness) systems
- Cultural reaction modeling
- Consciousness redemption pathways

---

## Core Components

### 1. Permanent Augment Component

```csharp
using Unity.Entities;
using Unity.Mathematics;

public struct PermanentAugmentComponent : IComponentData
{
    public Entity OriginalBodyReference; // Reference to destroyed/stored biological body
    public Entity CurrentChassis; // Current artificial body
    public float TimeInChassis; // Years since interment
    public bool IsPrototype; // Experimental augment vs refined design
    public AugmentationType Type; // What kind of permanent augment
    public float ChassisQuality; // 0-100, affects madness resistance
    public bool IsReversible; // Almost always false, but some late-game tech may allow
}

public enum AugmentationType
{
    PrototypeExperimental, // Early-stage test augment
    InjuryEmergency,       // Forced by fatal injury
    EnemyForced,           // Environmental/enemy threat requires it
    VoluntaryService,      // Chosen for duty/honor
    Punishment,            // Conscripted criminal/prisoner
    Enhancement            // Elective upgrade for power
}
```

### 2. Madness and Psychological State Component

```csharp
public struct MadnessComponent : IComponentData
{
    public float CurrentMadness; // 0-100
    public float MadnessResistance; // From Willpower, chassis quality
    public float BaseAccumulationRate; // Madness gained per year
    public MadnessStage CurrentStage; // Which stage of madness
    public bool IsUncontrollable; // Lost to madness completely
}

public enum MadnessStage
{
    Stable = 0,          // 0-20: Manageable symptoms
    Troubled = 1,        // 21-40: Identity crisis
    Dissociated = 2,     // 41-60: Severe detachment
    Fragmented = 3,      // 61-80: Personality breakdown
    Mad = 4             // 81-100: Complete insanity
}

public struct PsychologicalModifiers : IComponentData
{
    public bool HasPurpose; // Active mission reduces madness
    public float SocialConnection; // 0-100, interaction with others
    public float CulturalSupport; // -30 to +20, society's view
    public float IdentityStrength; // How well they remember original self
}
```

### 3. Dormancy Component

```csharp
public struct DormancyComponent : IComponentData
{
    public bool CanEnterDormancy; // Chassis supports sleep state
    public bool IsDormant; // Currently asleep
    public float DormancyDuration; // Years spent dormant
    public DormancyTrigger WakeTrigger; // What awakens them
    public float MadnessHealingRate; // Madness reduced per year dormant
    public float3 DormancyLocation; // Where they sleep
}

public enum DormancyTrigger
{
    Manual,              // Awakened by command
    WarDeclaration,      // Faction goes to war
    ThreatThreshold,     // Enemy power exceeds threshold
    SpecificEnemy,       // Particular foe appears
    CalendarEvent,       // Date/holiday/season
    SoulCall,            // Ritual summoning
    ResourceCritical,    // Critical shortage
    AllyDeath           // Specific ally killed
}

public struct DormancyTriggerData : IComponentData
{
    public Entity MonitoredFaction; // For war declaration trigger
    public float ThreatThreshold; // For threat trigger
    public int EnemyTypeID; // For specific enemy trigger
    public int CalendarDay; // For calendar trigger
    public Entity BondedAlly; // For ally death trigger
}
```

### 4. Cultural Reaction Component

```csharp
public struct CulturalReactionComponent : IComponentData
{
    public int HomeCultureID; // Culture that created them
    public CulturalAttitude HomeCultureAttitude; // How their own culture views them
    public float ReputationModifier; // Global reputation change
    public bool IsPersecuted; // Actively hunted by some cultures
    public bool IsVenerated; // Worshipped/honored by some cultures
}

public enum CulturalAttitude
{
    Veneration,     // Honored sacrifice, +20 reputation
    Acceptance,     // Pragmatic acceptance, +5 reputation
    Neutral,        // Mixed feelings, 0 reputation
    Pity,           // Tragic figure, -10 reputation
    Rejection,      // Abomination, -30 reputation, may be exiled
    Persecution     // Hunted for destruction, -50 reputation
}

public struct FactionReputationEntry
{
    public int FactionID;
    public float ReputationModifier; // -50 to +30
    public CulturalAttitude Attitude;
}
```

### 5. Redemption Progress Component

```csharp
public struct RedemptionComponent : IComponentData
{
    public bool IsRedeemable; // Can consciousness be saved
    public RedemptionMethod PreferredMethod; // How they want to be redeemed
    public float RedemptionProgress; // 0-100%
    public float SoulFragmentation; // How damaged soul is
    public int RedemptionCost; // Gold/resources needed
    public bool HasDonorBody; // Clone or donor body available
}

public enum RedemptionMethod
{
    None,                  // No redemption sought
    ChassisUpgrade,        // Transfer to better artificial body
    BiologicalRestoration, // Clone body return
    SoulHealing,           // Repair fragmented consciousness
    CollectiveMerger,      // Join hive mind
    DigitalExistence       // Upload to digital realm
}
```

---

## Core Algorithms

### 1. Prototype Augmentation Success Calculation

```csharp
public static class PermanentAugmentAlgorithms
{
    public static float CalculatePrototypeSuccess(
        int techLevel,
        float subjectWillpower,
        float practitionerSkill,
        float augmentComplexity,
        float subjectAge)
    {
        float techBonus = techLevel * 8f;
        float willBonus = subjectWillpower * 2f;
        float skillBonus = practitionerSkill * 3f;
        float complexityPenalty = augmentComplexity * 2f;
        float agePenalty = subjectAge / 5f;

        float successChance = techBonus + willBonus + skillBonus - complexityPenalty - agePenalty;
        return math.clamp(successChance, 0f, 100f);
    }

    public static AugmentOutcome DetermineOutcome(float successRoll)
    {
        if (successRoll >= 90f) return AugmentOutcome.CriticalSuccess;
        if (successRoll >= 70f) return AugmentOutcome.Success;
        if (successRoll >= 50f) return AugmentOutcome.PartialSuccess;
        if (successRoll >= 30f) return AugmentOutcome.Failure;
        return AugmentOutcome.CriticalFailure;
    }

    public enum AugmentOutcome
    {
        CriticalSuccess,  // Perfect integration, +10 stats, 0 initial madness
        Success,          // Functional, normal stats, 10 initial madness
        PartialSuccess,   // Works but flawed, -10 stats, 25 initial madness
        Failure,          // Malfunctions, -30 stats, 60 initial madness, may need termination
        CriticalFailure   // Death, soul may be trapped in failed augment
    }
}
```

### 2. Madness Progression System

```csharp
public static void UpdateMadnessProgression(
    ref MadnessComponent madness,
    in PsychologicalModifiers psych,
    in PermanentAugmentComponent augment,
    float deltaYears)
{
    // Base madness accumulation from being in artificial body
    float baseMadness = 0.5f * deltaYears;

    // Chassis quality reduces madness gain
    float qualityReduction = (augment.ChassisQuality / 100f) * 0.2f;

    // Purpose and meaning reduce madness
    float purposeReduction = psych.HasPurpose ? 0.3f * deltaYears : 0f;

    // Social connection helps maintain sanity
    float socialReduction = (psych.SocialConnection / 100f) * 0.2f * deltaYears;

    // Cultural support/rejection affects mental health
    float culturalModifier = (psych.CulturalSupport / 100f) * 0.15f * deltaYears;

    // Identity strength helps resist madness
    float identityReduction = (psych.IdentityStrength / 100f) * 0.15f * deltaYears;

    // Calculate total madness gain this period
    float totalReduction = qualityReduction + purposeReduction + socialReduction + identityReduction;
    float rawMadnessGain = baseMadness - totalReduction + culturalModifier;

    // Apply resistance
    float finalMadnessGain = rawMadnessGain * (1f - madness.MadnessResistance);

    // Update madness
    madness.CurrentMadness += math.max(0f, finalMadnessGain);
    madness.CurrentMadness = math.clamp(madness.CurrentMadness, 0f, 100f);

    // Update stage
    madness.CurrentStage = GetMadnessStage(madness.CurrentMadness);

    // Check if uncontrollable
    madness.IsUncontrollable = madness.CurrentMadness >= 85f;
}

public static MadnessStage GetMadnessStage(float madnessValue)
{
    if (madnessValue >= 81f) return MadnessStage.Mad;
    if (madnessValue >= 61f) return MadnessStage.Fragmented;
    if (madnessValue >= 41f) return MadnessStage.Dissociated;
    if (madnessValue >= 21f) return MadnessStage.Troubled;
    return MadnessStage.Stable;
}

public static float GetMadnessPenalty(MadnessStage stage)
{
    switch (stage)
    {
        case MadnessStage.Stable: return 0.05f;      // -5% skills
        case MadnessStage.Troubled: return 0.15f;    // -15% skills
        case MadnessStage.Dissociated: return 0.30f; // -30% skills
        case MadnessStage.Fragmented: return 0.50f;  // -50% skills
        case MadnessStage.Mad: return 1.0f;          // Uncontrollable
        default: return 0f;
    }
}
```

### 3. Dormancy System

```csharp
public static void ProcessDormancy(
    ref MadnessComponent madness,
    ref DormancyComponent dormancy,
    float deltaYears)
{
    if (!dormancy.IsDormant || !dormancy.CanEnterDormancy)
        return;

    // Dormancy heals madness over time
    float madnessHealing = dormancy.MadnessHealingRate * deltaYears;
    madness.CurrentMadness -= madnessHealing;
    madness.CurrentMadness = math.max(0f, madness.CurrentMadness);

    // Track dormancy duration
    dormancy.DormancyDuration += deltaYears;

    // Update madness stage
    madness.CurrentStage = GetMadnessStage(madness.CurrentMadness);
}

public static bool ShouldAwaken(
    in DormancyComponent dormancy,
    in DormancyTriggerData triggerData,
    bool isAtWar,
    float currentThreatLevel,
    int currentEnemyTypeID,
    int currentCalendarDay,
    bool isBondedAllyAlive)
{
    if (!dormancy.IsDormant)
        return false;

    switch (dormancy.WakeTrigger)
    {
        case DormancyTrigger.Manual:
            return false; // Only manual command wakes them

        case DormancyTrigger.WarDeclaration:
            return isAtWar;

        case DormancyTrigger.ThreatThreshold:
            return currentThreatLevel >= triggerData.ThreatThreshold;

        case DormancyTrigger.SpecificEnemy:
            return currentEnemyTypeID == triggerData.EnemyTypeID;

        case DormancyTrigger.CalendarEvent:
            return currentCalendarDay == triggerData.CalendarDay;

        case DormancyTrigger.AllyDeath:
            return !isBondedAllyAlive;

        default:
            return false;
    }
}

public static void EnterDormancy(
    ref DormancyComponent dormancy,
    float3 location,
    float madnessHealingRate = 5f) // 5 madness healed per year by default
{
    if (!dormancy.CanEnterDormancy)
        return;

    dormancy.IsDormant = true;
    dormancy.DormancyLocation = location;
    dormancy.MadnessHealingRate = madnessHealingRate;
}

public static void Awaken(ref DormancyComponent dormancy)
{
    dormancy.IsDormant = false;
}
```

### 4. Cultural Reaction System

```csharp
public static CulturalAttitude DetermineCulturalAttitude(
    int cultureID,
    CultureTraits traits,
    AugmentationType augmentType,
    bool wasVoluntary)
{
    // Religious cultures tend toward veneration or rejection extremes
    if (traits.IsReligious)
    {
        if (traits.VeneratesTransformation)
            return CulturalAttitude.Veneration;
        if (traits.CondemnsBodyModification)
            return CulturalAttitude.Persecution;
    }

    // Military cultures respect voluntary service
    if (traits.IsMilitaristic && wasVoluntary)
        return CulturalAttitude.Veneration;

    // Naturalist cultures reject artificial bodies
    if (traits.IsNaturalist)
        return CulturalAttitude.Rejection;

    // Pragmatic cultures accept necessity
    if (traits.IsPragmatic)
    {
        if (augmentType == AugmentationType.EnemyForced ||
            augmentType == AugmentationType.InjuryEmergency)
            return CulturalAttitude.Acceptance;
        return CulturalAttitude.Neutral;
    }

    // Scholarly cultures find it interesting
    if (traits.IsScholarly)
        return CulturalAttitude.Acceptance;

    // Default to pity for tragic transformation
    return CulturalAttitude.Pity;
}

public struct CultureTraits
{
    public bool IsReligious;
    public bool IsMilitaristic;
    public bool IsNaturalist;
    public bool IsPragmatic;
    public bool IsScholarly;
    public bool VeneratesTransformation;
    public bool CondemnsBodyModification;
}

public static float CalculateReputationModifier(CulturalAttitude attitude)
{
    switch (attitude)
    {
        case CulturalAttitude.Veneration: return 20f;
        case CulturalAttitude.Acceptance: return 5f;
        case CulturalAttitude.Neutral: return 0f;
        case CulturalAttitude.Pity: return -10f;
        case CulturalAttitude.Rejection: return -30f;
        case CulturalAttitude.Persecution: return -50f;
        default: return 0f;
    }
}

public static void UpdatePsychologicalModifiers(
    ref PsychologicalModifiers psych,
    CulturalAttitude homeCultureAttitude)
{
    // Cultural support affects mental health
    psych.CulturalSupport = CalculateReputationModifier(homeCultureAttitude);

    // Veneration provides purpose
    if (homeCultureAttitude == CulturalAttitude.Veneration)
        psych.HasPurpose = true;

    // Persecution damages social connection
    if (homeCultureAttitude == CulturalAttitude.Persecution)
        psych.SocialConnection = math.max(0f, psych.SocialConnection - 30f);
}
```

### 5. Consciousness Redemption System

```csharp
public static float CalculateRedemptionSuccess(
    in SoulComponent soul, // From Soul_System_Agnostic.md
    in RedemptionComponent redemption,
    int techLevel,
    float practitionerSkill,
    float yearsInChassis)
{
    float soulIntegrityBonus = soul.Integrity * 0.5f;
    float techBonus = techLevel * 5f;
    float skillBonus = practitionerSkill * 3f;
    float timePenalty = yearsInChassis * 0.5f;
    float fragmentationPenalty = redemption.SoulFragmentation * 0.3f;

    float successChance = soulIntegrityBonus + techBonus + skillBonus - timePenalty - fragmentationPenalty;
    return math.clamp(successChance, 0f, 100f);
}

public static int CalculateRedemptionCost(
    RedemptionMethod method,
    float soulFragmentation,
    int techLevel)
{
    int baseCost = 0;

    switch (method)
    {
        case RedemptionMethod.ChassisUpgrade:
            baseCost = 50000; // New refined chassis
            break;
        case RedemptionMethod.BiologicalRestoration:
            baseCost = 500000; // Clone body + transfer
            break;
        case RedemptionMethod.SoulHealing:
            baseCost = (int)(10000 * soulFragmentation); // More damaged = more expensive
            break;
        case RedemptionMethod.CollectiveMerger:
            baseCost = 5000; // Relatively cheap
            break;
        case RedemptionMethod.DigitalExistence:
            baseCost = 100000; // Digital infrastructure
            break;
    }

    // Tech level reduces cost (better efficiency)
    float techDiscount = 1f - (techLevel / 100f);
    return (int)(baseCost * techDiscount);
}

public static void ProcessSoulHealing(
    ref MadnessComponent madness,
    ref RedemptionComponent redemption,
    ref SoulComponent soul,
    float practitionerSkill,
    float magicPower,
    float deltaTime)
{
    if (redemption.PreferredMethod != RedemptionMethod.SoulHealing)
        return;

    // Each healing session reduces fragmentation
    float healingAmount = (practitionerSkill * 0.4f + magicPower * 0.3f) * deltaTime;
    redemption.SoulFragmentation -= healingAmount;
    redemption.SoulFragmentation = math.max(0f, redemption.SoulFragmentation);

    // As soul heals, madness reduces
    float madnessReduction = healingAmount * 2f;
    madness.CurrentMadness -= madnessReduction;
    madness.CurrentMadness = math.max(0f, madness.CurrentMadness);

    // Update soul integrity
    soul.Integrity += healingAmount * 0.5f;
    soul.Integrity = math.min(100f, soul.Integrity);

    // Update progress
    redemption.RedemptionProgress = (1f - redemption.SoulFragmentation / 100f) * 100f;
}

public static bool AttemptChassisUpgrade(
    ref PermanentAugmentComponent augment,
    Entity newChassis,
    float newChassisQuality,
    ref MadnessComponent madness)
{
    // Transfer consciousness to better chassis
    Entity oldChassis = augment.CurrentChassis;
    augment.CurrentChassis = newChassis;
    augment.ChassisQuality = newChassisQuality;

    // Better chassis reduces madness accumulation
    float madnessReduction = (newChassisQuality - augment.ChassisQuality) * 0.5f;
    madness.CurrentMadness -= madnessReduction;
    madness.CurrentMadness = math.max(0f, madness.CurrentMadness);

    // Reset time in chassis (fresh start)
    augment.TimeInChassis = 0f;

    return true; // Upgrade successful
}

public static bool AttemptBiologicalRestoration(
    in SoulComponent soul,
    in RedemptionComponent redemption,
    Entity cloneBody,
    float practitionerSkill,
    out float finalIntegrity)
{
    if (!redemption.HasDonorBody)
    {
        finalIntegrity = soul.Integrity;
        return false;
    }

    // Calculate success based on soul integrity and time spent in chassis
    float transferSuccess = (soul.Integrity * 0.8f) + (practitionerSkill * 0.2f);

    bool success = UnityEngine.Random.Range(0f, 100f) < transferSuccess;

    if (success)
    {
        // Successful return to biological form
        finalIntegrity = soul.Integrity * 0.95f; // Minor integrity loss from transfer
        return true;
    }
    else
    {
        // Failed restoration damages soul
        finalIntegrity = soul.Integrity * 0.7f;
        return false;
    }
}
```

---

## System Integration

### Integration with Soul System

Permanent augmentation heavily relies on soul mechanics:

```csharp
// From Soul_System_Agnostic.md
public static bool TransferSoulToPermanentChassis(
    ref SoulComponent soul,
    Entity sourcebody,
    Entity targetChassis,
    float practitionerSkill)
{
    // Calculate vessel compatibility (artificial bodies have lower compatibility)
    float compatibility = CalculateVesselCompatibility(soul, targetChassis);
    compatibility *= 0.6f; // 40% penalty for artificial vessel

    // Calculate transfer success
    float transferChance = (soul.Strength * 0.4f) + (compatibility * 0.3f) + (practitionerSkill * 0.3f);

    bool success = UnityEngine.Random.Range(0f, 100f) < transferChance;

    if (success)
    {
        // Soul successfully transferred but damaged
        soul.Integrity *= 0.85f; // 15% integrity loss from forced transfer
        soul.CurrentVessel = targetChassis;
        return true;
    }
    else
    {
        // Failed transfer severely damages soul
        soul.Integrity *= 0.5f;
        return false;
    }
}
```

### Integration with Mind ECS

Madness affects cognitive functions:

```csharp
public static void ApplyMadnessPenaltiesToMindECS(
    ref CognitionComponent cognition,
    ref LearningComponent learning,
    in MadnessComponent madness)
{
    float penalty = GetMadnessPenalty(madness.CurrentStage);

    // Reduce cognitive effectiveness
    cognition.ProcessingSpeed *= (1f - penalty);
    cognition.FocusDuration *= (1f - penalty);

    // Impair learning
    learning.LearningRate *= (1f - penalty * 0.5f);

    // High madness may prevent certain actions
    if (madness.CurrentStage >= MadnessStage.Fragmented)
    {
        cognition.CanMakeComplexDecisions = false;
        learning.CanLearnNewSkills = false;
    }

    if (madness.IsUncontrollable)
    {
        cognition.IsAutonomous = false; // Cannot control self
    }
}
```

### Integration with Body ECS

Madness affects combat behavior:

```csharp
public static void ApplyMadnessToCombatBehavior(
    ref CombatComponent combat,
    in MadnessComponent madness)
{
    switch (madness.CurrentStage)
    {
        case MadnessStage.Stable:
        case MadnessStage.Troubled:
            // Minimal combat impact
            break;

        case MadnessStage.Dissociated:
            // Increased aggression, reduced defense
            combat.AggressionMultiplier += 0.3f;
            combat.DefenseMultiplier -= 0.2f;
            break;

        case MadnessStage.Fragmented:
            // Erratic behavior, may ignore orders
            combat.FollowsOrders = UnityEngine.Random.Range(0f, 1f) > 0.5f;
            combat.AggressionMultiplier += 0.5f;
            combat.DefenseMultiplier -= 0.3f;
            break;

        case MadnessStage.Mad:
            // Berserker, attacks anything nearby
            combat.AttacksAllies = true;
            combat.FollowsOrders = false;
            combat.AggressionMultiplier += 1.0f;
            combat.DefenseMultiplier = 0f; // No self-preservation
            break;
    }
}
```

---

## ECS Systems

### Madness Progression System (Mind ECS - 1 Hz)

```csharp
[UpdateInGroup(typeof(MindECSGroup))]
public partial struct MadnessProgressionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaYears = SystemAPI.Time.DeltaTime / 31536000f; // Convert seconds to years

        foreach (var (madness, psych, augment) in
            SystemAPI.Query<RefRW<MadnessComponent>, RefRO<PsychologicalModifiers>, RefRO<PermanentAugmentComponent>>())
        {
            if (!augment.ValueRO.IsDormant)
            {
                UpdateMadnessProgression(
                    ref madness.ValueRW,
                    in psych.ValueRO,
                    in augment.ValueRO,
                    deltaYears);
            }
        }
    }
}
```

### Dormancy Management System (Mind ECS - 1 Hz)

```csharp
[UpdateInGroup(typeof(MindECSGroup))]
public partial struct DormancyManagementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaYears = SystemAPI.Time.DeltaTime / 31536000f;

        foreach (var (dormancy, madness, triggerData, entity) in
            SystemAPI.Query<RefRW<DormancyComponent>, RefRW<MadnessComponent>, RefRO<DormancyTriggerData>>()
                .WithEntityAccess())
        {
            if (dormancy.ValueRO.IsDormant)
            {
                // Process dormancy healing
                ProcessDormancy(ref madness.ValueRW, ref dormancy.ValueRW, deltaYears);

                // Check if should awaken
                // Note: These values would come from game state queries
                bool isAtWar = CheckIfAtWar(entity);
                float threatLevel = GetCurrentThreatLevel(entity);
                // ... other trigger checks ...

                if (ShouldAwaken(in dormancy.ValueRO, in triggerData.ValueRO, isAtWar, threatLevel, 0, 0, true))
                {
                    Awaken(ref dormancy.ValueRW);
                }
            }
        }
    }

    private bool CheckIfAtWar(Entity entity)
    {
        // Game-specific implementation
        return false;
    }

    private float GetCurrentThreatLevel(Entity entity)
    {
        // Game-specific implementation
        return 0f;
    }
}
```

### Redemption Processing System (Mind ECS - 1 Hz)

```csharp
[UpdateInGroup(typeof(MindECSGroup))]
public partial struct RedemptionProcessingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (redemption, madness, soul) in
            SystemAPI.Query<RefRW<RedemptionComponent>, RefRW<MadnessComponent>, RefRW<SoulComponent>>())
        {
            if (redemption.ValueRO.IsRedeemable && redemption.ValueRO.PreferredMethod == RedemptionMethod.SoulHealing)
            {
                // Assumes practitioner is available (game-specific)
                float practitionerSkill = 70f; // Would query from game state
                float magicPower = 50f;

                ProcessSoulHealing(
                    ref madness.ValueRW,
                    ref redemption.ValueRW,
                    ref soul.ValueRW,
                    practitionerSkill,
                    magicPower,
                    deltaTime);
            }
        }
    }
}
```

---

## Summary

This agnostic framework provides the mathematical and structural foundation for permanent augmentation systems:

**Core Mechanics:**
1. **Prototype Success**: `(TechLevel × 8) + (Willpower × 2) + (Skill × 3) - (Complexity × 2) - (Age / 5)`
2. **Madness Progression**: Base accumulation modified by chassis quality, purpose, social connection, cultural support
3. **Dormancy Healing**: Madness reduced over time while dormant, awakened by triggers
4. **Cultural Reactions**: Attitudes range from veneration (+20 reputation) to persecution (-50)
5. **Redemption**: Methods include chassis upgrade, biological restoration, soul healing, collective merger

**Integration Points:**
- Soul System: Consciousness transfer to/from chassis
- Mind ECS: Cognitive penalties from madness
- Body ECS: Combat behavior affected by psychological state
- Aggregate ECS: Cultural attitudes tracked over time

This framework allows game implementations to create rich narratives around the cost of augmentation, the nature of identity, and the redemption of lost humanity.
