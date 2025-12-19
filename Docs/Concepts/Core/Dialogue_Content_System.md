# Dialogue Content System

## Overview

The Dialogue Content System defines what entities communicate about - the actual topics, statements, questions, and responses that enable cooperation, deception, intimidation, and social interaction. Entities discuss everyday matters, share (or hide) their profiles, negotiate, give orders, make threats, request help, and attempt to influence each other's morale and behavior.

**Key Principles**:
- **Topic Categories**: Social, tactical, strategic, personal, transactional
- **Profile Sharing**: Entities identify themselves (or lie about who they are)
- **Intent Communication**: State plans, hide plans, or mislead about intentions
- **Influence Attempts**: Intimidate, rally, demoralize, reassure, persuade
- **Command Hierarchy**: Give orders to subordinates, request aid from equals
- **Cooperation**: Form parties, alliances, coordinate tactics
- **Deception Checks**: Charisma (lying) vs Insight (detecting lies)
- **Context-Aware**: Same statement has different effects based on relations, reputation, personality
- **Deterministic**: Same conversation inputs produce same outcomes

---

## Dialogue Topics

### Social Topics

Everyday conversation, relationship building, information gathering:

```csharp
public enum SocialTopic : byte
{
    // Greetings
    Greeting = 0,               // "Hello", "Good day"
    Farewell = 1,               // "Goodbye", "Safe travels"
    Introduction = 2,           // "I am X from Y"

    // Small Talk
    Weather = 10,               // "Nice weather today"
    LocalNews = 11,             // "Did you hear about...?"
    Gossip = 12,                // "I heard that merchant is a cheat"
    Complaint = 13,             // "These taxes are too high"

    // Information Requests
    AskName = 20,               // "What is your name?"
    AskOrigin = 21,             // "Where are you from?"
    AskProfession = 22,         // "What do you do?"
    AskAffiliation = 23,        // "Which guild/faction?"

    // Relationship Building
    Compliment = 30,            // "You fought well"
    SharedExperience = 31,      // "We both survived the raid"
    CommonEnemy = 32,           // "We both hate the bandits"
    OfferFriendship = 33,       // "I consider you a friend"

    // Emotional
    Apology = 40,               // "I'm sorry for..."
    Forgiveness = 41,           // "I forgive you"
    Gratitude = 42,             // "Thank you"
    Condolence = 43,            // "I'm sorry for your loss"

    // Romance/Flirtation
    Flirt = 50,                 // "You have beautiful eyes"
    ComplimentAppearance = 51,  // "You look lovely today"
    ExpressAttraction = 52,     // "I find you attractive"
    AskCourtship = 53,          // "May I court you?"
    ProposeMarriage = 54,       // "Will you marry me?"
    AcceptRomance = 55,         // "Yes, I feel the same"
    RejectRomance = 56,         // "I'm flattered, but no"
    BreakUp = 57,               // "This isn't working"

    // Familial
    FamilyGreeting = 60,        // "Hello mother/father/sibling"
    FamilyConcern = 61,         // "Are you okay?"
    FamilyAdvice = 62,          // "You should do X" (parental)
    FamilyPride = 63,           // "I'm proud of you"
    FamilyDisappointment = 64,  // "You've let me down"
    InheritanceDiscussion = 65, // "When I'm gone, this will be yours"

    // Neighborly/Community
    NeighborGreeting = 70,      // "Good morning, neighbor"
    OfferHelp = 71,             // "Need any help?"
    BorrowItem = 72,            // "Can I borrow your tools?"
    ReturnItem = 73,            // "Here's what I borrowed"
    CommunityNews = 74,         // "The harvest was good"
    LocalGossip = 75,           // "Did you see what X did?"

    // Outsider/Inter-Village
    OutsiderGreeting = 80,      // "Greetings, traveler"
    AskPermissionPass = 81,     // "May I pass through?"
    AskPermissionStay = 82,     // "May I stay the night?"
    TradingInterest = 83,       // "Your village has goods to trade?"
    DiplomaticContact = 84,     // "I represent village X"
    WarningOutsider = 85,       // "Leave or face consequences"

    // Learning/Teaching
    RequestTeaching = 90,       // "Will you teach me?"
    OfferTeaching = 91,         // "I can teach you"
    AskQuestion = 92,           // "How do I do X?"
    ShareKnowledge = 93,        // "Let me show you"
    AcknowledgeLesson = 94      // "Thank you for teaching me"
}

public struct SocialDialogue : IComponentData
{
    public Entity Speaker;
    public Entity Listener;
    public SocialTopic Topic;
    public DialogueIntent Intent;       // Genuine, deceptive, sarcastic
    public float EmotionalIntensity;    // 0.0 (casual) to 1.0 (intense)
    public RelationshipContext Context; // How they know each other
}

public enum RelationshipContext : byte
{
    Strangers = 0,          // First meeting
    Acquaintances = 1,      // Met before, not close
    Friends = 2,            // Good relation
    CloseFamily = 3,        // Parents, siblings, children
    DistantFamily = 4,      // Cousins, aunts, uncles
    Romantic = 5,           // Dating, married
    Neighbors = 6,          // Live nearby, same village
    SameGuild = 7,          // Guild members
    SameFaction = 8,        // Faction members
    Rivals = 9,             // Competing but not enemies
    Enemies = 10            // Hostile relation
}
```

### Profile Sharing

Entities reveal information about themselves (or lie):

