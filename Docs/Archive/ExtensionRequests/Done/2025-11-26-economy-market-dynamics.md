# Extension Request: Economy & Market Dynamics

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P3  
**Assigned To**: TBD

---

## Use Case

Both games need market simulation for resource trading:

- **Space4X**: Interstellar commodity markets, faction trade, embargo effects, supply/demand pricing
- **Godgame**: Village markets, traveling merchants, seasonal pricing, import/export

Shared needs:
- Supply/demand price calculation
- Price history tracking (for trends)
- Trade route profitability analysis
- Market event effects (shortages, gluts, embargoes)

---

## Proposed Solution

**Extension Type**: New Components + Helpers

### Components (`Packages/com.moni.puredots/Runtime/Runtime/Economy/`)

```csharp
/// <summary>
/// Market price for a resource type at a location.
/// </summary>
public struct MarketPrice : IComponentData
{
    public ushort ResourceTypeId;
    public float CurrentPrice;
    public float BasePrice;         // Natural equilibrium price
    public float Supply;            // Available quantity
    public float Demand;            // Desired quantity
    public half Elasticity;         // Price sensitivity (0.5 = inelastic, 2.0 = elastic)
    public uint LastUpdateTick;
}

/// <summary>
/// Price history for trend analysis.
/// </summary>
[InternalBufferCapacity(32)]
public struct PriceHistoryEntry : IBufferElementData
{
    public float Price;
    public float Supply;
    public float Demand;
    public uint Tick;
}

/// <summary>
/// Trade offer from a merchant or market.
/// </summary>
public struct TradeOffer : IBufferElementData
{
    public ushort ResourceTypeId;
    public float Quantity;
    public float PricePerUnit;
    public Entity OffererEntity;
    public TradeOfferType Type;      // Buy or Sell
    public uint ExpiryTick;
}

public enum TradeOfferType : byte
{
    Buy = 0,
    Sell = 1
}

/// <summary>
/// Trade route definition.
/// </summary>
public struct TradeRoute : IComponentData
{
    public Entity SourceMarket;
    public Entity DestinationMarket;
    public ushort ResourceTypeId;
    public float Volume;            // Units per trip
    public float TransportCost;     // Cost per unit
    public float RiskFactor;        // Loss chance (pirates, hazards)
    public half Profitability;      // Calculated profit margin
    public byte IsActive;
}

/// <summary>
/// Market event affecting prices.
/// </summary>
public struct MarketEvent : IComponentData
{
    public MarketEventType Type;
    public ushort AffectedResourceId;
    public float Magnitude;         // Effect strength
    public uint StartTick;
    public uint DurationTicks;
}

public enum MarketEventType : byte
{
    None = 0,
    Shortage = 1,       // Reduced supply
    Glut = 2,           // Excess supply
    Embargo = 3,        // Trade restriction
    Subsidy = 4,        // Price reduction
    Tariff = 5,         // Price increase
    Discovery = 6,      // New source found
    Disaster = 7        // Production disrupted
}

/// <summary>
/// Configuration for market simulation.
/// </summary>
public struct MarketConfig : IComponentData
{
    public float PriceUpdateInterval;    // Ticks between price updates
    public float MaxPriceChange;         // Max % change per update
    public float MinPrice;               // Floor price multiplier
    public float MaxPrice;               // Ceiling price multiplier
    public byte HistoryLength;           // Price history entries to keep
}
```

### Static Helpers

