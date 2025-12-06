# Combat Skill Gating System Integration Guide

## Overview

The Combat Skill Gating System provides a deterministic, scalable combat framework with hierarchical behavior graphs, procedural action composition, and adaptive learning. All systems are Burst-compiled, deterministic, and mod-extensible.

## Architecture

### System Groups

Combat systems run in `CombatSystemGroup` (within `PhysicsSystemGroup`). Execution order:

1. `BehaviorCatalogSystem` - Loads/validates behavior catalogs
2. `BehaviorGatingSystem` - Evaluates 3-tier behavior model
3. `StaminaUpdateSystem` - Regenerates stamina
4. `BehaviorCostSystem` - Consumes focus/stamina
5. `ActionComposerSystem` - Blends atomic actions
6. `CombatExecutionSystem` - Applies physics impulses
7. `BehaviorUnlockSystem` - Experience-driven unlocks
8. `SkillEfficiencySystem` - Efficiency modifiers
9. `ImpulseReactionSystem` - Reactive motion
10. `ParryReactionSystem` - Parry mechanics
11. `ReboundSystem` - Rebound effects
12. `TargetPacketSystem` - Multi-target packet building
13. `MultiTargetCombatSystem` - AoE handling
14. `CombatLearningSystem` - Adaptive learning
15. `LearningDecaySystem` - Tactical diversity
16. `CognitiveModifierSystem` - Stat modifiers
17. `FleetCommandSystem` - Aggregate command learning
18. `FormationAdaptationSystem` - Formation adaptation

### Core Components

**Required Components for Combat Entities:**
- `BehaviorTierState` - Current behavior tier (Baseline/Learned/Mastered)
- `StaminaState` - Physical endurance pool
- `FocusState` - Mental bandwidth (from `PureDOTS.Runtime.Focus`)
- `SkillSet` - Skill levels (from `PureDOTS.Runtime.Skills`)
- `ImplantTag` - Implant flags for behavior gating
- `CognitiveStats` - Wisdom/Finesse/Physique modifiers

**Buffers:**
- `BehaviorSet` - Unlocked behaviors per entity
- `ActionComposition` - Active action sequences
- `HitBuffer` - Damage/hit events
- `ImpulseEvent` - Physics impulse events
- `BehaviorEvent` - Events for presentation sync

## Quick Start

### 1. Setting Up a Combat Entity

```csharp
// In your authoring/baker
var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

// Add combat components
AddComponent(entity, new BehaviorTierState
{
    Tier = BehaviorTier.Baseline,
    ActiveBehaviorId = 0
});

AddComponent(entity, new StaminaState
{
    Current = 100f,
    Max = 100f,
    RegenRate = 5f,
    SoftThreshold = 30f,
    HardThreshold = 10f
});

AddComponent(entity, new ImplantTag
{
    Flags = ImplantFlags.None
});

AddComponent(entity, new CognitiveStats
{
    Wisdom = 5f,
    Finesse = 5f,
    Physique = 5f
});

// Add buffers
AddBuffer<BehaviorSet>(entity);
AddBuffer<ActionComposition>(entity);
AddBuffer<HitBuffer>(entity);
AddBuffer<ImpulseEvent>(entity);
AddBuffer<BehaviorEvent>(entity);
```

### 2. Creating Behavior Catalogs

Create a `BehaviorCatalogAuthoring` ScriptableObject:

1. Right-click in Project → `PureDOTS/Combat/Behavior Catalog`
2. Define behavior nodes with:
   - `Id` - Unique behavior identifier
   - `SkillReq` - Skill threshold (0.0-1.0)
   - `ImplantTag` - Required implant flags
   - `FocusCost` / `StaminaCost` - Resource costs
   - `Actions` - List of `ActionId` values

3. Convert to blob asset in baking system:

```csharp
var catalogBlob = catalogAuthoring.CreateBlobAsset();
AddComponent(entity, new BehaviorCatalogReference
{
    Catalog = catalogBlob
});
```

### 3. Behavior Unlocking

Behaviors unlock automatically when:
- Skill level >= `SkillReq` threshold
- Required `ImplantTag` flags are present
- Entity has sufficient resources

