# Error Fix Batch - 2025-11-27

**Total Errors**: ~25+ errors across PureDOTS and Godgame
**Agents Required**: 7 (can run in parallel)

---

## Pre-Read Requirements

Before starting, ALL agents must read:
- `TRI_PROJECT_BRIEFING.md` (in your project root)
- Focus on sections P8, P10, P11, P12, P13

---

## Agent 1: PureDOTS Burst BC1016 Fix - Aggregates/Spells

**Project**: PureDOTS (`C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS`)
**Error**: BC1016 - Managed function in Burst (FixedString constructor from string)

### Files to Fix
1. `Packages/com.moni.puredots/Runtime/Systems/Aggregates/BandFormationSystem.cs` (line ~255)
   - Method: `GoalToDescription`
   - Issue: Creates `FixedString128Bytes` from string inside Burst

2. `Packages/com.moni.puredots/Runtime/Systems/Spells/SpellEffectExecutionSystem.cs` (line ~311)
   - Method: `ApplyShieldEffect`
   - Issue: Creates `FixedString64Bytes` from string inside Burst

### Fix Pattern
```csharp
// BEFORE (inside Burst method):
var desc = new FixedString128Bytes("Some text");

// AFTER:
// 1. Add static readonly constants at class level (OUTSIDE any method):
private static readonly FixedString128Bytes DescSomeText = "Some text";

// 2. Reference in Burst method:
var desc = DescSomeText;

// For enum-to-string, use switch with pre-defined constants
```

### Verification
- Unity domain reload succeeds with no BC1016 errors mentioning these files
- Test: Run any scenario that exercises band formation or spell casting

---

## Agent 2: PureDOTS Burst BC1016 Fix - Time/Knowledge

**Project**: PureDOTS (`C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS`)
**Error**: BC1016 - Managed function in Burst

### Files to Fix
1. `Packages/com.moni.puredots/Runtime/Systems/Time/Branching/TimelineBranchSystem.cs` (line ~108)
   - Method: `WhatIfSimulationSystem.OnUpdate`
   - Issue: Creates `FixedString64Bytes` from string

2. `Packages/com.moni.puredots/Runtime/Systems/Knowledge/LessonAcquisitionSystem.cs` (line ~358)
   - Method: `CheckAttributeRequirement`
   - Issue: Creates `FixedString64Bytes` from string

### Fix Pattern
Same as Agent 1 - extract string literals to `static readonly` constants at class level.

### Verification
- Unity domain reload succeeds with no BC1016 errors mentioning these files

---

## Agent 3: Godgame Editor Stale References

**Project**: Godgame (`C:\Users\Moni\Documents\claudeprojects\unity\Godgame`)
**Errors**: CS0234, CS0246 - Missing namespace/type

### Files to Fix
1. `Assets/Editor/Presentation/SwappablePresentationBindingEditor.cs`
   - Line 3: `Godgame.Presentation` namespace doesn't exist
   - Line 13: `SwappablePresentationBindingAuthoring` type not found
   - Line 22: `PresentationRegistry` type not found

### Investigation Steps
1. `grep -r "namespace.*Presentation" --include="*.cs"` - Find if namespace moved
2. `grep -r "SwappablePresentationBindingAuthoring" --include="*.cs"` - Find if type exists elsewhere
3. `grep -r "PresentationRegistry" --include="*.cs"` - Same

### Fix Options
- **Option A**: If types exist elsewhere, update `using` statements
- **Option B**: If types were deleted, delete the entire editor file (it references dead code)
- **Option C**: If types should exist, stub them out

### Verification
- CS0234/CS0246 errors for this file are gone

---

## Agent 4: Godgame Scene Tools - Obsolete APIs + Missing Types

**Project**: Godgame (`C:\Users\Moni\Documents\claudeprojects\unity\Godgame`)
**Errors**: CS0618 (obsolete), CS0246 (type not found)

### Files to Fix
1. `Assets/Editor/Tools/GodgameDevSceneSetup.cs`
   - Line 170: `FindObjectsOfType<T>()` → `FindObjectsByType<T>(FindObjectsSortMode.None)`
   - Line 230: `FindObjectOfType<DevTools>()` → Check if DevTools exists, use modern API
   - Line 251: `DevTools` type not found
   - Line 258: `FindObjectOfType<Demo>()` → Check if Demo exists
   - Line 266: `Demo` type not found

2. `Assets/Editor/Tools/GodgameDemoSceneWizard.cs`
   - Line 284: `FindObjectsOfType<T>(bool)` → `FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)`

### Investigation for Missing Types
```bash
grep -r "class DevTools" --include="*.cs"
grep -r "class Demo" --include="*.cs"
```

### Fix Pattern
```csharp
// BEFORE:
var obj = FindObjectOfType<MyComponent>();
var objs = FindObjectsOfType<MyComponent>();
var objs = FindObjectsOfType<MyComponent>(true);

// AFTER:
var obj = FindFirstObjectByType<MyComponent>();
var objs = FindObjectsByType<MyComponent>(FindObjectsSortMode.None);
var objs = FindObjectsByType<MyComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
```

### Verification
- CS0618 warnings gone
- CS0246 errors resolved (either types found or references removed)

---

## Agent 5: PureDOTS CreateAssetMenu Fix - Part 1

**Project**: PureDOTS (`C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS`)
**Error**: CreateAssetMenu attribute on non-ScriptableObject (console warning)

### Files to Fix (find via grep)
1. `CultureStoryCatalogAuthoring`
2. `LessonCatalogAuthoring`
3. `SpellCatalogAuthoring`
4. `ItemPartCatalogAuthoring`
5. `EnlightenmentProfileAuthoring`

### Investigation
```bash
grep -r "CreateAssetMenu" --include="*.cs" -A2 | grep -E "(Authoring|MonoBehaviour)"
```

### Fix Pattern
```csharp
// If class extends MonoBehaviour, REMOVE the CreateAssetMenu attribute:
// BEFORE:
[CreateAssetMenu(menuName = "...")]
public class MyAuthoring : MonoBehaviour { }

// AFTER:
public class MyAuthoring : MonoBehaviour { }
```

### Verification
- Console warning about CreateAssetMenu on these classes is gone

---

## Agent 6: PureDOTS CreateAssetMenu Fix - Part 2

**Project**: PureDOTS (`C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS`)
**Error**: CreateAssetMenu attribute on non-ScriptableObject

### Files to Fix
1. `BuffCatalogAuthoring`
2. `SchoolComplexityCatalogAuthoring`
3. `QualityFormulaAuthoring`
4. `SpellSignatureCatalogAuthoring`
5. `QualityCurveAuthoring`

### Fix Pattern
Same as Agent 5 - remove `[CreateAssetMenu]` from MonoBehaviour classes.

### Verification
- Console warning about CreateAssetMenu on these classes is gone

---

## Agent 7: Integration Verification

**Project**: All projects
**Role**: Verify fixes from Agents 1-6

### Steps
1. Wait for Agents 1-6 to complete
2. Open Unity Editor for each project:
   - PureDOTS
   - Godgame
3. Force domain reload (Ctrl+R or edit any script)
4. Check Console for:
   - No BC1016 errors
   - No CS0234/CS0246 errors in fixed files
   - No CS0618 warnings in fixed files
   - No CreateAssetMenu warnings for fixed classes

### Report Format
```
✅ BC1016 BandFormationSystem - FIXED
✅ BC1016 SpellEffectExecutionSystem - FIXED
❌ CS0246 DevTools - STILL BROKEN (reason)
```

---

## Coordination Rules

1. **No file conflicts**: Each agent works on different files
2. **Don't modify shared types**: If you need to add a type, flag it as a blocker
3. **Verify locally before marking complete**: Domain reload must succeed
4. **Document any decisions**: If a file should be deleted vs fixed, note why

---

## Summary Table

| Agent | Project | Error Type | Files |
|-------|---------|------------|-------|
| 1 | PureDOTS | BC1016 | BandFormationSystem, SpellEffectExecutionSystem |
| 2 | PureDOTS | BC1016 | TimelineBranchSystem, LessonAcquisitionSystem |
| 3 | Godgame | CS0234/CS0246 | SwappablePresentationBindingEditor |
| 4 | Godgame | CS0618/CS0246 | GodgameDevSceneSetup, GodgameDemoSceneWizard |
| 5 | PureDOTS | CreateAssetMenu | 5 Authoring files (Culture, Lesson, Spell, Item, Enlightenment) |
| 6 | PureDOTS | CreateAssetMenu | 5 Authoring files (Buff, School, Quality, Signature, Curve) |
| 7 | All | Verification | Integration testing |

