# Extension Request: Threshold-Based Behavior Triggers

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Completed**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P2  

### Implementation
- `Packages/com.moni.puredots/Runtime/Runtime/AI/Threshold/ThresholdComponents.cs` - ThresholdState, ThresholdDirection, ThresholdActionType, ThresholdDefinition, ResourceThresholdState, ThresholdConfig, CommonThresholds
- `Packages/com.moni.puredots/Runtime/Runtime/AI/Threshold/ThresholdHelpers.cs` - Static helpers for threshold checking, urgency calculation, hysteresis

---

## Use Case

Both games need threshold-based behavior triggers for resource management:

**Space4X:**
- Strike craft recall at low ammo (< 20%), fuel (< 15%), or hull (< 25%)
- Automation policies: auto-repair when hull < 50%, auto-resupply when fuel < 30%
- Emergency protocols: flee when shields depleted, request reinforcements at 40% fleet strength

**Godgame:**
- Villagers flee when health < 30%
- Rest when energy < 20%, eat when hunger > 70%
- Abandon work and seek shelter when danger > 80%
- Migration triggers when village happiness < 40%

Shared needs:
- Multiple threshold levels (warning, critical, emergency)
- Hysteresis to prevent oscillation (different thresholds for entering/exiting states)
- Urgency calculation based on how far past threshold
- Configurable actions per threshold

---

## Proposed Solution

**Extension Type**: New Components + Static Helpers

### Components (`Packages/com.moni.puredots/Runtime/Runtime/AI/`)

```csharp
/// <summary>
/// State relative to a threshold.
/// </summary>
public enum ThresholdState : byte
{
    Safe = 0,           // Well above threshold
    Approaching = 1,    // Getting close to threshold
    Crossed = 2,        // Just crossed threshold
    Critical = 3,       // Far below threshold
    Recovering = 4      // Rising back toward safe
}

/// <summary>
/// Direction of threshold comparison.
/// </summary>
public enum ThresholdDirection : byte
{
    Below = 0,          // Trigger when value falls below threshold
    Above = 1           // Trigger when value rises above threshold
}

/// <summary>
/// Action to take when threshold is crossed.
/// </summary>
public enum ThresholdActionType : byte
{
    None = 0,
    Flee = 1,           // Retreat from danger
    Recall = 2,         // Return to base/carrier
    Resupply = 3,       // Seek supplies
    Rest = 4,           // Stop activity and recover
    Alert = 5,          // Notify player/AI
    Emergency = 6,      // Trigger emergency protocol
    Migrate = 7,        // Leave current location
    Request = 8         // Request assistance
}

/// <summary>
/// Definition of a single threshold.
/// </summary>
public struct ThresholdDefinition
{
    public float TriggerValue;         // Value at which to trigger
    public float RecoveryValue;        // Value to recover (hysteresis)
    public ThresholdDirection Direction;
    public ThresholdActionType Action;
    public half UrgencyMultiplier;     // How urgent when triggered
}

/// <summary>
/// Current state for a monitored resource.
/// </summary>
public struct ResourceThresholdState : IComponentData
{
    public float CurrentValue;
    public float MaxValue;
    public ThresholdState State;
    public ThresholdActionType ActiveAction;
    public half CurrentUrgency;        // 0-1, how urgent is the situation
    public uint StateChangedTick;
    public byte ActionInProgress;      // Is the triggered action being executed
}

/// <summary>
/// Configuration for threshold monitoring.
/// </summary>
[InternalBufferCapacity(4)]
public struct ThresholdConfig : IBufferElementData
{
    public FixedString32Bytes ResourceId;  // Which resource this monitors
    public ThresholdDefinition Warning;    // First level warning
    public ThresholdDefinition Critical;   // Serious threshold
    public ThresholdDefinition Emergency;  // Emergency threshold
    public byte IsEnabled;
}

/// <summary>
/// Event emitted when threshold is crossed.
/// </summary>
[InternalBufferCapacity(4)]
public struct ThresholdEvent : IBufferElementData
{
    public FixedString32Bytes ResourceId;
    public ThresholdState OldState;
    public ThresholdState NewState;
    public ThresholdActionType TriggeredAction;
    public float ValueAtTrigger;
    public uint Tick;
}

/// <summary>
/// Generic threshold set for common resources.
/// </summary>
public struct CommonThresholds : IComponentData
{
    // Health/Hull
    public half HealthWarning;         // e.g., 0.5 (50%)
    public half HealthCritical;        // e.g., 0.25 (25%)
    public half HealthEmergency;       // e.g., 0.1 (10%)
    
    // Energy/Fuel
    public half EnergyWarning;
    public half EnergyCritical;
    public half EnergyEmergency;
    
    // Ammo/Supplies
    public half SupplyWarning;
    public half SupplyCritical;
    public half SupplyEmergency;
    
    // Recovery values (for hysteresis)
    public half RecoveryMargin;        // How much above threshold to recover (e.g., 0.1 = +10%)
}
```

### Static Helpers

