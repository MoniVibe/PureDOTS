# Patience and Circadian Rhythm Systems

## Overview

Entities have patience levels (inversely tied to initiative) and circadian rhythms (sleep patterns) that diversify behavior and create emergent scheduling conflicts. High-initiative entities struggle to wait, listen, or rest, while low-initiative entities prefer patience and deliberation. Sleep patterns vary from night owls to early birds, affecting when entities are most productive.

**Key Principles**:
- **Patience inversely tied to initiative**: High initiative = low patience
- **Opt-in system**: Players can enable/disable behavioral complexity
- **Circadian diversity**: Night owls, early birds, split sleep, polyphasic, etc.
- **Behavioral consequences**: Impatient entities make rash decisions, patient entities miss opportunities
- **Learning impact**: Patience affects teaching/learning effectiveness
- **Cross-game**: Villagers (Godgame), crew/colonists (Space4X), both benefit from diversity

---

## Patience System

### Core Components

```csharp
public struct Patience : IComponentData
{
    public float PatienceRating;        // 0.0 to 1.0 (impatient to very patient)
    public float InitiativeRating;      // 0.0 to 1.0 (inverse relationship)
    public float CurrentPatience;       // Depletes when waiting
    public float PatienceRegenRate;     // Per second recovery

    // Breaking points
    public float WaitingThreshold;      // Max seconds can wait before acting
    public float ListeningThreshold;    // Max seconds can listen before interrupting
    public float LearningThreshold;     // Max seconds can study before distraction
    public float RestThreshold;         // Max seconds can rest before restlessness
}

public struct PatienceActivity : IComponentData
{
    public ActivityType CurrentActivity;
    public float ActivityStartTime;
    public float TimeInActivity;
    public bool IsPatientEnough;        // Can continue this activity?
    public float ImpatienceLevel;       // 0.0 to 1.0 (rising frustration)
}

public enum ActivityType : byte
{
    Idle = 0,
    Waiting = 1,            // Waiting for something
    Listening = 2,          // Being taught, in meeting
    Learning = 3,           // Studying, reading
    Resting = 4,            // Sleeping, relaxing
    Working = 5,            // Productive action
    Socializing = 6,        // Conversation
    Planning = 7,           // Strategic thinking
    Traveling = 8           // Long-distance movement
}
```

### Patience-Initiative Relationship

**Formula**: `Patience = 1.0 - (Initiative × InitiativeWeight)`

```csharp
[BurstCompile]
public partial struct PatienceCalculationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var patience in SystemAPI.Query<RefRW<Patience>>())
        {
            // Inverse relationship (configurable weight)
            float initiativeWeight = 0.8f; // 80% inverse correlation
            patience.ValueRW.PatienceRating = math.clamp(
                1.0f - (patience.ValueRO.InitiativeRating * initiativeWeight),
                0.1f,  // Min 10% patience (never zero)
                1.0f
            );

            // Calculate thresholds based on patience
            patience.ValueRW.WaitingThreshold = 10f + (patience.ValueRO.PatienceRating * 290f);     // 10s to 5min
            patience.ValueRW.ListeningThreshold = 5f + (patience.ValueRO.PatienceRating * 595f);    // 5s to 10min
            patience.ValueRW.LearningThreshold = 30f + (patience.ValueRO.PatienceRating * 1770f);   // 30s to 30min
            patience.ValueRW.RestThreshold = 60f + (patience.ValueRO.PatienceRating * 3540f);       // 1min to 1hr
        }
    }
}
```

### Patience Depletion

