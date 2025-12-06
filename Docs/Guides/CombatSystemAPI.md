# Combat System API Reference

Complete API reference for the Combat Skill Gating System.

## Namespaces

- `PureDOTS.Runtime.Combat` - Core combat components and systems
- `PureDOTS.Authoring.Combat` - Authoring components

## Enums

### BehaviorTier

```csharp
public enum BehaviorTier : byte
{
    Baseline = 0,  // Always available behaviors
    Learned = 1,  // Unlocked via skill thresholds
    Mastered = 2  // Requires skill + implant
}
```

### AtomicAction

```csharp
public enum AtomicAction : byte
{
    Dash = 0,
    Swing = 1,
    Parry = 2,
    Jump = 3,
    Fire = 4,
    Cast = 5
}
```

### ActionId

```csharp
public enum ActionId : ushort
{
    None = 0,
    SimpleAttack = 1,
    SimpleParry = 2,
    SimpleMove = 3,
    StrafeShoot = 4,
    CounterParry = 5,
    MultiTargetDodge = 6,
    DualCast = 7
}
```

### ImplantFlags

```csharp
[Flags]
public enum ImplantFlags : byte
{
    None = 0,
    DualSynapse = 1 << 0,
    NeuralBoost = 1 << 1,
    ReflexEnhancement = 1 << 2,
    CombatImplant1 = 1 << 3,
    CombatImplant2 = 1 << 4,
    CombatImplant3 = 1 << 5,
    CombatImplant4 = 1 << 6,
    CombatImplant5 = 1 << 7
}
```

## Components

### BehaviorTierState

Current behavior tier and active behavior.

```csharp
public struct BehaviorTierState : IComponentData
{
    public BehaviorTier Tier;        // Current tier
    public ushort ActiveBehaviorId;  // Active behavior ID
}
```

### StaminaState

Physical endurance pool.

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

### ImplantTag

Implant flags for behavior gating.

```csharp
public struct ImplantTag : IComponentData
{
    public ImplantFlags Flags;
}
```

### CognitiveStats

Wisdom/Finesse/Physique modifiers.

```csharp
public struct CognitiveStats : IComponentData
{
    public float Wisdom;    // Learning speed, focus regen
    public float Finesse;  // Multi-action precision
    public float Physique; // Mass, stamina, inertia
}
```

### BehaviorModifier

Computed modifiers from stats.

```csharp
public struct BehaviorModifier : IComponentData
{
    public float FocusCostMultiplier;      // Applied to focus costs
    public float StaminaCostMultiplier;     // Applied to stamina costs
    public float LearningRateMultiplier;   // Applied to learning speed
}
```

### MotionReactionState

Reactive motion capabilities.

```csharp
public struct MotionReactionState : IComponentData
{
    public float ReactionSkill;     // 0-1 reaction capability
    public bool CanMidAirParry;     // Can parry while airborne
}
```

### CombatLearningState

Adaptive learning configuration.

```csharp
public struct CombatLearningState : IComponentData
{
    public float LearningRate;      // How fast weights adjust
    public float DecayRate;         // How fast weights decay
    public uint LastDecayTick;      // Last decay application tick
}
```

### FleetCommandState

Fleet command learning state.

```csharp
public struct FleetCommandState : IComponentData
{
    public float LearnRate;
    public BlobAssetReference<TacticSuccessRateBlob> Tactics;
}
```

### Tags

```csharp
public struct MultiTargetBehaviorTag : IComponentData { }
public struct LeaderTag : IComponentData { }
public struct PresentationCommandQueueTag : IComponentData { }
```

## Buffers

### BehaviorSet

Unlocked behaviors per entity.

```csharp
[InternalBufferCapacity(16)]
public struct BehaviorSet : IBufferElementData
{
    public ushort BehaviorId;
    public uint UnlockTick;
}
```

### ActionComposition

Active action sequences.

```csharp
[InternalBufferCapacity(8)]
public struct ActionComposition : IBufferElementData
{
    public AtomicAction Action;
    public float StartTime;
    public float Duration;
    public float3 Direction;
}
```

### HitBuffer

Damage/hit events.

```csharp
[InternalBufferCapacity(8)]
public struct HitBuffer : IBufferElementData
{
    public Entity Target;
    public float Damage;
    public float3 HitPoint;
    public uint Tick;
}
```

### ImpulseEvent

Physics impulse events.

```csharp
[InternalBufferCapacity(8)]
public struct ImpulseEvent : IBufferElementData
{
    public float3 Force;
    public Entity Source;
    public float Magnitude;
    public uint Tick;
}
```

### BehaviorUnlockEvent

Unlock events for presentation.

```csharp
[InternalBufferCapacity(4)]
public struct BehaviorUnlockEvent : IBufferElementData
{
    public ushort BehaviorId;
    public uint UnlockTick;
}
```

### BehaviorSuccessRate

Success rate tracking for learning.

