# Extension Request: Regional & Spatial Modifiers System

**Status**: `[COMPLETED]`  
**Submitted**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Both games need location-based modifiers:

**Space4X:**
- Time flow variations (slower/faster regions)
- Nebulae affecting sensors and visibility
- Gravitic anomalies affecting movement
- Hazard zones dealing periodic damage
- Resource-rich regions with yield bonuses

**Godgame:**
- Terrain movement penalties (forest, mountains)
- Weather zones (rain, snow, heat)
- Magical ley lines with bonuses
- Cursed/corrupted areas with debuffs
- Fertile vs barren soil

Shared needs:
- Per-region modifier stacking
- Movement cost modifications
- Visibility/sensor modifications
- Hazard damage application
- Modifier decay over time

---

## Proposed Solution

**Extension Type**: New Components + Static Helpers

### Components

```csharp
/// <summary>
/// Type of spatial modifier.
/// </summary>
public enum SpatialModifierType : byte
{
    // Movement
    MovementSpeed = 0,
    MovementCost = 1,
    
    // Vision
    Visibility = 10,
    SensorRange = 11,
    Stealth = 12,
    
    // Combat
    AccuracyBonus = 20,
    DamageModifier = 21,
    DefenseModifier = 22,
    
    // Resources
    YieldMultiplier = 30,
    GatheringSpeed = 31,
    
    // Time
    TimeFlowRate = 40,
    
    // Health
    PeriodicDamage = 50,
    PeriodicHealing = 51,
    
    // Other
    MoraleModifier = 60,
    TechDiffusion = 61
}

/// <summary>
/// A spatial zone with modifiers.
/// </summary>
public struct SpatialZone : IComponentData
{
    public float3 Center;
    public float Radius;               // Spherical zone
    public float Height;               // For cylinder zones
    public byte ZoneShape;             // 0=sphere, 1=cylinder, 2=box
    public float FalloffStart;         // Where effect starts fading
    public byte IsActive;
}

/// <summary>
/// Modifier applied within a zone.
/// </summary>
[InternalBufferCapacity(4)]
public struct ZoneModifier : IBufferElementData
{
    public SpatialModifierType Type;
    public float Value;                // Additive or multiplier
    public byte IsMultiplier;          // 0=additive, 1=multiplier
    public float FalloffCurve;         // How quickly it fades at edges
}

/// <summary>
/// Regional trait (static characteristics).
/// </summary>
public struct RegionalTrait : IComponentData
{
    public FixedString32Bytes TraitName;
    public float BaseMovementMod;
    public float BaseVisibilityMod;
    public float BaseResourceMod;
    public float BaseHazardLevel;
    public byte IsTemporary;
    public uint ExpirationTick;
}

/// <summary>
/// Time flow modifier for region.
/// </summary>
public struct TimeFlowRegion : IComponentData
{
    public float TimeMultiplier;       // 0.5 = half speed, 2.0 = double
    public float StabilityFactor;      // How consistent the flow is
    public float AnomalyChance;        // Chance of temporal anomaly
    public uint LastAnomalyTick;
}

/// <summary>
/// Hazard zone dealing damage.
/// </summary>
public struct HazardZone : IComponentData
{
    public FixedString32Bytes HazardType;
    public float DamagePerTick;
    public float DamageInterval;       // Ticks between damage
    public float ResistanceType;       // What resists this
    public uint LastDamageTick;
}

/// <summary>
/// Accumulated modifiers on an entity from all zones.
/// </summary>
public struct AccumulatedSpatialModifiers : IComponentData
{
    public float MovementMod;          // Final movement multiplier
    public float VisibilityMod;        // Final visibility multiplier
    public float SensorMod;            // Final sensor multiplier
    public float DamageMod;            // Final damage multiplier
    public float TimeFlowMod;          // Final time multiplier
    public float YieldMod;             // Final resource yield multiplier
    public byte InHazard;              // Currently in hazard zone
}

/// <summary>
/// Weather/environmental condition overlay.
/// </summary>
public struct WeatherCondition : IComponentData
{
    public FixedString32Bytes ConditionType;
    public float Intensity;            // 0-1
    public float MovementPenalty;
    public float VisibilityPenalty;
    public float Duration;
    public uint StartTick;
}
```

### Static Helpers

