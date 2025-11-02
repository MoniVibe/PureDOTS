# Space4X Camera Setup Instructions

## Problem
The scene camera doesn't have the required Space4X camera components, causing "No cameras rendering" error.

## Solution

### Option 1: Use Editor Menu (Recommended)
1. In Unity Editor, select the **Main Camera** GameObject
2. Go to **Tools > Space4X > Setup Camera**
3. This will automatically:
   - Add `Space4XCameraController` component
   - Add `Space4XCameraRenderBridge` component
   - Assign Input Actions asset (`Assets/InputSystem_Actions.inputactions`)
   - Assign Camera Profile (`Assets/Space4X/Config/Space4XCameraProfile.asset`)

### Option 2: Manual Setup
1. Select the **Main Camera** GameObject in the scene
2. Click **Add Component** button
3. Search for and add:
   - `Space4XCameraController`
   - `Space4XCameraRenderBridge`
4. Configure `Space4XCameraController`:
   - **Input Actions**: Drag `Assets/InputSystem_Actions.inputactions` into the field
   - **Profile**: Drag `Assets/Space4X/Config/Space4XCameraProfile.asset` into the field

## Required Components Summary
- **Space4XCameraController**: Reads Unity Input System and writes to DOTS singletons
- **Space4XCameraRenderBridge**: Syncs DOTS camera state to Unity Camera transform

## Verification
After setup, enter Play mode and:
- Camera should display the scene (not black screen)
- WASD keys should pan the camera
- Scroll wheel should zoom
- Right mouse drag should rotate (if enabled in profile)

