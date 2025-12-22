# Miscommunication System

## Overview

The Miscommunication System introduces controlled chaos and varied outcomes to entity interactions. Messages can be misinterpreted, falsified, delayed, or lost entirely. Minor miscommunications cause small misunderstandings (wrong prices, slight intent shifts), while critical miscommunications can escalate to violence if not caught and corrected. Commands may arrive too late, too early, or never at all. Intentions may be misread. Entities can learn from miscommunications and improve their communication skills over time.

**Key Principles**:
- **Severity levels**: Minor (price differences, slight intent shifts) to Critical (provocative misunderstandings, false commands)
- **Timing failures**: Commands can arrive late, early, or never
- **Intent misinterpretation**: True intentions can be misread, leading to unexpected reactions
- **Escalation checks**: Critical miscommunications checked multiple times before violence
- **Learning system**: Entities improve communication skills through experience
- **Deterministic**: Same clarity + same conditions = same miscommunication chance
- **Cross-game**: Applies to Godgame (villager interactions) and Space4X (fleet commands, diplomatic messages)
- **Burst-friendly**: All calculations Burst-compatible

---

## Miscommunication Types

### Severity Classification

```csharp
public enum MiscommunicationSeverity : byte
{
    None = 0,               // No miscommunication
    Minor = 1,              // Small misunderstandings (price differences, slight intent shifts)
    Moderate = 2,           // Noticeable errors (wrong direction, misunderstood request)
    Major = 3,              // Significant errors (wrong target, misinterpreted command)
    Critical = 4,           // Provocative misunderstandings (hostile intent misread as peaceful)
    Catastrophic = 5        // Complete communication breakdown (command inverted, total misread)
}

public struct MiscommunicationEvent : IComponentData
{
    public Entity Sender;
    public Entity Receiver;
    public CommunicationAttempt OriginalAttempt;
    public CommunicationAttempt MisreadAttempt;      // What was actually understood
    public MiscommunicationSeverity Severity;
    public MiscommunicationType Type;
    public float ClarityAtTime;                      // Clarity when miscommunication occurred
    public uint OccurredTick;
    public bool WasCorrected;                        // Did entities catch and correct?
    public uint CorrectionTick;                     // When was it corrected (if ever)
    public bool EscalatedToViolence;                 // Did this lead to combat?
}
```

### Miscommunication Categories

```csharp
public enum MiscommunicationType : byte
{
    // Content miscommunications
    IntentMisread = 0,              // Peaceful intent read as hostile (or vice versa)
    PriceMisread = 1,               // Trade price misunderstood (10 gold vs 100 gold)
    DirectionMisread = 2,           // Wrong direction given/received
    TargetMisread = 3,              // Wrong target identified
    CommandMisread = 4,             // Tactical command misunderstood
    RequestMisread = 5,             // Request interpreted differently
    WarningMisread = 6,             // Warning not understood or inverted
    OfferMisread = 7,               // Trade offer misread (item, quantity, value)

    // Timing miscommunications
    CommandDelayed = 10,            // Command arrived late
    CommandEarly = 11,              // Command arrived too early (confusion)
    CommandLost = 12,               // Command never arrived
    MessageDelayed = 13,            // Message delayed (diplomatic, trade)
    MessageLost = 14,               // Message never received

    // Deception miscommunications
    DeceptionUndetected = 20,       // Deception not caught (false information accepted)
    DeceptionFalsePositive = 21,    // Honest message read as deceptive
    IntentDisguised = 22,           // True intent hidden (deception system)

    // Cultural miscommunications
    CulturalTaboo = 30,              // Cultural misunderstanding (offensive gesture)
    CulturalCustom = 31,            // Custom misinterpreted (greeting vs threat)
    LanguageNuance = 32,            // Nuance lost in translation

    // Context miscommunications
    ContextMissing = 40,             // Missing context caused misread
    ContextInverted = 41,            // Context inverted meaning
    SarcasmMissed = 42,              // Sarcasm taken literally
    JokeMissed = 43                  // Joke taken seriously
}
```

---

## Miscommunication Calculation

### Base Miscommunication Chance

