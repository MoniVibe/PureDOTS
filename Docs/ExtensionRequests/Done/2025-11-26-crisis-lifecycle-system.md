# Extension Request: Crisis Lifecycle Management System

**Status**: `[COMPLETED]`  
**Submitted**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Both games need multi-phase crisis management:

**Space4X:**
- Endgame crises (invasions, cataclysms, economic collapse)
- Crisis lifecycle: seeding → foreshadowing → emergence → escalation → resolution
- Player/AI responses affect outcomes
- Aftermath reshapes galaxy

**Godgame:**
- Seasonal crises (famine, plague, invasion)
- Natural disasters with warning signs
- Village-scale to region-scale events
- Recovery and rebuilding phases

Shared needs:
- Multi-phase lifecycle
- Tracker accumulation from triggers
- Intensity scaling
- Branching resolution paths
- Aftermath state changes

---

## Proposed Solution

**Extension Type**: New Components + Static Helpers

### Components

```csharp
/// <summary>
/// Phase of crisis lifecycle.
/// </summary>
public enum CrisisPhase : byte
{
    Dormant = 0,        // Not active, trackers accumulating
    Seeding = 1,        // Hidden triggers building up
    Foreshadowing = 2,  // Visible warnings
    Emergence = 3,      // Crisis begins
    Escalation = 4,     // Getting worse
    Climax = 5,         // Peak intensity
    Resolution = 6,     // Being resolved
    Aftermath = 7       // Post-crisis effects
}

/// <summary>
/// Type of crisis.
/// </summary>
public enum CrisisType : byte
{
    // External
    Invasion = 0,
    NaturalDisaster = 1,
    Pandemic = 2,
    
    // Internal
    EconomicCollapse = 10,
    Famine = 11,
    CivilWar = 12,
    Rebellion = 13,
    
    // Environmental
    ResourceDepletion = 20,
    ClimateShift = 21,
    Contamination = 22,
    
    // Supernatural/Tech
    AnomalyBreach = 30,
    AIUprising = 31,
    MagicSurge = 32
}

/// <summary>
/// Main crisis state component.
/// </summary>
public struct Crisis : IComponentData
{
    public CrisisType Type;
    public CrisisPhase Phase;
    public float Intensity;            // 0-1 current severity
    public float MaxIntensity;         // Highest reached
    public float TrackerValue;         // 0-1 progress to next phase
    public Entity OriginEntity;        // Where crisis started
    public Entity ScopeEntity;         // What area affected
    public uint StartTick;
    public uint PhaseStartTick;
    public uint EstimatedEndTick;
    public byte PlayerResponded;
}

/// <summary>
/// Crisis trigger conditions.
/// </summary>
[InternalBufferCapacity(4)]
public struct CrisisTrigger : IBufferElementData
{
    public FixedString32Bytes ConditionType;
    public float CurrentValue;
    public float ThresholdValue;
    public float ContributionWeight;   // How much this contributes
    public byte IsMet;
}

/// <summary>
/// Crisis tracker that accumulates toward triggering.
/// </summary>
public struct CrisisTracker : IComponentData
{
    public CrisisType TrackedType;
    public float AccumulatedValue;     // 0-1
    public float TriggerThreshold;     // When crisis triggers
    public float GrowthRate;           // Base accumulation rate
    public float DecayRate;            // Natural decay
    public uint LastUpdateTick;
}

/// <summary>
/// Crisis escalation milestone.
/// </summary>
[InternalBufferCapacity(4)]
public struct EscalationMilestone : IBufferElementData
{
    public float IntensityThreshold;
    public FixedString32Bytes EventType;
    public float ImpactMultiplier;
    public byte WasReached;
    public uint ReachedTick;
}

/// <summary>
/// Resolution path for crisis.
/// </summary>
[InternalBufferCapacity(4)]
public struct ResolutionPath : IBufferElementData
{
    public FixedString32Bytes PathId;
    public float SuccessChance;
    public float ResourceCost;
    public float TimeRequired;
    public float IntensityReduction;
    public byte RequiresAction;        // Needs player/AI input
}

/// <summary>
/// Aftermath effect from resolved crisis.
/// </summary>
[InternalBufferCapacity(8)]
public struct AftermathEffect : IBufferElementData
{
    public FixedString32Bytes EffectType;
    public float Magnitude;
    public uint DurationTicks;
    public uint AppliedTick;
    public byte IsPositive;            // Some crises have silver linings
}
```

### Static Helpers

