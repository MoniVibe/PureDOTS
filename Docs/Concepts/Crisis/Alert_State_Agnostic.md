# Alert State and Crisis Response System - Agnostic Framework

## Overview

This document provides **game-agnostic algorithms and ECS components** for implementing crisis detection, alert states, and community-wide behavioral responses to threats.

The framework supports:
- Multi-source crisis detection (observation, intelligence, divination/sensors)
- Hierarchical alert states (0-4: Normal → Desperate)
- Crisis-specific behavioral modifications
- Preparation and stockpiling mechanics
- Population-wide response coordination

---

## Core Components

### 1. Detection Components

```csharp
using Unity.Entities;
using Unity.Mathematics;

public struct ObservatoryComponent : IComponentData
{
    public float DetectionRange; // Kilometers or game units
    public float PredictionAccuracy; // 0-1
    public int StaffSkillLevel; // 0-100
    public float AdvanceWarningDays; // Days ahead it can predict
    public bool CanDetectWeather;
    public bool CanDetectCelestial;
    public bool CanDetectMagical; // Or technological anomalies
    public bool CanDetectMilitary; // Army movements
}

public struct ScoutNetworkComponent : IComponentData
{
    public int ScoutCount;
    public float CoverageRadius; // Area monitored
    public float AverageScoutSkill; // 0-100
    public float DetectionChance; // 0-1, probability of spotting threats
    public float WarningTimeHours; // Hours before threat arrives
}

public struct IntelligenceNetworkComponent : IComponentData
{
    public int AgentCount; // Spies, informants
    public float NetworkQuality; // 0-100, how good intelligence is
    public float PoliticalReach; // 0-1, % of region covered
    public float WarningTimeDays; // Days advance warning
    public int MonthlyCost; // Upkeep cost
}

public struct DivinationCapabilityComponent : IComponentData
{
    public bool HasDivination; // Magical/technological foresight
    public float PowerLevel; // 0-100, strength of divination
    public float Accuracy; // 0-1, how often correct
    public float MaxRangeDays; // How far ahead can predict
    public int ManaCost; // Or energy cost
}
```

### 2. Crisis Components

```csharp
public struct CrisisComponent : IComponentData
{
    public CrisisType Type;
    public int SeverityLevel; // 1-10
    public float TimeUntilImpact; // Days/hours remaining
    public float Duration; // How long crisis lasts
    public bool IsActive; // Currently occurring
    public bool IsPredicted; // Detected but not yet happened
    public float3 OriginLocation; // Where threat originates
}

public enum CrisisType
{
    None,
    ResourceShortage,    // Famine, water shortage
    Infiltration,        // Spies, assassins, saboteurs
    InvasionThreat,      // Enemy army approaching
    NaturalDisaster,     // Storm, earthquake, flood
    Plague,              // Disease outbreak
    MagicalAnomaly,      // Magical storm, planar rift
    PoliticalCrisis,     // Coup, rebellion, unrest
    TechnologicalFailure // Space4X: Life support, reactor
}

public struct CrisisDetectionComponent : IComponentData
{
    public CrisisType DetectedCrisis;
    public float ConfidenceLevel; // 0-1, how sure detection is
    public float TimeDetected; // Game time when detected
    public Entity DetectionSource; // Observatory, scout, etc.
    public bool AlertRaised; // Has population been informed
}
```

### 3. Alert State Components

```csharp
public struct AlertStateComponent : IComponentData
{
    public int CurrentAlertLevel; // 0-4
    public CrisisType TriggeringCrisis;
    public float TimeAtCurrentLevel; // Seconds at this alert
    public float AlertEscalationThreshold; // When to increase alert
    public float AlertDecayRate; // How fast alert drops if crisis resolves
}

public enum AlertLevel
{
    Normal = 0,      // No threats
    Concerned = 1,   // Potential threat detected
    Alarmed = 2,     // Confirmed threat approaching
    Crisis = 3,      // Threat imminent or occurring
    Desperate = 4    // Existential threat, survival mode
}

public struct PopulationAlertAwarenessComponent : IComponentData
{
    public int KnownAlertLevel; // What population knows
    public float FearLevel; // 0-100
    public float PanicLevel; // 0-100
    public bool IsEvacuating;
    public bool IsPreparing; // Stockpiling, fortifying
    public float MoraleModifier; // -1 to +1
}
```

### 4. Response Behavior Components