```csharp
public static class MarketHelpers
{
    /// <summary>
    /// Calculates price from supply/demand ratio.
    /// </summary>
    public static float CalculatePrice(
        float basePrice,
        float supply,
        float demand,
        float elasticity)
    {
        if (supply <= 0.001f)
        {
            // Extreme scarcity
            return basePrice * 10f;
        }

        float ratio = demand / supply;
        float modifier = math.pow(ratio, elasticity);
        return basePrice * math.clamp(modifier, 0.1f, 10f);
    }

    /// <summary>
    /// Calculates price with market event effects.
    /// </summary>
    public static float CalculatePriceWithEvents(
        float basePrice,
        float supply,
        float demand,
        float elasticity,
        MarketEventType eventType,
        float eventMagnitude)
    {
        float price = CalculatePrice(basePrice, supply, demand, elasticity);

        switch (eventType)
        {
            case MarketEventType.Shortage:
                price *= (1f + eventMagnitude);
                break;
            case MarketEventType.Glut:
                price *= (1f - eventMagnitude * 0.5f);
                break;
            case MarketEventType.Embargo:
                price *= (1f + eventMagnitude * 2f);
                break;
            case MarketEventType.Subsidy:
                price *= (1f - eventMagnitude);
                break;
            case MarketEventType.Tariff:
                price *= (1f + eventMagnitude);
                break;
        }

        return price;
    }

    /// <summary>
    /// Calculates trade route profitability.
    /// </summary>
    public static float CalculateRouteProfitability(
        float buyPrice,
        float sellPrice,
        float transportCostPerUnit,
        float riskFactor)
    {
        float grossMargin = sellPrice - buyPrice;
        float netMargin = grossMargin - transportCostPerUnit;
        float riskAdjusted = netMargin * (1f - riskFactor);
        
        // Return as percentage of buy price
        return buyPrice > 0 ? riskAdjusted / buyPrice : 0;
    }

    /// <summary>
    /// Calculates price trend from history.
    /// </summary>
    public static float CalculatePriceTrend(
        DynamicBuffer<PriceHistoryEntry> history,
        int sampleCount)
    {
        if (history.Length < 2) return 0;

        int startIdx = math.max(0, history.Length - sampleCount);
        float firstPrice = history[startIdx].Price;
        float lastPrice = history[history.Length - 1].Price;

        return firstPrice > 0 ? (lastPrice - firstPrice) / firstPrice : 0;
    }

    /// <summary>
    /// Finds best trade route for a resource.
    /// </summary>
    public static Entity FindBestTradeDestination(
        NativeArray<Entity> markets,
        NativeArray<MarketPrice> prices,
        ushort resourceTypeId,
        float buyPrice,
        float transportCostPerUnit)
    {
        Entity bestMarket = Entity.Null;
        float bestProfit = 0;

        for (int i = 0; i < markets.Length; i++)
        {
            if (prices[i].ResourceTypeId != resourceTypeId) continue;

            float profit = CalculateRouteProfitability(
                buyPrice,
                prices[i].CurrentPrice,
                transportCostPerUnit,
                0f); // Risk handled separately

            if (profit > bestProfit)
            {
                bestProfit = profit;
                bestMarket = markets[i];
            }
        }

        return bestMarket;
    }

    /// <summary>
    /// Updates supply/demand from trade activity.
    /// </summary>
    public static void ApplyTrade(
        ref MarketPrice price,
        float quantity,
        TradeOfferType tradeType)
    {
        if (tradeType == TradeOfferType.Buy)
        {
            price.Supply -= quantity;
            price.Demand += quantity * 0.1f; // Buying signals demand
        }
        else
        {
            price.Supply += quantity;
            price.Demand -= quantity * 0.1f; // Selling reduces pressure
        }

        price.Supply = math.max(0, price.Supply);
        price.Demand = math.max(0.1f, price.Demand);
    }
}
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Runtime/Economy/MarketComponents.cs`
- New: `Packages/com.moni.puredots/Runtime/Runtime/Economy/MarketHelpers.cs`

**Breaking Changes:** None - entirely new feature

---

## Example Usage

```csharp
// === Space4X: Check commodity prices ===
var market = EntityManager.GetComponentData<MarketPrice>(stationEntity);
float ironPrice = MarketHelpers.CalculatePrice(
    market.BasePrice, market.Supply, market.Demand, (float)market.Elasticity);

// === Space4X: Evaluate trade route ===
var sourcePrice = 10f;  // Buy iron at mining colony
var destPrice = 25f;    // Sell at industrial station
var transportCost = 3f;
var piracyRisk = 0.1f;

float profitability = MarketHelpers.CalculateRouteProfitability(
    sourcePrice, destPrice, transportCost, piracyRisk);
// Result: ~110% profit margin

// === Godgame: Village market with seasonal event ===
var harvestGlut = new MarketEvent
{
    Type = MarketEventType.Glut,
    AffectedResourceId = GRAIN_ID,
    Magnitude = 0.3f  // 30% price reduction
};

float grainPrice = MarketHelpers.CalculatePriceWithEvents(
    baseGrainPrice, grainSupply, grainDemand, 1f,
    harvestGlut.Type, harvestGlut.Magnitude);

// === Track price trends ===
var history = EntityManager.GetBuffer<PriceHistoryEntry>(marketEntity);
float trend = MarketHelpers.CalculatePriceTrend(history, 10);
// trend > 0: prices rising, trend < 0: prices falling
```

---

## Alternative Approaches Considered

- **Alternative 1**: Static price tables
  - **Rejected**: No emergent economy, feels artificial

- **Alternative 2**: Full agent-based economic simulation
  - **Rejected**: Too complex and expensive for gameplay needs

- **Alternative 3**: Game-specific implementations
  - **Rejected**: Both games need similar market mechanics

---

## Implementation Notes

**Dependencies:**
- Resource system for type IDs
- Spatial grid for market location queries

**Performance Considerations:**
- Price calculations are simple math
- History buffer is fixed-size, no allocations
- Route profitability can be cached and updated periodically

**Related Requests:**
- Resource registry for type definitions
- Entity relations for faction trade modifiers

---

## Review Notes

*(This section is for PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

