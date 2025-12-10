# Infiltration Detection System - Agnostic Framework

## Overview

This document provides **game-agnostic algorithms and ECS components** for implementing infiltration detection systems where entities detect breaches through physical evidence, mental scanning, digital traces, and absence detection.

The framework supports:
- Multi-modal detection (physical, mental, digital, absence-based)
- Personality-driven responses (combat, flee, investigate, steal, help)
- Alert escalation and propagation
- Investigation mechanics
- False positive/negative handling

---

## Core Components

### 1. Evidence Components

```csharp
using Unity.Entities;
using Unity.Mathematics;

public struct PhysicalEvidenceComponent : IComponentData
{
    public EvidenceType Type;
    public float3 WorldPosition;
    public float Freshness; // 1.0 = just created, decays to 0
    public float Obviousness; // 0-1, how easy to spot
    public int CreatorID; // Who/what left this evidence
    public float TimeCreated; // Game time when created
}

public enum EvidenceType
{
    Blood,
    Corpse,
    UnconsciousBody,
    SeveredLimb,
    Tracks,
    BrokenObject,
    OpenDoor,
    DisturbedDust,
    ScorchMark,
    HackingTrace,
    MissingItem,
    MissingPerson
}

public struct EvidenceDecayComponent : IComponentData
{
    public float DecayRate; // Freshness lost per second
    public float WeatherMultiplier; // Rain/wind accelerates decay
    public bool IsIndoor; // Indoor evidence lasts longer
    public bool IsCleaned; // Intentionally cleaned, accelerated decay
}
```

### 2. Detection Components

```csharp
public struct DetectionCapabilityComponent : IComponentData
{
    public float PerceptionRange; // Meters
    public float PerceptionSkill; // 0-100
    public float InvestigationSkill; // 0-100, for analyzing evidence
    public bool HasDarkvision; // Can see in darkness
    public bool HasTremorsense; // Detects vibrations
    public bool HasBlindsight; // Doesn't need light
}

public struct MentalDetectionComponent : IComponentData
{
    public bool HasTelepathy;
    public float TelepathyRange; // Meters
    public float TelepathyPower; // 0-100, penetration strength
    public bool CanDetectIntent; // Hostile thoughts
    public bool CanReadSurfaceThoughts;
    public bool CanReadDeepMemories;
}

public struct TechnologicalDetectionComponent : IComponentData
{
    public bool HasMotionSensors;
    public bool HasHeatVision;
    public bool HasCameraSurveillance;
    public bool HasBiometricScanner;
    public float SensorRange; // Meters
    public float SensorSensitivity; // 0-1
}

public struct DetectionStateComponent : IComponentData
{
    public bool IsAlerted; // Currently on high alert
    public float AlertTimer; // Time remaining in alert state
    public Entity LastThreatDetected; // What triggered alert
    public float3 LastThreatLocation; // Where threat was seen
    public float SuspicionLevel; // 0-100, accumulated suspicion
}
```

### 3. Response Components

```csharp
public struct ThreatResponseComponent : IComponentData
{
    public ResponseProfile PrimaryResponse;
    public ResponseProfile SecondaryResponse; // Fallback if primary fails
    public float CourageStat; // 0-100
    public float IntelligenceStat; // 0-100
    public float WisdomStat; // 0-100
    public AlignmentType Alignment;
    public bool IsTrained; // Military/guard training
}

public enum ResponseProfile
{
    CombatEngage,      // Attack threat
    AlertAndInvestigate, // Raise alarm, search area
    FleeToSafety,      // Run to safe location
    CowerAndHide,      // Hide in place
    OpportunisticTheft, // Steal from victim
    AidVictim,         // Help injured person
    IgnoreEvidence     // Pretend not to see
}

public enum AlignmentType
{
    Good,
    Neutral,
    Evil
}

public struct FleeDestinationComponent : IComponentData
{
    public Entity HomeLocation; // Residence
    public Entity PartyLocation; // Ally group
    public Entity GuardPostLocation; // Nearest security
    public float3 FallbackPosition; // Generic safe spot
}

public struct AlertBehaviorComponent : IComponentData
{
    public bool WillRaiseAlarm; // Shout/signal for help
    public float AlarmRange; // How far shout carries
    public bool WillCallReinforcements;
    public int MinReinforcementsNeeded; // Call backup if outnumbered by this much
    public bool WillLockdown; // Lock doors, secure area
}
```

