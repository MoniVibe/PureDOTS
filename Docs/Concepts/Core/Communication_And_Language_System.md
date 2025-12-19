# Communication and Language System

## Overview

The Communication and Language System enables entities to exchange information, intentions, knowledge, and deception through multiple communication methods. Entities attempt communication using a hierarchical approach: native language → known languages → general signs (universal gestures). Language barriers create opportunities for miscommunication, cultural friction, and strategic deception, while successful communication enables teaching, learning, trade negotiation, and diplomacy.

**Key Principles**:
- **Hierarchical Communication**: Try native language first, fall back to known languages, then universal signs
- **Language Diversity**: Multiple languages with varying proficiency levels
- **General Signs**: Universal gestures/symbols shared across all cultures (prone to miscommunication)
- **Intent Communication**: Share true intentions, disguise intentions (deception), or misread intentions
- **Knowledge Transfer**: Teaching and learning through successful communication
- **Spell Language Ties**: Magic spells tied to specific languages (verbal components)
- **Spell Signs**: Gesture-based magic using hand/limb movements (somatic components)
- **Miscommunication Gameplay**: Language barriers and sign misinterpretation create emergent narratives
- **Deterministic**: Same language proficiencies + same communication attempts = same results

---

## Core Concepts

### Communication Hierarchy

Entities attempt communication in order of reliability:

```csharp
public enum CommunicationMethod : byte
{
    NativeLanguage = 0,     // Highest reliability, full nuance
    KnownLanguage = 1,      // Good reliability, depends on proficiency
    GeneralSigns = 2,       // Low reliability, basic concepts only
    Empathy = 3,            // Emotion reading (psychic/magical)
    Telepathy = 4,          // Direct mind-to-mind (rare, magical)
    FailedCommunication = 5 // No understanding achieved
}

public struct CommunicationAttempt : IComponentData
{
    public Entity Sender;
    public Entity Receiver;
    public CommunicationMethod MethodUsed;
    public CommunicationIntent Intent;       // What sender wants to convey
    public CommunicationResult Result;       // What receiver understood
    public float Clarity;                    // 0.0 (total confusion) to 1.0 (perfect understanding)
    public bool WasDeceptive;                // Did sender disguise intent?
    public bool WasDetected;                 // Did receiver detect deception?
}

public enum CommunicationIntent : byte
{
    // Social
    Greeting = 0,
    Farewell = 1,
    Gratitude = 2,
    Apology = 3,
    Threat = 4,
    Submission = 5,

    // Trade
    WillingToTrade = 10,
    UnwillingToTrade = 11,
    TradeOfferSpecific = 12,    // Specific item/resource
    TradeRequestSpecific = 13,
    PriceNegotiation = 14,

    // Information
    AskForDirections = 20,      // Where is X?
    ProvideDirections = 21,
    AskForKnowledge = 22,       // How do I do X?
    ShareKnowledge = 23,
    Warning = 24,               // Danger nearby
    Rumor = 25,                 // Share gossip/intel

    // Intent
    PeacefulIntent = 30,
    HostileIntent = 31,
    NeutralIntent = 32,
    HiddenIntent = 33,          // Disguised intentions

    // Actions
    RequestHelp = 40,
    OfferHelp = 41,
    RequestAlliance = 42,
    DeclineRequest = 43,

    // Magic
    SpellIncantation = 50,      // Verbal spell component
    SpellSign = 51,             // Gesture spell component
    TeachSpell = 52,

    // Unknown
    Incomprehensible = 255
}
```

---

## Language System

### Language Definition

```csharp
public struct Language : IComponentData
{
    public FixedString64Bytes LanguageId;    // "Common", "Elvish", "Draconic", etc.
    public LanguageFamily Family;            // Related languages
    public LanguageComplexity Complexity;    // How hard to learn
    public uint VocabularySize;              // Number of words
    public bool HasWrittenForm;              // Can be read/written
    public bool HasSpokenForm;               // Can be spoken
    public bool HasSignForm;                 // Sign language variant
    public FixedList128Bytes<FixedString32Bytes> RelatedLanguages; // Similar languages (easier to learn)
}

public enum LanguageFamily : byte
{
    Common = 0,         // Trade languages, widespread
    Ancient = 1,        // Dead languages, used for spells
    Tribal = 2,         // Regional dialects
    Noble = 3,          // Court languages, formal
    Arcane = 4,         // Magical languages
    Primordial = 5,     // Elemental languages
    Celestial = 6,      // Divine languages
    Abyssal = 7,        // Demonic languages
    Alien = 8,          // Space4X: Alien species languages
    Synthetic = 9,      // Space4X: AI/construct languages
    Unique = 255        // One-of-a-kind languages
}

public enum LanguageComplexity : byte
{
    Simple = 0,         // 1-2 months to learn basic proficiency
    Moderate = 1,       // 6 months to learn basic proficiency
    Complex = 2,        // 1 year to learn basic proficiency
    VeryComplex = 3,    // 2+ years to learn basic proficiency
    Impossible = 4      // Cannot be learned (requires special trait)
}
```