```csharp
public struct ProfileStatement : IComponentData
{
    public Entity Speaker;
    public Entity Listener;
    public ProfileField Field;          // What information is being shared
    public FixedString64Bytes ClaimedValue; // What they claim
    public FixedString64Bytes TrueValue;    // What's actually true
    public bool IsDeceptive;            // Lying?
    public float DeceptionSkill;        // Speaker's charisma
    public float DetectionChance;       // Listener's insight
}

public enum ProfileField : byte
{
    Name = 0,                   // "I am called..."
    Race = 1,                   // "I am human/elf/dwarf"
    Profession = 2,             // "I am a blacksmith"
    Guild = 3,                  // "I belong to Thieves' Guild"
    Faction = 4,                // "I serve the King"
    Origin = 5,                 // "I come from the capital"
    Skills = 6,                 // "I am skilled in combat"
    Level = 7,                  // "I am a master swordsman"
    Equipment = 8,              // "I carry enchanted armor"
    Resources = 9,              // "I have 500 gold"
    Relationships = 10,         // "The Baron is my friend"
    Intentions = 11,            // "I seek the artifact"
    Weaknesses = 12             // "I fear fire" (rarely honest)
}

// Example: Spy disguised as merchant
ProfileStatement spyLie = new()
{
    Speaker = spyEntity,
    Listener = guardEntity,
    Field = ProfileField.Profession,
    ClaimedValue = "Merchant",      // Lie
    TrueValue = "Assassin",         // Truth
    IsDeceptive = true,
    DeceptionSkill = 0.9f,          // Expert liar (high charisma)
    DetectionChance = 0.15f         // Guard has low insight
};

// Guard likely believes the spy (85% success)
```

### Intent Communication

State what you plan to do (or mislead):

```csharp
public struct IntentStatement : IComponentData
{
    public Entity Speaker;
    public Entity Listener;
    public IntentType Type;
    public FixedString128Bytes StatedIntent;
    public FixedString128Bytes TrueIntent;
    public bool IsHonest;
    public IntentTimeframe Timeframe;   // Immediate, near-future, long-term
}

public enum IntentType : byte
{
    // Peaceful
    Trade = 0,                  // "I wish to trade"
    Travel = 1,                 // "I'm passing through"
    Explore = 2,                // "I'm exploring the area"
    Settle = 3,                 // "I want to build a home here"
    Help = 4,                   // "I want to help you"

    // Neutral
    Gather = 10,                // "I'm gathering resources"
    Rest = 11,                  // "I need to rest here"
    Investigate = 12,           // "I'm looking for something"

    // Suspicious
    Spy = 20,                   // "I'm scouting" (rarely honest)
    Steal = 21,                 // "I'm going to take X" (rarely honest)
    Sabotage = 22,              // "I will destroy X" (rarely honest)

    // Hostile
    Attack = 30,                // "I will attack"
    Raid = 31,                  // "I will raid your village"
    Conquer = 32,               // "I will take this land"
    Assassinate = 33            // "I will kill the leader" (rarely stated)
}

// Example: Bandit claiming to be traveler
IntentStatement banditLie = new()
{
    Speaker = banditEntity,
    Listener = villagerEntity,
    Type = IntentType.Travel,
    StatedIntent = "Just passing through peacefully",
    TrueIntent = "Scouting for raid tonight",
    IsHonest = false,
    Timeframe = IntentTimeframe.Immediate
};
```

---

## Influence and Morale

### Intimidation and Demoralization

Attempts to break enemy morale:

```csharp
public struct IntimidationAttempt : IComponentData
{
    public Entity Intimidator;
    public Entity Target;
    public IntimidationType Type;
    public float IntimidatorCharisma;   // Speaker's intimidation skill
    public float TargetResolve;         // Target's resistance to fear
    public float SuccessChance;
    public MoraleEffect Effect;
}

public enum IntimidationType : byte
{
    // Direct Threats
    DeathThreat = 0,            // "I will kill you"
    TortureThreat = 1,          // "You will suffer"
    FamilyThreat = 2,           // "I will harm your loved ones"

    // Psychological
    ShowStrength = 10,          // Demonstrate superior power
    BodyCount = 11,             // "Look at all I've slain"
    Reputation = 12,            // "I am the feared X"
    InevitableDefeat = 13,      // "You cannot win"

    // Group Demoralization
    LeaderDead = 20,            // "Your leader has fallen"
    Outnumbered = 21,           // "You are surrounded"
    Hopeless = 22,              // "Resistance is futile"
    BetrayedByAllies = 23       // "Your allies have fled"
}

public struct MoraleEffect
{
    public MoraleChange Type;
    public float Magnitude;             // 0.0 to 1.0
    public float Duration;              // Seconds
}

public enum MoraleChange : byte
{
    Terrified = 0,              // -80% combat effectiveness, high flee chance
    Demoralized = 1,            // -40% combat effectiveness
    Shaken = 2,                 // -20% combat effectiveness
    Uncertain = 3,              // -10% combat effectiveness
    Neutral = 4,                // No effect
    Encouraged = 5,             // +10% combat effectiveness
    Rallied = 6,                // +20% combat effectiveness
    Inspired = 7,               // +40% combat effectiveness
    Fanatic = 8                 // +80% combat effectiveness, fight to death
}

[BurstCompile]
public partial struct IntimidationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var attempt in SystemAPI.Query<RefRW<IntimidationAttempt>>())
        {
            // Calculate success based on charisma vs resolve
            float successChance = attempt.ValueRO.IntimidatorCharisma /
                                (attempt.ValueRO.IntimidatorCharisma + attempt.ValueRO.TargetResolve);

            // Modify by intimidation type severity
            switch (attempt.ValueRO.Type)
            {
                case IntimidationType.DeathThreat:
                    successChance += 0.2f; // +20% (very direct)
                    break;
                case IntimidationType.Reputation:
                    successChance += 0.1f; // +10% (if reputation is bad)
                    break;
                case IntimidationType.LeaderDead:
                    successChance += 0.3f; // +30% (very effective)
                    break;
            }

            attempt.ValueRW.SuccessChance = math.clamp(successChance, 0f, 0.95f);

            var random = new Unity.Mathematics.Random((uint)attempt.ValueRO.Intimidator.Index);
            if (random.NextFloat() < attempt.ValueRO.SuccessChance)
            {
                // Intimidation successful
                ApplyMoraleEffect(attempt.ValueRO.Target, MoraleChange.Demoralized, 0.6f, 60f);
            }
            else
            {
                // Failed - may actually boost target's resolve
                ApplyMoraleEffect(attempt.ValueRO.Target, MoraleChange.Rallied, 0.3f, 30f);
            }
        }
    }
}

// Example: Warrior intimidates bandit
IntimidationAttempt warriorThreat = new()
{
    Intimidator = warriorEntity,
    Target = banditEntity,
    Type = IntimidationType.ShowStrength,
    IntimidatorCharisma = 0.8f,     // Very intimidating
    TargetResolve = 0.3f,           // Low resolve (coward)
    SuccessChance = 0.8f / (0.8f + 0.3f) = 0.73 → 73% chance
};
// If successful: Bandit becomes Demoralized (-40% combat, may flee)
```

