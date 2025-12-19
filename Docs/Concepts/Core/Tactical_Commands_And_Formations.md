# Tactical Commands and Formations System

## Overview

Tactical command system where leaders issue orders (follow, attack, defend, formations) through communication channels. Formations require training - entities must learn formation roles and maintain cohesion with fellow members. A phalanx is only strong when all warriors know their positions and have high cohesion with their squad mates.

**Key Principles**:
- **Commands through communication**: Tactical orders use dialogue/language system
- **Formation training**: Entities must learn formations to receive full bonuses
- **Cohesion mechanics**: Formation strength scales with member cohesion
- **Relation-based**: High relations = better cohesion, strangers struggle to coordinate
- **Learning curve**: Formations improve with practice and drilling
- **Breaking conditions**: Formations collapse under disruption, morale loss, or casualties
- **Cross-game**: Infantry formations (Godgame), ship formations (Space4X)
- **Deterministic**: Same cohesion + same disruption = same formation integrity

---

## Tactical Commands

### Command Types

```csharp
public struct TacticalCommand : IComponentData
{
    public Entity Commander;
    public Entity Target;                   // Entity or position target
    public TacticalCommandType CommandType;
    public float3 TargetPosition;           // For positional commands
    public float Priority;                  // 0.0 to 1.0 (urgency)
    public uint IssuedTick;
    public uint ExpirationTick;             // Commands can expire
    public bool RequiresAcknowledgment;
    public bool WasAcknowledged;
}

public enum TacticalCommandType : byte
{
    // Movement commands
    Follow = 0,             // Follow designated entity
    MoveTo = 1,             // Move to position
    Stay = 2,               // Hold current position
    Retreat = 3,            // Fall back to rally point
    Patrol = 4,             // Patrol between waypoints
    Intercept = 5,          // Move to intercept enemy

    // Combat commands
    Attack = 10,            // Engage target
    Defend = 11,            // Protect target/position
    HoldFire = 12,          // Do not engage
    FocusFire = 13,         // Concentrate fire on target
    SuppressFire = 14,      // Suppressive fire, keep enemies pinned

    // Formation commands
    FormUp = 20,            // Enter formation
    HoldFormation = 21,     // Maintain current formation
    BreakFormation = 22,    // Abandon formation, free movement
    ChangeFormation = 23,   // Switch to different formation

    // Tactical commands
    TakeCover = 30,         // Seek cover
    Flank = 31,             // Flank enemy position
    Surround = 32,          // Encircle target
    Regroup = 33,           // Rally at position
    ChargeAttack = 34,      // Aggressive forward assault
    FeignRetreat = 35,      // Fake retreat (tactical deception)

    // Support commands
    ProvideCover = 40,      // Covering fire for allies
    ReviveAlly = 41,        // Rescue/heal wounded
    ResupplyAlly = 42,      // Share resources/ammo
    Scout = 43              // Reconnaissance mission
}

public struct CommandCompliance : IComponentData
{
    public Entity ReceivedCommand;
    public float ComplianceRating;          // 0.0 to 1.0 (will they obey?)
    public CommandComplianceReason Reason;
    public bool IsExecuting;
    public float ExecutionQuality;          // How well they follow orders
}

public enum CommandComplianceReason : byte
{
    Obeying = 0,            // Following orders
    Questioning = 1,        // Doubtful but complying
    Refusing = 2,           // Will not comply
    Unable = 3,             // Cannot comply (lacks capability)
    Delayed = 4,            // Will comply but not immediately
    Misunderstood = 5       // Communication failure
}
```

### Command Issuance System

