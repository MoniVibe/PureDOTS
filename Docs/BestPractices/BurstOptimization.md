# Burst Optimization Guide

**Burst Version**: 1.8.24+
**Last Updated**: 2025-12-01
**Maintainer**: PureDOTS Framework Team

---

## Project Context

**This guide applies to all three projects:**

- **PureDOTS**: `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS` - Framework code
- **Space4X**: `C:\Users\Moni\Documents\claudeprojects\unity\Space4x` - Game code
- **Godgame**: `C:\Users\Moni\Documents\claudeprojects\unity\Godgame` - Game code

**⚠️ Important:** When writing PureDOTS framework code, ensure it remains game-agnostic. Burst optimization patterns apply to all projects.

See [PROJECT_SEPARATION.md](PROJECT_SEPARATION.md) for project separation rules.

---

## Overview

Burst compilation transforms C# code into highly optimized native code. This guide covers optimization techniques, common errors, and workflow patterns for PureDOTS development.

**Key Benefits:**
- ✅ 10-100x performance improvement
- ✅ SIMD vectorization (parallel operations)
- ✅ Zero GC allocations
- ✅ Cross-platform optimization

**Restrictions:**
- ❌ No managed types (classes, strings, delegates)
- ❌ No managed allocations
- ❌ Limited Unity API access
- ❌ No reflection

---

## Burst Compilation Checklist

### 1. Add `[BurstCompile]` Attribute

```csharp
[BurstCompile]  // On struct
public partial struct MySystem : ISystem
{
    [BurstCompile]  // On each method
    public void OnUpdate(ref SystemState state)
    {
        // Burst-compiled code
    }
}
```

### 2. Verify Burst Compilation

**Check Burst Inspector:**
- **Window → Burst → Burst Inspector**
- Select your system/method
- Look for green lines (Burst-compiled)
- Red lines indicate non-Burst code

### 3. Common Burst Errors

| Error | Cause | Fix |
|-------|-------|-----|
| **BC1016** | Managed function call | Remove managed code, use native alternatives |
| **BC1063** | Non-blittable bool in function pointer | Use `[MarshalAs(UnmanagedType.U1)]` or `byte` |
| **BC1064** | Struct passed by value | Use `in` modifier for parameters |
| **BC1091** | Static constructor with FixedString | Pre-define as `static readonly` outside Burst |

---

## SIMD Vectorization

### What is SIMD?

**SIMD** (Single Instruction, Multiple Data) processes multiple values in parallel.

**Example:** Instead of processing 4 floats one-by-one, process all 4 at once.

### Enabling Vectorization

**Burst automatically vectorizes loops when:**
- Loop iterates over arrays/NativeArrays
- Operations are independent
- No data dependencies between iterations

```csharp
[BurstCompile]
public void ProcessArray(NativeArray<float> values)
{
    // Burst will vectorize this loop (process 4-8 floats at once)
    for (int i = 0; i < values.Length; i++)
    {
        values[i] = values[i] * 2f + 1f;  // Independent operations
    }
}
```

### Vectorization Hints

**Use `Unity.Mathematics` for SIMD-friendly operations:**

```csharp
using Unity.Mathematics;

[BurstCompile]
public void VectorizedMath(float3[] positions, float3 offset)
{
    // math operations are SIMD-friendly
    for (int i = 0; i < positions.Length; i++)
    {
        positions[i] = math.normalize(positions[i] + offset);
    }
}
```

### Preventing Vectorization (When Needed)

**Sometimes you want sequential processing:**

```csharp
[BurstCompile]
[BurstDiscard]  // Disable Burst for this method
public void ManagedOperation()
{
    // This won't be Burst-compiled
}
```

---

## Loop Optimization Techniques

### 1. Minimize Branching

**❌ Bad: Complex branching**
```csharp
for (int i = 0; i < count; i++)
{
    if (condition1)
    {
        if (condition2)
        {
            if (condition3)
            {
                // Nested branches hurt performance
            }
        }
    }
}
```

**✅ Good: Flatten branches**
```csharp
for (int i = 0; i < count; i++)
{
    // Use math.select or ternary for simple branches
    var result = math.select(value1, value2, condition);
}
```

### 2. Avoid Function Calls in Hot Loops

**❌ Bad: Function call per iteration**
```csharp
for (int i = 0; i < count; i++)
{
    values[i] = ExpensiveFunction(values[i]);  // Function call overhead
}
```

**✅ Good: Inline operations**
```csharp
for (int i = 0; i < count; i++)
{
    values[i] = math.exp(values[i]);  // Inline math operation
}
```

### 3. Use Native Containers

**✅ NativeArray, NativeList, NativeHashMap:**
```csharp
[BurstCompile]
public void ProcessData(NativeArray<float> data)
{
    // Native containers are Burst-friendly
    for (int i = 0; i < data.Length; i++)
    {
        data[i] *= 2f;
    }
}
```