### Rally and Encouragement

Boost ally morale:

```csharp
public struct RallyAttempt : IComponentData
{
    public Entity Leader;
    public Entity Target;               // Single entity or Entity.Null for AOE
    public RallyType Type;
    public float LeaderCharisma;
    public float EffectRadius;          // Meters (if AOE)
}

public enum RallyType : byte
{
    // Pre-Combat
    InspiringSpeech = 0,        // "For glory and honor!"
    SharedCause = 1,            // "We fight for our homes!"
    RememberFallen = 2,         // "Remember those we lost!"

    // During Combat
    VictoryClose = 10,          // "We're winning, push forward!"
    StandFirm = 11,             // "Hold the line!"
    FollowMe = 12,              // "Follow me to victory!"

    // After Setback
    NotOverYet = 20,            // "This isn't over!"
    Regroup = 21,               // "Rally to me!"
    TurnTide = 22               // "We can still win!"
}

// Example: Commander rallies troops
RallyAttempt commanderRally = new()
{
    Leader = commanderEntity,
    Target = Entity.Null,       // AOE
    Type = RallyType.InspiringSpeech,
    LeaderCharisma = 0.9f,      // Exceptional leader
    EffectRadius = 50f          // 50 meter radius
};
// All allies within 50m: +40% combat effectiveness for 120 seconds
```

### Memory Tapping and Shared Bonuses

Entities tap into shared memories to activate powerful temporary bonuses using focus:

```csharp
public struct MemoryTap : IComponentData
{
    public Entity Initiator;
    public SharedMemoryType MemoryType;
    public float InitiatorCharisma;     // Higher charisma = stronger effect
    public float FocusCost;             // Focus consumed per second
    public float EffectRadius;          // Area of effect
    public uint ParticipantCount;       // Entities sharing this memory
    public float BonusMagnitude;        // Calculated from participants
    public float Duration;              // Calculated from participants
}

public enum SharedMemoryType : byte
{
    // Personal/Familial
    Family = 0,             // "Remember our loved ones!"
    Home = 1,               // "We fight for our homes!"
    Ancestors = 2,          // "Our ancestors watch us!"

    // Community
    Village = 10,           // "For our village!"
    Neighbors = 11,         // "We protect each other!"
    CommonPast = 12,        // "We've survived worse together!"

    // Legacy/Dynasty
    Legacy = 20,            // "Uphold our legacy!"
    Dynasty = 21,           // "For the dynasty!"
    Tradition = 22,         // "Preserve our traditions!"

    // Ideological
    Patriotism = 30,        // "For our nation!"
    Pride = 31,             // "Show them our strength!"
    Glory = 32,             // "Fight for eternal glory!"
    Honor = 33,             // "Defend our honor!"
    Faith = 34,             // "Our gods are with us!"

    // Victory/Vengeance
    FallenComrades = 40,    // "Avenge the fallen!"
    PastVictory = 41,       // "We've beaten them before!"
    Revenge = 42,           // "They wronged us!"

    // Survival
    Desperation = 50,       // "Fight or die!"
    LastStand = 51,         // "This is our last stand!"
    Protection = 52         // "Protect the innocent!"
}

public struct SharedMemoryParticipant : IBufferElementData
{
    public Entity ParticipantEntity;
    public float MemoryStrength;        // 0.0 to 1.0 (how much they care)
    public float FocusContribution;     // Focus they contribute
    public bool IsActivelyParticipating;
}

public struct MemoryBonus : IComponentData
{
    public SharedMemoryType Source;
    public float RemainingDuration;
    public BonusProfile Bonuses;        // What bonuses are active
}

public struct BonusProfile
{
    // Combat bonuses
    public float AttackBonus;           // +0% to +100%
    public float DefenseBonus;          // +0% to +100%
    public float AccuracyBonus;         // +0% to +100%

    // Morale bonuses
    public float MoraleBonus;           // +0% to +100%
    public float FearResistance;        // +0% to +100%
    public float PainTolerance;         // +0% to +100%

    // Production bonuses
    public float ProductionSpeed;       // +0% to +100%
    public float ResourceEfficiency;    // +0% to +100%

    // Special effects
    public bool ImmuneToRout;           // Cannot flee
    public bool ImmuneToIntimidation;   // Cannot be demoralized
    public float HealthRegeneration;    // HP/second
}

[BurstCompile]
public partial struct MemoryTapSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (memoryTap, participants) in SystemAPI.Query<
            RefRW<MemoryTap>,
            DynamicBuffer<SharedMemoryParticipant>>())
        {
            // Count active participants sharing this memory
            uint activeCount = 0;
            float totalMemoryStrength = 0f;
            float totalFocusPool = 0f;

            for (int i = 0; i < participants.Length; i++)
            {
                if (participants[i].IsActivelyParticipating)
                {
                    activeCount++;
                    totalMemoryStrength += participants[i].MemoryStrength;
                    totalFocusPool += participants[i].FocusContribution;
                }
            }

            memoryTap.ValueRW.ParticipantCount = activeCount;

            // Bonus magnitude scales with participants and memory strength
            // Formula: BaseBonus * sqrt(ParticipantCount) * (AvgMemoryStrength)
            float avgMemoryStrength = activeCount > 0 ? totalMemoryStrength / activeCount : 0f;
            float participantMultiplier = math.sqrt(activeCount); // Diminishing returns
            float charismaMultiplier = 1f + memoryTap.ValueRO.InitiatorCharisma; // 1.0 to 2.0

            memoryTap.ValueRW.BonusMagnitude = GetBaseBonus(memoryTap.ValueRO.MemoryType)
                * participantMultiplier
                * avgMemoryStrength
                * charismaMultiplier;

            // Duration scales with total focus pool
            // More participants sharing focus = longer duration
            memoryTap.ValueRW.Duration = totalFocusPool / memoryTap.ValueRO.FocusCost;

            // Clamp bonuses
            memoryTap.ValueRW.BonusMagnitude = math.clamp(memoryTap.ValueRW.BonusMagnitude, 0f, 1f);
        }
    }

    private float GetBaseBonus(SharedMemoryType type)
    {
        return type switch
        {
            // Strong emotional bonds
            SharedMemoryType.Family => 0.6f,
            SharedMemoryType.Home => 0.5f,
            SharedMemoryType.FallenComrades => 0.7f,

            // Community bonds
            SharedMemoryType.Village => 0.4f,
            SharedMemoryType.Neighbors => 0.4f,

            // Ideological (variable strength)
            SharedMemoryType.Patriotism => 0.5f,
            SharedMemoryType.Glory => 0.6f,
            SharedMemoryType.Honor => 0.5f,
            SharedMemoryType.Faith => 0.7f,

            // Desperation (very strong but risky)
            SharedMemoryType.LastStand => 0.8f,
            SharedMemoryType.Desperation => 0.7f,

            _ => 0.4f
        };
    }
}

// Example: Stronghold under siege, commander rallies using "Home" memory
MemoryTap strongholdDefense = new()
{
    Initiator = commanderEntity,
    MemoryType = SharedMemoryType.Home,
    InitiatorCharisma = 0.85f,      // Strong leader
    FocusCost = 2f,                  // 2 focus per second
    EffectRadius = 100f,             // Entire stronghold
    ParticipantCount = 0             // Will be calculated
};

// Add participants (all defenders who share "Home" memory)
DynamicBuffer<SharedMemoryParticipant> participants = GetBuffer(strongholdDefenseEntity);
participants.Add(new SharedMemoryParticipant
{
    ParticipantEntity = defender1,
    MemoryStrength = 0.9f,          // Born here, loves home
    FocusContribution = 5f,         // Contributes 5 focus
    IsActivelyParticipating = true
});
participants.Add(new SharedMemoryParticipant
{
    ParticipantEntity = defender2,
    MemoryStrength = 0.6f,          // Immigrant, somewhat attached
    FocusContribution = 3f,
    IsActivelyParticipating = true
});
// ... 50 total defenders

// Result calculation:
// - 50 participants
// - Average memory strength: 0.75 (most were born here)
// - Total focus pool: 200 (50 defenders × avg 4 focus each)
// - Participant multiplier: sqrt(50) = 7.07
// - Charisma multiplier: 1.85
// - Base bonus (Home): 0.5
// Final bonus: 0.5 × 7.07 × 0.75 × 1.85 = 4.9 → clamped to 1.0 (100% bonus)
// Duration: 200 focus ÷ 2 per second = 100 seconds

BonusProfile defenseBonus = new()
{
    AttackBonus = 1.0f,             // +100% attack
    DefenseBonus = 1.0f,            // +100% defense
    MoraleBonus = 1.0f,             // +100% morale
    FearResistance = 1.0f,          // Immune to fear
    ImmuneToRout = true,            // Cannot flee (defending home)
    ImmuneToIntimidation = true     // Cannot be demoralized
};
// 50 defenders become unstoppable for 100 seconds
```

