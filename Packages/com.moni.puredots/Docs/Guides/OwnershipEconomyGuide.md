# Ownership & Economy Core - Integration Guide

**Last Updated**: 2025-01-27  
**Purpose**: Guide for agents implementing features that interact with the Ownership & Economy core

## Overview

The Ownership & Economy core provides a three-layer ECS architecture for economic simulation:
- **Body ECS** (60Hz): Physical resource extraction, production, logistics
- **Mind ECS** (1Hz): Investment decisions, risk assessment, trade negotiations
- **Aggregate ECS** (0.2Hz): Market equilibrium, empire wealth, tax collection

All systems run parallel to existing spatial, cognitive, and social worlds.

## Core Components

### Ownership Components

**Location**: `Runtime/Economy/Ownership/OwnershipComponents.cs`

```csharp
// Single owner asset
struct Ownership : IComponentData
{
    Entity Owner;           // Entity that owns this asset
    float Share;           // Ownership share [0..1]
    OwnershipRights Rights; // Bitmask: Manage, Trade, Tax, Use
}

// Multiple owners (joint ventures)
struct OwnershipBuffer : IBufferElementData
{
    Entity Owner;
    float Share;
    OwnershipRights Rights;
}

// Asset identification
struct AssetTag : IComponentData
{
    AssetType Type;        // Mine, Facility, Village, etc.
    ulong AssetId;         // Unique identifier
}
```

**Usage**: Attach `Ownership` or `OwnershipBuffer` to entities that can be owned. Use `AssetTag` to mark entities as economic assets.

### Financial Components

**Location**: `Runtime/Economy/Ownership/FinancialComponents.cs`

```csharp
// Financial ledger
struct Ledger : IComponentData
{
    float Cash;            // Current cash balance
    float Income;          // Income per period (calculated from Portfolio)
    float Expenses;        // Expenses per period
    uint LastUpdateTick;
}

// Portfolio of owned assets
struct Portfolio : IBufferElementData
{
    Entity Asset;          // Owned asset entity
    float OwnershipShare;  // Share [0..1]
    float ExpectedOutputValue; // Cached output value
}

// Dirty flag for optimization
struct FinancialState : IComponentData
{
    uint LastUpdateTick;
    bool DirtyFlag;        // True if needs recalculation
}
```

**Usage**: Attach `Ledger` to entities that participate in economic transactions. `Portfolio` tracks owned assets for income calculation.

### Legal Components

**Location**: `Runtime/Economy/Ownership/LegalComponents.cs`

```csharp
// Legal entity (corporation, empire, etc.)
struct LegalEntity : IComponentData
{
    Entity Founder;
    float Influence;       // [0..1] based on owned assets
    float TaxRate;         // [0..1] calculated from Influence
    float Treasury;        // Cash held by legal entity
    uint LastUpdateTick;
}

// Governing entity reference
struct GoverningEntity : IComponentData
{
    Entity LegalEntity;    // Reference to governing LegalEntity
}
```

**Usage**: Attach `LegalEntity` to organizations/empires. Attach `GoverningEntity` to assets to indicate jurisdiction for tax collection.

### Asset Specification

**Location**: `Runtime/Economy/Ownership/AssetSpec.cs`

```csharp
// Blob asset defining asset properties
struct AssetSpecBlob
{
    float CapitalCost;    // Initial cost to acquire/build
    float Upkeep;         // Maintenance cost per period
    float OutputRate;     // Production rate (units/second)
    FixedString64Bytes OutputType; // Resource type produced
    float WorkforceNeed;  // Workers required for full production
    AssetType Type;       // Asset category
}

// Catalog singleton
struct AssetSpecCatalog : IComponentData
{
    BlobAssetReference<AssetSpecCatalogBlob> Catalog;
}
```

**Usage**: Reference `AssetSpecCatalog` singleton to lookup asset specs by `AssetType` enum.

## System Architecture

### System Groups

