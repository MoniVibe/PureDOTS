# Extension Request: Fix CS0121 Ambiguous math.max Overload

**Status**: `[RESOLVED]`  
**Submitted**: 2025-11-27  
**Game Project**: Godgame  
**Priority**: P1  
**Assigned To**: TBD

---

## Use Case

Godgame compilation fails with CS0121 ambiguous overload errors in PureDOTS rendering code:

```
error CS0121: The call is ambiguous between the following methods or properties: 
  'math.max(int, int)' and 'math.max(uint2, uint2)'
```

This occurs when `math.max()` is called with arguments that can implicitly convert to multiple overload signatures (e.g., mixing `int` and `uint`, or using literals).

---

## Proposed Solution

**Extension Type**: Bug Fix

**Details:**

The issue is in `AnchoredSimulationHelpers.cs` where `math.max()` is called with arguments that match multiple overloads. The fix is to explicitly cast arguments to the intended type.

**Files Affected:**
- `Runtime/Runtime/Rendering/AnchoredSimulationHelpers.cs` (lines 40, 65, 102)

**Fix Pattern:**

```csharp
// ❌ Ambiguous - compiler can't choose overload
var result = math.max(someValue, 0);

// ✅ Explicit int
var result = math.max(someValue, (int)0);
// OR
var result = math.max((int)someValue, 0);

// ✅ Explicit uint
var result = math.max(someValue, 0u);

// ✅ Explicit float
var result = math.max(someValue, 0f);
```

---

## Impact Assessment

**Files/Systems Affected:**
- `Runtime/Runtime/Rendering/AnchoredSimulationHelpers.cs` - 3 ambiguous calls at lines 40, 65, 102

**Breaking Changes:**
- No - fixes are internal implementation changes
- Only affects compilation, not runtime behavior

---

## Example Usage

**Current (Broken):**
```csharp
// Line 40 - ambiguous
var maxValue = math.max(someInt, 0);  // Could be int or uint2

// Line 65 - ambiguous  
var clamped = math.max(value, 0);  // Could be int or uint2

// Line 102 - ambiguous
var result = math.max(threshold, 0);  // Could be int or uint2
```

**Fixed:**
```csharp
// Line 40 - explicit int
var maxValue = math.max(someInt, (int)0);

// Line 65 - explicit int
var clamped = math.max((int)value, 0);

// Line 102 - explicit int
var result = math.max((int)threshold, 0);
```

---

## Alternative Approaches Considered

- **Alternative 1**: Use different math function
  - **Rejected**: `math.max()` is the correct function, just needs explicit types

- **Alternative 2**: Change variable types
  - **Rejected**: May affect other code; explicit casting is safer

---

## Implementation Notes

**Required Changes:**

1. **Line 40**: Add explicit cast to `(int)` for the second argument
2. **Line 65**: Add explicit cast to `(int)` for the first or second argument
3. **Line 102**: Add explicit cast to `(int)` for the first or second argument

**Testing Requirements:**
- Verify compilation succeeds without CS0121 errors
- Verify runtime behavior unchanged (explicit casts don't change logic)

**Lesson Learned:**
When using Unity.Mathematics `math.*` functions, always ensure argument types match exactly to avoid ambiguous overload resolution - especially with mixed signed/unsigned integers or literal values.

---

## Review Notes

*(This section is for PureDOTS team use)*

**Reviewer**: {TBD}  
**Review Date**: {TBD}  
**Decision**: {PENDING}  
**Notes**: {TBD}

