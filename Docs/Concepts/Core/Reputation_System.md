# Reputation System

## Overview

The Reputation System tracks how entities are perceived by others based on past actions, behaviors, and interactions. Reputation influences trust, trade opportunities, diplomatic access, and social standing. Entities build or lose reputation through honest dealings, successful contracts, broken promises, acts of heroism, betrayals, and lies. Reputation spreads through witnesses, gossip, and trade networks, affecting both individual entities and aggregate groups (guilds, factions, settlements).

**Key Principles**:
- **Action-Based**: Reputation earned through behavior, not inherited or granted
- **Multi-Domain**: Separate reputations for trading, combat, diplomacy, magic, etc.
- **Spreads Organically**: Witnesses share information, creating reputation networks
- **Individual & Aggregate**: Both single entities and groups have reputations
- **Affects Opportunities**: High reputation unlocks access, low reputation closes doors
- **Slowly Changes**: Trust is hard to build, easy to lose, slow to rebuild
- **Tied to Communication**: Lies, deception, and broken promises damage reputation
- **Deterministic**: Same actions in same context = same reputation changes

---

## Core Concepts

### Reputation Domains

Entities have separate reputations in different domains:

```csharp
public struct EntityReputation : IComponentData
{
    public ReputationDomain[] Domains;       // Multiple reputation scores
    public int GlobalReputation;             // Overall reputation (-1000 to +1000)
    public ReputationTier Tier;              // Simplified tier for UI
    public uint WitnessCount;                // How many entities know about this entity
    public bool IsNotorious;                 // Famous for bad deeds
    public bool IsRenowned;                  // Famous for good deeds
}

public struct ReputationDomain : IBufferElementData
{
    public DomainType Type;
    public int Score;                        // -1000 to +1000
    public uint PositiveActions;             // Count of good deeds
    public uint NegativeActions;             // Count of bad deeds
    public float ChangeRate;                 // How quickly reputation changes
    public double LastUpdated;               // Tick of last reputation change
}

public enum DomainType : byte
{
    Trading = 0,        // Honest deals, fair prices, fulfilling contracts
    Combat = 1,         // Honorable fighting, accepting surrenders, prisoner treatment
    Diplomacy = 2,      // Keeping promises, honoring treaties, truthfulness
    Magic = 3,          // Ethical magic use, helping vs harming, spell integrity
    Crafting = 4,       // Quality goods, reliable work, meeting deadlines
    Leadership = 5,     // Protecting followers, fair treatment, strategic competence
    Religion = 6,       // Piety, temple support, miracle use
    Criminal = 7,       // Success in crime (higher = more feared)
    Exploration = 8,    // Discovery, mapping, navigation skill
    Teaching = 9,       // Knowledge sharing, student success
    General = 10        // Overall character, unspecified reputation
}

public enum ReputationTier : byte
{
    Despised = 0,       // -1000 to -750: Hated, attacked on sight
    Loathed = 1,        // -750 to -500: Deeply distrusted, avoided
    Disliked = 2,       // -500 to -250: Not trusted, poor deals
    Neutral = 3,        // -250 to +250: Unknown or mixed reputation
    Liked = 4,          // +250 to +500: Trusted for basic dealings
    Respected = 5,      // +500 to +750: Highly trusted, good deals
    Revered = 6,        // +750 to +1000: Legendary reputation, best opportunities
}
```

---

## Reputation Events

Actions generate reputation events that modify scores:

```csharp
public struct ReputationEvent : IComponentData
{
    public Entity Subject;                   // Who gained/lost reputation
    public Entity Witness;                   // Who observed the action (Entity.Null if public)
    public DomainType Domain;                // Which reputation domain
    public ReputationChange Change;
    public int ImpactMagnitude;              // How much reputation changes (-100 to +100)
    public bool IsPublic;                    // Does this spread to non-witnesses?
    public float SpreadRadius;               // Meters (gossip range)
    public uint SpreadCount;                 // How many entities learn about this
}

public enum ReputationChange : byte
{
    // Trading
    HonestDeal = 0,             // +5 to +20: Fair trade, both parties satisfied
    Swindle = 1,                // -20 to -50: Cheated in trade, lied about quality
    FulfilledContract = 2,      // +10 to +30: Completed contract as promised
    BrokenContract = 3,         // -30 to -80: Failed to fulfill contract
    Charity = 4,                // +20 to +50: Gave items/resources freely
    Theft = 5,                  // -50 to -100: Stole items/resources

    // Combat
    HonorableDuel = 10,         // +10 to +30: Fought fairly, accepted surrender
    Cowardice = 11,             // -10 to -30: Fled from battle, abandoned allies
    Heroism = 12,               // +30 to +80: Saved others, great personal risk
    Atrocity = 13,              // -80 to -150: War crimes, civilian harm
    AcceptedSurrender = 14,     // +5 to +15: Spared defeated enemy
    KilledPrisoner = 15,        // -40 to -100: Executed helpless captive

    // Diplomacy
    KeptPromise = 20,           // +10 to +40: Fulfilled diplomatic agreement
    BrokePromise = 21,          // -40 to -100: Violated agreement
    TruthfulStatement = 22,     // +2 to +10: Honest in negotiations
    CaughtLying = 23,           // -15 to -60: Detected in deception
    PeaceBrokered = 24,         // +30 to +80: Successfully negotiated peace
    BetrayedAlliance = 25,      // -100 to -200: Turned on ally

    // Magic
    HelpfulMagic = 30,          // +10 to +40: Healing, beneficial spells
    HarmfulMagic = 31,          // -20 to -80: Curses, harmful spells on innocents
    SharedKnowledge = 32,       // +15 to +50: Taught spells/magic freely
    MagicTheft = 33,            // -30 to -80: Stole spells, broke magical trust

    // Crafting
    QualityWork = 40,           // +10 to +30: Excellent craftsmanship
    Shoddy Work = 41,           // -15 to -40: Poor quality, breaks easily
    MetDeadline = 42,           // +5 to +15: Completed work on time
    MissedDeadline = 43,        // -10 to -30: Failed to deliver on time

    // Leadership
    ProtectedFollowers = 50,    // +20 to +60: Defended subordinates
    AbandonedFollowers = 51,    // -50 to -120: Left followers to die
    FairTreatment = 52,         // +10 to +30: Equitable resource distribution
    Exploitation = 53,          // -30 to -80: Abused power, unfair treatment

    // General
    Witnessed = 60,             // Variable: Observer confirms reputation
    RumorSpread = 61,           // Variable: Second-hand reputation gain/loss
    TimeDecay = 62              // Small: Reputation slowly returns to neutral
}

// Example: Merchant caught swindling customer
ReputationEvent swindleEvent = new()
{
    Subject = merchantEntity,
    Witness = customerEntity,
    Domain = DomainType.Trading,
    Change = ReputationChange.Swindle,
    ImpactMagnitude = -40,
    IsPublic = true,            // Customer will tell others
    SpreadRadius = 500f,        // Gossip spreads 500m
    SpreadCount = 10            // ~10 entities will hear about it
};
```

---

## Reputation Spread and Gossip

Reputation spreads through witnesses and social networks:

