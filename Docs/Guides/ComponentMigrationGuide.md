# Component Migration Guide

This guide explains how to migrate existing components to meet PureDOTS performance scale guidelines.

## Overview

For scale (100k+ entities), components should be optimized for:
- **Cache efficiency**: Hot data stays small and contiguous
- **Memory footprint**: Use smallest appropriate data types
- **Hot/Cold separation**: Move rarely-used data to companion entities

## Migration Patterns

### Pattern 1: Hot/Cold Split

**Before** (mixed hot/cold data):
```csharp
public struct VillagerData : IComponentData
{
    // Hot (updated every tick)
    public float3 Position;
    public float3 Velocity;
    public float Hunger;
    
    // Cold (updated rarely)
    public FixedString64Bytes Name;      // 64 bytes!
    public FixedString128Bytes Biography; // 128 bytes!
    public uint BirthTick;
}
// Total: ~220 bytes per entity
```

**After** (separated):
```csharp
// Hot component (on main entity) - ~36 bytes
public struct VillagerHot : IComponentData
{
    public float3 Position;
    public float3 Velocity;
    public float Hunger;
    public Entity ColdDataRef;
}

// Cold component (on companion entity)
public struct VillagerColdData : IComponentData
{
    public FixedString64Bytes Name;
    public FixedString128Bytes Biography;
    public uint BirthTick;
}
```

**Migration Steps**:
1. Create companion entity archetype
2. Move cold fields to companion component
3. Add Entity reference field to hot component
4. Update systems to resolve companion when needed
5. Update bakers to create companion entities

### Pattern 2: FixedString to Index

**Before** (string in hot component):
```csharp
public struct VillagerBelief : IComponentData
{
    public FixedString64Bytes PrimaryDeityId;  // 64 bytes!
    public float Faith;
    public float WorshipProgress;
}
// Total: ~72 bytes
```

**After** (index + catalog):
```csharp
// Hot component - ~10 bytes
public struct VillagerBeliefOptimized : IComponentData
{
    public byte DeityIndex;  // Index into catalog
    public float Faith;
    public float WorshipProgress;
}

// Catalog (blob asset, shared by all entities)
public struct DeityCatalogBlob
{
    public BlobArray<FixedString64Bytes> DeityIds;
}

public struct DeityCatalog : IComponentData
{
    public BlobAssetReference<DeityCatalogBlob> Catalog;
}
```

**Migration Steps**:
1. Create catalog blob asset with all possible string values
2. Replace FixedString field with index (byte/ushort)
3. Create catalog authoring and baker
4. Update systems to lookup string from catalog when needed
5. Update bakers to assign indices

### Pattern 3: Packed Flags

**Before** (multiple tag components):
```csharp
// 5 separate components = 5 × 16 bytes overhead = 80 bytes
public struct VillagerSelectedTag : IComponentData { }
public struct VillagerHighlightedTag : IComponentData { }
public struct VillagerInCombatTag : IComponentData { }
public struct VillagerCarryingTag : IComponentData { }
public struct VillagerDeadTag : IComponentData { }
```

**After** (packed flags):
```csharp
// Single component = 2 bytes
public struct VillagerFlags : IComponentData
{
    private byte _flags1;
    private byte _flags2;

    public bool IsSelected
    {
        get => (_flags1 & 0x01) != 0;
        set => _flags1 = (byte)(value ? _flags1 | 0x01 : _flags1 & ~0x01);
    }
    // ... other flags
}
```

**Migration Steps**:
1. Create packed flags component
2. Add property accessors for each flag
3. Update systems to use flags instead of tags
4. Remove old tag components
5. Update bakers

### Pattern 4: Buffer to Companion

**Before** (buffer on hot entity):
```csharp
// Large buffer on hot entity
public struct VillagerInventoryItem : IBufferElementData
{
    public FixedString64Bytes ResourceTypeId;
    public float Amount;
    public float MaxCapacity;
}
// DynamicBuffer<VillagerInventoryItem> on villager entity
```

**After** (buffer on companion):
```csharp
// Reference on hot entity - 4 bytes
public struct VillagerInventoryRef : IComponentData
{
    public Entity CompanionEntity;
}

// Optimized buffer element on companion
public struct VillagerInventoryItem : IBufferElementData
{
    public ushort ResourceTypeIndex;  // Index instead of string
    public float Amount;
    public float MaxCapacity;
}
```

**Migration Steps**:
1. Create companion entity for inventory
2. Add reference component to hot entity
3. Move buffer to companion entity
4. Update systems to resolve companion
5. Update bakers

## Data Type Optimization

### Numeric Types

| Range | Type | Size |
|-------|------|------|
| 0-255 | `byte` | 1 byte |
| 0-65535 | `ushort` | 2 bytes |
| -128 to 127 | `sbyte` | 1 byte |
| -32768 to 32767 | `short` | 2 bytes |
| Full range | `int` | 4 bytes |
| Precision needed | `float` | 4 bytes |

### Common Conversions

| Original | Optimized | Savings |
|----------|-----------|---------|
| `float Health` (0-100) | `byte HealthPercent` | 3 bytes |
| `int FactionId` (0-255) | `byte FactionId` | 3 bytes |
| `FixedString64Bytes Id` | `ushort IdIndex` | 62 bytes |
| `Entity[] Targets` | `DynamicBuffer` on companion | Variable |

## System Updates

### Resolving Companion Data

```csharp
// Before: direct access
var name = villager.Name;

// After: resolve companion
if (SystemAPI.HasComponent<VillagerColdData>(villager.ColdDataRef))
{
    var coldData = SystemAPI.GetComponent<VillagerColdData>(villager.ColdDataRef);
    var name = coldData.Name;
}
```

### Catalog Lookup

```csharp
// Before: direct string
var deityId = belief.PrimaryDeityId;

// After: catalog lookup
ref var catalog = ref SystemAPI.GetSingleton<DeityCatalog>().Catalog.Value;
var deityId = catalog.DeityIds[belief.DeityIndex];
```

## Baker Updates

### Creating Companion Entities

```csharp
public class VillagerBaker : Baker<VillagerAuthoring>
{
    public override void Bake(VillagerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        
        // Create companion entity for cold data
        var companion = CreateAdditionalEntity(TransformUsageFlags.None);
        
        // Add cold data to companion
        AddComponent(companion, new VillagerColdData
        {
            Name = authoring.Name,
            Biography = authoring.Biography,
            BirthTick = 0
        });
        
        // Add reference to main entity
        AddComponent(entity, new VillagerColdDataRef
        {
            CompanionEntity = companion
        });
        
        // Add hot data to main entity
        AddComponent(entity, new VillagerHot { /* ... */ });
    }
}
```

## Validation Checklist

Before migration:
- [ ] Measure current component sizes
- [ ] Identify hot vs cold data
- [ ] Plan companion entity structure

During migration:
- [ ] Create optimized components
- [ ] Update bakers
- [ ] Update systems
- [ ] Add backward compatibility if needed

After migration:
- [ ] Run scale tests
- [ ] Verify component sizes
- [ ] Check memory usage
- [ ] Validate performance budgets

## See Also

- `Docs/PERFORMANCE_PLAN.md` - Overall performance strategy
- `Docs/Guides/PerformanceIntegrationRoadmap.md` - Integration roadmap with real archetype examples
- `Docs/FoundationGuidelines.md` - Component size guidelines
- `Docs/QA/PerformanceBudgets.md` - Performance targets

