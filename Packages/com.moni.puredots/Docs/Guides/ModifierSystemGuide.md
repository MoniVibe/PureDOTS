# Modifier System Usage Guide

**Last Updated**: 2025-01-27  
**Purpose**: Guide for agents and developers on how to interface with and use the High-Performance Modifier System in PureDOTS.

---

## Overview

The Modifier System provides a scalable, event-driven way to apply, track, and aggregate modifiers (buffs/debuffs) across millions of entities without the O(n²) evaluation costs that plagued Paradox-style games. It uses numeric ID indexing, hot/cold path separation, and category aggregation for optimal performance.

---

## Quick Start

### 1. Setup Modifier Catalog

Create a `ModifierCatalog` ScriptableObject asset:

```csharp
// In Unity Editor: Create > PureDOTS > Modifier Catalog
// Or programmatically:
var catalog = ScriptableObject.CreateInstance<ModifierCatalog>();
catalog.modifiers = new List<ModifierDefinition>
{
    new ModifierDefinition
    {
        name = "Morale Boost",
        operation = ModifierOperation.Add,
        baseValue = 10f,
        category = ModifierCategory.Military,
        durationScale = 1f
    }
};
```

### 2. Author Catalog in Scene

Add `ModifierCatalogAuthoring` component to a GameObject in your bootstrap SubScene:

```csharp
// The baker automatically converts the ScriptableObject to a blob asset
// and creates a ModifierCatalogRef singleton
```

### 3. Apply Modifiers

Use the event-driven system to apply modifiers:

```csharp
// Get the event coordinator entity
var coordinatorQuery = SystemAPI.QueryBuilder()
    .WithAll<ModifierEventCoordinator, DynamicBuffer<ApplyModifierEvent>>()
    .Build();
var coordinatorEntity = coordinatorQuery.GetSingletonEntity();
var events = SystemAPI.GetBuffer<ApplyModifierEvent>(coordinatorEntity);

// Add modifier application event
events.Add(new ApplyModifierEvent
{
    Target = targetEntity,
    ModifierId = 0, // Index into catalog
    Value = 0f, // 0 = use BaseValue from catalog
    Duration = 300 // Ticks (-1 = permanent)
});
```

---

## Core Concepts

### Modifier IDs

**CRITICAL**: All modifier lookups use `ushort` indices into the catalog, NOT strings. This is Burst-safe and avoids string comparison overhead.

- Modifier ID 0 = first modifier in catalog
- Modifier ID 1 = second modifier in catalog
- etc.

### Event-Driven Application

Modifiers are applied via `ApplyModifierEvent` buffer on the `ModifierEventCoordinator` singleton entity. This allows:
- Batched processing (O(n) total modifiers, not per entity)
- Deferred application (events processed before gameplay systems)
- No per-frame allocations

### Hot/Cold Path Separation

- **Hot Path** (60Hz): Applies active modifiers to numeric stats
- **Cold Path** (0.2-1Hz): Expires modifiers, cleans up, pools instances

Only entities with `ModifierDirtyTag` are processed in hot path.

### Category Aggregation

Modifiers are aggregated by category:
- **Economy**: Income, upkeep modifiers
- **Military**: Morale, damage modifiers  
- **Environment**: Temperature, fertility modifiers

Pre-aggregated sums avoid cascading re-evaluations.

---

## API Reference

### Applying Modifiers

#### Method 1: Event Buffer (Recommended)

```csharp
// In any system that needs to apply modifiers
var coordinatorEntity = SystemAPI.GetSingletonEntity<ModifierEventCoordinator>();
var events = SystemAPI.GetBuffer<ApplyModifierEvent>(coordinatorEntity);

events.Add(new ApplyModifierEvent
{
    Target = entity,
    ModifierId = modifierId, // ushort index from catalog
    Value = overrideValue, // 0f = use BaseValue from catalog
    Duration = ticks // -1 = permanent
});
```

#### Method 2: Direct Buffer Access (Advanced)

```csharp
// Only if you need immediate application (not recommended)
var modifiers = SystemAPI.GetBuffer<ModifierInstance>(entity);
modifiers.Add(new ModifierInstance
{
    ModifierId = modifierId,
    Value = value,
    Duration = duration
});

// Mark as dirty for hot path recomputation
SystemAPI.SetComponent(entity, new ModifierDirtyTag());
```

### Reading Modifier Results

#### Category Accumulators

```csharp
// Read aggregated modifier sums
var accumulator = SystemAPI.GetComponent<ModifierCategoryAccumulator>(entity);

// Apply to base stats
float finalIncome = baseIncome * (1f + accumulator.EconomicMul) + accumulator.EconomicAdd;
float finalMorale = baseMorale * (1f + accumulator.MilitaryMul) + accumulator.MilitaryAdd;
```