```csharp
public struct CrisisResponseComponent : IComponentData
{
    public ResponseProfile Profile; // How this entity responds
    public float WorkRateModifier; // Multiply work speed
    public bool IsConscripted; // Forced into service
    public bool HasFledArea; // Evacuated
    public float PreparationProgress; // 0-100%, readiness
}

public enum ResponseProfile
{
    Worker_DoubleEffort,     // Gatherers work 2× harder
    Guard_Mobilize,          // Military readiness
    Civilian_Stockpile,      // Hoard resources
    Civilian_Evacuate,       // Flee to safety
    Leader_Coordinate,       // Organize response
    Craftsman_RushProduction // Make weapons/defenses quickly
}

public struct StockpileComponent : IComponentData
{
    public int FoodDaysRemaining; // Days of food in reserve
    public int WaterDaysRemaining;
    public int AmmoCount;          // Military supplies
    public int MedicineUnits;
    public float StockpileTarget;  // Target stockpile (days)
    public bool IsRationing;       // Reduce consumption
}

public struct DefensePreparationComponent : IComponentData
{
    public float FortificationProgress; // 0-100%
    public int TrapCount;               // Traps set
    public int BarricadesErected;
    public int MilitiaRecruited;        // Civilians trained
    public int GuardsOnDuty;            // % of force active
    public bool ScorchedEarthActive;    // Destroy resources enemy could use
}
```

---

## Core Algorithms

### 1. Crisis Detection

```csharp
public static class CrisisDetectionAlgorithms
{
    /// <summary>
    /// Calculate chance to detect approaching crisis
    /// </summary>
    public static float CalculateDetectionChance(
        in ObservatoryComponent observatory,
        CrisisType crisisType,
        float distanceToThreat,
        float daysUntilImpact)
    {
        // Out of range
        if (distanceToThreat > observatory.DetectionRange)
            return 0f;

        // Base detection chance from staff skill
        float baseChance = observatory.StaffSkillLevel / 100f;

        // Crisis type modifiers (some easier to detect)
        float typeModifier = GetCrisisDetectionModifier(observatory, crisisType);

        // Distance penalty (harder to detect far threats)
        float distancePenalty = distanceToThreat / observatory.DetectionRange * 0.5f;

        // Time pressure (less time = harder to detect)
        float timePenalty = daysUntilImpact < 1f ? 0.3f : 0f;

        // Accuracy modifier
        float accuracyBonus = observatory.PredictionAccuracy * 0.3f;

        float finalChance = baseChance + typeModifier + accuracyBonus - distancePenalty - timePenalty;

        return math.clamp(finalChance, 0f, 1f);
    }

    private static float GetCrisisDetectionModifier(
        in ObservatoryComponent observatory,
        CrisisType crisisType)
    {
        switch (crisisType)
        {
            case CrisisType.NaturalDisaster:
                return observatory.CanDetectWeather ? 0.3f : 0f;

            case CrisisType.InvasionThreat:
                return observatory.CanDetectMilitary ? 0.25f : 0f;

            case CrisisType.MagicalAnomaly:
                return observatory.CanDetectMagical ? 0.35f : 0f;

            case CrisisType.ResourceShortage:
                // Observable through trends, crop health
                return 0.2f;

            case CrisisType.Infiltration:
                // Very hard to detect via observation
                return 0.05f;

            case CrisisType.Plague:
                // Visible through patterns (fleeing refugees, etc.)
                return 0.15f;

            default:
                return 0f;
        }
    }

    /// <summary>
    /// Calculate advance warning time based on detection source
    /// </summary>
    public static float CalculateWarningTime(
        DetectionSource source,
        float distanceToThreat,
        float threatSpeed) // Units per day
    {
        float travelTimeDays = distanceToThreat / threatSpeed;

        switch (source)
        {
            case DetectionSource.Observatory:
                // Long-range prediction (weather patterns, celestial)
                return math.min(travelTimeDays, 90f); // Up to 90 days

            case DetectionSource.ScoutNetwork:
                // Visual sighting (closer range)
                return math.min(travelTimeDays * 0.3f, 5f); // Up to 5 days

            case DetectionSource.IntelligenceNetwork:
                // Political intel (mobilization reports)
                return math.min(travelTimeDays * 0.6f, 30f); // Up to 30 days

            case DetectionSource.Divination:
                // Magical/tech foresight (variable)
                return math.min(travelTimeDays * 0.8f, 180f); // Up to 180 days

            case DetectionSource.Refugees:
                // Fleeing population (very short warning)
                return math.min(travelTimeDays * 0.1f, 2f); // Up to 2 days

            default:
                return 0f;
        }
    }

    public enum DetectionSource
    {
        Observatory,
        ScoutNetwork,
        IntelligenceNetwork,
        Divination,
        Refugees,
        DirectObservation // Saw it with own eyes
    }
}
```