### 4. Investigation Components

```csharp
public struct InvestigationComponent : IComponentData
{
    public Entity EvidenceBeingInvestigated;
    public float InvestigationProgress; // 0-100%
    public float TimeInvested; // Seconds spent investigating
    public InvestigationStage CurrentStage;
    public bool HasConcludedInvestigation;
}

public enum InvestigationStage
{
    Approaching,    // Moving to evidence
    Observing,      // Initial examination
    Analyzing,      // Detailed analysis
    Questioning,    // If victim alive
    Reporting,      // Informing authorities
    Pursuing        // Chasing suspect if identified
}

public struct InvestigationResultComponent : IComponentData
{
    public bool IdentifiedCulprit;
    public Entity SuspectEntity; // Who they think did it
    public float ConfidenceLevel; // 0-100%, how sure they are
    public float3 SuspectLastKnownPosition;
    public float TimeOfIncident; // Estimated time crime occurred
}
```

### 5. Alert System Components

```csharp
public struct AlertLevelComponent : IComponentData
{
    public int CurrentLevel; // 0=Normal, 1=Suspicious, 2=Incident, 3=Crisis
    public float TimeAtCurrentLevel; // How long alert has been active
    public float DecayTimer; // Time until alert drops
    public AlertTrigger WhatTriggered; // What raised the alert
}

public enum AlertTrigger
{
    None,
    BloodFound,
    BodyFound,
    IntruderSpotted,
    AlarmRaised,
    VIPKilled,
    MultipleDeaths,
    HackDetected,
    MissingPerson
}

public struct AlertPropagationComponent : IComponentData
{
    public float PropagationRadius; // How far alert has spread
    public float PropagationSpeed; // Meters per second
    public int EntitiesAlerted; // Count of aware entities
    public bool UsesVerbalPropagation; // Shouting
    public bool UsesMagicalPropagation; // Telepathy
    public bool UsesTechnologicalPropagation; // Radio/network
}
```

---

## Core Algorithms

### 1. Physical Evidence Detection

```csharp
public static class DetectionAlgorithms
{
    /// <summary>
    /// Calculate chance for entity to detect physical evidence
    /// </summary>
    public static float CalculatePhysicalDetectionChance(
        in PhysicalEvidenceComponent evidence,
        in DetectionCapabilityComponent detector,
        in DetectionStateComponent state,
        float distanceToEvidence,
        bool hasLineOfSight,
        float lightingLevel, // 0-1
        float environmentalNoise) // 0-1, distractions
    {
        // Out of range
        if (distanceToEvidence > detector.PerceptionRange)
            return 0f;

        // No line of sight
        if (!hasLineOfSight && !detector.HasBlindsight)
            return 0f;

        // Base chance from evidence properties
        float baseChance = evidence.Obviousness * evidence.Freshness;

        // Perception skill bonus (0-0.5)
        float perceptionBonus = detector.PerceptionSkill / 200f;

        // Distance penalty (0-1, worse at distance)
        float distancePenalty = distanceToEvidence / detector.PerceptionRange;

        // Lighting affects visual detection (unless darkvision/blindsight)
        float lightingMod = 1f;
        if (!detector.HasDarkvision && !detector.HasBlindsight)
        {
            lightingMod = lightingLevel;
        }

        // Alert state increases awareness
        float alertBonus = state.IsAlerted ? 0.2f : 0f;

        // Environmental noise (crowd, music, etc.) reduces detection
        float noisePenalty = environmentalNoise * 0.3f;

        float finalChance = (baseChance + perceptionBonus + alertBonus - distancePenalty - noisePenalty) * lightingMod;

        return math.clamp(finalChance, 0f, 1f);
    }

    /// <summary>
    /// Decay evidence over time (weather, cleaning, time)
    /// </summary>
    public static void UpdateEvidenceDecay(
        ref PhysicalEvidenceComponent evidence,
        in EvidenceDecayComponent decay,
        float deltaTime)
    {
        float decayAmount = decay.DecayRate * deltaTime;

        // Weather accelerates outdoor decay
        if (!decay.IsIndoor)
        {
            decayAmount *= decay.WeatherMultiplier;
        }

        // Cleaning dramatically accelerates decay
        if (decay.IsCleaned)
        {
            decayAmount *= 10f;
        }

        evidence.Freshness -= decayAmount;
        evidence.Freshness = math.max(0f, evidence.Freshness);

        // Evidence disappears when freshness reaches 0
        // (Handled by system that destroys entity)
    }
}
```