```csharp
[BurstCompile]
public partial struct TacticalCommandSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (command, communication) in SystemAPI.Query<
            RefRO<TacticalCommand>,
            RefRO<CommunicationAttempt>>())
        {
            // Get all entities that should receive this command
            var recipients = GetCommandRecipients(command.ValueRO);

            foreach (var recipient in recipients)
            {
                // Check if recipient can understand the command
                float clarity = CalculateCommunicationClarity(
                    command.ValueRO.Commander,
                    recipient,
                    communication.ValueRO
                );

                if (clarity < 0.3f)
                {
                    // Command misunderstood
                    CreateCommandCompliance(recipient, command.ValueRO.Target,
                        CommandComplianceReason.Misunderstood, 0f);
                    continue;
                }

                // Calculate compliance based on authority, relations, personality
                float compliance = CalculateCommandCompliance(
                    command.ValueRO.Commander,
                    recipient,
                    command.ValueRO.CommandType,
                    command.ValueRO.Priority
                );

                // Apply compliance
                if (compliance > 0.7f)
                {
                    CreateCommandCompliance(recipient, command.ValueRO.Target,
                        CommandComplianceReason.Obeying, compliance);
                }
                else if (compliance > 0.4f)
                {
                    CreateCommandCompliance(recipient, command.ValueRO.Target,
                        CommandComplianceReason.Questioning, compliance);
                }
                else
                {
                    CreateCommandCompliance(recipient, command.ValueRO.Target,
                        CommandComplianceReason.Refusing, compliance);
                }
            }
        }
    }

    private float CalculateCommandCompliance(
        Entity commander,
        Entity recipient,
        TacticalCommandType commandType,
        float priority)
    {
        // Authority (rank/leadership)
        float authority = GetAuthorityLevel(commander, recipient); // 0.0 to 1.0

        // Relations (friendship/respect)
        float relations = GetRelationValue(recipient, commander); // -1.0 to 1.0
        float relationBonus = math.max(0f, relations * 0.3f); // Max +30% from good relations

        // Personality (some entities more obedient)
        float personalityFactor = GetObedienceTrait(recipient); // 0.0 to 1.0

        // Priority modifier (urgent commands more likely to be obeyed)
        float priorityBonus = priority * 0.2f; // Max +20% from urgency

        // Combined compliance
        float baseCompliance = (authority * 0.5f) + (personalityFactor * 0.3f);
        return math.clamp(baseCompliance + relationBonus + priorityBonus, 0f, 1f);
    }
}
```

---

## Formation System

### Formation Types

```csharp
public struct FormationDefinition : IComponentData
{
    public FixedString64Bytes FormationName;
    public FormationType Type;
    public uint RequiredMembers;            // Minimum members
    public uint OptimalMembers;             // Best size
    public uint MaxMembers;                 // Maximum size
    public float MinimumTrainingLevel;      // Required to attempt formation
    public float MinimumCohesion;           // Required to maintain formation
    public float LearningDifficulty;        // How hard to learn (0.0 to 1.0)
}

public enum FormationType : byte
{
    // Infantry formations
    Phalanx = 0,            // Tight shield wall, high defense, low mobility
    ShieldWall = 1,         // Defensive line, medium defense
    Wedge = 2,              // Offensive penetration, breaks enemy lines
    Line = 3,               // Standard battle line
    Column = 4,             // March formation, fast movement
    Square = 5,             // Anti-cavalry, defends all sides
    Skirmish = 6,           // Loose formation, mobility focused
    Testudo = 7,            // Roman turtle, extreme defense, very slow

    // Cavalry formations
    Charge = 10,            // Cavalry charge formation
    HammerAnvil = 11,       // Cavalry flanking maneuver
    Cantabrian = 12,        // Hit-and-run cavalry circle

    // Naval formations (Space4X/Godgame ships)
    LineAhead = 20,         // Ships in single file
    LineAbreast = 21,       // Ships side by side
    VShape = 22,            // V formation for concentrated fire
    CircleDefense = 23,     // Defensive circle protecting center

    // Ranged formations
    CheckerBoard = 30,      // Alternating front/back for volley fire
    ThreeRank = 31,         // Rotating ranks for continuous fire

    // Special formations
    Ambush = 40,            // Concealed positions for surprise
    Encirclement = 41,      // Surround enemy
    FalseRetreat = 42       // Feigned retreat tactical formation
}

public struct FormationBonuses
{
    // Combat bonuses
    public float AttackBonus;               // -50% to +100%
    public float DefenseBonus;              // -50% to +200%
    public float AccuracyBonus;             // -20% to +50%
    public float CohesionDamageResist;      // Damage reduction from formation integrity

    // Movement bonuses/penalties
    public float MovementSpeedModifier;     // 0.2× to 1.5×
    public float RotationSpeedModifier;     // How fast formation can turn
    public float TerrainPenaltyReduction;   // Reduce rough terrain penalty

    // Morale bonuses
    public float MoraleBonus;               // +0% to +50%
    public float FearResistance;            // +0% to +80%

    // Special properties
    public bool ImmuneToRout;               // Cannot flee while in formation
    public bool ImmuneToFlanking;           // Protected from rear/side attacks
    public bool ChargeResistance;           // Resist cavalry charges
    public float AreaDenial;                // Zone control bonus
}
```

