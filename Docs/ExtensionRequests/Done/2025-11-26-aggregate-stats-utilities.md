# Extension Request: Aggregate Stats Utilities

**Status**: `[COMPLETED]`  
**Verified**: `[DONE]` - Implementation verified 2025-01-27
**Submitted**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Both games need aggregate statistics calculations for groups of entities:

**Space4X:**
- Fleet morale average (crew across all ships)
- Department efficiency per carrier (weighted by staffing)
- Colony supply ratio (total supply / total demand)
- Fleet combat readiness (average hull, ammo, fuel)
- Bottleneck detection (which resource is most scarce)

**Godgame:**
- Village happiness average
- Band cohesion score
- Work crew productivity (weighted by skill)
- Settlement food security ratio
- Resource bottleneck identification

Shared needs:
- Weighted mean calculation
- Min/max/variance tracking
- Bottleneck detection
- Group composition analysis
- Efficiency calculations

---

## Proposed Solution

**Extension Type**: New Components + Static Helpers

### Components (`Packages/com.moni.puredots/Runtime/Runtime/Stats/`)

```csharp
/// <summary>
/// Aggregate statistics for a group of values.
/// </summary>
public struct AggregateStats : IComponentData
{
    public float Mean;
    public float WeightedMean;
    public float Min;
    public float Max;
    public float Variance;
    public float StandardDeviation;
    public int Count;
    public float TotalWeight;
    public uint LastCalculatedTick;
}

/// <summary>
/// Identifies the weakest point in a chain/group.
/// </summary>
public struct BottleneckEntry : IComponentData
{
    public ushort ResourceTypeId;      // Which resource is bottlenecked
    public Entity BottleneckEntity;    // Which entity is the bottleneck
    public float BottleneckValue;      // Current value at bottleneck
    public float ThresholdValue;       // What it should be
    public float DeficitRatio;         // How far below threshold (0-1)
    public byte Severity;              // 0 = minor, 1 = moderate, 2 = critical
    public uint DetectedTick;
}

/// <summary>
/// Buffer of bottlenecks in priority order.
/// </summary>
[InternalBufferCapacity(4)]
public struct BottleneckBuffer : IBufferElementData
{
    public BottleneckEntry Entry;
}

/// <summary>
/// Composition of a group (counts by category).
/// </summary>
public struct GroupComposition : IComponentData
{
    public int TotalCount;
    public int ActiveCount;            // Not disabled/dead
    public int HealthyCount;           // Above health threshold
    public int DamagedCount;           // Below health threshold
    public int CriticalCount;          // Below critical threshold
    public float HealthyRatio;         // Healthy / Total
    public float ReadinessScore;       // Weighted readiness
}

/// <summary>
/// Per-category count for detailed composition.
/// </summary>
[InternalBufferCapacity(8)]
public struct CompositionCategory : IBufferElementData
{
    public FixedString32Bytes CategoryId;
    public int Count;
    public float Weight;               // Importance/contribution weight
    public float AverageValue;         // Average stat for this category
}

/// <summary>
/// Configuration for aggregate calculations.
/// </summary>
public struct AggregateConfig : IComponentData
{
    public float HealthyThreshold;     // % to be considered healthy
    public float CriticalThreshold;    // % to be considered critical
    public float BottleneckThreshold;  // % below which is bottleneck
    public byte WeightByImportance;    // Weight by entity importance
    public byte IncludeInactive;       // Include inactive entities
}

/// <summary>
/// Single value contribution to aggregate.
/// </summary>
public struct StatContribution
{
    public float Value;
    public float Weight;
    public byte IsActive;
    public byte Category;
}
```

### Static Helpers

