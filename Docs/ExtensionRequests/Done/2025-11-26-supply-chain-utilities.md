# Extension Request: Supply Chain Calculation Utilities

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Completed**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P2  

### Implementation
- `Packages/com.moni.puredots/Runtime/Runtime/Resources/SupplyChainComponents.cs` - ConsumptionRate, SupplyStatus, SupplySource, SupplyRoute, EmergencySupplyState, SupplyChainConfig
- `Packages/com.moni.puredots/Runtime/Runtime/Resources/SupplyChainHelpers.cs` - Static helpers for consumption, route efficiency, deficit calculation

---

## Use Case

Both games need supply chain calculations for resource consumption and logistics:

**Space4X:**
- Fleet fuel consumption rates during travel
- Supply route efficiency between stations
- Emergency harvest triggers when supplies critical
- Logistics convoy scheduling
- Colony supply/demand ratio tracking

**Godgame:**
- Village food consumption based on population
- Caravan route profitability
- Foraging radius during food shortage
- Seasonal supply variation
- Work crew resource delivery

Shared needs:
- Consumption rate calculation (per-entity and aggregate)
- Supply duration estimation ("days until empty")
- Route efficiency evaluation
- Emergency threshold detection
- Source/consumer matching

---

## Proposed Solution

**Extension Type**: New Components + Static Helpers

### Components (`Packages/com.moni.puredots/Runtime/Runtime/Resources/`)

```csharp
/// <summary>
/// Per-resource consumption rate.
/// </summary>
public struct ConsumptionRate
{
    public ushort ResourceTypeId;
    public float BaseRate;             // Units consumed per tick (or day)
    public float CurrentRate;          // After modifiers
    public float Efficiency;           // 0-1, higher = less waste
}

/// <summary>
/// Current supply status for an entity or group.
/// </summary>
public struct SupplyStatus : IComponentData
{
    public float TotalSupply;          // Current stockpile
    public float MaxCapacity;          // Storage limit
    public float TotalConsumption;     // Sum of all consumption rates
    public float NetFlow;              // Income - consumption
    public float DaysRemaining;        // Estimated time until empty
    public byte IsInDeficit;           // Consuming more than receiving
    public byte IsEmergency;           // Below emergency threshold
    public uint LastUpdateTick;
}

/// <summary>
/// Buffer of consumption rates by resource type.
/// </summary>
[InternalBufferCapacity(8)]
public struct ConsumptionRateEntry : IBufferElementData
{
    public ConsumptionRate Rate;
}

/// <summary>
/// Supply source definition.
/// </summary>
public struct SupplySource : IComponentData
{
    public Entity SourceEntity;        // Where supplies come from
    public ushort ResourceTypeId;
    public float ProductionRate;       // Units produced per tick
    public float CurrentStock;         // Available at source
    public float MaxStock;
    public float ReserveRatio;         // Minimum % to keep in reserve
    public byte IsAvailable;           // Can currently supply
}

/// <summary>
/// Route between supply source and consumer.
/// </summary>
public struct SupplyRoute : IComponentData
{
    public Entity SourceEntity;
    public Entity DestinationEntity;
    public ushort ResourceTypeId;
    public float Distance;             // Travel distance
    public float TransportCapacity;    // Units per trip
    public float TravelTime;           // Ticks per round trip
    public float RiskFactor;           // Chance of loss (0-1)
    public float Efficiency;           // Calculated efficiency score
    public byte IsActive;
}

/// <summary>
/// Emergency supply situation.
/// </summary>
public struct EmergencySupplyState : IComponentData
{
    public ushort ResourceTypeId;
    public float CriticalThreshold;    // % below which is emergency
    public float CurrentRatio;         // Current supply / consumption
    public byte IsEmergency;
    public byte IsForaging;            // Emergency gathering active
    public uint EmergencyStartTick;
}

/// <summary>
/// Supply chain configuration.
/// </summary>
public struct SupplyChainConfig : IComponentData
{
    public float EmergencyThreshold;   // Days remaining to trigger emergency (e.g., 3)
    public float WarningThreshold;     // Days remaining for warning (e.g., 7)
    public float ReserveRatio;         // Target reserve % (e.g., 0.2 = 20%)
    public float MaxRouteDistance;     // Maximum viable supply route
    public float EfficiencyMinimum;    // Minimum route efficiency to use
}
```

### Static Helpers

```csharp
public static class SupplyChainHelpers
{
    /// <summary>
    /// Calculates total consumption rate from multiple consumers.
    /// </summary>
    public static float CalculateTotalConsumption(
        in DynamicBuffer<ConsumptionRateEntry> rates)
    {
        float total = 0;
        for (int i = 0; i < rates.Length; i++)
        {
            total += rates[i].Rate.CurrentRate;
        }
        return total;
    }

    /// <summary>
    /// Calculates burn rate with efficiency modifier.
    /// </summary>
    public static float CalculateBurnRate(
        float baseRate,
        int consumerCount,
        float efficiency)
    {
        float rawRate = baseRate * consumerCount;
        return rawRate * (2f - efficiency); // Low efficiency = higher burn
    }

    /// <summary>
    /// Estimates days until supply exhaustion.
    /// </summary>
    public static float EstimateSupplyDuration(
        float currentSupply,
        float consumptionRate,
        float incomeRate)
    {
        float netBurn = consumptionRate - incomeRate;
        if (netBurn <= 0) return float.MaxValue; // Not depleting
        return currentSupply / netBurn;
    }

    /// <summary>
    /// Calculates route efficiency score.
    /// </summary>
    public static float CalculateRouteEfficiency(
        float distance,
        float capacity,
        float travelTime,
        float riskFactor)
    {
        if (travelTime <= 0) return 0;
        
        // Throughput: how much delivered per unit time
        float throughput = capacity / travelTime;
        
        // Risk adjustment
        float safeDelivery = 1f - riskFactor;
        
        // Distance penalty (longer = less efficient)
        float distanceFactor = 1f / (1f + distance * 0.01f);
        
        return throughput * safeDelivery * distanceFactor;
    }

    /// <summary>
    /// Checks if supply situation is emergency.
    /// </summary>
    public static bool IsEmergencyThreshold(
        float currentSupply,
        float consumptionRate,
        float emergencyDays)
    {
        float daysRemaining = EstimateSupplyDuration(currentSupply, consumptionRate, 0);
        return daysRemaining < emergencyDays;
    }

    /// <summary>
    /// Calculates supply ratio (supply / demand).
    /// </summary>
    public static float CalculateSupplyRatio(
        float currentSupply,
        float maxCapacity,
        float consumptionRate)
    {
        if (consumptionRate <= 0) return 1f;
        
        // How many days of consumption we have
        float daysOfSupply = currentSupply / consumptionRate;
        
        // Normalize to 0-1 range (7 days = 1.0, more is capped)
        return math.saturate(daysOfSupply / 7f);
    }

    /// <summary>
    /// Finds the best supply source for a consumer.
    /// </summary>
    public static Entity FindBestSupplySource(
        NativeArray<Entity> sources,
        NativeArray<SupplySource> sourceData,
        float3 consumerPosition,
        NativeArray<float3> sourcePositions,
        float maxDistance,
        out float efficiency)
    {
        Entity best = Entity.Null;
        float bestScore = 0;
        efficiency = 0;

        for (int i = 0; i < sources.Length; i++)
        {
            if (sourceData[i].IsAvailable == 0) continue;
            
            float distance = math.distance(consumerPosition, sourcePositions[i]);
            if (distance > maxDistance) continue;
            
            // Score: available stock / distance
            float availableStock = sourceData[i].CurrentStock * (1f - sourceData[i].ReserveRatio);
            if (availableStock <= 0) continue;
            
            float score = availableStock / (1f + distance * 0.1f);
            
            if (score > bestScore)
            {
                bestScore = score;
                best = sources[i];
                efficiency = CalculateRouteEfficiency(distance, availableStock, distance * 2f, 0);
            }
        }

        return best;
    }

    /// <summary>
    /// Calculates supply deficit.
    /// </summary>
    public static float CalculateDeficit(
        float consumption,
        float income,
        float targetReserve,
        float currentSupply)
    {
        float netLoss = consumption - income;
        if (netLoss <= 0) return 0;
        
        // How much extra income needed to maintain reserve
        float targetStock = consumption * targetReserve;
        float deficit = netLoss + math.max(0, targetStock - currentSupply);
        
        return deficit;
    }

    /// <summary>
    /// Updates supply status from current data.
    /// </summary>
    public static SupplyStatus UpdateSupplyStatus(
        float currentSupply,
        float maxCapacity,
        in DynamicBuffer<ConsumptionRateEntry> rates,
        float incomeRate,
        in SupplyChainConfig config,
        uint currentTick)
    {
        float consumption = CalculateTotalConsumption(rates);
        float netFlow = incomeRate - consumption;
        float daysRemaining = EstimateSupplyDuration(currentSupply, consumption, incomeRate);
        
        return new SupplyStatus
        {
            TotalSupply = currentSupply,
            MaxCapacity = maxCapacity,
            TotalConsumption = consumption,
            NetFlow = netFlow,
            DaysRemaining = daysRemaining,
            IsInDeficit = (byte)(netFlow < 0 ? 1 : 0),
            IsEmergency = (byte)(daysRemaining < config.EmergencyThreshold ? 1 : 0),
            LastUpdateTick = currentTick
        };
    }

    /// <summary>
    /// Checks if emergency foraging should start.
    /// </summary>
    public static bool ShouldStartForaging(
        in SupplyStatus status,
        in SupplyChainConfig config)
    {
        return status.IsEmergency != 0 && status.DaysRemaining < config.EmergencyThreshold;
    }

    /// <summary>
    /// Calculates optimal delivery quantity.
    /// </summary>
    public static float CalculateDeliveryQuantity(
        float deficit,
        float transportCapacity,
        float sourceAvailable)
    {
        // Don't deliver more than needed or available
        return math.min(deficit, math.min(transportCapacity, sourceAvailable));
    }
}
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Runtime/Resources/SupplyChainComponents.cs`
- New: `Packages/com.moni.puredots/Runtime/Runtime/Resources/SupplyChainHelpers.cs`
- Integration: Existing resource systems can use these utilities

