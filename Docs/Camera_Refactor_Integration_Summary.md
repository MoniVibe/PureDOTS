# Camera Refactor Integration Summary

## Overview
- Player camera input is sampled by high-priority Mono bridges before DOTS executes, keeping latency under the 7.1 ms budget required for 140 Hz responsiveness.
- Authoritative camera pose lives in Mono controllers that implement `PureDOTS.Runtime.Camera.ICameraStateProvider` and push state into DOTS singletons once per frame.
- DOTS camera systems remain for state visibility and tooling, but they no longer integrate input or mutate the Unity camera directly when a Mono controller is active.

---

## Space4X Camera Pipeline

| Layer | Responsibility |
|-------|----------------|
| `Space4XCameraInputBridge` | Hooks `InputSystem.onBefore/AfterUpdate`, clamps sampling rate, and exposes telemetry (`SampleRateHz`, `LastSampleDeltaTime`). |
| `Space4XCameraMouseController` | Applies pan/zoom/rotate logic in `Update`, writes directly to `Camera.main`, mirrors `Space4XCameraState`, and holds the current `CameraRigState`. |
| `Space4XCameraInputSystem` | When the Mono controller is present, zeroes the `Space4XCameraInput` singleton so downstream systems never replay stale vectors. |
| `Space4XCameraSystem` | Detects `Space4XCameraMouseController.TryGetLatestState` and simply mirrors the Mono-authored state into DOTS, skipping Burst logic. |
| `Space4XCameraRenderSyncSystem` | Still guarantees the main camera exists, but skips transform writes whenever the Mono controller owns the pose. |

Key files:
- `Assets/Scripts/Space4x/Camera/Space4XCameraInputBridge.cs`
- `Assets/Scripts/Space4x/Camera/Space4XCameraMouseController.cs`
- `Assets/Scripts/Space4x/Registry/Space4XCameraSystem.cs`
- `Assets/Scripts/Space4x/Systems/Space4XCameraRenderSyncSystem.cs`

---

## Godgame Camera Pipeline

| Layer | Responsibility |
|-------|----------------|
| `GodgameCameraInputBridge` | Mirrors the Space4X bridge pattern; samples `Move`, `Vertical`, `Look`, button state, and exposes sample-rate telemetry. |
| `GodgameCameraController` | Owns the RTS/Orbital camera logic, writes to `Camera.main`, syncs `CameraTransform`/`CameraModeState`, and exposes `TryGetCurrentState`. |
| `InputReaderSystem` | Reduced to a thin mirror: copies the bridge snapshot into the `InputState` singleton for hand/gameplay systems. |
| `CameraControlSystem` | Detects `GodgameCameraController.TryGetCurrentState`; when active it simply calls `SyncFromMonoState` and returns, leaving the Burst path idle. |
| `CameraSyncSystem` | Continues to copy `CameraTransform` into the camera entity’s `LocalTransform` so DOTS scene data reflects the Mono pose. |

Key files:
- `Godgame/Assets/Scripts/Godgame/Camera/GodgameCameraInputBridge.cs`
- `Godgame/Assets/Scripts/Godgame/Camera/GodgameCameraController.cs`
- `Godgame/Assets/Scripts/Godgame/Interaction/Input/InputReaderSystem.cs`
- `Godgame/Assets/Scripts/Godgame/Camera/CameraControlSystem.cs`

---

## Shared Components & Instrumentation
- `Packages/com.moni.puredots/Runtime/Runtime/Camera/CameraRigState.cs` defines a shared struct and interface for Mono controllers.
- Both bridges expose `MaxSampleRateHz`, `SampleRateHz`, and `LastSampleDeltaTime` for diagnostics; defaults are tuned for ≥140 Hz.
- Controllers set `Space4XCameraInputFlags`/DOTS equivalents to `MovementHandled = RotationHandled = true`, ensuring ECS never replays stale snapshots.

---

## Residual DOTS Behaviour
- DOTS systems still create/host singletons so tooling (Entity Inspector, diagnostics) remains functional.
- If the Mono controller is absent (e.g., in pure DOTS tests), systems fall back to the historical Burst paths.
- Render-sync systems only update transforms when Mono control is unavailable, preventing the extra “resolve frame” that previously caused drift.

---

## Verification Checklist
- [ ] Confirm bridge telemetry reports ≥140 Hz sampling in editor diagnostics.
- [ ] Ensure `Space4XCameraInput` stays zero while moving/rotating (proves Mono consumption).
- [ ] Verify DOTS camera singletons track Mono state (Entities > Systems window).
- [ ] In Godgame orbital mode, confirm `CameraTransform.DistanceFromPivot` matches Mono controller distance.

---

## Next Steps
1. Update in-editor diagnostics (`CameraDiagnostic.cs`) to surface new telemetry and Mono ownership indicators.
2. Add playmode smoke tests that exercise bridge/controller handover (Mono present vs. absent).
3. Document runtime hooks (`MaxSampleRateHz`, `TryGetCurrentState`) in per-project README files.


