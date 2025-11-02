using PureDOTS.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Space4X.Registry
{
    /// <summary>
    /// Pure DOTS system that reads Input System actions directly and writes to camera control state.
    /// Runs early in PresentationSystemGroup to capture input before camera update.
    /// Non-Burst compatible due to Input System access.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(Space4XCameraSystem))]
    public partial struct Space4XCameraInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Space4XCameraInputConfig>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstDiscard]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<Space4XCameraInputConfig>(out var inputConfig))
            {
                return;
            }

            var inputActions = FindInputActionAsset();
            if (inputActions == null)
            {
                return;
            }

            // Always get fresh references - don't cache Unity objects in struct systems
            InputActionMap cameraActionMap = null;
            InputAction panAction = null;
            InputAction zoomAction = null;
            InputAction verticalMoveAction = null;
            InputAction rotateAction = null;
            InputAction resetAction = null;
            InputAction toggleVerticalModeAction = null;

            try
            {
                cameraActionMap = inputActions.FindActionMap("Camera");
                if (cameraActionMap != null)
                {
                    panAction = cameraActionMap.FindAction("Pan");
                    zoomAction = cameraActionMap.FindAction("Zoom");
                    verticalMoveAction = cameraActionMap.FindAction("VerticalMove");
                    rotateAction = cameraActionMap.FindAction("Rotate");
                    resetAction = cameraActionMap.FindAction("Reset");
                    toggleVerticalModeAction = cameraActionMap.FindAction("ToggleVerticalMode");

                    if (!cameraActionMap.enabled)
                    {
                        cameraActionMap.Enable();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Space4XCameraInputSystem: Error initializing input actions: {ex.Message}");
                return;
            }

            if (cameraActionMap == null || panAction == null || zoomAction == null || verticalMoveAction == null || rotateAction == null || resetAction == null || toggleVerticalModeAction == null)
            {
                return;
            }

            var controlState = ReadInputActions(inputConfig, panAction, zoomAction, verticalMoveAction, rotateAction, resetAction, toggleVerticalModeAction);
            
            if (!SystemAPI.HasSingleton<Space4XCameraControlState>())
            {
                var entity = state.EntityManager.CreateEntity(typeof(Space4XCameraControlState));
                SystemAPI.SetComponent(entity, controlState);
            }
            else
            {
                var entity = SystemAPI.GetSingletonEntity<Space4XCameraControlState>();
                SystemAPI.SetComponent(entity, controlState);
            }
        }

        [BurstDiscard]
        private InputActionAsset FindInputActionAsset()
        {
            try
            {
                var authoring = Object.FindFirstObjectByType<Space4XCameraInputAuthoring>();
                if (authoring != null)
                {
                    var actions = authoring.InputActions;
                    if (actions != null)
                    {
                        return actions;
                    }
                    else
                    {
                        Debug.LogWarning("Space4XCameraInputSystem: InputActionAsset is null on authoring component.");
                    }
                }
                else
                {
                    Debug.LogWarning("Space4XCameraInputSystem: Space4XCameraInputAuthoring component not found in scene.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Space4XCameraInputSystem: Error finding InputActionAsset: {ex.Message}");
            }
            
            return null;
        }

        [BurstDiscard]
        private Space4XCameraControlState ReadInputActions(
            Space4XCameraInputConfig config,
            InputAction panAction,
            InputAction zoomAction,
            InputAction verticalMoveAction,
            InputAction rotateAction,
            InputAction resetAction,
            InputAction toggleVerticalModeAction)
        {
            Vector2 panValue = Vector2.zero;
            Vector2 zoomValue = Vector2.zero;
            float verticalMoveValue = 0f;
            Vector2 rotateValue = Vector2.zero;
            bool resetPressed = false;
            bool toggleVerticalModePressed = false;

            try
            {
                panValue = panAction.ReadValue<Vector2>();
                zoomValue = zoomAction.ReadValue<Vector2>();
                verticalMoveValue = verticalMoveAction.ReadValue<float>();
                rotateValue = rotateAction.ReadValue<Vector2>();
                resetPressed = resetAction.WasPressedThisFrame();
                toggleVerticalModePressed = toggleVerticalModeAction.WasPressedThisFrame();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Space4XCameraInputSystem: Error reading input values: {ex.Message}");
            }

            bool rightMouseHeld = false;
            try
            {
                rightMouseHeld = Mouse.current != null && Mouse.current.rightButton.isPressed;
            }
            catch
            {
                // Mouse may be unavailable
            }

            var effectiveRotateInput = (config.EnableRotation && rightMouseHeld) ? rotateValue : Vector2.zero;

            return new Space4XCameraControlState
            {
                PanInput = math.float2(panValue.x, panValue.y),
                ZoomInput = zoomValue.y != 0f ? zoomValue.y : (zoomValue.x != 0f ? zoomValue.x : 0f),
                VerticalMoveInput = verticalMoveValue,
                RotateInput = math.float2(effectiveRotateInput.x, effectiveRotateInput.y),
                ResetRequested = resetPressed,
                ToggleVerticalModeRequested = toggleVerticalModePressed,
                EnablePan = config.EnablePan,
                EnableZoom = config.EnableZoom,
                EnableVerticalMove = config.EnableVerticalMove,
                EnableRotation = config.EnableRotation && rightMouseHeld
            };
        }
    }
}