### Formation Training System

```csharp
public struct FormationTraining : IComponentData
{
    public FormationType Formation;
    public float TrainingLevel;             // 0.0 to 1.0 (novice to master)
    public float PracticeTime;              // Hours spent drilling
    public uint DrillingCount;              // Number of practice sessions
    public uint CombatUsageCount;           // Times used in real combat
    public float LastPracticeQuality;       // How well last drill went
}

[InternalBufferCapacity(8)]
public struct KnownFormation : IBufferElementData
{
    public FormationType Formation;
    public float Proficiency;               // 0.0 to 1.0
    public uint TimesUsed;
    public uint SuccessfulHolds;            // Times maintained under pressure
    public uint BreakCount;                 // Times formation broke
}

[BurstCompile]
public partial struct FormationTrainingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (training, knownFormations) in SystemAPI.Query<
            RefRW<FormationTraining>,
            DynamicBuffer<KnownFormation>>())
        {
            if (training.ValueRO.PracticeTime > 0f)
            {
                // Drilling in progress
                training.ValueRW.PracticeTime += deltaTime;

                // Calculate training gain
                float learningRate = GetLearningRate(training.ValueRO.Formation);
                float practiceQuality = CalculatePracticeQuality(training.ValueRO);

                // Gain proficiency over time
                float proficiencyGain = learningRate * practiceQuality * deltaTime;

                // Update known formation proficiency
                for (int i = 0; i < knownFormations.Length; i++)
                {
                    if (knownFormations[i].Formation == training.ValueRO.Formation)
                    {
                        var known = knownFormations[i];
                        known.Proficiency = math.min(1f, known.Proficiency + proficiencyGain);
                        knownFormations[i] = known;
                        break;
                    }
                }

                training.ValueRW.TrainingLevel = GetHighestProficiency(knownFormations, training.ValueRO.Formation);
            }
        }
    }

    private float CalculatePracticeQuality(in FormationTraining training)
    {
        // Quality affected by:
        // - Drill instructor skill
        // - Number of participants (harder with more)
        // - Environmental conditions
        // - Fatigue level
        // Returns 0.0 to 1.0
        return 0.7f; // Simplified
    }
}
```

### Formation Cohesion System

**Cohesion is the formation's integrity** - how well members maintain positions and coordinate:

```csharp
public struct FormationCohesion : IComponentData
{
    public Entity FormationLeader;
    public FormationType Type;
    public float CurrentCohesion;           // 0.0 to 1.0 (current integrity)
    public float BaseCohesion;              // Starting cohesion
    public float CohesionDecayRate;         // Loss per second under stress
    public float CohesionRegenRate;         // Recovery when not fighting

    // Cohesion modifiers
    public float MemberProficiencyAverage;  // Avg training level of all members
    public float RelationCohesionBonus;     // Bonus from good relations
    public float ExperienceBonus;           // Bonus from combat together
    public float CommunicationClarity;      // Bonus from clear commands

    // Breaking conditions
    public float CohesionBreakThreshold;    // Cohesion level where formation breaks
    public bool IsBroken;
    public uint MemberCount;
    public uint TargetMemberCount;
}

[InternalBufferCapacity(32)]
public struct FormationMember : IBufferElementData
{
    public Entity MemberEntity;
    public FormationRole Role;
    public float Proficiency;               // How well they know this formation
    public float3 AssignedPosition;         // Relative position in formation
    public float PositionDeviation;         // How far from assigned position
    public float CohesionContribution;      // How much they add to cohesion
}

public enum FormationRole : byte
{
    Leader = 0,             // Formation commander
    FrontLine = 1,          // Front rank
    SecondRank = 2,         // Second rank
    ThirdRank = 3,          // Third rank
    Flanker = 4,            // Side protection
    Rear = 5,               // Rear guard
    Support = 6,            // Reserves/support
    Standard = 7            // Standard bearer (morale)
}

[BurstCompile]
public partial struct FormationCohesionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (cohesion, members) in SystemAPI.Query<
            RefRW<FormationCohesion>,
            DynamicBuffer<FormationMember>>())
        {
            // Calculate base cohesion from member proficiency
            float totalProficiency = 0f;
            float avgPositionDeviation = 0f;
            int memberCount = 0;

            for (int i = 0; i < members.Length; i++)
            {
                totalProficiency += members[i].Proficiency;
                avgPositionDeviation += members[i].PositionDeviation;
                memberCount++;
            }

            cohesion.ValueRW.MemberCount = (uint)memberCount;
            cohesion.ValueRW.MemberProficiencyAverage = totalProficiency / math.max(1, memberCount);

            // Calculate relation-based cohesion bonus
            float relationBonus = CalculateFormationRelationBonus(members);
            cohesion.ValueRW.RelationCohesionBonus = relationBonus;

            // Base cohesion from proficiency (50% weight)
            float proficiencyFactor = cohesion.ValueRO.MemberProficiencyAverage * 0.5f;

            // Relation bonus (30% weight)
            float relationFactor = relationBonus * 0.3f;

            // Position adherence (20% weight)
            float positionFactor = (1f - (avgPositionDeviation / memberCount)) * 0.2f;

            // Combined cohesion
            cohesion.ValueRW.BaseCohesion = math.clamp(
                proficiencyFactor + relationFactor + positionFactor,
                0f,
                1f
            );

            // Apply decay or regen
            if (IsUnderCombatStress(cohesion.ValueRO))
            {
                // Decay under combat stress
                cohesion.ValueRW.CurrentCohesion = math.max(0f,
                    cohesion.ValueRO.CurrentCohesion - (cohesion.ValueRO.CohesionDecayRate * deltaTime));
            }
            else
            {
                // Regenerate when not fighting
                cohesion.ValueRW.CurrentCohesion = math.min(cohesion.ValueRO.BaseCohesion,
                    cohesion.ValueRO.CurrentCohesion + (cohesion.ValueRO.CohesionRegenRate * deltaTime));
            }

            // Check if formation breaks
            if (cohesion.ValueRO.CurrentCohesion < cohesion.ValueRO.CohesionBreakThreshold)
            {
                cohesion.ValueRW.IsBroken = true;
                BreakFormation(cohesion.ValueRO, members);
            }
        }
    }

    private float CalculateFormationRelationBonus(DynamicBuffer<FormationMember> members)
    {
        // Calculate average relations between all members
        // High relations = high cohesion bonus (0% to +50%)
        float totalRelations = 0f;
        int relationCount = 0;

        for (int i = 0; i < members.Length; i++)
        {
            for (int j = i + 1; j < members.Length; j++)
            {
                float relation = GetRelationValue(members[i].MemberEntity, members[j].MemberEntity);
                totalRelations += relation; // -1.0 to 1.0
                relationCount++;
            }
        }

        float avgRelation = relationCount > 0 ? totalRelations / relationCount : 0f;
        return math.clamp((avgRelation + 1f) / 2f, 0f, 1f); // Normalize to 0-1
    }
}
```

### Formation Bonus Calculation