```csharp
[BurstCompile]
public partial struct PatienceDepletionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (patience, activity) in SystemAPI.Query<
            RefRW<Patience>,
            RefRW<PatienceActivity>>())
        {
            activity.ValueRW.TimeInActivity += deltaTime;

            // Check if activity requires patience
            if (RequiresPatience(activity.ValueRO.CurrentActivity))
            {
                // Deplete patience over time
                float depletionRate = GetDepletionRate(activity.ValueRO.CurrentActivity);
                patience.ValueRW.CurrentPatience -= depletionRate * deltaTime;

                // Calculate impatience level
                float threshold = GetThreshold(activity.ValueRO.CurrentActivity, patience.ValueRO);
                activity.ValueRW.ImpatienceLevel = math.clamp(
                    activity.ValueRO.TimeInActivity / threshold,
                    0f,
                    1.0f
                );

                // Check if patience exceeded
                activity.ValueRW.IsPatientEnough = activity.ValueRO.TimeInActivity < threshold;

                // Trigger impatience behavior if threshold exceeded
                if (!activity.ValueRO.IsPatientEnough)
{
                    TriggerImpatienceBehavior(activity.ValueRO.CurrentActivity);
                }
            }
            else
            {
                // Regenerate patience during non-waiting activities
                patience.ValueRW.CurrentPatience = math.min(
                    patience.ValueRO.PatienceRating,
                    patience.ValueRO.CurrentPatience + (patience.ValueRO.PatienceRegenRate * deltaTime)
                );
            }
        }
    }
}

private static bool RequiresPatience(ActivityType activity)
{
    return activity switch
    {
        ActivityType.Waiting => true,
        ActivityType.Listening => true,
        ActivityType.Learning => true,
        ActivityType.Resting => true,
        ActivityType.Socializing => true,  // Patience for long conversations
        ActivityType.Planning => true,     // Patience for deliberation
        _ => false
    };
}

private static float GetDepletionRate(ActivityType activity)
{
    return activity switch
    {
        ActivityType.Waiting => 0.1f,      // Waiting is tedious
        ActivityType.Listening => 0.05f,   // Listening is less tedious
        ActivityType.Learning => 0.03f,    // Learning is engaging
        ActivityType.Resting => 0.02f,     // Resting is natural
        ActivityType.Socializing => 0.04f,
        ActivityType.Planning => 0.06f,
        _ => 0f
    };
}
```

---

## Circadian Rhythm System

### Sleep Pattern Types

```csharp
public struct CircadianRhythm : IComponentData
{
    public SleepPatternType PatternType;
    public float PeakEnergyTime;        // Hour of day (0-24) when most energetic
    public float OptimalSleepTime;      // Hour of day (0-24) when prefers to sleep
    public float SleepDuration;         // Hours needed per cycle
    public float CurrentEnergy;         // 0.0 to 1.0
    public float CircadianPhase;        // 0.0 to 1.0 (position in 24hr cycle)
}

public enum SleepPatternType : byte
{
    // Standard patterns
    EarlyBird = 0,          // Wake 5am-7am, peak 8am-12pm, sleep 9pm-11pm
    Normal = 1,             // Wake 7am-9am, peak 10am-2pm, sleep 10pm-12am
    NightOwl = 2,           // Wake 10am-12pm, peak 2pm-6pm, sleep 1am-3am
    LateNight = 3,          // Wake 12pm-2pm, peak 4pm-8pm, sleep 3am-5am

    // Unusual patterns
    Nocturnal = 10,         // Awake at night, sleep during day (opposite)
    Polyphasic = 11,        // Multiple short sleep cycles (4-6 naps)
    SplitSleep = 12,        // Two distinct sleep periods (siesta style)
    Irregular = 13,         // No consistent pattern (chaotic)

    // Extreme patterns
    ShortSleeper = 20,      // Only needs 4-5 hours
    LongSleeper = 21,       // Needs 9-10 hours
    InsomniacTendency = 22, // Difficulty sleeping (reduced efficiency)
}

public struct SleepState : IComponentData
{
    public bool IsSleeping;
    public float TimeAsleep;            // Current sleep duration
    public float SleepQuality;          // 0.0 to 1.0 (how restful)
    public float SleepDebt;             // Accumulated sleep deficit
    public uint ConsecutiveMissedSleeps;
}
```

### Energy Curves by Pattern