### 2. Alert State Escalation

```csharp
public static class AlertStateAlgorithms
{
    /// <summary>
    /// Determine alert level based on crisis severity and time remaining
    /// </summary>
    public static int CalculateAlertLevel(
        in CrisisComponent crisis,
        float preparationTimeAvailable)
    {
        if (!crisis.IsActive && !crisis.IsPredicted)
            return 0; // Normal

        // Severity factor (1-10)
        float severityFactor = crisis.SeverityLevel / 10f;

        // Time pressure (less time = higher alert)
        float timePressure = 1f;
        if (preparationTimeAvailable > 14f)
            timePressure = 0.3f; // Distant threat
        else if (preparationTimeAvailable > 7f)
            timePressure = 0.5f; // Week away
        else if (preparationTimeAvailable > 3f)
            timePressure = 0.7f; // Days away
        else if (preparationTimeAvailable > 0f)
            timePressure = 0.9f; // Imminent
        else
            timePressure = 1.0f; // Happening now

        // Combined threat score
        float threatScore = severityFactor * timePressure;

        // Map to alert levels
        if (threatScore >= 0.8f) return 4; // Desperate
        if (threatScore >= 0.6f) return 3; // Crisis
        if (threatScore >= 0.4f) return 2; // Alarmed
        if (threatScore >= 0.2f) return 1; // Concerned

        return 0; // Normal
    }

    /// <summary>
    /// Update alert state over time (escalate or decay)
    /// </summary>
    public static void UpdateAlertState(
        ref AlertStateComponent alert,
        in CrisisComponent crisis,
        float deltaTime,
        bool newEvidenceFound)
    {
        // Calculate what alert level should be
        int targetLevel = CalculateAlertLevel(in crisis, crisis.TimeUntilImpact);

        // Escalate quickly if new evidence
        if (newEvidenceFound && targetLevel > alert.CurrentAlertLevel)
        {
            alert.CurrentAlertLevel = targetLevel;
            alert.TimeAtCurrentLevel = 0f;
            return;
        }

        // Gradual escalation if target higher
        if (targetLevel > alert.CurrentAlertLevel)
        {
            alert.TimeAtCurrentLevel += deltaTime;

            // Escalate after threshold time at current level
            if (alert.TimeAtCurrentLevel > alert.AlertEscalationThreshold)
            {
                alert.CurrentAlertLevel++;
                alert.TimeAtCurrentLevel = 0f;
            }
        }
        // Gradual decay if target lower
        else if (targetLevel < alert.CurrentAlertLevel)
        {
            float decayAmount = alert.AlertDecayRate * deltaTime;
            alert.TimeAtCurrentLevel -= decayAmount;

            // Decay to lower level
            if (alert.TimeAtCurrentLevel < 0f)
            {
                alert.CurrentAlertLevel--;
                alert.TimeAtCurrentLevel = 0f;
            }
        }
    }
}
```

### 3. Crisis Response Behaviors