```csharp
[BurstCompile]
public partial struct FormationBonusSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (cohesion, members) in SystemAPI.Query<
            RefRO<FormationCohesion>,
            DynamicBuffer<FormationMember>>())
        {
            if (cohesion.ValueRO.IsBroken)
                continue; // No bonuses if formation broken

            // Get base bonuses for this formation type
            FormationBonuses baseBonuses = GetBaseFormationBonuses(cohesion.ValueRO.Type);

            // Scale bonuses by cohesion
            // Formula: ActualBonus = BaseBonus × CurrentCohesion
            FormationBonuses scaledBonuses = new FormationBonuses
            {
                AttackBonus = baseBonuses.AttackBonus * cohesion.ValueRO.CurrentCohesion,
                DefenseBonus = baseBonuses.DefenseBonus * cohesion.ValueRO.CurrentCohesion,
                AccuracyBonus = baseBonuses.AccuracyBonus * cohesion.ValueRO.CurrentCohesion,
                MoraleBonus = baseBonuses.MoraleBonus * cohesion.ValueRO.CurrentCohesion,
                MovementSpeedModifier = math.lerp(1f, baseBonuses.MovementSpeedModifier, cohesion.ValueRO.CurrentCohesion),
                // Special properties require minimum cohesion threshold
                ImmuneToRout = cohesion.ValueRO.CurrentCohesion > 0.8f && baseBonuses.ImmuneToRout,
                ImmuneToFlanking = cohesion.ValueRO.CurrentCohesion > 0.7f && baseBonuses.ImmuneToFlanking,
                ChargeResistance = cohesion.ValueRO.CurrentCohesion > 0.75f && baseBonuses.ChargeResistance
            };

            // Apply bonuses to all formation members
            for (int i = 0; i < members.Length; i++)
            {
                ApplyFormationBonuses(members[i].MemberEntity, scaledBonuses);
            }
        }
    }

    private FormationBonuses GetBaseFormationBonuses(FormationType type)
    {
        return type switch
        {
            FormationType.Phalanx => new FormationBonuses
            {
                AttackBonus = 0.2f,             // +20% attack
                DefenseBonus = 1.0f,            // +100% defense
                AccuracyBonus = 0.1f,
                MovementSpeedModifier = 0.5f,   // 50% speed (very slow)
                MoraleBonus = 0.3f,
                ImmuneToFlanking = true,        // Protected from flanking
                ChargeResistance = true,        // Resists cavalry charges
                CohesionDamageResist = 0.4f     // 40% damage reduction from cohesion
            },

            FormationType.ShieldWall => new FormationBonuses
            {
                AttackBonus = 0.1f,
                DefenseBonus = 0.6f,            // +60% defense
                MovementSpeedModifier = 0.7f,
                MoraleBonus = 0.2f,
                ChargeResistance = true,
                CohesionDamageResist = 0.25f
            },

            FormationType.Wedge => new FormationBonuses
            {
                AttackBonus = 0.6f,             // +60% attack
                DefenseBonus = -0.2f,           // -20% defense (offensive)
                MovementSpeedModifier = 0.9f,
                MoraleBonus = 0.4f,             // High morale on attack
                CohesionDamageResist = 0.15f
            },

            FormationType.Skirmish => new FormationBonuses
            {
                AttackBonus = 0.3f,
                DefenseBonus = -0.3f,           // -30% defense (loose formation)
                AccuracyBonus = 0.3f,           // +30% accuracy (ranged focus)
                MovementSpeedModifier = 1.3f,   // 130% speed (very mobile)
                MoraleBonus = 0.1f,
                CohesionDamageResist = 0.05f
            },

            FormationType.Testudo => new FormationBonuses
            {
                AttackBonus = -0.4f,            // -40% attack (defensive only)
                DefenseBonus = 2.0f,            // +200% defense (extreme)
                MovementSpeedModifier = 0.3f,   // 30% speed (very slow)
                MoraleBonus = 0.5f,             // Very high morale
                ImmuneToRout = true,
                ImmuneToFlanking = true,
                ChargeResistance = true,
                CohesionDamageResist = 0.6f     // 60% damage reduction
            },

            FormationType.Square => new FormationBonuses
            {
                AttackBonus = 0f,
                DefenseBonus = 0.4f,
                MovementSpeedModifier = 0.6f,
                MoraleBonus = 0.25f,
                ImmuneToFlanking = true,        // Defends all sides
                ChargeResistance = true,
                CohesionDamageResist = 0.3f
            },

            _ => new FormationBonuses()
        };
    }
}
```

---

## Formation Breaking Mechanics

### Breaking Conditions