### 2. Mental Detection

```csharp
/// <summary>
/// Calculate chance for telepath to detect hostile intent
/// </summary>
public static float CalculateMentalDetectionChance(
    in MentalDetectionComponent detector,
    float targetWillpower, // 0-100, resistance
    float distanceToTarget,
    bool targetHasHostileIntent,
    bool targetHasMentalShielding)
{
    if (!detector.HasTelepathy)
        return 0f;

    if (distanceToTarget > detector.TelepathyRange)
        return 0f;

    // Base detection chance from intent strength
    float baseChance = targetHasHostileIntent ? 0.85f : 0.05f;

    // Telepath power bonus
    float powerBonus = detector.TelepathyPower / 100f; // 0-1

    // Target willpower resistance
    float willpowerResist = targetWillpower / 200f; // 0-0.5

    // Distance penalty
    float distancePenalty = distanceToTarget / detector.TelepathyRange * 0.3f;

    // Mental shielding (magic, tech, training)
    float shieldPenalty = targetHasMentalShielding ? 0.4f : 0f;

    float finalChance = baseChance + powerBonus - willpowerResist - distancePenalty - shieldPenalty;

    return math.clamp(finalChance, 0f, 1f);
}
```

### 3. Response Determination

```csharp
/// <summary>
/// Determine how entity responds to detected threat/evidence
/// </summary>
public static ResponseProfile DetermineResponseBehavior(
    in ThreatResponseComponent response,
    in DetectionStateComponent detectionState,
    EvidenceType evidenceDetected,
    bool seesDirectThreat,
    bool hasAlliesNearby,
    int threatCount)
{
    // Direct visible threat overrides profile for cowards
    if (seesDirectThreat && response.CourageStat < 40f)
    {
        return ResponseProfile.FleeToSafety;
    }

    // Trained combatants engage if brave enough
    if (response.IsTrained && response.CourageStat > 60f)
    {
        // Unless heavily outnumbered
        if (threatCount > 3 && !hasAlliesNearby)
        {
            return ResponseProfile.AlertAndInvestigate; // Call backup first
        }
        return ResponseProfile.CombatEngage;
    }

    // Criminal alignment steals from unconscious victims
    if (response.Alignment == AlignmentType.Evil &&
        evidenceDetected == EvidenceType.UnconsciousBody)
    {
        // Risk assessment (intelligence check)
        bool lowRisk = !hasAlliesNearby && detectionState.SuspicionLevel < 30f;
        if (lowRisk && response.IntelligenceStat > 40f)
        {
            return ResponseProfile.OpportunisticTheft;
        }
    }

    // Good alignment helps victims
    if (response.Alignment == AlignmentType.Good &&
        (evidenceDetected == EvidenceType.UnconsciousBody ||
         evidenceDetected == EvidenceType.Corpse))
    {
        return ResponseProfile.AidVictim;
    }

    // High wisdom entities investigate
    if (response.WisdomStat > 60f)
    {
        return ResponseProfile.AlertAndInvestigate;
    }

    // Default for untrained civilians
    if (!response.IsTrained)
    {
        // Flee if evidence is horrifying
        if (evidenceDetected == EvidenceType.Corpse ||
            evidenceDetected == EvidenceType.Blood ||
            evidenceDetected == EvidenceType.SeveredLimb)
        {
            return math.random(new Random((uint)evidenceDetected)).NextFloat() < 0.6f
                ? ResponseProfile.FleeToSafety
                : ResponseProfile.CowerAndHide;
        }

        return ResponseProfile.AlertAndInvestigate;
    }

    // Fallback to primary response
    return response.PrimaryResponse;
}

/// <summary>
/// Calculate flee destination based on context
/// </summary>
public static float3 CalculateFleeDestination(
    in FleeDestinationComponent destinations,
    float3 currentPosition,
    float3 threatPosition,
    bool isHomeNearby,
    bool areAlliesNearby,
    bool isGuardPostNearby)
{
    // Prioritize based on proximity and availability
    if (isHomeNearby && destinations.HomeLocation != Entity.Null)
    {
        // Home is closest safe place
        return GetPositionOfEntity(destinations.HomeLocation);
    }

    if (areAlliesNearby && destinations.PartyLocation != Entity.Null)
    {
        // Run to allies for protection
        return GetPositionOfEntity(destinations.PartyLocation);
    }

    if (isGuardPostNearby && destinations.GuardPostLocation != Entity.Null)
    {
        // Seek authority protection
        return GetPositionOfEntity(destinations.GuardPostLocation);
    }

    // Fallback: Run away from threat
    float3 directionFromThreat = math.normalize(currentPosition - threatPosition);
    return currentPosition + directionFromThreat * 50f; // 50 meters away
}

private static float3 GetPositionOfEntity(Entity entity)
{
    // Game-specific implementation to get entity position
    return float3.zero;
}
```