```csharp
public static class CrisisResponseAlgorithms
{
    /// <summary>
    /// Determine how entity should respond to crisis
    /// </summary>
    public static ResponseProfile DetermineResponse(
        EntityRole role,
        CrisisType crisisType,
        int alertLevel,
        float courageLevel, // 0-100
        bool hasFamily)
    {
        // Desperate = everyone flees or fights
        if (alertLevel >= 4)
        {
            if (courageLevel < 30f && role != EntityRole.Military)
                return ResponseProfile.Civilian_Evacuate;
        }

        // Role-based responses
        switch (role)
        {
            case EntityRole.Gatherer:
            case EntityRole.Farmer:
                if (crisisType == CrisisType.ResourceShortage)
                    return ResponseProfile.Worker_DoubleEffort;
                break;

            case EntityRole.Guard:
            case EntityRole.Soldier:
                if (crisisType == CrisisType.InvasionThreat ||
                    crisisType == CrisisType.Infiltration)
                    return ResponseProfile.Guard_Mobilize;
                break;

            case EntityRole.Craftsman:
                if (crisisType == CrisisType.InvasionThreat && alertLevel >= 2)
                    return ResponseProfile.Craftsman_RushProduction;
                break;

            case EntityRole.Leader:
                return ResponseProfile.Leader_Coordinate;

            case EntityRole.Civilian:
                if (alertLevel >= 2 && (crisisType == CrisisType.InvasionThreat ||
                                        crisisType == CrisisType.Plague ||
                                        crisisType == CrisisType.NaturalDisaster))
                {
                    // Evacuate if has family to protect
                    if (hasFamily && courageLevel < 60f)
                        return ResponseProfile.Civilian_Evacuate;

                    // Stockpile if staying
                    return ResponseProfile.Civilian_Stockpile;
                }
                break;
        }

        // Default: stockpile resources
        return ResponseProfile.Civilian_Stockpile;
    }

    public enum EntityRole
    {
        Civilian,
        Gatherer,
        Farmer,
        Craftsman,
        Guard,
        Soldier,
        Leader,
        Merchant
    }

    /// <summary>
    /// Calculate work rate modifier during crisis
    /// </summary>
    public static float CalculateWorkRateModifier(
        ResponseProfile response,
        int alertLevel,
        float entityStamina) // 0-100
    {
        float baseModifier = 1f;

        switch (response)
        {
            case ResponseProfile.Worker_DoubleEffort:
                baseModifier = 2.0f; // Work twice as hard
                break;

            case ResponseProfile.Craftsman_RushProduction:
                baseModifier = 1.5f; // Work 50% faster
                break;

            case ResponseProfile.Guard_Mobilize:
                baseModifier = 1.3f; // Extended shifts
                break;

            case ResponseProfile.Civilian_Evacuate:
                baseModifier = 0.1f; // Minimal work (fleeing)
                break;

            case ResponseProfile.Civilian_Stockpile:
                baseModifier = 1.2f; // Work harder to prepare
                break;
        }

        // Alert level increases work rate (desperation)
        float alertModifier = 1f + (alertLevel * 0.1f); // +10% per alert level

        // Stamina limits how long they can sustain
        float staminaModifier = entityStamina / 100f;

        return baseModifier * alertModifier * staminaModifier;
    }

    /// <summary>
    /// Calculate exhaustion from overwork during crisis
    /// </summary>
    public static void ApplyExhaustionFromCrisis(
        ref StaminaComponent stamina,
        float workRateModifier,
        float hoursWorked,
        float deltaTime)
    {
        // Normal work = 1.0 modifier, 8 hours/day
        // Double effort = 2.0 modifier, 12-16 hours/day

        float exhaustionRate = (workRateModifier - 1f) * 2f; // How much faster they tire

        float exhaustionAmount = exhaustionRate * (hoursWorked / 8f) * deltaTime;

        stamina.CurrentStamina -= exhaustionAmount;
        stamina.CurrentStamina = math.max(0f, stamina.CurrentStamina);

        // Death from exhaustion if stamina = 0 for extended period
        if (stamina.CurrentStamina <= 0f)
        {
            stamina.TimeAtZeroStamina += deltaTime;

            if (stamina.TimeAtZeroStamina > 86400f) // 24 hours at zero = death
            {
                // Entity dies from overwork
                // (Handled by death system)
            }
        }
    }

    public struct StaminaComponent : IComponentData
    {
        public float CurrentStamina; // 0-100
        public float TimeAtZeroStamina; // Seconds at 0 stamina
    }
}
```

### 4. Stockpiling and Preparation