```csharp
public struct FormationBreakCondition : IComponentData
{
    public BreakTrigger Trigger;
    public float Severity;                  // How severe the trigger
    public float CohesionDamage;            // How much cohesion lost
    public uint TriggerTick;
}

public enum BreakTrigger : byte
{
    CasualtyThreshold = 0,      // Lost too many members
    MoraleCollapse = 1,         // Formation morale broken
    LeaderDeath = 2,            // Formation leader killed
    FlankingAttack = 3,         // Hit from side/rear
    HeavyDisruption = 4,        // Damage/knockback disruption
    CommanderOrder = 5,         // Ordered to break formation
    TerrainObstacle = 6,        // Impassable terrain
    ExhaustionFailure = 7,      // Too tired to maintain
    CommunicationLoss = 8       // Lost contact with commander
}

[BurstCompile]
public partial struct FormationBreakingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (cohesion, members, breakCondition) in SystemAPI.Query<
            RefRW<FormationCohesion>,
            DynamicBuffer<FormationMember>,
            RefRO<FormationBreakCondition>>())
        {
            // Calculate cohesion damage from trigger
            float cohesionDamage = CalculateCohesionDamage(
                breakCondition.ValueRO.Trigger,
                breakCondition.ValueRO.Severity,
                cohesion.ValueRO.Type
            );

            // Apply cohesion damage
            cohesion.ValueRW.CurrentCohesion -= cohesionDamage;

            // Special cases
            switch (breakCondition.ValueRO.Trigger)
            {
                case BreakTrigger.LeaderDeath:
                    // Leader death causes massive cohesion loss
                    cohesion.ValueRW.CurrentCohesion *= 0.5f; // Lose 50% cohesion

                    // Try to promote new leader
                    if (!PromoteNewLeader(cohesion.ValueRW, members))
                    {
                        // No suitable leader, formation breaks
                        cohesion.ValueRW.IsBroken = true;
                    }
                    break;

                case BreakTrigger.CasualtyThreshold:
                    // Lost too many members
                    if (cohesion.ValueRO.MemberCount < GetMinimumMembers(cohesion.ValueRO.Type))
                    {
                        // Too few members, cannot maintain formation
                        cohesion.ValueRW.IsBroken = true;
                    }
                    break;

                case BreakTrigger.MoraleCollapse:
                    // Morale broken, formation collapses
                    cohesion.ValueRW.IsBroken = true;
                    TriggerRout(members); // All members flee
                    break;
            }

            // Check if cohesion fell below threshold
            if (cohesion.ValueRO.CurrentCohesion < cohesion.ValueRO.CohesionBreakThreshold)
            {
                cohesion.ValueRW.IsBroken = true;
            }
        }
    }

    private float CalculateCohesionDamage(BreakTrigger trigger, float severity, FormationType type)
    {
        float baseDamage = trigger switch
        {
            BreakTrigger.CasualtyThreshold => 0.2f,     // -20% per casualty
            BreakTrigger.MoraleCollapse => 1.0f,        // -100% instant break
            BreakTrigger.LeaderDeath => 0.4f,           // -40%
            BreakTrigger.FlankingAttack => 0.3f,        // -30%
            BreakTrigger.HeavyDisruption => 0.15f,      // -15%
            BreakTrigger.TerrainObstacle => 0.25f,      // -25%
            BreakTrigger.ExhaustionFailure => 0.2f,     // -20%
            BreakTrigger.CommunicationLoss => 0.1f,     // -10%
            _ => 0.1f
        };

        // Some formations more resistant to certain triggers
        float resistanceModifier = GetBreakResistance(type, trigger);

        return baseDamage * severity * resistanceModifier;
    }

    private float GetBreakResistance(FormationType type, BreakTrigger trigger)
    {
        // Phalanx very resistant to frontal assault, weak to flanking
        if (type == FormationType.Phalanx && trigger == BreakTrigger.FlankingAttack)
            return 2.0f; // 2× damage from flanking

        // Testudo extremely resistant to disruption
        if (type == FormationType.Testudo && trigger == BreakTrigger.HeavyDisruption)
            return 0.3f; // 70% resistance

        // Square formation resistant to flanking
        if (type == FormationType.Square && trigger == BreakTrigger.FlankingAttack)
            return 0.5f; // 50% resistance

        return 1.0f; // Normal
    }
}
```

---

## Example Scenarios

### Scenario 1: Novice Phalanx vs Veteran Phalanx