### Language Proficiency

Entities track proficiency per language:

```csharp
public struct LanguageProficiency : IBufferElementData
{
    public FixedString64Bytes LanguageId;
    public ProficiencyLevel Level;
    public float Experience;                 // Progress toward next level (0.0 to 1.0)
    public bool IsNative;                    // Native speaker (perfect clarity)
    public LanguageSkillset Skills;          // What can they do with this language
}

public enum ProficiencyLevel : byte
{
    None = 0,           // No understanding (0% clarity)
    Rudimentary = 1,    // Basic words, simple concepts (20% clarity)
    Basic = 2,          // Simple sentences, common topics (40% clarity)
    Conversational = 3, // Everyday communication (60% clarity)
    Fluent = 4,         // Complex ideas, nuance (80% clarity)
    Native = 5,         // Perfect understanding, idioms, slang (100% clarity)
    Scholarly = 6       // Academic mastery, archaic forms, etymology (100% + teach bonus)
}

[Flags]
public enum LanguageSkillset : uint
{
    None = 0,
    Understand = 1 << 0,        // Can comprehend spoken language
    Speak = 1 << 1,             // Can produce spoken language
    Read = 1 << 2,              // Can read written language
    Write = 1 << 3,             // Can write in language
    Teach = 1 << 4,             // Can teach language to others
    Translate = 1 << 5,         // Can translate to/from other languages
    CastSpells = 1 << 6,        // Can use language for spell incantations
    DetectLies = 1 << 7,        // Can detect deception via language cues
    Persuade = 1 << 8,          // Rhetoric and persuasion
    Intimidate = 1 << 9,        // Threats and coercion
    Negotiate = 1 << 10         // Trade and diplomacy
}

// Example: Native speaker of Common
LanguageProficiency commonNative = new()
{
    LanguageId = "Common",
    Level = ProficiencyLevel.Native,
    IsNative = true,
    Skills = LanguageSkillset.Understand | LanguageSkillset.Speak |
             LanguageSkillset.Read | LanguageSkillset.Write |
             LanguageSkillset.Teach | LanguageSkillset.DetectLies |
             LanguageSkillset.Persuade
};

// Example: Basic understanding of Elvish
LanguageProficiency elvishBasic = new()
{
    LanguageId = "Elvish",
    Level = ProficiencyLevel.Basic,
    Experience = 0.3f,  // 30% toward Conversational
    IsNative = false,
    Skills = LanguageSkillset.Understand | LanguageSkillset.Speak
};
```

---

## General Signs (Universal Gestures)

When language fails, entities fall back to **general signs** - universal gestures and body language:

```csharp
public struct GeneralSign : IComponentData
{
    public GeneralSignType Type;
    public float Clarity;                    // How clear is the gesture (0.0 to 1.0)
    public float MiscommunicationChance;     // Chance of misinterpretation (0.0 to 1.0)
    public CommunicationIntent IntendedMessage;
    public CommunicationIntent PossibleMisreading; // What it might be confused with
}

public enum GeneralSignType : byte
{
    // Emotional
    PointAtSelf = 0,            // "Me", "I"
    PointAtOther = 1,           // "You"
    Wave = 2,                   // Greeting or farewell
    Nod = 3,                    // Agreement, yes
    Shake = 4,                  // Disagreement, no
    Shrug = 5,                  // Don't know, don't care
    Bow = 6,                    // Respect, submission
    RaisedHands = 7,            // Surrender, peace

    // Directional
    PointDirection = 10,        // Indicating direction
    BeckOn = 11,                // Come here
    ShooAway = 12,              // Go away
    FollowMe = 13,              // Gesture to follow

    // Trade
    ShowItem = 20,              // Display object for trade
    OfferHandshake = 21,        // Agreement, deal
    CrossArms = 22,             // Refusal, closed
    RubFingers = 23,            // Money, payment
    WeighingHands = 24,         // Comparing value

    // Threat/Warning
    WeaponDisplay = 30,         // Threat, hostility
    FingerAcrossThroat = 31,    // Death threat
    PointAtGround = 32,         // Stay here, don't move
    WardingGesture = 33,        // Keep away, danger

    // Magic (Spell Signs)
    ArcaneGesture = 40,         // Complex hand movements for spells
    ElementalSign = 41,         // Elemental magic gesture
    DivineSign = 42,            // Holy symbol gesture
    CursedSign = 43,            // Dark magic gesture

    // Complex
    Pantomime = 50,             // Acting out scenario (very slow, prone to error)
    Drawing = 51,               // Draw in dirt/sand to communicate
    PointAtStars = 52           // Astronomical reference (Space4X)
}

// General signs are less reliable than language
// Clarity decreases with complexity of message

public struct GeneralSignCommunication : IComponentData
{
    public GeneralSignType PrimarySign;
    public GeneralSignType SecondarySign;    // Combined gestures for complex ideas
    public float BaseMiscommunicationChance; // Starts at 30%
    public float CulturalMiscommunicationBonus; // +20% if different cultures
    public float ContextualClarityBonus;     // -10% if context is obvious
}

// Example: Trying to ask for directions using general signs
// 1. Point at self (Me)
// 2. Point direction (That way?)
// 3. Shrug (Don't know)
// 4. Point at other (You)
// 5. Point direction again (Where?)
//
// Receiver might understand:
// - Correct: "Do you know which way to go?"
// - Misread: "Should I follow you that way?"
// - Misread: "Are you from that direction?"
//
// Miscommunication chance: 30% (base) + 20% (different cultures) - 10% (pointing is obvious) = 40%
```

