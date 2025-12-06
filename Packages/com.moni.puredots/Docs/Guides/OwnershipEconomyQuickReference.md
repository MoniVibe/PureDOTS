# Ownership & Economy Core - Quick Reference

Quick reference for common operations and patterns.

## Component Queries

```csharp
// Find all owned assets
SystemAPI.Query<RefRO<AssetTag>, RefRO<Ownership>>()

// Find assets owned by specific entity
SystemAPI.Query<RefRO<AssetTag>>()
    .WithAll<Ownership>()
    .Where((in Ownership ownership) => ownership.Owner == myEntity)

// Find entities with financial data
SystemAPI.Query<RefRO<Ledger>, DynamicBuffer<Portfolio>>()

// Find legal entities
SystemAPI.Query<RefRO<LegalEntity>>()

// Find assets under governance
SystemAPI.Query<RefRO<GoverningEntity>>()
```

## Common Operations

### Create Owned Asset
```csharp
var asset = entityManager.CreateEntity();
entityManager.AddComponent<AssetTag>(asset, new AssetTag { Type = AssetType.Mine, AssetId = id });
entityManager.AddComponent<Ownership>(asset, new Ownership { Owner = owner, Share = 1f });
entityManager.AddBuffer<ResourceStock>(asset);
entityManager.AddComponent<Ledger>(asset);
```

### Purchase Asset Share
```csharp
var purchaseBuffer = entityManager.GetBuffer<PurchaseEvent>(buyer);
purchaseBuffer.Add(new PurchaseEvent { Buyer = buyer, Asset = asset, Share = 0.5f, Price = 1000f });
// PortfolioManagementSystem processes automatically
```

### Check Ownership Rights
```csharp
if (ownership.Rights.HasFlag(OwnershipRights.Manage)) { /* can manage */ }
if (ownership.Rights.HasFlag(OwnershipRights.Trade)) { /* can trade */ }
if (ownership.Rights.HasFlag(OwnershipRights.Tax)) { /* can tax */ }
if (ownership.Rights.HasFlag(OwnershipRights.Use)) { /* can use */ }
```

### Calculate Total Wealth
```csharp
float wealth = ledger.Cash;
var portfolio = SystemAPI.GetBuffer<Portfolio>(entity);
for (int i = 0; i < portfolio.Length; i++)
    wealth += portfolio[i].ExpectedOutputValue * portfolio[i].OwnershipShare;
```

### Set Up Tax Collection
```csharp
// Create legal entity
entityManager.AddComponent<LegalEntity>(legalEntity, new LegalEntity { TaxRate = 0.1f });

// Attach to asset
entityManager.AddComponent<GoverningEntity>(asset, new GoverningEntity { LegalEntity = legalEntity });
// TaxCollectionSystem collects automatically
```

## System Tick Rates

- **BodyEconomySystemGroup**: 60Hz (every tick)
- **MindEconomySystemGroup**: 1Hz (every 60 ticks)
- **AggregateEconomySystemGroup**: 0.2Hz (every 300 ticks)

## Component Locations

- `OwnershipComponents.cs` - Ownership, AssetTag, OwnershipBuffer
- `FinancialComponents.cs` - Ledger, Portfolio, FinancialState
- `LegalComponents.cs` - LegalEntity, GoverningEntity
- `ProductionComponents.cs` - ResourceStock, TradeRoute
- `EventComponents.cs` - PurchaseEvent, SaleEvent, InvestmentCommand
- `MarketCellComponents.cs` - MarketCell, MarketCommodityData
- `AssetSpec.cs` - AssetSpecBlob, AssetSpecCatalog

## System Locations

- `Systems/Body/` - ProductionSystem, MineExtractionSystem, HaulingSystem
- `Systems/Financial/` - LedgerUpdateSystem, PortfolioManagementSystem
- `Systems/Mind/` - InvestmentDecisionSystem, RiskAssessmentSystem, TradeNegotiationSystem
- `Systems/Aggregate/` - MarketEquilibriumSystem, EmpireWealthSystem
- `Systems/Legal/` - LegalEntitySystem
- `Systems/Production/` - LogisticsSystem
- `Systems/Investment/` - InvestmentUtilitySystem
- `Systems/Behavior/` - EconomicBehaviorSystem

## Authoring Assets

- `Authoring/AssetSpecAuthoring.cs` - ScriptableObject for asset specs
- `Authoring/OwnershipAuthoring.cs` - MonoBehaviour + Baker for ownership

## Integration Points

- **Resource Systems**: `ResourceStock` buffers
- **Market Systems**: `MarketCell` + `SpatialGridResidency`
- **MindECS**: `InvestmentCommand` via `AgentSyncBus` (future)
- **Wealth Systems**: `Portfolio` ↔ `AssetHolding` bridge

## Performance Tips

1. Set `FinancialState.DirtyFlag = true` only when ownership/production changes
2. Use chunk-local aggregation for income calculations
3. Mind/Aggregate systems run at lower frequencies - use for expensive operations
4. Body systems run every tick - keep logic simple and fast

## Troubleshooting

**Income not updating?**
- Check `DirtyFlag` is set
- Verify `Portfolio.ExpectedOutputValue` > 0
- Ensure `LedgerUpdateSystem` enabled (1Hz)

**Tax not collected?**
- Verify `GoverningEntity` on asset
- Check `LegalEntity.TaxRate` > 0
- Ensure `Ledger.Income` > 0

**Production not working?**
- Verify `AssetTag` exists
- Check `AssetSpecCatalog` singleton
- Ensure `ResourceStock` buffer exists