```csharp
// Novice phalanx (recently trained)
var novicePhalanx = new FormationCohesion
{
    Type = FormationType.Phalanx,
    MemberCount = 20,
    MemberProficiencyAverage = 0.3f,        // Low training
    RelationCohesionBonus = 0.4f,           // Decent relations (local militia)
    CurrentCohesion = 0.5f,                 // 50% cohesion
    CohesionBreakThreshold = 0.3f
};

// Veteran phalanx (years of drilling together)
var veteranPhalanx = new FormationCohesion
{
    Type = FormationType.Phalanx,
    MemberCount = 20,
    MemberProficiencyAverage = 0.9f,        // High training
    RelationCohesionBonus = 0.8f,           // Long-time comrades
    CurrentCohesion = 0.95f,                // 95% cohesion
    CohesionBreakThreshold = 0.3f
};

// Combat bonuses:
// Novice: +100% defense × 0.5 cohesion = +50% defense
// Veteran: +100% defense × 0.95 cohesion = +95% defense

// Under flanking attack (30% cohesion damage):
// Novice: 0.5 - 0.3 = 0.2 cohesion → BREAKS (below 0.3 threshold)
// Veteran: 0.95 - 0.3 = 0.65 cohesion → HOLDS (still strong)
```

### Scenario 2: Shield Wall with Mixed Relations

```csharp
// Shield wall with strangers (mercenaries + conscripts)
var mixedShieldWall = new FormationCohesion
{
    Type = FormationType.ShieldWall,
    MemberCount = 15,
    MemberProficiencyAverage = 0.6f,        // Adequate training
    RelationCohesionBonus = 0.2f,           // Poor relations (strangers)
    CurrentCohesion = 0.55f,                // 55% cohesion
    CohesionBreakThreshold = 0.4f
};

// Shield wall with village defenders (neighbors)
var villageShieldWall = new FormationCohesion
{
    Type = FormationType.ShieldWall,
    MemberCount = 15,
    MemberProficiencyAverage = 0.5f,        // Slightly less training
    RelationCohesionBonus = 0.7f,           // High relations (neighbors)
    CurrentCohesion = 0.75f,                // 75% cohesion
    CohesionBreakThreshold = 0.4f
};

// Result: Village defenders maintain formation better despite less training
// Relations > Training for cohesion maintenance
```

### Scenario 3: Formation Training Progression

```csharp
// Week 1: Militia starts training phalanx
var week1 = new FormationTraining
{
    Formation = FormationType.Phalanx,
    TrainingLevel = 0.15f,                  // 15% proficiency
    PracticeTime = 8f,                      // 8 hours drilling
    DrillingCount = 4                       // 4 sessions
};
// Can attempt formation but cohesion = ~0.3 (very fragile)

// Week 4: Continued drilling
var week4 = new FormationTraining
{
    Formation = FormationType.Phalanx,
    TrainingLevel = 0.45f,                  // 45% proficiency
    PracticeTime = 32f,                     // 32 hours total
    DrillingCount = 16
};
// Cohesion = ~0.6 (functional, but breaks under pressure)

// Month 6: Combat veterans
var month6 = new FormationTraining
{
    Formation = FormationType.Phalanx,
    TrainingLevel = 0.8f,                   // 80% proficiency
    PracticeTime = 180f,                    // 180 hours
    DrillingCount = 72,
    CombatUsageCount = 12                   // Used in 12 battles
};
// Cohesion = ~0.9 (solid, holds under most conditions)
```

### Scenario 4: Leader Death Mid-Combat

```csharp
// Phalanx in combat, leader killed
var formationState = new FormationCohesion
{
    Type = FormationType.Phalanx,
    MemberCount = 18,                       // Lost 2 members already
    CurrentCohesion = 0.75f,                // Still strong
    FormationLeader = Entity.Null           // Leader just died
};

// Leader death trigger
var breakCondition = new FormationBreakCondition
{
    Trigger = BreakTrigger.LeaderDeath,
    Severity = 1.0f,
    CohesionDamage = 0.4f
};

// Result:
// - Cohesion: 0.75 → 0.35 (lost 40%)
// - Cohesion halved: 0.35 × 0.5 = 0.175
// - Below threshold (0.3) → Formation BREAKS
// - Members scatter, lose all formation bonuses
// - Now vulnerable to cavalry charge

// Alternative with promotion:
// - Second-in-command promoted
// - Cohesion: 0.75 → 0.35 (still lost 40%)
// - Barely holds at 0.35 cohesion
// - Bonuses severely reduced but formation intact
```

---

## Integration with Other Systems

### Communication System Integration