```csharp
[BurstCompile]
public partial struct ReputationSpreadSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var repEvent in SystemAPI.Query<RefRO<ReputationEvent>>())
        {
            if (!repEvent.ValueRO.IsPublic)
            {
                // Private event: Only witness knows
                UpdateReputation(
                    repEvent.ValueRO.Subject,
                    repEvent.ValueRO.Witness,
                    repEvent.ValueRO.Domain,
                    repEvent.ValueRO.ImpactMagnitude
                );
                continue;
            }

            // Public event: Spreads to nearby entities
            var nearbyEntities = GetEntitiesInRadius(
                repEvent.ValueRO.Subject,
                repEvent.ValueRO.SpreadRadius
            );

            int spreadsSoFar = 0;
            foreach (var nearby in nearbyEntities)
            {
                if (spreadsSoFar >= repEvent.ValueRO.SpreadCount)
                    break;

                // Gossip: Reputation impact reduced (60% of original)
                int gossipImpact = (int)(repEvent.ValueRO.ImpactMagnitude * 0.6f);

                UpdateReputation(
                    repEvent.ValueRO.Subject,
                    nearby,
                    repEvent.ValueRO.Domain,
                    gossipImpact
                );

                spreadsSoFar++;
            }
        }
    }
}

public struct WitnessedReputation : IBufferElementData
{
    public Entity SubjectEntity;             // Who this reputation is about
    public DomainType Domain;                // Which domain
    public int PerceivedScore;               // This witness's view (-1000 to +1000)
    public uint ConfidenceLevel;             // How sure (0-100): 100 = witnessed, 50 = gossip
    public double LastUpdated;               // When this was last confirmed
}

// Example: Customer entity stores witnessed reputation
WitnessedReputation customerView = new()
{
    SubjectEntity = merchantEntity,
    Domain = DomainType.Trading,
    PerceivedScore = -40,        // Merchant is a cheat!
    ConfidenceLevel = 100,       // Directly experienced
    LastUpdated = currentTick
};

// Example: Friend hears gossip
WitnessedReputation friendView = new()
{
    SubjectEntity = merchantEntity,
    Domain = DomainType.Trading,
    PerceivedScore = -24,        // -40 × 0.6 (gossip reduction)
    ConfidenceLevel = 50,        // Second-hand info
    LastUpdated = currentTick
};
```

---

## Aggregate Reputation

Groups have collective reputations:

```csharp
public struct AggregateReputation : IComponentData
{
    public Entity AggregateEntity;           // Guild, faction, settlement
    public AggregateReputationType Type;
    public ReputationDomain[] Domains;       // Same as individual reputation
    public int MemberCount;                  // How many members contribute
    public float AverageContribution;        // Average member reputation
    public bool InheritToMembers;            // Do members start with this reputation?
}

public enum AggregateReputationType : byte
{
    Guild = 0,          // Professional organization
    Faction = 1,        // Political/military group
    Settlement = 2,     // Village, city, nation
    Corporation = 3,    // Trading company (Space4X)
    Alliance = 4,       // Coalition of groups
    Religion = 5        // Church, temple network
}

// Aggregate reputation is average of member reputations + group actions
public struct AggregateReputationCalculation
{
    public int MemberAverageReputation;      // Average of all member scores
    public int GroupActionReputation;        // Reputation from group-level actions
    public float Weight;                     // How much each contributes (0.0 to 1.0)
}

// Example: Trading guild reputation
// Member average: +300 (generally honest traders)
// Group actions: +100 (guild enforces fair trade standards)
// Weight: 0.7 member, 0.3 group
// Final: (300 × 0.7) + (100 × 0.3) = 210 + 30 = +240 (Liked tier)

// If one member swindles (-50 reputation event):
// Member's personal reputation: -50
// Guild's reputation: (295 × 0.7) + (100 × 0.3) = 206.5 + 30 = +236.5
// Guild's reputation slightly damaged by member's actions
```

---

## Reputation Effects

Reputation unlocks or blocks opportunities:

```csharp
public struct ReputationRequirement : IComponentData
{
    public DomainType RequiredDomain;
    public int MinimumScore;                 // Minimum reputation needed
    public int OptimalScore;                 // Score for best outcome
    public RequirementType Type;
}

public enum RequirementType : byte
{
    Access = 0,         // Access to location, guild, service
    Discount = 1,       // Better prices in trading
    QuestUnlock = 2,    // Unlock special missions
    Teaching = 3,       // Learn advanced skills/spells
    Diplomatic = 4,     // Negotiate treaties, alliances
    Hiring = 5,         // Recruit followers, mercenaries
    MagicAccess = 6     // Learn restricted spells
}

// Example: Guild access
ReputationRequirement guildEntry = new()
{
    RequiredDomain = DomainType.Trading,
    MinimumScore = 250,          // Must be "Liked" (tier 4)
    Type = RequirementType.Access
};

// Example: Master-level teaching
ReputationRequirement masterTraining = new()
{
    RequiredDomain = DomainType.Magic,
    MinimumScore = 750,          // Must be "Revered" (tier 6)
    Type = RequirementType.Teaching
};

// Example: Trade discount
ReputationRequirement tradeBonus = new()
{
    RequiredDomain = DomainType.Trading,
    MinimumScore = 100,          // Slight discount at neutral+
    OptimalScore = 750,          // Maximum discount at revered
    Type = RequirementType.Discount
};

// Discount calculation: Linear interpolation
float discount = math.lerp(0.05f, 0.30f, (reputation - 100f) / (750f - 100f));
// 100 rep: 5% discount
// 425 rep: 17.5% discount
// 750 rep: 30% discount
```

