# Tactical AI Guide

**Purpose**: Guide for using the hierarchical behavior system (formations, tactical AI, group morale, cultural doctrines) in PureDOTS.

## Overview

The tactical layer connects aggregate strategy (formations, commands) with individual expression (personality modifiers, deviation) under a deterministic umbrella. Systems run at different frequencies:
- **Strategic** (0.2-1 Hz): Issues commands to formations
- **Tactical** (1-5 Hz): Processes commands, updates morale, evaluates AI state
- **Individual** (60 Hz): Applies deviation, updates movement

## Creating a Formation

### 1. Create Formation Entity

```csharp
// In authoring or runtime system
var formationEntity = ecb.CreateEntity();
ecb.AddComponent(formationEntity, new BandId
{
    Value = formationId,
    FactionId = factionId,
    Leader = leaderEntity
});
ecb.AddComponent(formationEntity, new BandFormation
{
    Formation = BandFormationType.Line,
    Spacing = 1.5f,
    Cohesion = 1.0f,
    Morale = 0.8f,
    FormationId = (ushort)formationId
});
ecb.AddComponent(formationEntity, new BandStats
{
    MemberCount = 0,
    AverageDiscipline = 0.7f,
    Morale = 0.8f,
    Cohesion = 1.0f
});
ecb.AddComponent(formationEntity, new GroupMorale
{
    CurrentMorale = 0.8f,
    LeaderAlive = true,
    CasualtyCount = 0
});
ecb.AddBuffer<BandMember>(formationEntity);
```

### 2. Add Members to Formation

```csharp
var members = SystemAPI.GetBuffer<BandMember>(formationEntity);
members.Add(new BandMember
{
    Villager = villagerEntity,
    Role = 0 // 0 = member, 1 = leader, etc.
});

// Add FormationMember component to villager
ecb.AddComponent(villagerEntity, new FormationMember
{
    FormationEntity = formationEntity,
    Offset = float3.zero, // Will be computed by FormationCommandSystem
    Alignment = 1.0f
});
```

### 3. Issue Formation Command

```csharp
// Strategic layer issues command
ecb.AddComponent(formationEntity, new FormationCommand
{
    CommandId = 1, // Move = 1, Attack = 2, Hold = 3, Regroup = 4
    TargetPos = targetPosition,
    Facing = targetDirection
});

// Mark as dirty for processing
ecb.AddComponent<FormationCommandDirtyTag>(formationEntity);
```

## Individual Behavior Profiles

### Setting BehaviorProfile

```csharp
// In villager authoring or runtime system
ecb.AddComponent(villagerEntity, new BehaviorProfile
{
    Discipline = 0.8f,  // High discipline = better formation following
    Courage = 0.7f,     // High courage = less morale loss from damage
    Chaos = 0.2f,       // Low chaos = less random deviation
    Zeal = 0.6f         // Moderate zeal = follows leader ideals
});
```

### BehaviorProfile Influence Formulas

- **Formation adherence**: `Alignment -= Chaos * dt`
- **Morale loss**: `ΔMorale = -Damage * (1 + (1 - Courage))`
- **Aggression**: `AttackWeight *= Zeal * GroupMorale`

## Group Morale System

### Updating Morale

```csharp
// Mark formation for morale update
ecb.AddComponent<MoraleDirtyTag>(formationEntity);

// GroupMoraleSystem will process:
// - Casualties: Morale -= Losses * 0.1f
// - Leader death: Morale *= 0.7f
// - Support proximity: Morale += AlliesNearby * 0.02f
// - Discipline/Courage modifiers applied automatically
```

### Rout State

When morale < 0.3, `RoutState` component is automatically added:

```csharp
// Check for rout state
if (SystemAPI.HasComponent<RoutState>(formationEntity))
{
    // Trigger flee behavior in AI systems
    // RoutState contains MoraleAtRout and RoutStartTick
}
```

## Cultural Doctrines

### Creating Cultural Doctrine Asset

1. Create `CulturalDoctrineAsset` ScriptableObject in Unity Editor
2. Set archetype properties:
   - `soulHarvestBias`: Converts enemy deaths to focus energy
   - `holyEntityMoraleBonus`: Morale bonus near holy entities
   - `deviationMultiplier`: Random formation deviation multiplier
   - `ignoreMoraleDecayOnGrudge`: Ignores morale loss vs grudge targets
   - `deadEnemyAttackWeightBonus`: Attack weight modifier based on dead enemies

### Applying Doctrine to Formation

```csharp
// In authoring or bootstrap
var doctrineAuthoring = GetComponent<CulturalDoctrineAuthoring>();
// Doctrine is baked via CulturalDoctrineBaker

// At runtime, link doctrine to formation
if (SystemAPI.HasComponent<CulturalDoctrineReference>(doctrineEntity))
{
    var doctrineRef = SystemAPI.GetComponent<CulturalDoctrineReference>(doctrineEntity);
    ecb.AddComponent(formationEntity, doctrineRef);
}
```

### Doctrine Effects

- **Corrupt Spiritualist**: High `soulHarvestBias`, converts enemy deaths → focus energy
- **Zealot Paladin**: High `holyEntityMoraleBonus`, gains morale near holy entities
- **Chaotic Marauder**: High `deviationMultiplier`, random formation deviation
- **Dwarven Captain**: `ignoreMoraleDecayOnGrudge = true`, ignores morale loss vs grudge targets

## Tactical AI State Machine

### States