---

## Communication Attempt System

```csharp
[BurstCompile]
public partial struct CommunicationAttemptSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (attempt, senderLanguages, receiverLanguages) in SystemAPI.Query<
            RefRW<CommunicationAttempt>,
            DynamicBuffer<LanguageProficiency>, // Sender's languages
            DynamicBuffer<LanguageProficiency>>() // Receiver's languages (separate query)
            .WithAll<CommunicationAttempt>())
        {
            // Step 1: Try native language
            var senderNative = GetNativeLanguage(senderLanguages);
            var receiverKnowledge = GetLanguageProficiency(receiverLanguages, senderNative.LanguageId);

            if (receiverKnowledge.Level >= ProficiencyLevel.Rudimentary)
            {
                // Receiver understands sender's native language
                attempt.ValueRW.MethodUsed = CommunicationMethod.NativeLanguage;
                attempt.ValueRW.Clarity = CalculateClarity(senderNative.Level, receiverKnowledge.Level);
                ProcessCommunication(ref attempt.ValueRW);
                continue;
            }

            // Step 2: Try other known languages (find common language)
            var commonLanguage = FindCommonLanguage(senderLanguages, receiverLanguages);
            if (commonLanguage.LanguageId != default)
            {
                var senderProf = GetLanguageProficiency(senderLanguages, commonLanguage.LanguageId);
                var receiverProf = GetLanguageProficiency(receiverLanguages, commonLanguage.LanguageId);

                attempt.ValueRW.MethodUsed = CommunicationMethod.KnownLanguage;
                attempt.ValueRW.Clarity = CalculateClarity(senderProf.Level, receiverProf.Level);
                ProcessCommunication(ref attempt.ValueRW);
                continue;
            }

            // Step 3: Fall back to general signs
            attempt.ValueRW.MethodUsed = CommunicationMethod.GeneralSigns;
            attempt.ValueRW.Clarity = CalculateGeneralSignClarity(attempt.ValueRO.Intent);
            ProcessGeneralSignCommunication(ref attempt.ValueRW);
        }
    }

    private float CalculateClarity(ProficiencyLevel senderLevel, ProficiencyLevel receiverLevel)
    {
        // Clarity is limited by the lower proficiency level
        var minLevel = (int)math.min((int)senderLevel, (int)receiverLevel);

        // Convert proficiency to clarity
        switch (minLevel)
        {
            case 0: return 0.0f;    // None
            case 1: return 0.2f;    // Rudimentary
            case 2: return 0.4f;    // Basic
            case 3: return 0.6f;    // Conversational
            case 4: return 0.8f;    // Fluent
            case 5: return 1.0f;    // Native
            case 6: return 1.0f;    // Scholarly
            default: return 0.0f;
        }
    }

    private float CalculateGeneralSignClarity(CommunicationIntent intent)
    {
        // General signs are less reliable
        // Simple concepts: 0.5-0.7 clarity
        // Complex concepts: 0.2-0.4 clarity

        switch (intent)
        {
            case CommunicationIntent.Greeting:
            case CommunicationIntent.Farewell:
            case CommunicationIntent.Threat:
            case CommunicationIntent.Submission:
                return 0.7f; // Simple emotions, high clarity

            case CommunicationIntent.WillingToTrade:
            case CommunicationIntent.UnwillingToTrade:
            case CommunicationIntent.PeacefulIntent:
            case CommunicationIntent.HostileIntent:
                return 0.5f; // Moderate clarity

            case CommunicationIntent.AskForDirections:
            case CommunicationIntent.TradeOfferSpecific:
            case CommunicationIntent.ShareKnowledge:
                return 0.3f; // Complex ideas, low clarity

            default:
                return 0.2f; // Very complex or abstract, very low clarity
        }
    }

    private void ProcessGeneralSignCommunication(ref CommunicationAttempt attempt)
    {
        var random = new Unity.Mathematics.Random((uint)(attempt.Sender.Index + attempt.Receiver.Index));

        // Base miscommunication chance: 30%
        float miscommunicationChance = 0.3f;

        // Cultural differences increase miscommunication
        if (AreDifferentCultures(attempt.Sender, attempt.Receiver))
            miscommunicationChance += 0.2f;

        // Context can reduce miscommunication
        if (HasObviousContext(attempt.Intent))
            miscommunicationChance -= 0.1f;

        // Roll for miscommunication
        if (random.NextFloat() < miscommunicationChance)
        {
            // Misread the sign!
            attempt.Result = GetMisreadIntent(attempt.Intent, random);
            attempt.Clarity *= 0.5f; // Clarity drops
        }
        else
        {
            // Correctly understood
            attempt.Result = attempt.Intent;
        }
    }

    private CommunicationIntent GetMisreadIntent(CommunicationIntent original, Unity.Mathematics.Random random)
    {
        // Common misreadings based on original intent
        switch (original)
        {
            case CommunicationIntent.Greeting:
                // Wave could be mistaken for farewell or shooing away
                return random.NextBool() ? CommunicationIntent.Farewell : CommunicationIntent.DeclineRequest;

            case CommunicationIntent.PeacefulIntent:
                // Raised hands could be surrender or threat
                return random.NextBool() ? CommunicationIntent.Submission : CommunicationIntent.Threat;

            case CommunicationIntent.WillingToTrade:
                // Showing item could be offer or demand
                return random.NextBool() ? CommunicationIntent.TradeOfferSpecific : CommunicationIntent.TradeRequestSpecific;

            case CommunicationIntent.AskForDirections:
                // Pointing could be asking or telling
                return random.NextBool() ? CommunicationIntent.ProvideDirections : CommunicationIntent.Warning;

            default:
                return CommunicationIntent.Incomprehensible;
        }
    }
}
```

