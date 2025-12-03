# Agent 2: Fix Burst BC1016 Errors - Time/Knowledge

## Your Mission
Fix BC1016 Burst compilation errors in PureDOTS by extracting string literals from Burst-compiled code.

## Project Location
`C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS`

## Required Reading First
Read `TRI_PROJECT_BRIEFING.md` in the project root, specifically sections **P8** and **P13**.

---

## Files to Fix

### File 1: TimelineBranchSystem.cs
**Path**: `Packages/com.moni.puredots/Runtime/Systems/Time/Branching/TimelineBranchSystem.cs`
**Line**: ~108
**Method**: `WhatIfSimulationSystem.OnUpdate`

**Error**:
```
BC1016: The managed function `System.String.get_Length` is not supported
at Unity.Collections.FixedString64Bytes..ctor
at PureDOTS.Runtime.Systems.Time.Branching.WhatIfSimulationSystem.OnUpdate
```

**Problem**: Creates `FixedString64Bytes` from string literal inside Burst-compiled OnUpdate.

---

### File 2: LessonAcquisitionSystem.cs
**Path**: `Packages/com.moni.puredots/Runtime/Systems/Knowledge/LessonAcquisitionSystem.cs`
**Line**: ~358
**Method**: `CheckAttributeRequirement`

**Error**:
```
BC1016: The managed function `System.String.get_Length` is not supported
at Unity.Collections.FixedString64Bytes..ctor
at PureDOTS.Systems.Knowledge.LessonAcquisitionSystem.ProcessAcquisitionRequestsJob.CheckAttributeRequirement
```

**Problem**: Creates `FixedString64Bytes` from string literal inside Burst-compiled job.

---

## Fix Pattern

```csharp
// ❌ WRONG - Inside Burst method:
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    var branchName = new FixedString64Bytes("WhatIf");  // BC1016!
    // ...
}

// ✅ CORRECT - Define constants at class level (OUTSIDE methods):
private static readonly FixedString64Bytes BranchNameWhatIf = "WhatIf";

[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    var branchName = BranchNameWhatIf;  // ✅ Just copies constant
    // ...
}
```

---

## Step-by-Step Instructions

1. Open `TimelineBranchSystem.cs`
2. Find the `WhatIfSimulationSystem` struct/class and its `OnUpdate` method (~line 108)
3. Identify all string literals being passed to FixedString constructors
4. Create `private static readonly` constants at class/struct level for each string
5. Replace the constructors with references to the constants
6. Repeat for `LessonAcquisitionSystem.cs` → `CheckAttributeRequirement` method

---

## Special Case: Attribute ID Strings

In `CheckAttributeRequirement`, the string might be an attribute identifier. Look for patterns like:
```csharp
// If there's a comparison like this:
if (attributeId == new FixedString64Bytes("Strength")) { ... }

// Create constants:
private static readonly FixedString64Bytes AttrStrength = "Strength";
private static readonly FixedString64Bytes AttrAgility = "Agility";
// etc.

// Then use:
if (attributeId == AttrStrength) { ... }
```

---

## Verification

After fixing, verify by:
1. Opening Unity Editor for PureDOTS project
2. Triggering domain reload (edit any script or Ctrl+R)
3. Check Console - should have NO BC1016 errors mentioning:
   - `WhatIfSimulationSystem`
   - `TimelineBranchSystem`
   - `LessonAcquisitionSystem`

---

## Constraints

- Use C# 9 syntax (no `ref readonly`, no collection expressions)
- Unity Entities 1.4.2 (not 1.5+)
- All constants must be `static readonly`, not `const`
- Keep constants near the top of the struct/class, before methods

---

## Report When Complete

```
✅ TimelineBranchSystem.WhatIfSimulationSystem.OnUpdate - FIXED
   - Added X static constants
   - Replaced X string constructors
   
✅ LessonAcquisitionSystem.CheckAttributeRequirement - FIXED
   - Added X static constants
   - Replaced X string constructors

Build status: Domain reload succeeded with no BC1016 errors for these files
```

---

## Status (Agent 2) – Updated 2025-11-27

- ✅ TimelineBranchSystem.WhatIfSimulationSystem.OnUpdate – fixed: constants added for branch names, no FixedString constructors remain in Burst paths.
- ✅ LessonAcquisitionSystem.CheckAttributeRequirement – fixed: converted static readonly fields to instance fields on the job struct, initialized at job creation time (avoids BC1091 static constructor errors).