#### Individual Modifier Buffers

```csharp
// Read individual modifiers (less common, for UI/debugging)
var modifiers = SystemAPI.GetBuffer<ModifierInstance>(entity);
for (int i = 0; i < modifiers.Length; i++)
{
    var modifier = modifiers[i];
    // Access modifier.ModifierId, modifier.Value, modifier.Duration
}
```

### Removing Modifiers

#### Method 1: Let Expiry System Handle (Recommended)

Modifiers automatically expire when `Duration` reaches 0. The `ModifierExpirySystem` removes them.

#### Method 2: Manual Removal

```csharp
var modifiers = SystemAPI.GetBuffer<ModifierInstance>(entity);
for (int i = modifiers.Length - 1; i >= 0; i--)
{
    if (modifiers[i].ModifierId == targetModifierId)
    {
        modifiers.RemoveAtSwapBack(i);
    }
}
SystemAPI.SetComponent(entity, new ModifierDirtyTag());
```

---

## Integration Patterns

### With Existing Buff System

The modifier system is designed to work alongside the existing `BuffApplicationSystem`:

```csharp
// Option 1: Keep using buff system (backward compatible)
// BuffApplicationSystem continues to work as before

// Option 2: Route buffs through modifier system
// Convert buff IDs to modifier IDs and emit ApplyModifierEvent
// ModifierHotPathSystem handles aggregation more efficiently
```

See integration comments in `BuffApplicationSystem.cs` and `BuffStatAggregationSystem.cs`.

### With Gameplay Systems

```csharp
// Example: Apply morale modifier when villager joins group
public partial struct VillagerGroupJoinSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var coordinatorEntity = SystemAPI.GetSingletonEntity<ModifierEventCoordinator>();
        var events = SystemAPI.GetBuffer<ApplyModifierEvent>(coordinatorEntity);

        foreach (var (villager, group) in SystemAPI.Query<VillagerId, GroupMember>())
        {
            // Apply group morale bonus
            events.Add(new ApplyModifierEvent
            {
                Target = villager,
                ModifierId = ModifierIds.GroupMoraleBonus, // Predefined constant
                Value = 0f, // Use BaseValue from catalog
                Duration = -1 // Permanent while in group
            });
        }
    }
}
```

### Reading Modifiers in Other Systems

```csharp
// Example: Use modifier results in combat calculation
public partial struct CombatDamageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (attacker, accumulator) in SystemAPI.Query<Entity, ModifierCategoryAccumulator>())
        {
            // Apply military modifiers to damage
            float baseDamage = 100f;
            float finalDamage = baseDamage * (1f + accumulator.MilitaryMul) + accumulator.MilitaryAdd;
            
            // Use finalDamage in combat calculation
        }
    }
}
```

---

## Authoring Modifier Catalogs

### Creating Modifier Definitions

```csharp
// In ModifierCatalog ScriptableObject
var modifier = new ModifierDefinition
{
    name = "Production Boost",
    description = "Increases production rate by 20%",
    operation = ModifierOperation.Multiply,
    baseValue = 0.2f, // +20% multiplier
    category = ModifierCategory.Economy,
    durationScale = 1f,
    dependencies = new List<ushort>() // Modifier IDs this depends on
};
```

### Modifier Operations

- **Add**: `finalValue = baseValue + modifierValue`
- **Multiply**: `finalValue = baseValue * (1 + modifierValue)`
- **Override**: `finalValue = modifierValue` (replaces base value)

### Modifier Categories

- **Economy** (0): Income, upkeep, production modifiers
- **Military** (1): Morale, damage, defense modifiers
- **Environment** (2): Temperature, fertility, weather modifiers

### Dependency Chains

Modifiers can depend on other modifiers. Dependencies are flattened at load time via topological sort:

```csharp
// Modifier A depends on Modifier B
modifierA.dependencies.Add(modifierBId);

// The system ensures B is evaluated before A
```

---

## Performance Considerations

### When to Use Modifier System

✅ **Use for**:
- Large-scale simulations (1000+ entities)
- Frequently changing modifiers
- Category-based aggregation needs
- Performance-critical modifier evaluation

❌ **Don't use for**:
- Simple, static modifiers (use direct component values)
- One-off effects (use direct stat modification)
- Very small entity counts (< 100)

### Performance Targets

- **Hot path**: < 0.1ms per 10k entities with modifiers
- **Cold path**: < 1ms per 100k entities (throttled to 0.2-1Hz)
- **Memory**: < 100 bytes per entity with modifiers
- **Churn**: < 2% modifiers expired/applied per second

