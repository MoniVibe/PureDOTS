# Extension Request: Fix Burst BC1091 Static Constructor Errors

**Status**: `[RESOLVED]`  
**Submitted**: 2025-11-27  
**Game Project**: Space4X  
**Priority**: P0  
**Assigned To**: TBD

---

## Use Case

The previous BC1016 fix attempt used `static readonly FixedString*` fields, which triggers **BC1091 errors**:

```
Burst error BC1091: External and internal calls are not allowed inside static constructors
```

The `static readonly` field initialization runs in a static constructor, which Burst tries to compile and fails.

Space4X compilation is **still completely blocked**.

---

## Proposed Solution

**Extension Type**: Bug Fix (Revision of previous fix)

**Details:**

The previous pattern `private static readonly FixedString32Bytes Name = new FixedString32Bytes("value")` **does NOT work** because Burst compiles the static constructor.

### Correct Fix: Initialize in OnCreate (Non-Burst context)

```csharp
// ❌ WRONG - static constructor triggers BC1091
public partial class MySystem : SystemBase
{
    private static readonly FixedString32Bytes ReactorIdPattern = new FixedString32Bytes("Reactor");
    // ERROR: BC1091 - static constructor called in Burst context
}

// ✅ CORRECT - initialize as instance field in OnCreate
public partial class MySystem : SystemBase
{
    private FixedString32Bytes _reactorIdPattern;
    
    protected override void OnCreate()
    {
        base.OnCreate();
        _reactorIdPattern = new FixedString32Bytes("Reactor");  // Safe - OnCreate is not Burst-compiled
    }
}
```

### Alternative: Use Byte-Level Constants

```csharp
// ✅ ALSO CORRECT - use byte literals (no string constructor)
public partial class MySystem : SystemBase
{
    // Pre-computed UTF8 bytes for "Reactor"
    private static readonly byte[] ReactorBytes = { 0x52, 0x65, 0x61, 0x63, 0x74, 0x6F, 0x72 };
    
    // Or use FixedString with default constructor then set bytes
}
```

---

## Impact Assessment

**Files/Systems Affected (Introduced by Previous Fix):**

| File | Line | Issue |
|------|------|-------|
| `Runtime/Runtime/Registry/Aggregates/AggregateHelpers.cs` | 17 | Static constructor (`.cctor()`) |
| `Runtime/Systems/Combat/HazardEmitFromDamageSystem.cs` | 76 | Static constructor (`.cctor()`) |
| `Runtime/Systems/Ships/LifeBoatEjectorSystem.cs` | 53 | Static constructor (`.cctor()`) |
| `Runtime/Systems/Spells/SchoolFoundingSystem.cs` | 122 | Static constructor (`.cctor()`) |

**Breaking Changes:**
- No - fixes are internal implementation changes
- Previous static readonly fields need to become instance fields or be removed

---

## Example Error Stack

```
Burst error BC1091: External and internal calls are not allowed inside static constructors: 
  System.Runtime.CompilerServices.RuntimeHelpers.get_OffsetToStringData()

 at Unity.Collections.FixedStringMethods.CopyFromTruncated(...)
 at Unity.Collections.FixedString32Bytes..ctor(...)
 at PureDOTS.Runtime.Registry.Aggregates.AggregateHelpers..cctor()  ← STATIC CONSTRUCTOR
```

---

## Alternative Approaches Considered

- **Alternative 1**: Keep static readonly fields
  - **Rejected**: Causes BC1091 errors

- **Alternative 2**: Move string operations out of Burst entirely
  - **Partially Viable**: Use `[BurstDiscard]` for non-critical paths

- **Alternative 3**: Pre-compute byte arrays
  - **Complex**: Requires manual UTF8 encoding

---

## Implementation Notes

**Required Changes:**

1. **AggregateHelpers.cs:17**: Change from static readonly to instance field, initialize in a Setup method
2. **HazardEmitFromDamageSystem.cs:76**: Move initialization to `OnCreate`
3. **LifeBoatEjectorSystem.cs:53**: Move initialization to `OnCreate`
4. **SchoolFoundingSystem.cs:122**: Move initialization to `OnCreate`

**Pattern to Apply:**

```csharp
// In system class:
private FixedString32Bytes _pattern;

protected override void OnCreate()
{
    base.OnCreate();
    _pattern = new FixedString32Bytes("SomeValue");
}

// In Burst job/method - reference the instance field
```

**Testing Requirements:**
```bash
# After fixes, this should complete without BC1016/BC1091 errors:
Unity -batchmode -projectPath ../Space4x -quit -buildWindows64Player Build/Space4x.exe
```

---

## Review Notes

**Reviewer**: Automated  
**Review Date**: 2025-11-27  
**Decision**: Resolved  
**Notes**: 

All BC1091 errors fixed using proper patterns:

**1. AggregateHelpers.cs** (static class)
- Replaced `static readonly` fields with helper methods that build FixedStrings using `Append()` (Burst-compatible)
- Pattern: `GetHarvestEventType()`, `GetBirthEventType()`, `GetDeathEventType()` methods

**2. HazardEmitFromDamageSystem.cs** (ISystem struct)
- Added instance fields `_reactorIdPattern`, `_engineIdPattern`
- Initialize in `OnCreate` (not Burst-compiled)
- Pass to job as `[ReadOnly]` fields

**3. LifeBoatEjectorSystem.cs** (ISystem struct)
- Added instance field `_bridgeIdPattern`
- Initialize in `OnCreate`
- Pass to job as `[ReadOnly]` field

**4. SchoolFoundingSystem.cs** (ISystem struct)
- Added instance field `_schoolIdPrefix`
- Initialize in `OnCreate`
- Pass to job as `[ReadOnly]` field

**Key insight**: `static readonly` fields with `new FixedString*("...")` trigger static constructors which Burst tries to compile. Solution is either:
- For static classes: Use helper methods that build strings with `Append()`
- For ISystem: Use instance fields initialized in `OnCreate` and passed to jobs 