### Stronghold-Wide Rallying

High charisma leaders can direct entire strongholds to a single intent:

```csharp
public struct StrongholdRally : IComponentData
{
    public Entity Leader;
    public Entity Stronghold;           // Fortress/village/city
    public SharedMemoryType RallyType;
    public FixedString512Bytes Speech;  // The rallying speech
    public float LeaderCharisma;
    public float SpeechQuality;         // 0.0 to 1.0 (how well-crafted)
    public uint TotalOccupants;
    public uint AffectedOccupants;      // Who responded
    public float CollectiveFocusPool;   // Total focus available
}

public struct RallySpeechElement : IBufferElementData
{
    public SpeechComponent Type;
    public SharedMemoryType InvokedMemory;
    public float EmotionalImpact;       // 0.0 to 1.0
}

public enum SpeechComponent : byte
{
    // Speech structure
    Opening = 0,            // "Brave defenders!"
    CallToAction = 1,       // "Today we fight!"
    InvokeMemory = 2,       // "Remember your families!"
    InvokeEnemy = 3,        // "The enemy threatens all we hold dear!"
    InvokeConsequence = 4,  // "If we fall, all is lost!"
    InvokeReward = 5,       // "Victory brings glory!"
    InvokeIdentity = 6,     // "We are warriors!"
    Climax = 7,             // "Will you stand with me?!"
    Closing = 8             // "For our home!"
}

[BurstCompile]
public partial struct StrongholdRallySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (rally, speechElements) in SystemAPI.Query<
            RefRW<StrongholdRally>,
            DynamicBuffer<RallySpeechElement>>())
        {
            // Calculate speech effectiveness
            float speechScore = CalculateSpeechQuality(speechElements);
            float charismaModifier = 1f + rally.ValueRO.LeaderCharisma; // 1.0 to 2.0
            float totalEffectiveness = speechScore * charismaModifier;

            // Get all occupants of stronghold
            var occupants = GetStrongholdOccupants(rally.ValueRO.Stronghold);
            rally.ValueRW.TotalOccupants = (uint)occupants.Length;

            uint affectedCount = 0;
            float focusPool = 0f;

            foreach (var occupant in occupants)
            {
                // Check if occupant shares the invoked memory
                float memoryResonance = GetMemoryResonance(occupant, rally.ValueRO.RallyType);

                // Check if occupant is inspired (based on relation to leader, personality, etc.)
                float inspirationChance = totalEffectiveness * memoryResonance;

                var random = new Unity.Mathematics.Random((uint)(rally.ValueRO.Leader.Index + occupant.Index));
                if (random.NextFloat() < inspirationChance)
                {
                    affectedCount++;
                    focusPool += GetEntityFocus(occupant);

                    // Apply memory bonus to this occupant
                    ApplyMemoryBonus(occupant, rally.ValueRO.RallyType, totalEffectiveness);
                }
            }

            rally.ValueRW.AffectedOccupants = affectedCount;
            rally.ValueRW.CollectiveFocusPool = focusPool;
            rally.ValueRW.SpeechQuality = speechScore;
        }
    }

    private float CalculateSpeechQuality(DynamicBuffer<RallySpeechElement> elements)
    {
        // Good speeches have structure: Opening → Build-up → Climax → Closing
        float structureScore = 0f;
        float emotionalScore = 0f;

        bool hasOpening = false;
        bool hasClimax = false;
        bool hasClosing = false;
        int memoryInvocations = 0;

        for (int i = 0; i < elements.Length; i++)
        {
            var element = elements[i];

            switch (element.Type)
            {
                case SpeechComponent.Opening:
                    hasOpening = true;
                    break;
                case SpeechComponent.Climax:
                    hasClimax = true;
                    break;
                case SpeechComponent.Closing:
                    hasClosing = true;
                    break;
                case SpeechComponent.InvokeMemory:
                    memoryInvocations++;
                    break;
            }

            emotionalScore += element.EmotionalImpact;
        }

        // Structure bonus
        if (hasOpening) structureScore += 0.2f;
        if (hasClimax) structureScore += 0.3f;
        if (hasClosing) structureScore += 0.2f;

        // Memory invocations (max 3 for best effect, more becomes repetitive)
        float memoryScore = math.min(memoryInvocations, 3) * 0.1f;

        // Average emotional impact
        float avgEmotional = emotionalScore / math.max(elements.Length, 1);

        return math.clamp(structureScore + memoryScore + avgEmotional, 0f, 1f);
    }
}

// Example: Legendary commander rallies fortress under siege (500 defenders)
StrongholdRally epicSpeech = new()
{
    Leader = legendaryCommanderEntity,
    Stronghold = besiegedFortressEntity,
    RallyType = SharedMemoryType.Home,
    LeaderCharisma = 0.95f,         // Legendary orator
    TotalOccupants = 500
};

// Build the speech
DynamicBuffer<RallySpeechElement> speech = GetBuffer(epicSpeechEntity);
speech.Add(new RallySpeechElement
{
    Type = SpeechComponent.Opening,
    EmotionalImpact = 0.6f
});
speech.Add(new RallySpeechElement
{
    Type = SpeechComponent.InvokeMemory,
    InvokedMemory = SharedMemoryType.Family,
    EmotionalImpact = 0.9f
});
speech.Add(new RallySpeechElement
{
    Type = SpeechComponent.InvokeMemory,
    InvokedMemory = SharedMemoryType.Home,
    EmotionalImpact = 0.95f
});
speech.Add(new RallySpeechElement
{
    Type = SpeechComponent.InvokeConsequence,
    EmotionalImpact = 0.8f
});
speech.Add(new RallySpeechElement
{
    Type = SpeechComponent.Climax,
    EmotionalImpact = 1.0f
});
speech.Add(new RallySpeechElement
{
    Type = SpeechComponent.Closing,
    EmotionalImpact = 0.9f
});

// Result:
// - Speech quality: 0.92 (excellent structure + high emotional impact)
// - Charisma modifier: 1.95
// - Total effectiveness: 1.79 (capped at 1.0 for calculation)
// - 480/500 defenders inspired (96% participation)
// - Collective focus pool: 1920 (480 defenders × avg 4 focus)
// - Duration: 960 seconds (16 minutes) at 2 focus/sec cost
// - Bonuses: +95% combat stats, immune to rout/fear
// Fortress becomes virtually impregnable for 16 minutes
```