### Optimization Tips

1. **Batch Modifier Applications**: Add multiple `ApplyModifierEvent` entries before processing
2. **Use Categories**: Prefer category aggregation over individual modifier lookups
3. **Minimize Dirty Tags**: Only mark entities dirty when modifiers actually change
4. **LOD Culling**: Distant entities use statistical aggregates instead of individual modifiers

---

## System Execution Order

The modifier systems run in this order:

1. `ModifierBootstrapSystem` (InitializationSystemGroup) - Creates coordinator entity
2. `ModifierEventApplicationSystem` (EventSystemGroup) - Processes application events
3. `ModifierDependencyResolverSystem` (ModifierSystemGroup, OrderFirst) - Resolves dependencies
4. `ModifierHotPathSystem` (ModifierHotPathGroup) - Applies active modifiers (60Hz)
5. `ModifierSIMDSystem` (ModifierHotPathGroup) - SIMD-optimized processing
6. `ModifierAggregationSystem` (GameplaySystemGroup) - Aggregates by category
7. `ModifierHierarchySystem` (GameplaySystemGroup) - Parent-child propagation
8. `ModifierExpirySystem` (ModifierColdPathGroup) - Expires modifiers (0.2-1Hz)
9. `ModifierPoolSystem` (ModifierColdPathGroup) - Recycles instances
10. `ModifierLODSystem` (ModifierColdPathGroup) - LOD culling
11. `ModifierProfilerSystem` (LateSimulationSystemGroup) - Performance profiling

---

## Common Use Cases

### Example 1: Apply Temporary Buff

```csharp
// Apply 30-second speed boost
var events = SystemAPI.GetBuffer<ApplyModifierEvent>(coordinatorEntity);
events.Add(new ApplyModifierEvent
{
    Target = playerEntity,
    ModifierId = ModifierIds.SpeedBoost,
    Value = 0f, // Use BaseValue from catalog
    Duration = 1800 // 30 seconds at 60Hz
});
```

### Example 2: Read Modifier Results

```csharp
// Calculate final movement speed
var accumulator = SystemAPI.GetComponent<ModifierCategoryAccumulator>(entity);
float baseSpeed = 5f;
float finalSpeed = baseSpeed * (1f + accumulator.MilitaryMul) + accumulator.MilitaryAdd;
```

### Example 3: Hierarchical Modifiers

```csharp
// Village modifier propagates to all villagers
// 1. Apply modifier to village entity
events.Add(new ApplyModifierEvent
{
    Target = villageEntity,
    ModifierId = ModifierIds.VillageProductionBonus,
    Duration = -1
});

// 2. ModifierHierarchySystem automatically propagates to children
// 3. Villagers inherit modifier via ModifierAggregator
```

---

## Troubleshooting

### Modifiers Not Applying

1. **Check catalog exists**: Verify `ModifierCatalogRef` singleton exists
2. **Check coordinator exists**: Verify `ModifierEventCoordinator` entity exists
3. **Check modifier ID**: Ensure modifier ID is valid (< catalog.Modifiers.Length)
4. **Check dirty tag**: Ensure entity has `ModifierDirtyTag` after manual buffer modification

### Modifiers Not Expiring

1. **Check duration**: Verify `Duration` is not -1 (permanent)
2. **Check expiry system**: Verify `ModifierExpirySystem` is enabled
3. **Check cold path throttling**: Expiry runs at 0.2-1Hz, not every tick

### Performance Issues

1. **Check churn rate**: Use `ModifierProfilerSystem` to monitor churn
2. **Reduce dirty tags**: Only mark entities dirty when necessary
3. **Use categories**: Prefer category aggregation over individual lookups
4. **Enable LOD**: Use `ModifierLODSystem` for distant entities

---

## Migration from Buff System

To migrate from the existing buff system:

1. **Create modifier catalog**: Convert buff definitions to modifier definitions
2. **Map buff IDs to modifier IDs**: Create a mapping table
3. **Update application code**: Replace `BuffApplicationRequest` with `ApplyModifierEvent`
4. **Update reading code**: Replace `BuffStatCache` with `ModifierCategoryAccumulator`
5. **Test thoroughly**: Verify modifier behavior matches buff behavior

See integration comments in buff systems for detailed migration path.

---

## Reference

- **Components**: `PureDOTS.Runtime.Modifiers` namespace
- **Systems**: `PureDOTS.Systems.Modifiers` namespace
- **Config**: `PureDOTS.Config.ModifierCatalog`
- **Authoring**: `PureDOTS.Authoring.ModifierCatalogAuthoring`

For implementation details, see the plan document: `high-performance_modifier_system_eda77a61.plan.md`