```csharp
public struct MiscommunicationChance : IComponentData
{
    public float BaseChance;                        // Base miscommunication probability
    public float ClarityModifier;                   // How clarity affects chance
    public float LanguageModifier;                  // Language proficiency effects
    public float CulturalModifier;                  // Cultural differences
    public float ContextModifier;                   // Context availability
    public float RelationModifier;                  // Relations affect interpretation
    public float ExperienceModifier;                // Communication experience
    public float TotalChance;                       // Final calculated chance
}

[BurstCompile]
public partial struct MiscommunicationCalculationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (attempt, miscommChance, senderLang, receiverLang) in SystemAPI.Query<
            RefRO<CommunicationAttempt>,
            RefRW<MiscommunicationChance>,
            DynamicBuffer<LanguageProficiency>,  // Sender languages
            DynamicBuffer<LanguageProficiency>>() // Receiver languages
            .WithAll<CommunicationAttempt>())
        {
            CalculateMiscommunicationChance(
                ref miscommChance.ValueRW,
                attempt.ValueRO,
                senderLang,
                receiverLang);
        }
    }

    private void CalculateMiscommunicationChance(
        ref MiscommunicationChance miscomm,
        in CommunicationAttempt attempt,
        DynamicBuffer<LanguageProficiency> senderLang,
        DynamicBuffer<LanguageProficiency> receiverLang)
    {
        // Base chance depends on communication method
        miscomm.BaseChance = attempt.MethodUsed switch
        {
            CommunicationMethod.NativeLanguage => 0.02f,      // 2% (very low)
            CommunicationMethod.KnownLanguage => 0.10f,       // 10% (moderate)
            CommunicationMethod.GeneralSigns => 0.30f,       // 30% (high)
            CommunicationMethod.Empathy => 0.15f,            // 15% (emotion reading)
            CommunicationMethod.Telepathy => 0.05f,          // 5% (direct mind link)
            _ => 0.50f                                       // 50% (failed communication)
        };

        // Clarity modifier: Lower clarity = higher miscommunication chance
        // Formula: BaseChance × (1 - Clarity)
        // Example: 10% base, 0.6 clarity → 10% × (1 - 0.6) = 4% additional
        miscomm.ClarityModifier = miscomm.BaseChance * (1f - attempt.Clarity);

        // Language proficiency modifier
        float senderProf = GetAverageProficiency(senderLang);
        float receiverProf = GetAverageProficiency(receiverLang);
        float minProf = math.min(senderProf, receiverProf);
        // Lower proficiency = higher miscommunication
        miscomm.LanguageModifier = miscomm.BaseChance * (1f - minProf) * 0.5f;

        // Cultural differences (if different cultures)
        bool differentCultures = AreDifferentCultures(attempt.Sender, attempt.Receiver);
        miscomm.CulturalModifier = differentCultures ? miscomm.BaseChance * 0.3f : 0f;

        // Context modifier (if context is obvious, reduce miscommunication)
        bool hasContext = HasObviousContext(attempt.Intent);
        miscomm.ContextModifier = hasContext ? -miscomm.BaseChance * 0.2f : 0f;

        // Relation modifier (better relations = more benefit of doubt)
        float relations = GetRelationValue(attempt.Receiver, attempt.Sender);
        // Relations -1.0 to 1.0, convert to 0-1 and invert for modifier
        float relationFactor = (relations + 1f) / 2f; // 0.0 (enemy) to 1.0 (friend)
        miscomm.RelationModifier = -miscomm.BaseChance * relationFactor * 0.3f; // Up to -30% reduction

        // Experience modifier (entities learn from past miscommunications)
        float senderExperience = GetCommunicationExperience(attempt.Sender);
        float receiverExperience = GetCommunicationExperience(attempt.Receiver);
        float avgExperience = (senderExperience + receiverExperience) / 2f;
        miscomm.ExperienceModifier = -miscomm.BaseChance * avgExperience * 0.2f; // Up to -20% reduction

        // Total chance (clamped to 0-1)
        miscomm.TotalChance = math.clamp(
            miscomm.BaseChance +
            miscomm.ClarityModifier +
            miscomm.LanguageModifier +
            miscomm.CulturalModifier +
            miscomm.ContextModifier +
            miscomm.RelationModifier +
            miscomm.ExperienceModifier,
            0f,
            0.95f // Cap at 95% (always some chance of understanding)
        );
    }
}
```

---

## Miscommunication Resolution

### Determining Miscommunication Type and Severity

