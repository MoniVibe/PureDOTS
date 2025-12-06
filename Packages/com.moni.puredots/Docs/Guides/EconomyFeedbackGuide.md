# Economy & Ecology Feedback Guide

**Purpose**: Guide for self-regulating resource loops via feedback components.

## Overview

Couple economy and ecology via simple feedback: Over-harvest → soil-quality ↓ → yield ↓. Over-population → food ↓ → morale ↓ → migration ↑. Implemented with 3-4 scalar components per biome, updated in fixed steps.

## Core Components

### SoilQuality

```csharp
public struct SoilQuality : IComponentData
{
    public float Fertility;      // 0-1, affects yield
    public float Moisture;       // 0-1, affects growth
    public float NutrientLevel; // 0-1, affects health
    public float Pollution;     // 0-1, affects all above
}
```

Soil quality component for economy/ecology feedback (3-4 scalars per biome).

### PopulationPressure

```csharp
public struct PopulationPressure : IComponentData
{
    public float Density;        // Population per area
    public float FoodDemand;     // Food required per tick
    public float FoodAvailability; // Food available per tick
    public float PressureRatio;  // Demand / Availability
}
```

Population pressure component tracking over-population effects.

### ResourceYield

```csharp
public struct ResourceYield : IComponentData
{
    public float BaseYield;      // Base yield per harvest
    public float CurrentYield;   // Current yield (affected by soil quality)
    public float HarvestRate;     // Harvests per tick
    public float RegenerationRate; // Regeneration per tick
}
```

Resource yield component tracking harvest yields.

## Usage Pattern

### Feedback Loop: Over-Harvest → Soil Quality ↓ → Yield ↓

```csharp
[UpdateInGroup(typeof(EconomySystemGroup))]
public partial struct EconomyFeedbackSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Update soil quality based on harvest rate
        foreach (var (soilQuality, yield, entity) in SystemAPI.Query<
            RefRW<SoilQuality>, RefRO<ResourceYield>>().WithEntityAccess())
        {
            // Over-harvest reduces soil quality
            if (yield.ValueRO.HarvestRate > yield.ValueRO.RegenerationRate)
            {
                float overHarvestRatio = yield.ValueRO.HarvestRate / yield.ValueRO.RegenerationRate;
                soilQuality.ValueRW.Fertility = math.max(0f, 
                    soilQuality.ValueRO.Fertility - overHarvestRatio * 0.01f);
            }
            
            // Soil quality affects yield
            yield.ValueRW.CurrentYield = yield.ValueRO.BaseYield * soilQuality.ValueRO.Fertility;
        }
    }
}
```

### Feedback Loop: Over-Population → Food ↓ → Morale ↓ → Migration ↑

```csharp
// Update population pressure
foreach (var (pressure, entity) in SystemAPI.Query<RefRW<PopulationPressure>>().WithEntityAccess())
{
    pressure.ValueRW.PressureRatio = pressure.ValueRO.FoodDemand / 
        math.max(0.001f, pressure.ValueRO.FoodAvailability);
    
    // High pressure reduces morale
    if (pressure.ValueRO.PressureRatio > 1.0f)
    {
        // Apply morale penalty
        ApplyMoralePenalty(entity, pressure.ValueRO.PressureRatio);
        
        // Trigger migration if pressure too high
        if (pressure.ValueRO.PressureRatio > 2.0f)
        {
            TriggerMigration(entity);
        }
    }
}
```

## Integration Points

- **ResourceSystem**: Integrates with resource gathering and yield calculations
- **VillagerSystem**: Integrates with villager needs and morale
- **EconomySystemGroup**: Runs at 0.1Hz for economy updates

## Best Practices

1. **Update in fixed steps**: Use `EconomySystemGroup` for fixed-step updates
2. **Simple feedback loops**: Keep feedback logic simple and understandable
3. **Scalar components**: Use 3-4 scalars per biome for efficiency
4. **Self-balancing**: Let feedback loops self-regulate without scripting
5. **Integrate with existing systems**: Work with ResourceSystem and VillagerSystem

## Performance Impact

- **Self-balancing simulation**: Feedback loops self-regulate resource cycles
- **No explicit scripting**: Systems self-organize without manual tuning
- **Efficient**: Scalar components provide efficient feedback calculations

## References

- `PureDOTS/Packages/com.moni.puredots/Runtime/Components/Economy/EconomyFeedback.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Economy/EconomyFeedbackSystem.cs`