```csharp
public static class AggregateStatsHelpers
{
    /// <summary>
    /// Calculates simple mean of values.
    /// </summary>
    public static float CalculateMean(NativeArray<float> values)
    {
        if (values.Length == 0) return 0;
        
        float sum = 0;
        for (int i = 0; i < values.Length; i++)
        {
            sum += values[i];
        }
        return sum / values.Length;
    }

    /// <summary>
    /// Calculates weighted mean of values.
    /// </summary>
    public static float CalculateWeightedMean(
        NativeArray<StatContribution> contributions)
    {
        if (contributions.Length == 0) return 0;
        
        float weightedSum = 0;
        float totalWeight = 0;
        
        for (int i = 0; i < contributions.Length; i++)
        {
            weightedSum += contributions[i].Value * contributions[i].Weight;
            totalWeight += contributions[i].Weight;
        }
        
        return totalWeight > 0 ? weightedSum / totalWeight : 0;
    }

    /// <summary>
    /// Calculates variance of values around mean.
    /// </summary>
    public static float CalculateVariance(NativeArray<float> values, float mean)
    {
        if (values.Length <= 1) return 0;
        
        float sumSquaredDiff = 0;
        for (int i = 0; i < values.Length; i++)
        {
            float diff = values[i] - mean;
            sumSquaredDiff += diff * diff;
        }
        
        return sumSquaredDiff / (values.Length - 1);
    }

    /// <summary>
    /// Calculates full aggregate statistics.
    /// </summary>
    public static AggregateStats CalculateFullStats(
        NativeArray<StatContribution> contributions,
        uint currentTick)
    {
        if (contributions.Length == 0)
        {
            return new AggregateStats { LastCalculatedTick = currentTick };
        }

        float min = float.MaxValue;
        float max = float.MinValue;
        float sum = 0;
        float weightedSum = 0;
        float totalWeight = 0;
        int count = 0;

        for (int i = 0; i < contributions.Length; i++)
        {
            var c = contributions[i];
            if (c.IsActive == 0) continue;
            
            sum += c.Value;
            weightedSum += c.Value * c.Weight;
            totalWeight += c.Weight;
            min = math.min(min, c.Value);
            max = math.max(max, c.Value);
            count++;
        }

        float mean = count > 0 ? sum / count : 0;
        float weightedMean = totalWeight > 0 ? weightedSum / totalWeight : 0;

        // Calculate variance
        float variance = 0;
        if (count > 1)
        {
            float sumSquaredDiff = 0;
            for (int i = 0; i < contributions.Length; i++)
            {
                if (contributions[i].IsActive == 0) continue;
                float diff = contributions[i].Value - mean;
                sumSquaredDiff += diff * diff;
            }
            variance = sumSquaredDiff / (count - 1);
        }

        return new AggregateStats
        {
            Mean = mean,
            WeightedMean = weightedMean,
            Min = count > 0 ? min : 0,
            Max = count > 0 ? max : 0,
            Variance = variance,
            StandardDeviation = math.sqrt(variance),
            Count = count,
            TotalWeight = totalWeight,
            LastCalculatedTick = currentTick
        };
    }

    /// <summary>
    /// Finds bottlenecks - values below threshold.
    /// </summary>
    public static void FindBottlenecks(
        NativeArray<StatContribution> contributions,
        NativeArray<ushort> resourceTypeIds,
        NativeArray<Entity> entities,
        float threshold,
        ref DynamicBuffer<BottleneckBuffer> bottlenecks,
        uint currentTick)
    {
        bottlenecks.Clear();
        
        for (int i = 0; i < contributions.Length; i++)
        {
            var c = contributions[i];
            if (c.IsActive == 0) continue;
            
            float ratio = c.Value; // Assuming normalized 0-1
            if (ratio < threshold)
            {
                float deficitRatio = 1f - (ratio / threshold);
                byte severity = (byte)(deficitRatio > 0.66f ? 2 : deficitRatio > 0.33f ? 1 : 0);
                
                bottlenecks.Add(new BottleneckBuffer
                {
                    Entry = new BottleneckEntry
                    {
                        ResourceTypeId = resourceTypeIds[i],
                        BottleneckEntity = entities[i],
                        BottleneckValue = c.Value,
                        ThresholdValue = threshold,
                        DeficitRatio = deficitRatio,
                        Severity = severity,
                        DetectedTick = currentTick
                    }
                });
            }
        }
        
        // Sort by deficit ratio (worst first)
        SortBottlenecksBySeverity(ref bottlenecks);
    }

    private static void SortBottlenecksBySeverity(ref DynamicBuffer<BottleneckBuffer> bottlenecks)
    {
        // Simple bubble sort for small buffers
        for (int i = 0; i < bottlenecks.Length - 1; i++)
        {
            for (int j = 0; j < bottlenecks.Length - i - 1; j++)
            {
                if (bottlenecks[j].Entry.DeficitRatio < bottlenecks[j + 1].Entry.DeficitRatio)
                {
                    var temp = bottlenecks[j];
                    bottlenecks[j] = bottlenecks[j + 1];
                    bottlenecks[j + 1] = temp;
                }
            }
        }
    }

    /// <summary>
    /// Calculates group composition from contributions.
    /// </summary>
    public static GroupComposition CalculateComposition(
        NativeArray<StatContribution> contributions,
        in AggregateConfig config)
    {
        int total = 0;
        int active = 0;
        int healthy = 0;
        int damaged = 0;
        int critical = 0;
        float readinessSum = 0;
        float readinessWeight = 0;

        for (int i = 0; i < contributions.Length; i++)
        {
            var c = contributions[i];
            total++;
            
            if (c.IsActive == 0 && config.IncludeInactive == 0) continue;
            if (c.IsActive != 0) active++;
            
            if (c.Value >= config.HealthyThreshold)
            {
                healthy++;
            }
            else if (c.Value >= config.CriticalThreshold)
            {
                damaged++;
            }
            else
            {
                critical++;
            }

            // Readiness is value weighted by importance
            readinessSum += c.Value * c.Weight;
            readinessWeight += c.Weight;
        }

        return new GroupComposition
        {
            TotalCount = total,
            ActiveCount = active,
            HealthyCount = healthy,
            DamagedCount = damaged,
            CriticalCount = critical,
            HealthyRatio = total > 0 ? (float)healthy / total : 0,
            ReadinessScore = readinessWeight > 0 ? readinessSum / readinessWeight : 0
        };
    }

    /// <summary>
    /// Calculates efficiency with bottleneck penalty.
    /// </summary>
    public static float CalculateEfficiency(
        in AggregateStats stats,
        in DynamicBuffer<BottleneckBuffer> bottlenecks,
        float bottleneckPenaltyMultiplier)
    {
        // Base efficiency from weighted mean
        float baseEfficiency = stats.WeightedMean;
        
        // Apply penalty for each bottleneck
        float penalty = 0;
        for (int i = 0; i < bottlenecks.Length; i++)
        {
            penalty += bottlenecks[i].Entry.DeficitRatio * bottleneckPenaltyMultiplier;
        }
        
        // Cap penalty at 50% reduction
        penalty = math.min(penalty, 0.5f);
        
        return baseEfficiency * (1f - penalty);
    }

    /// <summary>
    /// Calculates cohesion (how similar values are).
    /// Low variance = high cohesion.
    /// </summary>
    public static float CalculateCohesion(in AggregateStats stats)
    {
        if (stats.Count <= 1) return 1f;
        
        // Coefficient of variation (CV) normalized
        // Low CV = high cohesion
        float cv = stats.Mean > 0 ? stats.StandardDeviation / stats.Mean : 0;
        
        // Convert to 0-1 cohesion score (inverse relationship)
        return math.saturate(1f - cv);
    }

    /// <summary>
    /// Aggregates per-category stats.
    /// </summary>
    public static void CalculateCategoryStats(
        NativeArray<StatContribution> contributions,
        ref DynamicBuffer<CompositionCategory> categories)
    {
        // Count and sum by category
        NativeHashMap<byte, float> sums = new NativeHashMap<byte, float>(16, Allocator.Temp);
        NativeHashMap<byte, int> counts = new NativeHashMap<byte, int>(16, Allocator.Temp);
        NativeHashMap<byte, float> weights = new NativeHashMap<byte, float>(16, Allocator.Temp);

        for (int i = 0; i < contributions.Length; i++)
        {
            var c = contributions[i];
            if (c.IsActive == 0) continue;
            
            byte cat = c.Category;
            
            if (sums.TryGetValue(cat, out float sum))
            {
                sums[cat] = sum + c.Value;
                counts[cat] = counts[cat] + 1;
                weights[cat] = weights[cat] + c.Weight;
            }
            else
            {
                sums.Add(cat, c.Value);
                counts.Add(cat, 1);
                weights.Add(cat, c.Weight);
            }
        }

        // Update category buffer
        categories.Clear();
        var keys = sums.GetKeyArray(Allocator.Temp);
        for (int i = 0; i < keys.Length; i++)
        {
            byte cat = keys[i];
            int count = counts[cat];
            
            categories.Add(new CompositionCategory
            {
                CategoryId = new FixedString32Bytes($"Category_{cat}"),
                Count = count,
                Weight = weights[cat],
                AverageValue = count > 0 ? sums[cat] / count : 0
            });
        }

        sums.Dispose();
        counts.Dispose();
        weights.Dispose();
        keys.Dispose();
    }

    /// <summary>
    /// Gets the worst bottleneck (highest deficit).
    /// </summary>
    public static bool TryGetWorstBottleneck(
        in DynamicBuffer<BottleneckBuffer> bottlenecks,
        out BottleneckEntry worst)
    {
        worst = default;
        if (bottlenecks.Length == 0) return false;
        
        // Buffer is already sorted by severity
        worst = bottlenecks[0].Entry;
        return true;
    }

    /// <summary>
    /// Counts bottlenecks by severity level.
    /// </summary>
    public static void CountBottlenecksBySeverity(
        in DynamicBuffer<BottleneckBuffer> bottlenecks,
        out int minor,
        out int moderate,
        out int critical)
    {
        minor = 0;
        moderate = 0;
        critical = 0;
        
        for (int i = 0; i < bottlenecks.Length; i++)
        {
            switch (bottlenecks[i].Entry.Severity)
            {
                case 0: minor++; break;
                case 1: moderate++; break;
                case 2: critical++; break;
            }
        }
    }
}
```