```csharp
[BurstCompile]
public partial struct MiscommunicationResolutionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var random = new Unity.Mathematics.Random((uint)SystemAPI.Time.ElapsedTicks);

        foreach (var (attempt, miscommChance) in SystemAPI.Query<
            RefRW<CommunicationAttempt>,
            RefRO<MiscommunicationChance>>())
        {
            // Roll for miscommunication
            if (random.NextFloat() < miscommChance.ValueRO.TotalChance)
            {
                // Miscommunication occurred!
                var miscommEvent = ResolveMiscommunication(
                    attempt.ValueRO,
                    miscommChance.ValueRO,
                    ref random);

                // Apply miscommunication
                ApplyMiscommunication(ref attempt.ValueRW, miscommEvent);
            }
        }
    }

    private MiscommunicationEvent ResolveMiscommunication(
        in CommunicationAttempt attempt,
        in MiscommunicationChance chance,
        ref Unity.Mathematics.Random random)
    {
        // Determine severity based on total chance and intent complexity
        MiscommunicationSeverity severity = DetermineSeverity(attempt, chance.TotalChance, ref random);

        // Determine type based on intent and severity
        MiscommunicationType type = DetermineType(attempt.Intent, severity, ref random);

        // Create misread attempt
        CommunicationAttempt misread = CreateMisreadAttempt(attempt, type, severity, ref random);

        return new MiscommunicationEvent
        {
            Sender = attempt.Sender,
            Receiver = attempt.Receiver,
            OriginalAttempt = attempt,
            MisreadAttempt = misread,
            Severity = severity,
            Type = type,
            ClarityAtTime = attempt.Clarity,
            OccurredTick = (uint)SystemAPI.Time.ElapsedTicks,
            WasCorrected = false,
            EscalatedToViolence = false
        };
    }

    private MiscommunicationSeverity DetermineSeverity(
        in CommunicationAttempt attempt,
        float totalChance,
        ref Unity.Mathematics.Random random)
    {
        // Higher miscommunication chance = more severe outcomes
        if (totalChance > 0.7f)
        {
            // 70%+ chance: Critical or Catastrophic
            return random.NextFloat() < 0.3f ? MiscommunicationSeverity.Catastrophic : MiscommunicationSeverity.Critical;
        }
        else if (totalChance > 0.4f)
        {
            // 40-70%: Major or Critical
            return random.NextFloat() < 0.5f ? MiscommunicationSeverity.Major : MiscommunicationSeverity.Critical;
        }
        else if (totalChance > 0.2f)
        {
            // 20-40%: Moderate or Major
            return random.NextFloat() < 0.6f ? MiscommunicationSeverity.Moderate : MiscommunicationSeverity.Major;
        }
        else
        {
            // <20%: Minor or Moderate
            return random.NextFloat() < 0.7f ? MiscommunicationSeverity.Minor : MiscommunicationSeverity.Moderate;
        }
    }

    private MiscommunicationType DetermineType(
        CommunicationIntent intent,
        MiscommunicationSeverity severity,
        ref Unity.Mathematics.Random random)
    {
        // Type depends on original intent and severity
        switch (intent)
        {
            case CommunicationIntent.PeacefulIntent:
                if (severity >= MiscommunicationSeverity.Critical)
                    return MiscommunicationType.IntentMisread; // Read as hostile
                return MiscommunicationType.ContextMissing;

            case CommunicationIntent.HostileIntent:
                if (severity >= MiscommunicationSeverity.Major)
                    return MiscommunicationType.IntentMisread; // Read as peaceful (dangerous!)
                return MiscommunicationType.WarningMisread;

            case CommunicationIntent.TradeOfferSpecific:
            case CommunicationIntent.PriceNegotiation:
                if (severity >= MiscommunicationSeverity.Moderate)
                    return MiscommunicationType.PriceMisread;
                return MiscommunicationType.OfferMisread;

            case CommunicationIntent.AskForDirections:
            case CommunicationIntent.ProvideDirections:
                return MiscommunicationType.DirectionMisread;

            case CommunicationIntent.Warning:
                if (severity >= MiscommunicationSeverity.Major)
                    return MiscommunicationType.WarningMisread; // Warning inverted or ignored
                return MiscommunicationType.ContextMissing;

            default:
                // Generic misread based on severity
                return severity switch
                {
                    MiscommunicationSeverity.Critical => MiscommunicationType.IntentMisread,
                    MiscommunicationSeverity.Major => MiscommunicationType.RequestMisread,
                    MiscommunicationSeverity.Moderate => MiscommunicationType.ContextMissing,
                    _ => MiscommunicationType.RequestMisread
                };
        }
    }

    private CommunicationAttempt CreateMisreadAttempt(
        in CommunicationAttempt original,
        MiscommunicationType type,
        MiscommunicationSeverity severity,
        ref Unity.Mathematics.Random random)
    {
        CommunicationIntent misreadIntent = original.Intent;

        switch (type)
        {
            case MiscommunicationType.IntentMisread:
                // Invert intent (peaceful ↔ hostile)
                misreadIntent = original.Intent switch
                {
                    CommunicationIntent.PeacefulIntent => CommunicationIntent.HostileIntent,
                    CommunicationIntent.HostileIntent => CommunicationIntent.PeacefulIntent,
                    CommunicationIntent.NeutralIntent => random.NextBool() ?
                        CommunicationIntent.PeacefulIntent : CommunicationIntent.HostileIntent,
                    _ => CommunicationIntent.Incomprehensible
                };
                break;

            case MiscommunicationType.PriceMisread:
                // Price misunderstood (10x difference or inverted)
                // Handled separately in trade system
                break;

            case MiscommunicationType.DirectionMisread:
                // Wrong direction (opposite or perpendicular)
                misreadIntent = CommunicationIntent.ProvideDirections; // Will have wrong direction data
                break;

            case MiscommunicationType.CommandMisread:
                // Tactical command misunderstood
                // Handled in tactical command system
                break;

            case MiscommunicationType.WarningMisread:
                // Warning inverted or ignored
                if (severity >= MiscommunicationSeverity.Major)
                    misreadIntent = CommunicationIntent.Incomprehensible; // Warning lost
                break;

            default:
                // Generic misread
                misreadIntent = CommunicationIntent.Incomprehensible;
                break;
        }

        return new CommunicationAttempt
        {
            Sender = original.Sender,
            Receiver = original.Receiver,
            MethodUsed = original.MethodUsed,
            Intent = original.Intent, // Original intent preserved
            Result = misreadIntent,   // But result is misread
            Clarity = original.Clarity * 0.3f, // Clarity drops significantly
            WasDeceptive = original.WasDeceptive,
            WasDetected = original.WasDetected
        };
    }
}
```