```csharp
[BurstCompile]
public partial struct CircadianEnergySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var currentTime = GetCurrentGameTime(); // 0.0 to 24.0 hours
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (circadian, sleepState) in SystemAPI.Query<
            RefRW<CircadianRhythm>,
            RefRO<SleepState>>())
        {
            // Update circadian phase
            circadian.ValueRW.CircadianPhase = (currentTime % 24f) / 24f;

            // Calculate energy based on pattern
            float energyLevel = CalculateEnergyLevel(
                circadian.ValueRO.PatternType,
                currentTime,
                circadian.ValueRO.PeakEnergyTime,
                sleepState.ValueRO.SleepDebt
            );

            // Apply sleep debt penalty
            float sleepDebtPenalty = math.min(sleepState.ValueRO.SleepDebt * 0.1f, 0.5f); // Max -50%
            energyLevel -= sleepDebtPenalty;

            // Update current energy
            circadian.ValueRW.CurrentEnergy = math.clamp(energyLevel, 0f, 1.0f);
        }
    }

    private static float CalculateEnergyLevel(
        SleepPatternType pattern,
        float currentTime,
        float peakTime,
        float sleepDebt)
    {
        return pattern switch
        {
            SleepPatternType.EarlyBird => CalculateEarlyBirdEnergy(currentTime),
            SleepPatternType.Normal => CalculateNormalEnergy(currentTime),
            SleepPatternType.NightOwl => CalculateNightOwlEnergy(currentTime),
            SleepPatternType.LateNight => CalculateLateNightEnergy(currentTime),
            SleepPatternType.Nocturnal => CalculateNocturnalEnergy(currentTime),
            SleepPatternType.Polyphasic => CalculatePolyphasicEnergy(currentTime),
            SleepPatternType.SplitSleep => CalculateSplitSleepEnergy(currentTime),
            SleepPatternType.Irregular => CalculateIrregularEnergy(currentTime),
            _ => 0.5f
        };
    }

    // Example: Early bird energy curve
    private static float CalculateEarlyBirdEnergy(float hour)
    {
        // Peak 6am-12pm, low 6pm-12am
        if (hour >= 6f && hour < 12f)
            return 0.8f + (math.sin((hour - 6f) / 6f * math.PI) * 0.2f); // 0.8 to 1.0

        if (hour >= 12f && hour < 18f)
            return 0.6f - ((hour - 12f) / 6f * 0.2f); // 0.6 to 0.4

        if (hour >= 18f || hour < 6f)
            return 0.3f; // Low energy evening/night

        return 0.5f;
    }

    // Example: Night owl energy curve
    private static float CalculateNightOwlEnergy(float hour)
    {
        // Low morning, peak afternoon/evening
        if (hour >= 0f && hour < 10f)
            return 0.3f; // Very low morning

        if (hour >= 10f && hour < 16f)
            return 0.4f + ((hour - 10f) / 6f * 0.4f); // 0.4 to 0.8

        if (hour >= 16f && hour < 24f)
            return 0.8f + (math.sin((hour - 16f) / 8f * math.PI) * 0.2f); // 0.8 to 1.0

        return 0.5f;
    }

    // Polyphasic: Multiple peaks (every 4-6 hours)
    private static float CalculatePolyphasicEnergy(float hour)
    {
        // 4-hour cycles, micro-peaks after each nap
        float cycle = (hour % 4f) / 4f; // 0.0 to 1.0 over 4 hours
        return 0.6f + (math.sin(cycle * math.PI * 2f) * 0.3f); // Oscillates 0.3 to 0.9
    }
}
```

---

## Behavioral Consequences

### Impatience Behaviors

```csharp
public struct ImpatienceBehavior : IBufferElementData
{
    public BehaviorType Type;
    public float Severity;              // 0.0 to 1.0
    public uint TriggeredTick;
}

public enum BehaviorType : byte
{
    // Waiting impatience
    AbandonWait = 0,        // Stop waiting, do something else
    PaceAround = 1,         // Nervous movement
    ExpressAnger = 2,       // Complaint, frustration

    // Listening impatience
    Interrupt = 10,         // Cut off speaker
    StopListening = 11,     // Walk away mid-conversation
    Fidget = 12,            // Visible distraction

    // Learning impatience
    AbandonStudy = 20,      // Stop learning prematurely
    SkipAhead = 21,         // Try advanced content too soon
    SwitchSubject = 22,     // Jump to different topic

    // Resting impatience
    WakeEarly = 30,         // Can't sleep full duration
    RestlessSleep = 31,     // Poor sleep quality
    GetUp = 32,             // Stop resting early

    // Decision-making impatience
    RashDecision = 40,      // Act without full info
    Impulsive = 41,         // Ignore long-term consequences
    SkipPlanning = 42       // No strategic thinking
}
```

### Learning Impact

