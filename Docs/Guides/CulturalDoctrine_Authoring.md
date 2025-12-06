# Cultural Doctrine Authoring Guide

**Purpose**: Guide for creating and authoring cultural doctrine assets that modify formation behavior.

## Overview

Cultural doctrines define archetype behaviors that modify how formations respond to combat situations, morale changes, and environmental factors. Doctrines are stored as BlobAssets for zero-GC, read-only access.

## Creating a Cultural Doctrine Asset

### Step 1: Create ScriptableObject Asset

1. In Unity Editor, right-click in Project window
2. Select `Create > PureDOTS > Culture > Cultural Doctrine`
3. Name the asset (e.g., "CorruptSpiritualist", "ZealotPaladin")

### Step 2: Configure Doctrine Properties

Select the asset and configure properties in Inspector:

#### Archetype Name
- **Field**: `archetypeName`
- **Description**: Human-readable name for the archetype
- **Example**: "Corrupt Spiritualist", "Zealot Paladin", "Chaotic Marauder"

#### Soul Harvest Bias
- **Field**: `soulHarvestBias`
- **Range**: 0.0 - 2.0
- **Description**: Multiplier for converting enemy deaths to focus energy
- **Formula**: `AttackWeight += Doctrine.SoulHarvestBias * DeadEnemiesNearby`
- **Example**: 
  - `0.0` = No soul harvest effect
  - `1.0` = Standard conversion
  - `2.0` = Double conversion (Corrupt Spiritualist)

#### Holy Entity Morale Bonus
- **Field**: `holyEntityMoraleBonus`
- **Range**: 0.0 - 1.0
- **Description**: Morale bonus when near holy entities
- **Formula**: `Morale += Doctrine.HolyEntityMoraleBonus * ProximityFactor`
- **Example**:
  - `0.0` = No holy entity bonus
  - `0.5` = Moderate bonus (Zealot Paladin)
  - `1.0` = Maximum bonus

#### Deviation Multiplier
- **Field**: `deviationMultiplier`
- **Range**: 0.0 - 2.0
- **Description**: Multiplier for random formation deviation
- **Formula**: `AppliedChaos = BehaviorProfile.Chaos * Doctrine.DeviationMultiplier`
- **Example**:
  - `0.5` = Less deviation (disciplined formations)
  - `1.0` = Standard deviation
  - `2.0` = Double deviation (Chaotic Marauder)

#### Ignore Morale Decay on Grudge
- **Field**: `ignoreMoraleDecayOnGrudge`
- **Type**: Boolean
- **Description**: If true, formation ignores morale loss when fighting grudge targets
- **Example**: `true` for Dwarven Captain archetype

#### Dead Enemy Attack Weight Bonus
- **Field**: `deadEnemyAttackWeightBonus`
- **Range**: 0.0 - 1.0
- **Description**: Attack weight modifier based on dead enemies nearby
- **Formula**: `AttackWeight += Doctrine.DeadEnemyAttackWeightBonus * DeadEnemiesNearby`
- **Example**:
  - `0.0` = No bonus from dead enemies
  - `0.5` = Moderate bonus
  - `1.0` = Maximum bonus

## Example Archetypes

### Corrupt Spiritualist

```csharp
archetypeName = "Corrupt Spiritualist"
soulHarvestBias = 1.5f
holyEntityMoraleBonus = 0.0f
deviationMultiplier = 0.8f
ignoreMoraleDecayOnGrudge = false
deadEnemyAttackWeightBonus = 0.3f
```

**Behavior**: Converts enemy deaths to focus energy. Prioritizes soul-harvest actions. Moderate attack bonus from dead enemies.

### Zealot Paladin

```csharp
archetypeName = "Zealot Paladin"
soulHarvestBias = 0.0f
holyEntityMoraleBonus = 0.7f
deviationMultiplier = 0.6f
ignoreMoraleDecayOnGrudge = false
deadEnemyAttackWeightBonus = 0.0f
```

**Behavior**: Gains morale when near holy entities. Low deviation (disciplined). No soul harvest.

### Chaotic Marauder

```csharp
archetypeName = "Chaotic Marauder"
soulHarvestBias = 0.2f
holyEntityMoraleBonus = 0.0f
deviationMultiplier = 2.0f
ignoreMoraleDecayOnGrudge = false
deadEnemyAttackWeightBonus = 0.1f
```

**Behavior**: High random formation deviation. Less disciplined, more chaotic movement.