---

## Timing Miscommunications

### Command Timing Failures

```csharp
public struct CommandTiming : IComponentData
{
    public Entity Commander;
    public Entity Recipient;
    public TacticalCommand OriginalCommand;
    public uint IntendedTick;                    // When command should arrive
    public uint ActualArrivalTick;               // When command actually arrived
    public CommandTimingStatus Status;
    public float Reliability;                    // Communication reliability (0.0 to 1.0)
}

public enum CommandTimingStatus : byte
{
    OnTime = 0,              // Arrived at intended time
    Early = 1,               // Arrived too early (confusion)
    Late = 2,                // Arrived late (may be too late to act)
    Lost = 3,                // Never arrived
    Corrupted = 4            // Arrived but corrupted (partial command)
}

[BurstCompile]
public partial struct CommandTimingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var currentTick = (uint)SystemAPI.Time.ElapsedTicks;

        foreach (var (timing, command) in SystemAPI.Query<
            RefRW<CommandTiming>,
            RefRO<TacticalCommand>>())
        {
            // Check if command should have arrived by now
            if (currentTick >= timing.ValueRO.IntendedTick)
            {
                // Command should have arrived
                if (timing.ValueRO.ActualArrivalTick == 0)
                {
                    // Hasn't arrived yet - check if lost
                    if (ShouldCommandBeLost(timing.ValueRO, currentTick))
                    {
                        timing.ValueRW.Status = CommandTimingStatus.Lost;
                        timing.ValueRW.ActualArrivalTick = currentTick; // Mark as "arrived" but lost
                    }
                    else if (IsCommandLate(timing.ValueRO, currentTick))
                    {
                        timing.ValueRW.Status = CommandTimingStatus.Late;
                        timing.ValueRW.ActualArrivalTick = currentTick;
                    }
                }
            }
            else if (timing.ValueRO.ActualArrivalTick > 0 &&
                     timing.ValueRO.ActualArrivalTick < timing.ValueRO.IntendedTick)
            {
                // Arrived early
                timing.ValueRW.Status = CommandTimingStatus.Early;
            }
        }
    }

    private bool ShouldCommandBeLost(in CommandTiming timing, uint currentTick)
    {
        // Command is lost if:
        // - Reliability is very low (< 0.3)
        // - Too much time has passed (> 3x intended delay)
        uint intendedDelay = timing.IntendedTick - (timing.IntendedTick - 100); // Assume 100 tick base delay
        uint actualDelay = currentTick - (timing.IntendedTick - intendedDelay);

        if (timing.Reliability < 0.3f)
            return true;

        if (actualDelay > intendedDelay * 3)
            return true;

        return false;
    }

    private bool IsCommandLate(in CommandTiming timing, uint currentTick)
    {
        // Command is late if it arrives after intended tick
        return currentTick > timing.IntendedTick;
    }
}

// Calculate communication reliability for commands
public static float CalculateCommandReliability(
    Entity commander,
    Entity recipient,
    float distance,
    CommunicationMethod method,
    float weatherSeverity,
    float terrainDifficulty)
{
    float baseReliability = method switch
    {
        CommunicationMethod.Verbal => distance < 50f ? 0.9f : 0.3f, // Shouting range
        CommunicationMethod.Visual => distance < 100f ? 0.8f : 0f,  // Line of sight
        CommunicationMethod.Horn => distance < 200f ? 0.85f : 0.2f, // Horn range
        CommunicationMethod.Drum => distance < 150f ? 0.75f : 0.1f, // Drum range
        CommunicationMethod.Flag => distance < 300f ? 0.7f : 0f,    // Flag range (line of sight)
        CommunicationMethod.Magical => 0.98f,                       // Magic is reliable
        _ => 0.5f
    };

    // Distance penalty
    float distancePenalty = method switch
    {
        CommunicationMethod.Verbal => math.clamp(1f - (distance / 50f), 0.1f, 1f),
        CommunicationMethod.Visual => math.clamp(1f - (distance / 100f), 0f, 1f),
        CommunicationMethod.Horn => math.clamp(1f - (distance / 200f), 0.1f, 1f),
        CommunicationMethod.Drum => math.clamp(1f - (distance / 150f), 0.1f, 1f),
        CommunicationMethod.Flag => math.clamp(1f - (distance / 300f), 0f, 1f),
        CommunicationMethod.Magical => 1f, // No distance penalty
        _ => 1f
    };

    // Weather penalty (rain, fog, storms reduce reliability)
    float weatherPenalty = 1f - (weatherSeverity * 0.4f);

    // Terrain penalty (hills, forests block signals)
    float terrainPenalty = 1f - (terrainDifficulty * 0.3f);

    float reliability = baseReliability * distancePenalty * weatherPenalty * terrainPenalty;
    return math.clamp(reliability, 0.05f, 0.99f);
}
```