Unlocked behaviors are added to `BehaviorSet` buffer. Check unlock events:

```csharp
var unlockEvents = SystemAPI.GetBuffer<BehaviorUnlockEvent>(entity);
for (int i = 0; i < unlockEvents.Length; i++)
{
    var evt = unlockEvents[i];
    // Handle unlock: sync to presentation, update UI, etc.
}
```

### 4. Triggering Combat Actions

Actions are composed procedurally based on:
- Current `BehaviorTierState.Tier`
- Unlocked behaviors in `BehaviorSet`
- Available focus/stamina

To trigger a behavior, add actions to `ActionComposition` buffer:

```csharp
var compositions = SystemAPI.GetBuffer<ActionComposition>(entity);
compositions.Add(new ActionComposition
{
    Action = AtomicAction.Swing,
    StartTime = currentTime,
    Duration = 0.5f,
    Direction = targetDirection
});
```

### 5. Multi-Target Combat

For AoE behaviors:

1. Add `MultiTargetBehaviorTag` to entity
2. `TargetPacketSystem` builds target packets (max 8 targets)
3. `MultiTargetCombatSystem` divides focus cost by packet size
4. Process all targets in single burst job

```csharp
AddComponent(entity, new MultiTargetBehaviorTag());
// Targets populated automatically by TargetPacketSystem
```

## Integration Patterns

### With Existing Systems

**Focus Integration:**
- Combat uses existing `FocusState` from `PureDOTS.Runtime.Focus`
- `BehaviorCostSystem` consumes focus on behavior activation
- Focus regeneration handled by `FocusUpdateSystem`

**Skill Integration:**
- Uses `SkillSet` from `PureDOTS.Runtime.Skills`
- `BehaviorUnlockSystem` reads skill levels via `GetLevel(SkillId)`
- `SkillEfficiencySystem` applies efficiency modifiers

**Physics Integration:**
- Uses `PhysicsVelocity` for motion
- `CombatExecutionSystem` applies impulses via `ApplyLinearImpulse`
- All physics deterministic, Burst-compiled

**Group Integration:**
- Leaders use `LeaderTag` + `FleetCommandState`
- `FleetCommandSystem` tracks tactic success vs cultures
- `FormationAdaptationSystem` adapts group formations

### Presentation Sync

Behavior events sync to presentation layer:

1. `BehaviorEvent` buffer populated by combat systems
2. `CombatPresentationBridge` copies to `PresentationCommandQueue`
3. Presentation systems consume events for animation/VFX

```csharp
// In presentation system
var events = SystemAPI.GetBuffer<BehaviorEvent>(entity);
for (int i = 0; i < events.Length; i++)
{
    var evt = events[i];
    // Trigger animation, VFX, sound, etc.
}
```

## Performance Considerations

### Tiered Tick Rates

- **Baseline behaviors**: 60Hz (every tick)
- **Advanced behaviors**: 30Hz (every 2 ticks)

Systems automatically throttle based on `BehaviorTierState.Tier`.

### Buffer Pooling

All buffers use `[InternalBufferCapacity]` for pooling:
- `BehaviorSet`: 16 entries
- `ActionComposition`: 8 entries
- `HitBuffer`: 8 entries
- `ImpulseEvent`: 8 entries

### Dirty-Flagging

`BehaviorGatingSystem` only recalculates on skill changes. Use change filters:

```csharp
var query = SystemAPI.QueryBuilder()
    .WithAll<BehaviorTierState, SkillSet>()
    .WithChangeFilter<SkillSet>() // Only update on skill changes
    .Build();
```

## Modding Support

### Extending Action Types

Add new `AtomicAction` enum values:

```csharp
public enum AtomicAction : byte
{
    Dash = 0,
    Swing = 1,
    // ... existing actions ...
    CustomAction1 = 6, // Add your actions
    CustomAction2 = 7
}
```

### Custom Behavior Catalogs

Create modded `BehaviorCatalogAuthoring` assets:
1. Define custom behaviors with unique IDs
2. Set skill/implant requirements
3. Assign action sequences
4. Load at runtime via blob asset system