---

## Intent Communication and Deception

Entities can share true intentions or disguise them:

```csharp
public struct IntentCommunication : IComponentData
{
    public CommunicationIntent TrueIntent;       // What sender actually wants
    public CommunicationIntent StatedIntent;     // What sender claims to want
    public bool IsDeceptive;                     // Is stated different from true?
    public float DeceptionSkill;                 // 0.0 to 1.0 (sender's deception ability)
    public float DetectionChance;                // Chance receiver detects deception
}

[BurstCompile]
public partial struct DeceptionDetectionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (attempt, intentComm, senderTraits, receiverTraits) in SystemAPI.Query<
            RefRW<CommunicationAttempt>,
            RefRO<IntentCommunication>,
            RefRO<PersonalityTraits>,  // Sender
            RefRO<PersonalityTraits>>() // Receiver
            .WithAll<IntentCommunication>())
        {
            if (!intentComm.ValueRO.IsDeceptive)
            {
                // Honest communication
                attempt.ValueRW.WasDeceptive = false;
                continue;
            }

            // Calculate detection chance
            float detectionChance = 0.0f;

            // Receiver's insight increases detection
            detectionChance += receiverTraits.ValueRO.InsightScore / 100f * 0.5f; // Up to +50%

            // Sender's deception skill decreases detection
            detectionChance -= intentComm.ValueRO.DeceptionSkill * 0.4f; // Up to -40%

            // Language proficiency affects detection (easier to lie in foreign language)
            if (attempt.ValueRO.MethodUsed == CommunicationMethod.KnownLanguage &&
                attempt.ValueRO.Clarity < 0.8f)
            {
                detectionChance -= 0.2f; // -20% if not fluent
            }

            // General signs are easier to misread (harder to detect deception)
            if (attempt.ValueRO.MethodUsed == CommunicationMethod.GeneralSigns)
            {
                detectionChance -= 0.3f; // -30% for general signs
            }

            var random = new Unity.Mathematics.Random((uint)attempt.ValueRO.Sender.Index);
            attempt.ValueRW.WasDeceptive = true;
            attempt.ValueRW.WasDetected = random.NextFloat() < math.clamp(detectionChance, 0f, 1f);
        }
    }
}

public struct PersonalityTraits : IComponentData
{
    public byte HonestyScore;        // 0-100 (how truthful)
    public byte DeceptionScore;      // 0-100 (how good at lying)
    public byte InsightScore;        // 0-100 (how good at detecting lies)
    public byte TrustworthinessScore; // 0-100 (reputation for honesty)
}

// Example: Devious trader lying about item value
IntentCommunication deceptiveTrade = new()
{
    TrueIntent = CommunicationIntent.TradeOfferSpecific, // Worthless trinket
    StatedIntent = CommunicationIntent.TradeOfferSpecific, // Claims it's valuable relic
    IsDeceptive = true,
    DeceptionSkill = 0.8f, // Very good liar
    DetectionChance = 0.3f // 30% chance receiver sees through it
};
```