**Location**: `Runtime/Systems/SystemGroups.cs`

```csharp
// Parent group
[UpdateInGroup(typeof(GameplaySystemGroup))]
[UpdateAfter(typeof(ResourceSystemGroup))]
EconomySystemGroup

// Body layer (60Hz) - physical production/logistics
[UpdateInGroup(typeof(EconomySystemGroup), OrderFirst = true)]
BodyEconomySystemGroup

// Mind layer (1Hz) - investment/strategic decisions
[UpdateInGroup(typeof(EconomySystemGroup))]
[UpdateAfter(typeof(BodyEconomySystemGroup))]
MindEconomySystemGroup

// Aggregate layer (0.2Hz) - market/macro-economy
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[UpdateBefore(typeof(HistorySystemGroup))]
AggregateEconomySystemGroup
```

### Body ECS Systems (60Hz)

**ProductionSystem** (`Runtime/Economy/Ownership/Systems/Body/ProductionSystem.cs`)
- Queries entities with `AssetTag` + `AssetSpecCatalog`
- Calculates production: `OutputRate * Efficiency * DeltaTime`
- Updates `ResourceStock` buffers

**Integration**: Add `AssetTag` and `ResourceStock` buffer to production entities. System reads `AssetSpecCatalog` singleton.

**MineExtractionSystem** (`Runtime/Economy/Ownership/Systems/Body/MineExtractionSystem.cs`)
- Processes mines (`AssetType.Mine`)
- Handles resource extraction based on workforce

**HaulingSystem** (`Runtime/Economy/Ownership/Systems/Body/HaulingSystem.cs`)
- Processes `TradeRoute` entities
- Updates route state (actual resource movement handled by LogisticsSystem)

### Mind ECS Systems (1Hz)

**LedgerUpdateSystem** (`Runtime/Economy/Ownership/Systems/Financial/LedgerUpdateSystem.cs`)
- Runs every 60 ticks (1Hz)
- Calculates: `Income = Σ(asset.OutputValue × OwnershipShare)`
- Updates: `Cash += Income - Expenses`
- Only recalculates when `FinancialState.DirtyFlag == true`

**PortfolioManagementSystem** (`Runtime/Economy/Ownership/Systems/Financial/PortfolioManagementSystem.cs`)
- Processes `PurchaseEvent` and `SaleEvent` buffers
- Updates `Portfolio` buffers
- Validates cash availability
- Marks `FinancialState.DirtyFlag = true` after changes

**InvestmentDecisionSystem** (`Runtime/Economy/Ownership/Systems/Mind/InvestmentDecisionSystem.cs`)
- Evaluates investment opportunities
- Enqueues `PurchaseEvent` when utility > threshold
- **Future**: Integrates with MindECS traits via AgentSyncBus

### Aggregate ECS Systems (0.2Hz)

**MarketEquilibriumSystem** (`Runtime/Economy/Ownership/Systems/Aggregate/MarketEquilibriumSystem.cs`)
- Runs every 300 ticks (0.2Hz)
- Calculates: `Price = Price + k * (Demand - Supply)`
- Updates `MarketCommodityData` buffers on `MarketCell` entities

**EmpireWealthSystem** (`Runtime/Economy/Ownership/Systems/Aggregate/EmpireWealthSystem.cs`)
- Aggregates `Ledger.Cash` across `LegalEntity` hierarchies
- Calculates empire-level wealth metrics

**TaxCollectionSystem** (`Runtime/Economy/Policies/TaxCollectionSystem.cs` - extended)
- Queries assets with `GoverningEntity`
- Calculates: `tax = Income × TaxRate × LoyaltyModifier`
- Deducts from asset `Ledger`, adds to `LegalEntity.Treasury`

## Integration Patterns

### Creating an Owned Asset