```csharp
public static class ThresholdHelpers
{
    /// <summary>
    /// Checks a value against a threshold definition.
    /// </summary>
    public static ThresholdState CheckThreshold(
        float currentValue,
        float maxValue,
        in ThresholdDefinition threshold,
        ThresholdState previousState)
    {
        float ratio = maxValue > 0 ? currentValue / maxValue : 0;
        
        bool isBelowTrigger = threshold.Direction == ThresholdDirection.Below
            ? ratio < threshold.TriggerValue
            : ratio > threshold.TriggerValue;
            
        bool isAboveRecovery = threshold.Direction == ThresholdDirection.Below
            ? ratio > threshold.RecoveryValue
            : ratio < threshold.RecoveryValue;

        // Hysteresis logic
        if (previousState == ThresholdState.Safe || previousState == ThresholdState.Approaching)
        {
            if (isBelowTrigger)
                return ThresholdState.Crossed;
            if (IsApproaching(ratio, threshold))
                return ThresholdState.Approaching;
            return ThresholdState.Safe;
        }
        else // Was triggered
        {
            if (isAboveRecovery)
                return ThresholdState.Safe;
            if (IsCritical(ratio, threshold))
                return ThresholdState.Critical;
            return ThresholdState.Recovering;
        }
    }

    /// <summary>
    /// Checks if value is approaching threshold.
    /// </summary>
    private static bool IsApproaching(float ratio, in ThresholdDefinition threshold)
    {
        float margin = 0.1f; // 10% warning before threshold
        if (threshold.Direction == ThresholdDirection.Below)
            return ratio < threshold.TriggerValue + margin && ratio >= threshold.TriggerValue;
        else
            return ratio > threshold.TriggerValue - margin && ratio <= threshold.TriggerValue;
    }

    /// <summary>
    /// Checks if value is critically past threshold.
    /// </summary>
    private static bool IsCritical(float ratio, in ThresholdDefinition threshold)
    {
        float criticalMargin = 0.5f; // 50% past threshold is critical
        if (threshold.Direction == ThresholdDirection.Below)
            return ratio < threshold.TriggerValue * criticalMargin;
        else
            return ratio > threshold.TriggerValue * (2f - criticalMargin);
    }

    /// <summary>
    /// Calculates urgency based on how far past threshold.
    /// </summary>
    public static float GetUrgency(
        float currentValue,
        float maxValue,
        in ThresholdDefinition threshold)
    {
        float ratio = maxValue > 0 ? currentValue / maxValue : 0;
        
        if (threshold.Direction == ThresholdDirection.Below)
        {
            if (ratio >= threshold.TriggerValue)
                return 0; // Not triggered
            
            // Linear urgency from trigger to zero
            return math.saturate(1f - ratio / threshold.TriggerValue) * (float)threshold.UrgencyMultiplier;
        }
        else
        {
            if (ratio <= threshold.TriggerValue)
                return 0; // Not triggered
                
            // Linear urgency from trigger to max
            float excess = ratio - threshold.TriggerValue;
            float maxExcess = 1f - threshold.TriggerValue;
            return math.saturate(excess / maxExcess) * (float)threshold.UrgencyMultiplier;
        }
    }

    /// <summary>
    /// Evaluates multiple thresholds and returns the most urgent.
    /// </summary>
    public static ThresholdState EvaluateThresholds(
        float currentValue,
        float maxValue,
        in ThresholdDefinition warning,
        in ThresholdDefinition critical,
        in ThresholdDefinition emergency,
        ThresholdState previousState,
        out ThresholdActionType action,
        out float urgency)
    {
        // Check emergency first (most severe)
        var emergencyState = CheckThreshold(currentValue, maxValue, emergency, previousState);
        if (emergencyState == ThresholdState.Crossed || emergencyState == ThresholdState.Critical)
        {
            action = emergency.Action;
            urgency = GetUrgency(currentValue, maxValue, emergency);
            return emergencyState;
        }

        // Check critical
        var criticalState = CheckThreshold(currentValue, maxValue, critical, previousState);
        if (criticalState == ThresholdState.Crossed || criticalState == ThresholdState.Critical)
        {
            action = critical.Action;
            urgency = GetUrgency(currentValue, maxValue, critical);
            return criticalState;
        }

        // Check warning
        var warningState = CheckThreshold(currentValue, maxValue, warning, previousState);
        if (warningState == ThresholdState.Crossed || warningState == ThresholdState.Approaching)
        {
            action = warning.Action;
            urgency = GetUrgency(currentValue, maxValue, warning);
            return warningState;
        }

        action = ThresholdActionType.None;
        urgency = 0;
        return ThresholdState.Safe;
    }

    /// <summary>
    /// Checks if action should trigger based on state.
    /// </summary>
    public static bool ShouldTriggerAction(ThresholdState state)
    {
        return state == ThresholdState.Crossed ||
               state == ThresholdState.Critical;
    }

    /// <summary>
    /// Checks if entity has recovered from threshold state.
    /// </summary>
    public static bool HasRecovered(ThresholdState state)
    {
        return state == ThresholdState.Safe;
    }

    /// <summary>
    /// Creates common threshold definitions for recall behavior.
    /// </summary>
    public static ThresholdDefinition CreateRecallThreshold(float triggerPercent, float recoveryPercent)
    {
        return new ThresholdDefinition
        {
            TriggerValue = triggerPercent,
            RecoveryValue = recoveryPercent,
            Direction = ThresholdDirection.Below,
            Action = ThresholdActionType.Recall,
            UrgencyMultiplier = (half)1f
        };
    }

    /// <summary>
    /// Creates common threshold definitions for flee behavior.
    /// </summary>
    public static ThresholdDefinition CreateFleeThreshold(float triggerPercent, float recoveryPercent)
    {
        return new ThresholdDefinition
        {
            TriggerValue = triggerPercent,
            RecoveryValue = recoveryPercent,
            Direction = ThresholdDirection.Below,
            Action = ThresholdActionType.Flee,
            UrgencyMultiplier = (half)1.5f
        };
    }
}
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Runtime/AI/ThresholdComponents.cs`
- New: `Packages/com.moni.puredots/Runtime/Runtime/AI/ThresholdHelpers.cs`
- Integration: Game-specific systems consume threshold events