### 4. Investigation System

```csharp
/// <summary>
/// Calculate investigation success chance
/// </summary>
public static float CalculateInvestigationSuccess(
    float investigatorIntelligence, // 0-100
    float investigatorPerception, // 0-100
    float investigationSkill, // 0-100
    float evidenceQuality, // 0-1, how much evidence available
    float timeSpent, // Hours spent investigating
    bool hasWitnesses,
    bool hasMagicalScanning)
{
    float intBonus = investigatorIntelligence * 0.5f;
    float perceptionBonus = investigatorPerception * 0.3f;
    float skillBonus = investigationSkill * 0.2f;

    float evidenceBonus = evidenceQuality * 30f;
    float timeBonus = math.min(timeSpent * 5f, 20f); // Diminishing returns after 4 hours

    float witnessBonus = hasWitnesses ? 25f : 0f;
    float magicBonus = hasMagicalScanning ? 15f : 0f;

    float totalSuccess = intBonus + perceptionBonus + skillBonus + evidenceBonus +
                         timeBonus + witnessBonus + magicBonus;

    return math.clamp(totalSuccess, 5f, 95f); // Always 5-95% (never certain)
}

/// <summary>
/// Determine investigation outcome based on success roll
/// </summary>
public static InvestigationOutcome DetermineInvestigationOutcome(float successRoll)
{
    if (successRoll >= 90f) return InvestigationOutcome.CriticalSuccess;
    if (successRoll >= 70f) return InvestigationOutcome.Success;
    if (successRoll >= 50f) return InvestigationOutcome.PartialSuccess;
    if (successRoll >= 30f) return InvestigationOutcome.Failure;
    return InvestigationOutcome.CriticalFailure;
}

public enum InvestigationOutcome
{
    CriticalSuccess,  // Identify exact culprit, know location
    Success,          // Accurate description, general direction
    PartialSuccess,   // Vague description, method known
    Failure,          // Wrong conclusions, misdirected
    CriticalFailure   // Blame innocent, create bigger problem
}

/// <summary>
/// Update investigation progress over time
/// </summary>
public static void UpdateInvestigationProgress(
    ref InvestigationComponent investigation,
    float investigationSkill,
    float deltaTime)
{
    investigation.TimeInvested += deltaTime;

    // Progress rate based on skill
    float progressRate = (investigationSkill / 100f) * 10f; // 0-10% per second

    investigation.InvestigationProgress += progressRate * deltaTime;
    investigation.InvestigationProgress = math.min(100f, investigation.InvestigationProgress);

    // Advance stages
    if (investigation.InvestigationProgress < 20f)
        investigation.CurrentStage = InvestigationStage.Approaching;
    else if (investigation.InvestigationProgress < 40f)
        investigation.CurrentStage = InvestigationStage.Observing;
    else if (investigation.InvestigationProgress < 70f)
        investigation.CurrentStage = InvestigationStage.Analyzing;
    else if (investigation.InvestigationProgress < 90f)
        investigation.CurrentStage = InvestigationStage.Questioning;
    else
    {
        investigation.CurrentStage = InvestigationStage.Reporting;
        investigation.HasConcludedInvestigation = true;
    }
}
```

