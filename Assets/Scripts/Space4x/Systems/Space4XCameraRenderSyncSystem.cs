using Space4X.CameraComponents;
using Space4X.CameraControls;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using UnityEngine;
using PureDOTS.Runtime.Camera;

namespace Space4X.Systems
{
    /// <summary>
    /// DOTS system that syncs camera state singleton to Unity Camera GameObject transform.
    /// Runs in PresentationSystemGroup after camera update, before rendering.
    /// Not Burst-compiled (uses managed GameObject API).
    /// </summary>
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    public partial class Space4XCameraRenderSyncSystem : SystemBase
    {
        private Camera _unityCamera;
        private bool _cameraFound;
        private bool _duplicateCamerasResolved;
        private bool _duplicateWarningLogged;

        protected override void OnCreate()
        {
            Debug.Log("[Space4XCameraRenderSyncSystem] System created!");
            // Don't require singleton - allow system to run even if state doesn't exist yet
            // This ensures we can create the camera GameObject even before initialization
            // RequireForUpdate<Space4XCameraState>();
        }

        protected override void OnUpdate()
        {
            // Find Unity Camera GameObject if not cached
            if (!_cameraFound)
            {
                // First, disable any other cameras that might be interfering
                var allCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                Camera mainCameraFound = null;
                foreach (var cam in allCameras)
                {
                    if (!cam.CompareTag("MainCamera"))
                    {
                        continue;
                    }

                    if (mainCameraFound == null)
                    {
                        mainCameraFound = cam;
                    }
                    else
                    {
                        if (!_duplicateWarningLogged)
                        {
                            Debug.LogWarning($"[Space4XCameraRenderSyncSystem] Multiple cameras with MainCamera tag found: {mainCameraFound.name} and {cam.name}. Disabling and retagging {cam.name}.");
                            _duplicateWarningLogged = true;
                        }

                        cam.enabled = false;
                        try
                        {
                            cam.tag = "Untagged";
                        }
                        catch
                        {
                            // Ignore if tag assignment fails; disabling still prevents conflicts.
                        }
                    }
                }

                if (!_duplicateCamerasResolved && mainCameraFound != null)
                {
                    _duplicateCamerasResolved = true;
                }
                
                var mainCameraObject = GameObject.FindWithTag("MainCamera");
                
                // If not found by tag, try by name
                if (mainCameraObject == null)
                {
                    mainCameraObject = GameObject.Find("Main Camera");
                }
                
                // If still not found, try Camera.main
                if (mainCameraObject == null && Camera.main != null)
                {
                    mainCameraObject = Camera.main.gameObject;
                    Debug.Log($"[Space4XCameraRenderSyncSystem] Found Camera.main: {mainCameraObject.name}");
                }
                
                // If still not found, create it
                if (mainCameraObject == null)
                {
                    Debug.Log("[Space4XCameraRenderSyncSystem] Main Camera GameObject not found - creating it...");
                    mainCameraObject = new GameObject("Main Camera");
                    mainCameraObject.tag = "MainCamera";
                    // Position camera for good demo overview: higher up and further back
                    mainCameraObject.transform.position = new Vector3(0f, 25f, -30f);
                    mainCameraObject.transform.rotation = Quaternion.Euler(40f, 0f, 0f); // Look down at 40 degrees for good overview
                }
                else
                {
                    // If camera exists but is positioned awkwardly, reposition for better demo viewing
                    var camPos = mainCameraObject.transform.position;
                    var camRot = mainCameraObject.transform.rotation;
                    var euler = camRot.eulerAngles;
                    var pitchDegrees = euler.x;
                    if (pitchDegrees > 180f) pitchDegrees -= 360f;
                    
                    // Check if camera needs repositioning: too high/low, too close, or wrong angle
                    bool needsRepositioning = camPos.y > 40f || camPos.y < 10f || camPos.z > -15f || pitchDegrees < 20f || pitchDegrees < 0f;
                    
                    if (needsRepositioning)
                    {
                        Debug.Log($"[Space4XCameraRenderSyncSystem] Camera at {camPos} with rotation {euler} seems awkward, repositioning for better demo viewing...");
                        mainCameraObject.transform.position = new Vector3(0f, 25f, -30f);
                        mainCameraObject.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
                    }
                }
                
                // Disable any other cameras (except the one we want to use)
                foreach (var cam in allCameras)
                {
                    if (cam.gameObject != mainCameraObject && cam.enabled)
                    {
                        Debug.Log($"[Space4XCameraRenderSyncSystem] Disabling camera {cam.name} to prevent conflicts");
                        cam.enabled = false;
                    }
                }
                
                if (mainCameraObject != null)
                {
                    _unityCamera = mainCameraObject.GetComponent<Camera>();
                    
                    // If Camera component doesn't exist, add it
                    if (_unityCamera == null)
                    {
                        Debug.Log($"[Space4XCameraRenderSyncSystem] Main Camera GameObject found but missing Camera component, adding it...");
                        _unityCamera = mainCameraObject.AddComponent<Camera>();
                        _unityCamera.clearFlags = CameraClearFlags.SolidColor; // Use solid color for reliability
                        _unityCamera.backgroundColor = new Color(0.3f, 0.5f, 0.7f, 1f); // Sky blue background
                        _unityCamera.fieldOfView = 60f;
                        _unityCamera.nearClipPlane = 0.3f;
                        _unityCamera.farClipPlane = 1000f;
                        _unityCamera.depth = 0; // Ensure it's the main camera
                        _unityCamera.enabled = true; // Ensure camera is enabled
                        Debug.Log($"[Space4XCameraRenderSyncSystem] Camera component added and enabled. ClearFlags: {_unityCamera.clearFlags}, FOV: {_unityCamera.fieldOfView}, Enabled: {_unityCamera.enabled}, BG Color: {_unityCamera.backgroundColor}");
                    }
                    else
                    {
                        // Ensure camera settings are correct even if component already exists
                        if (_unityCamera.clearFlags == CameraClearFlags.Skybox)
                        {
                            Debug.Log("[Space4XCameraRenderSyncSystem] Camera has Skybox clear flags, changing to SolidColor for reliability...");
                            _unityCamera.clearFlags = CameraClearFlags.SolidColor;
                            _unityCamera.backgroundColor = new Color(0.3f, 0.5f, 0.7f, 1f);
                        }
                        if (!_unityCamera.enabled)
                        {
                            Debug.LogWarning("[Space4XCameraRenderSyncSystem] Camera was disabled, enabling...");
                            _unityCamera.enabled = true;
                        }
                        // Ensure depth is 0 or negative for main camera
                        if (_unityCamera.depth > 0)
                        {
                            _unityCamera.depth = 0;
                        }
                    }
                    
                    // Ensure MainCamera tag is set
                    if (!mainCameraObject.CompareTag("MainCamera"))
                    {
                        try
                        {
                            mainCameraObject.tag = "MainCamera";
                        }
                        catch
                        {
                            Debug.LogWarning("[Space4XCameraRenderSyncSystem] Could not set MainCamera tag");
                        }
                    }
                    
                    _cameraFound = true;
                    Debug.Log($"[Space4XCameraRenderSyncSystem] Found/Created Main Camera at {mainCameraObject.transform.position}, Depth: {_unityCamera.depth}, Enabled: {_unityCamera.enabled}");
                }

                if (!_cameraFound)
                {
                    if (UnityEngine.Time.frameCount % 60 == 0)
                    {
                        Debug.LogWarning("[Space4XCameraRenderSyncSystem] Failed to find or create Main Camera GameObject - cannot sync camera state!");
                    }
                    return; // Camera not found yet
                }
            }

            // Get camera state singleton
            if (!SystemAPI.TryGetSingleton<Space4XCameraState>(out var cameraState))
            {
                // If state doesn't exist yet, still ensure camera GameObject exists
                // This helps with early initialization
                if (!_cameraFound && UnityEngine.Time.frameCount % 60 == 0)
                {
                    Debug.Log("[Space4XCameraRenderSyncSystem] Camera state not found yet, but ensuring camera GameObject exists...");
                }
                return;
            }

            // Sync DOTS state to Unity Camera transform
            if (_unityCamera != null)
            {
                // Check if BW2StyleCameraController is attached and disable it (it runs in LateUpdate and overrides our camera)
                var bw2Controller = _unityCamera.GetComponent<BW2StyleCameraController>();
                if (bw2Controller != null && bw2Controller.enabled)
                {
                    Debug.LogWarning($"[Space4XCameraRenderSyncSystem] Found BW2StyleCameraController on {_unityCamera.name}, disabling it to prevent conflicts with DOTS camera system");
                    bw2Controller.enabled = false;
                }
                
                // Also check parent/child for BW2StyleCameraController
                var parentController = _unityCamera.transform.parent?.GetComponent<BW2StyleCameraController>();
                if (parentController != null && parentController.enabled)
                {
                    Debug.LogWarning($"[Space4XCameraRenderSyncSystem] Found BW2StyleCameraController on parent {_unityCamera.transform.parent.name}, disabling it to prevent conflicts");
                    parentController.enabled = false;
                }
                
                // Also search globally for any BW2StyleCameraController that might be controlling Camera.main
                // BW2StyleCameraController defaults to Camera.main if targetCamera is null, so disable all instances
                // to prevent conflicts with our DOTS camera system
                var allControllers = Object.FindObjectsByType<BW2StyleCameraController>(FindObjectsSortMode.None);
                foreach (var controller in allControllers)
                {
                    if (controller.enabled)
                    {
                        // Check if controller is on the same GameObject, parent, or child of our camera
                        var controllerCamera = controller.GetComponent<Camera>();
                        bool isRelatedToOurCamera = false;
                        
                        if (controllerCamera == _unityCamera)
                        {
                            isRelatedToOurCamera = true;
                        }
                        else if (_unityCamera.transform != null)
                        {
                            var parent = _unityCamera.transform.parent;
                            if (parent != null && controller.transform == parent)
                            {
                                isRelatedToOurCamera = true;
                            }
                            // Check if controller is in children
                            var children = _unityCamera.GetComponentsInChildren<Camera>();
                            if (System.Array.Exists(children, c => c.gameObject == controller.gameObject))
                            {
                                isRelatedToOurCamera = true;
                            }
                        }
                        
                        // Disable if related to our camera OR if Camera.main is our camera (BW2StyleCameraController falls back to Camera.main)
                        if (isRelatedToOurCamera || Camera.main == _unityCamera)
                        {
                            Debug.LogWarning($"[Space4XCameraRenderSyncSystem] Found BW2StyleCameraController on {controller.gameObject.name} - disabling to prevent conflicts with DOTS camera system");
                            controller.enabled = false;
                        }
                    }
                }
                
                var oldPos = _unityCamera.transform.position;
                var oldRot = _unityCamera.transform.rotation;
                
                // Ensure scale is correct (should always be 1,1,1 for cameras)
                if (_unityCamera.transform.localScale != Vector3.one)
                {
                    _unityCamera.transform.localScale = Vector3.one;
                }
                
                // Force update every frame to ensure camera follows DOTS state
                var monoOwnsCamera = Space4XCameraMouseController.TryGetLatestState(out _);
                if (!monoOwnsCamera)
                {
                    _unityCamera.transform.position = cameraState.Position;
                    _unityCamera.transform.rotation = cameraState.Rotation;
                }
                
                // Reduced logging - only log on initialization (excessive logging causes performance hiccups)
                if (UnityEngine.Time.frameCount <= 3)
                {
                    var rotQuat = new Quaternion(cameraState.Rotation.value.x, cameraState.Rotation.value.y, cameraState.Rotation.value.z, cameraState.Rotation.value.w);
                    var rotEuler = rotQuat.eulerAngles;
                    Debug.Log($"[Space4XCameraRenderSyncSystem] Initialized - Camera syncing: Pos: {cameraState.Position}, Rot: {rotEuler}, Frustum: Near={_unityCamera.nearClipPlane}, Far={_unityCamera.farClipPlane}, FOV={_unityCamera.fieldOfView}, Aspect={_unityCamera.aspect}");
                }
                
                // Ensure camera stays enabled and is set as the main camera
                if (!_unityCamera.enabled)
                {
                    Debug.LogWarning("[Space4XCameraRenderSyncSystem] Camera was disabled! Re-enabling...");
                    _unityCamera.enabled = true;
                }
                
                // Ensure this camera has depth <= 0 so it renders (main camera should have depth 0 or negative)
                if (_unityCamera.depth > 0)
                {
                    Debug.LogWarning($"[Space4XCameraRenderSyncSystem] Camera depth is {_unityCamera.depth}, setting to 0 to ensure it renders");
                    _unityCamera.depth = 0;
                }
                
                // Ensure this is the main camera (Camera.main should return this)
                if (Camera.main != _unityCamera)
                {
                    Debug.LogWarning($"[Space4XCameraRenderSyncSystem] Camera.main is {Camera.main?.name ?? "null"}, but we're syncing {_unityCamera.name}. This may cause rendering issues!");
                }
            }
            else
            {
                if (UnityEngine.Time.frameCount % 60 == 0)
                {
                    Debug.LogWarning("[Space4XCameraRenderSyncSystem] Unity Camera not found - cannot sync camera state!");
                }
            }
        }
    }
}