```csharp
public struct LearningSession : IComponentData
{
    public Entity Teacher;
    public Entity Student;
    public FixedString64Bytes Subject;
    public float SessionDuration;
    public float LearningEfficiency;    // Modified by patience

    // Patience factors
    public float StudentPatience;
    public float TeacherPatience;
    public bool StudentStillListening;
    public bool TeacherStillTeaching;
}

[BurstCompile]
public partial struct LearningPatienceSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (learning, studentPatience, teacherPatience) in SystemAPI.Query<
            RefRW<LearningSession>,
            RefRO<Patience>,  // Student
            RefRO<Patience>>()) // Teacher
        {
            // Student patience affects learning efficiency
            float studentFactor = studentPatience.ValueRO.CurrentPatience /
                                studentPatience.ValueRO.PatienceRating;

            // Teacher patience affects teaching quality
            float teacherFactor = teacherPatience.ValueRO.CurrentPatience /
                                teacherPatience.ValueRO.PatienceRating;

            // Combined efficiency
            learning.ValueRW.LearningEfficiency = (studentFactor + teacherFactor) / 2f;

            // Check if either runs out of patience
            learning.ValueRW.StudentStillListening =
                learning.ValueRO.SessionDuration < studentPatience.ValueRO.ListeningThreshold;

            learning.ValueRW.TeacherStillTeaching =
                learning.ValueRO.SessionDuration < teacherPatience.ValueRO.LearningThreshold;

            // If either leaves, session ends
            if (!learning.ValueRO.StudentStillListening || !learning.ValueRO.TeacherStillTeaching)
            {
                EndLearningSession(learning.ValueRO);
            }
        }
    }
}
```

---

## Scheduling Conflicts

### Meeting Coordination

```csharp
public struct MeetingRequest : IComponentData
{
    public Entity Organizer;
    public FixedList32Bytes<Entity> Participants;
    public float ProposedStartTime;     // Hour of day
    public float EstimatedDuration;     // Hours
    public float OptimalityScore;       // How well this fits everyone
}

[BurstCompile]
public partial struct MeetingSchedulingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var meeting in SystemAPI.Query<RefRW<MeetingRequest>>())
        {
            float totalOptimality = 0f;
            int participantCount = meeting.ValueRO.Participants.Length;

            for (int i = 0; i < participantCount; i++)
            {
                var participant = meeting.ValueRO.Participants[i];
                var circadian = GetCircadianRhythm(participant);

                // Calculate energy level at proposed time
                float energyAtTime = CalculateEnergyLevel(
                    circadian.PatternType,
                    meeting.ValueRO.ProposedStartTime,
                    circadian.PeakEnergyTime,
                    0f
                );

                totalOptimality += energyAtTime;
            }

            meeting.ValueRW.OptimalityScore = totalOptimality / participantCount;
        }
    }
}
```

**Example Conflicts**:
- Early bird warrior + night owl mage trying to plan strategy
- High-initiative scout can't wait for slow deliberation
- Nocturnal thief + diurnal guard have no overlap for coordination

---

## Example Scenarios

### Scenario 1: Impatient Scout vs Patient Strategist

```csharp
// Scout (high initiative, low patience)
var scoutPatience = new Patience
{
    InitiativeRating = 0.9f,        // Very high initiative
    PatienceRating = 0.18f,         // Very low patience (1.0 - 0.9*0.8 + 0.1)
    WaitingThreshold = 62f,         // Can only wait ~1 minute
    ListeningThreshold = 113f,      // Listens for ~2 minutes max
    LearningThreshold = 84f,        // Studies for ~1.5 minutes max
};

// Strategist (low initiative, high patience)
var strategistPatience = new Patience
{
    InitiativeRating = 0.2f,        // Low initiative
    PatienceRating = 0.84f,         // High patience (1.0 - 0.2*0.8)
    WaitingThreshold = 254f,        // Can wait ~4 minutes
    ListeningThreshold = 505f,      // Listens for ~8 minutes
    LearningThreshold = 1517f,      // Studies for ~25 minutes
};

// Conflict: Strategist wants to plan for 10 minutes
// Scout interrupts after 2 minutes, insists on immediate action
// Result: Suboptimal plan executed hastily
```

### Scenario 2: Night Owl Teacher + Early Bird Student

```csharp
// Teacher (night owl, peak 6pm)
var teacherCircadian = new CircadianRhythm
{
    PatternType = SleepPatternType.NightOwl,
    PeakEnergyTime = 18f,           // 6pm peak
    OptimalSleepTime = 2f,          // Sleeps 2am
    CurrentEnergy = CalculateNightOwlEnergy(8f) // 8am = 0.3 energy
};

// Student (early bird, peak 9am)
var studentCircadian = new CircadianRhythm
{
    PatternType = SleepPatternType.EarlyBird,
    PeakEnergyTime = 9f,            // 9am peak
    OptimalSleepTime = 22f,         // Sleeps 10pm
    CurrentEnergy = CalculateEarlyBirdEnergy(8f) // 8am = 0.9 energy
};

// Morning lesson (8am):
// - Student: 90% energy, great learning
// - Teacher: 30% energy, poor teaching quality
// - Combined efficiency: 60% (suboptimal)

// Evening lesson (6pm):
// - Student: 40% energy, struggling to stay focused
// - Teacher: 90% energy, great teaching
// - Combined efficiency: 65% (still suboptimal)

// Optimal: 2pm compromise
// - Student: 60% energy
// - Teacher: 70% energy
// - Combined efficiency: 65%
```

