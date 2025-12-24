# Camera Pipeline Status

**Last Updated**: 2025-12-XX  
**Purpose**: Document active vs legacy/unused camera systems across PureDOTS, Space4X, and Godgame.

**Cleanup Status**: Phase 4 cleanup complete - all orphaned .meta files and placeholder files removed.

## Active Camera Pipeline

### Standard Architecture: Mono Controllers → CameraRigService → CameraRigApplier

**Core Components:**
- `CameraRigService` (`PureDOTS.Runtime.Camera`) - Single source of truth for camera state
- `CameraRigApplier` (`PureDOTS.Runtime.Camera`) - Mono bridge that applies state to Unity Camera
- `CameraRigState` (`PureDOTS.Runtime.Camera`) - Canonical camera state struct

**Active Controllers:**

1. **Godgame**: `GodgameCameraController` (`godgame/Assets/Scripts/Godgame/Camera/`)
   - Reads input from ECS `CameraInput` component
   - Publishes to `CameraRigService`
   - Supports RTS/Free-fly modes

2. **Space4X**: `Space4XCameraRigController` (`space4x/Assets/Scripts/Space4x/Camera/`)
   - Reads input from Unity Input System actions
   - Publishes to `CameraRigService`
   - Supports orbit, pan, zoom controls

3. **Reusable**: `BW2StyleCameraController` (`PureDOTS.Runtime.Input`)
   - Reads input from Unity Input System
   - Uses `HandCameraInputRouter` for pointer/raycast context
   - Publishes to `CameraRigService`

## Removed/Legacy Systems

### PureDOTS Legacy Camera Systems (Removed)

**Files Deleted:**
- `puredots/Packages/com.moni.puredots/Runtime/Systems/Camera/CameraSystem.cs`
- `puredots/Packages/com.moni.puredots/Runtime/Systems/Camera/CameraRigTelemetrySystem.cs`
- `puredots/Packages/com.moni.puredots/Runtime/Systems/Input/IntentMappingSystem.cs`

**Reason**: Wrapped in `#if PUREDOTS_LEGACY_CAMERA` which was never defined. Used legacy components (`CameraState`, `CameraConfig`, `GodIntent`) that don't exist in current pipeline.

**Dependent Files Updated:**
- `DiagnosticsOverlayBehaviour.cs` - Removed `#if` wrapper (uses current `CameraRigService`)
- `DebugDisplaySystem.cs` - Removed `#if` wrapper (doesn't reference legacy components)
- `ManualPhaseControl.cs` - Removed `#if` wrapper (uses current `CameraRigService`)
- `ManualPhaseSystems.cs` - Removed `#if` wrapper (uses current systems)
- `RuntimeDebugConsole.cs` - Removed `#if` blocks (DiagnosticsOverlayBehaviour now always available)

### Godgame ECS Camera Pipeline (Removed)

**Files Deleted:**
- `godgame/Assets/Scripts/Godgame/Camera/GodgameCameraRigController.cs`
- `godgame/Assets/Scripts/Godgame/Camera/CameraRigSystem.cs`
- `godgame/Assets/Scripts/Godgame/Camera/CameraRigComponents.cs`

**Reason**: Alternative ECS pipeline (`CameraRigState`/`CameraRigCommand` ECS components) that was unused. `GodgameCameraController` (Mono-based, uses `CameraRigService`) is the active implementation.

**Updated Files:**
- `GodgameSurfaceFieldsStreamingFocusBridgeSystem.cs` - Updated to read from `CameraRigService` instead of ECS `CameraRigState`

### Camera Physics Stubs (Removed)

**File Deleted:**
- `puredots/Packages/com.moni.puredots/Runtime/Physics/CameraPhysicsStubs.cs`

**Reason**: Documented stub with no implementation. Camera code uses `UnityEngine.Physics` directly.

## Space4X DOTS Camera Systems (Removed)

**Files Deleted (2025-12-XX):**
- `puredots/Assets/Scripts/Space4x/Systems/Space4XCameraInitializationSystem.cs` - Empty placeholder
- `puredots/Assets/Scripts/Space4x/Systems/Space4XCameraInputSystem.cs` - Empty placeholder
- `puredots/Assets/Scripts/Space4x/Registry/Space4XCameraSystem.cs` - Empty placeholder
- `puredots/Assets/Scripts/Space4x/Systems/Space4XCameraRenderSyncSystem.cs` - Empty placeholder

**Status**: Removed empty placeholder files. Space4X uses `Space4XCameraRigController` (Mono-based) exclusively.

**Rationale**: Empty files provided no value and created confusion. If DOTS-only camera is needed in the future, implement from scratch with current architecture knowledge.

## Pipeline Decision

**Current Standard**: Mono controllers → `CameraRigService` → `CameraRigApplier` → Unity Camera

**Rationale:**
- Simple, maintainable architecture
- Works with both ECS input (Godgame) and Unity Input System (Space4X)
- Single source of truth (`CameraRigService`) prevents conflicts
- Easy to debug and extend

**Future Considerations:**
- If DOTS-only camera is needed, implement Space4X DOTS systems or migrate controllers to ECS
- Keep `CameraRigService` as the interface even if internal implementation changes

## Related Documentation

- `PureDOTS/Runtime/Camera/README.md` - Camera rig service documentation
- `PUREDOTS_CAMERA_REFACTOR.md` - Historical refactor notes (may be outdated)
- `Docs/StubTypes.md` - Stub tracking (CameraPhysicsStubs marked as removed)