---

## Language Learning

Entities learn languages through interaction:

```csharp
public struct LanguageLearning : IComponentData
{
    public FixedString64Bytes TargetLanguage;   // Language being learned
    public Entity Teacher;                       // Who is teaching (or Entity.Null if self-taught)
    public float LearningRate;                   // XP gain per interaction
    public float TotalExperience;                // Accumulated XP
    public float ExperienceToNextLevel;          // XP needed for next proficiency level
    public LearningMethod Method;
}

public enum LearningMethod : byte
{
    Immersion = 0,          // Learning through repeated exposure (slow)
    FormalTeaching = 1,     // Being taught by fluent speaker (fast)
    StudyingTexts = 2,      // Reading books/scrolls (moderate, read-only)
    Magical = 3,            // Instant learning via magic (rare)
    Telepathic = 4          // Mind-to-mind transfer (very rare)
}

[BurstCompile]
public partial struct LanguageLearningSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (learning, proficiencies) in SystemAPI.Query<
            RefRW<LanguageLearning>,
            DynamicBuffer<LanguageProficiency>>())
        {
            // Gain experience based on method
            float xpGain = CalculateXPGain(learning.ValueRO.Method, deltaTime);
            learning.ValueRW.TotalExperience += xpGain;

            // Check for level up
            if (learning.ValueRO.TotalExperience >= learning.ValueRO.ExperienceToNextLevel)
            {
                LevelUpLanguage(ref learning.ValueRW, proficiencies, learning.ValueRO.TargetLanguage);
            }
        }
    }

    private float CalculateXPGain(LearningMethod method, float deltaTime)
    {
        switch (method)
        {
            case LearningMethod.Immersion:
                return 0.1f * deltaTime; // Slow but steady

            case LearningMethod.FormalTeaching:
                return 1.0f * deltaTime; // Fast learning with teacher

            case LearningMethod.StudyingTexts:
                return 0.5f * deltaTime; // Moderate self-study

            case LearningMethod.Magical:
                return 100.0f; // Instant (one-time)

            case LearningMethod.Telepathic:
                return 50.0f; // Very fast

            default:
                return 0.0f;
        }
    }

    private void LevelUpLanguage(
        ref LanguageLearning learning,
        DynamicBuffer<LanguageProficiency> proficiencies,
        FixedString64Bytes languageId)
    {
        // Find language proficiency
        for (int i = 0; i < proficiencies.Length; i++)
        {
            if (proficiencies[i].LanguageId == languageId)
            {
                var prof = proficiencies[i];
                prof.Level = (ProficiencyLevel)math.min((int)prof.Level + 1, (int)ProficiencyLevel.Scholarly);

                // Unlock new skills based on level
                switch (prof.Level)
                {
                    case ProficiencyLevel.Rudimentary:
                        prof.Skills |= LanguageSkillset.Understand;
                        break;
                    case ProficiencyLevel.Basic:
                        prof.Skills |= LanguageSkillset.Speak;
                        break;
                    case ProficiencyLevel.Conversational:
                        prof.Skills |= LanguageSkillset.Read;
                        break;
                    case ProficiencyLevel.Fluent:
                        prof.Skills |= LanguageSkillset.Write | LanguageSkillset.DetectLies;
                        break;
                    case ProficiencyLevel.Native:
                        prof.Skills |= LanguageSkillset.Teach | LanguageSkillset.Persuade;
                        break;
                    case ProficiencyLevel.Scholarly:
                        prof.Skills |= LanguageSkillset.Translate | LanguageSkillset.CastSpells;
                        break;
                }

                proficiencies[i] = prof;

                // Reset XP for next level
                learning.TotalExperience = 0f;
                learning.ExperienceToNextLevel = CalculateXPRequired(prof.Level);
                break;
            }
        }
    }

    private float CalculateXPRequired(ProficiencyLevel currentLevel)
    {
        // Exponential XP requirements
        switch (currentLevel)
        {
            case ProficiencyLevel.None: return 100f;        // 100 XP to Rudimentary
            case ProficiencyLevel.Rudimentary: return 500f; // 500 XP to Basic
            case ProficiencyLevel.Basic: return 2000f;      // 2000 XP to Conversational
            case ProficiencyLevel.Conversational: return 5000f; // 5000 XP to Fluent
            case ProficiencyLevel.Fluent: return 10000f;    // 10000 XP to Native
            case ProficiencyLevel.Native: return 20000f;    // 20000 XP to Scholarly
            default: return float.MaxValue;
        }
    }
}

// Example: Learning Elvish through immersion
LanguageLearning elvishImmersion = new()
{
    TargetLanguage = "Elvish",
    Teacher = Entity.Null, // Self-taught
    Method = LearningMethod.Immersion,
    LearningRate = 0.1f,
    TotalExperience = 0f,
    ExperienceToNextLevel = 100f // To reach Rudimentary
};

// With immersion (0.1 XP/s), takes 1000 seconds (~17 minutes) to reach Rudimentary
// With formal teaching (1.0 XP/s), takes 100 seconds (~1.5 minutes)
```