### Scenario 3: Polyphasic Crew vs Normal Colonists

```csharp
// Space4X: Polyphasic engineer (always available in shifts)
var engineer = new CircadianRhythm
{
    PatternType = SleepPatternType.Polyphasic,
    SleepDuration = 2f,             // 30min naps, 6 per day
    CurrentEnergy = 0.7f            // Consistent energy
};

// Normal colonist
var colonist = new CircadianRhythm
{
    PatternType = SleepPatternType.Normal,
    SleepDuration = 8f,             // 8 hour sleep
    CurrentEnergy = CalculateNormalEnergy(currentTime)
};

// Advantage: Engineer can work emergency repairs at any hour
// Disadvantage: Lower peak energy (0.9 max vs 1.0)
// Trade-off: Flexibility vs peak performance
```

---

## Integration with Other Systems

### Memory Tapping Integration

Patience affects rally participation:

```csharp
public static float GetRallyParticipationModifier(in Patience patience)
{
    // Impatient entities struggle to listen to long speeches
    // Patient entities absorb the message better
    return patience.PatienceRating * 0.5f + 0.5f; // 50% to 100% effectiveness
}
```

### Dialogue Integration

Patience affects conversation quality:

```csharp
public static bool WillListenToFullConversation(
    in Patience listener,
    float conversationDuration)
{
    return conversationDuration < listener.ListeningThreshold;
}

public static float GetConversationQuality(
    in Patience speaker,
    in Patience listener)
{
    // Both need patience for good conversation
    return (speaker.PatienceRating + listener.PatienceRating) / 2f;
}
```

### Combat Integration

Circadian rhythm affects combat effectiveness:

```csharp
public static float GetCombatReadinessModifier(in CircadianRhythm circadian)
{
    // Low energy = lower combat effectiveness
    return 0.5f + (circadian.CurrentEnergy * 0.5f); // 50% to 100%
}
```

---

## Opt-In Configuration

```csharp
public struct BehavioralComplexitySettings : IComponentData
{
    public bool EnablePatienceSystem;
    public bool EnableCircadianRhythms;
    public bool EnableSchedulingConflicts;
    public bool ShowPatienceIndicators;     // UI feedback
    public bool ShowEnergyLevels;           // UI feedback

    // Difficulty modifiers
    public float PatienceVariability;       // 0.0 to 1.0 (how much variance)
    public float CircadianImpact;           // 0.0 to 1.0 (how much energy affects performance)
}
```

---

## Performance Targets

```
Patience Calculation:     <0.05ms per entity
Patience Depletion:       <0.03ms per entity
Circadian Energy:         <0.05ms per entity
Meeting Scheduling:       <0.2ms per meeting (10 participants)
────────────────────────────────────────
Total (1000 entities):    <130ms per frame
```

---

## Summary

**Patience System**:
- Inversely tied to initiative (high initiative = low patience)
- Depletes during waiting, listening, learning, resting
- Regenerates during active tasks
- Thresholds: 10s-5min waiting, 5s-10min listening, 30s-30min learning
- Triggers impatience behaviors when exceeded

**Circadian Rhythms** (8 pattern types):
- Early bird, normal, night owl, late night
- Nocturnal, polyphasic, split sleep, irregular
- Energy curves based on time of day
- Sleep debt accumulation
- Peak performance windows

**Behavioral Consequences**:
- Impatient entities: Interrupt, abandon learning, make rash decisions
- Patient entities: Better learning, strategic planning, miss opportunities
- Scheduling conflicts: Meetings suboptimal for mixed circadian types
- Reduced efficiency: Teaching/learning when energy misaligned

**Integration Points**:
- Memory tapping: Patience affects rally participation (50-100%)
- Dialogue: Conversation quality requires mutual patience
- Combat: Energy affects combat readiness (50-100%)
- Learning: Both teacher and student patience determine efficiency

**Opt-In**: Players can enable/disable for desired behavioral complexity.
