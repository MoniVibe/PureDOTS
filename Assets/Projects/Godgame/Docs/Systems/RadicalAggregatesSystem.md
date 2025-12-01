# Radical Aggregates & Radicalization System

## Concept

**Radicals** are disgruntled, often irrational individuals who form **cells** and **movements** to undermine village authority and destabilize the social order. Unlike guilds (which are organized and productive), radicals are chaotic, destructive, and driven by grievances rather than craft or commerce.

They:
- **Strike and riot** wherever opportunity presents
- **Spread propaganda** to recruit the discontented
- **Sabotage infrastructure** (farms, markets, defenses)
- **Challenge authority** through violence or disruption
- **Operate in shadows** (cells, not formal organizations)

Villages respond differently:
- **Exile** → Remove troublemakers
- **Execution** → Harsh suppression
- **Tolerance** → Allow dissent, risk growth
- **Reform** → Address grievances, reduce radicalization
- **Infiltration** → Spy on cells, preemptive action

## Core Mechanics

### 1. Radicalization (How Villagers Become Radicals)

Villagers radicalize when **grievances** accumulate beyond a threshold:

```csharp
/// <summary>
/// Tracks a villager's grievances and radicalization progress.
/// When GrievanceLevel exceeds threshold, villager may radicalize.
/// </summary>
public struct RadicalizationState : IComponentData
{
    /// <summary>
    /// Accumulated grievance points (0-100).
    /// Higher = more likely to radicalize.
    /// </summary>
    public float GrievanceLevel;

    /// <summary>
    /// Radicalization threshold (personality-dependent).
    /// Chaotic/rebellious villagers have lower thresholds.
    /// </summary>
    public float RadicalizationThreshold;

    /// <summary>
    /// Current radicalization stage.
    /// </summary>
    public RadicalizationStage Stage;

    /// <summary>
    /// Primary grievance driving radicalization.
    /// </summary>
    public GrievanceType PrimaryGrievance;

    /// <summary>
    /// Tick when radicalization began.
    /// </summary>
    public uint RadicalizedTick;

    /// <summary>
    /// Whether villager is currently part of a radical cell.
    /// </summary>
    public bool IsRadical;

    /// <summary>
    /// Whether villager is a cell leader.
    /// </summary>
    public bool IsCellLeader;

    /// <summary>
    /// Commitment to radical cause (0-100).
    /// Higher = harder to de-radicalize.
    /// </summary>
    public byte Commitment;
}

public enum RadicalizationStage : byte
{
    Stable,         // No grievances
    Discontented,   // Minor grievances, complains
    Agitated,       // Significant grievances, openly critical
    Radicalized,    // Joined a cell, willing to act
    Extremist       // Fully committed, willing to die for cause
}

/// <summary>
/// Types of grievances that drive radicalization.
/// </summary>
[Flags]
public enum GrievanceType : uint
{
    None = 0,

    // Economic grievances
    Poverty = 1 << 0,              // Low wealth
    Unemployment = 1 << 1,         // No job
    Exploitation = 1 << 2,         // Unfair wages
    Taxation = 1 << 3,             // High taxes
    Inequality = 1 << 4,           // Wealth gap

    // Social grievances
    Discrimination = 1 << 5,       // Unfair treatment
    Marginalization = 1 << 6,      // Excluded from society
    LackOfVoice = 1 << 7,          // No political representation
    CulturalSuppression = 1 << 8,  // Culture/religion banned

    // Authority grievances
    Authoritarianism = 1 << 9,     // Oppressive laws
    Corruption = 1 << 10,          // Leaders enriching themselves
    Injustice = 1 << 11,           // Unfair punishments
    LackOfFreedom = 1 << 12,       // Restricted movement/speech

    // Material grievances
    Hunger = 1 << 13,              // Food scarcity
    Homelessness = 1 << 14,        // No shelter
    Illness = 1 << 15,             // No healthcare
    Insecurity = 1 << 16,          // Constant danger

    // Ideological grievances
    ReligiousPersecution = 1 << 17, // Faith suppressed
    IdeologicalDifference = 1 << 18, // Disagrees with regime
    Nationalism = 1 << 19,          // Opposes foreign rule
    ClassConflict = 1 << 20,        // Worker vs elite

    // Psychological grievances
    Humiliation = 1 << 21,         // Public shaming
    Betrayal = 1 << 22,            // Trust violated
    Revenge = 1 << 23,             // Seeks vengeance
    Despair = 1 << 24,             // Sees no future
}
```

