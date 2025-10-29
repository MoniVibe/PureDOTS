# Camera Implementation Next Steps

**Status**: Core camera systems implemented. Ready for testing and integration.

## Verification Checklist

### Space4X Camera System

#### ✅ Code Files Created
- [x] `Assets/Scripts/Space4x/Registry/Space4XCameraComponents.cs` - DOTS components
- [x] `Assets/Scripts/Space4x/Authoring/Space4XCameraProfile.cs` - Profile ScriptableObject
- [x] `Assets/Scripts/Space4x/Authoring/Space4XCameraController.cs` - Input bridge
- [x] `Assets/Scripts/Space4x/Registry/Space4XCameraSystem.cs` - DOTS camera system

#### ✅ Configuration Updated
- [x] `Assets/InputSystem_Actions.inputactions` - Camera action map added
- [x] `Assets/Scripts/Space4x/Space4x.Gameplay.asmdef` - Unity.InputSystem reference

#### ⏳ Manual Steps Required

1. **Create Camera Profile Asset**
   - Location: `Assets/Space4X/Config/Space4XCameraProfile.asset`
   - Suggested defaults:
     - Pan Speed: 10
     - Pan Bounds Min: (-100, 0, -100)
     - Pan Bounds Max: (100, 100, 100)
     - Use Pan Bounds: false
     - Zoom Speed: 5
     - Zoom Min Distance: 10
     - Zoom Max Distance: 500
     - Rotation Speed: 90
     - Pitch Min: -30
     - Pitch Max: 85
     - Smoothing: 0.1

2. **Set Up Demo Scene Cameras**
   - Scenes: `Space4XMineLoop.unity` (and any Space4X demo scenes)
   - For each scene's "Main Camera":
     - Add Component: `Space4XCameraController`
     - Assign Input Actions: `Assets/InputSystem_Actions.inputactions`
     - Assign Profile: `Assets/Space4X/Config/Space4XCameraProfile.asset`
     - Enable Pan: ✓
     - Enable Zoom: ✓
     - Enable Rotation: ✗ (optional)

### Godgame Camera System

#### ✅ Code Files Created
- [x] `Assets/Scripts/Godgame/Interaction/Input/InputReaderSystem.cs` - Input reading
- [x] `Assets/Scripts/Godgame/Camera/CameraComponents.cs` - DOTS components
- [x] `Assets/Scripts/Godgame/Camera/CameraControlSystem.cs` - Camera logic
- [x] `Assets/Scripts/Godgame/Camera/CameraRenderBridge.cs` - Render bridge
- [x] `Assets/Scripts/Godgame/Authoring/CameraControllerAuthoring.cs` - Baker

#### ✅ Extended Components
- [x] `InputState` extended with Move, Vertical, CameraToggleMode
- [x] Input Actions enhanced with Camera action map

#### ⏳ Remaining Implementation

1. **CameraTerrainRaycastSystem** (for full BW2 orbital mode)
   - Terrain raycasts for grab plane
   - Pivot point establishment
   - Cursor position queries

2. **Complete Orbital Mode Features**
   - LMB pan with grab plane
   - Zoom toward cursor (not pivot)
   - Terrain collision (2m clearance)
   - Distance-scaled orbit sensitivity

3. **Scene Setup**
   - Add `CameraControllerAuthoring` to camera GameObjects
   - Configure initial camera mode and settings

## Testing Plan

### Space4X Camera Tests

1. **Basic Controls**
   - [ ] WASD pan works
   - [ ] Q/E vertical movement works (if implemented)
   - [ ] Mouse scroll zoom works
   - [ ] Camera respects zoom limits
   - [ ] Pan bounds work (if enabled)

2. **Feature Toggles**
   - [ ] Pan can be disabled in inspector
   - [ ] Zoom can be disabled in inspector
   - [ ] Rotation can be disabled in inspector

3. **Profile Configuration**
   - [ ] Changing profile values affects camera behavior
   - [ ] Default profile values are sensible

### Godgame Camera Tests

1. **Mode Toggle**
   - [ ] Tab key switches between RTS/Free-fly and Orbital modes
   - [ ] Mode switch is debounced (no rapid switching)
   - [ ] Visual indicator of current mode (if implemented)

2. **RTS/Free-fly Mode**
   - [ ] WASD moves camera forward/back/left/right relative to rotation
   - [ ] Q/E moves camera up/down
   - [ ] Mouse look rotates camera
   - [ ] Scroll wheel zooms (moves forward/back)

3. **Orbital Mode** (when fully implemented)
   - [ ] MMB establishes pivot point
   - [ ] MMB drag orbits around pivot
   - [ ] Pitch limits enforced (-30° to +85°)
   - [ ] Distance-scaled sensitivity works
   - [ ] Scroll zooms toward cursor position
   - [ ] Terrain collision maintains 2m clearance

4. **Rewind Safety**
   - [ ] Camera updates skipped during rewind playback
   - [ ] Camera state is deterministic

## Integration Checklist

### PureDOTS Base Components (Future)

If implementing shared base components per `CameraIntegrationArchitecture.md`:

1. Create `PureDOTS.Runtime.Camera` namespace with:
   - `CameraControlInput` singleton
   - `CameraState` singleton
   - `CameraConfig` singleton (extensible)

2. Create abstract `CameraInputBridge` MonoBehaviour

3. Add `CameraSystemGroup` to `SystemGroups.cs`

4. Update both Space4X and Godgame cameras to extend base

### Conflict Prevention

- [x] Space4X uses "Camera" action map (distinct)
- [x] Godgame uses separate actions from Space4X
- [ ] Both cameras can coexist in same scene (different GameObjects)
- [ ] Input action maps don't conflict

## Next Immediate Steps

1. **Create Space4X Profile Asset** (using Unity Editor or MCP tools)
2. **Set Up Space4X Demo Scene** (configure Main Camera)
3. **Test Space4X Camera** (verify controls work)
4. **Complete Godgame Terrain Raycast System** (for full orbital mode)
5. **Test Godgame Camera** (verify both modes work)
6. **Integration Test** (both cameras in same scene if needed)

## Known Issues / TODOs

- Godgame orbital mode needs terrain raycast system for full BW2 parity
- Space4X rotation bounds may need testing/adjustment
- Both systems should integrate with hand cursor position (Godgame priority)
- Profile assets need to be created and configured
- Scene setup needs to be done for each demo scene

## Documentation Updates Needed

- [ ] Update `DivineHandCamera_TODO.md` with implementation status
- [ ] Document Space4X camera usage in game-specific docs
- [ ] Document Godgame camera usage and BW2 parity status
- [ ] Update `CameraIntegrationArchitecture.md` with final implementation details

