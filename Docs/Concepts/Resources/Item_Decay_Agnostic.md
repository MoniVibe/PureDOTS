# Item Decay and Preservation (Agnostic Framework)

**Status:** Concept Design
**Category:** Core Resource Framework
**Shareability:** `shared-core`
**Last Updated:** 2025-12-07

---

## Overview

The **Item Decay Framework** provides agnostic mechanics for time-based degradation of perishable items and preservation methods to slow decay. Games implement specific item types, preservation methods, and environmental factors while PureDOTS provides the decay calculation algorithms, container tracking, and quality degradation formulas.

**Agnostic Aspects** (Provided by PureDOTS):
- ✅ Decay rate calculation framework
- ✅ Environmental modifier formulas
- ✅ Container preservation tracking
- ✅ Decay stage progression (fresh → rotten → destroyed)
- ✅ Quality loss from decay
- ✅ Preservation method lifespan multiplication

**Game-Specific Aspects** (Implemented by Games):
- Item types (food, organs, biological samples, cargo)
- Preservation methods (salting, smoking, magical cooling, refrigeration)
- Environmental factors (temperature, exposure, atmosphere)
- Container types (boxes, vaults, refrigeration units, stasis chambers)
- Visual presentation (decay stages, spoilage VFX)

---

## Core Agnostic Components

### PerishableItem (Body ECS)
```csharp
/// <summary>
/// Agnostic perishable item component
/// </summary>
public struct PerishableItem : IComponentData
{
    public byte ItemTypeId;                // Game-defined enum
    public float FreshnessPercent;         // 100% = fresh, 0% = destroyed
    public float BaseDecayRate;            // Percent per hour
    public float CurrentDecayRate;         // With all modifiers applied

    public uint CreatedTick;
    public uint LastDecayUpdateTick;

    public byte DecayStageId;              // Game-defined enum (Fresh/Aging/Spoiling/Rotten)
}
```

### PreservationContainer (Body ECS)
```csharp
/// <summary>
/// Agnostic container for preserving items
/// </summary>
public struct PreservationContainer : IComponentData
{
    public byte ContainerTypeId;           // Game-defined enum
    public float DecayModifier;            // 0.1 = 10% decay rate (90% reduction)
    public int Capacity;
    public int CurrentItems;

    // Power requirements (optional)
    public bool RequiresPower;
    public float PowerConsumption;
    public bool HasPower;

    // Temperature control (optional)
    public float Temperature;              // Game-defined units
    public bool IsTemperatureControlled;
}
```

### ItemStorage (Body ECS Buffer)
```csharp
/// <summary>
/// Agnostic item storage tracking
/// </summary>
[InternalBufferCapacity(16)]
public struct ItemStorage : IBufferElementData
{
    public Entity Item;
    public uint StoredTick;
}
```

### PreservationMethod (Mind ECS)
```csharp
/// <summary>
/// Agnostic preservation method
/// </summary>
public struct PreservationMethod : IComponentData
{
    public byte MethodTypeId;              // Game-defined enum
    public float LifespanMultiplier;       // 3x, 5x, 10x
    public float ApplicationTime;          // Hours
    public bool IsPermanent;
}
```

---

## Agnostic Algorithms

### Base Decay Rate Calculation
```csharp
/// <summary>
/// Calculate base decay rate for item type
/// Agnostic: Games provide rates per type
/// </summary>
public static float GetBaseDecayRate(byte itemTypeId, float[] decayRateTable)
{
    // Games provide table: itemTypeId → decay rate %/hour
    return decayRateTable[itemTypeId];
}
```

### Environmental Modifier Calculation
```csharp
/// <summary>
/// Calculate environmental decay modifier
/// Agnostic: Temperature, exposure, location
/// </summary>
public static float CalculateEnvironmentalModifier(
    float temperature,       // Game-defined scale
    float optimalTemp,       // Ideal temperature
    float tempSensitivity,   // How much temp affects decay
    bool isExposed,
    bool isProtected)
{
    // Temperature deviation from optimal
    float tempDeviation = math.abs(temperature - optimalTemp);
    float tempModifier = 1.0f + (tempDeviation * tempSensitivity);

    // Exposure modifier
    float exposureModifier = 1.0f;
    if (isExposed)
        exposureModifier = 2.5f;
    else if (isProtected)
        exposureModifier = 0.75f;

    return tempModifier * exposureModifier;
}
```