### Grievance Accumulation

```csharp
/// <summary>
/// Events that increase grievances.
/// </summary>
public struct GrievanceEvent : IBufferElementData
{
    public GrievanceType Type;
    public float Severity;           // 0-10
    public uint OccurredTick;
    public Entity CausedBy;          // Leader, noble, etc.
    public FixedString64Bytes Description;
}
```

**Example Grievance Sources**:
- **Poverty**: Wealth below 25% of village average → +0.5 grievance/day
- **Taxation**: Tax rate > 40% → +1.0 grievance/day
- **Hunger**: Food below minimum → +2.0 grievance/day
- **Injustice**: Unfair punishment → +10.0 grievance (instant spike)
- **Humiliation**: Public shaming → +15.0 grievance
- **Corruption**: Leader embezzling → +5.0 grievance to all who know

### 2. Radical Cells (Organization)

Unlike guilds, radicals organize in **secretive cells**:

```csharp
/// <summary>
/// A radical cell - small group of radicalized individuals.
/// Cells operate independently and semi-secretly.
/// </summary>
public struct RadicalCell : IComponentData
{
    public enum CellType : byte
    {
        Agitators,      // Spread propaganda, recruit
        Saboteurs,      // Destroy infrastructure
        Rioters,        // Violence against authority
        Infiltrators,   // Spy on leadership
        Revolutionaries, // Seek to overthrow
        Separatists,    // Want to leave village
        Cultists,       // Religious/ideological extremists
        Anarchists      // Reject all authority
    }

    public CellType Type;
    public FixedString64Bytes CellName; // "The Red Fist", "Shadow of Liberty"

    /// <summary>
    /// Cell ideology (what they oppose).
    /// </summary>
    public RadicalIdeology Ideology;

    /// <summary>
    /// Home village (what they're undermining).
    /// </summary>
    public Entity TargetVillage;

    /// <summary>
    /// Cell secrecy level (0-100).
    /// Higher = harder for authorities to detect.
    /// </summary>
    public byte SecrecyLevel;

    /// <summary>
    /// Cell aggression level (0-100).
    /// Higher = more violent actions.
    /// </summary>
    public byte AggressionLevel;

    /// <summary>
    /// Number of members.
    /// </summary>
    public ushort MemberCount;

    /// <summary>
    /// When cell was formed.
    /// </summary>
    public uint FoundedTick;

    /// <summary>
    /// Whether cell has been discovered by authorities.
    /// </summary>
    public bool IsDiscovered;

    /// <summary>
    /// Whether authorities are actively suppressing this cell.
    /// </summary>
    public bool UnderSuppression;
}

/// <summary>
/// Radical ideologies (what cells believe in).
/// </summary>
[Flags]
public enum RadicalIdeology : uint
{
    None = 0,

    // Economic ideologies
    AntiCapitalist = 1 << 0,       // Oppose merchant class
    AntiTax = 1 << 1,              // Oppose taxation
    Egalitarian = 1 << 2,          // Total equality
    WorkersRights = 1 << 3,        // Labor movement

    // Political ideologies
    Anarchist = 1 << 4,            // No government
    Democratic = 1 << 5,           // Demand elections
    Separatist = 1 << 6,           // Leave village/nation
    Revolutionary = 1 << 7,        // Overthrow regime

    // Social ideologies
    Feminist = 1 << 8,             // Gender equality
    RacialJustice = 1 << 9,        // End discrimination
    Religious = 1 << 10,           // Faith-based movement
    Secular = 1 << 11,             // Oppose religious rule

    // Chaotic ideologies
    Nihilist = 1 << 12,            // Destroy everything
    Apocalyptic = 1 << 13,         // End times cult
    Reactionary = 1 << 14,         // Return to old ways
    Supremacist = 1 << 15,         // Racial/cultural supremacy

    // Authority-focused
    AntiMonarchy = 1 << 16,        // Oppose kings/nobles
    AntiMilitary = 1 << 17,        // Oppose armed forces
    AntiMagic = 1 << 18,           // Fear/hate magic users
    AntiReligion = 1 << 19,        // Oppose organized faith
}

/// <summary>
/// Cell membership roster.
/// </summary>
public struct RadicalCellMember : IBufferElementData
{
    public Entity VillagerEntity;
    public uint JoinedTick;
    public byte CommitmentLevel;    // 0-100
    public bool IsLeader;
    public bool IsInfiltrator;      // Double agent for authorities
    public bool WillingToDie;       // Suicide missions
}
```