---

## Guild Signs and Membership Proof

Guilds use secret signs to identify members and grant access:

```csharp
public struct GuildMembership : IComponentData
{
    public Entity GuildEntity;
    public FixedString64Bytes GuildName;
    public MembershipRank Rank;
    public GuildSign SecretSign;             // Membership proof
    public bool CanTeachSign;                // Can share with trusted allies
    public bool IsRevoked;                   // Expelled from guild
}

public struct GuildSign : IComponentData
{
    public FixedString64Bytes SignId;        // "BlacksmithGuild_MasterSign"
    public SignComplexity Complexity;        // How hard to perform
    public FixedList128Bytes<GestureStep> Sequence; // Ordered gestures
    public float PerformanceTime;            // Seconds to complete
    public bool RequiresMagic;               // Magical component?
    public AccessLevel GrantsAccess;         // What does this sign unlock
}

public enum MembershipRank : byte
{
    Initiate = 0,       // Novice, limited access
    Member = 1,         // Full member, standard access
    Journeyman = 2,     // Experienced, some leadership
    Master = 3,         // Expert, teaches others
    GuildMaster = 4     // Leader, highest authority
}

public enum AccessLevel : byte
{
    PublicArea = 0,     // Anyone can enter
    MemberArea = 1,     // Members only
    JourneymanArea = 2, // Journeyman+
    MasterArea = 3,     // Master+
    SecretVault = 4     // Guild master only
}

public struct GestureStep
{
    public GeneralSignType Gesture;
    public float Duration;
    public LimbType RequiredLimb;
}

// Example: Thieves' Guild secret sign
GuildSign thievesSign = new()
{
    SignId = "ThievesGuild_MemberSign",
    Complexity = SignComplexity.Moderate,
    Sequence = new()
    {
        new GestureStep { Gesture = GeneralSignType.PointAtSelf, Duration = 0.5f, RequiredLimb = LimbType.Hands },
        new GestureStep { Gesture = GeneralSignType.CrossArms, Duration = 1.0f, RequiredLimb = LimbType.Hands },
        new GestureStep { Gesture = GeneralSignType.FingerAcrossThroat, Duration = 0.5f, RequiredLimb = LimbType.Hands }
        // Sequence: Point at self → Cross arms → Finger across throat
        // Meaning: "I am a shadow that brings death" (thieves' motto)
    },
    PerformanceTime = 2.0f,
    RequiresMagic = false,
    GrantsAccess = AccessLevel.MemberArea
};
```

### Teaching Guild Signs to Trusted Entities

Members can share signs with trusted allies:

```csharp
public struct SignTeaching : IComponentData
{
    public Entity Teacher;
    public Entity Student;
    public GuildSign SignBeingTaught;
    public float TeachingProgress;           // 0.0 to 1.0
    public float RequiredTrustLevel;         // Minimum relation to teach
    public bool IsForbidden;                 // Guild forbids sharing?
    public int ReputationRiskIfCaught;       // Penalty if caught sharing
}

[BurstCompile]
public partial struct SignTeachingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (teaching, relation) in SystemAPI.Query<
            RefRW<SignTeaching>,
            RefRO<EntityRelation>>())  // Relation between teacher and student
        {
            // Check trust requirement
            if (relation.ValueRO.RelationScore < teaching.ValueRO.RequiredTrustLevel)
            {
                // Not trusted enough, cancel teaching
                continue;
            }

            // Progress teaching
            teaching.ValueRW.TeachingProgress += deltaTime / 60f; // 60 seconds to teach

            if (teaching.ValueRO.TeachingProgress >= 1.0f)
            {
                // Student learned the sign
                GrantGuildSign(teaching.ValueRO.Student, teaching.ValueRO.SignBeingTaught);

                // If forbidden, risk reputation damage if caught
                if (teaching.ValueRO.IsForbidden)
                {
                    // 20% chance guild finds out
                    var random = new Unity.Mathematics.Random((uint)teaching.ValueRO.Teacher.Index);
                    if (random.NextFloat() < 0.2f)
                    {
                        // Reputation hit for teacher
                        ApplyReputationChange(
                            teaching.ValueRO.Teacher,
                            teaching.ValueRO.SignBeingTaught.GuildEntity,
                            teaching.ValueRO.ReputationRiskIfCaught
                        );
                    }
                }
            }
        }
    }
}

// Example: Dying guild member teaches sign to refugee
SignTeaching emergencyTeaching = new()
{
    Teacher = dyingMemberEntity,
    Student = refugeeEntity,
    SignBeingTaught = thievesGuildSign,
    RequiredTrustLevel = 500,    // Must deeply trust (Respected tier)
    IsForbidden = true,          // Guild forbids sharing
    ReputationRiskIfCaught = -100 // Heavy penalty if caught
};

// Scenario:
// 1. Member mortally wounded in raid
// 2. Refugees fleeing with member
// 3. Member trusts refugee leader (+600 relation)
// 4. Teaches guild sign before dying
// 5. Refugee can now access Thieves' Guild safe houses
// 6. Guild may or may not discover unauthorized sign sharing
```

### Magical Guild Doors

Some guilds use magical doors that verify signs:

```csharp
public struct MagicalDoor : IComponentData
{
    public Entity DoorEntity;
    public GuildSign RequiredSign;           // Sign needed to open
    public MembershipRank MinimumRank;       // Minimum rank required
    public bool VerifiesGuildMembership;     // Checks actual membership vs just sign
    public float OpenDuration;               // Seconds door stays open
    public DoorState State;
}

public enum DoorState : byte
{
    Closed = 0,
    Opening = 1,
    Open = 2,
    Closing = 3,
    Locked = 4          // Sealed, cannot be opened even with sign
}

[BurstCompile]
public partial struct MagicalDoorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (door, position) in SystemAPI.Query<
            RefRW<MagicalDoor>,
            RefRO<LocalTransform>>())
        {
            // Check for entities performing sign nearby
            var nearbyEntities = GetEntitiesInRadius(position.ValueRO.Position, 2.0f);

            foreach (var entity in nearbyEntities)
            {
                if (IsPerformingSign(entity, door.ValueRO.RequiredSign))
                {
                    // Verify sign is correct
                    if (VerifySignAuthenticity(entity, door.ValueRO.RequiredSign))
                    {
                        // Optionally verify actual guild membership
                        if (door.ValueRO.VerifiesGuildMembership)
                        {
                            var membership = GetGuildMembership(entity);
                            if (membership.Rank < door.ValueRO.MinimumRank)
                            {
                                // Sign is correct but rank insufficient
                                continue;
                            }
                        }

                        // Open door
                        door.ValueRW.State = DoorState.Opening;
                    }
                }
            }
        }
    }
}

// Example: Master Blacksmith workshop door
MagicalDoor workshopDoor = new()
{
    RequiredSign = blacksmithGuildMasterSign,
    MinimumRank = MembershipRank.Master,
    VerifiesGuildMembership = true,      // Must be actual member, not just know sign
    OpenDuration = 5.0f,
    State = DoorState.Closed
};

// Only Master Blacksmiths can enter
// Refugees with taught sign cannot access (membership check fails)
```

---

## Reputation and Communication Integration

Lies and broken promises damage reputation:

```csharp
// When communication contains deception:
if (attempt.WasDeceptive && attempt.WasDetected)
{
    // Generate reputation event
    ReputationEvent lieDetected = new()
    {
        Subject = attempt.Sender,
        Witness = attempt.Receiver,
        Domain = DomainType.Diplomacy,
        Change = ReputationChange.CaughtLying,
        ImpactMagnitude = -30,       // Moderate reputation loss
        IsPublic = true,
        SpreadRadius = 200f,
        SpreadCount = 5
    };

    // Receiver now distrusts sender
    // Reputation spreads to nearby entities via gossip
}

// Future interactions check reputation:
if (GetReputationWithEntity(sender, receiver, DomainType.Diplomacy) < -100)
{
    // Receiver distrusts sender due to past lies
    attempt.Clarity *= 0.5f;         // 50% clarity penalty
    // "I don't believe anything you say"
}
```

