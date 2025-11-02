using Space4X.CameraComponents;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Space4X.Systems
{
    /// <summary>
    /// DOTS system that initializes camera state and config singletons.
    /// Runs once in InitializationSystemGroup at startup.
    /// Note: Space4XCameraInputSystem runs in SimulationSystemGroup, so this always runs first.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct Space4XCameraInitializationSystem : ISystem
    {
        private bool _initialized;

        public void OnCreate(ref SystemState state)
        {
            UnityEngine.Debug.Log("[Space4XCameraInitializationSystem] System created!");
            // Don't require TimeState - create camera state regardless
        }

        public void OnUpdate(ref SystemState state)
        {
            UnityEngine.Debug.Log($"[Space4XCameraInitializationSystem] OnUpdate called, _initialized: {_initialized}");
            
            if (_initialized)
            {
                state.Enabled = false; // Only run once
                return;
            }

            var entityManager = state.EntityManager;

            // Initialize camera state singleton
            if (!SystemAPI.HasSingleton<Space4XCameraState>())
            {
                UnityEngine.Debug.Log("[Space4XCameraInitializationSystem] Creating camera state singleton...");
                var stateEntity = entityManager.CreateEntity(typeof(Space4XCameraState));
                // Initialize PerspectiveMode to false (RTS mode by default)
                
                // Find Main Camera GameObject and use its transform
                var mainCamera = GameObject.FindWithTag("MainCamera");
                // Better default camera position for RTS/demo viewing:
                // Higher up (y=25) for better overview, further back (z=-30) to see more area
                // Looking down at ~40 degrees for good visibility of terrain and entities
                float3 sceneCenter = new float3(0f, 0f, 0f);
                float3 initialPosition = new float3(0f, 25f, -30f);
                // Calculate pitch: angle from horizontal plane (atan2 of Y component vs horizontal distance)
                float horizontalDist = math.length(new float2(initialPosition.x - sceneCenter.x, initialPosition.z - sceneCenter.z));
                float initialPitch = math.atan2(initialPosition.y - sceneCenter.y, horizontalDist);
                float initialYaw = 0f; // Start facing forward (towards +Z)
                quaternion initialRotation = quaternion.Euler(initialPitch, initialYaw, 0f);

                if (mainCamera != null)
                {
                    var camPos = mainCamera.transform.position;
                    var camRot = mainCamera.transform.rotation;
                    
                    // Check if camera is positioned awkwardly - use better defaults if needed
                    // If camera is too high (>40), too low (<10), too close (z > -15), or looking wrong direction
                    bool needsRepositioning = camPos.y > 40f || camPos.y < 10f || camPos.z > -15f;
                    
                    // Also check if rotation is pointing too far up (pitch too low - looking up instead of down)
                    var euler = camRot.eulerAngles;
                    var pitchDegrees = euler.x;
                    if (pitchDegrees > 180f) pitchDegrees -= 360f;
                    // If pitch is less than 20 degrees (looking too horizontal) or negative (looking up), reposition
                    if (pitchDegrees < 20f || pitchDegrees < 0f)
                    {
                        needsRepositioning = true;
                    }
                    
                    if (needsRepositioning)
                    {
                        UnityEngine.Debug.Log($"[Space4XCameraInitializationSystem] Camera position {camPos} or rotation seems awkward, using optimized position for demo viewing");
                        initialPosition = new float3(0f, 25f, -30f);
                        float hDist = math.length(new float2(initialPosition.x - sceneCenter.x, initialPosition.z - sceneCenter.z));
                        initialPitch = math.atan2(initialPosition.y - sceneCenter.y, hDist);
                        initialYaw = 0f;
                        initialRotation = quaternion.Euler(initialPitch, initialYaw, 0f);
                        
                        // Update the actual GameObject transform too for immediate feedback
                        mainCamera.transform.position = initialPosition;
                        mainCamera.transform.rotation = initialRotation;
                    }
                    else
                    {
                        initialPosition = camPos;
                        initialRotation = camRot;
                        // Convert Unity's 0-360 euler angles to -180 to 180 range for pitch
                        initialPitch = math.radians(pitchDegrees);
                        initialYaw = math.radians(euler.y);
                    }
                    // Convert quaternion to Unity Quaternion for logging
                    var unityRot = new Quaternion(initialRotation.value.x, initialRotation.value.y, initialRotation.value.z, initialRotation.value.w);
                    UnityEngine.Debug.Log($"[Space4XCameraInitializationSystem] Found MainCamera at {initialPosition}, Euler: {unityRot.eulerAngles}, Pitch: {math.degrees(initialPitch)}, Yaw: {math.degrees(initialYaw)}");
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[Space4XCameraInitializationSystem] MainCamera not found, using optimized defaults for demo viewing");
                }

                var initialState = new Space4XCameraState
                {
                    Position = initialPosition,
                    Rotation = initialRotation,
                    Pitch = initialPitch,
                    Yaw = initialYaw,
                    PerspectiveMode = false // Start in RTS mode
                };

                entityManager.SetComponentData(stateEntity, initialState);
                UnityEngine.Debug.Log($"[Space4XCameraInitializationSystem] Camera state initialized at {initialPosition}, Pitch: {initialPitch}, Yaw: {initialYaw}");
            }
            else
            {
                UnityEngine.Debug.Log("[Space4XCameraInitializationSystem] Camera state singleton already exists, skipping creation");
            }

            // Initialize camera config singleton from profile or defaults
            if (!SystemAPI.HasSingleton<Space4XCameraConfig>())
            {
                var configEntity = entityManager.CreateEntity(typeof(Space4XCameraConfig));
                
                Space4XCameraConfig config;
                
                // Try to load profile asset
                #if UNITY_EDITOR
                var profilePath = UnityEditor.AssetDatabase.GUIDToAssetPath("7f8e9d0c1b2a3d4e5f6a7b8c9d0e1f2a");
                if (!string.IsNullOrEmpty(profilePath))
                {
                    var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<Authoring.Space4XCameraProfile>(profilePath);
                    if (profile != null)
                    {
                        config = profile.ToComponent();
                        UnityEngine.Debug.Log("[Space4XCameraInitializationSystem] Loaded camera config from profile");
                    }
                    else
                    {
                        config = GetDefaultConfig();
                        UnityEngine.Debug.Log("[Space4XCameraInitializationSystem] Profile not found, using defaults");
                    }
                }
                else
                {
                    config = GetDefaultConfig();
                    UnityEngine.Debug.Log("[Space4XCameraInitializationSystem] Using default camera config");
                }
                #else
                config = GetDefaultConfig();
                #endif

                entityManager.SetComponentData(configEntity, config);
                UnityEngine.Debug.Log($"[Space4XCameraInitializationSystem] Camera config initialized - PanSpeed: {config.PanSpeed}, VerticalPanSpeed: {config.VerticalPanSpeed}, EnablePan: {config.EnablePan}");
            }
            else
            {
                UnityEngine.Debug.Log("[Space4XCameraInitializationSystem] Camera config singleton already exists, skipping creation");
            }

            // Initialize camera input flags singleton used by MonoBehaviour controllers to coordinate with DOTS system
            if (!SystemAPI.HasSingleton<Space4XCameraInputFlags>())
            {
                var flagsEntity = entityManager.CreateEntity(typeof(Space4XCameraInputFlags));
                entityManager.SetComponentData(flagsEntity, new Space4XCameraInputFlags
                {
                    MovementHandled = false,
                    RotationHandled = false
                });
                UnityEngine.Debug.Log("[Space4XCameraInitializationSystem] Camera input flags singleton initialized");
            }
            else
            {
                UnityEngine.Debug.Log("[Space4XCameraInitializationSystem] Camera input flags singleton already exists, skipping creation");
            }

            if (!SystemAPI.HasSingleton<CameraInputBudget>())
            {
                var budgetEntity = entityManager.CreateEntity(typeof(CameraInputBudget));
                entityManager.SetComponentData(budgetEntity, default(CameraInputBudget));
                UnityEngine.Debug.Log("[Space4XCameraInitializationSystem] Camera input budget singleton initialized");
            }
            else
            {
                UnityEngine.Debug.Log("[Space4XCameraInitializationSystem] Camera input budget singleton already exists, skipping creation");
            }

            if (!SystemAPI.HasSingleton<Space4XCameraDiagnostics>())
            {
                var diagnosticsEntity = entityManager.CreateEntity(typeof(Space4XCameraDiagnostics));
                entityManager.SetComponentData(diagnosticsEntity, default(Space4XCameraDiagnostics));
                UnityEngine.Debug.Log("[Space4XCameraInitializationSystem] Camera diagnostics singleton initialized");
            }
            else
            {
                UnityEngine.Debug.Log("[Space4XCameraInitializationSystem] Camera diagnostics singleton already exists, skipping creation");
            }

            _initialized = true;
            UnityEngine.Debug.Log("[Space4XCameraInitializationSystem] Initialization complete, disabling system");
            state.Enabled = false; // Only run once
        }

        private static Space4XCameraConfig GetDefaultConfig()
        {
            return new Space4XCameraConfig
            {
                PanSpeed = 10f,
                VerticalPanSpeed = 10f, // Same speed as horizontal pan by default
                PanBoundsMin = new float3(-100f, 0f, -100f),
                PanBoundsMax = new float3(100f, 100f, 100f),
                UsePanBounds = false,
                ZoomSpeed = 5f,
                ZoomMinDistance = 10f,
                ZoomMaxDistance = 500f,
                RotationSpeed = 0.25f,
                PitchMin = math.radians(-30f),
                PitchMax = math.radians(85f),
                Smoothing = 0.1f,
                EnablePan = true,
                EnableZoom = true,
                EnableRotation = true
            };
        }
    }
}

