# Extension Request: Fix Burst BC1016 FixedString Errors

**Status**: `[RESOLVED]`  
**Submitted**: 2025-11-27  
**Game Project**: Space4X  
**Priority**: P0  
**Assigned To**: TBD

---

## Use Case

Space4X compilation is completely blocked by Burst BC1016 errors originating in PureDOTS package code. Multiple systems use managed `string` operations inside Burst-compiled jobs:

1. `new FixedString*(string)` constructors in Burst jobs
2. `.ToString()` calls on FixedString types in Burst jobs

These errors prevent any build or play-mode testing.

---

## Proposed Solution

**Extension Type**: Bug Fix

**Details:**

Replace runtime string-to-FixedString conversions with pre-defined static constants or [BurstDiscard] patterns.

### Pattern 1: `new FixedString*(string)` in Burst Code

```csharp
// ❌ Current (causes BC1016)
var name = new FixedString64Bytes("Shield");

// ✅ Fix: Pre-defined static constant
private static readonly FixedString64Bytes ShieldName = new FixedString64Bytes("Shield");

// In Burst job:
var name = ShieldName;  // OK - no string.Length call
```

### Pattern 2: `.ToString()` in Burst Code

```csharp
// ❌ Current (causes BC1016)
var str = moduleName.ToString();

// ✅ Fix: Use [BurstDiscard] for logging
[BurstDiscard]
private static void LogModuleName(in FixedString32Bytes name)
{
    UnityEngine.Debug.Log(name.ToString());
}
```

---

## Impact Assessment

**Files/Systems Affected:**

| File | Line | Method | Issue |
|------|------|--------|-------|
| `Runtime/Systems/Spells/SpellEffectExecutionSystem.cs` | 311 | `ApplyShieldEffect` | `new FixedString*()` |
| `Runtime/Systems/Aggregates/BandFormationSystem.cs` | 255 | `GoalToDescription` | `new FixedString*()` |
| `Runtime/Runtime/Registry/Aggregates/AggregateHelpers.cs` | 227 | `GeneratePseudoHistory` | `new FixedString*()` |
| `Runtime/Systems/Knowledge/LessonAcquisitionSystem.cs` | 358 | `CheckAttributeRequirement` | `new FixedString*()` |
| `Runtime/Systems/Combat/HazardEmitFromDamageSystem.cs` | 128 | `HazardEmitFromDamageJob.Execute` | `.ToString()` |
| `Runtime/Systems/Ships/LifeBoatEjectorSystem.cs` | 85 | `LifeBoatEjectorJob.Execute` | `.ToString()` |
| `Runtime/Systems/Spells/SchoolFoundingSystem.cs` | 148 | `ProcessFoundingRequestsJob.Execute` | `new FixedString*()` |

**Breaking Changes:**
- No - fixes are internal implementation changes
- API/interface remains identical

---

## Example Usage

After fix, no game code changes needed. Current Space4X code will compile:

```csharp
// Space4X already uses PureDOTS systems correctly
// The fix is internal to PureDOTS
public void OnUpdate(ref SystemState state)
{
    // Uses SpellEffectExecutionSystem internally - will work after fix
}
```

---

## Alternative Approaches Considered

- **Alternative 1**: Space4X wraps PureDOTS systems to avoid Burst
  - **Rejected**: Would defeat purpose of Burst compilation, massive performance loss

- **Alternative 2**: Disable Burst for affected systems
  - **Rejected**: Core systems need Burst performance

---

## Implementation Notes

**Priority Order** (by impact frequency):
1. SpellEffectExecutionSystem - most commonly triggered
2. AggregateHelpers - core registry functionality  
3. BandFormationSystem - formation AI
4. LessonAcquisitionSystem - knowledge system
5. HazardEmitFromDamageSystem - combat
6. LifeBoatEjectorSystem - ship systems
7. SchoolFoundingSystem - spell schools

**Testing Requirements:**
```bash
# After fixes, this should complete without BC1016 errors:
Unity -batchmode -projectPath ../Space4x -quit -buildWindows64Player Build/Space4x.exe
```

**Source Error Log:** `Space4X/Docs/TODO/consoleerrors.md`

---

## Review Notes

**Reviewer**: Automated  
**Review Date**: 2025-11-27  
**Decision**: Resolved  
**Notes**: 

**Files fixed in this session:**

1. **AggregateHelpers.cs** - Added static `FixedString32Bytes` constants (`EventTypeHarvest`, `EventTypeBirth`, `EventTypeDeath`) and replaced inline constructors.

2. **HazardEmitFromDamageSystem.cs** - Added static `FixedString32Bytes` constants (`ReactorIdPattern`, `EngineIdPattern`) and replaced `.ToString().Contains()` with `.IndexOf()`.

3. **LifeBoatEjectorSystem.cs** - Added static `FixedString32Bytes` constant (`BridgeIdPattern`) and replaced `.ToString().Contains()` with `.IndexOf()`.

4. **SchoolFoundingSystem.cs** - Added static `FixedString32Bytes` constant (`SchoolIdPrefix`) and replaced string interpolation with `Append()` calls.

**Files already fixed (verified no BC1016 patterns):**
- SpellEffectExecutionSystem.cs
- BandFormationSystem.cs
- LessonAcquisitionSystem.cs

All BC1016 errors from the original list have been addressed. 