### Trust Learning from Repeated Interactions

Entities remember who lied to them:

```csharp
public struct TrustHistory : IBufferElementData
{
    public Entity OtherEntity;
    public uint HonestInteractions;          // Times they were truthful
    public uint DeceptiveInteractions;       // Times they lied
    public float TrustScore;                 // 0.0 (never trust) to 1.0 (always trust)
    public double LastInteraction;
}

[BurstCompile]
public partial struct TrustUpdateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (attempt, trustHistory) in SystemAPI.Query<
            RefRO<CommunicationAttempt>,
            DynamicBuffer<TrustHistory>>())
        {
            // Find trust entry for this entity pair
            for (int i = 0; i < trustHistory.Length; i++)
            {
                if (trustHistory[i].OtherEntity == attempt.ValueRO.Sender)
                {
                    var entry = trustHistory[i];

                    if (attempt.ValueRO.WasDeceptive && attempt.ValueRO.WasDetected)
                    {
                        // Caught in a lie
                        entry.DeceptiveInteractions++;
                    }
                    else if (!attempt.ValueRO.WasDeceptive)
                    {
                        // Honest communication
                        entry.HonestInteractions++;
                    }

                    // Recalculate trust
                    entry.TrustScore = (float)entry.HonestInteractions /
                                      (entry.HonestInteractions + entry.DeceptiveInteractions);

                    entry.LastInteraction = SystemAPI.Time.ElapsedTime;
                    trustHistory[i] = entry;
                    break;
                }
            }
        }
    }
}

// Example: Merchant who lied 3 times, was honest 7 times
TrustHistory merchantTrust = new()
{
    OtherEntity = merchantEntity,
    HonestInteractions = 7,
    DeceptiveInteractions = 3,
    TrustScore = 7f / (7f + 3f) = 0.7f // 70% trust
};

// Customer is wary but not completely distrustful
// Future deceptions will lower trust further
// Consistent honesty can rebuild trust over time
```

---

## Aggregate Reputation Effects on Trade

Groups with poor reputations struggle to find trading partners:

```csharp
public struct TradeOpportunity : IComponentData
{
    public Entity Trader;
    public Entity PotentialPartner;
    public DomainType RelevantDomain;        // Usually Trading
    public int MinimumReputation;            // Will not trade below this
    public float PriceModifier;              // 0.5 (50% discount) to 2.0 (200% markup)
    public bool WillingToTrade;
}

[BurstCompile]
public partial struct TradeReputationCheckSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var opportunity in SystemAPI.Query<RefRW<TradeOpportunity>>())
        {
            // Get reputation of trader as seen by potential partner
            int reputation = GetReputationBetween(
                opportunity.ValueRO.Trader,
                opportunity.ValueRO.PotentialPartner,
                opportunity.ValueRO.RelevantDomain
            );

            // Check minimum threshold
            if (reputation < opportunity.ValueRO.MinimumReputation)
            {
                opportunity.ValueRW.WillingToTrade = false;
                continue;
            }

            // Calculate price modifier based on reputation
            // -500 rep: 2.0× markup (you're a cheater, I'm charging extra)
            // 0 rep: 1.0× normal price
            // +500 rep: 0.7× discount (you're trustworthy, here's a deal)
            opportunity.ValueRW.PriceModifier = math.lerp(2.0f, 0.7f, (reputation + 500f) / 1000f);
            opportunity.ValueRW.WillingToTrade = true;
        }
    }
}

// Example: Guild with bad reputation
// AggregateReputation (Trading): -300 (Disliked tier)
//
// Trying to trade with merchant:
// Merchant's minimum reputation: -100
// Guild's reputation: -300 → Below threshold
// Result: Merchant refuses to trade
//
// Guild must improve reputation to access trade
// Options:
// 1. Complete honest trades with others to rebuild reputation
// 2. Make reparations to those they swindled
// 3. Change guild policies (enforce fair trade)
// 4. Find desperate traders willing to deal with anyone
```

---