### 5. Alert Propagation

```csharp
/// <summary>
/// Propagate alert through population
/// </summary>
public static void PropagateAlert(
    ref AlertPropagationComponent propagation,
    float deltaTime)
{
    // Alert spreads over time
    float spreadDistance = propagation.PropagationSpeed * deltaTime;

    // Different propagation methods have different speeds
    if (propagation.UsesTechnologicalPropagation)
    {
        // Instant across network
        propagation.PropagationRadius = float.MaxValue;
    }
    else if (propagation.UsesMagicalPropagation)
    {
        // Very fast, limited by caster range
        propagation.PropagationRadius += spreadDistance * 10f;
    }
    else if (propagation.UsesVerbalPropagation)
    {
        // Sound travels fast but limited range
        propagation.PropagationRadius += spreadDistance;
    }
}

/// <summary>
/// Calculate alert level escalation
/// </summary>
public static int CalculateNewAlertLevel(
    int currentLevel,
    AlertTrigger newTrigger,
    float timeSinceLastEscalation)
{
    // Cannot escalate if recently escalated (prevents spam)
    if (timeSinceLastEscalation < 60f) // 1 minute cooldown
        return currentLevel;

    // Escalation triggers
    switch (newTrigger)
    {
        case AlertTrigger.BloodFound:
        case AlertTrigger.AlarmRaised:
            return math.max(currentLevel, 1); // Suspicious

        case AlertTrigger.BodyFound:
        case AlertTrigger.IntruderSpotted:
        case AlertTrigger.HackDetected:
            return math.max(currentLevel, 2); // Incident

        case AlertTrigger.VIPKilled:
        case AlertTrigger.MultipleDeaths:
            return 3; // Crisis (immediate escalation)

        default:
            return currentLevel;
    }
}

/// <summary>
/// Calculate alert decay over time
/// </summary>
public static void UpdateAlertDecay(
    ref AlertLevelComponent alert,
    float deltaTime,
    bool newEvidenceFound)
{
    // New evidence resets decay timer
    if (newEvidenceFound)
    {
        ResetDecayTimer(ref alert);
        return;
    }

    // Countdown decay timer
    alert.DecayTimer -= deltaTime;

    if (alert.DecayTimer <= 0f)
    {
        // Drop alert level
        alert.CurrentLevel = math.max(0, alert.CurrentLevel - 1);

        // Reset timer for new level
        ResetDecayTimer(ref alert);
    }

    alert.TimeAtCurrentLevel += deltaTime;
}

private static void ResetDecayTimer(ref AlertLevelComponent alert)
{
    switch (alert.CurrentLevel)
    {
        case 3: // Crisis
            alert.DecayTimer = float.MaxValue; // Never decays naturally
            break;
        case 2: // Incident
            alert.DecayTimer = 3600f; // 1 hour
            break;
        case 1: // Suspicious
            alert.DecayTimer = 1800f; // 30 minutes
            break;
        case 0: // Normal
            alert.DecayTimer = 0f;
            break;
    }
}
```

### 6. Absence Detection

