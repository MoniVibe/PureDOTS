# Ownership & Economy Core

Economic simulation system for PureDOTS with three-layer ECS architecture.

## Architecture

- **Body ECS** (60Hz): Physical production, extraction, logistics
- **Mind ECS** (1Hz): Investment decisions, risk assessment, trade
- **Aggregate ECS** (0.2Hz): Market equilibrium, empire wealth, taxes

## Components

### Ownership
- `Ownership` - Single owner per asset
- `OwnershipBuffer` - Multiple owners (joint ventures)
- `AssetTag` - Asset identification

### Financial
- `Ledger` - Cash, income, expenses
- `Portfolio` - Owned assets for income calculation
- `FinancialState` - Dirty flag optimization

### Legal
- `LegalEntity` - Organizations, corporations, empires
- `GoverningEntity` - Asset jurisdiction for taxes

### Production
- `ResourceStock` - Resource quantities on assets
- `TradeRoute` - Resource flow between assets

### Events
- `PurchaseEvent` - Asset acquisition requests
- `SaleEvent` - Asset disposal requests
- `InvestmentCommand` - MindECS investment decisions

## Systems

### Body (60Hz)
- `ProductionSystem` - Calculates production from AssetSpec
- `MineExtractionSystem` - Handles mine extraction
- `HaulingSystem` - Processes trade routes

### Mind (1Hz)
- `LedgerUpdateSystem` - Updates income/cash from portfolio
- `PortfolioManagementSystem` - Handles purchase/sale events
- `InvestmentDecisionSystem` - Evaluates investment opportunities
- `RiskAssessmentSystem` - Calculates asset risk scores
- `TradeNegotiationSystem` - Processes trade offers

### Aggregate (0.2Hz)
- `MarketEquilibriumSystem` - Calculates market prices
- `EmpireWealthSystem` - Aggregates wealth across hierarchies
- `LegalEntitySystem` - Manages legal entities, calculates tax rates
- `TaxCollectionSystem` - Collects taxes from governed assets

## Usage

See `Docs/Guides/OwnershipEconomyGuide.md` for detailed integration guide.

## Authoring

- `AssetSpecAuthoring` - ScriptableObject for asset specifications
- `AssetSpecCatalogAuthoring` - Catalog of all asset types
- `OwnershipAuthoring` - MonoBehaviour + Baker for initial ownership

## Integration Points

- **Resource Systems**: `ResourceStock` buffers integrate with existing resource components
- **Market Systems**: `MarketCell` uses spatial grid for queries
- **MindECS**: Investment decisions via `AgentSyncBus` (future)
- **Wealth Systems**: `Portfolio` bridges to `AssetHolding` for individual wealth