```csharp
// 1. Create entity with AssetTag
var assetEntity = entityManager.CreateEntity();
entityManager.AddComponent<AssetTag>(assetEntity, new AssetTag
{
    Type = AssetType.Mine,
    AssetId = GenerateUniqueId()
});

// 2. Add Ownership
entityManager.AddComponent<Ownership>(assetEntity, new Ownership
{
    Owner = ownerEntity,
    Share = 1.0f,
    Rights = OwnershipRights.Manage | OwnershipRights.Trade | OwnershipRights.Use
});

// 3. Add ResourceStock buffer for production
entityManager.AddBuffer<ResourceStock>(assetEntity);

// 4. Add Ledger if asset generates income
entityManager.AddComponent<Ledger>(assetEntity, new Ledger
{
    Cash = 0f,
    Income = 0f,
    Expenses = 0f
});

// 5. Add Portfolio entry to owner
var portfolioBuffer = entityManager.GetBuffer<Portfolio>(ownerEntity);
portfolioBuffer.Add(new Portfolio
{
    Asset = assetEntity,
    OwnershipShare = 1.0f,
    ExpectedOutputValue = 0f // Will be calculated by production systems
});
```

### Purchasing an Asset

```csharp
// 1. Create PurchaseEvent
var purchaseBuffer = entityManager.GetBuffer<PurchaseEvent>(buyerEntity);
purchaseBuffer.Add(new PurchaseEvent
{
    Buyer = buyerEntity,
    Asset = assetEntity,
    Share = 0.5f, // Purchase 50% share
    Price = 1000f,
    Tick = currentTick
});

// 2. PortfolioManagementSystem will:
//    - Validate buyer has sufficient cash
//    - Deduct cash from buyer's Ledger
//    - Update Ownership/OwnershipBuffer on asset
//    - Add/update Portfolio entry
//    - Mark FinancialState.DirtyFlag = true
```

### Setting Up Production

```csharp
// 1. Ensure AssetSpecCatalog exists (created by bootstrap)
var catalog = SystemAPI.GetSingleton<AssetSpecCatalog>();

// 2. ProductionSystem automatically:
//    - Queries entities with AssetTag + ResourceStock buffer
//    - Looks up AssetSpec by AssetTag.Type
//    - Calculates production from OutputRate
//    - Updates ResourceStock buffer with produced resources
```

### Tax Collection

```csharp
// 1. Create LegalEntity
var legalEntity = entityManager.CreateEntity();
entityManager.AddComponent<LegalEntity>(legalEntity, new LegalEntity
{
    Founder = founderEntity,
    Influence = 0f, // Calculated by LegalEntitySystem
    TaxRate = 0.1f, // 10% base rate
    Treasury = 0f
});

// 2. Attach GoverningEntity to assets
entityManager.AddComponent<GoverningEntity>(assetEntity, new GoverningEntity
{
    LegalEntity = legalEntity
});

// 3. TaxCollectionSystem automatically:
//    - Queries assets with GoverningEntity + Ledger
//    - Calculates tax = Income × TaxRate × LoyaltyModifier
//    - Deducts from asset Ledger, adds to LegalEntity.Treasury
```

## Authoring

### Asset Spec Authoring

**Location**: `Runtime/Economy/Ownership/Authoring/AssetSpecAuthoring.cs`

```csharp
// Create ScriptableObject asset
[CreateAssetMenu(menuName = "PureDOTS/Economy/Ownership/Asset Spec")]
public class AssetSpecAuthoring : ScriptableObject
{
    AssetType AssetType;
    float CapitalCost;
    float Upkeep;
    float OutputRate;
    string OutputType; // ResourceTypeId
    float WorkforceNeed;
}
```

**Usage**: Create asset spec assets in Unity Editor. Bootstrap system converts to `AssetSpecCatalog` blob.

### Ownership Authoring

**Location**: `Runtime/Economy/Ownership/Authoring/OwnershipAuthoring.cs`

```csharp
// MonoBehaviour component
public class OwnershipAuthoring : MonoBehaviour
{
    Entity OwnerEntity;
    float Share;
    OwnershipRights Rights;
}

// Baker converts to ECS component
public class OwnershipBaker : Baker<OwnershipAuthoring>
```