**Breaking Changes:** None - entirely new feature

---

## Example Usage

```csharp
// === Space4X: Strike craft recall ===
var thresholds = new CommonThresholds
{
    SupplyWarning = (half)0.3f,      // 30% ammo warning
    SupplyCritical = (half)0.15f,    // 15% ammo critical
    SupplyEmergency = (half)0.05f,   // 5% ammo emergency
    RecoveryMargin = (half)0.2f      // Recover at +20% above threshold
};

// Each frame, check ammo level
var state = EntityManager.GetComponentData<ResourceThresholdState>(craftEntity);
float currentAmmo = GetCurrentAmmo(craftEntity);
float maxAmmo = GetMaxAmmo(craftEntity);

var warning = ThresholdHelpers.CreateRecallThreshold(
    (float)thresholds.SupplyWarning,
    (float)thresholds.SupplyWarning + (float)thresholds.RecoveryMargin);

var newState = ThresholdHelpers.CheckThreshold(
    currentAmmo, maxAmmo, warning, state.State);

if (ThresholdHelpers.ShouldTriggerAction(newState))
{
    // Trigger recall to carrier
    StartRecall(craftEntity);
}

// === Godgame: Villager flee behavior ===
var healthConfig = new ThresholdDefinition
{
    TriggerValue = 0.3f,           // Flee at 30% health
    RecoveryValue = 0.6f,          // Stop fleeing at 60% health
    Direction = ThresholdDirection.Below,
    Action = ThresholdActionType.Flee,
    UrgencyMultiplier = (half)2f   // High urgency when health low
};

float currentHealth = GetVillagerHealth(villagerEntity);
float maxHealth = GetVillagerMaxHealth(villagerEntity);

var fleeState = ThresholdHelpers.CheckThreshold(
    currentHealth, maxHealth, healthConfig, previousState);
float urgency = ThresholdHelpers.GetUrgency(currentHealth, maxHealth, healthConfig);

if (ThresholdHelpers.ShouldTriggerAction(fleeState))
{
    // Start fleeing with urgency affecting speed
    StartFlee(villagerEntity, urgency);
}
else if (ThresholdHelpers.HasRecovered(fleeState))
{
    // Safe to stop fleeing
    StopFlee(villagerEntity);
}

// === Automation policies ===
var autoRepairConfig = new ThresholdConfig
{
    ResourceId = "hull_integrity",
    Warning = new ThresholdDefinition
    {
        TriggerValue = 0.7f,
        RecoveryValue = 0.9f,
        Direction = ThresholdDirection.Below,
        Action = ThresholdActionType.Request,  // Request repairs
        UrgencyMultiplier = (half)0.5f
    },
    Critical = ThresholdHelpers.CreateRecallThreshold(0.4f, 0.6f),
    Emergency = ThresholdHelpers.CreateFleeThreshold(0.15f, 0.3f),
    IsEnabled = 1
};
```

---

## Alternative Approaches Considered

- **Alternative 1**: Simple boolean triggers
  - **Rejected**: No hysteresis leads to oscillation (constant triggering at boundary)

- **Alternative 2**: Game-specific implementations
  - **Rejected**: Identical logic needed for recalls (Space4X) and flee behaviors (Godgame)

- **Alternative 3**: Event-only system (no state tracking)
  - **Rejected**: Need to track "in progress" actions and recovery

---

## Implementation Notes

**Dependencies:**
- None - standalone utility

**Performance Considerations:**
- All calculations are simple math, burst-compatible
- Threshold configs are small fixed-size buffers
- State tracking avoids repeated action triggers

**Related Requests:**
- Morale tiers system (thresholds for morale bands)
- Supply chain utilities (consumption triggers resupply thresholds)

---

## Review Notes

*(PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

