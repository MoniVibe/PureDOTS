# Runtime Auto-Setup Solution

I've created a **runtime auto-setup script** that will automatically add the camera components when you enter Play mode.

## What Was Created:

1. **Space4XCameraAutoSetup.cs** - A MonoBehaviour that runs at Start() and automatically:
   - Finds Main Camera
   - Adds Space4XCameraController if missing
   - Adds Space4XCameraRenderBridge if missing  
   - Assigns Input Actions and Profile if provided

2. **CameraAutoSetup GameObject** - Created in your scene (at origin)

## To Complete Setup:

**Option 1: Configure the Auto-Setup Component (Recommended)**

1. In Unity Editor, select **CameraAutoSetup** GameObject
2. In Inspector, find **Space4X Camera Auto Setup** component
3. Drag `Assets/InputSystem_Actions.inputactions` to **Input Actions** field
4. Drag `Assets/Space4X/Config/Space4XCameraProfile.asset` to **Profile** field
5. Enter Play mode - it will automatically set up Main Camera!

**Option 2: Manual Setup (If Auto-Setup Doesn't Work)**

1. Select **Main Camera** in Hierarchy
2. Add Component → `Space4XCameraController`
3. Add Component → `Space4XCameraRenderBridge`
4. Configure Space4XCameraController:
   - Input Actions: `Assets/InputSystem_Actions.inputactions`
   - Profile: `Assets/Space4X/Config/Space4XCameraProfile.asset`

## How It Works:

The auto-setup script runs in `Start()` when you enter Play mode. It:
- Finds Main Camera by tag
- Checks if components exist
- Adds them if missing
- Configures them automatically
- Then disables itself

## Debugging:

Check Unity Console for:
- `[Space4XCameraAutoSetup] Added Space4XCameraController...`
- `[Space4XCameraController] Initializing camera controller...`
- `[Space4XCameraRenderBridge] Camera Position: ...`

If you see warnings, the setup didn't complete properly.