```csharp
/// <summary>
/// Calculate time until missing person is noticed
/// </summary>
public static float CalculateMissingPersonDetectionTime(
    EntityRole role,
    RelationshipStrength closestRelationship,
    bool hasScheduledDuties)
{
    float baseTime = 0f;

    // Role affects how quickly missed
    switch (role)
    {
        case EntityRole.Guard:
            baseTime = 900f; // 15 minutes (shift change)
            break;
        case EntityRole.Servant:
            baseTime = 7200f; // 2 hours (scheduled tasks)
            break;
        case EntityRole.Noble:
            baseTime = 1800f; // 30 minutes (expected at events)
            break;
        case EntityRole.Commoner:
            baseTime = 86400f; // 24 hours (daily routine)
            break;
        case EntityRole.VIP:
            baseTime = 600f; // 10 minutes (heavily monitored)
            break;
    }

    // Relationship reduces time (loved ones notice faster)
    float relationshipModifier = 1f;
    switch (closestRelationship)
    {
        case RelationshipStrength.Spouse:
        case RelationshipStrength.Parent:
            relationshipModifier = 0.5f; // 50% faster detection
            break;
        case RelationshipStrength.Close:
            relationshipModifier = 0.7f;
            break;
        case RelationshipStrength.Acquaintance:
            relationshipModifier = 1.0f;
            break;
        case RelationshipStrength.Stranger:
            relationshipModifier = 2.0f; // 2x longer to notice
            break;
    }

    // Scheduled duties reduce time (expected to be somewhere)
    if (hasScheduledDuties)
    {
        relationshipModifier *= 0.5f;
    }

    return baseTime * relationshipModifier;
}

public enum EntityRole
{
    Guard,
    Servant,
    Noble,
    Commoner,
    VIP
}

public enum RelationshipStrength
{
    Spouse,
    Parent,
    Close,
    Acquaintance,
    Stranger
}

/// <summary>
/// Calculate time until missing item is noticed
/// </summary>
public static float CalculateMissingItemDetectionTime(
    int itemValue,
    ItemImportance importance,
    float lastCheckTime,
    float checkFrequency) // Hours between checks
{
    float baseTime = checkFrequency * 3600f; // Convert hours to seconds

    // Importance drastically reduces time
    float importanceMod = 1f;
    switch (importance)
    {
        case ItemImportance.Priceless:
            importanceMod = 0.1f; // Noticed almost immediately
            break;
        case ItemImportance.Critical:
            importanceMod = 0.3f;
            break;
        case ItemImportance.Valuable:
            importanceMod = 0.6f;
            break;
        case ItemImportance.Standard:
            importanceMod = 1.0f;
            break;
        case ItemImportance.Trivial:
            importanceMod = 5.0f; // May never be noticed
            break;
    }

    return baseTime * importanceMod;
}

public enum ItemImportance
{
    Priceless,   // Crown jewels, artifacts
    Critical,    // Keys, important documents
    Valuable,    // Gold, magic items
    Standard,    // Normal possessions
    Trivial      // Junk, common items
}
```

---

## ECS Systems

### Detection System (Body ECS - 60 Hz)

```csharp
[UpdateInGroup(typeof(BodyECSGroup))]
public partial struct PhysicalDetectionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Query all entities capable of detection
        foreach (var (capability, detectionState, transform, entity) in
            SystemAPI.Query<RefRO<DetectionCapabilityComponent>, RefRW<DetectionStateComponent>, RefRO<TransformComponent>>()
                .WithEntityAccess())
        {
            // Query all evidence in perception range
            foreach (var (evidence, evidenceTransform) in
                SystemAPI.Query<RefRO<PhysicalEvidenceComponent>, RefRO<TransformComponent>>())
            {
                float distance = math.distance(transform.ValueRO.Position, evidenceTransform.ValueRO.Position);

                if (distance > capability.ValueRO.PerceptionRange)
                    continue;

                // Check line of sight (simplified, would query physics)
                bool hasLOS = true; // Replace with actual raycaast

                // Get lighting level at evidence position (from environment system)
                float lighting = GetLightingLevel(evidenceTransform.ValueRO.Position);

                // Get environmental noise (from environment system)
                float noise = GetEnvironmentalNoise(evidenceTransform.ValueRO.Position);

                float detectionChance = DetectionAlgorithms.CalculatePhysicalDetectionChance(
                    in evidence.ValueRO,
                    in capability.ValueRO,
                    in detectionState.ValueRO,
                    distance,
                    hasLOS,
                    lighting,
                    noise);

                // Roll for detection
                if (UnityEngine.Random.Range(0f, 1f) < detectionChance)
                {
                    // Detected! Trigger response
                    detectionState.ValueRW.IsAlerted = true;
                    detectionState.ValueRW.LastThreatLocation = evidenceTransform.ValueRO.Position;
                    detectionState.ValueRW.SuspicionLevel = math.min(100f, detectionState.ValueRW.SuspicionLevel + 30f);

                    // Add response command (handled by response system)
                    state.EntityManager.AddComponent<TriggerResponseComponent>(entity);
                }
            }
        }
    }

    private float GetLightingLevel(float3 position)
    {
        // Game-specific implementation
        return 1f;
    }

    private float GetEnvironmentalNoise(float3 position)
    {
        // Game-specific implementation
        return 0f;
    }
}
```