---

## Critical Miscommunication Escalation

### Escalation Prevention System

Critical miscommunications (especially intent misreads) can lead to violence. The system checks multiple times before allowing escalation:

```csharp
public struct MiscommunicationEscalation : IComponentData
{
    public Entity Sender;
    public Entity Receiver;
    public MiscommunicationEvent MiscommEvent;
    public uint CheckCount;                      // Number of escalation checks performed
    public uint MaxChecks;                       // Maximum checks before allowing escalation
    public float EscalationChance;               // Current chance of escalation
    public bool EscalationBlocked;               // Was escalation prevented?
    public uint LastCheckTick;
}

[BurstCompile]
public partial struct MiscommunicationEscalationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var currentTick = (uint)SystemAPI.Time.ElapsedTicks;

        foreach (var (escalation, miscomm, relations) in SystemAPI.Query<
            RefRW<MiscommunicationEscalation>,
            RefRO<MiscommunicationEvent>,
            RefRO<RelationValue>>())
        {
            // Only check critical miscommunications
            if (miscomm.ValueRO.Severity < MiscommunicationSeverity.Critical)
                continue;

            // Perform escalation check
            if (ShouldPerformEscalationCheck(escalation.ValueRO, currentTick))
            {
                bool shouldEscalate = PerformEscalationCheck(
                    miscomm.ValueRO,
                    relations.ValueRO,
                    escalation.ValueRO.CheckCount);

                if (shouldEscalate && escalation.ValueRO.CheckCount >= escalation.ValueRO.MaxChecks)
                {
                    // Escalation allowed after multiple checks
                    escalation.ValueRW.EscalationChance = 1.0f;
                    // Trigger violence/combat
                    TriggerEscalationToViolence(miscomm.ValueRO);
                }
                else if (!shouldEscalate)
                {
                    // Escalation prevented
                    escalation.ValueRW.EscalationBlocked = true;
                    // Try to correct miscommunication
                    AttemptMiscommunicationCorrection(miscomm.ValueRO);
                }

                escalation.ValueRW.CheckCount++;
                escalation.ValueRW.LastCheckTick = currentTick;
            }
        }
    }

    private bool ShouldPerformEscalationCheck(in MiscommunicationEscalation escalation, uint currentTick)
    {
        // Perform check every 60 ticks (1 second at 60 TPS)
        if (currentTick - escalation.LastCheckTick < 60)
            return false;

        // Don't check if already blocked or escalated
        if (escalation.EscalationBlocked || escalation.EscalationChance >= 1.0f)
            return false;

        return true;
    }

    private bool PerformEscalationCheck(
        in MiscommunicationEvent miscomm,
        in RelationValue relations,
        uint checkCount)
    {
        // Each check has a chance to prevent escalation
        // Factors that prevent escalation:
        // - Good relations (friends don't fight over miscommunications)
        // - High communication experience (entities learn to clarify)
        // - Context clues (obvious context reduces escalation)
        // - Multiple checks (more checks = more chances to catch error)

        float relationFactor = (relations.Value + 1f) / 2f; // 0.0 (enemy) to 1.0 (friend)
        float experienceFactor = (GetCommunicationExperience(miscomm.Sender) +
                                  GetCommunicationExperience(miscomm.Receiver)) / 2f;
        float contextFactor = HasObviousContext(miscomm.OriginalAttempt.Intent) ? 0.8f : 0.5f;
        float checkFactor = 1f - (checkCount * 0.15f); // Each check reduces escalation chance by 15%

        // Combined prevention chance
        float preventionChance = (relationFactor * 0.4f) +
                                (experienceFactor * 0.3f) +
                                (contextFactor * 0.2f) +
                                (checkFactor * 0.1f);

        // Roll for prevention
        var random = new Unity.Mathematics.Random((uint)(miscomm.Sender.Index + miscomm.Receiver.Index + checkCount));
        return random.NextFloat() > preventionChance; // Returns true if escalation should proceed
    }

    private void AttemptMiscommunicationCorrection(in MiscommunicationEvent miscomm)
    {
        // Entities attempt to clarify miscommunication
        // Create new communication attempt to correct misunderstanding
        // This gives entities a chance to realize the error and fix it
    }
}
```