---

## Commands and Requests

### Command Hierarchy

Give orders to subordinates:

```csharp
public struct CommandStatement : IComponentData
{
    public Entity Commander;
    public Entity Subordinate;
    public CommandType Type;
    public CommandUrgency Urgency;
    public float3 TargetLocation;       // Where to go/what to do
    public Entity TargetEntity;         // Who/what to interact with
    public bool WillComply;             // Will subordinate obey?
}

public enum CommandType : byte
{
    // Movement
    MoveTo = 0,                 // "Go to X location"
    Follow = 1,                 // "Follow me"
    Retreat = 2,                // "Fall back to X"
    Guard = 3,                  // "Guard this position"

    // Combat
    Attack = 10,                // "Attack X target"
    Defend = 11,                // "Defend X entity"
    Flank = 12,                 // "Flank the enemy"
    CeaseFire = 13,             // "Stop attacking"

    // Non-Combat
    Gather = 20,                // "Gather resources at X"
    Build = 21,                 // "Construct X building"
    Scout = 22,                 // "Explore X area"
    Deliver = 23,               // "Take X to Y"

    // Social
    Negotiate = 30,             // "Talk to X entity"
    Intimidate = 31,            // "Threaten X entity"
    Spy = 32                    // "Infiltrate X group"
}

public enum CommandUrgency : byte
{
    Suggestion = 0,             // "Perhaps you could..."
    Request = 1,                // "Please do X"
    Order = 2,                  // "Do X now" (default for commanders)
    UrgentOrder = 3,            // "DO X IMMEDIATELY!"
    FinalOrder = 4              // "This is my last order"
}

[BurstCompile]
public partial struct CommandComplianceSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var command in SystemAPI.Query<RefRW<CommandStatement>>())
        {
            // Check authority relationship
            var authority = GetAuthority(command.ValueRO.Commander, command.ValueRO.Subordinate);

            // Base compliance from authority
            float complianceChance = authority.ComplianceBase;

            // Modify by urgency
            switch (command.ValueRO.Urgency)
            {
                case CommandUrgency.Suggestion:
                    complianceChance *= 0.6f; // 60% of normal
                    break;
                case CommandUrgency.UrgentOrder:
                    complianceChance *= 1.3f; // 130% of normal
                    break;
                case CommandUrgency.FinalOrder:
                    complianceChance *= 1.5f; // 150% of normal
                    break;
            }

            // Modify by relation
            var relation = GetRelation(command.ValueRO.Commander, command.ValueRO.Subordinate);
            if (relation < -200)
                complianceChance *= 0.5f; // Hate commander, unlikely to obey
            else if (relation > 500)
                complianceChance *= 1.2f; // Respect commander, more likely to obey

            command.ValueRW.WillComply = complianceChance > 0.5f;
        }
    }
}

public struct AuthorityRelationship : IComponentData
{
    public Entity Superior;
    public Entity Subordinate;
    public AuthorityLevel Level;
    public float ComplianceBase;        // 0.0 to 1.0
}

public enum AuthorityLevel : byte
{
    None = 0,               // No authority (0.0 compliance)
    Requested = 1,          // Asking favor (0.3 compliance)
    Hired = 2,              // Paid mercenary (0.6 compliance)
    Enlisted = 3,           // Soldier in army (0.8 compliance)
    Loyal = 4,              // Devoted follower (0.9 compliance)
    Enslaved = 5            // No choice (1.0 compliance, but may rebel)
}
```

