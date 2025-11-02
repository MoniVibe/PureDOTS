# Quick Fix: Camera Not Rendering

## Problem
The camera shows "No cameras rendering" because the required Space4X camera components are missing.

## Immediate Fix (Manual - Do This Now)

**You need to add 2 components to the Main Camera GameObject:**

1. **In Unity Editor:**
   - Select **Main Camera** in the Hierarchy
   - Click **Add Component** button
   - Search for: `Space4XCameraController`
   - Add it
   - Search for: `Space4XCameraRenderBridge`  
   - Add it

2. **Configure Space4XCameraController:**
   - **Input Actions**: Drag `Assets/InputSystem_Actions.inputactions` into the field
   - **Profile**: Drag `Assets/Space4X/Config/Space4XCameraProfile.asset` into the field

3. **Save the scene** (Ctrl+S)

4. **Enter Play mode** - the camera should now render!

## Why This Happens

The `Space4XCameraRenderBridge` component syncs DOTS camera state to the Unity Camera transform. Without these components:
- No input is being read from Unity Input System
- No camera state singleton is created
- The camera system isn't running
- Result: "No cameras rendering"

## Automated Setup (Future)

Once Unity finishes compiling, you can use:
- **Tools > Space4X > Setup Camera** menu item (auto-adds components)