---

## Learning from Miscommunications

### Communication Experience System

Entities learn from miscommunications and improve their skills:

```csharp
public struct CommunicationExperience : IComponentData
{
    public float OverallExperience;              // 0.0 to 1.0 (communication skill)
    public float LanguageExperience;             // Language learning from miscommunications
    public float CulturalAwareness;              // Understanding of cultural differences
    public float ContextReading;                 // Ability to read context clues
    public float ClarificationSkill;              // Ability to clarify misunderstandings
    public uint TotalMiscommunications;           // Total miscommunications experienced
    public uint CorrectedMiscommunications;       // Miscommunications successfully corrected
    public uint PreventedEscalations;            // Escalations prevented
}

[BurstCompile]
public partial struct CommunicationLearningSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (experience, miscomm) in SystemAPI.Query<
            RefRW<CommunicationExperience>,
            RefRO<MiscommunicationEvent>>()
            .WithAll<MiscommunicationEvent>())
        {
            // Learn from miscommunication
            LearnFromMiscommunication(ref experience.ValueRW, miscomm.ValueRO);
        }
    }

    private void LearnFromMiscommunication(
        ref CommunicationExperience experience,
        in MiscommunicationEvent miscomm)
    {
        experience.TotalMiscommunications++;

        // Gain experience based on severity (more severe = more learning)
        float experienceGain = miscomm.Severity switch
        {
            MiscommunicationSeverity.Minor => 0.001f,
            MiscommunicationSeverity.Moderate => 0.003f,
            MiscommunicationSeverity.Major => 0.005f,
            MiscommunicationSeverity.Critical => 0.01f,
            MiscommunicationSeverity.Catastrophic => 0.015f,
            _ => 0f
        };

        // Language experience (if language-related miscommunication)
        if (miscomm.OriginalAttempt.MethodUsed == CommunicationMethod.KnownLanguage ||
            miscomm.OriginalAttempt.MethodUsed == CommunicationMethod.GeneralSigns)
        {
            experience.LanguageExperience += experienceGain * 1.5f; // 1.5× gain for language mistakes
        }

        // Cultural awareness (if cultural miscommunication)
        if (miscomm.Type == MiscommunicationType.CulturalTaboo ||
            miscomm.Type == MiscommunicationType.CulturalCustom)
        {
            experience.CulturalAwareness += experienceGain * 2f; // 2× gain for cultural mistakes
        }

        // Context reading (if context-related miscommunication)
        if (miscomm.Type == MiscommunicationType.ContextMissing ||
            miscomm.Type == MiscommunicationType.ContextInverted)
        {
            experience.ContextReading += experienceGain * 1.2f;
        }

        // If miscommunication was corrected, gain clarification skill
        if (miscomm.WasCorrected)
        {
            experience.CorrectedMiscommunications++;
            experience.ClarificationSkill += experienceGain * 2f; // 2× gain for successful corrections
        }

        // If escalation was prevented, gain prevention experience
        // (handled in escalation system)

        // Update overall experience (weighted average)
        experience.OverallExperience = (
            experience.LanguageExperience * 0.3f +
            experience.CulturalAwareness * 0.25f +
            experience.ContextReading * 0.2f +
            experience.ClarificationSkill * 0.25f
        );

        // Clamp all values to 0-1
        experience.OverallExperience = math.clamp(experience.OverallExperience, 0f, 1f);
        experience.LanguageExperience = math.clamp(experience.LanguageExperience, 0f, 1f);
        experience.CulturalAwareness = math.clamp(experience.CulturalAwareness, 0f, 1f);
        experience.ContextReading = math.clamp(experience.ContextReading, 0f, 1f);
        experience.ClarificationSkill = math.clamp(experience.ClarificationSkill, 0f, 1f);
    }
}

// Helper to get communication experience for an entity
public static float GetCommunicationExperience(Entity entity)
{
    if (SystemAPI.HasComponent<CommunicationExperience>(entity))
    {
        return SystemAPI.GetComponent<CommunicationExperience>(entity).OverallExperience;
    }
    return 0f; // No experience yet
}
```

