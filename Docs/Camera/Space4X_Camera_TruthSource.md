# Space4X Camera Truth Source

**Implementation Chosen**: Space4XCameraRigController (MonoBehaviour-based)
**Contract**: Controller publishes `CameraRigState` only; `CameraRigApplier` is the sole transform writer.

## Files Used

### Core Camera Logic
- **Space4XCameraRigController.cs** - Main camera controller MonoBehaviour
  - Handles orbit, pan, zoom using Input System
  - Publishes CameraRigState via CameraRigService
  - Supports LMB drag pan, MMB orbit, WASD pan, Q/E zoom

### Supporting Components
- **Space4XCameraAuthoring.cs** - Configuration authoring component
- **Space4XCameraInputAuthoring.cs** - Input action references
- **Space4XCameraMain.cs** - Helper for ensuring main camera exists
- **Space4XCameraState.cs** - ECS state component for interaction systems

### Bootstrap & Setup
- **Space4XCameraBootstrap.cs** - Runtime bootstrap ensuring camera exists
- **Space4XCameraSetup.cs** - Editor utility to create camera prefab
- Modified **Space4XSceneSetupMenu.cs** - Adds camera bootstrap to new scenes

## Architecture

**Type**: MonoBehaviour with ECS integration
**Input**: Unity Input System (action-based)
**State**: Publishes to CameraRigService for ECS consumption
**Timing**: Update() updates canonical state (focus/yaw/pitch/distance) + consumes requests; LateUpdate publishes; `CameraRigApplier` derives/apply in LateUpdate

## Input Controls

- **LMB Drag**: Pan camera
- **MMB Drag**: Orbit (pitch/yaw)
- **WASD**: Pan movement
- **Q/E**: Zoom in/out
- **F**: Reset to default position

## Bootstrap Integration

Camera bootstrap runs in `Awake()` and:
1. Checks for existing main camera
2. Instantiates camera prefab if assigned
3. Falls back to basic camera setup if no prefab

Scene setup menu automatically adds camera bootstrap to new legacy scenes.

## Validation

Scene validation checks for:
- Camera bootstrap component exists
- Main camera is tagged properly
- Camera prefab is assigned (when available)

## Status: Stable ✅

**Verified Working**: Camera rig controller with proper namespace aliasing and bootstrap integration (single-writer camera contract).

**Bootstrap Integration**: Space4XCameraBootstrap ensures camera exists and is assigned to rig controller.

**Input System**: Uses Unity Input System actions for orbit, pan, zoom controls.

**ECS Integration**: Publishes CameraRigState via CameraRigService for ECS consumption.

## Contract Notes (Stop-the-Chaos)

- **No direct camera transform writes** from ECS systems or camera controllers.
- **Canonical truth** is `CameraRigState.Focus + Yaw/Pitch/Roll + Distance`; Unity transform is derived by `CameraRigApplier`.
- ECS focus/bookmark requests are emitted via `CameraRequestEvent` on the RTS input singleton; the active camera rig consumes them.

## Future Considerations

- Input actions currently referenced but may need wiring
- Camera prefab creation via editor menu
- Integration with existing input action assets