```csharp
public static class SpatialModifierHelpers
{
    /// <summary>
    /// Checks if position is within zone.
    /// </summary>
    public static bool IsInZone(
        float3 position,
        in SpatialZone zone)
    {
        float3 offset = position - zone.Center;
        
        return zone.ZoneShape switch
        {
            0 => math.length(offset) <= zone.Radius, // Sphere
            1 => math.length(offset.xz) <= zone.Radius && 
                 math.abs(offset.y) <= zone.Height * 0.5f, // Cylinder
            2 => math.abs(offset.x) <= zone.Radius && 
                 math.abs(offset.y) <= zone.Height * 0.5f && 
                 math.abs(offset.z) <= zone.Radius, // Box
            _ => false
        };
    }

    /// <summary>
    /// Calculates falloff factor based on distance.
    /// </summary>
    public static float CalculateFalloff(
        float3 position,
        in SpatialZone zone,
        float falloffCurve)
    {
        float distance = math.length(position - zone.Center);
        
        if (distance <= zone.FalloffStart)
            return 1f;
        
        if (distance >= zone.Radius)
            return 0f;
        
        float falloffRange = zone.Radius - zone.FalloffStart;
        float falloffProgress = (distance - zone.FalloffStart) / falloffRange;
        
        // Apply curve (1 = linear, 2 = quadratic, etc.)
        return math.pow(1f - falloffProgress, falloffCurve);
    }

    /// <summary>
    /// Accumulates modifiers from all affecting zones.
    /// </summary>
    public static AccumulatedSpatialModifiers AccumulateModifiers(
        float3 position,
        NativeArray<SpatialZone> zones,
        NativeArray<DynamicBuffer<ZoneModifier>> zoneModifiers)
    {
        var result = new AccumulatedSpatialModifiers
        {
            MovementMod = 1f,
            VisibilityMod = 1f,
            SensorMod = 1f,
            DamageMod = 1f,
            TimeFlowMod = 1f,
            YieldMod = 1f,
            InHazard = 0
        };

        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i].IsActive == 0) continue;
            if (!IsInZone(position, zones[i])) continue;

            var modifiers = zoneModifiers[i];
            for (int j = 0; j < modifiers.Length; j++)
            {
                float falloff = CalculateFalloff(position, zones[i], modifiers[j].FalloffCurve);
                float effectValue = modifiers[j].Value * falloff;

                ApplyModifier(ref result, modifiers[j].Type, effectValue, modifiers[j].IsMultiplier != 0);
            }
        }

        return result;
    }

    private static void ApplyModifier(
        ref AccumulatedSpatialModifiers result,
        SpatialModifierType type,
        float value,
        bool isMultiplier)
    {
        switch (type)
        {
            case SpatialModifierType.MovementSpeed:
            case SpatialModifierType.MovementCost:
                if (isMultiplier) result.MovementMod *= value;
                else result.MovementMod += value;
                break;
            case SpatialModifierType.Visibility:
                if (isMultiplier) result.VisibilityMod *= value;
                else result.VisibilityMod += value;
                break;
            case SpatialModifierType.SensorRange:
                if (isMultiplier) result.SensorMod *= value;
                else result.SensorMod += value;
                break;
            case SpatialModifierType.DamageModifier:
                if (isMultiplier) result.DamageMod *= value;
                else result.DamageMod += value;
                break;
            case SpatialModifierType.TimeFlowRate:
                if (isMultiplier) result.TimeFlowMod *= value;
                else result.TimeFlowMod += value;
                break;
            case SpatialModifierType.YieldMultiplier:
                if (isMultiplier) result.YieldMod *= value;
                else result.YieldMod += value;
                break;
            case SpatialModifierType.PeriodicDamage:
                result.InHazard = 1;
                break;
        }
    }

    /// <summary>
    /// Calculates movement cost through zone.
    /// </summary>
    public static float CalculateMovementCost(
        float3 start,
        float3 end,
        in AccumulatedSpatialModifiers startMods,
        in AccumulatedSpatialModifiers endMods)
    {
        float distance = math.length(end - start);
        float avgMovementMod = (startMods.MovementMod + endMods.MovementMod) * 0.5f;
        
        // Movement mod < 1 = slower, > 1 = faster
        return distance / math.max(0.1f, avgMovementMod);
    }

    /// <summary>
    /// Calculates hazard damage this tick.
    /// </summary>
    public static float CalculateHazardDamage(
        in HazardZone hazard,
        float resistance,
        uint currentTick)
    {
        if (currentTick - hazard.LastDamageTick < hazard.DamageInterval)
            return 0;
        
        // Resistance reduces damage
        float damage = hazard.DamagePerTick * (1f - resistance);
        return math.max(0, damage);
    }

    /// <summary>
    /// Calculates time flow for region.
    /// </summary>
    public static float GetEffectiveTimeFlow(
        in TimeFlowRegion region,
        uint currentTick)
    {
        // Check for temporal anomaly
        bool inAnomaly = (currentTick - region.LastAnomalyTick) < 100;
        
        if (inAnomaly)
        {
            // Anomaly causes instability
            return region.TimeMultiplier * (0.5f + region.StabilityFactor * 0.5f);
        }
        
        return region.TimeMultiplier;
    }

    /// <summary>
    /// Applies weather penalty to modifiers.
    /// </summary>
    public static void ApplyWeather(
        ref AccumulatedSpatialModifiers mods,
        in WeatherCondition weather)
    {
        mods.MovementMod *= 1f - (weather.MovementPenalty * weather.Intensity);
        mods.VisibilityMod *= 1f - (weather.VisibilityPenalty * weather.Intensity);
    }

    /// <summary>
    /// Gets effective visibility range.
    /// </summary>
    public static float GetEffectiveVisibility(
        float baseVisibility,
        in AccumulatedSpatialModifiers mods)
    {
        return baseVisibility * mods.VisibilityMod;
    }

    /// <summary>
    /// Gets effective resource yield.
    /// </summary>
    public static float GetEffectiveYield(
        float baseYield,
        in AccumulatedSpatialModifiers mods)
    {
        return baseYield * mods.YieldMod;
    }
}
```