---

## Integration with Other Systems

### Tactical Commands Integration

Miscommunications affect tactical commands:

```csharp
// In TacticalCommandSystem, check for miscommunications
private float CalculateCommandCompliance(
    Entity commander,
    Entity recipient,
    TacticalCommandType commandType,
    float priority)
{
    // ... existing compliance calculation ...

    // Check if command was miscommunicated
    if (HasMiscommunication(commander, recipient, out var miscomm))
    {
        // Miscommunication reduces compliance
        float miscommPenalty = miscomm.Severity switch
        {
            MiscommunicationSeverity.Minor => 0.1f,      // -10% compliance
            MiscommunicationSeverity.Moderate => 0.2f,    // -20%
            MiscommunicationSeverity.Major => 0.4f,       // -40%
            MiscommunicationSeverity.Critical => 0.7f,    // -70%
            MiscommunicationSeverity.Catastrophic => 0.9f, // -90%
            _ => 0f
        };

        compliance = math.max(0f, compliance - miscommPenalty);

        // If command was misread, recipient might execute wrong command
        if (miscomm.Type == MiscommunicationType.CommandMisread)
        {
            // Execute misread command instead
            ExecuteMisreadCommand(recipient, miscomm);
            return 0f; // No compliance to original command
        }
    }

    return compliance;
}
```

### Trade System Integration

Price miscommunications affect trade:

```csharp
// In trade negotiation, apply price miscommunication
private float ApplyPriceMiscommunication(
    float originalPrice,
    in MiscommunicationEvent miscomm,
    ref Unity.Mathematics.Random random)
{
    if (miscomm.Type != MiscommunicationType.PriceMisread)
        return originalPrice;

    // Price misread severity determines price difference
    float priceError = miscomm.Severity switch
    {
        MiscommunicationSeverity.Minor => 0.1f,      // ±10% error
        MiscommunicationSeverity.Moderate => 0.25f,   // ±25% error
        MiscommunicationSeverity.Major => 0.5f,       // ±50% error
        MiscommunicationSeverity.Critical => 1.0f,    // ±100% error (2× or 0.5×)
        MiscommunicationSeverity.Catastrophic => 2.0f, // ±200% error (3× or 0.33×)
        _ => 0f
    };

    // Random direction (higher or lower)
    bool higher = random.NextBool();
    float multiplier = higher ? (1f + priceError) : (1f - priceError);

    return originalPrice * multiplier;
}
```

### Relations System Integration

Miscommunications affect relations:

```csharp
// After miscommunication, update relations
private void UpdateRelationsFromMiscommunication(
    ref RelationValue relations,
    in MiscommunicationEvent miscomm)
{
    // Minor miscommunications: small relation hit
    // Critical miscommunications: large relation hit
    // But if corrected, relations recover (or even improve)

    float relationChange = miscomm.Severity switch
    {
        MiscommunicationSeverity.Minor => -0.01f,
        MiscommunicationSeverity.Moderate => -0.03f,
        MiscommunicationSeverity.Major => -0.05f,
        MiscommunicationSeverity.Critical => -0.1f,
        MiscommunicationSeverity.Catastrophic => -0.2f,
        _ => 0f
    };

    // If miscommunication was corrected, reduce penalty
    if (miscomm.WasCorrected)
    {
        relationChange *= 0.5f; // Half penalty
    }

    // If escalation was prevented, relations might even improve
    if (miscomm.EscalatedToViolence == false && miscomm.Severity >= MiscommunicationSeverity.Critical)
    {
        relationChange += 0.05f; // +0.05 for preventing violence
    }

    relations.Value = math.clamp(relations.Value + relationChange, -1f, 1f);
}
```

---

## Example Scenarios

### Scenario 1: Minor Price Miscommunication

```csharp
// Trade negotiation: Merchant offers 100 gold for item
var originalAttempt = new CommunicationAttempt
{
    Intent = CommunicationIntent.TradeOfferSpecific,
    Clarity = 0.7f, // Moderate clarity (foreign language)
    MethodUsed = CommunicationMethod.KnownLanguage
};

// Miscommunication occurs (30% chance due to moderate clarity)
var miscomm = new MiscommunicationEvent
{
    Severity = MiscommunicationSeverity.Minor,
    Type = MiscommunicationType.PriceMisread
};

// Price misread: 100 gold → 110 gold (10% error)
// Buyer thinks item costs 110 gold, agrees
// Later realizes mistake, but trade already completed
// Relations: -0.01 (minor hit)
```