```csharp
public static class PreparationAlgorithms
{
    /// <summary>
    /// Calculate stockpile target based on crisis prediction
    /// </summary>
    public static float CalculateStockpileTarget(
        CrisisType crisisType,
        float crisisDurationEstimate, // Days
        int populationSize,
        int alertLevel)
    {
        float baselineDays = 30f; // Normal stockpile

        // Crisis-specific targets
        float crisisTarget = baselineDays;

        switch (crisisType)
        {
            case CrisisType.ResourceShortage:
                crisisTarget = math.max(baselineDays, crisisDurationEstimate * 1.5f);
                break;

            case CrisisType.InvasionThreat:
                // Siege could last months
                crisisTarget = math.max(60f, crisisDurationEstimate * 2f);
                break;

            case CrisisType.Plague:
                // Quarantine period
                crisisTarget = math.max(45f, crisisDurationEstimate * 1.3f);
                break;

            case CrisisType.NaturalDisaster:
                // Aftermath recovery
                crisisTarget = math.max(40f, crisisDurationEstimate * 1.2f);
                break;
        }

        // Alert level increases target (higher panic = more hoarding)
        float alertMultiplier = 1f + (alertLevel * 0.15f); // +15% per level

        // Population size affects target (economies of scale)
        float populationModifier = 1f + math.log10(populationSize) * 0.1f;

        return crisisTarget * alertMultiplier * populationModifier;
    }

    /// <summary>
    /// Calculate resource consumption rate during crisis
    /// </summary>
    public static float CalculateConsumptionRate(
        int populationSize,
        bool isRationing,
        int alertLevel,
        CrisisType crisisType)
    {
        // Base consumption: 1 unit food per person per day
        float baseConsumption = populationSize * 1f;

        // Rationing reduces consumption
        float rationingModifier = 1f;
        if (isRationing)
        {
            switch (alertLevel)
            {
                case 1: rationingModifier = 0.9f; break;  // Light rationing
                case 2: rationingModifier = 0.7f; break;  // Moderate rationing
                case 3: rationingModifier = 0.5f; break;  // Severe rationing
                case 4: rationingModifier = 0.3f; break;  // Starvation rations
            }
        }

        // Increased work during crisis increases consumption
        float workModifier = 1f + (alertLevel * 0.1f);

        float finalConsumption = baseConsumption * rationingModifier * workModifier;

        return finalConsumption;
    }

    /// <summary>
    /// Update stockpile levels over time
    /// </summary>
    public static void UpdateStockpiles(
        ref StockpileComponent stockpile,
        int populationSize,
        float productionRate, // Food produced per day
        bool isRationing,
        int alertLevel,
        CrisisType crisisType,
        float deltaTimeDays)
    {
        // Calculate consumption
        float dailyConsumption = CalculateConsumptionRate(
            populationSize,
            isRationing,
            alertLevel,
            crisisType);

        // Net change (production - consumption)
        float netChange = productionRate - dailyConsumption;

        // Update food stockpile
        float foodChange = netChange * deltaTimeDays;
        stockpile.FoodDaysRemaining += foodChange / dailyConsumption;

        // Minimum 0 days (can't go negative)
        stockpile.FoodDaysRemaining = math.max(0f, stockpile.FoodDaysRemaining);

        // Similar for water, medicine, ammo
        // (Simplified here)
    }

    /// <summary>
    /// Determine if rationing should be enabled
    /// </summary>
    public static bool ShouldEnableRationing(
        in StockpileComponent stockpile,
        float stockpileTarget,
        int alertLevel)
    {
        // Enable rationing if below target or at alert 2+
        if (stockpile.FoodDaysRemaining < stockpileTarget)
            return true;

        if (alertLevel >= 2)
            return true;

        return false;
    }
}
```

### 5. Defense Preparation

```csharp
public static class DefensePreparationAlgorithms
{
    /// <summary>
    /// Calculate fortification progress based on labor and time
    /// </summary>
    public static void UpdateFortificationProgress(
        ref DefensePreparationComponent defense,
        int laborers,
        float laborerSkill, // 0-100
        float toolQuality, // 0-1
        float deltaTimeDays)
    {
        // Base progress per laborer per day
        float baseProgressPerDay = 1f; // 1% per laborer per day

        // Skill modifier
        float skillModifier = laborerSkill / 100f;

        // Tool quality modifier
        float toolModifier = 0.5f + (toolQuality * 0.5f); // 0.5-1.0

        // Calculate total progress
        float progressGain = baseProgressPerDay * laborers * skillModifier * toolModifier * deltaTimeDays;

        defense.FortificationProgress += progressGain;
        defense.FortificationProgress = math.min(100f, defense.FortificationProgress);
    }

    /// <summary>
    /// Calculate militia recruitment rate
    /// </summary>
    public static int CalculateMilitiaRecruitment(
        int populationSize,
        int alertLevel,
        float training TimeAvailable, // Days
        bool hasExistingMilitary)
    {
        // Base recruitment rate (% of population)
        float recruitmentRate = 0f;

        switch (alertLevel)
        {
            case 1: recruitmentRate = 0.05f; break; // 5% volunteer
            case 2: recruitmentRate = 0.15f; break; // 15% mobilize
            case 3: recruitmentRate = 0.30f; break; // 30% conscripted
            case 4: recruitmentRate = 0.50f; break; // 50% everyone who can fight
        }

        int maxRecruits = (int)(populationSize * recruitmentRate);

        // Training time limits effectiveness
        float trainingModifier = math.min(trainingTimeAvailable / 30f, 1f); // 30 days = full training

        int effectiveRecruits = (int)(maxRecruits * trainingModifier);

        return effectiveRecruits;
    }

    /// <summary>
    /// Calculate guard duty percentage based on alert
    /// </summary>
    public static float CalculateGuardsOnDuty(int alertLevel)
    {
        switch (alertLevel)
        {
            case 0: return 0.20f; // 20% on duty (normal shifts)
            case 1: return 0.40f; // 40% on duty (increased patrols)
            case 2: return 0.70f; // 70% on duty (high alert)
            case 3: return 1.00f; // 100% on duty (full mobilization)
            case 4: return 1.00f; // 100% + militia
            default: return 0.20f;
        }
    }
}
```

