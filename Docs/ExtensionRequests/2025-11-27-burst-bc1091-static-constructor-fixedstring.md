# Extension Request: Fix Burst BC1091 Static Constructor Errors (FixedString)

**Status**: `[RESOLVED]`  
**Submitted**: 2025-11-27  
**Game Project**: Godgame  
**Priority**: P0  
**Assigned To**: TBD

---

## Use Case

Godgame compilation is **completely blocked** by Burst BC1091 errors. The previous BC1016 fix attempt used `static readonly FixedString*` fields, which triggers static constructors that Burst cannot compile:

```
Burst error BC1091: External and internal calls are not allowed inside static constructors: 
  System.Runtime.CompilerServices.RuntimeHelpers.get_OffsetToStringData()
  at Unity.Collections.FixedStringMethods.CopyFromTruncated(...)
  at PureDOTS.Systems.Combat.HazardEmitFromDamageSystem..cctor()
```

**60+ systems** are failing to compile due to these 4 files.

---

## Proposed Solution

**Extension Type**: Bug Fix

**Details:**

The pattern `private static readonly FixedString32Bytes Name = new FixedString32Bytes("value")` **does NOT work** because it creates a static constructor (.cctor) which Burst tries to compile.

### Correct Fix: Initialize in OnCreate (Non-Burst context)

```csharp
// ❌ WRONG - static constructor triggers BC1091
public partial struct SomeSystem : ISystem
{
    private static readonly FixedString32Bytes s_Label = new("some_text");
    // ERROR: BC1091 - static constructor called in Burst context
}

// ✅ CORRECT - initialize as instance field in OnCreate
public partial struct SomeSystem : ISystem
{
    private FixedString32Bytes _label;
    
    public void OnCreate(ref SystemState state)
    {
        _label = new FixedString32Bytes("some_text");  // Safe - OnCreate is not Burst-compiled
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Use already-initialized field
        new SomeJob { Label = _label }.Schedule();
    }
}

partial struct SomeJob : IJobEntity
{
    public FixedString32Bytes Label;  // Passed from system
    void Execute(...) { /* use Label */ }
}
```

### For Static Classes: Use Helper Methods

```csharp
// ❌ WRONG - static class with static readonly
public static class AggregateHelpers
{
    private static readonly FixedString32Bytes s_PseudoPrefix = new("pseudo_");
}

// ✅ CORRECT - build strings with Append (Burst-compatible)
public static class AggregateHelpers
{
    public static FixedString32Bytes GetPseudoPrefix()
    {
        FixedString32Bytes result = default;
        result.Append((FixedString32Bytes)"pseudo_");
        return result;
    }
}
```

---

## Impact Assessment

**Files/Systems Affected:**

| File | .cctor Line | Usage Line | Issue |
|------|-------------|------------|-------|
| `Runtime/Runtime/Registry/Aggregates/AggregateHelpers.cs` | 17 | 189 | Static constructor from `static readonly` field |
| `Runtime/Systems/Combat/HazardEmitFromDamageSystem.cs` | 76 | 93 | Static constructor from `static readonly` field |
| `Runtime/Systems/Ships/LifeBoatEjectorSystem.cs` | 53 | 73 | Static constructor from `static readonly` field |
| `Runtime/Systems/Spells/SchoolFoundingSystem.cs` | 122 | 139 | Static constructor from `static readonly` field |

**Breaking Changes:**
- No - fixes are internal implementation changes
- Previous `static readonly` fields need to become instance fields or helper methods

**Affected Systems (60+):**
- `PseudoHistorySystem`, `SatisfyNeedSystem`, `RelationInteractionSystem`, `TelemetryTrendSystem`
- `CompressionSystem`, `MoraleBandSystem`, `BalanceAnalysisSystem`, `AggregateRegistrySystem`
- `HazardEmitFromDamageSystem`, `LifeBoatEjectorSystem`, `SchoolFoundingSystem`
- `SpellEffectExecutionSystem`, `HybridizationSystem`, `SpellLearningSystem`
- ... and 50+ more systems in the compile graph

---

## Example Error Stack

```
Burst error BC1091: External and internal calls are not allowed inside static constructors: 
  System.Runtime.CompilerServices.RuntimeHelpers.get_OffsetToStringData()

 at Unity.Collections.FixedStringMethods.CopyFromTruncated(ref Unity.Collections.FixedString32Bytes fs, System.String s)
 at Unity.Collections.FixedString32Bytes.Initialize(...)
 at Unity.Collections.FixedString32Bytes..ctor(...)
 at PureDOTS.Runtime.Registry.Aggregates.AggregateHelpers..cctor()  ← STATIC CONSTRUCTOR
 at PureDOTS.Runtime.Registry.Aggregates.AggregateHelpers.GeneratePseudoHistory(...)
 at PureDOTS.Runtime.Systems.Registry.Aggregates.PseudoHistorySystem.OnUpdate(...)
```

---

## Alternative Approaches Considered

- **Alternative 1**: Keep static readonly fields
  - **Rejected**: Causes BC1091 errors - Burst cannot compile static constructors

- **Alternative 2**: Use SharedStatic<T>
  - **Viable**: But adds complexity; OnCreate pattern is simpler

- **Alternative 3**: Pre-compute byte arrays
  - **Complex**: Requires manual UTF8 encoding

---

## Implementation Notes

**Required Changes:**

1. **AggregateHelpers.cs:17** (static class)
   - Remove `static readonly FixedString*` fields
   - Add helper methods that build FixedStrings using `Append()` (Burst-compatible)
   - Example: `GetPseudoPrefix()`, `GetHarvestEventType()`, etc.

2. **HazardEmitFromDamageSystem.cs:76** (ISystem struct)
   - Remove `static readonly` fields
   - Add instance fields (e.g., `_reactorIdPattern`, `_engineIdPattern`)
   - Initialize in `OnCreate`
   - Pass to job as `[ReadOnly]` fields

3. **LifeBoatEjectorSystem.cs:53** (ISystem struct)
   - Remove `static readonly` field
   - Add instance field `_bridgeIdPattern`
   - Initialize in `OnCreate`
   - Pass to job as `[ReadOnly]` field

4. **SchoolFoundingSystem.cs:122** (ISystem struct)
   - Remove `static readonly` field
   - Add instance field `_schoolIdPrefix`
   - Initialize in `OnCreate`
   - Pass to job as `[ReadOnly]` field

**Testing Requirements:**
```bash
# After fixes, verify no BC1091 errors:
Unity -projectPath "$(pwd)" -batchmode -quit -logFile Logs/burst-check.log
grep -i "BC1091\|burst error" Logs/burst-check.log
# Should return empty (no matches)
```

---

## Review Notes

*(This section is for PureDOTS team use)*

**Reviewer**: {TBD}  
**Review Date**: {TBD}  
**Decision**: {PENDING}  
**Notes**: {TBD}