Commands use language/communication clarity:

```csharp
public struct TacticalCommandCommunication : IComponentData
{
    public Entity Commander;
    public CommunicationMethod Method;
    public float Clarity;                   // From language system
    public bool RequiresLineOfSight;
}

public enum CommunicationMethod : byte
{
    Verbal = 0,             // Shouted orders
    Visual = 1,             // Hand signals
    Horn = 2,               // Horn blasts (pre-arranged signals)
    Drum = 3,               // Drum beats
    Flag = 4,               // Flag signals
    Magical = 5             // Telepathy/magical communication
}

// Example: Shouted order in native language = 100% clarity
// Hand signal = 70% clarity (universal but prone to misinterpretation)
// Horn blast = 90% clarity (pre-drilled signals)
```

### Relations System Integration

Cohesion scales with relations between formation members:

```csharp
// High relations (family, long-time friends)
relationBonus = 0.8f;   // +80% cohesion contribution

// Neutral (acquaintances)
relationBonus = 0.4f;   // +40%

// Low relations (strangers, rivals)
relationBonus = 0.1f;   // +10%

// Enemies forced to fight together
relationBonus = -0.2f;  // -20% (actively sabotaging cohesion)
```

### Patience System Integration

Impatient entities struggle to maintain formation:

```csharp
public static float GetFormationPatienceModifier(in Patience patience)
{
    // Formations require patience (standing in place, waiting for orders)
    // Impatient entities break formation prematurely
    return patience.PatienceRating; // 0.0 to 1.0 multiplier on cohesion
}

// Example: Impatient scout (0.2 patience) in phalanx
// Cohesion contribution: 0.2 (reduces overall formation cohesion)
// Likely to break formation early

// Patient warrior (0.8 patience)
// Cohesion contribution: 0.8 (maintains position well)
```

### Combat Mechanics Integration

Formation bonuses affect accuracy disruption:

```csharp
// Phalanx provides cohesion damage resistance
if (HasFormation(entity, FormationType.Phalanx))
{
    var bonuses = GetFormationBonuses(entity);
    damageDisruption *= (1f - bonuses.CohesionDamageResist); // 40% reduction
}

// Formation defense bonus stacks with stability
effectiveDefense = baseDefense * (1f + formationDefenseBonus);
```

---

## Performance Considerations

**Profiling Targets**:
```
Command Issuance:         <0.1ms per command (10 recipients)
Formation Cohesion Calc:  <0.2ms per formation (20 members)
Bonus Application:        <0.05ms per entity
Training Update:          <0.03ms per entity
Breaking Check:           <0.1ms per formation
────────────────────────────────────────
Total (100 formations):   <50ms per frame
```

**Optimizations**:
- Batch command processing (issue to all recipients at once)
- Cache relation lookups (don't recalculate every frame)
- Update cohesion at 10Hz instead of every frame
- Use spatial queries for formation member proximity
- Lazy update training (only when drilling or in combat)

---

## Summary

**Tactical Commands**:
- **Command types**: Movement (follow, stay, retreat), combat (attack, defend, focus fire), formation (form up, hold, break)
- **Compliance system**: Authority + relations + personality + priority determine obedience
- **Communication-based**: Commands use language/clarity system, can be misunderstood
- **Cross-game**: Infantry commands (Godgame), ship fleet commands (Space4X)

**Formation System**:
- **Training required**: Entities must learn formations through drilling and practice
- **Proficiency levels**: 0% to 100%, affects cohesion contribution
- **Formation types**: Phalanx, shield wall, wedge, testudo, skirmish, line, square, and more
- **Bonuses**: Attack, defense, morale, movement speed, special properties (immune to flanking/rout)

**Cohesion Mechanics**:
- **Cohesion = Proficiency (50%) + Relations (30%) + Position Adherence (20%)**
- **Bonuses scale with cohesion**: 50% cohesion = 50% of bonuses
- **Breaking conditions**: Casualties, morale collapse, leader death, flanking, disruption
- **Recovery**: Cohesion regenerates when not under combat stress

**Key Insight**: A phalanx is only as strong as its weakest link. Novice warriors with poor relations create fragile formations that break under pressure. Veteran comrades with high cohesion become unstoppable defensive walls.
