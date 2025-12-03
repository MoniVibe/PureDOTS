# C# 9 Features for DOTS Development

**Unity Version**: 2022.3+ (C# 9 support)
**Last Updated**: 2025-12-01
**Maintainer**: PureDOTS Framework Team

---

## Project Context

**This guide applies to all three projects:**

- **PureDOTS**: `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS` - Framework code
- **Space4X**: `C:\Users\Moni\Documents\claudeprojects\unity\Space4x` - Game code
- **Godgame**: `C:\Users\Moni\Documents\claudeprojects\unity\Godgame` - Game code

**⚠️ Important:** When writing PureDOTS framework code, ensure it remains game-agnostic. C# 9 features can be used in all projects, but PureDOTS must not reference game-specific types.

---

## Overview

C# 9 introduces several features useful for DOTS development. This guide covers which features work with Burst, IL2CPP, and managed code contexts.

**Key Compatibility Notes:**
- Unity 2022.3+ supports C# 9
- Burst-compiled code has restrictions (no managed types)
- IL2CPP supports most C# 9 features
- Managed code (authoring, presentation) can use all features

---

## Records

### Use Cases in DOTS

**Records** are reference types ideal for:
- Data transfer objects (authoring → runtime)
- Configuration structures
- Event/message types

### Examples

```csharp
// Configuration record
public record ResourceConfig(
    int MaxQuantity,
    float BaseWeight,
    bool IsPerishable
);

// DTO for baking
public record EntitySpawnRequest(
    float3 Position,
    quaternion Rotation,
    Entity Prefab
);

// Event/message type
public record VillagerDeathEvent(
    Entity VillagerEntity,
    uint DeathTick,
    DeathCause Cause
);
```

### Limitations

**❌ NOT for IComponentData:**
- Records are reference types (classes)
- Components must be structs (`IComponentData` requires struct)
- Not Burst-compatible

**✅ Use in:**
- Authoring code (MonoBehaviour, ScriptableObject)
- Managed systems (SystemBase)
- Event queues (managed collections)
- Configuration data (ScriptableObject assets)

---

## Init-Only Setters

### Component Patterns

**Init-only setters** provide compile-time immutability for configuration fields:

```csharp
public struct ConfigurableComponent : IComponentData
{
    // Mutable hot data
    public float CurrentValue;

    // Config (set once, immutable after)
    public float MaxValue { get; init; }
    public float MinValue { get; init; }
}

// Usage in baker
public class ConfigurableBaker : Baker<ConfigurableAuthoring>
{
    public override void Bake(ConfigurableAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        
        AddComponent(entity, new ConfigurableComponent
        {
            MaxValue = authoring.maxValue,  // Set via init
            MinValue = authoring.minValue,  // Set via init
            CurrentValue = authoring.maxValue  // Mutable field
        });
        
        // Compile error: init-only property
        // component.MaxValue = 50;  // ❌ Not allowed
    }
}
```

### Benefits

- ✅ **Clear separation** of config vs state
- ✅ **Compile-time immutability** (prevents accidental modification)
- ✅ **Better intent communication** (config fields are obvious)
- ✅ **Burst-compatible** (works on structs)

---

## Pattern Matching Enhancements

### Switch Expressions in Systems

**Switch expressions** provide cleaner state machine logic:

```csharp
public partial struct AIDecisionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (ai, entity) in
            SystemAPI.Query<RefRW<AIState>>().WithEntityAccess())
        {
            // Switch expression with pattern matching
            ai.ValueRW.NextAction = ai.ValueRO.CurrentState switch
            {
                AIStateType.Idle when ai.ValueRO.Hunger > 80 => ActionType.Eat,
                AIStateType.Idle when ai.ValueRO.Danger > 50 => ActionType.Flee,
                AIStateType.Fleeing when ai.ValueRO.Danger < 20 => ActionType.Idle,
                AIStateType.Attacking when ai.ValueRO.TargetValid => ActionType.Attack,
                _ => ActionType.Idle  // Default case
            };
        }
    }
}
```

### Property Patterns

```csharp
// Pattern match on component properties
foreach (var health in SystemAPI.Query<RefRW<Health>>())
{
    var damageMultiplier = health.ValueRO switch
    {
        { Current: <= 0 } => 0f,  // Dead
        { Current: < 25 } => 1.5f,  // Critical
        { Current: < 50 } => 1.2f,  // Low
        _ => 1f  // Normal
    };
}
```

### Benefits

- ✅ **Cleaner state machine logic** (less boilerplate)
- ✅ **Better compile-time checking** (exhaustiveness)
- ✅ **Easier to read intent** (declarative style)
- ✅ **Burst-compatible** (works in hot paths)

---

## Target-Typed New

### Component Initialization

**Target-typed `new`** reduces boilerplate:

```csharp
// Before (C# 8)
var buffer = new DynamicBuffer<ResourceElement>();
var query = new EntityQuery(desc);

// After (C# 9)
DynamicBuffer<ResourceElement> buffer = new();
EntityQuery query = new(desc);
```

### EntityCommandBuffer Usage

```csharp
// Cleaner ECB commands
ecb.AddComponent(entity, new HealthComponent { Value = 100 });
// becomes
ecb.AddComponent(entity, new() { Value = 100 });

// With multiple properties
ecb.AddComponent(entity, new MoveSpeed 
{ 
    Value = 5f,
    SprintMultiplier = 2f 
});
// becomes
ecb.AddComponent(entity, new() 
{ 
    Value = 5f,
    SprintMultiplier = 2f 
});
```

### Benefits

- ✅ **Less boilerplate** (type inferred from context)
- ✅ **Cleaner code** (especially with property initializers)
- ✅ **Burst-compatible** (syntactic sugar, no runtime cost)

---

## Top-Level Statements

### Bootstrap Entry Points

**Top-level statements** eliminate boilerplate for simple entry points:

```csharp
// BootstrapEntry.cs (PureDOTS framework)
using Unity.Entities;
using PureDOTS.Bootstrap;

// No class wrapper needed for simple entry points
BootstrapCoordinator.Initialize();
UnityEngine.Debug.Log("PureDOTS Bootstrap Complete");

// Note: Game projects (Space4X, Godgame) have their own bootstrap
// located in their respective project directories

// Helper methods below if needed
static void ConfigureDefaultWorld() 
{
    // Configuration logic
}
```

### Use Cases

- ✅ Simple bootstrap scripts
- ✅ Test runners
- ✅ Build automation scripts
- ✅ One-off utility scripts

### Limitations

- ❌ **Not Burst-compatible** (managed code only)
- ❌ **One per file** (only one top-level entry point)
- ❌ **Limited to simple scripts** (complex logic needs classes)

---

## Function Pointers (Burst-Compatible)

### Polymorphic Behavior in Jobs

**Function pointers** provide Burst-compatible polymorphism:

```csharp
[BurstCompile]
public unsafe struct PolymorphicJob : IJobChunk
{
    public delegate*<float, float> ProcessFunction;

    public void Execute(ArchetypeChunk chunk, int chunkIndex, 
        int firstEntityIndex)
    {
        var values = chunk.GetNativeArray(ref _valueComponent);
        
        for (int i = 0; i < chunk.Count; i++)
        {
            // Call function pointer (Burst-compatible)
            values[i] = ProcessFunction(values[i]);
        }
    }
}

// Usage
[BurstCompile]
private static float SquareFunction(float x) => x * x;

[BurstCompile]
private static float DoubleFunction(float x) => x * 2f;

// Schedule with different functions
new PolymorphicJob
{
    ProcessFunction = &SquareFunction
}.ScheduleParallel(query, dependency);

// Or
new PolymorphicJob
{
    ProcessFunction = &DoubleFunction
}.ScheduleParallel(query, dependency);
```

### Benefits vs Limitations

**✅ Benefits:**
- Burst-compatible (no managed delegates)
- Zero allocation (no GC pressure)
- Inlineable (compiler can optimize)
- Type-safe (compile-time checked)

**❌ Limitations:**
- No capturing (must be static methods)
- Unsafe context required (`unsafe` keyword)
- No instance methods (static only)
- Less flexible than delegates

---

## Module Initializers

### Static Registration

**Module initializers** run before any code in the assembly:

```csharp
public static class SystemRegistry
{
    private static readonly List<Type> RegisteredSystems = new();

    [ModuleInitializer]
    internal static void RegisterSystems()
    {
        // Runs before any code in assembly
        RegisterSystemType<VillagerAISystem>();
        RegisterSystemType<ResourceHarvestSystem>();
        RegisterSystemType<CombatSystem>();
    }

    private static void RegisterSystemType<T>() where T : ISystem
    {
        RegisteredSystems.Add(typeof(T));
    }
}
```

### Use Cases

- ✅ System registration (before World creation)
- ✅ Static lookups initialization
- ✅ Registry population
- ✅ Global configuration setup

### Limitations

- ❌ **Not Burst-compatible** (managed code only)
- ❌ **Runs before Domain reload** (Unity-specific timing)
- ❌ **One per module** (use carefully)

---

## Covariant Returns

### Type Hierarchy Helpers

**Covariant returns** allow derived classes to return more specific types:

```csharp
public abstract class ComponentBaker<T> : MonoBehaviour
{
    public abstract IComponentData GetComponent();
}

public class VillagerBaker : ComponentBaker<VillagerId>
{
    // Covariant return (C# 9) - returns VillagerId, not IComponentData
    public override VillagerId GetComponent()
        => new VillagerId { Value = GetInstanceID() };
}

// Before C# 9, would need to return IComponentData and cast
```

### Benefits

- ✅ **Type safety** (no casting needed)
- ✅ **Cleaner APIs** (more specific return types)
- ✅ **Better IntelliSense** (IDE knows exact type)

### Limitations

- ❌ **Not Burst-compatible** (managed code only)
- ❌ **Reference types only** (classes, interfaces)
- ❌ **Limited use cases** (mainly for authoring code)

---

## Best Practices Summary

1. **✅ Use records** for DTOs and config objects (authoring/managed code)
2. **✅ Use init setters** for component config fields (Burst-compatible)
3. **✅ Use pattern matching** for state machines (Burst-compatible)
4. **✅ Use target-typed new** for clarity (Burst-compatible)
5. **✅ Use function pointers** for polymorphism in Burst (Burst-compatible)
6. **✅ Use module initializers** for static setup (managed only)
7. **❌ Avoid** records/classes in hot paths (use structs)

---

## Compatibility Matrix

| Feature | Burst | IL2CPP | Managed | Notes |
|---------|-------|--------|---------|-------|
| Records | ❌ | ✅ | ✅ | Reference types only |
| Init setters | ✅ | ✅ | ✅ | Works on structs |
| Pattern matching | ✅ | ✅ | ✅ | Full support |
| Target-typed new | ✅ | ✅ | ✅ | Full support |
| Function pointers | ✅ | ✅ | ✅ | Burst-compatible |
| Module initializers | ❌ | ✅ | ✅ | Runs before Domain |
| Covariant returns | ❌ | ✅ | ✅ | Managed only |
| Top-level statements | ❌ | ✅ | ✅ | Managed only |

---

## Common Patterns

### Pattern 1: Config Component with Init Setters

```csharp
public struct WeaponConfig : IComponentData
{
    public float Damage { get; init; }
    public float Range { get; init; }
    public float FireRate { get; init; }
    
    // Mutable runtime state
    public float CooldownTimer;
}

// In baker
AddComponent(entity, new WeaponConfig
{
    Damage = authoring.damage,
    Range = authoring.range,
    FireRate = authoring.fireRate,
    CooldownTimer = 0f
});
```

### Pattern 2: Function Pointer for Damage Calculation

```csharp
[BurstCompile]
public unsafe struct DamageCalculationJob : IJobEntity
{
    public delegate*<float, float, float> CalculateDamage;

    [BurstCompile]
    private void Execute([ChunkIndexInQuery] int sortKey, 
        ref Health health, in DamageDealt damage)
    {
        var finalDamage = CalculateDamage(damage.Base, health.Armor);
        health.Current -= finalDamage;
    }
}

[BurstCompile]
private static float LinearDamage(float baseDamage, float armor)
    => math.max(0, baseDamage - armor);

[BurstCompile]
private static float ExponentialDamage(float baseDamage, float armor)
    => baseDamage * math.exp(-armor / 100f);

// Usage
new DamageCalculationJob
{
    CalculateDamage = &LinearDamage  // or &ExponentialDamage
}.ScheduleParallel();
```

### Pattern 3: Record for Event Messages

```csharp
// Event record (managed code)
public record ResourceHarvestedEvent(
    Entity HarvesterEntity,
    Entity ResourceEntity,
    ResourceTypeId ResourceType,
    int Quantity,
    uint Tick
);

// In managed system
public partial class EventSystem : SystemBase
{
    private Queue<ResourceHarvestedEvent> _eventQueue = new();

    protected override void OnUpdate()
    {
        // Process events (managed code)
        while (_eventQueue.Count > 0)
        {
            var evt = _eventQueue.Dequeue();
            // Handle event...
        }
    }
}
```

---

## Additional Resources

- [C# 9 Specification](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-9)
- [Unity C# Language Version](https://docs.unity3d.com/Manual/CSharpCompiler.html)
- [Burst Compiler Restrictions](https://docs.unity3d.com/Packages/com.unity.burst@latest/manual/csharp-overview.html)

---

*Last Updated: 2025-12-01*
*Maintainer: PureDOTS Framework Team*