**Breaking Changes:** None - entirely new feature

---

## Example Usage

```csharp
// === Space4X: Fleet supply tracking ===
var status = EntityManager.GetComponentData<SupplyStatus>(fleetEntity);
var rates = EntityManager.GetBuffer<ConsumptionRateEntry>(fleetEntity);
var config = EntityManager.GetComponentData<SupplyChainConfig>(fleetEntity);

// Update supply status each tick
float fuelIncome = 0; // No income while traveling
status = SupplyChainHelpers.UpdateSupplyStatus(
    currentFuel, maxFuel, rates, fuelIncome, config, currentTick);

if (status.IsEmergency != 0)
{
    // Trigger emergency protocols - find nearest station
    TriggerEmergencyResupply(fleetEntity);
}

// Estimate remaining travel capability
float daysRemaining = SupplyChainHelpers.EstimateSupplyDuration(
    currentFuel, fuelConsumption, 0);
if (daysRemaining < requiredTravelTime)
{
    // Cannot reach destination - need resupply
    PlanResupplyStop(fleetEntity);
}

// === Godgame: Village food management ===
var villageStatus = EntityManager.GetComponentData<SupplyStatus>(villageEntity);
var consumption = EntityManager.GetBuffer<ConsumptionRateEntry>(villageEntity);

// Calculate food burn rate based on population
float burnRate = SupplyChainHelpers.CalculateBurnRate(
    baseFoodPerVillager, villagerCount, farmEfficiency);

// Check if foraging needed
if (SupplyChainHelpers.ShouldStartForaging(villageStatus, config))
{
    // Send villagers to forage
    StartForagingTask(villageEntity);
}

// Evaluate caravan route
float routeEfficiency = SupplyChainHelpers.CalculateRouteEfficiency(
    distanceToTradingPost, caravanCapacity, roundTripTime, banditRisk);

if (routeEfficiency > config.EfficiencyMinimum)
{
    // Route is viable - schedule caravan
    ScheduleCaravan(villageEntity, tradingPostEntity);
}

// === Finding supply sources ===
Entity bestSource = SupplyChainHelpers.FindBestSupplySource(
    sources, sourceData, myPosition, sourcePositions, maxRange, out float efficiency);

if (bestSource != Entity.Null)
{
    // Create supply route
    CreateSupplyRoute(myEntity, bestSource, efficiency);
}
```

---

## Alternative Approaches Considered

- **Alternative 1**: Game-specific supply systems
  - **Rejected**: Core calculations (burn rate, duration, efficiency) identical

- **Alternative 2**: Simple inventory-only tracking
  - **Rejected**: No support for routes, sources, emergency detection

- **Alternative 3**: Full economic simulation
  - **Rejected**: Too complex - games need simple utility functions

---

## Implementation Notes

**Dependencies:**
- Resource type registry for type IDs
- Spatial system for distance calculations

**Performance Considerations:**
- All helpers are static and burst-compatible
- FindBestSupplySource is O(n) but sources are typically small count
- Consumption buffers are fixed-size

**Related Requests:**
- Threshold behavior triggers (emergency thresholds)
- Economy/market dynamics (route profitability)

---

## Review Notes

*(PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

