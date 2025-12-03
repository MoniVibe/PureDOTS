# Agent 3: FIXED – prompt closed

Completed in this session (PureDOTS fixes: time/rewind imports, hazard systems, spell resource IDs). No further action required for this prompt.

# Agent 3: Fix Stale Type References - Presentation Editor

## Your Mission
Fix CS0234/CS0246 errors in Godgame caused by references to types that no longer exist or have moved.

## Project Location
`C:\Users\Moni\Documents\claudeprojects\unity\Godgame`

## Required Reading First
Read `TRI_PROJECT_BRIEFING.md` in the project root, specifically section **P10**.

---

## File to Fix

### SwappablePresentationBindingEditor.cs
**Path**: `Assets/Editor/Presentation/SwappablePresentationBindingEditor.cs`

**Errors**:
```
Line 3: CS0234: The type or namespace name 'Presentation' does not exist in the namespace 'Godgame'

Line 13: CS0246: The type or namespace name 'SwappablePresentationBindingAuthoring' could not be found

Line 22: CS0246: The type or namespace name 'PresentationRegistry' could not be found
```

---

## Investigation Steps

Run these commands to find if the types exist elsewhere:

```bash
# Check if the Presentation namespace exists anywhere
grep -r "namespace.*Presentation" --include="*.cs"

# Check if SwappablePresentationBindingAuthoring exists
grep -r "class SwappablePresentationBindingAuthoring" --include="*.cs"
grep -r "SwappablePresentationBindingAuthoring" --include="*.cs"

# Check if PresentationRegistry exists
grep -r "class PresentationRegistry" --include="*.cs"
grep -r "PresentationRegistry" --include="*.cs"
```

---

## Decision Tree

### If types exist in a different namespace:
1. Update the `using` statements to point to the correct namespace
2. Verify the assembly references in the `.asmdef` file

### If types exist in PureDOTS package:
1. Add the correct `using` statement (e.g., `using PureDOTS.Presentation;`)
2. Ensure the Editor `.asmdef` references the PureDOTS assembly

### If types don't exist anywhere (deleted):
1. **Recommended**: Delete the entire editor file since it's referencing dead code
2. Check if there are any other files referencing this editor and clean those up too

### If types should exist but are missing:
1. Flag as blocker - someone needs to create the types first
2. Document what the types should be based on context in the editor file

---

## Fix Pattern for Namespace Update

```csharp
// ❌ WRONG - Old namespace
using Godgame.Presentation;

// ✅ CORRECT - If moved to PureDOTS
using PureDOTS.Presentation;

// ✅ CORRECT - If moved elsewhere in Godgame
using Godgame.Runtime.Presentation;
```

---

## Fix Pattern for Deleted Types

If the types were deleted and the editor is no longer needed:

```bash
# Delete the file
rm "Assets/Editor/Presentation/SwappablePresentationBindingEditor.cs"

# Also delete its .meta file
rm "Assets/Editor/Presentation/SwappablePresentationBindingEditor.cs.meta"

# If the Presentation folder is now empty, delete it too
rmdir "Assets/Editor/Presentation"
rm "Assets/Editor/Presentation.meta"
```

---

## Verification

After fixing, verify by:
1. Opening Unity Editor for Godgame project
2. Triggering domain reload
3. Check Console - should have NO CS0234/CS0246 errors mentioning:
   - `SwappablePresentationBindingEditor`
   - `Godgame.Presentation`
   - `SwappablePresentationBindingAuthoring`
   - `PresentationRegistry`

---

## Constraints

- Do not create stub types just to make compilation pass - either find real types or delete dead code
- If deleting, ensure no other files depend on this editor
- Document your decision in the report

---

## Report When Complete

```
Investigation Results:
- Godgame.Presentation namespace: [EXISTS at X / DOES NOT EXIST]
- SwappablePresentationBindingAuthoring: [EXISTS at X / DOES NOT EXIST]
- PresentationRegistry: [EXISTS at X / DOES NOT EXIST]

Action Taken: [UPDATED NAMESPACE / DELETED FILE / FLAGGED AS BLOCKER]

Details:
- [What you did and why]

Build status: [Domain reload succeeded / Still has errors because X]
```