---

## ECS Systems

### Crisis Detection System (Aggregate ECS - 0.2 Hz)

```csharp
[UpdateInGroup(typeof(AggregateECSGroup))]
public partial struct CrisisDetectionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Query all detection systems
        foreach (var (observatory, entity) in
            SystemAPI.Query<RefRO<ObservatoryComponent>>()
                .WithEntityAccess())
        {
            // Check for potential crises in range
            // (Game-specific: query for approaching armies, weather systems, etc.)

            foreach (var (crisis, crisisEntity) in
                SystemAPI.Query<RefRO<CrisisComponent>>()
                    .WithEntityAccess())
            {
                if (crisis.ValueRO.IsPredicted)
                    continue; // Already detected

                float distance = CalculateDistance(entity, crisisEntity);

                float detectionChance = CrisisDetectionAlgorithms.CalculateDetectionChance(
                    in observatory.ValueRO,
                    crisis.ValueRO.Type,
                    distance,
                    crisis.ValueRO.TimeUntilImpact);

                if (UnityEngine.Random.Range(0f, 1f) < detectionChance)
                {
                    // Crisis detected! Raise alert
                    var detection = new CrisisDetectionComponent
                    {
                        DetectedCrisis = crisis.ValueRO.Type,
                        ConfidenceLevel = detectionChance,
                        TimeDetected = (float)SystemAPI.Time.ElapsedTime,
                        DetectionSource = entity,
                        AlertRaised = false
                    };

                    state.EntityManager.AddComponentData(crisisEntity, detection);
                }
            }
        }
    }

    private float CalculateDistance(Entity a, Entity b)
    {
        // Game-specific implementation
        return 0f;
    }
}
```

### Alert State Management System (Mind ECS - 1 Hz)

```csharp
[UpdateInGroup(typeof(MindECSGroup))]
public partial struct AlertStateManagementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (alertState, crisis) in
            SystemAPI.Query<RefRW<AlertStateComponent>, RefRO<CrisisComponent>>())
        {
            bool newEvidence = false; // Would check for new detections

            AlertStateAlgorithms.UpdateAlertState(
                ref alertState.ValueRW,
                in crisis.ValueRO,
                deltaTime,
                newEvidence);
        }
    }
}
```

### Crisis Response System (Mind ECS - 1 Hz)

```csharp
[UpdateInGroup(typeof(MindECSGroup))]
public partial struct CrisisResponseSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Entities respond to crises based on their role and alert level
        foreach (var (response, role, alertAwareness, stamina) in
            SystemAPI.Query<RefRW<CrisisResponseComponent>, RefRO<EntityRoleComponent>, RefRO<PopulationAlertAwarenessComponent>, RefRW<StaminaComponent>>())
        {
            // Determine response profile
            ResponseProfile chosenResponse = CrisisResponseAlgorithms.DetermineResponse(
                role.ValueRO.Role,
                alertAwareness.ValueRO.KnownCrisis,
                alertAwareness.ValueRO.KnownAlertLevel,
                role.ValueRO.Courage,
                role.ValueRO.HasFamily);

            response.ValueRW.Profile = chosenResponse;

            // Calculate work rate modifier
            response.ValueRW.WorkRateModifier = CrisisResponseAlgorithms.CalculateWorkRateModifier(
                chosenResponse,
                alertAwareness.ValueRO.KnownAlertLevel,
                stamina.ValueRO.CurrentStamina);

            // Apply exhaustion from overwork
            float hoursWorked = GetHoursWorked(response.ValueRO); // Game-specific

            CrisisResponseAlgorithms.ApplyExhaustionFromCrisis(
                ref stamina.ValueRW,
                response.ValueRO.WorkRateModifier,
                hoursWorked,
                SystemAPI.Time.DeltaTime);
        }
    }

    private float GetHoursWorked(in CrisisResponseComponent response)
    {
        // Game-specific implementation
        return 8f;
    }
}

public struct EntityRoleComponent : IComponentData
{
    public CrisisResponseAlgorithms.EntityRole Role;
    public float Courage; // 0-100
    public bool HasFamily;
}
```