---

## Teaching and Knowledge Transfer

Entities can teach languages, spells, and knowledge:

```csharp
public struct TeachingSession : IComponentData
{
    public Entity Teacher;
    public Entity Student;
    public TeachingSubject Subject;
    public FixedString64Bytes SubjectId;         // Language name, spell name, etc.
    public float TeacherProficiency;             // How well teacher knows subject
    public float TeachingEfficiency;             // Teacher's teaching ability
    public float StudentAptitude;                // Student's learning ability
    public float SessionProgress;                // 0.0 to 1.0
    public float SessionDuration;                // Seconds
}

public enum TeachingSubject : byte
{
    Language = 0,
    Spell = 1,
    CraftingRecipe = 2,
    CombatTechnique = 3,
    Lore = 4,
    Trade = 5,
    Navigation = 6
}

[BurstCompile]
public partial struct TeachingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var teaching in SystemAPI.Query<RefRW<TeachingSession>>())
        {
            // Calculate learning progress
            float progressGain = teaching.ValueRO.TeachingEfficiency *
                                teaching.ValueRO.StudentAptitude *
                                deltaTime / teaching.ValueRO.SessionDuration;

            teaching.ValueRW.SessionProgress += progressGain;

            // Session complete
            if (teaching.ValueRO.SessionProgress >= 1.0f)
            {
                // Grant knowledge to student
                GrantKnowledge(
                    teaching.ValueRO.Student,
                    teaching.ValueRO.Subject,
                    teaching.ValueRO.SubjectId,
                    teaching.ValueRO.TeacherProficiency
                );
            }
        }
    }
}

// Example: Elvish teacher teaching Common speaker
TeachingSession elvishLesson = new()
{
    Teacher = elvishSpeakerEntity,
    Student = commonSpeakerEntity,
    Subject = TeachingSubject.Language,
    SubjectId = "Elvish",
    TeacherProficiency = 1.0f,  // Native speaker
    TeachingEfficiency = 0.8f,  // Good teacher
    StudentAptitude = 0.6f,     // Average learner
    SessionProgress = 0f,
    SessionDuration = 3600f     // 1 hour session
};

// Progress: 0.8 × 0.6 × 1.0 / 3600 = 0.00013/s
// Completes in ~1 hour of teaching
```

---

## Spell Languages and Spell Signs

Magic is tied to communication:

### Verbal Spell Components (Incantations)

```csharp
public struct SpellIncantation : IComponentData
{
    public FixedString64Bytes SpellId;
    public FixedString64Bytes RequiredLanguage;  // Language spell must be cast in
    public ProficiencyLevel MinimumProficiency;  // Minimum language level needed
    public FixedString128Bytes IncantationText;  // The words to speak
    public float CastTime;                       // Seconds to speak incantation
    public bool CanBeInterrupted;                // Can casting be disrupted?
}

// Example: Fireball spell requires Ancient Arcane language
SpellIncantation fireball = new()
{
    SpellId = "Fireball",
    RequiredLanguage = "AncientArcane",
    MinimumProficiency = ProficiencyLevel.Conversational, // Must be fluent-ish
    IncantationText = "Ignis Orbis Maximus",
    CastTime = 2.0f,        // 2 seconds to cast
    CanBeInterrupted = true // Silence spell can interrupt
};

// Casting requirements:
// - Must know Ancient Arcane at Conversational or higher
// - Must have CastSpells skill unlocked
// - Cannot be silenced/muted
// - Cannot have mouth covered/gagged
```

### Somatic Spell Components (Spell Signs)

