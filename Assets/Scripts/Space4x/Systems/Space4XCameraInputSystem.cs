using PureDOTS.Runtime.Camera;
using Space4X.CameraComponents;
using PureDOTS.Runtime.Components;
using Space4X.CameraControls;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Space4X.Systems
{
    /// <summary>
    /// DOTS system that reads Unity Input System and writes to Space4XCameraInput singleton.
    /// Runs in CameraInputSystemGroup (OrderFirst in SimulationSystemGroup) for highest priority, real-time input.
    /// This ensures input processing is never queued or delayed by other systems.
    /// Not Burst-compiled (uses managed Input System API).
    /// </summary>
    [UpdateInGroup(typeof(PureDOTS.Systems.CameraInputSystemGroup))]
    [UpdateBefore(typeof(Space4XCameraSystem))]
    public partial class Space4XCameraInputSystem : SystemBase
    {
        private InputActionMap _cameraMap;
        private InputAction _panAction;
        private InputAction _verticalPanAction;
        private InputAction _zoomAction;
        private InputAction _rotateAction;
        private InputAction _resetAction;
        private InputAction _togglePerspectiveAction;
        private bool _initialized;
        private InputActionAsset _inputActionsInstance;
        private bool _loggedMissingAsset;
        private bool _loggedMissingCameraMap;

        protected override void OnCreate()
        {
            // Don't require input singleton - we'll create it
            // Try to initialize immediately
            InitializeInputActions();
            if (!_initialized)
            {
                Enabled = false; // Disabled until initialized
            }
        }

        protected override void OnUpdate()
        {
            if (CameraRigService.IsEcsCameraEnabled)
            {
                return;
            }

            // Initialize Input Actions if not done
            if (!_initialized)
            {
                InitializeInputActions();
                if (!_initialized)
                {
                    return; // Can't initialize yet
                }
            }

            // Ensure input singleton exists
            Entity inputEntity;
            if (!SystemAPI.TryGetSingletonEntity<Space4XCameraInput>(out inputEntity))
            {
                inputEntity = EntityManager.CreateEntity(typeof(Space4XCameraInput));
                EntityManager.SetComponentData(inputEntity, new Space4XCameraInput());
            }

            if (Space4XCameraMouseController.TryGetLatestState(out _))
            {
                if (SystemAPI.TryGetSingletonRW<CameraInputBudget>(out var monoBudget))
                {
                    monoBudget.ValueRW = default;
                }
                SystemAPI.SetComponent(inputEntity, default(Space4XCameraInput));
                return;
            }

            var panInput = Vector2.zero;
            var verticalPanInput = 0f;
            var zoomInput = 0f;
            var rotateInput = Vector2.zero;
            var resetRequested = false;
            var togglePerspectiveMode = false;
            RefRW<CameraInputBudget> budgetHandle;
            var hasBudget = SystemAPI.TryGetSingletonRW<CameraInputBudget>(out budgetHandle);

            if (Space4XCameraInputBridge.TryGetSnapshot(out var snapshot))
            {
                panInput = snapshot.Pan;
                verticalPanInput = snapshot.VerticalPan;
                zoomInput = snapshot.Zoom;
                rotateInput = snapshot.Rotate;
                resetRequested = snapshot.ResetRequested;
                togglePerspectiveMode = snapshot.TogglePerspectiveMode;

                if (hasBudget)
                {
                    ref var budget = ref budgetHandle.ValueRW;
                    var frameId = (uint)snapshot.Frame;
                    if (frameId != budget.BridgeFrameId)
                    {
                        var rotateDelta = new float2(snapshot.Rotate.x, snapshot.Rotate.y);
                        if (math.lengthsq(rotateDelta) > float.Epsilon)
                        {
                            budget.RotateBudget += rotateDelta;
                        }

                        if (math.abs(snapshot.Zoom) > float.Epsilon)
                        {
                            budget.ZoomBudget += snapshot.Zoom;
                        }

                        budget.BridgeFrameId = frameId;
                    }

                    rotateInput = Vector2.zero;
                    zoomInput = 0f;
                }
            }
            else
            {
                var mouse = Mouse.current;
                var keyboard = Keyboard.current;

                // Read keyboard pan input (WASD) - real-time, direct keyboard state reading
                if (keyboard != null)
                {
                    float x = 0f;
                    float y = 0f;

                    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                        y += 1f;
                    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                        y -= 1f;
                    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                        x -= 1f;
                    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                        x += 1f;

                    panInput = new Vector2(x, y);

                    if (panInput.magnitude > 1f)
                    {
                        panInput.Normalize();
                    }
                }
                else if (_panAction != null)
                {
                    // Fallback to input action if keyboard is not available
                    panInput = _panAction.ReadValue<Vector2>();
                    if (panInput.magnitude < 0.001f)
                    {
                        panInput = Vector2.zero;
                    }
                }

                if (mouse != null && mouse.middleButton.isPressed)
                {
                    var mouseDelta = mouse.delta.ReadValue();
                    panInput += new Vector2(mouseDelta.x, -mouseDelta.y) * 0.01f;
                }

                if (mouse != null)
                {
                    var scrollDelta = mouse.scroll.ReadValue();
                    if (math.abs(scrollDelta.y) > 0f)
                    {
                        zoomInput = -scrollDelta.y;
                    }
                }
                else if (_zoomAction != null)
                {
                    try
                    {
                        var rawZoom = _zoomAction.ReadValue<float>();
                        if (math.abs(rawZoom) > 0.001f)
                        {
                            zoomInput = -rawZoom;
                        }
                    }
                    catch
                    {
                        try
                        {
                            var rawZoom = _zoomAction.ReadValue<Vector2>().y;
                            if (math.abs(rawZoom) > 0.001f)
                            {
                                zoomInput = -rawZoom;
                            }
                        }
                        catch
                        {
                            zoomInput = 0f;
                        }
                    }
                }

                if (keyboard != null)
                {
                    if (keyboard.eKey.isPressed)
                        verticalPanInput = 1f;
                    else if (keyboard.qKey.isPressed)
                        verticalPanInput = -1f;
                }
                else if (_verticalPanAction != null)
                {
                    var rawVerticalPan = _verticalPanAction.ReadValue<float>();
                    if (math.abs(rawVerticalPan) > 0.001f)
                    {
                        verticalPanInput = rawVerticalPan;
                    }
                }

                if (mouse != null && mouse.rightButton.isPressed)
                {
                    rotateInput = mouse.delta.ReadValue();
                }

                resetRequested = _resetAction != null && _resetAction.WasPressedThisFrame();

                if (_togglePerspectiveAction != null)
                {
                    togglePerspectiveMode = _togglePerspectiveAction.WasPressedThisFrame();
                }
                else if (keyboard != null)
                {
                    togglePerspectiveMode = keyboard.vKey.wasPressedThisFrame;
                }

                if (hasBudget)
                {
                    ref var budget = ref budgetHandle.ValueRW;
                    if (rotateInput.sqrMagnitude > 0.0001f || math.abs(zoomInput) > 0.0001f)
                    {
                        budget.RotateBudget += new float2(rotateInput.x, rotateInput.y);
                        budget.ZoomBudget += zoomInput;
                        budget.BridgeFrameId += 1u;
                    }

                    rotateInput = Vector2.zero;
                    zoomInput = 0f;
                }
            }

            if (hasBudget)
            {
                ref var budget = ref budgetHandle.ValueRW;
                budget.TicksToSpend = math.max(0, budget.TicksToSpend);
                budget.TicksToSpend += 1;
            }

            // Write to DOTS singleton - always write fresh values to ensure keys are cleared when released
            // CRITICAL: Always create a fresh input struct to ensure zero values when keys are released
            var input = new Space4XCameraInput
            {
                PanInput = panInput,
                VerticalPanInput = verticalPanInput,
                ZoomInput = zoomInput,
                RotateInput = rotateInput,
                ResetRequested = resetRequested,
                TogglePerspectiveMode = togglePerspectiveMode
            };

            // Always update the singleton with fresh values to clear any stale input
            // This ensures that when all keys are released, all input values are zero
            SystemAPI.SetComponent(inputEntity, input);
        }

        private void InitializeInputActions()
        {
            var projectAsset = LoadProjectInputAsset();
            if (projectAsset == null)
            {
                _initialized = false;
                Enabled = false;
                return;
            }

            DisposeInputActions();
            _initialized = false;

            _inputActionsInstance = Object.Instantiate(projectAsset);
            _inputActionsInstance.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

            _cameraMap = _inputActionsInstance.FindActionMap("Camera", throwIfNotFound: false);
            if (_cameraMap == null)
            {
                if (!_loggedMissingCameraMap)
                {
                    Debug.LogError("[Space4XCameraInputSystem] Input Actions asset is missing a 'Camera' action map.");
                    _loggedMissingCameraMap = true;
                }
                Enabled = false;
                return;
            }

            _loggedMissingCameraMap = false;

            _panAction = _cameraMap.FindAction("Pan", throwIfNotFound: false);
            _verticalPanAction = _cameraMap.FindAction("VerticalPan", throwIfNotFound: false);
            _zoomAction = _cameraMap.FindAction("Zoom", throwIfNotFound: false);
            _rotateAction = _cameraMap.FindAction("Rotate", throwIfNotFound: false);
            _resetAction = _cameraMap.FindAction("Reset", throwIfNotFound: false);
            _togglePerspectiveAction = _cameraMap.FindAction("TogglePerspectiveMode", throwIfNotFound: false);

            if (!_cameraMap.enabled)
            {
                _cameraMap.Enable();
            }

            _initialized = true;
            Enabled = true;
        }

        private InputActionAsset LoadProjectInputAsset()
        {
            InputActionAsset inputActionsAsset = null;

#if UNITY_EDITOR
            var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath("052faaac586de48259a63d0c4782560b");
            if (!string.IsNullOrEmpty(assetPath))
            {
                inputActionsAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            }

            if (inputActionsAsset == null)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("InputSystem_Actions t:InputActionAsset");
                if (guids.Length > 0)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    inputActionsAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                }
            }
#else
            inputActionsAsset = Resources.Load<InputActionAsset>("InputSystem_Actions");
#endif

            if (inputActionsAsset == null)
            {
                if (!_loggedMissingAsset)
                {
                    Debug.LogError("[Space4XCameraInputSystem] Input Actions asset not found. Ensure 'InputSystem_Actions.inputactions' is available to the player.");
                    _loggedMissingAsset = true;
                }
                return null;
            }

            _loggedMissingAsset = false;
            return inputActionsAsset;
        }

        private void DisposeInputActions()
        {
            if (_cameraMap != null && _cameraMap.enabled)
            {
                _cameraMap.Disable();
            }

            _cameraMap = null;
            _panAction = null;
            _verticalPanAction = null;
            _zoomAction = null;
            _rotateAction = null;
            _resetAction = null;
            _togglePerspectiveAction = null;

            if (_inputActionsInstance != null)
            {
                _inputActionsInstance.Disable();
                _inputActionsInstance = null;
            }

            _initialized = false;
        }

        protected override void OnDestroy()
        {
            DisposeInputActions();
            base.OnDestroy();
        }
    }
}