### Container Modifier Calculation
```csharp
/// <summary>
/// Calculate container preservation modifier
/// Agnostic: Container type, power state
/// </summary>
public static float CalculateContainerModifier(
    float containerDecayModifier,  // 0-1 (from container definition)
    bool requiresPower,
    bool hasPower)
{
    // If requires power but doesn't have it, decay accelerates
    if (requiresPower && !hasPower)
        return 1.5f; // 150% decay (spoiling faster)

    return containerDecayModifier;
}
```

### Final Decay Rate Calculation
```csharp
/// <summary>
/// Calculate final decay rate from all factors
/// Agnostic: Base × environmental × container × preservation
/// </summary>
public static float CalculateFinalDecayRate(
    float baseDecayRate,
    float environmentalModifier,
    float containerModifier,
    float preservationMultiplier)   // 0-1 (1/lifespan multiplier)
{
    float decayRate = baseDecayRate * environmentalModifier * containerModifier;

    // Preservation reduces decay (divide by multiplier)
    if (preservationMultiplier > 1.0f)
        decayRate /= preservationMultiplier;

    return decayRate;
}
```

### Decay Update
```csharp
/// <summary>
/// Update item freshness based on decay rate
/// Agnostic: Decay progression over time
/// </summary>
public static float UpdateFreshness(
    float currentFreshness,    // 0-100%
    float decayRate,           // %/hour
    float deltaTimeHours)      // Time since last update
{
    float decay = decayRate * deltaTimeHours;
    float newFreshness = currentFreshness - decay;
    return math.max(0f, newFreshness);
}
```

### Decay Stage Determination
```csharp
/// <summary>
/// Determine decay stage from freshness
/// Agnostic: Thresholds for stage transitions
/// </summary>
public static byte DetermineDecayStage(
    float freshnessPercent,
    float[] stageThresholds)    // Game-defined thresholds [80, 50, 20, 5]
{
    if (freshnessPercent >= stageThresholds[0])
        return 0; // Fresh

    if (freshnessPercent >= stageThresholds[1])
        return 1; // Aging

    if (freshnessPercent >= stageThresholds[2])
        return 2; // Spoiling

    if (freshnessPercent >= stageThresholds[3])
        return 3; // Rotten

    return 4; // Destroyed
}
```

### Quality Loss from Decay
```csharp
/// <summary>
/// Calculate quality multiplier from decay stage
/// Agnostic: Quality degradation curve
/// </summary>
public static float CalculateQualityFromDecay(byte decayStage)
{
    switch (decayStage)
    {
        case 0: // Fresh
            return 1.0f;

        case 1: // Aging
            return 0.8f;

        case 2: // Spoiling
            return 0.4f;

        case 3: // Rotten
            return 0.1f;

        case 4: // Destroyed
            return 0.0f;

        default:
            return 1.0f;
    }
}
```

---

## Extension Points for Games

### 1. Perishable Item Type Definitions
Games define what items decay:
```csharp
// Godgame example
public enum GodgamePerishableType : byte
{
    Food,
    OrganicMaterial,
    GraftingLimb,
    Herb,
    AlchemicalReagent,
}

// Space4X example
public enum Space4XPerishableType : byte
{
    BiologicalComponent,
    TissueSample,
    AlienOrganism,
    PerishableCargo,
}
```

### 2. Decay Stage Definitions
Games define decay stages:
```csharp
// Godgame example
public enum GodgameDecayStage : byte
{
    Fresh,
    Aging,
    Spoiling,
    Rotten,
    Destroyed,
}

// Space4X example
public enum Space4XDecayStage : byte
{
    Pristine,
    Degraded,
    Contaminated,
    Unusable,
}
```

### 3. Container Type Definitions
Games define preservation containers:
```csharp
// Godgame example
public enum GodgameContainerType : byte
{
    None,
    BasicBox,
    SealedContainer,
    EnchantedColdBox,
    FrostVault,
}

// Space4X example
public enum Space4XContainerType : byte
{
    None,
    CargoHold,
    RefrigeratedModule,
    CryoStorage,
    QuantumStasis,
}
```

### 4. Preservation Method Definitions
Games define preservation techniques:
```csharp
// Godgame example
public enum GodgamePreservationType : byte
{
    None,
    Salting,
    Smoking,
    Drying,
    MagicalCooling,
}

// Space4X example
public enum Space4XPreservationType : byte
{
    None,
    Refrigeration,
    CryoPreservation,
    StasisField,
}
```

---

## Integration Requirements

### Systems Games Must Implement

1. **ItemDecaySystem** (Body ECS, 60 Hz or 1 Hz)
   - Update freshness for all perishable items
   - Calculate decay rates from environmental factors
   - Update decay stages
   - Destroy items at 0% freshness