```csharp
public static class CrisisHelpers
{
    /// <summary>
    /// Updates crisis tracker with contributions.
    /// </summary>
    public static void UpdateTracker(
        ref CrisisTracker tracker,
        in DynamicBuffer<CrisisTrigger> triggers,
        float deltaTime,
        uint currentTick)
    {
        // Calculate contribution from triggers
        float totalContribution = 0;
        for (int i = 0; i < triggers.Length; i++)
        {
            if (triggers[i].IsMet != 0)
            {
                float overflow = triggers[i].CurrentValue - triggers[i].ThresholdValue;
                totalContribution += math.max(0, overflow) * triggers[i].ContributionWeight;
            }
        }
        
        // Apply growth and decay
        float growth = (tracker.GrowthRate + totalContribution) * deltaTime;
        float decay = tracker.DecayRate * deltaTime;
        
        tracker.AccumulatedValue = math.saturate(tracker.AccumulatedValue + growth - decay);
        tracker.LastUpdateTick = currentTick;
    }

    /// <summary>
    /// Checks if crisis should advance phase.
    /// </summary>
    public static CrisisPhase CheckPhaseAdvancement(
        in Crisis crisis,
        uint currentTick,
        float phaseTimeMultiplier)
    {
        uint ticksInPhase = currentTick - crisis.PhaseStartTick;
        float timeThreshold = GetPhaseTimeThreshold(crisis.Phase) * phaseTimeMultiplier;
        
        if (ticksInPhase < timeThreshold)
            return crisis.Phase;
        
        return crisis.Phase switch
        {
            CrisisPhase.Dormant => CrisisPhase.Seeding,
            CrisisPhase.Seeding => CrisisPhase.Foreshadowing,
            CrisisPhase.Foreshadowing => CrisisPhase.Emergence,
            CrisisPhase.Emergence => CrisisPhase.Escalation,
            CrisisPhase.Escalation => CrisisPhase.Climax,
            CrisisPhase.Climax => CrisisPhase.Resolution,
            CrisisPhase.Resolution => CrisisPhase.Aftermath,
            _ => crisis.Phase
        };
    }

    private static float GetPhaseTimeThreshold(CrisisPhase phase)
    {
        return phase switch
        {
            CrisisPhase.Seeding => 5000,
            CrisisPhase.Foreshadowing => 3000,
            CrisisPhase.Emergence => 2000,
            CrisisPhase.Escalation => 4000,
            CrisisPhase.Climax => 2000,
            CrisisPhase.Resolution => 3000,
            _ => 1000
        };
    }

    /// <summary>
    /// Calculates intensity change this tick.
    /// </summary>
    public static float CalculateIntensityChange(
        in Crisis crisis,
        float externalPressure,
        float playerMitigation)
    {
        float baseChange = crisis.Phase switch
        {
            CrisisPhase.Seeding => 0.001f,
            CrisisPhase.Foreshadowing => 0.002f,
            CrisisPhase.Emergence => 0.005f,
            CrisisPhase.Escalation => 0.01f,
            CrisisPhase.Climax => 0.005f,
            CrisisPhase.Resolution => -0.01f,
            CrisisPhase.Aftermath => -0.02f,
            _ => 0
        };
        
        // External pressure accelerates
        baseChange *= 1f + externalPressure;
        
        // Player mitigation slows or reverses
        baseChange -= playerMitigation;
        
        return baseChange;
    }

    /// <summary>
    /// Gets best resolution path.
    /// </summary>
    public static int GetBestResolutionPath(
        in DynamicBuffer<ResolutionPath> paths,
        float availableResources,
        float availableTime)
    {
        int bestIdx = -1;
        float bestScore = -1;
        
        for (int i = 0; i < paths.Length; i++)
        {
            if (paths[i].ResourceCost > availableResources) continue;
            if (paths[i].TimeRequired > availableTime) continue;
            
            float score = paths[i].SuccessChance * paths[i].IntensityReduction;
            if (score > bestScore)
            {
                bestScore = score;
                bestIdx = i;
            }
        }
        
        return bestIdx;
    }

    /// <summary>
    /// Applies resolution attempt.
    /// </summary>
    public static bool AttemptResolution(
        ref Crisis crisis,
        in ResolutionPath path,
        uint seed)
    {
        var rng = new Random(seed);
        bool success = rng.NextFloat(0, 1) <= path.SuccessChance;
        
        if (success)
        {
            crisis.Intensity = math.max(0, crisis.Intensity - path.IntensityReduction);
            if (crisis.Intensity <= 0.1f)
                crisis.Phase = CrisisPhase.Aftermath;
        }
        
        return success;
    }

    /// <summary>
    /// Checks and applies escalation milestones.
    /// </summary>
    public static void CheckEscalationMilestones(
        in Crisis crisis,
        ref DynamicBuffer<EscalationMilestone> milestones,
        uint currentTick)
    {
        for (int i = 0; i < milestones.Length; i++)
        {
            var milestone = milestones[i];
            if (milestone.WasReached == 0 && crisis.Intensity >= milestone.IntensityThreshold)
            {
                milestone.WasReached = 1;
                milestone.ReachedTick = currentTick;
                milestones[i] = milestone;
            }
        }
    }

    /// <summary>
    /// Generates aftermath effects based on crisis outcome.
    /// </summary>
    public static void GenerateAftermath(
        ref DynamicBuffer<AftermathEffect> effects,
        in Crisis crisis,
        float resolutionQuality,
        uint currentTick)
    {
        // Worse crises leave worse aftermath
        float magnitude = crisis.MaxIntensity * (1f - resolutionQuality);
        
        effects.Add(new AftermathEffect
        {
            EffectType = "reconstruction",
            Magnitude = magnitude,
            DurationTicks = (uint)(10000 * magnitude),
            AppliedTick = currentTick,
            IsPositive = 0
        });
        
        // Some positive effects from surviving crisis
        if (resolutionQuality > 0.5f)
        {
            effects.Add(new AftermathEffect
            {
                EffectType = "resilience",
                Magnitude = resolutionQuality * 0.5f,
                DurationTicks = 20000,
                AppliedTick = currentTick,
                IsPositive = 1
            });
        }
    }

    /// <summary>
    /// Calculates scope expansion chance.
    /// </summary>
    public static bool ShouldExpandScope(
        in Crisis crisis,
        float proximityFactor,
        uint seed)
    {
        if (crisis.Phase < CrisisPhase.Escalation)
            return false;
        
        float expandChance = crisis.Intensity * 0.1f * proximityFactor;
        var rng = new Random(seed);
        return rng.NextFloat(0, 1) < expandChance;
    }
}
```