**❌ Avoid managed collections:**
```csharp
// This won't compile in Burst
List<float> data = new List<float>();  // Managed type!
```

---

## Branch Prediction Hints

### `[BurstHint]` Attribute

**Guide compiler optimization:**

```csharp
[BurstCompile]
public void ProcessWithHint(bool condition)
{
    [BurstHint(BurstHintOptions.Likely)]
    if (condition)
    {
        // Compiler optimizes for this path
    }
    else
    {
        // Less optimized path
    }
}
```

### Common Patterns

```csharp
// Likely path (common case)
[BurstHint(BurstHintOptions.Likely)]
if (health.Current > 0)
{
    // Most entities are alive
}

// Unlikely path (rare case)
[BurstHint(BurstHintOptions.Unlikely)]
if (health.Current <= 0)
{
    // Few entities are dead
}
```

---

## Avoiding Managed Types

### Common Managed Types to Avoid

| Managed Type | Burst Alternative |
|--------------|-------------------|
| `string` | `FixedString64Bytes`, `FixedString128Bytes` |
| `List<T>` | `NativeList<T>` |
| `Dictionary<K,V>` | `NativeHashMap<K,V>` |
| `Array` (managed) | `NativeArray<T>` |
| `delegate` | Function pointers (`delegate*`) |

### FixedString Patterns

**❌ Wrong: String constructor in Burst**
```csharp
[BurstCompile]
public void ProcessName()
{
    var name = new FixedString64Bytes("Hello");  // BC1016: Managed constructor!
}
```

**✅ Correct: Pre-define constants**
```csharp
// Outside Burst context (static field)
private static readonly FixedString64Bytes HelloString = "Hello";

[BurstCompile]
public void ProcessName()
{
    var name = HelloString;  // Just reference the constant
}
```

### Enum-to-String Conversion

**❌ Wrong: ToString() in Burst**
```csharp
[BurstCompile]
public void ProcessEnum(MyEnum value)
{
    var str = value.ToString();  // BC1016: Managed method!
}
```

**✅ Correct: Switch with pre-defined strings**
```csharp
private static readonly FixedString64Bytes EnumValue1 = "Value1";
private static readonly FixedString64Bytes EnumValue2 = "Value2";

[BurstCompile]
public void ProcessEnum(MyEnum value)
{
    var str = value switch
    {
        MyEnum.Value1 => EnumValue1,
        MyEnum.Value2 => EnumValue2,
        _ => default
    };
}
```

---

## Native Containers Best Practices

### Allocation Patterns

**✅ Use appropriate allocators:**

```csharp
[BurstCompile]
public void ProcessChunk(ArchetypeChunk chunk)
{
    // Temp allocator for short-lived data
    var tempArray = new NativeArray<float>(chunk.Count, Allocator.Temp);
    
    // Process...
    
    tempArray.Dispose();  // Must dispose!
}
```

### Allocator Types

| Allocator | Use Case | Lifetime |
|-----------|----------|----------|
| `Allocator.Temp` | Frame-scoped data | Dispose same frame |
| `Allocator.TempJob` | Job-scoped data | Dispose after job completes |
| `Allocator.Persistent` | Long-lived data | Dispose manually |

### Disposal Patterns

**✅ Always dispose native containers:**

```csharp
[BurstCompile]
public void OnDestroy(ref SystemState state)
{
    if (_nativeList.IsCreated)
    {
        _nativeList.Dispose();
    }
}
```

---

## Function Pointers for Polymorphism

### Burst-Compatible Delegates

**Use function pointers instead of delegates:**

```csharp
[BurstCompile]
public unsafe struct ProcessJob : IJobChunk
{
    public delegate*<float, float> ProcessFunction;  // Function pointer

    public void Execute(ArchetypeChunk chunk, ...)
    {
        var values = chunk.GetNativeArray(ref _valueComponent);
        
        for (int i = 0; i < chunk.Count; i++)
        {
            values[i] = ProcessFunction(values[i]);  // Burst-compatible call
        }
    }
}

// Static functions (required for function pointers)
[BurstCompile]
private static float SquareFunction(float x) => x * x;

[BurstCompile]
private static float DoubleFunction(float x) => x * 2f;

// Usage
new ProcessJob
{
    ProcessFunction = &SquareFunction  // Assign function pointer
}.ScheduleParallel(query, dependency);
```

### Benefits

- ✅ Burst-compatible (no managed delegates)
- ✅ Zero allocation
- ✅ Inlineable (compiler can optimize)
- ✅ Type-safe

---

## Burst Inspector Workflow

### 1. Open Burst Inspector

**Window → Burst → Burst Inspector**

### 2. Select System/Method

- Navigate to your system
- Select method to inspect
- View generated assembly code

### 3. Check Indicators

| Indicator | Meaning |
|-----------|---------|
| **Green lines** | Burst-compiled |
| **Red lines** | Not Burst-compiled (managed code) |
| **SIMD indicators** | Vectorized operations |
| **Loop unrolling** | Optimized loops |