### 3. Radical Activities (Disruption)

Cells perform disruptive actions:

```csharp
/// <summary>
/// Active radical operation (riot, sabotage, etc.)
/// </summary>
public struct RadicalOperation : IComponentData
{
    public enum OperationType : byte
    {
        Propaganda,         // Recruit, spread grievances
        Demonstration,      // Peaceful protest
        Riot,              // Violent uprising
        Sabotage,          // Destroy infrastructure
        Assassination,     // Kill authority figures
        Theft,             // Steal resources/weapons
        Arson,             // Burn buildings
        Kidnapping,        // Take hostages
        Strike,            // Work stoppage
        Infiltration       // Plant spies
    }

    public OperationType Type;
    public Entity CellEntity;
    public Entity TargetEntity;      // Building, leader, etc.
    public float3 TargetPosition;

    public uint OperationStartTick;
    public uint PlannedDuration;

    /// <summary>
    /// Number of cell members participating.
    /// </summary>
    public ushort ParticipantCount;

    /// <summary>
    /// Expected impact (0-100).
    /// </summary>
    public byte ExpectedImpact;

    /// <summary>
    /// Chance of being caught (0-100).
    /// </summary>
    public byte DetectionRisk;

    /// <summary>
    /// Whether operation succeeded.
    /// </summary>
    public bool Succeeded;

    /// <summary>
    /// Whether authorities detected operation.
    /// </summary>
    public bool Detected;
}

/// <summary>
/// Impact of radical activities on village.
/// </summary>
public struct RadicalImpact : IComponentData
{
    /// <summary>
    /// Overall stability loss (0-100).
    /// High = village near collapse.
    /// </summary>
    public float StabilityLoss;

    /// <summary>
    /// Economic damage (gold).
    /// </summary>
    public float EconomicDamage;

    /// <summary>
    /// Infrastructure damage (buildings destroyed/damaged).
    /// </summary>
    public ushort InfrastructureDamage;

    /// <summary>
    /// Authority figures killed.
    /// </summary>
    public ushort AuthorityDeaths;

    /// <summary>
    /// Civilian casualties from riots/fighting.
    /// </summary>
    public ushort CivilianCasualties;

    /// <summary>
    /// Villagers recruited to radical cells.
    /// </summary>
    public ushort NewRadicals;

    /// <summary>
    /// Public support for radicals (0-100).
    /// Higher = more sympathizers.
    /// </summary>
    public byte PublicSupport;
}
```

### 4. Village Responses

Villages can respond to radicals in multiple ways:

```csharp
/// <summary>
/// Village's policy toward radicals.
/// </summary>
public struct RadicalResponsePolicy : IComponentData
{
    public enum PolicyType : byte
    {
        Tolerance,      // Allow dissent, minimal action
        Surveillance,   // Monitor cells, gather intel
        Infiltration,   // Plant spies in cells
        Suppression,    // Arrest/exile/execute
        Reform,         // Address grievances
        Negotiation,    // Talk to radical leaders
        Crackdown       // Total martial law
    }

    public PolicyType CurrentPolicy;

    /// <summary>
    /// Threshold of radical activity before policy escalates.
    /// </summary>
    public byte EscalationThreshold;

    /// <summary>
    /// Whether village uses exile for radicals.
    /// </summary>
    public bool AllowsExile;

    /// <summary>
    /// Whether village uses execution for radicals.
    /// </summary>
    public bool AllowsExecution;

    /// <summary>
    /// Whether village tolerates peaceful protest.
    /// </summary>
    public bool ToleratesProtest;

    /// <summary>
    /// Whether village attempts reform (address grievances).
    /// </summary>
    public bool AttemptsReform;

    /// <summary>
    /// Resources allocated to counter-radical operations.
    /// </summary>
    public float CounterRadicalBudget;
}

/// <summary>
/// Punishment for caught radicals.
/// </summary>
public struct RadicalPunishment : IComponentData
{
    public enum PunishmentType : byte
    {
        Warning,        // First offense
        Fine,           // Economic penalty
        Imprisonment,   // Jail time
        Exile,          // Banish from village
        Execution,      // Death
        Torture,        // Interrogation + pain
        PublicShaming,  // Humiliation (may increase grievances!)
        ForcedLabor     // Work camp
    }

    public PunishmentType Type;
    public Entity TargetVillager;
    public uint PunishmentStartTick;
    public uint Duration;            // For imprisonment/labor

    /// <summary>
    /// Whether punishment is public (visible to others).
    /// Public punishment deters some, radicalizes others.
    /// </summary>
    public bool IsPublic;

    /// <summary>
    /// Deterrent effect (reduces radicalization in others).
    /// </summary>
    public float DeterrentEffect;

    /// <summary>
    /// Martyrdom effect (increases radicalization in sympathizers).
    /// </summary>
    public float MartyrdomEffect;
}
```

