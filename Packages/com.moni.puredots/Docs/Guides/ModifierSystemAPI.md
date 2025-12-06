# Modifier System API Reference

**Last Updated**: 2025-01-27  
**Purpose**: Quick reference for modifier system APIs and components.

---

## Core Components

### ModifierInstance
```csharp
public struct ModifierInstance : IBufferElementData
{
    public ushort ModifierId;  // Catalog index (not string!)
    public float Value;        // Modifier value (+% or absolute)
    public short Duration;     // Ticks remaining (-1 = permanent)
}
```

**Usage**: Stored in `DynamicBuffer<ModifierInstance>` on entities with active modifiers.

---

### ModifierCatalogRef
```csharp
public struct ModifierCatalogRef : IComponentData
{
    public BlobAssetReference<ModifierCatalogBlob> Blob;
}
```

**Usage**: Singleton component referencing the modifier catalog blob asset. Created by `ModifierCatalogAuthoring` baker.

---

### ModifierCategoryAccumulator
```csharp
public struct ModifierCategoryAccumulator : IComponentData
{
    public float EconomicAdd;
    public float EconomicMul;
    public float MilitaryAdd;
    public float MilitaryMul;
    public float EnvironmentAdd;
    public float EnvironmentMul;
    public uint LastUpdateTick;
}
```

**Usage**: Aggregated modifier sums per category. Read by gameplay systems to apply modifiers to stats.

**Formula**: `finalValue = baseValue * (1 + Mul) + Add`

---

### ApplyModifierEvent
```csharp
public struct ApplyModifierEvent : IBufferElementData
{
    public Entity Target;      // Entity to apply modifier to
    public ushort ModifierId;  // Catalog index
    public float Value;         // Override value (0 = use BaseValue)
    public short Duration;      // Ticks (-1 = permanent)
}
```

**Usage**: Add to `ModifierEventCoordinator` entity's buffer to apply modifiers.

---

## System APIs

### Applying Modifiers

```csharp
// Get event coordinator
var coordinatorEntity = SystemAPI.GetSingletonEntity<ModifierEventCoordinator>();
var events = SystemAPI.GetBuffer<ApplyModifierEvent>(coordinatorEntity);

// Add modifier event
events.Add(new ApplyModifierEvent
{
    Target = targetEntity,
    ModifierId = modifierId,
    Value = 0f, // Use BaseValue
    Duration = 300 // Ticks
});
```

### Reading Modifiers

```csharp
// Read category accumulator
var accumulator = SystemAPI.GetComponent<ModifierCategoryAccumulator>(entity);

// Apply to base stat
float finalValue = baseValue * (1f + accumulator.MilitaryMul) + accumulator.MilitaryAdd;

// Read individual modifiers (for UI/debugging)
var modifiers = SystemAPI.GetBuffer<ModifierInstance>(entity);
for (int i = 0; i < modifiers.Length; i++)
{
    var modifier = modifiers[i];
    // Access modifier.ModifierId, modifier.Value, modifier.Duration
}
```

### Removing Modifiers

```csharp
// Manual removal
var modifiers = SystemAPI.GetBuffer<ModifierInstance>(entity);
for (int i = modifiers.Length - 1; i >= 0; i--)
{
    if (modifiers[i].ModifierId == targetId)
    {
        modifiers.RemoveAtSwapBack(i);
    }
}
SystemAPI.SetComponent(entity, new ModifierDirtyTag());
```

---

## Modifier Operations

### Add Operation
```csharp
// ModifierOperation.Add
finalValue = baseValue + modifierValue
```

### Multiply Operation
```csharp
// ModifierOperation.Multiply
finalValue = baseValue * (1 + modifierValue)
// Example: modifierValue = 0.2 → +20% multiplier
```

### Override Operation
```csharp
// ModifierOperation.Override
finalValue = modifierValue
// Replaces base value entirely
```

---

## Modifier Categories

### Economy (0)
- Income modifiers
- Upkeep modifiers
- Production rate modifiers

### Military (1)
- Morale modifiers
- Damage modifiers
- Defense modifiers

### Environment (2)
- Temperature modifiers
- Fertility modifiers
- Weather modifiers

---

## Constants

### Modifier IDs
```csharp
// Define constants for modifier IDs (recommended)
public static class ModifierIds
{
    public const ushort SpeedBoost = 0;
    public const ushort MoraleBonus = 1;
    public const ushort ProductionBoost = 2;
    // ... etc
}
```

### Duration Constants
```csharp
public const short Permanent = -1;
public const short OneSecond = 60; // At 60Hz
public const short OneMinute = 3600; // At 60Hz
```

---

## Query Patterns

### Entities with Active Modifiers
```csharp
foreach (var (entity, modifiers) in SystemAPI.Query<Entity, DynamicBuffer<ModifierInstance>>())
{
    // Process entities with modifiers
}
```

### Entities Needing Recomputation
```csharp
foreach (var (entity, accumulator, modifiers) in SystemAPI.Query<Entity, ModifierCategoryAccumulator, DynamicBuffer<ModifierInstance>>()
    .WithAll<ModifierDirtyTag>())
{
    // Process dirty entities
}
```

### Entities by Category
```csharp
// Query entities with economic modifiers
foreach (var (entity, accumulator) in SystemAPI.Query<Entity, ModifierCategoryAccumulator>())
{
    if (accumulator.EconomicMul != 0f || accumulator.EconomicAdd != 0f)
    {
        // Has economic modifiers
    }
}
```

---

## Error Handling

### Invalid Modifier ID
```csharp
if (modifierId >= catalog.Modifiers.Length)
{
    // Invalid modifier ID - skip or log error
    return;
}
```

### Missing Catalog
```csharp
if (!SystemAPI.TryGetSingleton<ModifierCatalogRef>(out var catalogRef) ||
    !catalogRef.Blob.IsCreated)
{
    // Catalog not loaded - skip modifier operations
    return;
}
```

### Missing Coordinator
```csharp
if (!SystemAPI.HasSingleton<ModifierEventCoordinator>())
{
    // Coordinator not initialized - create it or skip
    return;
}
```

---

## Performance Best Practices

1. **Batch Events**: Add multiple `ApplyModifierEvent` entries before processing
2. **Use Accumulators**: Read from `ModifierCategoryAccumulator` instead of iterating modifiers
3. **Minimize Dirty Tags**: Only mark entities dirty when modifiers change
4. **Prefer Categories**: Use category aggregation over individual modifier lookups

---

## See Also

- `ModifierSystemGuide.md` - Comprehensive usage guide
- `high-performance_modifier_system_eda77a61.plan.md` - Implementation plan