### Scenario 2: Critical Intent Miscommunication

```csharp
// Warrior approaches village with peaceful intent
var originalAttempt = new CommunicationAttempt
{
    Intent = CommunicationIntent.PeacefulIntent,
    Clarity = 0.4f, // Low clarity (general signs only)
    MethodUsed = CommunicationMethod.GeneralSigns
};

// Critical miscommunication: Peaceful intent read as hostile
var miscomm = new MiscommunicationEvent
{
    Severity = MiscommunicationSeverity.Critical,
    Type = MiscommunicationType.IntentMisread
};

// Village guards interpret raised hands as threat
// Escalation checks:
// - Check 1: Relations 0.5 (neutral) → 60% prevention chance → PREVENTED
// - Check 2: Context clues (no weapons drawn) → PREVENTED
// - Check 3: Warrior clarifies with item display → CORRECTED
// Result: No violence, relations -0.05 (recovered to -0.025 after correction)
```

### Scenario 3: Command Timing Failure

```csharp
// Commander orders "Retreat!" to unit 200m away
var command = new TacticalCommand
{
    CommandType = TacticalCommandType.Retreat,
    IssuedTick = 1000
};

var timing = new CommandTiming
{
    IntendedTick = 1010, // Should arrive in 10 ticks
    Reliability = 0.6f,  // 60% reliable (shouted command, distance penalty)
    ActualArrivalTick = 1025 // Arrived 15 ticks late
};

// Command status: Late
// Unit receives retreat order after enemy has already closed
// Unit takes casualties before retreating
// Command effectiveness: -30% (late arrival penalty)
```

### Scenario 4: Learning from Miscommunications

```csharp
// Entity starts with no communication experience
var experience = new CommunicationExperience
{
    OverallExperience = 0f
};

// Experiences 5 minor miscommunications
// Each minor miscommunication: +0.001 experience
// After 5: OverallExperience = 0.005 (0.5%)

// Experiences 1 critical miscommunication (corrected)
// Critical miscommunication: +0.01 base + 2× for correction = +0.02
// After correction: OverallExperience = 0.025 (2.5%)

// After 50 total miscommunications (various severities):
// OverallExperience = 0.15 (15%)
// LanguageExperience = 0.20 (20%) - focused on language mistakes
// CulturalAwareness = 0.10 (10%)
// ClarificationSkill = 0.18 (18%) - good at correcting errors

// Result: 15% reduction in future miscommunication chances
```

---

## Performance Considerations

**Profiling Targets**:
```
Miscommunication Calculation:    <0.05ms per communication attempt
Miscommunication Resolution:     <0.03ms per miscommunication
Timing Check:                     <0.02ms per command
Escalation Check:                 <0.05ms per critical miscommunication
Learning Update:                  <0.01ms per miscommunication
────────────────────────────────────────
Total (100 communications/frame): <15ms per frame
```

**Optimizations**:
- Batch miscommunication calculations (process all attempts together)
- Cache communication experience (don't recalculate every frame)
- Update timing checks at 5Hz instead of every frame
- Only check escalation for critical miscommunications
- Lazy update learning (only when miscommunication occurs)

---

## Summary

**Miscommunication Types**:
- **Content**: Intent, price, direction, target, command, request, warning, offer misreads
- **Timing**: Commands delayed, early, lost, or corrupted
- **Deception**: Undetected deception, false positives, disguised intent
- **Cultural**: Taboo violations, custom misinterpretations, language nuance loss
- **Context**: Missing context, inverted context, sarcasm/joke missed

**Severity Levels**:
- **Minor**: Small misunderstandings (price differences, slight intent shifts)
- **Moderate**: Noticeable errors (wrong direction, misunderstood request)
- **Major**: Significant errors (wrong target, misinterpreted command)
- **Critical**: Provocative misunderstandings (hostile intent misread as peaceful)
- **Catastrophic**: Complete breakdown (command inverted, total misread)

**Escalation Prevention**:
- Critical miscommunications checked multiple times before violence
- Factors: Relations, experience, context, multiple checks
- Entities can correct miscommunications before escalation

**Learning System**:
- Entities gain experience from miscommunications
- Experience reduces future miscommunication chances
- Specialized skills: Language, cultural awareness, context reading, clarification
- Successful corrections and prevented escalations grant bonus experience

**Key Insight**: Miscommunications add controlled chaos and variety to the simulation. Entities learn from mistakes, improving communication over time. Critical miscommunications are dangerous but preventable through multiple checks and clarification attempts. The system creates emergent narratives of misunderstandings, corrections, and learning.