---

## Example Usage

```csharp
// === Space4X: Nebula zone effects ===
var nebulaZone = new SpatialZone
{
    Center = new float3(1000, 0, 1000),
    Radius = 500f,
    FalloffStart = 400f,
    ZoneShape = 0, // Sphere
    IsActive = 1
};

var nebulaModifiers = EntityManager.GetBuffer<ZoneModifier>(nebulaEntity);
nebulaModifiers.Add(new ZoneModifier
{
    Type = SpatialModifierType.SensorRange,
    Value = 0.5f,    // -50% sensor range
    IsMultiplier = 1,
    FalloffCurve = 1f
});
nebulaModifiers.Add(new ZoneModifier
{
    Type = SpatialModifierType.Stealth,
    Value = 0.3f,    // +30% stealth
    IsMultiplier = 0,
    FalloffCurve = 1f
});

// Get accumulated effects for ship
float3 shipPosition = GetPosition(shipEntity);
var mods = SpatialModifierHelpers.AccumulateModifiers(shipPosition, zones, zoneModifiers);

// Apply to ship sensors
float effectiveSensorRange = ship.BaseSensorRange * mods.SensorMod;

// Check time flow region
var timeRegion = EntityManager.GetComponentData<TimeFlowRegion>(regionEntity);
float timeFlow = SpatialModifierHelpers.GetEffectiveTimeFlow(timeRegion, currentTick);
ship.EffectiveTickRate = baseTickRate * timeFlow;

// === Godgame: Forest terrain ===
var forestZone = new SpatialZone
{
    Center = forestCenterPosition,
    Radius = 100f,
    FalloffStart = 90f,
    ZoneShape = 1, // Cylinder
    Height = 50f,
    IsActive = 1
};

var forestModifiers = EntityManager.GetBuffer<ZoneModifier>(forestEntity);
forestModifiers.Add(new ZoneModifier
{
    Type = SpatialModifierType.MovementCost,
    Value = 0.7f,    // 30% slower movement
    IsMultiplier = 1,
    FalloffCurve = 0.5f
});
forestModifiers.Add(new ZoneModifier
{
    Type = SpatialModifierType.Visibility,
    Value = 0.6f,    // 40% less visibility
    IsMultiplier = 1,
    FalloffCurve = 1f
});

// Calculate path cost
float3 start = villagerPosition;
float3 end = targetPosition;
var startMods = SpatialModifierHelpers.AccumulateModifiers(start, zones, zoneModifiers);
var endMods = SpatialModifierHelpers.AccumulateModifiers(end, zones, zoneModifiers);
float movementCost = SpatialModifierHelpers.CalculateMovementCost(start, end, startMods, endMods);

// Apply weather
var weather = new WeatherCondition
{
    ConditionType = "rain",
    Intensity = 0.7f,
    MovementPenalty = 0.2f,
    VisibilityPenalty = 0.3f
};
SpatialModifierHelpers.ApplyWeather(ref startMods, weather);

// Calculate resource yield
float baseHarvest = 10f;
float actualHarvest = SpatialModifierHelpers.GetEffectiveYield(baseHarvest, startMods);
```

---

## Alternative Approaches Considered

- **Alternative 1**: Hardcoded terrain types
  - **Rejected**: Need flexible zone composition and stacking

- **Alternative 2**: Game-specific modifiers
  - **Rejected**: Core mechanics (zones, falloff, stacking) are identical

---

## Implementation Notes

**Dependencies:**
- float3 for positions
- Spatial query system for zone detection

**Performance Considerations:**
- Zone checks can use spatial partitioning
- Cache accumulated modifiers per entity
- Batch updates when entities move

**Related Requests:**
- Stealth/perception system
- Time system (time flow integration)

---

## Review Notes

*(PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:

