# Camera System Debugging Guide

## Problem: Camera Not Moving with WASD/Input

If the camera doesn't respond to input, check the following:

## Step 1: Verify Systems Are Created

When entering Play mode, you should see these logs in the Console:
- `[Space4XCameraInitializationSystem] System created!`
- `[Space4XCameraInputSystem] System created!`
- `[Space4XCameraSystem] System created!`
- `[Space4XCameraRenderSyncSystem] System created!`

**If you DON'T see these logs:**
- Systems are not being discovered by Unity's bootstrap
- Check that systems are in an assembly that references Unity.Entities
- Check that systems have proper `[UpdateInGroup]` attributes
- Verify the systems compile without errors

## Step 2: Verify Initialization

After systems are created, you should see:
- `[Space4XCameraInitializationSystem] Camera state initialized at (x, y, z)`
- `[Space4XCameraInputSystem] Found Input Actions asset: InputSystem_Actions`
- `[Space4XCameraInputSystem] Input Actions initialized successfully`
- `[Space4XCameraRenderSyncSystem] Found Main Camera`

**If initialization fails:**
- Check that Main Camera GameObject exists with tag "MainCamera"
- Check that Input Actions asset exists at `Assets/InputSystem_Actions.inputactions`
- Check that Input Actions asset has a "Camera" action map

## Step 3: Verify Input Detection

When pressing WASD or scrolling, you should see:
- `[Space4XCameraInputSystem] Input detected - Pan: (x, y), Zoom: z, Rotate: (x, y)`

**If input is NOT detected:**
- Check Unity's Input System is enabled (Project Settings → Input System Package)
- Check that Input Actions asset is imported correctly
- Verify the Camera action map has proper bindings (WASD for Pan, Scroll for Zoom)

## Step 4: Verify Camera Update

When input is detected, you should see:
- `[Space4XCameraSystem] Processing input - Pan: (x, y), Zoom: z`
- `[Space4XCameraRenderSyncSystem] Syncing camera - Position: (x, y, z), Rotation: (q)`

**If camera state is NOT updating:**
- Check that camera state singleton exists
- Check that camera config has `EnablePan = true`, `EnableZoom = true`
- Verify camera system is in PresentationSystemGroup (runs after input)

## Step 5: Check Systems Window

In Unity Editor:
1. Window → Entities → Systems
2. Look for:
   - `Space4XCameraInitializationSystem` in InitializationSystemGroup
   - `Space4XCameraInputSystem` in InitializationSystemGroup  
   - `Space4XCameraSystem` in PresentationSystemGroup
   - `Space4XCameraRenderSyncSystem` in PresentationSystemGroup

**If systems are missing:**
- Systems aren't being discovered
- Check assembly definitions reference Unity.Entities
- Check for compilation errors preventing system discovery

## Common Issues

### No "System created!" logs
**Problem:** Systems not discovered
**Solution:** Check assembly definitions, ensure systems compile

### "Input Actions asset not found"
**Problem:** Asset path/GUID incorrect
**Solution:** Verify asset exists, check GUID in InitializeInputActions()

### "Camera action map not found"
**Problem:** Input Actions asset doesn't have "Camera" map
**Solution:** Add Camera action map to Input Actions asset

### Input detected but camera doesn't move
**Problem:** Camera state not updating or render sync failing
**Solution:** Check camera state singleton exists, check render sync system

### Camera moves but view doesn't update
**Problem:** Unity Camera component missing or not syncing
**Solution:** Ensure Main Camera has Camera component, check render sync logs