### 4. Analyze Performance

- Check for unnecessary branches
- Verify SIMD vectorization
- Look for memory access patterns
- Identify optimization opportunities

---

## Common Burst Errors & Fixes

### BC1016: Managed Function Call

**Error:** `BC1016: A managed function is called...`

**Cause:** Calling managed code (string operations, Unity APIs, etc.)

**Fix:**
```csharp
// ❌ Wrong
var name = new FixedString64Bytes("Hello");

// ✅ Correct
private static readonly FixedString64Bytes HelloString = "Hello";
var name = HelloString;
```

### BC1063: Non-Blittable Bool in Function Pointer

**Error:** `BC1063: The type 'bool' cannot be used as a type argument...`

**Cause:** Bool fields in structs passed to function pointers

**Fix:**
```csharp
// ❌ Wrong
public struct Config { public bool Enabled; }

// ✅ Correct Option 1: Marshal bool
public struct Config 
{ 
    [MarshalAs(UnmanagedType.U1)] 
    public bool Enabled; 
}

// ✅ Correct Option 2: Use byte
public struct Config { public byte Enabled; }  // 0 = false, 1 = true
```

### BC1064: Struct Passed by Value

**Error:** `BC1064: A struct with a non-blittable field...`

**Cause:** Struct passed by value in external call

**Fix:**
```csharp
// ❌ Wrong
void Helper(Entity e, EntityCommandBuffer.ParallelWriter ecb) { }

// ✅ Correct
void Helper(in Entity e, in EntityCommandBuffer.ParallelWriter ecb) { }
```

### BC1091: Static Constructor with FixedString

**Error:** `BC1091: Static constructor is not supported...`

**Cause:** Static constructor initializing FixedString

**Fix:**
```csharp
// ❌ Wrong
static MyClass()
{
    MyString = new FixedString64Bytes("Hello");
}

// ✅ Correct
private static readonly FixedString64Bytes MyString = "Hello";
```

---

## Performance Patterns

### Pattern 1: Batch Processing

**Process data in batches for better cache utilization:**

```csharp
[BurstCompile]
public void ProcessBatch(NativeArray<float> data, int batchSize)
{
    for (int batchStart = 0; batchStart < data.Length; batchStart += batchSize)
    {
        int batchEnd = math.min(batchStart + batchSize, data.Length);
        
        // Process batch (better cache locality)
        for (int i = batchStart; i < batchEnd; i++)
        {
            data[i] = ProcessValue(data[i]);
        }
    }
}
```

### Pattern 2: Avoid Redundant Calculations

**Cache expensive calculations:**

```csharp
[BurstCompile]
public void ProcessWithCache(NativeArray<float3> positions, float3 offset)
{
    var normalizedOffset = math.normalize(offset);  // Calculate once
    
    for (int i = 0; i < positions.Length; i++)
    {
        positions[i] += normalizedOffset;  // Reuse cached value
    }
}
```

### Pattern 3: Use Math Functions

**Prefer `Unity.Mathematics` over `System.Math`:**

```csharp
using Unity.Mathematics;

// ✅ Good: Burst-optimized math
var result = math.sqrt(value);
var normalized = math.normalize(vector);

// ❌ Bad: System.Math (may not be Burst-optimized)
var result = System.Math.Sqrt(value);
```

---

## Debugging Burst Code

### Conditional Compilation

**Disable Burst for debugging:**

```csharp
#if UNITY_EDITOR && !BURST_DISABLE_DEBUG
    // Debug code (not Burst-compiled)
    Debug.Log($"Processing {count} entities");
#endif
```

### Burst Discard

**Temporarily disable Burst:**

```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    ProcessBurstCode();
    
    [BurstDiscard]
    DebugLogInfo();  // Not Burst-compiled
}
```

---

## Best Practices Summary

1. ✅ **Always use `[BurstCompile]`** on hot path systems
2. ✅ **Pre-define string constants** outside Burst context
3. ✅ **Use `in` modifier** for struct parameters
4. ✅ **Use native containers** (NativeArray, NativeList)
5. ✅ **Minimize branching** in hot loops
6. ✅ **Use function pointers** for polymorphism
7. ✅ **Check Burst Inspector** regularly
8. ✅ **Dispose native containers** properly
9. ❌ **Avoid managed types** in Burst code
10. ❌ **Don't use `ToString()`** or string constructors

---

## Additional Resources

- [Burst Compiler Manual](https://docs.unity3d.com/Packages/com.unity.burst@latest)
- [Unity Mathematics](https://docs.unity3d.com/Packages/com.unity.mathematics@latest)
- [DOTS 1.4 Patterns](BestPractices/DOTS_1_4_Patterns.md)
- [Foundation Guidelines](../FoundationGuidelines.md) - P8, P13 patterns

---

*Last Updated: 2025-12-01*
*Maintainer: PureDOTS Framework Team*