### Dwarven Captain

```csharp
archetypeName = "Dwarven Captain"
soulHarvestBias = 0.0f
holyEntityMoraleBonus = 0.0f
deviationMultiplier = 0.7f
ignoreMoraleDecayOnGrudge = true
deadEnemyAttackWeightBonus = 0.2f
```

**Behavior**: Ignores morale decay when fighting grudge targets. Disciplined formation. Moderate attack bonus from dead enemies.

## Applying Doctrine to Formations

### Method 1: Via Authoring Component

1. Create GameObject in scene
2. Add `CulturalDoctrineAuthoring` component
3. Assign `CulturalDoctrineAsset` to `doctrineAsset` field
4. Baker automatically creates `CulturalDoctrineReference` component

### Method 2: Runtime Assignment

```csharp
// Get doctrine reference from entity with CulturalDoctrineAuthoring
if (SystemAPI.HasComponent<CulturalDoctrineReference>(doctrineEntity))
{
    var doctrineRef = SystemAPI.GetComponent<CulturalDoctrineReference>(doctrineEntity);
    
    // Apply to formation
    ecb.AddComponent(formationEntity, doctrineRef);
}
```

## Reading Doctrine at Runtime

```csharp
// In Burst-compiled system
if (SystemAPI.HasComponent<CulturalDoctrineReference>(formationEntity))
{
    var doctrineRef = SystemAPI.GetComponent<CulturalDoctrineReference>(formationEntity);
    
    if (doctrineRef.Doctrine.IsCreated)
    {
        ref var doctrine = ref doctrineRef.Doctrine.Value;
        
        // Access doctrine properties
        var soulHarvestBias = doctrine.SoulHarvestBias;
        var holyBonus = doctrine.HolyEntityMoraleBonus;
        // etc.
    }
}
```

## Doctrine Effects in Systems

### CulturalDoctrineSystem

`CulturalDoctrineSystem` automatically applies doctrine effects:
- Soul harvest bias → modifies attack weights
- Holy entity proximity → modifies morale
- Deviation multiplier → affects `BehaviorProfile.Chaos` application
- Grudge condition → modifies morale decay calculations

### Custom Integration

To integrate doctrine effects in custom systems:

```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    foreach (var (formationEntity, doctrineRef, groupMorale) in SystemAPI
                 .Query<Entity, RefRO<CulturalDoctrineReference>, RefRW<GroupMorale>>())
    {
        if (doctrineRef.ValueRO.Doctrine.IsCreated)
        {
            ref var doctrine = ref doctrineRef.ValueRO.Doctrine.Value;
            
            // Apply custom logic based on doctrine
            var morale = groupMorale.ValueRO;
            var newMorale = morale.CurrentMorale;
            
            // Example: Apply soul harvest bonus
            newMorale += doctrine.SoulHarvestBias * deadEnemiesCount * 0.01f;
            newMorale = math.clamp(newMorale, 0f, 1f);
            
            groupMorale.ValueRW = new GroupMorale
            {
                CurrentMorale = newMorale,
                // ... other fields
            };
        }
    }
}
```

## Best Practices

1. **Naming**: Use descriptive archetype names (e.g., "Corrupt Spiritualist" not "Doctrine1")
2. **Balancing**: Test doctrine values in gameplay scenarios
3. **Reusability**: Create doctrine assets that can be shared across multiple formations
4. **Documentation**: Document expected behavior in archetype name or comments
5. **Performance**: Doctrines are BlobAssets (zero GC), safe to read frequently

## Troubleshooting

### Doctrine Not Applying

- Verify `CulturalDoctrineReference` component exists on formation entity
- Check `Doctrine.IsCreated` before accessing blob
- Ensure `CulturalDoctrineSystem` is enabled and running

### Effects Not Visible

- Check doctrine property values are non-zero
- Verify systems are reading doctrine (add debug logs)
- Ensure formation has required components (`GroupMorale`, `BandStats`, etc.)

### Blob Building Errors

- Verify `CulturalDoctrineAsset` is assigned in `CulturalDoctrineAuthoring`
- Check asset properties are within valid ranges
- Ensure baker is running (check Entities Hierarchy for `CulturalDoctrineReference`)

## See Also

- `Docs/Guides/TacticalAI_Guide.md` - Tactical AI usage guide
- `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/Culture/CulturalDoctrine.cs` - Doctrine blob structure
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Culture/CulturalDoctrineSystem.cs` - System implementation