### Stockpile Management System (Aggregate ECS - 0.2 Hz)

```csharp
[UpdateInGroup(typeof(AggregateECSGroup))]
public partial struct StockpileManagementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTimeDays = SystemAPI.Time.DeltaTime / 86400f; // Convert seconds to days

        foreach (var (stockpile, alertState, crisis, population) in
            SystemAPI.Query<RefRW<StockpileComponent>, RefRO<AlertStateComponent>, RefRO<CrisisComponent>, RefRO<PopulationComponent>>())
        {
            // Calculate stockpile target
            float target = PreparationAlgorithms.CalculateStockpileTarget(
                crisis.ValueRO.Type,
                crisis.ValueRO.Duration,
                population.ValueRO.Count,
                alertState.ValueRO.CurrentAlertLevel);

            stockpile.ValueRW.StockpileTarget = target;

            // Determine if rationing needed
            stockpile.ValueRW.IsRationing = PreparationAlgorithms.ShouldEnableRationing(
                in stockpile.ValueRO,
                target,
                alertState.ValueRO.CurrentAlertLevel);

            // Update stockpiles
            float productionRate = GetProductionRate(population.ValueRO); // Game-specific

            PreparationAlgorithms.UpdateStockpiles(
                ref stockpile.ValueRW,
                population.ValueRO.Count,
                productionRate,
                stockpile.ValueRO.IsRationing,
                alertState.ValueRO.CurrentAlertLevel,
                crisis.ValueRO.Type,
                deltaTimeDays);
        }
    }

    private float GetProductionRate(in PopulationComponent population)
    {
        // Game-specific implementation
        return 100f;
    }
}

public struct PopulationComponent : IComponentData
{
    public int Count;
}
```

---

## Summary

This agnostic framework provides the mathematical and structural foundation for crisis detection and response systems:

**Core Detection Methods:**
1. **Observatory**: Long-range prediction (weather, celestial, military movements)
2. **Scout Network**: Visual sighting (hours to days warning)
3. **Intelligence**: Political intel (weeks warning)
4. **Divination/Sensors**: Magical or technological foresight (variable warning)

**Alert State Hierarchy (0-4):**
- Normal (0) → Concerned (1) → Alarmed (2) → Crisis (3) → Desperate (4)
- Escalates based on threat severity and time remaining
- Decays gradually if crisis resolves

**Crisis-Specific Responses:**
- **Resource Shortage**: Workers × 2 effort, rationing, stockpiling
- **Infiltration**: Guards × 1.3 effort, curfews, searches
- **Invasion**: Militia recruitment, fortification, stockpile for siege
- **Natural Disaster**: Evacuate, reinforce buildings
- **Plague**: Quarantine, flee, stockpile medicine

**Preparation Benefits:**
- Early warning allows stockpiling (30 days food → 90 days)
- Fortification time (0% → 100% defensible)
- Militia training (untrained → basic trained)
- Evacuation (reduce casualties 70%)

**Work Rate Modifiers:**
- Double Effort: ×2.0 work rate (gatherers during famine)
- Rush Production: ×1.5 (craftsmen making weapons)
- Mobilization: ×1.3 (guards extended shifts)
- Exhaustion: Death if stamina = 0 for 24 hours

**Integration Points:**
- Body ECS: Work rate modifiers affect productivity
- Mind ECS: Fear, panic, decision-making changes
- Aggregate ECS: Settlement-wide alert propagation and stockpiling

This framework allows game implementations to create deep strategic crisis management where early detection and preparation dramatically increase survival rates.