## Reputation Decay and Restoration

Reputation slowly changes over time:

```csharp
public struct ReputationDecay : IComponentData
{
    public float NegativeDecayRate;          // Rate bad reputation recovers (+1/day)
    public float PositiveDecayRate;          // Rate good reputation fades (-0.5/day)
    public int NeutralTarget;                // What reputation trends toward (0)
    public float TimeAccelerator;            // Multiplier for inactive entities
}

[BurstCompile]
public partial struct ReputationDecaySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        double deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (reputation, decay) in SystemAPI.Query<
            DynamicBuffer<ReputationDomain>,
            RefRO<ReputationDecay>>())
        {
            for (int i = 0; i < reputation.Length; i++)
            {
                var domain = reputation[i];

                if (domain.Score < decay.ValueRO.NeutralTarget)
                {
                    // Negative reputation slowly recovers
                    domain.Score += (int)(decay.ValueRO.NegativeDecayRate * deltaTime);
                    domain.Score = math.min(domain.Score, decay.ValueRO.NeutralTarget);
                }
                else if (domain.Score > decay.ValueRO.NeutralTarget)
                {
                    // Positive reputation slowly fades (unless reinforced)
                    domain.Score -= (int)(decay.ValueRO.PositiveDecayRate * deltaTime);
                    domain.Score = math.max(domain.Score, decay.ValueRO.NeutralTarget);
                }

                reputation[i] = domain;
            }
        }
    }
}

// Example: Criminal who stops crime
// Year 0: -600 reputation (Loathed)
// Year 1: -365 reputation (no new crimes, +1/day recovery)
// Year 2: -130 reputation
// Year 3: +100 reputation (Neutral → Liked transition)
//
// Takes ~2 years to recover from deeply negative reputation
// Good reputation fades slower (people remember good deeds longer than absence)
```

---

## Cross-Game Applications

### Godgame: Village Reputation

```csharp
// Village with bad reputation (raided neighbors)
AggregateReputation villageRep = new()
{
    Type = AggregateReputationType.Settlement,
    GlobalReputation = -400,     // Disliked tier
    Domains = new[]
    {
        new ReputationDomain
        {
            Type = DomainType.Diplomacy,
            Score = -600,        // Loathed (aggressive, broke treaties)
        },
        new ReputationDomain
        {
            Type = DomainType.Trading,
            Score = -200,        // Disliked (occasionally honest)
        }
    }
};

// Effects:
// - Other villages refuse diplomatic contact
// - Trade only with desperate or criminal settlements
// - Attacked on sight by some factions
// - Cannot recruit allies for defense
// - Must raid to survive (reinforces bad reputation cycle)

// Path to redemption:
// - Make reparations to raided villages
// - Fulfill trade contracts honestly
// - Defend attacked settlements (even former enemies)
// - Slowly rebuild reputation over years
```

### Godgame: Individual Hero Reputation

```csharp
// Legendary hero with high reputation
EntityReputation heroRep = new()
{
    GlobalReputation = +850,     // Revered tier
    IsRenowned = true,
    WitnessCount = 1500,         // Known by many
    Domains = new[]
    {
        new ReputationDomain
        {
            Type = DomainType.Combat,
            Score = +900,        // Legendary warrior
            PositiveActions = 50 // 50 heroic deeds
        },
        new ReputationDomain
        {
            Type = DomainType.Leadership,
            Score = +800,        // Excellent leader
        }
    }
};

// Effects:
// - Villagers volunteer to follow hero
// - Free lodging and meals in settlements
// - Receives quests from leaders
// - Can recruit mercenaries easily
// - Diplomatic immunity (minor crimes forgiven)
// - Songs and stories spread reputation further
```

### Space4X: Corporation Reputation

```csharp
// Trading corporation with mixed reputation
AggregateReputation corpRep = new()
{
    Type = AggregateReputationType.Corporation,
    GlobalReputation = +300,     // Liked tier
    Domains = new[]
    {
        new ReputationDomain
        {
            Type = DomainType.Trading,
            Score = +600,        // Respected (reliable deliveries)
        },
        new ReputationDomain
        {
            Type = DomainType.Diplomacy,
            Score = -100,        // Neutral-Disliked (bribed officials)
        }
    }
};

// Effects:
// - Preferred trade partner for goods
// - Diplomatic relations strained (corruption allegations)
// - Some colonies ban them, others welcome
// - Competitors spread negative rumors
// - Must balance profit vs reputation
```