### Requests and Pleas

Ask for help (without authority):

```csharp
public struct RequestStatement : IComponentData
{
    public Entity Requester;
    public Entity Requestee;
    public RequestType Type;
    public RequestUrgency Urgency;
    public float OfferedPayment;        // What's being offered in return
    public bool WillGrant;              // Will they help?
}

public enum RequestType : byte
{
    // Assistance
    Help = 0,               // "Please help me"
    Rescue = 1,             // "Save me!"
    Heal = 2,               // "I need healing"
    Shelter = 3,            // "I need a place to stay"

    // Resources
    GiveItem = 10,          // "Can I have X?"
    LendItem = 11,          // "May I borrow X?"
    Trade = 12,             // "Will you trade?"
    Donation = 13,          // "Please give to charity"

    // Information
    Directions = 20,        // "How do I get to X?"
    Knowledge = 21,         // "Teach me X"
    Advice = 22,            // "What should I do?"

    // Social
    Forgiveness = 30,       // "Please forgive me"
    Alliance = 31,          // "Let's work together"
    Join = 32               // "Can I join your group?"
}

public enum RequestUrgency : byte
{
    Casual = 0,             // "If you have time..."
    Important = 1,          // "I really need..."
    Urgent = 2,             // "I desperately need..."
    Desperate = 3,          // "Please, I'm begging you!"
    Dying = 4               // "Help or I die!"
}

// Request compliance based on relation, reputation, urgency
// Higher relation = more likely to help
// Higher urgency = more likely to help (pity)
// Better reputation = more likely to help (trust)
```

### Demands and Threats

Coercive requests backed by force:

```csharp
public struct DemandStatement : IComponentData
{
    public Entity Demander;
    public Entity Target;
    public DemandType Type;
    public FixedString128Bytes Demand;      // What is demanded
    public ThreatType BackingThreat;        // What happens if refused
    public float ThreatCredibility;         // 0.0 to 1.0 (can they follow through?)
    public bool WillComply;
}

public enum DemandType : byte
{
    Surrender = 0,          // "Surrender now"
    GiveResources = 1,      // "Give me all your gold"
    GiveItem = 2,           // "Hand over the artifact"
    Leave = 3,              // "Leave this area"
    StopAction = 4,         // "Stop what you're doing"
    RevealInfo = 5,         // "Tell me where X is"
    BetrayAlly = 6          // "Turn against your friend"
}

public enum ThreatType : byte
{
    ViolenceThreat = 0,     // "Or I attack"
    DeathThreat = 1,        // "Or I kill you"
    TortureThreat = 2,      // "Or you suffer"
    HostageThreat = 3,      // "Or I harm the hostage"
    PropertyThreat = 4,     // "Or I burn your home"
    ReputationThreat = 5,   // "Or I ruin you socially"
    MagicThreat = 6         // "Or I curse you"
}

// Compliance calculation:
// High threat credibility + low target power = likely compliance
// Low threat credibility + high target power = refusal (may counter-threaten)
```

---

## Cooperation and Coordination

### Party Formation

Entities form temporary groups:

```csharp
public struct PartyInvitation : IComponentData
{
    public Entity Inviter;
    public Entity Invitee;
    public PartyPurpose Purpose;
    public float DurationEstimate;      // How long (in-game hours)
    public LootDistribution LootRule;
    public bool WillAccept;
}

public enum PartyPurpose : byte
{
    Exploration = 0,        // Explore dungeon/area together
    Combat = 1,             // Fight enemies together
    Gathering = 2,          // Gather resources together
    Trade = 3,              // Caravan escort
    Social = 4,             // Just traveling together
    Quest = 5               // Complete specific objective
}

public enum LootDistribution : byte
{
    Equal = 0,              // Split evenly
    NeedRoll = 1,           // Roll for items
    LeaderDecides = 2,      // Leader distributes
    FreeForAll = 3,         // Grab what you can
    ContributionBased = 4   // Based on damage/healing dealt
}

// Acceptance based on:
// - Relation (friends more likely to party)
// - Purpose alignment (combat-oriented accept combat quests)
// - Reputation (high reputation inviter = more trust)
// - Current goals (busy entities decline)
```

### Tactical Coordination

Entities coordinate in combat:

```csharp
public struct TacticalCallout : IComponentData
{
    public Entity Caller;
    public Entity[] Recipients;         // Who should hear this
    public CalloutType Type;
    public Entity TargetEntity;         // Enemy to focus/avoid
    public float3 TargetLocation;       // Position to move to
}

public enum CalloutType : byte
{
    // Target Priority
    FocusFire = 0,          // "All attack this one!"
    IgnoreTarget = 1,       // "Don't attack that one"
    PriorityTarget = 2,     // "Kill the healer first!"

    // Movement
    MoveToPosition = 10,    // "Go to X"
    Retreat = 11,           // "Fall back!"
    Flank = 12,             // "Flank left/right"
    Surround = 13,          // "Surround them"

    // Support
    NeedHealing = 20,       // "I need a heal!"
    CoverMe = 21,           // "Cover me while I cast"
    Ready = 22,             // "I'm ready to attack"

    // Warnings
    EnemySpotted = 30,      // "Enemy at X location"
    TrapWarning = 31,       // "Trap ahead!"
    LowHealth = 32,         // "I'm badly hurt"
    OutOfMana = 33          // "No mana left"
}

// Coordination effectiveness based on:
// - Team cohesion (how long they've worked together)
// - Communication clarity (language proficiency)
// - Trust (reputation, relation)
```

---

## Deception and Detection

### Charisma vs Insight

Lying successfully requires charisma, detecting lies requires insight:

```csharp
public struct DeceptionCheck : IComponentData
{
    public Entity Deceiver;
    public Entity Target;
    public FixedString128Bytes Lie;
    public FixedString128Bytes Truth;
    public float DeceiverCharisma;      // Lying skill (0.0 to 1.0)
    public float TargetInsight;         // Lie detection skill (0.0 to 1.0)
    public DeceptionDifficulty Difficulty;
    public bool WasDetected;
}

public enum DeceptionDifficulty : byte
{
    Trivial = 0,            // "I'm fine" (when bleeding) → Easy to detect
    Simple = 1,             // "I'm a merchant" (when actually thief) → Moderate
    Moderate = 2,           // "I serve the King" (elaborate disguise) → Hard
    Complex = 3,            // "I am Lord X" (impersonation) → Very hard
    MasterfulLie = 4        // Complete fabricated identity → Nearly impossible
}

[BurstCompile]
public partial struct DeceptionDetectionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var check in SystemAPI.Query<RefRW<DeceptionCheck>>())
        {
            // Base detection chance: Insight vs Charisma
            float detectionChance = check.ValueRO.TargetInsight /
                                  (check.ValueRO.TargetInsight + check.ValueRO.DeceiverCharisma);

            // Modify by difficulty
            switch (check.ValueRO.Difficulty)
            {
                case DeceptionDifficulty.Trivial:
                    detectionChance += 0.4f; // +40% (obvious lie)
                    break;
                case DeceptionDifficulty.Simple:
                    detectionChance += 0.2f; // +20%
                    break;
                case DeceptionDifficulty.Complex:
                    detectionChance -= 0.2f; // -20%
                    break;
                case DeceptionDifficulty.MasterfulLie:
                    detectionChance -= 0.4f; // -40% (nearly perfect)
                    break;
            }

            // Previous lies affect detection (pattern recognition)
            var trustHistory = GetTrustHistory(check.ValueRO.Deceiver, check.ValueRO.Target);
            if (trustHistory.DeceptiveInteractions > 0)
            {
                // "Fool me once... I'm watching you closely now"
                detectionChance += 0.1f * trustHistory.DeceptiveInteractions; // +10% per previous lie
            }

            detectionChance = math.clamp(detectionChance, 0.05f, 0.95f);

            var random = new Unity.Mathematics.Random((uint)(check.ValueRO.Deceiver.Index + check.ValueRO.Target.Index));
            check.ValueRW.WasDetected = random.NextFloat() < detectionChance;

            if (check.ValueRO.WasDetected)
            {
                // Generate reputation event (liar caught)
                CreateReputationEvent(check.ValueRO.Deceiver, check.ValueRO.Target, -40);
            }
        }
    }
}

// Example: Masterful liar vs perceptive guard
DeceptionCheck spyCheck = new()
{
    Deceiver = masterSpyEntity,
    Target = eliteGuardEntity,
    Lie = "I am the Duke's envoy",
    Truth = "I am an assassin",
    DeceiverCharisma = 0.95f,   // Master spy
    TargetInsight = 0.8f,       // Elite guard (very perceptive)
    Difficulty = DeceptionDifficulty.Complex
};

// Detection chance: 0.8 / (0.8 + 0.95) = 0.457 → 45.7%
// Difficulty penalty: -20%
// Final: 25.7% chance guard detects the lie
// Spy has good odds, but not guaranteed
```

---

## Context-Aware Responses

Same statement, different outcomes based on context:

```csharp
public struct DialogueResponse : IComponentData
{
    public Entity Responder;
    public Entity Speaker;
    public ResponseType Type;
    public FixedString128Bytes ResponseText;
    public float EmotionalTone;         // -1.0 (hostile) to +1.0 (friendly)
}

public enum ResponseType : byte
{
    // Positive
    Agreement = 0,          // "Yes, I agree"
    Acceptance = 1,         // "Okay, I'll do it"
    Gratitude = 2,          // "Thank you"
    Encouragement = 3,      // "You can do it!"

    // Neutral
    Acknowledgment = 10,    // "I understand"
    Question = 11,          // "What do you mean?"
    Deflection = 12,        // "Let's talk about something else"

    // Negative
    Disagreement = 20,      // "No, I disagree"
    Refusal = 21,           // "I won't do that"
    Insult = 22,            // "You fool!"
    Threat = 23,            // "Back off or else"

    // Emotional
    Anger = 30,             // "How dare you!"
    Fear = 31,              // "Please don't hurt me"
    Sadness = 32,           // "That makes me sad"
    Joy = 33                // "That's wonderful!"
}

// Example: "Join my party" request
// Context 1: Friend asks, relation +600
Response friendResponse = new()
{
    Type = ResponseType.Acceptance,
    ResponseText = "Of course! I'd be happy to help",
    EmotionalTone = 0.8f    // Very friendly
};

// Context 2: Enemy asks, relation -400
Response enemyResponse = new()
{
    Type = ResponseType.Refusal,
    ResponseText = "Never! I'd rather die",
    EmotionalTone = -0.9f   // Very hostile
};

// Context 3: Stranger asks, relation 0, high reputation
Response strangerResponse = new()
{
    Type = ResponseType.Acceptance,
    ResponseText = "I've heard of you. I'll join",
    EmotionalTone = 0.3f    // Cautiously friendly
};
```