---

## Impact Assessment

**Files/Systems Affected:**
- New: `Packages/com.moni.puredots/Runtime/Runtime/Stats/AggregateStatsComponents.cs`
- New: `Packages/com.moni.puredots/Runtime/Runtime/Stats/AggregateStatsHelpers.cs`
- Integration: Game-specific stat aggregation systems consume these utilities

**Breaking Changes:** None - entirely new feature

---

## Example Usage

```csharp
// === Space4X: Fleet morale aggregate ===
var contributions = new NativeArray<StatContribution>(shipCount, Allocator.Temp);

for (int i = 0; i < shipCount; i++)
{
    var morale = EntityManager.GetComponentData<MoraleState>(ships[i]);
    var importance = EntityManager.GetComponentData<FleetRole>(ships[i]);
    
    contributions[i] = new StatContribution
    {
        Value = morale.Current,
        Weight = importance.CommandWeight,
        IsActive = 1,
        Category = (byte)importance.Role
    };
}

var fleetMorale = AggregateStatsHelpers.CalculateFullStats(contributions, currentTick);
Debug.Log($"Fleet morale: mean={fleetMorale.Mean:F2}, min={fleetMorale.Min:F2}");

// Calculate cohesion
float cohesion = AggregateStatsHelpers.CalculateCohesion(fleetMorale);
Debug.Log($"Fleet cohesion: {cohesion:F2}");

// === Space4X: Resource bottleneck detection ===
var supplyContributions = new NativeArray<StatContribution>(resourceCount, Allocator.Temp);
var resourceIds = new NativeArray<ushort>(resourceCount, Allocator.Temp);
var resourceEntities = new NativeArray<Entity>(resourceCount, Allocator.Temp);

for (int i = 0; i < resourceCount; i++)
{
    var supply = EntityManager.GetComponentData<SupplyStatus>(resources[i]);
    
    supplyContributions[i] = new StatContribution
    {
        Value = supply.TotalSupply / supply.MaxCapacity,  // Normalized ratio
        Weight = 1f,
        IsActive = 1
    };
    resourceIds[i] = (ushort)i;
    resourceEntities[i] = resources[i];
}

var bottlenecks = EntityManager.GetBuffer<BottleneckBuffer>(fleetEntity);
AggregateStatsHelpers.FindBottlenecks(
    supplyContributions, resourceIds, resourceEntities,
    0.3f,  // 30% threshold for bottleneck
    ref bottlenecks,
    currentTick);

if (AggregateStatsHelpers.TryGetWorstBottleneck(bottlenecks, out var worst))
{
    Debug.Log($"Worst bottleneck: resource {worst.ResourceTypeId}, deficit {worst.DeficitRatio:P0}");
}

// === Godgame: Village happiness aggregate ===
var villagerContributions = new NativeArray<StatContribution>(villagerCount, Allocator.Temp);

for (int i = 0; i < villagerCount; i++)
{
    var happiness = EntityManager.GetComponentData<Happiness>(villagers[i]);
    var profession = EntityManager.GetComponentData<Profession>(villagers[i]);
    
    villagerContributions[i] = new StatContribution
    {
        Value = happiness.Current,
        Weight = 1f,  // Equal weight for all villagers
        IsActive = (byte)(happiness.IsAlive ? 1 : 0),
        Category = (byte)profession.Type
    };
}

var villageHappiness = AggregateStatsHelpers.CalculateFullStats(villagerContributions, currentTick);

// Get composition
var config = new AggregateConfig
{
    HealthyThreshold = 0.6f,   // 60%+ is healthy
    CriticalThreshold = 0.2f,  // Below 20% is critical
    BottleneckThreshold = 0.3f,
    WeightByImportance = 0,
    IncludeInactive = 0
};

var composition = AggregateStatsHelpers.CalculateComposition(villagerContributions, config);
Debug.Log($"Village: {composition.HealthyCount} happy, {composition.CriticalCount} miserable");

// Per-profession breakdown
var professionCategories = EntityManager.GetBuffer<CompositionCategory>(villageEntity);
AggregateStatsHelpers.CalculateCategoryStats(villagerContributions, ref professionCategories);

for (int i = 0; i < professionCategories.Length; i++)
{
    var cat = professionCategories[i];
    Debug.Log($"  {cat.CategoryId}: {cat.Count} villagers, avg happiness {cat.AverageValue:F2}");
}
```

---

## Alternative Approaches Considered

- **Alternative 1**: Game-specific aggregation
  - **Rejected**: Mean, variance, bottleneck calculations are identical

- **Alternative 2**: Simple min/max only
  - **Rejected**: Games need weighted means and composition analysis

- **Alternative 3**: Full statistics library
  - **Rejected**: Too heavy - games need focused utilities

---

## Implementation Notes

**Dependencies:**
- Unity.Collections for NativeArray/HashMap
- Unity.Mathematics for math operations

**Performance Considerations:**
- All helpers are static and burst-compatible
- Uses NativeArray for batch processing
- Sorting uses simple algorithm suitable for small buffers
- HashMap uses Allocator.Temp for temporary calculations

**Related Requests:**
- Supply chain utilities (supply status aggregation)
- Morale system (morale aggregation)
- Threshold behavior triggers (bottleneck detection)

---

## Review Notes

*(PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