---

## Example Usage

```csharp
// === Space4X: Galaxy-wide crisis ===
var tracker = EntityManager.GetComponentData<CrisisTracker>(trackerEntity);
var triggers = EntityManager.GetBuffer<CrisisTrigger>(trackerEntity);

// Update tracker based on galaxy state
CrisisHelpers.UpdateTracker(ref tracker, triggers, deltaTime, currentTick);

// Check if crisis triggers
if (tracker.AccumulatedValue >= tracker.TriggerThreshold)
{
    var crisis = new Crisis
    {
        Type = CrisisType.Invasion,
        Phase = CrisisPhase.Seeding,
        Intensity = 0.1f,
        ScopeEntity = sectorEntity,
        StartTick = currentTick,
        PhaseStartTick = currentTick
    };
    EntityManager.AddComponentData(crisisEntity, crisis);
}

// Each tick, advance crisis
var crisis = EntityManager.GetComponentData<Crisis>(crisisEntity);
crisis.Phase = CrisisHelpers.CheckPhaseAdvancement(crisis, currentTick, 1f);

float intensityChange = CrisisHelpers.CalculateIntensityChange(
    crisis, externalPressure, playerMitigation);
crisis.Intensity = math.saturate(crisis.Intensity + intensityChange);

// Check escalation milestones
var milestones = EntityManager.GetBuffer<EscalationMilestone>(crisisEntity);
CrisisHelpers.CheckEscalationMilestones(crisis, ref milestones, currentTick);

// Player attempts resolution
var paths = EntityManager.GetBuffer<ResolutionPath>(crisisEntity);
int bestPath = CrisisHelpers.GetBestResolutionPath(paths, playerResources, timeRemaining);

if (bestPath >= 0)
{
    bool success = CrisisHelpers.AttemptResolution(ref crisis, paths[bestPath], currentTick);
    if (success) SpendResources(paths[bestPath].ResourceCost);
}

// === Godgame: Famine crisis ===
var famineTracker = EntityManager.GetComponentData<CrisisTracker>(famineTrackerEntity);

// Low food stores increase famine tracker
if (foodStores < foodThreshold)
{
    var trigger = new CrisisTrigger
    {
        ConditionType = "low_food",
        CurrentValue = 1f - (foodStores / foodThreshold),
        ThresholdValue = 0.5f,
        ContributionWeight = 0.5f,
        IsMet = foodStores < foodThreshold * 0.5f ? (byte)1 : (byte)0
    };
}

// Generate aftermath from resolved famine
var famineCrisis = EntityManager.GetComponentData<Crisis>(famineCrisisEntity);
if (famineCrisis.Phase == CrisisPhase.Aftermath)
{
    var effects = EntityManager.GetBuffer<AftermathEffect>(famineCrisisEntity);
    CrisisHelpers.GenerateAftermath(ref effects, famineCrisis, resolutionQuality, currentTick);
}
```

---

## Alternative Approaches Considered

- **Alternative 1**: Binary crisis on/off
  - **Rejected**: Both games need phased escalation and resolution

- **Alternative 2**: Game-specific crisis systems
  - **Rejected**: Core mechanics (phases, intensity, aftermath) are identical

---

## Implementation Notes

**Dependencies:**
- Random for resolution rolls
- Entity references for scope

**Performance Considerations:**
- Crisis updates can be throttled (every N ticks)
- Tracker updates are lightweight

**Related Requests:**
- Situations framework (smaller-scale crises)
- Event system (crisis-triggered events)

---

## Review Notes

*(PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