**Usage**: Attach `OwnershipAuthoring` to GameObjects in SubScenes. Baker converts to `Ownership` component.

## Performance Considerations

### Dirty Flags

Always mark `FinancialState.DirtyFlag = true` when:
- Ownership changes (PurchaseEvent/SaleEvent processed)
- Asset production changes
- Portfolio buffer modified

`LedgerUpdateSystem` only recalculates when dirty flag is set.

### Temporal Batching

- **Body ECS**: Runs every tick (60Hz) - keep logic simple
- **Mind ECS**: Runs every 60 ticks (1Hz) - can do more complex calculations
- **Aggregate ECS**: Runs every 300 ticks (0.2Hz) - expensive aggregations

### Chunk-Local Aggregation

For performance, aggregate income/production within spatial chunks when possible. Use `IJobEntityBatch` for SoA processing.

## Integration with Existing Systems

### Resource Systems

`ResourceStock` buffers integrate with existing `ResourceComponents`. Production systems write to `ResourceStock`, which can be consumed by resource gathering systems.

### Market Systems

`MarketCell` entities use `SpatialGridResidency` for spatial queries. `MarketCommodityData` buffers track supply/demand per commodity.

### MindECS Integration

**Future**: `InvestmentDecisionSystem` will read MindECS traits (Intelligence, Greed, Fear) via `AgentSyncBus`. Investment commands sent via `InvestmentCommand` component.

### Wealth Systems

`Portfolio` bridges to existing `AssetHolding` buffer in `Runtime/Individual/WealthComponents.cs`. Both track asset ownership but serve different purposes:
- `Portfolio`: Economic income calculation
- `AssetHolding`: Individual wealth tracking

## Common Patterns

### Checking Ownership

```csharp
if (SystemAPI.HasComponent<Ownership>(assetEntity))
{
    var ownership = SystemAPI.GetComponent<Ownership>(assetEntity);
    if (ownership.Owner == myEntity && (ownership.Rights & OwnershipRights.Manage) != 0)
    {
        // Can manage this asset
    }
}
```

### Calculating Income

```csharp
float totalIncome = 0f;
var portfolioBuffer = SystemAPI.GetBuffer<Portfolio>(entity);
for (int i = 0; i < portfolioBuffer.Length; i++)
{
    totalIncome += portfolioBuffer[i].ExpectedOutputValue * portfolioBuffer[i].OwnershipShare;
}
```

### Finding Assets by Type

```csharp
foreach (var (assetTag, entity) in SystemAPI.Query<RefRO<AssetTag>>().WithEntityAccess())
{
    if (assetTag.ValueRO.Type == AssetType.Mine)
    {
        // Process mine
    }
}
```

## Troubleshooting

### Income Not Updating

1. Check `FinancialState.DirtyFlag` is set to `true` after changes
2. Verify `Portfolio` buffer has correct `ExpectedOutputValue`
3. Ensure `LedgerUpdateSystem` is enabled (runs at 1Hz)

### Tax Not Collected

1. Verify asset has `GoverningEntity` component
2. Check `LegalEntity.TaxRate` > 0
3. Ensure asset `Ledger.Income` > 0
4. Verify `TaxCollectionSystem` is in `AggregateEconomySystemGroup`

### Production Not Working

1. Verify entity has `AssetTag` component
2. Check `AssetSpecCatalog` singleton exists
3. Ensure `ResourceStock` buffer exists on entity
4. Verify `AssetSpec.OutputRate` > 0

## See Also

- `Runtime/Economy/Ownership/` - Component definitions
- `Runtime/Economy/Ownership/Systems/` - System implementations
- `Runtime/Economy/Ownership/Authoring/` - Authoring assets
- `Docs/TruthSources/RuntimeLifecycle_TruthSource.md` - System ordering reference