```csharp
public struct SpellSign : IComponentData
{
    public FixedString64Bytes SpellId;
    public SpellSignComplexity Complexity;       // How hard is the gesture
    public uint RequiredLimbs;                   // How many limbs needed
    public LimbType RequiredLimbType;            // Hands, tentacles, wings, etc.
    public float GestureTime;                    // Seconds to perform gesture
    public bool RequiresFreeMovement;            // Can't be restrained
}

public enum SpellSignComplexity : byte
{
    Simple = 0,         // One-handed, basic gesture (1-2 seconds)
    Moderate = 1,       // Two-handed, coordinated (3-5 seconds)
    Complex = 2,        // Two-handed, intricate (5-10 seconds)
    VeryComplex = 3,    // Requires full body movement (10-20 seconds)
    Impossible = 4      // Cannot be performed by most species
}

[Flags]
public enum LimbType : uint
{
    None = 0,
    Hands = 1 << 0,
    Tentacles = 1 << 1,
    Wings = 1 << 2,
    Tail = 1 << 3,
    Mandibles = 1 << 4,
    PsionicOrgan = 1 << 5
}

// Example: Shield spell using hand gestures
SpellSign shield = new()
{
    SpellId = "Shield",
    Complexity = SpellSignComplexity.Moderate,
    RequiredLimbs = 2,
    RequiredLimbType = LimbType.Hands,
    GestureTime = 3.0f,
    RequiresFreeMovement = true // Hands must be free
};

// Example: Silent casting (no incantation, gestures only)
// Allows spellcasting without speaking
// Useful when silenced, underwater, or sneaking
// Typically slower than verbal casting
```

### Combined Spell Casting

Many spells require both verbal and somatic components:

```csharp
public struct SpellCastingRequirements : IComponentData
{
    public bool RequiresVerbal;      // Needs incantation
    public bool RequiresSomatic;     // Needs gestures
    public bool RequiresMaterial;    // Needs physical components
    public bool RequiresFocus;       // Needs magical focus item
}

// Example: Fireball (verbal + somatic)
SpellCastingRequirements fireballReqs = new()
{
    RequiresVerbal = true,   // Must speak incantation
    RequiresSomatic = true,  // Must gesture to aim
    RequiresMaterial = false,
    RequiresFocus = false
};

// Example: Counterspell (somatic only, instant)
SpellCastingRequirements counterspellReqs = new()
{
    RequiresVerbal = false,  // Silent
    RequiresSomatic = true,  // Quick hand gesture
    RequiresMaterial = false,
    RequiresFocus = false
};

// Casting interruption:
// - Verbal: Can be interrupted by silence, gag, stun
// - Somatic: Can be interrupted by restraints, paralysis, disarm
// - Both: Either interruption stops the spell
```

---

## Miscommunication Consequences

Failed or misread communication affects relations:

```csharp
public struct MiscommunicationEvent : IComponentData
{
    public Entity Sender;
    public Entity Receiver;
    public CommunicationIntent IntendedMessage;
    public CommunicationIntent ReceivedMessage;
    public MiscommunicationType Type;
    public int RelationImpact;               // Positive or negative
}

public enum MiscommunicationType : byte
{
    LanguageBarrier = 0,    // Different languages, no common tongue
    SignMisread = 1,        // General sign interpreted incorrectly
    CulturalDifference = 2, // Cultural norms differ
    DeceptionDetected = 3,  // Caught in a lie
    DeceptionUndetected = 4, // Lie believed
    Unclear = 5             // Vague or ambiguous communication
}

// Example: Friendly wave misread as aggressive gesture
MiscommunicationEvent waveIncident = new()
{
    Sender = humanEntity,
    Receiver = alienEntity,
    IntendedMessage = CommunicationIntent.Greeting,
    ReceivedMessage = CommunicationIntent.Threat,
    Type = MiscommunicationType.SignMisread,
    RelationImpact = -50 // Major negative impact
};

// Alien now thinks human is hostile
// Human is confused why alien is upset
// Could escalate to combat without clarification
```

**Integration with Reactions System**:
```csharp
// Miscommunication triggers reaction events
// Example: Misread threat → Aggressive reaction
// Example: Detected lie → Trust penalty
// Example: Successful communication → Relation bonus
```

---

## Cross-Game Applications

### Godgame: NPC Communication

```csharp
// Villagers communicate with each other and player
// - Villagers share knowledge of resources, dangers, opportunities
// - Player can issue commands via general signs (if no shared language)
// - Teaching language to villagers allows more complex orders
// - Deceptive villagers can lie about threats to manipulate player

// Example: Villager warns about nearby wolves
CommunicationAttempt villagerWarning = new()
{
    Sender = villagerEntity,
    Receiver = playerEntity,
    Intent = CommunicationIntent.Warning,
    MethodUsed = CommunicationMethod.GeneralSigns, // Player doesn't speak their language
    Clarity = 0.5f // Moderate clarity (pointing, pantomiming fear)
};

// Player understands there's danger, but not specifics
// Learning villager language would clarify: "Wolves to the north!"
```

### Godgame: Spell Casting

```csharp
// Magic users must learn spell languages
// - Ancient languages unlock powerful spells
// - General sign spells (somatic only) are weaker but universal
// - Language barriers prevent spell theft (can't learn without understanding)

// Example: Elf teaches human wizard Elvish spells
TeachingSession spellLessson = new()
{
    Teacher = elfMageEntity,
    Student = humanWizardEntity,
    Subject = TeachingSubject.Spell,
    SubjectId = "MoonBeam",
    TeacherProficiency = 1.0f,
    SessionDuration = 7200f // 2 hours to teach spell
};

// Human must first learn Elvish (Language) to cast Elvish spells
// Or learn spell signs version (weaker, but language-independent)
```