---

## Integration Examples

### Godgame: Village Diplomacy

```csharp
// Scenario: Two villages negotiating alliance

// Village A sends diplomat
IntentStatement villageAIntent = new()
{
    Speaker = villageADiplomatEntity,
    Listener = villageBLeaderEntity,
    Type = IntentType.Help,
    StatedIntent = "We want to ally against bandits",
    TrueIntent = "We want to ally against bandits", // Honest
    IsHonest = true
};

// Village B leader responds based on:
// - Village A reputation (Trading: +400 = Liked)
// - Past interactions (Previously traded fairly)
// - Current threat level (Bandits ARE a problem)
// - Alignment compatibility (Both peaceful)

Response villageBResponse = new()
{
    Type = ResponseType.Acceptance,
    ResponseText = "We accept your alliance",
    EmotionalTone = 0.6f
};

// Alliance formed, mutual defense pact created
```

### Space4X: First Contact Protocol

```csharp
// Human ship meets alien ship for first time

// Step 1: Greeting (general signs - no common language)
SocialDialogue humanGreeting = new()
{
    Speaker = humanDiplomatEntity,
    Listener = alienCommanderEntity,
    Topic = SocialTopic.Greeting,
    Intent = DialogueIntent.Genuine
};
// Clarity: 70% (general signs)
// Alien understands: "Peaceful contact"

// Step 2: Profile exchange
ProfileStatement humanProfile = new()
{
    Speaker = humanDiplomatEntity,
    Listener = alienCommanderEntity,
    Field = ProfileField.Faction,
    ClaimedValue = "United Earth Alliance",
    TrueValue = "United Earth Alliance", // Honest
    IsDeceptive = false
};
// Clarity: 40% (language barrier)
// Alien understands: "Earth... alliance?"

// Step 3: Intent statement
IntentStatement humanIntent = new()
{
    Speaker = humanDiplomatEntity,
    Type = IntentType.Trade,
    StatedIntent = "We seek peaceful trade",
    TrueIntent = "We seek peaceful trade",
    IsHonest = true
};
// Clarity: 50%
// Alien understands general peaceful intent

// After 10 interactions, humans learn basic alien language
// Clarity improves to 80%
// More complex diplomacy becomes possible
```

---

## Performance Considerations

```csharp
// Dialogue caching for NPCs
public struct DialogueCache
{
    public FixedList128Bytes<CachedResponse> Responses;
}

public struct CachedResponse
{
    public SocialTopic Topic;
    public Entity LastAsked;
    public ResponseType LastResponse;
    public double CacheTime;
}

// Only recalculate responses when:
// - Relation changes significantly (±50)
// - Reputation changes significantly (±100)
// - New information learned
// - Cache expires (60 seconds)
```

**Profiling Targets**:
```
Deception Check:      <0.1ms per attempt
Command Compliance:   <0.05ms per command
Response Generation:  <0.1ms per response
Intimidation Effect:  <0.15ms per attempt
Memory Tap Calc:      <0.2ms per tap (participant counting)
Stronghold Rally:     <0.5ms per rally (500 occupants)
Speech Analysis:      <0.1ms per speech
────────────────────────────────────────
Total (100 dialogues): <4.0ms per frame
Total (10 rallies):    <5.0ms per frame
```

---

## Summary

The Dialogue Content System provides:

1. **Social Topics**: Greetings, small talk, information requests, relationship building, romance, familial, neighborly, outsider/inter-village, learning/teaching
2. **Profile Sharing**: Reveal (or lie about) name, profession, guild, skills, intentions
3. **Charisma vs Insight**: Lying skill vs detection skill determines deception success
4. **Influence**: Intimidate (demoralize), rally (inspire), persuade
5. **Memory Tapping**: Tap shared memories (family, home, legacy, patriotism, glory) to activate temporary bonuses using focus
6. **Shared Memory Scaling**: More participants sharing a memory = stronger and longer bonuses (diminishing returns via sqrt)
7. **Stronghold Rallying**: High charisma leaders can rally entire fortresses/villages with structured speeches
8. **Speech Structure**: Opening → Build-up → Climax → Closing, invoking multiple memories for emotional impact
9. **Focus Resource**: Collective focus pool from all participants determines rally duration
10. **Commands**: Give orders to subordinates (compliance based on authority + relation)
11. **Requests**: Ask for help (granted based on relation + reputation + urgency)
12. **Demands**: Coercive requests backed by credible threats
13. **Cooperation**: Form parties, coordinate tactics, share callouts
14. **Context-Aware**: Same statement produces different responses based on relations, reputation, personality, relationship context
15. **Integration**: Works with communication (language clarity affects understanding), reputation (lies damage trust), reactions (responses trigger relation changes)

**Key Innovations**:
- Communication is not just information exchange but **influence warfare** - entities lie, intimidate, inspire, and manipulate each other through dialogue
- **Memory tapping creates emergent collective power** - defenders of their homeland fight harder, warriors avenging fallen comrades gain bonuses, desperate last stands produce superhuman effort
- **Charismatic leaders amplify group strength** - one legendary commander can turn 500 ordinary defenders into an unstoppable force for 16 minutes by invoking shared memories and ideals
- Success determined by charisma, insight, reputation, relationship context, and **how many entities share the invoked memory**
