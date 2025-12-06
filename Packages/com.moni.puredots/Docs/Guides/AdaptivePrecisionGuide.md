# Adaptive Data Precision Guide

**Purpose**: Guide for using `half` type for secondary stats to achieve 2× memory throughput.

## Overview

Replace secondary stat fields (temperature, morale) with `half` type. Burst and SIMD treat halfs as packable pairs, providing 2× memory throughput while maintaining conversion helpers.

## Core Utilities

### HalfPrecision Utility

```csharp
public static class HalfPrecision
{
    public static float HalfToFloat(half h) => (float)h;
    public static half FloatToHalf(float f) => (half)f;
    public static float2 Half2ToFloat2(half2 h) => new float2((float)h.x, (float)h.y);
    public static half2 Float2ToHalf2(float2 f) => new half2((half)f.x, (half)f.y);
}
```

Burst-compatible conversion helpers.

## Usage Pattern

### Replacing Float Fields with Half

**Before**:
```csharp
public struct VillagerStats : IComponentData
{
    public float Temperature;  // Secondary stat
    public float Morale;       // Secondary stat
    public float Health;       // Primary stat (keep as float)
}
```

**After**:
```csharp
using PureDOTS.Runtime.Utils;

public struct VillagerStats : IComponentData
{
    public half Temperature;  // Secondary stat → half
    public half Morale;        // Secondary stat → half
    public float Health;       // Primary stat (keep as float)
}
```

### Converting Values

```csharp
// Reading half values
var stats = SystemAPI.GetComponent<VillagerStats>(entity);
float temperature = HalfPrecision.HalfToFloat(stats.Temperature);
float morale = HalfPrecision.HalfToFloat(stats.Morale);

// Writing half values
stats.Temperature = HalfPrecision.FloatToHalf(newTemperature);
stats.Morale = HalfPrecision.FloatToHalf(newMorale);
```

### Using Half2 for Pairs

```csharp
public struct SecondaryStats : IComponentData
{
    public half2 TemperatureAndMorale; // Packed pair
}

// Reading
var stats = SystemAPI.GetComponent<SecondaryStats>(entity);
float2 values = HalfPrecision.Half2ToFloat2(stats.TemperatureAndMorale);
float temperature = values.x;
float morale = values.y;

// Writing
stats.TemperatureAndMorale = HalfPrecision.Float2ToHalf2(new float2(temperature, morale));
```

## When to Use Half

### Use Half For:
- **Secondary stats**: Temperature, morale, secondary resources
- **Display values**: UI values that don't need full precision
- **Approximate values**: Values where precision loss is acceptable

### Keep Float For:
- **Primary stats**: Health, position, velocity
- **Combat calculations**: Damage, accuracy, critical hits
- **Physics**: Positions, rotations, forces

## Best Practices

1. **Use for secondary stats**: Temperature, morale, secondary resources
2. **Keep primary stats as float**: Health, position, velocity
3. **Use conversion helpers**: Always use `HalfPrecision` utilities
4. **Burst-compatible**: All conversions are Burst-compatible
5. **SIMD-friendly**: Halfs pack into pairs for SIMD operations

## Performance Impact

- **2× memory throughput**: Halfs pack into pairs
- **SIMD optimization**: Burst treats halfs as packable pairs
- **Memory savings**: Significant memory reduction at million-entity scale
- **Cache efficiency**: Better cache utilization with smaller data

## References

- `PureDOTS/Packages/com.moni.puredots/Runtime/Utils/HalfPrecision.cs`