### Response Outcomes

Different policies have trade-offs:

| Policy | Pros | Cons |
|--------|------|------|
| **Tolerance** | Preserves freedom, low cost | Radicals grow unchecked |
| **Surveillance** | Early detection | Resource intensive, privacy loss |
| **Infiltration** | Preemptive strikes | Risky, can backfire |
| **Suppression** | Removes threats | Creates martyrs, increases grievances |
| **Reform** | Addresses root causes | Expensive, slow |
| **Negotiation** | Peaceful resolution | Legitimizes radicals, may embolden |
| **Crackdown** | Total control | Extreme grievances, oppression |

## Radicalization Scenarios

### Example 1: Economic Radicals

```
Village: Ironforge
Situation: High taxes (50%) to fund war
Grievance Accumulation:
  - Taxation: +1.0/day
  - Poverty: +0.5/day (wealth depleted)
  - Exploitation: +0.3/day (nobles living lavishly)

Villager: Gunther (Blacksmith)
  - GrievanceLevel: 75/100
  - PrimaryGrievance: Taxation + Inequality
  - RadicalizationThreshold: 60 (rebellious personality)
  - Stage: Radicalized

Result:
  → Gunther joins cell "The Iron Fist"
  → Cell ideology: AntiTax | WorkersRights
  → Cell type: Rioters
  → Plans operation: Strike (refuse to make weapons)

Village Response: Suppression
  → Arrests Gunther
  → Punishment: Exile
  → Side effects:
      - Other workers gain +5 grievance (sympathy)
      - Cell gains martyr status
      - Recruitment increases by 20%
```

### Example 2: Religious Radicals

```
Village: Silverpeak
Situation: New law bans old gods worship
Grievance Accumulation:
  - ReligiousPersecution: +2.0/day
  - CulturalSuppression: +1.0/day
  - Injustice: +1.5/day (arrests for worship)

Villager: Elara (Priestess)
  - GrievanceLevel: 95/100
  - PrimaryGrievance: ReligiousPersecution
  - RadicalizationThreshold: 70
  - Stage: Extremist

Result:
  → Elara founds cell "Faithful Retribution"
  → Cell ideology: Religious | Separatist
  → Cell type: Cultists
  → Plans operation: Assassination (kill secular leader)

Village Response: Negotiation
  → Offers compromise: private worship allowed
  → Elara rejects (too committed)
  → Escalates to Crackdown
  → Result: Armed conflict, village destabilized
```

### Example 3: Anarchist Radicals

```
Village: Westmark
Situation: Corrupt noble embezzles food during famine
Grievance Accumulation:
  - Hunger: +2.5/day
  - Corruption: +5.0 (one-time spike)
  - Injustice: +3.0/day (no trial for noble)
  - Despair: +1.0/day

Villager: Marcus (Laborer)
  - GrievanceLevel: 100/100 (max)
  - PrimaryGrievance: Corruption + Hunger
  - RadicalizationThreshold: 50 (low - desperate)
  - Stage: Extremist
  - Commitment: 100 (willing to die)

Result:
  → Marcus joins cell "Ashes of Order"
  → Cell ideology: Anarchist | Revolutionary
  → Cell type: Saboteurs + Rioters
  → Plans operation: Arson (burn noble's mansion)

Village Response: Reform
  → Arrests corrupt noble
  → Redistributes embezzled food
  → Public trial (transparency)
  → Result:
      - Marcus's grievance drops to 40
      - Cell dissolves (cause addressed)
      - Other villagers see justice works
```

## System Architecture

### Radicalization System