2. **ContainerTemperatureSystem** (Body ECS, 60 Hz)
   - Manage temperature for temperature-controlled containers
   - Handle power failures (refrigeration units)
   - Apply cooling/heating effects

3. **PreservationApplicationSystem** (Mind ECS, 1 Hz)
   - Apply preservation methods to items (salting, smoking, etc.)
   - Track preservation duration (permanent vs temporary)
   - Calculate lifespan extensions

4. **DecayWarningSystem** (Mind ECS, 1 Hz)
   - Warn about imminent spoilage (freshness < 20%)
   - Notify about destroyed items
   - Alert about container failures

5. **QualityDegradationSystem** (Mind ECS, 1 Hz)
   - Apply quality penalties based on decay stage
   - Prevent use of rotten items (grafting, consumption)
   - Adjust market value by freshness

---

## Data Contracts

Games must provide:
- Item type decay rate table (itemTypeId → %/hour)
- Environmental factor definitions (temperature, exposure effects)
- Container decay modifier table (containerTypeId → modifier)
- Preservation method lifespan table (methodTypeId → multiplier)
- Decay stage thresholds (freshness % → stage)
- Quality loss curves (stage → quality multiplier)

---

## Game-Specific Implementations

### Godgame (Organic Decay)
**Full Implementation:** [Item_Decay_And_Preservation_System.md](../../../../Godgame/Docs/Concepts/Resources/Item_Decay_And_Preservation_System.md)

**Perishable Items:** Food, grafting limbs, herbs, organs
**Preservation:** Salting (3x), smoking (5x), drying (4x), magical cooling (10x), enchanted vaults (20x)
**Decay Factors:** Temperature, exposure, containers
**Example:** Dragon arm 48 hours fresh, 20 days with ice spell

### Space4X (Biological Sample Decay)
**Implementation Reference:** TBD

**Perishable Items:** Tissue samples, alien organisms, bio-materials, cargo
**Preservation:** Refrigeration (10x), cryo-storage (50x), stasis fields (infinite)
**Decay Factors:** Power availability, temperature, atmosphere
**Example:** Tissue samples 24 hours fresh, 30 days frozen, infinite in stasis

---

## Performance Targets

**Body ECS (60 Hz) Budget:** 1-2 ms/frame
- Decay updates: 1.0 ms (batch 100 items/frame)
- Temperature management: 0.5 ms (containers only)
- Power failure events: 0.5 ms (rare)

**Mind ECS (1 Hz) Budget:** 15-20 ms/update
- Preservation application: 5 ms
- Container management: 10 ms
- Decay warnings: 5 ms

**Optimization Strategies:**
- Batch decay updates (100 items per frame, not all at once)
- LOD decay (reduce update frequency for distant/frozen items)
- Event-based temperature (only recalculate when changed)
- Frozen item pooling (items in stasis don't update)

---

## Testing Guidelines

### Unit Tests (PureDOTS)
- ✅ Base decay rate retrieval (itemTypeId → rate)
- ✅ Environmental modifier (temperature, exposure)
- ✅ Container modifier (decay reduction, power failures)
- ✅ Final decay rate (all factors combined)
- ✅ Freshness update (decay over time)
- ✅ Decay stage transitions (freshness → stage)
- ✅ Quality loss (stage → quality multiplier)

### Integration Tests (Games)
- Items decay at correct rates
- Containers slow decay appropriately
- Power failures accelerate decay
- Preservation methods extend lifespan correctly
- Rotten items cannot be used (grafting, consumption)

---

## Migration Notes

**New Components Required:**
- `PerishableItem` (Body ECS)
- `PreservationContainer` (Body ECS)
- `ItemStorage` buffer (Body ECS)
- `PreservationMethod` (Mind ECS)

**Integration with Existing Systems:**
- Resource system (perishable vs non-perishable tracking)
- Container/inventory system (storage capacity)
- Crafting system (preservation method application)
- Power system (refrigeration power consumption)
- Market/economy system (freshness affects pricing)

---

## Related Documents

**PureDOTS Agnostic:**
- `Docs/Architecture/ThreePillarECS_Architecture.md` - ECS layers (to be created)
- `Docs/BestPractices/DOTS_1_4_Patterns.md` - DOTS coding patterns

**Game Implementations:**
- `Godgame/Docs/Concepts/Resources/Item_Decay_And_Preservation_System.md` - Full game-side concept
- `Space4X/Docs/Concepts/Resources/Biological_Sample_Preservation.md` - Space variant (to be created)

---

**Last Updated:** 2025-12-07
**Maintainer:** PureDOTS Core Team
**Status:** Awaiting Implementation