### Space4X: First Contact Reputation

```csharp
// Human faction meeting alien species for first time
// No prior reputation (0 for all domains)

// Scenario 1: Peaceful first contact
// - Share technology → +50 Diplomacy reputation
// - Fair trade → +30 Trading reputation
// - Respect borders → +20 General reputation
// Result: +100 total, foundation for alliance

// Scenario 2: Aggressive first contact
// - Demand surrender → -80 Diplomacy reputation
// - Steal resources → -100 Trading reputation
// - Attack settlements → -150 Combat reputation
// Result: -330 total, permanent enemy

// Reputation from first contact shapes entire relationship
// Hard to recover from bad first impression
```

---

## Performance Considerations

### Reputation Caching

```csharp
// Cache reputation lookups (expensive to recalculate)
public struct ReputationCache : IComponentData
{
    public FixedList128Bytes<CachedReputation> Cache;
    public double LastCacheClear;
}

public struct CachedReputation
{
    public Entity Subject;
    public Entity Observer;
    public DomainType Domain;
    public int CachedScore;
    public double CacheTime;
}

// Update cache when reputation changes
// Clear cache every 60 seconds or on significant changes
// Reduces lookups from O(n²) to O(1) for frequent checks
```

**Profiling Targets**:
```
Reputation Update:     <0.1ms per event
Gossip Spread:         <0.5ms per event (10 spreads)
Trust History Update:  <0.05ms per interaction
Trade Check:           <0.1ms per opportunity
Decay Update:          <0.05ms per entity per day
────────────────────────────────────────────────
Total (100 events):    <8.0ms per frame
```

---

## Integration with Existing Systems

### Communication System
- Lies detected → Reputation loss (Diplomacy domain)
- Honest communication → Slow reputation gain
- Teaching languages/spells → Reputation gain (Teaching domain)

### Reactions System
- Initial reactions modified by reputation
- High reputation → Positive initial reaction (+50 bonus)
- Low reputation → Negative initial reaction (-50 penalty)

### Tooltip System
```
┌─────────────────────────────────┐
│ Merchant: Silas                 │
│ Reputation:                     │
│  Trading: Respected (+650)      │
│  Diplomacy: Neutral (+50)       │
│                                 │
│ Your view: Trustworthy trader   │
│ Known by: 340 entities          │
│                                 │
│ Recent: +30 (fulfilled contract)│
│         -15 (late delivery)     │
└─────────────────────────────────┘
```

### Procedural Generation
- Gods have reputation requirements for miracles
- Guild membership tied to reputation thresholds
- Factions generate with starting reputations toward each other

---

## Summary

The Reputation System provides:

1. **Multi-Domain Tracking**: Separate reputations for trading, combat, diplomacy, magic, etc.
2. **Action-Based**: Reputation earned through behavior, not granted arbitrarily
3. **Organic Spread**: Witnesses and gossip create reputation networks
4. **Individual & Aggregate**: Both entities and groups have reputations
5. **Meaningful Consequences**: Reputation unlocks/blocks opportunities (guild access, trade, teaching)
6. **Guild Signs**: Secret membership proof, teachable to trusted allies in emergencies
7. **Magical Verification**: Doors and wards check signs and membership
8. **Trust Learning**: Entities remember who lied, who was honest, adjust trust accordingly
9. **Slow Change**: Trust hard to build, easy to lose, slow to rebuild
10. **Cross-Game**: Village reputations (Godgame), corporate reputations (Space4X), individual heroes
11. **Integration**: Works with communication (lies damage reputation), reactions (reputation affects initial relations), tooltips (show reputation details)

**Key Innovation**: Reputation is not a single score but a web of perceptions - each entity has different views of others based on direct experience, gossip, and group affiliations. A merchant might be revered by traders, loathed by one specific customer they swindled, and unknown to a distant village. This creates emergent social dynamics where trust, deception, and redemption drive narratives organically.