### Response System (Mind ECS - 1 Hz)

```csharp
[UpdateInGroup(typeof(MindECSGroup))]
public partial struct ThreatResponseSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Process entities that detected threats
        foreach (var (response, detectionState, fleeDestinations, entity) in
            SystemAPI.Query<RefRO<ThreatResponseComponent>, RefRO<DetectionStateComponent>, RefRO<FleeDestinationComponent>>()
                .WithAll<TriggerResponseComponent>()
                .WithEntityAccess())
        {
            // Determine response behavior
            bool seesDirectThreat = detectionState.ValueRO.LastThreatDetected != Entity.Null;
            bool hasAllies = CheckForNearbyAllies(entity); // Game-specific
            int threatCount = CountNearbyThreats(entity); // Game-specific

            ResponseProfile chosenResponse = DetectionAlgorithms.DetermineResponseBehavior(
                in response.ValueRO,
                in detectionState.ValueRO,
                EvidenceType.Blood, // Would query actual evidence type
                seesDirectThreat,
                hasAllies,
                threatCount);

            // Execute response
            ExecuteResponse(ref state, entity, chosenResponse, in fleeDestinations.ValueRO, detectionState.ValueRO.LastThreatLocation);

            // Remove trigger component (one-time response)
            state.EntityManager.RemoveComponent<TriggerResponseComponent>(entity);
        }
    }

    private bool CheckForNearbyAllies(Entity entity)
    {
        // Game-specific implementation
        return false;
    }

    private int CountNearbyThreats(Entity entity)
    {
        // Game-specific implementation
        return 1;
    }

    private void ExecuteResponse(
        ref SystemState state,
        Entity entity,
        ResponseProfile response,
        in FleeDestinationComponent destinations,
        float3 threatPosition)
    {
        // Game-specific implementation to add navigation commands, combat states, etc.
    }
}

// Trigger component (tag)
public struct TriggerResponseComponent : IComponentData { }
```

### Alert Propagation System (Aggregate ECS - 0.2 Hz)

```csharp
[UpdateInGroup(typeof(AggregateECSGroup))]
public partial struct AlertPropagationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (alertLevel, propagation) in
            SystemAPI.Query<RefRW<AlertLevelComponent>, RefRW<AlertPropagationComponent>>())
        {
            // Propagate alert
            DetectionAlgorithms.PropagateAlert(ref propagation.ValueRW, deltaTime);

            // Check for new evidence (would query game state)
            bool newEvidenceFound = false;

            // Update decay
            DetectionAlgorithms.UpdateAlertDecay(ref alertLevel.ValueRW, deltaTime, newEvidenceFound);
        }
    }
}
```

---

## Summary

This agnostic framework provides the mathematical and structural foundation for infiltration detection systems:

**Core Detection Methods:**
1. **Physical**: Evidence visibility based on range, lighting, perception
2. **Mental**: Telepathy detecting hostile intent vs willpower resistance
3. **Technological**: Sensors, cameras, biometric scans
4. **Absence**: Missing persons/items detected based on role, relationship, importance

**Response Behaviors:**
- Combat (trained, brave)
- Alert and Investigate (cautious, intelligent)
- Flee to Safety (civilians, cowards)
- Opportunistic Theft (criminals, low risk)
- Aid Victim (good alignment, helpful)

**Alert System:**
- 4 Levels: Normal → Suspicious → Incident → Crisis
- Propagation: Verbal (slow), Magical (fast), Technological (instant)
- Decay: Suspicion fades over time if no new evidence

**Investigation:**
- Success based on INT, Perception, Investigation Skill, Evidence Quality
- Outcomes: Identify culprit → Wrong conclusions → Blame innocent
- Time invested increases success chance

This framework allows game implementations to create deep stealth/infiltration gameplay with realistic detection and response systems.