```csharp
[InternalBufferCapacity(16)]
public struct BehaviorSuccessRate : IBufferElementData
{
    public ushort BehaviorId;
    public uint SuccessCount;
    public uint AttemptCount;
    public float Weight;
}
```

### BehaviorEvent

Events for presentation sync.

```csharp
[InternalBufferCapacity(8)]
public struct BehaviorEvent : IBufferElementData
{
    public ushort BehaviorId;
    public uint StartTick;
    public float3 Position;
    public float3 Direction;
}
```

## Blob Assets

### BehaviorCatalogBlob

Behavior catalog structure.

```csharp
public struct BehaviorCatalogBlob
{
    public BlobArray<BehaviorNode> Nodes;
}
```

### BehaviorNode

Individual behavior node (blob-safe).

```csharp
public struct BehaviorNode
{
    public ushort Id;
    public float SkillReq;
    public byte ImplantTag;
    public float FocusCost;
    public float StaminaCost;
    public float BaseWeight;
    public FixedList64Bytes<ActionId> Actions;
}
```

### TacticSuccessRateBlob

Fleet tactic success rates.

```csharp
public struct TacticSuccessRateBlob
{
    public BlobHashMap<int, float> TacticWeights;
}
```

## Systems

All systems are `[BurstCompile]` and run in `CombatSystemGroup` unless noted.

### BehaviorCatalogSystem

Loads and validates behavior catalogs. Runs first in group.

### BehaviorGatingSystem

Evaluates 3-tier behavior model. Updates `BehaviorTierState` based on skills/implants.

### StaminaUpdateSystem

Regenerates stamina each tick. Mirrors `FocusUpdateSystem` pattern.

### BehaviorCostSystem

Consumes focus/stamina on behavior activation. Downgrades tier if insufficient.

### ActionComposerSystem

Blends atomic actions into sequences. Evaluates up to N composites per entity per tick.

### CombatExecutionSystem

Applies physics impulses, damage, hit detection. Uses Unity Physics.

### BehaviorUnlockSystem

Checks skill thresholds + implant tags. Unlocks behaviors, adds to `BehaviorSet`.

### SkillEfficiencySystem

Applies efficiency modifiers: `FocusCost * (1 - Skill)`.

### ImpulseReactionSystem

Processes `ImpulseEvent` buffer. Computes resulting velocity via physics.

### ParryReactionSystem

Converts damage to stamina drain. Applies counter-impulse based on stamina ratio.

### ReboundSystem

Applies anime-style rebound based on stamina ratio.

### TargetPacketSystem

Builds target packets from spatial queries (max 8 targets).

### MultiTargetCombatSystem

Divides focus cost by packet size. Processes all targets in burst job.

### CombatLearningSystem

Tracks hit/miss, block/fail rates. Adjusts behavior weights.

### LearningDecaySystem

Applies periodic decay to maintain tactical diversity.

### CognitiveModifierSystem

Computes modifiers from `CognitiveStats`. Applies to costs and learning rates.

### FleetCommandSystem

Tracks tactic success vs culture. Updates weights via lerp.

### FormationAdaptationSystem

Biases formation/targeting heuristics toward successful doctrines.

### CombatPresentationBridge

Feeds `BehaviorEvent` buffer into `PresentationCommandQueue`. Runs in `PresentationSystemGroup`.

## Authoring

### BehaviorCatalogAuthoring

ScriptableObject for creating behavior catalogs.

```csharp
[CreateAssetMenu(fileName = "BehaviorCatalog", menuName = "PureDOTS/Combat/Behavior Catalog")]
public sealed class BehaviorCatalogAuthoring : ScriptableObject
{
    public List<BehaviorNodeDefinition> nodes;
    
    public BlobAssetReference<BehaviorCatalogBlob> CreateBlobAsset();
}
```

## Helper Functions

### Checking Behavior Unlock

```csharp
bool IsBehaviorUnlocked(DynamicBuffer<BehaviorSet> behaviorSet, ushort behaviorId)
{
    for (int i = 0; i < behaviorSet.Length; i++)
    {
        if (behaviorSet[i].BehaviorId == behaviorId)
            return true;
    }
    return false;
}
```

### Calculating Focus Cost with Modifiers

```csharp
float CalculateEffectiveCost(float baseCost, in BehaviorModifier modifier)
{
    return baseCost * modifier.FocusCostMultiplier;
}
```

### Checking Tier Requirements

```csharp
bool CanUseBehavior(BehaviorTier requiredTier, BehaviorTier currentTier)
{
    return currentTier >= requiredTier;
}
```

## Performance Notes

- All systems use `IJobEntity` for parallel execution
- Buffers use `[InternalBufferCapacity]` for pooling
- Tiered tick rates: Baseline 60Hz, Advanced 30Hz
- Change filters on skill updates for dirty-flagging
- Blob assets shared across millions of entities

## See Also

- `CombatSystemIntegration.md` - Integration guide
- `Docs/BestPractices/DOTS_1_4_Patterns.md` - DOTS patterns