### Space4X: Alien Diplomacy

```csharp
// First contact scenarios
// - Unknown alien species use general signs initially
// - High miscommunication chance (different biology, culture)
// - Research tech to decode alien language
// - Universal translators reduce miscommunication over time

// Example: First contact with insectoid species
CommunicationAttempt firstContact = new()
{
    Sender = humanDiplomatEntity,
    Receiver = insectoidQueenEntity,
    Intent = CommunicationIntent.PeacefulIntent,
    MethodUsed = CommunicationMethod.GeneralSigns,
    Clarity = 0.2f // Very low (radically different physiology)
};

// Human raises hands (peace gesture)
// Insectoid might interpret as submission, threat, or confusion
// Miscommunication risk: 60%

// After researching Insectoid Language:
CommunicationAttempt laterContact = new()
{
    Sender = humanDiplomatEntity,
    Receiver = insectoidQueenEntity,
    Intent = CommunicationIntent.RequestAlliance,
    MethodUsed = CommunicationMethod.KnownLanguage,
    Clarity = 0.6f // Conversational proficiency
};

// Much clearer communication, alliance possible
```

### Space4X: AI/Synthetic Communication

```csharp
// AI entities use synthetic languages (binary, machine code)
// - Humans can't understand without translator tech
// - AI can communicate via data transmission (instant, perfect clarity)
// - Hacked AI may use deception protocols

// Example: Human hacking AI to communicate
LanguageProficiency machineLang = new()
{
    LanguageId = "MachineCode",
    Level = ProficiencyLevel.Basic,
    Skills = LanguageSkillset.Understand | LanguageSkillset.Speak
};

// Allows basic queries to AI systems
// Higher proficiency unlocks AI hacking, programming
```

---

## Performance Considerations

### Communication Caching

```csharp
// Cache common language lookups
public struct CommunicationCache : IComponentData
{
    public FixedList128Bytes<CachedCommonLanguage> CommonLanguages;
}

public struct CachedCommonLanguage
{
    public Entity OtherEntity;
    public FixedString64Bytes SharedLanguage;
    public float CombinedClarity;
    public double LastChecked;
}

// Avoid re-checking language compatibility every frame
// Update cache when languages change or every 10 seconds
```

### General Sign Pooling

```csharp
// Reuse general sign components
// Don't create new GeneralSign component per attempt
// Pool and reset instead
```

**Profiling Targets**:
```
Language Lookup:         <0.1ms per attempt
Clarity Calculation:     <0.05ms per attempt
Deception Detection:     <0.1ms per attempt
Teaching XP Update:      <0.05ms per student
Miscommunication Event:  <0.1ms per event
────────────────────────────────────────────
Total (100 simultaneous): <4.0ms per frame
```

---

## Future Extensions

### Dynamic Language Evolution

```csharp
// Languages change over time
// - New words added (technological terms, slang)
// - Old words deprecated
// - Dialects diverge
// - Languages merge (creole formation)
```

### Sign Language Variants

```csharp
// Dedicated sign languages (not just general signs)
// - Full languages expressed via gestures
// - Used by deaf/mute entities
// - Advantage: Silent communication
// - Disadvantage: Requires line of sight
```

### Telepathic Languages

```csharp
// Mind-to-mind languages
// - Perfect clarity (1.0) regardless of proficiency
// - Cannot be overheard
// - Cannot be deceived (thoughts visible)
// - Requires psychic ability
```

---

## Summary

The Communication and Language System provides:

1. **Hierarchical Communication**: Native language → Known languages → General signs
2. **Language Diversity**: Multiple languages with proficiency levels and skillsets
3. **General Signs**: Universal gestures for basic communication (prone to miscommunication)
4. **Intent & Deception**: Share true intentions or disguise them strategically
5. **Language Learning**: Gain proficiency through immersion, teaching, or study
6. **Teaching System**: Transfer knowledge, spells, and languages between entities
7. **Spell Languages**: Verbal incantations require specific languages
8. **Spell Signs**: Somatic components for gesture-based magic
9. **Miscommunication**: Language barriers create emergent narratives
10. **Cross-Game Support**: NPC dialogue (Godgame), alien diplomacy (Space4X)
11. **Integration**: Works with reactions (miscommunication affects relations), tooltips (show language proficiency), procedural generation (gods have unique languages)

**Key Innovation**: Communication is not guaranteed to succeed - language barriers, cultural differences, and general sign ambiguity create opportunities for misunderstanding, strategic deception, and emergent storytelling where the same gesture can mean peace or war depending on context and interpretation.
