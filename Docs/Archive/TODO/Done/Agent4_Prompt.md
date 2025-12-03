# Agent 4: Fix Scene Tools - Obsolete APIs + Missing Types

**Status:** Completed – tasks addressed; keep for reference only.

## Your Mission
Fix CS0618 (obsolete API) warnings and CS0246 (missing type) errors in Godgame editor tools.

## Project Location
`C:\Users\Moni\Documents\claudeprojects\unity\Godgame`

## Required Reading First
Read `TRI_PROJECT_BRIEFING.md` in the project root, specifically sections **P10** and **P11**.

---

## Files to Fix

### File 1: GodgameDevSceneSetup.cs
**Path**: `Assets/Editor/Tools/GodgameDevSceneSetup.cs`

**Errors/Warnings**:
```
Line 170: CS0618 (warning): 'Object.FindObjectsOfType<T>()' is obsolete

Line 230: CS0618 (warning): 'Object.FindObjectOfType<T>()' is obsolete
Line 230: CS0246 (error): The type or namespace name 'DevTools' could not be found

Line 251: CS0246 (error): The type or namespace name 'DevTools' could not be found

Line 258: CS0618 (warning): 'Object.FindObjectOfType<T>()' is obsolete
Line 258: CS0246 (error): The type or namespace name 'Demo' could not be found

Line 266: CS0246 (error): The type or namespace name 'Demo' could not be found
```

---

### File 2: GodgameDemoSceneWizard.cs
**Path**: `Assets/Editor/Tools/GodgameDemoSceneWizard.cs`

**Errors/Warnings**:
```
Line 284: CS0618 (warning): 'Object.FindObjectsOfType<T>(bool)' is obsolete
```

---

## Part 1: Fix Obsolete Find Methods (CS0618)

### Replacement Pattern

```csharp
// ❌ OLD (deprecated):
var obj = FindObjectOfType<MyComponent>();
var objs = FindObjectsOfType<MyComponent>();
var objs = FindObjectsOfType<MyComponent>(true);  // include inactive

// ✅ NEW (Unity 2023+):
var obj = FindFirstObjectByType<MyComponent>();
var obj = FindAnyObjectByType<MyComponent>();  // faster, if order doesn't matter
var objs = FindObjectsByType<MyComponent>(FindObjectsSortMode.None);  // unsorted, faster
var objs = FindObjectsByType<MyComponent>(FindObjectsSortMode.InstanceID);  // legacy order
var objs = FindObjectsByType<MyComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None);  // include inactive
```

### Choosing the Right Replacement

| Old API | Use Case | New API |
|---------|----------|---------|
| `FindObjectOfType<T>()` | Need deterministic first match | `FindFirstObjectByType<T>()` |
| `FindObjectOfType<T>()` | Any match is fine | `FindAnyObjectByType<T>()` (faster) |
| `FindObjectsOfType<T>()` | Need legacy sort order | `FindObjectsByType<T>(FindObjectsSortMode.InstanceID)` |
| `FindObjectsOfType<T>()` | Order doesn't matter | `FindObjectsByType<T>(FindObjectsSortMode.None)` (faster) |
| `FindObjectsOfType<T>(true)` | Include inactive | `FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)` |

---

## Part 2: Fix Missing Types (CS0246)

### Investigation Commands

```bash
# Check if DevTools exists
grep -r "class DevTools" --include="*.cs"
grep -r "DevTools" --include="*.cs" | head -20

# Check if Demo exists
grep -r "class Demo" --include="*.cs"
grep -r "Demo" --include="*.cs" | head -20
```

### Decision Tree for Missing Types

**If type exists elsewhere:**
1. Add the correct `using` statement
2. Verify assembly references

**If type was deleted:**
1. Comment out or remove the code that references it
2. Add a `// TODO: DevTools component was removed, this feature needs redesign` comment
3. If the entire method is now useless, consider removing it

**If type should exist but doesn't:**
1. Flag as blocker for another agent to create the type

---

## Step-by-Step Instructions

### For GodgameDevSceneSetup.cs:

1. **Line 170**: Replace `FindObjectsOfType<T>()` with appropriate new API
2. **Lines 230, 251**: 
   - First check if `DevTools` exists anywhere
   - If not, comment out or remove the code block that uses it
   - Replace `FindObjectOfType` with `FindFirstObjectByType` if keeping
3. **Lines 258, 266**:
   - First check if `Demo` exists anywhere
   - If not, comment out or remove the code block that uses it
   - Replace `FindObjectOfType` with `FindFirstObjectByType` if keeping

### For GodgameDemoSceneWizard.cs:

1. **Line 284**: Replace `FindObjectsOfType<T>(true)` with:
   ```csharp
   FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
   ```

---

## Verification

After fixing, verify by:
1. Opening Unity Editor for Godgame project
2. Triggering domain reload
3. Check Console - should have:
   - NO CS0618 warnings for these files
   - NO CS0246 errors for `DevTools` or `Demo`

---

## Constraints

- Don't create stub types just to make compilation pass
- Keep functionality working if the types still exist
- Comment removed code clearly if types were deleted
- Use C# 9 syntax

---

## Report When Complete

```
Investigation Results:
- DevTools type: [EXISTS at X / DOES NOT EXIST]
- Demo type: [EXISTS at X / DOES NOT EXIST]

GodgameDevSceneSetup.cs:
- Line 170: FindObjectsOfType → [REPLACEMENT]
- Lines 230, 251: DevTools → [UPDATED USING / REMOVED CODE]
- Lines 258, 266: Demo → [UPDATED USING / REMOVED CODE]

GodgameDemoSceneWizard.cs:
- Line 284: FindObjectsOfType(bool) → FindObjectsByType with [PARAMS]

Build status: Domain reload succeeded with no CS0618/CS0246 for these files
```