### Implant Extensions

Extend `ImplantFlags` enum (8 bits available):

```csharp
[Flags]
public enum ImplantFlags : byte
{
    // ... existing flags ...
    CustomImplant1 = 1 << 6,
    CustomImplant2 = 1 << 7
}
```

## API Reference

### Component APIs

**BehaviorTierState:**
```csharp
public struct BehaviorTierState : IComponentData
{
    public BehaviorTier Tier;        // Baseline/Learned/Mastered
    public ushort ActiveBehaviorId;  // Currently active behavior
}
```

**StaminaState:**
```csharp
public struct StaminaState : IComponentData
{
    public float Current;          // 0..Max
    public float Max;
    public float RegenRate;        // Per tick regeneration
    public float SoftThreshold;    // Performance penalty threshold
    public float HardThreshold;    // Exhaustion threshold
}
```

**BehaviorModifier:**
```csharp
public struct BehaviorModifier : IComponentData
{
    public float FocusCostMultiplier;      // Applied to focus costs
    public float StaminaCostMultiplier;    // Applied to stamina costs
    public float LearningRateMultiplier;   // Applied to learning speed
}
```

### System Queries

**Query entities with combat capabilities:**
```csharp
var combatQuery = SystemAPI.QueryBuilder()
    .WithAll<BehaviorTierState, StaminaState, FocusState>()
    .Build();
```

**Query multi-target entities:**
```csharp
var multiTargetQuery = SystemAPI.QueryBuilder()
    .WithAll<MultiTargetBehaviorTag, TargetPacket>()
    .Build();
```

**Query leaders:**
```csharp
var leaderQuery = SystemAPI.QueryBuilder()
    .WithAll<LeaderTag, FleetCommandState>()
    .Build();
```

## Examples

### Example: Simple Attack Behavior

```csharp
// In your combat system
var compositions = SystemAPI.GetBuffer<ActionComposition>(entity);
var tierState = SystemAPI.GetComponent<BehaviorTierState>(entity);

if (tierState.Tier >= BehaviorTier.Baseline)
{
    compositions.Add(new ActionComposition
    {
        Action = AtomicAction.Swing,
        StartTime = currentTime,
        Duration = 0.5f,
        Direction = math.normalize(targetPos - myPos)
    });
}
```

### Example: Skill-Based Unlock Check

```csharp
var skillSet = SystemAPI.GetComponent<SkillSet>(entity);
byte castingSkill = skillSet.GetLevel(SkillId.Gunnery); // Example skill

if (castingSkill >= 70) // 70% skill threshold
{
    // Unlock advanced behavior (handled by BehaviorUnlockSystem)
    // Or manually add to BehaviorSet buffer
}
```

### Example: Applying Impulse Reaction

```csharp
var impulses = SystemAPI.GetBuffer<ImpulseEvent>(entity);
impulses.Add(new ImpulseEvent
{
    Force = knockbackForce,
    Source = attackerEntity,
    Magnitude = math.length(knockbackForce),
    Tick = currentTick
});
// ImpulseReactionSystem processes automatically
```

## Troubleshooting

### Behaviors Not Unlocking

1. Check `SkillSet` has sufficient skill level
2. Verify `ImplantTag` has required flags
3. Ensure `BehaviorCatalog` blob is loaded
4. Check `BehaviorUnlockSystem` is running

### Performance Issues

1. Verify tiered tick rates are working (check `BehaviorGatingSystem` tick modulo)
2. Profile buffer allocations (should use pooled buffers)
3. Check change filters on skill updates
4. Verify Burst compilation enabled

### Physics Not Applying

1. Ensure entity has `PhysicsVelocity` component
2. Check `CombatExecutionSystem` is running
3. Verify rewind guards are correct
4. Check impulse magnitudes are reasonable

## See Also

- `Docs/Guides/MovementAuthoringGuide.md` - Movement system integration
- `Docs/Physics/PhysicsIntegrationGuide.md` - Physics system details
- `Docs/BestPractices/DOTS_1_4_Patterns.md` - DOTS patterns
- `TRI_PROJECT_BRIEFING.md` - Project overview

