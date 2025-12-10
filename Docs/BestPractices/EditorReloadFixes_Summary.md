# Editor Reload Freeze Fixes - Summary

## Changes Made

### 1. RuntimeConfigRegistry.cs
**Problem:** `ScanAssemblies()` iterates all assemblies and calls `GetTypes()` which can hang during domain reload.

**Fixes:**
- Added `EditorApplication.isCompiling` / `EditorApplication.isUpdating` guards in `Initialize()` and `ScanAssemblies()`
- Added exception handling to skip problematic assemblies during reload
- Added debug logging to track when initialization runs

**Files Modified:**
- `PureDOTS/Packages/com.moni.puredots/Runtime/Runtime/Config/RuntimeConfigRegistry.cs`

### 2. CameraRigService.cs (Both Files)
**Problem:** Static constructor called `RuntimeConfigRegistry.Initialize()` during domain reload.

**Fixes:**
- Removed static constructor initialization
- Made `IsEcsCameraEnabled` property lazy (initializes on first access)
- Added comment explaining lazy initialization pattern

**Files Modified:**
- `PureDOTS/Packages/com.moni.puredots/Runtime/Camera/CameraRigService.cs` (canonical location)

### 3. PureDotsWorldBootstrap.cs
**Problem:** `ICustomBootstrap.Initialize()` could run during domain reload, creating worlds when Unity is compiling.

**Fixes:**
- Added guard to skip world creation when `!Application.isPlaying && EditorApplication.isCompiling`
- Returns `false` to let Unity use default bootstrap during reload

**Files Modified:**
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/PureDotsWorldBootstrap.cs`

### 4. Editor-World Systems
**Problem:** Systems with `WorldSystemFilterFlags.Editor` run in editor world even during domain reload.

**Fixes:**
- Added `state.WorldUnmanaged.IsCreated` checks in all `OnUpdate` methods
- Added `Application.isPlaying` guards to skip heavy work in edit mode

**Files Modified:**
- `PureDOTS/Packages/com.moni.puredots/Runtime/Demo/Village/VillagerWalkLoopSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Demo/Village/VillageDebugSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Demo/Village/VillageDemoBootstrapSystem.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Demo/Village/VillageVisualSetupSystem.cs`

## Verification Checklist

### ✅ Static Constructors
- [x] No `RuntimeConfigRegistry.Initialize()` calls in static constructors
- [x] `SystemRegistry` static constructor is lightweight (dictionary operations only)
- [x] `FacilityRecipes` static constructor is lightweight (array initialization only)

### ✅ InitializeOnLoad Methods
- [x] No `[InitializeOnLoad]` / `[InitializeOnLoadMethod]` found in codebase
- [x] No `[RuntimeInitializeOnLoadMethod]` found in codebase

### ✅ ExecuteAlways/ExecuteInEditMode
- [x] No `[ExecuteAlways]` / `[ExecuteInEditMode]` MonoBehaviours found

### ✅ EditorApplication.update
- [x] No `EditorApplication.update +=` subscriptions found

### ✅ Assembly Scanning
- [x] `RuntimeConfigRegistry.ScanAssemblies()` has compilation guards
- [x] Exception handling added for problematic assemblies

### ✅ Editor-World Systems
- [x] All systems with `WorldSystemFilterFlags.Editor` have `IsCreated` guards
- [x] All editor-world systems have `Application.isPlaying` guards

### ✅ ICustomBootstrap
- [x] `PureDotsWorldBootstrap.Initialize()` has compilation guard

## Remaining RuntimeConfigRegistry.Initialize() Calls

All remaining calls are safe:
- **System OnCreate methods** - Run when systems are created, not during static construction
- **Property getters** - Lazy initialization (CameraRigService)
- **Test code** - Explicit test setup

## Testing Instructions

1. **Trigger Recompile:**
   - Make a small code change (add/remove whitespace)
   - Save file to trigger Unity recompile

2. **Monitor Editor:**
   - Watch for "Hold on… Application.UpdateScene" dialog
   - Should NOT appear after fixes

3. **Check Editor.log:**
   - If freeze still occurs, check `%LOCALAPPDATA%\Unity\Editor\Editor.log`
   - Look for last `[RuntimeConfigRegistry]` log before hang
   - This will identify any remaining problematic code paths

4. **Verify Logging:**
   - After successful reload, check console for:
     - `[RuntimeConfigRegistry] Initialize - scanning assemblies`
     - `[RuntimeConfigRegistry] Initialize complete - registered X config vars`
   - These should only appear AFTER reload completes, not during

## Pattern Prevention

See `EditorReloadSafety.md` for:
- Anti-patterns to avoid
- Best practices for editor-time code
- Examples of safe vs unsafe patterns