```csharp
[UpdateInGroup(typeof(GameplaySystemGroup))]
public partial class RadicalizationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        // Accumulate grievances
        Entities
            .ForEach((ref RadicalizationState radicalization,
                     in DynamicBuffer<GrievanceEvent> events,
                     in VillagerPersonality personality) =>
            {
                // Calculate grievance accumulation
                float dailyGrievance = CalculateDailyGrievance(events);
                radicalization.GrievanceLevel += dailyGrievance * deltaTime;

                // Clamp to 0-100
                radicalization.GrievanceLevel = math.clamp(
                    radicalization.GrievanceLevel, 0, 100);

                // Update stage
                if (radicalization.GrievanceLevel >= radicalization.RadicalizationThreshold)
                {
                    radicalization.Stage = RadicalizationStage.Radicalized;
                }

                // Decay grievances slowly over time (forgiveness)
                if (!radicalization.IsRadical)
                {
                    radicalization.GrievanceLevel -= 0.1f * deltaTime;
                }
            }).ScheduleParallel();

        // Check for radicalization events
        CheckForRadicalization();
    }

    private void CheckForRadicalization()
    {
        // Find villagers ready to radicalize
        Entities
            .WithNone<RadicalCellMember>()
            .ForEach((Entity entity,
                     ref RadicalizationState radicalization) =>
            {
                if (radicalization.GrievanceLevel >=
                    radicalization.RadicalizationThreshold)
                {
                    // Try to join or form a cell
                    AttemptCellJoin(entity, radicalization);
                }
            }).WithStructuralChanges().Run();
    }
}
```

### Cell Operations System

```csharp
[UpdateInGroup(typeof(GameplaySystemGroup))]
public partial class RadicalOperationsSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var currentTick = GetSingleton<GameTickSingleton>().Tick;

        // Process active operations
        Entities
            .ForEach((Entity opEntity,
                     ref RadicalOperation operation,
                     in RadicalCell cell) =>
            {
                var elapsed = currentTick - operation.OperationStartTick;

                if (elapsed >= operation.PlannedDuration)
                {
                    // Operation complete - resolve
                    ResolveOperation(opEntity, operation, cell);
                }
                else
                {
                    // Check for detection
                    if (Random.value * 100 < operation.DetectionRisk)
                    {
                        operation.Detected = true;
                        AlertAuthorities(operation);
                    }
                }
            }).WithoutBurst().Run();

        // Plan new operations
        PlanCellOperations(currentTick);
    }

    private void ResolveOperation(Entity opEntity,
                                 RadicalOperation operation,
                                 RadicalCell cell)
    {
        switch (operation.Type)
        {
            case RadicalOperation.OperationType.Riot:
                // Damage buildings, kill guards, scare civilians
                CauseRiotDamage(operation);
                break;

            case RadicalOperation.OperationType.Sabotage:
                // Destroy target infrastructure
                SabotageTarget(operation.TargetEntity);
                break;

            case RadicalOperation.OperationType.Assassination:
                // Kill authority figure
                if (!operation.Detected)
                {
                    AttemptAssassination(operation.TargetEntity);
                }
                break;

            // etc.
        }

        EntityManager.DestroyEntity(opEntity);
    }
}
```

## Integration with Existing Systems

### Mood System

```csharp
// Radicals have permanent mood debuffs
if (radicalization.Stage >= RadicalizationStage.Radicalized)
{
    mood.CurrentMood = VillagerMood.Angry;
    mood.BaseHappiness -= 30; // Chronic unhappiness
}
```

### Alignment System

```csharp
// Radical actions shift alignment toward chaos
if (participatedInRiot)
{
    alignment.OrderAxis -= 10; // More chaotic
}
```

### Guild Integration

```csharp
// Guilds can counter radicalization
if (guildMembership.Rank >= 1) // Officer+
{
    radicalization.GrievanceLevel -= 5; // Sense of belonging
}

// Or guilds can become radicalized
if (guild.AverageGrievance > 60)
{
    // Entire guild becomes revolutionary
    ConvertGuildToRadicalCell(guildEntity);
}
```

## Files to Create

1. **RadicalAggregatesSystem.md** (this file) - Full design
2. **RadicalComponents.cs** - Component definitions
3. **RadicalExamples.md** - Scenario examples
4. **RadicalResponseStrategies.md** - How villages counter radicals
5. **RadicalIntegration.md** - Integration with existing systems

This captures the chaotic, disruptive nature of radicals while providing gameplay depth around political instability.