- **Idle**: Default state, transitions to Advance when cohesion > 0.7 and morale > 0.6
- **Advance**: Moving forward, transitions to Engage when engaged, Regroup if cohesion < 0.5
- **Engage**: In combat, transitions to Evaluate if morale < 0.4, Pursue if not engaged
- **Evaluate**: Assessing situation, transitions to Engage if morale > 0.5, Regroup if morale < 0.3
- **Regroup**: Recovering cohesion, transitions to Idle when cohesion > 0.7 and morale > 0.5
- **Pursue**: Chasing enemy, transitions to Engage if engaged, Regroup if cohesion < 0.5

### Adding Tactical AI to Formation

```csharp
ecb.AddComponent(formationEntity, new TacticalAIState
{
    State = TacticalAIStateType.Idle,
    DecisionTick = currentTick,
    Context = new float4(0.8f, 0.7f, 0.5f, 0.5f) // Morale, Cohesion, Commander, Battlefield
});
```

## Leader Influence Field

### Adding CommandAura to Leader

```csharp
ecb.AddComponent(leaderEntity, new CommandAura
{
    Radius = 10f,
    CohesionBonus = 0.1f,  // Per second bonus
    MoraleBonus = 0.05f    // Per second bonus
});
```

### Effect Application

`LeaderAuraSystem` automatically applies bonuses to members within radius:
- `member.Morale += CohesionBonus * Discipline`
- Updates formation cohesion based on average member alignment

## Performance Considerations

### Dirty Flags

Systems only process entities with dirty tags:
- `FormationCommandDirtyTag`: Added when `FormationCommand` changes
- `MoraleDirtyTag`: Added when morale needs update (casualties, leader death, etc.)

```csharp
// Mark for processing
ecb.AddComponent<FormationCommandDirtyTag>(formationEntity);
ecb.AddComponent<MoraleDirtyTag>(formationEntity);
```

### Update Frequencies

- **TacticalSystemGroup**: 1-5 Hz (use `PeriodicTickComponent` for throttling)
- **VillagerSystemGroup**: 60 Hz (runs every tick)

### Determinism

All systems check `RewindState.Mode` and only process in `Record` mode. All calculations use deterministic math (no random, seeded noise from EntityId).

## Integration with Existing Systems

### Band Systems

The tactical layer extends existing `BandFormation`, `BandStats`, `BandMember` components. Existing `BandFormationSystem` continues to work alongside new tactical systems.

### Morale Systems

`GroupMorale` extends individual `EntityMorale`. Group morale influences individual morale via `GroupMoraleSystem`.

### AI Systems

Tactical AI state machine integrates with existing AI pipeline. Formation commands can be issued by strategic AI or player input.

## Authoring Guide

### Villager Authoring

`BehaviorProfile` is automatically added to villagers via `VillagerAuthoring`. Default values:
- Discipline: 0.7f
- Courage: 0.6f
- Chaos: 0.2f
- Zeal: 0.5f

### Cultural Doctrine Authoring

1. Create `CulturalDoctrineAsset` ScriptableObject
2. Add `CulturalDoctrineAuthoring` component to GameObject
3. Assign asset to `doctrineAsset` field
4. Baker automatically creates `CulturalDoctrineReference` component

## Example: Complete Formation Setup

```csharp
// 1. Create formation
var formationEntity = ecb.CreateEntity();
ecb.AddComponent(formationEntity, new BandId { Value = 1, Leader = leaderEntity });
ecb.AddComponent(formationEntity, new BandFormation { Formation = BandFormationType.Line });
ecb.AddComponent(formationEntity, new BandStats { MemberCount = 10 });
ecb.AddComponent(formationEntity, new GroupMorale { CurrentMorale = 0.8f, LeaderAlive = true });
ecb.AddComponent(formationEntity, new TacticalAIState { State = TacticalAIStateType.Idle });
ecb.AddBuffer<BandMember>(formationEntity);

// 2. Add members
var members = SystemAPI.GetBuffer<BandMember>(formationEntity);
for (int i = 0; i < 10; i++)
{
    var villager = CreateVillager(); // Your villager creation logic
    members.Add(new BandMember { Villager = villager, Role = 0 });
    ecb.AddComponent(villager, new FormationMember { FormationEntity = formationEntity });
    ecb.AddComponent(villager, new BehaviorProfile { Discipline = 0.8f, Courage = 0.7f });
}

// 3. Issue command
ecb.AddComponent(formationEntity, new FormationCommand
{
    CommandId = 1, // Move
    TargetPos = new float3(10, 0, 10),
    Facing = new float3(0, 0, 1)
});
ecb.AddComponent<FormationCommandDirtyTag>(formationEntity);

// 4. Add leader aura
ecb.AddComponent(leaderEntity, new CommandAura
{
    Radius = 15f,
    CohesionBonus = 0.1f,
    MoraleBonus = 0.05f
});
```

## Troubleshooting

### Formations Not Moving

- Check `FormationCommand` component exists and has valid `TargetPos`
- Verify `FormationCommandDirtyTag` is added when command changes
- Ensure `FormationCommandSystem` is enabled and running

### Morale Not Updating

- Check `GroupMorale` component exists on formation
- Verify `MoraleDirtyTag` is added when casualties occur or leader dies
- Ensure `GroupMoraleSystem` is enabled

### Members Not Following Formation

- Verify `FormationMember` component exists on members
- Check `BehaviorProfile.Chaos` value (high chaos = more deviation)
- Ensure `FormationDeviationSystem` is running in `VillagerSystemGroup`

### Tactical AI Not Transitioning States

- Check `TacticalAIState` component exists
- Verify context values (morale, cohesion) are being updated
- Ensure minimum time in state (10 ticks) has passed

## See Also

- `Docs/Features/AggregateDynamics.md` - Aggregate dynamics overview
- `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/Bands/BandComponents.cs` - Component definitions
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Tactical/` - System implementations

